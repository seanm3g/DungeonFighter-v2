using System;
using RPGGame.Combat.Calculators;
using RPGGame.Data;

namespace RPGGame.Tests.Unit.Combat
{
    /// <summary>
    /// Status effects that change combat numbers: Weaken / Focus (outgoing), Vulnerability (incoming),
    /// Harden / Expose (Defense), their decay timing, Slow direction, and which actions can roll on which gear.
    /// </summary>
    public static class StatusCombatModifiersTests
    {
        private static int _testsRun;
        private static int _testsPassed;
        private static int _testsFailed;

        public static void RunAllTests()
        {
            Console.WriteLine("=== StatusCombatModifiers Tests ===\n");
            _testsRun = 0;
            _testsPassed = 0;
            _testsFailed = 0;

            TestWeakenLowersOutgoingUntilHolderActs();
            TestVulnerabilityRaisesIncomingAndDecays();
            TestHardenAndExposeShiftDefense();
            TestStacksAreCapped();
            TestSlowLengthensAttackTime();
            TestGearActionTagRules();

            Console.WriteLine($"\nStatusCombatModifiers: {_testsPassed}/{_testsRun} passed, {_testsFailed} failed.\n");
        }

        private static void TestWeakenLowersOutgoingUntilHolderActs()
        {
            var c = new Character("WeakenTest", 1);
            c.ApplyWeaken(1);
            c.UpdateTempEffects(Character.DEFAULT_ACTION_LENGTH);
            TestBase.AssertTrue(StatusCombatModifiers.OutgoingDamageMultiplier(c) < 1.0,
                "Weaken survives the start-of-action decay and lowers that action's damage", ref _testsRun, ref _testsPassed, ref _testsFailed);
            c.ConsumeOwnActionStatuses();
            TestBase.AssertEqual(1.0, StatusCombatModifiers.OutgoingDamageMultiplier(c),
                "Weaken ends after the holder's action", ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        private static void TestVulnerabilityRaisesIncomingAndDecays()
        {
            var c = new Character("VulnTest", 1);
            c.VulnerabilityStacks = 1;
            c.VulnerabilityTurns = 1;
            TestBase.AssertTrue(StatusCombatModifiers.IncomingDamageMultiplier(c) > 1.0,
                "Vulnerability raises damage taken", ref _testsRun, ref _testsPassed, ref _testsFailed);
            c.UpdateTempEffects(Character.DEFAULT_ACTION_LENGTH);
            TestBase.AssertNull(c.VulnerabilityStacks, "Vulnerability stacks clear when its turns run out", ref _testsRun, ref _testsPassed, ref _testsFailed);
            TestBase.AssertEqual(1.0, StatusCombatModifiers.IncomingDamageMultiplier(c),
                "No extra damage after Vulnerability ends", ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        private static void TestHardenAndExposeShiftDefense()
        {
            var c = new Character("DefenseTest", 1);
            c.HardenStacks = 1;
            c.HardenTurns = 2;
            TestBase.AssertTrue(StatusCombatModifiers.DefenseBonus(c, 100) > 0, "Harden adds Defense", ref _testsRun, ref _testsPassed, ref _testsFailed);
            var hardened = ClassDefenseCalculator.ApplyIncoming(c, 100, pierce: false);
            c.HardenStacks = null;
            c.HardenTurns = 0;
            var plain = ClassDefenseCalculator.ApplyIncoming(c, 100, pierce: false);
            TestBase.AssertTrue(hardened.Remaining < plain.Remaining, "Harden lowers damage taken", ref _testsRun, ref _testsPassed, ref _testsFailed);

            c.ExposeStacks = 3;
            c.ExposeTurns = 2;
            var exposed = ClassDefenseCalculator.ApplyIncoming(c, 100, pierce: false);
            TestBase.AssertTrue(exposed.Remaining > plain.Remaining, "Expose raises damage taken, even past zero Defense", ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        private static void TestStacksAreCapped()
        {
            var c = new Character("CapTest", 1);
            int cap = GameConfiguration.Instance.Combat.StatusMaxStacks;
            if (cap <= 0)
                return;
            c.VulnerabilityTurns = 3;
            c.VulnerabilityStacks = cap;
            double atCap = StatusCombatModifiers.IncomingDamageMultiplier(c);
            c.VulnerabilityStacks = cap + 5;
            TestBase.AssertEqual(atCap, StatusCombatModifiers.IncomingDamageMultiplier(c),
                "Stacks above the cap add nothing", ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        private static void TestSlowLengthensAttackTime()
        {
            var c = new Character("SlowTest", 1);
            double before = c.GetTotalAttackSpeed();
            c.Effects.ApplySlow(0.5, 2);
            TestBase.AssertTrue(c.GetTotalAttackSpeed() > before, "Slow makes attacks take longer, not shorter", ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        private static void TestGearActionTagRules()
        {
            TestBase.AssertTrue(GearActionPools.IsBasicAttack(new[] { "startingweapon", "mace" }), "Starter attacks are basic", ref _testsRun, ref _testsPassed, ref _testsFailed);
            TestBase.AssertTrue(GearActionPools.IsNeutral(new[] { "action" }), "Untagged-by-class actions are neutral", ref _testsRun, ref _testsPassed, ref _testsFailed);
            TestBase.AssertFalse(GearActionPools.IsNeutral(new[] { "class", "wizard" }), "Class-tagged actions are not neutral", ref _testsRun, ref _testsPassed, ref _testsFailed);
            TestBase.AssertTrue(GearActionPools.IsClassAction(new[] { "class", "barbarian" }, WeaponType.Mace), "Barbarian actions belong to the Mace", ref _testsRun, ref _testsPassed, ref _testsFailed);
            TestBase.AssertFalse(GearActionPools.IsClassAction(new[] { "class", "barbarian" }, WeaponType.Wand), "Barbarian actions do not roll on a Wand", ref _testsRun, ref _testsPassed, ref _testsFailed);
            TestBase.AssertTrue(GearActionPools.WeaponTypeForClass("Wizard") == WeaponType.Wand, "Wizard maps to Wand", ref _testsRun, ref _testsPassed, ref _testsFailed);
        }
    }
}
