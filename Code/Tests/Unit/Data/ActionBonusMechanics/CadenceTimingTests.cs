using System;
using System.Collections.Generic;
using System.Linq;
using RPGGame;
using RPGGame.Actions.Execution;
using RPGGame.Data;

namespace RPGGame.Tests.Unit.Data.ActionBonusMechanics
{
    /// <summary>
    /// Cadence timing (2026-11-15): ACTION ×N spreads over the next N strip slots; slot / delay / shape / every /
    /// perSlot / slotIf / untilMiss on TURN and ACTION grants; the SLOT rule key; the timing actions load with their timing.
    /// </summary>
    public static class CadenceTimingTests
    {
        private static int _run, _passed, _failed;

        public static void RunAll(ref int testsRun, ref int testsPassed, ref int testsFailed)
        {
            _run = _passed = _failed = 0;
            Console.WriteLine("\n=== Cadence timing ===\n");
            TestSlotIf();
            TestResolveActionSlots();
            TestActionSpreadOverSlots();
            TestActionChargeKeepsBurst();
            TestActionSlotTargetAndDelay();
            TestActionRampAndPerSlot();
            TestActionSlotIfGates();
            TestTurnDelay();
            TestTurnRampDecay();
            TestTurnEvery();
            TestTurnUntilMiss();
            TestTurnSlotIfAndPerSlot();
            TestRuleSlotKey();
            TestTimingActionsLoad();
            testsRun += _run;
            testsPassed += _passed;
            testsFailed += _failed;
        }

        private static void Check(bool ok, string msg) => TestBase.AssertTrue(ok, msg, ref _run, ref _passed, ref _failed);

        private static ActionAttackBonusGroup Group(string cadence, int count, CadenceTiming? timing, string type = "DAMAGE_MOD", double value = 10) =>
            new ActionAttackBonusGroup
            {
                Keyword = cadence, CadenceType = cadence, DurationType = cadence, Count = count, Timing = timing,
                Bonuses = new List<ActionAttackBonusItem> { new ActionAttackBonusItem { Type = type, Value = value } }
            };

        private static (Character hero, List<RPGGame.Action> strip) Hero(int slots)
        {
            var hero = TestDataBuilders.Character().WithName("TimingHero").WithStats(10, 10, 10, 10).Build();
            var strip = new List<RPGGame.Action>();
            for (int i = 0; i < slots; i++)
            {
                var a = TestDataBuilders.CreateMockAction("TIMING SLOT " + (i + 1), ActionType.Attack);
                a.IsComboAction = true;
                hero.AddAction(a, 1.0);
                hero.Actions.AddToCombo(a);
                strip.Add(a);
            }
            return (hero, ActionUtilities.GetComboActions(hero));
        }

        private static double SlotValue(Character hero, int slot0, string type = "DAMAGE_MOD") =>
            hero.Effects.GetPendingActionBonusesForSlot(slot0).Where(b => b.Type == type).Sum(b => b.Value);

        private static double Sum(List<ActionAttackBonusItem> items, string type = "DAMAGE_MOD") =>
            items.Where(b => b.Type == type).Sum(b => b.Value);

        private static void TestSlotIf()
        {
            Check(new CadenceTiming { SlotIf = "first" }.SlotIfHolds(0, 4), "slotIf first holds in slot 1");
            Check(!new CadenceTiming { SlotIf = "first" }.SlotIfHolds(1, 4), "slotIf first fails in slot 2");
            Check(new CadenceTiming { SlotIf = "last" }.SlotIfHolds(3, 4), "slotIf last holds in slot 4 of 4");
            Check(new CadenceTiming { SlotIf = ">=3" }.SlotIfHolds(2, 4) && !new CadenceTiming { SlotIf = ">=3" }.SlotIfHolds(1, 4), "slotIf >=3");
            Check(new CadenceTiming { SlotIf = "even" }.SlotIfHolds(1, 4) && !new CadenceTiming { SlotIf = "even" }.SlotIfHolds(2, 4), "slotIf even");
            Check(new CadenceTiming { SlotIf = "2" }.SlotIfHolds(1, 4) && !new CadenceTiming { SlotIf = "2" }.SlotIfHolds(0, 4), "slotIf 2");
            Check(!new CadenceTiming { SlotIf = "1" }.SlotIfHolds(-1, 0), "slotIf fails off-strip");
        }

        private static void TestResolveActionSlots()
        {
            Check(CadenceTiming.ResolveActionSlots(null, 1, 5, 3).SequenceEqual(new[] { 2, 3, 4 }), "spread from slot 2: slots 3,4,5");
            Check(CadenceTiming.ResolveActionSlots(null, 3, 5, 3).SequenceEqual(new[] { 4, 0, 1 }), "spread wraps past the finisher");
            Check(CadenceTiming.ResolveActionSlots(new CadenceTiming { Delay = 1 }, 0, 5, 2).SequenceEqual(new[] { 2, 3 }), "delay 1 skips the next slot");
            Check(CadenceTiming.ResolveActionSlots(new CadenceTiming { Slot = "last" }, 0, 4, 1).SequenceEqual(new[] { 3 }), "slot last");
            Check(CadenceTiming.ResolveActionSlots(new CadenceTiming { Slot = "5" }, 0, 3, 1).SequenceEqual(new[] { 2 }), "slot 5 on a 3-slot strip lands on the last");
            Check(CadenceTiming.ResolveActionSlots(new CadenceTiming { Slot = "first" }, 2, 4, 2).SequenceEqual(new[] { 0, 1 }), "slot first ×2");
        }

        private static void TestActionSpreadOverSlots()
        {
            var (hero, strip) = Hero(5);
            ActionExecutionFlow.DepositActionCadenceGroup(hero, strip[0], Group("ACTION", 3, null, value: 25), strip, 1);
            Check(SlotValue(hero, 1) == 25 && SlotValue(hero, 2) == 25 && SlotValue(hero, 3) == 25 && SlotValue(hero, 4) == 0,
                "ACTION ×3 from slot 1: +25% on each of slots 2-4");
            Check(!hero.Effects.HasPendingActionCadenceBank(), "ACTION ×3 spread leaves the next-action bank empty");

            var (hero1, strip1) = Hero(3);
            ActionExecutionFlow.DepositActionCadenceGroup(hero1, strip1[0], Group("ACTION", 1, null, value: 25), strip1, 1);
            Check(hero1.Effects.HasPendingActionCadenceBank() && Sum(hero1.Effects.PeekPendingActionBonusesNextHeroRoll()) == 25,
                "ACTION ×1 still uses the next-action bank");

            var (hero2, strip2) = Hero(4);
            ActionExecutionFlow.DepositActionCadenceGroup(hero2, strip2[2], Group("ACTION", 3, null, value: 25), strip2, 3);
            Check(Sum(hero2.Effects.PeekPendingActionBonusesNextHeroRoll()) == 25 && SlotValue(hero2, 0) == 0
                    && hero2.Effects.GetActionCadenceBankPaintSlot(0, 4) == 3,
                "ACTION ×3 from slot 3 of 4 clips to the one slot left (finisher, via the bank), no wrap");
            var (hero3, strip3) = Hero(3);
            ActionExecutionFlow.DepositActionCadenceGroup(hero3, strip3[2], Group("ACTION", 1, null, value: 25), strip3, 0);
            Check(Sum(hero3.Effects.PeekPendingActionBonusesNextHeroRoll()) == 25 && !hero3.Effects.GetPendingActionBonusSlots().Any()
                    && hero3.Effects.GetActionCadenceBankPaintSlot(2, 3) == 0,
                "plain ACTION ×1 from the finisher carries to the next cycle's opener (playtest 11-21: WIND UP)");
        }

        private static void TestActionChargeKeepsBurst()
        {
            var (hero, strip) = Hero(5);
            ActionExecutionFlow.DepositActionCadenceGroup(hero, strip[0], Group("ACTION", 3, new CadenceTiming { Shape = "charge" }, value: 25), strip, 1);
            Check(Sum(hero.Effects.PeekPendingActionBonusesNextHeroRoll()) == 75, "charge ×3: +75% on the next action");
        }

        private static void TestActionSlotTargetAndDelay()
        {
            var (hero, strip) = Hero(5);
            ActionExecutionFlow.DepositActionCadenceGroup(hero, strip[0], Group("ACTION", 1, new CadenceTiming { Slot = "last" }, value: 60), strip, 1);
            Check(SlotValue(hero, 4) == 60 && SlotValue(hero, 1) == 0, "slot last: +60% on the finisher only");

            var (hero2, strip2) = Hero(5);
            ActionExecutionFlow.DepositActionCadenceGroup(hero2, strip2[1], Group("ACTION", 2, new CadenceTiming { Delay = 1 }, "SPEED_MOD", 20), strip2, 2);
            Check(SlotValue(hero2, 2, "SPEED_MOD") == 0 && SlotValue(hero2, 3, "SPEED_MOD") == 20 && SlotValue(hero2, 4, "SPEED_MOD") == 20,
                "delay 1 from slot 2: the 2 actions after next (slots 4, 5)");
        }

        private static void TestActionRampAndPerSlot()
        {
            var (hero, strip) = Hero(5);
            ActionExecutionFlow.DepositActionCadenceGroup(hero, strip[0], Group("ACTION", 4, new CadenceTiming { Shape = "ramp" }, "AMP_MOD", 5), strip, 1);
            Check(SlotValue(hero, 1, "AMP_MOD") == 5 && SlotValue(hero, 2, "AMP_MOD") == 10 && SlotValue(hero, 3, "AMP_MOD") == 15 && SlotValue(hero, 4, "AMP_MOD") == 20,
                "ramp ×4: +5/+10/+15/+20 over the next 4 slots");

            var (hero2, strip2) = Hero(5);
            ActionExecutionFlow.DepositActionCadenceGroup(hero2, strip2[0], Group("ACTION", 3, new CadenceTiming { Shape = "decay" }, "SPEED_MOD", 10), strip2, 1);
            Check(SlotValue(hero2, 1, "SPEED_MOD") == 30 && SlotValue(hero2, 3, "SPEED_MOD") == 10, "decay ×3: +30 then … +10");

            var (hero3, strip3) = Hero(5);
            ActionExecutionFlow.DepositActionCadenceGroup(hero3, strip3[2], Group("ACTION", 1, new CadenceTiming { PerSlot = true }, "AMP_MOD", 10), strip3, 3);
            Check(Sum(hero3.Effects.PeekPendingActionBonusesNextHeroRoll(), "AMP_MOD") == 30, "perSlot from slot 3: +30% AMP");
        }

        private static void TestActionSlotIfGates()
        {
            var (hero, strip) = Hero(4);
            ActionExecutionFlow.DepositActionCadenceGroup(hero, strip[0], Group("ACTION", 2, new CadenceTiming { SlotIf = "even" }), strip, 1);
            Check(hero.Effects.GetPendingActionBonusSlots().Count() == 0, "slotIf even: nothing from slot 1");
            ActionExecutionFlow.DepositActionCadenceGroup(hero, strip[1], Group("ACTION", 2, new CadenceTiming { SlotIf = "even" }), strip, 2);
            Check(SlotValue(hero, 2) == 10 && SlotValue(hero, 3) == 10, "slotIf even: slot 2 grants");
        }

        private static List<List<ActionAttackBonusItem>> Rolls(Character hero, int n) =>
            Enumerable.Range(0, n).Select(_ => hero.Effects.GetAndConsumeTurnBonuses()).ToList();

        private static void GrantTurn(Character hero, ActionAttackBonusGroup g, RPGGame.Action? source = null) =>
            hero.Effects.AddActionAttackBonuses(new ActionAttackBonuses { BonusGroups = new List<ActionAttackBonusGroup> { g } }, source, hero);

        private static void TestTurnDelay()
        {
            var (hero, _) = Hero(1);
            GrantTurn(hero, Group("TURN", 1, new CadenceTiming { Delay = 2 }, value: 100));
            var r = Rolls(hero, 4);
            Check(Sum(r[0]) == 0 && Sum(r[1]) == 0 && Sum(r[2]) == 100 && Sum(r[3]) == 0, "TURN delay 2: lands on the 3rd roll only");
            Check(hero.Effects.SnapshotTurnBonuses().Length == 0, "delayed grant expires after it lands");
        }

        private static void TestTurnRampDecay()
        {
            var (hero, _) = Hero(1);
            GrantTurn(hero, Group("TURN", 3, new CadenceTiming { Shape = "ramp" }));
            Check(Sum(hero.Effects.PeekTurnBonuses()) == 10, "ramp peek shows the next roll's +10");
            var r = Rolls(hero, 4);
            Check(Sum(r[0]) == 10 && Sum(r[1]) == 20 && Sum(r[2]) == 30 && Sum(r[3]) == 0, "TURN ramp ×3: +10/+20/+30");

            GrantTurn(hero, Group("TURN", 3, new CadenceTiming { Shape = "decay" }, "CRIT", 1));
            r = Rolls(hero, 3);
            Check(Sum(r[0], "CRIT") == 3 && Sum(r[1], "CRIT") == 2 && Sum(r[2], "CRIT") == 1, "TURN decay ×3: +3/+2/+1 CRIT");
        }

        private static void TestTurnEvery()
        {
            var (hero, _) = Hero(1);
            GrantTurn(hero, Group("TURN", 6, new CadenceTiming { Every = 2 }, "COMBO", 2));
            var r = Rolls(hero, 7);
            var got = r.Select(x => Sum(x, "COMBO")).ToArray();
            Check(got.SequenceEqual(new double[] { 0, 2, 0, 2, 0, 2, 0 }), "TURN ×6 every 2: +2 COMBO on rolls 2, 4, 6");
        }

        private static void TestTurnUntilMiss()
        {
            var (hero, _) = Hero(1);
            GrantTurn(hero, Group("TURN", 4, new CadenceTiming { UntilMiss = true, Shape = "ramp" }, "CRIT", 1));
            GrantTurn(hero, Group("TURN", 4, null, "HIT", 1));
            hero.Effects.GetAndConsumeTurnBonuses();
            hero.Effects.BreakTurnBonusesOnMiss();
            var next = hero.Effects.GetAndConsumeTurnBonuses();
            Check(Sum(next, "CRIT") == 0 && Sum(next, "HIT") == 1, "untilMiss grant ends on a miss; plain TURN grant survives");
        }

        private static void TestTurnSlotIfAndPerSlot()
        {
            var (hero, strip) = Hero(4);
            GrantTurn(hero, Group("TURN", 3, new CadenceTiming { SlotIf = "first" }, "HIT", 1), strip[1]);
            Check(hero.Effects.SnapshotTurnBonuses().Length == 0, "TURN slotIf first: no grant from slot 2");
            GrantTurn(hero, Group("TURN", 3, new CadenceTiming { SlotIf = "first" }, "HIT", 1), strip[0]);
            Check(Sum(hero.Effects.PeekTurnBonuses(), "HIT") == 1, "TURN slotIf first: grants from slot 1");

            var (hero2, strip2) = Hero(4);
            GrantTurn(hero2, Group("TURN", 1, new CadenceTiming { PerSlot = true }, "CRIT", 1), strip2[3]);
            Check(Sum(hero2.Effects.PeekTurnBonuses(), "CRIT") == 4, "TURN perSlot from slot 4: +4 CRIT");
        }

        private static void TestRuleSlotKey()
        {
            var rule = CrossWireRule.Parse("WHILE IF SLOT>=3 -> DAMAGE 20%", "test");
            Check(rule != null && rule.Conditions.Count == 1 && rule.Conditions[0].Key == "SLOT", "rule parses IF SLOT>=3");
            var (hero, strip) = Hero(4);
            Check(rule != null && CrossWires.ConditionsHold(hero, null, rule, action: strip[2]), "SLOT>=3 holds for the slot-3 action");
            Check(rule != null && !CrossWires.ConditionsHold(hero, null, rule, action: strip[1]), "SLOT>=3 fails for the slot-2 action");
        }

        private static void TestTimingActionsLoad()
        {
            var expected = new (string Name, Func<CadenceTiming, bool> Ok)[]
            {
                ("GATHERING STORM", t => t.IsRamp),
                ("LIT FUSE", t => t.Delay == 2),
                ("HEADSMAN'S PROMISE", t => t.Slot == "last"),
                ("BLOOD FRENZY", t => t.IsRamp && t.UntilMiss),
                ("MARCHING DRUM", t => t.Every == 2),
                ("RELAY STRIKE", t => t.Delay == 1),
                ("OPENING GAMBIT", t => t.SlotIf == "first"),
                ("FLURRY STEP", t => t.IsDecay),
                ("PATIENT BLADE", t => t.Delay == 1),
                ("THIRD CUT", t => t.Slot == "3"),
                ("COUNTED PACES", t => t.PerSlot),
                ("EVEN ODDS", t => t.SlotIf == "even"),
                ("CRESCENDO", t => t.PerSlot),
                ("RUNE OF THE FIFTH", t => t.Slot == "5"),
                ("RISING CHORD", t => t.IsRamp),
                ("LATE BLOOM", t => t.SlotIf == ">=3"),
                ("HOURGLASS", t => t.Delay == 3),
                ("NEXT TIME", t => t.Slot == "first"),
            };
            foreach (var (name, ok) in expected)
            {
                var action = ActionLoader.GetAction(name);
                var timing = action?.ActionAttackBonuses?.BonusGroups?.FirstOrDefault()?.Timing;
                Check(timing != null && ok(timing), $"{name} loads with its timing");
            }
        }
    }
}
