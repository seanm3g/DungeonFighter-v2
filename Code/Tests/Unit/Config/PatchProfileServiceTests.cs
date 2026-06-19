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
            TestGameSettingsAlwaysUseDefault(ref testsRun, ref testsPassed, ref testsFailed);
            TestSanitizePatchName(ref testsRun, ref testsPassed, ref testsFailed);
            TestCreateAndSwitchPatch(ref testsRun, ref testsPassed, ref testsFailed);
            TestCreateAndSwitchBalancePatch(ref testsRun, ref testsPassed, ref testsFailed);
            TestAudioDefaultSeededFromTemplate(ref testsRun, ref testsPassed, ref testsFailed);

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
                TestBase.AssertTrue(active.EndsWith("default.json", StringComparison.OrdinalIgnoreCase),
                    "game settings always resolve to default patch path", ref testsRun, ref testsPassed, ref testsFailed);
            }
            finally
            {
                try { Directory.Delete(root, true); } catch { }
            }
        }

        private static void TestGameSettingsAlwaysUseDefault(ref int testsRun, ref int testsPassed, ref int testsFailed)
        {
            TestBase.SetCurrentTestName(nameof(TestGameSettingsAlwaysUseDefault));
            string root = CreateTempGameDataRoot();
            try
            {
                PatchProfileServiceTestHooks.OverrideGameDataRoot(root);

                string customGs = Path.Combine(root, "Patches", "GameSettings", "custom.json");
                Directory.CreateDirectory(Path.GetDirectoryName(customGs)!);
                File.WriteAllText(customGs, "{}");

                var profile = PatchProfileService.LoadProfile();
                profile.ActiveGameSettingsPatch = "custom";
                PatchProfileService.SaveProfile(profile);

                string gsActive = PatchProfileService.GetActivePatchFilePath(PatchCategory.GameSettings);
                TestBase.AssertTrue(gsActive.EndsWith("default.json", StringComparison.OrdinalIgnoreCase),
                    "game settings always use repo default patch", ref testsRun, ref testsPassed, ref testsFailed);

                bool threw = false;
                try { PatchProfileService.SetActivePatch(PatchCategory.GameSettings, "custom"); }
                catch (InvalidOperationException) { threw = true; }
                TestBase.AssertTrue(threw, "cannot switch game settings patch locally", ref testsRun, ref testsPassed, ref testsFailed);
            }
            finally
            {
                try { Directory.Delete(root, true); } catch { }
            }
        }

        private static void TestAudioDefaultSeededFromTemplate(ref int testsRun, ref int testsPassed, ref int testsFailed)
        {
            TestBase.SetCurrentTestName(nameof(TestAudioDefaultSeededFromTemplate));
            string root = CreateTempGameDataRoot();
            try
            {
                PatchProfileServiceTestHooks.OverrideGameDataRoot(root);

                string templateDir = Path.Combine(root, "Patches", "Audio");
                Directory.CreateDirectory(templateDir);
                File.WriteAllText(Path.Combine(templateDir, "default.template.json"),
                    "{\"masterVolume\":0.25,\"cueMap\":{\"Menu_Select\":{\"file\":\"SFX/test.wav\",\"volume\":1.0}},\"stateMusicMap\":{\"MainMenu\":\"Menu_Select\"}}");

                string active = PatchProfileService.GetActivePatchFilePath(PatchCategory.Audio);
                TestBase.AssertTrue(File.Exists(active), "creates local default audio patch", ref testsRun, ref testsPassed, ref testsFailed);
                string content = File.ReadAllText(active);
                TestBase.AssertTrue(content.Contains("SFX/test.wav"), "seeds cue bindings from template", ref testsRun, ref testsPassed, ref testsFailed);
                TestBase.AssertFalse(content.Contains("masterVolume"), "strips bus volume from seeded patch", ref testsRun, ref testsPassed, ref testsFailed);
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
            TestBase.AssertEqual("bad-name", PatchProfileService.SanitizePatchName("bad name"), "spaces become dashes", ref testsRun, ref testsPassed, ref testsFailed);
            TestBase.AssertEqual("my-audio-volumes", PatchProfileService.SanitizePatchName("my_audio_volumes"), "underscores become dashes", ref testsRun, ref testsPassed, ref testsFailed);
            TestBase.AssertEqual("my-audio-volumes", PatchProfileService.SanitizePatchName("my--audio---volumes"), "collapses repeated dashes", ref testsRun, ref testsPassed, ref testsFailed);
            bool threw = false;
            try { PatchProfileService.SanitizePatchName("   "); }
            catch { threw = true; }
            TestBase.AssertTrue(threw, "rejects empty after normalization", ref testsRun, ref testsPassed, ref testsFailed);
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

        private static void TestCreateAndSwitchBalancePatch(ref int testsRun, ref int testsPassed, ref int testsFailed)
        {
            TestBase.SetCurrentTestName(nameof(TestCreateAndSwitchBalancePatch));
            string root = CreateTempGameDataRoot();
            try
            {
                PatchProfileServiceTestHooks.OverrideGameDataRoot(root);

                PatchProfileService.CreatePatch(PatchCategory.Balance, "test-balance", "{\"combat\":{}}", switchActive: true);
                TestBase.AssertTrue(File.Exists(Path.Combine(root, "Patches", "Balance", "test-balance.json")), "creates balance patch file", ref testsRun, ref testsPassed, ref testsFailed);
                TestBase.AssertEqual("test-balance", PatchProfileService.LoadProfile().ActiveBalancePatch, "switches active balance patch", ref testsRun, ref testsPassed, ref testsFailed);

                string active = PatchProfileService.GetActivePatchFilePath(PatchCategory.Balance);
                TestBase.AssertTrue(active.EndsWith("test-balance.json", StringComparison.OrdinalIgnoreCase),
                    "resolves active balance patch path", ref testsRun, ref testsPassed, ref testsFailed);

                PatchProfileService.SetActivePatch(PatchCategory.Balance, "test-balance");
                TestBase.AssertEqual("test-balance", PatchProfileService.LoadProfile().ActiveBalancePatch,
                    "SetActivePatch succeeds for balance", ref testsRun, ref testsPassed, ref testsFailed);
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
