namespace RPGGame.Game.Testing.Commands
{
    using System;
    using System.IO;
    using System.Threading.Tasks;
    using RPGGame.UI.Avalonia;
    using RPGGame.UI;
    using RPGGame.Tests.Unit;

    /// <summary>
    /// Command to run ActionEditor tests (create, update, delete, validation).
    /// </summary>
    public class RunActionEditorTestCommand : TestCommandBase
    {
        public RunActionEditorTestCommand(
            CanvasUICoordinator canvasUI,
            TestExecutionCoordinator? testCoordinator,
            GameStateManager stateManager)
            : base(canvasUI, testCoordinator, stateManager)
        {
        }

        public override async Task ExecuteAsync()
        {
            CanvasUI.WriteLine("=== Action Editor Tests ===", UIMessageType.System);
            CanvasUI.WriteLine("Testing ActionEditor: create, update, delete, and validation functionality.", UIMessageType.System);
            CanvasUI.WriteBlankLine();
            
            // Capture console output
            var originalOut = Console.Out;
            using (var stringWriter = new StringWriter())
            {
                Console.SetOut(stringWriter);
                
                try
                {
                    ActionEditorTest.RunAllTests();
                    string output = stringWriter.ToString();
                    
                    // Display output
                    foreach (var line in output.Split(new[] { "\r\n", "\n", "\r" }, StringSplitOptions.None))
                    {
                        if (!string.IsNullOrWhiteSpace(line))
                        {
                            CanvasUI.WriteLine(line, UIMessageType.System);
                        }
                    }
                }
                finally
                {
                    Console.SetOut(originalOut);
                }
            }
            
            CanvasUI.WriteBlankLine();
            CanvasUI.WriteLine("Action Editor tests completed. Press any key to return to test menu...", UIMessageType.System);
            
            if (TestCoordinator != null)
            {
                TestCoordinator.WaitingForTestMenuReturn = true;
            }
            
            await Task.CompletedTask;
        }
    }
}

