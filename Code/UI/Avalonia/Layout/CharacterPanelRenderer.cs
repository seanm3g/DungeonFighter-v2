using System;
using System.Collections.Generic;
using Avalonia.Media;
using RPGGame;
using RPGGame.ActionInteractionLab;
using RPGGame.Combat.Calculators;
using RPGGame.UI;
using RPGGame.UI.Avalonia;
using RPGGame.UI.Avalonia.Feedback;
using RPGGame.UI.Avalonia.Renderers;
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
            int leftH = LayoutConstants.LEFT_PANEL_HEIGHT + 1;
            canvas.ClearTextInArea(leftX, leftY, leftW, leftH);
            canvas.ClearProgressBarsInArea(leftX, leftY, leftW, leftH);
            canvas.ClearSegmentedBarsInArea(leftX, leftY, leftW, leftH);
            canvas.ClearBoxesInArea(leftX, leftY, leftW, leftH);

            // Main border for character panel - starts at X=0 with no padding
            canvas.AddBorder(LayoutConstants.LEFT_PANEL_X, LayoutConstants.LEFT_PANEL_Y, LayoutConstants.LEFT_PANEL_WIDTH, LayoutConstants.LEFT_PANEL_HEIGHT, AsciiArtAssets.Colors.Blue);

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

            int y = contentTop - scroll;
            int contentOriginY = y;
            int x = LayoutConstants.LEFT_PANEL_X + 2; // Reduced from +4 since border now starts at 0
            int headerClickWidth = LayoutConstants.LEFT_PANEL_WIDTH - 4;
            
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

            // --- STATS --- (static gold like HERO/GEAR; no glow — glow animation shifted hue vs other headers)
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
                int totalDamage = character.GetAttributeDamageBonus() + weaponDamage + equipmentDamageBonus + modificationDamageBonus;
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

                int strRowY = y;
                PanelCharacterStat(x, y, "STR", character.GetEffectiveStrength(), 0,
                    primaryStat == "Strength" ? primaryStatHighlight : AsciiArtAssets.Colors.White);
                y++;
                int agiRowY = y;
                PanelCharacterStat(x, y, "AGI", character.GetEffectiveAgility(), 0,
                    primaryStat == "Agility" ? primaryStatHighlight : AsciiArtAssets.Colors.White);
                y++;
                int tecRowY = y;
                PanelCharacterStat(x, y, "TECH", character.GetEffectiveTechnique(), 0,
                    primaryStat == "Technique" ? primaryStatHighlight : AsciiArtAssets.Colors.White);
                y++;
                int intRowY = y;
                PanelCharacterStat(x, y, "INT", character.GetEffectiveIntelligence(), 0,
                    primaryStat == "Intelligence" ? primaryStatHighlight : AsciiArtAssets.Colors.White);
                y++;

                int naivete = NaiveteBalanceHelper.GetDisplayNaivete(character);
                int naivRowY = -1;
                if (naivete > 0)
                {
                    naivRowY = y;
                    var naivColor = AsciiArtAssets.Colors.Cyan;
                    PanelCharacterStat(x, y, "NAIV", naivete, 0, naivColor, naivColor);
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
                    RegisterLeftPanelHoverRow(x, strRowY, headerClickWidth, 1, "stat:str");
                    RegisterLeftPanelHoverRow(x, agiRowY, headerClickWidth, 1, "stat:agi");
                    RegisterLeftPanelHoverRow(x, tecRowY, headerClickWidth, 1, "stat:tec");
                    RegisterLeftPanelHoverRow(x, intRowY, headerClickWidth, 1, "stat:int");
                    if (naivRowY >= 0)
                        RegisterLeftPanelHoverRow(x, naivRowY, headerClickWidth, 1, "stat:naivete");
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

                if (stateManager == null || !stateManager.GearCollapsed)
                {
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

            // --- STATUS EFFECTS --- (hero; above thresholds; dungeon run + Action Lab; enemy effects are on the right panel)
            if (LeftPanelSectionVisibility.ShowStatusEffects(gameState, inDungeonRun))
            {
                PanelText(x, y, FormatLeftPanelSectionHeader(UIConstants.Headers.StatusEffects), AsciiArtAssets.Colors.Gold);
                y += 2;
                const int maxHeroEffectLines = 5;
                const int maxHeroLineLen = 29;
                var heroEffects = StatusEffectDisplayLines.Build(character, character);
                if (heroEffects.Count > 0)
                {
                    for (int i = 0; i < Math.Min(heroEffects.Count, maxHeroEffectLines); i++)
                    {
                        int effectRowY = y;
                        string line = heroEffects[i];
                        if (line.Length > maxHeroLineLen)
                            line = line.Substring(0, maxHeroLineLen - 3) + "...";
                        PanelText(x, y, line, AsciiArtAssets.Colors.White);
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
                y += 1;
            }

            // --- THRESHOLDS / CHANCES --- (ladder numbers or exclusive d20 %; bar is under health)
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
                    canvas, x, y, barWidth, character, stateManager, ThresholdBarPanel.Hero);
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
                    }
                }
            }
            if (thresholdsOpen)
                y += 1;

            int totalContentHeight = System.Math.Max(0, y - contentOriginY);
            ScrubLeftPanelOutsideViewport(leftX, leftY, leftW, leftH, contentTop, contentBottomExclusive);
            // Border rows may have been scrubbed; redraw the frame.
            canvas.AddBorder(LayoutConstants.LEFT_PANEL_X, LayoutConstants.LEFT_PANEL_Y, LayoutConstants.LEFT_PANEL_WIDTH, LayoutConstants.LEFT_PANEL_HEIGHT, AsciiArtAssets.Colors.Blue);

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

        private static string ThresholdChanceLabelToHoverId(string label) =>
            label switch
            {
                "Crit" => "thresh:crit",
                "Combo" => "thresh:combo",
                "Hit" => "thresh:hit",
                "Miss" => "thresh:miss",
                "Crit Miss" => "thresh:critmiss",
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

            int bottomScrubHeight = (leftY + leftH) - contentBottomExclusive;
            if (bottomScrubHeight > 0)
            {
                canvas.ClearTextInArea(leftX, contentBottomExclusive, leftW, bottomScrubHeight);
                canvas.ClearProgressBarsInArea(leftX, contentBottomExclusive, leftW, bottomScrubHeight);
                canvas.ClearSegmentedBarsInArea(leftX, contentBottomExclusive, leftW, bottomScrubHeight);
            }
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
                // Panel width is 32, left padding is 2, right border is 1, so available width is 29
                const int maxWidth = 29; // Panel width (32) - left padding (2) - right border (1) = 29
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
            if (sets.Count == 0)
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

