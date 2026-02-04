using RPGGame.UI;
using RPGGame.UI.Avalonia;
using RPGGame.Utils;

namespace RPGGame.Entity.Services
{
    /// <summary>
    /// Default implementation: logs to ScrollDebugLogger and shows message in console or Canvas UI.
    /// </summary>
    public class CharacterSaveErrorReporter : ICharacterSaveErrorReporter
    {
        public void ReportSaveError(string title, string message, string? filename = null)
        {
            string fullMessage = $"Error saving character{(filename != null ? $" to {filename}" : "")}: {message}";
            ScrollDebugLogger.LogAlways(fullMessage);
            ReportToUser(fullMessage, title, message);
        }

        public void ReportLoadError(string message)
        {
            ScrollDebugLogger.LogAlways($"Error loading character: {message}");
            if (UIManager.GetCustomUIManager() == null)
                UIManager.WriteLine($"Error loading character: {message}");
            else
            {
                var customUI = UIManager.GetCustomUIManager();
                if (customUI is CanvasUICoordinator canvasUI)
                    canvasUI.ShowError("Failed to load character", message);
                else
                    UIManager.WriteLine($"Error loading character: {message}");
            }
        }

        public void ReportDeleteError(string message)
        {
            ScrollDebugLogger.LogAlways($"Error deleting save file: {message}");
            if (UIManager.GetCustomUIManager() == null)
                UIManager.WriteLine($"Error deleting save file: {message}");
            else
            {
                var customUI = UIManager.GetCustomUIManager();
                if (customUI is CanvasUICoordinator canvasUI)
                    canvasUI.ShowError("Failed to delete save", message);
                else
                    UIManager.WriteLine($"Error deleting save file: {message}");
            }
        }

        private static void ReportToUser(string fullMessage, string title, string shortMessage)
        {
            if (UIManager.GetCustomUIManager() == null)
                UIManager.WriteLine(fullMessage);
            else
            {
                var customUI = UIManager.GetCustomUIManager();
                if (customUI is CanvasUICoordinator canvasUI)
                    canvasUI.ShowError(title, shortMessage);
                else
                    UIManager.WriteLine(fullMessage);
            }
        }
    }
}
