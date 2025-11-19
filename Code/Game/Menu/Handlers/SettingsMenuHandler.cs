using System.Threading.Tasks;
using RPGGame;
using DungeonFighter.Game.Menu.Commands;
using DungeonFighter.Game.Menu.Core;

namespace DungeonFighter.Game.Menu.Handlers
{
    /// <summary>
    /// Refactored Settings Menu Handler using the unified menu framework.
    /// Handles settings option selection and toggling.
    /// 
    /// BEFORE: ~150 lines with mixed logic
    /// AFTER: ~80 lines with clean command pattern
    /// </summary>
    public class SettingsMenuHandler : MenuHandlerBase
    {
        public override GameState TargetState => GameState.Settings;
        protected override string HandlerName => "Settings";

        /// <summary>
        /// Parse input into settings command.
        /// Supports: setting options (1-9) and action keys
        /// </summary>
        protected override IMenuCommand? ParseInput(string input)
        {
            string cleaned = input.Trim();

            if (cleaned.Length != 1)
                return null;

            return cleaned.ToLower() switch
            {
                // Settings options (mapped to actual settings)
                "1" => new ToggleOptionCommand("Difficulty"),
                "2" => new ToggleOptionCommand("Sound"),
                "3" => new ToggleOptionCommand("Music"),
                "4" => new ToggleOptionCommand("Animations"),
                "5" => new ToggleOptionCommand("ShowNumbers"),
                "6" => new ToggleOptionCommand("ShowStats"),
                "7" => new ToggleOptionCommand("AutoSave"),
                "8" => new ToggleOptionCommand("Colorblind"),
                "9" => new ToggleOptionCommand("DEBUG"),
                
                // Actions
                "c" => new SelectOptionCommand(0, "ConfirmSettings"),  // Confirm changes
                "0" => new CancelCommand("Settings"),
                
                _ => null
            };
        }

        /// <summary>
        /// Execute command and determine next state.
        /// </summary>
        protected override async Task<GameState?> ExecuteCommand(IMenuCommand command)
        {
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

            return command switch
            {
                SelectOptionCommand => GameState.MainMenu,  // Confirm and return
                CancelCommand => GameState.MainMenu,        // Cancel and return
                _ => (GameState?)null  // Stay in settings
            };
        }
    }
}

