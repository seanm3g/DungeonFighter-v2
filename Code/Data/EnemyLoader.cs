using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace RPGGame
{
    public class EnemyData
    {
        [JsonPropertyName("name")]
        public string Name { get; set; } = "";
        [JsonPropertyName("archetype")]
        public string Archetype { get; set; } = "Berserker"; // Default archetype
        [JsonPropertyName("overrides")]
        public StatOverridesConfig? Overrides { get; set; }
        /// <summary>
        /// Optional per-enemy base attributes. When present, these are treated as the level 1 attribute values
        /// (prior to any per-level growth).
        /// </summary>
        [JsonPropertyName("baseAttributes")]
        public EnemyAttributeSet? BaseAttributes { get; set; }
        /// <summary>
        /// Optional explicit per-level growth. When omitted, growth can be derived from tuning + overrides.
        /// </summary>
        [JsonPropertyName("growthPerLevel")]
        public EnemyAttributeSet? GrowthPerLevel { get; set; }
        /// <summary>
        /// Optional level-1 max health (before per-level growth). When null, uses baseline × archetype × overrides.health.
        /// </summary>
        [JsonPropertyName("baseHealth")]
        public double? BaseHealth { get; set; }
        /// <summary>
        /// Optional HP gained per level after level 1. When null, uses <see cref="ScalingPerLevelConfig.Health"/> × overrides.health (same weighting idea as attribute growth).
        /// </summary>
        [JsonPropertyName("healthGrowthPerLevel")]
        public double? HealthGrowthPerLevel { get; set; }
        [JsonPropertyName("actions")]
        public List<string> Actions { get; set; } = new List<string>();
        [JsonPropertyName("isLiving")]
        public bool IsLiving { get; set; } = true; // Default to living if not specified
        [JsonPropertyName("description")]
        public string Description { get; set; } = "";
        [JsonPropertyName("colorOverride")]
        public ColorOverride? ColorOverride { get; set; }
    }

    /// <summary>
    /// Attribute set used for enemy base stats and growth rates.
    /// Values are doubles so per-level gains can be fractional.
    /// </summary>
    public class EnemyAttributeSet
    {
        [JsonPropertyName("strength")]
        public double Strength { get; set; }
        [JsonPropertyName("agility")]
        public double Agility { get; set; }
        [JsonPropertyName("technique")]
        public double Technique { get; set; }
        [JsonPropertyName("intelligence")]
        public double Intelligence { get; set; }
    }

    public class StatOverridesConfig
    {
        [JsonPropertyName("health")]
        public double? Health { get; set; }
        [JsonPropertyName("strength")]
        public double? Strength { get; set; }
        [JsonPropertyName("agility")]
        public double? Agility { get; set; }
        [JsonPropertyName("technique")]
        public double? Technique { get; set; }
        [JsonPropertyName("intelligence")]
        public double? Intelligence { get; set; }
        [JsonPropertyName("armor")]
        public double? Armor { get; set; }
    }

    public static class EnemyLoader
    {
        private static readonly object EnemiesLock = new object();
        private static Dictionary<string, EnemyData>? _enemies;
        private static readonly string[] PossibleEnemyPaths = {
            Path.Combine("GameData", "Enemies.json"),
            Path.Combine("..", "GameData", "Enemies.json"),
            Path.Combine("..", "..", "GameData", "Enemies.json")
        };

        public static void LoadEnemies()
        {
            LoadEnemies(validate: false);
        }

        /// <summary>
        /// Loads enemies from JSON with optional validation
        /// </summary>
        /// <param name="validate">If true, validates loaded enemies and logs any issues</param>
        public static void LoadEnemies(bool validate)
        {
            lock (EnemiesLock)
                LoadEnemiesCore(validate);
        }

        /// <summary>Loads or replaces <see cref="_enemies"/>; caller must hold <see cref="EnemiesLock"/>.</summary>
        private static void LoadEnemiesCore(bool validate)
        {
            try
            {
                string? foundPath = null;
                foreach (string path in PossibleEnemyPaths)
                {
                    if (File.Exists(path))
                    {
                        foundPath = path;
                        break;
                    }
                }

                if (foundPath != null)
                {
                    string jsonContent = File.ReadAllText(foundPath);

                    var enemyList = JsonSerializer.Deserialize<List<EnemyData>>(jsonContent);

                    _enemies = new Dictionary<string, EnemyData>();
                    if (enemyList != null)
                    {
                        foreach (var enemy in enemyList)
                        {
                            if (!string.IsNullOrEmpty(enemy.Name))
                            {
                                _enemies[enemy.Name] = enemy;
                            }
                            else
                            {
                                UIManager.WriteSystemLine($"Warning: Found enemy with null/empty name");
                            }
                        }
                    }
                    else
                    {
                        UIManager.WriteSystemLine("Warning: JSON deserialization returned null");
                    }
                }
                else
                {
                    UIManager.WriteSystemLine($"Warning: Enemies file not found. Tried paths: {string.Join(", ", PossibleEnemyPaths)}");
                    _enemies = new Dictionary<string, EnemyData>();
                }

                if (validate)
                    ValidateLoadedEnemies();
            }
            catch (Exception ex)
            {
                UIManager.WriteSystemLine($"Error loading enemies: {ex.Message}");
                UIManager.WriteSystemLine($"Stack trace: {ex.StackTrace}");
                _enemies = new Dictionary<string, EnemyData>();
            }
        }

        /// <summary>
        /// Validates loaded enemies and logs any issues
        /// </summary>
        private static void ValidateLoadedEnemies()
        {
            try
            {
                var validator = new Data.Validation.EnemyDataValidator();
                var result = validator.Validate();
                
                if (!result.IsValid)
                {
                    UIManager.WriteSystemLine($"Enemy validation found {result.Errors.Count} errors and {result.Warnings.Count} warnings");
                    foreach (var error in result.Errors)
                    {
                        UIManager.WriteSystemLine($"Enemy validation error: {error.Message}");
                    }
                }
            }
            catch (Exception ex)
            {
                UIManager.WriteSystemLine($"Enemy validation failed: {ex.Message}");
            }
        }

        public static Enemy? CreateEnemy(string enemyType, int level = 1)
        {
            lock (EnemiesLock)
            {
                if (_enemies == null)
                    LoadEnemiesCore(validate: false);

                if (_enemies != null)
                {
                    if (_enemies.TryGetValue(enemyType, out var enemyData))
                        return CreateEnemyFromData(enemyData, level);

                    var caseInsensitiveMatch = _enemies.FirstOrDefault(kvp =>
                        string.Equals(kvp.Key, enemyType, StringComparison.OrdinalIgnoreCase));

                    if (caseInsensitiveMatch.Key != null)
                        return CreateEnemyFromData(caseInsensitiveMatch.Value, level);
                }

                return null;
            }
        }

        private static Enemy? CreateEnemyFromData(EnemyData data, int level)
        {
            var enemy = EnemyDataFactory.CreateEnemyFromData(data, level);
            if (enemy == null)
                return null;
            
            // Color override is set in EnemyDataFactory.CreateEnemyFromData
            
            // Add a common-tier weapon to the enemy (same system as heroes)
            var enemyWeapon = EnemyWeaponGenerator.GenerateCommonWeaponForEnemy(data.Name, enemy.Level);
            enemy.Weapon = enemyWeapon;
            
            // Add actions to the enemy
            EnemyActionManager.AddActionsToEnemy(enemy, data);
            
            return enemy;
        }

        public static bool HasEnemy(string enemyType)
        {
            lock (EnemiesLock)
            {
                if (_enemies == null)
                    LoadEnemiesCore(validate: false);

                if (_enemies == null)
                    return false;

                if (_enemies.ContainsKey(enemyType))
                    return true;

                return _enemies.Keys.Any(key =>
                    string.Equals(key, enemyType, StringComparison.OrdinalIgnoreCase));
            }
        }

        public static List<string> GetAllEnemyTypes()
        {
            lock (EnemiesLock)
            {
                if (_enemies == null)
                    LoadEnemiesCore(validate: false);

                return _enemies?.Keys.ToList() ?? new List<string>();
            }
        }

        public static EnemyData? GetEnemyData(string enemyType)
        {
            lock (EnemiesLock)
            {
                if (_enemies == null)
                    LoadEnemiesCore(validate: false);

                if (_enemies != null)
                {
                    if (_enemies.TryGetValue(enemyType, out var enemyData))
                        return enemyData;

                    var caseInsensitiveMatch = _enemies.FirstOrDefault(kvp =>
                        string.Equals(kvp.Key, enemyType, StringComparison.OrdinalIgnoreCase));

                    if (caseInsensitiveMatch.Key != null)
                        return caseInsensitiveMatch.Value;
                }

                return null;
            }
        }

        /// <summary>
        /// Gets all enemy data loaded from the JSON file
        /// </summary>
        public static List<EnemyData> GetAllEnemyData()
        {
            lock (EnemiesLock)
            {
                if (_enemies == null)
                    LoadEnemiesCore(validate: false);

                return _enemies?.Values.ToList() ?? new List<EnemyData>();
            }
        }

        /// <summary>
        /// Suggests an archetype for an enemy based on their stats
        /// </summary>
        private static EnemyArchetype SuggestArchetypeForEnemy(string name, int strength, int agility, int technique, int intelligence)
        {
            // Simple archetype suggestion based on primary stat
            int maxStat = Math.Max(Math.Max(strength, agility), Math.Max(technique, intelligence));
            
            if (maxStat == strength)
                return EnemyArchetype.Brute;
            else if (maxStat == agility)
                return EnemyArchetype.Assassin;
            else if (maxStat == technique)
                return EnemyArchetype.Berserker;
            else
                return EnemyArchetype.Guardian;
        }

    }
} 