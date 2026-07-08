namespace RPGGame
{
    using System;
    using System.Threading.Tasks;
    using RPGGame.UI;
    using RPGGame.UI.ColorSystem;
    using RPGGame.Utils;

    /// <summary>
    /// Handles death screen display and navigation back to main menu.
    /// Shows comprehensive run statistics when the player dies.
    /// </summary>
    public class DeathScreenHandler
    {
        private GameStateManager stateManager;
        
        // Delegates
        public delegate void OnShowMainMenu();
        public delegate void OnShowGameLoop();
        
        public event OnShowMainMenu? ShowMainMenuEvent;
        public event OnShowGameLoop? ShowGameLoopEvent;

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
                promptBuilder.Add("1 - Clone this hero (equipped gear is lost)\n", ColorPalette.Warning);
                promptBuilder.Add("0 - Return to main menu", ColorPalette.Warning);
                UIManager.WriteLineColoredTextBuilder(promptBuilder, UIMessageType.System);
            }
        }

        /// <summary>
        /// Handles the death-screen fate choice.
        /// </summary>
        public async Task HandleMenuInput(string input)
        {
            if (string.Equals(input?.Trim(), "1", StringComparison.OrdinalIgnoreCase))
            {
                await CloneAndReturnToGameLoopAsync().ConfigureAwait(true);
                return;
            }

            await ReturnToMainMenuAsDeadAsync().ConfigureAwait(true);
        }

        private async Task CloneAndReturnToGameLoopAsync()
        {
            var player = stateManager.CurrentPlayer ?? stateManager.GetActiveCharacter();
            if (player == null)
            {
                await ReturnToMainMenuAsDeadAsync().ConfigureAwait(true);
                return;
            }

            // Clear dungeon state
            stateManager.SetCurrentDungeon(null);
            stateManager.SetCurrentRoom(null);
            ClearCurrentEnemy();

            CharacterCloneService.CloneAfterDeath(player);
            stateManager.SetCurrentPlayer(player);

            try
            {
                var characterId = stateManager.GetCharacterId(player);
                await CharacterSaveManager.SaveCharacterAsync(player, characterId).ConfigureAwait(true);
            }
            catch (Exception ex)
            {
                DebugLogger.Log("DeathScreenHandler", $"Save cloned character failed: {ex.Message}");
            }

            ClearDisplayIfNeeded();
            if (ShowGameLoopEvent != null)
            {
                ShowGameLoopEvent.Invoke();
            }
            else
            {
                new GameScreenCoordinator(stateManager).ShowGameLoop();
            }
        }

        private async Task ReturnToMainMenuAsDeadAsync()
        {
            var player = stateManager.CurrentPlayer ?? stateManager.GetActiveCharacter();
            if (player != null)
            {
                try
                {
                    var characterId = stateManager.GetCharacterId(player);
                    await CharacterSaveManager.SaveCharacterAsync(player, characterId, filename: null, markDead: true).ConfigureAwait(true);
                }
                catch (Exception ex)
                {
                    DebugLogger.Log("DeathScreenHandler", $"Persist dead character save failed: {ex.Message}");
                }
            }

            stateManager.SetCurrentDungeon(null);
            stateManager.SetCurrentRoom(null);
            stateManager.SetCurrentPlayer(null);
            ClearCurrentEnemy();

            stateManager.TransitionToState(GameState.MainMenu);
            ClearDisplayIfNeeded();
            ShowMainMenuEvent?.Invoke();
        }

        private void ClearCurrentEnemy()
        {
            // Clear enemy from UI to prevent it from showing when starting a new game
            var uiManager = UIManager.GetCustomUIManager();
            if (uiManager is RPGGame.UI.Avalonia.CanvasUICoordinator canvasUI)
            {
                canvasUI.ClearCurrentEnemy();
            }
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

