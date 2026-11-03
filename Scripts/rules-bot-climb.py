#!/usr/bin/env python3
"""Hill-climb the rules bot (GameData/TuningProfiles/rules-bot.json) with paired-seed DUNGEONSIM runs; no smart bot.

Each step picks one edit, runs only the class it touches on fixed seed A, and keeps it if clear rate rises by MIN_GAIN
there and does not drop on seed B. Edits: --skills moves a skill's ranks (front / after the core picks / middle / end of
the class's learn order; scored at L20+L50); otherwise a Class@Level strip list gets promote / drop / insert-owned-action
/ strip-length edits. State lives in <work>/climb-state.json, so runs resume; the best file is <work>/climb-best.json.
Checked 2026-11-02: skill moves gained ~3 clear points per class; strip edits were noise. Validate the result on the
default seed (a full dungeon-sim pass) before copying it over rules-bot.json.

  python Scripts/rules-bot-climb.py --work <dir> --skills --budget 3600
  python Scripts/rules-bot-climb.py --work <dir> --explore <keep dir> --budget 3600   (strip edits; owned actions from
                                                                                         dungeon-sim.py --explore-strips --keep)
"""
import argparse, collections, glob, json, os, random, subprocess, sys, time

REPO = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
SEEDS = (11, 23)
MIN_GAIN = 0.02
LEVELS = [10, 15, 20, 35, 50, 75]
CLASSES = ['Barbarian', 'Warrior', 'Rogue', 'Wizard']

ap = argparse.ArgumentParser()
ap.add_argument('--work', required=True, help='folder for state, best file, log and sim scratch')
ap.add_argument('--explore', nargs='*', default=[], help='--explore-strips keep folders (owned-action pool for strip edits)')
ap.add_argument('--budget', type=int, default=560, help='seconds to run before saving and exiting')
ap.add_argument('--skills', action='store_true', help='climb skill orders (scored at --skill-levels) instead of strip lists')
ap.add_argument('--skill-levels', default='20,50', help='levels that score skill-order moves (default 20,50)')
ap.add_argument('--classes', default=','.join(CLASSES), help='classes to edit (default all)')
ap.add_argument('--patch', help='balance patch JSON to run against (default: the active patch)')
A = ap.parse_args()
SP = os.path.abspath(A.work)
os.makedirs(os.path.join(SP, 'runs'), exist_ok=True)
STATE = os.path.join(SP, 'climb-state.json')
BEST = os.path.join(SP, 'climb-best.json')
LOG = os.path.join(SP, 'climb.log')
SKILLS_ONLY = A.skills


def log(msg):
    line = time.strftime('%H:%M:%S ') + msg
    print(line, flush=True)
    open(LOG, 'a', encoding='utf-8').write(line + '\n')


def owned_pool():
    """Commonly owned actions per Class@Level, from the explore runs' fight rows."""
    pool = collections.defaultdict(collections.Counter)
    n = collections.Counter()
    for d in A.explore:
        for path in glob.glob(os.path.join(d, '*.json')):
            doc = json.load(open(path, encoding='utf-8-sig'))
            for f in doc['Fights'][::5]:
                key = f"{f['Class']}@{doc['Level']}"
                n[key] += 1
                pool[key].update(set(f.get('OwnedActions', [])))
    return {k: [a for a, c in pool[k].most_common() if c >= 0.15 * n[k]] for k in pool}


if os.path.exists(STATE):
    S = json.load(open(STATE))
else:
    S = {'data': json.load(open(os.path.join(REPO, 'GameData', 'TuningProfiles', 'rules-bot.json'), encoding='utf-8-sig')),
         'base': {}, 'tried': [], 'accepted': [], 'evals': 0, 'pool': owned_pool()}


SKILL_LEVELS = A.skill_levels
CLASSES = A.classes.split(',')


def evaluate(data, cls, level, seed):
    ov = os.path.join(SP, 'ovb-climb')
    os.makedirs(os.path.join(ov, 'TuningProfiles'), exist_ok=True)
    json.dump(data, open(os.path.join(ov, 'TuningProfiles', 'rules-bot.json'), 'w'), indent=1)
    out = os.path.join(SP, 'runs', 'climb-eval.json')
    subprocess.run([sys.executable, 'Scripts/dungeon-sim.py', '--tools', 'Code/bin/Debug/net8.0', '--levels', str(level),
                    '--classes', cls, '--runs', '16', '--seed', str(seed), '--overlay', ov, '--json', out] + (['--patch', os.path.abspath(A.patch)] if A.patch else []),
                   cwd=REPO, stdout=subprocess.DEVNULL, stderr=subprocess.DEVNULL, check=True)
    S['evals'] += 1
    rows = json.load(open(out))
    return sum(r['perClassClear'][cls] for r in rows) / len(rows)


def base(cls, level, seed):
    k = f'{cls}@{level}#{seed}'
    if k not in S['base']:
        S['base'][k] = evaluate(S['data'], cls, level, seed)
    return S['base'][k]


def candidates(key):
    if key.startswith('skill:'):
        order = S['data']['skillOrder'][key[6:]]
        tokens = list(dict.fromkeys(order))
        return ([('rush', t) for t in tokens[4:]] + [('defer', t) for t in tokens[1:]]
                + [('first', t) for t in tokens[1:]] + [('mid', t) for t in tokens[1:]])
    cur = S['data']['actionPriority'].get(key, [])
    owned = set(S['pool'].get(key, []))
    mine = [a for a in cur if a in owned]   # moves on actions the class never owns change nothing
    out = []
    for a in mine[1:8]:
        out.append(('promote', a))
    for a in mine[:4]:
        out.append(('drop', a))
    for a in S['pool'].get(key, [])[:12]:
        if a not in cur[:3]:
            out.append(('insert', a))
    for n in (1, 3, 4):
        out.append(('length', n))
    return out


def apply(data, key, move):
    d = json.loads(json.dumps(data))
    op, x = move
    if key.startswith('skill:'):
        order = d['skillOrder'][key[6:]]
        rest = [t for t in order if t != x]
        mine = [t for t in order if t == x]
        cut = {'rush': 4, 'first': 0, 'mid': 12}.get(op)
        d['skillOrder'][key[6:]] = rest + mine if cut is None else rest[:cut] + mine + rest[cut:]
        return d
    cur = d['actionPriority'].setdefault(key, [])
    if op == 'promote':
        cur.remove(x); cur.insert(0, x)
    elif op == 'drop':
        cur.remove(x)
    elif op == 'insert':
        if x in cur:
            cur.remove(x)
        cur.insert(0, x)
    elif op == 'length':
        d.setdefault('stripLength', {})[key] = x
    return d


start = time.time()
rng = random.Random(S['evals'])
while time.time() - start < A.budget - 75:
    keys = [f'skill:{c}' for c in CLASSES] if SKILLS_ONLY else [f'{c}@{l}' for c in CLASSES for l in LEVELS]
    pending = [(k, m) for k in keys for m in candidates(k) if f'{k}|{m[0]}|{m[1]}' not in S['tried']]
    if not pending:
        log('search exhausted'); break
    key, move = rng.choice(pending)
    tag = f'{key}|{move[0]}|{move[1]}'
    S['tried'].append(tag)
    cls, level = (key[6:], SKILL_LEVELS) if key.startswith('skill:') else key.split('@')
    b0 = base(cls, level, SEEDS[0])
    trial = apply(S['data'], key, move)
    v0 = evaluate(trial, cls, level, SEEDS[0])
    if v0 - b0 < MIN_GAIN:
        log(f'reject {tag}: {b0:.3f} -> {v0:.3f}')
    else:
        b1 = base(cls, level, SEEDS[1])
        v1 = evaluate(trial, cls, level, SEEDS[1])
        if v1 - b1 >= 0:
            S['data'] = trial
            S['base'][f'{cls}@{level}#{SEEDS[0]}'] = v0
            S['base'][f'{cls}@{level}#{SEEDS[1]}'] = v1
            # Other edits to this key were judged against the old list: let them be tried again.
            S['tried'] = [t for t in S['tried'] if not t.startswith(key + '|') or t == tag]
            S['accepted'].append({'move': tag, 'seedA': [b0, v0], 'seedB': [b1, v1]})
            json.dump(S['data'], open(BEST, 'w'), indent=1)
            log(f'ACCEPT {tag}: A {b0:.3f}->{v0:.3f}  B {b1:.3f}->{v1:.3f}')
        else:
            log(f'reject {tag} on seed B: A {b0:.3f}->{v0:.3f}  B {b1:.3f}->{v1:.3f}')
    json.dump(S, open(STATE, 'w'))
log(f'chunk done: {S["evals"]} evals, {len(S["accepted"])} accepted, {len(S["tried"])} tried')
