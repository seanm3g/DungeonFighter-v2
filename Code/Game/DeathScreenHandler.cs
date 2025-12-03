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
        /// Display the death screen with run statistics
        /// </summary>
        public void ShowDeathScreen(Character? player)
        {
            if (player == null) return;

            // End session and calculate final statistics
            player.SessionStats.EndSession();
            
            // Get defeat summary
            string defeatSummary = player.GetDefeatSummary();
            
            // Clear display buffer and suppress auto-rendering to prevent blank screen
            var uiManager = UIManager.GetCustomUIManager();
            if (uiManager is RPGGame.UI.Avalonia.CanvasUICoordinator canvasUI)
            {
                // Suppress display buffer auto-rendering FIRST to prevent any pending renders
                canvasUI.SuppressDisplayBufferRendering();
                // Clear buffer without triggering a render (since we're suppressing rendering anyway)
                canvasUI.ClearDisplayBufferWithoutRender();
                canvasUI.ClearClickableElements();
                
                // Render death screen using the canvas UI
                canvasUI.RenderDeathScreen(player, defeatSummary);
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
        /// Clears the display buffer when transitioning from death screen
        /// Also restores display buffer rendering for the main menu
        /// </summary>
        private void ClearDisplayIfNeeded()
        {
            var uiManager = UIManager.GetCustomUIManager();
            if (uiManager is RPGGame.UI.Avalonia.CanvasUICoordinator canvasUI)
            {
                // Restore display buffer rendering for main menu
                canvasUI.RestoreDisplayBufferRendering();
                canvasUI.ClearDisplayBuffer();
            }
        }
    }
}

