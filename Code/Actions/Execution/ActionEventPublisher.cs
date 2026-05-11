using System;
using RPGGame.Audio;
using RPGGame.Combat.Events;

namespace RPGGame.Actions.Execution
{
    /// <summary>
    /// Centralizes all CombatEventBus publishing for action execution
    /// Handles action executed, hit, miss, death, and threshold events
    /// </summary>
    internal static class ActionEventPublisher
    {
        internal const double LowHealthThreshold = 0.20;

        /// <summary>
        /// Publishes action executed event
        /// </summary>
        public static CombatEvent PublishActionExecuted(Actor source, Actor? target, Action action, int rollValue, bool isCombo, bool isCritical)
        {
            var actionEvent = new CombatEvent(CombatEventType.ActionExecuted, source)
            {
                Target = target,
                Action = action,
                RollValue = rollValue,
                IsCombo = isCombo,
                IsCritical = isCritical
            };
            CombatEventBus.Instance.Publish(actionEvent);
            return actionEvent;
        }

        /// <summary>
        /// Publishes action hit event
        /// </summary>
        public static CombatEvent PublishActionHit(Actor source, Actor target, Action action, int rollValue, bool isCombo, bool isCritical)
        {
            var hitEvent = new CombatEvent(CombatEventType.ActionHit, source)
            {
                Target = target,
                Action = action,
                RollValue = rollValue,
                IsCombo = isCombo,
                IsCritical = isCritical
            };
            CombatEventBus.Instance.Publish(hitEvent);
            AudioCues.Trigger(ResolveHitCue(source, target, isCombo, isCritical));
            return hitEvent;
        }

        private static AudioCue ResolveHitCue(Actor source, Actor target, bool isCombo, bool isCritical)
        {
            if (source is Enemy && target is Character and not Enemy)
                return AudioCue.Combat_HeroHurt;

            return isCritical
                ? AudioCue.Combat_CriticalHit
                : isCombo
                    ? AudioCue.Combat_ComboComplete
                    : AudioCue.Combat_Hit;
        }

        /// <summary>
        /// Publishes action miss event
        /// </summary>
        public static CombatEvent PublishActionMiss(Actor source, Actor target, Action action, int rollValue, bool isCriticalMiss = false)
        {
            var missEvent = new CombatEvent(CombatEventType.ActionMiss, source)
            {
                Target = target,
                Action = action,
                RollValue = rollValue,
                IsMiss = true,
                IsCriticalMiss = isCriticalMiss
            };
            CombatEventBus.Instance.Publish(missEvent);
            AudioCues.Trigger(isCriticalMiss ? AudioCue.Combat_CriticalMiss : AudioCue.Combat_Miss);
            return missEvent;
        }

        /// <summary>
        /// Publishes enemy death event and processes outcomes
        /// </summary>
        public static void PublishEnemyDeath(Actor source, Actor target, Action action, int damage)
        {
            var deathEvent = new CombatEvent(CombatEventType.EnemyDied, source)
            {
                Target = target,
                Action = action,
                Damage = damage
            };
            CombatEventBus.Instance.Publish(deathEvent);
            Combat.Outcomes.OutcomeHandlerRegistry.Instance.ProcessOutcomes(action, deathEvent, source, target);
        }

        /// <summary>
        /// Publishes enemy health threshold event and processes outcomes
        /// </summary>
        public static void PublishHealthThreshold(Actor source, Actor target, Action action, double healthPercentage)
        {
            var thresholdEvent = new CombatEvent(CombatEventType.EnemyHealthThreshold, source)
            {
                Target = target,
                Action = action,
                HealthPercentage = healthPercentage
            };
            CombatEventBus.Instance.Publish(thresholdEvent);
            Combat.Outcomes.OutcomeHandlerRegistry.Instance.ProcessOutcomes(action, thresholdEvent, source, target);
        }

        /// <summary>
        /// Gets an actor's current HP ratio for crossing-threshold checks.
        /// </summary>
        internal static double GetActorHealthPercentage(Actor actor)
        {
            if (actor is not Character character) return 1.0;

            int maxHealth = character.GetEffectiveMaxHealth();
            if (maxHealth <= 0) return 0.0;

            return Math.Clamp((double)character.CurrentHealth / maxHealth, 0.0, 1.0);
        }

        /// <summary>
        /// Publishes a low-health event when an actor crosses from above 20% HP to at-or-below 20% HP.
        /// Dead actors are excluded so defeat cues remain distinct.
        /// </summary>
        internal static void PublishLowHealthThresholdIfCrossed(Actor actor, double healthPercentageBefore)
        {
            if (actor is not Character character) return;
            if (double.IsNaN(healthPercentageBefore) || double.IsInfinity(healthPercentageBefore)) return;
            if (healthPercentageBefore <= LowHealthThreshold) return;

            double healthPercentageAfter = GetActorHealthPercentage(actor);
            if (healthPercentageAfter <= 0.0 || healthPercentageAfter > LowHealthThreshold) return;
            if (!character.IsAlive) return;

            var eventType = actor is Enemy
                ? CombatEventType.EnemyLowHealth
                : CombatEventType.HeroLowHealth;

            var lowHealthEvent = new CombatEvent(eventType, actor)
            {
                Target = actor,
                HealthPercentage = healthPercentageAfter
            };
            CombatEventBus.Instance.Publish(lowHealthEvent);
        }
    }
}

