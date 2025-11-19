using RPGGame;
using DungeonFighter.Game.Menu.Core;

namespace DungeonFighter.Game.Menu.Routing
{
    /// <summary>
    /// Interface for menu input validation.
    /// Centralizes input validation logic for all menus.
    /// </summary>
    public interface IMenuInputValidator
    {
        /// <summary>
        /// Validates input for a specific game state.
        /// </summary>
        /// <param name="input">The input to validate</param>
        /// <param name="state">The current game state</param>
        /// <returns>Validation result</returns>
        ValidationResult Validate(string input, GameState state);
    }
}

