using System;
using System.IO;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Threading;
using RPGGame.Tests;
using RPGGame.Tests.Runners;

namespace RPGGame.UI.Avalonia.Managers
{
    /// <summary>
    /// Orchestrates test execution with consistent error handling and output capture
    /// </summary>
    public class TestExecutionOrchestrator
    {
        private readonly TextBox? outputTextBox;
        private readonly TextBlock? statusTextBlock;
        private readonly ProgressBar? progressBar;
        private BatchedTestOutputWriter? testOutputWriter;
        private TextWriter? originalConsoleOut;
        private bool isRunning = false;
        private readonly object lockObject = new object();

        public TestExecutionOrchestrator(
            TextBox? outputTextBox,
            TextBlock? statusTextBlock = null,
            ProgressBar? progressBar = null)
        {
            this.outputTextBox = outputTextBox;
            this.statusTextBlock = statusTextBlock;
            this.progressBar = progressBar;
        }

        /// <summary>
        /// Runs a test method with consistent error handling and output capture
        /// </summary>
        /// <param name="testMethod">The test method to execute</param>
        /// <param name="statusMessage">Status message to display while running</param>
        /// <param name="completionMessage">Message to display when complete</param>
        /// <param name="getSummaryAfter">Optional function to get summary text after tests complete (before stopping capture)</param>
        public async Task RunTestAsync(
            System.Action testMethod,
            string statusMessage,
            string completionMessage,
            System.Func<string>? getSummaryAfter = null)
        {
            lock (lockObject)
            {
                if (isRunning) return;
                isRunning = true;
            }

            try
            {
                ClearOutput();
                UpdateStatus(statusMessage);
                SetProgress(0);

                // Reset collector so status stats reflect only this run (not prior suites in the session).
                TestResultCollector.Clear();
                
                StartCapturingOutput();
                
                // Run test method with timeout protection
                try
                {
                    await Task.Run(testMethod);
                }
                catch (Exception testEx)
                {
                    // Log test execution errors but continue to show summary
                    Console.WriteLine($"\n⚠️ Test execution error: {testEx.Message}");
                    Console.WriteLine($"Stack trace: {testEx.StackTrace}");
                }
                
                // Get and append summary if provided (while console capture is still active)
                if (getSummaryAfter != null)
                {
                    try
                    {
                        // Write a marker to indicate summary is about to be written
                        Console.WriteLine("\n" + new string('=', 60));
                        Console.WriteLine("GENERATING TEST SUMMARY...");
                        Console.WriteLine(new string('=', 60));
                        
                        var summary = getSummaryAfter();
                        if (!string.IsNullOrEmpty(summary))
                        {
                            Console.WriteLine(summary);
                            Console.WriteLine("\n" + new string('=', 60));
                            Console.WriteLine("SUMMARY COMPLETE");
                            Console.WriteLine(new string('=', 60));
                        }
                        else
                        {
                            Console.WriteLine("\n⚠️ WARNING: Summary is empty - no test results were collected!");
                            Console.WriteLine("This may indicate tests did not record results properly.");
                            Console.WriteLine(new string('=', 60));
                        }
                    }
                    catch (Exception summaryEx)
                    {
                        Console.WriteLine($"\n⚠️ Error generating summary: {summaryEx.Message}");
                        Console.WriteLine($"Stack trace: {summaryEx.StackTrace}");
                        Console.WriteLine(new string('=', 60));
                    }
                }
                
                // Force immediate flush of all buffered content
                if (testOutputWriter != null)
                {
                    testOutputWriter.Flush();
                }
                
                // Wait longer to ensure all batched writes complete and UI updates
                await Task.Delay(800);
                
                // Final flush to catch any remaining buffered content
                if (testOutputWriter != null)
                {
                    testOutputWriter.Flush();
                }
                
                // Additional wait for UI to fully update and ensure summary is visible
                await Task.Delay(500);
                
                // Calculate pass rate from captured output
                var (passedCount, totalCount, passRate) = CalculateTestResults();
                var finalMessage = FormatCompletionMessage(completionMessage, passedCount, totalCount, passRate);
                
                UpdateStatus(finalMessage);
                SetProgress(100);
            }
            catch (Exception ex)
            {
                AppendOutput($"\nError: {ex.Message}\n{ex.StackTrace}");
                UpdateStatus($"Error: {ex.Message}");
            }
            finally
            {
                StopCapturingOutput();
                lock (lockObject)
                {
                    isRunning = false;
                }
            }
        }

        /// <summary>
        /// Appends text directly to the output TextBox (use after StopCapturingOutput)
        /// </summary>
        public void AppendTextDirectly(string text)
        {
            if (outputTextBox != null)
            {
                Dispatcher.UIThread.Post(() =>
                {
                    var currentText = outputTextBox.Text ?? string.Empty;
                    outputTextBox.Text = currentText + text;
                    outputTextBox.CaretIndex = outputTextBox.Text.Length;
                });
            }
        }

        private void ClearOutput()
        {
            Dispatcher.UIThread.Post(() =>
            {
                if (outputTextBox != null)
                {
                    outputTextBox.Text = string.Empty;
                }
                UpdateStatus("Ready");
                SetProgress(0);
            });
        }

        private void AppendOutput(string text)
        {
            if (testOutputWriter != null)
            {
                testOutputWriter.Append(text);
            }
        }

        private void UpdateStatus(string status)
        {
            Dispatcher.UIThread.Post(() =>
            {
                if (statusTextBlock != null)
                {
                    statusTextBlock.Text = status;
                }
            });
        }

        private void SetProgress(double value)
        {
            Dispatcher.UIThread.Post(() =>
            {
                if (progressBar != null)
                {
                    progressBar.Value = value;
                }
            });
        }

        /// <summary>
        /// Starts capturing Console output and redirecting it to the TextBox
        /// </summary>
        private void StartCapturingOutput()
        {
            originalConsoleOut = Console.Out;
            testOutputWriter = new BatchedTestOutputWriter(outputTextBox);
            Console.SetOut(testOutputWriter);
        }

        /// <summary>
        /// Stops capturing Console output and restores original output
        /// </summary>
        private void StopCapturingOutput()
        {
            if (testOutputWriter != null)
            {
                testOutputWriter.Flush(); // Ensure all buffered content is written
            }
            
            if (originalConsoleOut != null)
            {
                Console.SetOut(originalConsoleOut);
                originalConsoleOut = null;
            }
            testOutputWriter = null;
        }

        /// <summary>
        /// Calculates test results from TestResultCollector (assertions) or by parsing console output.
        /// </summary>
        private (int passed, int total, double passRate) CalculateTestResults()
        {
            var (collectorTotal, collectorPassed, _, collectorRate) = TestResultCollector.GetStatistics();
            if (collectorTotal > 0)
            {
                return (collectorPassed, collectorTotal, collectorRate);
            }

            if (outputTextBox == null) return (0, 0, 0.0);

            var outputText = outputTextBox.Text ?? string.Empty;
            var lines = outputText.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);

            int passedCount = 0;
            int failedCount = 0;

            // Fallback for suites that do not record via TestBase: match TestBase output shapes only.
            // Avoid false positives from section headers like "--- On FAILURE ---" or summary "Failed: N".
            foreach (var line in lines)
            {
                var trimmedLine = line.Trim();

                if (trimmedLine.Contains("✗ FAILED:", StringComparison.Ordinal))
                {
                    failedCount++;
                }
                else if (TestPassLinePattern.IsMatch(trimmedLine))
                {
                    passedCount++;
                }
            }

            int totalCount = passedCount + failedCount;
            double passRate = totalCount > 0 ? (passedCount * 100.0 / totalCount) : 0.0;

            return (passedCount, totalCount, passRate);
        }

        /// <summary>Lines emitted by <see cref="TestBase"/> assertions: leading whitespace, ✓, message.</summary>
        private static readonly Regex TestPassLinePattern = new Regex(@"^\s*✓\s+\S", RegexOptions.Compiled);

        /// <summary>
        /// Formats the completion message with test statistics
        /// </summary>
        private string FormatCompletionMessage(string baseMessage, int passedCount, int totalCount, double passRate)
        {
            if (totalCount == 0)
            {
                return baseMessage;
            }
            
            return $"{baseMessage} - {passedCount}/{totalCount} passed ({passRate:F1}%)";
        }
    }
}
