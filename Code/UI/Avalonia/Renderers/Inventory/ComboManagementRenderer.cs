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
                HubTabBar.Draw(canvas, clickableElements.Add, x + 1, y, width - 2, HubTab.Actions);
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
            int libBlockH = Math.Max(2 + CardHeight + 1, bottom - top - seqBlockH - 1);
            bool seqBelow = ActionSequenceEditorLayout.SequenceBelow;
            int seqY = seqBelow ? top + libBlockH + 1 : top;
            int libY = seqBelow ? top : top + seqBlockH + 1;

            ActionSequenceEditorLayout.IsOpen = true;
            ActionSequenceEditorLayout.SequenceSlots.Clear();
            int blockW = perRow * (CardWidth + CardGap);
            ActionSequenceEditorLayout.SequenceArea = (left - 2, seqY, blockW + 2, seqBlockH);
            ActionSequenceEditorLayout.LibraryArea = (left - 2, libY, blockW + 2, libBlockH);
            bool dragging = ActionSequenceDragState.IsActive;

            // --- Sequence ---
            canvas.AddText(left, seqY, $"Action Sequence:  {combo.Count}/{cap} slots", gold);
            canvas.AddText(hintX, seqY, Truncate("drag to reorder, drag off to remove (or click)  |  F1: above/below library  |  Esc: back", hintW), AsciiArtAssets.Colors.DarkGray);
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
                DrawActionCard(cx, cy, combo[i], swing, isDragSource ? AsciiArtAssets.Colors.DarkGray : border, $"{ComboPointerInput.Prefix}rm:{i}", dimmed: isDragSource);
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
            int dropSlot = dragging ? ActionSequenceEditorLayout.DropSlotAt(ActionSequenceDragState.PointerX, ActionSequenceDragState.PointerY) : -1;
            if (dropSlot >= 0)
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
            canvas.AddText(hintX, libY, Truncate("actions on your equipped items  |  drag onto the sequence or click to add", hintW), AsciiArtAssets.Colors.DarkGray);
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
                DrawActionCard(cx, cy, action, PoolSwingLine(character, action), used ? AsciiArtAssets.Colors.DarkGray : gold,
                    $"{ComboPointerInput.Prefix}pool:{i}", dimmed: used);
                if (used)
                    canvas.AddText(cx + 2, cy + CardHeight - 2, "in sequence", AsciiArtAssets.Colors.DarkGray);
            }
            if (scroll > 0)
                canvas.AddText(left + 20, libY, "^ more above (Up)", AsciiArtAssets.Colors.Gray);
            if (libRows - scroll > libRowsFit)
                canvas.AddText(left, libY + 2 + libRowsFit * (CardHeight + 1) - 1, $"v {pool.Count - (scroll + libRowsFit) * perRow} more below (Down)", AsciiArtAssets.Colors.Gray);

            if (dragging)
                DrawDragGhost(x, y, width, height);

            return pool.Count + combo.Count;
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
        private void DrawActionCard(int cx, int cy, RPGGame.Action action, string swing, global::Avalonia.Media.Color border, string key, bool dimmed)
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
            var frame = element.IsHovered ? ColorPalette.Yellow.GetColor() : border;
            canvas.AddBox(cx, cy, CardWidth, CardHeight, frame, global::Avalonia.Media.Colors.Black);

            int innerW = CardWidth - 4;
            int ty = cy + 1;
            string name = (action.Name ?? "").ToUpperInvariant();
            canvas.AddText(cx + 2, ty++, Truncate(name, innerW), dimmed ? AsciiArtAssets.Colors.Gray : AsciiArtAssets.Colors.White);
            if (!string.IsNullOrEmpty(swing))
                canvas.AddText(cx + 2, ty++, Truncate(swing, innerW), dimmed ? AsciiArtAssets.Colors.DarkGray : AsciiArtAssets.Colors.Green);
            int linesLeft = cy + CardHeight - 1 - ty;
            foreach (var line in CombatActionStripBuilder.BuildActionStripDescriptionLines(action, innerW, Math.Max(0, linesLeft)))
                canvas.AddText(cx + 2, ty++, Truncate(line, innerW), AsciiArtAssets.Colors.Gray);
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
