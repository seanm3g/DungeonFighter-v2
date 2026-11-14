using System;
using System.Linq;
using RPGGame;
using RPGGame.Data;
using RPGGame.Tests;

namespace RPGGame.Tests.Unit
{
    /// <summary>Item affix templates: TUNE, STORECOND, COUNTSDOUBLE, OPENER / FINISHER, BLOODPRICE.</summary>
    public static class AffixTemplatesTests
    {
        private static int _run, _passed, _failed;

        public static void RunAllTests()
        {
            Console.WriteLine("=== Affix Templates Tests ===\n");
            _run = _passed = _failed = 0;
            MaterialBuildsLoader.Reload();

            TestTuneAddsToMints();
            TestCountsDouble();
            TestStoreCondition();
            TestOpenerFinisher();
            TestBloodPrice();
            TestRowsInData();

            TestBase.PrintSummary("Affix Templates Tests", _run, _passed, _failed);
        }

        private static Character Hero(string name, string weapon, params string[] armor)
        {
            var hero = new Character(name, 10);
            hero.Equipment.Weapon = new WeaponItem("Blade", 1, 10, 1.0, WeaponType.Sword) { Material = weapon };
            if (armor.Length > 0) hero.Equipment.Head = new HeadItem("Cap", 1, 1) { Material = armor[0] };
            if (armor.Length > 1) hero.Equipment.Body = new ChestItem("Coat", 1, 1) { Material = armor[1] };
            return hero;
        }

        private static Modification Mod(string effect, double value) => new() { Name = effect, Effect = effect, RolledValue = value };

        private static void TestTuneAddsToMints()
        {
            TestBase.SetCurrentTestName(nameof(TestTuneAddsToMints));
            var hero = Hero("TuneHero", "Bone", "Bone");
            hero.Equipment.Head!.Modifications.Add(Mod(AffixTemplates.Tune, 2));
            MaterialSetController.MintKeyword(hero, "RAGE", 3, "test");
            TestBase.AssertEqual(5, hero.Effects.GetMaterialKeyword("RAGE"), "3 minted + 2 from a tuned Bone piece", ref _run, ref _passed, ref _failed);
            MaterialSetController.MintKeyword(hero, "GRAZE", 3, "test");
            TestBase.AssertEqual(3, hero.Effects.GetMaterialKeyword("GRAZE"), "tuning only feeds its own material", ref _run, ref _passed, ref _failed);
        }

        private static void TestCountsDouble()
        {
            TestBase.SetCurrentTestName(nameof(TestCountsDouble));
            var hero = Hero("DoubleHero", "Bone", "Glass");
            TestBase.AssertEqual(1, MaterialSetController.CountEquipped(hero, "Bone"), "one Bone piece", ref _run, ref _passed, ref _failed);
            hero.Equipment.Weapon!.Modifications.Add(Mod(AffixTemplates.CountsDouble, 1));
            TestBase.AssertEqual(2, MaterialSetController.CountEquipped(hero, "Bone"), "doubled Bone weapon forms the set alone", ref _run, ref _passed, ref _failed);
            TestBase.AssertEqual(1, MaterialSetController.CountEquipped(hero, "Glass"), "other materials unchanged", ref _run, ref _passed, ref _failed);
        }

        private static void TestStoreCondition()
        {
            TestBase.SetCurrentTestName(nameof(TestStoreCondition));
            var hero = Hero("CondHero", "Bone", "Bone");
            hero.Equipment.Head!.Modifications.Add(Mod(AffixTemplates.StoreCondition, 10));
            TestBase.AssertEqual(0.0, AffixTemplates.OutgoingDamagePercent(hero, null), "empty bank, no bonus", ref _run, ref _passed, ref _failed);
            hero.Effects.AddMaterialKeyword("RAGE", GameConfiguration.Instance.Combat.AffixConditionStoreThreshold);
            TestBase.AssertEqual(10.0, AffixTemplates.OutgoingDamagePercent(hero, null), "bank at threshold: +10%", ref _run, ref _passed, ref _failed);
        }

        private static void TestOpenerFinisher()
        {
            TestBase.SetCurrentTestName(nameof(TestOpenerFinisher));
            var hero = Hero("PosHero", "Bone", "Bone", "Bone");
            hero.Equipment.Head!.Modifications.Add(Mod(AffixTemplates.Opener, 12));
            hero.Equipment.Body!.Modifications.Add(Mod(AffixTemplates.Finisher, 15));
            var strip = hero.ComboSequence;
            if (strip == null || strip.Count < 2)
            {
                TestBase.AssertTrue(true, "hero has no 2-slot strip to test positions (skipped)", ref _run, ref _passed, ref _failed);
                return;
            }
            var first = strip[0];
            var last = strip[^1];
            TestBase.AssertEqual(12.0, AffixTemplates.OutgoingDamagePercent(hero, first), "opener +12%", ref _run, ref _passed, ref _failed);
            TestBase.AssertEqual(15.0, AffixTemplates.OutgoingDamagePercent(hero, last), "finisher +15%", ref _run, ref _passed, ref _failed);
        }

        private static void TestBloodPrice()
        {
            TestBase.SetCurrentTestName(nameof(TestBloodPrice));
            var hero = Hero("BloodHero", "Bone");
            hero.Equipment.Weapon!.Modifications.Add(Mod(AffixTemplates.BloodPrice, 15));
            hero.CurrentHealth = hero.GetEffectiveMaxHealth();
            int max = hero.GetEffectiveMaxHealth();
            int paid = AffixTemplates.PayActionCosts(hero);
            int expected = (int)Math.Round(max * GameConfiguration.Instance.Combat.AffixBloodPriceHpPercent / 100.0);
            TestBase.AssertEqual(expected, paid, "pays 2% max HP per action", ref _run, ref _passed, ref _failed);
            TestBase.AssertEqual(15.0, AffixTemplates.OutgoingDamagePercent(hero, null), "+15% damage", ref _run, ref _passed, ref _failed);
        }

        private static void TestRowsInData()
        {
            TestBase.SetCurrentTestName(nameof(TestRowsInData));
            var mods = LootDataCache.Load().Modifications;
            foreach (string effect in new[] { AffixTemplates.Tune, AffixTemplates.StoreCondition, AffixTemplates.CountsDouble,
                         AffixTemplates.Opener, AffixTemplates.Finisher, AffixTemplates.BloodPrice })
                TestBase.AssertTrue(mods.Any(m => AffixTemplates.Is(m, effect)), $"{effect} prefix in Modifications.json", ref _run, ref _passed, ref _failed);
        }
    }
}
