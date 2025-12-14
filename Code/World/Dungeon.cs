namespace RPGGame
{
    public class Dungeon
    {
        public string Name { get; private set; }
        public int MinLevel { get; private set; }
        public int MaxLevel { get; private set; }
        public string Theme { get; private set; }
        public List<string> PossibleEnemies { get; private set; }
        public List<Environment> Rooms { get; private set; }
        public ColorOverride? ColorOverride { get; private set; }
        private Random random;

        public Dungeon(string name, int minLevel, int maxLevel, string theme, List<string>? possibleEnemies = null, ColorOverride? colorOverride = null)
        {
            random = new Random();
            Name = name;
            MinLevel = minLevel;
            MaxLevel = maxLevel;
            Theme = theme;
            PossibleEnemies = possibleEnemies ?? new List<string>();
            Rooms = new List<Environment>();
            ColorOverride = colorOverride;
        }

        public void Generate()
        {
            // Use TuningConfig for dungeon scaling
            var dungeonScaling = GameConfiguration.Instance.DungeonScaling;
            var dungeonConfig = GameCoordinator.GetDungeonGenerationConfig();
            
            int roomCount = Math.Max(dungeonScaling.RoomCountBase, (int)Math.Ceiling(MinLevel * dungeonScaling.RoomCountPerLevel));
            Rooms.Clear();

            bool hasHostileRoom = false; // Track if we've generated at least one hostile room

            for (int i = 0; i < roomCount; i++)
            {
                try
                {
                    // Determine room type and difficulty
                    // Ensure at least one room is hostile (force last room if none are hostile yet)
                    bool isHostile;
                    if (i == roomCount - 1 && !hasHostileRoom)
                    {
                        // Force the last room to be hostile if we haven't had any yet
                        isHostile = true;
                    }
                    else
                    {
                        isHostile = random.NextDouble() < dungeonConfig.hostileRoomChance;
                    }
                    
                    if (isHostile)
                    {
                        hasHostileRoom = true;
                    }
                    
                    int roomLevel = random.Next(MinLevel, MaxLevel + 1);

                    // Use RoomGenerator to create theme-appropriate rooms
                    var room = RoomGenerator.GenerateRoom(Theme, roomLevel, isHostile);
                    
                    if (room == null)
                    {
                        continue;
                    }

                    // Generate enemies with scaled levels and dungeon-specific enemy list
                    room.GenerateEnemies(roomLevel, PossibleEnemies);
                    Rooms.Add(room);
                }
                catch (Exception)
                {
                    // Continue generating other rooms even if one fails
                }
            }
            if (Rooms.Count == 0)
            {
            }
        }

        private string GetRoomTheme(int roomIndex, int totalRooms)
        {
            var dungeonConfig = GameCoordinator.GetDungeonGenerationConfig();
            
            if (roomIndex == totalRooms - 1) return dungeonConfig.bossRoomName;

            // The RoomGenerator will now handle theme-specific room selection
            // This method is kept for backward compatibility but the actual room generation
            // is now handled by RoomGenerator.GenerateRoom() which uses the JSON data
            return "Generated"; // Placeholder - actual room selection happens in RoomGenerator
        }

        private string? GetRoomDescription(string roomTheme, bool isHostile)
        {
            // Get theme-specific description from FlavorText
            string baseDescription = FlavorText.GenerateLocationDescription(Theme);
            
            // Add room-specific context based on both dungeon theme and room type
            string roomContext = FlavorText.GenerateRoomContext(Theme, roomTheme);
            
            // Add hostility context
            string hostilityContext = isHostile ? " Danger lurks in the shadows." : "\nIt seems safe... for now.";
            
            return baseDescription + roomContext + hostilityContext;
        }
    }
}
