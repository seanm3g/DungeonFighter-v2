using System;
using System.Collections.Generic;
using System.Linq;
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
            TestExposeTierTwoStripsArmorAndDecays();
            TestFoldedStatusesApplyExpose();
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

        private static void TestExposeTierTwoStripsArmorAndDecays()
        {
            var combat = GameConfiguration.Instance.Combat;
            int cap = combat.StatusMaxStacks;
            if (cap <= 0 || combat.StatusExposeArmorStripPerStack <= 0)
                return;
            var c = new Character("ExposeTierTest", 10);
            int armorBefore = c.GetMaxArmor();
            ExposeStatus.Apply(c, cap, 1);
            TestBase.AssertEqual(cap, c.ExposeStacks ?? 0, "Expose stacks up to the cap", ref _testsRun, ref _testsPassed, ref _testsFailed);
            TestBase.AssertFalse(ExposeStatus.IsArmorStripped(c), "No armor strip below tier 2", ref _testsRun, ref _testsPassed, ref _testsFailed);
            ExposeStatus.Apply(c, 1, 1);
            TestBase.AssertEqual(cap, c.ExposeStacks ?? 0, "Stacks stay at the cap", ref _testsRun, ref _testsPassed, ref _testsFailed);
            TestBase.AssertTrue(ExposeStatus.IsArmorStripped(c), "Application at the cap strips armor", ref _testsRun, ref _testsPassed, ref _testsFailed);
            if (armorBefore > 0)
                TestBase.AssertTrue(c.GetMaxArmor() < armorBefore, "Tier 2 lowers the armor rating", ref _testsRun, ref _testsPassed, ref _testsFailed);
            ExposeStatus.Apply(c, 20, 1);
            TestBase.AssertTrue(c.ExposeArmorStrip <= combat.StatusExposeArmorStripMax + 1e-9, "Strip is capped", ref _testsRun, ref _testsPassed, ref _testsFailed);
            c.UpdateTempEffects(Character.DEFAULT_ACTION_LENGTH);
            TestBase.AssertNull(c.ExposeStacks, "Expose clears when its turns run out", ref _testsRun, ref _testsPassed, ref _testsFailed);
            TestBase.AssertEqual(0.0, c.ExposeArmorStrip, "Armor strip ends with Expose", ref _testsRun, ref _testsPassed, ref _testsFailed);
            TestBase.AssertEqual(armorBefore, c.GetMaxArmor(), "Armor returns after Expose ends", ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        private static void TestFoldedStatusesApplyExpose()
        {
            var registry = new EffectHandlerRegistry();
            var foe = new Character("FoldTest", 1);
            var action = TestDataBuilders.CreateMockAction("FoldAction");
            registry.ApplyEffect("vulnerability", foe, action, new List<string>());
            registry.ApplyEffect("armorbreak", foe, action, new List<string>());
            TestBase.AssertEqual(2, foe.ExposeStacks ?? 0, "Vulnerability and Armor Break each add an Expose stack", ref _testsRun, ref _testsPassed, ref _testsFailed);
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
            c.ExposeTurns = 3;
            c.ExposeStacks = cap;
            double atCap = StatusCombatModifiers.DefenseBonus(c, 100);
            c.ExposeStacks = cap + 5;
            TestBase.AssertEqual(atCap, StatusCombatModifiers.DefenseBonus(c, 100),
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

            var barbarian = new[] { "barbarian" };
            var warbrute = new[] { "hybrid", "warbrute", "barbarian", "warrior" };
            TestBase.AssertTrue(GearActionPools.IsInClassPool(warbrute, barbarian), "Warbrute hybrids are in a Barbarian's pool", ref _testsRun, ref _testsPassed, ref _testsFailed);
            TestBase.AssertFalse(GearActionPools.IsInClassPool(warbrute, new[] { "rogue" }), "Warbrute hybrids are outside a Rogue's pool", ref _testsRun, ref _testsPassed, ref _testsFailed);
            TestBase.AssertTrue(GearActionPools.IsInClassPool(new[] { "swift" }, barbarian), "Neutral actions are in every pool", ref _testsRun, ref _testsPassed, ref _testsFailed);
            TestBase.AssertTrue(GearActionPools.IsInClassPool(new[] { "class", "warrior" }, new[] { "barbarian", "warrior" }), "A hybrid hero's pool has both classes", ref _testsRun, ref _testsPassed, ref _testsFailed);
            TestBase.AssertFalse(GearActionPools.CanRollOnGear(new ActionData { Name = "STUB", Tags = new List<string> { "unique" } }), "unique rows never roll on gear", ref _testsRun, ref _testsPassed, ref _testsFailed);
            TestBase.AssertFalse(GearActionPools.CanRollOnGear(new ActionData { Name = "IRON CULL", Tags = new List<string> { "iron" }, MaterialScale = "IRON" }), "Material set converts never roll on gear", ref _testsRun, ref _testsPassed, ref _testsFailed);
            var armor = new HeadItem("Cap", 1, 1);
            TestBase.AssertTrue(GearActionPools.ResolveClassTags(new WeaponItem("Wand", 1, 10, 0.05, WeaponType.Wand), null).Contains("wizard"), "No hero class: a weapon uses its own class", ref _testsRun, ref _testsPassed, ref _testsFailed);
            TestBase.AssertTrue(GearActionPools.ResolveClassTags(armor, null).Count == 0, "No hero class: armor has no class", ref _testsRun, ref _testsPassed, ref _testsFailed);
        }
    }
}
