using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.Media;

namespace RPGGame.UI.Avalonia.Settings
{
    /// <summary>
    /// Modal weapon picker for Action Interaction Lab — builds a throwaway character without a save file.
    /// </summary>
    public static class ActionLabWeaponPickerDialog
    {
        /// <summary>
        /// Shows the dialog. Returns 1-based weapon index for <see cref="GameInitializationManager.InitializeNewCharacter"/>, or null if cancelled.
        /// </summary>
        public static async Task<int?> ShowAsync(Window? owner)
        {
            var init = new GameInitializer();
            var gear = init.LoadStartingGear();
            var weapons = gear?.weapons ?? new List<StartingWeapon>();
            if (weapons.Count == 0)
            {
                weapons = new List<StartingWeapon>
                {
                    new StartingWeapon { name = "Mace", damage = 7.5, attackSpeed = 0.8 },
                    new StartingWeapon { name = "Sword", damage = 6.0, attackSpeed = 1.0 },
                    new StartingWeapon { name = "Dagger", damage = 4.3, attackSpeed = 1.2 },
                    new StartingWeapon { name = "Wand", damage = 5.5, attackSpeed = 1.1 }
                };
            }

            var dialog = new Window
            {
                Title = "Action Interaction Lab",
                Width = 440,
                Height = 340,
                WindowStartupLocation = WindowStartupLocation.CenterOwner,
                CanResize = false,
            };

            var hint = new TextBlock
            {
                Text = "Choose a starter weapon for the lab character. This build is only used in the lab and is not saved.",
                Margin = new Thickness(16, 16, 16, 8),
                TextWrapping = TextWrapping.Wrap,
            };

            var labels = weapons
                .Select(w => $"{w.name}  (dmg ~{w.damage:0.#}, speed {w.attackSpeed:0.##})")
                .ToList();

            var list = new ListBox
            {
                Margin = new Thickness(16, 0, 16, 8),
                MinHeight = 168,
                ItemsSource = labels,
            };
            if (labels.Count > 0)
                list.SelectedIndex = 0;

            var ok = new Button
            {
                Content = "Start lab",
                MinWidth = 100,
                HorizontalAlignment = HorizontalAlignment.Right,
                IsDefault = true,
            };
            var cancel = new Button
            {
                Content = "Cancel",
                MinWidth = 88,
                HorizontalAlignment = HorizontalAlignment.Right,
            };

            void CloseWith(int? choice)
            {
                dialog.Close(choice);
            }

            ok.Click += (_, _) =>
            {
                int idx = list.SelectedIndex;
                if (idx >= 0 && idx < weapons.Count)
                    CloseWith(idx + 1);
            };
            cancel.Click += (_, _) => CloseWith(null);

            var buttons = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                HorizontalAlignment = HorizontalAlignment.Right,
                Spacing = 10,
                Margin = new Thickness(16, 0, 16, 16),
                Children = { cancel, ok },
            };

            dialog.Content = new StackPanel
            {
                Children = { hint, list, buttons },
            };

            dialog.KeyDown += (_, e) =>
            {
                if (e.Key == Key.Escape)
                {
                    CloseWith(null);
                    e.Handled = true;
                }
            };

            Window? target = ResolveUsableOwnerWindow(owner);
            if (target == null)
                throw new InvalidOperationException(
                    "No visible window available to show the Action Lab weapon dialog. Close stale windows or reopen the app.");

            return await dialog.ShowDialog<int?>(target);
        }

        /// <summary>
        /// <see cref="ShowDialog"/> fails if the owner reference points at a closed window (e.g. stale <see cref="CanvasWindowManager"/> ref).
        /// Prefer a visible owner, then <see cref="IClassicDesktopStyleApplicationLifetime.MainWindow"/>, then any visible top-level window.
        /// </summary>
        private static Window? ResolveUsableOwnerWindow(Window? preferred)
        {
            static bool IsDialogOwnerUsable(Window? w)
            {
                if (w == null) return false;
                try
                {
                    return w.IsVisible;
                }
                catch
                {
                    return false;
                }
            }

            if (IsDialogOwnerUsable(preferred))
                return preferred;

            if (Application.Current?.ApplicationLifetime is not IClassicDesktopStyleApplicationLifetime life)
                return null;

            if (IsDialogOwnerUsable(life.MainWindow))
                return life.MainWindow;

            foreach (var w in life.Windows)
            {
                if (w is Window win && IsDialogOwnerUsable(win))
                    return win;
            }

            return null;
        }
    }
}
