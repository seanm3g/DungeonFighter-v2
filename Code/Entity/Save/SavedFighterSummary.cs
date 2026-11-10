using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;

namespace RPGGame
{
    /// <summary>
    /// Light read of a save file for the main menu's fighter hover panel: no Character is built, and results are cached
    /// per file until it is written again.
    /// </summary>
    public sealed class SavedFighterSummary
    {
        public string Name { get; init; } = "";
        public int Level { get; init; }
        public int Xp { get; init; }
        public int CurrentHealth { get; init; }
        public int MaxHealth { get; init; }
        public int Strength { get; init; }
        public int Agility { get; init; }
        public int Technique { get; init; }
        public int Intelligence { get; init; }
        public string ClassName { get; init; } = "Fighter";
        public string Weapon { get; init; } = "";
        public string Region { get; init; } = "";
        public bool IsDead { get; init; }
        public int EnemiesDefeated { get; init; }
        public int DungeonsCompleted { get; init; }
        public int HighestHit { get; init; }
        public double PlayMinutes { get; init; }
        public DateTime LastPlayed { get; init; }

        private static readonly object CacheLock = new();
        private static readonly Dictionary<string, (DateTime written, SavedFighterSummary? summary)> Cache = new(StringComparer.OrdinalIgnoreCase);

        /// <summary>Summary for a save listed by <see cref="CharacterSaveManager.ListAllSavedCharacters"/>, or null when unreadable.</summary>
        public static SavedFighterSummary? Load(string characterId)
        {
            if (string.IsNullOrWhiteSpace(characterId) || characterId.EndsWith("_legacy", StringComparison.Ordinal))
                return null;
            try
            {
                string path = GameConstants.GetGameDataFilePath($"character_{characterId}_save.json");
                if (!File.Exists(path))
                    return null;
                DateTime written = File.GetLastWriteTime(path);
                lock (CacheLock)
                {
                    if (Cache.TryGetValue(path, out var hit) && hit.written == written)
                        return hit.summary;
                }
                var summary = Read(path, written);
                lock (CacheLock)
                    Cache[path] = (written, summary);
                return summary;
            }
            catch
            {
                return null;
            }
        }

        private static SavedFighterSummary? Read(string path, DateTime written)
        {
            try
            {
                using var doc = JsonDocument.Parse(File.ReadAllText(path));
                var root = doc.RootElement;
                int Int(JsonElement e, string name) =>
                    e.ValueKind == JsonValueKind.Object && e.TryGetProperty(name, out var v) && v.ValueKind == JsonValueKind.Number && v.TryGetInt32(out int i) ? i : 0;
                double Dbl(JsonElement e, string name) =>
                    e.ValueKind == JsonValueKind.Object && e.TryGetProperty(name, out var v) && v.ValueKind == JsonValueKind.Number ? v.GetDouble() : 0;
                string Str(JsonElement e, string name) =>
                    e.ValueKind == JsonValueKind.Object && e.TryGetProperty(name, out var v) && v.ValueKind == JsonValueKind.String ? v.GetString() ?? "" : "";

                var progression = new CharacterProgression
                {
                    BarbarianPoints = Int(root, "barbarianPoints"),
                    WarriorPoints = Int(root, "warriorPoints"),
                    RoguePoints = Int(root, "roguePoints"),
                    WizardPoints = Int(root, "wizardPoints")
                };
                string className;
                try { className = AttributeClassNameComposer.ComposeDisplayClass(progression, GameConfiguration.Instance.ClassPresentation); }
                catch { className = "Fighter"; }

                root.TryGetProperty("weapon", out var weapon);
                root.TryGetProperty("sessionStats", out var stats);
                return new SavedFighterSummary
                {
                    Name = Str(root, "name"),
                    Level = Int(root, "level"),
                    Xp = Int(root, "xp"),
                    CurrentHealth = Int(root, "currentHealth"),
                    MaxHealth = Int(root, "maxHealth"),
                    Strength = Int(root, "strength"),
                    Agility = Int(root, "agility"),
                    Technique = Int(root, "technique"),
                    Intelligence = Int(root, "intelligence"),
                    ClassName = className,
                    Weapon = Str(weapon, "name"),
                    Region = Str(root, "currentRegionId"),
                    IsDead = root.TryGetProperty("isDead", out var dead) && dead.ValueKind == JsonValueKind.True,
                    EnemiesDefeated = Int(stats, "enemiesDefeated"),
                    DungeonsCompleted = Int(stats, "dungeonsCompleted"),
                    HighestHit = Int(stats, "highestSingleHitDamage"),
                    PlayMinutes = Dbl(stats, "totalPlayTimeMinutes"),
                    LastPlayed = written
                };
            }
            catch
            {
                return null;
            }
        }
    }
}
