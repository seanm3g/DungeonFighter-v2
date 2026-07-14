using System;
using System.Collections.Generic;
using RPGGame;
using RPGGame.ActionInteractionLab;
using RPGGame.Tuning;
using RPGGame.UI;
using RPGGame.UI.Avalonia.Managers;
using RPGGame.UI.Avalonia.Renderers.Inventory;

namespace RPGGame.UI.Avalonia.ActionInteractionLab
{
    /// <summary>
    /// Renders foes (type picker) and the action catalog for the Action Lab catalog pop-out.
    /// Two columns: left = enemy, right = actions.
    /// </summary>
    public static class ActionLabCatalogRenderer
    {
        private const int LeftColX = 2;
        private const int LeftColWidth = 34;
        private const int RightColX = 38;
        private const int RightColWidth = 32;

        public static void Render(GameCanvasControl canvas, ICanvasInteractionManager? interactionManager, ActionInteractionLabSession lab)
        {
            int panelBottom = Math.Max(2, canvas.GridHeight - 2);
            bool interactive = interactionManager != null;

            var names = ActionLoader.GetAllActionNames();
            names.Sort(StringComparer.OrdinalIgnoreCase);

            int y = 1;
            canvas.AddText(LeftColX, y, AsciiArtAssets.UIText.CreateHeader("LAB PICKERS"), AsciiArtAssets.Colors.Gold);
            y++;

            RenderFoeColumn(canvas, interactionManager, lab, interactive, y, panelBottom);
            RenderActionsColumn(canvas, interactionManager, lab, interactive, names, y, panelBottom);

            for (int divY = y; divY <= panelBottom; divY++)
                canvas.AddText(RightColX - 2, divY, "|", AsciiArtAssets.Colors.DarkGray);
        }

        private static void RenderFoeColumn(
            GameCanvasControl canvas,
            ICanvasInteractionManager? interactionManager,
            ActionInteractionLabSession lab,
            bool interactive,
            int startY,
            int panelBottom)
        {
            int x = LeftColX;
            int rowWidth = LeftColWidth;
            int y = startY;

            var enemyTypes = EnemyLoader.GetAllEnemyTypes();
            enemyTypes.Sort(StringComparer.OrdinalIgnoreCase);

            canvas.AddText(x, y, "Foes", AsciiArtAssets.Colors.Gold);
            y++;

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
            var enemyData = EnemyLoader.GetEnemyData(labEnemy.Name);
            if (enemyData != null)
            {
                var hpBreakdown = EnemyProgressionCurveEvaluator.GetHealthBreakdown(enemyData, labEnemy.Level);
                string factorsLine = hpBreakdown.FormatCompactLine();
                if (factorsLine.Length > rowWidth)
                    factorsLine = factorsLine.Substring(0, Math.Max(0, rowWidth - 1)) + "…";
                canvas.AddText(x, y, factorsLine, AsciiArtAssets.Colors.DarkGray);
                y++;
            }

            int enemyWheelMinY = y;
            if (interactive && enemyTypes.Count > 0)
            {
                var eUp = InventoryButtonFactory.CreateButton(x, y, rowWidth, "lab_enemy_up", "▲ types");
                interactionManager!.AddClickableElement(eUp);
                canvas.AddText(x, y, "▲ types", AsciiArtAssets.Colors.Gray);
            }
            else
                canvas.AddText(x, y, "▲", AsciiArtAssets.Colors.DarkGray);
            y++;

            // Fill remaining left-column height (same as the actions column).
            int enemySlotCount = Math.Max(0, panelBottom - y - 1);
            int maxScroll = Math.Max(0, enemyTypes.Count - enemySlotCount);
            lab.EnemyCatalogScrollOffset = Math.Max(0, Math.Min(lab.EnemyCatalogScrollOffset, maxScroll));
            lab.LastEnemyCatalogVisibleRowCount = enemySlotCount;
            int maxTypeRows = Math.Min(enemySlotCount, Math.Max(0, enemyTypes.Count - lab.EnemyCatalogScrollOffset));
            for (int i = 0; i < maxTypeRows; i++)
            {
                int idx = lab.EnemyCatalogScrollOffset + i;
                string t = enemyTypes[idx];
                string line = t.Length > 24 ? t.Substring(0, 21) + "..." : t;
                bool picked = string.Equals(t, lab.LabEnemy.Name, StringComparison.OrdinalIgnoreCase);
                if (interactive)
                {
                    var btn = InventoryButtonFactory.CreateButton(x, y, rowWidth, $"lab_enemy:{idx}", line);
                    interactionManager!.AddClickableElement(btn);
                    canvas.AddText(x, y, line, picked ? AsciiArtAssets.Colors.Yellow : AsciiArtAssets.Colors.White);
                }
                else
                    canvas.AddText(x, y, line, picked ? AsciiArtAssets.Colors.Yellow : AsciiArtAssets.Colors.White);
                y++;
            }

            if (interactive)
            {
                var eDn = InventoryButtonFactory.CreateButton(x, y, rowWidth, "lab_enemy_down", "▼ types");
                interactionManager!.AddClickableElement(eDn);
                canvas.AddText(x, y, "▼ types", AsciiArtAssets.Colors.Gray);
            }
            else
                canvas.AddText(x, y, "▼", AsciiArtAssets.Colors.DarkGray);
            int enemyWheelMaxY = y;

            lab.LastEnemyCatalogWheelMinGridX = x;
            lab.LastEnemyCatalogWheelMaxGridX = x + rowWidth - 1;
            lab.LastEnemyCatalogWheelMinGridY = enemyWheelMinY;
            lab.LastEnemyCatalogWheelMaxGridY = enemyWheelMaxY;
        }

        private static void RenderActionsColumn(
            GameCanvasControl canvas,
            ICanvasInteractionManager? interactionManager,
            ActionInteractionLabSession lab,
            bool interactive,
            List<string> names,
            int startY,
            int panelBottom)
        {
            int x = RightColX;
            int rowWidth = RightColWidth;
            int y = startY;

            canvas.AddText(x, y, "Actions", AsciiArtAssets.Colors.Gold);
            y++;

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

            int catalogSlotCount = Math.Max(0, panelBottom - y - 1);
            int maxScroll = Math.Max(0, names.Count - catalogSlotCount);
            lab.CatalogScrollOffset = Math.Max(0, Math.Min(lab.CatalogScrollOffset, maxScroll));
            int maxLines = Math.Min(catalogSlotCount, Math.Max(0, names.Count - lab.CatalogScrollOffset));
            lab.LastCatalogVisibleRowCount = maxLines;
            for (int i = 0; i < maxLines && lab.CatalogScrollOffset + i < names.Count; i++)
            {
                int idx = lab.CatalogScrollOffset + i;
                string nm = names[idx];
                string line = nm.Length > 28 ? nm.Substring(0, 25) + "..." : nm;
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

            lab.LastCatalogWheelMinGridX = x;
            lab.LastCatalogWheelMaxGridX = x + rowWidth - 1;
            lab.LastCatalogWheelMinGridY = catalogWheelMinY;
            lab.LastCatalogWheelMaxGridY = y;
        }
    }
}
