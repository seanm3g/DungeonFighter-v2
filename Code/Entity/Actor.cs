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
        public int RollPenaltyTurns { get; set; } = 0; // Attack/spell rolls remaining while penalty applies
        
        /// <summary>Cumulative max-HP poison (%); only increases when poison is applied; global DoT tick reads it without reducing.</summary>
        public double PoisonPercentOfMaxHealth { get; set; }
        public double LastPoisonTickTime { get; set; }

        /// <summary>Burn intensity: global tick deals this much damage, then decays by 1 and merges <see cref="PendingBurnFromHits"/>.</summary>
        public int BurnIntensity { get; set; }
        public int PendingBurnFromHits { get; set; }
        public double LastBurnTickTime { get; set; }

        /// <summary>Bleed intensity: same decay math as burn, resolved when the afflicted actor takes an action.</summary>
        public int BleedIntensity { get; set; }
        public int PendingBleedFromHits { get; set; }
        /// <summary>True when bleed intensity or pending bleed from hits is non-zero.</summary>
        public bool IsBleeding => BleedIntensity > 0 || PendingBleedFromHits > 0;
        
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
        protected Actor(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
                throw new ArgumentException("Actor name cannot be null or empty", nameof(name));

            Name = name;
            ActionPool = new List<(Action, double)>();
        }

        public virtual void AddAction(Action action, double probability)
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
        public virtual void AddActionAllowDuplicates(Action action, double probability)
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
        /// Applies a roll penalty to the actor (subtracted from roll bonus on attack/spell rolls).
        /// </summary>
        /// <param name="penalty">Amount to reduce rolls by</param>
        /// <param name="attacksRemaining">How many resolved Attack/Spell rolls keep this penalty; decremented in <see cref="ConsumeRollPenaltyAfterCombatRoll"/>.</param>
        public void ApplyRollPenalty(int penalty, int attacksRemaining)
        {
            RollPenalty = penalty;
            RollPenaltyTurns = Math.Max(0, attacksRemaining);
        }

        /// <summary>
        /// Call after a combat action that used the d20 for hit/miss (one consumption per action; multihit damage still applies <see cref="RollPenalty"/> per damage tick in <see cref="RPGGame.Actions.Execution.MultiHitProcessor"/>).
        /// </summary>
        public void ConsumeRollPenaltyAfterCombatRoll(Action? resolvedAction)
        {
            if (resolvedAction == null) return;
            if (resolvedAction.Type != ActionType.Attack && resolvedAction.Type != ActionType.Spell) return;
            if (RollPenaltyTurns <= 0 || RollPenalty == 0) return;
            RollPenaltyTurns--;
            if (RollPenaltyTurns <= 0)
            {
                RollPenaltyTurns = 0;
                RollPenalty = 0;
            }
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
            
            // Roll penalty uses attack-based consumption (ConsumeRollPenaltyAfterCombatRoll), not time decay here.

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
        
        /// <summary>Max HP used for poison % damage ticks (character/enemy override).</summary>
        public virtual int GetMaxHealthForPoisonDot() => 1;

        static double GetStatusDotTickIntervalSeconds()
        {
            double interval = GameConfiguration.Instance.Poison.TickInterval;
            return interval > 0 ? interval : 5.0;
        }

        /// <summary>
        /// Poison: on global tick, deal damage from cumulative % of max HP; stored % is unchanged.
        /// </summary>
        public virtual int ProcessPoison(double currentTime)
        {
            if (PoisonPercentOfMaxHealth <= 0)
                return 0;
            double interval = GetStatusDotTickIntervalSeconds();
            // GameTicker.Reset() zeros combat time; tick stamps can still hold pre-reset wall-clock values.
            // Then currentTime - LastPoisonTickTime is negative and no DoT ever runs until real time catches up.
            if (LastPoisonTickTime > currentTime)
                LastPoisonTickTime = 0;
            if (currentTime - LastPoisonTickTime < interval)
                return 0;
            LastPoisonTickTime = currentTime;
            int maxHp = Math.Max(1, GetMaxHealthForPoisonDot());
            return (int)Math.Floor(maxHp * PoisonPercentOfMaxHealth / 100.0);
        }

        /// <summary>
        /// Burn: on global tick, deal current intensity damage, then intensity = max(0, intensity - 1) + pending.
        /// </summary>
        public virtual int ProcessBurn(double currentTime)
        {
            if (BurnIntensity <= 0 && PendingBurnFromHits <= 0)
                return 0;
            double interval = GetStatusDotTickIntervalSeconds();
            if (LastBurnTickTime > currentTime)
                LastBurnTickTime = 0;
            if (currentTime - LastBurnTickTime < interval)
                return 0;
            LastBurnTickTime = currentTime;
            int pending = PendingBurnFromHits;
            PendingBurnFromHits = 0;
            int damage = BurnIntensity > 0 ? BurnIntensity : pending;
            if (BurnIntensity > 0)
                BurnIntensity = Math.Max(0, BurnIntensity - 1) + pending;
            else
                BurnIntensity = Math.Max(0, damage - 1);
            return damage;
        }

        /// <summary>
        /// Bleed: same intensity math as burn, invoked when the afflicted actor finishes taking an action (not global tick).
        /// </summary>
        public virtual int ProcessBleedOnAction()
        {
            if (BleedIntensity <= 0 && PendingBleedFromHits <= 0)
                return 0;
            int pending = PendingBleedFromHits;
            PendingBleedFromHits = 0;
            int damage = BleedIntensity > 0 ? BleedIntensity : pending;
            if (BleedIntensity > 0)
                BleedIntensity = Math.Max(0, BleedIntensity - 1) + pending;
            else
                BleedIntensity = Math.Max(0, damage - 1);
            return damage;
        }

        /// <summary>Label for poison DoT messaging (bleed uses a separate pipeline).</summary>
        public virtual string GetDamageTypeText() => "poison";

        /// <summary>Adds poison % of max HP (monotone; only increases).</summary>
        public virtual void ApplyPoisonPercent(double deltaPercent)
        {
            if (deltaPercent <= 0) return;
            PoisonPercentOfMaxHealth += deltaPercent;
        }

        public virtual void QueueBurnFromHit(int amount)
        {
            if (amount <= 0) return;
            PendingBurnFromHits += amount;
        }

        public virtual void QueueBleedFromHit(int amount)
        {
            if (amount <= 0) return;
            PendingBleedFromHits += amount;
        }

        /// <summary>Legacy entry: bleeding maps to <see cref="QueueBleedFromHit"/>; poison maps to <see cref="ApplyPoisonPercent"/>.</summary>
        public virtual void ApplyPoison(int damage, int stacks = 1, bool isBleeding = false)
        {
            int d = Math.Max(1, damage);
            int s = Math.Max(1, stacks);
            if (isBleeding)
                QueueBleedFromHit(d * s);
            else
                ApplyPoisonPercent(s);
        }

        /// <summary>Legacy entry: maps to <see cref="QueueBurnFromHit"/>.</summary>
        public virtual void ApplyBurn(int damage, int stacks = 1)
        {
            int d = Math.Max(1, damage);
            int s = Math.Max(1, stacks);
            QueueBurnFromHit(d * s);
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
            PoisonPercentOfMaxHealth = 0;
            LastPoisonTickTime = 0;
            BurnIntensity = 0;
            PendingBurnFromHits = 0;
            LastBurnTickTime = 0;
            BleedIntensity = 0;
            PendingBleedFromHits = 0;
            
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

            HasCriticalMissPenalty = false;
            CriticalMissPenaltyTurns = 0;

            VulnerabilityStacks = null;
            VulnerabilityTurns = 0;
            HardenStacks = null;
            HardenTurns = 0;
            FortifyStacks = null;
            FortifyTurns = 0;
            FortifyArmorBonus = null;
            FocusStacks = null;
            FocusTurns = 0;
            ExposeStacks = null;
            ExposeTurns = 0;
            ExposeArmorReduction = null;
            HPRegenStacks = null;
            HPRegenTurns = 0;
            HPRegenAmount = null;
            ArmorBreakStacks = null;
            ArmorBreakTurns = 0;
            ArmorBreakReduction = null;
            HasPierce = false;
            PierceTurns = 0;
            ReflectStacks = null;
            ReflectTurns = 0;
            ReflectPercentage = null;
            IsSilenced = false;
            SilenceTurns = 0;
            StatDrainStacks = null;
            StatDrainTurns = 0;
            StatDrainAmount = null;
            HasAbsorb = false;
            AbsorbTurns = 0;
            AbsorbThreshold = 0;
            AbsorbedDamage = 0;
            TemporaryHP = null;
            TemporaryHPTurns = 0;
            IsConfused = false;
            ConfusionTurns = 0;
            ConfusionChance = 0.0;
            IsMarked = false;
            MarkTurns = 0;
        }

        public abstract string GetDescription();

        public override string ToString()
        {
            return $"{Name} - {GetDescription()}";
        }
    }
}

