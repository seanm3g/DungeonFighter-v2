using System;
using System.Linq;
using System.Text.Json.Nodes;
using RPGGame.Entity.Services;

namespace RPGGame.Tests.Unit.Rebuild
{
    /// <summary>Rebuild 0.3: systems unlock on their first event and are saved with the character.</summary>
    public static class SystemUnlocksTests
    {
        private static int _run, _passed, _failed;

        public static void RunAllTests()
        {
            Console.WriteLine("=== SystemUnlocks Tests ===\n");
            _run = _passed = _failed = 0;

            New_Record_Opens_Only_The_Board();
            First_Item_Opens_Items_And_Rarity_Rungs();
            Unlock_Fires_Once();
            Strip_And_Material_Events();
            Seen_Keys_For_Statuses();
            Save_Round_Trip_Keeps_Unlocks();
            Legacy_Save_Infers_Unlocks();

            TestBase.PrintSummary("SystemUnlocks Tests", _run, _passed, _failed);
        }

        private static void New_Record_Opens_Only_The_Board()
        {
            var u = new SystemUnlocks();
            TestBase.AssertTrue(u.IsUnlocked(GameSystem.Board), "The board is open from the first fight", ref _run, ref _passed, ref _failed);
            foreach (var s in Enum.GetValues<GameSystem>().Where(s => s != GameSystem.Board))
                TestBase.AssertFalse(u.IsUnlocked(s), $"{s} starts locked", ref _run, ref _passed, ref _failed);
        }

        private static void First_Item_Opens_Items_And_Rarity_Rungs()
        {
            var u = new SystemUnlocks();
            u.OnItemReceived(new Item(ItemType.Chest) { Name = "Coat", Rarity = "Common" });
            TestBase.AssertTrue(u.IsUnlocked(GameSystem.Items), "A Common opens Items", ref _run, ref _passed, ref _failed);
            TestBase.AssertFalse(u.IsUnlocked(GameSystem.Synergies), "A Common does not open Synergies", ref _run, ref _passed, ref _failed);

            u.OnItemReceived(new Item(ItemType.Chest) { Name = "Boots", Rarity = "Uncommon" });
            TestBase.AssertTrue(u.IsUnlocked(GameSystem.Synergies), "An Uncommon opens Synergies", ref _run, ref _passed, ref _failed);
            TestBase.AssertFalse(u.IsUnlocked(GameSystem.LootDepth), "An Uncommon does not open loot depth", ref _run, ref _passed, ref _failed);

            var lucky = new SystemUnlocks();
            lucky.OnItemReceived(new Item(ItemType.Chest) { Name = "Crown", Rarity = "Legendary" });
            TestBase.AssertTrue(lucky.IsUnlocked(GameSystem.LootDepth) && lucky.IsUnlocked(GameSystem.Synergies),
                "A lucky early Legendary opens its rungs early (G1.5)", ref _run, ref _passed, ref _failed);
        }

        private static void Unlock_Fires_Once()
        {
            var u = new SystemUnlocks();
            int fired = 0;
            u.Unlocked += (_, _) => fired++;
            bool first = u.Unlock(GameSystem.Bosses, "boss");
            bool second = u.Unlock(GameSystem.Bosses, "boss again");
            TestBase.AssertTrue(first, "First unlock returns true", ref _run, ref _passed, ref _failed);
            TestBase.AssertFalse(second, "Second unlock returns false", ref _run, ref _passed, ref _failed);
            TestBase.AssertEqual(1, fired, "Unlocked event fires once", ref _run, ref _passed, ref _failed);
        }

        private static void Strip_And_Material_Events()
        {
            var u = new SystemUnlocks();
            TestBase.AssertFalse(u.OnStripLengthChanged(1), "One slot keeps Combos locked", ref _run, ref _passed, ref _failed);
            TestBase.AssertTrue(u.OnStripLengthChanged(2), "Two slots open Combos", ref _run, ref _passed, ref _failed);

            var one = new Item(ItemType.Chest) { Name = "A", Material = "Iron" };
            var two = new Item(ItemType.Chest) { Name = "B", Material = "Leather" };
            TestBase.AssertFalse(u.OnEquipmentChanged(new Item?[] { one, two, null }), "Two different materials keep sets locked", ref _run, ref _passed, ref _failed);
            var three = new Item(ItemType.Chest) { Name = "C", Material = "iron" };
            TestBase.AssertTrue(u.OnEquipmentChanged(new Item?[] { one, two, three }), "Two Iron pieces open material sets", ref _run, ref _passed, ref _failed);

            TestBase.AssertFalse(u.OnSkillPointAvailable(0), "No points keeps the tree locked", ref _run, ref _passed, ref _failed);
            TestBase.AssertTrue(u.OnSkillPointAvailable(1), "A point opens the tree", ref _run, ref _passed, ref _failed);
        }

        private static void Seen_Keys_For_Statuses()
        {
            var u = new SystemUnlocks();
            TestBase.AssertTrue(u.OnStatusApplied("Bleed"), "First status opens Statuses", ref _run, ref _passed, ref _failed);
            TestBase.AssertTrue(u.HasSeen("status", "bleed"), "Bleed is marked seen", ref _run, ref _passed, ref _failed);
            TestBase.AssertTrue(u.OnStatusApplied("Rattle"), "A new status is still news", ref _run, ref _passed, ref _failed);
            TestBase.AssertFalse(u.OnStatusApplied("bleed"), "A seen status is not news", ref _run, ref _passed, ref _failed);
        }

        private static void Save_Round_Trip_Keeps_Unlocks()
        {
            var hero = TestDataBuilders.Character().WithName("Unlock Tester").WithLevel(1).Build();
            hero.Unlocks = new SystemUnlocks();
            hero.Unlocks.Unlock(GameSystem.Items);
            hero.Unlocks.MarkSeen("status", "bleed");

            var serializer = new CharacterSerializer();
            string json = serializer.Serialize(hero);
            TestBase.AssertTrue(json.Contains("\"systemUnlocks\""), "The save writes systemUnlocks", ref _run, ref _passed, ref _failed);
            var data = serializer.Deserialize(json)!;
            var loaded = serializer.CreateCharacterFromSaveData(data);
            TestBase.AssertTrue(loaded.Unlocks.IsUnlocked(GameSystem.Items), "Items survives the round trip", ref _run, ref _passed, ref _failed);
            TestBase.AssertTrue(loaded.Unlocks.HasSeen("status", "bleed"), "Seen keys survive the round trip", ref _run, ref _passed, ref _failed);
            TestBase.AssertFalse(loaded.Unlocks.IsUnlocked(GameSystem.Bosses), "Locked rungs stay locked after load", ref _run, ref _passed, ref _failed);
        }

        private static void Legacy_Save_Infers_Unlocks()
        {
            var hero = TestDataBuilders.Character().WithName("Old Timer").WithLevel(8).Build();
            var serializer = new CharacterSerializer();
            var node = JsonNode.Parse(serializer.Serialize(hero))!.AsObject();
            node.Remove("systemUnlocks");
            var data = serializer.Deserialize(node.ToJsonString())!;
            TestBase.AssertTrue(data.SystemUnlocks == null, "A legacy save has no unlock record", ref _run, ref _passed, ref _failed);
            var loaded = serializer.CreateCharacterFromSaveData(data);
            TestBase.AssertTrue(loaded.Unlocks.IsUnlocked(GameSystem.Items), "A level 8 legacy hero has Items", ref _run, ref _passed, ref _failed);
            TestBase.AssertTrue(loaded.Unlocks.IsUnlocked(GameSystem.Bosses), "A level 8 legacy hero has Bosses", ref _run, ref _passed, ref _failed);
            TestBase.AssertTrue(loaded.Unlocks.IsUnlocked(GameSystem.SkillTree), "A level 8 legacy hero has the skill tree", ref _run, ref _passed, ref _failed);
        }
    }
}
