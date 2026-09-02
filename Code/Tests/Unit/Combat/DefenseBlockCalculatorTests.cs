using System;
using System.Collections.Generic;
using RPGGame;
using RPGGame.Actions;
using RPGGame.Actions.Execution;
using RPGGame.Actions.RollModification;
using RPGGame.Combat.Calculators;
using RPGGame.Combat.Formatting;
using RPGGame.Tests;
using RPGGame.UI.ColorSystem;

namespace RPGGame.Tests.Unit.Combat
{
    /// <summary>
    /// Legacy 1d20 armor×band helpers. Live hero mitigation is leftover BLOCK % + class DEFENSE.
    /// Enemies keep 100% armor; pierce and misses do not roll.
    /// </summary>
    public static class DefenseBlockCalculatorTests
    {
        private static int _testsRun = 0;
        private static int _testsPassed = 0;
        private static int _testsFailed = 0;

        public static void RunAllTests()
        {
            Console.WriteLine("=== DefenseBlockCalculator Tests ===\n");

            _testsRun = 0;
            _testsPassed = 0;
            _testsFailed = 0;

            TestBandTableAndRounding();
            TestShouldRollHeroOnlyNotPierce();
            TestPierceDoesNotConsumeUnforcedQueue();
            TestEnemyTargetDoesNotConsumeUnforcedQueue();
            TestCalculateDamageOpposedAttackAheadAndGuard();
            TestCalculateDamageOmittedFaceKeepsFullArmor();
            TestNeutralAttackFaceWhenMissing();
            TestMissDoesNotConsumeUnforcedQueue();
            TestHitStoresDefenseFaceFromUnforcedQueue();
            TestMultiHitReusesOneDefenseFace();
            TestFormatterFooterMatchesCalculateDamage();
            TestEnumerate1d20VsD20Distribution();

            TestBase.PrintSummary("DefenseBlockCalculator Tests", _testsRun, _testsPassed, _testsFailed);
        }

        private static void TestBandTableAndRounding()
        {
            Console.WriteLine("--- Testing opposed margin bands and rounding ---");

            TestBase.AssertEqual(0.75, DefenseBlockCalculator.GetMultiplier(20, 2), "20 vs 2 = attack-ahead 75%", ref _testsRun, ref _testsPassed, ref _testsFailed);
            TestBase.AssertEqual(0.75, DefenseBlockCalculator.GetMultiplier(16, 8), "16 vs 8 = +8 → 75%", ref _testsRun, ref _testsPassed, ref _testsFailed);
            TestBase.AssertEqual(0.75, DefenseBlockCalculator.GetMultiplier(20, 11), "20 vs 11 = +9 → 75%", ref _testsRun, ref _testsPassed, ref _testsFailed);
            TestBase.AssertEqual(0.75, DefenseBlockCalculator.GetMultiplierFromMargin(7), "margin +7 = 75%", ref _testsRun, ref _testsPassed, ref _testsFailed);
            TestBase.AssertEqual(0.75, DefenseBlockCalculator.GetMultiplierFromMargin(4), "margin +4 = 75%", ref _testsRun, ref _testsPassed, ref _testsFailed);
            TestBase.AssertEqual(1.0, DefenseBlockCalculator.GetMultiplierFromMargin(3), "margin +3 = 100%", ref _testsRun, ref _testsPassed, ref _testsFailed);
            TestBase.AssertEqual(1.0, DefenseBlockCalculator.GetMultiplier(12, 14), "12 vs 14 = scrape 100%", ref _testsRun, ref _testsPassed, ref _testsFailed);
            TestBase.AssertEqual(1.0, DefenseBlockCalculator.GetMultiplier(10, 11), "10 vs 11 = scrape 100%", ref _testsRun, ref _testsPassed, ref _testsFailed);
            TestBase.AssertEqual(1.0, DefenseBlockCalculator.GetMultiplierFromMargin(-3), "margin -3 = 100%", ref _testsRun, ref _testsPassed, ref _testsFailed);
            TestBase.AssertEqual(1.5, DefenseBlockCalculator.GetMultiplierFromMargin(-4), "margin -4 = 150%", ref _testsRun, ref _testsPassed, ref _testsFailed);
            TestBase.AssertEqual(1.5, DefenseBlockCalculator.GetMultiplierFromMargin(-7), "margin -7 = 150%", ref _testsRun, ref _testsPassed, ref _testsFailed);
            TestBase.AssertEqual(1.5, DefenseBlockCalculator.GetMultiplier(2, 20), "2 vs 20 = 150%", ref _testsRun, ref _testsPassed, ref _testsFailed);

            TestBase.AssertEqual(18, DefenseBlockCalculator.GetMargin(20, 2), "margin 20-2", ref _testsRun, ref _testsPassed, ref _testsFailed);
            TestBase.AssertEqual(-2, DefenseBlockCalculator.GetMargin(12, 14), "margin 12-14", ref _testsRun, ref _testsPassed, ref _testsFailed);
            TestBase.AssertEqual(0, DefenseBlockCalculator.GetMargin(0, 11), "missing attack face uses 11", ref _testsRun, ref _testsPassed, ref _testsFailed);
            TestBase.AssertEqual("+5", DefenseBlockCalculator.FormatMarginSigned(5), "positive margin signed", ref _testsRun, ref _testsPassed, ref _testsFailed);
            TestBase.AssertEqual("0", DefenseBlockCalculator.FormatMarginSigned(0), "zero margin", ref _testsRun, ref _testsPassed, ref _testsFailed);
            TestBase.AssertEqual("-3", DefenseBlockCalculator.FormatMarginSigned(-3), "negative margin", ref _testsRun, ref _testsPassed, ref _testsFailed);

            TestBase.AssertEqual(6, DefenseBlockCalculator.ComputeBlock(8, 20, 2), "8 * 0.75 = 6", ref _testsRun, ref _testsPassed, ref _testsFailed);
            TestBase.AssertEqual(4, DefenseBlockCalculator.ComputeBlock(5, 15, 11), "5 * 0.75 = 3.75 away-from-zero = 4", ref _testsRun, ref _testsPassed, ref _testsFailed);
            TestBase.AssertEqual(2, DefenseBlockCalculator.ComputeBlock(2, 15, 11), "2 * 0.75 = 1.5 away-from-zero = 2", ref _testsRun, ref _testsPassed, ref _testsFailed);
            TestBase.AssertEqual(1, DefenseBlockCalculator.ComputeBlock(1, 15, 11), "1 * 0.75 = 0.75 away-from-zero = 1", ref _testsRun, ref _testsPassed, ref _testsFailed);
            TestBase.AssertEqual(8, DefenseBlockCalculator.ComputeBlock(8, 10, 11), "scrape = 100% armor", ref _testsRun, ref _testsPassed, ref _testsFailed);
            TestBase.AssertEqual(6, DefenseBlockCalculator.ComputeBlock(4, 10, 16), "4 * 1.5 = 6", ref _testsRun, ref _testsPassed, ref _testsFailed);
            TestBase.AssertEqual(12, DefenseBlockCalculator.ComputeBlock(8, 2, 20), "2 vs 20 = 150% of 8 = 12", ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        private static void TestShouldRollHeroOnlyNotPierce()
        {
            Console.WriteLine("\n--- Testing ShouldRoll ---");

            var hero = TestDataBuilders.Character().WithName("DefHero").Build();
            var enemy = TestDataBuilders.Enemy().WithName("DefEnemy").Build();
            var jab = TestDataBuilders.CreateMockAction("JAB");
            var pierce = TestDataBuilders.CreateMockAction("PIERCE");
            pierce.CausesPierce = true;

            TestBase.AssertTrue(DefenseBlockCalculator.IsHeroDefender(hero), "hero is defender", ref _testsRun, ref _testsPassed, ref _testsFailed);
            TestBase.AssertTrue(!DefenseBlockCalculator.IsHeroDefender(enemy), "enemy is not hero defender", ref _testsRun, ref _testsPassed, ref _testsFailed);
            TestBase.AssertTrue(DefenseBlockCalculator.ShouldRoll(hero, jab), "hero roll on normal hit", ref _testsRun, ref _testsPassed, ref _testsFailed);
            TestBase.AssertTrue(!DefenseBlockCalculator.ShouldRoll(enemy, jab), "enemy does not roll", ref _testsRun, ref _testsPassed, ref _testsFailed);
            TestBase.AssertTrue(!DefenseBlockCalculator.ShouldRoll(hero, pierce), "pierce skips roll", ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        private static void TestPierceDoesNotConsumeUnforcedQueue()
        {
            Console.WriteLine("\n--- Pierce does not consume unforced queue ---");

            var hero = TestDataBuilders.Character().WithName("PierceHero").Build();
            var pierce = TestDataBuilders.CreateMockAction("PIERCE");
            pierce.CausesPierce = true;
            Dice.ClearUnforcedTestRolls();
            Dice.QueueUnforcedTestRolls(17);
            int? face = DefenseBlockCalculator.TryRollDefenseFace(hero, pierce);
            int leftover = Dice.RollUnforced(20);
            Dice.ClearUnforcedTestRolls();

            TestBase.AssertTrue(face == null, "pierce TryRoll returns null", ref _testsRun, ref _testsPassed, ref _testsFailed);
            TestBase.AssertEqual(17, leftover, "unforced queue unused on pierce", ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        private static void TestEnemyTargetDoesNotConsumeUnforcedQueue()
        {
            Console.WriteLine("\n--- Enemy target does not consume unforced queue ---");

            var enemy = TestDataBuilders.Enemy().WithName("ArmoredFoe").Build();
            var jab = TestDataBuilders.CreateMockAction("JAB");
            Dice.ClearUnforcedTestRolls();
            Dice.QueueUnforcedTestRolls(17);
            int? face = DefenseBlockCalculator.TryRollDefenseFace(enemy, jab);
            int leftover = Dice.RollUnforced(20);
            Dice.ClearUnforcedTestRolls();

            TestBase.AssertTrue(face == null, "enemy TryRoll returns null", ref _testsRun, ref _testsPassed, ref _testsFailed);
            TestBase.AssertEqual(17, leftover, "unforced queue unused vs enemy", ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        private static void TestCalculateDamageOpposedAttackAheadAndGuard()
        {
            Console.WriteLine("\n--- CalculateDamage leftover BLOCK independent of 1d20 faces ---");

            var attacker = TestDataBuilders.Enemy().WithName("Orc").WithHealth(100).Build();
            var hero = TestDataBuilders.Character().WithName("Tank").WithLevel(1).Build();
            hero.EquipItem(new ChestItem("Plate", 1, 8), "body");
            var action = TestDataBuilders.CreateMockAction("JAB");
            action.DamageMultiplier = 1.0;

            int raw = DamageCalculator.CalculateRawDamage(attacker, action, 1.0, 1.0, 10);
            hero.LeftoverEnergy = 0;
            int open = DamageCalculator.CalculateDamage(attacker, hero, action, 1.0, 1.0, 0, 10, true, 2, 20);
            hero.LeftoverEnergy = 0;
            int openOtherFace = DamageCalculator.CalculateDamage(attacker, hero, action, 1.0, 1.0, 0, 10, true, 20, 2);
            int warrior = (int)Math.Round(raw * (1.0 - ClassDefenseCalculator.GetWarriorArmorPercent(8)), MidpointRounding.AwayFromZero);
            int min = Math.Max(1, GameConfiguration.Instance.Combat.MinimumDamage);
            int expected = Math.Max(min, warrior);
            TestBase.AssertEqual(expected, open, "defense faces do not change leftover-0 DEFENSE %", ref _testsRun, ref _testsPassed, ref _testsFailed);
            TestBase.AssertEqual(expected, openOtherFace, "other defense face still leftover-0 DEFENSE %", ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        private static void TestCalculateDamageOmittedFaceKeepsFullArmor()
        {
            Console.WriteLine("\n--- Omitted defenseFace uses leftover 0 DEFENSE % ---");

            var attacker = TestDataBuilders.Enemy().WithName("Orc").WithHealth(100).Build();
            var hero = TestDataBuilders.Character().WithName("Tank").WithLevel(1).Build();
            hero.EquipItem(new ChestItem("Plate", 1, 8), "body");
            var action = TestDataBuilders.CreateMockAction("JAB");
            action.DamageMultiplier = 1.0;

            int raw = DamageCalculator.CalculateRawDamage(attacker, action, 1.0, 1.0, 10);
            int min = Math.Max(1, GameConfiguration.Instance.Combat.MinimumDamage);
            int dmg = DamageCalculator.CalculateDamage(attacker, hero, action, 1.0, 1.0, 0, 10);
            int warrior = (int)Math.Round(raw * (1.0 - ClassDefenseCalculator.GetWarriorArmorPercent(8)), MidpointRounding.AwayFromZero);
            TestBase.AssertEqual(Math.Max(min, warrior), dmg, "leftover 0 = Warrior DEFENSE %", ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        private static void TestNeutralAttackFaceWhenMissing()
        {
            Console.WriteLine("\n--- Missing attack face uses d20 stand-in 11 ---");

            var hero = TestDataBuilders.Character().WithName("EnvHero").WithLevel(1).Build();
            hero.EquipItem(new ChestItem("Plate", 1, 8), "body");
            var action = TestDataBuilders.CreateMockAction("HAZARD");
            action.Type = ActionType.Attack;

            int withNull = DefenseBlockCalculator.ResolveMitigation(hero, action, 11, null);
            int withZero = DefenseBlockCalculator.ResolveMitigation(hero, action, 11, 0);
            int explicitNeutral = DefenseBlockCalculator.ResolveMitigation(hero, action, 11, DefenseBlockCalculator.NeutralAttackFace);
            TestBase.AssertEqual(8, withNull, "null attack face vs 11 = scrape", ref _testsRun, ref _testsPassed, ref _testsFailed);
            TestBase.AssertEqual(8, withZero, "roll 0 attack face vs 11 = scrape", ref _testsRun, ref _testsPassed, ref _testsFailed);
            TestBase.AssertEqual(8, explicitNeutral, "explicit 11 vs 11 = scrape", ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        private static void TestMissDoesNotConsumeUnforcedQueue()
        {
            Console.WriteLine("\n--- Miss path does not dequeue unforced 1d20 ---");

            _ = GameConfiguration.Instance;
            var enemy = TestDataBuilders.Enemy().WithName("Misser").Build();
            var hero = TestDataBuilders.Character().WithName("MissHero").WithLevel(1).Build();
            hero.EquipItem(new ChestItem("Plate", 1, 8), "body");
            var jab = TestDataBuilders.CreateMockAction("JAB");
            jab.Type = ActionType.Attack;
            jab.DamageMultiplier = 1.0;
            jab.Target = TargetType.SingleTarget;

            var lastUsed = new Dictionary<Actor, Action>();
            var lastCrit = new Dictionary<Actor, bool>();
            var tm = RollModificationManager.GetThresholdManager();
            tm.ResetThresholds(enemy);
            int missBonus = ActionUtilities.CalculateRollBonus(enemy, jab, consumeTempBonus: false);
            int missBase = tm.GetHitThreshold(enemy) - missBonus - 1;
            if (missBase < 1) missBase = 1;
            if (missBase > 20) missBase = 20;
            Dice.ClearUnforcedTestRolls();
            Dice.SetTestRoll(missBase);
            Dice.QueueUnforcedTestRolls(17);
            try
            {
                var result = ActionExecutionFlow.Execute(enemy, hero, null, null, jab, null, lastUsed, lastCrit);
                TestBase.AssertTrue(!result.Hit, $"forced face {missBase} should miss (bonus {missBonus})", ref _testsRun, ref _testsPassed, ref _testsFailed);
                TestBase.AssertTrue(result.DefenseFace == null, "miss stores no defense face", ref _testsRun, ref _testsPassed, ref _testsFailed);
                int leftover = Dice.RollUnforced(20);
                TestBase.AssertEqual(17, leftover, "miss must not consume unforced d20", ref _testsRun, ref _testsPassed, ref _testsFailed);
            }
            finally
            {
                Dice.SetTestRoll(null);
                Dice.ClearUnforcedTestRolls();
            }
        }

        private static void TestHitStoresDefenseFaceFromUnforcedQueue()
        {
            Console.WriteLine("\n--- Hit stores 1d20 face from unforced queue ---");

            _ = GameConfiguration.Instance;
            var enemy = TestDataBuilders.Enemy().WithName("Hitter").Build();
            var hero = TestDataBuilders.Character().WithName("HitHero").WithLevel(1).Build();
            hero.EquipItem(new ChestItem("Plate", 1, 8), "body");
            var jab = TestDataBuilders.CreateMockAction("JAB");
            jab.Type = ActionType.Attack;
            jab.DamageMultiplier = 1.0;
            jab.Target = TargetType.SingleTarget;

            var lastUsed = new Dictionary<Actor, Action>();
            var lastCrit = new Dictionary<Actor, bool>();
            Dice.ClearUnforcedTestRolls();
            Dice.SetTestRoll(10);
            Dice.QueueUnforcedTestRolls(7);
            try
            {
                var result = ActionExecutionFlow.Execute(enemy, hero, null, null, jab, null, lastUsed, lastCrit);
                TestBase.AssertTrue(result.Hit, "roll 10 should hit", ref _testsRun, ref _testsPassed, ref _testsFailed);
                TestBase.AssertEqual(7, result.DefenseFace ?? -1, "hit stores unforced d20 face 7", ref _testsRun, ref _testsPassed, ref _testsFailed);
            }
            finally
            {
                Dice.SetTestRoll(null);
                Dice.ClearUnforcedTestRolls();
            }
        }

        private static void TestMultiHitReusesOneDefenseFace()
        {
            Console.WriteLine("\n--- Multi-hit reuses one 1d20 face ---");

            var attacker = TestDataBuilders.Enemy().WithName("MultiOrc").WithHealth(100).Build();
            var hero = TestDataBuilders.Character().WithName("MultiHero").WithLevel(1).Build();
            hero.EquipItem(new ChestItem("Plate", 1, 8), "body");
            var action = new Action
            {
                Name = "Triple",
                Type = ActionType.Attack,
                DamageMultiplier = 1.0,
                Target = TargetType.SingleTarget,
                Advanced = new AdvancedMechanicsProperties { MultiHitCount = 3 }
            };

            int rollBonus = 0;
            int totalRoll = 10;
            int oneHit = CombatCalculator.CalculateDamage(attacker, hero, action, 1.0, 1.0, rollBonus, totalRoll, true, 20, 2);

            Dice.ClearUnforcedTestRolls();
            Dice.QueueUnforcedTestRolls(3);
            int total = MultiHitProcessor.ProcessMultiHit(
                attacker, hero, action, 1.0, totalRoll, totalRoll, rollBonus, 10, null, 0, 20, 2);
            int leftover = Dice.RollUnforced(20);
            Dice.ClearUnforcedTestRolls();

            TestBase.AssertEqual(oneHit * 3, total, "three ticks share 2 vs 20 block", ref _testsRun, ref _testsPassed, ref _testsFailed);
            TestBase.AssertEqual(3, leftover, "ProcessMultiHit must not roll when total is passed", ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        private static void TestFormatterFooterMatchesCalculateDamage()
        {
            Console.WriteLine("\n--- Formatter footer matches leftover BLOCK / DEFENSE ---");

            var attacker = TestDataBuilders.Enemy().WithName("FmtOrc").WithHealth(100).Build();
            attacker.EquipItem(new WeaponItem("Club", 1, 12), "weapon");
            var hero = TestDataBuilders.Character().WithName("FmtHero").WithLevel(1).Build();
            hero.EquipItem(new ChestItem("Plate", 1, 8), "body");
            hero.LeftoverEnergy = 2;
            var action = TestDataBuilders.CreateMockAction("JAB");
            action.DamageMultiplier = 1.0;
            action.IsComboAction = false;

            int dmg = DamageCalculator.CalculateDamage(attacker, hero, action, 1.0, 1.0, 0, 10);
            TestBase.AssertTrue(dmg > 0, "hero still takes some damage", ref _testsRun, ref _testsPassed, ref _testsFailed);

            var (_, rollInfo) = CombatResults.FormatDamageDisplayColored(
                attacker, hero, dmg, dmg, action, 1.0, 1.0, 0, 10, 1, false, null, default, 8);
            string footer = ColoredTextRenderer.RenderAsPlainText(rollInfo);
            TestBase.AssertTrue(footer.Contains("leftover 2", StringComparison.Ordinal),
                $"footer should show leftover 2, got: {footer}", ref _testsRun, ref _testsPassed, ref _testsFailed);
            TestBase.AssertTrue(footer.Contains("BLOCK", StringComparison.Ordinal),
                $"footer should show BLOCK, got: {footer}", ref _testsRun, ref _testsPassed, ref _testsFailed);
            TestBase.AssertTrue(!footer.Contains("def: 8", StringComparison.Ordinal),
                "hero leftover footer does not use opposed 1d20 def:", ref _testsRun, ref _testsPassed, ref _testsFailed);

            var enemy = TestDataBuilders.Enemy().WithName("FmtFoe").WithHealth(100).Build();
            var (_, enemyInfo) = CombatResults.FormatDamageDisplayColored(
                attacker, enemy, 10, 10, action, 1.0, 1.0, 0, 10);
            string enemyFooter = ColoredTextRenderer.RenderAsPlainText(enemyInfo);
            TestBase.AssertTrue(!enemyFooter.Contains("leftover", StringComparison.Ordinal),
                "enemy target has no leftover BLOCK", ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        private static void TestEnumerate1d20VsD20Distribution()
        {
            Console.WriteLine("\n--- Enumerate 20 x 20 opposed outcomes ---");

            int n = 0;
            double sum = 0;
            int weak = 0;
            int scrape = 0;
            int strong = 0;
            int other = 0;
            int critWeak = 0;
            int critScrape = 0;
            int critStrong = 0;
            for (int attack = 1; attack <= 20; attack++)
            {
                for (int defense = 1; defense <= 20; defense++)
                {
                    double m = DefenseBlockCalculator.GetMultiplier(attack, defense);
                    sum += m;
                    n++;
                    if (m == 0.75) weak++;
                    else if (m == 1.0) scrape++;
                    else if (m == 1.5) strong++;
                    else other++;

                    if (attack == 20)
                    {
                        if (m == 0.75) critWeak++;
                        else if (m == 1.0) critScrape++;
                        else if (m == 1.5) critStrong++;
                    }
                }
            }

            double mean = sum / n;
            TestBase.AssertEqual(400, n, "20 x 20 = 400 outcomes", ref _testsRun, ref _testsPassed, ref _testsFailed);
            TestBase.AssertEqual(0, other, "only 75 / 100 / 150 bands", ref _testsRun, ref _testsPassed, ref _testsFailed);
            TestBase.AssertEqual(136, weak, "margin ≥ +4 = 136/400", ref _testsRun, ref _testsPassed, ref _testsFailed);
            TestBase.AssertEqual(128, scrape, "margin −3…+3 = 128/400", ref _testsRun, ref _testsPassed, ref _testsFailed);
            TestBase.AssertEqual(136, strong, "margin ≤ −4 = 136/400", ref _testsRun, ref _testsPassed, ref _testsFailed);
            TestBase.AssertTrue(mean > 1.0 && mean < 1.2,
                $"mean multiplier near 1.08 (got {mean:F3})",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
            TestBase.AssertEqual(16, critWeak, "attack 20 vs def 1–16 = 75%", ref _testsRun, ref _testsPassed, ref _testsFailed);
            TestBase.AssertEqual(4, critScrape, "attack 20 vs def 17–20 = 100%", ref _testsRun, ref _testsPassed, ref _testsFailed);
            TestBase.AssertEqual(0, critStrong, "attack 20 never reaches 150%", ref _testsRun, ref _testsPassed, ref _testsFailed);
        }
    }
}
