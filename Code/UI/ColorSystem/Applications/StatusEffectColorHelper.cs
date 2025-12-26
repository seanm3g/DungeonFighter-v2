using System;
using System.Collections.Generic;
using Avalonia.Media;
using RPGGame.UI.ColorSystem;
using RPGGame;

namespace RPGGame.UI.ColorSystem.Applications
{
    /// <summary>
    /// Centralized helper for coloring status effects using color templates
    /// Maps status effect names (case-insensitive) to their appropriate color templates
    /// </summary>
    public static class StatusEffectColorHelper
    {
        /// <summary>
        /// Maps status effect names to their color template names
        /// All status effects should use themed color templates for consistency
        /// Dictionary is case-insensitive, so only one entry per status effect is needed
        /// </summary>
        private static readonly Dictionary<string, string> StatusEffectTemplates = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            // Core status effects (case-insensitive matching, so only one entry needed)
            { "BLEED", "bleeding" },
            { "BLEEDING", "bleeding" },
            
            { "WEAKEN", "weakened" },
            { "WEAKENED", "weakened" },
            
            { "SLOW", "slowed" },
            { "SLOWED", "slowed" },
            
            { "POISON", "poisoned" },
            { "POISONED", "poisoned" },
            
            { "STUN", "stunned" },
            { "STUNNED", "stunned" },
            
            { "BURN", "burning" },
            { "BURNING", "burning" },
            
            { "FREEZE", "frozen" },
            { "FROZEN", "frozen" },
            
            // Advanced status effects (use corrupted template as default for debuffs)
            { "VULNERABILITY", "corrupted" },
            { "HARDEN", "holy" },
            { "FORTIFY", "holy" },
            { "FOCUS", "arcane" },
            { "EXPOSE", "corrupted" },
            { "HPREGEN", "heal" },
            { "ARMORBREAK", "corrupted" },
            { "PIERCE", "corrupted" },
            { "REFLECT", "arcane" },
            { "SILENCE", "corrupted" },
            { "STATDRAIN", "corrupted" },
            { "ABSORB", "arcane" },
            { "TEMPORARYHP", "heal" },
            { "CONFUSION", "corrupted" },
            { "CLEANSE", "holy" },
            { "MARK", "corrupted" },
            { "DISRUPT", "corrupted" },
            
            // Generic fallback
            { "EFFECT", "corrupted" }
        };
        
        /// <summary>
        /// Gets the color template name for a status effect
        /// </summary>
        /// <param name="effectName">The status effect name (case-insensitive)</param>
        /// <returns>The template name, or "corrupted" as fallback</returns>
        public static string GetTemplateName(string effectName)
        {
            if (string.IsNullOrEmpty(effectName))
                return "corrupted";
            
            if (StatusEffectTemplates.TryGetValue(effectName, out var template))
                return template;
            
            // Fallback to corrupted for unknown effects
            return "corrupted";
        }
        
        /// <summary>
        /// Gets colored text for a status effect name using its template
        /// </summary>
        /// <param name="effectName">The status effect name</param>
        /// <returns>List of ColoredText segments with themed colors</returns>
        public static List<ColoredText> GetColoredStatusEffect(string effectName)
        {
            try
            {
                if (string.IsNullOrEmpty(effectName))
                    return ColorTemplateLibrary.SingleColor(effectName ?? "", Colors.White);
                
                string templateName = GetTemplateName(effectName);
                return ColorTemplateLibrary.GetTemplate(templateName, effectName);
            }
            catch (TypeInitializationException tiex)
            {
                // Handle type initializer exceptions (wraps the real exception)
                // This can happen if color system initialization fails during first access
                Exception innerEx = tiex.InnerException ?? tiex;
                ErrorHandler.LogError(innerEx, "StatusEffectColorHelper.GetColoredStatusEffect", 
                    $"Type initializer failed for status effect '{effectName}'. Using fallback white color. Inner: {innerEx.Message}");
                return new List<ColoredText> { new ColoredText(effectName ?? "", Colors.White) };
            }
            catch (Exception ex)
            {
                // If color system initialization fails, fall back to white text
                // This prevents type initializer exceptions from crashing the game
                ErrorHandler.LogError(ex, "StatusEffectColorHelper.GetColoredStatusEffect", 
                    $"Failed to get colored status effect for '{effectName}'. Using fallback white color.");
                return new List<ColoredText> { new ColoredText(effectName ?? "", Colors.White) };
            }
        }
        
        /// <summary>
        /// Gets a single representative color for a status effect (for simple coloring)
        /// Uses the first non-white color from the template
        /// </summary>
        /// <param name="effectName">The status effect name</param>
        /// <returns>A Color for the status effect</returns>
        public static Color GetStatusEffectColor(string effectName)
        {
            try
            {
                if (string.IsNullOrEmpty(effectName))
                    return Colors.White;
                
                string templateName = GetTemplateName(effectName);
                return ColorTemplateLibrary.GetRepresentativeColorFromTemplate(templateName);
            }
            catch (TypeInitializationException tiex)
            {
                // Handle type initializer exceptions (wraps the real exception)
                // This can happen if color system initialization fails during first access
                Exception innerEx = tiex.InnerException ?? tiex;
                ErrorHandler.LogError(innerEx, "StatusEffectColorHelper.GetStatusEffectColor", 
                    $"Type initializer failed for status effect '{effectName}'. Using fallback white color. Inner: {innerEx.Message}");
                return Colors.White;
            }
            catch (Exception ex)
            {
                // If color system initialization fails, fall back to white
                // This prevents type initializer exceptions from crashing the game
                ErrorHandler.LogError(ex, "StatusEffectColorHelper.GetStatusEffectColor", 
                    $"Failed to get status effect color for '{effectName}'. Using fallback white color.");
                return Colors.White;
            }
        }
        
        /// <summary>
        /// Checks if a status effect has a defined template
        /// </summary>
        /// <param name="effectName">The status effect name</param>
        /// <returns>True if a template is defined, false otherwise</returns>
        public static bool HasTemplate(string effectName)
        {
            if (string.IsNullOrEmpty(effectName))
                return false;
            
            return StatusEffectTemplates.ContainsKey(effectName);
        }
    }
}

