namespace RPGGame
{
    using System;
    using System.Threading.Tasks;
    using RPGGame.UI.Avalonia;
    using RPGGame.Utils;

    /// <summary>
    /// Helper class to consolidate common test execution patterns.
    /// Handles try-catch, logging, UI updates, and waiting flag management.
    /// </summary>
    public static class TestExecutionHelper
    {
        /// <summary>
        /// Executes a test action with standard error handling and UI updates.
        /// </summary>
        /// <param name="canvasUI">The UI coordinator for displaying results</param>
        /// <param name="testAction">The test action to execute</param>
        /// <param name="testName">Name of the test for logging purposes</param>
        /// <param name="logToConsole">Whether to log start/completion to console</param>
        /// <param name="preTestAction">Optional action to execute before the test (e.g., custom UI messages)</param>
        /// <returns>True if test completed successfully, false otherwise</returns>
        public static async Task<bool> ExecuteTestWithUI(
            CanvasUICoordinator canvasUI,
            Func<Task> testAction,
            string testName,
            bool logToConsole = false,
            Action<CanvasUICoordinator>? preTestAction = null)
        {
            if (logToConsole)
            {
                Console.WriteLine($"[TestingSystemHandler] Starting {testName}");
            }
            try
            {
                // Execute any pre-test setup (like custom UI messages)
                preTestAction?.Invoke(canvasUI);
                
                // Execute the actual test
                await testAction();
                
                if (logToConsole)
                {
                    Console.WriteLine($"[TestingSystemHandler] {testName} completed");
                }
                // Show completion message
                ShowCompletionMessage(canvasUI);
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[TestingSystemHandler] Error in {testName}: {ex.Message}");
                canvasUI.WriteLine($"Error running tests: {ex.Message}", UIMessageType.System);
                canvasUI.ForceRenderDisplayBuffer();
                return false;
            }
        }

        /// <summary>
        /// Shows the standard completion message and renders the display buffer.
        /// </summary>
        private static void ShowCompletionMessage(CanvasUICoordinator canvasUI)
        {
            canvasUI.WriteBlankLine();
            canvasUI.WriteLine("=== Tests Complete ===", UIMessageType.System);
            canvasUI.WriteLine("Press any key to return to test menu...", UIMessageType.System);
            canvasUI.ForceRenderDisplayBuffer();
        }
    }
}

