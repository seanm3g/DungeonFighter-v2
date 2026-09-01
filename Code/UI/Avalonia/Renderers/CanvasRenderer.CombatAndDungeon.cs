using System;
using System.Collections.Generic;
using Avalonia.Threading;
using RPGGame;
using RPGGame.ActionInteractionLab;
using RPGGame.UI;
using RPGGame.UI.Avalonia.Managers;
using RPGGame.UI.Avalonia.Display;
using RPGGame.UI.Avalonia.Layout;

namespace RPGGame.UI.Avalonia.Renderers
{
    public partial class CanvasRenderer
    {
        /// <summary>
        /// Lab clone is never reference-equal to <see cref="ICanvasContextManager.GetCurrentCharacter()"/> when context
        /// was not updated; <see cref="CombatRenderingValidator"/> would block all combat repaints and the center stays blank.
        /// </summary>
        private bool ShouldBypassCombatValidationForLab(Character player)
        {
            var lab = ActionInteractionLabSession.Current;
            return lab != null && ReferenceEquals(player, lab.LabPlayer);
        }

        public void RenderCharacterCreation(Character character, CanvasContext context)
        {
            characterCreationRenderer.RenderCharacterCreation(character, context);
            // No center hover tooltip: it ClearTextInArea's the narrative region; cards in the strip still show Dmg/Spd.
            dungeonRenderer.RenderActionInfoStrip(character, drawHoverDetailOverlay: false, damageLineMode: ResolveActionStripDamageLineMode(character));
            canvas.Refresh();
        }

        public void RenderDungeonSelection(Character player, List<Dungeon> dungeons, CanvasContext context, string? customDungeonLevelEntryBuffer = null, string? currentRegionDisplayName = null)
        {
            RenderWithLayout(player, "DUNGEON SELECTION", (contentX, contentY, contentWidth, contentHeight) =>
            {
                dungeonRenderer.RenderDungeonSelection(contentX, contentY, contentWidth, contentHeight, dungeons, customDungeonLevelEntryBuffer, currentRegionDisplayName);
            }, context, null, null, null, clearCanvas: false);
            dungeonRenderer.RenderActionInfoStrip(player, damageLineMode: ResolveActionStripDamageLineMode(player));
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
            dungeonRenderer.RenderActionInfoStrip(player, damageLineMode: ResolveActionStripDamageLineMode(player));
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

            void PaintRoomEntryLayout()
            {
                RenderWithLayout(
                    player,
                    "DUNGEON FIGHTERS",
                    (contentX, contentY, contentWidth, contentHeight) =>
                    {
                        if (textManager is CanvasTextManager canvasTextManager)
                        {
                            var displayManager = canvasTextManager.DisplayManager;
                            var buffer = displayManager.Buffer;
                            // No enemy: keep room / exit-choice narrative in the mid band so combat
                            // history cannot fill reserved HUD rows (or the whole center column).
                            FighterResolveActionStackState.Clear();
                            CombatArenaHudLayout.ClearArenaInner(canvas);
                            CombatArenaHudLayout.GetNarrativeBand(out int nx, out int ny, out int nw, out int nh);
                            var renderer = new DisplayRenderer(new ColoredTextWriter(canvas));
                            renderer.Render(
                                buffer,
                                nx,
                                ny,
                                nw,
                                nh,
                                clearContent: true,
                                combatEnemyNamesForPrimaryLineRightAlign: contextManager.GetCombatLogEnemyAlignmentNames(),
                                combatHeroNameForLineAlignment: player?.Name);
                        }
                    },
                    context,
                    null,
                    displayDungeonName,
                    displayRoomName,
                    clearCanvas: false
                );
            }

            if (textManager is CanvasTextManager ctmSuppress)
            {
                using (ctmSuppress.DisplayManager.BeginSuppressReactiveRenderDuringImperativeRender())
                    PaintRoomEntryLayout();
            }
            else
            {
                PaintRoomEntryLayout();
            }

            dungeonRenderer.RenderActionInfoStrip(player, damageLineMode: ResolveActionStripDamageLineMode(player));
            canvas.Refresh();
        }

        public void RenderCombat(Character player, Enemy enemy, List<string> combatLog, CanvasContext context)
        {
            if (!ShouldBypassCombatValidationForLab(player) && !combatValidator.ValidateCharacterActive(player))
                return;
            EnsureDisplayManagerForPlayer(player);
            if (textManager is CanvasTextManager canvasTextManager)
            {
                // Use the same display manager as RenderCombatScreen / GetDisplayManagerForCharacter(player),
                // not DisplayManager getter alone (lab clone vs active player routing must stay aligned).
                var dm = canvasTextManager.GetDisplayManagerForCharacter(player);
                dm.SetMode(new CombatDisplayMode());
                System.Action renderCallback = () =>
                {
                    if (!ShouldBypassCombatValidationForLab(player) && !combatValidator.ValidateCharacterActive(player)) return;
                    if (Dispatcher.UIThread.CheckAccess())
                        RenderCombatScreenOnly(player, enemy, context);
                    else
                        Dispatcher.UIThread.Post(() => RenderCombatScreenOnly(player, enemy, context));
                };
                dm.SetExternalRenderCallback(renderCallback);
            }
            RenderCombatScreenOnly(player, enemy, context);
        }

        private void RenderCombatScreenOnly(Character player, Enemy enemy, CanvasContext context)
        {
            Enemy? currentEnemy = enemy ?? context.Enemy;
            if (currentEnemy == null && ActionInteractionLabSession.Current is { } labForEnemy)
                currentEnemy = labForEnemy.LabEnemy;
            if (!ShouldBypassCombatValidationForLab(player) && !combatValidator.ValidateCharacterActive(player))
                return;
            List<string>? filteredDungeonContext = context.DungeonContext;
            if (currentEnemy == null && filteredDungeonContext != null && filteredDungeonContext.Count > 0)
                filteredDungeonContext = new List<string>();
            bool shouldClear = context.IsFirstCombatRender;
            DisplayBuffer? combatBuffer = null;
            if (textManager is CanvasTextManager canvasTextManager)
                combatBuffer = canvasTextManager.GetDisplayManagerForCharacter(player).Buffer;

            RenderWithLayout(player, "COMBAT", (contentX, contentY, contentWidth, contentHeight) =>
            {
                CombatArenaHudLayout.ClearArenaInner(canvas);
            }, context, currentEnemy, context.DungeonName, context.RoomName, clearCanvas: shouldClear, hideSidePanelHealthBars: CombatArenaHudLayout.HideSidePanelHealthBars(currentEnemy != null));

            if (currentEnemy != null)
            {
                var arenaTextWriter = new ColoredTextWriter(canvas);
                CombatArenaHudRenderer.Render(
                    canvas,
                    arenaTextWriter,
                    player,
                    currentEnemy,
                    combatBuffer,
                    contextManager.GetCombatLogEnemyAlignmentNames());
                FighterResolveActionStackRenderer.Render(canvas);
            }
            else if (combatBuffer != null)
            {
                // Between fights: keep narrative between reserved HUD bands so defeat/room text
                // cannot overlap leftover or upcoming HP bar chrome.
                FighterResolveActionStackState.Clear();
                CombatArenaHudLayout.GetNarrativeBand(out int nx, out int ny, out int nw, out int nh);
                var displayRenderer = new DisplayRenderer(new ColoredTextWriter(canvas));
                displayRenderer.Render(
                    combatBuffer,
                    nx,
                    ny,
                    nw,
                    nh,
                    clearContent: true,
                    combatEnemyNamesForPrimaryLineRightAlign: contextManager.GetCombatLogEnemyAlignmentNames(),
                    combatHeroNameForLineAlignment: player.Name);
            }

            dungeonRenderer.RenderActionInfoStrip(player, damageLineMode: ResolveActionStripDamageLineMode(player));
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
                CombatArenaHudLayout.ClearArenaInner(canvas);
                CombatArenaHudLayout.GetNarrativeBand(out int nx, out int ny, out int nw, out int nh);
                dungeonRenderer.RenderEnemyEncounter(nx, ny, nw, nh, enemy, textManager, context.DungeonContext, contextManager.GetCombatLogEnemyAlignmentNames());
            }, context, enemy, dungeonName, roomName, clearCanvas: false);
            dungeonRenderer.RenderActionInfoStrip(player, damageLineMode: ResolveActionStripDamageLineMode(player));
            canvas.Refresh();
        }

        public void RenderCombatResult(bool playerSurvived, Character player, Enemy enemy, BattleNarrative? battleNarrative, string? dungeonName, string? roomName, CanvasContext context)
        {
            CombatActionInfoState.Clear();
            FighterResolveActionStackState.Clear();
            ActionStripHoverState.Clear();
            LeftPanelHoverState.Clear();
            if (textManager is CanvasTextManager canvasTextManager)
            {
                canvasTextManager.DisplayManager.SetMode(new StandardDisplayMode());
                canvasTextManager.DisplayManager.SetExternalRenderCallback(null);
            }
            RenderWithLayout(player, "COMBAT RESULT", (contentX, contentY, contentWidth, contentHeight) =>
            {
                CombatArenaHudLayout.ClearArenaInner(canvas);
                CombatArenaHudLayout.GetNarrativeBand(out int nx, out int ny, out int nw, out int nh);
                combatRenderer.RenderCombatResult(nx, ny, nw, nh, playerSurvived, enemy, battleNarrative);
            }, context, enemy, dungeonName, roomName);
            dungeonRenderer.RenderActionInfoStrip(player, damageLineMode: ResolveActionStripDamageLineMode(player));
            canvas.Refresh();
        }

        public void RenderRoomCompletion(Environment room, Character player, string? dungeonName, CanvasContext context)
        {
            FighterResolveActionStackState.Clear();
            RenderWithLayout(player, $"ROOM CLEARED: {room.Name.ToUpper()}", (contentX, contentY, contentWidth, contentHeight) =>
            {
                CombatArenaHudLayout.ClearArenaInner(canvas);
                CombatArenaHudLayout.GetNarrativeBand(out int nx, out int ny, out int nw, out int nh);
                dungeonRenderer.RenderRoomCompletion(nx, ny, nw, nh, room, player);
            }, context, null, dungeonName, null);
            dungeonRenderer.RenderActionInfoStrip(player, damageLineMode: ResolveActionStripDamageLineMode(player));
            canvas.Refresh();
        }

        public void RenderDungeonCompletion(Dungeon dungeon, Character player, int xpGained, Item? lootReceived, List<LevelUpInfo> levelUpInfos, List<Item> itemsFoundDuringRun, CanvasContext context)
        {
            EnsureDisplayManagerForPlayer(player);
            if (textManager is CanvasTextManager ctm)
            {
                ctm.DisplayManager.SetMode(new StandardDisplayMode());
                ctm.DisplayManager.SetExternalRenderCallback(null);
            }
            RenderWithLayout(player, $"DUNGEON COMPLETED: {dungeon.Name.ToUpper()}", (contentX, contentY, contentWidth, contentHeight) =>
            {
                if (textManager is CanvasTextManager canvasTextManager)
                {
                    var buffer = canvasTextManager.GetDisplayManagerForCharacter(player).Buffer;
                    dungeonRenderer.RenderDungeonCompletion(contentX, contentY, contentWidth, contentHeight, buffer);
                }
            }, context, null, context.DungeonName, null);
            dungeonRenderer.RenderActionInfoStrip(player, damageLineMode: ResolveActionStripDamageLineMode(player));
            canvas.Refresh();
        }

        public void RenderDeathScreen(Character player, string defeatSummary, CanvasContext context)
        {
            EnsureDisplayManagerForPlayer(player);
            CombatActionInfoState.Clear();
            FighterResolveActionStackState.Clear();
            ActionStripHoverState.Clear();
            LeftPanelHoverState.Clear();
            if (textManager is CanvasTextManager canvasTextManager)
            {
                canvasTextManager.DisplayManager.CancelPendingRenders();
                canvasTextManager.DisplayManager.SetMode(new StandardDisplayMode());
                canvasTextManager.DisplayManager.SetExternalRenderCallback(null);
            }
            RenderWithLayout(player, "YOU DIED", (contentX, contentY, contentWidth, contentHeight) =>
            {
                if (textManager is CanvasTextManager canvasTextManager)
                {
                    var buffer = canvasTextManager.GetDisplayManagerForCharacter(player).Buffer;
                    dungeonRenderer.RenderDeathScreen(contentX, contentY, contentWidth, contentHeight, buffer);
                }
            }, context, null, context.DungeonName, null, clearCanvas: false);
            dungeonRenderer.RenderActionInfoStrip(player, damageLineMode: ResolveActionStripDamageLineMode(player));
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
            dungeonRenderer.RenderActionInfoStrip(player, damageLineMode: ResolveActionStripDamageLineMode(player));
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
            dungeonRenderer.RenderActionInfoStrip(player, damageLineMode: ResolveActionStripDamageLineMode(player));
            canvas.Refresh();
        }

        public void RenderGameMenu(Character player, List<Item> inventory, CanvasContext context)
        {
            RenderWithLayout(player, $"WELCOME, {player.Name.ToUpper()}!", (contentX, contentY, contentWidth, contentHeight) =>
            {
                menuRenderer.RenderGameMenu(contentX, contentY, contentWidth, contentHeight);
            }, context, null, null, null, clearCanvas: false);
            dungeonRenderer.RenderActionInfoStrip(player, damageLineMode: ResolveActionStripDamageLineMode(player));
            canvas.Refresh();
        }

        public void RenderRegionTravel(Character player, IReadOnlyList<TravelRegion> destinations, TravelRouteResult? routeResult, CanvasContext context)
        {
            RenderWithLayout(player, "REGION TRAVEL", (contentX, contentY, contentWidth, contentHeight) =>
            {
                regionTravelRenderer.RenderRegionTravel(contentX, contentY, contentWidth, contentHeight, player, destinations, routeResult);
            }, context, null, null, null, clearCanvas: false);
            dungeonRenderer.RenderActionInfoStrip(player, damageLineMode: ResolveActionStripDamageLineMode(player));
            canvas.Refresh();
        }

        public void RenderSkillTree(Character player, int selectedIndex, int scrollOffset, string? statusMessage, CanvasContext context)
        {
            // clearCanvas:true matches Inventory — avoids hub/menu glyphs stacking under the tree.
            // Skill tree occupies the full center column (including the action-strip band); combo strip is hidden.
            RenderWithLayout(player, "SKILL TREE", (contentX, contentY, contentWidth, contentHeight) =>
            {
                int frameX = LayoutConstants.CENTER_PANEL_X;
                int frameY = LayoutConstants.CENTER_COLUMN_FULL_Y;
                int frameW = LayoutConstants.CENTER_PANEL_WIDTH;
                int frameH = LayoutConstants.CENTER_COLUMN_FULL_HEIGHT;
                canvas.ClearTextInArea(frameX, frameY, frameW, frameH);
                canvas.ClearBoxesInArea(frameX, frameY, frameW, frameH);
                canvas.AddBox(frameX, frameY, frameW, frameH, AsciiArtAssets.Colors.Cyan, CenterPanelModeTint.GetBackgroundColor());

                var (x, y, w, h) = LayoutConstants.GetCenterColumnFullContentRect();
                skillTreeRenderer.RenderSkillTree(x, y, w, h, player, selectedIndex, scrollOffset, statusMessage);
            }, context, null, null, null, clearCanvas: true);
            canvas.Refresh();
        }
    }
}
