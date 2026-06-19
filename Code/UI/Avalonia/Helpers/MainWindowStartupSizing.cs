using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Platform;

namespace RPGGame.UI.Avalonia.Helpers
{
    /// <summary>
    /// Scales the main game window on macOS so the 1920×1080 design fits within the monitor
    /// working area (menu bar and dock excluded). Targets Retina Macs at 2560×1600 where
    /// logical resolution is often ~1280×800 or ~1728×1117.
    /// </summary>
    public static class MainWindowStartupSizing
    {
        public const double DesignWidth = 1920;
        public const double DesignHeight = 1080;
        public const double MacEdgeMarginLogical = 16;
        /// <summary>Extra bottom margin when macOS dock overlaps <see cref="Screen.WorkingArea"/>.</summary>
        public const double MacDockSafetyMarginLogical = 80;
        /// <summary>Title-bar chrome not always reflected in client <see cref="Window.Height"/>.</summary>
        public const double MacTitleBarSafetyMarginLogical = 28;
        public const double MinStartupWidth = 800;
        public const double MinStartupHeight = 600;

        /// <summary>
        /// Computes a proportional startup size that fits the given logical working area.
        /// Never scales above the design size; only shrinks when the monitor is smaller.
        /// </summary>
        public static (double Width, double Height, double Scale) ComputeMacStartupSize(
            double workingAreaLogicalWidth,
            double workingAreaLogicalHeight,
            double designWidth = DesignWidth,
            double designHeight = DesignHeight,
            double edgeMargin = MacEdgeMarginLogical,
            double extraBottomMargin = 0,
            double extraTopMargin = 0)
        {
            if (designWidth <= 0 || designHeight <= 0)
                return (DesignWidth, DesignHeight, 1.0);

            double availableWidth = workingAreaLogicalWidth - (edgeMargin * 2);
            double availableHeight = workingAreaLogicalHeight - (edgeMargin * 2) - extraBottomMargin - extraTopMargin;

            if (availableWidth <= 0 || availableHeight <= 0)
                return (designWidth, designHeight, 1.0);

            double scaleX = availableWidth / designWidth;
            double scaleY = availableHeight / designHeight;
            double scale = Math.Min(scaleX, scaleY);
            scale = Math.Min(scale, 1.0);

            double width = designWidth * scale;
            double height = designHeight * scale;

            if (width < MinStartupWidth || height < MinStartupHeight)
            {
                double minScaleX = MinStartupWidth / designWidth;
                double minScaleY = MinStartupHeight / designHeight;
                scale = Math.Max(scale, Math.Max(minScaleX, minScaleY));
                width = designWidth * scale;
                height = designHeight * scale;
            }

            return (width, height, scale);
        }

        /// <summary>
        /// Prefer <see cref="Screen.Scaling"/>; <see cref="Window.RenderScaling"/> can still be 1.0 on first open.
        /// </summary>
        public static double ResolveScreenScaling(Screen screen, Window window)
        {
            if (screen.Scaling > 0)
                return screen.Scaling;
            if (window.RenderScaling > 0)
                return window.RenderScaling;
            return 1.0;
        }

        /// <summary>
        /// Centers the window in the working area and clamps so the frame cannot extend past it.
        /// </summary>
        public static PixelPoint ComputeClampedCenteredTopLeft(
            PixelRect workArea,
            int windowPixelWidth,
            int windowPixelHeight,
            int edgeMarginPixels = 0)
        {
            int x = workArea.X + Math.Max(0, (workArea.Width - windowPixelWidth) / 2);
            int y = workArea.Y + Math.Max(0, (workArea.Height - windowPixelHeight) / 2);

            int minX = workArea.X + edgeMarginPixels;
            int minY = workArea.Y + edgeMarginPixels;
            int maxX = workArea.X + workArea.Width - windowPixelWidth - edgeMarginPixels;
            int maxY = workArea.Y + workArea.Height - windowPixelHeight - edgeMarginPixels;

            if (maxX < minX)
                x = workArea.X;
            else
                x = Math.Clamp(x, minX, maxX);

            if (maxY < minY)
                y = workArea.Y;
            else
                y = Math.Clamp(y, minY, maxY);

            return new PixelPoint(x, y);
        }

        /// <summary>
        /// On macOS, resizes and re-centers the main window to fit the current screen working area.
        /// No-op on other platforms.
        /// </summary>
        public static void ApplyMacStartupSizingIfNeeded(Window window)
        {
            if (!OperatingSystem.IsMacOS())
                return;

            var screens = window.Screens;
            if (screens == null)
                return;

            Screen? screen = screens.ScreenFromWindow(window) ?? screens.Primary;
            if (screen == null)
                return;

            double scaling = ResolveScreenScaling(screen, window);
            PixelRect workArea = screen.WorkingArea;
            double logicalWidth = workArea.Width / scaling;
            double logicalHeight = workArea.Height / scaling;

            var (width, height, _) = ComputeMacStartupSize(
                logicalWidth,
                logicalHeight,
                extraBottomMargin: MacDockSafetyMarginLogical,
                extraTopMargin: MacTitleBarSafetyMarginLogical);
            window.Width = width;
            window.Height = height;

            int edgeMarginPixels = (int)Math.Ceiling(MacEdgeMarginLogical * scaling);
            window.Position = ComputeClampedCenteredTopLeft(
                workArea,
                ActionLabWindowPlacement.GetWindowPixelWidth(window),
                ActionLabWindowPlacement.GetWindowPixelHeight(window),
                edgeMarginPixels);
        }

        public static (double Width, double Height, double MinWidth, double MinHeight) ComputeScaledOverlayDimensions(
            double designWidth,
            double designHeight,
            double minWidth,
            double minHeight,
            double sizeRatio) =>
            (designWidth * sizeRatio, designHeight * sizeRatio, minWidth * sizeRatio, minHeight * sizeRatio);

        /// <summary>
        /// Scales overlay panel dimensions proportionally when the main window was shrunk on Mac.
        /// </summary>
        public static void ScaleOverlayPanel(
            Control? panel,
            double designWidth,
            double designHeight,
            double minWidth,
            double minHeight,
            double sizeRatio)
        {
            if (panel == null || sizeRatio <= 0)
                return;

            var dims = ComputeScaledOverlayDimensions(designWidth, designHeight, minWidth, minHeight, sizeRatio);
            panel.Width = dims.Width;
            panel.Height = dims.Height;
            panel.MinWidth = dims.MinWidth;
            panel.MinHeight = dims.MinHeight;
        }
    }
}
