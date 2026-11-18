using System;
using RPGGame.Entity.Services;

namespace RPGGame.Tests.Unit.Rebuild
{
    /// <summary>Rebuild 2.5 (D4.7): charms are off: no drops, no slot, an equipped charm moves to the bag on load.</summary>
    public static class CharmsOffTests
    {
        private static int _run, _passed, _failed;

        public static void RunAllTests()
        {
            Console.WriteLine("=== CharmsOff Tests ===\n");
            _run = _passed = _failed = 0;
            var loot = GameConfiguration.Instance.LootSystem;
            bool old = loot.CharmsEnabled;
            try
            {
                loot.CharmsEnabled = false;
                TestBase.AssertFalse(new LootSystemConfig().CharmsEnabled, "Charms are off by default", ref _run, ref _passed, ref _failed);
                bool any = false;
                var rng = new Random(5);
                for (int i = 0; i < 400; i++)
                    any |= LootGenerator.TryRollCharmLoot(rng) != null;
                TestBase.AssertFalse(any, "No charm drops in 400 rolls", ref _run, ref _passed, ref _failed);

                var hero = TestDataBuilders.Character().WithName("No Charm").Build();
                bool equipped = hero.TryEquipItem(new CharmItem("Lucky Charm", tier: 1), "charm", out _, out string? why);
                TestBase.AssertFalse(equipped, $"The charm slot refuses gear ({why})", ref _run, ref _passed, ref _failed);
                TestBase.AssertFalse(Array.Exists(RPGGame.MCP.Screens.McpText.EquipSlots, s => s == "charm"), "MCP lists no charm slot", ref _run, ref _passed, ref _failed);

                // Save migration: an equipped charm loads into the bag.
                loot.CharmsEnabled = true;
                var veteran = TestDataBuilders.Character().WithName("Charm Wearer").Build();
                veteran.EquipItem(new CharmItem("Old Charm", tier: 1), "charm");
                string json = new CharacterSerializer().Serialize(veteran);
                loot.CharmsEnabled = false;
                var serializer = new CharacterSerializer();
                var loaded = serializer.CreateCharacterFromSaveData(serializer.Deserialize(json)!);
                TestBase.AssertTrue(loaded.Equipment.Charm == null, "The charm slot is empty after load", ref _run, ref _passed, ref _failed);
                TestBase.AssertTrue(loaded.Inventory.Exists(i => i.Name.Contains("Old Charm")), "The charm moved to the bag", ref _run, ref _passed, ref _failed);
            }
            finally
            {
                loot.CharmsEnabled = old;
            }
            TestBase.PrintSummary("CharmsOff Tests", _run, _passed, _failed);
        }
    }
}
