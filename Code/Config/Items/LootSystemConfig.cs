using System;
namespace RPGGame
{
    /// <summary>
    /// Loot system configuration
    /// </summary>
    public class LootSystemConfig
    {
        public double BaseDropChance { get; set; }
        public double DropChancePerLevel { get; set; }
        public double MaxDropChance { get; set; }
        public double GuaranteedLootChance { get; set; }
        public double MagicFindEffectiveness { get; set; }
        public double GoldDropMultiplier { get; set; }
        public double ItemValueMultiplier { get; set; }

        /// <summary>At MF 100, affix-line tier weights for prefix/suffix pool rolls scale up to this multiplier on the highest tier index present in the pool (Common stays 1×).</summary>
        public double AffixMagicFindMaxWeightBoost { get; set; } = 2.0;

        /// <summary>At MF 100, optional affix *extra* chances (prefix/stat/action/extra combo slots) multiply by this value before capping at 1.0 per step.</summary>
        public double AffixMagicFindMaxExtraChanceBoost { get; set; } = 2.0;

        /// <summary>Starting combo sequence length before gear <see cref="Item.ExtraActionSlots"/>, named class-tier upgrades, and Wand intrinsic bonus.</summary>
        public int ComboSequenceBaseMax { get; set; } = 2;

        /// <summary>Hard cap on combo sequence length after gear ExtraActionSlots (catalog + affix + StatBonuses), class-tier upgrades, and Wand intrinsic bonus (<see cref="ComboSequenceMaxHelper"/>).</summary>
        public int ComboSequenceAbsoluteMax { get; set; } = 8;

        /// <summary>INT threshold for combo sequence unlocks (see <see cref="GameConstants.ComboSequenceIntelligenceThreshold"/>).</summary>
        public int ComboSequenceIntelligenceThreshold { get; set; } = 10;

        /// <summary>
        /// Legacy (true): affix lines roll their own tiers, at most one suffix per tier (repeats bump up), and the item is
        /// raised to its highest affix rank — so affix count effectively decides rarity.
        /// False: the rarity table decides item rarity; affix lines roll at or below it (see <see cref="AffixTierFalloff"/>);
        /// the first stat suffix carries the item's rarity; affixes never raise item rarity.
        /// </summary>
        public bool RarityFromAffixes { get; set; } = true;

        /// <summary>
        /// When <see cref="RarityFromAffixes"/> is false: each tier below the item's rarity is this many times more likely
        /// for non-signature affix lines (3 = a Rare item's extra lines are Common 9 : Uncommon 3 : Rare 1).
        /// </summary>
        public double AffixTierFalloff { get; set; } = 3.0;

        /// <summary>Chance (0–1) that a weapon drop is the hero's equipped weapon type (0 = uniform across types, legacy).</summary>
        public double EquippedWeaponTypeBias { get; set; }

        /// <summary>
        /// Each material piece rolls one item trigger from its material's pool (<see cref="RPGGame.Data.MaterialTriggerCatalog"/>),
        /// so set pieces carry the procs their set feeds on (e.g. Silver Prism: connect → Focus, which mints LENS).
        /// false = legacy: material sets own all procs and pieces carry none.
        /// </summary>
        public bool MaterialPieceTriggers { get; set; }

        /// <summary>
        /// Level cap on combo strip slots as [level, slots] anchors (e.g. [[1,1],[10,3]]): the highest anchor at or below the
        /// hero's level caps base + class slots. Gear and Action Lab sandbox slots are added on top. Empty = no level cap (legacy). Action Lab sandbox slots bypass it.
        /// </summary>
        public System.Collections.Generic.List<int[]> ComboSlotCapByLevel { get; set; } = new();

        /// <summary>Slot cap for <paramref name="level"/> from <see cref="ComboSlotCapByLevel"/>; 0 = uncapped.</summary>
        public int GetComboSlotCapForLevel(int level)
        {
            int cap = 0, bestLevel = int.MinValue;
            if (ComboSlotCapByLevel == null)
                return 0;
            foreach (var a in ComboSlotCapByLevel)
            {
                if (a == null || a.Length < 2 || a[0] > level || a[0] < bestLevel)
                    continue;
                bestLevel = a[0];
                cap = a[1];
            }
            return Math.Max(0, cap);
        }

        /// <summary>Chance (0–1) a weapon's gear action comes from its class's actions; otherwise a neutral action.</summary>
        public double WeaponGearActionClassShare { get; set; } = 0.7;

        /// <summary>Chance (0–1) an armor piece's gear action comes from the hero's class (equipped weapon); otherwise neutral.</summary>
        public double ArmorGearActionClassShare { get; set; }

        public RarityUpgradeConfig RarityUpgrade { get; set; } = new();
        public string Description { get; set; } = "";

        public void EnsureSensibleLootDefaults()
        {
            if (BaseDropChance <= 0)
                BaseDropChance = 0.15;
            if (DropChancePerLevel < 0)
                DropChancePerLevel = 0;
            if (MaxDropChance <= 0)
                MaxDropChance = 0.95;
            if (GuaranteedLootChance <= 0)
                GuaranteedLootChance = 1.0;
            if (MagicFindEffectiveness < 0)
                MagicFindEffectiveness = 0;
            if (GoldDropMultiplier <= 0)
                GoldDropMultiplier = 1.0;
            if (ItemValueMultiplier <= 0)
                ItemValueMultiplier = 1.0;
            if (AffixMagicFindMaxWeightBoost < 1.0)
                AffixMagicFindMaxWeightBoost = 1.0;
            if (AffixMagicFindMaxExtraChanceBoost < 1.0)
                AffixMagicFindMaxExtraChanceBoost = 1.0;
            if (ComboSequenceBaseMax < 1)
                ComboSequenceBaseMax = 2;
            if (ComboSequenceAbsoluteMax < ComboSequenceBaseMax)
                ComboSequenceAbsoluteMax = ComboSequenceBaseMax;
            if (ComboSequenceIntelligenceThreshold <= 0)
                ComboSequenceIntelligenceThreshold = 10;
        }
    }

    /// <summary>
    /// Rarity upgrade configuration - cascading rarity progression
    /// </summary>
    public class RarityUpgradeConfig
    {
        public bool Enabled { get; set; } = false;
        public double BaseUpgradeChance { get; set; } = 0.05;
        public double UpgradeChanceDecayPerTier { get; set; } = 0.5;
        public int MaxUpgradeTiers { get; set; } = 6;
        public double MagicFindBonus { get; set; } = 0.0001;
        public string Description { get; set; } = "Cascading rarity upgrade system - items can upgrade to next tier with exponentially decreasing probability";
    }

    /// <summary>
    /// Modification rarity configuration
    /// </summary>
    public class ModificationRarityConfig
    {
        public double Common { get; set; } = 35.0;
        public double Uncommon { get; set; } = 25.0;
        public double Rare { get; set; } = 20.0;
        public double Epic { get; set; } = 12.0;
        public double Legendary { get; set; } = 6.0;
        public double Mythic { get; set; } = 1.8;
        public double TierBonusPerLevel { get; set; } = 1.5;
        public double BonusPointEffectiveness { get; set; } = 1.0;
        public string Description { get; set; } = "Modification rarity distribution percentages. Total should equal 100.0";
    }
}
