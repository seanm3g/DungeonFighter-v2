"""Builds the item generation data for the 2026-11-14 redesign (Documentation/ITEM_GENERATION_REDESIGN_2026-11-14.md).

Writes / updates, in GameData/:
  ItemMods.json     mod families with tier ladders (keyword, value range, item level, weight, rarity requirement)
  Weapons.json      + class, damageMin / damageMax (tier-scaled, fixed per base), modifier
  Armor.json        + class, defenseMin / defenseMax, modifier
  RarityTable.json  + MinLevel, MinLines, MaxLines, Explode (old StatBonuses / ActionBonuses / Modifications columns dropped)

Sources: StatBonuses.json (stat suffixes + animal suffixes), Modifications.json (adjectives, RULE / CROSSWIRE
prefixes, affix templates), PrefixMaterialQuality.json (Quality), Actions.json + BaseModifiers.json (base modifiers).

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
# Keyword word for those tiers ("Uncanny Sharp", "of the Uncanny Ox"); "Lucky" is the Leather roll advantage.
EXCEPTIONAL_WORD = "Uncanny"
STATUS_LADDER = [(3, 0.6, 1.0, 100), (12, 1.0, 1.4, 140), (25, 1.4, 1.9, 180), (45, 1.9, 2.5, 220), (70, 2.5, 3.2, 260)]
RULE_LADDER = [(8, 1.0, 1.0, 100), (30, 1.4, 1.4, 140), (60, 1.8, 1.8, 180)]
MATERIAL_LADDER = [(1, 1.0, 1.0, 100), (25, 1.4, 1.4, 140), (55, 1.8, 1.8, 180)]

# Stat suffixes are animals (Documentation/AFFIX_PASS_2026-11-15.md section 4b): one animal per body stat, and one
# shared tier ladder for every animal ("of the Young Bear" ... "of the Primal Bear"). Suffixes never repeat a prefix's
# combat number: Damage / Hit / Crit / Combo / Speed live on prefixes only, Health / Defense / Magic find on suffixes.
TIER_WORDS = ["Young", "", "Great", "Dire", "Elder", "Ancient", "Primal"]


def animal_ladder(animal):
    return [(w + " " + animal).strip() for w in TIER_WORDS]


SUFFIX_STATS = {
    # StatType: (family, animal, description, tags, integer)
    "STR": ("Strength", "Bear", "+{n}% strength", ["str", "strength", "barbarian"], False),
    "AGI": ("Agility", "Fox", "+{n}% agility", ["agi", "agility", "warrior"], False),
    "TEC": ("Technique", "Spider", "+{n}% technique", ["tec", "technique", "rogue"], False),
    "INT": ("Intelligence", "Owl", "+{n}% intelligence", ["int", "intelligence", "wizard"], False),
    "Health": ("Life", "Ox", "+{n}% max health", ["health", "defense", "barbarian", "ontakehit"], False),
    "Armor": ("Defense", "Tortoise", "+{n}% defense", ["armor", "defense", "ontakehit"], False),
    "HealthRegen": ("Regeneration", "Salamander", "+{n} health regen", ["regen", "health", "defense"], False),
    "MagicFind": ("Magic Find", "Magpie", "+{n} magic find", ["luck", "magicfind"], False),
}

# Hybrid creatures: two body stats in one suffix, each part at lootSystem.itemGeneration.hybridPartShare.
SUFFIX_HYBRIDS = [
    ("Griffin", ["Strength", "Agility"]),
    ("Minotaur", ["Strength", "Life"]),
    ("Sphinx", ["Intelligence", "Technique"]),
    ("Basilisk", ["Technique", "Defense"]),
    ("Phoenix", ["Regeneration", "Intelligence"]),
    ("Kraken", ["Life", "Defense"]),
    # Phase 11 (D11.10): one stat composite per hybrid.
    ("Chimera", ["Strength", "Intelligence"]),
    ("Lynx", ["Agility", "Technique"]),
    ("Djinn", ["Agility", "Intelligence"]),
]
HYBRID_WEIGHT = 5
HYBRID_SHARE = 0.6  # only for the indicative min/max written on hybrid tiers; the roller uses the config value


# Every family names each of its tiers (user 2026-11-15): a line's keyword says how strong it is, from the level-1
# version to the level-100 one. Ladders below run weakest -> strongest; "Uncanny <word>" marks the common-only tier.

# Stat prefixes: the direct numbers of a swing, one word per tier (7 tiers).
PREFIX_EFFECTS = {
    # Modification Effect: (family, tier words, tags, integer)
    "DAMAGE": ("Brutality", ["Rough", "Heavy", "Brutal", "Crushing", "Ruinous", "Devastating", "Cataclysmic"],
               ["damage", "offense", "barbarian"], False),
    "HIT": ("Aim", ["Steady", "Sure", "True", "Keen", "Unerring", "Infallible", "Fated"],
            ["hit", "accuracy", "onconnect"], False),
    "CRIT": ("Menace", ["Sharp", "Wicked", "Deadly", "Lethal", "Murderous", "Merciless", "Executioner's"],
             ["crit", "oncritical", "rogue"], False),
    "COMBO": ("Poise", ["Balanced", "Poised", "Graceful", "Fluid", "Harmonious", "Masterful", "Transcendent"],
              ["combo", "oncombo", "warrior"], False),
    "SPEED": ("Swiftness", ["Quick", "Swift", "Fleet", "Rapid", "Blurring", "Lightning", "Timeless"],
              ["speed", "onturn", "warrior"], False),
    "MULTI-HIT": ("Flurry", ["Twin", "Darting", "Flurrying", "Volleying", "Storming", "Myriad", "Endless"],
                  ["multihit", "onconnect", "rogue"], False),
    "AMP": ("Amplify", ["Focused", "Honed", "Amplified", "Empowered", "Overcharged", "Exalted", "Apotheotic"],
            ["amp", "wizard", "oncombo"], False),
}

# Prefix hybrids: one adjective ladder for two swing numbers.
PREFIX_HYBRIDS = [
    ("Savage", ["Brutality", "Menace"], ["Feral", "Savage", "Vicious", "Ferocious", "Bloodthirsty", "Rampaging", "Primeval"]),
    ("Flowing", ["Poise", "Swiftness"], ["Nimble", "Flowing", "Streaming", "Rushing", "Cascading", "Torrential", "Tidal"]),
    ("Precise", ["Aim", "Menace"], ["Careful", "Precise", "Exacting", "Surgical", "Pinpoint", "Flawless", "Immaculate"]),
    ("Echoing", ["Amplify", "Flurry"], ["Ringing", "Echoing", "Reverberating", "Thundering", "Booming", "Clamorous", "Deafening"]),
    # Phase 11 (D11.10): hybrid composites.
    ("Rampant", ["Brutality", "Swiftness"], ["Brisk", "Rampant", "Charging", "Stampeding", "Trampling", "Juggernaut", "Unstoppable"]),
    ("Fencing", ["Swiftness", "Aim"], ["Lunging", "Fencing", "Feinting", "Dueling", "Bladedancing", "Swordsaint's", "Peerless"]),
    ("Arcing", ["Swiftness", "Amplify"], ["Sparking", "Arcing", "Crackling", "Galvanic", "Thunderstruck", "Stormcalled", "Skyrending"]),
]

# Phase 11 (D11.10): mechanic composites, one keyword rolling two parent lines (rare suffix, one phrase per tier).
MECH_COMPOSITES = [
    ("Executioner's Eye", ["Headsman's", "Sighting"]),
    ("Bookended", ["Opening", "Closing"]),
    ("Hexed", ["Steadied", "Late-Blooming"]),
    ("Quickening", ["Hastening", "Cascading"]),
]

# Phase 11 (D11.9): class cross-wires (one class material's spend into another's mint) leave item loot. The rows stay in
# Modifications.json for old saves.
# Rebuild step 7 (D4.9): one class-bending rule per class, added by the roller to every Mythic item (never rolled as a line).
MYTHIC_RULES = {"Worldsplitter's", "Bulwark-Born", "Fatebreaker's", "Spiral-Bound"}

CLASS_CROSSWIRES = {"Spiteful", "Resonant", "Measured", "Syncopated", "Headhunter's", "Grim", "Weighted", "Exultant",
                    "Restless", "Scrying", "Rhythmic", "Prismatic"}

# Phase 11 (Documentation/CORE_DESIGN_PASS_2026-11-17.md, D11.2 / D11.4): family -> (classes, slots). Classes: the 4 core
# classes and 6 hybrids (strong tilt, not a gate), "material" = the item's material's class, [] = neutral. Slots: the
# slot domain (a gate; the rare off-slot escape aside), [] = any slot.
W, H, C, L, F = "weapon", "head", "chest", "legs", "feet"
AFFINITY = {
    # stat prefixes
    # Stat prefixes span two slots each so every slot keeps at least three (chest and feet would otherwise have only Quality).
    "Brutality": (["barbarian"], [W, C]), "Aim": (["rogue"], [W, F]), "Menace": (["rogue"], [W, H]),
    "Poise": (["warrior"], [L, F]), "Swiftness": (["warrior"], [L]), "Flurry": (["warrior"], [W, C]),
    "Amplify": (["wizard"], [H, C]), "Quality": ([], []),
    "Savage": (["ravager"], [W]), "Flowing": (["warrior"], [L, F]), "Precise": (["rogue"], [W]),
    "Echoing": (["spellblade"], [H, W]), "Rampant": (["warbrute"], [W, L]), "Fencing": (["duelist"], [L, W]),
    "Arcing": (["spellblade"], [L, H]),
    # stat suffixes (any slot)
    "Strength": (["barbarian"], []), "Might": (["barbarian"], []), "Agility": (["warrior"], []), "Grace": (["warrior"], []),
    "Technique": (["rogue"], []), "Finesse": (["rogue"], []), "Intelligence": (["wizard"], []), "Wit": (["wizard"], []),
    "Life": (["barbarian"], []), "Vigor": (["barbarian"], []), "Defense": ([], []), "Warding": ([], []),
    "Regeneration": ([], []), "Mending": ([], []), "Magic Find": ([], []), "Fortune": ([], []),
    "Griffin": (["warbrute"], []), "Minotaur": (["barbarian"], []), "Sphinx": (["hexblade"], []),
    "Basilisk": (["rogue"], []), "Phoenix": (["wizard"], []), "Kraken": ([], []),
    "Chimera": (["ragecaller"], []), "Lynx": (["duelist"], []), "Djinn": (["spellblade"], []),
    # on-hit statuses
    "Burning": (["wizard"], [W]), "Bleeding": (["rogue"], [W]),
    # material templates and the class-less cross-wires
    "Listening": (["material"], []), "Attuned": (["material"], [C]), "Charged-Up": (["material"], [C]),
    "Doubled": (["material"], [C]),
    "Frayed": ([], [C]), "Smouldering": ([], [C]), "Gritted": ([], [C]), "Cryptic": ([], [C]), "Peculiar": ([], [C]),
    "Ominous": ([], [C]),
    # slot-position prefixes
    "Follow-Through": (["barbarian"], [H]), "Third-Strike": (["warrior"], [H]), "Late-Blooming": (["wizard"], [H]),
    "Sighting": (["rogue"], [H]), "Steadied": (["rogue"], [H]),
    # rule suffixes
    "Opening": (["barbarian"], [H]), "Closing": (["wizard"], [H]), "Bloodbound": (["barbarian"], [C]),
    "Vampiric": (["barbarian"], [W]), "Thorned": (["barbarian"], [C]), "Butcher's": (["rogue"], [W]),
    "Leaden": (["barbarian"], [W]), "Bloodied": (["barbarian"], [F]), "Towering": (["warrior"], [C]),
    "Ponderous": (["barbarian"], [L]), "Headsman's": (["barbarian"], [W]), "Berserker's": (["barbarian"], [F]),
    "Concussive": (["barbarian"], [W]), "Prying": (["rogue"], [W]), "Sapping": (["warrior"], [W]),
    "Envenomed": (["rogue"], [W]), "Jester's": (["rogue"], [F]), "Silvered": ([], [W]), "Giantslaying": ([], [W]),
    "Hourglass": (["warrior"], [L]), "Ember-Woven": (["wizard"], [C]), "Ledgered": (["wizard"], [C]),
    "Mismatched": ([], [C]), "Linked": (["wizard"], [H]), "Winged": (["warrior"], [L]), "Feasting": (["rogue"], [F]),
    "Hunter's": (["rogue"], [C]), "Trophy-Strung": (["rogue"], [F]), "Bountied": (["rogue"], [F]),
    "Extra Action Slot": (["wizard"], [H]),
    # D11.12 gap lines
    "Bracing": (["warrior"], [C]), "Walled": (["warrior"], [W]), "Hastening": (["warrior"], [L]),
    "Unyielding": (["warrior"], [F]), "Cascading": (["wizard"], [H]), "Foretelling": (["wizard"], [W]),
    "Recurring": (["wizard"], [H]), "Long-Striding": (["wizard"], [L]), "Rekindling": (["wizard"], [F]),
    "Momentous": (["barbarian"], [L]), "Defiant": (["barbarian"], [F]), "Steady-Footed": (["rogue"], [L]),
    "Sure-Footed": (["rogue"], [F]),
    # mechanic composites
    "Executioner's Eye": (["ravager"], [W, H]), "Bookended": (["ragecaller"], [H]), "Hexed": (["hexblade"], [H]),
    "Quickening": (["spellblade"], [L, H]),
}


def apply_affinity(families):
    """Writes classes and slots on every family (Phase 11); a family missing from AFFINITY stops the build."""
    missing = [f["family"] for f in families if f["family"] not in AFFINITY]
    if missing:
        raise SystemExit(f"Phase 11: add these families to AFFINITY: {missing}")
    for f in families:
        classes, slots = AFFINITY[f["family"]]
        if classes:
            f["classes"] = list(classes)
        if slots:
            f["slots"] = list(slots)

# Material-pool families (3 tiers): the family's own word is the middle tier.
MATERIAL_WORDS = {
    "Listening": ["Heeding", "Listening", "All-Hearing"],
    "Attuned": ["Tuned", "Attuned", "Harmonized"],
    "Charged-Up": ["Charged", "Charged-Up", "Overcharged"],
    "Doubled": ["Paired", "Doubled", "Redoubled"],
    "Spiteful": ["Petty", "Spiteful", "Vengeful"],
    "Resonant": ["Humming", "Resonant", "Thrumming"],
    "Measured": ["Careful-Handed", "Measured", "Metered"],
    "Syncopated": ["Offbeat", "Syncopated", "Polyrhythmic"],
    "Headhunter's": ["Tracker's", "Headhunter's", "Skullreaver's"],
    "Frayed": ["Worn-Edged", "Frayed", "Unraveling"],
    "Smouldering": ["Warm", "Smouldering", "Seething"],
    "Gritted": ["Clenched", "Gritted", "Ironjawed"],
    "Grim": ["Dour", "Grim", "Funereal"],
    "Weighted": ["Heavy-Set", "Weighted", "Anchored"],
    "Exultant": ["Glad", "Exultant", "Triumphant"],
    "Restless": ["Fidgeting", "Restless", "Unsleeping"],
    "Scrying": ["Peering", "Scrying", "Farseeing"],
    "Rhythmic": ["Tapping", "Rhythmic", "Drumming"],
    "Prismatic": ["Glinting", "Prismatic", "Iridescent"],
    "Cryptic": ["Veiled", "Cryptic", "Enigmatic"],
    "Peculiar": ["Odd", "Peculiar", "Bizarre"],
    "Ominous": ["Foreboding", "Ominous", "Doom-Laden"],
}


STATUS_EFFECTS = {
    "BURN": ("Burning", ["Smoldering", "Flaming", "Blazing", "Searing", "Infernal"], ["burn", "fire", "onconnect", "wizard"]),
    "BLEED": ("Bleeding", ["Notched", "Serrated", "Barbed", "Rending", "Exsanguinating"], ["bleed", "onconnect", "rogue"]),
}

# Slot prefixes (2026-11-21, user item 14): RULE rows in Modifications.json whose rule is gated on one strip slot
# (`WHILE IF SLOT=n -> DAMAGE|HIT|CRIT ...`; OPENER / FINISHER already cover slot 1 / the last slot). They roll as
# weighted prefixes in the material pool (Uncommon and up; never on a Common, D0.7), 3 tiers like the cross-wires.
SLOT_WORDS = {
    "Follow-Through": ["Following", "Follow-Through", "Unbroken"],
    "Third-Strike": ["Thrice-Struck", "Third-Strike", "Triple-Tolled"],
    "Late-Blooming": ["Patient", "Late-Blooming", "Long-Awaited"],
    "Sighting": ["Eyeing", "Sighting", "Deadeyed"],
    "Steadied": ["Steadying", "Steadied", "Unshaking"],
}

# Cross-wires spend one material store into another (or a status), so they sit in the material pool.
MATERIAL_TEMPLATES = {"LISTEN", "TUNE", "STORECOND", "COUNTSDOUBLE", "CROSSWIRE"}
MECHANIC_TEMPLATES = {"RULE", "OPENER", "FINISHER", "BLOODPRICE"}

QUALITY_LADDER = [  # keyword, min, max, level, weight
    ("Worn", 1.02, 1.05, 1, 100), ("Second Hand", 1.05, 1.08, 8, 140), ("Like New", 1.08, 1.12, 18, 180),
    ("New", 1.12, 1.18, 30, 220), ("Perfect", 1.18, 1.25, 45, 260), ("Masterwork", 1.25, 1.35, 60, 300),
    ("Heirloom", 1.35, 1.50, 80, 340), ("Cosmic", 2.0, 2.5, 101, 1),
]

RARITY_COLUMNS = {
    # MinLevel, MinLines, MaxLines, Explode. Each line picks its pool by lootSystem.itemGeneration.poolWeights
    # (the first is the guaranteed action), so rarity only says how many lines an item gets.
    "Common": (1, 1, 2, 0.5),  # the action, and half the time one descriptor suffix (user 2026-11-15: min 1, max 2)
    "Uncommon": (1, 2, 4, 0.30),
    "Rare": (1, 4, 6, 0.35),
    "Epic": (1, 5, 7, 0.40),
    "Legendary": (1, 6, 8, 0.45),
    "Mythic": (1, 6, 10, 0.50),
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
            tiers.append({"keyword": fmt(EXCEPTIONAL_WORD + " " + keywords[i]), "min": emin, "max": emax, "level": level,
                          "weight": max(1, int(weight * EXCEPTIONAL_WEIGHT)), "maxRarity": "Common"})
    return tiers


def event_tags(text):
    tags = []
    for token in ("ONCONNECT", "ONTAKEHIT", "ONCRITICAL", "ONMISS", "ONKILL", "ONCOMBOEND", "ONCOMBO", "ONFIRSTHIT",
                  "ONTURN", "ONROOMSCLEARED", "HEAL", "REFLECT", "BUFF", "APPLY", "EVERY", "BLEED", "POISON", "BURN"):
        if token in (text or "").upper().replace("_", ""):
            tags.append(token.lower())
    return tags


# Three suffix kinds (user 2026-11-15), written in this order after the base: common = descriptor ("of Might"),
# uncommon = animal ("of the Dire Bear", Pack-able), rare = poetic phrase for the fanciest effects ("Bathed in Blood").
# An item gets at most one suffix of each kind; its rarity sets the minimum (lootSystem.itemGeneration.suffixKinds).
DESCRIPTORS = {
    # animal family -> (descriptor family, one word per tier, weakest -> strongest)
    "Strength": ("Might", ["Brawn", "Might", "Strength", "Power", "Force", "Titans", "Colossi"]),
    "Agility": ("Grace", ["Ease", "Grace", "Agility", "Nimbleness", "Celerity", "Gales", "Zephyrs"]),
    "Technique": ("Finesse", ["Skill", "Finesse", "Craft", "Artistry", "Precision", "Mastery", "Perfection"]),
    "Intelligence": ("Wit", ["Wit", "Insight", "Intellect", "Wisdom", "Brilliance", "Genius", "Enlightenment"]),
    "Life": ("Vigor", ["Health", "Vigor", "Vitality", "Stamina", "Fortitude", "Endurance", "Immortality"]),
    "Defense": ("Warding", ["Guarding", "Warding", "Shielding", "Bulwarks", "Ramparts", "Bastions", "Citadels"]),
    "Regeneration": ("Mending", ["Healing", "Mending", "Restoration", "Renewal", "Rejuvenation", "Regrowth", "Rebirth"]),
    "Magic Find": ("Fortune", ["Luck", "Fortune", "Plenty", "Riches", "Treasure", "Bounty", "Providence"]),
}
DESCRIPTOR_SHARE = 0.8  # descriptor values = this x the animal's range at the same tier

# Rare suffixes: one poetic phrase per tier (status families have 5 tiers, rule families 3).
POETIC = {
    "Burning": ["Warmed by Flame", "Kissed by Flame", "Wreathed in Flame", "Forged in Flame", "Born of Flame"],
    "Venom": ["Grazed by the Serpent", "Kissed by the Serpent", "Bitten by the Serpent", "Fed by the Serpent",
              "Crowned by the Serpent"],
    "Bleeding": ["Scarred for the Fallen", "Bled for the Fallen", "Wept for the Fallen", "Sworn for the Fallen",
                 "Drowned for the Fallen"],
    "Opening": ["Danced at Dawn", "Danced in Daylight", "Danced Beneath the Sun"],
    "Closing": ["Given by Weary Hands", "Given by Dying Hands", "Given by Dead Hands"],
    "Bloodbound": ["Lent Against Debt", "Gambled by Debt", "Bound by Blood Debt"],
    "Vampiric": ["Tasted of Blood", "Bathed in Blood", "Drowned in Blood"],
    "Thorned": ["Rested on Edge", "Cradled on Edge", "Crowned on Edge"],
    "Butcher's": ["Worn to the Skin", "Worn to the Bone", "Worn to the Marrow"],
    "Leaden": ["Held for a Moment", "Held in Breath", "Held Beyond Breath"],
    "Bloodied": ["Carried til Dusk", "Carried til Waking", "Carried til the Grave"],
    "Towering": ["Named After an Uncle", "Named After a Father", "Named After a King"],
    "Ponderous": ["Burden of Friends", "Burden of Kin", "Burden of Ancestors"],
    "Headsman's": ["Claimed by Few", "Claimed by None", "Claimed by Death"],
    "Concussive": ["Short of Will", "Lost of Will", "Robbed of Will"],
    "Prying": ["Dipped in Starlight", "Dipped in Moonlight", "Dipped in Eclipse"],
    "Sapping": ["Absent for a While", "Absent for Long", "Absent Forever"],
    "Envenomed": ["Whispered in Secret", "Hushed in Secret", "Buried in Secret"],
    "Jester's": ["Lost and Found", "Lost and Found Again", "Lost and Found Forever"],
    "Silvered": ["Kept in Hope", "Kept in Faith", "Kept in Devotion"],
    "Giantslaying": ["Carried by a Cousin", "Carried by a Brother", "Carried by a Legion"],
    "Hourglass": ["Brushed by Time", "Kissed by Time", "Embraced by Time"],
    "Ember-Woven": ["Spun from Sparks", "Woven from Embers", "Forged from Wildfire"],
    "Ledgered": ["Owed to a Stranger", "Owed to an Heir", "Owed to a Dynasty"],
    "Mismatched": ["Spotted by Dew", "Rusted by Dew", "Eaten by Rust"],
    "Linked": ["Shared by Friends", "Cherished by Lovers", "Sworn by Soulmates"],
    "Winged": ["Hidden by Mist", "Hidden by Rain", "Hidden by Storm"],
    "Feasting": ["Fed by a Sister", "Mended by a Sister", "Raised by a Sister"],
    "Hunter's": ["Hidden Nearby", "Hidden Beneath", "Hidden Below the World"],
    "Trophy-Strung": ["Admired by a Child", "Worshipped by a Child", "Worshipped by a Nation"],
    "Bountied": ["Forgotten", "Forgotten and Remembered", "Remembered Forever"],
    "Berserker's": ["Roused by Rage", "Lost to Rage", "Consumed by Rage"],
    "Extra Action Slot": ["Remembered by the Careless"],
    # Phase 11 (D11.12) gap lines
    "Bracing": ["Braced by Comrades", "Braced by a Shieldwall", "Braced by an Army"],
    "Walled": ["Raised Behind a Fence", "Raised Behind a Wall", "Raised Behind a Rampart"],
    "Hastening": ["Hurried by a Drum", "Hurried by War Drums", "Hurried by Thunder"],
    "Unyielding": ["Stood Through Rain", "Stood Through Storm", "Stood Through the End"],
    "Cascading": ["Spilled from a Cup", "Spilled from a Fountain", "Spilled from the Heavens"],
    "Foretelling": ["Read in Tea Leaves", "Read in the Stars", "Read in the Book of Ends"],
    "Recurring": ["Heard Twice", "Heard Again and Again", "Heard Forever After"],
    "Long-Striding": ["Walked a Mile", "Walked a League", "Walked the World"],
    "Rekindling": ["Kept as Embers", "Kept as Coals", "Kept as a Hearthfire"],
    "Momentous": ["Swung by a Smith", "Swung by a Giant", "Swung by a Mountain"],
    "Defiant": ["Spat at Fate", "Spat at Kings", "Spat at the Gods"],
    "Steady-Footed": ["Trained on Ice", "Trained on Rope", "Trained on a Blade's Edge"],
    "Sure-Footed": ["Learned from a Fall", "Learned from a Hundred Falls", "Learned from Every Fall"],
    # Phase 11 (D11.10) mechanic composites
    "Executioner's Eye": ["Watched the Block", "Watched the Gallows", "Watched the Last Day"],
    "Bookended": ["Opened and Closed by Rage", "Opened and Closed by Storms", "Opened and Closed by Ruin"],
    "Hexed": ["Cursed by a Neighbor", "Cursed by a Witch", "Cursed by a Coven"],
    "Quickening": ["Caught in a Breeze", "Caught in a Gale", "Caught in a Maelstrom"],
}


def apply_suffix_kinds(families):
    by_name = {f["family"]: f for f in families}
    # Uncommon: the animals and hybrid creatures.
    for f in families:
        if f["pool"] == "stat" and f["affix"] == "suffix":
            f["suffixKind"] = "uncommon"
    # Common: one descriptor per animal stat, same tier gates, values at DESCRIPTOR_SHARE.
    for animal, (name, words) in DESCRIPTORS.items():
        a = by_name[animal]
        tiers = []
        normal = 0
        for t in a["tiers"]:
            uncanny = t.get("maxRarity") == "Common"
            word = words[normal - 1 if uncanny else normal]
            if not uncanny:
                normal += 1
            tier = {"keyword": f"of {EXCEPTIONAL_WORD + ' ' if uncanny else ''}{word}",
                    "min": rnd(t["min"] * DESCRIPTOR_SHARE, a["integer"]), "max": rnd(t["max"] * DESCRIPTOR_SHARE, a["integer"]),
                    "level": t["level"], "weight": t["weight"]}
            if uncanny:
                tier["maxRarity"] = "Common"
            tiers.append(tier)
        families.append({**{k: v for k, v in a.items() if k != "tiers"}, "family": name, "suffixKind": "common", "tiers": tiers})
    # Rare: every identity / fancy family moves to the suffix as a poetic phrase, one phrase per tier.
    for f in families:
        if f["pool"] == "mechanic":
            phrases = POETIC.get(f["family"])
            if not phrases or len(phrases) < len(f["tiers"]):
                raise SystemExit(f"{f['family']}: needs {len(f['tiers'])} poetic phrases (one per tier) in POETIC")
            f["affix"] = "suffix"
            f["suffixKind"] = "rare"
            for t, phrase in zip(f["tiers"], phrases):
                t["keyword"] = phrase


def check_tier_names(families):
    """Every tier of a family has its own name (user 2026-11-15); stops the build otherwise."""
    for f in families:
        names = [t["keyword"] for t in f["tiers"]]
        dupes = sorted(n for n, c in Counter(names).items() if c > 1)
        if dupes:
            raise SystemExit(f"{f['family']}: tiers share a name: {dupes}")



def build_families(gd):
    stat_rows = load(gd, "StatBonuses.json")
    mods = load(gd, "Modifications.json")
    families = []

    # Stat suffixes: base = Common row value (or half the Rare value).
    by_type = defaultdict(list)
    for r in stat_rows:
        if r.get("StatType"):
            by_type[r["StatType"]].append(r)
    for stat, (family, animal, desc, tags, integer) in SUFFIX_STATS.items():
        rows = by_type.get(stat, [])
        common = [r["Value"] for r in rows if (r.get("Rarity") or "Common") == "Common" and r["Value"] > 0]
        rare = [r["Value"] for r in rows if r.get("Rarity") == "Rare" and r["Value"] > 0]
        base = common[0] if common else (sum(rare) / len(rare) / 2 if rare else 1)
        families.append({
            "family": family, "pool": "stat", "affix": "suffix", "kind": "stat", "statType": stat,
            "description": desc, "weight": 10, "tags": tags, "integer": integer,
            "tiers": ladder_tiers(base, STAT_LADDER, animal_ladder(animal), lambda w: f"of the {w}", integer, True),
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

    # Hybrids: tiers copy the first part's gates (level, weight, Uncanny); values come from each part at roll time.
    by_name = {f["family"]: f for f in families}
    prefix_hybrids = [(name, parts) for name, parts, _ in PREFIX_HYBRIDS]
    hybrid_words = {name: words for name, _, words in PREFIX_HYBRIDS}
    for affix, hybrids in (("suffix", SUFFIX_HYBRIDS), ("prefix", prefix_hybrids)):
        for name, parts in hybrids:
            first = by_name[parts[0]]
            words = animal_ladder(name) if affix == "suffix" else hybrid_words[name]
            tiers = []
            for i, t in enumerate(first["tiers"]):
                word = words[i // 2]
                if t.get("maxRarity") == "Common":
                    word = EXCEPTIONAL_WORD + " " + word
                keyword = f"of the {word}" if affix == "suffix" else word
                tier = {"keyword": keyword, "min": round(t["min"] * HYBRID_SHARE, 2), "max": round(t["max"] * HYBRID_SHARE, 2),
                        "level": t["level"], "weight": t["weight"]}
                if t.get("maxRarity"):
                    tier["maxRarity"] = t["maxRarity"]
                tiers.append(tier)
            families.append({
                "family": name, "pool": "stat", "affix": affix, "kind": "hybrid", "parts": parts,
                "description": " + ".join(by_name[p].get("description") or p for p in parts),
                "weight": HYBRID_WEIGHT, "tags": sorted(set(t for p in parts for t in by_name[p].get("tags") or [])),
                "integer": False, "tiers": tiers,
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
        if m["Name"] in CLASS_CROSSWIRES or m["Name"] in MYTHIC_RULES:
            continue
        slot = m["Name"] in SLOT_WORDS
        pool = ("material" if effect in MATERIAL_TEMPLATES or slot
                else "mechanic" if effect in MECHANIC_TEMPLATES else None)
        if pool is None:
            continue
        lo, hi = float(m.get("MinValue") or 0), float(m.get("MaxValue") or 0)
        integer = float(lo).is_integer() and float(hi).is_integer()
        tiers = []
        words = SLOT_WORDS if slot else MATERIAL_WORDS
        if pool == "material" and m["Name"] not in words:
            raise SystemExit(f"{m['Name']}: needs one word per tier in MATERIAL_WORDS")
        for i, (level, mult, _, weight) in enumerate(MATERIAL_LADDER if pool == "material" else RULE_LADDER):
            tmin, tmax = (rnd(lo * mult, integer), rnd(hi * mult, integer)) if hi > 0 else (lo, hi)
            # Mechanic families get their poetic phrase per tier in apply_suffix_kinds.
            keyword = words[m["Name"]][i] if pool == "material" else m["Name"]
            tiers.append({"keyword": keyword, "min": tmin, "max": max(tmin, tmax),
                          "level": level, "weight": weight})
        families.append({
            "family": m["Name"], "pool": pool, "affix": "prefix", "kind": "mod", "template": m["Name"],
            "weight": round(10 * float(m.get("Weight") if m.get("Weight") is not None else 1.0), 2),
            "tags": sorted(set(event_tags(m.get("crossWire")) + event_tags(m.get("triggerWhen"))
                               + [effect.lower(), "slot" if slot else "material" if pool == "material" else "rule"])),
            "integer": integer, "tiers": tiers,
        })

    # The 120 one-tier animal proc suffixes (StatBonuses.json rows with a triggerName) no longer roll: animals are the
    # stat suffixes now. The rows stay for old saves.

    # Extra combo-strip slot.
    families.append({
        "family": "Extra Action Slot", "pool": "mechanic", "affix": "suffix", "kind": "slots",
        "weight": 4, "tags": ["combo", "action"], "integer": True,
        "tiers": [{"keyword": "of Repertoire", "min": 1, "max": 1, "level": 20, "weight": 100}],
    })

    # Mechanic composites (D11.10): tiers at the stricter part's gates, values from each part at roll time.
    by_name = {f["family"]: f for f in families}
    for name, parts in MECH_COMPOSITES:
        ps = [by_name[p] for p in parts]
        n = min(len(p["tiers"]) for p in ps)
        tiers = [{"keyword": f"{name} {i + 1}", "min": 0, "max": 0,
                  "level": max(p["tiers"][i]["level"] for p in ps), "weight": ps[0]["tiers"][i]["weight"]}
                 for i in range(n)]
        families.append({
            "family": name, "pool": "mechanic", "affix": "suffix", "kind": "hybrid", "parts": parts,
            "description": " + ".join(parts), "weight": HYBRID_WEIGHT * 0.4,
            "tags": sorted(set(t for p in ps for t in p.get("tags") or [])), "integer": False, "tiers": tiers,
        })

    apply_suffix_kinds(families)
    apply_affinity(families)
    check_tier_names(families)
    return families


# Weapon damage is fixed to the base (user 2026-11-15): no item-level multiplier at roll time. The tier carries the
# level instead: each tier's range is the old item-level-1 range x the old multiplier at that tier's usual drop level
# (TierDistribution.json: T1 ~L6, T2 ~L24, T3 ~L48, T4 ~L72, T5 ~L92), so the numbers match the old curve at mid-tier.
WEAPON_TIER_DAMAGE = {1: 1.05, 2: 1.23, 3: 1.47, 4: 1.71, 5: 1.91}
# Each base rolls in center x (1 -/+ this): a 0.2 spread is a 1 : 1.5 range, e.g. [50-75].
WEAPON_DAMAGE_SPREAD = 0.2


# Base modifiers (user 2026-11-15): every base, starters included, carries one fixed modifier written into the catalog
# ("modifier" = CrossWires WHILE rule(s), "{lo-hi}" = the value range rolled per drop). Low tiers are simple, high tiers
# add conditions and a second rule. Modifiers have no name (the old "modifierName" was flavor only, retired 2026-11-17).
MODIFIER_STATUSES = {"barbarian": ["bleed", "burn"], "warrior": ["bleed"], "rogue": ["poison", "bleed", "acid"],
                     "wizard": ["burn", "acid"]}
# Synergy pass (2026-11-16, Documentation/SYNERGY_PASS_2026-11-16.md): bases from tier 2 up name a tag (class, hybrid or
# theme tag, so one base pulls toward a family of actions instead of one action), then change what those actions do.
# Foe statuses a class's bases apply, self resources they grant, and the material stores they mint:
MODIFIER_FOE_STATUS = {"barbarian": ["stagger", "brittle", "expose"], "warrior": ["stagger", "rattle", "slow"],
                       "rogue": ["brittle", "mark", "expose"], "wizard": ["rattle", "weaken", "brittle"]}
MODIFIER_SELF_STATUS = {"barbarian": ["harden", "haste"], "warrior": ["haste", "fortify"],
                        "rogue": ["haste", "focus"], "wizard": ["focus", "haste"]}
MODIFIER_MINTS = {"barbarian": ["RAGE", "CRISIS", "TRIUMPH"], "warrior": ["BEAT", "DRAG", "STREAK"],
                  "rogue": ["GRAZE", "SWING", "TROPHY"], "wizard": ["CHAIN", "LENS", "CONTACT"]}
# Shapes per tier, dealt round-robin across the bases of one class x tier cell. A tuple is (kind, stat, lo, hi[, second]).
MODIFIER_SHAPES = {
    # User 2026-11-17: a base modifier changes or enhances another system (an action, a tag, a status, a store, a class
    # skill, the item's set count, the combo) and never hands out a conditional "+n% damage / speed" bonus. The one
    # percent left is POTENCY, which strengthens a status rather than the hero. Tier 1 touches one action or the class,
    # tier 2 a tag or a status chain, tier 3 the skill tree / set / store systems, tier 4 bends a rule (combo, events,
    # tags), tier 5 is a pair whose halves feed each other.
    1: [("action-apply",), ("action-tagas",), ("class-dot",), ("potency",), ("action-mint",)],
    2: [("tag-apply",), ("tag-add",), ("status-chain",), ("class-tagas",)],
    3: [("rank",), ("setcount",), ("tag-mint",), ("cash-apply",)],
    4: [("exempt",), ("countas",), ("full-add",), ("tag-tagas",), ("crit-apply",)],
    5: [("tagas-apply",), ("rank-pair",), ("chain-mint",), ("setcount-rank",)],
}
# Event aliases a class's tier-4 bases can grant (COUNTAS: the first event also counts as the second for triggers).
MODIFIER_COUNTAS = {"barbarian": [("ONTAKEHIT", "ONCOMBO")], "warrior": [("ONMISS", "ONCONSECUTIVEATTACK")],
                    "rogue": [("ONMISS", "ONCRITICAL")], "wizard": [("ONCOMBOEND", "ONCOMBO")]}
# Combo / streak exemptions (EXEMPT): what is protected and when.
MODIFIER_EXEMPT = {"barbarian": [("COMBO", "HP<50"), ("STREAK", "TARGET stagger")],
                   "warrior": [("STREAK", "STEP>=3"), ("COMBO", "TARGET rattle")],
                   "rogue": [("COMBO", "TARGET mark"), ("STREAK", "TARGET brittle")],
                   "wizard": [("COMBO", "STEP>=3"), ("STREAK", "focus>=1")]}
BASIC_ACTION_TAGS = {"startingweapon", "weapon_basic", "required", "unique", "enemy"}
CLASS_WEAPON_TAG = {"barbarian": "mace", "warrior": "sword", "rogue": "dagger", "wizard": "wand"}


SUBJECT_NAMES = {"verb": "Verbal", "status": "Affliction", "store": "Hoarder", "swift": "Swift", "action": "Action"}


def title(s):
    s = SUBJECT_NAMES.get(str(s).lower(), s)
    return " ".join(w[:1].upper() + w[1:].lower() for w in str(s).replace("_", " ").split())


class ModifierAssigner:
    """Deals each base a fixed modifier; deterministic for a given catalog + Actions.json."""

    def __init__(self, gd):
        actions = load(gd, "Actions.json")
        self.actions = {}
        for c in CLASSES:
            names = []
            for a in actions:
                tags = {t.strip().lower() for t in (a.get("tags") or "").split(",") if t.strip()}
                if tags & BASIC_ACTION_TAGS or "retired" in tags or a.get("materialScale") or not a.get("action"):
                    continue
                if tags & {c, CLASS_WEAPON_TAG[c]}:
                    names.append(a["action"].strip())
            self.actions[c] = sorted(set(names))
        mods = load(gd, "BaseModifiers.json")
        self.tags = {c: list(mods.get("tags", {}).get(c) or mods.get("tags", {}).get("*") or ["verb"]) for c in CLASSES}
        self.cursor = Counter()
        self.by_name = {}
        # Class skill nodes a RANK modifier can deepen: ranked rule nodes (their amounts scale by rank), per class tree.
        trees = load(gd, "SkillTrees.json")["trees"]
        self.nodes = {}
        for c in CLASSES:
            tree = next((t for t in trees if t["classKey"].lower() == c), {"nodes": []})
            self.nodes[c] = sorted((n["id"], n["name"]) for n in tree["nodes"]
                                   if n.get("crossWire") and int(n.get("maxRank") or 1) > 1
                                   and any(ch.isdigit() for ch in n["crossWire"]))

    def _next(self, key, options):
        i = self.cursor[key]
        self.cursor[key] += 1
        return options[i % len(options)]

    def _rule(self, cls, shape, ctx=None):
        """One rule (or a tier-5 pair) for a shape; ctx keeps the tag / status / store picked so far for this base."""
        kind = shape[0]
        ctx = ctx if ctx is not None else {}

        def pick(key, options):
            if key not in ctx:
                ctx[key] = self._next((key, cls), options)
            return ctx[key]

        tag = lambda: pick("tag", self.tags[cls])
        foe = lambda: pick("foe", MODIFIER_FOE_STATUS[cls])
        own = lambda: pick("own", MODIFIER_SELF_STATUS[cls])
        dot = lambda: pick("status", MODIFIER_STATUSES[cls])
        store = lambda: pick("mint", MODIFIER_MINTS[cls])
        action = lambda: pick("action", self.actions[cls] or ["Strike"])
        act = lambda: action().replace(" ", "_")

        def other_tag(avoid=None):
            # A different tag than the one the rule reads (and never the class itself): aliases cross families.
            skip = {avoid or tag(), cls}
            return pick("tag2", [x for x in self.tags[cls] if x not in skip] or [x for x in self.tags[cls] if x != cls])

        def node():
            used = ctx.setdefault("nodes", [])
            pool = [n for n in self.nodes[cls] if n[0] not in used] or self.nodes[cls]
            picked = self._next(("node", cls), pool)
            used.append(picked[0])
            return picked

        if kind == "action-apply":
            return f"ONCONNECT EVERY 2 IF ACTION {act()} -> APPLY {foe()} 1", title(action())
        if kind == "action-tagas":
            return f"WHILE IF ACTION {act()} -> TAGAS {other_tag(cls)}", title(action())
        if kind == "action-mint":
            return f"ONCOMBO IF ACTION {act()} -> MINT {store()} 1", title(action())
        if kind == "class-dot":
            return f"ONCONNECT EVERY 3 IF CLASS {cls} -> APPLY {dot()} 1", title(dot())
        if kind == "potency":
            return f"WHILE -> POTENCY {dot()} {{10-20}}%", title(dot())
        if kind == "tag-apply":
            return f"ONCONNECT EVERY 2 IF TAG {tag()} -> APPLY {foe()} 1", title(foe())
        if kind == "tag-add":
            return f"ONCOMBO EVERY 2 IF TAG {tag()} -> ADD {own()} 1", title(own())
        if kind == "status-chain":
            return f"ONSTATUSAPPLY:{dot()} EVERY 2 -> APPLY {foe()} 1", title(foe())
        if kind == "class-tagas":
            return f"WHILE IF CLASS {cls} -> TAGAS {other_tag(cls)}", title(other_tag(cls))
        if kind == "rank":
            nid, name = node()
            return f"WHILE -> RANK {nid} 1", name
        if kind == "setcount":
            return "WHILE -> SETCOUNT 1", "Paired"
        if kind == "tag-mint":
            return f"ONCONNECT EVERY 2 IF TAG {tag()} -> MINT {store()} 1", title(store())
        if kind == "cash-apply":
            return f"ONCASH:{store()} -> APPLY {foe()} 1", title(store())
        if kind == "exempt":
            what, cond = self._next(("exempt", cls), MODIFIER_EXEMPT[cls])
            return f"WHILE IF {cond} -> EXEMPT {what}", "Unbroken" if what == "COMBO" else "Steady"
        if kind == "countas":
            src, dst = self._next(("countas", cls), MODIFIER_COUNTAS[cls])
            return f"{src} -> COUNTAS {dst}", "Echo"
        if kind == "full-add":
            return f"ONSTOREFULL:{store()} -> ADD {own()} 2", title(store())
        if kind == "tag-tagas":
            return f"WHILE IF TAG {tag()} -> TAGAS {other_tag()}", title(tag())
        if kind == "crit-apply":
            return f"ONCRITICAL IF TAG {tag()} -> APPLY {foe()} 2", title(foe())
        if kind == "tagas-apply":
            return (f"WHILE IF TAG {tag()} -> TAGAS {other_tag()}; "
                    f"ONCONNECT EVERY 2 IF TAG {other_tag()} -> APPLY {foe()} 1"), title(other_tag())
        if kind == "rank-pair":
            (a, name), (b, _) = node(), node()
            return f"WHILE -> RANK {a} 1; WHILE -> RANK {b} 1", name
        if kind == "chain-mint":
            return (f"ONSTATUSAPPLY:{foe()} -> MINT {store()} 1; "
                    f"ONCASH:{store()} -> APPLY {foe()} 1"), title(store())
        if kind == "setcount-rank":
            nid, name = node()
            return f"WHILE -> SETCOUNT 1; WHILE -> RANK {nid} 1", name
        raise ValueError(kind)

    def assign(self, row, cls):
        # Same-named bases (a few catalog rows repeat a name) share one modifier, so a name always means one mechanic.
        key = (str(row.get("name") or "").strip().lower(), str(row.get("type") or row.get("slot") or "").lower())
        if key in self.by_name:
            row["modifier"] = self.by_name[key]
            row.pop("modifierName", None)
            row.pop("triggerName", None)
            return
        tier = min(5, max(1, int(row.get("tier") or 1)))
        cls = cls if cls in CLASSES else "barbarian"
        shape = self._next(("shape", cls, tier), MODIFIER_SHAPES[tier])
        ctx = {}
        rule, _subject = self._rule(cls, shape, ctx)
        row["modifier"] = rule
        row.pop("modifierName", None)  # retired 2026-11-17: the name was flavor only (subject + tier word)
        row.pop("triggerName", None)
        self.by_name[key] = rule


def weapon_rows(gd, assigner):
    rows = load(gd, "Weapons.json")
    for w in rows:
        tags = [t.lower() for t in (w.get("tags") or [])]
        cls = next((c for c in CLASSES if c in tags), "barbarian")
        w["class"] = cls
        tier = int(w.get("tier") or 1)
        base = int(w.get("baseDamage") or 0)
        bmin, bmax = sorted((int(w.get("damageBonusMin") or 0), int(w.get("damageBonusMax") or 0)))
        center = (base + (bmin + bmax) / 2 + 3 * tier) * WEAPON_TIER_DAMAGE.get(tier, 1.0)
        w["damageMin"] = max(1, int(round(center * (1 - WEAPON_DAMAGE_SPREAD))))
        w["damageMax"] = max(w["damageMin"] + 1, int(round(center * (1 + WEAPON_DAMAGE_SPREAD))))
        assigner.assign(w, cls)
    return rows


def armor_rows(gd, assigner):
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
    for a in rows:
        armor = int(a.get("armor") or 0)
        a["defenseMin"] = int(round(armor * 0.85))
        a["defenseMax"] = max(a["defenseMin"], int(round(armor * 1.15)))
        assigner.assign(a, a["class"])
    return rows


def rarity_rows(gd):
    rows = load(gd, "RarityTable.json")
    out = []
    for r in rows:
        min_level, min_lines, max_lines, explode = RARITY_COLUMNS[r["Name"]]
        out.append({"Name": r["Name"], "Weight": r["Weight"], "MinLevel": min_level, "MinLines": min_lines,
                    "MaxLines": max_lines, "Explode": explode})
    return out


def main():
    ap = argparse.ArgumentParser()
    ap.add_argument("--gamedata", default=os.path.join(os.path.dirname(__file__), "..", "GameData"))
    ap.add_argument("--modifiers-only", action="store_true",
                    help="only rewrite Weapons.json / Armor.json (base modifiers); keep the hand-edited ItemMods.json")
    args = ap.parse_args()
    gd = os.path.abspath(args.gamedata)
    if args.modifiers_only:
        assigner = ModifierAssigner(gd)
        save(gd, "Weapons.json", weapon_rows(gd, assigner))
        save(gd, "Armor.json", armor_rows(gd, assigner))
        print("Weapons.json / Armor.json: base modifiers rewritten")
        return
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
    assigner = ModifierAssigner(gd)
    save(gd, "Weapons.json", weapon_rows(gd, assigner))
    save(gd, "Armor.json", armor_rows(gd, assigner))
    save(gd, "RarityTable.json", rarity_rows(gd), compact_rows=True)
    pools = Counter(f["pool"] for f in families)
    print(f"ItemMods.json: {len(families)} families {dict(pools)}")


if __name__ == "__main__":
    main()
