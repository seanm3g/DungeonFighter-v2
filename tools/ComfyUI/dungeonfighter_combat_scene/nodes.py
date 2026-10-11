"""
ComfyUI nodes that pull DungeonFighter combat-scene JSON from the live localhost feed.

Default feed (while DF.exe / Avalonia game is running):
  http://127.0.0.1:7877/combat-scene
"""

from __future__ import annotations

import json
import urllib.error
import urllib.parse
import urllib.request
from typing import Any


DEFAULT_BASE = "http://127.0.0.1:7877"


def _get_json(url: str, timeout_s: float) -> dict[str, Any] | list[Any]:
    req = urllib.request.Request(url, headers={"Accept": "application/json", "Cache-Control": "no-cache"})
    with urllib.request.urlopen(req, timeout=timeout_s) as resp:
        raw = resp.read().decode("utf-8")
    return json.loads(raw) if raw else {}


def _as_envelope(payload: Any) -> dict[str, Any]:
    if isinstance(payload, dict) and "scenes" in payload:
        return payload
    if isinstance(payload, list):
        latest = payload[-1] if payload else {}
        return {
            "revision": 0,
            "beatIndex": len(payload) - 1 if payload else -1,
            "beatCount": len(payload),
            "status": "unknown",
            "latest": latest if isinstance(latest, dict) else {},
            "scenes": payload,
        }
    if isinstance(payload, dict):
        return {
            "revision": 0,
            "beatIndex": 0,
            "beatCount": 1,
            "status": "unknown",
            "latest": payload,
            "scenes": [payload],
        }
    return {
        "revision": 0,
        "beatIndex": -1,
        "beatCount": 0,
        "status": "idle",
        "latest": {},
        "scenes": [],
    }


def _join_effects(latest: dict[str, Any]) -> str:
    effects = latest.get("effects") or []
    if not isinstance(effects, list):
        return str(effects)
    return ", ".join(str(x) for x in effects if x)


def _gear_json(latest: dict[str, Any]) -> str:
    gear = latest.get("gear")
    if gear is None:
        return "{}"
    return json.dumps(gear, ensure_ascii=False)


def _build_prompt(latest: dict[str, Any]) -> str:
    parts: list[str] = []
    for key in ("environment", "camera", "composition", "character", "action", "target", "emotion"):
        value = latest.get(key)
        if value:
            parts.append(str(value))
    gear = latest.get("gear") or {}
    if isinstance(gear, dict):
        for key in ("helmet", "cloak", "armor", "weapon_main", "weapon_offhand"):
            value = gear.get(key)
            if value:
                parts.append(str(value))
    effects = _join_effects(latest)
    if effects:
        parts.append(effects)
    return ", ".join(parts)


class DungeonFighterCombatSceneFetch:
    """Fetch the latest combat-scene envelope from DungeonFighter's live feed."""

    @classmethod
    def INPUT_TYPES(cls):
        return {
            "required": {
                "base_url": ("STRING", {"default": DEFAULT_BASE, "multiline": False}),
                "wait_for_new": ("BOOLEAN", {"default": True}),
                "since_revision": ("INT", {"default": 0, "min": 0, "max": 2_147_483_647, "step": 1}),
                "timeout_ms": ("INT", {"default": 25000, "min": 0, "max": 120000, "step": 100}),
            }
        }

    RETURN_TYPES = (
        "INT",
        "STRING",
        "STRING",
        "STRING",
        "STRING",
        "STRING",
        "STRING",
        "STRING",
        "STRING",
        "STRING",
        "STRING",
        "STRING",
        "STRING",
    )
    RETURN_NAMES = (
        "revision",
        "character",
        "action",
        "target",
        "environment",
        "emotion",
        "camera",
        "composition",
        "effects",
        "gear_json",
        "latest_json",
        "scenes_json",
        "prompt",
    )
    FUNCTION = "fetch"
    CATEGORY = "DungeonFighter"

    def fetch(self, base_url: str, wait_for_new: bool, since_revision: int, timeout_ms: int):
        root = (base_url or DEFAULT_BASE).rstrip("/")
        if wait_for_new:
            query = urllib.parse.urlencode(
                {"since": int(since_revision), "timeoutMs": int(timeout_ms)}
            )
            url = f"{root}/combat-scene/wait?{query}"
            timeout_s = max(1.0, (int(timeout_ms) / 1000.0) + 2.0)
        else:
            url = f"{root}/combat-scene"
            timeout_s = 10.0

        try:
            payload = _get_json(url, timeout_s)
        except (urllib.error.URLError, TimeoutError, json.JSONDecodeError) as exc:
            empty = "{}"
            err = f"feed error: {exc}"
            return (0, "", "", "", "", "", "", "", "", empty, empty, "[]", err)

        envelope = _as_envelope(payload)
        latest = envelope.get("latest") if isinstance(envelope.get("latest"), dict) else {}
        scenes = envelope.get("scenes") if isinstance(envelope.get("scenes"), list) else []
        revision = int(envelope.get("revision") or 0)

        return (
            revision,
            str(latest.get("character") or ""),
            str(latest.get("action") or ""),
            str(latest.get("target") or ""),
            str(latest.get("environment") or ""),
            str(latest.get("emotion") or ""),
            str(latest.get("camera") or ""),
            str(latest.get("composition") or ""),
            _join_effects(latest),
            _gear_json(latest),
            json.dumps(latest, ensure_ascii=False, indent=2),
            json.dumps(scenes, ensure_ascii=False, indent=2),
            _build_prompt(latest),
        )


NODE_CLASS_MAPPINGS = {
    "DungeonFighterCombatSceneFetch": DungeonFighterCombatSceneFetch,
}

NODE_DISPLAY_NAME_MAPPINGS = {
    "DungeonFighterCombatSceneFetch": "DF Combat Scene (Live Feed)",
}
