using System;
using System.Collections.Concurrent;
using RPGGame.Actions.RollModification;
using RPGGame.Utils;

namespace RPGGame
{
    /// <summary>
    /// Handles action selection logic for different Actor types
    /// Refactored to focus purely on selection using shared utilities
    /// </summary>
    public static class ActionSelector
    {
        // Store the last action selection roll for consistency - using thread-safe concurrent dictionary
        private static readonly ConcurrentDictionary<Actor, int> _lastActionSelectionRolls = new ConcurrentDictionary<Actor, int>();

        /// <summary>
        /// Unnamed normal attack for roll 6-13. 100% damage, 1.0 speed, no modifiers.
        /// Empty name ensures combat message shows "X hits Y for Z damage" (no action name).
        /// </summary>
        private static readonly Action NormalAttackAction = new Action(
            name: "",
            damageMultiplier: 1.0,
            length: 1.0,
            isComboAction: false);

        /// <summary>
        /// Selects an action based on Actor type - heroes use roll-based logic, enemies use random selection
        /// </summary>
        /// <param name="source">The Actor selecting the action</param>
        /// <returns>The selected action or null if no action available</returns>
        public static Action? SelectActionByEntityType(Actor source)
        {
            // Heroes/Characters use advanced roll-based system with combos
            if (source is Character character && !(character is Enemy))
            {
                return SelectActionBasedOnRoll(source);
            }
            // Enemies use simple random probability-based selection
            else
            {
                return SelectEnemyActionBasedOnRoll(source);
            }
        }

        /// <summary>
        /// Selects an action based on dice roll logic:
        /// - Natural 20 always selects a combo-slot action (when available).
        /// - Otherwise compares preview attack total (same components as <see cref="RPGGame.Actions.Execution.ActionExecutionFlow"/>:
        ///   modified d20 + roll bonuses; peeked ACCURACY shifts the effective combo threshold, not the d20) to the effective combo threshold
        ///   (threshold manager + COMBO effect bonuses + combo action roll-mod threshold fields).
        /// For heroes only.
        /// </summary>
        /// <param name="source">The Actor selecting the action</param>
        /// <returns>The selected action or null if no action available</returns>
        public static Action? SelectActionBasedOnRoll(Actor source)
        {
            if (source.ActionPool.Count == 0)
                return null;

            // Check if Actor is stunned
            if (source.IsStunned)
                return null;

            // Roll first to determine what type of action to use
            int baseRoll = Dice.Roll(1, 20);

            // Store the base roll for use in the main execution
            _lastActionSelectionRolls.AddOrUpdate(source, baseRoll, (_, _) => baseRoll);

            if (baseRoll == 20)
                return SelectComboAction(source);

            PeekRollAccuracyAndComboBonuses(source as Character, out int acc, out int effectComboBonus);
            var comboActions = ActionUtilities.GetComboActions(source);
            if (comboActions.Count == 0)
                return SelectComboAction(source);

            int actionIdx = ActionUtilities.GetComboStep(source) % comboActions.Count;
            Action comboAction = comboActions[actionIdx];
            int totalCombo = PreviewAttackTotal(source, comboAction, baseRoll);
            int comboThreshold = GetEffectiveComboThresholdForSelection(source, comboAction, effectComboBonus, acc);

            if (totalCombo >= comboThreshold)
                return SelectComboAction(source);
            return SelectNormalAction(source);
        }

        /// <summary>
        /// Returns whether a natural <paramref name="baseRoll"/> would select a combo-slot action
        /// (same rules as <see cref="SelectActionBasedOnRoll"/>). Used by Action Interaction Lab so a
        /// catalog pick is only forced when the test roll would actually be a combo; otherwise the normal
        /// unnamed attack is used.
        /// </summary>
        public static bool WouldNaturalRollSelectComboAction(Actor source, int baseRoll)
        {
            if (source.ActionPool.Count == 0 || source.IsStunned)
                return false;

            var comboActions = ActionUtilities.GetComboActions(source);
            if (comboActions.Count == 0)
                return false;

            if (baseRoll == 20)
                return true;

            PeekRollAccuracyAndComboBonuses(source as Character, out int acc, out int effectComboBonus);
            int actionIdx = ActionUtilities.GetComboStep(source) % comboActions.Count;
            Action comboAction = comboActions[actionIdx];
            int totalCombo = PreviewAttackTotal(source, comboAction, baseRoll);
            int comboThreshold = GetEffectiveComboThresholdForSelection(source, comboAction, effectComboBonus, acc);
            return totalCombo >= comboThreshold;
        }

        /// <summary>
        /// Sum of peeked ACCURACY from ACTION/ATTACK/ABILITY queues (applied as threshold shifts on the next attack, not added to the d20).
        /// Heroes: slot + FIFO + ATTACK + ABILITY peek (matches <see cref="RPGGame.Actions.Execution.ActionExecutionFlow"/>).
        /// Enemies: FIFO layer only.
        /// </summary>
        public static int PeekQueuedAccuracyBonus(Character? c)
        {
            if (c == null) return 0;
            if (c is Enemy)
            {
                int acc = 0;
                foreach (var bonus in c.Effects.PeekPendingActionBonusesNextHeroRoll())
                {
                    if (string.Equals(bonus.Type, "ACCURACY", StringComparison.OrdinalIgnoreCase))
                        acc += (int)bonus.Value;
                }
                return acc;
            }
            PeekRollAccuracyAndComboBonuses(c, out int accuracyAccumulator, out _);
            return accuracyAccumulator;
        }

        /// <summary>
        /// Peeks ACCURACY (threshold shift) and COMBO (adjusts combo threshold) bonuses
        /// that <see cref="RPGGame.Actions.Execution.ActionExecutionFlow"/> would consume on this roll, without consuming.
        /// </summary>
        private static void PeekRollAccuracyAndComboBonuses(Character? c, out int accuracyAccumulator, out int effectComboBonus)
        {
            accuracyAccumulator = 0;
            effectComboBonus = 0;
            if (c == null || c is Enemy)
                return;

            var comboActions = ActionUtilities.GetComboActions(c);
            int comboLength = comboActions.Count;
            if (comboLength > 0)
            {
                int currentSlot = c.ComboStep % comboLength;
                foreach (var bonus in c.Effects.GetPendingActionBonusesForSlot(currentSlot))
                {
                    switch ((bonus.Type ?? "").ToUpper())
                    {
                        case "ACCURACY": accuracyAccumulator += (int)bonus.Value; break;
                        case "COMBO": effectComboBonus += (int)bonus.Value; break;
                    }
                }
            }
            foreach (var bonus in c.Effects.PeekPendingActionBonusesNextHeroRoll())
            {
                switch ((bonus.Type ?? "").ToUpper())
                {
                    case "ACCURACY": accuracyAccumulator += (int)bonus.Value; break;
                    case "COMBO": effectComboBonus += (int)bonus.Value; break;
                }
            }
            foreach (var bonus in c.Effects.PeekAttackBonuses())
            {
                switch (bonus.Type.ToUpper())
                {
                    case "ACCURACY": accuracyAccumulator += (int)bonus.Value; break;
                    case "COMBO": effectComboBonus += (int)bonus.Value; break;
                }
            }
            foreach (var bonus in c.Effects.PeekAbilityBonuses())
            {
                switch (bonus.Type.ToUpper())
                {
                    case "ACCURACY": accuracyAccumulator += (int)bonus.Value; break;
                    case "COMBO": effectComboBonus += (int)bonus.Value; break;
                }
            }
        }

        /// <summary>
        /// Approximates effective combo threshold for selection. Sheet combo <em>adjustments</em> on the action apply to the next roll only (see <see cref="RPGGame.Actions.Execution.ActionExecutionFlow"/>).
        /// </summary>
        private static int GetEffectiveComboThresholdForSelection(Actor source, Action comboAction, int effectComboBonus, int accuracyAcc)
        {
            var tm = RollModificationManager.GetThresholdManager();
            int t = tm.GetComboThreshold(source);
            if (comboAction.RollMods.ComboThresholdOverride > 0
                && !RollModificationManager.ShouldDeferRollModThresholdPackages(comboAction))
                t = comboAction.RollMods.ComboThresholdOverride;
            if (effectComboBonus != 0)
                t -= effectComboBonus;
            if (accuracyAcc != 0)
                t -= accuracyAcc;
            return Math.Max(1, t);
        }

        /// <summary>
        /// Preview attack total for an action path: modified base + roll bonus (no temp consume).
        /// Peeked ACCURACY is not added here — it is folded into <see cref="GetEffectiveComboThresholdForSelection"/> as a threshold shift.
        /// </summary>
        private static int PreviewAttackTotal(Actor source, Action action, int baseRoll)
        {
            int modified = RollModificationManager.ApplyActionRollModifications(baseRoll, action, source, null);
            int rollBonus = ActionUtilities.CalculateRollBonus(source, action, consumeTempBonus: false);
            return modified + rollBonus;
        }

        /// <summary>
        /// Selects an enemy action based on roll thresholds
        /// </summary>
        /// <param name="source">The enemy Actor</param>
        /// <returns>The selected action or null if no action available</returns>
        public static Action? SelectEnemyActionBasedOnRoll(Actor source)
        {
            if (source.ActionPool.Count == 0)
                return null;

            // Enemies can be stunned too
            if (source.IsStunned)
                return null;

            int baseRoll = Dice.Roll(1, 20);

            // Store the base roll for use in hit calculation (same as heroes)
            _lastActionSelectionRolls.AddOrUpdate(source, baseRoll, (_, _) => baseRoll);

            if (baseRoll == 20)
            {
                var comboActions20 = ActionUtilities.GetComboActions(source);
                if (comboActions20.Count > 0)
                {
                    int idx20 = ActionUtilities.GetComboStep(source) % comboActions20.Count;
                    return comboActions20[idx20];
                }
                return source.SelectAction();
            }

            PeekRollAccuracyAndComboBonuses(source as Character, out int acc, out int effectComboBonus);
            var comboActions = ActionUtilities.GetComboActions(source);
            if (comboActions.Count == 0)
                return source.SelectAction();

            // Use first combo action for threshold preview; actual pick stays random when multiple (same as prior roll usage pattern).
            Action comboPreview = comboActions[0];
            int totalCombo = PreviewAttackTotal(source, comboPreview, baseRoll);
            int comboThreshold = GetEffectiveComboThresholdForSelection(source, comboPreview, effectComboBonus, acc);

            if (totalCombo >= comboThreshold)
            {
                int idx = ActionUtilities.GetComboStep(source) % comboActions.Count;
                return comboActions[idx];
            }

            return SelectNormalAction(source);
        }

        /// <summary>
        /// Selects a combo action for the given Actor
        /// Only selects actions that are actually in the combo sequence
        /// If no combo actions are in the sequence, falls back to a normal action
        /// </summary>
        /// <param name="source">The Actor to select combo action for</param>
        /// <returns>Selected combo action from sequence, or fallback to normal action if sequence is empty, or null if no actions available</returns>
        private static Action? SelectComboAction(Actor source)
        {
            var comboActions = ActionUtilities.GetComboActions(source);
            if (comboActions.Count > 0)
            {
                int actionIdx = ActionUtilities.GetComboStep(source) % comboActions.Count;
                return comboActions[actionIdx];
            }
            else
            {
                // If combo sequence is empty, fall back to normal action instead of searching ActionPool
                // This ensures that actions not in the combo sequence (like CHANNEL) won't be used
                // unless they're explicitly added to the combo sequence
                return SelectNormalAction(source);
            }
        }

        /// <summary>
        /// Selects a normal (non-combo) action for the given Actor.
        /// Used when preview attack total is below the effective combo threshold (and not natural 20).
        /// For characters: returns unnamed normal attack (displays "X hits Y for Z damage").
        /// For enemies: first non-combo action, or first available action.
        /// </summary>
        /// <param name="source">The Actor to select normal action for</param>
        /// <returns>Unnamed normal attack for characters, or non-combo/first action for enemies, or null if no actions available</returns>
        private static Action? SelectNormalAction(Actor source)
        {
            if (source.ActionPool.Count == 0)
            {
                DebugLogger.LogFormat("ActionSelector",
                    "WARNING: {0} has no actions in ActionPool when trying to select normal action", source.Name);
                return null;
            }

            // For characters, use unnamed normal attack (displays "X hits Y for Z damage")
            if (source is Character character && !(character is Enemy))
            {
                return NormalAttackAction;
            }

            // Enemies: first non-combo action, or first available action
            foreach (var actionEntry in source.ActionPool)
            {
                if (!actionEntry.action.IsComboAction)
                    return actionEntry.action;
            }
            return source.ActionPool[0].action;
        }

        /// <summary>
        /// Gets the action roll for an Actor - uses stored roll for both heroes and enemies
        /// </summary>
        /// <param name="source">The Actor to get roll for</param>
        /// <returns>The stored roll or a new roll if not found</returns>
        public static int GetActionRoll(Actor source)
        {
            // Both heroes and enemies use the stored roll from action selection
            if (_lastActionSelectionRolls.TryGetValue(source, out int roll))
            {
                return roll;
            }
            else
            {
                // Fallback to a new roll if not found (shouldn't happen in normal flow)
                return Dice.Roll(1, 20);
            }
        }

        /// <summary>
        /// Clears stored action selection rolls (useful for testing or cleanup)
        /// </summary>
        public static void ClearStoredRolls()
        {
            _lastActionSelectionRolls.Clear();
        }

        /// <summary>
        /// Sets the stored d20 used by <see cref="GetActionRoll"/> when action selection is bypassed (e.g. forced action in dev lab).
        /// Pair with <see cref="Dice.SetTestRoll"/> if other random rolls in the same turn should match.
        /// </summary>
        public static void SetStoredActionRoll(Actor source, int roll)
        {
            _lastActionSelectionRolls.AddOrUpdate(source, roll, (_, _) => roll);
        }
    }
}

