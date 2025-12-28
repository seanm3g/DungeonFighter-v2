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
            // - All effects use 5-space indentation to match roll info
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
    }
}

