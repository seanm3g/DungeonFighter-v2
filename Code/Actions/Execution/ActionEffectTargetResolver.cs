using System;
using System.Collections.Generic;
using System.Linq;

namespace RPGGame.Actions.Execution
{
    /// <summary>
    /// Resolves which actor receives action effects (enemy vs self) during combat execution.
    /// </summary>
    internal static class ActionEffectTargetResolver
    {
        /// <summary>Test hook: when set, picks confused target deterministically (return value must be source or nominalTarget).</summary>
        internal static Func<Actor, Actor, Actor>? ConfusionTargetPickerOverride { get; set; }

        internal static bool IsConfusionActive(Actor? source) =>
            source != null && source.IsConfused && source.ConfusionTurns > 0;

        internal static bool ShouldRandomizeTargetForConfusion(Action? action) =>
            action != null
            && action.Target != TargetType.Self
            && action.Target != TargetType.Environment;

        /// <summary>
        /// Nominal combat opponent remapped to a random living participant while <see cref="Actor.IsConfused"/>.
        /// </summary>
        internal static Actor ResolveConfusedCombatTarget(Action action, Actor source, Actor nominalTarget)
        {
            if (!IsConfusionActive(source) || !ShouldRandomizeTargetForConfusion(action))
                return nominalTarget;

            return PickRandomConfusionTarget(source, nominalTarget);
        }

        internal static Actor PickRandomConfusionTarget(Actor source, Actor nominalTarget)
        {
            if (ConfusionTargetPickerOverride != null)
                return ConfusionTargetPickerOverride(source, nominalTarget);

            var candidates = new List<Actor> { source };
            if (nominalTarget != null && !ReferenceEquals(nominalTarget, source))
                candidates.Add(nominalTarget);

            candidates = candidates.Where(IsAlive).Distinct().ToList();
            if (candidates.Count == 0)
                return nominalTarget ?? source;
            if (candidates.Count == 1)
                return candidates[0];

            return RandomUtility.NextElement(candidates);
        }

        private static bool IsAlive(Actor actor)
        {
            if (actor is Character character)
                return character.IsAlive && character.CurrentHealth > 0;
            return actor != null;
        }

        /// <summary>
        /// Primary recipient for damage, healing, and display when an action resolves.
        /// </summary>
        internal static Actor ResolvePrimaryRecipient(Action action, Actor source, Actor combatTarget)
        {
            if (action.Target == TargetType.Self)
                return source;
            if (action.Target == TargetType.SelfAndTarget)
                return combatTarget;
            return combatTarget;
        }

        /// <summary>
        /// Actor that should receive a status effect from this action.
        /// </summary>
        internal static Actor ResolveStatusEffectRecipient(
            Action action,
            string effectType,
            Actor source,
            Actor combatTarget)
        {
            if (action.Target == TargetType.Self)
                return source;

            if (action.Advanced?.SelfTargetEffects != null
                && action.Advanced.SelfTargetEffects.Contains(effectType, StringComparer.OrdinalIgnoreCase))
                return source;

            return combatTarget;
        }

        /// <summary>
        /// Heals the attacker for action lifesteal plus equipment modification lifesteal after damage is dealt.
        /// </summary>
        internal static void ApplyLifestealHealing(Actor source, Action action, int damageDealt)
        {
            if (damageDealt <= 0 || source == null || action == null)
                return;

            double percent = action.Advanced?.LifestealPercent ?? 0;
            if (source is Character character)
                percent += character.GetModificationLifesteal();
            if (percent <= 0)
                return;

            int heal = Math.Max(1, (int)Math.Round(damageDealt * percent));
            ActionUtilities.ApplyHealing(source, heal);
            if (source is Character healCharacter)
                ActionStatisticsTracker.RecordHealingReceived(healCharacter, heal);
        }
    }
}
