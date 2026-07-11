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

        public Item? Legs
        {
            get => slotManager.Legs;
            set => slotManager.Legs = value;
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

        /// <summary>Catalog / material stats only (no <see cref="Item.StatBonuses"/> suffix lines).</summary>
        public int GetFlatEquipmentStatExcludingSuffixes(string statType) =>
            bonusCalculator.GetFlatStatBonusExcludingSuffixes(statType);

        /// <summary>Catalog / material stats only; includes <see cref="Item.CatalogAttackSpeed"/> for attack speed.</summary>
        public double GetFlatEquipmentStatExcludingSuffixesDouble(string statType) =>
            bonusCalculator.GetFlatStatBonusExcludingSuffixesDouble(statType);

        /// <param name="character">Pass the owning hero so suffix values apply as % of reference; omit for flat suffix sum (legacy).</param>
        public int GetEquipmentStatBonus(string statType, Character? character) =>
            bonusCalculator.GetStatBonus(statType, character);

        /// <param name="character">Pass the owning hero so suffix values apply as % of reference; omit for flat suffix sum (legacy).</param>
        public double GetEquipmentStatBonusDouble(string statType, Character? character) =>
            bonusCalculator.GetStatBonusDouble(statType, character);

        public int GetEquipmentDamageBonus(Character? character) => bonusCalculator.GetDamageBonus(character);
        public int GetEquipmentHealthBonus(Character? character) => bonusCalculator.GetHealthBonus(character);
        public int GetEquipmentRollBonus(Character? character) => bonusCalculator.GetRollBonus(character);
        public int GetMagicFind(Character? character)
        {
            int itemBonus = bonusCalculator.GetStatBonus("MagicFind", character);
            return itemBonus + modificationCalculator.GetMagicFind();
        }

        public double GetEquipmentAttackSpeedBonus(Character? character) => bonusCalculator.GetAttackSpeedBonus(character);
        public int GetEquipmentHealthRegenBonus(Character? character) => bonusCalculator.GetHealthRegenBonus(character);
        public int GetTotalArmor(Character? character) => bonusCalculator.GetTotalArmor(character);
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
        public int GetWeaponBleedPerHit() => modificationCalculator.GetWeaponBleedPerHit();
        public double GetWeaponPoisonPercentPerHit() => modificationCalculator.GetWeaponPoisonPercentPerHit();
        public int GetWeaponBurnPerHit() => modificationCalculator.GetWeaponBurnPerHit();
        public int GetWeaponAcidPerHit() => modificationCalculator.GetWeaponAcidPerHit();
        public double GetModificationFreezeChance() => modificationCalculator.GetFreezeChance();
        public double GetModificationStunChance() => modificationCalculator.GetStunChance();
        public double GetModificationUniqueActionChance() => modificationCalculator.GetUniqueActionChance();
        public List<string> GetModificationStatusEffects() => modificationCalculator.GetModificationStatusEffects();

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
