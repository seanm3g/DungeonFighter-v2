using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace RPGGame.Utils
{
    /// <summary>
    /// Enhanced error handling utility with comprehensive logging and recovery options
    /// Provides structured error handling patterns across the codebase
    /// </summary>
    public static class EnhancedErrorHandler
    {
        private static readonly List<ErrorLogEntry> _errorLog = new List<ErrorLogEntry>();
        private static readonly object _logLock = new object();
        private static bool _enableDetailedLogging = true;
        private static bool _enableFileLogging = false;
        private static string _logFilePath = "error_log.txt";
        
        /// <summary>
        /// Configuration for error handling behavior
        /// </summary>
        public static class Config
        {
            public static bool EnableDetailedLogging
            {
                get => _enableDetailedLogging;
                set => _enableDetailedLogging = value;
            }
            
            public static bool EnableFileLogging
            {
                get => _enableFileLogging;
                set => _enableFileLogging = value;
            }
            
            public static string LogFilePath
            {
                get => _logFilePath;
                set => _logFilePath = value ?? throw new ArgumentNullException(nameof(value));
            }
        }
        
        /// <summary>
        /// Logs an error with detailed information
        /// </summary>
        /// <param name="error">The exception that occurred</param>
        /// <param name="context">Additional context about where the error occurred</param>
        /// <param name="severity">The severity level of the error</param>
        public static void LogError(Exception error, string context = "", ErrorSeverity severity = ErrorSeverity.Error)
        {
            var entry = new ErrorLogEntry
            {
                Timestamp = DateTime.Now,
                Exception = error,
                Context = context,
                Severity = severity,
                StackTrace = error.StackTrace ?? ""
            };
            
            LogEntry(entry);
        }
        
        /// <summary>
        /// Logs a warning message
        /// </summary>
        /// <param name="message">The warning message</param>
        /// <param name="context">Additional context about where the warning occurred</param>
        public static void LogWarning(string message, string context = "")
        {
            var entry = new ErrorLogEntry
            {
                Timestamp = DateTime.Now,
                Exception = new Exception(message),
                Context = context,
                Severity = ErrorSeverity.Warning,
                StackTrace = ""
            };
            
            LogEntry(entry);
        }
        
        /// <summary>
        /// Logs an informational message
        /// </summary>
        /// <param name="message">The informational message</param>
        /// <param name="context">Additional context about where the message occurred</param>
        public static void LogInfo(string message, string context = "")
        {
            var entry = new ErrorLogEntry
            {
                Timestamp = DateTime.Now,
                Exception = new Exception(message),
                Context = context,
                Severity = ErrorSeverity.Info,
                StackTrace = ""
            };
            
            LogEntry(entry);
        }
        
        /// <summary>
        /// Executes an action with error handling and optional recovery
        /// </summary>
        /// <param name="action">The action to execute</param>
        /// <param name="context">Context for error reporting</param>
        /// <param name="recoveryAction">Optional recovery action to execute on error</param>
        /// <param name="fallbackValue">Fallback value to return on error</param>
        /// <returns>True if the action executed successfully, false otherwise</returns>
        public static bool TryExecute(System.Action action, string context = "", System.Action? recoveryAction = null, bool fallbackValue = false)
        {
            try
            {
                action();
                return true;
            }
            catch (Exception ex)
            {
                LogError(ex, context);
                recoveryAction?.Invoke();
                return fallbackValue;
            }
        }
        
        /// <summary>
        /// Executes a function with error handling and optional recovery
        /// </summary>
        /// <typeparam name="T">The return type of the function</typeparam>
        /// <param name="func">The function to execute</param>
        /// <param name="context">Context for error reporting</param>
        /// <param name="recoveryAction">Optional recovery action to execute on error</param>
        /// <param name="fallbackValue">Fallback value to return on error</param>
        /// <returns>The result of the function or the fallback value</returns>
        public static T TryExecute<T>(System.Func<T> func, string context = "", System.Action? recoveryAction = null, T fallbackValue = default(T)!)
        {
            try
            {
                return func();
            }
            catch (Exception ex)
            {
                LogError(ex, context);
                recoveryAction?.Invoke();
                return fallbackValue;
            }
        }
        
        /// <summary>
        /// Executes an action with retry logic (async version)
        /// </summary>
        /// <param name="action">The action to execute</param>
        /// <param name="maxRetries">Maximum number of retry attempts</param>
        /// <param name="retryDelayMs">Delay between retries in milliseconds</param>
        /// <param name="context">Context for error reporting</param>
        /// <returns>True if the action succeeded within retry limit, false otherwise</returns>
        public static async Task<bool> TryExecuteWithRetryAsync(System.Action action, int maxRetries = 3, int retryDelayMs = 1000, string context = "")
        {
            for (int attempt = 0; attempt <= maxRetries; attempt++)
            {
                try
                {
                    action();
                    return true;
                }
                catch (Exception ex)
                {
                    if (attempt == maxRetries)
                    {
                        LogError(ex, $"{context} (Failed after {maxRetries + 1} attempts)");
                        return false;
                    }
                    
                    LogWarning($"Attempt {attempt + 1} failed: {ex.Message}. Retrying in {retryDelayMs}ms...", context);
                    
                    if (retryDelayMs > 0)
                    {
                        await System.Threading.Tasks.Task.Delay(retryDelayMs);
                    }
                }
            }
            
            return false;
        }
        
        
        /// <summary>
        /// Validates a condition and throws an exception if it fails
        /// </summary>
        /// <param name="condition">The condition to validate</param>
        /// <param name="message">The error message if validation fails</param>
        /// <param name="context">Additional context for error reporting</param>
        /// <exception cref="InvalidOperationException">Thrown when validation fails</exception>
        public static void ValidateCondition(bool condition, string message, string context = "")
        {
            if (!condition)
            {
                var exception = new InvalidOperationException(message);
                LogError(exception, context);
                throw exception;
            }
        }
        
        /// <summary>
        /// Gets the current error log
        /// </summary>
        /// <returns>A copy of the current error log</returns>
        public static List<ErrorLogEntry> GetErrorLog()
        {
            lock (_logLock)
            {
                return new List<ErrorLogEntry>(_errorLog);
            }
        }
        
        /// <summary>
        /// Clears the error log
        /// </summary>
        public static void ClearErrorLog()
        {
            lock (_logLock)
            {
                _errorLog.Clear();
            }
        }
        
        /// <summary>
        /// Exports the error log to a file
        /// </summary>
        /// <param name="filePath">The path to export the log to</param>
        /// <returns>True if export was successful, false otherwise</returns>
        public static bool ExportErrorLog(string filePath)
        {
            try
            {
                var logEntries = GetErrorLog();
                var logContent = new StringBuilder();
                
                logContent.AppendLine("=== Error Log Export ===");
                logContent.AppendLine($"Export Date: {DateTime.Now}");
                logContent.AppendLine($"Total Entries: {logEntries.Count}");
                logContent.AppendLine();
                
                foreach (var entry in logEntries)
                {
                    logContent.AppendLine($"Timestamp: {entry.Timestamp}");
                    logContent.AppendLine($"Severity: {entry.Severity}");
                    logContent.AppendLine($"Context: {entry.Context}");
                    logContent.AppendLine($"Message: {entry.Exception.Message}");
                    if (!string.IsNullOrEmpty(entry.StackTrace))
                    {
                        logContent.AppendLine($"Stack Trace: {entry.StackTrace}");
                    }
                    logContent.AppendLine(new string('-', 50));
                }
                
                File.WriteAllText(filePath, logContent.ToString());
                return true;
            }
            catch (Exception ex)
            {
                LogError(ex, $"Failed to export error log to {filePath}");
                return false;
            }
        }
        
        /// <summary>
        /// Gets error statistics
        /// </summary>
        /// <returns>Error statistics summary</returns>
        public static ErrorStatistics GetErrorStatistics()
        {
            var logEntries = GetErrorLog();
            var stats = new ErrorStatistics();
            
            foreach (var entry in logEntries)
            {
                stats.TotalErrors++;
                
                switch (entry.Severity)
                {
                    case ErrorSeverity.Info:
                        stats.InfoCount++;
                        break;
                    case ErrorSeverity.Warning:
                        stats.WarningCount++;
                        break;
                    case ErrorSeverity.Error:
                        stats.ErrorCount++;
                        break;
                    case ErrorSeverity.Critical:
                        stats.CriticalCount++;
                        break;
                }
            }
            
            return stats;
        }
        
        private static void LogEntry(ErrorLogEntry entry)
        {
            lock (_logLock)
            {
                _errorLog.Add(entry);
                
                // Keep only the last 1000 entries to prevent memory issues
                if (_errorLog.Count > 1000)
                {
                    _errorLog.RemoveAt(0);
                }
            }
            
            // Console output
            var severityPrefix = entry.Severity switch
            {
                ErrorSeverity.Info => "[INFO]",
                ErrorSeverity.Warning => "[WARN]",
                ErrorSeverity.Error => "[ERROR]",
                ErrorSeverity.Critical => "[CRITICAL]",
                _ => "[UNKNOWN]"
            };
            
            Console.WriteLine($"{severityPrefix} {entry.Timestamp:yyyy-MM-dd HH:mm:ss} - {entry.Context}: {entry.Exception.Message}");
            
            // File output
            if (_enableFileLogging)
            {
                try
                {
                    var logLine = $"{entry.Timestamp:yyyy-MM-dd HH:mm:ss} [{entry.Severity}] {entry.Context}: {entry.Exception.Message}";
                    File.AppendAllText(_logFilePath, logLine + System.Environment.NewLine);
                }
                catch
                {
                    // Ignore file logging errors to prevent infinite loops
                }
            }
        }
        
        /// <summary>
        /// Error severity levels
        /// </summary>
        public enum ErrorSeverity
        {
            Info,
            Warning,
            Error,
            Critical
        }
        
        /// <summary>
        /// Represents an error log entry
        /// </summary>
        public class ErrorLogEntry
        {
            public DateTime Timestamp { get; set; }
            public Exception Exception { get; set; } = new Exception();
            public string Context { get; set; } = "";
            public ErrorSeverity Severity { get; set; }
            public string StackTrace { get; set; } = "";
        }
        
        /// <summary>
        /// Error statistics summary
        /// </summary>
        public class ErrorStatistics
        {
            public int TotalErrors { get; set; }
            public int InfoCount { get; set; }
            public int WarningCount { get; set; }
            public int ErrorCount { get; set; }
            public int CriticalCount { get; set; }
        }
    }
}
