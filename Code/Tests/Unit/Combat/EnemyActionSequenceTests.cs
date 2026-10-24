using System;
using System.Linq;
using RPGGame.Tests;

namespace RPGGame.Tests.Unit.Combat
{
    /// <summary>
    /// Enemy action sequence: built from the combo-flagged pool actions in pool order (the list combat used as a fallback
    /// before enemies had an explicit sequence), so the board's enemy strip shows what the enemy actually swings.
    /// </summary>
    public static class EnemyActionSequenceTests
    {
        public static void RunAllTests()
        {
            Console.WriteLine("=== EnemyActionSequence Tests ===\n");
            int run = 0, passed = 0, failed = 0;

            TestSingleActionEnemyHasOneSlot(ref run, ref passed, ref failed);
            TestSequenceKeepsPoolOrderAndSkipsNonCombo(ref run, ref passed, ref failed);
            TestSequenceMatchesCombatActions(ref run, ref passed, ref failed);

            TestBase.PrintSummary("EnemyActionSequence Tests", run, passed, failed);
        }

        private static Enemy MakeEnemy(params Action[] pool)
        {
            var enemy = new Enemy("Seq Test", level: 1, strength: 8);
            enemy.ActionPool.Clear();
            foreach (var a in pool)
                enemy.AddAction(a, 1.0);
            enemy.InitializeActionSequence();
            return enemy;
        }

        private static void TestSingleActionEnemyHasOneSlot(ref int run, ref int passed, ref int failed)
        {
            var enemy = MakeEnemy(new Action("BITE"));
            var seq = enemy.GetComboActions();
            TestBase.AssertTrue(seq.Count == 1 && seq[0].Name == "BITE",
                "One-action enemy has a one-slot sequence", ref run, ref passed, ref failed);
        }

        private static void TestSequenceKeepsPoolOrderAndSkipsNonCombo(ref int run, ref int passed, ref int failed)
        {
            // ComboOrder authored out of order must not reshuffle the enemy's data order.
            var enemy = MakeEnemy(
                new Action("WEB STRIKE", comboOrder: 5),
                new Action("SWAT", isComboAction: false),
                new Action("POISON BITE", comboOrder: 1));
            var names = enemy.GetComboActions().Select(a => a.Name).ToList();
            TestBase.AssertTrue(names.SequenceEqual(new[] { "WEB STRIKE", "POISON BITE" }),
                $"Sequence keeps pool order, skips non-combo (got {string.Join(", ", names)})", ref run, ref passed, ref failed);
            TestBase.AssertTrue(enemy.ComboStep == 0, "Sequence starts at slot 1", ref run, ref passed, ref failed);
        }

        private static void TestSequenceMatchesCombatActions(ref int run, ref int passed, ref int failed)
        {
            var enemy = MakeEnemy(new Action("JAB"), new Action("TAUNT"));
            var strip = enemy.GetComboActions();
            var combat = ActionUtilities.GetComboActions(enemy);
            TestBase.AssertTrue(strip.Count == combat.Count && strip.Zip(combat).All(p => ReferenceEquals(p.First, p.Second)),
                "Strip sequence is the list combat swings", ref run, ref passed, ref failed);
        }
    }
}
