#!/usr/bin/env python3
"""Bayesian damage-anchor solve: the enemy damage anchor at one level that gives a target dungeon clear rate.

Same question as `dungeon-sim.py --solve`, answered with fewer sims. Bisection spends a fixed batch per step and
keeps only the bracket; this keeps every result. Clear rate p falls as the anchor scale s rises, modelled as a
dose-response curve,

    logit p(s) = logit(target) + slope * (ln s - ln s*)

with an exact posterior over (s*, slope) on a grid. Each round runs one dungeon-sim.py batch (fresh seeds, all classes)
at the scale expected to shrink the uncertainty on s* most (one-step lookahead), and the solve stops once the 90%
interval on s* is within --tolerance, or after --max-rounds. Dungeons a hero runs back to back are not independent;
--deff discounts them (dispersion measured 0.9-1.6 per class at L10, 2026-11-04).

Checked 2026-11-04 on fastbot-v14 L10 (reference root 1.217 from a 2,879-dungeon sweep): five solves landed 1.197-1.283
(<=5.5% off, every 90% interval held the reference) at 530-780 dungeons; dungeon-sim.py --solve answered 1.189-1.542
(up to 27% off) at ~1,100. Clear rate is shallow in the anchor (~16 pts from scale 1.1 to 1.5), so +/-10% is about what
~800 dungeons buy; the point estimate is usually within 5%.

  python Scripts/bo-solve.py --level 20 --spark 18                 (target from balance-targets.json realClearAnchors)
  python Scripts/bo-solve.py --level 50 --target 65 --tolerance 0.05 --max-rounds 20
  python Scripts/bo-solve.py --level 20 --prior surrogate     (centre the prior on Scripts/surrogate.py)

Rounds run in this process on one dungeon-sim.py shard pool: GameData is copied and the Spark session uploaded once per
solve, not once per round (that setup was ~8 s of a ~14 s round). With --spark the local slots default to 0 (a local
shard is ~2x slower, so the round would wait on it) and --shards defaults to filling the slots.

Prints the solved scale and anchor with a 90% interval, the dungeons it used, and a Provenance line.
"""
import argparse, json, math, os, re, shutil, subprocess, sys, tempfile
import numpy as np

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
TARGETS = json.load(open(os.path.join(ROOT, 'GameData', 'TuningProfiles', 'balance-targets.json'), encoding='utf-8'))

ap = argparse.ArgumentParser()
ap.add_argument('--level', type=int, required=True)
ap.add_argument('--target', type=float, default=None, help='target clear %% (default: realClearAnchors in balance-targets.json at --level)')
ap.add_argument('--patch', default='', help='balance patch name or path (default: active patch)')
ap.add_argument('--anchor', action='append', default=[], help='other damage anchor overrides L=multiplier (repeatable)')
ap.add_argument('--tools', default='Code/bin/BalanceTools', help='folder holding the built DF.dll')
ap.add_argument('--shards', type=int, default=0, help='dungeon-sim shards per class per round (0 = slots // 4, at least 3)')
ap.add_argument('--runs', type=int, default=2, help='heroes per shard')
ap.add_argument('--dungeons', type=int, default=4, help='dungeons per hero')
ap.add_argument('--tolerance', type=float, default=0.10, help='stop when the 90%% interval on the scale is within +/- this fraction')
ap.add_argument('--max-rounds', type=int, default=12)
ap.add_argument('--scale-range', default='0.25,4', help='scales the solve may consider, lo,hi')
ap.add_argument('--deff', type=float, default=1.5, help='design effect: each dungeon counts as 1/deff of an independent one')
ap.add_argument('--prior', default='flat', choices=['flat', 'surrogate'],
                help='flat = log-uniform scale prior; surrogate = centred on Scripts/surrogate.py (needs a model trained on this patch)')
ap.add_argument('--seed', type=int, default=11, help='round r runs dungeon-sim.py --seed seed*100+r (fresh heroes each round)')
ap.add_argument('--parallel', type=int, default=None, help='local shard slots (default: 0 with --spark, else half the cores)')
ap.add_argument('--spark', type=int, default=0, help='Spark slots (see Scripts/simpool.py)')
ap.add_argument('--spark-host', default='spark')
ap.add_argument('--bot', default='rules', choices=['smart', 'rules', 'basic'])
ap.add_argument('--region-mix', action='store_true')
ap.add_argument('--json', default='', help='write rounds and result here')
ap.add_argument('--scope', default='band', choices=['band', 'level'],
                help="band = scale every anchor in --level's 5-level region band together (the curve steps per band; solve at a band "
                     "midpoint so the hero's +/-1 dungeon level roll stays inside it); level = only the anchor at --level")
A = ap.parse_args()
LOCAL_DEFAULT = max(1, (os.cpu_count() or 8) // 2)
if A.parallel is None:
    A.parallel = 0 if A.spark else LOCAL_DEFAULT


def target_at(level):
    """Clear target at a level: linear between balance-targets.json realClearAnchors (as balance-scan.py reads them)."""
    pts = sorted((a['level'], float(a['clear'])) for a in TARGETS.get('realClearAnchors', []))
    if not pts:
        return float(TARGETS.get('realClearTarget', 75))
    if level <= pts[0][0]:
        return pts[0][1]
    for (l0, c0), (l1, c1) in zip(pts, pts[1:]):
        if level <= l1:
            return c0 + (level - l0) / (l1 - l0) * (c1 - c0)
    return pts[-1][1]


if A.target is None:
    A.target = target_at(A.level)

LO, HI = (math.log(float(x)) for x in A.scale_range.split(','))
GRID_R = np.linspace(LO, HI, 401)                    # ln s*
GRID_B = -np.exp(np.linspace(math.log(0.3), math.log(25), 120))   # slope of logit p per ln s (negative: more damage, fewer clears)
LOGIT_T = math.log(A.target / (100 - A.target))


def patch_path():
    name = A.patch
    if not name:
        prof = os.path.join(ROOT, 'GameData', 'PatchProfile.json')
        name = json.load(open(prof, encoding='utf-8-sig')).get('activeBalancePatch', 'default') if os.path.exists(prof) else 'default'
    if os.path.exists(name):
        return os.path.abspath(name)
    return os.path.join(ROOT, 'GameData', 'Patches', 'Balance', name if name.endswith('.json') else name + '.json')


def band_levels(level):
    """Levels of the 5-level region band holding `level` (1-5, 6-10, ...)."""
    start = 5 * ((level - 1) // 5) + 1
    return range(start, start + 5)


def solve_anchors(overrides):
    """Anchor level -> current multiplier for the anchors the solve scales together."""
    d = json.load(open(patch_path(), encoding='utf-8-sig'))
    cur = {a['level']: a['multiplier'] for a in d['enemySystem']['damageCurve']['anchors']}
    cur.update(overrides)
    if A.scope == 'level':
        return {A.level: base_anchor(A.level, overrides)}
    band = {lv: m for lv, m in cur.items() if lv in band_levels(A.level)}
    if not band:
        raise SystemExit(f'No damage anchors inside the L{A.level} band {band_levels(A.level)[0]}-{band_levels(A.level)[-1]}; use --scope level')
    return band


def base_anchor(level, overrides):
    """Damage multiplier at a level (linear between anchors), as dungeon-sim.py computes it."""
    d = json.load(open(patch_path(), encoding='utf-8-sig'))
    cur = {a['level']: a['multiplier'] for a in d['enemySystem']['damageCurve']['anchors']}
    cur.update(overrides)
    pts = sorted(cur.items())
    if level <= pts[0][0]:
        return pts[0][1]
    for (l0, m0), (l1, m1) in zip(pts, pts[1:]):
        if level <= l1:
            return m0 + (level - l0) / (l1 - l0) * (m1 - m0)
    return pts[-1][1]


def log_prior():
    """Log prior on the (ln s*, slope) grid."""
    lp_b = -0.5 * ((np.log(-GRID_B) - math.log(4)) / 1.2) ** 2      # slope around -4 per ln s, wide
    lp_r = np.zeros_like(GRID_R)
    if A.prior == 'surrogate':
        center, slope = surrogate_prior()
        if center is not None:
            lp_r = -0.5 * ((GRID_R - center) / 0.35) ** 2
            lp_b = -0.5 * ((np.log(-GRID_B) - math.log(max(0.3, -slope))) / 0.7) ** 2
    return lp_r[:, None] + lp_b[None, :]


def surrogate_prior():
    """Root and logit slope of the surrogate's enemyDamage response at this level, if it was trained on this patch."""
    sys.path.insert(0, os.path.join(ROOT, 'Scripts'))
    import importlib, joblib
    sur = importlib.import_module('surrogate')
    name = os.path.splitext(os.path.basename(patch_path()))[0]
    path = sur.bundle_path(name)
    if not os.path.exists(path):
        print(f'NOTE: no surrogate model for {name} ({path}); using the flat prior. Train one with surrogate-data.py + surrogate.py train.')
        return None, None
    b = joblib.load(path)
    lo, hi = sur.KNOBS['enemyDamage']
    xs = np.linspace(math.log(lo), math.log(hi), 41)
    p = np.clip([sur.predict(b, A.level, sur.CLASSES, {'enemyDamage': math.exp(x)})['clear'] for x in xs], 0.01, 0.99)
    logit = np.log(p / (1 - p))
    slope = float(np.polyfit(xs, logit, 1)[0])
    if slope >= -0.1:
        print('NOTE: surrogate shows no damage response here; using the flat prior.')
        return None, None
    i = int(np.argmin(np.abs(logit - LOGIT_T)))
    print(f'Surrogate prior: scale {math.exp(xs[i]):.3f}, slope {slope:.2f} per ln scale')
    return float(xs[i]), slope


def posterior(obs):
    lp = log_prior()
    for x, k, n in obs:
        z = LOGIT_T + GRID_B[None, :] * (x - GRID_R[:, None])
        # log p and log(1-p) without overflow; counts discounted by the design effect.
        lp = lp + (k / A.deff) * -np.logaddexp(0, -z) + ((n - k) / A.deff) * -np.logaddexp(0, z)
    w = np.exp(lp - lp.max())
    return w / w.sum()


def root_quantiles(w, qs):
    cdf = np.cumsum(w.sum(axis=1))
    return [float(GRID_R[min(np.searchsorted(cdf, q), len(GRID_R) - 1)]) for q in qs]


def next_point(w, n):
    """The candidate scale whose round of n dungeons is expected to shrink the posterior variance of ln s* most.
    Running only at the current best guess pins p there but never the slope, and the root's interval is roughly
    p's uncertainty over the slope, so it stalls (L50, 2026-11-04: +/-15% after 725 dungeons)."""
    cands = root_quantiles(w, [0.05, 0.15, 0.3, 0.5, 0.7, 0.85, 0.95])
    frac = (np.arange(40) + 0.5) / 40            # outcome grid: cleared fraction of the round
    n_eff = n / A.deff
    best, best_var = cands[3], float('inf')
    for x in sorted(set(cands)):
        z = LOGIT_T + GRID_B[None, :] * (x - GRID_R[:, None])
        log_p, log_q = -np.logaddexp(0, -z), -np.logaddexp(0, z)
        expected_var = 0.0
        lik = np.exp(n_eff * (frac[:, None, None] * log_p + (1 - frac[:, None, None]) * log_q))   # outcome x grid
        joint = lik * w[None]
        p_out = joint.sum(axis=(1, 2))
        p_out = p_out / p_out.sum()
        for j in range(len(frac)):
            if p_out[j] < 1e-6:
                continue
            post_r = joint[j].sum(axis=1)
            post_r = post_r / post_r.sum()
            mean = (post_r * GRID_R).sum()
            expected_var += p_out[j] * (post_r * (GRID_R - mean) ** 2).sum()
        if expected_var < best_var:
            best, best_var = x, expected_var
    return best


def open_sim():
    """dungeon-sim.py loaded as a module (its CLI parsed from these arguments) with GameData copied and the shard pool
    started once; every round then calls its measure(), so rounds share the scratch GameData and the Spark session."""
    import importlib.util
    argv = ['dungeon-sim.py', '--levels', str(A.level), '--patch', patch_path(), '--tools', A.tools, '--shards', str(max(1, A.shards)),
            '--runs', str(A.runs), '--dungeons', str(A.dungeons), '--seed', str(A.seed * 100), '--parallel', str(A.parallel),
            '--spark', str(A.spark), '--spark-host', A.spark_host, '--bot', A.bot] + (['--region-mix'] if A.region_mix else [])
    saved, sys.argv = sys.argv, argv
    try:
        spec = importlib.util.spec_from_file_location('dungeon_sim', os.path.join(ROOT, 'Scripts', 'dungeon-sim.py'))
        ds = importlib.util.module_from_spec(spec)
        spec.loader.exec_module(ds)
    finally:
        sys.argv = saved
    ds.copy_gamedata()
    log = lambda m: print(m, flush=True)
    ds.POOL = ds.SimPool(ROOT, ds.WORK, ds.DATA, ds.DLL, local_slots=A.parallel, spark_slots=A.spark, host=A.spark_host, log=log)
    try:
        ds.POOL.start()
    except SystemExit:
        # Spark unreachable and no local slots asked for: fall back to local.
        ds.POOL = ds.SimPool(ROOT, ds.WORK, ds.DATA, ds.DLL, local_slots=LOCAL_DEFAULT, log=log)
        ds.POOL.start()
    import atexit
    # Also on a failed round or Ctrl+C: drop the Spark session folder and the scratch GameData.
    atexit.register(lambda: (ds.POOL.close(), os.environ.get("BOSOLVE_KEEP") or shutil.rmtree(ds.WORK, ignore_errors=True)))
    if A.shards <= 0:
        ds.A.shards = max(3, ds.POOL.slots // len(ds.CLASSES))
    print(f'Shards on {ds.POOL.describe()}: {ds.A.shards} per class per round', flush=True)
    return ds


def run_round(ds, r, scale, overrides):
    ds.A.seed = A.seed * 100 + r          # fresh heroes each round (dungeon-sim.py --seed seed*100+r)
    anchors = {**overrides, **{lv: round(m * scale, 4) for lv, m in solve_anchors(overrides).items()}}
    return ds.measure(A.level, anchors, f'r{r}')


def main():
    overrides = {int(s.split('=')[0]): float(s.split('=')[1]) for s in A.anchor}
    base = base_anchor(A.level, overrides)
    scaled = solve_anchors(overrides)
    what = f'anchors {sorted(scaled)}' if A.scope == 'band' else 'anchor'
    print(f'L{A.level}: solving the damage {what} (L{A.level} now x{base:.4f}) for {A.target:.0f}% clear | patch {patch_path()}')
    ds = open_sim()
    obs, rounds, provenance = [], [], set()
    w = posterior(obs)
    for r in range(A.max_rounds):
        # A round's size, from the rounds so far (retreats end runs early) or the nominal batch.
        n_round = sum(o[2] for o in obs) / len(obs) if obs else ds.A.shards * len(ds.CLASSES) * A.runs * A.dungeons
        x = next_point(w, n_round)
        res = run_round(ds, r, math.exp(x), overrides)
        k, n = round(res['clear'] * res['dungeons']), res['dungeons']
        obs.append((x, k, n))
        # Each round's patch differs only in the anchor being solved, so its hash is dropped.
        provenance.update(re.sub(r'@\S+', '', p) for p in res['provenance'])
        w = posterior(obs)
        q05, q50, q95 = root_quantiles(w, [0.05, 0.5, 0.95])
        rounds.append({'round': r, 'scale': math.exp(x), 'anchor': base * math.exp(x), 'cleared': k, 'dungeons': n,
                       'clear': res['clear'], 'clearCI': res['clearCI'], 'perClassClear': res['perClassClear'],
                       'solved': math.exp(q50), 'solvedCI90': [math.exp(q05), math.exp(q95)]})
        print(f'  round {r}: scale {math.exp(x):.3f} (anchor x{base * math.exp(x):.4f})  clear {k}/{n} = {res["clear"] * 100:5.1f}%  '
              f'-> solved scale {math.exp(q50):.3f}  90% [{math.exp(q05):.3f}-{math.exp(q95):.3f}]', flush=True)
        half_width = (math.exp(q95) / math.exp(q05)) ** 0.5 - 1
        if half_width <= A.tolerance:
            break
    q05, q50, q95 = root_quantiles(w, [0.05, 0.5, 0.95])
    used = sum(n for _, _, n in obs)
    converged = (math.exp(q95) / math.exp(q05)) ** 0.5 - 1 <= A.tolerance
    print(f'\n{"Solved" if converged else "NOT converged (raise --max-rounds or --tolerance)"}: L{A.level} damage anchor '
          f'x{base * math.exp(q50):.4f} (scale {math.exp(q50):.3f}, 90% [{base * math.exp(q05):.4f}-{base * math.exp(q95):.4f}]) '
          f'for {A.target:.0f}% clear | {len(obs)} rounds, {used} dungeons')
    if math.exp(q50) < math.exp(LO) * 1.05 or math.exp(q50) > math.exp(HI) / 1.05:
        print('WARNING: the answer sits at the edge of --scale-range; the target may be out of reach there.')
    if A.scope == 'band':
        print('Band anchors: ' + ', '.join(f'{lv}={m * math.exp(q50):.4f}' for lv, m in sorted(scaled.items())))
    print(f'Provenance: {sorted(provenance)}')
    if A.json:
        json.dump({'level': A.level, 'target': A.target, 'baseAnchor': base, 'converged': converged, 'dungeons': used,
                   'scale': math.exp(q50), 'anchor': base * math.exp(q50), 'anchorCI90': [base * math.exp(q05), base * math.exp(q95)],
                   'scope': A.scope, 'anchors': {str(lv): round(m * math.exp(q50), 4) for lv, m in scaled.items()},
                   'rounds': rounds, 'provenance': sorted(provenance)}, open(A.json, 'w', encoding='utf-8'), indent=2)
    print(f'Shards by host: {ds.POOL.counts}, ssh retries {ds.POOL.retries}')


if __name__ == '__main__':
    main()
