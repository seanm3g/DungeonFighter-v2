using System;
using RPGGame;
using RPGGame.ActionInteractionLab;
using RPGGame.UI;
using RPGGame.UI.Avalonia.Managers;
using RPGGame.UI.Avalonia.Renderers.Inventory;
namespace RPGGame.UI.Avalonia.ActionInteractionLab
{
    /// <summary>
    /// Renders Action Lab tooling (catalog, d20, step controls) on a canvas — typically the auxiliary Action Lab pop-out window.
    /// </summary>
    public static class ActionLabControlsRenderer
    {
        public static void Render(GameCanvasControl canvas, ICanvasInteractionManager? interactionManager, ActionInteractionLabSession lab)
        {
            int x = 2;
            int y = 1;
            int panelBottom = Math.Max(y + 1, canvas.GridHeight - 2);
            int rowWidth = Math.Max(8, canvas.GridWidth - 4);
            bool interactive = interactionManager != null;

            var names = ActionLoader.GetAllActionNames();
            names.Sort(StringComparer.OrdinalIgnoreCase);

            const int footerRows = 5;
            int footerTopY = panelBottom - footerRows + 1;

            canvas.AddText(x, y, AsciiArtAssets.UIText.CreateHeader("ACTION LAB"), AsciiArtAssets.Colors.Gold);
            y += 2;

            var enemyTypes = EnemyLoader.GetAllEnemyTypes();
            enemyTypes.Sort(StringComparer.OrdinalIgnoreCase);
            const int enemyVisible = 2;
            if (enemyTypes.Count > enemyVisible)
                lab.EnemyCatalogScrollOffset = Math.Max(0, Math.Min(lab.EnemyCatalogScrollOffset, enemyTypes.Count - enemyVisible));
            else
                lab.EnemyCatalogScrollOffset = 0;

            string enemyLabel = lab.LabEnemy.Name;
            if (enemyLabel.Length > 22)
                enemyLabel = enemyLabel.Substring(0, 19) + "...";
            canvas.AddText(x, y, "Foe: " + enemyLabel, AsciiArtAssets.Colors.Magenta);
            y++;
            var labEnemy = lab.LabEnemy;
            canvas.AddText(x, y,
                $"HP: {labEnemy.CurrentHealth}/{labEnemy.MaxHealth}",
                AsciiArtAssets.Colors.White);
            y++;
            if (interactive && enemyTypes.Count > 0)
            {
                var eUp = InventoryButtonFactory.CreateButton(x, y, rowWidth, "lab_enemy_up", "▲ types");
                interactionManager!.AddClickableElement(eUp);
                canvas.AddText(x, y, "▲ types", AsciiArtAssets.Colors.Gray);
            }
            else
                canvas.AddText(x, y, "▲", AsciiArtAssets.Colors.DarkGray);
            y++;
            for (int i = 0; i < enemyVisible && lab.EnemyCatalogScrollOffset + i < enemyTypes.Count; i++)
            {
                int idx = lab.EnemyCatalogScrollOffset + i;
                string t = enemyTypes[idx];
                string line = t.Length > 24 ? t.Substring(0, 21) + "..." : t;
                if (interactive)
                {
                    var btn = InventoryButtonFactory.CreateButton(x, y, rowWidth, $"lab_enemy:{idx}", line);
                    interactionManager!.AddClickableElement(btn);
                    canvas.AddText(x, y, line, AsciiArtAssets.Colors.White);
                }
                else
                    canvas.AddText(x, y, line, AsciiArtAssets.Colors.White);
                y++;
            }
            if (interactive && enemyTypes.Count > enemyVisible)
            {
                var eDn = InventoryButtonFactory.CreateButton(x, y, rowWidth, "lab_enemy_down", "▼ types");
                interactionManager!.AddClickableElement(eDn);
                canvas.AddText(x, y, "▼ types", AsciiArtAssets.Colors.Gray);
            }
            else if (enemyTypes.Count > enemyVisible)
                canvas.AddText(x, y, "▼", AsciiArtAssets.Colors.DarkGray);
            y++;

            var next = lab.GetNextActorToAct();
            string who = next == null ? "(time...)" : next == lab.LabPlayer ? "Player" : next == lab.LabEnemy ? "Enemy" : "Env";
            canvas.AddText(x, y, $"Next: {who}", AsciiArtAssets.Colors.Cyan);
            y++;
            var heroCombo = lab.LabPlayer.GetComboActions();
            if (heroCombo.Count > 0)
            {
                int stepNum = (lab.LabPlayer.ComboStep % heroCombo.Count) + 1;
                canvas.AddText(x, y, $"Strip step: {stepNum}/{heroCombo.Count}", AsciiArtAssets.Colors.White);
            }
            else
                canvas.AddText(x, y, "Strip step: (use catalog)", AsciiArtAssets.Colors.DarkGray);
            y++;
            canvas.AddText(x, y, "Slot/roll mods: Next Player", AsciiArtAssets.Colors.DarkGray);
            y++;

            const string d20Prefix = "d20 sel: ";
            canvas.AddText(x, y, d20Prefix, AsciiArtAssets.Colors.White);
            int afterPrefixX = x + d20Prefix.Length;
            if (lab.UseRandomD20PerStep)
            {
                if (interactive)
                {
                    var rndBtn = InventoryButtonFactory.CreateButton(afterPrefixX, y, 3, "lab_d20_random", "Rnd");
                    interactionManager!.AddClickableElement(rndBtn);
                }
                canvas.AddText(afterPrefixX, y, "Rnd", AsciiArtAssets.Colors.Yellow);
            }
            else
            {
                string numStr = lab.SelectedD20.ToString();
                canvas.AddText(afterPrefixX, y, numStr, AsciiArtAssets.Colors.White);
                if (interactive)
                {
                    int rndX = afterPrefixX + numStr.Length + 1;
                    var rndBtn = InventoryButtonFactory.CreateButton(rndX, y, 3, "lab_d20_random", "Rnd");
                    interactionManager!.AddClickableElement(rndBtn);
                    canvas.AddText(rndX, y, "Rnd", AsciiArtAssets.Colors.Gray);
                }
            }
            y++;
            int d20Row = y;
            for (int row = 0; row < 4; row++)
            {
                int colY = d20Row + row;
                for (int c = 0; c < 5; c++)
                {
                    int n = row * 5 + c + 1;
                    if (n > 20) break;
                    string label = n.ToString().PadLeft(2);
                    int colX = x + c * 6;
                    bool sel = !lab.UseRandomD20PerStep && lab.SelectedD20 == n;
                    if (interactive)
                    {
                        var btn = InventoryButtonFactory.CreateButton(colX, colY, 5, $"lab_d20:{n}", label);
                        interactionManager!.AddClickableElement(btn);
                        canvas.AddText(colX, colY, label, sel ? AsciiArtAssets.Colors.Yellow : AsciiArtAssets.Colors.White);
                    }
                    else
                        canvas.AddText(colX, colY, label, sel ? AsciiArtAssets.Colors.Yellow : AsciiArtAssets.Colors.White);
                }
            }
            y = d20Row + 5;

            string selName = lab.SelectedCatalogActionName;
            if (selName.Length > 24)
                selName = selName.Substring(0, 21) + "...";
            canvas.AddText(x, y, "Sel: " + selName, AsciiArtAssets.Colors.Silver);
            y++;

            int catalogWheelMinY = y;
            if (interactive)
            {
                var upBtn = InventoryButtonFactory.CreateButton(x, y, rowWidth, "lab_scrl:up", "▲ more");
                interactionManager!.AddClickableElement(upBtn);
                canvas.AddText(x, y, "▲ more", AsciiArtAssets.Colors.Gray);
            }
            else
                canvas.AddText(x, y, "▲", AsciiArtAssets.Colors.Gray);
            y++;

            int catalogSlotCount = Math.Max(0, footerTopY - y - 1);
            int maxScroll = Math.Max(0, names.Count - catalogSlotCount);
            lab.CatalogScrollOffset = Math.Max(0, Math.Min(lab.CatalogScrollOffset, maxScroll));
            int maxLines = Math.Min(catalogSlotCount, Math.Max(0, names.Count - lab.CatalogScrollOffset));
            lab.LastCatalogVisibleRowCount = maxLines;
            for (int i = 0; i < maxLines && lab.CatalogScrollOffset + i < names.Count; i++)
            {
                int idx = lab.CatalogScrollOffset + i;
                string nm = names[idx];
                string line = nm.Length > 26 ? nm.Substring(0, 23) + "..." : nm;
                bool picked = string.Equals(names[idx], lab.SelectedCatalogActionName, StringComparison.Ordinal);
                if (interactive)
                {
                    var btn = InventoryButtonFactory.CreateButton(x, y, rowWidth, $"lab_act:{idx}", line);
                    interactionManager!.AddClickableElement(btn);
                    canvas.AddText(x, y, line, picked ? AsciiArtAssets.Colors.Yellow : AsciiArtAssets.Colors.White);
                }
                else
                    canvas.AddText(x, y, line, picked ? AsciiArtAssets.Colors.Yellow : AsciiArtAssets.Colors.White);
                y++;
            }

            if (interactive)
            {
                var dnBtn = InventoryButtonFactory.CreateButton(x, y, rowWidth, "lab_scrl:down", "▼ more");
                interactionManager!.AddClickableElement(dnBtn);
                canvas.AddText(x, y, "▼ more", AsciiArtAssets.Colors.Gray);
            }
            else
                canvas.AddText(x, y, "▼", AsciiArtAssets.Colors.Gray);
            int catalogWheelMaxY = y;
            y++;

            lab.LastCatalogWheelMinGridX = x;
            lab.LastCatalogWheelMaxGridX = x + rowWidth - 1;
            lab.LastCatalogWheelMinGridY = catalogWheelMinY;
            lab.LastCatalogWheelMaxGridY = catalogWheelMaxY;

            y = footerTopY;
            if (interactive)
            {
                var stripPrev = InventoryButtonFactory.CreateButton(x, y, 13, "lab_combo_prev", "[◀ strip]");
                interactionManager!.AddClickableElement(stripPrev);
                canvas.AddText(x, y, "[◀ strip]", AsciiArtAssets.Colors.Cyan);
                var stripNext = InventoryButtonFactory.CreateButton(x + 14, y, 13, "lab_combo_next", "[strip ▶]");
                interactionManager!.AddClickableElement(stripNext);
                canvas.AddText(x + 14, y, "[strip ▶]", AsciiArtAssets.Colors.Cyan);
                y++;
                y++;
                var step = InventoryButtonFactory.CreateButton(x, y, 12, "lab_step", "[ Step ]");
                interactionManager!.AddClickableElement(step);
                canvas.AddText(x, y, "[ Step ]", AsciiArtAssets.Colors.Green);
                var back = InventoryButtonFactory.CreateButton(x + 14, y, 12, "lab_undo", "[ Back ]");
                interactionManager!.AddClickableElement(back);
                canvas.AddText(x + 14, y, "[ Back ]", AsciiArtAssets.Colors.Orange);
                y++;
                var resetCombo = InventoryButtonFactory.CreateButton(x, y, rowWidth, "lab_reset_combo", "[ Reset combo ]");
                interactionManager!.AddClickableElement(resetCombo);
                canvas.AddText(x, y, "[ Reset combo ]", AsciiArtAssets.Colors.Yellow);
                y++;
                var exit = InventoryButtonFactory.CreateButton(x, y, rowWidth, "lab_exit", "[ Exit lab ]");
                interactionManager!.AddClickableElement(exit);
                canvas.AddText(x, y, "[ Exit lab ]", AsciiArtAssets.Colors.Red);
            }
            else
            {
                canvas.AddText(x, y, "[◀ strip] [strip ▶]", AsciiArtAssets.Colors.DarkGray);
                y++;
                y++;
                canvas.AddText(x, y, "[ Step ] [ Back ]", AsciiArtAssets.Colors.DarkGray);
                y++;
                canvas.AddText(x, y, "[ Reset combo ]", AsciiArtAssets.Colors.DarkGray);
                y++;
                canvas.AddText(x, y, "[ Exit lab ]", AsciiArtAssets.Colors.DarkGray);
            }
        }
    }
}
