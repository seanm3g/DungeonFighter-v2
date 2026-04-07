using RPGGame;

namespace DungeonFighter.Game.Menu.Core
{
    /// <summary>
    /// Validation rules for Inventory menu.
    /// Valid inputs: 1-4 (actions), 0 (return to game menu), h (help)
    /// </summary>
    public class InventoryValidationRules : IValidationRules
    {
        public ValidationResult Validate(string input)
        {
            string cleaned = input.Trim();

            if (string.IsNullOrEmpty(cleaned))
                return ValidationResult.Invalid("Please enter an inventory option (1-4 or 0)");

            if (cleaned.Length != 1)
                return ValidationResult.Invalid("Please enter a single digit");

            if ("1234h0".Contains(cleaned))
                return ValidationResult.Valid();

            return ValidationResult.Invalid("Invalid option. Enter 1-4 for actions, 'h' for help, or '0' to return to game menu");
        }
    }
}

