using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;

namespace RPGGame
{
    public class GameConfiguration
    {
        private static GameConfiguration? _instance;
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
        public EnemyBalanceConfig EnemyBalance { get; set; } = new();
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

        public static GameConfiguration Instance
        {
            get
            {
                if (_instance == null)
                {
                    lock (_lock)
                    {
                        if (_instance == null)
                        {
                            _instance = new GameConfiguration();
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
                    var config = JsonSerializer.Deserialize<GameConfiguration>(jsonContent, new JsonSerializerOptions
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
                        EnemyBalance = config.EnemyBalance;
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
        public int PlayerBaseHealth { get; set; }
        public int HealthPerLevel { get; set; }
        public int EnemyHealthPerLevel { get; set; }
    }

    public class AttributesConfig
    {
        public AttributeSet PlayerBaseAttributes { get; set; } = new();
        public int PlayerAttributesPerLevel { get; set; }
        public int EnemyAttributesPerLevel { get; set; }
        public int EnemyPrimaryAttributeBonus { get; set; }
        public int IntelligenceRollBonusPer { get; set; }
    }

    public class AttributeSet
    {
        public int Strength { get; set; }
        public int Agility { get; set; }
        public int Technique { get; set; }
        public int Intelligence { get; set; }
    }

    public class CombatConfig
    {
        public int CriticalHitThreshold { get; set; }
        public double CriticalHitMultiplier { get; set; }
        public int MinimumDamage { get; set; }
        public double BaseAttackTime { get; set; }
        public double AgilitySpeedReduction { get; set; }
        public double MinimumAttackTime { get; set; }
    }

    public class PoisonConfig
    {
        public double TickInterval { get; set; }
        public int DamagePerTick { get; set; }
        public int StacksPerApplication { get; set; }
    }


    public class MinMaxConfig
    {
        public int Min { get; set; }
        public int Max { get; set; }
    }

    public class ProgressionConfig
    {
        public int BaseXPToLevel2 { get; set; }
        public double XPScalingFactor { get; set; }
        public int EnemyXPBase { get; set; }
        public int EnemyXPPerLevel { get; set; }
        public int EnemyGoldBase { get; set; }
        public int EnemyGoldPerLevel { get; set; }
    }


    public class RollSystemConfig
    {
        public MinMaxConfig MissThreshold { get; set; } = new();
        public MinMaxConfig BasicAttackThreshold { get; set; } = new();
        public MinMaxConfig ComboThreshold { get; set; } = new();
        public int CriticalThreshold { get; set; }
    }

    public class ComboSystemConfig
    {
        public double ComboAmplifierAtTech5 { get; set; }
        public double ComboAmplifierMax { get; set; }
        public int ComboAmplifierMaxTech { get; set; }
    }

    public class EnemyScalingConfig
    {
        public double EnemyHealthMultiplier { get; set; }
        public double EnemyDamageMultiplier { get; set; }
        public int EnemyLevelVariance { get; set; }
        public double BaseDPSAtLevel1 { get; set; }
        public double DPSPerLevel { get; set; }
        public int EnemyBaseArmorAtLevel1 { get; set; }
        public double EnemyArmorPerLevel { get; set; }
    }

    public class EnemyBalanceConfig
    {
        public int BaseTotalPointsAtLevel1 { get; set; }
        public int TotalPointsPerLevel { get; set; }
        public AllocationConfig DPSAllocation { get; set; } = new();
        public AllocationConfig SUSTAINAllocation { get; set; } = new();
        public DPSComponentsConfig DPSComponents { get; set; } = new();
        public SUSTAINComponentsConfig SUSTAINComponents { get; set; } = new();
        public StatConversionRatesConfig StatConversionRates { get; set; } = new();
        public Dictionary<string, ArchetypeConfig> ArchetypeConfigs { get; set; } = new();
    }

    public class ArchetypeConfig
    {
        public double DPSPoolRatio { get; set; }
        public double SUSTAINPoolRatio { get; set; }
        public double DPSAttackRatio { get; set; }
        public double DPSAttackSpeedRatio { get; set; }
        public double SUSTAINHealthRatio { get; set; }
        public double SUSTAINArmorRatio { get; set; }
    }

    public class AllocationConfig
    {
        public double MinPercentage { get; set; }
        public double MaxPercentage { get; set; }
        public double DefaultPercentage { get; set; }
    }

    public class DPSComponentsConfig
    {
        public double AttackWeight { get; set; }
        public double AttackSpeedWeight { get; set; }
    }

    public class SUSTAINComponentsConfig
    {
        public double HealthWeight { get; set; }
        public double ArmorWeight { get; set; }
    }

    public class StatConversionRatesConfig
    {
        public double DamagePerPoint { get; set; }
        public double AttackSpeedPerPoint { get; set; }
        public double HealthPerPoint { get; set; }
        public double ArmorPerPoint { get; set; }
    }

    public class UIConfig
    {
        public bool EnableTextDelays { get; set; }
        public int CombatDelay { get; set; }
        public int MenuDelay { get; set; }
        public int SystemDelay { get; set; }
        public int TitleDelay { get; set; }
    }
    
    public class GameSpeedConfig
    {
        public double GameTickerInterval { get; set; }
        public double GameSpeedMultiplier { get; set; }
    }
    
    public class ItemScalingConfig
    {
        public Dictionary<string, int> StartingWeaponDamage { get; set; } = new();
        public Dictionary<string, TierRange> TierDamageRanges { get; set; } = new();
        public double GlobalDamageMultiplier { get; set; }
        public int WeaponDamagePerTier { get; set; }
        public int ArmorValuePerTier { get; set; }
        public double SpeedBonusPerTier { get; set; }
        public int MaxTier { get; set; }
        public double EnchantmentChance { get; set; }
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
        public double StrengthWeight { get; set; }
        public double AgilityWeight { get; set; }
        public double TechniqueWeight { get; set; }
        public double IntelligenceWeight { get; set; }
    }
    
    public class RarityModifierConfig
    {
        public double DamageMultiplier { get; set; }
        public double ArmorMultiplier { get; set; }
        public double BonusChanceMultiplier { get; set; }
    }
    
    public class LevelScalingCapsConfig
    {
        public double MaxDamageScaling { get; set; }
        public double MaxArmorScaling { get; set; }
        public double MaxSpeedScaling { get; set; }
        public MinimumValuesConfig MinimumValues { get; set; } = new();
    }
    
    public class MinimumValuesConfig
    {
        public int Damage { get; set; }
        public int Armor { get; set; }
        public double Speed { get; set; }
    }
    
    public class FormulaConfig
    {
        public double BaseMultiplier { get; set; }
        public double TierScaling { get; set; }
        public double LevelScaling { get; set; }
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
        public double Common { get; set; }
        public double Uncommon { get; set; }
        public double Rare { get; set; }
        public double Epic { get; set; }
        public double Legendary { get; set; }
    }
    
    public class RollChanceFormulas
    {
        public string ActionBonusChance { get; set; } = "";
        public string StatBonusChance { get; set; } = "";
    }
    
    public class ProgressionCurvesConfig
    {
        public string ExperienceFormula { get; set; } = "";
        public string AttributeGrowth { get; set; } = "";
        public double ExponentFactor { get; set; }
        public double LinearGrowth { get; set; }
        public double QuadraticGrowth { get; set; }
    }
    
    public class XPRewardsConfig
    {
        public string BaseXPFormula { get; set; } = "";
        public Dictionary<string, LevelDifficultyMultiplier> LevelDifferenceMultipliers { get; set; } = new();
        public DungeonCompletionBonusConfig DungeonCompletionBonus { get; set; } = new();
        public string GroupXPFormula { get; set; } = "";
        public int MinimumXP { get; set; }
        public double MaximumXPMultiplier { get; set; }
    }
    
    public class LevelDifficultyMultiplier
    {
        public int LevelDifference { get; set; }
        public double Multiplier { get; set; }
        public string Description { get; set; } = "";
    }
    
    public class DungeonCompletionBonusConfig
    {
        public int BaseBonus { get; set; }
        public int BonusPerRoom { get; set; }
        public double LevelDifferenceMultiplier { get; set; }
    }
    
    public class EnemyDPSConfig
    {
        public double BaseDPSAtLevel1 { get; set; }
        public double DPSPerLevel { get; set; }
        public string DPSScalingFormula { get; set; } = "";
        public Dictionary<string, EnemyArchetypeConfig> Archetypes { get; set; } = new();
        public DPSBalanceValidationConfig BalanceValidation { get; set; } = new();
        public SustainBalanceConfig SustainBalance { get; set; } = new();
    }
    
    public class EnemyArchetypeConfig
    {
        public string Name { get; set; } = "";
        public string Description { get; set; } = "";
        public double SpeedRatio { get; set; }
        public double DamageRatio { get; set; }
        public Dictionary<string, double> AttributeFocus { get; set; } = new();
    }
    
    public class DPSBalanceValidationConfig
    {
        public double TolerancePercentage { get; set; }
        public double MinimumDPS { get; set; }
        public double MaximumDPSMultiplier { get; set; }
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
        public double PerPointMultiplier { get; set; }
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
        public double BaseMultiplier { get; set; }
        public double LevelReduction { get; set; }
        public string Description { get; set; } = "";
    }
    
    public class UncommonRarityScalingConfig
    {
        public double BaseMultiplier { get; set; }
        public double LevelBonus { get; set; }
        public string Description { get; set; } = "";
    }
    
    public class RareRarityScalingConfig
    {
        public double BaseMultiplier { get; set; }
        public double LevelBonus { get; set; }
        public string Description { get; set; } = "";
    }
    
    public class EpicRarityScalingConfig
    {
        public int MinLevel { get; set; }
        public double EarlyMultiplier { get; set; }
        public double BaseMultiplier { get; set; }
        public double LevelBonus { get; set; }
        public string Description { get; set; } = "";
    }
    
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
    
    public class WeaponScalingConfig
    {
        public StartingWeaponDamageConfig StartingWeaponDamage { get; set; } = new();
        public TierDamageRangesConfig TierDamageRanges { get; set; } = new();
        public double GlobalDamageMultiplier { get; set; }
        public string Description { get; set; } = "";
    }
    
    public class StartingWeaponDamageConfig
    {
        public int Mace { get; set; }
        public int Sword { get; set; }
        public int Dagger { get; set; }
        public int Wand { get; set; }
    }
    
    public class TierDamageRangesConfig
    {
        public MinMaxConfig Tier1 { get; set; } = new();
        public MinMaxConfig Tier2 { get; set; } = new();
        public MinMaxConfig Tier3 { get; set; } = new();
        public MinMaxConfig Tier4 { get; set; } = new();
        public MinMaxConfig Tier5 { get; set; } = new();
    }
    
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
    
    public class TargetActionsToKillConfig
    {
        public int Level1 { get; set; }
        public int Level10 { get; set; }
        public int Level20 { get; set; }
        public int Level30 { get; set; }
        public string Formula { get; set; } = "";
    }
    
    public class DPSToSustainRatioConfig
    {
        public double BaseRatio { get; set; }
        public string Description { get; set; } = "";
        public Dictionary<string, double> ArchetypeModifiers { get; set; } = new();
    }
    
    public class SpeedToDamageRatioConfig
    {
        public double BaseRatio { get; set; }
        public string Description { get; set; } = "";
        public Dictionary<string, double> ArchetypeModifiers { get; set; } = new();
    }
    
    public class HealthToArmorRatioConfig
    {
        public double BaseRatio { get; set; }
        public string Description { get; set; } = "";
        public Dictionary<string, double> ArchetypeModifiers { get; set; } = new();
    }
    
    public class AttributeGainRatioConfig
    {
        public double BaseRatio { get; set; }
        public string Description { get; set; } = "";
        public double PrimaryAttributeBonus { get; set; }
        public double SecondaryAttributeBonus { get; set; }
        public Dictionary<string, double> ArchetypeModifiers { get; set; } = new();
    }
    
    public class SustainScalingConfig
    {
        public double HealthPerLevel { get; set; }
        public double ArmorPerLevel { get; set; }
        public double RegenerationPerLevel { get; set; }
        public string Description { get; set; } = "";
    }
    
    public class SustainBalanceValidationConfig
    {
        public int MinActionsToKill { get; set; }
        public int MaxActionsToKill { get; set; }
        public double DPSRatioTolerance { get; set; }
        public double SpeedRatioTolerance { get; set; }
        public double HealthArmorRatioTolerance { get; set; }
        public double AttributeRatioTolerance { get; set; }
    }
    
    public class GameDataConfig
    {
        public bool AutoGenerateOnLaunch { get; set; }
        public bool ShowGenerationMessages { get; set; }
        public string Description { get; set; } = "";
    }

    public class DebugConfig
    {
        public bool EnableDebugOutput { get; set; }
        public string Description { get; set; } = "";
    }

    public class CombatBalanceConfig
    {
        public string ArmorReductionFormula { get; set; } = "";
        public double CriticalHitChance { get; set; }
        public double CriticalHitDamageMultiplier { get; set; }
        public RollDamageMultipliersConfig RollDamageMultipliers { get; set; } = new();
        public StatusEffectScalingConfig StatusEffectScaling { get; set; } = new();
        public EnvironmentalEffectsConfig EnvironmentalEffects { get; set; } = new();
        public string Description { get; set; } = "";
    }

    public class RollDamageMultipliersConfig
    {
        public double ComboRollDamageMultiplier { get; set; }
        public double BasicRollDamageMultiplier { get; set; }
        public double ComboAmplificationScalingMultiplier { get; set; }
        public double TierScalingFallbackMultiplier { get; set; }
    }

    public class StatusEffectScalingConfig
    {
        public double BleedDuration { get; set; }
        public double PoisonDuration { get; set; }
        public double StunDuration { get; set; }
        public double BurnDuration { get; set; }
        public double FreezeDuration { get; set; }
        public double StatusEffectDamageScaling { get; set; }
    }

    public class EnvironmentalEffectsConfig
    {
        public bool EnableEnvironmentalEffects { get; set; }
        public double EnvironmentalDamageMultiplier { get; set; }
        public double EnvironmentalDebuffChance { get; set; }
        public double EnvironmentalBuffChance { get; set; }
    }

    public class ExperienceSystemConfig
    {
        public string BaseXPFormula { get; set; } = "";
        public int LevelCap { get; set; }
        public int StatPointsPerLevel { get; set; }
        public int SkillPointsPerLevel { get; set; }
        public int AttributeCap { get; set; }
        public string Description { get; set; } = "";
    }

    public class LootSystemConfig
    {
        public double BaseDropChance { get; set; }
        public double DropChancePerLevel { get; set; }
        public double MaxDropChance { get; set; }
        public double MagicFindEffectiveness { get; set; }
        public double GoldDropMultiplier { get; set; }
        public double ItemValueMultiplier { get; set; }
        public string Description { get; set; } = "";
    }

    public class DungeonScalingConfig
    {
        public int RoomCountBase { get; set; }
        public double RoomCountPerLevel { get; set; }
        public int EnemyCountPerRoom { get; set; }
        public double BossRoomChance { get; set; }
        public double TrapRoomChance { get; set; }
        public double TreasureRoomChance { get; set; }
        public string Description { get; set; } = "";
    }

    public class StatusEffectsConfig
    {
        public StatusEffectConfig Bleed { get; set; } = new();
        public StatusEffectConfig Burn { get; set; } = new();
        public StatusEffectConfig Freeze { get; set; } = new();
        public StatusEffectConfig Stun { get; set; } = new();
        public StatusEffectConfig Poison { get; set; } = new();
        public string Description { get; set; } = "";
    }

    public class StatusEffectConfig
    {
        public int DamagePerTick { get; set; }
        public double TickInterval { get; set; }
        public int MaxStacks { get; set; }
        public int StacksPerApplication { get; set; }
        public double SpeedReduction { get; set; }
        public double Duration { get; set; }
        public int SkipTurns { get; set; }
    }


    public class EquipmentScalingConfig
    {
        public int WeaponDamagePerTier { get; set; }
        public int ArmorValuePerTier { get; set; }
        public double SpeedBonusPerTier { get; set; }
        public int MaxTier { get; set; }
        public double EnchantmentChance { get; set; }
        public string Description { get; set; } = "";
    }

    public class ClassBalanceConfig
    {
        public ClassMultipliers Barbarian { get; set; } = new();
        public ClassMultipliers Warrior { get; set; } = new();
        public ClassMultipliers Rogue { get; set; } = new();
        public ClassMultipliers Wizard { get; set; } = new();
        public string Description { get; set; } = "";
    }

    public class ClassMultipliers
    {
        public double HealthMultiplier { get; set; }
        public double DamageMultiplier { get; set; }
        public double SpeedMultiplier { get; set; }
    }

    public class DifficultySettingsConfig
    {
        public DifficultyLevel Easy { get; set; } = new();
        public DifficultyLevel Normal { get; set; } = new();
        public DifficultyLevel Hard { get; set; } = new();
        public string Description { get; set; } = "";
    }

    public class DifficultyLevel
    {
        public double EnemyHealthMultiplier { get; set; }
        public double EnemyDamageMultiplier { get; set; }
        public double XPMultiplier { get; set; }
        public double LootMultiplier { get; set; }
    }

    public class UICustomizationConfig
    {
        public string MenuSeparator { get; set; } = "";
        public string SubMenuSeparator { get; set; } = "";
        public string InvalidChoiceMessage { get; set; } = "";
        public string PressAnyKeyMessage { get; set; } = "";
        public RarityPrefixesConfig RarityPrefixes { get; set; } = new();
        public ActionNamesConfig ActionNames { get; set; } = new();
        public ErrorMessagesConfig ErrorMessages { get; set; } = new();
        public DebugMessagesConfig DebugMessages { get; set; } = new();
    }

    public class RarityPrefixesConfig
    {
        public string Common { get; set; } = "";
        public string Uncommon { get; set; } = "";
        public string Rare { get; set; } = "";
        public string Epic { get; set; } = "";
        public string Legendary { get; set; } = "";
    }

    public class ActionNamesConfig
    {
        public string BasicAttackName { get; set; } = "";
        public string DefaultActionDescription { get; set; } = "";
    }

    public class ErrorMessagesConfig
    {
        public string FileNotFoundError { get; set; } = "";
        public string JsonDeserializationError { get; set; } = "";
        public string InvalidDataError { get; set; } = "";
        public string SaveError { get; set; } = "";
        public string LoadError { get; set; } = "";
    }

    public class DebugMessagesConfig
    {
        public string DebugPrefix { get; set; } = "";
        public string WarningPrefix { get; set; } = "";
        public string ErrorPrefix { get; set; } = "";
        public string InfoPrefix { get; set; } = "";
    }


    public class BalanceValidationConfig
    {
        public bool EnableBalanceChecks { get; set; }
        public double MaxDamageVariance { get; set; }
        public double MinCombatDuration { get; set; }
        public double MaxCombatDuration { get; set; }
        public List<double> TargetDPSRange { get; set; } = new();
        public bool ValidateEnemyScaling { get; set; }
        public bool ValidateLootRates { get; set; }
        public bool ValidateXPProgression { get; set; }
        public string Description { get; set; } = "";
    }

    public class FormulaLibraryConfig
    {
        public string ArmorReductionFormula { get; set; } = "";
        public string CriticalHitFormula { get; set; } = "";
        public string ComboAmplificationFormula { get; set; } = "";
        public string StatusEffectDamageFormula { get; set; } = "";
        public string EnvironmentalDamageFormula { get; set; } = "";
        public string Description { get; set; } = "";
    }

}
