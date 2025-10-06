namespace RPGGame
{
    using System.Text.Json;


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
                    dungeonThemes = new List<string> { "Forest", "Lava", "Crypt" }, // Minimal fallback
                    roomThemes = new List<string> { "Chamber", "Hall", "Vault" }, // Minimal fallback
                    dungeonGeneration = new DungeonGenerationConfig()
                };
            }
        }


        public void ShowMainMenu()
        {
            menuManager.ShowMainMenu();
        }

    }
} 