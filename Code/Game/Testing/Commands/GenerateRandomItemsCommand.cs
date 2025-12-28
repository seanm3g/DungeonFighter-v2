namespace RPGGame.Game.Testing.Commands
{
    using System;
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using RPGGame.UI.Avalonia;

    /// <summary>
    /// Command to generate and display 10 random items.
    /// </summary>
    public class GenerateRandomItemsCommand : TestCommandBase
    {
        public GenerateRandomItemsCommand(
            CanvasUICoordinator canvasUI,
            TestExecutionCoordinator? testCoordinator,
            GameStateManager stateManager)
            : base(canvasUI, testCoordinator, stateManager)
        {
        }

        public override async Task ExecuteAsync()
        {
            try
            {
                CanvasUI.WriteLine("=== GENERATING 10 RANDOM ITEMS ===", UIMessageType.System);
                CanvasUI.WriteBlankLine();

                // Get player level for item generation (use level 10 as default if no player)
                int playerLevel = StateManager.CurrentPlayer?.Level ?? 10;
                int dungeonLevel = 1;

                // Generate 10 random items
                var items = new List<Item>();
                for (int i = 0; i < 10; i++)
                {
                    var item = LootGenerator.GenerateLoot(playerLevel, dungeonLevel, StateManager.CurrentPlayer, guaranteedLoot: true);
                    if (item != null)
                    {
                        items.Add(item);
                    }
                }

                if (items.Count == 0)
                {
                    TestUICoordinator.HandleTestError(CanvasUI, TestCoordinator, "Error: No items were generated.", "GenerateRandomItems");
                    return;
                }

                // Use helper to display items
                RandomItemDisplayHelper.DisplayItems(CanvasUI, items, StateManager.CurrentPlayer);
                CanvasUI.ForceRenderDisplayBuffer();
                TestUICoordinator.MarkWaitingForReturn(TestCoordinator);
            }
            catch (Exception ex)
            {
                TestUICoordinator.HandleTestError(CanvasUI, TestCoordinator, $"Error generating items: {ex.Message}", "GenerateRandomItems");
            }

            await Task.CompletedTask;
        }
    }
}
