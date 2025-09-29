using System;

namespace RPGGame
{
    /// <summary>
    /// Centralized debug logging system that handles all debug output
    /// Replaces scattered Console.WriteLine debug statements across the codebase
    /// </summary>
    public static class DebugLogger
    {
        /// <summary>
        /// Logs a debug message if debug mode is enabled
        /// </summary>
        /// <param name="message">The debug message to log</param>
        public static void Log(string message)
        {
            if (TuningConfig.IsDebugEnabled)
            {
                UIManager.WriteLine($"DEBUG: {message}");
            }
        }

        /// <summary>
        /// Logs a debug message with a specific context
        /// </summary>
        /// <param name="context">The context or class name</param>
        /// <param name="message">The debug message to log</param>
        public static void Log(string context, string message)
        {
            if (TuningConfig.IsDebugEnabled)
            {
                UIManager.WriteLine($"DEBUG [{context}]: {message}");
            }
        }

        /// <summary>
        /// Logs a debug message with formatted parameters
        /// </summary>
        /// <param name="message">The debug message template</param>
        /// <param name="args">Arguments to format into the message</param>
        public static void LogFormat(string message, params object[] args)
        {
            if (TuningConfig.IsDebugEnabled)
            {
                UIManager.WriteLine($"DEBUG: {string.Format(message, args)}");
            }
        }

        /// <summary>
        /// Logs a debug message with context and formatted parameters
        /// </summary>
        /// <param name="context">The context or class name</param>
        /// <param name="message">The debug message template</param>
        /// <param name="args">Arguments to format into the message</param>
        public static void LogFormat(string context, string message, params object[] args)
        {
            if (TuningConfig.IsDebugEnabled)
            {
                UIManager.WriteLine($"DEBUG [{context}]: {string.Format(message, args)}");
            }
        }

        /// <summary>
        /// Logs method entry for debugging
        /// </summary>
        /// <param name="methodName">The name of the method being entered</param>
        public static void LogMethodEntry(string methodName)
        {
            if (TuningConfig.IsDebugEnabled)
            {
                UIManager.WriteLine($"DEBUG: Entering {methodName}");
            }
        }

        /// <summary>
        /// Logs method entry with context
        /// </summary>
        /// <param name="context">The context or class name</param>
        /// <param name="methodName">The name of the method being entered</param>
        public static void LogMethodEntry(string context, string methodName)
        {
            if (TuningConfig.IsDebugEnabled)
            {
                UIManager.WriteLine($"DEBUG [{context}]: Entering {methodName}");
            }
        }

        /// <summary>
        /// Logs a debug message about action pool changes
        /// </summary>
        /// <param name="entityName">The name of the entity</param>
        /// <param name="actionCount">The current action count</param>
        /// <param name="operation">The operation being performed</param>
        public static void LogActionPoolChange(string entityName, int actionCount, string operation)
        {
            if (TuningConfig.IsDebugEnabled)
            {
                UIManager.WriteLine($"DEBUG: {operation} - {entityName} now has {actionCount} actions");
            }
        }

        /// <summary>
        /// Logs a debug message about gear actions
        /// </summary>
        /// <param name="gearName">The name of the gear item</param>
        /// <param name="actionCount">The number of actions found</param>
        /// <param name="actionNames">The names of the actions</param>
        public static void LogGearActions(string gearName, int actionCount, string actionNames)
        {
            if (TuningConfig.IsDebugEnabled)
            {
                UIManager.WriteLine($"DEBUG: GetGearActions returned {actionCount} actions for {gearName}: {actionNames}");
            }
        }

        /// <summary>
        /// Logs a debug message about class points
        /// </summary>
        /// <param name="barbarianPoints">Barbarian class points</param>
        /// <param name="warriorPoints">Warrior class points</param>
        /// <param name="roguePoints">Rogue class points</param>
        /// <param name="wizardPoints">Wizard class points</param>
        public static void LogClassPoints(int barbarianPoints, int warriorPoints, int roguePoints, int wizardPoints)
        {
            if (TuningConfig.IsDebugEnabled)
            {
                UIManager.WriteLine($"DEBUG: Class points - Barbarian: {barbarianPoints}, Warrior: {warriorPoints}, Rogue: {roguePoints}, Wizard: {wizardPoints}");
            }
        }
    }
}
