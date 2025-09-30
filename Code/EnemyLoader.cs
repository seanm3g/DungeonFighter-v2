using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace RPGGame
{
    public class EnemyStats
    {
        [JsonPropertyName("strength")]
        public int Strength { get; set; }
        [JsonPropertyName("agility")]
        public int Agility { get; set; }
        [JsonPropertyName("technique")]
        public int Technique { get; set; }
        [JsonPropertyName("intelligence")]
        public int Intelligence { get; set; } = 4; // Default value if not specified
    }

    public class EnemyData
    {
        [JsonPropertyName("name")]
        public string Name { get; set; } = "";
        [JsonPropertyName("baseLevel")]
        public int BaseLevel { get; set; }
        [JsonPropertyName("baseHealth")]
        public int BaseHealth { get; set; }
        [JsonPropertyName("baseStats")]
        public EnemyStats BaseStats { get; set; } = new EnemyStats();
        [JsonPropertyName("baseArmor")]
        public int BaseArmor { get; set; }
        [JsonPropertyName("actions")]
        public List<string> Actions { get; set; } = new List<string>();
        [JsonPropertyName("primaryAttribute")]
        public string PrimaryAttribute { get; set; } = "Strength"; // Default to Strength if not specified
        [JsonPropertyName("isLiving")]
        public bool IsLiving { get; set; } = true; // Default to living if not specified
        
        // Convenience properties for backward compatibility
        public int Strength => BaseStats.Strength;
        public int Agility => BaseStats.Agility;
        public int Technique => BaseStats.Technique;
        public int Intelligence => BaseStats.Intelligence;
        public int Armor => BaseArmor;
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

        private static Enemy CreateEnemyFromData(EnemyData data, int level)
        {
            // Determine archetype first
            var suggestedArchetype = EnemyDPSCalculator.SuggestArchetypeForEnemy(data.Name, data.Strength, data.Agility, data.Technique, data.Intelligence);
            
            // Use new layered balance calculation system
            var baseStats = new EnemyBaseStats
            {
                Strength = data.Strength,
                Agility = data.Agility,
                Technique = data.Technique,
                Intelligence = data.Intelligence
            };
            
            var calculatedStats = EnemyBalanceCalculator.CalculateStats(level, suggestedArchetype, baseStats);
            
            // Parse the primary attribute string to enum
            PrimaryAttribute primaryAttribute = PrimaryAttribute.Strength; // Default
            if (Enum.TryParse<PrimaryAttribute>(data.PrimaryAttribute, out var parsedAttribute))
            {
                primaryAttribute = parsedAttribute;
            }

            var enemy = new Enemy(data.Name, level, calculatedStats.Health, calculatedStats.Strength, calculatedStats.Agility, calculatedStats.Technique, calculatedStats.Intelligence, calculatedStats.Armor, primaryAttribute, data.IsLiving, suggestedArchetype);
            
            // Apply DPS-based scaling to set target values
            EnemyDPSSystem.ApplyDPSScaling(enemy);
            
            // Clear default actions and add actions from the data
            enemy.ActionPool.Clear();
            foreach (var actionName in data.Actions)
            {
                var action = ActionLoader.GetAction(actionName);
                if (action != null)
                {
                    enemy.AddAction(action, 1.0); // Default weight of 1.0
                }
                else
                {
                    // If JSON action fails to load, add a fallback basic attack
                    var fallbackAction = new Action(
                        actionName,
                        ActionType.Attack,
                        TargetType.SingleTarget,
                        baseValue: 8,
                        range: 1,
                        description: $"A {actionName.ToLower()}"
                    );
                    enemy.AddAction(fallbackAction, 1.0);
                }
            }

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
    }
} 