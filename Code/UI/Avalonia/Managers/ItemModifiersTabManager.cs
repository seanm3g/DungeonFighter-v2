using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.LogicalTree;
using RPGGame.UI.Avalonia.Settings;
using RPGGame.Data;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using Avalonia.Threading;

namespace RPGGame.UI.Avalonia.Managers
{
    /// <summary>
    /// Manages the Item Modifiers settings tab
    /// Handles loading, displaying, and saving modifier rarity assignments
    /// </summary>
    public class ItemModifiersTabManager
    {
        private readonly Action<string, bool>? showStatusMessage;
        private readonly ItemModifiersDataCoordinator dataCoordinator;
        private ObservableCollection<RarityGroupViewModel>? rarityGroups;
        private ItemModifiersSettingsPanel? panel;
        private ModifierViewModel? selectedModifier;
        private HashSet<int> deletedDiceResults = new HashSet<int>();
        private Dictionary<int, ItemModifiersDataService.ModificationData> originalModifications = new Dictionary<int, ItemModifiersDataService.ModificationData>();

        public ItemModifiersTabManager(Action<string, bool>? showStatusMessage = null)
        {
            this.showStatusMessage = showStatusMessage;
            var dataService = new ItemModifiersDataService(showStatusMessage);
            this.dataCoordinator = new ItemModifiersDataCoordinator(dataService, showStatusMessage);
        }

        /// <summary>
        /// Initialize the Item Modifiers panel with data
        /// </summary>
        public void Initialize(ItemModifiersSettingsPanel panel)
        {
            this.panel = panel;
            
            // Wire up button events
            var addButton = panel.FindControl<Button>("AddModifierButton");
            var editButton = panel.FindControl<Button>("EditModifierButton");
            var deleteButton = panel.FindControl<Button>("DeleteModifierButton");
            
            if (addButton != null) addButton.Click += OnAddModifierClick;
            if (editButton != null) editButton.Click += OnEditModifierClick;
            if (deleteButton != null) deleteButton.Click += OnDeleteModifierClick;
            
            // Wire up selection events
            panel.ModifierSelected += OnModifierSelected;
            
            try
            {
                LoadModifiersData(panel);
            }
            catch (Exception ex)
            {
                showStatusMessage?.Invoke($"Error loading modifiers: {ex.Message}", false);
            }
        }

        private void OnModifierSelected(object? sender, ModifierViewModel? modifier)
        {
            selectedModifier = modifier;
            bool canEdit = modifier != null;
            bool canDelete = modifier != null;
            panel?.SetButtonStates(canEdit, canDelete);
        }

        private async void OnAddModifierClick(object? sender, RoutedEventArgs e)
        {
            var availableRarities = dataCoordinator.GetAvailableRarities();
            var nextDiceResult = dataCoordinator.GetNextAvailableDiceResult(rarityGroups);
            
            var viewModel = new ItemModifierEditViewModel
            {
                DiceResult = nextDiceResult,
                Name = "",
                Description = "",
                Effect = "",
                ItemRank = "Common",
                MinValue = 0,
                MaxValue = 0,
                IsEditMode = false
            };
            
            foreach (var rarity in availableRarities)
            {
                viewModel.AvailableRarities.Add(rarity);
            }
            
            var dialog = new ItemModifierEditDialog(viewModel);
            if (panel != null)
            {
                var window = panel.GetLogicalAncestors().OfType<Window>().FirstOrDefault();
                if (window != null)
                {
                    dialog.WindowStartupLocation = WindowStartupLocation.CenterOwner;
                    var result = await dialog.ShowDialog<bool?>(window);
                    if (result == true && viewModel.Validate())
                    {
                        AddNewModifier(viewModel);
                    }
                }
            }
        }

        private async void OnEditModifierClick(object? sender, RoutedEventArgs e)
        {
            if (selectedModifier == null)
            {
                showStatusMessage?.Invoke("No modifier selected", false);
                return;
            }
            
            var availableRarities = dataCoordinator.GetAvailableRarities();
            var viewModel = new ItemModifierEditViewModel
            {
                DiceResult = selectedModifier.DiceResult,
                Name = selectedModifier.Name,
                Description = selectedModifier.Description,
                Effect = selectedModifier.Effect,
                ItemRank = selectedModifier.SelectedRarity,
                MinValue = selectedModifier.MinValue,
                MaxValue = selectedModifier.MaxValue,
                IsEditMode = true
            };
            
            foreach (var rarity in availableRarities)
            {
                viewModel.AvailableRarities.Add(rarity);
            }
            
            var dialog = new ItemModifierEditDialog(viewModel);
            if (panel != null)
            {
                var window = panel.GetLogicalAncestors().OfType<Window>().FirstOrDefault();
                if (window != null)
                {
                    dialog.WindowStartupLocation = WindowStartupLocation.CenterOwner;
                    var result = await dialog.ShowDialog<bool?>(window);
                    if (result == true && viewModel.Validate())
                    {
                        UpdateModifier(selectedModifier, viewModel);
                    }
                }
            }
        }

        private void OnDeleteModifierClick(object? sender, RoutedEventArgs e)
        {
            if (selectedModifier == null)
            {
                showStatusMessage?.Invoke("No modifier selected", false);
                return;
            }
            
            // Mark for deletion
            deletedDiceResults.Add(selectedModifier.DiceResult);
            
            // Remove from UI
            if (rarityGroups != null)
            {
                foreach (var group in rarityGroups)
                {
                    var modifierToRemove = group.Modifiers.FirstOrDefault(m => m.DiceResult == selectedModifier.DiceResult);
                    if (modifierToRemove != null)
                    {
                        group.Modifiers.Remove(modifierToRemove);
                        break;
                    }
                }
            }
            
            selectedModifier = null;
            panel?.SetSelectedModifier(null);
            panel?.SetButtonStates(false, false);
            showStatusMessage?.Invoke("Modifier marked for deletion. Click Save to apply changes.", true);
        }

        private void AddNewModifier(ItemModifierEditViewModel viewModel)
        {
            if (rarityGroups == null) return;
            
            var availableRarities = dataCoordinator.GetAvailableRarities();
            var modifierVM = new ModifierViewModel
            {
                DiceResult = viewModel.DiceResult,
                Name = viewModel.Name,
                Description = viewModel.Description,
                Effect = viewModel.Effect,
                CurrentRarity = viewModel.ItemRank,
                SelectedRarity = viewModel.ItemRank,
                MinValue = viewModel.MinValue,
                MaxValue = viewModel.MaxValue
            };
            
            foreach (var rarity in availableRarities)
            {
                modifierVM.AvailableRarities.Add(rarity);
            }
            
            // Find or create the rarity group
            var rarityGroup = rarityGroups.FirstOrDefault(g => g.RarityName == viewModel.ItemRank);
            if (rarityGroup == null)
            {
                rarityGroup = new RarityGroupViewModel { RarityName = viewModel.ItemRank };
                rarityGroups.Add(rarityGroup);
                // Re-sort groups
                var sorted = rarityGroups.OrderBy(g => GetRarityOrderInternal(g.RarityName)).ToList();
                rarityGroups.Clear();
                foreach (var group in sorted)
                {
                    rarityGroups.Add(group);
                }
                rarityGroup = rarityGroups.FirstOrDefault(g => g.RarityName == viewModel.ItemRank);
            }
            
            if (rarityGroup != null)
            {
                rarityGroup.Modifiers.Add(modifierVM);
                // Sort modifiers by DiceResult
                var sorted = rarityGroup.Modifiers.OrderBy(m => m.DiceResult).ToList();
                rarityGroup.Modifiers.Clear();
                foreach (var mod in sorted)
                {
                    rarityGroup.Modifiers.Add(mod);
                }
            }
            
            showStatusMessage?.Invoke($"Modifier '{viewModel.Name}' added. Click Save to apply changes.", true);
        }

        private void UpdateModifier(ModifierViewModel modifierVM, ItemModifierEditViewModel viewModel)
        {
            modifierVM.Name = viewModel.Name;
            modifierVM.Description = viewModel.Description;
            modifierVM.Effect = viewModel.Effect;
            modifierVM.SelectedRarity = viewModel.ItemRank;
            modifierVM.MinValue = viewModel.MinValue;
            modifierVM.MaxValue = viewModel.MaxValue;
            
            // If rarity changed, move to correct group
            if (modifierVM.CurrentRarity != viewModel.ItemRank && rarityGroups != null)
            {
                // Remove from old group
                foreach (var group in rarityGroups)
                {
                    if (group.Modifiers.Remove(modifierVM))
                    {
                        break;
                    }
                }
                
                // Add to new group
                var newGroup = rarityGroups.FirstOrDefault(g => g.RarityName == viewModel.ItemRank);
                if (newGroup == null)
                {
                    newGroup = new RarityGroupViewModel { RarityName = viewModel.ItemRank };
                    rarityGroups.Add(newGroup);
                    // Re-sort groups
                    var sorted = rarityGroups.OrderBy(g => GetRarityOrderInternal(g.RarityName)).ToList();
                    rarityGroups.Clear();
                    foreach (var group in sorted)
                    {
                        rarityGroups.Add(group);
                    }
                    newGroup = rarityGroups.FirstOrDefault(g => g.RarityName == viewModel.ItemRank);
                }
                
                if (newGroup != null)
                {
                    modifierVM.CurrentRarity = viewModel.ItemRank;
                    newGroup.Modifiers.Add(modifierVM);
                    // Sort modifiers by DiceResult
                    var sorted = newGroup.Modifiers.OrderBy(m => m.DiceResult).ToList();
                    newGroup.Modifiers.Clear();
                    foreach (var mod in sorted)
                    {
                        newGroup.Modifiers.Add(mod);
                    }
                }
            }
            
            showStatusMessage?.Invoke($"Modifier '{viewModel.Name}' updated. Click Save to apply changes.", true);
        }

        /// <summary>
        /// Load modifiers from Modifications.json and group by rarity
        /// </summary>
        private void LoadModifiersData(ItemModifiersSettingsPanel panel)
        {
            var (loadedGroups, loadedOriginals) = dataCoordinator.LoadModifiersData();
            rarityGroups = loadedGroups;
            originalModifications = loadedOriginals;
            
            // Bind to ItemsControl
            var itemsControl = panel.FindControl<ItemsControl>("RarityGroupsItemsControl");
            if (itemsControl != null)
            {
                itemsControl.ItemsSource = rarityGroups;
            }
        }


        /// <summary>
        /// Save modifier rarity assignments back to Modifications.json
        /// </summary>
        public void SaveModifierRarities()
        {
            if (rarityGroups == null)
            {
                showStatusMessage?.Invoke("No modifier data to save", false);
                return;
            }

            try
            {
                dataCoordinator.SaveModifiers(rarityGroups, originalModifications);
                
                // Reload data to refresh UI
                if (panel != null)
                {
                    LoadModifiersData(panel);
                }
                
                // Clear deletion tracking
                deletedDiceResults.Clear();
                originalModifications.Clear();
            }
            catch (Exception ex)
            {
                showStatusMessage?.Invoke($"Error saving modifiers: {ex.Message}", false);
            }
        }

        /// <summary>
        /// Get rarity order for sorting (lower = more common)
        /// </summary>
        private int GetRarityOrderInternal(string rarity)
        {
            return rarity.ToLower() switch
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

