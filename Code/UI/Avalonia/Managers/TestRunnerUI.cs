using System;
using System.IO;
using System.Text;
using System.Threading;
using Avalonia.Threading;
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
        /// Runs all tests and displays results in the center panel
        /// </summary>
        public void RunAllTests()
        {
            if (isRunning)
            {
                return;
            }

            try
            {
                isRunning = true;
                PrepareUIForTestOutput();
                StartCapturingOutput();
                
                // Write header
                uiCoordinator?.WriteLine("========================================");
                uiCoordinator?.WriteLine("  COMPREHENSIVE GAME TEST SUITE");
                uiCoordinator?.WriteLine("========================================");
                uiCoordinator?.WriteBlankLine();

                // Run all tests
                ComprehensiveTestRunner.RunAllTests();
                
                // Force render to ensure results are displayed
                ForceTestOutputRender();
            }
            catch (Exception ex)
            {
                uiCoordinator?.WriteLine($"Error running tests: {ex.Message}");
            }
            finally
            {
                StopCapturingOutput();
                isRunning = false;
            }
        }

        /// <summary>
        /// Runs quick tests
        /// </summary>
        public void RunQuickTests()
        {
            if (isRunning) return;

            try
            {
                isRunning = true;
                PrepareUIForTestOutput();
                StartCapturingOutput();
                
                uiCoordinator?.WriteLine("=== QUICK TESTS ===");
                uiCoordinator?.WriteBlankLine();

                ComprehensiveTestRunner.RunQuickTests();
                
                // Force render to ensure results are displayed
                ForceTestOutputRender();
            }
            catch (Exception ex)
            {
                uiCoordinator?.WriteLine($"Error running quick tests: {ex.Message}");
            }
            finally
            {
                StopCapturingOutput();
                isRunning = false;
            }
        }

        /// <summary>
        /// Runs action system tests
        /// </summary>
        public void RunActionSystemTests()
        {
            if (isRunning) return;

            try
            {
                isRunning = true;
                PrepareUIForTestOutput();
                StartCapturingOutput();
                
                ComprehensiveTestRunner.RunActionSystemTests();
                
                ForceTestOutputRender();
            }
            catch (Exception ex)
            {
                uiCoordinator?.WriteLine($"Error running action system tests: {ex.Message}");
            }
            finally
            {
                StopCapturingOutput();
                isRunning = false;
            }
        }

        /// <summary>
        /// Runs dice mechanics tests
        /// </summary>
        public void RunDiceMechanicsTests()
        {
            if (isRunning) return;

            try
            {
                isRunning = true;
                PrepareUIForTestOutput();
                StartCapturingOutput();
                
                ComprehensiveTestRunner.RunDiceMechanicsTests();
                
                ForceTestOutputRender();
            }
            catch (Exception ex)
            {
                uiCoordinator?.WriteLine($"Error running dice mechanics tests: {ex.Message}");
            }
            finally
            {
                StopCapturingOutput();
                isRunning = false;
            }
        }

        /// <summary>
        /// Runs combo system tests
        /// </summary>
        public void RunComboSystemTests()
        {
            if (isRunning) return;

            try
            {
                isRunning = true;
                PrepareUIForTestOutput();
                StartCapturingOutput();
                
                ComprehensiveTestRunner.RunComboSystemTests();
                
                ForceTestOutputRender();
            }
            catch (Exception ex)
            {
                uiCoordinator?.WriteLine($"Error running combo system tests: {ex.Message}");
            }
            finally
            {
                StopCapturingOutput();
                isRunning = false;
            }
        }

        /// <summary>
        /// Runs color system tests
        /// </summary>
        public void RunColorSystemTests()
        {
            if (isRunning) return;

            try
            {
                isRunning = true;
                PrepareUIForTestOutput();
                StartCapturingOutput();
                
                ComprehensiveTestRunner.RunColorSystemTests();
                
                // Force render to ensure results are displayed
                ForceTestOutputRender();
            }
            catch (Exception ex)
            {
                uiCoordinator?.WriteLine($"Error running color system tests: {ex.Message}");
            }
            finally
            {
                StopCapturingOutput();
                isRunning = false;
            }
        }

        /// <summary>
        /// Runs display system tests
        /// </summary>
        public void RunDisplaySystemTests()
        {
            if (isRunning) return;

            try
            {
                isRunning = true;
                PrepareUIForTestOutput();
                StartCapturingOutput();
                
                ComprehensiveTestRunner.RunDisplaySystemTests();
                
                ForceTestOutputRender();
            }
            catch (Exception ex)
            {
                uiCoordinator?.WriteLine($"Error running display system tests: {ex.Message}");
            }
            finally
            {
                StopCapturingOutput();
                isRunning = false;
            }
        }

        /// <summary>
        /// Runs character system tests
        /// </summary>
        public void RunCharacterSystemTests()
        {
            if (isRunning) return;

            try
            {
                isRunning = true;
                PrepareUIForTestOutput();
                StartCapturingOutput();
                
                ComprehensiveTestRunner.RunCharacterSystemTests();
                
                ForceTestOutputRender();
            }
            catch (Exception ex)
            {
                uiCoordinator?.WriteLine($"Error running character system tests: {ex.Message}");
            }
            finally
            {
                StopCapturingOutput();
                isRunning = false;
            }
        }

        /// <summary>
        /// Runs status effects tests
        /// </summary>
        public void RunStatusEffectsTests()
        {
            if (isRunning) return;

            try
            {
                isRunning = true;
                PrepareUIForTestOutput();
                StartCapturingOutput();
                
                ComprehensiveTestRunner.RunStatusEffectsTests();
                
                ForceTestOutputRender();
            }
            catch (Exception ex)
            {
                uiCoordinator?.WriteLine($"Error running status effects tests: {ex.Message}");
            }
            finally
            {
                StopCapturingOutput();
                isRunning = false;
            }
        }

        /// <summary>
        /// Runs integration tests
        /// </summary>
        public void RunIntegrationTests()
        {
            if (isRunning) return;

            try
            {
                isRunning = true;
                PrepareUIForTestOutput();
                StartCapturingOutput();
                
                ComprehensiveTestRunner.RunIntegrationTests();
                
                ForceTestOutputRender();
            }
            catch (Exception ex)
            {
                uiCoordinator?.WriteLine($"Error running integration tests: {ex.Message}");
            }
            finally
            {
                StopCapturingOutput();
                isRunning = false;
            }
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

