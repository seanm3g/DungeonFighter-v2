using System.Collections.Generic;

namespace RPGGame
{
    /// <summary>
    /// Represents a single attribute requirement for an item
    /// Extensible system that supports current attributes (Strength, Agility, Technique, Intelligence)
    /// and future secondary attributes
    /// </summary>
    public class AttributeRequirement
    {
        /// <summary>
        /// The name of the attribute (e.g., "Strength", "Agility", "Technique", "Intelligence")
        /// </summary>
        public string AttributeName { get; set; } = "";

        /// <summary>
        /// The minimum value required for this attribute
        /// </summary>
        public int RequiredValue { get; set; } = 0;
    }

    /// <summary>
    /// Collection of attribute requirements for an item
    /// Uses dictionary for extensibility and easy lookup
    /// </summary>
    public class AttributeRequirements : Dictionary<string, int>
    {
        /// <summary>
        /// Creates an empty AttributeRequirements collection
        /// </summary>
        public AttributeRequirements() : base()
        {
        }

        /// <summary>
        /// Creates AttributeRequirements from a dictionary
        /// </summary>
        public AttributeRequirements(Dictionary<string, int> requirements) : base(requirements)
        {
        }

        /// <summary>
        /// Adds a requirement for a specific attribute
        /// </summary>
        public void AddRequirement(string attributeName, int requiredValue)
        {
            this[attributeName] = requiredValue;
        }

        /// <summary>
        /// Checks if this collection has any requirements
        /// </summary>
        public bool HasRequirements => Count > 0;
    }
}

