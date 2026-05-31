using System;
using RPGGame;
using RPGGame.Tests;

namespace RPGGame.Tests.Unit.Data
{
    public static class EnemyRarityRewardTests
    {
        public static void RunAllTests()
        {
            Console.WriteLine("=== EnemyRarityReward Tests ===\n");
            int run = 0, pass = 0, fail = 0;
            RareEnemyHigherRewardsThanCommon(ref run, ref pass, ref fail);
            ApplyToEnemySetsRarityLabel(ref run, ref pass, ref fail);
            LootMagicFindBonusIncreasesForHigherRarity(ref run, ref pass, ref fail);
            TestBase.PrintSummary("EnemyRarityReward Tests", run, pass, fail);
        }

        private static void RareEnemyHigherRewardsThanCommon(ref int run, ref int pass, ref int fail)
        {
            TestBase.SetCurrentTestName(nameof(RareEnemyHigherRewardsThanCommon));
            var common = new Enemy("Mob", 5, 50, 8, 6, 4, 4, 0);
            int commonGold = common.GoldReward;
            int commonXp = common.XPReward;

            var rare = new Enemy("Mob", 5, 50, 8, 6, 4, 4, 0);
            EnemyRarityRewardHelper.ApplyToEnemy(rare, "Rare");

            TestBase.AssertTrue(rare.GoldReward > commonGold, "rare gold > common", ref run, ref pass, ref fail);
            TestBase.AssertTrue(rare.XPReward > commonXp, "rare xp > common", ref run, ref pass, ref fail);
        }

        private static void ApplyToEnemySetsRarityLabel(ref int run, ref int pass, ref int fail)
        {
            TestBase.SetCurrentTestName(nameof(ApplyToEnemySetsRarityLabel));
            var enemy = new Enemy("Mob", 1, 50, 8, 6, 4, 4, 0);
            EnemyRarityRewardHelper.ApplyToEnemy(enemy, "Epic");
            TestBase.AssertEqual("Epic", enemy.Rarity, "rarity label", ref run, ref pass, ref fail);
        }

        private static void LootMagicFindBonusIncreasesForHigherRarity(ref int run, ref int pass, ref int fail)
        {
            TestBase.SetCurrentTestName(nameof(LootMagicFindBonusIncreasesForHigherRarity));
            double common = EnemyRarityRewardHelper.GetLootMagicFindBonus("Common");
            double mythic = EnemyRarityRewardHelper.GetLootMagicFindBonus("Mythic");
            TestBase.AssertTrue(mythic > common, "mythic MF bonus > common", ref run, ref pass, ref fail);
        }
    }
}
