using System;

namespace RPGGame.Utils
{
    /// <summary>
    /// Game-specific constants to replace magic numbers throughout the codebase
    /// Improves maintainability and reduces the risk of inconsistent values
    /// </summary>
    public static class GameConstants
    {
        // Combat System Constants
        public const int CRITICAL_HIT_THRESHOLD = 20;
        public const double CRITICAL_HIT_MULTIPLIER = 1.3;
        public const int MINIMUM_DAMAGE = 1;
        public const int MAXIMUM_DAMAGE_CAP = 999;
        
        // Action System Constants
        public const double DEFAULT_ACTION_LENGTH = 1.0;
        public const int MAX_ACTION_COOLDOWN = 10;
        public const int MIN_ACTION_COOLDOWN = 0;
        
        // Character System Constants
        public const int MAX_CHARACTER_LEVEL = 100;
        public const int MIN_CHARACTER_LEVEL = 1;
        public const int DEFAULT_CHARACTER_LEVEL = 1;
        public const int MAX_ATTRIBUTE_VALUE = 100;
        public const int MIN_ATTRIBUTE_VALUE = 1;
        public const int DEFAULT_ATTRIBUTE_VALUE = 8;
        
        // Health System Constants
        public const int MAX_HEALTH_CAP = 9999;
        public const int MIN_HEALTH_VALUE = 1;
        public const int DEFAULT_HEALTH_VALUE = 200;
        public const int HEALTH_PER_LEVEL = 5;
        
        // Experience System Constants
        public const int MAX_EXPERIENCE_CAP = 999999;
        public const int MIN_EXPERIENCE_VALUE = 0;
        public const double EXPERIENCE_SCALING_FACTOR = 1.5;
        
        // Equipment System Constants
        public const int MAX_EQUIPMENT_TIER = 5;
        public const int MIN_EQUIPMENT_TIER = 1;
        public const int MAX_INVENTORY_SLOTS = 50;
        public const int MIN_INVENTORY_SLOTS = 10;
        
        // Dungeon System Constants
        public const int MAX_DUNGEON_LEVEL = 50;
        public const int MIN_DUNGEON_LEVEL = 1;
        public const int MAX_ROOMS_PER_DUNGEON = 20;
        public const int MIN_ROOMS_PER_DUNGEON = 3;
        public const double ROOM_COUNT_SCALING = 0.5;
        
        // Enemy System Constants
        public const int MAX_ENEMY_LEVEL = 50;
        public const int MIN_ENEMY_LEVEL = 1;
        public const int MAX_ENEMIES_PER_ROOM = 5;
        public const int MIN_ENEMIES_PER_ROOM = 1;
        
        // Loot System Constants
        public const double BASE_DROP_CHANCE = 0.3;
        public const double MAX_DROP_CHANCE = 0.8;
        public const double MAGIC_FIND_EFFECTIVENESS = 0.01;
        public const int MAX_LOOT_ITEMS = 10;
        
        // UI System Constants
        public const int MAX_TEXT_LENGTH = 1000;
        public const int MIN_TEXT_LENGTH = 1;
        public const int DEFAULT_TEXT_DELAY_MS = 50;
        public const int MAX_TEXT_DELAY_MS = 1000;
        public const int MIN_TEXT_DELAY_MS = 0;
        
        // Animation System Constants
        public const int DEFAULT_ANIMATION_DURATION_MS = 500;
        public const int MAX_ANIMATION_DURATION_MS = 5000;
        public const int MIN_ANIMATION_DURATION_MS = 100;
        
        // Random System Constants
        public const int RANDOM_SEED_DEFAULT = 12345;
        public const int MAX_RANDOM_VALUE = 100;
        public const int MIN_RANDOM_VALUE = 1;
        
        // File System Constants
        public const int MAX_FILE_SIZE_MB = 100;
        public const int MAX_FILENAME_LENGTH = 255;
        public const int MIN_FILENAME_LENGTH = 1;
        
        // Performance Constants
        public const int MAX_CACHE_SIZE = 1000;
        public const int DEFAULT_CACHE_SIZE = 100;
        public const int MAX_MEMORY_USAGE_MB = 500;
        
        // Validation Constants
        public const int MAX_NAME_LENGTH = 50;
        public const int MIN_NAME_LENGTH = 1;
        public const int MAX_DESCRIPTION_LENGTH = 500;
        public const int MIN_DESCRIPTION_LENGTH = 1;
        
        // Combat Balance Constants
        public const double ARMOR_REDUCTION_FACTOR = 100.0;
        public const double ATTACK_SPEED_BASE = 1.0;
        public const double COMBO_AMPLIFIER_BASE = 1.01;
        public const double COMBO_AMPLIFIER_MAX = 2.0;
        public const int COMBO_AMPLIFIER_MAX_TECH = 20;
        
        // Status Effect Constants
        public const int MAX_STATUS_EFFECT_DURATION = 20;
        public const int MIN_STATUS_EFFECT_DURATION = 1;
        public const int MAX_STATUS_EFFECT_STACKS = 10;
        public const int MIN_STATUS_EFFECT_STACKS = 1;
        
        // Save System Constants
        public const int MAX_SAVE_FILES = 10;
        public const int MAX_SAVE_FILE_SIZE_MB = 10;
        public const string SAVE_FILE_EXTENSION = ".json";
        
        // Debug System Constants
        public const int MAX_LOG_ENTRIES = 10000;
        public const int MAX_LOG_MESSAGE_LENGTH = 1000;
        public const int LOG_RETENTION_DAYS = 30;
        
        // Network Constants (for future use)
        public const int DEFAULT_PORT = 8080;
        public const int MAX_CONNECTIONS = 100;
        public const int CONNECTION_TIMEOUT_MS = 30000;
        
        // Audio Constants (for future use)
        public const double DEFAULT_VOLUME = 0.5;
        public const double MAX_VOLUME = 1.0;
        public const double MIN_VOLUME = 0.0;
        public const int DEFAULT_SAMPLE_RATE = 44100;
        
        // Graphics Constants (for future use)
        public const int DEFAULT_SCREEN_WIDTH = 1920;
        public const int DEFAULT_SCREEN_HEIGHT = 1080;
        public const int MIN_SCREEN_WIDTH = 800;
        public const int MIN_SCREEN_HEIGHT = 600;
        public const int MAX_FPS = 120;
        public const int DEFAULT_FPS = 60;
        public const int MIN_FPS = 30;
    }
}
