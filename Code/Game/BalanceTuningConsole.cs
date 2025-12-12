using System;
using System.Collections.Generic;
using System.Linq;
using RPGGame.Config;
using RPGGame.Utils;

namespace RPGGame
{
    /// <summary>
    /// Interactive console for real-time balance tuning
    /// </summary>
    public static class BalanceTuningConsole
    {
        private static readonly Stack<GameConfiguration> UndoStack = new();
        private static readonly Stack<GameConfiguration> RedoStack = new();
        private const int MaxUndoHistory = 10;

        /// <summary>
        /// Save current state for undo
        /// </summary>
        private static void SaveState()
        {
            // Create a deep copy of current config
            var config = GameConfiguration.Instance;
            var json = System.Text.Json.JsonSerializer.Serialize(config);
            var copy = System.Text.Json.JsonSerializer.Deserialize<GameConfiguration>(json);
            
            if (copy != null)
            {
                UndoStack.Push(copy);
                if (UndoStack.Count > MaxUndoHistory)
                {
                    // Remove oldest
                    var list = UndoStack.ToList();
                    list.RemoveAt(0);
                    UndoStack.Clear();
                    foreach (var item in list)
                    {
                        UndoStack.Push(item);
                    }
                }
                RedoStack.Clear(); // Clear redo when new action taken
            }
        }

        /// <summary>
        /// Adjust global enemy multiplier
        /// </summary>
        public static bool AdjustGlobalEnemyMultiplier(string multiplierName, double value)
        {
            try
            {
                SaveState();
                var config = GameConfiguration.Instance;
                var multipliers = config.EnemySystem.GlobalMultipliers;

                switch (multiplierName.ToLower())
                {
                    case "healthmultiplier":
                    case "health":
                        multipliers.HealthMultiplier = value;
                        break;
                    case "damagemultiplier":
                    case "damage":
                        multipliers.DamageMultiplier = value;
                        break;
                    case "armormultiplier":
                    case "armor":
                        multipliers.ArmorMultiplier = value;
                        break;
                    case "speedmultiplier":
                    case "speed":
                        multipliers.SpeedMultiplier = value;
                        break;
                    default:
                        ScrollDebugLogger.Log($"BalanceTuningConsole: Unknown multiplier '{multiplierName}'");
                        return false;
                }

                ScrollDebugLogger.Log($"BalanceTuningConsole: Set {multiplierName} to {value}");
                return true;
            }
            catch (Exception ex)
            {
                ScrollDebugLogger.Log($"BalanceTuningConsole: Error adjusting multiplier: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Adjust archetype multiplier
        /// </summary>
        public static bool AdjustArchetype(string archetypeName, string statName, double value)
        {
            try
            {
                SaveState();
                var config = GameConfiguration.Instance;
                var archetypes = config.EnemySystem.Archetypes;

                if (!archetypes.ContainsKey(archetypeName))
                {
                    ScrollDebugLogger.Log($"BalanceTuningConsole: Unknown archetype '{archetypeName}'");
                    return false;
                }

                var archetype = archetypes[archetypeName];
                switch (statName.ToLower())
                {
                    case "health":
                        archetype.Health = value;
                        break;
                    case "strength":
                        archetype.Strength = value;
                        break;
                    case "agility":
                        archetype.Agility = value;
                        break;
                    case "technique":
                        archetype.Technique = value;
                        break;
                    case "intelligence":
                        archetype.Intelligence = value;
                        break;
                    case "armor":
                        archetype.Armor = value;
                        break;
                    default:
                        ScrollDebugLogger.Log($"BalanceTuningConsole: Unknown stat '{statName}'");
                        return false;
                }

                ScrollDebugLogger.Log($"BalanceTuningConsole: Set {archetypeName}.{statName} to {value}");
                return true;
            }
            catch (Exception ex)
            {
                ScrollDebugLogger.Log($"BalanceTuningConsole: Error adjusting archetype: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Adjust enemy override (requires modifying Enemies.json directly)
        /// </summary>
        public static bool AdjustEnemyOverride(string enemyName, string statName, double? value)
        {
            // Note: This would require loading/modifying Enemies.json
            // For now, just log that this needs to be done manually
            ScrollDebugLogger.Log($"BalanceTuningConsole: Enemy overrides must be adjusted in Enemies.json for '{enemyName}'");
            return false;
        }

        /// <summary>
        /// Adjust weapon scaling
        /// </summary>
        public static bool AdjustWeaponScaling(string weaponType, string parameter, double value)
        {
            try
            {
                SaveState();
                var config = GameConfiguration.Instance;
                
                if (config.WeaponScaling == null)
                {
                    config.WeaponScaling = new WeaponScalingConfig();
                }

                if (parameter.ToLower() == "damagemultiplier" || parameter.ToLower() == "damage")
                {
                    config.WeaponScaling.GlobalDamageMultiplier = value;
                    ScrollDebugLogger.Log($"BalanceTuningConsole: Set weapon global damage multiplier to {value}");
                    return true;
                }

                // Weapon-specific adjustments would require modifying Weapons.json
                ScrollDebugLogger.Log($"BalanceTuningConsole: Weapon-specific scaling requires modifying Weapons.json");
                return false;
            }
            catch (Exception ex)
            {
                ScrollDebugLogger.Log($"BalanceTuningConsole: Error adjusting weapon scaling: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Save current configuration as a patch
        /// </summary>
        public static bool SavePatch(string patchName, string author, string description, 
            string version = "1.0", List<string>? tags = null, 
            BalancePatchManager.TestResults? testResults = null)
        {
            try
            {
                var patch = BalancePatchManager.CreatePatch(patchName, author, description, version, tags, testResults);
                return BalancePatchManager.SavePatch(patch);
            }
            catch (Exception ex)
            {
                ScrollDebugLogger.Log($"BalanceTuningConsole: Error saving patch: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Export patch to external location
        /// </summary>
        public static bool ExportPatch(string patchName, string exportPath)
        {
            try
            {
                var patch = BalancePatchManager.GetPatch(patchName);
                if (patch == null)
                {
                    // Try to find by name
                    var patches = BalancePatchManager.ListPatches();
                    patch = patches.FirstOrDefault(p => p.PatchMetadata.Name == patchName);
                }

                if (patch == null)
                {
                    ScrollDebugLogger.Log($"BalanceTuningConsole: Patch '{patchName}' not found");
                    return false;
                }

                return BalancePatchManager.ExportPatch(patch, exportPath);
            }
            catch (Exception ex)
            {
                ScrollDebugLogger.Log($"BalanceTuningConsole: Error exporting patch: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Import patch from external location
        /// </summary>
        public static bool ImportPatch(string filePath)
        {
            try
            {
                return BalancePatchManager.ImportPatch(filePath);
            }
            catch (Exception ex)
            {
                ScrollDebugLogger.Log($"BalanceTuningConsole: Error importing patch: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Load and apply a patch
        /// </summary>
        public static bool LoadPatch(string patchId)
        {
            try
            {
                SaveState();
                var patch = BalancePatchManager.GetPatch(patchId);
                if (patch == null)
                {
                    ScrollDebugLogger.Log($"BalanceTuningConsole: Patch '{patchId}' not found");
                    return false;
                }

                return BalancePatchManager.ApplyPatch(patch);
            }
            catch (Exception ex)
            {
                ScrollDebugLogger.Log($"BalanceTuningConsole: Error loading patch: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Save current configuration as a profile
        /// </summary>
        public static bool SaveProfile(string profileName, string description = "", string notes = "")
        {
            try
            {
                var profile = TuningProfileManager.CreateProfile(profileName, description, notes);
                return TuningProfileManager.SaveProfile(profile);
            }
            catch (Exception ex)
            {
                ScrollDebugLogger.Log($"BalanceTuningConsole: Error saving profile: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Load and apply a profile
        /// </summary>
        public static bool LoadProfile(string profileName)
        {
            try
            {
                SaveState();
                var profile = TuningProfileManager.LoadProfile(profileName);
                if (profile == null)
                {
                    ScrollDebugLogger.Log($"BalanceTuningConsole: Profile '{profileName}' not found");
                    return false;
                }

                return TuningProfileManager.ApplyProfile(profile);
            }
            catch (Exception ex)
            {
                ScrollDebugLogger.Log($"BalanceTuningConsole: Error loading profile: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Undo last change
        /// </summary>
        public static bool Undo()
        {
            if (UndoStack.Count == 0)
            {
                return false;
            }

            try
            {
                var current = GameConfiguration.Instance;
                var json = System.Text.Json.JsonSerializer.Serialize(current);
                var copy = System.Text.Json.JsonSerializer.Deserialize<GameConfiguration>(json);
                if (copy != null)
                {
                    RedoStack.Push(copy);
                }

                var previous = UndoStack.Pop();
                ApplyConfiguration(previous);
                ScrollDebugLogger.Log("BalanceTuningConsole: Undo performed");
                return true;
            }
            catch (Exception ex)
            {
                ScrollDebugLogger.Log($"BalanceTuningConsole: Error undoing: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Redo last undone change
        /// </summary>
        public static bool Redo()
        {
            if (RedoStack.Count == 0)
            {
                return false;
            }

            try
            {
                var current = GameConfiguration.Instance;
                var json = System.Text.Json.JsonSerializer.Serialize(current);
                var copy = System.Text.Json.JsonSerializer.Deserialize<GameConfiguration>(json);
                if (copy != null)
                {
                    UndoStack.Push(copy);
                }

                var next = RedoStack.Pop();
                ApplyConfiguration(next);
                ScrollDebugLogger.Log("BalanceTuningConsole: Redo performed");
                return true;
            }
            catch (Exception ex)
            {
                ScrollDebugLogger.Log($"BalanceTuningConsole: Error redoing: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Apply configuration (helper method)
        /// </summary>
        private static void ApplyConfiguration(GameConfiguration config)
        {
            var current = GameConfiguration.Instance;
            // Copy all properties (same as in TuningProfileManager)
            current.Character = config.Character;
            current.Attributes = config.Attributes;
            current.Combat = config.Combat;
            current.CombatBalance = config.CombatBalance;
            current.RollSystem = config.RollSystem;
            current.EnemySystem = config.EnemySystem;
            current.WeaponScaling = config.WeaponScaling;
            current.EquipmentScaling = config.EquipmentScaling;
            current.Progression = config.Progression;
            current.StatusEffects = config.StatusEffects;
            current.ComboSystem = config.ComboSystem;
            current.LootSystem = config.LootSystem;
            current.DungeonScaling = config.DungeonScaling;
            current.DungeonGeneration = config.DungeonGeneration;
            current.ModificationRarity = config.ModificationRarity;
            current.GameSpeed = config.GameSpeed;
            current.GameData = config.GameData;
            current.Debug = config.Debug;
            current.BalanceAnalysis = config.BalanceAnalysis;
            current.BalanceValidation = config.BalanceValidation;
            current.DifficultySettings = config.DifficultySettings;
            current.UICustomization = config.UICustomization;
        }

        /// <summary>
        /// Save current configuration to file
        /// </summary>
        public static bool SaveConfiguration()
        {
            try
            {
                return GameConfiguration.Instance.SaveToFile();
            }
            catch (Exception ex)
            {
                ScrollDebugLogger.Log($"BalanceTuningConsole: Error saving configuration: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Adjust player base attributes
        /// </summary>
        public static bool AdjustPlayerBaseAttribute(string attributeName, int value)
        {
            try
            {
                SaveState();
                var config = GameConfiguration.Instance;
                var baseAttributes = config.Attributes.PlayerBaseAttributes;

                switch (attributeName.ToLower())
                {
                    case "strength":
                        baseAttributes.Strength = value;
                        break;
                    case "agility":
                        baseAttributes.Agility = value;
                        break;
                    case "technique":
                        baseAttributes.Technique = value;
                        break;
                    case "intelligence":
                        baseAttributes.Intelligence = value;
                        break;
                    default:
                        ScrollDebugLogger.Log($"BalanceTuningConsole: Unknown attribute '{attributeName}'");
                        return false;
                }

                ScrollDebugLogger.Log($"BalanceTuningConsole: Set player base {attributeName} to {value}");
                return true;
            }
            catch (Exception ex)
            {
                ScrollDebugLogger.Log($"BalanceTuningConsole: Error adjusting player attribute: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Adjust player attributes per level
        /// </summary>
        public static bool AdjustPlayerAttributesPerLevel(int value)
        {
            try
            {
                SaveState();
                var config = GameConfiguration.Instance;
                config.Attributes.PlayerAttributesPerLevel = value;
                ScrollDebugLogger.Log($"BalanceTuningConsole: Set player attributes per level to {value}");
                return true;
            }
            catch (Exception ex)
            {
                ScrollDebugLogger.Log($"BalanceTuningConsole: Error adjusting player attributes per level: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Adjust player base health
        /// </summary>
        public static bool AdjustPlayerBaseHealth(int value)
        {
            try
            {
                SaveState();
                var config = GameConfiguration.Instance;
                config.Character.PlayerBaseHealth = value;
                ScrollDebugLogger.Log($"BalanceTuningConsole: Set player base health to {value}");
                return true;
            }
            catch (Exception ex)
            {
                ScrollDebugLogger.Log($"BalanceTuningConsole: Error adjusting player base health: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Adjust player health per level
        /// </summary>
        public static bool AdjustPlayerHealthPerLevel(int value)
        {
            try
            {
                SaveState();
                var config = GameConfiguration.Instance;
                config.Character.HealthPerLevel = value;
                ScrollDebugLogger.Log($"BalanceTuningConsole: Set player health per level to {value}");
                return true;
            }
            catch (Exception ex)
            {
                ScrollDebugLogger.Log($"BalanceTuningConsole: Error adjusting player health per level: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Adjust enemy baseline stats (affects all enemies)
        /// </summary>
        public static bool AdjustEnemyBaselineStat(string statName, double value)
        {
            try
            {
                SaveState();
                var config = GameConfiguration.Instance;
                var baselineStats = config.EnemySystem.BaselineStats;

                switch (statName.ToLower())
                {
                    case "health":
                        baselineStats.Health = (int)value;
                        break;
                    case "strength":
                        baselineStats.Strength = (int)value;
                        break;
                    case "agility":
                        baselineStats.Agility = (int)value;
                        break;
                    case "technique":
                        baselineStats.Technique = (int)value;
                        break;
                    case "intelligence":
                        baselineStats.Intelligence = (int)value;
                        break;
                    case "armor":
                        baselineStats.Armor = (int)value;
                        break;
                    default:
                        ScrollDebugLogger.Log($"BalanceTuningConsole: Unknown stat '{statName}'");
                        return false;
                }

                ScrollDebugLogger.Log($"BalanceTuningConsole: Set enemy baseline {statName} to {value}");
                return true;
            }
            catch (Exception ex)
            {
                ScrollDebugLogger.Log($"BalanceTuningConsole: Error adjusting enemy baseline stat: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Adjust enemy scaling per level
        /// </summary>
        public static bool AdjustEnemyScalingPerLevel(string statName, double value)
        {
            try
            {
                SaveState();
                var config = GameConfiguration.Instance;
                var scaling = config.EnemySystem.ScalingPerLevel;

                switch (statName.ToLower())
                {
                    case "health":
                        scaling.Health = (int)value;
                        break;
                    case "attributes":
                        scaling.Attributes = (int)value;
                        break;
                    case "armor":
                        scaling.Armor = value;
                        break;
                    default:
                        ScrollDebugLogger.Log($"BalanceTuningConsole: Unknown scaling stat '{statName}'");
                        return false;
                }

                ScrollDebugLogger.Log($"BalanceTuningConsole: Set enemy scaling per level {statName} to {value}");
                return true;
            }
            catch (Exception ex)
            {
                ScrollDebugLogger.Log($"BalanceTuningConsole: Error adjusting enemy scaling: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Apply quick preset
        /// </summary>
        public static bool ApplyPreset(string presetName)
        {
            try
            {
                SaveState();
                var config = GameConfiguration.Instance;

                switch (presetName.ToLower())
                {
                    case "aggressive_enemies":
                    case "hard_mode":
                        config.EnemySystem.GlobalMultipliers.HealthMultiplier = 1.2;
                        config.EnemySystem.GlobalMultipliers.DamageMultiplier = 1.2;
                        ScrollDebugLogger.Log("BalanceTuningConsole: Applied 'Aggressive Enemies' preset");
                        return true;

                    case "tanky_enemies":
                        config.EnemySystem.GlobalMultipliers.HealthMultiplier = 1.5;
                        config.EnemySystem.GlobalMultipliers.ArmorMultiplier = 1.3;
                        config.EnemySystem.GlobalMultipliers.DamageMultiplier = 0.9;
                        ScrollDebugLogger.Log("BalanceTuningConsole: Applied 'Tanky Enemies' preset");
                        return true;

                    case "fast_enemies":
                        config.EnemySystem.GlobalMultipliers.SpeedMultiplier = 1.3;
                        config.EnemySystem.GlobalMultipliers.HealthMultiplier = 0.9;
                        ScrollDebugLogger.Log("BalanceTuningConsole: Applied 'Fast Enemies' preset");
                        return true;

                    case "baseline":
                    case "default":
                        config.EnemySystem.GlobalMultipliers.HealthMultiplier = 1.0;
                        config.EnemySystem.GlobalMultipliers.DamageMultiplier = 1.0;
                        config.EnemySystem.GlobalMultipliers.ArmorMultiplier = 1.0;
                        config.EnemySystem.GlobalMultipliers.SpeedMultiplier = 1.0;
                        ScrollDebugLogger.Log("BalanceTuningConsole: Applied 'Baseline' preset");
                        return true;

                    default:
                        ScrollDebugLogger.Log($"BalanceTuningConsole: Unknown preset '{presetName}'");
                        return false;
                }
            }
            catch (Exception ex)
            {
                ScrollDebugLogger.Log($"BalanceTuningConsole: Error applying preset: {ex.Message}");
                return false;
            }
        }
    }
}

