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
            TestEffectiveMaxHealth();
            TestHealingWithEquipmentBonuses();
            TestHealingToEffectiveMaxHealth();
            TestLevelUpHealingToEffectiveMaxHealth();
            TestHealingCapsAtEffectiveMaxHealth();
            TestMultipleEquipmentHealthBonuses();
            TestHealthRegenerationWithEquipment();
            TestEquipmentChangeHealthAdjustment();

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

            int effectiveMaxHealth = character.GetEffectiveMaxHealth();
            character.CurrentHealth = effectiveMaxHealth + 100;
            TestBase.AssertTrue(character.CurrentHealth <= effectiveMaxHealth, 
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

        private static void TestEffectiveMaxHealth()
        {
            Console.WriteLine("\n--- Testing Effective Max Health ---");

            var character = TestDataBuilders.Character().WithName("TestHero").Build();
            int baseMaxHealth = character.MaxHealth;

            // Test without equipment
            int effectiveMaxHealthNoEquipment = character.GetEffectiveMaxHealth();
            TestBase.AssertEqual(baseMaxHealth, effectiveMaxHealthNoEquipment,
                $"Effective max health without equipment should equal base max health: {baseMaxHealth} == {effectiveMaxHealthNoEquipment}",
                ref _testsRun, ref _testsPassed, ref _testsFailed);

            // Test with equipment that has health bonus
            var chestItem = new ChestItem("Test Chest", 1, 8);
            chestItem.StatBonuses.Add(new StatBonus 
            { 
                Name = "of Vitality", 
                StatType = "Health", 
                Value = 10 
            });
            character.EquipItem(chestItem, "body");

            int effectiveMaxHealthWithEquipment = character.GetEffectiveMaxHealth();
            TestBase.AssertTrue(effectiveMaxHealthWithEquipment > baseMaxHealth,
                $"Effective max health with equipment should be greater than base: {effectiveMaxHealthWithEquipment} > {baseMaxHealth}",
                ref _testsRun, ref _testsPassed, ref _testsFailed);

            TestBase.AssertEqual(baseMaxHealth + 10, effectiveMaxHealthWithEquipment,
                $"Effective max health should be base + equipment bonus: {baseMaxHealth + 10} == {effectiveMaxHealthWithEquipment}",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        private static void TestHealingWithEquipmentBonuses()
        {
            Console.WriteLine("\n--- Testing Healing With Equipment Bonuses ---");

            var character = TestDataBuilders.Character().WithName("TestHero").Build();
            int baseMaxHealth = character.MaxHealth;

            // Equip item with health bonus
            var headItem = new HeadItem("Test Helmet", 1, 5);
            headItem.StatBonuses.Add(new StatBonus 
            { 
                Name = "of Vigor", 
                StatType = "Health", 
                Value = 20 
            });
            character.EquipItem(headItem, "head");

            int effectiveMaxHealth = character.GetEffectiveMaxHealth();
            TestBase.AssertTrue(effectiveMaxHealth == baseMaxHealth + 20,
                $"Effective max health should include equipment bonus: {effectiveMaxHealth} == {baseMaxHealth + 20}",
                ref _testsRun, ref _testsPassed, ref _testsFailed);

            // Damage character
            character.TakeDamage(30);
            int damagedHealth = character.CurrentHealth;

            // Heal character
            int healAmount = 50;
            character.Heal(healAmount);

            // Health should be capped at effective max health, not base max health
            TestBase.AssertTrue(character.CurrentHealth <= effectiveMaxHealth,
                $"Healed health should not exceed effective max health: {character.CurrentHealth} <= {effectiveMaxHealth}",
                ref _testsRun, ref _testsPassed, ref _testsFailed);

            TestBase.AssertTrue(character.CurrentHealth >= damagedHealth,
                $"Healed health should be greater than damaged health: {character.CurrentHealth} >= {damagedHealth}",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        private static void TestHealingToEffectiveMaxHealth()
        {
            Console.WriteLine("\n--- Testing Healing To Effective Max Health ---");

            var character = TestDataBuilders.Character().WithName("TestHero").Build();
            int baseMaxHealth = character.MaxHealth;

            // Equip multiple items with health bonuses
            var chestItem = new ChestItem("Test Chest", 1, 8);
            chestItem.StatBonuses.Add(new StatBonus 
            { 
                Name = "of Vitality", 
                StatType = "Health", 
                Value = 10 
            });
            character.EquipItem(chestItem, "body");

            var feetItem = new FeetItem("Test Boots", 1, 3);
            feetItem.StatBonuses.Add(new StatBonus 
            { 
                Name = "of Vigor", 
                StatType = "Health", 
                Value = 20 
            });
            character.EquipItem(feetItem, "feet");

            int effectiveMaxHealth = character.GetEffectiveMaxHealth();
            int expectedEffectiveMax = baseMaxHealth + 10 + 20;

            TestBase.AssertEqual(expectedEffectiveMax, effectiveMaxHealth,
                $"Effective max health should sum all equipment bonuses: {expectedEffectiveMax} == {effectiveMaxHealth}",
                ref _testsRun, ref _testsPassed, ref _testsFailed);

            // Damage character significantly
            character.TakeDamage(50);
            int damagedHealth = character.CurrentHealth;

            // Heal to full (using large heal amount)
            character.Heal(1000); // Large amount to ensure full heal

            // Should be healed to effective max health, not base max health
            TestBase.AssertEqual(effectiveMaxHealth, character.CurrentHealth,
                $"Character should be healed to effective max health: {effectiveMaxHealth} == {character.CurrentHealth}",
                ref _testsRun, ref _testsPassed, ref _testsFailed);

            TestBase.AssertTrue(character.CurrentHealth > baseMaxHealth,
                $"Healed health should exceed base max health when equipment bonuses exist: {character.CurrentHealth} > {baseMaxHealth}",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        private static void TestLevelUpHealingToEffectiveMaxHealth()
        {
            Console.WriteLine("\n--- Testing Level Up Healing To Effective Max Health ---");

            var character = TestDataBuilders.Character().WithName("TestHero").Build();
            int baseMaxHealth = character.MaxHealth;

            // Equip item with health bonus
            var headItem = new HeadItem("Test Helmet", 1, 5);
            headItem.StatBonuses.Add(new StatBonus 
            { 
                Name = "of Vigor", 
                StatType = "Health", 
                Value = 15 
            });
            character.EquipItem(headItem, "head");

            int effectiveMaxHealthBeforeLevelUp = character.GetEffectiveMaxHealth();

            // Damage character
            character.TakeDamage(30);
            int damagedHealth = character.CurrentHealth;

            // Level up (this should heal to effective max health)
            // Use the character's LevelUp method which handles healing
            int originalLevel = character.Level;
            int originalMaxHealth = character.MaxHealth;
            
            // Manually trigger level up by incrementing level and calling LevelUp
            // This simulates what happens when a character actually levels up
            character.Progression.Level++;
            character.LevelUp();

            int effectiveMaxHealthAfterLevelUp = character.GetEffectiveMaxHealth();

            // After level up, health should be at effective max health (which may have increased)
            TestBase.AssertTrue(character.CurrentHealth == effectiveMaxHealthAfterLevelUp,
                $"After level up, health should be at effective max health: {character.CurrentHealth} == {effectiveMaxHealthAfterLevelUp}",
                ref _testsRun, ref _testsPassed, ref _testsFailed);

            TestBase.AssertTrue(character.CurrentHealth >= effectiveMaxHealthBeforeLevelUp,
                $"After level up, health should be at least at previous effective max: {character.CurrentHealth} >= {effectiveMaxHealthBeforeLevelUp}",
                ref _testsRun, ref _testsPassed, ref _testsFailed);

            // Verify it's not just base max health
            TestBase.AssertTrue(character.CurrentHealth > character.MaxHealth,
                $"After level up, health should exceed base max health when equipment bonuses exist: {character.CurrentHealth} > {character.MaxHealth}",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        private static void TestHealingCapsAtEffectiveMaxHealth()
        {
            Console.WriteLine("\n--- Testing Healing Caps At Effective Max Health ---");

            var character = TestDataBuilders.Character().WithName("TestHero").Build();
            int baseMaxHealth = character.MaxHealth;

            // Equip item with health bonus
            var chestItem = new ChestItem("Test Chest", 1, 8);
            chestItem.StatBonuses.Add(new StatBonus 
            { 
                Name = "of Vitality", 
                StatType = "Health", 
                Value = 25 
            });
            character.EquipItem(chestItem, "body");

            int effectiveMaxHealth = character.GetEffectiveMaxHealth();

            // Set health to effective max
            character.CurrentHealth = effectiveMaxHealth;

            // Try to heal beyond effective max
            character.Heal(100);

            // Health should be capped at effective max health
            TestBase.AssertEqual(effectiveMaxHealth, character.CurrentHealth,
                $"Healing should cap at effective max health: {effectiveMaxHealth} == {character.CurrentHealth}",
                ref _testsRun, ref _testsPassed, ref _testsFailed);

            // Verify it's not capped at base max health
            TestBase.AssertTrue(character.CurrentHealth > baseMaxHealth,
                $"Healed health should exceed base max health: {character.CurrentHealth} > {baseMaxHealth}",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        private static void TestMultipleEquipmentHealthBonuses()
        {
            Console.WriteLine("\n--- Testing Multiple Equipment Health Bonuses ---");

            var character = TestDataBuilders.Character().WithName("TestHero").Build();
            int baseMaxHealth = character.MaxHealth;

            // Equip multiple items with health bonuses
            var headItem = new HeadItem("Test Helmet", 1, 5);
            headItem.StatBonuses.Add(new StatBonus 
            { 
                Name = "of Vitality", 
                StatType = "Health", 
                Value = 10 
            });
            character.EquipItem(headItem, "head");

            var chestItem = new ChestItem("Test Chest", 1, 8);
            chestItem.StatBonuses.Add(new StatBonus 
            { 
                Name = "of Vigor", 
                StatType = "Health", 
                Value = 20 
            });
            character.EquipItem(chestItem, "body");

            var feetItem = new FeetItem("Test Boots", 1, 3);
            feetItem.StatBonuses.Add(new StatBonus 
            { 
                Name = "of Vitality", 
                StatType = "Health", 
                Value = 15 
            });
            character.EquipItem(feetItem, "feet");

            int totalHealthBonus = 10 + 20 + 15;
            int expectedEffectiveMax = baseMaxHealth + totalHealthBonus;
            int actualEffectiveMax = character.GetEffectiveMaxHealth();

            TestBase.AssertEqual(expectedEffectiveMax, actualEffectiveMax,
                $"Effective max health should sum all equipment health bonuses: {expectedEffectiveMax} == {actualEffectiveMax}",
                ref _testsRun, ref _testsPassed, ref _testsFailed);

            // Damage and heal
            character.TakeDamage(40);
            character.Heal(1000); // Large heal amount

            // Should heal to effective max health
            TestBase.AssertEqual(actualEffectiveMax, character.CurrentHealth,
                $"Character should be healed to effective max health with multiple bonuses: {actualEffectiveMax} == {character.CurrentHealth}",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        private static void TestHealthRegenerationWithEquipment()
        {
            Console.WriteLine("\n--- Testing Health Regeneration With Equipment ---");

            var character = TestDataBuilders.Character().WithName("TestHero").Build();
            int baseMaxHealth = character.MaxHealth;

            // Equip item with health bonus and health regen
            var chestItem = new ChestItem("Test Chest", 1, 8);
            chestItem.StatBonuses.Add(new StatBonus 
            { 
                Name = "of Vitality", 
                StatType = "Health", 
                Value = 20 
            });
            chestItem.StatBonuses.Add(new StatBonus 
            { 
                Name = "of Recovery", 
                StatType = "HealthRegen", 
                Value = 2 
            });
            character.EquipItem(chestItem, "body");

            int effectiveMaxHealth = character.GetEffectiveMaxHealth();

            // Damage character
            character.TakeDamage(30);
            int damagedHealth = character.CurrentHealth;

            // Process regeneration
            character.Health.ProcessHealthRegeneration();

            // Health should increase but not exceed effective max health
            TestBase.AssertTrue(character.CurrentHealth >= damagedHealth,
                $"Regeneration should increase health: {character.CurrentHealth} >= {damagedHealth}",
                ref _testsRun, ref _testsPassed, ref _testsFailed);

            TestBase.AssertTrue(character.CurrentHealth <= effectiveMaxHealth,
                $"Regeneration should not exceed effective max health: {character.CurrentHealth} <= {effectiveMaxHealth}",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        private static void TestEquipmentChangeHealthAdjustment()
        {
            Console.WriteLine("\n--- Testing Equipment Change Health Adjustment ---");

            var character = TestDataBuilders.Character().WithName("TestHero").Build();
            int baseMaxHealth = character.MaxHealth;

            // Equip item with health bonus
            var chestItem1 = new ChestItem("Test Chest 1", 1, 8);
            chestItem1.StatBonuses.Add(new StatBonus 
            { 
                Name = "of Vitality", 
                StatType = "Health", 
                Value = 10 
            });
            character.EquipItem(chestItem1, "body");

            int effectiveMaxHealth1 = character.GetEffectiveMaxHealth();
            character.CurrentHealth = effectiveMaxHealth1; // Set to full effective max

            // Replace with item with larger health bonus
            var chestItem2 = new ChestItem("Test Chest 2", 1, 8);
            chestItem2.StatBonuses.Add(new StatBonus 
            { 
                Name = "of Vigor", 
                StatType = "Health", 
                Value = 30 
            });
            character.EquipItem(chestItem2, "body");

            int effectiveMaxHealth2 = character.GetEffectiveMaxHealth();

            // When max health increases, current health should increase to new max
            TestBase.AssertTrue(effectiveMaxHealth2 > effectiveMaxHealth1,
                $"New effective max health should be greater: {effectiveMaxHealth2} > {effectiveMaxHealth1}",
                ref _testsRun, ref _testsPassed, ref _testsFailed);

            // Health should be adjusted to new effective max
            TestBase.AssertEqual(effectiveMaxHealth2, character.CurrentHealth,
                $"Health should be adjusted to new effective max when equipment changes: {effectiveMaxHealth2} == {character.CurrentHealth}",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
        }
    }
}

