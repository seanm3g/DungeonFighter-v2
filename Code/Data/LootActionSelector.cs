using System;
using System.Collections.Generic;
using System.Linq;

namespace RPGGame
{
    /// <summary>
    /// Selects actions for items based on weapon type and context
    /// Implements 80/20 split: 80% weapon-appropriate actions, 20% cross-contamination (any action)
    ///
    /// Example: A Wand gets a Wizard action 80% of the time (Arcane Blast, Fireball)
    ///          and a random action from any table 20% of the time (could get Shield Bash!)
    /// </summary>
    public class LootActionSelector
    {
        private readonly Random _random;
        private readonly ActionTables _tables;

        public LootActionSelector(Random random)
        {
            _random = random ?? new Random();
            _tables = LootTableLoader.GetActionTables();
        }

        /// <summary>
        /// Main entry point - selects an action for an item based on its type and context
        /// </summary>
        public string? SelectAction(LootContext? context, Item item)
        {
            // For weapons, use weapon-type-based selection
            if (item is WeaponItem weapon)
            {
                return SelectWeaponAction(weapon.WeaponType.ToString());
            }

            // For armor (Head, Feet, Chest items), use armor action pool
            if (item is HeadItem or FeetItem or ChestItem)
            {
                return SelectArmorAction();
            }

            return null;
        }

        /// <summary>
        /// Selects an action for a weapon based on its type
        /// Implements 80/20 split: 80% from weapon's class table, 20% any table
        /// </summary>
        public string? SelectWeaponAction(string weaponType)
        {
            // Determine if we do cross-contamination (20% chance)
            if (_random.NextDouble() < _tables.CrossContaminationChance)
            {
                // 20% chance: select any action from any table
                return SelectRandomActionFromAnyTable();
            }

            // 80% chance: select from the weapon-appropriate table
            var classTable = _tables.GetTableForWeapon(weaponType);
            if (classTable != null && classTable.Actions.Count > 0)
            {
                return SelectByWeight(classTable.Actions);
            }

            // Fallback: if no weapon-specific table, try any table
            return SelectRandomActionFromAnyTable();
        }

        /// <summary>
        /// Selects an action from the armor action pool
        /// </summary>
        public string? SelectArmorAction()
        {
            if (_tables.ArmorActions.Count == 0)
                return null;

            return SelectByWeight(_tables.ArmorActions);
        }

        /// <summary>
        /// Selects a random action from ALL available tables (for cross-contamination)
        /// This is called 20% of the time for weapons
        /// </summary>
        public string? SelectRandomActionFromAnyTable()
        {
            var allActions = new List<ActionTableEntry>();

            // Collect all actions from all class tables
            foreach (var classTable in _tables.ClassTables.Values)
            {
                allActions.AddRange(classTable.Actions);
            }

            // Include armor actions
            allActions.AddRange(_tables.ArmorActions);

            if (allActions.Count == 0)
                return null;

            // Use weighted selection
            return SelectByWeight(allActions);
        }

        /// <summary>
        /// Gets the class table for a specific weapon type
        /// Useful for understanding which actions are "normal" for a weapon
        /// </summary>
        public ClassActionTable? GetClassTableForWeapon(string weaponType)
        {
            return _tables.GetTableForWeapon(weaponType);
        }

        /// <summary>
        /// Weighted random selection from a list of actions
        /// Higher weight = higher probability of selection
        /// </summary>
        private string? SelectByWeight(List<ActionTableEntry> actions)
        {
            int totalWeight = actions.Sum(x => x.Weight);
            if (totalWeight <= 0)
                return actions.Count > 0 ? actions[0].Name : null;

            int roll = _random.Next(totalWeight);

            int accumulated = 0;
            foreach (var action in actions)
            {
                accumulated += action.Weight;
                if (roll < accumulated)
                {
                    return action.Name;
                }
            }

            // Fallback (shouldn't happen)
            return actions.Count > 0 ? actions[0].Name : null;
        }

        /// <summary>
        /// Gets all available actions for a weapon type
        /// Useful for UI/information purposes
        /// </summary>
        public List<string> GetAvailableActionsForWeapon(string weaponType)
        {
            var classTable = _tables.GetTableForWeapon(weaponType);
            if (classTable == null)
                return new List<string>();

            return classTable.Actions.Select(a => a.Name).ToList();
        }

        /// <summary>
        /// Gets all available armor actions
        /// </summary>
        public List<string> GetAvailableArmorActions()
        {
            return _tables.ArmorActions.Select(a => a.Name).ToList();
        }
    }
}
