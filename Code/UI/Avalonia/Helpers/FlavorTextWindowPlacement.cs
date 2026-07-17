using System;
using System.Collections.Generic;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Platform;
using Avalonia.Threading;

namespace RPGGame.UI.Avalonia.Helpers
{
    /// <summary>
    /// Monitor-aware layout for Flavor Text EXPAND: center the Settings (Generate) window,
    /// place Forms / Form template / Categories / Legacy banks in left and right columns.
    /// </summary>
    public static class FlavorTextWindowPlacement
    {
        public const int EdgeMarginPixels = 10;
        public const int MinSideColumnPixels = 280;
        public const int MinMainWidthPixels = 420;
        public const int MinMainHeightPixels = 360;
        public const int MinSectionHeightPixels = 160;

        public readonly record struct SectionPlacement(PixelPoint Position, PixelSize Size);

        public readonly record struct ExpandLayout(
            PixelPoint MainPosition,
            PixelSize MainSize,
            SectionPlacement Forms,
            SectionPlacement Template,
            SectionPlacement Categories,
            SectionPlacement Legacy);

        /// <summary>
        /// Computes a 3-column layout inside <paramref name="workArea"/>:
        /// left = Forms (top) + Categories (bottom), center = main Settings, right = Template (top) + Legacy (bottom).
        /// Sizes scale with the monitor working area.
        /// </summary>
        public static ExpandLayout Compute(PixelRect workArea)
        {
            int gap = EdgeMarginPixels;
            int availW = Math.Max(1, workArea.Width - 2 * gap);
            int availH = Math.Max(1, workArea.Height - 2 * gap);

            // Side columns share leftover width after a centered main (~42% of available).
            int mainW = Clamp((int)(availW * 0.42), MinMainWidthPixels, Math.Max(MinMainWidthPixels, availW - 2 * MinSideColumnPixels - 2 * gap));
            int sideW = (availW - mainW - 2 * gap) / 2;
            if (sideW < MinSideColumnPixels)
            {
                sideW = Math.Max(200, Math.Min(MinSideColumnPixels, (availW - MinMainWidthPixels - 2 * gap) / 2));
                mainW = Math.Max(MinMainWidthPixels, availW - 2 * sideW - 2 * gap);
            }

            // Main is vertically centered; side columns split top/bottom with a gap.
            int mainH = Clamp((int)(availH * 0.62), MinMainHeightPixels, availH);
            int sideStackGap = gap;
            int sideTotalH = availH;
            int topH = Math.Max(MinSectionHeightPixels, (sideTotalH - sideStackGap) / 2);
            int bottomH = Math.Max(MinSectionHeightPixels, sideTotalH - sideStackGap - topH);

            int leftX = workArea.X + gap;
            int mainX = leftX + sideW + gap;
            int rightX = mainX + mainW + gap;
            int topY = workArea.Y + gap;
            int mainY = workArea.Y + gap + Math.Max(0, (availH - mainH) / 2);
            int bottomY = topY + topH + sideStackGap;

            return new ExpandLayout(
                MainPosition: new PixelPoint(mainX, mainY),
                MainSize: new PixelSize(mainW, mainH),
                Forms: new SectionPlacement(new PixelPoint(leftX, topY), new PixelSize(sideW, topH)),
                Template: new SectionPlacement(new PixelPoint(rightX, topY), new PixelSize(sideW, topH)),
                Categories: new SectionPlacement(new PixelPoint(leftX, bottomY), new PixelSize(sideW, bottomH)),
                Legacy: new SectionPlacement(new PixelPoint(rightX, bottomY), new PixelSize(sideW, bottomH)));
        }

        public static SectionPlacement GetPlacement(ExpandLayout layout, string hostName) =>
            hostName switch
            {
                "FlavorTextFormsHost" => layout.Forms,
                "FlavorTextTemplateHost" => layout.Template,
                "FlavorTextCategoriesHost" => layout.Categories,
                "FlavorTextLegacyHost" => layout.Legacy,
                _ => layout.Forms
            };

        /// <summary>
        /// Centers Settings and arranges section windows on the Settings window's monitor.
        /// Call on the UI thread after section windows are shown (preferably after a layout pass).
        /// </summary>
        public static void Apply(Window mainSettingsWindow, IReadOnlyDictionary<string, Window> sectionWindowsByHost)
        {
            var screens = mainSettingsWindow.Screens;
            if (screens == null)
                return;

            Screen? host = screens.ScreenFromWindow(mainSettingsWindow) ?? screens.Primary;
            if (host == null)
                return;

            ExpandLayout layout = Compute(host.WorkingArea);
            ApplyWindowBounds(mainSettingsWindow, layout.MainPosition, layout.MainSize);

            foreach (var (hostName, window) in sectionWindowsByHost)
            {
                if (window == null)
                    continue;
                SectionPlacement placement = GetPlacement(layout, hostName);
                ApplyWindowBounds(window, placement.Position, placement.Size);
            }
        }

        public static void ApplyAfterLayout(Window mainSettingsWindow, IReadOnlyDictionary<string, Window> sectionWindowsByHost)
        {
            Dispatcher.UIThread.Post(
                () => Apply(mainSettingsWindow, sectionWindowsByHost),
                DispatcherPriority.Loaded);
        }

        private static void ApplyWindowBounds(Window window, PixelPoint position, PixelSize pixelSize)
        {
            double scaling = Math.Max(1.0, window.RenderScaling);
            window.Width = Math.Max(window.MinWidth, pixelSize.Width / scaling);
            window.Height = Math.Max(window.MinHeight, pixelSize.Height / scaling);
            window.WindowStartupLocation = WindowStartupLocation.Manual;
            window.Position = position;
        }

        private static int Clamp(int value, int min, int max)
        {
            if (max < min)
                return min;
            if (value < min) return min;
            if (value > max) return max;
            return value;
        }
    }
}
