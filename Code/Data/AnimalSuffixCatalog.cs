using System;
using System.Collections.Generic;

namespace RPGGame.Data
{
    /// <summary>Authoring row for an animal StatBonus suffix → trigger identity + taxon tag.</summary>
    public readonly struct AnimalSuffixDef
    {
        public AnimalSuffixDef(
            string suffixName,
            string triggerName,
            string taxon,
            string description,
            string when,
            string mechanics,
            double value,
            string channel = "combat",
            string scope = "TURN",
            string? filters = null,
            string effectTarget = "hero")
        {
            SuffixName = suffixName;
            TriggerName = triggerName;
            Taxon = taxon;
            Description = description;
            When = when;
            Mechanics = mechanics;
            Value = value;
            Channel = channel;
            Scope = scope;
            Filters = filters;
            EffectTarget = effectTarget;
        }

        public string SuffixName { get; }
        public string TriggerName { get; }
        public string Taxon { get; }
        public string Description { get; }
        public string When { get; }
        public string Mechanics { get; }
        public double Value { get; }
        public string Channel { get; }
        public string Scope { get; }
        public string? Filters { get; }
        public string EffectTarget { get; }
    }

    /// <summary>Canonical animal suffix → taxon → combat identity map.</summary>
    public static class AnimalSuffixCatalog
    {
        public static IReadOnlyList<AnimalSuffixDef> All { get; } = Build();

        private static AnimalSuffixDef A(
            string animal,
            string taxon,
            string desc,
            string when,
            string mech,
            double value,
            string channel = "combat",
            string scope = "TURN",
            string? filters = null,
            string effectTarget = "hero")
        {
            string key = animal.Replace(" ", "").Replace("-", "");
            return new AnimalSuffixDef(
                suffixName: "of the " + animal,
                triggerName: key + "Suffix",
                taxon: taxon,
                description: desc,
                when: when,
                mechanics: mech,
                value: value,
                channel: channel,
                scope: scope,
                filters: filters,
                effectTarget: effectTarget);
        }

        private static List<AnimalSuffixDef> Build()
        {
            var list = new List<AnimalSuffixDef>
            {
                // Shell
                A("Tortoise", "shell", "On miss, apply Harden.", "ONMISS", "harden", 1, effectTarget: "self"),
                A("Crab", "shell", "On take hit, tax foe accuracy by 1 this turn.", "ONTAKEHIT", "enemy_accuracy", -1, effectTarget: "enemy"),
                A("Pangolin", "shell", "On take hit, apply Harden.", "ONTAKEHIT", "harden", 1, effectTarget: "self"),
                A("Armadillo", "shell", "After a miss, apply Harden.", "ONAFTERMISS", "harden", 1, effectTarget: "self"),
                A("Abalone", "shell", "On take hit, apply Fortify.", "ONTAKEHIT", "fortify", 1, effectTarget: "self"),
                A("Lobster", "shell", "On room clear, +1 armor for the fight.", "ONROOMSCLEARED", "armor", 1, scope: "FIGHT"),
                A("Horseshoe Crab", "shell", "After a miss, heal 2.", "ONAFTERMISS", "heal", 2),
                A("Nautilus", "shell", "On connect, loop the combo strip.", "ONCONNECT", "strip_loop", 1, scope: ""),
                A("Trilobite", "shell", "On combo, repeat the current strip slot.", "ONCOMBO", "strip_repeat", 1, scope: ""),
                A("Scarab Beetle", "shell", "On room clear, heal 2.", "ONROOMSCLEARED", "heal", 2, scope: ""),
                A("Fiddler Crab", "shell", "On crit, next action +10% amp.", "ONCRITICAL", "hero_next_action_amp", 10, scope: "ACTION"),

                // Reptile
                A("Cobra", "reptile", "On connect, apply Poison.", "ONCONNECT", "poison", 1, effectTarget: "enemy"),
                A("Python", "reptile", "On combo, apply Slow.", "ONCOMBO", "slow", 1, effectTarget: "enemy"),
                A("Crocodile", "reptile", "On combo, repeat the current strip slot.", "ONCOMBO", "strip_repeat", 1, scope: ""),
                A("Saltwater Crocodile", "reptile", "On first hit, apply Slow.", "ONFIRSTHIT", "slow", 1, effectTarget: "enemy"),
                A("Viper", "reptile", "On crit, apply Poison.", "ONCRITICAL", "poison", 1, effectTarget: "enemy"),
                A("Fer-de-Lance", "reptile", "On connect, apply Poison.", "ONCONNECT", "poison", 1, effectTarget: "enemy"),
                A("Jade Serpent", "reptile", "On crit, apply Poison.", "ONCRITICAL", "poison", 1, effectTarget: "enemy"),
                A("Basilisk", "reptile", "On crit, apply Slow.", "ONCRITICAL", "slow", 1, effectTarget: "enemy"),
                A("Gecko", "reptile", "On connect, gain miss salvage for the fight.", "ONCONNECT", "salvage_miss", 1, scope: "FIGHT"),
                A("Salamander", "reptile", "On crit, apply Burn.", "ONCRITICAL", "burn", 1, effectTarget: "enemy"),
                A("Toad", "reptile", "On take hit, apply Poison to the attacker.", "ONTAKEHIT", "poison", 1, effectTarget: "enemy"),

                // Bird
                A("Cheetah", "beast", "On first hit, next action +15% speed.", "ONFIRSTHIT", "hero_next_action_speed", 15, scope: "ACTION"), // land striker — beast
                A("Peregrine Falcon", "bird", "On crit, next action +10% amp.", "ONCRITICAL", "hero_next_action_amp", 10, scope: "ACTION"),
                A("Hummingbird", "bird", "On connect, weapon speed +1 this turn.", "ONCONNECT", "hero_weapon_speed", 1),
                A("Swift", "bird", "After a miss, next action +10% speed.", "ONAFTERMISS", "hero_next_action_speed", 10, scope: "ACTION"),
                A("Dragonfly", "bug", "On combo, next action +1 multihit.", "ONCOMBO", "hero_next_action_multihit", 1, scope: "ACTION"),
                A("Damselfly", "bug", "After a miss, next action +10% speed.", "ONAFTERMISS", "hero_next_action_speed", 10, scope: "ACTION"),
                A("Osprey", "bird", "On crit, apply Pierce.", "ONCRITICAL", "pierce", 1, effectTarget: "enemy"),
                A("Kingfisher", "bird", "On first hit, apply Pierce.", "ONFIRSTHIT", "pierce", 1, effectTarget: "enemy"),
                A("Sparrowhawk", "bird", "On first hit, crit faces start at 19 this turn.", "ONFIRSTHIT", "crit_face_min:19", 19),
                A("Firefly", "bug", "On an even roll, apply Expose.", "ONEVEN", "expose", 1, effectTarget: "enemy"),
                A("Golden Eagle", "bird", "On kill, replace next d20 with 20.", "ONKILL", "replace_next_roll:20", 20, scope: ""),
                A("Crane", "bird", "On connect, apply Focus.", "ONCONNECT", "focus", 1, effectTarget: "self"),
                A("Heron", "bird", "On first hit, apply Pierce.", "ONFIRSTHIT", "pierce", 1, effectTarget: "enemy"),
                A("Swallow", "bird", "On combo end, loop the combo strip.", "ONCOMBOEND", "strip_loop", 1, scope: ""),
                A("Shrike", "bird", "On crit, apply Pierce.", "ONCRITICAL", "pierce", 1, effectTarget: "enemy"),
                A("Rook", "bird", "On combo end, foe hit threshold +1 this turn.", "ONCOMBOEND", "enemy_hit_threshold", 1, effectTarget: "enemy"),
                A("Ibis", "bird", "On connect, apply Pierce.", "ONCONNECT", "pierce", 1, effectTarget: "enemy"),
                A("Kestrel", "bird", "On connect, apply Focus.", "ONCONNECT", "focus", 1, effectTarget: "self"),
                A("Nighthawk", "bird", "After a miss, next action +10% speed.", "ONAFTERMISS", "hero_next_action_speed", 10, scope: "ACTION"),
                A("Sicklebill", "bird", "On connect, apply Pierce.", "ONCONNECT", "pierce", 1, effectTarget: "enemy"),
                A("Hawk", "bird", "On crit, next action +10% amp.", "ONCRITICAL", "hero_next_action_amp", 10, scope: "ACTION"),
                A("Owl", "bird", "On first hit, crit faces start at 18 this turn.", "ONFIRSTHIT", "crit_face_min:18", 18),
                A("Jay", "bird", "On miss, foe next action +10% damage.", "ONMISS", "enemy_next_action_damage", 10, scope: "ACTION", effectTarget: "enemy"),
                A("Magpie", "bird", "On kill, replace next d20 with 15.", "ONKILL", "replace_next_roll:15", 15, scope: ""),
                A("Jackdaw", "bird", "On connect, pick a random next strip action.", "ONCONNECT", "strip_random", 1, scope: ""),
                A("Corvid", "bird", "On kill, replace next d20 with 20.", "ONKILL", "replace_next_roll:20", 20, scope: ""),
                A("Raven", "bird", "On connect, pick a random next strip action.", "ONCONNECT", "strip_random", 1, scope: ""),
                A("Bloodhound", "beast", "On connect, accuracy +1 this turn.", "ONCONNECT", "hero_accuracy", 1),

                // Bug
                A("Ant", "bug", "On combo, next action +1 multihit.", "ONCOMBO", "hero_next_action_multihit", 1, scope: "ACTION"),
                A("Millipede", "bug", "On combo, next action +1 multihit.", "ONCOMBO", "hero_next_action_multihit", 1, scope: "ACTION"),
                A("Mantis", "bug", "On first hit, next action +10% amp.", "ONFIRSTHIT", "hero_next_action_amp", 10, scope: "ACTION"),
                A("Praying Mantis", "bug", "On first hit, next action +10% amp.", "ONFIRSTHIT", "hero_next_action_amp", 10, scope: "ACTION"),
                A("Scorpion", "bug", "On crit, apply Poison.", "ONCRITICAL", "poison", 1, effectTarget: "enemy"),
                A("Spider", "bug", "On connect, apply Slow.", "ONCONNECT", "slow", 1, effectTarget: "enemy"),
                A("Clockwork Beetle", "bug", "On combo, retrigger the next strip action.", "ONCOMBO", "retrigger_next", 1, scope: ""),
                A("Pistol Shrimp", "bug", "On crit, apply Pierce.", "ONCRITICAL", "pierce", 1, effectTarget: "enemy"),
                A("Mantis Shrimp", "bug", "On crit, apply Pierce.", "ONCRITICAL", "pierce", 1, effectTarget: "enemy"),

                // Fish / sea
                A("Squid", "fish", "On miss, next action +10% speed.", "ONMISS", "hero_next_action_speed", 10, scope: "ACTION"),
                A("Giant Squid", "fish", "On combo, next action +1 multihit.", "ONCOMBO", "hero_next_action_multihit", 1, scope: "ACTION"),
                A("Octopus", "fish", "On connect, pick a random next strip action.", "ONCONNECT", "strip_random", 1, scope: ""),
                A("Cuttlefish", "fish", "On miss, tax foe accuracy by 1 this turn.", "ONMISS", "enemy_accuracy", -1, effectTarget: "enemy"),
                A("Stonefish", "fish", "On connect, apply Poison.", "ONCONNECT", "poison", 1, effectTarget: "enemy"),
                A("Sawfish", "fish", "On connect, apply Pierce.", "ONCONNECT", "pierce", 1, effectTarget: "enemy"),
                A("Piranha", "fish", "On connect, next action +1 multihit.", "ONCONNECT", "hero_next_action_multihit", 1, scope: "ACTION"),
                A("Sperm Whale", "fish", "On connect, apply Pierce.", "ONCONNECT", "pierce", 1, effectTarget: "enemy"),
                A("Starfish", "fish", "After a miss, heal 2.", "ONAFTERMISS", "heal", 2, scope: ""),
                A("Kraken", "mythic", "On combo, next action +1 multihit.", "ONCOMBO", "hero_next_action_multihit", 1, scope: "ACTION"),

                // Beast
                A("Bison", "beast", "On first hit, next action +10% damage.", "ONFIRSTHIT", "hero_next_action_damage", 10, scope: "ACTION"),
                A("Elephant", "beast", "On connect with same action as last, this swing +100% damage.", "ONCONNECT", "hero_action_damage", 100, scope: "", filters: "IFSAMESACTION"),
                A("Cape Buffalo", "beast", "On take hit, next action +10% damage.", "ONTAKEHIT", "hero_next_action_damage", 10, scope: "ACTION"),
                A("Gorilla", "beast", "On combo end, foe hit threshold +1 this turn.", "ONCOMBOEND", "enemy_hit_threshold", 1, effectTarget: "enemy"),
                A("Hippopotamus", "beast", "On crit, apply Bleed.", "ONCRITICAL", "bleed", 1, effectTarget: "enemy"),
                A("Golem", "mythic", "On miss, apply Harden.", "ONMISS", "harden", 1, effectTarget: "self"),
                A("Moose", "beast", "On connect, foe weapon speed -1 this turn.", "ONCONNECT", "enemy_weapon_speed", -1, effectTarget: "enemy"),
                A("Elk", "beast", "On first hit, next action +10% amp.", "ONFIRSTHIT", "hero_next_action_amp", 10, scope: "ACTION"),
                A("Water Buffalo", "beast", "On take hit, apply Fortify.", "ONTAKEHIT", "fortify", 1, effectTarget: "self"),
                A("Ox", "beast", "On room clear, weapon damage +1 for the dungeon.", "ONROOMSCLEARED", "hero_weapon_damage", 1, scope: "DUNGEON"),
                A("Mammoth", "beast", "On first hit, apply Fortify.", "ONFIRSTHIT", "fortify", 1, effectTarget: "self"),
                A("Stallion", "beast", "On first hit, next action +10% speed.", "ONFIRSTHIT", "hero_next_action_speed", 10, scope: "ACTION"),
                A("Ram", "beast", "On connect, apply Slow.", "ONCONNECT", "slow", 1, effectTarget: "enemy"),
                A("Mule", "beast", "On miss, stop strip progression.", "ONMISS", "strip_stop", 1, scope: ""),
                A("Kodiak Bear", "beast", "On crit, apply Bleed.", "ONCRITICAL", "bleed", 1, effectTarget: "enemy"),
                A("Cave Bear", "beast", "On take hit, apply Harden.", "ONTAKEHIT", "harden", 1, effectTarget: "self"),
                A("Grizzly Bear", "beast", "On crit, apply Bleed.", "ONCRITICAL", "bleed", 1, effectTarget: "enemy"),
                A("Wolverine", "beast", "On take hit, next action +10% damage.", "ONTAKEHIT", "hero_next_action_damage", 10, scope: "ACTION"),
                A("Jaguar", "beast", "On first hit, crit threshold +1 this turn.", "ONFIRSTHIT", "hero_crit_threshold", 1),
                A("Hyena", "beast", "On kill, heal 2.", "ONKILL", "heal", 2, scope: ""),
                A("Lynx", "beast", "On miss, tax foe accuracy by 1 this turn.", "ONMISS", "enemy_accuracy", -1, effectTarget: "enemy"),
                A("Boar", "beast", "On connect, gain miss salvage for the fight.", "ONCONNECT", "salvage_miss", 1, scope: "FIGHT"),
                A("Badger", "beast", "On take hit, apply Harden.", "ONTAKEHIT", "harden", 1, effectTarget: "self"),
                A("Wolf", "beast", "On combo, combo threshold +1 this turn.", "ONCOMBO", "hero_combo_threshold", 1),
                A("Dire Wolf", "beast", "On kill, retrigger your opener.", "ONKILL", "retrigger_opener", 1, scope: ""),
                A("Panther", "beast", "On first hit, crit faces start at 18 this turn.", "ONFIRSTHIT", "crit_face_min:18", 18),
                A("Tiger", "beast", "On first hit, next action +10% amp.", "ONFIRSTHIT", "hero_next_action_amp", 10, scope: "ACTION"),
                A("Snow Leopard", "beast", "On first hit, next action +10% amp.", "ONFIRSTHIT", "hero_next_action_amp", 10, scope: "ACTION"),
                A("Lion", "beast", "On combo end, foe hit threshold +1 this turn.", "ONCOMBOEND", "enemy_hit_threshold", 1, effectTarget: "enemy"),
                A("Fox", "beast", "After a miss, hit threshold +1 this turn.", "ONAFTERMISS", "hero_hit_threshold", 1),
                A("Ferret", "beast", "On connect, skip the next strip slot.", "ONCONNECT", "strip_skip", 1, scope: ""),
                A("Caracal", "beast", "On crit, crit threshold +1 this turn.", "ONCRITICAL", "hero_crit_threshold", 1),
                A("Otter", "beast", "On an even roll, heal 1.", "ONEVEN", "heal", 1, scope: ""),
                A("Weasel", "beast", "On connect with same action as last, this swing +100% damage.", "ONCONNECT", "hero_action_damage", 100, scope: "", filters: "IFSAMESACTION"),
                A("Deer", "beast", "After a miss, next action +10% speed.", "ONAFTERMISS", "hero_next_action_speed", 10, scope: "ACTION"),
                A("Pronghorn", "beast", "On connect, weapon speed +1 this turn.", "ONCONNECT", "hero_weapon_speed", 1),
                A("Marten", "beast", "On connect, skip the next strip slot.", "ONCONNECT", "strip_skip", 1, scope: ""),
                A("Mongoose", "beast", "On connect vs a DoT'd foe, apply Pierce.", "ONCONNECT", "pierce", 1, effectTarget: "enemy", filters: "IFTARGETUNDERDOT"),
                A("Auroch", "beast", "On combo end, weapon damage +1 this turn.", "ONCOMBOEND", "hero_weapon_damage", 1),
                A("Rhinoceros", "beast", "On first hit, next action +15% damage.", "ONFIRSTHIT", "hero_next_action_damage", 15, scope: "ACTION"),
                A("Orangutan", "beast", "On connect, jump ahead on the combo strip.", "ONCONNECT", "strip_jump", 1, scope: ""),
                A("Monkey", "beast", "On connect, pick a random next strip action.", "ONCONNECT", "strip_random", 1, scope: ""),
                A("Tasmanian Devil", "beast", "On combo, repeat the current strip slot.", "ONCOMBO", "strip_repeat", 1, scope: ""),
                A("Velociraptor", "beast", "On combo, combo threshold +1 this turn.", "ONCOMBO", "hero_combo_threshold", 1),

                // Mythic
                A("Kirin", "mythic", "On crit, heal 3.", "ONCRITICAL", "heal", 3, scope: ""),
                A("Dragon", "mythic", "On crit, apply Burn.", "ONCRITICAL", "burn", 1, effectTarget: "enemy"),
                A("Phoenix", "mythic", "On crit miss, heal 2.", "ONCRITICALMISS", "heal", 2, scope: ""),
                A("Hydra", "mythic", "On take hit, heal 2.", "ONTAKEHIT", "heal", 2, scope: ""),
                A("Thunderbird", "mythic", "On crit, apply Burn.", "ONCRITICAL", "burn", 1, effectTarget: "enemy"),
                A("Kitsune", "mythic", "On connect, replace the next strip action.", "ONCONNECT", "strip_replace_next", 1, scope: ""),
                A("Sphinx", "mythic", "On connect, apply Focus.", "ONCONNECT", "focus", 1, effectTarget: "self"),
            };

            // Ensure Cheetah is beast (already set). Dragonfly/Damselfly/Firefly are bug.
            return list;
        }

        /// <summary>Per-taxon set (From) and fantasy amp (To) synergies.</summary>
        public static IReadOnlyList<TriggerIdentityData> BuildTaxonSynergies(int startingId)
        {
            var list = new List<TriggerIdentityData>();
            int id = startingId;
            foreach (var taxon in StatBonusTriggerMerge.TaxonTags)
            {
                string cap = char.ToUpperInvariant(taxon[0]) + taxon.Substring(1);
                list.Add(new TriggerIdentityData
                {
                    Id = id++,
                    Name = StatBonusTriggerMerge.TaxonSetFromName(taxon),
                    Description = $"While equipped with 2+ {cap}-tagged gear, +2 armor.",
                    EffectTarget = "hero",
                    When = "WHILE_EQUIPPED",
                    Count = "1",
                    Scope = "",
                    Mechanics = "armor",
                    Value = 2,
                    Filters = $"IFGEARHASTAG:{taxon}:count>=2",
                    Channel = "equip"
                });
                list.Add(new TriggerIdentityData
                {
                    Id = id++,
                    Name = StatBonusTriggerMerge.TaxonAmpToName(taxon),
                    Description = TaxonAmpDescription(taxon),
                    EffectTarget = TaxonAmpTarget(taxon),
                    When = TaxonAmpWhen(taxon),
                    Count = "1",
                    Scope = TaxonAmpScope(taxon),
                    Mechanics = TaxonAmpMech(taxon),
                    Value = TaxonAmpValue(taxon),
                    Filters = $"IFGEARHASTAG:{taxon}",
                    Channel = "combat"
                });
            }
            return list;
        }

        private static string TaxonAmpDescription(string taxon) => taxon switch
        {
            "shell" => "On connect with Shell gear, apply Harden.",
            "reptile" => "On connect with Reptile gear, apply Poison.",
            "bird" => "On connect with Bird gear, next action +10% speed.",
            "bug" => "On combo with Bug gear, next action +1 multihit.",
            "fish" => "On miss with Fish gear, tax foe accuracy by 1.",
            "beast" => "On first hit with Beast gear, next action +10% damage.",
            "mythic" => "On crit with Mythic gear, heal 2.",
            _ => $"On connect with {taxon} gear, gain Focus."
        };

        private static string TaxonAmpWhen(string taxon) => taxon switch
        {
            "bug" => "ONCOMBO",
            "fish" => "ONMISS",
            "beast" => "ONFIRSTHIT",
            "mythic" => "ONCRITICAL",
            _ => "ONCONNECT"
        };

        private static string TaxonAmpMech(string taxon) => taxon switch
        {
            "shell" => "harden",
            "reptile" => "poison",
            "bird" => "hero_next_action_speed",
            "bug" => "hero_next_action_multihit",
            "fish" => "enemy_accuracy",
            "beast" => "hero_next_action_damage",
            "mythic" => "heal",
            _ => "focus"
        };

        private static double TaxonAmpValue(string taxon) => taxon switch
        {
            "bird" => 10,
            "bug" => 1,
            "fish" => -1,
            "beast" => 10,
            "mythic" => 2,
            _ => 1
        };

        private static string TaxonAmpScope(string taxon) => taxon switch
        {
            "bird" or "bug" or "beast" => "ACTION",
            "mythic" => "",
            _ => "TURN"
        };

        private static string TaxonAmpTarget(string taxon) => taxon switch
        {
            "shell" => "self",
            "reptile" or "fish" => "enemy",
            _ => "hero"
        };
    }
}
