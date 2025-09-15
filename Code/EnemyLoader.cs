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
        [JsonPropertyName("level")]
        public int Level { get; set; }
        [JsonPropertyName("strength")]
        public int Strength { get; set; }
        [JsonPropertyName("agility")]
        public int Agility { get; set; }
        [JsonPropertyName("technique")]
        public int Technique { get; set; }
        [JsonPropertyName("intelligence")]
        public int Intelligence { get; set; } = 4; // Default value if not specified
        [JsonPropertyName("armor")]
        public int Armor { get; set; }
        [JsonPropertyName("actions")]
        public List<string> Actions { get; set; } = new List<string>();
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
                                Console.WriteLine($"Warning: Found enemy with null/empty name");
                            }
                        }
                    }
                    else
                    {
                        Console.WriteLine("Warning: JSON deserialization returned null");
                    }
                }
                else
                {
                    Console.WriteLine($"Warning: Enemies file not found. Tried paths: {string.Join(", ", PossibleEnemyPaths)}");
                    _enemies = new Dictionary<string, EnemyData>();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error loading enemies: {ex.Message}");
                Console.WriteLine($"Stack trace: {ex.StackTrace}");
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
            // Use the stats from the JSON data with tuning config
            var tuning = TuningConfig.Instance;
            int health = 80 + (level * tuning.Character.EnemyHealthPerLevel); // Use tuning config for health scaling
            int strength = data.Strength;
            int agility = data.Agility;
            int technique = data.Technique;
            int intelligence = data.Intelligence;

            var enemy = new Enemy(data.Name, level, health, strength, agility, technique, intelligence);
            
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
    }
} 