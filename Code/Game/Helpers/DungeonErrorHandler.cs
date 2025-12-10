using System;
using RPGGame.UI;
using RPGGame.UI.Avalonia;
using RPGGame.Utils;

namespace RPGGame.GameCore.Helpers
{
    /// <summary>
    /// Handles error scenarios in dungeon execution
    /// </summary>
    public static class DungeonErrorHandler
    {
        /// <summary>
        /// Handles missing components error and returns to dungeon selection
        /// </summary>
        public static void HandleMissingComponents(GameStateManager stateManager, IUIManager? customUIManager)
        {
            if (customUIManager is CanvasUICoordinator canvasUI)
            {
                canvasUI.WriteLine("ERROR: Cannot start dungeon - missing required components.", UIMessageType.System);
            }
            stateManager.TransitionToState(GameState.DungeonSelection);
            if (customUIManager is CanvasUICoordinator canvasUI2 && stateManager.CurrentPlayer != null)
            {
                canvasUI2.RenderDungeonSelection(stateManager.CurrentPlayer, stateManager.AvailableDungeons);
            }
        }

        /// <summary>
        /// Handles dungeon generation error and returns to dungeon selection
        /// </summary>
        public static void HandleDungeonGenerationError(GameStateManager stateManager, IUIManager? customUIManager, string errorMessage)
        {
            if (customUIManager is CanvasUICoordinator canvasUI)
            {
                canvasUI.WriteLine($"ERROR: {errorMessage}", UIMessageType.System);
            }
            stateManager.TransitionToState(GameState.DungeonSelection);
            if (customUIManager is CanvasUICoordinator canvasUI2 && stateManager.CurrentPlayer != null)
            {
                canvasUI2.RenderDungeonSelection(stateManager.CurrentPlayer, stateManager.AvailableDungeons);
            }
        }

        /// <summary>
        /// Handles exception during dungeon execution
        /// </summary>
        public static void HandleException(Exception ex, IUIManager? customUIManager)
        {
            if (customUIManager is CanvasUICoordinator canvasUI)
            {
                canvasUI.WriteLine($"ERROR: Failed to run dungeon: {ex.Message}", UIMessageType.System);
            }
        }
    }
}

