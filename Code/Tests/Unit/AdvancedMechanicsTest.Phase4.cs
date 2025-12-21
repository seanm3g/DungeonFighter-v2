using System;
using RPGGame;
using RPGGame.Combat.Events;
using RPGGame.Progression;
using RPGGame.Combat.Outcomes;

namespace RPGGame.Tests.Unit
{
    /// <summary>
    /// Phase 4: Outcome-Based Actions & Meta-Progression Tests
    /// </summary>
    public static class AdvancedMechanicsTest_Phase4
    {
        private static int _testsRun = 0;
        private static int _testsPassed = 0;
        private static int _testsFailed = 0;
        
        public static void RunAllTests()
        {
            Console.WriteLine("=== Phase 4: Outcome-Based Actions & Meta-Progression ===\n");
            
            _testsRun = 0;
            _testsPassed = 0;
            _testsFailed = 0;
            
            TestActionUsageTracker();
            TestConditionalXPGain();
            TestOutcomeHandlers();
            
            PrintSummary();
        }
        
        private static void TestActionUsageTracker()
        {
            Console.WriteLine("Testing ActionUsageTracker...");
            try
            {
                var tracker = ActionUsageTracker.Instance;
                var actor = new Character("Test", 1);
                var action = new Action { Name = "Test Action" };

                tracker.RecordActionUsage(actor, action);
                int count = tracker.GetUsageCount(actor, action);
                AssertTrue(count == 1, $"Action usage count: {count} (expected 1)");

                tracker.RecordActionUsage(actor, action);
                count = tracker.GetUsageCount(actor, action);
                AssertTrue(count == 2, $"Action usage count after second use: {count} (expected 2)");
            }
            catch (Exception ex)
            {
                AssertTrue(false, $"Action usage tracker test failed: {ex.Message}");
            }
        }

        private static void TestConditionalXPGain()
        {
            Console.WriteLine("Testing ConditionalXPGain...");
            try
            {
                var character = new Character("Test", 1);
                var evt = new CombatEvent(CombatEventType.EnemyDied, character);
                var initialXP = character.Progression.XP;

                ConditionalXPGain.GrantXPFromEvent(evt, character);
                var finalXP = character.Progression.XP;

                AssertTrue(finalXP > initialXP, $"XP gained: {initialXP} -> {finalXP}");
            }
            catch (Exception ex)
            {
                AssertTrue(false, $"Conditional XP gain test failed: {ex.Message}");
            }
        }

        private static void TestOutcomeHandlers()
        {
            Console.WriteLine("Testing OutcomeHandlers...");
            try
            {
                var handler = new ConditionalOutcomeHandler();
                var evt = new CombatEvent(CombatEventType.EnemyDied, new Character("Test", 1));
                var source = new Character("Test", 1);
                var target = new Enemy("TestEnemy", 1, 10, 5, 5, 5, 5);

                handler.HandleOutcome(evt, source, target, null);
                AssertTrue(true, "Outcome handler executed without exception");
            }
            catch (Exception ex)
            {
                AssertTrue(false, $"Outcome handler test failed: {ex.Message}");
            }
        }
        
        private static void AssertTrue(bool condition, string message)
        {
            _testsRun++;
            if (condition)
            {
                _testsPassed++;
                Console.WriteLine($"  ✓ {message}");
            }
            else
            {
                _testsFailed++;
                Console.WriteLine($"  ✗ FAILED: {message}");
            }
        }
        
        private static void PrintSummary()
        {
            Console.WriteLine("\n=== Phase 4 Test Summary ===");
            Console.WriteLine($"Total Tests: {_testsRun}");
            Console.WriteLine($"Passed: {_testsPassed}");
            Console.WriteLine($"Failed: {_testsFailed}");
            
            if (_testsRun > 0)
            {
                Console.WriteLine($"Success Rate: {(_testsPassed * 100.0 / _testsRun):F1}%");
            }
            
            if (_testsFailed == 0)
            {
                Console.WriteLine("\n✅ All Phase 4 tests passed!");
            }
            else
            {
                Console.WriteLine($"\n❌ {_testsFailed} test(s) failed");
            }
        }
    }
}

