using System;
using System.Threading;

namespace RPGGame
{
    /// <summary>
    /// Centralized combat logging system that handles all combat-related messages
    /// with consistent formatting, delays, and line breaks between different entities
    /// </summary>
    public static class CombatLogger
    {
        private static string? lastActingEntity = null;
        
        /// <summary>
        /// Logs a combat message with proper formatting and delays
        /// </summary>
        /// <param name="message">The combat message to log</param>
        public static void Log(string message)
        {
            // Extract entity name from message (format: [EntityName] ...)
            string? currentEntity = ExtractEntityNameFromMessage(message);
            
            // Add line break if this is a different entity than the last one
            if (currentEntity != null && lastActingEntity != null && currentEntity != lastActingEntity)
            {
                Console.WriteLine(); // Add blank line between different entities
            }
            
            Console.WriteLine(message);
            
            // Update the last acting entity
            if (currentEntity != null)
            {
                lastActingEntity = currentEntity;
            }
            
            // Apply delay if enabled
            var tuning = TuningConfig.Instance;
            if (tuning.UI.EnableTextDelays)
            {
                Thread.Sleep(tuning.UI.CombatLogDelay);
            }
        }
        
        /// <summary>
        /// Logs a combat message without entity tracking (for system messages)
        /// </summary>
        /// <param name="message">The message to log</param>
        public static void LogSystem(string message)
        {
            Console.WriteLine(message);
            
            var tuning = TuningConfig.Instance;
            if (tuning.UI.EnableTextDelays)
            {
                Thread.Sleep(tuning.UI.CombatLogDelay);
            }
        }
        
        /// <summary>
        /// Logs multiple related messages as a group (no line breaks between them)
        /// </summary>
        /// <param name="messages">Array of messages to log</param>
        public static void LogGroup(string[] messages)
        {
            if (messages == null || messages.Length == 0) return;
            
            // Extract entity name from first message
            string? currentEntity = ExtractEntityNameFromMessage(messages[0]);
            
            // Add line break if this is a different entity than the last one
            if (currentEntity != null && lastActingEntity != null && currentEntity != lastActingEntity)
            {
                Console.WriteLine(); // Add blank line between different entities
            }
            
            // Log all messages in the group
            foreach (string message in messages)
            {
                Console.WriteLine(message);
            }
            
            // Update the last acting entity
            if (currentEntity != null)
            {
                lastActingEntity = currentEntity;
            }
            
            // Apply delay if enabled
            var tuning = TuningConfig.Instance;
            if (tuning.UI.EnableTextDelays)
            {
                Thread.Sleep(tuning.UI.CombatLogDelay);
            }
        }
        
        /// <summary>
        /// Resets the entity tracking for a new battle
        /// </summary>
        public static void ResetForNewBattle()
        {
            lastActingEntity = null;
        }
        
        /// <summary>
        /// Extracts the entity name from a combat message
        /// </summary>
        /// <param name="message">The combat message</param>
        /// <returns>The entity name if found, null otherwise</returns>
        private static string? ExtractEntityNameFromMessage(string message)
        {
            // Look for pattern [EntityName] at the start of the message
            if (message.StartsWith("[") && message.Contains("]"))
            {
                int endBracket = message.IndexOf("]");
                if (endBracket > 1)
                {
                    return message.Substring(1, endBracket - 1);
                }
            }
            return null;
        }
    }
}
