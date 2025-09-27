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
            // Determine archetype first
            var suggestedArchetype = EnemyDPSCalculator.SuggestArchetypeForEnemy(data.Name, data.Strength, data.Agility, data.Technique, data.Intelligence);
            
            // Use stat pool system for balanced distribution
            var statDistribution = EnemyStatPoolSystem.DistributeStats(level, suggestedArchetype);
            
            // Blend JSON base stats with stat pool distribution (70% stat pool, 30% JSON base)
            int strength = (int)Math.Round(statDistribution.Strength * 0.7 + data.Strength * 0.3);
            int agility = (int)Math.Round(statDistribution.Agility * 0.7 + data.Agility * 0.3);
            int technique = (int)Math.Round(statDistribution.Technique * 0.7 + data.Technique * 0.3);
            int intelligence = (int)Math.Round(statDistribution.Intelligence * 0.7 + data.Intelligence * 0.3);
            int health = (int)Math.Round(statDistribution.Health * 0.7 + data.BaseHealth * 0.3);
            int armor = (int)Math.Round(statDistribution.Armor * 0.7 + data.BaseArmor * 0.3);
            
            // Ensure minimum values
            strength = Math.Max(1, strength);
            agility = Math.Max(1, agility);
            technique = Math.Max(1, technique);
            intelligence = Math.Max(1, intelligence);
            health = Math.Max(10, health);
            armor = Math.Max(data.BaseArmor, armor); // Ensure armor is at least the base armor value

            // Parse the primary attribute string to enum
            PrimaryAttribute primaryAttribute = PrimaryAttribute.Strength; // Default
            if (Enum.TryParse<PrimaryAttribute>(data.PrimaryAttribute, out var parsedAttribute))
            {
                primaryAttribute = parsedAttribute;
            }

            var enemy = new Enemy(data.Name, level, health, strength, agility, technique, intelligence, armor, primaryAttribute, data.IsLiving, suggestedArchetype);
            
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