using System;
using System.IO;
using System.Linq;
using RPGGame.Config;
using RPGGame.Tests;

namespace RPGGame.Tests.Unit.Config
{
    public static class PatchProfileServiceTests
    {
        public static void RunAllTests()
        {
            Console.WriteLine("=== PatchProfileService Tests ===\n");

            int testsRun = 0, testsPassed = 0, testsFailed = 0;

            TestListAndActivePatch(ref testsRun, ref testsPassed, ref testsFailed);
            TestSanitizePatchName(ref testsRun, ref testsPassed, ref testsFailed);
            TestCreateAndSwitchPatch(ref testsRun, ref testsPassed, ref testsFailed);

            TestBase.PrintSummary("PatchProfileService Tests", testsRun, testsPassed, testsFailed);
        }

        private static void TestListAndActivePatch(ref int testsRun, ref int testsPassed, ref int testsFailed)
        {
            TestBase.SetCurrentTestName(nameof(TestListAndActivePatch));
            string root = CreateTempGameDataRoot();
            try
            {
                PatchProfileServiceTestHooks.OverrideGameDataRoot(root);

                string gsPath = Path.Combine(root, "Patches", "GameSettings", "alpha.json");
                Directory.CreateDirectory(Path.GetDirectoryName(gsPath)!);
                File.WriteAllText(gsPath, "{}");

                var names = PatchProfileService.ListPatches(PatchCategory.GameSettings);
                TestBase.AssertTrue(names.Contains("alpha"), "lists committed patch", ref testsRun, ref testsPassed, ref testsFailed);

                string active = PatchProfileService.GetActivePatchFilePath(PatchCategory.GameSettings);
                TestBase.AssertTrue(active.EndsWith("default.json", StringComparison.OrdinalIgnoreCase)
                    || active.EndsWith("alpha.json", StringComparison.OrdinalIgnoreCase), "resolves active patch path", ref testsRun, ref testsPassed, ref testsFailed);
            }
            finally
            {
                try { Directory.Delete(root, true); } catch { }
            }
        }

        private static void TestSanitizePatchName(ref int testsRun, ref int testsPassed, ref int testsFailed)
        {
            TestBase.SetCurrentTestName(nameof(TestSanitizePatchName));
            TestBase.AssertEqual("my-patch", PatchProfileService.SanitizePatchName(" my-patch "), "trims", ref testsRun, ref testsPassed, ref testsFailed);
            bool threw = false;
            try { PatchProfileService.SanitizePatchName("bad name"); }
            catch { threw = true; }
            TestBase.AssertTrue(threw, "rejects spaces", ref testsRun, ref testsPassed, ref testsFailed);
        }

        private static void TestCreateAndSwitchPatch(ref int testsRun, ref int testsPassed, ref int testsFailed)
        {
            TestBase.SetCurrentTestName(nameof(TestCreateAndSwitchPatch));
            string root = CreateTempGameDataRoot();
            try
            {
                PatchProfileServiceTestHooks.OverrideGameDataRoot(root);

                PatchProfileService.CreatePatch(PatchCategory.Audio, "test-audio", "{\"masterVolume\":0.5}", switchActive: true);
                TestBase.AssertTrue(File.Exists(Path.Combine(root, "Patches", "Audio", "test-audio.json")), "creates patch file", ref testsRun, ref testsPassed, ref testsFailed);
                TestBase.AssertEqual("test-audio", PatchProfileService.LoadProfile().ActiveAudioPatch, "switches active patch", ref testsRun, ref testsPassed, ref testsFailed);
            }
            finally
            {
                try { Directory.Delete(root, true); } catch { }
            }
        }

        private static string CreateTempGameDataRoot()
        {
            string dir = Path.Combine(Path.GetTempPath(), "df-patch-test-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(dir);
            return dir;
        }
    }

    /// <summary>Test-only hook to redirect patch paths without touching GameConstants.</summary>
    internal static class PatchProfileServiceTestHooks
    {
        public static void OverrideGameDataRoot(string root) =>
            PatchProfileService.SetGameDataRootForTests(root);
    }
}
