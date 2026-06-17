#!/usr/bin/env python3
"""Apply registry tags to GameData/Actions.json from category, class deck, and action flavor."""

from __future__ import annotations

import json
import re
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
ACTIONS_PATH = ROOT / "GameData" / "Actions.json"
CLASS_ACTIONS_PATH = ROOT / "GameData" / "ClassActions.json"

VALID_TAGS = frozenset({
    "environment", "enemy", "weapon", "class", "item", "action", "unique", "starter", "modtrade",
    "warrior", "barbarian", "rogue", "wizard",
    "sword", "mace", "dagger", "wand",
    "common", "uncommon", "rare", "epic", "legendary", "mythic",
    "fire", "earth", "water", "air",
    "scorched", "flooded", "overgrown", "exposed",
    "required", "opener", "finisher",
    "swift", "bludgeon", "focus", "insight",
    "confidence", "footwork", "target", "aim",
    "weapon_basic",
})

TAG_ORDER = [
    "environment", "enemy", "weapon", "class", "item", "action", "modtrade", "unique", "starter",
    "required", "opener", "finisher",
    "warrior", "barbarian", "rogue", "wizard",
    "sword", "mace", "dagger", "wand",
    "common", "uncommon", "rare", "epic", "legendary", "mythic",
    "swift", "bludgeon", "focus", "insight",
    "confidence", "footwork", "target", "aim",
    "fire", "earth", "water", "air",
    "scorched", "flooded", "overgrown", "exposed",
]

CLASS_BY_CATEGORY = {
    "BARBARIAN": "barbarian",
    "WARRIOR": "warrior",
    "ROGUE": "rogue",
    "WIZARD": "wizard",
}

PRIMARY_CLASS_KEYS = frozenset({"barbarian", "warrior", "rogue", "wizard"})

WEAPON_TYPE_BY_ACTION = {
    "STRIKE": "sword",
    "STAB": "dagger",
    "SLAM": "mace",
    "MAGIC MISSILE": "wand",
    "CAST": "wand",
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
}

FIRE_RE = re.compile(r"\b(fire|lava|magma|flame|burn|scorch|ember|ash)\b", re.I)
EARTH_RE = re.compile(r"\b(stone|rock|sand|earth|cave|crypt|mace|slam)\b", re.I)
WATER_RE = re.compile(r"\b(water|ice|swamp|flood|miasma)\b", re.I)
AIR_RE = re.compile(r"\b(wind|storm|arcane|magic|wand)\b", re.I)


def load_class_action_map() -> dict[str, set[str]]:
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
    return tags


def env_prefix_tags(action_name: str) -> list[str]:
    if " " not in action_name:
        return []
    prefix = action_name.split(" ", 1)[0].upper()
    return list(ENV_PREFIX_TAGS.get(prefix, []))


def infer_structured_tags(action: dict) -> list[str]:
    tags: list[str] = []
    if str(action.get("opener", "")).strip().lower() in ("true", "1", "yes"):
        tags.append("opener")
    if str(action.get("finisher", "")).strip().lower() in ("true", "1", "yes"):
        tags.append("finisher")
    if "weapon_basic" in parse_tags_field(action.get("tags")):
        tags.append("required")
    if action.get("speedMod"):
        tags.append("swift")
    if action.get("damageMod"):
        tags.append("bludgeon")
    if action.get("ampMod"):
        tags.append("focus")
    if action.get("heroAccuracy") or action.get("focus"):
        tags.append("insight")
    for field, tag in (
        ("heroHit", "footwork"),
        ("heroCombo", "target"),
        ("heroCrit", "aim"),
        ("heroCritMiss", "confidence"),
    ):
        val = action.get(field)
        if val not in (None, "", "0", 0):
            tags.append(tag)
    return tags


def infer_tags(action: dict, class_map: dict[str, set[str]]) -> list[str]:
    name = (action.get("action") or "").strip()
    desc = (action.get("description") or "").strip()
    category = (action.get("category") or "").strip()
    rarity = (action.get("rarity") or "").strip().lower()
    text = f"{name} {desc}"

    existing = parse_tags_field(action.get("tags"))
    tags: list[str] = []
    for preserved in ("unique", "starter", "modtrade", "required", "opener", "finisher"):
        if preserved in existing:
            tags.append(preserved)

    if category == "ENVIRONMENT":
        tags.append("environment")
        tags.extend(env_prefix_tags(name))
        tags.extend(infer_match_tags(text))
    elif category == "ENEMY":
        tags.append("enemy")
        tags.extend(infer_match_tags(text))
    elif category == "ModTrade":
        tags.append("modtrade")
    elif category == "WEAPON":
        tags.append("weapon")
        weapon_type = WEAPON_TYPE_BY_ACTION.get(name.upper())
        if weapon_type:
            tags.append(weapon_type)
    elif category in CLASS_BY_CATEGORY:
        tags.append("class")
        tags.append("action")
        tags.append(CLASS_BY_CATEGORY[category])
    elif category == "ARMOR" or category == "ITEM":
        tags.append("item")
        tags.append("action")
    else:
        class_keys = class_map.get(name, set())
        if class_keys:
            tags.append("class")
            tags.append("action")
            tags.extend(sorted(class_keys))
        elif category == "":
            tags.append("action")

    tags.extend(infer_structured_tags(action))

    if rarity and rarity in VALID_TAGS and rarity not in tags:
        tags.append(rarity)

    tags.extend(infer_match_tags(text))
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
