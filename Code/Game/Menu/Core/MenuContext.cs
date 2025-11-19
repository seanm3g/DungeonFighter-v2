using RPGGame;

namespace DungeonFighter.Game.Menu.Core
{
    /// <summary>
    /// Provides context for menu command execution.
    /// Encapsulates access to game systems and state managers.
    /// </summary>
    public class MenuContext : IMenuContext
    {
        /// <summary>
        /// Gets the state manager for state transitions and queries.
        /// </summary>
        public GameStateManager StateManager { get; }

        /// <summary>
        /// Creates a new MenuContext with the required dependencies.
        /// </summary>
        /// <param name="stateManager">The game state manager</param>
        public MenuContext(GameStateManager stateManager)
        {
            StateManager = stateManager ?? throw new System.ArgumentNullException(nameof(stateManager));
            DebugLogger.Log("MenuContext", "Context created with StateManager");
        }
    }
}

