namespace RPGGame
{
    using System;
    using RPGGame.UI.Avalonia;
    using RPGGame.UI.Avalonia.Display;

    /// <summary>
    /// Helper class for coordinating UI operations during test execution.
    /// Consolidates common UI patterns like clearing buffer, marking waiting state, and error handling.
    /// </summary>
    public static class TestUICoordinator
    {
        /// <summary>
        /// Clears the display buffer and prepares for a new test.
        /// Re-enables display buffer rendering so test output can be displayed.
        /// Also clears the canvas to remove menu rendering and ensures display buffer will be visible.
        /// </summary>
        /// <param name="canvasUI">The UI coordinator</param>
        public static void ClearAndPrepareForTest(CanvasUICoordinator canvasUI)
        {
            // Clear the canvas to remove any menu rendering
            canvasUI.Clear();
            
            // Manually restore display buffer rendering (override automatic suppression for Testing state)
            // This ensures test output will be displayed even though we're in a menu state
            // Access the displayBufferManager via reflection or add a public getter
            var displayBufferManagerField = typeof(CanvasUICoordinator).GetField("displayBufferManager", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            
            if (displayBufferManagerField?.GetValue(canvasUI) is DisplayBufferManager manager)
            {
                manager.ManuallyRestore();
            }
            else
            {
                // Fallback to direct restoration if manager not available
                canvasUI.RestoreDisplayBufferRendering();
            }
            
            // Clear the display buffer to start fresh
            canvasUI.ClearDisplayBuffer();
        }

        /// <summary>
        /// Marks that we're waiting for a key press to return to the test menu.
        /// </summary>
        /// <param name="testCoordinator">The test execution coordinator</param>
        public static void MarkWaitingForReturn(TestExecutionCoordinator? testCoordinator)
        {
            if (testCoordinator != null)
            {
                testCoordinator.WaitingForTestMenuReturn = true;
            }
        }

        /// <summary>
        /// Handles a test error by displaying it and marking waiting state.
        /// </summary>
        /// <param name="canvasUI">The UI coordinator</param>
        /// <param name="testCoordinator">The test execution coordinator</param>
        /// <param name="errorMessage">The error message to display</param>
        /// <param name="context">Optional context for logging (e.g., method name)</param>
        public static void HandleTestError(
            CanvasUICoordinator canvasUI,
            TestExecutionCoordinator? testCoordinator,
            string errorMessage,
            string? context = null)
        {
            if (!string.IsNullOrEmpty(context))
            {
                Console.WriteLine($"[TestingSystemHandler] Error in {context}: {errorMessage}");
            }
            
            canvasUI.WriteLine(errorMessage, UIMessageType.System);
            canvasUI.ForceRenderDisplayBuffer();
            MarkWaitingForReturn(testCoordinator);
        }

        /// <summary>
        /// Completes a test by displaying a completion message and marking waiting state.
        /// </summary>
        /// <param name="canvasUI">The UI coordinator</param>
        /// <param name="testCoordinator">The test execution coordinator</param>
        /// <param name="completionMessage">Optional custom completion message</param>
        public static void CompleteTest(
            CanvasUICoordinator canvasUI,
            TestExecutionCoordinator? testCoordinator,
            string? completionMessage = null)
        {
            if (!string.IsNullOrEmpty(completionMessage))
            {
                canvasUI.WriteBlankLine();
                canvasUI.WriteLine(completionMessage, UIMessageType.System);
            }
            
            canvasUI.ForceRenderDisplayBuffer();
            MarkWaitingForReturn(testCoordinator);
        }
    }
}
