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
            Rooms.Clear();

            try
            {
                // Use TuningConfig for dungeon scaling
                var dungeonScaling = GameConfiguration.Instance.DungeonScaling;
                var dungeonConfig = GameCoordinator.GetDungeonGenerationConfig();
                
                // Calculate room count with fallback to ensure at least 1 room
                int roomCount = Math.Max(1, Math.Max(dungeonScaling.RoomCountBase, (int)Math.Ceiling(MinLevel * dungeonScaling.RoomCountPerLevel)));

                bool hasHostileRoom = false; // Track if we've generated at least one hostile room

            for (int i = 0; i < roomCount; i++)
            {
                Environment? room = null;
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
                    try
                    {
                        room = RoomGenerator.GenerateRoom(Theme, roomLevel, isHostile);
                    }
                    catch (Exception ex)
                    {
                        // Log error but continue with fallback
                        if (GameConfiguration.IsDebugEnabled)
                        {
                            System.Console.WriteLine($"RoomGenerator.GenerateRoom failed for {Theme} room {i}: {ex.Message}");
                        }
                        room = null; // Will trigger fallback below
                    }
                    
                    if (room == null)
                    {
                        // Fallback: create a basic room if RoomGenerator fails
                        room = new Environment(
                            name: $"{Theme} Room",
                            description: $"A {Theme.ToLower()} room.",
                            isHostile: isHostile,
                            theme: Theme
                        );
                    }

                    // Generate enemies with scaled levels and dungeon-specific enemy list
                    // Pass dungeon level bounds to ensure enemy levels stay within range
                    try
                    {
                        room.GenerateEnemies(roomLevel, PossibleEnemies, MinLevel, MaxLevel);
                    }
                    catch (Exception ex)
                    {
                        // Log error but continue - room will be added without enemies if needed
                        if (GameConfiguration.IsDebugEnabled)
                        {
                            System.Console.WriteLine($"GenerateEnemies failed for room {i}: {ex.Message}");
                        }
                    }
                    
                    Rooms.Add(room);
                }
                catch (Exception ex)
                {
                    // If room creation completely fails, try to create a minimal fallback room
                    if (GameConfiguration.IsDebugEnabled)
                    {
                        System.Console.WriteLine($"Room generation failed for room {i}: {ex.Message}");
                    }
                    
                    // Try to create a minimal fallback room
                    try
                    {
                        int roomLevel = random.Next(MinLevel, MaxLevel + 1);
                        var fallbackRoom = new Environment(
                            name: $"{Theme} Room {i + 1}",
                            description: $"A {Theme.ToLower()} room.",
                            isHostile: true,
                            theme: Theme
                        );
                        try
                        {
                            fallbackRoom.GenerateEnemies(roomLevel, PossibleEnemies, MinLevel, MaxLevel);
                        }
                        catch
                        {
                            // Room will be added even without enemies
                        }
                        Rooms.Add(fallbackRoom);
                        hasHostileRoom = true;
                    }
                    catch
                    {
                        // If even the fallback fails, continue to next room
                        // The final fallback at the end will ensure at least one room
                    }
                }
            }
            }
            catch (Exception)
            {
                // If initialization or generation fails completely, ensure we still create at least one room
            }
            
            // Final fallback: if no rooms were generated, create at least one
            if (Rooms.Count == 0)
            {
                try
                {
                    int roomLevel = random.Next(MinLevel, MaxLevel + 1);
                    var fallbackRoom = new Environment(
                        name: $"{Theme} Room",
                        description: $"A {Theme.ToLower()} room.",
                        isHostile: true,
                        theme: Theme
                    );
                    fallbackRoom.GenerateEnemies(roomLevel, PossibleEnemies, MinLevel, MaxLevel);
                    Rooms.Add(fallbackRoom);
                }
                catch (Exception)
                {
                    // Even the fallback failed - create a minimal room without enemies
                    var minimalRoom = new Environment(
                        name: $"{Theme} Room",
                        description: $"A {Theme.ToLower()} room.",
                        isHostile: true,
                        theme: Theme
                    );
                    Rooms.Add(minimalRoom);
                }
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

        /// <summary>
        /// Scales the dungeon level up by the specified amount
        /// Used for dynamic difficulty adjustment (e.g., when player one-shots enemies)
        /// </summary>
        /// <param name="levelIncrease">The amount to increase the dungeon level (default: 1)</param>
        public void ScaleLevelUp(int levelIncrease = 1)
        {
            MinLevel = Math.Min(MinLevel + levelIncrease, Utils.GameConstants.MAX_DUNGEON_LEVEL);
            MaxLevel = Math.Min(MaxLevel + levelIncrease, Utils.GameConstants.MAX_DUNGEON_LEVEL);
        }
    }
}
