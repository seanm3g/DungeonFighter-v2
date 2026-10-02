using System;

namespace RPGGame.UI.Avalonia.Effects
{
    /// <summary>
    /// Pure math for wind-sway chromatic aberration fringe offsets (red/cyan ghost passes).
    /// </summary>
    public static class WindSwayChromatic
    {
        /// <summary>
        /// Offsets below this (pixels) are treated as idle (no fringe).
        /// Kept high enough that subpixel residuals cannot smear pixel fonts with CA ghosts.
        /// </summary>
        public const double IdleEpsilon = 0.45;

        /// <summary>
        /// Computes the red-ghost fringe delta from the glyph sway offset.
        /// Cyan uses the negated vector. Returns (0,0) when idle or spread is non-positive.
        /// Direction follows <paramref name="ox"/>/<paramref name="oy"/>; falls back to horizontal if near-zero.
        /// Fringe magnitude = offset magnitude × <paramref name="spreadFraction"/>.
        /// </summary>
        public static (double Dx, double Dy) ComputeFringe(double ox, double oy, double spreadFraction)
        {
            if (spreadFraction <= 0)
                return (0, 0);

            double mag = Math.Sqrt(ox * ox + oy * oy);
            if (mag < IdleEpsilon)
                return (0, 0);

            double dirX = ox / mag;
            double dirY = oy / mag;
            if (Math.Abs(dirX) < IdleEpsilon && Math.Abs(dirY) < IdleEpsilon)
            {
                dirX = 1.0;
                dirY = 0.0;
            }

            double fringe = mag * spreadFraction;
            return (dirX * fringe, dirY * fringe);
        }

        /// <summary>Clamps opacity to 0–1 and converts to a byte alpha.</summary>
        public static byte OpacityToAlpha(double opacity)
        {
            double clamped = Math.Clamp(opacity, 0.0, 1.0);
            return (byte)Math.Round(clamped * 255.0);
        }
    }
}
