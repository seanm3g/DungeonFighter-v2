using System;
using System.Collections.Generic;
using System.Linq;

namespace RPGGame.Combat.Rules
{
    /// <summary>One resolved rule, for the trigger trace (D6.16).</summary>
    public readonly record struct RuleFire(string Owner, RuleOwnerKind Kind, string Rule, string Event, int Generation);

    /// <summary>
    /// The resolution order for triggered rules (Documentation/CORE_DESIGN_PASS_2026-11-17.md D6.11–D6.16):
    /// <list type="bullet">
    /// <item>D6.12: within one event, gains (MINT / ADD / APPLY) resolve before everything else; ties go by owner order
    /// (<see cref="RuleBook"/>), with the item that granted the swing's card first, then by written order.</item>
    /// <item>D6.13: an event raised while another is resolving (a spend, a store filling up) waits in a first-in,
    /// first-out queue and resolves after every rule on the current event; the queue drains before the swing moves on.</item>
    /// <item>D6.14: generation 0 is the beat step's own event; rule-raised events may reach generation
    /// <see cref="MaxGeneration"/>; deeper ones are dropped with a log line.</item>
    /// <item>D6.16: every fire is recorded with its owner and generation (<see cref="Trace"/>).</item>
    /// </list>
    /// Hero-side only for now; enemy and room owners (D6.15) arrive with their migration.
    /// </summary>
    public static class RuleEngine
    {
        /// <summary>Deepest generation of rule-raised events that still resolves (D6.14).</summary>
        public const int MaxGeneration = 2;

        /// <summary>How many recent fires <see cref="Trace"/> keeps.</summary>
        public const int TraceSize = 64;

        private sealed class Pending
        {
            public required Character Hero;
            public required string Name;
            public required System.Action Resolve;
            public int Generation;
        }

        [ThreadStatic] private static int _scopeDepth;
        [ThreadStatic] private static int _generation;
        [ThreadStatic] private static Queue<Pending>? _queue;
        [ThreadStatic] private static List<RuleFire>? _trace;

        /// <summary>Events dropped past <see cref="MaxGeneration"/> since start (tests and sims read it).</summary>
        public static long ChainsStopped { get; private set; }

        /// <summary>Raised for each dropped event: hero, event name.</summary>
        public static event Action<Character, string>? ChainStopped;

        /// <summary>Raised for each resolved rule (narrative, sims).</summary>
        public static event Action<Character, RuleFire>? Fired;

        /// <summary>The most recent fires on this thread, oldest first.</summary>
        public static IReadOnlyList<RuleFire> Trace => _trace ?? (IReadOnlyList<RuleFire>)Array.Empty<RuleFire>();

        /// <summary>Generation of the event resolving now (0 outside any).</summary>
        public static int CurrentGeneration => _generation;

        /// <summary>True while an event is resolving: newly raised events queue instead of resolving at once.</summary>
        public static bool Resolving => _scopeDepth > 0;

        public static void ClearTrace() => _trace?.Clear();

        /// <summary>
        /// Resolves <paramref name="resolve"/> as an event: at once when nothing is resolving (generation 0, then the queue
        /// drains), else queued one generation deeper (dropped past <see cref="MaxGeneration"/>).
        /// </summary>
        public static void Raise(Character hero, string name, System.Action resolve)
        {
            if (_scopeDepth > 0)
            {
                int generation = _generation + 1;
                if (generation > MaxGeneration)
                {
                    ChainsStopped++;
                    RPGGame.DebugLogger.WriteCombatDebug("Rules", $"{name}: the trigger chain stops here (generation {generation})");
                    ChainStopped?.Invoke(hero, name);
                    return;
                }
                (_queue ??= new Queue<Pending>()).Enqueue(new Pending { Hero = hero, Name = name, Resolve = resolve, Generation = generation });
                return;
            }
            using (Scope())
                resolve();
        }

        /// <summary>
        /// Opens a resolution scope for a beat step's event (generation 0): events raised inside it queue, and they drain
        /// when the outermost scope closes.
        /// </summary>
        public static IDisposable Scope() => new ScopeHandle();

        private sealed class ScopeHandle : IDisposable
        {
            private bool _done;

            public ScopeHandle()
            {
                if (_scopeDepth == 0)
                    _generation = 0;
                _scopeDepth++;
            }

            public void Dispose()
            {
                if (_done)
                    return;
                _done = true;
                if (_scopeDepth == 1)
                    Drain();
                _scopeDepth--;
                if (_scopeDepth == 0)
                    _generation = 0;
            }
        }

        private static void Drain()
        {
            while (_queue != null && _queue.Count > 0)
            {
                var next = _queue.Dequeue();
                int saved = _generation;
                _generation = next.Generation;
                try
                {
                    next.Resolve();
                }
                finally
                {
                    _generation = saved;
                }
            }
        }

        /// <summary>
        /// The triggered entries in resolution order: phase (gains first), then the item that granted
        /// <paramref name="swing"/>'s card, then owner order and written order.
        /// </summary>
        public static IEnumerable<RuleEntry> InOrder(IEnumerable<RuleEntry> entries, Action? swing = null, Character? hero = null)
        {
            Item? granting = swing != null && hero != null ? GrantingItem(hero, swing) : null;
            return entries
                .OrderBy(e => e.Phase)
                .ThenBy(e => granting != null && ReferenceEquals(e.Owner.Item, granting) ? 0 : 1)
                .ThenBy(e => e.Owner.Order)
                .ThenBy(e => e.Index);
        }

        /// <summary>The worn piece whose gear action is <paramref name="swing"/>, if any.</summary>
        public static Item? GrantingItem(Character hero, Action swing)
        {
            if (string.IsNullOrWhiteSpace(swing.Name))
                return null;
            foreach (var (item, _) in RuleBook.SlotsInOrder(hero))
                if (item != null && string.Equals(item.GearAction?.Trim(), swing.Name.Trim(), StringComparison.OrdinalIgnoreCase))
                    return item;
            return null;
        }

        /// <summary>Records a resolved rule (D6.16).</summary>
        public static void Record(Character hero, RuleEntry entry, string eventName)
        {
            var fire = new RuleFire(entry.Owner.Name, entry.Owner.Kind, entry.Rule.Text, eventName, _generation);
            var trace = _trace ??= new List<RuleFire>(TraceSize);
            if (trace.Count >= TraceSize)
                trace.RemoveAt(0);
            trace.Add(fire);
            Fired?.Invoke(hero, fire);
        }
    }
}
