using Avalonia.Media;
using Avalonia.Threading;
using RPGGame;
using RPGGame.Editors;
using RPGGame.UI;
using RPGGame.UI.Avalonia.Managers;
using RPGGame.UI.Avalonia.Renderers;
using RPGGame.UI.Avalonia.Coordinators;
using RPGGame.UI.Avalonia.Utils;
using RPGGame.UI.ColorSystem;
using RPGGame.Utils;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;

namespace RPGGame.UI.Avalonia
{
    /// <summary>
    /// Refactored coordinated UI manager that delegates to specialized coordinators
    /// Replaces the monolithic CanvasUIManager with a clean separation of concerns
    /// </summary>
    public class CanvasUICoordinator : IUIManager
    {
        private readonly GameCanvasControl canvas;
        private readonly CanvasRenderer renderer;
        private readonly ICanvasInteractionManager interactionManager;
        private readonly CanvasLayoutManager layoutManager;
        private readonly ICanvasTextManager textManager;
        private readonly ICanvasAnimationManager animationManager;
        private readonly ICanvasContextManager contextManager;
        
        // Specialized coordinators (consolidated from 6 to 3)
        private readonly MessageWritingCoordinator messageWritingCoordinator;
        private readonly ScreenRenderingCoordinator screenRenderingCoordinator;
        private readonly UtilityCoordinator utilityCoordinator;
        private readonly ColoredTextCoordinator coloredTextCoordinator;
        private readonly BatchOperationCoordinator batchOperationCoordinator;
        private readonly Display.DisplayUpdateCoordinator displayUpdateCoordinator;
        
        private System.Action? closeAction = null;
        private MainWindow? mainWindow = null;
        private GameCoordinator? game = null;
        private GameStateManager? stateManager = null;

        public CanvasUICoordinator(GameCanvasControl canvas)
        {
            this.canvas = canvas;
            
            // Initialize specialized managers
            this.contextManager = new CanvasContextManager();
            this.layoutManager = new CanvasLayoutManager();
            this.interactionManager = new CanvasInteractionManager();
            this.textManager = new CanvasTextManager(canvas, new Renderers.ColoredTextWriter(canvas), contextManager, stateManager: stateManager);
            this.renderer = new CanvasRenderer(canvas, textManager, interactionManager, contextManager);
            
            // Initialize specialized coordinators (consolidated)
            this.messageWritingCoordinator = new MessageWritingCoordinator(textManager, renderer, contextManager);
            
            // Create animation manager with null parameters initially (will be set up properly)
            this.animationManager = new CanvasAnimationManager(canvas, null, null);
            this.screenRenderingCoordinator = new ScreenRenderingCoordinator(renderer, contextManager, animationManager, textManager);
            this.utilityCoordinator = new UtilityCoordinator(canvas, renderer, textManager, contextManager);
            this.coloredTextCoordinator = new ColoredTextCoordinator(textManager, messageWritingCoordinator);
            this.batchOperationCoordinator = new BatchOperationCoordinator(textManager, messageWritingCoordinator);
            
            // Create DisplayUpdateCoordinator with display manager if available
            Display.CenterPanelDisplayManager? displayManager = null;
            Managers.CanvasTextManager? canvasTextManager = textManager as Managers.CanvasTextManager;
            if (canvasTextManager != null)
            {
                displayManager = canvasTextManager.DisplayManager;
            }
            this.displayUpdateCoordinator = new Display.DisplayUpdateCoordinator(canvas, textManager, displayManager);
            
            // Set up animation manager with proper dependencies
            var dungeonRenderer = new DungeonRenderer(canvas, new Renderers.ColoredTextWriter(canvas), interactionManager.ClickableElements);
            Action<Character, List<Dungeon>> reRenderCallback = (player, dungeons) => 
            {
                if (player != null && dungeons != null)
                    screenRenderingCoordinator.RenderDungeonSelection(player, dungeons);
            };
            animationManager.SetupAnimationManager(dungeonRenderer, reRenderCallback, null); // State manager will be set later via SetStateManager
            
            // Set up crit line re-render callback to trigger display buffer re-renders
            if (animationManager is CanvasAnimationManager canvasAnimationManager && canvasTextManager != null)
            {
                System.Action critLineReRenderCallback = () =>
                {
                    canvasTextManager.DisplayManager.ForceRender();
                };
                canvasAnimationManager.SetCritLineReRenderCallback(critLineReRenderCallback);
            }
        }
        
        /// <summary>
        /// Sets the main window reference for accessing UI controls
        /// </summary>
        public void SetMainWindow(MainWindow window)
        {
            this.mainWindow = window;
        }
        
        /// <summary>
        /// Gets the main window reference
        /// </summary>
        public MainWindow? GetMainWindow()
        {
            return this.mainWindow;
        }
        
        /// <summary>
        /// Gets the animation manager for configuration updates
        /// </summary>
        public ICanvasAnimationManager GetAnimationManager()
        {
            return this.animationManager;
        }

        /// <summary>
        /// Focuses the canvas to ensure keyboard input is captured
        /// </summary>
        public void FocusCanvas()
        {
            Dispatcher.UIThread.Post(() =>
            {
                try
                {
                    canvas.Focus();
                }
                catch (Exception)
                {
                    // Ignore focus errors
                }
            }, DispatcherPriority.Normal);
        }
        
        /// <summary>
        /// Sets the game instance reference for accessing handlers
        /// </summary>
        public void SetGame(GameCoordinator gameInstance)
        {
            this.game = gameInstance;
        }
        
        /// <summary>
        /// Sets the game state manager for the animation system.
        /// This allows the animation manager to subscribe to state change events.
        /// Should be called after Game is initialized.
        /// </summary>
        public void SetStateManager(GameStateManager stateManager)
        {
            this.stateManager = stateManager;
            
            // Update state manager in display manager and render coordinator
            if (textManager is Managers.CanvasTextManager canvasTextManager)
            {
                canvasTextManager.DisplayManager.SetStateManager(stateManager);
            }
            
            if (animationManager is CanvasAnimationManager canvasAnimationManager)
            {
                var dungeonRenderer = new Renderers.DungeonRenderer(canvas, new Renderers.ColoredTextWriter(canvas), interactionManager.ClickableElements);
                Action<Character, List<Dungeon>> reRenderCallback = (player, dungeons) => 
                {
                    if (player != null && dungeons != null)
                        screenRenderingCoordinator.RenderDungeonSelection(player, dungeons);
                };
                canvasAnimationManager.SetupAnimationManager(dungeonRenderer, reRenderCallback, stateManager);
                
                // Set up crit line re-render callback to trigger display buffer re-renders
                // Reuse canvasTextManager from outer scope if it exists
                if (textManager is Managers.CanvasTextManager textMgr)
                {
                    System.Action critLineReRenderCallback = () =>
                    {
                        textMgr.DisplayManager.ForceRender();
                    };
                    canvasAnimationManager.SetCritLineReRenderCallback(critLineReRenderCallback);
                }
            }
            
            // Subscribe to character switch events for multi-character support
            stateManager.CharacterSwitched += OnCharacterSwitched;
        }
        
        /// <summary>
        /// Handles character switch events - refreshes character panel and updates UI
        /// </summary>
        private void OnCharacterSwitched(object? sender, CharacterSwitchedEventArgs e)
        {
            Dispatcher.UIThread.Post(() =>
            {
                try
                {
                    // Clear any external render callbacks from previous character's combat
                    // This prevents background combat from interrupting the new character's view
                    if (textManager is CanvasTextManager canvasTextManager)
                    {
                        // Clear external render callback (prevents old combat callbacks from firing)
                        canvasTextManager.DisplayManager.SetExternalRenderCallback(null);
                        // Reset to standard display mode (prevents combat mode from persisting)
                        canvasTextManager.DisplayManager.SetMode(new Display.StandardDisplayMode());
                        // Clear display buffer to prevent old combat messages from showing for new character
                        // This ensures clean transition when switching characters
                        canvasTextManager.DisplayManager.Clear();
                    }
                    
                    // Clear enemy context to prevent old combat from showing
                    // This ensures the render system doesn't think combat is active for the new character
                    ClearCurrentEnemy();
                    
                    // Clear dungeon context to prevent old character's enemy info from showing
                    // This ensures the dungeon context doesn't contain enemy info from the previous character
                    contextManager.ClearDungeonContext();
                    
                    // Force a full layout render to ensure clean state
                    // This ensures the enemy is cleared from the right panel and everything is refreshed
                    ForceFullLayoutRender();
                    
                    RefreshCharacterPanel();
                }
                catch (Exception ex)
                {
                    // Log error but don't crash
                    System.Diagnostics.Debug.WriteLine($"Error refreshing character panel on switch: {ex.Message}");
                }
            }, DispatcherPriority.Normal);
        }
        
        /// <summary>
        /// Refreshes the character panel with the current active character
        /// </summary>
        public void RefreshCharacterPanel()
        {
            if (stateManager != null)
            {
                var activeCharacter = stateManager.GetActiveCharacter();
                if (activeCharacter != null)
                {
                    SetCharacter(activeCharacter);
                    // Force a re-render of the character panel if needed
                    // The character panel should auto-update via contextManager
                }
            }
        }

        #region IUIManager Implementation

        public void SetCloseAction(System.Action action)
        {
            closeAction = action;
        }

        public int CenterX => canvas.CenterX;

        public void Close()
        {
            closeAction?.Invoke();
        }

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
                    bool isMenuState = Display.DisplayStateCoordinator.IsMenuState(currentState);
                    
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

        // Message writing methods - delegated to MessageWritingCoordinator
        public void WriteLine(string message, UIMessageType messageType = UIMessageType.System) 
            => messageWritingCoordinator.WriteLine(message, messageType);
        public void Write(string message) => messageWritingCoordinator.Write(message);
        public void WriteSystemLine(string message) => messageWritingCoordinator.WriteSystemLine(message);
        public void WriteMenuLine(string message) => messageWritingCoordinator.WriteMenuLine(message);
        public void WriteTitleLine(string message) => messageWritingCoordinator.WriteTitleLine(message);
        public void WriteDungeonLine(string message) => messageWritingCoordinator.WriteDungeonLine(message);
        public void WriteRoomLine(string message) => messageWritingCoordinator.WriteRoomLine(message);
        public void WriteEnemyLine(string message) => messageWritingCoordinator.WriteEnemyLine(message);
        public void WriteRoomClearedLine(string message) => messageWritingCoordinator.WriteRoomClearedLine(message);
        public void WriteEffectLine(string message) => messageWritingCoordinator.WriteEffectLine(message);
        public void WriteBlankLine() => messageWritingCoordinator.WriteBlankLine();
        public void WriteChunked(string message, UI.ChunkedTextReveal.RevealConfig? config = null) 
            => messageWritingCoordinator.WriteChunked(message, config);
        public void ResetForNewBattle() => messageWritingCoordinator.ResetForNewBattle();
        public void ResetMenuDelayCounter() => messageWritingCoordinator.ResetMenuDelayCounter();
        public int GetConsecutiveMenuLineCount() => messageWritingCoordinator.GetConsecutiveMenuLineCount();
        public int GetBaseMenuDelay() => messageWritingCoordinator.GetBaseMenuDelay();

        #endregion

        #region Context Management

        public void SetDungeonContext(List<string> context)
        {
            // If we're in a menu state, don't set dungeon context that might contain enemy info
            // This prevents background combat from setting dungeon context with enemy info when in menus
            var currentState = stateManager?.CurrentState;
            bool isMenuState = Display.DisplayStateCoordinator.IsMenuState(currentState);
            
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
        /// Checks if a character is currently the active character
        /// </summary>
        public bool IsCharacterActive(Character? character)
        {
            return Display.DisplayStateCoordinator.IsCharacterActive(character, stateManager);
        }
        
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
            bool isMenuState = Display.DisplayStateCoordinator.IsMenuState(currentState);
            
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
        public void SetDungeonName(string? dungeonName) => contextManager.SetDungeonName(dungeonName);
        public void SetRoomName(string? roomName) => contextManager.SetRoomName(roomName);
        public void ClearCurrentEnemy() => contextManager.ClearCurrentEnemy();

        #endregion

        #region Screen Rendering - Delegated to ScreenRenderingCoordinator

        public void RenderMainMenu() => screenRenderingCoordinator.RenderMainMenu();
        public void RenderMainMenu(bool hasSavedGame, string? characterName, int characterLevel) 
            => screenRenderingCoordinator.RenderMainMenu(hasSavedGame, characterName, characterLevel);
        public void RenderInventory(Character character, List<Item> inventory) 
            => screenRenderingCoordinator.RenderInventory(character, inventory);
        public void RenderItemSelectionPrompt(Character character, List<Item> inventory, string promptMessage, string actionType) 
            => screenRenderingCoordinator.RenderItemSelectionPrompt(character, inventory, promptMessage, actionType);
        public void RenderSlotSelectionPrompt(Character character) 
            => screenRenderingCoordinator.RenderSlotSelectionPrompt(character);
        public void RenderRaritySelectionPrompt(Character character, List<System.Linq.IGrouping<string, Item>> rarityGroups) 
            => screenRenderingCoordinator.RenderRaritySelectionPrompt(character, rarityGroups);
        public void RenderItemComparison(Character character, Item newItem, Item? currentItem, string slot) 
            => screenRenderingCoordinator.RenderItemComparison(character, newItem, currentItem, slot);
        public void RenderComboManagement(Character character) 
            => screenRenderingCoordinator.RenderComboManagement(character);
        public void RenderComboActionSelection(Character character, string actionType) 
            => screenRenderingCoordinator.RenderComboActionSelection(character, actionType);
        public void RenderComboReorderPrompt(Character character, string currentSequence = "") 
            => screenRenderingCoordinator.RenderComboReorderPrompt(character, currentSequence);
        public void RenderCombat(Character player, Enemy enemy, List<string> combatLog) 
            => screenRenderingCoordinator.RenderCombat(player, enemy, combatLog);
        public void RenderWeaponSelection(List<StartingWeapon> weapons) 
            => screenRenderingCoordinator.RenderWeaponSelection(weapons);
        public void RenderCharacterSelection(List<Character> characters, string? activeCharacterName, Dictionary<string, string> characterStatuses)
            => screenRenderingCoordinator.RenderCharacterSelection(characters, activeCharacterName, characterStatuses);
        public void RenderCharacterCreation(Character character) 
            => screenRenderingCoordinator.RenderCharacterCreation(character);
        public void RenderSettings() => screenRenderingCoordinator.RenderSettings();
        public void RenderTestingMenu(string? subMenu = null) => screenRenderingCoordinator.RenderTestingMenu(subMenu);
        public void RenderDeveloperMenu() => screenRenderingCoordinator.RenderDeveloperMenu();
        public void RenderBattleStatisticsMenu(BattleStatisticsRunner.StatisticsResult? results, bool isRunning) => 
            screenRenderingCoordinator.RenderBattleStatisticsMenu(results, isRunning);
        public void RenderBattleStatisticsResults(BattleStatisticsRunner.StatisticsResult results) => 
            screenRenderingCoordinator.RenderBattleStatisticsResults(results);

        public void RenderWeaponTestResults(List<BattleStatisticsRunner.WeaponTestResult> results) => 
            screenRenderingCoordinator.RenderWeaponTestResults(results);

        public void RenderComprehensiveWeaponEnemyResults(BattleStatisticsRunner.ComprehensiveWeaponEnemyTestResult results) => 
            screenRenderingCoordinator.RenderComprehensiveWeaponEnemyResults(results);
        public void UpdateBattleStatisticsProgress(int completed, int total, string status)
        {
            // Update progress display - for now just log it
            ScrollDebugLogger.Log($"Battle Statistics: {completed}/{total} - {status}");
        }
        public void RenderVariableEditor(EditableVariable? selectedVariable = null, bool isEditing = false, string? currentInput = null, string? message = null) => screenRenderingCoordinator.RenderVariableEditor(selectedVariable, isEditing, currentInput, message);
        public void RenderTuningParametersMenu(string? selectedCategory = null, EditableVariable? selectedVariable = null, bool isEditing = false, string? currentInput = null, string? message = null)
        {
            // Show the interactive tuning panel instead of rendering on canvas
            ScrollDebugLogger.Log($"CanvasUICoordinator.RenderTuningParametersMenu: mainWindow={mainWindow != null}, game={game != null}");
            
            if (mainWindow != null && game != null)
            {
                // Get the variable editor from the handler
                var tuningHandler = game.GetTuningParametersHandler();
                ScrollDebugLogger.Log($"CanvasUICoordinator.RenderTuningParametersMenu: tuningHandler={tuningHandler != null}");
                
                if (tuningHandler != null)
                {
                    var variableEditor = tuningHandler.GetVariableEditor();
                    ScrollDebugLogger.Log($"CanvasUICoordinator.RenderTuningParametersMenu: variableEditor={variableEditor != null}, calling ShowTuningMenuPanel");
                    if (variableEditor != null)
                    {
                        mainWindow.ShowTuningMenuPanel(variableEditor);
                        return;
                    }
                }
            }
            
            ScrollDebugLogger.Log("CanvasUICoordinator.RenderTuningParametersMenu: Falling back to canvas rendering");
            // Fallback to canvas rendering if interactive panel not available
            screenRenderingCoordinator.RenderTuningParametersMenu(selectedCategory, selectedVariable, isEditing, currentInput, message);
        }
        
        public void HideTuningParametersMenu()
        {
            if (mainWindow != null)
            {
                mainWindow.HideTuningMenuPanel();
            }
        }
        public void RenderActionEditor() => screenRenderingCoordinator.RenderActionEditor();
        public void RenderActionList(List<ActionData> actions, int page) => screenRenderingCoordinator.RenderActionList(actions, page);
        public void RenderCreateActionForm(ActionData actionData, int currentStep, string[] formSteps, string? currentInput = null, bool isEditMode = false) => screenRenderingCoordinator.RenderCreateActionForm(actionData, currentStep, formSteps, currentInput, isEditMode);
        public void RenderActionDetails(ActionData action) => screenRenderingCoordinator.RenderActionDetails(action);
        public void RenderDeleteActionConfirmation(ActionData action, string? errorMessage = null) => screenRenderingCoordinator.RenderDeleteActionConfirmation(action, errorMessage);

        public void RenderDungeonSelection(Character player, List<Dungeon> dungeons)
        {
            RPGGame.Utils.InputValidator.ValidateNotNull(player, nameof(player));
            RPGGame.Utils.InputValidator.ValidateNotNull(dungeons, nameof(dungeons));
            
            // Clear clickable elements to remove game menu options
            ClearClickableElements();
            
            // Suppress display buffer rendering (matches pattern in ShowInventory)
            SuppressDisplayBufferRendering();
            ClearDisplayBufferWithoutRender();
            
            // Clear canvas BEFORE rendering to remove the game menu
            // This ensures the game menu is removed when transitioning to dungeon selection
            Clear();
            
            if (screenRenderingCoordinator != null)
            {
                // After validation, player and dungeons are guaranteed to be non-null
                screenRenderingCoordinator.RenderDungeonSelection(player!, dungeons!);
            }
        }

        public void StopDungeonSelectionAnimation() => screenRenderingCoordinator.StopDungeonSelectionAnimation();
        public void RenderDungeonStart(Dungeon dungeon, Character player) 
            => screenRenderingCoordinator.RenderDungeonStart(dungeon, player);
        public void RenderRoomEntry(Environment room, Character player, string? dungeonName = null, int? startFromBufferIndex = null) 
            => screenRenderingCoordinator.RenderRoomEntry(room, player, dungeonName, startFromBufferIndex);
        public void RenderEnemyEncounter(Enemy enemy, Character player, List<string> dungeonLog, string? dungeonName = null, string? roomName = null) 
            => screenRenderingCoordinator.RenderEnemyEncounter(enemy, player, dungeonLog, dungeonName, roomName);
        public void RenderCombatResult(bool playerSurvived, Character player, Enemy enemy, BattleNarrative? battleNarrative = null, string? dungeonName = null, string? roomName = null) 
            => screenRenderingCoordinator.RenderCombatResult(playerSurvived, player, enemy, battleNarrative, dungeonName, roomName);
        public void RenderRoomCompletion(Environment room, Character player, string? dungeonName = null) 
            => screenRenderingCoordinator.RenderRoomCompletion(room, player, dungeonName);
        public void RenderDungeonCompletion(Dungeon dungeon, Character player, int xpGained, Item? lootReceived, List<LevelUpInfo> levelUpInfos, List<Item> itemsFoundDuringRun) 
            => screenRenderingCoordinator.RenderDungeonCompletion(dungeon, player, xpGained, lootReceived, levelUpInfos, itemsFoundDuringRun);
        public void RenderDeathScreen(Character player, string defeatSummary) 
            => screenRenderingCoordinator.RenderDeathScreen(player, defeatSummary);
        public void RenderDungeonExploration(Character player, string currentLocation, List<string> availableActions, List<string> recentEvents) 
            => screenRenderingCoordinator.RenderDungeonExploration(player, currentLocation, availableActions, recentEvents);
        public void RenderGameMenu(Character player, List<Item> inventory)
        {
            // Explicitly clear canvas before render to ensure clean state
            // This ensures the menu is always redrawn whenever it's cleared
            // Prevents the dynamic title from causing unwanted clears during render
            Clear();
            screenRenderingCoordinator.RenderGameMenu(player, inventory);
        }

        #endregion

        #region Message Display - Delegated to MessageWritingCoordinator

        public void AddVictoryMessage(Enemy enemy, BattleNarrative? battleNarrative) 
            => messageWritingCoordinator.AddVictoryMessage(enemy, battleNarrative);
        public void AddDefeatMessage() => messageWritingCoordinator.AddDefeatMessage();
        public void AddRoomClearedMessage() => messageWritingCoordinator.AddRoomClearedMessage();

        #endregion

        #region Utility Methods - Delegated to UtilityCoordinator

        public void Clear() => utilityCoordinator.Clear();
        public void Refresh() => utilityCoordinator.Refresh();
        public void ClearDisplay() => utilityCoordinator.ClearDisplay();
        public void ClearDisplayBuffer() => displayUpdateCoordinator.Clear(Display.DisplayUpdateCoordinator.ClearOperation.Buffer);
        
        /// <summary>
        /// Clears the display buffer without triggering a render
        /// Used when switching to menu screens that handle their own rendering
        /// </summary>
        public void ClearDisplayBufferWithoutRender() => displayUpdateCoordinator.Clear(Display.DisplayUpdateCoordinator.ClearOperation.BufferWithoutRender);
        
        /// <summary>
        /// Starts a batch transaction for adding multiple messages
        /// Messages are added to the buffer but render is only triggered when transaction completes
        /// </summary>
        public Display.DisplayBatchTransaction StartBatch(bool autoRender = true)
        {
            if (textManager is Managers.CanvasTextManager canvasTextManager)
                return canvasTextManager.StartBatch(autoRender);
            return new Display.DisplayBatchTransaction(null, autoRender);
        }

        public int GetDisplayBufferCount() => textManager.BufferLineCount;
        public void RenderDisplayBuffer() => renderer.RenderDisplayBuffer(contextManager.GetCurrentContext());
        
        /// <summary>
        /// Gets the display buffer text as a single string
        /// </summary>
        public string GetDisplayBufferText()
        {
            DebugLoggingHelper.LogGetDisplayBufferTextEntry(textManager?.GetType().Name);
            
            if (textManager is Managers.CanvasTextManager canvasTextManager)
            {
                var messages = canvasTextManager.DisplayManager.Buffer.MessagesAsStrings;
                DebugLoggingHelper.LogGetDisplayBufferTextMessagesRetrieved(messages?.Count, messages?.FirstOrDefault()?.Length);
                
                if (messages == null || messages.Count == 0)
                {
                    return "";
                }
                
                var result = string.Join(System.Environment.NewLine, messages);
                DebugLoggingHelper.LogGetDisplayBufferTextResult(result?.Length);
                return result ?? "";
            }
            
            DebugLoggingHelper.LogGetDisplayBufferTextReturningEmpty();
            return "";
        }
        
        /// <summary>
        /// Gets the display buffer messages as colored text segments
        /// Returns a list of lines, where each line is a list of ColoredText segments
        /// </summary>
        public IReadOnlyList<IReadOnlyList<ColorSystem.ColoredText>> GetDisplayBufferColoredSegments()
        {
            if (textManager is Managers.CanvasTextManager canvasTextManager)
            {
                return canvasTextManager.DisplayManager.Buffer.Messages;
            }
            
            return new List<List<ColorSystem.ColoredText>>();
        }
        public void ForceRenderDisplayBuffer()
        {
            // Force immediate render of display buffer
            if (textManager is Managers.CanvasTextManager canvasTextManager)
            {
                canvasTextManager.DisplayManager.ForceRender();
            }
        }
        
        public void ForceFullLayoutRender()
        {
            // Force a full layout render by resetting render state
            if (textManager is Managers.CanvasTextManager canvasTextManager)
            {
                canvasTextManager.DisplayManager.ForceFullLayoutRender();
            }
        }
        
        /// <summary>
        /// Cancels any pending display buffer renders and prevents auto-rendering
        /// Used when showing menu screens that handle their own rendering
        /// </summary>
        public void SuppressDisplayBufferRendering()
        {
            if (textManager is Managers.CanvasTextManager canvasTextManager)
            {
                canvasTextManager.DisplayManager.CancelPendingRenders();
                // Set external render callback to do nothing - this prevents auto-rendering
                // When callback is set, TriggerRender() will call the callback instead of PerformRender()
                canvasTextManager.DisplayManager.SetExternalRenderCallback(() => { });
            }
        }
        
        /// <summary>
        /// Restores normal display buffer auto-rendering
        /// </summary>
        public void RestoreDisplayBufferRendering()
        {
            if (textManager is Managers.CanvasTextManager canvasTextManager)
            {
                // Clear external render callback to restore normal auto-rendering
                canvasTextManager.DisplayManager.SetExternalRenderCallback(null);
            }
        }
        
        public void ScrollUp(int lines = 3) => textManager.ScrollUp(lines);
        public void ScrollDown(int lines = 3) => textManager.ScrollDown(lines);
        public void ResetScroll() => textManager.ResetScroll();
        public void ShowMessage(string message, Color color = default) => utilityCoordinator.ShowMessage(message, color);
        public void ShowError(string error) => utilityCoordinator.ShowError(error);
        public void ShowSuccess(string message) => utilityCoordinator.ShowSuccess(message);
        public void ShowLoadingAnimation(string message = "Loading...") => utilityCoordinator.ShowLoadingAnimation(message);
        public void ShowError(string error, string suggestion = "") => utilityCoordinator.ShowError(error, suggestion);
        public void UpdateStatus(string message) => utilityCoordinator.UpdateStatus(message);
        public void ShowInvalidKeyMessage(string message) => utilityCoordinator.ShowInvalidKeyMessage(message);
        public void ToggleHelp() => utilityCoordinator.ToggleHelp();
        public void RenderHelp() => utilityCoordinator.RenderHelp();
        public void ShowPressKeyMessage() => utilityCoordinator.ShowPressKeyMessage();
        public void ResetDeleteConfirmation() => utilityCoordinator.ResetDeleteConfirmation();
        public void SetDeleteConfirmationPending(bool pending) => utilityCoordinator.SetDeleteConfirmationPending(pending);
        public void ClearTextInRange(int startY, int endY) => utilityCoordinator.ClearTextInRange(startY, endY);
        public void ClearTextInArea(int startX, int startY, int width, int height) 
            => utilityCoordinator.ClearTextInArea(startX, startY, width, height);

        #endregion

        #region Mouse Interaction - Direct implementation (merged from InteractionCoordinator)

        public ClickableElement? GetElementAt(int x, int y) => interactionManager.GetElementAt(x, y);
        public void SetHoverPosition(int x, int y)
        {
            if (interactionManager.SetHoverPosition(x, y))
                renderer.Refresh();
        }
        public void ClearClickableElements() => interactionManager.ClearClickableElements();

        #endregion

        #region Text Rendering - Delegated to MessageWritingCoordinator

        public void WriteLineColored(string message, int x, int y) 
            => messageWritingCoordinator.WriteLineColored(message, x, y);
        public int WriteLineColoredWrapped(string message, int x, int y, int maxWidth) 
            => messageWritingCoordinator.WriteLineColoredWrapped(message, x, y, maxWidth);
        public void WriteLineColoredSegments(List<ColoredText> segments, int x, int y) 
            => messageWritingCoordinator.WriteLineColoredSegments(segments, x, y);

        #endregion

        #region Colored Text System Methods (Primary API)

        public void WriteColoredText(ColoredText coloredText, UIMessageType messageType = UIMessageType.System) 
            => coloredTextCoordinator.WriteColoredText(coloredText, messageType);
        public void WriteLineColoredText(ColoredText coloredText, UIMessageType messageType = UIMessageType.System) 
            => coloredTextCoordinator.WriteLineColoredText(coloredText, messageType);
        public void WriteColoredSegments(List<ColoredText> segments, UIMessageType messageType = UIMessageType.System) 
            => coloredTextCoordinator.WriteColoredSegments(segments, messageType);
        public void WriteLineColoredSegments(List<ColoredText> segments, UIMessageType messageType = UIMessageType.System) 
            => coloredTextCoordinator.WriteLineColoredSegments(segments, messageType);
        public void WriteColoredTextBuilder(ColoredTextBuilder builder, UIMessageType messageType = UIMessageType.System) 
            => coloredTextCoordinator.WriteColoredTextBuilder(builder, messageType);
        public void WriteLineColoredTextBuilder(ColoredTextBuilder builder, UIMessageType messageType = UIMessageType.System) 
            => coloredTextCoordinator.WriteLineColoredTextBuilder(builder, messageType);
        public void WriteColoredSegmentsBatch(List<(List<ColoredText> segments, UIMessageType messageType)> messageGroups, int delayAfterBatchMs = 0) 
            => batchOperationCoordinator.WriteColoredSegmentsBatch(messageGroups, delayAfterBatchMs);
        public async System.Threading.Tasks.Task WriteColoredSegmentsBatchAsync(List<(List<ColoredText> segments, UIMessageType messageType)> messageGroups, int delayAfterBatchMs = 0) 
            => await batchOperationCoordinator.WriteColoredSegmentsBatchAsync(messageGroups, delayAfterBatchMs);

        #endregion

        public void Dispose()
        {
            animationManager?.Dispose();
        }
    }
}
