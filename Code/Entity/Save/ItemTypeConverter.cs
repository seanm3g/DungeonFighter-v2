using System.Collections.Generic;

namespace RPGGame
{
    /// <summary>
    /// Helper methods for converting base Item objects to their proper derived types
    /// </summary>
    public static class ItemTypeConverter
    {
        /// <summary>
        /// Converts a base Item to its proper derived type based on the ItemType
        /// </summary>
        /// <param name="item">The base item to convert</param>
        /// <returns>The properly typed item, or null if input is null</returns>
        public static Item? ConvertItemToProperType(Item? item)
        {
            if (item == null) return null;

            return item.Type switch
            {
                ItemType.Weapon => ConvertToWeaponItem(item),
                ItemType.Head => ConvertToHeadItem(item),
                ItemType.Chest => ConvertToChestItem(item),
                ItemType.Feet => ConvertToFeetItem(item),
                _ => item // Return as-is if type is unknown
            };
        }

        /// <summary>
        /// Converts a list of base Items to their proper derived types
        /// </summary>
        /// <param name="items">The list of base items to convert</param>
        /// <returns>A new list with properly typed items</returns>
        public static List<Item> ConvertItemsToProperTypes(List<Item> items)
        {
            var convertedItems = new List<Item>();
            foreach (var item in items)
            {
                var converted = ConvertItemToProperType(item);
                if (converted != null)
                {
                    convertedItems.Add(converted);
                }
            }
            return convertedItems;
        }

        private static WeaponItem ConvertToWeaponItem(Item item)
        {
            var weapon = new WeaponItem(item.Name, item.Tier);
            
            // Copy all properties from the base item
            CopyBaseItemProperties(item, weapon);
            
            // Try to preserve weapon-specific properties if they exist
            // Note: These might be lost during JSON serialization, but we'll set reasonable defaults
            if (item is WeaponItem originalWeapon)
            {
                weapon.BaseDamage = originalWeapon.BaseDamage;
                weapon.BaseAttackSpeed = originalWeapon.BaseAttackSpeed;
                weapon.WeaponType = originalWeapon.WeaponType;
            }
            else
            {
                // Set reasonable defaults if we can't preserve the original values
                weapon.BaseDamage = 10 + item.Tier * 2;
                weapon.BaseAttackSpeed = 0.05;
                weapon.WeaponType = WeaponType.Sword;
            }
            
            return weapon;
        }

        private static HeadItem ConvertToHeadItem(Item item)
        {
            var head = new HeadItem(item.Name, item.Tier);
            CopyBaseItemProperties(item, head);
            
            if (item is HeadItem originalHead)
            {
                head.Armor = originalHead.Armor;
            }
            else
            {
                head.Armor = 5 + item.Tier;
            }
            
            return head;
        }

        private static ChestItem ConvertToChestItem(Item item)
        {
            var chest = new ChestItem(item.Name, item.Tier);
            CopyBaseItemProperties(item, chest);
            
            if (item is ChestItem originalChest)
            {
                chest.Armor = originalChest.Armor;
            }
            else
            {
                chest.Armor = 8 + item.Tier * 2;
            }
            
            return chest;
        }

        private static FeetItem ConvertToFeetItem(Item item)
        {
            var feet = new FeetItem(item.Name, item.Tier);
            CopyBaseItemProperties(item, feet);
            
            if (item is FeetItem originalFeet)
            {
                feet.Armor = originalFeet.Armor;
            }
            else
            {
                feet.Armor = 3 + item.Tier;
            }
            
            return feet;
        }

        private static void CopyBaseItemProperties(Item source, Item destination)
        {
            destination.Name = source.Name;
            destination.Type = source.Type;
            destination.Tier = source.Tier;
            destination.ComboBonus = source.ComboBonus;
            destination.Rarity = source.Rarity;
            destination.StatBonuses = source.StatBonuses;
            destination.ActionBonuses = source.ActionBonuses;
            destination.Modifications = source.Modifications;
            destination.ArmorStatuses = source.ArmorStatuses;
            destination.BonusDamage = source.BonusDamage;
            destination.BonusAttackSpeed = source.BonusAttackSpeed;
            destination.GearAction = source.GearAction;
        }
    }
}

