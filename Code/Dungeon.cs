namespace RPGGame
{
    public class Dungeon
    {
        public string Name { get; private set; }
        public int MinLevel { get; private set; }
        public int MaxLevel { get; private set; }
        public string Theme { get; private set; }
        public List<Environment> Rooms { get; private set; }
        private Random random;

        public Dungeon(string name, int minLevel, int maxLevel, string theme)
        {
            random = new Random();
            Name = name;
            MinLevel = minLevel;
            MaxLevel = maxLevel;
            Theme = theme;
            Rooms = new List<Environment>();
        }

        public void Generate()
        {
            // Use TuningConfig for dungeon scaling
            var dungeonScaling = GameConfiguration.Instance.DungeonScaling;
            var dungeonConfig = Game.LoadDungeonConfig();
            
            int roomCount = Math.Max(dungeonScaling.RoomCountBase, (int)Math.Ceiling(MinLevel * dungeonScaling.RoomCountPerLevel));
            Rooms.Clear();

            for (int i = 0; i < roomCount; i++)
            {
                // Determine room type and difficulty
                bool isHostile = random.NextDouble() < dungeonConfig.dungeonGeneration.hostileRoomChance;
                int roomLevel = random.Next(MinLevel, MaxLevel + 1);

                // Create room with appropriate theme
                string roomTheme = GetRoomTheme(i, roomCount);
                string? desc = GetRoomDescription(roomTheme, isHostile);
                string roomDesc = desc ?? "A mysterious room with an unknown purpose.";
                var room = new Environment(
                    name: $"{roomTheme} Room",
                    description: roomDesc,
                    isHostile: isHostile,
                    theme: Theme,
                    roomType: roomTheme
                );

                // Generate enemies with scaled levels
                room.GenerateEnemies(roomLevel);
                Rooms.Add(room);
            }
        }

        private string GetRoomTheme(int roomIndex, int totalRooms)
        {
            // Load dungeon config to get room themes
            var dungeonConfig = Game.LoadDungeonConfig();
            
            if (roomIndex == totalRooms - 1) return dungeonConfig.dungeonGeneration.bossRoomName;

            return dungeonConfig.roomThemes[random.Next(dungeonConfig.roomThemes.Count)];
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
