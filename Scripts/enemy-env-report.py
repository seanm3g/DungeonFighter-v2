#!/usr/bin/env python3
"""Enemy and environment balance layers: one full-coverage DUNGEONSIM (every band midpoint, all three dungeons of each
band, every feature on), read per fight, into GameData/TuningRuns/enemy-env-report-<patch>-s<seed>.json for the balance
dashboard's "Enemies" and "Environments" tabs.

  python Scripts/enemy-env-report.py --patch affix-v44e --seed 16001       # run the sim, then the report
  python Scripts/enemy-env-report.py --from-keep <dir> --patch affix-v44e   # report from kept shard JSON

Enemies: every enemy's HP cost per fight against the level's average fight, its hero-death rate against the level's,
hero actions to kill it; outliers past the limits are flagged. Plus prefix, region / dungeon affix and Demon tables.
Environments: each dungeon's clear rate against the other dungeons of its band (spread within a band), plus room affix,
terrain and wandering room tables.
"""
import argparse, collections, glob, json, os, statistics, subprocess, sys
from datetime import datetime

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
RUNS = os.path.join(ROOT, 'GameData', 'TuningRuns')
MIDPOINTS = '3,8,13,18,23,28,33,38,43,48,53,58,63,68,73'

ap = argparse.ArgumentParser(description=__doc__, formatter_class=argparse.RawDescriptionHelpFormatter)
ap.add_argument('--patch', default='', help='balance patch (default: active)')
ap.add_argument('--seed', type=int, default=16001)
ap.add_argument('--levels', default=MIDPOINTS)
ap.add_argument('--shards', type=int, default=16)
ap.add_argument('--spark', default='auto')
ap.add_argument('--parallel', type=int, default=-1)
ap.add_argument('--tools', default='Code/bin/BalanceTools')
ap.add_argument('--from-keep', default='', help='skip the sim: read kept shard JSON from this folder')
ap.add_argument('--out', default='')
# Limits (pts of max HP per fight unless noted). Shown on the dashboard next to each table.
ap.add_argument('--enemy-hp-limit', type=float, default=8.0, help='enemy flagged when its HP cost is this far from the level mean')
ap.add_argument('--enemy-death-ratio', type=float, default=2.0, help='deadly when hero deaths per fight are this many times the level rate')
ap.add_argument('--min-fights', type=int, default=20)
ap.add_argument('--dungeon-gap-limit', type=float, default=8.0, help='dungeon flagged when its clear is this many pts from its band mean')
ap.add_argument('--affix-hp-limit', type=float, default=4.0, help='prefix / affix / room flagged past this HP cost')
A = ap.parse_args()

tag = f"{A.patch or 'active'}-s{A.seed}"
keep = A.from_keep or os.path.join(RUNS, 'raw', f'enemy-env-{tag}')
out = A.out or os.path.join(RUNS, f'enemy-env-report-{tag}.json')

if not A.from_keep:
    cmd = [sys.executable, os.path.join(ROOT, 'Scripts', 'dungeon-sim.py'), '--levels', A.levels, '--shards', str(A.shards),
           '--seed', str(A.seed), '--spark', A.spark, '--tools', A.tools, '--keep', keep, '--json', keep + '.json',
           '--batch', f'enemy-env-report-{tag}:1/1']
    if A.patch:
        cmd += ['--patch', A.patch]
    if A.parallel >= 0:
        cmd += ['--parallel', str(A.parallel)]
    print('RUN', ' '.join(cmd[1:]), flush=True)
    rc = subprocess.run(cmd, cwd=ROOT).returncode
    if rc:
        sys.exit(f'dungeon-sim failed (exit {rc})')

AFFIX = json.load(open(os.path.join(ROOT, 'GameData', 'EnemyAffixes.json'), encoding='utf-8'))
REGION_OF = {d['theme']: d['region'] for d in AFFIX['dungeons']}
ENEMIES = {e['name']: e for e in json.load(open(os.path.join(ROOT, 'GameData', 'Enemies.json'), encoding='utf-8'))}

docs = [json.load(open(f, encoding='utf-8')) for f in glob.glob(os.path.join(keep, '*.json'))]
if not docs:
    sys.exit(f'no shard JSON in {keep}')
fights = [dict(f, _level=d['Level']) for d in docs for f in d['Fights']]
levels = sorted({d['Level'] for d in docs})

# ---- per-level baselines: mean HP lost and hero deaths per fight
def baseline(rows):
    out = {}
    for L in levels:
        fl = [f for f in rows if f['_level'] == L]
        if fl:
            out[L] = (statistics.mean(f.get('HpLost', 0) for f in fl), statistics.mean(1.0 if f.get('HeroDied') else 0.0 for f in fl))
    return out


base = baseline(fights)
# Bosses (the Demons) are priced against other boss fights at the same level, not against every fight.
boss_base = baseline([f for f in fights if f.get('BossFight')])
# Regular enemies, prefixes and enemy affixes are priced against regular fights at the level.
reg_base = baseline([f for f in fights if not f.get('BossFight')])


def cost_rows(rows, key, min_n=1, base=base):
    """name -> fights, HP cost vs level mean (pts), hero deaths per fight (%), death ratio vs level, hero / enemy actions."""
    g = collections.defaultdict(list)
    for f in rows:
        for k in key(f):
            if k:
                g[k].append(f)
    res = []
    for k, v in g.items():
        if len(v) < min_n:
            continue
        hp = 100 * statistics.mean(f.get('HpLost', 0) - base[f['_level']][0] for f in v)
        death = statistics.mean(1.0 if f.get('HeroDied') else 0.0 for f in v)
        lvl_death = statistics.mean(base[f['_level']][1] for f in v)
        res.append({'name': k, 'n': len(v), 'hp': round(hp, 2), 'death': round(100 * death, 2),
                    'deathRatio': round(death / lvl_death, 2) if lvl_death > 0 else None,
                    'heroActs': round(statistics.mean(f.get('HeroActions', 0) for f in v), 2),
                    'enemyActs': round(statistics.mean(f.get('EnemyActions', 0) for f in v), 2)})
    return sorted(res, key=lambda r: -r['hp'])


def flag(r, hp_limit):
    if r['hp'] > hp_limit or (r['deathRatio'] or 0) >= A.enemy_death_ratio and r['death'] >= 5:
        return 'deadly'
    if r['hp'] < -hp_limit:
        return 'soft'
    return 'ok'


# ---- enemies (home fights and wanderers alike; bosses priced apart)
regular = [f for f in fights if not f.get('BossFight')]
enemies = cost_rows(regular, lambda f: [f['Enemy']], A.min_fights, base=reg_base)
for r in enemies:
    e = ENEMIES.get(r['name'], {})
    r.update(home=e.get('biome') if e.get('biome') not in (None, 'n/a') else 'General', archetype=e.get('archetype'),
             rarity=e.get('rarity'), kit=e.get('kit') or e.get('actions'), flag=flag(r, A.enemy_hp_limit))
bosses = cost_rows([f for f in fights if f.get('BossFight')], lambda f: [f['Enemy']], max(5, A.min_fights // 4), base=boss_base)
for r in bosses:
    r['flag'] = flag(r, 2 * A.enemy_hp_limit)


def base_affix(name):
    parts = name.rsplit(' ', 1)
    return parts[0] if len(parts) == 2 and parts[1] in ('I', 'II', 'III', 'IV', 'V', 'VI', 'VII') else name


region_names = {f['name'] for f in AFFIX['regionFamilies']}
dungeon_names = {d['affix']['name'] for d in AFFIX['dungeons'] if d.get('affix')}
region_aff = cost_rows(regular, lambda f: [a for a in (f.get('EnemyAffixes') or []) if base_affix(a) in region_names], base=reg_base)
dungeon_aff = cost_rows(regular, lambda f: [a for a in (f.get('EnemyAffixes') or []) if a in dungeon_names], base=reg_base)
prefixes = cost_rows(regular, lambda f: [f.get('EnemyPrefix') or 'none'], base=reg_base)
for r in prefixes + dungeon_aff:
    r['flag'] = flag(r, A.affix_hp_limit)
demon = cost_rows([f for f in fights if f.get('BossFight')], lambda f: ['reached phase 2' if f.get('DemonPhase') else 'died before phase 2'], base=boss_base)

# ---- environments
rooms = cost_rows(fights, lambda f: [f.get('RoomAffix') or 'none'])
for r in rooms:
    r['flag'] = flag(r, A.affix_hp_limit)
terrain = cost_rows(fights, lambda f: (f.get('RoomTerrain') or ['none']))
wander = cost_rows(fights, lambda f: ['wandering room' if f.get('RoomWanderer') else 'home room',
                                     'wandering enemy' if f.get('EnemyWanderer') else 'home enemy'])

# Per dungeon clear rate by level from the shards' DungeonOutcomes.
dun_lv = collections.defaultdict(lambda: collections.defaultdict(lambda: [0, 0]))
for d in docs:
    for o in d.get('DungeonOutcomes') or []:
        if o['Theme'] in REGION_OF:
            c = dun_lv[d['Level']][o['Theme']]
            c[0] += o['Cleared']; c[1] += o['Dungeons']
level_rows = {}
gaps = collections.defaultdict(list)
for L in levels:
    cells = {t: v[0] / v[1] for t, v in dun_lv[L].items() if v[1]}
    if not cells:
        continue
    n = {t: dun_lv[L][t][1] for t in cells}
    band = sum(cells[t] * n[t] for t in cells) / sum(n.values())
    level_rows[str(L)] = {'band': round(100 * band, 1), 'spread': round(100 * (max(cells.values()) - min(cells.values())), 1),
                          'dungeons': {t: {'clear': round(100 * c, 1), 'n': n[t]} for t, c in sorted(cells.items())}}
    for t, c in cells.items():
        gaps[t].append((100 * (c - band), n[t]))
dungeons = []
for t, g in gaps.items():
    w = sum(n for _, n in g)
    gap = sum(x * n for x, n in g) / w
    worst = max(g, key=lambda x: abs(x[0]))[0]
    dungeons.append({'name': t, 'region': REGION_OF[t], 'gap': round(gap, 1), 'worstGap': round(worst, 1), 'dungeons': w,
                     'flag': 'hard' if gap < -A.dungeon_gap_limit else 'easy' if gap > A.dungeon_gap_limit else 'ok'})
dungeons.sort(key=lambda r: r['gap'])

spreads = [r['spread'] for r in level_rows.values()]
summary = {
    'fights': len(fights), 'levels': levels,
    'enemiesPriced': len(enemies), 'enemiesDeadly': sum(r['flag'] == 'deadly' for r in enemies),
    'enemiesSoft': sum(r['flag'] == 'soft' for r in enemies), 'bossesDeadly': sum(r['flag'] == 'deadly' for r in bosses),
    'prefixOutliers': sum(r['flag'] != 'ok' and r['name'] != 'none' for r in prefixes),
    'dungeonAffixOutliers': sum(r['flag'] != 'ok' for r in dungeon_aff),
    'meanDungeonSpread': round(statistics.mean(spreads), 1) if spreads else None,
    'dungeonsOff': sum(r['flag'] != 'ok' for r in dungeons),
    'roomAffixOutliers': sum(r['flag'] != 'ok' and r['name'] != 'none' for r in rooms),
    'demonPhaseRate': next((round(100 * r['n'] / max(1, sum(x['n'] for x in demon)), 1) for r in demon if r['name'] == 'reached phase 2'), None),
}
report = {
    'note': f"Full-coverage DUNGEONSIM (all three dungeons per band, every feature on), patch {A.patch or 'active'}, seed {A.seed}, "
            f"{A.shards} shards per class, levels {A.levels}. Built {datetime.now():%Y-%m-%d %H:%M}.",
    'limits': {'enemyHp': A.enemy_hp_limit, 'enemyDeathRatio': A.enemy_death_ratio, 'minFights': A.min_fights,
               'dungeonGap': A.dungeon_gap_limit, 'affixHp': A.affix_hp_limit},
    'summary': summary, 'enemies': enemies, 'bosses': bosses, 'regionAffixes': region_aff, 'dungeonAffixes': dungeon_aff,
    'prefixes': prefixes, 'demon': demon, 'levels': level_rows, 'dungeons': dungeons, 'rooms': rooms, 'terrain': terrain,
    'wander': wander,
}
json.dump(report, open(out, 'w', encoding='utf-8'), indent=1)
print(f'wrote {out}')
print('summary', json.dumps(summary))
