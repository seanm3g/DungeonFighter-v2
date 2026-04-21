# Merges the mod-trade catalog (104 actions) into GameData/Actions.json.
# Strips any existing rows with category ModTrade or tags containing modtrade, then appends fresh rows.
# `action` = NameHero (else NameEnemy); `description` = catalog **Description** column; tags/category preserved on each row.
# Run from repo root: python scripts/generate_mod_trade_actions.py
import json
import pathlib

ROOT = pathlib.Path(__file__).resolve().parents[1]
ACTIONS_JSON = ROOT / "GameData" / "Actions.json"
# Legacy sidecar (no longer loaded by the game); kept empty so old paths do not ship duplicate data.
LEGACY_MODTRADE_JSON = ROOT / "GameData" / "ModTradeActions.json"


def _is_mod_trade_row(obj: object) -> bool:
    if not isinstance(obj, dict):
        return False
    if (obj.get("category") or "").strip() == "ModTrade":
        return True
    tags = (obj.get("tags") or "").strip().lower()
    if not tags:
        return False
    parts = [p.strip() for p in tags.split(",") if p.strip()]
    return "modtrade" in parts or tags == "modtrade"

_INVALID_NAMES = frozenset({"", "—", "–", "-"})


def display_action_name(name_hero: str, name_enemy: str, pid: str) -> str:
    h = (name_hero or "").strip()
    e = (name_enemy or "").strip()
    if h not in _INVALID_NAMES and h:
        s = h
    elif e not in _INVALID_NAMES and e:
        s = e
    else:
        return f"MT_{pid}"
    return (
        s.replace("\u2019", "'")
        .replace("\u2018", "'")
        .replace("\u201c", '"')
        .replace("\u201d", '"')
    )


def row(pid: str, name_hero: str, name_enemy: str, catalog_description: str, dmg: float, spd: float, hits: int, **mods):
    """spd_pct: 100 = baseline action length 1.00; lower = faster (see ModTradeActionCatalog)."""
    action_name = display_action_name(name_hero, name_enemy, pid)
    desc = " ".join((catalog_description or "").split())
    spd_s = f"{round(spd / 100, 2):.2f}"
    dps_n = min(250, max(8, int(dmg * hits / max(0.35, spd / 100))))
    j = {
        "action": action_name,
        "description": desc,
        "dps": f"{dps_n}%",
        "numberOfHits": str(hits),
        "damage": f"{int(round(dmg))}%",
        "speed": spd_s,
        "target": "ENEMY",
        "category": "ModTrade",
        "tags": "modtrade",
    }
    for k, v in mods.items():
        if v is not None and v != "":
            j[k] = str(v)
    return j


def main():
    a = []
    # §1a Hero (12) — Description from ModTradeActionCatalog.md
    a += [
        row("HS_S2D", "Commitment Cleaver", "", "You telegraph; your next swing hits like a finisher.", 147, 118, 1, speedMod="-20", damageMod="25"),
        row("HS_S2A", "Stance of Crescendo", "", "You lose rhythm now to stack combo scaling on what follows.", 95, 108, 1, speedMod="-10", ampMod="15"),
        row("HS_S2M", "Windup Flurry Mark", "", "Slow wind-up loads extra hits on your next volley.", 100, 115, 1, speedMod="-15", multiHitMod="1"),
        row("HS_D2S", "Quick Razors", "", "Light taps reclaim initiative for your next card.", 74, 92, 1, damageMod="-10", speedMod="20"),
        row("HS_D2A", "Feather Chain", "", "Weak surface hit; your combo amplifier climbs faster next.", 84, 92, 1, damageMod="-10", ampMod="10"),
        row("HS_D2M", "Rakeload", "", "Shallow damage now, more cuts registered next time.", 79, 100, 1, damageMod="-10", multiHitMod="2"),
        row("HS_A2S", "Reset Step", "", "You cash combo juice to snap your next action forward.", 116, 100, 1, ampMod="-15", speedMod="20"),
        row("HS_A2D", "Compression Strike", "", "You compress scaling into one heavier wallop next.", 126, 100, 1, ampMod="-15", damageMod="25"),
        row("HS_A2M", "Scatter Focus", "", "You spread amplification across many small impacts next.", 100, 100, 1, ampMod="-8", multiHitMod="2"),
        row("HS_M2S", "Singletap Recovery", "", "Fewer follow-ups; you recover spacing on the next beat.", 95, 95, 2, multiHitMod="-1", speedMod="15"),
        row("HS_M2D", "Collapse Cut", "", "You merge many cuts into one decisive strike next.", 84, 100, 3, multiHitMod="-2", damageMod="35"),
        row("HS_M2A", "Tight Spiral", "", "Fewer hits next, each carrying more combo weight.", 89, 100, 2, multiHitMod="-1", ampMod="15"),
    ]
    # §1b Enemy (12)
    a += [
        row("ES_S2D", "", "Lumbering Smash", "It falls behind, then its next hit is devastating.", 137, 118, 1, enemySpeedMod="-20", enemyDamageMod="25"),
        row("ES_S2A", "", "Charging Menace", "Slow approach; its combo scaling spikes on the follow-up.", 105, 112, 1, enemySpeedMod="-10", enemyAmpMod="15"),
        row("ES_S2M", "", "Delayed Shredder", "It commits tempo to set a burst of extra ticks next.", 116, 112, 1, enemySpeedMod="-15", enemyMultiHitMod="1"),
        row("ES_D2S", "", "Harrier Poke", "Weak injury that buys it the next initiative.", 74, 100, 1, enemyDamageMod="-10", enemySpeedMod="20"),
        row("ES_D2A", "", "Taunting Feint", "Scratch damage while it builds internal rage scaling.", 79, 100, 1, enemyDamageMod="-10", enemyAmpMod="10"),
        row("ES_D2M", "", "Swarm Posture", "Low per-hit pressure that widens into many ticks next.", 74, 100, 1, enemyDamageMod="-10", enemyMultiHitMod="2"),
        row("ES_A2S", "", "Cooldown Rush", "It dumps buildup to cut the gap on its next move.", 111, 112, 1, enemyAmpMod="-15", enemySpeedMod="20"),
        row("ES_A2D", "", "Finishing Protocol", "It spends stacked threat on one spike next.", 121, 112, 1, enemyAmpMod="-15", enemyDamageMod="25"),
        row("ES_A2M", "", "Scatter Burst", "It sprays stored power across a flurry next.", 100, 100, 1, enemyAmpMod="-8", enemyMultiHitMod="2"),
        row("ES_M2S", "", "Repositioning Skitter", "It stops chaining hits and becomes slippery next.", 89, 100, 2, enemyMultiHitMod="-1", enemySpeedMod="15"),
        row("ES_M2D", "", "Coalesce Slam", "Fewer swings; one armor-breaking slam next.", 79, 112, 3, enemyMultiHitMod="-2", enemyDamageMod="35"),
        row("ES_M2A", "", "Focused Predator", "It narrows pressure into fewer, heavier meaningful hits.", 95, 100, 2, enemyMultiHitMod="-1", enemyAmpMod="15"),
    ]
    # §2 Dual same-stat (16)
    a += [
        row("DE_S_opp_HP", "Initiative Theft", "Staggering Lunge", "You surge; their next tempo stumbles.", 105, 100, 1, speedMod="20", enemySpeedMod="-20"),
        row("DE_S_opp_EH", "Overextended Strike", "Predator Surge", "You pay speed; they accelerate next.", 126, 100, 1, speedMod="-20", enemySpeedMod="20"),
        row("DE_D_opp_HP", "Shatter Plate", "Weakened Claw", "Your next hit bites harder; theirs softens.", 95, 100, 1, damageMod="20", enemyDamageMod="-20"),
        row("DE_D_opp_EH", "Feint Opening", "Bloodletting Riposte", "You lighten damage; their next swing loads.", 84, 100, 1, damageMod="-10", enemyDamageMod="20"),
        row("DE_A_opp_HP", "Momentum Hijack", "Broken Focus", "Your combo scaling improves; theirs decays.", 100, 100, 1, ampMod="15", enemyAmpMod="-15"),
        row("DE_A_opp_EH", "Stalled Rhythm", "Enrage Spark", "You stall scaling; they ignite theirs next.", 111, 100, 1, ampMod="-8", enemyAmpMod="15"),
        row("DE_M_opp_HP", "Chainloaded", "Disrupted Swarm", "You widen follow-ups; theirs shortens next.", 89, 100, 1, multiHitMod="2", enemyMultiHitMod="-2"),
        row("DE_M_opp_EH", "Tight Combo", "Needle Hail Incoming", "You simplify hits; they go wide next.", 100, 100, 2, multiHitMod="-1", enemyMultiHitMod="2"),
        row("DE_S_align_PP", "Doubletime Gambit", "Berserk Tempo", "Both sides accelerate next; chaotic round.", 95, 100, 1, speedMod="10", enemySpeedMod="10"),
        row("DE_S_align_MM", "Gluefoot Exchange", "Exhaustion Grind", "Both slog next; mud, gravity, fatigue.", 105, 100, 1, speedMod="-10", enemySpeedMod="-10"),
        row("DE_D_align_PP", "Blood Moon Strike", "Mirror Berserk", "Both next hits hit harder.", 89, 100, 1, damageMod="10", enemyDamageMod="10"),
        row("DE_D_align_MM", "Pacifying Ward", "Softened Claws", "Both next hits muted.", 95, 100, 1, damageMod="-10", enemyDamageMod="-10"),
        row("DE_A_align_PP", "Duel of Crescendos", "Rival's Anthem", "Both climb scaling next.", 95, 100, 1, ampMod="10", enemyAmpMod="10"),
        row("DE_A_align_MM", "Suppress Sigil", "Flattened Fury", "Both lose scaling next.", 100, 100, 1, ampMod="-10", enemyAmpMod="-10"),
        row("DE_M_align_PP", "Storm of Blades", "Hail Partner", "Both gain extra ticks next.", 84, 100, 1, multiHitMod="1", enemyMultiHitMod="1"),
        row("DE_M_align_MM", "Clean Exchange", "Single-blow Oath", "Both lose follow-up volume next.", 95, 100, 2, multiHitMod="-1", enemyMultiHitMod="-1"),
    ]
    # §3 Cross-stat (64)
    a += [
        row("XC_SS_pp", "Sprint Harmony", "Rush Echo", "You and it both speed up next.", 95, 100, 1, speedMod="10", enemySpeedMod="10"),
        row("XC_SS_pm", "Lane Clear", "Tripwire Kick", "You surge; they stumble next.", 105, 100, 1, speedMod="20", enemySpeedMod="-20"),
        row("XC_SS_mp", "Heavy Commit", "Opportunist Dash", "You pay tempo; they dash next.", 126, 100, 1, speedMod="-20", enemySpeedMod="20"),
        row("XC_SS_mm", "Anchor Duel", "Mired Beast", "Mutual slow next.", 105, 100, 1, speedMod="-10", enemySpeedMod="-10"),
        row("XC_SD_pp", "Blitzer's Mark", "Armored Counter", "You quicken; their next hit hardens.", 95, 100, 1, speedMod="10", enemyDamageMod="10"),
        row("XC_SD_pm", "Outpace Shell", "Dulled Fang", "You accelerate; their next damage softens.", 100, 100, 1, speedMod="20", enemyDamageMod="-20"),
        row("XC_SD_mp", "Winded Rush", "Loaded Maw", "You slog; their next strike grows.", 105, 100, 1, speedMod="-20", enemyDamageMod="20"),
        row("XC_SD_mm", "Cautious Brake", "Pulled Punch", "Both next hits soften after tempo dip.", 100, 100, 1, speedMod="-10", enemyDamageMod="-10"),
        row("XC_SA_pp", "Flow State", "Rival Crescendo", "Both ramp combo scaling next.", 100, 100, 1, speedMod="10", enemyAmpMod="10"),
        row("XC_SA_pm", "Tempo Lock", "Broken Chorus", "You speed up; their amp decays next.", 100, 100, 1, speedMod="20", enemyAmpMod="-15"),
        row("XC_SA_mp", "Late Block", "Fury Spark", "You eat speed; they ignite scaling next.", 105, 100, 1, speedMod="-20", enemyAmpMod="15"),
        row("XC_SA_mm", "Fatigue Spiral", "Dampened Rage", "Both lose amp pressure next.", 100, 100, 1, speedMod="-10", enemyAmpMod="-10"),
        row("XC_SM_pp", "Flurry Haste", "Partnered Storm", "Both widen hit volume next.", 100, 100, 1, speedMod="10", enemyMultiHitMod="1"),
        row("XC_SM_pm", "Cut the Chain", "Snapped Swarm", "You speed up; their multihit shortens next.", 100, 100, 1, speedMod="20", enemyMultiHitMod="-2"),
        row("XC_SM_mp", "Trapped Feet", "Scatter Needles", "You slow; they spray ticks next.", 100, 100, 1, speedMod="-20", enemyMultiHitMod="2"),
        row("XC_SM_mm", "Grounded Guard", "Collapsed Flurry", "Both lose extra hits next after tempo dip.", 100, 100, 1, speedMod="-10", enemyMultiHitMod="-1"),
        row("XC_DS_pp", "Power Rush", "Swift Retort", "You hit harder; they move faster next.", 95, 100, 1, damageMod="20", enemySpeedMod="20"),
        row("XC_DS_pm", "Crushing Hold", "Pin Down", "Your damage buff; they lose speed next.", 100, 100, 1, damageMod="20", enemySpeedMod="-20"),
        row("XC_DS_mp", "Glancing Blow", "Slippery Prey", "You lighten hit; they accelerate next.", 89, 100, 1, damageMod="-10", enemySpeedMod="20"),
        row("XC_DS_mm", "Soft Tap", "Sluggish Claw", "Both next actions soften on damage and tempo.", 100, 100, 1, damageMod="-10", enemySpeedMod="-10"),
        row("XC_DD_pp", "Arms Race Cut", "Mirror Cleave", "Both next hits spike damage.", 89, 100, 1, damageMod="10", enemyDamageMod="10"),
        row("XC_DD_pm", "Sundering Edge", "Cracked Carapace", "You buff damage; theirs weakens next.", 95, 100, 1, damageMod="20", enemyDamageMod="-20"),
        row("XC_DD_mp", "Opening Gift", "Riposte Load", "You feint; they load damage next.", 84, 100, 1, damageMod="-10", enemyDamageMod="20"),
        row("XC_DD_mm", "Pulled Strikes", "Blunted Rage", "Both next hits muted.", 95, 100, 1, damageMod="-10", enemyDamageMod="-10"),
        row("XC_DA_pp", "Heavy Crescendo", "Spiteful Ramp", "Big hit plus enemy scaling up next.", 95, 100, 1, damageMod="20", enemyAmpMod="15"),
        row("XC_DA_pm", "Shatter Focus", "Silenced Fury", "You hit hard; their amp falls next.", 100, 100, 1, damageMod="20", enemyAmpMod="-15"),
        row("XC_DA_mp", "Tickling Jab", "Building Menace", "Light hit; they ramp next.", 89, 100, 1, damageMod="-10", enemyAmpMod="15"),
        row("XC_DA_mm", "Muffled Exchange", "Dampened Core", "Both damage and their amp dip next.", 100, 100, 1, damageMod="-10", enemyAmpMod="-10"),
        row("XC_DM_pp", "Cleaving Tempest", "Answering Hail", "Heavy hit meets wider enemy flurry next.", 100, 100, 1, damageMod="20", enemyMultiHitMod="2"),
        row("XC_DM_pm", "Finisher's Law", "Broken Volley", "You spike damage; their ticks shrink next.", 100, 100, 1, damageMod="20", enemyMultiHitMod="-2"),
        row("XC_DM_mp", "Chip Scatter", "Needle Echo", "Weak surface; they widen hits next.", 89, 100, 1, damageMod="-10", enemyMultiHitMod="2"),
        row("XC_DM_mm", "Softened Hail", "Short Chain", "Both lose damage bite and extra hits next.", 100, 100, 1, damageMod="-10", enemyMultiHitMod="-1"),
        row("XC_AS_pp", "Crescendo Sprint", "Racing Torment", "Your scaling and their speed rise next.", 100, 100, 1, ampMod="10", enemySpeedMod="10"),
        row("XC_AS_pm", "Dominant Line", "Anchored Beast", "You ramp; they slow next.", 100, 100, 1, ampMod="15", enemySpeedMod="-20"),
        row("XC_AS_mp", "Lost Bridge", "Surging Flank", "You stall amp; they speed up next.", 105, 100, 1, ampMod="-10", enemySpeedMod="20"),
        row("XC_AS_mm", "Quiet Measure", "Winded Hunter", "Both amp and speed pressure ease next.", 100, 100, 1, ampMod="-10", enemySpeedMod="-10"),
        row("XC_AD_pp", "Amp to Steel", "Spiked Counter", "Scaling meets enemy damage buff next.", 100, 100, 1, ampMod="10", enemyDamageMod="10"),
        row("XC_AD_pm", "Overwhelming Theme", "Dulled Retort", "You ramp; their hit softens next.", 100, 100, 1, ampMod="15", enemyDamageMod="-20"),
        row("XC_AD_mp", "Burnt Fuse", "Sundering Answer", "You drop amp; they sharpen damage next.", 105, 100, 1, ampMod="-10", enemyDamageMod="20"),
        row("XC_AD_mm", "Flattened Verse", "Weakened Slam", "Both scaling and enemy damage dip next.", 100, 100, 1, ampMod="-10", enemyDamageMod="-10"),
        row("XC_AA_pp", "Dueling Anthems", "Mirror Rage", "Both climb amp next.", 95, 100, 1, ampMod="10", enemyAmpMod="10"),
        row("XC_AA_pm", "Steal the Chorus", "Shattered Pride", "You ramp; they lose amp next.", 100, 100, 1, ampMod="15", enemyAmpMod="-15"),
        row("XC_AA_mp", "Broken Streak", "Predator's Crescendo", "You stall; they surge scaling next.", 111, 100, 1, ampMod="-8", enemyAmpMod="15"),
        row("XC_AA_mm", "Suppressed Duel", "Cowed Beast", "Both amp curves flatten next.", 100, 100, 1, ampMod="-10", enemyAmpMod="-10"),
        row("XC_AM_pp", "Chorus of Cuts", "Swarm in Kind", "You ramp; they widen multihit next.", 100, 100, 1, ampMod="10", enemyMultiHitMod="2"),
        row("XC_AM_pm", "Precision Crescendo", "Snipped Tide", "You ramp; their volume shrinks next.", 100, 100, 1, ampMod="15", enemyMultiHitMod="-2"),
        row("XC_AM_mp", "Fumbled Bridge", "Scatter Spite", "You lose amp; they go wide next.", 105, 100, 1, ampMod="-10", enemyMultiHitMod="2"),
        row("XC_AM_mm", "Muted Finale", "Broken Hail", "Both amp and multihit ease next.", 100, 100, 1, ampMod="-10", enemyMultiHitMod="-1"),
        row("XC_MS_pp", "Flurry Sprint", "Echo Rush", "Extra ticks and enemy speed up next.", 100, 100, 1, multiHitMod="1", enemySpeedMod="10"),
        row("XC_MS_pm", "Tanglefoot Flurry", "Caught Leg", "You widen hits; they slow next.", 100, 100, 1, multiHitMod="2", enemySpeedMod="-20"),
        row("XC_MS_mp", "Short Chain", "Slipstream", "Fewer your hits; they accelerate next.", 100, 100, 1, multiHitMod="-1", enemySpeedMod="20"),
        row("XC_MS_mm", "Collapsed Tempo", "Mired Swarm", "Both volume and speed ease next.", 100, 100, 1, multiHitMod="-1", enemySpeedMod="-10"),
        row("XC_MD_pp", "Many Cuts, Deep Answer", "Spiked Hail", "Volume meets enemy damage spike next.", 100, 100, 1, multiHitMod="2", enemyDamageMod="20"),
        row("XC_MD_pm", "Shred Armor", "Dulled Swipes", "You add ticks; their damage softens next.", 100, 100, 1, multiHitMod="2", enemyDamageMod="-20"),
        row("XC_MD_mp", "Single Bite", "Heavy Tail", "Fewer hits; enemy damage loads next.", 100, 100, 1, multiHitMod="-2", enemyDamageMod="20"),
        row("XC_MD_mm", "Quiet Rake", "Soft Swipes", "Both volume and enemy damage dip next.", 100, 100, 1, multiHitMod="-1", enemyDamageMod="-10"),
        row("XC_MA_pp", "Volume Crescendo", "Hate Symphony", "You widen hits; their combo scaling climbs next.", 100, 100, 1, multiHitMod="2", enemyAmpMod="15"),
        row("XC_MA_pm", "Shred the Song", "Broken Meter", "You widen hits; their amp decays next.", 100, 100, 1, multiHitMod="2", enemyAmpMod="-15"),
        row("XC_MA_mp", "Tight Rope", "Spiteful Build", "Fewer hits; they ramp next.", 100, 100, 1, multiHitMod="-2", enemyAmpMod="15"),
        row("XC_MA_mm", "Sparse Line", "Dampened Spite", "Both multihit and enemy amp ease next.", 100, 100, 1, multiHitMod="-1", enemyAmpMod="-10"),
        row("XC_MM_pp", "Twin Storms", "Mirror Hail", "Both widen multihit next.", 84, 100, 1, multiHitMod="1", enemyMultiHitMod="1"),
        row("XC_MM_pm", "Overwhelming Tide", "Snapped Chain", "You add ticks; theirs shortens next.", 89, 100, 1, multiHitMod="2", enemyMultiHitMod="-2"),
        row("XC_MM_mp", "Controlled Pace", "Needle Torrent", "You tighten; they spray next.", 100, 100, 2, multiHitMod="-1", enemyMultiHitMod="2"),
        row("XC_MM_mm", "Sparse Exchange", "Broken Rhythm", "Both lose extra hits next.", 95, 100, 2, multiHitMod="-1", enemyMultiHitMod="-1"),
    ]
    assert len(a) == 104, len(a)
    names = [x["action"] for x in a]
    dup = [n for n in names if names.count(n) > 1]
    if dup:
        raise SystemExit(f"Duplicate action names: {set(dup)}")

    raw = ACTIONS_JSON.read_text(encoding="utf-8")
    existing = json.loads(raw)
    if not isinstance(existing, list):
        raise SystemExit("Actions.json must be a JSON array")
    kept = [x for x in existing if not _is_mod_trade_row(x)]
    kept_names = {x.get("action") for x in kept if isinstance(x, dict) and x.get("action")}
    for mod_row in a:
        n = mod_row["action"]
        if n in kept_names:
            raise SystemExit(
                f"Mod-trade action {n!r} collides with a non-mod-trade row in Actions.json; rename one side."
            )
    merged = kept + a
    ACTIONS_JSON.write_text(json.dumps(merged, indent=2) + "\n", encoding="utf-8")
    LEGACY_MODTRADE_JSON.write_text("[]\n", encoding="utf-8")
    print(f"Wrote {len(a)} mod-trade rows into {ACTIONS_JSON} (total actions: {len(merged)}). Cleared {LEGACY_MODTRADE_JSON}.")


if __name__ == "__main__":
    main()
