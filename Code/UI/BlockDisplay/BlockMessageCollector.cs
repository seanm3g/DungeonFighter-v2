using System.Collections.Generic;
using System.Linq;
using RPGGame.UI.ColorSystem;
using Avalonia.Media;
using System;
using RPGGame;

namespace RPGGame.UI.BlockDisplay
{
    /// <summary>
    /// Collects all messages for an action block into a structured list
    /// </summary>
    public static class BlockMessageCollector
    {
        /// <summary>
        /// Gets the darkening factor for subsequent lines from UIConfiguration
        /// Defaults to 0.8 (20% darker) if not configured
        /// </summary>
        private static double GetSubsequentLineDarkening()
        {
            try
            {
                var config = UIConfiguration.LoadFromFile();
                return config.SubsequentLineDarkening;
            }
            catch
            {
                // Default to 0.8 (20% darker) if config can't be loaded
                return 0.8;
            }
        }
        /// <summary>
        /// Collects all messages for an action block into a structured list
        /// </summary>
        public static List<(List<ColoredText> segments, UIMessageType messageType)> CollectActionBlockMessages(
            List<ColoredText>? actionText,
            List<ColoredText>? rollInfo,
            List<List<ColoredText>>? statusEffects,
            List<ColoredText>? criticalMissNarrative,
            List<List<ColoredText>>? narratives)
        {
            var messageGroups = new List<(List<ColoredText> segments, UIMessageType messageType)>();
            
            // Add action text
            if (actionText != null && actionText.Count > 0)
            {
                messageGroups.Add((actionText, UIMessageType.Combat));
            }
            
            // Add roll info (subsequent line - darken by 20%)
            if (rollInfo != null && rollInfo.Count > 0)
            {
                var darkenedRollInfo = DarkenColors(rollInfo);
                messageGroups.Add((darkenedRollInfo, UIMessageType.RollInfo));
            }
            
            // Add critical miss narrative
            // Critical miss narratives get keyword coloring and blank line before (same as regular narratives)
            // Note: Blank line after removed - TextSpacingSystem handles spacing between action blocks
            if (criticalMissNarrative != null && criticalMissNarrative.Count > 0)
            {
                // Add blank line before narrative
                messageGroups.Add((new List<ColoredText>(), UIMessageType.System));
                
                // Apply keyword coloring to narrative text
                // Convert ColoredText to plain text, then apply keyword coloring
                string plainText = ColoredTextRenderer.RenderAsPlainText(criticalMissNarrative);
                List<ColoredText> keywordColoredNarrative = KeywordColorSystem.Colorize(plainText);
                
                // Darken the narrative (subsequent line - darken by 20%)
                var darkenedNarrative = DarkenColors(keywordColoredNarrative);
                
                // Add the keyword-colored narrative
                messageGroups.Add((darkenedNarrative, UIMessageType.System));
                
                // Note: Blank line after narrative removed - TextSpacingSystem handles spacing between action blocks
            }
            
            // Add status effects
            // Multiple status effects from one action should be grouped together:
            // - No blank lines between them
            // - All effects use 5-space indentation to match roll info
            // Note: Blank line after status effects removed - TextSpacingSystem handles spacing between action blocks
            if (statusEffects != null && statusEffects.Count > 0)
            {
                var combinedStatusEffects = new List<ColoredText>();
                bool isFirst = true;
                
                foreach (var effect in statusEffects)
                {
                    if (effect != null && effect.Count > 0)
                    {
                        // If not the first effect, add a newline before it
                        if (!isFirst)
                        {
                            combinedStatusEffects.Add(new ColoredText(System.Environment.NewLine, Colors.White));
                        }
                        
                        // Process the effect to handle indentation
                        var processedEffect = ProcessStatusEffectIndentation(effect, isFirst);
                        combinedStatusEffects.AddRange(processedEffect);
                        
                        isFirst = false;
                    }
                }
                
                // Add the combined status effects as a single message group (subsequent lines - darken by 20%)
                if (combinedStatusEffects.Count > 0)
                {
                    var darkenedStatusEffects = DarkenColors(combinedStatusEffects);
                    messageGroups.Add((darkenedStatusEffects, UIMessageType.EffectMessage));
                    // Note: Blank line after status effects removed - TextSpacingSystem handles spacing between action blocks
                }
            }
            
            // Add all narratives (all part of the same turn block)
            // Narratives get keyword coloring and blank line before
            // Note: Blank line after removed - TextSpacingSystem handles spacing between action blocks
            if (narratives != null)
            {
                foreach (var narrative in narratives)
                {
                    if (narrative != null && narrative.Count > 0)
                    {
                        // Add blank line before narrative
                        messageGroups.Add((new List<ColoredText>(), UIMessageType.System));
                        
                        // Apply keyword coloring to narrative text
                        // Convert ColoredText to plain text, then apply keyword coloring
                        string plainText = ColoredTextRenderer.RenderAsPlainText(narrative);
                        List<ColoredText> keywordColoredNarrative = KeywordColorSystem.Colorize(plainText);
                        
                        // Darken the narrative (subsequent line - darken by 20%)
                        var darkenedNarrative = DarkenColors(keywordColoredNarrative);
                        
                        // Add the keyword-colored narrative
                        messageGroups.Add((darkenedNarrative, UIMessageType.System));
                        
                        // Note: Blank line after narrative removed - TextSpacingSystem handles spacing between action blocks
                    }
                }
            }
            
            // Note: Blank line after status effects removed - TextSpacingSystem handles spacing between action blocks
            
            return messageGroups;
        }
        
        /// <summary>
        /// Standard indentation for action block subsequent lines (roll info, effects, etc.)
        /// </summary>
        private const string ACTION_BLOCK_INDENT = "     "; // 5 spaces
        
        /// <summary>
        /// Processes status effect indentation:
        /// - All effects use 5-space indentation to match roll info
        /// - All effects are always indented (they are subsequent lines in the action block)
        /// - The isFirst parameter only affects newline insertion, not indentation
        /// </summary>
        private static List<ColoredText> ProcessStatusEffectIndentation(List<ColoredText> effect, bool isFirst)
        {
            if (effect == null || effect.Count == 0)
            {
                return new List<ColoredText>();
            }
            
            var result = new List<ColoredText>(effect);
            
            if (result.Count > 0)
            {
                var firstSegment = result[0];
                string firstText = firstSegment.Text ?? "";
                
                // All status effects should be indented (they are subsequent lines in action blocks)
                // Check if already has 5-space indentation
                bool hasIndentation = firstText.StartsWith(ACTION_BLOCK_INDENT) || 
                                     (firstText == ACTION_BLOCK_INDENT && result.Count > 0);
                
                if (!hasIndentation)
                {
                    // Check if it has 4-space indentation (upgrade to 5)
                    if (firstText.StartsWith("    ") || firstText == "    ")
                    {
                        // Replace 4-space indentation with 5-space
                        if (firstText == "    " && result.Count > 1)
                        {
                            // Replace the 4-space segment with 5-space
                            result[0] = new ColoredText(ACTION_BLOCK_INDENT, Colors.White);
                        }
                        else if (firstText.StartsWith("    "))
                        {
                            // Add one more space to make it 5 spaces
                            result[0] = new ColoredText(" " + firstText, firstSegment.Color);
                        }
                    }
                    else
                    {
                        // Add 5-space indentation at the beginning
                        result.Insert(0, new ColoredText(ACTION_BLOCK_INDENT, Colors.White));
                    }
                }
            }
            
            return result;
        }
        
        /// <summary>
        /// Darkens all colors in a list of ColoredText segments by the specified factor (20% darker = 0.8 brightness)
        /// Used to make subsequent lines in action blocks visually distinct from the primary line
        /// </summary>
        private static List<ColoredText> DarkenColors(List<ColoredText> segments)
        {
            if (segments == null || segments.Count == 0)
            {
                return segments ?? new List<ColoredText>();
            }
            
            var darkenedSegments = new List<ColoredText>();
            
            foreach (var segment in segments)
            {
                if (segment == null)
                {
                    // Skip null segments to avoid null reference warnings
                    continue;
                }
                
                // Get darkening factor from configuration
                double darkeningFactor = GetSubsequentLineDarkening();
                
                // Darken the color by multiplying RGB values by the darkening factor
                // Clamp values to ensure they stay within valid byte range (0-255)
                byte r = (byte)Math.Min(255, Math.Max(0, (int)(segment.Color.R * darkeningFactor)));
                byte g = (byte)Math.Min(255, Math.Max(0, (int)(segment.Color.G * darkeningFactor)));
                byte b = (byte)Math.Min(255, Math.Max(0, (int)(segment.Color.B * darkeningFactor)));
                
                // Preserve alpha channel
                Color darkenedColor = Color.FromArgb(segment.Color.A, r, g, b);
                
                // Create new ColoredText with darkened color
                darkenedSegments.Add(new ColoredText(segment.Text, darkenedColor));
            }
            
            return darkenedSegments;
        }
    }
}

