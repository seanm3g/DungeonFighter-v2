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
        [JsonPropertyName("actions")]
        public List<string> Actions { get; set; } = new List<string>();
        [JsonPropertyName("isLiving")]
        public bool IsLiving { get; set; } = true; // Default to living if not specified
        [JsonPropertyName("description")]
        public string Description { get; set; } = "";
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
        private static Dictionary<string, EnemyData>? _enemies;
        private static readonly string[] PossibleEnemyPaths = {
            Path.Combine("GameData", "Enemies.json"),
            Path.Combine("..", "GameData", "Enemies.json"),
            Path.Combine("..", "..", "GameData", "Enemies.json"),
            Path.Combine("DF4 - CONSOLE", "GameData", "Enemies.json"),
            Path.Combine("..", "DF4 - CONSOLE", "GameData", "Enemies.json")
        };

        public static void LoadEnemies()
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
            }
            catch (Exception ex)
            {
                UIManager.WriteSystemLine($"Error loading enemies: {ex.Message}");
                UIManager.WriteSystemLine($"Stack trace: {ex.StackTrace}");
                _enemies = new Dictionary<string, EnemyData>();
            }
        }

        public static Enemy? CreateEnemy(string enemyType, int level = 1)
        {
            if (_enemies == null)
            {
                LoadEnemies();
            }

            if (_enemies != null)
            {
                // Try exact match first
                if (_enemies.TryGetValue(enemyType, out var enemyData))
                {
                    return CreateEnemyFromData(enemyData, level);
                }
                
                // Try case-insensitive match
                var caseInsensitiveMatch = _enemies.FirstOrDefault(kvp => 
                    string.Equals(kvp.Key, enemyType, StringComparison.OrdinalIgnoreCase));
                
                if (caseInsensitiveMatch.Key != null)
                {
                    return CreateEnemyFromData(caseInsensitiveMatch.Value, level);
                }
            }

            return null;
        }

        private static Enemy? CreateEnemyFromData(EnemyData data, int level)
        {
            var enemy = EnemyDataFactory.CreateEnemyFromData(data, level);
            if (enemy == null)
                return null;
            
            // Add a common-tier weapon to the enemy (same system as heroes)
            var enemyWeapon = EnemyWeaponGenerator.GenerateCommonWeaponForEnemy(data.Name, enemy.Level);
            enemy.Weapon = enemyWeapon;
            
            // Add actions to the enemy
            EnemyActionManager.AddActionsToEnemy(enemy, data);
            
            return enemy;
        }

        public static bool HasEnemy(string enemyType)
        {
            if (_enemies == null)
            {
                LoadEnemies();
            }

            if (_enemies == null) return false;
            
            // Try exact match first
            if (_enemies.ContainsKey(enemyType))
            {
                return true;
            }
            
            // Try case-insensitive match
            return _enemies.Keys.Any(key => 
                string.Equals(key, enemyType, StringComparison.OrdinalIgnoreCase));
        }

        public static List<string> GetAllEnemyTypes()
        {
            if (_enemies == null)
            {
                LoadEnemies();
            }

            return _enemies?.Keys.ToList() ?? new List<string>();
        }

        public static EnemyData? GetEnemyData(string enemyType)
        {
            if (_enemies == null)
            {
                LoadEnemies();
            }

            if (_enemies != null)
            {
                // Try exact match first
                if (_enemies.TryGetValue(enemyType, out var enemyData))
                {
                    return enemyData;
                }
                
                // Try case-insensitive match
                var caseInsensitiveMatch = _enemies.FirstOrDefault(kvp => 
                    string.Equals(kvp.Key, enemyType, StringComparison.OrdinalIgnoreCase));
                
                if (caseInsensitiveMatch.Key != null)
                {
                    return caseInsensitiveMatch.Value;
                }
            }

            return null;
        }

        /// <summary>
        /// Gets all enemy data loaded from the JSON file
        /// </summary>
        public static List<EnemyData> GetAllEnemyData()
        {
            if (_enemies == null)
            {
                LoadEnemies();
            }

            return _enemies?.Values.ToList() ?? new List<EnemyData>();
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