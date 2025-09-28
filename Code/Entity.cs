namespace RPGGame
{
    public abstract class Entity
    {
        public List<(Action action, double probability)> ActionPool { get; private set; }
        public string Name { get; set; }
        
        // Common effect properties (consolidated from CharacterEffects)
        // Weaken debuff system
        public bool IsWeakened { get; set; } = false; // Whether entity is weakened
        public int WeakenTurns { get; set; } = 0; // Number of turns weakened
        public double WeakenMultiplier { get; set; } = 0.5; // Damage reduction when weakened (50% outgoing damage)
        
        // Stun debuff system
        public bool IsStunned { get; set; } = false; // Whether entity is stunned
        public int StunTurnsRemaining { get; set; } = 0; // Number of turns stunned
        
        // Roll penalty system (for effects like Dust Cloud)
        public int RollPenalty { get; set; } = 0; // Penalty to dice rolls
        public int RollPenaltyTurns { get; set; } = 0; // Number of turns the penalty lasts
        
        // Poison/Burn system (common to all entities)
        public int PoisonDamage { get; set; } = 0;
        public int PoisonStacks { get; set; } = 0;
        public double LastPoisonTick { get; set; } = 0.0;
        public bool IsBleeding { get; set; } = false;
        public int BurnDamage { get; set; } = 0;
        public int BurnStacks { get; set; } = 0;
        public double LastBurnTick { get; set; } = 0.0;
        
        // Damage reduction system
        public double DamageReduction { get; set; } = 0.0;

        protected Entity(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
                throw new ArgumentException("Entity name cannot be null or empty", nameof(name));

            Name = name;
            ActionPool = new List<(Action, double)>();
        }

        public void AddAction(Action action, double probability)
        {
            if (probability < 0 || probability > 1)
                throw new ArgumentException("Probability must be between 0 and 1", nameof(probability));

            // Remove any existing action with the same name
            ActionPool.RemoveAll(a => a.action.Name == action.Name);
            ActionPool.Add((action, probability));
        }

        public virtual void RemoveAction(Action action)
        {
            ActionPool.RemoveAll(a => a.action.Name == action.Name && a.action.ComboOrder == action.ComboOrder);
        }
        
        /// <summary>
        /// Applies a roll penalty to the entity
        /// </summary>
        /// <param name="penalty">Amount to reduce rolls by</param>
        /// <param name="turns">Number of turns the penalty lasts</param>
        public void ApplyRollPenalty(int penalty, int turns)
        {
            RollPenalty = penalty;
            RollPenaltyTurns = turns;
        }

        public Action? SelectAction()
        {
            if (ActionPool.Count == 0)
                return null;

            // Check if entity is stunned (now works for all entities since stun properties are in base class)
            if (IsStunned)
                return null;

            double totalProbability = ActionPool.Sum(a => a.probability);
            double randomValue = new Random().NextDouble() * totalProbability;
            double cumulativeProbability = 0;

            foreach (var (action, probability) in ActionPool)
            {
                cumulativeProbability += probability;
                if (randomValue <= cumulativeProbability)
                    return action;
            }

            // Fallback to the last action if no action was selected
            return ActionPool.Last().action;
        }

        /// <summary>
        /// Updates temporary effects that decay over time
        /// </summary>
        /// <param name="actionLength">Length of the action in turns</param>
        public virtual void UpdateTempEffects(double actionLength = 1.0)
        {
            // Calculate how many turns this action represents
            double turnsPassed = actionLength / 1.0; // Using 1.0 as default action length
            
            // Update stun effects
            if (StunTurnsRemaining > 0)
            {
                StunTurnsRemaining = Math.Max(0, StunTurnsRemaining - (int)Math.Ceiling(turnsPassed));
                if (StunTurnsRemaining == 0)
                    IsStunned = false;
            }
            
            // Update roll penalty effects
            if (RollPenaltyTurns > 0)
            {
                RollPenaltyTurns = Math.Max(0, RollPenaltyTurns - (int)Math.Ceiling(turnsPassed));
                if (RollPenaltyTurns == 0)
                    RollPenalty = 0;
            }
            
            // Update weaken debuff
            if (WeakenTurns > 0)
            {
                WeakenTurns = Math.Max(0, WeakenTurns - (int)Math.Ceiling(turnsPassed));
                if (WeakenTurns == 0)
                {
                    IsWeakened = false;
                }
            }
            
            // Update damage reduction decay (per turn, not per action)
            if (DamageReduction > 0)
            {
                DamageReduction = Math.Max(0.0, DamageReduction - (0.1 * Math.Ceiling(turnsPassed)));
            }
        }
        
        /// <summary>
        /// Processes poison damage over time
        /// </summary>
        /// <param name="currentTime">Current game time in seconds</param>
        /// <returns>Damage taken from poison this tick</returns>
        public virtual int ProcessPoison(double currentTime)
        {
            if (PoisonStacks > 0)
            {
                var poisonConfig = TuningConfig.Instance.Poison;
                
                if (currentTime - LastPoisonTick >= poisonConfig.TickInterval)
                {
                    int totalDamage = PoisonDamage * PoisonStacks;
                    LastPoisonTick = currentTime;
                    
                    PoisonStacks--;
                    if (PoisonStacks <= 0)
                    {
                        PoisonDamage = 0;
                        PoisonStacks = 0;
                        IsBleeding = false;
                    }
                    return totalDamage;
                }
            }
            return 0;
        }
        
        /// <summary>
        /// Processes burn damage over time
        /// </summary>
        /// <param name="currentTime">Current game time in seconds</param>
        /// <returns>Damage taken from burn this tick</returns>
        public virtual int ProcessBurn(double currentTime)
        {
            if (BurnStacks > 0)
            {
                var poisonConfig = TuningConfig.Instance.Poison;
                
                if (currentTime - LastBurnTick >= poisonConfig.TickInterval)
                {
                    int totalDamage = BurnDamage * BurnStacks;
                    LastBurnTick = currentTime;
                    
                    BurnStacks--;
                    if (BurnStacks <= 0)
                    {
                        BurnDamage = 0;
                        BurnStacks = 0;
                    }
                    return totalDamage;
                }
            }
            return 0;
        }
        
        /// <summary>
        /// Gets the damage type text for poison effects
        /// </summary>
        /// <returns>Either "bleed" or "poison"</returns>
        public virtual string GetDamageTypeText()
        {
            return IsBleeding ? "bleed" : "poison";
        }
        
        /// <summary>
        /// Applies poison damage to the entity
        /// </summary>
        /// <param name="damage">Base damage per stack</param>
        /// <param name="stacks">Number of stacks to apply</param>
        /// <param name="isBleeding">Whether this is bleeding damage</param>
        public virtual void ApplyPoison(int damage, int stacks = 1, bool isBleeding = false)
        {
            if (PoisonStacks == 0 || PoisonDamage != damage)
            {
                PoisonDamage = damage;
            }
            PoisonStacks += stacks;
            LastPoisonTick = GameTicker.Instance.GetCurrentGameTime();
            IsBleeding = isBleeding;
        }
        
        /// <summary>
        /// Applies burn damage to the entity
        /// </summary>
        /// <param name="damage">Base damage per stack</param>
        /// <param name="stacks">Number of stacks to apply</param>
        public virtual void ApplyBurn(int damage, int stacks = 1)
        {
            if (BurnStacks == 0 || BurnDamage != damage)
            {
                BurnDamage = damage;
            }
            BurnStacks += stacks;
            LastBurnTick = GameTicker.Instance.GetCurrentGameTime();
        }
        
        /// <summary>
        /// Applies weaken debuff to the entity
        /// </summary>
        /// <param name="turns">Number of turns to be weakened</param>
        public virtual void ApplyWeaken(int turns)
        {
            IsWeakened = true;
            WeakenTurns = turns;
        }
        
        /// <summary>
        /// Clears all temporary effects from the entity
        /// </summary>
        public virtual void ClearAllTempEffects()
        {
            // Clear poison/bleed effects
            PoisonDamage = 0;
            PoisonStacks = 0;
            IsBleeding = false;
            LastPoisonTick = 0.0;
            
            // Clear burn effects
            BurnDamage = 0;
            BurnStacks = 0;
            LastBurnTick = 0.0;
            
            // Clear stun effects
            IsStunned = false;
            StunTurnsRemaining = 0;
            
            // Clear weaken effects
            IsWeakened = false;
            WeakenTurns = 0;
            
            // Clear roll penalty effects
            RollPenalty = 0;
            RollPenaltyTurns = 0;
            
            // Clear damage reduction
            DamageReduction = 0.0;
        }

        public abstract string GetDescription();

        public override string ToString()
        {
            return $"{Name} - {GetDescription()}";
        }
    }
} 