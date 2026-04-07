using System;
using System.Collections.Generic;
using Avalonia.Threading;
using RPGGame;
using RPGGame.UI;
using RPGGame.UI.Avalonia.Managers;
using RPGGame.UI.Avalonia.Display;

namespace RPGGame.UI.Avalonia.Renderers
{
    public partial class CanvasRenderer
    {
        public void RenderCharacterCreation(Character character, CanvasContext context)
        {
            characterCreationRenderer.RenderCharacterCreation(character, context);
            // No center hover tooltip: it ClearTextInArea's the narrative region; cards in the strip still show Dmg/Spd.
            dungeonRenderer.RenderActionInfoStrip(character, drawHoverDetailOverlay: false);
            canvas.Refresh();
        }

        public void RenderDungeonSelection(Character player, List<Dungeon> dungeons, CanvasContext context)
        {
            RenderWithLayout(player, "DUNGEON SELECTION", (contentX, contentY, contentWidth, contentHeight) =>
            {
                dungeonRenderer.RenderDungeonSelection(contentX, contentY, contentWidth, contentHeight, dungeons);
            }, context, null, null, null, clearCanvas: false);
            dungeonRenderer.RenderActionInfoStrip(player);
            canvas.Refresh();
        }

        public void RenderDungeonStart(Dungeon dungeon, Character player, CanvasContext context)
        {
            EnsureDisplayManagerForPlayer(player);
            if (textManager is CanvasTextManager ctm)
            {
                ctm.DisplayManager.SetMode(new StandardDisplayMode());
                ctm.DisplayManager.SetExternalRenderCallback(null);
            }
            RenderWithLayout(player, "DUNGEON FIGHTERS", (contentX, contentY, contentWidth, contentHeight) =>
            {
                dungeonRenderer.RenderDungeonStart(contentX, contentY, contentWidth, contentHeight, dungeon, textManager, context.DungeonContext);
            }, context, null, context.DungeonName, null);
            dungeonRenderer.RenderActionInfoStrip(player);
            canvas.Refresh();
        }

        public void RenderRoomEntry(Environment room, Character player, string? dungeonName, CanvasContext context, int? startFromBufferIndex = null)
        {
            EnsureDisplayManagerForPlayer(player);
            if (textManager is CanvasTextManager ctm)
            {
                ctm.DisplayManager.SetMode(new StandardDisplayMode());
                ctm.DisplayManager.SetExternalRenderCallback(null);
            }
            string? displayDungeonName = dungeonName ?? context.DungeonName;
            string? displayRoomName = room?.Name ?? context.RoomName;
            RenderWithLayout(
                player,
                "DUNGEON FIGHTERS",
                (contentX, contentY, contentWidth, contentHeight) =>
                {
                    if (textManager is CanvasTextManager canvasTextManager)
                    {
                        var displayManager = canvasTextManager.DisplayManager;
                        var buffer = displayManager.Buffer;
                        var renderer = new DisplayRenderer(new ColoredTextWriter(canvas));
                        renderer.Render(buffer, contentX, contentY, contentWidth, contentHeight, clearContent: true);
                    }
                },
                context,
                null,
                displayDungeonName,
                displayRoomName,
                clearCanvas: false
            );
            dungeonRenderer.RenderActionInfoStrip(player);
            canvas.Refresh();
        }

        public void RenderCombat(Character player, Enemy enemy, List<string> combatLog, CanvasContext context)
        {
            if (!combatValidator.ValidateCharacterActive(player))
                return;
            EnsureDisplayManagerForPlayer(player);
            if (textManager is CanvasTextManager canvasTextManager)
            {
                canvasTextManager.DisplayManager.SetMode(new CombatDisplayMode());
                System.Action renderCallback = () =>
                {
                    if (!combatValidator.ValidateCharacterActive(player)) return;
                    if (Dispatcher.UIThread.CheckAccess())
                        RenderCombatScreenOnly(player, enemy, context);
                    else
                        Dispatcher.UIThread.Post(() => RenderCombatScreenOnly(player, enemy, context));
                };
                canvasTextManager.DisplayManager.SetExternalRenderCallback(renderCallback);
            }
            RenderCombatScreenOnly(player, enemy, context);
        }

        private void RenderCombatScreenOnly(Character player, Enemy enemy, CanvasContext context)
        {
            Enemy? currentEnemy = enemy ?? context.Enemy;
            if (!combatValidator.ValidateCharacterActive(player))
                return;
            List<string>? filteredDungeonContext = context.DungeonContext;
            if (currentEnemy == null && filteredDungeonContext != null && filteredDungeonContext.Count > 0)
                filteredDungeonContext = new List<string>();
            bool shouldClear = context.IsFirstCombatRender;
            RenderWithLayout(player, "COMBAT", (contentX, contentY, contentWidth, contentHeight) =>
            {
                if (currentEnemy != null)
                {
                    dungeonRenderer.RenderCombatScreen(contentX, contentY, contentWidth, contentHeight,
                        null, null, currentEnemy, textManager, player, filteredDungeonContext);
                }
            }, context, currentEnemy, context.DungeonName, context.RoomName, clearCanvas: shouldClear);
            dungeonRenderer.RenderActionInfoStrip(player);
            canvas.Refresh();
            if (shouldClear)
                contextManager.MarkCombatRenderComplete();
        }

        public void RenderEnemyEncounter(Enemy enemy, Character player, List<string> dungeonLog, string? dungeonName, string? roomName, CanvasContext context)
        {
            if (!combatValidator.ValidateCharacterActive(player)) return;
            EnsureDisplayManagerForPlayer(player);
            if (textManager is CanvasTextManager ctm)
            {
                ctm.DisplayManager.SetMode(new StandardDisplayMode());
                ctm.DisplayManager.SetExternalRenderCallback(null);
            }
            RenderWithLayout(player, "COMBAT", (contentX, contentY, contentWidth, contentHeight) =>
            {
                dungeonRenderer.RenderEnemyEncounter(contentX, contentY, contentWidth, contentHeight, enemy, textManager, context.DungeonContext);
            }, context, enemy, dungeonName, roomName, clearCanvas: false);
            dungeonRenderer.RenderActionInfoStrip(player);
            canvas.Refresh();
        }

        public void RenderCombatResult(bool playerSurvived, Character player, Enemy enemy, BattleNarrative? battleNarrative, string? dungeonName, string? roomName, CanvasContext context)
        {
            CombatActionInfoState.Clear();
            ActionStripHoverState.Clear();
            LeftPanelHoverState.Clear();
            if (textManager is CanvasTextManager canvasTextManager)
            {
                canvasTextManager.DisplayManager.SetMode(new StandardDisplayMode());
                canvasTextManager.DisplayManager.SetExternalRenderCallback(null);
            }
            RenderWithLayout(player, "COMBAT RESULT", (contentX, contentY, contentWidth, contentHeight) =>
            {
                combatRenderer.RenderCombatResult(contentX, contentY, contentWidth, contentHeight, playerSurvived, enemy, battleNarrative);
            }, context, enemy, dungeonName, roomName);
            dungeonRenderer.RenderActionInfoStrip(player);
            canvas.Refresh();
        }

        public void RenderRoomCompletion(Environment room, Character player, string? dungeonName, CanvasContext context)
        {
            RenderWithLayout(player, $"ROOM CLEARED: {room.Name.ToUpper()}", (contentX, contentY, contentWidth, contentHeight) =>
            {
                dungeonRenderer.RenderRoomCompletion(contentX, contentY, contentWidth, contentHeight, room, player);
            }, context, null, dungeonName, null);
            dungeonRenderer.RenderActionInfoStrip(player);
            canvas.Refresh();
        }

        public void RenderDungeonCompletion(Dungeon dungeon, Character player, int xpGained, Item? lootReceived, List<LevelUpInfo> levelUpInfos, List<Item> itemsFoundDuringRun, CanvasContext context)
        {
            RenderWithLayout(player, $"DUNGEON COMPLETED: {dungeon.Name.ToUpper()}", (contentX, contentY, contentWidth, contentHeight) =>
            {
                dungeonRenderer.RenderDungeonCompletion(contentX, contentY, contentWidth, contentHeight, dungeon, player, xpGained, lootReceived, levelUpInfos ?? new List<LevelUpInfo>(), itemsFoundDuringRun ?? new List<Item>(), context.DungeonContext);
            }, context, null, context.DungeonName, null);
            dungeonRenderer.RenderActionInfoStrip(player);
            canvas.Refresh();
        }

        public void RenderDeathScreen(Character player, string defeatSummary, CanvasContext context)
        {
            CombatActionInfoState.Clear();
            ActionStripHoverState.Clear();
            LeftPanelHoverState.Clear();
            if (textManager is CanvasTextManager canvasTextManager)
            {
                canvasTextManager.DisplayManager.CancelPendingRenders();
                canvasTextManager.DisplayManager.SetMode(new MenuDisplayMode());
                canvasTextManager.DisplayManager.SetExternalRenderCallback(null);
            }
            RenderWithLayout(player, "YOU DIED", (contentX, contentY, contentWidth, contentHeight) =>
            {
                dungeonRenderer.RenderDeathScreen(contentX, contentY, contentWidth, contentHeight, player, defeatSummary);
            }, context, null, context.DungeonName, null, clearCanvas: false);
            dungeonRenderer.RenderActionInfoStrip(player);
            canvas.Refresh();
        }

        public void RenderDungeonExploration(Character player, string currentLocation, List<string> availableActions, List<string> recentEvents, CanvasContext context)
        {
            EnsureDisplayManagerForPlayer(player);
            if (textManager is CanvasTextManager ctm)
            {
                ctm.DisplayManager.SetMode(new StandardDisplayMode());
                ctm.DisplayManager.SetExternalRenderCallback(null);
            }
            RenderWithLayout(
                player,
                "DUNGEON EXPLORATION",
                (contentX, contentY, contentWidth, contentHeight) =>
                {
                    dungeonExplorationRenderer.RenderExplorationContent(
                        contentX, contentY, contentWidth, contentHeight,
                        currentLocation, availableActions, recentEvents);
                },
                context,
                null,
                context.DungeonName,
                context.RoomName,
                clearCanvas: false);
            dungeonRenderer.RenderActionInfoStrip(player);
            canvas.Refresh();
        }

        /// <summary>
        /// Character info shell (center empty), matching <see cref="GameScreenCoordinator.ShowCharacterInfo"/> transition.
        /// </summary>
        public void RenderCharacterInfoScreen(Character player, CanvasContext context)
        {
            RenderWithLayout(player, "CHARACTER INFO", (contentX, contentY, contentWidth, contentHeight) =>
            {
            }, context, null, null, null, clearCanvas: true);
            dungeonRenderer.RenderActionInfoStrip(player);
            canvas.Refresh();
        }

        public void RenderGameMenu(Character player, List<Item> inventory, CanvasContext context)
        {
            RenderWithLayout(player, $"WELCOME, {player.Name.ToUpper()}!", (contentX, contentY, contentWidth, contentHeight) =>
            {
                menuRenderer.RenderGameMenu(contentX, contentY, contentWidth, contentHeight);
            }, context, null, null, null, clearCanvas: false);
            dungeonRenderer.RenderActionInfoStrip(player);
            canvas.Refresh();
        }
    }
}
