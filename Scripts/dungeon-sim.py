#!/usr/bin/env python3
"""Real dungeon clear / retreat / death rates at fixed hero levels (DUNGEONSIM, sharded), and a damage-anchor solver.

Each shard is one DUNGEONSIM process (one class): fresh heroes raised to the level with collected loot, the smart bot
choosing gear / skills / sequence, then several dungeons through the real game loop with HP carried between fights and
the retreat rule. Shards run in parallel on a scratch copy of GameData (death screens write saves).

  python Scripts/dungeon-sim.py --levels 10,20 --patch overnight-v2
  python Scripts/dungeon-sim.py --levels 20 --anchor 20=1.2 --anchor 15=0.7      (try a damage curve variant)
  python Scripts/dungeon-sim.py --solve 20 --target 70                           (bisect the L20 damage anchor to 70% clear)

Rates carry 95% Wilson intervals; a result is only "on target" when the interval overlaps target +/- tolerance.
"""
import argparse, json, math, os, shutil, subprocess, sys, tempfile
from concurrent.futures import ThreadPoolExecutor

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
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
ap.add_argument('--anchor', action='append', default=[], help='damage anchor override L=multiplier (repeatable)')
ap.add_argument('--solve', type=int, default=0, help='bisect the damage anchor at this level to hit --target clear %%')
ap.add_argument('--target', type=float, default=0, help='--solve: target clear %%')
ap.add_argument('--iterations', type=int, default=5)
ap.add_argument('--scale-range', default='0.25,4', help='--solve: bracket for the anchor scale, lo,hi')
ap.add_argument('--json', default='', help='write results here')
ap.add_argument('--parallel', type=int, default=os.cpu_count() or 8,
                help='shards at once; all levels share one pool, so no level waits on the slowest shard of another')
ap.add_argument('--shard-timeout', type=int, default=1800, help='seconds before a shard is killed and dropped (a stuck fight)')
A = ap.parse_args()

DLL = os.path.join(ROOT, A.tools, 'DF.dll')
WORK = tempfile.mkdtemp(prefix='dsim-')
DATA = os.path.join(WORK, 'GameData')


def active_patch_path():
    name = A.patch
    if not name:
        prof = os.path.join(ROOT, 'GameData', 'PatchProfile.json')
        name = json.load(open(prof, encoding='utf-8-sig')).get('activeBalancePatch', 'default') if os.path.exists(prof) else 'default'
    if os.path.exists(name):
        return os.path.abspath(name)
    return os.path.join(ROOT, 'GameData', 'Patches', 'Balance', name if name.endswith('.json') else name + '.json')


def copy_gamedata():
    skip_dirs = {'Audio', 'Video', 'CombatLogs', 'LabSnapshots', 'TuningRuns'}
    def ignore(d, names):
        return [n for n in names if (n in skip_dirs and os.path.isdir(os.path.join(d, n))) or n.startswith('character_')]
    shutil.copytree(os.path.join(ROOT, 'GameData'), DATA, ignore=ignore)


def write_variant(anchors, tag):
    """Patch copy with damage anchors overridden; returns its path."""
    d = json.load(open(active_patch_path(), encoding='utf-8-sig'))
    cur = {a['level']: a['multiplier'] for a in d['enemySystem']['damageCurve']['anchors']}
    cur.update(anchors)
    d['enemySystem']['damageCurve']['anchors'] = [{'level': k, 'multiplier': v} for k, v in sorted(cur.items())]
    path = os.path.join(WORK, f'patch-{tag}.json')
    json.dump(d, open(path, 'w', encoding='utf-8'), indent=2)
    return path


def damage_anchor_at(level, overrides):
    """Effective damage multiplier at a level (linear between anchors), after overrides."""
    d = json.load(open(active_patch_path(), encoding='utf-8-sig'))
    cur = {a['level']: a['multiplier'] for a in d['enemySystem']['damageCurve']['anchors']}
    cur.update(overrides)
    pts = sorted(cur.items())
    if level <= pts[0][0]:
        return pts[0][1]
    for (l0, m0), (l1, m1) in zip(pts, pts[1:]):
        if level <= l1:
            return m0 + (level - l0) / (l1 - l0) * (m1 - m0)
    return pts[-1][1]


def shard(patch, level, cls, i, tag):
    out = os.path.join(WORK, f'{tag}-L{level}-{cls}-{i}.json')
    args = ['dotnet', DLL, 'DUNGEONSIM', '--patch', patch, '--level', str(level), '--runs', str(A.runs),
            '--dungeons', str(A.dungeons), '--classes', cls, '--retreat-below', str(A.retreat), '--json', out]
    if A.start_loot_per_level > 0:
        args += ['--start-loot-per-level', str(A.start_loot_per_level)]
    env = dict(os.environ, DF_GAMEDATA_DIR=DATA)
    log = open(os.path.join(WORK, f'{tag}-L{level}-{cls}-{i}.log'), 'w', encoding='utf-8')
    try:
        subprocess.run(args, cwd=ROOT, stdout=log, stderr=subprocess.STDOUT, env=env, timeout=A.shard_timeout)
    except subprocess.TimeoutExpired:
        print(f'     WARNING: shard {cls}-{i} (L{level}, {tag}) passed {A.shard_timeout}s and was dropped', flush=True)
        return None
    try:
        return json.load(open(out, encoding='utf-8'))
    except Exception:
        return None


def wilson(k, n, z=1.96):
    if n == 0:
        return 0.0, 1.0
    p = k / n
    den = 1 + z * z / n
    c = (p + z * z / (2 * n)) / den
    h = z * math.sqrt(p * (1 - p) / n + z * z / (4 * n * n)) / den
    return max(0.0, c - h), min(1.0, c + h)


def measure(level, anchors, tag):
    patch = write_variant(anchors, tag)
    jobs = [(c, i) for c in CLASSES for i in range(A.shards)]
    with ThreadPoolExecutor(min(len(jobs), A.parallel)) as ex:
        docs = [d for d in ex.map(lambda j: shard(patch, level, j[0], j[1], tag), jobs) if d]
    return summarize(level, anchors, docs)


def measure_levels(levels, anchors):
    """All levels in one pool (no per-level barrier): returns results in level order."""
    patch = write_variant(anchors, 'm')
    jobs = [(l, c, i) for l in levels for c in CLASSES for i in range(A.shards)]
    with ThreadPoolExecutor(min(len(jobs), A.parallel)) as ex:
        out = list(ex.map(lambda j: (j[0], shard(patch, j[0], j[1], j[2], f'm{j[0]}')), jobs))
    return [summarize(l, anchors, [d for lv, d in out if lv == l and d]) for l in levels]


def summarize(level, anchors, docs):
    if not docs:
        raise SystemExit(f'No DUNGEONSIM output for L{level}; see logs in {WORK}')
    provs = {d['Provenance'] for d in docs}
    tot = {'Runs': 0, 'Dungeons': 0, 'Cleared': 0, 'Retreated': 0, 'Died': 0, 'Fights': 0, 'HpLost': 0.0, 'HeroActions': 0.0, 'LevelDrift': 0}
    per_class = {}
    for d in docs:
        for o in d['Outcomes']:
            if o['Class'] == 'all':
                continue
            pc = per_class.setdefault(o['Class'], {'Dungeons': 0, 'Cleared': 0, 'Died': 0})
            pc['Dungeons'] += o['Dungeons']; pc['Cleared'] += o['Cleared']; pc['Died'] += o['Died']
            for k in ('Runs', 'Dungeons', 'Cleared', 'Retreated', 'Died', 'Fights', 'LevelDrift'):
                tot[k] += o[k]
            tot['HpLost'] += o['HpLostPerFight'] * o['Fights']
            tot['HeroActions'] += o.get('HeroActionsPerFight', 0) * o['Fights']
    n = tot['Dungeons']
    res = {
        'level': level, 'anchors': anchors, 'damageMultiplier': round(damage_anchor_at(level, anchors), 4),
        'dungeons': n, 'runs': tot['Runs'],
        'clear': tot['Cleared'] / n if n else 0, 'clearCI': wilson(tot['Cleared'], n),
        'retreat': tot['Retreated'] / n if n else 0, 'retreatCI': wilson(tot['Retreated'], n),
        'death': tot['Died'] / n if n else 0, 'deathCI': wilson(tot['Died'], n),
        'hpLostPerFight': tot['HpLost'] / tot['Fights'] if tot['Fights'] else 0,
        'heroActionsPerFight': tot['HeroActions'] / tot['Fights'] if tot['Fights'] else 0,
        'levelDrift': tot['LevelDrift'],
        'perClassClear': {c: round(v['Cleared'] / v['Dungeons'], 3) if v['Dungeons'] else None for c, v in per_class.items()},
        'provenance': sorted(provs),
    }
    return res


def show(r):
    pct = lambda ci: f'[{ci[0] * 100:.0f}-{ci[1] * 100:.0f}]'
    print(f"L{r['level']:<3} dmg x{r['damageMultiplier']:<6} {r['dungeons']:4} dungeons  "
          f"clear {r['clear'] * 100:5.1f}% {pct(r['clearCI']):9}  retreat {r['retreat'] * 100:5.1f}% {pct(r['retreatCI']):9}  "
          f"death {r['death'] * 100:4.1f}% {pct(r['deathCI']):9}  HP lost/fight {r['hpLostPerFight'] * 100:4.1f}%  hero acts {r['heroActionsPerFight']:4.1f}  "
          f"by class {r['perClassClear']}", flush=True)
    if r['levelDrift']:
        print(f"     WARNING: {r['levelDrift']} hero(es) levelled inside a dungeon", flush=True)
    if len(r['provenance']) > 1:
        print(f"     WARNING: shards ran different code/data: {r['provenance']}", flush=True)


def main():
    copy_gamedata()
    print(f'Patch: {active_patch_path()}')
    overrides = {int(s.split('=')[0]): float(s.split('=')[1]) for s in A.anchor}
    results = []
    if A.solve:
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
    if A.json:
        json.dump(results, open(A.json, 'w', encoding='utf-8'), indent=2)
    shutil.rmtree(WORK, ignore_errors=True)


if __name__ == '__main__':
    main()
