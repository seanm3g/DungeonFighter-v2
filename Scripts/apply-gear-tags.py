#!/usr/bin/env python3
"""Apply base catalog tags to GameData/Weapons.json and Armor.json.

Catalog tags are identity metadata (weapon type, class path, starter) — not the action pool-gate ``weapon`` tag.
Material and other affix tags are rolled at loot time and must not live on base rows.
"""

from __future__ import annotations

import json
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
WEAPONS_PATH = ROOT / "GameData" / "Weapons.json"
ARMOR_PATH = ROOT / "GameData" / "Armor.json"

# Item-scoped registry tags allowed on catalog rows (no materials / creature attrs).
VALID_CATALOG_TAGS = frozenset(
    {
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
        "fire",
        "earth",
        "water",
        "air",
        "living",
        "undead",
        "plant",
        "elemental",
        "celestial",
    }
)

WEAPON_TAG_ORDER = [
    "sword",
    "mace",
    "dagger",
    "wand",
    "warrior",
    "barbarian",
    "rogue",
    "wizard",
    "unique",
    "starter",
]

TYPE_TO_WEAPON_TAG = {
    "sword": "sword",
    "mace": "mace",
    "dagger": "dagger",
    "wand": "wand",
}

TYPE_TO_CLASS_TAG = {
    "mace": "barbarian",
    "sword": "warrior",
    "dagger": "rogue",
    "wand": "wizard",
}

PRESERVE_TAGS = frozenset({"unique"})


def order_tags(tags: list[str], order: list[str]) -> list[str]:
    rank = {t: i for i, t in enumerate(order)}
    return sorted(tags, key=lambda t: (rank.get(t, len(order)), t))


def normalize_existing(tags) -> set[str]:
    if not tags:
        return set()
    return {str(t).strip().lower() for t in tags if str(t).strip()}


def build_weapon_tags(row: dict, *, is_starter: bool) -> list[str]:
    weapon_type = (row.get("type") or "").strip().lower()
    type_tag = TYPE_TO_WEAPON_TAG.get(weapon_type)
    class_tag = TYPE_TO_CLASS_TAG.get(weapon_type)

    tags: set[str] = set()
    if type_tag:
        tags.add(type_tag)
    if class_tag:
        tags.add(class_tag)

    existing = normalize_existing(row.get("tags"))
    for tag in existing:
        if tag in PRESERVE_TAGS and tag in VALID_CATALOG_TAGS:
            tags.add(tag)
    if is_starter:
        tags.add("starter")

    return order_tags(sorted(tags), WEAPON_TAG_ORDER)


def resolve_starter_weapon_indices(rows: list[dict]) -> set[int]:
    """First starter-tagged row per weapon type keeps starter; duplicates lose it."""
    seen_types: set[str] = set()
    starter_indices: set[int] = set()
    for i, row in enumerate(rows):
        existing = normalize_existing(row.get("tags"))
        if "starter" not in existing:
            continue
        weapon_type = (row.get("type") or "").strip().lower()
        if not weapon_type or weapon_type in seen_types:
            continue
        seen_types.add(weapon_type)
        starter_indices.add(i)
    return starter_indices


def apply_weapons() -> tuple[int, int]:
    rows = json.loads(WEAPONS_PATH.read_text(encoding="utf-8"))
    starter_indices = resolve_starter_weapon_indices(rows)
    changed = 0
    for i, row in enumerate(rows):
        new_tags = build_weapon_tags(row, is_starter=i in starter_indices)
        old_tags = row.get("tags")
        if old_tags != new_tags:
            row["tags"] = new_tags
            changed += 1
    WEAPONS_PATH.write_text(json.dumps(rows, indent=2, ensure_ascii=False) + "\n", encoding="utf-8")
    return len(rows), changed


def apply_armor() -> tuple[int, int]:
    rows = json.loads(ARMOR_PATH.read_text(encoding="utf-8"))
    changed = 0
    for row in rows:
        existing = normalize_existing(row.get("tags"))
        keep = [t for t in ("starter", "unique") if t in existing and t in VALID_CATALOG_TAGS]
        if keep:
            new_tags = order_tags(keep, ["unique", "starter"])
            if row.get("tags") != new_tags:
                row["tags"] = new_tags
                changed += 1
        elif row.get("tags") is not None:
            row.pop("tags", None)
            changed += 1
    ARMOR_PATH.write_text(json.dumps(rows, indent=2, ensure_ascii=False) + "\n", encoding="utf-8")
    return len(rows), changed


def main() -> None:
    weapon_total, weapon_changed = apply_weapons()
    armor_total, armor_changed = apply_armor()
    print(f"Weapons: updated {weapon_changed}/{weapon_total} rows in {WEAPONS_PATH.name}")
    print(f"Armor: updated {armor_changed}/{armor_total} rows in {ARMOR_PATH.name}")


if __name__ == "__main__":
    main()
