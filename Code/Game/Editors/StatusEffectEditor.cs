using System;
using System.Collections.Generic;
using System.Linq;
using RPGGame;
using RPGGame.Utils;

namespace RPGGame.Editors
{
    /// <summary>
    /// Editor for creating, editing, and deleting status effects
    /// </summary>
    public class StatusEffectEditor
    {
        private readonly GameConfiguration config;

        public StatusEffectEditor()
        {
            config = GameConfiguration.Instance;
            config.StatusEffects.InitializeDefaults();
        }

        /// <summary>
        /// Get all status effects
        /// </summary>
        public List<StatusEffectInfo> GetStatusEffects()
        {
            config.StatusEffects.InitializeDefaults();
            return config.StatusEffects.Effects.Select(kvp => new StatusEffectInfo
            {
                Name = kvp.Key,
                Config = kvp.Value
            }).ToList();
        }

        /// <summary>
        /// Get a status effect by name
        /// </summary>
        public StatusEffectConfig? GetStatusEffect(string name)
        {
            return config.StatusEffects.GetEffect(name);
        }

        /// <summary>
        /// Create a new status effect
        /// </summary>
        public bool CreateStatusEffect(string name, StatusEffectConfig effectConfig)
        {
            try
            {
                if (config.StatusEffects.GetEffect(name) != null)
                {
                    return false; // Already exists
                }

                config.StatusEffects.SetEffect(name, effectConfig);
                return SaveStatusEffects();
            }
            catch (Exception)
            {
                return false;
            }
        }

        /// <summary>
        /// Update an existing status effect
        /// </summary>
        public bool UpdateStatusEffect(string originalName, string newName, StatusEffectConfig updatedConfig)
        {
            try
            {
                var existingEffect = config.StatusEffects.GetEffect(originalName);
                if (existingEffect == null)
                {
                    return false;
                }

                // If name changed, check for conflicts
                if (!originalName.Equals(newName, StringComparison.OrdinalIgnoreCase))
                {
                    if (config.StatusEffects.GetEffect(newName) != null)
                    {
                        return false; // New name already exists
                    }
                    
                    // Remove old and add new
                    config.StatusEffects.RemoveEffect(originalName);
                    config.StatusEffects.SetEffect(newName, updatedConfig);
                }
                else
                {
                    // Just update the config
                    config.StatusEffects.SetEffect(originalName, updatedConfig);
                }

                return SaveStatusEffects();
            }
            catch (Exception)
            {
                return false;
            }
        }

        /// <summary>
        /// Delete a status effect
        /// </summary>
        public bool DeleteStatusEffect(string name, out string? errorMessage)
        {
            errorMessage = null;
            try
            {
                if (config.StatusEffects.IsDefaultEffect(name))
                {
                    errorMessage = $"Cannot delete '{name}' - it is a default status effect and cannot be removed.";
                    return false;
                }
                
                if (config.StatusEffects.RemoveEffect(name) && SaveStatusEffects())
                {
                    return true;
                }
                
                errorMessage = $"Failed to delete status effect '{name}'. It may not exist.";
                return false;
            }
            catch (Exception ex)
            {
                errorMessage = $"Error deleting status effect: {ex.Message}";
                return false;
            }
        }

        /// <summary>
        /// Delete a status effect (backward compatibility)
        /// </summary>
        public bool DeleteStatusEffect(string name)
        {
            return DeleteStatusEffect(name, out _);
        }

        /// <summary>
        /// Check if a status effect is a default effect (cannot be deleted)
        /// </summary>
        public bool IsDefaultEffect(string name)
        {
            return config.StatusEffects.IsDefaultEffect(name);
        }

        /// <summary>
        /// Validate a status effect
        /// </summary>
        public string? ValidateStatusEffect(string name, string? originalName = null)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                return "Status effect name cannot be empty.";
            }

            // Check name uniqueness (only for new effects or if name changed)
            if (originalName == null || !originalName.Equals(name, StringComparison.OrdinalIgnoreCase))
            {
                var existingEffect = GetStatusEffect(name);
                if (existingEffect != null)
                {
                    return $"A status effect with the name '{name}' already exists.";
                }
            }

            return null; // Validation passed
        }

        /// <summary>
        /// Save status effects to TuningConfig.json
        /// </summary>
        private bool SaveStatusEffects()
        {
            try
            {
                return config.SaveToFile();
            }
            catch (Exception)
            {
                return false;
            }
        }
    }

    /// <summary>
    /// Info class for status effects (name + config)
    /// </summary>
    public class StatusEffectInfo
    {
        public string Name { get; set; } = "";
        public StatusEffectConfig Config { get; set; } = new();
    }
}
