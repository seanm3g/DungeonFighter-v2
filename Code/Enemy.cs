namespace RPGGame
{
    public enum PrimaryAttribute
    {
        Strength,
        Agility,
        Technique,
        Intelligence
    }

    public class Enemy : Character
    {
        public int GoldReward { get; private set; }
        public int XPReward { get; private set; }
        public PrimaryAttribute PrimaryAttribute { get; private set; }
        public int Armor { get; private set; }
        public bool IsLiving { get; private set; }
        public EnemyArchetype Archetype { get; private set; }
        public EnemyAttackProfile AttackProfile { get; private set; }
        
        // DPS-based system properties
        public double TargetDPS { get; private set; }
        public double TargetDamage { get; private set; }
        public double TargetAttackSpeed { get; private set; }

        public Enemy(string? name = null, int level = 1, int maxHealth = 50, int strength = 8, int agility = 6, int technique = 4, int intelligence = 4, int armor = 0, PrimaryAttribute primaryAttribute = PrimaryAttribute.Strength, bool isLiving = true, EnemyArchetype? archetype = null)
            : base(name ?? "Unknown Enemy")
        {
            Level = level;
            PrimaryAttribute = primaryAttribute;
            IsLiving = isLiving;
            
            // Determine archetype if not specified
            Archetype = archetype ?? EnemyDPSCalculator.SuggestArchetypeForEnemy(name ?? "Unknown", strength, agility, technique, intelligence);
            AttackProfile = EnemyDPSCalculator.GetArchetypeProfile(Archetype);
            
            var tuning = TuningConfig.Instance;
            
            // Scale health and stats based on level and tuning config
            MaxHealth = maxHealth + (level * tuning.Character.EnemyHealthPerLevel);
            CurrentHealth = MaxHealth;
            
            // Use the stats as provided (they should already be calculated for target DPS)
            Strength = strength;
            Agility = agility;
            Technique = technique;
            Intelligence = intelligence;
            
            // Set armor from constructor parameter
            Armor = armor;
            
            // Primary attribute bonus is already included in the calculated stats

            // Scale rewards based on level and tuning config
            GoldReward = tuning.Progression.EnemyGoldBase + (level * tuning.Progression.EnemyGoldPerLevel);
            XPReward = tuning.Progression.EnemyXPBase + (level * tuning.Progression.EnemyXPPerLevel);

            ActionPool.Clear();
            AddDefaultActions();
        }

        private void AddDefaultActions()
        {
            // Use simpler base values - the unified damage system will handle scaling
            var basicAttack = new Action(
                "Basic Attack",
                ActionType.Attack,
                TargetType.SingleTarget,
                baseValue: 8,  // Base damage - unified system will add weapon + strength
                range: 1,
                description: "A basic attack"
            );

            var jab = new Action(
                "Jab",
                ActionType.Attack,
                TargetType.SingleTarget,
                baseValue: 5,  // Base damage - unified system will add weapon + agility/2
                range: 1,
                description: "A quick jab"
            );

            var specialAttack = new Action(
                "Special Attack",
                ActionType.Attack,
                TargetType.SingleTarget,
                baseValue: 12,  // Base damage - unified system will add weapon + technique
                range: 1,
                cooldown: 2,
                description: "A powerful special attack"
            );

            // Weighted action selection based on level and primary attribute
            if (Level >= 5)
            {
                // Higher level enemies get access to special attacks
                AddAction(basicAttack, 0.5);
                AddAction(jab, 0.3);
                AddAction(specialAttack, 0.2);
            }
            else
            {
                // Lower level enemies use basic attacks
                AddAction(basicAttack, 0.7);
                AddAction(jab, 0.3);
            }
        }

        public override string GetDescription()
        {
            string primaryAttr = PrimaryAttribute.ToString();
            return $"Level {Level} Enemy (Health: {CurrentHealth}/{MaxHealth}) (STR: {Strength}, AGI: {Agility}, TEC: {Technique}, INT: {Intelligence}) Primary: {primaryAttr} (Reward: {GoldReward} gold, {XPReward} XP)";
        }

        public override string ToString()
        {
            return base.ToString();
        }

        /// <summary>
        /// Calculates enemy attack speed using archetype modifiers
        /// </summary>
        public new double GetTotalAttackSpeed()
        {
            var tuning = TuningConfig.Instance;
            
            // Base calculation similar to Character.GetTotalAttackSpeed
            double baseAttackTime = tuning.Combat.BaseAttackTime;
            double agilityReduction = Agility * tuning.Combat.AgilitySpeedReduction;
            double finalAttackTime = baseAttackTime - agilityReduction;
            
            // Apply archetype speed multiplier
            finalAttackTime *= AttackProfile.SpeedMultiplier;
            
            // Apply slow debuff if active
            if (SlowTurns > 0)
            {
                finalAttackTime *= SlowMultiplier;
            }
            
            // Apply minimum cap
            return Math.Max(tuning.Combat.MinimumAttackTime, finalAttackTime);
        }

        /// <summary>
        /// Gets the archetype-modified damage multiplier for this enemy
        /// </summary>
        public double GetArchetypeDamageMultiplier()
        {
            return AttackProfile.DamageMultiplier;
        }
        
        /// <summary>
        /// Sets target DPS values for the DPS-based system
        /// </summary>
        public void SetTargetDPS(double targetDPS)
        {
            TargetDPS = targetDPS;
        }
        
        public void SetTargetDamage(double targetDamage)
        {
            TargetDamage = targetDamage;
        }
        
        public void SetTargetAttackSpeed(double targetAttackSpeed)
        {
            TargetAttackSpeed = targetAttackSpeed;
        }

        /// <summary>
        /// Attempts multiple actions based on attack speed
        /// </summary>
        public (string result, bool success) AttemptMultiAction(Character target, Environment? environment = null)
        {
            int attacksPerTurn = GetAttacksPerTurn();
            var results = new List<string>();
            bool anySuccess = false;
            
            for (int i = 0; i < attacksPerTurn; i++)
            {
                if (!target.IsAlive) break; // Stop if target is dead
                
                var (result, success) = AttemptAction(target, environment);
                if (!string.IsNullOrEmpty(result))
                {
                    results.Add(result);
                }
                if (success) anySuccess = true;
            }
            return (string.Join("\n", results), anySuccess);
        }

        // Add a method to Enemy to handle action selection with a roll threshold
        public (string result, bool success) AttemptAction(Character target, Environment? environment = null)
        {
            var availableActions = new List<Action>();
            foreach (var entry in ActionPool)
            {
                availableActions.Add(entry.action);
            }
            if (availableActions.Count == 0)
                return ($"{Name} has no available actions!", false);
            
            // Select action based on weights
            var action = SelectAction();
            if (action == null)
                return ($"{Name} has no available actions!", false);
                
            int roll = Dice.Roll(20);
            int difficulty = 8 + (Level / 2);  // Higher level enemies have better accuracy
            
            // Check if we're in narrative mode
            if (Combat.IsInNarrativeMode())
            {
                if (roll >= difficulty)
                {
                    var settings = GameSettings.Instance;
                    int finalEffect = Combat.CalculateDamage(this, target, action, 1.0, settings.EnemyDamageMultiplier, 0, roll);
                    
                    var evt = new BattleEvent
                    {
                        Actor = Name,
                        Target = target.Name,
                        Action = action.Name,
                        Damage = 0,
                        IsSuccess = true,
                        Roll = roll,
                        Difficulty = difficulty
                    };

                    if (action.Type == ActionType.Attack)
                    {
                        target.TakeDamage(finalEffect);
                        evt.Damage = finalEffect;
                    }
                    else if (action.Type == ActionType.Debuff)
                    {
                        // Handle debuff effects if needed
                    }

                    Combat.AddBattleEvent(evt);
                    
                    // Check narrative balance setting - if 0, show action messages
                    var narrativeSettings = GameSettings.Instance;
                    if (narrativeSettings.NarrativeBalance <= 0.0)
                    {
                        // Use the same parameters as the actual damage calculation to avoid duplicate weakened messages
                        int actualDamage = Combat.CalculateDamage(this, target, action, 1.0, settings.EnemyDamageMultiplier, 0, roll, false);
                        string damageDisplay = Combat.FormatDamageDisplay(this, target, finalEffect, actualDamage, action, 1.0, settings.EnemyDamageMultiplier, 0, roll);
                        string actionResult = $"[{Name}] uses [{action.Name}] on [{target.Name}]: deals {damageDisplay}. (Rolled {roll}, need {difficulty})";
                        return (actionResult, true);
                    }
                    return ("", true); // Return empty string in narrative mode, success = true
                }
                else
                {
                    var evt = new BattleEvent
                    {
                        Actor = Name,
                        Target = target.Name,
                        Action = action.Name,
                        IsSuccess = false,
                        Roll = roll,
                        Difficulty = difficulty
                    };

                    Combat.AddBattleEvent(evt);
                    
                    // Always show failed attacks, regardless of narrative mode
                    string actionResult = $"[{Name}] attempts [{action.Name}] but fails. (Rolled {roll}, need {difficulty}) No action performed.";
                    return (actionResult, false);
                }
            }
            else
            {
                // Fallback to old message format if not using narrative mode
                if (roll >= difficulty)
                {
                    var settings = GameSettings.Instance;
                    int finalEffect = Combat.CalculateDamage(this, target, action, 1.0, settings.EnemyDamageMultiplier, 0, roll);
                    if (action.Type == ActionType.Attack)
                    {
                        target.TakeDamage(finalEffect);
                        // Use the same parameters as the actual damage calculation to avoid duplicate weakened messages
                        int actualDamage = Combat.CalculateDamage(this, target, action, 1.0, settings.EnemyDamageMultiplier, 0, roll, false);
                        string damageDisplay = Combat.FormatDamageDisplay(this, target, finalEffect, actualDamage, action, 1.0, settings.EnemyDamageMultiplier, 0, roll);
                        return ($"[{Name}] uses [{action.Name}] on [{target.Name}]: deals {damageDisplay}. (Rolled {roll}, need {difficulty})", true);
                    }
                    else if (action.Type == ActionType.Debuff)
                    {
                        return ($"[{Name}] uses [{action.Name}] on [{target.Name}]: applies debuff. (Rolled {roll}, need {difficulty})", true);
                    }
                    return ($"[{Name}] uses [{action.Name}] on [{target.Name}]. (Rolled {roll}, need {difficulty})", true);
                }
                else
                {
                    return ($"[{Name}] attempts [{action.Name}] but fails. (Rolled {roll}, need {difficulty}) No action performed.", false);
                }
            }
        }
        
        /// <summary>
        /// Override TakeDamage to add health milestone tracking for enemies
        /// </summary>
        public new void TakeDamage(int amount)
        {
            TakeDamageWithNotifications(amount);
        }
        
        public new List<string> TakeDamageWithNotifications(int amount)
        {
            // Apply damage reduction if active
            if (DamageReduction > 0)
            {
                amount = (int)(amount * (1.0 - DamageReduction));
            }
            
            CurrentHealth = Math.Max(0, CurrentHealth - amount);
            
            // Check for health milestones and leadership changes
            return Combat.CheckHealthMilestones(this, amount);
        }

        /// <summary>
        /// Override ProcessPoison to prevent undead enemies from taking bleed/poison damage
        /// </summary>
        /// <param name="currentTime">Current game time in seconds</param>
        /// <returns>Damage taken from poison this tick (0 for undead)</returns>
        public new int ProcessPoison(double currentTime)
        {
            // Undead enemies are immune to poison and bleed damage
            if (!IsLiving)
            {
                return 0;
            }
            
            // For living enemies, use the base class implementation
            return base.ProcessPoison(currentTime);
        }
    }
} 