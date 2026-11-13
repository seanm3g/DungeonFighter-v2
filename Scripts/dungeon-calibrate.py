#!/usr/bin/env python3
"""Even out the 12 region dungeons inside each level band: solve enemySystem.dungeonScale (damage per dungeon theme).

A region band holds three dungeons (Den / Labyrinth / Dense Brush, ...). One band knob sets the band's level; this sets
each dungeon's damage so its clear rate matches the other two in the same band. Each pass runs dungeon-sim.py at every
band midpoint (all three dungeons rotate, per-dungeon outcomes in the JSON), compares each dungeon's clear rate with its
band's mean in log-odds, and moves its damage multiplier by exp(gain x gap). The three dungeons of a region are then
renormalised to a geometric mean of 1, so the band curves keep their meaning. Writes an overlay patch after each pass.

  python Scripts/dungeon-calibrate.py --patch affix-v44a --next affix-v44d --iterations 4

Then solve the band knobs (balance-loop.py / balance-chain.py) from --next, and confirm on fresh seeds.
"""
import argparse, json, math, os, subprocess, sys

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
PATCH_DIR = os.path.join(ROOT, 'GameData', 'Patches', 'Balance')
RUN_DIR = os.path.join(ROOT, 'GameData', 'TuningRuns', 'dungeon-calibrate')
MIDPOINTS = '3,8,13,18,23,28,33,38,43,48,53,58,63,68,73'

ap = argparse.ArgumentParser(description=__doc__, formatter_class=argparse.RawDescriptionHelpFormatter)
ap.add_argument('--patch', required=True, help='base patch (the overlay builds on it)')
ap.add_argument('--next', required=True, help='name of the overlay patch to write')
ap.add_argument('--levels', default=MIDPOINTS)
ap.add_argument('--iterations', type=int, default=4)
ap.add_argument('--shards', type=int, default=16)
ap.add_argument('--spark', default='auto')
ap.add_argument('--parallel', type=int, default=-1, help='local slots for dungeon-sim (-1 = its default)')
ap.add_argument('--seed-base', type=int, default=15000, help='pass i uses seed seed-base + i')
ap.add_argument('--gain', type=float, default=0.6, help='damage step per unit of log-odds gap (higher clear -> more damage)')
ap.add_argument('--max-step', type=float, default=1.6, help='largest change to one multiplier in one pass (x)')
ap.add_argument('--damage-range', default='0.5,2.0', help='damage scale bounds; a step past them moves HP instead')
ap.add_argument('--health-exponent', type=float, default=0.7, help='HP change per unit of damage change once damage is at a bound')
ap.add_argument('--health-range', default='0.5,1.6')
ap.add_argument('--tools', default='Code/bin/BalanceTools')
ap.add_argument('--bot', default='synergy')
A = ap.parse_args()

AFFIX = json.load(open(os.path.join(ROOT, 'GameData', 'EnemyAffixes.json'), encoding='utf-8'))
REGION_OF = {d['theme']: d['region'] for d in AFFIX['dungeons']}
THEMES = list(REGION_OF)


def logit(p):
    p = min(0.97, max(0.03, p))
    return math.log(p / (1 - p))


def write_patch(scale, health):
    path = os.path.join(PATCH_DIR, A.next + '.json')
    doc = {'base': A.patch,
           'description': f'Per-dungeon enemy damage and HP (enemySystem.dungeonScale) from Scripts/dungeon-calibrate.py on {A.patch}.',
           'enemySystem': {'dungeonScale': {t: {'damage': round(v, 4), 'health': round(health[t], 4)} for t, v in sorted(scale.items())}}}
    # balancepatch.write_patch writes it whole when A.patch is already at the 16-base overlay cap.
    import balancepatch
    base = doc.pop('base')
    balancepatch.write_patch(path, balancepatch.merge(balancepatch.resolve(base, PATCH_DIR), doc), base=base, patch_dir=PATCH_DIR)
    return path


def clamp_into(t, scale, health, dlo, dhi, hlo, hhi):
    """Damage past a bound moves into HP: a dungeon that is still too hard at the damage floor gets less HP (shorter
    fights, fewer DoT ticks and heals); one still too easy at the ceiling gets more."""
    if scale[t] < dlo:
        health[t] *= (scale[t] / dlo) ** A.health_exponent
        scale[t] = dlo
    elif scale[t] > dhi:
        health[t] *= (scale[t] / dhi) ** A.health_exponent
        scale[t] = dhi
    health[t] = min(hhi, max(hlo, health[t]))


def run_sim(it, seed):
    os.makedirs(RUN_DIR, exist_ok=True)
    out = os.path.join(RUN_DIR, f'{A.next}-pass{it}-s{seed}.json')
    cmd = [sys.executable, os.path.join(ROOT, 'Scripts', 'dungeon-sim.py'), '--levels', A.levels, '--patch', A.next,
           '--shards', str(A.shards), '--seed', str(seed), '--spark', A.spark, '--tools', A.tools, '--bot', A.bot,
           '--json', out, '--batch', f'dungeon-calibrate-{A.next}:{it}/{A.iterations}']
    if A.parallel >= 0:
        cmd += ['--parallel', str(A.parallel)]
    with open(out.replace('.json', '.log'), 'w', encoding='utf-8') as log:
        rc = subprocess.run(cmd, cwd=ROOT, stdout=log, stderr=subprocess.STDOUT).returncode
    if rc != 0:
        sys.exit(f'dungeon-sim failed (exit {rc}); see {out.replace(".json", ".log")}')
    return json.load(open(out, encoding='utf-8'))


def main():
    # Start from the base patch's scale when it has one, else 1 for every theme.
    scale = {t: 1.0 for t in THEMES}
    health = {t: 1.0 for t in THEMES}
    dlo, dhi = (float(x) for x in A.damage_range.split(','))
    hlo, hhi = (float(x) for x in A.health_range.split(','))
    sys.path.insert(0, os.path.join(ROOT, 'Scripts'))
    try:
        import balancepatch
        base = balancepatch.resolve(A.patch)
        for t, v in ((base.get('enemySystem') or {}).get('dungeonScale') or {}).items():
            if t in scale and isinstance(v, dict):
                scale[t] = float(v.get('damage', 1.0))
                health[t] = float(v.get('health', 1.0))
    except Exception as ex:  # a missing base is caught by dungeon-sim below
        print(f'note: could not read {A.patch} dungeonScale ({ex}); starting at 1', flush=True)
    # Start inside the bounds (an earlier unbounded pass may have left a theme outside them).
    for t in THEMES:
        clamp_into(t, scale, health, dlo, dhi, hlo, hhi)
    write_patch(scale, health)

    for it in range(1, A.iterations + 1):
        seed = A.seed_base + it
        results = run_sim(it, seed)
        gaps = {t: [] for t in THEMES}
        print(f'\nPass {it} (seed {seed}): clear by dungeon, band mean, log-odds gap', flush=True)
        for r in results:
            pd = {t: v for t, v in (r.get('perDungeon') or {}).items() if t in REGION_OF and v.get('dungeons')}
            if len(pd) < 2:
                continue
            band = sum(v['clear'] * v['dungeons'] for v in pd.values()) / sum(v['dungeons'] for v in pd.values())
            cells = []
            for t, v in pd.items():
                g = logit(v['clear']) - logit(band)
                gaps[t].append((g, v['dungeons']))
                cells.append(f"{t} {v['clear'] * 100:4.0f}%")
            print(f"  L{r['level']:<3} band {band * 100:4.0f}%  " + '  '.join(cells), flush=True)
        for t in THEMES:
            if not gaps[t]:
                continue
            w = sum(n for _, n in gaps[t])
            g = sum(x * n for x, n in gaps[t]) / w
            step = min(A.max_step, max(1 / A.max_step, math.exp(A.gain * g)))
            scale[t] *= step
            clamp_into(t, scale, health, dlo, dhi, hlo, hhi)
        # Keep each region's three dungeons centred on 1 (geometric mean): band knobs set the level.
        for region in sorted(set(REGION_OF.values())):
            ts = [t for t in THEMES if REGION_OF[t] == region]
            gm = math.exp(sum(math.log(scale[t]) for t in ts) / len(ts))
            for t in ts:
                scale[t] /= gm
        for t in THEMES:
            clamp_into(t, scale, health, dlo, dhi, hlo, hhi)
        path = write_patch(scale, health)
        print('  damage scale: ' + '  '.join(f'{t} x{scale[t]:.3f}' for t in THEMES), flush=True)
        print('  HP scale:     ' + '  '.join(f'{t} x{health[t]:.3f}' for t in THEMES), flush=True)
        print(f'  wrote {path}', flush=True)


if __name__ == '__main__':
    main()
