using System;
using System.Collections.Generic;
using System.Linq;
using RPGGame.Actions.Conditional;
using RPGGame.Combat.Events;
using RPGGame.Combat.Rules;

namespace RPGGame.Tests.Unit
{
    /// <summary>
    /// The fixed resolution order (Documentation/CORE_DESIGN_PASS_2026-11-17.md D6.12–D6.16): owner order with tree order
    /// for skills, gains before everything else, the granting item first, follow-up events queued after the current
    /// event, the generation cap and the owner trace.
    /// </summary>
    public static class RuleEngineOrderTests
    {
        private static int _run, _passed, _failed;

        public static void RunAllTests()
        {
            Console.WriteLine("=== RuleEngineOrder Tests ===\n");
            _run = _passed = _failed = 0;
            var trees = SkillTreesConfig.TryLoadFromGameDataFile();
            if (trees != null)
                GameConfiguration.Instance.SkillTrees = trees;

            TestItemsInSlotOrder();
            TestSkillsInTreeOrderNotLearnOrder();
            TestGainsBeforeSpends();
            TestGrantingItemFirst();
            TestFollowUpsQueueAfterCurrentEvent();
            TestGenerationCap();
            TestTraceRecordsOwnerAndGeneration();
            TestCacheRebuildsOnChange();
            TestAffixTemplatesAreItemRules();

            TestBase.PrintSummary("RuleEngineOrder Tests", _run, _passed, _failed);
        }

        private static Character Hero(string name)
        {
            var hero = TestDataBuilders.Character().WithName(name).Build();
            hero.Equipment.Weapon = TestDataBuilders.Weapon().WithName(name + "Blade").WithWeaponType(WeaponType.Dagger).Build();
            hero.Equipment.Head = new HeadItem(name + "Helm", 1, 1);
            hero.Equipment.Body = new ChestItem(name + "Coat", 1, 1);
            CrossWires.ResetFight(hero);
            return hero;
        }

        private static void TestAffixTemplatesAreItemRules()
        {
            var helm = new HeadItem("TplHelm", 1, 1) { Material = "Glass" };
            string Of(string effect, string? when = null)
            {
                var mod = new Modification { Name = effect, Effect = effect, RolledValue = 12, MinValue = 12, TriggerWhen = when ?? "" };
                var rule = AffixTemplates.Rules(helm, mod).FirstOrDefault();
                return rule == null ? "" : rule.Verb + (rule.IsPassive ? " passive" : "") + (rule.Listener ? " listener" : "");
            }
            TestBase.AssertEqual("MINTBONUS passive", Of(AffixTemplates.Tune), "TUNE is WHILE -> MINTBONUS", ref _run, ref _passed, ref _failed);
            TestBase.AssertEqual("DAMAGE passive", Of(AffixTemplates.StoreCondition), "STORECOND is a gated DAMAGE rule", ref _run, ref _passed, ref _failed);
            TestBase.AssertEqual("SETCOUNT passive", Of(AffixTemplates.CountsDouble), "COUNTSDOUBLE is WHILE -> SETCOUNT", ref _run, ref _passed, ref _failed);
            TestBase.AssertEqual("DAMAGE passive", Of(AffixTemplates.Opener), "OPENER is IF SLOT=1 DAMAGE", ref _run, ref _passed, ref _failed);
            TestBase.AssertEqual("DAMAGE passive", Of(AffixTemplates.Finisher), "FINISHER is IF SLOTSLEFT=0 DAMAGE", ref _run, ref _passed, ref _failed);
            TestBase.AssertEqual("DAMAGE passive", Of(AffixTemplates.BloodPrice), "BLOODPRICE's damage is a DAMAGE rule", ref _run, ref _passed, ref _failed);
            TestBase.AssertEqual("MINT listener", Of(ListenerAffix.EffectName, "ONCRITICAL"), "LISTEN is an item-owned mint", ref _run, ref _passed, ref _failed);
        }

        private static int LastForcedFace(Character hero) =>
            CombatTriggerContext.TryConsumePendingReplaceRollFace(hero, out int face) ? face : 0;

        private static void TestItemsInSlotOrder()
        {
            var hero = Hero("Slots");
            hero.Equipment.Head!.ImplicitRule = "ONSELFCOST -> NEXTROLL 3";
            hero.Weapon!.ImplicitRule = "ONSELFCOST -> NEXTROLL 4";
            var owners = RuleBook.For(hero).Select(e => e.Owner.Name).ToList();
            TestBase.AssertTrue(owners.IndexOf("SlotsBlade") < owners.IndexOf("SlotsHelm"),
                "the weapon's rules come before the head's (Weapon, Head, Body, Legs, Feet)", ref _run, ref _passed, ref _failed);
            HeroEventHub.Raise(hero, "ONSELFCOST");
            TestBase.AssertEqual(3, LastForcedFace(hero), "so the head's NEXTROLL resolves last and wins",
                ref _run, ref _passed, ref _failed);
        }

        private static void TestSkillsInTreeOrderNotLearnOrder()
        {
            var nodes = GameConfiguration.Instance.SkillTrees.Trees.SelectMany(t => t.Nodes)
                .Where(n => !string.IsNullOrWhiteSpace(n.CrossWire)).ToList();
            var a = nodes.OrderBy(RuleBook.TreePosition).First();
            var b = nodes.OrderBy(RuleBook.TreePosition).Last();
            var hero = Hero("Tree");
            hero.Progression.LearnedSkillRanks[b.Id] = 1; // learned first
            hero.Progression.LearnedSkillRanks[a.Id] = 1;
            var skillOwners = RuleBook.For(hero).Where(e => e.Owner.Kind == RuleOwnerKind.Skill).Select(e => e.Owner.Node!.Id).Distinct().ToList();
            TestBase.AssertTrue(skillOwners.Count == 2 && skillOwners[0] == a.Id && skillOwners[1] == b.Id,
                "skill nodes resolve in tree order, not the order they were learned", ref _run, ref _passed, ref _failed);
            TestBase.AssertTrue(RuleBook.For(hero).TakeWhile(e => e.Owner.Kind == RuleOwnerKind.Item).Count()
                    == RuleBook.For(hero).Count(e => e.Owner.Kind == RuleOwnerKind.Item),
                "items come before skill nodes", ref _run, ref _passed, ref _failed);
        }

        private static void TestGainsBeforeSpends()
        {
            var hero = Hero("Gains");
            // The weapon (first owner) reads Loaded; the body (later owner) adds it. Gains resolve first, so the read sees it.
            hero.Weapon!.ImplicitRule = "ONSELFCOST IF loaded>=1 -> NEXTROLL 20";
            hero.Equipment.Body!.ImplicitRule = "ONSELFCOST -> ADD loaded 1";
            HeroEventHub.Raise(hero, "ONSELFCOST");
            TestBase.AssertEqual(20, LastForcedFace(hero), "a gain from a later owner is seen by an earlier owner's condition",
                ref _run, ref _passed, ref _failed);
        }

        private static void TestGrantingItemFirst()
        {
            var hero = Hero("Grant");
            hero.Equipment.Body!.GearAction = "TEST_GRANTED";
            hero.Weapon!.ImplicitRule = "ONSELFCOST -> NEXTROLL 4";
            hero.Equipment.Body!.ImplicitRule = "ONSELFCOST -> NEXTROLL 6";
            var swing = new Action { Name = "TEST_GRANTED", Type = ActionType.Attack };
            var itemRules = RuleBook.For(hero).Where(e => e.Owner.Kind == RuleOwnerKind.Item).ToList();
            var order = RuleEngine.InOrder(itemRules, swing, hero).Select(e => e.Owner.Name).ToList();
            TestBase.AssertEqual("GrantCoat", order.FirstOrDefault() ?? "", "the item that granted the swing's card resolves first",
                ref _run, ref _passed, ref _failed);
            var plain = RuleEngine.InOrder(itemRules).Select(e => e.Owner.Name).ToList();
            // Material sets' earn rules sit after the items and before the skill nodes (D6.12), in MaterialBuilds.json order.
            var book = RuleBook.For(hero);
            var sets = book.Where(e => e.Owner.Kind == RuleOwnerKind.Set).ToList();
            TestBase.AssertEqual(RPGGame.Data.MaterialBuildsLoader.GetAll().Count(b => !string.IsNullOrWhiteSpace(b.Keyword)),
                sets.Count, "every material set has its earn rule in the book", ref _run, ref _passed, ref _failed);
            TestBase.AssertTrue(sets.All(e => e.Owner.Order > RuleBook.ItemOrder + 4 && e.Owner.Order < RuleBook.SkillOrder),
                "set owners sort between items and skills", ref _run, ref _passed, ref _failed);
            TestBase.AssertTrue(!CrossWires.Active(hero).Any(r => r.SetBuild != null),
                "set earn rules stay out of the modifier rule list", ref _run, ref _passed, ref _failed);
            TestBase.AssertEqual("GrantBlade", plain.FirstOrDefault() ?? "", "without a swing, slot order", ref _run, ref _passed, ref _failed);
        }

        private static void TestFollowUpsQueueAfterCurrentEvent()
        {
            var hero = Hero("Queue");
            Stores.Add(hero, "loaded", 1);
            // Weapon spends (raising cash); head then forces 5; the cash rule forces 7. Queued: cash resolves after the
            // head's rule, so 7 is last. Immediate resolution would end on 5.
            hero.Weapon!.ImplicitRule = "ONSELFCOST -> SPEND loaded 1";
            hero.Equipment.Head!.ImplicitRule = "ONSELFCOST -> NEXTROLL 5";
            hero.Equipment.Body!.ImplicitRule = "ONCASH:loaded -> NEXTROLL 7";
            HeroEventHub.Raise(hero, "ONSELFCOST");
            TestBase.AssertEqual(7, LastForcedFace(hero), "a spend's cash event resolves after every rule on the current event",
                ref _run, ref _passed, ref _failed);
        }

        private static void TestGenerationCap()
        {
            var hero = Hero("Cap");
            Stores.Add(hero, "loaded", 10);
            hero.Weapon!.ImplicitRule = "ONSELFCOST -> SPEND loaded 1";
            hero.Equipment.Head!.ImplicitRule = "ONCASH:loaded -> SPEND loaded 1";
            long stoppedBefore = RuleEngine.ChainsStopped;
            HeroEventHub.Raise(hero, "ONSELFCOST");
            TestBase.AssertEqual(7, Stores.Get(hero, "loaded"),
                "a self-feeding chain resolves the event plus two generations of follow-ups (3 spends)", ref _run, ref _passed, ref _failed);
            TestBase.AssertEqual(stoppedBefore + 1, RuleEngine.ChainsStopped, "the third generation is dropped and counted",
                ref _run, ref _passed, ref _failed);
            TestBase.AssertTrue(!RuleEngine.Resolving, "no scope is left open", ref _run, ref _passed, ref _failed);
        }

        private static void TestTraceRecordsOwnerAndGeneration()
        {
            var hero = Hero("Trace");
            Stores.Add(hero, "loaded", 2);
            hero.Weapon!.ImplicitRule = "ONSELFCOST -> SPEND loaded 1";
            hero.Equipment.Body!.ImplicitRule = "ONCASH:loaded -> NEXTROLL 9";
            RuleEngine.ClearTrace();
            HeroEventHub.Raise(hero, "ONSELFCOST");
            var trace = RuleEngine.Trace.ToList();
            TestBase.AssertTrue(trace.Count == 2
                    && trace[0].Owner == "TraceBlade" && trace[0].Generation == 0 && trace[0].Kind == RuleOwnerKind.Item
                    && trace[1].Owner == "TraceCoat" && trace[1].Generation == 1,
                "the trace names each rule's owner and generation", ref _run, ref _passed, ref _failed);
            LastForcedFace(hero);
        }

        private static void TestCacheRebuildsOnChange()
        {
            var hero = Hero("Cache");
            int before = RuleBook.For(hero).Count;
            hero.Equipment.Head!.ImplicitRule = "WHILE -> DAMAGE 5%";
            TestBase.AssertEqual(before + 1, RuleBook.For(hero).Count, "editing an item's rule rebuilds the book",
                ref _run, ref _passed, ref _failed);
            hero.Equipment.Head = null;
            TestBase.AssertEqual(before, RuleBook.For(hero).Count, "unequipping rebuilds the book", ref _run, ref _passed, ref _failed);
        }
    }
}
