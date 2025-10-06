using System;
using System.Collections.Generic;

namespace RPGGame
{
    /// <summary>
    /// Character-related configuration settings
    /// </summary>
    public class CharacterConfig
    {
        public int PlayerBaseHealth { get; set; }
        public int HealthPerLevel { get; set; }
        public int EnemyHealthPerLevel { get; set; }
    }

    /// <summary>
    /// Character attributes configuration
    /// </summary>
    public class AttributesConfig
    {
        public AttributeSet PlayerBaseAttributes { get; set; } = new();
        public int PlayerAttributesPerLevel { get; set; }
        public int EnemyAttributesPerLevel { get; set; }
        public int EnemyPrimaryAttributeBonus { get; set; }
        public int IntelligenceRollBonusPer { get; set; }
    }

    /// <summary>
    /// Base attribute set for characters
    /// </summary>
    public class AttributeSet
    {
        public int Strength { get; set; }
        public int Agility { get; set; }
        public int Technique { get; set; }
        public int Intelligence { get; set; }
    }

    /// <summary>
    /// Character progression and experience configuration
    /// </summary>
    public class ProgressionConfig
    {
        public int BaseXPToLevel2 { get; set; }
        public double XPScalingFactor { get; set; }
        public int EnemyXPBase { get; set; }
        public int EnemyXPPerLevel { get; set; }
        public int EnemyGoldBase { get; set; }
        public int EnemyGoldPerLevel { get; set; }
    }

    /// <summary>
    /// Experience system configuration
    /// </summary>
    public class ExperienceSystemConfig
    {
        public string BaseXPFormula { get; set; } = "";
        public int LevelCap { get; set; }
        public int StatPointsPerLevel { get; set; }
        public int SkillPointsPerLevel { get; set; }
        public int AttributeCap { get; set; }
        public string Description { get; set; } = "";
    }

    /// <summary>
    /// XP rewards configuration
    /// </summary>
    public class XPRewardsConfig
    {
        public string BaseXPFormula { get; set; } = "";
        public Dictionary<string, LevelDifficultyMultiplier> LevelDifferenceMultipliers { get; set; } = new();
        public DungeonCompletionBonusConfig DungeonCompletionBonus { get; set; } = new();
        public string GroupXPFormula { get; set; } = "";
        public int MinimumXP { get; set; }
        public double MaximumXPMultiplier { get; set; }
    }

    /// <summary>
    /// Level difficulty multiplier configuration
    /// </summary>
    public class LevelDifficultyMultiplier
    {
        public int LevelDifference { get; set; }
        public double Multiplier { get; set; }
        public string Description { get; set; } = "";
    }

    /// <summary>
    /// Dungeon completion bonus configuration
    /// </summary>
    public class DungeonCompletionBonusConfig
    {
        public int BaseBonus { get; set; }
        public int BonusPerRoom { get; set; }
        public double LevelDifferenceMultiplier { get; set; }
    }

    /// <summary>
    /// Class balance configuration
    /// </summary>
    public class ClassBalanceConfig
    {
        public ClassMultipliers Barbarian { get; set; } = new();
        public ClassMultipliers Warrior { get; set; } = new();
        public ClassMultipliers Rogue { get; set; } = new();
        public ClassMultipliers Wizard { get; set; } = new();
        public string Description { get; set; } = "";
    }

    /// <summary>
    /// Class multipliers configuration
    /// </summary>
    public class ClassMultipliers
    {
        public double HealthMultiplier { get; set; }
        public double DamageMultiplier { get; set; }
        public double SpeedMultiplier { get; set; }
    }
}
