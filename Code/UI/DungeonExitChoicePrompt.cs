using System.Collections.Generic;
using RPGGame.UI.Avalonia;

namespace RPGGame.UI
{
    /// <summary>
    /// Room cleared, stay or leave: while <see cref="IsOpen"/> the between-fights view draws CONTINUE / LEAVE buttons at
    /// the bottom of the column (they send 1 / 2, the same keys as before).
    /// </summary>
    public static class DungeonExitChoicePrompt
    {
        public const string ContinueKey = "1";
        public const string LeaveKey = "2";
        /// <summary>Marks this prompt's click targets so a repaint replaces them instead of stacking more.</summary>
        public const string ButtonTag = "exitchoice:";

        private static readonly object Gate = new();
        private static bool _isOpen;

        public static bool IsOpen
        {
            get { lock (Gate) return _isOpen; }
        }

        /// <summary>The window's click-target list (set by the canvas UI); null when headless.</summary>
        public static List<ClickableElement>? ClickTargets { get; set; }

        public static void Open()
        {
            lock (Gate) _isOpen = true;
        }

        public static void Close()
        {
            lock (Gate) _isOpen = false;
            RemoveTargets();
        }

        public static void RemoveTargets()
        {
            var list = ClickTargets;
            list?.RemoveAll(e => e.DisplayText != null && e.DisplayText.StartsWith(ButtonTag, System.StringComparison.Ordinal));
        }
    }
}
