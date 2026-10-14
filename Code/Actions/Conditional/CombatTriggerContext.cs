using System;
using System.Collections.Concurrent;
using System.Linq;

namespace RPGGame.Actions.Conditional
{
    /// <summary>
    /// Per-fight memory for trigger predicates (first blood, after-miss, mirror/switch-up, last-enemy count),
    /// strip mutations, miss salvage, and pending roll rewrites.
    /// Reset when a battle narrative starts.
    /// </summary>
    public static class CombatTriggerContext
    {
        private static readonly ConcurrentDictionary<Actor, string> PreviousActionNames = new();
        private static readonly ConcurrentDictionary<Actor, bool> HasConnected = new();
        private static readonly ConcurrentDictionary<Actor, bool> LastSwingWasMiss = new();
        private static readonly ConcurrentDictionary<Actor, StripMutationState> StripStates = new();
        private static readonly ConcurrentDictionary<Actor, int> MissSalvageCharges = new();
        private static readonly ConcurrentDictionary<Actor, int> PendingReplaceRollFaces = new();
        private static readonly ConcurrentDictionary<Actor, int> CritFaceMinOverrides = new();

        /// <summary>Living enemies in the room when the current fight began (1 = last stand).</summary>
        public static int LivingEnemyCountAtFightStart { get; private set; } = 1;

        public static void ResetForBattle()
        {
            PreviousActionNames.Clear();
            HasConnected.Clear();
            LastSwingWasMiss.Clear();
            foreach (var kv in StripStates)
                kv.Value.Clear();
            StripStates.Clear();
            MissSalvageCharges.Clear();
            PendingReplaceRollFaces.Clear();
            CritFaceMinOverrides.Clear();
            RetriggerScheduler.ResetForBattle();
            LivingEnemyCountAtFightStart = 1;
        }

        public static void SetLivingEnemyCount(int count) =>
            LivingEnemyCountAtFightStart = Math.Max(0, count);

        public static void SetLivingEnemyCountFromRoom(Environment? room)
        {
            if (room == null)
            {
                LivingEnemyCountAtFightStart = 1;
                return;
            }

            LivingEnemyCountAtFightStart = room.GetEnemies().Count(e => e.IsAlive);
        }

        public static bool SourceHasConnectedThisFight(Actor? source) =>
            source != null && HasConnected.TryGetValue(source, out bool connected) && connected;

        public static bool SourceLastSwingWasMiss(Actor? source) =>
            source != null && LastSwingWasMiss.TryGetValue(source, out bool miss) && miss;

        public static bool TryGetPreviousActionName(Actor? source, out string? name)
        {
            name = null;
            if (source == null)
                return false;
            if (!PreviousActionNames.TryGetValue(source, out var stored) || string.IsNullOrEmpty(stored))
                return false;
            name = stored;
            return true;
        }

        public static StripMutationState GetOrCreateStripState(Actor source) =>
            StripStates.GetOrAdd(source, _ => new StripMutationState());

        public static StripMutationState? GetStripState(Actor? source) =>
            source != null && StripStates.TryGetValue(source, out var s) ? s : null;

        public static void AddMissSalvageCharges(Actor source, int charges)
        {
            if (source == null || charges <= 0)
                return;
            MissSalvageCharges.AddOrUpdate(source, charges, (_, old) => old + charges);
        }

        public static bool TryConsumeMissSalvage(Actor source)
        {
            if (source == null)
                return false;
            if (!MissSalvageCharges.TryGetValue(source, out int charges) || charges <= 0)
                return false;
            if (charges == 1)
                MissSalvageCharges.TryRemove(source, out _);
            else
                MissSalvageCharges[source] = charges - 1;
            return true;
        }

        public static int GetMissSalvageCharges(Actor? source) =>
            source != null && MissSalvageCharges.TryGetValue(source, out int c) ? c : 0;

        /// <summary>Queues a forced natural die face for the actor's next attack roll.</summary>
        public static void SetPendingReplaceRollFace(Actor source, int face)
        {
            if (source == null)
                return;
            face = Math.Clamp(face, 1, 20);
            PendingReplaceRollFaces[source] = face;
        }

        public static bool TryConsumePendingReplaceRollFace(Actor source, out int face)
        {
            face = 0;
            if (source == null)
                return false;
            if (!PendingReplaceRollFaces.TryRemove(source, out face))
                return false;
            face = Math.Clamp(face, 1, 20);
            return true;
        }

        /// <summary>Fight-scoped minimum face that counts as crit (e.g. 19 ⇒ 19–20 crit). 0 = none.</summary>
        public static void SetCritFaceMin(Actor source, int minFace)
        {
            if (source == null)
                return;
            minFace = Math.Clamp(minFace, 1, 20);
            CritFaceMinOverrides[source] = minFace;
        }

        public static bool TryGetCritFaceMin(Actor? source, out int minFace)
        {
            minFace = 0;
            return source != null && CritFaceMinOverrides.TryGetValue(source, out minFace) && minFace > 0;
        }

        /// <summary>
        /// Call after status gates are evaluated for a swing (hit or miss).
        /// Updates first-blood / after-miss / previous-action memory for the next swing.
        /// </summary>
        public static void NotifySwingResolved(Actor source, Action? action, bool connected, bool missed)
        {
            if (source == null)
                return;

            if (connected)
                HasConnected[source] = true;

            LastSwingWasMiss[source] = missed && !connected;

            if (action != null && !string.IsNullOrWhiteSpace(action.Name))
            {
                PreviousActionNames[source] = action.Name;
                if (source is Character character)
                    character.Effects.LastAction = action;
            }
        }
    }
}
