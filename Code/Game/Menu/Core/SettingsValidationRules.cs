using RPGGame;

namespace DungeonFighter.Game.Menu.Core
{
    /// <summary>
    /// Validation rules for Settings menu.
    /// Valid inputs: setting option numbers and action keys
    /// </summary>
    public class SettingsValidationRules : IValidationRules
    {
        public ValidationResult Validate(string input)
        {
            string cleaned = input.Trim();

            if (string.IsNullOrEmpty(cleaned))
                return ValidationResult.Invalid("Please enter a settings option");

            if (cleaned.Length == 1)
            {
                if ("123456789hc0".Contains(cleaned.ToLower()))
                    return ValidationResult.Valid();
            }

            return ValidationResult.Invalid("Enter setting number, 'h' for help, 'c' to confirm, or '0' to return");
        }
    }
}

