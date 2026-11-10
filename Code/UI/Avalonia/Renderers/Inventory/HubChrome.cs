namespace RPGGame.UI.Avalonia.Renderers.Inventory
{
    using System;
    using System.Collections.Generic;
    using global::Avalonia.Media;
    using RPGGame;
    using RPGGame.Handlers.Inventory;
    using RPGGame.UI;
    using RPGGame.UI.Avalonia;
    using RPGGame.UI.Avalonia.Layout;
    using RPGGame.UI.Avalonia.Renderers.Helpers;
    using RPGGame.UI.ColorSystem.Themes;

    /// <summary>The hub's center-panel tabs. The side panels stay put; only the center changes.</summary>
    public enum HubTab
    {
        Inventory,
        Skills,
        Actions,
        TradeUp
    }

    /// <summary>
    /// Tab strip along the top of the hub's center panel (Inventory / Skills / Actions / Trade Up).
    /// Clicking a tab sends <c>hub:tab:{name}</c>; letter keys I / K / E / T do the same from the inventory.
    /// </summary>
    public static class HubTabBar
    {
        public const string TokenPrefix = "hub:tab:";
        public const string InventoryToken = TokenPrefix + "inventory";
        public const string SkillsToken = TokenPrefix + "skills";
        public const string ActionsToken = TokenPrefix + "actions";
        public const string TradeUpToken = TokenPrefix + "tradeup";

        private static readonly (HubTab Tab, string Label, string Token)[] Tabs =
        {
            (HubTab.Inventory, "INVENTORY", InventoryToken),
            (HubTab.Skills, "SKILLS", SkillsToken),
            (HubTab.Actions, "ACTIONS", ActionsToken),
            (HubTab.TradeUp, "TRADE UP", TradeUpToken),
        };

        /// <summary>True when the hub chrome (tabs, Dungeon Select button, escape menu) is in use: the windowed game, not an MCP agent.</summary>
        public static bool IsActive => !RPGGame.MCP.MCPMode.IsActive;

        public static HubTab? ParseToken(string? input) => input switch
        {
            InventoryToken => HubTab.Inventory,
            SkillsToken => HubTab.Skills,
            ActionsToken => HubTab.Actions,
            TradeUpToken => HubTab.TradeUp,
            _ => null
        };

        /// <summary>Columns the strip takes (labels plus separators).</summary>
        public static int Width
        {
            get
            {
                int w = 0;
                foreach (var t in Tabs)
                    w += t.Label.Length + 4 + 1;
                return w - 1;
            }
        }

        /// <summary>
        /// Draws the strip with its labels on row <paramref name="y"/> as folder tabs: each tab is outlined from half a row
        /// above the labels down to a baseline in the middle of row <c>y + 1</c> (so callers keep that row free), and the
        /// baseline runs edge to edge of the center frame except under the active tab, which opens into the page below.
        /// Row <c>y - 1</c> is the frame's top row. While <paramref name="locked"/> (a trade-up that must finish) the other
        /// tabs are dimmed and not clickable. <paramref name="joinedFrame"/>: the frame was widened by
        /// <see cref="PanelJoin.ApplyCenter"/>. With <paramref name="character"/> given, the Trade Up tab is dimmed and not
        /// clickable while the bag lacks 5 unlocked items of one tradeable rarity.
        /// </summary>
        public static void Draw(GameCanvasControl canvas, Action<ClickableElement> addClickable, int x, int y, int maxWidth, HubTab active,
            bool locked = false, Color? frameColor = null, bool joinedFrame = true, Character? character = null)
        {
            bool tradeUpDisabled = character != null && active != HubTab.TradeUp
                && !InventoryTradeUpHandler.CanTradeUp(character.Inventory);
            Color frame = frameColor ?? PanelJoin.Border(AsciiArtAssets.Colors.Cyan);
            double baseY = y + 1.5;
            double frameLeft = joinedFrame ? PanelJoin.CenterFrameLeft : LayoutConstants.CENTER_PANEL_X;
            double frameRight = joinedFrame ? PanelJoin.CenterFrameRight : LayoutConstants.CENTER_PANEL_X + LayoutConstants.CENTER_PANEL_WIDTH;
            // Lines persist until cleared (screens that skip a full canvas clear redraw the strip each frame).
            canvas.ClearLinesInArea(LayoutConstants.CENTER_PANEL_X - 2, y - 1, LayoutConstants.CENTER_PANEL_WIDTH + 4, 3);

            double? openLeft = null, openRight = null;
            int cx = x;
            int right = x + Math.Max(0, maxWidth);
            foreach (var (tab, label, token) in Tabs)
            {
                bool isActive = tab == active;
                bool disabled = locked || (tradeUpDisabled && tab == HubTab.TradeUp);
                string text = $"  {label}  ";
                if (cx + text.Length > right)
                    break;
                Color color = isActive
                    ? AsciiArtAssets.Colors.Gold
                    : disabled ? AsciiArtAssets.Colors.DarkGray : AsciiArtAssets.Colors.Gray;
                canvas.AddText(cx, y, text, color);
                // The active tab stands a little taller, in the frame's color, as the front layer; the others sit behind it.
                DrawTabOutline(canvas, cx, cx + text.Length, isActive ? y - 0.5 : y - 0.25, baseY,
                    isActive ? frame : AsciiArtAssets.Colors.DarkGray);
                if (isActive)
                {
                    openLeft = cx;
                    openRight = cx + text.Length;
                }
                if (!isActive && !disabled)
                {
                    // Hit box covers the whole drawn tab (outline top in row y - 1 down to the baseline in row y + 1)
                    // plus the 1-column gap to the next tab. No hover glow: the tab strip stays still under the mouse.
                    addClickable(new ClickableElement
                    {
                        X = cx,
                        Y = y - 1,
                        Width = text.Length + 1,
                        Height = 3,
                        Type = ElementType.MenuOption,
                        Value = token,
                        DisplayText = label,
                        NoHoverGlow = true
                    });
                }
                cx += text.Length + 1;
            }

            if (openLeft is double gapLeft && openRight is double gapRight)
            {
                canvas.AddLine(frameLeft, baseY, gapLeft, baseY, frame, LineThickness);
                canvas.AddLine(gapRight, baseY, frameRight, baseY, frame, LineThickness);
            }
            else
                canvas.AddLine(frameLeft, baseY, frameRight, baseY, frame, LineThickness);
        }

        private const double LineThickness = 1.0;

        /// <summary>Sides and top of one tab (top corners clipped), open at the bottom.</summary>
        private static void DrawTabOutline(GameCanvasControl canvas, double left, double right, double top, double bottom, Color color)
        {
            const double cornerX = 0.5;
            const double cornerY = 0.25;
            canvas.AddLine(left, bottom, left, top + cornerY, color, LineThickness);
            canvas.AddLine(left, top + cornerY, left + cornerX, top, color, LineThickness);
            canvas.AddLine(left + cornerX, top, right - cornerX, top, color, LineThickness);
            canvas.AddLine(right - cornerX, top, right, top + cornerY, color, LineThickness);
            canvas.AddLine(right, top + cornerY, right, bottom, color, LineThickness);
        }
    }

    /// <summary>
    /// Esc menu over the hub: Resume, Settings, Save &amp; Main Menu, Quit. While open the inventory center panel shows only
    /// this box and other inventory input is ignored.
    /// </summary>
    public static class HubEscapeMenu
    {
        public const string ResumeToken = "hub:esc:resume";
        public const string SnapshotToken = "hub:esc:snapshot";

        public static bool IsOpen { get; set; }

        /// <summary>Menu rows: label and the input each sends (number keys 1-5 pick them too).</summary>
        public static readonly (string Label, string Token)[] Options =
        {
            ("Resume", ResumeToken),
            ("Settings", InventoryMenuHandler.HubSettings),
            ("Action Lab Snapshot", SnapshotToken),
            ("Save & Main Menu", InventoryMenuHandler.HubMainMenu),
            ("Quit Game", InventoryMenuHandler.HubQuit),
        };

        /// <summary>Draws the menu box centered in the given area.</summary>
        public static void Draw(GameCanvasControl canvas, List<ClickableElement> clickables, int x, int y, int width, int height)
        {
            const int boxW = 34;
            const int buttonH = 3;
            int boxH = 4 + Options.Length * (buttonH + 1) + 1;
            int bx = x + Math.Max(0, (width - boxW) / 2);
            int by = y + Math.Max(0, (height - boxH) / 2);
            canvas.ClearTextInArea(bx, by, boxW, boxH);
            canvas.AddBox(bx, by, boxW, boxH, AsciiArtAssets.Colors.Gold, AsciiArtAssets.Colors.Black, borderThicknessPixels: 2);
            const string title = "MENU";
            canvas.AddText(bx + (boxW - title.Length) / 2, by + 1, title, AsciiArtAssets.Colors.Gold);
            int rowY = by + 3;
            for (int i = 0; i < Options.Length; i++)
            {
                var (label, token) = Options[i];
                BoxButton.Draw(canvas, clickables, bx + 3, rowY, boxW - 6, buttonH, $"{i + 1}  {label}", token);
                rowY += buttonH + 1;
            }
            const string hint = "Esc: resume";
            canvas.AddText(bx + (boxW - hint.Length) / 2, by + boxH - 1, hint, AsciiArtAssets.Colors.DarkGray);
        }
    }

    /// <summary>
    /// Worn gear as a little figure of item icons (head over body, weapon and charm as arms, legs, feet). Drawn in the
    /// left panel's GEAR section. On the inventory hub its slots click / drag like bag items (<c>invslot:</c>) and a bag
    /// item dropped on it equips; elsewhere the slots only hover the gear tooltip.
    /// </summary>
    public static class InventoryPaperDoll
    {
        /// <summary>Columns between the arm slots and the body (drawn as <c>──</c>).</summary>
        public const int ArmGap = 2;

        /// <summary>Equipped slot the inventory description pane shows (gold frame); set by the inventory screen state.</summary>
        public static string? SelectedSlot { get; set; }

        private static readonly (string Slot, string Label, int Col, int Row)[] Slots =
        {
            ("head", "Head", 1, 0),
            ("weapon", "Weapon", 0, 1),
            ("body", "Body", 1, 1),
            ("charm", "Charm", 2, 1),
            ("legs", "Legs", 1, 2),
            ("feet", "Feet", 1, 3),
        };

        public static int WidthFor(int tileCols) => 3 * tileCols + 2 * ArmGap;

        public static int HeightFor(int tileRows) => 4 * tileRows + 3;

        /// <summary>
        /// Draws the figure with its top-left at (<paramref name="dollX"/>, <paramref name="dollTop"/>).
        /// <paramref name="isVisible"/> (row, height) skips tiles clipped by a scrolling panel.
        /// </summary>
        public static void Draw(
            GameCanvasControl canvas,
            Action<ClickableElement> addClickable,
            int dollX,
            int dollTop,
            int tileRows,
            int tileCols,
            Character character,
            bool inventoryInput,
            Item? hoveredItem,
            Func<int, int, bool> isVisible)
        {
            int stepX = tileCols + ArmGap;
            int stepY = tileRows + 1;
            var line = AsciiArtAssets.Colors.DarkGray;

            int spineX = dollX + stepX + tileCols / 2;
            for (int row = 0; row < 3; row++)
            {
                int ly = dollTop + row * stepY + tileRows;
                if (isVisible(ly, 1))
                    canvas.AddText(spineX, ly, "│", line);
            }
            int armY = dollTop + stepY + tileRows / 2;
            if (isVisible(armY, 1))
            {
                canvas.AddText(dollX + tileCols, armY, new string('─', ArmGap), line);
                canvas.AddText(dollX + stepX + tileCols, armY, new string('─', ArmGap), line);
            }

            string? dropSlot = InventoryDragState.IsActive && InventoryDragState.FromBag && InventoryDragState.Item != null
                ? InventoryActionPoolEntries.GetEquipSlotForItem(InventoryDragState.Item)
                : null;
            bool haveArt = ItemArtAtlas.IsAvailable;
            // Worn pieces of one material build a set, so gear-to-gear hover and a selected item also match material.
            // With nothing hovered, the inventory selection drives the glow.
            bool matchMaterial = false;
            if (inventoryInput)
                (hoveredItem, matchMaterial) = InventoryAffixSynergy.ResolveSource(hoveredItem, character);
            else
                matchMaterial = InventoryAffixSynergy.IsEquipped(hoveredItem, character);
            var affixes = InventoryAffixSynergy.GetAffixKeys(hoveredItem, includeMaterial: matchMaterial);

            foreach (var (slot, label, col, row) in Slots)
            {
                int tx = dollX + col * stepX;
                int ty = dollTop + row * stepY;
                if (!isVisible(ty, tileRows))
                    continue;
                var item = character.Equipment.GetSlotItem(slot);
                bool beingDragged = InventoryDragState.IsActive && InventoryDragState.SourceSlot == slot;
                bool isDropTarget = string.Equals(dropSlot, slot, StringComparison.Ordinal);
                bool shown = item != null && !beingDragged;

                Color frame;
                if (isDropTarget)
                    frame = ItemRendererHelper.IsEquipBlockedForCharacter(InventoryDragState.Item!, character)
                        ? AsciiArtAssets.Colors.Red
                        : AsciiArtAssets.Colors.Green;
                else if (!shown)
                    frame = AsciiArtAssets.Colors.DarkGray;
                else
                    frame = ItemRendererHelper.IsEquipBlockedForCharacter(item!, character)
                        ? AsciiArtAssets.Colors.Red
                        : ItemThemeProvider.GetRarityColor(item!.Rarity?.Trim() ?? "Common");

                bool isPaneSelection = inventoryInput && shown && string.Equals(SelectedSlot, slot, StringComparison.Ordinal);
                bool synergy = shown && !isDropTarget && !ReferenceEquals(item, hoveredItem)
                    && InventoryAffixSynergy.SharesAffix(item, hoveredItem, affixes, includeMaterial: matchMaterial);
                if (synergy)
                    frame = AsciiArtAssets.Colors.Cyan;
                else if (isPaneSelection && !isDropTarget)
                    frame = AsciiArtAssets.Colors.Gold;
                var c = AsciiArtAssets.Colors.Cyan;
                canvas.AddBox(tx, ty, tileCols, tileRows, frame, synergy ? Color.FromArgb(56, c.R, c.G, c.B) : default,
                    borderThicknessPixels: isDropTarget || isPaneSelection || synergy ? 2 : 1);
                if (shown && haveArt && ItemArtAtlas.TryGetIcon(item!, out var bitmap, out var sourceRect))
                    canvas.AddImage(tx, ty, tileCols, tileRows, bitmap, sourceRect, paddingPixels: 3, followsAsciiStyle: true);
                else
                {
                    string text = Truncate(shown ? item!.Name : label, tileCols - 2);
                    canvas.AddText(tx + Math.Max(1, (tileCols - text.Length) / 2), ty + tileRows / 2, text, shown ? frame : AsciiArtAssets.Colors.DarkGray);
                }

                string hover = LeftPanelHoverState.Prefix + "gear:" + slot;
                if (inventoryInput)
                {
                    addClickable(InventoryButtonFactory.CreateButton(
                        tx, ty, tileCols, tileRows,
                        InventoryDetailViewState.SlotInputPrefix + slot,
                        item != null ? $"{label}: {item.Name}" : $"{label}: empty",
                        item != null ? hover : null));
                }
                else if (item != null)
                {
                    addClickable(new ClickableElement
                    {
                        X = tx,
                        Y = ty,
                        Width = tileCols,
                        Height = tileRows,
                        Type = ElementType.Text,
                        Value = hover,
                        DisplayText = "Left panel tooltip"
                    });
                }
            }
        }

        private static string Truncate(string? text, int max)
        {
            string s = text ?? "";
            if (max <= 0)
                return "";
            return s.Length <= max ? s : s.Substring(0, max);
        }
    }
}
