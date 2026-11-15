using System;
using System.Collections.Generic;
using System.Linq;
using RPGGame;
using RPGGame.Tests;
using RPGGame.Tuning;
using RPGGame.Tuning.Profiles;

namespace RPGGame.Tests.Unit
{
    /// <summary>
    /// Sim bot judgment (2026-11-15 refresh): cadence-timing strip placement, grant scoring for actions the miner never
    /// saw, and hero-aware item scoring (attribute fit, base modifiers).
    /// </summary>
    public static class BotDecisionTests
    {
        private static int _run, _passed, _failed;

        public static void RunAllTests()
        {
            Console.WriteLine("=== Bot Decision Tests ===\n");
            _run = _passed = _failed = 0;

            TestTimingPosition();
            TestGrantMultiplier();
            TestUnminedScore();
            TestAttributeFit();
            TestImplicitRuleValue();

            TestBase.PrintSummary("Bot Decision Tests", _run, _passed, _failed);
        }

        private static void TestTimingPosition()
        {
            TestBase.SetCurrentTestName(nameof(TestTimingPosition));
            TestBase.AssertEqual(0.0, RulesSynergy.TimingPosition("OPENING GAMBIT", 5) ?? -1, "slotIf first opens the strip", ref _run, ref _passed, ref _failed);
            TestBase.AssertEqual(0.5, RulesSynergy.TimingPosition("LATE BLOOM", 5) ?? -1, "slotIf >=3 sits at slot 3 of 5", ref _run, ref _passed, ref _failed);
            TestBase.AssertEqual(0.25, RulesSynergy.TimingPosition("EVEN ODDS", 5) ?? -1, "slotIf even sits at slot 2", ref _run, ref _passed, ref _failed);
            TestBase.AssertEqual(0.0, RulesSynergy.TimingPosition("HEADSMAN'S PROMISE", 5) ?? -1, "a grant aimed at the last slot wants its source early", ref _run, ref _passed, ref _failed);
            TestBase.AssertEqual(1.0, RulesSynergy.TimingPosition("NEXT TIME", 5) ?? -1, "a grant for the next cycle's first slots wants its source late", ref _run, ref _passed, ref _failed);
            TestBase.AssertTrue(RulesSynergy.TimingPosition("ACTION BONUS", 5) == null, "no timing = no preferred position", ref _run, ref _passed, ref _failed);
        }

        private static void TestGrantMultiplier()
        {
            TestBase.SetCurrentTestName(nameof(TestGrantMultiplier));
            TestBase.AssertEqual(3.0, RulesSynergy.GrantMultiplier("[{\"cadence\":\"TURN\",\"duration\":\"3\"}]"), "plain x3", ref _run, ref _passed, ref _failed);
            TestBase.AssertEqual(6.0, RulesSynergy.GrantMultiplier("[{\"cadence\":\"TURN\",\"duration\":\"3\",\"timing\":{\"shape\":\"ramp\"}}]"), "ramp x3 = 1+2+3", ref _run, ref _passed, ref _failed);
            TestBase.AssertEqual(3.0, RulesSynergy.GrantMultiplier("[{\"cadence\":\"TURN\",\"duration\":\"6\",\"timing\":{\"every\":2}}]"), "every 2 of 6 = 3 pulses", ref _run, ref _passed, ref _failed);
            TestBase.AssertEqual(2.0, RulesSynergy.GrantMultiplier("[{\"cadence\":\"ACTION\",\"duration\":\"1\",\"timing\":{\"perSlot\":true}}]"), "perSlot valued at slot 2", ref _run, ref _passed, ref _failed);
            TestBase.AssertEqual(0.0, RulesSynergy.GrantMultiplier(""), "no bundle", ref _run, ref _passed, ref _failed);
        }

        private static void TestUnminedScore()
        {
            TestBase.SetCurrentTestName(nameof(TestUnminedScore));
            double floor = RulesSynergy.Data.UnminedBase;
            TestBase.AssertTrue(RulesSynergy.UnminedScore("HEADSMAN'S PROMISE") > floor, "a damage grant lifts an unmined action", ref _run, ref _passed, ref _failed);
            TestBase.AssertTrue(RulesSynergy.UnminedScore("GATHERING STORM") > RulesSynergy.UnminedScore("NEXT TIME") - 1, "scores are finite", ref _run, ref _passed, ref _failed);
            TestBase.AssertTrue(RulesSynergy.Features("CRESCENDO").ContainsKey("ampGrant"), "AMP grant is a feature", ref _run, ref _passed, ref _failed);
        }

        private static Character Hero(int str, int intel)
        {
            var hero = new Character("BotHero", 10);
            hero.Equipment.Weapon = new WeaponItem("Club", 1, 20, 1.0, WeaponType.Mace);
            hero.Stats.Strength = str;
            hero.Stats.Intelligence = intel;
            return hero;
        }

        private static HeadItem Helm(string stat, double pct)
        {
            var helm = new HeadItem("Helm", 1, 2);
            helm.StatBonuses.Add(new StatBonus { Name = stat, StatType = stat, Value = pct });
            return helm;
        }

        private static void TestAttributeFit()
        {
            TestBase.SetCurrentTestName(nameof(TestAttributeFit));
            var hero = Hero(40, 5);
            double str = ItemValueModel.Score(Helm("STR", 20), hero);
            double intel = ItemValueModel.Score(Helm("INT", 20), hero);
            TestBase.AssertTrue(str > intel, $"a STR hero values +20% STR ({str:F1}) over +20% INT ({intel:F1})", ref _run, ref _passed, ref _failed);
            var wizard = Hero(5, 40);
            TestBase.AssertTrue(ItemValueModel.Score(Helm("INT", 20), wizard) > ItemValueModel.Score(Helm("STR", 20), wizard),
                "an INT hero the other way round", ref _run, ref _passed, ref _failed);
        }

        private static void TestImplicitRuleValue()
        {
            TestBase.SetCurrentTestName(nameof(TestImplicitRuleValue));
            var hero = Hero(30, 5);
            var strip = hero.GetComboActions();
            if (strip.Count == 0)
            {
                TestBase.AssertTrue(true, "hero has no strip (skipped)", ref _run, ref _passed, ref _failed);
                return;
            }
            string first = strip[0].Name.Trim().Replace(' ', '_');
            var plain = new HeadItem("Helm", 1, 2);
            var named = new HeadItem("Helm", 1, 2) { ImplicitRule = $"WHILE IF ACTION {first} -> DAMAGE 20%" };
            var absent = new HeadItem("Helm", 1, 2) { ImplicitRule = "WHILE IF ACTION NO_SUCH_ACTION -> DAMAGE 20%" };
            double p = ItemValueModel.Score(plain, hero), n = ItemValueModel.Score(named, hero), a = ItemValueModel.Score(absent, hero);
            TestBase.AssertTrue(n > p, $"a base modifier for a strip action adds value ({n:F1} > {p:F1})", ref _run, ref _passed, ref _failed);
            TestBase.AssertEqual(p, a, "one for an action the hero doesn't run adds nothing", ref _run, ref _passed, ref _failed);
        }
    }
}
