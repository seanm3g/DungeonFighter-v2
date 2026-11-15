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
            ["weapon"] = 4, ["head"] = 1, ["chest"] = 1, ["legs"] = 1, ["feet"] = 1
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

        /// <summary>Action lines every item gets before the weighted lines (they count toward the rarity's line count).</summary>
        public int GuaranteedActionLines { get; set; } = 1;

        public RollUpRule ItemLevelRollUp { get; set; } = new(0.10, 0.5, 5);
        public RollUpRule RarityRollUp { get; set; } = new(0.05, 0.5, 2);
        public RollUpRule TierRollUp { get; set; } = new(0.08, 0.5, 2);

        public static Dictionary<string, double> DefaultPoolWeights() => new()
        {
            ["stat"] = 50, ["mechanic"] = 20, ["material"] = 15, ["action"] = 15
        };

        /// <summary>Roll-up chances × (1 + magic find / 100 × this).</summary>
        public double MagicFindRollUpScale { get; set; } = 1.0;

        /// <summary>Influence: weight × min(cap, 1 + affinity × matching tags).</summary>
        public double InfluenceAffinity { get; set; } = 0.5;
        public double InfluenceCap { get; set; } = 3.0;

        /// <summary>Chance a weapon's action comes from its own class's actions (else the rest of the gear pool).</summary>
        public double WeaponActionClassShare { get; set; } = 0.7;

        /// <summary>Weapon damage × (1 + this × (item level − 1)).</summary>
        public double DamagePerItemLevel { get; set; } = 0.01;

        /// <summary>Catalog attributes on armor (STR / AGI / TEC / INT) × (1 + this × (item level − 1)).</summary>
        public double AttributePerItemLevel { get; set; } = 0.02;

        /// <summary>Percent magnitudes on a base item's implicit (next-action / this-swing damage, amp, speed)
        /// × (1 + this × (item level − 1)). Implicits that already scale (scaleFrom) or are discrete never scale.</summary>
        public double ImplicitPercentPerItemLevel { get; set; } = 0.01;

        /// <summary>Flat magnitudes on a base item's implicit (heal, armor, weapon damage) × (1 + this × (item level − 1)).</summary>
        public double ImplicitFlatPerItemLevel { get; set; } = 0.02;

        public void EnsureSensibleDefaults()
        {
            SlotWeights ??= new Dictionary<string, double>();
            if (PoolWeights == null || PoolWeights.Count == 0)
                PoolWeights = DefaultPoolWeights();
            else
                PoolWeights = new Dictionary<string, double>(PoolWeights, StringComparer.OrdinalIgnoreCase);
            GuaranteedActionLines = Math.Max(0, GuaranteedActionLines);
            ItemLevelRollUp ??= new RollUpRule(0.10, 0.5, 5);
            RarityRollUp ??= new RollUpRule(0.05, 0.5, 2);
            TierRollUp ??= new RollUpRule(0.08, 0.5, 2);
            HeroClassShare = Math.Clamp(HeroClassShare, 0, 1);
            if (InfluenceCap < 1)
                InfluenceCap = 1;
        }
    }
}
