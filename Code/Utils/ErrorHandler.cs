using System;

namespace RPGGame
{
    /// <summary>
    /// Centralized error handling utility that provides common error handling patterns
    /// Eliminates duplication of try-catch blocks across the codebase
    /// </summary>
    public static class ErrorHandler
    {
        /// <summary>
        /// Executes an action with error handling and logging
        /// </summary>
        /// <param name="action">The action to execute</param>
        /// <param name="operationName">The name of the operation for logging</param>
        /// <param name="fallbackAction">Optional fallback action to execute on error</param>
        /// <returns>True if successful, false if an error occurred</returns>
        public static bool TryExecute(System.Action action, string operationName, System.Action? fallbackAction = null)
        {
            try
            {
                action();
                return true;
            }
            catch (Exception ex)
            {
                UIManager.WriteSystemLine($"Error in {operationName}: {ex.Message}");
                if (GameConfiguration.IsDebugEnabled)
                {
                    UIManager.WriteSystemLine($"Stack trace: {ex.StackTrace}");
                }
                
                fallbackAction?.Invoke();
                return false;
            }
        }

        /// <summary>
        /// Executes a function with error handling and logging
        /// </summary>
        /// <typeparam name="T">The return type</typeparam>
        /// <param name="func">The function to execute</param>
        /// <param name="operationName">The name of the operation for logging</param>
        /// <param name="fallbackValue">The value to return if an error occurs</param>
        /// <returns>The result of the function or the fallback value</returns>
        public static T TryExecute<T>(Func<T> func, string operationName, T fallbackValue = default(T)!)
        {
            try
            {
                return func();
            }
            catch (Exception ex)
            {
                UIManager.WriteSystemLine($"Error in {operationName}: {ex.Message}");
                if (GameConfiguration.IsDebugEnabled)
                {
                    UIManager.WriteSystemLine($"Stack trace: {ex.StackTrace}");
                }
                return fallbackValue;
            }
        }

        /// <summary>
        /// Executes a function with error handling and logging, with a fallback function
        /// </summary>
        /// <typeparam name="T">The return type</typeparam>
        /// <param name="func">The function to execute</param>
        /// <param name="operationName">The name of the operation for logging</param>
        /// <param name="fallbackFunc">The fallback function to execute on error</param>
        /// <returns>The result of the function or the fallback function</returns>
        public static T TryExecute<T>(Func<T> func, string operationName, Func<T> fallbackFunc)
        {
            try
            {
                return func();
            }
            catch (Exception ex)
            {
                UIManager.WriteSystemLine($"Error in {operationName}: {ex.Message}");
                if (GameConfiguration.IsDebugEnabled)
                {
                    UIManager.WriteSystemLine($"Stack trace: {ex.StackTrace}");
                }
                return fallbackFunc();
            }
        }

        /// <summary>
        /// Executes a JSON loading operation with error handling
        /// </summary>
        /// <typeparam name="T">The type to deserialize to</typeparam>
        /// <param name="loadFunc">The function that loads the JSON</param>
        /// <param name="fileName">The name of the file being loaded</param>
        /// <param name="fallbackValue">The value to return if loading fails</param>
        /// <returns>The loaded object or fallback value</returns>
        public static T TryLoadJson<T>(Func<T> loadFunc, string fileName, T fallbackValue = default(T)!)
        {
            try
            {
                return loadFunc();
            }
            catch (Exception ex)
            {
                UIManager.WriteSystemLine($"Error loading {fileName}: {ex.Message}");
                if (GameConfiguration.IsDebugEnabled)
                {
                    UIManager.WriteSystemLine($"Stack trace: {ex.StackTrace}");
                }
                return fallbackValue;
            }
        }

        /// <summary>
        /// Executes a JSON saving operation with error handling
        /// </summary>
        /// <param name="saveAction">The action that saves the JSON</param>
        /// <param name="fileName">The name of the file being saved</param>
        /// <returns>True if successful, false if an error occurred</returns>
        public static bool TrySaveJson(System.Action saveAction, string fileName)
        {
            try
            {
                saveAction();
                return true;
            }
            catch (Exception ex)
            {
                UIManager.WriteSystemLine($"Error saving {fileName}: {ex.Message}");
                if (GameConfiguration.IsDebugEnabled)
                {
                    UIManager.WriteSystemLine($"Stack trace: {ex.StackTrace}");
                }
                return false;
            }
        }

        /// <summary>
        /// Executes a file operation with error handling
        /// </summary>
        /// <param name="fileOperation">The file operation to execute</param>
        /// <param name="operationName">The name of the operation for logging</param>
        /// <param name="fallbackAction">Optional fallback action to execute on error</param>
        /// <returns>True if successful, false if an error occurred</returns>
        public static bool TryFileOperation(System.Action fileOperation, string operationName, System.Action? fallbackAction = null)
        {
            try
            {
                fileOperation();
                return true;
            }
            catch (Exception ex)
            {
                UIManager.WriteSystemLine($"Error in file operation '{operationName}': {ex.Message}");
                if (GameConfiguration.IsDebugEnabled)
                {
                    UIManager.WriteSystemLine($"Stack trace: {ex.StackTrace}");
                }
                
                fallbackAction?.Invoke();
                return false;
            }
        }

        /// <summary>
        /// Logs an error with context information
        /// </summary>
        /// <param name="ex">The exception to log</param>
        /// <param name="context">The context where the error occurred</param>
        /// <param name="additionalInfo">Additional information about the error</param>
        public static void LogError(Exception ex, string context, string additionalInfo = "")
        {
            UIManager.WriteSystemLine($"Error in {context}: {ex.Message}");
            if (!string.IsNullOrEmpty(additionalInfo))
            {
                UIManager.WriteSystemLine($"Additional info: {additionalInfo}");
            }
            if (GameConfiguration.IsDebugEnabled)
            {
                UIManager.WriteSystemLine($"Stack trace: {ex.StackTrace}");
            }
        }

        /// <summary>
        /// Logs a warning message
        /// </summary>
        /// <param name="message">The warning message</param>
        /// <param name="context">The context where the warning occurred</param>
        public static void LogWarning(string message, string context = "")
        {
            string fullMessage = string.IsNullOrEmpty(context) ? message : $"{context}: {message}";
            UIManager.WriteSystemLine($"Warning: {fullMessage}");
        }

        /// <summary>
        /// Logs an info message
        /// </summary>
        /// <param name="message">The info message</param>
        /// <param name="context">The context where the info occurred</param>
        public static void LogInfo(string message, string context = "")
        {
            string fullMessage = string.IsNullOrEmpty(context) ? message : $"{context}: {message}";
            UIManager.WriteSystemLine($"Info: {fullMessage}");
        }
    }
}
