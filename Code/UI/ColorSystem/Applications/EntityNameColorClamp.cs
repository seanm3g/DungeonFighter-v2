using System;
using Avalonia.Media;
using RPGGame.UI;

namespace RPGGame.UI.ColorSystem.Applications
{
    /// <summary>
    /// Applies the shared entity-name brightness floor so per-glyph names remain readable on black UI surfaces.
    /// </summary>
    public static class EntityNameColorClamp
    {
        public const double DefaultBrightnessFloor = 150.0;

        private static readonly object CacheLock = new object();
        private static DateTime lastLoadedUtc = DateTime.MinValue;
        private static double cachedBrightnessFloor = DefaultBrightnessFloor;

        public static Color Apply(Color color)
        {
            return ColorValidator.ClampAnimatedTextBrightness(color, GetBrightnessFloor(), 255);
        }

        private static double GetBrightnessFloor()
        {
            var now = DateTime.UtcNow;
            lock (CacheLock)
            {
                if ((now - lastLoadedUtc).TotalSeconds < 1)
                    return cachedBrightnessFloor;

                cachedBrightnessFloor = ResolveBrightnessFloor();
                lastLoadedUtc = now;
                return cachedBrightnessFloor;
            }
        }

        private static double ResolveBrightnessFloor()
        {
            try
            {
                double configured = UIConfiguration.LoadFromFile()
                    .DungeonSelectionAnimation
                    .AnimatedTextBrightnessMin;

                return configured > 0
                    ? Math.Clamp(configured, 0, 255)
                    : DefaultBrightnessFloor;
            }
            catch
            {
                return DefaultBrightnessFloor;
            }
        }
    }
}
