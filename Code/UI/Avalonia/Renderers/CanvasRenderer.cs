using Avalonia.Media;
using Avalonia.Threading;
using RPGGame;
using RPGGame.Editors;
using RPGGame.UI.Avalonia.Managers;
using RPGGame.UI.Avalonia.Display;
using RPGGame.UI.Avalonia.Renderers.Layout;
using System;
using System.Collections.Generic;

namespace RPGGame.UI.Avalonia.Renderers
{
    /// <summary>
    /// Refactored centralized canvas renderer that coordinates all rendering operations
    /// Uses specialized renderers for different screen types and functionalities
    /// </summary>
    public class CanvasRenderer : ICanvasRenderer
    {
        private readonly GameCanvasControl canvas;
        private readonly ICanvasTextManager textManager;
        private readonly ICanvasInteractionManager interactionManager;
        private readonly ICanvasContextManager contextManager;
        private readonly LayoutCoordinator layoutCoordinator;
        
        // Core specialized renderers
        private readonly MenuRenderer menuRenderer;
        private readonly InventoryRenderer inventoryRenderer;
        private readonly CombatRenderer combatRenderer;
        private readonly DungeonRenderer dungeonRenderer;
        
        // New specialized renderers
        private readonly MessageDisplayRenderer messageRenderer;
        private readonly HelpSystemRenderer helpRenderer;
        private readonly CharacterCreationRenderer characterCreationRenderer;
        private readonly DungeonExplorationRenderer dungeonExplorationRenderer;

        public CanvasRenderer(GameCanvasControl canvas, ICanvasTextManager textManager, ICanvasInteractionManager interactionManager, ICanvasContextManager contextManager)
        {
            this.canvas = canvas;
            this.textManager = textManager;
            this.interactionManager = interactionManager;
            this.contextManager = contextManager;
            this.layoutCoordinator = new LayoutCoordinator(canvas, interactionManager);
            
            // Initialize core specialized renderers
            this.menuRenderer = new MenuRenderer(canvas, interactionManager.ClickableElements, textManager, interactionManager);
            this.inventoryRenderer = new InventoryRenderer(canvas, new Renderers.ColoredTextWriter(canvas), interactionManager.ClickableElements);
            this.combatRenderer = new CombatRenderer(canvas, new Renderers.ColoredTextWriter(canvas));
            this.dungeonRenderer = new DungeonRenderer(canvas, new Renderers.ColoredTextWriter(canvas), interactionManager.ClickableElements);
            
            // Initialize new specialized renderers
            this.messageRenderer = new MessageDisplayRenderer(canvas);
            this.helpRenderer = new HelpSystemRenderer(canvas);
            this.characterCreationRenderer = new CharacterCreationRenderer(canvas, textManager, interactionManager);
            this.dungeonExplorationRenderer = new DungeonExplorationRenderer(canvas, interactionManager);
        }

        public void RenderDisplayBuffer(CanvasContext context)
        {
            // Rendering is now handled automatically by the unified display system
            // This method is kept for backward compatibility but does nothing
            // The display manager handles all rendering automatically
        }

        public void RenderMainMenu(bool hasSavedGame, string? characterName, int characterLevel)
        {
            // Use persistent layout with no character (null) to show blank panels
            RenderWithLayout(null, "MAIN MENU", (contentX, contentY, contentWidth, contentHeight) =>
            {
                menuRenderer.RenderMainMenuContent(contentX, contentY, contentWidth, contentHeight, hasSavedGame, characterName, characterLevel);
            }, new CanvasContext());
        }

        public void RenderInventory(Character character, List<Item> inventory, CanvasContext context)
        {
            // Always clear canvas when rendering inventory to ensure clean transition from other screens
            RenderWithLayout(character, "INVENTORY", (contentX, contentY, contentWidth, contentHeight) =>
            {
                inventoryRenderer.RenderInventory(contentX, contentY, contentWidth, contentHeight, character, inventory);
            }, context, null, null, null, clearCanvas: true);
        }

        public void RenderItemSelectionPrompt(Character character, List<Item> inventory, string promptMessage, string actionType, CanvasContext context)
        {
            RenderWithLayout(character, "INVENTORY", (contentX, contentY, contentWidth, contentHeight) =>
            {
                inventoryRenderer.RenderItemSelectionPrompt(contentX, contentY, contentWidth, contentHeight, character, inventory, promptMessage, actionType);
            }, context);
        }

        public void RenderSlotSelectionPrompt(Character character, CanvasContext context)
        {
            RenderWithLayout(character, "INVENTORY", (contentX, contentY, contentWidth, contentHeight) =>
            {
                inventoryRenderer.RenderSlotSelectionPrompt(contentX, contentY, contentWidth, contentHeight, character);
            }, context);
        }

        public void RenderItemComparison(Character character, Item newItem, Item? currentItem, string slot, CanvasContext context)
        {
            RenderWithLayout(character, "INVENTORY", (contentX, contentY, contentWidth, contentHeight) =>
            {
                inventoryRenderer.RenderItemComparison(contentX, contentY, contentWidth, contentHeight, character, newItem, currentItem, slot);
            }, context);
        }
        
        public void RenderComboManagement(Character character, CanvasContext context)
        {
            RenderWithLayout(character, "COMBO MANAGEMENT", (contentX, contentY, contentWidth, contentHeight) =>
            {
                inventoryRenderer.RenderComboManagement(contentX, contentY, contentWidth, contentHeight, character);
            }, context);
        }
        
        public void RenderComboActionSelection(Character character, string actionType, CanvasContext context)
        {
            RenderWithLayout(character, "COMBO MANAGEMENT", (contentX, contentY, contentWidth, contentHeight) =>
            {
                inventoryRenderer.RenderComboActionSelection(contentX, contentY, contentWidth, contentHeight, character, actionType);
            }, context);
        }
        
        public void RenderComboReorderPrompt(Character character, string currentSequence, CanvasContext context)
        {
            RenderWithLayout(character, "COMBO MANAGEMENT", (contentX, contentY, contentWidth, contentHeight) =>
            {
                inventoryRenderer.RenderComboReorderPrompt(contentX, contentY, contentWidth, contentHeight, character, currentSequence);
            }, context);
        }

        public void RenderWeaponSelection(List<StartingWeapon> weapons, CanvasContext context)
        {
            RenderWithLayout(null, "WEAPON SELECTION", (contentX, contentY, contentWidth, contentHeight) =>
            {
                menuRenderer.RenderWeaponSelectionContent(contentX, contentY, contentWidth, contentHeight, weapons);
            }, context);
        }

        public void RenderCharacterCreation(Character character, CanvasContext context)
        {
            characterCreationRenderer.RenderCharacterCreation(character, context);
        }

        public void RenderSettings()
        {
            menuRenderer.RenderSettings();
        }

        public void RenderTestingMenu(string? subMenu = null) => RenderMenuScreen("COMPREHENSIVE GAME SYSTEM TESTS", 
            (x, y, w, h) => menuRenderer.RenderTestingMenu(x, y, w, h, subMenu));
        
        public void RenderDeveloperMenu() => RenderMenuScreen("DEVELOPER MENU", 
            (x, y, w, h) => menuRenderer.RenderDeveloperMenuContent(x, y, w, h));
        
        public void RenderBattleStatisticsMenu(BattleStatisticsRunner.StatisticsResult? results, bool isRunning) => 
            RenderMenuScreen("BATTLE STATISTICS", 
                (x, y, w, h) => menuRenderer.RenderBattleStatisticsMenuContent(x, y, w, h, results, isRunning));
        
        public void RenderBattleStatisticsResults(BattleStatisticsRunner.StatisticsResult results) => 
            RenderMenuScreen("BATTLE STATISTICS RESULTS", 
                (x, y, w, h) => menuRenderer.RenderBattleStatisticsResultsContent(x, y, w, h, results));

        public void RenderWeaponTestResults(List<BattleStatisticsRunner.WeaponTestResult> results) => 
            RenderMenuScreen("WEAPON TYPE TEST RESULTS", 
                (x, y, w, h) => menuRenderer.RenderWeaponTestResultsContent(x, y, w, h, results));

        public void RenderComprehensiveWeaponEnemyResults(BattleStatisticsRunner.ComprehensiveWeaponEnemyTestResult results) => 
            RenderMenuScreen("COMPREHENSIVE WEAPON-ENEMY TEST RESULTS", 
                (x, y, w, h) => menuRenderer.RenderComprehensiveWeaponEnemyResultsContent(x, y, w, h, results));
        
        public void RenderVariableEditor(EditableVariable? selectedVariable = null, bool isEditing = false, string? currentInput = null, string? message = null) => RenderMenuScreen("EDIT GAME VARIABLES", 
            (x, y, w, h) => menuRenderer.RenderVariableEditorContent(x, y, w, h, selectedVariable, isEditing, currentInput, message));
        
        public void RenderTuningParametersMenu(string? selectedCategory = null, EditableVariable? selectedVariable = null, bool isEditing = false, string? currentInput = null, string? message = null) => RenderMenuScreen("TUNING PARAMETERS", 
            (x, y, w, h) => menuRenderer.RenderTuningParametersContent(x, y, w, h, selectedCategory, selectedVariable, isEditing, currentInput, message));
        
        public void RenderActionEditor() => RenderMenuScreen("EDIT ACTIONS", 
            (x, y, w, h) => menuRenderer.RenderActionEditorContent(x, y, w, h));
        
        public void RenderActionList(List<ActionData> actions, int page) => RenderMenuScreen("ALL ACTIONS", 
            (x, y, w, h) => menuRenderer.RenderActionListContent(x, y, w, h, actions, page));
        
        public void RenderCreateActionForm(ActionData actionData, int currentStep, string[] formSteps, string? currentInput = null, bool isEditMode = false)
        {
            bool shouldClearCanvas = string.IsNullOrEmpty(currentInput);
            string title = isEditMode ? "EDIT ACTION" : "CREATE ACTION";
            RenderWithLayout(null, title, 
                (x, y, w, h) => menuRenderer.RenderCreateActionFormContent(x, y, w, h, actionData, currentStep, formSteps, currentInput, isEditMode),
                new CanvasContext(), null, null, null, shouldClearCanvas);
        }
        
        public void RenderActionDetails(ActionData action) => RenderMenuScreen("ACTION DETAILS", 
            (x, y, w, h) => menuRenderer.RenderActionDetailContent(x, y, w, h, action));

        public void RenderDeleteActionConfirmation(ActionData action, string? errorMessage = null) => RenderMenuScreen("DELETE ACTION CONFIRMATION", 
            (x, y, w, h) => menuRenderer.RenderDeleteActionConfirmationContent(x, y, w, h, action, errorMessage));

        public void RenderDungeonSelection(Character player, List<Dungeon> dungeons, CanvasContext context)
        {
            RenderWithLayout(player, "DUNGEON SELECTION", (contentX, contentY, contentWidth, contentHeight) =>
            {
                dungeonRenderer.RenderDungeonSelection(contentX, contentY, contentWidth, contentHeight, dungeons);
            }, context);
        }

        public void RenderDungeonStart(Dungeon dungeon, Character player, CanvasContext context)
        {
            // Use generic title - the actual "ENTERING DUNGEON" text comes from the display buffer content
            RenderWithLayout(player, "DUNGEON FIGHTERS", (contentX, contentY, contentWidth, contentHeight) =>
            {
                dungeonRenderer.RenderDungeonStart(contentX, contentY, contentWidth, contentHeight, dungeon, textManager, context.DungeonContext);
            }, context);
        }

        /// <summary>
        /// Renders the room entry screen.
        /// The display buffer should already be populated via ShowRoomEntry() or AddCurrentInfoToDisplayBuffer().
        /// Uses clearCanvas: false to preserve existing content and only update what's needed.
        /// The DisplayRenderer will clear the content area before rendering new content.
        /// </summary>
        public void RenderRoomEntry(Environment room, Character player, string? dungeonName, CanvasContext context, int? startFromBufferIndex = null)
        {
            // Use context values if parameters are null (context should be up-to-date after SetUIContext())
            string? displayDungeonName = dungeonName ?? context.DungeonName;
            string? displayRoomName = room?.Name ?? context.RoomName;
            
            // Use clearCanvas: false to prevent clearing the entire canvas
            // This avoids clearing content that was just rendered by the reactive system
            // The DisplayRenderer will handle clearing just the content area before rendering
            RenderWithLayout(
                player,
                "DUNGEON FIGHTERS",
                (contentX, contentY, contentWidth, contentHeight) =>
                {
                    // Render the display buffer content (dungeon header + room info)
                    // DisplayRenderer.Render() will clear the content area before rendering
                    if (textManager is CanvasTextManager canvasTextManager)
                    {
                        var displayManager = canvasTextManager.DisplayManager;
                        var buffer = displayManager.Buffer;
                        var renderer = new DisplayRenderer(new ColoredTextWriter(canvas));
                        renderer.Render(buffer, contentX, contentY, contentWidth, contentHeight, clearContent: true);
                    }
                },
                context,
                null, // no enemy yet
                displayDungeonName,
                displayRoomName,
                clearCanvas: false  // Don't clear full canvas - preserve layout, only clear content area
            );
        }

        public void RenderCombat(Character player, Enemy enemy, List<string> combatLog, CanvasContext context)
        {
            // Enable combat mode for proper timing and disable auto-rendering
            if (textManager is CanvasTextManager canvasTextManager)
            {
                canvasTextManager.DisplayManager.SetMode(new Display.CombatDisplayMode());
                
                // Set up callback to re-render combat screen when new messages are added
                System.Action renderCallback = () =>
                {
                    if (Dispatcher.UIThread.CheckAccess())
                    {
                        RenderCombatScreenOnly(player, enemy, context);
                    }
                    else
                    {
                        Dispatcher.UIThread.Post(() => RenderCombatScreenOnly(player, enemy, context));
                    }
                };
                
                canvasTextManager.DisplayManager.SetExternalRenderCallback(renderCallback);
            }
            
            // Render complete combat screen with all structured content + combat log
            RenderCombatScreenOnly(player, enemy, context);
        }
        
        /// <summary>
        /// Renders just the combat screen (called from RenderCombat and from callback)
        /// </summary>
        private void RenderCombatScreenOnly(Character player, Enemy enemy, CanvasContext context)
        {
            // Use enemy from context if available (it's always up-to-date), otherwise fall back to parameter
            Enemy currentEnemy = context.Enemy ?? enemy;
            
            // Determine if we should clear canvas - clear on first render to ensure clean transition
            // The LayoutCoordinator will detect title changes and clear automatically, but we need
            // to ensure clean transition from dungeon selection screen
            bool shouldClear = context.IsFirstCombatRender;
            
            // Canvas will be cleared automatically if title changes (handled by PersistentLayoutManager)
            // First render should clear to ensure clean transition from other screens
            RenderWithLayout(player, "COMBAT", (contentX, contentY, contentWidth, contentHeight) =>
            {
                // Use the unified combat screen renderer with dungeon context
                dungeonRenderer.RenderCombatScreen(contentX, contentY, contentWidth, contentHeight, 
                    null, null, currentEnemy, textManager, context.DungeonContext);
            }, context, currentEnemy, context.DungeonName, context.RoomName, clearCanvas: shouldClear);
            
            // Mark combat render as complete after first render
            if (shouldClear)
            {
                contextManager.MarkCombatRenderComplete();
            }
        }

        public void RenderEnemyEncounter(Enemy enemy, Character player, List<string> dungeonLog, string? dungeonName, string? roomName, CanvasContext context)
        {
            // Render enemy encounter using structured format, below dungeon and room info
            // Canvas will be cleared automatically if title changes (handled by PersistentLayoutManager)
            RenderWithLayout(player, "COMBAT", (contentX, contentY, contentWidth, contentHeight) =>
            {
                dungeonRenderer.RenderEnemyEncounter(contentX, contentY, contentWidth, contentHeight, enemy, textManager, context.DungeonContext);
            }, context, enemy, dungeonName, roomName, clearCanvas: false);
        }

        public void RenderCombatResult(bool playerSurvived, Character player, Enemy enemy, BattleNarrative? battleNarrative, string? dungeonName, string? roomName, CanvasContext context)
        {
            // Switch back to standard mode and re-enable auto-rendering when combat ends
            if (textManager is CanvasTextManager canvasTextManager)
            {
                canvasTextManager.DisplayManager.SetMode(new Display.StandardDisplayMode());
                canvasTextManager.DisplayManager.SetExternalRenderCallback(null);
            }
            
            // Canvas will be cleared automatically if title changes (handled by PersistentLayoutManager)
            RenderWithLayout(player, "COMBAT RESULT", (contentX, contentY, contentWidth, contentHeight) =>
            {
                combatRenderer.RenderCombatResult(contentX, contentY, contentWidth, contentHeight, playerSurvived, enemy, battleNarrative);
            }, context, enemy, dungeonName, roomName);
        }

        public void RenderRoomCompletion(Environment room, Character player, string? dungeonName, CanvasContext context)
        {
            RenderWithLayout(player, $"ROOM CLEARED: {room.Name.ToUpper()}", (contentX, contentY, contentWidth, contentHeight) =>
            {
                dungeonRenderer.RenderRoomCompletion(contentX, contentY, contentWidth, contentHeight, room, player);
            }, context);
        }

        public void RenderDungeonCompletion(Dungeon dungeon, Character player, int xpGained, Item? lootReceived, List<LevelUpInfo> levelUpInfos, CanvasContext context)
        {
            RenderWithLayout(player, $"DUNGEON COMPLETED: {dungeon.Name.ToUpper()}", (contentX, contentY, contentWidth, contentHeight) =>
            {
                dungeonRenderer.RenderDungeonCompletion(contentX, contentY, contentWidth, contentHeight, dungeon, player, xpGained, lootReceived, levelUpInfos ?? new List<LevelUpInfo>());
            }, context);
        }

        public void RenderDeathScreen(Character player, string defeatSummary, CanvasContext context)
        {
            // Clear canvas first to ensure clean death screen display
            canvas.Clear();
            
            // Switch back to standard mode and disable external render callback when showing death screen
            if (textManager is CanvasTextManager canvasTextManager)
            {
                canvasTextManager.DisplayManager.SetMode(new Display.StandardDisplayMode());
                canvasTextManager.DisplayManager.SetExternalRenderCallback(null);
                // Cancel any pending renders that might interfere
                canvasTextManager.DisplayManager.CancelPendingRenders();
            }
            
            RenderWithLayout(player, "YOU DIED", (contentX, contentY, contentWidth, contentHeight) =>
            {
                dungeonRenderer.RenderDeathScreen(contentX, contentY, contentWidth, contentHeight, player, defeatSummary);
            }, context);
            
            // Force immediate refresh to display the death screen
            canvas.Refresh();
        }

        public void RenderDungeonExploration(Character player, string currentLocation, List<string> availableActions, List<string> recentEvents, CanvasContext context)
        {
            dungeonExplorationRenderer.RenderDungeonExploration(player, currentLocation, availableActions, recentEvents, context);
        }

        public void RenderGameMenu(Character player, List<Item> inventory, CanvasContext context)
        {
            RenderWithLayout(player, $"WELCOME, {player.Name.ToUpper()}!", (contentX, contentY, contentWidth, contentHeight) =>
            {
                menuRenderer.RenderGameMenu(contentX, contentY, contentWidth, contentHeight);
            }, context);
        }

        // Message display methods - delegated to MessageDisplayRenderer
        public void ShowMessage(string message, Color color = default) => messageRenderer.ShowMessage(message, color);
        public void ShowError(string error) => messageRenderer.ShowError(error);
        public void ShowError(string error, string suggestion = "") => messageRenderer.ShowError(error, suggestion);
        public void ShowSuccess(string message) => messageRenderer.ShowSuccess(message);
        public void ShowLoadingAnimation(string message = "Loading...") => messageRenderer.ShowLoadingAnimation(message);
        public void UpdateStatus(string message) => messageRenderer.UpdateStatus(message);
        public void ShowInvalidKeyMessage(string message) => messageRenderer.ShowInvalidKeyMessage(message);

        // Help system methods - delegated to HelpSystemRenderer
        public void ToggleHelp()
        {
            bool showHelp = helpRenderer.ToggleHelp();
            if (showHelp)
            {
                helpRenderer.RenderHelp();
            }
            else
            {
                RenderMainMenu(false, null, 0);
            }
        }

        public void RenderHelp()
        {
            helpRenderer.RenderHelp();
        }

        public void Refresh()
        {
            canvas.Refresh();
        }

        #region Private Helper Methods

        private void RenderMenuScreen(string title, Action<int, int, int, int> renderContent)
        {
            RenderWithLayout(null, title, renderContent, new CanvasContext());
        }

        private void RenderWithLayout(Character? character, string title, Action<int, int, int, int> renderContent, CanvasContext context)
        {
            RenderWithLayout(character, title, renderContent, context, null, null, null);
        }

        private void RenderWithLayout(Character? character, string title, Action<int, int, int, int> renderContent, CanvasContext context, Enemy? enemy, string? dungeonName, string? roomName, bool clearCanvas = true)
        {
            layoutCoordinator.RenderWithLayout(character, title, renderContent, context, enemy, dungeonName, roomName, clearCanvas);
        }

        #endregion
    }
}
