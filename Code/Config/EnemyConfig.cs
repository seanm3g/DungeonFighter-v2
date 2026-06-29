using System;
using System.Collections.Generic;
using System.Linq;

namespace RPGGame
{

    /// <summary>
    /// Pool configuration for enemy attributes
    /// </summary>
    public class PoolConfig
    {
        public int BasePointsAtLevel1 { get; set; }
        public int PointsPerLevel { get; set; }
    }

    /// <summary>
    /// Archetype configuration
    /// </summary>
    public class ArchetypeConfig
    {
        public double AttributePoolRatio { get; set; }
        public double SUSTAINPoolRatio { get; set; }
        public double StrengthRatio { get; set; }
        public double AgilityRatio { get; set; }
        public double TechniqueRatio { get; set; }
        public double IntelligenceRatio { get; set; }
        public double SUSTAINHealthRatio { get; set; }
        public double SUSTAINArmorRatio { get; set; }
        public Level1Modifiers? Level1Modifiers { get; set; }
        public ArchetypeBonuses? ArchetypeBonuses { get; set; }
    }

    /// <summary>
    /// Archetype bonuses configuration
    /// </summary>
    public class ArchetypeBonuses
    {
        public double StrengthMultiplier { get; set; } = 1.0;
        public double AgilityMultiplier { get; set; } = 1.0;
        public double HealthMultiplier { get; set; } = 1.0;
        public double ArmorMultiplier { get; set; } = 1.0;
        public double AttackSpeedMultiplier { get; set; } = 1.0;
    }

    /// <summary>
    /// Base enemy configuration
    /// </summary>
    public class BaseEnemyConfig
    {
        public int BaseLevel { get; set; }
        public double HealthRatio { get; set; } = 1.0;
        public BaseEnemyStats BaseStats { get; set; } = new();
        public int BaseArmor { get; set; }
        public string PrimaryAttribute { get; set; } = "Strength";
        public bool IsLiving { get; set; } = true;
        public List<string> Actions { get; set; } = new();
    }

    /// <summary>
    /// Base enemy stats configuration
    /// </summary>
    public class BaseEnemyStats
    {
        public int Strength { get; set; }
        public int Agility { get; set; }
        public int Technique { get; set; }
        public int Intelligence { get; set; }
    }

    /// <summary>
    /// Level 1 modifiers configuration
    /// </summary>
    public class Level1Modifiers
    {
        public int HealthBonus { get; set; }
        public int StrengthBonus { get; set; }
        public int AgilityBonus { get; set; }
        public int TechniqueBonus { get; set; }
        public int IntelligenceBonus { get; set; }
        public int ArmorBonus { get; set; }
    }

    /// <summary>
    /// Stat conversion rates configuration
    /// </summary>
    public class StatConversionRatesConfig
    {
        public double StrengthPerPoint { get; set; }
        public double AgilityPerPoint { get; set; }
        public double TechniquePerPoint { get; set; }
        public double IntelligencePerPoint { get; set; }
        public double HealthPerPoint { get; set; }
        public double ArmorPerPoint { get; set; }
    }

    /// <summary>
    /// DPS balance validation configuration
    /// </summary>
    public class DPSBalanceValidationConfig
    {
        public double TolerancePercentage { get; set; }
        public double MinimumDPS { get; set; }
        public double MaximumDPSMultiplier { get; set; }
    }

    /// <summary>
    /// Sustain balance configuration
    /// </summary>
    public class SustainBalanceConfig
    {
        public string Description { get; set; } = "";
        public TargetActionsToKillConfig TargetActionsToKill { get; set; } = new();
        public DPSToSustainRatioConfig DPSToSustainRatio { get; set; } = new();
        public SpeedToDamageRatioConfig SpeedToDamageRatio { get; set; } = new();
        public HealthToArmorRatioConfig HealthToArmorRatio { get; set; } = new();
        public AttributeGainRatioConfig AttributeGainRatio { get; set; } = new();
        public SustainScalingConfig SustainScaling { get; set; } = new();
        public SustainBalanceValidationConfig BalanceValidation { get; set; } = new();
    }

    /// <summary>
    /// Target actions to kill configuration
    /// </summary>
    public class TargetActionsToKillConfig
    {
        public int Level1 { get; set; }
        public int Level10 { get; set; }
        public int Level20 { get; set; }
        public int Level30 { get; set; }
        public string Formula { get; set; } = "";
    }

    /// <summary>
    /// DPS to sustain ratio configuration
    /// </summary>
    public class DPSToSustainRatioConfig
    {
        public double BaseRatio { get; set; }
        public string Description { get; set; } = "";
        public Dictionary<string, double> ArchetypeModifiers { get; set; } = new();
    }

    /// <summary>
    /// Speed to damage ratio configuration
    /// </summary>
    public class SpeedToDamageRatioConfig
    {
        public double BaseRatio { get; set; }
        public string Description { get; set; } = "";
        public Dictionary<string, double> ArchetypeModifiers { get; set; } = new();
    }

    /// <summary>
    /// Health to armor ratio configuration
    /// </summary>
    public class HealthToArmorRatioConfig
    {
        public double BaseRatio { get; set; }
        public string Description { get; set; } = "";
        public Dictionary<string, double> ArchetypeModifiers { get; set; } = new();
    }

    /// <summary>
    /// Attribute gain ratio configuration
    /// </summary>
    public class AttributeGainRatioConfig
    {
        public double BaseRatio { get; set; }
        public string Description { get; set; } = "";
        public double PrimaryAttributeBonus { get; set; }
        public double SecondaryAttributeBonus { get; set; }
        public Dictionary<string, double> ArchetypeModifiers { get; set; } = new();
    }

    /// <summary>
    /// Sustain scaling configuration
    /// </summary>
    public class SustainScalingConfig
    {
        public double HealthPerLevel { get; set; }
        public double ArmorPerLevel { get; set; }
        public double RegenerationPerLevel { get; set; }
        public string Description { get; set; } = "";
    }

    /// <summary>
    /// Sustain balance validation configuration
    /// </summary>
    public class SustainBalanceValidationConfig
    {
        public int MinActionsToKill { get; set; }
        public int MaxActionsToKill { get; set; }
        public double DPSRatioTolerance { get; set; }
        public double SpeedRatioTolerance { get; set; }
        public double HealthArmorRatioTolerance { get; set; }
        public double AttributeRatioTolerance { get; set; }
    }

    /// <summary>
    /// Base stats configuration
    /// </summary>
    public class BaseStatsConfig
    {
        public int Health { get; set; }
        public int Strength { get; set; }
        public int Agility { get; set; }
        public int Technique { get; set; }
        public int Intelligence { get; set; }
        public int Armor { get; set; }
    }

    /// <summary>
    /// Scaling per level configuration
    /// </summary>
    public class ScalingPerLevelConfig
    {
        public int Health { get; set; }
        public double Attributes { get; set; }
        public double Armor { get; set; }
    }

    /// <summary>
    /// Enemy archetype configuration
    /// </summary>
    public class EnemyArchetypeConfig
    {
        public StatMultipliersConfig StatMultipliers { get; set; } = new();
        public string Description { get; set; } = "";
    }

    /// <summary>
    /// Stat multipliers configuration
    /// </summary>
    public class StatMultipliersConfig
    {
        public double Health { get; set; } = 1.0;
        public double Strength { get; set; } = 1.0;
        public double Agility { get; set; } = 1.0;
        public double Technique { get; set; } = 1.0;
        public double Intelligence { get; set; } = 1.0;
        public double Armor { get; set; } = 1.0;
    }


    /// <summary>
    /// Global scales applied to enemy level curves before <see cref="GlobalMultipliersConfig"/> (per-stat) multipliers.
    /// Persisted under <c>TuningConfig.json</c> → <c>enemySystem.progressionScales</c>.
    /// </summary>
    public class EnemyProgressionScalesConfig
    {
        /// <summary>Multiplies level-1 (and explicit) base health contribution.</summary>
        public double BaseHealthScale { get; set; } = 1.0;
        /// <summary>Multiplies HP gained per level (explicit <c>healthGrowthPerLevel</c> or tuning default).</summary>
        public double HealthGrowthScale { get; set; } = 1.0;
        /// <summary>Multiplies STR/AGI/TEC/INT per-level growth after the 6-point budget normalization.</summary>
        public double AttributeGrowthScale { get; set; } = 1.0;

        /// <summary>Uniform multiplier on enemy max HP (fight tempo / duration).</summary>
        public double CombatTempoScale { get; set; } = 1.0;

        /// <summary>0 = early/base-dominated HP curve; 1 = late/growth-dominated.</summary>
        public double ProgressionShape { get; set; } = 0.5;

        /// <summary>+1 = hero relatively tougher; −1 = hero relatively weaker (player max HP).</summary>
        public double PlayerEnemyParity { get; set; }

        /// <summary>Level where growth weight reaches full strength (design pivot).</summary>
        public int ProgressionPivotLevel { get; set; } = 30;

        public void EnsurePositiveScales()
        {
            if (BaseHealthScale <= 0)
                BaseHealthScale = 1.0;
            if (HealthGrowthScale <= 0)
                HealthGrowthScale = 1.0;
            if (AttributeGrowthScale <= 0)
                AttributeGrowthScale = 1.0;
            if (CombatTempoScale <= 0)
                CombatTempoScale = 1.0;
            ProgressionShape = Math.Clamp(ProgressionShape, 0, 1);
            PlayerEnemyParity = Math.Clamp(PlayerEnemyParity, -1, 1);
            if (ProgressionPivotLevel < 10)
                ProgressionPivotLevel = 10;
            if (ProgressionPivotLevel > 100)
                ProgressionPivotLevel = 100;
        }
    }

    /// <summary>Percent chance for each spawn tier (should sum to 100).</summary>
    public class EnemySpawnTierWeightsConfig
    {
        public int CommonPercent { get; set; } = 50;
        public int UncommonBiomePercent { get; set; } = 10;
        public int UncommonRegionPercent { get; set; } = 15;
        public int UncommonLocationPercent { get; set; } = 15;
        public int RareLocationPercent { get; set; } = 5;
        public int AnywherePercent { get; set; } = 5;

        public int TotalPercent =>
            CommonPercent + UncommonBiomePercent + UncommonRegionPercent +
            UncommonLocationPercent + RareLocationPercent + AnywherePercent;

        public static EnemySpawnTierWeightsConfig CreateDefaults() => new();

        public void EnsureSanitized()
        {
            CommonPercent = Math.Clamp(CommonPercent, 0, 100);
            UncommonBiomePercent = Math.Clamp(UncommonBiomePercent, 0, 100);
            UncommonRegionPercent = Math.Clamp(UncommonRegionPercent, 0, 100);
            UncommonLocationPercent = Math.Clamp(UncommonLocationPercent, 0, 100);
            RareLocationPercent = Math.Clamp(RareLocationPercent, 0, 100);
            AnywherePercent = Math.Clamp(AnywherePercent, 0, 100);

            int sum = TotalPercent;
            if (sum <= 0)
            {
                var defaults = CreateDefaults();
                CommonPercent = defaults.CommonPercent;
                UncommonBiomePercent = defaults.UncommonBiomePercent;
                UncommonRegionPercent = defaults.UncommonRegionPercent;
                UncommonLocationPercent = defaults.UncommonLocationPercent;
                RareLocationPercent = defaults.RareLocationPercent;
                AnywherePercent = defaults.AnywherePercent;
                return;
            }

            if (sum == 100)
                return;

            NormalizeProportionally();
        }

        private void NormalizeProportionally()
        {
            int[] raw =
            {
                CommonPercent,
                UncommonBiomePercent,
                UncommonRegionPercent,
                UncommonLocationPercent,
                RareLocationPercent,
                AnywherePercent
            };

            int sum = raw.Sum();
            if (sum <= 0)
                return;

            int[] normalized = new int[6];
            int allocated = 0;
            for (int i = 0; i < raw.Length - 1; i++)
            {
                normalized[i] = (int)Math.Round(raw[i] * 100.0 / sum);
                allocated += normalized[i];
            }

            normalized[^1] = Math.Max(0, 100 - allocated);

            CommonPercent = normalized[0];
            UncommonBiomePercent = normalized[1];
            UncommonRegionPercent = normalized[2];
            UncommonLocationPercent = normalized[3];
            RareLocationPercent = normalized[4];
            AnywherePercent = normalized[5];
        }
    }

    /// <summary>Spawn tier weights keyed by settlement density (Rural / Town / City).</summary>
    public class EnemySpawnTierWeightsBySettlementConfig
    {
        public EnemySpawnTierWeightsConfig Rural { get; set; } = EnemySpawnTierWeightsConfig.CreateDefaults();
        public EnemySpawnTierWeightsConfig Town { get; set; } = EnemySpawnTierWeightsConfig.CreateDefaults();
        public EnemySpawnTierWeightsConfig City { get; set; } = EnemySpawnTierWeightsConfig.CreateDefaults();

        public EnemySpawnTierWeightsConfig Get(SettlementType settlementType) => settlementType switch
        {
            SettlementType.Town => Town ??= EnemySpawnTierWeightsConfig.CreateDefaults(),
            SettlementType.City => City ??= EnemySpawnTierWeightsConfig.CreateDefaults(),
            _ => Rural ??= EnemySpawnTierWeightsConfig.CreateDefaults()
        };

        public void EnsureFromLegacy(EnemySpawnTierWeightsConfig? legacy)
        {
            Rural ??= EnemySpawnTierWeightsConfig.CreateDefaults();
            Town ??= EnemySpawnTierWeightsConfig.CreateDefaults();
            City ??= EnemySpawnTierWeightsConfig.CreateDefaults();

            if (legacy == null || !LegacyDiffersFromDefaults(legacy))
                return;

            if (ProfilesMatchDefaults(Rural) && ProfilesMatchDefaults(Town) && ProfilesMatchDefaults(City))
            {
                Rural = Clone(legacy);
                Town = Clone(legacy);
                City = Clone(legacy);
            }
        }

        public void EnsureSanitized()
        {
            Rural ??= EnemySpawnTierWeightsConfig.CreateDefaults();
            Town ??= EnemySpawnTierWeightsConfig.CreateDefaults();
            City ??= EnemySpawnTierWeightsConfig.CreateDefaults();
            Rural.EnsureSanitized();
            Town.EnsureSanitized();
            City.EnsureSanitized();
        }

        private static bool LegacyDiffersFromDefaults(EnemySpawnTierWeightsConfig legacy)
        {
            var defaults = EnemySpawnTierWeightsConfig.CreateDefaults();
            return legacy.CommonPercent != defaults.CommonPercent
                   || legacy.UncommonBiomePercent != defaults.UncommonBiomePercent
                   || legacy.UncommonRegionPercent != defaults.UncommonRegionPercent
                   || legacy.UncommonLocationPercent != defaults.UncommonLocationPercent
                   || legacy.RareLocationPercent != defaults.RareLocationPercent
                   || legacy.AnywherePercent != defaults.AnywherePercent;
        }

        private static bool ProfilesMatchDefaults(EnemySpawnTierWeightsConfig profile) =>
            !LegacyDiffersFromDefaults(profile);

        private static EnemySpawnTierWeightsConfig Clone(EnemySpawnTierWeightsConfig source) => new()
        {
            CommonPercent = source.CommonPercent,
            UncommonBiomePercent = source.UncommonBiomePercent,
            UncommonRegionPercent = source.UncommonRegionPercent,
            UncommonLocationPercent = source.UncommonLocationPercent,
            RareLocationPercent = source.RareLocationPercent,
            AnywherePercent = source.AnywherePercent
        };
    }

    /// <summary>
    /// Unified enemy system configuration that consolidates all enemy-related tuning
    /// </summary>
    public class EnemySystemConfig
    {
        public GlobalMultipliersConfig GlobalMultipliers { get; set; } = new();
        public EnemyProgressionScalesConfig ProgressionScales { get; set; } = new();
        public BaselineStatsConfig BaselineStats { get; set; } = new();
        public ScalingPerLevelConfig ScalingPerLevel { get; set; } = new();
        public Dictionary<string, ArchetypeMultipliersConfig> Archetypes { get; set; } = new();
        public double AttributeGrowthBudgetPerLevel { get; set; } = 6.0;
        public int LevelVariance { get; set; } = 1;
        public string Description { get; set; } = "";

        /// <summary>Gold/XP multipliers by enemy rarity name (e.g. Rare → 1.5).</summary>
        public Dictionary<string, double> RarityRewardMultipliers { get; set; } = new(StringComparer.OrdinalIgnoreCase);

        /// <summary>Extra magic-find fraction when rolling loot from a defeated enemy of that rarity.</summary>
        public Dictionary<string, double> RarityLootMagicFindBonus { get; set; } = new(StringComparer.OrdinalIgnoreCase);

        /// <summary>Percent weights for tiered enemy spawn rolls (legacy single profile; mirrors Rural on save).</summary>
        public EnemySpawnTierWeightsConfig SpawnTierWeights { get; set; } = new();

        /// <summary>Spawn tier weights per settlement type (Rural / Town / City).</summary>
        public EnemySpawnTierWeightsBySettlementConfig SpawnTierWeightsBySettlement { get; set; } = new();

        /// <summary>Optional location name → Rural/Town/City overrides for spawn tier weight selection.</summary>
        public Dictionary<string, string> LocationSettlementOverrides { get; set; } = new(StringComparer.OrdinalIgnoreCase);

        public EnemySpawnTierWeightsConfig GetSpawnTierWeights(SettlementType settlementType)
        {
            SpawnTierWeightsBySettlement ??= new EnemySpawnTierWeightsBySettlementConfig();
            return SpawnTierWeightsBySettlement.Get(settlementType);
        }

        public void EnsureSanitizedDefaults()
        {
            GlobalMultipliers ??= new GlobalMultipliersConfig();
            GlobalMultipliers.EnsurePositiveMultipliers();
            ProgressionScales ??= new EnemyProgressionScalesConfig();
            ProgressionScales.EnsurePositiveScales();
            SpawnTierWeights ??= new EnemySpawnTierWeightsConfig();
            SpawnTierWeightsBySettlement ??= new EnemySpawnTierWeightsBySettlementConfig();
            SpawnTierWeightsBySettlement.EnsureFromLegacy(SpawnTierWeights);
            SpawnTierWeightsBySettlement.EnsureSanitized();
            SpawnTierWeights = CloneSpawnWeights(SpawnTierWeightsBySettlement.Rural);
            LocationSettlementOverrides ??= new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            BaselineStats ??= new BaselineStatsConfig();
            BaselineStats.EnsureValidNonEmptyBaseline();
            ScalingPerLevel ??= new ScalingPerLevelConfig();
            Archetypes ??= new Dictionary<string, ArchetypeMultipliersConfig>();
            if (LevelVariance <= 0)
                LevelVariance = 1;
            if (AttributeGrowthBudgetPerLevel <= 0)
                AttributeGrowthBudgetPerLevel = 6.0;
            EnsureDefaultArchetypes();
        }

        private void EnsureDefaultArchetypes()
        {
            Archetypes ??= new Dictionary<string, ArchetypeMultipliersConfig>();
            EnsureArchetype("Berserker", 0.8, 1.5, 0.9, 0.9, 0.8, 0.5);
            EnsureArchetype("Guardian", 1.0, 0.8, 0.8, 0.8, 0.8, 1.8);
            EnsureArchetype("Assassin", 0.7, 1.0, 1.6, 1.2, 1.0, 0.4);
            EnsureArchetype("Brute", 1.6, 1.0, 0.7, 0.7, 0.7, 0.8);
            EnsureArchetype("Mage", 0.6, 0.6, 0.8, 0.8, 1.8, 0.3);
            EnsureArchetype("Acrobat", 0.75, 0.9, 1.5, 1.3, 0.9, 0.35);
        }

        private void EnsureArchetype(string name, double health, double str, double agi, double tec, double intel, double armor)
        {
            if (!Archetypes.ContainsKey(name))
            {
                Archetypes[name] = new ArchetypeMultipliersConfig
                {
                    Health = health,
                    Strength = str,
                    Agility = agi,
                    Technique = tec,
                    Intelligence = intel,
                    Armor = armor
                };
            }
        }

        private static EnemySpawnTierWeightsConfig CloneSpawnWeights(EnemySpawnTierWeightsConfig source) => new()
        {
            CommonPercent = source.CommonPercent,
            UncommonBiomePercent = source.UncommonBiomePercent,
            UncommonRegionPercent = source.UncommonRegionPercent,
            UncommonLocationPercent = source.UncommonLocationPercent,
            RareLocationPercent = source.RareLocationPercent,
            AnywherePercent = source.AnywherePercent
        };
    }

    /// <summary>
    /// Global multipliers applied to all enemies
    /// </summary>
    public class GlobalMultipliersConfig
    {
        public double HealthMultiplier { get; set; } = 1.0;
        public double DamageMultiplier { get; set; } = 1.0;
        public double ArmorMultiplier { get; set; } = 1.0;
        public double SpeedMultiplier { get; set; } = 1.0;
        public string Description { get; set; } = "";

        public void EnsurePositiveMultipliers()
        {
            if (HealthMultiplier <= 0)
                HealthMultiplier = 1.0;
            if (DamageMultiplier <= 0)
                DamageMultiplier = 1.0;
            if (ArmorMultiplier <= 0)
                ArmorMultiplier = 1.0;
            if (SpeedMultiplier <= 0)
                SpeedMultiplier = 1.0;
        }
    }

    /// <summary>
    /// Baseline stats that all enemies start with
    /// </summary>
    public class BaselineStatsConfig
    {
        public int Health { get; set; } = 50;
        public int Strength { get; set; } = 3;
        public int Agility { get; set; } = 3;
        public int Technique { get; set; } = 3;
        public int Intelligence { get; set; } = 3;
        public int Armor { get; set; } = 2;
        public string Description { get; set; } = "";

        public void EnsureValidNonEmptyBaseline()
        {
            if (Health <= 0)
                Health = 50;
            if (Strength <= 0)
                Strength = 3;
            if (Agility <= 0)
                Agility = 3;
            if (Technique <= 0)
                Technique = 3;
            if (Intelligence <= 0)
                Intelligence = 3;
            if (Armor < 0)
                Armor = 0;
        }
    }

    /// <summary>
    /// Alias for BaselineStatsConfig (legacy compatibility)
    /// </summary>
    public class EnemyBaselineConfig : BaselineStatsConfig
    {
    }

    /// <summary>
    /// Stat multipliers for each archetype
    /// </summary>
    public class ArchetypeMultipliersConfig
    {
        public double Health { get; set; } = 1.0;
        public double Strength { get; set; } = 1.0;
        public double Agility { get; set; } = 1.0;
        public double Technique { get; set; } = 1.0;
        public double Intelligence { get; set; } = 1.0;
        public double Armor { get; set; } = 1.0;
        public string Description { get; set; } = "";
    }


}
