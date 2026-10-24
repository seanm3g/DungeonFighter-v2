using System;
using System.Collections.Generic;
using RPGGame.Combat;

namespace RPGGame
{
    /// <summary>One upcoming turn in <see cref="CombatTurnQueue.GetUpcoming"/>.</summary>
    public readonly record struct CombatTurnQueueEntry(Actor Actor, bool IsActing);

    /// <summary>
    /// Live fight's turn order for the combat board: who is acting now and who acts after, projected from the
    /// <see cref="ActionSpeedSystem"/> schedule. Bound only for unmuted (on-screen) fights, so sims never touch it.
    /// Turn handlers display a swing before they advance the actor's timeline, so while a turn plays the acting
    /// entity still sits at the front of the raw schedule; <see cref="GetUpcoming"/> can project it past that turn.
    /// </summary>
    public static class CombatTurnQueue
    {
        private static readonly object Gate = new();
        private static ActionSpeedSystem? _speed;
        private static Actor? _acting;
        private static Action? _actingAction;
        private static long _turnSerial;

        /// <summary>Entity whose turn the combat loop is processing, or null between turns.</summary>
        public static Actor? ActingEntity { get { lock (Gate) return _acting; } }

        /// <summary>Action the acting entity resolved this turn when it has no swing sequence (environment hazards).</summary>
        public static Action? ActingAction { get { lock (Gate) return _actingAction; } }

        /// <summary>Bumped by every <see cref="BeginTurn"/>, so the board can tell one turn from the next.</summary>
        public static long TurnSerial { get { lock (Gate) return _turnSerial; } }

        public static bool IsBound { get { lock (Gate) return _speed != null; } }

        public static void Bind(ActionSpeedSystem speed)
        {
            if (CombatUiMuteScope.IsMuted)
                return;
            lock (Gate)
            {
                _speed = speed;
                _acting = null;
                _actingAction = null;
            }
        }

        public static void Unbind()
        {
            lock (Gate)
            {
                _speed = null;
                _acting = null;
                _actingAction = null;
            }
        }

        public static void BeginTurn(Actor actor)
        {
            lock (Gate)
            {
                if (_speed == null)
                    return;
                _acting = actor;
                _actingAction = null;
                _turnSerial++;
            }
        }

        public static void SetActingAction(Action action)
        {
            lock (Gate)
            {
                if (_speed != null)
                    _actingAction = action;
            }
        }

        public static void EndTurn()
        {
            lock (Gate)
            {
                _acting = null;
                _actingAction = null;
            }
        }

        /// <summary>
        /// Next <paramref name="count"/> turns in order. When <paramref name="pastActingTurn"/> is set, the acting entity's
        /// current turn is treated as spent (its readiness pushed out by <paramref name="actingLength"/>), so the list
        /// starts with whoever acts after it. Later turns assume a 1.0-length swing; environment turns may still be skipped
        /// on their roll.
        /// </summary>
        public static List<CombatTurnQueueEntry> GetUpcoming(int count, bool pastActingTurn, double actingLength = 1.0)
        {
            ActionSpeedSystem? speed;
            Actor? acting;
            lock (Gate)
            {
                speed = _speed;
                acting = _acting;
            }

            var result = new List<CombatTurnQueueEntry>();
            if (speed == null || count <= 0)
                return result;

            List<CombatEntity> schedule;
            try
            {
                schedule = speed.GetScheduleSnapshot();
            }
            catch (InvalidOperationException)
            {
                return result; // roster changed mid-copy; the next paint retries
            }
            if (schedule.Count == 0)
                return result;

            bool actingPending = acting != null;
            if (pastActingTurn && acting != null)
            {
                foreach (var e in schedule)
                {
                    if (ReferenceEquals(e.Entity, acting))
                        e.NextActionTime += EstimateDuration(e, actingLength);
                }
                actingPending = false;
            }

            for (int n = 0; n < count; n++)
            {
                CombatEntity? best = null;
                foreach (var e in schedule)
                {
                    if (best == null || e.NextActionTime < best.NextActionTime
                        || (e.NextActionTime == best.NextActionTime && string.CompareOrdinal(e.Entity.Name, best.Entity.Name) < 0))
                        best = e;
                }
                if (best == null)
                    break;
                bool isActing = actingPending && n == 0 && ReferenceEquals(best.Entity, acting);
                result.Add(new CombatTurnQueueEntry(best.Entity, isActing));
                best.NextActionTime += EstimateDuration(best, 1.0);
            }
            return result;
        }

        private static double EstimateDuration(CombatEntity e, double length)
        {
            double perLength = e.Entity is Character c ? c.GetTotalAttackSpeed() : e.BaseSpeed;
            return Math.Max(0.01, perLength * Math.Max(0.1, length));
        }
    }
}
