#!/usr/bin/env python3
"""Which rules actually fire in real dungeons: one-rule-engine trace (RuleEngine.Fired) from DUNGEONSIM fight rows.

  python Scripts/rule-fire-report.py RAW_DIR [RAW_DIR...] [--json out.json] [--top 25]

RAW_DIR is a dungeon-sim.py --keep folder (fight rows carry RuleFires / RuleChained / RuleChainsStopped since
2026-11-21). Prints, per class and owner kind (Action, Item, Set, Skill, Enemy, Room):
  - fires per fight and the share of fights with any fire
  - chained fires (generation 1+) and chains stopped past RuleEngine.MaxGeneration
  - top rules by fires per fight
  - learned skills that never fire a triggered rule (passive-only nodes are expected here; triggered ones are suspects)
  - strip actions with card rules that never fire
"""
import argparse, glob, json, os, sys
from collections import Counter, defaultdict

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))


def skill_index():
    try:
        trees = json.load(open(os.path.join(ROOT, 'GameData', 'SkillTrees.json'), encoding='utf-8'))['trees']
    except Exception:
        return {}
    out = {}
    for t in trees:
        for n in t.get('nodes', []):
            out[n['id']] = n
    return out


def fights_from(paths):
    for d in paths:
        files = glob.glob(os.path.join(d, '**', '*.json'), recursive=True) if os.path.isdir(d) else [d]
        for f in files:
            try:
                data = json.load(open(f, encoding='utf-8'))
            except Exception:
                continue
            rows = data if isinstance(data, list) else [data]
            for r in rows:
                if not isinstance(r, dict):
                    continue
                for fight in r.get('Fights', []) or []:
                    if 'RuleFires' in fight:
                        yield r.get('Level') or fight.get('EnemyLevel'), fight


def main():
    ap = argparse.ArgumentParser()
    ap.add_argument('dirs', nargs='+')
    ap.add_argument('--json', default='')
    ap.add_argument('--top', type=int, default=25)
    A = ap.parse_args()
    skills = skill_index()
    name_of = {k: v.get('name', k) for k, v in skills.items()}

    n_fights = Counter()
    kind_fires = defaultdict(Counter)          # cls -> kind -> fires
    kind_fights = defaultdict(Counter)         # cls -> kind -> fights with any fire
    rule_fires = Counter()                     # (kind, rule) -> fires
    rule_owner = defaultdict(Counter)          # (kind, rule) -> owner counter
    chained = Counter(); stopped = Counter()
    skill_learned = Counter(); skill_fired = Counter()   # skill name -> fights learned / fights fired
    action_slotted = Counter(); action_fired = Counter()
    action_has_rule = set()
    for _, f in fights_from(A.dirs):
        cls = f.get('Class', '?')
        for c in (cls, 'all'):
            n_fights[c] += 1
        fires = f.get('RuleFires') or {}
        kinds_here = set()
        owners_here = set()
        for key, n in fires.items():
            kind, owner, rule = (key.split('|', 2) + ['', ''])[:3]
            kinds_here.add(kind)
            owners_here.add((kind, owner))
            for c in (cls, 'all'):
                kind_fires[c][kind] += n
            rule_fires[(kind, rule)] += n
            rule_owner[(kind, rule)][owner] += 1
            if kind == 'Action':
                action_has_rule.add(owner.upper())
        for c in (cls, 'all'):
            for k in kinds_here:
                kind_fights[c][k] += 1
            chained[c] += f.get('RuleChained', 0) or 0
            stopped[c] += f.get('RuleChainsStopped', 0) or 0
        for sid in (f.get('Skills') or {}):
            nm = name_of.get(sid, sid)
            skill_learned[nm] += 1
            if ('Skill', nm) in owners_here:
                skill_fired[nm] += 1
        for a in set(x.upper() for x in (f.get('Strip') or [])):
            action_slotted[a] += 1
            if ('Action', a) in {(k, o.upper()) for k, o in owners_here}:
                action_fired[a] += 1

    if not n_fights:
        print('no fights with RuleFires (shards older than 2026-11-21 or no --keep)')
        return 1
    kinds = ['Action', 'Item', 'Set', 'Skill', 'Enemy', 'Room']
    print(f"fights: {dict(n_fights)}")
    print(f"{'class':10} " + ' '.join(f'{k:>14}' for k in kinds) + '   chained/f  stopped/f')
    for c in sorted(n_fights, key=lambda x: (x == 'all', x)):
        nf = n_fights[c]
        cells = [f"{kind_fires[c][k] / nf:6.2f} ({100 * kind_fights[c][k] / nf:3.0f}%)" for k in kinds]
        print(f"{c:10} " + ' '.join(f'{x:>14}' for x in cells) + f"   {chained[c] / nf:8.3f}  {stopped[c] / nf:8.4f}")
    print('\n(fires per fight, share of fights with any fire)\n')
    nf = n_fights['all']
    print(f'top {A.top} rules by fires per fight:')
    for (kind, rule), n in rule_fires.most_common(A.top):
        owners = ', '.join(o for o, _ in rule_owner[(kind, rule)].most_common(2))
        print(f"  {n / nf:7.3f}  {kind:6} {rule[:70]:70}  [{owners[:50]}]")
    rare = [(k, r, n) for (k, r), n in rule_fires.items() if n / nf < 0.002]
    print(f'\nrules that fired at all but under 0.2% of fights: {len(rare)}')
    rule_text = {v.get('name', k): (v.get('crossWire') or '') for k, v in skills.items()}

    def triggered(nm):
        parts = [p.strip() for p in rule_text.get(nm, '').split(';') if p.strip()]
        return any(not p.upper().startswith('WHILE') and 'COUNTAS' not in p.upper() for p in parts)

    print('\nSUSPECTS: learned skills (>= 20 fights) with a triggered rule that never fired:')
    silent = sorted((nm for nm, n in skill_learned.items() if n >= 20 and skill_fired[nm] == 0), key=lambda x: -skill_learned[x])
    for nm in silent:
        if triggered(nm):
            print(f"  {nm:30} learned in {skill_learned[nm]:5} fights   {rule_text[nm][:80]}")
    print('(passive-only or coded nodes, silent by design: ' + ', '.join(nm for nm in silent if not triggered(nm))[:700] + ')')
    print('\nlearned skills that fire in under 25% of the fights they are learned in (>= 20 fights):')
    for nm, n in sorted(skill_learned.items(), key=lambda kv: -kv[1]):
        if n >= 20 and 0 < skill_fired[nm] / n < 0.25:
            print(f"  {nm:34} {100 * skill_fired[nm] / n:5.1f}% of {n}")
    print('\nstrip actions with card rules that fire in under 25% of the fights they are slotted in (>= 20 fights):')
    for a, n in sorted(action_slotted.items(), key=lambda kv: -kv[1]):
        if a in action_has_rule and n >= 20 and action_fired[a] / n < 0.25:
            print(f"  {a:34} {100 * action_fired[a] / n:5.1f}% of {n}")
    if A.json:
        json.dump({
            'fights': n_fights,
            'kindFiresPerFight': {c: {k: kind_fires[c][k] / n_fights[c] for k in kinds} for c in n_fights},
            'kindFightShare': {c: {k: kind_fights[c][k] / n_fights[c] for k in kinds} for c in n_fights},
            'chainedPerFight': {c: chained[c] / n_fights[c] for c in n_fights},
            'stoppedPerFight': {c: stopped[c] / n_fights[c] for c in n_fights},
            'rules': [{'kind': k, 'rule': r, 'firesPerFight': n / nf, 'owners': dict(rule_owner[(k, r)].most_common(5))}
                      for (k, r), n in rule_fires.most_common()],
            'skills': {nm: {'learnedFights': n, 'firedFights': skill_fired[nm]} for nm, n in skill_learned.items()},
            'actions': {a: {'slottedFights': n, 'firedFights': action_fired[a], 'hasRule': a in action_has_rule}
                        for a, n in action_slotted.items()},
        }, open(A.json, 'w', encoding='utf-8'), indent=1)
    return 0


if __name__ == '__main__':
    sys.exit(main())
