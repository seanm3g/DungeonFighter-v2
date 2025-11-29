using System.Threading.Tasks;
using RPGGame;
using DungeonFighter.Game.Menu.Core;

namespace DungeonFighter.Game.Menu.Handlers
{
    /// <summary>
    /// Refactored Settings Menu Handler using direct method calls.
    /// Handles settings option selection and toggling.
    /// </summary>
    public class SettingsMenuHandler : MenuHandlerBase
    {
        public override GameState TargetState => GameState.Settings;
        protected override string HandlerName => "Settings";

        /// <summary>
        /// Handle input directly and return next game state.
        /// </summary>
        protected override async Task<GameState?> HandleInputDirect(string input)
        {
            string cleaned = input.Trim().ToLower();

            if (cleaned.Length != 1)
                return null;

            return cleaned switch
            {
                // Settings options (mapped to actual settings)
                "1" => await ToggleSetting("Difficulty"),
                "2" => await ToggleSetting("Sound"),
                "3" => await ToggleSetting("Music"),
                "4" => await ToggleSetting("Animations"),
                "5" => await ToggleSetting("ShowNumbers"),
                "6" => await ToggleSetting("ShowStats"),
                "7" => await ToggleSetting("AutoSave"),
                "8" => await ToggleSetting("Colorblind"),
                "9" => await ToggleSetting("DEBUG"),
                // Actions
                "c" => GameState.MainMenu,  // Confirm and return
                "0" => GameState.MainMenu,  // Cancel and return
                _ => null
            };
        }

        private Task<GameState?> ToggleSetting(string optionName)
        {
            LogStep($"Toggling {optionName} option");
            // Settings toggle logic would be handled by SettingsManager
            // This handler just marks the action
            return Task.FromResult<GameState?>(null); // Stay in settings
        }
    }
}

