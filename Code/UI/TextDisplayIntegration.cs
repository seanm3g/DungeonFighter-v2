using System;
using System.Collections.Generic;
using System.Threading;
using RPGGame.UI;
using RPGGame.UI.ColorSystem;

namespace RPGGame
{
    /// <summary>
    /// Integration layer for displaying text using the new UIManager beat-based timing system
    /// Provides a clean interface for game components to display messages
    /// </summary>
    public static class TextDisplayIntegration
    {
        /// <summary>
        /// Applies keyword coloring to text if enabled
        /// Skips coloring if text already has explicit color codes to prevent conflicts
        /// </summary>
        private static string ApplyKeywordColoring(string text)
        {
            // Return text as-is. Keyword coloring will be applied at render time if needed.
            // This method is kept for compatibility but no longer modifies the text.
            return text;
        }
        
        // ===== COLORED TEXT METHODS (PRIMARY API) =====
        
        /// <summary>
        /// Displays a combat action using ColoredText for better color management
        /// This is the primary method - uses structured ColoredText for action, roll info, and status effects
        /// </summary>
        public static void DisplayCombatAction(
            List<ColoredText> actionText, 
            List<ColoredText> rollInfo, 
            List<List<ColoredText>>? statusEffects = null,
            List<List<ColoredText>>? narrativeMessages = null)
        {
            // Check if this is a critical miss and extract critical miss narrative
            List<ColoredText>? criticalMissNarrative = null;
            var remainingNarratives = new List<List<ColoredText>>();
            
            if (narrativeMessages != null)
            {
                // Check if action text contains "CRITICAL MISS" or "misses"
                string actionPlainText = ColoredTextRenderer.RenderAsPlainText(actionText);
                bool isCriticalMiss = actionPlainText.Contains("CRITICAL MISS", StringComparison.OrdinalIgnoreCase) || 
                                     actionPlainText.Contains("misses", StringComparison.OrdinalIgnoreCase);
                
                foreach (var narrative in narrativeMessages)
                {
                    if (narrative != null && narrative.Count > 0)
                    {
                        string narrativeText = ColoredTextRenderer.RenderAsPlainText(narrative);
                        // Check if this is a critical miss narrative (contains keywords like "wild swing", "misses completely", etc.)
                        bool isCriticalMissNarrative = narrativeText.Contains("wild swing", StringComparison.OrdinalIgnoreCase) ||
                                                      narrativeText.Contains("misses completely", StringComparison.OrdinalIgnoreCase) ||
                                                      narrativeText.Contains("goes wide", StringComparison.OrdinalIgnoreCase) ||
                                                      narrativeText.Contains("fails spectacularly", StringComparison.OrdinalIgnoreCase) ||
                                                      narrativeText.Contains("critical miss", StringComparison.OrdinalIgnoreCase);
                        
                        // If this is a critical miss action and the narrative is a critical miss narrative, include it in the action block
                        if (isCriticalMiss && isCriticalMissNarrative && criticalMissNarrative == null)
                        {
                            criticalMissNarrative = narrative;
                        }
                        else
                        {
                            // Keep other narratives to display separately
                            remainingNarratives.Add(narrative);
                        }
                    }
                }
            }
            
            // Display the action block with ColoredText, including critical miss narrative if present
            BlockDisplayManager.DisplayActionBlock(actionText, rollInfo, statusEffects, criticalMissNarrative);
            
            // Display remaining narrative messages as separate blocks
            foreach (var narrative in remainingNarratives)
            {
                if (narrative != null && narrative.Count > 0)
                {
                    BlockDisplayManager.DisplayNarrativeBlock(narrative);
                }
            }
        }
        
        
        /// <summary>
        /// Displays a menu with title and options
        /// </summary>
        /// <param name="title">Menu title</param>
        /// <param name="options">Menu options</param>
        public static void DisplayMenu(string title, List<string> options)
        {
            // Reset menu delay counter at the start of menu display
            UIManager.ResetMenuDelayCounter();
            
            UIManager.WriteTitleLine(CenterText(ApplyKeywordColoring(title)));
            
            // Add separator line between title and options
            if (!string.IsNullOrWhiteSpace(title))
            {
                // Calculate separator length based on the longest line in the title
                string[] titleLines = title.Split('\n');
                int maxLength = 0;
                foreach (string line in titleLines)
                {
                    if (line.Trim().Length > maxLength)
                    {
                        maxLength = line.Trim().Length;
                    }
                }
                
                // Use dashes for menu separator (shorter than title separator)
                if (maxLength > 0)
                {
                    UIManager.WriteMenuLine(CenterText(new string('-', Math.Max(8, maxLength))));
                }
            }
            
            foreach (var option in options)
            {
                UIManager.WriteMenuLine(CenterText(ApplyKeywordColoring(option)));
            }
            
            // Add blank line after menu options for better spacing
            UIManager.WriteBlankLine();
            
            // Reset menu delay counter after menu display is complete
            UIManager.ResetMenuDelayCounter();
        }
        
        /// <summary>
        /// Centers text based on console window width
        /// Handles multi-line strings by centering each line independently
        /// </summary>
        private static string CenterText(string text)
        {
            if (string.IsNullOrEmpty(text))
                return text;
                
            try
            {
                int consoleWidth = Console.WindowWidth;
                if (consoleWidth <= 0)
                    return text; // Can't center if width is invalid
                
                // Handle multi-line strings
                if (text.Contains('\n'))
                {
                    var lines = text.Split('\n');
                    var centeredLines = new List<string>();
                    foreach (var line in lines)
                    {
                        centeredLines.Add(CenterSingleLine(line, consoleWidth));
                    }
                    return string.Join("\n", centeredLines);
                }
                
                return CenterSingleLine(text, consoleWidth);
            }
            catch
            {
                // If console width can't be determined, return text as-is
                return text;
            }
        }
        
        /// <summary>
        /// Centers a single line of text
        /// </summary>
        private static string CenterSingleLine(string line, int consoleWidth)
        {
            // Calculate visible length (excluding color markup)
            int visibleLength = GetVisibleLength(line);
            
            if (visibleLength >= consoleWidth)
                return line; // Line is too long to center
            
            int padding = (consoleWidth - visibleLength) / 2;
            return new string(' ', padding) + line;
        }
        
        /// <summary>
        /// Gets the visible length of text, excluding color markup codes
        /// Handles both {{template|text}}, &X foreground, and ^X background markup formats
        /// </summary>
        private static int GetVisibleLength(string text)
        {
            if (string.IsNullOrEmpty(text))
                return 0;
            
            int length = 0;
            int i = 0;
            
            while (i < text.Length)
            {
                // Check for {{template|text}} markup
                if (i < text.Length - 1 && text[i] == '{' && text[i + 1] == '{')
                {
                    // Find the closing }}
                    int closeIndex = text.IndexOf("}}", i + 2);
                    if (closeIndex != -1)
                    {
                        // Extract the text portion after the |
                        int pipeIndex = text.IndexOf('|', i + 2);
                        if (pipeIndex != -1 && pipeIndex < closeIndex)
                        {
                            // Count only the text after the pipe
                            string visibleText = text.Substring(pipeIndex + 1, closeIndex - pipeIndex - 1);
                            length += visibleText.Length;
                            i = closeIndex + 2;
                            continue;
                        }
                    }
                }
                
                // Check for &X foreground color markup or ^X background color markup
                if ((text[i] == '&' || text[i] == '^') && i < text.Length - 1)
                {
                    // Skip the & or ^ and the color character
                    i += 2;
                    continue;
                }
                
                // Regular character
                length++;
                i++;
            }
            
            return length;
        }
        
        /// <summary>
        /// Displays a system message
        /// </summary>
        /// <param name="message">The system message</param>
        public static void DisplaySystem(string message)
        {
            UIManager.WriteSystemLine(ApplyKeywordColoring(message));
        }
        
        /// <summary>
        /// Displays a title message
        /// </summary>
        /// <param name="message">The title message</param>
        public static void DisplayTitle(string message)
        {
            UIManager.WriteTitleLine(ApplyKeywordColoring(message));
        }
        
        
        
        /// <summary>
        /// Displays a blank line
        /// </summary>
        public static void DisplayBlankLine()
        {
            UIManager.WriteBlankLine();
        }
        
        /// <summary>
        /// Resets the system for a new battle
        /// </summary>
        public static void ResetForNewBattle()
        {
            UIManager.ResetForNewBattle();
            BlockDisplayManager.ResetForNewBattle();
        }
    }
}
