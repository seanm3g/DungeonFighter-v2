using System;
using System.Collections.Generic;
using RPGGame.UI;
using RPGGame.UI.Avalonia;

namespace RPGGame
{
    /// <summary>
    /// Central coordinator for high-level screen transitions.
    /// 
    /// Goal:
    /// - Provide a single place to manage how core screens are rendered
    ///   (Game Menu, Inventory, Dungeon Completion, etc.).
    /// - Make state + UI invariants explicit and reduce duplicated
    ///   CanvasUICoordinator access scattered across Game/handlers.
    /// 
    /// This is intentionally small and focused – it wraps existing
    /// CanvasUICoordinator APIs without changing their behavior.
    /// </summary>
    public class GameScreenCoordinator
    {
        private readonly GameStateManager stateManager;

        public GameScreenCoordinator(GameStateManager stateManager)
        {
            this.stateManager = stateManager ?? throw new ArgumentNullException(nameof(stateManager));
        }

        /// <summary>
        /// Helper to get the current CanvasUICoordinator, if any.
        /// Uses the global UIManager entry point so we don't have to
        /// plumb an IUIManager reference everywhere.
        /// </summary>
        private CanvasUICoordinator? TryGetCanvasUI()
        {
            var ui = UIManager.GetCustomUIManager();
            return ui as CanvasUICoordinator;
        }

        /// <summary>
        /// Show the main in-game menu (GameLoop screen).
        /// Ensures the persistent layout character panel is set and
        /// transitions state to GameLoop.
        /// </summary>
        public void ShowGameLoop()
        {
            var player = stateManager.CurrentPlayer;
            var inventory = stateManager.CurrentInventory;
            var canvasUI = TryGetCanvasUI();

            if (canvasUI != null && player != null)
            {
                // Restore display buffer rendering in case it was suppressed (e.g., from inventory screen)
                // This ensures dungeon exploration and combat screens work correctly
                canvasUI.RestoreDisplayBufferRendering();
                
                // Ensure character is set in UI coordinator for persistent display
                canvasUI.SetCharacter(player);
                canvasUI.RenderGameMenu(player, inventory);
            }

            stateManager.TransitionToState(GameState.GameLoop);
        }

        /// <summary>
        /// Show the dungeon completion screen with reward data.
        /// Centralizes the CanvasUICoordinator calls and state
        /// transition logic for this screen.
        /// </summary>
        public void ShowDungeonCompletion(int xpGained, Item? lootReceived, List<LevelUpInfo> levelUpInfos)
        {
            stateManager.TransitionToState(GameState.DungeonCompletion);

            var canvasUI = TryGetCanvasUI();
            var player = stateManager.CurrentPlayer;
            var dungeon = stateManager.CurrentDungeon;

            if (canvasUI == null || player == null || dungeon == null)
            {
                // If we can't render the completion screen, fail loudly in logs.
                DebugLogger.Log("GameScreenCoordinator",
                    $"ShowDungeonCompletion skipped: canvasUI={canvasUI != null}, player={player != null}, dungeon={dungeon != null}");
                return;
            }

            // Clear old interactive elements first
            canvasUI.ClearClickableElements();

            // Render the completion screen with reward data and level-up info
            canvasUI.RenderDungeonCompletion(
                dungeon,
                player,
                xpGained,
                lootReceived,
                levelUpInfos ?? new List<LevelUpInfo>()
            );
        }

        /// <summary>
        /// Show the inventory screen.
        /// Centralizes the CanvasUICoordinator calls for inventory rendering,
        /// ensuring clean transitions from other screens (e.g., dungeon completion).
        /// </summary>
        public void ShowInventory()
        {
            var canvasUI = TryGetCanvasUI();
            var player = stateManager.CurrentPlayer;
            var inventory = stateManager.CurrentInventory;

            if (canvasUI == null || player == null)
            {
                // If we can't render inventory, fail loudly in logs.
                DebugLogger.Log("GameScreenCoordinator",
                    $"ShowInventory skipped: canvasUI={canvasUI != null}, player={player != null}");
                // Still transition state so input routing works, but screen won't render
                stateManager.TransitionToState(GameState.Inventory);
                return;
            }

            // Ensure inventory is never null - use player's inventory if state manager's is null
            if (inventory == null)
            {
                DebugLogger.Log("GameScreenCoordinator",
                    "ShowInventory: CurrentInventory is null, using player.Inventory");
                inventory = player.Inventory ?? new List<Item>();
            }

            // Clear dungeon/room context when transitioning to inventory
            canvasUI.ClearCurrentEnemy();
            canvasUI.SetDungeonName(null);
            canvasUI.SetRoomName(null);

            // Suppress display buffer auto-rendering FIRST to prevent any pending renders
            // This prevents the display buffer from auto-rendering and clearing the inventory screen
            canvasUI.SuppressDisplayBufferRendering();
            // Clear buffer without triggering a render (since we're suppressing rendering anyway)
            canvasUI.ClearDisplayBufferWithoutRender();

            // Set character for persistent layout panel
            canvasUI.SetCharacter(player);

            // Render the inventory screen
            canvasUI.RenderInventory(player, inventory);

            // Force a refresh to ensure the screen is displayed
            // This is critical when transitioning from other screens
            canvasUI.Refresh();

            // Transition state after rendering
            stateManager.TransitionToState(GameState.Inventory);
        }
    }
}


