using System.Collections.Generic;
using RPGGame.UI.Avalonia;

namespace RPGGame.UI
{
    /// <summary>
    /// Tracks which left-panel STATS or GEAR region is under the pointer (for center-panel tooltips).
    /// </summary>
    public static class LeftPanelHoverState
    {
        public const string Prefix = "lphover:";

        private static readonly object Lock = new object();
        private static string _value = "";
        private static int _targetX;
        private static int _targetY;
        private static int _targetWidth;
        private static int _targetHeight;
        private static bool _hasTargetBounds;

        /// <summary>Full clickable <see cref="ClickableElement.Value"/> (e.g. <c>lphover:stat:damage</c>), or empty if none.</summary>
        public static string Value
        {
            get
            {
                lock (Lock) return _value;
            }
        }

        public static bool IsActive
        {
            get
            {
                lock (Lock) return !string.IsNullOrEmpty(_value);
            }
        }

        public static bool TryGetTargetBounds(out int x, out int y, out int width, out int height)
        {
            lock (Lock)
            {
                x = _targetX;
                y = _targetY;
                width = _targetWidth;
                height = _targetHeight;
                return !string.IsNullOrEmpty(_value) && _hasTargetBounds;
            }
        }

        /// <summary>
        /// Picks the topmost hovered element whose value starts with <see cref="Prefix"/>.
        /// </summary>
        public static bool UpdateFromClickables(IReadOnlyList<ClickableElement> elements)
        {
            string found = "";
            int foundX = 0;
            int foundY = 0;
            int foundWidth = 0;
            int foundHeight = 0;
            bool hasBounds = false;
            for (int i = elements.Count - 1; i >= 0; i--)
            {
                var el = elements[i];
                if (!el.IsHovered)
                    continue;
                string hoverKey = el.TooltipHoverValue ?? el.Value;
                if (string.IsNullOrEmpty(hoverKey))
                    continue;
                if (!hoverKey.StartsWith(Prefix, System.StringComparison.Ordinal))
                    continue;
                found = hoverKey;
                foundX = el.X;
                foundY = el.Y;
                foundWidth = el.Width;
                foundHeight = el.Height;
                hasBounds = true;
                break;
            }

            lock (Lock)
            {
                if (_value == found &&
                    _hasTargetBounds == hasBounds &&
                    _targetX == foundX &&
                    _targetY == foundY &&
                    _targetWidth == foundWidth &&
                    _targetHeight == foundHeight)
                    return false;
                _value = found;
                _targetX = foundX;
                _targetY = foundY;
                _targetWidth = foundWidth;
                _targetHeight = foundHeight;
                _hasTargetBounds = hasBounds;
                return true;
            }
        }

        public static void Clear()
        {
            lock (Lock)
            {
                _value = "";
                _targetX = 0;
                _targetY = 0;
                _targetWidth = 0;
                _targetHeight = 0;
                _hasTargetBounds = false;
            }
        }
    }
}
