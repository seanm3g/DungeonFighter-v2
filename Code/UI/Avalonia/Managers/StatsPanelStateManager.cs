using System;

namespace RPGGame.UI.Avalonia.Managers
{
    /// <summary>
    /// THRESHOLDS section display mode on the left/right combat panels.
    /// The colored d20 bar lives under the health bar; this mode only controls ladder numbers vs CHANCES %.
    /// </summary>
    public enum ThresholdsHudMode
    {
        Ladder,
        Chances
    }

    /// <summary>
    /// Manages HUD section collapse state (left HERO/STATS/GEAR and right panel sections)
    /// and stats area bounds for click/glow.
    /// </summary>
    public class StatsPanelStateManager
    {
        private bool heroCollapsed;
        private bool statsCollapsed;
        private bool gearCollapsed;
        private bool thresholdsCollapsed;
        private ThresholdsHudMode thresholdsHudMode = ThresholdsHudMode.Ladder;
        private DateTimeOffset? thresholdsChancesFlashUntilUtc;
        private int statsAreaX = -1;
        private int statsAreaY = -1;
        private int statsAreaWidth = -1;
        private int statsAreaHeight = -1;
        
        /// <summary>HERO section body hidden when true.</summary>
        public bool HeroCollapsed
        {
            get => heroCollapsed;
            set => heroCollapsed = value;
        }

        /// <summary>STATS section body hidden when true.</summary>
        public bool StatsCollapsed
        {
            get => statsCollapsed;
            set => statsCollapsed = value;
        }

        /// <summary>GEAR section body hidden when true.</summary>
        public bool GearCollapsed
        {
            get => gearCollapsed;
            set => gearCollapsed = value;
        }

        /// <summary>THRESHOLDS section body (left character panel) hidden when true.</summary>
        public bool ThresholdsCollapsed
        {
            get => thresholdsCollapsed;
            set => thresholdsCollapsed = value;
        }

        /// <summary>Current thresholds section display mode (ladder numbers or CHANCES %).</summary>
        public ThresholdsHudMode ThresholdsHudMode
        {
            get => thresholdsHudMode;
            set
            {
                thresholdsHudMode = value;
                if (thresholdsHudMode != ThresholdsHudMode.Chances)
                    thresholdsChancesFlashUntilUtc = null;
            }
        }

        /// <summary>True when the section shows exclusive d20 outcome chances instead of ladder numbers.</summary>
        public bool ThresholdsShowChances => thresholdsHudMode == ThresholdsHudMode.Chances;

        public void ToggleHeroCollapsed() => heroCollapsed = !heroCollapsed;
        public void ToggleStatsCollapsed() => statsCollapsed = !statsCollapsed;
        public void ToggleGearCollapsed() => gearCollapsed = !gearCollapsed;
        public void ToggleThresholdsCollapsed() => thresholdsCollapsed = !thresholdsCollapsed;

        public void ToggleThresholdsDisplayMode()
        {
            thresholdsHudMode = thresholdsHudMode == ThresholdsHudMode.Ladder
                ? ThresholdsHudMode.Chances
                : ThresholdsHudMode.Ladder;
            if (thresholdsHudMode != ThresholdsHudMode.Chances)
                thresholdsChancesFlashUntilUtc = null;
        }

        /// <summary>Brief highlight after switching the thresholds header into CHANCES (% view).</summary>
        public void BeginThresholdsChancesModeFlash(TimeSpan duration)
        {
            thresholdsChancesFlashUntilUtc = DateTimeOffset.UtcNow.Add(duration);
        }

        /// <summary>True while the post-toggle CHANCES flash should paint (gold tint on chance rows).</summary>
        public bool IsThresholdsChancesFlashActive() =>
            thresholdsChancesFlashUntilUtc.HasValue && DateTimeOffset.UtcNow < thresholdsChancesFlashUntilUtc.Value;

        /// <summary>
        /// Sets the stats area bounds for click detection
        /// </summary>
        public void SetStatsAreaBounds(int x, int y, int width, int height)
        {
            statsAreaX = x;
            statsAreaY = y;
            statsAreaWidth = width;
            statsAreaHeight = height;
        }
        
        /// <summary>
        /// Gets the stats area bounds
        /// </summary>
        public (int x, int y, int width, int height) GetStatsAreaBounds()
        {
            return (statsAreaX, statsAreaY, statsAreaWidth, statsAreaHeight);
        }
        
        /// <summary>
        /// Checks if the given coordinates are within the stats area
        /// </summary>
        public bool IsInStatsArea(int x, int y)
        {
            if (statsAreaX < 0 || statsAreaY < 0 || statsAreaWidth < 0 || statsAreaHeight < 0)
                return false;
                
            return x >= statsAreaX && x < statsAreaX + statsAreaWidth &&
                   y >= statsAreaY && y < statsAreaY + statsAreaHeight;
        }
        
        /// <summary>
        /// Resets the stats area bounds (call when stats area is not visible)
        /// </summary>
        public void ResetStatsAreaBounds()
        {
            statsAreaX = -1;
            statsAreaY = -1;
            statsAreaWidth = -1;
            statsAreaHeight = -1;
        }
    }
}
