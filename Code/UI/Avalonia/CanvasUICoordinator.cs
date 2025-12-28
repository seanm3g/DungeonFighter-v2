using Avalonia.Media;
using Avalonia.Threading;
using RPGGame;
using RPGGame.Editors;
using RPGGame.UI;
using RPGGame.UI.Avalonia.Builders;
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
        private readonly UtilityCoordinator utilityCoordinator;
        private readonly ColoredTextCoordinator coloredTextCoordinator;
        private readonly BatchOperationCoordinator batchOperationCoordinator;
        private readonly Display.DisplayUpdateCoordinator displayUpdateCoordinator;
        private readonly Display.DisplayBufferManager displayBufferManager;
        private readonly Coordinators.CharacterSwitchHandler characterSwitchHandler;
        private readonly UIContextCoordinator contextCoordinator;
        
        private System.Action? closeAction = null;
        private MainWindow? mainWindow = null;
        private GameCoordinator? game = null;
        private GameStateManager? stateManager = null;
        
        // Screen state tracking to prevent unnecessary re-renders
        private GameState? lastRenderedScreenState = null;
        
        /// <summary>
        /// Gets the last rendered screen state.
        /// Used to prevent unnecessary re-renders when already showing the same screen.
        /// </summary>
        public GameState? LastRenderedScreenState => lastRenderedScreenState;
        
        /// <summary>
        /// Sets the last rendered screen state.
        /// Called by ScreenTransitionProtocol after rendering.
        /// </summary>
        internal void SetLastRenderedScreenState(GameState state)
        {
            lastRenderedScreenState = state;
        }

        public CanvasUICoordinator(GameCanvasControl canvas)
        {
            this.canvas = canvas;
            
            // Use builder to initialize all dependencies
            var builder = new CanvasUICoordinatorBuilder(canvas);
            var buildResult = builder.Build();
            
            // Assign built dependencies
            this.contextManager = buildResult.ContextManager;
            this.layoutManager = buildResult.LayoutManager;
            this.interactionManager = buildResult.InteractionManager;
            this.textManager = buildResult.TextManager;
            this.renderer = buildResult.Renderer;
            this.animationManager = buildResult.AnimationManager;
            this.messageWritingCoordinator = buildResult.MessageWritingCoordinator;
            this.utilityCoordinator = buildResult.UtilityCoordinator;
            this.coloredTextCoordinator = buildResult.ColoredTextCoordinator;
            this.batchOperationCoordinator = buildResult.BatchOperationCoordinator;
            this.displayUpdateCoordinator = buildResult.DisplayUpdateCoordinator;
            this.displayBufferManager = buildResult.DisplayBufferManager;
            
            // Set up DisplayBufferManager with this coordinator (circular reference resolved)
            displayBufferManager.SetCoordinator(this);
            
            // Create context coordinator for context management operations
            this.contextCoordinator = new UIContextCoordinator(contextManager, textManager, stateManager);
            
            // Create character switch handler
            this.characterSwitchHandler = new Coordinators.CharacterSwitchHandler(
                textManager,
                contextManager,
                null, // Will be set via SetStateManager
                (System.Action<Character?>)SetCharacter,
                (System.Action)ClearCurrentEnemy,
                (System.Action)ForceFullLayoutRender);
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
        /// Gets the game instance reference
        /// </summary>
        public GameCoordinator? GetGame()
        {
            return this.game;
        }
        
        /// <summary>
        /// Sets the game state manager for the animation system.
        /// This allows the animation manager to subscribe to state change events.
        /// Should be called after Game is initialized.
        /// </summary>
        public void SetStateManager(GameStateManager stateManager)
        {
            this.stateManager = stateManager;
            
            // Update context coordinator with state manager
            contextCoordinator.SetStateManager(stateManager);
            
            // Update state manager in display manager and render coordinator
            if (textManager is Managers.CanvasTextManager canvasTextManager)
            {
                // Set state manager on CanvasTextManager (which will update all per-character display managers)
                canvasTextManager.SetStateManager(stateManager);
            }
            
            if (animationManager is CanvasAnimationManager canvasAnimationManager)
            {
                var dungeonRenderer = new Renderers.DungeonRenderer(canvas, new Renderers.ColoredTextWriter(canvas), interactionManager.ClickableElements);
                System.Action<Character, List<Dungeon>> reRenderCallback = (player, dungeons) => 
                {
                    if (player != null && dungeons != null)
                        RenderDungeonSelection(player, dungeons);
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
            
            // Set state manager in DisplayBufferManager for automatic state-based management
            displayBufferManager.SetStateManager(stateManager);
            
            // Update character switch handler with state manager
            characterSwitchHandler.SetStateManager(stateManager);
            
            // Subscribe to character switch events for multi-character support
            stateManager.CharacterSwitched += characterSwitchHandler.OnCharacterSwitched;
        }
        
        /// <summary>
        /// Refreshes the character panel with the current active character
        /// </summary>
        public void RefreshCharacterPanel()
        {
            characterSwitchHandler.RefreshCharacterPanel();
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
            contextCoordinator.SetCharacter(character);
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
            contextCoordinator.SetDungeonContext(context);
        }
        
        /// <summary>
        /// Checks if a character is currently the active character
        /// </summary>
        public bool IsCharacterActive(Character? character)
        {
            return contextCoordinator.IsCharacterActive(character);
        }
        
        public void SetCurrentEnemy(Enemy enemy)
        {
            contextCoordinator.SetCurrentEnemy(enemy);
        }
        
        public void SetDungeonName(string? dungeonName)
        {
            contextCoordinator.SetDungeonName(dungeonName);
        }
        
        public void SetRoomName(string? roomName)
        {
            contextCoordinator.SetRoomName(roomName);
        }
        
        public void ClearCurrentEnemy()
        {
            contextCoordinator.ClearCurrentEnemy();
        }

        #endregion

        #region Screen Rendering - Delegated to CanvasRenderer

        public void RenderMainMenu() => renderer.RenderMainMenu(false, null, 0);
        public void RenderMainMenu(bool hasSavedGame, string? characterName, int characterLevel) 
            => renderer.RenderMainMenu(hasSavedGame, characterName, characterLevel);
        public void RenderInventory(Character character, List<Item> inventory) 
            => renderer.RenderInventory(character, inventory, contextManager.GetCurrentContext());
        public void RenderItemSelectionPrompt(Character character, List<Item> inventory, string promptMessage, string actionType) 
            => renderer.RenderItemSelectionPrompt(character, inventory, promptMessage, actionType, contextManager.GetCurrentContext());
        public void RenderSlotSelectionPrompt(Character character) 
            => renderer.RenderSlotSelectionPrompt(character, contextManager.GetCurrentContext());
        public void RenderRaritySelectionPrompt(Character character, List<System.Linq.IGrouping<string, Item>> rarityGroups) 
            => renderer.RenderRaritySelectionPrompt(character, rarityGroups, contextManager.GetCurrentContext());
        public void RenderTradeUpPreview(Character character, List<Item> itemsToTrade, Item resultingItem, string currentRarity, string nextRarity)
            => renderer.RenderTradeUpPreview(character, itemsToTrade, resultingItem, currentRarity, nextRarity, contextManager.GetCurrentContext());
        public void RenderItemComparison(Character character, Item newItem, Item? currentItem, string slot) 
            => renderer.RenderItemComparison(character, newItem, currentItem, slot, contextManager.GetCurrentContext());
        public void RenderComboManagement(Character character) 
            => renderer.RenderComboManagement(character, contextManager.GetCurrentContext());
        public void RenderComboActionSelection(Character character, string actionType) 
            => renderer.RenderComboActionSelection(character, actionType, contextManager.GetCurrentContext());
        public void RenderComboReorderPrompt(Character character, string currentSequence = "") 
            => renderer.RenderComboReorderPrompt(character, currentSequence, contextManager.GetCurrentContext());
        public void RenderCombat(Character player, Enemy enemy, List<string> combatLog) 
        {
            if (player != null && enemy != null && combatLog != null)
            {
                renderer.RenderCombat(player, enemy, combatLog, contextManager.GetCurrentContext());
            }
        }
        public void RenderWeaponSelection(List<StartingWeapon> weapons) 
            => renderer.RenderWeaponSelection(weapons, contextManager.GetCurrentContext());
        public void RenderCharacterSelection(List<Character> characters, string? activeCharacterName, Dictionary<string, string> characterStatuses)
            => renderer.RenderCharacterSelection(characters, activeCharacterName, characterStatuses, contextManager.GetCurrentContext());
        public void RenderCharacterCreation(Character character) 
            => renderer.RenderCharacterCreation(character, contextManager.GetCurrentContext());
        public void RenderSettings() => renderer.RenderSettings();
        public void RenderDeveloperMenu() => renderer.RenderDeveloperMenu();
        public void RenderBattleStatisticsMenu(BattleStatisticsRunner.StatisticsResult? results, bool isRunning) => 
            renderer.RenderBattleStatisticsMenu(results, isRunning);
        public void RenderBattleStatisticsResults(BattleStatisticsRunner.StatisticsResult results) => 
            renderer.RenderBattleStatisticsResults(results);

        public void RenderWeaponTestResults(List<BattleStatisticsRunner.WeaponTestResult> results) => 
            renderer.RenderWeaponTestResults(results);

        public void RenderComprehensiveWeaponEnemyResults(BattleStatisticsRunner.ComprehensiveWeaponEnemyTestResult results) => 
            renderer.RenderComprehensiveWeaponEnemyResults(results);
        public void UpdateBattleStatisticsProgress(int completed, int total, string status)
        {
            // Update progress display - for now just log it
            ScrollDebugLogger.Log($"Battle Statistics: {completed}/{total} - {status}");
        }
        public void RenderVariableEditor(EditableVariable? selectedVariable = null, bool isEditing = false, string? currentInput = null, string? message = null) => renderer.RenderVariableEditor(selectedVariable, isEditing, currentInput, message);
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
            renderer.RenderTuningParametersMenu(selectedCategory, selectedVariable, isEditing, currentInput, message);
        }
        
        public void HideTuningParametersMenu()
        {
            if (mainWindow != null)
            {
                mainWindow.HideTuningMenuPanel();
            }
        }
        public void RenderActionEditor() => renderer.RenderActionEditor();
        public void RenderActionList(List<ActionData> actions, int page) => renderer.RenderActionList(actions, page);
        public void RenderCreateActionForm(ActionData actionData, int currentStep, string[] formSteps, string? currentInput = null, bool isEditMode = false) => renderer.RenderCreateActionForm(actionData, currentStep, formSteps, currentInput, isEditMode);
        public void RenderActionDetails(ActionData action) => renderer.RenderActionDetails(action);
        public void RenderDeleteActionConfirmation(ActionData action, string? errorMessage = null) => renderer.RenderDeleteActionConfirmation(action, errorMessage);

        public void RenderDungeonSelection(Character player, List<Dungeon> dungeons)
        {
            RPGGame.Utils.InputValidator.ValidateNotNull(player, nameof(player));
            RPGGame.Utils.InputValidator.ValidateNotNull(dungeons, nameof(dungeons));
            
            // Note: When called from GameScreenCoordinator via ScreenTransitionProtocol,
            // the protocol already handles:
            // - Display buffer suppression and clearing
            // - Clickable elements clearing
            // - Canvas clearing
            // - State transition
            // This method just performs the actual rendering.
            
            // Handle animation for dungeon selection
            if (animationManager != null)
            {
                animationManager.StartDungeonSelectionAnimation(player!, dungeons!);
            }
            
            // After validation, player and dungeons are guaranteed to be non-null
            renderer.RenderDungeonSelection(player!, dungeons!, contextManager.GetCurrentContext());
        }

        public void StopDungeonSelectionAnimation() => animationManager?.StopDungeonSelectionAnimation();
        public void RenderDungeonStart(Dungeon dungeon, Character player) 
            => renderer.RenderDungeonStart(dungeon, player, contextManager.GetCurrentContext());
        public void RenderRoomEntry(Environment room, Character player, string? dungeonName = null, int? startFromBufferIndex = null) 
            => renderer.RenderRoomEntry(room, player, dungeonName, contextManager.GetCurrentContext(), startFromBufferIndex);
        public void RenderEnemyEncounter(Enemy enemy, Character player, List<string> dungeonLog, string? dungeonName = null, string? roomName = null) 
            => renderer.RenderEnemyEncounter(enemy, player, dungeonLog, dungeonName, roomName, contextManager.GetCurrentContext());
        public void RenderCombatResult(bool playerSurvived, Character player, Enemy enemy, BattleNarrative? battleNarrative = null, string? dungeonName = null, string? roomName = null) 
            => renderer.RenderCombatResult(playerSurvived, player, enemy, battleNarrative, dungeonName, roomName, contextManager.GetCurrentContext());
        public void RenderRoomCompletion(Environment room, Character player, string? dungeonName = null) 
            => renderer.RenderRoomCompletion(room, player, dungeonName, contextManager.GetCurrentContext());
        public void RenderDungeonCompletion(Dungeon dungeon, Character player, int xpGained, Item? lootReceived, List<LevelUpInfo> levelUpInfos, List<Item> itemsFoundDuringRun) 
            => renderer.RenderDungeonCompletion(dungeon, player, xpGained, lootReceived, levelUpInfos ?? new List<LevelUpInfo>(), itemsFoundDuringRun ?? new List<Item>(), contextManager.GetCurrentContext());
        public void RenderDeathScreen(Character player, string defeatSummary) 
            => renderer.RenderDeathScreen(player, defeatSummary, contextManager.GetCurrentContext());
        public void RenderDungeonExploration(Character player, string currentLocation, List<string> availableActions, List<string> recentEvents) 
            => renderer.RenderDungeonExploration(player, currentLocation, availableActions, recentEvents, contextManager.GetCurrentContext());
        public void RenderGameMenu(Character player, List<Item> inventory)
        {
            // Explicitly clear canvas before render to ensure clean state
            // This ensures the menu is always redrawn whenever it's cleared
            // Prevents the dynamic title from causing unwanted clears during render
            Clear();
            // Ensure character is set in context manager for persistent display
            contextManager.SetCurrentCharacter(player);
            renderer.RenderGameMenu(player, inventory, contextManager.GetCurrentContext());
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
        /// Gets the text manager (for advanced usage, e.g., per-character display managers)
        /// </summary>
        public ICanvasTextManager GetTextManager() => textManager;
        
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
        public void WriteColoredSegmentsBatch(List<(List<ColoredText> segments, UIMessageType messageType)> messageGroups, int delayAfterBatchMs = 0, Character? character = null) 
            => batchOperationCoordinator.WriteColoredSegmentsBatch(messageGroups, delayAfterBatchMs, character);
        public async System.Threading.Tasks.Task WriteColoredSegmentsBatchAsync(List<(List<ColoredText> segments, UIMessageType messageType)> messageGroups, int delayAfterBatchMs = 0, Character? character = null) 
            => await batchOperationCoordinator.WriteColoredSegmentsBatchAsync(messageGroups, delayAfterBatchMs, character);

        #endregion

        public void Dispose()
        {
            animationManager?.Dispose();
        }
    }
}
