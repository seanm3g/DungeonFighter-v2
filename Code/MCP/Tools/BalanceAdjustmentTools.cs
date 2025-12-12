using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using ModelContextProtocol.Server;
using RPGGame;
using RPGGame.Config;
using RPGGame.MCP.Tools;

namespace RPGGame.MCP.Tools
{
    /// <summary>
    /// Balance adjustment tools
    /// </summary>
    public static class BalanceAdjustmentTools
    {
        [McpServerTool(Name = "adjust_global_enemy_multiplier", Title = "Adjust Global Enemy Multiplier")]
        [Description("Adjusts global enemy multipliers (health, damage, armor, speed). Affects all enemies.")]
        public static Task<string> AdjustGlobalEnemyMultiplier(
            [Description("Multiplier name: 'health', 'damage', 'armor', or 'speed'")] string multiplierName,
            [Description("New multiplier value (e.g., 1.2 for 20% increase)")] double value)
        {
            return McpToolExecutor.ExecuteAsync(() =>
            {
                var success = BalanceTuningConsole.AdjustGlobalEnemyMultiplier(multiplierName, value);
                var config = GameConfiguration.Instance;
                var multipliers = config.EnemySystem.GlobalMultipliers;

                return new
                {
                    success = success,
                    message = success ? $"Set {multiplierName} to {value}" : $"Failed to set {multiplierName}",
                    currentMultipliers = new
                    {
                        health = multipliers.HealthMultiplier,
                        damage = multipliers.DamageMultiplier,
                        armor = multipliers.ArmorMultiplier,
                        speed = multipliers.SpeedMultiplier
                    }
                };
            }, writeIndented: true);
        }

        [McpServerTool(Name = "adjust_archetype", Title = "Adjust Archetype")]
        [Description("Adjusts archetype stat multipliers (health, strength, agility, technique, intelligence, armor).")]
        public static Task<string> AdjustArchetype(
            [Description("Archetype name (e.g., 'Berserker', 'Tank', 'Assassin')")] string archetypeName,
            [Description("Stat name: 'health', 'strength', 'agility', 'technique', 'intelligence', or 'armor'")] string statName,
            [Description("New stat value")] double value)
        {
            return McpToolExecutor.ExecuteAsync(() =>
            {
                var success = BalanceTuningConsole.AdjustArchetype(archetypeName, statName, value);
                var config = GameConfiguration.Instance;
                var archetypes = config.EnemySystem.Archetypes;

                var archetypeInfo = new Dictionary<string, object>();
                if (archetypes.ContainsKey(archetypeName))
                {
                    var arch = archetypes[archetypeName];
                    archetypeInfo[archetypeName] = new
                    {
                        health = arch.Health,
                        strength = arch.Strength,
                        agility = arch.Agility,
                        technique = arch.Technique,
                        intelligence = arch.Intelligence,
                        armor = arch.Armor
                    };
                }

                return new
                {
                    success = success,
                    message = success ? $"Set {archetypeName}.{statName} to {value}" : $"Failed to set {archetypeName}.{statName}",
                    archetype = archetypeInfo
                };
            }, writeIndented: true);
        }

        [McpServerTool(Name = "adjust_player_base_attribute", Title = "Adjust Player Base Attribute")]
        [Description("Adjusts player base attributes (strength, agility, technique, intelligence).")]
        public static Task<string> AdjustPlayerBaseAttribute(
            [Description("Attribute name: 'strength', 'agility', 'technique', or 'intelligence'")] string attributeName,
            [Description("New base attribute value")] int value)
        {
            return McpToolExecutor.ExecuteAsync(() =>
            {
                var success = BalanceTuningConsole.AdjustPlayerBaseAttribute(attributeName, value);
                var config = GameConfiguration.Instance;
                var baseAttributes = config.Attributes.PlayerBaseAttributes;

                return new
                {
                    success = success,
                    message = success ? $"Set player base {attributeName} to {value}" : $"Failed to set player base {attributeName}",
                    currentAttributes = new
                    {
                        strength = baseAttributes.Strength,
                        agility = baseAttributes.Agility,
                        technique = baseAttributes.Technique,
                        intelligence = baseAttributes.Intelligence
                    }
                };
            }, writeIndented: true);
        }

        [McpServerTool(Name = "adjust_player_base_health", Title = "Adjust Player Base Health")]
        [Description("Adjusts player base health.")]
        public static Task<string> AdjustPlayerBaseHealth(
            [Description("New base health value")] int value)
        {
            return McpToolExecutor.ExecuteAsync(() =>
            {
                var success = BalanceTuningConsole.AdjustPlayerBaseHealth(value);
                var config = GameConfiguration.Instance;

                return new
                {
                    success = success,
                    message = success ? $"Set player base health to {value}" : "Failed to set player base health",
                    currentHealth = new
                    {
                        baseHealth = config.Character.PlayerBaseHealth,
                        healthPerLevel = config.Character.HealthPerLevel
                    }
                };
            }, writeIndented: true);
        }

        [McpServerTool(Name = "adjust_player_attributes_per_level", Title = "Adjust Player Attributes Per Level")]
        [Description("Adjusts how many attributes the player gains per level.")]
        public static Task<string> AdjustPlayerAttributesPerLevel(
            [Description("New attributes per level value")] int value)
        {
            return McpToolExecutor.ExecuteAsync(() =>
            {
                var success = BalanceTuningConsole.AdjustPlayerAttributesPerLevel(value);
                var config = GameConfiguration.Instance;

                return new
                {
                    success = success,
                    message = success ? $"Set player attributes per level to {value}" : "Failed to set player attributes per level",
                    currentAttributesPerLevel = config.Attributes.PlayerAttributesPerLevel
                };
            }, writeIndented: true);
        }

        [McpServerTool(Name = "adjust_player_health_per_level", Title = "Adjust Player Health Per Level")]
        [Description("Adjusts how much health the player gains per level.")]
        public static Task<string> AdjustPlayerHealthPerLevel(
            [Description("New health per level value")] int value)
        {
            return McpToolExecutor.ExecuteAsync(() =>
            {
                var success = BalanceTuningConsole.AdjustPlayerHealthPerLevel(value);
                var config = GameConfiguration.Instance;

                return new
                {
                    success = success,
                    message = success ? $"Set player health per level to {value}" : "Failed to set player health per level",
                    currentHealthPerLevel = config.Character.HealthPerLevel
                };
            }, writeIndented: true);
        }

        [McpServerTool(Name = "adjust_enemy_baseline_stat", Title = "Adjust Enemy Baseline Stat")]
        [Description("Adjusts enemy baseline stats (health, strength, agility, technique, intelligence, armor). Affects all enemies.")]
        public static Task<string> AdjustEnemyBaselineStat(
            [Description("Stat name: 'health', 'strength', 'agility', 'technique', 'intelligence', or 'armor'")] string statName,
            [Description("New baseline stat value")] double value)
        {
            return McpToolExecutor.ExecuteAsync(() =>
            {
                var success = BalanceTuningConsole.AdjustEnemyBaselineStat(statName, value);
                var config = GameConfiguration.Instance;
                var baselineStats = config.EnemySystem.BaselineStats;

                return new
                {
                    success = success,
                    message = success ? $"Set enemy baseline {statName} to {value}" : $"Failed to set enemy baseline {statName}",
                    currentBaselineStats = new
                    {
                        health = baselineStats.Health,
                        strength = baselineStats.Strength,
                        agility = baselineStats.Agility,
                        technique = baselineStats.Technique,
                        intelligence = baselineStats.Intelligence,
                        armor = baselineStats.Armor
                    }
                };
            }, writeIndented: true);
        }

        [McpServerTool(Name = "adjust_enemy_scaling_per_level", Title = "Adjust Enemy Scaling Per Level")]
        [Description("Adjusts how much enemy stats increase per level (health, attributes, armor).")]
        public static Task<string> AdjustEnemyScalingPerLevel(
            [Description("Scaling type: 'health', 'attributes', or 'armor'")] string statName,
            [Description("New scaling value")] double value)
        {
            return McpToolExecutor.ExecuteAsync(() =>
            {
                var success = BalanceTuningConsole.AdjustEnemyScalingPerLevel(statName, value);
                var config = GameConfiguration.Instance;
                var scaling = config.EnemySystem.ScalingPerLevel;

                return new
                {
                    success = success,
                    message = success ? $"Set enemy scaling per level {statName} to {value}" : $"Failed to set enemy scaling per level {statName}",
                    currentScaling = new
                    {
                        health = scaling.Health,
                        attributes = scaling.Attributes,
                        armor = scaling.Armor
                    }
                };
            }, writeIndented: true);
        }

        [McpServerTool(Name = "adjust_weapon_scaling", Title = "Adjust Weapon Scaling")]
        [Description("Adjusts weapon scaling multipliers.")]
        public static Task<string> AdjustWeaponScaling(
            [Description("Weapon type (e.g., 'Mace', 'Sword', 'Dagger', 'Wand') or 'global' for global multiplier")] string weaponType,
            [Description("Parameter name: 'damage' or 'damageMultiplier'")] string parameter,
            [Description("New value")] double value)
        {
            return McpToolExecutor.ExecuteAsync(() =>
            {
                var success = BalanceTuningConsole.AdjustWeaponScaling(weaponType, parameter, value);
                var config = GameConfiguration.Instance;

                return new
                {
                    success = success,
                    message = success ? $"Set weapon scaling {parameter} to {value}" : $"Failed to set weapon scaling",
                    currentGlobalDamageMultiplier = config.WeaponScaling?.GlobalDamageMultiplier ?? 1.0
                };
            }, writeIndented: true);
        }

        [McpServerTool(Name = "apply_preset", Title = "Apply Preset")]
        [Description("Applies a quick preset configuration: 'aggressive_enemies', 'tanky_enemies', 'fast_enemies', or 'baseline'.")]
        public static Task<string> ApplyPreset(
            [Description("Preset name: 'aggressive_enemies', 'tanky_enemies', 'fast_enemies', or 'baseline'")] string presetName)
        {
            return McpToolExecutor.ExecuteAsync(() =>
            {
                var success = BalanceTuningConsole.ApplyPreset(presetName);
                var config = GameConfiguration.Instance;
                var multipliers = config.EnemySystem.GlobalMultipliers;

                return new
                {
                    success = success,
                    message = success ? $"Applied preset '{presetName}'" : $"Failed to apply preset '{presetName}'",
                    currentMultipliers = new
                    {
                        health = multipliers.HealthMultiplier,
                        damage = multipliers.DamageMultiplier,
                        armor = multipliers.ArmorMultiplier,
                        speed = multipliers.SpeedMultiplier
                    }
                };
            }, writeIndented: true);
        }

        [McpServerTool(Name = "save_configuration", Title = "Save Configuration")]
        [Description("Saves the current game configuration to TuningConfig.json.")]
        public static Task<string> SaveConfiguration()
        {
            return McpToolExecutor.ExecuteAsync(() =>
            {
                var success = BalanceTuningConsole.SaveConfiguration();
                return new
                {
                    success = success,
                    message = success ? "Configuration saved successfully" : "Failed to save configuration"
                };
            });
        }

        [McpServerTool(Name = "get_current_configuration", Title = "Get Current Configuration")]
        [Description("Gets the current game configuration values (enemy multipliers, archetypes, etc.).")]
        public static Task<string> GetCurrentConfiguration()
        {
            return McpToolExecutor.ExecuteAsync(() =>
            {
                var config = GameConfiguration.Instance;
                var multipliers = config.EnemySystem.GlobalMultipliers;
                var archetypes = config.EnemySystem.Archetypes.ToDictionary(
                    kvp => kvp.Key,
                    kvp => new
                    {
                        health = kvp.Value.Health,
                        strength = kvp.Value.Strength,
                        agility = kvp.Value.Agility,
                        technique = kvp.Value.Technique,
                        intelligence = kvp.Value.Intelligence,
                        armor = kvp.Value.Armor
                    });

                return new
                {
                    globalMultipliers = new
                    {
                        health = multipliers.HealthMultiplier,
                        damage = multipliers.DamageMultiplier,
                        armor = multipliers.ArmorMultiplier,
                        speed = multipliers.SpeedMultiplier
                    },
                    archetypes = archetypes,
                    weaponScaling = new
                    {
                        globalDamageMultiplier = config.WeaponScaling?.GlobalDamageMultiplier ?? 1.0
                    }
                };
            }, writeIndented: true);
        }
    }
}
