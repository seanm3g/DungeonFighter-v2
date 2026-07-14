using System;
using RPGGame;
using RPGGame.ActionInteractionLab;
using RPGGame.UI;
using RPGGame.UI.Avalonia.Managers;
using RPGGame.UI.Avalonia.Renderers.Inventory;

namespace RPGGame.UI.Avalonia.ActionInteractionLab
{
    /// <summary>
    /// Renders Action Lab session tools: snapshots, dungeon, turn/d20, and step/sim footer.
    /// Foe types and the action catalog live in <see cref="ActionLabCatalogRenderer"/>.
    /// </summary>
    public static class ActionLabControlsRenderer
    {
        private const int ColX = 2;
        private const int ColWidth = 34;

        public static void Render(GameCanvasControl canvas, ICanvasInteractionManager? interactionManager, ActionInteractionLabSession lab)
        {
            bool interactive = interactionManager != null;
            int x = ColX;
            int rowWidth = ColWidth;
            int y = 1;

            canvas.AddText(x, y, AsciiArtAssets.UIText.CreateHeader("ACTION LAB"), AsciiArtAssets.Colors.Gold);
            y++;
            if (interactive)
            {
                var refreshBtn = InventoryButtonFactory.CreateButton(x, y, rowWidth, "lab_refresh_data", "[ Refresh data ]");
                interactionManager!.AddClickableElement(refreshBtn);
                canvas.AddText(x, y, "[ Refresh data ]", AsciiArtAssets.Colors.Cyan);
            }
            else
                canvas.AddText(x, y, "[ Refresh data ]", AsciiArtAssets.Colors.DarkGray);
            y++;
            y++; // blank before Snapshots

            // Character snapshots
            canvas.AddText(x, y, "Snapshots", AsciiArtAssets.Colors.Gold);
            y++;
            var snapNames = CharacterLabSnapshotService.ListNames();
            int snapVisible = ActionInteractionLabSession.SnapshotListVisibleRowCount;
            if (snapNames.Count > snapVisible)
                lab.SnapshotScrollOffset = Math.Max(0, Math.Min(lab.SnapshotScrollOffset, snapNames.Count - snapVisible));
            else
                lab.SnapshotScrollOffset = 0;
            if (interactive && snapNames.Count > snapVisible)
            {
                var sUp = InventoryButtonFactory.CreateButton(x, y, rowWidth, "lab_snap_up", "▲ snaps");
                interactionManager!.AddClickableElement(sUp);
                canvas.AddText(x, y, "▲ snaps", AsciiArtAssets.Colors.Gray);
            }
            else
                canvas.AddText(x, y, "▲ snaps", AsciiArtAssets.Colors.DarkGray);
            y++;
            for (int i = 0; i < snapVisible; i++)
            {
                int idx = lab.SnapshotScrollOffset + i;
                if (idx >= snapNames.Count)
                {
                    canvas.AddText(x, y, "(empty)", AsciiArtAssets.Colors.DarkGray);
                    y++;
                    continue;
                }

                string sn = snapNames[idx];
                string line = sn.Length > 24 ? sn.Substring(0, 21) + "..." : sn;
                bool picked = string.Equals(sn, lab.SelectedSnapshotName, StringComparison.OrdinalIgnoreCase);
                if (interactive)
                {
                    var btn = InventoryButtonFactory.CreateButton(x, y, rowWidth, $"lab_snap:{idx}", line);
                    interactionManager!.AddClickableElement(btn);
                    canvas.AddText(x, y, line, picked ? AsciiArtAssets.Colors.Yellow : AsciiArtAssets.Colors.White);
                }
                else
                    canvas.AddText(x, y, line, picked ? AsciiArtAssets.Colors.Yellow : AsciiArtAssets.Colors.White);
                y++;
            }
            if (interactive && snapNames.Count > snapVisible)
            {
                var sDn = InventoryButtonFactory.CreateButton(x, y, rowWidth, "lab_snap_down", "▼ snaps");
                interactionManager!.AddClickableElement(sDn);
                canvas.AddText(x, y, "▼ snaps", AsciiArtAssets.Colors.Gray);
            }
            else
                canvas.AddText(x, y, "▼ snaps", AsciiArtAssets.Colors.DarkGray);
            y++;
            if (interactive)
            {
                var loadSnap = InventoryButtonFactory.CreateButton(x, y, 12, "lab_snap_load", "[ Load ]");
                interactionManager!.AddClickableElement(loadSnap);
                canvas.AddText(x, y, "[ Load ]", AsciiArtAssets.Colors.Cyan);
                var refSnap = InventoryButtonFactory.CreateButton(x + 13, y, 12, "lab_snap_refresh", "[ List ]");
                interactionManager!.AddClickableElement(refSnap);
                canvas.AddText(x + 13, y, "[ List ]", AsciiArtAssets.Colors.Gray);
            }
            else
                canvas.AddText(x, y, "[ Load ]  [ List ]", AsciiArtAssets.Colors.DarkGray);
            y++;
            if (!string.IsNullOrEmpty(lab.SnapshotStatusMessage))
            {
                string st = lab.SnapshotStatusMessage;
                if (st.Length > rowWidth)
                    st = st.Substring(0, Math.Max(0, rowWidth - 1)) + "…";
                canvas.AddText(x, y, st, AsciiArtAssets.Colors.DarkGray);
                y++;
            }
            y++; // blank before Dungeon

            // Seeded dungeon scenario
            canvas.AddText(x, y, "Dungeon", AsciiArtAssets.Colors.Gold);
            y++;
            if (string.IsNullOrWhiteSpace(lab.LabDungeonCatalogKey))
            {
                var dungCatalog = ActionLabDungeonFactory.ListCatalogDungeonNames();
                if (dungCatalog.Count > 0)
                    lab.LabDungeonCatalogKey = dungCatalog[0];
            }
            string dungName = lab.LabDungeonCatalogKey;
            if (dungName.Length > 22)
                dungName = dungName.Substring(0, 19) + "...";
            canvas.AddText(x, y, dungName, AsciiArtAssets.Colors.White);
            if (interactive)
            {
                var prevD = InventoryButtonFactory.CreateButton(x + rowWidth - 7, y, 3, "lab_dung_prev", "[<]");
                interactionManager!.AddClickableElement(prevD);
                canvas.AddText(x + rowWidth - 7, y, "[<]", AsciiArtAssets.Colors.Cyan);
                var nextD = InventoryButtonFactory.CreateButton(x + rowWidth - 3, y, 3, "lab_dung_next", "[>]");
                interactionManager!.AddClickableElement(nextD);
                canvas.AddText(x + rowWidth - 3, y, "[>]", AsciiArtAssets.Colors.Cyan);
            }
            y++;
            string deltaSeedPrefix = $"Δlvl {lab.LabDungeonLevelDelta:+#;-#;0}  seed ";
            canvas.AddText(x, y, deltaSeedPrefix, AsciiArtAssets.Colors.DarkGray);
            string dungSeedLabel = $"[{lab.LabDungeonSeed}]";
            int seedX = x + deltaSeedPrefix.Length;
            if (interactive)
            {
                int seedW = Math.Clamp(dungSeedLabel.Length, 5, Math.Max(5, rowWidth - deltaSeedPrefix.Length));
                if (dungSeedLabel.Length > seedW)
                    dungSeedLabel = dungSeedLabel.Substring(0, seedW);
                var seedBtn = InventoryButtonFactory.CreateButton(seedX, y, seedW, "lab_dung_seed_edit", dungSeedLabel);
                interactionManager!.AddClickableElement(seedBtn);
                canvas.AddText(seedX, y, dungSeedLabel, AsciiArtAssets.Colors.Cyan);
            }
            else
                canvas.AddText(seedX, y, dungSeedLabel, AsciiArtAssets.Colors.DarkGray);
            y++;
            if (interactive)
            {
                var dUp = InventoryButtonFactory.CreateButton(x, y, 6, "lab_dung_lv_up", "[+Δ]");
                interactionManager!.AddClickableElement(dUp);
                canvas.AddText(x, y, "[+Δ]", AsciiArtAssets.Colors.Gray);
                var dDn = InventoryButtonFactory.CreateButton(x + 7, y, 6, "lab_dung_lv_dn", "[-Δ]");
                interactionManager!.AddClickableElement(dDn);
                canvas.AddText(x + 7, y, "[-Δ]", AsciiArtAssets.Colors.Gray);
            }
            y++;
            if (interactive)
            {
                var gen = InventoryButtonFactory.CreateButton(x, y, 10, "lab_dung_gen", "[ Gen ]");
                interactionManager!.AddClickableElement(gen);
                canvas.AddText(x, y, "[ Gen ]", AsciiArtAssets.Colors.Cyan);
                var rPrev = InventoryButtonFactory.CreateButton(x + 11, y, 8, "lab_dung_room_prev", "[◀ Rm]");
                interactionManager!.AddClickableElement(rPrev);
                canvas.AddText(x + 11, y, "[◀ Rm]", AsciiArtAssets.Colors.Orange);
                var rNext = InventoryButtonFactory.CreateButton(x + 20, y, 8, "lab_dung_room_next", "[Rm ▶]");
                interactionManager!.AddClickableElement(rNext);
                canvas.AddText(x + 20, y, "[Rm ▶]", AsciiArtAssets.Colors.Orange);
            }
            else
                canvas.AddText(x, y, "[ Gen ] [◀ Rm] [Rm ▶]", AsciiArtAssets.Colors.DarkGray);
            y++;
            string roomCap = lab.FormatLabDungeonRoomCaption();
            if (roomCap.Length > rowWidth)
                roomCap = roomCap.Substring(0, Math.Max(0, rowWidth - 1)) + "…";
            canvas.AddText(x, y, roomCap, AsciiArtAssets.Colors.White);
            y++;
            y++; // blank before turn info

            var next = lab.GetNextActorToAct();
            string who = next == null ? "(time...)" : next == lab.LabPlayer ? "Player" : next == lab.LabEnemy ? "Enemy" : "Env";
            canvas.AddText(x, y, $"Next: {who}", AsciiArtAssets.Colors.Cyan);
            y++;
            canvas.AddText(x, y, $"Actions taken: {lab.LabTotalActionTicks}", AsciiArtAssets.Colors.White);
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
            y++; // blank before d20

            const string d20Prefix = "d20: ";
            canvas.AddText(x, y, d20Prefix, AsciiArtAssets.Colors.White);
            int afterPrefixX = x + d20Prefix.Length;
            if (lab.UseSeededD20)
            {
                string seedLabel = "Seed";
                if (interactive)
                {
                    var seedBtn = InventoryButtonFactory.CreateButton(afterPrefixX, y, 4, "lab_d20_seed", seedLabel);
                    interactionManager!.AddClickableElement(seedBtn);
                }
                canvas.AddText(afterPrefixX, y, seedLabel, AsciiArtAssets.Colors.Yellow);
                int rndX = afterPrefixX + 5;
                if (interactive)
                {
                    var rndBtn = InventoryButtonFactory.CreateButton(rndX, y, 3, "lab_d20_random", "Rnd");
                    interactionManager!.AddClickableElement(rndBtn);
                }
                canvas.AddText(rndX, y, "Rnd", AsciiArtAssets.Colors.Gray);
            }
            else if (lab.UseRandomD20PerStep)
            {
                if (interactive)
                {
                    var seedBtn = InventoryButtonFactory.CreateButton(afterPrefixX, y, 4, "lab_d20_seed", "Seed");
                    interactionManager!.AddClickableElement(seedBtn);
                    canvas.AddText(afterPrefixX, y, "Seed", AsciiArtAssets.Colors.Gray);
                    var rndBtn = InventoryButtonFactory.CreateButton(afterPrefixX + 5, y, 3, "lab_d20_random", "Rnd");
                    interactionManager!.AddClickableElement(rndBtn);
                }
                canvas.AddText(afterPrefixX + 5, y, "Rnd", AsciiArtAssets.Colors.Yellow);
            }
            else
            {
                string numStr = lab.SelectedD20.ToString();
                canvas.AddText(afterPrefixX, y, numStr, AsciiArtAssets.Colors.White);
                int modeX = afterPrefixX + numStr.Length + 1;
                if (interactive)
                {
                    var seedBtn = InventoryButtonFactory.CreateButton(modeX, y, 4, "lab_d20_seed", "Seed");
                    interactionManager!.AddClickableElement(seedBtn);
                    canvas.AddText(modeX, y, "Seed", AsciiArtAssets.Colors.Gray);
                    var rndBtn = InventoryButtonFactory.CreateButton(modeX + 5, y, 3, "lab_d20_random", "Rnd");
                    interactionManager!.AddClickableElement(rndBtn);
                    canvas.AddText(modeX + 5, y, "Rnd", AsciiArtAssets.Colors.Gray);
                }
            }
            y++;
            if (lab.UseSeededD20)
            {
                string seedLine = $"seq seed: {lab.D20SequenceSeed}";
                if (seedLine.Length > rowWidth)
                    seedLine = seedLine.Substring(0, rowWidth);
                canvas.AddText(x, y, seedLine, AsciiArtAssets.Colors.DarkGray);
                if (interactive)
                {
                    var rew = InventoryButtonFactory.CreateButton(x + Math.Min(rowWidth - 8, 16), y, 8, "lab_d20_seed_rewind", "[Rewind]");
                    interactionManager!.AddClickableElement(rew);
                    canvas.AddText(x + Math.Min(rowWidth - 8, 16), y, "[Rewind]", AsciiArtAssets.Colors.Cyan);
                }
                y++;
            }
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
                    bool sel = !lab.UseRandomD20PerStep && !lab.UseSeededD20 && lab.SelectedD20 == n;
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

            // Flow footer directly under the d20 pad (one blank row).
            int footerTopY = d20Row + 4 + 1;
            RenderFooter(canvas, interactionManager, lab, interactive, footerTopY);
        }

        private static void RenderFooter(
            GameCanvasControl canvas,
            ICanvasInteractionManager? interactionManager,
            ActionInteractionLabSession lab,
            bool interactive,
            int footerTopY)
        {
            int x = ColX;
            int y = footerTopY;

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
                var back = InventoryButtonFactory.CreateButton(x, y, 12, "lab_undo", "[ Back ]");
                interactionManager!.AddClickableElement(back);
                canvas.AddText(x, y, "[ Back ]", AsciiArtAssets.Colors.Orange);
                if (lab.CanStepForward)
                {
                    var step = InventoryButtonFactory.CreateButton(x + 14, y, 12, "lab_step", "[ Step ]");
                    interactionManager!.AddClickableElement(step);
                    canvas.AddText(x + 14, y, "[ Step ]", AsciiArtAssets.Colors.Green);
                }
                else
                    canvas.AddText(x + 14, y, "[ Step ]", AsciiArtAssets.Colors.DarkGray);
                y++;
                y++;
                var resetCombo = InventoryButtonFactory.CreateButton(x, y, 20, "lab_reset_combo", "[ Reset ]");
                interactionManager!.AddClickableElement(resetCombo);
                canvas.AddText(x, y, "[ Reset ]", AsciiArtAssets.Colors.Yellow);
                y++;
                y++;
                bool simBusy = lab.IsEncounterSimulationRunning;
                var simColor = simBusy ? AsciiArtAssets.Colors.Yellow : AsciiArtAssets.Colors.Green;
                string simLabel = $"[ Sim {lab.EncounterSimulationBatchCount} ]";
                lab.LastSimBatchWheelMinGridX = x;
                lab.LastSimBatchWheelMaxGridX = x + Math.Max(simLabel.Length, 18) - 1;
                lab.LastSimBatchWheelGridY = simBusy ? -1 : y;
                if (simBusy)
                    canvas.AddText(x, y, simLabel, simColor);
                else
                {
                    var simBtn = InventoryButtonFactory.CreateButton(x, y, Math.Max(simLabel.Length, 18), "lab_sim_run", simLabel);
                    interactionManager!.AddClickableElement(simBtn);
                    canvas.AddText(x, y, simLabel, simColor);
                }

                string dungSimLabel = $"[ Dungeon Sim {lab.EncounterSimulationBatchCount} ]";
                // Narrow tools column: dungeon sim on next row.
                y++;
                if (simBusy)
                    canvas.AddText(x, y, dungSimLabel, AsciiArtAssets.Colors.DarkGray);
                else
                {
                    var dungSim = InventoryButtonFactory.CreateButton(x, y, Math.Max(dungSimLabel.Length, 22), "lab_dung_sim_run", dungSimLabel);
                    interactionManager!.AddClickableElement(dungSim);
                    canvas.AddText(x, y, dungSimLabel, AsciiArtAssets.Colors.Magenta);
                }

                if (simBusy)
                {
                    y++;
                    string running = $"Running {lab.EncounterSimulationBatchCount}…";
                    if (running.Length > ColWidth)
                        running = running.Substring(0, ColWidth - 1) + "…";
                    canvas.AddText(x, y, running, simColor);
                }

                y++;
                if (!simBusy)
                {
                    string threadLabel = lab.UseParallelEncounterSimulation ? "[ Par ]" : "[ 1T ]";
                    var threadColor = lab.UseParallelEncounterSimulation ? AsciiArtAssets.Colors.Cyan : AsciiArtAssets.Colors.Gray;
                    var threadBtn = InventoryButtonFactory.CreateButton(x, y, threadLabel.Length, "lab_sim_parallel_toggle", threadLabel);
                    interactionManager!.AddClickableElement(threadBtn);
                    canvas.AddText(x, y, threadLabel, threadColor);
                    int reqX = x + threadLabel.Length + 1;
                    string reqLabel = lab.IgnoreActionRequirements ? "[ !Req ]" : "[ Req ]";
                    var reqColor = lab.IgnoreActionRequirements ? AsciiArtAssets.Colors.Yellow : AsciiArtAssets.Colors.Cyan;
                    var reqBtn = InventoryButtonFactory.CreateButton(reqX, y, reqLabel.Length, "lab_req_toggle", reqLabel);
                    interactionManager!.AddClickableElement(reqBtn);
                    canvas.AddText(reqX, y, reqLabel, reqColor);
                    y++;
                }
                var exit = InventoryButtonFactory.CreateButton(x, y, 20, "lab_exit", "[ Exit lab ]");
                interactionManager!.AddClickableElement(exit);
                canvas.AddText(x, y, "[ Exit lab ]", AsciiArtAssets.Colors.Red);
            }
            else
            {
                canvas.AddText(x, y, "[◀ strip] [strip ▶]", AsciiArtAssets.Colors.DarkGray);
                y++;
                y++;
                canvas.AddText(x, y, "[ Back ] [ Step ]", AsciiArtAssets.Colors.DarkGray);
                y++;
                y++;
                canvas.AddText(x, y, "[ Reset ]", AsciiArtAssets.Colors.DarkGray);
                y++;
                y++;
                canvas.AddText(x, y, $"[ Sim {lab.EncounterSimulationBatchCount} ]", AsciiArtAssets.Colors.DarkGray);
                y++;
                canvas.AddText(x, y, $"[ Dungeon Sim {lab.EncounterSimulationBatchCount} ]", AsciiArtAssets.Colors.DarkGray);
                y++;
                string threadHint = lab.UseParallelEncounterSimulation ? "[ Par ]" : "[ 1T ]";
                canvas.AddText(x, y, threadHint, AsciiArtAssets.Colors.DarkGray);
                int reqHintX = x + threadHint.Length + 1;
                string reqHint = lab.IgnoreActionRequirements ? "[ !Req ]" : "[ Req ]";
                canvas.AddText(reqHintX, y, reqHint, AsciiArtAssets.Colors.DarkGray);
                y++;
                canvas.AddText(x, y, "[ Exit lab ]", AsciiArtAssets.Colors.DarkGray);
            }
        }
    }
}
