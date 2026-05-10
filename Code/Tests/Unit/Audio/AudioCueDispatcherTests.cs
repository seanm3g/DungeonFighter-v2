using System;
using System.IO;
using RPGGame;
using RPGGame.Audio;
using RPGGame.Combat.Events;

namespace RPGGame.Tests.Unit.Audio
{
    /// <summary>
    /// Tests for <see cref="AudioCueDispatcher"/> and <see cref="MusicController"/>.
    /// All tests use <see cref="NullAudioEngine"/> so no real audio runs in CI.
    /// </summary>
    public static class AudioCueDispatcherTests
    {
        public static void RunAllTests()
        {
            Console.WriteLine("=== AudioCueDispatcher Tests ===\n");

            int run = 0, passed = 0, failed = 0;

            TestRoutesCombatHitToHitCue(ref run, ref passed, ref failed);
            TestRespectsGlobalMute(ref run, ref passed, ref failed);
            TestRespectsRateLimit(ref run, ref passed, ref failed);
            TestMissingFileDoesNotCrash(ref run, ref passed, ref failed);
            TestMusicControllerChangesTrackOnStateChange(ref run, ref passed, ref failed);
            TestSfxBusDisableSilencesSfxOnly(ref run, ref passed, ref failed);
            TestAudioConfigSerialization(ref run, ref passed, ref failed);

            TestBase.PrintSummary("AudioCueDispatcher Tests", run, passed, failed);
        }

        private static AudioConfig CreateTestConfig(string cueFile = "stub.wav")
        {
            var cfg = new AudioConfig();
            cfg.EnsureDefaultEntriesForAllCues();
            cfg.CueMap[AudioCue.Combat_Hit.ToString()].File = cueFile;
            cfg.CueMap[AudioCue.Combat_Hit.ToString()].RateLimitMs = 80;
            cfg.CueMap[AudioCue.Combat_Miss.ToString()].File = cueFile;
            cfg.CueMap[AudioCue.Music_MainMenu.ToString()].File = cueFile;
            cfg.CueMap[AudioCue.Music_Dungeon.ToString()].File = cueFile;
            cfg.StateMusicMap["MainMenu"] = "Music_MainMenu";
            cfg.StateMusicMap["Dungeon"] = "Music_Dungeon";
            return cfg;
        }

        /// <summary>Creates a temp file so dispatcher's File.Exists check passes and Play() gets called.</summary>
        private static string CreateStubFile()
        {
            string path = Path.Combine(Path.GetTempPath(), $"audio_test_{Guid.NewGuid():N}.wav");
            File.WriteAllBytes(path, new byte[] { 0x52, 0x49, 0x46, 0x46 }); // "RIFF" header bytes (content doesn't matter)
            return path;
        }

        private static void TestRoutesCombatHitToHitCue(ref int run, ref int passed, ref int failed)
        {
            string stub = CreateStubFile();
            try
            {
                var engine = new NullAudioEngine { RecordCalls = true };
                var cfg = CreateTestConfig(stub);
                var dispatcher = new AudioCueDispatcher(engine,
                    configResolver: () => cfg,
                    globalEnabledResolver: () => true);

                dispatcher.Trigger(AudioCue.Combat_Hit);

                TestBase.AssertEqual(1, engine.PlayCalls.Count, "Combat_Hit fires one Play call", ref run, ref passed, ref failed);
                if (engine.PlayCalls.Count > 0)
                {
                    TestBase.AssertEqualEnum(AudioBusKind.Sfx, engine.PlayCalls[0].bus, "Combat_Hit routes to SFX bus", ref run, ref passed, ref failed);
                }
            }
            finally { TryDelete(stub); }
        }

        private static void TestRespectsGlobalMute(ref int run, ref int passed, ref int failed)
        {
            string stub = CreateStubFile();
            try
            {
                var engine = new NullAudioEngine { RecordCalls = true };
                var cfg = CreateTestConfig(stub);
                var dispatcher = new AudioCueDispatcher(engine,
                    configResolver: () => cfg,
                    globalEnabledResolver: () => false);

                dispatcher.Trigger(AudioCue.Combat_Hit);
                dispatcher.Trigger(AudioCue.Combat_Miss);

                TestBase.AssertEqual(0, engine.PlayCalls.Count, "Global mute short-circuits all Play calls", ref run, ref passed, ref failed);
            }
            finally { TryDelete(stub); }
        }

        private static void TestRespectsRateLimit(ref int run, ref int passed, ref int failed)
        {
            string stub = CreateStubFile();
            try
            {
                var engine = new NullAudioEngine { RecordCalls = true };
                var cfg = CreateTestConfig(stub);
                var dispatcher = new AudioCueDispatcher(engine,
                    configResolver: () => cfg,
                    globalEnabledResolver: () => true);

                // Use deterministic clock so the test isn't timing-dependent.
                long fakeTicks = 1000;
                typeof(AudioCueDispatcher)
                    .GetField("TicksProviderForTests", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)
                    ?.SetValue(dispatcher, new Func<long>(() => fakeTicks));

                for (int i = 0; i < 20; i++) dispatcher.Trigger(AudioCue.Combat_Hit);

                // RateLimitMs is 80ms; only the first call passes within the same tick.
                TestBase.AssertEqual(1, engine.PlayCalls.Count, "20 rapid Combat_Hit calls collapse to 1 within rate-limit window", ref run, ref passed, ref failed);

                // Advance the clock past the rate limit; the next call should pass.
                fakeTicks += 200;
                dispatcher.Trigger(AudioCue.Combat_Hit);
                TestBase.AssertEqual(2, engine.PlayCalls.Count, "Cue fires again after rate-limit window elapses", ref run, ref passed, ref failed);
            }
            finally { TryDelete(stub); }
        }

        private static void TestMissingFileDoesNotCrash(ref int run, ref int passed, ref int failed)
        {
            var engine = new NullAudioEngine { RecordCalls = true };
            var cfg = CreateTestConfig("/nonexistent/path/missing.wav");

            var dispatcher = new AudioCueDispatcher(engine,
                configResolver: () => cfg,
                globalEnabledResolver: () => true);

            try
            {
                dispatcher.Trigger(AudioCue.Combat_Hit);
                TestBase.AssertEqual(0, engine.PlayCalls.Count, "Missing file → Play not called (no throw)", ref run, ref passed, ref failed);
            }
            catch (Exception ex)
            {
                TestBase.AssertTrue(false, $"Missing file threw: {ex.Message}", ref run, ref passed, ref failed);
            }
        }

        private static void TestMusicControllerChangesTrackOnStateChange(ref int run, ref int passed, ref int failed)
        {
            string stub = CreateStubFile();
            try
            {
                var engine = new NullAudioEngine { RecordCalls = true };
                var cfg = CreateTestConfig(stub);
                var controller = new MusicController(engine,
                    configResolver: () => cfg,
                    globalEnabledResolver: () => true);

                controller.OnStateChanged(GameState.MainMenu);
                int afterMainMenu = engine.PlayMusicCalls.Count;

                controller.OnStateChanged(GameState.Dungeon);
                int afterDungeon = engine.PlayMusicCalls.Count;

                TestBase.AssertEqual(1, afterMainMenu, "MainMenu transition triggers PlayMusic", ref run, ref passed, ref failed);
                TestBase.AssertEqual(2, afterDungeon, "Dungeon transition triggers a second PlayMusic", ref run, ref passed, ref failed);
                TestBase.AssertEqualEnum(AudioCue.Music_Dungeon, controller.CurrentMusicCue, "Controller tracks active music cue", ref run, ref passed, ref failed);
            }
            finally { TryDelete(stub); }
        }

        private static void TestSfxBusDisableSilencesSfxOnly(ref int run, ref int passed, ref int failed)
        {
            string stub = CreateStubFile();
            try
            {
                var engine = new NullAudioEngine { RecordCalls = true };
                var cfg = CreateTestConfig(stub);
                cfg.SfxEnabled = false;
                cfg.MusicEnabled = true;
                var dispatcher = new AudioCueDispatcher(engine,
                    configResolver: () => cfg,
                    globalEnabledResolver: () => true);

                dispatcher.Trigger(AudioCue.Combat_Hit);
                TestBase.AssertEqual(0, engine.PlayCalls.Count, "SFX disabled → SFX cue does not fire", ref run, ref passed, ref failed);

                dispatcher.Trigger(AudioCue.Music_MainMenu);
                TestBase.AssertEqual(1, engine.PlayMusicCalls.Count, "SFX disabled → Music cue still fires", ref run, ref passed, ref failed);
            }
            finally { TryDelete(stub); }
        }

        private static void TestAudioConfigSerialization(ref int run, ref int passed, ref int failed)
        {
            // Round-trip an AudioConfig through JSON to make sure new entries serialize cleanly.
            var cfg = new AudioConfig
            {
                MasterVolume = 0.5f,
                MusicVolume = 0.4f,
                SfxVolume = 0.6f,
                MusicEnabled = false,
                SfxEnabled = true,
                MusicCrossfadeMs = 500
            };
            cfg.EnsureDefaultEntriesForAllCues();
            cfg.CueMap[AudioCue.Menu_Select.ToString()].File = "SFX/menu_select.wav";
            cfg.CueMap[AudioCue.Menu_Select.ToString()].Volume = 0.75f;
            cfg.StateMusicMap["MainMenu"] = "Music_MainMenu";

            string json = System.Text.Json.JsonSerializer.Serialize(cfg);
            var round = System.Text.Json.JsonSerializer.Deserialize<AudioConfig>(json)!;

            TestBase.AssertTrue(Math.Abs(round.MasterVolume - 0.5f) < 1e-4, "AudioConfig round-trip preserves master volume", ref run, ref passed, ref failed);
            TestBase.AssertTrue(round.MusicEnabled == false, "AudioConfig round-trip preserves musicEnabled flag", ref run, ref passed, ref failed);
            TestBase.AssertTrue(round.CueMap.ContainsKey("Menu_Select"), "AudioConfig round-trip preserves cueMap entries", ref run, ref passed, ref failed);
            TestBase.AssertTrue(round.CueMap["Menu_Select"].File == "SFX/menu_select.wav", "AudioConfig round-trip preserves cue file path", ref run, ref passed, ref failed);
            TestBase.AssertTrue(round.StateMusicMap.TryGetValue("MainMenu", out var s) && s == "Music_MainMenu", "AudioConfig round-trip preserves stateMusicMap", ref run, ref passed, ref failed);
        }

        private static void TryDelete(string path)
        {
            try { if (File.Exists(path)) File.Delete(path); }
            catch { /* test cleanup; ignore */ }
        }
    }
}
