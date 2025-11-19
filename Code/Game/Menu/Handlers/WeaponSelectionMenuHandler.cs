using System.Threading.Tasks;
using RPGGame;
using DungeonFighter.Game.Menu.Commands;
using DungeonFighter.Game.Menu.Core;

namespace DungeonFighter.Game.Menu.Handlers
{
    /// <summary>
    /// Refactored Weapon Selection Menu Handler using the unified menu framework.
    /// Handles weapon selection by index.
    /// 
    /// BEFORE: ~150 lines with type parsing logic
    /// AFTER: ~80 lines with clean command pattern
    /// </summary>
    public class WeaponSelectionMenuHandler : MenuHandlerBase
    {
        private const int WeaponCount = 4;

        public override GameState TargetState => GameState.WeaponSelection;
        protected override string HandlerName => "WeaponSelection";

        /// <summary>
        /// Parse input into weapon selection command.
        /// Supports: weapon numbers (1-4) and action keys
        /// </summary>
        protected override IMenuCommand? ParseInput(string input)
        {
            string cleaned = input.Trim();

            // Try to parse as weapon number
            if (int.TryParse(cleaned, out int weaponNum))
            {
                if (weaponNum >= 1 && weaponNum <= WeaponCount)
                    return new SelectWeaponCommand(weaponNum);
                else
                    return null;  // Invalid weapon number
            }

            // Handle action keys
            return cleaned.ToLower() switch
            {
                "c" => new SelectWeaponCommand(0),  // Confirm current selection
                "0" => new CancelCommand("WeaponSelection"),
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
                SelectWeaponCommand => GameState.CharacterCreation,
                CancelCommand => GameState.MainMenu,
                _ => (GameState?)null  // Stay in weapon selection
            };
        }
    }
}

