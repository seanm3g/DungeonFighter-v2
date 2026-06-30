using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using RPGGame.Config;

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
        /// <summary>Absolute path to TuningConfig.json once discovered or created; keeps Save and Load on the same file.</summary>
        private static string? _cachedTuningConfigPath;
        
        // Character-related configurations
        public CharacterConfig Character { get; set; } = new();
        public AttributesConfig Attributes { get; set; } = new();
        public ProgressionConfig Progression { get; set; } = new();
        public XPRewardsConfig XPRewards { get; set; } = new();
        public ExperienceSystemConfig ExperienceSystem { get; set; } = new();
        public ClassBalanceConfig ClassBalance { get; set; } = new();
        public ClassPresentationConfig ClassPresentation { get; set; } = new();
        public EarlyGameConfig EarlyGame { get; set; } = new();

        /// <summary>CLASS ACTIONS sheet / ClassActions.json — not written by <see cref="SaveToFile"/>.</summary>
        [JsonIgnore]
        public ClassActionsUnlockConfig ClassActionsUnlock { get; set; } = ClassActionsUnlockConfig.CreateBuiltInDefaults();

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
        /// <summary>Per-rarity affix min/max/chance tuning for item generation (see <see cref="ItemAffixByRaritySettings"/>).</summary>
        public ItemAffixByRaritySettings ItemAffixByRarity { get; set; } = new();
        public StartingGearConfig StartingGear { get; set; } = new();

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

        private static IEnumerable<string> TuningConfigPathCandidates()
        {
            string executableDir = AppDomain.CurrentDomain.BaseDirectory;
            string currentDir = Directory.GetCurrentDirectory();
            yield return Path.Combine(executableDir, "GameData", "TuningConfig.json");
            yield return Path.Combine(executableDir, "..", "GameData", "TuningConfig.json");
            yield return Path.Combine(executableDir, "..", "..", "GameData", "TuningConfig.json");
            yield return Path.Combine(currentDir, "GameData", "TuningConfig.json");
            yield return Path.Combine(currentDir, "..", "GameData", "TuningConfig.json");
            yield return Path.Combine(currentDir, "..", "..", "GameData", "TuningConfig.json");
            yield return Path.Combine("GameData", "TuningConfig.json");
            yield return Path.Combine("..", "GameData", "TuningConfig.json");
            yield return Path.Combine("..", "..", "GameData", "TuningConfig.json");
        }

        /// <summary>Resolves the active balance patch file (absolute path). Caches so Save writes the same file Load reads.</summary>
        private static string ResolveTuningConfigPathForRead()
        {
            if (_cachedTuningConfigPath != null && File.Exists(_cachedTuningConfigPath))
                return _cachedTuningConfigPath;

            PatchProfileService.EnsureBootstrapped();
            string patchPath = PatchProfileService.GetActivePatchFilePath(PatchCategory.Balance);
            _cachedTuningConfigPath = Path.GetFullPath(patchPath);
            return _cachedTuningConfigPath;
        }

        /// <summary>Path to write the active balance patch (creates patch folder if needed).</summary>
        private static string ResolveTuningConfigPathForCreate()
        {
            PatchProfileService.EnsureBootstrapped();
            string patchPath = PatchProfileService.GetActivePatchFilePath(PatchCategory.Balance);
            _cachedTuningConfigPath = Path.GetFullPath(patchPath);
            return _cachedTuningConfigPath;
        }

        private void LoadFromFile()
        {
            try
            {
                string configPath = ResolveTuningConfigPathForRead();

                if (File.Exists(configPath))
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
                        EarlyGame = config.EarlyGame;
                        ClassPresentation = config.ClassPresentation != null
                            ? config.ClassPresentation.EnsureNormalized()
                            : new ClassPresentationConfig().EnsureNormalized();

                        // Combat-related configurations
                        Combat = config.Combat;
                        CombatBalance = config.CombatBalance;
                        RollSystem = config.RollSystem;
                        ComboSystem = config.ComboSystem;
                        Poison = config.Poison;
                        StatusEffects = config.StatusEffects;
                        
                        // Migrate status effects from old format to new dictionary format if needed
                        MigrateStatusEffects();

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
                        ItemAffixByRarity = config.ItemAffixByRarity ?? new ItemAffixByRaritySettings();
                        StartingGear = config.StartingGear;

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
                        BalanceTuningGoals = config.BalanceTuningGoals ?? BalanceTuningGoals;

                        // UI-related configurations
                        UICustomization = config.UICustomization;
                        
                        // Modification-related configurations
                        ModificationRarity = config.ModificationRarity;
                    }
                }
                else
                {
                    UIManager.WriteSystemLine($"Warning: balance patch not found at {ResolveTuningConfigPathForRead()}, using default values.");
                }
            }
            catch (Exception ex)
            {
                UIManager.WriteSystemLine($"Error loading tuning config: {ex.Message}");
                UIManager.WriteSystemLine("Using default values");
            }

            // JSON may omit nested objects; restore so sanitizer methods never null-ref.
            Character ??= new CharacterConfig();
            CombatBalance ??= new CombatBalanceConfig();
            RollSystem ??= new RollSystemConfig();
            EnemySystem ??= new EnemySystemConfig();
            LootSystem ??= new LootSystemConfig();
            ItemAffixByRarity ??= new ItemAffixByRaritySettings();
            EquipmentScaling ??= new EquipmentScalingConfig();
            WeaponScaling ??= new WeaponScalingConfig();
            ItemScaling ??= new ItemScalingConfig();
            Progression ??= new ProgressionConfig();
            ClassBalance ??= new ClassBalanceConfig();
            EarlyGame ??= new EarlyGameConfig();
            DungeonScaling ??= new DungeonScalingConfig();
            RarityScaling ??= new RarityScalingConfig();
            RarityScaling.MagicFindScaling ??= new MagicFindScalingConfig();

            Combat.EnsureValidCombatTimingDefaults();
            Combat.EnsureValidCombatCriticalAndDamageDefaults();
            CombatBalance.EnsureValidRollDamageAndCritDefaults();
            CombatBalance.ActionMechanics ??= new ActionMechanicsConfig();
            CombatBalance.ActionMechanics.EnsureValidDefaults();
            RollSystem.EnsureValidDefaultThresholdBands();
            Progression.EnsureValidEnemyXpAndGoldDefaults();
            Attributes.EnsureValidIntelligenceRollBonusDefaults();
            Character.EnsureValidPlayerHealthDefaults();
            Attributes.EnsureValidPlayerBaseStatDefaults();
            ComboSystem.EnsureValidComboAmplifierDefaults();
            EnemySystem.EnsureSanitizedDefaults();
            LootSystem.EnsureSensibleLootDefaults();
            EquipmentScaling.EnsureSensibleDefaults();
            WeaponScaling.EnsureSanitizedDefaults();
            ItemScaling.EnsureSanitizedWeaponScalingDefaults();
            ClassBalance.EnsureNonDegenerateClassMultipliers();
            EarlyGame.EnsureValidDefaults();
            DungeonScaling.EnsureSensibleDefaults();

            ReloadClassActionsUnlockFromDisk();
        }

        private void ReloadClassActionsUnlockFromDisk()
        {
            ClassActionsUnlock = ClassActionsUnlockConfig.TryLoadFromGameDataFile()
                ?? ClassActionsUnlockConfig.CreateBuiltInDefaults();
        }

        public void Reload()
        {
            LoadFromFile();
        }

        /// <summary>
        /// Migrates status effects from old property-based format to new dictionary format
        /// Handles JSON deserialization where effects are stored as direct properties
        /// </summary>
        private void MigrateStatusEffects()
        {
            // If Effects dictionary is empty, try to migrate from legacy properties or JSON
            if (StatusEffects.Effects.Count == 0)
            {
                // Try to read from raw JSON file to handle old format
                try
                {
                    string? configPath = ResolveTuningConfigPathForRead();
                    
                    if (configPath != null)
                    {
                        string jsonContent = File.ReadAllText(configPath);
                        using (var doc = System.Text.Json.JsonDocument.Parse(jsonContent))
                        {
                            if (doc.RootElement.TryGetProperty("statusEffects", out var statusEffectsElement))
                            {
                                // Try to read from "effects" dictionary first (new format)
                                if (statusEffectsElement.TryGetProperty("effects", out var effectsElement))
                                {
                                    foreach (var prop in effectsElement.EnumerateObject())
                                    {
                                        var effectConfig = System.Text.Json.JsonSerializer.Deserialize<StatusEffectConfig>(
                                            prop.Value.GetRawText(),
                                            new System.Text.Json.JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                                        if (effectConfig != null)
                                        {
                                            StatusEffects.Effects[prop.Name] = effectConfig;
                                        }
                                    }
                                }
                                else
                                {
                                    // Old format: effects are direct properties of statusEffects
                                    var knownEffects = new[] { "bleed", "burn", "freeze", "stun", "poison" };
                                    foreach (var effectName in knownEffects)
                                    {
                                        if (statusEffectsElement.TryGetProperty(effectName, out var effectElement))
                                        {
                                            var effectConfig = System.Text.Json.JsonSerializer.Deserialize<StatusEffectConfig>(
                                                effectElement.GetRawText(),
                                                new System.Text.Json.JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                                            if (effectConfig != null)
                                            {
                                                // Capitalize first letter for dictionary key
                                                string key = char.ToUpper(effectName[0]) + effectName.Substring(1);
                                                StatusEffects.Effects[key] = effectConfig;
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
                catch (Exception)
                {
                    // If migration fails, continue to default initialization
                }
                
                // If still empty after migration, initialize defaults
                if (StatusEffects.Effects.Count == 0)
                {
                    StatusEffects.InitializeDefaults();
                }
            }
        }

        /// <summary>Absolute path to the active balance patch file, or null if missing.</summary>
        public static string? TryGetExistingTuningConfigFilePath()
        {
            try
            {
                string path = ResolveTuningConfigPathForRead();
                return File.Exists(path) ? path : null;
            }
            catch
            {
                return null;
            }
        }

        /// <summary>Path to use when writing tuning JSON (existing file, or create path).</summary>
        public static string GetTuningConfigFilePathForWrite() =>
            TryGetExistingTuningConfigFilePath() ?? ResolveTuningConfigPathForCreate();

        /// <summary>
        /// Resets the singleton instance, forcing a reload on next access
        /// Used by tuner to reload configuration after balance adjustments
        /// </summary>
        public static void ResetInstance()
        {
            lock (_lock)
            {
                _instance = null;
                _cachedTuningConfigPath = null;
            }
        }

        /// <summary>
        /// Save current configuration to TuningConfig.json
        /// </summary>
        public bool SaveToFile()
        {
            try
            {
                string configPath = ResolveTuningConfigPathForRead() ?? ResolveTuningConfigPathForCreate();
                
                ClassPresentation = ClassPresentation.EnsureNormalized();

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