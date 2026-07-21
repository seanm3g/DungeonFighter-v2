using System.Collections.Generic;
using RPGGame.Actions.Conditional;
using RPGGame.Combat.Events;

namespace RPGGame.Actions.Execution
{
    /// <summary>
    /// Fires <see cref="CombatEventType.RoomCleared"/> and applies status effects from hero actions
    /// gated by <c>ONROOMSCLEARED</c> (optional <c>ONROOMSCLEARED:N</c> for the Nth clear).
    /// </summary>
    public static class RoomClearedTriggerApplicator
    {
        /// <summary>
        /// Records a room clear on the hero, publishes the event, and applies matching action statuses.
        /// </summary>
        /// <returns>Status effect log lines produced by triggered actions.</returns>
        public static List<string> ApplyForHero(Character hero)
        {
            var messages = new List<string>();
            if (hero == null)
                return messages;

            hero.RecordRoomCleared();
            int cleared = hero.SessionStats.RoomsCleared;

            var roomClearedEvent = new CombatEvent(CombatEventType.RoomCleared, hero)
            {
                Target = hero,
                RoomsClearedCount = cleared,
                StatValue = cleared
            };
            CombatEventBus.Instance.Publish(roomClearedEvent);

            foreach (var action in EnumerateDistinctPoolActions(hero))
            {
                if (!ActionHasRoomsClearedTrigger(action))
                    continue;

                roomClearedEvent.Action = action;
                CombatEffectsSimplified.ApplyStatusEffects(action, hero, hero, messages, roomClearedEvent);
            }

            return messages;
        }

        private static bool ActionHasRoomsClearedTrigger(Action action)
        {
            if (action.Triggers?.Bundles != null)
            {
                foreach (var bundle in action.Triggers.Bundles)
                {
                    if (bundle == null || !bundle.IsEnabled)
                        continue;
                    if (ActionTriggerGate.NormalizeToken(bundle.When) == "ONROOMSCLEARED")
                        return true;
                }
            }

            var conditions = action.Triggers?.TriggerConditions;
            if (conditions == null || conditions.Count == 0)
                return action.Triggers?.RoomsClearedTriggerValue > 0;

            foreach (var raw in conditions)
            {
                if (string.IsNullOrWhiteSpace(raw))
                    continue;
                string upper = raw.Trim().ToUpperInvariant();
                int colon = upper.IndexOf(':');
                string token = colon >= 0 ? upper.Substring(0, colon) : upper;
                token = ActionTriggerGate.NormalizeToken(token);
                if (token == "ONROOMSCLEARED")
                    return true;
            }

            return action.Triggers?.RoomsClearedTriggerValue > 0;
        }

        private static IEnumerable<Action> EnumerateDistinctPoolActions(Character hero)
        {
            var seen = new HashSet<string>(System.StringComparer.OrdinalIgnoreCase);
            var all = hero.Actions?.GetAllActions(hero) ?? hero.GetActionPool();
            foreach (var action in all)
            {
                if (action == null || string.IsNullOrWhiteSpace(action.Name))
                    continue;
                if (!seen.Add(action.Name))
                    continue;
                yield return action;
            }
        }
    }
}
