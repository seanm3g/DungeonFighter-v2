using System;
using System.Collections;
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
    /// Action Interaction Lab: pick base gear from game data, optional prefix modifications and suffix stat bonuses (multi-select).
    /// </summary>
    public static class ActionLabWeaponEditDialog
    {
        /// <summary>Default dialog width; lists use a star grid so height drives usability.</summary>
        private const double DefaultShellWidth = 560;

        /// <summary>Default dialog height (~1000px) so weapon/prefix/suffix lists are not overly compressed.</summary>
        private const double DefaultShellHeight = 1000;

        private const double BaseItemListMinHeight = 140;

        private const double AffixListMinHeight = 96;

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

            var hint = "Choose a base weapon from Weapons.json, then optional prefixes (modifications) and suffixes (stat bonuses). Click rows to toggle selection; multiple rows allowed. Rolled mod values use a random value in range. Use type and rarity filters to narrow lists — rarity uses each prefix’s ItemRank; suffixes use optional ItemRank in StatBonuses.json (blank = all rarities); weapons use tier bands (T1 Common … T5 Legendary+, with Mythic/Transcendent showing tier-5 bases).";
            var rarityOptions = BuildRarityComboOptions(cache);
            var typeCombo = new ComboBox
            {
                MinWidth = 120,
                ItemsSource = new[] { "All types", "Mace", "Sword", "Dagger", "Wand" },
                SelectedIndex = 0,
            };
            var rarityCombo = new ComboBox
            {
                MinWidth = 140,
                ItemsSource = rarityOptions,
                SelectedIndex = 0,
            };
            var filterStrip = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                Spacing = 10,
                Margin = new Thickness(16, 0, 16, 4),
                Children =
                {
                    new TextBlock { Text = "Weapon type", VerticalAlignment = VerticalAlignment.Center },
                    typeCombo,
                    new TextBlock { Text = "Rarity", VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(8, 0, 0, 0) },
                    rarityCombo,
                },
            };

            var dialog = CreateShellWindow("Action Lab — weapon", DefaultShellWidth, DefaultShellHeight, hint, filterStrip, out var grid, out var shellRowOffset, out var ok, out var cancel);

            var displayedWeapons = new List<WeaponData>();
            var baseList = CreateBaseItemListBox();

            var prefixLabel = new TextBlock { Text = "Prefixes (modifications) — multi-select", Margin = new Thickness(16, 0, 16, 4) };
            var prefixList = CreateAffixMultiSelectList(Array.Empty<ComboModItem>());

            var suffixLabel = new TextBlock { Text = "Suffixes (stat bonuses) — multi-select", Margin = new Thickness(16, 0, 16, 4) };
            var suffixList = CreateAffixMultiSelectList(Array.Empty<ComboSuffixItem>());

            string? SelectedTypeOrNull() =>
                typeCombo.SelectedIndex <= 0 ? null : typeCombo.SelectedItem as string;

            string? SelectedRarityOrNull() =>
                rarityCombo.SelectedIndex <= 0 ? null : rarityCombo.SelectedItem as string;

            void RebuildFilteredLists(bool preserveWeaponSelection)
            {
                string? wantType = SelectedTypeOrNull();
                string? wantRarity = SelectedRarityOrNull();

                int prevWeaponIdx = preserveWeaponSelection ? baseList.SelectedIndex : -1;
                WeaponData? prevWeapon = prevWeaponIdx >= 0 && prevWeaponIdx < displayedWeapons.Count
                    ? displayedWeapons[prevWeaponIdx]
                    : null;

                displayedWeapons.Clear();
                displayedWeapons.AddRange(weapons.Where(w =>
                    ActionLabGearCatalogFilter.WeaponMatchesTypeFilter(w, wantType)
                    && ActionLabGearCatalogFilter.WeaponMatchesRarityFilter(w, wantRarity)));

                baseList.Items.Clear();
                foreach (var w in displayedWeapons)
                    baseList.Items.Add($"{w.Name}  [{w.Type}]  dmg {w.BaseDamage}  {w.AttackSpeed:0.##}s  T{w.Tier}");

                if (displayedWeapons.Count == 0)
                {
                    baseList.SelectedIndex = -1;
                }
                else if (prevWeapon != null)
                {
                    int found = -1;
                    for (int i = 0; i < displayedWeapons.Count; i++)
                    {
                        if (string.Equals(displayedWeapons[i].Name, prevWeapon.Name, StringComparison.OrdinalIgnoreCase)
                            && string.Equals(displayedWeapons[i].Type, prevWeapon.Type, StringComparison.OrdinalIgnoreCase)
                            && displayedWeapons[i].Tier == prevWeapon.Tier)
                        {
                            found = i;
                            break;
                        }
                    }

                    baseList.SelectedIndex = found >= 0 ? found : 0;
                }
                else
                {
                    int defaultIdx = ActionLabWeaponFactory.FindBestWeaponDataIndex(displayedWeapons, labPlayer?.Weapon as WeaponItem);
                    if (defaultIdx >= 0 && defaultIdx < displayedWeapons.Count)
                        baseList.SelectedIndex = defaultIdx;
                    else
                        baseList.SelectedIndex = 0;
                }

                var prefixItems = BuildPrefixItems(mods.Where(m => ActionLabGearCatalogFilter.ModificationMatchesRarityFilter(m, wantRarity)).ToList());
                prefixList.ItemsSource = prefixItems;
                SelectPrefixesFromEquipped(prefixList, prefixItems, labPlayer?.Weapon);

                var suffixItems = BuildSuffixItems(suffixes.Where(s => ActionLabGearCatalogFilter.StatBonusMatchesRarityFilter(s, wantRarity)).ToList());
                suffixList.ItemsSource = suffixItems;
                SelectSuffixesFromEquipped(suffixList, suffixItems, labPlayer?.Weapon);
            }

            typeCombo.SelectionChanged += (_, _) => RebuildFilteredLists(preserveWeaponSelection: true);
            rarityCombo.SelectionChanged += (_, _) => RebuildFilteredLists(preserveWeaponSelection: true);
            RebuildFilteredLists(preserveWeaponSelection: false);

            AddShellRow(grid, 1 + shellRowOffset, new TextBlock { Text = "Weapon", Margin = new Thickness(16, 0, 16, 4), FontWeight = FontWeight.DemiBold });
            AddShellRow(grid, 2 + shellRowOffset, baseList);
            AddShellRow(grid, 3 + shellRowOffset, prefixLabel);
            AddShellRow(grid, 4 + shellRowOffset, prefixList);
            AddShellRow(grid, 5 + shellRowOffset, suffixLabel);
            AddShellRow(grid, 6 + shellRowOffset, suffixList);
            AddShellRow(grid, 7 + shellRowOffset, CreateButtonRow(cancel, ok));

            void CloseWith(Item? result) => dialog.Close(result);

            ok.Click += (_, _) =>
            {
                int wi = baseList.SelectedIndex;
                if (wi < 0 || wi >= displayedWeapons.Count)
                {
                    CloseWith(null);
                    return;
                }

                var prefixListSel = CollectSelectedPrefixes(prefixList);
                var suffixListSel = CollectSelectedSuffixes(suffixList);

                try
                {
                    var built = ActionLabWeaponFactory.CreateWeapon(
                        displayedWeapons[wi],
                        prefixListSel.Count > 0 ? prefixListSel : null,
                        suffixListSel.Count > 0 ? suffixListSel : null);
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

            var hint = "Choose a base armor piece from Armor.json, then optional prefixes (modifications) and suffixes (stat bonuses). Click rows to toggle selection; multiple rows allowed. Rolled mod values use a random value in range. Use armor class and rarity filters to narrow lists — class is the name without the last word (e.g. Cloth Cap → Cloth; Studded Leather Boots → Studded Leather); rarity uses each prefix’s ItemRank; suffixes use optional ItemRank in StatBonuses.json (blank = all rarities); armor bases use the same tier→rarity bands as weapons (T1 Common … T5 Legendary+, with Mythic/Transcendent showing tier-5 bases).";
            var rarityOptions = BuildRarityComboOptions(cache);
            var classOptions = new List<string> { "All classes" };
            foreach (var c in armors
                         .Select(a => ActionLabGearCatalogFilter.GetArmorCatalogClass(a.Name))
                         .Where(s => !string.IsNullOrWhiteSpace(s))
                         .Distinct(StringComparer.OrdinalIgnoreCase)
                         .OrderBy(s => s, StringComparer.OrdinalIgnoreCase))
            {
                classOptions.Add(c);
            }

            var classCombo = new ComboBox
            {
                MinWidth = 140,
                ItemsSource = classOptions,
                SelectedIndex = 0,
            };
            var rarityCombo = new ComboBox
            {
                MinWidth = 140,
                ItemsSource = rarityOptions,
                SelectedIndex = 0,
            };
            var filterStrip = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                Spacing = 10,
                Margin = new Thickness(16, 0, 16, 4),
                Children =
                {
                    new TextBlock { Text = "Armor class", VerticalAlignment = VerticalAlignment.Center },
                    classCombo,
                    new TextBlock { Text = "Rarity", VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(8, 0, 0, 0) },
                    rarityCombo,
                },
            };

            var dialog = CreateShellWindow(title, DefaultShellWidth, DefaultShellHeight, hint, filterStrip, out var grid, out var shellRowOffset, out var ok, out var cancel);

            var displayedArmors = new List<ArmorData>();
            var baseList = CreateBaseItemListBox();

            Item? equipped = equipSlot switch
            {
                "head" => labPlayer?.Head,
                "body" => labPlayer?.Body,
                "feet" => labPlayer?.Feet,
                _ => null,
            };

            var prefixLabel = new TextBlock { Text = "Prefixes (modifications) — multi-select", Margin = new Thickness(16, 0, 16, 4) };
            var prefixList = CreateAffixMultiSelectList(Array.Empty<ComboModItem>());

            var suffixLabel = new TextBlock { Text = "Suffixes (stat bonuses) — multi-select", Margin = new Thickness(16, 0, 16, 4) };
            var suffixList = CreateAffixMultiSelectList(Array.Empty<ComboSuffixItem>());

            string? SelectedClassOrNull() =>
                classCombo.SelectedIndex <= 0 ? null : classCombo.SelectedItem as string;

            string? SelectedRarityOrNull() =>
                rarityCombo.SelectedIndex <= 0 ? null : rarityCombo.SelectedItem as string;

            void RebuildFilteredLists(bool preserveArmorSelection)
            {
                string? wantClass = SelectedClassOrNull();
                string? wantRarity = SelectedRarityOrNull();

                int prevIdx = preserveArmorSelection ? baseList.SelectedIndex : -1;
                ArmorData? prev = prevIdx >= 0 && prevIdx < displayedArmors.Count
                    ? displayedArmors[prevIdx]
                    : null;

                displayedArmors.Clear();
                displayedArmors.AddRange(armors.Where(a =>
                    ActionLabGearCatalogFilter.ArmorMatchesClassFilter(a, wantClass)
                    && ActionLabGearCatalogFilter.ArmorMatchesRarityFilter(a, wantRarity)));

                baseList.Items.Clear();
                foreach (var a in displayedArmors)
                    baseList.Items.Add($"{a.Name}  arm {a.Armor}  T{a.Tier}");

                if (displayedArmors.Count == 0)
                {
                    baseList.SelectedIndex = -1;
                }
                else if (prev != null)
                {
                    int found = -1;
                    for (int i = 0; i < displayedArmors.Count; i++)
                    {
                        if (string.Equals(displayedArmors[i].Name, prev.Name, StringComparison.OrdinalIgnoreCase)
                            && string.Equals(displayedArmors[i].Slot, prev.Slot, StringComparison.OrdinalIgnoreCase)
                            && displayedArmors[i].Tier == prev.Tier)
                        {
                            found = i;
                            break;
                        }
                    }

                    baseList.SelectedIndex = found >= 0 ? found : 0;
                }
                else
                {
                    int defaultIdx = ActionLabArmorFactory.FindBestArmorDataIndex(displayedArmors, equipped);
                    if (defaultIdx >= 0 && defaultIdx < displayedArmors.Count)
                        baseList.SelectedIndex = defaultIdx;
                    else
                        baseList.SelectedIndex = 0;
                }

                var prefixItems = BuildPrefixItems(mods.Where(m => ActionLabGearCatalogFilter.ModificationMatchesRarityFilter(m, wantRarity)).ToList());
                prefixList.ItemsSource = prefixItems;
                SelectPrefixesFromEquipped(prefixList, prefixItems, equipped);

                var suffixItems = BuildSuffixItems(suffixes.Where(s => ActionLabGearCatalogFilter.StatBonusMatchesRarityFilter(s, wantRarity)).ToList());
                suffixList.ItemsSource = suffixItems;
                SelectSuffixesFromEquipped(suffixList, suffixItems, equipped);
            }

            classCombo.SelectionChanged += (_, _) => RebuildFilteredLists(preserveArmorSelection: true);
            rarityCombo.SelectionChanged += (_, _) => RebuildFilteredLists(preserveArmorSelection: true);
            RebuildFilteredLists(preserveArmorSelection: false);

            AddShellRow(grid, 1 + shellRowOffset, new TextBlock { Text = slotTitle, Margin = new Thickness(16, 0, 16, 4), FontWeight = FontWeight.DemiBold });
            AddShellRow(grid, 2 + shellRowOffset, baseList);
            AddShellRow(grid, 3 + shellRowOffset, prefixLabel);
            AddShellRow(grid, 4 + shellRowOffset, prefixList);
            AddShellRow(grid, 5 + shellRowOffset, suffixLabel);
            AddShellRow(grid, 6 + shellRowOffset, suffixList);
            AddShellRow(grid, 7 + shellRowOffset, CreateButtonRow(cancel, ok));

            void CloseWith(Item? result) => dialog.Close(result);

            ok.Click += (_, _) =>
            {
                int ai = baseList.SelectedIndex;
                if (ai < 0 || ai >= displayedArmors.Count)
                {
                    CloseWith(null);
                    return;
                }

                var prefixListSel = CollectSelectedPrefixes(prefixList);
                var suffixListSel = CollectSelectedSuffixes(suffixList);

                try
                {
                    IReadOnlyList<Modification>? prefixTemplates = prefixListSel.Count > 0 ? prefixListSel : null;
                    IReadOnlyList<StatBonus>? suffixTemplates = suffixListSel.Count > 0 ? suffixListSel : null;
                    var built = ActionLabArmorFactory.CreateArmor(displayedArmors[ai], prefixTemplates, suffixTemplates);
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

        private static void AddShellRow(Grid grid, int row, Control control)
        {
            Grid.SetRow(control, row);
            grid.Children.Add(control);
        }

        private static Window CreateShellWindow(
            string title,
            double width,
            double height,
            string hintText,
            Control? filterStrip,
            out Grid grid,
            out int shellRowOffset,
            out Button ok,
            out Button cancel)
        {
            shellRowOffset = filterStrip != null ? 1 : 0;

            ok = new Button { Content = "Apply", MinWidth = 100, HorizontalAlignment = HorizontalAlignment.Right, IsDefault = true };
            cancel = new Button { Content = "Cancel", MinWidth = 88, HorizontalAlignment = HorizontalAlignment.Right };

            var hintBlock = new TextBlock
            {
                Text = hintText,
                Margin = new Thickness(16, 16, 16, 8),
                TextWrapping = TextWrapping.Wrap,
            };

            grid = new Grid();
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            if (filterStrip != null)
                grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            grid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            grid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            grid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

            int row = 0;
            Grid.SetRow(hintBlock, row++);
            grid.Children.Add(hintBlock);
            if (filterStrip != null)
            {
                Grid.SetRow(filterStrip, row++);
                grid.Children.Add(filterStrip);
            }

            return new Window
            {
                Title = title,
                Width = width,
                Height = height,
                MinWidth = 480,
                MinHeight = 520,
                WindowStartupLocation = WindowStartupLocation.CenterOwner,
                CanResize = true,
                Content = grid,
            };
        }

        private static List<string> BuildRarityComboOptions(LootDataCache cache)
        {
            var list = new List<string> { "All rarities" };
            if (cache.RarityData != null && cache.RarityData.Count > 0)
            {
                foreach (var r in cache.RarityData)
                {
                    if (string.IsNullOrWhiteSpace(r.Name))
                        continue;
                    string n = r.Name.Trim();
                    if (!list.Any(x => string.Equals(x, n, StringComparison.OrdinalIgnoreCase)))
                        list.Add(n);
                }

                return list;
            }

            foreach (var n in ActionLabGearCatalogFilter.RarityOrder)
            {
                if (!list.Any(x => string.Equals(x, n, StringComparison.OrdinalIgnoreCase)))
                    list.Add(n);
            }

            return list;
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
            MinHeight = BaseItemListMinHeight,
            VerticalAlignment = VerticalAlignment.Stretch,
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

        private static ListBox CreateAffixMultiSelectList(IEnumerable items)
        {
            return new ListBox
            {
                ItemsSource = items,
                MinHeight = AffixListMinHeight,
                VerticalAlignment = VerticalAlignment.Stretch,
                Margin = new Thickness(16, 0, 16, 8),
                SelectionMode = SelectionMode.Multiple | SelectionMode.Toggle,
            };
        }

        private static void ClearListBoxSelection(ListBox list)
        {
            if (list.SelectedItems is not IList li)
                return;
            while (li.Count > 0)
                li.RemoveAt(li.Count - 1);
        }

        private static void SelectPrefixesFromEquipped(ListBox list, List<ComboModItem> items, Item? equipped)
        {
            ClearListBoxSelection(list);
            if (equipped?.Modifications == null || equipped.Modifications.Count == 0)
                return;

            foreach (var mod in equipped.Modifications)
            {
                string want = mod.Name;
                foreach (var row in items)
                {
                    if (row.Template != null && string.Equals(row.Template.Name, want, StringComparison.OrdinalIgnoreCase))
                    {
                        list.SelectedItems?.Add(row);
                        break;
                    }
                }
            }
        }

        private static void SelectSuffixesFromEquipped(ListBox list, List<ComboSuffixItem> items, Item? equipped)
        {
            ClearListBoxSelection(list);
            if (equipped?.StatBonuses == null || equipped.StatBonuses.Count == 0)
                return;

            foreach (var sb in equipped.StatBonuses)
            {
                string want = sb.Name;
                foreach (var row in items)
                {
                    if (row.Template != null && string.Equals(row.Template.Name, want, StringComparison.OrdinalIgnoreCase))
                    {
                        list.SelectedItems?.Add(row);
                        break;
                    }
                }
            }
        }

        private static List<Modification> CollectSelectedPrefixes(ListBox list)
        {
            var result = new List<Modification>();
            if (list.SelectedItems == null)
                return result;
            foreach (var o in list.SelectedItems)
            {
                if (o is ComboModItem { Template: { } t })
                    result.Add(t);
            }
            return result;
        }

        private static List<StatBonus> CollectSelectedSuffixes(ListBox list)
        {
            var result = new List<StatBonus>();
            if (list.SelectedItems == null)
                return result;
            foreach (var o in list.SelectedItems)
            {
                if (o is ComboSuffixItem { Template: { } t })
                    result.Add(t);
            }
            return result;
        }

        private static List<ComboModItem> BuildPrefixItems(List<Modification> mods)
        {
            var list = new List<ComboModItem>();
            foreach (var m in mods)
                list.Add(new ComboModItem($"{m.Name}  ({m.ItemRank}, {m.Effect})", m));
            return list;
        }

        private static List<ComboSuffixItem> BuildSuffixItems(List<StatBonus> statBonuses)
        {
            var list = new List<ComboSuffixItem>();
            foreach (var s in statBonuses)
            {
                string rank = string.IsNullOrWhiteSpace(s.ItemRank) ? "" : $"  ({s.ItemRank})";
                list.Add(new ComboSuffixItem($"{s.Name}  — {s.Description}{rank}", s));
            }

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
