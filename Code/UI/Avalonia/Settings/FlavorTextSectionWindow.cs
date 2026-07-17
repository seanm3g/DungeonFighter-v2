using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using RPGGame.UI.Avalonia.Helpers;

namespace RPGGame.UI.Avalonia.Settings
{
    /// <summary>
    /// Non-modal pop-out that hosts one Flavor Text section (Forms, template, categories, or legacy banks).
    /// Content is reparented from the main panel host; closing re-docks via <see cref="ClosedWithContent"/>.
    /// </summary>
    public sealed class FlavorTextSectionWindow : Window
    {
        public string HostName { get; }

        /// <summary>Raised when the window closes with the section control that should be re-docked (may be null).</summary>
        public event Action<FlavorTextSectionWindow, Control?>? ClosedWithContent;

        public FlavorTextSectionWindow(string hostName, string title, Control sectionContent, PixelPoint position, Size size)
        {
            HostName = hostName;
            Title = $"Flavor Text — {title}";
            Width = size.Width;
            Height = size.Height;
            MinWidth = 320;
            MinHeight = 200;
            CanResize = true;
            WindowStartupLocation = WindowStartupLocation.Manual;
            Position = position;
            Background = new SolidColorBrush(Color.FromRgb(30, 30, 30));
            Classes.Add("settings-ui");

            Content = new Border
            {
                Padding = new Thickness(12),
                Background = Brushes.Transparent,
                Child = sectionContent
            };

            Closed += OnClosed;
        }

        public static FlavorTextSectionWindow Open(
            Window? owner,
            string hostName,
            string title,
            Control sectionContent,
            PixelPoint position,
            Size size)
        {
            var window = new FlavorTextSectionWindow(hostName, title, sectionContent, position, size);
            var effectiveOwner = WindowOwnerResolver.ResolveUsableOwnerWindow(owner);
            if (effectiveOwner != null)
                window.Show(effectiveOwner);
            else
                window.Show();
            return window;
        }

        /// <summary>Takes the section control out of the window without firing re-dock (used by CONTRACT).</summary>
        public Control? TakeContentWithoutRedock()
        {
            Closed -= OnClosed;
            return ExtractSectionControl();
        }

        private void OnClosed(object? sender, EventArgs e)
        {
            Closed -= OnClosed;
            var section = ExtractSectionControl();
            ClosedWithContent?.Invoke(this, section);
        }

        private Control? ExtractSectionControl()
        {
            if (Content is Border border)
            {
                var child = border.Child as Control;
                border.Child = null;
                Content = null;
                return child;
            }

            if (Content is Control direct)
            {
                Content = null;
                return direct;
            }

            return null;
        }
    }
}
