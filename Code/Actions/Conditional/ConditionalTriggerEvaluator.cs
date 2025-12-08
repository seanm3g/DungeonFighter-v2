using System;
using System.Collections.Generic;
using System.Linq;
using RPGGame;
using RPGGame.Combat.Events;

namespace RPGGame.Actions.Conditional
{
    /// <summary>
    /// Evaluates trigger conditions to determine if actions should fire
    /// </summary>
    public class ConditionalTriggerEvaluator
    {
        private readonly Dictionary<Actor, Action?> _lastActions = new Dictionary<Actor, Action?>();

        /// <summary>
        /// Evaluates a list of conditions - all must be true for trigger to fire
        /// </summary>
        public bool EvaluateConditions(List<TriggerCondition> conditions, CombatEvent evt, Actor source, Actor? target, Action? action)
        {
            if (conditions == null || conditions.Count == 0)
                return true; // No conditions means always trigger

            foreach (var condition in conditions)
            {
                if (!EvaluateCondition(condition, evt, source, target, action))
                {
                    return false; // All conditions must be true
                }
            }

            return true;
        }

        /// <summary>
        /// Evaluates a single condition
        /// </summary>
        private bool EvaluateCondition(TriggerCondition condition, CombatEvent evt, Actor source, Actor? target, Action? action)
        {
            return condition.Type switch
            {
                TriggerConditionType.OnMiss => evt.IsMiss,
                TriggerConditionType.OnNormalHit => evt.Type == CombatEventType.ActionHit && !evt.IsCombo && !evt.IsCritical,
                TriggerConditionType.OnComboHit => evt.IsCombo,
                TriggerConditionType.OnCriticalHit => evt.IsCritical,
                TriggerConditionType.OnExactRollValue => evt.RollValue == (int)(condition.Value ?? 0),
                TriggerConditionType.IfSameActionUsedPreviously => CheckSameActionUsedPreviously(source, action),
                TriggerConditionType.IfDifferentActionUsedPreviously => CheckDifferentActionUsedPreviously(source, action),
                TriggerConditionType.IfActionHasTag => action != null && action.Tags.Contains(condition.Tag ?? ""),
                TriggerConditionType.IfGearHasTag => CheckGearHasTag(source, condition.Tag ?? ""),
                TriggerConditionType.IfTargetHealthBelow => target != null && GetHealthPercentage(target) <= (double)(condition.Value ?? 0.0),
                TriggerConditionType.IfTargetHealthAbove => target != null && GetHealthPercentage(target) >= (double)(condition.Value ?? 0.0),
                TriggerConditionType.IfSourceHealthBelow => GetHealthPercentage(source) <= (double)(condition.Value ?? 0.0),
                TriggerConditionType.IfSourceHealthAbove => GetHealthPercentage(source) >= (double)(condition.Value ?? 0.0),
                TriggerConditionType.IfComboPosition => CheckComboPosition(source, condition.ComboPosition ?? 0),
                TriggerConditionType.IfComboLength => CheckComboLength(source, (int)(condition.Value ?? 0)),
                _ => false
            };
        }

        private bool CheckSameActionUsedPreviously(Actor source, Action? currentAction)
        {
            if (currentAction == null) return false;
            _lastActions.TryGetValue(source, out var lastAction);
            return lastAction != null && lastAction.Name == currentAction.Name;
        }

        private bool CheckDifferentActionUsedPreviously(Actor source, Action? currentAction)
        {
            if (currentAction == null) return false;
            _lastActions.TryGetValue(source, out var lastAction);
            return lastAction != null && lastAction.Name != currentAction.Name;
        }

        private bool CheckGearHasTag(Actor source, string tag)
        {
            if (source is Character character)
            {
                // Check weapon tags
                if (character.Weapon != null && character.Weapon.Tags != null && character.Weapon.Tags.Contains(tag))
                    return true;
                
                // Check armor tags (Body, Head, Feet)
                if (character.Body != null && character.Body.Tags != null && character.Body.Tags.Contains(tag))
                    return true;
                if (character.Head != null && character.Head.Tags != null && character.Head.Tags.Contains(tag))
                    return true;
                if (character.Feet != null && character.Feet.Tags != null && character.Feet.Tags.Contains(tag))
                    return true;
            }
            else if (source is Enemy enemy)
            {
                if (enemy.Weapon != null && enemy.Weapon.Tags != null && enemy.Weapon.Tags.Contains(tag))
                    return true;
            }
            
            return false;
        }

        private double GetHealthPercentage(Actor actor)
        {
            int currentHealth = 0;
            int maxHealth = 0;

            if (actor is Character character)
            {
                currentHealth = character.CurrentHealth;
                maxHealth = character.MaxHealth;
            }
            else if (actor is Enemy enemy)
            {
                currentHealth = enemy.CurrentHealth;
                maxHealth = enemy.MaxHealth;
            }

            if (maxHealth == 0) return 0.0;
            return (double)currentHealth / maxHealth;
        }

        private bool CheckComboPosition(Actor source, int position)
        {
            if (source is Character character)
            {
                return character.Effects.ComboStep == position;
            }
            return false;
        }

        private bool CheckComboLength(Actor source, int length)
        {
            if (source is Character character)
            {
                var comboActions = character.Actions.GetComboActions();
                return comboActions.Count == length;
            }
            return false;
        }

        /// <summary>
        /// Records that an action was used (for tracking previous actions)
        /// </summary>
        public void RecordActionUsed(Actor source, Action action)
        {
            _lastActions[source] = action;
        }

        /// <summary>
        /// Clears action history (useful for testing or resetting)
        /// </summary>
        public void ClearHistory()
        {
            _lastActions.Clear();
        }
    }
}

