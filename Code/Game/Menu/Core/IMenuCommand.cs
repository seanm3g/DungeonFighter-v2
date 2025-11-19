using System.Threading.Tasks;
using RPGGame;

namespace DungeonFighter.Game.Menu.Core
{
    /// <summary>
    /// Interface for menu command execution.
    /// Represents an executable action that can be performed from a menu.
    /// Uses the Command Pattern to decouple menu logic from business logic.
    /// </summary>
    public interface IMenuCommand
    {
        /// <summary>
        /// Executes the command with the provided context.
        /// </summary>
        /// <param name="context">The menu context containing references to game systems (can be null for testing)</param>
        Task Execute(IMenuContext? context);
    }

    /// <summary>
    /// Interface providing context for command execution.
    /// Gives commands access to game systems they might need.
    /// </summary>
    public interface IMenuContext
    {
        /// <summary>
        /// Gets the state manager for state transitions.
        /// </summary>
        GameStateManager StateManager { get; }

        /// <summary>
        /// Gets the UI manager for display operations.
        /// </summary>
        // TODO: Add proper UI interface when ready
        // IUIManager UIManager { get; }
    }
}

