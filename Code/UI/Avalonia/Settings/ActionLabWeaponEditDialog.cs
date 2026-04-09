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
using RPGGame.ActionInteractionLab;

namespace RPGGame.UI.Avalonia.Settings
{
    /// <summary>
    /// Action Interaction Lab: pick a weapon from game data, optional prefix modification and suffix stat bonus.
    /// </summary>
    public static class ActionLabWeaponEditDialog
    {
        private const string NoneLabel = "(None)";

        /// <summary>Shows the editor. Returns a new <see cref="WeaponItem"/> or null if cancelled.</summary>
        public static async Task<WeaponItem?> ShowAsync(Window? owner, Character? labPlayer)
        {
            var cache = LootDataCache.Load();
            var weapons = cache.WeaponData
                .Where(w => !string.IsNullOrWhiteSpace(w.Type) && !string.IsNullOrWhiteSpace(w.Name))
                .OrderBy(w => w.Name, StringComparer.OrdinalIgnoreCase)
                .ToList();

            if (weapons.Count == 0)
            {
                var empty = new Window
                {
                    Title = "Lab weapon",
                    Width = 360,
                    Height = 120,
                    WindowStartupLocation = WindowStartupLocation.CenterOwner,
                    Content = new TextBlock
                    {
                        Margin = new Thickness(16),
                        Text = "No weapons found in Weapons.json.",
                        TextWrapping = TextWrapping.Wrap,
                    },
                };
                Window? t = ResolveOwner(owner);
                if (t == null)
                    throw new InvalidOperationException("No window available for Action Lab weapon dialog.");
                await empty.ShowDialog(t).ConfigureAwait(true);
                return null;
            }

            var mods = cache.Modifications
                .Where(m => !string.IsNullOrWhiteSpace(m.Name))
                .OrderBy(m => m.Name, StringComparer.OrdinalIgnoreCase)
                .ToList();

            var suffixes = cache.StatBonuses
                .Where(s => !string.IsNullOrWhiteSpace(s.Name))
                .OrderBy(s => s.Name, StringComparer.OrdinalIgnoreCase)
                .ToList();

            var dialog = new Window
            {
                Title = "Action Lab — weapon",
                Width = 520,
                Height = 420,
                WindowStartupLocation = WindowStartupLocation.CenterOwner,
                CanResize = false,
            };

            var hint = new TextBlock
            {
                Text = "Choose a base weapon from Weapons.json, then optional prefix (modification) and suffix (stat bonus). Rolled mod values use a random value in range.",
                Margin = new Thickness(16, 16, 16, 8),
                TextWrapping = TextWrapping.Wrap,
            };

            var weaponList = new ListBox
            {
                MinHeight = 140,
                Margin = new Thickness(16, 0, 16, 8),
            };
            foreach (var w in weapons)
            {
                weaponList.Items.Add($"{w.Name}  [{w.Type}]  dmg {w.BaseDamage}  {w.AttackSpeed:0.##}s  T{w.Tier}");
            }

            int defaultIdx = ActionLabWeaponFactory.FindBestWeaponDataIndex(weapons, labPlayer?.Weapon as WeaponItem);
            if (defaultIdx >= 0 && defaultIdx < weaponList.Items.Count)
                weaponList.SelectedIndex = defaultIdx;
            else if (weaponList.Items.Count > 0)
                weaponList.SelectedIndex = 0;

            var prefixLabel = new TextBlock { Text = "Prefix (modification)", Margin = new Thickness(16, 0, 16, 4) };
            var prefixCombo = new ComboBox
            {
                Margin = new Thickness(16, 0, 16, 8),
                ItemsSource = BuildPrefixItems(mods),
            };
            prefixCombo.SelectedIndex = 0;

            var suffixLabel = new TextBlock { Text = "Suffix (stat bonus)", Margin = new Thickness(16, 0, 16, 4) };
            var suffixCombo = new ComboBox
            {
                Margin = new Thickness(16, 0, 16, 8),
                ItemsSource = BuildSuffixItems(suffixes),
            };
            suffixCombo.SelectedIndex = 0;

            var ok = new Button { Content = "Apply", MinWidth = 100, HorizontalAlignment = HorizontalAlignment.Right, IsDefault = true };
            var cancel = new Button { Content = "Cancel", MinWidth = 88, HorizontalAlignment = HorizontalAlignment.Right };

            void CloseWith(WeaponItem? result) => dialog.Close(result);

            ok.Click += (_, _) =>
            {
                int wi = weaponList.SelectedIndex;
                if (wi < 0 || wi >= weapons.Count)
                {
                    CloseWith(null);
                    return;
                }

                Modification? prefix = null;
                if (prefixCombo.SelectedItem is ComboModItem p && p.Template != null)
                    prefix = p.Template;

                StatBonus? suffix = null;
                if (suffixCombo.SelectedItem is ComboSuffixItem s && s.Template != null)
                    suffix = s.Template;

                try
                {
                    var built = ActionLabWeaponFactory.CreateWeapon(weapons[wi], prefix, suffix);
                    CloseWith(built);
                }
                catch
                {
                    CloseWith(null);
                }
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
                Children =
                {
                    hint,
                    new TextBlock { Text = "Weapon", Margin = new Thickness(16, 0, 16, 4), FontWeight = FontWeight.DemiBold },
                    weaponList,
                    prefixLabel,
                    prefixCombo,
                    suffixLabel,
                    suffixCombo,
                    buttons,
                },
            };

            dialog.KeyDown += (_, e) =>
            {
                if (e.Key == Key.Escape)
                {
                    CloseWith(null);
                    e.Handled = true;
                }
            };

            Window? target = ResolveOwner(owner);
            if (target == null)
                throw new InvalidOperationException("No window available for Action Lab weapon dialog.");

            return await dialog.ShowDialog<WeaponItem?>(target);
        }

        private static List<ComboModItem> BuildPrefixItems(List<Modification> mods)
        {
            var list = new List<ComboModItem> { new(NoneLabel, null) };
            foreach (var m in mods)
                list.Add(new ComboModItem($"{m.Name}  ({m.ItemRank}, {m.Effect})", m));
            return list;
        }

        private static List<ComboSuffixItem> BuildSuffixItems(List<StatBonus> statBonuses)
        {
            var list = new List<ComboSuffixItem> { new(NoneLabel, null) };
            foreach (var s in statBonuses)
                list.Add(new ComboSuffixItem($"{s.Name}  — {s.Description}", s));
            return list;
        }

        private static Window? ResolveOwner(Window? owner)
        {
            if (owner != null)
                return owner;
            if (Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
                return desktop.MainWindow;
            return null;
        }

        private sealed class ComboModItem
        {
            public ComboModItem(string label, Modification? template)
            {
                Label = label;
                Template = template;
            }

            public string Label { get; }
            public Modification? Template { get; }

            public override string ToString() => Label;
        }

        private sealed class ComboSuffixItem
        {
            public ComboSuffixItem(string label, StatBonus? template)
            {
                Label = label;
                Template = template;
            }

            public string Label { get; }
            public StatBonus? Template { get; }

            public override string ToString() => Label;
        }
    }
}
