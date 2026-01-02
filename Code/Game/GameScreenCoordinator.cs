using System;
using System.Collections.Generic;
using RPGGame.UI;
using RPGGame.UI.Avalonia;
using RPGGame.UI.Avalonia.Transitions;
using RPGGame.Utils;

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
    /// - Use standardized ScreenTransitionProtocol for consistent behavior.
    /// 
    /// This coordinator now handles ALL screen transitions using the
    /// standardized protocol to ensure consistent behavior.
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
        /// Uses standardized ScreenTransitionProtocol for consistent behavior.
        /// </summary>
        public void ShowGameLoop()
        {
            var player = stateManager.CurrentPlayer ?? stateManager.GetActiveCharacter();
            var inventory = stateManager.CurrentInventory;
            var canvasUI = TryGetCanvasUI();

            if (canvasUI == null || player == null)
            {
                // If no character is available, log error and return to main menu
                if (player == null)
                {
                    DebugLogger.Log("GameScreenCoordinator", "Error: ShowGameLoop called but no character is available");
                }
                stateManager.TransitionToState(GameState.GameLoop);
                return;
            }

            ScreenTransitionProtocol.TransitionToMenuScreen(
                stateManager,
                canvasUI,
                GameState.GameLoop,
                (ui) => ui.RenderGameMenu(player, inventory),
                character: player,
                clearEnemyContext: true,
                clearDungeonContext: false
            );
        }

        /// <summary>
        /// Show the dungeon completion screen with reward data.
        /// Uses standardized ScreenTransitionProtocol for consistent behavior.
        /// </summary>
        public void ShowDungeonCompletion(int xpGained, Item? lootReceived, List<LevelUpInfo> levelUpInfos, List<Item> itemsFoundDuringRun)
        {
            var canvasUI = TryGetCanvasUI();
            var player = stateManager.CurrentPlayer;
            var dungeon = stateManager.CurrentDungeon;

            if (canvasUI == null || player == null || dungeon == null)
            {
                stateManager.TransitionToState(GameState.DungeonCompletion);
                return;
            }

            ScreenTransitionProtocol.TransitionToMenuScreen(
                stateManager,
                canvasUI,
                GameState.DungeonCompletion,
                (ui) => ui.RenderDungeonCompletion(
                    dungeon,
                    player,
                    xpGained,
                    lootReceived,
                    levelUpInfos ?? new List<LevelUpInfo>(),
                    itemsFoundDuringRun ?? new List<Item>()
                ),
                character: player,
                clearEnemyContext: true,
                clearDungeonContext: false
            );
        }

        /// <summary>
        /// Show the inventory screen.
        /// Uses standardized ScreenTransitionProtocol for consistent behavior.
        /// </summary>
        public void ShowInventory()
        {
            var canvasUI = TryGetCanvasUI();
            var player = stateManager.CurrentPlayer;
            var inventory = stateManager.CurrentInventory;

            if (canvasUI == null || player == null)
            {
                stateManager.TransitionToState(GameState.Inventory);
                return;
            }

            // Ensure inventory is never null - use player's inventory if state manager's is null
            if (inventory == null)
            {
                inventory = player.Inventory ?? new List<Item>();
            }

            ScreenTransitionProtocol.TransitionToMenuScreen(
                stateManager,
                canvasUI,
                GameState.Inventory,
                (ui) => ui.RenderInventory(player, inventory),
                character: player,
                clearEnemyContext: true,
                clearDungeonContext: true
            );
        }

        /// <summary>
        /// Show the main menu.
        /// Uses standardized ScreenTransitionProtocol for consistent behavior.
        /// </summary>
        public void ShowMainMenu(bool hasSavedGame = false, string? characterName = null, int characterLevel = 0)
        {
            var canvasUI = TryGetCanvasUI();

            if (canvasUI == null)
            {
                stateManager.TransitionToState(GameState.MainMenu);
                return;
            }

            ScreenTransitionProtocol.TransitionToMenuScreen(
                stateManager,
                canvasUI,
                GameState.MainMenu,
                (ui) => ui.RenderMainMenu(hasSavedGame, characterName, characterLevel),
                character: null,
                clearEnemyContext: true,
                clearDungeonContext: true
            );
        }

        /// <summary>
        /// Show the death screen with run statistics.
        /// Uses standardized ScreenTransitionProtocol for consistent behavior.
        /// </summary>
        public void ShowDeathScreen(Character player)
        {
            if (player == null) return;

            // End session and calculate final statistics
            player.SessionStats.EndSession();
            
            // Get defeat summary
            string defeatSummary = player.GetDefeatSummary();

            var canvasUI = TryGetCanvasUI();

            if (canvasUI == null)
            {
                stateManager.TransitionToState(GameState.Death);
                return;
            }

            ScreenTransitionProtocol.TransitionToMenuScreen(
                stateManager,
                canvasUI,
                GameState.Death,
                (ui) => ui.RenderDeathScreen(player, defeatSummary),
                character: player,
                clearEnemyContext: true,
                clearDungeonContext: true
            );
        }

        /// <summary>
        /// Show the dungeon selection screen.
        /// Uses standardized ScreenTransitionProtocol for consistent behavior.
        /// </summary>
        public void ShowDungeonSelection()
        {
            var canvasUI = TryGetCanvasUI();
            var player = stateManager.CurrentPlayer;
            var dungeons = stateManager.AvailableDungeons;

            if (canvasUI == null || player == null || dungeons == null)
            {
                stateManager.TransitionToState(GameState.DungeonSelection);
                return;
            }

            ScreenTransitionProtocol.TransitionToMenuScreen(
                stateManager,
                canvasUI,
                GameState.DungeonSelection,
                (ui) => ui.RenderDungeonSelection(player, dungeons),
                character: player,
                clearEnemyContext: true,
                clearDungeonContext: true
            );
        }

        /// <summary>
        /// Show the settings menu.
        /// Uses standardized ScreenTransitionProtocol for consistent behavior.
        /// </summary>
        public void ShowSettings()
        {
            var canvasUI = TryGetCanvasUI();

            if (canvasUI == null)
            {
                stateManager.TransitionToState(GameState.Settings);
                return;
            }

            ScreenTransitionProtocol.TransitionToMenuScreen(
                stateManager,
                canvasUI,
                GameState.Settings,
                (ui) => ui.RenderSettings(),
                character: null,
                clearEnemyContext: true,
                clearDungeonContext: true
            );
        }

        /// <summary>
        /// Show the character info screen.
        /// Character info is displayed in the persistent layout panel, so we just set the character and transition state.
        /// </summary>
        public void ShowCharacterInfo()
        {
            var canvasUI = TryGetCanvasUI();
            var player = stateManager.CurrentPlayer;

            if (canvasUI == null || player == null)
            {
                stateManager.TransitionToState(GameState.CharacterInfo);
                return;
            }

            // Character info is displayed in the persistent layout, so we use a simple transition
            ScreenTransitionProtocol.TransitionToMenuScreen(
                stateManager,
                canvasUI,
                GameState.CharacterInfo,
                (ui) => 
                {
                    // Character info is displayed in the persistent layout panel
                    // No additional rendering needed beyond setting the character
                },
                character: player,
                clearEnemyContext: true,
                clearDungeonContext: true
            );
        }

        /// <summary>
        /// Show the weapon selection screen.
        /// Uses standardized ScreenTransitionProtocol for consistent behavior.
        /// Weapon selection should not show character info since character isn't fully initialized yet.
        /// </summary>
        public void ShowWeaponSelection(List<StartingWeapon> weapons)
        {
            var canvasUI = TryGetCanvasUI();

            if (canvasUI == null || weapons == null)
            {
                stateManager.TransitionToState(GameState.WeaponSelection);
                return;
            }

            ScreenTransitionProtocol.TransitionToMenuScreen(
                stateManager,
                canvasUI,
                GameState.WeaponSelection,
                (ui) => ui.RenderWeaponSelection(weapons),
                character: null, // Don't show character panel - character isn't fully initialized yet
                clearEnemyContext: true,
                clearDungeonContext: true
            );
        }

        /// <summary>
        /// Show the character creation screen.
        /// Uses standardized ScreenTransitionProtocol for consistent behavior.
        /// </summary>
        public void ShowCharacterCreation(Character character)
        {
            var canvasUI = TryGetCanvasUI();

            if (canvasUI == null || character == null)
            {
                stateManager.TransitionToState(GameState.CharacterCreation);
                return;
            }

            ScreenTransitionProtocol.TransitionToMenuScreen(
                stateManager,
                canvasUI,
                GameState.CharacterCreation,
                (ui) => ui.RenderCharacterCreation(character),
                character: character,
                clearEnemyContext: true,
                clearDungeonContext: true
            );
        }
    }
}


