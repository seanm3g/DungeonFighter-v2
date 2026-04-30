using System.Collections.Generic;
using RPGGame.Data;

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
                weapon.RolledDamageBonus = originalWeapon.RolledDamageBonus;
                weapon.BaseAttackSpeed = originalWeapon.BaseAttackSpeed;
                weapon.WeaponType = originalWeapon.WeaponType;
            }
            else
            {
                // Set reasonable defaults if we can't preserve the original values
                weapon.BaseDamage = 10 + item.Tier * 2;
                weapon.BaseAttackSpeed = 0.05;
                weapon.WeaponType = item.WeaponType;
                // Old saves deserialized weapons as base Item without weaponType; recover from Weapons.json by name.
                if (WeaponTypeFromCatalog.TryGetByWeaponName(item.Name, out var catalogType))
                    weapon.WeaponType = catalogType;
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
                head.Armor = ResolveArmorWhenDeserializedAsBaseItem(item);
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
                chest.Armor = ResolveArmorWhenDeserializedAsBaseItem(item);
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
                feet.Armor = ResolveArmorWhenDeserializedAsBaseItem(item);
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
            destination.WeaponType = source.WeaponType;
            destination.Level = source.Level;
            destination.Tags = source.Tags;
            destination.AttributeRequirements = source.AttributeRequirements;
        }

        /// <summary>
        /// Legacy saves and older JSON clones deserialize armor slots as base <see cref="Item"/> (no <c>Armor</c> field).
        /// Prefer <see cref="GameConstants.ArmorJson"/> by name and slot; keep tier heuristics only for unknown gear.
        /// </summary>
        private static int ResolveArmorWhenDeserializedAsBaseItem(Item item)
        {
            if (TryGetArmorFromArmorCatalog(item, out int catalogArmor))
                return catalogArmor;

            return item.Type switch
            {
                ItemType.Head => 5 + item.Tier,
                ItemType.Chest => 8 + item.Tier * 2,
                ItemType.Feet => 3 + item.Tier,
                _ => 0
            };
        }

        private static bool TryGetArmorFromArmorCatalog(Item item, out int armor)
        {
            armor = 0;
            if (string.IsNullOrWhiteSpace(item.Name))
                return false;

            string? jsonSlot = item.Type switch
            {
                ItemType.Head => "head",
                ItemType.Chest => "chest",
                ItemType.Feet => "feet",
                _ => null
            };
            if (jsonSlot == null)
                return false;

            var rows = JsonLoader.LoadJsonList<ArmorData>(GameConstants.ArmorJson, useCache: true);
            for (int i = 0; i < rows.Count; i++)
            {
                var row = rows[i];
                if (string.Equals(row.Slot, jsonSlot, System.StringComparison.OrdinalIgnoreCase)
                    && string.Equals(row.Name, item.Name, System.StringComparison.OrdinalIgnoreCase))
                {
                    armor = row.Armor;
                    return true;
                }
            }

            return false;
        }
    }
}

