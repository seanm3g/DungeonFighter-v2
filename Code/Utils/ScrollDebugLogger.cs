using System;
using System.IO;
using RPGGame;

namespace RPGGame.Utils
{
    /// <summary>
    /// Simple debug logger for scroll debugging that writes to a file
    /// </summary>
    public static class ScrollDebugLogger
    {
        private static readonly string LogFile = Path.Combine(Directory.GetCurrentDirectory(), "scroll_debug.log");
        private static readonly object LockObject = new object();

        public static void Log(string message)
        {
            // Only log if debug output is enabled
            if (!GameConfiguration.IsDebugEnabled)
                return;

            lock (LockObject)
            {
                try
                {
                    var timestamp = DateTime.Now.ToString("HH:mm:ss.fff");
                    var logMessage = $"[{timestamp}] {message}";
                    File.AppendAllText(LogFile, logMessage + "\n");
                    Console.WriteLine(logMessage); // Also write to console if available
                }
                catch
                {
                    // Ignore errors
                }
            }
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

