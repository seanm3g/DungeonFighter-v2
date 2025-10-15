using System;
using System.Threading;
using RPGGame;
using RPGGame.UI.Avalonia;

namespace RPGGame.UI.TitleScreen
{
    /// <summary>
    /// Main controller for the title screen animation
    /// Orchestrates the animation sequence and rendering
    /// Replaces the legacy TitleScreenAnimator with a cleaner, more maintainable architecture
    /// </summary>
    public class TitleScreenController
    {
        private readonly TitleAnimationConfig _config;
        private readonly TitleAnimation _animation;
        private readonly ITitleRenderer _renderer;

        /// <summary>
        /// Creates a new title screen controller with specified configuration and renderer
        /// </summary>
        public TitleScreenController(TitleAnimationConfig config, ITitleRenderer renderer)
        {
            _config = config ?? throw new ArgumentNullException(nameof(config));
            _renderer = renderer ?? throw new ArgumentNullException(nameof(renderer));
            _animation = new TitleAnimation(config);
        }

        /// <summary>
        /// Plays the complete title screen animation sequence
        /// </summary>
        public void PlayAnimation()
        {
            foreach (var step in _animation.GenerateAnimationSequence())
            {
                _renderer.RenderFrame(step.Frame);
                Thread.Sleep(step.DurationMs);
            }
        }

        /// <summary>
        /// Shows the complete animated title screen with press key message
        /// This is the main entry point for displaying the title screen
        /// </summary>
        public void ShowAnimatedTitleScreen()
        {
            // Play the animation sequence
            PlayAnimation();

            // Show press key message
            _renderer.ShowPressKeyMessage();
        }

        /// <summary>
        /// Gets the total animation duration (useful for progress tracking)
        /// </summary>
        public int GetAnimationDuration()
        {
            return _animation.GetTotalDurationMs();
        }
    }

    /// <summary>
    /// Static helper class for easy title screen display
    /// Provides backward compatibility with legacy TitleScreenAnimator API
    /// </summary>
    public static class TitleScreenHelper
    {
        private static TitleAnimationConfig? _cachedConfig;

        /// <summary>
        /// Gets or loads the title animation configuration
        /// </summary>
        private static TitleAnimationConfig GetConfig()
        {
            if (_cachedConfig == null)
            {
                _cachedConfig = TitleAnimationConfigLoader.LoadOrDefault();
            }
            return _cachedConfig;
        }

        /// <summary>
        /// Reloads the configuration from file
        /// </summary>
        public static void ReloadConfiguration()
        {
            _cachedConfig = null;
        }

        /// <summary>
        /// Shows the animated title screen using the appropriate renderer
        /// Automatically detects whether to use Canvas or Console renderer
        /// </summary>
        public static void ShowAnimatedTitleScreen()
        {
            var config = GetConfig();
            var renderer = CreateRenderer();

            if (renderer != null)
            {
                var controller = new TitleScreenController(config, renderer);
                controller.ShowAnimatedTitleScreen();
            }
            else
            {
                // Fallback to legacy animation if no renderer available
                ErrorHandler.LogWarning("TitleScreenHelper.ShowAnimatedTitleScreen", 
                    "No renderer available, falling back to legacy OpeningAnimation");
                OpeningAnimation.ShowOpeningAnimation();
            }
        }

        /// <summary>
        /// Animates just the title screen without the press key message
        /// </summary>
        public static void AnimateTitleScreen()
        {
            var config = GetConfig();
            var renderer = CreateRenderer();

            if (renderer != null)
            {
                var controller = new TitleScreenController(config, renderer);
                controller.PlayAnimation();
            }
        }

        /// <summary>
        /// Creates the appropriate renderer based on available UI manager
        /// </summary>
        private static ITitleRenderer? CreateRenderer()
        {
            var uiManager = UIManager.GetCustomUIManager();

            if (uiManager is CanvasUIManager canvasUI)
            {
                return new CanvasTitleRenderer(canvasUI);
            }
            else if (uiManager == null)
            {
                // Console mode
                return new ConsoleTitleRenderer();
            }

            return null;
        }
    }

    /// <summary>
    /// Loads title animation configuration from file or provides defaults
    /// </summary>
    public static class TitleAnimationConfigLoader
    {
        private const string ConfigFileName = "TitleAnimationConfig.json";

        /// <summary>
        /// Loads configuration from JSON file or returns default configuration
        /// </summary>
        public static TitleAnimationConfig LoadOrDefault()
        {
            try
            {
                // Use JsonLoader to find the file in the correct GameData directory
                string? configPath = JsonLoader.FindGameDataFile(ConfigFileName);

                if (configPath != null && System.IO.File.Exists(configPath))
                {
                    var json = System.IO.File.ReadAllText(configPath);
                    var config = System.Text.Json.JsonSerializer.Deserialize<TitleAnimationConfig>(json);

                    if (config != null)
                    {
                        System.Diagnostics.Debug.WriteLine($"[DEBUG] Loaded TitleAnimationConfig from: {configPath}");
                        System.Diagnostics.Debug.WriteLine($"[DEBUG] FighterFinalColor: {config.ColorScheme.FighterFinalColor}");
                        return config;
                    }
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine($"[DEBUG] TitleAnimationConfig.json not found, using defaults");
                }
            }
            catch (Exception ex)
            {
                ErrorHandler.LogWarning("TitleAnimationConfigLoader.LoadOrDefault", 
                    $"Failed to load title animation config: {ex.Message}. Using defaults.");
            }

            // Return default configuration
            return new TitleAnimationConfig();
        }

        /// <summary>
        /// Saves configuration to JSON file
        /// </summary>
        public static void Save(TitleAnimationConfig config)
        {
            try
            {
                // Use JsonLoader to find the correct GameData directory
                string? configPath = JsonLoader.FindGameDataFile(ConfigFileName);
                
                if (configPath == null)
                {
                    // If not found, create in default GameData folder
                    configPath = System.IO.Path.Combine("GameData", ConfigFileName);
                }
                
                var options = new System.Text.Json.JsonSerializerOptions
                {
                    WriteIndented = true
                };

                var json = System.Text.Json.JsonSerializer.Serialize(config, options);
                System.IO.File.WriteAllText(configPath, json);
                
                System.Diagnostics.Debug.WriteLine($"[DEBUG] Saved TitleAnimationConfig to: {configPath}");
            }
            catch (Exception ex)
            {
                ErrorHandler.LogError(ex, "TitleAnimationConfigLoader.Save", 
                    "Failed to save title animation config");
            }
        }
    }
}

