using System;
using RPGGame.UI.TitleScreen;

namespace RPGGame.UI.Avalonia.Canvas
{
    /// <summary>
    /// Crossfades the picture behind the screen when the location changes: menu cave ↔ inn common room ↔ each dungeon
    /// room's plate (or nothing). Fed once per paint; the outgoing picture is kept and faded out over
    /// <see cref="DurationSeconds"/> while the new one fades in.
    /// </summary>
    internal sealed class LocationCrossfade
    {
        public const double DurationSeconds = 3.0;

        /// <summary>What a paint shows behind its text: a menu scene or a room plate (or neither).</summary>
        public readonly record struct Location(MenuScene Scene, CanvasBackdrop? Backdrop)
        {
            public bool IsEmpty => Scene == MenuScene.None && Backdrop == null;

            /// <summary>Same place: same scene and same plate (the backdrop object is rebuilt every paint).</summary>
            public bool SamePlace(Location other) =>
                Scene == other.Scene && ReferenceEquals(Backdrop?.Source, other.Backdrop?.Source);
        }

        private readonly System.Diagnostics.Stopwatch clock = System.Diagnostics.Stopwatch.StartNew();
        private Location shown;
        private Location outgoing;
        private double fadeStart = double.NegativeInfinity;
        private bool started;

        /// <summary>True while a fade is running (the canvas keeps repainting until it ends).</summary>
        public bool IsActive => Progress < 1;

        /// <summary>0 when the location just changed, 1 once the fade is done.</summary>
        public double Progress => Math.Clamp((clock.Elapsed.TotalSeconds - fadeStart) / DurationSeconds, 0, 1);

        /// <summary>The location being faded out, while <see cref="IsActive"/>.</summary>
        public Location Outgoing => IsActive ? outgoing : default;

        /// <summary>
        /// Records this paint's location and returns the opacities to draw the incoming and outgoing pictures at.
        /// Plate to plate draws the old plate solid with the new one fading in over it; anything else fades both.
        /// </summary>
        public (double Incoming, double Outgoing) Update(Location now)
        {
            if (!started)
            {
                // The first paint just shows what is there.
                started = true;
                shown = now;
            }
            else if (!now.SamePlace(shown))
            {
                // A change mid-fade restarts from whatever was last asked for.
                outgoing = shown;
                shown = now;
                fadeStart = clock.Elapsed.TotalSeconds;
            }

            if (!IsActive)
                return (1, 0);

            double p = Progress;
            bool plateOverPlate = outgoing.Backdrop != null && now.Backdrop != null;
            return (p, plateOverPlate ? 1 : 1 - p);
        }
    }
}
