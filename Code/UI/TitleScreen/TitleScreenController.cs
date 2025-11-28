using System;
using System.Threading;
using RPGGame;
using RPGGame.UI.Avalonia;
using RPGGame.UI.Animations;
using RPGGame.Data;

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
        /// Uses async/await for proper timing with UI thread
        /// </summary>
        public async System.Threading.Tasks.Task PlayAnimationAsync()
        {
            foreach (var step in _animation.GenerateAnimationSequence())
            {
                // Render the frame (this will wait for UI thread to complete)
                _renderer.RenderFrame(step.Frame);
                
                // Wait for the specified duration before next frame
                // Ensure minimum delay of 50ms to allow UI thread to process and display the frame
                // This prevents frames from being skipped due to UI thread being busy
                int delayMs = Math.Max(step.DurationMs, 50);
                await System.Threading.Tasks.Task.Delay(delayMs);
            }
        }

        /// <summary>
        /// Plays the complete title screen animation sequence (synchronous version)
        /// </summary>
        public void PlayAnimation()
        {
            // For synchronous calls, run async version and wait
            PlayAnimationAsync().GetAwaiter().GetResult();
        }

        /// <summary>
        /// Shows the complete animated title screen with press key message
        /// This is the main entry point for displaying the title screen
        /// </summary>
        public async System.Threading.Tasks.Task ShowAnimatedTitleScreenAsync()
        {
            // Play the animation sequence
            await PlayAnimationAsync();

            // Show press key message
            _renderer.ShowPressKeyMessage();
        }

        /// <summary>
        /// Shows the complete animated title screen with press key message (synchronous version)
        /// </summary>
        public void ShowAnimatedTitleScreen()
        {
            // For synchronous calls, run async version and wait
            ShowAnimatedTitleScreenAsync().GetAwaiter().GetResult();
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
        public static async System.Threading.Tasks.Task ShowAnimatedTitleScreenAsync()
        {
            var config = GetConfig();
            var renderer = CreateRenderer();

            if (renderer != null)
            {
                var controller = new TitleScreenController(config, renderer);
                await controller.ShowAnimatedTitleScreenAsync();
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
        /// Shows the animated title screen using the appropriate renderer (synchronous version)
        /// </summary>
        public static void ShowAnimatedTitleScreen()
        {
            // For synchronous calls, run async version and wait
            ShowAnimatedTitleScreenAsync().GetAwaiter().GetResult();
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
        /// Shows a static title screen (final frame) without animation
        /// Displays the final colored title screen immediately
        /// Yellow text for DUNGEON, red text for FIGHTER
        /// </summary>
        public static void ShowStaticTitleScreen()
        {
            // Preload color codes to ensure they're available before rendering
            // This prevents white default colors from appearing initially
            _ = RPGGame.Data.ColorCodeLoader.GetColor("W"); // Preload yellow
            _ = RPGGame.Data.ColorCodeLoader.GetColor("o"); // Preload dark orange (orange-red)
            
            var config = GetConfig();
            var renderer = CreateRenderer();

            if (renderer != null)
            {
                // Clear any existing content first
                renderer.Clear();
                
                // Build the static frame with solid colors: yellow (W) for DUNGEON, dark orange (o) for FIGHTER
                var frameBuilder = new TitleFrameBuilder(config);
                var finalFrame = frameBuilder.BuildSolidColorFrame("W", "o");
                
                // Render the final frame
                renderer.RenderFrame(finalFrame);
                
                // Show press key message
                renderer.ShowPressKeyMessage();
            }
            else
            {
                // Fallback to legacy animation if no renderer available
                ErrorHandler.LogWarning("TitleScreenHelper.ShowStaticTitleScreen", 
                    "No renderer available, falling back to legacy OpeningAnimation");
                OpeningAnimation.ShowOpeningAnimation();
            }
        }

        /// <summary>
        /// Creates the appropriate renderer based on available UI manager
        /// </summary>
        private static ITitleRenderer? CreateRenderer()
        {
            var uiManager = UIManager.GetCustomUIManager();

            if (uiManager is CanvasUICoordinator canvasUI)
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

