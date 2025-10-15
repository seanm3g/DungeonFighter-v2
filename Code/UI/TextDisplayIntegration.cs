using System;
using System.Collections.Generic;
using RPGGame.UI;

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
            // Skip keyword coloring if text already has explicit color codes
            // This prevents double-coloring and spacing issues
            if (ColorParser.HasColorMarkup(text))
            {
                return text;
            }
            return KeywordColorSystem.Colorize(text);
        }
        
        /// <summary>
        /// Displays a combat action using the new block-based system
        /// </summary>
        /// <param name="combatResult">The combat result message</param>
        /// <param name="narrativeMessages">Narrative messages triggered by this action</param>
        /// <param name="statusEffects">Status effect messages</param>
        /// <param name="entityName">The entity performing the action</param>
        public static void DisplayCombatAction(string combatResult, List<string> narrativeMessages, List<string> statusEffects, string entityName)
        {
            // Parse the combat result to extract action text and roll info
            string actionText = "";
            string rollInfo = "";
            
            if (!string.IsNullOrEmpty(combatResult) && combatResult.Contains('\n') && combatResult.Contains("("))
            {
                // Split the result into damage text and roll info
                var lines = combatResult.Split('\n');
                if (lines.Length >= 2)
                {
                    actionText = lines[0];
                    rollInfo = lines[1].Trim(); // Remove the 4-space indentation
                }
            }
            else if (!string.IsNullOrEmpty(combatResult))
            {
                // Simple combat result without roll info
                actionText = combatResult;
                rollInfo = ""; // Will be handled by the action system
            }
            
            // Display the action block
            var validEffects = statusEffects?.FindAll(m => !string.IsNullOrEmpty(m)) ?? new List<string>();
            // Remove existing indentation from status effects since BlockDisplayManager will add it
            var trimmedEffects = validEffects.Select(e => e.TrimStart()).ToList();
            BlockDisplayManager.DisplayActionBlock(actionText, rollInfo, trimmedEffects);
            
            // Display narrative messages as separate blocks (no extra spacing)
            var validNarratives = narrativeMessages?.FindAll(m => !string.IsNullOrEmpty(m)) ?? new List<string>();
            foreach (var narrative in validNarratives)
            {
                BlockDisplayManager.DisplayNarrativeBlock(narrative);
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
