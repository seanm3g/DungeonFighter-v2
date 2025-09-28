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
        public EquipmentConfig Equipment { get; set; } = new();
        public ProgressionConfig Progression { get; set; } = new();
        public XPRewardsConfig XPRewards { get; set; } = new();
        public LootConfig Loot { get; set; } = new();
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
                        Equipment = config.Equipment;
                        Progression = config.Progression;
                        XPRewards = config.XPRewards;
                        Loot = config.Loot;
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
                    }
                }
                else
                {
                    Console.WriteLine($"Warning: TuningConfig.json not found, using default values. Tried paths: {string.Join(", ", possiblePaths)}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error loading tuning config: {ex.Message}");
                Console.WriteLine("Using default values");
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

    public class EquipmentConfig
    {
        public int BonusDamagePerTier { get; set; } = 1;
        public MinMaxConfig BonusAttackSpeedRange { get; set; } = new();
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

    public class LootConfig
    {
        public double LootChanceBase { get; set; } = 0.3;
        public double LootChancePerLevel { get; set; } = 0.05;
        public double MaximumLootChance { get; set; } = 0.8;
        public double MagicFindLootChanceMultiplier { get; set; } = 0.01;
        public string Description { get; set; } = "";
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
    }

    public class UIConfig
    {
        public bool EnableTextDelays { get; set; } = true;
        public int BaseDelayPerAction { get; set; } = 400;
        public int MinimumDelay { get; set; } = 50;
        public double CombatSpeedMultiplier { get; set; } = 1.0;
        public int CombatLogDelay { get; set; } = 500;
        public int MainMenuDelay { get; set; } = 500;
        public int DungeonEntryDelay { get; set; } = 1000;
        public int RoomEntryDelay { get; set; } = 1000;
        public int EnemyEncounterDelay { get; set; } = 1000;
        public int RoomClearedDelay { get; set; } = 800;
    }
    
    public class GameSpeedConfig
    {
        public double GameTickerInterval { get; set; } = 1.0;
        public double GameSpeedMultiplier { get; set; } = 1.0;
    }
    
    public class ItemScalingConfig
    {
        public Dictionary<string, WeaponTypeConfig> WeaponTypes { get; set; } = new();
        public Dictionary<string, ArmorTypeConfig> ArmorTypes { get; set; } = new();
        public Dictionary<string, RarityModifierConfig> RarityModifiers { get; set; } = new();
        public LevelScalingCapsConfig LevelScalingCaps { get; set; } = new();
        public FormulaConfig WeaponDamageFormula { get; set; } = new();
        public FormulaConfig ArmorValueFormula { get; set; } = new();
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
        public bool ShowGenerationMessages { get; set; } = true;
        public string Description { get; set; } = "Controls automatic generation of game data files at launch";
    }

    public class DebugConfig
    {
        public bool EnableDebugOutput { get; set; } = false;
        public string Description { get; set; } = "Controls whether debug messages are displayed throughout the game";
    }

}
