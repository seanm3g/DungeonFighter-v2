using Avalonia.Media;
using RPGGame;
using RPGGame.UI;
using RPGGame.UI.Avalonia.Managers;
using RPGGame.UI.Avalonia.Renderers;
using RPGGame.UI.Avalonia.Coordinators;
using RPGGame.UI.Avalonia.Helpers;
using RPGGame.UI.ColorSystem;
using System;
using System.Collections.Generic;

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
        
        private System.Action? closeAction = null;

        public CanvasUICoordinator(GameCanvasControl canvas)
        {
            this.canvas = canvas;
            
            // Initialize specialized managers
            this.contextManager = new CanvasContextManager();
            this.layoutManager = new CanvasLayoutManager();
            this.interactionManager = new CanvasInteractionManager();
            this.textManager = new CanvasTextManager(canvas, new Renderers.ColoredTextWriter(canvas), contextManager);
            this.renderer = new CanvasRenderer(canvas, textManager, interactionManager, contextManager);
            
            // Initialize specialized coordinators (consolidated)
            this.messageWritingCoordinator = new MessageWritingCoordinator(textManager, renderer, contextManager);
            
            // Create animation manager with null parameters initially (will be set up properly)
            this.animationManager = new CanvasAnimationManager(canvas, null, null);
            this.screenRenderingCoordinator = new ScreenRenderingCoordinator(renderer, contextManager, animationManager, textManager);
            this.utilityCoordinator = new UtilityCoordinator(canvas, renderer, textManager, contextManager);
            this.coloredTextCoordinator = new ColoredTextCoordinator(textManager, messageWritingCoordinator);
            this.batchOperationCoordinator = new BatchOperationCoordinator(textManager, messageWritingCoordinator);
            
            // Set up animation manager with proper dependencies
            var dungeonRenderer = new DungeonRenderer(canvas, new Renderers.ColoredTextWriter(canvas), interactionManager.ClickableElements);
            Action<Character, List<Dungeon>> reRenderCallback = (player, dungeons) => 
            {
                if (player != null && dungeons != null)
                    screenRenderingCoordinator.RenderDungeonSelection(player, dungeons);
            };
            animationManager.SetupAnimationManager(dungeonRenderer, reRenderCallback);
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
            contextManager.SetCurrentCharacter(character);
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

        public void SetDungeonContext(List<string> context) => contextManager.SetDungeonContext(context);
        public void SetCurrentEnemy(Enemy enemy) => contextManager.SetCurrentEnemy(enemy);
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
        public void RenderCharacterCreation(Character character) 
            => screenRenderingCoordinator.RenderCharacterCreation(character);
        public void RenderSettings() => screenRenderingCoordinator.RenderSettings();
        public void RenderTestingMenu() => screenRenderingCoordinator.RenderTestingMenu();

        public void RenderDungeonSelection(Character player, List<Dungeon> dungeons)
        {
            RPGGame.Utils.ScrollDebugLogger.Log($"[RENDER] CanvasUICoordinator.RenderDungeonSelection called - player: {player != null}, dungeons: {dungeons?.Count ?? 0}, screenRenderingCoordinator: {screenRenderingCoordinator != null}");
            ValidationHelper.ValidatePlayer(player);
            ValidationHelper.ValidateDungeonsList(dungeons);
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
        public void RenderDungeonCompletion(Dungeon dungeon, Character player, int xpGained, Item? lootReceived) 
            => screenRenderingCoordinator.RenderDungeonCompletion(dungeon, player, xpGained, lootReceived);
        public void RenderDeathScreen(Character player, string defeatSummary) 
            => screenRenderingCoordinator.RenderDeathScreen(player, defeatSummary);
        public void RenderDungeonExploration(Character player, string currentLocation, List<string> availableActions, List<string> recentEvents) 
            => screenRenderingCoordinator.RenderDungeonExploration(player, currentLocation, availableActions, recentEvents);
        public void RenderGameMenu(Character player, List<Item> inventory) 
            => screenRenderingCoordinator.RenderGameMenu(player, inventory);

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
        public void ClearDisplayBuffer() => textManager.ClearDisplayBuffer();
        
        /// <summary>
        /// Clears the display buffer without triggering a render
        /// Used when switching to menu screens that handle their own rendering
        /// </summary>
        public void ClearDisplayBufferWithoutRender() => textManager.ClearDisplayBufferWithoutRender();
        
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
            if (textManager is Managers.CanvasTextManager canvasTextManager)
            {
                var messages = canvasTextManager.DisplayManager.Buffer.MessagesAsStrings;
                return string.Join(System.Environment.NewLine, messages);
            }
            return "";
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
