using System;

namespace RPGGame
{
    /// <summary>
    /// Service for handling equipment display logic to eliminate duplication between display managers
    /// </summary>
    public class EquipmentDisplayService
    {
        /// <summary>
        /// Displays currently equipped items with their stats
        /// </summary>
        public void DisplayCurrentEquipment(Character character, Action<string> writeMenuLine)
        {
            writeMenuLine("Currently Equipped:");
            
            // Show weapon with indented stats
            if (character.Weapon is WeaponItem weapon)
            {
                writeMenuLine($"Weapon: {ItemDisplayFormatter.GetColoredItemName(weapon)}");
                writeMenuLine($"    Damage: {weapon.GetTotalDamage()}, Attack Speed: {weapon.GetAttackSpeedMultiplier():F1}x");
                
                // Show weapon bonuses and modifications
                ItemDisplayFormatter.FormatItemBonuses(weapon, writeMenuLine);
            }
            else
            {
                writeMenuLine("Weapon: None");
            }
            
            // Show armor pieces with indented stats
            DisplayArmorPiece(character.Head, "Head", writeMenuLine);
            DisplayArmorPiece(character.Body, "Chest", writeMenuLine);
            DisplayArmorPiece(character.Legs, "Legs", writeMenuLine);
            DisplayArmorPiece(character.Feet, "Feet", writeMenuLine);
        }

        /// <summary>
        /// Displays a single armor piece
        /// </summary>
        private void DisplayArmorPiece(Item? armor, string slotName, Action<string> writeMenuLine)
        {
            if (armor != null)
            {
                writeMenuLine($"{slotName}: {ItemDisplayFormatter.GetColoredItemName(armor)}");
                writeMenuLine($"    Armor: {GetArmorValue(armor)}");
                
                // Show armor bonuses and modifications
                ItemDisplayFormatter.FormatItemBonuses(armor, writeMenuLine);
            }
            else
            {
                writeMenuLine($"{slotName}: None");
            }
        }

        /// <summary>
        /// Gets the armor value for an armor item
        /// </summary>
        private int GetArmorValue(Item armor)
        {
            return armor switch
            {
                HeadItem head => head.GetTotalArmor(),
                ChestItem chest => chest.GetTotalArmor(),
                LegsItem legs => legs.GetTotalArmor(),
                FeetItem feet => feet.GetTotalArmor(),
                _ => 0
            };
        }

        /// <summary>
        /// Gets item actions as a formatted string
        /// </summary>
        public string GetItemActions(Item item)
        {
            var actions = GearActionNames.Resolve(item);
            return actions.Count > 0 ? " | " + string.Join(" | ", actions) : "";
        }
    }
}
