using System;
using System.IO;
using System.Text.Json;
using RPGGame.Audio;
using RPGGame.Config;
using RPGGame.Tests;

namespace RPGGame.Tests.Unit.Config
{
    public static class GeneralSettingsStoreTests
    {
        public static void RunAllTests()
        {
            Console.WriteLine("=== GeneralSettingsStore Tests ===\n");

            int testsRun = 0, testsPassed = 0, testsFailed = 0;

            TestLoadSaveRoundTrip(ref testsRun, ref testsPassed, ref testsFailed);
            TestMigrateFromLegacyGameSettingsPatch(ref testsRun, ref testsPassed, ref testsFailed);
            TestMigrateFromLegacyAudioPatchBusFields(ref testsRun, ref testsPassed, ref testsFailed);
            TestAudioConfigSplitSave(ref testsRun, ref testsPassed, ref testsFailed);

            TestBase.PrintSummary("GeneralSettingsStore Tests", testsRun, testsPassed, testsFailed);
        }

        private static void TestLoadSaveRoundTrip(ref int testsRun, ref int testsPassed, ref int testsFailed)
        {
            TestBase.SetCurrentTestName(nameof(TestLoadSaveRoundTrip));
            string root = CreateTempRoot();
            try
            {
                PatchProfileServiceTestHooks.OverrideGameDataRoot(root);
                GeneralSettingsStore.ResetCacheForTests();

                var gs = new GameSettings { CombatSpeed = 1.5, FastCombat = true };
                var prefs = new AudioPreferences { MasterVolume = 0.42f, SfxVolume = 0.55f };
                GeneralSettingsStore.Save(gs, prefs);

                GeneralSettingsStore.ResetCacheForTests();
                var doc = GeneralSettingsStore.Load();
                TestBase.AssertTrue(Math.Abs(doc.GameSettings.CombatSpeed - 1.5) < 1e-6, "round-trip combat speed", ref testsRun, ref testsPassed, ref testsFailed);
                TestBase.AssertTrue(doc.GameSettings.FastCombat, "round-trip fast combat", ref testsRun, ref testsPassed, ref testsFailed);
                TestBase.AssertTrue(Math.Abs(doc.AudioPreferences.MasterVolume - 0.42f) < 1e-4, "round-trip master volume", ref testsRun, ref testsPassed, ref testsFailed);
            }
            finally
            {
                try { Directory.Delete(root, true); } catch { }
            }
        }

        private static void TestMigrateFromLegacyGameSettingsPatch(ref int testsRun, ref int testsPassed, ref int testsFailed)
        {
            TestBase.SetCurrentTestName(nameof(TestMigrateFromLegacyGameSettingsPatch));
            string root = CreateTempRoot();
            try
            {
                PatchProfileServiceTestHooks.OverrideGameDataRoot(root);
                GeneralSettingsStore.ResetCacheForTests();

                string legacy = Path.Combine(root, "Patches", "GameSettings", "default.json");
                Directory.CreateDirectory(Path.GetDirectoryName(legacy)!);
                File.WriteAllText(legacy, "{\"CombatSpeed\":0.75,\"FastCombat\":true}");

                GeneralSettingsStore.EnsureBootstrapped();
                var doc = GeneralSettingsStore.Load();
                TestBase.AssertTrue(Math.Abs(doc.GameSettings.CombatSpeed - 0.75) < 1e-6, "migrates legacy game settings", ref testsRun, ref testsPassed, ref testsFailed);
                TestBase.AssertTrue(doc.GameSettings.FastCombat, "migrates legacy fast combat", ref testsRun, ref testsPassed, ref testsFailed);
                TestBase.AssertTrue(File.Exists(GeneralSettingsStore.GetFilePath()), "creates GeneralSettings.json", ref testsRun, ref testsPassed, ref testsFailed);
            }
            finally
            {
                try { Directory.Delete(root, true); } catch { }
            }
        }

        private static void TestMigrateFromLegacyAudioPatchBusFields(ref int testsRun, ref int testsPassed, ref int testsFailed)
        {
            TestBase.SetCurrentTestName(nameof(TestMigrateFromLegacyAudioPatchBusFields));
            string root = CreateTempRoot();
            try
            {
                PatchProfileServiceTestHooks.OverrideGameDataRoot(root);
                GeneralSettingsStore.ResetCacheForTests();

                string audioDir = Path.Combine(root, "Patches", "Audio");
                Directory.CreateDirectory(audioDir);
                File.WriteAllText(Path.Combine(audioDir, "default.json"),
                    "{\"masterVolume\":0.33,\"musicVolume\":0.44,\"sfxVolume\":0.55,\"cueMap\":{},\"stateMusicMap\":{}}");

                GeneralSettingsStore.EnsureBootstrapped();
                var doc = GeneralSettingsStore.Load();
                TestBase.AssertTrue(Math.Abs(doc.AudioPreferences.MasterVolume - 0.33f) < 1e-4, "migrates legacy master volume", ref testsRun, ref testsPassed, ref testsFailed);
                TestBase.AssertTrue(Math.Abs(doc.AudioPreferences.MusicVolume - 0.44f) < 1e-4, "migrates legacy music volume", ref testsRun, ref testsPassed, ref testsFailed);
            }
            finally
            {
                try { Directory.Delete(root, true); } catch { }
            }
        }

        private static void TestAudioConfigSplitSave(ref int testsRun, ref int testsPassed, ref int testsFailed)
        {
            TestBase.SetCurrentTestName(nameof(TestAudioConfigSplitSave));
            string root = CreateTempRoot();
            try
            {
                PatchProfileServiceTestHooks.OverrideGameDataRoot(root);
                GeneralSettingsStore.ResetCacheForTests();
                AudioConfig.InvalidatePatchPathCache();

                string audioDir = Path.Combine(root, "Patches", "Audio");
                Directory.CreateDirectory(audioDir);
                File.WriteAllText(Path.Combine(audioDir, "default.json"),
                    "{\"cueMap\":{\"Menu_Select\":{\"file\":\"SFX/old.wav\",\"volume\":1.0}},\"stateMusicMap\":{\"MainMenu\":\"Menu_Select\"}}");

                GeneralSettingsStore.EnsureBootstrapped();

                AudioConfig.ReloadFromFile();
                var cfg = AudioConfig.Instance;
                cfg.MasterVolume = 0.61f;
                cfg.CueMap["Menu_Select"].File = "SFX/new.wav";
                TestBase.AssertTrue(cfg.Save(), "split save succeeds", ref testsRun, ref testsPassed, ref testsFailed);

                string patchJson = File.ReadAllText(Path.Combine(audioDir, "default.json"));
                TestBase.AssertTrue(patchJson.Contains("SFX/new.wav"), "patch file has updated cue binding", ref testsRun, ref testsPassed, ref testsFailed);
                TestBase.AssertFalse(patchJson.Contains("masterVolume"), "patch file omits bus volume", ref testsRun, ref testsPassed, ref testsFailed);

                GeneralSettingsStore.ResetCacheForTests();
                var prefs = GeneralSettingsStore.Load().AudioPreferences;
                TestBase.AssertTrue(Math.Abs(prefs.MasterVolume - 0.61f) < 1e-4, "general settings has bus volume", ref testsRun, ref testsPassed, ref testsFailed);
            }
            finally
            {
                try { Directory.Delete(root, true); } catch { }
            }
        }

        private static string CreateTempRoot()
        {
            string dir = Path.Combine(Path.GetTempPath(), "df-general-settings-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(dir);
            return dir;
        }
    }
}
