using System.Threading.Tasks;
using RPGGame;

namespace DungeonFighter.Game.Menu.Core
{
    /// <summary>
    /// Unified interface for all menu handlers.
    /// Defines the contract that all menu handlers must implement.
    /// </summary>
    public interface IMenuHandler
    {
        /// <summary>
        /// Gets the game state this handler is responsible for.
        /// </summary>
        GameState TargetState { get; }

        /// <summary>
        /// Processes user input for this menu.
        /// </summary>
        /// <param name="input">The user input string (e.g., "1", "2", "help")</param>
        /// <returns>Result of the input processing</returns>
        Task<MenuInputResult> HandleInput(string input);
    }
}

