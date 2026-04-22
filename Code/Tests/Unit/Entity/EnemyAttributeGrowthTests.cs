using System;
using System.Collections.Generic;
using System.Text.Json;
using RPGGame;
using RPGGame.Tests;

namespace RPGGame.Tests.Unit.Entity
{
    public static class EnemyAttributeGrowthTests
    {
        private static int _testsRun;
        private static int _testsPassed;
        private static int _testsFailed;

        public static void RunAllTests()
        {
            Console.WriteLine("=== Enemy Attribute Growth Tests ===\n");

            _testsRun = 0;
            _testsPassed = 0;
            _testsFailed = 0;

            TestBaseAndZeroGrowthIsStable();
            TestPartialGrowthSharesSixPointBudget();
            TestJsonEnemyUsesAttributesNotDirectStats();
            TestPartialBaseAttributesFallsBackForOmittedStats();
            TestLegacyRootStatsFoldIntoGrowthOnly();
            TestExplicitGrowthPerLevelFractional();
            TestExplicitHealthGrowthPerLevel();

            TestBase.PrintSummary("Enemy Attribute Growth Tests", _testsRun, _testsPassed, _testsFailed);
        }

        private static void TestBaseAndZeroGrowthIsStable()
        {
            Console.WriteLine("--- Explicit zero growth cells normalize to 1.5/level each (6 total) ---");

            var config = GameConfiguration.Instance;
            double oldScaling = config.EnemySystem.ScalingPerLevel.Attributes;
            try
            {
                config.EnemySystem.ScalingPerLevel.Attributes = 10.0;
                config.EnemySystem.GlobalMultipliers.DamageMultiplier = 1.0;
                config.EnemySystem.GlobalMultipliers.SpeedMultiplier = 1.0;

                var data = new EnemyData
                {
                    Name = "BaseOnly",
                    Archetype = "Berserker",
                    BaseAttributes = new EnemyAttributeSet
                    {
                        Strength = 5.9,
                        Agility = 3.1,
                        Technique = 2.0,
                        Intelligence = 1.0
                    },
                    GrowthPerLevel = new EnemyAttributeSet
                    {
                        Strength = 0,
                        Agility = 0,
                        Technique = 0,
                        Intelligence = 0
                    }
                };

                var e1 = EnemyDataFactory.CreateEnemyFromData(data, level: 1);
                var e10 = EnemyDataFactory.CreateEnemyFromData(data, level: 10);

                TestBase.AssertNotNull(e1, "Enemy at level 1 should be created", ref _testsRun, ref _testsPassed, ref _testsFailed);
                TestBase.AssertNotNull(e10, "Enemy at level 10 should be created", ref _testsRun, ref _testsPassed, ref _testsFailed);
                if (e1 == null || e10 == null) return;

                TestBase.AssertEqual(5, e1.Strength, "Level 1 floors base STR", ref _testsRun, ref _testsPassed, ref _testsFailed);
                TestBase.AssertEqual(3, e1.Agility, "Level 1 floors base AGI", ref _testsRun, ref _testsPassed, ref _testsFailed);
                TestBase.AssertEqual(2, e1.Technique, "Level 1 keeps base TEC", ref _testsRun, ref _testsPassed, ref _testsFailed);
                TestBase.AssertEqual(1, e1.Intelligence, "Level 1 keeps base INT", ref _testsRun, ref _testsPassed, ref _testsFailed);

                TestBase.AssertEqual(19, e10.Strength, "Level 10 STR gains 9 * 1.5 normalized growth", ref _testsRun, ref _testsPassed, ref _testsFailed);
                TestBase.AssertEqual(16, e10.Agility, "Level 10 AGI gains 9 * 1.5", ref _testsRun, ref _testsPassed, ref _testsFailed);
                TestBase.AssertEqual(15, e10.Technique, "Level 10 TEC gains 9 * 1.5", ref _testsRun, ref _testsPassed, ref _testsFailed);
                TestBase.AssertEqual(14, e10.Intelligence, "Level 10 INT gains 9 * 1.5", ref _testsRun, ref _testsPassed, ref _testsFailed);
            }
            finally
            {
                config.EnemySystem.ScalingPerLevel.Attributes = oldScaling;
            }
        }

        /// <summary>Only STR growth specified; remaining budget is split across AGI/TEC/INT so total per level is 6.</summary>
        private static void TestPartialGrowthSharesSixPointBudget()
        {
            Console.WriteLine("\n--- Partial growth shares 6-point budget ---");

            var config = GameConfiguration.Instance;
            double oldScaling = config.EnemySystem.ScalingPerLevel.Attributes;
            try
            {
                config.EnemySystem.ScalingPerLevel.Attributes = 0.2;
                config.EnemySystem.GlobalMultipliers.DamageMultiplier = 1.0;
                config.EnemySystem.GlobalMultipliers.SpeedMultiplier = 1.0;

                var data = new EnemyData
                {
                    Name = "Weighted",
                    Archetype = "Berserker",
                    BaseAttributes = new EnemyAttributeSet { Strength = 2, Agility = 2, Technique = 2, Intelligence = 2 },
                    GrowthPerLevel = new EnemyAttributeSet { Strength = 0.2 }
                };

                // lv = 5; growth normalized: STR 0.2 + (6-0.2)/3 each for AGI/TEC/INT
                var e6 = EnemyDataFactory.CreateEnemyFromData(data, level: 6);
                TestBase.AssertNotNull(e6, "Enemy at level 6 should be created", ref _testsRun, ref _testsPassed, ref _testsFailed);
                if (e6 == null) return;

                TestBase.AssertEqual(3, e6.Strength, "STR floor(2 + 5*0.2)", ref _testsRun, ref _testsPassed, ref _testsFailed);
                TestBase.AssertEqual(11, e6.Agility, "AGI gains filled growth slots", ref _testsRun, ref _testsPassed, ref _testsFailed);
            }
            finally
            {
                config.EnemySystem.ScalingPerLevel.Attributes = oldScaling;
            }
        }

        private static void TestJsonEnemyUsesAttributesNotDirectStats()
        {
            Console.WriteLine("\n--- Attributes-only combat path ---");

            var config = GameConfiguration.Instance;
            double oldScaling = config.EnemySystem.ScalingPerLevel.Attributes;
            try
            {
                config.EnemySystem.ScalingPerLevel.Attributes = 0.2;

                var data = new EnemyData
                {
                    Name = "AttrOnly",
                    Archetype = "Assassin",
                    BaseAttributes = new EnemyAttributeSet { Strength = 2, Agility = 5, Technique = 3, Intelligence = 2 },
                    GrowthPerLevel = new EnemyAttributeSet { Strength = 1.5, Agility = 1.5, Technique = 1.5, Intelligence = 1.5 }
                };

                var e = EnemyDataFactory.CreateEnemyFromData(data, level: 10);
                TestBase.AssertNotNull(e, "Enemy should be created", ref _testsRun, ref _testsPassed, ref _testsFailed);
                if (e == null) return;

                TestBase.AssertFalse(e.UsesDirectCombatStats(), "JSON enemy should not use direct combat stats", ref _testsRun, ref _testsPassed, ref _testsFailed);
                TestBase.AssertEqual(0, e.Damage, "JSON enemy direct Damage should remain 0", ref _testsRun, ref _testsPassed, ref _testsFailed);
                TestBase.AssertTrue(e.Strength > 0 || e.Agility > 0 || e.Technique > 0 || e.Intelligence > 0,
                    "JSON enemy should have attribute stats set", ref _testsRun, ref _testsPassed, ref _testsFailed);
            }
            finally
            {
                config.EnemySystem.ScalingPerLevel.Attributes = oldScaling;
            }
        }

        /// <summary>
        /// Mirrors Bat-style data from Enemies.json: fractional growth, floor after (level-1) * gain.
        /// </summary>
        /// <summary>
        /// Matches common <c>Enemies.json</c> rows: only <c>strength</c> under <c>baseAttributes</c> / <c>growthPerLevel</c>.
        /// Omitted stats must use tuning baseline + per-level scaling, not stay at 0.
        /// </summary>
        private static void TestPartialBaseAttributesFallsBackForOmittedStats()
        {
            Console.WriteLine("\n--- Partial baseAttributes / growthPerLevel (Wraith-style) ---");

            var wraithLike = new EnemyData
            {
                Name = "PartialStatEnemy",
                Archetype = "Mage",
                BaseHealth = 33,
                HealthGrowthPerLevel = 2.05,
                BaseAttributes = new EnemyAttributeSet { Strength = 2 },
                GrowthPerLevel = new EnemyAttributeSet { Strength = 0.08 },
                IsLiving = false,
                Actions = new List<string>()
            };

            var e = EnemyDataFactory.CreateEnemyFromData(wraithLike, level: 28);
            TestBase.AssertNotNull(e, "Partial-template enemy should be created", ref _testsRun, ref _testsPassed, ref _testsFailed);
            if (e == null) return;

            TestBase.AssertEqual(4, e.Strength, "STR from explicit base + growth (floor)", ref _testsRun, ref _testsPassed, ref _testsFailed);
            TestBase.AssertTrue(e.Agility > 0, "Omitted base AGI must fall back to baseline scaling, not 0", ref _testsRun, ref _testsPassed, ref _testsFailed);
            TestBase.AssertTrue(e.Technique > 0, "Omitted base TECH must fall back to baseline scaling, not 0", ref _testsRun, ref _testsPassed, ref _testsFailed);
            TestBase.AssertTrue(e.Intelligence > 0, "Omitted base INT must fall back to baseline scaling, not 0", ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        private static void TestLegacyRootStatsFoldIntoGrowthOnly()
        {
            Console.WriteLine("\n--- Legacy root strength/agility (ExtensionData) fold into growth ---");

            const string json = """
            {"name":"T","archetype":"Mage","strength":0.7,"agility":0.2,"baseAttributes":{"strength":2},"growthPerLevel":{},"baseHealth":100,"actions":[],"isLiving":false,"description":""}
            """;
            var opts = new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };
            var e = JsonSerializer.Deserialize<EnemyData>(json, opts);
            TestBase.AssertNotNull(e, "Deserialize enemy", ref _testsRun, ref _testsPassed, ref _testsFailed);
            if (e == null) return;

            EnemyDataPostLoad.Apply(e);
            TestBase.AssertEqual(0.7, e.GrowthPerLevel!.Strength!.Value, "root strength -> growthPerLevel.strength", ref _testsRun, ref _testsPassed, ref _testsFailed);
            TestBase.AssertEqual(0.2, e.GrowthPerLevel.Agility!.Value, "root agility -> growthPerLevel.agility", ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        private static void TestExplicitGrowthPerLevelFractional()
        {
            Console.WriteLine("\n--- Explicit growthPerLevel (fractional) ---");

            var enemySystem = new EnemySystemConfig
            {
                GlobalMultipliers = new GlobalMultipliersConfig
                {
                    HealthMultiplier = 1.0,
                    DamageMultiplier = 1.0,
                    ArmorMultiplier = 1.0,
                    SpeedMultiplier = 1.0
                },
                BaselineStats = new BaselineStatsConfig
                {
                    Health = 50,
                    Strength = 3,
                    Agility = 3,
                    Technique = 3,
                    Intelligence = 3,
                    Armor = 2
                },
                ScalingPerLevel = new ScalingPerLevelConfig
                {
                    Health = 0,
                    Attributes = 99,
                    Armor = 0
                },
                Archetypes = new Dictionary<string, ArchetypeMultipliersConfig>()
            };

            var data = new EnemyData
            {
                Name = "BatLike",
                Archetype = "Assassin",
                BaseAttributes = new EnemyAttributeSet
                {
                    Strength = 2,
                    Agility = 5,
                    Technique = 3,
                    Intelligence = 2
                },
                GrowthPerLevel = new EnemyAttributeSet
                {
                    Strength = 0.08,
                    Agility = 0.35,
                    Technique = 0.18,
                    Intelligence = 0.1
                }
            };

            var stats = EnemyStatCalculator.CalculateStats(data, 10, enemySystem);

            // lv = 9; explicit growth sums 0.71 -> scaled to 6/0.71 per level total
            TestBase.AssertEqual(8, stats.Strength, "Calculator STR at L10", ref _testsRun, ref _testsPassed, ref _testsFailed);
            TestBase.AssertEqual(31, stats.Agility, "Calculator AGI at L10", ref _testsRun, ref _testsPassed, ref _testsFailed);
            TestBase.AssertEqual(16, stats.Technique, "Calculator TEC at L10", ref _testsRun, ref _testsPassed, ref _testsFailed);
            TestBase.AssertEqual(9, stats.Intelligence, "Calculator INT at L10", ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        private static void TestExplicitHealthGrowthPerLevel()
        {
            Console.WriteLine("\n--- Explicit baseHealth + healthGrowthPerLevel ---");

            var enemySystem = new EnemySystemConfig
            {
                GlobalMultipliers = new GlobalMultipliersConfig
                {
                    HealthMultiplier = 1.0,
                    DamageMultiplier = 1.0,
                    ArmorMultiplier = 1.0,
                    SpeedMultiplier = 1.0
                },
                BaselineStats = new BaselineStatsConfig
                {
                    Health = 50,
                    Strength = 3,
                    Agility = 3,
                    Technique = 3,
                    Intelligence = 3,
                    Armor = 2
                },
                ScalingPerLevel = new ScalingPerLevelConfig
                {
                    Health = 99,
                    Attributes = 0,
                    Armor = 0
                },
                Archetypes = new Dictionary<string, ArchetypeMultipliersConfig>()
            };

            var data = new EnemyData
            {
                Name = "HpTest",
                Archetype = "Berserker",
                BaseHealth = 100,
                HealthGrowthPerLevel = 2.7,
                BaseAttributes = new EnemyAttributeSet { Strength = 2, Agility = 2, Technique = 2, Intelligence = 2 },
                GrowthPerLevel = new EnemyAttributeSet { Strength = 0, Agility = 0, Technique = 0, Intelligence = 0 }
            };

            var stats = EnemyStatCalculator.CalculateStats(data, 10, enemySystem);
            // lv=9; floor(100 + 9*2.7)=floor(124.3)=124 — tuning scaling.Health must not apply when growth is explicit
            TestBase.AssertEqual(124, stats.Health, "Calculator HP at L10 with explicit growth", ref _testsRun, ref _testsPassed, ref _testsFailed);
        }
    }
}

