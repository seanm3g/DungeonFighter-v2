using System;
using System.Collections.Generic;
using RPGGame.Combat;

namespace RPGGame
{
    /// <summary>One upcoming turn in <see cref="CombatTurnQueue.GetUpcoming"/>. <paramref name="IsRoomTrigger"/> marks the
    /// room's bonus action set off by a swing landing its trigger face (off the speed schedule, no roll of its own).
    /// <paramref name="IsFreeAction"/> marks the acting entity's free combo action, earned by the swing that just landed.</summary>
    public readonly record struct CombatTurnQueueEntry(Actor Actor, bool IsActing, bool IsRoomTrigger = false, bool IsFreeAction = false);

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
        private static bool _actingQuiet;
        private static bool _actingBonus;
        private static bool _actingFree;
        private static int _freeBaseStep = -1, _freeActionsTaken;
        private static long _turnSerial;
        private static Environment? _room;

        /// <summary>Entity whose turn the combat loop is processing, or null between turns.</summary>
        public static Actor? ActingEntity { get { lock (Gate) return _acting; } }

        /// <summary>Action the acting entity resolved this turn when it has no swing sequence (environment hazards).</summary>
        public static Action? ActingAction { get { lock (Gate) return _actingAction; } }

        /// <summary>The acting environment rolled under its strike threshold this turn and does nothing.</summary>
        public static bool ActingQuiet { get { lock (Gate) return _actingQuiet; } }

        /// <summary>The acting turn is the room's trigger bonus action (begun with <see cref="BeginBonusTurn"/>).</summary>
        public static bool ActingBonus { get { lock (Gate) return _actingBonus; } }

        /// <summary>Strip step the acting entity's free combo action is lined up on (its ComboStep when the action began),
        /// or -1 when no free action plays.</summary>
        public static int ActingFreeStep { get { lock (Gate) return _actingFree ? _freeBaseStep : -1; } }

        /// <summary>Bumped by every <see cref="BeginTurn"/>, so the board can tell one turn from the next.</summary>
        public static long TurnSerial { get { lock (Gate) return _turnSerial; } }

        public static bool IsBound { get { lock (Gate) return _speed != null; } }

        /// <summary>Room of the on-screen fight (hostile or not), for HUD reads such as its trigger face.</summary>
        public static Environment? Room { get { lock (Gate) return _speed != null ? _room : null; } }

        public static void Bind(ActionSpeedSystem speed, Environment? room = null)
        {
            if (CombatUiMuteScope.IsMuted)
                return;
            CombatTurnAnnouncement.Reset();
            lock (Gate)
            {
                _speed = speed;
                _room = room;
                _acting = null;
                _actingAction = null;
                _actingQuiet = false;
                _actingBonus = false;
                ResetFreeActions();
            }
        }

        public static void Unbind()
        {
            lock (Gate)
            {
                _speed = null;
                _room = null;
                _acting = null;
                _actingAction = null;
                _actingQuiet = false;
                _actingBonus = false;
                ResetFreeActions();
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
                _actingQuiet = false;
                _actingBonus = false;
                ResetFreeActions();
                _turnSerial++;
            }
        }

        /// <summary>
        /// The acting <paramref name="actor"/>'s turn can chain free combo actions (the turn handler allows them): a swing
        /// that moves its strip forward from where it stands now earns one, which <see cref="GetUpcoming"/> then lists.
        /// </summary>
        public static void ArmFreeActions(Character actor)
        {
            lock (Gate)
            {
                if (_speed != null && ReferenceEquals(_acting, actor))
                    _freeBaseStep = actor.ComboStep;
            }
        }

        /// <summary>Starts a free combo action of the acting <paramref name="actor"/> (same turn, no speed time spent).</summary>
        public static void BeginFreeAction(Character actor)
        {
            lock (Gate)
            {
                if (_speed == null || !ReferenceEquals(_acting, actor))
                    return;
                _actingFree = true;
                _freeActionsTaken++;
                _freeBaseStep = actor.ComboStep;
            }
        }

        private static void ResetFreeActions()
        {
            _actingFree = false;
            _freeBaseStep = -1;
            _freeActionsTaken = 0;
        }

        /// <summary>
        /// The acting entity's last swing earned a free combo action: it moved the strip forward without wrapping and the
        /// chain hasn't run the strip's length (mirrors the turn handler's free-action loop).
        /// </summary>
        private static bool FreeActionEarned(Actor? acting, int baseStep, int taken)
        {
            if (acting is not Character ch || baseStep < 0 || !ch.IsAlive || ch.StunTurnsRemaining > 0)
                return false;
            int length = ch.GetComboActions().Count;
            return length > 1 && taken < length - 1 && ch.ComboStep > baseStep && ch.ComboStep % length != 0;
        }

        /// <summary>
        /// Starts the room's trigger bonus action: it acts now without spending its slot on the speed schedule, so
        /// <see cref="GetUpcoming"/> leaves the room's regular turns where they are.
        /// </summary>
        public static void BeginBonusTurn(Environment room)
        {
            BeginTurn(room);
            lock (Gate)
            {
                if (_speed != null)
                    _actingBonus = true;
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

        public static void SetActingQuiet()
        {
            lock (Gate)
            {
                if (_speed != null)
                    _actingQuiet = true;
            }
        }

        public static void EndTurn()
        {
            lock (Gate)
            {
                _acting = null;
                _actingAction = null;
                _actingQuiet = false;
                _actingBonus = false;
                ResetFreeActions();
            }
        }

        /// <summary>
        /// Next <paramref name="count"/> turns in order. When <paramref name="pastActingTurn"/> is set, the acting entity's
        /// current turn is treated as spent (its readiness pushed out by <paramref name="actingLength"/>), so the list
        /// starts with whoever acts after it. Later turns assume a 1.0-length swing unless <paramref name="lengthOf"/> gives
        /// one per (actor, how many of that actor's turns come before it in the list); environment turns may still be
        /// skipped on their roll. A swing that rolled the room's trigger face queues the room's bonus action right after
        /// that swing (ahead of the schedule), and the bonus action, while it plays, doesn't spend the room's slot.
        /// With <paramref name="showFreeAction"/>, a spent swing that earned a free combo action lists that action first
        /// (pass it only once the swing's result has shown). A free action spends no time: the actor's slot already moved
        /// on with the turn's first swing.
        /// </summary>
        public static List<CombatTurnQueueEntry> GetUpcoming(int count, bool pastActingTurn, double actingLength = 1.0,
            Func<Actor, int, double>? lengthOf = null, bool showFreeAction = false)
        {
            ActionSpeedSystem? speed;
            Actor? acting;
            Environment? room;
            bool actingBonus, actingFree;
            int freeBase, freeTaken;
            lock (Gate)
            {
                speed = _speed;
                acting = _acting;
                room = _room;
                actingBonus = _actingBonus;
                actingFree = _actingFree;
                freeBase = _freeBaseStep;
                freeTaken = _freeActionsTaken;
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
            if (actingBonus && acting != null)
            {
                if (!pastActingTurn)
                    result.Add(new CombatTurnQueueEntry(acting, true, IsRoomTrigger: true));
                actingPending = false;
            }
            else if (acting != null && actingFree && !pastActingTurn)
            {
                result.Add(new CombatTurnQueueEntry(acting, true));
                actingPending = false;
            }
            else if (pastActingTurn && acting != null)
            {
                if (!actingFree)
                {
                    foreach (var e in schedule)
                    {
                        if (ReferenceEquals(e.Entity, acting))
                            e.NextActionTime += EstimateDuration(e, actingLength);
                    }
                }
                if (showFreeAction && FreeActionEarned(acting, freeBase, freeTaken))
                    result.Add(new CombatTurnQueueEntry(acting, false, IsFreeAction: true));
                actingPending = false;
            }

            var seen = new Dictionary<Actor, int>(ReferenceEqualityComparer.Instance);
            for (int n = 0; result.Count < count; n++)
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
                seen.TryGetValue(best.Entity, out int ordinal);
                seen[best.Entity] = ordinal + 1;
                best.NextActionTime += EstimateDuration(best, lengthOf?.Invoke(best.Entity, ordinal) ?? 1.0);
            }

            // A pending trigger goes off once the turn that rolled it ends: behind that swing (and any free action it
            // earned) while it still plays.
            if (!actingBonus && room != null && room.HasPendingRollTrigger && count > 0)
            {
                int at = result.Count > 0 && (result[0].IsActing || result[0].IsFreeAction) ? 1 : 0;
                result.Insert(Math.Min(at, result.Count), new CombatTurnQueueEntry(room, false, IsRoomTrigger: true));
                if (result.Count > count)
                    result.RemoveAt(result.Count - 1);
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
