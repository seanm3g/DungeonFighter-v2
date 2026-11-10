using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using SkiaSharp;

namespace RPGGame.UI.Avalonia
{
    /// <summary>
    /// Combat-screen backdrops packed by df-darkfantasy's <c>build_room_backdrops.py</c>: <c>GameData/RoomArt/index.json</c>
    /// lists the 50 ft environment plates for each room location (Rooms.json "location"). A room resolves by exact
    /// name, then by the longest location name it contains. One plate is picked at random each time the room changes
    /// and kept for as long as that room is shown; only that one bitmap is held (the previous one is left to the GC, since the canvas may still be drawing it).
    /// </summary>
    public static class RoomBackdropArt
    {
        private const string Folder = "RoomArt";

        private static readonly object Gate = new();
        private static readonly Random Rng = new();
        private static bool _loaded;
        private static string _dir = "";
        private static readonly Dictionary<string, string[]> Rooms = new(StringComparer.OrdinalIgnoreCase);
        private static string? _currentRoom;
        private static SKImage? _current;

        public static bool IsAvailable
        {
            get { EnsureLoaded(); return Rooms.Count > 0; }
        }

        /// <summary>The backdrop for <paramref name="roomName"/>; the same bitmap while the room name stays the same.</summary>
        public static bool TryGetBackdrop(string? roomName, out SKImage bitmap)
        {
            bitmap = null!;
            if (string.IsNullOrWhiteSpace(roomName))
                return false;
            EnsureLoaded();
            lock (Gate)
            {
                if (!string.Equals(_currentRoom, roomName, StringComparison.OrdinalIgnoreCase))
                {
                    _currentRoom = roomName;
                    _current = Load(Resolve(roomName));
                }
                if (_current == null)
                    return false;
                bitmap = _current;
                return true;
            }
        }

        private static string? _currentRegion;
        private static SKImage? _regionImage;
        private static Dictionary<string, List<string>>? _regionLocations;

        /// <summary>
        /// An environment plate for a region (hub right panel): a random plate from one of the region element's room
        /// locations (Rooms.json "region" = <paramref name="regionTheme"/>). Kept while <paramref name="regionKey"/> stays the same;
        /// held apart from the room plate so going in and out of a dungeon does not re-roll either.
        /// </summary>
        public static bool TryGetRegionBackdrop(string? regionKey, string? regionTheme, out SKImage bitmap)
        {
            bitmap = null!;
            if (string.IsNullOrWhiteSpace(regionKey) || string.IsNullOrWhiteSpace(regionTheme))
                return false;
            EnsureLoaded();
            lock (Gate)
            {
                if (!string.Equals(_currentRegion, regionKey, StringComparison.OrdinalIgnoreCase))
                {
                    _currentRegion = regionKey;
                    _regionImage = null;
                    var locations = GetRegionLocations(regionTheme);
                    if (locations.Count > 0)
                        _regionImage = Load(Rooms[locations[Rng.Next(locations.Count)]]);
                }
                if (_regionImage == null)
                    return false;
                bitmap = _regionImage;
                return true;
            }
        }

        /// <summary>Locations (with plates) of the rooms whose Rooms.json "region" is <paramref name="regionTheme"/>. Caller holds <see cref="Gate"/>.</summary>
        private static List<string> GetRegionLocations(string regionTheme)
        {
            if (_regionLocations == null)
            {
                _regionLocations = new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase);
                try
                {
                    string path = GameConstants.ResolveGameDataPath("Rooms.json");
                    if (File.Exists(path))
                    {
                        using var doc = JsonDocument.Parse(File.ReadAllText(path));
                        foreach (var room in doc.RootElement.EnumerateArray())
                        {
                            string region = room.TryGetProperty("region", out var r) ? r.GetString() ?? "" : "";
                            string location = room.TryGetProperty("location", out var l) ? l.GetString() ?? "" : "";
                            if (region.Length == 0 || location.Length == 0)
                                continue;
                            string? key = ResolveLocation(location);
                            if (key == null)
                                continue;
                            if (!_regionLocations.TryGetValue(region, out var list))
                                _regionLocations[region] = list = new List<string>();
                            if (!list.Contains(key))
                                list.Add(key);
                        }
                    }
                }
                catch (Exception ex)
                {
                    DebugLogger.Log("RoomBackdropArt", $"Could not read Rooms.json regions: {ex.Message}");
                }
            }
            return _regionLocations.TryGetValue(regionTheme, out var found) ? found : new List<string>();
        }

        /// <summary>Which location's plates a room name maps to; null = none.</summary>
        internal static string? ResolveLocation(string roomName)
        {
            EnsureLoaded();
            string? key = Rooms.ContainsKey(roomName.Trim()) ? roomName.Trim() : null;
            if (key != null)
                return key;
            string padded = " " + roomName.ToLowerInvariant() + " ";
            return Rooms.Keys
                .Where(k => padded.Contains(" " + k.ToLowerInvariant() + " ", StringComparison.Ordinal))
                .OrderByDescending(k => k.Length)
                .FirstOrDefault();
        }

        private static string[] Resolve(string roomName)
        {
            string? location = ResolveLocation(roomName);
            return location != null ? Rooms[location] : Array.Empty<string>();
        }

        private static SKImage? Load(string[] files)
        {
            if (files.Length == 0)
                return null;
            string file = files[Rng.Next(files.Length)];
            try
            {
                var image = SKImage.FromEncodedData(Path.Combine(_dir, file));
                if (image == null)
                    DebugLogger.Log("RoomBackdropArt", $"Could not decode {file}");
                return image;
            }
            catch (Exception ex)
            {
                DebugLogger.Log("RoomBackdropArt", $"Could not load {file}: {ex.Message}");
                return null;
            }
        }

        private static void EnsureLoaded()
        {
            lock (Gate)
            {
                if (_loaded)
                    return;
                _loaded = true;
                try
                {
                    _dir = GameConstants.ResolveGameDataPath(Folder);
                    string indexPath = Path.Combine(_dir, "index.json");
                    if (!File.Exists(indexPath))
                        return;

                    using var doc = JsonDocument.Parse(File.ReadAllText(indexPath));
                    foreach (var prop in doc.RootElement.GetProperty("rooms").EnumerateObject())
                    {
                        var files = prop.Value.EnumerateArray()
                            .Select(f => f.GetString() ?? "")
                            .Where(f => f.Length > 0 && File.Exists(Path.Combine(_dir, f)))
                            .ToArray();
                        if (files.Length > 0)
                            Rooms[prop.Name] = files;
                    }
                }
                catch (Exception ex)
                {
                    Rooms.Clear();
                    DebugLogger.Log("RoomBackdropArt", $"Could not read {Folder}/index.json: {ex.Message}");
                }
            }
        }
    }
}
