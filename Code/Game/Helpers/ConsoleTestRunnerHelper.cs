namespace RPGGame
{
    using System;
    using System.IO;
    using System.Threading.Tasks;
    using RPGGame.UI.Avalonia;

    /// <summary>
    /// Helper class for running tests that output to console and capturing their output for UI display.
    /// Consolidates the common pattern used by RunItemGenerationTest, RunTierDistributionTest, and RunCommonItemModificationTest.
    /// </summary>
    public static class ConsoleTestRunnerHelper
    {
        /// <summary>
        /// Runs a console-based test, captures its output, and displays it in the UI.
        /// </summary>
        /// <param name="canvasUI">The UI coordinator for displaying results</param>
        /// <param name="testAction">The test action to execute (should write to Console)</param>
        /// <param name="testTitle">Title to display before the test</param>
        /// <param name="testDescription">Description to display before the test</param>
        /// <param name="completionMessage">Message to display after test completion</param>
        /// <param name="filterLines">Optional function to filter out unwanted lines from output</param>
        /// <returns>True if test completed successfully, false otherwise</returns>
        public static async Task<bool> RunConsoleTestWithUI(
            CanvasUICoordinator canvasUI,
            System.Action testAction,
            string testTitle,
            string testDescription,
            string completionMessage,
            Func<string, bool>? filterLines = null)
        {
            try
            {
                // Display header
                canvasUI.WriteLine($"=== {testTitle} ===", UIMessageType.System);
                canvasUI.WriteLine(testDescription, UIMessageType.System);
                canvasUI.WriteBlankLine();
                canvasUI.WriteLine("Starting test (this may take a moment)...", UIMessageType.System);
                canvasUI.WriteBlankLine();
                canvasUI.ForceRenderDisplayBuffer();

                // Capture console output
                var originalOut = Console.Out;
                using (var stringWriter = new StringWriter())
                {
                    Console.SetOut(stringWriter);

                    try
                    {
                        // Run the test in a background task
                        await Task.Run(() =>
                        {
                            testAction();
                        });

                        string output = stringWriter.ToString();

                        // Display captured output with optional filtering
                        Func<string, bool> defaultFilter = (line) => 
                            !string.IsNullOrWhiteSpace(line) && !line.Contains("Press any key");
                        
                        var filter = filterLines ?? defaultFilter;
                        
                        foreach (var line in output.Split(new[] { "\r\n", "\n", "\r" }, StringSplitOptions.None))
                        {
                            if (filter(line))
                            {
                                canvasUI.WriteLine(line, UIMessageType.System);
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        canvasUI.WriteLine($"Error running test: {ex.Message}", UIMessageType.System);
                        if (ex.StackTrace != null)
                        {
                            canvasUI.WriteLine($"Stack trace: {ex.StackTrace}", UIMessageType.System);
                        }
                    }
                    finally
                    {
                        Console.SetOut(originalOut);
                    }
                }

                // Display completion message
                canvasUI.WriteBlankLine();
                canvasUI.WriteLine(completionMessage, UIMessageType.System);
                canvasUI.ForceRenderDisplayBuffer();
                
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ConsoleTestRunnerHelper] Error in RunConsoleTestWithUI: {ex.Message}");
                canvasUI.WriteLine($"Error running test: {ex.Message}", UIMessageType.System);
                canvasUI.ForceRenderDisplayBuffer();
                return false;
            }
        }
    }
}
