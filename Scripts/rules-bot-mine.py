#!/usr/bin/env python3
"""Mine the rules bot's priority lists from smart-bot DUNGEONSIM shards (dungeon-sim.py --keep folders).

The rules bot (DUNGEONSIM --bot rules) makes no simulated decisions: it learns skills in the order the smart bot
tends to learn them, fills its strip from the actions the smart bot tends to run, and builds class sets by rule.
Re-mine after a change that moves smart-bot picks (skill tree, actions, classes), then re-check the pairing:

  python Scripts/dungeon-sim.py --bot smart --levels 20,35,50,75 --keep <dir>   (smart bot, writes shard rows)
  python Scripts/rules-bot-mine.py <dir> [<dir> ...]                        (writes GameData/TuningProfiles/rules-bot.json)
"""
import argparse, collections, glob, json, os, statistics

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
ap = argparse.ArgumentParser()
ap.add_argument('dirs', nargs='+', help='folders of DUNGEONSIM shard JSON (smart bot)')
ap.add_argument('--out', default=os.path.join(ROOT, 'GameData', 'TuningProfiles', 'rules-bot.json'))
ap.add_argument('--min-share', type=float, default=0.25, help='skill picks made in fewer runs than this go last')
A = ap.parse_args()

skill_pos = collections.defaultdict(lambda: collections.defaultdict(list))   # class -> token -> [normalized index]
skill_runs = collections.Counter()
action_uses = collections.defaultdict(collections.Counter)                   # class -> action -> strips holding it
action_pos = collections.defaultdict(lambda: collections.defaultdict(list))
fill = collections.defaultdict(list)                                         # class -> strip length / slot max
set_uses = collections.defaultdict(collections.Counter)                      # class -> worn set material (2+ pieces)
sources = []
def mine_strip(cls, r, strip):
    if r.get('SlotMax'):
        fill[cls].append(len(strip) / r['SlotMax'])
    worn = {m: n for m, n in (r.get('Sets') or {}).items() if n >= 2}
    if worn:
        set_uses[cls][max(worn, key=worn.get)] += 1
    for i, a in enumerate(strip):
        action_uses[cls][a] += 1
        action_pos[cls][a].append(i / max(1, len(strip)))


for d in A.dirs:
    for f in sorted(glob.glob(os.path.join(d, '*.json'))):
        data = json.load(open(f, encoding='utf-8-sig'))
        if data.get('Bot') != 'smart':
            continue
        sources.append(os.path.basename(f))
        for m in data.get('Milestones', []):
            learned = m.get('SkillsLearned') or []
            if len(learned) < 2:
                continue
            cls = m.get('Class') or data['Outcomes'][0]['Class']
            skill_runs[cls] += 1
            seen = collections.Counter()
            for i, node in enumerate(learned):
                seen[node] += 1
                skill_pos[cls][f'{node}#{seen[node]}'].append(i / len(learned))
        for r in data.get('Fights', []):
            if r.get('FightInDungeon') != 1:
                continue
            strip = r.get('Strip') or []
            # Strip picks shift with level: mine per class and per "Class@Level" (the bot uses the closest level).
            for cls in (r['Class'], f"{r['Class']}@{data.get('Level', 0)}"):
                mine_strip(cls, r, strip)

# Rule knobs, picked 2026-11-01 against smart-bot runs at L20/35/50/75 (overall clear within 8 points): full strips,
# per-level strip lists, home-class sets first. Mined set preference and filling with unlisted actions made it worse.
KNOBS = {'stripFillOverride': 1.0, 'perLevelStrips': 1, 'homeSetFirst': 1, 'useSetPreference': 0, 'fillUnlisted': 0,
         'setMinValueShare': 0}
out = {'description': 'Rules-bot priority lists mined from smart-bot DUNGEONSIM runs by Scripts/rules-bot-mine.py.',
       'sources': len(sources), **KNOBS,
       'skillOrder': {}, 'actionPriority': {}, 'actionOrder': {}, 'stripFill': {}, 'setPreference': {}}
for cls, tokens in skill_pos.items():
    runs = skill_runs[cls]
    common = [t for t, v in tokens.items() if len(v) >= A.min_share * runs]
    rare = [t for t, v in tokens.items() if len(v) < A.min_share * runs]
    order = sorted(common, key=lambda t: statistics.mean(tokens[t])) + sorted(rare, key=lambda t: -len(tokens[t]))
    out['skillOrder'][cls] = [t.split('#')[0] for t in order]
for cls, uses in action_uses.items():
    out['actionPriority'][cls] = [a for a, _ in uses.most_common()]
    out['actionOrder'][cls] = {a: round(statistics.mean(v), 3) for a, v in action_pos[cls].items()}
    out['setPreference'][cls] = [m for m, _ in set_uses[cls].most_common()]
    out['stripFill'][cls] = round(statistics.median(fill[cls]), 3) if fill[cls] else 1.0
json.dump(out, open(A.out, 'w', encoding='utf-8'), indent=2)
print(f'{len(sources)} shard files -> {A.out}')
for cls in sorted(out['skillOrder']):  # per-level strip keys (Class@L) are in actionPriority only
    print(f"  {cls}: {len(out['skillOrder'][cls])} skill picks, top actions {out['actionPriority'].get(cls, [])[:6]}, fill {out['stripFill'].get(cls)}")
