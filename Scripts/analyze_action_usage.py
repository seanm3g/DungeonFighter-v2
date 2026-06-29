#!/usr/bin/env python3
"""Scan GameData + Code for which Actions.json rows are explicitly referenced."""
import json
import re
from collections import Counter
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]


def norm(s):
    return (s or "").strip().upper()


def add_ref(refs, name, source):
    if not name:
        return
    refs.setdefault(norm(name), set()).add(source)


def main():
    actions_path = ROOT / "GameData" / "Actions.json"
    with open(actions_path, encoding="utf-8") as f:
        actions = json.load(f)

    all_actions = {}
    modtrade = []
    for a in actions:
        name = a.get("action") or a.get("name") or ""
        key = norm(name)
        all_actions[key] = a
        tags = (a.get("tags") or "")
        cat = (a.get("category") or "")
        if "modtrade" in tags.lower() or cat == "ModTrade":
            modtrade.append(name)

    print(f"Total actions in Actions.json: {len(actions)}")
    print(f"ModTrade actions: {len(modtrade)}")

    refs = {}

    with open(ROOT / "GameData" / "Enemies.json", encoding="utf-8") as f:
        for e in json.load(f):
            for act in e.get("actions", []):
                add_ref(refs, act, "Enemies.json")

    with open(ROOT / "GameData" / "ClassActions.json", encoding="utf-8") as f:
        ca = json.load(f)
    for r in ca.get("rules", []):
        add_ref(refs, r.get("actionName"), "ClassActions.json")

    with open(ROOT / "GameData" / "ActionTables.json", encoding="utf-8") as f:
        at = json.load(f)
    for cls, tbl in at.get("classTables", {}).items():
        for act in tbl.get("actions", []):
            add_ref(refs, act.get("name"), f"ActionTables.json/{cls}")
    for act in at.get("armorActions", []):
        add_ref(refs, act.get("name"), "ActionTables.json/armor")

    with open(ROOT / "GameData" / "Rooms.json", encoding="utf-8") as f:
        rooms = json.load(f)
    for room in rooms:
        for act in room.get("actions", []):
            if isinstance(act, dict):
                add_ref(refs, act.get("name"), "Rooms.json")
            else:
                add_ref(refs, act, "Rooms.json")

    for a in actions:
        if a.get("isDefaultAction") or a.get("IsDefaultAction"):
            add_ref(refs, a.get("action") or a.get("name"), "Actions.json:isDefaultAction")

    action_names_upper = set(all_actions.keys())
    for p in (ROOT / "Code").rglob("*.cs"):
        try:
            text = p.read_text(encoding="utf-8", errors="ignore")
        except OSError:
            continue
        for m in re.finditer(r'"([A-Z][A-Z0-9 \'\-\u2019]+)"', text):
            s = norm(m.group(1))
            if s in action_names_upper and len(s) > 2:
                add_ref(refs, m.group(1), f"Code:{p.relative_to(ROOT)}")

    with open(ROOT / "GameData" / "EnvironmentalActions.json", encoding="utf-8") as f:
        env = json.load(f)

    explicitly_used = set(refs.keys())
    not_in_actions = [k for k in refs if k not in all_actions]

    unused = []
    modtrade_unused = []
    modtrade_used = []
    for k, a in all_actions.items():
        name = a.get("action") or a.get("name")
        if k not in explicitly_used:
            unused.append(name)
        if norm(name) in {norm(x) for x in modtrade}:
            if k in explicitly_used:
                modtrade_used.append(name)
            else:
                modtrade_unused.append(name)

    print(f"Explicitly referenced actions: {len(explicitly_used)}")
    print(f"Unused (no explicit ref): {len(unused)}")
    print(f"ModTrade unused: {len(modtrade_unused)}")
    print(f"ModTrade used: {len(modtrade_used)}")
    if modtrade_used:
        print("ModTrade USED:", modtrade_used)
    print(f"Refs to unknown actions: {len(not_in_actions)}")
    if not_in_actions:
        print("Unknown refs sample:", sorted(not_in_actions)[:20])

    cat_counts = Counter()
    for a in actions:
        name = a.get("action") or a.get("name")
        if norm(name) in unused:
            cat_counts[a.get("category") or "(none)"] += 1
    print("\nUnused by category:")
    for cat, cnt in cat_counts.most_common(20):
        print(f"  {cat}: {cnt}")

    # Tag-pool reachable (loot fallback) but not explicitly listed
    tag_pool = []
    for a in actions:
        tags = (a.get("tags") or "").lower()
        name = a.get("action") or a.get("name")
        if norm(name) in explicitly_used:
            continue
        if any(t in tags for t in ("weapon", "class", "item", "action")) and "modtrade" not in tags:
            tag_pool.append(name)

    print(f"\nTag-pool reachable but unreferenced: {len(tag_pool)}")

    # Environmental actions file stats
    env_ids = {norm(e.get("id", "")) for e in env}
    env_names = {norm(e.get("name", "")) for e in env}
    room_names = {k for k in refs if "Rooms.json" in refs[k]}
    room_not_in_env = sorted(room_names - env_ids - env_names)
    print(f"\nEnvironmentalActions.json entries: {len(env)}")
    print(f"Rooms.json action names: {len(room_names)}")
    print(f"Room actions not in EnvironmentalActions.json: {len(room_not_in_env)}")
    if room_not_in_env:
        print("  sample:", room_not_in_env[:15])

    report = {
        "total": len(actions),
        "modtrade_total": len(modtrade),
        "modtrade_unused": sorted(modtrade_unused),
        "modtrade_used": sorted(modtrade_used),
        "unused_count": len(unused),
        "unused": sorted(unused),
        "explicitly_used_count": len(explicitly_used),
        "explicitly_used": sorted(
            all_actions[k].get("action") or all_actions[k].get("name") for k in explicitly_used if k in all_actions
        ),
        "tag_pool_unreferenced": sorted(tag_pool),
        "room_actions_not_in_env": room_not_in_env,
        "refs_by_action": {k: sorted(v) for k, v in sorted(refs.items())},
    }
    out = ROOT / "Scripts" / "action_usage_report.json"
    with open(out, "w", encoding="utf-8") as f:
        json.dump(report, f, indent=2)
    print(f"\nWrote report to {out}")


if __name__ == "__main__":
    main()
