#!/usr/bin/env python3
"""Adjective-prefix value solve: scale each prefix's rolled value (Modifications.json Min/MaxValue) so a hero whose every
piece carries it (DUNGEONSIM --affix mod:X) gains about --gain clear points over an unforced run on the same seed,
averaged over the test levels and all classes. Data is tested through --overlay (repo untouched until --write).

  python Scripts/affix-solve.py --out affix-v1 --rounds 3 [--start start.json] [--write]

Writes GameData/TuningRuns/affix-solve-<out>/ (log, round JSON, overlay folders); --write copies the final values into
GameData/Modifications.json.
"""
import argparse, json, math, os, shutil, subprocess, sys, time

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
CLASSES = ['Barbarian', 'Warrior', 'Rogue', 'Wizard']

ap = argparse.ArgumentParser()
ap.add_argument('--out', default='affix-v1')
ap.add_argument('--rounds', type=int, default=3)
ap.add_argument('--levels', default='18,38,58')
ap.add_argument('--shards', type=int, default=8)
ap.add_argument('--seed', type=int, default=9500)
ap.add_argument('--spark', default='10')
ap.add_argument('--parallel', type=int, default=12)
ap.add_argument('--tools', default='Code/bin/AffixTools')
ap.add_argument('--patch', default='')
ap.add_argument('--gain', type=float, default=8.0, help='target clear gain (points) of a full forced set over no forcing')
ap.add_argument('--slope', type=float, default=10.0, help='prior gain points per e-fold of the value')
ap.add_argument('--mods', default='Light,Acrobatic,Featherweight,Nimble,Heavy,Brutal,Keen,refined,Charged,Long,Reinforced,Hardened,Ancient,Sturdy,Flaming,serrated,Poisonous')
ap.add_argument('--start', default='', help='JSON {mod: value} starting values (default: current file values)')
ap.add_argument('--write', action='store_true')
A = ap.parse_args()

MODS = A.mods.split(',')
OUTDIR = os.path.join(ROOT, 'GameData', 'TuningRuns', f'affix-solve-{A.out}')
os.makedirs(OUTDIR, exist_ok=True)
LOG = open(os.path.join(OUTDIR, 'log.txt'), 'a', encoding='utf-8')
SRC = json.load(open(os.path.join(ROOT, 'GameData', 'Modifications.json'), encoding='utf-8-sig'))


def log(msg):
    line = f'[{time.strftime("%H:%M:%S")}] {msg}'
    print(line, flush=True)
    LOG.write(line + '\n')
    LOG.flush()


values = {m['Name']: m['MaxValue'] for m in SRC if m['Name'] in MODS}
if A.start:
    values.update(json.load(open(A.start, encoding='utf-8')))


def overlay(vals, rnd):
    d = os.path.join(OUTDIR, f'overlay-r{rnd}')
    os.makedirs(d, exist_ok=True)
    rows = json.loads(json.dumps(SRC))
    for m in rows:
        if m['Name'] in vals:
            m['MinValue'] = m['MaxValue'] = round(vals[m['Name']], 4)
    json.dump(rows, open(os.path.join(d, 'Modifications.json'), 'w', encoding='utf-8'), indent=2)
    return d


def run(tag, rnd, ov, seed, affix=None):
    out = os.path.join(OUTDIR, f'r{rnd}-{tag}.json')
    cmd = [sys.executable, 'Scripts/dungeon-sim.py', '--levels', A.levels, '--shards', str(A.shards), '--seed', str(seed),
           '--spark', A.spark, '--parallel', str(A.parallel), '--tools', A.tools, '--overlay', ov, '--json', out]
    if affix:
        cmd += ['--affix', affix]
    if A.patch:
        cmd += ['--patch', A.patch]
    with open(out[:-5] + '.txt', 'w', encoding='utf-8') as fh:
        subprocess.run(cmd, cwd=ROOT, stdout=fh, stderr=subprocess.STDOUT, env={**os.environ, 'DF_SPARK_APP_PER_HASH': '1'})
    rows = json.load(open(out, encoding='utf-8'))
    return {r['level']: {c: r['perClassClear'].get(c) for c in CLASSES} for r in rows}


def gain(per, base):
    diffs = [100 * (per[L][c] - base[L][c]) for L in base for c in CLASSES
             if per.get(L, {}).get(c) is not None and base[L].get(c) is not None]
    return sum(diffs) / len(diffs) if diffs else 0.0


history = {m: [] for m in MODS}
log(f'start values {values}')
for rnd in range(1, A.rounds + 1):
    confirm = rnd == A.rounds
    ov = overlay(values, rnd)
    seed = A.seed + 41 * rnd
    base = run('none', rnd, ov, seed)
    gains = {}
    for m in MODS:
        per = run(m, rnd, ov, seed, f'mod:{m}')
        gains[m] = gain(per, base)
        history[m].append((values[m], gains[m]))
        by_level = '  '.join(f'L{L} {100 * sum(per[L][c] - base[L][c] for c in CLASSES) / 4:+5.1f}' for L in sorted(base))
        log(f'r{rnd} {m:<14} v={values[m]:<8.4g} gain {gains[m]:+5.1f}   {by_level}')
    json.dump({'round': rnd, 'values': values, 'gains': gains, 'history': history},
              open(os.path.join(OUTDIR, f'round{rnd}.json'), 'w', encoding='utf-8'), indent=1)
    rms = math.sqrt(sum((g - A.gain) ** 2 for g in gains.values()) / len(gains))
    log(f'r{rnd} rms miss vs +{A.gain}: {rms:.1f}')
    if confirm:
        break
    for m in MODS:
        h = history[m]
        slope = A.slope
        if len(h) >= 2 and h[-1][0] > 0 and h[-2][0] > 0 and abs(math.log(h[-1][0] / h[-2][0])) > 0.05:
            est = (h[-1][1] - h[-2][1]) / math.log(h[-1][0] / h[-2][0])
            if est > 2:
                slope = min(40, est)
        miss = gains[m] - A.gain
        if abs(miss) < 2.5:
            continue
        values[m] = max(1e-3, values[m] * math.exp(max(-1.2, min(1.2, -miss / slope))))
log(f'final values {json.dumps({m: round(v, 4) for m, v in values.items()})}')
if A.write:
    path = os.path.join(ROOT, 'GameData', 'Modifications.json')
    rows = json.load(open(path, encoding='utf-8-sig'))
    for m in rows:
        if m['Name'] in values:
            m['MinValue'] = m['MaxValue'] = round(values[m['Name']], 4)
    json.dump(rows, open(path, 'w', encoding='utf-8'), indent=2)
    log('wrote GameData/Modifications.json')
