using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.RegularExpressions;
using RPGGame.Config;
using RPGGame.Entity.Services;

namespace RPGGame.ActionInteractionLab
{
    /// <summary>
    /// Persists named character presets under <c>GameData/LabSnapshots/</c> for Action Lab load.
    /// </summary>
    public static class CharacterLabSnapshotService
    {
        private static readonly JsonSerializerOptions JsonOptions = new()
        {
            WriteIndented = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };

        public static string GetSnapshotsDirectory()
        {
            string dir = Path.Combine(PatchProfileService.GetGameDataRoot(), "LabSnapshots");
            Directory.CreateDirectory(dir);
            return dir;
        }

        public static string SanitizeFileName(string displayName)
        {
            if (string.IsNullOrWhiteSpace(displayName))
                return "snapshot";
            string s = displayName.Trim();
            foreach (char c in Path.GetInvalidFileNameChars())
                s = s.Replace(c, '_');
            s = Regex.Replace(s, @"\s+", "_");
            s = Regex.Replace(s, @"_+", "_").Trim('_');
            return string.IsNullOrEmpty(s) ? "snapshot" : s;
        }

        public static string GetFilePath(string displayName)
        {
            string safe = SanitizeFileName(displayName);
            return Path.Combine(GetSnapshotsDirectory(), safe + ".json");
        }

        public static CharacterLabSnapshotData CreateFromCharacter(Character character, string displayName, string? notes = null)
        {
            if (character == null)
                throw new ArgumentNullException(nameof(character));
            if (string.IsNullOrWhiteSpace(displayName))
                throw new ArgumentException("Display name is required.", nameof(displayName));

            var serializer = new CharacterSerializer();
            return new CharacterLabSnapshotData
            {
                DisplayName = displayName.Trim(),
                CreatedUtc = DateTime.UtcNow,
                CharacterJson = serializer.Serialize(character),
                ComboStripActionNames = character.GetComboActions().Select(a => a.Name).ToList(),
                Notes = notes
            };
        }

        public static string Save(CharacterLabSnapshotData snapshot, bool overwrite = true)
        {
            if (snapshot == null)
                throw new ArgumentNullException(nameof(snapshot));
            if (string.IsNullOrWhiteSpace(snapshot.DisplayName))
                throw new ArgumentException("Snapshot DisplayName is required.");
            if (string.IsNullOrWhiteSpace(snapshot.CharacterJson))
                throw new ArgumentException("Snapshot CharacterJson is required.");

            string path = GetFilePath(snapshot.DisplayName);
            if (!overwrite && File.Exists(path))
                throw new InvalidOperationException($"Snapshot already exists: {snapshot.DisplayName}");

            string json = JsonSerializer.Serialize(snapshot, JsonOptions);
            File.WriteAllText(path, json);
            return path;
        }

        public static string SaveFromCharacter(Character character, string displayName, bool overwrite = true, string? notes = null)
        {
            var data = CreateFromCharacter(character, displayName, notes);
            return Save(data, overwrite);
        }

        public static CharacterLabSnapshotData? Load(string displayName)
        {
            if (string.IsNullOrWhiteSpace(displayName))
                return null;
            string path = GetFilePath(displayName);
            if (!File.Exists(path))
            {
                // Allow load by sanitized file stem when UI lists stems.
                path = Path.Combine(GetSnapshotsDirectory(), SanitizeFileName(displayName) + ".json");
            }
            if (!File.Exists(path))
                return null;

            string json = File.ReadAllText(path);
            return JsonSerializer.Deserialize<CharacterLabSnapshotData>(json, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });
        }

        public static bool Delete(string displayName)
        {
            string path = GetFilePath(displayName);
            if (!File.Exists(path))
                return false;
            File.Delete(path);
            return true;
        }

        /// <summary>Sorted display names (from file contents when available, else file stem).</summary>
        public static IReadOnlyList<string> ListNames()
        {
            string dir = GetSnapshotsDirectory();
            var names = new List<string>();
            foreach (string path in Directory.EnumerateFiles(dir, "*.json"))
            {
                try
                {
                    string json = File.ReadAllText(path);
                    var data = JsonSerializer.Deserialize<CharacterLabSnapshotData>(json, new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    });
                    if (data != null && !string.IsNullOrWhiteSpace(data.DisplayName))
                        names.Add(data.DisplayName.Trim());
                    else
                        names.Add(Path.GetFileNameWithoutExtension(path));
                }
                catch
                {
                    names.Add(Path.GetFileNameWithoutExtension(path));
                }
            }

            names.Sort(StringComparer.OrdinalIgnoreCase);
            return names;
        }

        /// <summary>Builds a lab hero from snapshot JSON and reapplied combo strip.</summary>
        public static Character CreateCharacter(CharacterLabSnapshotData snapshot)
        {
            if (snapshot == null)
                throw new ArgumentNullException(nameof(snapshot));
            if (string.IsNullOrWhiteSpace(snapshot.CharacterJson))
                throw new ArgumentException("Snapshot has no character JSON.", nameof(snapshot));

            var serializer = new CharacterSerializer();
            var data = serializer.Deserialize(snapshot.CharacterJson)
                       ?? throw new InvalidOperationException("Failed to deserialize snapshot character JSON.");
            var character = serializer.CreateCharacterFromSaveData(data);
            LabCombatEntityFactory.ReapplyComboStrip(character, snapshot.ComboStripActionNames ?? new List<string>(), preferActionPool: true);
            return character;
        }

        public static string SuggestDefaultName(Character character)
        {
            if (character == null)
                throw new ArgumentNullException(nameof(character));
            string baseName = string.IsNullOrWhiteSpace(character.Name) ? "Hero" : character.Name.Trim();
            string stamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
            return SanitizeFileName($"{baseName}_L{character.Level}_{stamp}");
        }
    }
}
