using System;
using System.Collections.Generic;
using System.Linq;
using Avalonia.Media;
using RPGGame.ActionInteractionLab;
using RPGGame.UI.ColorSystem;
using RPGGame.Utils;

namespace RPGGame
{
    /// <summary>
    /// Shared utilities for action-related operations
    /// Consolidates duplicate logic from ActionExecutor, ActionSelector, and CombatActions
    /// </summary>
    public static class ActionUtilities
    {
        /// <summary>
        /// Gets combo actions for an entity
        /// </summary>
        /// <param name="source">The entity to get combo actions for</param>
        /// <returns>List of combo actions</returns>
        public static List<Action> GetComboActions(Actor source)
        {
            if (source is Character character)
            {
                return character.GetComboActions();
            }
            else
            {
                // For enemies, get combo actions from ActionPool
                // Optimized: Single pass instead of LINQ chain
                var comboActions = new List<Action>();
                foreach (var actionEntry in source.ActionPool)
                {
                    if (actionEntry.action.IsComboAction)
                    {
                        comboActions.Add(actionEntry.action);
                    }
                }
                return comboActions;
            }
        }
        
        /// <summary>
        /// Gets the current combo step for an entity
        /// </summary>
        /// <param name="source">The entity to get combo step for</param>
        /// <returns>Current combo step</returns>
        public static int GetComboStep(Actor source)
        {
            if (source is Character character)
            {
                return character.ComboStep;
            }
            else
            {
                return 0; // Enemies don't have combo steps
            }
        }

        /// <summary>
        /// When true, combo-slot action selection uses <see cref="Character.ComboStep"/> order; when false, strip index
        /// still follows <c>ComboStep % stripLength</c> in normal combat, with salt-based override for lab/catalog previews.
        /// </summary>
        public static bool UsesOrderedComboSequence(Character character) =>
            character.GetEffectiveIntelligence() >= GameConstants.ComboSequenceIntelligenceThreshold;

        /// <summary>
        /// Resolves which combo-strip slot to use for this attack's combo action pick.
        /// Ordered (INT high): <c>ComboStep % count</c>.
        /// Below INT threshold: same index as ordered — <c>ComboStep % count</c> — so the strip, HUD highlight, and
        /// combo-path pick stay aligned; <see cref="Character.IncrementComboStep"/> still ignores authored
        /// <c>ComboRouter</c> jumps (linear advance only). When <paramref name="deterministicSalt"/> is set
        /// (e.g. Action Lab tying the catalog to a test d20), index is <c>(salt % count + count) % count</c>.
        /// </summary>
        public static int ResolveComboStripIndex(Character character, IReadOnlyList<Action> comboActions, int? deterministicSalt)
        {
            int count = comboActions?.Count ?? 0;
            if (count <= 0)
                return 0;
            // Only one combo slot: index is always 0 (legacy chaotic path used 1d(count); count==1 would call Roll(1,1), invalid).
            if (count == 1)
                return 0;
            if (UsesOrderedComboSequence(character))
                return character.ComboStep % count;
            if (deterministicSalt.HasValue)
            {
                int s = deterministicSalt.Value;
                return (s % count + count) % count;
            }
            // Low INT: deterministic strip index from ComboStep (matches UI highlight); no per-swing random slot.
            return character.ComboStep % count;
        }

        /// <summary>
        /// Slot index for pending ACTION cadence on the hero: matches executed combo action when possible, else ComboStep.
        /// </summary>
        public static int GetComboSlotForPendingBonuses(Character character, Action? selectedAction, IReadOnlyList<Action> comboActions)
        {
            int n = comboActions?.Count ?? 0;
            if (n <= 0 || comboActions == null)
                return 0;
            if (selectedAction != null && selectedAction.IsComboAction)
            {
                int idx = TryGetComboActionSlotIndex(selectedAction, comboActions);
                if (idx >= 0)
                    return idx;
            }
            return character.ComboStep % n;
        }

        /// <summary>
        /// Calculates roll bonus based on entity type and action
        /// </summary>
        /// <param name="source">The entity performing the action</param>
        /// <param name="action">The action being performed (optional)</param>
        /// <param name="consumeTempBonus">Whether to consume the temporary roll bonus (true for execution, false for selection)</param>
        /// <returns>Roll bonus value</returns>
        public static int CalculateRollBonus(Actor source, Action? action = null, bool consumeTempBonus = true)
        {
            return CombatCalculator.CalculateRollBonus(source, action, GetComboActions(source), GetComboStep(source), consumeTempBonus);
        }

        /// <summary>
        /// Action used for roll-bonus HUD preview: last executed action, else current combo-slot action, else first attack/spell in pool.
        /// </summary>
        public static Action? GetPreviewActionForRollBonus(Character c)
        {
            if (c.Effects.LastAction != null)
                return c.Effects.LastAction;
            var comboActions = GetComboActions(c);
            if (comboActions.Count > 0)
                return comboActions[GetComboStep(c) % comboActions.Count];
            foreach (var entry in c.ActionPool)
            {
                if (entry.action.Type == ActionType.Attack || entry.action.Type == ActionType.Spell)
                    return entry.action;
            }
            return null;
        }

        /// <summary>
        /// 0-based index of <paramref name="action"/> in <paramref name="comboActions"/> (reference match, then name).
        /// Returns -1 when not found.
        /// </summary>
        public static int TryGetComboActionSlotIndex(Action action, IReadOnlyList<Action> comboActions)
        {
            if (comboActions == null || comboActions.Count == 0 || action == null)
                return -1;
            for (int i = 0; i < comboActions.Count; i++)
            {
                if (ReferenceEquals(comboActions[i], action))
                    return i;
            }
            if (!string.IsNullOrEmpty(action.Name))
            {
                for (int i = 0; i < comboActions.Count; i++)
                {
                    if (string.Equals(comboActions[i].Name, action.Name, StringComparison.OrdinalIgnoreCase))
                        return i;
                }
            }
            return -1;
        }

        /// <summary>
        /// Combo slot that receives pending ACTION / Ability-cadence modifiers (for next action in the strip).
        /// Uses the executed action's index in the ordered combo so the last slot (finisher) wraps to 0;
        /// falls back to <see cref="GetComboStep"/> when the action is not found in the list.
        /// </summary>
        public static int GetNextComboSlotForPendingBonuses(Character character, Action? executed, List<Action> comboActions)
        {
            if (comboActions == null || comboActions.Count == 0)
                return 0;
            int n = comboActions.Count;
            if (executed == null)
                return (character.ComboStep + 1) % n;

            int idx = TryGetComboActionSlotIndex(executed, comboActions);
            if (idx < 0)
                idx = character.ComboStep % n;
            return (idx + 1) % n;
        }

        /// <summary>
        /// Exponent for combo amplification: <c>Math.Pow(GetComboAmplifier(), exponent)</c>.
        /// Uses the action's **position in the combo strip** (first slot = 0 → 1.0×, second = 1 → baseline AMP, then further scaling).
        /// Opener/finisher flags do not reorder tiers—only strip order matters, matching the HUD sequence.
        /// When the action is not in the list, falls back to <see cref="Character.ComboStep"/> modulo length.
        /// </summary>
        public static int GetComboAmplificationExponent(Actor source, Action action, List<Action> comboActions)
        {
            if (comboActions.Count == 0 || !action.IsComboAction)
                return 0;

            int slot = TryGetComboActionSlotIndex(action, comboActions);
            if (slot >= 0)
                return slot;

            // Enemy derives from Character; use Character for ComboStep on both heroes and enemies.
            return source is Character ch ? ch.ComboStep % comboActions.Count : 0;
        }

        /// <summary>
        /// Calculates damage multiplier based on entity type and action
        /// Uses amplification from the action's combo-strip slot index (first slot 1.0×, then baseline^1, baseline^2, …).
        /// </summary>
        /// <param name="source">The entity performing the action</param>
        /// <param name="action">The action being performed</param>
        /// <returns>Damage multiplier value</returns>
        public static double CalculateDamageMultiplier(Actor source, Action action)
        {
            if (source is Character character)
            {
                // Only apply combo amplification to combo actions
                if (action.IsComboAction)
                {
                    var comboActions = character.GetComboActions();
                    // Action lab: the strip is configured on the lab clone; lab enemies often have no combo sequence.
                    // Use the player's sequence so forced catalog actions still get step-based amplification.
                    if (comboActions.Count == 0
                        && character is Enemy
                        && ActionInteractionLabSession.Current is { } labSession
                        && ReferenceEquals(character, labSession.LabEnemy))
                    {
                        comboActions = labSession.LabPlayer.GetComboActions();
                    }
                    if (comboActions.Count > 0)
                    {
                        double baseAmp = character.GetComboAmplifier();
                        int exponent = GetComboAmplificationExponent(source, action, comboActions);
                        double mult = Math.Pow(baseAmp, exponent);
                        int comboStep = character.ComboStep;
                        return ChainPositionBonusApplier.AdjustComboDamageMultiplier(mult, source, action, comboActions, comboStep);
                    }
                }
            }
            else if (source is Enemy enemy)
            {
                // Enemies also get combo amplification (same as heroes)
                if (action.IsComboAction)
                {
                    var comboActions = enemy.GetComboActions();
                    if (comboActions.Count > 0)
                    {
                        double baseAmp = enemy.GetComboAmplifier();
                        int exponent = GetComboAmplificationExponent(source, action, comboActions);
                        double mult = Math.Pow(baseAmp, exponent);
                        int comboStep = enemy.ComboStep;
                        return ChainPositionBonusApplier.AdjustComboDamageMultiplier(mult, source, action, comboActions, comboStep);
                    }
                }
            }
            return 1.0;
        }

        /// <summary>
        /// Calculates the amount of healing for a healing action
        /// </summary>
        /// <param name="source">The entity performing the healing</param>
        /// <param name="action">The healing action</param>
        /// <returns>Healing amount</returns>
        public static int CalculateHealAmount(Actor source, Action action)
        {
            // Base healing from action properties
            int baseHeal = action.Advanced.HealAmount;
            
            // Add technique-based healing for characters
            if (source is Character character)
            {
                baseHeal += character.Technique;
            }
            else if (source is Enemy enemy)
            {
                // For enemies, use a simple calculation based on their stats
                baseHeal += enemy.Technique;
            }
            
            return Math.Max(1, baseHeal); // Ensure at least 1 healing
        }

        /// <summary>
        /// Applies damage to target entity
        /// </summary>
        /// <param name="target">The entity receiving damage</param>
        /// <param name="damage">The amount of damage to apply</param>
        public static void ApplyDamage(Actor target, int damage)
        {
            if (target is Character targetCharacter)
            {
                targetCharacter.TakeDamage(damage);
            }
            else if (target is Enemy targetEnemy)
            {
                targetEnemy.TakeDamage(damage);
            }
        }

        /// <summary>
        /// Applies healing to a target entity
        /// </summary>
        /// <param name="target">The entity receiving healing</param>
        /// <param name="amount">The amount of healing to apply</param>
        public static void ApplyHealing(Actor target, int amount)
        {
            if (target is Character character)
            {
                character.Heal(amount);
            }
            else if (target is Enemy enemy)
            {
                // For enemies, we need to add a Heal method or use direct health modification
                enemy.CurrentHealth = Math.Min(enemy.MaxHealth, enemy.CurrentHealth + amount);
            }
        }

        /// <summary>
        /// Gets the current health of an entity
        /// </summary>
        /// <param name="entity">The entity to get health for</param>
        /// <returns>Current health value</returns>
        public static int GetEntityHealth(Actor entity)
        {
            return entity switch
            {
                Enemy enemy => enemy.CurrentHealth,
                Character character => character.CurrentHealth,
                _ => 0
            };
        }

        /// <summary>
        /// Handles unique action chance for characters
        /// </summary>
        /// <param name="character">The character to check for unique action chance</param>
        /// <param name="selectedAction">The currently selected action</param>
        /// <returns>The action to use (may be different from selectedAction)</returns>
        public static Action HandleUniqueActionChance(Character character, Action selectedAction)
        {
            double uniqueActionChance = character.GetModificationUniqueActionChance();
            if (uniqueActionChance > 0.0)
            {
                double roll = Dice.Roll(1, 100) / 100.0;
                if (roll < uniqueActionChance)
                {
                    var availableUniqueActions = character.GetAvailableUniqueActions();
                    if (availableUniqueActions.Count > 0)
                    {
                        int randomIndex = availableUniqueActions.Count > 1 ? Dice.Roll(1, availableUniqueActions.Count) - 1 : 0;
                        selectedAction = availableUniqueActions[randomIndex];
                        // Use ColoredText for unique action message
                        var uniqueBuilder = new ColoredTextBuilder();
                        uniqueBuilder.Add(character.Name, ColorPalette.Player);
                        uniqueBuilder.Add(" channels unique power and uses ", Colors.White);
                        uniqueBuilder.Add(selectedAction.Name, ColorPalette.Warning);
                        uniqueBuilder.Add("!", Colors.White);
                        // Pass character to filter display for multi-character support
                        TextDisplayIntegration.DisplayCombatAction(uniqueBuilder.Build(), new List<ColoredText>(), null, null, character);
                    }
                }
            }
            return selectedAction;
        }

        /// <summary>
        /// Creates and adds a BattleEvent to the current battle narrative
        /// </summary>
        /// <param name="source">The entity performing the action</param>
        /// <param name="target">The target entity</param>
        /// <param name="action">The action being performed</param>
        /// <param name="damage">Damage dealt (0 for non-damage actions)</param>
        /// <param name="totalRoll">Total roll value</param>
        /// <param name="rollBonus">Roll bonus applied</param>
        /// <param name="isSuccess">Whether the action was successful</param>
        /// <param name="isCombo">Whether this was a combo action</param>
        /// <param name="comboStep">Current combo step</param>
        /// <param name="healAmount">Healing amount (0 for non-healing actions)</param>
        /// <param name="isCritical">Whether this was a critical hit</param>
        /// <param name="battleNarrative">The battle narrative to add the event to</param>
        public static void CreateAndAddBattleEvent(Actor source, Actor target, Action action, int damage, int totalRoll, int rollBonus, bool isSuccess, bool isCombo, int comboStep, int healAmount, bool isCritical, int naturalRoll, BattleNarrative? battleNarrative)
        {
            try
            {
                if (battleNarrative == null)
                {
                    return; // No active battle narrative
                }

                // Create the battle event
                var battleEvent = new BattleEvent
                {
                    Actor = source.Name,
                    Target = target.Name,
                    Action = action.Name,
                    Damage = damage,
                    IsSuccess = isSuccess,
                    IsCombo = isCombo,
                    ComboStep = comboStep,
                    IsHeal = healAmount > 0,
                    HealAmount = healAmount,
                    Roll = totalRoll - rollBonus, // Base roll without bonuses
                    NaturalRoll = naturalRoll, // Natural dice roll (1-20) before any modifications
                    Difficulty = 0, // Action doesn't have Difficulty property, use 0
                    IsCritical = isCritical,
                    ActorHealthBefore = GetEntityHealth(source),
                    TargetHealthBefore = GetEntityHealth(target),
                    ActorHealthAfter = GetEntityHealth(source),
                    TargetHealthAfter = GetEntityHealth(target) - damage + healAmount
                };

                // Add the event to the narrative
                battleNarrative.AddEvent(battleEvent);
            }
            catch (Exception)
            {
                // Log error but don't break combat
                if (!ActionExecutor.DisableCombatDebugOutput)
                {
                }
            }
        }
    }
}

