namespace DungeonFighter.Game.Menu.Core
{
    /// <summary>
    /// Interface for menu-specific validation rules.
    /// Defines the contract for implementing validation logic for different menu types.
    /// Uses the Strategy Pattern to allow different validation rules per menu.
    /// </summary>
    public interface IValidationRules
    {
        /// <summary>
        /// Validates the given input string.
        /// </summary>
        /// <param name="input">The input string to validate</param>
        /// <returns>Result of the validation</returns>
        ValidationResult Validate(string input);
    }
}

