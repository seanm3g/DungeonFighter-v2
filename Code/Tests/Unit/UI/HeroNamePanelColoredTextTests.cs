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
            double minSoloBrightness = soloSegs.Min(s => ColorValidator.GetHsvValue255(s.Color));
            TestBase.AssertTrue(minSoloBrightness >= EntityNameColorClamp.DefaultBrightnessFloor - 1,
                "class-colored hero name applies entity-name brightness floor",
                ref run, ref passed, ref failed);

            var hybrid = new Character("Hex", 5);
            hybrid.Progression.RoguePoints = 2;
            hybrid.Progression.WizardPoints = 2;
            var hybridSegs = HeroNamePanelColoredText.BuildLeftPanelHeroNameSegments(hybrid);
            TestBase.AssertTrue(hybridSegs.Count == "Hex".Length, "hybrid name -> per-glyph segments", ref run, ref passed, ref failed);
            int distinctHybrid = hybridSegs.Select(s => (s.Color.R, s.Color.G, s.Color.B)).Distinct().Count();
            TestBase.AssertTrue(distinctHybrid >= 2, "hybrid uses multi-color pattern", ref run, ref passed, ref failed);

            TestCombatLogNameMatchesHudHeroPanel(ref run, ref passed, ref failed);

            TestBase.PrintSummary("HeroNamePanelColoredText Tests", run, passed, failed);
        }

        /// <summary>
        /// Combat log character name (<see cref="EntityColorHelper.AppendActorNameColored"/>) must produce
        /// the same per-glyph segments as the left HERO panel so a class-titled hero like "Malachi Sunshard"
        /// reads with one consistent palette across the HUD and the log.
        /// </summary>
        private static void TestCombatLogNameMatchesHudHeroPanel(ref int run, ref int passed, ref int failed)
        {
            // Class-titled solo hero (Barbarian => mace palette in HeroNamePanelColoredText)
            var hero = new Character("Malachi Sunshard", 4);
            hero.Progression.BarbarianPoints = 3;

            var panelSegs = HeroNamePanelColoredText.BuildLeftPanelHeroNameSegments(hero);
            var logBuilder = new ColoredTextBuilder();
            EntityColorHelper.AppendActorNameColored(logBuilder, hero);
            var logSegs = logBuilder.Build();

            TestBase.AssertEqual(panelSegs.Count, logSegs.Count,
                "combat log name segment count matches HERO panel for class-titled hero",
                ref run, ref passed, ref failed);

            int matched = 0;
            int compare = Math.Min(panelSegs.Count, logSegs.Count);
            for (int i = 0; i < compare; i++)
            {
                bool textOk = string.Equals(panelSegs[i].Text, logSegs[i].Text, StringComparison.Ordinal);
                bool colorOk = panelSegs[i].Color.R == logSegs[i].Color.R
                    && panelSegs[i].Color.G == logSegs[i].Color.G
                    && panelSegs[i].Color.B == logSegs[i].Color.B;
                if (textOk && colorOk) matched++;
            }
            TestBase.AssertEqual(compare, matched,
                "every combat log name glyph matches the HERO panel glyph (text + color)",
                ref run, ref passed, ref failed);

            // Default Fighter display (no class points) collapses to one gold segment in both paths
            var fighter = new Character("Aldric Brightmoon", 1);
            fighter.Progression.BarbarianPoints = 0;
            fighter.Progression.RoguePoints = 0;
            fighter.Progression.WarriorPoints = 0;
            fighter.Progression.WizardPoints = 0;

            var fighterPanel = HeroNamePanelColoredText.BuildLeftPanelHeroNameSegments(fighter);
            var fighterLogBuilder = new ColoredTextBuilder();
            EntityColorHelper.AppendActorNameColored(fighterLogBuilder, fighter);
            var fighterLog = fighterLogBuilder.Build();

            TestBase.AssertEqual(1, fighterLog.Count,
                "Fighter combat log name is one segment (matches single-segment HUD)",
                ref run, ref passed, ref failed);
            TestBase.AssertEqual(fighterPanel[0].Text, fighterLog[0].Text,
                "Fighter combat log name text matches HUD",
                ref run, ref passed, ref failed);
            TestBase.AssertTrue(
                fighterPanel[0].Color.R == fighterLog[0].Color.R
                && fighterPanel[0].Color.G == fighterLog[0].Color.G
                && fighterPanel[0].Color.B == fighterLog[0].Color.B,
                "Fighter combat log name color matches HUD (gold)",
                ref run, ref passed, ref failed);
        }
    }
}
