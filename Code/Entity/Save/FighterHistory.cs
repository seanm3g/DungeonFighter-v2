using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using RPGGame.Utils;

namespace RPGGame
{
    /// <summary>One fallen fighter in the History log: who they were, what killed them, and their life's bests.</summary>
    public class FighterHistoryEntry
    {
        public string Name { get; set; } = "";
        public string Class { get; set; } = "";
        public int Level { get; set; }
        public DateTime DiedAt { get; set; }
        public string KilledBy { get; set; } = "";
        public int KillerLevel { get; set; }
        public string Dungeon { get; set; } = "";
        public string Room { get; set; } = "";
        public string Region { get; set; } = "";

        public int TotalXP { get; set; }
        public int EnemiesDefeated { get; set; }
        public int DungeonsCompleted { get; set; }
        public int RoomsCleared { get; set; }
        public int TotalDamageDealt { get; set; }
        public int TotalDamageReceived { get; set; }
        public int HighestSingleHitDamage { get; set; }
        public int HighestTurnDamage { get; set; }
        public int HighestComboStep { get; set; }
        public int TotalCriticalHits { get; set; }
        public int ItemsCollected { get; set; }
        public int LegendaryItemsFound { get; set; }
    }

    /// <summary>
    /// Persistent log of fallen fighters (<c>GameData/fighter_history.json</c>) behind the main menu's History screen.
    /// Bests such as highest level or biggest hit are derived from the entries rather than stored separately.
    /// </summary>
    public static class FighterHistory
    {
        public const string FileName = "fighter_history.json";

        private static readonly object FileLock = new();
        private static readonly JsonSerializerOptions JsonOptions = new()
        {
            WriteIndented = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            PropertyNameCaseInsensitive = true
        };

        /// <summary>Off by default so tests and headless tools never write history; the GUI launch turns it on.</summary>
        public static bool RecordingEnabled { get; set; }

        private static string FilePath => GameConstants.GetGameDataFilePath(FileName);

        /// <summary>All recorded deaths, oldest first. Empty when the file is missing or unreadable.</summary>
        public static List<FighterHistoryEntry> Load()
        {
            lock (FileLock)
                return LoadUnlocked() ?? new List<FighterHistoryEntry>();
        }

        /// <summary>Appends a death record for <paramref name="fighter"/>. Never throws: history must not break the death flow.</summary>
        public static void RecordDeath(Character fighter, Enemy? killer, string? dungeonName, string? roomName)
        {
            if (!RecordingEnabled)
                return;
            try
            {
                var entry = CreateEntry(fighter, killer, dungeonName, roomName);
                lock (FileLock)
                {
                    // Unreadable file: leave it alone rather than overwrite the whole history with one entry.
                    var entries = LoadUnlocked();
                    if (entries == null)
                        return;
                    entries.Add(entry);
                    File.WriteAllText(FilePath, JsonSerializer.Serialize(entries, JsonOptions));
                }
            }
            catch (Exception ex)
            {
                ScrollDebugLogger.LogAlways($"FighterHistory: failed to record death: {ex.Message}");
            }
        }

        public static FighterHistoryEntry CreateEntry(Character fighter, Enemy? killer, string? dungeonName, string? roomName)
        {
            var stats = fighter.SessionStats;
            stats.EndTurn();
            return new FighterHistoryEntry
            {
                Name = fighter.Name,
                Class = fighter.GetCurrentClass(),
                Level = Math.Max(fighter.Level, stats.FinalLevel),
                DiedAt = DateTime.Now,
                KilledBy = killer?.Name ?? "",
                KillerLevel = killer?.Level ?? 0,
                Dungeon = dungeonName ?? "",
                Room = roomName ?? "",
                Region = fighter.CurrentRegionId ?? "",
                TotalXP = stats.TotalXP,
                EnemiesDefeated = stats.EnemiesDefeated,
                DungeonsCompleted = stats.DungeonsCompleted,
                RoomsCleared = stats.RoomsCleared,
                TotalDamageDealt = stats.TotalDamageDealt,
                TotalDamageReceived = stats.TotalDamageReceived,
                HighestSingleHitDamage = stats.HighestSingleHitDamage,
                HighestTurnDamage = stats.HighestTurnDamage,
                HighestComboStep = stats.HighestComboStep,
                TotalCriticalHits = stats.TotalCriticalHits,
                ItemsCollected = stats.ItemsCollected,
                LegendaryItemsFound = stats.LegendaryItemsFound
            };
        }

        /// <summary>Entries on disk, empty when the file is missing, or null when it exists but cannot be read.</summary>
        private static List<FighterHistoryEntry>? LoadUnlocked()
        {
            try
            {
                string path = FilePath;
                if (!File.Exists(path))
                    return new List<FighterHistoryEntry>();
                return JsonSerializer.Deserialize<List<FighterHistoryEntry>>(File.ReadAllText(path), JsonOptions)
                    ?? new List<FighterHistoryEntry>();
            }
            catch (Exception ex)
            {
                ScrollDebugLogger.LogAlways($"FighterHistory: could not read {FileName}: {ex.Message}");
                return null;
            }
        }
    }
}
