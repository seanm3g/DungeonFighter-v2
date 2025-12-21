using System;
using RPGGame;
using RPGGame.Actions.RollModification;
using RPGGame.Combat;

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
        /// Applies threshold overrides and adjustments from an action
        /// </summary>
        public static void ApplyThresholdOverrides(Action action, Actor source, Actor? target = null)
        {
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

            // Apply threshold adjustments (adds to current/default)
            if (action.RollMods.CriticalMissThresholdAdjustment != 0)
            {
                _thresholdManager.AdjustCriticalMissThreshold(source, action.RollMods.CriticalMissThresholdAdjustment);
                if (target != null && action.RollMods.ApplyThresholdAdjustmentsToBoth)
                {
                    _thresholdManager.AdjustCriticalMissThreshold(target, action.RollMods.CriticalMissThresholdAdjustment);
                }
            }
            if (action.RollMods.CriticalHitThresholdAdjustment != 0)
            {
                _thresholdManager.AdjustCriticalHitThreshold(source, action.RollMods.CriticalHitThresholdAdjustment);
                if (target != null && action.RollMods.ApplyThresholdAdjustmentsToBoth)
                {
                    _thresholdManager.AdjustCriticalHitThreshold(target, action.RollMods.CriticalHitThresholdAdjustment);
                }
            }
            if (action.RollMods.ComboThresholdAdjustment != 0)
            {
                _thresholdManager.AdjustComboThreshold(source, action.RollMods.ComboThresholdAdjustment);
                if (target != null && action.RollMods.ApplyThresholdAdjustmentsToBoth)
                {
                    _thresholdManager.AdjustComboThreshold(target, action.RollMods.ComboThresholdAdjustment);
                }
            }
            if (action.RollMods.HitThresholdAdjustment != 0)
            {
                _thresholdManager.AdjustHitThreshold(source, action.RollMods.HitThresholdAdjustment);
                if (target != null && action.RollMods.ApplyThresholdAdjustmentsToBoth)
                {
                    _thresholdManager.AdjustHitThreshold(target, action.RollMods.HitThresholdAdjustment);
                }
            }
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

