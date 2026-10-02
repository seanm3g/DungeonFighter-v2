using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using RPGGame.Audio;

namespace RPGGame.Config
{
    /// <summary>
    /// Player-local general settings: game preferences, audio bus volume/mute/crossfade,
    /// and UI font/zoom preferences.
    /// Persisted to gitignored <c>GameData/GeneralSettings.json</c>.
    /// </summary>
    public static class GeneralSettingsStore
    {
        public const string FileName = "GeneralSettings.json";

        private static readonly object StoreLock = new();
        private static GeneralSettingsDocument? _cached;

        private static readonly JsonSerializerOptions JsonOptions = new()
        {
            WriteIndented = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            PropertyNameCaseInsensitive = true
        };

        public static string GetFilePath() =>
            Path.Combine(PatchProfileService.GetGameDataRoot(), FileName);

        public static GeneralSettingsDocument Load()
        {
            lock (StoreLock)
            {
                if (_cached != null)
                    return _cached;

                EnsureBootstrapped();
                string path = GetFilePath();
                if (File.Exists(path))
                {
                    try
                    {
                        string json = File.ReadAllText(path);
                        var doc = JsonSerializer.Deserialize<GeneralSettingsDocument>(json, JsonOptions);
                        if (doc != null)
                        {
                            NormalizeDocument(doc);
                            _cached = doc;
                            return _cached;
                        }
                    }
                    catch (Exception ex)
                    {
                        ErrorHandler.LogError(ex, "GeneralSettingsStore.Load", "Could not load GeneralSettings.json; using defaults");
                    }
                }

                _cached = CreateDefaultDocument();
                return _cached;
            }
        }

        public static void Save(GameSettings gameSettings, AudioPreferences audioPreferences)
        {
            gameSettings.ValidateAndFix();
            audioPreferences.ValidateAndFix();

            // Preserve UI font/zoom prefs from cache or disk (do not wipe on game/audio saves).
            var fonts = Load().UiFontPreferences ?? new UiFontPreferences();
            fonts.ValidateAndFix();

            var doc = new GeneralSettingsDocument
            {
                GameSettings = gameSettings,
                AudioPreferences = audioPreferences,
                UiFontPreferences = fonts
            };
            SaveDocument(doc);
        }

        /// <summary>Persists UI font preset + per-font zoom without touching game/audio settings.</summary>
        public static void SaveUiFontPreferences(UiFontPreferences fonts)
        {
            fonts.ValidateAndFix();
            var doc = Load();
            doc.UiFontPreferences = fonts;
            SaveDocument(doc);
        }

        public static void SaveDocument(GeneralSettingsDocument doc)
        {
            NormalizeDocument(doc);

            lock (StoreLock)
            {
                _cached = doc;
                WriteAtomic(GetFilePath(), JsonSerializer.Serialize(doc, JsonOptions));
            }
        }

        private static void NormalizeDocument(GeneralSettingsDocument doc)
        {
            doc.GameSettings ??= new GameSettings();
            doc.AudioPreferences ??= new AudioPreferences();
            doc.UiFontPreferences ??= new UiFontPreferences();
            doc.GameSettings.ValidateAndFix();
            doc.AudioPreferences.ValidateAndFix();
            doc.UiFontPreferences.ValidateAndFix();
        }

        public static void InvalidateCache()
        {
            lock (StoreLock)
            {
                _cached = null;
            }
        }

        public static void EnsureBootstrapped()
        {
            lock (StoreLock)
            {
                string path = GetFilePath();
                if (File.Exists(path))
                    return;

                var doc = MigrateFromLegacySources() ?? CreateDefaultDocument();
                WriteAtomic(path, JsonSerializer.Serialize(doc, JsonOptions));
                _cached = doc;
            }
        }

        /// <summary>Copies bus-level fields from an <see cref="AudioConfig"/> into preferences.</summary>
        public static AudioPreferences ExtractAudioPreferences(AudioConfig config)
        {
            return new AudioPreferences
            {
                MasterVolume = config.MasterVolume,
                MusicVolume = config.MusicVolume,
                SfxVolume = config.SfxVolume,
                MasterEnabled = config.MasterEnabled,
                MusicEnabled = config.MusicEnabled,
                SfxEnabled = config.SfxEnabled,
                MusicCrossfadeMs = config.MusicCrossfadeMs,
                MusicTransitionSyncBpm = config.MusicTransitionSyncBpm,
                MusicTransitionCarryElapsed = config.MusicTransitionCarryElapsed
            };
        }

        /// <summary>Applies stored preferences onto an in-memory <see cref="AudioConfig"/>.</summary>
        public static void ApplyAudioPreferences(AudioConfig config, AudioPreferences prefs)
        {
            config.MasterVolume = prefs.MasterVolume;
            config.MusicVolume = prefs.MusicVolume;
            config.SfxVolume = prefs.SfxVolume;
            config.MasterEnabled = prefs.MasterEnabled;
            config.MusicEnabled = prefs.MusicEnabled;
            config.SfxEnabled = prefs.SfxEnabled;
            config.MusicCrossfadeMs = prefs.MusicCrossfadeMs;
            config.MusicTransitionSyncBpm = prefs.MusicTransitionSyncBpm;
            config.MusicTransitionCarryElapsed = prefs.MusicTransitionCarryElapsed;
        }

        private static GeneralSettingsDocument? MigrateFromLegacySources()
        {
            GameSettings? gameSettings = TryLoadLegacyGameSettings();
            AudioPreferences? audioPrefs = TryLoadLegacyAudioPreferences();

            if (gameSettings == null && audioPrefs == null)
                return null;

            return new GeneralSettingsDocument
            {
                GameSettings = gameSettings ?? new GameSettings(),
                AudioPreferences = audioPrefs ?? new AudioPreferences(),
                UiFontPreferences = new UiFontPreferences()
            };
        }

        private static GameSettings? TryLoadLegacyGameSettings()
        {
            string legacyPatch = Path.Combine(
                PatchProfileService.GetCategoryFolder(PatchCategory.GameSettings),
                PatchProfile.DefaultPatchName + ".json");
            if (!File.Exists(legacyPatch))
                return null;

            try
            {
                string json = File.ReadAllText(legacyPatch);
                var settings = JsonSerializer.Deserialize<GameSettings>(json);
                settings?.ValidateAndFix();
                return settings;
            }
            catch
            {
                return null;
            }
        }

        private static AudioPreferences? TryLoadLegacyAudioPreferences()
        {
            try
            {
                string root = PatchProfileService.GetGameDataRoot();
                string patchName = TryReadActiveAudioPatchName(root) ?? PatchProfile.DefaultPatchName;
                string audioPath = Path.Combine(root, "Patches", "Audio", patchName + ".json");
                if (!File.Exists(audioPath))
                {
                    audioPath = Path.Combine(root, "Patches", "Audio", PatchProfile.DefaultPatchName + ".json");
                    if (!File.Exists(audioPath))
                        return null;
                }

                string json = File.ReadAllText(audioPath);
                var cfg = JsonSerializer.Deserialize<AudioConfig>(json);
                if (cfg == null)
                    return null;

                ApplyLegacyMusicTransitionCarryDefault(json, cfg);
                return ExtractAudioPreferences(cfg);
            }
            catch
            {
                return null;
            }
        }

        private static string? TryReadActiveAudioPatchName(string gameDataRoot)
        {
            string profilePath = Path.Combine(gameDataRoot, PatchProfileService.ProfileFileName);
            if (!File.Exists(profilePath))
                return null;

            try
            {
                using var doc = JsonDocument.Parse(File.ReadAllText(profilePath));
                if (doc.RootElement.TryGetProperty("activeAudioPatch", out var prop)
                    && prop.ValueKind == JsonValueKind.String)
                {
                    string? name = prop.GetString();
                    return string.IsNullOrWhiteSpace(name) ? null : name.Trim();
                }
            }
            catch { /* ignore */ }

            return null;
        }

        private static void ApplyLegacyMusicTransitionCarryDefault(string rawJson, AudioConfig cfg)
        {
            try
            {
                using var doc = JsonDocument.Parse(rawJson);
                if (doc.RootElement.ValueKind != JsonValueKind.Object) return;
                if (!doc.RootElement.TryGetProperty("musicTransitionCarryElapsed", out _))
                    cfg.MusicTransitionCarryElapsed = true;
            }
            catch { /* ignore */ }
        }

        private static GeneralSettingsDocument CreateDefaultDocument()
        {
            var gameSettings = TryLoadGameSettingsTemplate() ?? new GameSettings();
            gameSettings.ValidateAndFix();
            var fonts = new UiFontPreferences();
            fonts.ValidateAndFix();
            return new GeneralSettingsDocument
            {
                GameSettings = gameSettings,
                AudioPreferences = new AudioPreferences(),
                UiFontPreferences = fonts
            };
        }

        private static GameSettings? TryLoadGameSettingsTemplate()
        {
            string templatePath = Path.Combine(
                PatchProfileService.GetCategoryFolder(PatchCategory.GameSettings),
                PatchProfile.DefaultPatchName + ".template.json");
            if (!File.Exists(templatePath))
                return null;

            try
            {
                string json = File.ReadAllText(templatePath);
                var settings = JsonSerializer.Deserialize<GameSettings>(json);
                settings?.ValidateAndFix();
                return settings;
            }
            catch
            {
                return null;
            }
        }

        private static void WriteAtomic(string path, string json)
        {
            string? dir = Path.GetDirectoryName(path);
            if (!string.IsNullOrEmpty(dir))
                Directory.CreateDirectory(dir);

            string temp = path + ".tmp";
            File.WriteAllText(temp, json);
            if (File.Exists(path))
                File.Replace(temp, path, path + ".bak");
            else
                File.Move(temp, path);
        }

        /// <summary>Test hook: reset cached document.</summary>
        internal static void ResetCacheForTests()
        {
            lock (StoreLock)
            {
                _cached = null;
            }
        }
    }

    public sealed class GeneralSettingsDocument
    {
        [JsonPropertyName("gameSettings")]
        public GameSettings GameSettings { get; set; } = new();

        [JsonPropertyName("audioPreferences")]
        public AudioPreferences AudioPreferences { get; set; } = new();

        [JsonPropertyName("uiFontPreferences")]
        public UiFontPreferences UiFontPreferences { get; set; } = new();
    }

    /// <summary>
    /// Active ASCII canvas font and per-preset UI zoom multipliers (Ctrl+/-).
    /// </summary>
    public sealed class UiFontPreferences
    {
        public const double DefaultZoom = 1.0;
        public const double MinZoom = 0.5;
        public const double MaxZoom = 2.0;

        [JsonPropertyName("activePreset")]
        public string ActivePreset { get; set; } = "Vt323";

        [JsonPropertyName("zoomByPreset")]
        public Dictionary<string, double> ZoomByPreset { get; set; } =
            new(StringComparer.OrdinalIgnoreCase);

        public void ValidateAndFix()
        {
            if (string.IsNullOrWhiteSpace(ActivePreset))
                ActivePreset = "Vt323";

            ZoomByPreset ??= new Dictionary<string, double>(StringComparer.OrdinalIgnoreCase);

            // Normalize known preset keys; keep any extras clamped.
            string[] known =
            {
                "Vt323",
                "SueEllenFrancisco",
                "Bytesized",
                "CourierNew"
            };

            var normalized = new Dictionary<string, double>(StringComparer.OrdinalIgnoreCase);
            foreach (string key in known)
            {
                double zoom = DefaultZoom;
                if (ZoomByPreset.TryGetValue(key, out double stored))
                    zoom = stored;
                normalized[key] = ClampZoom(zoom);
            }

            foreach (var pair in ZoomByPreset)
            {
                if (normalized.ContainsKey(pair.Key))
                    continue;
                normalized[pair.Key] = ClampZoom(pair.Value);
            }

            ZoomByPreset = normalized;

            bool knownActive = false;
            foreach (string key in known)
            {
                if (string.Equals(key, ActivePreset, StringComparison.OrdinalIgnoreCase))
                {
                    ActivePreset = key;
                    knownActive = true;
                    break;
                }
            }

            if (!knownActive)
                ActivePreset = "Vt323";
        }

        public double GetZoom(string presetName)
        {
            if (string.IsNullOrWhiteSpace(presetName))
                return DefaultZoom;
            return ZoomByPreset.TryGetValue(presetName, out double zoom)
                ? ClampZoom(zoom)
                : DefaultZoom;
        }

        public void SetZoom(string presetName, double zoom)
        {
            if (string.IsNullOrWhiteSpace(presetName))
                return;
            ZoomByPreset[presetName] = ClampZoom(zoom);
        }

        public static double ClampZoom(double zoom)
        {
            if (double.IsNaN(zoom) || double.IsInfinity(zoom) || zoom <= 0)
                return DefaultZoom;
            if (zoom < MinZoom) return MinZoom;
            if (zoom > MaxZoom) return MaxZoom;
            return zoom;
        }
    }

    /// <summary>Bus-level audio settings stored in general settings (not in committable audio patches).</summary>
    public sealed class AudioPreferences
    {
        [JsonPropertyName("masterVolume")] public float MasterVolume { get; set; } = 1.0f;
        [JsonPropertyName("musicVolume")] public float MusicVolume { get; set; } = 0.7f;
        [JsonPropertyName("sfxVolume")] public float SfxVolume { get; set; } = 0.9f;
        [JsonPropertyName("masterEnabled")] public bool MasterEnabled { get; set; } = true;
        [JsonPropertyName("musicEnabled")] public bool MusicEnabled { get; set; } = true;
        [JsonPropertyName("sfxEnabled")] public bool SfxEnabled { get; set; } = true;
        [JsonPropertyName("musicCrossfadeMs")] public int MusicCrossfadeMs { get; set; } = AudioConfig.DefaultMusicCrossfadeMs;
        [JsonPropertyName("musicTransitionSyncBpm")] public float MusicTransitionSyncBpm { get; set; } = 0f;
        [JsonPropertyName("musicTransitionCarryElapsed")] public bool MusicTransitionCarryElapsed { get; set; } = true;

        public void ValidateAndFix()
        {
            MasterVolume = Clamp01(MasterVolume);
            MusicVolume = Clamp01(MusicVolume);
            SfxVolume = Clamp01(SfxVolume);
            MusicCrossfadeMs = Math.Clamp(MusicCrossfadeMs, 0, AudioConfig.MaxMusicCrossfadeMs);
            if (MusicTransitionSyncBpm < 0f) MusicTransitionSyncBpm = 0f;
            else if (MusicTransitionSyncBpm > 0f && MusicTransitionSyncBpm < 20f) MusicTransitionSyncBpm = 0f;
            else if (MusicTransitionSyncBpm > 400f) MusicTransitionSyncBpm = 400f;
        }

        private static float Clamp01(float v) => v < 0f ? 0f : (v > 1f ? 1f : v);
    }
}
