using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
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

        [JsonPropertyName("musicEnabled")] public bool MusicEnabled { get; set; } = true;
        [JsonPropertyName("sfxEnabled")]   public bool SfxEnabled   { get; set; } = true;

        [JsonPropertyName("musicCrossfadeMs")] public int MusicCrossfadeMs { get; set; } = 200;

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

        /// <summary>Resolves the <c>GameData/Audio/AudioConfig.json</c> path using the same logic as other game data files.</summary>
        public static string GetConfigFilePath()
        {
            string? settingsDir = GameConstants.GetSettingsDirectory();
            string audioDir = settingsDir != null
                ? Path.Combine(settingsDir, "Audio")
                : Path.Combine(GameConstants.GameDataDirectory, "Audio");
            return Path.Combine(audioDir, "AudioConfig.json");
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
            return Path.GetFullPath(Path.Combine(audioDir, relativeFile));
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
            fallback.EnsureDefaultEntriesForAllCues();
            return fallback;
        }

        /// <summary>Clamps volume values into 0..1 (and other invariants) so a corrupt file cannot crash playback.</summary>
        public void ValidateAndFix()
        {
            MasterVolume = Clamp01(MasterVolume);
            MusicVolume  = Clamp01(MusicVolume);
            SfxVolume    = Clamp01(SfxVolume);
            MusicCrossfadeMs = Math.Max(0, MusicCrossfadeMs);
            CueMap ??= new Dictionary<string, AudioCueBinding>();
            StateMusicMap ??= new Dictionary<string, string>();
            foreach (var binding in CueMap.Values)
            {
                binding.Volume = Clamp01(binding.Volume);
                if (binding.RateLimitMs is int ms && ms < 0) binding.RateLimitMs = 0;
                binding.File ??= string.Empty;
            }
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
