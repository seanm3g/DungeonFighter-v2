using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Platform;
using RPGGame.UI.Avalonia;

namespace RPGGame.UI.Avalonia.Helpers
{
    /// <summary>
    /// Multi-monitor–friendly placement when the Action Interaction Lab opens: center the main game
    /// window on the monitor that contains it, dock the lab tools window to that monitor’s working-area
    /// right edge, and tuck any pop-out <see cref="SettingsWindow"/> on the same monitor to the left.
    /// </summary>
    public static class ActionLabWindowPlacement
    {
        public const int EdgeMarginPixels = 10;

        public static PixelPoint ComputeCenteredTopLeft(PixelRect workArea, int windowPixelWidth, int windowPixelHeight)
        {
            int x = workArea.X + Math.Max(0, (workArea.Width - windowPixelWidth) / 2);
            int y = workArea.Y + Math.Max(0, (workArea.Height - windowPixelHeight) / 2);
            return new PixelPoint(x, y);
        }

        public static PixelPoint ComputeRightAnchoredTopLeft(PixelRect workArea, int windowPixelWidth, int windowPixelHeight, int rightMarginPixels)
        {
            int x = workArea.X + workArea.Width - windowPixelWidth - rightMarginPixels;
            if (x < workArea.X)
                x = workArea.X;
            int y = workArea.Y + Math.Max(0, (workArea.Height - windowPixelHeight) / 2);
            return new PixelPoint(x, y);
        }

        public static PixelPoint ComputeLeftAnchoredTopLeft(PixelRect workArea, int windowPixelWidth, int windowPixelHeight, int leftMarginPixels)
        {
            int x = workArea.X + leftMarginPixels;
            int rightLimit = workArea.X + workArea.Width - windowPixelWidth - leftMarginPixels;
            if (x > rightLimit)
                x = Math.Max(workArea.X, rightLimit);
            int y = workArea.Y + Math.Max(0, (workArea.Height - windowPixelHeight) / 2);
            return new PixelPoint(x, y);
        }

        public static int GetWindowPixelWidth(Window w) =>
            Math.Max(1, (int)Math.Ceiling(w.Width * Math.Max(1.0, w.RenderScaling)));

        public static int GetWindowPixelHeight(Window w) =>
            Math.Max(1, (int)Math.Ceiling(w.Height * Math.Max(1.0, w.RenderScaling)));

        /// <summary>
        /// Applies desktop layout after both windows exist (call from UI thread after first layout).
        /// </summary>
        public static void ApplyActionLabOpenMultiWindowLayout(Window mainGameWindow, Window actionLabControlsWindow)
        {
            var screens = mainGameWindow.Screens;
            if (screens == null)
                return;

            Screen? host = screens.ScreenFromWindow(mainGameWindow) ?? screens.Primary;
            if (host == null)
                return;

            PixelRect wa = host.WorkingArea;

            mainGameWindow.Position = ComputeCenteredTopLeft(
                wa,
                GetWindowPixelWidth(mainGameWindow),
                GetWindowPixelHeight(mainGameWindow));

            actionLabControlsWindow.Position = ComputeRightAnchoredTopLeft(
                wa,
                GetWindowPixelWidth(actionLabControlsWindow),
                GetWindowPixelHeight(actionLabControlsWindow),
                EdgeMarginPixels);

            if (Application.Current?.ApplicationLifetime is not IClassicDesktopStyleApplicationLifetime desktop)
                return;

            foreach (var top in desktop.Windows)
            {
                if (top is not SettingsWindow settings || !settings.IsVisible)
                    continue;

                Screen? swScreen = screens.ScreenFromWindow(settings);
                if (swScreen == null || !host.Equals(swScreen))
                    continue;

                settings.Position = ComputeLeftAnchoredTopLeft(
                    wa,
                    GetWindowPixelWidth(settings),
                    GetWindowPixelHeight(settings),
                    EdgeMarginPixels);
            }
        }
    }
}
