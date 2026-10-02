using System.Collections.Generic;
using RPGGame.UI.ColorSystem;

namespace RPGGame.UI
{
    /// <summary>
    /// Hit targets for F7 narrative combat-log prose lines that carry mechanical combat-log info tips.
    /// Rebuilt each time <see cref="Avalonia.Display.DisplayRenderer"/> paints the log.
    /// </summary>
    public static class CombatLogProseHoverMap
    {
        public readonly struct Entry
        {
            public Entry(int x, int y, int width, int height, List<List<ColoredText>> infoLines)
            {
                X = x;
                Y = y;
                Width = width;
                Height = height;
                InfoLines = infoLines;
            }

            public int X { get; }
            public int Y { get; }
            public int Width { get; }
            public int Height { get; }
            public List<List<ColoredText>> InfoLines { get; }

            public bool Contains(int gridX, int gridY) =>
                gridX >= X && gridX < X + Width && gridY >= Y && gridY < Y + Height;
        }

        private static readonly object Lock = new();
        private static List<Entry> _entries = new();

        public static void BeginFrame()
        {
            lock (Lock)
                _entries = new List<Entry>();
        }

        public static void Add(int x, int y, int width, int height, List<List<ColoredText>>? infoLines)
        {
            if (infoLines == null || infoLines.Count == 0 || width < 1 || height < 1)
                return;
            lock (Lock)
                _entries.Add(new Entry(x, y, width, height, infoLines));
        }

        /// <summary>
        /// Topmost (last-registered) entry under the pointer, if any.
        /// </summary>
        public static bool TryHit(
            int gridX,
            int gridY,
            out List<List<ColoredText>> infoLines,
            out int targetY,
            out int targetHeight)
        {
            return TryHit(gridX, gridY, out infoLines, out _, out targetY, out _, out targetHeight);
        }

        /// <summary>
        /// Topmost entry under the pointer, including the prose band X/width for tip framing.
        /// </summary>
        public static bool TryHit(
            int gridX,
            int gridY,
            out List<List<ColoredText>> infoLines,
            out int targetX,
            out int targetY,
            out int targetWidth,
            out int targetHeight)
        {
            lock (Lock)
            {
                for (int i = _entries.Count - 1; i >= 0; i--)
                {
                    var e = _entries[i];
                    if (!e.Contains(gridX, gridY))
                        continue;
                    infoLines = e.InfoLines;
                    targetX = e.X;
                    targetY = e.Y;
                    targetWidth = e.Width;
                    targetHeight = e.Height;
                    return true;
                }
            }

            infoLines = new List<List<ColoredText>>();
            targetX = 0;
            targetY = 0;
            targetWidth = 0;
            targetHeight = 0;
            return false;
        }

        public static void Clear()
        {
            lock (Lock)
                _entries = new List<Entry>();
        }

        internal static int EntryCountForTests
        {
            get
            {
                lock (Lock) return _entries.Count;
            }
        }
    }
}
