"""Aggregates an ITEMSTATS dump into distribution tables (rarity, slots, classes, materials, pools, families, tiers,
roll-ups, re-routes, actions, implicits, power by level, anomalies) for the item roller dashboard.

Input:  DF.exe ITEMSTATS --dump items.json --levels 1-100 --count 100 --seed 1
Usage:  python -I Scripts/item-stats.py items.json out.json [--gamedata GameData]
Output: {"tables": {name: [row, ...]}, "meta": [...]} with this script's own text in meta.
"""
import argparse
import json
import os
import random
from collections import Counter, defaultdict

RARITIES = ["Common", "Uncommon", "Rare", "Epic", "Legendary", "Mythic"]
POOLS = ["material", "stat", "mechanic", "action"]


def band(level):
    lo = (level - 1) // 10 * 10 + 1
    return f"{lo}-{lo + 9}"


ANOMALY_LIMIT = 150


def find_anomalies(items, tier_info, describe):
    """Tags items that rolled something unusual. Each tag is a rule over one item; an item's score is the sum of
    -log10(share of items carrying each of its tags), so rare tags and stacked tags rank first. Returns the top
    ANOMALY_LIMIT items (with tags and the reason for each) and one row per tag with how often it fired."""
    import math
    import statistics

    n = len(items)
    pools_of = lambda i, p: [l for l in i["lines"] if l["pool"] == p]

    # Per-rarity line-count cut: the lowest count (above the mode) that at most 1.5% of that rarity's items reach.
    by_rarity = defaultdict(Counter)
    for i in items:
        by_rarity[i["rarity"]][len(i["lines"])] += 1
    line_cut = {}
    for r, counts in by_rarity.items():
        total = sum(counts.values())
        mode = counts.most_common(1)[0][0]
        line_cut[r] = next((c for c in range(mode + 1, max(counts) + 2)
                            if sum(v for k, v in counts.items() if k >= c) <= 0.015 * total), max(counts) + 1)

    # Power z-score within the same 10-level band and weapon type / armor slot.
    groups = defaultdict(list)
    key = lambda i: (band(i["lootLevel"]), i.get("weaponType") or i["slot"])
    for i in items:
        groups[key(i)].append(i["power"])
    stats = {k: (statistics.mean(v), statistics.pstdev(v)) for k, v in groups.items()}

    route_counts = Counter((l["from"], l["pool"]) for i in items for l in i["lines"] if l.get("from"))

    def tags_of(i):
        t = {}
        if RARITIES.index(i["rarity"]) >= 3:
            t[f'{i["rarity"]} drop'] = f'rolled {i["rarity"]}'
        rar_ups = [r for r in i["rollUps"] if r.startswith("rarity")]
        if len(rar_ups) >= 2:
            t["Rarity rolled up twice"] = ", ".join(rar_ups)
        il = i["itemLevel"] - i["lootLevel"]
        if il >= 2:
            t["Item level +2 or more"] = f'item level {i["itemLevel"]} from loot level {i["lootLevel"]}'
        if len(i["rollUps"]) >= 3:
            t["Roll-up chain (3+)"] = ", ".join(i["rollUps"])
        if len(i["lines"]) >= line_cut[i["rarity"]]:
            t["Extra lines for rarity"] = f'{len(i["lines"])} lines; 98.5%+ of {i["rarity"]} items have fewer than {line_cut[i["rarity"]]}'
        for pool, cut, name in (("material", 2, "Stacked material lines"), ("mechanic", 2, "Stacked mechanic lines"),
                                ("action", 3, "3+ gear actions")):
            k = len(pools_of(i, pool))
            if k >= cut:
                t[name] = f"{k} {pool} lines"
        jumps = [r for r in i["rollUps"] if r.startswith("tier:") and not r.endswith("+1")]
        if jumps:
            t["Tier jumped 2+"] = ", ".join(jumps)
        for l in i["lines"]:
            if l.get("from") and route_counts[(l["from"], l["pool"])] < 0.005 * n:
                t["Rare pool move"] = f'{l["family"]}: {l["from"]} → {l["pool"]}'
        mean, sd = stats[key(i)]
        if sd > 0:
            z = (i["power"] - mean) / sd
            what = f'{i.get("weaponType") or i["slot"]} in levels {band(i["lootLevel"])}'
            if z >= 3:
                t["Power high for level"] = f'power {i["power"]} vs avg {mean:.0f} for a {what} ({z:+.1f} sd)'
            elif z <= -3:
                t["Power low for level"] = f'power {i["power"]} vs avg {mean:.0f} for a {what} ({z:+.1f} sd)'
        mod_lines = [l for l in i["lines"] if l["pool"] != "action" and tier_info(l)]
        tops = 0
        for l in mod_lines:
            ti = tier_info(l)
            span = ti["max"] - ti["min"]
            if span > 0 and (l["value"] - ti["min"]) / span >= 0.9:
                tops += 1
        if len(mod_lines) >= 2 and tops == len(mod_lines):
            t["Near-perfect rolls"] = f"all {tops} mod lines in the top 10% of their tier's range"
        return t

    tagged = [(i, tags_of(i)) for i in items]
    freq = Counter(name for _, t in tagged for name in t)
    weight = {name: -math.log10(c / n) for name, c in freq.items()}
    ranked = sorted(((sum(weight[x] for x in t), i, t) for i, t in tagged if t), key=lambda s: -s[0])

    rows = [{
        "id": k + 1,
        "score": round(score, 2),
        "tags": " · ".join(sorted(t, key=lambda x: -weight[x])),
        "why": "\n".join(f"{name}: {t[name]}" for name in sorted(t, key=lambda x: -weight[x])),
        "item": i["name"],
        "rarity": i["rarity"],
        "itemLevel": i["itemLevel"],
        "lootLevel": i["lootLevel"],
        "slot": i["slot"],
        "class": i["cls"],
        "material": i["material"],
        "power": i["power"],
        "lines": "\n".join(describe(l) for l in i["lines"]),
    } for k, (score, i, t) in enumerate(ranked[:ANOMALY_LIMIT])]
    tag_rows = [{"tag": name, "items": c, "pct": round(100 * c / n, 2), "weight": round(weight[name], 2)}
                for name, c in sorted(freq.items(), key=lambda kv: kv[1])]
    return rows, tag_rows, len(ranked)


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

    # 100 sample items picked at random (fixed seed) from the dump, shown whole, lowest item level first.
    def describe(l):
        if l["pool"] == "action":
            return f'action: {l["keyword"]}'
        up = " (rolled up)" if l["rolledUp"] else ""
        moved = f' (from {l["from"]})' if l.get("from") else ""
        return f'{l["pool"]}: {l["keyword"]} = {l["value"]:g} [{l["family"]} T{l["tier"]}]{up}{moved}'

    picks = sorted(random.Random(7).sample(items, min(100, n)), key=lambda i: (i["itemLevel"], i["name"]))
    tables["samples"] = [{
        "id": k + 1,
        "item": i["name"],
        "rarity": i["rarity"],
        "itemLevel": i["itemLevel"],
        "slot": i["slot"],
        "class": i["cls"],
        "base": i["baseName"],
        "tier": i["tier"],
        "material": i["material"],
        "power": i["power"],
        "implicit": i["implicitName"] or "",
        "lines": "\n".join(describe(l) for l in i["lines"]),
        "rollUps": ", ".join(i["rollUps"]),
    } for k, i in enumerate(picks)]

    anomalies, anomaly_tags, anomalous = find_anomalies(items, tier_info, describe)
    tables["summary"][0]["anomalousItems"] = anomalous
    tables["summary"][0]["anomalousPct"] = pct(anomalous, n)
    tables["anomalies"] = anomalies
    tables["anomalyTags"] = anomaly_tags

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
