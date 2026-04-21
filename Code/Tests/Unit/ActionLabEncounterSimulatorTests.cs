using System;
using System.Linq;
using System.Threading.Tasks;
using RPGGame;
using RPGGame.ActionInteractionLab;
using RPGGame.Combat;
using RPGGame.Entity.Services;
using RPGGame.Tests;

namespace RPGGame.Tests.Unit
{
    public static class ActionLabEncounterSimulatorTests
    {
        /// <summary><see cref="Random.Next()"/> returns a fixed value once (used so parallel batches use a known batch seed).</summary>
        private sealed class FirstInt32Random : Random
        {
            private readonly int _first;
            private bool _consumed;

            public FirstInt32Random(int first32)
                : base(0) => _first = first32;

            public override int Next()
            {
                if (!_consumed)
                {
                    _consumed = true;
                    return _first;
                }

                return base.Next();
            }
        }

        public static void RunAllTests()
        {
            int run = 0, passed = 0, failed = 0;

            ValidateSnapshot_RejectsEmptyStrip(ref run, ref passed, ref failed);
            RunBatch_CompletesWithExpectedCount(ref run, ref passed, ref failed);
            RunBatch_CompletesWithEnemyLevelThree(ref run, ref passed, ref failed);
            RunBatch_AggregatesExtendedStats(ref run, ref passed, ref failed);
            RunBatch_ParallelMatchesSequentialPerEncounterMetrics(ref run, ref passed, ref failed);
            RunBatch_ParallelStress_NoErroredEncounters(ref run, ref passed, ref failed);
            ComboStreakStatistics_DetectsRunsAndMax(ref run, ref passed, ref failed);

            TestBase.PrintSummary("ActionLabEncounterSimulatorTests", run, passed, failed);
        }

        private static void ValidateSnapshot_RejectsEmptyStrip(ref int run, ref int passed, ref int failed)
        {
            var snap = new LabCombatSnapshot(
                initialPlayerJson: "{}",
                labPanelStrDelta: 0,
                labPanelAgiDelta: 0,
                labPanelTecDelta: 0,
                labPanelIntDelta: 0,
                labPanelLevelDelta: 0,
                labPanelArmorDelta: 0,
                sessionEnemyLoaderType: null,
                enemyLevel: 1,
                comboStripActionNames: Array.Empty<string>(),
                selectedCatalogActionName: "JAB");
            string? err = ActionLabEncounterSimulator.ValidateSnapshot(snap);
            TestBase.AssertTrue(!string.IsNullOrEmpty(err), "ValidateSnapshot rejects empty strip", ref run, ref passed, ref failed);
        }

        private static void RunBatch_CompletesWithExpectedCount(ref int run, ref int passed, ref int failed)
        {
            ActionLoader.LoadActions();
            var jab = ActionLoader.GetAction("JAB");
            if (jab == null)
            {
                TestBase.AssertTrue(true, "RunBatch_CompletesWithExpectedCount skipped (no JAB)", ref run, ref passed, ref failed);
                return;
            }

            var hero = TestDataBuilders.Character().WithName("SimBatchHero").Build();
            if (!jab.IsComboAction)
                jab.IsComboAction = true;
            hero.AddAction(jab, 1.0);
            hero.Actions.AddToCombo(jab);

            var serializer = new CharacterSerializer();
            string json = serializer.Serialize(hero);
            var strip = hero.GetComboActions().Select(a => a.Name).ToList();
            var snapshot = new LabCombatSnapshot(
                json,
                0, 0, 0, 0, 0, 0,
                null,
                1,
                strip,
                "JAB");

            string? v = ActionLabEncounterSimulator.ValidateSnapshot(snapshot);
            TestBase.AssertTrue(v == null, "Snapshot valid for sim batch", ref run, ref passed, ref failed);
            if (v != null)
                return;

            var report = Task.Run(() => ActionLabEncounterSimulator.RunBatchAsync(snapshot, 5, new Random(42)))
                .GetAwaiter().GetResult();
            TestBase.AssertEqual(5, report.EncounterCount, "batch size", ref run, ref passed, ref failed);
            TestBase.AssertEqual(5, report.Encounters.Count, "encounters list", ref run, ref passed, ref failed);
            TestBase.AssertEqual(0, report.ErroredEncounters, "no errored encounters", ref run, ref passed, ref failed);
        }

        private static void RunBatch_CompletesWithEnemyLevelThree(ref int run, ref int passed, ref int failed)
        {
            ActionLoader.LoadActions();
            var jab = ActionLoader.GetAction("JAB");
            if (jab == null)
            {
                TestBase.AssertTrue(true, "RunBatch_CompletesWithEnemyLevelThree skipped (no JAB)", ref run, ref passed, ref failed);
                return;
            }

            var hero = TestDataBuilders.Character().WithName("SimEnLvHero").Build();
            if (!jab.IsComboAction)
                jab.IsComboAction = true;
            hero.AddAction(jab, 1.0);
            hero.Actions.AddToCombo(jab);

            var serializer = new CharacterSerializer();
            string json = serializer.Serialize(hero);
            var strip = hero.GetComboActions().Select(a => a.Name).ToList();
            var snapshot = new LabCombatSnapshot(
                json,
                0, 0, 0, 0, 0, 0,
                null,
                enemyLevel: 3,
                strip,
                "JAB");

            string? v = ActionLabEncounterSimulator.ValidateSnapshot(snapshot);
            TestBase.AssertTrue(v == null, "Snapshot with enemy L3 valid", ref run, ref passed, ref failed);
            if (v != null)
                return;

            var report = Task.Run(() => ActionLabEncounterSimulator.RunBatchAsync(snapshot, 3, new Random(99)))
                .GetAwaiter().GetResult();
            TestBase.AssertEqual(3, report.EncounterCount, "batch with L3 enemy", ref run, ref passed, ref failed);
            TestBase.AssertEqual(0, report.ErroredEncounters, "no errors with scaled dummy", ref run, ref passed, ref failed);
        }

        private static void RunBatch_AggregatesExtendedStats(ref int run, ref int passed, ref int failed)
        {
            ActionLoader.LoadActions();
            var jab = ActionLoader.GetAction("JAB");
            if (jab == null)
            {
                TestBase.AssertTrue(true, "RunBatch_AggregatesExtendedStats skipped (no JAB)", ref run, ref passed, ref failed);
                return;
            }

            var hero = TestDataBuilders.Character().WithName("SimAggHero").Build();
            if (!jab.IsComboAction)
                jab.IsComboAction = true;
            hero.AddAction(jab, 1.0);
            hero.Actions.AddToCombo(jab);

            var serializer = new CharacterSerializer();
            string json = serializer.Serialize(hero);
            var strip = hero.GetComboActions().Select(a => a.Name).ToList();
            var snapshot = new LabCombatSnapshot(
                json,
                0, 0, 0, 0, 0, 0,
                null,
                1,
                strip,
                "JAB");

            var report = Task.Run(() => ActionLabEncounterSimulator.RunBatchAsync(snapshot, 5, new Random(7)))
                .GetAwaiter().GetResult();

            TestBase.AssertTrue(!double.IsNaN(report.MedianTurns) && !double.IsInfinity(report.MedianTurns), "MedianTurns finite", ref run, ref passed, ref failed);
            TestBase.AssertTrue(!double.IsNaN(report.StdDevTurns) && report.StdDevTurns >= 0, "StdDevTurns valid", ref run, ref passed, ref failed);
            TestBase.AssertTrue(report.MedianPlayerDamage >= 0, "MedianPlayerDamage non-negative", ref run, ref passed, ref failed);
            TestBase.AssertTrue(report.SimulationWallElapsed > TimeSpan.Zero, "batch records simulation wall time", ref run, ref passed, ref failed);
            string text = ActionLabEncounterSimulator.FormatReportText(report, snapshot);
            TestBase.AssertTrue(text.Contains("Forced catalog action:", StringComparison.Ordinal), "report has setup header", ref run, ref passed, ref failed);
            TestBase.AssertTrue(text.Contains("Player combo chains", StringComparison.Ordinal), "report includes combo chain section", ref run, ref passed, ref failed);
            TestBase.AssertTrue(text.Contains("Mean longest chain per encounter:", StringComparison.Ordinal), "report includes mean longest combo chain", ref run, ref passed, ref failed);
            TestBase.AssertTrue(text.Contains("Simulation wall time:", StringComparison.Ordinal), "report footer includes wall time", ref run, ref passed, ref failed);
            TestBase.AssertEqual(1000, ActionLabEncounterSimulator.DefaultBatchEncounterCount, "default batch is 1000", ref run, ref passed, ref failed);
        }

        private static void RunBatch_ParallelMatchesSequentialPerEncounterMetrics(ref int run, ref int passed, ref int failed)
        {
            ActionLoader.LoadActions();
            var jab = ActionLoader.GetAction("JAB");
            if (jab == null)
            {
                TestBase.AssertTrue(true, "RunBatch_ParallelMatchesSequential skipped (no JAB)", ref run, ref passed, ref failed);
                return;
            }

            var hero = TestDataBuilders.Character().WithName("SimParHero").Build();
            if (!jab.IsComboAction)
                jab.IsComboAction = true;
            hero.AddAction(jab, 1.0);
            hero.Actions.AddToCombo(jab);

            var serializer = new CharacterSerializer();
            string json = serializer.Serialize(hero);
            var strip = hero.GetComboActions().Select(a => a.Name).ToList();
            var snapshot = new LabCombatSnapshot(
                json,
                0, 0, 0, 0, 0, 0,
                null,
                1,
                strip,
                "JAB");

            if (ActionLabEncounterSimulator.ValidateSnapshot(snapshot) != null)
            {
                TestBase.AssertTrue(true, "RunBatch_ParallelMatchesSequential skipped (invalid snapshot)", ref run, ref passed, ref failed);
                return;
            }

            const int batchSeed = unchecked((int)0xC001D00D);
            const int encounterCount = 4;
            const int parallelDop = 4;

            var sequential = new EncounterMetrics[encounterCount];
            for (int i = 0; i < encounterCount; i++)
            {
                sequential[i] = ActionLabEncounterSimulator.RunSingleEncounterAsync(
                        snapshot,
                        new Random(ActionLabEncounterSimulator.DeriveEncounterRandomSeed(batchSeed, i)))
                    .GetAwaiter().GetResult();
            }

            var parallelReport = ActionLabEncounterSimulator.RunBatchAsync(
                    snapshot,
                    encounterCount,
                    new FirstInt32Random(batchSeed),
                    maxDegreeOfParallelism: parallelDop)
                .GetAwaiter().GetResult();

            TestBase.AssertEqual(encounterCount, parallelReport.Encounters.Count, "parallel batch encounter count", ref run, ref passed, ref failed);
            for (int i = 0; i < encounterCount; i++)
            {
                var a = sequential[i];
                var b = parallelReport.Encounters[i];
                TestBase.AssertEqual(a.PlayerWon, b.PlayerWon, $"enc {i} PlayerWon", ref run, ref passed, ref failed);
                TestBase.AssertEqual(a.Turns, b.Turns, $"enc {i} Turns", ref run, ref passed, ref failed);
                TestBase.AssertEqual(a.PlayerDamageDealt, b.PlayerDamageDealt, $"enc {i} PlayerDamageDealt", ref run, ref passed, ref failed);
                TestBase.AssertEqual(a.ErrorMessage ?? "", b.ErrorMessage ?? "", $"enc {i} ErrorMessage", ref run, ref passed, ref failed);
            }
        }

        private static void RunBatch_ParallelStress_NoErroredEncounters(ref int run, ref int passed, ref int failed)
        {
            ActionLoader.LoadActions();
            var jab = ActionLoader.GetAction("JAB");
            if (jab == null)
            {
                TestBase.AssertTrue(true, "RunBatch_ParallelStress_NoErroredEncounters skipped (no JAB)", ref run, ref passed, ref failed);
                return;
            }

            var hero = TestDataBuilders.Character().WithName("SimStressHero").Build();
            if (!jab.IsComboAction)
                jab.IsComboAction = true;
            hero.AddAction(jab, 1.0);
            hero.Actions.AddToCombo(jab);

            var serializer = new CharacterSerializer();
            string json = serializer.Serialize(hero);
            var strip = hero.GetComboActions().Select(a => a.Name).ToList();
            var snapshot = new LabCombatSnapshot(
                json,
                0, 0, 0, 0, 0, 0,
                null,
                1,
                strip,
                "JAB");

            if (ActionLabEncounterSimulator.ValidateSnapshot(snapshot) != null)
            {
                TestBase.AssertTrue(true, "RunBatch_ParallelStress_NoErroredEncounters skipped (invalid snapshot)", ref run, ref passed, ref failed);
                return;
            }

            const int encounterCount = 80;
            var report = Task.Run(() => ActionLabEncounterSimulator.RunBatchAsync(
                    snapshot,
                    encounterCount,
                    new Random(2026),
                    maxDegreeOfParallelism: 8))
                .GetAwaiter().GetResult();

            TestBase.AssertEqual(encounterCount, report.EncounterCount, "parallel stress encounter count", ref run, ref passed, ref failed);
            TestBase.AssertEqual(0, report.ErroredEncounters, "parallel stress no errored encounters", ref run, ref passed, ref failed);
        }

        private static void ComboStreakStatistics_DetectsRunsAndMax(ref int run, ref int passed, ref int failed)
        {
            var t0 = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);
            var events = new System.Collections.Generic.List<BattleEvent>
            {
                new()
                {
                    Actor = "H", Target = "E", IsSuccess = true, IsCombo = true, Timestamp = t0
                },
                new()
                {
                    Actor = "H", Target = "E", IsSuccess = true, IsCombo = true, Timestamp = t0.AddTicks(1)
                },
                new()
                {
                    Actor = "H", Target = "E", IsSuccess = true, IsCombo = false, Timestamp = t0.AddTicks(2)
                },
                new()
                {
                    Actor = "H", Target = "E", IsSuccess = true, IsCombo = true, Timestamp = t0.AddTicks(3)
                },
                new()
                {
                    Actor = "H", Target = "E", IsSuccess = true, IsCombo = true, Timestamp = t0.AddTicks(4)
                },
                new()
                {
                    Actor = "E", Target = "H", IsSuccess = true, IsCombo = false, Timestamp = t0.AddTicks(5)
                },
            };

            var s = BattleNarrativeGenerator.CalculatePlayerComboStreakStatistics(events, "H", "E");
            TestBase.AssertEqual(2, s.MaxStreak, "combo streak max", ref run, ref passed, ref failed);
            TestBase.AssertEqual(2, s.RunCountsByLength.GetValueOrDefault(2), "two runs of length 2", ref run, ref passed, ref failed);
            TestBase.AssertEqual(0, s.RunCountsByLength.GetValueOrDefault(3), "no length-3 run", ref run, ref passed, ref failed);
        }
    }
}
