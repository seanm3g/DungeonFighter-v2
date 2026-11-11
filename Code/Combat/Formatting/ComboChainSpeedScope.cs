using System;
using System.Threading;

namespace RPGGame.Combat.Formatting
{
    /// <summary>
    /// While a free combo swing resolves, speed readouts for its actor show the time that swing adds to the combo chain
    /// (<see cref="ActionSpeedSystem.PreviewChainedActionCost"/>) instead of the swing's own length. Opened by the turn
    /// handler around the free swing; read by <see cref="ActionSpeedCalculator"/>.
    /// </summary>
    public static class ComboChainSpeedScope
    {
        private sealed record Frame(ActionSpeedSystem Speed, Actor Actor, double Growth);

        private static readonly AsyncLocal<Frame?> Current = new();

        public static IDisposable Enter(ActionSpeedSystem speed, Actor actor, double growth)
        {
            var previous = Current.Value;
            Current.Value = new Frame(speed, actor, growth);
            return new Restore(previous);
        }

        /// <summary>The swing's displayed time: its chain increase inside a free-swing scope for <paramref name="actor"/>, else <paramref name="swing"/>.</summary>
        public static double Apply(Actor actor, double swing)
        {
            var frame = Current.Value;
            if (frame == null || !ReferenceEquals(frame.Actor, actor) || swing <= 0)
                return swing;
            return frame.Speed.PreviewChainedActionCost(actor, swing, frame.Growth);
        }

        private sealed class Restore : IDisposable
        {
            private readonly Frame? _previous;
            private bool _done;
            public Restore(Frame? previous) => _previous = previous;
            public void Dispose()
            {
                if (_done) return;
                _done = true;
                Current.Value = _previous;
            }
        }
    }
}
