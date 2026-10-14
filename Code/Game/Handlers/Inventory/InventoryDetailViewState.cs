namespace RPGGame.Handlers.Inventory
{
    /// <summary>
    /// F1 inventory detail view: items listed one line each on the left, the selected item's full description on the right.
    /// </summary>
    /// <param name="SelectedInventoryIndex">Raw inventory index of the described item; -1 (or a hidden item) falls back to the first listed item.</param>
    /// <param name="Grid">New inventory: item icons in a grid on the left instead of one-line names.</param>
    public sealed record InventoryDetailViewState(int SelectedInventoryIndex, bool Grid = false)
    {
        /// <summary>Click token for the header button that switches between the new (icon grid) and old inventory.</summary>
        public const string ToggleGridInput = "invgrid";

        /// <summary>Click token prefix for detail-view list rows (<c>invsel:{inventoryIndex}</c>).</summary>
        public const string SelectInputPrefix = "invsel:";

        /// <summary>Click token prefix for new-inventory paper-doll gear slots (<c>invslot:{slot}</c>); clicks only show the tooltip.</summary>
        public const string SlotInputPrefix = "invslot:";

        /// <summary>Drop token: bag item dragged onto the paper doll (<c>invequip:{inventoryIndex}</c>), equips into its own slot.</summary>
        public const string DropEquipPrefix = "invequip:";

        /// <summary>Drop token: gear slot dragged into the bag (<c>invunequip:{slot}</c>).</summary>
        public const string DropUnequipPrefix = "invunequip:";

        /// <summary>Names longer than this are cut off in the list; the full name shows in the description pane.</summary>
        public const int ListNameMaxChars = 15;
    }
}
