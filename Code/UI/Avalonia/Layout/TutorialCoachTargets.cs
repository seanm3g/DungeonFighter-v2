using System.Collections.Generic;
using RPGGame;

namespace RPGGame.UI.Avalonia.Layout
{
    /// <summary>
    /// Grid rects of the combat screen parts a <see cref="TutorialCoachNote"/> can point at, recorded by the renderers
    /// on each paint (null when that part isn't on screen).
    /// </summary>
    public static class TutorialCoachTargets
    {
        private static readonly Dictionary<TutorialCoachFocus, (int X, int Y, int W, int H)> _rects = new();

        public static void Set(TutorialCoachFocus focus, (int X, int Y, int W, int H)? rect)
        {
            lock (_rects)
            {
                if (rect is { W: > 0, H: > 0 } r)
                    _rects[focus] = r;
                else
                    _rects.Remove(focus);
            }
        }

        public static (int X, int Y, int W, int H)? Get(TutorialCoachFocus focus)
        {
            lock (_rects)
                return _rects.TryGetValue(focus, out var r) ? r : null;
        }
    }
}
