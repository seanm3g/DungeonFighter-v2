using System;
using System.Collections.Generic;
using System.Linq;
using RPGGame.Data;
using RPGGame.Tests;

namespace RPGGame.Tests.Unit.Data
{
    /// <summary>
    /// Live Actions.json: cadence mechanics must carry a value, so a card's bonus lines match what the row points at.
    /// Set DF_DUMP_ACTION_CARDS=1 to print every card (description + cadence tail) for a copy pass.
    /// </summary>
    public static class ActionCardConsistencyTests
    {
        public static void RunAllTests()
        {
            Console.WriteLine("=== ActionCardConsistency Tests ===\n");
            int run = 0, passed = 0, failed = 0;

            ActionLoader.EnsureLoaded();
            TestCadencePointersResolveToBonuses(ref run, ref passed, ref failed);
            TestRenamedActionsResolveFromOldNames(ref run, ref passed, ref failed);
            TestStatusActionsCarryTheirStatus(ref run, ref passed, ref failed);
            TestBerserkerIsHealthGated(ref run, ref passed, ref failed);

            TestBase.PrintSummary("ActionCardConsistency Tests", run, passed, failed);
        }

        private static void TestCadencePointersResolveToBonuses(ref int run, ref int passed, ref int failed)
        {
            bool dump = System.Environment.GetEnvironmentVariable("DF_DUMP_ACTION_CARDS") == "1";
            foreach (string name in ActionLoader.GetAllActionNames())
            {
                var data = ActionLoader.GetActionData(name);
                var action = ActionLoader.GetAction(name);
                if (data == null || action == null)
                    continue;

                if (dump)
                    DumpCard(action);

                // A hero dice / stat / mod mechanic in the cadence must produce a bonus line on the card.
                bool pointsAtHeroBonus = (data.Mechanics ?? new List<string>())
                    .Select(m => ActionMechanicsRegistry.NormalizeMechanicId(m.Trim()))
                    .Any(id => id.StartsWith("hero_", StringComparison.OrdinalIgnoreCase));
                if (!pointsAtHeroBonus)
                    continue;

                bool hasBonus = action.ActionAttackBonuses?.BonusGroups?
                    .Any(g => g.Bonuses?.Any(b => b.Value != 0) == true) == true;
                TestBase.AssertTrue(hasBonus, $"{name}: hero cadence mechanic resolves to a bonus value",
                    ref run, ref passed, ref failed);
            }
        }

        private static void TestRenamedActionsResolveFromOldNames(ref int run, ref int passed, ref int failed)
        {
            var renames = new (string Old, string New)[]
            {
                ("ANGRY ENRAGE", "ENRAGE"), ("BERZERKER", "BERSERKER"), ("TURN BONUS", "STEADY HAND"),
                ("DUNGEON BONUS", "KEEN EYE"), ("TWISTING STRIKE", "HONE"), ("WHIRLWIND", "WHETSTONE"),
            };
            foreach (var (oldName, newName) in renames)
            {
                TestBase.AssertEqual(newName, ActionLoader.GetAction(oldName)?.Name ?? "",
                    $"{oldName} (old save name) loads as {newName}", ref run, ref passed, ref failed);
            }
        }

        private static void TestStatusActionsCarryTheirStatus(ref int run, ref int passed, ref int failed)
        {
            var skull = ActionLoader.GetActionData("SKULL CRACK");
            TestBase.AssertTrue(skull?.CausesExpose == true, "SKULL CRACK exposes", ref run, ref passed, ref failed);
            var frost = ActionLoader.GetActionData("FROST BOLT");
            TestBase.AssertTrue(frost?.CausesSlow == true, "FROST BOLT slows", ref run, ref passed, ref failed);
            var parry = ActionLoader.GetActionData("PARRY");
            TestBase.AssertTrue(parry?.CausesHarden == true, "PARRY hardens", ref run, ref passed, ref failed);
            var leech = ActionLoader.GetActionData("LEECHING CUT");
            TestBase.AssertTrue(Math.Abs((leech?.LifestealPercent ?? 0) - 0.02) < 1e-9, "LEECHING CUT lifesteals 2%",
                ref run, ref passed, ref failed);
            var bludgeon = ActionLoader.GetActionData("BLUDGEON");
            TestBase.AssertTrue(bludgeon?.CausesDisrupt == true, "BLUDGEON disrupts", ref run, ref passed, ref failed);
            var bash = ActionLoader.GetActionData("BASH");
            TestBase.AssertTrue(bash?.CausesVulnerability == true, "BASH leaves the enemy vulnerable",
                ref run, ref passed, ref failed);
        }

        private static void TestBerserkerIsHealthGated(ref int run, ref int passed, ref int failed)
        {
            var data = ActionLoader.GetActionData("BERSERKER");
            bool gated = data?.TriggerConditions?.Any(c =>
                c.StartsWith("IFSOURCEHEALTHBELOW", StringComparison.OrdinalIgnoreCase)) == true;
            TestBase.AssertTrue(gated, "BERSERKER bonus only below the HP threshold", ref run, ref passed, ref failed);
        }

        private static void DumpCard(Action action)
        {
            Console.WriteLine($"[{action.Name}]");
            foreach (string line in CombatActionStripBuilder.BuildActionStripDescriptionLines(action, 60, 6))
                Console.WriteLine("  " + line);
            foreach (string line in CombatActionStripBuilder.BuildActionStripModifierTailLines(action, 60, 12))
                Console.WriteLine("  > " + line);
        }
    }
}
