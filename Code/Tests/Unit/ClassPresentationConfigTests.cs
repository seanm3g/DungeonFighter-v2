using RPGGame.Tests;

namespace RPGGame.Tests.Unit
{
    public static class ClassPresentationConfigTests
    {
        public static void RunAllTests()
        {
            int run = 0, passed = 0, failed = 0;

            TierBands(ref run, ref passed, ref failed);
            MeaningfulAttributeMinimumClamps(ref run, ref passed, ref failed);
            MigratesLegacyThreeThresholds(ref run, ref passed, ref failed);
            FormatRankedTitleUsesTiers(ref run, ref passed, ref failed);
            FormatRankedTitleUsesPathEvolvedNames(ref run, ref passed, ref failed);
            ParseDuoHybridLines(ref run, ref passed, ref failed);
            DuoRulesDistinctBandPairs(ref run, ref passed, ref failed);
            AttributeDuoCoreNameUsesConfig(ref run, ref passed, ref failed);
            AttributeThirdPathSuffixUsesConfig(ref run, ref passed, ref failed);

            TestBase.PrintSummary("ClassPresentationConfigTests", run, passed, failed);
        }

        private static void TierBands(ref int run, ref int passed, ref int failed)
        {
            var c = new ClassPresentationConfig { TierThresholds = new[] { 5, 15, 40, 80 } }.EnsureNormalized();
            TestBase.AssertEqual(-1, c.GetTierBandIndex(0), "0 pts -> -1", ref run, ref passed, ref failed);
            TestBase.AssertEqual(0, c.GetTierBandIndex(3), "below first threshold", ref run, ref passed, ref failed);
            TestBase.AssertEqual(1, c.GetTierBandIndex(5), "tier band 1", ref run, ref passed, ref failed);
            TestBase.AssertEqual(2, c.GetTierBandIndex(20), "tier band 2", ref run, ref passed, ref failed);
            TestBase.AssertEqual(3, c.GetTierBandIndex(50), "tier band 3", ref run, ref passed, ref failed);
            TestBase.AssertEqual(4, c.GetTierBandIndex(100), "tier band 4", ref run, ref passed, ref failed);
        }

        private static void MeaningfulAttributeMinimumClamps(ref int run, ref int passed, ref int failed)
        {
            var c = new ClassPresentationConfig { MeaningfulAttributeMinimum = 0 }.EnsureNormalized();
            TestBase.AssertEqual(1, c.MeaningfulAttributeMinimum, "meaningful attribute minimum clamped to at least 1", ref run, ref passed, ref failed);
        }

        private static void MigratesLegacyThreeThresholds(ref int run, ref int passed, ref int failed)
        {
            var c = new ClassPresentationConfig
            {
                TierThresholds = new[] { 2, 20, 60 },
                TierNames = new[] { "A", "B", "C" }
            }.EnsureNormalized();
            TestBase.AssertEqual(4, c.TierThresholds.Length, "migrated to four thresholds", ref run, ref passed, ref failed);
            TestBase.AssertEqual(4, c.TierNames.Length, "migrated to four global tier names", ref run, ref passed, ref failed);
            TestBase.AssertTrue(c.TierThresholds[3] > c.TierThresholds[2], "fourth gate after third", ref run, ref passed, ref failed);
        }

        private static void FormatRankedTitleUsesTiers(ref int run, ref int passed, ref int failed)
        {
            var c = new ClassPresentationConfig
            {
                TierThresholds = new[] { 2, 10, 30, 100 },
                TierNames = new[] { "A", "B", "C", "D" },
                MaceClassDisplayName = "Zed"
            }.EnsureNormalized();
            TestBase.AssertEqual("Novice", c.FormatRankedTitle(WeaponType.Mace, 1), "pre-tier label", ref run, ref passed, ref failed);
            TestBase.AssertEqual("A Zed", c.FormatRankedTitle(WeaponType.Mace, 2), "tier1 name", ref run, ref passed, ref failed);
            TestBase.AssertEqual("B Zed", c.FormatRankedTitle(WeaponType.Mace, 10), "tier2", ref run, ref passed, ref failed);
            TestBase.AssertEqual("C Zed", c.FormatRankedTitle(WeaponType.Mace, 30), "tier3", ref run, ref passed, ref failed);
            TestBase.AssertEqual("D Zed", c.FormatRankedTitle(WeaponType.Mace, 200), "tier4", ref run, ref passed, ref failed);
        }

        private static void FormatRankedTitleUsesPathEvolvedNames(ref int run, ref int passed, ref int failed)
        {
            var c = new ClassPresentationConfig
            {
                TierThresholds = new[] { 2, 10, 30, 100 },
                TierNames = new[] { "A", "B", "C", "D" },
                MaceClassDisplayName = "Zed",
                MaceTierNames = new[] { "Warchief", "Ravager", "Titan", "Paragon" }
            }.EnsureNormalized();
            TestBase.AssertEqual("Warchief", c.FormatRankedTitle(WeaponType.Mace, 2), "path tier1 evolved only", ref run, ref passed, ref failed);
            TestBase.AssertEqual("Ravager", c.FormatRankedTitle(WeaponType.Mace, 10), "path tier2", ref run, ref passed, ref failed);
            TestBase.AssertEqual("Titan", c.FormatRankedTitle(WeaponType.Mace, 30), "path tier3", ref run, ref passed, ref failed);
            TestBase.AssertEqual("Paragon", c.FormatRankedTitle(WeaponType.Mace, 200), "path tier4", ref run, ref passed, ref failed);
        }

        private static void ParseDuoHybridLines(ref int run, ref int passed, ref int failed)
        {
            string text = "Mace,Sword,2,1:Skullreaver|Ironhowl" + System.Environment.NewLine + "# comment" + System.Environment.NewLine;
            var list = HybridTitleRuleText.ParseDuoRules(text);
            TestBase.AssertEqual(1, list.Count, "one duo rule", ref run, ref passed, ref failed);
            TestBase.AssertEqual("Skullreaver", list[0].Titles[0], "first title", ref run, ref passed, ref failed);
            string roundTrip = HybridTitleRuleText.FormatDuoRules(list);
            TestBase.AssertTrue(roundTrip.Contains("Mace,Sword,2,1:"), "format round-trip", ref run, ref passed, ref failed);
        }

        private static void DuoRulesDistinctBandPairs(ref int run, ref int passed, ref int failed)
        {
            var cfg = new ClassPresentationConfig
            {
                HybridDuoTierRules = new System.Collections.Generic.List<HybridDuoTierRule>
                {
                    new HybridDuoTierRule
                    {
                        PrimaryPath = "Mace",
                        SecondaryPath = "Sword",
                        PrimaryTierBand = 0,
                        SecondaryTierBand = 3,
                        Titles = new[] { "LowHigh" }
                    },
                    new HybridDuoTierRule
                    {
                        PrimaryPath = "Mace",
                        SecondaryPath = "Sword",
                        PrimaryTierBand = 2,
                        SecondaryTierBand = 1,
                        Titles = new[] { "MidMid" }
                    }
                }
            }.EnsureNormalized();
            TestBase.AssertTrue(cfg.TryPickDuoHybridTitle(WeaponType.Mace, 0, WeaponType.Sword, 3, 0, out string a),
                "duo pick bands 0+3", ref run, ref passed, ref failed);
            TestBase.AssertEqual("LowHigh", a, "title A", ref run, ref passed, ref failed);
            TestBase.AssertTrue(cfg.TryPickDuoHybridTitle(WeaponType.Mace, 2, WeaponType.Sword, 1, 0, out string b),
                "duo pick bands 2+1", ref run, ref passed, ref failed);
            TestBase.AssertEqual("MidMid", b, "title B", ref run, ref passed, ref failed);
        }

        private static void AttributeDuoCoreNameUsesConfig(ref int run, ref int passed, ref int failed)
        {
            var cfg = new ClassPresentationConfig { AttributeDuoMaceSword = "Skullsplitter" }.EnsureNormalized();
            TestBase.AssertEqual("Skullsplitter", cfg.GetAttributeDuoCoreName(WeaponType.Mace, WeaponType.Sword), "config duo core", ref run, ref passed, ref failed);
        }

        private static void AttributeThirdPathSuffixUsesConfig(ref int run, ref int passed, ref int failed)
        {
            var cfg = new ClassPresentationConfig { AttributeThirdPathSuffix = " of the Deep " }.EnsureNormalized();
            TestBase.AssertEqual("of the Deep", cfg.GetAttributeThirdPathSuffix(), "trimmed third-path suffix", ref run, ref passed, ref failed);
        }
    }
}
