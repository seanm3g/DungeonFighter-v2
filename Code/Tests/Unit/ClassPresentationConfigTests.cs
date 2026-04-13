using RPGGame.Tests;

namespace RPGGame.Tests.Unit
{
    public static class ClassPresentationConfigTests
    {
        public static void RunAllTests()
        {
            int run = 0, passed = 0, failed = 0;

            TierBands(ref run, ref passed, ref failed);
            MigratesLegacyThreeThresholds(ref run, ref passed, ref failed);
            FormatRankedTitleUsesTiers(ref run, ref passed, ref failed);
            AttributeDuoCoreNameUsesConfig(ref run, ref passed, ref failed);

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

        private static void MigratesLegacyThreeThresholds(ref int run, ref int passed, ref int failed)
        {
            var c = new ClassPresentationConfig { TierThresholds = new[] { 2, 20, 60 } }.EnsureNormalized();
            TestBase.AssertEqual(4, c.TierThresholds.Length, "migrated to four thresholds", ref run, ref passed, ref failed);
            TestBase.AssertTrue(c.TierThresholds[3] > c.TierThresholds[2], "fourth gate after third", ref run, ref passed, ref failed);
        }

        private static void FormatRankedTitleUsesTiers(ref int run, ref int passed, ref int failed)
        {
            var c = new ClassPresentationConfig
            {
                TierThresholds = new[] { 2, 10, 30, 100 },
                MaceClassDisplayName = "Zed"
            }.EnsureNormalized();
            TestBase.AssertEqual("Novice", c.FormatRankedTitle(WeaponType.Mace, 1), "pre-tier label", ref run, ref passed, ref failed);
            TestBase.AssertEqual("Adept Zed", c.FormatRankedTitle(WeaponType.Mace, 2), "tier1 name", ref run, ref passed, ref failed);
            TestBase.AssertEqual("Expert Zed", c.FormatRankedTitle(WeaponType.Mace, 10), "tier2", ref run, ref passed, ref failed);
            TestBase.AssertEqual("Master Zed", c.FormatRankedTitle(WeaponType.Mace, 30), "tier3", ref run, ref passed, ref failed);
            TestBase.AssertEqual("Paragon Zed", c.FormatRankedTitle(WeaponType.Mace, 200), "tier4", ref run, ref passed, ref failed);
        }

        private static void AttributeDuoCoreNameUsesConfig(ref int run, ref int passed, ref int failed)
        {
            var cfg = new ClassPresentationConfig { AttributeDuoMaceSword = "Skullsplitter" }.EnsureNormalized();
            TestBase.AssertEqual("Skullsplitter", cfg.GetAttributeDuoCoreName(WeaponType.Mace, WeaponType.Sword), "config duo core", ref run, ref passed, ref failed);
        }

    }
}
