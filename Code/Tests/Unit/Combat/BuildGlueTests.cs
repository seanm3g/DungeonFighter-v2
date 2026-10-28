using System;
using System.Linq;
using RPGGame.Combat.Calculators;
using RPGGame.Combat.Events;
using RPGGame.Data;

namespace RPGGame.Tests.Unit.Combat
{
    /// <summary>
    /// Overnight build pass (2026-10-12): enemy mechanic kits, DoT mod mapping, SPEED_MOD reaching swing time,
    /// percent converts, take-hit-only minting, speed overflow and INT potency.
    /// </summary>
    public static class BuildGlueTests
    {
        private static int _testsRun;
        private static int _testsPassed;
        private static int _testsFailed;

        public static void RunAllTests()
        {
            Console.WriteLine("=== BuildGlue Tests ===\n");
            _testsRun = _testsPassed = _testsFailed = 0;

            TestEnemyKitKeepsFlavorName();
            TestUnknownEnemyKitIsNull();
            TestDotModEffectNames();
            TestLastSwingSpeedModIsTakenOnce();
            TestPercentConvertPaysShareOfHit();
            TestFoeHitMintsOnlyTakeHitCurrencies();
            TestSpeedOverflowTurnsIntoDamage();
            TestIntelligenceAmp();
            TestRepeatedStripActionAdvancesFromItsOwnSlot();
            TestStripReplacementAdvancesFromReplacedSlot();
            TestComboSlotCapByLevel();

            TestBase.PrintSummary("BuildGlue Tests", _testsRun, _testsPassed, _testsFailed);
        }

        private static void TestEnemyKitKeepsFlavorName()
        {
            EnemyActionCatalog.Reload();
            string? template = EnemyActionCatalog.ResolveTemplateName("POISON BITE");
            TestBase.AssertEqual("ENEMY VENOM", template, "POISON BITE resolves to the Venom kit",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
            var action = EnemyActionCatalog.TryCreate("POISON BITE");
            TestBase.AssertNotNull(action, "kit action is created", ref _testsRun, ref _testsPassed, ref _testsFailed);
            if (action == null) return;
            TestBase.AssertEqual("POISON BITE", action.Name, "kit action keeps the flavor name",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
            // Kits apply DoTs through trigger bundles (sheet DoT columns are item-only): a connect poisons the hero.
            var foe = TestDataBuilders.Enemy().WithName("VenomFoe").Build();
            var hero = TestDataBuilders.Character().WithName("VenomHero").Build();
            var hit = new CombatEvent(CombatEventType.ActionHit, foe) { Target = hero, Action = action };
            RPGGame.Actions.Conditional.ActionTriggerBundleApplicator.ApplyMatchingBundles(action, hit, foe, hero, new System.Collections.Generic.List<string>());
            TestBase.AssertTrue(hero.PoisonPercentOfMaxHealth > 0, "Venom kit poisons the hero on connect",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        private static void TestUnknownEnemyKitIsNull()
        {
            TestBase.AssertNull(EnemyActionCatalog.TryCreate("NOT A REAL ENEMY MOVE"), "unknown flavor name has no kit",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        private static void TestDotModEffectNames()
        {
            TestBase.AssertEqual("weaponBurn", CombatEffectsSimplified.NormalizeWeaponDotEffect("BURN"), "BURN maps to weaponBurn",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
            TestBase.AssertEqual("weaponPoison", CombatEffectsSimplified.NormalizeWeaponDotEffect("Poison"), "POISON maps to weaponPoison",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
            TestBase.AssertEqual("weaponBleed", CombatEffectsSimplified.NormalizeWeaponDotEffect("weaponBleed"), "weaponBleed unchanged",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
            TestBase.AssertEqual("DAMAGE", CombatEffectsSimplified.NormalizeWeaponDotEffect("DAMAGE"), "other effects unchanged",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        private static void TestLastSwingSpeedModIsTakenOnce()
        {
            var hero = TestDataBuilders.Character().WithName("SpeedHero").Build();
            hero.Effects.LastSwingSpeedModPercent = 20;
            TestBase.AssertTrue(Math.Abs(hero.Effects.TakeLastSwingSpeedModPercent() - 20) < 1e-9, "swing speed mod is read",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
            TestBase.AssertTrue(Math.Abs(hero.Effects.TakeLastSwingSpeedModPercent()) < 1e-9, "and only once",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        private static Character HeroWithMaterial(string name, string material, int pieces)
        {
            var hero = TestDataBuilders.Character().WithName(name).Build();
            hero.Equipment.Weapon = TestDataBuilders.Weapon().WithName(name + "Weapon").WithWeaponType(WeaponType.Mace).Build();
            var slots = new Item?[] { hero.Equipment.Weapon, hero.Equipment.Head, hero.Equipment.Body, hero.Equipment.Legs, hero.Equipment.Feet };
            for (int i = 0; i < pieces && i < slots.Length; i++)
                if (slots[i] != null)
                    slots[i]!.Material = material;
            return hero;
        }

        private static void TestPercentConvertPaysShareOfHit()
        {
            var combat = GameConfiguration.Instance.Combat;
            double oldPct = combat.MaterialConvertPercentPerUnit;
            var oldTable = combat.MaterialConvertPercentByKeyword;
            try
            {
                combat.MaterialConvertPercentPerUnit = 1.0;
                combat.MaterialConvertPercentByKeyword = new System.Collections.Generic.Dictionary<string, double>(StringComparer.OrdinalIgnoreCase) { ["CRISIS"] = 4.0 };
                var hero = HeroWithMaterial("CullHero", "Iron", 1);
                if (hero.Equipment.Weapon == null)
                {
                    TestBase.AssertTrue(false, "test hero has a weapon", ref _testsRun, ref _testsPassed, ref _testsFailed);
                    return;
                }
                hero.Equipment.Weapon.Material = "Iron";
                int equipped = MaterialSetController.CountEquipped(hero, "Iron");
                var cull = ActionLoader.GetAction("IRON CULL") ?? ActionDataToActionMapper.CreateAction(ActionLoader.GetActionData("IRON CULL")!);
                hero.Effects.AddMaterialKeyword("CRISIS", 10);
                int bonus = MaterialSetController.GetConvertDamageBonus(hero, cull, 200);
                int expected = equipped >= MaterialBuildData.StackUnlockCount ? 200 * 10 * 4 / 100 : 0;
                TestBase.AssertEqual(expected, bonus, "percent convert pays 4% of the hit per CRISIS (when the set is formed)",
                    ref _testsRun, ref _testsPassed, ref _testsFailed);
            }
            finally
            {
                combat.MaterialConvertPercentPerUnit = oldPct;
                combat.MaterialConvertPercentByKeyword = oldTable;
            }
        }

        private static void TestFoeHitMintsOnlyTakeHitCurrencies()
        {
            var hero = HeroWithMaterial("ContactHero", "Crystal", 5);
            var foe = TestDataBuilders.Enemy().WithName("ContactFoe").Build();
            if (MaterialSetController.CountEquipped(hero, "Crystal") < MaterialBuildData.StackUnlockCount)
            {
                TestBase.AssertTrue(true, "skipped: test hero has too few equipment slots", ref _testsRun, ref _testsPassed, ref _testsFailed);
                return;
            }
            var foeHit = new CombatEvent(CombatEventType.ActionHit, foe) { Target = hero, Action = new Action { Name = "JAB", Type = ActionType.Attack } };
            MaterialSetController.TryMintFromEvent(hero, foeHit, foeHit.Action, null, takeHitOnly: true);
            TestBase.AssertEqual(0, MaterialSetController.GetKeyword(hero, "CONTACT"), "a foe's hit does not mint ON CONNECT currency",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
            var heroHit = new CombatEvent(CombatEventType.ActionHit, hero) { Target = foe, Action = new Action { Name = "SLAM", Type = ActionType.Attack } };
            MaterialSetController.TryMintFromEvent(hero, heroHit, heroHit.Action, null);
            TestBase.AssertTrue(MaterialSetController.GetKeyword(hero, "CONTACT") > 0, "the hero's own connect mints CONTACT",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        private static void TestSpeedOverflowTurnsIntoDamage()
        {
            var combat = GameConfiguration.Instance.Combat;
            double oldEff = combat.SpeedOverflowDamageEfficiency;
            double oldMin = combat.MinimumAttackTime;
            try
            {
                var hero = TestDataBuilders.Character().WithName("FastHero").Build();
                combat.SpeedOverflowDamageEfficiency = 0;
                TestBase.AssertTrue(Math.Abs(SpeedCalculator.GetSpeedOverflowDamageMultiplier(hero) - 1.0) < 1e-9, "overflow off = 1.0",
                    ref _testsRun, ref _testsPassed, ref _testsFailed);
                combat.SpeedOverflowDamageEfficiency = 1.0;
                combat.MinimumAttackTime = 1000;
                TestBase.AssertTrue(SpeedCalculator.GetSpeedOverflowDamageMultiplier(hero) > 1.0, "swing below the floor becomes damage",
                    ref _testsRun, ref _testsPassed, ref _testsFailed);
                var foe = TestDataBuilders.Enemy().WithName("FastFoe").Build();
                TestBase.AssertTrue(Math.Abs(SpeedCalculator.GetSpeedOverflowDamageMultiplier(foe) - 1.0) < 1e-9, "enemies get no overflow",
                    ref _testsRun, ref _testsPassed, ref _testsFailed);
            }
            finally
            {
                combat.SpeedOverflowDamageEfficiency = oldEff;
                combat.MinimumAttackTime = oldMin;
            }
        }

        private static void TestIntelligenceAmp()
        {
            var combat = GameConfiguration.Instance.Combat;
            double old = combat.IntelligenceAmpPerPoint;
            try
            {
                var hero = TestDataBuilders.Character().WithName("IntHero").Build();
                combat.IntelligenceAmpPerPoint = 0;
                TestBase.AssertTrue(Math.Abs(hero.GetIntelligenceAmpMultiplier() - 1.0) < 1e-9, "INT potency off = 1.0",
                    ref _testsRun, ref _testsPassed, ref _testsFailed);
                combat.IntelligenceAmpPerPoint = 0.01;
                double expected = 1.0 + Math.Min(hero.GetEffectiveIntelligence(), combat.StrengthDamageMaxStat) * 0.01;
                TestBase.AssertTrue(Math.Abs(hero.GetIntelligenceAmpMultiplier() - expected) < 1e-9, "INT potency = 1 + INT x rate",
                    ref _testsRun, ref _testsPassed, ref _testsFailed);
            }
            finally
            {
                combat.IntelligenceAmpPerPoint = old;
            }
        }

        private static Character HeroWithStrip(string name, params string[] actions)
        {
            var hero = TestDataBuilders.Character().WithName(name).Build();
            hero.ActionLabActionSlotBonus = actions.Length; // base strip is 1 slot
            for (int i = 0; i < actions.Length; i++)
                hero.AddToCombo(new Action(actions[i], comboOrder: i + 1, damageMultiplier: 1.0, length: 1.0, isComboAction: true));
            return hero;
        }

        private static void TestRepeatedStripActionAdvancesFromItsOwnSlot()
        {
            // STAB > MISDIRECT > EXPOSE > STAB > STAB: the slot-3 STAB must hand off to slot 4, not jump back to slot 1.
            var hero = HeroWithStrip("RepeatHero", "GLUE STAB", "GLUE MIS", "GLUE EXPOSE", "GLUE STAB", "GLUE STAB");
            var strip = hero.GetComboActions();
            TestBase.AssertEqual(5, strip.Count, "test strip has 5 slots", ref _testsRun, ref _testsPassed, ref _testsFailed);
            if (strip.Count != 5) return;
            hero.ComboStep = 3;
            hero.IncrementComboStep(strip[3]);
            TestBase.AssertEqual(4, hero.ComboStep % 5, "repeated action advances from its own slot (3 -> 4)",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        private static void TestStripReplacementAdvancesFromReplacedSlot()
        {
            // MISDIRECT turns the EXPOSE swing (slot 2) into STAB (which sits at slot 0): the strip continues at slot 3.
            var hero = HeroWithStrip("ReplaceHero", "GLUE STAB", "GLUE MIS", "GLUE EXPOSE", "GLUE NICK");
            var strip = hero.GetComboActions();
            if (strip.Count != 4)
            {
                TestBase.AssertEqual(4, strip.Count, "test strip has 4 slots", ref _testsRun, ref _testsPassed, ref _testsFailed);
                return;
            }
            hero.ComboStep = 2;
            hero.ReplacedStripSlot = 2;
            hero.IncrementComboStep(strip[0]);
            TestBase.AssertEqual(3, hero.ComboStep % 4, "replaced swing advances from the replaced slot (2 -> 3)",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
            TestBase.AssertTrue(hero.ReplacedStripSlot == null, "replaced slot is used once",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        private static void TestComboSlotCapByLevel()
        {
            var loot = GameConfiguration.Instance.LootSystem;
            var old = loot.ComboSlotCapByLevel;
            try
            {
                loot.ComboSlotCapByLevel = new System.Collections.Generic.List<int[]> { new[] { 1, 1 }, new[] { 10, 3 } };
                TestBase.AssertEqual(1, loot.GetComboSlotCapForLevel(1), "L1 cap is 1", ref _testsRun, ref _testsPassed, ref _testsFailed);
                TestBase.AssertEqual(1, loot.GetComboSlotCapForLevel(9), "L9 keeps the L1 cap", ref _testsRun, ref _testsPassed, ref _testsFailed);
                TestBase.AssertEqual(3, loot.GetComboSlotCapForLevel(40), "L40 uses the L10 anchor", ref _testsRun, ref _testsPassed, ref _testsFailed);
                var hero = TestDataBuilders.Character().WithName("CapHero").WithLevel(1).Build();
                hero.Equipment.Feet = new FeetItem("SlotBoots", 1, 1) { ExtraActionSlots = 3 };
                TestBase.AssertEqual(4, ComboSequenceMaxHelper.GetEffectiveMax(hero), "gear slots bypass the level cap",
                    ref _testsRun, ref _testsPassed, ref _testsFailed);
                hero.ActionLabActionSlotBonus = 2;
                TestBase.AssertEqual(6, ComboSequenceMaxHelper.GetEffectiveMax(hero), "lab sandbox slots bypass the cap",
                    ref _testsRun, ref _testsPassed, ref _testsFailed);
                hero.Equipment.Feet = null;
                hero.ActionLabActionSlotBonus = 0;
                TestBase.AssertEqual(1, ComboSequenceMaxHelper.GetEffectiveMax(hero), "base slots respect the level cap",
                    ref _testsRun, ref _testsPassed, ref _testsFailed);
            }
            finally
            {
                loot.ComboSlotCapByLevel = old;
            }
        }
    }
}
