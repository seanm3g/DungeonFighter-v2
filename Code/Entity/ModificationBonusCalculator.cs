using System;
using System.Collections.Generic;
using System.Linq;

namespace RPGGame
{
    /// <summary>
    /// Calculates all modification-related bonuses from equipped items.
    /// Handles damage, speed, lifesteal, godlike, bleed chance, unique action chance, etc.
    /// </summary>
    public class ModificationBonusCalculator
    {
        private readonly EquipmentSlotManager slots;

        public ModificationBonusCalculator(EquipmentSlotManager slots)
        {
            this.slots = slots;
        }

        /// <summary>
        /// Gets magic find bonus from modifications.
        /// </summary>
        public int GetMagicFind() => GetModificationBonusInt("magicFind");

        /// <summary>
        /// Gets roll bonus from modifications.
        /// </summary>
        public int GetRollBonus() => GetModificationBonusInt("rollBonus");

        /// <summary>
        /// Gets damage bonus from modifications.
        /// </summary>
        public int GetDamageBonus() => GetModificationBonusInt("damage");

        /// <summary>
        /// Gets speed multiplier from modifications (multiplicative).
        /// </summary>
        public double GetSpeedMultiplier()
        {
            double totalMultiplier = 1.0;

            var equippedItems = slots.GetEquippedItems();
            foreach (var item in equippedItems)
            {
                if (item != null)
                {
                    foreach (var modification in item.Modifications)
                    {
                        if (modification.Effect == "speedMultiplier")
                        {
                            totalMultiplier *= modification.RolledValue;
                        }
                    }
                }
            }

            return totalMultiplier;
        }

        /// <summary>
        /// Gets damage multiplier from modifications (multiplicative).
        /// </summary>
        public double GetDamageMultiplier()
        {
            double totalMultiplier = 1.0;

            var equippedItems = slots.GetEquippedItems();
            foreach (var item in equippedItems)
            {
                if (item != null)
                {
                    foreach (var modification in item.Modifications)
                    {
                        if (modification.Effect == "damageMultiplier")
                        {
                            totalMultiplier *= modification.RolledValue;
                        }
                    }
                }
            }

            return totalMultiplier;
        }

        /// <summary>
        /// Gets lifesteal bonus from modifications (additive).
        /// </summary>
        public double GetLifesteal()
        {
            double totalLifesteal = 0.0;

            var equippedItems = slots.GetEquippedItems();
            foreach (var item in equippedItems)
            {
                if (item != null)
                {
                    foreach (var modification in item.Modifications)
                    {
                        if (modification.Effect == "lifesteal")
                        {
                            totalLifesteal += modification.RolledValue;
                        }
                    }
                }
            }

            return totalLifesteal;
        }

        /// <summary>
        /// Gets godlike bonus from modifications.
        /// </summary>
        public int GetGodlikeBonus() => GetModificationBonusInt("godlike");

        /// <summary>
        /// Gets bleed chance from modifications.
        /// </summary>
        public double GetBleedChance()
        {
            double totalBleedChance = 0.0;

            var equippedItems = slots.GetEquippedItems();
            foreach (var item in equippedItems)
            {
                if (item != null)
                {
                    foreach (var modification in item.Modifications)
                    {
                        if (modification.Effect == "bleedChance")
                        {
                            totalBleedChance += modification.RolledValue;
                        }
                    }
                }
            }

            return totalBleedChance;
        }

        /// <summary>
        /// Gets unique action chance from modifications.
        /// </summary>
        public double GetUniqueActionChance()
        {
            double totalUniqueActionChance = 0.0;

            var equippedItems = slots.GetEquippedItems();
            foreach (var item in equippedItems)
            {
                if (item != null)
                {
                    foreach (var modification in item.Modifications)
                    {
                        if (modification.Effect == "uniqueActionChance")
                        {
                            totalUniqueActionChance += modification.RolledValue;
                        }
                    }
                }
            }

            return totalUniqueActionChance;
        }

        /// <summary>
        /// Gets all active modifications from equipped items.
        /// </summary>
        /// <returns>List of all modifications</returns>
        public List<Modification> GetAllModifications()
        {
            var allModifications = new List<Modification>();

            var equippedItems = slots.GetEquippedItems();
            foreach (var item in equippedItems)
            {
                if (item != null)
                {
                    allModifications.AddRange(item.Modifications);
                }
            }

            return allModifications;
        }

        /// <summary>
        /// Gets modifications of a specific effect type.
        /// </summary>
        /// <param name="effectType">The effect type to filter by</param>
        /// <returns>List of matching modifications</returns>
        public List<Modification> GetModificationsByType(string effectType)
        {
            return GetAllModifications()
                .Where(m => m.Effect == effectType)
                .ToList();
        }

        /// <summary>
        /// Gets a modification bonus as an integer (additive).
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
    }
}

