using System;
using RPGGame.Actions.Execution;

namespace RPGGame.Combat.Analytics
{
    /// <summary>
    /// Passive damage tap for headless analysis: every HP loss that goes through
    /// <see cref="CharacterHealthManager.TakeDamageWithNotifications"/> is reported with the source it came from.
    /// Read-only: it never changes damage, dice or state, and costs nothing while <see cref="Report"/> is null (normal play).
    /// </summary>
    public static class DamageLedger
    {
        public const string Hit = "hit";
        public const string Dot = "dot";
        public const string Retrigger = "retrigger";

        /// <summary>(target, HP actually lost, source). Set by <see cref="Tuning.PlaythroughFightRecorder"/>.</summary>
        public static Action<Actor, int, string>? Report { get; set; }

        [ThreadStatic] private static string? _tag;

        /// <summary>Source of damage applied right now: an open <see cref="Tag"/> scope, else retrigger or a plain hit.</summary>
        public static string CurrentSource => _tag ?? (ActionExecutionFlow.IsResolvingRetrigger ? Retrigger : Hit);

        /// <summary>Marks damage applied inside the scope (DoT ticks) as <paramref name="source"/>.</summary>
        public static Scope Tag(string source) => new(source);

        public readonly struct Scope : IDisposable
        {
            private readonly string? _previous;

            public Scope(string source)
            {
                _previous = _tag;
                _tag = source;
            }

            public void Dispose() => _tag = _previous;
        }
    }
}
