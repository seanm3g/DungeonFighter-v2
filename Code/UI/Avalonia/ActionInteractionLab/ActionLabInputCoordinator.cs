using System;
using System.Collections.Generic;
using System.Globalization;
using System.Threading;
using System.Threading.Tasks;
using Avalonia.Controls;
using RPGGame;
using RPGGame.ActionInteractionLab;

namespace RPGGame.UI.Avalonia.ActionInteractionLab
{
    /// <summary>
    /// Handles clicks for Action Lab control tokens (<c>lab_*</c>) from the main canvas or the tools pop-out window.
    /// </summary>
    public static class ActionLabInputCoordinator
    {
        /// <summary>Prevents fire-and-forget clicks from stacking concurrent Step/Undo/Reset work.</summary>
        private static int _labControlInFlight;

        /// <summary>
        /// Maps Page Up / Page Down to lab step / undo tokens (<c>lab_step</c>, <c>lab_undo</c>).
        /// </summary>
        public static string? MapPageStepInput(string input)
        {
            return input?.Trim().ToLowerInvariant() switch
            {
                "pageup" => "lab_step",
                "pagedown" => "lab_undo",
                _ => null
            };
        }

        private static async Task TryShowSimulationReportAsync(CanvasUICoordinator canvasUI, string title, string body)
        {
            var owner = canvasUI.GetMainWindow();
            try
            {
                await ActionLabSimulationReportDialog.ShowAsync(owner, title, body).ConfigureAwait(true);
            }
            catch (InvalidOperationException)
            {
                string clipped = body.Length > 400 ? body.Substring(0, 400) + "…" : body;
                canvasUI.UpdateStatus($"{title}: {clipped}");
            }
        }

        /// <summary>
        /// Moves <see cref="ActionInteractionLabSession.CatalogScrollOffset"/> by <paramref name="delta"/> rows
        /// (negative = toward earlier names, same as ▲ more). Optionally refreshes combat UI when <paramref name="canvasUI"/> is non-null.
        /// </summary>
        public static void ApplyCatalogScrollOffsetDelta(ActionInteractionLabSession session, int delta, CanvasUICoordinator? canvasUI)
        {
            if (delta == 0) return;
            var names = ActionLoader.GetAllActionNames();
            names.Sort(StringComparer.OrdinalIgnoreCase);
            int visible = session.LastCatalogVisibleRowCount > 0
                ? session.LastCatalogVisibleRowCount
                : ActionInteractionLabSession.LabCatalogVisibleNameRows;
            int maxScroll = Math.Max(0, names.Count - visible);
            if (delta < 0)
                session.CatalogScrollOffset = Math.Max(0, session.CatalogScrollOffset + delta);
            else
                session.CatalogScrollOffset = Math.Min(maxScroll, session.CatalogScrollOffset + delta);

            if (canvasUI != null)
                canvasUI.RenderCombat(session.LabPlayer, session.LabEnemy, new List<string>());
        }

        /// <summary>
        /// Moves <see cref="ActionInteractionLabSession.EnemyCatalogScrollOffset"/> by <paramref name="delta"/> rows
        /// (negative = toward earlier types, same as ▲ types). Optionally refreshes combat UI when <paramref name="canvasUI"/> is non-null.
        /// </summary>
        public static void ApplyEnemyCatalogScrollOffsetDelta(ActionInteractionLabSession session, int delta, CanvasUICoordinator? canvasUI)
        {
            if (delta == 0) return;
            var enemyTypes = EnemyLoader.GetAllEnemyTypes();
            enemyTypes.Sort(StringComparer.OrdinalIgnoreCase);
            int visible = session.LastEnemyCatalogVisibleRowCount > 0
                ? session.LastEnemyCatalogVisibleRowCount
                : ActionInteractionLabSession.EnemyCatalogVisibleRowCount;
            int maxScroll = Math.Max(0, enemyTypes.Count - visible);
            if (delta < 0)
                session.EnemyCatalogScrollOffset = Math.Max(0, session.EnemyCatalogScrollOffset + delta);
            else
                session.EnemyCatalogScrollOffset = Math.Min(maxScroll, session.EnemyCatalogScrollOffset + delta);

            if (canvasUI != null)
                canvasUI.RenderCombat(session.LabPlayer, session.LabEnemy, new List<string>());
        }

        /// <summary>
        /// Wheel over the Action Lab sim row: steps <see cref="ActionInteractionLabSession.EncounterSimulationBatchCount"/>
        /// through 1 / 10 / 100 / 1000 (clamped at 1 and 1000). Positive <paramref name="wheelDeltaY"/> scrolls toward larger counts.
        /// </summary>
        public static void ApplyEncounterSimulationBatchWheel(ActionInteractionLabSession session, double wheelDeltaY, CanvasUICoordinator? canvasUI)
        {
            if (session.IsEncounterSimulationRunning)
                return;
            if (Math.Abs(wheelDeltaY) < 0.1)
                return;
            int direction = wheelDeltaY > 0 ? 1 : -1;
            session.CycleEncounterSimulationBatchCount(direction);
            if (canvasUI != null)
                canvasUI.RenderCombat(session.LabPlayer, session.LabEnemy, new List<string>());
        }

        public static async Task HandleLabControlAsync(string value, CanvasUICoordinator? canvasUI, GameCoordinator? game)
        {
            if (canvasUI == null || game == null) return;
            if (Interlocked.CompareExchange(ref _labControlInFlight, 1, 0) != 0)
                return;

            try
            {
                await HandleLabControlCoreAsync(value, canvasUI, game).ConfigureAwait(true);
            }
            finally
            {
                Interlocked.Exchange(ref _labControlInFlight, 0);
            }
        }

        private static async Task HandleLabControlCoreAsync(string value, CanvasUICoordinator canvasUI, GameCoordinator game)
        {
            var session = ActionInteractionLabSession.Current;
            if (session == null) return;

            void RefreshLabCombat()
            {
                canvasUI.RenderCombat(session.LabPlayer, session.LabEnemy, new List<string>());
            }

            if (value == "lab_exit")
            {
                game.ExitActionInteractionLab();
                try { canvasUI.GetMainWindow()?.Activate(); } catch { /* ignore */ }
                return;
            }

            if (value == "lab_snap_up")
            {
                session.SnapshotScrollOffset = Math.Max(0, session.SnapshotScrollOffset - 1);
                RefreshLabCombat();
                return;
            }

            if (value == "lab_snap_down")
            {
                var snaps = CharacterLabSnapshotService.ListNames();
                int maxScroll = Math.Max(0, snaps.Count - ActionInteractionLabSession.SnapshotListVisibleRowCount);
                session.SnapshotScrollOffset = Math.Min(maxScroll, session.SnapshotScrollOffset + 1);
                RefreshLabCombat();
                return;
            }

            if (value == "lab_snap_refresh")
            {
                session.SnapshotStatusMessage = $"{CharacterLabSnapshotService.ListNames().Count} snapshot(s)";
                RefreshLabCombat();
                return;
            }

            if (value.StartsWith("lab_snap:", StringComparison.Ordinal))
            {
                if (int.TryParse(value.AsSpan("lab_snap:".Length), out int idx))
                {
                    var snaps = CharacterLabSnapshotService.ListNames();
                    if (idx >= 0 && idx < snaps.Count)
                        session.SelectedSnapshotName = snaps[idx];
                }
                RefreshLabCombat();
                return;
            }

            if (value == "lab_snap_load")
            {
                if (string.IsNullOrWhiteSpace(session.SelectedSnapshotName))
                {
                    session.SnapshotStatusMessage = "Select a snapshot first";
                    RefreshLabCombat();
                    return;
                }

                if (session.TryLoadCharacterSnapshotByName(session.SelectedSnapshotName, out var err))
                    session.SnapshotStatusMessage = $"Loaded: {session.SelectedSnapshotName}";
                else
                    session.SnapshotStatusMessage = err ?? "Load failed";
                RefreshLabCombat();
                return;
            }

            if (value == "lab_d20_random")
            {
                session.UseRandomD20PerStep = true;
                session.UseSeededD20 = false;
                RefreshLabCombat();
                return;
            }

            if (value == "lab_d20_seed")
            {
                session.UseRandomD20PerStep = false;
                session.UseSeededD20 = true;
                session.RewindSeededD20Stream();
                RefreshLabCombat();
                return;
            }

            if (value == "lab_d20_seed_rewind")
            {
                session.RewindSeededD20Stream();
                RefreshLabCombat();
                return;
            }

            if (value == "lab_dung_prev")
            {
                session.CycleLabDungeonCatalog(-1);
                RefreshLabCombat();
                return;
            }

            if (value == "lab_dung_next")
            {
                session.CycleLabDungeonCatalog(1);
                RefreshLabCombat();
                return;
            }

            if (value == "lab_dung_lv_up")
            {
                session.NudgeLabDungeonLevelDelta(1);
                RefreshLabCombat();
                return;
            }

            if (value == "lab_dung_lv_dn")
            {
                session.NudgeLabDungeonLevelDelta(-1);
                RefreshLabCombat();
                return;
            }

            if (value == "lab_dung_seed_edit")
            {
                Window? owner = (Window?)ActionLabControlsWindow.CurrentInstance
                    ?? canvasUI.GetMainWindow();
                if (owner == null)
                {
                    canvasUI.UpdateStatus("Cannot open seed dialog (no window).");
                    return;
                }

                string? typed = await LabSnapshotNameDialog.PromptAsync(
                    owner,
                    "Dungeon seed",
                    session.LabDungeonSeed.ToString(CultureInfo.InvariantCulture),
                    "Enter dungeon seed:").ConfigureAwait(true);
                if (typed == null)
                    return;

                if (!int.TryParse(
                        typed.Trim(),
                        NumberStyles.Integer,
                        CultureInfo.InvariantCulture,
                        out int seed))
                {
                    canvasUI.UpdateStatus("Invalid seed — enter an integer.");
                    return;
                }

                session.SetLabDungeonSeed(seed);
                RefreshLabCombat();
                return;
            }

            if (value == "lab_dung_gen")
            {
                session.GenerateLabDungeon();
                RefreshLabCombat();
                return;
            }

            if (value == "lab_dung_room_prev")
            {
                session.TryMoveLabDungeonRoom(-1);
                RefreshLabCombat();
                return;
            }

            if (value == "lab_dung_room_next")
            {
                session.TryMoveLabDungeonRoom(1);
                RefreshLabCombat();
                return;
            }

            if (value.StartsWith("lab_d20:", StringComparison.Ordinal))
            {
                if (int.TryParse(value.AsSpan("lab_d20:".Length), out int d) && d >= 1 && d <= 20)
                {
                    session.UseRandomD20PerStep = false;
                    session.UseSeededD20 = false;
                    session.SelectedD20 = d;
                }
                RefreshLabCombat();
                return;
            }

            if (value.StartsWith("lab_act:", StringComparison.Ordinal))
            {
                if (int.TryParse(value.AsSpan("lab_act:".Length), out int idx))
                {
                    var names = ActionLoader.GetAllActionNames();
                    names.Sort(StringComparer.OrdinalIgnoreCase);
                    if (idx >= 0 && idx < names.Count)
                    {
                        session.SelectedCatalogActionName = names[idx];
                        session.AddSelectedCatalogActionToComboStrip();
                    }
                }
                return;
            }

            if (value == "lab_combo_prev")
            {
                session.NudgeLabPlayerComboStep(-1);
                return;
            }

            if (value == "lab_combo_next")
            {
                session.NudgeLabPlayerComboStep(1);
                return;
            }

            if (value == "lab_scrl:up")
            {
                ApplyCatalogScrollOffsetDelta(session, -1, canvasUI);
                return;
            }

            if (value == "lab_scrl:down")
            {
                ApplyCatalogScrollOffsetDelta(session, +1, canvasUI);
                return;
            }

            if (value == "lab_enemy_up")
            {
                ApplyEnemyCatalogScrollOffsetDelta(session, -1, canvasUI);
                return;
            }

            if (value == "lab_enemy_down")
            {
                ApplyEnemyCatalogScrollOffsetDelta(session, +1, canvasUI);
                return;
            }

            if (value.StartsWith("lab_enemy:", StringComparison.Ordinal))
            {
                if (int.TryParse(value.AsSpan("lab_enemy:".Length), out int idx))
                {
                    var enemyTypes = EnemyLoader.GetAllEnemyTypes();
                    enemyTypes.Sort(StringComparer.OrdinalIgnoreCase);
                    if (idx >= 0 && idx < enemyTypes.Count)
                        session.SetLabEnemyFromLoader(enemyTypes[idx], 1);
                }
                return;
            }

            if (value == "lab_reset_combo")
            {
                await session.ResetLabEncounterAsync().ConfigureAwait(true);
                return;
            }

            if (value == "lab_refresh_data")
            {
                if (session.IsEncounterSimulationRunning)
                    return;
                await session.RefreshGameDataAsync().ConfigureAwait(true);
                return;
            }

            if (value == "lab_step")
            {
                if (!session.CanStepForward)
                    return;
                await session.StepAsync(session.ResolveD20ForNextStep(), session.SelectedCatalogActionName).ConfigureAwait(true);
                return;
            }

            if (value == "lab_sim_parallel_toggle")
            {
                if (session.IsEncounterSimulationRunning)
                    return;
                session.UseParallelEncounterSimulation = !session.UseParallelEncounterSimulation;
                RefreshLabCombat();
                return;
            }

            if (value == "lab_req_toggle")
            {
                session.IgnoreActionRequirements = !session.IgnoreActionRequirements;
                RefreshLabCombat();
                return;
            }

            if (value == "lab_sim_run")
            {
                var snapshot = session.CaptureSimulationSnapshot();
                var validationError = ActionLabEncounterSimulator.ValidateSnapshot(snapshot);
                if (validationError != null)
                {
                    await TryShowSimulationReportAsync(canvasUI, "Action Lab — simulation", validationError)
                        .ConfigureAwait(true);
                    return;
                }

                session.SetEncounterSimulationRunning(true);
                canvasUI.RenderCombat(session.LabPlayer, session.LabEnemy, new List<string>());
                try
                {
                    int n = session.EncounterSimulationBatchCount;
                    int dop = session.UseParallelEncounterSimulation ? -1 : 1;
                    var report = await Task.Run(() =>
                            ActionLabEncounterSimulator.RunBatchAsync(snapshot, n, Random.Shared, maxDegreeOfParallelism: dop))
                        .ConfigureAwait(true);
                    session.RecordEncounterSimulationTurns(report);
                    string body = ActionLabEncounterReportFormatter.FormatReportText(report, snapshot);
                    // End busy state before the modal so the footer does not still show "Running…" behind it.
                    session.SetEncounterSimulationRunning(false);
                    canvasUI.RenderCombat(session.LabPlayer, session.LabEnemy, new List<string>());
                    await TryShowSimulationReportAsync(canvasUI, $"Action Lab — {n} encounters", body)
                        .ConfigureAwait(true);
                }
                catch (Exception ex)
                {
                    session.SetEncounterSimulationRunning(false);
                    canvasUI.RenderCombat(session.LabPlayer, session.LabEnemy, new List<string>());
                    await TryShowSimulationReportAsync(canvasUI, "Action Lab — simulation error", ex.ToString())
                        .ConfigureAwait(true);
                }
                finally
                {
                    session.SetEncounterSimulationRunning(false);
                    canvasUI.RenderCombat(session.LabPlayer, session.LabEnemy, new List<string>());
                }

                return;
            }

            if (value == "lab_dung_sim_run")
            {
                var snapshot = session.CaptureSimulationSnapshot();
                var validationError = ActionLabDungeonSimulator.ValidateSnapshot(snapshot);
                if (validationError != null)
                {
                    await TryShowSimulationReportAsync(canvasUI, "Action Lab — dungeon sim", validationError)
                        .ConfigureAwait(true);
                    return;
                }

                if (string.IsNullOrWhiteSpace(session.LabDungeonCatalogKey))
                {
                    var names = ActionLabDungeonFactory.ListCatalogDungeonNames();
                    if (names.Count > 0)
                        session.LabDungeonCatalogKey = names[0];
                }

                session.SetEncounterSimulationRunning(true);
                canvasUI.RenderCombat(session.LabPlayer, session.LabEnemy, new List<string>());
                try
                {
                    int n = session.EncounterSimulationBatchCount;
                    int level = Math.Clamp(session.LabPlayer.Level + session.LabDungeonLevelDelta, 1, 99);
                    string catalog = session.LabDungeonCatalogKey;
                    int seed = session.LabDungeonSeed;
                    int dop = session.UseParallelEncounterSimulation ? -1 : 1;
                    var report = await Task.Run(() =>
                            ActionLabDungeonSimulator.RunBatchAsync(
                                snapshot, catalog, level, seed, n, varySeedPerRun: true, maxDegreeOfParallelism: dop))
                        .ConfigureAwait(true);
                    string body = ActionLabDungeonSimulator.FormatReportText(report);
                    session.SetEncounterSimulationRunning(false);
                    canvasUI.RenderCombat(session.LabPlayer, session.LabEnemy, new List<string>());
                    await TryShowSimulationReportAsync(canvasUI, $"Action Lab — {n} dungeon runs", body)
                        .ConfigureAwait(true);
                }
                catch (Exception ex)
                {
                    session.SetEncounterSimulationRunning(false);
                    canvasUI.RenderCombat(session.LabPlayer, session.LabEnemy, new List<string>());
                    await TryShowSimulationReportAsync(canvasUI, "Action Lab — dungeon sim error", ex.ToString())
                        .ConfigureAwait(true);
                }
                finally
                {
                    session.SetEncounterSimulationRunning(false);
                    canvasUI.RenderCombat(session.LabPlayer, session.LabEnemy, new List<string>());
                }

                return;
            }

            if (value == "lab_undo")
            {
                await session.UndoLastStepAsync().ConfigureAwait(true);
            }
        }
    }
}
