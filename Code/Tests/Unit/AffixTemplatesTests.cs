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
            TestSlotPrefixOnlyOnItsSlot();
            TestSlotPrefixesRollAndRead();

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
            hero.Equipment.Weapon!.Modifications.RemoveAll(m => AffixTemplates.Is(m, AffixTemplates.CountsDouble));
            hero.Equipment.Weapon!.Modifications.Add(Mod(AffixTemplates.CountsDouble, 2));
            TestBase.AssertEqual(3, MaterialSetController.CountEquipped(hero, "Bone"), "Redoubled (2) counts the piece three times", ref _run, ref _passed, ref _failed);
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

        /// <summary>A hero with a <paramref name="slots"/>-action strip and armor to hang prefixes on.</summary>
        private static (Character hero, System.Collections.Generic.List<RPGGame.Action> strip) StripHero(string name, int slots)
        {
            var hero = TestDataBuilders.Character().WithName(name).WithStats(10, 10, 10, 10).Build();
            hero.Equipment.Body = new ChestItem("Coat", 1, 1) { Material = "Bone" };
            for (int i = 0; i < slots; i++)
            {
                var a = TestDataBuilders.CreateMockAction($"{name} SLOT {i + 1}", ActionType.Attack);
                a.IsComboAction = true;
                hero.AddAction(a, 1.0);
                hero.Actions.AddToCombo(a);
            }
            return (hero, ActionUtilities.GetComboActions(hero));
        }

        /// <summary>Item 14 (2026-11-21): a slot prefix (<c>WHILE IF SLOT=n -> DAMAGE +x%</c>) applies on that slot only.</summary>
        private static void TestSlotPrefixOnlyOnItsSlot()
        {
            TestBase.SetCurrentTestName(nameof(TestSlotPrefixOnlyOnItsSlot));
            var (hero, strip) = StripHero("SlotHero", 4);
            if (strip.Count < 4)
            {
                TestBase.AssertTrue(false, $"strip has 4 slots (got {strip.Count})", ref _run, ref _passed, ref _failed);
                return;
            }
            hero.Equipment.Body!.Modifications.Add(new Modification
            {
                Name = "Follow-Through", Effect = CrossWires.RuleEffect, CrossWire = "WHILE IF SLOT=2 -> DAMAGE +{n}%",
                MinValue = 15, MaxValue = 15, RolledValue = 15
            });
            TestBase.AssertEqual(0.0, AffixTemplates.OutgoingDamagePercent(hero, strip[0]), "slot 1: no slot-2 bonus", ref _run, ref _passed, ref _failed);
            TestBase.AssertEqual(15.0, AffixTemplates.OutgoingDamagePercent(hero, strip[1]), "slot 2: +15%", ref _run, ref _passed, ref _failed);
            TestBase.AssertEqual(0.0, AffixTemplates.OutgoingDamagePercent(hero, strip[2]), "slot 3: no slot-2 bonus", ref _run, ref _passed, ref _failed);

            hero.Equipment.Body!.Modifications.Add(new Modification
            {
                Name = "Late-Blooming", Effect = CrossWires.RuleEffect, CrossWire = "WHILE IF SLOT>=4 -> DAMAGE +{n}%",
                MinValue = 10, MaxValue = 10, RolledValue = 10
            });
            TestBase.AssertEqual(10.0, AffixTemplates.OutgoingDamagePercent(hero, strip[3]), "slot 4: +10% from slot 4+", ref _run, ref _passed, ref _failed);
            TestBase.AssertEqual(15.0, AffixTemplates.OutgoingDamagePercent(hero, strip[1]), "slot 2 still only the slot-2 line", ref _run, ref _passed, ref _failed);

            var crit = CrossWireRule.Parse("WHILE IF SLOT=2 -> CRIT +1", "test");
            TestBase.AssertTrue(crit != null && CrossWires.ConditionsHold(hero, null, crit, action: strip[1])
                && !CrossWires.ConditionsHold(hero, null, crit, action: strip[2]), "slot-gated crit holds on slot 2 only", ref _run, ref _passed, ref _failed);
        }

        /// <summary>The slot families are in ItemMods.json, roll as prefixes and read as plain card text.</summary>
        private static void TestSlotPrefixesRollAndRead()
        {
            TestBase.SetCurrentTestName(nameof(TestSlotPrefixesRollAndRead));
            var cache = LootDataCache.Load();
            foreach (string name in new[] { "Follow-Through", "Third-Strike", "Late-Blooming", "Sighting", "Steadied" })
            {
                var family = cache.ItemMods.Find(name);
                TestBase.AssertTrue(family != null && family.Pool == RPGGame.Data.ItemGeneration.ModPools.Material && family.Tiers.Count == 3,
                    $"{name}: a 3-tier prefix family", ref _run, ref _passed, ref _failed);
                var template = cache.Modifications.FirstOrDefault(m => m.Name == name);
                TestBase.AssertTrue(template != null && CrossWires.RulesOf(template, name).Any(), $"{name}: its rule parses", ref _run, ref _passed, ref _failed);
            }
            var mod = new Modification
            {
                Name = "Following", Effect = CrossWires.RuleEffect, CrossWire = "WHILE IF SLOT=2 -> DAMAGE +{n}%", RolledValue = 12
            };
            TestBase.AssertEqual("+12% damage on strip slot 2", CrossWires.DescribeMod(mod) ?? "", "slot line reads plainly", ref _run, ref _passed, ref _failed);
            var late = new Modification { Name = "Patient", Effect = CrossWires.RuleEffect, CrossWire = "WHILE IF SLOT>=4 -> DAMAGE +{n}%", RolledValue = 9 };
            TestBase.AssertEqual("+9% damage on strip slot 4 and later", CrossWires.DescribeMod(late) ?? "", "slot 4+ line reads plainly", ref _run, ref _passed, ref _failed);
        }
    }
}
