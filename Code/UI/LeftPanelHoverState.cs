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

        /// <summary>
        /// Picks the topmost hovered element whose value starts with <see cref="Prefix"/>.
        /// </summary>
        public static bool UpdateFromClickables(IReadOnlyList<ClickableElement> elements)
        {
            string found = "";
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
                break;
            }

            lock (Lock)
            {
                if (_value == found)
                    return false;
                _value = found;
                return true;
            }
        }

        public static void Clear()
        {
            lock (Lock)
            {
                _value = "";
            }
        }
    }
}
