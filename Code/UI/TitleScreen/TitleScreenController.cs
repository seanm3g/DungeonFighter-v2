using System;
using System.Threading;
using System.Threading.Tasks;
using RPGGame;
using RPGGame.Data;
using RPGGame.UI.Avalonia;
using RPGGame.UI.Avalonia.Managers;

namespace RPGGame.UI.TitleScreen
{
    /// <summary>
    /// Main controller for the title screen
    /// Boot skips intro and runs cancelable idle gradient with atomic press-key paint
    /// </summary>
    public class TitleScreenController
    {
        private readonly TitleAnimationConfig _config;
        private readonly TitleAnimation _animation;
        private readonly ITitleRenderer _renderer;

        public TitleScreenController(
            TitleAnimationConfig config,
            ITitleRenderer renderer,
            TitleIdlePalette? palette = null)
        {
            _config = config ?? throw new ArgumentNullException(nameof(config));
            _renderer = renderer ?? throw new ArgumentNullException(nameof(renderer));
            _animation = new TitleAnimation(config, palette);
        }

        public TitleIdlePalette Palette => _animation.Palette;

        /// <summary>
        /// Plays the intro animation sequence only.
        /// </summary>
        public async Task PlayAnimationAsync()
        {
            foreach (var step in _animation.GenerateAnimationSequence())
            {
                _renderer.RenderFrame(step.Frame);

                if (step.DurationMs <= 0)
                    continue;

                int delayMs = Math.Max(step.DurationMs, 16);
                await Task.Delay(delayMs).ConfigureAwait(false);
            }
        }

        /// <summary>
        /// Skips the intro sequence and runs idle gradient with press-key until cancelled.
        /// </summary>
        /// <param name="onReadyForKey">Invoked after the first idle frame (with press-key) is shown.</param>
        /// <param name="firstFrameAlreadyRendered">
        /// When true, the first idle frame was already painted (hidden-window preload).
        /// Skip the opening clear/paint so revealing the window does not flash black.
        /// </param>
        public async Task ShowAnimatedTitleScreenAsync(
            CancellationToken idleCancellationToken = default,
            System.Action? onReadyForKey = null,
            bool firstFrameAlreadyRendered = false)
        {
            await RunIdleCycleAsync(idleCancellationToken, onReadyForKey, firstFrameAlreadyRendered)
                .ConfigureAwait(false);
        }

        /// <summary>
        /// Loops dungeon-selection undulation on DEMON/FIGHTER until cancellation.
        /// Holds each neon palette for <see cref="TitleAnimationConfig.PaletteShiftIntervalMs"/>,
        /// then RGB-crossfades into the next over <see cref="TitleAnimationConfig.PaletteTransitionMs"/>.
        /// Backdrop stays black. Press-key is painted in the same UI pass as each frame.
        /// </summary>
        public async Task RunIdleCycleAsync(
            CancellationToken cancellationToken,
            System.Action? onReadyForKey = null,
            bool firstFrameAlreadyRendered = false)
        {
            var animConfig = UIConfiguration.LoadFromFile().DungeonSelectionAnimation
                ?? new DungeonSelectionAnimationConfig();
            int undulationMs = Math.Max(16, animConfig.UndulationIntervalMs);
            int maskIntervalMs = Math.Max(undulationMs, animConfig.BrightnessMask.UpdateIntervalMs);
            bool maskEnabled = animConfig.BrightnessMask.Enabled;
            int paletteHoldMs = Math.Max(undulationMs, _config.PaletteShiftIntervalMs);
            int transitionMs = Math.Max(undulationMs, _config.PaletteTransitionMs);

            var state = DungeonSelectionAnimationState.Instance;
            int maskAccumMs = 0;
            int holdAccumMs = 0;
            int transitionAccumMs = 0;
            bool notifiedReady = false;
            bool skipOpeningPaint = firstFrameAlreadyRendered;

            TitleIdlePalette current = _animation.Palette;
            TitleIdlePalette? next = null;

            // Opening clear would flash black if the first frame was already painted while hidden.
            if (!skipOpeningPaint)
                _renderer.ResetBackground();

            try
            {
                while (!cancellationToken.IsCancellationRequested)
                {
                    if (skipOpeningPaint)
                    {
                        skipOpeningPaint = false;
                        if (!notifiedReady)
                        {
                            onReadyForKey?.Invoke();
                            notifiedReady = true;
                        }

                        try
                        {
                            await Task.Delay(undulationMs, cancellationToken).ConfigureAwait(false);
                        }
                        catch (OperationCanceledException)
                        {
                            break;
                        }

                        continue;
                    }
                    float blendProgress = 1f;
                    TitleIdlePalette? blendFrom = null;
                    TitleIdlePalette blendTo = current;

                    if (next != null)
                    {
                        transitionAccumMs += undulationMs;
                        blendProgress = Math.Clamp(transitionAccumMs / (float)transitionMs, 0f, 1f);
                        blendFrom = current;
                        blendTo = next;

                        if (blendProgress >= 1f)
                        {
                            current = next;
                            _animation.SetPalette(current);
                            next = null;
                            transitionAccumMs = 0;
                            holdAccumMs = 0;
                            blendFrom = null;
                            blendTo = current;
                            blendProgress = 1f;
                        }
                    }
                    else
                    {
                        holdAccumMs += undulationMs;
                        if (holdAccumMs >= paletteHoldMs)
                        {
                            next = TitleIdlePalettePicker.PickRandomExcept(current.TemplateName);
                            transitionAccumMs = 0;
                            holdAccumMs = 0;
                            blendFrom = current;
                            blendTo = next;
                            blendProgress = 0f;
                        }
                    }

                    state.AdvanceUndulation();
                    if (maskEnabled)
                    {
                        maskAccumMs += undulationMs;
                        if (maskAccumMs >= maskIntervalMs)
                        {
                            state.AdvanceBrightnessMask();
                            maskAccumMs = 0;
                        }
                    }

                    var frame = _animation.BuildIdleFrame(state, blendFrom, blendTo, blendProgress);
                    _renderer.RenderFrame(frame, includePressKey: true);

                    if (!notifiedReady)
                    {
                        onReadyForKey?.Invoke();
                        notifiedReady = true;
                    }

                    try
                    {
                        await Task.Delay(undulationMs, cancellationToken).ConfigureAwait(false);
                    }
                    catch (OperationCanceledException)
                    {
                        break;
                    }
                }
            }
            finally
            {
                _renderer.ResetBackground();
            }
        }

        public int GetAnimationDuration()
        {
            return _animation.GetTotalDurationMs();
        }
    }

    /// <summary>
    /// CPU-side title idle assets built before the main window is shown.
    /// </summary>
    public sealed class TitleScreenPreload
    {
        public TitleAnimationConfig Config { get; }
        public TitleIdlePalette Palette { get; }
        public TitleFrame FirstFrame { get; }

        public TitleScreenPreload(TitleAnimationConfig config, TitleIdlePalette palette, TitleFrame firstFrame)
        {
            Config = config ?? throw new ArgumentNullException(nameof(config));
            Palette = palette ?? throw new ArgumentNullException(nameof(palette));
            FirstFrame = firstFrame ?? throw new ArgumentNullException(nameof(firstFrame));
        }
    }

    /// <summary>
    /// Static helper for title screen preload, first-frame paint, and idle display.
    /// </summary>
    public static class TitleScreenHelper
    {
        private static TitleAnimationConfig? _cachedConfig;
        private static TitleScreenPreload? _preload;

        private static TitleAnimationConfig GetConfig()
        {
            if (_cachedConfig == null)
            {
                _cachedConfig = TitleAnimationConfigLoader.LoadOrDefault();
            }
            return _cachedConfig;
        }

        public static void ReloadConfiguration()
        {
            _cachedConfig = null;
            _preload = null;
        }

        /// <summary>
        /// Loads color tables, title config, and the first idle frame before the window is visible.
        /// Idempotent; later idle uses the same palette so the first paint does not flash.
        /// </summary>
        public static TitleScreenPreload Preload()
        {
            if (_preload != null)
                return _preload;

            var config = GetConfig();
            _ = ColorCodeLoader.GetColor("W");
            _ = ColorCodeLoader.GetColor("o");
            ColorTemplateLoader.LoadColorTemplates();
            _ = UIConfiguration.LoadFromFile();

            var palette = TitleIdlePalettePicker.PickRandom();
            var animation = new TitleAnimation(config, palette);
            var firstFrame = animation.BuildIdleFrame(DungeonSelectionAnimationState.Instance);
            _preload = new TitleScreenPreload(config, palette, firstFrame);
            return _preload;
        }

        /// <summary>
        /// Paints the preloaded first idle frame (including press-key) on the current renderer.
        /// </summary>
        public static void RenderPreloadedFirstFrame()
        {
            var preload = Preload();
            var renderer = CreateRenderer();
            if (renderer == null)
                return;

            renderer.ResetBackground();
            renderer.RenderFrame(preload.FirstFrame, includePressKey: true);
        }

        /// <summary>
        /// Shows the animated title screen with random idle palette and cancelable idle loop.
        /// </summary>
        /// <param name="firstFrameAlreadyRendered">
        /// True when <see cref="RenderPreloadedFirstFrame"/> already painted while the window was hidden.
        /// </param>
        public static async Task ShowAnimatedTitleScreenAsync(
            CancellationToken idleCancellationToken = default,
            System.Action? onReadyForKey = null,
            bool firstFrameAlreadyRendered = false)
        {
            var preload = Preload();
            var renderer = CreateRenderer();

            if (renderer != null)
            {
                var controller = new TitleScreenController(preload.Config, renderer, preload.Palette);
                await controller.ShowAnimatedTitleScreenAsync(
                        idleCancellationToken,
                        onReadyForKey,
                        firstFrameAlreadyRendered)
                    .ConfigureAwait(false);
            }
            else
            {
                ErrorHandler.LogWarning("TitleScreenHelper.ShowAnimatedTitleScreen",
                    "No renderer available, skipping title screen");
                onReadyForKey?.Invoke();
            }
        }

        public static async Task AnimateTitleScreenAsync()
        {
            var config = GetConfig();
            var renderer = CreateRenderer();
            var palette = TitleIdlePalettePicker.PickRandom();

            if (renderer != null)
            {
                var controller = new TitleScreenController(config, renderer, palette);
                await controller.PlayAnimationAsync().ConfigureAwait(false);
            }
        }

        /// <summary>
        /// Shows a static title screen (final frame) without animation.
        /// </summary>
        public static void ShowStaticTitleScreen()
        {
            _ = RPGGame.Data.ColorCodeLoader.GetColor("W");
            _ = RPGGame.Data.ColorCodeLoader.GetColor("o");

            var config = GetConfig();
            var renderer = CreateRenderer();

            if (renderer != null)
            {
                renderer.Clear();
                var palette = TitleIdlePalettePicker.PickRandom();
                var frameBuilder = new TitleFrameBuilder(config);
                var finalFrame = frameBuilder.BuildPhasedPaletteFrame(palette, 0);
                renderer.RenderFrame(finalFrame, includePressKey: true);
            }
            else
            {
                ErrorHandler.LogWarning("TitleScreenHelper.ShowStaticTitleScreen",
                    "No renderer available, skipping title screen");
            }
        }

        private static ITitleRenderer? CreateRenderer()
        {
            var uiManager = UIManager.GetCustomUIManager();

            if (uiManager is CanvasUICoordinator canvasUI)
            {
                return new CanvasTitleRenderer(canvasUI);
            }
            else if (uiManager == null)
            {
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

        public static TitleAnimationConfig LoadOrDefault()
        {
            try
            {
                string? configPath = JsonLoader.FindGameDataFile(ConfigFileName);

                if (configPath != null && System.IO.File.Exists(configPath))
                {
                    var json = System.IO.File.ReadAllText(configPath);
                    var config = System.Text.Json.JsonSerializer.Deserialize<TitleAnimationConfig>(json);

                    if (config != null)
                    {
                        // Prefer SettleFrames; sync from FinalTransitionFrames when settle unset.
                        if (config.SettleFrames <= 0 && config.FinalTransitionFrames > 0)
                            config.SettleFrames = config.FinalTransitionFrames;
                        else if (config.FinalTransitionFrames <= 0 && config.SettleFrames > 0)
                            config.FinalTransitionFrames = config.SettleFrames;

                        System.Diagnostics.Debug.WriteLine($"[DEBUG] Loaded TitleAnimationConfig from: {configPath}");
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

            return new TitleAnimationConfig();
        }

        public static void Save(TitleAnimationConfig config)
        {
            try
            {
                string? configPath = JsonLoader.FindGameDataFile(ConfigFileName);

                if (configPath == null)
                {
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
