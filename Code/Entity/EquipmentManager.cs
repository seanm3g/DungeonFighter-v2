using System;
using System.Collections.Generic;
using RPGGame.Combat.Calculators;

namespace RPGGame
{
    /// <summary>
    /// Manages equipment operations for Character, handling complex equipment change logic
    /// Extracts equipment management from the main Character class
    /// </summary>
    public class EquipmentManager
    {
        private readonly Character _character;

        public EquipmentManager(Character character)
        {
            _character = character;
        }

        /// <summary>
        /// Equips an item and handles all related updates
        /// </summary>
        public Item? EquipItem(Item item, string slot)
        {
            // Store current health percentage before equipping
            double healthPercentage = _character.GetHealthPercentage();
            int oldMaxHealth = _character.GetEffectiveMaxHealth();
            
            Item? previousItem = _character.Equipment.EquipItem(item, slot);
            
            // Check if max health changed and adjust current health accordingly
            int newMaxHealth = _character.GetEffectiveMaxHealth();
            _character.Health.AdjustHealthForMaxHealthChange(oldMaxHealth, newMaxHealth);
            
            // Update actions after equipment change
            UpdateActionsAfterGearChange(previousItem, item, slot);
            
            // Apply roll bonuses from the new item
            _character.Actions.ApplyRollBonusesFromGear(_character, item);
            
            // Update reroll charges from Divine modifications
            _character.Effects.RerollCharges = _character.Equipment.GetTotalRerollCharges();
            
            // Invalidate damage cache since equipment changed
            DamageCalculator.InvalidateCache(_character);
            
            return previousItem;
        }

        /// <summary>
        /// Unequips an item and handles all related updates
        /// </summary>
        public Item? UnequipItem(string slot)
        {
            // Store current health percentage before unequipping
            double healthPercentage = _character.GetHealthPercentage();
            int oldMaxHealth = _character.GetEffectiveMaxHealth();
            
            Item? unequippedItem = _character.Equipment.UnequipItem(slot);
            
            // Check if max health changed and adjust current health accordingly
            int newMaxHealth = _character.GetEffectiveMaxHealth();
            _character.Health.AdjustHealthForMaxHealthChange(oldMaxHealth, newMaxHealth);
            
            // Remove roll bonuses from the unequipped item
            if (unequippedItem != null)
            {
                _character.Actions.RemoveRollBonusesFromGear(_character, unequippedItem);
            }
            
            // Update reroll charges from Divine modifications
            _character.Effects.RerollCharges = _character.Equipment.GetTotalRerollCharges();
            
            // Update actions after equipment change
            UpdateActionsAfterGearChange(unequippedItem, null, slot);
            
            // Invalidate damage cache since equipment changed
            DamageCalculator.InvalidateCache(_character);
            
            return unequippedItem;
        }

        /// <summary>
        /// Updates actions after equipment changes
        /// </summary>
        private void UpdateActionsAfterGearChange(Item? previousItem, Item? newItem, string slot)
        {
            // Remove actions from previous item
            if (previousItem != null)
            {
                if (previousItem is WeaponItem oldWeapon)
                {
                    _character.Actions.RemoveWeaponActions(_character, oldWeapon);
                }
                else
                {
                    _character.Actions.RemoveArmorActions(_character, previousItem);
                }
            }

            // Add actions from new item
            if (newItem != null)
            {
                if (newItem is WeaponItem weapon)
                {
                    _character.Actions.AddWeaponActions(_character, weapon);
                }
                else
                {
                    _character.Actions.AddArmorActions(_character, newItem);
                }
            }

            // CRITICAL: Ensure BASIC ATTACK is always available after equipment changes
            // This prevents the issue where rolls 6-13 can't find BASIC ATTACK
            _character.Actions.EnsureBasicAttackAvailable(_character);

            // Update combo sequence after equipment change
            _character.Actions.UpdateComboSequenceAfterGearChange(_character);
            
            // If weapon was changed, handle combo sequence intelligently
            if (slot.ToLower() == "weapon")
            {
                // If combo is now empty, initialize default combo
                if (_character.Actions.ComboSequence.Count == 0)
                {
                    _character.Actions.InitializeDefaultCombo(_character, _character.Equipment.Weapon as WeaponItem);
                }
            }
            
            // Track item equipping statistics
            if (newItem != null)
            {
                _character.RecordItemEquipped();
            }
        }

        /// <summary>
        /// Adds an item to inventory
        /// </summary>
        public void AddToInventory(Item item) => _character.Equipment.AddToInventory(item);

        /// <summary>
        /// Removes an item from inventory
        /// </summary>
        public bool RemoveFromInventory(Item item) => _character.Equipment.RemoveFromInventory(item);
    }
}
