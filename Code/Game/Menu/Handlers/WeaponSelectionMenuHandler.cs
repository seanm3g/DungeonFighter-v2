using System.Threading.Tasks;
using RPGGame;
using DungeonFighter.Game.Menu.Core;

namespace DungeonFighter.Game.Menu.Handlers
{
    /// <summary>
    /// Refactored Weapon Selection Menu Handler using direct method calls.
    /// Handles weapon selection by index.
    /// </summary>
    public class WeaponSelectionMenuHandler : MenuHandlerBase
    {
        private const int WeaponCount = 4;

        public override GameState TargetState => GameState.WeaponSelection;
        protected override string HandlerName => "WeaponSelection";

        /// <summary>
        /// Handle input directly and return next game state.
        /// </summary>
        protected override async Task<GameState?> HandleInputDirect(string input)
        {
            string cleaned = input.Trim();

            // Try to parse as weapon number
            if (int.TryParse(cleaned, out int weaponNum))
            {
                if (weaponNum >= 1 && weaponNum <= WeaponCount)
                    return await SelectWeapon(weaponNum);
                else
                    return null;  // Invalid weapon number
            }

            // Handle action keys
            return cleaned.ToLower() switch
            {
                "c" => await SelectWeapon(0),  // Confirm current selection
                "0" => GameState.MainMenu,
                _ => null
            };
        }

        private Task<GameState?> SelectWeapon(int weaponIndex)
        {
            LogStep($"Selecting weapon index: {weaponIndex}");
            // Weapon selection logic would be handled by WeaponSelectionHandler
            // This handler just marks the selection and transitions
            return Task.FromResult<GameState?>(GameState.CharacterCreation);
        }
    }
}

