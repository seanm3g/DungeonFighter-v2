using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.RegularExpressions;
using RPGGame.Audio;

namespace RPGGame.Config
{
    /// <summary>
    /// Resolves patch library paths, loads the local <see cref="PatchProfile"/>, and writes patch files.
    /// </summary>
    public static class PatchProfileService
    {
        public const string ProfileFileName = "PatchProfile.json";
        private static readonly object ProfileLock = new();
        private static PatchProfile? _cachedProfile;
        private static string? _cachedGameDataRoot;
        private static bool _bootstrapDone;

        private static readonly Regex PatchNamePattern = new(@"^[a-zA-Z0-9][a-zA-Z0-9_-]{0,63}$", RegexOptions.CultureInvariant);

        public static string GetGameDataRoot()
        {
            if (_cachedGameDataRoot != null)
                return _cachedGameDataRoot;

            string? settingsDir = GameConstants.GetSettingsDirectory();
            if (!string.IsNullOrEmpty(settingsDir))
            {
                try { _cachedGameDataRoot = Path.GetFullPath(settingsDir); }
                catch { _cachedGameDataRoot = settingsDir; }
                return _cachedGameDataRoot;
            }

            string fallback = GameConstants.GetGameDataFilePath(string.Empty);
            try
            {
                string? dir = Path.GetDirectoryName(Path.GetFullPath(fallback));
                _cachedGameDataRoot = string.IsNullOrEmpty(dir) ? GameConstants.GameDataDirectory : dir;
            }
            catch
            {
                _cachedGameDataRoot = GameConstants.GameDataDirectory;
            }
            return _cachedGameDataRoot;
        }

        public static string GetProfileFilePath() =>
            Path.Combine(GetGameDataRoot(), ProfileFileName);

        public static string GetPatchesRoot() =>
            Path.Combine(GetGameDataRoot(), "Patches");

        public static string GetCategoryFolder(PatchCategory category) => category switch
        {
            PatchCategory.GameSettings => Path.Combine(GetPatchesRoot(), "GameSettings"),
            PatchCategory.Audio => Path.Combine(GetPatchesRoot(), "Audio"),
            PatchCategory.Balance => Path.Combine(GetPatchesRoot(), "Balance"),
            _ => throw new ArgumentOutOfRangeException(nameof(category))
        };

        public static string GetCategoryDisplayName(PatchCategory category) => category switch
        {
            PatchCategory.GameSettings => "Game settings",
            PatchCategory.Audio => "Audio / music",
            PatchCategory.Balance => "Balance",
            _ => category.ToString()
        };

        public static void EnsureBootstrapped()
        {
            if (_bootstrapDone) return;
            lock (ProfileLock)
            {
                if (_bootstrapDone) return;
                Directory.CreateDirectory(GetCategoryFolder(PatchCategory.GameSettings));
                Directory.CreateDirectory(GetCategoryFolder(PatchCategory.Audio));
                Directory.CreateDirectory(GetCategoryFolder(PatchCategory.Balance));

                MigrateLegacyIfNeeded(PatchCategory.GameSettings, LegacyGameSettingsPaths(), PatchProfile.DefaultPatchName);
                MigrateLegacyIfNeeded(PatchCategory.Audio, LegacyAudioPaths(), PatchProfile.DefaultPatchName);
                MigrateLegacyIfNeeded(PatchCategory.Balance, LegacyBalancePaths(), PatchProfile.DefaultPatchName);

                _bootstrapDone = true;
            }
        }

        public static PatchProfile LoadProfile()
        {
            EnsureBootstrapped();
            if (_cachedProfile != null)
                return _cachedProfile;

            lock (ProfileLock)
            {
                if (_cachedProfile != null)
                    return _cachedProfile;

                string path = GetProfileFilePath();
                if (File.Exists(path))
                {
                    try
                    {
                        string json = File.ReadAllText(path);
                        var profile = JsonSerializer.Deserialize<PatchProfile>(json);
                        if (profile != null)
                        {
                            NormalizeProfile(profile);
                            _cachedProfile = profile;
                            return _cachedProfile;
                        }
                    }
                    catch { /* fall through to default */ }
                }

                _cachedProfile = new PatchProfile();
                SaveProfile(_cachedProfile);
                return _cachedProfile;
            }
        }

        public static void SaveProfile(PatchProfile profile)
        {
            lock (ProfileLock)
            {
                NormalizeProfile(profile);
                string path = GetProfileFilePath();
                string? dir = Path.GetDirectoryName(path);
                if (!string.IsNullOrEmpty(dir))
                    Directory.CreateDirectory(dir);
                string json = JsonSerializer.Serialize(profile, new JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText(path, json);
                _cachedProfile = profile;
            }
        }

        public static void SetActivePatch(PatchCategory category, string patchName)
        {
            string sanitized = SanitizePatchName(patchName);
            if (!PatchExists(category, sanitized))
                throw new FileNotFoundException($"Patch '{sanitized}' was not found for {GetCategoryDisplayName(category)}.", GetPatchFilePath(category, sanitized));

            var profile = LoadProfile();
            profile.SetActivePatchName(category, sanitized);
            SaveProfile(profile);
            InvalidateRuntimeCaches(category);
        }

        public static IReadOnlyList<string> ListPatches(PatchCategory category)
        {
            EnsureBootstrapped();
            string folder = GetCategoryFolder(category);
            if (!Directory.Exists(folder))
                return Array.Empty<string>();

            return Directory.GetFiles(folder, "*.json", SearchOption.TopDirectoryOnly)
                .Select(f => Path.GetFileNameWithoutExtension(f))
                .Where(n => !string.IsNullOrEmpty(n))
                .OrderBy(n => n, StringComparer.OrdinalIgnoreCase)
                .ToList();
        }

        public static bool PatchExists(PatchCategory category, string patchName) =>
            File.Exists(GetPatchFilePath(category, patchName));

        public static string GetActivePatchFilePath(PatchCategory category)
        {
            EnsureBootstrapped();
            var profile = LoadProfile();
            string name = profile.GetActivePatchName(category);
            string path = GetPatchFilePath(category, name);
            if (File.Exists(path))
                return path;

            string defaultPath = GetPatchFilePath(category, PatchProfile.DefaultPatchName);
            if (File.Exists(defaultPath))
            {
                profile.SetActivePatchName(category, PatchProfile.DefaultPatchName);
                SaveProfile(profile);
                return defaultPath;
            }

            Directory.CreateDirectory(GetCategoryFolder(category));
            File.WriteAllText(defaultPath, "{}");
            profile.SetActivePatchName(category, PatchProfile.DefaultPatchName);
            SaveProfile(profile);
            return defaultPath;
        }

        public static string GetPatchFilePath(PatchCategory category, string patchName) =>
            Path.Combine(GetCategoryFolder(category), SanitizePatchName(patchName) + ".json");

        public static string SanitizePatchName(string patchName)
        {
            if (string.IsNullOrWhiteSpace(patchName))
                throw new ArgumentException("Patch name is required.", nameof(patchName));

            string trimmed = patchName.Trim();
            if (!PatchNamePattern.IsMatch(trimmed))
                throw new ArgumentException("Patch name may use letters, numbers, hyphens, and underscores (max 64 characters).", nameof(patchName));

            return trimmed;
        }

        public static void WritePatchContent(PatchCategory category, string patchName, string jsonContent, bool overwrite)
        {
            EnsureBootstrapped();
            string sanitized = SanitizePatchName(patchName);
            string path = GetPatchFilePath(category, sanitized);
            if (File.Exists(path) && !overwrite)
                throw new IOException($"Patch '{sanitized}' already exists.");

            string? dir = Path.GetDirectoryName(path);
            if (!string.IsNullOrEmpty(dir))
                Directory.CreateDirectory(dir);

            string temp = path + ".tmp";
            File.WriteAllText(temp, jsonContent);
            if (File.Exists(path))
                File.Replace(temp, path, path + ".bak");
            else
                File.Move(temp, path);
        }

        public static void CreatePatch(PatchCategory category, string patchName, string jsonContent, bool switchActive)
        {
            WritePatchContent(category, patchName, jsonContent, overwrite: false);
            if (switchActive)
                SetActivePatch(category, patchName);
        }

        public static void UpdateActivePatch(PatchCategory category, string jsonContent)
        {
            var profile = LoadProfile();
            string name = profile.GetActivePatchName(category);
            WritePatchContent(category, name, jsonContent, overwrite: true);
            InvalidateRuntimeCaches(category);
        }

        /// <summary>Copies an external JSON file into the patch library under <paramref name="patchName"/>.</summary>
        public static string ImportPatchFromFile(PatchCategory category, string sourceFilePath, string patchName, bool overwrite = false)
        {
            if (!File.Exists(sourceFilePath))
                throw new FileNotFoundException("Import source file was not found.", sourceFilePath);

            string json = File.ReadAllText(sourceFilePath);
            if (string.IsNullOrWhiteSpace(json))
                throw new InvalidDataException("Import file is empty.");

            using var doc = JsonDocument.Parse(json);
            if (doc.RootElement.ValueKind != JsonValueKind.Object)
                throw new InvalidDataException("Patch file must be a JSON object.");

            WritePatchContent(category, patchName, json, overwrite);
            return GetPatchFilePath(category, SanitizePatchName(patchName));
        }

        /// <summary>Copies a library patch to an external path.</summary>
        public static void ExportPatchToFile(PatchCategory category, string patchName, string destinationFilePath)
        {
            string sanitized = SanitizePatchName(patchName);
            string source = GetPatchFilePath(category, sanitized);
            if (!File.Exists(source))
                throw new FileNotFoundException($"Patch '{sanitized}' was not found.", source);

            string? dir = Path.GetDirectoryName(destinationFilePath);
            if (!string.IsNullOrEmpty(dir))
                Directory.CreateDirectory(dir);

            File.Copy(source, destinationFilePath, overwrite: true);
        }

        public static PatchCategory ParseCategoryLabel(string label) => label switch
        {
            "Game settings" => PatchCategory.GameSettings,
            "Audio / music" => PatchCategory.Audio,
            "Balance" => PatchCategory.Balance,
            _ => throw new ArgumentException($"Unknown patch category: {label}", nameof(label))
        };

        public static string GetCategoryLabel(PatchCategory category) => category switch
        {
            PatchCategory.GameSettings => "Game settings",
            PatchCategory.Audio => "Audio / music",
            PatchCategory.Balance => "Balance",
            _ => category.ToString()
        };

        public static void InvalidateRuntimeCaches(PatchCategory category)
        {
            switch (category)
            {
                case PatchCategory.GameSettings:
                    GameSettings.InvalidatePatchPathCache();
                    GameSettings.ReloadFromFile();
                    break;
                case PatchCategory.Audio:
                    AudioConfig.InvalidatePatchPathCache();
                    AudioConfig.ReloadFromFile();
                    break;
                case PatchCategory.Balance:
                    GameConfiguration.ResetInstance();
                    break;
            }
        }

        public static void InvalidateAllRuntimeCaches()
        {
            GameSettings.InvalidatePatchPathCache();
            AudioConfig.InvalidatePatchPathCache();
            GameConfiguration.ResetInstance();
            GameSettings.ReloadFromFile();
            AudioConfig.ReloadFromFile();
            _ = GameConfiguration.Instance;
        }

        /// <summary>Test hook: redirect patch root and reset bootstrap state.</summary>
        internal static void SetGameDataRootForTests(string root)
        {
            lock (ProfileLock)
            {
                _cachedGameDataRoot = root;
                _cachedProfile = null;
                _bootstrapDone = false;
            }
        }

        private static void NormalizeProfile(PatchProfile profile)
        {
            if (string.IsNullOrWhiteSpace(profile.ActiveGameSettingsPatch))
                profile.ActiveGameSettingsPatch = PatchProfile.DefaultPatchName;
            if (string.IsNullOrWhiteSpace(profile.ActiveAudioPatch))
                profile.ActiveAudioPatch = PatchProfile.DefaultPatchName;
            if (string.IsNullOrWhiteSpace(profile.ActiveBalancePatch))
                profile.ActiveBalancePatch = PatchProfile.DefaultPatchName;
        }

        private static void MigrateLegacyIfNeeded(PatchCategory category, IEnumerable<string> legacyCandidates, string defaultPatchName)
        {
            string target = GetPatchFilePath(category, defaultPatchName);
            if (File.Exists(target))
                return;

            foreach (string candidate in legacyCandidates)
            {
                try
                {
                    string full = Path.GetFullPath(candidate);
                    if (!File.Exists(full))
                        continue;
                    string? dir = Path.GetDirectoryName(target);
                    if (!string.IsNullOrEmpty(dir))
                        Directory.CreateDirectory(dir);
                    File.Copy(full, target, overwrite: false);
                    return;
                }
                catch
                {
                    // try next candidate
                }
            }
        }

        private static IEnumerable<string> LegacyGameSettingsPaths()
        {
            string root = GetGameDataRoot();
            yield return Path.Combine(root, GameConstants.GameSettingsJson);
            yield return Path.Combine(Directory.GetCurrentDirectory(), GameConstants.GameSettingsJson);
            yield return Path.Combine(Directory.GetCurrentDirectory(), "Code", GameConstants.GameSettingsJson);
        }

        private static IEnumerable<string> LegacyAudioPaths()
        {
            string root = GetGameDataRoot();
            yield return Path.Combine(root, "Audio", "AudioConfig.json");
        }

        private static IEnumerable<string> LegacyBalancePaths()
        {
            string root = GetGameDataRoot();
            yield return Path.Combine(root, GameConstants.TuningConfigJson);
            yield return Path.Combine(Directory.GetCurrentDirectory(), "GameData", GameConstants.TuningConfigJson);
        }
    }
}
