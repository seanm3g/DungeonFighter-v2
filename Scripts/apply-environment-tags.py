#!/usr/bin/env python3
"""Apply registry environment tags to GameData/Rooms.json from location, description, and actions."""

from __future__ import annotations

import json
import re
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
ROOMS_PATH = ROOT / "GameData" / "Rooms.json"

VALID_TAGS = frozenset(
    {"fire", "earth", "water", "air", "scorched", "flooded", "overgrown", "exposed"}
)
TAG_ORDER = ["fire", "earth", "water", "air", "scorched", "flooded", "overgrown", "exposed"]

# Primary mapping from room display name (location key).
LOCATION_TAGS: dict[str, list[str]] = {
    # Generic dungeon interiors (theme-neutral)
    "Entrance": ["earth", "exposed"],
    "Treasure Room": ["earth", "exposed"],
    "Guard Post": ["earth", "exposed"],
    "Trap Room": ["earth", "exposed"],
    "Puzzle Chamber": ["earth", "exposed"],
    "Rest Area": ["earth", "exposed"],
    "Storage Room": ["earth", "exposed"],
    "Library": ["earth", "exposed"],
    "Armory": ["earth", "exposed"],
    "Kitchen": ["earth", "exposed"],
    "Dining Hall": ["earth", "exposed"],
    "Boss Chamber": ["earth", "exposed"],
    # Forest
    "Forest Clearing": ["overgrown"],
    "Ancient Grove": ["overgrown"],
    "Wolf Den": ["overgrown", "earth"],
    "Bear Cave": ["overgrown", "earth"],
    "Sacred Grove": ["overgrown"],
    "Wildflower Meadow": ["overgrown"],
    "Natural Spring": ["overgrown", "water"],
    # Lava / volcanic
    "Lava Chamber": ["fire", "scorched"],
    "Magma Pool": ["fire", "scorched"],
    "Volcanic Vent": ["fire", "scorched"],
    "Magma Chamber": ["fire", "scorched"],
    "Fire Chamber": ["fire", "scorched"],
    # Crypt / ruins
    "Crypt Passage": ["exposed"],
    "Burial Chamber": ["exposed"],
    "Tomb of the Forgotten": ["exposed"],
    "Crumbling Hall": ["earth", "exposed"],
    "Lost Shrine": ["earth", "exposed"],
    "Decayed Chamber": ["earth", "exposed"],
    # Ice
    "Frozen Cavern": ["water", "exposed"],
    "Glacial Chamber": ["water", "exposed"],
    "Frozen Lake": ["water", "exposed"],
    # Crystal / stone
    "Crystal Garden": ["earth", "exposed"],
    "Geode Chamber": ["earth", "exposed"],
    # Shadow / void
    "Shadow Realm": ["exposed"],
    "Umbra Sanctum": ["exposed"],
    "Void Chamber": ["exposed"],
    "Emptiness Hall": ["exposed"],
    "Nothingness Room": ["exposed"],
    # Industrial / steam
    "Steam Engine Room": ["fire", "earth"],
    "Clockwork Laboratory": ["earth", "exposed"],
    "Cog Chamber": ["earth", "exposed"],
    # Swamp
    "Bog Clearing": ["water", "flooded"],
    "Marsh Sanctuary": ["water", "flooded", "overgrown"],
    "Toxic Pool": ["water", "flooded"],
    # Cosmic
    "Star Chamber": ["air", "exposed"],
    "Nebula Observatory": ["air", "exposed"],
    "Galaxy Vault": ["air", "exposed"],
    # Cavern / mining
    "Deep Tunnel": ["earth", "exposed"],
    "Subterranean Hall": ["earth", "exposed"],
    "Mining Shaft": ["earth", "exposed"],
    # Storm
    "Thunder Hall": ["air", "exposed"],
    "Tempest Chamber": ["air", "exposed"],
    "Hurricane Vault": ["air", "exposed"],
    # Arcane
    "Spell Chamber": ["exposed"],
    "Wizard's Study": ["exposed"],
    "Magic Vault": ["exposed"],
    # Desert
    "Sand Dune": ["earth", "scorched"],
    "Oasis": ["water", "earth"],
    "Mirage Chamber": ["earth", "scorched", "air"],
    # Ocean
    "Underwater Cavern": ["water", "flooded"],
    "Coral Garden": ["water", "flooded"],
    "Abyssal Depths": ["water", "flooded"],
    # Mountain
    "Peak Summit": ["earth", "exposed"],
    "Alpine Meadow": ["earth", "overgrown", "exposed"],
    "Rocky Outcrop": ["earth", "exposed"],
    # Time
    "Time Chamber": ["air", "exposed"],
    "Chronos Sanctum": ["air", "exposed"],
    "Echo Chamber": ["earth", "exposed"],
    # Dream
    "Dreamscape": ["exposed"],
    "Lucid Chamber": ["exposed"],
    "Subconscious Hall": ["exposed"],
    # Dimensional
    "Dimension Rift": ["air", "exposed"],
    "Multiverse Hall": ["air", "exposed"],
    "Quantum Chamber": ["air", "exposed"],
    # Celestial / divine
    "Sacred Altar": ["fire", "exposed"],
    "Heavenly Chamber": ["air", "exposed"],
    "Celestial Sanctum": ["air", "exposed"],
    "Eternal Hall": ["air", "exposed"],
}

FIRE_RE = re.compile(
    r"\b(lava|magma|volcanic|ember|flame|fire|steam|sacred flame|ash|scorch|heat|solar)\b",
    re.I,
)
EARTH_RE = re.compile(
    r"\b(stone|rock|sand|dune|cave|cavern|tunnel|mine|crypt|tomb|burial|"
    r"crystal|geode|gear|clockwork|cog|pillar|ruin|debris|mountain|alpine|rocky|echo)\b",
    re.I,
)
WATER_RE = re.compile(
    r"\b(water|lake|frozen|ice|glacial|frost|ocean|coral|abyssal|tidal|swamp|"
    r"bog|marsh|toxic pool|quicksand|spring|oasis|flood)\b",
    re.I,
)
AIR_RE = re.compile(
    r"\b(wind|storm|thunder|tempest|hurricane|lightning|cosmic|stellar|nebula|"
    r"galaxy|gravity|mirage|time|temporal|dimension|quantum|celestial|heavenly|divine)\b",
    re.I,
)
SCORCHED_RE = re.compile(r"\b(scorch|desert|dune|mirage|volcanic|lava|magma|ash)\b", re.I)
FLOODED_RE = re.compile(r"\b(swamp|bog|marsh|flood|underwater|ocean|abyssal|coral|toxic pool)\b", re.I)
OVERGROWN_RE = re.compile(
    r"\b(forest|grove|meadow|wildflower|vine|thorn|wolf|bear|tree|wood|spring|lush)\b",
    re.I,
)
EXPOSED_RE = re.compile(
    r"\b(crypt|tomb|shadow|void|darkness|arcane|magic|spell|dream|nightmare|"
    r"ruin|decay|exposed|open|chamber|hall)\b",
    re.I,
)

FIRE_ACTIONS = re.compile(
    r"\b(Lava|Steam|Sacred Flames|Ash|Fire|Magma)\b",
    re.I,
)
WATER_ACTIONS = re.compile(
    r"\b(Ice|Frost|Frozen|Ocean|Tidal|Healing Waters|Quicksand)\b",
    re.I,
)
AIR_ACTIONS = re.compile(
    r"\b(Lightning|Wind|Storm|Blizzard|Cosmic|Gravity|Stellar)\b",
    re.I,
)
EARTH_ACTIONS = re.compile(
    r"\b(Falling Rocks|Rockfall|Cave-in|Crystal|Gear|Rockfall|Avalanche)\b",
    re.I,
)
OVERGROWN_ACTIONS = re.compile(
    r"\b(Forest Spirits|Thorn Vines|Falling Branch|Nature's Blessing|Pollen)\b",
    re.I,
)


def _action_blob(room: dict) -> str:
    names = [a.get("name", "") for a in room.get("actions") or []]
    return " ".join(names)


def infer_tags(room: dict) -> list[str]:
    location = (room.get("location") or room.get("name") or "").strip()
    if location in LOCATION_TAGS:
        raw = LOCATION_TAGS[location]
    else:
        desc = room.get("description") or ""
        actions = _action_blob(room)
        text = f"{location} {desc} {actions}"
        raw = []
        if FIRE_RE.search(text) or FIRE_ACTIONS.search(actions):
            raw.append("fire")
        if EARTH_RE.search(text) or EARTH_ACTIONS.search(actions):
            raw.append("earth")
        if WATER_RE.search(text) or WATER_ACTIONS.search(actions):
            raw.append("water")
        if AIR_RE.search(text) or AIR_ACTIONS.search(actions):
            raw.append("air")
        if SCORCHED_RE.search(text):
            raw.append("scorched")
        if FLOODED_RE.search(text):
            raw.append("flooded")
        if OVERGROWN_RE.search(text) or OVERGROWN_ACTIONS.search(actions):
            raw.append("overgrown")
        if EXPOSED_RE.search(text) or not raw:
            raw.append("exposed")

    seen: set[str] = set()
    ordered: list[str] = []
    for tag in TAG_ORDER:
        if tag in raw and tag not in seen:
            seen.add(tag)
            ordered.append(tag)
    for tag in raw:
        if tag not in seen and tag in VALID_TAGS:
            seen.add(tag)
            ordered.append(tag)
    return ordered


def main() -> None:
    rooms = json.loads(ROOMS_PATH.read_text(encoding="utf-8"))
    updated = 0
    for room in rooms:
        tags = infer_tags(room)
        if room.get("tags") != tags:
            room["tags"] = tags
            updated += 1
    ROOMS_PATH.write_text(json.dumps(rooms, indent=2, ensure_ascii=False) + "\n", encoding="utf-8")
    print(f"Updated {updated} / {len(rooms)} rooms in {ROOMS_PATH.relative_to(ROOT)}")


if __name__ == "__main__":
    main()
