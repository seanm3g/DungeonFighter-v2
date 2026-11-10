#!/usr/bin/env python3
"""Solve enemySystem.healthCurve / damageCurve anchors against the MEDIAN catalog build at each level.

Every BUILD_CATALOG ladder (GameData/TuningProfiles/build-ladders.json) is run in BUILDLAB at the ladder step a player
is expected to have reached by that level (STEP below), with strips trimmed to the level's real slot max
(--level-slots: lootSystem.comboSlotCapByLevel). Health pass: near-harmless enemies (damage x0.05) so hero turns measure
kill speed; each health anchor is scaled so the median build needs the level's turns target (balance-targets.json
heroTurnsAnchors: 2 at L1 rising to 11 at L11, then heroTurnsTarget).

Damage pass (--only damage, DEPRECATED): bisects a damage scale so the median build's SINGLE-FIGHT win rate hits the
WR table below. The damage curve is now solved for real dungeon clear (realClearAnchors) with Scripts/bo-solve.py, so
the default run is the health pass only, and --check reports win rate without judging it.

  python Scripts/median-curve-solve.py --check                                   (active patch, verify only)
  python Scripts/median-curve-solve.py --patch my-candidate --tools Code/bin/T1 --passes 1
  python Scripts/median-curve-solve.py --patch my-candidate --levels 35           (one health anchor)
"""
import json, subprocess, os, sys, argparse, statistics, math, tempfile
from concurrent.futures import ThreadPoolExecutor
ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
def _active_patch():
    try:
        return json.load(open(os.path.join(ROOT, 'GameData', 'PatchProfile.json'), encoding='utf-8-sig')).get('activeBalancePatch') or 'default'
    except Exception:
        return 'default'
ap = argparse.ArgumentParser()
ap.add_argument('--patch', default='', help='balance patch name (default: active patch in GameData/PatchProfile.json)'); ap.add_argument('--tools', default='Code/bin/BalanceTools')
ap.add_argument('--passes', type=int, default=1); ap.add_argument('--encounters', type=int, default=8)
ap.add_argument('--only', default='health', help='health (default) or damage (deprecated single-fight win-rate pass; use bo-solve.py)')
ap.add_argument('--check', action='store_true', help='report median build turns / win rate vs targets; change nothing')
ap.add_argument('--json', default='', help='--check: also write per-level medians to this file (balance-check.ps1 reads it)')
ap.add_argument('--levels', default='', help='comma list of levels to check / solve (default: every anchor level)')
ap.add_argument('--parallel', type=int, default=12, help='BUILDLAB processes at once')
ap.add_argument('--real-damage-turns', action='store_true', help='--check: measure turns at real enemy damage (pre-2026-11-09 behaviour; losses cut fights short)')
ARGS = ap.parse_args()
ARGS.patch = ARGS.patch or _active_patch()
DLL = os.path.join(ROOT, ARGS.tools, 'DF.dll')
PATCH = os.path.join(ROOT, 'GameData', 'Patches', 'Balance', ARGS.patch + '.json')
E = "Goblin,Rats,Wraith,Wolf,Bear,Lich,Fire Elemental,Zombie,Wight,Glass Shard Golem,Guardian Scarab,Vigil Alpha"
# Ladder step a typical player has by each anchor level (0 default, 1 class strip, 2 two-piece + convert,
# 3 full set, 4 weapon mods, 5 catalog skills, 6 core & rite). Strips are trimmed to the level's slot cap.
STEP = {1: 0, 3: 0, 5: 1, 7: 1, 9: 1, 10: 2, 11: 2, 20: 2, 35: 3, 50: 4, 75: 5, 100: 6}
# Health pass levels (2026-11-05, band curves): every level 1-10 is its own band (the early ramp, 3 -> 11 hero actions,
# moves inside a region), then one value per 5-level region band, solved at its midpoint.
HEALTH_LEVELS = list(range(1, 11)) + list(range(13, 99, 5))

def step_for(level):
    """Ladder step a player has by `level`: the STEP entry at or below it."""
    return STEP[max(l for l in STEP if l <= level)]
# Legacy single-fight win-rate targets: used only by the deprecated damage pass; --check prints them for reference.
WR = {10: 99.1, 20: 97.1, 35: 93.6, 50: 89.4, 75: 79.2, 100: 1.5}
TARGETS = json.load(open(os.path.join(ROOT, 'GameData', 'TuningProfiles', 'balance-targets.json'), encoding='utf-8'))

def turns_target(level):
    """Median-build hero turns wanted at a level: heroTurnsAnchors (early-game ramp, linear), else heroTurnsTarget."""
    pts = sorted((a['level'], a['turns']) for a in TARGETS.get('heroTurnsAnchors', []))
    if not pts: return float(TARGETS['heroTurnsTarget'])
    if level <= pts[0][0]: return float(pts[0][1])
    for (l0, t0), (l1, t1) in zip(pts, pts[1:]):
        if level <= l1: return t0 + (t1 - t0) * (level - l0) / (l1 - l0)
    return float(pts[-1][1])

def turns_tolerance(level):
    # heroTurnsTolerance is set for 11 turns; a 2-turn fight gets the same relative slack (at least half a turn).
    return max(0.5, float(TARGETS['heroTurnsTolerance']) * turns_target(level) / float(TARGETS['heroTurnsTarget']))
BUILDS = [b['id'] for b in json.load(open(os.path.join(ROOT, 'GameData', 'TuningProfiles', 'build-ladders.json'), encoding='utf-8'))['builds']]
TMP = tempfile.mkdtemp(prefix='mcs-')

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import balancepatch as bp
def load(): return bp.resolve(PATCH)
def save(d): bp.write_patch(PATCH, d, base=bp.base_name(PATCH))   # an overlay patch stays an overlay

def one(build, level, sets, tag):
    out = os.path.join(TMP, f'{tag}-{build}-L{level}.json')
    args = ['dotnet', DLL, 'BUILDLAB', '--patch', ARGS.patch, '--builds', build, '--level', str(level), '--max-step', str(step_for(level)),
            '--level-slots', '--encounters', str(ARGS.encounters), '--enemies', E, '--seed', '1', '--json', out]
    for s in sets: args += ['--set', s]
    subprocess.run(args, cwd=ROOT, stdout=subprocess.DEVNULL, stderr=subprocess.DEVNULL)
    try:
        rows = [r for r in json.load(open(out, encoding='utf-8')) if r['Step'] == step_for(level)]
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
    d = load(); curve = bp.get_curve(d, 'health'); curve['enabled'] = True
    for l in chosen(HEALTH_LEVELS):
        rows = median_at(l, ['enemySystem.damageCurve.baseMultiplier=0.05'], f'h{l}')
        if not rows: print(f'health L{l}: no data', flush=True); continue
        turns = statistics.median(r['HeroTurns'] for r in rows)
        old = bp.band_value(curve, l); new = round(old * (turns_target(l) / max(0.5, turns)) ** 0.9, 4)
        lo, hi = (l, l) if l <= 10 else bp.region_band(l)
        bp.set_range(curve, lo, hi, new)
        print(f'health L{lo}-{hi}: median build {turns:.1f} turns (target {turns_target(l):.1f}, {len(rows)} builds)  {old:.4f} -> {new}', flush=True)
    save(d)

def damage_pass():
    raise SystemExit("The single-fight win-rate damage pass was removed 2026-11-05 (curves are bands now): solve clear rate with Scripts/bo-solve.py.")
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
    # Turns are kill speed, measured like the health pass (near-harmless enemies): at real damage a lost fight ends when
    # the hero dies, so at lethal levels "turns" read death speed (2026-11-09: L63-73 read 4.7 turns at 8-23% wins while
    # the bot needed 11). Win rate still comes from a real-damage run (reference only).
    jobs = [(l, b, k) for l in levels for b in BUILDS for k in (('turns', 'win') if not ARGS.real_damage_turns else ('win',))]
    harmless = ['enemySystem.damageCurve.baseMultiplier=0.05']
    with ThreadPoolExecutor(ARGS.parallel) as ex:
        res = list(ex.map(lambda j: (j[0], j[1], j[2], one(j[1], j[0], harmless if j[2] == 'turns' else [], f'c{j[2]}{j[0]}')), jobs))
    win = {(l, b): r for l, b, k, r in res if k == 'win' and r}
    kill = {(l, b): r for l, b, k, r in res if k == ('win' if ARGS.real_damage_turns else 'turns') and r}
    results = [(l, (kill.get((l, b)), win.get((l, b)))) for l in levels for b in BUILDS]
    print(f"{'level':6}{'step':>5}{'turns':>8}{'target':>8}{'win%':>8}{'target':>8}  result")
    ok = 0; n = 0; out = []
    for l in levels:
        rows = [k for lv, (k, w_) in results if lv == l and k]
        wins = [w_ for lv, (k, w_) in results if lv == l and w_]
        if not rows: print(f'L{l}: no data'); continue
        t = statistics.median(r['HeroTurns'] for r in rows); w = statistics.median(r['WinRate'] for r in wins) * 100 if wins else float('nan')
        good_t = abs(t - turns_target(l)) <= turns_tolerance(l) if l < 75 else True   # lethal levels: short losses, turns not a target
        good_w = True   # single-fight win rate is reference only (clear rate is judged by dungeon-sim / bo-solve)
        n += 1; ok += good_t and good_w
        # Per enemy (median over builds): one slow enemy, e.g. a pack whose bodies each need two swings, hides in the mean.
        per = {}
        for name in E.split(','):
            pe = [r['PerEnemy'][name] for r in rows if name in (r.get('PerEnemy') or {})]
            pw = [r['PerEnemy'][name] for r in wins if name in (r.get('PerEnemy') or {})]
            if pe:
                per[name] = {'heroTurns': round(statistics.median(x['HeroTurns'] for x in pe), 2),
                             'heroTurnsP90': round(statistics.median(x['HeroTurnsP90'] for x in pe), 2),
                             'winRate': round(statistics.median(x['WinRate'] for x in pw) * 100, 2) if pw else None}
        out.append({'level': l, 'step': step_for(l), 'builds': len(rows), 'heroTurns': round(t, 2), 'heroTurnsTarget': round(turns_target(l), 2), 'heroTurnsTolerance': round(turns_tolerance(l), 2), 'winRate': round(w, 2), 'perEnemy': per})
        spread = ''
        if per:
            slow = max(per, key=lambda k: per[k]['heroTurns']); fast = min(per, key=lambda k: per[k]['heroTurns'])
            spread = f"  slowest {slow} {per[slow]['heroTurns']:.1f} (p90 {per[slow]['heroTurnsP90']:.0f}), fastest {fast} {per[fast]['heroTurns']:.1f}"
        print(f"L{l:<5}{step_for(l):>5}{t:8.1f}{turns_target(l):8.1f}{w:8.1f}{WR.get(l, float('nan')):8.1f}  {'PASS' if good_t and good_w else 'FAIL'}{spread}")
    print(f'{ok} of {n} levels on target (median of {len(BUILDS)} catalog builds)')
    if ARGS.json:
        json.dump(out, open(ARGS.json, 'w', encoding='utf-8'), indent=1)

if ARGS.check:
    check(); sys.exit(0)
for it in range(ARGS.passes):
    print('== pass', it, flush=True)
    if ARGS.only in ('', 'health'): health_pass()
    if ARGS.only == 'damage':
        print('WARNING: the damage pass fits a single-fight win-rate table (deprecated); use Scripts/bo-solve.py for clear rate.', flush=True)
        damage_pass()
