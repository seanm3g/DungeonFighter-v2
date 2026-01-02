using System;

namespace RPGGame.Data.Validation
{
    /// <summary>
    /// Standalone runner for data validation
    /// Can be called from test suite, game startup, or development tools
    /// </summary>
    public static class ValidationRunner
    {
        /// <summary>
        /// Runs validation and prints formatted report to console
        /// </summary>
        /// <returns>True if validation passed (no errors), false otherwise</returns>
        public static bool RunValidation()
        {
            Console.WriteLine("=== Running Data Quality Validation ===\n");

            var validator = new GameDataValidator();
            var result = validator.ValidateAll();

            result.PrintReport();

            return result.IsValid;
        }

        /// <summary>
        /// Runs validation and returns the result without printing
        /// </summary>
        /// <returns>Validation result</returns>
        public static ValidationResult RunValidationSilent()
        {
            var validator = new GameDataValidator();
            return validator.ValidateAll();
        }

        /// <summary>
        /// Runs validation and returns summary string
        /// </summary>
        /// <returns>Summary of validation results</returns>
        public static string RunValidationGetSummary()
        {
            var validator = new GameDataValidator();
            var result = validator.ValidateAll();
            return result.GetSummary();
        }
    }
}
