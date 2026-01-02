using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace RPGGame
{
    public class RoomActionData
    {
        [JsonPropertyName("name")]
        public string Name { get; set; } = "";
        [JsonPropertyName("weight")]
        public double Weight { get; set; }
    }

    public class RoomData
    {
        [JsonPropertyName("name")]
        public string Name { get; set; } = "";
        [JsonPropertyName("description")]
        public string Description { get; set; } = "";
        [JsonPropertyName("theme")]
        public string Theme { get; set; } = "";
        [JsonPropertyName("isHostile")]
        public bool IsHostile { get; set; }
        [JsonPropertyName("actions")]
        public List<RoomActionData> Actions { get; set; } = new List<RoomActionData>();
    }

    public static class RoomLoader
    {
        private static Dictionary<string, RoomData>? _rooms;
        private static readonly string[] PossibleRoomPaths = {
            Path.Combine("GameData", "Rooms.json"),
            Path.Combine("..", "GameData", "Rooms.json"),
            Path.Combine("..", "..", "GameData", "Rooms.json"),
            Path.Combine("DF4 - CONSOLE", "GameData", "Rooms.json"),
            Path.Combine("..", "DF4 - CONSOLE", "GameData", "Rooms.json")
        };

        public static void LoadRooms()
        {
            try
            {
                string? foundPath = null;
                foreach (string path in PossibleRoomPaths)
                {
                    if (File.Exists(path))
                    {
                        foundPath = path;
                        break;
                    }
                }

                if (foundPath != null)
                {
                    string jsonContent = File.ReadAllText(foundPath);
                    
                    var roomList = JsonSerializer.Deserialize<List<RoomData>>(jsonContent);
                    
                    _rooms = new Dictionary<string, RoomData>();
                    if (roomList != null)
                    {
                        foreach (var room in roomList)
                        {
                            if (!string.IsNullOrEmpty(room.Name))
                            {
                                _rooms[room.Name] = room;
                            }
                            else
                            {
                                // Silently skip rooms with null/empty names
                                // UIManager may not be available during tests
                            }
                        }
                    }
                    else
                    {
                        // JSON deserialization returned null - will use empty dictionary
                        // UIManager may not be available during tests
                    }
                }
                else
                {
                    // Rooms file not found - will use empty dictionary (fallback generation will be used)
                    // UIManager may not be available during tests
                    _rooms = new Dictionary<string, RoomData>();
                }
            }
            catch (Exception ex)
            {
                // Log error if UIManager is available, but don't fail if it's not (e.g., during tests)
                try
                {
                    UIManager.WriteSystemLine($"Error loading rooms: {ex.Message}");
                    if (GameConfiguration.IsDebugEnabled)
                    {
                        UIManager.WriteSystemLine($"Stack trace: {ex.StackTrace}");
                    }
                }
                catch
                {
                    // UIManager not available (e.g., during tests) - continue silently
                    if (GameConfiguration.IsDebugEnabled)
                    {
                        System.Console.WriteLine($"Error loading rooms: {ex.Message}");
                    }
                }
                _rooms = new Dictionary<string, RoomData>();
            }
        }

        public static RoomData? GetRoomData(string roomName)
        {
            if (_rooms == null)
            {
                LoadRooms();
            }

            return _rooms?.GetValueOrDefault(roomName);
        }

        public static List<string> GetAllRoomNames()
        {
            if (_rooms == null)
            {
                LoadRooms();
            }

            return _rooms?.Keys.ToList() ?? new List<string>();
        }

        public static List<string> GetRoomsByTheme(string theme)
        {
            if (_rooms == null)
            {
                LoadRooms();
            }

            return _rooms?.Where(r => r.Value.Theme.Equals(theme, StringComparison.OrdinalIgnoreCase))
                        .Select(r => r.Key)
                        .ToList() ?? new List<string>();
        }

        public static List<string> GetRoomsByHostility(bool isHostile)
        {
            if (_rooms == null)
            {
                LoadRooms();
            }

            return _rooms?.Where(r => r.Value.IsHostile == isHostile)
                        .Select(r => r.Key)
                        .ToList() ?? new List<string>();
        }

        public static List<RoomData> GetAllRoomData()
        {
            if (_rooms == null)
            {
                LoadRooms();
            }

            return _rooms?.Values.ToList() ?? new List<RoomData>();
        }

        public static Environment CreateRoom(string roomName, string dungeonTheme, bool? overrideIsHostile = null)
        {
            var roomData = GetRoomData(roomName);
            if (roomData != null)
            {
                // Use override value if provided, otherwise use JSON value
                bool isHostile = overrideIsHostile ?? roomData.IsHostile;
                
                var room = new Environment(
                    name: roomData.Name,
                    description: roomData.Description,
                    isHostile: isHostile,
                    theme: dungeonTheme
                );

                // Add room-specific actions
                foreach (var actionData in roomData.Actions)
                {
                    var action = ActionLoader.GetAction(actionData.Name);
                    if (action != null)
                    {
                        room.AddAction(action, actionData.Weight);
                    }
                }

                return room;
            }

            // Fallback to default room if not found
            return new Environment(
                name: roomName,
                description: "A mysterious room with an unknown purpose.",
                isHostile: overrideIsHostile ?? true,
                theme: dungeonTheme
            );
        }
    }
} 