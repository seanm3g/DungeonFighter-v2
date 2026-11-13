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

        /// <summary>Library card rectangles by action pool index (visible cards only), from the last paint.</summary>
        public static Dictionary<int, (int X, int Y, int W, int H)> LibrarySlots { get; } = new();

        /// <summary>
        /// Where a hover tooltip for an editor card goes, so it never covers the hovered card or the sequence row.
        /// Sequence card: just past the sequence block (over the library), lined up with the card. Library card: beside
        /// the card (right when it fits, else left), top-aligned with it, kept off the sequence block.
        /// False when the card's rect is unknown (caller keeps its default placement).
        /// </summary>
        public static bool TryPlaceCardTooltip(
            bool sequenceCard, int index, int boxW, int boxH,
            int innerLeft, int innerTop, int innerRight, int maxBottom,
            out int boxX, out int boxY)
        {
            boxX = boxY = 0;
            if (!IsOpen)
                return false;
            (int X, int Y, int W, int H) card;
            if (sequenceCard)
            {
                if (index < 0 || index >= SequenceSlots.Count)
                    return false;
                card = SequenceSlots[index];
            }
            else if (!LibrarySlots.TryGetValue(index, out card))
                return false;

            int maxX = innerRight - boxW + 1;
            int ClampX(int x) => System.Math.Max(innerLeft, System.Math.Min(x, maxX));
            int ClampY(int y, int lo, int hi) => System.Math.Max(lo, System.Math.Min(y, System.Math.Max(lo, hi - boxH + 1)));

            var seq = SequenceArea;
            int seqTop = seq.Y;
            int seqBottom = seq.Y + seq.H; // first row past the block (card numbers sit on the last row)

            if (sequenceCard)
            {
                boxX = ClampX(card.X);
                if (!SequenceBelow && seqBottom + boxH - 1 <= maxBottom)
                {
                    boxY = seqBottom;
                    return true;
                }
                if (SequenceBelow && seqTop - boxH >= innerTop)
                {
                    boxY = seqTop - boxH;
                    return true;
                }
                // No room off the sequence: beside the card instead (falls through to the library rule below).
            }

            int right = card.X + card.W + 1;
            int left = card.X - boxW - 1;
            if (right + boxW - 1 <= innerRight)
                boxX = right;
            else if (left >= innerLeft)
                boxX = left;
            else
                boxX = ClampX(innerRight - card.X - card.W > card.X - innerLeft ? right : left);

            // Keep the sequence row clear when the library side has the height for the box.
            int lo = innerTop, hi = maxBottom;
            if (!sequenceCard)
            {
                if (!SequenceBelow && maxBottom - seqBottom + 1 >= boxH)
                    lo = System.Math.Max(lo, seqBottom);
                else if (SequenceBelow && seqTop - innerTop >= boxH)
                    hi = System.Math.Min(hi, seqTop - 1);
            }
            boxY = ClampY(card.Y, lo, hi);
            return true;
        }

        public static bool Contains((int X, int Y, int W, int H) r, int x, int y) =>
            r.W > 0 && r.H > 0 && x >= r.X && x < r.X + r.W && y >= r.Y && y < r.Y + r.H;

        /// <summary>
        /// Where a card dropped at (<paramref name="x"/>, <paramref name="y"/>) in the sequence block lands: the slot under
        /// the pointer (inserting before it), or the end when past the last card. -1 when outside the sequence block.
        /// </summary>
        public static int DropSlotAt(int x, int y) => DropTargetAt(x, y, out int slot, out _) ? slot : -1;

        /// <summary>Columns at each side of a sequence card that still mean "insert here" rather than "drop onto this card".</summary>
        public const int InsertEdgeColumns = 4;

        /// <summary>
        /// Drop target in the sequence block. <paramref name="onto"/> true: the pointer is over the middle of card
        /// <paramref name="slot"/> (swap with it / replace it). False: insert before <paramref name="slot"/> (the gap or a
        /// card's left edge; a card's right edge inserts after it; past the last card, the end). False when outside the block.
        /// </summary>
        public static bool DropTargetAt(int x, int y, out int slot, out bool onto)
        {
            slot = -1;
            onto = false;
            if (!Contains(SequenceArea, x, y))
                return false;
            for (int i = 0; i < SequenceSlots.Count; i++)
            {
                var r = SequenceSlots[i];
                if (y < r.Y || y >= r.Y + r.H + 1)
                    continue;
                if (x < r.X + InsertEdgeColumns)
                {
                    slot = i;
                    return true;
                }
                if (x < r.X + r.W - InsertEdgeColumns)
                {
                    slot = i;
                    onto = y < r.Y + r.H;
                    return true;
                }
                if (x < r.X + r.W)
                {
                    slot = i + 1;
                    return true;
                }
            }
            slot = SequenceSlots.Count;
            return true;
        }
    }

    /// <summary>
    /// Drag and drop in the Action Sequence editor: drag a library card between sequence cards to add it at that spot or
    /// onto a sequence card to replace it, drag a sequence card between cards to move it or onto another card to swap the
    /// two, or drag it off the sequence to take it out. A press and release in place stays
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
