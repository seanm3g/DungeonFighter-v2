#!/usr/bin/env python3
"""Apply registry tags to GameData/Enemies.json from name, description, isLiving, and rarity."""

from __future__ import annotations

import json
import re
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
ENEMIES_PATH = ROOT / "GameData" / "Enemies.json"

UNDEAD_RE = re.compile(
    r"\b(skeleton|zombie|ghoul|wight|lich|mummy|shade|wraith|undead|necromantic|"
    r"tomb|burial|sarcoph|ancestor|bone spreader|deserter shade|pollen wraith|"
    r"torch wraith|altitude wraith|ruin wraith|loop shade)\b",
    re.I,
)
PLANT_RE = re.compile(
    r"\b(treant|vine|root sprite|thorn|reed stalker|pollen|nectar|spore|"
    r"charwood|living wall|animated vine|scorched treant|overgrown|plant)\b",
    re.I,
)
ELEMENTAL_RE = re.compile(
    r"\b(elemental|golem|spirit|wisp|effigy|construct|ember|magma|molten|"
    r"crystal leech|glass shard|soot golem|reignited)\b",
    re.I,
)
CELESTIAL_RE = re.compile(r"\b(moon blessed|sun priest|celestial|divine|sacred cleric)\b", re.I)

FIRE_RE = re.compile(
    r"\b(fire|lava|magma|ember|cinder|ash|scorch|flame|torch|molten|salamander|"
    r"sun lance|reignit|smog|charwood|volcanic|pyre|heat|solar|ignit)\b",
    re.I,
)
EARTH_RE = re.compile(
    r"\b(stone|rock|sand|dune|glass|slag|crystal|golem|colossus|tortoise|"
    r"crab|amber|bridge|temple|pilar|seal|throne|mound|scarab|pyramid|"
    r"statue|carved|terracotta|burrow|serpent|scorpion)\b",
    re.I,
)
WATER_RE = re.compile(r"\b(leech|oasis|flood|drift|island|restorative|sap|vitality)\b", re.I)
AIR_RE = re.compile(
    r"\b(storm|hawk|harpy|ashwing|altitude|wind|mirage|glare|sandstorm|"
    r"desert noon|flying|bat\b|avian)\b",
    re.I,
)

GIANT_RE = re.compile(r"\b(titan|colossus|golem|warden|mound|bear|treant|scarab|roller)\b", re.I)
YOUNG_RE = re.compile(
    r"\b(young|juvenile|fledgling|hatchling|cub|whelp|spawn|sentinel|watcher)\b",
    re.I,
)
FRAIL_RE = re.compile(
    r"\b(wisp|skeleton|glass|thin|brittle|frail|shard|bony|gaunt|fragile|spider)\b",
    re.I,
)
TINY_RE = re.compile(
    r"\b(bat\b|imp\b|wisp|sprite|leech|rat\b|spider|drone|creeper|scorpion|"
    r"serpent|toad|scarab)\b",
    re.I,
)
HANDS_NAME_RE = re.compile(
    r"\b(goblin|knight|priest|sentinel|mimic|runner|duplicate|lich|"
    r"warlord|duelist|trickster|acrobat|flanker|cleric|guard|"
    r"warden|stalker|hunter|caster|imp)\b",
    re.I,
)
FEET_NAME_RE = re.compile(
    r"\b(wolf|bear|spider|bat|boar|scorpion|serpent|crab|tortoise|beetle|"
    r"scarab|rat|leech|toad|salamander|goblin|knight|guard|sentinel|warden)\b",
    re.I,
)
LEGS_NAME_RE = re.compile(
    r"\b(wolf|bear|spider|bat|boar|scorpion|serpent|goblin|knight|guard|"
    r"sentinel|warden|imp|acrobat|hunter|stalker)\b",
    re.I,
)
HEAD_NAME_RE = re.compile(
    r"\b(wolf|bear|spider|bat|boar|scorpion|serpent|crab|tortoise|beetle|"
    r"goblin|knight|guard|sentinel|warden|lich|treant|dragon|hydra)\b",
    re.I,
)

BOSS_NAMES = {
    "summit titan",
    "stone colossus",
    "burial lich",
    "royal flame guard",
    "throne effigy",
    "moon blessed",
    "the other",
    "mound warden",
    "vigil alpha",
    "bridge warden",
    "sun priest",
    "glass shard golem",
    "dune roller",
}
BOSS_RE = re.compile(
    r"\b(titan|colossus|royal flame|throne effigy|moon blessed|the other|"
    r"mound warden|vigil alpha|bridge warden|burial lich)\b",
    re.I,
)

MINION_RE = re.compile(
    r"\b(goblin|spider|bat\b|imp\b|rat\b|wisp|sprite|drone|scavenger|"
    r"overflow imp|guile sprite|root sprite|nectar drone|vent spore|"
    r"reignited wisp|ember spirit)\b",
    re.I,
)

TAG_ORDER = [
    "living",
    "undead",
    "plant",
    "elemental",
    "celestial",
    "fire",
    "earth",
    "water",
    "air",
    "boss",
    "minion",
    "giant",
    "young",
    "tiny",
    "frail",
    "has_hands",
    "has_feet",
    "has_legs",
    "has_head",
]


def infer_tags(enemy: dict) -> list[str]:
    name = (enemy.get("name") or "").strip()
    desc = (enemy.get("description") or "").strip()
    text = f"{name} {desc}".lower()
    name_l = name.lower()
    rarity = (enemy.get("rarity") or "common").strip().lower()
    living = enemy.get("isLiving", True)

    tags: list[str] = []

    if UNDEAD_RE.search(text) or (
        not living
        and not PLANT_RE.search(text)
        and not ELEMENTAL_RE.search(text)
        and UNDEAD_RE.search(name_l)
    ):
        tags.append("undead")
    elif PLANT_RE.search(text):
        tags.append("plant")
    elif ELEMENTAL_RE.search(text) or "elemental" in name_l:
        tags.append("elemental")
    elif CELESTIAL_RE.search(text):
        tags.append("celestial")
    elif living:
        tags.append("living")
    else:
        if ELEMENTAL_RE.search(text) or FIRE_RE.search(text):
            tags.append("elemental")
        elif PLANT_RE.search(text):
            tags.append("plant")
        else:
            tags.append("undead")

    if FIRE_RE.search(text):
        tags.append("fire")
    if EARTH_RE.search(text):
        tags.append("earth")
    if WATER_RE.search(text):
        tags.append("water")
    if AIR_RE.search(text):
        tags.append("air")

    if name_l in BOSS_NAMES or BOSS_RE.search(name_l) or (
        rarity == "rare" and BOSS_RE.search(text)
    ):
        tags.append("boss")
    elif rarity in ("common", "uncommon") and MINION_RE.search(name_l):
        tags.append("minion")

    if BOSS_RE.search(name_l) or name_l in BOSS_NAMES:
        if GIANT_RE.search(text) or GIANT_RE.search(name_l):
            tags.append("giant")
    elif GIANT_RE.search(name_l) or (
        GIANT_RE.search(text) and rarity in ("rare", "uncommon")
    ):
        tags.append("giant")

    has_giant = "giant" in tags

    if not has_giant and YOUNG_RE.search(text):
        tags.append("young")

    if TINY_RE.search(name_l):
        tags.append("tiny")

    if FRAIL_RE.search(text):
        tags.append("frail")

    if HANDS_NAME_RE.search(name_l):
        tags.append("has_hands")
    if FEET_NAME_RE.search(name_l):
        tags.append("has_feet")
    if LEGS_NAME_RE.search(name_l):
        tags.append("has_legs")
    if HEAD_NAME_RE.search(name_l):
        tags.append("has_head")

    seen: set[str] = set()
    ordered: list[str] = []
    tag_set = set(tags)
    for key in TAG_ORDER:
        if key in tag_set and key not in seen:
            seen.add(key)
            ordered.append(key)
    for t in tags:
        if t not in seen:
            seen.add(t)
            ordered.append(t)
    return ordered


def main() -> None:
    data = json.loads(ENEMIES_PATH.read_text(encoding="utf-8"))
    if not isinstance(data, list):
        raise SystemExit("Expected JSON array")

    for enemy in data:
        tags = infer_tags(enemy)
        if tags:
            enemy["tags"] = tags
        elif "tags" in enemy:
            del enemy["tags"]

    ENEMIES_PATH.write_text(
        json.dumps(data, indent=2, ensure_ascii=False) + "\n",
        encoding="utf-8",
    )
    print(f"Tagged {len(data)} enemies -> {ENEMIES_PATH}")


if __name__ == "__main__":
    main()
