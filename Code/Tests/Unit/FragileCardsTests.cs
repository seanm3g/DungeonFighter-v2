using System;
using System.Collections.Generic;
using System.Linq;
using RPGGame;
using RPGGame.Actions.Conditional;
using RPGGame.Combat.Narrative;
using RPGGame.Combat.Rules;
using RPGGame.Data;
using RPGGame.Tests;

namespace RPGGame.Tests.Unit
{
    /// <summary>Rebuild step 10: Fragile cards (D5.4, D5.7), Jam / Burn out strip effects (D3.15), build-moment callouts.</summary>
    public static class FragileCardsTests
    {
        private static int _run, _passed, _failed;

        public static void RunAllTests()
        {
            Console.WriteLine("=== Fragile Cards Tests ===\n");
            _run = _passed = _failed = 0;

            TestFragileBurnsOutAndStripClosesUp();
            TestBurnOutRepointsComboStep();
            TestStripIsWholeOutsideAFight();
            TestLastCardNeverBurnsOut();
            TestJamSkipsNextCard();
            TestBurnOutNeedsLongStrip();
            TestStripOpsHitTheFoe();
            TestDataCards();
            TestCardLineShowsFragile();
            TestBuildMomentCallouts();
            TestBoardShowsBurnedOutAndUsesLeft();

            CombatTriggerContext.EndBattle();
            TestBase.PrintSummary("Fragile Cards Tests", _run, _passed, _failed);
        }

        private static Character MakeHero(params (string Name, int Fragile)[] cards)
        {
            var hero = new Character("FragileHero", 10);
            hero.Stats.Intelligence = 20;
            while (hero.GetComboActions().Count > 0)
                hero.RemoveFromCombo(hero.GetComboActions()[0], ignoreWeaponRequirement: true);
            for (int i = 0; i < cards.Length; i++)
            {
                hero.Actions.AddToCombo(new Action
                {
                    Name = cards[i].Name,
                    Type = ActionType.Attack,
                    IsComboAction = true,
                    ComboOrder = i + 1,
                    Fragile = cards[i].Fragile,
                    ComboRouting = new ComboRoutingProperties()
                }, maxComboLength: null);
            }
            hero.ComboStep = 0;
            return hero;
        }

        private static void TestFragileBurnsOutAndStripClosesUp()
        {
            TestBase.SetCurrentTestName(nameof(TestFragileBurnsOutAndStripClosesUp));
            CombatTriggerContext.ResetForBattle();
            var hero = MakeHero(("A", 0), ("B", 2), ("C", 0));
            var b = hero.GetComboActions()[1];
            var log = new List<string>();
            TestBase.AssertEqual(2, FragileCards.UsesLeft(hero, b), "a Fragile 2 card starts with 2 uses", ref _run, ref _passed, ref _failed);
            TestBase.AssertFalse(FragileCards.AfterSwing(hero, b, log), "first swing of Fragile 2 does not burn out", ref _run, ref _passed, ref _failed);
            TestBase.AssertEqual(1, FragileCards.UsesLeft(hero, b), "one use left", ref _run, ref _passed, ref _failed);
            TestBase.AssertTrue(FragileCards.AfterSwing(hero, b, log), "second swing burns it out", ref _run, ref _passed, ref _failed);
            var live = hero.GetComboActions().Select(a => a.Name).ToList();
            TestBase.AssertEqual("A,C", string.Join(",", live), "the strip closes up (A, C)", ref _run, ref _passed, ref _failed);
            TestBase.AssertTrue(log.Any(l => l.Contains("burns out")), "a burn-out line is logged", ref _run, ref _passed, ref _failed);
            TestBase.AssertFalse(FragileCards.AfterSwing(hero, new Action { Name = "A" }, log), "a plain card never burns out", ref _run, ref _passed, ref _failed);
        }

        /// <summary>What the strips draw: burned-out cards (cracked, after the live ones) and a Fragile card's uses left.</summary>
        private static void TestBoardShowsBurnedOutAndUsesLeft()
        {
            TestBase.SetCurrentTestName(nameof(TestBoardShowsBurnedOutAndUsesLeft));
            CombatTriggerContext.ResetForBattle();
            var hero = MakeHero(("A", 0), ("B", 2), ("C", 3));
            var b = hero.GetComboActions()[1];
            var c = hero.GetComboActions()[2];
            var log = new List<string>();
            TestBase.AssertEqual("2 left", FragileCards.UsesBadge(hero, b), "a fresh Fragile 2 card shows 2 left", ref _run, ref _passed, ref _failed);
            TestBase.AssertTrue(FragileCards.UsesBadge(hero, hero.GetComboActions()[0]) == null, "a plain card has no badge", ref _run, ref _passed, ref _failed);
            FragileCards.AfterSwing(hero, b, log);
            TestBase.AssertEqual("last use", FragileCards.UsesBadge(hero, b), "one swing left reads 'last use'", ref _run, ref _passed, ref _failed);
            TestBase.AssertEqual(0, FragileCards.BurnedOutCards(hero).Count, "nothing burned out yet", ref _run, ref _passed, ref _failed);
            FragileCards.AfterSwing(hero, b, log);
            var burned = FragileCards.BurnedOutCards(hero);
            TestBase.AssertTrue(burned.Count == 1 && ReferenceEquals(burned[0], b), "the burned-out card is listed for the strip", ref _run, ref _passed, ref _failed);
            TestBase.AssertEqual("3 left", FragileCards.UsesBadge(hero, c), "another Fragile card keeps its own count", ref _run, ref _passed, ref _failed);
            CombatTriggerContext.EndBattle();
            TestBase.AssertEqual(0, FragileCards.BurnedOutCards(hero).Count, "after the fight nothing is burned out", ref _run, ref _passed, ref _failed);
            TestBase.AssertTrue(FragileCards.UsesBadge(hero, b) == null, "outside a fight there's no badge (the card line says Fragile N)", ref _run, ref _passed, ref _failed);
        }

        private static void TestBurnOutRepointsComboStep()
        {
            TestBase.SetCurrentTestName(nameof(TestBurnOutRepointsComboStep));
            CombatTriggerContext.ResetForBattle();
            var hero = MakeHero(("A", 0), ("B", 1), ("C", 0), ("D", 0));
            // B just fired; the combo moved on to C (step 2).
            hero.ComboStep = 2;
            FragileCards.AfterSwing(hero, hero.GetComboActions()[1], null);
            var live = hero.GetComboActions();
            TestBase.AssertEqual(3, live.Count, "strip is one card shorter", ref _run, ref _passed, ref _failed);
            TestBase.AssertEqual("C", live[hero.ComboStep % live.Count].Name, "the next card is still C", ref _run, ref _passed, ref _failed);
            TestBase.AssertTrue(hero.ComboStep > 0, "still mid-chain", ref _run, ref _passed, ref _failed);

            CombatTriggerContext.ResetForBattle();
            hero = MakeHero(("A", 1), ("B", 0), ("C", 0));
            hero.ComboStep = 1; // A fired, B is next
            FragileCards.AfterSwing(hero, hero.GetComboActions()[0], null);
            live = hero.GetComboActions();
            TestBase.AssertEqual("B", live[hero.ComboStep % live.Count].Name, "opener burned out: B is next", ref _run, ref _passed, ref _failed);
            TestBase.AssertTrue(hero.ComboStep > 0, "a burned opener keeps the chain alive", ref _run, ref _passed, ref _failed);
        }

        private static void TestStripIsWholeOutsideAFight()
        {
            TestBase.SetCurrentTestName(nameof(TestStripIsWholeOutsideAFight));
            CombatTriggerContext.ResetForBattle();
            var hero = MakeHero(("A", 0), ("B", 1), ("C", 0));
            FragileCards.AfterSwing(hero, hero.GetComboActions()[1], null);
            TestBase.AssertEqual(2, hero.GetComboActions().Count, "burned out during the fight", ref _run, ref _passed, ref _failed);
            CombatTriggerContext.EndBattle();
            TestBase.AssertEqual(3, hero.GetComboActions().Count, "the strip is whole again after the fight", ref _run, ref _passed, ref _failed);
            CombatTriggerContext.ResetForBattle();
            TestBase.AssertEqual(3, hero.GetComboActions().Count, "and whole at the next fight's start", ref _run, ref _passed, ref _failed);
        }

        private static void TestLastCardNeverBurnsOut()
        {
            TestBase.SetCurrentTestName(nameof(TestLastCardNeverBurnsOut));
            CombatTriggerContext.ResetForBattle();
            var hero = MakeHero(("A", 1));
            TestBase.AssertFalse(FragileCards.AfterSwing(hero, hero.GetComboActions()[0], null), "the only card stays", ref _run, ref _passed, ref _failed);
            TestBase.AssertEqual(1, hero.GetComboActions().Count, "strip keeps its last card", ref _run, ref _passed, ref _failed);
        }

        private static void TestJamSkipsNextCard()
        {
            TestBase.SetCurrentTestName(nameof(TestJamSkipsNextCard));
            CombatTriggerContext.ResetForBattle();
            var foe = MakeHero(("A", 0), ("B", 0), ("C", 0));
            foe.ComboStep = 1; // B is next
            var log = new List<string>();
            TestBase.AssertTrue(FragileCards.Jam(foe, "JAB", log), "Jam applies", ref _run, ref _passed, ref _failed);
            var live = foe.GetComboActions();
            TestBase.AssertEqual("C", live[foe.ComboStep % live.Count].Name, "B is skipped, C is next", ref _run, ref _passed, ref _failed);
            TestBase.AssertEqual(3, live.Count, "Jam takes nothing off the strip", ref _run, ref _passed, ref _failed);
            TestBase.AssertTrue(CombatTriggerContext.GetStripState(foe)!.WasSkippedThisRun(1), "the jammed card shows as skipped", ref _run, ref _passed, ref _failed);
            var single = MakeHero(("A", 0));
            TestBase.AssertFalse(FragileCards.Jam(single, "JAB", log), "a one-card strip can't be jammed", ref _run, ref _passed, ref _failed);
        }

        private static void TestBurnOutNeedsLongStrip()
        {
            TestBase.SetCurrentTestName(nameof(TestBurnOutNeedsLongStrip));
            CombatTriggerContext.ResetForBattle();
            var log = new List<string>();
            var shortFoe = MakeHero(("A", 0), ("B", 0));
            TestBase.AssertFalse(FragileCards.BurnOutLast(shortFoe, "SHATTER", log), "Burn out needs 3+ cards", ref _run, ref _passed, ref _failed);
            var longFoe = MakeHero(("A", 0), ("B", 0), ("C", 0));
            TestBase.AssertTrue(FragileCards.BurnOutLast(longFoe, "SHATTER", log), "Burn out on a 3-card strip", ref _run, ref _passed, ref _failed);
            TestBase.AssertEqual("A,B", string.Join(",", longFoe.GetComboActions().Select(a => a.Name)), "the last card is gone", ref _run, ref _passed, ref _failed);
        }

        private static void TestStripOpsHitTheFoe()
        {
            TestBase.SetCurrentTestName(nameof(TestStripOpsHitTheFoe));
            CombatTriggerContext.ResetForBattle();
            var hero = MakeHero(("H1", 0), ("H2", 0), ("H3", 0));
            var foe = MakeHero(("F1", 0), ("F2", 0), ("F3", 0));
            var log = new List<string>();
            var card = new Action { Name = "JAB" };
            TestBase.AssertTrue(StripMutationApplier.IsStripMechanic("strip_jam") && StripMutationApplier.IsStripMechanic("strip_burnout"),
                "jam and burnout are strip mechanics", ref _run, ref _passed, ref _failed);
            TestBase.AssertTrue(StripMutationApplier.TryApply("strip_jam", null, "1", card, hero, log, foe), "strip_jam applies to the foe", ref _run, ref _passed, ref _failed);
            TestBase.AssertEqual(1, foe.ComboStep, "the foe's combo jumped", ref _run, ref _passed, ref _failed);
            TestBase.AssertEqual(0, hero.ComboStep, "the source's combo did not", ref _run, ref _passed, ref _failed);
            TestBase.AssertFalse(StripMutationApplier.TryApply("strip_jam", null, "1", card, hero, log, null), "no target, no jam", ref _run, ref _passed, ref _failed);
            TestBase.AssertTrue(StripMutationApplier.TryApply("strip_burnout", null, "1", card, hero, log, foe), "strip_burnout applies to the foe", ref _run, ref _passed, ref _failed);
            TestBase.AssertEqual(2, foe.GetComboActions().Count, "the foe lost a card", ref _run, ref _passed, ref _failed);
            TestBase.AssertEqual(3, hero.GetComboActions().Count, "the source kept its strip", ref _run, ref _passed, ref _failed);
        }

        private static void TestDataCards()
        {
            TestBase.SetCurrentTestName(nameof(TestDataCards));
            foreach (var (name, uses) in new[] { ("SHATTERING BLOW", 1), ("LAST OATH", 2), ("SNAPPING EDGE", 2), ("GLASS NOVA", 1) })
            {
                var a = ActionLoader.GetAction(name);
                TestBase.AssertTrue(a != null && a.Fragile == uses, $"{name} loads as Fragile {uses}", ref _run, ref _passed, ref _failed);
            }
            foreach (string name in new[] { "JAB", "BLUDGEON", "SHOULDER CHECK" })
            {
                var a = ActionLoader.GetAction(name);
                var rules = a == null ? new List<CrossWireRule>() : ActionRules.For(a).ToList();
                TestBase.AssertTrue(rules.Any(r => r.Verb == "STRIP" && r.Store.StartsWith("strip_jam", StringComparison.Ordinal)),
                    $"{name} jams (was Disrupt)", ref _run, ref _passed, ref _failed);
            }
        }

        private static void TestCardLineShowsFragile()
        {
            TestBase.SetCurrentTestName(nameof(TestCardLineShowsFragile));
            var nova = ActionLoader.GetAction("GLASS NOVA");
            TestBase.AssertTrue(ActionRoles.CardLine(nova, "").StartsWith("Payoff · Fragile 1", StringComparison.Ordinal),
                $"GLASS NOVA's card says Fragile 1 ({ActionRoles.CardLine(nova, "")})", ref _run, ref _passed, ref _failed);
        }

        private static void TestBuildMomentCallouts()
        {
            TestBase.SetCurrentTestName(nameof(TestBuildMomentCallouts));
            CombatTriggerContext.ResetForBattle();
            var hero = MakeHero(("A", 0), ("B", 1), ("C", 0));
            var enemy = MakeHero(("E", 0));
            using var session = new FernisFightSession(hero, enemy, subscribe: false);
            session.OnRuleFired(hero, new RuleFire("Iron", RuleOwnerKind.Set, "ONHIT -> MINT X 1", "hit", 0));
            session.OnRuleFired(hero, new RuleFire("Iron", RuleOwnerKind.Set, "ONHIT -> MINT X 1", "hit", 0));
            session.OnRuleFired(hero, new RuleFire("Thorned Gauntlets", RuleOwnerKind.Item, "r", "hit", 0));
            session.OnRuleFired(hero, new RuleFire("Bloodrite", RuleOwnerKind.Skill, "r", "hit", 0, Capstone: true));
            session.OnRuleFired(hero, new RuleFire("STAB", RuleOwnerKind.Action, "r", "hit", 0));
            session.OnRuleFired(enemy, new RuleFire("Other", RuleOwnerKind.Item, "r", "hit", 0));
            session.OnCardBurnedOut(hero, hero.GetComboActions()[1]);
            var callouts = session.Callouts;
            TestBase.AssertEqual(4, callouts.Count, $"one callout each: set, item, capstone, burnout ({string.Join(" | ", callouts)})", ref _run, ref _passed, ref _failed);
            TestBase.AssertTrue(callouts.Any(c => c.Contains("Iron set")), "set callout", ref _run, ref _passed, ref _failed);
            TestBase.AssertTrue(callouts.Any(c => c.Contains("capstone Bloodrite")), "capstone callout", ref _run, ref _passed, ref _failed);
            TestBase.AssertTrue(hero.Unlocks.HasSeen("moment", "set:Iron"), "the moment is remembered for the run", ref _run, ref _passed, ref _failed);

            using var later = new FernisFightSession(hero, enemy, subscribe: false);
            later.OnRuleFired(hero, new RuleFire("Iron", RuleOwnerKind.Set, "r", "hit", 0));
            TestBase.AssertEqual(0, later.Callouts.Count, "the next fight doesn't repeat it", ref _run, ref _passed, ref _failed);
        }
    }
}
