using System;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace RPGGame
{
    /// <summary>
    /// Handles loading and caching of loot modification and action tables from JSON files
    /// Tables are loaded once and cached for performance
    /// </summary>
    public static class LootTableLoader
    {
        private static ModificationTables? _modificationTables;
        private static ActionTables? _actionTables;
        private static bool _tablesLoaded = false;

        /// <summary>
        /// Gets the modification tables, loading from JSON if needed
        /// </summary>
        public static ModificationTables GetModificationTables()
        {
            if (_modificationTables == null)
            {
                LoadTables();
            }
            return _modificationTables ?? new ModificationTables();
        }

        /// <summary>
        /// Gets the action tables, loading from JSON if needed
        /// </summary>
        public static ActionTables GetActionTables()
        {
            if (_actionTables == null)
            {
                LoadTables();
            }
            return _actionTables ?? new ActionTables();
        }

        /// <summary>
        /// Loads both modification and action tables from JSON files
        /// File paths are relative to the GameData directory
        /// </summary>
        private static void LoadTables()
        {
            if (_tablesLoaded)
                return;

            _tablesLoaded = true;

            try
            {
                // Try to load modification tables
                var modPath = Path.Combine(GetGameDataPath(), "ModificationTables.json");
                if (File.Exists(modPath))
                {
                    var json = File.ReadAllText(modPath);
                    var options = new JsonSerializerOptions
                    {
                        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                        WriteIndented = true,
                        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
                    };
                    _modificationTables = JsonSerializer.Deserialize<ModificationTables>(json, options);
                }
                else
                {
                    _modificationTables = new ModificationTables();
                }

                // Try to load action tables
                var actionPath = Path.Combine(GetGameDataPath(), "ActionTables.json");
                if (File.Exists(actionPath))
                {
                    var json = File.ReadAllText(actionPath);
                    var options = new JsonSerializerOptions
                    {
                        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                        WriteIndented = true,
                        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
                    };
                    _actionTables = JsonSerializer.Deserialize<ActionTables>(json, options);
                }
                else
                {
                    _actionTables = new ActionTables();
                }
            }
            catch (Exception ex)
            {
                // If loading fails, use empty tables as fallback
                Console.WriteLine($"Error loading loot tables: {ex.Message}");
                _modificationTables = new ModificationTables();
                _actionTables = new ActionTables();
            }
        }

        /// <summary>
        /// Reloads the tables from disk (useful for live editing during development)
        /// </summary>
        public static void Reload()
        {
            _modificationTables = null;
            _actionTables = null;
            _tablesLoaded = false;
            LoadTables();
        }

        /// <summary>
        /// Gets the path to the GameData directory
        /// Looks for it relative to the executable location
        /// </summary>
        private static string GetGameDataPath()
        {
            // Try relative to current directory first
            var relativePath = Path.Combine(Directory.GetCurrentDirectory(), "GameData");
            if (Directory.Exists(relativePath))
                return relativePath;

            // Try relative to executing assembly
            var assemblyPath = Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location) ?? "";
            var pathFromAssembly = Path.Combine(assemblyPath, "..", "..", "..", "GameData");
            if (Directory.Exists(pathFromAssembly))
                return Path.GetFullPath(pathFromAssembly);

            // Fallback to current directory + GameData
            return Path.Combine(Directory.GetCurrentDirectory(), "GameData");
        }

        /// <summary>
        /// Clears the cached tables
        /// Used for testing or to force a reload
        /// </summary>
        public static void Clear()
        {
            _modificationTables = null;
            _actionTables = null;
            _tablesLoaded = false;
        }
    }
}
