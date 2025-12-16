namespace RPGGame
{
    using System;
    using RPGGame.UI.Avalonia;

    /// <summary>
    /// Helper class for coordinating UI operations during test execution.
    /// Consolidates common UI patterns like clearing buffer, marking waiting state, and error handling.
    /// </summary>
    public static class TestUICoordinator
    {
        /// <summary>
        /// Clears the display buffer and prepares for a new test.
        /// Re-enables display buffer rendering so test output can be displayed.
        /// </summary>
        /// <param name="canvasUI">The UI coordinator</param>
        public static void ClearAndPrepareForTest(CanvasUICoordinator canvasUI)
        {
            // Re-enable display buffer rendering (it may have been suppressed by menu rendering)
            canvasUI.RestoreDisplayBufferRendering();
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
            canvasUI.RenderDisplayBuffer();
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
            
            canvasUI.RenderDisplayBuffer();
            MarkWaitingForReturn(testCoordinator);
        }
    }
}
