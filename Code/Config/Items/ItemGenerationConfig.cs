using System;
using System.Collections.Generic;

namespace RPGGame
{
    /// <summary>One roll-up: after a ranked roll, <see cref="Chance"/> to step up a rank; each success rolls again at
    /// chance × <see cref="Decay"/>^n, at most <see cref="Max"/> steps.</summary>
    public sealed class RollUpRule
    {
        public double Chance { get; set; }
        public double Decay { get; set; } = 0.5;
        public int Max { get; set; } = 1;

        public RollUpRule() { }

        public RollUpRule(double chance, double decay, int max)
        {
            Chance = chance;
            Decay = decay;
            Max = max;
        }

        /// <summary>Steps gained (0..Max). <paramref name="chanceScale"/> multiplies the chance (magic find).</summary>
        public int Roll(Random rng, double chanceScale = 1.0)
        {
            int steps = 0;
            double chance = Math.Clamp(Chance * chanceScale, 0, 1);
            while (steps < Max && rng.NextDouble() < chance)
            {
                steps++;
                chance *= Decay;
            }
            return steps;
        }
    }

    /// <summary>
    /// Item generation knobs (<c>lootSystem.itemGeneration</c> in a balance patch). Design:
    /// Documentation/ITEM_GENERATION_REDESIGN_2026-11-14.md. Rarity line counts live in <c>RarityTable.json</c>, mod
    /// families and tiers in <c>ItemMods.json</c>.
    /// </summary>
    public sealed class ItemGenerationConfig
    {
        /// <summary>Slot roll weights (weapon / head / chest / legs / feet).</summary>
        public Dictionary<string, double> SlotWeights { get; set; } = new()
        {
            // A third of drops are weapons, the rest split evenly over the four armor slots (user 2026-11-21; was 4:1:1:1:1).
            ["weapon"] = 2, ["head"] = 1, ["chest"] = 1, ["legs"] = 1, ["feet"] = 1
        };

        /// <summary>Share of drops on the hero's class bases; the rest split evenly over the other classes.</summary>
        public double HeroClassShare { get; set; } = 0.40;

        /// <summary>Chance a weapon takes one of its base class's three set materials (else any of the 12).</summary>
        public double WeaponClassMaterialShare { get; set; } = 0.9;

        /// <summary>Chance armor takes one of its base class's three set materials (else the rarity-matched pool).</summary>
        public double ArmorClassMaterialShare { get; set; } = 0.5;

        /// <summary>
        /// Each line (after the guaranteed actions) picks its pool by these weights × the item's influence tilt, among
        /// the pools that still have something this item can roll. The one dial for how often each kind of line shows up.
        /// </summary>
        public Dictionary<string, double> PoolWeights { get; set; } = DefaultPoolWeights();

        /// <summary>
        /// Rebuild 2.1 (D0.7, D5.6): Commons are stats and a material only: no base modifier, no action, no mechanic
        /// lines. Starter gear keeps its action. False restores the old Commons.
        /// </summary>
        public bool CommonsArePlain { get; set; } = true;

        /// <summary>Action lines every item gets before the weighted lines (they count toward the rarity's line count).</summary>
        public int GuaranteedActionLines { get; set; } = 1;

        /// <summary>
        /// Suffix kinds each rarity always gets (common = descriptor, uncommon = animal, rare = poetic phrase). Suffix
        /// lines count toward the rarity's line count; weighted lines fill the rest with prefixes and actions.
        /// </summary>
        public Dictionary<string, List<string>> SuffixKinds { get; set; } = DefaultSuffixKinds();

        /// <summary>Chance an item whose rarity doesn't guarantee a poetic (rare) suffix rolls one anyway. 0 by user call
        /// (2026-11-15): poetic phrases only on Rare and up.</summary>
        public double PoeticSuffixChance { get; set; } = 0.0;

        public static Dictionary<string, List<string>> DefaultSuffixKinds() => new(StringComparer.OrdinalIgnoreCase)
        {
            ["Common"] = new() { "common" },
            ["Uncommon"] = new() { "uncommon" },
            ["Rare"] = new() { "rare" },
            ["Epic"] = new() { "rare" },
            ["Legendary"] = new() { "uncommon", "rare" },
            ["Mythic"] = new() { "common", "uncommon", "rare" },
        };

        public IReadOnlyList<string> SuffixKindsFor(string? rarity)
        {
            if (rarity != null && SuffixKinds != null)
                foreach (var kv in SuffixKinds)
                    if (string.Equals(kv.Key, rarity.Trim(), StringComparison.OrdinalIgnoreCase))
                        return kv.Value ?? new List<string>();
            return new[] { "common" };
        }

        public RollUpRule ItemLevelRollUp { get; set; } = new(0.10, 0.5, 5);
        /// <summary>
        /// Rarity roll-up (user 2026-11-15): 2% per step, no decay, any number of steps (up to Mythic). A rolled-up item
        /// keeps its drop rarity for line values (tier gates such as the common-only highs) and gets the higher rarity's
        /// line count, suffix kinds and name.
        /// </summary>
        public RollUpRule RarityRollUp { get; set; } = new(0.02, 1.0, 5);
        public RollUpRule TierRollUp { get; set; } = new(0.08, 0.5, 2);

        public static Dictionary<string, double> DefaultPoolWeights() => new()
        {
            ["stat"] = 50, ["mechanic"] = 0, ["material"] = 25, ["action"] = 15
        };

        /// <summary>Roll-up chances × (1 + magic find / 100 × this).</summary>
        public double MagicFindRollUpScale { get; set; } = 1.0;

        /// <summary>Influence: weight × min(cap, 1 + affinity × matching tags).</summary>
        public double InfluenceAffinity { get; set; } = 0.5;
        public double InfluenceCap { get; set; } = 3.0;

        /// <summary>Chance a weapon's action comes from its own class's actions (else the rest of the gear pool).</summary>
        public double WeaponActionClassShare { get; set; } = 0.7;

        /// <summary>
        /// Weapon swing time × (1 − this × (tier − 1)): higher tiers swing faster. Damage has no item-level term; each
        /// base's catalog range (damageMin..damageMax) already carries its tier (user 2026-11-15).
        /// </summary>
        public double WeaponSpeedPerTier { get; set; } = 0.04;

        /// <summary>Catalog attributes on armor (STR / AGI / TEC / INT) × (1 + this × (item level − 1)).</summary>
        public double AttributePerItemLevel { get; set; } = 0.02;

        /// <summary>Percent magnitudes on a base item's implicit (next-action / this-swing damage, amp, speed)
        /// × (1 + this × (item level − 1)). Implicits that already scale (scaleFrom) or are discrete never scale.</summary>
        public double ImplicitPercentPerItemLevel { get; set; } = 0.01;

        /// <summary>Flat magnitudes on a base item's implicit (heal, armor, weapon damage) × (1 + this × (item level − 1)).</summary>
        public double ImplicitFlatPerItemLevel { get; set; } = 0.02;

        /// <summary>Pack rule: this many equipped pieces with the same animal suffix (stat suffix family) form a pack.</summary>
        public int AnimalPackPieces { get; set; } = 2;

        /// <summary>Pack rule: lines of a packed animal count × this.</summary>
        public double AnimalPackMultiplier { get; set; } = 2.0;

        /// <summary>A hybrid line (two stats in one keyword) rolls each part at this share of a single-stat line.</summary>
        public double HybridPartShare { get; set; } = 0.6;

        // ---- Phase 11 (Documentation/CORE_DESIGN_PASS_2026-11-17.md, D11.2-D11.8) ----

        /// <summary>Chance an item's class affinity is one of its base class's 3 hybrids instead of the class itself.</summary>
        public double HybridAffinityShare { get; set; } = 0.3;

        /// <summary>Weight × this for a family serving the item's own affinity (strong tilt, not a gate).</summary>
        public double OwnClassTilt { get; set; } = 3.5;

        /// <summary>Weight × this for a family serving a related class (a hybrid and its parent, either way).</summary>
        public double RelatedClassTilt { get; set; } = 1.2;

        /// <summary>Weight × this for a family serving an unrelated class (rare off-class lines).</summary>
        public double OffClassTilt { get; set; } = 1.0;

        /// <summary>Weight × this for a neutral family (no classes).</summary>
        public double NeutralClassTilt { get; set; } = 1.0;

        /// <summary>Per drop rarity: chance the item gets one line outside its slot domain (at most one per item, never the
        /// first mechanic line, one tier lower). D11.5.</summary>
        public Dictionary<string, double> OffSlotChance { get; set; } = DefaultOffSlotChance();

        /// <summary>Tiers an off-slot line drops below what it would roll (floor: the lowest tier).</summary>
        public int OffSlotTierDrop { get; set; } = 1;

        /// <summary>The first mechanic line must serve the item's affinity and come from its own slot (D11.8 coherence).</summary>
        public bool FirstMechanicLineOnClass { get; set; } = true;

        public static Dictionary<string, double> DefaultOffSlotChance() => new(StringComparer.OrdinalIgnoreCase)
        {
            ["Common"] = 0, ["Uncommon"] = 0, ["Rare"] = 0.01, ["Epic"] = 0.02, ["Legendary"] = 0.04, ["Mythic"] = 0.08
        };

        public double OffSlotChanceFor(string? rarity) =>
            OffSlotChance != null && rarity != null && OffSlotChance.TryGetValue(rarity.Trim(), out double c) ? Math.Clamp(c, 0, 1) : 0;

        public void EnsureSensibleDefaults()
        {
            SlotWeights ??= new Dictionary<string, double>();
            if (PoolWeights == null || PoolWeights.Count == 0)
                PoolWeights = DefaultPoolWeights();
            else
                PoolWeights = new Dictionary<string, double>(PoolWeights, StringComparer.OrdinalIgnoreCase);
            GuaranteedActionLines = Math.Max(0, GuaranteedActionLines);
            if (SuffixKinds == null || SuffixKinds.Count == 0)
                SuffixKinds = DefaultSuffixKinds();
            PoeticSuffixChance = Math.Clamp(PoeticSuffixChance, 0, 1);
            ItemLevelRollUp ??= new RollUpRule(0.10, 0.5, 5);
            RarityRollUp ??= new RollUpRule(0.02, 1.0, 5);
            WeaponSpeedPerTier = Math.Clamp(WeaponSpeedPerTier, 0, 0.2);
            TierRollUp ??= new RollUpRule(0.08, 0.5, 2);
            HeroClassShare = Math.Clamp(HeroClassShare, 0, 1);
            HybridAffinityShare = Math.Clamp(HybridAffinityShare, 0, 1);
            OffSlotChance = OffSlotChance == null || OffSlotChance.Count == 0
                ? DefaultOffSlotChance()
                : new Dictionary<string, double>(OffSlotChance, StringComparer.OrdinalIgnoreCase);
            OffSlotTierDrop = Math.Max(0, OffSlotTierDrop);
            if (InfluenceCap < 1)
                InfluenceCap = 1;
        }
    }
}
