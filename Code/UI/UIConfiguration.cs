using System;
using System.Collections.Generic;
using System.Text.Json;
using RPGGame.UI.TextAnimation;

namespace RPGGame
{
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
        Encounter,

        /// <summary>
        /// Post-dungeon victory / outcome lines appended to the center log: center-justified like the rest of the completion summary.
        /// </summary>
        OutcomeSummary
    }

    /// <summary>
    /// Comprehensive UI configuration system for easily adjusting combat log display, delays, and spacing
    /// Refactored to use separate configuration classes for better maintainability
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
        
        // Block Spacing Configuration (merged from BlockSpacingConfiguration)
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
        
        /// <summary>
        /// Darkening factor for subsequent lines in action blocks (roll info, status effects, etc.)
        /// 0.0 = completely black, 1.0 = no darkening (same as primary line), 0.8 = 20% darker (default)
        /// Lower values = darker subsequent lines, higher values = brighter subsequent lines
        /// </summary>
        public double SubsequentLineDarkening { get; set; } = 0.8;

        /// <summary>
        /// Padding inside both the left and right edges of the center combat log, as a percent of the
        /// log's width (so it scales with the monitor/window). Hero lines start this far from the left edge
        /// and enemy lines end this far from the right edge, so the log reads like a chat thread.
        /// 0 = text runs edge to edge.
        /// </summary>
        public double CombatLogSidePaddingPercent { get; set; } = 0;

        // Animation Configuration
        /// <summary>
        /// [LEGACY/UNUSED] Controls the speed of the undulation/shimmer effect on colored text (in milliseconds)
        /// This property is not used in the current implementation.
        /// Active undulation configuration is in DungeonSelectionAnimation.UndulationIntervalMs
        /// </summary>
        public int UndulationTimerMs { get; set; }
        
        /// <summary>
        /// [LEGACY/UNUSED] Configuration for the moving brightness mask effect
        /// This property is not used in the current implementation.
        /// Active brightness mask configuration is in DungeonSelectionAnimation.BrightnessMask
        /// </summary>
        public BrightnessMaskConfig BrightnessMask { get; set; } = new BrightnessMaskConfig();
        
        /// <summary>
        /// Configuration for dungeon selection screen animations
        /// All parameters can be adjusted in UIConfiguration.json
        /// </summary>
        public DungeonSelectionAnimationConfig DungeonSelectionAnimation { get; set; } = new DungeonSelectionAnimationConfig();

        /// <summary>
        /// Per-cell MP4 overlay on the center combat-log band (glyph luminance → opacity).
        /// </summary>
        public RPGGame.UI.Avalonia.Effects.NarrativeVideoOverlayConfig NarrativeVideoOverlay { get; set; } =
            new RPGGame.UI.Avalonia.Effects.NarrativeVideoOverlayConfig();

        /// <summary>
        /// Combat-screen room plate look: brightness, contrast, saturation, blur, opacity, blend mode, base color.
        /// </summary>
        public RPGGame.UI.Avalonia.Effects.RoomBackdropConfig RoomBackdrop { get; set; } =
            new RPGGame.UI.Avalonia.Effects.RoomBackdropConfig();

        /// <summary>
        /// Named layered text animation presets (base color + stackable overlay / HSV layers).
        /// </summary>
        public Dictionary<string, TextAnimationPresetConfig> TextAnimationPresets { get; set; } =
            new Dictionary<string, TextAnimationPresetConfig>();
        
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

    }
}
