using RPGGame;

namespace DungeonFighter.Game.Menu.Core
{
    /// <summary>
    /// Validation rules for the Main Menu.
    /// Valid inputs: "1"-"9" (fighter card), "N" (New Game), "A" (All saved), "F" (Fighter Overview), "S" (Settings), "Q"/"0" (Exit), "C" (Characters)
    /// </summary>
    public class MainMenuValidationRules : IValidationRules
    {
        public ValidationResult Validate(string input)
        {
            string cleaned = input.Trim();

            if (cleaned.Length != 1)
                return ValidationResult.Invalid("Please enter a single key");

            if (!"0123456789nafsqc".Contains(char.ToLowerInvariant(cleaned[0])))
                return ValidationResult.Invalid("Invalid choice. Enter 1-9 (fighter), N (New Game), A (All saved), F (Fighter Overview), S (Settings), or Q (Exit)");

            return ValidationResult.Valid();
        }
    }
}

