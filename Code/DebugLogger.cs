using System;

namespace RPGGame
{
    /// <summary>
    /// Simple debug logging utility that provides basic logging capabilities
    /// </summary>
    public static class DebugLogger
    {
        /// <summary>
        /// Logs a general debug message
        /// </summary>
        /// <param name="message">The message to log</param>
        public static void Log(string message)
        {
            if (GameConfiguration.IsDebugEnabled)
            {
                UIManager.WriteSystemLine($"DEBUG: {message}");
            }
        }

        /// <summary>
        /// Logs a general debug message with context
        /// </summary>
        /// <param name="message">The message to log</param>
        /// <param name="context">The context</param>
        public static void Log(string message, string context)
        {
            if (GameConfiguration.IsDebugEnabled)
            {
                UIManager.WriteSystemLine($"DEBUG [{context}]: {message}");
            }
        }

        /// <summary>
        /// Logs method entry
        /// </summary>
        /// <param name="methodName">The method name</param>
        public static void LogMethodEntry(string methodName)
        {
            if (GameConfiguration.IsDebugEnabled)
            {
                UIManager.WriteSystemLine($"DEBUG [MethodEntry]: {methodName}");
            }
        }

        /// <summary>
        /// Logs method entry with context
        /// </summary>
        /// <param name="methodName">The method name</param>
        /// <param name="context">The context</param>
        public static void LogMethodEntry(string methodName, string context)
        {
            if (GameConfiguration.IsDebugEnabled)
            {
                UIManager.WriteSystemLine($"DEBUG [MethodEntry] [{context}]: {methodName}");
            }
        }

        /// <summary>
        /// Logs class points information
        /// </summary>
        /// <param name="message">The message to log</param>
        public static void LogClassPoints(string message)
        {
            if (GameConfiguration.IsDebugEnabled)
            {
                UIManager.WriteSystemLine($"DEBUG [ClassPoints]: {message}");
            }
        }

        /// <summary>
        /// Logs class points information with multiple parameters
        /// </summary>
        /// <param name="param1">First parameter</param>
        /// <param name="param2">Second parameter</param>
        /// <param name="param3">Third parameter</param>
        /// <param name="param4">Fourth parameter</param>
        public static void LogClassPoints(string param1, string param2, string param3, string param4)
        {
            if (GameConfiguration.IsDebugEnabled)
            {
                UIManager.WriteSystemLine($"DEBUG [ClassPoints]: {param1}, {param2}, {param3}, {param4}");
            }
        }

        /// <summary>
        /// Logs class points information with integer parameters
        /// </summary>
        /// <param name="param1">First parameter</param>
        /// <param name="param2">Second parameter</param>
        /// <param name="param3">Third parameter</param>
        /// <param name="param4">Fourth parameter</param>
        public static void LogClassPoints(int param1, int param2, int param3, int param4)
        {
            if (GameConfiguration.IsDebugEnabled)
            {
                UIManager.WriteSystemLine($"DEBUG [ClassPoints]: {param1}, {param2}, {param3}, {param4}");
            }
        }

        /// <summary>
        /// Logs action pool changes
        /// </summary>
        /// <param name="message">The message to log</param>
        public static void LogActionPoolChange(string message)
        {
            if (GameConfiguration.IsDebugEnabled)
            {
                UIManager.WriteSystemLine($"DEBUG [ActionPool]: {message}");
            }
        }

        /// <summary>
        /// Logs action pool changes with multiple parameters
        /// </summary>
        /// <param name="param1">First parameter</param>
        /// <param name="param2">Second parameter</param>
        /// <param name="param3">Third parameter</param>
        public static void LogActionPoolChange(string param1, string param2, string param3)
        {
            if (GameConfiguration.IsDebugEnabled)
            {
                UIManager.WriteSystemLine($"DEBUG [ActionPool]: {param1}, {param2}, {param3}");
            }
        }

        /// <summary>
        /// Logs action pool changes with mixed parameters
        /// </summary>
        /// <param name="param1">First parameter</param>
        /// <param name="param2">Second parameter (int)</param>
        public static void LogActionPoolChange(string param1, int param2)
        {
            if (GameConfiguration.IsDebugEnabled)
            {
                UIManager.WriteSystemLine($"DEBUG [ActionPool]: {param1}, {param2}");
            }
        }

        /// <summary>
        /// Logs action pool changes with mixed parameters
        /// </summary>
        /// <param name="param1">First parameter (int)</param>
        /// <param name="param2">Second parameter (int)</param>
        public static void LogActionPoolChange(int param1, int param2)
        {
            if (GameConfiguration.IsDebugEnabled)
            {
                UIManager.WriteSystemLine($"DEBUG [ActionPool]: {param1}, {param2}");
            }
        }

        /// <summary>
        /// Logs action pool changes with mixed parameters
        /// </summary>
        /// <param name="param1">First parameter (int)</param>
        /// <param name="param2">Second parameter (string)</param>
        public static void LogActionPoolChange(int param1, string param2)
        {
            if (GameConfiguration.IsDebugEnabled)
            {
                UIManager.WriteSystemLine($"DEBUG [ActionPool]: {param1}, {param2}");
            }
        }

        /// <summary>
        /// Logs action pool changes with mixed parameters
        /// </summary>
        /// <param name="param1">First parameter (string)</param>
        /// <param name="param2">Second parameter (int)</param>
        /// <param name="param3">Third parameter (string)</param>
        public static void LogActionPoolChange(string param1, int param2, string param3)
        {
            if (GameConfiguration.IsDebugEnabled)
            {
                UIManager.WriteSystemLine($"DEBUG [ActionPool]: {param1}, {param2}, {param3}");
            }
        }

        /// <summary>
        /// Logs formatted messages
        /// </summary>
        /// <param name="message">The message to log</param>
        public static void LogFormat(string message)
        {
            if (GameConfiguration.IsDebugEnabled)
            {
                UIManager.WriteSystemLine($"DEBUG [Format]: {message}");
            }
        }

        /// <summary>
        /// Logs formatted messages with multiple parameters
        /// </summary>
        /// <param name="param1">First parameter</param>
        /// <param name="param2">Second parameter</param>
        /// <param name="param3">Third parameter</param>
        public static void LogFormat(string param1, string param2, string param3)
        {
            if (GameConfiguration.IsDebugEnabled)
            {
                UIManager.WriteSystemLine($"DEBUG [Format]: {param1}, {param2}, {param3}");
            }
        }

        /// <summary>
        /// Logs formatted messages with four parameters
        /// </summary>
        /// <param name="param1">First parameter</param>
        /// <param name="param2">Second parameter</param>
        /// <param name="param3">Third parameter</param>
        /// <param name="param4">Fourth parameter</param>
        public static void LogFormat(string param1, string param2, string param3, string param4)
        {
            if (GameConfiguration.IsDebugEnabled)
            {
                UIManager.WriteSystemLine($"DEBUG [Format]: {param1}, {param2}, {param3}, {param4}");
            }
        }

        /// <summary>
        /// Logs formatted messages with mixed parameters
        /// </summary>
        /// <param name="param1">First parameter</param>
        /// <param name="param2">Second parameter (int)</param>
        public static void LogFormat(string param1, int param2)
        {
            if (GameConfiguration.IsDebugEnabled)
            {
                UIManager.WriteSystemLine($"DEBUG [Format]: {param1}, {param2}");
            }
        }

        /// <summary>
        /// Logs formatted messages with mixed parameters
        /// </summary>
        /// <param name="param1">First parameter (int)</param>
        /// <param name="param2">Second parameter (int)</param>
        public static void LogFormat(int param1, int param2)
        {
            if (GameConfiguration.IsDebugEnabled)
            {
                UIManager.WriteSystemLine($"DEBUG [Format]: {param1}, {param2}");
            }
        }

        /// <summary>
        /// Logs formatted messages with mixed parameters
        /// </summary>
        /// <param name="param1">First parameter</param>
        /// <param name="param2">Second parameter</param>
        /// <param name="param3">Third parameter (WeaponType)</param>
        public static void LogFormat(string param1, string param2, WeaponType param3)
        {
            if (GameConfiguration.IsDebugEnabled)
            {
                UIManager.WriteSystemLine($"DEBUG [Format]: {param1}, {param2}, {param3}");
            }
        }

        /// <summary>
        /// Logs formatted messages with mixed parameters
        /// </summary>
        /// <param name="param1">First parameter</param>
        /// <param name="param2">Second parameter</param>
        /// <param name="param3">Third parameter (int)</param>
        public static void LogFormat(string param1, string param2, int param3)
        {
            if (GameConfiguration.IsDebugEnabled)
            {
                UIManager.WriteSystemLine($"DEBUG [Format]: {param1}, {param2}, {param3}");
            }
        }

        /// <summary>
        /// Logs formatted messages with mixed parameters
        /// </summary>
        /// <param name="param1">First parameter</param>
        /// <param name="param2">Second parameter</param>
        /// <param name="param3">Third parameter (WeaponType)</param>
        /// <param name="param4">Fourth parameter</param>
        public static void LogFormat(string param1, string param2, WeaponType param3, string param4)
        {
            if (GameConfiguration.IsDebugEnabled)
            {
                UIManager.WriteSystemLine($"DEBUG [Format]: {param1}, {param2}, {param3}, {param4}");
            }
        }

        /// <summary>
        /// Logs formatted messages with mixed parameters
        /// </summary>
        /// <param name="param1">First parameter</param>
        /// <param name="param2">Second parameter</param>
        /// <param name="param3">Third parameter</param>
        /// <param name="param4">Fourth parameter (WeaponType)</param>
        public static void LogFormat(string param1, string param2, string param3, WeaponType param4)
        {
            if (GameConfiguration.IsDebugEnabled)
            {
                UIManager.WriteSystemLine($"DEBUG [Format]: {param1}, {param2}, {param3}, {param4}");
            }
        }

        /// <summary>
        /// Logs gear actions
        /// </summary>
        /// <param name="message">The message to log</param>
        public static void LogGearActions(string message)
        {
            if (GameConfiguration.IsDebugEnabled)
            {
                UIManager.WriteSystemLine($"DEBUG [GearActions]: {message}");
            }
        }

        /// <summary>
        /// Logs gear actions with multiple parameters
        /// </summary>
        /// <param name="param1">First parameter</param>
        /// <param name="param2">Second parameter</param>
        /// <param name="param3">Third parameter</param>
        public static void LogGearActions(string param1, string param2, string param3)
        {
            if (GameConfiguration.IsDebugEnabled)
            {
                UIManager.WriteSystemLine($"DEBUG [GearActions]: {param1}, {param2}, {param3}");
            }
        }

        /// <summary>
        /// Logs gear actions with mixed parameters
        /// </summary>
        /// <param name="param1">First parameter</param>
        /// <param name="param2">Second parameter (int)</param>
        public static void LogGearActions(string param1, int param2)
        {
            if (GameConfiguration.IsDebugEnabled)
            {
                UIManager.WriteSystemLine($"DEBUG [GearActions]: {param1}, {param2}");
            }
        }

        /// <summary>
        /// Logs gear actions with mixed parameters
        /// </summary>
        /// <param name="param1">First parameter (string)</param>
        /// <param name="param2">Second parameter (int)</param>
        /// <param name="param3">Third parameter (string)</param>
        public static void LogGearActions(string param1, int param2, string param3)
        {
            if (GameConfiguration.IsDebugEnabled)
            {
                UIManager.WriteSystemLine($"DEBUG [GearActions]: {param1}, {param2}, {param3}");
            }
        }
    }
}
