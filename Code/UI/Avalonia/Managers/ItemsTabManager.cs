using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.LogicalTree;
using RPGGame.UI.Avalonia.Settings;
using RPGGame.Data;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Avalonia.Threading;
using RPGGame;

namespace RPGGame.UI.Avalonia.Managers
{
    /// <summary>
    /// Manages the Items settings tab
    /// Handles loading, displaying, and saving weapons and armor
    /// </summary>
    public class ItemsTabManager
    {
        private readonly Action<string, bool>? showStatusMessage;
        private readonly ItemsDataCoordinator dataCoordinator;
        private ObservableCollection<ItemViewModel>? weapons;
        private ObservableCollection<ItemViewModel>? armor;
        private ItemsSettingsPanel? panel;
        private ItemViewModel? selectedItem;
        private HashSet<string> deletedWeapons = new HashSet<string>();
        private HashSet<string> deletedArmor = new HashSet<string>();
        private Dictionary<string, WeaponData> originalWeapons = new Dictionary<string, WeaponData>();
        private Dictionary<string, ArmorData> originalArmor = new Dictionary<string, ArmorData>();

        public ItemsTabManager(Action<string, bool>? showStatusMessage = null)
        {
            this.showStatusMessage = showStatusMessage;
            var dataService = new ItemsDataService(showStatusMessage);
            this.dataCoordinator = new ItemsDataCoordinator(dataService, showStatusMessage);
        }

        /// <summary>
        /// Initialize the Items panel with data
        /// </summary>
        public void Initialize(ItemsSettingsPanel panel)
        {
            this.panel = panel;
            
            // Wire up button events
            var addWeaponButton = panel.FindControl<Button>("AddWeaponButton");
            var addArmorButton = panel.FindControl<Button>("AddArmorButton");
            var editButton = panel.FindControl<Button>("EditItemButton");
            var deleteButton = panel.FindControl<Button>("DeleteItemButton");
            
            if (addWeaponButton != null) addWeaponButton.Click += OnAddWeaponClick;
            if (addArmorButton != null) addArmorButton.Click += OnAddArmorClick;
            if (editButton != null) editButton.Click += OnEditItemClick;
            if (deleteButton != null) deleteButton.Click += OnDeleteItemClick;
            
            // Wire up selection events
            panel.ItemSelected += OnItemSelected;
            
            try
            {
                LoadItemsData(panel);
            }
            catch (Exception ex)
            {
                showStatusMessage?.Invoke($"Error loading items: {ex.Message}", false);
            }
        }

        private void OnItemSelected(object? sender, ItemViewModel? item)
        {
            selectedItem = item;
            bool canEdit = item != null;
            bool canDelete = item != null;
            panel?.SetButtonStates(canEdit, canDelete);
        }

        private void OnAddWeaponClick(object? sender, RoutedEventArgs e)
        {
            // For now, just show a message - can be extended with a dialog later
            showStatusMessage?.Invoke("Add weapon functionality - to be implemented", false);
        }

        private void OnAddArmorClick(object? sender, RoutedEventArgs e)
        {
            // For now, just show a message - can be extended with a dialog later
            showStatusMessage?.Invoke("Add armor functionality - to be implemented", false);
        }

        private async void OnEditItemClick(object? sender, RoutedEventArgs e)
        {
            if (selectedItem == null)
            {
                showStatusMessage?.Invoke("No item selected", false);
                return;
            }
            
            // Create view model with current item data
            var viewModel = new ItemEditViewModel
            {
                Name = selectedItem.Name,
                Type = selectedItem.Type,
                Slot = selectedItem.Slot,
                Tier = selectedItem.SelectedTier,
                BaseDamage = selectedItem.BaseDamage,
                AttackSpeed = selectedItem.AttackSpeed,
                Armor = selectedItem.Armor,
                IsWeapon = selectedItem.IsWeapon
            };
            
            // Populate available weapon types
            foreach (var weaponType in Enum.GetNames(typeof(WeaponType)))
            {
                viewModel.AvailableWeaponTypes.Add(weaponType);
            }
            
            // Populate available armor slots
            foreach (var itemType in Enum.GetNames(typeof(ItemType)))
            {
                if (itemType != "Weapon") // Exclude Weapon from armor slots
                {
                    viewModel.AvailableSlots.Add(itemType);
                }
            }
            
            // Populate available tiers
            for (int i = 1; i <= 5; i++)
            {
                viewModel.AvailableTiers.Add(i);
            }
            
            // Open dialog
            var dialog = new ItemEditDialog(viewModel);
            if (panel != null)
            {
                var window = panel.GetLogicalAncestors().OfType<Window>().FirstOrDefault();
                if (window != null)
                {
                    dialog.WindowStartupLocation = WindowStartupLocation.CenterOwner;
                    var result = await dialog.ShowDialog<bool?>(window);
                    if (result == true && viewModel.Validate())
                    {
                        // Store old name before updating
                        var oldName = selectedItem.Name;
                        var nameChanged = viewModel.Name != oldName;
                        
                        // Update the selected item with new values
                        selectedItem.Name = viewModel.Name;
                        selectedItem.Type = viewModel.Type;
                        selectedItem.Slot = viewModel.Slot;
                        selectedItem.SelectedTier = viewModel.Tier;
                        selectedItem.BaseDamage = viewModel.BaseDamage;
                        selectedItem.AttackSpeed = viewModel.AttackSpeed;
                        selectedItem.Armor = viewModel.Armor;
                        
                        // Update original data if it exists
                        if (selectedItem.IsWeapon && originalWeapons.TryGetValue(oldName, out var originalWeapon))
                        {
                            // If name changed, we need to handle it differently
                            if (nameChanged)
                            {
                                // Remove old entry and add new one
                                originalWeapons.Remove(oldName);
                                originalWeapons[viewModel.Name] = new WeaponData
                                {
                                    Type = viewModel.Type,
                                    Name = viewModel.Name,
                                    Tier = viewModel.Tier,
                                    BaseDamage = viewModel.BaseDamage,
                                    AttackSpeed = viewModel.AttackSpeed
                                };
                            }
                            else
                            {
                                originalWeapon.Type = viewModel.Type;
                                originalWeapon.Tier = viewModel.Tier;
                                originalWeapon.BaseDamage = viewModel.BaseDamage;
                                originalWeapon.AttackSpeed = viewModel.AttackSpeed;
                            }
                        }
                        else if (!selectedItem.IsWeapon && originalArmor.TryGetValue(oldName, out var originalArmorItem))
                        {
                            // If name changed, we need to handle it differently
                            if (nameChanged)
                            {
                                // Remove old entry and add new one
                                originalArmor.Remove(oldName);
                                originalArmor[viewModel.Name] = new ArmorData
                                {
                                    Slot = viewModel.Slot,
                                    Name = viewModel.Name,
                                    Tier = viewModel.Tier,
                                    Armor = viewModel.Armor
                                };
                            }
                            else
                            {
                                originalArmorItem.Slot = viewModel.Slot;
                                originalArmorItem.Tier = viewModel.Tier;
                                originalArmorItem.Armor = viewModel.Armor;
                            }
                        }
                        
                        showStatusMessage?.Invoke($"Item '{viewModel.Name}' updated. Click Save to apply changes.", true);
                    }
                }
            }
        }

        private void OnDeleteItemClick(object? sender, RoutedEventArgs e)
        {
            if (selectedItem == null)
            {
                showStatusMessage?.Invoke("No item selected", false);
                return;
            }
            
            // Mark for deletion
            if (selectedItem.IsWeapon)
            {
                deletedWeapons.Add(selectedItem.Name);
                if (weapons != null)
                {
                    weapons.Remove(selectedItem);
                }
            }
            else
            {
                deletedArmor.Add(selectedItem.Name);
                if (armor != null)
                {
                    armor.Remove(selectedItem);
                }
            }
            
            selectedItem = null;
            panel?.SetSelectedItem(null);
            panel?.SetButtonStates(false, false);
            showStatusMessage?.Invoke("Item marked for deletion. Click Save to apply changes.", true);
        }

        /// <summary>
        /// Load weapons and armor from JSON files
        /// </summary>
        private void LoadItemsData(ItemsSettingsPanel panel)
        {
            var (loadedWeapons, loadedArmor, loadedOriginalWeapons, loadedOriginalArmor) = dataCoordinator.LoadItemsData();
            weapons = loadedWeapons;
            armor = loadedArmor;
            originalWeapons = loadedOriginalWeapons;
            originalArmor = loadedOriginalArmor;

            // Bind to ItemsControls
            var weaponsControl = panel.FindControl<ItemsControl>("WeaponsItemsControl");
            if (weaponsControl != null)
            {
                weaponsControl.ItemsSource = weapons;
            }

            var armorControl = panel.FindControl<ItemsControl>("ArmorItemsControl");
            if (armorControl != null)
            {
                armorControl.ItemsSource = armor;
            }
        }

        /// <summary>
        /// Save items back to JSON files
        /// </summary>
        public void SaveItems()
        {
            if (weapons == null || armor == null)
            {
                showStatusMessage?.Invoke("No item data to save", false);
                return;
            }

            try
            {
                dataCoordinator.SaveItems(weapons, armor, originalWeapons, originalArmor);
                
                // Reload data to refresh UI
                if (panel != null)
                {
                    LoadItemsData(panel);
                }
                
                // Clear deletion tracking
                deletedWeapons.Clear();
                deletedArmor.Clear();
                originalWeapons.Clear();
                originalArmor.Clear();
            }
            catch (Exception ex)
            {
                showStatusMessage?.Invoke($"Error saving items: {ex.Message}", false);
            }
        }
    }
}
