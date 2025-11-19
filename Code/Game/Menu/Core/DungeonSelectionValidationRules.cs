using RPGGame;

namespace DungeonFighter.Game.Menu.Core
{
    /// <summary>
    /// Validation rules for Dungeon Selection menu.
    /// Valid inputs: dungeon numbers and action keys
    /// </summary>
    public class DungeonSelectionValidationRules : IValidationRules
    {
        private int dungeonCount;

        public DungeonSelectionValidationRules(int count = 10)
        {
            dungeonCount = count;
        }

        public ValidationResult Validate(string input)
        {
            string cleaned = input.Trim();

            if (string.IsNullOrEmpty(cleaned))
                return ValidationResult.Invalid($"Please select a dungeon (1-{dungeonCount})");

            // Check if it's a number
            if (int.TryParse(cleaned, out int dungeonNum))
            {
                if (dungeonNum < 1 || dungeonNum > dungeonCount)
                    return ValidationResult.Invalid($"Please select 1-{dungeonCount}");
                return ValidationResult.Valid();
            }

            // Check for other valid inputs
            if (cleaned.Length == 1 && "ch".Contains(cleaned.ToLower()))
                return ValidationResult.Valid();

            return ValidationResult.Invalid($"Enter dungeon (1-{dungeonCount}), 'c' to confirm, 'h' for help, or '0' to return");
        }
    }
}

