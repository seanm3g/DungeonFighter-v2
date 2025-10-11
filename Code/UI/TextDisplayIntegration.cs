using System;
using System.Collections.Generic;

namespace RPGGame
{
    /// <summary>
    /// Integration layer for displaying text using the new UIManager beat-based timing system
    /// Provides a clean interface for game components to display messages
    /// </summary>
    public static class TextDisplayIntegration
    {
        
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
            
            UIManager.WriteTitleLine(title);
            
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
                    UIManager.WriteMenuLine(new string('-', Math.Max(8, maxLength)));
                }
            }
            
            foreach (var option in options)
            {
                UIManager.WriteMenuLine(option);
            }
            
            // Add blank line after menu options for better spacing
            UIManager.WriteBlankLine();
            
            // Reset menu delay counter after menu display is complete
            UIManager.ResetMenuDelayCounter();
        }
        
        /// <summary>
        /// Displays a system message
        /// </summary>
        /// <param name="message">The system message</param>
        public static void DisplaySystem(string message)
        {
            UIManager.WriteSystemLine(message);
        }
        
        /// <summary>
        /// Displays a title message
        /// </summary>
        /// <param name="message">The title message</param>
        public static void DisplayTitle(string message)
        {
            UIManager.WriteTitleLine(message);
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
