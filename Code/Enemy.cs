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

        public Enemy(string? name = null, int level = 1, int maxHealth = 50, int strength = 8, int agility = 6, int technique = 4, int intelligence = 4, int armor = 0, PrimaryAttribute primaryAttribute = PrimaryAttribute.Strength)
            : base(name ?? FlavorText.GenerateEnemyName())
        {
            Level = level;
            PrimaryAttribute = primaryAttribute;
            
            var tuning = TuningConfig.Instance;
            
            // Scale health and stats based on level and tuning config
            MaxHealth = maxHealth + (level * tuning.Character.EnemyHealthPerLevel);
            CurrentHealth = MaxHealth;
            
            // Base scaling based on tuning config
            Strength = strength + (level * tuning.Attributes.EnemyAttributesPerLevel);
            Agility = agility + (level * tuning.Attributes.EnemyAttributesPerLevel);
            Technique = technique + (level * tuning.Attributes.EnemyAttributesPerLevel);
            Intelligence = intelligence + (level * tuning.Attributes.EnemyAttributesPerLevel);
            
            // Scale armor based on tuning config
            Armor = armor + (level * tuning.Combat.EnemyArmorPerLevel);
            
            // Primary attribute gets extra bonus per level based on tuning config
            switch (PrimaryAttribute)
            {
                case PrimaryAttribute.Strength:
                    Strength += level * tuning.Attributes.EnemyPrimaryAttributeBonus;
                    break;
                case PrimaryAttribute.Agility:
                    Agility += level * tuning.Attributes.EnemyPrimaryAttributeBonus;
                    break;
                case PrimaryAttribute.Technique:
                    Technique += level * tuning.Attributes.EnemyPrimaryAttributeBonus;
                    break;
                case PrimaryAttribute.Intelligence:
                    Intelligence += level * tuning.Attributes.EnemyPrimaryAttributeBonus;
                    break;
            }

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
                        string actionResult = $"[{Name}] uses [{action.Name}] on [{target.Name}]: deals {finalEffect} damage. (Rolled {roll}, need {difficulty})";
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
                    
                    // Check narrative balance setting - if 0, show action messages
                    var narrativeSettings = GameSettings.Instance;
                    if (narrativeSettings.NarrativeBalance <= 0.0)
                    {
                        string actionResult = $"[{Name}] attempts [{action.Name}] but fails. (Rolled {roll}, need {difficulty}) No action performed.";
                        return (actionResult, false);
                    }
                    return ("", false); // Return empty string in narrative mode, success = false
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
                        return ($"[{Name}] uses [{action.Name}] on [{target.Name}]: deals {finalEffect} damage. (Rolled {roll}, need {difficulty})", true);
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
    }
} 