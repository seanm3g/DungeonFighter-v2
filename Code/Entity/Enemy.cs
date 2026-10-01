using System;
using System.Collections.Generic;
using System.Linq;

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
        public string Rarity { get; private set; } = "Common";

        /// <summary>Match/flavor tags from <see cref="EnemyData.Tags"/> plus derived substance tags.</summary>
        public IReadOnlyList<string> Tags => _tags;

        private readonly List<string> _tags = new List<string>();

        private int[]? _bodyMax;
        private int[]? _bodyCurrent;
        private int? _packHeadlineCount;

        /// <summary>True when this enemy is several bodies that share one combat role.</summary>
        public bool IsPack => _bodyMax != null && _bodyMax.Length > 1;

        /// <summary>Authored body count. Single enemies are 1.</summary>
        public int PackSize => _bodyMax?.Length ?? 1;

        /// <summary>Bodies with HP remaining. A single enemy reports 1 while alive.</summary>
        public int LivingBodyCount { get; private set; } = 1;

        /// <summary>Sum of per-body max HP. Matches <see cref="Character.MaxHealth"/> after <see cref="InitializePack"/>.</summary>
        public int PackMaxHealth
        {
            get
            {
                if (_bodyMax == null)
                    return MaxHealth;
                int sum = 0;
                for (int i = 0; i < _bodyMax.Length; i++)
                    sum += _bodyMax[i];
                return sum;
            }
        }

        /// <summary>Sum of per-body current HP.</summary>
        public int PackCurrentHealth
        {
            get
            {
                if (_bodyCurrent == null)
                    return 0;
                int sum = 0;
                for (int i = 0; i < _bodyCurrent.Length; i++)
                    sum += _bodyCurrent[i];
                return sum;
            }
        }

        /// <summary>HP left on the rightmost living body (the one a swing can hit).</summary>
        public int FrontBodyRemaining
        {
            get
            {
                int index = FrontBodyIndex;
                return index < 0 || _bodyCurrent == null ? 0 : _bodyCurrent[index];
            }
        }

        /// <summary>
        /// When set, combat-log names use this body count instead of <see cref="LivingBodyCount"/>
        /// so a killing swing still shows the count from the start of that swing.
        /// </summary>
        public int? PackHeadlineCount
        {
            get => _packHeadlineCount;
            set => _packHeadlineCount = value;
        }

        /// <summary>Damage past the front body on the last <see cref="DamageFrontBody"/> call.</summary>
        public int LastOverkillWasted { get; private set; }

        /// <summary>Fractions of max HP where black bar dividers sit (one per boundary between bodies).</summary>
        public double[] PackDividerFractions
        {
            get
            {
                if (!IsPack || _bodyMax == null || PackMaxHealth <= 0)
                    return Array.Empty<double>();
                var fractions = new double[_bodyMax.Length - 1];
                int cumulative = 0;
                int total = PackMaxHealth;
                for (int i = 0; i < fractions.Length; i++)
                {
                    cumulative += _bodyMax[i];
                    fractions[i] = (double)cumulative / total;
                }
                return fractions;
            }
        }

        /// <summary>
        /// Splits max HP into <paramref name="packSize"/> bodies. Values of 1 or less stay a single enemy.
        /// Remainder HP is added to the last bodies so the pack total stays <see cref="Character.MaxHealth"/>.
        /// </summary>
        public void InitializePack(int packSize)
        {
            packSize = Math.Max(1, packSize);
            if (packSize <= 1 || MaxHealth <= 0)
            {
                _bodyMax = null;
                _bodyCurrent = null;
                LivingBodyCount = CurrentHealth > 0 ? 1 : 0;
                return;
            }

            if (MaxHealth < packSize)
                packSize = MaxHealth;

            _bodyMax = SplitPackHealth(MaxHealth, packSize);
            _bodyCurrent = (int[])_bodyMax.Clone();
            RefreshLivingBodyCount();
        }

        /// <summary>Removes HP from the rightmost living body only. Extra damage is wasted.</summary>
        public int DamageFrontBody(int amount)
        {
            if (!IsPack || amount <= 0 || _bodyCurrent == null)
            {
                LastOverkillWasted = 0;
                return 0;
            }

            int index = FrontBodyIndex;
            if (index < 0)
            {
                LastOverkillWasted = amount;
                return 0;
            }

            int applied = Math.Min(amount, _bodyCurrent[index]);
            _bodyCurrent[index] -= applied;
            LastOverkillWasted = amount - applied;
            RefreshLivingBodyCount();
            return applied;
        }

        /// <summary>Heals living bodies from the wounded front backward. Dead bodies stay dead.</summary>
        public void HealLivingBodies(int amount)
        {
            if (!IsPack || amount <= 0 || _bodyCurrent == null || _bodyMax == null)
                return;

            for (int i = _bodyCurrent.Length - 1; i >= 0 && amount > 0; i--)
            {
                if (_bodyCurrent[i] <= 0)
                    continue;
                int room = _bodyMax[i] - _bodyCurrent[i];
                if (room <= 0)
                    continue;
                int add = Math.Min(room, amount);
                _bodyCurrent[i] += add;
                amount -= add;
            }
            RefreshLivingBodyCount();
        }

        /// <summary>Fills every body, including ones already at 0. Used when current HP is assigned to max.</summary>
        public void RestorePackFull()
        {
            if (!IsPack || _bodyCurrent == null || _bodyMax == null)
                return;
            for (int i = 0; i < _bodyCurrent.Length; i++)
                _bodyCurrent[i] = _bodyMax[i];
            RefreshLivingBodyCount();
        }

        public void KillAllBodies()
        {
            if (_bodyCurrent == null)
            {
                LivingBodyCount = 0;
                return;
            }
            Array.Clear(_bodyCurrent, 0, _bodyCurrent.Length);
            LivingBodyCount = 0;
        }

        /// <summary>Moves displayed HP toward <paramref name="requested"/> without spilling past one body or reviving.</summary>
        public void AssignPackHealth(int requested)
        {
            if (!IsPack)
                return;
            int current = PackCurrentHealth;
            if (requested >= PackMaxHealth)
                RestorePackFull();
            else if (requested <= 0)
                KillAllBodies();
            else if (requested < current)
                DamageFrontBody(current - requested);
            else if (requested > current)
                HealLivingBodies(requested - current);
        }

        /// <summary>Rebuilds body maxes when max HP changes. Dead bodies stay dead; full bodies stay full.</summary>
        public void ResizePack(int newMax)
        {
            if (!IsPack || _bodyMax == null || _bodyCurrent == null)
                return;
            int count = _bodyMax.Length;
            if (newMax < count)
                newMax = count;
            var oldMax = (int[])_bodyMax.Clone();
            var oldCurrent = (int[])_bodyCurrent.Clone();
            var nextMax = SplitPackHealth(newMax, count);
            for (int i = 0; i < count; i++)
            {
                if (oldCurrent[i] <= 0)
                    _bodyCurrent[i] = 0;
                else if (oldMax[i] <= 0 || oldCurrent[i] >= oldMax[i])
                    _bodyCurrent[i] = nextMax[i];
                else
                {
                    int scaled = (int)Math.Round(oldCurrent[i] * (double)nextMax[i] / oldMax[i]);
                    _bodyCurrent[i] = Math.Clamp(scaled, 1, nextMax[i]);
                }
                _bodyMax[i] = nextMax[i];
            }
            RefreshLivingBodyCount();
        }

        /// <summary>Suffix such as <c>(3x)</c> while any body is alive. Empty for a single enemy or a wiped pack.</summary>
        public string PackSuffix
        {
            get
            {
                if (!IsPack)
                    return "";
                int count = _packHeadlineCount ?? LivingBodyCount;
                return count >= 1 ? $"({count}x)" : "";
            }
        }

        private int FrontBodyIndex
        {
            get
            {
                if (_bodyCurrent == null)
                    return -1;
                for (int i = _bodyCurrent.Length - 1; i >= 0; i--)
                {
                    if (_bodyCurrent[i] > 0)
                        return i;
                }
                return -1;
            }
        }

        private void RefreshLivingBodyCount()
        {
            if (_bodyCurrent == null)
            {
                LivingBodyCount = 1;
                return;
            }
            int living = 0;
            for (int i = 0; i < _bodyCurrent.Length; i++)
            {
                if (_bodyCurrent[i] > 0)
                    living++;
            }
            LivingBodyCount = living;
        }

        private static int[] SplitPackHealth(int total, int count)
        {
            var sizes = new int[count];
            int baseSize = total / count;
            int remainder = total % count;
            for (int i = 0; i < count; i++)
                sizes[i] = baseSize + (i >= count - remainder ? 1 : 0);
            return sizes;
        }

        internal void SetTags(IEnumerable<string>? tags)
        {
            _tags.Clear();
            if (tags == null)
                return;
            foreach (var tag in tags)
            {
                if (!string.IsNullOrWhiteSpace(tag) &&
                    !_tags.Any(t => string.Equals(t, tag.Trim(), StringComparison.OrdinalIgnoreCase)))
                    _tags.Add(tag.Trim());
            }
        }
        
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

        /// <summary>Applies rarity tier label and scales gold/XP rewards.</summary>
        public void ApplyRarityScaling(string rarityName, double rewardMultiplier)
        {
            Rarity = string.IsNullOrWhiteSpace(rarityName) ? "Common" : rarityName.Trim();
            if (rewardMultiplier <= 0)
                rewardMultiplier = 1.0;
            GoldReward = Math.Max(0, (int)Math.Round(GoldReward * rewardMultiplier));
            XPReward = Math.Max(1, (int)Math.Round(XPReward * rewardMultiplier));
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
            // INT no longer adds to roll totals. TECH shifts HIT/COMBO/CRIT thresholds via
            // TechniqueMilestoneThresholdBonuses (applied per attack after threshold reset).
            return 0;
        }

        /// <summary>
        /// Gets combo amplification for enemies (same as heroes: based on Technique)
        /// </summary>
        public new double GetComboAmplifier()
        {
            var tuning = GameConfiguration.Instance;
            return ComboAmplifierCurve.Compute(Technique, tuning.ComboSystem);
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
        /// Same path as <see cref="Character.TakeDamage"/> so pack bodies cap overkill in one place.
        /// </summary>
        public new void TakeDamage(int amount) => base.TakeDamage(amount);

        public new List<string> TakeDamageWithNotifications(int amount) => base.TakeDamageWithNotifications(amount);

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
