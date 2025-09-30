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
        
        // Direct stat properties (for new system)
        public int Damage { get; private set; }
        public double AttackSpeed { get; private set; }

        public Enemy(string? name = null, int level = 1, int maxHealth = 50, int strength = 8, int agility = 6, int technique = 4, int intelligence = 4, int armor = 0, PrimaryAttribute primaryAttribute = PrimaryAttribute.Strength, bool isLiving = true, EnemyArchetype? archetype = null)
            : base(name ?? "Unknown Enemy")
        {
            Level = level;
            PrimaryAttribute = primaryAttribute;
            IsLiving = isLiving;
            
            // Determine archetype if not specified
            Archetype = archetype ?? EnemyDPSCalculator.SuggestArchetypeForEnemy(name ?? "Unknown", strength, agility, technique, intelligence);
            AttackProfile = EnemyDPSCalculator.GetArchetypeProfile(Archetype);
            
            var tuning = GameConfiguration.Instance;
            
            // Use the calculated health directly (no additional scaling)
            MaxHealth = maxHealth;
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

        // New constructor for direct stat system
        public Enemy(string? name = null, int level = 1, int maxHealth = 50, int damage = 8, int armor = 0, double attackSpeed = 1.0, PrimaryAttribute primaryAttribute = PrimaryAttribute.Strength, bool isLiving = true, EnemyArchetype? archetype = null, bool useDirectStats = true)
            : base(name ?? "Unknown Enemy")
        {
            Level = level;
            PrimaryAttribute = primaryAttribute;
            IsLiving = isLiving;
            Archetype = archetype ?? EnemyArchetype.Warrior;
            AttackProfile = EnemyDPSCalculator.GetArchetypeProfile(Archetype);
            
            var tuning = GameConfiguration.Instance;
            
            // Use direct stats
            MaxHealth = maxHealth;
            CurrentHealth = MaxHealth;
            Damage = damage;
            Armor = armor;
            AttackSpeed = attackSpeed;
            
            // Set legacy attributes to 0 since we're using direct stats
            Strength = 0;
            Agility = 0;
            Technique = 0;
            Intelligence = 0;
            
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
        /// Gets total armor for enemies (uses the Armor property)
        /// </summary>
        public new int GetTotalArmor()
        {
            return Armor;
        }

        /// <summary>
        /// Calculates enemy attack speed using direct stat or archetype modifiers
        /// </summary>
        public new double GetTotalAttackSpeed()
        {
            // If using direct stat system, return the direct attack speed
            if (Damage > 0 && Strength == 0 && Agility == 0)
            {
                return AttackSpeed;
            }
            
            // Otherwise use shared attack speed calculation logic
            return CombatCalculator.CalculateAttackSpeed(this);
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
            
            // Simplified combat logic - narrative mode handling moved to CombatManager
            if (roll >= difficulty)
            {
                var settings = GameSettings.Instance;
                int finalEffect = CombatCalculator.CalculateDamage(this, target, action, 1.0, settings.EnemyDamageMultiplier, 0, roll, false);
                
                if (action.Type == ActionType.Attack)
                {
                    target.TakeDamage(finalEffect);
                    // Use the same parameters as the actual damage calculation to avoid duplicate weakened messages
                    int actualDamage = CombatCalculator.CalculateDamage(this, target, action, 1.0, settings.EnemyDamageMultiplier, 0, roll, false);
                    string damageDisplay = CombatResults.FormatDamageDisplay(this, target, finalEffect, actualDamage, action, 1.0, settings.EnemyDamageMultiplier, 0, roll);
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
        
        /// <summary>
        /// Override TakeDamage to add health milestone tracking for enemies
        /// </summary>
        public new void TakeDamage(int amount)
        {
            TakeDamageWithNotifications(amount);
        }
        
        public new List<string> TakeDamageWithNotifications(int amount)
        {
            // Apply damage reduction if active (now inherited from Entity base class)
            if (DamageReduction > 0)
            {
                amount = (int)(amount * (1.0 - DamageReduction));
            }
            
            CurrentHealth = Math.Max(0, CurrentHealth - amount);
            
            // Check for health milestones and leadership changes
            // Note: Health milestone checking is now handled by CombatManager
            return new List<string>(); // Return empty list since milestone checking moved to CombatManager
        }

        /// <summary>
        /// Override ProcessPoison to prevent undead enemies from taking bleed/poison damage
        /// </summary>
        /// <param name="currentTime">Current game time in seconds</param>
        /// <returns>Damage taken from poison this tick (0 for undead)</returns>
        public override int ProcessPoison(double currentTime)
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