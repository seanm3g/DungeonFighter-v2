using System;
using RPGGame.Config;
using RPGGame.Utils;

namespace RPGGame.Tuning
{
    public static class AdjustmentExecutor
    {
        public static void ApplyConfiguration(GameConfiguration config)
        {
            var current = GameConfiguration.Instance;
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

        public static bool AdjustGlobalEnemyMultiplier(string multiplierName, double value)
        {
            try
            {
                UndoRedoManager.SaveState();
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
                        ScrollDebugLogger.Log($"AdjustmentExecutor: Unknown multiplier '{multiplierName}'");
                        return false;
                }

                ScrollDebugLogger.Log($"AdjustmentExecutor: Set {multiplierName} to {value}");
                return true;
            }
            catch (Exception ex)
            {
                ScrollDebugLogger.Log($"AdjustmentExecutor: Error adjusting multiplier: {ex.Message}");
                return false;
            }
        }

        public static bool AdjustArchetype(string archetypeName, string statName, double value)
        {
            try
            {
                UndoRedoManager.SaveState();
                var config = GameConfiguration.Instance;
                var archetypes = config.EnemySystem.Archetypes;

                if (!archetypes.ContainsKey(archetypeName))
                {
                    ScrollDebugLogger.Log($"AdjustmentExecutor: Unknown archetype '{archetypeName}'");
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
                        ScrollDebugLogger.Log($"AdjustmentExecutor: Unknown stat '{statName}'");
                        return false;
                }

                ScrollDebugLogger.Log($"AdjustmentExecutor: Set {archetypeName}.{statName} to {value}");
                return true;
            }
            catch (Exception ex)
            {
                ScrollDebugLogger.Log($"AdjustmentExecutor: Error adjusting archetype: {ex.Message}");
                return false;
            }
        }

        public static bool AdjustWeaponScaling(string weaponType, string parameter, double value)
        {
            try
            {
                UndoRedoManager.SaveState();
                var config = GameConfiguration.Instance;
                
                if (config.WeaponScaling == null)
                {
                    config.WeaponScaling = new WeaponScalingConfig();
                }

                if (parameter.ToLower() == "damagemultiplier" || parameter.ToLower() == "damage")
                {
                    config.WeaponScaling.GlobalDamageMultiplier = value;
                    ScrollDebugLogger.Log($"AdjustmentExecutor: Set weapon global damage multiplier to {value}");
                    return true;
                }

                ScrollDebugLogger.Log($"AdjustmentExecutor: Weapon-specific scaling requires modifying Weapons.json");
                return false;
            }
            catch (Exception ex)
            {
                ScrollDebugLogger.Log($"AdjustmentExecutor: Error adjusting weapon scaling: {ex.Message}");
                return false;
            }
        }

        public static bool AdjustPlayerBaseAttribute(string attributeName, int value)
        {
            try
            {
                UndoRedoManager.SaveState();
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
                        ScrollDebugLogger.Log($"AdjustmentExecutor: Unknown attribute '{attributeName}'");
                        return false;
                }

                ScrollDebugLogger.Log($"AdjustmentExecutor: Set player base {attributeName} to {value}");
                return true;
            }
            catch (Exception ex)
            {
                ScrollDebugLogger.Log($"AdjustmentExecutor: Error adjusting player attribute: {ex.Message}");
                return false;
            }
        }

        public static bool AdjustPlayerAttributesPerLevel(int value)
        {
            try
            {
                UndoRedoManager.SaveState();
                var config = GameConfiguration.Instance;
                config.Attributes.PlayerAttributesPerLevel = value;
                ScrollDebugLogger.Log($"AdjustmentExecutor: Set player attributes per level to {value}");
                return true;
            }
            catch (Exception ex)
            {
                ScrollDebugLogger.Log($"AdjustmentExecutor: Error adjusting player attributes per level: {ex.Message}");
                return false;
            }
        }

        public static bool AdjustPlayerBaseHealth(int value)
        {
            try
            {
                UndoRedoManager.SaveState();
                var config = GameConfiguration.Instance;
                config.Character.PlayerBaseHealth = value;
                ScrollDebugLogger.Log($"AdjustmentExecutor: Set player base health to {value}");
                return true;
            }
            catch (Exception ex)
            {
                ScrollDebugLogger.Log($"AdjustmentExecutor: Error adjusting player base health: {ex.Message}");
                return false;
            }
        }

        public static bool AdjustPlayerHealthPerLevel(int value)
        {
            try
            {
                UndoRedoManager.SaveState();
                var config = GameConfiguration.Instance;
                config.Character.HealthPerLevel = value;
                ScrollDebugLogger.Log($"AdjustmentExecutor: Set player health per level to {value}");
                return true;
            }
            catch (Exception ex)
            {
                ScrollDebugLogger.Log($"AdjustmentExecutor: Error adjusting player health per level: {ex.Message}");
                return false;
            }
        }

        public static bool AdjustEnemyBaselineStat(string statName, double value)
        {
            try
            {
                UndoRedoManager.SaveState();
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
                        ScrollDebugLogger.Log($"AdjustmentExecutor: Unknown stat '{statName}'");
                        return false;
                }

                ScrollDebugLogger.Log($"AdjustmentExecutor: Set enemy baseline {statName} to {value}");
                return true;
            }
            catch (Exception ex)
            {
                ScrollDebugLogger.Log($"AdjustmentExecutor: Error adjusting enemy baseline stat: {ex.Message}");
                return false;
            }
        }

        public static bool AdjustEnemyScalingPerLevel(string statName, double value)
        {
            try
            {
                UndoRedoManager.SaveState();
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
                        ScrollDebugLogger.Log($"AdjustmentExecutor: Unknown scaling stat '{statName}'");
                        return false;
                }

                ScrollDebugLogger.Log($"AdjustmentExecutor: Set enemy scaling per level {statName} to {value}");
                return true;
            }
            catch (Exception ex)
            {
                ScrollDebugLogger.Log($"AdjustmentExecutor: Error adjusting enemy scaling: {ex.Message}");
                return false;
            }
        }

        public static bool ApplyPreset(string presetName)
        {
            try
            {
                UndoRedoManager.SaveState();
                var config = GameConfiguration.Instance;

                switch (presetName.ToLower())
                {
                    case "aggressive_enemies":
                    case "hard_mode":
                        config.EnemySystem.GlobalMultipliers.HealthMultiplier = 1.2;
                        config.EnemySystem.GlobalMultipliers.DamageMultiplier = 1.2;
                        ScrollDebugLogger.Log("AdjustmentExecutor: Applied 'Aggressive Enemies' preset");
                        return true;

                    case "tanky_enemies":
                        config.EnemySystem.GlobalMultipliers.HealthMultiplier = 1.5;
                        config.EnemySystem.GlobalMultipliers.ArmorMultiplier = 1.3;
                        config.EnemySystem.GlobalMultipliers.DamageMultiplier = 0.9;
                        ScrollDebugLogger.Log("AdjustmentExecutor: Applied 'Tanky Enemies' preset");
                        return true;

                    case "fast_enemies":
                        config.EnemySystem.GlobalMultipliers.SpeedMultiplier = 1.3;
                        config.EnemySystem.GlobalMultipliers.HealthMultiplier = 0.9;
                        ScrollDebugLogger.Log("AdjustmentExecutor: Applied 'Fast Enemies' preset");
                        return true;

                    case "baseline":
                    case "default":
                        config.EnemySystem.GlobalMultipliers.HealthMultiplier = 1.0;
                        config.EnemySystem.GlobalMultipliers.DamageMultiplier = 1.0;
                        config.EnemySystem.GlobalMultipliers.ArmorMultiplier = 1.0;
                        config.EnemySystem.GlobalMultipliers.SpeedMultiplier = 1.0;
                        ScrollDebugLogger.Log("AdjustmentExecutor: Applied 'Baseline' preset");
                        return true;

                    default:
                        ScrollDebugLogger.Log($"AdjustmentExecutor: Unknown preset '{presetName}'");
                        return false;
                }
            }
            catch (Exception ex)
            {
                ScrollDebugLogger.Log($"AdjustmentExecutor: Error applying preset: {ex.Message}");
                return false;
            }
        }
    }
}

