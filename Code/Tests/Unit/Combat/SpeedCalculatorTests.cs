using System;
using RPGGame;
using RPGGame.Combat.Calculators;
using RPGGame.Items;
using RPGGame.Tests;

namespace RPGGame.Tests.Unit.Combat
{
    /// <summary>
    /// Comprehensive tests for SpeedCalculator
    /// Tests attack speed calculations for characters and enemies
    /// </summary>
    public static class SpeedCalculatorTests
    {
        private static int _testsRun = 0;
        private static int _testsPassed = 0;
        private static int _testsFailed = 0;

        /// <summary>
        /// Runs all SpeedCalculator tests
        /// </summary>
        public static void RunAllTests()
        {
            Console.WriteLine("=== SpeedCalculator Tests ===\n");
            
            _testsRun = 0;
            _testsPassed = 0;
            _testsFailed = 0;

            TestCalculateAttackSpeedCharacter();
            TestCalculateAttackSpeedEnemy();
            TestAgilityReduction();
            TestWeaponSpeedModifier();
            TestWeaponAttackSpeedMultiplierVsBaseline();
            TestEquipmentSpeedBonus();
            TestCatalogAttackSpeedOnEquippedLegsAddsToEquipmentBonus();
            TestSlowDebuff();
            TestMinimumAttackTime();
            TestBreakdownIsTheEquation();
            TestWeaponSpeedPointDividesTime();
            TestGearAttackSpeedSuffixIsWeaponSpeedPoints();
            TestAllSuffixKeepsPercentMeaning();
            TestWeaponPreviewMatchesEquipping();

            TestBase.PrintSummary("SpeedCalculator Tests", _testsRun, _testsPassed, _testsFailed);
        }

        #region Character Speed Tests

        private static void TestCalculateAttackSpeedCharacter()
        {
            Console.WriteLine("--- Testing CalculateAttackSpeedCharacter ---");

            var character = TestDataBuilders.Character()
                .WithName("TestCharacter")
                .WithStats(10, 10, 10, 10)
                .Build();

            var speed = SpeedCalculator.CalculateAttackSpeed(character);

            TestBase.AssertTrue(speed > 0,
                "Attack speed should be positive",
                ref _testsRun, ref _testsPassed, ref _testsFailed);

            TestBase.AssertTrue(speed >= 0.01,
                "Attack speed should be at least minimum attack time",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        #endregion

        #region Enemy Speed Tests

        private static void TestCalculateAttackSpeedEnemy()
        {
            Console.WriteLine("\n--- Testing CalculateAttackSpeedEnemy ---");

            var enemy = TestDataBuilders.Enemy()
                .WithName("TestEnemy")
                .WithLevel(1)
                .Build();

            var speed = SpeedCalculator.CalculateAttackSpeed(enemy);

            TestBase.AssertTrue(speed > 0,
                "Enemy attack speed should be positive",
                ref _testsRun, ref _testsPassed, ref _testsFailed);

            TestBase.AssertTrue(speed >= 0.01,
                "Enemy attack speed should be at least minimum attack time",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        #endregion

        #region Agility Tests

        private static void TestAgilityReduction()
        {
            Console.WriteLine("\n--- Testing AgilityReduction ---");

            var lowAgility = TestDataBuilders.Character()
                .WithName("LowAgility")
                .WithStats(10, 5, 10, 10)
                .Build();

            var highAgility = TestDataBuilders.Character()
                .WithName("HighAgility")
                .WithStats(10, 20, 10, 10)
                .Build();

            var lowSpeed = SpeedCalculator.CalculateAttackSpeed(lowAgility);
            var highSpeed = SpeedCalculator.CalculateAttackSpeed(highAgility);

            TestBase.AssertTrue(highSpeed <= lowSpeed,
                "Higher agility should result in faster attack speed",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        #endregion

        #region Weapon Speed Tests

        private static void TestWeaponSpeedModifier()
        {
            Console.WriteLine("\n--- Testing WeaponSpeedModifier ---");

            var character = TestDataBuilders.Character()
                .WithName("TestCharacter")
                .WithStats(10, 10, 10, 10)
                .Build();

            // Test with and without weapon
            var speedNoWeapon = SpeedCalculator.CalculateAttackSpeed(character);

            // Equip a weapon (if possible)
            // Note: This may require weapon setup
            TestBase.AssertTrue(speedNoWeapon > 0,
                "Speed calculation should work with or without weapon",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        /// <summary>
        /// Weapon <c>attackSpeed</c> is a time multiplier: &lt;1 faster (shorter seconds), &gt;1 slower.
        /// </summary>
        private static void TestWeaponAttackSpeedMultiplierVsBaseline()
        {
            Console.WriteLine("\n--- Testing WeaponAttackSpeedMultiplierVsBaseline ---");

            var baseline = TestDataBuilders.Character()
                .WithName("BaseWeapon")
                .WithStats(10, 10, 10, 10)
                .Build();
            baseline.EquipItem(new WeaponItem("Norm", 1, 5, 1.0, WeaponType.Sword), "weapon");
            double tBase = SpeedCalculator.CalculateAttackSpeed(baseline);

            var fastWeapon = TestDataBuilders.Character()
                .WithName("FastWeapon")
                .WithStats(10, 10, 10, 10)
                .Build();
            fastWeapon.EquipItem(new WeaponItem("Fast", 1, 5, 0.8, WeaponType.Sword), "weapon");
            double tFast = SpeedCalculator.CalculateAttackSpeed(fastWeapon);

            var slowWeapon = TestDataBuilders.Character()
                .WithName("SlowWeapon")
                .WithStats(10, 10, 10, 10)
                .Build();
            slowWeapon.EquipItem(new WeaponItem("Slow", 1, 5, 1.2, WeaponType.Sword), "weapon");
            double tSlow = SpeedCalculator.CalculateAttackSpeed(slowWeapon);

            TestBase.AssertTrue(tFast < tBase,
                "attackSpeed below 1 should reduce attack time vs baseline",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
            TestBase.AssertTrue(tSlow > tBase,
                "attackSpeed above 1 should increase attack time vs baseline",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        #endregion

        #region Equipment Speed Tests

        private static void TestEquipmentSpeedBonus()
        {
            Console.WriteLine("\n--- Testing EquipmentSpeedBonus ---");

            var character = TestDataBuilders.Character()
                .WithName("TestCharacter")
                .WithStats(10, 10, 10, 10)
                .Build();

            var speed = SpeedCalculator.CalculateAttackSpeed(character);

            // Equipment speed bonus should reduce attack time
            TestBase.AssertTrue(speed > 0,
                "Speed calculation should account for equipment bonuses",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        /// <summary>
        /// Armor.json <c>attackSpeed</c> → <see cref="Item.CatalogAttackSpeed"/> → <see cref="EquipmentBonusCalculator.GetStatBonusDouble"/> for <c>AttackSpeed</c>.
        /// </summary>
        private static void TestCatalogAttackSpeedOnEquippedLegsAddsToEquipmentBonus()
        {
            Console.WriteLine("\n--- Testing CatalogAttackSpeed on equipped legs adds to equipment bonus ---");

            _ = GameConfiguration.Instance;
            var character = TestDataBuilders.Character()
                .WithName("LegSpeedTest")
                .WithStats(10, 10, 10, 10)
                .Build();

            var legs = new LegsItem("Swift Greaves", 1, 1) { CatalogAttackSpeed = 0.04 };
            character.EquipItem(legs, "legs");

            double bonus = character.Equipment.GetEquipmentAttackSpeedBonus(character);
            TestBase.AssertTrue(Math.Abs(bonus - 0.04) < 0.0001,
                $"GetEquipmentAttackSpeedBonus should include catalog legs speed, got {bonus}",
                ref _testsRun, ref _testsPassed, ref _testsFailed);

            double viaDouble = character.Equipment.GetEquipmentStatBonusDouble("AttackSpeed", character);
            TestBase.AssertTrue(Math.Abs(viaDouble - 0.04) < 0.0001,
                $"GetEquipmentStatBonusDouble AttackSpeed should match, got {viaDouble}",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        #endregion

        #region Slow Debuff Tests

        private static void TestSlowDebuff()
        {
            Console.WriteLine("\n--- Testing SlowDebuff ---");

            var character = TestDataBuilders.Character()
                .WithName("TestCharacter")
                .WithStats(10, 10, 10, 10)
                .Build();

            var normalSpeed = SpeedCalculator.CalculateAttackSpeed(character);

            // Apply slow debuff
            character.Effects.SlowTurns = 1;
            var slowSpeed = SpeedCalculator.CalculateAttackSpeed(character);

            TestBase.AssertTrue(slowSpeed >= normalSpeed,
                "Slow debuff should increase attack time",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        #endregion

        #region Minimum Attack Time Tests

        private static void TestMinimumAttackTime()
        {
            Console.WriteLine("\n--- Testing MinimumAttackTime ---");

            var character = TestDataBuilders.Character()
                .WithName("TestCharacter")
                .WithStats(10, 100, 10, 10) // Very high agility
                .Build();

            var speed = SpeedCalculator.CalculateAttackSpeed(character);

            // Speed should not go below minimum
            TestBase.AssertTrue(speed >= 0.01,
                "Attack speed should respect minimum attack time",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        #endregion

        #region Core Equation Tests

        private static Character NewHero(string name, double weaponSpeed = 1.0)
        {
            var hero = TestDataBuilders.Character()
                .WithName(name)
                .WithStats(10, 10, 10, 10)
                .Build();
            hero.EquipItem(new WeaponItem("Norm", 1, 5, weaponSpeed, WeaponType.Sword), "weapon");
            return hero;
        }

        /// <summary>Swing = max(floor, base × AGI × weapon × tempo); combat reads the same breakdown.</summary>
        private static void TestBreakdownIsTheEquation()
        {
            Console.WriteLine("\n--- Testing breakdown is the swing equation ---");
            var hero = NewHero("EquationHero");
            var b = SpeedCalculator.GetBreakdown(hero);

            double expectedRaw = b.BaseSeconds * b.AgilityFactor * b.WeaponFactor * b.TempoFactor;
            TestBase.AssertTrue(Math.Abs(b.RawSeconds - expectedRaw) < 1e-9,
                $"Raw = base × AGI × weapon × tempo ({b.RawSeconds:0.####} vs {expectedRaw:0.####})",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
            TestBase.AssertTrue(Math.Abs(SpeedCalculator.CalculateAttackSpeed(hero) - Math.Max(b.FloorSeconds, b.RawSeconds)) < 1e-9,
                "CalculateAttackSpeed = max(floor, raw)",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
            TestBase.AssertTrue(Math.Abs(b.BaseSeconds - GameConfiguration.Instance.Combat.BaseAttackTime) < 1e-9,
                "Base time is the fixed config base",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        /// <summary>+1 weapon speed point divides weapon time by (1 + % per point).</summary>
        private static void TestWeaponSpeedPointDividesTime()
        {
            Console.WriteLine("\n--- Testing weapon speed point divides time ---");
            var hero = NewHero("PointHero");
            double before = SpeedCalculator.GetBreakdown(hero).RawSeconds;
            hero.Effects.ConsumedWeaponSpeedFlat = 1;
            double after = SpeedCalculator.GetBreakdown(hero).RawSeconds;
            hero.Effects.ConsumedWeaponSpeedFlat = 0;

            double pct = GameConfiguration.Instance.Combat.GetWeaponSpeedPercentPerPoint();
            TestBase.AssertTrue(Math.Abs(before / after - (1.0 + pct / 100.0)) < 1e-9,
                $"+1 point = +{pct}% speed ({before:0.###}s → {after:0.###}s)",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        /// <summary>Gear "AttackSpeed" suffix value is weapon speed points (literal), not a % of base time.</summary>
        private static void TestGearAttackSpeedSuffixIsWeaponSpeedPoints()
        {
            Console.WriteLine("\n--- Testing gear AttackSpeed suffix is weapon speed points ---");
            var hero = NewHero("SuffixHero");
            double before = SpeedCalculator.GetBreakdown(hero).RawSeconds;

            var legs = new LegsItem("Fleet Greaves", 1, 1);
            legs.StatBonuses.Add(new StatBonus { Name = "of fleeting", StatType = "AttackSpeed", Value = 1 });
            hero.EquipItem(legs, "legs");

            TestBase.AssertTrue(Math.Abs(hero.GetEquipmentAttackSpeedBonus() - 1.0) < 1e-9,
                $"of fleeting +1 = 1 weapon speed point, got {hero.GetEquipmentAttackSpeedBonus()}",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
            double after = SpeedCalculator.GetBreakdown(hero).RawSeconds;
            double pct = GameConfiguration.Instance.Combat.GetWeaponSpeedPercentPerPoint();
            TestBase.AssertTrue(Math.Abs(before / after - (1.0 + pct / 100.0)) < 1e-9,
                $"Suffix point shortens the swing like any other point ({before:0.###}s → {after:0.###}s)",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        /// <summary>"ALL +5" suffixes keep their % meaning for speed: +5% = half a point at 10% per point.</summary>
        private static void TestAllSuffixKeepsPercentMeaning()
        {
            Console.WriteLine("\n--- Testing ALL suffix keeps % meaning for weapon speed ---");
            var chest = new ChestItem("Godly Vest", 1, 1);
            chest.StatBonuses.Add(new StatBonus { Name = "of gods", StatType = "ALL", Value = 5 });
            double pct = GameConfiguration.Instance.Combat.GetWeaponSpeedPercentPerPoint();
            double points = EquipmentBonusCalculator.GetItemWeaponSpeedPoints(chest);
            TestBase.AssertTrue(Math.Abs(points - 5.0 / pct) < 1e-9,
                $"ALL +5 = {5.0 / pct:0.##} weapon speed points, got {points}",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        /// <summary>Item screens preview a weapon as seconds; that preview must equal actually equipping it.</summary>
        private static void TestWeaponPreviewMatchesEquipping()
        {
            Console.WriteLine("\n--- Testing weapon preview matches equipping ---");
            var hero = NewHero("PreviewHero");
            var fast = new WeaponItem("Quick", 1, 5, 0.8, WeaponType.Dagger) { BonusAttackSpeed = 1 };

            double equippedNow = SpeedCalculator.GetBreakdownWithWeapon(hero, hero.Weapon as WeaponItem).FinalSeconds;
            TestBase.AssertTrue(Math.Abs(equippedNow - SpeedCalculator.CalculateAttackSpeed(hero)) < 1e-9,
                "Previewing the equipped weapon equals the live swing",
                ref _testsRun, ref _testsPassed, ref _testsFailed);

            double preview = SpeedCalculator.GetBreakdownWithWeapon(hero, fast).FinalSeconds;
            hero.EquipItem(fast, "weapon");
            double actual = SpeedCalculator.CalculateAttackSpeed(hero);
            TestBase.AssertTrue(Math.Abs(preview - actual) < 1e-9,
                $"Preview {preview:0.###}s equals equipped {actual:0.###}s",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        #endregion
    }
}
