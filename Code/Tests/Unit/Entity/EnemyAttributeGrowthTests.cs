using System;
using System.Collections.Generic;
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
            TestWeightedGrowthUsesOverridesAndFloors();
            TestJsonEnemyUsesAttributesNotDirectStats();
            TestExplicitGrowthPerLevelFractional();
            TestExplicitHealthGrowthPerLevel();

            TestBase.PrintSummary("Enemy Attribute Growth Tests", _testsRun, _testsPassed, _testsFailed);
        }

        private static void TestBaseAndZeroGrowthIsStable()
        {
            Console.WriteLine("--- Base-only (zero growth) ---");

            var config = GameConfiguration.Instance;
            double oldScaling = config.EnemySystem.ScalingPerLevel.Attributes;
            try
            {
                // Make sure any fallback growth doesn't interfere.
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
                    },
                    Overrides = new StatOverridesConfig
                    {
                        Strength = 2.0,
                        Agility = 2.0,
                        Technique = 2.0,
                        Intelligence = 2.0
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

                TestBase.AssertEqual(e1.Strength, e10.Strength, "Zero growth keeps STR stable across levels", ref _testsRun, ref _testsPassed, ref _testsFailed);
                TestBase.AssertEqual(e1.Agility, e10.Agility, "Zero growth keeps AGI stable across levels", ref _testsRun, ref _testsPassed, ref _testsFailed);
                TestBase.AssertEqual(e1.Technique, e10.Technique, "Zero growth keeps TEC stable across levels", ref _testsRun, ref _testsPassed, ref _testsFailed);
                TestBase.AssertEqual(e1.Intelligence, e10.Intelligence, "Zero growth keeps INT stable across levels", ref _testsRun, ref _testsPassed, ref _testsFailed);
            }
            finally
            {
                config.EnemySystem.ScalingPerLevel.Attributes = oldScaling;
            }
        }

        private static void TestWeightedGrowthUsesOverridesAndFloors()
        {
            Console.WriteLine("\n--- Weighted growth from overrides ---");

            var config = GameConfiguration.Instance;
            double oldScaling = config.EnemySystem.ScalingPerLevel.Attributes;
            try
            {
                config.EnemySystem.ScalingPerLevel.Attributes = 0.2; // global growth magnitude for this test
                config.EnemySystem.GlobalMultipliers.DamageMultiplier = 1.0;
                config.EnemySystem.GlobalMultipliers.SpeedMultiplier = 1.0;

                var data = new EnemyData
                {
                    Name = "Weighted",
                    Archetype = "Berserker",
                    BaseAttributes = new EnemyAttributeSet { Strength = 2, Agility = 2, Technique = 2, Intelligence = 2 },
                    Overrides = new StatOverridesConfig
                    {
                        Strength = 1.0,
                        Agility = 2.0,
                        Technique = 0.5,
                        Intelligence = 1.5
                    }
                };

                // Level 6 => lv = 5
                // STR: floor(2 + 5*(0.2*1.0)) = floor(3.0) = 3
                // AGI: floor(2 + 5*(0.2*2.0)) = floor(4.0) = 4
                // TEC: floor(2 + 5*(0.2*0.5)) = floor(2.5) = 2
                // INT: floor(2 + 5*(0.2*1.5)) = floor(3.5) = 3
                var e6 = EnemyDataFactory.CreateEnemyFromData(data, level: 6);
                TestBase.AssertNotNull(e6, "Enemy at level 6 should be created", ref _testsRun, ref _testsPassed, ref _testsFailed);
                if (e6 == null) return;

                TestBase.AssertEqual(3, e6.Strength, "Weighted growth STR uses override weight and floor", ref _testsRun, ref _testsPassed, ref _testsFailed);
                TestBase.AssertEqual(4, e6.Agility, "Weighted growth AGI uses override weight and floor", ref _testsRun, ref _testsPassed, ref _testsFailed);
                TestBase.AssertEqual(2, e6.Technique, "Weighted growth TEC uses override weight and floor", ref _testsRun, ref _testsPassed, ref _testsFailed);
                TestBase.AssertEqual(3, e6.Intelligence, "Weighted growth INT uses override weight and floor", ref _testsRun, ref _testsPassed, ref _testsFailed);
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
                    Overrides = new StatOverridesConfig { Strength = 1.0, Agility = 1.7, Technique = 1.3, Intelligence = 0.9 }
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
                Overrides = new StatOverridesConfig
                {
                    Health = 0.4,
                    Agility = 1.7,
                    Technique = 1.3,
                    Armor = 0.2,
                    Intelligence = 0.9,
                    Strength = 0.8
                },
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

            // lv = 9; floor(2 + 9*0.08)=2, floor(5+9*0.35)=8, floor(3+9*0.18)=4, floor(2+9*0.1)=2
            TestBase.AssertEqual(2, stats.Strength, "Calculator STR at L10", ref _testsRun, ref _testsPassed, ref _testsFailed);
            TestBase.AssertEqual(8, stats.Agility, "Calculator AGI at L10", ref _testsRun, ref _testsPassed, ref _testsFailed);
            TestBase.AssertEqual(4, stats.Technique, "Calculator TEC at L10", ref _testsRun, ref _testsPassed, ref _testsFailed);
            TestBase.AssertEqual(2, stats.Intelligence, "Calculator INT at L10", ref _testsRun, ref _testsPassed, ref _testsFailed);
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

