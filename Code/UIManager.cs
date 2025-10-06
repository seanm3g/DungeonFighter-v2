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
        
        private static string? lastActingEntity = null;
        private static UIConfiguration? _uiConfig = null;
        
        // Progressive delay system for menu lines
        private static int consecutiveMenuLines = 0;
        private static int baseMenuDelay = 0;
        
        /// <summary>
        /// Gets the current UI configuration
        /// </summary>
        private static UIConfiguration UIConfig
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
        /// Writes a line with entity tracking for combat messages
        /// </summary>
        /// <param name="message">Combat message to display</param>
        public static void WriteCombatLine(string message)
        {
            if (DisableAllUIOutput || UIConfig.DisableAllOutput) return;
            
            // Extract entity name from message (format: [EntityName] ...)
            string? currentEntity = ExtractEntityNameFromMessage(message);
            
            // Add line break if this is a different entity than the last one and spacing is enabled
            if (UIConfig.AddBlankLinesBetweenEntities && 
                currentEntity != null && lastActingEntity != null && currentEntity != lastActingEntity)
            {
                Console.WriteLine(); // Add blank line between different entities (no delay)
            }
            
            Console.WriteLine(message);
            
            // Update the last acting entity
            if (currentEntity != null)
            {
                lastActingEntity = currentEntity;
            }
            
            // Apply delay for combat messages using new configuration system
            ApplyDelay(UIMessageType.Combat);
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
        /// Writes a roll information message with 0.75 beat delay
        /// </summary>
        /// <param name="message">Roll information message to display</param>
        public static void WriteRollInfoLine(string message)
        {
            WriteLine(message, UIMessageType.RollInfo);
        }
        
        /// <summary>
        /// Writes a roll information message with 0.75 beat delay without updating entity tracking
        /// This is used for roll info that belongs to the previous combat action
        /// </summary>
        /// <param name="message">Roll information message to display</param>
        public static void WriteRollInfoLineNoEntityTracking(string message)
        {
            if (DisableAllUIOutput || UIConfig.DisableAllOutput) return;
            Console.WriteLine(message);
            ApplyDelay(UIMessageType.RollInfo);
        }
        
        /// <summary>
        /// Writes a stun message with effect message delay (backward compatibility)
        /// </summary>
        /// <param name="message">Stun message to display</param>
        public static void WriteStunLine(string message)
        {
            WriteEffectLine(message);
        }
        
        /// <summary>
        /// Writes a damage over time message with damage over time delay
        /// </summary>
        /// <param name="message">Damage over time message to display</param>
        public static void WriteDamageOverTimeLine(string message)
        {
            // Add blank line before damage over time if configured
            if (UIConfig.AddBlankLinesAfterDamageOverTime)
            {
                WriteBlankLine();
            }
            
            WriteLine(message, UIMessageType.DamageOverTime);
        }
        
        /// <summary>
        /// Resets entity tracking for a new battle
        /// </summary>
        public static void ResetForNewBattle()
        {
            lastActingEntity = null;
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
            
            // Calculate progressive delay: base delay minus 1ms per consecutive line
            int progressiveDelay = Math.Max(0, baseMenuDelay - consecutiveMenuLines);
            
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
        
        
        /// <summary>
        /// Extracts the entity name from a combat message
        /// </summary>
        /// <param name="message">The combat message</param>
        /// <returns>Entity name if found, null otherwise</returns>
        private static string? ExtractEntityNameFromMessage(string message)
        {
            if (string.IsNullOrEmpty(message)) return null;
            
            int startIndex = message.IndexOf('[');
            if (startIndex == -1) return null;
            
            int endIndex = message.IndexOf(']', startIndex);
            if (endIndex == -1) return null;
            
            return message.Substring(startIndex + 1, endIndex - startIndex - 1);
        }
    }
}
