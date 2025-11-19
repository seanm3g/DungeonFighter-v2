namespace DungeonFighter.Game.Menu.Core
{
    /// <summary>
    /// Represents the result of input validation.
    /// Provides a consistent way to communicate validation success/failure.
    /// </summary>
    public class ValidationResult
    {
        /// <summary>
        /// Gets whether the validation passed.
        /// </summary>
        public bool IsValid { get; private set; }

        /// <summary>
        /// Gets the error message if validation failed.
        /// Null if validation passed.
        /// </summary>
        public string? Error { get; private set; }

        /// <summary>
        /// Private constructor - use factory methods instead
        /// </summary>
        private ValidationResult(bool isValid, string? error = null)
        {
            IsValid = isValid;
            Error = error;
        }

        /// <summary>
        /// Factory method for successful validation.
        /// </summary>
        public static ValidationResult Valid()
        {
            return new ValidationResult(isValid: true);
        }

        /// <summary>
        /// Factory method for failed validation with error message.
        /// </summary>
        public static ValidationResult Invalid(string errorMessage)
        {
            return new ValidationResult(isValid: false, error: errorMessage);
        }
    }
}

