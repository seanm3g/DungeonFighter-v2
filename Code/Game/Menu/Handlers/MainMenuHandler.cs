using System.Threading.Tasks;
using RPGGame;
using DungeonFighter.Game.Menu.Core;

namespace DungeonFighter.Game.Menu.Handlers
{
    /// <summary>
    /// Refactored Main Menu Handler using direct method calls.
    /// Handles input for: New Game (1), Load Game (2), Settings (3), Exit (0)
    /// 
    /// Simplified from command pattern to direct handler methods for better maintainability.
    /// </summary>
    public class MainMenuHandler : MenuHandlerBase
    {
        public override GameState TargetState => GameState.MainMenu;
        protected override string HandlerName => "MainMenu";

        /// <summary>
        /// Handle input directly and return next game state.
        /// </summary>
        protected override async Task<GameState?> HandleInputDirect(string input)
        {
            return input.Trim() switch
            {
                "1" => await StartNewGame(),
                "2" => await LoadGame(),
                "3" => GameState.Settings,
                "0" => await ExitGame(),
                _ => null
            };
        }

        private Task<GameState?> StartNewGame()
        {
            LogStep("Starting new game");
            
            if (StateManager == null)
            {
                LogError("StateManager is null");
                return Task.FromResult<GameState?>(null);
            }

            // Create new character (null triggers random name generation)
            var newCharacter = new Character(null, 1);
            StateManager.SetCurrentPlayer(newCharacter);
            
            // Apply health multiplier if configured
            var settings = GameSettings.Instance;
            if (settings.PlayerHealthMultiplier != 1.0)
            {
                newCharacter.ApplyHealthMultiplier(settings.PlayerHealthMultiplier);
            }
            
            LogStep("New character created");
            return Task.FromResult<GameState?>(GameState.WeaponSelection);
        }

        private Task<GameState?> LoadGame()
        {
            LogStep("Loading game");
            
            if (StateManager == null)
            {
                LogError("StateManager is null");
                return Task.FromResult<GameState?>(null);
            }

            var savedCharacter = Character.LoadCharacter();
            if (savedCharacter != null)
            {
                StateManager.SetCurrentPlayer(savedCharacter);
                
                // Apply health multiplier if configured
                var settings = GameSettings.Instance;
                if (settings.PlayerHealthMultiplier != 1.0)
                {
                    savedCharacter.ApplyHealthMultiplier(settings.PlayerHealthMultiplier);
                }
                
                LogStep("Game loaded successfully");
                return Task.FromResult<GameState?>(GameState.GameLoop);
            }
            else
            {
                LogError("No saved game found");
                return Task.FromResult<GameState?>(null);
            }
        }

        private Task<GameState?> ExitGame()
        {
            LogStep("Exiting game");
            
            if (StateManager?.CurrentPlayer != null)
            {
                // Save character before exit
                StateManager.CurrentPlayer.SaveCharacter();
                LogStep("Game saved before exit");
            }
            
            return Task.FromResult<GameState?>(null); // Signal exit
        }
    }
}

