#!/usr/bin/env python3
"""Remove ModTrade and other unreferenced rows from Actions.json (core-only trim)."""
import json
import re
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
ACTIONS_PATH = ROOT / "GameData" / "Actions.json"
ENV_PATH = ROOT / "GameData" / "EnvironmentalActions.json"
REPORT_PATH = ROOT / "Scripts" / "action_trim_report.json"


def norm(s):
    return (s or "").strip().upper()


def action_name(row):
    return row.get("action") or row.get("name") or ""


def is_modtrade(row):
    tags = (row.get("tags") or "").lower()
    cat = (row.get("category") or "")
    return "modtrade" in tags or cat == "ModTrade"


def collect_keep_names():
    keep = set()

    def add(name, _source):
        if name:
            keep.add(norm(name))

    with open(ROOT / "GameData" / "Enemies.json", encoding="utf-8") as f:
        for e in json.load(f):
            for act in e.get("actions", []):
                add(act, "Enemies.json")

    with open(ROOT / "GameData" / "ClassActions.json", encoding="utf-8") as f:
        for r in json.load(f).get("rules", []):
            add(r.get("actionName"), "ClassActions.json")

    with open(ROOT / "GameData" / "ActionTables.json", encoding="utf-8") as f:
        at = json.load(f)
    for tbl in at.get("classTables", {}).values():
        for act in tbl.get("actions", []):
            add(act.get("name"), "ActionTables.json")
    for act in at.get("armorActions", []):
        add(act.get("name"), "ActionTables.json/armor")

    with open(ROOT / "GameData" / "Rooms.json", encoding="utf-8") as f:
        for room in json.load(f):
            for act in room.get("actions", []):
                if isinstance(act, dict):
                    add(act.get("name"), "Rooms.json")
                else:
                    add(act, "Rooms.json")

    actions = json.loads(ACTIONS_PATH.read_text(encoding="utf-8"))
    for row in actions:
        if row.get("isDefaultAction") or row.get("IsDefaultAction"):
            add(action_name(row), "Actions.json:isDefaultAction")

    action_keys = {norm(action_name(r)) for r in actions}
    for p in (ROOT / "Code").rglob("*.cs"):
        try:
            text = p.read_text(encoding="utf-8", errors="ignore")
        except OSError:
            continue
        for m in re.finditer(r'"([A-Z][A-Z0-9 \'\-\u2019]+)"', text):
            s = norm(m.group(1))
            if s in action_keys and len(s) > 2:
                add(m.group(1), f"Code:{p.relative_to(ROOT)}")

    return keep, actions


def main():
    keep, actions = collect_keep_names()
    kept_rows = []
    removed = []

    for row in actions:
        name = action_name(row)
        key = norm(name)
        if is_modtrade(row):
            removed.append({"name": name, "reason": "ModTrade"})
            continue
        if key in keep:
            kept_rows.append(row)
        else:
            removed.append({"name": name, "reason": "unreferenced"})

    ACTIONS_PATH.write_text(json.dumps(kept_rows, indent=2) + "\n", encoding="utf-8")

    env_removed = []
    env_kept = []
    if ENV_PATH.exists():
        env_rows = json.loads(ENV_PATH.read_text(encoding="utf-8"))
        kept_names = {norm(action_name(r)) for r in kept_rows}
        for row in env_rows:
            ids = {norm(row.get("id")), norm(row.get("name"))}
            if ids & kept_names:
                env_kept.append(row)
            else:
                env_removed.append(row.get("name") or row.get("id"))

        ENV_PATH.write_text(json.dumps(env_kept, indent=2) + "\n", encoding="utf-8")

    report = {
        "actions_before": len(actions),
        "actions_after": len(kept_rows),
        "actions_removed": len(removed),
        "modtrade_removed": sum(1 for r in removed if r["reason"] == "ModTrade"),
        "unreferenced_removed": sum(1 for r in removed if r["reason"] == "unreferenced"),
        "removed_sample": removed[:40],
        "environmental_before": len(env_rows) if ENV_PATH.exists() else 0,
        "environmental_after": len(env_kept),
        "environmental_removed": len(env_removed),
    }
    REPORT_PATH.write_text(json.dumps(report, indent=2), encoding="utf-8")

    print(f"Actions.json: {report['actions_before']} -> {report['actions_after']} "
          f"(removed {report['actions_removed']}, modtrade {report['modtrade_removed']})")
    print(f"EnvironmentalActions.json: {report['environmental_before']} -> {report['environmental_after']}")
    print(f"Report: {REPORT_PATH}")


if __name__ == "__main__":
    main()
