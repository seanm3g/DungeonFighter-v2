using System;
using System.Collections.Generic;
using RPGGame.UI.Avalonia.Canvas;
using RPGGame.UI.Avalonia.Layout;

namespace RPGGame.UI.TitleScreen
{
    /// <summary>Which animated scene a screen shows behind it.</summary>
    public enum MenuScene
    {
        None,
        /// <summary>The title's torch-lit cave (<see cref="TitleSceneBuilder.BuildBackdrop"/>): main menu, settings, character creation.</summary>
        Cave,
        /// <summary>The inn's common room (<see cref="InnInteriorSceneBuilder"/>): hub menu, character, inventory and skill tree screens.</summary>
        InnInterior
    }

    /// <summary>
    /// The title's cave (<see cref="TitleSceneBuilder"/>) or the inn's common room (<see cref="InnInteriorSceneBuilder"/>)
    /// animating behind screens outside a run. Uses the title's scene seed so the scene's layout stays put for the session.
    /// </summary>
    public sealed class MenuSceneBackdrop
    {
        /// <summary>Color multiplier for the scene behind menus.</summary>
        public const double Brightness = 0.45;

        /// <summary>Repaint interval while a menu shows a scene (~20 fps; the title runs at ~30).</summary>
        public const int FrameIntervalMs = 50;

        /// <summary>Empty cells kept beside each line of menu text.</summary>
        private const int TextPadding = 1;

        private readonly System.Diagnostics.Stopwatch _clock = System.Diagnostics.Stopwatch.StartNew();
        private TitleSceneBuilder? _cave;
        private InnInteriorSceneBuilder? _interior;
        private int _builderSeed;
        private bool[] _clearMask = Array.Empty<bool>();

        /// <summary>
        /// Builds this paint's <paramref name="scene"/> with cells under <paramref name="texts"/> left clear, or null for
        /// <see cref="MenuScene.None"/> or while the title scene itself is on the canvas (its full-screen runs are marked
        /// <see cref="CanvasText.IgnoresCenterColumn"/>).
        /// </summary>
        public TitleSceneFrame? BuildFrame(int gridWidth, int gridHeight, IReadOnlyList<CanvasText> texts, MenuScene scene)
        {
            if (scene == MenuScene.None || gridWidth <= 0 || gridHeight <= 0)
                return null;
            foreach (var text in texts)
            {
                if (text.IgnoresCenterColumn)
                    return null;
            }

            int seed = TitleScreenHelper.SceneSeed;
            if (_cave == null || _builderSeed != seed)
            {
                _cave = new TitleSceneBuilder(seed);
                _interior = new InnInteriorSceneBuilder(seed);
                _builderSeed = seed;
            }

            BuildClearMask(gridWidth, gridHeight, texts);
            var mask = _clearMask;
            double t = _clock.Elapsed.TotalSeconds;
            if (scene == MenuScene.InnInterior)
                return BuildInCenterPanel(_interior!, gridWidth, gridHeight, t, mask);

            return _cave.BuildBackdrop(gridWidth, gridHeight, t, Brightness,
                (x, y) => x < gridWidth && y < gridHeight && mask[y * gridWidth + x]);
        }

        /// <summary>
        /// The common room fitted inside the center panel's border, so the side panels stay plain.
        /// </summary>
        private static TitleSceneFrame BuildInCenterPanel(InnInteriorSceneBuilder builder, int gridWidth, int gridHeight, double t, bool[] mask)
        {
            int left = Math.Max(0, LayoutConstants.CENTER_PANEL_X + 1);
            int top = Math.Max(0, LayoutConstants.CENTER_PANEL_Y + 1);
            int right = Math.Min(gridWidth, LayoutConstants.CENTER_PANEL_X + LayoutConstants.CENTER_PANEL_WIDTH - 1);
            int bottom = Math.Min(gridHeight, LayoutConstants.CENTER_PANEL_Y + LayoutConstants.CENTER_PANEL_HEIGHT - 1);
            int width = Math.Max(1, right - left);
            int height = Math.Max(1, bottom - top);

            var room = builder.BuildBackdrop(width, height, t, Brightness, (x, y) =>
            {
                int gx = x + left, gy = y + top;
                return gx < gridWidth && gy < gridHeight && mask[gy * gridWidth + gx];
            });

            // Shift into place and trim anything past the panel (the room never builds smaller than 40 × 24).
            var frame = new TitleSceneFrame(gridWidth, gridHeight, room.Background);
            foreach (var run in room.Texts)
            {
                if (run.Y >= height || run.X >= width)
                    continue;
                string text = run.X + run.Text.Length > width ? run.Text.Substring(0, width - run.X) : run.Text;
                frame.Texts.Add(new TitleSceneText(run.X + left, run.Y + top, text, run.Color));
            }
            return frame;
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
