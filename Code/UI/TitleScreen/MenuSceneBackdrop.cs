using System;
using System.Collections.Generic;
using RPGGame.UI.Avalonia.Canvas;
using RPGGame.UI.Avalonia.Layout;

namespace RPGGame.UI.TitleScreen
{
    /// <summary>
    /// The title screen's torch-lit cave (<see cref="TitleSceneBuilder.BuildBackdrop"/>) kept animating behind menus.
    /// Uses the title's scene seed so the torches, drips and eyes stay where the title left them.
    /// </summary>
    public sealed class MenuSceneBackdrop
    {
        /// <summary>Color multiplier for the cave behind menus (title screen = 1).</summary>
        public const double Brightness = 0.45;

        /// <summary>Repaint interval while a menu shows the cave (~20 fps; the title runs at ~30).</summary>
        public const int FrameIntervalMs = 50;

        /// <summary>Empty cells kept beside each line of menu text.</summary>
        private const int TextPadding = 1;

        private readonly System.Diagnostics.Stopwatch _clock = System.Diagnostics.Stopwatch.StartNew();
        private TitleSceneBuilder? _builder;
        private int _builderSeed;
        private bool[] _clearMask = Array.Empty<bool>();

        /// <summary>
        /// Builds this paint's cave with cells under <paramref name="texts"/> left clear, or null while the title scene
        /// itself is on the canvas (its full-screen runs are marked <see cref="CanvasText.IgnoresCenterColumn"/>).
        /// </summary>
        public TitleSceneFrame? BuildFrame(int gridWidth, int gridHeight, IReadOnlyList<CanvasText> texts)
        {
            if (gridWidth <= 0 || gridHeight <= 0)
                return null;
            foreach (var text in texts)
            {
                if (text.IgnoresCenterColumn)
                    return null;
            }

            int seed = TitleScreenHelper.SceneSeed;
            if (_builder == null || _builderSeed != seed)
            {
                _builder = new TitleSceneBuilder(seed);
                _builderSeed = seed;
            }

            BuildClearMask(gridWidth, gridHeight, texts);
            var mask = _clearMask;
            return _builder.BuildBackdrop(gridWidth, gridHeight, _clock.Elapsed.TotalSeconds, Brightness,
                (x, y) => x < gridWidth && y < gridHeight && mask[y * gridWidth + x]);
        }

        private void BuildClearMask(int width, int height, IReadOnlyList<CanvasText> texts)
        {
            if (_clearMask.Length != width * height)
                _clearMask = new bool[width * height];
            else
                Array.Clear(_clearMask);

            foreach (var text in texts)
            {
                if (string.IsNullOrWhiteSpace(text.Content) || text.Y < 0 || text.Y >= height)
                    continue;

                // Center-column text is drawn zoomed, so it covers more cells than it has characters.
                double scale = CenterColumnTextZoom.IsCenterColumnX(text.X) ? CenterColumnTextZoom.FontScale : 1.0;
                int start = text.X - TextPadding;
                int end = text.X + (int)Math.Ceiling(text.Content.TrimEnd().Length * scale) + TextPadding;
                int rows = Math.Max(1, (int)Math.Ceiling(scale));
                for (int y = text.Y; y < Math.Min(height, text.Y + rows); y++)
                {
                    for (int x = Math.Max(0, start); x < Math.Min(width, end); x++)
                        _clearMask[y * width + x] = true;
                }
            }
        }
    }
}
