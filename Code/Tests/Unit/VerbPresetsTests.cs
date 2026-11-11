using System;
using System.Collections.Generic;
using System.Linq;
using RPGGame;
using RPGGame.Data;
using RPGGame.Combat.Calculators;
using RPGGame.Tests;

namespace RPGGame.Tests.Unit
{
    /// <summary>Attunement (dominant material, ties → weapon) and the store-reading verb presets (CASH IN, DETONATE, LIQUIDATE, RECKONING, INDEX).</summary>
    public static class VerbPresetsTests
    {
        private static int _run, _passed, _failed;

        public static void RunAllTests()
        {
            Console.WriteLine("=== Verb Presets Tests ===\n");
            _run = _passed = _failed = 0;
            MaterialBuildsLoader.Reload();

            var combat = GameConfiguration.Instance.Combat;
            bool paysDamage = combat.MaterialConvertPaysDamage;
            var byKeyword = combat.MaterialConvertPercentByKeyword;
            double statusCash = combat.VerbStatusCashPercentPerUnit;
            double scaleCap = combat.VerbScaleMaxPercent;
            int maxUnits = combat.MaterialConvertMaxUnits;
            try
            {
                combat.MaterialConvertPercentByKeyword = new Dictionary<string, double>(StringComparer.OrdinalIgnoreCase)
                {
                    ["RAGE"] = 5, ["GRAZE"] = 4
                };
                combat.VerbStatusCashPercentPerUnit = 6;
                combat.VerbScaleMaxPercent = 100;
                combat.MaterialConvertMaxUnits = 0;

                TestAttunementDominantMaterial();
                TestAttunementTieGoesToWeapon();
                TestSpecParse();
                TestCashInAttuned(combat);
                TestReckoningScalesWithoutSpending();
                TestDetonateSpendsLargestStatus();
                TestLiquidateSpendsEveryMint();
                TestIndexCountsDistinctMaterials();
                TestDamageCalculatorAddsVerbBonus();
                TestApplyMark();
                TestBarrier();
                TestDistill();
                TestTransmute();
                TestInvestPaysBackLater();
                TestPalmBanksRoll();
                TestPresetsInData();
            }
            finally
            {
                combat.MaterialConvertPaysDamage = paysDamage;
                combat.MaterialConvertPercentByKeyword = byKeyword;
                combat.VerbStatusCashPercentPerUnit = statusCash;
                combat.VerbScaleMaxPercent = scaleCap;
                combat.MaterialConvertMaxUnits = maxUnits;
            }

            TestBase.PrintSummary("Verb Presets Tests", _run, _passed, _failed);
        }

        private static Character Hero(string name, string weapon, params string[] armor)
        {
            var hero = new Character(name, 10);
            hero.Equipment.Weapon = new WeaponItem("Blade", 1, 10, 1.0, WeaponType.Sword) { Material = weapon };
            if (armor.Length > 0) hero.Equipment.Head = new HeadItem("Cap", 1, 1) { Material = armor[0] };
            if (armor.Length > 1) hero.Equipment.Body = new ChestItem("Coat", 1, 1) { Material = armor[1] };
            if (armor.Length > 2) hero.Equipment.Legs = new LegsItem("Pants", 1, 1) { Material = armor[2] };
            if (armor.Length > 3) hero.Equipment.Feet = new FeetItem("Boots", 1, 1) { Material = armor[3] };
            return hero;
        }

        private static Action Verb(string name, string spec) =>
            new() { Name = name, Verb = spec, DamageMultiplier = 1.0, Type = ActionType.Attack };

        private static void TestAttunementDominantMaterial()
        {
            TestBase.SetCurrentTestName(nameof(TestAttunementDominantMaterial));
            var hero = Hero("AttuneHero", "Bone", "Glass", "Glass", "Glass");
            TestBase.AssertEqual("Glass", Attunement.GetMaterial(hero) ?? "", "3 Glass beats 1 Bone weapon", ref _run, ref _passed, ref _failed);
            TestBase.AssertEqual("GRAZE", Attunement.GetKeyword(hero) ?? "", "attuned keyword is the material's mint", ref _run, ref _passed, ref _failed);
            TestBase.AssertNull(Attunement.GetMaterial(new Character("Naked", 1)), "no gear, no attunement", ref _run, ref _passed, ref _failed);
        }

        private static void TestAttunementTieGoesToWeapon()
        {
            TestBase.SetCurrentTestName(nameof(TestAttunementTieGoesToWeapon));
            var hero = Hero("TieHero", "Bone", "Glass", "Glass", "Bone");
            TestBase.AssertEqual("Bone", Attunement.GetMaterial(hero) ?? "", "2-2 tie goes to the weapon's material", ref _run, ref _passed, ref _failed);
        }

        private static void TestSpecParse()
        {
            TestBase.SetCurrentTestName(nameof(TestSpecParse));
            var cash = VerbSpec.Parse("CASH ATTUNED 75");
            TestBase.AssertTrue(cash != null && cash.Kind == VerbKind.Cash && cash.Store == "ATTUNED" && cash.Rate == 75, "CASH ATTUNED 75", ref _run, ref _passed, ref _failed);
            var det = VerbSpec.Parse("detonate");
            TestBase.AssertTrue(det != null && det.Kind == VerbKind.Detonate && det.Rate == 100, "verb only: rate 100", ref _run, ref _passed, ref _failed);
            var cashBleed = VerbSpec.Parse("CASH bleed 50%");
            TestBase.AssertTrue(cashBleed != null && cashBleed.Store == "bleed" && cashBleed.Rate == 50, "named store, % suffix", ref _run, ref _passed, ref _failed);
            TestBase.AssertNull(VerbSpec.Parse("SHOUT 3"), "unknown verb is null", ref _run, ref _passed, ref _failed);
            TestBase.AssertNull(VerbSpec.Parse(""), "empty is null", ref _run, ref _passed, ref _failed);
        }

        private static void TestCashInAttuned(CombatConfig combat)
        {
            TestBase.SetCurrentTestName(nameof(TestCashInAttuned));
            var cashIn = Verb("CASH IN", "CASH ATTUNED 75");
            var foe = new Character("CashFoe", 5);

            combat.MaterialConvertPaysDamage = true;
            var hero = Hero("CashHero", "Bone", "Bone");
            hero.Effects.AddMaterialKeyword("RAGE", 4);
            double pct = VerbPresets.GetDamagePercent(hero, foe, VerbPresets.GetSpec(cashIn)!);
            TestBase.AssertTrue(Math.Abs(pct - 15) < 1e-9, $"4 RAGE x 5% x 75% = 15% (got {pct})", ref _run, ref _passed, ref _failed);
            TestBase.AssertEqual(4, VerbPresets.OnConnect(hero, foe, cashIn), "connect spends the attuned bank", ref _run, ref _passed, ref _failed);
            TestBase.AssertEqual(0, hero.Effects.GetMaterialKeyword("RAGE"), "RAGE is empty after cashing", ref _run, ref _passed, ref _failed);

            combat.MaterialConvertPaysDamage = false;
            var statHero = Hero("CashStatHero", "Bone", "Bone");
            statHero.Effects.AddMaterialKeyword("RAGE", 4);
            double off = VerbPresets.GetDamagePercent(statHero, foe, VerbPresets.GetSpec(cashIn)!);
            TestBase.AssertEqual(0.0, off, "mint cash pays no damage when the patch turns convert damage off", ref _run, ref _passed, ref _failed);
            TestBase.AssertEqual(4, VerbPresets.OnConnect(statHero, foe, cashIn), "still spends (pays the stat convert)", ref _run, ref _passed, ref _failed);

            var mixed = Hero("CashGlassHero", "Glass", "Glass", "Bone");
            mixed.Effects.AddMaterialKeyword("RAGE", 3);
            mixed.Effects.AddMaterialKeyword("GRAZE", 2);
            VerbPresets.OnConnect(mixed, foe, cashIn);
            TestBase.AssertEqual(0, mixed.Effects.GetMaterialKeyword("GRAZE"), "Glass-attuned CASH IN spends GRAZE", ref _run, ref _passed, ref _failed);
            TestBase.AssertEqual(3, mixed.Effects.GetMaterialKeyword("RAGE"), "and leaves RAGE alone", ref _run, ref _passed, ref _failed);
        }

        private static void TestReckoningScalesWithoutSpending()
        {
            TestBase.SetCurrentTestName(nameof(TestReckoningScalesWithoutSpending));
            var reckoning = Verb("RECKONING", "SCALE ATTUNED 50");
            var hero = Hero("ReckonHero", "Bone", "Bone");
            hero.Effects.AddMaterialKeyword("RAGE", 4);
            var spec = VerbPresets.GetSpec(reckoning)!;
            double pct = VerbPresets.GetDamagePercent(hero, null, spec);
            TestBase.AssertTrue(Math.Abs(pct - 10) < 1e-9, $"4 RAGE x 5% x 50% = 10% (got {pct})", ref _run, ref _passed, ref _failed);
            TestBase.AssertEqual(0, VerbPresets.OnConnect(hero, null, reckoning), "SCALE spends nothing", ref _run, ref _passed, ref _failed);
            TestBase.AssertEqual(4, hero.Effects.GetMaterialKeyword("RAGE"), "RAGE kept", ref _run, ref _passed, ref _failed);
            hero.Effects.AddMaterialKeyword("RAGE", 1000);
            TestBase.AssertEqual(100.0, VerbPresets.GetDamagePercent(hero, null, spec), "capped at VerbScaleMaxPercent", ref _run, ref _passed, ref _failed);
        }

        private static void TestDetonateSpendsLargestStatus()
        {
            TestBase.SetCurrentTestName(nameof(TestDetonateSpendsLargestStatus));
            var detonate = Verb("DETONATE", "DETONATE 100");
            var hero = Hero("DetHero", "Bone");
            var foe = new Character("DetFoe", 5);
            ExposeStatus.Apply(foe, 2, 3);
            foe.BleedIntensity = 5;
            TestBase.AssertEqual("bleed", VerbPresets.LargestTargetStatus(foe) ?? "", "bleed 5 beats expose 2", ref _run, ref _passed, ref _failed);
            double pct = VerbPresets.GetDamagePercent(hero, foe, VerbPresets.GetSpec(detonate)!);
            TestBase.AssertTrue(Math.Abs(pct - 30) < 1e-9, $"5 bleed x 6% = 30% (got {pct})", ref _run, ref _passed, ref _failed);
            TestBase.AssertEqual(5, VerbPresets.OnConnect(hero, foe, detonate), "spends the bleed", ref _run, ref _passed, ref _failed);
            TestBase.AssertEqual(0, foe.BleedIntensity, "bleed gone", ref _run, ref _passed, ref _failed);
            TestBase.AssertEqual(2, foe.ExposeStacks ?? 0, "expose untouched", ref _run, ref _passed, ref _failed);
            TestBase.AssertEqual(0.0, VerbPresets.GetDamagePercent(hero, new Character("CleanFoe", 5), VerbPresets.GetSpec(detonate)!),
                "nothing to detonate, no bonus", ref _run, ref _passed, ref _failed);
        }

        private static void TestLiquidateSpendsEveryMint()
        {
            TestBase.SetCurrentTestName(nameof(TestLiquidateSpendsEveryMint));
            var liquidate = Verb("LIQUIDATE", "LIQUIDATE 50");
            var hero = Hero("LiqHero", "Bone", "Glass");
            hero.Effects.AddMaterialKeyword("RAGE", 2);
            hero.Effects.AddMaterialKeyword("GRAZE", 3);
            TestBase.AssertEqual(5, VerbPresets.OnConnect(hero, null, liquidate), "spends RAGE and GRAZE", ref _run, ref _passed, ref _failed);
            TestBase.AssertEqual(0, hero.Effects.GetMaterialKeyword("RAGE") + hero.Effects.GetMaterialKeyword("GRAZE"), "both empty", ref _run, ref _passed, ref _failed);
        }

        private static void TestIndexCountsDistinctMaterials()
        {
            TestBase.SetCurrentTestName(nameof(TestIndexCountsDistinctMaterials));
            var index = Verb("INDEX", "INDEX 8");
            var hero = Hero("IndexHero", "Bone", "Glass", "Glass", "Iron");
            TestBase.AssertEqual(3, Attunement.CountDistinctMaterials(hero), "Bone, Glass, Iron", ref _run, ref _passed, ref _failed);
            TestBase.AssertEqual(24.0, VerbPresets.GetDamagePercent(hero, null, VerbPresets.GetSpec(index)!), "3 x 8%", ref _run, ref _passed, ref _failed);
        }

        private static void TestDamageCalculatorAddsVerbBonus()
        {
            TestBase.SetCurrentTestName(nameof(TestDamageCalculatorAddsVerbBonus));
            var hero = Hero("CalcHero", "Bone", "Glass", "Iron");
            var foe = new Character("CalcFoe", 1);
            var plain = new Action { Name = "PLAIN", DamageMultiplier = 1.0, Type = ActionType.Attack };
            var index = Verb("INDEX", "INDEX 50");
            int a = DamageCalculator.CalculateDamage(hero, foe, plain, 1.0, 1.0, 0, 10, showWeakenedMessage: false);
            int b = DamageCalculator.CalculateDamage(hero, foe, index, 1.0, 1.0, 0, 10, showWeakenedMessage: false);
            TestBase.AssertTrue(b > a, $"INDEX hits harder than the same action without a verb ({b} > {a})", ref _run, ref _passed, ref _failed);
        }

        private static void TestApplyMark()
        {
            TestBase.SetCurrentTestName(nameof(TestApplyMark));
            var spec = VerbSpec.Parse("APPLY mark");
            TestBase.AssertTrue(spec != null && spec.Rate == 1, "APPLY defaults to 1 stack", ref _run, ref _passed, ref _failed);
            var hero = Hero("MarkHero", "Bone");
            var foe = new Character("MarkFoe", 5);
            VerbPresets.OnConnect(hero, foe, Verb("MARK", "APPLY mark 1"));
            TestBase.AssertTrue(foe.IsMarked, "target is marked", ref _run, ref _passed, ref _failed);
            TestBase.AssertFalse(hero.IsMarked, "hero is not", ref _run, ref _passed, ref _failed);
        }

        private static void TestBarrier()
        {
            TestBase.SetCurrentTestName(nameof(TestBarrier));
            var hero = Hero("BarrierHero", "Bone");
            int expected = (int)Math.Round(hero.GetEffectiveMaxHealth() * 0.08);
            VerbPresets.OnConnect(hero, null, Verb("BARRIER", "BARRIER 8"));
            TestBase.AssertEqual(Math.Max(1, expected), hero.TemporaryHP ?? 0, "temp HP = 8% max HP", ref _run, ref _passed, ref _failed);
        }

        private static void TestDistill()
        {
            TestBase.SetCurrentTestName(nameof(TestDistill));
            var hero = Hero("DistillHero", "Glass", "Glass");
            var foe = new Character("DistillFoe", 5);
            foe.BurnIntensity = 6;
            TestBase.AssertEqual(6, VerbPresets.OnConnect(hero, foe, Verb("DISTILL", "DISTILL 50")), "spends 6 burn", ref _run, ref _passed, ref _failed);
            TestBase.AssertEqual(0, foe.BurnIntensity, "burn gone", ref _run, ref _passed, ref _failed);
            TestBase.AssertEqual(3, hero.Effects.GetMaterialKeyword("GRAZE"), "3 GRAZE (50%) into the Glass mint", ref _run, ref _passed, ref _failed);
        }

        private static void TestTransmute()
        {
            TestBase.SetCurrentTestName(nameof(TestTransmute));
            var hero = Hero("TransHero", "Bone", "Bone", "Glass");
            hero.Effects.AddMaterialKeyword("RAGE", 2);
            hero.Effects.AddMaterialKeyword("GRAZE", 4);
            TestBase.AssertEqual(4, VerbPresets.OnConnect(hero, null, Verb("TRANSMUTE", "TRANSMUTE 75")), "spends the 4 GRAZE", ref _run, ref _passed, ref _failed);
            TestBase.AssertEqual(5, hero.Effects.GetMaterialKeyword("RAGE"), "2 + floor(4 x 0.75) = 5 RAGE", ref _run, ref _passed, ref _failed);
            TestBase.AssertEqual(0, hero.Effects.GetMaterialKeyword("GRAZE"), "GRAZE empty", ref _run, ref _passed, ref _failed);
        }

        private static void TestInvestPaysBackLater()
        {
            TestBase.SetCurrentTestName(nameof(TestInvestPaysBackLater));
            var spec = VerbSpec.Parse("INVEST ATTUNED 200 2");
            TestBase.AssertTrue(spec != null && spec.Rate == 200 && spec.Delay == 2, "rate then delay", ref _run, ref _passed, ref _failed);
            var hero = Hero("InvestHero", "Bone", "Bone");
            hero.Effects.AddMaterialKeyword("RAGE", 3);
            VerbPresets.OnConnect(hero, null, Verb("INVEST", "INVEST ATTUNED 200 2"));
            TestBase.AssertEqual(0, hero.Effects.GetMaterialKeyword("RAGE"), "RAGE spent now", ref _run, ref _passed, ref _failed);
            TestBase.AssertEqual(6, VerbPresets.GetPendingInvestment(hero, "RAGE"), "6 pending", ref _run, ref _passed, ref _failed);
            VerbPresets.TickInvestments(hero); // the INVEST action itself
            VerbPresets.TickInvestments(hero);
            TestBase.AssertEqual(0, hero.Effects.GetMaterialKeyword("RAGE"), "not yet after 1 more action", ref _run, ref _passed, ref _failed);
            TestBase.AssertEqual(6, VerbPresets.TickInvestments(hero), "pays 6 after 2 more actions", ref _run, ref _passed, ref _failed);
            TestBase.AssertEqual(6, hero.Effects.GetMaterialKeyword("RAGE"), "RAGE back doubled", ref _run, ref _passed, ref _failed);
            TestBase.AssertEqual(0, VerbPresets.GetPendingInvestment(hero, "RAGE"), "nothing pending", ref _run, ref _passed, ref _failed);
        }

        private static void TestPalmBanksRoll()
        {
            TestBase.SetCurrentTestName(nameof(TestPalmBanksRoll));
            var hero = Hero("PalmHero", "Bone");
            VerbPresets.OnConnect(hero, null, Verb("PALM", "PALM"), naturalRoll: 17);
            bool banked = RPGGame.Actions.Conditional.CombatTriggerContext.TryConsumePendingReplaceRollFace(hero, out int face);
            TestBase.AssertTrue(banked && face == 17, $"next roll becomes the palmed 17 (got {face})", ref _run, ref _passed, ref _failed);
        }

        private static void TestPresetsInData()
        {
            TestBase.SetCurrentTestName(nameof(TestPresetsInData));
            foreach (string name in new[] { "CASH IN", "DETONATE", "LIQUIDATE", "RECKONING", "INDEX",
                         "MARK", "BARRIER", "DISTILL", "TRANSMUTE", "INVEST", "PALM" })
            {
                var data = ActionLoader.GetActionData(name);
                TestBase.AssertTrue(data != null && VerbSpec.Parse(data.Verb) != null, $"{name} loads with a valid verb", ref _run, ref _passed, ref _failed);
                TestBase.AssertTrue(data != null && data.Tier == 4, $"{name} is tier 4 (outside the default active set)", ref _run, ref _passed, ref _failed);
            }
        }
    }
}
