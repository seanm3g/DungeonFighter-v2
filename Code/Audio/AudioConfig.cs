using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using RPGGame.Config;
using RPGGame.Utils;

namespace RPGGame.Audio
{
    /// <summary>
    /// Per-cue binding row in <see cref="AudioConfig.CueMap"/>.
    /// </summary>
    public sealed class AudioCueBinding
    {
        /// <summary>Path to the audio file, relative to <c>GameData/Audio/</c>. Empty string means "no sound bound" (silent).</summary>
        [JsonPropertyName("file")] public string File { get; set; } = string.Empty;
        /// <summary>Per-cue volume multiplier (0..1) applied on top of bus volume.</summary>
        [JsonPropertyName("volume")] public float Volume { get; set; } = 1.0f;
        /// <summary>If set, this cue cannot fire more often than this many milliseconds. Use for chatty cues like Combat_Hit.</summary>
        [JsonPropertyName("rateLimitMs")] public int? RateLimitMs { get; set; }
    }

    /// <summary>
    /// Mutable audio configuration persisted to <c>GameData/Audio/AudioConfig.json</c>.
    /// </summary>
    /// <remarks>
    /// The Audio settings tab edits this file. <see cref="AudioCueDispatcher"/> and
    /// <see cref="MusicController"/> read from it via the singleton <see cref="Instance"/>.
    /// </remarks>
    public sealed class AudioConfig
    {
        [JsonPropertyName("masterVolume")] public float MasterVolume { get; set; } = 1.0f;
        [JsonPropertyName("musicVolume")]  public float MusicVolume  { get; set; } = 0.7f;
        [JsonPropertyName("sfxVolume")]    public float SfxVolume    { get; set; } = 0.9f;

        [JsonPropertyName("masterEnabled")] public bool MasterEnabled { get; set; } = true;
        [JsonPropertyName("musicEnabled")] public bool MusicEnabled { get; set; } = true;
        [JsonPropertyName("sfxEnabled")]   public bool SfxEnabled   { get; set; } = true;

        /// <summary>Default crossfade when <c>musicCrossfadeMs</c> is omitted from JSON or the save UI has no value.</summary>
        public const int DefaultMusicCrossfadeMs = 1000;
        /// <summary>Upper bound aligned with the Audio settings numeric (hand-edited JSON is clamped here).</summary>
        public const int MaxMusicCrossfadeMs = 10000;

        [JsonPropertyName("musicCrossfadeMs")] public int MusicCrossfadeMs { get; set; } = DefaultMusicCrossfadeMs;

        /// <summary>
        /// When &gt; 0, music transitions (state music and triggered music cues) start the incoming track at the same
        /// phase within one beat of this tempo (seconds mod 60/BPM), so layered tracks with the same BPM stay aligned.
        /// </summary>
        [JsonPropertyName("musicTransitionSyncBpm")] public float MusicTransitionSyncBpm { get; set; } = 0f;

        /// <summary>
        /// When <see cref="MusicTransitionSyncBpm"/> is 0, if true the incoming track starts at the same elapsed seconds
        /// as the outgoing track (clamped to the new file length). When false, new music always starts at 0.
        /// Default true so layered scene music (stems) stays time-aligned without extra setup.
        /// </summary>
        [JsonPropertyName("musicTransitionCarryElapsed")] public bool MusicTransitionCarryElapsed { get; set; } = true;

        /// <summary>Cue name (matches <see cref="AudioCue"/> enum) → binding row.</summary>
        [JsonPropertyName("cueMap")] public Dictionary<string, AudioCueBinding> CueMap { get; set; } = new();

        /// <summary>GameState name → cue name. Used by <see cref="MusicController"/>.</summary>
        [JsonPropertyName("stateMusicMap")] public Dictionary<string, string> StateMusicMap { get; set; } = new();

        /// <summary>Cached singleton (matches <see cref="GameSettings.Instance"/> pattern).</summary>
        private static AudioConfig? _instance;
        private static readonly object _instanceLock = new();

        /// <summary>Returns the singleton instance, loading from disk on first access.</summary>
        public static AudioConfig Instance
        {
            get
            {
                if (_instance != null) return _instance;
                lock (_instanceLock)
                {
                    _instance ??= LoadFromFile();
                }
                return _instance;
            }
        }

        /// <summary>Reloads the singleton from disk (call when the settings UI opens).</summary>
        public static void ReloadFromFile()
        {
            lock (_instanceLock)
            {
                _instance = LoadFromFile();
            }
        }

        private static string? _cachedConfigFilePath;

        /// <summary>Clears cached patch path so the next load/save resolves the active audio patch.</summary>
        public static void InvalidatePatchPathCache()
        {
            lock (_instanceLock)
            {
                _cachedConfigFilePath = null;
            }
        }

        /// <summary>Resolves the active audio patch JSON path.</summary>
        public static string GetConfigFilePath()
        {
            if (_cachedConfigFilePath != null)
                return _cachedConfigFilePath;
            lock (_instanceLock)
            {
                _cachedConfigFilePath ??= PatchProfileService.GetActivePatchFilePath(PatchCategory.Audio);
                return _cachedConfigFilePath;
            }
        }

        /// <summary>
        /// Canonicalizes relative cue paths to forward slashes for cross-platform JSON and playback.
        /// Accepts legacy Windows <c>Music\track.wav</c> entries; <see cref="Path.Combine"/> resolves <c>/</c> on all OSes.
        /// </summary>
        internal static string NormalizeRelativeAssetPath(string relativeFile)
        {
            if (string.IsNullOrEmpty(relativeFile)) return string.Empty;
            if (Path.IsPathRooted(relativeFile)) return relativeFile;
            return relativeFile.Replace('\\', '/');
        }

        /// <summary>Resolves an absolute path for a cue's <see cref="AudioCueBinding.File"/> (always rooted under GameData/Audio/).</summary>
        public static string ResolveAssetPath(string relativeFile)
        {
            if (string.IsNullOrEmpty(relativeFile)) return string.Empty;
            // Already absolute? Use as-is.
            if (Path.IsPathRooted(relativeFile))
                return relativeFile;
            string? settingsDir = GameConstants.GetSettingsDirectory();
            string audioDir = settingsDir != null
                ? Path.Combine(settingsDir, "Audio")
                : Path.Combine(GameConstants.GameDataDirectory, "Audio");
            string normalized = NormalizeRelativeAssetPath(relativeFile);
            return Path.GetFullPath(Path.Combine(audioDir, normalized));
        }

        /// <summary>
        /// Older <c>AudioConfig.json</c> files omitted <see cref="MusicTransitionCarryElapsed"/>; JSON bool defaults to false
        /// on deserialize. Treat omitted key as "carry on" so scene music stays time-aligned.
        /// </summary>
        private static void ApplyLegacyMusicTransitionCarryDefault(string rawJson, AudioConfig cfg)
        {
            try
            {
                using var doc = JsonDocument.Parse(rawJson);
                if (doc.RootElement.ValueKind != JsonValueKind.Object) return;
                if (!doc.RootElement.TryGetProperty("musicTransitionCarryElapsed", out _))
                    cfg.MusicTransitionCarryElapsed = true;
            }
            catch { /* ignore; keep deserialized value */ }
        }

        private static AudioConfig LoadFromFile()
        {
            string path = GetConfigFilePath();
            try
            {
                if (File.Exists(path))
                {
                    string json = File.ReadAllText(path);
                    if (!string.IsNullOrWhiteSpace(json))
                    {
                        var cfg = JsonSerializer.Deserialize<AudioConfig>(json);
                        if (cfg != null)
                        {
                            ApplyLegacyMusicTransitionCarryDefault(json, cfg);
                            cfg.ValidateAndFix();
                            cfg.EnsureDefaultEntriesForAllCues();
                            return cfg;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                ErrorHandler.LogError(ex, "AudioConfig.LoadFromFile", "Could not load AudioConfig.json; using defaults");
            }
            var fallback = new AudioConfig();
            fallback.ValidateAndFix();
            fallback.EnsureDefaultEntriesForAllCues();
            return fallback;
        }

        /// <summary>Clamps volume values into 0..1 (and other invariants) so a corrupt file cannot crash playback.</summary>
        public void ValidateAndFix()
        {
            MasterVolume = Clamp01(MasterVolume);
            MusicVolume  = Clamp01(MusicVolume);
            SfxVolume    = Clamp01(SfxVolume);
            MusicCrossfadeMs = Math.Clamp(MusicCrossfadeMs, 0, MaxMusicCrossfadeMs);
            if (MusicTransitionSyncBpm < 0f) MusicTransitionSyncBpm = 0f;
            else if (MusicTransitionSyncBpm > 0f && MusicTransitionSyncBpm < 20f) MusicTransitionSyncBpm = 0f;
            else if (MusicTransitionSyncBpm > 400f) MusicTransitionSyncBpm = 400f;
            CueMap ??= new Dictionary<string, AudioCueBinding>();
            StateMusicMap ??= new Dictionary<string, string>();
            MigrateLegacyCombatCueNames();
            foreach (var binding in CueMap.Values)
            {
                binding.Volume = Clamp01(binding.Volume);
                if (binding.RateLimitMs is int ms && ms < 0) binding.RateLimitMs = 0;
                binding.File = NormalizeRelativeAssetPath(binding.File ?? string.Empty);
            }

            EnsureDefaultStateMusicMappings();
        }

        private void MigrateLegacyCombatCueNames()
        {
            const string legacyCritical = "Combat_Critical";
            string criticalHit = AudioCue.Combat_CriticalHit.ToString();
            if (CueMap.TryGetValue(legacyCritical, out var legacy)
                && (!CueMap.TryGetValue(criticalHit, out var current) || string.IsNullOrEmpty(current.File)))
            {
                CueMap[criticalHit] = legacy;
            }
            CueMap.Remove(legacyCritical);
        }

        /// <summary>
        /// Adds built-in state→music rows missing from older <c>AudioConfig.json</c> files. Does not overwrite existing keys.
        /// </summary>
        private void EnsureDefaultStateMusicMappings()
        {
            // GameLoop = in-game hub ("What would you like to do?"). It was omitted from early maps, so music resolved to None.
            string gameLoop = nameof(GameState.GameLoop);
            if (!StateMusicMap.ContainsKey(gameLoop))
                StateMusicMap[gameLoop] = AudioCue.Music_MainMenu.ToString();
        }

        /// <summary>Adds a stub binding for every <see cref="AudioCue"/> that is missing from <see cref="CueMap"/>; never overwrites existing entries.</summary>
        public void EnsureDefaultEntriesForAllCues()
        {
            foreach (AudioCue cue in Enum.GetValues(typeof(AudioCue)))
            {
                if (cue == AudioCue.None) continue;
                string key = cue.ToString();
                if (!CueMap.ContainsKey(key))
                    CueMap[key] = new AudioCueBinding();
            }
        }

        /// <summary>Persists the current config to <c>GameData/Audio/AudioConfig.json</c>.</summary>
        public bool Save()
        {
            ValidateAndFix();
            string path = GetConfigFilePath();
            try
            {
                string? dir = Path.GetDirectoryName(path);
                if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
                    Directory.CreateDirectory(dir);
                string json = JsonSerializer.Serialize(this, new JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText(path, json);
                return true;
            }
            catch (Exception ex)
            {
                ErrorHandler.LogError(ex, "AudioConfig.Save", "Could not save AudioConfig.json");
                return false;
            }
        }

        /// <summary>Returns the binding for a cue, or null if unbound.</summary>
        public AudioCueBinding? GetBinding(AudioCue cue)
        {
            if (cue == AudioCue.None) return null;
            return CueMap.TryGetValue(cue.ToString(), out var b) ? b : null;
        }

        /// <summary>
        /// Uses <see cref="MusicTransitionSyncBpm"/> / <see cref="MusicTransitionCarryElapsed"/> to derive a seek offset
        /// for the next music file from the outgoing track's playback time.
        /// </summary>
        public double ComputeMusicStartOffsetSecondsForTransition(double? outgoingTimeSeconds)
        {
            if (outgoingTimeSeconds is not double t || !double.IsFinite(t) || t < 0) return 0;
            if (MusicTransitionSyncBpm > 0f)
            {
                double beat = 60.0 / MusicTransitionSyncBpm;
                if (!double.IsFinite(beat) || beat <= 0) return 0;
                double m = t % beat;
                return m < 0 ? m + beat : m;
            }
            if (MusicTransitionCarryElapsed) return t;
            return 0;
        }

        /// <summary>Returns the music cue mapped to a game state, or <see cref="AudioCue.None"/> if no music is mapped.</summary>
        public AudioCue GetMusicCueForState(string stateName)
        {
            if (string.IsNullOrEmpty(stateName)) return AudioCue.None;
            if (!StateMusicMap.TryGetValue(stateName, out var cueName) || string.IsNullOrEmpty(cueName))
                return AudioCue.None;
            return Enum.TryParse<AudioCue>(cueName, out var cue) ? cue : AudioCue.None;
        }

        private static float Clamp01(float v) => v < 0f ? 0f : (v > 1f ? 1f : v);
    }
}
