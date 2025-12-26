using System.Collections.Generic;
using System.Linq;
using RPGGame.UI.ColorSystem;
using Avalonia.Media;

namespace RPGGame.UI.BlockDisplay
{
    /// <summary>
    /// Collects all messages for an action block into a structured list
    /// </summary>
    public static class BlockMessageCollector
    {
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
            
            // Add roll info
            if (rollInfo != null && rollInfo.Count > 0)
            {
                messageGroups.Add((rollInfo, UIMessageType.RollInfo));
            }
            
            // Add critical miss narrative
            // Critical miss narratives get keyword coloring and blank lines before/after (same as regular narratives)
            if (criticalMissNarrative != null && criticalMissNarrative.Count > 0)
            {
                // Add blank line before narrative
                messageGroups.Add((new List<ColoredText>(), UIMessageType.System));
                
                // Apply keyword coloring to narrative text
                // Convert ColoredText to plain text, then apply keyword coloring
                string plainText = ColoredTextRenderer.RenderAsPlainText(criticalMissNarrative);
                List<ColoredText> keywordColoredNarrative = KeywordColorSystem.Colorize(plainText);
                
                // Add the keyword-colored narrative
                messageGroups.Add((keywordColoredNarrative, UIMessageType.System));
                
                // Add blank line after narrative
                messageGroups.Add((new List<ColoredText>(), UIMessageType.System));
            }
            
            // Add status effects
            // Multiple status effects from one action should be grouped together:
            // - No blank lines between them
            // - First one has no indentation, subsequent ones are indented
            // - Blank line after the last status effect
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
                
                // Add the combined status effects as a single message group
                if (combinedStatusEffects.Count > 0)
                {
                    messageGroups.Add((combinedStatusEffects, UIMessageType.EffectMessage));
                    // Note: Blank line after status effects will be added after narratives (if present)
                    // or at the end if no narratives, to ensure it appears before the next character's action
                }
            }
            
            // Add all narratives (all part of the same turn block)
            // Narratives get keyword coloring and blank lines before/after
            bool hasNarratives = false;
            if (narratives != null)
            {
                foreach (var narrative in narratives)
                {
                    if (narrative != null && narrative.Count > 0)
                    {
                        hasNarratives = true;
                        // Add blank line before narrative
                        messageGroups.Add((new List<ColoredText>(), UIMessageType.System));
                        
                        // Apply keyword coloring to narrative text
                        // Convert ColoredText to plain text, then apply keyword coloring
                        string plainText = ColoredTextRenderer.RenderAsPlainText(narrative);
                        List<ColoredText> keywordColoredNarrative = KeywordColorSystem.Colorize(plainText);
                        
                        // Add the keyword-colored narrative
                        messageGroups.Add((keywordColoredNarrative, UIMessageType.System));
                        
                        // Add blank line after narrative
                        messageGroups.Add((new List<ColoredText>(), UIMessageType.System));
                    }
                }
            }
            
            // Add blank line after status effects (if present) to separate from next character's action
            // Only add if no narratives are present, since narratives already add a blank line at the end
            if (statusEffects != null && statusEffects.Count > 0 && !hasNarratives)
            {
                messageGroups.Add((new List<ColoredText>(), UIMessageType.System));
            }
            
            return messageGroups;
        }
        
        /// <summary>
        /// Processes status effect indentation:
        /// - First effect: removes leading indentation (4 spaces)
        /// - Subsequent effects: ensures they have 4 spaces of indentation
        /// - Stat bonus messages (starting with "     (") always keep 5-space indentation to match roll info
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
                
                // Check if this is a stat bonus message (starts with 5 spaces and opening paren)
                // Stat bonus messages should always keep their 5-space indentation to match roll info
                // Check if first segment starts with "     (" or if first segment is "     " (5 spaces) and next segment starts with "("
                bool isStatBonus = firstText.StartsWith("     (") || 
                                  (firstText == "     " && result.Count > 1 && result[1].Text?.StartsWith("(") == true);
                
                // Remove leading indentation from first effect
                if (isFirst)
                {
                    // Stat bonus messages keep their 5-space indentation even when first
                    if (isStatBonus)
                    {
                        // If first segment is exactly "     " (5 spaces), keep it
                        // Otherwise it already starts with "     (", keep as-is
                        return result;
                    }
                    
                    // Check if first segment starts with 4 spaces
                    if (firstText.StartsWith("    "))
                    {
                        // Remove the 4-space indentation
                        string remainingText = firstText.Substring(4);
                        if (string.IsNullOrEmpty(remainingText))
                        {
                            // If the entire segment was just indentation, remove it
                            result.RemoveAt(0);
                        }
                        else
                        {
                            // Replace with the text without indentation
                            result[0] = new ColoredText(remainingText, firstSegment.Color);
                        }
                    }
                    // Check if first segment is exactly 4 spaces (separate whitespace segment)
                    else if (firstText == "    " && result.Count > 1)
                    {
                        // Remove the whitespace segment
                        result.RemoveAt(0);
                    }
                }
                // Ensure subsequent effects have indentation
                else
                {
                    // Stat bonus messages keep their 5-space indentation
                    if (isStatBonus)
                    {
                        // Ensure it has 5-space indentation
                        if (firstText == "     " && result.Count > 1)
                        {
                            // Already has 5-space segment, keep as-is
                            return result;
                        }
                        else if (!firstText.StartsWith("     "))
                        {
                            // If it doesn't start with 5 spaces, add them
                            if (firstText.StartsWith("    "))
                            {
                                // Replace 4 spaces with 5 spaces
                                result[0] = new ColoredText(" " + firstText, firstSegment.Color);
                            }
                            else
                            {
                                // Add 5-space indentation
                                result.Insert(0, new ColoredText("     ", Colors.White));
                            }
                        }
                        return result;
                    }
                    
                    // Check if already has indentation
                    bool hasIndentation = firstText.StartsWith("    ") || 
                                         (firstText == "    " && result.Count > 0);
                    
                    if (!hasIndentation)
                    {
                        // Add 4-space indentation at the beginning
                        result.Insert(0, new ColoredText("    ", Colors.White));
                    }
                }
            }
            
            return result;
        }
    }
}

