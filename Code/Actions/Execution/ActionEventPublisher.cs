using RPGGame.Combat.Events;

namespace RPGGame.Actions.Execution
{
    /// <summary>
    /// Centralizes all CombatEventBus publishing for action execution
    /// Handles action executed, hit, miss, death, and threshold events
    /// </summary>
    internal static class ActionEventPublisher
    {
        /// <summary>
        /// Publishes action executed event
        /// </summary>
        public static CombatEvent PublishActionExecuted(Actor source, Actor target, Action action, int rollValue, bool isCombo, bool isCritical)
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
            return hitEvent;
        }

        /// <summary>
        /// Publishes action miss event
        /// </summary>
        public static CombatEvent PublishActionMiss(Actor source, Actor target, Action action, int rollValue)
        {
            var missEvent = new CombatEvent(CombatEventType.ActionMiss, source)
            {
                Target = target,
                Action = action,
                RollValue = rollValue,
                IsMiss = true
            };
            CombatEventBus.Instance.Publish(missEvent);
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
    }
}

