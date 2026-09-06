using System;
using System.Collections.Generic;
using System.Linq;
using RPGGame;
using RPGGame.Actions.RollModification;
using RPGGame.Combat.Sequence;
using RPGGame.Combat.UI;
using RPGGame.Tests;
using RPGGame.UI.ColorSystem;

namespace RPGGame.Tests.Unit.Combat
{
    public static class CombatSequenceBuilderTests
    {
        private static int _run, _passed, _failed;

        public static void RunAllTests()
        {
            Console.WriteLine("=== CombatSequenceBuilder Tests ===\n");
            _run = _passed = _failed = 0;

            TestHitIncludesActionRollOutcomeDamage();
            TestMissSkipsDefenseAndDamage();
            TestCritComboLabel();
            TestComboShowsActionName();
            TestCritShowsActionName();
            TestLuckRollText();
            TestRollLuckMathBeatsAreSequential();
            TestOutcomeMathBeatsClimbHitLadder();
            TestDamageMathBeatsWalkTheFormula();
            TestDefenseStepWhenHeroFacePresent();
            TestHealStep();
            TestUnnamedHitActionIsNA();
            TestEnvironmentalActionAndDamage();
            TestSnapshotHealthHoldBeforeDamage();
            TestEmptyWhenNoAction();

            TestBase.PrintSummary("CombatSequenceBuilder Tests", _run, _passed, _failed);
        }

        private static void TestHitIncludesActionRollOutcomeDamage()
        {
            Console.WriteLine("--- Hit: ATTACKER, ROLL, OUTCOME, ACTION, DAMAGE ---");
            var result = HitResult("STRIKE", damage: 12);
            var hero = DummyHero();
            var enemy = DummyEnemy();
            var steps = CombatSequenceBuilder.From(result, hero, enemy);
            AssertKinds(steps, CombatSequenceStepKind.Attacker, CombatSequenceStepKind.Roll,
                CombatSequenceStepKind.Outcome, CombatSequenceStepKind.Action, CombatSequenceStepKind.Damage);
            TestBase.AssertTrue(Plain(steps[0]).Contains("SeqHero", System.StringComparison.Ordinal),
                "ATTACKER is the actor name", ref _run, ref _passed, ref _failed);
            var action = steps.First(s => s.Kind == CombatSequenceStepKind.Action);
            TestBase.AssertTrue(Plain(action) == "N/A",
                "plain HIT ACTION is N/A because no strip action was used", ref _run, ref _passed, ref _failed);
            TestBase.AssertTrue(Plain(steps[2]).Contains("HIT", System.StringComparison.Ordinal),
                "OUTCOME is HIT", ref _run, ref _passed, ref _failed);
            TestBase.AssertTrue(Plain(steps[4]).Contains("12", System.StringComparison.Ordinal),
                "DAMAGE shows 12", ref _run, ref _passed, ref _failed);
            TestBase.AssertTrue(steps[4].Cue == CombatSequenceCue.HealthBar,
                "DAMAGE cue is HealthBar", ref _run, ref _passed, ref _failed);
        }

        private static void TestMissSkipsDefenseAndDamage()
        {
            Console.WriteLine("--- Miss skips DEFENSE and DAMAGE ---");
            var result = new ActionExecutionResult
            {
                SelectedAction = TestDataBuilders.CreateMockAction("JAB", ActionType.Attack),
                Hit = false,
                ModifiedBaseRoll = 4,
                AttackRoll = 4
            };
            var steps = CombatSequenceBuilder.From(result, DummyHero(), DummyEnemy());
            AssertKinds(steps, CombatSequenceStepKind.Attacker, CombatSequenceStepKind.Roll,
                CombatSequenceStepKind.Outcome, CombatSequenceStepKind.Action);
            TestBase.AssertTrue(Plain(steps[2]).Contains("MISS", System.StringComparison.Ordinal),
                "OUTCOME is MISS", ref _run, ref _passed, ref _failed);
            TestBase.AssertTrue(steps.All(s => s.Kind != CombatSequenceStepKind.Defense && s.Kind != CombatSequenceStepKind.Damage),
                "miss has no defense or damage step", ref _run, ref _passed, ref _failed);
        }

        private static void TestCritComboLabel()
        {
            Console.WriteLine("--- Crit combo outcome label ---");
            var result = HitResult("FINISHER", damage: 20);
            result.IsCritical = true;
            result.IsCombo = true;
            result.SelectedAction!.IsComboAction = true;
            var steps = CombatSequenceBuilder.From(result, DummyHero(), DummyEnemy());
            var outcome = steps.First(s => s.Kind == CombatSequenceStepKind.Outcome);
            TestBase.AssertTrue(Plain(outcome).Contains("CRIT COMBO", System.StringComparison.Ordinal),
                "crit+combo label", ref _run, ref _passed, ref _failed);
        }

        private static void TestComboShowsActionName()
        {
            Console.WriteLine("--- Combo ACTION is the attack name ---");
            var result = HitResult("STRIKE", damage: 12);
            result.IsCombo = true;
            result.SelectedAction!.IsComboAction = true;
            var action = CombatSequenceBuilder.From(result, DummyHero(), DummyEnemy())
                .First(s => s.Kind == CombatSequenceStepKind.Action);
            TestBase.AssertTrue(Plain(action).Contains("STRIKE", System.StringComparison.Ordinal),
                "combo ACTION is the attack name", ref _run, ref _passed, ref _failed);
        }

        private static void TestCritShowsActionName()
        {
            Console.WriteLine("--- Crit ACTION is the attack name ---");
            var result = HitResult("STRIKE", damage: 12);
            result.IsCritical = true;
            var action = CombatSequenceBuilder.From(result, DummyHero(), DummyEnemy())
                .First(s => s.Kind == CombatSequenceStepKind.Action);
            TestBase.AssertTrue(Plain(action).Contains("STRIKE", System.StringComparison.Ordinal),
                "crit ACTION is the attack name", ref _run, ref _passed, ref _failed);
        }

        private static void TestLuckRollText()
        {
            Console.WriteLine("--- Luck roll text ---");
            var result = HitResult("STRIKE", damage: 8);
            result.ModifiedBaseRoll = 18;
            result.MultiDiceRollDetail = MultiDiceRollDetail.FromTwoDice(MultiDiceLuckMode.Advantage, 18, 5);
            var steps = CombatSequenceBuilder.From(result, DummyHero(), DummyEnemy());
            var roll = steps.First(s => s.Kind == CombatSequenceStepKind.Roll);
            string joined = JoinBeats(roll);
            TestBase.AssertTrue(joined.Contains("18/5", System.StringComparison.Ordinal)
                && joined.Contains("18", System.StringComparison.Ordinal),
                $"luck roll math beats show both dice then keep, got: {joined}", ref _run, ref _passed, ref _failed);
        }

        private static void TestRollLuckMathBeatsAreSequential()
        {
            Console.WriteLine("--- ROLL math beats: dice then keep then bonus ---");
            var result = HitResult("STRIKE", damage: 8);
            result.ModifiedBaseRoll = 18;
            result.AttackRoll = 20;
            result.RollBonus = 2;
            result.MultiDiceRollDetail = MultiDiceRollDetail.FromTwoDice(MultiDiceLuckMode.Advantage, 18, 5);
            var roll = CombatSequenceBuilder.From(result, DummyHero(), DummyEnemy())
                .First(s => s.Kind == CombatSequenceStepKind.Roll);
            TestBase.AssertTrue(roll.MathBeats.Count >= 3,
                "luck + bonus is at least three math beats", ref _run, ref _passed, ref _failed);
            TestBase.AssertTrue(PlainBeat(roll, 0).Contains("18/5", System.StringComparison.Ordinal),
                "first beat is the two dice", ref _run, ref _passed, ref _failed);
            TestBase.AssertTrue(PlainBeat(roll, 1).Contains("18", System.StringComparison.Ordinal),
                "second beat keeps 18", ref _run, ref _passed, ref _failed);
            TestBase.AssertTrue(PlainBeat(roll, roll.MathBeats.Count - 1).Contains("20", System.StringComparison.Ordinal),
                "last beat is the total", ref _run, ref _passed, ref _failed);
        }

        private static void TestOutcomeMathBeatsClimbHitLadder()
        {
            Console.WriteLine("--- OUTCOME math beats climb the threshold ladder ---");
            var result = HitResult("STRIKE", damage: 12);
            result.AttackRoll = 15;
            result.IsCombo = true;
            result.ResolvedHitThreshold = 5;
            result.ResolvedComboThreshold = 14;
            var outcome = CombatSequenceBuilder.From(result, DummyHero(), DummyEnemy())
                .First(s => s.Kind == CombatSequenceStepKind.Outcome);
            string joined = JoinBeats(outcome);
            TestBase.AssertTrue(joined.Contains("≥", System.StringComparison.Ordinal)
                && joined.Contains("6", System.StringComparison.Ordinal)
                && joined.Contains("14", System.StringComparison.Ordinal)
                && joined.Contains("COMBO", System.StringComparison.Ordinal),
                $"combo climb shows hit then combo then label, got: {joined}", ref _run, ref _passed, ref _failed);
        }

        private static void TestDamageMathBeatsWalkTheFormula()
        {
            Console.WriteLine("--- DAMAGE math beats walk base × action − block ---");
            var result = HitResult("SLAM", damage: 27);
            result.SelectedAction!.DamageMultiplier = 1.18;
            result.DamageTrace = new CombatSequenceDamageTrace
            {
                BaseDamage = 23,
                ActionMultiplier = 1.18,
                Amp = 1.0,
                Block = 5,
                Raw = 32,
                Final = 27
            };
            var damage = CombatSequenceBuilder.From(result, DummyHero(), DummyEnemy())
                .First(s => s.Kind == CombatSequenceStepKind.Damage);
            string joined = JoinBeats(damage);
            TestBase.AssertTrue(joined.Contains("23", System.StringComparison.Ordinal)
                && joined.Contains("1.18", System.StringComparison.Ordinal)
                && joined.Contains("block", System.StringComparison.OrdinalIgnoreCase)
                && joined.Contains("27", System.StringComparison.Ordinal),
                $"damage formula beats, got: {joined}", ref _run, ref _passed, ref _failed);
        }

        private static void TestDefenseStepWhenHeroFacePresent()
        {
            Console.WriteLine("--- Defense step when hero defense face is set ---");
            var hero = DummyHero();
            var result = HitResult("SLAM", damage: 5);
            result.DefenseFace = 8;
            result.ModifiedBaseRoll = 12;
            var steps = CombatSequenceBuilder.From(result, DummyEnemy(), hero);
            TestBase.AssertTrue(steps.Any(s => s.Kind == CombatSequenceStepKind.Defense),
                "defense step present", ref _run, ref _passed, ref _failed);
            var def = steps.First(s => s.Kind == CombatSequenceStepKind.Defense);
            string joined = JoinBeats(def);
            TestBase.AssertTrue(joined.Contains("def", System.StringComparison.OrdinalIgnoreCase)
                && joined.Contains("8", System.StringComparison.Ordinal)
                && joined.Contains("block", System.StringComparison.OrdinalIgnoreCase),
                $"defense math beats show def/block, got: {joined}", ref _run, ref _passed, ref _failed);
        }

        private static void TestHealStep()
        {
            Console.WriteLine("--- Heal step ---");
            var result = new ActionExecutionResult
            {
                SelectedAction = TestDataBuilders.CreateMockAction("BANDAGE", ActionType.Heal),
                Hit = true,
                HealAmount = 7,
                ModifiedBaseRoll = 11
            };
            var steps = CombatSequenceBuilder.From(result, DummyHero(), DummyHero());
            TestBase.AssertTrue(steps.Any(s => s.Kind == CombatSequenceStepKind.Heal),
                "heal step present", ref _run, ref _passed, ref _failed);
            TestBase.AssertTrue(!steps.Any(s => s.Kind == CombatSequenceStepKind.Defense),
                "heal has no defense step", ref _run, ref _passed, ref _failed);
            var heal = steps.First(s => s.Kind == CombatSequenceStepKind.Heal);
            TestBase.AssertTrue(Plain(heal).Contains("7", System.StringComparison.Ordinal),
                "heal amount 7", ref _run, ref _passed, ref _failed);
        }

        private static void TestUnnamedHitActionIsNA()
        {
            Console.WriteLine("--- Unnamed ACTION is N/A on hit, miss on miss ---");
            var hitSteps = CombatSequenceBuilder.From(HitResult("", damage: 3), DummyHero(), DummyEnemy());
            var hitAction = hitSteps.First(s => s.Kind == CombatSequenceStepKind.Action);
            TestBase.AssertTrue(Plain(hitAction) == "N/A",
                "unnamed hit ACTION is N/A", ref _run, ref _passed, ref _failed);

            var missResult = new ActionExecutionResult
            {
                SelectedAction = TestDataBuilders.CreateMockAction("", ActionType.Attack),
                Hit = false,
                ModifiedBaseRoll = 4,
                AttackRoll = 4
            };
            var missSteps = CombatSequenceBuilder.From(missResult, DummyHero(), DummyEnemy());
            var missAction = missSteps.First(s => s.Kind == CombatSequenceStepKind.Action);
            TestBase.AssertTrue(Plain(missAction) == "miss",
                "unnamed miss ACTION is miss", ref _run, ref _passed, ref _failed);
        }

        private static void TestEnvironmentalActionAndDamage()
        {
            Console.WriteLine("--- Environmental Setup + Action + Damage ---");
            var hero = DummyHero();
            var steps = CombatSequenceBuilder.FromEnvironmental("ROOM COLLAPSE", 4, defenseFace: 11, hero, attackFace: 11);
            TestBase.AssertTrue(steps[0].Kind == CombatSequenceStepKind.Attacker,
                "env ATTACKER first", ref _run, ref _passed, ref _failed);
            var action = steps.First(s => s.Kind == CombatSequenceStepKind.Action);
            TestBase.AssertTrue(Plain(action).Contains("ROOM COLLAPSE", System.StringComparison.Ordinal),
                "env ACTION name", ref _run, ref _passed, ref _failed);
            TestBase.AssertTrue(steps.Any(s => s.Kind == CombatSequenceStepKind.Defense),
                "env with defense face includes DEFENSE", ref _run, ref _passed, ref _failed);
            TestBase.AssertTrue(steps.Any(s => s.Kind == CombatSequenceStepKind.Damage && Plain(s).Contains("4")),
                "env DAMAGE 4", ref _run, ref _passed, ref _failed);
        }

        private static void TestSnapshotHealthHoldBeforeDamage()
        {
            Console.WriteLine("--- SnapshotHealthHolds captures pre-swing HP ---");
            var enemy = DummyEnemy();
            int before = enemy.CurrentHealth;
            var result = HitResult("STRIKE", damage: 1);
            CombatSequenceBuilder.SnapshotHealthHolds(result, DummyHero(), enemy);
            TestBase.AssertTrue(result.HealthBarHolds.Count == 1, "one hold", ref _run, ref _passed, ref _failed);
            if (result.HealthBarHolds.Count == 1)
            {
                TestBase.AssertEqual(HealthBarEntityId.ForActor(enemy) ?? "", result.HealthBarHolds[0].EntityId,
                    "hold id is enemy bar", ref _run, ref _passed, ref _failed);
                TestBase.AssertEqual(before, result.HealthBarHolds[0].Health,
                    "hold is pre-damage HP", ref _run, ref _passed, ref _failed);
            }
        }

        private static void TestEmptyWhenNoAction()
        {
            Console.WriteLine("--- Empty when SelectedAction is null ---");
            var steps = CombatSequenceBuilder.From(new ActionExecutionResult(), DummyHero(), DummyEnemy());
            TestBase.AssertEqual(0, steps.Count, "no steps without an action", ref _run, ref _passed, ref _failed);
        }

        private static ActionExecutionResult HitResult(string name, int damage)
        {
            var action = TestDataBuilders.CreateMockAction(name, ActionType.Attack);
            return new ActionExecutionResult
            {
                SelectedAction = action,
                Hit = true,
                Damage = damage,
                ModifiedBaseRoll = 15,
                AttackRoll = 15,
                ResolvedMultiHitCount = 1
            };
        }

        private static Character DummyHero() => TestDataBuilders.CreateTestCharacter("SeqHero", 1);

        private static Enemy DummyEnemy() => TestDataBuilders.Enemy().WithName("SeqFoe").Build();

        private static string Plain(CombatSequenceStep step) =>
            ColoredTextRenderer.RenderAsPlainText(step.Result);

        private static string PlainBeat(CombatSequenceStep step, int index) =>
            ColoredTextRenderer.RenderAsPlainText(step.MathBeats[index]);

        private static string JoinBeats(CombatSequenceStep step) =>
            string.Join(" | ", step.MathBeats.Select(ColoredTextRenderer.RenderAsPlainText));

        private static void AssertKinds(List<CombatSequenceStep> steps, params CombatSequenceStepKind[] expected)
        {
            var actual = steps.Select(s => s.Kind).ToList();
            TestBase.AssertEqual(expected.Length, actual.Count,
                $"step count ({string.Join(",", actual)})", ref _run, ref _passed, ref _failed);
            for (int i = 0; i < expected.Length && i < actual.Count; i++)
            {
                TestBase.AssertTrue(actual[i] == expected[i],
                    $"step {i} is {expected[i]} (got {actual[i]})", ref _run, ref _passed, ref _failed);
            }
        }
    }
}
