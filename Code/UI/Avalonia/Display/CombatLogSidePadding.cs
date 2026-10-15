using System;

namespace RPGGame.UI.Avalonia.Display
{
    /// <summary>
    /// Cached <see cref="UIConfiguration.CombatLogSidePaddingPercent"/> for the center combat log.
    /// Read once from UIConfiguration.json (rendering runs every frame); the settings slider updates it live.
    /// Padding is a percent of the log width, so it scales with monitor/window size.
    /// </summary>
    public static class CombatLogSidePadding
    {
        public const double MaxPercent = 45;

        /// <summary>Columns of text the log keeps even when the padding would squeeze it further.</summary>
        public const int MinTextWidth = 30;

        private static double? cachedPercent;

        /// <summary>Configured padding per side, as a percent of the log width.</summary>
        public static double ConfiguredPercent
        {
            get
            {
                if (cachedPercent == null)
                {
                    try
                    {
                        cachedPercent = Math.Clamp(UIConfiguration.LoadFromFile().CombatLogSidePaddingPercent, 0, MaxPercent);
                    }
                    catch
                    {
                        cachedPercent = 0;
                    }
                }
                return cachedPercent.Value;
            }
            set => cachedPercent = Math.Clamp(value, 0, MaxPercent);
        }

        /// <summary>
        /// Padding in columns per side for a log with <paramref name="innerWidth"/> columns, reduced so at least
        /// <see cref="MinTextWidth"/> columns remain for text.
        /// </summary>
        public static int Effective(int innerWidth)
        {
            int padding = (int)Math.Round(innerWidth * ConfiguredPercent / 100.0);
            int roomForPadding = Math.Max(0, (innerWidth - MinTextWidth) / 2);
            return Math.Clamp(padding, 0, roomForPadding);
        }
    }
}
