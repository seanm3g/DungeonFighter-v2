namespace RPGGame.UI.Avalonia.Renderers.Inventory
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using RPGGame;
    using RPGGame.Handlers.Inventory;
    using RPGGame.UI;
    using RPGGame.UI.Avalonia;
    using RPGGame.UI.Avalonia.Layout;
    using RPGGame.UI.Avalonia.Managers;
    using RPGGame.UI.Avalonia.Renderers.Helpers;
    using RPGGame.UI.Avalonia.Renderers.Text;
    using RPGGame.UI.ColorSystem;
    using RPGGame.Items.Helpers;
    using RPGGame.Actions.RollModification;

    /// <summary>
    /// Renders the actions workspace: sequence slots (remove) + pool from gear (add). Order matches the strip above.
    /// </summary>
    public class ComboManagementRenderer
    {
        private readonly GameCanvasControl canvas;
        private readonly List<ClickableElement> clickableElements;

        private const int FooterLines = 5;
        private const int MaxSequenceRows = 8;
        private const int LinesPerPoolEntry = 2;

        public ComboManagementRenderer(
            GameCanvasControl canvas,
            List<ClickableElement> clickableElements)
        {
            this.canvas = canvas;
            this.clickableElements = clickableElements;
        }

        private const int CardWidth = 28;
        private const int CardHeight = 9;
        private const int CardGap = 3;

        /// <summary>Compelled (required weapon basic) card: crimson leans toward magenta, away from the orange-red class color.</summary>
        private static readonly global::Avalonia.Media.Color CompelledCrimson = global::Avalonia.Media.Color.FromRgb(170, 20, 50);
        private static readonly global::Avalonia.Media.Color CompelledCrimsonBright = global::Avalonia.Media.Color.FromRgb(230, 40, 75);

        /// <summary>
        /// Full-screen Action Sequence editor: the sequence as a row of cards (click one to take it out) and the
        /// library of actions from equipped gear as a grid (click one to add it). F1 puts the sequence below the library
        /// instead of above it. Back returns to the inventory.
        /// </summary>
        public int RenderComboManagement(int x, int y, int width, int height, Character character, bool hubTabs = false)
        {
            var gold = ColorPalette.Gold.GetColor();
            int left = x + (hubTabs ? 2 : 4);
            int usableW;
            int top;
            if (hubTabs)
            {
                // The hub's Actions tab: side panels stay (their click targets are already registered), tabs on top.
                HubTabBar.Draw(canvas, clickableElements.Add, x + 1, y, width - 2, HubTab.Actions, character: character);
                usableW = Math.Max(CardWidth, width - 4);
                top = y + 2;
            }
            else
            {
                clickableElements.Clear();
                usableW = Math.Max(CardWidth, width - 8 - 26); // keep the right column for Settings
                top = y + 5;
                // Back (top left), Settings (bottom right)
                BoxButton.Draw(canvas, clickableElements, left, y + 1, 10, 3, "< Back", $"{ComboPointerInput.Prefix}back");
                const int sideW = 20;
                int sideX = x + width - sideW - 4;
                BoxButton.Draw(canvas, clickableElements, sideX, y + height - 5, sideW, 3, "Settings", InventoryMenuHandler.HubSettings);
            }
            int perRow = Math.Max(1, (usableW + CardGap) / (CardWidth + CardGap));
            int hintX = left + 40;
            int hintW = Math.Max(0, x + width - 1 - hintX);

            var combo = character.GetComboActions();
            var panels = CombatActionStripBuilder.BuildPanelData(character);
            int cap = ComboSequenceMaxHelper.GetEffectiveMax(character);
            var pool = character.GetActionPool();
            var inSequence = new HashSet<RPGGame.Action>(combo);

            // Section heights: the sequence takes as many card rows as it needs (usually one); the library gets the rest.
            // Empty slots up to the cap are drawn too, so the row shows how long the sequence can get.
            int seqSlots = Math.Max(combo.Count, cap);
            int seqRows = Math.Max(1, (Math.Max(seqSlots, 1) + perRow - 1) / perRow);
            int seqBlockH = 2 + seqRows * (CardHeight + 2);
            int bottom = y + height - 2;
            // Fixed details panel under both blocks: the hovered card's full description lives here, not over the cards.
            int minLibBlockH = 2 + CardHeight + 1;
            int infoH = Math.Min(MaxInfoPanelHeight, bottom - top - seqBlockH - 1 - minLibBlockH - 1);
            if (infoH < MinInfoPanelHeight)
                infoH = 0;
            int libBlockH = Math.Max(minLibBlockH, bottom - top - seqBlockH - 1 - (infoH > 0 ? infoH + 1 : 0));
            bool seqBelow = ActionSequenceEditorLayout.SequenceBelow;
            int seqY = seqBelow ? top + libBlockH + 1 : top;
            int libY = seqBelow ? top : top + seqBlockH + 1;
            int infoY = top + seqBlockH + 1 + libBlockH + 1;

            ActionSequenceEditorLayout.IsOpen = true;
            ActionSequenceEditorLayout.SequenceSlots.Clear();
            ActionSequenceEditorLayout.LibrarySlots.Clear();
            int blockW = perRow * (CardWidth + CardGap);
            ActionSequenceEditorLayout.SequenceArea = (left - 2, seqY, blockW + 2, seqBlockH);
            ActionSequenceEditorLayout.LibraryArea = (left - 2, libY, blockW + 2, libBlockH);
            bool dragging = ActionSequenceDragState.IsActive;

            // Hovered card (hover state comes from the previous paint's click targets) and what it synergizes with.
            int hoverSeq = dragging ? -1 : RightPanelActionHoverState.HoveredSequenceIndex;
            int hoverPool = dragging || RightPanelActionHoverState.HoveredInventoryPoolIndex >= 0 ? -1 : RightPanelActionHoverState.HoveredPoolIndex;
            RPGGame.Action? hovered = hoverSeq >= 0 && hoverSeq < combo.Count ? combo[hoverSeq]
                : hoverPool >= 0 && hoverPool < pool.Count ? pool[hoverPool] : null;
            var synergy = BuildSynergyMap(hovered, combo, pool);

            // --- Sequence ---
            canvas.AddText(left, seqY, $"Action Sequence:  {combo.Count}/{cap} slots", gold);
            canvas.AddText(hintX, seqY, Truncate("drag between to move, onto a card to swap, off (or right-click) to remove  |  F1: above/below library  |  Esc: back", hintW), AsciiArtAssets.Colors.DarkGray);
            // Sequence info: totals over the cards and the combo speed (chain time when every combo roll lands).
            if (combo.Count > 0)
            {
                var summary = CombatActionStripBuilder.BuildSequenceSummary(character);
                canvas.AddText(left, seqY + 1, Truncate(CombatActionStripBuilder.FormatSequenceSummaryLine(in summary), Math.Max(0, x + width - 1 - left)), AsciiArtAssets.Colors.Cyan);
            }
            int nextStep = combo.Count > 0 ? character.ComboStep % combo.Count : -1;
            for (int i = 0; i < combo.Count; i++)
            {
                int cx = left + (i % perRow) * (CardWidth + CardGap);
                int cy = seqY + 2 + (i / perRow) * (CardHeight + 2);
                string swing = i < panels.Count
                    ? CombatActionStripBuilder.FormatStripSwingLine(panels[i], character, combo[i], ActionStripDamageLineMode.EffectiveWithComboAmp, i)
                    : "";
                var border = i == nextStep ? AsciiArtAssets.Colors.White : AsciiArtAssets.Colors.Gray;
                bool isDragSource = dragging && ActionSequenceDragState.FromSequenceIndex == i;
                DrawActionCard(character, cx, cy, combo[i], swing, isDragSource ? AsciiArtAssets.Colors.DarkGray : border, $"{ComboPointerInput.Prefix}rm:{i}", dimmed: isDragSource,
                    synergy: synergy.TryGetValue(combo[i], out var seqLink) ? seqLink : null);
                ActionSequenceEditorLayout.SequenceSlots.Add((cx, cy, CardWidth, CardHeight));
                string number = (i + 1).ToString();
                canvas.AddText(cx + (CardWidth - number.Length) / 2, cy + CardHeight, number, AsciiArtAssets.Colors.Green);
            }

            for (int i = combo.Count; i < seqSlots; i++)
            {
                int cx = left + (i % perRow) * (CardWidth + CardGap);
                int cy = seqY + 2 + (i / perRow) * (CardHeight + 2);
                canvas.AddBox(cx, cy, CardWidth, CardHeight, AsciiArtAssets.Colors.DarkGray, global::Avalonia.Media.Colors.Black);
                const string emptyLabel = "empty slot";
                canvas.AddText(cx + (CardWidth - emptyLabel.Length) / 2, cy + CardHeight / 2, emptyLabel, AsciiArtAssets.Colors.DarkGray);
                string number = (i + 1).ToString();
                canvas.AddText(cx + (CardWidth - number.Length) / 2, cy + CardHeight, number, AsciiArtAssets.Colors.DarkGray);
            }

            // Drop marker: a bar where the dragged card would land (needs this paint's slot rects, so after the cards).
            int dropSlot = -1;
            bool dropOnto = false;
            if (dragging && !ActionSequenceEditorLayout.DropTargetAt(ActionSequenceDragState.PointerX, ActionSequenceDragState.PointerY, out dropSlot, out dropOnto))
                dropSlot = -1;
            if (dropOnto && dropSlot != ActionSequenceDragState.FromSequenceIndex)
            {
                // Onto a card: frame it (swap / replace).
                var r = ActionSequenceEditorLayout.SequenceSlots[dropSlot];
                canvas.AddBorder(r.X, r.Y, r.W, r.H, ColorPalette.Yellow.GetColor(), 2);
            }
            else if (dropSlot >= 0 && !dropOnto)
            {
                int mi = dropSlot;
                int mx = left + (mi % perRow) * (CardWidth + CardGap) - 2;
                int my = seqY + 2 + (mi / perRow) * (CardHeight + 2);
                var yellow = ColorPalette.Yellow.GetColor();
                for (int r = 0; r < CardHeight; r++)
                    canvas.AddText(mx, my + r, "|", yellow);
            }

            // --- Library (actions on equipped gear) ---
            canvas.AddText(left, libY, "Available Actions:", gold);
            canvas.AddText(hintX, libY, Truncate("actions on your equipped items  |  drag into the sequence (onto a card to replace it) or click to add", hintW), AsciiArtAssets.Colors.DarkGray);
            int libRowsFit = Math.Max(1, (libBlockH - 2) / (CardHeight + 1));
            int libRows = (pool.Count + perRow - 1) / perRow;
            int scroll = Math.Clamp(ActionSequenceEditorLayout.LibraryScrollRow, 0, Math.Max(0, libRows - libRowsFit));
            ActionSequenceEditorLayout.LibraryScrollRow = scroll;
            if (pool.Count == 0)
                canvas.AddText(left, libY + 2, "(no actions: equip items that grant actions)", AsciiArtAssets.Colors.Gray);
            for (int i = scroll * perRow; i < pool.Count; i++)
            {
                int row = i / perRow - scroll;
                if (row >= libRowsFit)
                    break;
                int cx = left + (i % perRow) * (CardWidth + CardGap);
                int cy = libY + 2 + row * (CardHeight + 1);
                var action = pool[i];
                bool used = inSequence.Contains(action);
                DrawActionCard(character, cx, cy, action, PoolSwingLine(character, action), used ? AsciiArtAssets.Colors.DarkGray : gold,
                    $"{ComboPointerInput.Prefix}pool:{i}", dimmed: used,
                    synergy: synergy.TryGetValue(action, out var poolLink) ? poolLink : null);
                ActionSequenceEditorLayout.LibrarySlots[i] = (cx, cy, CardWidth, CardHeight);
                if (used)
                    canvas.AddText(cx + 2, cy + CardHeight - 2, "in sequence", AsciiArtAssets.Colors.DarkGray);
            }
            if (scroll > 0)
                canvas.AddText(left + 20, libY, "^ more above (Up)", AsciiArtAssets.Colors.Gray);
            if (libRows - scroll > libRowsFit)
                canvas.AddText(left, libY + 2 + libRowsFit * (CardHeight + 1) - 1, $"v {pool.Count - (scroll + libRowsFit) * perRow} more below (Down)", AsciiArtAssets.Colors.Gray);

            if (infoH > 0)
                DrawInfoPanel(character, hovered, hoverSeq, combo, pool, synergy, left, infoY, blockW - CardGap, infoH);

            if (dragging)
                DrawDragGhost(x, y, width, height);

            return pool.Count + combo.Count;
        }

        private const int MinInfoPanelHeight = 7;
        private const int MaxInfoPanelHeight = 16;
        private const int InfoColumnMinWidth = 46;
        private const int InfoColumnGap = 3;

        private static readonly global::Avalonia.Media.Color SynergyStrong = global::Avalonia.Media.Color.FromRgb(70, 220, 230);
        private static readonly global::Avalonia.Media.Color SynergyWeak = global::Avalonia.Media.Color.FromRgb(40, 110, 120);

        /// <summary>
        /// Each sequence / library action (by reference) that shares a key with <paramref name="hovered"/>, with its best
        /// link (mechanic keys before class keys). Empty when nothing is hovered.
        /// </summary>
        private static Dictionary<RPGGame.Action, ActionSynergy.Link> BuildSynergyMap(
            RPGGame.Action? hovered, List<RPGGame.Action> combo, List<RPGGame.Action> pool)
        {
            var map = new Dictionary<RPGGame.Action, ActionSynergy.Link>(ReferenceEqualityComparer.Instance);
            if (hovered == null)
                return map;
            var hoveredKeys = ActionSynergy.Keys(hovered);
            if (hoveredKeys.Count == 0)
                return map;
            foreach (var other in combo.Concat(pool))
            {
                if (other == null || ReferenceEquals(other, hovered) || map.ContainsKey(other))
                    continue;
                var shared = ActionSynergy.Shared(hoveredKeys, ActionSynergy.Keys(other));
                if (shared.Count > 0)
                    map[other] = shared[0];
            }
            return map;
        }

        /// <summary>
        /// The fixed details panel under the sequence and library: the hovered card's full tooltip (with a Synergy
        /// section naming the cards and worn gear it works with), flowed into side-by-side columns. A hint when idle.
        /// </summary>
        private void DrawInfoPanel(
            Character character, RPGGame.Action? hovered, int hoverSeq,
            List<RPGGame.Action> combo, List<RPGGame.Action> pool,
            Dictionary<RPGGame.Action, ActionSynergy.Link> synergy,
            int px, int py, int pw, int ph)
        {
            var gold = ColorPalette.Gold.GetColor();
            canvas.AddBox(px, py, pw, ph, hovered != null ? gold : AsciiArtAssets.Colors.DarkGray, global::Avalonia.Media.Colors.Black);
            canvas.AddText(px + 2, py, " Action details ", hovered != null ? gold : AsciiArtAssets.Colors.Gray);

            int textX = px + 2;
            int textY = py + 1;
            int textW = Math.Max(8, pw - 4);
            int rowsPerCol = Math.Max(1, ph - 2);

            if (hovered == null)
            {
                canvas.AddText(textX, textY, Truncate("Hover an action to read it here. Cards that work with it light up: bright = shared mechanic, dim = same class.", textW), AsciiArtAssets.Colors.DarkGray);
                return;
            }

            int cols = Math.Clamp((textW + InfoColumnGap) / (InfoColumnMinWidth + InfoColumnGap), 1, 3);
            int colW = (textW - (cols - 1) * InfoColumnGap) / cols;
            int maxLines = cols * rowsPerCol + 8;
            bool alt = HoverTooltipDetailState.IsAltDetailActive;
            var lines = hoverSeq >= 0
                ? CombatActionStripBuilder.BuildColoredActionTooltipLinesForSlot(character, hoverSeq, maxLines, ActionStripDamageLineMode.EffectiveWithComboAmp, alt)
                : CombatActionStripBuilder.BuildColoredActionTooltipLines(character, hovered, maxLines, includeExtendedDetails: alt);

            // Synergy section right after the title block, so it never falls off the end of the columns.
            int insertAt = lines.FindIndex(l => l == null || l.Count == 0);
            if (insertAt < 0)
                insertAt = lines.Count;
            lines.InsertRange(insertAt, BuildSynergyLines(character, hovered, combo, pool, synergy));

            // Wrap to the column width, then flow down each column in turn (no column starts on a blank row).
            var rows = new List<List<ColoredText>>();
            foreach (var line in lines)
            {
                var wrapped = line == null || line.Count == 0 ? null : TextWrappingHelper.WrapColoredSegments(line, colW);
                if (wrapped == null || wrapped.Count == 0)
                    rows.Add(new List<ColoredText>());
                else
                    rows.AddRange(wrapped);
            }

            int col = 0, rowInCol = 0;
            for (int ri = 0; ri < rows.Count; ri++)
            {
                var row = rows[ri];
                if (row.Count == 0 && rowInCol == 0 && col > 0)
                    continue;
                // Keep a section heading with its first line: a heading on a column's last row moves to the next column.
                bool isHeading = row.Count > 0 && ri > 0 && rows[ri - 1].Count == 0
                    && ri + 1 < rows.Count && rows[ri + 1].Count > 0;
                if (isHeading && rowInCol == rowsPerCol - 1 && rowsPerCol > 1)
                    rowInCol = rowsPerCol;
                if (rowInCol >= rowsPerCol)
                {
                    col++;
                    rowInCol = 0;
                    if (row.Count == 0)
                        continue;
                }
                if (col >= cols)
                {
                    // Out of room: mark the cut on the last row.
                    int lastColX = textX + (cols - 1) * (colW + InfoColumnGap);
                    canvas.AddText(lastColX + Math.Max(0, colW - 3), textY + rowsPerCol - 1, "...", AsciiArtAssets.Colors.Gray);
                    break;
                }
                int colX = textX + col * (colW + InfoColumnGap);
                int cx = colX;
                foreach (var seg in row)
                {
                    if (string.IsNullOrEmpty(seg.Text))
                        continue;
                    int room = colX + colW - cx;
                    if (room <= 0)
                        break;
                    string text = seg.Text.Length > room ? seg.Text.Substring(0, room) : seg.Text;
                    canvas.AddText(cx, textY + rowInCol, text, ColorConverter.ConvertSegmentToCanvasColor(seg));
                    cx += text.Length;
                }
                rowInCol++;
            }
        }

        /// <summary>"Synergy" lines: cards sharing a mechanic, cards sharing only the class, and worn gear whose rule names a key.</summary>
        private static List<List<ColoredText>> BuildSynergyLines(
            Character character, RPGGame.Action hovered,
            List<RPGGame.Action> combo, List<RPGGame.Action> pool,
            Dictionary<RPGGame.Action, ActionSynergy.Link> synergy)
        {
            var result = new List<List<ColoredText>>();
            var gray = AsciiArtAssets.Colors.Gray;
            var white = AsciiArtAssets.Colors.White;

            // One entry per name (a library card and its sequence copy are the same action).
            var strong = new List<(string Name, string Key)>();
            var weak = new List<string>();
            var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var a in combo.Concat(pool))
            {
                if (a == null || !synergy.TryGetValue(a, out var link) || !seen.Add(a.Name ?? ""))
                    continue;
                if (link.Strong)
                    strong.Add((a.Name ?? "", link.Key));
                else
                    weak.Add(a.Name ?? "");
            }
            var gear = ActionSynergy.GearLinks(character, ActionSynergy.Keys(hovered));

            result.Add(new List<ColoredText>());
            result.Add(new ColoredTextBuilder().Add("Synergy", AsciiArtAssets.Colors.Cyan).Build());
            if (strong.Count == 0 && weak.Count == 0 && gear.Count == 0)
            {
                result.Add(new ColoredTextBuilder().Add("Nothing in your sequence, library or gear shares a tag with it.", gray).Build());
                return result;
            }
            if (strong.Count > 0)
            {
                var b = new ColoredTextBuilder().Add("Works with: ", gray);
                for (int i = 0; i < strong.Count; i++)
                {
                    if (i > 0)
                        b.Add(", ", gray);
                    b.Add(strong[i].Name.ToUpperInvariant(), white);
                    b.Add($" ({strong[i].Key})", SynergyStrong);
                }
                result.Add(b.Build());
            }
            if (weak.Count > 0)
                result.Add(new ColoredTextBuilder().Add("Same class: ", gray).Add(string.Join(", ", weak.Select(n => n.ToUpperInvariant())), SynergyWeak).Build());
            // One line per shared key: "Gear: (warrior) on Coat, Leggings, ..." instead of a line per piece.
            foreach (var group in gear.GroupBy(g => g.Key))
            {
                string key = group.Key;
                var b = new ColoredTextBuilder().Add("Gear: ", gray);
                b.Add($"({key})", ActionSynergy.IsClassKey(key) ? SynergyWeak : SynergyStrong);
                b.Add(" on ", gray).Add(string.Join(", ", group.Select(g => g.Item)), gray);
                result.Add(b.Build());
            }
            return result;
        }

        /// <summary>The dragged card follows the pointer (drawn on the overlay layer, above the cards).</summary>
        private void DrawDragGhost(int x, int y, int width, int height)
        {
            const int gw = 22;
            const int gh = 3;
            int gx = Math.Clamp(ActionSequenceDragState.PointerX - gw / 2, x, Math.Max(x, x + width - gw));
            int gy = Math.Clamp(ActionSequenceDragState.PointerY - 1, y, Math.Max(y, y + height - gh));
            var yellow = ColorPalette.Yellow.GetColor();
            canvas.AddOverlayBox(gx, gy, gw, gh, yellow, global::Avalonia.Media.Color.FromRgb(20, 20, 20));
            string label = Truncate((ActionSequenceDragState.ActionName ?? "").ToUpperInvariant(), gw - 4);
            canvas.AddOverlayText(gx + Math.Max(2, (gw - label.Length) / 2), gy + 1, label, yellow);
        }

        /// <summary>One action card: name, damage | speed, then the description wrapped into what is left.</summary>
        private void DrawActionCard(Character character, int cx, int cy, RPGGame.Action action, string swing, global::Avalonia.Media.Color border, string key, bool dimmed,
            ActionSynergy.Link? synergy = null)
        {
            var element = new ClickableElement
            {
                X = cx,
                Y = cy,
                Width = CardWidth,
                Height = CardHeight,
                Type = ElementType.MenuOption,
                Value = key,
                DisplayText = action.Name
            };
            clickableElements.Add(element);
            // The compelled weapon basic gets a crimson frame and title: a cooler, deeper red than the Barbarian
            // class red (Berserker, Might), so "required" never reads as a class / rarity color.
            bool compelled = WeaponRequiredComboAction.IsRequiredBasicForEquippedWeapon(character, action);
            if (compelled)
                border = dimmed ? ActionInfoStripLayout.DarkenRgb(CompelledCrimson, 0.5)
                    : border == AsciiArtAssets.Colors.White ? CompelledCrimsonBright : CompelledCrimson;
            // Synergy with the hovered card: bright frame for a shared mechanic, dim for the same class only.
            if (synergy is { } link)
            {
                var tint = link.Strong ? SynergyStrong : SynergyWeak;
                border = dimmed ? ActionInfoStripLayout.DarkenRgb(tint, 0.6) : tint;
            }
            var frame = element.IsHovered ? ColorPalette.Yellow.GetColor() : border;
            canvas.AddBox(cx, cy, CardWidth, CardHeight, frame, global::Avalonia.Media.Colors.Black);
            if (synergy is { Strong: true } && !element.IsHovered)
                canvas.AddBorder(cx, cy, CardWidth, CardHeight, dimmed ? ActionInfoStripLayout.DarkenRgb(SynergyStrong, 0.6) : SynergyStrong, 2);

            int innerW = CardWidth - 4;
            int ty = cy + 1;
            string name = (action.Name ?? "").ToUpperInvariant();
            var nameColor = compelled ? CompelledCrimsonBright : ActionNameColors.Resolve(action);
            if (dimmed)
                nameColor = ActionInfoStripLayout.DarkenRgb(nameColor, 0.5);
            canvas.AddText(cx + 2, ty++, Truncate(name, innerW), nameColor);
            if (compelled && !dimmed)
            {
                const string tag = "required";
                canvas.AddText(cx + CardWidth - 2 - tag.Length, cy + CardHeight - 2, tag, CompelledCrimson);
            }
            if (!string.IsNullOrEmpty(swing))
                canvas.AddText(cx + 2, ty++, Truncate(swing, innerW), dimmed ? AsciiArtAssets.Colors.DarkGray : AsciiArtAssets.Colors.Green);
            // Tags stay off the card face: the hover tooltip lists them.
            int linesLeft = cy + CardHeight - 1 - ty - (synergy.HasValue ? 1 : 0);
            foreach (var line in CombatActionStripBuilder.BuildActionStripDescriptionLines(action, innerW, Math.Max(0, linesLeft)))
                canvas.AddText(cx + 2, ty++, Truncate(line, innerW), AsciiArtAssets.Colors.Gray);
            // The shared key on the card's last row (right side when "in sequence" holds the left).
            if (synergy is { } shown)
            {
                string label = Truncate("+ " + shown.Key, dimmed ? innerW - 12 : (compelled ? innerW - 9 : innerW));
                int lx = dimmed ? cx + CardWidth - 2 - label.Length : cx + 2;
                canvas.AddText(lx, cy + CardHeight - 2, label, border);
            }
        }

        /// <summary>Damage | speed for a library card (no combo slot: base values plus gear bonuses).</summary>
        private static string PoolSwingLine(Character character, RPGGame.Action action)
        {
            try
            {
                double dmgPct = action.DamageMultiplier * 100.0;
                double spdPct = action.Length > 0 ? ActionDisplayFormatter.CalculateActionSpeedPercentage(action) : 0;
                int hits;
                try { hits = RollModificationManager.GetEffectiveMultiHitCountForModifierScaling(action, character, -1, includeConsumedMods: false); }
                catch { hits = 1; }
                var info = new ActionPanelInfo(action.Name ?? "", dmgPct, dmgPct, spdPct, spdPct, "", 0, hits);
                return CombatActionStripBuilder.FormatStripSwingLine(info, character, action, ActionStripDamageLineMode.EffectiveWithComboAmp, -1);
            }
            catch
            {
                return "";
            }
        }

        private static string Truncate(string text, int max)
        {
            if (string.IsNullOrEmpty(text) || text.Length <= max)
                return text ?? "";
            return max > 1 ? text.Substring(0, max - 1) + "." : text.Substring(0, max);
        }

        /// <summary>
        /// Renders action selection prompt for adding/removing combo actions
        /// </summary>
        public int RenderComboActionSelection(int x, int y, int width, int height, Character character, string actionType)
        {
            clickableElements.Clear();
            int currentLineCount = 0;

            canvas.ClearTextInArea(x, y, width, height);
            canvas.ClearProgressBarsInArea(x, y, width, height);

            int startY = y;

            string headerText = actionType == "add" ? "ADD ACTION TO COMBO" : "REMOVE ACTION FROM COMBO";
            canvas.AddText(x + 2, y, AsciiArtAssets.UIText.CreateHeader(headerText), AsciiArtAssets.Colors.Gold);
            y += 2;
            currentLineCount += 2;

            if (actionType == "add")
            {
                var actionPool = character.GetActionPool();
                var comboActions = character.GetComboActions();

                if (actionPool.Count == 0)
                {
                    canvas.AddText(x + 2, y, "No actions available to add to combo.", AsciiArtAssets.Colors.White);
                    y += 2;
                    currentLineCount += 2;
                }
                else
                {
                    canvas.AddText(x + 2, y, "Available actions:", AsciiArtAssets.Colors.White);
                    y += 2;
                    currentLineCount += 2;

                    for (int i = 0; i < actionPool.Count; i++)
                    {
                        var action = actionPool[i];
                        int timesInCombo = comboActions.Count(ca => ca.Name == action.Name);
                        int timesAvailable = actionPool.Count(ap => ap.Name == action.Name);
                        string usageInfo = timesInCombo > 0 ? $" [In combo: {timesInCombo}/{timesAvailable}]" : "";

                        var button = InventoryButtonFactory.CreateButton(x + 2, y, width - 4, (i + 1).ToString(), MenuOptionFormatter.FormatItem(i + 1, action.Name + usageInfo));
                        clickableElements.Add(button);

                        canvas.AddMenuOption(x + 2, y, i + 1, $"{action.Name}{usageInfo}", AsciiArtAssets.Colors.White, button.IsHovered);
                        y++;
                        currentLineCount++;

                        string addMods = CombatActionStripBuilder.BuildActionMechanicalModSummary(character, action, -1);
                        if (addMods.Length > width - 8)
                            addMods = addMods.Substring(0, Math.Max(0, width - 11)) + "...";
                        canvas.AddText(x + 4, y, addMods, AsciiArtAssets.Colors.Gray);
                        y++;
                        currentLineCount++;
                    }
                }
            }
            else
            {
                var comboActions = character.GetComboActions();

                if (comboActions.Count == 0)
                {
                    canvas.AddText(x + 2, y, "No actions in combo sequence to remove.", AsciiArtAssets.Colors.White);
                    y += 2;
                    currentLineCount += 2;
                }
                else
                {
                    canvas.AddText(x + 2, y, "Current combo sequence:", AsciiArtAssets.Colors.White);
                    y += 2;
                    currentLineCount += 2;

                    for (int i = 0; i < comboActions.Count; i++)
                    {
                        var action = comboActions[i];
                        string currentStep = character.ComboStep % comboActions.Count == i ? " <- NEXT" : "";

                        var button = InventoryButtonFactory.CreateButton(x + 2, y, width - 4, (i + 1).ToString(), MenuOptionFormatter.FormatItem(i + 1, action.Name + currentStep));
                        clickableElements.Add(button);

                        canvas.AddMenuOption(x + 2, y, i + 1, $"{action.Name}{currentStep}", AsciiArtAssets.Colors.White, button.IsHovered);
                        y++;
                        currentLineCount++;

                        string seqMods = CombatActionStripBuilder.BuildActionMechanicalModSummary(character, action, i);
                        if (seqMods.Length > width - 8)
                            seqMods = seqMods.Substring(0, Math.Max(0, width - 11)) + "...";
                        canvas.AddText(x + 4, y, seqMods, AsciiArtAssets.Colors.Gray);
                        y++;
                        currentLineCount++;
                    }
                }
            }

            y++;
            currentLineCount++;

            var cancelButton = InventoryButtonFactory.CreateButton(x + 2, y, 28, "0", MenuOptionFormatter.Format(0, UIConstants.MenuOptions.Cancel));
            clickableElements.Add(cancelButton);
            canvas.AddMenuOption(x + 2, y, 0, UIConstants.MenuOptions.Cancel, AsciiArtAssets.Colors.White, cancelButton.IsHovered);
            currentLineCount++;

            return currentLineCount;
        }

        /// <summary>
        /// Renders combo reorder prompt
        /// </summary>
        public int RenderComboReorderPrompt(int x, int y, int width, int height, Character character, string currentSequence = "")
        {
            clickableElements.Clear();
            int currentLineCount = 0;

            canvas.ClearTextInArea(x, y, width, height);
            canvas.ClearProgressBarsInArea(x, y, width, height);

            canvas.AddText(x + 2, y, AsciiArtAssets.UIText.CreateHeader("REORDER COMBO ACTIONS"), AsciiArtAssets.Colors.Gold);
            y += 2;
            currentLineCount += 2;

            var comboActions = character.GetComboActions();

            canvas.AddText(x + 2, y, "Current combo sequence:", AsciiArtAssets.Colors.White);
            y += 2;
            currentLineCount += 2;

            for (int i = 0; i < comboActions.Count; i++)
            {
                var action = comboActions[i];
                string currentStep = character.ComboStep % comboActions.Count == i ? " <- NEXT" : "";
                canvas.AddText(x + 4, y, $"{i + 1}. {action.Name}{currentStep}", AsciiArtAssets.Colors.White);
                y++;
                currentLineCount++;
            }

            y += 2;
            currentLineCount += 2;

            canvas.AddText(x + 2, y, $"Enter the new order using numbers 1-{comboActions.Count} (e.g., 15324):", AsciiArtAssets.Colors.White);
            y += 2;
            currentLineCount += 2;

            canvas.AddText(x + 2, y, $"New order: {currentSequence}", AsciiArtAssets.Colors.White);
            y += 2;
            currentLineCount += 2;

            canvas.AddText(x + 2, y, "Press 0 to confirm, or continue entering numbers.", AsciiArtAssets.Colors.Gray);
            currentLineCount++;

            return currentLineCount;
        }
    }
}
