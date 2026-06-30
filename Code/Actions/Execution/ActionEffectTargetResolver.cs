using System;
using System.Collections.Generic;

namespace RPGGame.Actions.Execution
{
    /// <summary>
    /// Resolves which actor receives action effects (enemy vs self) during combat execution.
    /// </summary>
    internal static class ActionEffectTargetResolver
    {
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
