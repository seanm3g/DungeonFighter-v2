using System;
using System.Collections.Generic;
using System.Linq;
using RPGGame.Audio;
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
        /// Equips an item when catalog <see cref="Item.AttributeRequirements"/> are met by effective STR/AGI/TEC/INT.
        /// Inventory and UI must use this (or check <see cref="Item.GetEquipBlockedReason"/> first) before mutating bag contents.
        /// </summary>
        /// <returns><c>true</c> if the item is now equipped; <c>false</c> if requirements block equip (no state change).</returns>
        public bool TryEquipItem(
            Item item,
            string slot,
            out Item? replacedItem,
            out string? failureReason,
            bool ignoreAttributeRequirements = false)
        {
            replacedItem = null;
            failureReason = ignoreAttributeRequirements ? null : item.GetEquipBlockedReason(_character);
            if (failureReason != null)
                return false;

            int oldMaxHealth = _character.GetEffectiveMaxHealth();

            replacedItem = _character.Equipment.EquipItem(item, slot);

            int newMaxHealth = _character.GetEffectiveMaxHealth();
            _character.Health.AdjustHealthForMaxHealthChange(oldMaxHealth, newMaxHealth);
            _character.RefreshRoomArmor();

            UpdateActionsAfterGearChange(replacedItem, item, slot);

            _character.Actions.ApplyRollBonusesFromGear(_character, item);

            _character.Effects.RerollCharges = _character.Equipment.GetTotalRerollCharges();

            DamageCalculator.InvalidateCache(_character);

            AudioCues.Trigger(AudioCue.Loot_Equip);
            return true;
        }

        /// <summary>
        /// Equips an item and handles all related updates. On attribute requirement failure, returns <c>null</c> and does not equip.
        /// Prefer <see cref="TryEquipItem"/> when the caller must distinguish failure from an empty slot.
        /// </summary>
        public Item? EquipItem(Item item, string slot)
        {
            if (!TryEquipItem(item, slot, out var previousItem, out _))
                return null;
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
            _character.RefreshRoomArmor();
            
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

            if (unequippedItem != null)
                AudioCues.Trigger(AudioCue.Loot_Unequip);
            return unequippedItem;
        }

        /// <summary>
        /// Updates actions after equipment changes
        /// </summary>
        private void UpdateActionsAfterGearChange(Item? previousItem, Item? newItem, string slot)
        {
            // Pool mutations replace some Action instances (e.g. AddClassActions removes/re-adds class actions).
            // Combo slots hold references; matching only by reference would drop valid actions by name.
            // Save order by name and restore after the pool is updated (same idea as CharacterSerializer.RebuildCharacterActions).
            var savedComboNames = _character.GetComboActions().Select(a => a.Name).ToList();

            // Remove actions from previous item
            if (previousItem != null)
            {
                if (IsWeaponSlotOrItem(slot, previousItem))
                {
                    _character.Actions.RemoveWeaponActions(_character, previousItem);
                }
                else
                {
                    _character.Actions.RemoveArmorActions(_character, previousItem);
                }
            }

            // Re-add class actions before adding gear actions so RemoveClassActions does not strip
            // gear-added actions (JAB, TAUNT, etc. are in AllClassActions and would otherwise be removed).
            WeaponType? weaponType = GearActionNames.TryResolveWeaponType(_character.Equipment.Weapon, out var resolvedWeaponType)
                ? resolvedWeaponType
                : null;
            _character.Actions.AddClassActions(_character, _character.Progression, weaponType);

            // Add actions from new item (after class actions so gear actions are never removed by RemoveClassActions)
            if (newItem != null)
            {
                if (IsWeaponSlotOrItem(slot, newItem))
                {
                    _character.Actions.AddWeaponActions(_character, newItem);
                }
                else
                {
                    _character.Actions.AddArmorActions(_character, newItem);
                }
            }

            if (savedComboNames.Count > 0)
                _character.RestoreComboFromActionNames(savedComboNames);
            else
                _character.Actions.UpdateComboSequenceAfterGearChange(_character);

            ComboSequenceMaxHelper.TrimComboSequenceToMax(
                _character,
                ComboSequenceMaxHelper.GetEffectiveMax(_character));
            _character.Effects.ClearPendingActionBonuses(); // Slot indices may be invalid after combo change

            // If combo is now empty after gear change (regardless of slot), reinitialize default combo
            // This ensures that changing any gear that removes actions doesn't leave the player without a default action
            if (_character.Actions.ComboSequence.Count == 0)
            {
                _character.Actions.InitializeDefaultCombo(_character, _character.Equipment.Weapon);
            }
            
            // Track item equipping statistics
            if (newItem != null)
            {
                _character.RecordItemEquipped();
            }
        }

        private static bool IsWeaponSlotOrItem(string slot, Item? item)
        {
            return string.Equals(slot, "weapon", StringComparison.OrdinalIgnoreCase) ||
                   item?.Type == ItemType.Weapon;
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
