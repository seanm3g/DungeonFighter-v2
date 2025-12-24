using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;

namespace RPGGame.UI.Avalonia.Utils
{
    /// <summary>
    /// Helper class for debug logging operations
    /// Extracted from CanvasUICoordinator to reduce complexity
    /// </summary>
    public static class DebugLoggingHelper
    {
        private const string DebugLogPath = @"d:\Code Projects\github projects\DungeonFighter-v2\.cursor\debug.log";

        /// <summary>
        /// Logs a debug message to the debug log file
        /// </summary>
        public static void LogDebug(string sessionId, string runId, string hypothesisId, string location, string message, object? data = null)
        {
            try
            {
                var logEntry = new
                {
                    sessionId,
                    runId,
                    hypothesisId,
                    location,
                    message,
                    data,
                    timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()
                };
                
                File.AppendAllText(DebugLogPath, JsonSerializer.Serialize(logEntry) + "\n");
            }
            catch
            {
                // Silently ignore logging errors
            }
        }

        /// <summary>
        /// Logs GetDisplayBufferText entry
        /// </summary>
        public static void LogGetDisplayBufferTextEntry(string? textManagerType)
        {
            LogDebug("debug-session", "run1", "B", "CanvasUICoordinator.cs:343", 
                "GetDisplayBufferText entry", 
                new { textManagerType = textManagerType ?? "null" });
        }

        /// <summary>
        /// Logs GetDisplayBufferText messages retrieved
        /// </summary>
        public static void LogGetDisplayBufferTextMessagesRetrieved(int? messageCount, int? firstMessageLength)
        {
            LogDebug("debug-session", "run1", "B", "CanvasUICoordinator.cs:347", 
                "GetDisplayBufferText - messages retrieved", 
                new { messageCount = messageCount ?? 0, firstMessageLength = firstMessageLength ?? 0 });
        }

        /// <summary>
        /// Logs GetDisplayBufferText result
        /// </summary>
        public static void LogGetDisplayBufferTextResult(int? resultLength)
        {
            LogDebug("debug-session", "run1", "B", "CanvasUICoordinator.cs:350", 
                "GetDisplayBufferText - result", 
                new { resultLength = resultLength ?? 0 });
        }

        /// <summary>
        /// Logs GetDisplayBufferText returning empty
        /// </summary>
        public static void LogGetDisplayBufferTextReturningEmpty()
        {
            LogDebug("debug-session", "run1", "B", "CanvasUICoordinator.cs:355", 
                "GetDisplayBufferText - returning empty", 
                new { });
        }
    }
}

