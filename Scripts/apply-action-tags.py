#!/usr/bin/env python3
"""Apply registry tags to GameData/Actions.json from category, class deck, and action flavor."""

from __future__ import annotations

import json
import re
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
ACTIONS_PATH = ROOT / "GameData" / "Actions.json"
CLASS_ACTIONS_PATH = ROOT / "GameData" / "ClassActions.json"

VALID_TAGS = frozenset(
    {
        "environment",
        "enemy",
        "weapon",
        "class",
        "unique",
        "starter",
        "modtrade",
        "warrior",
        "barbarian",
        "rogue",
        "wizard",
        "sword",
        "mace",
        "dagger",
        "wand",
        "common",
        "uncommon",
        "rare",
        "epic",
        "legendary",
        "mythic",
        "fire",
        "earth",
        "water",
        "air",
        "scorched",
        "flooded",
        "overgrown",
        "exposed",
    }
)

TAG_ORDER = [
    "environment",
    "enemy",
    "weapon",
    "class",
    "modtrade",
    "unique",
    "starter",
    "warrior",
    "barbarian",
    "rogue",
    "wizard",
    "sword",
    "mace",
    "dagger",
    "wand",
    "common",
    "uncommon",
    "rare",
    "epic",
    "legendary",
    "mythic",
    "fire",
    "earth",
    "water",
    "air",
    "scorched",
    "flooded",
    "overgrown",
    "exposed",
]

CLASS_BY_CATEGORY = {
    "BARBARIAN": "barbarian",
    "WARRIOR": "warrior",
    "ROGUE": "rogue",
    "WIZARD": "wizard",
}

# ClassActions.json classKey values that map to hero class pool tags (not weapon-path tier names).
PRIMARY_CLASS_KEYS = frozenset({"barbarian", "warrior", "rogue", "wizard"})

WEAPON_TYPE_BY_ACTION = {
    "STRIKE": "sword",
    "STAB": "dagger",
    "SLAM": "mace",
    "MAGIC MISSILE": "wand",
}

ENV_PREFIX_TAGS: dict[str, list[str]] = {
    "FOREST": ["overgrown"],
    "LAVA": ["fire", "scorched"],
    "CRYPT": ["exposed"],
    "CAVERN": ["earth", "exposed"],
    "SWAMP": ["water", "flooded"],
    "DESERT": ["earth", "scorched"],
    "ICE": ["water", "exposed"],
    "RUINS": ["exposed"],
    "CASTLE": ["earth", "exposed"],
    "GRAVEYARD": ["exposed"],
    "TREASURE": ["earth", "exposed"],
    "GUARD": ["earth", "exposed"],
    "TRAP": ["earth", "exposed"],
    "BOSS": ["earth", "exposed"],
    "ARMORY": ["earth", "exposed"],
    "KITCHEN": ["earth", "exposed"],
    "DINING": ["earth", "exposed"],
    "LIBRARY": ["earth", "exposed"],
    "STORAGE": ["earth", "exposed"],
    "REST": ["earth", "exposed"],
    "PUZZLE": ["earth", "exposed"],
    "CHAMBER": ["earth", "exposed"],
    "HALL": ["earth", "exposed"],
    "VAULT": ["earth", "exposed"],
    "SHRINE": ["exposed"],
    "SANCTUM": ["exposed"],
    "THRONE": ["earth", "exposed"],
    "CATACOMB": ["exposed"],
    "GROTTO": ["earth", "exposed"],
    "OBSERVATORY": ["air", "exposed"],
    "LABORATORY": ["earth", "exposed"],
    "GENERIC": ["exposed"],
}

FIRE_RE = re.compile(
    r"\b(fire|lava|magma|flame|burn|ignite|ember|ash|scorch|heat|solar|pyre|"
    r"volcanic|torch|sacred flame|meteor|hellfire|inferno|cinder)\b",
    re.I,
)
EARTH_RE = re.compile(
    r"\b(stone|rock|sand|dune|cave|cavern|crypt|tomb|burial|bone|earth|"
    r"crystal|geode|gear|clockwork|pillar|stalactite|rockfall|avalanche|"
    r"quake|mud|spike|portcullis|barricade|collapse|debris|ruin|castle|"
    r"armory|trap|guard|treasure|kitchen|dining|library|storage|mace|slam)\b",
    re.I,
)
WATER_RE = re.compile(
    r"\b(water|ice|frost|frozen|blizzard|glacial|ocean|tidal|coral|abyssal|"
    r"swamp|bog|marsh|flood|miasma|quicksand|bogsink|spring|leech|drown|"
    r"poison pool|toxic pool|healing waters)\b",
    re.I,
)
AIR_RE = re.compile(
    r"\b(wind|storm|thunder|lightning|tempest|hurricane|cosmic|stellar|"
    r"nebula|galaxy|gravity|mirage|bat swarm|sandstorm|gust|howl|zephyr|"
    r"divine|celestial|heavenly|void|shadow|arcane|magic missile|wand)\b",
    re.I,
)
SCORCHED_RE = re.compile(r"\b(scorch|desert|dune|heatwave|lava|magma|volcanic|ash)\b", re.I)
FLOODED_RE = re.compile(r"\b(swamp|bog|marsh|flood|underwater|ocean|miasma|quicksand|bogsink)\b", re.I)
OVERGROWN_RE = re.compile(
    r"\b(forest|grove|vine|thorn|branch|meadow|wildflower|nature|pollen|canopy)\b",
    re.I,
)
EXPOSED_RE = re.compile(
    r"\b(crypt|tomb|shadow|void|darkness|ruin|exposed|curse|necrotic|"
    r"ancient|magical barrier|puzzle|chamber|sanctum)\b",
    re.I,
)


def load_class_action_map() -> dict[str, set[str]]:
    """Action name -> primary class tags (warrior/rogue/wizard/barbarian) from class deck rules."""
    data = json.loads(CLASS_ACTIONS_PATH.read_text(encoding="utf-8"))
    out: dict[str, set[str]] = {}
    for rule in data.get("rules", []):
        name = (rule.get("actionName") or "").strip()
        key = (rule.get("classKey") or "").strip().lower()
        if not name or key not in PRIMARY_CLASS_KEYS:
            continue
        out.setdefault(name, set()).add(key)
    return out


def parse_tags_field(raw) -> list[str]:
    if raw is None:
        return []
    if isinstance(raw, list):
        parts = [str(x).strip().lower() for x in raw if str(x).strip()]
    else:
        parts = [p.strip().lower() for p in str(raw).replace(";", ",").split(",") if p.strip()]
    return [p for p in parts if p in VALID_TAGS]


def ordered_unique(tags: list[str]) -> list[str]:
    seen: set[str] = set()
    out: list[str] = []
    for tag in TAG_ORDER:
        if tag in tags and tag not in seen:
            seen.add(tag)
            out.append(tag)
    for tag in tags:
        if tag in VALID_TAGS and tag not in seen:
            seen.add(tag)
            out.append(tag)
    return out


def infer_match_tags(text: str) -> list[str]:
    tags: list[str] = []
    if FIRE_RE.search(text):
        tags.append("fire")
    if EARTH_RE.search(text):
        tags.append("earth")
    if WATER_RE.search(text):
        tags.append("water")
    if AIR_RE.search(text):
        tags.append("air")
    if SCORCHED_RE.search(text):
        tags.append("scorched")
    if FLOODED_RE.search(text):
        tags.append("flooded")
    if OVERGROWN_RE.search(text):
        tags.append("overgrown")
    if EXPOSED_RE.search(text) or not tags:
        tags.append("exposed")
    return tags


def env_prefix_tags(action_name: str) -> list[str]:
    if " " not in action_name:
        return []
    prefix = action_name.split(" ", 1)[0].upper()
    return list(ENV_PREFIX_TAGS.get(prefix, []))


def status_effect_hints(action: dict) -> list[str]:
    hints: list[str] = []
    if action.get("burn"):
        hints.append("fire")
    if action.get("poison"):
        hints.append("water")
    if action.get("bleed"):
        hints.append("earth")
    if action.get("slow") and "ice" not in hints:
        hints.append("water")
    return hints


def infer_tags(action: dict, class_map: dict[str, set[str]]) -> list[str]:
    name = (action.get("action") or "").strip()
    desc = (action.get("description") or "").strip()
    category = (action.get("category") or "").strip()
    rarity = (action.get("rarity") or "").strip().lower()
    text = f"{name} {desc}"

    existing = parse_tags_field(action.get("tags"))
    tags: list[str] = []
    for preserved in ("unique", "starter"):
        if preserved in existing:
            tags.append(preserved)

    if category == "ENVIRONMENT":
        if "environment" not in tags:
            tags.append("environment")
        tags.extend(env_prefix_tags(name))
        tags.extend(infer_match_tags(text))
    elif category == "ENEMY":
        if "enemy" not in tags:
            tags.append("enemy")
        tags.extend(status_effect_hints(action))
        tags.extend(infer_match_tags(text))
    elif category == "ModTrade":
        if "modtrade" not in tags:
            tags.append("modtrade")
    elif category == "WEAPON":
        if "weapon" not in tags:
            tags.append("weapon")
        weapon_type = WEAPON_TYPE_BY_ACTION.get(name.upper())
        if weapon_type:
            tags.append(weapon_type)
    elif category in CLASS_BY_CATEGORY:
        if "class" not in tags:
            tags.append("class")
        class_tag = CLASS_BY_CATEGORY[category]
        if class_tag not in tags:
            tags.append(class_tag)
    else:
        class_keys = class_map.get(name, set())
        if class_keys:
            if "class" not in tags:
                tags.append("class")
            tags.extend(sorted(class_keys))
        elif category == "" and rarity in VALID_TAGS:
            if "class" not in tags:
                tags.append("class")
            if rarity not in tags:
                tags.append(rarity)
        elif category == "":
            if "class" not in tags:
                tags.append("class")

    if rarity and rarity in VALID_TAGS and rarity not in tags and category not in CLASS_BY_CATEGORY:
        tags.append(rarity)

    return ordered_unique(tags)


def format_tags_cell(tags: list[str]) -> str:
    return ", ".join(tags)


def main() -> None:
    class_map = load_class_action_map()
    actions = json.loads(ACTIONS_PATH.read_text(encoding="utf-8"))
    updated = 0
    for action in actions:
        tags = infer_tags(action, class_map)
        cell = format_tags_cell(tags)
        if action.get("tags") != cell:
            if cell:
                action["tags"] = cell
            elif "tags" in action:
                del action["tags"]
            updated += 1
    ACTIONS_PATH.write_text(json.dumps(actions, indent=2, ensure_ascii=False) + "\n", encoding="utf-8")
    print(f"Updated {updated} / {len(actions)} actions in {ACTIONS_PATH.relative_to(ROOT)}")


if __name__ == "__main__":
    main()
