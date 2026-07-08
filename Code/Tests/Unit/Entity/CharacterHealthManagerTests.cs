using System;
using RPGGame.Combat.Calculators;
using RPGGame.Tests;

namespace RPGGame.Tests.Unit.Entity
{
    /// <summary>
    /// Comprehensive tests for CharacterHealthManager
    /// Tests health management, damage, healing, and death handling
    /// </summary>
    public static class CharacterHealthManagerTests
    {
        private static int _testsRun = 0;
        private static int _testsPassed = 0;
        private static int _testsFailed = 0;

        /// <summary>
        /// Runs all CharacterHealthManager tests
        /// </summary>
        public static void RunAllTests()
        {
            Console.WriteLine("=== CharacterHealthManager Tests ===\n");
            
            _testsRun = 0;
            _testsPassed = 0;
            _testsFailed = 0;

            TestTakeDamage();
            TestArmorFlatReductionPersists();
            TestArmorUnaffectedByHits();
            TestArmorTracksEquipUnequip();
            TestIsAlive();
            TestGetEffectiveMaxHealth();
            TestGetHealthPercentage();
            TestMeetsHealthThreshold();

            TestBase.PrintSummary("CharacterHealthManager Tests", _testsRun, _testsPassed, _testsFailed);
        }

        #region Damage Tests

        private static void TestTakeDamage()
        {
            Console.WriteLine("--- Testing TakeDamage ---");

            var character = TestDataBuilders.Character()
                .WithName("TestPlayer")
                .WithLevel(1)
                .Build();

            var initialHealth = character.CurrentHealth;
            character.Health.TakeDamage(10);

            TestBase.AssertTrue(character.CurrentHealth < initialHealth,
                "Health should decrease after taking damage",
                ref _testsRun, ref _testsPassed, ref _testsFailed);

            TestBase.AssertTrue(character.CurrentHealth >= 0,
                "Health should not go below 0",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        /// <summary>
        /// Attack damage path subtracts effective armor in DamageCalculator; TakeDamage does not consume armor.
        /// </summary>
        private static void TestArmorFlatReductionPersists()
        {
            Console.WriteLine("\n--- Testing ArmorFlatReductionPersists ---");

            var attacker = TestDataBuilders.Enemy()
                .WithName("Attacker")
                .WithHealth(100)
                .Build();

            var defender = TestDataBuilders.Character()
                .WithName("ArmoredPlayer")
                .WithLevel(1)
                .Build();

            var chest = new ChestItem("TestChest", 1, 10);
            defender.EquipItem(chest, "body");

            TestBase.AssertEqual(10, defender.GetMaxArmor(), "Effective armor should match equipped armor", ref _testsRun, ref _testsPassed, ref _testsFailed);
            TestBase.AssertEqual(10, defender.CurrentArmor, "CurrentArmor should alias effective armor", ref _testsRun, ref _testsPassed, ref _testsFailed);

            var action = TestDataBuilders.CreateMockAction("JAB");
            action.DamageMultiplier = 1.0;

            int raw = DamageCalculator.CalculateRawDamage(attacker, action, 1.0, 1.0, 10);
            int mitigated = DamageCalculator.CalculateDamage(attacker, defender, action, 1.0, 1.0, 0, 10);
            int expected = Math.Max(GameConfiguration.Instance.Combat.MinimumDamage, raw - 10);

            TestBase.AssertEqual(expected, mitigated, "Hero armor should flat-reduce attack damage", ref _testsRun, ref _testsPassed, ref _testsFailed);
            TestBase.AssertEqual(10, defender.GetMaxArmor(), "Armor value should still be present after mitigation calc", ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        private static void TestArmorUnaffectedByHits()
        {
            Console.WriteLine("\n--- Testing ArmorUnaffectedByHits ---");

            var character = TestDataBuilders.Character()
                .WithName("ArmoredPlayer")
                .WithLevel(1)
                .Build();

            var chest = new ChestItem("TestChest", 1, 8);
            character.EquipItem(chest, "body");

            int initialHealth = character.CurrentHealth;
            character.Health.TakeDamage(8);

            TestBase.AssertEqual(8, character.GetMaxArmor(), "TakeDamage must not consume armor", ref _testsRun, ref _testsPassed, ref _testsFailed);
            TestBase.AssertEqual(initialHealth - 8, character.CurrentHealth, "TakeDamage applies amount directly to health", ref _testsRun, ref _testsPassed, ref _testsFailed);

            character.RefreshRoomArmor();
            TestBase.AssertEqual(8, character.GetMaxArmor(), "RefreshRoomArmor is a no-op for flat armor", ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        private static void TestArmorTracksEquipUnequip()
        {
            Console.WriteLine("\n--- Testing ArmorTracksEquipUnequip ---");

            var character = TestDataBuilders.Character()
                .WithName("ArmoredPlayer")
                .WithLevel(1)
                .Build();

            var chest = new ChestItem("HeavyChest", 1, 10);
            character.EquipItem(chest, "body");
            TestBase.AssertEqual(10, character.GetMaxArmor(), "Armor should match equipped piece", ref _testsRun, ref _testsPassed, ref _testsFailed);

            character.UnequipItem("body");
            TestBase.AssertEqual(0, character.GetMaxArmor(), "Unequipping armor should drop effective armor to 0", ref _testsRun, ref _testsPassed, ref _testsFailed);
            TestBase.AssertEqual(0, character.CurrentArmor, "CurrentArmor should match effective armor after unequip", ref _testsRun, ref _testsPassed, ref _testsFailed);

            var upgradedChest = new ChestItem("BetterChest", 1, 8);
            character.EquipItem(upgradedChest, "body");
            character.Health.TakeDamage(4);
            TestBase.AssertEqual(8, character.GetMaxArmor(), "Equipping armor sets effective flat DR; hits do not deplete it", ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        #endregion

        #region Healing Tests

        private static void TestHeal()
        {
            Console.WriteLine("\n--- Testing Heal ---");

            var character = TestDataBuilders.Character()
                .WithName("TestPlayer")
                .WithLevel(1)
                .Build();

            // Take some damage first
            character.Health.TakeDamage(20);
            var healthBeforeHeal = character.CurrentHealth;

            // Heal
            character.Health.Heal(10);

            TestBase.AssertTrue(character.CurrentHealth >= healthBeforeHeal,
                "Health should increase after healing",
                ref _testsRun, ref _testsPassed, ref _testsFailed);

            // Test healing doesn't exceed max
            var maxHealth = character.MaxHealth;
            character.Health.Heal(9999);
            TestBase.AssertTrue(character.CurrentHealth <= maxHealth,
                "Health should not exceed max health",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        #endregion

        #region Status Tests

        private static void TestIsAlive()
        {
            Console.WriteLine("\n--- Testing IsAlive ---");

            var character = TestDataBuilders.Character()
                .WithName("TestPlayer")
                .WithLevel(1)
                .Build();

            // Character should be alive initially
            TestBase.AssertTrue(character.Health.IsAlive,
                "Character should be alive initially",
                ref _testsRun, ref _testsPassed, ref _testsFailed);

            // Take massive damage
            character.Health.TakeDamage(9999);

            // Character should be dead
            TestBase.AssertFalse(character.Health.IsAlive,
                "Character should be dead after taking massive damage",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        private static void TestGetEffectiveMaxHealth()
        {
            Console.WriteLine("\n--- Testing GetEffectiveMaxHealth ---");

            var character = TestDataBuilders.Character()
                .WithName("TestPlayer")
                .WithLevel(1)
                .Build();

            var effectiveMaxHealth = character.Health.GetEffectiveMaxHealth();
            TestBase.AssertTrue(effectiveMaxHealth > 0,
                $"Effective max health should be > 0, got {effectiveMaxHealth}",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        private static void TestGetHealthPercentage()
        {
            Console.WriteLine("\n--- Testing GetHealthPercentage ---");

            var character = TestDataBuilders.Character()
                .WithName("TestPlayer")
                .WithLevel(1)
                .Build();

            var percentage = character.Health.GetHealthPercentage();
            TestBase.AssertTrue(percentage >= 0 && percentage <= 1.0,
                $"Health percentage should be between 0 and 1, got {percentage}",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        private static void TestMeetsHealthThreshold()
        {
            Console.WriteLine("\n--- Testing MeetsHealthThreshold ---");

            var character = TestDataBuilders.Character()
                .WithName("TestPlayer")
                .WithLevel(1)
                .Build();

            // Test at full health (100% health, threshold is 50%)
            // MeetsHealthThreshold returns true if health percentage <= threshold
            // At 100% health, 100% is NOT <= 50%, so should return false
            var meetsThreshold = character.Health.MeetsHealthThreshold(0.5);
            TestBase.AssertFalse(meetsThreshold,
                "Character at full health (100%) should not meet 50% threshold (100% > 50%)",
                ref _testsRun, ref _testsPassed, ref _testsFailed);

            // Take damage to below threshold (0% health)
            // At 0% health, 0% IS <= 50%, so should return true
            character.Health.TakeDamage(character.MaxHealth);
            var meetsThresholdAfterDamage = character.Health.MeetsHealthThreshold(0.5);
            TestBase.AssertTrue(meetsThresholdAfterDamage,
                "Character at 0% health should meet 50% threshold (0% <= 50%)",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        #endregion
    }
}
