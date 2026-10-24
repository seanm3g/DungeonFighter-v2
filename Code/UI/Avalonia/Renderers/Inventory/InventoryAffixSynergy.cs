using System;
using System.Collections.Generic;

namespace RPGGame.UI.Avalonia.Renderers.Inventory
{
    /// <summary>
    /// Which items share an affix with the hovered one, so the inventory grid can light their frames.
    /// Affixes are adjective prefixes and "of …" suffixes; material and quality prefixes are skipped
    /// because nearly every item has one.
    /// </summary>
    public static class InventoryAffixSynergy
    {
        public static HashSet<string> GetAffixNames(Item? item)
        {
            var names = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            if (item == null)
                return names;
            foreach (var mod in item.Modifications)
            {
                if (string.IsNullOrWhiteSpace(mod.Name))
                    continue;
                string category = mod.PrefixCategory?.Trim() ?? "";
                if (category.Equals("MATERIAL", StringComparison.OrdinalIgnoreCase)
                    || category.Equals("QUALITY", StringComparison.OrdinalIgnoreCase))
                    continue;
                names.Add(mod.Name.Trim());
            }
            foreach (var bonus in item.StatBonuses)
            {
                if (!string.IsNullOrWhiteSpace(bonus.Name))
                    names.Add(bonus.Name.Trim());
            }
            return names;
        }

        /// <summary>True when <paramref name="item"/> is a different item carrying any of <paramref name="hoveredAffixes"/>.</summary>
        public static bool SharesAffix(Item? item, Item? hovered, HashSet<string> hoveredAffixes)
        {
            if (item == null || hovered == null || ReferenceEquals(item, hovered) || hoveredAffixes.Count == 0)
                return false;
            return GetAffixNames(item).Overlaps(hoveredAffixes);
        }

        /// <summary>
        /// The item under the pointer from a <see cref="LeftPanelHoverState"/> value: a bag row/tile
        /// (<c>lphover:inv:{index}</c>) or a gear slot (<c>lphover:gear:{slot}</c>); null otherwise.
        /// </summary>
        public static Item? ResolveHoveredItem(string hoverValue, IReadOnlyList<Item>? inventory, Character? character)
        {
            if (string.IsNullOrEmpty(hoverValue))
                return null;
            string invPrefix = LeftPanelHoverState.Prefix + "inv:";
            string gearPrefix = LeftPanelHoverState.Prefix + "gear:";
            if (hoverValue.StartsWith(invPrefix, StringComparison.Ordinal))
            {
                if (inventory != null && int.TryParse(hoverValue.AsSpan(invPrefix.Length), out int index)
                    && index >= 0 && index < inventory.Count)
                    return inventory[index];
                return null;
            }
            if (hoverValue.StartsWith(gearPrefix, StringComparison.Ordinal))
                return character?.Equipment.GetSlotItem(hoverValue.Substring(gearPrefix.Length));
            return null;
        }
    }
}
