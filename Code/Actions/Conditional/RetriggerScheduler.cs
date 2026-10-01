using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using RPGGame;
using RPGGame.Data;

namespace RPGGame.Actions.Conditional
{
    /// <summary>
    /// Schedules a single nested strip re-resolve (distinct from Multihit damage ticks).
    /// Depth is enforced by <see cref="ActionExecutionFlow"/> (max 1).
    /// </summary>
    public static class RetriggerScheduler
    {
        public enum RetriggerKind
        {
            None,
            Next,
            Opener,
            Finisher,
            Slot
        }

        private static readonly ConcurrentDictionary<Actor, PendingRetrigger> Pending = new();

        private sealed class PendingRetrigger
        {
            public RetriggerKind Kind { get; set; }
            public int Slot1Based { get; set; }
            /// <summary>
            /// 0-based strip index captured when <see cref="RetriggerKind.Next"/> is scheduled.
            /// Scheduling happens before the outer swing advances <c>ComboStep</c>, so this is the
            /// immediate next slot — not <c>ComboStep + 1</c> after that advance (which wraps onto
            /// the action that just fired when the strip is short).
            /// </summary>
            public int ResolvedIndex { get; set; } = -1;
        }

        public static void ResetForBattle() => Pending.Clear();

        /// <summary>When false (nested retrigger swing), new schedules are ignored.</summary>
        public static bool AllowScheduling { get; set; } = true;

        public static bool IsRetriggerMechanic(string? mechanicId)
        {
            string id = ActionMechanicsRegistry.NormalizeMechanicId(mechanicId ?? "");
            return id.StartsWith("retrigger_", StringComparison.OrdinalIgnoreCase);
        }

        public static bool TrySchedule(
            string mechanicId,
            string? mechanicArg,
            string? bundleCount,
            Action action,
            Actor source,
            List<string> messages)
        {
            if (!AllowScheduling || source == null)
                return false;

            string id = ActionMechanicsRegistry.NormalizeMechanicId(mechanicId);
            RetriggerKind kind;
            int slot = 0;
            if (id.StartsWith("retrigger_slot", StringComparison.OrdinalIgnoreCase))
            {
                kind = RetriggerKind.Slot;
                if (!int.TryParse((mechanicArg ?? "").Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out slot)
                    || slot <= 0)
                {
                    int.TryParse((bundleCount ?? "").Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out slot);
                }
                if (slot <= 0)
                    return false;
            }
            else
            {
                kind = id switch
                {
                    "retrigger_next" => RetriggerKind.Next,
                    "retrigger_opener" => RetriggerKind.Opener,
                    "retrigger_finisher" => RetriggerKind.Finisher,
                    _ => RetriggerKind.None
                };
            }

            if (kind == RetriggerKind.None)
                return false;

            // Do not overwrite with a second schedule in the same swing; first wins.
            if (Pending.ContainsKey(source))
                return false;

            int resolved = kind == RetriggerKind.Next ? ResolveNextStripIndex(source, action) : -1;
            Pending[source] = new PendingRetrigger { Kind = kind, Slot1Based = slot, ResolvedIndex = resolved };
            string label = kind switch
            {
                RetriggerKind.Next => "next strip slot",
                RetriggerKind.Opener => "opener",
                RetriggerKind.Finisher => "finisher",
                RetriggerKind.Slot => $"slot {slot}",
                _ => "action"
            };
            messages.Add($"{source.Name} prepares a retrigger ({label}).");
            return true;
        }

        public static bool TryConsume(Actor source, out Action? forcedAction)
            => TryConsume(source, out forcedAction, out _, out _);

        public static bool TryConsume(Actor source, out Action? forcedAction, out RetriggerKind kind, out int slotIndex)
        {
            forcedAction = null;
            kind = RetriggerKind.None;
            slotIndex = -1;
            if (source == null || !Pending.TryGetValue(source, out var pending) || pending == null)
                return false;
            if (source is not Character character)
                return false;

            var combo = ActionUtilities.GetComboActions(character);
            if (combo.Count == 0)
                return false;

            int idx = pending.Kind switch
            {
                RetriggerKind.Opener => 0,
                RetriggerKind.Finisher => combo.Count - 1,
                // Snapshot from schedule time. Falling back to the live step (already advanced on a
                // combo hit) still names the next slot; ComboStep+1 would skip it or wrap onto self.
                RetriggerKind.Next => pending.ResolvedIndex >= 0
                    ? pending.ResolvedIndex % combo.Count
                    : ((character.ComboStep % combo.Count) + combo.Count) % combo.Count,
                RetriggerKind.Slot => Math.Clamp(pending.Slot1Based - 1, 0, combo.Count - 1),
                _ => -1
            };
            if (idx < 0 || idx >= combo.Count)
                return false;

            // Prefer opener/finisher tags when kind asks for them
            if (pending.Kind == RetriggerKind.Opener)
            {
                int tagged = combo.FindIndex(a => a.ComboRouting?.IsOpener == true);
                if (tagged >= 0) idx = tagged;
            }
            else if (pending.Kind == RetriggerKind.Finisher)
            {
                int tagged = combo.FindIndex(a => a.ComboRouting?.IsFinisher == true);
                if (tagged >= 0) idx = tagged;
            }

            forcedAction = combo[idx];
            if (forcedAction == null)
                return false;

            kind = pending.Kind;
            slotIndex = idx;
            // Only drop pending once we have a resolvable strip action.
            Pending.TryRemove(source, out _);
            return true;
        }

        /// <summary>
        /// Next strip slot relative to the action that just scheduled the retrigger.
        /// Prefers that action's place on the strip; otherwise the live combo step (not yet advanced).
        /// </summary>
        private static int ResolveNextStripIndex(Actor source, Action? action)
        {
            if (source is not Character character)
                return -1;
            var combo = ActionUtilities.GetComboActions(character);
            if (combo.Count == 0)
                return -1;

            int current = -1;
            if (action != null)
            {
                current = combo.FindIndex(a => ReferenceEquals(a, action));
                if (current < 0)
                    current = combo.FindIndex(a => string.Equals(a.Name, action.Name, StringComparison.Ordinal));
            }
            if (current < 0)
                current = ((character.ComboStep % combo.Count) + combo.Count) % combo.Count;
            return (current + 1) % combo.Count;
        }
    }
}
