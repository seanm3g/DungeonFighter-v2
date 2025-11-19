using RPGGame;

namespace DungeonFighter.Game.Menu.Core
{
    /// <summary>
    /// Validation rules for Inventory menu.
    /// Valid inputs: inventory action numbers 1-7
    /// </summary>
    public class InventoryValidationRules : IValidationRules
    {
        public ValidationResult Validate(string input)
        {
            string cleaned = input.Trim();

            if (string.IsNullOrEmpty(cleaned))
                return ValidationResult.Invalid("Please enter an inventory option (1-7)");

            if (cleaned.Length != 1)
                return ValidationResult.Invalid("Please enter a single digit");

            if ("1234567h0".Contains(cleaned))
                return ValidationResult.Valid();

            return ValidationResult.Invalid("Invalid option. Enter 1-7 for actions, 'h' for help, or '0' to return");
        }
    }
}

