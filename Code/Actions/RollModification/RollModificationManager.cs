using System;
using System.Collections.Generic;
using System.Linq;
using RPGGame;
using RPGGame.Actions.RollModification;
using RPGGame.Combat;
using RPGGame.Data;

namespace RPGGame.Actions.RollModification
{
    /// <summary>
    /// Manages roll modifications for actions
    /// Integrates roll modifiers with the action system
    /// </summary>
    public static class RollModificationManager
    {
        private static readonly RollModifierRegistry _registry = new RollModifierRegistry();
        private static readonly ThresholdManager _thresholdManager = new ThresholdManager();

        /// <summary>Deferred absolute threshold override markers (applied before HIT/COMBO/CRIT deltas).</summary>
        public const string SetCriticalMissThresholdType = "SET_CRIT_MISS_THRESH";
        public const string SetCriticalHitThresholdType = "SET_CRIT_THRESH";
        public const string SetComboThresholdType = "SET_COMBO_THRESH";
        public const string SetHitThresholdType = "SET_HIT_THRESH";

        /// <summary>
        /// Applies roll modifications from an action to a base roll
        /// </summary>
        public static int ApplyActionRollModifications(int baseRoll, Action action, Actor source, Actor? target)
        {
            int modifiedRoll = baseRoll;
            var context = new RollModificationContext(source, target, action);

            // Apply multiple dice mode if specified
            if (action.RollMods.MultipleDiceCount > 1)
            {
                var mode = ParseDiceMode(action.RollMods.MultipleDiceMode);
                modifiedRoll = MultiDiceRoller.RollMultipleDice(action.RollMods.MultipleDiceCount, 20, mode);
            }

            // Apply exploding dice if enabled
            if (action.RollMods.ExplodingDice && modifiedRoll >= action.RollMods.ExplodingDiceThreshold)
            {
                var explodingModifier = new ExplodingDiceModifier("ActionExploding", action.RollMods.ExplodingDiceThreshold);
                modifiedRoll = explodingModifier.ModifyRoll(modifiedRoll, context);
            }

            // Apply reroll if enabled
            if (action.RollMods.AllowReroll && action.RollMods.RerollChance > 0)
            {
                var rerollModifier = new RerollModifier("ActionReroll", action.RollMods.RerollChance);
                modifiedRoll = rerollModifier.ModifyRoll(modifiedRoll, context);
            }

            // Apply multiplier
            if (action.RollMods.Multiplier != 1.0)
            {
                var multiplier = new MultiplicativeRollModifier("ActionMultiplier", action.RollMods.Multiplier);
                modifiedRoll = multiplier.ModifyRoll(modifiedRoll, context);
            }

            // Apply additive modifier
            if (action.RollMods.Additive != 0)
            {
                var additive = new AdditiveRollModifier("ActionAdditive", action.RollMods.Additive);
                modifiedRoll = additive.ModifyRoll(modifiedRoll, context);
            }

            // Apply clamp
            if (action.RollMods.Min != 1 || action.RollMods.Max != 20)
            {
                var clamp = new ClampRollModifier("ActionClamp", action.RollMods.Min, action.RollMods.Max);
                modifiedRoll = clamp.ModifyRoll(modifiedRoll, context);
            }

            return modifiedRoll;
        }

        /// <summary>
        /// Gets the threshold manager instance
        /// </summary>
        public static ThresholdManager GetThresholdManager()
        {
            return _thresholdManager;
        }

        /// <summary>
        /// When <see cref="Action.DefersSheetCombatPackagesToNextHeroRoll"/> is true, sheet threshold overrides and
        /// sheet accuracy are packaged for a later roll (blank cadence = next ACTION / 1 turn; ATTACK = immediate).
        /// </summary>
        public static bool ShouldDeferRollModThresholdPackages(Action? action) => Action.DefersSheetCombatPackagesToNextHeroRoll(action);

        /// <summary>
        /// Applies SET_* deferred override items (must run before CRIT_MISS / CRIT / COMBO / HIT deltas for this roll).
        /// </summary>
        public static void ApplyDeferredThresholdPackageSetPhase(Actor actor, List<ActionAttackBonusItem>? items)
        {
            if (items == null || items.Count == 0) return;
            foreach (var bonus in items)
            {
                switch ((bonus.Type ?? "").ToUpperInvariant())
                {
                    case SetCriticalMissThresholdType:
                        if ((int)bonus.Value > 0)
                            _thresholdManager.SetCriticalMissThreshold(actor, (int)bonus.Value);
                        break;
                    case SetCriticalHitThresholdType:
                        if ((int)bonus.Value > 0)
                            _thresholdManager.SetCriticalHitThreshold(actor, (int)bonus.Value);
                        break;
                    case SetComboThresholdType:
                        if ((int)bonus.Value > 0)
                            _thresholdManager.SetComboThreshold(actor, (int)bonus.Value);
                        break;
                    case SetHitThresholdType:
                        if ((int)bonus.Value > 0)
                            _thresholdManager.SetHitThreshold(actor, (int)bonus.Value);
                        break;
                }
            }
        }

        /// <summary>
        /// Applies threshold <em>overrides</em> (absolute values) from an action.
        /// Hero/enemy dice <em>adjustments</em> (sheet HIT/COMBO/CRIT/CRIT MISS) are deferred to the next attack roll
        /// via <see cref="EnqueueDeferredRollModThresholdAdjustmentsForNextRoll"/>.
        /// When <see cref="ShouldDeferRollModThresholdPackages"/> is true, overrides are also deferred (SET_* items).
        /// </summary>
        public static void ApplyThresholdOverrides(Action action, Actor source, Actor? target = null)
        {
            if (ShouldDeferRollModThresholdPackages(action))
                return;

            // Apply threshold overrides (absolute values)
            if (action.RollMods.CriticalMissThresholdOverride > 0)
            {
                _thresholdManager.SetCriticalMissThreshold(source, action.RollMods.CriticalMissThresholdOverride);
                if (target != null && action.RollMods.ApplyThresholdAdjustmentsToBoth)
                {
                    _thresholdManager.SetCriticalMissThreshold(target, action.RollMods.CriticalMissThresholdOverride);
                }
            }
            if (action.RollMods.CriticalHitThresholdOverride > 0)
            {
                _thresholdManager.SetCriticalHitThreshold(source, action.RollMods.CriticalHitThresholdOverride);
                if (target != null && action.RollMods.ApplyThresholdAdjustmentsToBoth)
                {
                    _thresholdManager.SetCriticalHitThreshold(target, action.RollMods.CriticalHitThresholdOverride);
                }
            }
            if (action.RollMods.ComboThresholdOverride > 0)
            {
                _thresholdManager.SetComboThreshold(source, action.RollMods.ComboThresholdOverride);
                if (target != null && action.RollMods.ApplyThresholdAdjustmentsToBoth)
                {
                    _thresholdManager.SetComboThreshold(target, action.RollMods.ComboThresholdOverride);
                }
            }
            if (action.RollMods.HitThresholdOverride > 0)
            {
                _thresholdManager.SetHitThreshold(source, action.RollMods.HitThresholdOverride);
                if (target != null && action.RollMods.ApplyThresholdAdjustmentsToBoth)
                {
                    _thresholdManager.SetHitThreshold(target, action.RollMods.HitThresholdOverride);
                }
            }
        }

        /// <summary>
        /// Builds pending bonus items from the action's threshold <em>adjustment</em> fields (hero or enemy columns).
        /// Does not include absolute overrides. <see cref="EnqueueDeferredRollModThresholdAdjustmentsForNextRoll"/> multiplies these values by <see cref="GetEffectiveMultiHitCountForModifierScaling"/> before queuing.
        /// </summary>
        public static bool TryBuildDeferredRollModThresholdBonuses(Action action, Actor source, out List<ActionAttackBonusItem> items)
        {
            items = new List<ActionAttackBonusItem>();
            bool useEnemy = source is Enemy;
            int cm = useEnemy ? action.RollMods.EnemyCriticalMissThresholdAdjustment : action.RollMods.CriticalMissThresholdAdjustment;
            int ch = useEnemy ? action.RollMods.EnemyCriticalHitThresholdAdjustment : action.RollMods.CriticalHitThresholdAdjustment;
            int combo = useEnemy ? action.RollMods.EnemyComboThresholdAdjustment : action.RollMods.ComboThresholdAdjustment;
            int hit = useEnemy ? action.RollMods.EnemyHitThresholdAdjustment : action.RollMods.HitThresholdAdjustment;
            if (cm != 0) items.Add(new ActionAttackBonusItem { Type = "CRIT_MISS", Value = cm });
            if (ch != 0) items.Add(new ActionAttackBonusItem { Type = "CRIT", Value = ch });
            if (combo != 0) items.Add(new ActionAttackBonusItem { Type = "COMBO", Value = combo });
            if (hit != 0) items.Add(new ActionAttackBonusItem { Type = "HIT", Value = hit });
            return items.Count > 0;
        }

        /// <summary>
        /// Builds SET_* items for absolute threshold overrides when they must not apply to the current roll.
        /// </summary>
        public static bool TryBuildDeferredRollModThresholdOverrideSetItems(Action action, out List<ActionAttackBonusItem> items)
        {
            items = new List<ActionAttackBonusItem>();
            if (action.RollMods.CriticalMissThresholdOverride > 0)
                items.Add(new ActionAttackBonusItem { Type = SetCriticalMissThresholdType, Value = action.RollMods.CriticalMissThresholdOverride });
            if (action.RollMods.CriticalHitThresholdOverride > 0)
                items.Add(new ActionAttackBonusItem { Type = SetCriticalHitThresholdType, Value = action.RollMods.CriticalHitThresholdOverride });
            if (action.RollMods.ComboThresholdOverride > 0)
                items.Add(new ActionAttackBonusItem { Type = SetComboThresholdType, Value = action.RollMods.ComboThresholdOverride });
            if (action.RollMods.HitThresholdOverride > 0)
                items.Add(new ActionAttackBonusItem { Type = SetHitThresholdType, Value = action.RollMods.HitThresholdOverride });
            return items.Count > 0;
        }

        /// <summary>
        /// Queues sheet threshold adjustments and deferred absolute overrides for the next application.
        /// Deferred sheet hero/enemy accuracy is queued only on a successful hit in <see cref="RPGGame.Actions.Execution.ActionExecutionFlow"/>
        /// (FIFO <c>ACCURACY</c>), not here, so it is not double-stacked with temp roll bonus or shown twice in the HUD.
        /// FIFO per hero roll by default; <see cref="Action.Cadence"/> <c>Ability</c> uses the ability bonus queue.
        /// Adjustment values are multiplied by the action's effective multihit count (same rules as multihit damage).
        /// </summary>
        public static void EnqueueDeferredRollModThresholdAdjustmentsForNextRoll(Action action, Actor source, Actor? target)
        {
            bool deferPackages = ShouldDeferRollModThresholdPackages(action);
            var combined = new List<ActionAttackBonusItem>();

            if (deferPackages && TryBuildDeferredRollModThresholdOverrideSetItems(action, out var setItems) && setItems.Count > 0)
                combined.AddRange(setItems.Select(b => new ActionAttackBonusItem { Type = b.Type, Value = b.Value }));

            if (TryBuildDeferredRollModThresholdBonuses(action, source, out var rawAdjustments) && rawAdjustments.Count > 0)
            {
                int hitLayers = GetEffectiveMultiHitCountForModifierScaling(action, source);
                if (hitLayers < 1) hitLayers = 1;
                combined.AddRange(rawAdjustments.Select(b => new ActionAttackBonusItem { Type = b.Type, Value = b.Value * hitLayers }));
            }

            if (combined.Count == 0)
                return;

            bool routeToAbilityQueue = string.Equals((action.Cadence ?? "").Trim(), "Ability", StringComparison.OrdinalIgnoreCase);

            void EnqueueOn(Character ch)
            {
                var copy = combined.Select(b => new ActionAttackBonusItem { Type = b.Type, Value = b.Value }).ToList();
                if (routeToAbilityQueue)
                {
                    ch.Effects.AbilityBonuses.Add(new ActionAttackBonusGroup
                    {
                        Keyword = "ABILITY",
                        CadenceType = "ABILITY",
                        Count = 1,
                        Bonuses = copy
                    });
                }
                else
                    ch.Effects.AddPendingActionBonusesNextHeroRoll(copy);
            }

            if (source is Character src)
                EnqueueOn(src);
            if (action.RollMods.ApplyThresholdAdjustmentsToBoth && target is Character tgt)
                EnqueueOn(tgt);
        }

        /// <summary>
        /// Multihit count used when scaling per-hit modifiers: deferred roll-mod threshold adjustments, deferred sheet accuracy on hit, etc.
        /// Aligns with multihit damage tick count in <see cref="RPGGame.Actions.Execution.MultiHitProcessor"/>.
        /// </summary>
        public static int GetEffectiveMultiHitCountForModifierScaling(Action action, Actor source)
        {
            int n = action.Advanced?.MultiHitCount ?? 1;
            if (n <= 0) n = 1;
            if (source is Character character && character.Effects.ConsumedMultiHitMod != 0)
                n = Math.Max(1, n + (int)Math.Max(0, character.Effects.ConsumedMultiHitMod));
            n = Math.Max(1, n + ChainPositionBonusApplier.GetMultiHitDelta(source, action, ActionUtilities.GetComboActions(source), ActionUtilities.GetComboStep(source)));
            return n;
        }

        private static MultiDiceRoller.DiceSelectionMode ParseDiceMode(string mode)
        {
            return mode.ToLower() switch
            {
                "takelowest" => MultiDiceRoller.DiceSelectionMode.TakeLowest,
                "takehighest" => MultiDiceRoller.DiceSelectionMode.TakeHighest,
                "takeaverage" => MultiDiceRoller.DiceSelectionMode.TakeAverage,
                "sum" => MultiDiceRoller.DiceSelectionMode.Sum,
                _ => MultiDiceRoller.DiceSelectionMode.Sum
            };
        }
    }
}

