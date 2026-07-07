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
            TestBase.AssertTrue(lines.Count == 2
                && lines[0] == "TURN (3x)"
                && lines[1] == "COMBO +1",
                "FormatBlockLinesFromEditor emits header then mechanic line",
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
            TestBase.AssertTrue(groupLines.Count == 3
                && groupLines[0] == "ACTION (2x)"
                && groupLines[1] == "ACC +1"
                && groupLines[2] == "DAMAGE +20%",
                "FormatGroupLines mirrors card layout for bonus groups",
                ref run, ref passed, ref failed);

            Console.WriteLine($"\nCadenceCardLineFormatter: {passed}/{run} passed, {failed} failed\n");
        }
    }
}
