using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace RPGGame
{
    public class EnemyActionData
    {
        [JsonPropertyName("name")]
        public string Name { get; set; } = "";
        [JsonPropertyName("weight")]
        public double Weight { get; set; }
    }

    public class EnemyData
    {
        [JsonPropertyName("name")]
        public string Name { get; set; } = "";
        [JsonPropertyName("baseHealth")]
        public int BaseHealth { get; set; }
        [JsonPropertyName("healthPerLevel")]
        public int HealthPerLevel { get; set; }
        [JsonPropertyName("baseStrength")]
        public int BaseStrength { get; set; }
        [JsonPropertyName("strengthPerLevel")]
        public int StrengthPerLevel { get; set; }
        [JsonPropertyName("baseAgility")]
        public int BaseAgility { get; set; }
        [JsonPropertyName("agilityPerLevel")]
        public int AgilityPerLevel { get; set; }
        [JsonPropertyName("baseTechnique")]
        public int BaseTechnique { get; set; }
        [JsonPropertyName("techniquePerLevel")]
        public int TechniquePerLevel { get; set; }
        [JsonPropertyName("actions")]
        public List<EnemyActionData> Actions { get; set; } = new List<EnemyActionData>();
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
                    Console.WriteLine($"Reading JSON from {foundPath}, content length: {jsonContent.Length}");
                    
                    var enemyList = JsonSerializer.Deserialize<List<EnemyData>>(jsonContent);
                    
                    _enemies = new Dictionary<string, EnemyData>();
                    if (enemyList != null)
                    {
                        Console.WriteLine($"Deserialized {enemyList.Count} enemy types from JSON");
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
                    Console.WriteLine($"Successfully loaded {_enemies.Count} enemy types from {foundPath}");
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

            if (_enemies != null && _enemies.TryGetValue(enemyType.ToLower(), out var enemyData))
            {
                return CreateEnemyFromData(enemyData, level);
            }

            return null;
        }

        private static Enemy CreateEnemyFromData(EnemyData data, int level)
        {
            // Calculate stats based on level
            int health = data.BaseHealth + (data.HealthPerLevel * level);
            int strength = data.BaseStrength + (data.StrengthPerLevel * level);
            int agility = data.BaseAgility + (data.AgilityPerLevel * level);
            int technique = data.BaseTechnique + (data.TechniquePerLevel * level);

            var enemy = new Enemy(data.Name, level, health, strength, agility, technique);
            enemy.ActionPool.Clear();

            // Add actions from the data
            foreach (var actionData in data.Actions)
            {
                var action = ActionLoader.GetAction(actionData.Name);
                if (action != null)
                {
                    enemy.AddAction(action, actionData.Weight);
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

            return _enemies?.ContainsKey(enemyType.ToLower()) ?? false;
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

            if (_enemies != null && _enemies.TryGetValue(enemyType.ToLower(), out var enemyData))
            {
                return enemyData;
            }

            return null;
        }
    }
} 