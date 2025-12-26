using RPGGame;
using RPGGame.UI.Avalonia.Display;
using RPGGame.UI.Avalonia.Managers;
using System.Collections.Generic;

namespace RPGGame.UI.Avalonia.Coordinators
{
    /// <summary>
    /// Coordinates UI context management operations.
    /// Extracts context management logic from CanvasUICoordinator to improve Single Responsibility Principle compliance.
    /// </summary>
    public class UIContextCoordinator
    {
        private readonly ICanvasContextManager contextManager;
        private readonly ICanvasTextManager textManager;
        private GameStateManager? stateManager;

        public UIContextCoordinator(
            ICanvasContextManager contextManager,
            ICanvasTextManager textManager,
            GameStateManager? stateManager = null)
        {
            this.contextManager = contextManager ?? throw new System.ArgumentNullException(nameof(contextManager));
            this.textManager = textManager ?? throw new System.ArgumentNullException(nameof(textManager));
            this.stateManager = stateManager;
        }

        /// <summary>
        /// Sets the game state manager (called after construction when state manager is available).
        /// </summary>
        public void SetStateManager(GameStateManager stateManager)
        {
            this.stateManager = stateManager ?? throw new System.ArgumentNullException(nameof(stateManager));
        }

        /// <summary>
        /// Sets the dungeon context, with logic to prevent setting context in menu states.
        /// </summary>
        public void SetDungeonContext(List<string> context)
        {
            // If we're in a menu state, don't set dungeon context that might contain enemy info
            // This prevents background combat from setting dungeon context with enemy info when in menus
            var currentState = stateManager?.CurrentState;
            bool isMenuState = DisplayStateCoordinator.IsMenuState(currentState);

            if (isMenuState)
            {
                // In menu state - clear dungeon context instead of setting it
                // This ensures old enemy info doesn't persist when switching to menus
                contextManager.ClearDungeonContext();
            }
            else
            {
                contextManager.SetDungeonContext(context);
            }
        }

        /// <summary>
        /// Sets the current character, with logic to handle display buffer clearing.
        /// </summary>
        public void SetCharacter(Character? character)
        {
            // Only set character if it matches the active character
            // This prevents background combat from changing the character context
            var activeCharacter = stateManager?.GetActiveCharacter();

            if (character == null || character == activeCharacter)
            {
                var previousCharacter = contextManager.GetCurrentCharacter();
                contextManager.SetCurrentCharacter(character);

                // CRITICAL: If the character changed, clear the display buffer
                // However, if we're in a menu state, don't trigger a render as it will interfere
                // with menu rendering and cause flashing. Use ClearWithoutRender instead.
                if (previousCharacter != character && textManager is CanvasTextManager canvasTextManager)
                {
                    // Check if we're in a menu state where display buffer rendering should be suppressed
                    var currentState = stateManager?.CurrentState;
                    bool isMenuState = DisplayStateCoordinator.IsMenuState(currentState);

                    if (isMenuState)
                    {
                        // In menu state - clear without triggering render to avoid interfering with menu rendering
                        canvasTextManager.DisplayManager.ClearWithoutRender();
                    }
                    else
                    {
                        // Not in menu state - clear and trigger render normally
                        canvasTextManager.DisplayManager.Clear();
                        canvasTextManager.DisplayManager.TriggerRender();
                    }
                }

                // Character panel will auto-update via contextManager
            }
            // If character doesn't match active, this is background combat - don't change context
        }

        /// <summary>
        /// Sets the current enemy, with validation to prevent setting enemy in menu states or for inactive characters.
        /// </summary>
        public void SetCurrentEnemy(Enemy enemy)
        {
            // Only set enemy if:
            // 1. The current character matches the active character
            // 2. We're NOT in a menu state (menus don't allow combat enemy display)
            // This prevents background combat from setting enemy context for inactive characters or when in menus
            var currentCharacter = contextManager.GetCurrentCharacter();
            var activeCharacter = stateManager?.GetActiveCharacter();
            var currentState = stateManager?.CurrentState;
            bool characterMatches = currentCharacter != null && currentCharacter == activeCharacter;

            // Menu states where combat shouldn't set enemy context
            bool isMenuState = DisplayStateCoordinator.IsMenuState(currentState);

            if (characterMatches && !isMenuState && enemy != null)
            {
                contextManager.SetCurrentEnemy(enemy);
            }
            else
            {
                // If we're in a menu state, also clear any existing enemy to ensure clean state
                // This handles the case where an enemy was set before entering a menu
                if (isMenuState)
                {
                    contextManager.ClearCurrentEnemy();
                }
            }
            // If characters don't match or in menu state, this is background combat or menu - don't set enemy context
        }

        /// <summary>
        /// Checks if a character is currently the active character.
        /// </summary>
        public bool IsCharacterActive(Character? character)
        {
            return DisplayStateCoordinator.IsCharacterActive(character, stateManager);
        }

        /// <summary>
        /// Sets the dungeon name in the context.
        /// </summary>
        public void SetDungeonName(string? dungeonName)
        {
            contextManager.SetDungeonName(dungeonName);
        }

        /// <summary>
        /// Sets the room name in the context.
        /// </summary>
        public void SetRoomName(string? roomName)
        {
            contextManager.SetRoomName(roomName);
        }

        /// <summary>
        /// Clears the current enemy from the context.
        /// </summary>
        public void ClearCurrentEnemy()
        {
            contextManager.ClearCurrentEnemy();
        }
    }
}

