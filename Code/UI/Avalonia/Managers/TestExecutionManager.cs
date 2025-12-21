using Avalonia.Controls;
using Avalonia.Threading;
using Avalonia;
using RPGGame;
using RPGGame.Game.Testing.Commands;
using System;
using System.Threading.Tasks;
using System.Timers;

namespace RPGGame.UI.Avalonia.Managers
{
    /// <summary>
    /// Manages test execution in SettingsPanel
    /// Extracted from SettingsPanel.axaml.cs to reduce file size
    /// </summary>
    public class TestExecutionManager
    {
        private readonly CanvasUICoordinator? canvasUI;
        private readonly GameStateManager? gameStateManager;
        private readonly TextBlock testOutputTextBlock;
        private readonly ScrollViewer? testOutputScrollViewer;
        private readonly Action<string, bool> showStatusMessage;

        public TestExecutionManager(
            CanvasUICoordinator? canvasUI,
            GameStateManager? gameStateManager,
            TextBlock testOutputTextBlock,
            ScrollViewer? testOutputScrollViewer,
            Action<string, bool> showStatusMessage)
        {
            this.canvasUI = canvasUI;
            this.gameStateManager = gameStateManager;
            this.testOutputTextBlock = testOutputTextBlock;
            this.testOutputScrollViewer = testOutputScrollViewer;
            this.showStatusMessage = showStatusMessage;
        }

        public Task ExecuteTest(string commandKey)
        {
            CanvasUICoordinator? uiToUse = canvasUI;
            if (uiToUse == null)
            {
                var uiManager = RPGGame.UIManager.GetCustomUIManager();
                uiToUse = uiManager as CanvasUICoordinator;
            }
            
            if (uiToUse == null)
            {
                showStatusMessage("UI not available - game may not be fully initialized. Please wait for the game to load completely.", false);
                return Task.CompletedTask;
            }
            
            try
            {
                Dispatcher.UIThread.Post(() =>
                {
                    testOutputTextBlock.Text = "Running test...\n";
                });
                
                uiToUse.ClearDisplayBuffer();
                uiToUse.RestoreDisplayBufferRendering();
                
                var testCoordinator = new TestExecutionCoordinator(uiToUse);
                var stateManager = gameStateManager ?? new GameStateManager();
                
                ITestCommand? command = commandKey switch
                {
                    "1" => new RunAllTestsCommand(uiToUse, testCoordinator, stateManager),
                    "2" => new RunSystemTestsCommand(uiToUse, testCoordinator, stateManager, "Character"),
                    "3" => new RunCombatTestsCommand(uiToUse, testCoordinator, stateManager),
                    "4" => new RunInventoryDungeonTestsCommand(uiToUse, testCoordinator, stateManager),
                    "5" => new RunDataUITestsCommand(uiToUse, testCoordinator, stateManager),
                    "6" => new RunAdvancedTestsCommand(uiToUse, testCoordinator, stateManager),
                    "7" => new GenerateRandomItemsCommand(uiToUse, testCoordinator, stateManager),
                    "8" => new RunItemGenerationTestCommand(uiToUse, testCoordinator, stateManager),
                    "9" => new RunTierDistributionTestCommand(uiToUse, testCoordinator, stateManager),
                    "10" => new RunCommonItemModificationTestCommand(uiToUse, testCoordinator, stateManager),
                    "11" => new RunColorSystemTestsCommand(uiToUse, testCoordinator, stateManager),
                    "12" => new RunActionSystemTestsCommand(uiToUse, testCoordinator, stateManager),
                    "13" => new RunActionEditorTestCommand(uiToUse, testCoordinator, stateManager),
                    _ => null
                };
                
                if (command == null)
                {
                    Dispatcher.UIThread.Post(() =>
                    {
                        testOutputTextBlock.Text = $"Unknown test command: {commandKey}";
                        showStatusMessage("Unknown test", false);
                    });
                    return Task.CompletedTask;
                }
                
                Dispatcher.UIThread.Post(() =>
                {
                    testOutputTextBlock.Text = "Test execution started...\n";
                    showStatusMessage("Test execution started", true);
                });
                
                System.Timers.Timer? outputUpdateTimer = new System.Timers.Timer(100);
                outputUpdateTimer.Elapsed += (s, e) =>
                {
                    try
                    {
                        string currentOutput = uiToUse.GetDisplayBufferText();
                        if (!string.IsNullOrWhiteSpace(currentOutput))
                        {
                            Dispatcher.UIThread.Post(() =>
                            {
                                testOutputTextBlock.Text = currentOutput;
                                ScrollToBottom();
                            });
                        }
                    }
                    catch
                    {
                        // Log error but don't break test execution
                    }
                };
                outputUpdateTimer.Start();
                
                _ = Task.Run(async () =>
                {
                    try
                    {
                        await command.ExecuteAsync();
                        
                        outputUpdateTimer?.Stop();
                        outputUpdateTimer?.Dispose();
                        
                        string testOutput = uiToUse.GetDisplayBufferText();
                        
                        Dispatcher.UIThread.Post(() =>
                        {
                            if (!string.IsNullOrWhiteSpace(testOutput))
                            {
                                testOutputTextBlock.Text = testOutput;
                                ScrollToBottom();
                            }
                            else
                            {
                                testOutputTextBlock.Text = "Test completed, but no output was captured.\n";
                            }
                            showStatusMessage("Test completed", true);
                        });
                    }
                    catch (Exception ex)
                    {
                        outputUpdateTimer?.Stop();
                        outputUpdateTimer?.Dispose();
                        
                        Dispatcher.UIThread.Post(() =>
                        {
                            testOutputTextBlock.Text = $"Error executing test: {ex.Message}\n{ex.StackTrace}";
                            showStatusMessage($"Error: {ex.Message}", false);
                        });
                    }
                });
                
                return Task.CompletedTask;
            }
            catch (Exception ex)
            {
                Dispatcher.UIThread.Post(() =>
                {
                    testOutputTextBlock.Text = $"Error: {ex.Message}\n{ex.StackTrace}";
                    showStatusMessage($"Error: {ex.Message}", false);
                });
                return Task.CompletedTask;
            }
        }

        private void ScrollToBottom()
        {
            if (testOutputScrollViewer != null)
            {
                Dispatcher.UIThread.Post(() =>
                {
                    if (testOutputScrollViewer.Extent.Height > testOutputScrollViewer.Viewport.Height)
                    {
                        double maxScroll = testOutputScrollViewer.Extent.Height - testOutputScrollViewer.Viewport.Height;
                        testOutputScrollViewer.Offset = new Vector(testOutputScrollViewer.Offset.X, maxScroll);
                    }
                }, DispatcherPriority.Background);
            }
        }
    }
}

