using Avalonia.Media;
using RPGGame;
using RPGGame.UI;
using RPGGame.UI.Avalonia.Managers;
using RPGGame.UI.Avalonia.Renderers;
using RPGGame.UI.Avalonia.Coordinators;
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
            
            // Now set up the animation manager with proper dependencies
            var dungeonRenderer = new DungeonRenderer(canvas, new Renderers.ColoredTextWriter(canvas), interactionManager.ClickableElements);
            Action<Character, List<Dungeon>> reRenderCallback = (player, dungeons) => 
            {
                // Defensive null check to prevent crashes from race conditions
                if (player != null && dungeons != null)
                {
                    screenRenderingCoordinator.RenderDungeonSelection(player, dungeons);
                }
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
        {
            messageWritingCoordinator.WriteLine(message, messageType);
        }

        public void Write(string message)
        {
            messageWritingCoordinator.Write(message);
        }

        public void WriteSystemLine(string message)
        {
            messageWritingCoordinator.WriteSystemLine(message);
        }

        public void WriteMenuLine(string message)
        {
            messageWritingCoordinator.WriteMenuLine(message);
        }

        public void WriteTitleLine(string message)
        {
            messageWritingCoordinator.WriteTitleLine(message);
        }

        public void WriteDungeonLine(string message)
        {
            messageWritingCoordinator.WriteDungeonLine(message);
        }

        public void WriteRoomLine(string message)
        {
            messageWritingCoordinator.WriteRoomLine(message);
        }

        public void WriteEnemyLine(string message)
        {
            messageWritingCoordinator.WriteEnemyLine(message);
        }

        public void WriteRoomClearedLine(string message)
        {
            messageWritingCoordinator.WriteRoomClearedLine(message);
        }

        public void WriteEffectLine(string message)
        {
            messageWritingCoordinator.WriteEffectLine(message);
        }

        public void WriteBlankLine()
        {
            messageWritingCoordinator.WriteBlankLine();
        }

        public void WriteChunked(string message, UI.ChunkedTextReveal.RevealConfig? config = null)
        {
            messageWritingCoordinator.WriteChunked(message, config);
        }

        public void ResetForNewBattle()
        {
            messageWritingCoordinator.ResetForNewBattle();
        }

        public void ResetMenuDelayCounter()
        {
            messageWritingCoordinator.ResetMenuDelayCounter();
        }

        public int GetConsecutiveMenuLineCount()
        {
            return messageWritingCoordinator.GetConsecutiveMenuLineCount();
        }

        public int GetBaseMenuDelay()
        {
            return messageWritingCoordinator.GetBaseMenuDelay();
        }

        #endregion

        #region Context Management

        public void SetDungeonContext(List<string> context)
        {
            contextManager.SetDungeonContext(context);
        }

        public void SetCurrentEnemy(Enemy enemy)
        {
            contextManager.SetCurrentEnemy(enemy);
        }

        public void SetDungeonName(string? dungeonName)
        {
            contextManager.SetDungeonName(dungeonName);
        }

        public void SetRoomName(string? roomName)
        {
            contextManager.SetRoomName(roomName);
        }

        public void ClearCurrentEnemy()
        {
            contextManager.ClearCurrentEnemy();
        }

        #endregion

        #region Screen Rendering - Delegated to ScreenRenderingCoordinator

        public void RenderMainMenu()
        {
            screenRenderingCoordinator.RenderMainMenu();
        }

        public void RenderMainMenu(bool hasSavedGame, string? characterName, int characterLevel)
        {
            screenRenderingCoordinator.RenderMainMenu(hasSavedGame, characterName, characterLevel);
        }

        public void RenderInventory(Character character, List<Item> inventory)
        {
            screenRenderingCoordinator.RenderInventory(character, inventory);
        }

        public void RenderItemSelectionPrompt(Character character, List<Item> inventory, string promptMessage, string actionType)
        {
            screenRenderingCoordinator.RenderItemSelectionPrompt(character, inventory, promptMessage, actionType);
        }

        public void RenderSlotSelectionPrompt(Character character)
        {
            screenRenderingCoordinator.RenderSlotSelectionPrompt(character);
        }

        public void RenderCombat(Character player, Enemy enemy, List<string> combatLog)
        {
            screenRenderingCoordinator.RenderCombat(player, enemy, combatLog);
        }

        public void RenderWeaponSelection(List<StartingWeapon> weapons)
        {
            screenRenderingCoordinator.RenderWeaponSelection(weapons);
        }

        public void RenderCharacterCreation(Character character)
        {
            screenRenderingCoordinator.RenderCharacterCreation(character);
        }

        public void RenderSettings()
        {
            screenRenderingCoordinator.RenderSettings();
        }

        public void RenderTestingMenu()
        {
            screenRenderingCoordinator.RenderTestingMenu();
        }

        public void RenderDungeonSelection(Character player, List<Dungeon> dungeons)
        {
            // Validate inputs
            if (player == null)
            {
                throw new ArgumentNullException(nameof(player), "Player cannot be null");
            }
            
            if (dungeons == null)
            {
                throw new ArgumentNullException(nameof(dungeons), "Dungeons list cannot be null");
            }
            
            screenRenderingCoordinator.RenderDungeonSelection(player, dungeons);
        }

        public void StopDungeonSelectionAnimation()
        {
            screenRenderingCoordinator.StopDungeonSelectionAnimation();
        }

        public void RenderDungeonStart(Dungeon dungeon, Character player)
        {
            screenRenderingCoordinator.RenderDungeonStart(dungeon, player);
        }

        public void RenderRoomEntry(Environment room, Character player, string? dungeonName = null)
        {
            screenRenderingCoordinator.RenderRoomEntry(room, player, dungeonName);
        }

        public void RenderEnemyEncounter(Enemy enemy, Character player, List<string> dungeonLog, string? dungeonName = null, string? roomName = null)
        {
            screenRenderingCoordinator.RenderEnemyEncounter(enemy, player, dungeonLog, dungeonName, roomName);
        }

        public void RenderCombatResult(bool playerSurvived, Character player, Enemy enemy, BattleNarrative? battleNarrative = null, string? dungeonName = null, string? roomName = null)
        {
            screenRenderingCoordinator.RenderCombatResult(playerSurvived, player, enemy, battleNarrative, dungeonName, roomName);
        }

        public void RenderRoomCompletion(Environment room, Character player, string? dungeonName = null)
        {
            screenRenderingCoordinator.RenderRoomCompletion(room, player, dungeonName);
        }

        public void RenderDungeonCompletion(Dungeon dungeon, Character player, int xpGained, Item? lootReceived)
        {
            screenRenderingCoordinator.RenderDungeonCompletion(dungeon, player, xpGained, lootReceived);
        }

        public void RenderDungeonExploration(Character player, string currentLocation, List<string> availableActions, List<string> recentEvents)
        {
            screenRenderingCoordinator.RenderDungeonExploration(player, currentLocation, availableActions, recentEvents);
        }

        public void RenderGameMenu(Character player, List<Item> inventory)
        {
            screenRenderingCoordinator.RenderGameMenu(player, inventory);
        }

        #endregion

        #region Message Display - Delegated to MessageWritingCoordinator

        public void AddVictoryMessage(Enemy enemy, BattleNarrative? battleNarrative)
        {
            messageWritingCoordinator.AddVictoryMessage(enemy, battleNarrative);
        }

        public void AddDefeatMessage()
        {
            messageWritingCoordinator.AddDefeatMessage();
        }

        public void AddRoomClearedMessage()
        {
            messageWritingCoordinator.AddRoomClearedMessage();
        }

        #endregion

        #region Utility Methods - Delegated to UtilityCoordinator

        public void Clear()
        {
            utilityCoordinator.Clear();
        }

        public void Refresh()
        {
            utilityCoordinator.Refresh();
        }

        public void ClearDisplay()
        {
            utilityCoordinator.ClearDisplay();
        }

        public void ClearDisplayBuffer()
        {
            textManager.ClearDisplayBuffer();
        }

        public int GetDisplayBufferCount()
        {
            return textManager.BufferLineCount;
        }

        public void RenderDisplayBuffer()
        {
            renderer.RenderDisplayBuffer(contextManager.GetCurrentContext());
        }

        /// <summary>
        /// Scrolls the combat display buffer up (shows older content)
        /// </summary>
        /// <param name="lines">Number of lines to scroll up</param>
        public void ScrollUp(int lines = 3)
        {
            textManager.ScrollUp(lines);
        }

        /// <summary>
        /// Scrolls the combat display buffer down (shows newer content)
        /// </summary>
        /// <param name="lines">Number of lines to scroll down</param>
        public void ScrollDown(int lines = 3)
        {
            textManager.ScrollDown(lines);
        }

        /// <summary>
        /// Resets scrolling to auto-scroll mode (scrolls to bottom)
        /// </summary>
        public void ResetScroll()
        {
            textManager.ResetScroll();
        }

        public void ShowMessage(string message, Color color = default)
        {
            utilityCoordinator.ShowMessage(message, color);
        }

        public void ShowError(string error)
        {
            utilityCoordinator.ShowError(error);
        }

        public void ShowSuccess(string message)
        {
            utilityCoordinator.ShowSuccess(message);
        }

        public void ShowLoadingAnimation(string message = "Loading...")
        {
            utilityCoordinator.ShowLoadingAnimation(message);
        }

        public void ShowError(string error, string suggestion = "")
        {
            utilityCoordinator.ShowError(error, suggestion);
        }

        public void UpdateStatus(string message)
        {
            utilityCoordinator.UpdateStatus(message);
        }

        public void ToggleHelp()
        {
            utilityCoordinator.ToggleHelp();
        }

        public void RenderHelp()
        {
            utilityCoordinator.RenderHelp();
        }

        public void ShowPressKeyMessage()
        {
            utilityCoordinator.ShowPressKeyMessage();
        }

        public void ResetDeleteConfirmation()
        {
            utilityCoordinator.ResetDeleteConfirmation();
        }

        public void SetDeleteConfirmationPending(bool pending)
        {
            utilityCoordinator.SetDeleteConfirmationPending(pending);
        }

        #endregion

        #region Mouse Interaction - Direct implementation (merged from InteractionCoordinator)

        public ClickableElement? GetElementAt(int x, int y)
        {
            return interactionManager.GetElementAt(x, y);
        }

        public void SetHoverPosition(int x, int y)
        {
            bool hoverChanged = interactionManager.SetHoverPosition(x, y);
            if (hoverChanged)
            {
                renderer.Refresh();
            }
        }

        public void ClearClickableElements()
        {
            interactionManager.ClearClickableElements();
        }

        #endregion

        #region Text Rendering - Delegated to MessageWritingCoordinator

        public void WriteLineColored(string message, int x, int y)
        {
            messageWritingCoordinator.WriteLineColored(message, x, y);
        }

        public int WriteLineColoredWrapped(string message, int x, int y, int maxWidth)
        {
            return messageWritingCoordinator.WriteLineColoredWrapped(message, x, y, maxWidth);
        }
        
        public void WriteLineColoredSegments(List<ColoredText> segments, int x, int y)
        {
            messageWritingCoordinator.WriteLineColoredSegments(segments, x, y);
        }

        #endregion

        // ===== NEW COLORED TEXT SYSTEM METHODS =====
        
        public void WriteColoredText(ColoredText coloredText, UIMessageType messageType = UIMessageType.System)
        {
            // Convert ColoredText to string for now (backward compatibility)
            var plainText = ColoredTextRenderer.RenderAsPlainText(new List<ColoredText> { coloredText });
            messageWritingCoordinator.WriteLine(plainText, messageType);
        }
        
        public void WriteLineColoredText(ColoredText coloredText, UIMessageType messageType = UIMessageType.System)
        {
            WriteColoredText(coloredText, messageType);
        }
        
        public void WriteColoredSegments(List<ColoredText> segments, UIMessageType messageType = UIMessageType.System)
        {
            // Convert ColoredText segments to string for now (backward compatibility)
            var plainText = ColoredTextRenderer.RenderAsPlainText(segments);
            messageWritingCoordinator.WriteLine(plainText, messageType);
        }
        
        public void WriteLineColoredSegments(List<ColoredText> segments, UIMessageType messageType = UIMessageType.System)
        {
            WriteColoredSegments(segments, messageType);
        }

        public void Dispose()
        {
            animationManager?.Dispose();
        }
    }
}
