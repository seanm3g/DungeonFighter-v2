using System;

namespace RPGGame
{
    /// <summary>
    /// Centralized configuration for basic gear names
    /// Eliminates duplication of hardcoded gear name arrays across multiple files
    /// </summary>
    public static class BasicGearConfig
    {
        /// <summary>
        /// Gets the list of basic gear names that should have no special actions
        /// </summary>
        /// <returns>Array of basic gear names</returns>
        public static string[] GetBasicGearNames()
        {
            return new string[] 
            { 
                "Leather Helmet", 
                "Leather Armor", 
                "Leather Boots", 
                "Cloth Hood", 
                "Cloth Robes", 
                "Cloth Shoes" 
            };
        }

        /// <summary>
        /// Checks if a gear item is basic starter gear
        /// </summary>
        /// <param name="gearName">The name of the gear item</param>
        /// <returns>True if the gear is basic starter gear</returns>
        public static bool IsBasicGear(string gearName)
        {
            if (string.IsNullOrEmpty(gearName))
                return false;

            var basicGearNames = GetBasicGearNames();
            return Array.Exists(basicGearNames, name => 
                string.Equals(name, gearName, StringComparison.OrdinalIgnoreCase));
        }

        /// <summary>
        /// Gets basic gear names for a specific type
        /// </summary>
        /// <param name="gearType">The type of gear (helmet, armor, boots, etc.)</param>
        /// <returns>Array of basic gear names for the specified type</returns>
        public static string[] GetBasicGearNamesByType(string gearType)
        {
            if (string.IsNullOrEmpty(gearType))
                return new string[0];

            var allBasicGear = GetBasicGearNames();
            return Array.FindAll(allBasicGear, name => 
                name.Contains(gearType, StringComparison.OrdinalIgnoreCase));
        }

        /// <summary>
        /// Gets basic gear names by material
        /// </summary>
        /// <param name="material">The material (leather, cloth, etc.)</param>
        /// <returns>Array of basic gear names for the specified material</returns>
        public static string[] GetBasicGearNamesByMaterial(string material)
        {
            if (string.IsNullOrEmpty(material))
                return new string[0];

            var allBasicGear = GetBasicGearNames();
            return Array.FindAll(allBasicGear, name => 
                name.Contains(material, StringComparison.OrdinalIgnoreCase));
        }

        /// <summary>
        /// Gets the count of basic gear items
        /// </summary>
        /// <returns>The total number of basic gear items</returns>
        public static int GetBasicGearCount()
        {
            return GetBasicGearNames().Length;
        }

        /// <summary>
        /// Gets basic gear names as a formatted string
        /// </summary>
        /// <param name="separator">The separator to use between names</param>
        /// <returns>Formatted string of basic gear names</returns>
        public static string GetBasicGearNamesString(string separator = ", ")
        {
            return string.Join(separator, GetBasicGearNames());
        }
    }
}
