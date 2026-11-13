#!/usr/bin/env python3
"""The balance loop: measure on fresh seeds, solve what is off, confirm on other fresh seeds, keep only what beats noise.

Written from the 2026-11-05 overnight runs (Documentation/BALANCE_PROCESS.md). Every step writes JSON and a log line
under GameData/TuningRuns/balance-loop/<run>/, and `export` turns a run into the dashboard's data files.

  python Scripts/balance-loop.py measure --patch fastbot-v21 --seeds 7101,7102,7103,7104 --ref fastbot-v20
  python Scripts/balance-loop.py solve --patch fastbot-v21 --knob damage --next fastbot-v22
  python Scripts/balance-loop.py solve --patch fastbot-v22 --knob class --next fastbot-v23
  python Scripts/balance-loop.py solve --patch fastbot-v21 --knob health --next fastbot-v22
  python Scripts/balance-loop.py botcheck --patch fastbot-v21
  python Scripts/balance-loop.py export --run GameData/TuningRuns/balance-loop/2026-11-05-revamp --out <dir>

Knobs:
  damage  enemy damage band per region band; target realClearAnchors (all classes).
  health  enemy health band; target heroTurnsAnchors (hero actions per fight in real dungeons, rules bot).
  class   each class's hero damage band (classBalance.<class>.damageCurve); target realClearAnchors per class, so the
          best and worst class end within classParityMaxSpread.
Rules learned overnight: judge on seeds the choice never saw (selection bias made a 4.0->2.85 gain read 3.14->2.96
held-out); paired comparisons on shared seeds; untouched bands must come out identical (checks determinism); the
surrogate prior is not used (it was 20-30% off).
"""
import argparse, glob, hashlib, json, math, os, shutil, statistics, subprocess, sys, time
from datetime import datetime, timezone

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import balancepatch as bp
from simstats import mean_se, paired_gap_change, beats_noise
from simpool import spark_slots_arg

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
TARGETS = json.load(open(os.path.join(ROOT, 'GameData', 'TuningProfiles', 'balance-targets.json'), encoding='utf-8'))
CLASSES = ['Barbarian', 'Warrior', 'Rogue', 'Wizard']
MIDPOINTS = [3, 8, 13, 18, 23, 28, 33, 38, 43, 48, 53, 58, 63, 68, 73]
PY = sys.executable


def interp(pts, level):
    pts = sorted(pts)
    if level <= pts[0][0]: return pts[0][1]
    for (l0, c0), (l1, c1) in zip(pts, pts[1:]):
        if level <= l1: return c0 + (level - l0) / (l1 - l0) * (c1 - c0)
    return pts[-1][1]


def clear_target(L): return interp([(a['level'], float(a['clear'])) for a in TARGETS['realClearAnchors']], L)


def turns_target(L):
    pts = [(a['level'], float(a['turns'])) for a in TARGETS.get('heroTurnsAnchors', [])]
    return interp(pts, L) if pts and L <= max(p[0] for p in pts) else float(TARGETS.get('heroTurnsTarget', 11))


PARITY = float(TARGETS.get('classParityMaxSpread', 10))


class Run:
    def __init__(self, name):
        self.dir = os.path.join(ROOT, 'GameData', 'TuningRuns', 'balance-loop', name)
        os.makedirs(self.dir, exist_ok=True)
        self.logf = open(os.path.join(self.dir, 'log.txt'), 'a', encoding='utf-8')

    def log(self, msg):
        line = f'[{time.strftime("%H:%M:%S")}] {msg}'
        print(line, flush=True); self.logf.write(line + '\n'); self.logf.flush()

    def call(self, args, name):
        t = time.time(); self.log('RUN ' + ' '.join(map(str, args)))
        with open(os.path.join(self.dir, name), 'w', encoding='utf-8') as f:
            r = subprocess.run([PY, '-u'] + [str(a) for a in args], cwd=ROOT, stdout=f, stderr=subprocess.STDOUT,
                               env={**os.environ, 'PYTHONIOENCODING': 'utf-8'})
        self.log(f'  exit {r.returncode} {time.time() - t:.0f}s {name}')
        return r.returncode

    def save(self, name, obj):
        json.dump(obj, open(os.path.join(self.dir, name), 'w', encoding='utf-8'), indent=1, default=str)


def sim(run, A, patch, seed, tag, levels=None):
    """dungeon-sim.py at every level, all classes, one seed -> {level: result}."""
    levels = levels or A.levels
    js = os.path.join(run.dir, f'{tag}-{patch}-s{seed}.json')
    # Seeded sims are bit-identical, so a run of the same resolved patch content on the same seed/levels/bot/shards
    # under another tag (conf vs measure) is reused instead of rerun. Keyed on content, not name, because candidate
    # names (<patch>-<knob>-cand) are rewritten with different content on every solve.
    key = '|'.join(map(str, (hashlib.sha1(json.dumps(bp.resolve(patch), sort_keys=True).encode()).hexdigest(),
                             seed, ','.join(map(str, levels)), A.bot, A.shards)))
    idx_path = os.path.join(run.dir, 'simcache.json')
    idx = json.load(open(idx_path, encoding='utf-8')) if os.path.exists(idx_path) else {}
    if not os.path.exists(js) and os.path.exists(idx.get(key, '')):
        shutil.copyfile(idx[key], js); run.log(f'reuse {os.path.basename(idx[key])} -> {os.path.basename(js)} (same patch content and seed)')
    if not os.path.exists(js):
        run.call(['Scripts/dungeon-sim.py', '--levels', ','.join(map(str, levels)), '--patch', patch, '--shards', A.shards,
                  '--seed', seed, '--spark', A.spark, '--tools', A.tools, '--bot', A.bot, '--json', js], os.path.basename(js)[:-5] + '.txt')
        if os.path.exists(js):
            idx[key] = js; json.dump(idx, open(idx_path, 'w', encoding='utf-8'), indent=1)
    return {r['level']: r for r in json.load(open(js, encoding='utf-8'))}


def measure(run, A, patch, seeds, tag):
    """Per level: clear (all), per-class clear, hero actions; lists over seeds."""
    out = {}
    for s in seeds:
        for L, r in sim(run, A, patch, s, tag).items():
            o = out.setdefault(L, {'clear': [], 'acts': [], 'cls': {c: [] for c in CLASSES}, 'dun': {}})
            o['clear'].append(100 * r['clear']); o['acts'].append(r['heroActionsPerFight'])
            for t, v in (r.get('perDungeon') or {}).items():  # '?' = dungeon with no recorded fight, not a theme
                if v.get('dungeons') and t != '?':
                    d = o['dun'].setdefault(t, [0, 0])
                    d[0] += v['clear'] * v['dungeons']; d[1] += v['dungeons']
            for c, v in r['perClassClear'].items():
                if v is not None: o['cls'][c].append(100 * v)
    return out


def table(m):
    rows = {}
    for L, o in sorted(m.items()):
        cm = {c: statistics.mean(v) for c, v in o['cls'].items() if v}
        rows[L] = {'target': round(clear_target(L), 1), 'clear': round(statistics.mean(o['clear']), 1),
                   'clearSE': round(mean_se(o['clear'])[1], 1), 'heroActions': round(statistics.mean(o['acts']), 2),
                   'turnsTarget': round(turns_target(L), 1), 'classes': {c: round(v, 1) for c, v in cm.items()},
                   'classSpread': round(max(cm.values()) - min(cm.values()), 1) if cm else None,
                   # Per dungeon theme, pooled over seeds (the band's three dungeons since 2026-11-12).
                   'dungeons': {t: round(100 * k / n, 1) for t, (k, n) in sorted(o.get('dun', {}).items()) if n},
                   'dungeonSpread': (round(max(100 * k / n for k, n in o['dun'].values() if n) - min(100 * k / n for k, n in o['dun'].values() if n), 1)
                                     if len([1 for k, n in o.get('dun', {}).values() if n]) > 1 else None)}
    return rows


def summary_line(rows):
    gap = statistics.mean(abs(r['clear'] - r['target']) for r in rows.values())
    spread = statistics.mean(r['classSpread'] for r in rows.values() if r['classSpread'] is not None)
    acts = statistics.mean(abs(r['heroActions'] - r['turnsTarget']) for r in rows.values())
    over = sum(1 for r in rows.values() if (r['classSpread'] or 0) > PARITY)
    dspread = [r['dungeonSpread'] for r in rows.values() if r.get('dungeonSpread') is not None]
    return {'meanClearGap': round(gap, 2), 'meanClassSpread': round(spread, 1), 'bandsOverParity': over,
            'meanTurnsGap': round(acts, 2), 'meanDungeonSpread': round(statistics.mean(dspread), 1) if dspread else None}


# ---------------------------------------------------------------- commands

def cmd_measure(A):
    run = Run(A.run)
    seeds = [int(s) for s in A.seeds.split(',')]
    res = {'command': 'measure', 'patch': A.patch, 'seeds': seeds, 'at': datetime.now(timezone.utc).isoformat()}
    m = table(measure(run, A, A.patch, seeds, 'measure'))
    res['levels'] = m; res['summary'] = summary_line(m)
    if A.ref:
        r = table(measure(run, A, A.ref, seeds, 'measure'))
        res['ref'] = A.ref; res['refLevels'] = r; res['refSummary'] = summary_line(r)
    run.save(f'measure-{A.patch}.json', res)
    run.log(f'measure {A.patch}: {res["summary"]}' + (f' | {A.ref}: {res["refSummary"]}' if A.ref else ''))
    for L, r in m.items():
        print(f'L{L:<3} target {r["target"]:5.1f} clear {r["clear"]:5.1f}±{r["clearSE"]:<4} acts {r["heroActions"]:5.2f}/{r["turnsTarget"]:<4} '
              f'spread {r["classSpread"]:5.1f}  ' + ' '.join(f'{c[:3]} {v:5.1f}' for c, v in r['classes'].items()))
        if r.get('dungeons'):
            print('     dungeons: ' + '  '.join(f'{t} {v:5.1f}' for t, v in r['dungeons'].items())
                  + (f'   (spread {r["dungeonSpread"]:.1f})' if r.get('dungeonSpread') is not None else ''))


def cmd_solve(A):
    run = Run(A.run)
    diag = [int(s) for s in A.diag_seeds.split(',')]
    conf = [int(s) for s in A.confirm_seeds.split(',')]
    S = {'command': 'solve', 'knob': A.knob, 'patch': A.patch, 'next': A.next, 'diagSeeds': diag, 'confirmSeeds': conf,
         'solves': {}, 'accepted': [], 'rejected': [], 'railed': [], 'at': datetime.now(timezone.utc).isoformat()}
    raw = measure(run, A, A.patch, diag, 'diag')
    rows = table(raw); S['diagnose'] = rows; S['diagSummary'] = summary_line(rows)
    run.log(f'solve {A.knob} on {A.patch}: diagnose {S["diagSummary"]}')
    # Cells to solve: (curve, class or None, level).
    cells = []
    for L, o in raw.items():
        if A.knob == 'damage':
            m, se = mean_se(o['clear'])
            if abs(m - clear_target(L)) > max(A.gap_floor, 2 * se): cells.append(('damage', None, L))
        elif A.knob == 'health':
            m, se = mean_se(o['acts'])
            if abs(m - turns_target(L)) > max(A.turns_floor, 2 * se): cells.append(('health', None, L))
        else:
            for c, v in o['cls'].items():
                m, se = mean_se(v)
                if abs(m - clear_target(L)) > max(A.class_floor, 2 * se): cells.append((f'class:{c}', c, L))
    run.log(f'cells to solve: {[(c if not k else k) + "@L" + str(L) for c, k, L in cells]}')
    solved = {}
    for i, (curve, cls, L) in enumerate(cells):
        tag = f'bo-{curve.replace(":", "-")}-L{L}'
        js = os.path.join(run.dir, tag + '.json')
        if not os.path.exists(js):
            run.call(['Scripts/bo-solve.py', '--level', L, '--patch', A.patch, '--knob', curve, '--bot', A.bot, '--seed', A.solve_seed + i,
                      '--tools', A.tools, '--spark', A.spark, '--tolerance', A.tolerance, '--max-rounds', A.max_rounds, '--scale-range', A.scale_range, '--json', js], tag + '.txt')
        if not os.path.exists(js):
            S['solves'][f'{curve}@L{L}'] = 'no result'; continue
        r = json.load(open(js, encoding='utf-8'))
        S['solves'][f'{curve}@L{L}'] = {k: r[k] for k in ('target', 'converged', 'scale', 'value', 'valueCI90', 'dungeons', 'band')}
        run.log(f'{curve}@L{L}: scale {r["scale"]:.3f} value {r["value"]:.4f} converged={r["converged"]} dungeons={r["dungeons"]}')
        # A solve pinned at the edge of --scale-range means the target is further than one step can reach. Apply the edge
        # value anyway (the paired confirm still decides) and report it, so the next run of the step can go further; these
        # used to be dropped silently (2026-11-05: health@L68 at x4 and class:Wizard@L23/43/53 at x0.25 vanished).
        lo, hi = (float(x) for x in A.scale_range.split(','))
        if r['scale'] <= lo * 1.08 or r['scale'] >= hi * 0.925:
            S['railed'].append(f'{curve}@L{L}')
            run.log(f'   {curve}@L{L} railed at x{r["scale"]:.3f} (scale range {A.scale_range}): applying the edge value; run the step again to go further')
        if abs(r['scale'] - 1) > 0.005:
            solved[(curve, L)] = r['value']
    if not solved:
        run.log('nothing to change'); run.save(f'solve-{A.knob}-{A.patch}.json', S); return

    def edit_with(cells_values):
        def edit(d):
            for (curve, L), v in cells_values.items():
                c = bp.get_curve(d, None, curve.split(':', 1)[1].lower()) if curve.startswith('class:') else bp.get_curve(d, curve)
                c['enabled'] = True
                bp.set_band(c, L, v)
        return edit

    cand = bp.variant(A.patch, f'{A.knob}-cand', edit_with(solved))
    cb = measure(run, A, A.patch, conf, 'conf')
    cc = measure(run, A, cand, conf, 'conf')
    keep = {}
    S['confirm'] = {}
    for (curve, L), v in solved.items():
        if curve == 'damage':
            base_v, cand_v, t = cb[L]['clear'], cc[L]['clear'], clear_target(L)
        elif curve == 'health':
            base_v, cand_v, t = cb[L]['acts'], cc[L]['acts'], turns_target(L)
        else:
            c = curve.split(':', 1)[1]; base_v, cand_v, t = cb[L]['cls'][c], cc[L]['cls'][c], clear_target(L)
        ch, se = paired_gap_change(base_v, cand_v, t)
        ok = beats_noise(ch, se, A.min_gain if curve != 'health' else 0.3)
        S['confirm'][f'{curve}@L{L}'] = {'target': round(t, 1), 'base': round(statistics.mean(base_v), 2),
                                          'cand': round(statistics.mean(cand_v), 2), 'gapChange': round(ch, 2), 'gapChangeSE': round(se, 2),
                                          'value': v, 'kept': ok}
        (S['accepted'] if ok else S['rejected']).append(f'{curve}@L{L}')
        if ok: keep[(curve, L)] = v
    # Determinism: a level none of the solved bands touch must read the same on the same seeds.
    touched = {bp.region_band(L) for _, L in solved}
    for L in cb:
        if bp.region_band(L) not in touched and A.knob != 'class' and cb[L]['clear'] != cc[L]['clear']:
            S.setdefault('determinism', []).append(f'L{L} differs although no solved band covers it')
    if S.get('determinism'): run.log('WARNING determinism: ' + '; '.join(S['determinism']))
    os.remove(bp.patch_path(cand))
    if keep:
        name = A.next
        full = bp.resolve(A.patch); edit_with(keep)(full)
        full['description'] = (full.get('description') or '').rstrip() + \
            f' {name}: {A.patch} + {A.knob} bands {sorted(f"{c}@L{L}" for c, L in keep)} (balance-loop, paired confirm on seeds {conf}).'
        bp.write_patch(bp.patch_path(name), full, base=A.patch)
        S['newPatch'] = name
    run.log(f'kept {S["accepted"]} dropped {S["rejected"]} -> {S.get("newPatch", "no new patch")}')
    if S['railed']:
        run.log(f'railed (target beyond one step; run this step again): {S["railed"]}')
    run.save(f'solve-{A.knob}-{A.patch}.json', S)


def cmd_botcheck(A):
    """Bot (DUNGEONSIM rules bot) hero actions per fight against the median catalog build's turns, per band midpoint."""
    run = Run(A.run)
    seeds = [int(s) for s in A.seeds.split(',')]
    m = table(measure(run, A, A.patch, seeds, 'botcheck'))
    js = os.path.join(run.dir, f'median-{A.patch}.json')
    if not os.path.exists(js):
        run.call(['Scripts/median-curve-solve.py', '--check', '--patch', A.patch, '--tools', A.tools,
                  '--levels', ','.join(map(str, A.levels)), '--json', js], f'median-{A.patch}.txt')
    med = {r['level']: r['heroTurns'] for r in json.load(open(js, encoding='utf-8'))}
    rows = {}
    for L, r in m.items():
        if L in med and r['heroActions'] > 0:
            ratio = med[L] / r['heroActions']
            rows[L] = {'bot': r['heroActions'], 'median': med[L], 'ratio': round(ratio, 2), 'flag': not (1 / 1.33 <= ratio <= 1.33)}
            print(f'L{L:<3} bot {r["heroActions"]:5.2f}  median build {med[L]:5.2f}  ratio {ratio:4.2f}' + ('  <-- off' if rows[L]['flag'] else ''))
    run.save(f'botcheck-{A.patch}.json', {'command': 'botcheck', 'patch': A.patch, 'rows': rows})
    run.log(f'botcheck {A.patch}: {sum(r["flag"] for r in rows.values())} of {len(rows)} bands outside 1/1.33-1.33')


def cmd_export(A):
    """Dashboard files from a run: status, levels (per patch, per measure), classes, solves, steps."""
    rdir = A.run if os.path.isdir(A.run) else os.path.join(ROOT, 'GameData', 'TuningRuns', 'balance-loop', A.run)
    out = A.out; os.makedirs(out, exist_ok=True)
    levels, classes, solves, steps = [], [], [], []
    files = sorted(glob.glob(os.path.join(rdir, 'measure-*.json')) + glob.glob(os.path.join(rdir, 'solve-*.json')), key=os.path.getmtime)
    for i, f in enumerate(files, start=1):
        S = json.load(open(f, encoding='utf-8'))
        sets = [(S['patch'], S.get('levels') or S.get('diagnose'))] + ([(S['ref'], S['refLevels'])] if S.get('ref') else [])
        for patch, rows in sets:
            for L, r in (rows or {}).items():
                levels.append({'step': i, 'patch': patch, 'level': int(L), 'target': r['target'], 'clear': r['clear'],
                               'heroActions': r['heroActions'], 'turnsTarget': r['turnsTarget'], 'classSpread': r['classSpread']})
                for c, v in r['classes'].items():
                    classes.append({'step': i, 'patch': patch, 'class': c, 'level': int(L), 'clear': v, 'target': r['target']})
        for k, v in (S.get('solves') or {}).items():
            if isinstance(v, dict):
                curve, L = k.split('@L')
                solves.append({'step': i, 'cell': k, 'knob': curve, 'level': int(L), 'target': v['target'], 'scale': round(v['scale'], 3),
                               'converged': 'yes' if v['converged'] else 'no', 'dungeons': v['dungeons'],
                               'kept': 'yes' if k in S.get('accepted', []) else 'no'})
        summ = S.get('summary') or S.get('diagSummary') or {}
        steps.append({'step': i, 'command': S['command'], 'knob': S.get('knob', ''), 'patch': S['patch'], 'result': S.get('newPatch') or '',
                      'meanClearGap': summ.get('meanClearGap'), 'meanClassSpread': summ.get('meanClassSpread'),
                      'meanTurnsGap': summ.get('meanTurnsGap'), 'kept': len(S.get('accepted', [])), 'dropped': len(S.get('rejected', []))})
    last = open(os.path.join(rdir, 'log.txt'), encoding='utf-8').read().splitlines()[-1] if os.path.exists(os.path.join(rdir, 'log.txt')) else ''
    status = [{'lastEvent': last[11:171], 'lastEventTime': last[1:9], 'steps': len(steps), 'parityLimit': PARITY,
               'updatedAt': datetime.now(timezone.utc).isoformat(timespec='seconds')}]
    for name, rows in (('status', status), ('levels', levels), ('classes', classes), ('solves', solves), ('steps', steps)):
        json.dump(rows, open(os.path.join(out, name + '.json'), 'w', encoding='utf-8'), indent=1)
    print(f'exported {len(steps)} steps to {out}')


def main():
    ap = argparse.ArgumentParser(description=__doc__, formatter_class=argparse.RawDescriptionHelpFormatter)
    sub = ap.add_subparsers(dest='cmd', required=True)

    def common(p):
        p.add_argument('--patch', required=True)
        p.add_argument('--run', default=datetime.now().strftime('%Y-%m-%d'), help='run folder name under GameData/TuningRuns/balance-loop')
        p.add_argument('--levels', default=','.join(map(str, MIDPOINTS)), type=lambda s: [int(x) for x in s.split(',')])
        p.add_argument('--shards', type=int, default=16, help='dungeon-sim shards per class (16 x 2 heroes = 32 heroes per class per seed)')
        p.add_argument('--spark', type=spark_slots_arg, default='auto')
        p.add_argument('--tools', default='Code/bin/BalanceTools')
        p.add_argument('--bot', default='synergy', choices=['rules', 'synergy', 'smart', 'basic'], help='DUNGEONSIM bot for every sim')

    p = sub.add_parser('measure'); common(p)
    p.add_argument('--seeds', default='7101,7102,7103,7104'); p.add_argument('--ref', default='')
    p = sub.add_parser('solve'); common(p)
    p.add_argument('--knob', required=True, choices=['damage', 'health', 'class'])
    p.add_argument('--next', required=True, help='name of the patch to write (an overlay of --patch)')
    p.add_argument('--diag-seeds', default='8101,8102,8103,8104'); p.add_argument('--confirm-seeds', default='8201,8202,8203,8204')
    p.add_argument('--gap-floor', type=float, default=3.0, help='damage: clear-rate gap (pts) worth solving')
    p.add_argument('--class-floor', type=float, default=4.0, help='class: per-class clear gap (pts) worth solving')
    p.add_argument('--turns-floor', type=float, default=0.8, help='health: hero-actions gap worth solving')
    p.add_argument('--min-gain', type=float, default=1.0)
    p.add_argument('--tolerance', type=float, default=0.07); p.add_argument('--max-rounds', type=int, default=16)
    p.add_argument('--solve-seed', type=int, default=41)
    p.add_argument('--scale-range', default='0.25,4', help='bo-solve scale range per cell, lo,hi (an edge hit is applied and reported)')
    p = sub.add_parser('botcheck'); common(p); p.add_argument('--seeds', default='9101,9102')
    p = sub.add_parser('export'); p.add_argument('--run', required=True); p.add_argument('--out', required=True)
    A = ap.parse_args()
    {'measure': cmd_measure, 'solve': cmd_solve, 'botcheck': cmd_botcheck, 'export': cmd_export}[A.cmd](A)


if __name__ == '__main__':
    main()
