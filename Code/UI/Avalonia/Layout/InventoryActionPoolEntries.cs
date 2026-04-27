using System.Collections.Generic;
using RPGGame;

namespace RPGGame.UI.Avalonia.Layout
{
    /// <summary>
    /// Flat list of actions granted by unequipped inventory items (for the inventory right panel and combo management UI).
    /// </summary>
    public static class InventoryActionPoolEntries
    {
        public readonly struct Entry
        {
            public int InventoryIndex { get; }
            public int ActionIndexInItem { get; }
            public string ActionName { get; }
            public string ItemName { get; }

            public Entry(int inventoryIndex, int actionIndexInItem, string actionName, string itemName)
            {
                InventoryIndex = inventoryIndex;
                ActionIndexInItem = actionIndexInItem;
                ActionName = actionName ?? "";
                ItemName = itemName ?? "";
            }
        }

        /// <summary>
        /// Equipment slot key for <see cref="Character.EquipItem"/> / comparison flow.
        /// </summary>
        public static string? GetEquipSlotForItem(Item item)
        {
            if (item == null)
                return null;
            return item.Type switch
            {
                ItemType.Weapon => "weapon",
                ItemType.Head => "head",
                ItemType.Chest => "body",
                ItemType.Feet => "feet",
                _ => null
            };
        }

        /// <summary>
        /// One row per action on each bag item, in inventory order.
        /// </summary>
        public static List<Entry> Build(Character character)
        {
            var list = new List<Entry>();
            if (character?.Inventory == null || character.Inventory.Count == 0)
                return list;

            for (int i = 0; i < character.Inventory.Count; i++)
            {
                var item = character.Inventory[i];
                if (item == null || GetEquipSlotForItem(item) == null)
                    continue;

                var names = GearActionNames.Resolve(item);
                for (int j = 0; j < names.Count; j++)
                {
                    if (string.IsNullOrWhiteSpace(names[j]))
                        continue;
                    list.Add(new Entry(i, j, names[j], item.Name ?? ""));
                }
            }

            return list;
        }
    }
}
