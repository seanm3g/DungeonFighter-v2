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

        /// <summary>
        /// When tuning JSON has non-positive values, <see cref="Character"/> uses these same fallbacks at runtime.
        /// Applying them after load keeps <see cref="GameConfiguration"/>, tools, and the variable editor aligned with gameplay.
        /// </summary>
        public void EnsureValidPlayerHealthDefaults()
        {
            if (PlayerBaseHealth <= 0)
                PlayerBaseHealth = 60;
            if (HealthPerLevel <= 0)
                HealthPerLevel = 3;
        }
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

        /// <summary>
        /// When <see cref="IntelligenceRollBonusPer"/> is omitted or zero in tuning JSON, roll bonus from INT is disabled.
        /// Restores the standard scale (+1 accuracy per N INT, same as <see cref="Enemy.GetIntelligenceRollBonus"/>).
        /// </summary>
        public void EnsureValidIntelligenceRollBonusDefaults()
        {
            if (IntelligenceRollBonusPer <= 0)
                IntelligenceRollBonusPer = 10;
        }

        /// <summary>
        /// When all four base stats are zero or points-per-level is invalid, <see cref="CharacterStats"/> uses these fallbacks.
        /// Keeps loaded tuning consistent with new character creation.
        /// </summary>
        public void EnsureValidPlayerBaseStatDefaults()
        {
            PlayerBaseAttributes ??= new AttributeSet();
            var a = PlayerBaseAttributes;
            if (a.Strength == 0 && a.Agility == 0 && a.Technique == 0 && a.Intelligence == 0)
            {
                a.Strength = 3;
                a.Agility = 3;
                a.Technique = 3;
                a.Intelligence = 3;
            }
            if (PlayerAttributesPerLevel <= 0)
                PlayerAttributesPerLevel = 2;
        }
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
        /// <summary>When &gt; 0, XP to go from level 1 to 2 is set to this value (whole curve scales vs one tier-1 dungeon completion). When 0, the L1→2 bar equals one tier-1 dungeon completion bonus.</summary>
        public int BaseXPToLevel2 { get; set; }
        /// <summary>Global multiplier on every level’s XP bar. When ≤0, treated as 1.0. (Bar shape is dungeon-paced: 1, 1.5, 2, 3, … tier-1 completions per level — see <see cref="CharacterProgression.GetXpRequiredToAdvanceFromLevel"/>.)</summary>
        public double XPScalingFactor { get; set; }
        public int EnemyXPBase { get; set; }
        public int EnemyXPPerLevel { get; set; }
        public int EnemyGoldBase { get; set; }
        public int EnemyGoldPerLevel { get; set; }

        /// <summary>
        /// Same numeric fallbacks as Enemy reward construction and XPRewardSystem when JSON stores non-positive values.
        /// </summary>
        public void EnsureValidEnemyXpAndGoldDefaults()
        {
            if (EnemyXPBase <= 0)
                EnemyXPBase = 25;
            if (EnemyXPPerLevel <= 0)
                EnemyXPPerLevel = 5;
            if (EnemyGoldBase <= 0)
                EnemyGoldBase = 10;
            if (EnemyGoldPerLevel <= 0)
                EnemyGoldPerLevel = 3;
        }
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

        /// <summary>
        /// Non-positive multipliers are skipped at level-up health scaling; 1.0 matches the intended neutral multiplier so the variable editor reflects behavior.
        /// </summary>
        public void EnsureNonDegenerateClassMultipliers()
        {
            Barbarian.EnsurePositiveMultipliersOrOne();
            Warrior.EnsurePositiveMultipliersOrOne();
            Rogue.EnsurePositiveMultipliersOrOne();
            Wizard.EnsurePositiveMultipliersOrOne();
        }
    }

    /// <summary>
    /// Class multipliers configuration
    /// </summary>
    public class ClassMultipliers
    {
        public double HealthMultiplier { get; set; }
        public double DamageMultiplier { get; set; }
        public double SpeedMultiplier { get; set; }

        public void EnsurePositiveMultipliersOrOne()
        {
            if (HealthMultiplier <= 0)
                HealthMultiplier = 1.0;
            if (DamageMultiplier <= 0)
                DamageMultiplier = 1.0;
            if (SpeedMultiplier <= 0)
                SpeedMultiplier = 1.0;
        }
    }
}
