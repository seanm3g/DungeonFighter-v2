#!/usr/bin/env python3
"""Material set parity solve: one strength multiplier per material, fitted so a hero wearing a full forced set of it
(DUNGEONSIM --material X) clears near a common target at every test level.

The multiplier scales only that material's own knobs (its keyword's convert stat / mint speed / mint DR, and its
per-piece set speed / DR / attribute), so each set keeps its identity and only its strength moves. Class materials are
measured on their own class (materials pay their own attribute, so off-class is a choice, not a target); class-less
materials on all four classes.

  python Scripts/material-solve.py --base fastbot-syn47 --out diversity-mat --rounds 4 --levels 18,38,58

Writes GameData/Patches/Balance/<out>-r<N>.json each round (overlay on --base) and a log/JSON under
GameData/TuningRuns/material-solve-<out>/. Fresh seeds every round; the last round is a confirm (no update).
"""
import argparse, json, math, os, subprocess, sys, time

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import balancepatch as bp

ROOT = bp.ROOT
CLASSES = ['Barbarian', 'Warrior', 'Rogue', 'Wizard']

ap = argparse.ArgumentParser()
ap.add_argument('--base', default='fastbot-syn47')
ap.add_argument('--out', default='diversity-mat')
ap.add_argument('--rounds', type=int, default=4)
ap.add_argument('--levels', default='18,38,58')
ap.add_argument('--shards', type=int, default=12)
ap.add_argument('--seed', type=int, default=9100)
ap.add_argument('--spark', default='18')
ap.add_argument('--tools', default='Code/bin/DiversityTools')
ap.add_argument('--bonus', type=float, default=5, help='target = curve target at the level + this many points')
ap.add_argument('--slope', type=float, default=18, help='prior clear points per e-fold of the multiplier')
ap.add_argument('--materials', default='', help='comma list (default: all MaterialBuilds rows)')
ap.add_argument('--start', default='', help='JSON {material: multiplier} to resume from')
A = ap.parse_args()

LEVELS = [int(x) for x in A.levels.split(',')]
TARGETS = json.load(open(os.path.join(ROOT, 'GameData', 'TuningProfiles', 'balance-targets.json'), encoding='utf-8'))
OUTDIR = os.path.join(ROOT, 'GameData', 'TuningRuns', f'material-solve-{A.out}')
os.makedirs(OUTDIR, exist_ok=True)
LOG = open(os.path.join(OUTDIR, 'log.txt'), 'a', encoding='utf-8')


def log(msg):
    line = f'[{time.strftime("%H:%M:%S")}] {msg}'
    print(line, flush=True)
    LOG.write(line + '\n')
    LOG.flush()


def curve_target(level):
    """Real-clear target at a level from balance-targets.json (realClearAnchors, linear between anchors)."""
    pts = sorted((int(a['level']), float(a['clear'])) for a in TARGETS.get('realClearAnchors') or [])
    if not pts:
        return 70.0
    if level <= pts[0][0]:
        return pts[0][1]
    for (l0, v0), (l1, v1) in zip(pts, pts[1:]):
        if l0 <= level <= l1:
            return v0 + (v1 - v0) * (level - l0) / (l1 - l0)
    return pts[-1][1]


ROWS = json.load(open(os.path.join(ROOT, 'GameData', 'MaterialBuilds.json'), encoding='utf-8'))
MATS = {}
for r in ROWS:
    kw = r['synthesis'].split('=')[-1].strip()
    MATS[r['material']] = {'keyword': kw, 'classes': [r['class'].title()] if r.get('class') else CLASSES}
if A.materials:
    MATS = {m: v for m, v in MATS.items() if m in A.materials.split(',')}

BASE = bp.resolve(A.base)
COMBAT = BASE.get('combat', {})
KW_KNOBS = ['materialConvertPrimaryStatPerUnit', 'materialFightMintSpeedPercentPerUnit', 'materialFightMintDamageReductionPercentPerUnit']
PIECE_KNOBS = ['materialSetSpeedPercentPerPiece', 'materialSetDamageReductionPercentPerPiece', 'materialSetAttributePerPiece']


def get(d, knob, key):
    t = d.get(knob) or {}
    k = next((x for x in t if x.lower() == key.lower()), None)
    return t.get(k) if k else None


def build_patch(mult, name):
    full = json.loads(json.dumps(BASE))
    c = full.setdefault('combat', {})
    for m, s in mult.items():
        kw = MATS[m]['keyword']
        for knob in KW_KNOBS:
            v = get(COMBAT, knob, kw)
            if v is not None:
                c.setdefault(knob, {})[kw] = round(v * s, 4)
        for knob in PIECE_KNOBS:
            v = get(COMBAT, knob, m)
            if v is not None:
                c.setdefault(knob, {})[m] = round(v * s, 4)
    path = bp.patch_path(name)
    bp.write_patch(path, full, base=A.base)
    return name


def measure(patch, material, classes, seed):
    out = os.path.join(OUTDIR, f'{patch}-{material}.json')
    cmd = [sys.executable, 'Scripts/dungeon-sim.py', '--levels', A.levels, '--material', material, '--classes', ','.join(classes),
           '--shards', str(A.shards), '--seed', str(seed), '--spark', A.spark, '--tools', A.tools, '--patch', patch, '--json', out]
    with open(os.path.join(OUTDIR, f'{patch}-{material}.txt'), 'w', encoding='utf-8') as fh:
        subprocess.run(cmd, cwd=ROOT, stdout=fh, stderr=subprocess.STDOUT, env={**os.environ, 'DF_SPARK_APP_PER_HASH': '1'})
    rows = json.load(open(out, encoding='utf-8'))
    per = {}
    for r in rows:
        vals = [r['perClassClear'][c] for c in classes if r.get('perClassClear', {}).get(c) is not None]
        per[r['level']] = 100 * sum(vals) / len(vals) if vals else None
    return per


mult = {m: 1.0 for m in MATS}
if A.start:
    mult.update(json.load(open(A.start, encoding='utf-8')))
history = {m: [] for m in MATS}
target = {L: curve_target(L) + A.bonus for L in LEVELS}
log(f'base {A.base} targets {target} materials {list(MATS)}')

for rnd in range(1, A.rounds + 1):
    confirm = rnd == A.rounds
    name = build_patch(mult, f'{A.out}-r{rnd}')
    seed = A.seed + rnd * 37
    gaps = {}
    for m, info in MATS.items():
        per = measure(name, m, info['classes'], seed)
        g = [per[L] - target[L] for L in LEVELS if per.get(L) is not None]
        gaps[m] = sum(g) / len(g) if g else 0
        history[m].append((mult[m], gaps[m]))
        log(f'r{rnd} {m:<10} x{mult[m]:.3f}  ' + '  '.join(f'L{L} {per.get(L, 0):5.1f}' for L in LEVELS) + f'  gap {gaps[m]:+.1f}')
    json.dump({'round': rnd, 'patch': name, 'mult': mult, 'gaps': gaps, 'history': history},
              open(os.path.join(OUTDIR, f'round{rnd}.json'), 'w', encoding='utf-8'), indent=1)
    rms = math.sqrt(sum(g * g for g in gaps.values()) / len(gaps))
    log(f'r{rnd} rms gap {rms:.1f} pts  spread {max(gaps.values()) - min(gaps.values()):.1f}')
    if confirm:
        break
    for m in MATS:
        h = history[m]
        slope = A.slope
        if len(h) >= 2 and abs(math.log(h[-1][0]) - math.log(h[-2][0])) > 0.05:
            est = (h[-1][1] - h[-2][1]) / (math.log(h[-1][0]) - math.log(h[-2][0]))
            if est > 3:
                slope = min(60, est)
        if abs(gaps[m]) < 4:
            continue
        step = max(-0.7, min(0.7, -gaps[m] / slope))
        mult[m] = max(0.2, min(10.0, mult[m] * math.exp(step)))
log(f'final multipliers {json.dumps({m: round(v, 3) for m, v in mult.items()})}')
