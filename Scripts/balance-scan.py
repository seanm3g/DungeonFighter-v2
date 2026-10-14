#!/usr/bin/env python3
"""Quick balance scan (~3 min on 24 cores): find the problem areas, then spend sim time only there.

Tier 1 of scan -> focus -> gate (Documentation/BALANCE_PROCESS.md). One pool runs, in parallel:
  - the median catalog build grid (BUILDLAB, same jobs as median-curve-solve.py --check) at every matrix level,
  - DUNGEONSIM with one hero per class and several dungeons at a few levels (HP carried, retreat rule, smart bot).
Then ranks findings against GameData/TuningProfiles/balance-targets.json, each with a focus command to run next:
  median turns per level / per enemy, real dungeon clear per level, smart bot vs median build kill speed (is the
  median build's stepping stone what real play hands out?), per-enemy turns and HP cost in real dungeons, class spread.

Small samples by design, so only large misses show. A hero's dungeons share its gear and skills, so the real sample is
heroes, not dungeons: clear intervals come from per-hero clear rates (never narrower than the dungeon-count interval),
and per-enemy / class rows need several heroes before they read FAIL. Rows whose interval still reaches the target
read LIKELY. Confirm with the focus command, gate with balance-check.ps1.

  python Scripts/balance-scan.py                              (active patch, builds tools into Code/bin/ScanTools)
  python Scripts/balance-scan.py --patch my-variant --skip-build --compare GameData/TuningRuns/balance-scan-<...>.json
  python Scripts/balance-scan.py --dungeon-levels 20 --dungeons 12 --median-levels 20     (one level, a bit deeper)
"""
import argparse, ast, json, math, os, shutil, statistics, subprocess, sys, tempfile, time
from concurrent.futures import ThreadPoolExecutor
from datetime import datetime

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
TARGETS = json.load(open(os.path.join(ROOT, 'GameData', 'TuningProfiles', 'balance-targets.json'), encoding='utf-8-sig'))
CLASSES = ['Barbarian', 'Warrior', 'Rogue', 'Wizard']


def median_steps():
    """STEP (level -> catalog stepping stone) from median-curve-solve.py, so both scripts price the same build."""
    src = open(os.path.join(ROOT, 'Scripts', 'median-curve-solve.py'), encoding='utf-8').read()
    for node in ast.parse(src).body:
        if isinstance(node, ast.Assign) and any(getattr(t, 'id', '') == 'STEP' for t in node.targets):
            return ast.literal_eval(node.value)
    raise SystemExit('STEP not found in median-curve-solve.py')


STEP = median_steps()
ap = argparse.ArgumentParser()
ap.add_argument('--patch', default='', help='balance patch name or path (default: active patch)')
ap.add_argument('--tools', default='Code/bin/ScanTools', help='folder for the built DF.dll')
ap.add_argument('--skip-build', action='store_true')
ap.add_argument('--median-levels', default=','.join(str(l) for l in TARGETS['matrixLevels']))
ap.add_argument('--dungeon-levels', default='10,20,35,50', help='DUNGEONSIM levels (empty = skip)')
ap.add_argument('--heroes', type=int, default=2, help='heroes (DUNGEONSIM processes) per class per level')
ap.add_argument('--dungeons', type=int, default=4, help='dungeons per hero (hero setup ~1 min, a dungeon ~10 s; heroes carry the variance)')
ap.add_argument('--encounters', type=int, default=8, help='BUILDLAB fights per enemy')
ap.add_argument('--builds', default='', help='comma list of catalog builds (default: all)')
ap.add_argument('--min-fights', type=int, default=6, help='real fights an enemy needs at a level to be judged')
ap.add_argument('--bot-ratio', type=float, default=1.33, help='flag bot/median kill-speed ratios outside 1/x .. x')
ap.add_argument('--from', dest='from_scan', default='', help='re-analyze the raw results saved in an earlier scan JSON (no sims)')
ap.add_argument('--compare', default='', help='earlier balance-scan JSON: print level metrics side by side')
ap.add_argument('--parallel', type=int, default=os.cpu_count() or 8)
ap.add_argument('--json', default='', help='output path (default GameData/TuningRuns/balance-scan-<patch>-<time>.json)')
A = ap.parse_args()

WORK = tempfile.mkdtemp(prefix='dfscan-')
DLL = os.path.join(ROOT, A.tools, 'DF.dll')
ENEMIES = TARGETS['matrixEnemies']
TURNS, TURNS_TOL = float(TARGETS['heroTurnsTarget']), float(TARGETS['heroTurnsTolerance'])
ENEMY_TOL = float(TARGETS.get('perEnemyTurnsTolerance', 4))
CLEAR_TOL = float(TARGETS.get('realClearTolerance', 4))
RETREAT = int(TARGETS.get('retreatBelowHpPercent', 50))


def patch_name():
    if A.patch:
        return A.patch
    prof = os.path.join(ROOT, 'GameData', 'PatchProfile.json')
    return json.load(open(prof, encoding='utf-8-sig')).get('activeBalancePatch', 'default') if os.path.exists(prof) else 'default'


PATCH = patch_name()


def interp(anchors, level, key):
    a = sorted(anchors, key=lambda x: x['level'])
    if level <= a[0]['level']:
        return float(a[0][key])
    for p, q in zip(a, a[1:]):
        if level <= q['level']:
            return p[key] + (level - p['level']) / (q['level'] - p['level']) * (q[key] - p[key])
    return float(a[-1][key])


def wilson(k, n, z=1.96):
    if n == 0:
        return 0.0, 1.0
    p = k / n; den = 1 + z * z / n
    c = (p + z * z / (2 * n)) / den
    h = z * math.sqrt(p * (1 - p) / n + z * z / (4 * n * n)) / den
    return max(0.0, c - h), min(1.0, c + h)


T95 = {1: 12.71, 2: 4.30, 3: 3.18, 4: 2.78, 5: 2.57, 6: 2.45, 7: 2.36, 8: 2.31, 9: 2.26, 10: 2.23}


def hero_interval(heroes):
    """95% interval for a clear rate from (cleared, dungeons) per hero: a t interval over hero clear rates (dungeons in one
    hero are correlated through its build), widened to at least the Wilson interval on the pooled dungeon count."""
    k = sum(c for c, _ in heroes); n = sum(d for _, d in heroes)
    lo, hi = wilson(k, n)
    rates = [c / d for c, d in heroes if d]
    if len(rates) >= 2:
        m = k / n; half = T95.get(len(rates) - 1, 1.96) * statistics.stdev(rates) / math.sqrt(len(rates))
        lo, hi = min(lo, max(0.0, m - half)), max(hi, min(1.0, m + half))
    return lo, hi


# ---------------------------------------------------------------- jobs

def build():
    print(f'Building tools -> {A.tools}', flush=True)
    r = subprocess.run(['dotnet', 'build', os.path.join(ROOT, 'Code', 'Code.csproj'), '-c', 'Debug', '-o', os.path.join(ROOT, A.tools),
                        f'-p:BaseIntermediateOutputPath={os.path.join(WORK, "obj")}{os.sep}'],
                       cwd=ROOT, stdout=subprocess.PIPE, stderr=subprocess.STDOUT, text=True)
    if r.returncode != 0:
        print(r.stdout[-3000:]); raise SystemExit('Build failed.')


def copy_gamedata():
    skip = {'Audio', 'Video', 'CombatLogs', 'LabSnapshots', 'TuningRuns'}
    dst = os.path.join(WORK, 'GameData')
    shutil.copytree(os.path.join(ROOT, 'GameData'), dst, ignore=lambda d, names: [
        n for n in names if (n in skip and os.path.isdir(os.path.join(d, n))) or n.startswith('character_')])
    return dst


def buildlab(build_id, level):
    out = os.path.join(WORK, f'bl-{build_id}-L{level}.json')
    subprocess.run(['dotnet', DLL, 'BUILDLAB', '--patch', PATCH, '--builds', build_id, '--level', str(level),
                    '--max-step', str(STEP[level]), '--level-slots', '--encounters', str(A.encounters), '--enemies', ENEMIES,
                    '--seed', '1', '--json', out], cwd=ROOT, stdout=subprocess.DEVNULL, stderr=subprocess.DEVNULL)
    try:
        rows = [r for r in json.load(open(out, encoding='utf-8-sig')) if r['Step'] == STEP[level]]
        return rows[0] if rows else None
    except Exception:
        return None


def dungeonsim(level, cls, data_dir):
    cls, hero = cls.split('#')
    out = os.path.join(WORK, f'ds-L{level}-{cls}-{hero}.json')
    log = open(os.path.join(WORK, f'ds-L{level}-{cls}-{hero}.log'), 'w', encoding='utf-8')
    try:
        subprocess.run(['dotnet', DLL, 'DUNGEONSIM', '--patch', PATCH, '--level', str(level), '--runs', '1',
                        '--dungeons', str(A.dungeons), '--classes', cls, '--retreat-below', str(RETREAT), '--json', out],
                       cwd=ROOT, stdout=log, stderr=subprocess.STDOUT, env=dict(os.environ, DF_GAMEDATA_DIR=data_dir), timeout=1800)
        return json.load(open(out, encoding='utf-8-sig'))
    except Exception:
        return None


def run_all(median_levels, dungeon_levels, builds):
    data_dir = copy_gamedata() if dungeon_levels else None
    # Dungeon shards first: they are the long poles (~1-2 min each), BUILDLAB jobs (2-8 s) fill the gaps.
    jobs = [('ds', l, f'{c}#{h}') for l in dungeon_levels for c in CLASSES for h in range(A.heroes)] + [('bl', l, b) for l in median_levels for b in builds]
    def go(j):
        t = time.time()
        r = dungeonsim(j[1], j[2], data_dir) if j[0] == 'ds' else buildlab(j[2], j[1])
        SECONDS[j] = time.time() - t
        return j, r
    with ThreadPoolExecutor(min(A.parallel, len(jobs))) as ex:
        return list(ex.map(go, jobs))


# ---------------------------------------------------------------- analysis

FINDINGS, METRICS, SECONDS = [], {}, {}


def finding(area, level, subject, value, target, severity, status, focus, detail=''):
    FINDINGS.append({'area': area, 'level': level, 'subject': subject, 'value': value, 'target': target,
                     'severity': round(severity, 2), 'status': status, 'focus': focus, 'detail': detail})


def tools_arg():
    return f'--tools {A.tools} --patch {PATCH}'


def analyze_median(results, median_levels):
    out = {}
    for l in median_levels:
        rows = [r for (k, lv, _), r in results if k == 'bl' and lv == l and r]
        if not rows:
            finding('median build', l, 'all', 'no data', '-', 99, 'FAIL', f'see BUILDLAB logs; rerun with --builds B1'); continue
        t = statistics.median(r['HeroTurns'] for r in rows); w = 100 * statistics.median(r['WinRate'] for r in rows)
        per = {}
        for name in ENEMIES.split(','):
            pe = [r['PerEnemy'][name] for r in rows if name in (r.get('PerEnemy') or {})]
            if pe:
                per[name] = (statistics.median(x['HeroTurns'] for x in pe), statistics.median(x['HeroTurnsP90'] for x in pe))
        out[l] = {'turns': t, 'winRate': w, 'builds': len(rows), 'perEnemy': per}
        METRICS[f'median turns L{l}'] = round(t, 2); METRICS[f'median full-HP win L{l}'] = round(w, 1)
        if l > TARGETS.get('heroTurnsCheckMaxLevel', 50):
            continue   # lethal levels: short losses, turns are not a target
        dev = abs(t - TURNS)
        if dev > TURNS_TOL:
            finding('median turns', l, 'median build', f'{t:.1f} ({len(rows)} builds)', f'{TURNS:g} +/- {TURNS_TOL:g}', dev / TURNS_TOL - 1, 'FAIL',
                    f'python Scripts/median-curve-solve.py {tools_arg()} --only health --levels {l}   (writes the patch)',
                    'enemies die too fast' if t < TURNS else 'enemies take too long')
        for name, (et, p90) in per.items():
            edev = abs(et - TURNS)
            if edev > ENEMY_TOL:
                finding('median turns per enemy', l, name, f'{et:.1f} (p90 {p90:.0f})', f'{TURNS:g} +/- {ENEMY_TOL:g}', edev / ENEMY_TOL - 1, 'FAIL',
                        f'dotnet {A.tools}/DF.dll BUILDLAB --patch {PATCH} --level {l} --max-step {STEP[l]} --level-slots --enemies "{name}" --encounters 40 --seed 2'
                        f'  then  FIGHTLOG --level {l} --enemy "{name}" --gear typical',
                        'slow pack or tank (Enemies.json HP / bodies)' if et > TURNS else 'paper enemy')
    return out


def analyze_dungeons(results, dungeon_levels, median):
    by_class = {c: [] for c in CLASSES}   # (cleared, dungeons) per hero over all scanned levels
    for l in dungeon_levels:
        docs = [(c, d) for (k, lv, c), d in results if k == 'ds' and lv == l and d]
        if not docs:
            finding('dungeon clear', l, 'all', 'no data', '-', 99, 'FAIL', f'see DUNGEONSIM logs in {WORK}'); continue
        outs = [o for _, d in docs for o in d['Outcomes'] if o['Class'] != 'all']   # one row per hero (--runs 1)
        n = sum(o['Dungeons'] for o in outs); k = sum(o['Cleared'] for o in outs)
        died = sum(o['Died'] for o in outs)
        nf = sum(o['Fights'] for o in outs)
        acts = sum(o.get('HeroActionsPerFight', 0) * o['Fights'] for o in outs) / nf if nf else 0
        hp = sum(o['HpLostPerFight'] * o['Fights'] for o in outs) / nf if nf else 0
        for o in outs:
            by_class[o['Class']].append((o['Cleared'], o['Dungeons']))
        clear = 100 * k / n if n else 0; lo, hi = (100 * x for x in hero_interval([(o['Cleared'], o['Dungeons']) for o in outs]))
        METRICS[f'dungeon clear L{l}'] = round(clear, 1); METRICS[f'bot hero acts L{l}'] = round(acts, 2)
        METRICS[f'death per dungeon L{l}'] = round(100 * died / n, 1) if n else 0
        target = interp(TARGETS['realClearAnchors'], l, 'clear')
        if abs(clear - target) > CLEAR_TOL:
            inside = hi >= target - CLEAR_TOL and lo <= target + CLEAR_TOL
            finding('dungeon clear', l, 'all classes', f'{clear:.0f}% [{lo:.0f}-{hi:.0f}] {len(outs)} heroes, n={n}', f'{target:.0f}% +/- {CLEAR_TOL:g}',
                    (abs(clear - target) - CLEAR_TOL) / CLEAR_TOL * (0.5 if inside else 1), 'LIKELY' if inside else 'FAIL',
                    f'python Scripts/dungeon-sim.py {tools_arg()} --solve {l} --target {target:.0f}',
                    ('too easy' if clear > target else 'too hard') + f', death {100 * died / n:.0f}%/dungeon, HP lost/fight {100 * hp:.1f}%')
        # Bot vs median build: if real play kills much faster or slower than the build the HP curve is solved for,
        # the median build's stepping stone (STEP) is not what players have, and the curve targets the wrong hero.
        if l in median and acts > 0:
            ratio = median[l]['turns'] / acts
            METRICS[f'median/bot kill speed L{l}'] = round(ratio, 2)
            if ratio > A.bot_ratio or ratio < 1 / A.bot_ratio:
                finding('bot vs median build', l, 'kill speed', f'x{ratio:.2f} (bot {acts:.1f} acts, median {median[l]["turns"]:.1f} turns)',
                        f'1/{A.bot_ratio:g} .. {A.bot_ratio:g}', abs(math.log(ratio)) / math.log(A.bot_ratio) - 1, 'FAIL',
                        f'compare BUILDLAB --level {l} --max-step {STEP[l] + (1 if ratio > 1 else -1)} --level-slots with the bot (RUNLOG); '
                        f'then move STEP[{l}] in median-curve-solve.py or the health anchor',
                        'real play outpaces the median build' if ratio > 1 else 'median build outpaces real play')
        # Real fights per enemy at this level: turns and HP cost.
        fights = [dict(f, Hero=i) for i, (_, d) in enumerate(docs) for f in d.get('Fights', [])]
        if fights:
            mean_hp = statistics.mean(f['HpLost'] for f in fights) or 1e-9
            # Judge enemies against this level's real mean, not the 11-turn target: a level-wide gap (the bot kills
            # faster than the median build everywhere) is the bot-vs-median row, not a per-enemy problem.
            mean_acts = statistics.mean(f['HeroActions'] for f in fights) or 1e-9
            rel_tol = ENEMY_TOL / TURNS
            groups = {}
            for f in fights:
                groups.setdefault(f['Enemy'], []).append(f)
            for name, fs in groups.items():
                if len(fs) < A.min_fights:
                    continue
                t = statistics.mean(f['HeroActions'] for f in fs); h = statistics.mean(f['HpLost'] for f in fs)
                kills = sum(1 for f in fs if f['HeroDied'])
                heroes = len({f['Hero'] for f in fs})
                # One hero's build can make an enemy look slow or deadly: FAIL only with enough fights from several heroes.
                status = 'FAIL' if len(fs) >= 3 * A.min_fights and heroes >= 4 else 'LIKELY'
                if abs(t / mean_acts - 1) > rel_tol:
                    finding('real fights per enemy', l, name, f'{t:.1f} acts (n={len(fs)}, {heroes} heroes)',
                            f'level mean {mean_acts:.1f} +/- {100 * rel_tol:.0f}%', abs(t / mean_acts - 1) / rel_tol - 1, status,
                            f'dotnet {A.tools}/DF.dll FIGHTLOG --patch {PATCH} --level {l} --enemy "{name}" --gear typical')
                if h > 2 * mean_hp and h > 0.05:
                    finding('real HP cost per enemy', l, name, f'{100 * h:.1f}% HP/fight (n={len(fs)}, {heroes} heroes, kills {kills})',
                            f'<= 2x level mean {100 * mean_hp:.1f}%', h / (2 * mean_hp) - 1, status,
                            f'python Scripts/enemy-calibrate.py --patch {PATCH} --iterations 1   (identity calibration, whole roster)')
    # Class spread over every scanned level (more dungeons than any one level).
    tot_k = sum(c for v in by_class.values() for c, _ in v); tot_n = sum(d for v in by_class.values() for _, d in v)
    if tot_n:
        mean = tot_k / tot_n
        for c, heroes in by_class.items():
            k = sum(x for x, _ in heroes); n = sum(d for _, d in heroes)
            if not n:
                continue
            METRICS[f'class clear {c}'] = round(100 * k / n, 1)
            lo, hi = hero_interval(heroes)
            if hi < mean or lo > mean:
                finding('class spread', 0, c, f'{100 * k / n:.0f}% clear [{100 * lo:.0f}-{100 * hi:.0f}] {len(heroes)} heroes, n={n}', f'all classes {100 * mean:.0f}%',
                        abs(k / n - mean) / 0.1, 'FAIL',
                        f'classBalance.{c}.healthMultiplier; confirm with python Scripts/dungeon-sim.py --levels {",".join(map(str, dungeon_levels))} --shards 4',
                        'below the rest' if k / n < mean else 'above the rest')


def print_report(provenance, elapsed, median_levels, dungeon_levels):
    print(f'\nBalance scan: patch {PATCH}  ({elapsed / 60:.1f} min)')
    print(f'Provenance: {" | ".join(sorted(provenance)) or "unknown"}' + ('  MIXED' if len(provenance) > 1 else ''))
    print('\nLevel summary')
    print(f"{'level':>6}{'step':>5}{'median turns':>14}{'bot acts':>10}{'clear':>8}{'target':>8}{'death':>8}")
    for l in sorted(set(median_levels) | set(dungeon_levels)):
        g = lambda key, fmt: (fmt.format(METRICS[key]) if key in METRICS else '-')
        tgt = f"{interp(TARGETS['realClearAnchors'], l, 'clear'):.0f}%" if l in dungeon_levels else '-'
        print(f"{l:>6}{STEP.get(l, '-'):>5}{g(f'median turns L{l}', '{:.1f}'):>14}{g(f'bot hero acts L{l}', '{:.1f}'):>10}"
              f"{g(f'dungeon clear L{l}', '{:.0f}%'):>8}{tgt:>8}{g(f'death per dungeon L{l}', '{:.0f}%'):>8}")
    ranked = sorted(FINDINGS, key=lambda f: (f['status'] != 'FAIL', -f['severity']))
    print(f'\nProblem areas ({len(ranked)}, worst first; LIKELY = inside sampling noise, confirm before acting)')
    if not ranked:
        print('  none at scan resolution. Gate with .\\Scripts\\balance-check.ps1 before saving the patch.')
    for i, f in enumerate(ranked, 1):
        lvl = f"L{f['level']}" if f['level'] else 'all'
        print(f"{i:>3}. [{f['status']:<6}] {f['area']} {lvl} {f['subject']}: {f['value']} (target {f['target']})"
              + (f" - {f['detail']}" if f['detail'] else ''))
        print(f"       focus: {f['focus']}")
    return ranked


def print_compare(path):
    try:
        old = json.load(open(path, encoding='utf-8-sig'))
    except Exception as e:
        print(f'\n--compare: cannot read {path}: {e}'); return
    print(f"\nCompared with {os.path.basename(path)} (patch {old.get('patch')}):")
    print(f"{'metric':32}{'before':>10}{'now':>10}{'change':>10}")
    for key in sorted(set(METRICS) | set(old.get('metrics', {}))):
        a, b = old.get('metrics', {}).get(key), METRICS.get(key)
        delta = f'{b - a:+.1f}' if a is not None and b is not None else ''
        print(f"{key:32}{'-' if a is None else a:>10}{'-' if b is None else b:>10}{delta:>10}")
    before = {(f['area'], f['level'], f['subject']) for f in old.get('findings', [])}
    now = {(f['area'], f['level'], f['subject']) for f in FINDINGS}
    if before - now:
        print('  resolved: ' + '; '.join(f'{a} L{l} {s}' for a, l, s in sorted(before - now)))
    if now - before:
        print('  new:      ' + '; '.join(f'{a} L{l} {s}' for a, l, s in sorted(now - before)))


def main():
    t0 = time.time()
    median_levels = [int(x) for x in A.median_levels.split(',') if x.strip()]
    dungeon_levels = [int(x) for x in A.dungeon_levels.split(',') if x.strip()]
    bad = [l for l in median_levels if l not in STEP]
    if bad:
        raise SystemExit(f'--median-levels {bad}: median-curve-solve.py STEP has no stepping stone for them ({sorted(STEP)})')
    builds = [b for b in A.builds.split(',') if b] or [b['id'] for b in json.load(open(
        os.path.join(ROOT, 'GameData', 'TuningProfiles', 'build-ladders.json'), encoding='utf-8-sig'))['builds']]
    if A.from_scan:
        saved = json.load(open(A.from_scan, encoding='utf-8-sig'))
        results = [(tuple(j), r) for j, r in saved['raw']]
        median_levels = sorted({j[1] for j, _ in results if j[0] == 'bl'})
        dungeon_levels = sorted({j[1] for j, _ in results if j[0] == 'ds'})
        return analyze_and_report(results, median_levels, dungeon_levels, t0)
    if not A.skip_build:
        build()
    print(f'Scanning patch {PATCH}: median build x{len(builds)} at L{",".join(map(str, median_levels))}; '
          f'dungeons {A.heroes} heroes x {A.dungeons} per class at L{",".join(map(str, dungeon_levels)) or "-"}', flush=True)
    results = run_all(median_levels, dungeon_levels, builds)
    for l in dungeon_levels:
        secs = [v for (k, lv, _), v in SECONDS.items() if k == 'ds' and lv == l]
        print(f'  DUNGEONSIM L{l}: {statistics.mean(secs):.0f} s per hero (max {max(secs):.0f})', flush=True)
    analyze_and_report(results, median_levels, dungeon_levels, t0)


def analyze_and_report(results, median_levels, dungeon_levels, t0):
    provenance = {d['Provenance'] for (k, _, _), d in results if k == 'ds' and d and d.get('Provenance')}
    median = analyze_median(results, median_levels)
    if dungeon_levels:
        analyze_dungeons(results, dungeon_levels, median)
    ranked = print_report(provenance, time.time() - t0, median_levels, dungeon_levels)
    if A.compare:
        print_compare(A.compare)
    out = A.json or os.path.join(ROOT, 'GameData', 'TuningRuns', f'balance-scan-{os.path.basename(PATCH).replace(".json", "")}-{datetime.now():%Y%m%d-%H%M%S}.json')
    os.makedirs(os.path.dirname(os.path.abspath(out)), exist_ok=True)
    json.dump({'patch': PATCH, 'provenance': sorted(provenance), 'settings': vars(A), 'metrics': METRICS, 'findings': ranked,
               'seconds': {f'{k} L{l} {who}': round(v) for (k, l, who), v in SECONDS.items()},
               'raw': [[list(j), r] for j, r in results]},
              open(out, 'w', encoding='utf-8'), indent=1)
    print(f'\nWrote {out}  (pass it to --compare after your next change)')
    shutil.rmtree(WORK, ignore_errors=True)
    sys.exit(1 if any(f['status'] == 'FAIL' for f in ranked) else 0)


if __name__ == '__main__':
    main()
