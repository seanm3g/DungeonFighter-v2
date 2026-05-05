using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.LogicalTree;
using RPGGame.UI.Avalonia.Settings;
using RPGGame;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace RPGGame.UI.Avalonia.Managers
{
    public class ItemSuffixesTabManager
    {
        private readonly Action<string, bool>? showStatusMessage;
        private readonly ItemSuffixesDataCoordinator dataCoordinator;
        private ObservableCollection<SuffixRarityGroupViewModel>? groups;
        private ItemSuffixesSettingsPanel? panel;
        private SuffixModifierViewModel? selected;
        private Dictionary<int, StatBonus> originals = new Dictionary<int, StatBonus>();

        public ItemSuffixesTabManager(Action<string, bool>? showStatusMessage = null)
        {
            this.showStatusMessage = showStatusMessage;
            var dataService = new ItemSuffixesDataService(showStatusMessage);
            dataCoordinator = new ItemSuffixesDataCoordinator(dataService, showStatusMessage);
        }

        public void Initialize(ItemSuffixesSettingsPanel p)
        {
            panel = p;
            var addButton = panel.FindControl<Button>("AddSuffixButton");
            var editButton = panel.FindControl<Button>("EditSuffixButton");
            var deleteButton = panel.FindControl<Button>("DeleteSuffixButton");
            if (addButton != null) addButton.Click += OnAddClick;
            if (editButton != null) editButton.Click += OnEditClick;
            if (deleteButton != null) deleteButton.Click += OnDeleteClick;
            panel.SuffixSelected += OnSuffixSelected;
            try
            {
                LoadData(panel);
            }
            catch (Exception ex)
            {
                showStatusMessage?.Invoke($"Error loading suffixes: {ex.Message}", false);
            }
        }

        public void SaveSuffixes()
        {
            if (groups == null)
                return;

            try
            {
                dataCoordinator.SaveSuffixes(groups, originals);
                if (panel != null)
                    LoadData(panel);
            }
            catch (Exception ex)
            {
                showStatusMessage?.Invoke($"Error saving suffixes: {ex.Message}", false);
            }
        }

        /// <summary>After spreadsheet PULL or other external reload, refresh if this tab was opened.</summary>
        public void RefreshFromFileIfLoaded()
        {
            if (panel == null) return;
            try
            {
                selected = null;
                panel.SetSelectedSuffix(null);
                panel.SetButtonStates(false, false);
                LoadData(panel);
            }
            catch (Exception ex)
            {
                showStatusMessage?.Invoke($"Error refreshing suffixes tab: {ex.Message}", false);
            }
        }

        private void LoadData(ItemSuffixesSettingsPanel p)
        {
            var (loaded, orig) = dataCoordinator.LoadSuffixesData();
            groups = loaded;
            originals = orig;
            var itemsControl = p.FindControl<ItemsControl>("SuffixGroupsItemsControl");
            if (itemsControl != null)
                itemsControl.ItemsSource = groups;
        }

        private void OnSuffixSelected(object? sender, SuffixModifierViewModel? vm)
        {
            selected = vm;
            panel?.SetButtonStates(vm != null, vm != null);
        }

        private async void OnAddClick(object? sender, RoutedEventArgs e)
        {
            var rarities = dataCoordinator.GetAvailableRarities();
            int nextId = dataCoordinator.GetNextEntryId(groups, originals);
            var vm = new StatSuffixEditViewModel
            {
                Rarity = rarities.FirstOrDefault() ?? "Common",
                IsEditMode = false
            };
            foreach (var r in rarities)
                vm.AvailableRarities.Add(r);

            var dialog = new StatSuffixEditDialog(vm);
            if (panel == null) return;
            var window = panel.GetLogicalAncestors().OfType<Window>().FirstOrDefault();
            if (window == null) return;
            dialog.WindowStartupLocation = WindowStartupLocation.CenterOwner;
            var result = await dialog.ShowDialog<bool?>(window);
            if (result != true || vm.GetValidationError() != null) return;

            AddRow(nextId, vm);
        }

        private async void OnEditClick(object? sender, RoutedEventArgs e)
        {
            if (selected == null)
            {
                showStatusMessage?.Invoke("No suffix selected", false);
                return;
            }

            var rarities = dataCoordinator.GetAvailableRarities();
            var vm = new StatSuffixEditViewModel
            {
                Name = selected.Name,
                Description = selected.Description,
                Rarity = selected.SelectedRarity,
                ItemRank = selected.ItemRank,
                StatType = selected.StatType,
                Value = selected.Value.ToString(System.Globalization.CultureInfo.InvariantCulture),
                MechanicsText = selected.MechanicsText,
                IsEditMode = true
            };
            foreach (var r in rarities)
                vm.AvailableRarities.Add(r);

            var dialog = new StatSuffixEditDialog(vm);
            if (panel == null) return;
            var window = panel.GetLogicalAncestors().OfType<Window>().FirstOrDefault();
            if (window == null) return;
            dialog.WindowStartupLocation = WindowStartupLocation.CenterOwner;
            var result = await dialog.ShowDialog<bool?>(window);
            if (result != true || vm.GetValidationError() != null) return;

            UpdateRow(selected, vm);
        }

        private void OnDeleteClick(object? sender, RoutedEventArgs e)
        {
            if (selected == null)
            {
                showStatusMessage?.Invoke("No suffix selected", false);
                return;
            }

            int id = selected.EntryId;
            if (groups != null)
            {
                foreach (var g in groups)
                {
                    var m = g.Modifiers.FirstOrDefault(x => x.EntryId == id);
                    if (m != null)
                    {
                        g.Modifiers.Remove(m);
                        break;
                    }
                }
            }

            originals.Remove(id);
            selected = null;
            panel?.SetSelectedSuffix(null);
            panel?.SetButtonStates(false, false);
            showStatusMessage?.Invoke("Suffix removed from the list. Click Save to write StatBonuses.json.", true);
        }

        private void AddRow(int entryId, StatSuffixEditViewModel editVm)
        {
            if (groups == null) return;
            var rarities = dataCoordinator.GetAvailableRarities();
            var row = new SuffixModifierViewModel
            {
                EntryId = entryId,
                Name = editVm.Name,
                Description = editVm.Description,
                CurrentRarity = editVm.Rarity,
                SelectedRarity = editVm.Rarity,
                ItemRank = editVm.ItemRank ?? "",
                StatType = editVm.StatType ?? "",
                Value = editVm.GetLegacyValueDouble(),
                MechanicsText = editVm.MechanicsText ?? ""
            };
            foreach (var r in rarities)
                row.AvailableRarities.Add(r);

            var group = groups.FirstOrDefault(g => g.RarityName == editVm.Rarity);
            if (group == null)
            {
                group = new SuffixRarityGroupViewModel { RarityName = editVm.Rarity };
                groups.Add(group);
                SortGroups();
                group = groups.FirstOrDefault(g => g.RarityName == editVm.Rarity);
            }

            group?.Modifiers.Add(row);
            SortModifiersInGroup(group!);
            showStatusMessage?.Invoke($"Suffix '{editVm.Name}' added. Click Save to apply.", true);
        }

        private void UpdateRow(SuffixModifierViewModel row, StatSuffixEditViewModel editVm)
        {
            row.Name = editVm.Name;
            row.Description = editVm.Description;
            row.SelectedRarity = editVm.Rarity;
            row.ItemRank = editVm.ItemRank ?? "";
            row.StatType = editVm.StatType ?? "";
            row.Value = editVm.GetLegacyValueDouble();
            row.MechanicsText = editVm.MechanicsText ?? "";

            if (row.CurrentRarity != editVm.Rarity && groups != null)
            {
                foreach (var g in groups)
                {
                    if (g.Modifiers.Remove(row))
                        break;
                }

                var newGroup = groups.FirstOrDefault(g => g.RarityName == editVm.Rarity);
                if (newGroup == null)
                {
                    newGroup = new SuffixRarityGroupViewModel { RarityName = editVm.Rarity };
                    groups.Add(newGroup);
                    SortGroups();
                    newGroup = groups.FirstOrDefault(g => g.RarityName == editVm.Rarity);
                }

                newGroup?.Modifiers.Add(row);
                row.CurrentRarity = editVm.Rarity;
                SortModifiersInGroup(newGroup!);
            }

            showStatusMessage?.Invoke($"Suffix '{editVm.Name}' updated. Click Save to apply.", true);
        }

        private void SortGroups()
        {
            if (groups == null) return;
            var sorted = groups.OrderBy(g => RarityOrder(g.RarityName)).ToList();
            groups.Clear();
            foreach (var g in sorted)
                groups.Add(g);
        }

        private static void SortModifiersInGroup(SuffixRarityGroupViewModel g)
        {
            var sorted = g.Modifiers.OrderBy(m => m.Name, StringComparer.OrdinalIgnoreCase).ToList();
            g.Modifiers.Clear();
            foreach (var m in sorted)
                g.Modifiers.Add(m);
        }

        private static int RarityOrder(string rarity)
        {
            return rarity.ToLowerInvariant() switch
            {
                "common" => 0,
                "uncommon" => 1,
                "rare" => 2,
                "epic" => 3,
                "legendary" => 4,
                "mythic" => 5,
                "transcendent" => 6,
                _ => 99
            };
        }
    }
}
