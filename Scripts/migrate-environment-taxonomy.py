#!/usr/bin/env python3
"""Apply canonical environment tags + activity/structure + unstableThresholdMod to Rooms.json."""

from __future__ import annotations

import json
import re
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
ROOMS_PATH = ROOT / "GameData" / "Rooms.json"

VALID_TAGS = frozenset({
    "fire", "earth", "water", "air",
    "scorched", "flooded", "overgrown", "exposed",
    "elegant", "dilapidated",
    "dormant", "cycling", "active",
})
TAG_ORDER = [
    "fire", "earth", "water", "air",
    "scorched", "flooded", "overgrown", "exposed",
    "elegant", "dilapidated",
    "dormant", "cycling", "active",
]

ELEGANT_RE = re.compile(r"\b(temple|court|sanctuary|shrine|observatory|library|armory|dining|chamber|vault|star|galaxy|nebula)\b", re.I)
DILAPIDATED_RE = re.compile(r"\b(crypt|tomb|ruin|crumbl|decay|lost|shadow|void|trap|burial|catacomb|abandon)\b", re.I)
DORMANT_RE = re.compile(r"\b(rest|safe|spring|meadow|clearing|sanctuary)\b", re.I)
ACTIVE_RE = re.compile(r"\b(lava|magma|volcanic|swamp|toxic|trap|boss|storm|hurricane|temporal|unstable)\b", re.I)
UNSTABLE_LOCATIONS = {
    "Temporal Rift": 4,
    "Time Chamber": -2,
    "Trap Room": 2,
    "Volcanic Vent": 0,
}


def ordered_unique(tags: list[str]) -> list[str]:
    seen: set[str] = set()
    out: list[str] = []
    tag_set = set(tags)
    for key in TAG_ORDER:
        if key in tag_set and key not in seen:
            seen.add(key)
            out.append(key)
    for t in tags:
        if t in VALID_TAGS and t not in seen:
            seen.add(t)
            out.append(t)
    return out


def infer_tags(room: dict) -> list[str]:
    location = (room.get("location") or room.get("name") or "").strip()
    desc = (room.get("description") or "").strip()
    text = f"{location} {desc}".lower()

    existing = [t.strip().lower() for t in (room.get("tags") or []) if str(t).strip()]
    tags = [t for t in existing if t in VALID_TAGS]

    if ELEGANT_RE.search(text) and "elegant" not in tags:
        tags.append("elegant")
    if DILAPIDATED_RE.search(text) and "dilapidated" not in tags:
        tags.append("dilapidated")

    has_activity = any(t in tags for t in ("dormant", "cycling", "active"))
    if not has_activity:
        if DORMANT_RE.search(text):
            tags.append("dormant")
        elif ACTIVE_RE.search(text):
            tags.append("active")
        else:
            tags.append("cycling")

    return ordered_unique(tags)


def main() -> None:
    rooms = json.loads(ROOMS_PATH.read_text(encoding="utf-8"))
    for room in rooms:
        room["tags"] = infer_tags(room)
        loc = (room.get("location") or room.get("name") or "").strip()
        if loc in UNSTABLE_LOCATIONS:
            room["unstableThresholdMod"] = UNSTABLE_LOCATIONS[loc]
        elif "unstable" in (room.get("description") or "").lower():
            room["unstableThresholdMod"] = 2
        else:
            room.pop("unstableThresholdMod", None)

    ROOMS_PATH.write_text(json.dumps(rooms, indent=2, ensure_ascii=False) + "\n", encoding="utf-8")
    print(f"Tagged {len(rooms)} rooms -> {ROOMS_PATH}")


if __name__ == "__main__":
    main()
