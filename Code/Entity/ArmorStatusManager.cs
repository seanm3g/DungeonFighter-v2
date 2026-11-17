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

    }
}

