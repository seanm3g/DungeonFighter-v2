namespace RPGGame.UI
{
    /// <summary>
    /// New-inventory drag and drop between the bag grid and the paper-doll gear slots.
    /// A press on a bag tile or a filled gear slot arms the drag; it becomes active once the pointer leaves the press cell
    /// (a press and release in place stays a normal click). Pointer events and rendering both run on the UI thread.
    /// </summary>
    public static class InventoryDragState
    {
        /// <summary>A drag source is held (pointer down on a tile or slot).</summary>
        public static bool IsArmed { get; private set; }

        /// <summary>The pointer has left the press cell: render the ghost and drop highlights.</summary>
        public static bool IsActive { get; private set; }

        /// <summary>Raw inventory index when dragging out of the bag; -1 when dragging a gear slot.</summary>
        public static int SourceInventoryIndex { get; private set; } = -1;

        /// <summary>Equip slot key (<c>weapon</c>, <c>head</c>, …) when dragging out of the paper doll; null for bag drags.</summary>
        public static string? SourceSlot { get; private set; }

        public static Item? Item { get; private set; }

        public static int PointerX { get; private set; }
        public static int PointerY { get; private set; }

        private static int _pressX;
        private static int _pressY;

        public static bool FromBag => IsArmed && SourceInventoryIndex >= 0;
        public static bool FromSlot => IsArmed && SourceSlot != null;

        public static void ArmFromBag(int inventoryIndex, Item item, int gridX, int gridY) =>
            Arm(inventoryIndex, null, item, gridX, gridY);

        public static void ArmFromSlot(string slot, Item item, int gridX, int gridY) =>
            Arm(-1, slot, item, gridX, gridY);

        private static void Arm(int inventoryIndex, string? slot, Item item, int gridX, int gridY)
        {
            IsArmed = true;
            IsActive = false;
            SourceInventoryIndex = inventoryIndex;
            SourceSlot = slot;
            Item = item;
            _pressX = PointerX = gridX;
            _pressY = PointerY = gridY;
        }

        /// <summary>Tracks the pointer cell. Returns true when the drawing must change (drag started or the ghost moved).</summary>
        public static bool MoveTo(int gridX, int gridY)
        {
            if (!IsArmed || (gridX == PointerX && gridY == PointerY))
                return false;
            PointerX = gridX;
            PointerY = gridY;
            if (!IsActive && (gridX != _pressX || gridY != _pressY))
                IsActive = true;
            return IsActive;
        }

        public static void Clear()
        {
            IsArmed = false;
            IsActive = false;
            SourceInventoryIndex = -1;
            SourceSlot = null;
            Item = null;
        }
    }
}
