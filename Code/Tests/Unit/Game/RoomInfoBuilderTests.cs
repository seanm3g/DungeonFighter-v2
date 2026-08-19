using System;
using System.Collections.Generic;
using System.Linq;
using RPGGame.Display.Dungeon;
using RPGGame.Tests;
using RPGGame.UI.ColorSystem;

namespace RPGGame.Tests.Unit.Game
{
    /// <summary>
    /// Room-entry flavor wiring: split buffer lines, theme resolution, roomContexts
    /// preferred over locationDescriptions when {biome}/{roomType} exists.
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

            TestCryptGuardPostSplitsUntruncatedLines();
            TestAbyssalDepthsUsesWaterFlavorNotCrypt();
            TestForestBossAppendsRoomContextNotLocation();
            TestForestKitchenPrefersRoomContext();
            TestForestLibraryPrefersRoomContext();
            TestForestSanctumPrefersRoomContext();
            TestForestShrinePrefersRoomContext();
            TestForestTreasurePrefersRoomContext();
            TestForestLocationDescriptionsFitDisplayCap();
            TestForestDiningHallFallsBackToLocationDescriptions();
            TestHasRoomContextDoesNotUseGenericFallback();

            TestBase.PrintSummary("RoomInfoBuilder Tests", _testsRun, _testsPassed, _testsFailed);
        }

        private static void TestCryptGuardPostSplitsUntruncatedLines()
        {
            Console.WriteLine("--- Crypt Guard Post split / no truncation ---");

            const string description = "Ancient stone walls lined with burial niches, the air thick with dust and decay.";
            var room = new Environment("Guard Post", description, true, "Crypt");
            var lines = RoomInfoBuilder.BuildRoomInfo(room, 1, 5);
            var plain = lines.Select(PlainText).ToList();

            TestBase.AssertTrue(plain.Any(l => l == description),
                "Rooms.json description should be its own line",
                ref _testsRun, ref _testsPassed, ref _testsFailed);

            var flavorBanks = FlavorText.GetData().Environments.LocationDescriptions;
            TestBase.AssertTrue(plain.Any(l => flavorBanks["Crypt"].Contains(l)),
                "Crypt locationDescriptions line should be a separate row (no Crypt/guard roomContexts)",
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

        private static void TestForestBossAppendsRoomContextNotLocation()
        {
            Console.WriteLine("\n--- Forest / boss roomContexts wins ---");

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
            TestBase.AssertTrue(contextIndex >= 0,
                "Forest boss should append roomContexts, not locationDescriptions",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
            TestBase.AssertTrue(flavorIndex < 0,
                "Forest boss should not also append locationDescriptions",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
            TestBase.AssertTrue(descIndex < contextIndex,
                "Content lines should appear in order: description, then room context",
                ref _testsRun, ref _testsPassed, ref _testsFailed);

            Console.WriteLine($"  appended roomContexts: {plain[contextIndex]}");
        }

        private static void TestForestKitchenPrefersRoomContext()
        {
            Console.WriteLine("\n--- Forest / Kitchen roomContexts wins ---");
            AssertForestRoomTypePrefersContext(
                "Kitchen",
                "A large cooking area, with pots and pans hanging from the ceiling.",
                "kitchen");
        }

        private static void TestForestLibraryPrefersRoomContext()
        {
            Console.WriteLine("\n--- Forest / Library roomContexts wins ---");
            AssertForestRoomTypePrefersContext(
                "Library",
                "Rows of ancient tomes and scrolls fill this room.",
                "library");

            var libraryBank = FlavorText.GetData().Environments.RoomContexts["Forest"]["library"];
            TestBase.AssertTrue(libraryBank.Length > 0,
                "Forest/library roomContexts bank should not be empty",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
            foreach (var line in libraryBank)
            {
                TestBase.AssertTrue(line.Length <= 152,
                    $"Forest/library roomContexts line should be <= 152 chars (was {line.Length}): {line}",
                    ref _testsRun, ref _testsPassed, ref _testsFailed);
            }
        }

        private static void TestForestSanctumPrefersRoomContext()
        {
            Console.WriteLine("\n--- Forest / Sanctum roomContexts wins ---");
            AssertForestRoomTypePrefersContext(
                "Sanctum",
                "A hollow ring of trees, quieter than the rest of the wood.",
                "sanctum");

            AssertForestRoomContextBankFitsDisplayCap("sanctum", expectedCount: 4);
        }

        private static void TestForestShrinePrefersRoomContext()
        {
            Console.WriteLine("\n--- Forest / Shrine roomContexts wins ---");
            AssertForestRoomTypePrefersContext(
                "Shrine",
                "A quiet grove of offerings tucked into the roots.",
                "shrine");
            AssertForestRoomContextBankFitsDisplayCap("shrine", expectedCount: 4);
        }

        private static void TestForestTreasurePrefersRoomContext()
        {
            Console.WriteLine("\n--- Forest / Treasure roomContexts wins ---");
            AssertForestRoomTypePrefersContext(
                "Treasure Chamber",
                "A cache of coin packed into a hollow trunk.",
                "treasure");
            AssertForestRoomContextBankFitsDisplayCap("treasure", expectedCount: 4);
        }

        private static void TestForestLocationDescriptionsFitDisplayCap()
        {
            Console.WriteLine("\n--- Forest flavor banks 152-char cap ---");
            var locationBank = FlavorText.GetData().Environments.LocationDescriptions["Forest"];
            TestBase.AssertEqual(9, locationBank.Length,
                "Forest locationDescriptions bank should have 9 lines",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
            for (int i = 0; i < locationBank.Length; i++)
            {
                TestBase.AssertTrue(locationBank[i].Length <= 152,
                    $"Forest locationDescriptions[{i}] should be <= 152 chars (was {locationBank[i].Length}): {locationBank[i]}",
                    ref _testsRun, ref _testsPassed, ref _testsFailed);
            }

            string[] roomTypes = { "armory", "boss", "chamber", "kitchen", "library", "sanctum", "shrine", "treasure" };
            var forestContexts = FlavorText.GetData().Environments.RoomContexts["Forest"];
            foreach (var roomType in roomTypes)
            {
                TestBase.AssertTrue(forestContexts.ContainsKey(roomType),
                    $"Forest/{roomType} roomContexts bank should exist",
                    ref _testsRun, ref _testsPassed, ref _testsFailed);
                var bank = forestContexts[roomType];
                for (int i = 0; i < bank.Length; i++)
                {
                    TestBase.AssertTrue(bank[i].Length <= 152,
                        $"Forest/{roomType}[{i}] should be <= 152 chars (was {bank[i].Length}): {bank[i]}",
                        ref _testsRun, ref _testsPassed, ref _testsFailed);
                }
            }
        }

        private static void AssertForestRoomContextBankFitsDisplayCap(string roomTypeKey, int expectedCount)
        {
            var bank = FlavorText.GetData().Environments.RoomContexts["Forest"][roomTypeKey];
            TestBase.AssertEqual(expectedCount, bank.Length,
                $"Forest/{roomTypeKey} roomContexts bank should have {expectedCount} lines",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
            for (int i = 0; i < bank.Length; i++)
            {
                TestBase.AssertTrue(bank[i].Length <= 152,
                    $"Forest/{roomTypeKey}[{i}] should be <= 152 chars (was {bank[i].Length}): {bank[i]}",
                    ref _testsRun, ref _testsPassed, ref _testsFailed);
            }
        }

        private static void AssertForestRoomTypePrefersContext(string roomName, string description, string roomTypeKey)
        {
            var room = new Environment(roomName, description, true, "Forest");
            TestBase.AssertEqual(roomTypeKey, FlavorLocationResolver.ResolveRoomType(room),
                $"{roomName} should resolve room type {roomTypeKey}",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
            TestBase.AssertTrue(FlavorText.HasRoomContext("Forest", roomTypeKey),
                $"Forest/{roomTypeKey} roomContexts bank should exist",
                ref _testsRun, ref _testsPassed, ref _testsFailed);

            var lines = RoomInfoBuilder.BuildRoomInfo(room, 2, 5);
            var plain = lines.Where(l => !string.IsNullOrWhiteSpace(PlainText(l))).Select(PlainText).ToList();
            var locationBank = FlavorText.GetData().Environments.LocationDescriptions["Forest"];
            var contextBank = FlavorText.GetData().Environments.RoomContexts["Forest"][roomTypeKey];
            var genericBank = FlavorText.GetData().Environments.RoomContexts["Generic"][roomTypeKey];

            int descIndex = plain.FindIndex(l => l == description);
            int contextIndex = plain.FindIndex(l => contextBank.Contains(l));
            int locationIndex = plain.FindIndex(l => locationBank.Contains(l));
            int genericIndex = plain.FindIndex(l => genericBank.Contains(l));

            TestBase.AssertTrue(descIndex >= 0,
                $"{roomName} should keep the Rooms.json description",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
            TestBase.AssertTrue(contextIndex >= 0,
                $"{roomName} should append Forest/{roomTypeKey} roomContexts",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
            TestBase.AssertTrue(locationIndex < 0,
                $"{roomName} should not append Forest locationDescriptions",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
            TestBase.AssertTrue(genericIndex < 0,
                $"{roomName} should not fall back to Generic/{roomTypeKey} roomContexts",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
            TestBase.AssertTrue(descIndex < contextIndex,
                $"{roomName} flavor should append under the Rooms.json description",
                ref _testsRun, ref _testsPassed, ref _testsFailed);

            Console.WriteLine($"  room: {roomName}");
            Console.WriteLine($"  Rooms.json: {description}");
            Console.WriteLine($"  appended roomContexts (Forest/{roomTypeKey}): {plain[contextIndex]}");
            Console.WriteLine($"  appended length: {plain[contextIndex].Length} (cap 152)");
            TestBase.AssertTrue(plain[contextIndex].Length <= 152,
                $"{roomName} appended flavor should be <= 152 chars (was {plain[contextIndex].Length})",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        private static void TestForestDiningHallFallsBackToLocationDescriptions()
        {
            Console.WriteLine("\n--- Forest / Dining Hall locationDescriptions fallback ---");

            const string description = "A long table set for a feast, though the food looks... questionable.";
            var room = new Environment("Dining Hall", description, true, "Forest");
            TestBase.AssertEqual("dining", FlavorLocationResolver.ResolveRoomType(room),
                "Dining Hall should resolve room type dining",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
            TestBase.AssertTrue(!FlavorText.HasRoomContext("Forest", "dining"),
                "Forest/dining should have no roomContexts bank",
                ref _testsRun, ref _testsPassed, ref _testsFailed);

            var lines = RoomInfoBuilder.BuildRoomInfo(room, 3, 5);
            var plain = lines.Where(l => !string.IsNullOrWhiteSpace(PlainText(l))).Select(PlainText).ToList();
            var locationBank = FlavorText.GetData().Environments.LocationDescriptions["Forest"];
            var genericDining = FlavorText.GetData().Environments.RoomContexts["Generic"]["dining"];
            var forestContexts = FlavorText.GetData().Environments.RoomContexts["Forest"]
                .SelectMany(kv => kv.Value)
                .ToHashSet(StringComparer.Ordinal);

            int descIndex = plain.FindIndex(l => l == description);
            int locationIndex = plain.FindIndex(l => locationBank.Contains(l));
            int genericIndex = plain.FindIndex(l => genericDining.Contains(l));
            int forestContextIndex = plain.FindIndex(l => forestContexts.Contains(l));

            TestBase.AssertTrue(descIndex >= 0,
                "Dining Hall should keep the Rooms.json description",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
            TestBase.AssertTrue(locationIndex >= 0,
                "Dining Hall should append Forest locationDescriptions (no Forest/dining roomContexts)",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
            TestBase.AssertTrue(genericIndex < 0,
                "Dining Hall should not use Generic/dining roomContexts",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
            TestBase.AssertTrue(forestContextIndex < 0,
                "Dining Hall should not append any Forest roomContexts line",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
            TestBase.AssertTrue(descIndex < locationIndex,
                "Dining Hall flavor should append under the Rooms.json description",
                ref _testsRun, ref _testsPassed, ref _testsFailed);

            Console.WriteLine($"  room: Dining Hall");
            Console.WriteLine($"  Rooms.json: {description}");
            Console.WriteLine($"  appended locationDescriptions (Forest): {plain[locationIndex]}");
        }

        private static void TestHasRoomContextDoesNotUseGenericFallback()
        {
            Console.WriteLine("\n--- HasRoomContext ignores Generic fallback ---");
            TestBase.AssertTrue(FlavorText.HasRoomContext("Forest", "kitchen"),
                "Forest/kitchen should report a roomContexts match",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
            TestBase.AssertTrue(FlavorText.HasRoomContext("forest", "Kitchen"),
                "Biome/roomType lookup should be case-insensitive (forest/Kitchen)",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
            TestBase.AssertTrue(!FlavorText.HasRoomContext("Forest", "dining"),
                "Forest/dining should not count Generic/dining as a match",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
            TestBase.AssertTrue(!FlavorText.HasRoomContext("Forest", "hall"),
                "Forest/hall should not count Generic/hall as a match",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        private static string PlainText(string markup)
        {
            var parsed = ColoredTextParser.Parse(markup ?? "");
            return string.Concat(parsed.Select(s => s.Text));
        }
    }
}
