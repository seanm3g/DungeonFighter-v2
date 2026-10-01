using System;
using System.Collections.Generic;
using RPGGame.Data;
using RPGGame.Tests;
using RPGGame.UI;

namespace RPGGame.Tests.Unit.UI
{
    public static class CadenceCardLineFormatterTests
    {
        public static void RunAllTests()
        {
            Console.WriteLine("=== CadenceCardLineFormatter Tests ===\n");
            int run = 0, passed = 0, failed = 0;

            TestBase.AssertEqual("TURN (3x)", CadenceCardLineFormatter.FormatCadenceHeader("TURN", 3),
                "FormatCadenceHeader uses parenthetical duration", ref run, ref passed, ref failed);

            TestBase.AssertEqual("COMBO +1", CadenceCardLineFormatter.FormatMechanicLine("hero_combo_threshold", 1),
                "FormatMechanicLine uses label then signed quantity", ref run, ref passed, ref failed);

            TestBase.AssertEqual("DAMAGE +25%", CadenceCardLineFormatter.FormatMechanicLine("hero_next_action_damage", 25),
                "FormatMechanicLine appends percent for damage mods", ref run, ref passed, ref failed);

            var block = new CadenceEditorBlock
            {
                Cadence = "Turn",
                Duration = 3,
                Mechanics = new List<CadenceMechanicRow>
                {
                    new CadenceMechanicRow { MechanicId = "hero_combo_threshold", Quantity = 1 }
                }
            };
            var lines = CadenceCardLineFormatter.FormatBlockLinesFromEditor(block);
            TestBase.AssertTrue(lines.Count == 1
                && lines[0] == "turn x3: COMBO +1",
                "FormatBlockLinesFromEditor prefixes the lingering mechanic",
                ref run, ref passed, ref failed);

            var group = new ActionAttackBonusGroup
            {
                CadenceType = "ACTION",
                Count = 2,
                Bonuses = new List<ActionAttackBonusItem>
                {
                    new ActionAttackBonusItem { Type = "ACCURACY", Value = 1 },
                    new ActionAttackBonusItem { Type = "DAMAGE_MOD", Value = 20 }
                }
            };
            var groupLines = CadenceCardLineFormatter.FormatGroupLines(group, 2);
            TestBase.AssertTrue(groupLines.Count == 2
                && groupLines[0] == "action x2: ACC +1"
                && groupLines[1] == "action x2: DAMAGE +20%",
                "FormatGroupLines prefixes each lingering mechanic",
                ref run, ref passed, ref failed);

            TestBase.AssertEqual("turn:", CadenceCardLineFormatter.FormatLingeringPrefix("TURN", 1),
                "duration 1 omits the count", ref run, ref passed, ref failed);

            Console.WriteLine($"\nCadenceCardLineFormatter: {passed}/{run} passed, {failed} failed\n");
        }
    }
}
