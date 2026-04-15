using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Avalonia.Media;
using RPGGame;
using RPGGame.ActionInteractionLab;
using RPGGame.Editors;
using RPGGame.UI.Avalonia.ActionInteractionLab;
using RPGGame.UI.Avalonia.Display;
using RPGGame.UI.Avalonia.Layout;
using RPGGame.UI.Avalonia.Managers;
using RPGGame.UI.ColorSystem;
using RPGGame.Utils;

namespace RPGGame.UI.Avalonia
{
    /// <summary>
    /// Rendering-related public methods that forward to renderer/display.
    /// </summary>
    public partial class CanvasUICoordinator
    {
        #region Rendering and display buffer

        /// <summary>
        /// Clears the canvas.
        /// </summary>
        public void Clear()
        {
            utilityCoordinator.Clear();
        }

        /// <summary>
        /// Refreshes the canvas to display pending changes.
        /// </summary>
        public void Refresh()
        {
            utilityCoordinator.Refresh();
        }

        /// <summary>
        /// Clears the loading status message from the bottom left corner.
        /// </summary>
        public void ClearLoadingStatus()
        {
            utilityCoordinator.ClearLoadingStatus();
        }

        /// <summary>
        /// Clears all clickable elements from the interaction manager.
        /// </summary>
        public void ClearClickableElements()
        {
            interactionManager.ClearClickableElements();
        }

        /// <summary>
        /// Suppresses display buffer rendering (e.g. when showing a menu).
        /// Cancels any pending display buffer renders, clears the combat external render callback, and resets display mode
        /// so display <c>TriggerRender</c> routes through <c>PerformRender</c> (menu suppression) instead of
        /// replaying a stale combat layout over menu screens (e.g. character creation after weapon select).
        /// </summary>
        public void SuppressDisplayBufferRendering()
        {
            if (textManager is CanvasTextManager canvasTextManager)
            {
                canvasTextManager.SuppressExternalRenderPathOnAllDisplayManagers();
            }
        }

        /// <summary>
        /// Restores display buffer rendering after it was suppressed.
        /// </summary>
        public void RestoreDisplayBufferRendering()
        {
            if (textManager is CanvasTextManager canvasTextManager)
            {
                canvasTextManager.ForceRenderForActiveCharacter();
            }
        }

        /// <summary>
        /// Selects the per-character center display buffer for <paramref name="character"/> before clear/render.
        /// Required when narrative uses <see cref="CanvasTextManager.GetDisplayManagerForCharacter"/> but UI thread still holds another manager as current.
        /// </summary>
        public void SwitchDisplayBufferToCharacter(Character? character)
        {
            if (textManager is CanvasTextManager ctm)
                ctm.SwitchToCharacterDisplayManager(character);
        }

        /// <summary>
        /// Clears the display buffer without triggering a render.
        /// </summary>
        public void ClearDisplayBufferWithoutRender()
        {
            textManager.ClearDisplayBufferWithoutRender();
        }

        /// <summary>
        /// Clears the display buffer and triggers a render.
        /// </summary>
        public void ClearDisplayBuffer()
        {
            textManager.ClearDisplayBuffer();
        }

        /// <summary>
        /// Forces a full layout render (resets render state and performs render).
        /// </summary>
        public void ForceFullLayoutRender()
        {
            if (textManager is CanvasTextManager canvasTextManager)
            {
                canvasTextManager.ForceFullLayoutRenderForActiveCharacter();
            }
        }

        /// <summary>
        /// Forces an immediate render without resetting layout state. Prefer after small data updates (e.g. combo reorder).
        /// </summary>
        public void ForceRender()
        {
            if (textManager is CanvasTextManager canvasTextManager)
            {
                canvasTextManager.ForceRenderForActiveCharacter();
            }
        }

        /// <summary>
        /// True when the center panel is in combat log mode (combat UI active). Independent of canvas enemy context, which can be cleared during renders.
        /// </summary>
        public bool IsCombatDisplayActive()
        {
            if (textManager is CanvasTextManager canvasTextManager)
                return canvasTextManager.DisplayManager.IsCombatDisplayMode;
            return false;
        }

        /// <summary>
        /// Clears text elements within a specific rectangular area.
        /// </summary>
        public void ClearTextInArea(int startX, int startY, int width, int height)
        {
            utilityCoordinator.ClearTextInArea(startX, startY, width, height);
        }

        /// <summary>
        /// Gets the text manager for direct buffer/display access.
        /// </summary>
        public ICanvasTextManager? GetTextManager()
        {
            return textManager;
        }

        /// <summary>
        /// Starts a display batch transaction.
        /// </summary>
        public DisplayBatchTransaction StartBatch(bool autoRender = true)
        {
            if (textManager is CanvasTextManager ctm)
                return ctm.StartBatch(autoRender);
            throw new System.InvalidOperationException("StartBatch requires CanvasTextManager.");
        }

        /// <summary>
        /// Gets the number of lines in the display buffer.
        /// </summary>
        public int GetDisplayBufferCount()
        {
            return textManager.BufferLineCount;
        }

        /// <summary>
        /// Renders the display buffer to the specified area.
        /// </summary>
        public void RenderDisplayBuffer(int x, int y, int width, int height)
        {
            textManager.RenderDisplayBuffer(x, y, width, height);
        }

        /// <summary>
        /// Renders the display buffer to the center panel area.
        /// </summary>
        public void RenderDisplayBuffer()
        {
            textManager.RenderDisplayBuffer(
                LayoutConstants.CENTER_PANEL_X + 1,
                LayoutConstants.CENTER_PANEL_Y + 1,
                LayoutConstants.CENTER_PANEL_WIDTH - 2,
                LayoutConstants.CENTER_PANEL_HEIGHT - 2);
        }

        /// <summary>
        /// Forces an immediate render of the display buffer.
        /// </summary>
        public void ForceRenderDisplayBuffer()
        {
            if (textManager is CanvasTextManager ctm)
                ctm.ForceRenderForActiveCharacter();
        }

        /// <summary>
        /// Adds room cleared message to the display buffer.
        /// </summary>
        public void AddRoomClearedMessage()
        {
            messageWritingCoordinator.AddRoomClearedMessage();
        }

        /// <summary>
        /// Scrolls the display up.
        /// </summary>
        public void ScrollUp(int lines = 3)
        {
            textManager.ScrollUp(lines);
        }

        /// <summary>
        /// Scrolls the display down.
        /// </summary>
        public void ScrollDown(int lines = 3)
        {
            textManager.ScrollDown(lines);
        }

        /// <summary>
        /// Shows a message on the canvas.
        /// </summary>
        public void ShowMessage(string message, Color color = default)
        {
            utilityCoordinator.ShowMessage(message, color);
        }

        /// <summary>
        /// Shows an error message.
        /// </summary>
        public void ShowError(string error)
        {
            utilityCoordinator.ShowError(error);
        }

        /// <summary>
        /// Shows an error message with suggestion.
        /// </summary>
        public void ShowError(string error, string suggestion = "")
        {
            utilityCoordinator.ShowError(error, suggestion);
        }

        /// <summary>
        /// Shows loading status in the bottom left corner.
        /// </summary>
        public void ShowLoadingStatus(string message = "Loading data...")
        {
            utilityCoordinator.ShowLoadingStatus(message);
        }

        /// <summary>
        /// Shows loading animation.
        /// </summary>
        public void ShowLoadingAnimation(string message = "Loading...")
        {
            utilityCoordinator.ShowLoadingAnimation(message);
        }

        /// <summary>
        /// Shows invalid key message at the bottom.
        /// </summary>
        public void ShowInvalidKeyMessage(string message)
        {
            utilityCoordinator.ShowInvalidKeyMessage(message);
        }

        /// <summary>
        /// Shows press key message at the bottom (e.g. "Press any key to continue").
        /// </summary>
        public void ShowPressKeyMessage()
        {
            utilityCoordinator.ShowPressKeyMessage();
        }

        /// <summary>
        /// Updates status message (e.g. in loading/settings).
        /// </summary>
        public void UpdateStatus(string message)
        {
            utilityCoordinator.UpdateStatus(message);
        }

        /// <summary>
        /// Toggles the help display.
        /// </summary>
        public void ToggleHelp()
        {
            utilityCoordinator.ToggleHelp();
        }

        /// <summary>
        /// Resets scroll to auto-scroll mode.
        /// </summary>
        public void ResetScroll()
        {
            textManager.ResetScroll();
        }

        /// <summary>
        /// Gets the display buffer content as a single string.
        /// </summary>
        public string GetDisplayBufferText()
        {
            return string.Join(System.Environment.NewLine, textManager.DisplayBuffer);
        }

        /// <summary>
        /// Gets the clickable element at the specified coordinates.
        /// </summary>
        public ClickableElement? GetElementAt(int x, int y)
        {
            return interactionManager.GetElementAt(x, y);
        }

        /// <summary>
        /// Sets the current hover position for highlight updates.
        /// </summary>
        public bool SetHoverPosition(int x, int y)
        {
            return interactionManager.SetHoverPosition(x, y);
        }

        /// <summary>
        /// Clears all clickable hover highlights (e.g. before repointing after leaving strip/side panels).
        /// </summary>
        public void ClearHoverStates()
        {
            interactionManager.ClearHoverStates();
        }

        /// <summary>
        /// Live clickable elements for the current frame (inventory right panel, menus, etc.).
        /// </summary>
        public IReadOnlyList<ClickableElement> GetClickableElements() => interactionManager.ClickableElements;

        /// <summary>
        /// Resets the delete confirmation state.
        /// </summary>
        public void ResetDeleteConfirmation()
        {
            utilityCoordinator.ResetDeleteConfirmation();
        }

        /// <summary>
        /// Hides the tuning parameters menu panel.
        /// </summary>
        public void HideTuningParametersMenu()
        {
            windowManager.GetMainWindow()?.HideTuningMenuPanel();
        }

        /// <summary>
        /// Stops the dungeon selection animation.
        /// </summary>
        public void StopDungeonSelectionAnimation()
        {
            animationManager.StopDungeonSelectionAnimation();
        }

        /// <summary>
        /// Updates battle statistics progress display (re-renders running state).
        /// </summary>
        public void UpdateBattleStatisticsProgress(int completed, int total, string status)
        {
            renderer.RenderBattleStatisticsMenu(null, true);
        }

        #endregion

        #region Screen rendering (forward to renderer with context)

        private CanvasContext GetContext() => contextManager.GetCurrentContext();

        public void RenderDungeonSelection(Character player, List<Dungeon> dungeons)
        {
            animationManager.StartDungeonSelectionAnimation(player, dungeons);
            renderer.RenderDungeonSelection(player, dungeons, GetContext());
        }

        public void RenderMainMenu(bool hasSavedGame, string? characterName, int characterLevel)
        {
            renderer.RenderMainMenu(hasSavedGame, characterName, characterLevel);
        }

        public void RenderWeaponSelection(List<StartingWeapon> weapons)
        {
            renderer.RenderWeaponSelection(weapons, GetContext());
        }

        public void RenderCharacterCreation(Character character)
        {
            renderer.RenderCharacterCreation(character, GetContext());
        }

        public void RenderCharacterSelection(List<Character> characters, string? activeCharacterName, Dictionary<string, string> characterStatuses)
        {
            renderer.RenderCharacterSelection(characters, activeCharacterName, characterStatuses, GetContext());
        }

        public void RenderLoadCharacterSelection(List<(string characterId, string characterName, int level)> savedCharacters)
        {
            renderer.RenderLoadCharacterSelection(savedCharacters, GetContext());
        }

        public void RenderSettings()
        {
            renderer.RenderSettings();
        }

        public void RenderDeveloperMenu()
        {
            renderer.RenderDeveloperMenu();
        }

        public void RenderVariableEditor(EditableVariable? selectedVariable = null, bool isEditing = false, string? currentInput = null, string? message = null)
        {
            renderer.RenderVariableEditor(selectedVariable, isEditing, currentInput, message);
        }

        public void RenderTuningParametersMenu(string? selectedCategory = null, EditableVariable? selectedVariable = null, bool isEditing = false, string? currentInput = null, string? message = null)
        {
            renderer.RenderTuningParametersMenu(selectedCategory, selectedVariable, isEditing, currentInput, message);
        }

        public void RenderActionEditor()
        {
            renderer.RenderActionEditor();
        }

        public void RenderActionList(List<ActionData> actions, int page)
        {
            renderer.RenderActionList(actions, page);
        }

        public void RenderCreateActionForm(ActionData actionData, int currentStep, string[] formSteps, string? currentInput = null, bool isEditMode = false)
        {
            renderer.RenderCreateActionForm(actionData, currentStep, formSteps, currentInput, isEditMode);
        }

        public void RenderActionDetails(ActionData action)
        {
            renderer.RenderActionDetails(action);
        }

        public void RenderDeleteActionConfirmation(ActionData action, string? errorMessage = null)
        {
            renderer.RenderDeleteActionConfirmation(action, errorMessage);
        }

        public void RenderBattleStatisticsMenu(BattleStatisticsRunner.StatisticsResult? results, bool isRunning)
        {
            renderer.RenderBattleStatisticsMenu(results, isRunning);
        }

        public void RenderBattleStatisticsResults(BattleStatisticsRunner.StatisticsResult results)
        {
            renderer.RenderBattleStatisticsResults(results);
        }

        public void RenderWeaponTestResults(List<BattleStatisticsRunner.WeaponTestResult> results)
        {
            renderer.RenderWeaponTestResults(results);
        }

        public void RenderComprehensiveWeaponEnemyResults(BattleStatisticsRunner.ComprehensiveWeaponEnemyTestResult results)
        {
            renderer.RenderComprehensiveWeaponEnemyResults(results);
        }

        public void RenderInventory(Character character, List<Item> inventory)
        {
            renderer.RenderInventory(character, inventory, GetContext());
        }

        public void RenderItemSelectionPrompt(Character character, List<Item> inventory, string promptMessage, string actionType)
        {
            renderer.RenderItemSelectionPrompt(character, inventory, promptMessage, actionType, GetContext());
        }

        public void RenderSlotSelectionPrompt(Character character)
        {
            renderer.RenderSlotSelectionPrompt(character, GetContext());
        }

        public void RenderRaritySelectionPrompt(Character character, List<IGrouping<string, Item>> rarityGroups)
        {
            renderer.RenderRaritySelectionPrompt(character, rarityGroups, GetContext());
        }

        public void RenderTradeUpPreview(Character character, List<Item> itemsToTrade, Item resultingItem, string currentRarity, string nextRarity)
        {
            renderer.RenderTradeUpPreview(character, itemsToTrade, resultingItem, currentRarity, nextRarity, GetContext());
        }

        public void RenderItemComparison(Character character, Item newItem, Item? currentItem, string slot)
        {
            renderer.RenderItemComparison(character, newItem, currentItem, slot, GetContext());
        }

        public void RenderComboManagement(Character character)
        {
            renderer.RenderComboManagement(character, GetContext());
        }

        public void RenderComboReorderPrompt(Character character, string currentSequence)
        {
            renderer.RenderComboReorderPrompt(character, currentSequence, GetContext());
        }

        public void RenderComboActionSelection(Character character, string actionType)
        {
            renderer.RenderComboActionSelection(character, actionType, GetContext());
        }

        public void RenderGameMenu(Character player, List<Item> inventory)
        {
            renderer.RenderGameMenu(player, inventory, GetContext());
        }

        /// <inheritdoc cref="CanvasRenderer.RefreshActionInfoStripOnly(Character?, bool)"/>
        public void RefreshActionInfoStripOnly(Character player, bool drawHoverDetailOverlay = true)
        {
            renderer.RefreshActionInfoStripOnly(player, drawHoverDetailOverlay);
        }

        public void RenderCharacterInfoScreen(Character player)
        {
            renderer.RenderCharacterInfoScreen(player, GetContext());
        }

        public void RenderDungeonStart(Dungeon dungeon, Character player)
        {
            renderer.RenderDungeonStart(dungeon, player, GetContext());
        }

        public void RenderRoomEntry(Environment room, Character player, string? dungeonName, int? startFromBufferIndex = null)
        {
            renderer.RenderRoomEntry(room, player, dungeonName, GetContext(), startFromBufferIndex);
        }

        public void RenderEnemyEncounter(Enemy enemy, Character player, List<string> dungeonLog, string? dungeonName, string? roomName)
        {
            renderer.RenderEnemyEncounter(enemy, player, dungeonLog, dungeonName, roomName, GetContext());
        }

        public void RenderCombat(Character player, Enemy enemy, List<string> combatLog)
        {
            renderer.RenderCombat(player, enemy, combatLog, GetContext());
            if (stateManager?.CurrentState == GameState.ActionInteractionLab && ActionInteractionLabSession.Current != null)
                ActionLabControlsWindow.RefreshIfOpen();
        }

        public void RenderCombatResult(bool playerSurvived, Character player, Enemy enemy, BattleNarrative? battleNarrative, string? dungeonName, string? roomName)
        {
            renderer.RenderCombatResult(playerSurvived, player, enemy, battleNarrative, dungeonName, roomName, GetContext());
        }

        public void RenderRoomCompletion(Environment room, Character player, string? dungeonName)
        {
            renderer.RenderRoomCompletion(room, player, dungeonName, GetContext());
        }

        public void RenderDungeonCompletion(Dungeon dungeon, Character player, int xpGained, Item? lootReceived, List<LevelUpInfo> levelUpInfos, List<Item> itemsFoundDuringRun)
        {
            renderer.RenderDungeonCompletion(dungeon, player, xpGained, lootReceived, levelUpInfos ?? new List<LevelUpInfo>(), itemsFoundDuringRun ?? new List<Item>(), GetContext());
        }

        public void RenderDeathScreen(Character player, string defeatSummary)
        {
            renderer.RenderDeathScreen(player, defeatSummary, GetContext());
        }

        public void RenderDungeonExploration(Character player, string currentLocation, List<string> availableActions, List<string> recentEvents)
        {
            renderer.RenderDungeonExploration(player, currentLocation, availableActions, recentEvents, GetContext());
        }

        #endregion
    }
}
