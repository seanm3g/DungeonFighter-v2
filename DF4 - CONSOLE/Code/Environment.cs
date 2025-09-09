using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;

namespace RPGGame
{
    public enum PassiveEffectType
    {
        None,
        DamageMultiplier, // e.g., -10% damage
        SpeedMultiplier   // e.g., +25% attack speed
    }

    public class Environment : Entity
    {
        public string Description { get; private set; }
        public bool IsHostile { get; private set; }
        private List<Enemy> enemies;
        private Random random;
        public string Theme { get; private set; }

        // Passive and active effect support
        public PassiveEffectType PassiveEffectType { get; private set; } = PassiveEffectType.None;
        public double PassiveEffectValue { get; private set; } = 1.0;
        public Action? ActiveEffectAction { get; private set; }

        public Environment(string name, string description, bool isHostile, string theme)
            : base(name)
        {
            random = new Random();
            Description = description;
            IsHostile = isHostile;
            Theme = theme;
            enemies = new List<Enemy>();
            InitializeActions();
        }

        private void InitializeActions()
        {
            LoadEnvironmentalActionsFromJson();
        }

        private void LoadEnvironmentalActionsFromJson()
        {
            try
            {
                string jsonPath = Path.Combine("GameData", "Actions.json");
                if (File.Exists(jsonPath))
                {
                    string jsonContent = File.ReadAllText(jsonPath);
                    var allActions = System.Text.Json.JsonSerializer.Deserialize<List<ActionData>>(jsonContent);
                    
                    if (allActions != null)
                    {
                        // Filter actions by environment tag and theme
                        var environmentalActions = allActions.Where(action => 
                            action.tags != null && 
                            action.tags.Contains("environment") && 
                            action.tags.Contains(Theme.ToLower())
                        ).ToList();
                        
                        if (environmentalActions.Any())
                        {
                            // Add all matching environmental actions
                            foreach (var actionData in environmentalActions)
                            {
                                var action = CreateActionFromData(actionData);
                                AddAction(action, 0.7); // 70% probability for environmental actions
                            }
                        }
                        else
                        {
                            // Fallback to default environmental action
                            AddDefaultEnvironmentalAction();
                        }
                    }
                    else
                    {
                        AddDefaultEnvironmentalAction();
                    }
                }
                else
                {
                    AddDefaultEnvironmentalAction();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error loading environmental actions from JSON: {ex.Message}");
                AddDefaultEnvironmentalAction();
            }
        }

        private void AddDefaultEnvironmentalAction()
        {
            var defaultAction = new Action(
                name: "Environmental Hazard",
                description: "The environment itself poses a threat!",
                type: ActionType.Attack,
                targetType: TargetType.AreaOfEffect,
                baseValue: 5,
                cooldown: 2
            );
            AddAction(defaultAction, 1.0);
        }

        private Action CreateActionFromData(ActionData data)
        {
            var actionType = Enum.TryParse<ActionType>(data.type, true, out var parsedType) ? parsedType : ActionType.Attack;
            var targetType = TargetType.AreaOfEffect; // Environmental actions are always area of effect
            
            var action = new Action(
                name: data.name,
                type: actionType,
                targetType: targetType,
                baseValue: 0,
                range: 1,
                cooldown: 0,
                description: data.description ?? "",
                comboOrder: data.comboOrder ?? 0,
                damageMultiplier: data.damageMultiplier,
                length: data.length,
                causesBleed: data.causesBleed ?? false,
                causesWeaken: data.causesWeaken ?? false,
                isComboAction: data.isComboAction ?? false,
                comboBonusAmount: data.comboBonusAmount ?? 0,
                comboBonusDuration: data.comboBonusDuration ?? 0
            );
            
            return action;
        }

        public void GenerateEnemies(int roomLevel)
        {
            if (!IsHostile) return;

            int enemyCount = Math.Max(1, (int)Math.Ceiling(roomLevel / 2.0));
            
            // Try to load enemy data from JSON first
            var jsonEnemies = LoadEnemyDataFromJson();
            if (jsonEnemies != null && jsonEnemies.Count > 0)
            {
                GenerateEnemiesFromJson(roomLevel, enemyCount, jsonEnemies);
                return;
            }
            
            // Fallback to hardcoded enemies
            var enemyTypes = Theme switch
            {
                "Forest" => new[] { 
                    new { Name = "Goblin", BaseHealth = 80, BaseStrength = 4, BaseAgility = 6, BaseTechnique = 2, Primary = PrimaryAttribute.Agility },
                    new { Name = "Bandit", BaseHealth = 85, BaseStrength = 6, BaseAgility = 8, BaseTechnique = 3, Primary = PrimaryAttribute.Agility },
                    new { Name = "Spider", BaseHealth = 75, BaseStrength = 3, BaseAgility = 10, BaseTechnique = 3, Primary = PrimaryAttribute.Agility }
                },
                "Lava" => new[] { 
                    new { Name = "Wraith", BaseHealth = 85, BaseStrength = 5, BaseAgility = 7, BaseTechnique = 6, Primary = PrimaryAttribute.Technique },
                    new { Name = "Slime", BaseHealth = 95, BaseStrength = 7, BaseAgility = 3, BaseTechnique = 2, Primary = PrimaryAttribute.Strength },
                    new { Name = "Bat", BaseHealth = 70, BaseStrength = 3, BaseAgility = 12, BaseTechnique = 4, Primary = PrimaryAttribute.Agility }
                },
                "Crypt" => new[] { 
                    new { Name = "Skeleton", BaseHealth = 85, BaseStrength = 6, BaseAgility = 4, BaseTechnique = 3, Primary = PrimaryAttribute.Strength },
                    new { Name = "Zombie", BaseHealth = 100, BaseStrength = 8, BaseAgility = 2, BaseTechnique = 1, Primary = PrimaryAttribute.Strength },
                    new { Name = "Wraith", BaseHealth = 85, BaseStrength = 5, BaseAgility = 7, BaseTechnique = 6, Primary = PrimaryAttribute.Technique }
                },
                "Cavern" => new[] { 
                    new { Name = "Orc", BaseHealth = 95, BaseStrength = 10, BaseAgility = 4, BaseTechnique = 2, Primary = PrimaryAttribute.Strength },
                    new { Name = "Bat", BaseHealth = 70, BaseStrength = 3, BaseAgility = 12, BaseTechnique = 4, Primary = PrimaryAttribute.Agility },
                    new { Name = "Slime", BaseHealth = 95, BaseStrength = 7, BaseAgility = 3, BaseTechnique = 2, Primary = PrimaryAttribute.Strength }
                },
                "Swamp" => new[] { 
                    new { Name = "Slime", BaseHealth = 95, BaseStrength = 7, BaseAgility = 3, BaseTechnique = 2, Primary = PrimaryAttribute.Strength },
                    new { Name = "Spider", BaseHealth = 75, BaseStrength = 3, BaseAgility = 10, BaseTechnique = 3, Primary = PrimaryAttribute.Agility },
                    new { Name = "Cultist", BaseHealth = 85, BaseStrength = 4, BaseAgility = 5, BaseTechnique = 8, Primary = PrimaryAttribute.Technique }
                },
                "Desert" => new[] { 
                    new { Name = "Bandit", BaseHealth = 85, BaseStrength = 6, BaseAgility = 8, BaseTechnique = 3, Primary = PrimaryAttribute.Agility },
                    new { Name = "Cultist", BaseHealth = 85, BaseStrength = 4, BaseAgility = 5, BaseTechnique = 8, Primary = PrimaryAttribute.Technique },
                    new { Name = "Wraith", BaseHealth = 85, BaseStrength = 5, BaseAgility = 7, BaseTechnique = 6, Primary = PrimaryAttribute.Technique }
                },
                "Ice" => new[] { 
                    new { Name = "Wraith", BaseHealth = 85, BaseStrength = 5, BaseAgility = 7, BaseTechnique = 6, Primary = PrimaryAttribute.Technique },
                    new { Name = "Skeleton", BaseHealth = 85, BaseStrength = 6, BaseAgility = 4, BaseTechnique = 3, Primary = PrimaryAttribute.Strength },
                    new { Name = "Bat", BaseHealth = 70, BaseStrength = 3, BaseAgility = 12, BaseTechnique = 4, Primary = PrimaryAttribute.Agility }
                },
                "Ruins" => new[] { 
                    new { Name = "Cultist", BaseHealth = 85, BaseStrength = 4, BaseAgility = 5, BaseTechnique = 8, Primary = PrimaryAttribute.Technique },
                    new { Name = "Skeleton", BaseHealth = 85, BaseStrength = 6, BaseAgility = 4, BaseTechnique = 3, Primary = PrimaryAttribute.Strength },
                    new { Name = "Bandit", BaseHealth = 85, BaseStrength = 6, BaseAgility = 8, BaseTechnique = 3, Primary = PrimaryAttribute.Agility }
                },
                "Castle" => new[] { 
                    new { Name = "Bandit", BaseHealth = 85, BaseStrength = 6, BaseAgility = 8, BaseTechnique = 3, Primary = PrimaryAttribute.Agility },
                    new { Name = "Cultist", BaseHealth = 85, BaseStrength = 4, BaseAgility = 5, BaseTechnique = 8, Primary = PrimaryAttribute.Technique },
                    new { Name = "Wraith", BaseHealth = 85, BaseStrength = 5, BaseAgility = 7, BaseTechnique = 6, Primary = PrimaryAttribute.Technique }
                },
                "Graveyard" => new[] { 
                    new { Name = "Zombie", BaseHealth = 100, BaseStrength = 8, BaseAgility = 2, BaseTechnique = 1, Primary = PrimaryAttribute.Strength },
                    new { Name = "Wraith", BaseHealth = 85, BaseStrength = 5, BaseAgility = 7, BaseTechnique = 6, Primary = PrimaryAttribute.Technique },
                    new { Name = "Skeleton", BaseHealth = 85, BaseStrength = 6, BaseAgility = 4, BaseTechnique = 3, Primary = PrimaryAttribute.Strength }
                },
                _ => new[] { 
                    new { Name = "Goblin", BaseHealth = 80, BaseStrength = 4, BaseAgility = 6, BaseTechnique = 2, Primary = PrimaryAttribute.Agility },
                    new { Name = "Orc", BaseHealth = 95, BaseStrength = 10, BaseAgility = 4, BaseTechnique = 2, Primary = PrimaryAttribute.Strength },
                    new { Name = "Skeleton", BaseHealth = 85, BaseStrength = 6, BaseAgility = 4, BaseTechnique = 3, Primary = PrimaryAttribute.Strength },
                    new { Name = "Bandit", BaseHealth = 85, BaseStrength = 6, BaseAgility = 8, BaseTechnique = 3, Primary = PrimaryAttribute.Agility },
                    new { Name = "Cultist", BaseHealth = 85, BaseStrength = 4, BaseAgility = 5, BaseTechnique = 8, Primary = PrimaryAttribute.Technique },
                    new { Name = "Spider", BaseHealth = 75, BaseStrength = 3, BaseAgility = 10, BaseTechnique = 3, Primary = PrimaryAttribute.Agility },
                    new { Name = "Slime", BaseHealth = 95, BaseStrength = 7, BaseAgility = 3, BaseTechnique = 2, Primary = PrimaryAttribute.Strength },
                    new { Name = "Bat", BaseHealth = 70, BaseStrength = 3, BaseAgility = 12, BaseTechnique = 4, Primary = PrimaryAttribute.Agility },
                    new { Name = "Zombie", BaseHealth = 100, BaseStrength = 8, BaseAgility = 2, BaseTechnique = 1, Primary = PrimaryAttribute.Strength },
                    new { Name = "Wraith", BaseHealth = 85, BaseStrength = 5, BaseAgility = 7, BaseTechnique = 6, Primary = PrimaryAttribute.Technique }
                }
            };
            
            var settings = GameSettings.Instance;
            for (int i = 0; i < enemyCount; i++)
            {
                int enemyLevel = Math.Max(1, roomLevel + random.Next(-1, 2));
                var enemyType = enemyTypes[random.Next(enemyTypes.Length)];
                
                // Apply difficulty multipliers
                int adjustedHealth = (int)(enemyType.BaseHealth * settings.EnemyHealthMultiplier);
                int adjustedStrength = (int)(enemyType.BaseStrength * settings.EnemyDamageMultiplier);
                int adjustedAgility = (int)(enemyType.BaseAgility * settings.EnemyDamageMultiplier);
                int adjustedTechnique = (int)(enemyType.BaseTechnique * settings.EnemyDamageMultiplier);
                
                var enemy = new Enemy(
                    $"{enemyType.Name} Lv{enemyLevel}", 
                    enemyLevel,
                    adjustedHealth,
                    adjustedStrength,
                    adjustedAgility,
                    adjustedTechnique,
                    0, // Base armor - will be scaled by level in Enemy constructor
                    enemyType.Primary
                );
                enemies.Add(enemy);
            }
        }

        public bool HasLivingEnemies()
        {
            return enemies.Any(e => e.IsAlive);
        }

        public Enemy? GetNextLivingEnemy()
        {
            return enemies.FirstOrDefault(e => e.IsAlive);
        }

        public bool ShouldEnvironmentAct()
        {
            return IsHostile && random.NextDouble() < 0.3; // 30% chance to act
        }

        public override string GetDescription()
        {
            string enemyInfo = "";
            if (enemies.Any())
            {
                var livingEnemies = enemies.Count(e => e.IsAlive);
                enemyInfo = $"\nThere are {livingEnemies} enemies present.";
            }
            return $"{Description}{enemyInfo}";
        }

        public override string ToString()
        {
            return $"{Name}: {GetDescription()}";
        }

        // Methods to apply passive and active effects
        public double ApplyPassiveEffect(double value)
        {
            if (PassiveEffectType == PassiveEffectType.DamageMultiplier)
                return value * PassiveEffectValue;
            return value;
        }

        public void ApplyActiveEffect(Character player, Enemy enemy)
        {
            if (ActiveEffectAction != null)
            {
                int dmg = ActiveEffectAction.BaseValue;
                player.TakeDamage(dmg);
                enemy.TakeDamage(dmg);
            }
        }

        private List<EnemyData>? LoadEnemyDataFromJson()
        {
            try
            {
                string jsonPath = Path.Combine("..", "GameData", "Enemies.json");
                if (File.Exists(jsonPath))
                {
                    string jsonContent = File.ReadAllText(jsonPath);
                    var enemies = System.Text.Json.JsonSerializer.Deserialize<List<EnemyData>>(jsonContent);
                    return enemies;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error loading enemy data from JSON: {ex.Message}");
            }
            return null;
        }

        private void GenerateEnemiesFromJson(int roomLevel, int enemyCount, List<EnemyData> enemyData)
        {
            var settings = GameSettings.Instance;
            for (int i = 0; i < enemyCount; i++)
            {
                int enemyLevel = Math.Max(1, roomLevel + random.Next(-1, 2));
                var enemyTemplate = enemyData[random.Next(enemyData.Count)];
                
                // Apply difficulty multipliers
                int adjustedHealth = (int)(enemyTemplate.BaseHealth * settings.EnemyHealthMultiplier);
                int adjustedStrength = (int)(enemyTemplate.BaseStrength * settings.EnemyDamageMultiplier);
                int adjustedAgility = (int)(enemyTemplate.BaseAgility * settings.EnemyDamageMultiplier);
                int adjustedTechnique = (int)(enemyTemplate.BaseTechnique * settings.EnemyDamageMultiplier);
                int adjustedArmor = (int)(enemyTemplate.BaseArmor * settings.EnemyHealthMultiplier);
                
                var enemy = new Enemy(
                    $"{enemyTemplate.Name} Lv{enemyLevel}", 
                    enemyLevel,
                    adjustedHealth,
                    adjustedStrength,
                    adjustedAgility,
                    adjustedTechnique,
                    adjustedArmor,
                    enemyTemplate.Primary
                );
                enemies.Add(enemy);
            }
        }
    }

    // Data class for JSON deserialization
    public class EnemyData
    {
        public string Name { get; set; } = "";
        public int Level { get; set; }
        public int Strength { get; set; }
        public int Agility { get; set; }
        public int Technique { get; set; }
        public int Armor { get; set; }
        public List<string> Actions { get; set; } = new List<string>();
        
        // Properties for compatibility with existing system
        public int BaseHealth => 80 + (Level * 10); // Base health calculation
        public int BaseStrength => Strength;
        public int BaseAgility => Agility;
        public int BaseTechnique => Technique;
        public int BaseArmor => Armor;
        public PrimaryAttribute Primary => PrimaryAttribute.Strength; // Default primary attribute
    }
} 