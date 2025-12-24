namespace RPGGame
{
    using System;
    using System.Collections.Generic;
    using System.Threading.Tasks;

    /// <summary>
    /// Manages all game initialization including character creation, game loading, and setup.
    /// This manager centralizes initialization logic extracted from Game.cs.
    /// 
    /// Responsibilities:
    /// - Create new characters with weapon selection
    /// - Load saved characters from disk
    /// - Initialize game data (dungeons, items, etc.)
    /// - Apply game settings (health multipliers, etc.)
    /// - Generate starting dungeons
    /// - Handle character setup and configuration
    /// 
    /// Design: Wraps GameInitializer and provides enhanced functionality
    /// Usage: Use for all game initialization workflows
    /// </summary>
    public class GameInitializationManager
    {
        private GameInitializer gameInitializer;

        public GameInitializationManager()
        {
            gameInitializer = new GameInitializer();
        }

        /// <summary>
        /// Creates and initializes a new character with starting gear.
        /// This is the primary method for starting a new game.
        /// </summary>
        /// <param name="character">The character to initialize with starting gear.</param>
        /// <param name="weaponChoice">The weapon index to equip (0 for default).</param>
        /// <returns>True if initialization successful, false otherwise.</returns>
        public bool InitializeNewCharacter(Character character, int weaponChoice = 0)
        {
            try
            {
                if (character == null)
                {
                    UIManager.WriteSystemLine("Cannot initialize character: null reference");
                    return false;
                }

                var dungeons = new List<Dungeon>();
                gameInitializer.InitializeNewGame(character, dungeons, weaponChoice);
                return true;
            }
            catch (Exception ex)
            {
                UIManager.WriteSystemLine($"Error initializing character: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Loads a saved character from disk asynchronously.
        /// Used when player selects "Load Game" from menu.
        /// </summary>
        /// <returns>The loaded character, or null if no save exists or load fails.</returns>
        public async Task<Character?> LoadSavedCharacterAsync()
        {
            try
            {
                var savedCharacter = await Character.LoadCharacterAsync().ConfigureAwait(false);
                return savedCharacter;
            }
            catch (Exception ex)
            {
                UIManager.WriteSystemLine($"Error loading character: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Loads a saved character from disk synchronously.
        /// NOTE: This method is deprecated. Use LoadSavedCharacterAsync instead for proper async handling.
        /// This synchronous wrapper blocks the calling thread and should not be used in UI contexts.
        /// </summary>
        /// <returns>The loaded character, or null if no save exists or load fails.</returns>
        [Obsolete("Use LoadSavedCharacterAsync instead. This method blocks the calling thread and may freeze the UI.")]
        public Character? LoadSavedCharacter()
        {
            try
            {
                // For backward compatibility only - callers should migrate to async version
                // Using ConfigureAwait(false) to avoid deadlocks, but this still blocks
                var savedCharacter = Character.LoadCharacterAsync().ConfigureAwait(false).GetAwaiter().GetResult();
                return savedCharacter;
            }
            catch (Exception ex)
            {
                UIManager.WriteSystemLine($"Error loading character: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Initializes game data for an existing character.
        /// Generates dungeons, sets up inventory, applies configurations.
        /// </summary>
        /// <param name="character">The character to initialize for.</param>
        /// <param name="dungeons">List to populate with generated dungeons.</param>
        public void InitializeGameData(Character character, List<Dungeon> dungeons)
        {
            try
            {
                if (character == null)
                {
                    UIManager.WriteSystemLine("Cannot initialize game data: character is null");
                    return;
                }

                gameInitializer.InitializeExistingGame(character, dungeons);
            }
            catch (Exception ex)
            {
                UIManager.WriteSystemLine($"Error initializing game data: {ex.Message}");
            }
        }

        /// <summary>
        /// Applies health multiplier from game settings to character.
        /// This allows adjusting game difficulty through settings.
        /// </summary>
        /// <param name="character">The character to apply multiplier to.</param>
        public void ApplyHealthMultiplier(Character character)
        {
            try
            {
                if (character == null)
                {
                    UIManager.WriteSystemLine("Cannot apply health multiplier: character is null");
                    return;
                }

                var settings = GameSettings.Instance;
                if (settings.PlayerHealthMultiplier != 1.0)
                {
                    character.ApplyHealthMultiplier(settings.PlayerHealthMultiplier);
                }
            }
            catch (Exception ex)
            {
                UIManager.WriteSystemLine($"Error applying health multiplier: {ex.Message}");
            }
        }

        /// <summary>
        /// Applies all game settings to a character.
        /// Includes health multiplier and any other active settings.
        /// </summary>
        /// <param name="character">The character to apply settings to.</param>
        public void ApplyGameSettings(Character character)
        {
            try
            {
                if (character == null) return;

                ApplyHealthMultiplier(character);
                // Future: Add other setting applications here
            }
            catch (Exception ex)
            {
                UIManager.WriteSystemLine($"Error applying game settings: {ex.Message}");
            }
        }

        /// <summary>
        /// Gets the dungeon generation configuration for the game.
        /// This controls how dungeons are procedurally generated.
        /// </summary>
        /// <returns>Configuration object for dungeon generation.</returns>
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

        /// <summary>
        /// Gets theme-specific room names for all dungeon themes.
        /// Returns mapping of theme → list of possible room names.
        /// </summary>
        /// <returns>Dictionary mapping dungeon themes to room names.</returns>
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

        /// <summary>
        /// Validates that a character is properly initialized for gameplay.
        /// Checks for required fields and valid state.
        /// </summary>
        /// <param name="character">The character to validate.</param>
        /// <returns>True if character is valid for gameplay.</returns>
        public bool ValidateCharacter(Character character)
        {
            if (character == null)
            {
                UIManager.WriteSystemLine("Character validation failed: character is null");
                return false;
            }

            if (string.IsNullOrEmpty(character.Name))
            {
                UIManager.WriteSystemLine("Character validation failed: name is empty");
                return false;
            }

            if (character.CurrentHealth <= 0)
            {
                UIManager.WriteSystemLine("Character validation failed: health is zero or negative");
                return false;
            }

            if (character.Inventory == null)
            {
                UIManager.WriteSystemLine("Character validation failed: inventory is null");
                return false;
            }

            return true;
        }

        /// <summary>
        /// Gets a human-readable summary of initialization status.
        /// Useful for debugging and logging.
        /// </summary>
        /// <returns>Status string.</returns>
        public override string ToString()
        {
            return $"GameInitializationManager (wraps GameInitializer)";
        }
    }
}
