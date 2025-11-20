using System;
using System.Collections.Generic;
using System.Linq;

namespace RPGGame
{
    /// <summary>
    /// Manages armor statuses and effects from equipped armor pieces.
    /// Handles armor spike damage, armor special effects, and armor status aggregation.
    /// </summary>
    public class ArmorStatusManager
    {
        private readonly EquipmentSlotManager slots;

        public ArmorStatusManager(EquipmentSlotManager slots)
        {
            this.slots = slots;
        }

        /// <summary>
        /// Gets the total spike damage from armor-based spikes.
        /// </summary>
        public double GetArmorSpikeDamage()
        {
            double totalSpikeDamage = 0.0;

            var equippedItems = slots.GetEquippedItems();
            foreach (var item in equippedItems)
            {
                if (item != null)
                {
                    foreach (var armorStatus in item.ArmorStatuses)
                    {
                        if (armorStatus.Effect == "armorSpikes")
                        {
                            totalSpikeDamage += armorStatus.Value;
                        }
                    }
                }
            }

            return totalSpikeDamage;
        }

        /// <summary>
        /// Gets all armor statuses from equipped items.
        /// </summary>
        public List<ArmorStatus> GetAllArmorStatuses()
        {
            var allStatuses = new List<ArmorStatus>();

            var equippedItems = slots.GetEquippedItems();
            foreach (var item in equippedItems)
            {
                if (item != null)
                {
                    allStatuses.AddRange(item.ArmorStatuses);
                }
            }

            return allStatuses;
        }

        /// <summary>
        /// Gets armor statuses of a specific effect type.
        /// </summary>
        /// <param name="effectType">The effect type to filter by</param>
        /// <returns>List of matching armor statuses</returns>
        public List<ArmorStatus> GetStatusesByEffect(string effectType)
        {
            return GetAllArmorStatuses()
                .Where(s => s.Effect == effectType)
                .ToList();
        }

        /// <summary>
        /// Gets the total value of a specific armor status effect.
        /// </summary>
        /// <param name="effectType">The effect type</param>
        /// <returns>The total value of that effect</returns>
        public double GetStatusEffectValue(string effectType)
        {
            return GetStatusesByEffect(effectType)
                .Sum(s => s.Value);
        }

        /// <summary>
        /// Gets a count of a specific armor status effect.
        /// </summary>
        /// <param name="effectType">The effect type</param>
        /// <returns>The count of that effect</returns>
        public int GetStatusEffectCount(string effectType)
        {
            return GetStatusesByEffect(effectType).Count;
        }

        /// <summary>
        /// Checks if a specific armor status effect is active.
        /// </summary>
        /// <param name="effectType">The effect type to check</param>
        /// <returns>True if at least one of this effect is active</returns>
        public bool HasArmorStatus(string effectType)
        {
            return GetStatusesByEffect(effectType).Count > 0;
        }
    }
}

