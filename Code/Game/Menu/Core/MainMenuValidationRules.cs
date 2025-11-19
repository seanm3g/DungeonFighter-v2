using RPGGame;

namespace DungeonFighter.Game.Menu.Core
{
    /// <summary>
    /// Validation rules for the Main Menu.
    /// Valid inputs: "1" (New Game), "2" (Load Game), "3" (Settings), "0" (Exit)
    /// </summary>
    public class MainMenuValidationRules : IValidationRules
    {
        public ValidationResult Validate(string input)
        {
            string cleaned = input.Trim();

            if (cleaned.Length != 1)
                return ValidationResult.Invalid("Please enter a single digit (0, 1, 2, or 3)");

            if (!"0123".Contains(cleaned))
                return ValidationResult.Invalid("Invalid choice. Enter 1 (New Game), 2 (Load), 3 (Settings), or 0 (Exit)");

            return ValidationResult.Valid();
        }
    }
}

