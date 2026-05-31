using System;
using System.Collections.Generic;
using RPGGame;
using RPGGame.Tests;

namespace RPGGame.Tests.Unit.Data
{
    public static class EnemySpawnFilterTests
    {
        public static void RunAllTests()
        {
            Console.WriteLine("=== EnemySpawnFilter Tests ===\n");
            int run = 0, pass = 0, fail = 0;
            FilterEmptyPlacementMatchesAll(ref run, ref pass, ref fail);
            FilterBiomeMismatchExcludes(ref run, ref pass, ref fail);
            FilterRegionMatchesIdOrDisplayName(ref run, ref pass, ref fail);
            FilterLocationListMatches(ref run, ref pass, ref fail);
            TierRollDistributionSkewsCommon(ref run, ref pass, ref fail);
            TierBiomePickSelectsUncommonInBiome(ref run, ref pass, ref fail);
            TierRareLocationPickSelectsRareAtLocation(ref run, ref pass, ref fail);
            TierEmptyPoolFallsBackToAnywhere(ref run, ref pass, ref fail);
            SettlementTypeFromLocationName(ref run, ref pass, ref fail);
            SettlementTypeFromRegionDefault(ref run, ref pass, ref fail);
            SpawnWeightsUseCityProfile(ref run, ref pass, ref fail);
            SpawnWeightsNormalizeOnSanitize(ref run, ref pass, ref fail);
            TestBase.PrintSummary("EnemySpawnFilter Tests", run, pass, fail);
        }

        private static EnemyData Make(string name, string? region = null, string? biome = null, string? location = null, string? rarity = null) =>
            new()
            {
                Name = name,
                Region = region,
                Biome = biome,
                Location = location,
                Rarity = rarity,
                Archetype = "Berserker",
                Actions = new List<string> { "JAB" }
            };

        private static void FilterEmptyPlacementMatchesAll(ref int run, ref int pass, ref int fail)
        {
            TestBase.SetCurrentTestName(nameof(FilterEmptyPlacementMatchesAll));
            var pool = new List<EnemyData> { Make("Goblin") };
            var ctx = new EnemySpawnContext("forest", "Forest", "Dark Clearing");
            var filtered = EnemySpawnFilter.Filter(pool, ctx);
            TestBase.AssertEqual(1, filtered.Count, "wildcard placement", ref run, ref pass, ref fail);
        }

        private static void FilterBiomeMismatchExcludes(ref int run, ref int pass, ref int fail)
        {
            TestBase.SetCurrentTestName(nameof(FilterBiomeMismatchExcludes));
            var pool = new List<EnemyData> { Make("Goblin", biome: "Crypt") };
            var ctx = new EnemySpawnContext(null, "Forest", null);
            var filtered = EnemySpawnFilter.Filter(pool, ctx);
            TestBase.AssertEqual(0, filtered.Count, "biome mismatch", ref run, ref pass, ref fail);
        }

        private static void FilterRegionMatchesIdOrDisplayName(ref int run, ref int pass, ref int fail)
        {
            TestBase.SetCurrentTestName(nameof(FilterRegionMatchesIdOrDisplayName));
            var pool = new List<EnemyData> { Make("Goblin", region: "Whisperledger Wilds") };
            var region = new TravelRegion { Id = "forest", DisplayName = "Whisperledger Wilds", Theme = "Forest" };
            var ctx = new EnemySpawnContext("forest", "Forest", null);
            var filtered = EnemySpawnFilter.Filter(pool, ctx, region);
            TestBase.AssertEqual(1, filtered.Count, "display name match", ref run, ref pass, ref fail);
        }

        private static void FilterLocationListMatches(ref int run, ref int pass, ref int fail)
        {
            TestBase.SetCurrentTestName(nameof(FilterLocationListMatches));
            var pool = new List<EnemyData> { Make("Goblin", location: "Dark Clearing, Swamp Path") };
            var ctx = new EnemySpawnContext(null, null, "Swamp Path");
            var filtered = EnemySpawnFilter.Filter(pool, ctx);
            TestBase.AssertEqual(1, filtered.Count, "location list", ref run, ref pass, ref fail);
        }

        private static void TierRollDistributionSkewsCommon(ref int run, ref int pass, ref int fail)
        {
            TestBase.SetCurrentTestName(nameof(TierRollDistributionSkewsCommon));
            var pool = new List<EnemyData>
            {
                Make("CommonMob", rarity: "Common"),
                Make("UncommonForest", rarity: "Uncommon", biome: "Forest"),
                Make("RareHere", rarity: "Rare", location: "Clearing")
            };
            var ctx = new EnemySpawnContext("forest", "Forest", "Clearing");
            var rng = new Random(42);
            int commonPicks = 0;
            for (int i = 0; i < 500; i++)
            {
                var pick = EnemySpawnFilter.PickByTieredSpawnRoll(pool, ctx, null, rng);
                if (string.Equals(pick.Name, "CommonMob", StringComparison.Ordinal))
                    commonPicks++;
            }
            TestBase.AssertTrue(commonPicks > 200, $"common tier should dominate (~50%, got {commonPicks}/500)", ref run, ref pass, ref fail);
        }

        private static void TierBiomePickSelectsUncommonInBiome(ref int run, ref int pass, ref int fail)
        {
            TestBase.SetCurrentTestName(nameof(TierBiomePickSelectsUncommonInBiome));
            var pool = new List<EnemyData>
            {
                Make("CommonMob", rarity: "Common"),
                Make("UncommonForest", rarity: "Uncommon", biome: "Forest"),
                Make("UncommonCrypt", rarity: "Uncommon", biome: "Crypt")
            };
            var ctx = new EnemySpawnContext(null, "Forest", null);
            var candidates = EnemySpawnFilter.GetCandidatesForTier(pool, EnemySpawnTier.UncommonBiome, ctx, null);
            TestBase.AssertEqual(1, candidates.Count, "one uncommon forest", ref run, ref pass, ref fail);
            TestBase.AssertEqual("UncommonForest", candidates[0].Name, "forest uncommon", ref run, ref pass, ref fail);
        }

        private static void TierRareLocationPickSelectsRareAtLocation(ref int run, ref int pass, ref int fail)
        {
            TestBase.SetCurrentTestName(nameof(TierRareLocationPickSelectsRareAtLocation));
            var pool = new List<EnemyData>
            {
                Make("RareHere", rarity: "Rare", location: "Clearing"),
                Make("RareElsewhere", rarity: "Rare", location: "Cave")
            };
            var ctx = new EnemySpawnContext(null, null, "Clearing");
            var candidates = EnemySpawnFilter.GetCandidatesForTier(pool, EnemySpawnTier.RareLocation, ctx, null);
            TestBase.AssertEqual(1, candidates.Count, "one rare at location", ref run, ref pass, ref fail);
            TestBase.AssertEqual("RareHere", candidates[0].Name, "rare at clearing", ref run, ref pass, ref fail);
        }

        private static void TierEmptyPoolFallsBackToAnywhere(ref int run, ref int pass, ref int fail)
        {
            TestBase.SetCurrentTestName(nameof(TierEmptyPoolFallsBackToAnywhere));
            var pool = new List<EnemyData> { Make("OnlyMob", rarity: "Common") };
            var ctx = new EnemySpawnContext("forest", "Forest", "Nowhere");
            var rng = new Random(99);
            // Force UncommonBiome tier with a stub roll — pick roll 55 which is uncommon biome band
            var tier = EnemySpawnFilter.ResolveSpawnTierFromRoll(55, new EnemySpawnTierWeightsConfig());
            TestBase.AssertTrue(tier == EnemySpawnTier.UncommonBiome, "roll 55 is uncommon biome", ref run, ref pass, ref fail);
            var pick = EnemySpawnFilter.PickByTieredSpawnRoll(pool, ctx, null, rng);
            TestBase.AssertEqual("OnlyMob", pick.Name, "fallback when tier empty", ref run, ref pass, ref fail);
        }

        private static void SettlementTypeFromLocationName(ref int run, ref int pass, ref int fail)
        {
            TestBase.SetCurrentTestName(nameof(SettlementTypeFromLocationName));
            TestBase.AssertTrue(SettlementTypeResolver.Resolve("Amber City", null) == SettlementType.City,
                "city in name", ref run, ref pass, ref fail);
            TestBase.AssertTrue(SettlementTypeResolver.Resolve("Mill Town", null) == SettlementType.Town,
                "town in name", ref run, ref pass, ref fail);
            TestBase.AssertTrue(SettlementTypeResolver.Resolve("Dark Clearing", null) == SettlementType.Rural,
                "generic location", ref run, ref pass, ref fail);
        }

        private static void SettlementTypeFromRegionDefault(ref int run, ref int pass, ref int fail)
        {
            TestBase.SetCurrentTestName(nameof(SettlementTypeFromRegionDefault));
            var region = new TravelRegion { SettlementType = "City" };
            TestBase.AssertTrue(SettlementTypeResolver.Resolve("General", region) == SettlementType.City,
                "region default", ref run, ref pass, ref fail);
        }

        private static void SpawnWeightsUseCityProfile(ref int run, ref int pass, ref int fail)
        {
            TestBase.SetCurrentTestName(nameof(SpawnWeightsUseCityProfile));
            var sys = new EnemySystemConfig
            {
                SpawnTierWeightsBySettlement = new EnemySpawnTierWeightsBySettlementConfig
                {
                    Rural = new EnemySpawnTierWeightsConfig { CommonPercent = 50 },
                    Town = new EnemySpawnTierWeightsConfig { CommonPercent = 40 },
                    City = new EnemySpawnTierWeightsConfig { CommonPercent = 10, AnywherePercent = 45 }
                }
            };
            sys.EnsureSanitizedDefaults();
            var ctx = new EnemySpawnContext(null, null, "Capital City");
            var weights = EnemySpawnFilter.ResolveSpawnTierWeights(ctx, null, sys);
            TestBase.AssertEqual(10, weights.CommonPercent, "city profile common", ref run, ref pass, ref fail);
            TestBase.AssertEqual(45, weights.AnywherePercent, "city profile anywhere", ref run, ref pass, ref fail);
        }

        private static void SpawnWeightsNormalizeOnSanitize(ref int run, ref int pass, ref int fail)
        {
            TestBase.SetCurrentTestName(nameof(SpawnWeightsNormalizeOnSanitize));
            var weights = new EnemySpawnTierWeightsConfig
            {
                CommonPercent = 50,
                UncommonBiomePercent = 10,
                UncommonRegionPercent = 10,
                UncommonLocationPercent = 10,
                RareLocationPercent = 10,
                AnywherePercent = 10
            };
            weights.EnsureSanitized();
            TestBase.AssertEqual(100, weights.TotalPercent, "normalized total", ref run, ref pass, ref fail);
        }
    }
}
