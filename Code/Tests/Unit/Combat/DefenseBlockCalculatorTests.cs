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
    /// Hero 2d10 vs attack face: armor → per-swing block. Enemies keep 100% armor; pierce and misses do not roll.
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
            TestCalculateDamageOpposedPunchAndGuard();
            TestCalculateDamageOmittedFaceKeepsFullArmor();
            TestNeutralAttackFaceWhenMissing();
            TestMissDoesNotConsumeUnforcedQueue();
            TestHitStoresDefenseTotalFromUnforcedQueue();
            TestMultiHitReusesOneDefenseTotal();
            TestFormatterFooterMatchesCalculateDamage();
            TestEnumerate2d10VsD20Distribution();

            TestBase.PrintSummary("DefenseBlockCalculator Tests", _testsRun, _testsPassed, _testsFailed);
        }

        private static void TestBandTableAndRounding()
        {
            Console.WriteLine("--- Testing opposed margin bands and rounding ---");

            TestBase.AssertEqual(0.0, DefenseBlockCalculator.GetMultiplier(20, 2), "20 vs 2 = punch 0%", ref _testsRun, ref _testsPassed, ref _testsFailed);
            TestBase.AssertEqual(0.0, DefenseBlockCalculator.GetMultiplier(16, 8), "16 vs 8 = +8 punch", ref _testsRun, ref _testsPassed, ref _testsFailed);
            TestBase.AssertEqual(0.0, DefenseBlockCalculator.GetMultiplier(20, 11), "20 vs 11 = +9 punch", ref _testsRun, ref _testsPassed, ref _testsFailed);
            TestBase.AssertEqual(0.25, DefenseBlockCalculator.GetMultiplierFromMargin(7), "margin +7 = 25%", ref _testsRun, ref _testsPassed, ref _testsFailed);
            TestBase.AssertEqual(0.25, DefenseBlockCalculator.GetMultiplierFromMargin(4), "margin +4 = 25%", ref _testsRun, ref _testsPassed, ref _testsFailed);
            TestBase.AssertEqual(1.0, DefenseBlockCalculator.GetMultiplierFromMargin(3), "margin +3 = 100%", ref _testsRun, ref _testsPassed, ref _testsFailed);
            TestBase.AssertEqual(1.0, DefenseBlockCalculator.GetMultiplier(12, 14), "12 vs 14 = scrape 100%", ref _testsRun, ref _testsPassed, ref _testsFailed);
            TestBase.AssertEqual(1.0, DefenseBlockCalculator.GetMultiplier(10, 11), "10 vs 11 = scrape 100%", ref _testsRun, ref _testsPassed, ref _testsFailed);
            TestBase.AssertEqual(1.0, DefenseBlockCalculator.GetMultiplierFromMargin(-3), "margin -3 = 100%", ref _testsRun, ref _testsPassed, ref _testsFailed);
            TestBase.AssertEqual(1.25, DefenseBlockCalculator.GetMultiplierFromMargin(-4), "margin -4 = 125%", ref _testsRun, ref _testsPassed, ref _testsFailed);
            TestBase.AssertEqual(1.25, DefenseBlockCalculator.GetMultiplierFromMargin(-7), "margin -7 = 125%", ref _testsRun, ref _testsPassed, ref _testsFailed);
            TestBase.AssertEqual(2.0, DefenseBlockCalculator.GetMultiplierFromMargin(-8), "margin -8 = 200%", ref _testsRun, ref _testsPassed, ref _testsFailed);
            TestBase.AssertEqual(2.0, DefenseBlockCalculator.GetMultiplier(2, 20), "2 vs 20 = 200%", ref _testsRun, ref _testsPassed, ref _testsFailed);

            TestBase.AssertEqual(18, DefenseBlockCalculator.GetMargin(20, 2), "margin 20-2", ref _testsRun, ref _testsPassed, ref _testsFailed);
            TestBase.AssertEqual(-2, DefenseBlockCalculator.GetMargin(12, 14), "margin 12-14", ref _testsRun, ref _testsPassed, ref _testsFailed);
            TestBase.AssertEqual(0, DefenseBlockCalculator.GetMargin(0, 11), "missing attack face uses 11", ref _testsRun, ref _testsPassed, ref _testsFailed);
            TestBase.AssertEqual("+5", DefenseBlockCalculator.FormatMarginSigned(5), "positive margin signed", ref _testsRun, ref _testsPassed, ref _testsFailed);
            TestBase.AssertEqual("0", DefenseBlockCalculator.FormatMarginSigned(0), "zero margin", ref _testsRun, ref _testsPassed, ref _testsFailed);
            TestBase.AssertEqual("-3", DefenseBlockCalculator.FormatMarginSigned(-3), "negative margin", ref _testsRun, ref _testsPassed, ref _testsFailed);

            TestBase.AssertEqual(0, DefenseBlockCalculator.ComputeBlock(8, 20, 2), "punch block 0", ref _testsRun, ref _testsPassed, ref _testsFailed);
            TestBase.AssertEqual(1, DefenseBlockCalculator.ComputeBlock(5, 15, 11), "5 * 0.25 rounds to 1", ref _testsRun, ref _testsPassed, ref _testsFailed);
            TestBase.AssertEqual(1, DefenseBlockCalculator.ComputeBlock(2, 15, 11), "2 * 0.25 = 0.5 away-from-zero = 1", ref _testsRun, ref _testsPassed, ref _testsFailed);
            TestBase.AssertEqual(0, DefenseBlockCalculator.ComputeBlock(1, 15, 11), "1 * 0.25 = 0.25 rounds to 0", ref _testsRun, ref _testsPassed, ref _testsFailed);
            TestBase.AssertEqual(8, DefenseBlockCalculator.ComputeBlock(8, 10, 11), "scrape = 100% armor", ref _testsRun, ref _testsPassed, ref _testsFailed);
            TestBase.AssertEqual(5, DefenseBlockCalculator.ComputeBlock(4, 10, 16), "4 * 1.25 = 5", ref _testsRun, ref _testsPassed, ref _testsFailed);
            TestBase.AssertEqual(16, DefenseBlockCalculator.ComputeBlock(8, 2, 20), "2 vs 20 = 200%", ref _testsRun, ref _testsPassed, ref _testsFailed);
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

        private static void TestCalculateDamageOpposedPunchAndGuard()
        {
            Console.WriteLine("\n--- CalculateDamage opposed punch and great guard ---");

            var attacker = TestDataBuilders.Enemy().WithName("Orc").WithHealth(100).Build();
            var hero = TestDataBuilders.Character().WithName("Tank").WithLevel(1).Build();
            hero.EquipItem(new ChestItem("Plate", 1, 8), "body");
            var action = TestDataBuilders.CreateMockAction("JAB");
            action.DamageMultiplier = 1.0;

            int raw = DamageCalculator.CalculateRawDamage(attacker, action, 1.0, 1.0, 10);
            int min = Math.Max(1, GameConfiguration.Instance.Combat.MinimumDamage);

            int dmgPunch = DamageCalculator.CalculateDamage(attacker, hero, action, 1.0, 1.0, 0, 10, true, 2, 20);
            TestBase.AssertEqual(Math.Max(min, raw), dmgPunch, "20 vs 2 uses 0 block", ref _testsRun, ref _testsPassed, ref _testsFailed);

            int dmgGuard = DamageCalculator.CalculateDamage(attacker, hero, action, 1.0, 1.0, 0, 10, true, 20, 2);
            TestBase.AssertEqual(Math.Max(min, raw - 16), dmgGuard, "2 vs 20 uses 200% armor (16)", ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        private static void TestCalculateDamageOmittedFaceKeepsFullArmor()
        {
            Console.WriteLine("\n--- Omitted defenseFace keeps 100% hero armor ---");

            var attacker = TestDataBuilders.Enemy().WithName("Orc").WithHealth(100).Build();
            var hero = TestDataBuilders.Character().WithName("Tank").WithLevel(1).Build();
            hero.EquipItem(new ChestItem("Plate", 1, 8), "body");
            var action = TestDataBuilders.CreateMockAction("JAB");
            action.DamageMultiplier = 1.0;

            int raw = DamageCalculator.CalculateRawDamage(attacker, action, 1.0, 1.0, 10);
            int min = Math.Max(1, GameConfiguration.Instance.Combat.MinimumDamage);
            int dmg = DamageCalculator.CalculateDamage(attacker, hero, action, 1.0, 1.0, 0, 10);
            TestBase.AssertEqual(Math.Max(min, raw - 8), dmg, "omitted face = 100% armor", ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        private static void TestNeutralAttackFaceWhenMissing()
        {
            Console.WriteLine("\n--- Missing attack face uses 2d10 mean 11 ---");

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
            Console.WriteLine("\n--- Miss path does not dequeue unforced 2d10 ---");

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
            Dice.QueueUnforcedTestRolls(7, 4);
            try
            {
                var result = ActionExecutionFlow.Execute(enemy, hero, null, null, jab, null, lastUsed, lastCrit);
                TestBase.AssertTrue(!result.Hit, $"forced face {missBase} should miss (bonus {missBonus})", ref _testsRun, ref _testsPassed, ref _testsFailed);
                TestBase.AssertTrue(result.DefenseFace == null, "miss stores no defense face", ref _testsRun, ref _testsPassed, ref _testsFailed);
                int leftoverA = Dice.RollUnforced(10);
                int leftoverB = Dice.RollUnforced(10);
                TestBase.AssertEqual(7, leftoverA, "miss must not consume first unforced d10", ref _testsRun, ref _testsPassed, ref _testsFailed);
                TestBase.AssertEqual(4, leftoverB, "miss must not consume second unforced d10", ref _testsRun, ref _testsPassed, ref _testsFailed);
            }
            finally
            {
                Dice.SetTestRoll(null);
                Dice.ClearUnforcedTestRolls();
            }
        }

        private static void TestHitStoresDefenseTotalFromUnforcedQueue()
        {
            Console.WriteLine("\n--- Hit stores 2d10 sum from unforced queue ---");

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
            Dice.QueueUnforcedTestRolls(1, 1);
            try
            {
                var result = ActionExecutionFlow.Execute(enemy, hero, null, null, jab, null, lastUsed, lastCrit);
                TestBase.AssertTrue(result.Hit, "roll 10 should hit", ref _testsRun, ref _testsPassed, ref _testsFailed);
                TestBase.AssertEqual(2, result.DefenseFace ?? -1, "hit stores 2d10 sum 1+1=2", ref _testsRun, ref _testsPassed, ref _testsFailed);
            }
            finally
            {
                Dice.SetTestRoll(null);
                Dice.ClearUnforcedTestRolls();
            }
        }

        private static void TestMultiHitReusesOneDefenseTotal()
        {
            Console.WriteLine("\n--- Multi-hit reuses one 2d10 total ---");

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
            Dice.QueueUnforcedTestRolls(1, 1);
            int total = MultiHitProcessor.ProcessMultiHit(
                attacker, hero, action, 1.0, totalRoll, totalRoll, rollBonus, 10, null, 0, 20, 2);
            int leftover = Dice.RollUnforced(10);
            Dice.ClearUnforcedTestRolls();

            TestBase.AssertEqual(oneHit * 3, total, "three ticks share 2 vs 20 block", ref _testsRun, ref _testsPassed, ref _testsFailed);
            TestBase.AssertEqual(1, leftover, "ProcessMultiHit must not roll when total is passed", ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        private static void TestFormatterFooterMatchesCalculateDamage()
        {
            Console.WriteLine("\n--- Formatter footer matches CalculateDamage ---");

            var attacker = TestDataBuilders.Enemy().WithName("FmtOrc").WithHealth(100).Build();
            attacker.EquipItem(new WeaponItem("Club", 1, 12), "weapon");
            var hero = TestDataBuilders.Character().WithName("FmtHero").WithLevel(1).Build();
            hero.EquipItem(new ChestItem("Plate", 1, 8), "body");
            var action = TestDataBuilders.CreateMockAction("JAB");
            action.DamageMultiplier = 1.0;
            action.IsComboAction = false;

            const int defenseTotal = 8;
            const int attackFace = 10;
            int raw = CombatCalculator.CalculateRawDamage(attacker, action, 1.0, 1.0, 10, 0);
            int block = DefenseBlockCalculator.ResolveMitigation(hero, action, defenseTotal, attackFace);
            int dmg = DamageCalculator.CalculateDamage(attacker, hero, action, 1.0, 1.0, 0, 10, true, defenseTotal, attackFace);
            int min = Math.Max(1, GameConfiguration.Instance.Combat.MinimumDamage);
            TestBase.AssertEqual(Math.Max(min, raw - block), dmg, "damage uses same block as ResolveMitigation", ref _testsRun, ref _testsPassed, ref _testsFailed);

            var (_, rollInfo) = CombatResults.FormatDamageDisplayColored(
                attacker, hero, dmg, dmg, action, 1.0, 1.0, 0, attackFace, 1, false, null, default, defenseTotal);
            string footer = ColoredTextRenderer.RenderAsPlainText(rollInfo);
            TestBase.AssertTrue(footer.Contains("def: 8", StringComparison.Ordinal),
                $"footer should show def: 8, got: {footer}", ref _testsRun, ref _testsPassed, ref _testsFailed);
            TestBase.AssertTrue(footer.Contains(" | +2", StringComparison.Ordinal) || footer.Contains("| +2 |", StringComparison.Ordinal),
                $"footer should show margin +2, got: {footer}", ref _testsRun, ref _testsPassed, ref _testsFailed);
            TestBase.AssertTrue(footer.Contains($"{block} block", StringComparison.Ordinal),
                $"footer should show {block} block, got: {footer}", ref _testsRun, ref _testsPassed, ref _testsFailed);
            TestBase.AssertTrue(!footer.Contains(" armor", StringComparison.Ordinal),
                "hero defense footer uses block not armor", ref _testsRun, ref _testsPassed, ref _testsFailed);

            var enemy = TestDataBuilders.Enemy().WithName("FmtFoe").WithHealth(100).Build();
            var (_, enemyInfo) = CombatResults.FormatDamageDisplayColored(
                attacker, enemy, 10, 10, action, 1.0, 1.0, 0, 10);
            string enemyFooter = ColoredTextRenderer.RenderAsPlainText(enemyInfo);
            TestBase.AssertTrue(!enemyFooter.Contains("def:", StringComparison.Ordinal),
                "enemy target has no def: segment", ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        private static void TestEnumerate2d10VsD20Distribution()
        {
            Console.WriteLine("\n--- Enumerate 20 x 10 x 10 opposed outcomes ---");

            int n = 0;
            double sum = 0;
            int punch = 0;
            int weak = 0;
            int scrape = 0;
            int strong = 0;
            int guard = 0;
            for (int attack = 1; attack <= 20; attack++)
            {
                for (int d1 = 1; d1 <= 10; d1++)
                {
                    for (int d2 = 1; d2 <= 10; d2++)
                    {
                        double m = DefenseBlockCalculator.GetMultiplier(attack, d1 + d2);
                        sum += m;
                        n++;
                        if (m == 0.0) punch++;
                        else if (m == 0.25) weak++;
                        else if (m == 1.0) scrape++;
                        else if (m == 1.25) strong++;
                        else if (m == 2.0) guard++;
                    }
                }
            }

            double mean = sum / n;
            TestBase.AssertEqual(2000, n, "20 x 10 x 10 = 2000 outcomes", ref _testsRun, ref _testsPassed, ref _testsFailed);
            TestBase.AssertTrue(scrape > punch && scrape > weak && scrape > strong && scrape > guard,
                $"100% scrape should be the mode (punch {punch}, weak {weak}, scrape {scrape}, strong {strong}, guard {guard})",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
            TestBase.AssertTrue(mean > 0.85 && mean < 1.25,
                $"mean multiplier near 1 (got {mean:F3}; punch {punch}/2000, guard {guard}/2000)",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
        }
    }
}
