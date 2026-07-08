using System;
using System.Threading;
using RPGGame.Combat.UI;

namespace RPGGame.Combat
{
    /// <summary>
    /// Scoped combat UI mute so lab/sim/balance runs do not clobber a live fight's mute flag.
    /// Prefer <see cref="Begin"/> over toggling <see cref="CombatManager.DisableCombatUIOutput"/> when nesting is possible.
    /// </summary>
    public static class CombatUiMuteScope
    {
        private static readonly AsyncLocal<bool?> AsyncMuted = new();
        private static bool GlobalMuted;

        /// <summary>
        /// True when combat UI should be suppressed. AsyncLocal override wins over the process-global flag.
        /// </summary>
        public static bool IsMuted => AsyncMuted.Value ?? GlobalMuted;

        /// <summary>
        /// Process-global mute used by legacy get/set on <see cref="CombatManager.DisableCombatUIOutput"/>.
        /// </summary>
        public static bool GlobalMute
        {
            get => GlobalMuted;
            set => GlobalMuted = value;
        }

        /// <summary>
        /// Begin a mute override for the current async flow. Dispose to restore the previous override.
        /// </summary>
        public static IDisposable Begin(bool muted = true)
        {
            var previous = AsyncMuted.Value;
            bool enteringMute = muted && !IsMuted;
            AsyncMuted.Value = muted;
            if (enteringMute)
                HealthBarDeltaDamageHint.ClearAll();
            return new Scope(previous);
        }

        private sealed class Scope : IDisposable
        {
            private readonly bool? _previous;
            private bool _disposed;

            public Scope(bool? previous) => _previous = previous;

            public void Dispose()
            {
                if (_disposed) return;
                _disposed = true;
                AsyncMuted.Value = _previous;
            }
        }
    }
}
