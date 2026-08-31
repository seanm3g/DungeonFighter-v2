using System;
using System.Collections.Generic;
using RPGGame;
using RPGGame.Actions;

namespace RPGGame.UI.Avalonia.Layout
{
    /// <summary>
    /// Fighter-only horizontal resolve row: Previous | Current | Next.
    /// Begin/End happen in the same combat tick; a short hold keeps Current visible long enough to paint.
    /// </summary>
    public static class FighterResolveActionStackState
    {
        public const int CurrentHoldMs = 900;

        private static ActionPanelInfo? _previous;
        private static ActionPanelInfo? _current;
        private static ActionPanelInfo? _next;
        private static bool _pendingEnd;
        private static DateTimeOffset _holdUntil = DateTimeOffset.MinValue;

        internal static Func<DateTimeOffset>? UtcNowProviderForTests;
        private static DateTimeOffset Now() => UtcNowProviderForTests?.Invoke() ?? DateTimeOffset.UtcNow;

        public static ActionPanelInfo? Previous
        {
            get { ExpireIfNeeded(); return _previous; }
        }

        public static ActionPanelInfo? Current
        {
            get { ExpireIfNeeded(); return _current; }
        }

        public static ActionPanelInfo? Next
        {
            get { ExpireIfNeeded(); return _next; }
        }

        public static void BeginResolve(Character fighter, Action selectedAction)
        {
            if (fighter == null || selectedAction == null || fighter is Enemy)
                return;

            var panels = CombatActionStripBuilder.BuildPanelData(fighter);
            var combo = ActionUtilities.GetComboActions(fighter);
            if (panels.Count == 0 || combo.Count == 0)
            {
                Clear();
                return;
            }

            int step = fighter.ComboStep % combo.Count;
            int currentIndex = step;
            for (int i = 0; i < combo.Count; i++)
            {
                if (ReferenceEquals(combo[i], selectedAction)
                    || string.Equals(combo[i]?.Name, selectedAction.Name, StringComparison.Ordinal))
                {
                    currentIndex = i;
                    break;
                }
            }

            if (currentIndex < 0 || currentIndex >= panels.Count)
                currentIndex = Math.Clamp(step, 0, panels.Count - 1);

            _current = panels[currentIndex];
            int nextIndex = (currentIndex + 1) % panels.Count;
            _next = panels.Count > 1 ? panels[nextIndex] : null;
            _pendingEnd = false;
            _holdUntil = DateTimeOffset.MinValue;
        }

        public static void EndResolve()
        {
            if (!_current.HasValue)
                return;
            _pendingEnd = true;
            _holdUntil = Now().AddMilliseconds(CurrentHoldMs);
        }

        public static void Clear()
        {
            _previous = null;
            _current = null;
            _next = null;
            _pendingEnd = false;
            _holdUntil = DateTimeOffset.MinValue;
        }

        internal static void ResetForTests()
        {
            Clear();
            UtcNowProviderForTests = null;
        }

        private static void ExpireIfNeeded()
        {
            if (!_pendingEnd || !_current.HasValue)
                return;
            if (Now() < _holdUntil)
                return;

            _previous = _current;
            _current = null;
            _pendingEnd = false;
            _holdUntil = DateTimeOffset.MinValue;
        }
    }
}
