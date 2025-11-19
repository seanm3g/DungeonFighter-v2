using RPGGame;

namespace DungeonFighter.Game.Menu.Core
{
    /// <summary>
    /// Validation rules for Character Creation menu.
    /// Valid inputs: numbers for stat adjustments, letters for actions
    /// </summary>
    public class CharacterCreationValidationRules : IValidationRules
    {
        public ValidationResult Validate(string input)
        {
            string cleaned = input.Trim().ToLower();

            if (string.IsNullOrEmpty(cleaned))
                return ValidationResult.Invalid("Please enter a command");

            // Valid single character inputs for character creation
            if (cleaned.Length == 1)
            {
                if ("123456789rceh".Contains(cleaned))
                    return ValidationResult.Valid();

                return ValidationResult.Invalid("Invalid command. Use 1-9 for stats, 'r' for random, 'c' to confirm, 'e' for export, 'h' for help");
            }

            return ValidationResult.Invalid("Please enter a single character command");
        }
    }
}

