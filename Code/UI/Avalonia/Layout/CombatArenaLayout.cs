using System;

namespace RPGGame.UI.Avalonia.Layout
{
    /// <summary>
    /// Grid geometry for the arena combat layout (<see cref="CombatBoardLayout.IsArena"/>). Hero on the left half, enemy
    /// mirrored on the right. The two portraits meet in the middle of the top row with the turn arrow and the d20 between
    /// them; each side's health bar runs outward from its portrait. Status effects sit under each portrait, the combo strips
    /// on the outer sides under the bars, and the log in a panel centered under everything, over the room backdrop.
    /// </summary>
    public static class CombatArenaLayout
    {
        public const int PortraitW = 13;
        public const int PortraitH = 7;

        /// <summary>Columns between the two portraits: the "next" arrow over the die.</summary>
        public const int CenterGap = 14;

        /// <summary>Whole usable grid: every column inside the outer margin, every row.</summary>
        public static (int X, int Y, int W, int H) Screen() => LayoutConstants.GetChromelessContentRect();

        private static int Half => Screen().X + Screen().W / 2;

        /// <summary>Hero plate: name, bar and level line on the left, portrait at its right end (screen middle).</summary>
        public static (int X, int Y, int W, int H) HeroPlate()
        {
            var s = Screen();
            int right = Half - CenterGap / 2;
            return (s.X, 0, Math.Max(1, right - s.X), PortraitH);
        }

        /// <summary>Enemy plate: portrait at its left end (screen middle), name, bar and level line to the right.</summary>
        public static (int X, int Y, int W, int H) EnemyPlate()
        {
            var s = Screen();
            int left = Half + CenterGap / 2;
            return (left, 0, Math.Max(1, s.X + s.W - left), PortraitH);
        }

        public static int HeroPortraitX => HeroPlate().X + HeroPlate().W - PortraitW;

        public static int EnemyPortraitX => EnemyPlate().X;

        /// <summary>Gap between the portraits: turn arrow on top, the die under it.</summary>
        public static (int X, int Y, int W, int H) Center()
        {
            var hero = HeroPlate();
            var enemy = EnemyPlate();
            return (hero.X + hero.W, 0, enemy.X - (hero.X + hero.W), PortraitH);
        }

        public static int StripY => PortraitH + 1;

        public static int StripH => Math.Clamp(Screen().H / 5, 6, Math.Max(6, LayoutConstants.ACTION_INFO_STRIP_HEIGHT));

        /// <summary>Hero combo strip, under the hero's bar: the action-info strip moves here while the arena is up.</summary>
        public static (int X, int Y, int W, int H) HeroStrip()
        {
            var plate = HeroPlate();
            return (plate.X, StripY, Math.Max(1, plate.W - PortraitW - 1), StripH);
        }

        public static (int X, int Y, int W, int H) EnemyStrip()
        {
            var plate = EnemyPlate();
            return (plate.X + PortraitW + 1, StripY, Math.Max(1, plate.W - PortraitW - 1), StripH);
        }

        /// <summary>Status effect column under the hero portrait.</summary>
        public static (int X, int Y, int W, int H) HeroStatus() => (HeroPortraitX, StripY, PortraitW, StripH);

        /// <summary>Status effect column under the enemy portrait.</summary>
        public static (int X, int Y, int W, int H) EnemyStatus() => (EnemyPortraitX, StripY, PortraitW, StripH);

        /// <summary>Framed combat log, centered under the strips down to the help footer row.</summary>
        public static (int X, int Y, int W, int H) Log()
        {
            var s = Screen();
            int y = StripY + StripH + 1;
            int w = Math.Clamp(s.W * 11 / 20, Math.Min(40, s.W), s.W);
            int x = s.X + (s.W - w) / 2;
            return (x, y, w, Math.Max(4, s.Y + s.H - 1 - y));
        }
    }

    /// <summary>
    /// The arena's entrance at the start of a fight: the portraits land in the middle with VS between them, the plates
    /// open outward as the health bars fill from the portraits, then the strips, status and log come up, and the fight
    /// holds a beat before the first turn. <see cref="Begin"/> starts it; the encounter waits <see cref="TotalMs"/>.
    /// </summary>
    public static class CombatArenaIntro
    {
        /// <summary>Portraits in, plates and bars opening outward.</summary>
        public const int OpenMs = 700;

        /// <summary>Plates settled; strips, status and log appear at the end of this.</summary>
        public const int RevealMs = 250;

        /// <summary>Everything up and still before the first turn plays.</summary>
        public const int HoldMs = 600;

        public static int TotalMs => OpenMs + RevealMs + HoldMs;

        private static DateTimeOffset _startedUtc;

        public static void Begin() => _startedUtc = DateTimeOffset.UtcNow;

        private static double ElapsedMs => _startedUtc == default ? double.MaxValue : (DateTimeOffset.UtcNow - _startedUtc).TotalMilliseconds;

        /// <summary>True while anything still moves; the arena asks for frames until then.</summary>
        public static bool Running => ElapsedMs < OpenMs + RevealMs;

        /// <summary>0..1, eased: how far the plates have opened out from the portraits.</summary>
        public static double Open => CombatBoardAnimation.EaseOut(Math.Clamp(ElapsedMs / OpenMs, 0.0, 1.0));

        /// <summary>Strips, status effects and the log show once the plates have settled.</summary>
        public static bool ShowsBelowPlates => ElapsedMs >= OpenMs + RevealMs;
    }
}
