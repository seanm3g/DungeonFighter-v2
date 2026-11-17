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
        /// Only displays if the character is currently active (for multi-character support)
        /// Overview action blocks omit flavor prose (<paramref name="narrativeMessages"/> is ignored; F7 is the flavor path).
        /// </summary>
        /// <param name="actionText">The action text to display</param>
        /// <param name="rollInfo">Roll information</param>
        /// <param name="statusEffects">Status effects</param>
        /// <param name="narrativeMessages">Ignored — kept for call-site compatibility</param>
        /// <param name="character">The character this combat action belongs to (null = always display)</param>
        public static void DisplayCombatAction(
            List<ColoredText> actionText, 
            List<ColoredText> rollInfo, 
            List<List<ColoredText>>? statusEffects = null,
            List<List<ColoredText>>? narrativeMessages = null,
            Character? character = null)
        {
            _ = narrativeMessages;
            BlockDisplayManager.DisplayActionBlock(actionText, rollInfo, statusEffects, null, null, character);
        }

        /// <summary>
        /// Displays a combat action using ColoredText (async version)
        /// This version waits for the display delay to complete, allowing the combat loop to wait for each action
        /// Only displays if the character is currently active (for multi-character support)
        /// Overview action blocks omit flavor prose (<paramref name="narrativeMessages"/> is ignored; F7 is the flavor path).
        /// </summary>
        /// <param name="actionText">The action text to display</param>
        /// <param name="rollInfo">Roll information</param>
        /// <param name="statusEffects">Status effects</param>
        /// <param name="narrativeMessages">Ignored — kept for call-site compatibility</param>
        /// <param name="character">The character this combat action belongs to (null = always display)</param>
        public static async System.Threading.Tasks.Task DisplayCombatActionAsync(
            List<ColoredText> actionText, 
            List<ColoredText> rollInfo, 
            List<List<ColoredText>>? statusEffects = null,
            List<List<ColoredText>>? narrativeMessages = null,
            Character? character = null)
        {
            _ = narrativeMessages;
            await BlockDisplayManager.DisplayActionBlockAsync(actionText, rollInfo, statusEffects, null, null, character);
        }

        /// <summary>
        /// Legacy splitter kept for tests: overview combat log no longer attaches flavor under action blocks,
        /// so both out params are always empty / null.
        /// </summary>
        public static void SplitCombatNarratives(
            List<ColoredText> actionText,
            List<List<ColoredText>>? narrativeMessages,
            out List<ColoredText>? criticalMissNarrative,
            out List<List<ColoredText>> remainingNarratives)
        {
            _ = actionText;
            _ = narrativeMessages;
            criticalMissNarrative = null;
            remainingNarratives = new List<List<ColoredText>>();
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
        /// Resets the system for a new battle
        /// </summary>
        public static void ResetForNewBattle()
        {
            UIManager.ResetForNewBattle();
            BlockDisplayManager.ResetForNewBattle();
        }
    }
}
