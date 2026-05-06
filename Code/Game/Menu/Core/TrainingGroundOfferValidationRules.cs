namespace DungeonFighter.Game.Menu.Core
{
    /// <summary>Valid inputs for the pre-weapon Training Ground offer (1 = enter, 2 = skip).</summary>
    public class TrainingGroundOfferValidationRules : IValidationRules
    {
        public ValidationResult Validate(string input)
        {
            string cleaned = input?.Trim() ?? "";
            if (string.IsNullOrEmpty(cleaned))
                return ValidationResult.Invalid("Please enter 1 or 2");

            if (cleaned == "1" || cleaned == "2")
                return ValidationResult.Valid();

            return ValidationResult.Invalid("Invalid choice. Press 1 to enter Training Ground, or 2 to skip.");
        }
    }
}
