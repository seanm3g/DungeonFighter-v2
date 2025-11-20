using System;
using System.Collections.Generic;
using System.Linq;

namespace RPGGame
{
    /// <summary>
    /// Manages character equipment, inventory, and equipment-related bonuses.
    /// Uses a facade pattern to delegate specialized responsibilities to focused managers.
    /// </summary>
    public class CharacterEquipment
    {
        public List<Item> Inventory { get; set; }

        // Specialized managers using facade pattern
        private readonly EquipmentSlotManager slotManager;
        private readonly EquipmentBonusCalculator bonusCalculator;
        private readonly ModificationBonusCalculator modificationCalculator;
        private readonly ArmorStatusManager armorStatusManager;
        private readonly EquipmentActionProvider actionProvider;

        public CharacterEquipment()
        {
            Inventory = new List<Item>();

            // Initialize specialized managers
            slotManager = new EquipmentSlotManager();
            bonusCalculator = new EquipmentBonusCalculator(slotManager);
            modificationCalculator = new ModificationBonusCalculator(slotManager);
            armorStatusManager = new ArmorStatusManager(slotManager);
            actionProvider = new EquipmentActionProvider(slotManager);
        }

        // ============================================================================
        // Equipment Slot Properties & Methods (Facade to EquipmentSlotManager)
        // ============================================================================

        public Item? Head
        {
            get => slotManager.Head;
            set => slotManager.Head = value;
        }

        public Item? Body
        {
            get => slotManager.Body;
            set => slotManager.Body = value;
        }

        public Item? Weapon
        {
            get => slotManager.Weapon;
            set => slotManager.Weapon = value;
        }

        public Item? Feet
        {
            get => slotManager.Feet;
            set => slotManager.Feet = value;
        }

        public Item? EquipItem(Item item, string slot) => slotManager.EquipItem(item, slot);
        public Item? UnequipItem(string slot) => slotManager.UnequipItem(slot);

        public void AddToInventory(Item item) => Inventory.Add(item);
        public bool RemoveFromInventory(Item item) => Inventory.Remove(item);

        // ============================================================================
        // Equipment Bonus Methods (Facade to EquipmentBonusCalculator)
        // ============================================================================

        public int GetEquipmentStatBonus(string statType) => bonusCalculator.GetStatBonus(statType);
        public double GetEquipmentStatBonusDouble(string statType) => bonusCalculator.GetStatBonusDouble(statType);
        public int GetEquipmentDamageBonus() => bonusCalculator.GetDamageBonus();
        public int GetEquipmentHealthBonus() => bonusCalculator.GetHealthBonus();
        public int GetEquipmentRollBonus() => bonusCalculator.GetRollBonus();
        public int GetMagicFind() => bonusCalculator.GetMagicFind();
        public double GetEquipmentAttackSpeedBonus() => bonusCalculator.GetAttackSpeedBonus();
        public int GetEquipmentHealthRegenBonus() => bonusCalculator.GetHealthRegenBonus();
        public int GetTotalArmor() => bonusCalculator.GetTotalArmor();
        public int GetTotalRerollCharges() => bonusCalculator.GetTotalRerollCharges();

        // ============================================================================
        // Modification Bonus Methods (Facade to ModificationBonusCalculator)
        // ============================================================================

        public int GetModificationMagicFind() => modificationCalculator.GetMagicFind();
        public int GetModificationRollBonus() => modificationCalculator.GetRollBonus();
        public int GetModificationDamageBonus() => modificationCalculator.GetDamageBonus();
        public double GetModificationSpeedMultiplier() => modificationCalculator.GetSpeedMultiplier();
        public double GetModificationDamageMultiplier() => modificationCalculator.GetDamageMultiplier();
        public double GetModificationLifesteal() => modificationCalculator.GetLifesteal();
        public int GetModificationGodlikeBonus() => modificationCalculator.GetGodlikeBonus();
        public double GetModificationBleedChance() => modificationCalculator.GetBleedChance();
        public double GetModificationUniqueActionChance() => modificationCalculator.GetUniqueActionChance();

        // ============================================================================
        // Armor Status Methods (Facade to ArmorStatusManager)
        // ============================================================================

        public double GetArmorSpikeDamage() => armorStatusManager.GetArmorSpikeDamage();
        public List<ArmorStatus> GetEquippedArmorStatuses() => armorStatusManager.GetAllArmorStatuses();
        public bool HasAutoSuccess() => bonusCalculator.HasAutoSuccess();

        // ============================================================================
        // Equipment Action Methods (Facade to EquipmentActionProvider)
        // ============================================================================

        public List<string> GetGearActions(Item gear) => actionProvider.GetGearActions(gear);
        public List<string> GetAllEquippedGearActions() => actionProvider.GetAllEquippedGearActions();
    }
}
