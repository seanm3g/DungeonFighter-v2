using System;
using RPGGame.Tests;

namespace RPGGame.Tests.Unit
{
    /// <summary>
    /// Tests for roll bonus calculations
    /// Tests base roll bonuses, equipment bonuses, combo bonuses, and combined calculations
    /// </summary>
    public static class RollBonusTests
    {
        private static int _testsRun = 0;
        private static int _testsPassed = 0;
        private static int _testsFailed = 0;

        public static void RunAllTests()
        {
            Console.WriteLine("=== Roll Bonus Tests ===\n");
            
            _testsRun = 0;
            _testsPassed = 0;
            _testsFailed = 0;

            TestBaseRollBonusFromAttributes();
            TestEquipmentRollBonuses();
            TestComboRollBonuses();
            TestTemporaryRollBonuses();
            TestRollPenalties();
            TestIntelligenceBasedRollBonuses();
            TestCombinedRollBonusCalculations();

            TestBase.PrintSummary("Roll Bonus Tests", _testsRun, _testsPassed, _testsFailed);
        }

        private static void TestBaseRollBonusFromAttributes()
        {
            Console.WriteLine("--- Testing Base Roll Bonus from Attributes ---");

            var character = TestDataBuilders.Character().WithName("TestHero").Build();
            
            // Test intelligence roll bonus calculation
            int intBonus = character.GetIntelligenceRollBonus();
            TestBase.AssertTrue(intBonus >= 0, 
                $"Intelligence roll bonus should be non-negative, got {intBonus}", 
                ref _testsRun, ref _testsPassed, ref _testsFailed);

            // Test that higher intelligence gives higher bonus
            character.Stats.Intelligence = 20;
            int highIntBonus = character.GetIntelligenceRollBonus();
            TestBase.AssertTrue(highIntBonus >= intBonus, 
                $"Higher intelligence should give higher or equal roll bonus", 
                ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        private static void TestEquipmentRollBonuses()
        {
            Console.WriteLine("\n--- Testing Equipment Roll Bonuses ---");

            var character = TestDataBuilders.Character().WithName("TestHero").Build();
            
            // Test equipment roll bonus calculation
            int equipmentBonus = character.Equipment.GetEquipmentRollBonus();
            TestBase.AssertTrue(equipmentBonus >= 0, 
                $"Equipment roll bonus should be non-negative, got {equipmentBonus}", 
                ref _testsRun, ref _testsPassed, ref _testsFailed);

            // Test modification roll bonus
            int modBonus = character.Equipment.GetModificationRollBonus();
            TestBase.AssertTrue(modBonus >= 0, 
                $"Modification roll bonus should be non-negative, got {modBonus}", 
                ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        private static void TestComboRollBonuses()
        {
            Console.WriteLine("\n--- Testing Combo Roll Bonuses ---");

            var character = TestDataBuilders.Character().WithName("TestHero").Build();
            
            // Test combo bonus calculation
            int comboBonus = character.Effects.ComboBonus;
            TestBase.AssertTrue(comboBonus >= 0, 
                $"Combo bonus should be non-negative, got {comboBonus}", 
                ref _testsRun, ref _testsPassed, ref _testsFailed);

            // Test temporary combo bonus
            int tempComboBonus = character.Effects.TempComboBonus;
            TestBase.AssertTrue(tempComboBonus >= 0, 
                $"Temporary combo bonus should be non-negative, got {tempComboBonus}", 
                ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        private static void TestTemporaryRollBonuses()
        {
            Console.WriteLine("\n--- Testing Temporary Roll Bonuses ---");

            var character = TestDataBuilders.Character().WithName("TestHero").Build();
            
            // Test temporary roll bonus
            int tempBonus = character.Effects.GetTempRollBonus();
            TestBase.AssertTrue(tempBonus >= 0, 
                $"Temporary roll bonus should be non-negative, got {tempBonus}", 
                ref _testsRun, ref _testsPassed, ref _testsFailed);

            // Test setting temporary roll bonus
            character.Effects.TempRollBonus = 5;
            character.Effects.TempRollBonusTurns = 3;
            int setBonus = character.Effects.GetTempRollBonus();
            TestBase.AssertEqual(5, setBonus, 
                "Temporary roll bonus should be settable", 
                ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        private static void TestRollPenalties()
        {
            Console.WriteLine("\n--- Testing Roll Penalties ---");

            var character = TestDataBuilders.Character().WithName("TestHero").Build();
            
            // Test roll penalty
            int rollPenalty = character.RollPenalty;
            TestBase.AssertTrue(rollPenalty >= 0, 
                $"Roll penalty should be non-negative, got {rollPenalty}", 
                ref _testsRun, ref _testsPassed, ref _testsFailed);

            // Test setting roll penalty
            character.RollPenalty = 3;
            TestBase.AssertEqual(3, character.RollPenalty, 
                "Roll penalty should be settable", 
                ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        private static void TestIntelligenceBasedRollBonuses()
        {
            Console.WriteLine("\n--- Testing Intelligence-Based Roll Bonuses ---");

            var character = TestDataBuilders.Character().WithName("TestHero").Build();
            var enemy = TestDataBuilders.Enemy().WithName("TestEnemy").Build();

            // Test character intelligence bonus
            int charIntBonus = character.GetIntelligenceRollBonus();
            TestBase.AssertTrue(charIntBonus >= 0, 
                $"Character intelligence bonus should be non-negative, got {charIntBonus}", 
                ref _testsRun, ref _testsPassed, ref _testsFailed);

            // Test enemy intelligence bonus
            int enemyIntBonus = enemy.GetIntelligenceRollBonus();
            TestBase.AssertTrue(enemyIntBonus >= 0, 
                $"Enemy intelligence bonus should be non-negative, got {enemyIntBonus}", 
                ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        private static void TestCombinedRollBonusCalculations()
        {
            Console.WriteLine("\n--- Testing Combined Roll Bonus Calculations ---");

            var character = TestDataBuilders.Character().WithName("TestHero").Build();
            var action = TestDataBuilders.CreateMockAction("TestAction");

            // Test combined roll bonus calculation
            var comboActions = character.GetComboActions();
            int comboStep = character.ComboStep;
            int totalBonus = CombatCalculator.CalculateRollBonus(character, action, comboActions, comboStep);
            TestBase.AssertTrue(totalBonus >= 0, 
                $"Total roll bonus should be non-negative, got {totalBonus}", 
                ref _testsRun, ref _testsPassed, ref _testsFailed);

            // Test that roll bonus includes all sources
            int intBonus = character.GetIntelligenceRollBonus();
            int equipBonus = character.Equipment.GetEquipmentRollBonus();
            int modBonus = character.Equipment.GetModificationRollBonus();
            
            // Total should be at least the sum of individual bonuses (may have more from combo/temp)
            TestBase.AssertTrue(totalBonus >= (intBonus + equipBonus + modBonus), 
                $"Total bonus should include all sources", 
                ref _testsRun, ref _testsPassed, ref _testsFailed);
        }
    }
}

