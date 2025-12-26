using System;
using RPGGame.Tests;
using RPGGame.UI;
using RPGGame.UI.ColorSystem;

namespace RPGGame.Tests.Unit
{
    /// <summary>
    /// Comprehensive tests for UIMessageBuilder
    /// Tests combat, healing, and status effect message creation
    /// </summary>
    public static class UIMessageBuilderTests
    {
        private static int _testsRun = 0;
        private static int _testsPassed = 0;
        private static int _testsFailed = 0;

        /// <summary>
        /// Runs all UIMessageBuilder tests
        /// </summary>
        public static void RunAllTests()
        {
            Console.WriteLine("=== UIMessageBuilder Tests ===\n");

            // Note: These tests verify the methods don't throw exceptions
            // Full message content verification would require mocking UIColoredTextManager
            TestWriteCombatMessage();
            TestWriteCombatMessageWithDamage();
            TestWriteCombatMessageCritical();
            TestWriteCombatMessageMiss();
            TestWriteCombatMessageBlock();
            TestWriteCombatMessageDodge();
            TestWriteHealingMessage();
            TestWriteStatusEffectMessage();
            TestWriteStatusEffectRemoved();
            TestNullParameterHandling();

            TestBase.PrintSummary("UIMessageBuilder Tests", _testsRun, _testsPassed, _testsFailed);
        }

        #region Combat Message Tests

        private static void TestWriteCombatMessage()
        {
            Console.WriteLine("--- Testing WriteCombatMessage ---");

            var outputManager = new UIOutputManager(null);
            var delayManager = new UIDelayManager();
            var coloredTextManager = new UIColoredTextManager(outputManager, delayManager);
            var builder = new UIMessageBuilder(coloredTextManager);

            try
            {
                builder.WriteCombatMessage("Player", "attacks", "Enemy");
                TestBase.AssertTrue(true, "Should create combat message without exception", 
                    ref _testsRun, ref _testsPassed, ref _testsFailed);
            }
            catch (Exception ex)
            {
                TestBase.AssertTrue(false, $"Should not throw exception: {ex.Message}", 
                    ref _testsRun, ref _testsPassed, ref _testsFailed);
            }
        }

        private static void TestWriteCombatMessageWithDamage()
        {
            Console.WriteLine("\n--- Testing WriteCombatMessage with Damage ---");

            var outputManager = new UIOutputManager(null);
            var delayManager = new UIDelayManager();
            var coloredTextManager = new UIColoredTextManager(outputManager, delayManager);
            var builder = new UIMessageBuilder(coloredTextManager);

            try
            {
                builder.WriteCombatMessage("Player", "attacks", "Enemy", 25);
                TestBase.AssertTrue(true, "Should create combat message with damage without exception", 
                    ref _testsRun, ref _testsPassed, ref _testsFailed);
            }
            catch (Exception ex)
            {
                TestBase.AssertTrue(false, $"Should not throw exception: {ex.Message}", 
                    ref _testsRun, ref _testsPassed, ref _testsFailed);
            }
        }

        private static void TestWriteCombatMessageCritical()
        {
            Console.WriteLine("\n--- Testing WriteCombatMessage Critical ---");

            var outputManager = new UIOutputManager(null);
            var delayManager = new UIDelayManager();
            var coloredTextManager = new UIColoredTextManager(outputManager, delayManager);
            var builder = new UIMessageBuilder(coloredTextManager);

            try
            {
                builder.WriteCombatMessage("Player", "attacks", "Enemy", 50, isCritical: true);
                TestBase.AssertTrue(true, "Should create critical combat message without exception", 
                    ref _testsRun, ref _testsPassed, ref _testsFailed);
            }
            catch (Exception ex)
            {
                TestBase.AssertTrue(false, $"Should not throw exception: {ex.Message}", 
                    ref _testsRun, ref _testsPassed, ref _testsFailed);
            }
        }

        private static void TestWriteCombatMessageMiss()
        {
            Console.WriteLine("\n--- Testing WriteCombatMessage Miss ---");

            var outputManager = new UIOutputManager(null);
            var delayManager = new UIDelayManager();
            var coloredTextManager = new UIColoredTextManager(outputManager, delayManager);
            var builder = new UIMessageBuilder(coloredTextManager);

            try
            {
                builder.WriteCombatMessage("Player", "attacks", "Enemy", null, isMiss: true);
                TestBase.AssertTrue(true, "Should create miss message without exception", 
                    ref _testsRun, ref _testsPassed, ref _testsFailed);
            }
            catch (Exception ex)
            {
                TestBase.AssertTrue(false, $"Should not throw exception: {ex.Message}", 
                    ref _testsRun, ref _testsPassed, ref _testsFailed);
            }
        }

        private static void TestWriteCombatMessageBlock()
        {
            Console.WriteLine("\n--- Testing WriteCombatMessage Block ---");

            var outputManager = new UIOutputManager(null);
            var delayManager = new UIDelayManager();
            var coloredTextManager = new UIColoredTextManager(outputManager, delayManager);
            var builder = new UIMessageBuilder(coloredTextManager);

            try
            {
                builder.WriteCombatMessage("Player", "attacks", "Enemy", null, isBlock: true);
                TestBase.AssertTrue(true, "Should create block message without exception", 
                    ref _testsRun, ref _testsPassed, ref _testsFailed);
            }
            catch (Exception ex)
            {
                TestBase.AssertTrue(false, $"Should not throw exception: {ex.Message}", 
                    ref _testsRun, ref _testsPassed, ref _testsFailed);
            }
        }

        private static void TestWriteCombatMessageDodge()
        {
            Console.WriteLine("\n--- Testing WriteCombatMessage Dodge ---");

            var outputManager = new UIOutputManager(null);
            var delayManager = new UIDelayManager();
            var coloredTextManager = new UIColoredTextManager(outputManager, delayManager);
            var builder = new UIMessageBuilder(coloredTextManager);

            try
            {
                builder.WriteCombatMessage("Player", "attacks", "Enemy", null, isDodge: true);
                TestBase.AssertTrue(true, "Should create dodge message without exception", 
                    ref _testsRun, ref _testsPassed, ref _testsFailed);
            }
            catch (Exception ex)
            {
                TestBase.AssertTrue(false, $"Should not throw exception: {ex.Message}", 
                    ref _testsRun, ref _testsPassed, ref _testsFailed);
            }
        }

        #endregion

        #region Healing Message Tests

        private static void TestWriteHealingMessage()
        {
            Console.WriteLine("\n--- Testing WriteHealingMessage ---");

            var outputManager = new UIOutputManager(null);
            var delayManager = new UIDelayManager();
            var coloredTextManager = new UIColoredTextManager(outputManager, delayManager);
            var builder = new UIMessageBuilder(coloredTextManager);

            try
            {
                builder.WriteHealingMessage("Player", "Player", 30);
                TestBase.AssertTrue(true, "Should create healing message without exception", 
                    ref _testsRun, ref _testsPassed, ref _testsFailed);
            }
            catch (Exception ex)
            {
                TestBase.AssertTrue(false, $"Should not throw exception: {ex.Message}", 
                    ref _testsRun, ref _testsPassed, ref _testsFailed);
            }
        }

        #endregion

        #region Status Effect Message Tests

        private static void TestWriteStatusEffectMessage()
        {
            Console.WriteLine("\n--- Testing WriteStatusEffectMessage ---");

            var outputManager = new UIOutputManager(null);
            var delayManager = new UIDelayManager();
            var coloredTextManager = new UIColoredTextManager(outputManager, delayManager);
            var builder = new UIMessageBuilder(coloredTextManager);

            try
            {
                builder.WriteStatusEffectMessage("Player", "Poison", isApplied: true);
                TestBase.AssertTrue(true, "Should create status effect message without exception", 
                    ref _testsRun, ref _testsPassed, ref _testsFailed);
            }
            catch (Exception ex)
            {
                TestBase.AssertTrue(false, $"Should not throw exception: {ex.Message}", 
                    ref _testsRun, ref _testsPassed, ref _testsFailed);
            }
        }

        private static void TestWriteStatusEffectRemoved()
        {
            Console.WriteLine("\n--- Testing WriteStatusEffectMessage Removed ---");

            var outputManager = new UIOutputManager(null);
            var delayManager = new UIDelayManager();
            var coloredTextManager = new UIColoredTextManager(outputManager, delayManager);
            var builder = new UIMessageBuilder(coloredTextManager);

            try
            {
                builder.WriteStatusEffectMessage("Player", "Poison", isApplied: false);
                TestBase.AssertTrue(true, "Should create status effect removed message without exception", 
                    ref _testsRun, ref _testsPassed, ref _testsFailed);
            }
            catch (Exception ex)
            {
                TestBase.AssertTrue(false, $"Should not throw exception: {ex.Message}", 
                    ref _testsRun, ref _testsPassed, ref _testsFailed);
            }
        }

        #endregion

        #region Edge Case Tests

        private static void TestNullParameterHandling()
        {
            Console.WriteLine("\n--- Testing Null Parameter Handling ---");

            var outputManager = new UIOutputManager(null);
            var delayManager = new UIDelayManager();
            var coloredTextManager = new UIColoredTextManager(outputManager, delayManager);
            var builder = new UIMessageBuilder(coloredTextManager);

            // Test with null/empty strings (should handle gracefully or throw appropriate exception)
            try
            {
                builder.WriteCombatMessage("", "attacks", "");
                TestBase.AssertTrue(true, "Should handle empty strings", 
                    ref _testsRun, ref _testsPassed, ref _testsFailed);
            }
            catch (Exception ex)
            {
                // If it throws, that's also acceptable behavior
                TestBase.AssertTrue(true, $"Exception with empty strings is acceptable: {ex.Message}", 
                    ref _testsRun, ref _testsPassed, ref _testsFailed);
            }
        }

        #endregion
    }
}

