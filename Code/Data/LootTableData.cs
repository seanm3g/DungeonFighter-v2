using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace RPGGame
{
    /// <summary>
    /// Entry in a modification table
    /// Maps a modification name to dice results that should trigger it (for contextual biasing)
    /// </summary>
    public class ModificationTableEntry
    {
        /// <summary>
        /// The modification name (e.g., "Burning", "Verdant", "Cursed")
        /// </summary>
        [JsonPropertyName("name")]
        public string Name { get; set; } = "";

        /// <summary>
        /// Dice result values that should favor this modification
        /// When rolling for modifications, if any of these results appear, this mod is favored (70% bias)
        /// Example: [5, 9, 13] means results of 5, 9, or 13 favor this modification
        /// </summary>
        [JsonPropertyName("diceResults")]
        public List<int> DiceResults { get; set; } = new();

        /// <summary>
        /// Weight for weighted random selection among multiple matching modifications
        /// Higher weight = more likely to be selected
        /// </summary>
        [JsonPropertyName("weight")]
        public int Weight { get; set; } = 1;
    }

    /// <summary>
    /// Entry in an action table
    /// Represents an action that can be applied to an item
    /// </summary>
    public class ActionTableEntry
    {
        /// <summary>
        /// The action name (e.g., "ARCANE BLAST", "PRECISION STRIKE")
        /// </summary>
        [JsonPropertyName("name")]
        public string Name { get; set; } = "";

        /// <summary>
        /// Weight for weighted random selection among actions in the same table
        /// Higher weight = more likely to be selected
        /// </summary>
        [JsonPropertyName("weight")]
        public int Weight { get; set; } = 1;
    }

    /// <summary>
    /// Action table for a specific class (Wizard, Warrior, Rogue, Barbarian)
    /// Maps weapon types to their appropriate actions
    /// </summary>
    public class ClassActionTable
    {
        /// <summary>
        /// The weapon type this table applies to (Wand, Sword, Dagger, Mace, etc.)
        /// </summary>
        [JsonPropertyName("weaponType")]
        public string WeaponType { get; set; } = "";

        /// <summary>
        /// List of actions that can be found on this weapon type
        /// </summary>
        [JsonPropertyName("actions")]
        public List<ActionTableEntry> Actions { get; set; } = new();
    }

    /// <summary>
    /// Container for all modification tables (theme-based and archetype-based)
    /// </summary>
    public class ModificationTables
    {
        /// <summary>
        /// Modifications organized by dungeon theme
        /// Key: Theme name (Forest, Lava, Crypt, etc.)
        /// Value: List of modifications that appear in that theme
        /// </summary>
        [JsonPropertyName("themeModifications")]
        public Dictionary<string, List<ModificationTableEntry>> ThemeModifications { get; set; } = new();

        /// <summary>
        /// Modifications organized by enemy archetype
        /// Key: Archetype name (Berserker, Guardian, Assassin, etc.)
        /// Value: List of modifications typical of that archetype
        /// </summary>
        [JsonPropertyName("archetypeModifications")]
        public Dictionary<string, List<ModificationTableEntry>> ArchetypeModifications { get; set; } = new();

        /// <summary>
        /// Generic modifications that appear in all contexts
        /// Used as fallback when no theme/archetype match
        /// </summary>
        [JsonPropertyName("genericModifications")]
        public List<ModificationTableEntry> GenericModifications { get; set; } = new();
    }

    /// <summary>
    /// Container for all action tables (organized by class and weapon type)
    /// </summary>
    public class ActionTables
    {
        /// <summary>
        /// Action tables organized by class name
        /// Key: Class name (Wizard, Warrior, Rogue, Barbarian)
        /// Value: ClassActionTable containing weapon type and actions
        /// </summary>
        [JsonPropertyName("classTables")]
        public Dictionary<string, ClassActionTable> ClassTables { get; set; } = new();

        /// <summary>
        /// Actions that can appear on armor (defensive/shield actions)
        /// Used for armor items that don't have weapon type-specific actions
        /// </summary>
        [JsonPropertyName("armorActions")]
        public List<ActionTableEntry> ArmorActions { get; set; } = new();

        /// <summary>
        /// Probability of selecting an action from a non-matching table (cross-contamination)
        /// Default: 0.20 (20% chance to get an action from ANY table instead of the weapon's table)
        /// Example: 20% chance a Wand gets a Shield Bash instead of an Arcane action
        /// </summary>
        [JsonPropertyName("crossContaminationChance")]
        public double CrossContaminationChance { get; set; } = 0.20;

        /// <summary>
        /// Helper method to get action table for a specific weapon type
        /// Returns the appropriate class table for the weapon
        /// </summary>
        public ClassActionTable? GetTableForWeapon(string weaponType)
        {
            // Find the class table where weaponType matches
            foreach (var classTable in ClassTables.Values)
            {
                if (classTable.WeaponType.Equals(weaponType, StringComparison.OrdinalIgnoreCase))
                {
                    return classTable;
                }
            }
            return null;
        }

        /// <summary>
        /// Gets a random action from any table (for cross-contamination)
        /// </summary>
        public ActionTableEntry? GetRandomActionFromAnyTable(Random random)
        {
            var allActions = new List<ActionTableEntry>();

            foreach (var classTable in ClassTables.Values)
            {
                allActions.AddRange(classTable.Actions);
            }

            if (allActions.Count == 0)
                return null;

            return allActions[random.Next(allActions.Count)];
        }
    }
}
