namespace RPGGame
{
    // Enemy data classes moved to EnemyData.cs

    public class Enemy : Character
    {
        public int GoldReward { get; private set; }
        public int XPReward { get; private set; }
        public PrimaryAttribute PrimaryAttribute { get; private set; }
        public int Armor { get; private set; }
        public bool IsLiving { get; private set; }
        public EnemyArchetype Archetype { get; private set; }
        public EnemyAttackProfile AttackProfile { get; private set; }
        public ColorOverride? ColorOverride { get; private set; }
        
        // DPS-based system properties
        public double TargetDPS { get; private set; }
        public double TargetDamage { get; private set; }
        public double TargetAttackSpeed { get; private set; }
        
        // Direct stat properties (for new system)
        public int Damage { get; private set; }
        public double AttackSpeed { get; private set; }

        // NEW: Combat manager for enemy-specific combat logic
        private readonly EnemyCombatManager _combatManager;

        /// <summary>
        /// Lab / harness only: when true, <see cref="Damage"/> and <see cref="AttackSpeed"/> drive combat instead of attributes.
        /// Real enemies from data always use the attribute constructor and leave this false.
        /// </summary>
        private readonly bool _usesDirectCombatStats;

        public Enemy(string? name = null, int level = 1, int maxHealth = 50, int strength = 8, int agility = 6, int technique = 4, int intelligence = 4, int armor = 0, PrimaryAttribute primaryAttribute = PrimaryAttribute.Strength, bool isLiving = true, EnemyArchetype? archetype = null)
            : base(name ?? "Unknown Enemy")
        {
            _usesDirectCombatStats = false;
            Level = level;
            PrimaryAttribute = primaryAttribute;
            IsLiving = isLiving;
            
            // Determine archetype if not specified using ArchetypeManager
            Archetype = archetype ?? ArchetypeManager.SuggestArchetypeForEnemy(name ?? "Unknown", strength, agility, technique, intelligence);
            AttackProfile = ArchetypeManager.GetArchetypeProfile(Archetype);
            
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
            
            // Scale rewards based on level and tuning config
            GoldReward = tuning.Progression.EnemyGoldBase + (level * tuning.Progression.EnemyGoldPerLevel);
            
            // Calculate XP reward with fallback to ensure it's never 0 and scales with level
            int baseXP = tuning.Progression.EnemyXPBase;
            if (baseXP <= 0)
            {
                baseXP = 25; // Fallback minimum if config is 0 or negative
            }
            int xpPerLevel = tuning.Progression.EnemyXPPerLevel;
            if (xpPerLevel <= 0)
            {
                xpPerLevel = 5; // Fallback to ensure higher level enemies give more XP
            }
            XPReward = baseXP + (level * xpPerLevel);

            // Initialize combat manager
            _combatManager = new EnemyCombatManager(this);

            ActionPool.Clear();
            AddDefaultActions();
        }

        // New constructor for direct stat system
        public Enemy(string? name = null, int level = 1, int maxHealth = 50, int damage = 8, int armor = 0, double attackSpeed = 1.0, PrimaryAttribute primaryAttribute = PrimaryAttribute.Strength, bool isLiving = true, EnemyArchetype? archetype = null, bool useDirectStats = true)
            : base(name ?? "Unknown Enemy")
        {
            _usesDirectCombatStats = useDirectStats;
            Level = level;
            PrimaryAttribute = primaryAttribute;
            IsLiving = isLiving;
            Archetype = archetype ?? EnemyArchetype.Berserker;
            AttackProfile = ArchetypeManager.GetArchetypeProfile(Archetype);
            
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
            
            // Calculate XP reward with fallback to ensure it's never 0 and scales with level
            int baseXP = tuning.Progression.EnemyXPBase;
            if (baseXP <= 0)
            {
                baseXP = 25; // Fallback minimum if config is 0 or negative
            }
            int xpPerLevel = tuning.Progression.EnemyXPPerLevel;
            if (xpPerLevel <= 0)
            {
                xpPerLevel = 5; // Fallback to ensure higher level enemies give more XP
            }
            XPReward = baseXP + (level * xpPerLevel);

            // Initialize combat manager
            _combatManager = new EnemyCombatManager(this);

            ActionPool.Clear();
            AddDefaultActions();
        }

        private void AddDefaultActions()
        {
            // Use simpler base values - the unified damage system will handle scaling
            var jab = new Action(
                "Jab",
                ActionType.Attack,
                TargetType.SingleTarget,
                cooldown: 0,
                description: "A quick jab"
            );

            var specialAttack = new Action(
                "Special Attack",
                ActionType.Attack,
                TargetType.SingleTarget,
                cooldown: 2,
                description: "A powerful special attack"
            );

            // Weighted action selection based on level and primary attribute
            if (Level >= 5)
            {
                // Higher level enemies get access to special attacks
                AddAction(jab, 0.6);
                AddAction(specialAttack, 0.4);
            }
            else
            {
                // Lower level enemies use simpler attacks
                AddAction(jab, 1.0);
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
        /// True for lab / harness enemies constructed with direct damage and attack speed instead of attributes.
        /// </summary>
        public bool UsesDirectCombatStats() => _usesDirectCombatStats;

        /// <summary>
        /// Calculates enemy attack speed using direct stat or archetype modifiers
        /// </summary>
        public new double GetTotalAttackSpeed()
        {
            if (_usesDirectCombatStats)
                return AttackSpeed;

            return CombatCalculator.CalculateAttackSpeed(this);
        }

        /// <summary>
        /// Gets intelligence roll bonus for enemies (same as heroes: +1 per 10 INT)
        /// </summary>
        public new int GetIntelligenceRollBonus()
        {
            var tuning = GameConfiguration.Instance;
            // Prevent divide by zero - if IntelligenceRollBonusPer is 0 or not configured, return 0
            if (tuning.Attributes.IntelligenceRollBonusPer <= 0)
            {
                return 0;
            }
            return Intelligence / tuning.Attributes.IntelligenceRollBonusPer; // Every X points of INT gives +1 to rolls
        }

        /// <summary>
        /// Gets combo amplification for enemies (same as heroes: based on Technique)
        /// </summary>
        public new double GetComboAmplifier()
        {
            var tuning = GameConfiguration.Instance;
            return ComboAmplifierFromTechnique.Compute(Technique, tuning.ComboSystem);
        }

        /// <summary>HUD: sheet <c>DAMAGE_MOD</c> (percent points) queued on this enemy for their next attack.</summary>
        public double PeekQueuedSheetEnemyDamageModPercentForDisplay() =>
            CharacterEffectsState.PeekSheetDamageModPercentQueuedForNextEnemyAttack(this);

        /// <summary>
        /// Gets effective strength for enemies (same as heroes: used for damage)
        /// </summary>
        public new int GetEffectiveStrength()
        {
            if (_usesDirectCombatStats)
                return Damage;
            return Strength;
        }

        /// <summary>
        /// Gets current combo amplification for enemies (same as heroes)
        /// Step 0 adds no bonus (1.0x), bonus starts at Step 1+
        /// </summary>
        public new double GetCurrentComboAmplification()
        {
            var comboActions = GetComboActions();
            if (comboActions.Count == 0) return GetComboAmplifier();
            
            int currentStep = ComboStep % comboActions.Count;
            double baseAmp = GetComboAmplifier();
            var currentAction = comboActions[currentStep];
            int amplificationStep = ActionUtilities.GetComboAmplificationExponent(this, currentAction, comboActions);
            return Math.Pow(baseAmp, amplificationStep);
        }

        /// <summary>
        /// Gets the archetype-modified damage multiplier for this enemy (delegated to ArchetypeManager)
        /// </summary>
        public double GetArchetypeDamageMultiplier()
        {
            return ArchetypeManager.GetArchetypeDamageMultiplier(AttackProfile);
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
        /// Attempts multiple actions based on attack speed (delegated to combat manager)
        /// </summary>
        public (string result, bool success) AttemptMultiAction(Character target, Environment? environment = null)
        {
            return _combatManager.AttemptMultiAction(target, environment);
        }

        /// <summary>
        /// Attempts a single action against a target (delegated to combat manager)
        /// </summary>
        public (string result, bool success) AttemptAction(Character target, Environment? environment = null)
        {
            return _combatManager.AttemptAction(target, environment);
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
            // Apply damage reduction if active (now inherited from Actor base class)
            if (DamageReduction > 0)
            {
                amount = (int)(amount * (1.0 - DamageReduction));
            }
            
            CurrentHealth = Math.Max(0, CurrentHealth - amount);
            
            // Check for health milestones and leadership changes
            // Note: Health milestone checking is now handled by CombatManager
            return new List<string>(); // Return empty list since milestone checking moved to CombatManager
        }

        public override int GetMaxHealthForPoisonDot() => MaxHealth;

        /// <summary>Undead are immune to poison % DoT.</summary>
        public override int ProcessPoison(double currentTime)
        {
            if (!IsLiving)
                return 0;
            return base.ProcessPoison(currentTime);
        }

        /// <summary>Undead are immune to bleed.</summary>
        public override int ProcessBleedOnAction()
        {
            if (!IsLiving)
                return 0;
            return base.ProcessBleedOnAction();
        }

        // Archetype-related methods moved to ArchetypeManager
    }
} 
