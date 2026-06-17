#!/usr/bin/env python3
"""One-time migration: Artificer→Sage, large/bulky→canonical size tags."""

from __future__ import annotations

import json
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
ENEMIES_PATH = ROOT / "GameData" / "Enemies.json"

# Per-enemy tag replacement (name -> new size/build tags to set after removing large/bulky)
SIZE_MIGRATION: dict[str, list[str]] = {
    "Spider": ["frail", "tiny"],
    "Treant": ["giant"],
    "Dune Roller": ["giant"],
    "Poison Dart Sentinel": ["young"],
    "Guardian Scarab": ["giant", "tiny"],
    "Rock Lobber": ["giant"],
    "Soot Golem": ["giant"],
    "Scorched Treant": ["giant"],
    "Temple Knight": ["young"],
    "Pilar Watcher": ["young"],
    "Royal Flame Guard": ["giant"],
    "Throne Effigy": ["giant"],
    "Slag Tortoise": ["giant"],
    "Molten Crab": ["giant"],
    "Amber Sentinel": ["giant"],
    "Mound Warden": ["giant"],
    "Mud Lurker": ["giant"],
    "Fourecourt Guard": ["giant"],
    "Tide Sentinel": ["giant"],
    "Aqueduct Warden": ["giant"],
}

SIZE_TAGS = {"giant", "young", "tiny", "frail", "large", "bulky"}
BODY_TAGS = {"has_hands", "has_feet", "has_legs", "has_head"}

# Infer body-part flags from name/description
QUADRUPED_RE = (
    "wolf", "bear", "spider", "bat", "boar", "scorpion", "serpent", "crab",
    "tortoise", "beetle", "scarab", "rat", "leech", "toad", "salamander",
)
HUMANOID_RE = (
    "goblin", "knight", "guard", "sentinel", "warden", "priest", "caster",
    "warrior", "rogue", "lich", "mimic", "imp", "duelist", "acrobat",
    "trickster", "hunter", "stalker", "cleric", "zealot",
)
HEADLESS_RE = ("golem", "effigy", "construct", "wisp", "spirit", "elemental")


def infer_body_tags(name: str, desc: str) -> set[str]:
    text = f"{name} {desc}".lower()
    name_l = name.lower()
    tags: set[str] = set()
    if any(w in name_l or w in text for w in HUMANOID_RE):
        tags.update({"has_hands", "has_feet", "has_legs", "has_head"})
    elif any(w in name_l for w in QUADRUPED_RE):
        tags.update({"has_feet", "has_legs", "has_head"})
    elif any(w in name_l or w in text for w in HEADLESS_RE):
        pass  # constructs may lack body parts
    elif "treant" in name_l or "plant" in text:
        tags.add("has_head")
    return tags


def migrate_enemy(enemy: dict) -> None:
    if enemy.get("archetype") == "Artificer":
        enemy["archetype"] = "Sage"

    name = (enemy.get("name") or "").strip()
    tags = list(enemy.get("tags") or [])
    non_size = [t for t in tags if t.lower() not in SIZE_TAGS and t.lower() not in BODY_TAGS]
    existing_body = {t.lower() for t in tags if t.lower() in BODY_TAGS}

    if name in SIZE_MIGRATION:
        new_size = SIZE_MIGRATION[name]
    else:
        new_size = [t for t in tags if t.lower() in SIZE_TAGS and t.lower() not in ("large", "bulky")]

    body = existing_body | {t.lower() for t in infer_body_tags(name, enemy.get("description") or "")}
    # preserve explicit has_hands from data
    if any(t.lower() == "has_hands" for t in tags):
        body.add("has_hands")

    ordered_body = [t for t in ("has_hands", "has_feet", "has_legs", "has_head") if t in body]
    enemy["tags"] = non_size + new_size + ordered_body


def main() -> None:
    data = json.loads(ENEMIES_PATH.read_text(encoding="utf-8"))
    for enemy in data:
        migrate_enemy(enemy)
    ENEMIES_PATH.write_text(
        json.dumps(data, indent=2, ensure_ascii=False) + "\n",
        encoding="utf-8",
    )
    print(f"Migrated {len(data)} enemies -> {ENEMIES_PATH}")


if __name__ == "__main__":
    main()
