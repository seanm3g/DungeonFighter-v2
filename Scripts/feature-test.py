"""Enemy / room affix feature test: paired DUNGEONSIM runs with one feature switched off at a time (--features-off), same
seed, then GameData/TuningRuns/feature-test-<name>.json for the balance dashboard's "Enemy & room features" tab.

  python Scripts/feature-test.py --seed 3302                     # all 10 variants on the Spark, then the JSON
  python Scripts/feature-test.py --build-only --keep-root <dir>  # rebuild the JSON from kept shards

Variants: on (everything), off (all), then one each: region, dungeon, prefix, room, terrain, wander, demon, kits.
Each sim carries --batch feature-test-s<seed>:I/N, so the dashboard shows an ETA for the whole batch.
"""
import argparse, collections, glob, json, os, statistics, subprocess, sys, time

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
ap = argparse.ArgumentParser(description=__doc__, formatter_class=argparse.RawDescriptionHelpFormatter)
ap.add_argument('--seed', type=int, default=3301)
ap.add_argument('--levels', default='8,13,18,23,28,33,38,48,58,68')
ap.add_argument('--shards', type=int, default=16)
ap.add_argument('--spark', default='18', help='Spark slots (passed to dungeon-sim)')
ap.add_argument('--parallel', type=int, default=0, help='local slots (0 = Spark only)')
ap.add_argument('--patch', default='', help='balance patch (default: active)')
ap.add_argument('--variants', default='on,off,region,dungeon,prefix,room,terrain,wander,demon,kits')
ap.add_argument('--keep-root', default='', help='folder for the kept shard JSON (default GameData/TuningRuns/raw/feature-test-s<seed>)')
ap.add_argument('--out', default='', help='result JSON (default GameData/TuningRuns/feature-test-s<seed>.json)')
ap.add_argument('--note', default='')
ap.add_argument('--build-only', action='store_true')
A = ap.parse_args()
root = A.keep_root or os.path.join(ROOT, 'GameData', 'TuningRuns', 'raw', f'feature-test-s{A.seed}')
out = A.out or os.path.join(ROOT, 'GameData', 'TuningRuns', f'feature-test-s{A.seed}.json')
note = A.note or (f'Paired DUNGEONSIM, --region-mix (all 12 dungeons), seed {A.seed}, {A.shards} shards per class, levels {A.levels}'
                  + (f', patch {A.patch}' if A.patch else ', active patch') + '. Each "no X" row switches one feature off (--features-off X).')
VARIANT_LIST = [v for v in A.variants.split(',') if v]
if not A.build_only:
    os.makedirs(root, exist_ok=True)
    t0 = time.time()
    for i, v in enumerate(VARIANT_LIST, 1):
        off = {'on': '', 'off': 'all'}.get(v, v)
        cmd = [sys.executable, os.path.join(ROOT, 'Scripts', 'dungeon-sim.py'), '--levels', A.levels, '--shards', str(A.shards),
               '--parallel', str(A.parallel), '--spark', A.spark, '--seed', str(A.seed), '--region-mix',
               '--keep', os.path.join(root, v), '--json', os.path.join(root, v + '.json'),
               '--batch', f'feature-test-s{A.seed}:{i}/{len(VARIANT_LIST)}']
        if off:
            cmd += ['--features-off', off]
        if A.patch:
            cmd += ['--patch', A.patch]
        with open(os.path.join(root, v + '.log'), 'w', encoding='utf-8') as log:
            rc = subprocess.run(cmd, cwd=ROOT, stdout=log, stderr=subprocess.STDOUT).returncode
        print(f'{v}: exit {rc} at {time.time() - t0:.0f}s', flush=True)
VARIANTS = VARIANT_LIST
CLS = ['Barbarian', 'Warrior', 'Rogue', 'Wizard']
E = {e['name']: e for e in json.load(open(os.path.join(ROOT, 'GameData', 'Enemies.json'), encoding='utf-8'))}


def shards(v):
    return [json.load(open(f, encoding='utf-8')) for f in glob.glob(os.path.join(root, v, '*.json'))]


levels = collections.defaultdict(dict)
for v in VARIANTS:
    data = shards(v)
    if not data:
        continue
    acc = collections.defaultdict(lambda: collections.defaultdict(collections.Counter))
    for d in data:
        for o in d['Outcomes']:
            if o['Class'] not in CLS:
                continue
            a = acc[d['Level']][o['Class']]
            a['dungeons'] += o['Dungeons']; a['cleared'] += o['Cleared']; a['died'] += o['Died']
            a['fights'] += o['Fights']; a['acts'] += o['HeroActionsPerFight'] * o['Fights']
    for L, by in acc.items():
        row = {c: by[c]['cleared'] / by[c]['dungeons'] for c in CLS if by[c]['dungeons']}
        tot = collections.Counter()
        for c in by.values():
            tot.update(c)
        row['all'] = tot['cleared'] / max(1, tot['dungeons'])
        row['death'] = tot['died'] / max(1, tot['dungeons'])
        row['acts'] = tot['acts'] / max(1, tot['fights'])
        row['dungeons'] = tot['dungeons']
        levels[str(L)][v] = row

fights = [fi for d in shards('on') for fi in d['Fights']]
n = max(1, len(fights))
boss = [f for f in fights if f.get('BossFight')]
nonboss = [f for f in fights if not f.get('BossFight')]
occ = {
    'fights': len(fights),
    'wandering enemies': f"{100 * sum(f.get('EnemyWanderer', False) for f in fights) / n:.1f}%",
    'wandering rooms': f"{100 * sum(f.get('RoomWanderer', False) for f in fights) / n:.1f}%",
    'room affix': f"{100 * sum(bool(f.get('RoomAffix')) for f in fights) / n:.1f}%",
    'prefixed enemies': f"{100 * sum(bool(f.get('EnemyPrefix')) for f in nonboss) / max(1, len(nonboss)):.1f}%",
    'Demon fights': len(boss),
    'Demon reached phase 2': f"{100 * sum(f.get('DemonPhase', False) for f in boss) / max(1, len(boss)):.1f}%",
    'dungeons played': len({E.get(f['Enemy'], {}).get('biome') for f in fights if not f.get('EnemyWanderer')} - {None, 'n/a'}),
}


def cost(rows, key):
    by_level = collections.defaultdict(list)
    for f in rows:
        by_level[f['EnemyLevel']].append(f.get('HpLost', 0))
    mean = {L: statistics.mean(v) for L, v in by_level.items()}
    g = collections.defaultdict(list)
    for f in rows:
        k = key(f)
        if k:
            g[k].append(f)
    res = [{'name': k, 'n': len(v), 'hp': 100 * statistics.mean(f.get('HpLost', 0) - mean[f['EnemyLevel']] for f in v),
            'death': 100 * statistics.mean(f.get('HeroDied', False) for f in v), 'acts': statistics.mean(f.get('HeroActions', 0) for f in v)}
           for k, v in g.items()]
    return sorted(res, key=lambda r: -r['hp'])


def biome_stats(fs):
    g = collections.defaultdict(list)
    for f in fs:
        if f.get('EnemyWanderer') or f.get('RoomWanderer') or f.get('BossFight'):
            continue
        b = E.get(f['Enemy'], {}).get('biome')
        if b and b != 'n/a':
            g[b].append(f)
    return {b: (len(v), 100 * statistics.mean(x.get('HpLost', 0) for x in v), 100 * statistics.mean(x.get('HeroDied', False) for x in v),
                statistics.mean(x.get('HeroActions', 0) for x in v)) for b, v in g.items()}


dungeons = []
off_fights = [fi for d in shards('off') for fi in d['Fights']]
if off_fights:
    a, b = biome_stats(fights), biome_stats(off_fights)
    for k in a:
        if k in b:
            dungeons.append({'name': k, 'n': a[k][0], 'hpOn': a[k][1], 'hpOff': b[k][1], 'deathOn': a[k][2], 'deathOff': b[k][2],
                             'actsOn': a[k][3], 'actsOff': b[k][3]})
    dungeons.sort(key=lambda r: -(r['hpOn'] - r['hpOff']))

result = {
    'note': note,
    'levels': dict(sorted(levels.items(), key=lambda kv: int(kv[0]))),
    'occurrence': occ,
    'prefixCost': cost(nonboss, lambda f: f.get('EnemyPrefix') or 'none'),
    'roomCost': cost(fights, lambda f: f.get('RoomAffix') or 'none'),
    'demonCost': cost(boss, lambda f: 'reached phase 2' if f.get('DemonPhase') else 'died before phase 2'),
    'dungeons': dungeons,
}
json.dump(result, open(out, 'w', encoding='utf-8'), indent=1)
print('wrote', out, 'variants', sorted({v for lv in levels.values() for v in lv}))
