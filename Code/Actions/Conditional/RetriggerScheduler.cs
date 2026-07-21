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

            Pending[source] = new PendingRetrigger { Kind = kind, Slot1Based = slot };
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
        {
            forcedAction = null;
            if (source == null || !Pending.TryRemove(source, out var pending) || pending == null)
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
                RetriggerKind.Next => (character.ComboStep + 1) % combo.Count,
                RetriggerKind.Slot => Math.Clamp(pending.Slot1Based - 1, 0, combo.Count - 1),
                _ => -1
            };
            if (idx < 0)
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
            return forcedAction != null;
        }
    }
}
