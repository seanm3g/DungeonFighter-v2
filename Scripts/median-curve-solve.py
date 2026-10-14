#!/usr/bin/env python3
"""Solve enemySystem.healthCurve / damageCurve anchors against the MEDIAN catalog build at each level.

Every BUILD_CATALOG ladder (GameData/TuningProfiles/build-ladders.json) is run in BUILDLAB at the ladder step a player
is expected to have reached by that level (STEP below), with strips trimmed to the level's real slot max
(--level-slots: lootSystem.comboSlotCapByLevel). Health pass: near-harmless enemies (damage x0.05) so hero turns measure
kill speed; each health anchor is scaled so the median build needs ~11 hero turns. Damage pass: per level, bisect a
damage scale so the median build's win rate hits the target, then fold it into the anchor.

  python Scripts/median-curve-solve.py --patch overnight-v2 --tools Code/bin/T1 --passes 1
  python Scripts/median-curve-solve.py --patch overnight-v2 --tools Code/bin/T1 --check   (verify only)
  python Scripts/median-curve-solve.py --patch overnight-v2 --only health --levels 35      (one health anchor)
"""
import json, subprocess, os, sys, argparse, statistics, math, tempfile
from concurrent.futures import ThreadPoolExecutor
ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
ap = argparse.ArgumentParser()
ap.add_argument('--patch', default='overnight-v2'); ap.add_argument('--tools', default='Code/bin/BalanceTools')
ap.add_argument('--passes', type=int, default=1); ap.add_argument('--encounters', type=int, default=8)
ap.add_argument('--only', default='', help='health or damage')
ap.add_argument('--check', action='store_true', help='report median build turns / win rate vs targets; change nothing')
ap.add_argument('--json', default='', help='--check: also write per-level medians to this file (balance-check.ps1 reads it)')
ap.add_argument('--levels', default='', help='comma list of levels to check / solve (default: every anchor level)')
ap.add_argument('--parallel', type=int, default=12, help='BUILDLAB processes at once')
ARGS = ap.parse_args()
DLL = os.path.join(ROOT, ARGS.tools, 'DF.dll')
PATCH = os.path.join(ROOT, 'GameData', 'Patches', 'Balance', ARGS.patch + '.json')
E = "Goblin,Rats,Wraith,Wolf,Bear,Lich,Fire Elemental,Zombie,Wight,Glass Shard Golem,Guardian Scarab,Vigil Alpha"
# Ladder step a typical player has by each anchor level (0 default, 1 class strip, 2 two-piece + convert,
# 3 full set, 4 weapon mods, 5 catalog skills, 6 core & rite). Strips are trimmed to the level's slot cap.
STEP = {1: 0, 5: 1, 10: 2, 20: 2, 35: 3, 50: 4, 75: 5, 100: 6}
WR = {10: 99.1, 20: 97.1, 35: 93.6, 50: 89.4, 75: 79.2, 100: 1.5}
TURNS = 11.0
BUILDS = [b['id'] for b in json.load(open(os.path.join(ROOT, 'GameData', 'TuningProfiles', 'build-ladders.json'), encoding='utf-8'))['builds']]
TMP = tempfile.mkdtemp(prefix='mcs-')

def load(): return json.load(open(PATCH, encoding='utf-8'))
def save(d): json.dump(d, open(PATCH, 'w', encoding='utf-8'), indent=2, ensure_ascii=False)

def one(build, level, sets, tag):
    out = os.path.join(TMP, f'{tag}-{build}-L{level}.json')
    args = ['dotnet', DLL, 'BUILDLAB', '--patch', ARGS.patch, '--builds', build, '--level', str(level), '--max-step', str(STEP[level]),
            '--level-slots', '--encounters', str(ARGS.encounters), '--enemies', E, '--seed', '1', '--json', out]
    for s in sets: args += ['--set', s]
    subprocess.run(args, cwd=ROOT, stdout=subprocess.DEVNULL, stderr=subprocess.DEVNULL)
    try:
        rows = [r for r in json.load(open(out, encoding='utf-8')) if r['Step'] == STEP[level]]
        return rows[0] if rows else None
    except Exception:
        return None

def median_at(level, sets, tag):
    with ThreadPoolExecutor(ARGS.parallel) as ex:
        rows = [r for r in ex.map(lambda b: one(b, level, sets, tag), BUILDS) if r]
    return rows

def chosen(levels):
    pick = {int(x) for x in ARGS.levels.split(',') if x}
    return [l for l in sorted(levels) if not pick or l in pick]

def health_pass():
    d = load(); anchors = {a['level']: a for a in d['enemySystem']['healthCurve']['anchors']}
    for l in chosen(STEP):
        rows = median_at(l, ['enemySystem.damageCurve.baseMultiplier=0.05'], f'h{l}')
        if not rows: print(f'health L{l}: no data', flush=True); continue
        turns = statistics.median(r['HeroTurns'] for r in rows)
        old = anchors[l]['multiplier']; new = round(old * (TURNS / max(0.5, turns)) ** 0.9, 3)
        anchors[l]['multiplier'] = new
        print(f'health L{l}: median build {turns:.1f} turns ({len(rows)} builds)  {old} -> {new}', flush=True)
    save(d)

def damage_pass():
    d = load(); anchors = {a['level']: a for a in d['enemySystem']['damageCurve']['anchors']}
    for l in chosen(WR):
        target = WR[l] / 100.0
        def wr(f):
            rows = median_at(l, [f'enemySystem.damageCurve.baseMultiplier={f:.4f}'], f'd{l}-{f:.3f}')
            return statistics.median(r['WinRate'] for r in rows) if rows else None
        lo, hi = 0.1, 10.0
        w = wr(1.0)
        if w is None: print(f'damage L{l}: no data', flush=True); continue
        f = 1.0
        for _ in range(7):
            if abs(w - target) < 0.01: break
            if w > target: lo = f
            else: hi = f
            f = math.sqrt(lo * hi)
            w = wr(f)
        old = anchors[l]['multiplier']; anchors[l]['multiplier'] = round(old * f, 3)
        print(f'damage L{l}: x{f:.3f} {old} -> {anchors[l]["multiplier"]}  (median WR {w*100:.1f}% target {WR[l]}%)', flush=True)
    save(d)

def check():
    # Every (level, build) job in one pool: the levels are independent, so this keeps all workers busy.
    levels = [int(x) for x in ARGS.levels.split(',') if x] or sorted(STEP)
    jobs = [(l, b) for l in levels for b in BUILDS]
    with ThreadPoolExecutor(ARGS.parallel) as ex:
        results = list(ex.map(lambda j: (j[0], one(j[1], j[0], [], f'c{j[0]}')), jobs))
    print(f"{'level':6}{'step':>5}{'turns':>8}{'target':>8}{'win%':>8}{'target':>8}  result")
    ok = 0; n = 0; out = []
    for l in levels:
        rows = [r for lv, r in results if lv == l and r]
        if not rows: print(f'L{l}: no data'); continue
        t = statistics.median(r['HeroTurns'] for r in rows); w = statistics.median(r['WinRate'] for r in rows) * 100
        good_t = abs(t - TURNS) <= 1.5 if l < 75 else True   # lethal levels: short losses, turns not a target
        good_w = abs(w - WR[l]) <= 4 if l in WR else True
        n += 1; ok += good_t and good_w
        # Per enemy (median over builds): one slow enemy, e.g. a pack whose bodies each need two swings, hides in the mean.
        per = {}
        for name in E.split(','):
            pe = [r['PerEnemy'][name] for r in rows if name in (r.get('PerEnemy') or {})]
            if pe:
                per[name] = {'heroTurns': round(statistics.median(x['HeroTurns'] for x in pe), 2),
                             'heroTurnsP90': round(statistics.median(x['HeroTurnsP90'] for x in pe), 2),
                             'winRate': round(statistics.median(x['WinRate'] for x in pe) * 100, 2)}
        out.append({'level': l, 'step': STEP[l], 'builds': len(rows), 'heroTurns': round(t, 2), 'winRate': round(w, 2), 'perEnemy': per})
        spread = ''
        if per:
            slow = max(per, key=lambda k: per[k]['heroTurns']); fast = min(per, key=lambda k: per[k]['heroTurns'])
            spread = f"  slowest {slow} {per[slow]['heroTurns']:.1f} (p90 {per[slow]['heroTurnsP90']:.0f}), fastest {fast} {per[fast]['heroTurns']:.1f}"
        print(f"L{l:<5}{STEP[l]:>5}{t:8.1f}{TURNS:8.0f}{w:8.1f}{WR.get(l, float('nan')):8.1f}  {'PASS' if good_t and good_w else 'FAIL'}{spread}")
    print(f'{ok} of {n} levels on target (median of {len(BUILDS)} catalog builds)')
    if ARGS.json:
        json.dump(out, open(ARGS.json, 'w', encoding='utf-8'), indent=1)

if ARGS.check:
    check(); sys.exit(0)
for it in range(ARGS.passes):
    print('== pass', it, flush=True)
    if ARGS.only != 'damage': health_pass()
    if ARGS.only != 'health': damage_pass()
