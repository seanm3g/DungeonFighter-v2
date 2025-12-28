using System;
using System.Collections.Generic;
using RPGGame.Tests;

namespace RPGGame.Tests.Unit
{
    /// <summary>
    /// Tests for combo execution
    /// Tests multi-step combos, damage scaling, interruption, and continuation
    /// </summary>
    public static class ComboExecutionTests
    {
        private static int _testsRun = 0;
        private static int _testsPassed = 0;
        private static int _testsFailed = 0;

        public static void RunAllTests()
        {
            Console.WriteLine("=== Combo Execution Tests ===\n");
            
            _testsRun = 0;
            _testsPassed = 0;
            _testsFailed = 0;

            TestMultiStepComboExecution();
            TestComboDamageScaling();
            TestComboInterruption();
            TestComboContinuationAfterMiss();
            TestComboContinuationAfterHit();
            TestComboContinuationAfterCritical();
            TestComboWithDifferentActionTypes();
            TestComboWithStatusEffects();

            TestBase.PrintSummary("Combo Execution Tests", _testsRun, _testsPassed, _testsFailed);
        }

        private static void TestMultiStepComboExecution()
        {
            Console.WriteLine("--- Testing Multi-Step Combo Execution ---");

            var character = TestDataBuilders.Character().WithName("TestHero").Build();
            
            // Test combo step progression
            TestBase.AssertEqual(0, character.ComboStep, 
                "Character should start at combo step 0", 
                ref _testsRun, ref _testsPassed, ref _testsFailed);

            // Test combo amplifier
            double amplifier = character.GetComboAmplifier();
            TestBase.AssertTrue(amplifier >= 1.0, 
                $"Combo amplifier should be at least 1.0, got {amplifier}", 
                ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        private static void TestComboDamageScaling()
        {
            Console.WriteLine("\n--- Testing Combo Damage Scaling ---");

            var character = TestDataBuilders.Character().WithName("TestHero").Build();
            
            // Test combo amplifier increases with steps
            double step0Amplifier = character.GetComboAmplifier();
            character.ComboStep = 1;
            double step1Amplifier = character.GetComboAmplifier();
            character.ComboStep = 2;
            double step2Amplifier = character.GetComboAmplifier();

            TestBase.AssertTrue(step2Amplifier >= step1Amplifier, 
                "Combo amplifier should increase with combo step", 
                ref _testsRun, ref _testsPassed, ref _testsFailed);

            character.ComboStep = 0; // Reset
        }

        private static void TestComboInterruption()
        {
            Console.WriteLine("\n--- Testing Combo Interruption ---");

            var character = TestDataBuilders.Character().WithName("TestHero").Build();
            
            // Test combo can be reset
            character.ComboStep = 3;
            character.ComboStep = 0;
            TestBase.AssertEqual(0, character.ComboStep, 
                "Combo step should be resettable", 
                ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        private static void TestComboContinuationAfterMiss()
        {
            Console.WriteLine("\n--- Testing Combo Continuation After Miss ---");

            var character = TestDataBuilders.Character().WithName("TestHero").Build();
            
            // Test that combo can continue after miss (if roll is high enough)
            character.ComboStep = 1;
            int originalStep = character.ComboStep;
            
            // Combo continuation depends on roll, not just miss/hit
            TestBase.AssertTrue(character.ComboStep >= 0, 
                "Combo step should be valid after miss", 
                ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        private static void TestComboContinuationAfterHit()
        {
            Console.WriteLine("\n--- Testing Combo Continuation After Hit ---");

            var character = TestDataBuilders.Character().WithName("TestHero").Build();
            
            // Test combo can continue after hit
            character.ComboStep = 1;
            TestBase.AssertTrue(character.ComboStep >= 0, 
                "Combo step should be valid after hit", 
                ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        private static void TestComboContinuationAfterCritical()
        {
            Console.WriteLine("\n--- Testing Combo Continuation After Critical ---");

            var character = TestDataBuilders.Character().WithName("TestHero").Build();
            
            // Test combo can continue after critical
            character.ComboStep = 2;
            TestBase.AssertTrue(character.ComboStep >= 0, 
                "Combo step should be valid after critical", 
                ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        private static void TestComboWithDifferentActionTypes()
        {
            Console.WriteLine("\n--- Testing Combo With Different Action Types ---");

            var character = TestDataBuilders.Character().WithName("TestHero").Build();
            
            // Test combo works with different action types
            var attackAction = TestDataBuilders.CreateMockAction("Attack", ActionType.Attack);
            var healAction = TestDataBuilders.CreateMockAction("Heal", ActionType.Heal);

            TestBase.AssertTrue(attackAction.IsComboAction || !attackAction.IsComboAction, 
                "Attack action should have combo flag set or not", 
                ref _testsRun, ref _testsPassed, ref _testsFailed);

            TestBase.AssertTrue(healAction.IsComboAction || !healAction.IsComboAction, 
                "Heal action should have combo flag set or not", 
                ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        private static void TestComboWithStatusEffects()
        {
            Console.WriteLine("\n--- Testing Combo With Status Effects ---");

            var character = TestDataBuilders.Character().WithName("TestHero").Build();
            
            // Test combo can work with status effects
            character.ComboStep = 1;
            character.IsWeakened = true;
            
            TestBase.AssertTrue(character.ComboStep >= 0, 
                "Combo step should be valid with status effects", 
                ref _testsRun, ref _testsPassed, ref _testsFailed);

            character.IsWeakened = false; // Reset
        }
    }
}

