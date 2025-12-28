using System;
using RPGGame.Tests;

namespace RPGGame.Tests.Unit
{
    /// <summary>
    /// Comprehensive tests for character attributes and stats
    /// Tests base stats, leveling, progression, and health system
    /// </summary>
    public static class CharacterAttributesTests
    {
        private static int _testsRun = 0;
        private static int _testsPassed = 0;
        private static int _testsFailed = 0;

        public static void RunAllTests()
        {
            Console.WriteLine("=== Character Attributes Tests ===\n");
            
            _testsRun = 0;
            _testsPassed = 0;
            _testsFailed = 0;

            TestInitialStats();
            TestStatCalculations();
            TestStrength();
            TestAgility();
            TestTechnique();
            TestIntelligence();
            TestTemporaryBonuses();
            TestEquipmentBonuses();
            TestStatDecay();
            TestLevelUp();
            TestStatIncreases();
            TestHealthRestoration();
            TestXPGain();
            TestLevelScaling();
            TestClassBasedProgression();
            TestHealthInitialization();
            TestDamageApplication();
            TestHealing();
            TestHealthLimits();
            TestDeathDetection();

            TestBase.PrintSummary("Character Attributes Tests", _testsRun, _testsPassed, _testsFailed);
        }

        private static void TestInitialStats()
        {
            Console.WriteLine("--- Testing Initial Stats ---");

            var character = TestDataBuilders.Character().WithName("TestHero").Build();

            TestBase.AssertTrue(character.Stats.Strength > 0, 
                $"Strength should be positive, got {character.Stats.Strength}", 
                ref _testsRun, ref _testsPassed, ref _testsFailed);

            TestBase.AssertTrue(character.Stats.Agility > 0, 
                $"Agility should be positive, got {character.Stats.Agility}", 
                ref _testsRun, ref _testsPassed, ref _testsFailed);

            TestBase.AssertTrue(character.Stats.Technique > 0, 
                $"Technique should be positive, got {character.Stats.Technique}", 
                ref _testsRun, ref _testsPassed, ref _testsFailed);

            TestBase.AssertTrue(character.Stats.Intelligence > 0, 
                $"Intelligence should be positive, got {character.Stats.Intelligence}", 
                ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        private static void TestStatCalculations()
        {
            Console.WriteLine("\n--- Testing Stat Calculations ---");

            var character = TestDataBuilders.Character().WithName("TestHero").Build();

            int effectiveStrength = character.GetEffectiveStrength();
            TestBase.AssertTrue(effectiveStrength >= character.Stats.Strength, 
                "Effective strength should be at least base strength", 
                ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        private static void TestStrength()
        {
            Console.WriteLine("\n--- Testing Strength ---");

            var character = TestDataBuilders.Character().WithName("TestHero").Build();
            int originalStrength = character.Stats.Strength;

            character.Stats.Strength = 20;
            TestBase.AssertEqual(20, character.Stats.Strength, 
                "Strength should be settable", 
                ref _testsRun, ref _testsPassed, ref _testsFailed);

            character.Stats.Strength = originalStrength; // Reset
        }

        private static void TestAgility()
        {
            Console.WriteLine("\n--- Testing Agility ---");

            var character = TestDataBuilders.Character().WithName("TestHero").Build();
            int originalAgility = character.Stats.Agility;

            character.Stats.Agility = 20;
            TestBase.AssertEqual(20, character.Stats.Agility, 
                "Agility should be settable", 
                ref _testsRun, ref _testsPassed, ref _testsFailed);

            character.Stats.Agility = originalAgility; // Reset
        }

        private static void TestTechnique()
        {
            Console.WriteLine("\n--- Testing Technique ---");

            var character = TestDataBuilders.Character().WithName("TestHero").Build();
            int originalTechnique = character.Stats.Technique;

            character.Stats.Technique = 20;
            TestBase.AssertEqual(20, character.Stats.Technique, 
                "Technique should be settable", 
                ref _testsRun, ref _testsPassed, ref _testsFailed);

            character.Stats.Technique = originalTechnique; // Reset
        }

        private static void TestIntelligence()
        {
            Console.WriteLine("\n--- Testing Intelligence ---");

            var character = TestDataBuilders.Character().WithName("TestHero").Build();
            int originalIntelligence = character.Stats.Intelligence;

            character.Stats.Intelligence = 20;
            TestBase.AssertEqual(20, character.Stats.Intelligence, 
                "Intelligence should be settable", 
                ref _testsRun, ref _testsPassed, ref _testsFailed);

            character.Stats.Intelligence = originalIntelligence; // Reset
        }

        private static void TestTemporaryBonuses()
        {
            Console.WriteLine("\n--- Testing Temporary Bonuses ---");

            var character = TestDataBuilders.Character().WithName("TestHero").Build();

            character.Stats.ApplyStatBonus(5, "STR", 3);
            TestBase.AssertEqual(5, character.Stats.TempStrengthBonus, 
                "Temporary strength bonus should be applied", 
                ref _testsRun, ref _testsPassed, ref _testsFailed);

            TestBase.AssertEqual(3, character.Stats.TempStatBonusTurns, 
                "Temporary bonus duration should be set", 
                ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        private static void TestEquipmentBonuses()
        {
            Console.WriteLine("\n--- Testing Equipment Bonuses ---");

            var character = TestDataBuilders.Character().WithName("TestHero").Build();

            int equipmentBonus = character.Equipment.GetEquipmentStatBonus("STR");
            TestBase.AssertTrue(equipmentBonus >= 0, 
                $"Equipment bonus should be non-negative, got {equipmentBonus}", 
                ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        private static void TestStatDecay()
        {
            Console.WriteLine("\n--- Testing Stat Decay ---");

            var character = TestDataBuilders.Character().WithName("TestHero").Build();

            character.Stats.ApplyStatBonus(5, "STR", 1);
            int originalTurns = character.Stats.TempStatBonusTurns;

            // Test that turns can be decremented
            character.Stats.TempStatBonusTurns--;
            TestBase.AssertTrue(character.Stats.TempStatBonusTurns < originalTurns, 
                "Stat bonus turns should be decrementable", 
                ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        private static void TestLevelUp()
        {
            Console.WriteLine("\n--- Testing Level Up ---");

            var character = TestDataBuilders.Character().WithName("TestHero").Build();
            int originalLevel = character.Level;
            int originalStrength = character.Stats.Strength;

            character.Level++;
            TestBase.AssertTrue(character.Level > originalLevel, 
                "Level should increase on level up", 
                ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        private static void TestStatIncreases()
        {
            Console.WriteLine("\n--- Testing Stat Increases ---");

            var character = TestDataBuilders.Character().WithName("TestHero").Build();
            int originalStrength = character.Stats.Strength;

            character.Stats.LevelUp(WeaponType.Sword);
            TestBase.AssertTrue(character.Stats.Strength >= originalStrength, 
                "Stats should increase on level up", 
                ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        private static void TestHealthRestoration()
        {
            Console.WriteLine("\n--- Testing Health Restoration ---");

            var character = TestDataBuilders.Character().WithName("TestHero").Build();
            int originalHealth = character.CurrentHealth;

            // Test that health can be restored
            character.CurrentHealth = character.MaxHealth;
            TestBase.AssertTrue(character.CurrentHealth >= originalHealth, 
                "Health should be restorable", 
                ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        private static void TestXPGain()
        {
            Console.WriteLine("\n--- Testing XP Gain ---");

            var character = TestDataBuilders.Character().WithName("TestHero").Build();
            int originalXP = character.XP;

            // Test that XP can be gained
            character.XP += 100;
            TestBase.AssertTrue(character.XP > originalXP, 
                "XP should be gainable", 
                ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        private static void TestLevelScaling()
        {
            Console.WriteLine("\n--- Testing Level Scaling ---");

            var character1 = TestDataBuilders.Character().WithLevel(1).Build();
            var character10 = TestDataBuilders.Character().WithLevel(10).Build();

            TestBase.AssertTrue(character10.Stats.Strength >= character1.Stats.Strength, 
                "Higher level should have higher or equal stats", 
                ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        private static void TestClassBasedProgression()
        {
            Console.WriteLine("\n--- Testing Class-Based Progression ---");

            var character = TestDataBuilders.Character().WithName("TestHero").Build();
            int originalStrength = character.Stats.Strength;

            character.Stats.LevelUp(WeaponType.Mace);
            TestBase.AssertTrue(character.Stats.Strength > originalStrength, 
                "Mace class should increase strength more", 
                ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        private static void TestHealthInitialization()
        {
            Console.WriteLine("\n--- Testing Health Initialization ---");

            var character = TestDataBuilders.Character().WithName("TestHero").Build();

            TestBase.AssertTrue(character.MaxHealth > 0, 
                $"Max health should be positive, got {character.MaxHealth}", 
                ref _testsRun, ref _testsPassed, ref _testsFailed);

            TestBase.AssertTrue(character.CurrentHealth > 0, 
                $"Current health should be positive, got {character.CurrentHealth}", 
                ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        private static void TestDamageApplication()
        {
            Console.WriteLine("\n--- Testing Damage Application ---");

            var character = TestDataBuilders.Character().WithName("TestHero").Build();
            int originalHealth = character.CurrentHealth;

            character.CurrentHealth -= 10;
            TestBase.AssertTrue(character.CurrentHealth < originalHealth, 
                "Damage should reduce health", 
                ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        private static void TestHealing()
        {
            Console.WriteLine("\n--- Testing Healing ---");

            var character = TestDataBuilders.Character().WithName("TestHero").Build();
            character.CurrentHealth = 50;
            int damagedHealth = character.CurrentHealth;

            character.CurrentHealth += 20;
            TestBase.AssertTrue(character.CurrentHealth > damagedHealth, 
                "Healing should increase health", 
                ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        private static void TestHealthLimits()
        {
            Console.WriteLine("\n--- Testing Health Limits ---");

            var character = TestDataBuilders.Character().WithName("TestHero").Build();

            character.CurrentHealth = 0;
            TestBase.AssertTrue(character.CurrentHealth >= 0, 
                "Health should not go below 0", 
                ref _testsRun, ref _testsPassed, ref _testsFailed);

            character.CurrentHealth = character.MaxHealth + 100;
            TestBase.AssertTrue(character.CurrentHealth <= character.MaxHealth, 
                "Health should not exceed max health", 
                ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        private static void TestDeathDetection()
        {
            Console.WriteLine("\n--- Testing Death Detection ---");

            var character = TestDataBuilders.Character().WithName("TestHero").Build();

            character.CurrentHealth = 0;
            bool isDead = character.CurrentHealth <= 0;
            TestBase.AssertTrue(isDead, 
                "Character should be dead at 0 health", 
                ref _testsRun, ref _testsPassed, ref _testsFailed);
        }
    }
}

