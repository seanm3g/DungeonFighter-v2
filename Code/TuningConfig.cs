using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;

namespace RPGGame
{
    public class TuningConfig
    {
        private static TuningConfig? _instance;
        private static readonly object _lock = new object();
        
        public CharacterConfig Character { get; set; } = new();
        public AttributesConfig Attributes { get; set; } = new();
        public CombatConfig Combat { get; set; } = new();
        public PoisonConfig Poison { get; set; } = new();
        public ProgressionConfig Progression { get; set; } = new();
        public XPRewardsConfig XPRewards { get; set; } = new();
        public RollSystemConfig RollSystem { get; set; } = new();
        public ComboSystemConfig ComboSystem { get; set; } = new();
        public EnemyScalingConfig EnemyScaling { get; set; } = new();
        public UIConfig UI { get; set; } = new();
        public GameSpeedConfig GameSpeed { get; set; } = new();
        public ItemScalingConfig? ItemScaling { get; set; } = new();
        public WeaponScalingConfig? WeaponScaling { get; set; } = new();
        public RarityScalingConfig? RarityScaling { get; set; } = new();
        public ProgressionCurvesConfig? ProgressionCurves { get; set; } = new();
        public EnemyDPSConfig? EnemyDPS { get; set; } = new();
        public GameDataConfig GameData { get; set; } = new();
        public DebugConfig Debug { get; set; } = new();
        public CombatBalanceConfig CombatBalance { get; set; } = new();
        public ExperienceSystemConfig ExperienceSystem { get; set; } = new();
        public LootSystemConfig LootSystem { get; set; } = new();
        public DungeonScalingConfig DungeonScaling { get; set; } = new();
        public StatusEffectsConfig StatusEffects { get; set; } = new();
        public EquipmentScalingConfig EquipmentScaling { get; set; } = new();
        public ClassBalanceConfig ClassBalance { get; set; } = new();
        public DifficultySettingsConfig DifficultySettings { get; set; } = new();
        public UICustomizationConfig UICustomization { get; set; } = new();
        public DungeonGenerationConfig DungeonGeneration { get; set; } = new();
        public BalanceValidationConfig BalanceValidation { get; set; } = new();
        public FormulaLibraryConfig FormulaLibrary { get; set; } = new();

        public static TuningConfig Instance
        {
            get
            {
                if (_instance == null)
                {
                    lock (_lock)
                    {
                        if (_instance == null)
                        {
                            _instance = new TuningConfig();
                            _instance.LoadFromFile();
                        }
                    }
                }
                return _instance;
            }
        }

        public static bool IsDebugEnabled => Instance.Debug.EnableDebugOutput;

        private void LoadFromFile()
        {
            try
            {
                // Try multiple possible paths for the config file
                string executableDir = AppDomain.CurrentDomain.BaseDirectory;
                string currentDir = Directory.GetCurrentDirectory();
                
                string[] possiblePaths = {
                    // Relative to executable directory
                    Path.Combine(executableDir, "GameData", "TuningConfig.json"),
                    Path.Combine(executableDir, "..", "GameData", "TuningConfig.json"),
                    Path.Combine(executableDir, "..", "..", "GameData", "TuningConfig.json"),
                    
                    // Relative to current working directory
                    Path.Combine(currentDir, "GameData", "TuningConfig.json"),
                    Path.Combine(currentDir, "..", "GameData", "TuningConfig.json"),
                    Path.Combine(currentDir, "..", "..", "GameData", "TuningConfig.json"),
                    
                    // Legacy paths for backward compatibility
                    Path.Combine("GameData", "TuningConfig.json"),
                    Path.Combine("..", "GameData", "TuningConfig.json"),
                    Path.Combine("..", "..", "GameData", "TuningConfig.json")
                };
                
                string? configPath = null;
                foreach (string path in possiblePaths)
                {
                    if (File.Exists(path))
                    {
                        configPath = path;
                        break;
                    }
                }
                
                if (configPath != null)
                {
                    string jsonContent = File.ReadAllText(configPath);
                    var config = JsonSerializer.Deserialize<TuningConfig>(jsonContent, new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    });
                    
                    if (config != null)
                    {
                        Character = config.Character;
                        Attributes = config.Attributes;
                        Combat = config.Combat;
                        Progression = config.Progression;
                        XPRewards = config.XPRewards;
                        RollSystem = config.RollSystem;
                        ComboSystem = config.ComboSystem;
                        EnemyScaling = config.EnemyScaling;
                        UI = config.UI;
                        GameSpeed = config.GameSpeed;
                        ItemScaling = config.ItemScaling;
                        RarityScaling = config.RarityScaling;
                        ProgressionCurves = config.ProgressionCurves;
                        EnemyDPS = config.EnemyDPS;
                        GameData = config.GameData;
                        Debug = config.Debug;
                        CombatBalance = config.CombatBalance;
                        ExperienceSystem = config.ExperienceSystem;
                        LootSystem = config.LootSystem;
                        DungeonScaling = config.DungeonScaling;
                        StatusEffects = config.StatusEffects;
                        EquipmentScaling = config.EquipmentScaling;
                        ClassBalance = config.ClassBalance;
                        DifficultySettings = config.DifficultySettings;
                        UICustomization = config.UICustomization;
                        DungeonGeneration = config.DungeonGeneration;
                        BalanceValidation = config.BalanceValidation;
                        FormulaLibrary = config.FormulaLibrary;
                    }
                }
                else
                {
                    UIManager.WriteSystemLine($"Warning: TuningConfig.json not found, using default values. Tried paths: {string.Join(", ", possiblePaths)}");
                }
            }
            catch (Exception ex)
            {
                UIManager.WriteSystemLine($"Error loading tuning config: {ex.Message}");
                UIManager.WriteSystemLine("Using default values");
            }
        }

        public void Reload()
        {
            LoadFromFile();
        }
    }

    public class CharacterConfig
    {
        public int PlayerBaseHealth { get; set; } = 50;
        public int HealthPerLevel { get; set; } = 3;
        public int EnemyHealthPerLevel { get; set; } = 3;
    }

    public class AttributesConfig
    {
        public AttributeSet PlayerBaseAttributes { get; set; } = new();
        public int PlayerAttributesPerLevel { get; set; } = 2;
        public int EnemyAttributesPerLevel { get; set; } = 2;
        public int EnemyPrimaryAttributeBonus { get; set; } = 1;
        public int IntelligenceRollBonusPer { get; set; } = 10;
    }

    public class AttributeSet
    {
        public int Strength { get; set; } = 8;
        public int Agility { get; set; } = 6;
        public int Technique { get; set; } = 4;
        public int Intelligence { get; set; } = 4;
    }

    public class CombatConfig
    {
        public int CriticalHitThreshold { get; set; } = 20;
        public double CriticalHitMultiplier { get; set; } = 2.0;
        public int MinimumDamage { get; set; } = 1;
        public double BaseAttackTime { get; set; } = 10.0;
        public double AgilitySpeedReduction { get; set; } = 0.1;
        public double MinimumAttackTime { get; set; } = 1.0;
    }

    public class PoisonConfig
    {
        public double TickInterval { get; set; } = 10.0;
        public int DamagePerTick { get; set; } = 3;
        public int StacksPerApplication { get; set; } = 3;
    }


    public class MinMaxConfig
    {
        public int Min { get; set; }
        public int Max { get; set; }
    }

    public class ProgressionConfig
    {
        public int BaseXPToLevel2 { get; set; } = 100;
        public double XPScalingFactor { get; set; } = 1.5;
        public int EnemyXPBase { get; set; } = 10;
        public int EnemyXPPerLevel { get; set; } = 5;
        public int EnemyGoldBase { get; set; } = 5;
        public int EnemyGoldPerLevel { get; set; } = 3;
    }


    public class RollSystemConfig
    {
        public MinMaxConfig MissThreshold { get; set; } = new() { Min = 1, Max = 5 };
        public MinMaxConfig BasicAttackThreshold { get; set; } = new() { Min = 6, Max = 13 };
        public MinMaxConfig ComboThreshold { get; set; } = new() { Min = 14, Max = 20 };
        public int CriticalThreshold { get; set; } = 20;
    }

    public class ComboSystemConfig
    {
        public double ComboAmplifierAtTech5 { get; set; } = 1.05;
        public double ComboAmplifierMax { get; set; } = 2.0;
        public int ComboAmplifierMaxTech { get; set; } = 100;
    }

    public class EnemyScalingConfig
    {
        public double EnemyHealthMultiplier { get; set; } = 1.0;
        public double EnemyDamageMultiplier { get; set; } = 1.0;
        public int EnemyLevelVariance { get; set; } = 1;
        public double BaseDPSAtLevel1 { get; set; } = 1.5;
        public double DPSPerLevel { get; set; } = 0.3;
    }

    public class UIConfig
    {
        public bool EnableTextDelays { get; set; } = true;
        public int CombatDelay { get; set; } = 400;
        public int MenuDelay { get; set; } = 25;
        public int SystemDelay { get; set; } = 1000;
    }
    
    public class GameSpeedConfig
    {
        public double GameTickerInterval { get; set; } = 1.0;
        public double GameSpeedMultiplier { get; set; } = 1.0;
    }
    
    public class ItemScalingConfig
    {
        public Dictionary<string, int> StartingWeaponDamage { get; set; } = new();
        public Dictionary<string, TierRange> TierDamageRanges { get; set; } = new();
        public double GlobalDamageMultiplier { get; set; } = 1.0;
        public int WeaponDamagePerTier { get; set; } = 2;
        public int ArmorValuePerTier { get; set; } = 1;
        public double SpeedBonusPerTier { get; set; } = 0.1;
        public int MaxTier { get; set; } = 5;
        public double EnchantmentChance { get; set; } = 0.1;
    }
    
    public class TierRange
    {
        public int Min { get; set; }
        public int Max { get; set; }
    }
    
    public class WeaponTypeConfig
    {
        public string DamageFormula { get; set; } = "";
        public string SpeedFormula { get; set; } = "";
        public ScalingFactorsConfig ScalingFactors { get; set; } = new();
    }
    
    public class ArmorTypeConfig
    {
        public string ArmorFormula { get; set; } = "";
        public string ActionChanceFormula { get; set; } = "";
    }
    
    public class ScalingFactorsConfig
    {
        public double StrengthWeight { get; set; } = 1.0;
        public double AgilityWeight { get; set; } = 1.0;
        public double TechniqueWeight { get; set; } = 1.0;
        public double IntelligenceWeight { get; set; } = 1.0;
    }
    
    public class RarityModifierConfig
    {
        public double DamageMultiplier { get; set; } = 1.0;
        public double ArmorMultiplier { get; set; } = 1.0;
        public double BonusChanceMultiplier { get; set; } = 1.0;
    }
    
    public class LevelScalingCapsConfig
    {
        public double MaxDamageScaling { get; set; } = 5.0;
        public double MaxArmorScaling { get; set; } = 3.0;
        public double MaxSpeedScaling { get; set; } = 2.0;
        public MinimumValuesConfig MinimumValues { get; set; } = new();
    }
    
    public class MinimumValuesConfig
    {
        public int Damage { get; set; } = 1;
        public int Armor { get; set; } = 0;
        public double Speed { get; set; } = 0.01;
    }
    
    public class FormulaConfig
    {
        public double BaseMultiplier { get; set; } = 1.0;
        public double TierScaling { get; set; } = 1.0;
        public double LevelScaling { get; set; } = 0.1;
        public string Formula { get; set; } = "";
    }
    
    public class RarityScalingConfig
    {
        public RarityMultipliers StatBonusMultipliers { get; set; } = new();
        public RollChanceFormulas RollChanceFormulas { get; set; } = new();
        public MagicFindScalingConfig MagicFindScaling { get; set; } = new();
        public LevelBasedRarityScalingConfig LevelBasedRarityScaling { get; set; } = new();
    }
    
    public class RarityMultipliers
    {
        public double Common { get; set; } = 1.0;
        public double Uncommon { get; set; } = 1.3;
        public double Rare { get; set; } = 1.7;
        public double Epic { get; set; } = 2.2;
        public double Legendary { get; set; } = 3.0;
    }
    
    public class RollChanceFormulas
    {
        public string ActionBonusChance { get; set; } = "BaseChance * (1 + PlayerLevel * 0.02)";
        public string StatBonusChance { get; set; } = "BaseChance * (1 + PlayerLevel * 0.03)";
    }
    
    public class ProgressionCurvesConfig
    {
        public string ExperienceFormula { get; set; } = "BaseXP * (Level^ExponentFactor)";
        public string AttributeGrowth { get; set; } = "BaseAttributes + (Level * LinearGrowth) + (Level^2 * QuadraticGrowth)";
        public double ExponentFactor { get; set; } = 1.5;
        public double LinearGrowth { get; set; } = 1.0;
        public double QuadraticGrowth { get; set; } = 0.1;
    }
    
    public class XPRewardsConfig
    {
        public string BaseXPFormula { get; set; } = "EnemyXPBase + (EnemyLevel * EnemyXPPerLevel)";
        public Dictionary<string, LevelDifficultyMultiplier> LevelDifferenceMultipliers { get; set; } = new();
        public DungeonCompletionBonusConfig DungeonCompletionBonus { get; set; } = new();
        public string GroupXPFormula { get; set; } = "BaseXP * LevelMultiplier * DifficultyMultiplier";
        public int MinimumXP { get; set; } = 1;
        public double MaximumXPMultiplier { get; set; } = 5.0;
    }
    
    public class LevelDifficultyMultiplier
    {
        public int LevelDifference { get; set; } = 0;
        public double Multiplier { get; set; } = 1.0;
        public string Description { get; set; } = "";
    }
    
    public class DungeonCompletionBonusConfig
    {
        public int BaseBonus { get; set; } = 50;
        public int BonusPerRoom { get; set; } = 25;
        public double LevelDifferenceMultiplier { get; set; } = 1.0;
    }
    
    public class EnemyDPSConfig
    {
        public double BaseDPSAtLevel1 { get; set; } = 3.0;
        public double DPSPerLevel { get; set; } = 2.5;
        public string DPSScalingFormula { get; set; } = "BaseDPSAtLevel1 + (Level * DPSPerLevel)";
        public Dictionary<string, EnemyArchetypeConfig> Archetypes { get; set; } = new();
        public DPSBalanceValidationConfig BalanceValidation { get; set; } = new();
        public SustainBalanceConfig SustainBalance { get; set; } = new();
    }
    
    public class EnemyArchetypeConfig
    {
        public string Name { get; set; } = "";
        public string Description { get; set; } = "";
        public double SpeedRatio { get; set; } = 1.0;
        public double DamageRatio { get; set; } = 1.0;
        public Dictionary<string, double> AttributeFocus { get; set; } = new();
    }
    
    public class DPSBalanceValidationConfig
    {
        public double TolerancePercentage { get; set; } = 10.0;
        public double MinimumDPS { get; set; } = 1.0;
        public double MaximumDPSMultiplier { get; set; } = 10.0;
    }
    
    public class MagicFindScalingConfig
    {
        public RarityMagicFindConfig Common { get; set; } = new();
        public RarityMagicFindConfig Uncommon { get; set; } = new();
        public RarityMagicFindConfig Rare { get; set; } = new();
        public RarityMagicFindConfig Epic { get; set; } = new();
        public RarityMagicFindConfig Legendary { get; set; } = new();
    }
    
    public class RarityMagicFindConfig
    {
        public double PerPointMultiplier { get; set; } = 0.0;
        public string Description { get; set; } = "";
    }
    
    public class LevelBasedRarityScalingConfig
    {
        public CommonRarityScalingConfig Common { get; set; } = new();
        public UncommonRarityScalingConfig Uncommon { get; set; } = new();
        public RareRarityScalingConfig Rare { get; set; } = new();
        public EpicRarityScalingConfig Epic { get; set; } = new();
        public LegendaryRarityScalingConfig Legendary { get; set; } = new();
    }
    
    public class CommonRarityScalingConfig
    {
        public double BaseMultiplier { get; set; } = 1.0;
        public double LevelReduction { get; set; } = 0.0;
        public string Description { get; set; } = "";
    }
    
    public class UncommonRarityScalingConfig
    {
        public double BaseMultiplier { get; set; } = 1.0;
        public double LevelBonus { get; set; } = 0.0;
        public string Description { get; set; } = "";
    }
    
    public class RareRarityScalingConfig
    {
        public double BaseMultiplier { get; set; } = 1.0;
        public double LevelBonus { get; set; } = 0.0;
        public string Description { get; set; } = "";
    }
    
    public class EpicRarityScalingConfig
    {
        public int MinLevel { get; set; } = 1;
        public double EarlyMultiplier { get; set; } = 1.0;
        public double BaseMultiplier { get; set; } = 1.0;
        public double LevelBonus { get; set; } = 0.0;
        public string Description { get; set; } = "";
    }
    
    public class LegendaryRarityScalingConfig
    {
        public int MinLevel { get; set; } = 1;
        public int EarlyThreshold { get; set; } = 1;
        public double EarlyMultiplier { get; set; } = 1.0;
        public double MidMultiplier { get; set; } = 1.0;
        public double BaseMultiplier { get; set; } = 1.0;
        public double LevelBonus { get; set; } = 0.0;
        public string Description { get; set; } = "";
    }
    
    public class WeaponScalingConfig
    {
        public StartingWeaponDamageConfig StartingWeaponDamage { get; set; } = new();
        public TierDamageRangesConfig TierDamageRanges { get; set; } = new();
        public double GlobalDamageMultiplier { get; set; } = 1.0;
        public string Description { get; set; } = "";
    }
    
    public class StartingWeaponDamageConfig
    {
        public int Mace { get; set; } = 3;
        public int Sword { get; set; } = 2;
        public int Dagger { get; set; } = 1;
        public int Wand { get; set; } = 1;
    }
    
    public class TierDamageRangesConfig
    {
        public MinMaxConfig Tier1 { get; set; } = new() { Min = 1, Max = 3 };
        public MinMaxConfig Tier2 { get; set; } = new() { Min = 4, Max = 6 };
        public MinMaxConfig Tier3 { get; set; } = new() { Min = 7, Max = 9 };
        public MinMaxConfig Tier4 { get; set; } = new() { Min = 10, Max = 12 };
        public MinMaxConfig Tier5 { get; set; } = new() { Min = 13, Max = 15 };
    }
    
    public class SustainBalanceConfig
    {
        public string Description { get; set; } = "Comprehensive balance system covering DPS/sustain, speed/damage, health/armor, and attribute gain ratios";
        public TargetActionsToKillConfig TargetActionsToKill { get; set; } = new();
        public DPSToSustainRatioConfig DPSToSustainRatio { get; set; } = new();
        public SpeedToDamageRatioConfig SpeedToDamageRatio { get; set; } = new();
        public HealthToArmorRatioConfig HealthToArmorRatio { get; set; } = new();
        public AttributeGainRatioConfig AttributeGainRatio { get; set; } = new();
        public SustainScalingConfig SustainScaling { get; set; } = new();
        public SustainBalanceValidationConfig BalanceValidation { get; set; } = new();
    }
    
    public class TargetActionsToKillConfig
    {
        public int Level1 { get; set; } = 10;
        public int Level10 { get; set; } = 12;
        public int Level20 { get; set; } = 15;
        public int Level30 { get; set; } = 18;
        public string Formula { get; set; } = "BaseActions + (Level * ActionsPerLevel) + (Level^2 * QuadraticFactor)";
    }
    
    public class DPSToSustainRatioConfig
    {
        public double BaseRatio { get; set; } = 1.0;
        public string Description { get; set; } = "Overall DPS vs sustain balance (1.0 = balanced, >1.0 = high DPS/low sustain, <1.0 = low DPS/high sustain)";
        public Dictionary<string, double> ArchetypeModifiers { get; set; } = new();
    }
    
    public class SpeedToDamageRatioConfig
    {
        public double BaseRatio { get; set; } = 1.0;
        public string Description { get; set; } = "Attack speed vs damage per hit balance (1.0 = balanced, >1.0 = fast/weak, <1.0 = slow/strong)";
        public Dictionary<string, double> ArchetypeModifiers { get; set; } = new();
    }
    
    public class HealthToArmorRatioConfig
    {
        public double BaseRatio { get; set; } = 10.0;
        public string Description { get; set; } = "Health pool vs armor value balance (10.0 = 10:1 ratio, higher = more health-focused, lower = more armor-focused)";
        public Dictionary<string, double> ArchetypeModifiers { get; set; } = new();
    }
    
    public class AttributeGainRatioConfig
    {
        public double BaseRatio { get; set; } = 1.0;
        public string Description { get; set; } = "Attribute scaling relative to level (1.0 = balanced, >1.0 = high attribute gain, <1.0 = low attribute gain)";
        public double PrimaryAttributeBonus { get; set; } = 2.0;
        public double SecondaryAttributeBonus { get; set; } = 1.0;
        public Dictionary<string, double> ArchetypeModifiers { get; set; } = new();
    }
    
    public class SustainScalingConfig
    {
        public double HealthPerLevel { get; set; } = 2;
        public double ArmorPerLevel { get; set; } = 0.1;
        public double RegenerationPerLevel { get; set; } = 0.1;
        public string Description { get; set; } = "How sustain stats scale with level relative to DPS";
    }
    
    public class SustainBalanceValidationConfig
    {
        public int MinActionsToKill { get; set; } = 5;
        public int MaxActionsToKill { get; set; } = 25;
        public double DPSRatioTolerance { get; set; } = 0.3;
        public double SpeedRatioTolerance { get; set; } = 0.2;
        public double HealthArmorRatioTolerance { get; set; } = 2.0;
        public double AttributeRatioTolerance { get; set; } = 0.2;
    }
    
    public class GameDataConfig
    {
        public bool AutoGenerateOnLaunch { get; set; } = true;
        public bool ShowGenerationMessages { get; set; } = false;
        public string Description { get; set; } = "Controls automatic generation of game data files at launch";
    }

    public class DebugConfig
    {
        public bool EnableDebugOutput { get; set; } = false;
        public string Description { get; set; } = "Controls whether debug messages are displayed throughout the game";
    }

    public class CombatBalanceConfig
    {
        public string ArmorReductionFormula { get; set; } = "Damage * (1 - Armor / (Armor + 100))";
        public double CriticalHitChance { get; set; } = 0.05;
        public double CriticalHitDamageMultiplier { get; set; } = 1.5;
        public RollDamageMultipliersConfig RollDamageMultipliers { get; set; } = new();
        public StatusEffectScalingConfig StatusEffectScaling { get; set; } = new();
        public EnvironmentalEffectsConfig EnvironmentalEffects { get; set; } = new();
        public string Description { get; set; } = "Advanced combat mechanics and balance settings";
    }

    public class RollDamageMultipliersConfig
    {
        public double ComboRollDamageMultiplier { get; set; } = 1.5;
        public double BasicRollDamageMultiplier { get; set; } = 1.25;
        public double ComboAmplificationScalingMultiplier { get; set; } = 2.0;
        public double TierScalingFallbackMultiplier { get; set; } = 0.5;
    }

    public class StatusEffectScalingConfig
    {
        public double BleedDuration { get; set; } = 3.0;
        public double PoisonDuration { get; set; } = 5.0;
        public double StunDuration { get; set; } = 2.0;
        public double BurnDuration { get; set; } = 4.0;
        public double FreezeDuration { get; set; } = 3.0;
        public double StatusEffectDamageScaling { get; set; } = 1.0;
    }

    public class EnvironmentalEffectsConfig
    {
        public bool EnableEnvironmentalEffects { get; set; } = true;
        public double EnvironmentalDamageMultiplier { get; set; } = 0.1;
        public double EnvironmentalDebuffChance { get; set; } = 0.15;
        public double EnvironmentalBuffChance { get; set; } = 0.1;
    }

    public class ExperienceSystemConfig
    {
        public string BaseXPFormula { get; set; } = "BaseXP * (Level^1.5)";
        public int LevelCap { get; set; } = 100;
        public int StatPointsPerLevel { get; set; } = 2;
        public int SkillPointsPerLevel { get; set; } = 1;
        public int AttributeCap { get; set; } = 100;
        public string Description { get; set; } = "Character progression and experience system";
    }

    public class LootSystemConfig
    {
        public double BaseDropChance { get; set; } = 0.3;
        public double DropChancePerLevel { get; set; } = 0.02;
        public double MaxDropChance { get; set; } = 0.8;
        public double MagicFindEffectiveness { get; set; } = 0.01;
        public double GoldDropMultiplier { get; set; } = 1.0;
        public double ItemValueMultiplier { get; set; } = 1.0;
        public string Description { get; set; } = "Loot drop rates and economy settings";
    }

    public class DungeonScalingConfig
    {
        public int RoomCountBase { get; set; } = 3;
        public double RoomCountPerLevel { get; set; } = 0.5;
        public int EnemyCountPerRoom { get; set; } = 2;
        public double BossRoomChance { get; set; } = 0.1;
        public double TrapRoomChance { get; set; } = 0.2;
        public double TreasureRoomChance { get; set; } = 0.15;
        public string Description { get; set; } = "Dungeon generation and scaling parameters";
    }

    public class StatusEffectsConfig
    {
        public StatusEffectConfig Bleed { get; set; } = new() { DamagePerTick = 2, TickInterval = 3.0, MaxStacks = 5 };
        public StatusEffectConfig Burn { get; set; } = new() { DamagePerTick = 3, TickInterval = 2.0, MaxStacks = 3 };
        public StatusEffectConfig Freeze { get; set; } = new() { SpeedReduction = 0.5, Duration = 5.0 };
        public StatusEffectConfig Stun { get; set; } = new() { SkipTurns = 1, Duration = 2.0 };
        public string Description { get; set; } = "Status effect configurations and balance";
    }

    public class StatusEffectConfig
    {
        public int DamagePerTick { get; set; } = 0;
        public double TickInterval { get; set; } = 0;
        public int MaxStacks { get; set; } = 1;
        public double SpeedReduction { get; set; } = 0;
        public double Duration { get; set; } = 0;
        public int SkipTurns { get; set; } = 0;
    }

    public class EquipmentScalingConfig
    {
        public int WeaponDamagePerTier { get; set; } = 2;
        public int ArmorValuePerTier { get; set; } = 1;
        public double SpeedBonusPerTier { get; set; } = 0.1;
        public int MaxTier { get; set; } = 5;
        public double EnchantmentChance { get; set; } = 0.1;
        public string Description { get; set; } = "Equipment progression and scaling";
    }

    public class ClassBalanceConfig
    {
        public ClassMultipliers Barbarian { get; set; } = new() { HealthMultiplier = 1.2, DamageMultiplier = 1.1, SpeedMultiplier = 0.9 };
        public ClassMultipliers Warrior { get; set; } = new() { HealthMultiplier = 1.1, DamageMultiplier = 1.0, SpeedMultiplier = 1.0 };
        public ClassMultipliers Rogue { get; set; } = new() { HealthMultiplier = 0.9, DamageMultiplier = 1.2, SpeedMultiplier = 1.1 };
        public ClassMultipliers Wizard { get; set; } = new() { HealthMultiplier = 0.8, DamageMultiplier = 1.3, SpeedMultiplier = 1.0 };
        public string Description { get; set; } = "Character class balance multipliers";
    }

    public class ClassMultipliers
    {
        public double HealthMultiplier { get; set; } = 1.0;
        public double DamageMultiplier { get; set; } = 1.0;
        public double SpeedMultiplier { get; set; } = 1.0;
    }

    public class DifficultySettingsConfig
    {
        public DifficultyLevel Easy { get; set; } = new() { EnemyHealthMultiplier = 0.8, EnemyDamageMultiplier = 0.8, XPMultiplier = 1.2, LootMultiplier = 1.1 };
        public DifficultyLevel Normal { get; set; } = new() { EnemyHealthMultiplier = 1.0, EnemyDamageMultiplier = 1.0, XPMultiplier = 1.0, LootMultiplier = 1.0 };
        public DifficultyLevel Hard { get; set; } = new() { EnemyHealthMultiplier = 1.3, EnemyDamageMultiplier = 1.2, XPMultiplier = 1.5, LootMultiplier = 1.3 };
        public string Description { get; set; } = "Difficulty level multipliers for game balance";
    }

    public class DifficultyLevel
    {
        public double EnemyHealthMultiplier { get; set; } = 1.0;
        public double EnemyDamageMultiplier { get; set; } = 1.0;
        public double XPMultiplier { get; set; } = 1.0;
        public double LootMultiplier { get; set; } = 1.0;
    }

    public class UICustomizationConfig
    {
        public string MenuSeparator { get; set; } = "==========================================";
        public string SubMenuSeparator { get; set; } = "------------------------------------------";
        public string InvalidChoiceMessage { get; set; } = "Invalid choice. Please try again.";
        public string PressAnyKeyMessage { get; set; } = "Press any key to continue...";
        public RarityPrefixesConfig RarityPrefixes { get; set; } = new();
        public ActionNamesConfig ActionNames { get; set; } = new();
        public ErrorMessagesConfig ErrorMessages { get; set; } = new();
        public DebugMessagesConfig DebugMessages { get; set; } = new();
    }

    public class RarityPrefixesConfig
    {
        public string Common { get; set; } = "Common";
        public string Uncommon { get; set; } = "Uncommon";
        public string Rare { get; set; } = "Rare";
        public string Epic { get; set; } = "Epic";
        public string Legendary { get; set; } = "Legendary";
    }

    public class ActionNamesConfig
    {
        public string BasicAttackName { get; set; } = "BASIC ATTACK";
        public string DefaultActionDescription { get; set; } = "A basic action";
    }

    public class ErrorMessagesConfig
    {
        public string FileNotFoundError { get; set; } = "File not found";
        public string JsonDeserializationError { get; set; } = "JSON deserialization failed";
        public string InvalidDataError { get; set; } = "Invalid data format";
        public string SaveError { get; set; } = "Failed to save data";
        public string LoadError { get; set; } = "Failed to load data";
    }

    public class DebugMessagesConfig
    {
        public string DebugPrefix { get; set; } = "DEBUG";
        public string WarningPrefix { get; set; } = "Warning";
        public string ErrorPrefix { get; set; } = "Error";
        public string InfoPrefix { get; set; } = "Info";
    }


    public class BalanceValidationConfig
    {
        public bool EnableBalanceChecks { get; set; } = true;
        public double MaxDamageVariance { get; set; } = 0.2;
        public double MinCombatDuration { get; set; } = 2.0;
        public double MaxCombatDuration { get; set; } = 10.0;
        public List<double> TargetDPSRange { get; set; } = new() { 1.0, 5.0 };
        public bool ValidateEnemyScaling { get; set; } = true;
        public bool ValidateLootRates { get; set; } = true;
        public bool ValidateXPProgression { get; set; } = true;
        public string Description { get; set; } = "Built-in balance validation and checking tools";
    }

    public class FormulaLibraryConfig
    {
        public string ArmorReductionFormula { get; set; } = "Damage * (1 - Armor / (Armor + 100))";
        public string CriticalHitFormula { get; set; } = "BaseDamage * CriticalMultiplier";
        public string ComboAmplificationFormula { get; set; } = "BaseAmplifier ^ ComboStep";
        public string StatusEffectDamageFormula { get; set; } = "BaseDamage * StatusEffectScaling";
        public string EnvironmentalDamageFormula { get; set; } = "BaseDamage * EnvironmentalMultiplier";
        public string Description { get; set; } = "Configurable mathematical formulas for game calculations";
    }

}
