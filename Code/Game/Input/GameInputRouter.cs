using System;
using System.Threading.Tasks;
using RPGGame;
using RPGGame.Utils;
using DungeonFighter.Game.Menu.Core;

namespace RPGGame.GameCore.Input
{
    /// <summary>
    /// Routes input to appropriate handlers based on game state
    /// Extracted from Game.cs HandleInput() method
    /// </summary>
    public class GameInputRouter
    {
        private readonly GameStateManager stateManager;
        private readonly GameInputHandlers handlers;
        private readonly Action<string> showMessage;
        private readonly Action<string> handleCombatScroll;

        public GameInputRouter(
            GameStateManager stateManager,
            GameInputHandlers handlers,
            Action<string> showMessage,
            Action<string> handleCombatScroll)
        {
            this.stateManager = stateManager ?? throw new ArgumentNullException(nameof(stateManager));
            this.handlers = handlers ?? throw new ArgumentNullException(nameof(handlers));
            this.showMessage = showMessage ?? throw new ArgumentNullException(nameof(showMessage));
            this.handleCombatScroll = handleCombatScroll ?? throw new ArgumentNullException(nameof(handleCombatScroll));
        }

        /// <summary>
        /// Routes input to the appropriate handler based on current game state
        /// </summary>
        public async Task RouteInput(string input)
        {
            if (string.IsNullOrEmpty(input)) return;

            // Debug: Log input and state for troubleshooting
            switch (stateManager.CurrentState)
            {
                case GameState.MainMenu:
                    if (handlers.MainMenuHandler != null)
                    {
                        await handlers.MainMenuHandler.HandleMenuInput(input);
                    }
                    else
                    {
                    }
                    break;
                case GameState.CharacterInfo:
                    handlers.CharacterMenuHandler?.HandleMenuInput(input);
                    break;
                case GameState.Settings:
                    if (handlers.SettingsMenuHandler != null)
                    {
                        handlers.SettingsMenuHandler.HandleMenuInput(input);
                    }
                    else
                    {
                    }
                    break;
                case GameState.DeveloperMenu:
                    if (handlers.DeveloperMenuHandler != null)
                    {
                        handlers.DeveloperMenuHandler.HandleMenuInput(input);
                    }
                    else
                    {
                    }
                    break;
                case GameState.BattleStatistics:
                    // Allow scrolling during battle statistics to view test results
                    if (input == "up" || input == "down")
                    {
                        handleCombatScroll(input);
                        return; // Don't process other input when scrolling
                    }
                    else if (handlers.BattleStatisticsHandler != null)
                    {
                        handlers.BattleStatisticsHandler.HandleMenuInput(input);
                    }
                    else
                    {
                    }
                    break;
                case GameState.VariableEditor:
                    if (handlers.VariableEditorHandler != null)
                    {
                        handlers.VariableEditorHandler.HandleMenuInput(input);
                    }
                    break;
                case GameState.TuningParameters:
                    if (handlers.TuningParametersHandler != null)
                    {
                        handlers.TuningParametersHandler.HandleMenuInput(input);
                    }
                    break;
                case GameState.ActionEditor:
                    if (handlers.ActionEditorHandler != null)
                    {
                        handlers.ActionEditorHandler.HandleMenuInput(input);
                    }
                    else
                    {
                    }
                    break;
                case GameState.CreateAction:
                    if (handlers.ActionEditorHandler != null)
                    {
                        handlers.ActionEditorHandler.HandleCreateActionInput(input);
                    }
                    else
                    {
                    }
                    break;
                case GameState.ViewAction:
                    if (handlers.ActionEditorHandler != null)
                    {
                        handlers.ActionEditorHandler.HandleActionDetailInput(input);
                    }
                    else
                    {
                    }
                    break;
                case GameState.Inventory:
                    handlers.InventoryMenuHandler?.HandleMenuInput(input);
                    break;
                case GameState.WeaponSelection:
                    handlers.WeaponSelectionHandler?.HandleMenuInput(input);
                    break;
                case GameState.GameLoop:
                    if (handlers.GameLoopInputHandler != null)
                        await handlers.GameLoopInputHandler.HandleMenuInput(input);
                    break;
                case GameState.DungeonSelection:
                    if (handlers.DungeonSelectionHandler != null)
                        await handlers.DungeonSelectionHandler.HandleMenuInput(input);
                    break;
                case GameState.DungeonCompletion:
                    if (handlers.DungeonCompletionHandler != null)
                        await handlers.DungeonCompletionHandler.HandleMenuInput(input);
                    break;
                case GameState.Death:
                    if (handlers.DeathScreenHandler != null)
                        await handlers.DeathScreenHandler.HandleMenuInput(input);
                    break;
                case GameState.Testing:
                    // Allow scrolling during testing to view test results
                    if (input == "up" || input == "down")
                    {
                        // Don't show message - it replaces the content we're trying to scroll
                        handleCombatScroll(input);
                        return; // Don't process other input when scrolling
                    }
                    else if (handlers.TestingSystemHandler != null)
                    {
                        await handlers.TestingSystemHandler.HandleMenuInput(input);
                    }
                    else
                    {
                    }
                    break;
                case GameState.CharacterCreation:
                    if (handlers.CharacterCreationHandler != null)
                    {
                        handlers.CharacterCreationHandler.HandleMenuInput(input);
                    }
                    else
                    {
                    }
                    break;
                case GameState.Dungeon:
                case GameState.Combat:
                    // Handle scrolling during combat
                    if (input == "up" || input == "down")
                    {
                        handleCombatScroll(input);
                    }
                    // Other input is handled internally by the managers
                    break;
                default:
                    showMessage($"Unknown state: {stateManager.CurrentState}");
                    break;
            }
        }
    }

    /// <summary>
    /// Container for all game input handlers
    /// </summary>
    public class GameInputHandlers
    {
        public MainMenuHandler? MainMenuHandler { get; set; }
        public CharacterMenuHandler? CharacterMenuHandler { get; set; }
        public SettingsMenuHandler? SettingsMenuHandler { get; set; }
        public DeveloperMenuHandler? DeveloperMenuHandler { get; set; }
        public ActionEditorHandler? ActionEditorHandler { get; set; }
        public BattleStatisticsHandler? BattleStatisticsHandler { get; set; }
        public TuningParametersHandler? TuningParametersHandler { get; set; }
        public VariableEditorHandler? VariableEditorHandler { get; set; }
        public InventoryMenuHandler? InventoryMenuHandler { get; set; }
        public WeaponSelectionHandler? WeaponSelectionHandler { get; set; }
        public CharacterCreationHandler? CharacterCreationHandler { get; set; }
        public GameLoopInputHandler? GameLoopInputHandler { get; set; }
        public DungeonSelectionHandler? DungeonSelectionHandler { get; set; }
        public DungeonCompletionHandler? DungeonCompletionHandler { get; set; }
        public DeathScreenHandler? DeathScreenHandler { get; set; }
        public TestingSystemHandler? TestingSystemHandler { get; set; }
    }
}

