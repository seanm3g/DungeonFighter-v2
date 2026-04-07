namespace RPGGame.UI.Avalonia.Renderers.Inventory
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using RPGGame;
    using RPGGame.Handlers.Inventory;
    using RPGGame.Items.Helpers;
    using RPGGame.UI;
    using RPGGame.UI.Avalonia;

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

        /// <summary>
        /// Renders the pool-centric actions menu (sequence editing uses strip drag + row remove here).
        /// </summary>
        public int RenderComboManagement(int x, int y, int width, int height, Character character)
        {
            clickableElements.Clear();
            int currentLineCount = 0;

            canvas.ClearTextInArea(x, y, width, height);
            canvas.ClearProgressBarsInArea(x, y, width, height);

            int startY = y;
            int maxY = y + height - FooterLines;

            canvas.AddText(x + 2, y, AsciiArtAssets.UIText.CreateHeader("ACTIONS"), AsciiArtAssets.Colors.Gold);
            y += 2;
            currentLineCount += 2;

            canvas.AddText(x + 2, y, "Sequence: drag the cards above to reorder (when allowed).", AsciiArtAssets.Colors.Gray);
            y++;
            currentLineCount++;
            canvas.AddText(x + 2, y, "Pool: from equipped gear & class — click a row to add to the sequence.", AsciiArtAssets.Colors.Gray);
            y++;
            currentLineCount++;
            canvas.AddText(x + 2, y, "Keys: 1=add list  2=remove list  8=reorder  9=add all  0/5=back", AsciiArtAssets.Colors.DarkGray);
            y += 2;
            currentLineCount += 2;

            var comboActions = character.GetComboActions();
            canvas.AddText(x + 2, y, AsciiArtAssets.UIText.CreateHeader("SEQUENCE (remove)"), AsciiArtAssets.Colors.Gold);
            y += 2;
            currentLineCount += 2;

            if (comboActions.Count > 0)
            {
                int showSeq = Math.Min(comboActions.Count, MaxSequenceRows);
                for (int i = 0; i < showSeq && y <= maxY; i++)
                {
                    var action = comboActions[i];
                    string step = character.ComboStep % comboActions.Count == i ? " <- next" : "";
                    string label = $"{action.Name}{step}";
                    string line = label + "  [click to remove]";
                    if (line.Length > width - 8)
                        line = line.Substring(0, Math.Max(0, width - 11)) + "...";

                    string value = $"{ComboPointerInput.Prefix}rm:{i}";
                    var removeBtn = InventoryButtonFactory.CreateButton(x + 2, y, Math.Max(8, width - 4), value, line);
                    clickableElements.Add(removeBtn);
                    var seqColor = removeBtn.IsHovered ? AsciiArtAssets.Colors.Yellow : AsciiArtAssets.Colors.White;
                    canvas.AddText(x + 2, y, line, seqColor);
                    y++;
                    currentLineCount++;
                }

                if (comboActions.Count > showSeq)
                {
                    canvas.AddText(x + 2, y, $"... +{comboActions.Count - showSeq} more in sequence (strip above)", AsciiArtAssets.Colors.Gray);
                    y++;
                    currentLineCount++;
                }
            }
            else
            {
                canvas.AddText(x + 2, y, "(empty — add from pool below)", AsciiArtAssets.Colors.DarkGray);
                y += 2;
                currentLineCount += 2;
            }

            y++;
            if (y > maxY)
                y = maxY;

            canvas.AddText(x + 2, y, AsciiArtAssets.UIText.CreateHeader("POOL (from gear)"), AsciiArtAssets.Colors.Gold);
            y += 2;
            currentLineCount += 2;

            var actionPool = character.GetActionPool();
            var comboSet = new HashSet<RPGGame.Action>(comboActions);

            if (actionPool.Count == 0)
            {
                canvas.AddText(x + 2, y, "(no actions — equip items that grant abilities)", AsciiArtAssets.Colors.DarkGray);
                y += 2;
                currentLineCount += 2;
            }
            else
            {
                int poolIdx = 0;
                while (poolIdx < actionPool.Count && y + LinesPerPoolEntry - 1 <= maxY)
                {
                    var action = actionPool[poolIdx];
                    bool inSeq = comboSet.Contains(action);
                    string suffix = inSeq ? "  [in sequence]" : "";
                    string nameLine = action.Name + suffix;
                    if (nameLine.Length > width - 8)
                        nameLine = nameLine.Substring(0, Math.Max(0, width - 11)) + "...";

                    string poolValue = $"{ComboPointerInput.Prefix}pool:{poolIdx}";
                    var addBtn = InventoryButtonFactory.CreateButton(x + 2, y, Math.Max(8, width - 4), poolValue, nameLine);
                    clickableElements.Add(addBtn);
                    var nameColor = inSeq ? AsciiArtAssets.Colors.DarkGray : AsciiArtAssets.Colors.Cyan;
                    var poolDrawColor = addBtn.IsHovered ? AsciiArtAssets.Colors.Yellow : nameColor;
                    canvas.AddText(x + 2, y, nameLine, poolDrawColor);
                    y++;
                    currentLineCount++;

                    string stats = ActionDisplayFormatter.GetActionStats(action);
                    if (stats.Length > width - 8)
                        stats = stats.Substring(0, Math.Max(0, width - 11)) + "...";
                    canvas.AddText(x + 4, y, stats, AsciiArtAssets.Colors.Gray);
                    y++;
                    currentLineCount++;

                    poolIdx++;
                }

                if (poolIdx < actionPool.Count)
                {
                    canvas.AddText(x + 2, y, $"... +{actionPool.Count - poolIdx} more — press 1 for full numbered list", AsciiArtAssets.Colors.Gray);
                    y++;
                    currentLineCount++;
                }
            }

            int footerY = startY + height - FooterLines;
            canvas.AddText(x + 2, footerY, AsciiArtAssets.UIText.CreateHeader(UIConstants.Headers.Actions), AsciiArtAssets.Colors.Gold);
            footerY += 2;

            var backBtn = InventoryButtonFactory.CreateButton(x + 2, footerY, 26, $"{ComboPointerInput.Prefix}back", "Back");
            var reorderBtn = InventoryButtonFactory.CreateButton(x + 30, footerY, 28, $"{ComboPointerInput.Prefix}reorder", "Reorder");
            var addAllBtn = InventoryButtonFactory.CreateButton(x + 2, footerY + 1, 26, $"{ComboPointerInput.Prefix}addall", "Add all");
            clickableElements.AddRange(new[] { backBtn, reorderBtn, addAllBtn });

            canvas.AddMenuOption(x + 2, footerY, 0, "Back to inventory (0/5)", AsciiArtAssets.Colors.White, backBtn.IsHovered);
            canvas.AddMenuOption(x + 30, footerY, 8, "Reorder (keys) (8)", AsciiArtAssets.Colors.White, reorderBtn.IsHovered);
            currentLineCount++;
            canvas.AddMenuOption(x + 2, footerY + 1, 9, "Add all from pool (9)", AsciiArtAssets.Colors.White, addAllBtn.IsHovered);
            currentLineCount++;

            return currentLineCount;
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

                        canvas.AddText(x + 4, y, ActionDisplayFormatter.GetActionDescription(action), AsciiArtAssets.Colors.Gray);
                        y++;
                        currentLineCount++;

                        canvas.AddText(x + 4, y, ActionDisplayFormatter.GetActionStats(action), AsciiArtAssets.Colors.Gray);
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

                        canvas.AddText(x + 4, y, $"  {action.Description}", AsciiArtAssets.Colors.Gray);
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
