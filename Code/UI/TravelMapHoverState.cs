using System.Collections.Generic;

namespace RPGGame.UI
{
    /// <summary>
    /// Tracks which region marker on the travel map is under the pointer.
    /// The travel renderer registers hit rects each draw; the mouse handler reports pointer moves
    /// and redraws the screen when the hovered region changes.
    /// </summary>
    public static class TravelMapHoverState
    {
        private readonly record struct Target(int X, int Y, int Width, int Height, string RegionId);

        private static readonly object Lock = new();
        private static readonly List<Target> Targets = new();
        private static string? _hoveredRegionId;
        private static int _pointerX = -1;
        private static int _pointerY = -1;

        /// <summary>Region id under the pointer, or null.</summary>
        public static string? HoveredRegionId
        {
            get
            {
                lock (Lock)
                    return _hoveredRegionId;
            }
        }

        /// <summary>Clears registered markers before a redraw.</summary>
        public static void BeginTargets()
        {
            lock (Lock)
                Targets.Clear();
        }

        public static void AddTarget(int x, int y, int width, int height, string regionId)
        {
            lock (Lock)
            {
                Targets.Add(new Target(x, y, width, height, regionId));
                // Keep hover in sync with markers that moved under a still pointer.
                _hoveredRegionId = Resolve(_pointerX, _pointerY);
            }
        }

        /// <summary>Returns true when the hovered region changed.</summary>
        public static bool UpdateFromPointer(int gridX, int gridY)
        {
            lock (Lock)
            {
                _pointerX = gridX;
                _pointerY = gridY;
                string? next = Resolve(gridX, gridY);
                if (next == _hoveredRegionId)
                    return false;
                _hoveredRegionId = next;
                return true;
            }
        }

        public static void Clear()
        {
            lock (Lock)
            {
                Targets.Clear();
                _hoveredRegionId = null;
                _pointerX = -1;
                _pointerY = -1;
            }
        }

        private static string? Resolve(int x, int y)
        {
            foreach (var t in Targets)
            {
                if (x >= t.X && x < t.X + t.Width && y >= t.Y && y < t.Y + t.Height)
                    return t.RegionId;
            }
            return null;
        }
    }
}
