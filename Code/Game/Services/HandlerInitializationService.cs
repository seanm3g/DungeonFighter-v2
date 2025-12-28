using System;
using System.Threading.Tasks;
using RPGGame.Handlers;
using RPGGame.UI;
using RPGGame.GameCore.Input;
using DungeonFighter.Game.Menu.Routing;
using DungeonFighter.Game.Menu.Core;
using RPGGame.Menu;

namespace RPGGame.Game.Services
{
    /// <summary>
    /// Service for initializing game handlers and wiring up events
    /// </summary>
    public class HandlerInitializationService
    {
        /// <summary>
        /// Result containing all initialized handlers and routers
        /// </summary>
        public class InitializationResult
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
            public DungeonRunnerManager? DungeonRunnerManager { get; set; }
            public DungeonCompletionHandler? DungeonCompletionHandler { get; set; }
            public DeathScreenHandler? DeathScreenHandler { get; set; }
            public CharacterManagementHandler? CharacterManagementHandler { get; set; }
            public MenuInputRouter? MenuInputRouter { get; set; }
            public MenuInputValidator? MenuInputValidator { get; set; }
            public GameInputRouter? InputRouter { get; set; }
            public EscapeKeyHandler? EscapeKeyHandler { get; set; }
        }
        
        /// <summary>
        /// Initializes all handlers and wires up events
        /// </summary>
        public static InitializationResult Initialize(
            GameStateManager stateManager,
            GameInitializationManager initializationManager,
            GameInitializer gameInitializer,
            DungeonManagerWithRegistry dungeonManager,
            CombatManager combatManager,
            GameNarrativeManager narrativeManager,
            IUIManager? uiManager,
            System.Action showGameLoop,
            System.Action showMainMenu,
            System.Action showInventory,
            System.Action showCharacterInfo,
            System.Action<string> showMessage,
            System.Action exitGame,
            Func<Task> showDungeonSelection,
            System.Action<int, Item?, List<LevelUpInfo>, List<Item>> showDungeonCompletion,
            System.Action<Character> showDeathScreen,
            System.Action saveGame,
            System.Action showVariableEditor,
            System.Action showActionEditor,
            System.Action showTuningParameters,
            System.Action<string> handleCombatScroll)
        {
            var result = new InitializationResult();
            
            // Create handlers using HandlerInitializer
            var handlerResult = HandlerInitializer.CreateHandlers(
                stateManager, initializationManager, gameInitializer, dungeonManager, 
                combatManager, narrativeManager, uiManager);
            
            // Assign handlers from HandlerInitializer result
            result.MainMenuHandler = handlerResult.MainMenuHandler;
            result.CharacterMenuHandler = handlerResult.CharacterMenuHandler;
            result.SettingsMenuHandler = handlerResult.SettingsMenuHandler;
            result.InventoryMenuHandler = handlerResult.InventoryMenuHandler;
            result.WeaponSelectionHandler = handlerResult.WeaponSelectionHandler;
            result.CharacterCreationHandler = handlerResult.CharacterCreationHandler;
            result.GameLoopInputHandler = handlerResult.GameLoopInputHandler;
            result.DungeonSelectionHandler = handlerResult.DungeonSelectionHandler;
            result.DungeonRunnerManager = handlerResult.DungeonRunnerManager;
            result.DungeonCompletionHandler = handlerResult.DungeonCompletionHandler;
            result.DeathScreenHandler = handlerResult.DeathScreenHandler;
            result.CharacterManagementHandler = handlerResult.CharacterManagementHandler;
            
            // Create additional handlers that aren't in HandlerInitializer
            result.DeveloperMenuHandler = new DeveloperMenuHandler(stateManager, uiManager);
            result.ActionEditorHandler = new ActionEditorHandler(stateManager, uiManager);
            result.BattleStatisticsHandler = new BattleStatisticsHandler(stateManager, uiManager);
            result.TuningParametersHandler = new TuningParametersHandler(stateManager, uiManager);
            result.VariableEditorHandler = new VariableEditorHandler(stateManager, uiManager);
            
            // Wire up handler events using HandlerInitializer
            HandlerInitializer.WireHandlerEvents(
                handlerResult, stateManager, uiManager,
                showGameLoop, showMainMenu, showInventory, showCharacterInfo, showMessage, exitGame,
                showDungeonSelection,
                showDungeonCompletion, showDeathScreen, saveGame);
            
            // Wire up developer menu handler events
            if (result.DeveloperMenuHandler != null)
            {
                result.DeveloperMenuHandler.ShowSettingsEvent += () => result.SettingsMenuHandler?.ShowSettings();
                result.DeveloperMenuHandler.ShowVariableEditorEvent += () => showVariableEditor();
                result.DeveloperMenuHandler.ShowActionEditorEvent += () => showActionEditor();
                result.DeveloperMenuHandler.ShowBattleStatisticsEvent += () => result.BattleStatisticsHandler?.ShowBattleStatisticsMenu();
                result.DeveloperMenuHandler.ShowTuningParametersEvent += () => showTuningParameters();
            }
            
            // Wire up tuning parameters handler events
            if (result.TuningParametersHandler != null)
            {
                result.TuningParametersHandler.ShowDeveloperMenuEvent += () => result.DeveloperMenuHandler?.ShowDeveloperMenu();
            }
            
            // Wire up variable editor handler events
            if (result.VariableEditorHandler != null)
            {
                result.VariableEditorHandler.ShowDeveloperMenuEvent += () => result.DeveloperMenuHandler?.ShowDeveloperMenu();
            }
            
            // Wire up battle statistics handler events
            if (result.BattleStatisticsHandler != null)
            {
                result.BattleStatisticsHandler.ShowDeveloperMenuEvent += () => result.DeveloperMenuHandler?.ShowDeveloperMenu();
            }
            
            // Wire up action editor handler events
            if (result.ActionEditorHandler != null)
            {
                result.ActionEditorHandler.ShowDeveloperMenuEvent += () => result.DeveloperMenuHandler?.ShowDeveloperMenu();
            }
            
            // Initialize Menu Input Framework
            var menuInputResult = MenuInputFrameworkInitializer.Initialize();
            result.MenuInputRouter = menuInputResult.MenuInputRouter;
            result.MenuInputValidator = menuInputResult.MenuInputValidator;
            
            // Initialize input handlers
            InitializeInputHandlers(result, stateManager, showMessage, showGameLoop, showMainMenu, 
                showSettings: () => result.SettingsMenuHandler?.ShowSettings(),
                showDeveloperMenu: () => result.DeveloperMenuHandler?.ShowDeveloperMenu(),
                showActionEditor: () => result.ActionEditorHandler?.ShowActionEditor(),
                handleCombatScroll);
            
            return result;
        }
        
        /// <summary>
        /// Initializes input routing handlers
        /// </summary>
        private static void InitializeInputHandlers(
            InitializationResult result,
            GameStateManager stateManager,
            System.Action<string> showMessage,
            System.Action showGameLoop,
            System.Action showMainMenu,
            System.Action showSettings,
            System.Action showDeveloperMenu,
            System.Action showActionEditor,
            System.Action<string> handleCombatScroll)
        {
            // Create handler containers
            var inputHandlers = new GameInputHandlers
            {
                MainMenuHandler = result.MainMenuHandler,
                CharacterMenuHandler = result.CharacterMenuHandler,
                SettingsMenuHandler = result.SettingsMenuHandler,
                DeveloperMenuHandler = result.DeveloperMenuHandler,
                ActionEditorHandler = result.ActionEditorHandler,
                BattleStatisticsHandler = result.BattleStatisticsHandler,
                TuningParametersHandler = result.TuningParametersHandler,
                VariableEditorHandler = result.VariableEditorHandler,
                InventoryMenuHandler = result.InventoryMenuHandler,
                WeaponSelectionHandler = result.WeaponSelectionHandler,
                CharacterCreationHandler = result.CharacterCreationHandler,
                GameLoopInputHandler = result.GameLoopInputHandler,
                DungeonSelectionHandler = result.DungeonSelectionHandler,
                DungeonCompletionHandler = result.DungeonCompletionHandler,
                DeathScreenHandler = result.DeathScreenHandler,
                CharacterManagementHandler = result.CharacterManagementHandler,
                DungeonExitChoiceHandler = result.DungeonRunnerManager?.GetExitChoiceHandler()
            };
            
            var escapeKeyHandlers = new EscapeKeyHandlers
            {
                ActionEditorHandler = result.ActionEditorHandler
            };
            
            // Create input router
            result.InputRouter = new GameInputRouter(
                stateManager,
                inputHandlers,
                showMessage,
                handleCombatScroll);
            
            // Create escape key handler
            result.EscapeKeyHandler = new EscapeKeyHandler(
                stateManager,
                escapeKeyHandlers,
                showGameLoop,
                showMainMenu,
                showSettings,
                showDeveloperMenu,
                showActionEditor);
        }
    }
}

