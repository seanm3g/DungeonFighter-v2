import json

path = r"C:\Users\maxwe\Documents\GitHub\DungeonFighter-v2\GameData\Actions.json"
with open(path, encoding="utf-8") as f:
    data = json.load(f)

existing = {a.get("action", "").upper() for a in data}
new_actions = [
    {
        "action": "MIGHTY SWING",
        "description": "110% finisher. Gains damage from skipped/disabled strip slots.",
        "rarity": "RARE",
        "tier": 2,
        "dps": "110%",
        "numberOfHits": "1",
        "damage": "110%",
        "speed": "1.20",
        "tags": "class, barbarian, finisher, bludgeon",
    },
    {
        "action": "WARCRY",
        "description": "Apply Weaken. Next miss salvage for the fight.",
        "rarity": "COMMON",
        "tier": 2,
        "dps": "40%",
        "numberOfHits": "1",
        "damage": "40%",
        "speed": "0.80",
        "weaken": "1",
        "tags": "class, barbarian",
        "triggerBundlesJson": '[{"when":"ONCONNECT","count":"1","scope":"FIGHT","mechanics":"salvage_miss","value":null,"filters":null,"IsEnabled":true}]',
    },
    {
        "action": "BARBARIAN RAGE",
        "description": "Next 2 actions gain damage and speed. Kill retriggers opener.",
        "rarity": "RARE",
        "tier": 4,
        "dps": "100%",
        "numberOfHits": "1",
        "damage": "100%",
        "speed": "1.00",
        "speedMod": "25",
        "damageMod": "25",
        "tags": "class, barbarian",
        "cadenceBundlesJson": '[{"cadence":"ACTION","enable":"1","duration":"2","mechanics":"hero_next_action_damage, hero_next_action_speed","IsEnabled":true}]',
        "triggerBundlesJson": '[{"when":"ONKILL","count":"1","scope":"","mechanics":"retrigger_opener","value":null,"filters":null,"IsEnabled":true}]',
    },
    {
        "action": "CHALLENGE",
        "description": "Mark and Weaken the enemy.",
        "rarity": "COMMON",
        "tier": 1,
        "dps": "30%",
        "numberOfHits": "1",
        "damage": "30%",
        "speed": "0.70",
        "weaken": "1",
        "tags": "class, warrior",
    },
    {
        "action": "MEASURED CUT",
        "description": "90% Sword attack that banks unused armor into the next action.",
        "rarity": "COMMON",
        "tier": 2,
        "dps": "90%",
        "numberOfHits": "1",
        "damage": "90%",
        "speed": "1.00",
        "tags": "class, warrior",
    },
    {
        "action": "MISDIRECT",
        "description": "Low damage. On connect, replace next strip action.",
        "rarity": "COMMON",
        "tier": 2,
        "dps": "40%",
        "numberOfHits": "1",
        "damage": "40%",
        "speed": "0.80",
        "tags": "class, rogue",
        "triggerBundlesJson": '[{"when":"ONCONNECT","count":"1","scope":"","mechanics":"strip_replace_next:STAB","value":null,"filters":null,"IsEnabled":true}]',
    },
    {
        "action": "LOADED DICE",
        "description": "Replace next natural roll with 12.",
        "rarity": "RARE",
        "tier": 2,
        "dps": "50%",
        "numberOfHits": "1",
        "damage": "50%",
        "speed": "0.90",
        "tags": "class, rogue",
        "triggerBundlesJson": '[{"when":"ONCONNECT","count":"1","scope":"","mechanics":"replace_next_roll:12","value":null,"filters":null,"IsEnabled":true}]',
    },
    {
        "action": "ECHO SPELL",
        "description": "On Combo, retrigger next strip slot.",
        "rarity": "RARE",
        "tier": 2,
        "dps": "70%",
        "numberOfHits": "1",
        "damage": "70%",
        "speed": "1.00",
        "tags": "class, wizard",
        "triggerBundlesJson": '[{"when":"ONCOMBO","count":"1","scope":"","mechanics":"retrigger_next","value":null,"filters":null,"IsEnabled":true}]',
    },
    {
        "action": "REWRITE FATE",
        "description": "Spend Theorems to replace the next natural roll.",
        "rarity": "RARE",
        "tier": 2,
        "dps": "40%",
        "numberOfHits": "1",
        "damage": "40%",
        "speed": "0.90",
        "tags": "class, wizard",
    },
    {
        "action": "CHANNEL",
        "description": "Bank +50% AMP and -20% speed onto the next action.",
        "rarity": "COMMON",
        "tier": 3,
        "dps": "20%",
        "numberOfHits": "1",
        "damage": "20%",
        "speed": "1.20",
        "ampMod": "50",
        "speedMod": "-20",
        "tags": "class, wizard",
        "cadenceBundlesJson": '[{"cadence":"ACTION","enable":"1","duration":"1","mechanics":"hero_next_action_amp, hero_next_action_speed","IsEnabled":true}]',
    },
]

added = 0
for action in new_actions:
    if action["action"].upper() not in existing:
        data.append(action)
        added += 1
        existing.add(action["action"].upper())

with open(path, "w", encoding="utf-8") as f:
    json.dump(data, f, indent=2, ensure_ascii=False)

print(f"added {added}; total {len(data)}")
