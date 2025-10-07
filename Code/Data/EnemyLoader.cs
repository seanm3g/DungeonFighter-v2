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
        private static List<WeaponData>? _weaponData;

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
            var tuning = GameConfiguration.Instance;
            
            // Use the new unified enemy system
            if (!string.IsNullOrEmpty(data.Archetype) && tuning.EnemySystem != null && 
                IsValidArchetype(data.Archetype))
            {
                return CreateEnemyWithNewSystem(data, level, tuning);
            }
            else
            {
                // If no valid archetype, create a basic enemy with default stats
                UIManager.WriteSystemLine($"Warning: Enemy '{data.Name}' has invalid archetype '{data.Archetype}'. Using default Berserker archetype.");
                data.Archetype = "Berserker";
                return CreateEnemyWithNewSystem(data, level, tuning);
            }
        }

        private static Enemy CreateEnemyWithNewSystem(EnemyData data, int level, GameConfiguration tuning)
        {
            // Use the new unified EnemySystem configuration
            var enemySystem = tuning.EnemySystem;
            
            // 1. Start with baseline stats
            var baseline = enemySystem.BaselineStats;
            var scaling = enemySystem.ScalingPerLevel;
            var global = enemySystem.GlobalMultipliers;
            
            // 2. Apply archetype multipliers
            var archetype = enemySystem.Archetypes.GetValueOrDefault(data.Archetype);
            if (archetype == null)
            {
                // Fallback to Berserker archetype if not found
                archetype = enemySystem.Archetypes.GetValueOrDefault("Berserker") ?? new ArchetypeMultipliersConfig();
            }
            
            // 3. Apply individual enemy overrides
            var overrides = data.Overrides ?? new StatOverridesConfig();
            
            // 4. Calculate base stats with archetype multipliers and overrides
            var baseHealth = (int)(baseline.Health * archetype.Health * (overrides.Health ?? 1.0));
            var baseStrength = (int)(baseline.Strength * archetype.Strength * (overrides.Strength ?? 1.0));
            var baseAgility = (int)(baseline.Agility * archetype.Agility * (overrides.Agility ?? 1.0));
            var baseTechnique = (int)(baseline.Technique * archetype.Technique * (overrides.Technique ?? 1.0));
            var baseIntelligence = (int)(baseline.Intelligence * archetype.Intelligence * (overrides.Intelligence ?? 1.0));
            var baseArmor = (int)(baseline.Armor * archetype.Armor * (overrides.Armor ?? 1.0));
            
            // 5. Scale by level
            var levelScaledStats = new
            {
                Health = baseHealth + (level - 1) * scaling.Health,
                Strength = baseStrength + (level - 1) * scaling.Attributes,
                Agility = baseAgility + (level - 1) * scaling.Attributes,
                Technique = baseTechnique + (level - 1) * scaling.Attributes,
                Intelligence = baseIntelligence + (level - 1) * scaling.Attributes,
                Armor = (int)(baseArmor + (level - 1) * scaling.Armor)
            };
            
            // 6. Apply global multipliers
            var finalStats = new
            {
                Health = (int)(levelScaledStats.Health * global.HealthMultiplier),
                Strength = (int)(levelScaledStats.Strength * global.DamageMultiplier),
                Agility = (int)(levelScaledStats.Agility * global.SpeedMultiplier),
                Technique = (int)(levelScaledStats.Technique * global.DamageMultiplier),
                Intelligence = (int)(levelScaledStats.Intelligence * global.DamageMultiplier),
                Armor = (int)(levelScaledStats.Armor * global.ArmorMultiplier)
            };
            
            // Determine primary attribute based on highest stat
            var primaryAttribute = DeterminePrimaryAttribute(finalStats.Strength, finalStats.Agility, finalStats.Technique, finalStats.Intelligence);
            
            // Convert archetype string to enum
            var enemyArchetype = ConvertStringToEnemyArchetype(data.Archetype);
            
            var enemy = new Enemy(data.Name, level, finalStats.Health, finalStats.Strength, finalStats.Agility, finalStats.Technique, finalStats.Intelligence, finalStats.Armor, primaryAttribute, data.IsLiving, enemyArchetype);
            
            // Add a common-tier weapon to the enemy (same system as heroes)
            var enemyWeapon = GenerateCommonWeaponForEnemy(data.Name, enemy.Level);
            enemy.Weapon = enemyWeapon;
            
            // Add actions to the enemy
            AddActionsToEnemy(enemy, data);
            
            return enemy;
        }


        private static PrimaryAttribute DeterminePrimaryAttribute(int strength, int agility, int technique, int intelligence)
        {
            var stats = new[] { (strength, PrimaryAttribute.Strength), (agility, PrimaryAttribute.Agility), (technique, PrimaryAttribute.Technique), (intelligence, PrimaryAttribute.Intelligence) };
            return stats.OrderByDescending(s => s.Item1).First().Item2;
        }

        /// <summary>
        /// Generates a common-tier weapon for an enemy
        /// </summary>
        /// <param name="enemyName">The name of the enemy</param>
        /// <param name="enemyLevel">The level of the enemy</param>
        /// <returns>A common-tier weapon appropriate for the enemy</returns>
        private static WeaponItem GenerateCommonWeaponForEnemy(string enemyName, int enemyLevel)
        {
            // Load weapon data if not already loaded
            if (_weaponData == null)
            {
                LoadWeaponData();
            }

            // Get only tier 1 (common) weapons
            var commonWeapons = _weaponData?.Where(w => w.Tier == 1).ToList() ?? new List<WeaponData>();
            
            if (!commonWeapons.Any())
            {
                // Fallback to basic weapon if no common weapons found
                return new WeaponItem($"{enemyName} Weapon", 1, 6, 0.0, WeaponType.Sword);
            }

            // Select a random common weapon
            var selectedWeapon = commonWeapons[RandomUtility.Next(commonWeapons.Count)];
            
            // Generate the weapon item
            var weapon = ItemGenerator.GenerateWeaponItem(selectedWeapon);
            
            // Ensure it's marked as common rarity
            weapon.Rarity = "Common";
            
            return weapon;
        }

        private static bool IsValidArchetype(string archetypeString)
        {
            return archetypeString.ToLower() switch
            {
                "berserker" => true,
                "guardian" => true,
                "assassin" => true,
                "brute" => true,
                "mage" => true,
                _ => false
            };
        }

        private static EnemyArchetype ConvertStringToEnemyArchetype(string archetypeString)
        {
            return archetypeString.ToLower() switch
            {
                "berserker" => EnemyArchetype.Berserker,
                "guardian" => EnemyArchetype.Guardian,
                "assassin" => EnemyArchetype.Assassin,
                "brute" => EnemyArchetype.Brute,
                "mage" => EnemyArchetype.Mage,
                _ => EnemyArchetype.Berserker
            };
        }

        private static void AddActionsToEnemy(Enemy enemy, EnemyData data)
        {
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

            // Ensure ALL enemies have a BASIC ATTACK action
            bool hasBasicAttack = enemy.ActionPool.Any(a => string.Equals(a.action.Name, "BASIC ATTACK", StringComparison.OrdinalIgnoreCase));
            if (!hasBasicAttack)
            {
                // Try to inject BASIC ATTACK from actions data
                var basic = ActionLoader.GetAction("BASIC ATTACK");
                if (basic != null)
                {
                    enemy.AddAction(basic, 1.0);
                }
                else
                {
                    // Final fallback: create a simple basic attack
                    var createdBasic = new Action(
                        name: "BASIC ATTACK",
                        type: ActionType.Attack,
                        targetType: TargetType.SingleTarget,
                        baseValue: 8,
                        range: 1,
                        description: "A standard physical attack"
                    );
                    enemy.AddAction(createdBasic, 1.0);
                }
            }

            // Additional safeguard: ensure enemy has at least one damaging action (Attack or Spell)
            bool hasDamagingAction = enemy.ActionPool.Any(a => a.action.Type == ActionType.Attack || a.action.Type == ActionType.Spell);
            if (!hasDamagingAction)
            {
                UIManager.WriteSystemLine($"Warning: Enemy '{data.Name}' still has no damaging actions after adding BASIC ATTACK.");
            }
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

        /// <summary>
        /// Loads weapon data from JSON file
        /// </summary>
        private static void LoadWeaponData()
        {
            string? filePath = JsonLoader.FindGameDataFile("Weapons.json");
            if (filePath != null)
            {
                _weaponData = JsonLoader.LoadJsonList<WeaponData>(filePath);
            }
            else
            {
                UIManager.WriteLine("Error loading weapon data: Weapons.json not found", UIMessageType.System);
                _weaponData = new List<WeaponData>();
            }
        }
    }
} 