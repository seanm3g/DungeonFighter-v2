#!/usr/bin/env python3
"""Normalize skill costs to 1 SP and pad each tree to >=99 max SP sink."""
from __future__ import annotations

import json
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
PATH = ROOT / "GameData" / "SkillTrees.json"
TARGET_SP = 99

# Filler flavor packs: (name_stem, branch, tier, requires_key_hint) per class
FILLERS = {
    "Barbarian": [
        ("Overhand", "Impact", 1, "b-root"),
        ("Ribbreaker", "Impact", 3, "b-blood"),
        ("Stampede", "Impact", 4, "b-mastery"),
        ("Copper Vein", "Alloy", 1, "b-root"),
        ("Tempered Core", "Alloy", 3, "b-temper"),
        ("Ore Pulse", "Alloy", 4, "b-age3"),
        ("Grit", "Instinct", 2, "b-gut"),
        ("Scar Tissue", "Instinct", 3, "b-memory"),
        ("Battle Breath", "Instinct", 4, "b-memory"),
        ("Crush Cadence", "Impact", 3, "b-venom"),
        ("Bronze Nail", "Alloy", 2, "b-age1"),
        ("War Drum", "Instinct", 1, "b-root"),
    ],
    "Warrior": [
        ("Parry Drill", "Guard", 1, "w-root"),
        ("Shield Bite", "Guard", 3, "w-duty"),
        ("Bulwark Nest", "Guard", 4, "w-bulwark"),
        ("Metronome", "Tempo", 1, "w-root"),
        ("Long Step", "Tempo", 3, "w-march"),
        ("Tempo Bleed", "Tempo", 4, "w-flow"),
        ("Order Cut", "Command", 2, "w-opening"),
        ("Formation", "Command", 3, "w-mastery"),
        ("Banner Pulse", "Command", 1, "w-root"),
        ("Brace Line", "Guard", 2, "w-riposte"),
        ("Cadence Edge", "Tempo", 2, "w-cadence"),
        ("Rally Spark", "Command", 4, "w-mastery"),
    ],
    "Rogue": [
        ("Needlepoint", "Venom", 1, "r-root"),
        ("Sepsis", "Venom", 3, "r-cut"),
        ("Black Dose", "Venom", 4, "r-ledger"),
        ("Sidestep", "Evasion", 1, "r-root"),
        ("Afterimage", "Evasion", 3, "r-debt"),
        ("Vanish Cut", "Evasion", 2, "r-ghost"),
        ("Deadeye", "Precision", 1, "r-root"),
        ("Cold Count", "Precision", 3, "r-fingers"),
        ("Clockwork", "Precision", 4, "r-mastery"),
        ("Venom Tip", "Venom", 2, "r-edge"),
        ("Soft Sole", "Evasion", 4, "r-witness"),
        ("Keen Edge", "Precision", 2, "r-snake"),
    ],
    "Wizard": [
        ("Ember Note", "Elements", 1, "z-root"),
        ("Ash Cycle", "Elements", 3, "z-bleed"),
        ("Flux Burn", "Elements", 4, "z-catalyst"),
        ("Echo Trace", "Echo", 1, "z-root"),
        ("Resonance", "Echo", 3, "z-lens"),
        ("Aftertone", "Echo", 2, "z-memory"),
        ("Odds Taker", "Probability", 1, "z-root"),
        ("Bias Die", "Probability", 3, "z-analysis"),
        ("Lattice", "Probability", 2, "z-prime"),
        ("Spark Weave", "Elements", 2, "z-kindle"),
        ("Mirror Cadence", "Echo", 4, "z-mastery"),
        ("Bayes Hint", "Probability", 4, "z-analysis"),
    ],
}

PREFIX = {
    "Barbarian": "b",
    "Warrior": "w",
    "Rogue": "r",
    "Wizard": "z",
}


def is_action(n: dict) -> bool:
    return (n.get("type") or "").lower() == "action" or bool(n.get("unlockActionName"))


def max_sink(nodes: list[dict]) -> int:
    total = 0
    for n in nodes:
        cost = 0 if n.get("tier", 0) == 0 else 1
        if n.get("tier", 0) == 0:
            max_r = 1
        elif is_action(n):
            max_r = 1
        else:
            max_r = int(n.get("maxRank") or 5)
            if max_r <= 0:
                max_r = 5
        total += cost * max_r
    return total


def main() -> None:
    data = json.loads(PATH.read_text(encoding="utf-8"))
    for tree in data["trees"]:
        key = tree["classKey"]
        for n in tree["nodes"]:
            n["cost"] = 0 if n.get("tier", 0) == 0 else 1
            # refresh effect text mentions of old PP costs is optional

        sink = max_sink(tree["nodes"])
        existing_ids = {n["id"] for n in tree["nodes"]}
        prefix = PREFIX[key]
        packs = FILLERS[key]
        idx = 0
        pack_i = 0
        while sink < TARGET_SP and pack_i < len(packs) * 3:
            name, branch, tier, req = packs[pack_i % len(packs)]
            pack_i += 1
            idx += 1
            nid = f"{prefix}-pack{idx}"
            if nid in existing_ids:
                continue
            # ensure require exists
            if req not in existing_ids:
                # fall back to root
                req = f"{prefix}-root"
            node = {
                "id": nid,
                "name": f"{name}",
                "branch": branch,
                "tier": tier,
                "type": "Passive",
                "requires": [req],
                "effect": f"+1% damage per rank on hit (stacks).",
                "payoff": "Skill Point sink with steady combat payoff.",
                "customEffectId": nid,
                "damageModPerRank": 1,
                "cost": 1,
                "maxRank": 5,
            }
            # Alternate every other pack to heal to diversify
            if idx % 3 == 0:
                node["effect"] = f"On hit, heal 1 HP per rank."
                node["payoff"] = "Sustain sink for long dungeon runs."
                node["healOnHitPerRank"] = 1
                del node["damageModPerRank"]
            tree["nodes"].append(node)
            existing_ids.add(nid)
            sink = max_sink(tree["nodes"])

        print(f"{key}: {len(tree['nodes'])} nodes, max SP sink={max_sink(tree['nodes'])}")

    PATH.write_text(json.dumps(data, indent=2, ensure_ascii=False) + "\n", encoding="utf-8")
    print("wrote", PATH)


if __name__ == "__main__":
    main()
