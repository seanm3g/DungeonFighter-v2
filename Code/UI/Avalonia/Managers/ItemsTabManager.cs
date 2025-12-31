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

namespace RPGGame.UI.Avalonia.Managers
{
    /// <summary>
    /// Manages the Items settings tab
    /// Handles loading, displaying, and saving weapons and armor
    /// </summary>
    public class ItemsTabManager
    {
        private readonly Action<string, bool>? showStatusMessage;
        private readonly ItemsDataService dataService;
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
            this.dataService = new ItemsDataService(showStatusMessage);
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
                showStatusMessage?.Invoke("Items loaded successfully", true);
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

        private void OnEditItemClick(object? sender, RoutedEventArgs e)
        {
            if (selectedItem == null)
            {
                showStatusMessage?.Invoke("No item selected", false);
                return;
            }
            
            // For now, just show a message - can be extended with a dialog later
            showStatusMessage?.Invoke($"Edit {selectedItem.Name} - to be implemented", false);
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
            // Load weapons
            var weaponsData = dataService.LoadWeapons();
            weapons = new ObservableCollection<ItemViewModel>();

            foreach (var weapon in weaponsData.OrderBy(w => w.Tier).ThenBy(w => w.Name))
            {
                // Store original weapon data
                originalWeapons[weapon.Name] = weapon;
                
                var weaponVM = new ItemViewModel
                {
                    Name = weapon.Name,
                    Type = weapon.Type,
                    CurrentTier = weapon.Tier,
                    SelectedTier = weapon.Tier,
                    BaseDamage = weapon.BaseDamage,
                    AttackSpeed = weapon.AttackSpeed,
                    HitCount = 0, // Not available in WeaponData class
                    Effect = "none", // Not available in WeaponData class
                    IsWeapon = true
                };

                // Add available tiers
                for (int i = 1; i <= 5; i++)
                {
                    weaponVM.AvailableTiers.Add(i);
                }

                weapons.Add(weaponVM);
            }

            // Load armor
            var armorData = dataService.LoadArmor();
            armor = new ObservableCollection<ItemViewModel>();

            foreach (var armorItem in armorData.OrderBy(a => a.Tier).ThenBy(a => a.Name))
            {
                // Store original armor data
                originalArmor[armorItem.Name] = armorItem;
                
                var armorVM = new ItemViewModel
                {
                    Name = armorItem.Name,
                    Slot = armorItem.Slot,
                    CurrentTier = armorItem.Tier,
                    SelectedTier = armorItem.Tier,
                    Armor = armorItem.Armor,
                    IsWeapon = false
                };

                // Add available tiers
                for (int i = 1; i <= 5; i++)
                {
                    armorVM.AvailableTiers.Add(i);
                }

                armor.Add(armorVM);
            }

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
                // Collect all weapons (existing and modified)
                var allWeapons = new List<WeaponData>();
                
                foreach (var weaponVM in weapons)
                {
                    if (originalWeapons.TryGetValue(weaponVM.Name, out var originalWeapon))
                    {
                        // Update existing weapon
                        originalWeapon.Tier = weaponVM.SelectedTier;
                        originalWeapon.BaseDamage = weaponVM.BaseDamage;
                        originalWeapon.AttackSpeed = weaponVM.AttackSpeed;
                        // HitCount and Effect not available in WeaponData class
                        allWeapons.Add(originalWeapon);
                    }
                    else
                    {
                        // New weapon (if we had add functionality)
                        allWeapons.Add(new WeaponData
                        {
                            Type = weaponVM.Type,
                            Name = weaponVM.Name,
                            Tier = weaponVM.SelectedTier,
                            BaseDamage = weaponVM.BaseDamage,
                            AttackSpeed = weaponVM.AttackSpeed
                            // HitCount and Effect not available in WeaponData class
                        });
                    }
                }

                // Collect all armor (existing and modified)
                var allArmor = new List<ArmorData>();
                
                foreach (var armorVM in armor)
                {
                    if (originalArmor.TryGetValue(armorVM.Name, out var originalArmorItem))
                    {
                        // Update existing armor
                        originalArmorItem.Tier = armorVM.SelectedTier;
                        originalArmorItem.Armor = armorVM.Armor;
                        allArmor.Add(originalArmorItem);
                    }
                    else
                    {
                        // New armor (if we had add functionality)
                        allArmor.Add(new ArmorData
                        {
                            Slot = armorVM.Slot,
                            Name = armorVM.Name,
                            Tier = armorVM.SelectedTier,
                            Armor = armorVM.Armor
                        });
                    }
                }

                // Save back to JSON files
                dataService.SaveWeapons(allWeapons);
                dataService.SaveArmor(allArmor);
                
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
