using System;
using System.Collections.Generic;
using System.Linq;
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
            TestInformationalSummaryExcludesComboCounts();
            TestDisplayGateAfterAnalyzeEvent();
            TestFullFightDisplaysEachNarrativeType();

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

        /// <summary>
        /// AnalyzeEvent sets one-shot flags; display must still return the text generated for this event.
        /// </summary>
        private static void TestDisplayGateAfterAnalyzeEvent()
        {
            Console.WriteLine("\n--- Testing display gate after AnalyzeEvent mutates flags ---");

            var settings = GameSettings.Instance;
            bool prevEnable = settings.EnableNarrativeEvents;
            double prevBalance = settings.NarrativeBalance;
            settings.EnableNarrativeEvents = true;
            settings.NarrativeBalance = 0.8;

            try
            {
                var narrative = new BattleNarrative("Hero", "Goblin", "Hall", 100, 100);
                narrative.AddEvent(new BattleEvent
                {
                    Actor = "Hero",
                    Target = "Goblin",
                    Damage = 10,
                    IsSuccess = true
                });

                var displayed = narrative.GetTriggeredNarrativesIfSignificant();
                TestBase.AssertTrue(displayed.Count > 0,
                    "First-blood event should display after flags are set",
                    ref _testsRun, ref _testsPassed, ref _testsFailed);
                TestBase.AssertTrue(displayed.Any(MatchesCombatBank("firstBlood")),
                    "Displayed lines should include firstBlood text",
                    ref _testsRun, ref _testsPassed, ref _testsFailed);

                var second = narrative.GetTriggeredNarrativesIfSignificant();
                TestBase.AssertTrue(second.Count > 0,
                    "Re-querying the same event should still return cached display text",
                    ref _testsRun, ref _testsPassed, ref _testsFailed);
            }
            finally
            {
                settings.EnableNarrativeEvents = prevEnable;
                settings.NarrativeBalance = prevBalance;
            }
        }

        private static void TestFullFightDisplaysEachNarrativeType()
        {
            Console.WriteLine("\n--- Testing full fight displays each narrative type ---");

            var settings = GameSettings.Instance;
            bool prevEnable = settings.EnableNarrativeEvents;
            double prevBalance = settings.NarrativeBalance;
            settings.EnableNarrativeEvents = true;
            settings.NarrativeBalance = 0.8;

            try
            {
                var collected = new List<string>();
                var narrative = new BattleNarrative("Hero", "Goblin", "Hall", 100, 100);

                void Play(BattleEvent evt)
                {
                    narrative.AddEvent(evt);
                    collected.AddRange(narrative.GetTriggeredNarrativesIfSignificant());
                }

                // firstBlood + healthLeadChange (Hero 100, Goblin 90)
                Play(new BattleEvent { Actor = "Hero", Target = "Goblin", Damage = 10, IsSuccess = true });

                // criticalHit
                Play(new BattleEvent
                {
                    Actor = "Hero",
                    Target = "Goblin",
                    Damage = 12,
                    IsSuccess = true,
                    IsCritical = true,
                    Roll = 18
                });

                // criticalMiss
                Play(new BattleEvent
                {
                    Actor = "Hero",
                    Target = "Goblin",
                    Damage = 0,
                    IsSuccess = false,
                    NaturalRoll = 1
                });

                // below50Percent (Hero 100 → 49)
                Play(new BattleEvent { Actor = "Goblin", Target = "Hero", Damage = 51, IsSuccess = true });

                // below10Percent (Hero 49 → 9)
                Play(new BattleEvent { Actor = "Goblin", Target = "Hero", Damage = 40, IsSuccess = true });

                // Pad action counts to taunt thresholds (player ~11, enemy ~9 at balance 0.8).
                // Zero damage so health/threshold flags stay put.
                for (int i = 0; i < 14; i++)
                    Play(new BattleEvent { Actor = "Hero", Target = "Goblin", Damage = 0, IsSuccess = true });
                for (int i = 0; i < 12; i++)
                    Play(new BattleEvent { Actor = "Goblin", Target = "Hero", Damage = 0, IsSuccess = true });

                TestBase.AssertTrue(collected.Any(MatchesCombatBank("firstBlood")),
                    "full fight should display firstBlood",
                    ref _testsRun, ref _testsPassed, ref _testsFailed);
                TestBase.AssertTrue(collected.Any(line => MatchesCombatBank("criticalHit", ("name", "Hero"))(line)),
                    "full fight should display criticalHit",
                    ref _testsRun, ref _testsPassed, ref _testsFailed);
                TestBase.AssertTrue(collected.Any(line => MatchesCombatBank("criticalMiss", ("name", "Hero"))(line)),
                    "full fight should display criticalMiss",
                    ref _testsRun, ref _testsPassed, ref _testsFailed);
                TestBase.AssertTrue(collected.Any(line => MatchesCombatBank("below50Percent", ("name", "Hero"))(line)),
                    "full fight should display below50Percent",
                    ref _testsRun, ref _testsPassed, ref _testsFailed);
                TestBase.AssertTrue(collected.Any(line => MatchesCombatBank("below10Percent", ("name", "Hero"))(line)),
                    "full fight should display below10Percent",
                    ref _testsRun, ref _testsPassed, ref _testsFailed);
                TestBase.AssertTrue(collected.Any(line => MatchesCombatBank("healthLeadChange", ("name", "Hero"))(line)),
                    "full fight should display healthLeadChange",
                    ref _testsRun, ref _testsPassed, ref _testsFailed);
                TestBase.AssertTrue(collected.Any(IsPlayerTauntLine),
                    "full fight should display playerTaunt",
                    ref _testsRun, ref _testsPassed, ref _testsFailed);
                TestBase.AssertTrue(collected.Any(IsEnemyTauntLine),
                    "full fight should display enemyTaunt",
                    ref _testsRun, ref _testsPassed, ref _testsFailed);
            }
            finally
            {
                settings.EnableNarrativeEvents = prevEnable;
                settings.NarrativeBalance = prevBalance;
            }
        }

        private static Func<string, bool> MatchesCombatBank(string key, params (string Token, string Value)[] replacements)
        {
            return line =>
            {
                if (string.IsNullOrEmpty(line))
                    return false;
                if (!FlavorText.GetData().CombatNarratives.TryGetValue(key, out var bank) || bank == null)
                    return false;
                foreach (var template in bank)
                {
                    string filled = template;
                    foreach (var (token, value) in replacements)
                        filled = filled.Replace("{" + token + "}", value);
                    if (string.Equals(line, filled, StringComparison.Ordinal))
                        return true;
                }
                return false;
            };
        }

        private static bool IsPlayerTauntLine(string line)
        {
            if (MatchesCombatBank("playerTaunt", ("name", "Hero"), ("enemy", "Goblin"))(line))
                return true;
            foreach (var key in FlavorText.GetData().CombatNarratives.Keys)
            {
                if (key.StartsWith("playerTaunt", StringComparison.OrdinalIgnoreCase)
                    && MatchesCombatBank(key, ("name", "Hero"), ("enemy", "Goblin"))(line))
                    return true;
            }
            return false;
        }

        private static bool IsEnemyTauntLine(string line)
        {
            if (MatchesCombatBank("enemyTaunt", ("name", "Goblin"), ("player", "Hero"))(line))
                return true;
            foreach (var key in FlavorText.GetData().CombatNarratives.Keys)
            {
                if (key.StartsWith("enemyTaunt", StringComparison.OrdinalIgnoreCase)
                    && MatchesCombatBank(key, ("name", "Goblin"), ("player", "Hero"))(line))
                    return true;
            }
            return false;
        }

        #endregion
    }
}
