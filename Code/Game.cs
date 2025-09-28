namespace RPGGame
{
    using System.Text.Json;

    // Data classes moved to separate files:
    // - StartingGearData, StartingWeapon, StartingArmor -> Code/StartingGear.cs
    // - DungeonConfig, DungeonGenerationConfig -> Code/DungeonConfig.cs
    // - Dungeon class -> Code/Dungeon.cs

    public class Game
    {
        private GameMenuManager menuManager;

        public Game()
        {
            // Start the game ticker
            GameTicker.Instance.Start();
            menuManager = new GameMenuManager();
        }

        public Game(Character existingCharacter)
        {
            var settings = GameSettings.Instance;
            
            if (settings.PlayerHealthMultiplier != 1.0)
            {
                existingCharacter.ApplyHealthMultiplier(settings.PlayerHealthMultiplier);
            }
            
            // Start the game ticker
            GameTicker.Instance.Start();
            menuManager = new GameMenuManager();
            
            // Initialize existing game
            var gameInitializer = new GameInitializer();
            var inventory = new List<Item>();
            var availableDungeons = new List<Dungeon>();
            gameInitializer.InitializeExistingGame(existingCharacter, availableDungeons);
        }

        // LoadStartingGear method moved to GameInitializer class

        public static DungeonConfig LoadDungeonConfig()
        {
            try
            {
                string jsonPath = Path.Combine("..", "GameData", "DungeonConfig.json");
                string jsonContent = File.ReadAllText(jsonPath);
                var config = JsonSerializer.Deserialize<DungeonConfig>(jsonContent) ?? new DungeonConfig();
                return config;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error loading dungeon config: {ex.Message}");
                // Return default config if loading fails
                return new DungeonConfig
                {
                    dungeonThemes = new List<string> { "Forest", "Lava", "Crypt", "Cavern", "Swamp", "Desert", "Ice", "Ruins", "Castle", "Graveyard" },
                    roomThemes = new List<string> { "Treasure", "Guard", "Trap", "Puzzle", "Rest", "Storage", "Library", "Armory", "Kitchen", "Dining", "Chamber", "Hall", "Vault", "Sanctum", "Grotto", "Catacomb", "Shrine", "Laboratory", "Observatory", "Throne" },
                    dungeonGeneration = new DungeonGenerationConfig()
                };
            }
        }

        // InitializeGame method moved to GameInitializer class

        // InitializeGameForExistingCharacter method moved to GameInitializer class

        public void ShowMainMenu()
        {
            menuManager.ShowMainMenu();
        }

        // StartNewGame, LoadAndRunGame, GetSavedCharacterInfo, ShowSettings, and Run methods moved to GameMenuManager class

        // ChooseDungeon and AwardLootAndXP methods moved to DungeonManager class
    }
} 