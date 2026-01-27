using Avalonia.Media;
using Avalonia.Threading;
using RPGGame;
using RPGGame.Editors;
using RPGGame.UI.Avalonia.Managers;
using RPGGame.UI.Avalonia.Display;
using RPGGame.UI.Avalonia.Renderers.Layout;
using RPGGame.UI.Avalonia.Renderers.Validators;
using RPGGame.UI.Avalonia.Renderers.Helpers;
using System;
using System.Collections.Generic;
using System.Linq;

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
        
        // Validators and helpers
        private readonly CombatRenderingValidator combatValidator;
        private readonly MenuScreenRenderingHelper menuScreenHelper;
        
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
            
            // Initialize validators and helpers
            this.combatValidator = new CombatRenderingValidator(contextManager);
            this.menuScreenHelper = new MenuScreenRenderingHelper(canvas, layoutCoordinator);
            
            // Initialize core specialized renderers
            this.menuRenderer = new MenuRenderer(canvas, interactionManager.ClickableElements, textManager, interactionManager);
            this.inventoryRenderer = new InventoryRenderer(canvas, new Renderers.ColoredTextWriter(canvas), interactionManager.ClickableElements);
            this.combatRenderer = new CombatRenderer(canvas, new Renderers.ColoredTextWriter(canvas));
            this.dungeonRenderer = new DungeonRenderer(canvas, new Renderers.ColoredTextWriter(canvas), interactionManager.ClickableElements);
            
            // Initialize new specialized renderers
            // MessageDisplayRenderer and HelpSystemRenderer need clearing actions - use canvas.Clear() directly
            // This is acceptable as these renderers are used for full-screen operations that need immediate clearing
            System.Action clearCanvasAction = () => canvas.Clear();
            this.messageRenderer = new MessageDisplayRenderer(canvas, clearCanvasAction);
            this.helpRenderer = new HelpSystemRenderer(canvas, clearCanvasAction);
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
            // Note: This method is called through ScreenTransitionProtocol which already clears the canvas
            // Pass clearCanvas: false to avoid double-clearing
            // Use persistent layout with no character (null) to show blank panels
            RenderWithLayout(null, "MAIN MENU", (contentX, contentY, contentWidth, contentHeight) =>
            {
                menuRenderer.RenderMainMenuContent(contentX, contentY, contentWidth, contentHeight, hasSavedGame, characterName, characterLevel);
            }, new CanvasContext(), null, null, null, clearCanvas: false);
            
            // Clear loading status message now that menu is fully rendered
            messageRenderer.ClearLoadingStatus();
            
            // Force immediate refresh to display the main menu
            canvas.Refresh();
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
            }, context, null, null, null);
        }

        public void RenderSlotSelectionPrompt(Character character, CanvasContext context)
        {
            RenderWithLayout(character, "INVENTORY", (contentX, contentY, contentWidth, contentHeight) =>
            {
                inventoryRenderer.RenderSlotSelectionPrompt(contentX, contentY, contentWidth, contentHeight, character);
            }, context, null, null, null);
        }
        
        public void RenderRaritySelectionPrompt(Character character, List<System.Linq.IGrouping<string, Item>> rarityGroups, CanvasContext context)
        {
            RenderWithLayout(character, "INVENTORY", (contentX, contentY, contentWidth, contentHeight) =>
            {
                inventoryRenderer.RenderRaritySelectionPrompt(contentX, contentY, contentWidth, contentHeight, character, rarityGroups);
            }, context, null, null, null);
        }
        
        public void RenderTradeUpPreview(Character character, List<Item> itemsToTrade, Item resultingItem, string currentRarity, string nextRarity, CanvasContext context)
        {
            RenderWithLayout(character, "INVENTORY", (contentX, contentY, contentWidth, contentHeight) =>
            {
                inventoryRenderer.RenderTradeUpPreview(contentX, contentY, contentWidth, contentHeight, character, itemsToTrade, resultingItem, currentRarity, nextRarity);
            }, context, null, null, null);
        }

        public void RenderItemComparison(Character character, Item newItem, Item? currentItem, string slot, CanvasContext context)
        {
            RenderWithLayout(character, "INVENTORY", (contentX, contentY, contentWidth, contentHeight) =>
            {
                inventoryRenderer.RenderItemComparison(contentX, contentY, contentWidth, contentHeight, character, newItem, currentItem, slot);
            }, context, null, null, null);
        }
        
        public void RenderComboManagement(Character character, CanvasContext context)
        {
            RenderWithLayout(character, "COMBO MANAGEMENT", (contentX, contentY, contentWidth, contentHeight) =>
            {
                inventoryRenderer.RenderComboManagement(contentX, contentY, contentWidth, contentHeight, character);
            }, context, null, null, null);
        }
        
        public void RenderComboActionSelection(Character character, string actionType, CanvasContext context)
        {
            RenderWithLayout(character, "COMBO MANAGEMENT", (contentX, contentY, contentWidth, contentHeight) =>
            {
                inventoryRenderer.RenderComboActionSelection(contentX, contentY, contentWidth, contentHeight, character, actionType);
            }, context, null, null, null);
        }
        
        public void RenderComboReorderPrompt(Character character, string currentSequence, CanvasContext context)
        {
            RenderWithLayout(character, "COMBO MANAGEMENT", (contentX, contentY, contentWidth, contentHeight) =>
            {
                inventoryRenderer.RenderComboReorderPrompt(contentX, contentY, contentWidth, contentHeight, character, currentSequence);
            }, context, null, null, null);
        }

        public void RenderWeaponSelection(List<StartingWeapon> weapons, CanvasContext context)
        {
            // Ensure weapons list is not null
            if (weapons == null)
            {
                weapons = new List<StartingWeapon>();
            }
            
            // Use clearCanvas: true to ensure clean transition, but render immediately to prevent flashing
            // The canvas clear happens synchronously with the render in LayoutCoordinator
            RenderWithLayout(null, "WEAPON SELECTION", (contentX, contentY, contentWidth, contentHeight) =>
            {
                menuRenderer.RenderWeaponSelectionContent(contentX, contentY, contentWidth, contentHeight, weapons ?? new List<StartingWeapon>());
            }, context, null, null, null, clearCanvas: true);
            
            // Clear loading status message now that weapon selection is fully rendered
            messageRenderer.ClearLoadingStatus();
            
            // Force immediate refresh to minimize visible black frame
            canvas.Refresh();
        }

        public void RenderCharacterSelection(List<Character> characters, string? activeCharacterName, Dictionary<string, string> characterStatuses, CanvasContext context)
        {
            RenderWithLayout(null, "CHARACTER SELECTION", (contentX, contentY, contentWidth, contentHeight) =>
            {
                menuRenderer.RenderCharacterSelectionContent(contentX, contentY, contentWidth, contentHeight, characters, activeCharacterName, characterStatuses);
            }, context, null, null, null);
        }

        public void RenderLoadCharacterSelection(List<(string characterId, string characterName, int level)> savedCharacters, CanvasContext context)
        {
            RenderWithLayout(null, "LOAD CHARACTER", (contentX, contentY, contentWidth, contentHeight) =>
            {
                menuRenderer.RenderLoadCharacterSelectionContent(contentX, contentY, contentWidth, contentHeight, savedCharacters);
            }, context, null, null, null);
        }

        public void RenderCharacterCreation(Character character, CanvasContext context)
        {
            characterCreationRenderer.RenderCharacterCreation(character, context);
        }

        public void RenderSettings()
        {
            menuRenderer.RenderSettings();
        }

        public void RenderDeveloperMenu() => menuScreenHelper.RenderMenuScreen("DEVELOPER MENU", 
            (x, y, w, h) => menuRenderer.RenderDeveloperMenuContent(x, y, w, h));
        
        public void RenderBattleStatisticsMenu(BattleStatisticsRunner.StatisticsResult? results, bool isRunning) => 
            menuScreenHelper.RenderMenuScreen("BATTLE STATISTICS", 
                (x, y, w, h) => menuRenderer.RenderBattleStatisticsMenuContent(x, y, w, h, results, isRunning));
        
        public void RenderBattleStatisticsResults(BattleStatisticsRunner.StatisticsResult results) => 
            menuScreenHelper.RenderMenuScreen("BATTLE STATISTICS RESULTS", 
                (x, y, w, h) => menuRenderer.RenderBattleStatisticsResultsContent(x, y, w, h, results));

        public void RenderWeaponTestResults(List<BattleStatisticsRunner.WeaponTestResult> results) => 
            menuScreenHelper.RenderMenuScreen("WEAPON TYPE TEST RESULTS", 
                (x, y, w, h) => menuRenderer.RenderWeaponTestResultsContent(x, y, w, h, results));

        public void RenderComprehensiveWeaponEnemyResults(BattleStatisticsRunner.ComprehensiveWeaponEnemyTestResult results) => 
            menuScreenHelper.RenderMenuScreen("COMPREHENSIVE WEAPON-ENEMY TEST RESULTS", 
                (x, y, w, h) => menuRenderer.RenderComprehensiveWeaponEnemyResultsContent(x, y, w, h, results));
        
        public void RenderVariableEditor(EditableVariable? selectedVariable = null, bool isEditing = false, string? currentInput = null, string? message = null) => menuScreenHelper.RenderMenuScreen("EDIT GAME VARIABLES", 
            (x, y, w, h) => menuRenderer.RenderVariableEditorContent(x, y, w, h, selectedVariable, isEditing, currentInput, message));
        
        public void RenderTuningParametersMenu(string? selectedCategory = null, EditableVariable? selectedVariable = null, bool isEditing = false, string? currentInput = null, string? message = null) => menuScreenHelper.RenderMenuScreen("TUNING PARAMETERS", 
            (x, y, w, h) => menuRenderer.RenderTuningParametersContent(x, y, w, h, selectedCategory, selectedVariable, isEditing, currentInput, message));
        
        public void RenderActionEditor() => menuScreenHelper.RenderMenuScreen("EDIT ACTIONS", 
            (x, y, w, h) => menuRenderer.RenderActionEditorContent(x, y, w, h));
        
        public void RenderActionList(List<ActionData> actions, int page) => menuScreenHelper.RenderMenuScreen("ALL ACTIONS", 
            (x, y, w, h) => menuRenderer.RenderActionListContent(x, y, w, h, actions, page));
        
        public void RenderCreateActionForm(ActionData actionData, int currentStep, string[] formSteps, string? currentInput = null, bool isEditMode = false)
        {
            bool shouldClearCanvas = string.IsNullOrEmpty(currentInput);
            string title = isEditMode ? "EDIT ACTION" : "CREATE ACTION";
            RenderWithLayout(null, title, 
                (x, y, w, h) => menuRenderer.RenderCreateActionFormContent(x, y, w, h, actionData, currentStep, formSteps, currentInput, isEditMode),
                new CanvasContext(), null, null, null, shouldClearCanvas);
        }
        
        public void RenderActionDetails(ActionData action) => menuScreenHelper.RenderMenuScreen("ACTION DETAILS", 
            (x, y, w, h) => menuRenderer.RenderActionDetailContent(x, y, w, h, action));

        public void RenderDeleteActionConfirmation(ActionData action, string? errorMessage = null) => menuScreenHelper.RenderMenuScreen("DELETE ACTION CONFIRMATION", 
            (x, y, w, h) => menuRenderer.RenderDeleteActionConfirmationContent(x, y, w, h, action, errorMessage));

        public void RenderDungeonSelection(Character player, List<Dungeon> dungeons, CanvasContext context)
        {
            // Canvas is already cleared in CanvasUICoordinator before this is called
            // Pass clearCanvas: false since we've already cleared - this prevents double-clear
            RenderWithLayout(player, "DUNGEON SELECTION", (contentX, contentY, contentWidth, contentHeight) =>
            {
                dungeonRenderer.RenderDungeonSelection(contentX, contentY, contentWidth, contentHeight, dungeons);
            }, context, null, null, null, clearCanvas: false);
            
            // Force immediate refresh to ensure the cleared canvas and new content are visible
            // This removes any visible game menu from the previous state
            canvas.Refresh();
        }

        public void RenderDungeonStart(Dungeon dungeon, Character player, CanvasContext context)
        {
            // Use generic title - the actual "ENTERING DUNGEON" text comes from the display buffer content
            RenderWithLayout(player, "DUNGEON FIGHTERS", (contentX, contentY, contentWidth, contentHeight) =>
            {
                dungeonRenderer.RenderDungeonStart(contentX, contentY, contentWidth, contentHeight, dungeon, textManager, context.DungeonContext);
            }, context, null, context.DungeonName, null);
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
            // Check if this character is currently active before rendering
            // This prevents background combat from interrupting other character views
            if (!combatValidator.ValidateCharacterActive(player))
            {
                // Character is not active - don't render combat
                return;
            }
            
            // Enable combat mode for proper timing and disable auto-rendering
            if (textManager is CanvasTextManager canvasTextManager)
            {
                canvasTextManager.DisplayManager.SetMode(new Display.CombatDisplayMode());
                
                // Set up callback to re-render combat screen when new messages are added
                // Only render if this character is still the active character
                System.Action renderCallback = () =>
                {
                    // Check if the player from this combat is still the active character
                    // This prevents rendering combat for inactive characters
                    if (!combatValidator.ValidateCharacterActive(player))
                    {
                        // Character is no longer active - don't render
                        return;
                    }
                    
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
            // CRITICAL: Always prefer the enemy parameter over context.Enemy
            // The parameter is the current enemy for this combat, while context might not be updated yet
            // This ensures the enemy info stays visible in the right panel from the moment it appears
            Enemy? currentEnemy = enemy ?? context.Enemy;
            
            // CRITICAL: Check if this character is still active before rendering
            // This prevents background combat from rendering when we've switched to another character
            if (!combatValidator.ValidateCharacterActive(player))
            {
                // Character is not active - don't render combat screen
                return;
            }
            
            // CRITICAL: Also check if dungeon context contains enemy info when we shouldn't be showing it
            // Filter out enemy info from dungeon context if it exists but enemy is null (menu state)
            // This prevents stale enemy info from being displayed
            List<string>? filteredDungeonContext = context.DungeonContext;
            if (currentEnemy == null && filteredDungeonContext != null && filteredDungeonContext.Count > 0)
            {
                // Enemy is null but dungeon context exists - this shouldn't happen in combat mode
                // Clear dungeon context to prevent stale enemy info from being displayed
                filteredDungeonContext = new List<string>();
            }
            
            // Determine if we should clear canvas - clear on first render to ensure clean transition
            // The LayoutCoordinator will detect title changes and clear automatically, but we need
            // to ensure clean transition from dungeon selection screen
            bool shouldClear = context.IsFirstCombatRender;
            
            // Canvas will be cleared automatically if title changes (handled by PersistentLayoutManager)
            // First render should clear to ensure clean transition from other screens
            RenderWithLayout(player, "COMBAT", (contentX, contentY, contentWidth, contentHeight) =>
            {
                // Use the unified combat screen renderer with filtered dungeon context
                // currentEnemy is guaranteed non-null here due to earlier checks
                if (currentEnemy != null)
                {
                    // CRITICAL: Pass the player character so RenderCombatScreen uses the correct display manager
                    // This ensures we render the combat player's buffer, not the active character's buffer
                    dungeonRenderer.RenderCombatScreen(contentX, contentY, contentWidth, contentHeight, 
                        null, null, currentEnemy, textManager, player, filteredDungeonContext);
                }
            }, context, currentEnemy, context.DungeonName, context.RoomName, clearCanvas: shouldClear);
            
            // Mark combat render as complete after first render
            if (shouldClear)
            {
                contextManager.MarkCombatRenderComplete();
            }
        }

        public void RenderEnemyEncounter(Enemy enemy, Character player, List<string> dungeonLog, string? dungeonName, string? roomName, CanvasContext context)
        {
            // Check if this character is currently active before rendering
            // This prevents background combat from interrupting other character views
            if (!combatValidator.ValidateCharacterActive(player))
            {
                // Character is not active - don't render enemy encounter
                return;
            }
            
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
            }, context, null, dungeonName, null);
        }

        public void RenderDungeonCompletion(Dungeon dungeon, Character player, int xpGained, Item? lootReceived, List<LevelUpInfo> levelUpInfos, List<Item> itemsFoundDuringRun, CanvasContext context)
        {
            RenderWithLayout(player, $"DUNGEON COMPLETED: {dungeon.Name.ToUpper()}", (contentX, contentY, contentWidth, contentHeight) =>
            {
                dungeonRenderer.RenderDungeonCompletion(contentX, contentY, contentWidth, contentHeight, dungeon, player, xpGained, lootReceived, levelUpInfos ?? new List<LevelUpInfo>(), itemsFoundDuringRun ?? new List<Item>(), context.DungeonContext);
            }, context, null, context.DungeonName, null);
        }

        public void RenderDeathScreen(Character player, string defeatSummary, CanvasContext context)
        {
            // Switch to menu display mode (Death is a menu state) and disable external render callback
            // This must happen BEFORE rendering to prevent any pending renders from interfering
            if (textManager is CanvasTextManager canvasTextManager)
            {
                // Cancel any pending renders FIRST to prevent them from clearing the canvas after we render
                canvasTextManager.DisplayManager.CancelPendingRenders();
                // Set to MenuDisplayMode since Death is a menu state - this prevents display buffer from rendering
                canvasTextManager.DisplayManager.SetMode(new Display.MenuDisplayMode());
                // Disable external render callback to prevent combat rendering from interfering
                canvasTextManager.DisplayManager.SetExternalRenderCallback(null);
            }
            
            // Note: This method is called through ScreenTransitionProtocol which already clears the canvas
            // Pass clearCanvas: false to avoid double-clearing
            
            RenderWithLayout(player, "YOU DIED", (contentX, contentY, contentWidth, contentHeight) =>
            {
                dungeonRenderer.RenderDeathScreen(contentX, contentY, contentWidth, contentHeight, player, defeatSummary);
            }, context, null, context.DungeonName, null, clearCanvas: false);
            
            // Force immediate refresh to display the death screen
            canvas.Refresh();
        }

        public void RenderDungeonExploration(Character player, string currentLocation, List<string> availableActions, List<string> recentEvents, CanvasContext context)
        {
            dungeonExplorationRenderer.RenderDungeonExploration(player, currentLocation, availableActions, recentEvents, context);
        }

        public void RenderGameMenu(Character player, List<Item> inventory, CanvasContext context)
        {
            // Canvas is already cleared in CanvasUICoordinator before this is called
            // Pass clearCanvas: false since we've already cleared - this prevents double-clear
            // and ensures the menu is properly redrawn after the clear
            RenderWithLayout(player, $"WELCOME, {player.Name.ToUpper()}!", (contentX, contentY, contentWidth, contentHeight) =>
            {
                menuRenderer.RenderGameMenu(contentX, contentY, contentWidth, contentHeight);
            }, context, null, null, null, clearCanvas: false);
            
            // Force immediate refresh to ensure the cleared canvas and new content are visible
            // This ensures the game menu is always redrawn after clearing
            canvas.Refresh();
        }

        // Message display methods - delegated to MessageDisplayRenderer
        public void ShowMessage(string message, Color color = default) => messageRenderer.ShowMessage(message, color);
        public void ShowError(string error) => messageRenderer.ShowError(error);
        public void ShowError(string error, string suggestion = "") => messageRenderer.ShowError(error, suggestion);
        public void ShowSuccess(string message) => messageRenderer.ShowSuccess(message);
        public void ShowLoadingAnimation(string message = "Loading...") => messageRenderer.ShowLoadingAnimation(message);
        public void UpdateStatus(string message) => messageRenderer.UpdateStatus(message);
        public void ShowInvalidKeyMessage(string message) => messageRenderer.ShowInvalidKeyMessage(message);
        public void ShowLoadingStatus(string message = "Loading data...") => messageRenderer.ShowLoadingStatus(message);
        public void ClearLoadingStatus() => messageRenderer.ClearLoadingStatus();

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

        private void RenderWithLayout(Character? character, string title, Action<int, int, int, int> renderContent, CanvasContext context, Enemy? enemy, string? dungeonName, string? roomName, bool clearCanvas = true)
        {
            layoutCoordinator.RenderWithLayout(character, title, renderContent, context, enemy, dungeonName, roomName, clearCanvas);
        }

        #endregion
    }
}
