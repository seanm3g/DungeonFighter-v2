using System;
using System.IO;
using RPGGame;

namespace RPGGame.Utils
{
    /// <summary>
    /// Simple debug logger for scroll debugging that writes to a file
    /// Uses AsyncEventLogger for non-blocking file I/O
    /// </summary>
    public static class ScrollDebugLogger
    {
        private static readonly string LogFile = Path.Combine(Directory.GetCurrentDirectory(), "scroll_debug.log");
        private static readonly object LockObject = new object();
        private static AsyncEventLogger? _asyncLogger = null;

        public static void Log(string message)
        {
            // Only log if debug output is enabled
            if (!GameConfiguration.IsDebugEnabled)
                return;

            lock (LockObject)
            {
                try
                {
                    // Initialize async logger on first use
                    if (_asyncLogger == null)
                    {
                        _asyncLogger = new AsyncEventLogger(LogFile, batchSize: 10, batchTimeoutMs: 1000);
                    }
                    
                    var timestamp = DateTime.Now.ToString("HH:mm:ss.fff");
                    var logMessage = $"[{timestamp}] {message}";
                    _asyncLogger.LogAsync(logMessage);
                    Console.WriteLine(logMessage); // Also write to console if available
                }
                catch
                {
                    // Ignore errors
                }
            }
        }
        
        /// <summary>
        /// Flushes all pending log messages
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
        }

        public static void Clear()
        {
            lock (LockObject)
            {
                try
                {
                    if (File.Exists(LogFile))
                    {
                        File.Delete(LogFile);
                    }
                }
                catch
                {
                    // Ignore errors
                }
            }
        }
    }
}

