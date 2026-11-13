using System;
using System.Threading.Tasks;
using RPGGame;
using RPGGame.Audio;
using RPGGame.UI.Avalonia.Feedback;
using DungeonFighter.Game.Menu.Core;
using Action = System.Action;

namespace RPGGame.GameCore.Input
{
    /// <summary>
    /// Handles escape key input and state transitions
    /// Extracted from Game.cs HandleEscapeKey() method
    /// </summary>
    public class EscapeKeyHandler
    {
        private readonly GameStateManager stateManager;
        private readonly EscapeKeyHandlers handlers;
        private readonly System.Action showGameLoop;
        private readonly System.Action showMainMenu;
        private readonly System.Action showSettings;
        private readonly System.Action showDeveloperMenu;
        private readonly System.Action showActionEditor;
        private readonly System.Action exitActionInteractionLab;

        public EscapeKeyHandler(
            GameStateManager stateManager,
            EscapeKeyHandlers handlers,
            System.Action showGameLoop,
            System.Action showMainMenu,
            System.Action showSettings,
            System.Action showDeveloperMenu,
            System.Action showActionEditor,
            System.Action exitActionInteractionLab)
        {
            this.stateManager = stateManager ?? throw new ArgumentNullException(nameof(stateManager));
            this.handlers = handlers ?? throw new ArgumentNullException(nameof(handlers));
            this.showGameLoop = showGameLoop ?? throw new ArgumentNullException(nameof(showGameLoop));
            this.showMainMenu = showMainMenu ?? throw new ArgumentNullException(nameof(showMainMenu));
            this.showSettings = showSettings ?? throw new ArgumentNullException(nameof(showSettings));
            this.showDeveloperMenu = showDeveloperMenu ?? throw new ArgumentNullException(nameof(showDeveloperMenu));
            this.showActionEditor = showActionEditor ?? throw new ArgumentNullException(nameof(showActionEditor));
            this.exitActionInteractionLab = exitActionInteractionLab ?? throw new ArgumentNullException(nameof(exitActionInteractionLab));
            // The run's Esc menu only lives over the dungeon / combat screens.
            stateManager.StateChanged += (_, e) =>
            {
                if (!IsRunState(e.NewState))
                    CombatEscapeMenu.SetOpen(false);
            };
        }

        /// <summary>Quits the app (Esc menu → Quit Game). Wired by <see cref="Services.HandlerInitializationService"/>.</summary>
        public System.Action? ExitGame { get; set; }

        private static bool IsRunState(GameState state) => state == GameState.Dungeon || state == GameState.Combat;

        /// <summary>
        /// While the run's Esc menu is open it takes every input: its buttons / 1-4 act, anything else is swallowed so
        /// nothing under it changes. Combat keeps running. False when the menu is closed.
        /// </summary>
        public bool TryHandleCombatEscapeMenuInput(string input)
        {
            if (!CombatEscapeMenu.IsOpen)
                return false;
            if (!IsRunState(stateManager.CurrentState))
            {
                CombatEscapeMenu.SetOpen(false);
                return false;
            }
            switch (CombatEscapeMenu.TokenForInput(input))
            {
                case CombatEscapeMenu.ResumeToken:
                    CombatEscapeMenu.SetOpen(false);
                    break;
                case CombatEscapeMenu.SettingsToken:
                    // Settings opens in its own window; the menu stays up behind it.
                    showSettings();
                    break;
                case CombatEscapeMenu.MainMenuToken:
                    CombatEscapeMenu.SetOpen(false);
                    stateManager.TransitionToState(GameState.MainMenu);
                    showMainMenu();
                    break;
                case CombatEscapeMenu.QuitToken:
                    CombatEscapeMenu.SetOpen(false);
                    ExitGame?.Invoke();
                    break;
            }
            return true;
        }

        /// <summary>
        /// Handles escape key input and transitions to appropriate state
        /// </summary>
        public Task HandleEscapeKey()
        {
            // Single "menu back" SFX fires for every Escape that ends up transitioning. Tradeup
            // and other early-returns below skip the SFX naturally because they happen before
            // any state change. No-op if the cue is unbound.
            AudioCues.Trigger(AudioCue.Menu_Back);
            switch (stateManager.CurrentState)
            {
                case GameState.Inventory:
                    // Hub: Esc opens (or closes) the Esc menu over whatever hub screen is up; it never goes back.
                    if (handlers.InventoryMenuHandler != null && handlers.InventoryMenuHandler.TryToggleEscapeMenu())
                        return Task.CompletedTask;
                    // Non-hub layout (MCP / headless): old back behaviour.
                    // Prevent ESC during trade-up flow - must complete trade-up once started
                    if (handlers.InventoryMenuHandler != null && handlers.InventoryMenuHandler.IsInTradeUpFlow())
                    {
                        return Task.CompletedTask;
                    }
                    // The Action Sequence editor closes back to the inventory.
                    if (handlers.InventoryMenuHandler != null && handlers.InventoryMenuHandler.TryCloseActionSequenceEditor())
                        return Task.CompletedTask;
                    stateManager.TransitionToState(GameState.GameLoop);
                    showGameLoop();
                    break;
                case GameState.SkillTree:
                    if (handlers.InventoryMenuHandler != null && handlers.InventoryMenuHandler.TryToggleEscapeMenu())
                        return Task.CompletedTask;
                    stateManager.TransitionToState(GameState.GameLoop);
                    showGameLoop();
                    break;
                case GameState.CharacterInfo:
                case GameState.Settings:
                    stateManager.TransitionToState(GameState.MainMenu);
                    showMainMenu();
                    break;
                case GameState.DungeonSelection:
                    if (handlers.DungeonSelectionHandler != null && handlers.DungeonSelectionHandler.CancelCustomLevelPromptIfActive())
                    {
                        return Task.CompletedTask;
                    }
                    if (handlers.InventoryMenuHandler != null && handlers.InventoryMenuHandler.TryToggleEscapeMenu())
                        return Task.CompletedTask;

                    stateManager.TransitionToState(GameState.GameLoop);
                    showGameLoop();
                    break;
                case GameState.RegionTravel:
                    // Esc closes the travel popup first, then returns to dungeon selection.
                    if (handlers.RegionTravelHandler != null)
                        return handlers.RegionTravelHandler.HandleMenuInput(RegionTravelHandler.CancelKey);
                    stateManager.TransitionToState(GameState.GameLoop);
                    showGameLoop();
                    break;
                case GameState.Dungeon:
                case GameState.Combat:
                    // Esc opens (or closes) the Esc menu over the run; combat keeps going underneath.
                    // MCP / headless has no overlay: old back-to-main-menu behaviour.
                    if (handlers.InventoryMenuHandler != null && handlers.InventoryMenuHandler.IsHubLayout)
                    {
                        CombatEscapeMenu.Toggle();
                        return Task.CompletedTask;
                    }
                    stateManager.TransitionToState(GameState.MainMenu);
                    showMainMenu();
                    break;
                case GameState.ActionInteractionLab:
                    exitActionInteractionLab();
                    break;
                case GameState.Testing:
                    stateManager.TransitionToState(GameState.Settings);
                    showSettings();
                    break;
                case GameState.BattleStatistics:
                    // Return to developer menu
                    stateManager.TransitionToState(GameState.DeveloperMenu);
                    showDeveloperMenu();
                    break;
                case GameState.DeveloperMenu:
                    stateManager.TransitionToState(GameState.Settings);
                    showSettings();
                    break;
                case GameState.VariableEditor:
                case GameState.TuningParameters:
                case GameState.ActionEditor:
                    stateManager.TransitionToState(GameState.DeveloperMenu);
                    showDeveloperMenu();
                    break;
                case GameState.CreateAction:
                    stateManager.TransitionToState(GameState.ActionEditor);
                    showActionEditor();
                    break;
                case GameState.ViewAction:
                    // Return to action list (which will be handled by ActionEditorHandler)
                    if (handlers.ActionEditorHandler != null)
                    {
                        handlers.ActionEditorHandler.HandleActionDetailInput("0");
                    }
                    break;
                case GameState.Stable:
                    // Esc first cancels a dungeon pick / confirm on the Stable, then leaves it.
                    if (handlers.StableHandler != null)
                        return handlers.StableHandler.HandleInput("escape");
                    stateManager.TransitionToState(GameState.MainMenu);
                    showMainMenu();
                    break;
                default:
                    stateManager.TransitionToState(GameState.MainMenu);
                    showMainMenu();
                    break;
            }

            return Task.CompletedTask;
        }
    }

    /// <summary>
    /// Container for handlers needed for escape key handling
    /// </summary>
    public class EscapeKeyHandlers
    {
        public ActionEditorHandler? ActionEditorHandler { get; set; }
        public InventoryMenuHandler? InventoryMenuHandler { get; set; }
        public DungeonSelectionHandler? DungeonSelectionHandler { get; set; }
        public StableHandler? StableHandler { get; set; }
        public RegionTravelHandler? RegionTravelHandler { get; set; }
    }
}

