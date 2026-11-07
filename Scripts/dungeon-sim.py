#!/usr/bin/env python3
"""Real dungeon clear / retreat / death rates at fixed hero levels (DUNGEONSIM, sharded), and a damage-anchor solver.

Each shard is one DUNGEONSIM process (one class): fresh heroes raised to the level with collected loot, the bot (rules by default, --bot smart)
choosing gear / skills / sequence, then several dungeons through the real game loop with HP carried between fights and
the retreat rule. Shards run in parallel on a scratch copy of GameData (death screens write saves).

  python Scripts/dungeon-sim.py --levels 8,18 --patch my-candidate --seed 707 --shards 8
  python Scripts/dungeon-sim.py --levels 18 --anchor 20=1.2 --anchor 15=0.7      (try a damage curve variant)
  python Scripts/dungeon-sim.py --levels 8,18 --spark 18                          (also run 18 shards at once on the DGX Spark)

Solving an anchor: use Scripts/bo-solve.py. --solve (bisection) is deprecated: it reuses one seed set every step and
landed up to 27% off at L10 (2026-11-04).

Rate intervals are 95% intervals over shards: a shard's heroes and dungeons share builds, so dungeons are not
independent. The interval is a t interval over per-shard clear rates, never narrower than the Wilson interval on the
dungeon count ('clearCIDungeons' keeps that one). Use --shards 8+ (16 heroes per class) to read class gaps, and confirm
a change on a fresh --seed, paired against the base patch on the same seed.
"""
import argparse, json, math, os, re, shutil, subprocess, sys, tempfile
from concurrent.futures import ThreadPoolExecutor

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
sys.path.insert(0, os.path.join(ROOT, 'Scripts'))
from simpool import SIM_SKIP, SimPool, add_args as add_pool_args
import balancepatch as bp
from simstats import wilson, cluster_interval
TARGETS = json.load(open(os.path.join(ROOT, 'GameData', 'TuningProfiles', 'balance-targets.json'), encoding='utf-8'))
CLASSES = ['Barbarian', 'Warrior', 'Rogue', 'Wizard']

ap = argparse.ArgumentParser()
ap.add_argument('--levels', default='10', help='comma list of hero levels')
ap.add_argument('--patch', default='', help='balance patch name or path (default: active patch)')
ap.add_argument('--tools', default='Code/bin/BalanceTools', help='folder holding the built DF.dll')
ap.add_argument('--shards', type=int, default=6, help='processes per class')
ap.add_argument('--runs', type=int, default=2, help='heroes per shard')
ap.add_argument('--dungeons', type=int, default=6, help='dungeons per hero')
ap.add_argument('--retreat', type=int, default=int(TARGETS.get('retreatBelowHpPercent', 50)))
ap.add_argument('--start-loot-per-level', type=int, default=0, help='drops per level in the starting bag (0 = tool default)')
ap.add_argument('--anchor', action='append', default=[], help='enemy damage band override L=multiplier: sets the 5-level band holding L (repeatable)')
ap.add_argument('--solve', type=int, default=0, help='DEPRECATED (use Scripts/bo-solve.py): bisect the damage anchor at this level to hit --target clear %%')
ap.add_argument('--target', type=float, default=0, help='--solve: target clear %%')
ap.add_argument('--iterations', type=int, default=5)
ap.add_argument('--scale-range', default='0.25,4', help='--solve: bracket for the anchor scale, lo,hi')
ap.add_argument('--json', default='', help='write results here')
ap.add_argument('--seed', type=int, default=1, help='base seed; each shard derives its own, so reruns replay the same heroes (0 = unseeded)')
ap.add_argument('--parallel', type=int, default=os.cpu_count() or 8,
                help='shards at once; all levels share one pool, so no level waits on the slowest shard of another')
ap.add_argument('--bot', default='rules', choices=['smart', 'rules', 'basic'],
                help='rules (default) = fast rule-based picks (GameData/TuningProfiles/rules-bot.json); smart = picks by simulation, ~40x slower')
ap.add_argument('--region-mix', action='store_true',
                help='runs rotate through the nearest region of each theme (Forest/Water/Fire/Air) instead of only the region whose band holds the level')
ap.add_argument('--region-theme', help='every run starts in the nearest region of this theme (Forest / Water / Fire / Air)')
ap.add_argument('--explore-strips', action='store_true', help='rules bot plays random strips per dungeon (owned-action pool for Scripts/rules-bot-climb.py --explore)')
ap.add_argument('--classes', default=','.join(CLASSES), help='comma list of classes to run')
ap.add_argument('--screen-fights', type=int, default=-1, help='bot screen fights per enemy for skill/gear picks (0 = score every candidate in full, -1 = tool default)')
ap.add_argument('--eval-enemies', default='', help='comma list of enemies the bot scores skill/gear/strip picks against (empty = tool default)')
ap.add_argument('--material', default='', help='every loot roll is this material (Bone, Iron, ...): the run tests that set as the dominant build')
ap.add_argument('--overlay',default='', help='folder of GameData files copied over the scratch GameData (test data edits without touching the repo)')
ap.add_argument('--keep', default='', help='copy the raw shard JSON (fight rows) into this folder')
ap.add_argument('--shard-timeout', type=int, default=1800, help='seconds before a shard is killed and dropped (a stuck fight)')
add_pool_args(ap)
A = ap.parse_args()
CLASSES = [c.strip() for c in A.classes.split(',') if c.strip()]

DLL = os.path.join(ROOT, A.tools, 'DF.dll')
WORK = tempfile.mkdtemp(prefix='dsim-')
DATA = os.path.join(WORK, 'GameData')
POOL = None


def active_patch_path():
    name = A.patch
    if not name:
        prof = os.path.join(ROOT, 'GameData', 'PatchProfile.json')
        name = json.load(open(prof, encoding='utf-8-sig')).get('activeBalancePatch', 'default') if os.path.exists(prof) else 'default'
    if os.path.exists(name):
        return os.path.abspath(name)
    return os.path.join(ROOT, 'GameData', 'Patches', 'Balance', name if name.endswith('.json') else name + '.json')


def copy_gamedata():
    skip_dirs = SIM_SKIP
    def ignore(d, names):
        return [n for n in names if (n in skip_dirs and os.path.isdir(os.path.join(d, n))) or n.startswith('character_')]
    shutil.copytree(os.path.join(ROOT, 'GameData'), DATA, ignore=ignore)
    if A.overlay:
        shutil.copytree(A.overlay, DATA, dirs_exist_ok=True)


def _apply(d, overrides):
    """overrides: {level: value} sets the enemy damage band holding that level; {(curve, level): value} sets another
    curve's band, curve = 'damage' | 'health' | 'class:<Class>' (hero damage curve of one class)."""
    for k, v in overrides.items():
        curve, level = k if isinstance(k, tuple) else ('damage', k)
        c = bp.get_curve(d, None, curve.split(':', 1)[1].lower()) if curve.startswith('class:') else bp.get_curve(d, curve)
        c['enabled'] = True
        bp.set_band(c, int(level), float(v))
    return d


def write_variant(overrides, tag):
    """Full (resolved) patch copy with curve bands overridden; returns its path."""
    d = _apply(bp.resolve(active_patch_path()), overrides)
    path = os.path.join(WORK, f'patch-{tag}.json')
    json.dump(d, open(path, 'w', encoding='utf-8'), indent=2)
    return path


def damage_anchor_at(level, overrides):
    """Effective enemy damage multiplier at a level (band value x base), after overrides."""
    d = _apply(bp.resolve(active_patch_path()), {k: v for k, v in overrides.items() if not isinstance(k, tuple) or k[0] == 'damage'})
    return bp.value_at(bp.get_curve(d, 'damage'), level)


def shard(patch, level, cls, i, tag):
    out = os.path.join(WORK, f'{tag}-L{level}-{cls}-{i}.json')
    args = ['DUNGEONSIM', '--patch', patch, '--level', str(level), '--runs', str(A.runs),
            '--dungeons', str(A.dungeons), '--classes', cls, '--retreat-below', str(A.retreat), '--json', out,
            '--bot', A.bot]
    if A.explore_strips:
        args += ['--explore-strips']
    if A.region_mix:
        args += ['--region-mix']
    if A.region_theme:
        args += ['--region-theme', A.region_theme]
    if A.start_loot_per_level > 0:
        args += ['--start-loot-per-level', str(A.start_loot_per_level)]
    if A.screen_fights >= 0:
        args += ['--screen-fights', str(A.screen_fights)]
    if A.eval_enemies:
        args += ['--eval-enemies', A.eval_enemies]
    if A.material:
        args += ['--material', A.material]
    if A.seed:
        args += ['--seed', str(A.seed * 1_000_000 + level * 1000 + i)]
    log = os.path.join(WORK, f'{tag}-L{level}-{cls}-{i}.log')
    host = POOL.run(args, out, log, timeout=A.shard_timeout)
    try:
        doc = json.load(open(out, encoding='utf-8'))
    except Exception:
        print(f'     WARNING: shard {cls}-{i} (L{level}, {tag}) on {host} gave no result (timeout or crash) and was dropped; see {log}', flush=True)
        return None
    doc['Host'] = host
    return doc


def measure(level, anchors, tag):
    patch = write_variant(anchors, tag)
    jobs = [(c, i) for c in CLASSES for i in range(A.shards)]
    with ThreadPoolExecutor(min(len(jobs), POOL.slots)) as ex:
        docs = [d for d in ex.map(lambda j: shard(patch, level, j[0], j[1], tag), jobs) if d]
    return summarize(level, anchors, docs)


def measure_levels(levels, anchors):
    """All levels in one pool (no per-level barrier): returns results in level order."""
    patch = write_variant(anchors, 'm')
    jobs = [(l, c, i) for l in levels for c in CLASSES for i in range(A.shards)]
    with ThreadPoolExecutor(min(len(jobs), POOL.slots)) as ex:
        out = list(ex.map(lambda j: (j[0], shard(patch, j[0], j[1], j[2], f'm{j[0]}')), jobs))
    return [summarize(l, anchors, [d for lv, d in out if lv == l and d]) for l in levels]


def summarize(level, anchors, docs):
    if not docs:
        raise SystemExit(f'No DUNGEONSIM output for L{level}; see logs in {WORK}')
    # The DLL hash differs between the Windows and linux-arm64 builds of the same source (simpool keeps them in sync).
    provs = {re.sub(r'build=\S+ ', '', d['Provenance']) for d in docs}
    tot = {'Runs': 0, 'Dungeons': 0, 'Cleared': 0, 'Retreated': 0, 'Died': 0, 'Fights': 0, 'HpLost': 0.0, 'HeroActions': 0.0, 'LevelDrift': 0}
    per_class = {}
    shard_units = {'Cleared': [], 'Retreated': [], 'Died': []}
    class_units = {}
    for d in docs:
        sk = {'Cleared': 0, 'Retreated': 0, 'Died': 0, 'Dungeons': 0}
        for o in d['Outcomes']:
            if o['Class'] == 'all':
                continue
            pc = per_class.setdefault(o['Class'], {'Dungeons': 0, 'Cleared': 0, 'Died': 0, 'Fights': 0, 'EnemyActions': 0.0})
            pc['Dungeons'] += o['Dungeons']; pc['Cleared'] += o['Cleared']; pc['Died'] += o['Died']
            pc['Fights'] += o['Fights']; pc['EnemyActions'] += o.get('EnemyActionsPerFight', 0) * o['Fights']
            for k in ('Runs', 'Dungeons', 'Cleared', 'Retreated', 'Died', 'Fights', 'LevelDrift'):
                tot[k] += o[k]
            for k in sk:
                sk[k] += o[k]
            class_units.setdefault(o['Class'], []).append((o['Cleared'], o['Dungeons']))
            tot['HpLost'] += o['HpLostPerFight'] * o['Fights']
            tot['HeroActions'] += o.get('HeroActionsPerFight', 0) * o['Fights']
        if sk['Dungeons']:
            for k in shard_units:
                shard_units[k].append((sk[k], sk['Dungeons']))
    n = tot['Dungeons']
    res = {
        'level': level, 'anchors': {(f'{k[0]}@{k[1]}' if isinstance(k, tuple) else str(k)): v for k, v in anchors.items()}, 'damageMultiplier': round(damage_anchor_at(level, anchors), 4),
        'dungeons': n, 'runs': tot['Runs'],
        'clear': tot['Cleared'] / n if n else 0, 'clearCI': cluster_interval(shard_units['Cleared']),
        'clearCIDungeons': wilson(tot['Cleared'], n), 'shards': len(shard_units['Cleared']),
        'retreat': tot['Retreated'] / n if n else 0, 'retreatCI': cluster_interval(shard_units['Retreated']),
        'death': tot['Died'] / n if n else 0, 'deathCI': cluster_interval(shard_units['Died']),
        'hpLostPerFight': tot['HpLost'] / tot['Fights'] if tot['Fights'] else 0,
        'heroActionsPerFight': tot['HeroActions'] / tot['Fights'] if tot['Fights'] else 0,
        'levelDrift': tot['LevelDrift'],
        'perClassClear': {c: round(v['Cleared'] / v['Dungeons'], 3) if v['Dungeons'] else None for c, v in per_class.items()},
        'perClassClearCI': {c: [round(x, 3) for x in cluster_interval(u)] for c, u in class_units.items()},
        # Enemy attacks the hero takes per fight, by class (class-parity gauge).
        'perClassEnemyActs': {c: round(v['EnemyActions'] / v['Fights'], 2) if v['Fights'] else None for c, v in per_class.items()},
        'provenance': sorted(provs),
    }
    return res


def show(r):
    pct = lambda ci: f'[{ci[0] * 100:.0f}-{ci[1] * 100:.0f}]'
    print(f"L{r['level']:<3} dmg x{r['damageMultiplier']:<6} {r['dungeons']:4} dungeons  "
          f"clear {r['clear'] * 100:5.1f}% {pct(r['clearCI']):9}  retreat {r['retreat'] * 100:5.1f}% {pct(r['retreatCI']):9}  "
          f"death {r['death'] * 100:4.1f}% {pct(r['deathCI']):9}  HP lost/fight {r['hpLostPerFight'] * 100:4.1f}%  hero acts {r['heroActionsPerFight']:4.1f}  "
          f"by class {r['perClassClear']}  enemy acts/fight {r['perClassEnemyActs']}", flush=True)
    if r['levelDrift']:
        print(f"     WARNING: {r['levelDrift']} hero(es) levelled inside a dungeon", flush=True)
    if len(r['provenance']) > 1:
        print(f"     WARNING: shards ran different code/data: {r['provenance']}", flush=True)


def main():
    global POOL
    copy_gamedata()
    POOL = SimPool(ROOT, WORK, DATA, DLL, local_slots=A.parallel, spark_slots=A.spark, host=A.spark_host,
                   log=lambda m: print(m, flush=True))
    POOL.start()
    print(f'Patch: {active_patch_path()} | shards on {POOL.describe()}')
    overrides = {int(s.split('=')[0]): float(s.split('=')[1]) for s in A.anchor}
    results = []
    if A.solve:
        print('WARNING: --solve (bisection on one seed set) is deprecated and was up to 27% off; use '
              f'python Scripts/bo-solve.py --level {A.solve} instead.', flush=True)
        if not A.target:
            raise SystemExit('--solve needs --target')
        level, target = A.solve, A.target / 100.0
        base = damage_anchor_at(level, overrides)
        # Clear rate falls as enemy damage rises: bisect a scale on the anchor at this level (log space).
        lo, hi = (float(x) for x in A.scale_range.split(','))
        for it in range(A.iterations):
            scale = math.sqrt(lo * hi)
            r = measure(level, {**overrides, level: round(base * scale, 4)}, f's{it}')
            show(r); results.append(r)
            if r['clear'] > target:
                lo = scale
            else:
                hi = scale
        best = min(results, key=lambda r: abs(r['clear'] - target))
        print(f"\nBest: L{level} damage anchor {best['damageMultiplier']} -> clear {best['clear'] * 100:.1f}% "
              f"[{best['clearCI'][0] * 100:.0f}-{best['clearCI'][1] * 100:.0f}] (target {A.target:.0f}%)")
    else:
        for r in measure_levels([int(x) for x in A.levels.split(',') if x.strip()], overrides):
            show(r); results.append(r)
    print(f'Provenance: {sorted({p for r in results for p in r["provenance"]})}')
    if A.spark:
        print(f'Shards by host: {POOL.counts}, ssh retries {POOL.retries}')
    if A.json:
        json.dump(results, open(A.json, 'w', encoding='utf-8'), indent=2)
    if A.keep:
        os.makedirs(A.keep, exist_ok=True)
        for f in os.listdir(WORK):
            if f.endswith('.json') and not f.startswith('patch-'):
                shutil.copy(os.path.join(WORK, f), A.keep)
    POOL.close()
    shutil.rmtree(WORK, ignore_errors=True)


if __name__ == '__main__':
    main()
