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

        /// <summary>At MF 100, optional affix *extra* chances (prefix/stat/action) multiply by this value before capping at 1.0 per step.</summary>
        public double AffixMagicFindMaxExtraChanceBoost { get; set; } = 2.0;

        /// <summary>Default maximum number of actions in the player combo sequence before feet bonuses.</summary>
        public int ComboSequenceBaseMax { get; set; } = 2;

        /// <summary>Hard cap on combo sequence length after equipment <see cref="Item.ExtraActionSlots"/> and <c>ExtraActionSlots</c> affixes.</summary>
        public int ComboSequenceAbsoluteMax { get; set; } = 8;

        /// <summary>INT threshold for combo sequence unlocks (see <see cref="GameConstants.ComboSequenceIntelligenceThreshold"/>).</summary>
        public int ComboSequenceIntelligenceThreshold { get; set; } = 10;

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
