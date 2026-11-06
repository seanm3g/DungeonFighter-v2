using System.Collections.Generic;

namespace RPGGame.UI
{
    /// <summary>
    /// Action Sequence editor layout toggle (F1): the sequence row sits above the action library by default, or below it.
    /// Kept for the session. Also holds the hit areas the editor drew last, for drag and drop.
    /// </summary>
    public static class ActionSequenceEditorLayout
    {
        public static bool SequenceBelow { get; private set; }

        public static void ToggleSequenceBelow() => SequenceBelow = !SequenceBelow;

        /// <summary>First row of the library shown (scrolls when there are more actions than fit).</summary>
        public static int LibraryScrollRow { get; set; }

        /// <summary>True while the editor is the screen on show (set by the renderers).</summary>
        public static bool IsOpen { get; set; }

        /// <summary>Sequence card rectangles in slot order, from the last paint.</summary>
        public static List<(int X, int Y, int W, int H)> SequenceSlots { get; } = new();

        /// <summary>Whole sequence block (title row through the last card row), from the last paint.</summary>
        public static (int X, int Y, int W, int H) SequenceArea { get; set; }

        /// <summary>Whole library block, from the last paint.</summary>
        public static (int X, int Y, int W, int H) LibraryArea { get; set; }

        public static bool Contains((int X, int Y, int W, int H) r, int x, int y) =>
            r.W > 0 && r.H > 0 && x >= r.X && x < r.X + r.W && y >= r.Y && y < r.Y + r.H;

        /// <summary>
        /// Where a card dropped at (<paramref name="x"/>, <paramref name="y"/>) in the sequence block lands: the slot under
        /// the pointer (inserting before it), or the end when past the last card. -1 when outside the sequence block.
        /// </summary>
        public static int DropSlotAt(int x, int y)
        {
            if (!Contains(SequenceArea, x, y))
                return -1;
            for (int i = 0; i < SequenceSlots.Count; i++)
            {
                var r = SequenceSlots[i];
                if (y >= r.Y && y < r.Y + r.H + 1 && x < r.X + r.W)
                    return i;
            }
            return SequenceSlots.Count;
        }
    }

    /// <summary>
    /// Drag and drop in the Action Sequence editor: drag a library card onto the sequence to add it at that spot, drag a
    /// sequence card sideways to reorder, or drag it off the sequence to take it out. A press and release in place stays
    /// a click. Pointer events and rendering both run on the UI thread.
    /// </summary>
    public static class ActionSequenceDragState
    {
        public static bool IsArmed { get; private set; }
        public static bool IsActive { get; private set; }

        /// <summary>Action pool index when dragging from the library; -1 otherwise.</summary>
        public static int FromLibraryIndex { get; private set; } = -1;

        /// <summary>Sequence slot when dragging a sequence card; -1 otherwise.</summary>
        public static int FromSequenceIndex { get; private set; } = -1;

        public static string ActionName { get; private set; } = "";
        public static int PointerX { get; private set; }
        public static int PointerY { get; private set; }

        private static int _pressX;
        private static int _pressY;

        public static void ArmFromLibrary(int poolIndex, string actionName, int x, int y) => Arm(poolIndex, -1, actionName, x, y);

        public static void ArmFromSequence(int slot, string actionName, int x, int y) => Arm(-1, slot, actionName, x, y);

        private static void Arm(int poolIndex, int slot, string actionName, int x, int y)
        {
            IsArmed = true;
            IsActive = false;
            FromLibraryIndex = poolIndex;
            FromSequenceIndex = slot;
            ActionName = actionName ?? "";
            _pressX = PointerX = x;
            _pressY = PointerY = y;
        }

        /// <summary>Tracks the pointer cell. True when the drawing must change (drag started or the ghost moved).</summary>
        public static bool MoveTo(int x, int y)
        {
            if (!IsArmed || (x == PointerX && y == PointerY))
                return false;
            PointerX = x;
            PointerY = y;
            if (!IsActive && (x != _pressX || y != _pressY))
                IsActive = true;
            return IsActive;
        }

        public static void Clear()
        {
            IsArmed = false;
            IsActive = false;
            FromLibraryIndex = -1;
            FromSequenceIndex = -1;
            ActionName = "";
        }
    }
}
