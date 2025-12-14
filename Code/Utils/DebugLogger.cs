using System;
using System.IO;
using RPGGame.Utils;

namespace RPGGame
{
    /// <summary>
    /// Centralized debug logging system that writes to debug analysis files
    /// Uses AsyncEventLogger for non-blocking file I/O
    /// </summary>
    public static class DebugLogger
    {
        private static string? _currentDebugFile = null;
        private static readonly object _lockObject = new object();
        private static AsyncEventLogger? _asyncLogger = null;

        /// <summary>
        /// Writes a debug message to the debug analysis file (always writes, regardless of debug setting)
        /// </summary>
        /// <param name="message">The debug message to write</param>
        public static void WriteDebugAlways(string message)
        {
            WriteDebugInternal(message);
        }

        /// <summary>
        /// Writes a debug message to the debug analysis file
        /// </summary>
        /// <param name="message">The debug message to write</param>
        public static void WriteDebug(string message)
        {
            if (!GameConfiguration.IsDebugEnabled)
                return;
                
            WriteDebugInternal(message);
        }

        /// <summary>
        /// Writes a debug message only if LogCombatActions is enabled
        /// </summary>
        /// <param name="category">The debug category</param>
        /// <param name="message">The debug message to write</param>
        public static void WriteCombatDebug(string category, string message)
        {
            if (!GameConfiguration.IsDebugEnabled || !GameConfiguration.Instance.Debug.LogCombatActions)
                return;
                
            WriteDebugInternal($"[{category}] {message}");
        }

        /// <summary>
        /// Writes a debug message only if LogCombatActions is enabled (single parameter version)
        /// </summary>
        /// <param name="message">The debug message to write</param>
        public static void WriteCombatDebug(string message)
        {
            if (!GameConfiguration.IsDebugEnabled || !GameConfiguration.Instance.Debug.LogCombatActions)
                return;
                
            WriteDebugInternal(message);
        }

        /// <summary>
        /// Writes a debug message only if LogOnlyOnErrors is enabled
        /// </summary>
        /// <param name="message">The debug message to write</param>
        public static void WriteErrorDebug(string message)
        {
            if (!GameConfiguration.IsDebugEnabled || !GameConfiguration.Instance.Debug.LogOnlyOnErrors)
                return;
                
            WriteDebugInternal(message);
        }

        /// <summary>
        /// Internal method that actually writes the debug message
        /// Uses AsyncEventLogger for non-blocking file I/O
        /// </summary>
        /// <param name="message">The debug message to write</param>
        private static void WriteDebugInternal(string message)
        {
            lock (_lockObject)
            {
                try
                {
                    // Always use Code/DebugAnalysis relative to the project root
                    string currentDir = Directory.GetCurrentDirectory();
                    string debugDir;
                    
                    // If we're in the Code directory, use DebugAnalysis directly
                    if (currentDir.EndsWith("Code", StringComparison.OrdinalIgnoreCase))
                    {
                        debugDir = Path.Combine(currentDir, "DebugAnalysis");
                    }
                    // If we're in the project root, use Code/DebugAnalysis
                    else if (Directory.Exists(Path.Combine(currentDir, "Code")))
                    {
                        debugDir = Path.Combine(currentDir, "Code", "DebugAnalysis");
                    }
                    // Fallback to relative path
                    else
                    {
                        debugDir = Path.Combine("Code", "DebugAnalysis");
                    }
                    
                    // Debug path info removed from console output
                    if (!Directory.Exists(debugDir))
                    {
                        Directory.CreateDirectory(debugDir);
                    }
                    
                    // Create a single debug file per game session
                    if (_currentDebugFile == null)
                    {
                        string timestamp = DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss");
                        _currentDebugFile = Path.Combine(debugDir, $"debug_analysis_{timestamp}.txt");
                        
                        // Initialize async logger
                        _asyncLogger = new AsyncEventLogger(_currentDebugFile, batchSize: 10, batchTimeoutMs: 1000);
                        
                        // Write session header synchronously (only once)
                        File.WriteAllText(_currentDebugFile, $"DEBUG ANALYSIS SESSION - {DateTime.Now:yyyy-MM-dd HH:mm:ss}\n");
                        File.AppendAllText(_currentDebugFile, "=" + new string('=', 50) + "\n\n");
                        // Debug file creation confirmation removed from console output
                    }
                    
                    // Use async logger for non-blocking writes
                    _asyncLogger?.LogAsync(message);
                }
                catch
                {
                    // Ignore file write errors - debug output is optional
                }
            }
        }
        
        /// <summary>
        /// Flushes all pending log messages (for shutdown or critical points)
        /// </summary>
        public static void Flush()
        {
            _asyncLogger?.Flush();
        }
        
        /// <summary>
        /// Disposes the async logger (call on application shutdown)
        /// </summary>
        public static void Dispose()
        {
            _asyncLogger?.Dispose();
            _asyncLogger = null;
            _currentDebugFile = null;
        }

        /// <summary>
        /// Writes a debug message with a specific component tag (always writes, regardless of debug setting)
        /// </summary>
        /// <param name="component">The component generating the debug message</param>
        /// <param name="message">The debug message to write</param>
        public static void WriteDebugAlways(string component, string message)
        {
            WriteDebugInternal($"DEBUG [{component}]: {message}");
        }

        /// <summary>
        /// Writes a debug message with a specific component tag
        /// </summary>
        /// <param name="component">The component generating the debug message</param>
        /// <param name="message">The debug message to write</param>
        public static void WriteDebug(string component, string message)
        {
            WriteDebug($"DEBUG [{component}]: {message}");
        }

        /// <summary>
        /// Writes a debug message with method entry information
        /// </summary>
        /// <param name="component">The component generating the debug message</param>
        /// <param name="methodName">The method name</param>
        public static void WriteMethodEntry(string component, string methodName)
        {
            WriteDebug($"DEBUG [MethodEntry] [{methodName}]: {component}");
        }

        /// <summary>
        /// Writes a debug message with formatted parameters
        /// </summary>
        /// <param name="component">The component generating the debug message</param>
        /// <param name="message">The debug message format</param>
        /// <param name="args">The format arguments</param>
        public static void WriteDebugFormat(string component, string message, params object[] args)
        {
            WriteDebug($"DEBUG [Format]: {component}, {string.Format(message, args)}");
        }
        
        // Legacy method names for compatibility
        public static void LogMethodEntry(string component, string methodName) => WriteMethodEntry(component, methodName);
        public static void Log(string component, string message) => WriteDebug(component, message);
        public static void LogFormat(string component, string message, params object[] args) => WriteDebugFormat(component, message, args);
        public static void LogClassPoints(string component, string message) => WriteDebug(component, message);
        public static void LogActionPoolChange(string component, string message) => WriteDebug(component, message);
        public static void LogGearActions(string component, string message) => WriteDebug(component, message);
        
        // Overloads for different parameter signatures
        public static void LogClassPoints(int barbarian, int warrior, int rogue, int wizard) => WriteDebug("CharacterActions", $"ClassPoints: {barbarian}, {warrior}, {rogue}, {wizard}");
        public static void LogActionPoolChange(string name, int count, string context) => WriteDebug("CharacterActions", $"ActionPool: {name}, {count}, {context}");
        public static void LogGearActions(string gearName, int count, string actions) => WriteDebug("CharacterActions", $"GearActions: {gearName}, {count}, {actions}");
    }
}