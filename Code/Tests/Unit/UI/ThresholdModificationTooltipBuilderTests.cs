using System;
using System.Collections.Generic;
using System.Linq;
using RPGGame;
using RPGGame.Data;
using RPGGame.Tests;
using RPGGame.UI.Avalonia.Layout;

namespace RPGGame.Tests.Unit.UI
{
    public static class ThresholdModificationTooltipBuilderTests
    {
        public static void RunAllTests()
        {
            Console.WriteLine("=== ThresholdModificationTooltipBuilder Tests ===\n");
            int run = 0, passed = 0, failed = 0;

            var hero = new Character("T", 1);
            hero.Stats.Strength = 18;
            hero.Stats.Agility = 8;
            hero.Stats.Technique = 8;
            hero.Stats.Intelligence = 8;

            var hitLines = BuildLines(hero, ThresholdModificationTooltipBuilder.Kind.Hit);
            TestBase.AssertTrue(hitLines.Any(l => l.Contains("Naiveté", StringComparison.OrdinalIgnoreCase)),
                "low-stat hero hit tooltip includes naiveté",
                ref run, ref passed, ref failed);

            hero.DungeonSearchBuffs.AddHitThresholdAdjustment(2);
            var dungeonLines = BuildLines(hero, ThresholdModificationTooltipBuilder.Kind.Hit);
            TestBase.AssertTrue(dungeonLines.Any(l => l.Contains("Dungeon buff (hit)", StringComparison.Ordinal)),
                "dungeon hit buff appears in hit tooltip",
                ref run, ref passed, ref failed);

            hero.Effects.AddPendingActionBonusesNextHeroRoll(new List<ActionAttackBonusItem>
            {
                new ActionAttackBonusItem { Type = "ACCURACY", Value = 3 }
            });
            var comboLines = BuildLines(hero, ThresholdModificationTooltipBuilder.Kind.Combo);
            TestBase.AssertTrue(comboLines.Any(l => l.Contains("Queued accuracy (+3)", StringComparison.Ordinal)),
                "queued accuracy appears on combo tooltip",
                ref run, ref passed, ref failed);

            TestBase.PrintSummary("ThresholdModificationTooltipBuilder Tests", run, passed, failed);
        }

        private static List<string> BuildLines(Character hero, ThresholdModificationTooltipBuilder.Kind kind)
        {
            var lines = new List<string>();
            ThresholdModificationTooltipBuilder.AppendLines(hero, kind, lines.Add);
            return lines;
        }
    }
}
