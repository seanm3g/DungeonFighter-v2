using System.Collections.Generic;

namespace RPGGame.Data.ItemGeneration
{
    /// <summary>
    /// What to roll. Every field is optional: unset fields roll normally. Guaranteed drops, new-game armor, trade-ups,
    /// the Item Generation lab and sim tool overrides are all constraints here, not separate pipelines.
    /// </summary>
    public sealed class ItemRollRequest
    {
        public int HeroLevel { get; set; } = 1;
        public int DungeonLevel { get; set; } = 1;

        /// <summary>Overrides the loot-level formula (trade-ups, lab).</summary>
        public int? ItemLevel { get; set; }

        public Character? Hero { get; set; }
        public string? DungeonTheme { get; set; }

        /// <summary>Exact slot: weapon / head / chest / legs / feet.</summary>
        public string? Slot { get; set; }

        /// <summary>true = weapons only, false = armor only, null = slot roll decides.</summary>
        public bool? Weapon { get; set; }

        public WeaponType? WeaponType { get; set; }

        /// <summary>Base item class (barbarian / warrior / rogue / wizard); null = class roll.</summary>
        public string? Class { get; set; }

        public int? Tier { get; set; }

        /// <summary>Forced rarity (no rarity roll or rarity roll-up).</summary>
        public string? Rarity { get; set; }

        /// <summary>False turns off every roll-up (item level, rarity, pool, tier).</summary>
        public bool AllowRollUps { get; set; } = true;

        /// <summary>Skip starter-tagged bases when anything else fits (new-game bonus armor).</summary>
        public bool ExcludeStarter { get; set; }

        /// <summary>0–100; null = the hero's magic find. Scales roll-up chances.</summary>
        public double? MagicFind { get; set; }

        /// <summary>Tools: every item takes this material (DUNGEONSIM --material).</summary>
        public string? ForcedMaterial { get; set; }

        /// <summary>Tools: force or strip one line (DUNGEONSIM --affix: mod:Heavy, mod:-, stat:CRIT, stat:-, stat:-CRIT).</summary>
        public string? ForcedAffix { get; set; }

        /// <summary>Item seed; null = drawn from the loot RNG.</summary>
        public int? Seed { get; set; }
    }

    /// <summary>One rolled line, as recorded on the spec.</summary>
    public sealed class ItemRollLine
    {
        public string Pool { get; init; } = "";
        public string Family { get; init; } = "";
        public int Tier { get; init; }
        public string Keyword { get; init; } = "";
        public double Value { get; init; }
        public bool RolledUp { get; init; }

        /// <summary>Pool the line was planned in, when it was re-routed or rolled up into another.</summary>
        public string? From { get; init; }

        public override string ToString() =>
            $"{Pool}:{Family} T{Tier} {Keyword} {Value:0.##}{(RolledUp ? " (rolled up)" : "")}{(From != null ? $" (from {From})" : "")}";
    }

    /// <summary>Every decision the roller made for one item (for the lab, logs and tests).</summary>
    public sealed class ItemRollSpec
    {
        public int Seed { get; set; }
        public int LootLevel { get; set; }
        public int ItemLevel { get; set; }
        public string Slot { get; set; } = "";
        public string Class { get; set; } = "";
        public int Tier { get; set; }
        public string BaseName { get; set; } = "";
        public string Rarity { get; set; } = "";
        /// <summary>The rarity first rolled when a roll-up raised it (empty otherwise); lines roll at this rarity.</summary>
        public string DropRarity { get; set; } = "";
        public string Material { get; set; } = "";
        /// <summary>The base modifier rule(s) as rolled (catalog template with its values filled in).</summary>
        public string Implicit { get; set; } = "";
        /// <summary>The base modifier's name (catalog <c>modifierName</c>).</summary>
        public string ImplicitName { get; set; } = "";

        /// <summary>The first rolled value of the base modifier (null when it has none).</summary>
        public double? ImplicitValue { get; set; }
        public Dictionary<string, int> PlannedLines { get; } = new();
        public List<ItemRollLine> Lines { get; } = new();
        public List<string> RollUps { get; } = new();

        /// <summary>Filters the base pick had to loosen (class, tier) because nothing matched.</summary>
        public List<string> Relaxed { get; } = new();
    }
}
