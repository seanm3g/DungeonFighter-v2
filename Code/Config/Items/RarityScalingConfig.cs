namespace RPGGame
{
    /// <summary>
    /// Magic find scaling configuration
    /// </summary>
    public class MagicFindScalingConfig
    {
        public RarityMagicFindConfig Common { get; set; } = new();
        public RarityMagicFindConfig Uncommon { get; set; } = new();
        public RarityMagicFindConfig Rare { get; set; } = new();
        public RarityMagicFindConfig Epic { get; set; } = new();
        public RarityMagicFindConfig Legendary { get; set; } = new();
        public RarityMagicFindConfig Mythic { get; set; } = new();
    }

    /// <summary>
    /// Rarity magic find configuration
    /// </summary>
    public class RarityMagicFindConfig
    {
        public double PerPointMultiplier { get; set; }
        public string Description { get; set; } = "";
    }

    /// <summary>
    /// Level-based rarity scaling configuration
    /// </summary>
    public class LevelBasedRarityScalingConfig
    {
        public CommonRarityScalingConfig Common { get; set; } = new();
        public UncommonRarityScalingConfig Uncommon { get; set; } = new();
        public RareRarityScalingConfig Rare { get; set; } = new();
        public EpicRarityScalingConfig Epic { get; set; } = new();
        public LegendaryRarityScalingConfig Legendary { get; set; } = new();
    }

    /// <summary>
    /// Common rarity scaling configuration
    /// </summary>
    public class CommonRarityScalingConfig
    {
        public double BaseMultiplier { get; set; }
        public double LevelReduction { get; set; }
        public string Description { get; set; } = "";
    }

    /// <summary>
    /// Uncommon rarity scaling configuration
    /// </summary>
    public class UncommonRarityScalingConfig
    {
        public double BaseMultiplier { get; set; }
        public double LevelBonus { get; set; }
        public string Description { get; set; } = "";
    }

    /// <summary>
    /// Rare rarity scaling configuration
    /// </summary>
    public class RareRarityScalingConfig
    {
        public double BaseMultiplier { get; set; }
        public double LevelBonus { get; set; }
        public string Description { get; set; } = "";
    }

    /// <summary>
    /// Epic rarity scaling configuration
    /// </summary>
    public class EpicRarityScalingConfig
    {
        public int MinLevel { get; set; }
        public double EarlyMultiplier { get; set; }
        public double BaseMultiplier { get; set; }
        public double LevelBonus { get; set; }
        public string Description { get; set; } = "";
    }

    /// <summary>
    /// Legendary rarity scaling configuration
    /// </summary>
    public class LegendaryRarityScalingConfig
    {
        public int MinLevel { get; set; }
        public int EarlyThreshold { get; set; }
        public double EarlyMultiplier { get; set; }
        public double MidMultiplier { get; set; }
        public double BaseMultiplier { get; set; }
        public double LevelBonus { get; set; }
        public string Description { get; set; } = "";
    }
}
