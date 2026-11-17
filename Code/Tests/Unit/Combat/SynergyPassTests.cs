using System;
using System.Collections.Generic;
using System.Linq;
using RPGGame.Combat.Events;
using RPGGame.Data;
using RPGGame.Data.ItemGeneration;

namespace RPGGame.Tests.Unit.Combat
{
    /// <summary>
    /// Synergy pass (Documentation/SYNERGY_PASS_2026-11-16.md): the extended rule grammar (PER, WEAPON, PREVTAG / NEXTTAG,
    /// CHANCE, ONCE, NEXT with FOR / RAMP / IFTAG, AMP / MULTIHIT / attribute passives, rank scaling), the new statuses
    /// (Brittle, Stagger, Haste, Rattle, Mark's crit), the RELEASE verb, cadence ifTag / perTag, either-or skill choices and
    /// that every rule in the catalogs parses.
    /// </summary>
    public static class SynergyPassTests
    {
        private static int _run, _passed, _failed;

        public static void RunAllTests()
        {
            Console.WriteLine("=== SynergyPass Tests ===\n");
            _run = _passed = _failed = 0;
            var trees = SkillTreesConfig.TryLoadFromGameDataFile();
            if (trees != null)
                GameConfiguration.Instance.SkillTrees = trees;

            TestGrammarParses();
            TestEveryCatalogRuleParses();
            TestPerSourcesAndRankScaling();
            TestWeaponAndAdjacencyConditions();
            TestNextQueuesTurnGrant();
            TestIfTagWaitsForTag();
            TestReleasePullsQueuedBuffs();
            TestNewStatusStores();
            TestMarkForcesCritAndIsSpent();
            TestBrittlePaysOnCrit();
            TestStaggerAndHasteChangeTime();
            TestEitherOrSkillChoices();
            TestNewActionsLoad();
            TestBotSequencesNewActions();

            TestBase.PrintSummary("SynergyPass Tests", _run, _passed, _failed);
        }

        private static void Check(bool ok, string label) => TestBase.AssertTrue(ok, label, ref _run, ref _passed, ref _failed);

        private static Character MakeHero(string name, WeaponType weapon = WeaponType.Mace)
        {
            var hero = TestDataBuilders.Character().WithName(name).Build();
            hero.Progression.BarbarianPoints = hero.Progression.WarriorPoints = hero.Progression.RoguePoints = hero.Progression.WizardPoints = 80;
            hero.Progression.EnsureSkillTreeRootsGranted();
            hero.Equipment.Weapon = TestDataBuilders.Weapon().WithName(name + "Weapon").WithWeaponType(weapon).Build();
            hero.Effects.TurnBonuses.Clear();
            return hero;
        }

        private static void TestGrammarParses()
        {
            Console.WriteLine("--- grammar ---");
            var a = CrossWireRule.Parse("ONCRITICAL CHANCE 35% IF WEAPON mace -> APPLY stun 1");
            Check(a != null && Math.Abs(a.Chance - 0.35) < 1e-9 && a.Conditions[0].Subject == "weapon", "CHANCE + WEAPON parse");
            var b = CrossWireRule.Parse("ONTAKEHIT ONCE -> ADD harden 2");
            Check(b is { Once: true }, "ONCE parses");
            var c = CrossWireRule.Parse("ONCOMBO IF TAG focus -> NEXT AMP 40% FOR 2 RAMP IFTAG status");
            Check(c != null && c.Verb == "NEXT" && c.Turns == 2 && c.Shape == "ramp" && c.IfTag == "status", "NEXT with FOR / RAMP / IFTAG parses");
            var d = CrossWireRule.Parse("WHILE IF WEAPON wand -> AMP 2% PER STAT INT 10");
            Check(d?.Per is { Kind: "stat", Arg: "INT", Div: 10 }, "PER STAT with a divisor parses");
            var e = CrossWireRule.Parse("WHILE IF TAG barbarian AND PREVTAG bludgeon -> DAMAGE 12%");
            Check(e != null && e.Conditions.Any(x => x.Subject == "prevtag"), "PREVTAG parses");
            Check(CrossWireRule.Parse("WHILE -> STR 25 PER EXTRASLOTS")?.Per is { Kind: "self", Arg: "EXTRASLOTS" }, "bare self-key PER parses");
            Check(CrossWireRule.Parse("WHILE ONCE -> DAMAGE 5%") == null, "passive rules reject ONCE");
            Check(CrossWireRule.Parse("ONCONNECT -> NEXT BOGUS 5") == null, "NEXT rejects unknown stats");
            Check(CrossWires.Describe(c!).Contains("next 2 status actions"), "NEXT describes its tag wait");
        }

        private static void TestEveryCatalogRuleParses()
        {
            Console.WriteLine("--- every catalog rule parses ---");
            var cache = LootDataCache.Load();
            var bad = new List<string>();
            foreach (var (name, template) in cache.WeaponData.Select(w => (w.Name, w.Modifier)).Concat(cache.ArmorData.Select(a => (a.Name, a.Modifier))))
            {
                if (string.IsNullOrWhiteSpace(template))
                    continue;
                if (BaseModifierRoller.FromCatalog(template, new Random(1)) == null)
                    bad.Add($"{name}: {template}");
            }
            Check(bad.Count == 0, $"every base modifier rolls and parses ({bad.Count} bad{(bad.Count > 0 ? ": " + bad[0] : "")})");

            var badNodes = GameConfiguration.Instance.SkillTrees.Trees.SelectMany(t => t.Nodes)
                .Where(n => !string.IsNullOrWhiteSpace(n.CrossWire))
                .SelectMany(n => n.CrossWire.Split(';').Select(p => (n.Id, Part: p.Trim())))
                .Where(x => CrossWireRule.Parse(x.Part) == null)
                .Select(x => $"{x.Id}: {x.Part}")
                .ToList();
            Check(badNodes.Count == 0, $"every skill node rule parses ({badNodes.Count} bad{(badNodes.Count > 0 ? ": " + badNodes[0] : "")})");
        }

        private static void TestPerSourcesAndRankScaling()
        {
            Console.WriteLine("--- PER sources and rank scaling ---");
            var hero = MakeHero("Per");
            hero.Weapon!.Material = "Bone";
            var worn = new RulePer { Kind = "worn", Arg = "barbarian" };
            Check(CrossWires.PerValue(hero, null, worn) >= 1, "WORN barbarian counts a Bone weapon");
            var stat = new RulePer { Kind = "stat", Arg = "STR", Div = 5 };
            Check(Math.Abs(CrossWires.PerValue(hero, null, stat) - Math.Floor(hero.GetEffectiveStrength() / 5.0)) < 1e-9, "PER STAT STR 5 = floor(STR / 5)");

            double before = SkillEffectRouter.Instance.GetSkillAttributeBonus(hero, "STR");
            hero.Progression.LearnedSkillRanks["b-kin"] = 3;
            double power = GameConfiguration.Instance.SkillTrees.GetNode("b-kin")?.Power ?? 1;
            int expected = (int)Math.Round(CrossWires.WornCount(hero, "barbarian") * 3 * power);
            Check(SkillEffectRouter.Instance.GetSkillAttributeBonus(hero, "STR") - before == expected,
                "Tribal Kinship rule: STR per worn piece x rank x power");

            // Attribute rules reading attributes do not recurse.
            hero.Progression.LearnedSkillRanks["b-gut"] = 3;
            int str = hero.GetEffectiveStrength();
            Check(str > 0, "Gut Instinct (STR per INT) resolves without recursion");
        }

        private static void TestWeaponAndAdjacencyConditions()
        {
            Console.WriteLine("--- WEAPON / PREVTAG / NEXTTAG ---");
            var hero = MakeHero("Adj", WeaponType.Mace);
            hero.Level = 40;
            var a = new Action { Name = "ADJ_A", Type = ActionType.Attack, Tags = new List<string> { "bludgeon" }, IsComboAction = true };
            var b = new Action { Name = "ADJ_B", Type = ActionType.Attack, Tags = new List<string> { "barbarian" }, IsComboAction = true };
            hero.AddToCombo(a);
            hero.AddToCombo(b);
            var prev = CrossWireRule.Parse("WHILE IF PREVTAG bludgeon -> DAMAGE 10%")!;
            var next = CrossWireRule.Parse("WHILE IF NEXTTAG barbarian -> DAMAGE 10%")!;
            var mace = CrossWireRule.Parse("WHILE IF WEAPON mace -> DAMAGE 10%")!;
            var sword = CrossWireRule.Parse("WHILE IF WEAPON sword -> DAMAGE 10%")!;
            bool stripOk = hero.GetComboActions().Count >= 2;
            Check(stripOk, "test strip holds both actions");
            if (stripOk)
            {
                Check(CrossWires.ConditionsHold(hero, null, prev, action: b), "B follows a bludgeon action");
                Check(CrossWires.ConditionsHold(hero, null, next, action: a), "A sits before a barbarian action");
            }
            Check(CrossWires.ConditionsHold(hero, null, mace) && !CrossWires.ConditionsHold(hero, null, sword), "WEAPON reads the equipped weapon");
        }

        private static void TestNextQueuesTurnGrant()
        {
            Console.WriteLine("--- NEXT ---");
            var hero = MakeHero("Next");
            hero.Progression.LearnedSkillRanks["r-lvl2"] = 1;   // ONATTACK -> NEXT DAMAGE 1% PER ROLL
            hero.Equipment.Weapon = TestDataBuilders.Weapon().WithName("NextDagger").WithWeaponType(WeaponType.Dagger).Build();
            var swing = new Action { Name = "TEST_SWING", Type = ActionType.Attack };
            MaterialSetController.TryMintFromEvent(hero, new CombatEvent(CombatEventType.ActionHit, hero)
            {
                Target = TestDataBuilders.Enemy().WithName("Dummy").Build(), Action = swing, NaturalRollValue = 12, Damage = 5
            }, swing);
            var got = hero.Effects.GetAndConsumeTurnBonuses(swing);
            double power = GameConfiguration.Instance.SkillTrees.GetNode("r-lvl2")?.Power ?? 1;
            Check(got.Any(x => x.Type == "DAMAGE_MOD" && Math.Abs(x.Value - 12 * power) < 1e-6), "roll 12 queues +12% x power damage on the next roll");
            Check(hero.Effects.GetAndConsumeTurnBonuses(swing).Count == 0, "the grant is spent after one roll");
        }

        private static void TestIfTagWaitsForTag()
        {
            Console.WriteLine("--- ifTag waits ---");
            var hero = MakeHero("Wait");
            hero.Effects.TurnBonuses.Add(new ActionAttackBonusGroup
            {
                Keyword = "TURN", CadenceType = "TURN", Count = 1, Total = 1,
                Timing = new CadenceTiming { IfTag = "bludgeon" },
                Bonuses = new List<ActionAttackBonusItem> { new() { Type = "DAMAGE_MOD", Value = 50 } }
            });
            var plain = new Action { Name = "PLAIN", Tags = new List<string> { "barbarian" } };
            var club = new Action { Name = "CLUB", Tags = new List<string> { "bludgeon" } };
            Check(hero.Effects.GetAndConsumeTurnBonuses(plain).Count == 0, "a non-bludgeon roll neither takes nor spends it");
            Check(hero.Effects.GetAndConsumeTurnBonuses(club).Any(x => x.Value == 50), "the bludgeon roll takes it");
            Check(hero.Effects.TurnBonuses.Count == 0, "then it is spent");
        }

        private static void TestReleasePullsQueuedBuffs()
        {
            Console.WriteLine("--- RELEASE ---");
            var hero = MakeHero("Release");
            hero.Effects.TurnBonuses.Add(new ActionAttackBonusGroup
            {
                Keyword = "TURN", CadenceType = "TURN", Count = 3, Total = 3, Timing = new CadenceTiming { Shape = "ramp" },
                Bonuses = new List<ActionAttackBonusItem> { new() { Type = "DAMAGE_MOD", Value = 10 } }
            });
            hero.Effects.AddPendingActionBonuses(2, new List<ActionAttackBonusItem> { new() { Type = "DAMAGE_MOD", Value = 7 } });
            var got = hero.Effects.ReleaseQueuedBonuses(1.25, currentSlot: 0);
            double total = got.Where(x => x.Type == "DAMAGE_MOD").Sum(x => x.Value);
            Check(Math.Abs(total - (10 + 20 + 30 + 7) * 1.25) < 1e-6, $"RELEASE pays every remaining roll + other slots at +25% (got {total})");
            Check(hero.Effects.TurnBonuses.Count == 0 && !hero.Effects.GetPendingActionBonusSlots().Any(), "queues are emptied");
            Check(VerbSpec.Parse("RELEASE 25") is { Kind: VerbKind.Release, Rate: 25 }, "RELEASE parses as a verb");
        }

        private static void TestNewStatusStores()
        {
            Console.WriteLine("--- new status stores ---");
            var foe = TestDataBuilders.Enemy().WithName("Dummy").Build();
            var hero = MakeHero("Stores");
            foreach (var id in new[] { "brittle", "stagger", "rattle" })
            {
                Stores.Add(foe, id, 2);
                Check(Stores.Get(foe, id) == 2, $"{id}: add 2, read 2");
                Check(Stores.Spend(foe, id, 1) == 1 && Stores.Get(foe, id) == 1, $"{id}: spend 1 leaves 1");
                Check(Stores.TryGet(id, out var def) && def.Kind == StoreKind.Status, $"{id} is a status on the target");
            }
            Stores.Add(hero, "haste", 9);
            Check(Stores.Get(hero, "haste") == TempoStatuses.MaxStacks, "haste caps at the extra-status max");
            Check(VerbPresets.Holder(hero, foe, "haste") == hero && VerbPresets.Holder(hero, foe, "brittle") == foe,
                "APPLY haste lands on you; APPLY brittle on the target");
            var snap = RPGGame.Combat.StatusApplySnapshot.Take(foe);
            Stores.Add(foe, "brittle", 1);
            Check(snap.Gained(foe).Contains("brittle"), "Brittle counts as a status application (ONSTATUSAPPLY:brittle)");
        }

        private static void TestMarkForcesCritAndIsSpent()
        {
            Console.WriteLine("--- Mark ---");
            var hero = MakeHero("Mark");
            var foe = TestDataBuilders.Enemy().WithName("Dummy").Build();
            Stores.Add(foe, "mark", 1);
            var tm = RPGGame.Actions.RollModification.RollModificationManager.GetThresholdManager();
            tm.ResetThresholds(hero);
            TempoStatuses.BeforeRoll(hero, foe, tm);
            Check(tm.GetCriticalHitThreshold(hero) <= 1, "a marked target drops the attacker's crit threshold");
            TempoStatuses.AfterRoll(hero, foe, hit: false, critical: false);
            Check(foe.IsMarked, "a miss keeps the Mark");
            TempoStatuses.AfterRoll(hero, foe, hit: true, critical: true);
            Check(!foe.IsMarked, "the landed hit spends it");
            tm.ResetThresholds(hero);
        }

        private static void TestBrittlePaysOnCrit()
        {
            Console.WriteLine("--- Brittle ---");
            var hero = MakeHero("Brittle");
            var foe = TestDataBuilders.Enemy().WithName("Dummy").Build();
            Stores.Add(foe, "brittle", 2);
            hero.Effects.ConsumedDamageModPercent = 0;
            TempoStatuses.AfterRoll(hero, foe, hit: true, critical: false);
            Check(TempoStatuses.Brittle(foe) == 2 && hero.Effects.ConsumedDamageModPercent == 0, "a normal hit leaves Brittle");
            TempoStatuses.AfterRoll(hero, foe, hit: true, critical: true);
            double per = GameConfiguration.Instance.Combat.StatusBrittleCritDamagePerStack * 100;
            Check(Math.Abs(hero.Effects.ConsumedDamageModPercent - 2 * per) < 1e-6 && TempoStatuses.Brittle(foe) == 0,
                "a crit spends Brittle for +damage per stack on that swing");
            hero.Effects.ConsumedDamageModPercent = 0;
        }

        private static void TestStaggerAndHasteChangeTime()
        {
            Console.WriteLine("--- Stagger / Haste ---");
            var foe = TestDataBuilders.Enemy().WithName("Dummy").Build();
            Stores.Add(foe, "stagger", 2);
            double slow = TempoStatuses.AdjustDuration(foe, 1.0);
            Check(slow > 1.0 && foe.StaggerStacks == 0, "Stagger lengthens the next action, then is spent");
            var hero = MakeHero("Haste");
            Stores.Add(hero, "haste", 2);
            Check(TempoStatuses.AdjustDuration(hero, 1.0) < 1.0, "Haste shortens actions");
            hero.ConsumeOwnActionStatuses();
            Check(hero.HasteStacks == 1, "one Haste stack falls off per own action");
        }

        private static void TestEitherOrSkillChoices()
        {
            Console.WriteLine("--- either-or skill choices ---");
            var trees = GameConfiguration.Instance.SkillTrees;
            var blood = trees.GetNode("b-blood")!;
            var trance = trees.GetNode("b-trance")!;
            var prog = MakeHero("Choice").Progression;
            prog.BarbarianPoints = 99;
            // Open the Bone band below (any tier-13 Bone node) and the trunk.
            foreach (var n in SkillTreePrerequisites.GetBandPredecessorOptions(SkillTreePrerequisites.FindOwnerTree(blood)!, blood))
                prog.LearnedSkillRanks[n.Id] = 1;
            var trunk = SkillTreePrerequisites.GetTrunkTip(SkillTreePrerequisites.FindOwnerTree(blood)!);
            if (trunk != null) prog.LearnedSkillRanks[trunk.Id] = 1;
            Check(SkillTreePrerequisites.AreMet(prog, blood) && SkillTreePrerequisites.AreMet(prog, trance), "both sides of a pair are open before a pick");
            prog.LearnedSkillRanks["b-trance"] = 1;
            Check(!SkillTreePrerequisites.AreMet(prog, blood), "taking Battle Trance locks Blood Price");
            Check(SkillTreePrerequisites.ExcludedBy(prog, blood)?.Id == "b-trance", "the lock names its cause");
            Check(SkillTreePrerequisites.FormatRequirementSummary(blood).Contains("Either-or"), "card text names the either-or");
            int pairs = trees.Trees.SelectMany(t => t.Nodes).Count(n => n.Excludes is { Count: > 0 });
            Check(pairs >= 32, $"class trees carry either-or choices ({pairs} nodes)");
        }

        private static void TestNewActionsLoad()
        {
            Console.WriteLine("--- new actions ---");
            ActionLoader.LoadActions();
            foreach (var name in new[] { "STAGGERING BLOW", "SUNDER", "QUICKSTEP", "JINX", "TOPPLE", "UNLEASH", "COILED STRIKE", "WAR BAND" })
                Check(ActionLoader.GetActionData(name) != null, $"{name} loads");
            Check(VerbPresets.GetSpec(ActionLoader.GetAction("SUNDER")) is { Kind: VerbKind.Apply, Store: "brittle" }, "SUNDER applies Brittle");
            var coil = ActionLoader.GetAction("COILED STRIKE");
            var timing = coil?.ActionAttackBonuses?.BonusGroups?.FirstOrDefault()?.Timing;
            Check(timing?.IfTag == "bludgeon", "COILED STRIKE's grant waits for a bludgeon action");
        }

        private static void TestBotSequencesNewActions()
        {
            Console.WriteLine("--- bot: strip context and sequence order ---");
            ActionLoader.LoadActions();
            var order = RPGGame.Tuning.RulesSynergy.SequenceOrder(new List<string> { "UNLEASH", "TOPPLE", "COILED STRIKE", "STAGGERING BLOW" });
            Check(order.IndexOf("TOPPLE") > order.IndexOf("STAGGERING BLOW"), "payoff (TOPPLE) goes after its Stagger applier");
            Check(order[^1] == "UNLEASH", "RELEASE (UNLEASH) goes last");
            int coil = order.IndexOf("COILED STRIKE");
            Check(coil + 1 < order.Count && ActionLoader.GetAction(order[coil + 1])?.Tags?.Contains("bludgeon") == true,
                "COILED STRIKE sits right before a bludgeon action");

            var hero = MakeHero("BotSeq");
            double without = RPGGame.Tuning.RulesSynergy.StripContextAdjust(hero, "COILED STRIKE", new[] { "JINX" });
            double with = RPGGame.Tuning.RulesSynergy.StripContextAdjust(hero, "COILED STRIKE", new[] { "STAGGERING BLOW" });
            Check(with == 0 && without <= 0, "an ifTag setup loses its grant value with no tagged action on the strip");
            double one = RPGGame.Tuning.RulesSynergy.StripContextAdjust(hero, "CHORUS", new[] { "JINX" });
            double three = RPGGame.Tuning.RulesSynergy.StripContextAdjust(hero, "CHORUS", new[] { "JINX", "ENERVATE", "PRIMER" });
            Check(three >= one, "a perTag grant is worth more with more tagged actions on the strip");
        }
    }
}
