"""Builds the item generation data for the 2026-11-14 redesign (Documentation/ITEM_GENERATION_REDESIGN_2026-11-14.md).

Writes / updates, in GameData/:
  ItemMods.json     mod families with tier ladders (keyword, value range, item level, weight, rarity requirement)
  Weapons.json      + class, damageMin / damageMax, triggerName (the base item's implicit)
  Armor.json        + class, defenseMin / defenseMax, triggerName (implicit)
  RarityTable.json  + MinLevel, Pools, MaxLines, Explode (old StatBonuses / ActionBonuses / Modifications columns dropped)

Sources: StatBonuses.json (stat suffixes + animal suffixes), Modifications.json (adjectives, RULE / CROSSWIRE
prefixes, affix templates), PrefixMaterialQuality.json (Quality), Triggers.json (implicit candidates).

Re-run after editing the ladders below; it overwrites ItemMods.json and the generated catalog columns.
Usage: python -I Scripts/build-item-mods.py [--gamedata GameData]
"""
import argparse
import json
import os
from collections import Counter, defaultdict

CLASSES = ["barbarian", "warrior", "rogue", "wizard"]
ATTR_CLASS = {"strength": "barbarian", "agility": "warrior", "technique": "rogue", "intelligence": "wizard"}

# Normal ladder: 7 tiers. (item level, value multiplier range on the family's base value, weight)
STAT_LADDER = [
    (1, 0.5, 0.8, 100), (6, 0.8, 1.1, 140), (14, 1.1, 1.5, 180), (25, 1.5, 1.9, 220),
    (40, 1.9, 2.4, 260), (60, 2.4, 2.9, 300), (80, 2.9, 3.4, 340),
]
# Common-only tier above each normal tier: same level, values past that tier's max, rarer.
EXCEPTIONAL_LOW, EXCEPTIONAL_HIGH, EXCEPTIONAL_WEIGHT = 1.05, 1.5, 0.25
STATUS_LADDER = [(3, 0.6, 1.0, 100), (12, 1.0, 1.4, 140), (25, 1.4, 1.9, 180), (45, 1.9, 2.5, 220), (70, 2.5, 3.2, 260)]
RULE_LADDER = [(8, 1.0, 1.0, 100), (30, 1.4, 1.4, 140), (60, 1.8, 1.8, 180)]
MATERIAL_LADDER = [(1, 1.0, 1.0, 100), (25, 1.4, 1.4, 140), (55, 1.8, 1.8, 180)]
RULE_WORDS = ["", "Greater", "Grand"]

SUFFIX_STATS = {
    # StatType: (family, keyword ladder (7), "of the" or "of", description, tags, integer)
    "Health": ("Life", ["Sprout", "Sapling", "Oak", "Elder", "Ancient", "Ironbark", "Worldroot"], "of the", "+{n}% max health", ["health", "defense", "barbarian", "ontakehit"], False),
    "STR": ("Strength", ["Ox", "Bear", "Bull", "Mammoth", "Titan", "Giant", "Colossus"], "of the", "+{n}% strength", ["str", "strength", "barbarian"], False),
    "AGI": ("Agility", ["Hare", "Fox", "Hawk", "Panther", "Falcon", "Wind", "Tempest"], "of the", "+{n}% agility", ["agi", "agility", "warrior"], False),
    "TEC": ("Technique", ["Apprentice", "Journeyman", "Artisan", "Craftsman", "Master", "Virtuoso", "Paragon"], "of the", "+{n}% technique", ["tec", "technique", "rogue"], False),
    "INT": ("Intelligence", ["Candle", "Lamp", "Lantern", "Beacon", "Star", "Comet", "Sun"], "of the", "+{n}% intelligence", ["int", "intelligence", "wizard"], False),
    "Damage": ("Damage", ["Thorns", "Spikes", "Fangs", "Claws", "Slaughter", "Carnage", "Ruin"], "of", "+{n}% damage", ["damage", "offense", "onconnect"], False),
    "Armor": ("Defense", ["Hide", "Shell", "Scale", "Plate", "Ramparts", "Bastions", "Citadels"], "of", "+{n} defense", ["armor", "defense", "ontakehit"], False),
    "HIT": ("Accuracy", ["Aim", "Sight", "Focus", "Precision", "Accuracy", "Truth", "Certainty"], "of", "+{n} hit", ["hit", "accuracy", "onconnect", "onmiss"], True),
    "CRIT": ("Critical", ["Edges", "Wounds", "Ferocity", "Savagery", "Butchery", "Execution", "Annihilation"], "of", "+{n} crit", ["crit", "oncritical", "rogue"], True),
    "COMBO": ("Combo", ["Rhythm", "Flow", "Cadence", "Tempo", "Harmony", "Symphony", "Crescendo"], "of", "+{n} combo", ["combo", "oncombo", "oncomboend", "warrior"], True),
    "AttackSpeed": ("Attack Speed", ["Haste", "Quickness", "Swiftness", "Alacrity", "Celerity", "Speed", "Lightning"], "of", "+{n} attack speed", ["speed", "onturn", "rogue", "warrior"], False),
    "HealthRegen": ("Regeneration", ["Mending", "Renewal", "Restoration", "Regrowth", "Rebirth", "Vigor", "Eternity"], "of", "+{n} health regen", ["regen", "health", "defense"], False),
    "MagicFind": ("Magic Find", ["Coins", "Gold", "Treasure", "Fortune", "Riches", "Plenty", "Avarice"], "of", "+{n} magic find", ["luck", "magicfind"], False),
}

PREFIX_EFFECTS = {
    # Modification Effect: (family, ladder (7), tags, integer)
    "DAMAGE": ("Brutality", ["Sharp", "Jagged", "Vicious", "Savage", "Brutal", "Cruel", "Merciless"], ["damage", "offense", "barbarian"], False),
    "ARMOR": ("Toughness", ["Padded", "Sturdy", "Reinforced", "Hardened", "Bulwark", "Fortified", "Impregnable"], ["armor", "defense", "ontakehit"], False),
    "HEALTH BONUS": ("Vitality", ["Hale", "Hearty", "Robust", "Vigorous", "Ancient", "Undying", "Eternal"], ["health", "defense", "barbarian"], False),
    "HIT": ("Aim", ["Steady", "Short", "Swift", "True", "Unerring", "Deadeye", "Fated"], ["hit", "accuracy", "onconnect"], False),
    "CRIT": ("Menace", ["Long", "Charged", "Wicked", "Deadly", "Lethal", "Murderous", "Ruinous"], ["crit", "oncritical", "rogue"], False),
    "COMBO": ("Poise", ["Balanced", "Precise", "Flowing", "Measured", "Seamless", "Masterful", "Perfect"], ["combo", "oncombo", "warrior"], False),
    "SPEED": ("Swiftness", ["Light", "Nimble", "Featherweight", "Quick", "Fleet", "Blurred", "Timeless"], ["speed", "onturn", "warrior"], False),
    "MULTI-HIT": ("Flurry", ["Acrobatic", "Twin", "Flurrying", "Whirling", "Storming", "Myriad", "Endless"], ["multihit", "onconnect", "rogue"], False),
    "MAGIC FIND": ("Fortune", ["Enchanted", "Blessed", "Charmed", "Gilded", "Jeweled", "Fabled", "Destined"], ["luck", "magicfind"], False),
    "AMP": ("Amplify", ["Refined", "Tuned", "Focused", "Resonant", "Amplified", "Overcharged", "Transcendent"], ["amp", "wizard", "oncombo"], False),
}

STATUS_EFFECTS = {
    "BURN": ("Burning", ["Smoldering", "Flaming", "Blazing", "Searing", "Infernal"], ["burn", "fire", "onconnect", "wizard"]),
    "POISON": ("Venom", ["Tainted", "Poisonous", "Venomous", "Toxic", "Virulent"], ["poison", "onconnect", "rogue"]),
    "BLEED": ("Bleeding", ["Notched", "Serrated", "Barbed", "Rending", "Exsanguinating"], ["bleed", "onconnect", "barbarian"]),
}

MATERIAL_TEMPLATES = {"LISTEN", "TUNE", "STORECOND", "COUNTSDOUBLE"}
MECHANIC_TEMPLATES = {"RULE", "CROSSWIRE", "OPENER", "FINISHER", "BLOODPRICE"}

QUALITY_LADDER = [  # keyword, min, max, level, weight
    ("Worn", 1.02, 1.05, 1, 100), ("Second Hand", 1.05, 1.08, 8, 140), ("Like New", 1.08, 1.12, 18, 180),
    ("New", 1.12, 1.18, 30, 220), ("Perfect", 1.18, 1.25, 45, 260), ("Masterwork", 1.25, 1.35, 60, 300),
    ("Heirloom", 1.35, 1.50, 80, 340), ("Cosmic", 2.0, 2.5, 101, 1),
]

IMPLICITS = {
    "barbarian": ["HurtPride", "HurtHarden", "BarbarianActionDamage", "BarbarianBlood", "StrCleave", "BloodlustCrit",
                  "KillSpray", "KillReload20", "BludgeonChorus", "EarthBrand", "ClutchIron", "DepthHunger",
                  "PrimaryPulse", "MarchCadence", "GuardUp", "FoeOvercommit", "ClassPrimaryAttr"],
    "warrior": ["ComboSpray", "EndBreathDamage", "ComboThresholdPush", "OpenTheLane", "SharpenedEdge", "DoubleBeat",
                "FinisherEncore", "ComboWeaken", "EnemyFlinch", "WarriorActionAmp", "WarriorPointsGuard",
                "ClassWarriorPrimary", "FootworkGreaves", "FinisherSeal", "ComboEndHeal", "LoopChain", "EchoStep"],
    "rogue": ["CritAmpCarry", "CritThresholdPush", "CritExpose", "CritFace19", "CritFace18", "ConnectPierce",
              "PunishDot", "ObsidianEdge", "RogueActionAmp", "RoguePointsSpeed", "AgiSkirmish", "SwiftSchool",
              "QuickGrip", "SkipFiller", "LeapAhead", "SlotTwoEncore", "MirrorSwingDamage", "BossNeedle"],
    "wizard": ["FirstBloodAmp", "WizardActionAmp", "WizardPointsAmp", "IntArchitect", "FocusLens", "InsightClarity",
               "Firebrand", "WaterBrand", "AirBrand", "SteadyFocus", "OpenerCrown", "LuckySevenHit",
               "ExactFifteenCombo", "EvenRollHeal", "OddSpite", "ChaosOpen", "ReplaceNextStrip", "SpiteFocus"],
}

RARITY_COLUMNS = {
    # MinLevel, Pools {pool: [min, max]}, MaxLines, Explode
    "Common": (1, {"material": [0, 1], "stat": [1, 2], "mechanic": [0, 1], "action": [1, 1]}, 3, 0.25),
    "Uncommon": (1, {"material": [0, 1], "stat": [1, 2], "mechanic": [0, 1], "action": [1, 2]}, 4, 0.30),
    "Rare": (1, {"material": [1, 1], "stat": [2, 3], "mechanic": [0, 2], "action": [1, 2]}, 6, 0.35),
    "Epic": (5, {"material": [1, 2], "stat": [2, 3], "mechanic": [1, 2], "action": [1, 2]}, 7, 0.40),
    "Legendary": (15, {"material": [1, 2], "stat": [3, 4], "mechanic": [1, 2], "action": [1, 3]}, 8, 0.45),
    "Mythic": (15, {"material": [1, 2], "stat": [3, 4], "mechanic": [1, 3], "action": [1, 3]}, 10, 0.50),
}


def load(gd, name):
    with open(os.path.join(gd, name), encoding="utf-8-sig") as f:
        return json.load(f)


def save(gd, name, data, compact_rows=False):
    path = os.path.join(gd, name)
    if compact_rows:
        text = "[\r\n" + ",\r\n".join("  " + json.dumps(r, ensure_ascii=False) for r in data) + "\r\n]\r\n"
    else:
        text = json.dumps(data, indent=2, ensure_ascii=False).replace("\n", "\r\n") + "\r\n"
    with open(path, "w", encoding="utf-8", newline="") as f:
        f.write(text)


def rnd(v, integer):
    return max(1, int(round(v))) if integer else round(v, 2)


def ladder_tiers(base, ladder, keywords, fmt, integer, exceptional):
    tiers = []
    for i, (level, lo, hi, weight) in enumerate(ladder):
        tmin, tmax = rnd(base * lo, integer), rnd(base * hi, integer)
        tmax = max(tmax, tmin)
        tiers.append({"keyword": fmt(keywords[i]), "min": tmin, "max": tmax, "level": level, "weight": weight})
        if exceptional:
            emin = rnd(tmax * EXCEPTIONAL_LOW, integer)
            emax = rnd(tmax * EXCEPTIONAL_HIGH, integer)
            if integer and emin <= tmax:
                emin = tmax + 1
            emax = max(emax, emin)
            tiers.append({"keyword": fmt("Lucky " + keywords[i]), "min": emin, "max": emax, "level": level,
                          "weight": max(1, int(weight * EXCEPTIONAL_WEIGHT)), "maxRarity": "Common"})
    return tiers


def event_tags(text):
    tags = []
    for token in ("ONCONNECT", "ONTAKEHIT", "ONCRITICAL", "ONMISS", "ONKILL", "ONCOMBOEND", "ONCOMBO", "ONFIRSTHIT",
                  "ONTURN", "ONROOMSCLEARED", "HEAL", "REFLECT", "BUFF", "APPLY", "EVERY", "BLEED", "POISON", "BURN"):
        if token in (text or "").upper().replace("_", ""):
            tags.append(token.lower())
    return tags


def build_families(gd):
    stat_rows = load(gd, "StatBonuses.json")
    mods = load(gd, "Modifications.json")
    families = []

    # Stat suffixes: base = Common row value (or half the Rare value).
    by_type = defaultdict(list)
    for r in stat_rows:
        if r.get("StatType"):
            by_type[r["StatType"]].append(r)
    for stat, (family, words, of, desc, tags, integer) in SUFFIX_STATS.items():
        rows = by_type.get(stat, [])
        common = [r["Value"] for r in rows if (r.get("Rarity") or "Common") == "Common" and r["Value"] > 0]
        rare = [r["Value"] for r in rows if r.get("Rarity") == "Rare" and r["Value"] > 0]
        base = common[0] if common else (sum(rare) / len(rare) / 2 if rare else 1)
        families.append({
            "family": family, "pool": "stat", "affix": "suffix", "kind": "stat", "statType": stat,
            "description": desc, "weight": 10, "tags": tags, "integer": integer,
            "tiers": ladder_tiers(base, STAT_LADDER, words, lambda w, of=of: f"{of} {w}", integer, True),
        })

    # Stat prefixes from adjectives, grouped by Effect.
    adj = [m for m in mods if (m.get("prefixCategory") or "").upper() == "ADJECTIVE"]
    by_effect = defaultdict(list)
    for m in adj:
        by_effect[(m.get("Effect") or "").upper()].append(m)
    for effect, (family, words, tags, integer) in PREFIX_EFFECTS.items():
        rows = by_effect.get(effect)
        if not rows:
            continue
        base = sum(r["MinValue"] for r in rows) / len(rows)
        families.append({
            "family": family, "pool": "stat", "affix": "prefix", "kind": "mod", "template": rows[0]["Name"],
            "weight": 10, "tags": tags, "integer": integer,
            "tiers": ladder_tiers(base, STAT_LADDER, words, lambda w: w, integer, True),
        })

    # Quality: multiplies the base item's own numbers.
    families.append({
        "family": "Quality", "pool": "stat", "affix": "prefix", "kind": "mod", "template": "Worn",
        "weight": 10, "tags": ["quality"], "integer": False,
        "tiers": [{"keyword": k, "min": lo, "max": hi, "level": lv, "weight": w} for k, lo, hi, lv, w in QUALITY_LADDER],
    })

    # On-hit statuses.
    for effect, (family, words, tags) in STATUS_EFFECTS.items():
        rows = by_effect.get(effect)
        if not rows:
            continue
        base = rows[0]["MinValue"]
        families.append({
            "family": family, "pool": "mechanic", "affix": "prefix", "kind": "mod", "template": rows[0]["Name"],
            "weight": 10, "tags": tags, "integer": False,
            "tiers": ladder_tiers(base, STATUS_LADDER, words, lambda w: w, False, False),
        })

    # Affix templates, rule and cross-wire prefixes: one family each, 3 tiers.
    for m in adj:
        effect = (m.get("Effect") or "").upper()
        pool = "material" if effect in MATERIAL_TEMPLATES else "mechanic" if effect in MECHANIC_TEMPLATES else None
        if pool is None:
            continue
        lo, hi = float(m.get("MinValue") or 0), float(m.get("MaxValue") or 0)
        integer = float(lo).is_integer() and float(hi).is_integer()
        tiers = []
        for i, (level, mult, _, weight) in enumerate(MATERIAL_LADDER if pool == "material" else RULE_LADDER):
            tmin, tmax = (rnd(lo * mult, integer), rnd(hi * mult, integer)) if hi > 0 else (lo, hi)
            word = RULE_WORDS[i]
            tiers.append({"keyword": (word + " " + m["Name"]).strip(), "min": tmin, "max": max(tmin, tmax),
                          "level": level, "weight": weight})
        families.append({
            "family": m["Name"], "pool": pool, "affix": "prefix", "kind": "mod", "template": m["Name"],
            "weight": round(10 * float(m.get("Weight") if m.get("Weight") is not None else 1.0), 2),
            "tags": sorted(set(event_tags(m.get("crossWire")) + event_tags(m.get("triggerWhen"))
                               + [effect.lower(), "material" if pool == "material" else "rule"])),
            "integer": integer, "tiers": tiers,
        })

    # Animal suffixes: one family each, a single tier (they feed the animal-tag sets).
    for r in stat_rows:
        if r.get("StatType") or not r.get("triggerName"):
            continue
        families.append({
            "family": r["Name"], "pool": "mechanic", "affix": "suffix", "kind": "suffix", "template": r["Name"],
            "weight": 2, "tags": sorted(set((r.get("tags") or []) + event_tags(r.get("Description")))),
            "integer": True, "tiers": [{"keyword": r["Name"], "min": 0, "max": 0, "level": 5, "weight": 100}],
        })

    # Extra combo-strip slot.
    families.append({
        "family": "Extra Action Slot", "pool": "mechanic", "affix": "suffix", "kind": "slots",
        "weight": 4, "tags": ["combo", "action"], "integer": True,
        "tiers": [{"keyword": "of Repertoire", "min": 1, "max": 1, "level": 20, "weight": 100}],
    })
    return families


def weapon_rows(gd, triggers):
    rows = load(gd, "Weapons.json")
    cursor = Counter()
    for w in rows:
        tags = [t.lower() for t in (w.get("tags") or [])]
        cls = next((c for c in CLASSES if c in tags), "barbarian")
        w["class"] = cls
        tier = int(w.get("tier") or 1)
        base = int(w.get("baseDamage") or 0)
        bmin, bmax = sorted((int(w.get("damageBonusMin") or 0), int(w.get("damageBonusMax") or 0)))
        w["damageMin"] = base + bmin + 3 * tier
        w["damageMax"] = base + bmax + 3 * tier
        if "starter" not in tags:
            w["triggerName"] = pick_implicit(cls, cursor, triggers)
    return rows


def armor_rows(gd, triggers):
    rows = load(gd, "Armor.json")
    attrs = list(ATTR_CLASS)

    def scores(a):
        return {ATTR_CLASS[k]: int(a.get(k) or 0) for k in attrs}

    for a in rows:
        s = scores(a)
        top = max(s.values())
        a["class"] = None if top == 0 else max(CLASSES, key=lambda c: (s[c], -CLASSES.index(c)))
    # Every slot x tier cell gets at least one base per class: move the weakest-margin pieces from crowded classes.
    cells = defaultdict(list)
    for a in rows:
        cells[(a["slot"], int(a.get("tier") or 1))].append(a)
    for cell in cells.values():
        for a in cell:
            if a["class"] is None:
                counts = Counter(x["class"] for x in cell if x["class"])
                a["class"] = min(CLASSES, key=lambda c: counts[c])
        for want in CLASSES:
            if any(a["class"] == want for a in cell):
                continue
            counts = Counter(a["class"] for a in cell)
            donors = [a for a in cell if counts[a["class"]] > 1 and "starter" not in [t.lower() for t in (a.get("tags") or [])]]
            if not donors:
                continue
            donor = max(donors, key=lambda a: (scores(a)[want] - max(scores(a).values()), counts[a["class"]]))
            donor["class"] = want
    cursor = Counter()
    for a in rows:
        armor = int(a.get("armor") or 0)
        a["defenseMin"] = int(round(armor * 0.85))
        a["defenseMax"] = max(a["defenseMin"], int(round(armor * 1.15)))
        if "starter" not in [t.lower() for t in (a.get("tags") or [])]:
            a["triggerName"] = pick_implicit(a["class"], cursor, triggers)
    return rows


def pick_implicit(cls, cursor, triggers):
    options = [n for n in IMPLICITS[cls] if n in triggers]
    if not options:
        return ""
    name = options[cursor[cls] % len(options)]
    cursor[cls] += 1
    return name


def rarity_rows(gd):
    rows = load(gd, "RarityTable.json")
    out = []
    for r in rows:
        min_level, pools, max_lines, explode = RARITY_COLUMNS[r["Name"]]
        out.append({"Name": r["Name"], "Weight": r["Weight"], "MinLevel": min_level, "Pools": pools,
                    "MaxLines": max_lines, "Explode": explode})
    return out


def main():
    ap = argparse.ArgumentParser()
    ap.add_argument("--gamedata", default=os.path.join(os.path.dirname(__file__), "..", "GameData"))
    gd = os.path.abspath(ap.parse_args().gamedata)
    triggers = {t["name"] for t in load(gd, "Triggers.json")}

    families = build_families(gd)
    save(gd, "ItemMods.json", {
        "_doc": "Item mod families (Documentation/ITEM_GENERATION_REDESIGN_2026-11-14.md). Generated by "
                "Scripts/build-item-mods.py, then hand-edited. pool: material|stat|mechanic|action. kind: stat "
                "(StatBonus of statType), mod (clone of the Modifications/PrefixMaterialQuality template), suffix "
                "(clone of a StatBonuses row), slots (+combo strip slots). Tier: keyword, min/max value, level (item "
                "level needed; slotLevel overrides per slot), weight, maxRarity (highest item rarity that can roll it; "
                "'Common' tiers are the common-only highs), slots (optional per-tier slot limit).",
        "families": families,
    })
    save(gd, "Weapons.json", weapon_rows(gd, triggers))
    save(gd, "Armor.json", armor_rows(gd, triggers))
    save(gd, "RarityTable.json", rarity_rows(gd), compact_rows=True)
    pools = Counter(f["pool"] for f in families)
    print(f"ItemMods.json: {len(families)} families {dict(pools)}")


if __name__ == "__main__":
    main()
