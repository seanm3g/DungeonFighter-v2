using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using RPGGame.Utils;

namespace RPGGame
{
    /// <summary>
    /// Stable mode opens every fighter on the main menu roster, except the ones the player closed out. Closed fighters
    /// are remembered in <c>GameData/stable_closed.json</c> until they are added back from the saved-hero list.
    /// </summary>
    public static class StableRosterSync
    {
        public const string FileName = "stable_closed.json";

        private static readonly object Lock = new();
        private static HashSet<string>? closed;

        /// <summary>Off by default so tests and headless tools never load every save; the GUI launch turns it on.</summary>
        public static bool Enabled { get; set; }

        private static string FilePath => GameConstants.GetGameDataFilePath(FileName);

        public static bool IsClosed(string characterId)
        {
            lock (Lock)
                return Closed().Contains(characterId);
        }

        /// <summary>Keeps this fighter out of Stable mode until it is reopened.</summary>
        public static void Close(string characterId)
        {
            if (string.IsNullOrEmpty(characterId))
                return;
            lock (Lock)
            {
                if (Closed().Add(characterId))
                    Write();
            }
        }

        public static void Reopen(string characterId)
        {
            if (string.IsNullOrEmpty(characterId))
                return;
            lock (Lock)
            {
                if (Closed().Remove(characterId))
                    Write();
            }
        }

        /// <summary>Saves to open in Stable mode: the main menu roster order (named saves, most recent first), minus closed ones.</summary>
        public static List<string> SavesToOpen(int max)
        {
            List<(string characterId, string characterName, int level)> saves;
            try { saves = CharacterSaveManager.ListAllSavedCharacters() ?? new(); }
            catch { return new List<string>(); }
            return saves
                .Where(s => !string.IsNullOrWhiteSpace(s.characterName))
                .Take(MainMenuHandler.MaxFighterHotkeys)
                .Select(s => s.characterId)
                .Where(id => !IsClosed(id) && SavedFighterSummary.Load(id)?.IsDead != true)
                .Take(max)
                .ToList();
        }

        private static HashSet<string> Closed()
        {
            if (closed != null)
                return closed;
            closed = new HashSet<string>(StringComparer.Ordinal);
            if (!Enabled)
                return closed;
            try
            {
                if (File.Exists(FilePath))
                    foreach (var id in JsonSerializer.Deserialize<List<string>>(File.ReadAllText(FilePath)) ?? new())
                        closed.Add(id);
            }
            catch (Exception ex)
            {
                ScrollDebugLogger.LogAlways($"StableRosterSync: could not read {FileName}: {ex.Message}");
            }
            return closed;
        }

        private static void Write()
        {
            if (!Enabled || closed == null)
                return;
            try
            {
                File.WriteAllText(FilePath, JsonSerializer.Serialize(closed.OrderBy(s => s).ToList(), new JsonSerializerOptions { WriteIndented = true }));
            }
            catch (Exception ex)
            {
                ScrollDebugLogger.LogAlways($"StableRosterSync: could not write {FileName}: {ex.Message}");
            }
        }
    }
}
