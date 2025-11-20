using System;
using System.Collections.Generic;
using System.Linq;

namespace RPGGame
{
    /// <summary>
    /// Calculates stat bonuses provided by equipped items.
    /// Handles all stat bonus types: Damage, Health, RollBonus, Armor, AttackSpeed, etc.
    /// </summary>
    public class EquipmentBonusCalculator
    {
        private readonly EquipmentSlotManager slots;

        public EquipmentBonusCalculator(EquipmentSlotManager slots)
        {
            this.slots = slots;
        }

        /// <summary>
        /// Gets the bonus for a specific stat type from equipped items.
        /// </summary>
        /// <param name="statType">The stat type to calculate bonus for</param>
        /// <returns>The total bonus value as an integer</returns>
        public int GetStatBonus(string statType)
        {
            int totalBonus = 0;

            var equippedItems = slots.GetEquippedItems();
            foreach (var item in equippedItems)
            {
                if (item != null)
                {
                    foreach (var statBonus in item.StatBonuses)
                    {
                        if (statBonus.StatType == statType || statBonus.StatType == "ALL")
                        {
                            totalBonus += (int)statBonus.Value;
                        }
                    }
                }
            }

            return totalBonus;
        }

        /// <summary>
        /// Gets the bonus for a specific stat type from equipped items (as double).
        /// </summary>
        /// <param name="statType">The stat type to calculate bonus for</param>
        /// <returns>The total bonus value as a double</returns>
        public double GetStatBonusDouble(string statType)
        {
            double totalBonus = 0.0;

            var equippedItems = slots.GetEquippedItems();
            foreach (var item in equippedItems)
            {
                if (item != null)
                {
                    foreach (var statBonus in item.StatBonuses)
                    {
                        if (statBonus.StatType == statType || statBonus.StatType == "ALL")
                        {
                            totalBonus += statBonus.Value;
                        }
                    }
                }
            }

            return totalBonus;
        }

        /// <summary>
        /// Gets the damage bonus from equipped items.
        /// </summary>
        public int GetDamageBonus() => GetStatBonus("Damage");

        /// <summary>
        /// Gets the health bonus from equipped items.
        /// </summary>
        public int GetHealthBonus() => GetStatBonus("Health");

        /// <summary>
        /// Gets the roll bonus from equipped items.
        /// </summary>
        public int GetRollBonus() => GetStatBonus("RollBonus");

        /// <summary>
        /// Gets the magic find bonus from equipped items and modifications.
        /// </summary>
        public int GetMagicFind()
        {
            int itemBonus = GetStatBonus("MagicFind");
            int modBonus = GetModificationBonusInt("magicFind");
            return itemBonus + modBonus;
        }

        /// <summary>
        /// Gets the attack speed bonus from equipped items.
        /// </summary>
        public double GetAttackSpeedBonus() => GetStatBonusDouble("AttackSpeed");

        /// <summary>
        /// Gets the health regeneration bonus from equipped items.
        /// </summary>
        public int GetHealthRegenBonus() => GetStatBonus("HealthRegen");

        /// <summary>
        /// Calculates total armor from equipped armor pieces and stat bonuses.
        /// </summary>
        public int GetTotalArmor()
        {
            int totalArmor = 0;

            if (slots.Head is HeadItem head)
                totalArmor += head.GetTotalArmor();
            if (slots.Body is ChestItem chest)
                totalArmor += chest.GetTotalArmor();
            if (slots.Feet is FeetItem feet)
                totalArmor += feet.GetTotalArmor();

            totalArmor += GetStatBonus("Armor");

            return totalArmor;
        }

        /// <summary>
        /// Gets the total reroll charges from equipped items.
        /// </summary>
        public int GetTotalRerollCharges()
        {
            int totalRerolls = 0;

            var equippedItems = slots.GetEquippedItems();
            foreach (var item in equippedItems)
            {
                if (item != null)
                {
                    foreach (var modification in item.Modifications)
                    {
                        if (modification.Effect == "reroll")
                        {
                            totalRerolls++;
                        }
                    }
                }
            }

            return totalRerolls;
        }

        /// <summary>
        /// Gets a modification bonus as an integer.
        /// </summary>
        private int GetModificationBonusInt(string effectType)
        {
            int totalBonus = 0;

            var equippedItems = slots.GetEquippedItems();
            foreach (var item in equippedItems)
            {
                if (item != null)
                {
                    foreach (var modification in item.Modifications)
                    {
                        if (modification.Effect == effectType)
                        {
                            totalBonus += (int)modification.RolledValue;
                        }
                    }
                }
            }

            return totalBonus;
        }

        /// <summary>
        /// Checks if character has auto-success modification.
        /// </summary>
        public bool HasAutoSuccess()
        {
            var equippedItems = slots.GetEquippedItems();
            foreach (var item in equippedItems)
            {
                if (item != null)
                {
                    foreach (var modification in item.Modifications)
                    {
                        if (modification.Effect == "autoSuccess")
                        {
                            return true;
                        }
                    }
                }
            }

            return false;
        }
    }
}

