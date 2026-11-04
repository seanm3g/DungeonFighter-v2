using System;
using System.Runtime.CompilerServices;
using RPGGame.Audio;
using RPGGame.Combat.Events;

namespace RPGGame.Actions.Execution
{
    /// <summary>
    /// Centralizes all CombatEventBus publishing for action execution
    /// Handles action executed, hit, miss, death, and threshold events.
    /// Hit/miss SFX is queued until the combat-log punchline reveal.
    /// </summary>
    internal static class ActionEventPublisher
    {
        internal const double LowHealthThreshold = 0.20;

        /// <summary>
        /// Publishes action executed event
        /// </summary>
        public static CombatEvent PublishActionExecuted(Actor source, Actor? target, Action action, int rollValue, bool isCombo, bool isCritical, int naturalRollValue = 0)
        {
            var actionEvent = new CombatEvent(CombatEventType.ActionExecuted, source)
            {
                Target = target,
                Action = action,
                RollValue = rollValue,
                NaturalRollValue = naturalRollValue,
                IsCombo = isCombo,
                IsCritical = isCritical
            };
            CombatEventBus.Instance.Publish(actionEvent);
            return actionEvent;
        }

        /// <summary>
        /// Publishes action hit event
        /// </summary>
        public static CombatEvent PublishActionHit(Actor source, Actor target, Action action, int rollValue, bool isCombo, bool isCritical, int naturalRollValue = 0)
        {
            var hitEvent = new CombatEvent(CombatEventType.ActionHit, source)
            {
                Target = target,
                Action = action,
                RollValue = rollValue,
                NaturalRollValue = naturalRollValue,
                IsCombo = isCombo,
                IsCritical = isCritical
            };
            CombatEventBus.Instance.Publish(hitEvent);
            AudioCues.QueueForPunchline(ResolveHitCue(source, target, isCombo, isCritical));
            return hitEvent;
        }

        private static AudioCue ResolveHitCue(Actor source, Actor target, bool isCombo, bool isCritical)
        {
            if (source is Enemy && target is Character and not Enemy)
                return AudioCue.Combat_HeroHurt;

            if (isCritical) return AudioCue.Combat_CriticalHit;
            if (!isCombo) return AudioCue.Combat_Hit;
            return source is Character hero and not Enemy
                ? ResolveComboStepCue(hero)
                : AudioCue.Combat_ComboComplete;
        }

        /// <summary>0-based note of each hero's last combo hit; the melody climbs one note per chained hit.</summary>
        private static readonly ConditionalWeakTable<Character, StrongBox<int>> comboNoteByHero = new();

        /// <summary>
        /// Combo hits climb one major-scale note per hit in the chain (capped at 7); the strip's last slot plays a
        /// jingle that starts on the note that hit would have played. The note follows the chain, not the slot, so
        /// routed jumps/skips and repeated slots never jump the pitch. A chain (re)starts on the strip's first slot.
        /// Read before the combo step advances, so <see cref="Character.ComboStep"/> is the slot being swung.
        /// </summary>
        internal static AudioCue ResolveComboStepCue(Character hero)
        {
            int count = hero.GetComboActions().Count;
            if (count <= 0) return AudioCue.Combat_ComboComplete;
            int slot = hero.ReplacedStripSlot is int replaced && replaced >= 0 && replaced < count
                ? replaced
                : ((hero.ComboStep % count) + count) % count;

            var note = comboNoteByHero.GetValue(hero, _ => new StrongBox<int>(-1));
            // A retriggered opener is an encore inside the chain, not a fresh chain.
            bool chainStart = slot == 0 && !ActionExecutionFlow.IsResolvingRetrigger;
            note.Value = chainStart || note.Value < 0 ? 0 : Math.Min(note.Value + 1, 6);

            return slot == count - 1
                ? AudioCueExtensions.ComboFinishCueForNote(note.Value)
                : AudioCueExtensions.ComboStepCueForSlot(note.Value);
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
            AudioCues.QueueForPunchline(isCriticalMiss ? AudioCue.Combat_CriticalMiss : AudioCue.Combat_Miss);
            return missEvent;
        }

        /// <summary>
        /// Publishes enemy death event and processes outcomes
        /// </summary>
        public static CombatEvent PublishEnemyDeath(Actor source, Actor target, Action action, int damage, int rollValue = 0)
        {
            var deathEvent = new CombatEvent(CombatEventType.EnemyDied, source)
            {
                Target = target,
                Action = action,
                Damage = damage,
                RollValue = rollValue
            };
            CombatEventBus.Instance.Publish(deathEvent);
            Combat.Outcomes.OutcomeHandlerRegistry.Instance.ProcessOutcomes(action, deathEvent, source, target);
            return deathEvent;
        }

        /// <summary>
        /// Publishes enemy health threshold event and processes outcomes
        /// </summary>
        public static CombatEvent PublishHealthThreshold(Actor source, Actor target, Action action, double healthPercentage, int rollValue = 0)
        {
            var thresholdEvent = new CombatEvent(CombatEventType.EnemyHealthThreshold, source)
            {
                Target = target,
                Action = action,
                HealthPercentage = healthPercentage,
                RollValue = rollValue
            };
            CombatEventBus.Instance.Publish(thresholdEvent);
            Combat.Outcomes.OutcomeHandlerRegistry.Instance.ProcessOutcomes(action, thresholdEvent, source, target);
            return thresholdEvent;
        }

        /// <summary>
        /// Publishes combo-chain ended (broken or completed wrap to opener).
        /// </summary>
        public static CombatEvent PublishComboEnded(Actor source, Actor? target, Action action, int rollValue, bool isCombo, bool isCritical)
        {
            var comboEnded = new CombatEvent(CombatEventType.ComboEnded, source)
            {
                Target = target,
                Action = action,
                RollValue = rollValue,
                IsCombo = isCombo,
                IsCritical = isCritical
            };
            CombatEventBus.Instance.Publish(comboEnded);
            Combat.Outcomes.OutcomeHandlerRegistry.Instance.ProcessOutcomes(action, comboEnded, source, target);
            return comboEnded;
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

