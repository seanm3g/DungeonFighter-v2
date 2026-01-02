using System;
using System.Collections.Generic;

namespace RPGGame.Data.Validation
{
    /// <summary>
    /// Main validator that orchestrates validation of all game data files
    /// </summary>
    public class GameDataValidator
    {
        private readonly List<IDataValidator> _validators;

        public GameDataValidator()
        {
            _validators = new List<IDataValidator>();
            
            // Register all validators
            _validators.Add(new ActionDataValidator());
            _validators.Add(new EnemyDataValidator());
            _validators.Add(new WeaponDataValidator());
            _validators.Add(new ArmorDataValidator());
            _validators.Add(new DungeonDataValidator());
            _validators.Add(new RoomDataValidator());
            _validators.Add(new ReferenceValidator());
        }

        /// <summary>
        /// Validates all game data files
        /// </summary>
        /// <returns>Validation result containing all errors and warnings</returns>
        public ValidationResult ValidateAll()
        {
            var result = new ValidationResult();

            // Ensure data is loaded before validation
            EnsureDataLoaded();

            // Run all validators
            foreach (var validator in _validators)
            {
                try
                {
                    var validatorResult = validator.Validate();
                    result.Merge(validatorResult);
                }
                catch (Exception ex)
                {
                    result.AddError("Validation", validator.GetType().Name, "", 
                        $"Validator threw exception: {ex.Message}");
                }
            }

            return result;
        }

        /// <summary>
        /// Registers a validator to be run during validation
        /// </summary>
        public void RegisterValidator(IDataValidator validator)
        {
            if (validator != null)
            {
                _validators.Add(validator);
            }
        }

        /// <summary>
        /// Ensures all data loaders have loaded their data
        /// </summary>
        private void EnsureDataLoaded()
        {
            // Load actions if not already loaded
            if (ActionLoader.GetAllActions().Count == 0)
            {
                ActionLoader.LoadActions();
            }

            // Load enemies if not already loaded
            if (EnemyLoader.GetAllEnemyTypes().Count == 0)
            {
                EnemyLoader.LoadEnemies();
            }
        }
    }

    /// <summary>
    /// Interface for data validators
    /// </summary>
    public interface IDataValidator
    {
        ValidationResult Validate();
    }
}
