#!/usr/bin/env python3
"""Run a sequence of balance-loop.py solves, each on the previous step's patch, then a held-out measure.

  python Scripts/balance-chain.py --start fastbot-v21 --first 22 --steps health,damage,class,damage --ref fastbot-v20

Patches are named fastbot-v<first>, v<first+1>, ...; a step that keeps nothing passes its input along. Each step gets
fresh diagnose / confirm seeds (base 8000 + 200*step), and the final held-out measure uses seeds no step saw.
"""
import argparse, json, os, subprocess, sys

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
ap = argparse.ArgumentParser()
ap.add_argument('--start', required=True)
ap.add_argument('--first', type=int, required=True, help='version number of the first patch the chain writes')
ap.add_argument('--steps', default='health,damage,class,damage')
ap.add_argument('--ref', default='', help='reference patch for the final held-out measure')
ap.add_argument('--run', default='chain')
ap.add_argument('--tools', default='Code/bin/BalanceTools')
ap.add_argument('--prefix', default='fastbot-v')
ap.add_argument('--seed-base', type=int, default=8000, help='diag/confirm seeds start here (+200 per step); change it for a second chain so no seed is reused')
ap.add_argument('--levels', default='', help='band midpoints for every step (balance-loop.py --levels; default its midpoints)')
ap.add_argument('--holdout-seeds', default='9901,9902,9903,9904')
ap.add_argument('--bot', default='synergy', help='DUNGEONSIM bot for every step (balance-loop.py --bot)')
A = ap.parse_args()

cur, ver = A.start, A.first
loop = [sys.executable, '-u', os.path.join(ROOT, 'Scripts', 'balance-loop.py')]
lv = ['--levels', A.levels] if A.levels else []
for i, knob in enumerate(A.steps.split(',')):
    nxt = f'{A.prefix}{ver}'
    b = A.seed_base + 200 * i
    diag = ','.join(str(b + k) for k in range(1, 5)); conf = ','.join(str(b + 100 + k) for k in range(1, 5))
    print(f'== step {i + 1}: {knob} on {cur} -> {nxt}', flush=True)
    subprocess.run(loop + ['solve'] + lv + ['--patch', cur, '--knob', knob, '--next', nxt, '--run', A.run, '--tools', A.tools, '--bot', A.bot,
                           '--diag-seeds', diag, '--confirm-seeds', conf, '--solve-seed', str(40 + 20 * i)], cwd=ROOT)
    res = os.path.join(ROOT, 'GameData', 'TuningRuns', 'balance-loop', A.run, f'solve-{knob}-{cur}.json')
    made = os.path.exists(os.path.join(ROOT, 'GameData', 'Patches', 'Balance', nxt + '.json'))
    if made:
        cur, ver = nxt, ver + 1
    print(f'   -> {"new patch " + cur if made else "no change, staying on " + cur}', flush=True)
args = loop + ['measure'] + lv + ['--patch', cur, '--seeds', A.holdout_seeds, '--run', A.run, '--tools', A.tools, '--bot', A.bot]
if A.ref:
    args += ['--ref', A.ref]
subprocess.run(args, cwd=ROOT)
subprocess.run(loop + ['botcheck'] + lv + ['--patch', cur, '--run', A.run, '--tools', A.tools, '--bot', A.bot, '--seeds', '9951,9952'], cwd=ROOT)
print(f'== chain done: final patch {cur}', flush=True)
