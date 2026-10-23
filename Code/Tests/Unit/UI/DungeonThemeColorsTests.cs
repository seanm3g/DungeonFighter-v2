using System;
using System.Linq;
using RPGGame;
using RPGGame.Tests;
using RPGGame.UI.Avalonia;

namespace RPGGame.Tests.Unit.UI
{
    /// <summary>
    /// Every dungeon theme (and so every room) and every travel region resolves to a multi-color template.
    /// </summary>
    public static class DungeonThemeColorsTests
    {
        public static void RunAllTests()
        {
            Console.WriteLine("=== DungeonThemeColors Tests ===\n");
            int run = 0, passed = 0, failed = 0;

            foreach (var d in JsonLoader.LoadJsonList<DungeonData>("Dungeons.json"))
            {
                string? template = DungeonThemeColors.GetThemeTemplate(d.theme);
                TestBase.AssertTrue(template != null, $"Theme '{d.theme}' ({d.name}) has a color template", ref run, ref passed, ref failed);

                var segments = DungeonThemeColors.BuildThemedName(d.name, d.theme);
                int colors = segments.Where(s => !string.IsNullOrWhiteSpace(s.Text)).Select(s => s.Color).Distinct().Count();
                TestBase.AssertTrue(colors > 1, $"'{d.name}' is painted in several colors", ref run, ref passed, ref failed);
                TestBase.AssertEqual(d.name, string.Concat(segments.Select(s => s.Text)), $"'{d.name}' text is unchanged", ref run, ref passed, ref failed);

                TestBase.AssertEqual(d.theme, DungeonThemeColors.GetThemeForDungeonName(d.name), $"'{d.name}' maps back to its theme", ref run, ref passed, ref failed);
            }

            TestBase.AssertEqual("fire_temple", DungeonThemeColors.GetThemeTemplate("Fire Temple"), "Multi-word themes use underscore slugs", ref run, ref passed, ref failed);
            TestBase.AssertTrue(DungeonThemeColors.GetThemeTemplate("No Such Theme") == null, "Unknown theme has no template", ref run, ref passed, ref failed);

            foreach (var region in new TravelRegionCatalog().GetAllRegions())
            {
                TestBase.AssertTrue(DungeonThemeColors.GetRegionThemeTemplate(region.Theme) != null,
                    $"Region '{region.DisplayName}' theme '{region.Theme}' has a color template", ref run, ref passed, ref failed);
            }

            TestBase.PrintSummary("DungeonThemeColors Tests", run, passed, failed);
        }
    }
}
