#!/usr/bin/env python3
"""Solve the enemy damage curve as one flat value per 5-level region band (L11-75), each band to its clear target.

Regions cycle Forest/Water/Fire/Air every 5 levels, so a band is one region; a step curve absorbs region difficulty.
Targets: linear between the clear targets L10 75, L15 65, L20 70, L35 70, L50 65, L75 30, read at the band midpoint.
Measure at the band midpoint (dungeon-sim.py, all classes). Secant on log(multiplier).

Checked 2026-11-02 (fastbot-v8): 13 bands in ~25 min at --runs 8; band ends that jump (L15, L20, L50, L75) need a
two-level ramp anchor fitted afterwards with dungeon-sim.py --solve.

  python Scripts/band-solve.py --work <dir> --base GameData/Patches/Balance/<patch>.json --out <new patch.json>
"""
import argparse, json, math, os, subprocess, sys, time

REPO = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
ap = argparse.ArgumentParser()
ap.add_argument('--work', required=True, help='folder for state, log and per-eval patches (rerun resumes solved bands)')
ap.add_argument('--base', required=True)
ap.add_argument('--out', required=True)
ap.add_argument('--bands', default='11,16,21,26,31,36,41,46,51,56,61,66,71')
ap.add_argument('--runs', type=int, default=8)
ap.add_argument('--seed', type=int, default=101)
ap.add_argument('--evals', type=int, default=5)
ap.add_argument('--tol', type=float, default=2.5)
A = ap.parse_args()
SP = os.path.abspath(A.work)
os.makedirs(os.path.join(SP, 'runs'), exist_ok=True)
LOG = os.path.join(SP, 'band_solve.log')
STATE = os.path.join(SP, 'band_values.json')
TARGETS = [(10, 75), (15, 65), (20, 70), (35, 70), (50, 65), (75, 30)]


def target_at(L):
    for (l0, t0), (l1, t1) in zip(TARGETS, TARGETS[1:]):
        if l0 <= L <= l1:
            return t0 + (t1 - t0) * (L - l0) / (l1 - l0)
    return TARGETS[-1][1]


def log(m):
    line = time.strftime('%H:%M:%S ') + m
    print(line, flush=True)
    open(LOG, 'a', encoding='utf-8').write(line + '\n')


base = json.load(open(A.base, encoding='utf-8-sig'))
old = sorted(base['enemySystem']['damageCurve']['anchors'], key=lambda a: a['level'])


def interp(L):
    for a0, a1 in zip(old, old[1:]):
        if a0['level'] <= L <= a1['level']:
            t = (L - a0['level']) / (a1['level'] - a0['level'])
            return a0['multiplier'] + t * (a1['multiplier'] - a0['multiplier'])
    return old[-1]['multiplier']


def build(values, path):
    d = json.loads(json.dumps(base))
    keep = [a for a in old if a['level'] <= 10 or a['level'] > 75]
    steps = []
    for start, v in values.items():
        s = int(start)
        steps += [{'level': s, 'multiplier': round(v, 4)}, {'level': s + 4, 'multiplier': round(v, 4)}]
    d['enemySystem']['damageCurve']['anchors'] = sorted(keep + steps, key=lambda a: a['level'])
    json.dump(d, open(path, 'w', encoding='utf-8'), indent=2)


def clear(values, L, tag):
    p = os.path.join(SP, f'bs-{tag}.json')
    build(values, p)
    out = os.path.join(SP, 'runs', f'bs-{tag}.json')
    subprocess.run([sys.executable, 'Scripts/dungeon-sim.py', '--tools', 'Code/bin/Debug/net8.0', '--patch', p, '--levels', str(L),
                    '--runs', str(A.runs), '--seed', str(A.seed), '--json', out],
                   cwd=REPO, stdout=subprocess.DEVNULL, stderr=subprocess.DEVNULL, check=True)
    r = json.load(open(out))[0]
    return 100 * r['clear'], {k[:4]: round(100 * v) for k, v in r['perClassClear'].items()}


starts = [int(x) for x in A.bands.split(',')]
values = json.load(open(STATE)) if os.path.exists(STATE) else {}
for s in starts:          # unsolved bands start at the old curve's midpoint value
    values.setdefault(str(s), interp(s + 2))
for s in starts:
    key = str(s)
    if key in values and isinstance(values.get('_done'), list) and s in values['_done']:
        continue
    L, tgt = s + 2, target_at(s + 2)
    pts = []
    v = values[key]
    for i in range(A.evals):
        values[key] = v
        c, cls = clear({k: x for k, x in values.items() if not k.startswith('_')}, L, f'L{L}')
        log(f'L{L} (band {s}-{s + 4}) x{v:.4f} -> {c:.1f} (target {tgt:.1f}) {cls}')
        pts.append((math.log(v), c))
        if abs(c - tgt) <= A.tol:
            break
        if len(pts) == 1:
            slope = -50.0
        else:
            (x0, y0), (x1, y1) = pts[-2], pts[-1]
            slope = (y1 - y0) / (x1 - x0) if x1 != x0 else -50.0
            if slope > -10:
                slope = -50.0
        x = pts[-1][0] + (tgt - pts[-1][1]) / slope
        v = math.exp(min(pts[-1][0] + math.log(2.0), max(pts[-1][0] - math.log(2.5), x)))
    best = min(pts, key=lambda p: abs(p[1] - tgt))
    values[key] = round(math.exp(best[0]), 4)
    values.setdefault('_done', []).append(s)
    json.dump(values, open(STATE, 'w'), indent=1)
    log(f'band {s}-{s + 4} => x{values[key]} ({best[1]:.1f} vs {tgt:.1f})')
build({k: x for k, x in values.items() if not k.startswith('_')}, A.out)
log(f'wrote {A.out}')
