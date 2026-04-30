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
        /// <summary>Chance to bypass Category filter when selecting actions for items (any category possible).</summary>
        private const double CategoryBypassChance = 0.05;

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
        /// Falls back to tag-based matching if ActionTables.json doesn't have the weapon type
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

            // Fallback: if no weapon-specific table, try tag-based matching from Actions.json
            var tagBasedActions = GetTagBasedWeaponActions(weaponType);
            if (tagBasedActions.Count > 0)
            {
                // Randomly select one from the tag-based actions
                return tagBasedActions[_random.Next(tagBasedActions.Count)];
            }

            // Final fallback: if no weapon-specific actions found, try any table
            return SelectRandomActionFromAnyTable();
        }

        /// <summary>
        /// Selects an action for a starter weapon based on its type
        /// Starter weapons are exempt from the 20% random action chance - always uses weapon-appropriate actions
        /// </summary>
        public string? SelectWeaponActionForStarter(string weaponType)
        {
            // Starter weapons always get actions from their weapon-appropriate table (no 20% random chance)
            var classTable = _tables.GetTableForWeapon(weaponType);
            if (classTable != null && classTable.Actions.Count > 0)
            {
                return SelectByWeight(classTable.Actions);
            }

            // Fallback: if no weapon-specific table, try tag-based matching from Actions.json
            var tagBasedActions = GetTagBasedWeaponActions(weaponType);
            if (tagBasedActions.Count > 0)
            {
                // Randomly select one from the tag-based actions
                return tagBasedActions[_random.Next(tagBasedActions.Count)];
            }

            // Final fallback: return null if no weapon-specific actions found (starter weapons shouldn't get random actions)
            return null;
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

            if (allActions.Count > 0)
            {
                // Use weighted selection
                return SelectByWeight(allActions);
            }

            // Fallback to tag-based actions from Actions.json
            var allTagBasedActions = GetAllTagBasedActions();
            if (allTagBasedActions.Count > 0)
            {
                return allTagBasedActions[_random.Next(allTagBasedActions.Count)];
            }

            return null;
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
        /// Gets weapon actions from Actions.json using new WeaponTypes property or tag-based matching.
        /// Category filter: only include actions whose Category is appropriate for weapons (or empty); small % chance to bypass.
        /// </summary>
        private List<string> GetTagBasedWeaponActions(string weaponType)
        {
            var weaponTag = weaponType.ToLower();
            var weaponTypeName = weaponType; // Keep original case for WeaponTypes property
            var allActionData = ActionLoader.GetAllActionData();

            var weaponActions = new List<string>();

            // First, check for actions assigned via WeaponTypes property
            foreach (var actionData in allActionData)
            {
                // Check if action is assigned to this weapon type via WeaponTypes property
                if (actionData.WeaponTypes != null && 
                    actionData.WeaponTypes.Any(wt => wt.Equals(weaponTypeName, StringComparison.OrdinalIgnoreCase)))
                {
                    weaponActions.Add(actionData.Name);
                }
            }

            // If no actions found via WeaponTypes, fall back to tag-based matching
            if (weaponActions.Count == 0)
            {
                var allActions = ActionLoader.GetAllActions();
                // Get weapon-specific actions from JSON using tag matching
                // Actions must have both "weapon" tag and the weapon type tag (e.g., "wand", "mace")
                // Exclude "class" tagged actions (class-only actions cannot appear on weapons)
                weaponActions = allActions
                    .Where(action => action.Tags != null &&
                                    action.Tags.Any(tag => tag.Equals("weapon", StringComparison.OrdinalIgnoreCase)) &&
                                    action.Tags.Any(tag => tag.Equals(weaponTag, StringComparison.OrdinalIgnoreCase)) &&
                                    !action.Tags.Any(tag => tag.Equals("unique", StringComparison.OrdinalIgnoreCase)) &&
                                    !action.Tags.Any(tag => tag.Equals("enemy", StringComparison.OrdinalIgnoreCase)) &&
                                    !action.Tags.Any(tag => tag.Equals("class", StringComparison.OrdinalIgnoreCase)))
                    .Select(action => action.Name)
                    .ToList();
            }

            // Category filter: exclude actions whose Category is not appropriate for this item type (e.g. class-only on weapons)
            // Small % chance to bypass so any category can appear
            if (weaponActions.Count > 0 && _random.NextDouble() >= CategoryBypassChance)
            {
                var nameToData = allActionData.Where(a => !string.IsNullOrEmpty(a.Name)).ToDictionary(a => a.Name, a => a, StringComparer.OrdinalIgnoreCase);
                var filtered = weaponActions.Where(name =>
                {
                    if (!nameToData.TryGetValue(name, out var data)) return true;
                    var cat = (data.Category ?? "").Trim();
                    if (string.IsNullOrEmpty(cat)) return true;
                    return cat.Equals("Weapon", StringComparison.OrdinalIgnoreCase) ||
                           cat.Equals(weaponTypeName, StringComparison.OrdinalIgnoreCase) ||
                           cat.Equals(weaponTag, StringComparison.OrdinalIgnoreCase);
                }).ToList();
                if (filtered.Count > 0)
                    weaponActions = filtered;
            }

            return weaponActions;
        }

        /// <summary>
        /// Gets all actions from Actions.json using tag-based matching
        /// Used as fallback when ActionTables.json is empty or incomplete
        /// </summary>
        private List<string> GetAllTagBasedActions()
        {
            var allActions = ActionLoader.GetAllActions();

            // Get all weapon actions (not unique or enemy/environment specific)
            // Exclude "class" tagged actions (class-only actions cannot appear on weapons)
            var weaponActions = allActions
                .Where(action => action.Tags != null &&
                                action.Tags.Any(tag => tag.Equals("weapon", StringComparison.OrdinalIgnoreCase)) &&
                                !action.Tags.Any(tag => tag.Equals("unique", StringComparison.OrdinalIgnoreCase)) &&
                                !action.Tags.Any(tag => tag.Equals("enemy", StringComparison.OrdinalIgnoreCase)) &&
                                !action.Tags.Any(tag => tag.Equals("environment", StringComparison.OrdinalIgnoreCase)) &&
                                !action.Tags.Any(tag => tag.Equals("class", StringComparison.OrdinalIgnoreCase)))
                .Select(action => action.Name)
                .ToList();

            return weaponActions;
        }

        /// <summary>
        /// Gets all available actions for a weapon type
        /// Useful for UI/information purposes
        /// </summary>
        public List<string> GetAvailableActionsForWeapon(string weaponType)
        {
            var classTable = _tables.GetTableForWeapon(weaponType);
            if (classTable != null && classTable.Actions.Count > 0)
            {
                return classTable.Actions.Select(a => a.Name).ToList();
            }

            // Fallback to tag-based actions
            return GetTagBasedWeaponActions(weaponType);
        }

        /// <summary>
        /// Gets all available armor actions
        /// </summary>
        public List<string> GetAvailableArmorActions()
        {
            return _tables.ArmorActions.Select(a => a.Name).ToList();
        }

        /// <summary>
        /// Gets starting weapon actions from Actions.json using the "startingWeapon" tag
        /// Returns all actions with "startingWeapon" tag that match the weapon type
        /// </summary>
        public List<string> GetStartingWeaponActions(string weaponType)
        {
            var weaponTag = weaponType.ToLower();
            var allActions = ActionLoader.GetAllActions();

            // Get all actions with "startingWeapon" tag that match the weapon type
            var startingActions = allActions
                .Where(action => action.Tags != null &&
                                action.Tags.Any(tag => tag.Equals("startingWeapon", StringComparison.OrdinalIgnoreCase)) &&
                                action.Tags.Any(tag => tag.Equals(weaponTag, StringComparison.OrdinalIgnoreCase)))
                .Select(action => action.Name)
                .ToList();

            return startingActions;
        }
    }
}
