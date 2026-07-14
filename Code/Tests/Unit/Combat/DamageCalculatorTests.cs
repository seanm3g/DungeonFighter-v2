using System;
using RPGGame.Combat.Calculators;
using RPGGame.Tests;

namespace RPGGame.Tests.Unit.Combat
{
    /// <summary>
    /// Comprehensive tests for DamageCalculator
    /// Tests damage calculations, caching, and edge cases
    /// </summary>
    public static class DamageCalculatorTests
    {
        private static int _testsRun = 0;
        private static int _testsPassed = 0;
        private static int _testsFailed = 0;

        /// <summary>
        /// Runs all DamageCalculator tests
        /// </summary>
        public static void RunAllTests()
        {
            Console.WriteLine("=== DamageCalculator Tests ===\n");
            
            _testsRun = 0;
            _testsPassed = 0;
            _testsFailed = 0;

            // Clear cache before tests
            DamageCalculator.ClearAllCaches();

            TestCalculateRawDamage();
            TestDirectStatEnemyRawDamageUsesDamageField();
            TestAttributeDamageIncludesStrengthAndPrimary();
            TestCalculateDamage();
            TestDamageReflectsStatChangesWithoutStaleCache();
            TestCacheInvalidation();
            TestCacheClearing();
            TestCacheStats();
            TestEdgeCases();
            TestDamageWithArmor();
            TestResolveTargetArmor_SubtractsAcidArmorReduction();
            TestPierceIgnoresArmor();
            TestComboBandRollDoesNotAmplifyRawDamage();
            TestDamageWithMultipliers();

            TestBase.PrintSummary("DamageCalculator Tests", _testsRun, _testsPassed, _testsFailed);
        }

        #region Raw Damage Tests

        private static void TestCalculateRawDamage()
        {
            Console.WriteLine("--- Testing CalculateRawDamage ---");

            var attacker = TestDataBuilders.Character()
                .WithName("Attacker")
                .WithStats(10, 10, 10, 10)
                .Build();

            var action = TestDataBuilders.CreateMockAction("JAB");
            action.DamageMultiplier = 1.0;

            // Equip a weapon (required for damage in real game)
            var weapon = new WeaponItem("TestSword", 1, 10);
            attacker.EquipItem(weapon, "weapon");
            
            // Test basic raw damage calculation
            var damage = DamageCalculator.CalculateRawDamage(attacker, action, 1.0, 1.0, 10);
            
            // CRITICAL: Raw damage should ALWAYS be positive, not just non-negative
            TestBase.AssertTrue(damage > 0,
                $"Raw damage should be positive, got: {damage}. This indicates a critical bug!",
                ref _testsRun, ref _testsPassed, ref _testsFailed);

            // Test with combo amplifier
            var comboDamage = DamageCalculator.CalculateRawDamage(attacker, action, 2.0, 1.0, 10);
            TestBase.AssertTrue(comboDamage >= damage,
                "Combo amplifier should increase damage",
                ref _testsRun, ref _testsPassed, ref _testsFailed);

            // Test with damage multiplier
            var multiplierDamage = DamageCalculator.CalculateRawDamage(attacker, action, 1.0, 2.0, 10);
            TestBase.AssertTrue(multiplierDamage >= damage,
                "Damage multiplier should increase damage",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        /// <summary>Direct-stat enemies must use <see cref="Enemy.GetEffectiveStrength"/> (Damage field), not STR from the Character branch.</summary>
        private static void TestDirectStatEnemyRawDamageUsesDamageField()
        {
            Console.WriteLine("\n--- Testing direct-stat enemy raw damage ---");

            var labDummy = new Enemy(
                name: "LabDummy",
                level: 1,
                maxHealth: 100,
                damage: 40,
                armor: 0,
                attackSpeed: 1.0,
                primaryAttribute: PrimaryAttribute.Strength,
                isLiving: true,
                archetype: EnemyArchetype.Berserker,
                useDirectStats: true);

            var action = TestDataBuilders.CreateMockAction("JAB");
            action.DamageMultiplier = 1.0;

            int raw = DamageCalculator.CalculateRawDamage(labDummy, action, 1.0, 1.0, roll: 0);
            TestBase.AssertTrue(
                raw >= 40 && raw < 80,
                $"Direct-stat enemy raw damage should use Damage field once (>= 40 and < 80), got {raw}",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        /// <summary>Base damage includes effective STR plus the highest effective attribute (primary).</summary>
        private static void TestAttributeDamageIncludesStrengthAndPrimary()
        {
            Console.WriteLine("\n--- Testing STR + primary attribute damage ---");

            var agilityPrimary = TestDataBuilders.Character()
                .WithName("AgiPrimary")
                .WithStats(5, 20, 5, 5)
                .Build();
            var weapon = new WeaponItem("TestSword", 1, 0);
            agilityPrimary.EquipItem(weapon, "weapon");

            var equalStats = TestDataBuilders.Character()
                .WithName("EqualStats")
                .WithStats(10, 10, 10, 10)
                .Build();
            equalStats.EquipItem(new WeaponItem("TestSword2", 1, 0), "weapon");

            var action = TestDataBuilders.CreateMockAction("JAB");
            action.DamageMultiplier = 1.0;

            int agiPrimaryRaw = DamageCalculator.CalculateRawDamage(agilityPrimary, action, 1.0, 1.0, roll: 0);
            TestBase.AssertTrue(
                agiPrimaryRaw >= 25,
                $"AGI-primary attacker should get STR (5) + primary AGI (20) = 25 base, got {agiPrimaryRaw}",
                ref _testsRun, ref _testsPassed, ref _testsFailed);

            int equalRaw = DamageCalculator.CalculateRawDamage(equalStats, action, 1.0, 1.0, roll: 0);
            TestBase.AssertTrue(
                equalRaw >= 20,
                $"Equal stats tie-break to STR: STR (10) + primary STR (10) = 20 base, got {equalRaw}",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        #endregion

        #region Final Damage Tests

        private static void TestCalculateDamage()
        {
            Console.WriteLine("\n--- Testing CalculateDamage ---");

            var attacker = TestDataBuilders.Character()
                .WithName("Attacker")
                .WithStats(10, 10, 10, 10)
                .Build();

            var target = TestDataBuilders.Enemy()
                .WithName("Target")
                .WithHealth(100)
                .Build();

            // Equip a weapon (required for damage in real game)
            var weapon = new WeaponItem("TestSword", 1, 10);
            attacker.EquipItem(weapon, "weapon");

            var action = TestDataBuilders.CreateMockAction("JAB");
            action.DamageMultiplier = 1.0;

            // Test basic damage calculation
            var damage = DamageCalculator.CalculateDamage(attacker, target, action, 1.0, 1.0, 0, 10);
            
            // CRITICAL: Damage should ALWAYS be positive, not just non-negative
            TestBase.AssertTrue(damage > 0,
                $"Damage should be positive, got: {damage}. This indicates a critical bug!",
                ref _testsRun, ref _testsPassed, ref _testsFailed);

            // Damage should be less than or equal to raw damage (due to armor)
            var rawDamage = DamageCalculator.CalculateRawDamage(attacker, action, 1.0, 1.0, 10);
            TestBase.AssertTrue(damage <= rawDamage,
                "Final damage should be less than or equal to raw damage",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        /// <summary>
        /// Regression: damage must not be served from a cache keyed only by actor identity — STR changes must change output.
        /// </summary>
        private static void TestDamageReflectsStatChangesWithoutStaleCache()
        {
            Console.WriteLine("\n--- Testing damage updates when stats change (no stale cache) ---");

            var attacker = TestDataBuilders.Character()
                .WithName("StatMorph")
                .WithStats(1, 1, 1, 1)
                .Build();
            var weapon = new WeaponItem("BareFist", 1, 0);
            attacker.EquipItem(weapon, "weapon");

            var target = TestDataBuilders.Enemy()
                .WithName("Dummy")
                .WithHealth(100)
                .Build();

            var action = TestDataBuilders.CreateMockAction("JAB");
            action.DamageMultiplier = 1.0;

            int before = DamageCalculator.CalculateDamage(attacker, target, action, 1.0, 1.0, 0, 10);
            attacker.Stats.Strength = 30;
            int after = DamageCalculator.CalculateDamage(attacker, target, action, 1.0, 1.0, 0, 10);

            TestBase.AssertTrue(after > before,
                $"Damage after raising STR should exceed prior ({after} vs {before})",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        private static void TestDamageWithArmor()
        {
            Console.WriteLine("\n--- Testing DamageWithArmor ---");

            var attacker = TestDataBuilders.Character()
                .WithName("Attacker")
                .WithStats(10, 10, 10, 10)
                .Build();

            var lowArmorTarget = TestDataBuilders.Enemy()
                .WithName("LowArmor")
                .WithHealth(100)
                .Build();

            var action = TestDataBuilders.CreateMockAction("JAB");
            action.DamageMultiplier = 1.0;

            var damageLowArmor = DamageCalculator.CalculateDamage(attacker, lowArmorTarget, action, 1.0, 1.0, 0, 10);
            
            TestBase.AssertTrue(damageLowArmor >= 0,
                "Damage against low armor target should be calculated",
                ref _testsRun, ref _testsPassed, ref _testsFailed);

            var heroTarget = TestDataBuilders.Character()
                .WithName("HeroTarget")
                .WithLevel(1)
                .Build();
            heroTarget.EquipItem(new ChestItem("Plate", 1, 5), "body");

            var weapon = new WeaponItem("TestSword", 1, 10);
            attacker.EquipItem(weapon, "weapon");

            int rawVsHero = DamageCalculator.CalculateRawDamage(attacker, action, 1.0, 1.0, 10);
            int dmgVsHero = DamageCalculator.CalculateDamage(attacker, heroTarget, action, 1.0, 1.0, 0, 10);
            int expectedHero = Math.Max(GameConfiguration.Instance.Combat.MinimumDamage, rawVsHero - 5);
            TestBase.AssertEqual(expectedHero, dmgVsHero,
                "Hero armor should flat-reduce incoming attack damage",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
            TestBase.AssertEqual(5, heroTarget.GetMaxArmor(),
                "Hero armor must remain after damage calculation",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        private static void TestResolveTargetArmor_SubtractsAcidArmorReduction()
        {
            Console.WriteLine("\n--- Testing ResolveTargetArmor with AcidArmorReduction ---");

            var enemy = new Enemy("ArmoredConstruct", 1, 100, 5, 5, 5, 5, armor: 10, isLiving: false);
            TestBase.AssertEqual(10, DamageCalculator.ResolveTargetArmor(enemy),
                "enemy base armor", ref _testsRun, ref _testsPassed, ref _testsFailed);
            enemy.AcidArmorReduction = 4;
            TestBase.AssertEqual(6, DamageCalculator.ResolveTargetArmor(enemy),
                "enemy armor after acid shred", ref _testsRun, ref _testsPassed, ref _testsFailed);
            enemy.AcidArmorReduction = 20;
            TestBase.AssertEqual(0, DamageCalculator.ResolveTargetArmor(enemy),
                "enemy armor floors at 0", ref _testsRun, ref _testsPassed, ref _testsFailed);

            var hero = TestDataBuilders.Character().WithName("HeroArmor").WithLevel(1).Build();
            hero.EquipItem(new ChestItem("Plate", 1, 8), "body");
            TestBase.AssertEqual(8, hero.GetMaxArmor(), "hero gear armor", ref _testsRun, ref _testsPassed, ref _testsFailed);
            hero.AcidArmorReduction = 3;
            TestBase.AssertEqual(5, hero.GetMaxArmor(),
                "hero GetMaxArmor subtracts AcidArmorReduction",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
            TestBase.AssertEqual(5, DamageCalculator.ResolveTargetArmor(hero),
                "ResolveTargetArmor matches hero GetMaxArmor with acid",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        private static void TestPierceIgnoresArmor()
        {
            Console.WriteLine("\n--- Testing Pierce ignores armor ---");

            var attacker = TestDataBuilders.Character()
                .WithName("Piercer")
                .WithStats(10, 10, 10, 10)
                .Build();
            attacker.EquipItem(new WeaponItem("TestSword", 1, 20), "weapon");

            var armoredEnemy = new Enemy("Armored", 1, 100, 5, 5, 5, 5, armor: 15, isLiving: true);
            var normalAction = TestDataBuilders.CreateMockAction("JAB");
            normalAction.DamageMultiplier = 1.0;

            int raw = DamageCalculator.CalculateRawDamage(attacker, normalAction, 1.0, 1.0, 10);
            int withArmor = DamageCalculator.CalculateDamage(attacker, armoredEnemy, normalAction, 1.0, 1.0, 0, 10);
            int expectedArmored = Math.Max(GameConfiguration.Instance.Combat.MinimumDamage, raw - 15);
            TestBase.AssertEqual(expectedArmored, withArmor,
                "non-pierce swing should subtract enemy armor",
                ref _testsRun, ref _testsPassed, ref _testsFailed);

            var pierceAction = TestDataBuilders.CreateMockAction("PIERCE");
            pierceAction.DamageMultiplier = 1.0;
            pierceAction.CausesPierce = true;
            int rawPierce = DamageCalculator.CalculateRawDamage(attacker, pierceAction, 1.0, 1.0, 10);
            int pierceDmg = DamageCalculator.CalculateDamage(attacker, armoredEnemy, pierceAction, 1.0, 1.0, 0, 10);
            TestBase.AssertEqual(rawPierce, pierceDmg,
                "CausesPierce swing should ignore armor",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
            TestBase.AssertEqual(0, DamageCalculator.ResolveTargetArmor(armoredEnemy, pierceAction),
                "ResolveTargetArmor with CausesPierce is 0",
                ref _testsRun, ref _testsPassed, ref _testsFailed);

            armoredEnemy.HasPierce = true;
            armoredEnemy.PierceTurns = 3;
            TestBase.AssertEqual(0, DamageCalculator.ResolveTargetArmor(armoredEnemy),
                "pierced target ResolveTargetArmor is 0",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
            int vsPierced = DamageCalculator.CalculateDamage(attacker, armoredEnemy, normalAction, 1.0, 1.0, 0, 10);
            TestBase.AssertEqual(raw, vsPierced,
                "subsequent swings vs pierced target ignore armor",
                ref _testsRun, ref _testsPassed, ref _testsFailed);

            var heroTarget = TestDataBuilders.Character().WithName("HeroPierce").WithLevel(1).Build();
            heroTarget.EquipItem(new ChestItem("Plate", 1, 8), "body");
            heroTarget.HasPierce = true;
            heroTarget.PierceTurns = 2;
            TestBase.AssertEqual(8, heroTarget.GetMaxArmor(),
                "HUD effective armor unchanged while pierced",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
            TestBase.AssertEqual(0, DamageCalculator.ResolveTargetArmor(heroTarget),
                "ResolveTargetArmor ignores pierced hero armor",
                ref _testsRun, ref _testsPassed, ref _testsFailed);

            heroTarget.UpdateTempEffects(1.0);
            TestBase.AssertEqual(1, heroTarget.PierceTurns,
                "PierceTurns decays by one turn",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
            heroTarget.UpdateTempEffects(1.0);
            TestBase.AssertTrue(!heroTarget.HasPierce && heroTarget.PierceTurns == 0,
                "pierce clears when turns expire",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
            TestBase.AssertEqual(8, DamageCalculator.ResolveTargetArmor(heroTarget),
                "armor applies again after pierce expires",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        /// <summary>
        /// Combo-tier rolls (default 14+) unlock named combo actions / AMP; they must not multiply raw damage.
        /// </summary>
        private static void TestComboBandRollDoesNotAmplifyRawDamage()
        {
            Console.WriteLine("\n--- Testing combo-band roll does not amplify raw damage ---");

            var balance = GameConfiguration.Instance.CombatBalance;
                balance.RollDamageMultipliers ??= new RollDamageMultipliersConfig();
            double previousCombo = balance.RollDamageMultipliers.ComboRollDamageMultiplier;
            double previousBasic = balance.RollDamageMultipliers.BasicRollDamageMultiplier;
            try
            {
                balance.RollDamageMultipliers.ComboRollDamageMultiplier = 1.0;
                balance.RollDamageMultipliers.BasicRollDamageMultiplier = 1.0;

                var attacker = TestDataBuilders.Character()
                    .WithName("RollBand")
                    .WithStats(10, 10, 10, 10)
                    .Build();
                attacker.EquipItem(new WeaponItem("TestSword", 1, 20), "weapon");
                var action = TestDataBuilders.CreateMockAction("PIERCE");
                action.DamageMultiplier = 1.0;

                int basicBand = DamageCalculator.CalculateRawDamage(attacker, action, 1.0, 1.0, 10);
                int comboBand = DamageCalculator.CalculateRawDamage(attacker, action, 1.0, 1.0, 16);
                TestBase.AssertEqual(basicBand, comboBand,
                    "roll 16 (combo band) raw damage equals roll 10 (basic band) at 1.0× multipliers",
                    ref _testsRun, ref _testsPassed, ref _testsFailed);
            }
            finally
            {
                balance.RollDamageMultipliers.ComboRollDamageMultiplier = previousCombo;
                balance.RollDamageMultipliers.BasicRollDamageMultiplier = previousBasic;
            }
        }

        private static void TestDamageWithMultipliers()
        {
            Console.WriteLine("\n--- Testing DamageWithMultipliers ---");

            var attacker = TestDataBuilders.Character()
                .WithName("Attacker")
                .WithStats(10, 10, 10, 10)
                .Build();

            var target = TestDataBuilders.Enemy()
                .WithName("Target")
                .WithHealth(100)
                .Build();

            var action = TestDataBuilders.CreateMockAction("JAB");
            action.DamageMultiplier = 1.0;

            // Test with different multipliers
            var baseDamage = DamageCalculator.CalculateDamage(attacker, target, action, 1.0, 1.0, 0, 10);
            var doubleDamage = DamageCalculator.CalculateDamage(attacker, target, action, 2.0, 1.0, 0, 10);
            var tripleDamage = DamageCalculator.CalculateDamage(attacker, target, action, 1.0, 3.0, 0, 10);

            TestBase.AssertTrue(doubleDamage >= baseDamage,
                "Double combo amplifier should increase damage",
                ref _testsRun, ref _testsPassed, ref _testsFailed);

            TestBase.AssertTrue(tripleDamage >= baseDamage,
                "Triple damage multiplier should increase damage",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        #endregion

        #region Cache Tests

        private static void TestCacheInvalidation()
        {
            Console.WriteLine("\n--- Testing CacheInvalidation ---");

            var actor = TestDataBuilders.Character()
                .WithName("Actor")
                .WithStats(10, 10, 10, 10)
                .Build();

            var action = TestDataBuilders.CreateMockAction("JAB");

            // Calculate damage to populate cache
            DamageCalculator.CalculateRawDamage(actor, action, 1.0, 1.0, 10);

            // Invalidate cache
            DamageCalculator.InvalidateCache(actor);

            // Should not throw exception
            TestBase.AssertTrue(true,
                "Cache invalidation should complete without errors",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        private static void TestCacheClearing()
        {
            Console.WriteLine("\n--- Testing CacheClearing ---");

            var actor = TestDataBuilders.Character()
                .WithName("Actor")
                .WithStats(10, 10, 10, 10)
                .Build();

            var action = TestDataBuilders.CreateMockAction("JAB");

            // Calculate damage to populate cache
            DamageCalculator.CalculateRawDamage(actor, action, 1.0, 1.0, 10);

            // Clear all caches
            DamageCalculator.ClearAllCaches();

            // Should not throw exception
            TestBase.AssertTrue(true,
                "Cache clearing should complete without errors",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        private static void TestCacheStats()
        {
            Console.WriteLine("\n--- Testing CacheStats ---");

            // Get cache stats
            var stats = DamageCalculator.GetCacheStats();

            TestBase.AssertTrue(stats.rawHits >= 0,
                "Raw cache hits should be non-negative",
                ref _testsRun, ref _testsPassed, ref _testsFailed);

            TestBase.AssertTrue(stats.rawMisses >= 0,
                "Raw cache misses should be non-negative",
                ref _testsRun, ref _testsPassed, ref _testsFailed);

            TestBase.AssertTrue(stats.finalHits >= 0,
                "Final cache hits should be non-negative",
                ref _testsRun, ref _testsPassed, ref _testsFailed);

            TestBase.AssertTrue(stats.finalMisses >= 0,
                "Final cache misses should be non-negative",
                ref _testsRun, ref _testsPassed, ref _testsFailed);

            TestBase.AssertTrue(stats.rawHitRate >= 0 && stats.rawHitRate <= 1,
                "Raw hit rate should be between 0 and 1",
                ref _testsRun, ref _testsPassed, ref _testsFailed);

            TestBase.AssertTrue(stats.finalHitRate >= 0 && stats.finalHitRate <= 1,
                "Final hit rate should be between 0 and 1",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        #endregion

        #region Edge Cases

        private static void TestEdgeCases()
        {
            Console.WriteLine("\n--- Testing EdgeCases ---");

            var attacker = TestDataBuilders.Character()
                .WithName("Attacker")
                .WithStats(1, 1, 1, 1)
                .Build();

            var target = TestDataBuilders.Enemy()
                .WithName("Target")
                .WithHealth(1)
                .Build();

            var action = TestDataBuilders.CreateMockAction("JAB");
            action.DamageMultiplier = 0.1;

            // Test with very low stats
            var lowDamage = DamageCalculator.CalculateDamage(attacker, target, action, 1.0, 1.0, 0, 1);
            TestBase.AssertTrue(lowDamage >= 0,
                "Damage with low stats should be non-negative",
                ref _testsRun, ref _testsPassed, ref _testsFailed);

            // Test with zero multiplier
            var zeroDamage = DamageCalculator.CalculateDamage(attacker, target, action, 0.0, 1.0, 0, 10);
            TestBase.AssertTrue(zeroDamage >= 0,
                "Damage with zero multiplier should be non-negative",
                ref _testsRun, ref _testsPassed, ref _testsFailed);

            // Test with null action
            var nullActionDamage = DamageCalculator.CalculateRawDamage(attacker, null, 1.0, 1.0, 10);
            TestBase.AssertTrue(nullActionDamage >= 0,
                "Damage with null action should be non-negative",
                ref _testsRun, ref _testsPassed, ref _testsFailed);

            // Test with very high roll
            var highRollDamage = DamageCalculator.CalculateDamage(attacker, target, action, 1.0, 1.0, 0, 20);
            TestBase.AssertTrue(highRollDamage >= 0,
                "Damage with high roll should be non-negative",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        #endregion
    }
}
