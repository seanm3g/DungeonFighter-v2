using System;

using System.Collections.Generic;

using System.IO;

using System.Linq;

using System.Text.Json;

using System.Text.Json.Serialization;

using RPGGame.Data;

using RPGGame.World.Tags;



namespace RPGGame

{

    public class RoomActionData

    {

        [JsonPropertyName("name")]

        public string Name { get; set; } = "";

        [JsonPropertyName("weight")]

        public double Weight { get; set; }

    }



    /// <summary>Weighted enemy template for a room / environment (<c>Rooms.json</c> <c>enemies</c> array).</summary>

    public class RoomEnemyData

    {

        [JsonPropertyName("name")]

        public string Name { get; set; } = "";

        [JsonPropertyName("weight")]

        public double Weight { get; set; }

    }



    /// <summary>Room / environment row from ENVIRONMENTS sheet → <c>Rooms.json</c>.</summary>

    public class RoomData

    {

        [JsonPropertyName("region")]

        public string Region { get; set; } = "";

        [JsonPropertyName("biome")]

        public string Biome { get; set; } = "";

        [JsonPropertyName("location")]

        public string Location { get; set; } = "";

        [JsonPropertyName("description")]

        public string Description { get; set; } = "";

        [JsonPropertyName("tags")]

        public List<string>? Tags { get; set; }

        [JsonPropertyName("actions")]

        public List<RoomActionData> Actions { get; set; } = new List<RoomActionData>();

        [JsonPropertyName("enemies")]

        public List<RoomEnemyData> Enemies { get; set; } = new List<RoomEnemyData>();



        /// <summary>Legacy <c>name</c> key; kept for older JSON until re-pulled from sheet.</summary>

        [JsonPropertyName("name")]

        public string Name { get; set; } = "";

        /// <summary>Legacy dungeon theme column; use <see cref="Biome"/> when authoring via sheet.</summary>

        [JsonPropertyName("theme")]

        public string Theme { get; set; } = "";

        [JsonPropertyName("isHostile")]

        public bool IsHostile { get; set; } = true;



        /// <summary>Display / lookup name: <c>location</c> from sheet, else legacy <c>name</c>.</summary>

        public string GetLocationKey() =>

            !string.IsNullOrWhiteSpace(Location) ? Location.Trim()

            : !string.IsNullOrWhiteSpace(Name) ? Name.Trim()

            : "";



        /// <summary>Biome used to match dungeon theme when picking catalog rooms.</summary>

        public string GetBiomeForThemeMatch() =>

            !string.IsNullOrWhiteSpace(Biome) ? Biome.Trim()

            : !string.IsNullOrWhiteSpace(Theme) ? Theme.Trim()

            : "";



        /// <summary>Empty biome means the room can appear in any dungeon theme.</summary>

        public bool HasUniversalBiome => string.IsNullOrWhiteSpace(GetBiomeForThemeMatch());



        /// <summary>True when biome is blank (anywhere) or matches <paramref name="dungeonTheme"/>.</summary>

        public bool MatchesDungeonTheme(string? dungeonTheme)

        {

            if (HasUniversalBiome)

                return true;

            if (string.IsNullOrWhiteSpace(dungeonTheme))

                return false;

            return string.Equals(GetBiomeForThemeMatch(), dungeonTheme.Trim(), StringComparison.OrdinalIgnoreCase);

        }

    }



    public static class RoomLoader

    {

        private static Dictionary<string, RoomData>? _rooms;

        private static readonly string[] PossibleRoomPaths = {

            Path.Combine("GameData", "Rooms.json"),

            Path.Combine("..", "GameData", "Rooms.json"),

            Path.Combine("..", "..", "GameData", "Rooms.json")

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



                    _rooms = new Dictionary<string, RoomData>(StringComparer.OrdinalIgnoreCase);

                    if (roomList != null)

                    {

                        foreach (var room in roomList)

                        {

                            NormalizeRoomAfterLoad(room);

                            var key = room.GetLocationKey();

                            if (!string.IsNullOrEmpty(key))

                                _rooms[key] = room;

                        }

                    }

                }

                else

                {

                    _rooms = new Dictionary<string, RoomData>(StringComparer.OrdinalIgnoreCase);

                }

            }

            catch (Exception ex)

            {

                try

                {

                    UIManager.WriteSystemLine($"Error loading rooms: {ex.Message}");

                    if (GameConfiguration.IsDebugEnabled)

                        UIManager.WriteSystemLine($"Stack trace: {ex.StackTrace}");

                }

                catch

                {

                    if (GameConfiguration.IsDebugEnabled)

                        System.Console.WriteLine($"Error loading rooms: {ex.Message}");

                }

                _rooms = new Dictionary<string, RoomData>(StringComparer.OrdinalIgnoreCase);

            }

        }



        internal static void NormalizeRoomAfterLoad(RoomData room)

        {

            if (string.IsNullOrWhiteSpace(room.Location) && !string.IsNullOrWhiteSpace(room.Name))

                room.Location = room.Name.Trim();

            if (string.IsNullOrWhiteSpace(room.Name) && !string.IsNullOrWhiteSpace(room.Location))

                room.Name = room.Location.Trim();

            if (string.IsNullOrWhiteSpace(room.Biome) && !string.IsNullOrWhiteSpace(room.Theme))

                room.Biome = room.Theme.Trim();

        }



        public static RoomData? GetRoomData(string roomName)

        {

            if (_rooms == null)

                LoadRooms();



            return _rooms?.GetValueOrDefault(roomName);

        }



        public static List<string> GetAllRoomNames()

        {

            if (_rooms == null)

                LoadRooms();



            return _rooms?.Keys.ToList() ?? new List<string>();

        }



        /// <summary>Catalog rooms for a theme: matching biome plus blank biome (any theme).</summary>

        public static List<string> GetRoomsByTheme(string theme)

        {

            if (_rooms == null)

                LoadRooms();



            if (string.IsNullOrWhiteSpace(theme))

                return new List<string>();



            return _rooms?

                .Where(r => r.Value.MatchesDungeonTheme(theme))

                .Select(r => r.Key)

                .ToList() ?? new List<string>();

        }



        public static List<string> GetRoomsByHostility(bool isHostile)

        {

            if (_rooms == null)

                LoadRooms();



            return _rooms?

                .Where(r => r.Value.IsHostile == isHostile)

                .Select(r => r.Key)

                .ToList() ?? new List<string>();

        }



        public static List<RoomData> GetAllRoomData()

        {

            if (_rooms == null)

                LoadRooms();



            return _rooms?.Values.ToList() ?? new List<RoomData>();

        }



        public static Environment CreateRoom(string roomName, string dungeonTheme, bool? overrideIsHostile = null)

        {

            var roomData = GetRoomData(roomName);

            if (roomData != null)

            {

                bool isHostile = overrideIsHostile ?? roomData.IsHostile;

                IReadOnlyList<RoomEnemyData>? enemyPool = roomData.Enemies is { Count: > 0 } ? roomData.Enemies : null;

                var displayName = roomData.GetLocationKey();



                var room = new Environment(

                    name: displayName,

                    description: roomData.Description,

                    isHostile: isHostile,

                    theme: dungeonTheme,

                    roomType: "",

                    roomEnemySpawnPool: enemyPool

                );



                foreach (var actionData in roomData.Actions)

                {

                    var action = ActionLoader.GetAction(actionData.Name);

                    if (action != null)

                        room.AddAction(action, actionData.Weight);

                }



                ApplyEnvironmentTags(room, roomData.Tags, dungeonTheme);

                return room;

            }



            var fallback = new Environment(

                name: roomName,

                description: "A mysterious room with an unknown purpose.",

                isHostile: overrideIsHostile ?? true,

                theme: dungeonTheme

            );

            ApplyEnvironmentTags(fallback, null, dungeonTheme);

            return fallback;

        }



        internal static void ApplyEnvironmentTags(Environment room, IEnumerable<string>? authoredTags, string dungeonTheme)

        {

            var normalized = GameDataTagHelper.NormalizeDistinct(authoredTags);

            if (normalized.Count > 0)

                room.SetTags(normalized);

            else

                room.SetTags(EnvironmentThemeTags.GetFallbackForTheme(dungeonTheme));

        }

    }

}


