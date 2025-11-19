using System.Threading.Tasks;
using RPGGame;
using DungeonFighter.Game.Menu.Commands;
using DungeonFighter.Game.Menu.Core;

namespace DungeonFighter.Game.Menu.Handlers
{
    /// <summary>
    /// Refactored Main Menu Handler using the unified menu framework.
    /// Handles input for: New Game (1), Load Game (2), Settings (3), Exit (0)
    /// 
    /// BEFORE: ~200 lines with scattered logic
    /// AFTER: ~80 lines with clear separation of concerns
    /// </summary>
    public class MainMenuHandler : MenuHandlerBase
    {
        public override GameState TargetState => GameState.MainMenu;
        protected override string HandlerName => "MainMenu";

        /// <summary>
        /// Parse input into appropriate Main Menu command.
        /// </summary>
        protected override IMenuCommand? ParseInput(string input)
        {
            return input.Trim() switch
            {
                "1" => new StartNewGameCommand(),
                "2" => new LoadGameCommand(),
                "3" => new SettingsCommand(),
                "0" => new ExitGameCommand(),
                _ => null
            };
        }

        /// <summary>
        /// Execute command and return next game state.
        /// </summary>
        protected override async Task<GameState?> ExecuteCommand(IMenuCommand command)
        {
            // Create context with real state manager
            if (StateManager != null)
            {
                var context = new MenuContext(StateManager);
                await command.Execute(context);
            }
            else
            {
                DebugLogger.Log(HandlerName, "WARNING: StateManager is null, executing command with null context");
                await command.Execute(null);
            }

            // Determine next state based on command type
            return command switch
            {
                StartNewGameCommand => GameState.WeaponSelection,
                LoadGameCommand => GameState.GameLoop,
                SettingsCommand => GameState.Settings,
                ExitGameCommand => (GameState?)null, // ExitGameCommand handles exit logic internally
                _ => (GameState?)null
            };
        }
    }
}

