using System;
using System.Collections.Generic;
using RPGGame;
using RPGGame.Editors;
using RPGGame.Data;
using RPGGame.UI.Avalonia.Managers;

namespace RPGGame.UI.Avalonia.Renderers
{
    public partial class CanvasRenderer
    {
        public void RenderMainMenu(bool hasSavedGame, string? characterName, int characterLevel)
        {
            RenderWithLayout(null, "MAIN MENU", (contentX, contentY, contentWidth, contentHeight) =>
            {
                menuRenderer.RenderMainMenuContent(contentX, contentY, contentWidth, contentHeight, hasSavedGame, characterName, characterLevel);
            }, new CanvasContext(), null, null, null, clearCanvas: false);
            messageRenderer.ClearLoadingStatus();
            canvas.Refresh();
        }

        public void RenderWeaponSelection(List<StartingWeapon> weapons, CanvasContext context)
        {
            if (weapons == null)
                weapons = new List<StartingWeapon>();
            RenderWithLayout(null, "WEAPON SELECTION", (contentX, contentY, contentWidth, contentHeight) =>
            {
                menuRenderer.RenderWeaponSelectionContent(contentX, contentY, contentWidth, contentHeight, weapons ?? new List<StartingWeapon>());
            }, context, null, null, null, clearCanvas: true);
            messageRenderer.ClearLoadingStatus();
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
    }
}
