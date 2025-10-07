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


        public static Dictionary<string, List<string>> GetThemeSpecificRooms()
        {
            return new Dictionary<string, List<string>>
            {
                ["Forest"] = new List<string> { "Grove", "Thicket", "Canopy", "Meadow", "Wilderness", "Tree Hollow", "Sacred Grove" },
                ["Lava"] = new List<string> { "Magma Chamber", "Fire Pit", "Molten Pool", "Volcanic Vent", "Ash Field", "Ember Cave", "Inferno Hall" },
                ["Crypt"] = new List<string> { "Burial Chamber", "Tomb", "Mausoleum", "Necropolis", "Death Shrine", "Sarcophagus Room", "Undead Vault" },
                ["Cavern"] = new List<string> { "Crystal Cave", "Underground Lake", "Stalactite Hall", "Deep Tunnel", "Mining Shaft", "Cave System", "Underground River" },
                ["Swamp"] = new List<string> { "Bog", "Marsh", "Quagmire", "Fen", "Wetland", "Mud Pit", "Marshland" },
                ["Desert"] = new List<string> { "Sand Dune", "Oasis", "Mirage", "Sandstorm", "Dune Valley", "Desert Spring", "Sand Temple" },
                ["Ice"] = new List<string> { "Glacier", "Ice Cave", "Frozen Lake", "Blizzard", "Frost Field", "Ice Palace", "Frozen Tundra" },
                ["Ruins"] = new List<string> { "Crumbling Hall", "Broken Tower", "Fallen Temple", "Ancient Ruins", "Decayed Chamber", "Lost City", "Forgotten Shrine" },
                ["Castle"] = new List<string> { "Throne Room", "Great Hall", "Dungeon", "Tower", "Courtyard", "Royal Chamber", "Castle Keep" },
                ["Graveyard"] = new List<string> { "Cemetery", "Mausoleum", "Tomb", "Grave Site", "Burial Ground", "Death Garden", "Necropolis" },
                ["Crystal"] = new List<string> { "Crystal Chamber", "Prism Hall", "Geode Cave", "Crystal Garden", "Shard Room", "Crystal Palace", "Gem Vault" },
                ["Temple"] = new List<string> { "Sanctuary", "Altar Room", "Prayer Hall", "Sacred Chamber", "Divine Shrine", "Holy Sanctum", "Temple Vault" },
                ["Generic"] = new List<string> { "Common Room", "Storage", "Hallway", "Chamber", "Vault", "Guard Room", "Meeting Hall" },
                ["Shadow"] = new List<string> { "Shadow Realm", "Dark Chamber", "Void Space", "Shadow Garden", "Umbra Hall", "Dark Sanctum", "Shadow Vault" },
                ["Steampunk"] = new List<string> { "Steam Chamber", "Gear Room", "Clockwork Hall", "Mechanical Vault", "Steam Engine", "Cog Chamber", "Industrial Hall" },
                ["Astral"] = new List<string> { "Star Chamber", "Cosmic Hall", "Nebula Room", "Galaxy Vault", "Celestial Sanctum", "Astral Observatory", "Space Temple" },
                ["Underground"] = new List<string> { "Deep Tunnel", "Subterranean Hall", "Underground City", "Cave System", "Mining District", "Underground Lake", "Deep Chamber" },
                ["Storm"] = new List<string> { "Thunder Hall", "Lightning Chamber", "Storm Eye", "Tempest Room", "Hurricane Vault", "Wind Tunnel", "Storm Sanctum" },
                ["Nature"] = new List<string> { "Garden", "Grove", "Meadow", "Wilderness", "Natural Spring", "Flower Field", "Nature Shrine" },
                ["Arcane"] = new List<string> { "Study", "Library", "Laboratory", "Spell Chamber", "Magic Vault", "Arcane Sanctum", "Wizard Tower" },
                ["Volcano"] = new List<string> { "Magma Chamber", "Volcanic Vent", "Ash Field", "Lava Pool", "Volcanic Crater", "Fire Chamber", "Volcano Summit" },
                ["Ocean"] = new List<string> { "Underwater Chamber", "Coral Reef", "Deep Sea Vault", "Ocean Floor", "Sea Cave", "Abyssal Hall", "Ocean Depths" },
                ["Mountain"] = new List<string> { "Peak", "Summit", "Mountain Pass", "High Altitude", "Rocky Outcrop", "Mountain Cave", "Alpine Chamber" },
                ["Temporal"] = new List<string> { "Time Chamber", "Chronos Hall", "Temporal Rift", "Time Vault", "Echo Chamber", "Paradox Room", "Timeline Sanctum" },
                ["Dream"] = new List<string> { "Dreamscape", "Nightmare Realm", "Lucid Chamber", "Subconscious Hall", "Fantasy Vault", "Dream Sanctum", "Sleep Chamber" },
                ["Void"] = new List<string> { "Void Chamber", "Emptiness Hall", "Null Space", "Void Vault", "Nothingness Room", "Absence Chamber", "Void Sanctum" },
                ["Dimensional"] = new List<string> { "Dimension Rift", "Reality Chamber", "Multiverse Hall", "Space-Time Vault", "Quantum Room", "Dimensional Sanctum", "Rift Chamber" },
                ["Divine"] = new List<string> { "Heavenly Chamber", "Divine Hall", "Sacred Vault", "Celestial Sanctum", "Holy Chamber", "Divine Temple", "Eternal Hall" }
            };
        }

        public static DungeonGenerationConfig GetDungeonGenerationConfig()
        {
            return new DungeonGenerationConfig
            {
                minRooms = 2,
                roomCountScaling = 0.5,
                hostileRoomChance = 0.8,
                bossRoomName = "Boss"
            };
        }


        public void ShowMainMenu()
        {
            menuManager.ShowMainMenu();
        }

    }
} 