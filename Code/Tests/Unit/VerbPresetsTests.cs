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
            // Step 5: these tests cover the retired statuses' code, which stays until step 12 (CoreStatuses).
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

                TestFullestStore();
                TestSpecParse();
                TestCashInFullest(combat);
                TestDetonateSpendsLargestStatus();
                TestDamageCalculatorAddsVerbBonus(combat);
                TestStatusActions();
                TestRetiredVerbsLoadAsReplacements();
                TestCashIntoParse();
                TestCashIntoPayouts(combat);
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

        private static void TestFullestStore()
        {
            TestBase.SetCurrentTestName(nameof(TestFullestStore));
            var hero = Hero("FullHero", "Bone", "Glass", "Glass", "Glass");
            TestBase.AssertNull(FullestStore.GetKeyword(hero), "every store empty: nothing to cash", ref _run, ref _passed, ref _failed);
            hero.Effects.AddMaterialKeyword("RAGE", 3);
            hero.Effects.AddMaterialKeyword("GRAZE", 2);
            TestBase.AssertEqual("RAGE", FullestStore.GetKeyword(hero) ?? "", "3 RAGE beats 2 GRAZE, whatever you wear most",
                ref _run, ref _passed, ref _failed);
            TestBase.AssertEqual("Bone", FullestStore.GetMaterial(hero) ?? "", "its material", ref _run, ref _passed, ref _failed);
            TestBase.AssertEqual(2, FullestStore.CountDistinctMaterials(hero), "Bone, Glass", ref _run, ref _passed, ref _failed);
        }

        private static void TestSpecParse()
        {
            TestBase.SetCurrentTestName(nameof(TestSpecParse));
            var cash = VerbSpec.Parse("CASH FULLEST 75");
            TestBase.AssertTrue(cash != null && cash.Kind == VerbKind.Cash && cash.Store == "FULLEST" && cash.Rate == 75, "CASH FULLEST 75", ref _run, ref _passed, ref _failed);
            TestBase.AssertEqual("FULLEST", VerbSpec.Parse("CASH ATTUNED 75")?.Store ?? "", "ATTUNED reads as FULLEST", ref _run, ref _passed, ref _failed);
            foreach (string retired in new[] { "LIQUIDATE 50", "SCALE ATTUNED 50", "INDEX 8", "BARRIER 8", "DISTILL 50", "TRANSMUTE 75", "INVEST ATTUNED 200 2", "PALM" })
                TestBase.AssertNull(VerbSpec.Parse(retired), $"retired verb {retired} no longer parses", ref _run, ref _passed, ref _failed);
            var det = VerbSpec.Parse("detonate");
            TestBase.AssertTrue(det != null && det.Kind == VerbKind.Detonate && det.Rate == 100, "verb only: rate 100", ref _run, ref _passed, ref _failed);
            var cashBleed = VerbSpec.Parse("CASH bleed 50%");
            TestBase.AssertTrue(cashBleed != null && cashBleed.Store == "bleed" && cashBleed.Rate == 50, "named store, % suffix", ref _run, ref _passed, ref _failed);
            TestBase.AssertNull(VerbSpec.Parse("SHOUT 3"), "unknown verb is null", ref _run, ref _passed, ref _failed);
            TestBase.AssertNull(VerbSpec.Parse(""), "empty is null", ref _run, ref _passed, ref _failed);
        }

        private static void TestCashInFullest(CombatConfig combat)
        {
            TestBase.SetCurrentTestName(nameof(TestCashInFullest));
            var cashIn = Verb("CASH IN", "CASH FULLEST 75");
            var foe = new Character("CashFoe", 5);

            combat.MaterialConvertPaysDamage = true;
            var hero = Hero("CashHero", "Bone", "Bone");
            hero.Effects.AddMaterialKeyword("RAGE", 4);
            double pct = VerbPresets.GetDamagePercent(hero, foe, VerbPresets.GetSpec(cashIn)!);
            TestBase.AssertTrue(Math.Abs(pct - 15) < 1e-9, $"4 RAGE x 5% x 75% = 15% (got {pct})", ref _run, ref _passed, ref _failed);
            TestBase.AssertEqual(4, VerbPresets.OnConnect(hero, foe, cashIn), "connect spends the fullest bank", ref _run, ref _passed, ref _failed);
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
            TestBase.AssertEqual(0, mixed.Effects.GetMaterialKeyword("RAGE"), "CASH IN spends the fullest store (RAGE 3) though Glass is worn most", ref _run, ref _passed, ref _failed);
            TestBase.AssertEqual(2, mixed.Effects.GetMaterialKeyword("GRAZE"), "and leaves GRAZE alone", ref _run, ref _passed, ref _failed);
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

        private static void TestDamageCalculatorAddsVerbBonus(CombatConfig combat)
        {
            TestBase.SetCurrentTestName(nameof(TestDamageCalculatorAddsVerbBonus));
            combat.MaterialConvertPaysDamage = true;
            var hero = Hero("CalcHero", "Bone", "Bone", "Iron");
            hero.Effects.AddMaterialKeyword("RAGE", 10);
            var foe = new Character("CalcFoe", 1);
            var plain = new Action { Name = "PLAIN", DamageMultiplier = 1.0, Type = ActionType.Attack };
            var cash = Verb("CASH IN", "CASH FULLEST 100");
            int a = DamageCalculator.CalculateDamage(hero, foe, plain, 1.0, 1.0, 0, 10, showWeakenedMessage: false);
            int b = DamageCalculator.CalculateDamage(hero, foe, cash, 1.0, 1.0, 0, 10, showWeakenedMessage: false);
            TestBase.AssertTrue(b > a, $"CASH IN hits harder than the same action without a verb ({b} > {a})", ref _run, ref _passed, ref _failed);
        }

        /// <summary>A connect with the card: its APPLY verb runs as the card's rule (ActionRules).</summary>
        private static void Connect(Character hero, Actor foe, Action action) =>
            RPGGame.Combat.Rules.ActionRules.Dispatch(action,
                new RPGGame.Combat.Events.CombatEvent(RPGGame.Combat.Events.CombatEventType.ActionHit, hero) { Target = foe, Action = action },
                hero, foe, new List<string>());

        private static void TestRetiredVerbsLoadAsReplacements()
        {
            TestBase.SetCurrentTestName(nameof(TestRetiredVerbsLoadAsReplacements));
            foreach (var (old, now) in new[] { ("LIQUIDATE", "CASH IN"), ("RECKONING", "CASH IN"), ("INDEX", "CASH IN"), ("MARK", "CASH IN"),
                         ("TRANSMUTE", "CASH IN"), ("INVEST", "CASH IN"), ("PALM", "CASH IN"), ("BARRIER", "BULWARK"), ("DISTILL", "DETONATE") })
                TestBase.AssertEqual(now, ActionLoader.ResolveActionName(old) ?? "", $"a saved {old} loads as {now}", ref _run, ref _passed, ref _failed);
        }

        private static void TestStatusActions()
        {
            TestBase.SetCurrentTestName(nameof(TestStatusActions));
            var hero = Hero("StatusHero", "Bone");
            var foe = new Character("StatusFoe", 5);
            var venomFoe = new Character("VenomFoe", 5);
            Connect(hero, venomFoe, Verb("VENOM COAT", "APPLY poison 6"));
            TestBase.AssertEqual(6, venomFoe.PendingBleedFromHits, "VENOM COAT: poison lands as 6 bleed", ref _run, ref _passed, ref _failed);
            Connect(hero, foe, Verb("OPEN VEIN", "APPLY bleed 2"));
            TestBase.AssertEqual(2, foe.PendingBleedFromHits, "OPEN VEIN: 2 bleed queued", ref _run, ref _passed, ref _failed);
            Connect(hero, foe, Verb("STOKE", "APPLY burn 2"));
            TestBase.AssertEqual(2, foe.PendingBurnFromHits, "STOKE: 2 burn queued", ref _run, ref _passed, ref _failed);
            Connect(hero, foe, Verb("CORRODE", "APPLY acid 1"));
            TestBase.AssertTrue((foe.ExposeStacks ?? 0) >= 1, "CORRODE: acid lands as Expose", ref _run, ref _passed, ref _failed);
            Connect(hero, foe, Verb("DAZE", "APPLY stun 1"));
            TestBase.AssertTrue(foe.StaggerStacks >= 1, "DAZE: stun lands as Stagger", ref _run, ref _passed, ref _failed);
            Connect(hero, foe, Verb("BRACE", "APPLY fortify 2"));
            TestBase.AssertEqual(2, Stores.Get(hero, "fortify"), "BRACE: 2 Fortify on the hero", ref _run, ref _passed, ref _failed);
            TestBase.AssertEqual(0, Stores.Get(foe, "fortify"), "BRACE: not on the target", ref _run, ref _passed, ref _failed);
            Stores.Add(foe, "weaken", 1);
            TestBase.AssertTrue(Stores.Get(foe, "rattle") >= 1, "weaken store adds Rattle", ref _run, ref _passed, ref _failed);

            foreach (string name in new[] { "VENOM COAT", "OPEN VEIN", "STOKE", "CORRODE", "DAZE", "BRACE" })
            {
                var data = ActionLoader.GetActionData(name);
                TestBase.AssertTrue(data != null && VerbSpec.Parse(data.Verb)?.Kind == VerbKind.Apply
                    && data.Tags.Any(t => t.Equals("action", StringComparison.OrdinalIgnoreCase)), $"{name} is an APPLY action in the gear pool", ref _run, ref _passed, ref _failed);
            }
        }

        private static void TestCashIntoParse()
        {
            TestBase.SetCurrentTestName(nameof(TestCashIntoParse));
            var harden = VerbSpec.Parse("CASH ATTUNED 100 INTO harden");
            TestBase.AssertTrue(harden != null && harden.Kind == VerbKind.Cash && harden.Store == "FULLEST" && harden.Into == "harden",
                "CASH ATTUNED 100 INTO harden", ref _run, ref _passed, ref _failed);
            TestBase.AssertEqual("TEC", VerbSpec.Parse("CASH RAGE INTO tech")?.Into ?? "", "TECH normalizes to TEC", ref _run, ref _passed, ref _failed);
            TestBase.AssertEqual(CashPayout.Damage, VerbSpec.Parse("CASH ATTUNED 75")?.Into ?? "", "no INTO = damage", ref _run, ref _passed, ref _failed);
            TestBase.AssertNull(VerbSpec.Parse("CASH ATTUNED INTO nonsense"), "unknown payout is null", ref _run, ref _passed, ref _failed);
            TestBase.AssertNull(VerbSpec.Parse("DETONATE INTO harden"), "INTO only on CASH", ref _run, ref _passed, ref _failed);
        }

        private static void TestCashIntoPayouts(CombatConfig combat)
        {
            TestBase.SetCurrentTestName(nameof(TestCashIntoPayouts));
            var saved = combat.CashIntoPerUnit;
            bool paysDamage = combat.MaterialConvertPaysDamage;
            try
            {
                combat.CashIntoPerUnit = null; // defaults
                combat.MaterialConvertPaysDamage = true;
                Character Fresh(string name)
                {
                    var h = Hero(name, "Bone", "Bone");
                    h.Effects.AddMaterialKeyword("RAGE", 6);
                    return h;
                }
                var foe = new Character("IntoFoe", 5);

                var dmg = Fresh("IntoDamage");
                TestBase.AssertEqual(0.0, VerbPresets.GetDamagePercent(dmg, foe, VerbSpec.Parse("CASH ATTUNED 100 INTO harden")!),
                    "a non-damage payout adds no hit damage", ref _run, ref _passed, ref _failed);

                var hard = Fresh("IntoHarden");
                TestBase.AssertEqual(6, VerbPresets.OnConnect(hard, foe, Verb("GRIT TEETH", "CASH ATTUNED 100 INTO harden")), "spends 6 RAGE", ref _run, ref _passed, ref _failed);
                TestBase.AssertEqual(0, hard.Effects.GetMaterialKeyword("RAGE"), "RAGE empty", ref _run, ref _passed, ref _failed);
                TestBase.AssertEqual(3, Stores.Get(hard, "harden"), "6 RAGE x 0.5 = 3 Harden", ref _run, ref _passed, ref _failed);

                var bleed = Fresh("IntoBleed");
                VerbPresets.OnConnect(bleed, foe, Verb("BLOODLET", "CASH ATTUNED 100 INTO bleed"));
                TestBase.AssertEqual(6, Stores.Get(foe, "bleed"), "6 RAGE x 1 = 6 Bleed on the target", ref _run, ref _passed, ref _failed);
                TestBase.AssertEqual(0, Stores.Get(bleed, "bleed"), "not on the hero", ref _run, ref _passed, ref _failed);

                var heal = Fresh("IntoHeal");
                int max = heal.GetEffectiveMaxHealth();
                heal.CurrentHealth = max / 2;
                VerbPresets.OnConnect(heal, foe, Verb("RESTORE", "CASH ATTUNED 100 INTO HEAL"));
                int expectHeal = (int)Math.Round(max * 0.02);
                TestBase.AssertEqual(Math.Min(max, max / 2 + expectHeal), heal.CurrentHealth, "6 RAGE x 0.75% = 4.5%, capped at 2% max HP", ref _run, ref _passed, ref _failed);

                var capped = Hero("IntoHealCap", "Bone", "Bone");
                capped.Effects.AddMaterialKeyword("RAGE", 10);
                int capMax = capped.GetEffectiveMaxHealth();
                capped.CurrentHealth = capMax / 2;
                VerbPresets.OnConnect(capped, foe, Verb("RESTORE", "CASH ATTUNED 100 INTO HEAL"));
                TestBase.AssertEqual(capMax / 2 + (int)Math.Round(capMax * 0.02), capped.CurrentHealth, "10 RAGE x 0.75% = 7.5%, capped at 2% per cash", ref _run, ref _passed, ref _failed);

                var temp = Fresh("IntoTemp");
                VerbPresets.OnConnect(temp, foe, Verb("BULWARK", "CASH ATTUNED 100 INTO temphp"));
                TestBase.AssertEqual((int)Math.Round(temp.GetEffectiveMaxHealth() * 0.30), temp.TemporaryHP ?? 0, "6 RAGE x 5% = 30% max HP temp", ref _run, ref _passed, ref _failed);

                var stat = Fresh("IntoStat");
                int before = stat.GetEffectiveStrength();
                VerbPresets.OnConnect(stat, foe, Verb("SURGE", "CASH ATTUNED 100 INTO PRIMARY"));
                int after = stat.GetEffectiveStrength();
                TestBase.AssertTrue(after > before, $"Bone cashes into STR for the fight ({before} -> {after})", ref _run, ref _passed, ref _failed);

                combat.CashIntoPerUnit = new Dictionary<string, double>(StringComparer.OrdinalIgnoreCase) { ["harden"] = 1.0 };
                var patched = Fresh("IntoPatched");
                VerbPresets.OnConnect(patched, foe, Verb("GRIT TEETH", "CASH ATTUNED 100 INTO harden"));
                TestBase.AssertEqual(Math.Min(6, Stores.Get(patched, "harden")), Stores.Get(patched, "harden"), "patch rate applies (capped by Harden's max)", ref _run, ref _passed, ref _failed);
                TestBase.AssertTrue(Stores.Get(patched, "harden") > 3, "patch rate 1.0 beats the default", ref _run, ref _passed, ref _failed);

                var empty = Hero("IntoEmpty", "Bone", "Bone");
                TestBase.AssertEqual(0, VerbPresets.OnConnect(empty, foe, Verb("GRIT TEETH", "CASH ATTUNED 100 INTO harden")), "nothing to spend", ref _run, ref _passed, ref _failed);
                TestBase.AssertEqual(0, Stores.Get(empty, "harden"), "and nothing paid", ref _run, ref _passed, ref _failed);
            }
            finally
            {
                combat.CashIntoPerUnit = saved;
                combat.MaterialConvertPaysDamage = paysDamage;
            }
        }

        private static void TestPresetsInData()
        {
            TestBase.SetCurrentTestName(nameof(TestPresetsInData));
            foreach (string name in new[] { "CASH IN", "DETONATE",
                         "RESTORE", "SURGE", "BULWARK", "GRIT TEETH", "CENTER", "BLOODLET", "QUICKEN", "IGNITE", "LAY BARE", "STEEL NERVE" })
            {
                var data = ActionLoader.GetActionData(name);
                TestBase.AssertTrue(data != null && VerbSpec.Parse(data.Verb) != null, $"{name} loads with a valid verb", ref _run, ref _passed, ref _failed);
                TestBase.AssertTrue(data != null && data.Tier <= 3 && data.Tags.Any(t => t == "action"), $"{name} is in play (tier <= 3, action gear pool)", ref _run, ref _passed, ref _failed);
            }
        }
    }
}
