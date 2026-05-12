using System;
using System.Collections.Generic;
using System.IO;
using RPGGame;
using RPGGame.Actions.Execution;
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

            TestCombatOutcomeCueOrder(ref run, ref passed, ref failed);
            TestCombatOutcomeCuesRouteToSfxBus(ref run, ref passed, ref failed);
            TestRoutesCombatHitToHitCue(ref run, ref passed, ref failed);
            TestPublisherRoutesCombatHitToHitCue(ref run, ref passed, ref failed);
            TestPublisherRoutesComboHitToComboCue(ref run, ref passed, ref failed);
            TestPublisherRoutesCriticalHitToCriticalHitCue(ref run, ref passed, ref failed);
            TestPublisherRoutesEnemyHitToHeroHurtCue(ref run, ref passed, ref failed);
            TestPublisherRoutesMissToMissCue(ref run, ref passed, ref failed);
            TestPublisherRoutesCriticalMissToCriticalMissCue(ref run, ref passed, ref failed);
            TestRoutesLowHealthEventsToDistinctCues(ref run, ref passed, ref failed);
            TestLowHealthThresholdPublishesOnlyWhenCrossing(ref run, ref passed, ref failed);
            TestRespectsGlobalMute(ref run, ref passed, ref failed);
            TestMasterDisableSilencesBothBuses(ref run, ref passed, ref failed);
            TestMusicBusDisableSilencesMusicOnly(ref run, ref passed, ref failed);
            TestSettingsPreviewIgnoresMutes(ref run, ref passed, ref failed);
            TestRespectsRateLimit(ref run, ref passed, ref failed);
            TestMissingFileDoesNotCrash(ref run, ref passed, ref failed);
            TestMusicControllerChangesTrackOnStateChange(ref run, ref passed, ref failed);
            TestMusicTransitionBeatSyncStartOffset(ref run, ref passed, ref failed);
            TestMusicFadeLoopRestartPolicy(ref run, ref passed, ref failed);
            TestMusicFadeIncomingTrackPolicy(ref run, ref passed, ref failed);
            TestSfxBusDisableSilencesSfxOnly(ref run, ref passed, ref failed);
            TestAudioConfigSerialization(ref run, ref passed, ref failed);
            TestAudioConfigMusicCrossfadeDefaultAndClamp(ref run, ref passed, ref failed);
            TestAudioConfigComputeMusicStartOffset(ref run, ref passed, ref failed);
            TestGameLoopStateMusicDefaultWhenMissing(ref run, ref passed, ref failed);

            TestBase.PrintSummary("AudioCueDispatcher Tests", run, passed, failed);
        }

        private static void TestCombatOutcomeCueOrder(ref int run, ref int passed, ref int failed)
        {
            var names = Enum.GetNames(typeof(AudioCue));
            int critMiss = Array.IndexOf(names, nameof(AudioCue.Combat_CriticalMiss));
            int miss = Array.IndexOf(names, nameof(AudioCue.Combat_Miss));
            int hit = Array.IndexOf(names, nameof(AudioCue.Combat_Hit));
            int combo = Array.IndexOf(names, nameof(AudioCue.Combat_ComboComplete));
            int critHit = Array.IndexOf(names, nameof(AudioCue.Combat_CriticalHit));

            TestBase.AssertTrue(critMiss < miss && miss < hit && hit < combo && combo < critHit,
                "combat outcome cues appear in order: crit miss, miss, hit, combo, crit hit",
                ref run, ref passed, ref failed);
        }

        private static void TestCombatOutcomeCuesRouteToSfxBus(ref int run, ref int passed, ref int failed)
        {
            var cues = new[]
            {
                AudioCue.Combat_CriticalMiss,
                AudioCue.Combat_Miss,
                AudioCue.Combat_Hit,
                AudioCue.Combat_ComboComplete,
                AudioCue.Combat_CriticalHit
            };

            bool allSfx = true;
            foreach (var cue in cues)
                allSfx &= cue.GetBus() == AudioBusKind.Sfx;

            TestBase.AssertTrue(allSfx, "combat outcome cues route to SFX bus, not music/crossfade", ref run, ref passed, ref failed);
        }

        private static AudioConfig CreateTestConfig(string cueFile = "stub.wav")
        {
            var cfg = new AudioConfig();
            cfg.EnsureDefaultEntriesForAllCues();
            cfg.CueMap[AudioCue.Combat_Hit.ToString()].File = cueFile;
            cfg.CueMap[AudioCue.Combat_Hit.ToString()].RateLimitMs = 80;
            cfg.CueMap[AudioCue.Combat_Miss.ToString()].File = cueFile;
            cfg.CueMap[AudioCue.Combat_CriticalMiss.ToString()].File = cueFile;
            cfg.CueMap[AudioCue.Combat_CriticalHit.ToString()].File = cueFile;
            cfg.CueMap[AudioCue.Combat_ComboComplete.ToString()].File = cueFile;
            cfg.CueMap[AudioCue.Combat_HeroHurt.ToString()].File = cueFile;
            cfg.CueMap[AudioCue.Combat_HeroLowHealth.ToString()].File = cueFile;
            cfg.CueMap[AudioCue.Combat_EnemyLowHealth.ToString()].File = cueFile;
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

        private static void TestPublisherRoutesCombatHitToHitCue(ref int run, ref int passed, ref int failed)
        {
            string stub = CreateStubFile();
            try
            {
                CombatEventBus.Reset();
                var engine = new NullAudioEngine { RecordCalls = true };
                var cfg = CreateTestConfig(stub);
                using var dispatcher = new AudioCueDispatcher(engine,
                    configResolver: () => cfg,
                    globalEnabledResolver: () => true);
                AudioCues.SetDispatcher(dispatcher);

                ActionEventPublisher.PublishActionHit(
                    new Character("Hero", 1),
                    new Enemy(name: "Goblin", level: 1, maxHealth: 100, strength: 8, agility: 6, technique: 4, intelligence: 4, armor: 0),
                    new RPGGame.Action(name: "Strike"),
                    rollValue: 12,
                    isCombo: false,
                    isCritical: false);

                TestBase.AssertEqual(1, engine.PlayCalls.Count, "Action hit publisher routes to Combat_Hit cue", ref run, ref passed, ref failed);
                if (engine.PlayCalls.Count > 0)
                    TestBase.AssertEqual(stub, engine.PlayCalls[0].file, "Action hit publisher plays the hit binding", ref run, ref passed, ref failed);
            }
            finally
            {
                AudioCues.SetDispatcher(null);
                CombatEventBus.Reset();
                TryDelete(stub);
            }
        }

        private static void TestPublisherRoutesComboHitToComboCue(ref int run, ref int passed, ref int failed)
        {
            string hitStub = CreateStubFile();
            string comboStub = CreateStubFile();
            try
            {
                CombatEventBus.Reset();
                var engine = new NullAudioEngine { RecordCalls = true };
                var cfg = CreateTestConfig(hitStub);
                cfg.CueMap[AudioCue.Combat_ComboComplete.ToString()].File = comboStub;
                using var dispatcher = new AudioCueDispatcher(engine,
                    configResolver: () => cfg,
                    globalEnabledResolver: () => true);
                AudioCues.SetDispatcher(dispatcher);

                ActionEventPublisher.PublishActionHit(
                    new Character("Hero", 1),
                    new Enemy(name: "Goblin", level: 1, maxHealth: 100, strength: 8, agility: 6, technique: 4, intelligence: 4, armor: 0),
                    new RPGGame.Action(name: "Combo Strike"),
                    rollValue: 12,
                    isCombo: true,
                    isCritical: false);

                TestBase.AssertEqual(1, engine.PlayCalls.Count, "Combo action hit publisher routes to one cue", ref run, ref passed, ref failed);
                if (engine.PlayCalls.Count > 0)
                    TestBase.AssertEqual(comboStub, engine.PlayCalls[0].file, "Combo action hit publisher plays the combo binding", ref run, ref passed, ref failed);
            }
            finally
            {
                AudioCues.SetDispatcher(null);
                CombatEventBus.Reset();
                TryDelete(hitStub);
                TryDelete(comboStub);
            }
        }

        private static void TestPublisherRoutesCriticalHitToCriticalHitCue(ref int run, ref int passed, ref int failed)
        {
            string hitStub = CreateStubFile();
            string critStub = CreateStubFile();
            try
            {
                CombatEventBus.Reset();
                var engine = new NullAudioEngine { RecordCalls = true };
                var cfg = CreateTestConfig(hitStub);
                cfg.CueMap[AudioCue.Combat_CriticalHit.ToString()].File = critStub;
                using var dispatcher = new AudioCueDispatcher(engine,
                    configResolver: () => cfg,
                    globalEnabledResolver: () => true);
                AudioCues.SetDispatcher(dispatcher);

                ActionEventPublisher.PublishActionHit(
                    new Character("Hero", 1),
                    new Enemy(name: "Goblin", level: 1, maxHealth: 100, strength: 8, agility: 6, technique: 4, intelligence: 4, armor: 0),
                    new RPGGame.Action(name: "Critical Strike"),
                    rollValue: 20,
                    isCombo: false,
                    isCritical: true);

                TestBase.AssertEqual(1, engine.PlayCalls.Count, "Critical action hit publisher routes to one cue", ref run, ref passed, ref failed);
                if (engine.PlayCalls.Count > 0)
                    TestBase.AssertEqual(critStub, engine.PlayCalls[0].file, "Critical action hit publisher plays the critical-hit binding", ref run, ref passed, ref failed);
            }
            finally
            {
                AudioCues.SetDispatcher(null);
                CombatEventBus.Reset();
                TryDelete(hitStub);
                TryDelete(critStub);
            }
        }

        private static void TestPublisherRoutesEnemyHitToHeroHurtCue(ref int run, ref int passed, ref int failed)
        {
            string hitStub = CreateStubFile();
            string hurtStub = CreateStubFile();
            try
            {
                CombatEventBus.Reset();
                var engine = new NullAudioEngine { RecordCalls = true };
                var cfg = CreateTestConfig(hitStub);
                cfg.CueMap[AudioCue.Combat_HeroHurt.ToString()].File = hurtStub;
                using var dispatcher = new AudioCueDispatcher(engine,
                    configResolver: () => cfg,
                    globalEnabledResolver: () => true);
                AudioCues.SetDispatcher(dispatcher);

                ActionEventPublisher.PublishActionHit(
                    new Enemy(name: "Goblin", level: 1, maxHealth: 100, strength: 8, agility: 6, technique: 4, intelligence: 4, armor: 0),
                    new Character("Hero", 1),
                    new RPGGame.Action(name: "Savage Combo"),
                    rollValue: 20,
                    isCombo: true,
                    isCritical: true);

                TestBase.AssertEqual(1, engine.PlayCalls.Count, "Enemy successful hit publisher routes to one hero-hurt cue", ref run, ref passed, ref failed);
                if (engine.PlayCalls.Count > 0)
                    TestBase.AssertEqual(hurtStub, engine.PlayCalls[0].file, "Enemy successful hit publisher plays the hero-hurt binding instead of hit/combo/crit", ref run, ref passed, ref failed);
            }
            finally
            {
                AudioCues.SetDispatcher(null);
                CombatEventBus.Reset();
                TryDelete(hitStub);
                TryDelete(hurtStub);
            }
        }

        private static void TestPublisherRoutesMissToMissCue(ref int run, ref int passed, ref int failed)
        {
            string stub = CreateStubFile();
            try
            {
                CombatEventBus.Reset();
                var engine = new NullAudioEngine { RecordCalls = true };
                var cfg = CreateTestConfig(stub);
                using var dispatcher = new AudioCueDispatcher(engine,
                    configResolver: () => cfg,
                    globalEnabledResolver: () => true);
                AudioCues.SetDispatcher(dispatcher);

                ActionEventPublisher.PublishActionMiss(
                    new Character("Hero", 1),
                    new Enemy(name: "Goblin", level: 1, maxHealth: 100, strength: 8, agility: 6, technique: 4, intelligence: 4, armor: 0),
                    new RPGGame.Action(name: "Swing"),
                    rollValue: 6,
                    isCriticalMiss: false);

                TestBase.AssertEqual(1, engine.PlayCalls.Count, "Miss publisher routes to one cue", ref run, ref passed, ref failed);
                if (engine.PlayCalls.Count > 0)
                    TestBase.AssertEqual(stub, engine.PlayCalls[0].file, "Miss publisher plays the miss binding", ref run, ref passed, ref failed);
            }
            finally
            {
                AudioCues.SetDispatcher(null);
                CombatEventBus.Reset();
                TryDelete(stub);
            }
        }

        private static void TestPublisherRoutesCriticalMissToCriticalMissCue(ref int run, ref int passed, ref int failed)
        {
            string missStub = CreateStubFile();
            string critMissStub = CreateStubFile();
            try
            {
                CombatEventBus.Reset();
                var engine = new NullAudioEngine { RecordCalls = true };
                var cfg = CreateTestConfig(missStub);
                cfg.CueMap[AudioCue.Combat_CriticalMiss.ToString()].File = critMissStub;
                using var dispatcher = new AudioCueDispatcher(engine,
                    configResolver: () => cfg,
                    globalEnabledResolver: () => true);
                AudioCues.SetDispatcher(dispatcher);

                ActionEventPublisher.PublishActionMiss(
                    new Character("Hero", 1),
                    new Enemy(name: "Goblin", level: 1, maxHealth: 100, strength: 8, agility: 6, technique: 4, intelligence: 4, armor: 0),
                    new RPGGame.Action(name: "Miss"),
                    rollValue: 1,
                    isCriticalMiss: true);

                TestBase.AssertEqual(1, engine.PlayCalls.Count, "Critical miss publisher routes to one cue", ref run, ref passed, ref failed);
                if (engine.PlayCalls.Count > 0)
                    TestBase.AssertEqual(critMissStub, engine.PlayCalls[0].file, "Critical miss publisher plays the critical-miss binding", ref run, ref passed, ref failed);
            }
            finally
            {
                AudioCues.SetDispatcher(null);
                CombatEventBus.Reset();
                TryDelete(missStub);
                TryDelete(critMissStub);
            }
        }

        private static void TestRoutesLowHealthEventsToDistinctCues(ref int run, ref int passed, ref int failed)
        {
            string stub = CreateStubFile();
            try
            {
                CombatEventBus.Reset();
                var engine = new NullAudioEngine { RecordCalls = true };
                var cfg = CreateTestConfig(stub);
                using var dispatcher = new AudioCueDispatcher(engine,
                    configResolver: () => cfg,
                    globalEnabledResolver: () => true);

                dispatcher.SubscribeToCombatEvents();

                CombatEventBus.Instance.Publish(new CombatEvent(CombatEventType.HeroLowHealth, new Character("Hero", 1))
                {
                    HealthPercentage = 0.20
                });
                CombatEventBus.Instance.Publish(new CombatEvent(CombatEventType.EnemyLowHealth,
                    new Enemy(name: "Goblin", level: 1, maxHealth: 100, strength: 8, agility: 6, technique: 4, intelligence: 4, armor: 0))
                {
                    HealthPercentage = 0.20
                });

                TestBase.AssertEqual(2, engine.PlayCalls.Count, "Hero and enemy low-health events each fire one SFX cue", ref run, ref passed, ref failed);
            }
            finally
            {
                CombatEventBus.Reset();
                TryDelete(stub);
            }
        }

        private static void TestLowHealthThresholdPublishesOnlyWhenCrossing(ref int run, ref int passed, ref int failed)
        {
            string stub = CreateStubFile();
            try
            {
                CombatEventBus.Reset();
                var engine = new NullAudioEngine { RecordCalls = true };
                var cfg = CreateTestConfig(stub);
                using var dispatcher = new AudioCueDispatcher(engine,
                    configResolver: () => cfg,
                    globalEnabledResolver: () => true);

                dispatcher.SubscribeToCombatEvents();

                var hero = new Character("Hero", 1) { MaxHealth = 100, CurrentHealth = 25 };
                double before = ActionEventPublisher.GetActorHealthPercentage(hero);
                hero.CurrentHealth = 20;
                ActionEventPublisher.PublishLowHealthThresholdIfCrossed(hero, before);

                TestBase.AssertEqual(1, engine.PlayCalls.Count, "Hero low-health cue fires when crossing to 20% HP", ref run, ref passed, ref failed);

                before = ActionEventPublisher.GetActorHealthPercentage(hero);
                hero.CurrentHealth = 10;
                ActionEventPublisher.PublishLowHealthThresholdIfCrossed(hero, before);

                TestBase.AssertEqual(1, engine.PlayCalls.Count, "Hero low-health cue does not repeat while already below threshold", ref run, ref passed, ref failed);

                var enemy = new Enemy(name: "Goblin", level: 1, maxHealth: 100, strength: 8, agility: 6, technique: 4, intelligence: 4, armor: 0)
                {
                    CurrentHealth = 25
                };
                before = ActionEventPublisher.GetActorHealthPercentage(enemy);
                enemy.CurrentHealth = 19;
                ActionEventPublisher.PublishLowHealthThresholdIfCrossed(enemy, before);

                TestBase.AssertEqual(2, engine.PlayCalls.Count, "Enemy low-health cue fires independently when enemy crosses threshold", ref run, ref passed, ref failed);
            }
            finally
            {
                CombatEventBus.Reset();
                TryDelete(stub);
            }
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

        private static void TestMasterDisableSilencesBothBuses(ref int run, ref int passed, ref int failed)
        {
            string stub = CreateStubFile();
            try
            {
                var engine = new NullAudioEngine { RecordCalls = true };
                var cfg = CreateTestConfig(stub);
                cfg.MasterEnabled = false;
                var dispatcher = new AudioCueDispatcher(engine,
                    configResolver: () => cfg,
                    globalEnabledResolver: () => true);

                dispatcher.Trigger(AudioCue.Combat_Hit);
                dispatcher.Trigger(AudioCue.Music_MainMenu);

                TestBase.AssertEqual(0, engine.PlayCalls.Count, "Master disabled -> SFX cue does not fire", ref run, ref passed, ref failed);
                TestBase.AssertEqual(0, engine.PlayMusicCalls.Count, "Master disabled -> Music cue does not fire", ref run, ref passed, ref failed);
            }
            finally { TryDelete(stub); }
        }

        private static void TestMusicBusDisableSilencesMusicOnly(ref int run, ref int passed, ref int failed)
        {
            string stub = CreateStubFile();
            try
            {
                var engine = new NullAudioEngine { RecordCalls = true };
                var cfg = CreateTestConfig(stub);
                cfg.MasterEnabled = true;
                cfg.MusicEnabled = false;
                cfg.SfxEnabled = true;
                var dispatcher = new AudioCueDispatcher(engine,
                    configResolver: () => cfg,
                    globalEnabledResolver: () => true);

                dispatcher.Trigger(AudioCue.Music_MainMenu);
                dispatcher.Trigger(AudioCue.Combat_Hit);

                TestBase.AssertEqual(0, engine.PlayMusicCalls.Count, "Music disabled -> Music cue does not fire", ref run, ref passed, ref failed);
                TestBase.AssertEqual(1, engine.PlayCalls.Count, "Music disabled -> SFX cue still fires", ref run, ref passed, ref failed);
            }
            finally { TryDelete(stub); }
        }

        /// <summary>Settings <c>Test</c> uses preview mode so bindings can be verified even when sound is muted.</summary>
        private static void TestSettingsPreviewIgnoresMutes(ref int run, ref int passed, ref int failed)
        {
            string stub = CreateStubFile();
            try
            {
                var engine = new NullAudioEngine { RecordCalls = true };
                var cfg = CreateTestConfig(stub);
                cfg.MusicEnabled = false;
                cfg.SfxEnabled = false;
                var dispatcher = new AudioCueDispatcher(engine,
                    configResolver: () => cfg,
                    globalEnabledResolver: () => false);

                dispatcher.Trigger(AudioCue.Combat_Hit, settingsPreview: true);
                TestBase.AssertEqual(1, engine.PlaySettingsPreviewCalls.Count, "settings preview: SFX cue uses audible preview path despite global + bus mutes", ref run, ref passed, ref failed);

                int previewBefore = engine.PlaySettingsPreviewCalls.Count;
                engine.PlayCalls.Clear();
                dispatcher.Trigger(AudioCue.Music_MainMenu, settingsPreview: true);
                TestBase.AssertEqual(previewBefore + 1, engine.PlaySettingsPreviewCalls.Count, "settings preview: music routes to PlaySettingsPreview", ref run, ref passed, ref failed);
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
                cfg.MusicCrossfadeMs = 1234;
                var controller = new MusicController(engine,
                    configResolver: () => cfg,
                    globalEnabledResolver: () => true);

                controller.OnStateChanged(GameState.MainMenu);
                int afterMainMenu = engine.PlayMusicCalls.Count;

                controller.OnStateChanged(GameState.Dungeon);
                int afterDungeon = engine.PlayMusicCalls.Count;

                TestBase.AssertEqual(1, afterMainMenu, "MainMenu transition triggers PlayMusic", ref run, ref passed, ref failed);
                TestBase.AssertEqual(2, afterDungeon, "Dungeon transition triggers a second PlayMusic", ref run, ref passed, ref failed);
                TestBase.AssertEqual(1234, engine.PlayMusicCalls[0].crossfadeMs, "First music cue from silence still uses configured crossfade length", ref run, ref passed, ref failed);
                TestBase.AssertEqualEnum(AudioCue.Music_Dungeon, controller.CurrentMusicCue, "Controller tracks active music cue", ref run, ref passed, ref failed);
            }
            finally { TryDelete(stub); }
        }

        private static void TestMusicTransitionBeatSyncStartOffset(ref int run, ref int passed, ref int failed)
        {
            string stub = CreateStubFile();
            try
            {
                var engine = new NullAudioEngine { RecordCalls = true, SimulatedMusicPlaybackTimeSeconds = 1.25 };
                var cfg = CreateTestConfig(stub);
                cfg.MusicTransitionSyncBpm = 60f;
                var controller = new MusicController(engine,
                    configResolver: () => cfg,
                    globalEnabledResolver: () => true);

                controller.OnStateChanged(GameState.MainMenu);
                engine.SimulatedMusicPlaybackTimeSeconds = 1.25;
                controller.OnStateChanged(GameState.Dungeon);

                TestBase.AssertEqual(2, engine.PlayMusicCalls.Count, "Beat sync test runs two transitions", ref run, ref passed, ref failed);
                double off = engine.PlayMusicCalls[1].startOffsetSeconds;
                TestBase.AssertTrue(Math.Abs(off - 0.25) < 1e-6, $"Second PlayMusic uses beat phase offset (~0.25), got {off}", ref run, ref passed, ref failed);
            }
            finally { TryDelete(stub); }
        }

        private static void TestMusicFadeLoopRestartPolicy(ref int run, ref int passed, ref int failed)
        {
            TestBase.AssertTrue(
                MusicFadeLoopPolicy.ShouldRestartEndedTrack(7, 7, cancellationRequested: false),
                "Outgoing fade track restarts when the fade generation is still current",
                ref run, ref passed, ref failed);

            TestBase.AssertTrue(
                !MusicFadeLoopPolicy.ShouldRestartEndedTrack(7, 8, cancellationRequested: false),
                "Outgoing fade track does not restart after a newer music transition supersedes it",
                ref run, ref passed, ref failed);

            TestBase.AssertTrue(
                !MusicFadeLoopPolicy.ShouldRestartEndedTrack(7, 7, cancellationRequested: true),
                "Outgoing fade track does not restart after fade cancellation",
                ref run, ref passed, ref failed);
        }

        private static void TestMusicFadeIncomingTrackPolicy(ref int run, ref int passed, ref int failed)
        {
            TestBase.AssertTrue(
                MusicFadeLoopPolicy.ShouldFadeIncomingTrack(1000, hasOutgoingTrack: false),
                "Incoming music fades in when crossfade is configured and no track is currently playing",
                ref run, ref passed, ref failed);

            TestBase.AssertTrue(
                !MusicFadeLoopPolicy.ShouldFadeIncomingTrack(1000, hasOutgoingTrack: true),
                "Incoming music uses the crossfade path when an outgoing track exists",
                ref run, ref passed, ref failed);

            TestBase.AssertTrue(
                !MusicFadeLoopPolicy.ShouldFadeIncomingTrack(0, hasOutgoingTrack: false),
                "Incoming music starts immediately when crossfade duration is zero",
                ref run, ref passed, ref failed);
        }

        /// <summary>GameLoop is the in-game hub; older configs omitted it from stateMusicMap.</summary>
        private static void TestGameLoopStateMusicDefaultWhenMissing(ref int run, ref int passed, ref int failed)
        {
            var cfg = new AudioConfig { StateMusicMap = new Dictionary<string, string>() };
            cfg.ValidateAndFix();
            TestBase.AssertEqualEnum(AudioCue.Music_MainMenu, cfg.GetMusicCueForState(nameof(GameState.GameLoop)),
                "ValidateAndFix adds GameLoop → Music_MainMenu when absent", ref run, ref passed, ref failed);

            cfg.StateMusicMap[nameof(GameState.GameLoop)] = AudioCue.Music_Combat.ToString();
            cfg.ValidateAndFix();
            TestBase.AssertEqualEnum(AudioCue.Music_Combat, cfg.GetMusicCueForState(nameof(GameState.GameLoop)),
                "ValidateAndFix does not replace an existing GameLoop mapping", ref run, ref passed, ref failed);
        }

        private static void TestAudioConfigComputeMusicStartOffset(ref int run, ref int passed, ref int failed)
        {
            var cfg = new AudioConfig { MusicTransitionSyncBpm = 120f };
            double beat = 60.0 / 120.0;
            double o = cfg.ComputeMusicStartOffsetSecondsForTransition(2.5);
            TestBase.AssertTrue(Math.Abs(o - (2.5 % beat)) < 1e-9, "BPM sync uses modulo of beat length", ref run, ref passed, ref failed);

            cfg.MusicTransitionSyncBpm = 0f;
            cfg.MusicTransitionCarryElapsed = true;
            TestBase.AssertTrue(Math.Abs(cfg.ComputeMusicStartOffsetSecondsForTransition(12.3) - 12.3) < 1e-9, "Carry elapsed passes wall time", ref run, ref passed, ref failed);

            cfg.MusicTransitionCarryElapsed = false;
            double z = cfg.ComputeMusicStartOffsetSecondsForTransition(99.0);
            TestBase.AssertTrue(Math.Abs(z) < 1e-9, "Default is start at 0", ref run, ref passed, ref failed);
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
                MasterEnabled = false,
                MusicEnabled = false,
                SfxEnabled = true,
                MusicCrossfadeMs = 500,
                MusicTransitionSyncBpm = 140f,
                MusicTransitionCarryElapsed = true
            };
            cfg.EnsureDefaultEntriesForAllCues();
            cfg.CueMap[AudioCue.Menu_Select.ToString()].File = "SFX/menu_select.wav";
            cfg.CueMap[AudioCue.Menu_Select.ToString()].Volume = 0.75f;
            cfg.StateMusicMap["MainMenu"] = "Music_MainMenu";

            string json = System.Text.Json.JsonSerializer.Serialize(cfg);
            var round = System.Text.Json.JsonSerializer.Deserialize<AudioConfig>(json)!;

            TestBase.AssertTrue(Math.Abs(round.MasterVolume - 0.5f) < 1e-4, "AudioConfig round-trip preserves master volume", ref run, ref passed, ref failed);
            TestBase.AssertTrue(round.MasterEnabled == false, "AudioConfig round-trip preserves masterEnabled flag", ref run, ref passed, ref failed);
            TestBase.AssertTrue(round.MusicEnabled == false, "AudioConfig round-trip preserves musicEnabled flag", ref run, ref passed, ref failed);
            TestBase.AssertTrue(round.CueMap.ContainsKey("Menu_Select"), "AudioConfig round-trip preserves cueMap entries", ref run, ref passed, ref failed);
            TestBase.AssertTrue(round.CueMap["Menu_Select"].File == "SFX/menu_select.wav", "AudioConfig round-trip preserves cue file path", ref run, ref passed, ref failed);
            TestBase.AssertTrue(round.StateMusicMap.TryGetValue("MainMenu", out var s) && s == "Music_MainMenu", "AudioConfig round-trip preserves stateMusicMap", ref run, ref passed, ref failed);
            TestBase.AssertTrue(Math.Abs(round.MusicTransitionSyncBpm - 140f) < 1e-4, "AudioConfig round-trip preserves musicTransitionSyncBpm", ref run, ref passed, ref failed);
            TestBase.AssertTrue(round.MusicTransitionCarryElapsed, "AudioConfig round-trip preserves musicTransitionCarryElapsed", ref run, ref passed, ref failed);
            TestBase.AssertTrue(round.MusicCrossfadeMs == 500, "AudioConfig round-trip preserves musicCrossfadeMs", ref run, ref passed, ref failed);
        }

        private static void TestAudioConfigMusicCrossfadeDefaultAndClamp(ref int run, ref int passed, ref int failed)
        {
            Console.WriteLine("--- AudioConfig music crossfade default and ValidateAndFix clamp ---");
            var fresh = new AudioConfig();
            fresh.ValidateAndFix();
            TestBase.AssertTrue(fresh.MusicCrossfadeMs == AudioConfig.DefaultMusicCrossfadeMs, "default MusicCrossfadeMs matches constant", ref run, ref passed, ref failed);

            var huge = new AudioConfig { MusicCrossfadeMs = 9_000_000 };
            huge.ValidateAndFix();
            TestBase.AssertTrue(huge.MusicCrossfadeMs == AudioConfig.MaxMusicCrossfadeMs, "oversized crossfade clamped to max", ref run, ref passed, ref failed);

            var negative = new AudioConfig { MusicCrossfadeMs = -10 };
            negative.ValidateAndFix();
            TestBase.AssertTrue(negative.MusicCrossfadeMs == 0, "negative crossfade clamped to 0", ref run, ref passed, ref failed);
        }

        private static void TryDelete(string path)
        {
            try { if (File.Exists(path)) File.Delete(path); }
            catch { /* test cleanup; ignore */ }
        }
    }
}
