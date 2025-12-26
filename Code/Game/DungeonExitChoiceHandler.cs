namespace RPGGame
{
    using System;
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using RPGGame.UI.Avalonia;
    using RPGGame.UI.ColorSystem;

    /// <summary>
    /// Handles player choice to leave dungeon safely.
    /// This allows players to exit without rewards between rooms.
    /// </summary>
    public class DungeonExitChoiceHandler
    {
        private GameStateManager stateManager;
        private IUIManager? customUIManager;
        private DungeonDisplayManager displayManager;
        
        // Delegates
        public delegate void OnShowMessage(string message);
        
        public event OnShowMessage? ShowMessageEvent;
        
        // Task completion source to wait for player choice
        private TaskCompletionSource<bool>? exitChoiceTaskSource;
        
        public DungeonExitChoiceHandler(
            GameStateManager stateManager, 
            IUIManager? customUIManager,
            DungeonDisplayManager displayManager)
        {
            this.stateManager = stateManager ?? throw new ArgumentNullException(nameof(stateManager));
            this.customUIManager = customUIManager;
            this.displayManager = displayManager ?? throw new ArgumentNullException(nameof(displayManager));
        }
        
        /// <summary>
        /// Display the exit choice menu and wait for player input
        /// </summary>
        /// <returns>True if player chose to exit, false if they chose to continue</returns>
        public async Task<bool> ShowExitChoiceMenu(int currentRoom, int totalRooms)
        {
            // Create task completion source to wait for player choice
            exitChoiceTaskSource = new TaskCompletionSource<bool>();
            
            // Display the menu
            if (customUIManager is CanvasUICoordinator canvasUI)
            {
                // Option 1 with color
                var option1Builder = new ColoredTextBuilder()
                    .Add("  ", ColorPalette.White)
                    .Add("1", ColorPalette.Success)
                    .Add(" - Stay and continue through the dungeon", ColorPalette.White);
                canvasUI.WriteLineColoredSegments(option1Builder.Build(), UIMessageType.System);
                
                // Option 2 with color
                var option2Builder = new ColoredTextBuilder()
                    .Add("  ", ColorPalette.White)
                    .Add("2", ColorPalette.Warning)
                    .Add(" - Leave the dungeon", ColorPalette.White);
                canvasUI.WriteLineColoredSegments(option2Builder.Build(), UIMessageType.System);
                
                // Blank line after menu options
                canvasUI.WriteLineColoredSegments(new ColoredTextBuilder().Build(), UIMessageType.System);
                
                // Bottom separator line with color (same length as top separator)
                var bottomSeparatorBuilder = new ColoredTextBuilder()
                    .Add(AsciiArtAssets.UIText.Divider, ColorPalette.Info);
                canvasUI.WriteLineColoredSegments(bottomSeparatorBuilder.Build(), UIMessageType.System);
                
                // Render the menu
                if (stateManager.CurrentPlayer != null && stateManager.CurrentRoom != null)
                {
                    canvasUI.RenderRoomEntry(
                        stateManager.CurrentRoom, 
                        stateManager.CurrentPlayer, 
                        stateManager.CurrentDungeon?.Name);
                }
            }
            
            // Wait for player choice
            bool shouldExit = await exitChoiceTaskSource.Task;
            exitChoiceTaskSource = null;
            
            return shouldExit;
        }
        
        /// <summary>
        /// Handle menu input for exit choice
        /// </summary>
        public void HandleMenuInput(string input)
        {
            if (exitChoiceTaskSource == null || exitChoiceTaskSource.Task.IsCompleted)
            {
                // Not waiting for exit choice, ignore input
                return;
            }
            
            string trimmedInput = input?.Trim() ?? "";
            
            switch (trimmedInput)
            {
                case "1":
                    // Continue exploring
                    exitChoiceTaskSource.SetResult(false);
                    break;
                case "2":
                    // Leave dungeon safely
                    exitChoiceTaskSource.SetResult(true);
                    break;
                default:
                    ShowMessageEvent?.Invoke("Invalid choice. Please select 1 (Continue) or 2 (Leave safely).");
                    break;
            }
        }
        
        /// <summary>
        /// Check if we're currently waiting for exit choice input
        /// </summary>
        public bool IsWaitingForChoice => exitChoiceTaskSource != null && !exitChoiceTaskSource.Task.IsCompleted;
    }
}

