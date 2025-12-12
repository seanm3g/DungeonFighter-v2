using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;

namespace RPGGame
{
    /// <summary>
    /// Main game configuration class that aggregates all configuration domains
    /// Refactored from 1000+ lines to a clean orchestrator class
    /// </summary>
    public class GameConfiguration
    {
        private static GameConfiguration? _instance;
        private static readonly object _lock = new object();
        
        // Character-related configurations
        public CharacterConfig Character { get; set; } = new();
        public AttributesConfig Attributes { get; set; } = new();
        public ProgressionConfig Progression { get; set; } = new();
        public XPRewardsConfig XPRewards { get; set; } = new();
        public ExperienceSystemConfig ExperienceSystem { get; set; } = new();
        public ClassBalanceConfig ClassBalance { get; set; } = new();

        // Combat-related configurations
        public CombatConfig Combat { get; set; } = new();
        public CombatBalanceConfig CombatBalance { get; set; } = new();
        public RollSystemConfig RollSystem { get; set; } = new();
        public ComboSystemConfig ComboSystem { get; set; } = new();
        public PoisonConfig Poison { get; set; } = new();
        public StatusEffectsConfig StatusEffects { get; set; } = new();

        // Enemy-related configurations (Legacy - kept for backward compatibility)
        public EnemyScalingConfig EnemyScaling { get; set; } = new();
        public EnemyBalanceConfig EnemyBalance { get; set; } = new();
        public EnemyBaselineConfig EnemyBaseline { get; set; } = new();
        public EnemyArchetypesConfig EnemyArchetypes { get; set; } = new();
        public EnemyDPSConfig? EnemyDPS { get; set; } = new();
        
        // Unified enemy system configuration (New improved system)
        public EnemySystemConfig EnemySystem { get; set; } = new();

        // Item-related configurations
        public ItemScalingConfig? ItemScaling { get; set; } = new();
        public WeaponScalingConfig? WeaponScaling { get; set; } = new();
        public RarityScalingConfig? RarityScaling { get; set; } = new();
        public EquipmentScalingConfig EquipmentScaling { get; set; } = new();
        public LootSystemConfig LootSystem { get; set; } = new();

        // Dungeon-related configurations
        public DungeonScalingConfig DungeonScaling { get; set; } = new();
        public DungeonGenerationConfig DungeonGeneration { get; set; } = new();

        // System-related configurations
        public GameSpeedConfig GameSpeed { get; set; } = new();
        public GameDataConfig GameData { get; set; } = new();
        public DebugConfig Debug { get; set; } = new();
        public BalanceAnalysisConfig BalanceAnalysis { get; set; } = new();
        public BalanceValidationConfig BalanceValidation { get; set; } = new();
        public BalanceTuningGoalsConfig BalanceTuningGoals { get; set; } = new();
        public DifficultySettingsConfig DifficultySettings { get; set; } = new();
        public ProgressionCurvesConfig? ProgressionCurves { get; set; } = new();

        // UI-related configurations
        public UICustomizationConfig UICustomization { get; set; } = new();
        
        // Modification-related configurations
        public ModificationRarityConfig ModificationRarity { get; set; } = new();

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
                        // Character-related configurations
                        Character = config.Character;
                        Attributes = config.Attributes;
                        Progression = config.Progression;
                        XPRewards = config.XPRewards;
                        ExperienceSystem = config.ExperienceSystem;
                        ClassBalance = config.ClassBalance;

                        // Combat-related configurations
                        Combat = config.Combat;
                        CombatBalance = config.CombatBalance;
                        RollSystem = config.RollSystem;
                        ComboSystem = config.ComboSystem;
                        Poison = config.Poison;
                        StatusEffects = config.StatusEffects;

                        // Enemy-related configurations (Legacy)
                        EnemyScaling = config.EnemyScaling;
                        EnemyBalance = config.EnemyBalance;
                        EnemyBaseline = config.EnemyBaseline;
                        EnemyArchetypes = config.EnemyArchetypes;
                        EnemyDPS = config.EnemyDPS;
                        
                        // Unified enemy system configuration (New improved system)
                        EnemySystem = config.EnemySystem;

                        // Item-related configurations
                        ItemScaling = config.ItemScaling;
                        WeaponScaling = config.WeaponScaling;
                        RarityScaling = config.RarityScaling;
                        EquipmentScaling = config.EquipmentScaling;
                        LootSystem = config.LootSystem;

                        // Dungeon-related configurations
                        DungeonScaling = config.DungeonScaling;
                        DungeonGeneration = config.DungeonGeneration;

                        // System-related configurations
                        GameSpeed = config.GameSpeed;
                        GameData = config.GameData;
                        Debug = config.Debug;
                        BalanceAnalysis = config.BalanceAnalysis;
                        BalanceValidation = config.BalanceValidation;
                        DifficultySettings = config.DifficultySettings;
                        ProgressionCurves = config.ProgressionCurves;

                        // UI-related configurations
                        UICustomization = config.UICustomization;
                        
                        // Modification-related configurations
                        ModificationRarity = config.ModificationRarity;
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

        /// <summary>
        /// Save current configuration to TuningConfig.json
        /// </summary>
        public bool SaveToFile()
        {
            try
            {
                string executableDir = AppDomain.CurrentDomain.BaseDirectory;
                string currentDir = Directory.GetCurrentDirectory();
                
                string[] possiblePaths = {
                    Path.Combine(executableDir, "GameData", "TuningConfig.json"),
                    Path.Combine(executableDir, "..", "GameData", "TuningConfig.json"),
                    Path.Combine(executableDir, "..", "..", "GameData", "TuningConfig.json"),
                    Path.Combine(currentDir, "GameData", "TuningConfig.json"),
                    Path.Combine(currentDir, "..", "GameData", "TuningConfig.json"),
                    Path.Combine(currentDir, "..", "..", "GameData", "TuningConfig.json"),
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
                
                // If no existing file found, use default location
                if (configPath == null)
                {
                    configPath = Path.Combine("GameData", "TuningConfig.json");
                    // Ensure directory exists
                    string? dir = Path.GetDirectoryName(configPath);
                    if (dir != null && !Directory.Exists(dir))
                    {
                        Directory.CreateDirectory(dir);
                    }
                }
                
                var options = new JsonSerializerOptions
                {
                    WriteIndented = true,
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                };
                
                string json = JsonSerializer.Serialize(this, options);
                File.WriteAllText(configPath, json);
                
                return true;
            }
            catch (Exception ex)
            {
                UIManager.WriteSystemLine($"Error saving tuning config: {ex.Message}");
                return false;
            }
        }
    }
}