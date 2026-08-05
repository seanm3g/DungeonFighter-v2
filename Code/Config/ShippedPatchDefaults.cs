using System;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace RPGGame.Config
{
    /// <summary>
    /// Repo-tracked defaults for first-run / missing <c>PatchProfile.json</c>.
    /// Unlike the gitignored profile, this file ships with the client so new players
    /// start on the intended balance (and audio) patch.
    /// </summary>
    public sealed class ShippedPatchDefaultsDocument
    {
        [JsonPropertyName("defaultBalancePatch")]
        public string DefaultBalancePatch { get; set; } = PatchProfile.DefaultPatchName;

        [JsonPropertyName("defaultAudioPatch")]
        public string DefaultAudioPatch { get; set; } = PatchProfile.DefaultPatchName;
    }

    /// <summary>
    /// Loads and saves <c>GameData/ShippedPatchDefaults.json</c>.
    /// </summary>
    public static class ShippedPatchDefaults
    {
        public const string FileName = "ShippedPatchDefaults.json";

        private static readonly object StoreLock = new();
        private static ShippedPatchDefaultsDocument? _cached;

        private static readonly JsonSerializerOptions JsonOptions = new()
        {
            WriteIndented = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            PropertyNameCaseInsensitive = true
        };

        public static string GetFilePath() =>
            Path.Combine(PatchProfileService.GetGameDataRoot(), FileName);

        public static ShippedPatchDefaultsDocument Load()
        {
            lock (StoreLock)
            {
                if (_cached != null)
                    return _cached;

                string path = GetFilePath();
                if (File.Exists(path))
                {
                    try
                    {
                        string json = File.ReadAllText(path);
                        var doc = JsonSerializer.Deserialize<ShippedPatchDefaultsDocument>(json, JsonOptions);
                        if (doc != null)
                        {
                            Normalize(doc);
                            _cached = doc;
                            return _cached;
                        }
                    }
                    catch
                    {
                        // fall through to defaults
                    }
                }

                _cached = new ShippedPatchDefaultsDocument();
                return _cached;
            }
        }

        public static void Save(ShippedPatchDefaultsDocument document)
        {
            if (document == null)
                throw new ArgumentNullException(nameof(document));

            lock (StoreLock)
            {
                Normalize(document);
                string path = GetFilePath();
                string? dir = Path.GetDirectoryName(path);
                if (!string.IsNullOrEmpty(dir))
                    Directory.CreateDirectory(dir);

                string json = JsonSerializer.Serialize(document, JsonOptions);
                File.WriteAllText(path, json);
                _cached = document;
            }
        }

        /// <summary>
        /// Balance patch name used when creating a new local profile, or when the
        /// active selection is missing. Falls back to <see cref="PatchProfile.DefaultPatchName"/>
        /// if the shipped name is blank or the file is absent.
        /// </summary>
        public static string ResolveDefaultBalancePatchName()
        {
            var doc = Load();
            string name = string.IsNullOrWhiteSpace(doc.DefaultBalancePatch)
                ? PatchProfile.DefaultPatchName
                : doc.DefaultBalancePatch.Trim();

            try
            {
                name = PatchProfileService.SanitizePatchName(name);
            }
            catch
            {
                return PatchProfile.DefaultPatchName;
            }

            if (PatchProfileService.PatchExists(PatchCategory.Balance, name))
                return name;

            return PatchProfile.DefaultPatchName;
        }

        /// <summary>
        /// Audio patch name for first-run profile creation. Falls back to
        /// <see cref="PatchProfile.DefaultPatchName"/> when missing.
        /// </summary>
        public static string ResolveDefaultAudioPatchName()
        {
            var doc = Load();
            string name = string.IsNullOrWhiteSpace(doc.DefaultAudioPatch)
                ? PatchProfile.DefaultPatchName
                : doc.DefaultAudioPatch.Trim();

            try
            {
                name = PatchProfileService.SanitizePatchName(name);
            }
            catch
            {
                return PatchProfile.DefaultPatchName;
            }

            if (PatchProfileService.PatchExists(PatchCategory.Audio, name))
                return name;

            return PatchProfile.DefaultPatchName;
        }

        public static void SetDefaultBalancePatch(string patchName)
        {
            string sanitized = PatchProfileService.SanitizePatchName(patchName);
            if (!PatchProfileService.PatchExists(PatchCategory.Balance, sanitized))
                throw new FileNotFoundException(
                    $"Balance patch '{sanitized}' was not found.",
                    PatchProfileService.GetPatchFilePath(PatchCategory.Balance, sanitized));

            var doc = Load();
            doc.DefaultBalancePatch = sanitized;
            Save(doc);
        }

        /// <summary>Test hook: clear cached document.</summary>
        internal static void InvalidateCache()
        {
            lock (StoreLock)
            {
                _cached = null;
            }
        }

        private static void Normalize(ShippedPatchDefaultsDocument doc)
        {
            if (string.IsNullOrWhiteSpace(doc.DefaultBalancePatch))
                doc.DefaultBalancePatch = PatchProfile.DefaultPatchName;
            if (string.IsNullOrWhiteSpace(doc.DefaultAudioPatch))
                doc.DefaultAudioPatch = PatchProfile.DefaultPatchName;
        }
    }
}
