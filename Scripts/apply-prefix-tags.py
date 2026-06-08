#!/usr/bin/env python3
"""Apply registry tags to prefix rows in Modifications.json and PrefixMaterialQuality.json."""

from __future__ import annotations

import json
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
MODIFICATIONS_PATH = ROOT / "GameData" / "Modifications.json"
PREFIX_MQ_PATH = ROOT / "GameData" / "PrefixMaterialQuality.json"

CLASS_FROM_DESCRIPTION = {
    "barbarian": "barbarian",
    "warrior": "warrior",
    "rogue": "rogue",
    "wizard": "wizard",
}

TAG_ORDER = [
    "bone",
    "bronze",
    "glass",
    "willow",
    "steel",
    "gold",
    "obsidian",
    "silver",
    "damascus",
    "mithril",
    "shadow",
    "crystal",
    "stone",
    "unknown",
    "strange",
    "celestial",
    "barbarian",
    "warrior",
    "rogue",
    "wizard",
    "fire",
    "earth",
    "water",
    "air",
    "living",
    "undead",
    "plant",
    "elemental",
]


def order_tags(tags: list[str]) -> list[str]:
    rank = {t: i for i, t in enumerate(TAG_ORDER)}
    return sorted(tags, key=lambda t: (rank.get(t, len(TAG_ORDER)), t))


def build_tags(row: dict) -> list[str] | None:
    category = (row.get("prefixCategory") or "ADJECTIVE").strip().upper()
    name = (row.get("Name") or "").strip()
    effect = (row.get("Effect") or "").strip().upper()
    description = (row.get("Description") or "").strip().lower()

    if category == "QUALITY":
        return None

    tags: set[str] = set()

    if category == "MATERIAL":
        if name:
            tags.add(name.strip().lower())
        for key, class_tag in CLASS_FROM_DESCRIPTION.items():
            if key in description:
                tags.add(class_tag)
                break
        return order_tags(sorted(tags)) if tags else None

    # Adjective
    if name.lower() == "flaming" or effect == "BURN":
        tags.add("fire")

    return order_tags(sorted(tags)) if tags else None


def apply_file(path: Path) -> tuple[int, int]:
    rows = json.loads(path.read_text(encoding="utf-8"))
    changed = 0
    for row in rows:
        new_tags = build_tags(row)
        old_tags = row.get("tags")
        if new_tags is None:
            if old_tags is not None:
                row.pop("tags", None)
                changed += 1
            continue
        if old_tags != new_tags:
            row["tags"] = new_tags
            changed += 1
    path.write_text(json.dumps(rows, indent=2, ensure_ascii=False) + "\n", encoding="utf-8")
    return len(rows), changed


def main() -> None:
    for path in (MODIFICATIONS_PATH, PREFIX_MQ_PATH):
        total, changed = apply_file(path)
        print(f"{path.name}: updated {changed}/{total} rows")


if __name__ == "__main__":
    main()
