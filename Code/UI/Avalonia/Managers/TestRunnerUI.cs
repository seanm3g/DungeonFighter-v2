using System;
using System.IO;
using System.Text;
using System.Threading;
using Avalonia.Threading;
using RPGGame;
using RPGGame.Tests.Runners;
using RPGGame.UI.Avalonia;

namespace RPGGame.UI.Avalonia.Managers
{
    /// <summary>
    /// Manages test execution and redirects Console output to the UI center panel
    /// </summary>
    public class TestRunnerUI
    {
        private readonly CanvasUICoordinator? uiCoordinator;
        private TextWriter? originalConsoleOut;
        private TestOutputWriter? testOutputWriter;
        private bool isRunning = false;

        public TestRunnerUI(CanvasUICoordinator? uiCoordinator)
        {
            this.uiCoordinator = uiCoordinator;
        }

        /// <summary>
        /// Common test execution pattern that handles setup, execution, and cleanup
        /// </summary>
        /// <param name="testRunner">The test runner method to execute</param>
        /// <param name="testSuiteName">Name of the test suite for error messages</param>
        /// <param name="headerText">Optional header text to display before running tests (can be multi-line)</param>
        private void RunTestSuite(System.Action testRunner, string testSuiteName, string? headerText = null)
        {
            if (isRunning) return;

            try
            {
                isRunning = true;
                PrepareUIForTestOutput();
                StartCapturingOutput();
                
                // Write header if provided (handle multi-line headers)
                if (!string.IsNullOrEmpty(headerText))
                {
                    var headerLines = headerText.Split(new[] { "\r\n", "\n", "\r" }, StringSplitOptions.None);
                    foreach (var line in headerLines)
                    {
                        if (!string.IsNullOrEmpty(line))
                        {
                            uiCoordinator?.WriteLine(line);
                        }
                    }
                    uiCoordinator?.WriteBlankLine();
                }

                // Run the tests
                testRunner();
                
                // Force render to ensure results are displayed
                ForceTestOutputRender();
            }
            catch (Exception ex)
            {
                uiCoordinator?.WriteLine($"Error running {testSuiteName}: {ex.Message}");
            }
            finally
            {
                StopCapturingOutput();
                isRunning = false;
            }
        }

        /// <summary>
        /// Runs all tests and displays results in the center panel
        /// </summary>
        public void RunAllTests()
        {
            RunTestSuite(
                () => ComprehensiveTestRunner.RunAllTests(),
                "tests",
                $"{GameConstants.StandardSeparator}\n  COMPREHENSIVE GAME TEST SUITE\n{GameConstants.StandardSeparator}"
            );
        }

        /// <summary>
        /// Runs quick tests
        /// </summary>
        public void RunQuickTests()
        {
            RunTestSuite(
                () => ComprehensiveTestRunner.RunQuickTests(),
                "quick tests",
                "=== QUICK TESTS ==="
            );
        }

        /// <summary>
        /// Runs action system tests
        /// </summary>
        public void RunActionSystemTests()
        {
            RunTestSuite(
                () => ComprehensiveTestRunner.RunActionSystemTests(),
                "action system tests"
            );
        }

        /// <summary>
        /// Runs dice mechanics tests
        /// </summary>
        public void RunDiceMechanicsTests()
        {
            RunTestSuite(
                () => ComprehensiveTestRunner.RunDiceMechanicsTests(),
                "dice mechanics tests"
            );
        }

        /// <summary>
        /// Runs combo system tests
        /// </summary>
        public void RunComboSystemTests()
        {
            RunTestSuite(
                () => ComprehensiveTestRunner.RunComboSystemTests(),
                "combo system tests"
            );
        }

        /// <summary>
        /// Runs color system tests
        /// </summary>
        public void RunColorSystemTests()
        {
            RunTestSuite(
                () => ComprehensiveTestRunner.RunColorSystemTests(),
                "color system tests"
            );
        }

        /// <summary>
        /// Runs display system tests
        /// </summary>
        public void RunDisplaySystemTests()
        {
            RunTestSuite(
                () => ComprehensiveTestRunner.RunDisplaySystemTests(),
                "display system tests"
            );
        }

        /// <summary>
        /// Runs character system tests
        /// </summary>
        public void RunCharacterSystemTests()
        {
            RunTestSuite(
                () => ComprehensiveTestRunner.RunCharacterSystemTests(),
                "character system tests"
            );
        }

        /// <summary>
        /// Runs status effects tests
        /// </summary>
        public void RunStatusEffectsTests()
        {
            RunTestSuite(
                () => ComprehensiveTestRunner.RunStatusEffectsTests(),
                "status effects tests"
            );
        }

        /// <summary>
        /// Runs integration tests
        /// </summary>
        public void RunIntegrationTests()
        {
            RunTestSuite(
                () => ComprehensiveTestRunner.RunIntegrationTests(),
                "integration tests"
            );
        }

        /// <summary>
        /// Runs action block tests
        /// </summary>
        public void RunActionBlockTests()
        {
            RunTestSuite(
                () => ComprehensiveTestRunner.RunActionBlockTests(),
                "action block tests"
            );
        }

        /// <summary>
        /// Runs dice roll mechanics tests
        /// </summary>
        public void RunDiceRollMechanicsTests()
        {
            RunTestSuite(
                () => ComprehensiveTestRunner.RunDiceRollMechanicsTests(),
                "dice roll mechanics tests"
            );
        }

        /// <summary>
        /// Runs dungeon and enemy generation tests
        /// </summary>
        public void RunDungeonEnemyGenerationTests()
        {
            RunTestSuite(
                () => ComprehensiveTestRunner.RunDungeonEnemyGenerationTests(),
                "dungeon/enemy generation tests"
            );
        }

        /// <summary>
        /// Runs action execution tests
        /// </summary>
        public void RunActionExecutionTests()
        {
            RunTestSuite(
                () => ComprehensiveTestRunner.RunActionExecutionTests(),
                "action execution tests"
            );
        }

        /// <summary>
        /// Prepares the UI for test output by restoring rendering and clearing the buffer
        /// </summary>
        private void PrepareUIForTestOutput()
        {
            if (uiCoordinator == null) return;
            
            // CRITICAL: Restore display buffer rendering so test results appear in center panel
            // The Settings panel suppresses rendering, but we need it enabled for test output
            uiCoordinator.RestoreDisplayBufferRendering();
            
            // Clear the display buffer to start fresh
            uiCoordinator.GetTextManager()?.ClearDisplayBuffer();
        }
        
        /// <summary>
        /// Forces rendering of test output even when in menu states
        /// </summary>
        private void ForceTestOutputRender()
        {
            if (uiCoordinator == null) return;
            
            // Clear clickable elements to prevent settings menu from opening when clicking after tests
            uiCoordinator.ClearClickableElements();
            
            // Force a render to ensure test results are displayed
            // This bypasses the normal suppression for menu states
            uiCoordinator.ForceRenderDisplayBuffer();
        }
        
        /// <summary>
        /// Starts capturing Console output and redirecting it to the UI
        /// </summary>
        private void StartCapturingOutput()
        {
            if (uiCoordinator == null) return;

            originalConsoleOut = Console.Out;
            testOutputWriter = new TestOutputWriter(uiCoordinator);
            Console.SetOut(testOutputWriter);
        }

        /// <summary>
        /// Stops capturing Console output and restores original output
        /// </summary>
        private void StopCapturingOutput()
        {
            if (originalConsoleOut != null)
            {
                Console.SetOut(originalConsoleOut);
                originalConsoleOut = null;
            }
            testOutputWriter = null;
        }

        /// <summary>
        /// Custom TextWriter that redirects Console output to the UI
        /// </summary>
        private class TestOutputWriter : TextWriter
        {
            private readonly CanvasUICoordinator uiCoordinator;

            public TestOutputWriter(CanvasUICoordinator uiCoordinator)
            {
                this.uiCoordinator = uiCoordinator;
            }

            public override Encoding Encoding => Encoding.UTF8;

            public override void Write(char value)
            {
                // Handle individual characters (for Write(char))
                if (value == '\n')
                {
                    Dispatcher.UIThread.Post(() => uiCoordinator.WriteBlankLine());
                }
                else if (value != '\r')
                {
                    Dispatcher.UIThread.Post(() => uiCoordinator.Write(value.ToString()));
                }
            }

            public override void Write(string? value)
            {
                if (value != null)
                {
                    // Split by newlines and write each line
                    var lines = value.Split(new[] { "\r\n", "\n", "\r" }, StringSplitOptions.None);
                    for (int i = 0; i < lines.Length; i++)
                    {
                        if (!string.IsNullOrEmpty(lines[i]))
                        {
                            string line = lines[i]; // Capture for closure
                            Dispatcher.UIThread.Post(() => uiCoordinator.WriteLine(line));
                        }
                        if (i < lines.Length - 1)
                        {
                            Dispatcher.UIThread.Post(() => uiCoordinator.WriteBlankLine());
                        }
                    }
                }
            }

            public override void WriteLine(string? value)
            {
                if (value != null)
                {
                    string line = value; // Capture for closure
                    Dispatcher.UIThread.Post(() => uiCoordinator.WriteLine(line));
                }
                else
                {
                    Dispatcher.UIThread.Post(() => uiCoordinator.WriteBlankLine());
                }
            }
        }
    }
}

