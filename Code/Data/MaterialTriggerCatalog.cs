using System;
using System.Collections.Generic;
using System.Linq;
using RPGGame.Data;

namespace RPGGame
{
    /// <summary>One material-owned combat trigger identity (ported from main animal-suffix procs).</summary>
    public readonly struct MaterialTriggerDef
    {
        public MaterialTriggerDef(string material, string triggerName, string description, string when, string mechanics, double value, string channel = "combat", string scope = "TURN", string? filters = null)
        {
            Material = material;
            TriggerName = triggerName;
            Description = description;
            When = when;
            Mechanics = mechanics;
            Value = value;
            Channel = channel;
            Scope = scope;
            Filters = filters;
        }

        public string Material { get; }
        public string TriggerName { get; }
        public string Description { get; }
        public string When { get; }
        public string Mechanics { get; }
        public double Value { get; }
        public string Channel { get; }
        public string Scope { get; }
        public string? Filters { get; }
    }

    /// <summary>
    /// Per-material pools of trigger identities. Loot picks one at random after EnsureMaterial.
    /// Supersedes main's animal-suffix trigger ownership.
    /// </summary>
    public static class MaterialTriggerCatalog
    {
        public static IReadOnlyList<MaterialTriggerDef> All { get; } = Build();

        public static IReadOnlyDictionary<string, IReadOnlyList<MaterialTriggerDef>> PoolsByMaterial { get; } =
            All.GroupBy(d => d.Material, StringComparer.OrdinalIgnoreCase)
                .ToDictionary(
                    g => g.Key,
                    g => (IReadOnlyList<MaterialTriggerDef>)g.ToList(),
                    StringComparer.OrdinalIgnoreCase);

        public static bool TryGetPool(string? material, out IReadOnlyList<MaterialTriggerDef> pool)
        {
            pool = Array.Empty<MaterialTriggerDef>();
            if (string.IsNullOrWhiteSpace(material))
                return false;
            return PoolsByMaterial.TryGetValue(material.Trim(), out pool!);
        }

        private static MaterialTriggerDef D(string material, string key, string desc, string when, string mech, double value, string scope = "TURN", string? filters = null)
            => new MaterialTriggerDef(material, material + key, desc, when, mech, value, scope: scope, filters: filters);

        // Poison values are % of max HP added per tick and never decay within a fight, so they stay small
        // (0.25 per connect, 0.5 on crit / take-hit); they scale with the target's health bar instead.
        // Balance (BUILDLAB per-trigger pricing, 2026-10-12): one piece should be worth ~+5-15%. Extra multihit / retrigger
        // procs on every combo or connect were worth x2.6-3.8 and Focus on every connect x1.6, so they fire on crit; "+10% amp"
        // lifted openers to the full TEC amp (x1.8) and is "+10% damage" instead.
        // Strip-control procs (skip / jump / loop / random / repeat) on every connect or combo kept the strip at its opener
        // (one piece x0.66-0.69; a Steel set of them dropped Triumph Cadence to x0.3), so they fire on crit.
        private static List<MaterialTriggerDef> Build()
        {
            var list = new List<MaterialTriggerDef>
            {
                // Bone
                D("Bone", "Crab", "On take hit, tax foe accuracy by 1 this turn.", "ONTAKEHIT", "enemy_accuracy", -1),
                D("Bone", "Armadillo", "After a miss, apply Harden.", "ONAFTERMISS", "harden", 1),
                D("Bone", "Lobster", "On room clear, +1 armor for the fight.", "ONROOMSCLEARED", "armor", 1, scope: "FIGHT"),
                D("Bone", "Nautilus", "On crit, loop the combo strip.", "ONCRITICAL", "strip_loop", 1, scope: ""),
                D("Bone", "ScarabBeetle", "On room clear, heal 2.", "ONROOMSCLEARED", "heal", 2, scope: ""),
                D("Bone", "Cheetah", "On first hit, next action +15% speed.", "ONFIRSTHIT", "hero_next_action_speed", 15, scope: "ACTION"),
                D("Bone", "CapeBuffalo", "On take hit, next action +10% damage.", "ONTAKEHIT", "hero_next_action_damage", 10, scope: "ACTION"),
                D("Bone", "Elk", "On first hit, next action +10% damage.", "ONFIRSTHIT", "hero_next_action_damage", 10, scope: "ACTION"),
                D("Bone", "Stallion", "On first hit, next action +10% speed.", "ONFIRSTHIT", "hero_next_action_speed", 10, scope: "ACTION"),
                D("Bone", "CaveBear", "On take hit, apply Harden.", "ONTAKEHIT", "harden", 1),
                D("Bone", "Hyena", "On kill, heal 2.", "ONKILL", "heal", 2, scope: ""),
                D("Bone", "Wolf", "On combo, combo threshold +1 this turn.", "ONCOMBO", "hero_combo_threshold", 1),
                D("Bone", "SnowLeopard", "On first hit, next action +10% damage.", "ONFIRSTHIT", "hero_next_action_damage", 10, scope: "ACTION"),
                D("Bone", "Caracal", "On crit, crit threshold +1 this turn.", "ONCRITICAL", "hero_crit_threshold", 1),
                D("Bone", "Pronghorn", "On connect, weapon speed +1 this turn.", "ONCONNECT", "hero_weapon_speed", 1),
                D("Bone", "Rhinoceros", "On first hit, next action +15% damage.", "ONFIRSTHIT", "hero_next_action_damage", 15, scope: "ACTION"),
                D("Bone", "Velociraptor", "On combo, combo threshold +1 this turn.", "ONCOMBO", "hero_combo_threshold", 1),
                // Steel
                D("Steel", "Bloodhound", "On connect, accuracy +1 this turn.", "ONCONNECT", "hero_accuracy", 1),
                D("Steel", "Gorilla", "On combo end, foe hit threshold +1 this turn.", "ONCOMBOEND", "enemy_hit_threshold", 1),
                D("Steel", "WaterBuffalo", "On take hit, apply Fortify.", "ONTAKEHIT", "fortify", 1),
                D("Steel", "Ram", "On crit, apply Slow.", "ONCRITICAL", "slow", 1),
                D("Steel", "GrizzlyBear", "On crit, apply Bleed.", "ONCRITICAL", "bleed", 1),
                D("Steel", "Lynx", "On miss, tax foe accuracy by 1 this turn.", "ONMISS", "enemy_accuracy", -1),
                D("Steel", "DireWolf", "On kill, retrigger your opener.", "ONKILL", "retrigger_opener", 1, scope: ""),
                D("Steel", "Lion", "On combo end, foe hit threshold +1 this turn.", "ONCOMBOEND", "enemy_hit_threshold", 1),
                D("Steel", "Otter", "On an even roll, heal 1.", "ONEVEN", "heal", 1, scope: ""),
                D("Steel", "Marten", "On crit, skip the next strip slot.", "ONCRITICAL", "strip_skip", 1, scope: ""),
                D("Steel", "Orangutan", "On crit, jump ahead on the combo strip.", "ONCRITICAL", "strip_jump", 1, scope: ""),
                // Iron (was Damascus)
                D("Iron", "Bison", "On first hit, next action +10% damage.", "ONFIRSTHIT", "hero_next_action_damage", 10, scope: "ACTION"),
                D("Iron", "Hippopotamus", "On crit, apply Bleed.", "ONCRITICAL", "bleed", 1),
                D("Iron", "Ox", "On room clear, weapon damage +1 for the dungeon.", "ONROOMSCLEARED", "hero_weapon_damage", 1, scope: "DUNGEON"),
                D("Iron", "Mule", "On miss, stop strip progression.", "ONMISS", "strip_stop", 1, scope: ""),
                D("Iron", "Wolverine", "On take hit, next action +10% damage.", "ONTAKEHIT", "hero_next_action_damage", 10, scope: "ACTION"),
                D("Iron", "Boar", "On connect, gain miss salvage for the fight.", "ONCONNECT", "salvage_miss", 1, scope: "FIGHT"),
                D("Iron", "Panther", "On first hit, crit faces start at 18 this turn.", "ONFIRSTHIT", "crit_face_min:18", 18),
                D("Iron", "Fox", "After a miss, hit threshold +1 this turn.", "ONAFTERMISS", "hero_hit_threshold", 1),
                D("Iron", "Weasel", "On connect with same action as last, this swing +100% damage.", "ONCONNECT", "hero_action_damage", 100, scope: "", filters: "IFSAMESACTION"),
                D("Iron", "Mongoose", "On connect vs a DoT'd foe, apply Pierce.", "ONCONNECT", "pierce", 1, filters: "IFTARGETUNDERDOT"),
                D("Iron", "Monkey", "On crit, pick a random next strip action.", "ONCRITICAL", "strip_random", 1, scope: ""),
                D("Iron", "Phoenix", "On crit miss, heal 2.", "ONCRITICALMISS", "heal", 2, scope: ""),
                // Bronze
                D("Bronze", "PeregrineFalcon", "On crit, next action +10% damage.", "ONCRITICAL", "hero_next_action_damage", 10, scope: "ACTION"),
                D("Bronze", "GoldenEagle", "On kill, replace next d20 with 20.", "ONKILL", "replace_next_roll:20", 20, scope: ""),
                D("Bronze", "Ibis", "On connect, apply Pierce.", "ONCONNECT", "pierce", 1),
                D("Bronze", "Jay", "On miss, foe next action +10% damage.", "ONMISS", "enemy_next_action_damage", 10, scope: "ACTION"),
                D("Bronze", "Bell", "On combo, weapon speed +1 this turn.", "ONCOMBO", "hero_weapon_speed", 1),
                D("Bronze", "Drumskin", "On first hit, next action +10% speed.", "ONFIRSTHIT", "hero_next_action_speed", 10, scope: "ACTION"),
                // Gold
                D("Gold", "Hummingbird", "On connect, weapon speed +1 this turn.", "ONCONNECT", "hero_weapon_speed", 1),
                D("Gold", "Crane", "On crit, apply Focus.", "ONCRITICAL", "focus", 1),
                D("Gold", "Kestrel", "On crit, apply Focus.", "ONCRITICAL", "focus", 1),
                D("Gold", "Magpie", "On kill, replace next d20 with 15.", "ONKILL", "replace_next_roll:15", 15, scope: ""),
                // Gold mints DRAG on Slow; its own pool needs Slow sources.
                D("Gold", "Ingot", "On combo, apply Slow.", "ONCOMBO", "slow", 1),
                D("Gold", "Crown", "On combo end, apply Fortify.", "ONCOMBOEND", "fortify", 1),
                // Mithril
                D("Mithril", "Swift", "After a miss, next action +10% speed.", "ONAFTERMISS", "hero_next_action_speed", 10, scope: "ACTION"),
                D("Mithril", "Heron", "On first hit, apply Pierce.", "ONFIRSTHIT", "pierce", 1),
                D("Mithril", "Nighthawk", "After a miss, next action +10% speed.", "ONAFTERMISS", "hero_next_action_speed", 10, scope: "ACTION"),
                D("Mithril", "Jackdaw", "On crit, pick a random next strip action.", "ONCRITICAL", "strip_random", 1, scope: ""),
                D("Mithril", "Filigree", "On connect, accuracy +1 this turn.", "ONCONNECT", "hero_accuracy", 1),
                D("Mithril", "Chainlink", "On combo, combo threshold +1 this turn.", "ONCOMBO", "hero_combo_threshold", 1),
                // Glass
                D("Glass", "Cobra", "On connect, apply Poison.", "ONCONNECT", "poison", 0.25),
                D("Glass", "SaltwaterCrocodile", "On first hit, apply Slow.", "ONFIRSTHIT", "slow", 1),
                D("Glass", "JadeSerpent", "On crit, apply Poison.", "ONCRITICAL", "poison", 0.5),
                D("Glass", "Salamander", "On crit, apply Burn.", "ONCRITICAL", "burn", 1),
                D("Glass", "Firefly", "On crit, apply Expose.", "ONCRITICAL", "expose", 1),
                D("Glass", "PrayingMantis", "On first hit, next action +10% damage.", "ONFIRSTHIT", "hero_next_action_damage", 10, scope: "ACTION"),
                D("Glass", "PistolShrimp", "On crit, apply Pierce.", "ONCRITICAL", "pierce", 1),
                // Obsidian
                D("Obsidian", "Python", "On crit, apply Slow.", "ONCRITICAL", "slow", 1),
                D("Obsidian", "Viper", "On crit, apply Poison.", "ONCRITICAL", "poison", 0.5),
                D("Obsidian", "Basilisk", "On crit, apply Slow.", "ONCRITICAL", "slow", 1),
                D("Obsidian", "Toad", "On take hit, apply Poison to the attacker.", "ONTAKEHIT", "poison", 0.5),
                D("Obsidian", "Flake", "On kill, heal 2.", "ONKILL", "heal", 2, scope: ""),
                D("Obsidian", "Headsman", "On kill, retrigger your opener.", "ONKILL", "retrigger_opener", 1, scope: ""),
                // Shadow
                D("Shadow", "Crocodile", "On crit, repeat the current strip slot.", "ONCRITICAL", "strip_repeat", 1, scope: ""),
                D("Shadow", "FerdeLance", "On connect, apply Poison.", "ONCONNECT", "poison", 0.25),
                D("Shadow", "Gecko", "On connect, gain miss salvage for the fight.", "ONCONNECT", "salvage_miss", 1, scope: "FIGHT"),
                D("Shadow", "Damselfly", "After a miss, next action +10% speed.", "ONAFTERMISS", "hero_next_action_speed", 10, scope: "ACTION"),
                D("Shadow", "Mantis", "On first hit, next action +10% damage.", "ONFIRSTHIT", "hero_next_action_damage", 10, scope: "ACTION"),
                // Crit-fishing builds (Dice Thief, Shadow Gambler) crit most swings: on crit this was a second swing (x1.9 alone).
                D("Shadow", "ClockworkBeetle", "On combo end, retrigger the next strip action.", "ONCOMBOEND", "retrigger_next", 1, scope: ""),
                // Willow
                D("Willow", "Sparrowhawk", "On first hit, crit faces start at 19 this turn.", "ONFIRSTHIT", "crit_face_min:19", 19),
                D("Willow", "Rook", "On combo end, foe hit threshold +1 this turn.", "ONCOMBOEND", "enemy_hit_threshold", 1),
                D("Willow", "Owl", "On first hit, crit faces start at 18 this turn.", "ONFIRSTHIT", "crit_face_min:18", 18),
                D("Willow", "Dragon", "On crit, apply Burn.", "ONCRITICAL", "burn", 1),
                D("Willow", "Sphinx", "On crit, apply Focus.", "ONCRITICAL", "focus", 1),
                // Silver
                D("Silver", "Kirin", "On crit, heal 3.", "ONCRITICAL", "heal", 3, scope: ""),
                D("Silver", "Kitsune", "On crit, replace the next strip action.", "ONCRITICAL", "strip_replace_next", 1, scope: ""),
                // Silver mints LENS on gaining Focus; its own pool needs Focus sources (first hit: on crit starved Silver builds of LENS).
                D("Silver", "Prism", "On first hit, apply Focus.", "ONFIRSTHIT", "focus", 1),
                D("Silver", "Mirror", "On first hit, apply Focus.", "ONFIRSTHIT", "focus", 1),
                D("Silver", "Moonglass", "On first hit, crit faces start at 19 this turn.", "ONFIRSTHIT", "crit_face_min:19", 19),
                D("Silver", "Halo", "On first hit, next action +10% damage.", "ONFIRSTHIT", "hero_next_action_damage", 10, scope: "ACTION"),
                // Crystal
                D("Crystal", "Golem", "On miss, apply Harden.", "ONMISS", "harden", 1),
                D("Crystal", "Thunderbird", "On crit, apply Burn.", "ONCRITICAL", "burn", 1),
                D("Crystal", "Geode", "On connect, next action +10% damage.", "ONCONNECT", "hero_next_action_damage", 10, scope: "ACTION"),
                D("Crystal", "Ember", "On combo, apply Burn.", "ONCOMBO", "burn", 1),
                D("Crystal", "Facet", "On take hit, heal 2.", "ONTAKEHIT", "heal", 2, scope: ""),
                D("Crystal", "Spark", "On crit, next action +1 multihit.", "ONCRITICAL", "hero_next_action_multihit", 1, scope: "ACTION"),
                // Stone
                D("Stone", "Tortoise", "On miss, apply Harden.", "ONMISS", "harden", 1),
                D("Stone", "Pangolin", "On take hit, apply Harden.", "ONTAKEHIT", "harden", 1),
                D("Stone", "Abalone", "On take hit, apply Fortify.", "ONTAKEHIT", "fortify", 1),
                D("Stone", "HorseshoeCrab", "After a miss, heal 2.", "ONAFTERMISS", "heal", 2),
                D("Stone", "Trilobite", "On crit, repeat the current strip slot.", "ONCRITICAL", "strip_repeat", 1, scope: ""),
                D("Stone", "FiddlerCrab", "On crit, next action +10% damage.", "ONCRITICAL", "hero_next_action_damage", 10, scope: "ACTION"),
                // Leather
                D("Leather", "Elephant", "On connect with same action as last, this swing +100% damage.", "ONCONNECT", "hero_action_damage", 100, scope: "", filters: "IFSAMESACTION"),
                D("Leather", "Moose", "On connect, foe weapon speed -1 this turn.", "ONCONNECT", "enemy_weapon_speed", -1),
                D("Leather", "Mammoth", "On first hit, apply Fortify.", "ONFIRSTHIT", "fortify", 1),
                D("Leather", "KodiakBear", "On crit, apply Bleed.", "ONCRITICAL", "bleed", 1),
                D("Leather", "Jaguar", "On first hit, crit threshold +1 this turn.", "ONFIRSTHIT", "hero_crit_threshold", 1),
                D("Leather", "Badger", "On take hit, apply Harden.", "ONTAKEHIT", "harden", 1),
                D("Leather", "Tiger", "On first hit, next action +10% damage.", "ONFIRSTHIT", "hero_next_action_damage", 10, scope: "ACTION"),
                D("Leather", "Ferret", "On crit, skip the next strip slot.", "ONCRITICAL", "strip_skip", 1, scope: ""),
                D("Leather", "Deer", "After a miss, next action +10% speed.", "ONAFTERMISS", "hero_next_action_speed", 10, scope: "ACTION"),
                D("Leather", "Auroch", "On combo end, weapon damage +1 this turn.", "ONCOMBOEND", "hero_weapon_damage", 1),
                D("Leather", "TasmanianDevil", "On crit, repeat the current strip slot.", "ONCRITICAL", "strip_repeat", 1, scope: ""),
                // Wood
                D("Wood", "Osprey", "On crit, apply Pierce.", "ONCRITICAL", "pierce", 1),
                D("Wood", "Swallow", "On combo end, loop the combo strip.", "ONCOMBOEND", "strip_loop", 1, scope: ""),
                D("Wood", "Sicklebill", "On connect, apply Pierce.", "ONCONNECT", "pierce", 1),
                D("Wood", "Corvid", "On kill, replace next d20 with 20.", "ONKILL", "replace_next_roll:20", 20, scope: ""),
                D("Wood", "Ant", "On crit, next action +1 multihit.", "ONCRITICAL", "hero_next_action_multihit", 1, scope: "ACTION"),
                D("Wood", "Scorpion", "On crit, apply Poison.", "ONCRITICAL", "poison", 0.5),
                D("Wood", "MantisShrimp", "On crit, apply Pierce.", "ONCRITICAL", "pierce", 1),
                D("Wood", "Octopus", "On crit, pick a random next strip action.", "ONCRITICAL", "strip_random", 1, scope: ""),
                D("Wood", "Sawfish", "On connect, apply Pierce.", "ONCONNECT", "pierce", 1),
                D("Wood", "Starfish", "After a miss, heal 2.", "ONAFTERMISS", "heal", 2, scope: ""),
                // Cloth
                D("Cloth", "Kingfisher", "On first hit, apply Pierce.", "ONFIRSTHIT", "pierce", 1),
                D("Cloth", "Shrike", "On crit, apply Pierce.", "ONCRITICAL", "pierce", 1),
                D("Cloth", "Hawk", "On crit, next action +10% damage.", "ONCRITICAL", "hero_next_action_damage", 10, scope: "ACTION"),
                D("Cloth", "Raven", "On crit, pick a random next strip action.", "ONCRITICAL", "strip_random", 1, scope: ""),
                // Unknown
                D("Unknown", "Squid", "On miss, next action +10% speed.", "ONMISS", "hero_next_action_speed", 10, scope: "ACTION"),
                D("Unknown", "Cuttlefish", "On miss, tax foe accuracy by 1 this turn.", "ONMISS", "enemy_accuracy", -1),
                D("Unknown", "Piranha", "On crit, next action +1 multihit.", "ONCRITICAL", "hero_next_action_multihit", 1, scope: "ACTION"),
                // Strange
                D("Strange", "Dragonfly", "On crit, next action +1 multihit.", "ONCRITICAL", "hero_next_action_multihit", 1, scope: "ACTION"),
                D("Strange", "Millipede", "On crit, next action +1 multihit.", "ONCRITICAL", "hero_next_action_multihit", 1, scope: "ACTION"),
                D("Strange", "Spider", "On crit, apply Slow.", "ONCRITICAL", "slow", 1),
                D("Strange", "GiantSquid", "On crit, next action +1 multihit.", "ONCRITICAL", "hero_next_action_multihit", 1, scope: "ACTION"),
                D("Strange", "Stonefish", "On connect, apply Poison.", "ONCONNECT", "poison", 0.25),
                D("Strange", "SpermWhale", "On connect, apply Pierce.", "ONCONNECT", "pierce", 1),
                // Celestial
                D("Celestial", "Kraken", "On crit, next action +1 multihit.", "ONCRITICAL", "hero_next_action_multihit", 1, scope: "ACTION"),
                D("Celestial", "Hydra", "On take hit, heal 2.", "ONTAKEHIT", "heal", 2, scope: ""),
            };
            return list;
        }
    }
}
