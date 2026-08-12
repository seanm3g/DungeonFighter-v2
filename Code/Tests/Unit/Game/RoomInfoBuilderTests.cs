using System;
using System.Collections.Generic;
using System.Linq;
using RPGGame.Display.Dungeon;
using RPGGame.Tests;
using RPGGame.UI.ColorSystem;

namespace RPGGame.Tests.Unit.Game
{
    /// <summary>
    /// Room-entry flavor wiring: split buffer lines, theme resolution, roomContexts append.
    /// </summary>
    public static class RoomInfoBuilderTests
    {
        private static int _testsRun;
        private static int _testsPassed;
        private static int _testsFailed;

        public static void RunAllTests()
        {
            Console.WriteLine("=== RoomInfoBuilder Tests ===\n");
            _testsRun = _testsPassed = _testsFailed = 0;

            TestCryptPassageSplitsUntruncatedLines();
            TestAbyssalDepthsUsesWaterFlavorNotCrypt();
            TestForestBossAppendsThreeContentLines();

            TestBase.PrintSummary("RoomInfoBuilder Tests", _testsRun, _testsPassed, _testsFailed);
        }

        private static void TestCryptPassageSplitsUntruncatedLines()
        {
            Console.WriteLine("--- Crypt Passage split / no truncation ---");

            const string description = "Ancient stone walls lined with burial niches, the air thick with dust and decay.";
            var room = new Environment("Crypt Passage", description, true, "Crypt");
            var lines = RoomInfoBuilder.BuildRoomInfo(room, 1, 5);
            var plain = lines.Select(PlainText).ToList();

            TestBase.AssertTrue(plain.Any(l => l == description),
                "Rooms.json description should be its own line",
                ref _testsRun, ref _testsPassed, ref _testsFailed);

            var flavorBanks = FlavorText.GetData().Environments.LocationDescriptions;
            TestBase.AssertTrue(plain.Any(l => flavorBanks["Crypt"].Contains(l)),
                "Crypt locationDescriptions line should be a separate row",
                ref _testsRun, ref _testsPassed, ref _testsFailed);

            foreach (var line in plain.Where(l => !string.IsNullOrEmpty(l)))
            {
                TestBase.AssertTrue(line.Length <= 152,
                    $"Room info line should be <= 152 chars (was {line.Length}): {line}",
                    ref _testsRun, ref _testsPassed, ref _testsFailed);
                TestBase.AssertTrue(!line.EndsWith("...", StringComparison.Ordinal),
                    "Room info line should not be ellipsis-truncated",
                    ref _testsRun, ref _testsPassed, ref _testsFailed);
            }

            int combined = description.Length + 2 + flavorBanks["Crypt"].Max(s => s.Length);
            TestBase.AssertTrue(combined > 152,
                "Sanity: concatenated description+flavor would exceed the 152-char buffer cap",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        private static void TestAbyssalDepthsUsesWaterFlavorNotCrypt()
        {
            Console.WriteLine("\n--- Abyssal Depths in Crypt dungeon ---");

            const string description = "The crushing pressure of the deep ocean creates an otherworldly atmosphere.";
            var room = new Environment("Abyssal Depths", description, true, "Crypt");
            RoomLoader.ApplyEnvironmentTags(room, new[] { "water", "flooded", "cycling" }, "Crypt");

            var lines = RoomInfoBuilder.BuildRoomInfo(room, 2, 5);
            var plain = lines.Select(PlainText).ToList();
            var banks = FlavorText.GetData().Environments.LocationDescriptions;

            TestBase.AssertTrue(plain.Any(l => l == description),
                "Abyssal Depths should keep the Rooms.json description",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
            TestBase.AssertTrue(plain.Any(l => banks["Swamp"].Contains(l)),
                "Abyssal Depths should pull Swamp (water) locationDescriptions, not dungeon theme",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
            TestBase.AssertTrue(!plain.Any(l => banks["Crypt"].Contains(l)),
                "Abyssal Depths should not pull Crypt shelf flavor",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        private static void TestForestBossAppendsThreeContentLines()
        {
            Console.WriteLine("\n--- Forest / boss three-line append ---");

            const string description = "A grand chamber, clearly the domain of a powerful being. The walls are adorned with trophies of past conquests.";
            var room = new Environment("Boss Chamber", description, true, "Forest", "boss");
            var lines = RoomInfoBuilder.BuildRoomInfo(room, 5, 5);
            var plain = lines.Where(l => !string.IsNullOrWhiteSpace(PlainText(l))).Select(PlainText).ToList();

            var locationBank = FlavorText.GetData().Environments.LocationDescriptions["Forest"];
            var contextBank = FlavorText.GetData().Environments.RoomContexts["Forest"]["boss"];

            int descIndex = plain.FindIndex(l => l == description);
            int flavorIndex = plain.FindIndex(l => locationBank.Contains(l));
            int contextIndex = plain.FindIndex(l => contextBank.Contains(l));

            TestBase.AssertTrue(descIndex >= 0,
                "Forest boss should include Rooms.json description",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
            TestBase.AssertTrue(flavorIndex >= 0,
                "Forest boss should include locationDescriptions line",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
            TestBase.AssertTrue(contextIndex >= 0,
                "Forest boss should include roomContexts line",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
            TestBase.AssertTrue(descIndex < flavorIndex && flavorIndex < contextIndex,
                "Three content lines should appear in order: description, location, room context",
                ref _testsRun, ref _testsPassed, ref _testsFailed);

            foreach (var line in new[] { description, plain[flavorIndex], plain[contextIndex] })
            {
                TestBase.AssertTrue(line.Length <= 152,
                    $"Forest boss line should be untruncated (len {line.Length})",
                    ref _testsRun, ref _testsPassed, ref _testsFailed);
            }
        }

        private static string PlainText(string markup)
        {
            var parsed = ColoredTextParser.Parse(markup ?? "");
            return string.Concat(parsed.Select(s => s.Text));
        }
    }
}
