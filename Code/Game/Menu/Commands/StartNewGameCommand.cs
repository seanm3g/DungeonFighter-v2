using System.Threading.Tasks;
using RPGGame;
using DungeonFighter.Game.Menu.Core;
using RPGGame.Combat.Events;
using RPGGame.UI.ColorSystem;

namespace DungeonFighter.Game.Menu.Commands
{
    /// <summary>
    /// Command for starting a new game.
    /// Creates a new character and transitions to weapon selection.
    /// </summary>
    public class StartNewGameCommand : MenuCommand
    {
        protected override string CommandName => "StartNewGame";

        protected override async Task ExecuteCommand(IMenuContext? context)
        {
            LogStep("Starting new game flow");
            
            if (context?.StateManager != null)
            {
                // Reset all game state first to ensure clean start
                // This prevents test state or previous game state from affecting the new game
                context.StateManager.ResetGameState();
                
                // Reset static state that may have been modified by tests or previous sessions
                ResetStaticState();
                
                // Create new character (null triggers random name generation)
                var newCharacter = new Character(null, 1);
                context.StateManager.SetCurrentPlayer(newCharacter);
                
                // Apply health multiplier if configured
                var settings = GameSettings.Instance;
                if (settings.PlayerHealthMultiplier != 1.0)
                {
                    newCharacter.ApplyHealthMultiplier(settings.PlayerHealthMultiplier);
                }
                
                LogStep("New character created, transitioning to weapon selection");
            }
            
            await Task.CompletedTask;
        }

        /// <summary>
        /// Resets static state that may have been modified by tests or previous game sessions.
        /// This ensures a clean state when starting a new game.
        /// </summary>
        private static void ResetStaticState()
        {
            try
            {
                // Reset UIManager custom UI manager (tests may set it to null)
                UIManager.SetCustomUIManager(null);
                UIManager.ReloadConfiguration();
                
                // Clear CombatEventBus subscribers (tests may add subscribers)
                CombatEventBus.Instance.Clear();
                
                // Stop GameTicker if it's running (tests may start it)
                if (GameTicker.Instance.IsRunning)
                {
                    GameTicker.Instance.Stop();
                }
                
                // Reset GameTicker time
                GameTicker.Instance.Reset();
                
                // Reset UIManager flags
                UIManager.DisableAllUIOutput = false;
                
                // Clear character names from KeywordColorSystem (tests may register character names)
                // This prevents test character names from affecting the game's character name coloring
                KeywordColorSystem.ClearCharacterNames();
                
                // Reset GameConfiguration singleton to force reload from file
                // Tests may have modified the instance in memory, so we need to reload it
                GameConfiguration.ResetInstance();
                
                // Reset UI display state - clear any persistent display buffers
                var uiManager = UIManager.GetCustomUIManager();
                if (uiManager is RPGGame.UI.Avalonia.CanvasUICoordinator canvasUI)
                {
                    canvasUI.ClearCurrentEnemy();
                    canvasUI.ClearDisplayBuffer();
                }
            }
            catch (System.Exception ex)
            {
                // Log but don't fail - cleanup is best effort
                // Note: We can't use DebugLogger here as it might not be available in command context
                System.Console.WriteLine($"Warning: Error during static state reset: {ex.Message}");
            }
        }
    }
}

