using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.Media;
using RPGGame.UI.Avalonia.Helpers;

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
            _ = GameConfiguration.Instance;
            var weapons = GameInitializer.BuildStarterWeaponsForMenu();
            if (weapons.Count == 0)
                throw new InvalidOperationException("No starter weapons: add the \"starter\" tag to Weapons.json rows or ensure tier-1 weapons exist for every class path.");

            var dialog = new Window
            {
                Title = "Action Lab",
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

            var labels = new List<string>(weapons.Count);
            for (int i = 0; i < weapons.Count; i++)
            {
                var preview = GameInitializer.CreateStarterWeaponForMenuIndex(i + 1);
                labels.Add(
                    $"{preview.Name}  (dmg {preview.GetTotalDamage()}, speed {preview.GetTotalAttackSpeed():0.##}×)");
            }

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

            Window? target = WindowOwnerResolver.ResolveUsableOwnerWindow(owner);
            if (target == null)
                throw new InvalidOperationException(
                    "No visible window available to show the Action Lab weapon dialog. Close stale windows or reopen the app.");

            return await dialog.ShowDialog<int?>(target);
        }
    }
}
