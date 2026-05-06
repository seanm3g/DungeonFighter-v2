using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using RPGGame;
using RPGGame.Data;
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
            TestArmorData_DeserializesNullStatCellsAsZero();
            TestArmorData_JsonNormalizerCoercesNullStatsBeforeDeserialize();
            TestGenerateArmorItem_CopiesTags();
            TestGenerateArmorItem_CopiesExtendedCatalogStats();
            TestGenerateArmorItem_ExtraActionSlotsCatalogRolls();
            TestGenerateWeaponItem_CatalogExtraActionSlots();
            TestEquipmentStatBonusIncludesBaseCatalogStrength();
            TestSelectRandomItemByTier();
            TestGenerateItemNameWithBonuses();
            TestGenerateItemNameWithBonuses_IdempotentWhenItemNameAlreadyComposed();
            TestGenerateItemNameWithBonuses_IdempotentWithEqualLengthStatSuffixes();
            TestGenerateItemNameWithBonuses_StatSuffixRarityDisplayOrder();
            TestStatBonusEnumerateContributionsWhenMechanicsSet();

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

                TestBase.AssertEqual(0, weapon.ExtraActionSlots,
                    "Weapon without extra slot catalog fields should have zero combo slots",
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

        /// <summary>Armor.json from sheets often uses <c>null</c> for blank stat columns (e.g. AGILITY).</summary>
        private static void TestArmorData_DeserializesNullStatCellsAsZero()
        {
            Console.WriteLine("\n--- Testing ArmorData JSON null stat cells ---");
            const string json = """
                [{
                    "slot": "chest",
                    "name": "Shirt",
                    "armor": 1,
                    "STRENGTH": 1,
                    "AGILITY": null,
                    "TECHNIQUE": null,
                    "tier": 1
                }]
                """;
            var opts = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
            var list = JsonSerializer.Deserialize<List<ArmorData>>(json, opts);
            TestBase.AssertNotNull(list, "list", ref _testsRun, ref _testsPassed, ref _testsFailed);
            if (list == null || list.Count == 0) return;
            var row = list[0];
            TestBase.AssertEqual(1, row.Strength, "STR", ref _testsRun, ref _testsPassed, ref _testsFailed);
            TestBase.AssertEqual(0, row.Agility, "AGI null -> 0", ref _testsRun, ref _testsPassed, ref _testsFailed);
            TestBase.AssertEqual(0, row.Technique, "TEC null -> 0", ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        private static void TestArmorData_JsonNormalizerCoercesNullStatsBeforeDeserialize()
        {
            Console.WriteLine("\n--- Testing GameDataJsonNormalizer for Armor.json null stats ---");
            const string json = """
                [{
                    "slot": "chest",
                    "name": "Shirt",
                    "armor": 1,
                    "AGILITY": null,
                    "tier": 1
                }]
                """;
            string norm = GameDataJsonNormalizer.NormalizeForGameDataFile(GameConstants.ArmorJson, json);
            var opts = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
            var list = JsonSerializer.Deserialize<List<ArmorData>>(norm, opts);
            TestBase.AssertNotNull(list, "list", ref _testsRun, ref _testsPassed, ref _testsFailed);
            if (list == null || list.Count == 0) return;
            TestBase.AssertEqual(0, list[0].Agility, "normalized AGILITY", ref _testsRun, ref _testsPassed, ref _testsFailed);
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

        private static void TestGenerateArmorItem_CopiesExtendedCatalogStats()
        {
            Console.WriteLine("\n--- Testing GenerateArmorItem extended catalog stats ---");
            var data = new ArmorData
            {
                Name = "War Boots",
                Slot = "feet",
                Tier = 1,
                Armor = 2,
                Strength = 1,
                Agility = 0,
                Technique = 0,
                Intelligence = 0,
                Hit = 0,
                Combo = 0,
                Crit = 0,
                ExtraActionSlots = 2,
                MinActionBonuses = 0
            };
            var item = ItemGenerator.GenerateArmorItem(data) as FeetItem;
            TestBase.AssertNotNull(item, "feet", ref _testsRun, ref _testsPassed, ref _testsFailed);
            if (item == null) return;
            TestBase.AssertEqual(1, item.BaseStrength, "base str", ref _testsRun, ref _testsPassed, ref _testsFailed);
            TestBase.AssertEqual(2, item.ExtraActionSlots, "extra slots", ref _testsRun, ref _testsPassed, ref _testsFailed);

            var helm = new ArmorData
            {
                Name = "Circlet",
                Slot = "head",
                Tier = 1,
                Armor = 1,
                MinActionBonuses = 2
            };
            var head = ItemGenerator.GenerateArmorItem(helm) as HeadItem;
            TestBase.AssertNotNull(head, "head", ref _testsRun, ref _testsPassed, ref _testsFailed);
            if (head != null)
                TestBase.AssertEqual(2, head.MinGeneratedActionBonuses, "min action bonuses", ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        private static void TestGenerateArmorItem_ExtraActionSlotsCatalogRolls()
        {
            Console.WriteLine("\n--- Testing GenerateArmorItem extraActionSlots min/max roll ---");
            var fixedRange = new ArmorData
            {
                Name = "Treads",
                Slot = "feet",
                Tier = 1,
                Armor = 1,
                ExtraActionSlotsMin = 2,
                ExtraActionSlotsMax = 2
            };
            for (int i = 0; i < 5; i++)
            {
                var feet = ItemGenerator.GenerateArmorItem(fixedRange) as FeetItem;
                TestBase.AssertNotNull(feet, "feet", ref _testsRun, ref _testsPassed, ref _testsFailed);
                if (feet != null)
                    TestBase.AssertEqual(2, feet.ExtraActionSlots, "min=max=2", ref _testsRun, ref _testsPassed, ref _testsFailed);
            }

            var varied = new ArmorData
            {
                Name = "Greaves",
                Slot = "feet",
                Tier = 1,
                Armor = 1,
                ExtraActionSlotsMin = 0,
                ExtraActionSlotsMax = 2
            };
            var seen = new HashSet<int>();
            for (int i = 0; i < 48; i++)
            {
                var f = ItemGenerator.GenerateArmorItem(varied) as FeetItem;
                TestBase.AssertNotNull(f, "feet2", ref _testsRun, ref _testsPassed, ref _testsFailed);
                if (f != null)
                {
                    TestBase.AssertTrue(f.ExtraActionSlots >= 0 && f.ExtraActionSlots <= 2, "roll in 0..2",
                        ref _testsRun, ref _testsPassed, ref _testsFailed);
                    seen.Add(f.ExtraActionSlots);
                }
            }

            TestBase.AssertTrue(seen.Count >= 2, "expected spread over 0..2 range",
                ref _testsRun, ref _testsPassed, ref _testsFailed);

            var legacyOnly = new ArmorData
            {
                Name = "Sandals",
                Slot = "feet",
                Tier = 1,
                Armor = 1,
                ExtraActionSlots = 4,
                ExtraActionSlotsMin = 0,
                ExtraActionSlotsMax = 0
            };
            var sand = ItemGenerator.GenerateArmorItem(legacyOnly) as FeetItem;
            TestBase.AssertNotNull(sand, "sand", ref _testsRun, ref _testsPassed, ref _testsFailed);
            if (sand != null)
                TestBase.AssertEqual(4, sand.ExtraActionSlots, "legacy fixed when min/max unused",
                    ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        private static void TestGenerateWeaponItem_CatalogExtraActionSlots()
        {
            Console.WriteLine("\n--- Testing GenerateWeaponItem catalog extraActionSlots ---");
            var w = ItemGenerator.GenerateWeaponItem(new WeaponData
            {
                Name = "Combo Staff",
                Type = "Wand",
                Tier = 1,
                BaseDamage = 5,
                AttackSpeed = 0.05,
                ExtraActionSlotsMin = 1,
                ExtraActionSlotsMax = 1
            });
            TestBase.AssertEqual(1, w.ExtraActionSlots, "weapon fixed roll 1", ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        private static void TestEquipmentStatBonusIncludesBaseCatalogStrength()
        {
            Console.WriteLine("\n--- Testing equipment STR includes catalog base ---");
            var c = TestDataBuilders.Character().WithName("StrGear").Build();
            var chest = ItemGenerator.GenerateArmorItem(new ArmorData
            {
                Name = "Plate",
                Slot = "chest",
                Tier = 1,
                Armor = 5,
                Strength = 3
            });
            c.EquipItem(chest, "body");
            TestBase.AssertEqual(3, c.Equipment.GetEquipmentStatBonus("STR"),
                "STR from chest BaseStrength", ref _testsRun, ref _testsPassed, ref _testsFailed);
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

        private static void TestGenerateItemNameWithBonuses_StatSuffixRarityDisplayOrder()
        {
            Console.WriteLine("\n--- Testing GenerateItemNameWithBonuses stat suffix rarity order ---");
            var weapon = TestDataBuilders.Weapon()
                .WithName("Sword")
                .WithTier(1)
                .Build();

            weapon.StatBonuses.Add(new StatBonus { Name = "of RareOne", Rarity = "Rare", StatType = "Damage", Value = 1 });
            weapon.StatBonuses.Add(new StatBonus { Name = "of CommonOne", Rarity = "Common", StatType = "Armor", Value = 1 });
            weapon.StatBonuses.Add(new StatBonus { Name = "of UncommonOne", Rarity = "Uncommon", StatType = "Health", Value = 1 });

            string name = ItemGenerator.GenerateItemNameWithBonuses(weapon);
            int iU = name.IndexOf("of UncommonOne", StringComparison.Ordinal);
            int iC = name.IndexOf("of CommonOne", StringComparison.Ordinal);
            int iR = name.IndexOf("of RareOne", StringComparison.Ordinal);
            TestBase.AssertTrue(iU >= 0 && iC >= 0 && iR >= 0, "all suffix tokens present", ref _testsRun, ref _testsPassed, ref _testsFailed);
            TestBase.AssertTrue(iU < iC && iC < iR, "Uncommon then Common then Rare", ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        private static void TestStatBonusEnumerateContributionsWhenMechanicsSet()
        {
            Console.WriteLine("\n--- Testing StatBonus EnumerateContributions (Mechanics) ---");
            var sb = new StatBonus
            {
                StatType = "Legacy",
                Value = 99,
                Mechanics = new List<StatBonusMechanic>
                {
                    new() { StatType = "Armor", Value = 5 },
                    new() { StatType = "Health", Value = 15 }
                }
            };

            var list = sb.EnumerateContributions().ToList();
            TestBase.AssertEqual(2, list.Count, "mechanics only", ref _testsRun, ref _testsPassed, ref _testsFailed);
            TestBase.AssertEqual("Armor", list[0].StatType, "first stat type", ref _testsRun, ref _testsPassed, ref _testsFailed);
            TestBase.AssertEqual(5, (int)list[0].Value, "first value", ref _testsRun, ref _testsPassed, ref _testsFailed);
            TestBase.AssertEqual(5, sb.SumContributionValuesForStatType("Armor"), "armor sum", ref _testsRun, ref _testsPassed, ref _testsFailed);
            TestBase.AssertEqual(15, sb.SumContributionValuesForStatType("Health"), "health sum", ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        #endregion
    }
}
