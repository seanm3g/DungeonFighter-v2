using System;
using System.Threading;

namespace RPGGame
{
    /// <summary>
    /// Centralized UI management system that handles all console output, formatting, and delays
    /// Replaces scattered Console.WriteLine calls across the codebase
    /// </summary>
    public static class UIManager
    {
        // Flag to disable all UI output during balance analysis
        public static bool DisableAllUIOutput = false;
        
        private static UIConfiguration? _uiConfig = null;
        
        // Progressive delay system for menu lines
        private static int consecutiveMenuLines = 0;
        private static int baseMenuDelay = 0;
        
        /// <summary>
        /// Gets the current UI configuration
        /// </summary>
        public static UIConfiguration UIConfig
        {
            get
            {
                if (_uiConfig == null)
                {
                    _uiConfig = UIConfiguration.LoadFromFile();
                }
                return _uiConfig;
            }
        }
        
        /// <summary>
        /// Reloads the UI configuration from file
        /// </summary>
        public static void ReloadConfiguration()
        {
            _uiConfig = UIConfiguration.LoadFromFile();
        }
        
        /// <summary>
        /// Writes a line to console with optional delay
        /// </summary>
        /// <param name="message">Message to display</param>
        /// <param name="messageType">Type of message for delay configuration</param>
        public static void WriteLine(string message, UIMessageType messageType = UIMessageType.System)
        {
            if (DisableAllUIOutput || UIConfig.DisableAllOutput) return;
            Console.WriteLine(message);
            ApplyDelay(messageType);
        }
        
        /// <summary>
        /// Writes text to console without newline
        /// </summary>
        /// <param name="message">Message to display</param>
        public static void Write(string message)
        {
            if (DisableAllUIOutput) return;
            Console.Write(message);
        }
        
        
        
        /// <summary>
        /// Writes a system message (no entity tracking)
        /// </summary>
        /// <param name="message">System message to display</param>
        public static void WriteSystemLine(string message)
        {
            WriteLine(message, UIMessageType.System);
        }
        
        /// <summary>
        /// Writes a menu line with progressive delay reduction (speeds up with each consecutive line)
        /// </summary>
        /// <param name="message">Menu message to display</param>
        public static void WriteMenuLine(string message)
        {
            if (DisableAllUIOutput || UIConfig.DisableAllOutput) return;
            Console.WriteLine(message);
            ApplyProgressiveMenuDelay();
        }
        
        /// <summary>
        /// Writes a title line with title delay
        /// </summary>
        /// <param name="message">Title message to display</param>
        public static void WriteTitleLine(string message)
        {
            WriteLine(message, UIMessageType.Title);
        }
        
        
        
        /// <summary>
        /// Writes a dungeon-related message with system delay
        /// </summary>
        /// <param name="message">Dungeon message to display</param>
        public static void WriteDungeonLine(string message)
        {
            WriteLine(message, UIMessageType.System);
        }
        
        /// <summary>
        /// Writes a room-related message with system delay
        /// </summary>
        /// <param name="message">Room message to display</param>
        public static void WriteRoomLine(string message)
        {
            WriteLine(message, UIMessageType.System);
        }
        
        /// <summary>
        /// Writes an enemy encounter message with system delay
        /// </summary>
        /// <param name="message">Enemy encounter message to display</param>
        public static void WriteEnemyLine(string message)
        {
            WriteLine(message, UIMessageType.System);
        }
        
        /// <summary>
        /// Writes a room cleared message with system delay
        /// </summary>
        /// <param name="message">Room cleared message to display</param>
        public static void WriteRoomClearedLine(string message)
        {
            WriteLine(message, UIMessageType.System);
        }
        
        
        /// <summary>
        /// Writes a status effect message (stun, poison, bleed, etc.) with effect message delay
        /// </summary>
        /// <param name="message">Status effect message to display</param>
        public static void WriteEffectLine(string message)
        {
            WriteLine(message, UIMessageType.EffectMessage);
        }
        
        
        
        
        /// <summary>
        /// Resets entity tracking for a new battle
        /// </summary>
        public static void ResetForNewBattle()
        {
            // Entity tracking is now handled by BlockDisplayManager
        }
        
        /// <summary>
        /// Applies appropriate delay based on message type using the beat-based timing system
        /// </summary>
        /// <param name="messageType">Type of message for delay configuration</param>
        public static void ApplyDelay(UIMessageType messageType)
        {
            int delayMs = UIConfig.GetEffectiveDelay(messageType);
            
            if (delayMs > 0)
            {
                Thread.Sleep(delayMs);
            }
        }
        
        /// <summary>
        /// Applies progressive menu delay - reduces delay by 1ms for each consecutive menu line
        /// After 20 lines, slowly ramps delay down by 1ms each line
        /// </summary>
        private static void ApplyProgressiveMenuDelay()
        {
            // Get base menu delay from configuration
            int baseDelay = UIConfig.BeatTiming.GetMenuDelay();
            
            // Store base delay on first menu line
            if (consecutiveMenuLines == 0)
            {
                baseMenuDelay = baseDelay;
            }
            
            int progressiveDelay;
            
            if (consecutiveMenuLines < 20)
            {
                // First 20 lines: normal progressive reduction (base delay minus 1ms per line)
                progressiveDelay = Math.Max(0, baseMenuDelay - consecutiveMenuLines);
            }
            else
            {
                // After 20 lines: slowly ramp down by 1ms each line
                // Start from the delay at line 20, then reduce by 1ms each subsequent line
                int delayAtLine20 = Math.Max(0, baseMenuDelay - 19); // Delay at line 20 (0-indexed, so line 20 is index 19)
                progressiveDelay = Math.Max(0, delayAtLine20 - (consecutiveMenuLines - 20));
            }
            
            // Apply the delay
            if (progressiveDelay > 0)
            {
                Thread.Sleep(progressiveDelay);
            }
            
            // Increment consecutive menu line counter
            consecutiveMenuLines++;
        }
        
        /// <summary>
        /// Resets the progressive menu delay counter (call when menu section is complete)
        /// </summary>
        public static void ResetMenuDelayCounter()
        {
            consecutiveMenuLines = 0;
            baseMenuDelay = 0;
        }
        
        /// <summary>
        /// Gets the current consecutive menu line count (for testing/debugging)
        /// </summary>
        public static int GetConsecutiveMenuLineCount()
        {
            return consecutiveMenuLines;
        }
        
        /// <summary>
        /// Gets the current base menu delay (for testing/debugging)
        /// </summary>
        public static int GetBaseMenuDelay()
        {
            return baseMenuDelay;
        }
        
        
        /// <summary>
        /// Writes a blank line without any delay
        /// </summary>
        public static void WriteBlankLine()
        {
            if (DisableAllUIOutput || UIConfig.DisableAllOutput) return;
            Console.WriteLine();
        }
        
        
    }
}
