using RPGGame;

namespace DungeonFighter.Game.Menu.Core
{
    /// <summary>
    /// Validation rules for Weapon Selection menu.
    /// Valid inputs: weapon numbers and action keys
    /// </summary>
    public class WeaponSelectionValidationRules : IValidationRules
    {
        private int weaponCount;

        public WeaponSelectionValidationRules(int count = 4)
        {
            weaponCount = count;
        }

        public ValidationResult Validate(string input)
        {
            string cleaned = input.Trim();

            if (string.IsNullOrEmpty(cleaned))
                return ValidationResult.Invalid("Please enter a weapon number or command");

            // Check if it's a number
            if (int.TryParse(cleaned, out int weaponNum))
            {
                if (weaponNum < 1 || weaponNum > weaponCount)
                    return ValidationResult.Invalid($"Please select 1-{weaponCount}");
                return ValidationResult.Valid();
            }

            // Check for other valid inputs
            if (cleaned.Length == 1 && "ch".Contains(cleaned.ToLower()))
                return ValidationResult.Valid();

            return ValidationResult.Invalid($"Enter weapon number (1-{weaponCount}), 'c' to confirm, or 'h' for help");
        }
    }
}

