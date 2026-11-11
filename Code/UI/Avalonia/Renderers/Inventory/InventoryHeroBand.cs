namespace RPGGame.UI.Avalonia.Renderers.Inventory
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using global::Avalonia.Media;
    using RPGGame;
    using RPGGame.Combat.Calculators;
    using RPGGame.UI;
    using RPGGame.UI.Avalonia;
    using RPGGame.UI.Avalonia.Feedback;
    using RPGGame.UI.Avalonia.Layout;
    using RPGGame.UI.Avalonia.Managers;

    /// <summary>
    /// Inventory hub: the band along the bottom of the center column (where the action strip sits elsewhere) shows the
    /// hero's STATS and THRESHOLDS side by side. Actions live on their own hub tab. While the band is shown the
    /// left panel leaves those sections (and its set / class-tag lines) out, and the strip's hover / drag / right-click hit areas are off.
    /// Rows register <c>lphover:*</c> targets, so they share the left panel's tooltips.
    /// </summary>
    public static class InventoryHeroBand
    {
        private static bool _requested;

        /// <summary>True while the current screen draws this band in place of the action strip (set per layout pass).</summary>
        public static bool IsShown { get; private set; }

        /// <summary>Called by a screen before its layout pass to ask for the band.</summary>
        public static void Request() => _requested = true;

        /// <summary>Layout pass start: the band shows only when the screen asked for it on the windowed hub.</summary>
        public static void SyncForLayoutPass()
        {
            IsShown = _requested && HubTabBar.IsActive && LayoutConstants.ActionStripAtBottom;
            _requested = false;
            SplitTopY = null;
            ListColumn = null;
        }

        /// <summary>
        /// Set by the inventory screen when the center frame runs the full column height: the item list stops above this
        /// row, the description pane runs to the frame bottom, and the band draws under the list only.
        /// </summary>
        public static int? SplitTopY { get; set; }

        /// <summary>Columns the band takes in split mode (the list side, left of the description separator); set by the list renderer.</summary>
        public static (int X, int Width)? ListColumn { get; set; }

        private const int ColumnGap = 1;
        private const int ValueColumn = 9;

        public static void Render(GameCanvasControl canvas, Action<ClickableElement>? addClickable, Character? character, StatsPanelStateManager? stateManager)
        {
            if (SplitTopY is int splitTop)
            {
                // No list / description split this frame (empty bag, old list view): the band spans the frame's inner width.
                var (listX, listW) = ListColumn ?? (LayoutConstants.CENTER_PANEL_X + 1, Math.Max(1, LayoutConstants.CENTER_PANEL_WIDTH - 2));
                RenderBesideDetail(canvas, addClickable, character, stateManager, listX, splitTop, listW);
                return;
            }

            int bandX = LayoutConstants.ACTION_INFO_X;
            int bandY = LayoutConstants.ACTION_INFO_Y;
            int bandW = LayoutConstants.ACTION_INFO_WIDTH;
            int bandH = LayoutConstants.ACTION_INFO_HEIGHT;
            canvas.ClearTextInArea(bandX, bandY, bandW, bandH);
            canvas.ClearBoxesInArea(bandX, bandY, bandW, bandH);
            if (character == null || bandW < 12 || bandH < 4)
                return;

            int edge = LayoutConstants.ACTION_INFO_PANEL_EDGE_MARGIN;
            int avail = Math.Max(0, bandW - 2 * edge - ColumnGap);
            int statsW = avail * 58 / 100;
            int threshW = avail - statsW;
            int statsX = bandX + edge;
            int threshX = statsX + statsW + ColumnGap;

            var border = ActionInfoStripLayout.DarkenRgb(AsciiArtAssets.Colors.NeutralGray70, 0.5);
            int titleY = bandY + 1;
            int firstRow = bandY + 2;
            int endRow = bandY + bandH - 1; // exclusive (bottom border row)

            var hover = MakeHover(addClickable, firstRow, endRow);

            // --- STATS ---
            canvas.AddBorder(statsX, bandY, statsW, bandH, border);
            canvas.AddText(statsX + 2, titleY, UIConstants.Headers.Stats, AsciiArtAssets.Colors.Gold);
            RenderStats(canvas, character, stateManager, statsX + 2, statsW - 4, firstRow, endRow, hover);

            // --- THRESHOLDS / CHANCES ---
            canvas.AddBorder(threshX, bandY, threshW, bandH, border);
            bool chances = (stateManager?.ThresholdsHudMode ?? ThresholdsHudMode.Ladder) == ThresholdsHudMode.Chances;
            canvas.AddText(threshX + 2, titleY, chances ? UIConstants.Headers.Chances : UIConstants.Headers.Thresholds, AsciiArtAssets.Colors.Gold);
            RenderThresholds(canvas, character, stateManager, threshX + 2, threshW - 4, firstRow, endRow, chances, hover);
            TutorialCoachTargets.Set(TutorialCoachFocus.Thresholds, (threshX, bandY, threshW, bandH));
        }

        /// <summary>
        /// Split layout: inside the full-height center frame, under the item list and left of the description pane.
        /// A rule across the top, then STATS and THRESHOLDS side by side under "==== X ====" headers (no boxes).
        /// </summary>
        private static void RenderBesideDetail(GameCanvasControl canvas, Action<ClickableElement>? addClickable, Character? character,
            StatsPanelStateManager? stateManager, int bandX, int bandY, int bandW)
        {
            // Frame bottom border row (exclusive end of content).
            int endRow = LayoutConstants.CENTER_COLUMN_FULL_Y + LayoutConstants.CENTER_COLUMN_FULL_HEIGHT - 1;
            int bandH = Math.Max(0, endRow - bandY);
            canvas.ClearTextInArea(bandX, bandY, bandW, bandH);
            canvas.ClearProgressBarsInArea(bandX, bandY, bandW, bandH);
            if (character == null || bandW < 12 || bandH < 4)
                return;

            var rule = ActionInfoStripLayout.DarkenRgb(AsciiArtAssets.Colors.NeutralGray70, 0.5);
            canvas.AddText(bandX + 1, bandY, new string('─', Math.Max(0, bandW - 2)), rule);

            int inner = Math.Max(0, bandW - 4);
            int statsW = inner * 55 / 100;
            int statsX = bandX + 2;
            int threshX = statsX + statsW + 2;
            int threshW = Math.Max(1, bandX + bandW - 2 - threshX);

            int titleY = bandY + 1;
            // A blank row under the headers when the band has room for it.
            int firstRow = bandY + (bandH >= 9 ? 3 : 2);
            var hover = MakeHover(addClickable, firstRow, endRow);

            canvas.AddText(statsX, titleY, AsciiArtAssets.UIText.CreateHeader(UIConstants.Headers.Stats), AsciiArtAssets.Colors.Gold);
            RenderStats(canvas, character, stateManager, statsX, statsW, firstRow, endRow, hover);

            bool chances = (stateManager?.ThresholdsHudMode ?? ThresholdsHudMode.Ladder) == ThresholdsHudMode.Chances;
            canvas.AddText(threshX, titleY, AsciiArtAssets.UIText.CreateHeader(chances ? UIConstants.Headers.Chances : UIConstants.Headers.Thresholds), AsciiArtAssets.Colors.Gold);
            RenderThresholds(canvas, character, stateManager, threshX, threshW, firstRow, endRow, chances, hover);
            TutorialCoachTargets.Set(TutorialCoachFocus.Thresholds, (threshX - 1, bandY, threshW + 2, bandH));
        }

        private static Action<int, int, int, string> MakeHover(Action<ClickableElement>? addClickable, int firstRow, int endRow) =>
            (x, y, w, id) =>
            {
                if (addClickable == null || w < 1 || y < firstRow || y >= endRow)
                    return;
                addClickable(new ClickableElement
                {
                    X = x,
                    Y = y,
                    Width = w,
                    Height = 1,
                    Type = ElementType.Text,
                    Value = LeftPanelHoverState.Prefix + id,
                    DisplayText = "Hero stat tooltip"
                });
            };

        private static string Line(string label, string value)
        {
            string l = label + ":";
            return l + new string(' ', Math.Max(1, ValueColumn - l.Length)) + value;
        }

        private static string Fit(string text, int width)
        {
            if (width <= 0)
                return "";
            if (text.Length <= width)
                return text;
            return width <= 3 ? text.Substring(0, width) : text.Substring(0, width - 3) + "...";
        }

        private static void RenderStats(GameCanvasControl canvas, Character character, StatsPanelStateManager? stateManager,
            int x, int width, int firstRow, int endRow, Action<int, int, int, string> hover)
        {
            var white = AsciiArtAssets.Colors.White;
            var cyan = AsciiArtAssets.Colors.Cyan;
            int colW = Math.Max(1, (width - 2) / 2);
            int col2X = x + colW + 2;

            // Left column: combat numbers, then any secondary stats that are in play.
            var combat = new List<(string Text, Color Color, string Id)>
            {
                (Line("Damage", character.Combat.GetTotalDamage().ToString()), white, "stat:damage"),
                (Line("Speed", $"{character.GetTotalAttackSpeed():F2}s"), white, "stat:speed"),
                (Line("AMP", $"{character.GetComboAmplifier():F2}x"), white, "stat:amp"),
                (Line("Defense", ClassDefenseCalculator.GetDisplayedDefense(character).ToString()), white, "stat:armor"),
            };
            int magicFind = character.GetMagicFind();
            if (magicFind > 0) combat.Add((Line("MAG FIND", $"+{magicFind}"), cyan, "stat:magfind"));
            int regen = character.GetEquipmentHealthRegenBonus();
            if (regen > 0) combat.Add((Line("HP REGEN", $"+{regen}/turn"), cyan, "stat:hpregen"));
            double lifesteal = character.GetModificationLifesteal();
            if (lifesteal > 0) combat.Add((Line("LIFESTEAL", $"{lifesteal:P0}"), cyan, "stat:lifesteal"));
            int bleed = character.GetWeaponBleedPerHit();
            if (bleed > 0) combat.Add((Line("BLEED", $"+{bleed}"), cyan, "stat:bleed"));
            int burn = character.GetWeaponBurnPerHit();
            if (burn > 0) combat.Add((Line("BURN", $"+{burn}"), cyan, "stat:burn"));
            double poison = character.GetWeaponPoisonPercentPerHit();
            if (poison > 0) combat.Add((Line("POISON", $"+{poison:0.#}%"), cyan, "stat:poison"));

            int y = firstRow;
            foreach (var (text, color, id) in combat)
            {
                if (y >= endRow)
                    break;
                canvas.AddText(x, y, Fit(text, colW), color);
                hover(x, y, colW, id);
                y++;
            }

            // Right column: attributes (primary stat highlighted, hybrid attribute after a gap).
            string primary = character.Weapon is WeaponItem wi ? CharacterStats.GetPrimaryStatLabelForWeapon(wi.WeaponType) : "";
            var highlight = PrimaryStatRowHighlightColors.ForCharacter(character);
            bool sort = stateManager?.SortAttributesByValue == true;
            int col2W = Math.Max(1, x + width - col2X);
            y = firstRow;
            foreach (var row in LeftPanelAttributeOrder.Build(character, sort))
            {
                if (row.IsHybrid)
                    y++;
                if (y >= endRow)
                    break;
                var color = row.IsHybrid || primary == row.PrimaryName ? highlight : white;
                canvas.AddText(col2X, y, Fit(Line(row.Label, row.Value.ToString()), col2W), color);
                hover(col2X, y, col2W, row.HoverId);
                y++;
            }
        }

        private static void RenderThresholds(GameCanvasControl canvas, Character character, StatsPanelStateManager? stateManager,
            int x, int width, int firstRow, int endRow, bool chances, Action<int, int, int, string> hover)
        {
            var result = ThresholdSectionRenderer.Render(canvas, x, firstRow, width, character, stateManager,
                ThresholdBarPanel.Hero, firstRow, endRow);

            if (chances && result.ChanceHoverOrder != null)
            {
                for (int i = 0; i < result.ChanceHoverOrder.Length; i++)
                    hover(x, firstRow + i, width, ChanceLabelToHoverId(result.ChanceHoverOrder[i].Label));
                return;
            }
            hover(x, firstRow, width, "thresh:crit");
            hover(x, firstRow + 1, width, "thresh:combo");
            hover(x, firstRow + 2, width, "thresh:hit");
            hover(x, firstRow + 3, width, "thresh:critmiss");
            if (DiceRollThresholdRowsRenderer.CurrentRoomTriggerRoll() > 0)
                hover(x, firstRow + 4, width, "thresh:room");
        }

        private static string ChanceLabelToHoverId(string label) => label switch
        {
            "Crit" => "thresh:crit",
            "Action" => "thresh:combo",
            "Hit" => "thresh:hit",
            "Miss" => "thresh:miss",
            "Crit Miss" => "thresh:critmiss",
            DiceRollThresholdRowsRenderer.RoomTriggerLabel => "thresh:room",
            _ => "thresh:hit"
        };
    }
}
