using System;
using System.Collections.Generic;
using System.Linq;

namespace RPGGame
{
    public abstract class Actor
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
        
        // Critical miss system - doubles action speed for next turn
        public bool HasCriticalMissPenalty { get; set; } = false;
        public int CriticalMissPenaltyTurns { get; set; } = 0;
        
        // Advanced status effects (Phase 2)
        public int? VulnerabilityStacks { get; set; } = null;
        public int VulnerabilityTurns { get; set; } = 0;
        public int? HardenStacks { get; set; } = null;
        public int HardenTurns { get; set; } = 0;
        public int? FortifyStacks { get; set; } = null;
        public int FortifyTurns { get; set; } = 0;
        public int? FortifyArmorBonus { get; set; } = null;
        public int? FocusStacks { get; set; } = null;
        public int FocusTurns { get; set; } = 0;
        public int? ExposeStacks { get; set; } = null;
        public int ExposeTurns { get; set; } = 0;
        public int? ExposeArmorReduction { get; set; } = null;
        public int? HPRegenStacks { get; set; } = null;
        public int HPRegenTurns { get; set; } = 0;
        public int? HPRegenAmount { get; set; } = null;
        public int? ArmorBreakStacks { get; set; } = null;
        public int ArmorBreakTurns { get; set; } = 0;
        public int? ArmorBreakReduction { get; set; } = null;
        public bool HasPierce { get; set; } = false;
        public int PierceTurns { get; set; } = 0;
        public int? ReflectStacks { get; set; } = null;
        public int ReflectTurns { get; set; } = 0;
        public int? ReflectPercentage { get; set; } = null;
        public bool IsSilenced { get; set; } = false;
        public int SilenceTurns { get; set; } = 0;
        public int? StatDrainStacks { get; set; } = null;
        public int StatDrainTurns { get; set; } = 0;
        public int? StatDrainAmount { get; set; } = null;
        public bool HasAbsorb { get; set; } = false;
        public int AbsorbTurns { get; set; } = 0;
        public int AbsorbThreshold { get; set; } = 0;
        public int AbsorbedDamage { get; set; } = 0;
        public int? TemporaryHP { get; set; } = null;
        public int TemporaryHPTurns { get; set; } = 0;
        public bool IsConfused { get; set; } = false;
        public int ConfusionTurns { get; set; } = 0;
        public double ConfusionChance { get; set; } = 0.0;
        public bool IsMarked { get; set; } = false;
        public int MarkTurns { get; set; } = 0;
        public int BleedStacks { get; set; } = 0; // For bleed tracking

        protected Actor(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
                throw new ArgumentException("Actor name cannot be null or empty", nameof(name));

            Name = name;
            ActionPool = new List<(Action, double)>();
        }

        public void AddAction(Action action, double probability)
        {
            if (probability < 0 || probability > 1)
                throw new ArgumentException("Probability must be between 0 and 1", nameof(probability));

            // Remove any existing action with the same name
            ActionPool.RemoveAll(item => item.action.Name == action.Name);
            ActionPool.Add((action, probability));
        }

        /// <summary>
        /// Adds an action to the action pool, allowing duplicates.
        /// Used when an item has the same action multiple times (e.g., ARCANE ECHO appearing twice).
        /// Each duplicate will have a unique ComboOrder to distinguish it.
        /// </summary>
        public void AddActionAllowDuplicates(Action action, double probability)
        {
            if (probability < 0 || probability > 1)
                throw new ArgumentException("Probability must be between 0 and 1", nameof(probability));

            // Ensure this action has a unique ComboOrder
            // Find the highest ComboOrder for actions with the same name
            int maxComboOrder = ActionPool
                .Where(item => item.action.Name == action.Name)
                .Select(item => item.action.ComboOrder)
                .DefaultIfEmpty(0)
                .Max();

            // Set this action's ComboOrder to be unique
            action.ComboOrder = maxComboOrder + 1;

            // Add without removing existing actions
            ActionPool.Add((action, probability));
        }

        public virtual void RemoveAction(Action action)
        {
            ActionPool.RemoveAll(item => item.action.Name == action.Name && item.action.ComboOrder == action.ComboOrder);
        }

        /// <summary>
        /// Removes all instances of an action by name from the action pool
        /// Used when removing gear that may have added duplicate actions
        /// </summary>
        public virtual void RemoveAllActionsByName(string actionName)
        {
            ActionPool.RemoveAll(item => item.action.Name == actionName);
        }
        
        /// <summary>
        /// Applies a roll penalty to the actor
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

            // Check if actor is stunned (now works for all actors since stun properties are in base class)
            if (IsStunned)
                return null;

            double totalProbability = ActionPool.Sum(item => item.probability);
            double randomValue = new Random().NextDouble() * totalProbability;
            double cumulativeProbability = 0;

            foreach ((Action action, double probability) in ActionPool)
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
            
            // Update critical miss penalty
            if (CriticalMissPenaltyTurns > 0)
            {
                CriticalMissPenaltyTurns = Math.Max(0, CriticalMissPenaltyTurns - (int)Math.Ceiling(turnsPassed));
                if (CriticalMissPenaltyTurns == 0)
                    HasCriticalMissPenalty = false;
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
                var poisonConfig = GameConfiguration.Instance.Poison;
                
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
                var poisonConfig = GameConfiguration.Instance.Poison;
                
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
        /// Applies poison damage to the actor
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
            if (isBleeding)
            {
                BleedStacks = PoisonStacks; // Sync BleedStacks with PoisonStacks for bleed effects
            }
        }
        
        /// <summary>
        /// Applies burn damage to the actor
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
        /// Applies weaken debuff to the actor
        /// </summary>
        /// <param name="turns">Number of turns to be weakened</param>
        public virtual void ApplyWeaken(int turns)
        {
            IsWeakened = true;
            WeakenTurns = turns;
        }
        
        /// <summary>
        /// Clears all temporary effects from the actor
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

