using System;

namespace RPGGame.Tests
{
    /// <summary>
    /// Runs a block against a <see cref="GameConfiguration"/> built from code defaults (no balance patch), then puts the
    /// live config back. For suites that check rules with fixed numbers (armor = gear + level, attribute scale 1.0, slot
    /// caps, pack sizes, ...) so the active patch's tuning cannot change their expected values.
    /// <code>using var config = TestConfigScope.CodeDefaults();</code>
    /// </summary>
    public sealed class TestConfigScope : IDisposable
    {
        private readonly GameConfiguration? _previous;
        private bool _disposed;

        private TestConfigScope(GameConfiguration? previous) => _previous = previous;

        public static TestConfigScope CodeDefaults() => new(GameConfiguration.UseCodeDefaultsForTests());

        public void Dispose()
        {
            if (_disposed)
                return;
            _disposed = true;
            GameConfiguration.RestoreInstanceForTests(_previous);
        }
    }
}
