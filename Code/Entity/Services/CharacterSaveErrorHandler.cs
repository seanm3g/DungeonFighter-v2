using RPGGame;
using RPGGame.UI;
using RPGGame.UI.Avalonia;
using RPGGame.Utils;

namespace RPGGame.Entity.Services
{
    /// <summary>
    /// Handles error logging and UI display for character save/load operations.
    /// Extracted from CharacterSaveService to improve Single Responsibility Principle compliance.
    /// </summary>
    public class CharacterSaveErrorHandler
    {
        /// <summary>
        /// Handles save errors by logging and showing messages to the user
        /// </summary>
        public void HandleSaveError(string errorMessage, string? filename)
        {
            string fullMessage = $"Error saving character{(filename != null ? $" to {filename}" : "")}: {errorMessage}";
            
            // Always log to debug logger
            ScrollDebugLogger.LogAlways(fullMessage);
            
            // Show error in console mode
            if (UIManager.GetCustomUIManager() == null)
            {
                UIManager.WriteLine(fullMessage);
            }
            else
            {
                // Show error in custom UI mode
                var customUI = UIManager.GetCustomUIManager();
                if (customUI is CanvasUICoordinator canvasUI)
                {
                    canvasUI.ShowError($"Failed to save character", errorMessage);
                }
                else
                {
                    // Fallback for other UI types
                    UIManager.WriteLine(fullMessage);
                }
            }
        }
        
        /// <summary>
        /// Handles load errors by showing messages to the user (only in console mode)
        /// </summary>
        public void HandleLoadError(string errorMessage)
        {
            // Only show error in console mode (not in custom UI mode)
            if (UIManager.GetCustomUIManager() == null)
            {
                UIManager.WriteLine(errorMessage);
            }
        }
        
        /// <summary>
        /// Handles file not found errors during load
        /// </summary>
        public void HandleFileNotFoundError(string filename)
        {
            HandleLoadError($"No save file found at {filename}");
        }
        
        /// <summary>
        /// Handles deserialization errors during load
        /// </summary>
        public void HandleDeserializationError()
        {
            HandleLoadError("Failed to deserialize character data");
        }
        
        /// <summary>
        /// Handles I/O errors during load
        /// </summary>
        public void HandleIOError(string operation, string message)
        {
            HandleLoadError($"Error {operation}: I/O error - {message}");
        }
        
        /// <summary>
        /// Handles access denied errors during load
        /// </summary>
        public void HandleAccessDeniedError(string operation, string message)
        {
            HandleLoadError($"Error {operation}: Access denied - {message}");
        }
        
        /// <summary>
        /// Handles JSON deserialization errors during load
        /// </summary>
        public void HandleJsonError(string operation, string message)
        {
            HandleLoadError($"Error {operation}: JSON deserialization error - {message}");
        }
        
        /// <summary>
        /// Handles generic errors during load
        /// </summary>
        public void HandleGenericError(string operation, string message)
        {
            HandleLoadError($"Error {operation}: {message}");
        }
        
        /// <summary>
        /// Shows a success message when a character is loaded (only in console mode)
        /// </summary>
        public void ShowLoadSuccess(string filename)
        {
            // Only show load message in console mode (not in custom UI mode)
            if (UIManager.GetCustomUIManager() == null)
            {
                UIManager.WriteLine($"Character loaded from {filename}");
            }
        }
    }
}
