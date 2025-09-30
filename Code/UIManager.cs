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
        private static string? lastActingEntity = null;
        
        /// <summary>
        /// Writes a line to console with optional delay
        /// </summary>
        /// <param name="message">Message to display</param>
        /// <param name="delayType">Type of delay to apply</param>
        public static void WriteLine(string message, UIDelayType delayType = UIDelayType.None)
        {
            Console.WriteLine(message);
            ApplyDelay(delayType);
        }
        
        /// <summary>
        /// Writes text to console without newline
        /// </summary>
        /// <param name="message">Message to display</param>
        public static void Write(string message)
        {
            Console.Write(message);
        }
        
        /// <summary>
        /// Writes a line with entity tracking for combat messages
        /// </summary>
        /// <param name="message">Combat message to display</param>
        public static void WriteCombatLine(string message)
        {
            // Extract entity name from message (format: [EntityName] ...)
            string? currentEntity = ExtractEntityNameFromMessage(message);
            
            // Add line break if this is a different entity than the last one
            if (currentEntity != null && lastActingEntity != null && currentEntity != lastActingEntity)
            {
                Console.WriteLine(); // Add blank line between different entities (no delay)
            }
            
            Console.WriteLine(message);
            
            // Update the last acting entity
            if (currentEntity != null)
            {
                lastActingEntity = currentEntity;
            }
            
            // Apply delay for combat messages using TuningConfig values
            if (GameConfiguration.Instance.UI.EnableTextDelays)
            {
                // Check if Fast Combat is enabled - if so, set combat delays to zero
                int combatDelay = GameSettings.Instance.FastCombat ? 0 : GameConfiguration.Instance.UI.CombatDelay;
                if (combatDelay > 0)
                {
                    Thread.Sleep(combatDelay);
                }
            }
        }
        
        /// <summary>
        /// Writes multiple related messages as a group
        /// </summary>
        /// <param name="messages">Array of messages to display</param>
        public static void WriteGroup(string[] messages)
        {
            if (messages == null || messages.Length == 0) return;
            
            // Extract entity name from first message
            string? currentEntity = ExtractEntityNameFromMessage(messages[0]);
            
            // Add line break if this is a different entity than the last one
            if (currentEntity != null && lastActingEntity != null && currentEntity != lastActingEntity)
            {
                Console.WriteLine(); // Add blank line between different entities
            }
            
            // Write all messages in the group
            foreach (string message in messages)
            {
                Console.WriteLine(message);
            }
            
            // Update the last acting entity
            if (currentEntity != null)
            {
                lastActingEntity = currentEntity;
            }
            
            ApplyDelay(UIDelayType.Combat);
        }
        
        /// <summary>
        /// Writes a system message (no entity tracking)
        /// </summary>
        /// <param name="message">System message to display</param>
        public static void WriteSystemLine(string message)
        {
            Console.WriteLine(message);
            ApplyDelay(UIDelayType.System);
        }
        
        /// <summary>
        /// Writes a menu line with menu delay
        /// </summary>
        /// <param name="message">Menu message to display</param>
        public static void WriteMenuLine(string message)
        {
            Console.WriteLine(message);
            ApplyDelay(UIDelayType.Menu);
        }
        
        /// <summary>
        /// Writes a title line with title delay
        /// </summary>
        /// <param name="message">Title message to display</param>
        public static void WriteTitleLine(string message)
        {
            Console.WriteLine(message);
            ApplyDelay(UIDelayType.Title);
        }
        
        /// <summary>
        /// Writes a dungeon-related message with system delay
        /// </summary>
        /// <param name="message">Dungeon message to display</param>
        public static void WriteDungeonLine(string message)
        {
            Console.WriteLine(message);
            ApplyDelay(UIDelayType.System);
        }
        
        /// <summary>
        /// Writes a room-related message with system delay
        /// </summary>
        /// <param name="message">Room message to display</param>
        public static void WriteRoomLine(string message)
        {
            Console.WriteLine(message);
            ApplyDelay(UIDelayType.System);
        }
        
        /// <summary>
        /// Writes an enemy encounter message with system delay
        /// </summary>
        /// <param name="message">Enemy encounter message to display</param>
        public static void WriteEnemyLine(string message)
        {
            Console.WriteLine(message);
            ApplyDelay(UIDelayType.System);
        }
        
        /// <summary>
        /// Writes a room cleared message with system delay
        /// </summary>
        /// <param name="message">Room cleared message to display</param>
        public static void WriteRoomClearedLine(string message)
        {
            Console.WriteLine(message);
            ApplyDelay(UIDelayType.System);
        }
        
        /// <summary>
        /// Resets entity tracking for a new battle
        /// </summary>
        public static void ResetForNewBattle()
        {
            lastActingEntity = null;
        }
        
        /// <summary>
        /// Applies appropriate delay based on delay type
        /// </summary>
        /// <param name="delayType">Type of delay to apply</param>
        private static void ApplyDelay(UIDelayType delayType)
        {
            if (!GameConfiguration.Instance.UI.EnableTextDelays) return;
            
            // Check if Fast Combat is enabled - if so, set combat delays to zero
            bool fastCombat = GameSettings.Instance.FastCombat;
            
            int delayMs = delayType switch
            {
                UIDelayType.Combat => fastCombat ? 0 : GameConfiguration.Instance.UI.CombatDelay,
                UIDelayType.Menu => GameConfiguration.Instance.UI.MenuDelay,
                UIDelayType.System => GameConfiguration.Instance.UI.SystemDelay,
                UIDelayType.Title => GameConfiguration.Instance.UI.TitleDelay,
                UIDelayType.None => 0,
                _ => 0
            };
            
            if (delayMs > 0)
            {
                Thread.Sleep(delayMs);
            }
        }
        
        /// <summary>
        /// Applies a gap delay between actions
        /// NOTE: This method is deprecated - delays are now handled by WriteCombatLine to prevent accumulation
        /// </summary>
        public static void ApplyActionGap()
        {
            // Delays are now handled by WriteCombatLine to prevent accumulation
            // This method is kept for compatibility but does nothing
        }
        
        /// <summary>
        /// Writes a blank line without any delay
        /// </summary>
        public static void WriteBlankLine()
        {
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
    
    /// <summary>
    /// Types of UI delays available
    /// </summary>
    public enum UIDelayType
    {
        None,
        Combat,
        Menu,
        System,
        Title
    }
}
