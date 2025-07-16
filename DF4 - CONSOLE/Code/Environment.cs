using System;
using System.Collections.Generic;
using System.Linq;

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
            bool hasAction = false;
            // Theme-specific passive/active effects and actions
            switch (Theme)
            {
                case "Forest":
                    var poisonSpores = new Action(
                        name: "Poison Spores",
                        description: "Clouds of toxic spores fill the air!",
                        type: ActionType.Debuff,
                        targetType: TargetType.AreaOfEffect,
                        baseValue: 4,
                        cooldown: 3
                    );
                    AddAction(poisonSpores, 0.7);
                    hasAction = true;
                    break;
                case "Lava":
                    var lavaOverflow = new Action(
                        name: "Lava Overflow",
                        description: "Lava surges, burning all combatants!",
                        type: ActionType.Attack,
                        targetType: TargetType.AreaOfEffect,
                        baseValue: 10,
                        cooldown: 3
                    );
                    AddAction(lavaOverflow, 0.7);
                    hasAction = true;
                    break;
                case "Crypt":
                    var hauntingWail = new Action(
                        name: "Haunting Wail",
                        description: "A ghostly wail chills you to the bone!",
                        type: ActionType.Debuff,
                        targetType: TargetType.AreaOfEffect,
                        baseValue: 5,
                        cooldown: 3
                    );
                    AddAction(hauntingWail, 0.7);
                    hasAction = true;
                    break;
                // Add more themes as needed
            }
            // Fallback to default if no theme action
            if (!hasAction)
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
        }

        public void GenerateEnemies(int roomLevel)
        {
            if (!IsHostile) return;

            int enemyCount = Math.Max(1, (int)Math.Ceiling(roomLevel / 2.0));
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
    }
} 