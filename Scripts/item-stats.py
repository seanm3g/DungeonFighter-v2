"""Aggregates an ITEMSTATS dump into distribution tables (rarity, slots, classes, materials, pools, families, tiers,
roll-ups, re-routes, actions, implicits, power by level) for the item roller dashboard.

Input:  DF.exe ITEMSTATS --dump items.json --levels 1-100 --count 100 --seed 1
Usage:  python -I Scripts/item-stats.py items.json out.json [--gamedata GameData]
Output: {"tables": {name: [row, ...]}, "meta": [...]} with this script's own text in meta.
"""
import argparse
import json
import os
from collections import Counter, defaultdict

RARITIES = ["Common", "Uncommon", "Rare", "Epic", "Legendary", "Mythic"]
POOLS = ["material", "stat", "mechanic", "action"]


def band(level):
    lo = (level - 1) // 10 * 10 + 1
    return f"{lo}-{lo + 9}"


def main():
    ap = argparse.ArgumentParser()
    ap.add_argument("dump")
    ap.add_argument("out")
    ap.add_argument("--gamedata", default=os.path.join(os.path.dirname(os.path.abspath(__file__)), "..", "GameData"))
    a = ap.parse_args()

    with open(a.dump, encoding="utf-8-sig") as f:
        dump = json.load(f)
    items = dump["items"]
    n = len(items)
    with open(os.path.join(a.gamedata, "ItemMods.json"), encoding="utf-8-sig") as f:
        mods = {fam["family"]: fam for fam in json.load(f)["families"]}

    def tier_info(line):
        fam = mods.get(line["family"])
        if not fam or not (1 <= line["tier"] <= len(fam["tiers"])):
            return None
        return fam["tiers"][line["tier"] - 1]

    def pct(c, total):
        """Percent of total, 2 decimals (the dashboard shows these as they are)."""
        return round(100 * c / total, 2) if total else 0

    def per(c, total):
        return round(c / total, 3) if total else 0

    tables = {}

    # Summary (one row).
    lines_all = [l for it in items for l in it["lines"]]
    mod_lines = [l for l in lines_all if l["pool"] != "action"]
    common_only = sum(1 for l in mod_lines if (tier_info(l) or {}).get("maxRarity") == "Common")
    tables["summary"] = [{
        "items": n,
        "levels": f'{min(i["lootLevel"] for i in items)}-{max(i["lootLevel"] for i in items)}',
        "avgLines": round(len(lines_all) / n, 2),
        "nonCommonPct": pct(sum(1 for i in items if i["rarity"] != "Common"), n),
        "rollUpItemsPct": pct(sum(1 for i in items if i["rollUps"]), n),
        "rolledUpLinesPct": pct(sum(1 for l in mod_lines if l["rolledUp"]), len(mod_lines)),
        "commonOnlyLines": common_only,
        "movedLines": sum(1 for l in lines_all if l.get("from")),
        "relaxedBases": sum(1 for i in items if i["relaxed"]),
    }]

    # Rarity overall and by level band.
    rc = Counter(i["rarity"] for i in items)
    tables["rarity"] = [{"rarity": r, "items": rc[r], "pct": pct(rc[r], n)} for r in RARITIES]
    by_band = defaultdict(Counter)
    for i in items:
        by_band[band(i["lootLevel"])][i["rarity"]] += 1
    tables["rarityByBand"] = [
        {"band": b, **{r: pct(c[r], sum(c.values())) for r in RARITIES}, "items": sum(c.values())}
        for b, c in sorted(by_band.items(), key=lambda kv: int(kv[0].split("-")[0]))]

    # Slot x class, materials.
    sc = Counter((i["slot"], i["cls"]) for i in items)
    tables["slotClass"] = [{"slot": s, "class": c, "items": v} for (s, c), v in sorted(sc.items())]
    mat = Counter(i["material"] for i in items)
    mat_w = Counter(i["material"] for i in items if i["slot"] == "weapon")
    tables["materials"] = [{"material": m, "items": v, "pct": pct(v, n), "weapons": mat_w[m], "armor": v - mat_w[m]}
                           for m, v in mat.most_common()]

    # Lines per item by rarity (count distribution) and avg lines per pool by rarity.
    lpi = defaultdict(Counter)
    pool_sum = defaultdict(Counter)
    for i in items:
        lpi[i["rarity"]][len(i["lines"])] += 1
        for l in i["lines"]:
            pool_sum[i["rarity"]][l["pool"]] += 1
    max_lines = max((k for c in lpi.values() for k in c), default=0)
    tables["linesPerItem"] = [{"rarity": r, "lines": k, "items": lpi[r][k], "pct": pct(lpi[r][k], rc[r])}
                              for r in RARITIES if rc[r] for k in range(max_lines + 1)]
    tables["poolsByRarity"] = [{"rarity": r, "pool": p, "avgLines": round(pool_sum[r][p] / rc[r], 2)}
                               for r in RARITIES if rc[r] for p in POOLS]

    # Families (mod lines).
    fam = defaultdict(lambda: {"lines": 0, "tierSum": 0, "valueSum": 0.0, "rolledUp": 0, "commonOnly": 0})
    for l in mod_lines:
        f = fam[l["family"]]
        f["lines"] += 1
        f["tierSum"] += l["tier"]
        f["valueSum"] += l["value"]
        f["rolledUp"] += 1 if l["rolledUp"] else 0
        f["commonOnly"] += 1 if (tier_info(l) or {}).get("maxRarity") == "Common" else 0
    tables["families"] = sorted(({
        "family": k, "pool": mods.get(k, {}).get("pool", ""), "kind": mods.get(k, {}).get("kind", ""),
        "lines": v["lines"], "perItem": per(v["lines"], n), "avgTier": round(v["tierSum"] / v["lines"], 2),
        "avgValue": round(v["valueSum"] / v["lines"], 3), "rolledUp": v["rolledUp"], "commonOnly": v["commonOnly"],
    } for k, v in fam.items()), key=lambda r: -r["lines"])

    # Pools: totals; tiers by pool (tier position as a share of the family ladder).
    pc = Counter(l["pool"] for l in lines_all)
    tables["pools"] = [{"pool": p, "lines": pc[p], "perItem": per(pc[p], n)} for p in POOLS]
    tier_band = defaultdict(lambda: [0, 0])
    for i in items:
        for l in i["lines"]:
            if l["pool"] == "action":
                continue
            fam_def = mods.get(l["family"])
            if not fam_def:
                continue
            ladder = len(fam_def["tiers"])
            tier_band[(band(i["lootLevel"]), l["pool"])][0] += l["tier"] / ladder
            tier_band[(band(i["lootLevel"]), l["pool"])][1] += 1
    tables["tierByBand"] = [{"band": b, "pool": p, "ladderPct": round(100 * s / c, 1), "lines": c}
                            for (b, p), (s, c) in sorted(tier_band.items(), key=lambda kv: (int(kv[0][0].split("-")[0]), kv[0][1]))]

    # Roll-ups and re-routes.
    ru = Counter()
    for i in items:
        for r in i["rollUps"]:
            ru[r.split(":")[0].split("+")[0].split(">")[0]] += 1
    names = {"ilvl": "Item level", "rarity": "Rarity", "pool": "Pool", "tier": "Tier"}
    tables["rollUps"] = [{"rollUp": names.get(k, k), "count": v, "perItem": per(v, n)} for k, v in ru.most_common()]
    rr = Counter((l["from"], l["pool"]) for l in lines_all if l.get("from"))
    tables["reroutes"] = [{"from": f, "to": t, "lines": v} for (f, t), v in rr.most_common()]

    # Actions and implicits (top 25 each).
    act = Counter(l["family"] for l in lines_all if l["pool"] == "action")
    tables["actions"] = [{"action": k, "lines": v, "perItem": per(v, n)} for k, v in act.most_common(25)]
    imp = Counter(i["implicitName"] or "(none)" for i in items)
    tables["implicits"] = [{"implicit": k, "items": v, "pct": pct(v, n)} for k, v in imp.most_common(25)]

    # Power by level band: weapon damage / armor defense.
    pw = defaultdict(list)
    for i in items:
        pw[(band(i["lootLevel"]), "weapon" if i["slot"] == "weapon" else "armor")].append(i["power"])
    tables["powerByBand"] = [{"band": b, "kind": k, "avg": round(sum(v) / len(v), 1), "min": min(v), "max": max(v), "items": len(v)}
                             for (b, k), v in sorted(pw.items(), key=lambda kv: (int(kv[0][0].split("-")[0]), kv[0][1]))]

    with open(os.path.abspath(__file__), encoding="utf-8") as f:
        code = f.read()
    meta = [
        {"label": "item-stats.py", "value": code, "type": "code", "lang": "python"},
        {"label": "Input", "value": "DF.exe ITEMSTATS --dump items.json --levels 1-100 --count 100 --seed 1 (100 items per loot level 1-100)"},
        {"label": "Run", "value": dump.get("provenance", "")},
    ]
    with open(a.out, "w", encoding="utf-8") as f:
        json.dump({"tables": tables, "meta": meta}, f, separators=(",", ":"))
    print(f"{n} items -> {a.out} ({', '.join(f'{k}:{len(v)}' for k, v in tables.items())})")


if __name__ == "__main__":
    main()
