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
                            action.Tags != null && 
                            action.Tags.Contains("environment") &&
                            (action.Tags.Contains(Theme.ToLower()) || action.Tags.Contains("generic"))
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
            // Create theme-specific environmental actions instead of generic fallback
            var themeActions = GetThemeSpecificActions();
            foreach (var action in themeActions)
            {
                AddAction(action, 0.7); // 70% probability for environmental actions
            }
        }

        private List<Action> GetThemeSpecificActions()
        {
            var actions = new List<Action>();
            
            switch (Theme.ToLower())
            {
                case "forest":
                    actions.Add(new Action("Falling Branch", ActionType.Attack, TargetType.AreaOfEffect, 8, 0, 0, "A heavy branch falls from above", -1, 1.5, 3.0, false, false, false, 0, 0));
                    actions.Add(new Action("Thorn Vines", ActionType.Attack, TargetType.AreaOfEffect, 5, 0, 0, "Thorny vines entangle and scratch", -1, 1.0, 2.0, false, false, false, 0, 0));
                    break;
                    
                case "lava":
                    actions.Add(new Action("Lava Splash", ActionType.Attack, TargetType.AreaOfEffect, 10, 0, 0, "Molten lava splashes from the ground", -1, 2.0, 4.0, false, false, false, 0, 0));
                    actions.Add(new Action("Steam Burst", ActionType.Attack, TargetType.AreaOfEffect, 6, 0, 0, "Scalding steam erupts from cracks", -1, 1.2, 2.0, false, false, false, 0, 0));
                    break;
                    
                case "crypt":
                    actions.Add(new Action("Curse of the Crypt", ActionType.Debuff, TargetType.AreaOfEffect, 0, 0, 0, "Ancient curse weakens all who enter", -1, 0.0, 5.0, false, false, false, 0, 0));
                    actions.Add(new Action("Ghostly Whisper", ActionType.Attack, TargetType.AreaOfEffect, 9, 0, 0, "Ethereal voices drain your life force", -1, 1.8, 3.0, false, false, false, 0, 0));
                    break;
                    
                case "crystal":
                    actions.Add(new Action("Crystal Shards", ActionType.Attack, TargetType.AreaOfEffect, 9, 0, 0, "Sharp crystal shards explode from the walls", -1, 1.8, 2.5, false, false, false, 0, 0));
                    actions.Add(new Action("Prism Refraction", ActionType.Attack, TargetType.AreaOfEffect, 7, 0, 0, "Light refracts through crystals, creating a blinding attack", -1, 1.5, 2.0, false, false, false, 0, 0));
                    break;
                    
                case "temple":
                    actions.Add(new Action("Divine Wrath", ActionType.Attack, TargetType.AreaOfEffect, 10, 0, 0, "The temple's divine power strikes down the unworthy", -1, 2.0, 3.5, false, false, false, 0, 0));
                    actions.Add(new Action("Sacred Flames", ActionType.Attack, TargetType.AreaOfEffect, 8, 0, 0, "Holy flames erupt from the temple's sacred braziers", -1, 1.6, 2.5, false, false, false, 0, 0));
                    break;
                    
                default: // Generic theme
                    actions.Add(new Action("Dungeon Collapse", ActionType.Attack, TargetType.AreaOfEffect, 5, 0, 0, "The unstable dungeon structure causes debris to fall", -1, 1.0, 2.0, false, false, false, 0, 0));
                    actions.Add(new Action("Poisonous Gas", ActionType.Attack, TargetType.AreaOfEffect, 4, 0, 0, "Toxic fumes fill the air, causing damage over time", -1, 0.8, 1.5, false, false, false, 0, 0));
                    break;
            }
            
            return actions;
        }

        private Action CreateActionFromData(ActionData data)
        {
            var actionType = Enum.TryParse<ActionType>(data.Type, true, out var parsedType) ? parsedType : ActionType.Attack;
            var targetType = TargetType.AreaOfEffect; // Environmental actions are always area of effect
            
            // Enhance description with modifiers
            string enhancedDescription = EnhanceActionDescription(data);
            
            var action = new Action(
                name: data.Name,
                type: actionType,
                targetType: targetType,
                baseValue: data.BaseValue,
                range: data.Range,
                cooldown: data.Cooldown,
                description: enhancedDescription,
                comboOrder: -1, // Default combo order
                damageMultiplier: data.DamageMultiplier,
                length: data.Length,
                causesBleed: data.CausesBleed,
                causesWeaken: data.CausesWeaken,
                isComboAction: false, // Default to false
                comboBonusAmount: data.ComboBonusAmount,
                comboBonusDuration: data.ComboBonusDuration
            );
            
            return action;
        }

        private string EnhanceActionDescription(ActionData data)
        {
            var modifiers = new List<string>();
            
            // Add roll bonus information
            if (data.RollBonus != 0)
            {
                string rollText = data.RollBonus > 0 ? $"+{data.RollBonus}" : data.RollBonus.ToString();
                modifiers.Add($"Roll: {rollText}");
            }
            
            // Add damage multiplier information
            if (data.DamageMultiplier != 1.0)
            {
                modifiers.Add($"Damage: {data.DamageMultiplier:F1}x");
            }
            
            // Add combo bonus information
            if (data.ComboBonusAmount > 0 && data.ComboBonusDuration > 0)
            {
                modifiers.Add($"Combo: +{data.ComboBonusAmount} for {data.ComboBonusDuration} turns");
            }
            
            // Add status effect information
            if (data.CausesBleed)
            {
                modifiers.Add("Causes Bleed");
            }
            
            if (data.CausesWeaken)
            {
                modifiers.Add("Causes Weaken");
            }
            
            // Add multi-hit information
            if (data.MultiHitCount > 1)
            {
                modifiers.Add($"Multi-hit: {data.MultiHitCount} attacks");
            }
            
            // Add self-damage information
            if (data.SelfDamagePercent > 0)
            {
                modifiers.Add($"Self-damage: {data.SelfDamagePercent}%");
            }
            
            // Add special effects
            if (data.SkipNextTurn)
            {
                modifiers.Add("Skips next turn");
            }
            
            if (data.RepeatLastAction)
            {
                modifiers.Add("Repeats last action");
            }
            
            // Combine base description with modifiers
            string result = data.Description;
            if (modifiers.Count > 0)
            {
                result += $" | {string.Join(", ", modifiers)}";
            }
            
            return result;
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
            
            // Fallback: Create basic enemies if JSON loading fails
            Console.WriteLine("Warning: Could not load enemy data from JSON, creating basic enemies");
            var tuning = TuningConfig.Instance;
            var basicEnemies = new[] { 
                new { Name = "Basic Enemy", BaseHealth = 80, BaseStrength = 8, BaseAgility = 6, BaseTechnique = 4, BaseIntelligence = 3, Primary = PrimaryAttribute.Strength }
            };
            
            for (int i = 0; i < enemyCount; i++)
            {
                int enemyLevel = Math.Max(1, roomLevel + random.Next(-tuning.EnemyScaling.EnemyLevelVariance, tuning.EnemyScaling.EnemyLevelVariance + 1));
                var enemyType = basicEnemies[random.Next(basicEnemies.Length)];
                
                // Apply difficulty multipliers from tuning config
                int adjustedHealth = (int)(enemyType.BaseHealth * tuning.EnemyScaling.EnemyHealthMultiplier);
                int adjustedStrength = (int)(enemyType.BaseStrength * tuning.EnemyScaling.EnemyDamageMultiplier);
                int adjustedAgility = (int)(enemyType.BaseAgility * tuning.EnemyScaling.EnemyDamageMultiplier);
                int adjustedTechnique = (int)(enemyType.BaseTechnique * tuning.EnemyScaling.EnemyDamageMultiplier);
                int adjustedIntelligence = (int)(enemyType.BaseIntelligence * tuning.EnemyScaling.EnemyDamageMultiplier);
                
                var enemy = new Enemy(
                    enemyType.Name, 
                    enemyLevel,
                    adjustedHealth,
                    adjustedStrength,
                    adjustedAgility,
                    adjustedTechnique,
                    adjustedIntelligence,
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
                string[] possiblePaths = {
                    Path.Combine("GameData", "Enemies.json"),
                    Path.Combine("..", "GameData", "Enemies.json"),
                    Path.Combine("..", "..", "GameData", "Enemies.json"),
                    Path.Combine("DF4 - CONSOLE", "GameData", "Enemies.json"),
                    Path.Combine("..", "DF4 - CONSOLE", "GameData", "Enemies.json")
                };

                string? foundPath = null;
                foreach (string path in possiblePaths)
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
                    var enemies = System.Text.Json.JsonSerializer.Deserialize<List<EnemyData>>(jsonContent);
                    return enemies;
                }
                else
                {
                    Console.WriteLine($"Warning: Enemies.json not found. Tried paths: {string.Join(", ", possiblePaths)}");
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
            var tuning = TuningConfig.Instance;
            
            // Filter enemies by theme if possible
            var themeEnemies = GetThemeAppropriateEnemies(enemyData);
            var availableEnemies = themeEnemies.Count > 0 ? themeEnemies : enemyData;
            
            for (int i = 0; i < enemyCount; i++)
            {
                int enemyLevel = Math.Max(1, roomLevel + random.Next(-tuning.EnemyScaling.EnemyLevelVariance, tuning.EnemyScaling.EnemyLevelVariance + 1));
                var enemyTemplate = availableEnemies[random.Next(availableEnemies.Count)];
                
                // Use EnemyLoader to create the enemy with proper actions loaded
                var enemy = EnemyLoader.CreateEnemy(enemyTemplate.Name, enemyLevel);
                if (enemy != null)
                {
                    enemies.Add(enemy);
                }
                else
                {
                    // Fallback: Create basic enemy if EnemyLoader fails
                    Console.WriteLine($"Warning: Could not create enemy {enemyTemplate.Name} from EnemyLoader, creating basic enemy");
                    var basicEnemy = new Enemy(
                        enemyTemplate.Name, 
                        enemyLevel,
                        80 + (enemyLevel * tuning.Character.EnemyHealthPerLevel),
                        enemyTemplate.Strength,
                        enemyTemplate.Agility,
                        enemyTemplate.Technique,
                        enemyTemplate.Intelligence,
                        enemyTemplate.Armor,
                        PrimaryAttribute.Strength
                    );
                    enemies.Add(basicEnemy);
                }
            }
        }

        private List<EnemyData> GetThemeAppropriateEnemies(List<EnemyData> allEnemies)
        {
            // Map themes to appropriate enemies based on the dungeon enemy lists
            var themeEnemyMap = new Dictionary<string, string[]>
            {
                ["Forest"] = new[] { "Goblin", "Spider", "Wolf", "Bear", "Treant" },
                ["Lava"] = new[] { "Wraith", "Slime", "Bat", "Fire Elemental", "Lava Golem", "Salamander" },
                ["Crypt"] = new[] { "Skeleton", "Zombie", "Wraith", "Lich", "Ghoul", "Wight" },
                ["Crystal"] = new[] { "Crystal Golem", "Prism Spider", "Shard Beast", "Crystal Sprite", "Geode Beast", "Crystal Wyrm" },
                ["Temple"] = new[] { "Stone Guardian", "Temple Warden", "Ancient Sentinel", "Temple Guard", "Priest", "Paladin" },
                ["Generic"] = new[] { "Bandit", "Orc", "Troll", "Kobold", "Goblin" }
            };

            if (themeEnemyMap.TryGetValue(Theme, out var themeEnemyNames))
            {
                return allEnemies.Where(e => themeEnemyNames.Contains(e.Name)).ToList();
            }

            // If theme not found, return all enemies
            return allEnemies;
        }
    }

} 