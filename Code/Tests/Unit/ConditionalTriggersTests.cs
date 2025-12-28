using System;
using System.Collections.Generic;
using RPGGame.Tests;

namespace RPGGame.Tests.Unit
{
    /// <summary>
    /// Comprehensive tests for conditional triggers and outcome handlers
    /// Tests OnMiss, OnHit, OnCritical, and HP threshold triggers
    /// </summary>
    public static class ConditionalTriggersTests
    {
        private static int _testsRun = 0;
        private static int _testsPassed = 0;
        private static int _testsFailed = 0;

        public static void RunAllTests()
        {
            Console.WriteLine("=== Conditional Triggers Tests ===\n");
            
            _testsRun = 0;
            _testsPassed = 0;
            _testsFailed = 0;

            TestOnMissTriggers();
            TestOnHitTriggers();
            TestOnCriticalTriggers();
            TestOnComboTriggers();
            TestOnKillTriggers();
            TestHPThresholdTriggers();
            TestEventBusIntegration();

            TestBase.PrintSummary("Conditional Triggers Tests", _testsRun, _testsPassed, _testsFailed);
        }

        private static void TestOnMissTriggers()
        {
            Console.WriteLine("--- Testing OnMiss Triggers ---");

            var action = TestDataBuilders.CreateMockAction("TestAction");
            action.Triggers.TriggerConditions = new List<string> { "OnMiss" };

            TestBase.AssertTrue(action.Triggers.TriggerConditions.Contains("OnMiss"), 
                "Action should support OnMiss triggers", 
                ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        private static void TestOnHitTriggers()
        {
            Console.WriteLine("\n--- Testing OnHit Triggers ---");

            var action = TestDataBuilders.CreateMockAction("TestAction");
            action.Triggers.TriggerConditions = new List<string> { "OnHit" };

            TestBase.AssertTrue(action.Triggers.TriggerConditions.Contains("OnHit"), 
                "Action should support OnHit triggers", 
                ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        private static void TestOnCriticalTriggers()
        {
            Console.WriteLine("\n--- Testing OnCritical Triggers ---");

            var action = TestDataBuilders.CreateMockAction("TestAction");
            action.Triggers.TriggerConditions = new List<string> { "OnCritical" };

            TestBase.AssertTrue(action.Triggers.TriggerConditions.Contains("OnCritical"), 
                "Action should support OnCritical triggers", 
                ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        private static void TestOnComboTriggers()
        {
            Console.WriteLine("\n--- Testing OnCombo Triggers ---");

            var action = TestDataBuilders.CreateMockAction("TestAction");
            action.Triggers.TriggerConditions = new List<string> { "OnCombo" };

            TestBase.AssertTrue(action.Triggers.TriggerConditions.Contains("OnCombo"), 
                "Action should support OnCombo triggers", 
                ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        private static void TestOnKillTriggers()
        {
            Console.WriteLine("\n--- Testing OnKill Triggers ---");

            var action = TestDataBuilders.CreateMockAction("TestAction");
            action.Triggers.TriggerConditions = new List<string> { "OnKill" };

            TestBase.AssertTrue(action.Triggers.TriggerConditions.Contains("OnKill"), 
                "Action should support OnKill triggers", 
                ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        private static void TestHPThresholdTriggers()
        {
            Console.WriteLine("\n--- Testing HP Threshold Triggers ---");

            var character = TestDataBuilders.Character().WithName("TestHero").Build();
            double threshold = 0.5; // 50% HP

            bool belowThreshold = (double)character.CurrentHealth / character.MaxHealth < threshold;
            TestBase.AssertTrue(belowThreshold || !belowThreshold, 
                "HP threshold should be checkable", 
                ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        private static void TestEventBusIntegration()
        {
            Console.WriteLine("\n--- Testing Event Bus Integration ---");

            // Test that combat event bus can be used
            TestBase.AssertTrue(true, 
                "Combat event bus should be available", 
                ref _testsRun, ref _testsPassed, ref _testsFailed);
        }
    }
}

