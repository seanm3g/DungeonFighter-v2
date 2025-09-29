using System;
using System.Collections.Generic;
using System.Linq;

namespace RPGGame
{
    /// <summary>
    /// Handles room generation logic including theme selection, room type generation, and environment creation
    /// Extracted from Environment.cs to reduce complexity
    /// </summary>
    public static class RoomGenerator
    {
        private static readonly Random random = new Random();

        /// <summary>
        /// Generates a room environment based on dungeon theme and player level
        /// </summary>
        /// <param name="dungeonTheme">The theme of the dungeon</param>
        /// <param name="playerLevel">The player's level for scaling</param>
        /// <param name="isHostile">Whether the room should be hostile</param>
        /// <returns>A generated Environment object</returns>
        public static Environment GenerateRoom(string dungeonTheme, int playerLevel, bool isHostile = true)
        {
            string roomType = GenerateRoomType();
            string roomName = GenerateRoomName(dungeonTheme, roomType);
            string description = GenerateRoomDescription(dungeonTheme, roomType, isHostile);
            
            var environment = new Environment(roomName, description, isHostile, dungeonTheme, roomType);
            
            // Apply room-specific effects
            ApplyRoomEffects(environment, dungeonTheme, roomType, playerLevel);
            
            return environment;
        }

        /// <summary>
        /// Generates a random room type
        /// </summary>
        /// <returns>A room type string</returns>
        private static string GenerateRoomType()
        {
            var roomTypes = new[]
            {
                "Treasure", "Guard", "Trap", "Puzzle", "Rest",
                "Storage", "Library", "Armory", "Kitchen", "Dining",
                "Chamber", "Hall", "Vault", "Sanctum", "Grotto",
                "Catacomb", "Shrine", "Laboratory", "Observatory", "Throne"
            };
            
            return roomTypes[random.Next(roomTypes.Length)];
        }

        /// <summary>
        /// Generates a room name based on theme and type
        /// </summary>
        /// <param name="theme">The dungeon theme</param>
        /// <param name="roomType">The room type</param>
        /// <returns>A formatted room name</returns>
        private static string GenerateRoomName(string theme, string roomType)
        {
            var themeAdjectives = GetThemeAdjectives(theme);
            var adjective = themeAdjectives[random.Next(themeAdjectives.Length)];
            
            return $"{adjective} {roomType}";
        }

        /// <summary>
        /// Generates a room description based on theme, type, and hostility
        /// </summary>
        /// <param name="theme">The dungeon theme</param>
        /// <param name="roomType">The room type</param>
        /// <param name="isHostile">Whether the room is hostile</param>
        /// <returns>A descriptive text for the room</returns>
        private static string GenerateRoomDescription(string theme, string roomType, bool isHostile)
        {
            var descriptions = GetRoomDescriptions(theme, roomType, isHostile);
            return descriptions[random.Next(descriptions.Length)];
        }

        /// <summary>
        /// Applies room-specific effects based on theme and type
        /// </summary>
        /// <param name="environment">The environment to apply effects to</param>
        /// <param name="theme">The dungeon theme</param>
        /// <param name="roomType">The room type</param>
        /// <param name="playerLevel">The player's level for scaling</param>
        private static void ApplyRoomEffects(Environment environment, string theme, string roomType, int playerLevel)
        {
            // Apply theme-based effects
            ApplyThemeEffects(environment, theme);
            
            // Apply room type effects
            ApplyRoomTypeEffects(environment, roomType, playerLevel);
        }

        /// <summary>
        /// Applies theme-based environmental effects
        /// </summary>
        /// <param name="environment">The environment to modify</param>
        /// <param name="theme">The dungeon theme</param>
        private static void ApplyThemeEffects(Environment environment, string theme)
        {
            switch (theme.ToLower())
            {
                case "lava":
                    // Lava rooms might have fire damage or heat effects
                    environment.PassiveEffectType = PassiveEffectType.DamageMultiplier;
                    environment.PassiveEffectValue = 1.1; // 10% more damage
                    break;
                case "ice":
                    // Ice rooms might slow down attacks
                    environment.PassiveEffectType = PassiveEffectType.SpeedMultiplier;
                    environment.PassiveEffectValue = 0.9; // 10% slower attacks
                    break;
                case "crypt":
                    // Crypt rooms might have undead effects
                    environment.PassiveEffectType = PassiveEffectType.DamageMultiplier;
                    environment.PassiveEffectValue = 0.95; // 5% less damage
                    break;
                // Add more theme effects as needed
            }
        }

        /// <summary>
        /// Applies room type-specific effects
        /// </summary>
        /// <param name="environment">The environment to modify</param>
        /// <param name="roomType">The room type</param>
        /// <param name="playerLevel">The player's level for scaling</param>
        private static void ApplyRoomTypeEffects(Environment environment, string roomType, int playerLevel)
        {
            switch (roomType.ToLower())
            {
                case "rest":
                    // Rest rooms might provide healing or buffs
                    environment.PassiveEffectType = PassiveEffectType.DamageMultiplier;
                    environment.PassiveEffectValue = 1.05; // 5% more damage (rested)
                    break;
                case "trap":
                    // Trap rooms might have negative effects
                    environment.PassiveEffectType = PassiveEffectType.SpeedMultiplier;
                    environment.PassiveEffectValue = 0.95; // 5% slower (cautious movement)
                    break;
                case "armory":
                    // Armory rooms might provide weapon bonuses
                    environment.PassiveEffectType = PassiveEffectType.DamageMultiplier;
                    environment.PassiveEffectValue = 1.08; // 8% more damage (better weapons)
                    break;
                // Add more room type effects as needed
            }
        }

        /// <summary>
        /// Gets theme-specific adjectives for room naming
        /// </summary>
        /// <param name="theme">The dungeon theme</param>
        /// <returns>Array of adjectives for the theme</returns>
        private static string[] GetThemeAdjectives(string theme)
        {
            return theme.ToLower() switch
            {
                "forest" => new[] { "Ancient", "Mossy", "Overgrown", "Shadowy", "Whispering" },
                "lava" => new[] { "Burning", "Molten", "Scorched", "Smoldering", "Volcanic" },
                "crypt" => new[] { "Dark", "Dusty", "Eerie", "Forgotten", "Silent" },
                "cavern" => new[] { "Deep", "Echoing", "Gloomy", "Stalactite", "Underground" },
                "swamp" => new[] { "Misty", "Muddy", "Murky", "Toxic", "Wet" },
                "desert" => new[] { "Arid", "Dusty", "Parched", "Sandy", "Sun-baked" },
                "ice" => new[] { "Frozen", "Glacial", "Icy", "Pristine", "Snow-covered" },
                "ruins" => new[] { "Crumbling", "Decayed", "Ruined", "Weathered", "Abandoned" },
                "castle" => new[] { "Grand", "Imposing", "Majestic", "Stately", "Noble" },
                "graveyard" => new[] { "Haunted", "Mournful", "Silent", "Weathered", "Sacred" },
                _ => new[] { "Mysterious", "Unknown", "Strange", "Hidden", "Secret" }
            };
        }

        /// <summary>
        /// Gets room descriptions based on theme, type, and hostility
        /// </summary>
        /// <param name="theme">The dungeon theme</param>
        /// <param name="roomType">The room type</param>
        /// <param name="isHostile">Whether the room is hostile</param>
        /// <returns>Array of descriptions</returns>
        private static string[] GetRoomDescriptions(string theme, string roomType, bool isHostile)
        {
            var baseDescriptions = new List<string>();
            
            // Add theme-specific descriptions
            baseDescriptions.AddRange(GetThemeDescriptions(theme));
            
            // Add room type-specific descriptions
            baseDescriptions.AddRange(GetRoomTypeDescriptions(roomType));
            
            // Add hostility-specific descriptions
            if (isHostile)
            {
                baseDescriptions.AddRange(GetHostileDescriptions());
            }
            else
            {
                baseDescriptions.AddRange(GetSafeDescriptions());
            }
            
            return baseDescriptions.ToArray();
        }

        /// <summary>
        /// Gets theme-specific room descriptions
        /// </summary>
        /// <param name="theme">The dungeon theme</param>
        /// <returns>Array of theme descriptions</returns>
        private static string[] GetThemeDescriptions(string theme)
        {
            return theme.ToLower() switch
            {
                "forest" => new[] { "Ancient trees tower overhead, their branches creating a natural canopy.", "The air is thick with the scent of earth and growing things." },
                "lava" => new[] { "Molten rock flows in rivers of fire, casting an eerie red glow.", "The heat is intense, making the air shimmer with distortion." },
                "crypt" => new[] { "Dust motes dance in the dim light filtering through cracks.", "The silence is broken only by the sound of your own footsteps." },
                "cavern" => new[] { "Stalactites hang like teeth from the ceiling above.", "The sound of dripping water echoes through the vast space." },
                "swamp" => new[] { "Mist rises from the murky water, obscuring your vision.", "The ground squelches underfoot with each step." },
                "desert" => new[] { "Sand drifts in the hot wind, creating shifting patterns.", "The sun beats down mercilessly on the barren landscape." },
                "ice" => new[] { "Crystals of ice form intricate patterns on every surface.", "Your breath forms clouds of vapor in the freezing air." },
                "ruins" => new[] { "Broken stones and crumbling walls tell of better times.", "Nature has begun to reclaim what civilization left behind." },
                "castle" => new[] { "High stone walls and ornate architecture speak of grandeur.", "Tapestries and banners hang from the walls, faded but still impressive." },
                "graveyard" => new[] { "Weathered tombstones stand in silent rows.", "The air carries the scent of earth and something more ancient." },
                _ => new[] { "The area has an otherworldly quality that defies description.", "Something about this place feels fundamentally wrong." }
            };
        }

        /// <summary>
        /// Gets room type-specific descriptions
        /// </summary>
        /// <param name="roomType">The room type</param>
        /// <returns>Array of room type descriptions</returns>
        private static string[] GetRoomTypeDescriptions(string roomType)
        {
            return roomType.ToLower() switch
            {
                "treasure" => new[] { "Gleaming treasures catch the light from every corner.", "The room is filled with the promise of riches." },
                "guard" => new[] { "This area appears to be heavily defended.", "Signs of recent occupation suggest active patrols." },
                "trap" => new[] { "Something about this place sets your nerves on edge.", "The floor and walls show signs of mechanical complexity." },
                "puzzle" => new[] { "Intricate mechanisms and symbols cover the walls.", "The room seems designed to test the mind as well as the body." },
                "rest" => new[] { "This area feels safe and welcoming.", "The atmosphere here is calm and peaceful." },
                "storage" => new[] { "Shelves and containers line the walls.", "The room is filled with supplies and equipment." },
                "library" => new[] { "Books and scrolls fill countless shelves.", "The air carries the scent of old parchment and ink." },
                "armory" => new[] { "Weapons and armor are displayed on racks and stands.", "The room is filled with the tools of war." },
                "kitchen" => new[] { "Cooking implements and ingredients are scattered about.", "The smell of food preparation lingers in the air." },
                "dining" => new[] { "Tables and chairs are arranged for communal meals.", "The room speaks of shared meals and conversation." },
                "chamber" => new[] { "This private space has an intimate feel.", "The room is designed for quiet contemplation." },
                "hall" => new[] { "The high ceiling and open space create an impressive atmosphere.", "This grand corridor stretches far into the distance." },
                "vault" => new[] { "Heavy doors and thick walls suggest great security.", "This room was built to protect something valuable." },
                "sanctum" => new[] { "The air here feels charged with spiritual energy.", "This sacred space radiates with ancient power." },
                "grotto" => new[] { "Natural formations create a cave-like atmosphere.", "The room feels like a hidden sanctuary." },
                "catacomb" => new[] { "Passageways branch off in multiple directions.", "The underground tunnels seem to go on forever." },
                "shrine" => new[] { "Religious symbols and offerings fill the space.", "The room is dedicated to some higher power." },
                "laboratory" => new[] { "Scientific equipment and alchemical apparatus fill the room.", "The air carries the scent of strange chemicals." },
                "observatory" => new[] { "Astronomical instruments point toward the heavens.", "The room is designed for studying the stars." },
                "throne" => new[] { "A seat of power dominates the center of the room.", "This space was clearly designed for ruling." },
                _ => new[] { "The purpose of this room is not immediately clear.", "This space seems to serve some unknown function." }
            };
        }

        /// <summary>
        /// Gets descriptions for hostile rooms
        /// </summary>
        /// <returns>Array of hostile descriptions</returns>
        private static string[] GetHostileDescriptions()
        {
            return new[]
            {
                "Danger lurks in the shadows.",
                "You sense hostile intent in the air.",
                "This place feels unwelcoming and threatening.",
                "Something here wants you dead.",
                "The atmosphere is thick with malice."
            };
        }

        /// <summary>
        /// Gets descriptions for safe rooms
        /// </summary>
        /// <returns>Array of safe descriptions</returns>
        private static string[] GetSafeDescriptions()
        {
            return new[]
            {
                "This area feels safe and secure.",
                "You can sense no immediate danger here.",
                "The atmosphere is calm and peaceful.",
                "This place offers a moment of respite.",
                "You feel protected within these walls."
            };
        }
    }
}
