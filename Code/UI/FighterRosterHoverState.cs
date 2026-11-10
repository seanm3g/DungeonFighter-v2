using System.Collections.Generic;
using RPGGame.UI.Avalonia;

namespace RPGGame.UI
{
    /// <summary>
    /// Pointer over the fighter roster (main menu and the Stable grid). Menu screens only redraw when something asks,
    /// so the mouse handler redraws when the hovered target changes, and the roster renderer reads the pointer here
    /// to draw hover highlights and the fighter detail panel.
    /// While the pointer is over the detail panel, <see cref="Value"/> keeps the card the panel belongs to, so the panel
    /// stays put and its buttons can be reached.
    /// </summary>
    public static class FighterRosterHoverState
    {
        private static readonly object Lock = new();
        private static string _value = "";
        private static string _inner = "";
        private static int _x = -1;
        private static int _y = -1;
        private static bool _hasPanel;
        private static int _panelX, _panelY, _panelW, _panelH;

        /// <summary>Card (or button) under the pointer, or empty; the panel's card while over the panel.</summary>
        public static string Value
        {
            get { lock (Lock) return _value; }
        }

        public static bool IsOver(ClickableElement element)
        {
            lock (Lock) return _x >= 0 && element.Contains(_x, _y);
        }

        /// <summary>Detail panel bounds from the last draw; null clears them.</summary>
        public static void SetPanel(int x, int y, int width, int height)
        {
            lock (Lock)
            {
                _hasPanel = true;
                _panelX = x;
                _panelY = y;
                _panelW = width;
                _panelH = height;
            }
        }

        public static void ClearPanel()
        {
            lock (Lock) _hasPanel = false;
        }

        /// <summary>Records the pointer; returns true when what is under it changed (needs a redraw).</summary>
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
                bool inPanel = _hasPanel && x >= _panelX && x < _panelX + _panelW && y >= _panelY && y < _panelY + _panelH;
                if (inPanel)
                {
                    if (found == _inner)
                        return false;
                    _inner = found;
                    return true;
                }
                bool innerChanged = _inner.Length > 0;
                _inner = "";
                if (found == _value)
                    return innerChanged;
                _value = found;
                return true;
            }
        }

        public static void Clear()
        {
            lock (Lock)
            {
                _value = "";
                _inner = "";
                _x = -1;
                _y = -1;
                _hasPanel = false;
            }
        }
    }
}
