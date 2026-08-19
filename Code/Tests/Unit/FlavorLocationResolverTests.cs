using System;
using System.Linq;
using RPGGame.Tests;

namespace RPGGame.Tests.Unit
{
    /// <summary>
    /// Theme / room-type resolution for FlavorText banks (tags → name → dungeon theme).
    /// </summary>
    public static class FlavorLocationResolverTests
    {
        private static int _testsRun;
        private static int _testsPassed;
        private static int _testsFailed;

        public static void RunAllTests()
        {
            Console.WriteLine("=== FlavorLocationResolver Tests ===\n");
            _testsRun = _testsPassed = _testsFailed = 0;

            TestAbyssalDepthsResolvesToSwampNotCrypt();
            TestNameMatchBeatsDungeonTheme();
            TestTagsDoNotMatchElementalVocabulary();
            TestExactTagKeyWinsWhenPresent();
            TestBossChamberRoomType();
            TestAuthoredRoomTypePreferred();
            TestKitchenAndLibraryRoomTypes();

            TestBase.PrintSummary("FlavorLocationResolver Tests", _testsRun, _testsPassed, _testsFailed);
        }

        private static void TestAbyssalDepthsResolvesToSwampNotCrypt()
        {
            Console.WriteLine("--- Abyssal Depths theme ---");
            string theme = FlavorLocationResolver.ResolveLocationTheme(
                "Abyssal Depths",
                new[] { "water", "flooded", "cycling" },
                "Crypt");
            TestBase.AssertEqual("Swamp", theme,
                "Abyssal Depths should resolve to Swamp (water bank), not Crypt",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        private static void TestNameMatchBeatsDungeonTheme()
        {
            Console.WriteLine("\n--- Name match vs dungeon theme ---");
            TestBase.AssertEqual("Forest",
                FlavorLocationResolver.ResolveLocationTheme("Ancient Grove", Array.Empty<string>(), "Crypt"),
                "grove name should resolve Forest inside a Crypt dungeon",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
            TestBase.AssertEqual("Ice",
                FlavorLocationResolver.ResolveLocationTheme("Frozen Cavern", new[] { "water", "exposed" }, "Crypt"),
                "Frozen Cavern should resolve Ice (name), not Swamp from water tag",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        private static void TestTagsDoNotMatchElementalVocabulary()
        {
            Console.WriteLine("\n--- Elemental tags are not flavor-bank keys ---");
            string theme = FlavorLocationResolver.ResolveLocationTheme(
                "Entrance",
                new[] { "earth", "exposed", "cycling" },
                "Crypt");
            TestBase.AssertEqual("Crypt", theme,
                "Elemental tags should fall through to dungeon theme when the name has no match",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        private static void TestExactTagKeyWinsWhenPresent()
        {
            Console.WriteLine("\n--- Exact tag key match ---");
            string theme = FlavorLocationResolver.ResolveLocationTheme(
                "Mystery Room",
                new[] { "Forest" },
                "Crypt");
            TestBase.AssertEqual("Forest", theme,
                "A tag that exactly matches a locationDescriptions key should win",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        private static void TestBossChamberRoomType()
        {
            Console.WriteLine("\n--- Boss Chamber room type ---");
            TestBase.AssertEqual("boss",
                FlavorLocationResolver.ResolveRoomType("", "Boss Chamber"),
                "Boss Chamber should infer room type boss (before chamber)",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        private static void TestAuthoredRoomTypePreferred()
        {
            Console.WriteLine("\n--- Authored RoomType ---");
            TestBase.AssertEqual("treasure",
                FlavorLocationResolver.ResolveRoomType("Treasure", "Boss Chamber"),
                "Authored RoomType should win over the display name",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        private static void TestKitchenAndLibraryRoomTypes()
        {
            Console.WriteLine("\n--- Kitchen / Library room types ---");
            TestBase.AssertEqual("kitchen",
                FlavorLocationResolver.ResolveRoomType("", "Kitchen"),
                "Kitchen display name should infer kitchen",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
            TestBase.AssertEqual("library",
                FlavorLocationResolver.ResolveRoomType("", "Library"),
                "Library display name should infer library",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
            TestBase.AssertEqual("dining",
                FlavorLocationResolver.ResolveRoomType("", "Dining Hall"),
                "Dining Hall display name should infer dining",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
        }
    }
}
