using System;
using System.Collections.Generic;
using RPGGame;
using DungeonFighter.Game.Menu.Core;

namespace DungeonFighter.Game.Menu.Routing
{
    /// <summary>
    /// Centralized input validator for all menus.
    /// Validates input based on game state using state-specific validation rules.
    /// Uses the Strategy Pattern with IValidationRules implementations.
    /// </summary>
    public class MenuInputValidator : IMenuInputValidator
    {
        private readonly Dictionary<GameState, IValidationRules> validationRules;

        /// <summary>
        /// Creates a new MenuInputValidator instance.
        /// </summary>
        public MenuInputValidator()
        {
            validationRules = new Dictionary<GameState, IValidationRules>();
            DebugLogger.Log("MenuInputValidator", "Validator initialized");
        }

        /// <summary>
        /// Registers validation rules for a specific game state.
        /// </summary>
        /// <param name="state">The game state</param>
        /// <param name="rules">The validation rules to use for this state</param>
        public void RegisterRules(GameState state, IValidationRules rules)
        {
            if (rules == null)
                throw new ArgumentNullException(nameof(rules));

            validationRules[state] = rules;
            DebugLogger.Log("MenuInputValidator", 
                $"Registered validation rules for state: {state}");
        }

        /// <summary>
        /// Validates input for a specific game state.
        /// </summary>
        public ValidationResult Validate(string input, GameState state)
        {
            try
            {
                DebugLogger.Log("MenuInputValidator", 
                    $"Validating input: '{input}' for state: {state}");

                // Check for null/empty first
                if (string.IsNullOrWhiteSpace(input))
                {
                    return ValidationResult.Invalid("Input cannot be empty");
                }

                // Get validation rules for this state
                if (!validationRules.TryGetValue(state, out var rules))
                {
                    var error = $"No validation rules registered for state: {state}";
                    DebugLogger.Log("MenuInputValidator", error);
                    return ValidationResult.Invalid(error);
                }

                // Use state-specific rules to validate
                var result = rules.Validate(input);
                
                if (!result.IsValid)
                {
                    DebugLogger.Log("MenuInputValidator", 
                        $"Validation failed: {result.Error}");
                }
                else
                {
                    DebugLogger.Log("MenuInputValidator", "Validation passed");
                }

                return result;
            }
            catch (Exception ex)
            {
                var error = $"Exception during validation: {ex.Message}";
                DebugLogger.Log("MenuInputValidator", error);
                return ValidationResult.Invalid("Validation error");
            }
        }

        /// <summary>
        /// Gets the number of registered rule sets.
        /// </summary>
        public int RuleCount => validationRules.Count;
    }
}

