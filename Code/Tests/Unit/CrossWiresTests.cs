using System;
using System.Linq;
using RPGGame;
using RPGGame.Combat.Events;
using RPGGame.Data;
using RPGGame.Tests;

namespace RPGGame.Tests.Unit
{
    /// <summary>Cross-wire rules: parsing, skill-node and item sources, hub and combat events, IF conditions.</summary>
    public static class CrossWiresTests
    {
        private static int _run, _passed, _failed;

        public static void RunAllTests()
        {
            Console.WriteLine("=== Cross Wires Tests ===\n");
            _run = _passed = _failed = 0;
            MaterialBuildsLoader.Reload();

            TestParse();
            TestSkillNodeCashMintsOtherStore();
            TestItemWireAndCondition();
            TestTargetConditionAndApply();
            TestCombatEventWire();
            TestDataHasWires();
            TestPersistenceCarriesHeroStores();
            TestContagionMovesStatuses();
            TestConversionTable();

            TestBase.PrintSummary("Cross Wires Tests", _run, _passed, _failed);
        }

        private static Character Hero(string name)
        {
            var hero = new Character(name, 10);
            hero.Equipment.Weapon = new WeaponItem("Blade", 1, 10, 1.0, WeaponType.Mace) { Material = "Bone" };
            return hero;
        }

        private static void TestParse()
        {
            TestBase.SetCurrentTestName(nameof(TestParse));
            var r = CrossWireRule.Parse("ONCASH:RAGE -> MINT CRISIS 50%");
            TestBase.AssertTrue(r != null && r.Verb == "MINT" && r.Store == "CRISIS" && r.Amount == 50 && r.Percent, "cash → mint percent", ref _run, ref _passed, ref _failed);
            var c = CrossWireRule.Parse("ONKILL IF CRISIS>=1 -> MINT TRIUMPH 2");
            TestBase.AssertTrue(c != null && c.IfStore == "CRISIS" && c.IfAtLeast == 1 && !c.Percent, "store condition", ref _run, ref _passed, ref _failed);
            var t = CrossWireRule.Parse("ONATTACK IF TARGET expose -> APPLY bleed 1");
            TestBase.AssertTrue(t != null && t.IfTargetStatus == "expose" && t.Verb == "APPLY", "target condition", ref _run, ref _passed, ref _failed);
            TestBase.AssertNull(CrossWireRule.Parse("NOTANEVENT -> MINT RAGE 1"), "unknown event rejected", ref _run, ref _passed, ref _failed);
            TestBase.AssertNull(CrossWireRule.Parse("ONCASH -> SHOUT RAGE 1"), "unknown verb rejected", ref _run, ref _passed, ref _failed);
        }

        private static void TestSkillNodeCashMintsOtherStore()
        {
            TestBase.SetCurrentTestName(nameof(TestSkillNodeCashMintsOtherStore));
            var hero = Hero("WireHero");
            hero.Progression.LearnedSkillRanks["b-x-bone"] = 1;
            TestBase.AssertTrue(CrossWires.Active(hero).Any(r => r.Store == "CRISIS"), "Pain to Execution is active", ref _run, ref _passed, ref _failed);
            HeroEventHub.OnCash(hero, "RAGE", 6);
            TestBase.AssertEqual(3, hero.Effects.GetMaterialKeyword("CRISIS"), "6 RAGE spent → 3 CRISIS", ref _run, ref _passed, ref _failed);
            HeroEventHub.OnCash(hero, "GRAZE", 6);
            TestBase.AssertEqual(3, hero.Effects.GetMaterialKeyword("CRISIS"), "other stores don't trigger it", ref _run, ref _passed, ref _failed);
        }

        private static void TestItemWireAndCondition()
        {
            TestBase.SetCurrentTestName(nameof(TestItemWireAndCondition));
            var hero = Hero("ItemWireHero");
            hero.Equipment.Weapon!.Modifications.Add(new Modification { Name = "Gritted", Effect = CrossWires.AffixEffect, CrossWire = "ONCASH:GRIT -> ADD harden 1" });
            HeroEventHub.OnCash(hero, "GRIT", 2);
            TestBase.AssertEqual(1, hero.HardenStacks ?? 0, "item cross-wire grants Harden", ref _run, ref _passed, ref _failed);

            var gated = Hero("GatedHero");
            gated.Progression.LearnedSkillRanks["b-x-iron"] = 1;
            var foe = TestDataBuilders.Enemy().WithName("GateFoe").Build();
            var kill = new CombatEvent(CombatEventType.EnemyDied, gated) { Target = foe, Action = new Action { Name = "SLAM", Type = ActionType.Attack } };
            MaterialSetController.TryMintFromEvent(gated, kill, kill.Action);
            TestBase.AssertEqual(0, gated.Effects.GetMaterialKeyword("TRIUMPH"), "no CRISIS: Culling Glory stays quiet", ref _run, ref _passed, ref _failed);
            gated.Effects.AddMaterialKeyword("CRISIS", 1);
            MaterialSetController.TryMintFromEvent(gated, kill, kill.Action);
            TestBase.AssertEqual(2, gated.Effects.GetMaterialKeyword("TRIUMPH"), "kill with CRISIS mints 2 TRIUMPH", ref _run, ref _passed, ref _failed);
        }

        private static void TestTargetConditionAndApply()
        {
            TestBase.SetCurrentTestName(nameof(TestTargetConditionAndApply));
            var hero = Hero("MarkWireHero");
            hero.Progression.LearnedSkillRanks["r-x-glass"] = 1;
            var foe = new Character("MarkWireFoe", 5);
            HeroEventHub.SetTarget(hero, foe);
            HeroEventHub.OnCash(hero, "GRAZE", 2);
            TestBase.AssertTrue(foe.IsMarked, "spending GRAZE marks the current target", ref _run, ref _passed, ref _failed);
        }

        private static void TestCombatEventWire()
        {
            TestBase.SetCurrentTestName(nameof(TestCombatEventWire));
            var hero = Hero("ShadowWireHero");
            hero.Progression.LearnedSkillRanks["r-x-shadow"] = 1;
            var foe = new Character("ShadowFoe", 5);
            HeroEventHub.SetTarget(hero, foe);
            var swing = new CombatEvent(CombatEventType.ActionHit, hero) { Target = foe, Action = new Action { Name = "STAB", Type = ActionType.Attack } };
            MaterialSetController.TryMintFromEvent(hero, swing, swing.Action);
            TestBase.AssertEqual(0, foe.BleedIntensity, "no Expose: no bleed", ref _run, ref _passed, ref _failed);
            ExposeStatus.Apply(foe, 1, 3);
            MaterialSetController.TryMintFromEvent(hero, swing, swing.Action);
            TestBase.AssertEqual(1, foe.BleedIntensity, "attack on an Exposed target adds 1 Bleed", ref _run, ref _passed, ref _failed);
        }

        private static void TestPersistenceCarriesHeroStores()
        {
            TestBase.SetCurrentTestName(nameof(TestPersistenceCarriesHeroStores));
            var hero = Hero("StrideHero");
            hero.Progression.LearnedSkillRanks["w-persist"] = 1;
            hero.Effects.MaterialConsecutiveConnects = 4;
            Stores.Add(hero, "focus", 2);
            Persistence.OnFightEnd(hero, null);
            MaterialSetController.ResetFightConnects(hero);
            Stores.SpendAll(hero, "focus");
            Persistence.OnFightStart(hero, null);
            TestBase.AssertEqual(4, hero.Effects.MaterialConsecutiveConnects, "hit streak carried", ref _run, ref _passed, ref _failed);
            TestBase.AssertEqual(2, Stores.Get(hero, "focus"), "Focus carried", ref _run, ref _passed, ref _failed);

            var plain = Hero("PlainHero");
            plain.Effects.MaterialConsecutiveConnects = 4;
            Persistence.OnFightEnd(plain, null);
            MaterialSetController.ResetFightConnects(plain);
            Persistence.OnFightStart(plain, null);
            TestBase.AssertEqual(0, plain.Effects.MaterialConsecutiveConnects, "without the capstone nothing carries", ref _run, ref _passed, ref _failed);
        }

        private static void TestContagionMovesStatuses()
        {
            TestBase.SetCurrentTestName(nameof(TestContagionMovesStatuses));
            var hero = Hero("ContagionHero");
            hero.Progression.LearnedSkillRanks["r-persist"] = 1;
            var dead = new Character("DeadFoe", 5);
            ExposeStatus.Apply(dead, 2, 3);
            dead.BleedIntensity = 4;
            Persistence.OnFightEnd(hero, dead);
            var next = new Character("NextFoe", 5);
            Persistence.OnFightStart(hero, next);
            TestBase.AssertEqual(2, next.ExposeStacks ?? 0, "Expose jumped to the next enemy", ref _run, ref _passed, ref _failed);
            TestBase.AssertEqual(4, next.BleedIntensity, "Bleed jumped too", ref _run, ref _passed, ref _failed);
        }

        private static void TestConversionTable()
        {
            TestBase.SetCurrentTestName(nameof(TestConversionTable));
            ConversionTable.Reload();
            var ids = ConversionTable.Rows.Select(r => r.Id).ToList();
            foreach (string id in new[] { "venom_mace", "envenomed_edge", "hemorrhage", "kindle", "thermal_shock" })
                TestBase.AssertTrue(ids.Contains(id), $"{id} row loads", ref _run, ref _passed, ref _failed);
            var edge = ConversionTable.Rows.First(r => r.Id == "envenomed_edge");
            var hero = Hero("EdgeHero");
            var foe = new Character("EdgeFoe", 5);
            foe.PoisonPercentOfMaxHealth = 3.0;
            int passes = ConversionTable.ApplyRow(hero, foe, edge, isCritical: true);
            TestBase.AssertEqual(2, passes, "a crit converts twice", ref _run, ref _passed, ref _failed);
            TestBase.AssertEqual(4, foe.PendingBleedFromHits, "4 bleed queued (1:2)", ref _run, ref _passed, ref _failed);
            TestBase.AssertTrue(Math.Abs(foe.PoisonPercentOfMaxHealth - 1.0) < 1e-9, "2% poison spent (4 units per pass)", ref _run, ref _passed, ref _failed);
            var thin = new Character("ThinFoe", 5);
            thin.PoisonPercentOfMaxHealth = 0.5;
            ConversionTable.ApplyRow(hero, thin, edge, isCritical: true);
            TestBase.AssertEqual(0.0, thin.PoisonPercentOfMaxHealth, "short poison is spent to 0, second pass stops", ref _run, ref _passed, ref _failed);
            TestBase.AssertEqual(2, thin.PendingBleedFromHits, "one pass only once poison is gone", ref _run, ref _passed, ref _failed);
            var none = new Character("CleanFoe", 5);
            TestBase.AssertEqual(0, ConversionTable.ApplyRow(hero, none, edge, isCritical: false), "no poison, no conversion", ref _run, ref _passed, ref _failed);
        }

        private static void TestDataHasWires()
        {
            TestBase.SetCurrentTestName(nameof(TestDataHasWires));
            var trees = SkillTreeService.Trees;
            int nodes = trees?.Trees.SelectMany(t => t.Nodes).Count(n => CrossWireRule.Parse(n.CrossWire) != null) ?? 0;
            TestBase.AssertEqual(12, nodes, "12 class cross-wire nodes parse", ref _run, ref _passed, ref _failed);
            int mods = LootDataCache.Load().Modifications.Count(m => AffixTemplates.Is(m, CrossWires.AffixEffect) && CrossWireRule.Parse(m.CrossWire) != null);
            TestBase.AssertEqual(8, mods, "8 cross-wire prefixes parse", ref _run, ref _passed, ref _failed);
            int caps = trees?.Trees.SelectMany(t => t.Nodes).Count(n => !string.IsNullOrWhiteSpace(n.Persist)) ?? 0;
            TestBase.AssertEqual(4, caps, "4 class Persistence capstones", ref _run, ref _passed, ref _failed);
        }
    }
}
