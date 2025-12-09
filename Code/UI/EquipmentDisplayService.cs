using System;
using System.Collections.Generic;
using System.Linq;

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
                FeetItem feet => feet.GetTotalArmor(),
                _ => 0
            };
        }

        /// <summary>
        /// Gets item actions as a formatted string
        /// </summary>
        public string GetItemActions(Item item)
        {
            var actions = new List<string>();
            
            if (item is WeaponItem weapon)
            {
                var weaponActions = GetWeaponActionsFromJson(weapon.WeaponType);
                actions.AddRange(weaponActions);
            }
            else if (ShouldArmorHaveActions(item))
            {
                var armorActions = GetRandomArmorActionFromJson(item);
                actions.AddRange(armorActions);
            }
            
            return actions.Count > 0 ? " | " + string.Join(" | ", actions) : "";
        }

        /// <summary>
        /// Determines if an armor piece should have special actions
        /// </summary>
        private bool ShouldArmorHaveActions(Item armor)
        {
            // Check if this armor piece has special properties that indicate it should have actions
            
            // 1. Check if it has modifications (indicates special gear)
            if (armor.Modifications.Count > 0)
            {
                return true;
            }
            
            // 2. Check if it has stat bonuses (indicates special gear)
            if (armor.StatBonuses.Count > 0)
            {
                return true;
            }
            
            // 3. Check if it has action bonuses (legacy system)
            if (armor.ActionBonuses.Count > 0)
            {
                return true;
            }
            
            // 4. Check if it's not basic starter gear by name
            // Basic gear names moved to GameData configuration
            string[] basicGearNames = BasicGearConfig.GetBasicGearNames();
            if (basicGearNames.Contains(armor.Name))
            {
                return false; // Basic starter gear should have no actions
            }
            
            // 5. If it has a special name or properties, it might be special gear
            // For now, assume any non-basic gear might have actions
            return true;
        }

        /// <summary>
        /// Gets weapon actions from JSON data using tag-based matching.
        /// </summary>
        private List<string> GetWeaponActionsFromJson(WeaponType weaponType)
        {
            var weaponTag = weaponType.ToString().ToLower();
            var allActions = ActionLoader.GetAllActions();
            var weaponActions = new List<string>();
            
            // Get actions that match the weapon type and have "weapon" tag, but exclude "unique" actions
            // Uses case-insensitive comparison for robustness
            foreach (var actionData in allActions)
            {
                if (actionData.Tags != null &&
                    actionData.Tags.Any(tag => tag.Equals("weapon", StringComparison.OrdinalIgnoreCase)) &&
                    actionData.Tags.Any(tag => tag.Equals(weaponTag, StringComparison.OrdinalIgnoreCase)) &&
                    !actionData.Tags.Any(tag => tag.Equals("unique", StringComparison.OrdinalIgnoreCase)))
                {
                    weaponActions.Add(actionData.Name);
                }
            }
            
            // Fallback to BASIC ATTACK if no weapon-specific actions found
            if (weaponActions.Count == 0)
            {
                return new List<string> { "BASIC ATTACK" };
            }
            
            return weaponActions;
        }

        /// <summary>
        /// Gets random armor actions from JSON data
        /// </summary>
        private List<string> GetRandomArmorActionFromJson(Item armor)
        {
            // Return empty list - no action bonuses to display
            return new List<string>();
        }
    }
}
