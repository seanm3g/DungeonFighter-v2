using Avalonia.Media;
using RPGGame;
using RPGGame.UI.Avalonia.Managers;
using System.Collections.Generic;

namespace RPGGame.UI.Avalonia.Renderers
{
    /// <summary>
    /// Interface for canvas rendering operations
    /// </summary>
    public interface ICanvasRenderer
    {
        void RenderDisplayBuffer(CanvasContext context);
        void RenderMainMenu(bool hasSavedGame, string? characterName, int characterLevel);
        void RenderInventory(Character character, List<Item> inventory, CanvasContext context);
        void RenderItemSelectionPrompt(Character character, List<Item> inventory, string promptMessage, string actionType, CanvasContext context);
        void RenderSlotSelectionPrompt(Character character, CanvasContext context);
        void RenderRaritySelectionPrompt(Character character, List<System.Linq.IGrouping<string, Item>> rarityGroups, CanvasContext context);
        void RenderTradeUpPreview(Character character, List<Item> itemsToTrade, Item resultingItem, string currentRarity, string nextRarity, CanvasContext context);
        void RenderItemComparison(Character character, Item newItem, Item? currentItem, string slot, CanvasContext context);
        void RenderWeaponSelection(List<StartingWeapon> weapons, CanvasContext context);
        void RenderCharacterCreation(Character character, CanvasContext context);
        void RenderSettings();
        void RenderDungeonSelection(Character player, List<Dungeon> dungeons, CanvasContext context);
        void RenderDungeonStart(Dungeon dungeon, Character player, CanvasContext context);
        void RenderRoomEntry(Environment room, Character player, string? dungeonName, CanvasContext context, int? startFromBufferIndex = null);
        void RenderEnemyEncounter(Enemy enemy, Character player, List<string> dungeonLog, string? dungeonName, string? roomName, CanvasContext context);
        void RenderCombatResult(bool playerSurvived, Character player, Enemy enemy, BattleNarrative? battleNarrative, string? dungeonName, string? roomName, CanvasContext context);
        void RenderRoomCompletion(Environment room, Character player, string? dungeonName, CanvasContext context);
        void RenderDungeonCompletion(Dungeon dungeon, Character player, int xpGained, Item? lootReceived, List<LevelUpInfo> levelUpInfos, List<Item> itemsFoundDuringRun, CanvasContext context);
        void RenderDungeonExploration(Character player, string currentLocation, List<string> availableActions, List<string> recentEvents, CanvasContext context);
        void RenderGameMenu(Character player, List<Item> inventory, CanvasContext context);
        void ShowMessage(string message, Color color = default);
        void ShowError(string error);
        void ShowSuccess(string message);
        void ShowLoadingAnimation(string message = "Loading...");
        void ShowError(string error, string suggestion = "");
        void UpdateStatus(string message);
        void ShowInvalidKeyMessage(string message);
        void ToggleHelp();
        void RenderHelp();
        void Refresh();
    }
}
