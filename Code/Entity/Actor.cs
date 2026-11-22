using System;
using System.Collections.Generic;
using System.Linq;
using RPGGame.Data;

namespace RPGGame
{
    public abstract class Actor
    {
        public List<(Action action, double probability)> ActionPool { get; private set; }
        public string Name { get; set; }
        
        /// <summary>
        /// Opening-roll edge for the current fight (surprise / scouting): the next N attack rolls use 2d20 keep highest.
        /// Consumed in <c>ActionExecutionFlow</c> when the roll is made.
        /// </summary>
        public int EncounterAdvantageRolls { get; set; }

        /// <summary>Consumes one opening advantage roll when available.</summary>
        public bool TryConsumeEncounterAdvantage()
        {
            if (EncounterAdvantageRolls <= 0)
                return false;
            EncounterAdvantageRolls--;
            return true;
        }
        
        // Roll penalty system (for effects like Dust Cloud)
        public int RollPenalty { get; set; } = 0; // Penalty to dice rolls
        public int RollPenaltyTurns { get; set; } = 0; // Attack/spell rolls remaining while penalty applies
        
        /// <summary>Burn intensity: global tick deals this much damage, then decays by 1 and merges <see cref="PendingBurnFromHits"/>.</summary>
        public int BurnIntensity { get; set; }
        public int PendingBurnFromHits { get; set; }
        public double LastBurnTickTime { get; set; }

        /// <summary>Bleed intensity: same decay math as burn, resolved when the afflicted actor takes an action.</summary>
        public int BleedIntensity { get; set; }
        public int PendingBleedFromHits { get; set; }

        /// <summary>
        /// POTENCY from the hero who last hit this foe (base item modifiers, "+20% bleed damage"): status id → percent.
        /// Its bleed / burn / poison / acid ticks are scaled by it (<see cref="DotPotencyMultiplier"/>).
        /// </summary>
        public Dictionary<string, double>? DotPotency { get; set; }

        /// <summary>1 + POTENCY% for <paramref name="status"/> (1 when none).</summary>
        public double DotPotencyMultiplier(string status) =>
            DotPotency != null && DotPotency.TryGetValue(status, out double pct) ? Math.Max(0, 1.0 + pct / 100.0) : 1.0;
        /// <summary>True when bleed intensity or pending bleed from hits is non-zero.</summary>
        public bool IsBleeding => BleedIntensity > 0 || PendingBleedFromHits > 0;
        
        // Damage reduction system
        public double DamageReduction { get; set; } = 0.0;
        
        // Critical miss system - doubles action speed for next turn
        public bool HasCriticalMissPenalty { get; set; } = false;
        public int CriticalMissPenaltyTurns { get; set; } = 0;
        
        // Advanced status effects (Phase 2)
        public int? HardenStacks { get; set; } = null;
        public int HardenTurns { get; set; } = 0;
        public int? FortifyStacks { get; set; } = null;
        public int FortifyTurns { get; set; } = 0;
        public int? FortifyArmorBonus { get; set; } = null;
        public int? FocusStacks { get; set; } = null;
        public int FocusTurns { get; set; } = 0;
        public int? ExposeStacks { get; set; } = null;
        public int ExposeTurns { get; set; } = 0;
        /// <summary>Expose tier 2: fraction of armor stripped while Exposed (see <see cref="ExposeStatus"/>).</summary>
        public double ExposeArmorStrip { get; set; }
        /// <summary>Temporary HP: a hero resource (cash-in payout), not a status.</summary>
        public int? TemporaryHP { get; set; } = null;
        public int TemporaryHPTurns { get; set; } = 0;
        public bool IsMarked { get; set; } = false;
        public int MarkTurns { get; set; } = 0;
        /// <summary>Stagger (TempoStatuses): this actor's next action takes longer, then it is spent.</summary>
        public int StaggerStacks { get; set; }
        /// <summary>Haste (TempoStatuses): this actor's actions take less time; one stack falls off per own action.</summary>
        public int HasteStacks { get; set; }
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

        /// <summary>Focus lasts for this actor's next resolved actions; each action uses one turn.</summary>
        public void ConsumeOwnActionStatuses()
        {
            FocusTurns = TickStackStatus(FocusTurns, 1, () => FocusStacks = null);
            TempoStatuses.AfterOwnAction(this);
        }

        private static int TickStackStatus(int turns, int ticks, System.Action clear)
        {
            if (turns <= 0)
                return 0;
            int left = Math.Max(0, turns - ticks);
            if (left == 0)
                clear();
            return left;
        }

        public Action? SelectAction()
        {
            if (ActionPool.Count == 0)
                return null;

            // Reserve-pool actions are available for combo strip / explicit picks, not default weighted rolls.
            var rollable = ActionPool
                .Where(item => !ActionTagSyncHelper.IsReservePool(item.action.Tags))
                .ToList();
            if (rollable.Count == 0)
                return null;

            double totalProbability = rollable.Sum(item => item.probability);
            if (totalProbability <= 0)
                return rollable.Last().action;

            double randomValue = RandomUtility.Current.NextDouble() * totalProbability;
            double cumulativeProbability = 0;

            foreach ((Action action, double probability) in rollable)
            {
                cumulativeProbability += probability;
                if (randomValue <= cumulativeProbability)
                    return action;
            }

            // Fallback to the last rollable action if no action was selected
            return rollable.Last().action;
        }

        /// <summary>
        /// Updates temporary effects that decay over time
        /// </summary>
        /// <param name="actionLength">Length of the action in turns</param>
        public virtual void UpdateTempEffects(double actionLength = 1.0)
        {
            // Calculate how many turns this action represents
            double turnsPassed = actionLength / 1.0; // Using 1.0 as default action length
            
            // Roll penalty uses attack-based consumption (ConsumeRollPenaltyAfterCombatRoll), not time decay here.

            // Focus changes this actor's own swings: consumed after its action
            // (ConsumeOwnActionStatuses), not here, or they expire before the swing they should affect.

            // Incoming-damage statuses cover the opponent's swings until this actor acts again.
            int ticks = (int)Math.Ceiling(turnsPassed);
            HardenTurns = TickStackStatus(HardenTurns, ticks, () => HardenStacks = null);
            ExposeTurns = TickStackStatus(ExposeTurns, ticks, () => { ExposeStacks = null; ExposeArmorStrip = 0; });
            
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
        
        /// <summary>Max HP that DoT text and overkill checks read (character / enemy override).</summary>
        public virtual int GetMaxHealthForDot() => 1;

        static double GetStatusDotTickIntervalSeconds()
        {
            double interval = GameConfiguration.Instance.Poison.TickInterval;
            return interval > 0 ? interval : 5.0;
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
            return ScaleDotTickDamage(damage);
        }

        /// <summary>
        /// Burn / bleed tick damage × (1 + (level − 1) × <see cref="CombatConfig.DotDamagePerLevel"/>), level = this actor's
        /// (the target's), so a level 1 target takes exactly its intensity. Stacks are flat integers, so without it a
        /// 1-stack bleed did 1 damage at L60. 0 = legacy.
        /// </summary>
        protected int ScaleDotTickDamage(int damage)
        {
            double k = GameConfiguration.Instance.Combat?.DotDamagePerLevel ?? 0;
            if (k <= 0 || damage <= 0)
                return damage;
            int level = this is Character c ? Math.Max(1, c.Level) : 1;
            return (int)Math.Round(damage * (1.0 + (level - 1) * k));
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
            return ScaleDotTickDamage(damage);
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

        /// <summary>Queues <paramref name="damage"/> × <paramref name="stacks"/> bleed (Poison loads as Bleed).</summary>
        public void ApplyBleed(int damage, int stacks = 1) => QueueBleedFromHit(Math.Max(1, damage) * Math.Max(1, stacks));

        /// <summary>Legacy entry: maps to <see cref="QueueBurnFromHit"/>.</summary>
        public virtual void ApplyBurn(int damage, int stacks = 1)
        {
            int d = Math.Max(1, damage);
            int s = Math.Max(1, stacks);
            QueueBurnFromHit(d * s);
        }

        /// <summary>
        /// Clears all temporary effects from the actor
        /// </summary>
        public virtual void ClearAllTempEffects()
        {
            BurnIntensity = 0;
            PendingBurnFromHits = 0;
            LastBurnTickTime = 0;
            BleedIntensity = 0;
            PendingBleedFromHits = 0;
            
            // Clear roll penalty effects
            RollPenalty = 0;
            RollPenaltyTurns = 0;
            
            // Clear damage reduction
            DamageReduction = 0.0;

            HasCriticalMissPenalty = false;
            CriticalMissPenaltyTurns = 0;

            StaggerStacks = 0;
            HasteStacks = 0;

            HardenStacks = null;
            HardenTurns = 0;
            FortifyStacks = null;
            FortifyTurns = 0;
            FortifyArmorBonus = null;
            FocusStacks = null;
            FocusTurns = 0;
            ExposeStacks = null;
            ExposeTurns = 0;
            ExposeArmorStrip = 0;
            TemporaryHP = null;
            TemporaryHPTurns = 0;
            IsMarked = false;
            MarkTurns = 0;
            StatusSources.Clear();
        }

        /// <summary>Last action or item that applied a status, keyed by effect id (harden, bleed, …).</summary>
        [System.Text.Json.Serialization.JsonIgnore]
        public Dictionary<string, string> StatusSources { get; } = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        public void NoteStatusSource(string? effectId, string? source)
        {
            if (string.IsNullOrWhiteSpace(effectId) || string.IsNullOrWhiteSpace(source))
                return;
            StatusSources[effectId.Trim()] = source.Trim();
        }

        public string GetStatusSource(string? effectId)
        {
            if (string.IsNullOrWhiteSpace(effectId))
                return "";
            return StatusSources.TryGetValue(effectId.Trim(), out string? source) ? source : "";
        }

        public abstract string GetDescription();

        public override string ToString()
        {
            return $"{Name} - {GetDescription()}";
        }
    }
}

