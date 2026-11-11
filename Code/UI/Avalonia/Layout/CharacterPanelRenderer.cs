using System;
using System.Collections.Generic;
using System.Linq;
using Avalonia.Media;
using RPGGame;
using RPGGame.ActionInteractionLab;
using RPGGame.Combat.Calculators;
using RPGGame.UI;
using RPGGame.UI.Avalonia;
using RPGGame.UI.Avalonia.Feedback;
using RPGGame.UI.Avalonia.Renderers;
using RPGGame.UI.Avalonia.Renderers.Inventory;
using RPGGame.UI.Avalonia.Managers;
using RPGGame.UI.ColorSystem;
using RPGGame.UI.ColorSystem.Applications;

namespace RPGGame.UI.Avalonia.Layout
{

    /// <summary>
    /// Renders the character information panel (left side)
    /// </summary>
    public class CharacterPanelRenderer
    {
        private const string ToggleSectionHero = "toggle_section_hero";
        private const string ToggleSectionStats = "toggle_section_stats";
        private const string ToggleSectionGear = "toggle_section_gear";
        private const string ToggleSectionThresholds = "toggle_section_thresholds";

        /// <summary>ASCII section line matching STATS: <c>====  LABEL  ====</c> (double spaces around label).</summary>
        private static string FormatLeftPanelSectionHeader(string label) => $"====  {label}  ====";

        private readonly GameCanvasControl canvas;
        private readonly ColoredTextWriter textWriter;
        private readonly StatsPanelStateManager? stateManager;
        private readonly ICanvasInteractionManager? interactionManager;
        private bool correctingLeftPanelScroll;
        
        public CharacterPanelRenderer(
            GameCanvasControl canvas, 
            ColoredTextWriter textWriter,
            StatsPanelStateManager? stateManager = null,
            ICanvasInteractionManager? interactionManager = null)
        {
            this.canvas = canvas;
            this.textWriter = textWriter;
            this.stateManager = stateManager;
            this.interactionManager = interactionManager;
        }
        
        /// <summary>
        /// Renders the character information panel (left side). The player hero is expected; dice thresholds
        /// and status lines use this character as the roll source.
        /// </summary>
        /// <param name="gameState">Current game state; Action Lab keeps GEAR and STATUS EFFECTS.</param>
        /// <param name="inDungeonRun">True while a dungeon is selected; hides GEAR and shows STATUS EFFECTS until leave.</param>
        public void RenderCharacterPanel(Character character, GameState? gameState = null, bool inDungeonRun = false)
        {
            // Clear the left panel area before drawing so re-renders with clearCanvas: false (e.g. after level-up) do not leave duplicate content
            int leftX = LayoutConstants.LEFT_PANEL_X;
            int leftY = LayoutConstants.LEFT_PANEL_Y;
            int leftW = LayoutConstants.LEFT_PANEL_WIDTH;
            // Clear through bottom-border spill so prior threshold rows cannot linger below the frame.
            int leftH = LeftPanelViewport.ScrubBottomExclusive - leftY;
            canvas.ClearTextInArea(leftX, leftY, leftW, leftH);
            canvas.ClearProgressBarsInArea(leftX, leftY, leftW, leftH);
            canvas.ClearSegmentedBarsInArea(leftX, leftY, leftW, leftH);
            canvas.ClearBoxesInArea(leftX, leftY, leftW, leftH);

            // Main border for character panel - starts at X=0 with no padding
            canvas.AddBorder(LayoutConstants.LEFT_PANEL_X, LayoutConstants.LEFT_PANEL_Y, LayoutConstants.LEFT_PANEL_WIDTH, LayoutConstants.LEFT_PANEL_HEIGHT, PanelJoin.Border(AsciiArtAssets.Colors.Blue));
            PanelJoin.ApplyLeftPanel(canvas);

            int contentTop = LeftPanelViewport.ContentTop;
            int contentBottomExclusive = LeftPanelViewport.ContentBottomExclusive;
            int viewportHeight = LeftPanelViewport.ContentHeight;
            int scroll = 0;
            if (stateManager != null)
            {
                scroll = LeftPanelViewport.ClampScrollOffset(
                    stateManager.LeftPanelScrollOffset,
                    stateManager.LeftPanelContentHeight,
                    viewportHeight);
            }

            // Keep click-burst messes glued to left-panel rows when the body scrolls.
            canvas.SyncClickBurstViewportScroll(
                GameCanvasControl.ClickBurstLeftViewportId,
                scroll,
                contentTop,
                viewportHeight);

            int y = contentTop - scroll;
            int contentOriginY = y;
            int x = LayoutConstants.LEFT_PANEL_X + 2; // Reduced from +4 since border now starts at 0
            int headerClickWidth = LayoutConstants.LEFT_PANEL_WIDTH - 4;

            // Roster view (toggled by clicking empty left-panel space): saved fighter names instead of the hero sheet.
            if (stateManager != null && stateManager.ShowRosterNames)
            {
                RenderRosterNames(character, x, y, headerClickWidth);
                ScrubLeftPanelOutsideViewport(leftX, leftY, leftW, leftH, contentTop, contentBottomExclusive);
                canvas.AddBorder(LayoutConstants.LEFT_PANEL_X, LayoutConstants.LEFT_PANEL_Y, LayoutConstants.LEFT_PANEL_WIDTH, LayoutConstants.LEFT_PANEL_HEIGHT, PanelJoin.Border(AsciiArtAssets.Colors.Blue));
                PanelJoin.ApplyLeftPanel(canvas);
                return;
            }

            // --- HERO --- (left-aligned like STATS/GEAR; body order: name, HP bar, Lvl+class, XP)
            int heroHeaderY = y;
            string heroHeaderText = FormatLeftPanelSectionHeader(UIConstants.Headers.Hero);
            PanelText(x, y, heroHeaderText, AsciiArtAssets.Colors.Gold);
            y += 2;
            if (interactionManager != null && stateManager != null && LeftPanelViewport.IsRowVisible(heroHeaderY))
            {
                interactionManager.AddClickableElement(new ClickableElement
                {
                    X = x,
                    Y = heroHeaderY,
                    Width = headerClickWidth,
                    Height = 1,
                    Type = ElementType.Text,
                    Value = ToggleSectionHero,
                    DisplayText = "Hero"
                });
            }

            bool heroOpen = stateManager == null || !stateManager.HeroCollapsed;
            if (heroOpen)
            {
                int nameY = y;
                var heroNameSegments = HeroNamePanelColoredText.BuildLeftPanelHeroNameSegments(character);
                if (LeftPanelViewport.IsRowVisible(nameY))
                    textWriter.RenderSegments(heroNameSegments, x, nameY);
                y++;

                int healthBarWidth = LayoutConstants.LEFT_PANEL_WIDTH - 4;
                int healthBarY = y;
                int displayedDefense = ClassDefenseCalculator.GetDisplayedDefense(character);
                int barAreaHeight = D20ThresholdBarRenderer.CombatBarAreaRowCount;
                int thresholdBarY = healthBarY;
                int thresholdHoverRowY = healthBarY + 1;
                int hpValueY = healthBarY + barAreaHeight;

                int displayHp = RPGGame.Combat.UI.HealthBarDisplayHold.Resolve($"player_{character.Name}", character.CurrentHealth);
                int maxHp = character.GetEffectiveMaxHealth();

                ThresholdDisplayFormatting.D20OutcomeSegment[] thresholdSegments = System.Array.Empty<ThresholdDisplayFormatting.D20OutcomeSegment>();
                if (LeftPanelViewport.IsRangeVisible(healthBarY, barAreaHeight))
                {
                    canvas.ClearProgressBarsInArea(x, healthBarY, healthBarWidth, barAreaHeight);
                    canvas.ClearSegmentedBarsInArea(x, healthBarY, healthBarWidth, barAreaHeight);
                    if (LeftPanelViewport.IsRowVisible(hpValueY))
                        canvas.ClearTextInArea(x, hpValueY, healthBarWidth, 1);

                    canvas.AddHealthBar(
                        x,
                        healthBarY,
                        healthBarWidth,
                        displayHp,
                        maxHp,
                        entityId: $"player_{character.Name}",
                        heightScale: D20ThresholdBarRenderer.CombatHealthHeightScale);

                    thresholdSegments = D20ThresholdBarRenderer.RenderBar(
                        canvas,
                        x,
                        thresholdBarY,
                        healthBarWidth,
                        character,
                        ThresholdBarPanel.Hero,
                        D20ThresholdBarRenderer.CombatStripHeightScale,
                        D20ThresholdBarRenderer.CombatStripVerticalOffsetNoArmor);
                }

                PanelText(
                    x,
                    hpValueY,
                    $"Health {displayHp}/{maxHp}  Defense {displayedDefense}",
                    AsciiArtAssets.Colors.White);

                y = hpValueY + 1;

                string currentClass = character.GetCurrentClass();
                int levelY = y;
                PanelText(x, y, $"Lvl {character.Level} {currentClass}", AsciiArtAssets.Colors.Gold);
                y++;

                int xpRequired = character.Progression.GetXPRequiredForNextLevel();
                int xpY = y;
                PanelText(x, y, $"XP {character.XP}/{xpRequired}", AsciiArtAssets.Colors.Cyan);
                y++;

                string classPointsText = GetClassPointsDisplay(character);
                int? classPointsY = null;
                if (!string.IsNullOrEmpty(classPointsText))
                {
                    classPointsY = y;
                    PanelText(x, y, classPointsText, AsciiArtAssets.Colors.Gray);
                    y++;
                }

                if (interactionManager != null && stateManager != null)
                {
                    RegisterLeftPanelHoverRow(x, nameY, headerClickWidth, 1, "hero:name");
                    RegisterLeftPanelHoverRow(x, healthBarY, headerClickWidth, barAreaHeight, "hero:hp");
                    var thresholdHoverWidths = D20ThresholdBarRenderer.GetSegmentHoverWidths(healthBarWidth, thresholdSegments);
                    int thresholdHoverX = x;
                    for (int i = 0; i < thresholdSegments.Length; i++)
                    {
                        RegisterLeftPanelHoverRow(
                            thresholdHoverX,
                            thresholdHoverRowY,
                            thresholdHoverWidths[i],
                            1,
                            ThresholdChanceLabelToHoverId(thresholdSegments[i].Label));
                        thresholdHoverX += thresholdHoverWidths[i];
                    }
                    RegisterLeftPanelHoverRow(x, hpValueY, headerClickWidth, 1, "stat:armor");
                    RegisterLeftPanelHoverRow(x, levelY, headerClickWidth, 1, "hero:level");
                    RegisterLeftPanelHoverRow(x, xpY, headerClickWidth, 1, "hero:xp");
                    if (classPointsY.HasValue)
                        RegisterLeftPanelHoverRow(x, classPointsY.Value, headerClickWidth, 1, "hero:classpts");
                }

                y += 2;
            }

            // Inventory hub: STATS and THRESHOLDS sit in the band along the bottom of the center column instead (sets stay under GEAR).
            bool heroBand = InventoryHeroBand.IsShown;

            // --- GEAR --- (hidden for the whole dungeon run; hover hero name for loadout; Action Lab keeps the section)
            if (LeftPanelSectionVisibility.ShowGear(gameState, inDungeonRun))
            {
                int gearHeaderY = y;
                PanelText(x, y, FormatLeftPanelSectionHeader(UIConstants.Headers.Gear), AsciiArtAssets.Colors.Gold);
                y += 2;
                if (interactionManager != null && stateManager != null && LeftPanelViewport.IsRowVisible(gearHeaderY))
                {
                    interactionManager.AddClickableElement(new ClickableElement
                    {
                        X = x,
                        Y = gearHeaderY,
                        Width = headerClickWidth,
                        Height = 1,
                        Type = ElementType.Text,
                        Value = ToggleSectionGear,
                        DisplayText = "Gear"
                    });
                }

                // Clicking the figure swaps to item names (the inventory hub keeps the figure for drag/drop).
                bool gearNames = stateManager?.ShowGearNames == true && gameState != GameState.Inventory;
                if ((stateManager == null || !stateManager.GearCollapsed) && !gearNames && TryRenderGearFigure(character, gameState, x, ref y, headerClickWidth))
                {
                    RenderFormingSets(character, x, ref y, headerClickWidth);
                    RenderAnimalSets(character, x, ref y, headerClickWidth);
                }
                else if (stateManager == null || !stateManager.GearCollapsed)
                {
                    InventoryScreenRenderer.LastPaperDollBounds = default;
                    RenderEquipmentSlot(x, ref y, headerClickWidth, "Weapon", character.Weapon, "gear:weapon", 1);
                    RenderEquipmentSlot(x, ref y, headerClickWidth, "Head", character.Head, "gear:head", 1);
                    RenderEquipmentSlot(x, ref y, headerClickWidth, "Body", character.Body, "gear:body", 1);
                    RenderEquipmentSlot(x, ref y, headerClickWidth, "Legs", character.Legs, "gear:legs", 1);
                    RenderEquipmentSlot(x, ref y, headerClickWidth, "Feet", character.Feet, "gear:feet", 1);
                    RenderEquipmentSlot(x, ref y, headerClickWidth, "Charm", character.Charm, "gear:charm", 1);
                    RenderFormingSets(character, x, ref y, headerClickWidth);
                    RenderAnimalSets(character, x, ref y, headerClickWidth);
                }
            }

            // --- STATS --- (static gold like HERO/GEAR; no glow — glow animation shifted hue vs other headers)
            if (heroBand)
                stateManager?.ResetStatsAreaBounds();
            else
            {
            int statsHeaderY = y;
            string statsHeaderText = FormatLeftPanelSectionHeader(UIConstants.Headers.Stats);
            PanelText(x, y, statsHeaderText, AsciiArtAssets.Colors.Gold);
            y += 2;
            if (interactionManager != null && stateManager != null && LeftPanelViewport.IsRowVisible(statsHeaderY))
            {
                interactionManager.AddClickableElement(new ClickableElement
                {
                    X = x,
                    Y = statsHeaderY,
                    Width = headerClickWidth,
                    Height = 1,
                    Type = ElementType.Text,
                    Value = ToggleSectionStats,
                    DisplayText = "Stats"
                });
            }

            bool statsOpen = stateManager == null || !stateManager.StatsCollapsed;
            if (statsOpen)
            {
                int weaponDamage = (character.Weapon is WeaponItem w) ? w.GetTotalDamage() : 0;
                int equipmentDamageBonus = character.GetEquipmentDamageBonus();
                int modificationDamageBonus = character.GetModificationDamageBonus();
                int totalDamage = character.Combat.GetTotalDamage();
                double attackSpeed = character.GetTotalAttackSpeed();

                int damageRowY = y;
                PanelCharacterStat(x, y, "Damage", totalDamage, 0, AsciiArtAssets.Colors.White, AsciiArtAssets.Colors.White);
                y++;
                int speedRowY = y;
                PanelText(x, y, $"Speed:   {attackSpeed:F2}s", AsciiArtAssets.Colors.White);
                y++;
                int ampRowY = y;
                double ampBasePerStep = character.GetComboAmplifier();
                double queuedSheetAmpPct = character.PeekQueuedSheetAmpModPercentForDisplay();
                const int statsValueColumnIndex = 9; // first character index of value in AddCharacterStat lines (Damage/Speed/Armor)
                string ampPrefix = "AMP:";
                string ampCore = ampPrefix + new string(' ', statsValueColumnIndex - ampPrefix.Length) + $"{ampBasePerStep:F2}x";
                if (queuedSheetAmpPct > 0.05)
                    ampCore += $"  +{queuedSheetAmpPct:0.#}% nxt";
                PanelText(x, y, ampCore, AsciiArtAssets.Colors.White);
                y++;
                int armorRowY = y;
                PanelCharacterStat(x, y, "Defense", ClassDefenseCalculator.GetDisplayedDefense(character), 0, AsciiArtAssets.Colors.White, AsciiArtAssets.Colors.DarkBlue);
                y++;
                int slotsRowY = -1;
                if (ActionInteractionLabSession.Current != null)
                {
                    slotsRowY = y;
                    PanelCharacterStat(x, y, "SLOTS", ComboSequenceMaxHelper.GetEffectiveMax(character), 0, AsciiArtAssets.Colors.White, AsciiArtAssets.Colors.Cyan);
                    y++;
                }
                y++;

                string primaryStat = GetPrimaryStatForCharacter(character);
                var primaryStatHighlight = PrimaryStatRowHighlightColors.ForCharacter(character);

                // Click any attribute row to toggle highest → lowest ordering (stable for ties).
                bool sortAttributes = stateManager?.SortAttributesByValue == true;
                var attributeRows = LeftPanelAttributeOrder.Build(character, sortAttributes);
                var attributeRowYs = new List<(int rowY, string idSuffix)>();
                foreach (var row in attributeRows)
                {
                    if (row.IsHybrid)
                        y++; // blank line between the four base stats and the hybrid attribute
                    attributeRowYs.Add((y, row.HoverId));
                    PanelCharacterStat(x, y, row.Label, row.Value, 0,
                        row.IsHybrid || primaryStat == row.PrimaryName ? primaryStatHighlight : AsciiArtAssets.Colors.White);
                    y++;
                }

                List<(int rowY, string idSuffix)>? expandedHoverTargets = interactionManager != null && stateManager != null
                    ? new List<(int rowY, string idSuffix)>()
                    : null;

                // Secondary stat lines (magic find, roll bonus, etc.) always shown when STATS is open; lphover:* rows registered below.
                RenderExpandedStats(character, x, ref y, expandedHoverTargets);

                y += 2;
                int statsAreaEndY = y;

                if (stateManager != null && interactionManager != null)
                {
                    stateManager.SetStatsAreaBounds(x, statsHeaderY, headerClickWidth, statsAreaEndY - statsHeaderY);
                    RegisterLeftPanelHoverRow(x, damageRowY, headerClickWidth, 1, "stat:damage");
                    RegisterLeftPanelHoverRow(x, speedRowY, headerClickWidth, 1, "stat:speed");
                    RegisterLeftPanelHoverRow(x, ampRowY, headerClickWidth, 1, "stat:amp");
                    RegisterLeftPanelHoverRow(x, armorRowY, headerClickWidth, 1, "stat:armor");
                    if (slotsRowY >= 0)
                        RegisterLeftPanelHoverRow(x, slotsRowY, headerClickWidth, 1, "stat:actionslots");
                    foreach (var (rowY, id) in attributeRowYs)
                        RegisterLeftPanelHoverRow(x, rowY, headerClickWidth, 1, id);
                    if (expandedHoverTargets != null)
                    {
                        foreach (var (rowY, id) in expandedHoverTargets)
                            RegisterLeftPanelHoverRow(x, rowY, headerClickWidth, 1, id);
                    }
                }
            }
            else if (stateManager != null)
            {
                stateManager.ResetStatsAreaBounds();
            }
            }

            // --- STATUS EFFECTS --- (hero; above thresholds; dungeon run + Action Lab; enemy effects are on the right panel)
            if (LeftPanelSectionVisibility.ShowStatusEffects(gameState, inDungeonRun))
            {
                int statusHeaderY = y;
                PanelText(x, y, FormatLeftPanelSectionHeader(UIConstants.Headers.StatusEffects), AsciiArtAssets.Colors.Gold);
                y += 2;
                const int maxHeroEffectLines = 5;
                const int maxHeroLineLen = 27; // after the 2-column icon gutter
                var heroEffects = StatusEffectDisplayLines.Build(character, character);
                if (heroEffects.Count > 0)
                {
                    for (int i = 0; i < Math.Min(heroEffects.Count, maxHeroEffectLines); i++)
                    {
                        int effectRowY = y;
                        string line = heroEffects[i];
                        // The icon stands in for the effect's name; its numbers follow.
                        var (iconKey, afterIcon) = StatusEffectDisplayLines.SplitIcon(line);
                        if (StatusArt.TryGetIcon(iconKey, out var icon, out var iconRect))
                        {
                            line = afterIcon;
                            if (LeftPanelViewport.IsRowVisible(y))
                                canvas.AddImage(x, y, 2, 1, icon, iconRect, paddingPixels: 1);
                        }
                        if (line.Length > maxHeroLineLen)
                            line = line.Substring(0, maxHeroLineLen - 3) + "...";
                        PanelText(x + 2, y, line, AsciiArtAssets.Colors.White);
                        if (interactionManager != null && stateManager != null)
                            RegisterLeftPanelHoverRow(x, effectRowY, headerClickWidth, 1, "status:" + i);
                        y++;
                    }
                    if (heroEffects.Count > maxHeroEffectLines)
                    {
                        int overflowY = y;
                        PanelText(x, y, $"+{heroEffects.Count - maxHeroEffectLines} more", AsciiArtAssets.Colors.Gray);
                        if (interactionManager != null && stateManager != null)
                            RegisterLeftPanelHoverRow(x, overflowY, headerClickWidth, 1, "status:overflow");
                        y++;
                    }
                }
                TutorialCoachTargets.Set(TutorialCoachFocus.StatusEffects, (x - 1, statusHeaderY - 1, headerClickWidth + 2, y - statusHeaderY + 2));
                y += 1;
            }
            else
                TutorialCoachTargets.Set(TutorialCoachFocus.StatusEffects, null);

            // --- THRESHOLDS / CHANCES --- (ladder numbers or exclusive d20 %; bar is under health)
            if (!heroBand)
            {
            var thresholdsHudMode = stateManager?.ThresholdsHudMode ?? ThresholdsHudMode.Ladder;
            bool showThresholdChances = thresholdsHudMode == ThresholdsHudMode.Chances;
            int thresholdsHeaderY = y;
            string thresholdsHeaderLabel = showThresholdChances ? UIConstants.Headers.Chances : UIConstants.Headers.Thresholds;
            PanelText(x, y, FormatLeftPanelSectionHeader(thresholdsHeaderLabel), AsciiArtAssets.Colors.Gold);
            y += 2;
            if (interactionManager != null && stateManager != null && LeftPanelViewport.IsRowVisible(thresholdsHeaderY))
            {
                interactionManager.AddClickableElement(new ClickableElement
                {
                    X = x,
                    Y = thresholdsHeaderY,
                    Width = headerClickWidth,
                    Height = 1,
                    Type = ElementType.Text,
                    Value = ToggleSectionThresholds,
                    DisplayText = "Thresholds"
                });
            }

            bool thresholdsOpen = stateManager == null || !stateManager.ThresholdsCollapsed;
            if (thresholdsOpen)
            {
                int thresholdsContentY = y;
                int barWidth = LayoutConstants.LEFT_PANEL_WIDTH - 4;
                var thresholdRender = ThresholdSectionRenderer.Render(
                    canvas,
                    x,
                    y,
                    barWidth,
                    character,
                    stateManager,
                    ThresholdBarPanel.Hero,
                    contentTop,
                    contentBottomExclusive);
                y = thresholdRender.NextY;

                if (interactionManager != null && stateManager != null)
                {
                    if (showThresholdChances && thresholdRender.ChanceHoverOrder != null)
                    {
                        for (int i = 0; i < thresholdRender.ChanceHoverOrder.Length; i++)
                            RegisterLeftPanelHoverRow(
                                x,
                                thresholdsContentY + i,
                                headerClickWidth,
                                1,
                                ThresholdChanceLabelToHoverId(thresholdRender.ChanceHoverOrder[i].Label));
                    }
                    else
                    {
                        RegisterLeftPanelHoverRow(x, thresholdsContentY, headerClickWidth, 1, "thresh:crit");
                        RegisterLeftPanelHoverRow(x, thresholdsContentY + 1, headerClickWidth, 1, "thresh:combo");
                        RegisterLeftPanelHoverRow(x, thresholdsContentY + 2, headerClickWidth, 1, "thresh:hit");
                        RegisterLeftPanelHoverRow(x, thresholdsContentY + 3, headerClickWidth, 1, "thresh:critmiss");
                        if (DiceRollThresholdRowsRenderer.CurrentRoomTriggerRoll() > 0)
                            RegisterLeftPanelHoverRow(x, thresholdsContentY + 4, headerClickWidth, 1, "thresh:room");
                    }
                }
            }
            TutorialCoachTargets.Set(TutorialCoachFocus.Thresholds, (x - 1, thresholdsHeaderY - 1, headerClickWidth + 2, y - thresholdsHeaderY + 1));
            if (thresholdsOpen)
                y += 1;
            }

            int totalContentHeight = System.Math.Max(0, y - contentOriginY);
            ScrubLeftPanelOutsideViewport(leftX, leftY, leftW, leftH, contentTop, contentBottomExclusive);
            // Border rows may have been scrubbed; redraw the frame.
            canvas.AddBorder(LayoutConstants.LEFT_PANEL_X, LayoutConstants.LEFT_PANEL_Y, LayoutConstants.LEFT_PANEL_WIDTH, LayoutConstants.LEFT_PANEL_HEIGHT, PanelJoin.Border(AsciiArtAssets.Colors.Blue));
            PanelJoin.ApplyLeftPanel(canvas);

            if (stateManager != null)
            {
                int scrollBeforeUpdate = scroll;
                stateManager.UpdateLeftPanelContentHeight(totalContentHeight);
                if (!correctingLeftPanelScroll && stateManager.LeftPanelScrollOffset != scrollBeforeUpdate)
                {
                    correctingLeftPanelScroll = true;
                    try
                    {
                        RenderCharacterPanel(character, gameState, inDungeonRun);
                    }
                    finally
                    {
                        correctingLeftPanelScroll = false;
                    }
                }
            }
        }

        /// <summary>
        /// Character roster in the left panel: every saved fighter by name and level, the active hero highlighted.
        /// Clicking empty space again returns to the hero sheet.
        /// </summary>
        private void RenderRosterNames(Character character, int x, int y, int width)
        {
            int top = LeftPanelViewport.ContentTop;
            int bottom = LeftPanelViewport.ContentBottomExclusive;
            y = top;
            PanelText(x, y, FormatLeftPanelSectionHeader("ROSTER"), AsciiArtAssets.Colors.Gold);
            y += 2;

            List<(string characterId, string characterName, int level)> saved;
            try { saved = RPGGame.CharacterSaveManager.ListAllSavedCharacters() ?? new(); }
            catch { saved = new(); }
            saved = saved.FindAll(c => !string.IsNullOrWhiteSpace(c.characterName));

            if (saved.Count == 0)
            {
                PanelText(x, y, "No saved fighters.", AsciiArtAssets.Colors.Gray);
                y++;
            }

            int listBottom = bottom - 2;
            for (int i = 0; i < saved.Count && y < listBottom; i++)
            {
                var (_, name, level) = saved[i];
                bool active = string.Equals(name, character.Name, System.StringComparison.Ordinal);
                string levelText = $"Lv {level}";
                int nameW = System.Math.Max(1, width - levelText.Length - 3);
                string shown = name.Length > nameW ? name.Substring(0, System.Math.Max(1, nameW - 1)) + "…" : name;
                string line = (active ? "> " : "  ") + shown.PadRight(nameW) + " " + levelText;
                PanelText(x, y, line, active ? AsciiArtAssets.Colors.Gold : AsciiArtAssets.Colors.White);
                y++;
            }
            if (y >= listBottom && saved.Count > 0)
            {
                int shownCount = System.Math.Max(0, listBottom - (top + 2));
                if (shownCount < saved.Count)
                    PanelText(x, listBottom, $"... +{saved.Count - shownCount} more", AsciiArtAssets.Colors.Gray);
            }

            if (bottom - 1 > y)
                PanelText(x, bottom - 1, "Click empty space: hero", AsciiArtAssets.Colors.DarkGray);
        }

        private static string ThresholdChanceLabelToHoverId(string label) =>
            label switch
            {
                "Crit" => "thresh:crit",
                "Action" => "thresh:combo",
                "Hit" => "thresh:hit",
                "Miss" => "thresh:miss",
                "Crit Miss" => "thresh:critmiss",
                DiceRollThresholdRowsRenderer.RoomTriggerLabel => "thresh:room",
                _ => "thresh:hit"
            };

        private void RegisterLeftPanelHoverRow(int x, int rowY, int width, int height, string idSuffix)
        {
            if (interactionManager == null || stateManager == null || height < 1 || width < 1)
                return;
            int clippedY = System.Math.Max(rowY, LeftPanelViewport.ContentTop);
            int clippedEnd = System.Math.Min(rowY + height, LeftPanelViewport.ContentBottomExclusive);
            if (clippedEnd <= clippedY || width < 1)
                return;
            interactionManager.AddClickableElement(new ClickableElement
            {
                X = x,
                Y = clippedY,
                Width = width,
                Height = clippedEnd - clippedY,
                Type = ElementType.Text,
                Value = LeftPanelHoverState.Prefix + idSuffix,
                DisplayText = "Left panel tooltip"
            });
        }

        private void PanelText(int x, int y, string text, Color color)
        {
            if (LeftPanelViewport.IsRowVisible(y))
                canvas.AddText(x, y, text, color);
        }

        private void PanelCharacterStat(int x, int y, string statName, int value, int maxValue, Color nameColor = default, Color valueColor = default)
        {
            if (LeftPanelViewport.IsRowVisible(y))
                canvas.AddCharacterStat(x, y, statName, value, maxValue, nameColor, valueColor);
        }

        private void ScrubLeftPanelOutsideViewport(int leftX, int leftY, int leftW, int leftH, int contentTop, int contentBottomExclusive)
        {
            int topScrubHeight = contentTop - leftY;
            if (topScrubHeight > 0)
            {
                canvas.ClearTextInArea(leftX, leftY, leftW, topScrubHeight);
                canvas.ClearProgressBarsInArea(leftX, leftY, leftW, topScrubHeight);
                canvas.ClearSegmentedBarsInArea(leftX, leftY, leftW, topScrubHeight);
            }

            // Border row plus spill below the panel (threshold/chance rows that advanced past the clip).
            int scrubBottomExclusive = System.Math.Max(leftY + leftH, LeftPanelViewport.ScrubBottomExclusive);
            int bottomScrubHeight = scrubBottomExclusive - contentBottomExclusive;
            if (bottomScrubHeight > 0)
            {
                canvas.ClearTextInArea(leftX, contentBottomExclusive, leftW, bottomScrubHeight);
                canvas.ClearProgressBarsInArea(leftX, contentBottomExclusive, leftW, bottomScrubHeight);
                canvas.ClearSegmentedBarsInArea(leftX, contentBottomExclusive, leftW, bottomScrubHeight);
            }
        }
        
        /// <summary>
        /// GEAR as item icons on a little figure (see <see cref="InventoryPaperDoll"/>) in place of the item names. On the
        /// inventory hub the slots click / drag and take bag items dropped on them. False when the panel is too narrow
        /// for the figure (the caller falls back to names) or the canvas is headless.
        /// </summary>
        private bool TryRenderGearFigure(Character character, GameState? gameState, int x, ref int y, int innerWidth)
        {
            InventoryScreenRenderer.LastPaperDollBounds = default;
            if (interactionManager == null || RPGGame.MCP.MCPMode.IsActive)
                return false;

            int tileRows = 0, tileCols = 0;
            // Largest square tiles whose figure still fits the panel width, so the icons fill the section.
            foreach (int rows in new[] { 6, 5, 4, 3, 2 })
            {
                int cols = InventoryScreenRenderer.GridTileColumns(canvas.GetCharWidth(), canvas.GetCharHeight(), rows);
                if (InventoryPaperDoll.WidthFor(cols) <= innerWidth)
                {
                    tileRows = rows;
                    tileCols = cols;
                    break;
                }
            }
            if (tileRows == 0)
                return false;

            int dollWidth = InventoryPaperDoll.WidthFor(tileCols);
            int dollHeight = InventoryPaperDoll.HeightFor(tileRows);
            int dollX = x + Math.Max(0, (innerWidth - dollWidth) / 2);
            bool inventoryInput = gameState == GameState.Inventory;
            Item? hovered = InventoryDragState.IsActive
                ? null
                : InventoryAffixSynergy.ResolveHoveredItem(LeftPanelHoverState.Value, character.Inventory, character);
            InventoryPaperDoll.Draw(canvas, interactionManager.AddClickableElement, dollX, y, tileRows, tileCols, character,
                inventoryInput, hovered, (row, h) => LeftPanelViewport.IsRangeVisible(row, h));
            if (inventoryInput)
                InventoryScreenRenderer.LastPaperDollBounds = (dollX, y, dollWidth, dollHeight);
            y += dollHeight + 1;
            return true;
        }

        /// <summary>
        /// Helper method to render a single equipment slot with consistent formatting and text wrapping
        /// Uses colored text system to show item colors based on type and modifiers
        /// </summary>
        private void RenderEquipmentSlot(int x, ref int y, int hoverWidth, string slotName, Item? item, string hoverGearId, int spacingAfter = 1)
        {
            int blockStartY = y;
            PanelText(x, y, $"{slotName}:", AsciiArtAssets.Colors.Gray);
            y++;
            
            if (item != null)
            {
                // Get colored item name segments
                var itemNameSegments = ItemDisplayColoredText.FormatFullItemName(item);
                
                // Wrap text if it's too long (max width accounts for padding: panel width - left padding - right border)
                int maxWidth = Math.Max(8, LayoutConstants.LEFT_PANEL_WIDTH - 3);
                var wrappedLines = textWriter.WrapColoredSegments(itemNameSegments, maxWidth);
                
                // Render each wrapped line with proper colors
                foreach (var lineSegments in wrappedLines)
                {
                    if (lineSegments.Count > 0 && LeftPanelViewport.IsRowVisible(y))
                        textWriter.RenderSegments(lineSegments, x, y);
                    y++;
                }
            }
            else
            {
                // Empty slot - show "None" in gray
                PanelText(x, y, "None", AsciiArtAssets.Colors.Gray);
                y++;
            }

            int contentEndY = y;
            y += spacingAfter;

            if (interactionManager != null && stateManager != null)
            {
                int h = contentEndY - blockStartY;
                if (h > 0)
                    RegisterLeftPanelHoverRow(x, blockStartY, hoverWidth, h, hoverGearId);
            }
        }

        /// <summary>
        /// Compact MATERIAL BUILDS progress under GEAR for any material with 2+ equipped pieces.
        /// </summary>
        private void RenderFormingSets(Character character, int x, ref int y, int hoverWidth)
        {
            var sets = MaterialSetController.GetFormingSets(character);
            // A learned hybrid material node merges its parents' tags, so the hybrid row replaces both parent rows.
            var mergedHybrids = HybridTrees.All.Where(h => SkillRanks.GetRank(character, h.MaterialEffect) > 0).ToList();
            var classTags = ClassTags.All
                .Where(tag => !mergedHybrids.Any(h => h.ClassA == tag || h.ClassB == tag))
                .Concat(mergedHybrids.Select(h => h.Tag))
                .Select(tag => (Tag: tag, Count: ClassTags.Count(character, tag)))
                .Where(e => e.Count > 0)
                .ToList();
            if (sets.Count == 0 && classTags.Count == 0)
                return;

            PanelText(x, y, "Sets:", AsciiArtAssets.Colors.Gray);
            y++;

            const int maxWidth = 29;
            foreach (var (material, count) in sets)
            {
                int rowY = y;
                string line = MaterialSetController.FormatFormingSetHudLine(material, count);
                if (line.Length > maxWidth)
                    line = line.Substring(0, maxWidth - 3) + "...";
                PanelText(x, y, line, AsciiArtAssets.Colors.Cyan);
                y++;
                RegisterLeftPanelHoverRow(x, rowY, hoverWidth, 1, "set:" + material);
            }

            // Class tag totals (gear tags + class materials after the Core root + strip actions), as the cadence rules count them.
            foreach (var (tag, count) in classTags)
            {
                int rowY = y;
                PanelText(x, y, $"{char.ToUpperInvariant(tag[0])}{tag.Substring(1)} tags {count}", AsciiArtAssets.Colors.Gold);
                y++;
                RegisterLeftPanelHoverRow(x, rowY, hoverWidth, 1, "classtag:" + tag);
            }

            y++;
        }

        /// <summary>Animal tag ladder progress under GEAR (Animals / taxon / specific).</summary>
        private void RenderAnimalSets(Character character, int x, ref int y, int hoverWidth)
        {
            var sets = AnimalSetController.GetFormingSets(character);
            if (sets.Count == 0)
                return;

            PanelText(x, y, "Animals:", AsciiArtAssets.Colors.Gray);
            y++;

            const int maxWidth = 29;
            foreach (var (label, _) in sets)
            {
                int rowY = y;
                string line = label;
                if (line.Length > maxWidth)
                    line = line.Substring(0, maxWidth - 3) + "...";
                PanelText(x, y, line, AsciiArtAssets.Colors.Green);
                y++;
                RegisterLeftPanelHoverRow(x, rowY, hoverWidth, 1, "animal:" + label);
            }

            y++;
        }
        
        /// <summary>
        /// Renders an empty character panel when no character is loaded
        /// </summary>
        public void RenderEmptyCharacterPanel()
        {
            int leftX = LayoutConstants.LEFT_PANEL_X;
            int leftY = LayoutConstants.LEFT_PANEL_Y;
            int leftW = LayoutConstants.LEFT_PANEL_WIDTH;
            int leftH = LayoutConstants.LEFT_PANEL_HEIGHT + 1;
            canvas.ClearTextInArea(leftX, leftY, leftW, leftH);
            canvas.ClearBoxesInArea(leftX, leftY, leftW, leftH);

            canvas.AddBorder(LayoutConstants.LEFT_PANEL_X, LayoutConstants.LEFT_PANEL_Y, LayoutConstants.LEFT_PANEL_WIDTH, LayoutConstants.LEFT_PANEL_HEIGHT, AsciiArtAssets.Colors.Gray);
            
            int y = LayoutConstants.LEFT_PANEL_Y + LayoutConstants.LEFT_PANEL_HEIGHT / 2;
            int x = LayoutConstants.LEFT_PANEL_X + 2; // Reduced from +6 since border now starts at 0
            
            canvas.AddText(x, y, "No Character", AsciiArtAssets.Colors.Gray);
            canvas.AddText(x, y + 1, "Loaded", AsciiArtAssets.Colors.Gray);
        }
        
        /// <summary>
        /// Gets a formatted string displaying all class points the character has
        /// Returns empty string if character has no class points
        /// </summary>
        private string GetClassPointsDisplay(Character character)
        {
            var classPoints = new List<string>();
            
            if (character.BarbarianPoints > 0)
                classPoints.Add($"Barb: {character.BarbarianPoints}");
            if (character.WarriorPoints > 0)
                classPoints.Add($"War: {character.WarriorPoints}");
            if (character.RoguePoints > 0)
                classPoints.Add($"Rog: {character.RoguePoints}");
            if (character.WizardPoints > 0)
                classPoints.Add($"Wiz: {character.WizardPoints}");
            
            return classPoints.Count > 0 ? string.Join(" | ", classPoints) : "";
        }
        
        /// <summary>
        /// Stat row highlight for +3 level-up target: follows equipped weapon type (same as <see cref="CharacterStats.LevelUp"/>), not class points or display title.
        /// </summary>
        private static string GetPrimaryStatForCharacter(Character character)
        {
            if (character.Weapon is WeaponItem wi)
                return CharacterStats.GetPrimaryStatLabelForWeapon(wi.WeaponType);
            return "";
        }
        
        /// <summary>
        /// Pads <c>NAME:</c> so the value starts at character column 9 (same as Damage / Armor in this panel).
        /// Single-line text avoids <see cref="CanvasElementBuilder.AddCharacterStat"/> with value 0 plus a partial overlay, which left a stray trailing <c>0</c>.
        /// </summary>
        private static string FormatSecondaryStatLine(string statName, string valueText)
        {
            const int valueColumn = 9;
            string label = statName + ":";
            int pad = valueColumn - label.Length;
            if (pad < 0)
                pad = 0;
            return label + new string(' ', pad) + valueText;
        }

        /// <summary>
        /// Renders secondary stats (magic find, HP regen, etc.). When <paramref name="expandedHoverTargets"/> is non-null,
        /// appends (row Y, hover id) for each row; the caller registers <c>lphover:*</c> clickables for tooltips.
        /// </summary>
        private void RenderExpandedStats(Character character, int x, ref int y, List<(int rowY, string idSuffix)>? expandedHoverTargets)
        {
            var cyan = AsciiArtAssets.Colors.Cyan;

            // Magic Find (only if > 0)
            int magicFind = character.GetMagicFind();
            if (magicFind > 0)
            {
                int rowY = y;
                PanelText(x, y, FormatSecondaryStatLine("MAG FIND", $"+{magicFind}"), cyan);
                y++;
                expandedHoverTargets?.Add((rowY, "stat:magfind"));
            }

            // Health Regen (only if > 0)
            int healthRegen = character.GetEquipmentHealthRegenBonus();
            if (healthRegen > 0)
            {
                int rowY = y;
                PanelText(x, y, FormatSecondaryStatLine("HP REGEN", $"+{healthRegen} per turn"), cyan);
                y++;
                expandedHoverTargets?.Add((rowY, "stat:hpregen"));
            }

            // Lifesteal (only if > 0)
            double lifesteal = character.GetModificationLifesteal();
            if (lifesteal > 0)
            {
                int rowY = y;
                PanelText(x, y, FormatSecondaryStatLine("LIFESTEAL", $"{lifesteal:P0}"), cyan);
                y++;
                expandedHoverTargets?.Add((rowY, "stat:lifesteal"));
            }

            int bleedOnHit = character.GetWeaponBleedPerHit();
            if (bleedOnHit > 0)
            {
                int rowY = y;
                PanelText(x, y, FormatSecondaryStatLine("BLEED", $"+{bleedOnHit}"), cyan);
                y++;
                expandedHoverTargets?.Add((rowY, "stat:bleed"));
            }

            int burnOnHit = character.GetWeaponBurnPerHit();
            if (burnOnHit > 0)
            {
                int rowY = y;
                PanelText(x, y, FormatSecondaryStatLine("BURN", $"+{burnOnHit}"), cyan);
                y++;
                expandedHoverTargets?.Add((rowY, "stat:burn"));
            }

            double poisonOnHit = character.GetWeaponPoisonPercentPerHit();
            if (poisonOnHit > 0)
            {
                int rowY = y;
                PanelText(x, y, FormatSecondaryStatLine("POISON", $"+{poisonOnHit:0.#}%"), cyan);
                y++;
                expandedHoverTargets?.Add((rowY, "stat:poison"));
            }
        }
    }
}

