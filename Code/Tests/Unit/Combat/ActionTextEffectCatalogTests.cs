using System;
using RPGGame.Combat.Sequence;
using RPGGame.Tests;

namespace RPGGame.Tests.Unit.Combat
{
    public static class ActionTextEffectCatalogTests
    {
        private static int _run, _passed, _failed;

        public static void RunAllTests()
        {
            Console.WriteLine("=== ActionTextEffectCatalog Tests ===\n");
            _run = _passed = _failed = 0;

            TestSlamMapsToDistortionPulse();
            TestUnknownIsNone();
            TestNullAndBlankAreNone();
            TestCaseInsensitive();

            TestBase.PrintSummary("ActionTextEffectCatalog Tests", _run, _passed, _failed);
        }

        private static void TestSlamMapsToDistortionPulse()
        {
            Console.WriteLine("--- SLAM maps to DistortionPulse ---");
            TestBase.AssertTrue(
                ActionTextEffectCatalog.Resolve("SLAM") == ActionTextEffectKind.DistortionPulse,
                "SLAM → DistortionPulse",
                ref _run, ref _passed, ref _failed);
            TestBase.AssertTrue(
                ActionTextEffectCatalog.IsDistortionPulse("SLAM"),
                "IsDistortionPulse(SLAM)",
                ref _run, ref _passed, ref _failed);
        }

        private static void TestUnknownIsNone()
        {
            Console.WriteLine("\n--- Unknown action is None ---");
            TestBase.AssertTrue(
                ActionTextEffectCatalog.Resolve("STRIKE") == ActionTextEffectKind.None,
                "STRIKE → None",
                ref _run, ref _passed, ref _failed);
            TestBase.AssertTrue(
                !ActionTextEffectCatalog.IsDistortionPulse("POUND"),
                "POUND is not DistortionPulse",
                ref _run, ref _passed, ref _failed);
        }

        private static void TestNullAndBlankAreNone()
        {
            Console.WriteLine("\n--- Null/blank are None ---");
            TestBase.AssertTrue(
                ActionTextEffectCatalog.Resolve(null) == ActionTextEffectKind.None,
                "null → None",
                ref _run, ref _passed, ref _failed);
            TestBase.AssertTrue(
                ActionTextEffectCatalog.Resolve("   ") == ActionTextEffectKind.None,
                "blank → None",
                ref _run, ref _passed, ref _failed);
        }

        private static void TestCaseInsensitive()
        {
            Console.WriteLine("\n--- Case insensitive ---");
            TestBase.AssertTrue(
                ActionTextEffectCatalog.Resolve("slam") == ActionTextEffectKind.DistortionPulse,
                "slam → DistortionPulse",
                ref _run, ref _passed, ref _failed);
        }
    }
}
