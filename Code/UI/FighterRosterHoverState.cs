using System.Collections.Generic;
using RPGGame.UI.Avalonia;

namespace RPGGame.UI
{
    /// <summary>
    /// Pointer over the fighter roster (main menu and the Stable grid). Menu screens only redraw when something asks,
    /// so the mouse handler redraws when the hovered target changes, and the roster renderer reads the pointer here
    /// to draw hover highlights and the fighter detail panel.
    /// </summary>
    public static class FighterRosterHoverState
    {
        private static readonly object Lock = new();
        private static string _value = "";
        private static int _x = -1;
        private static int _y = -1;

        /// <summary>Value of the topmost element under the pointer, or empty.</summary>
        public static string Value
        {
            get { lock (Lock) return _value; }
        }

        public static bool IsOver(ClickableElement element)
        {
            lock (Lock) return _x >= 0 && element.Contains(_x, _y);
        }

        /// <summary>Records the pointer; returns true when the hovered target changed.</summary>
        public static bool Update(IReadOnlyList<ClickableElement> elements, int x, int y)
        {
            string found = "";
            for (int i = elements.Count - 1; i >= 0; i--)
            {
                if (elements[i].Contains(x, y))
                {
                    found = elements[i].Value ?? "";
                    break;
                }
            }
            lock (Lock)
            {
                _x = x;
                _y = y;
                if (found == _value)
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
                _x = -1;
                _y = -1;
            }
        }
    }
}
