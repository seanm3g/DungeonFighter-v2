using System;
using RPGGame.Tests;
using RPGGame;
using RPGGame.Combat;

namespace RPGGame.Tests.Unit.Combat
{
    /// <summary>
    /// Tests for BattleNarrative
    /// Tests narrative generation, event tracking, and text generation
    /// </summary>
    public static class BattleNarrativeTests
    {
        private static int _testsRun = 0;
        private static int _testsPassed = 0;
        private static int _testsFailed = 0;

        /// <summary>
        /// Runs all BattleNarrative tests
        /// </summary>
        public static void RunAllTests()
        {
            Console.WriteLine("=== BattleNarrative Tests ===\n");
            
            _testsRun = 0;
            _testsPassed = 0;
            _testsFailed = 0;

            TestConstructor();
            TestAddEvent();
            TestGetNarratives();
            TestEventOrderIsInsertionOrder();
            TestCriticalMissFlavorDoesNotReplayOnLaterHits();
            TestCriticalMissFlavorIsConsumedOnce();
            TestFirstBloodNarrativeReachesDisplay();
            TestCriticalHitNarrativeReachesDisplay();
            TestInformationalSummaryExcludesComboCounts();

            TestBase.PrintSummary("BattleNarrative Tests", _testsRun, _testsPassed, _testsFailed);
        }

        #region Constructor Tests

        private static void TestConstructor()
        {
            Console.WriteLine("--- Testing Constructor ---");

            try
            {
                var narrative = new BattleNarrative("Player", "Enemy", "Dungeon", 100, 50);
                
                TestBase.AssertTrue(narrative != null,
                    "BattleNarrative should be created successfully",
                    ref _testsRun, ref _testsPassed, ref _testsFailed);
            }
            catch (Exception ex)
            {
                TestBase.AssertTrue(false,
                    $"BattleNarrative constructor failed: {ex.Message}",
                    ref _testsRun, ref _testsPassed, ref _testsFailed);
            }
        }

        #endregion

        #region Event Tests

        private static void TestAddEvent()
        {
            Console.WriteLine("\n--- Testing AddEvent ---");

            try
            {
                var narrative = new BattleNarrative("Player", "Enemy");
                var evt = new BattleEvent
                {
                    Actor = "Player",
                    Target = "Enemy",
                    Action = "Attack",
                    Damage = 10,
                    IsSuccess = true
                };
                
                narrative.AddEvent(evt);
                
                TestBase.AssertTrue(true,
                    "AddEvent should complete without errors",
                    ref _testsRun, ref _testsPassed, ref _testsFailed);
            }
            catch (Exception ex)
            {
                TestBase.AssertTrue(false,
                    $"AddEvent failed: {ex.Message}",
                    ref _testsRun, ref _testsPassed, ref _testsFailed);
            }
        }

        private static void TestGetNarratives()
        {
            Console.WriteLine("\n--- Testing GetTriggeredNarratives ---");

            try
            {
                var narrative = new BattleNarrative("Player", "Enemy");
                var narratives = narrative.GetTriggeredNarratives();
                
                TestBase.AssertTrue(narratives != null,
                    "GetTriggeredNarratives should return non-null list",
                    ref _testsRun, ref _testsPassed, ref _testsFailed);
            }
            catch (Exception ex)
            {
                TestBase.AssertTrue(false,
                    $"GetTriggeredNarratives failed: {ex.Message}",
                    ref _testsRun, ref _testsPassed, ref _testsFailed);
            }
        }

        private static void TestEventOrderIsInsertionOrder()
        {
            Console.WriteLine("\n--- Testing event insertion order ---");

            var narrative = new BattleNarrative("Hero", "Goblin", "Test", 100, 100);
            var first = new BattleEvent { Actor = "Hero", Target = "Goblin", Action = "A", IsSuccess = false, NaturalRoll = 1 };
            var second = new BattleEvent { Actor = "Goblin", Target = "Hero", Action = "B", IsSuccess = true, NaturalRoll = 12, Damage = 3 };
            var third = new BattleEvent { Actor = "Hero", Target = "Goblin", Action = "C", IsSuccess = true, NaturalRoll = 15, Damage = 8 };
            narrative.AddEvent(first);
            narrative.AddEvent(second);
            narrative.AddEvent(third);

            var events = narrative.GetAllEvents();
            TestBase.AssertEqual(3, events.Count, "GetAllEvents should keep three events",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
            TestBase.AssertTrue(ReferenceEquals(events[0], first),
                "First recorded event should stay first (not ConcurrentBag LIFO)",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
            TestBase.AssertTrue(ReferenceEquals(events[2], third),
                "Last recorded event should stay last",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        private static void TestCriticalMissFlavorDoesNotReplayOnLaterHits()
        {
            Console.WriteLine("\n--- Testing crit-miss flavor does not replay on later hits ---");

            var narrative = new BattleNarrative("Hero", "Goblin", "Test", 100, 100);
            narrative.AddEvent(new BattleEvent
            {
                Actor = "Hero",
                Target = "Goblin",
                Action = "Swing",
                IsSuccess = false,
                NaturalRoll = 1,
                Damage = 0
            });
            var missTagged = narrative.GetTriggeredNarrativesTagged();
            var missLines = narrative.GetTriggeredNarrativesIfSignificant();
            TestBase.AssertTrue(
                missTagged.Exists(t => t.Bank == "criticalMiss"),
                "Natural 1 miss should surface critical-miss flavor",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
            _ = missLines;

            for (int i = 0; i < 5; i++)
            {
                narrative.AddEvent(new BattleEvent
                {
                    Actor = i % 2 == 0 ? "Goblin" : "Hero",
                    Target = i % 2 == 0 ? "Hero" : "Goblin",
                    Action = "Hit",
                    IsSuccess = true,
                    NaturalRoll = 12 + (i % 6),
                    Damage = 5
                });
                var laterTagged = narrative.GetTriggeredNarrativesTagged();
                narrative.GetTriggeredNarrativesIfSignificant();
                TestBase.AssertTrue(
                    !laterTagged.Exists(t => t.Bank == "criticalMiss"),
                    $"Hit {i + 1} after a crit miss must not reuse miss flavor",
                    ref _testsRun, ref _testsPassed, ref _testsFailed);
            }
        }

        private static void TestCriticalMissFlavorIsConsumedOnce()
        {
            Console.WriteLine("\n--- Testing crit-miss flavor is consumed once ---");

            var narrative = new BattleNarrative("Hero", "Goblin", "Test", 100, 100);
            narrative.AddEvent(new BattleEvent
            {
                Actor = "Hero",
                Target = "Goblin",
                Action = "Swing",
                IsSuccess = false,
                NaturalRoll = 1
            });
            var first = narrative.GetTriggeredNarrativesIfSignificant();
            var second = narrative.GetTriggeredNarrativesIfSignificant();
            TestBase.AssertTrue(first.Count > 0,
                "First display pass should return the miss flavor",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
            TestBase.AssertEqual(0, second.Count,
                "Second display pass without a new event should not replay flavor",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        /// <summary>
        /// Regression guard for the display-gate bug: BattleEventAnalyzer.AnalyzeEvent
        /// consumes the one-shot state flags (first blood, defeats, thresholds) at the
        /// moment it generates a line. Any display-side code that re-derives significance
        /// from those same flags afterwards always answers "not significant" and silently
        /// discards the line. Symptom in-game: zero combat narratives across whole fights.
        /// </summary>
        private static void TestFirstBloodNarrativeReachesDisplay()
        {
            Console.WriteLine("\n--- Testing first blood narrative reaches the display path ---");

            var narrative = new BattleNarrative("Hero", "Goblin", "Test", 100, 100);
            var evt = new BattleEvent
            {
                Actor = "Hero",
                Target = "Goblin",
                Action = "Strike",
                IsSuccess = true,
                NaturalRoll = 14,
                Damage = 7
            };
            narrative.AddEvent(evt);

            var generated = narrative.GetTriggeredNarratives();
            TestBase.AssertTrue(generated.Count > 0,
                "First damaging hit should generate a first-blood narrative",
                ref _testsRun, ref _testsPassed, ref _testsFailed);

            var displayed = narrative.GetTriggeredNarrativesIfSignificant();
            TestBase.AssertTrue(displayed.Count > 0,
                "Generated first-blood narrative must survive the display gate, not be dropped",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        private static void TestCriticalHitNarrativeReachesDisplay()
        {
            Console.WriteLine("\n--- Testing critical hit narrative reaches the display path ---");

            var narrative = new BattleNarrative("Hero", "Goblin", "Test", 100, 100);

            // Spend first blood on an ordinary hit so the crit below is the only thing the
            // next event can generate - otherwise a surviving first-blood line would mask
            // a dropped critical-hit line.
            narrative.AddEvent(new BattleEvent
            {
                Actor = "Hero",
                Target = "Goblin",
                Action = "Strike",
                IsSuccess = true,
                NaturalRoll = 12,
                Damage = 5
            });
            narrative.GetTriggeredNarrativesIfSignificant();

            narrative.AddEvent(new BattleEvent
            {
                Actor = "Hero",
                Target = "Goblin",
                Action = "Strike",
                IsSuccess = true,
                IsCritical = true,
                NaturalRoll = 19,
                Roll = 19,
                Damage = 11
            });

            var generated = narrative.GetTriggeredNarratives();
            TestBase.AssertTrue(generated.Count > 0,
                "A critical hit on a high roll should generate a critical-hit narrative",
                ref _testsRun, ref _testsPassed, ref _testsFailed);

            var displayed = narrative.GetTriggeredNarrativesIfSignificant();
            TestBase.AssertEqual(generated.Count, displayed.Count,
                "Every generated critical-hit line must survive the display gate",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        private static void TestInformationalSummaryExcludesComboCounts()
        {
            Console.WriteLine("\n--- Testing informational summary (no combo line) ---");

            string playerWin = BattleNarrativeGenerator.GenerateInformationalSummary(
                40, 5, playerWon: true, enemyWon: false, "Hero", "Goblin");
            TestBase.AssertTrue(string.IsNullOrEmpty(playerWin),
                "Player victory summary should be empty (no combo or damage line)",
                ref _testsRun, ref _testsPassed, ref _testsFailed);

            string enemyWin = BattleNarrativeGenerator.GenerateInformationalSummary(
                10, 50, playerWon: false, enemyWon: true, "Hero", "Goblin");
            TestBase.AssertTrue(
                enemyWin.Contains("Total damage dealt", StringComparison.Ordinal)
                && enemyWin.Contains("Goblin defeats Hero", StringComparison.Ordinal)
                && !enemyWin.Contains("Combos", StringComparison.OrdinalIgnoreCase),
                "Enemy victory summary should include damage totals but not combo counts",
                ref _testsRun, ref _testsPassed, ref _testsFailed);

            string stalemate = BattleNarrativeGenerator.GenerateInformationalSummary(
                25, 25, playerWon: false, enemyWon: false, "Hero", "Goblin");
            TestBase.AssertTrue(
                stalemate.Contains("stalemate", StringComparison.OrdinalIgnoreCase)
                && !stalemate.Contains("Combos", StringComparison.OrdinalIgnoreCase),
                "Stalemate summary should not include combo counts",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        #endregion
    }
}
