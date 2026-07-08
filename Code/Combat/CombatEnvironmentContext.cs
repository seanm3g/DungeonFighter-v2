using System;
using System.Threading;

namespace RPGGame.Combat
{
    /// <summary>
    /// Tracks the active environment during combat for roll/threshold modifiers.
    /// Uses AsyncLocal so parallel lab/sim fights do not share one room pointer.
    /// </summary>
    public static class CombatEnvironmentContext
    {
        private static readonly AsyncLocal<Environment?> AsyncCurrentRoom = new();

        /// <summary>
        /// Room for the current async combat flow. Null outside an active fight scope.
        /// </summary>
        public static Environment? CurrentRoom
        {
            get => AsyncCurrentRoom.Value;
            set => AsyncCurrentRoom.Value = value;
        }

        /// <summary>
        /// Sets <see cref="CurrentRoom"/> for the current async flow and restores the previous value on dispose.
        /// </summary>
        public static IDisposable BeginScope(Environment? room)
        {
            var previous = AsyncCurrentRoom.Value;
            AsyncCurrentRoom.Value = room;
            return new Scope(previous);
        }

        private sealed class Scope : IDisposable
        {
            private readonly Environment? _previous;
            private bool _disposed;

            public Scope(Environment? previous) => _previous = previous;

            public void Dispose()
            {
                if (_disposed) return;
                _disposed = true;
                AsyncCurrentRoom.Value = _previous;
            }
        }
    }
}
