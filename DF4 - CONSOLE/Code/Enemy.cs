namespace RPGGame
{
    public enum PrimaryAttribute
    {
        Strength,
        Agility,
        Technique
    }

    public class Enemy : Character
    {
        public int GoldReward { get; private set; }
        public int XPReward { get; private set; }
        public PrimaryAttribute PrimaryAttribute { get; private set; }

        public Enemy(string? name = null, int level = 1, int maxHealth = 50, int strength = 8, int agility = 6, int technique = 4, PrimaryAttribute primaryAttribute = PrimaryAttribute.Strength)
            : base(name ?? FlavorText.GenerateEnemyName())
        {
            Level = level;
            PrimaryAttribute = primaryAttribute;
            
            // Scale health and stats based on level
            MaxHealth = maxHealth + (level * 3);  // +3 health per level (same as heroes)
            CurrentHealth = MaxHealth;
            
            // Base scaling: +2 per level for all attributes
            Strength = strength + (level * 2);
            Agility = agility + (level * 2);
            Technique = technique + (level * 2);
            
            // Primary attribute gets +1 extra per level
            switch (PrimaryAttribute)
            {
                case PrimaryAttribute.Strength:
                    Strength += level;
                    break;
                case PrimaryAttribute.Agility:
                    Agility += level;
                    break;
                case PrimaryAttribute.Technique:
                    Technique += level;
                    break;
            }

            // Scale rewards based on level
            GoldReward = 5 + (level * 3);
            XPReward = 10 + (level * 5);

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
            return $"Level {Level} Enemy (Health: {CurrentHealth}/{MaxHealth}) (STR: {Strength}, AGI: {Agility}, TEC: {Technique}) Primary: {primaryAttr} (Reward: {GoldReward} gold, {XPReward} XP)";
        }

        public override string ToString()
        {
            return base.ToString();
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
                    int finalEffect = Combat.CalculateDamage(this, target, action, 1.0, settings.EnemyDamageMultiplier);
                    
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
                    int finalEffect = Combat.CalculateDamage(this, target, action, 1.0, settings.EnemyDamageMultiplier);
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