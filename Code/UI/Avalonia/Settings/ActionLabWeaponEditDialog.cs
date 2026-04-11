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
    /// <summary>Which gear row opened the Action Lab editor.</summary>
    public enum ActionLabGearEditSlot
    {
        Weapon,
        Head,
        Body,
        Feet,
    }

    /// <summary>
    /// Action Interaction Lab: pick base gear from game data, optional prefix modification and suffix stat bonus.
    /// </summary>
    public static class ActionLabWeaponEditDialog
    {
        private const string NoneLabel = "(None)";

        private const double BaseItemListMaxHeight = 200;

        /// <summary>Equip slot name passed to <see cref="ActionInteractionLabSession.ApplyLabGear"/>.</summary>
        public static string GetEquipSlotName(ActionLabGearEditSlot slot) => slot switch
        {
            ActionLabGearEditSlot.Weapon => "weapon",
            ActionLabGearEditSlot.Head => "head",
            ActionLabGearEditSlot.Body => "body",
            ActionLabGearEditSlot.Feet => "feet",
            _ => throw new ArgumentOutOfRangeException(nameof(slot))
        };

        /// <summary>Shows the weapon editor. Returns a new <see cref="WeaponItem"/> or null if cancelled.</summary>
        public static async Task<WeaponItem?> ShowAsync(Window? owner, Character? labPlayer)
        {
            var item = await ShowGearEditAsync(owner, labPlayer, ActionLabGearEditSlot.Weapon).ConfigureAwait(true);
            return item as WeaponItem;
        }

        /// <summary>Shows the editor for any lab gear slot. Returns a new item or null if cancelled.</summary>
        public static async Task<Item?> ShowGearEditAsync(Window? owner, Character? labPlayer, ActionLabGearEditSlot slot)
        {
            var cache = LootDataCache.Load();
            var mods = cache.Modifications
                .Where(m => !string.IsNullOrWhiteSpace(m.Name))
                .OrderBy(m => m.Name, StringComparer.OrdinalIgnoreCase)
                .ToList();

            var suffixes = cache.StatBonuses
                .Where(s => !string.IsNullOrWhiteSpace(s.Name))
                .OrderBy(s => s.Name, StringComparer.OrdinalIgnoreCase)
                .ToList();

            return slot switch
            {
                ActionLabGearEditSlot.Weapon => await ShowWeaponInternalAsync(owner, labPlayer, cache, mods, suffixes).ConfigureAwait(true),
                ActionLabGearEditSlot.Head => await ShowArmorInternalAsync(owner, labPlayer, cache, mods, suffixes, ActionLabGearEditSlot.Head, "head").ConfigureAwait(true),
                ActionLabGearEditSlot.Body => await ShowArmorInternalAsync(owner, labPlayer, cache, mods, suffixes, ActionLabGearEditSlot.Body, "body").ConfigureAwait(true),
                ActionLabGearEditSlot.Feet => await ShowArmorInternalAsync(owner, labPlayer, cache, mods, suffixes, ActionLabGearEditSlot.Feet, "feet").ConfigureAwait(true),
                _ => throw new ArgumentOutOfRangeException(nameof(slot)),
            };
        }

        private static async Task<Item?> ShowWeaponInternalAsync(
            Window? owner,
            Character? labPlayer,
            LootDataCache cache,
            List<Modification> mods,
            List<StatBonus> suffixes)
        {
            var weapons = cache.WeaponData
                .Where(w => !string.IsNullOrWhiteSpace(w.Type) && !string.IsNullOrWhiteSpace(w.Name))
                .OrderBy(w => w.Name, StringComparer.OrdinalIgnoreCase)
                .ToList();

            if (weapons.Count == 0)
            {
                await ShowEmptyMessageAsync(owner, "No weapons found in Weapons.json.", "Lab weapon").ConfigureAwait(true);
                return null;
            }

            var hint = "Choose a base weapon from Weapons.json, then optional prefix (modification) and suffix (stat bonus). Rolled mod values use a random value in range.";
            var dialog = CreateShellWindow("Action Lab — weapon", 520, 460, hint, out var stack, out var ok, out var cancel);

            var baseList = CreateBaseItemListBox();
            foreach (var w in weapons)
                baseList.Items.Add($"{w.Name}  [{w.Type}]  dmg {w.BaseDamage}  {w.AttackSpeed:0.##}s  T{w.Tier}");

            int defaultIdx = ActionLabWeaponFactory.FindBestWeaponDataIndex(weapons, labPlayer?.Weapon as WeaponItem);
            if (defaultIdx >= 0 && defaultIdx < baseList.Items.Count)
                baseList.SelectedIndex = defaultIdx;
            else if (baseList.Items.Count > 0)
                baseList.SelectedIndex = 0;

            var prefixLabel = new TextBlock { Text = "Prefix (modification)", Margin = new Thickness(16, 0, 16, 4) };
            var prefixItems = BuildPrefixItems(mods);
            var prefixCombo = new ComboBox
            {
                Margin = new Thickness(16, 0, 16, 8),
                ItemsSource = prefixItems,
            };
            SelectPrefixFromEquipped(prefixCombo, prefixItems, labPlayer?.Weapon);

            var suffixLabel = new TextBlock { Text = "Suffix (stat bonus)", Margin = new Thickness(16, 0, 16, 4) };
            var suffixItems = BuildSuffixItems(suffixes);
            var suffixCombo = new ComboBox
            {
                Margin = new Thickness(16, 0, 16, 8),
                ItemsSource = suffixItems,
            };
            SelectSuffixFromEquipped(suffixCombo, suffixItems, labPlayer?.Weapon);

            stack.Children.Add(new TextBlock { Text = "Weapon", Margin = new Thickness(16, 0, 16, 4), FontWeight = FontWeight.DemiBold });
            stack.Children.Add(baseList);
            stack.Children.Add(prefixLabel);
            stack.Children.Add(prefixCombo);
            stack.Children.Add(suffixLabel);
            stack.Children.Add(suffixCombo);
            stack.Children.Add(CreateButtonRow(cancel, ok));

            void CloseWith(Item? result) => dialog.Close(result);

            ok.Click += (_, _) =>
            {
                int wi = baseList.SelectedIndex;
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
                throw new InvalidOperationException("No window available for Action Lab gear dialog.");

            return await dialog.ShowDialog<Item?>(target);
        }

        private static async Task<Item?> ShowArmorInternalAsync(
            Window? owner,
            Character? labPlayer,
            LootDataCache cache,
            List<Modification> mods,
            List<StatBonus> suffixes,
            ActionLabGearEditSlot slot,
            string equipSlot)
        {
            string jsonSlot = ActionLabArmorFactory.ArmorJsonSlotFromEquipSlot(equipSlot);
            var armors = ActionLabArmorFactory.FilterArmorDataForEquipSlot(cache.ArmorData, equipSlot);

            if (armors.Count == 0)
            {
                await ShowEmptyMessageAsync(owner, $"No armor found in Armor.json for slot '{jsonSlot}'.", "Lab armor").ConfigureAwait(true);
                return null;
            }

            string title = slot switch
            {
                ActionLabGearEditSlot.Head => "Action Lab — head",
                ActionLabGearEditSlot.Body => "Action Lab — body",
                ActionLabGearEditSlot.Feet => "Action Lab — feet",
                _ => "Action Lab — armor",
            };

            string slotTitle = slot switch
            {
                ActionLabGearEditSlot.Head => "Head",
                ActionLabGearEditSlot.Body => "Body",
                ActionLabGearEditSlot.Feet => "Feet",
                _ => "Armor",
            };

            var hint = "Choose a base armor piece from Armor.json, then optional prefix (modification) and suffix (stat bonus). Rolled mod values use a random value in range.";
            var dialog = CreateShellWindow(title, 520, 460, hint, out var stack, out var ok, out var cancel);

            var baseList = CreateBaseItemListBox();
            foreach (var a in armors)
                baseList.Items.Add($"{a.Name}  arm {a.Armor}  T{a.Tier}");

            Item? equipped = equipSlot switch
            {
                "head" => labPlayer?.Head,
                "body" => labPlayer?.Body,
                "feet" => labPlayer?.Feet,
                _ => null,
            };

            int defaultIdx = ActionLabArmorFactory.FindBestArmorDataIndex(armors, equipped);
            if (defaultIdx >= 0 && defaultIdx < baseList.Items.Count)
                baseList.SelectedIndex = defaultIdx;
            else if (baseList.Items.Count > 0)
                baseList.SelectedIndex = 0;

            var prefixLabel = new TextBlock { Text = "Prefix (modification)", Margin = new Thickness(16, 0, 16, 4) };
            var prefixItems = BuildPrefixItems(mods);
            var prefixCombo = new ComboBox
            {
                Margin = new Thickness(16, 0, 16, 8),
                ItemsSource = prefixItems,
            };
            SelectPrefixFromEquipped(prefixCombo, prefixItems, equipped);

            var suffixLabel = new TextBlock { Text = "Suffix (stat bonus)", Margin = new Thickness(16, 0, 16, 4) };
            var suffixItems = BuildSuffixItems(suffixes);
            var suffixCombo = new ComboBox
            {
                Margin = new Thickness(16, 0, 16, 8),
                ItemsSource = suffixItems,
            };
            SelectSuffixFromEquipped(suffixCombo, suffixItems, equipped);

            stack.Children.Add(new TextBlock { Text = slotTitle, Margin = new Thickness(16, 0, 16, 4), FontWeight = FontWeight.DemiBold });
            stack.Children.Add(baseList);
            stack.Children.Add(prefixLabel);
            stack.Children.Add(prefixCombo);
            stack.Children.Add(suffixLabel);
            stack.Children.Add(suffixCombo);
            stack.Children.Add(CreateButtonRow(cancel, ok));

            void CloseWith(Item? result) => dialog.Close(result);

            ok.Click += (_, _) =>
            {
                int ai = baseList.SelectedIndex;
                if (ai < 0 || ai >= armors.Count)
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
                    var built = ActionLabArmorFactory.CreateArmor(armors[ai], prefix, suffix);
                    CloseWith(built);
                }
                catch
                {
                    CloseWith(null);
                }
            };

            cancel.Click += (_, _) => CloseWith(null);
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
                throw new InvalidOperationException("No window available for Action Lab gear dialog.");

            return await dialog.ShowDialog<Item?>(target);
        }

        private static Window CreateShellWindow(string title, double width, double height, string hintText, out StackPanel stack, out Button ok, out Button cancel)
        {
            ok = new Button { Content = "Apply", MinWidth = 100, HorizontalAlignment = HorizontalAlignment.Right, IsDefault = true };
            cancel = new Button { Content = "Cancel", MinWidth = 88, HorizontalAlignment = HorizontalAlignment.Right };

            stack = new StackPanel();
            stack.Children.Add(new TextBlock
            {
                Text = hintText,
                Margin = new Thickness(16, 16, 16, 8),
                TextWrapping = TextWrapping.Wrap,
            });

            return new Window
            {
                Title = title,
                Width = width,
                Height = height,
                WindowStartupLocation = WindowStartupLocation.CenterOwner,
                CanResize = false,
                Content = stack,
            };
        }

        private static StackPanel CreateButtonRow(Button cancel, Button ok) => new()
        {
            Orientation = Orientation.Horizontal,
            HorizontalAlignment = HorizontalAlignment.Right,
            Spacing = 10,
            Margin = new Thickness(16, 0, 16, 16),
            Children = { cancel, ok },
        };

        private static ListBox CreateBaseItemListBox() => new()
        {
            MinHeight = 120,
            MaxHeight = BaseItemListMaxHeight,
            Margin = new Thickness(16, 0, 16, 8),
        };

        private static async Task ShowEmptyMessageAsync(Window? owner, string message, string title)
        {
            var empty = new Window
            {
                Title = title,
                Width = 360,
                Height = 120,
                WindowStartupLocation = WindowStartupLocation.CenterOwner,
                Content = new TextBlock
                {
                    Margin = new Thickness(16),
                    Text = message,
                    TextWrapping = TextWrapping.Wrap,
                },
            };
            Window? t = ResolveOwner(owner);
            if (t == null)
                throw new InvalidOperationException("No window available for Action Lab gear dialog.");
            await empty.ShowDialog(t).ConfigureAwait(true);
        }

        private static void SelectPrefixFromEquipped(ComboBox combo, List<ComboModItem> items, Item? equipped)
        {
            if (equipped?.Modifications == null || equipped.Modifications.Count == 0)
            {
                combo.SelectedIndex = 0;
                return;
            }

            string want = equipped.Modifications[0].Name;
            for (int i = 0; i < items.Count; i++)
            {
                if (items[i].Template != null && string.Equals(items[i].Template!.Name, want, StringComparison.OrdinalIgnoreCase))
                {
                    combo.SelectedIndex = i;
                    return;
                }
            }

            combo.SelectedIndex = 0;
        }

        private static void SelectSuffixFromEquipped(ComboBox combo, List<ComboSuffixItem> items, Item? equipped)
        {
            if (equipped?.StatBonuses == null || equipped.StatBonuses.Count == 0)
            {
                combo.SelectedIndex = 0;
                return;
            }

            string want = equipped.StatBonuses[0].Name;
            for (int i = 0; i < items.Count; i++)
            {
                if (items[i].Template != null && string.Equals(items[i].Template!.Name, want, StringComparison.OrdinalIgnoreCase))
                {
                    combo.SelectedIndex = i;
                    return;
                }
            }

            combo.SelectedIndex = 0;
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
