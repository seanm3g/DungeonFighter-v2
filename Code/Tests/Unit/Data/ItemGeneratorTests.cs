using System;
using System.Collections.Generic;
using RPGGame;
using RPGGame.Tests;

namespace RPGGame.Tests.Unit.Data
{
    /// <summary>
    /// Comprehensive tests for ItemGenerator
    /// Tests item generation, tier selection, and name generation
    /// </summary>
    public static class ItemGeneratorTests
    {
        private static int _testsRun = 0;
        private static int _testsPassed = 0;
        private static int _testsFailed = 0;

        /// <summary>
        /// Runs all ItemGenerator tests
        /// </summary>
        public static void RunAllTests()
        {
            Console.WriteLine("=== ItemGenerator Tests ===\n");
            
            _testsRun = 0;
            _testsPassed = 0;
            _testsFailed = 0;

            TestGenerateWeaponItem();
            TestGenerateWeaponItem_RollsDamageBonusInclusive();
            TestGenerateWeaponItem_CopiesTags();
            TestGenerateArmorItem();
            TestGenerateArmorItem_CopiesTags();
            TestSelectRandomItemByTier();
            TestGenerateItemNameWithBonuses();
            TestGenerateItemNameWithBonuses_IdempotentWhenItemNameAlreadyComposed();
            TestGenerateItemNameWithBonuses_IdempotentWithEqualLengthStatSuffixes();

            TestBase.PrintSummary("ItemGenerator Tests", _testsRun, _testsPassed, _testsFailed);
        }

        #region Weapon Generation Tests

        private static void TestGenerateWeaponItem()
        {
            Console.WriteLine("--- Testing GenerateWeaponItem ---");

            var weaponData = new WeaponData
            {
                Name = "Test Sword",
                Type = "Sword",
                Tier = 1,
                BaseDamage = 10,
                AttackSpeed = 0.05
            };

            var weapon = ItemGenerator.GenerateWeaponItem(weaponData);

            TestBase.AssertNotNull(weapon,
                "Should generate weapon item",
                ref _testsRun, ref _testsPassed, ref _testsFailed);

            if (weapon != null)
            {
                TestBase.AssertEqual("Test Sword", weapon.Name,
                    "Weapon should have correct name",
                    ref _testsRun, ref _testsPassed, ref _testsFailed);

                TestBase.AssertEqual(1, weapon.Tier,
                    "Weapon should have correct tier",
                    ref _testsRun, ref _testsPassed, ref _testsFailed);

                TestBase.AssertEqual(10, weapon.BaseDamage,
                    "Weapon should have catalog base damage",
                    ref _testsRun, ref _testsPassed, ref _testsFailed);

                TestBase.AssertEqual(0, weapon.RolledDamageBonus,
                    "Weapon with no damage bonus range should have zero rolled bonus",
                    ref _testsRun, ref _testsPassed, ref _testsFailed);

                TestBase.AssertEqual(10, weapon.GetTotalDamage(),
                    "Total damage should match base when roll and tier bonus are zero",
                    ref _testsRun, ref _testsPassed, ref _testsFailed);

                TestBase.AssertEqual(0.05, weapon.BaseAttackSpeed,
                    "Weapon should have correct attack speed",
                    ref _testsRun, ref _testsPassed, ref _testsFailed);
            }
        }

        private static void TestGenerateWeaponItem_RollsDamageBonusInclusive()
        {
            Console.WriteLine("\n--- Testing GenerateWeaponItem damage bonus roll ---");

            var weaponData = new WeaponData
            {
                Name = "Bonus Blade",
                Type = "Sword",
                Tier = 1,
                BaseDamage = 3,
                DamageBonusMin = 0,
                DamageBonusMax = 2,
                AttackSpeed = 1.0
            };

            var seen = new HashSet<int>();
            for (int i = 0; i < 400; i++)
            {
                var w = ItemGenerator.GenerateWeaponItem(weaponData);
                TestBase.AssertEqual(weaponData.BaseDamage, w.BaseDamage,
                    "catalog base damage should stay on BaseDamage",
                    ref _testsRun, ref _testsPassed, ref _testsFailed);
                int bonus = w.RolledDamageBonus;
                TestBase.AssertTrue(bonus >= 0 && bonus <= 2,
                    "rolled bonus must be within inclusive 0..2",
                    ref _testsRun, ref _testsPassed, ref _testsFailed);
                TestBase.AssertEqual(weaponData.BaseDamage + bonus, w.GetTotalDamage(),
                    "total should be base + rolled when tier bonus is zero",
                    ref _testsRun, ref _testsPassed, ref _testsFailed);
                seen.Add(bonus);
            }

            TestBase.AssertEqual(3, seen.Count,
                "over many rolls, all inclusive outcomes 0,1,2 should appear",
                ref _testsRun, ref _testsPassed, ref _testsFailed);

            var fixedBonus = new WeaponData
            {
                Name = "Fixed",
                Type = "Sword",
                Tier = 1,
                BaseDamage = 10,
                DamageBonusMin = 4,
                DamageBonusMax = 4,
                AttackSpeed = 1.0
            };
            var wFixed = ItemGenerator.GenerateWeaponItem(fixedBonus);
            TestBase.AssertEqual(10, wFixed.BaseDamage,
                "catalog base unchanged",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
            TestBase.AssertEqual(4, wFixed.RolledDamageBonus,
                "min equals max implies deterministic rolled bonus",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
            TestBase.AssertEqual(14, wFixed.GetTotalDamage(),
                "total is base + roll when tier bonus is zero",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        private static void TestGenerateWeaponItem_CopiesTags()
        {
            Console.WriteLine("\n--- Testing GenerateWeaponItem copies tags ---");
            var weaponData = new WeaponData
            {
                Name = "Tagged Blade",
                Type = "Sword",
                Tier = 1,
                BaseDamage = 5,
                AttackSpeed = 1.0,
                Tags = new List<string> { "magical", " Magical ", "rare" }
            };
            var weapon = ItemGenerator.GenerateWeaponItem(weaponData);
            TestBase.AssertNotNull(weapon, "weapon", ref _testsRun, ref _testsPassed, ref _testsFailed);
            if (weapon == null) return;
            TestBase.AssertEqual(2, weapon.Tags.Count, "duplicate tag normalized", ref _testsRun, ref _testsPassed, ref _testsFailed);
            TestBase.AssertTrue(weapon.Tags.Contains("magical") && weapon.Tags.Contains("rare"),
                "expected tags", ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        #endregion

        #region Armor Generation Tests

        private static void TestGenerateArmorItem()
        {
            Console.WriteLine("\n--- Testing GenerateArmorItem ---");

            // Test Head item
            var headData = new ArmorData
            {
                Name = "Test Helmet",
                Slot = "head",
                Tier = 1,
                Armor = 5
            };

            var headItem = ItemGenerator.GenerateArmorItem(headData);
            TestBase.AssertNotNull(headItem,
                "Should generate head item",
                ref _testsRun, ref _testsPassed, ref _testsFailed);

            if (headItem != null)
            {
                TestBase.AssertEqual("Test Helmet", headItem.Name,
                    "Armor should have correct name",
                    ref _testsRun, ref _testsPassed, ref _testsFailed);
            }

            // Test Chest item
            var chestData = new ArmorData
            {
                Name = "Test Chestplate",
                Slot = "chest",
                Tier = 2,
                Armor = 10
            };

            var chestItem = ItemGenerator.GenerateArmorItem(chestData);
            TestBase.AssertNotNull(chestItem,
                "Should generate chest item",
                ref _testsRun, ref _testsPassed, ref _testsFailed);

            // Test Feet item
            var feetData = new ArmorData
            {
                Name = "Test Boots",
                Slot = "feet",
                Tier = 1,
                Armor = 3
            };

            var feetItem = ItemGenerator.GenerateArmorItem(feetData);
            TestBase.AssertNotNull(feetItem,
                "Should generate feet item",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        private static void TestGenerateArmorItem_CopiesTags()
        {
            Console.WriteLine("\n--- Testing GenerateArmorItem copies tags ---");
            var data = new ArmorData
            {
                Name = "Tagged Helm",
                Slot = "head",
                Tier = 1,
                Armor = 2,
                Tags = new List<string> { "heavy", "heavy" }
            };
            var item = ItemGenerator.GenerateArmorItem(data);
            TestBase.AssertNotNull(item, "item", ref _testsRun, ref _testsPassed, ref _testsFailed);
            if (item == null) return;
            TestBase.AssertEqual(1, item.Tags.Count, "deduped", ref _testsRun, ref _testsPassed, ref _testsFailed);
            TestBase.AssertTrue(item.Tags.Contains("heavy"), "heavy tag", ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        #endregion

        #region Tier Selection Tests

        private static void TestSelectRandomItemByTier()
        {
            Console.WriteLine("\n--- Testing SelectRandomItemByTier ---");

            var items = new List<WeaponData>
            {
                new WeaponData { Name = "Tier1-1", Tier = 1 },
                new WeaponData { Name = "Tier1-2", Tier = 1 },
                new WeaponData { Name = "Tier2-1", Tier = 2 },
                new WeaponData { Name = "Tier2-2", Tier = 2 }
            };

            // Test selecting tier 1
            var tier1Item = ItemGenerator.SelectRandomItemByTier(items, 1);
            TestBase.AssertNotNull(tier1Item,
                "Should select item from tier 1",
                ref _testsRun, ref _testsPassed, ref _testsFailed);

            if (tier1Item != null)
            {
                TestBase.AssertEqual(1, tier1Item.Tier,
                    "Selected item should be tier 1",
                    ref _testsRun, ref _testsPassed, ref _testsFailed);
            }

            // Test selecting tier 2
            var tier2Item = ItemGenerator.SelectRandomItemByTier(items, 2);
            TestBase.AssertNotNull(tier2Item,
                "Should select item from tier 2",
                ref _testsRun, ref _testsPassed, ref _testsFailed);

            // Test selecting non-existent tier
            var tier3Item = ItemGenerator.SelectRandomItemByTier(items, 3);
            TestBase.AssertNull(tier3Item,
                "Should return null for non-existent tier",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        #endregion

        #region Name Generation Tests

        private static void TestGenerateItemNameWithBonuses()
        {
            Console.WriteLine("\n--- Testing GenerateItemNameWithBonuses ---");

            // Create a test item with bonuses
            var weapon = TestDataBuilders.Weapon()
                .WithName("Test Sword")
                .WithTier(1)
                .Build();

            // Add some stat bonuses
            weapon.StatBonuses.Add(new StatBonus { StatType = "Strength", Value = 5 });
            weapon.StatBonuses.Add(new StatBonus { StatType = "Agility", Value = 3 });

            var name = ItemGenerator.GenerateItemNameWithBonuses(weapon);

            TestBase.AssertTrue(!string.IsNullOrEmpty(name),
                "Generated name should not be empty",
                ref _testsRun, ref _testsPassed, ref _testsFailed);

            TestBase.AssertTrue(name.Contains("Test Sword"),
                "Generated name should contain base name",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        /// <summary>
        /// LootBonusApplier calls <see cref="ItemGenerator.GenerateItemNameWithBonuses"/> twice; the lab used to call it again.
        /// Re-entry must not stack prefixes/suffixes already present in <see cref="Item.Name"/>.
        /// </summary>
        private static void TestGenerateItemNameWithBonuses_IdempotentWhenItemNameAlreadyComposed()
        {
            Console.WriteLine("\n--- Testing GenerateItemNameWithBonuses idempotency ---");

            var weapon = TestDataBuilders.Weapon()
                .WithName("OBLITERATOR")
                .WithTier(4)
                .Build();

            weapon.Modifications.Add(new Modification
            {
                Name = "Chipped",
                PrefixCategory = "Quality"
            });
            weapon.Modifications.Add(new Modification
            {
                Name = "Sturdy",
                PrefixCategory = "Adjective"
            });
            weapon.Modifications.Add(new Modification
            {
                Name = "of Flame",
                PrefixCategory = "Adjective"
            });
            weapon.StatBonuses.Add(new StatBonus { Name = "of Swiftness", StatType = "Agility", Value = 3 });

            string first = ItemGenerator.GenerateItemNameWithBonuses(weapon);
            weapon.Name = first;
            string second = ItemGenerator.GenerateItemNameWithBonuses(weapon);
            weapon.Name = second;
            string third = ItemGenerator.GenerateItemNameWithBonuses(weapon);

            TestBase.AssertEqual(first, second,
                "Second pass should match first (no duplicated prefix/suffix tokens)",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
            TestBase.AssertEqual(second, third,
                "Third pass should still be stable",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        /// <summary>
        /// Two stat suffixes with the same string length (e.g. "of the Titan" and "of Alacrity") must strip
        /// from the composed name in multiple passes; otherwise the first stays on the base and re-assembly duplicates it.
        /// </summary>
        private static void TestGenerateItemNameWithBonuses_IdempotentWithEqualLengthStatSuffixes()
        {
            Console.WriteLine("\n--- Testing GenerateItemNameWithBonuses idempotency (equal-length stat suffixes) ---");

            var weapon = TestDataBuilders.Weapon()
                .WithName("VOID TOME")
                .WithTier(5)
                .Build();

            weapon.StatBonuses.Add(new StatBonus { Name = "of the Titan", StatType = "Strength", Value = 5 });
            weapon.StatBonuses.Add(new StatBonus { Name = "of Alacrity", StatType = "Agility", Value = 3 });

            string first = ItemGenerator.GenerateItemNameWithBonuses(weapon);
            weapon.Name = first;
            string second = ItemGenerator.GenerateItemNameWithBonuses(weapon);

            TestBase.AssertEqual(first, second,
                "Re-entry must not duplicate the first suffix when suffix names share the same length",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
            TestBase.AssertEqual(1, CountOccurrencesOrdinalIgnoreCase(first, "of the Titan"),
                "composed name should contain exactly one 'of the Titan'",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        private static int CountOccurrencesOrdinalIgnoreCase(string haystack, string needle)
        {
            if (string.IsNullOrEmpty(haystack) || string.IsNullOrEmpty(needle))
                return 0;
            int count = 0;
            int idx = 0;
            while (idx <= haystack.Length - needle.Length)
            {
                idx = haystack.IndexOf(needle, idx, StringComparison.OrdinalIgnoreCase);
                if (idx < 0)
                    break;
                count++;
                idx += needle.Length;
            }

            return count;
        }

        #endregion
    }
}
