using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using RPGGame.BattleStatistics;
using RPGGame.Combat;

namespace RPGGame.ActionInteractionLab
{
    /// <summary>
    /// Headless multi-room dungeon runs for Action Lab (seeded generation + encounter loop per hostile room).
    /// </summary>
    public static class ActionLabDungeonSimulator
    {
        private static readonly SemaphoreSlim RunMutex = new(1, 1);

        public static string? ValidateSnapshot(LabCombatSnapshot snapshot) =>
            ActionLabEncounterSimulator.ValidateSnapshot(snapshot);

        public static async Task<ActionLabDungeonSimulationReport> RunBatchAsync(
            LabCombatSnapshot snapshot,
            string catalogKey,
            int dungeonLevel,
            int baseSeed,
            int runCount,
            bool varySeedPerRun = true,
            int maxDegreeOfParallelism = 1)
        {
            if (runCount < 1)
                throw new ArgumentOutOfRangeException(nameof(runCount));
            var validation = ValidateSnapshot(snapshot);
            if (validation != null)
                throw new InvalidOperationException(validation);

            var report = new ActionLabDungeonSimulationReport
            {
                CatalogKey = catalogKey,
                DungeonLevel = dungeonLevel,
                BaseSeed = baseSeed
            };
            var wall = Stopwatch.StartNew();
            ActionLoader.ReloadActions();
            EnemyLoader.LoadEnemies();

            int dop = maxDegreeOfParallelism <= 0
                ? Math.Max(1, global::System.Environment.ProcessorCount)
                : maxDegreeOfParallelism;

            using (CombatUiMuteScope.Begin(muted: true))
            {
                if (dop <= 1)
                {
                    for (int i = 0; i < runCount; i++)
                    {
                        int seed = varySeedPerRun
                            ? ActionLabDungeonFactory.DeriveDungeonRunSeed(baseSeed, i)
                            : baseSeed;
                        report.Runs.Add(await RunSingleDungeonAsync(snapshot, catalogKey, dungeonLevel, seed)
                            .ConfigureAwait(false));
                    }
                }
                else
                {
                    var results = new ActionLabDungeonRunMetrics[runCount];
                    var options = new ParallelOptions { MaxDegreeOfParallelism = dop };
                    await Parallel.ForEachAsync(
                        Enumerable.Range(0, runCount),
                        options,
                        async (i, ct) =>
                        {
                            int seed = varySeedPerRun
                                ? ActionLabDungeonFactory.DeriveDungeonRunSeed(baseSeed, i)
                                : baseSeed;
                            await RunMutex.WaitAsync(ct).ConfigureAwait(false);
                            try
                            {
                                results[i] = await RunSingleDungeonAsync(snapshot, catalogKey, dungeonLevel, seed)
                                    .ConfigureAwait(false);
                            }
                            finally
                            {
                                RunMutex.Release();
                            }
                        }).ConfigureAwait(false);
                    report.Runs.AddRange(results);
                }
            }

            Aggregate(report);
            wall.Stop();
            report.SimulationWallElapsed = wall.Elapsed;
            return report;
        }

        public static async Task<ActionLabDungeonRunMetrics> RunSingleDungeonAsync(
            LabCombatSnapshot snapshot,
            string catalogKey,
            int dungeonLevel,
            int seed)
        {
            var metrics = new ActionLabDungeonRunMetrics { SeedUsed = seed };
            try
            {
                var gen = ActionLabDungeonFactory.Generate(catalogKey, dungeonLevel, seed);
                metrics.SeedUsed = gen.SeedUsed;
                var rooms = ActionLabDungeonFactory.GetHostileRooms(gen.Dungeon).ToList();
                if (rooms.Count == 0)
                    rooms = gen.Dungeon.Rooms.ToList();
                metrics.HostileRoomCount = rooms.Count;

                var player = LabCombatEntityFactory.ClonePlayerFromJson(
                    snapshot.InitialPlayerJson,
                    "Lab dungeon sim: invalid player JSON.");
                LabCombatEntityFactory.ApplyPanelDeltas(player, snapshot);
                LabCombatEntityFactory.ReapplyComboStrip(player, snapshot.ComboStripActionNames);
                player.CurrentHealth = player.GetEffectiveMaxHealth();

                var forced = ActionLoader.GetAction(snapshot.SelectedCatalogActionName.Trim());
                if (forced == null)
                {
                    metrics.ErrorMessage = $"Unknown action '{snapshot.SelectedCatalogActionName}'.";
                    return metrics;
                }
                if (!forced.IsComboAction)
                    forced.IsComboAction = true;

                var rng = new Random(seed);
                for (int ri = 0; ri < rooms.Count; ri++)
                {
                    var room = rooms[ri];
                    var src = room.GetEnemies().FirstOrDefault(e => e.IsAlive) ?? room.GetEnemies().FirstOrDefault();
                    if (src == null)
                    {
                        metrics.RoomsCleared++;
                        continue;
                    }

                    Enemy enemy = EnemyLoader.CreateEnemy(src.Name, Math.Clamp(src.Level, 1, 99))
                                  ?? src;
                    enemy.CurrentHealth = enemy.GetEffectiveMaxHealth();

                    bool won = await FightRoomAsync(player, enemy, room, forced, rng).ConfigureAwait(false);
                    if (!won || !player.IsAlive || player.CurrentHealth <= 0)
                    {
                        metrics.DeathRoomIndex = ri;
                        metrics.PlayerFinalHealth = Math.Max(0, player.CurrentHealth);
                        metrics.ClearedDungeon = false;
                        return metrics;
                    }

                    metrics.RoomsCleared++;
                    player.Facade.ClearAllTempEffects();
                    player.ComboStep = 0;
                }

                metrics.ClearedDungeon = true;
                metrics.PlayerFinalHealth = player.CurrentHealth;
            }
            catch (Exception ex)
            {
                metrics.ErrorMessage = $"{ex.GetType().Name}: {ex.Message}";
            }

            return metrics;
        }

        private static async Task<bool> FightRoomAsync(
            Character player,
            Enemy enemy,
            Environment room,
            RPGGame.Action forced,
            Random rng)
        {
            using var ticker = GameTicker.BeginIsolatedEncounterGameTime();
            var combatManager = new CombatManager();
            bool narrativeStarted = false;
            try
            {
                GameTicker.Instance.Reset();
                player.ComboStep = 0;
                enemy.ComboStep = 0;
                combatManager.StartBattleNarrative(player.Name, enemy.Name, room.Name, player.CurrentHealth, enemy.CurrentHealth);
                narrativeStarted = true;
                combatManager.InitializeCombatEntities(player, enemy, room, playerGetsFirstAttack: true, enemyGetsFirstAttack: false);
                room.ResetForNewFight();
                Dice.ClearAsyncForcedD20Rolls();

                int safety = 0;
                const int maxAdvances = 50_000;
                while (player.IsAlive && enemy.IsAlive && safety++ < maxAdvances)
                {
                    int d20 = rng.Next(1, 21);
                    Dice.QueueAsyncForcedD20Rolls(d20);
                    ActionSelector.SetStoredActionRoll(player, d20);
                    ActionSelector.SetStoredActionRoll(enemy, d20);
                    Action? forcedForPlayer = ActionSelector.WouldNaturalRollSelectComboAction(player, d20) ? forced : null;
                    CombatSingleTurnResult step;
                    try
                    {
                        step = await combatManager.AdvanceSingleTurnAsync(player, enemy, room, forcedForPlayer)
                            .ConfigureAwait(false);
                    }
                    finally
                    {
                        Dice.ClearAsyncForcedD20Rolls();
                    }

                    if (step == CombatSingleTurnResult.Advanced)
                        continue;
                    if (step == CombatSingleTurnResult.EnemyDefeated)
                        return player.IsAlive;
                    return false;
                }

                return enemy.CurrentHealth <= 0 && player.IsAlive;
            }
            finally
            {
                Dice.ClearAsyncForcedD20Rolls();
                ActionSelector.RemoveStoredRoll(player);
                ActionSelector.RemoveStoredRoll(enemy);
                if (narrativeStarted)
                {
                    try { combatManager.EndBattleNarrative(player, enemy); } catch { /* ignore */ }
                }
                try { combatManager.Cleanup(); } catch { /* ignore */ }
            }
        }

        private static void Aggregate(ActionLabDungeonSimulationReport report)
        {
            report.ErroredRuns = report.Runs.Count(r => !string.IsNullOrEmpty(r.ErrorMessage));
            var samples = report.Runs.Where(r => string.IsNullOrEmpty(r.ErrorMessage)).ToList();
            if (samples.Count == 0)
                return;
            report.ClearedRuns = samples.Count(r => r.ClearedDungeon);
            report.ClearRate = report.ClearedRuns / (double)samples.Count;
            report.AverageRoomsCleared = samples.Average(r => r.RoomsCleared);
            foreach (var r in samples.Where(x => x.DeathRoomIndex >= 0))
            {
                report.DeathsByRoomIndex.TryGetValue(r.DeathRoomIndex, out int c);
                report.DeathsByRoomIndex[r.DeathRoomIndex] = c + 1;
            }
        }

        public static string FormatReportText(ActionLabDungeonSimulationReport report)
        {
            var lines = new List<string>
            {
                $"Dungeon: {report.CatalogKey}  L{report.DungeonLevel}  seed {report.BaseSeed}",
                $"Runs: {report.Runs.Count}  clear rate: {report.ClearRate:P1}  avg rooms: {report.AverageRoomsCleared:F2}",
                $"Errors: {report.ErroredRuns}  wall: {report.SimulationWallElapsed.TotalMilliseconds:F0} ms"
            };
            if (report.DeathsByRoomIndex.Count > 0)
            {
                lines.Add("Deaths by room index:");
                foreach (var kv in report.DeathsByRoomIndex.OrderBy(k => k.Key))
                    lines.Add($"  room {kv.Key + 1}: {kv.Value}");
            }
            return string.Join(global::System.Environment.NewLine, lines);
        }
    }
}
