using System;
using System.Collections.Generic;

namespace RPGGame
{
    /// <summary>
    /// Manages equipment slots (Head, Body, Weapon, Feet) for a character.
    /// Handles equipping and unequipping items from specific slots.
    /// </summary>
    public class EquipmentSlotManager
    {
        public Item? Head { get; set; }
        public Item? Body { get; set; }
        public Item? Weapon { get; set; }
        public Item? Feet { get; set; }

        public EquipmentSlotManager()
        {
            Head = null;
            Body = null;
            Weapon = null;
            Feet = null;
        }

        /// <summary>
        /// Equips an item to a specific slot, returning any previously equipped item.
        /// </summary>
        /// <param name="item">The item to equip</param>
        /// <param name="slot">The slot name (head, body, weapon, feet)</param>
        /// <returns>The previously equipped item, or null if slot was empty</returns>
        public Item? EquipItem(Item item, string slot)
        {
            Item? previousItem = null;
            switch (slot.ToLower())
            {
                case "head":
                    previousItem = Head;
                    Head = item;
                    break;
                case "body":
                    previousItem = Body;
                    Body = item;
                    break;
                case "weapon":
                    previousItem = Weapon;
                    Weapon = item;
                    break;
                case "feet":
                    previousItem = Feet;
                    Feet = item;
                    break;
            }
            return previousItem;
        }

        /// <summary>
        /// Unequips an item from a specific slot.
        /// </summary>
        /// <param name="slot">The slot name (head, body, weapon, feet)</param>
        /// <returns>The unequipped item, or null if slot was empty</returns>
        public Item? UnequipItem(string slot)
        {
            Item? unequippedItem = null;
            switch (slot.ToLower())
            {
                case "head":
                    unequippedItem = Head;
                    Head = null;
                    break;
                case "body":
                    unequippedItem = Body;
                    Body = null;
                    break;
                case "weapon":
                    unequippedItem = Weapon;
                    Weapon = null;
                    break;
                case "feet":
                    unequippedItem = Feet;
                    Feet = null;
                    break;
            }
            return unequippedItem;
        }

        /// <summary>
        /// Gets all currently equipped items.
        /// </summary>
        /// <returns>Array of equipped items (may contain nulls)</returns>
        public Item?[] GetEquippedItems()
        {
            return new[] { Head, Body, Weapon, Feet };
        }

        /// <summary>
        /// Checks if a specific slot is occupied.
        /// </summary>
        /// <param name="slot">The slot name</param>
        /// <returns>True if the slot has an item, false otherwise</returns>
        public bool IsSlotEquipped(string slot)
        {
            return (slot.ToLower()) switch
            {
                "head" => Head != null,
                "body" => Body != null,
                "weapon" => Weapon != null,
                "feet" => Feet != null,
                _ => false
            };
        }

        /// <summary>
        /// Gets the item in a specific slot.
        /// </summary>
        /// <param name="slot">The slot name</param>
        /// <returns>The item in the slot, or null if empty</returns>
        public Item? GetSlotItem(string slot)
        {
            return (slot.ToLower()) switch
            {
                "head" => Head,
                "body" => Body,
                "weapon" => Weapon,
                "feet" => Feet,
                _ => null
            };
        }

        /// <summary>
        /// Unequips all items from all slots.
        /// </summary>
        public void UnequipAll()
        {
            Head = null;
            Body = null;
            Weapon = null;
            Feet = null;
        }
    }
}

