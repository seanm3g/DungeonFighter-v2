namespace RPGGame
{
    using System;
    using System.Threading.Tasks;
    using RPGGame.UI;
    using RPGGame.UI.ColorSystem;

    /// <summary>
    /// Handles death screen display and navigation back to main menu.
    /// Shows comprehensive run statistics when the player dies.
    /// </summary>
    public class DeathScreenHandler
    {
        private GameStateManager stateManager;
        
        // Delegates
        public delegate void OnShowMainMenu();
        
        public event OnShowMainMenu? ShowMainMenuEvent;

        public DeathScreenHandler(GameStateManager stateManager)
        {
            this.stateManager = stateManager ?? throw new ArgumentNullException(nameof(stateManager));
        }

        /// <summary>
        /// Display the death screen with run statistics.
        /// Note: This method is kept for console UI fallback and backward compatibility.
        /// For CanvasUI, Game.cs uses GameScreenCoordinator.ShowDeathScreen() which uses
        /// the standardized ScreenTransitionProtocol.
        /// </summary>
        public void ShowDeathScreen(Character? player)
        {
            if (player == null) return;

            // End session and calculate final statistics
            player.SessionStats.EndSession();
            
            // Get defeat summary
            string defeatSummary = player.GetDefeatSummary();
            
            var uiManager = UIManager.GetCustomUIManager();
            if (uiManager is RPGGame.UI.Avalonia.CanvasUICoordinator canvasUI)
            {
                // Use GameScreenCoordinator for standardized screen transition
                // This ensures consistent behavior with the protocol
                var screenCoordinator = new GameScreenCoordinator(stateManager);
                screenCoordinator.ShowDeathScreen(player);
            }
            else
            {
                // Fallback for console UI
                // Create colored death screen
                var deathBuilder = new ColoredTextBuilder();
                deathBuilder.Add("\n\n", ColorPalette.White);
                deathBuilder.Add("═══════════════════════════════════════\n", ColorPalette.Error);
                deathBuilder.Add("              YOU DIED\n", ColorPalette.Error);
                deathBuilder.Add("═══════════════════════════════════════\n\n", ColorPalette.Error);
                
                // Display defeat summary using UIManager
                UIManager.WriteLineColoredTextBuilder(deathBuilder, UIMessageType.System);
                
                // Display statistics line by line
                string[] summaryLines = defeatSummary.Split('\n');
                foreach (string line in summaryLines)
                {
                    if (!string.IsNullOrWhiteSpace(line))
                    {
                        UIManager.WriteLine(line, UIMessageType.System);
                    }
                }
                
                // Add prompt to continue
                var promptBuilder = new ColoredTextBuilder();
                promptBuilder.Add("\n\n", ColorPalette.White);
                promptBuilder.Add("Press any key to return to main menu...", ColorPalette.Warning);
                UIManager.WriteLineColoredTextBuilder(promptBuilder, UIMessageType.System);
            }
        }

        /// <summary>
        /// Handle any key press to return to main menu
        /// </summary>
        public async Task HandleMenuInput(string input)
        {
            // Any key press returns to main menu
            // Clear dungeon state
            stateManager.SetCurrentDungeon(null);
            stateManager.SetCurrentRoom(null);
            
            // Clear player reference (character is dead)
            stateManager.SetCurrentPlayer(null);
            
            // Clear enemy from UI to prevent it from showing when starting a new game
            var uiManager = UIManager.GetCustomUIManager();
            if (uiManager is RPGGame.UI.Avalonia.CanvasUICoordinator canvasUI)
            {
                canvasUI.ClearCurrentEnemy();
            }
            
            // Transition to main menu
            stateManager.TransitionToState(GameState.MainMenu);
            
            // Clear display buffer
            ClearDisplayIfNeeded();
            
            // Show main menu
            ShowMainMenuEvent?.Invoke();
            
            await Task.CompletedTask;
        }
        
        /// <summary>
        /// Clears the display buffer when transitioning from death screen.
        /// DisplayBufferManager will automatically handle restoration when transitioning to MainMenu.
        /// </summary>
        private void ClearDisplayIfNeeded()
        {
            var uiManager = UIManager.GetCustomUIManager();
            if (uiManager is RPGGame.UI.Avalonia.CanvasUICoordinator canvasUI)
            {
                // Clear buffer - DisplayBufferManager will handle restoration automatically
                // when state transitions to MainMenu (non-menu state)
                canvasUI.ClearDisplayBuffer();
            }
        }
    }
}

