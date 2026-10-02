using System;
using RPGGame;
using RPGGame.Actions;
using RPGGame.Tests;

namespace RPGGame.Tests.Unit.Combat
{
    /// <summary>
    /// Leather 3-piece WHILE_EQUIPPED Lucky status — new heroes start with it; unequipping drops the bonus.
    /// </summary>
    public static class LeatherSetBonusTests
    {
        private static int _run, _passed, _failed;

        public static void RunAllTests()
        {
            Console.WriteLine("=== Leather Set Bonus Tests ===\n");
            _run = _passed = _failed = 0;

            TestThreeLeatherGrantsAdvantage();
            TestUnequipLosesAdvantage();
            TestStarterCatalogUnlocksLuck();
            TestLuckyAppearsAsStatusEffect();

            TestBase.PrintSummary("Leather Set Bonus Tests", _run, _passed, _failed);
        }

        private static void TestThreeLeatherGrantsAdvantage()
        {
            TestBase.SetCurrentTestName(nameof(TestThreeLeatherGrantsAdvantage));
            var hero = new Character("LeatherLuck", 1);
            hero.Equipment.Head = new HeadItem("Helmet", 1, 1) { Material = "Leather" };
            hero.Equipment.Body = new ChestItem("Armor", 1, 1) { Material = "Leather" };
            hero.Equipment.Feet = new FeetItem("Boots", 1, 1) { Material = "Leather" };

            TestBase.AssertTrue(LeatherSetBonus.HasLuckyStatus(hero),
                "3 leather pieces unlock Lucky status", ref _run, ref _passed, ref _failed);
            TestBase.AssertTrue(LeatherSetBonus.HasLuckAdvantage(hero),
                "HasLuckAdvantage aliases HasLuckyStatus", ref _run, ref _passed, ref _failed);

            bool advantage = false, disadvantage = false;
            LeatherSetBonus.CollectAdvantageFlags(hero, ref advantage, ref disadvantage);
            TestBase.AssertTrue(advantage, "CollectAdvantageFlags sets advantage", ref _run, ref _passed, ref _failed);
            TestBase.AssertFalse(disadvantage, "Leather set does not set disadvantage", ref _run, ref _passed, ref _failed);

            ActionSelector.PeekPendingMultiDiceFlags(hero, out bool peekAdv, out bool peekDis, null);
            TestBase.AssertTrue(peekAdv, "PeekPendingMultiDiceFlags includes leather Lucky", ref _run, ref _passed, ref _failed);
            TestBase.AssertFalse(peekDis, "Peek does not invent disadvantage", ref _run, ref _passed, ref _failed);
        }

        private static void TestUnequipLosesAdvantage()
        {
            TestBase.SetCurrentTestName(nameof(TestUnequipLosesAdvantage));
            var hero = new Character("UpgradeOffLeather", 1);
            hero.Equipment.Head = new HeadItem("Helmet", 1, 1) { Material = "Leather" };
            hero.Equipment.Body = new ChestItem("Armor", 1, 1) { Material = "Leather" };
            hero.Equipment.Feet = new FeetItem("Boots", 1, 1) { Material = "Leather" };
            TestBase.AssertTrue(LeatherSetBonus.HasLuckyStatus(hero),
                "starts with Lucky status", ref _run, ref _passed, ref _failed);

            // Swap chest for better non-leather gear — set drops below 3.
            hero.Equipment.Body = new ChestItem("Iron Plate", 2, 5) { Material = "Iron" };
            TestBase.AssertFalse(LeatherSetBonus.HasLuckyStatus(hero),
                "replacing leather chest loses Lucky status", ref _run, ref _passed, ref _failed);

            bool advantage = false, disadvantage = false;
            LeatherSetBonus.CollectAdvantageFlags(hero, ref advantage, ref disadvantage);
            TestBase.AssertFalse(advantage, "no advantage after breaking the set", ref _run, ref _passed, ref _failed);
        }

        private static void TestStarterCatalogUnlocksLuck()
        {
            TestBase.SetCurrentTestName(nameof(TestStarterCatalogUnlocksLuck));
            var items = StarterCatalogItems.LoadStarterArmorItems();
            var hero = new Character("StarterLeather", 1);
            foreach (var item in items)
            {
                string slot = StarterCatalogItems.GetEquipmentSlotKey(item);
                hero.TryEquipItem(item, slot, out _, out _);
            }

            TestBase.AssertEqual(3, LeatherSetBonus.CountEquipped(hero),
                "starter kit equips 3 leather pieces", ref _run, ref _passed, ref _failed);
            TestBase.AssertTrue(LeatherSetBonus.HasLuckyStatus(hero),
                "starter leather kit grants Lucky status", ref _run, ref _passed, ref _failed);
        }

        private static void TestLuckyAppearsAsStatusEffect()
        {
            TestBase.SetCurrentTestName(nameof(TestLuckyAppearsAsStatusEffect));
            var hero = new Character("LuckyHud", 1);
            hero.Equipment.Head = new HeadItem("Helmet", 1, 1) { Material = "Leather" };
            hero.Equipment.Body = new ChestItem("Armor", 1, 1) { Material = "Leather" };
            hero.Equipment.Feet = new FeetItem("Boots", 1, 1) { Material = "Leather" };

            var lines = RPGGame.UI.Avalonia.Layout.StatusEffectDisplayLines.Build(hero, hero);
            TestBase.AssertTrue(lines.Contains(LeatherSetBonus.LuckyStatusName),
                "Lucky appears in STATUS EFFECTS when leather set is active", ref _run, ref _passed, ref _failed);

            hero.Equipment.Body = new ChestItem("Iron Plate", 2, 5) { Material = "Iron" };
            var after = RPGGame.UI.Avalonia.Layout.StatusEffectDisplayLines.Build(hero, hero);
            TestBase.AssertFalse(after.Contains(LeatherSetBonus.LuckyStatusName),
                "Lucky leaves STATUS EFFECTS when set breaks", ref _run, ref _passed, ref _failed);
        }
    }
}
