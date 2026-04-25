using System;
using System.Linq;
using RPGGame;
using RPGGame.Tests;
using RPGGame.UI.ColorSystem;
using RPGGame.UI.ColorSystem.Applications;

namespace RPGGame.Tests.Unit.UI
{
    public static class HeroNamePanelColoredTextTests
    {
        public static void RunAllTests()
        {
            Console.WriteLine("=== HeroNamePanelColoredText Tests ===\n");
            int run = 0, passed = 0, failed = 0;

            _ = GameConfiguration.Instance;

            var stillFighter = new Character("Aldric Brightmoon", 6);
            stillFighter.Progression.BarbarianPoints = 1;
            stillFighter.Progression.RoguePoints = 0;
            stillFighter.Progression.WarriorPoints = 0;
            stillFighter.Progression.WizardPoints = 0;
            var fighterSegs = HeroNamePanelColoredText.BuildLeftPanelHeroNameSegments(stillFighter);
            TestBase.AssertTrue(fighterSegs.Count == 1, "below first tier gate -> single segment (plain)", ref run, ref passed, ref failed);
            TestBase.AssertTrue(
                string.Equals(fighterSegs[0].Text, "Aldric Brightmoon", StringComparison.Ordinal),
                "single segment preserves full name",
                ref run, ref passed, ref failed);
            TestBase.AssertTrue(fighterSegs[0].Color == ColorPalette.Gold.GetColor(), "Fighter display uses gold (yellow hero name)", ref run, ref passed, ref failed);

            var soloBarb = new Character("Grimgar", 3);
            soloBarb.Progression.BarbarianPoints = 2;
            var soloSegs = HeroNamePanelColoredText.BuildLeftPanelHeroNameSegments(soloBarb);
            TestBase.AssertTrue(soloSegs.Count == "Grimgar".Length, "solo name -> one segment per glyph", ref run, ref passed, ref failed);
            int distinctSolo = soloSegs.Select(s => (s.Color.R, s.Color.G, s.Color.B)).Distinct().Count();
            TestBase.AssertTrue(distinctSolo >= 2, "solo barb cycles at least two colors", ref run, ref passed, ref failed);

            var hybrid = new Character("Hex", 5);
            hybrid.Progression.RoguePoints = 2;
            hybrid.Progression.WizardPoints = 2;
            var hybridSegs = HeroNamePanelColoredText.BuildLeftPanelHeroNameSegments(hybrid);
            TestBase.AssertTrue(hybridSegs.Count == "Hex".Length, "hybrid name -> per-glyph segments", ref run, ref passed, ref failed);
            int distinctHybrid = hybridSegs.Select(s => (s.Color.R, s.Color.G, s.Color.B)).Distinct().Count();
            TestBase.AssertTrue(distinctHybrid >= 2, "hybrid uses multi-color pattern", ref run, ref passed, ref failed);

            TestBase.PrintSummary("HeroNamePanelColoredText Tests", run, passed, failed);
        }
    }
}
