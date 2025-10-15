using System;
using System.Collections.Generic;
using System.Text.Json;

namespace RPGGame
{
    /// <summary>
    /// Comprehensive UI configuration system for easily adjusting combat log display, delays, and spacing
    /// </summary>
    public class UIConfiguration
    {
        // Display Settings
        public bool EnableDelays { get; set; } = true;
        public bool FastCombat { get; set; } = false;
        public bool DisableAllOutput { get; set; } = false;
        
        // Spacing Configuration
        public bool AddBlankLinesBetweenEntities { get; set; } = true;
        public bool AddBlankLinesAfterDamageOverTime { get; set; } = true;
        public bool AddBlankLinesAfterStunMessages { get; set; } = true;
        
        // Block Spacing Configuration
        public BlockSpacingConfiguration BlockSpacing { get; set; } = new BlockSpacingConfiguration();
        
        // Debug Configuration
        public bool ShowTimingInfo { get; set; } = false;
        public bool ShowEntityNames { get; set; } = true;
        public bool ShowDetailedCombatInfo { get; set; } = true;
        
        // Beat-based Timing System
        public BeatTimingConfiguration BeatTiming { get; set; } = new BeatTimingConfiguration();
        
        // Color System Configuration
        /// <summary>
        /// Controls the intensity of warm/cool white temperature gradients
        /// 0.0 = no temperature shift (pure white), 1.0 = default intensity, 2.0 = extreme temperature shift
        /// Warm white: yellowish tint for early dungeon; Cool white: bluish tint for deep dungeon
        /// </summary>
        public double WhiteTemperatureIntensity { get; set; } = 1.0;
        
        // Animation Configuration
        /// <summary>
        /// Controls the speed of the undulation/shimmer effect on colored text (in milliseconds)
        /// Lower values = faster shimmer, higher values = slower, more subtle shimmer
        /// Must be set in UIConfiguration.json
        /// </summary>
        public int UndulationTimerMs { get; set; }
        
        /// <summary>
        /// Configuration for the moving brightness mask effect
        /// </summary>
        public BrightnessMaskConfig BrightnessMask { get; set; } = new BrightnessMaskConfig();
        
        
        /// <summary>
        /// Loads configuration from a JSON file
        /// Uses the centralized GameData path finding from GameConstants
        /// This ensures all builds (Debug, Release, dist) reference the same GameData folder
        /// </summary>
        /// <param name="fileName">The configuration file name</param>
        /// <returns>Loaded configuration or default if file doesn't exist</returns>
        public static UIConfiguration LoadFromFile(string fileName = "UIConfiguration.json")
        {
            try
            {
                // Use the same path-finding logic as all other game data files
                string? foundPath = JsonLoader.FindGameDataFile(fileName);
                
                if (foundPath != null && System.IO.File.Exists(foundPath))
                {
                    string json = System.IO.File.ReadAllText(foundPath);
                    
                    var config = JsonSerializer.Deserialize<UIConfiguration>(json, new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true,
                        WriteIndented = true,
                        ReadCommentHandling = JsonCommentHandling.Skip,
                        AllowTrailingCommas = true
                    });
                    
                    return config ?? new UIConfiguration();
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine($"[DEBUG] UIConfiguration.json not found in GameData, using defaults. WhiteTemperatureIntensity will be: 1.0 (from C# default)");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Warning: Could not load UI configuration: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"[DEBUG] Error loading config: {ex.Message}");
            }
            
            return new UIConfiguration();
        }
        
        /// <summary>
        /// Saves configuration to a JSON file
        /// </summary>
        /// <param name="filePath">Path to save the configuration file</param>
        public void SaveToFile(string filePath = "GameData/UIConfiguration.json")
        {
            try
            {
                string json = JsonSerializer.Serialize(this, new JsonSerializerOptions
                {
                    WriteIndented = true,
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                });
                System.IO.File.WriteAllText(filePath, json);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Warning: Could not save UI configuration to {filePath}: {ex.Message}");
            }
        }
        
        /// <summary>
        /// Creates a preset configuration by loading base config from JSON and applying preset overrides
        /// </summary>
        /// <param name="preset">The preset to create</param>
        /// <returns>Configuration with preset values</returns>
        public static UIConfiguration CreatePreset(UIConfigurationPreset preset)
        {
            // Load base configuration from JSON file first
            var config = LoadFromFile();
            
            switch (preset)
            {
                case UIConfigurationPreset.Fast:
                    config.EnableDelays = false;
                    config.FastCombat = true;
                    config.BeatTiming.CombatBeatLengthMs = 0; // No delays
                    config.BeatTiming.MenuBeatLengthMs = 10; // Very fast menus
                    config.BeatTiming.BeatMultipliers.Combat = 0;
                    config.BeatTiming.BeatMultipliers.System = 0;
                    config.BeatTiming.BeatMultipliers.Environmental = 0;
                    config.BeatTiming.BeatMultipliers.EffectMessage = 0;
                    config.BeatTiming.BeatMultipliers.DamageOverTime = 0;
                    config.BeatTiming.BeatMultipliers.Title = 0;
                    config.BeatTiming.BeatMultipliers.MainTitle = 0;
                    config.BeatTiming.BeatMultipliers.RollInfo = 0;
                    config.AddBlankLinesBetweenEntities = false;
                    break;
                    
                case UIConfigurationPreset.Cinematic:
                    // Use values from JSON file as base, only override specific cinematic settings
                    config.EnableDelays = true;
                    config.FastCombat = false;
                    // Note: BeatTiming values are loaded from JSON file
                    config.AddBlankLinesBetweenEntities = true;
                    break;
                    
                case UIConfigurationPreset.Balanced:
                    // Use default values from JSON file
                    break;
                    
                case UIConfigurationPreset.Debug:
                    config.EnableDelays = false;
                    config.FastCombat = true;
                    config.BeatTiming.CombatBeatLengthMs = 0; // No delays for debugging
                    config.BeatTiming.MenuBeatLengthMs = 0; // Instant menus
                    config.BeatTiming.BeatMultipliers.Combat = 0;
                    config.BeatTiming.BeatMultipliers.System = 0;
                    config.BeatTiming.BeatMultipliers.Environmental = 0;
                    config.BeatTiming.BeatMultipliers.EffectMessage = 0;
                    config.BeatTiming.BeatMultipliers.DamageOverTime = 0;
                    config.BeatTiming.BeatMultipliers.Title = 0;
                    config.BeatTiming.BeatMultipliers.MainTitle = 0;
                    config.BeatTiming.BeatMultipliers.Encounter = 0;
                    config.BeatTiming.BeatMultipliers.RollInfo = 0;
                    break;
                    
                case UIConfigurationPreset.Snappy:
                    config.EnableDelays = true;
                    config.FastCombat = false;
                    config.BeatTiming.CombatBeatLengthMs = 50; // Quick beats
                    config.BeatTiming.MenuBeatLengthMs = 15; // Fast menus
                    config.BeatTiming.BeatMultipliers.Combat = 1.0; // 1 beat = 50ms
                    config.BeatTiming.BeatMultipliers.System = 0.8; // 0.8 beats = 40ms
                    config.BeatTiming.BeatMultipliers.Environmental = 1.2; // 1.2 beats = 60ms
                    config.BeatTiming.BeatMultipliers.EffectMessage = 0.4; // 0.4 beats = 20ms
                    config.BeatTiming.BeatMultipliers.DamageOverTime = 0.4; // 0.4 beats = 20ms
                    config.BeatTiming.BeatMultipliers.Title = 2.0; // 2 beats = 100ms
                    config.BeatTiming.BeatMultipliers.MainTitle = 4.0; // 4 beats = 200ms
                    config.BeatTiming.BeatMultipliers.Encounter = 0.67; // 0.67 beats = 33.5ms
                    config.BeatTiming.BeatMultipliers.RollInfo = 0.05; // 0.05 beats = 2.5ms
                    break;
                    
                case UIConfigurationPreset.Relaxed:
                    config.EnableDelays = true;
                    config.FastCombat = false;
                    config.BeatTiming.CombatBeatLengthMs = 150; // Slower beats
                    config.BeatTiming.MenuBeatLengthMs = 50; // Moderate menus
                    config.BeatTiming.BeatMultipliers.Combat = 1.0; // 1 beat = 150ms
                    config.BeatTiming.BeatMultipliers.System = 1.0; // 1 beat = 150ms
                    config.BeatTiming.BeatMultipliers.Environmental = 2.0; // 2 beats = 300ms
                    config.BeatTiming.BeatMultipliers.EffectMessage = 0.7; // 0.7 beats = 105ms
                    config.BeatTiming.BeatMultipliers.DamageOverTime = 0.7; // 0.7 beats = 105ms
                    config.BeatTiming.BeatMultipliers.Title = 5.0; // 5 beats = 750ms
                    config.BeatTiming.BeatMultipliers.MainTitle = 10.0; // 10 beats = 1500ms
                    config.BeatTiming.BeatMultipliers.Encounter = 0.67; // 0.67 beats = 100.5ms
                    config.BeatTiming.BeatMultipliers.RollInfo = 0.2; // 0.2 beats = 30ms
                    break;
                    
                case UIConfigurationPreset.Instant:
                    config.EnableDelays = false;
                    config.FastCombat = true;
                    config.BeatTiming.CombatBeatLengthMs = 0; // No delays
                    config.BeatTiming.MenuBeatLengthMs = 0; // Instant menus
                    config.BeatTiming.BeatMultipliers.Combat = 0;
                    config.BeatTiming.BeatMultipliers.System = 0;
                    config.BeatTiming.BeatMultipliers.Environmental = 0;
                    config.BeatTiming.BeatMultipliers.EffectMessage = 0;
                    config.BeatTiming.BeatMultipliers.DamageOverTime = 0;
                    config.BeatTiming.BeatMultipliers.Title = 0;
                    config.BeatTiming.BeatMultipliers.MainTitle = 0;
                    config.BeatTiming.BeatMultipliers.Encounter = 0;
                    config.BeatTiming.BeatMultipliers.RollInfo = 0;
                    config.AddBlankLinesBetweenEntities = false;
                    break;
            }
            
            return config;
        }
        
        /// <summary>
        /// Gets the effective delay for a specific message type using dual beat-based timing
        /// </summary>
        /// <param name="messageType">Type of message</param>
        /// <returns>Delay in milliseconds</returns>
        public int GetEffectiveDelay(UIMessageType messageType)
        {
            if (!EnableDelays) return 0;
            if (FastCombat && messageType == UIMessageType.Combat) return 0;
            
            // Get the beat multiplier for this message type
            double beatMultiplier = BeatTiming.GetBeatMultiplier(messageType);
            
            // Get the appropriate base beat length (combat or menu beat)
            int baseBeatLength = BeatTiming.GetBaseBeatLength(messageType);
            
            // Calculate delay: beatMultiplier * appropriate base beat length
            int delay = (int)(beatMultiplier * baseBeatLength);
            
            return delay;
        }
    }
    
    /// <summary>
    /// Beat-based timing configuration system with two core beats
    /// </summary>
    public class BeatTimingConfiguration
    {
        /// <summary>
        /// Combat beat length in milliseconds - used for combat, environmental, effects, and damage over time
        /// </summary>
        public int CombatBeatLengthMs { get; set; } = 1500;
        
        /// <summary>
        /// Menu beat length in milliseconds - used for system messages and titles
        /// </summary>
        public int MenuBeatLengthMs { get; set; } = 20;
        
        /// <summary>
        /// Beat multipliers for different message types
        /// </summary>
        public BeatMultipliers BeatMultipliers { get; set; } = new BeatMultipliers();
        
        /// <summary>
        /// Block-based delays for the new display system
        /// </summary>
        public Dictionary<string, double> BlockDelays { get; set; } = new Dictionary<string, double>();
        
        /// <summary>
        /// Legacy multipliers for backward compatibility
        /// </summary>
        public LegacyMultipliers LegacyMultipliers { get; set; } = new LegacyMultipliers();
        
        /// <summary>
        /// Gets the beat multiplier for a specific message type
        /// </summary>
        /// <param name="messageType">Type of message</param>
        /// <returns>Beat multiplier (e.g., 1.0 = 1 beat, 0.5 = half beat, 2.0 = 2 beats)</returns>
        public double GetBeatMultiplier(UIMessageType messageType)
        {
            return messageType switch
            {
                UIMessageType.Combat => BeatMultipliers.Combat,
                UIMessageType.Menu => 0, // Menu uses MenuSpeedMs instead
                UIMessageType.System => BeatMultipliers.System,
                UIMessageType.Title => BeatMultipliers.Title,
                UIMessageType.MainTitle => BeatMultipliers.MainTitle,
                UIMessageType.Environmental => BeatMultipliers.Environmental,
                UIMessageType.EffectMessage => BeatMultipliers.EffectMessage,
                UIMessageType.DamageOverTime => BeatMultipliers.DamageOverTime,
                UIMessageType.RollInfo => BeatMultipliers.RollInfo, // Use JSON configuration
                UIMessageType.Encounter => BeatMultipliers.Encounter,
                _ => 0
            };
        }
        
        /// <summary>
        /// Gets the appropriate base beat length for a message type
        /// </summary>
        /// <param name="messageType">Type of message</param>
        /// <returns>Base beat length in milliseconds</returns>
        public int GetBaseBeatLength(UIMessageType messageType)
        {
            return messageType switch
            {
                UIMessageType.Combat => CombatBeatLengthMs,
                UIMessageType.Environmental => CombatBeatLengthMs,
                UIMessageType.EffectMessage => CombatBeatLengthMs,
                UIMessageType.DamageOverTime => CombatBeatLengthMs,
                UIMessageType.RollInfo => CombatBeatLengthMs,
                UIMessageType.Encounter => CombatBeatLengthMs,
                UIMessageType.System => MenuBeatLengthMs,
                UIMessageType.Title => MenuBeatLengthMs,
                UIMessageType.MainTitle => MenuBeatLengthMs,
                UIMessageType.Menu => MenuBeatLengthMs,
                _ => CombatBeatLengthMs
            };
        }
        
        /// <summary>
        /// Gets the effective menu delay (independent of beat system)
        /// </summary>
        /// <returns>Menu delay in milliseconds</returns>
        public int GetMenuDelay()
        {
            return MenuBeatLengthMs;
        }
    }
    
    /// <summary>
    /// Beat multipliers for different message types
    /// </summary>
    public class BeatMultipliers
    {
        /// <summary>
        /// Combat actions - 1 beat (standard timing)
        /// </summary>
        public double Combat { get; set; } = 1.0;
        
        /// <summary>
        /// System messages - 1 beat (same as combat)
        /// </summary>
        public double System { get; set; } = 1.0;
        
        /// <summary>
        /// Environmental actions - 1.5 beats (slightly longer for dramatic effect)
        /// </summary>
        public double Environmental { get; set; } = 1.5;
        
        /// <summary>
        /// Status effect messages (stun, poison, bleed, etc.) - 0.5 beats (quick, snappy)
        /// </summary>
        public double EffectMessage { get; set; } = 0.5;
        
        /// <summary>
        /// Damage over time - 0.5 beats (quick, like stun)
        /// </summary>
        public double DamageOverTime { get; set; } = 0.5;
        
        /// <summary>
        /// Title screens - 10 beats (dramatic, long pause)
        /// </summary>
        public double Title { get; set; } = 10.0;
        
        /// <summary>
        /// Main title screens - 20 beats (extra dramatic pause for main game title)
        /// </summary>
        public double MainTitle { get; set; } = 20.0;
        
        /// <summary>
        /// Encounter messages - 0.67 beats (dramatic pause after encountering enemies)
        /// </summary>
        public double Encounter { get; set; } = 0.67;
        
        /// <summary>
        /// Roll information messages - 0.10 beats (quick display of roll details)
        /// </summary>
        public double RollInfo { get; set; } = 0.10;
    }
    
    /// <summary>
    /// Legacy multipliers for backward compatibility with older configurations
    /// These are alternative timing values that can be loaded from JSON
    /// </summary>
    public class LegacyMultipliers
    {
        public double Title { get; set; } = 15.0;
        public double MainTitle { get; set; } = 20.0;
        public double System { get; set; } = 1.0;
        public double Combat { get; set; } = 0.5;
        public double Environmental { get; set; } = 1.5;
        public double EffectMessage { get; set; } = 0.4;
        public double DamageOverTime { get; set; } = 0.4;
        public double RollInfo { get; set; } = 1.0;
        public double Encounter { get; set; } = 2.0;
    }
    
    /// <summary>
    /// Preset configurations for common use cases
    /// </summary>
    public enum UIConfigurationPreset
    {
        Balanced,
        Fast,
        Cinematic,
        Debug,
        Snappy,      // Quick but not instant
        Relaxed,     // Slower, more deliberate
        Instant      // No delays at all
    }
    
    /// <summary>
    /// Types of UI messages for delay configuration
    /// </summary>
    public enum UIMessageType
    {
        Combat,
        Menu,
        System,
        Title,
        MainTitle,
        Environmental,
        EffectMessage,
        DamageOverTime,
        RollInfo,
        Encounter
    }
    
    /// <summary>
    /// Configuration for block spacing in the new display system
    /// </summary>
    public class BlockSpacingConfiguration
    {
        /// <summary>
        /// Add blank lines between action blocks
        /// </summary>
        public bool AddBlankLinesBetweenActionBlocks { get; set; } = true;
        
        /// <summary>
        /// Add blank lines between effect blocks
        /// </summary>
        public bool AddBlankLinesBetweenEffectBlocks { get; set; } = true;
        
        /// <summary>
        /// Add blank lines around narrative blocks
        /// </summary>
        public bool AddBlankLinesAroundNarrativeBlocks { get; set; } = true;
    }
    
    /// <summary>
    /// Configuration for the moving brightness mask effect
    /// </summary>
    public class BrightnessMaskConfig
    {
        /// <summary>
        /// Enable or disable the brightness mask effect
        /// </summary>
        public bool Enabled { get; set; }
        
        /// <summary>
        /// Maximum brightness adjustment (+/- this value in percent)
        /// Must be set in UIConfiguration.json
        /// </summary>
        public float Intensity { get; set; }
        
        /// <summary>
        /// Length of the wave pattern (higher = slower, more gradual changes)
        /// Must be set in UIConfiguration.json
        /// </summary>
        public float WaveLength { get; set; }
        
        /// <summary>
        /// How often the brightness mask moves (in milliseconds)
        /// Must be set in UIConfiguration.json
        /// </summary>
        public int UpdateIntervalMs { get; set; }
    }
}
