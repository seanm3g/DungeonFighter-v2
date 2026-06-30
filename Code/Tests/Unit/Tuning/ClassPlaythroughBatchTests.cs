using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using RPGGame;
using RPGGame.Tests;
using RPGGame.Tuning;

namespace RPGGame.Tests.Unit.Tuning
{
    public static class ClassPlaythroughBatchTests
    {
        private static int _run;
        private static int _pass;
        private static int _fail;

        public static void RunAllTests()
        {
            Console.WriteLine("=== Class Playthrough Batch Tests ===\n");
            _run = _pass = _fail = 0;

            TestPolicyKeyMenus();
            TestWeaponSlotMapping();
            TestClassResolver();
            TestAggregationMath();
            TestParityWarnings();
            TestMeanActionsByLevelAggregation();
            TestBatchSmoke().GetAwaiter().GetResult();

            TestBase.PrintSummary("Class Playthrough Batch Tests", _run, _pass, _fail);
        }

        private static void TestPolicyKeyMenus()
        {
            Console.WriteLine("--- ClassPlaythroughPolicy key menus ---");

            TestBase.AssertEqual("2", ClassPlaythroughPolicy.PickAction("TrainingGroundOffer", 1),
                "TrainingGroundOffer skips training", ref _run, ref _pass, ref _fail);
            TestBase.AssertEqual("3", ClassPlaythroughPolicy.PickAction("WeaponSelection", 3),
                "WeaponSelection uses slot 3", ref _run, ref _pass, ref _fail);
            TestBase.AssertEqual("2", ClassPlaythroughPolicy.PickAction("DungeonSelection", 1),
                "DungeonSelection prefers option 2", ref _run, ref _pass, ref _fail);
            TestBase.AssertTrue(ClassPlaythroughPolicy.IsDungeonEntry("DungeonSelection", "2"),
                "DungeonSelection option 2 counts as dungeon entry", ref _run, ref _pass, ref _fail);
            TestBase.AssertTrue(!ClassPlaythroughPolicy.IsDungeonEntry("DungeonSelection", "0"),
                "DungeonSelection option 0 is not dungeon entry", ref _run, ref _pass, ref _fail);
        }

        private static void TestWeaponSlotMapping()
        {
            Console.WriteLine("--- Weapon slot mapping ---");

            var all = ClassPlaythroughClassResolver.ResolveAllClasses();
            TestBase.AssertEqual(4, all.Count, "Four class paths", ref _run, ref _pass, ref _fail);
            TestBase.AssertTrue(all[0].WeaponType == WeaponType.Mace, "Slot 1 = Mace", ref _run, ref _pass, ref _fail);
            TestBase.AssertTrue(all[1].WeaponType == WeaponType.Sword, "Slot 2 = Sword", ref _run, ref _pass, ref _fail);
            TestBase.AssertTrue(all[2].WeaponType == WeaponType.Dagger, "Slot 3 = Dagger", ref _run, ref _pass, ref _fail);
            TestBase.AssertTrue(all[3].WeaponType == WeaponType.Wand, "Slot 4 = Wand", ref _run, ref _pass, ref _fail);
        }

        private static void TestClassResolver()
        {
            Console.WriteLine("--- Class resolver ---");

            var barbarianOnly = ClassPlaythroughClassResolver.ResolveClasses("Barbarian");
            TestBase.AssertEqual(1, barbarianOnly.Count, "Single class filter", ref _run, ref _pass, ref _fail);
            TestBase.AssertTrue(barbarianOnly[0].WeaponType == WeaponType.Mace, "Barbarian maps to Mace", ref _run, ref _pass, ref _fail);

            var twoClasses = ClassPlaythroughClassResolver.ResolveClasses("Warrior, Rogue");
            TestBase.AssertEqual(2, twoClasses.Count, "Two-class filter", ref _run, ref _pass, ref _fail);

            try
            {
                ClassPlaythroughClassResolver.ResolveClasses("Paladin");
                TestBase.AssertTrue(false, "Unknown class should throw", ref _run, ref _pass, ref _fail);
            }
            catch (ArgumentException)
            {
                _run++;
                _pass++;
            }
        }

        private static void TestAggregationMath()
        {
            Console.WriteLine("--- Aggregation math ---");

            var runs = new List<PlaythroughRunResult>
            {
                new() { FinalLevel = 4, DungeonsCompleted = 2, TurnCount = 100, ReachedDeath = true },
                new() { FinalLevel = 6, DungeonsCompleted = 3, TurnCount = 120, ReachedDeath = true },
                new() { FinalLevel = 5, DungeonsCompleted = 2, TurnCount = 110, ReachedDeath = false }
            };

            var aggregate = ClassPlaythroughAggregator.BuildAggregate(WeaponType.Sword, "Warrior", runs);
            TestBase.AssertEqual(3, aggregate.RunCount, "Run count", ref _run, ref _pass, ref _fail);
            TestBase.AssertTrue(Math.Abs(aggregate.DeathRate - (2.0 / 3.0)) < 0.001, "Death rate", ref _run, ref _pass, ref _fail);
            TestBase.AssertTrue(Math.Abs(aggregate.MeanFinalLevel - 5.0) < 0.001, "Mean final level", ref _run, ref _pass, ref _fail);
            TestBase.AssertTrue(Math.Abs(aggregate.MedianFinalLevel - 5.0) < 0.001, "Median final level", ref _run, ref _pass, ref _fail);
            TestBase.AssertTrue(Math.Abs(aggregate.MeanDungeonsCompleted - (7.0 / 3.0)) < 0.001, "Mean dungeons", ref _run, ref _pass, ref _fail);
        }

        private static void TestParityWarnings()
        {
            Console.WriteLine("--- Parity warnings ---");

            var aggregates = new List<ClassPlaythroughAggregate>
            {
                new() { ClassDisplayName = "Barbarian", MeanFinalLevel = 10, MeanDungeonsCompleted = 5 },
                new() { ClassDisplayName = "Wizard", MeanFinalLevel = 4, MeanDungeonsCompleted = 1 }
            };

            var warnings = ClassPlaythroughAggregator.BuildParityWarnings(aggregates);
            TestBase.AssertTrue(warnings.Count >= 2, "Large spread should produce warnings", ref _run, ref _pass, ref _fail);
        }

        private static void TestMeanActionsByLevelAggregation()
        {
            Console.WriteLine("--- Per-level action aggregation ---");

            var runs = new List<PlaythroughRunResult>
            {
                new()
                {
                    ActionsByLevel = new Dictionary<int, int> { [1] = 10, [2] = 20, [3] = 5 }
                },
                new()
                {
                    ActionsByLevel = new Dictionary<int, int> { [1] = 14, [2] = 16 }
                }
            };

            var stats = ClassPlaythroughAggregator.BuildMeanActionsByLevel(runs);
            var level1 = stats.First(s => s.Level == 1);
            var level2 = stats.First(s => s.Level == 2);
            var level3 = stats.FirstOrDefault(s => s.Level == 3);

            TestBase.AssertTrue(Math.Abs(level1.MeanActions - 12.0) < 0.001, "L1 mean actions", ref _run, ref _pass, ref _fail);
            TestBase.AssertEqual(2, level1.SampleRunCount, "L1 sample count", ref _run, ref _pass, ref _fail);
            TestBase.AssertTrue(Math.Abs(level2.MeanActions - 18.0) < 0.001, "L2 mean actions", ref _run, ref _pass, ref _fail);
            TestBase.AssertTrue(level3 != null && Math.Abs(level3.MeanActions - 5.0) < 0.001, "L3 mean actions", ref _run, ref _pass, ref _fail);
            TestBase.AssertEqual(1, level3!.SampleRunCount, "L3 sample count", ref _run, ref _pass, ref _fail);

            var aggregate = ClassPlaythroughAggregator.BuildAggregate(WeaponType.Sword, "Warrior", runs);
            TestBase.AssertEqual(3, aggregate.MeanActionsByLevel.Count, "Aggregate level stats count", ref _run, ref _pass, ref _fail);
        }

        private static async Task TestBatchSmoke()
        {
            Console.WriteLine("--- Batch smoke (1 run/class, short action cap) ---");

            try
            {
                var batch = await ClassPlaythroughBatchRunner.RunAsync(
                    runsPerClass: 1,
                    classesCsv: null,
                    maxActionsPerRun: 80);

                TestBase.AssertEqual(4, batch.ClassAggregates.Count,
                    "Batch should include four class aggregates", ref _run, ref _pass, ref _fail);

                foreach (var aggregate in batch.ClassAggregates)
                {
                    TestBase.AssertTrue(!string.IsNullOrWhiteSpace(aggregate.ClassDisplayName),
                        $"Class name present for {aggregate.WeaponType}", ref _run, ref _pass, ref _fail);
                    TestBase.AssertEqual(1, aggregate.RunCount,
                        $"One run recorded for {aggregate.ClassDisplayName}", ref _run, ref _pass, ref _fail);
                }

                var report = ClassPlaythroughBatchRunner.FormatReport(batch);
                TestBase.AssertTrue(report.Contains("Class Playthrough Batch Report"),
                    "Report header present", ref _run, ref _pass, ref _fail);
                TestBase.AssertTrue(report.Contains("Avg actions spent at each hero level"),
                    "Per-level section present", ref _run, ref _pass, ref _fail);
                TestBase.AssertTrue(batch.ClassAggregates.Any(a => a.MeanActionsByLevel.Count > 0),
                    "Smoke batch records per-level actions", ref _run, ref _pass, ref _fail);
            }
            catch (Exception ex)
            {
                TestBase.AssertTrue(false, $"Batch smoke failed: {ex.Message}", ref _run, ref _pass, ref _fail);
            }
        }
    }
}
