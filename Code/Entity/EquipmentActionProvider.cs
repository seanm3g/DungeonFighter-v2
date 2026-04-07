using System.Collections.Generic;
using System.Linq;

namespace RPGGame
{
    /// <summary>
    /// Manages and provides actions from equipped gear (weapons and armor).
    /// Delegates name resolution to <see cref="GearActionNames"/> so inventory display matches the action pool.
    /// </summary>
    public class EquipmentActionProvider
    {
        private readonly EquipmentSlotManager slots;

        public EquipmentActionProvider(EquipmentSlotManager slots)
        {
            this.slots = slots;
        }

        /// <summary>
        /// Gets all available actions from a piece of equipment.
        /// </summary>
        /// <param name="gear">The equipment piece to get actions from</param>
        /// <returns>List of action names from this gear</returns>
        public List<string> GetGearActions(Item gear)
        {
            return GearActionNames.Resolve(gear);
        }

        /// <summary>
        /// Gets all actions from all equipped gear.
        /// </summary>
        public List<string> GetAllEquippedGearActions()
        {
            var allActions = new List<string>();

            var equippedItems = slots.GetEquippedItems();
            foreach (var item in equippedItems)
            {
                if (item != null)
                {
                    allActions.AddRange(GetGearActions(item));
                }
            }

            return allActions.Distinct().ToList();
        }
    }
}
