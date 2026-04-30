using RPGGame;
using RPGGame.Data;
using RPGGame.UI.Avalonia.Settings;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace RPGGame.UI.Avalonia.Managers
{
    /// <summary>
    /// Handles data loading, saving, and view model creation for items (weapons and armor).
    /// Extracted from ItemsTabManager to improve Single Responsibility Principle compliance.
    /// </summary>
    public class ItemsDataCoordinator
    {
        private const char TagsEmptyPlaceholder = '\u2014'; // em dash shown when item has no tags

        private readonly ItemsDataService dataService;
        private readonly Action<string, bool>? showStatusMessage;
        
        public ItemsDataCoordinator(ItemsDataService dataService, Action<string, bool>? showStatusMessage = null)
        {
            this.dataService = dataService ?? throw new ArgumentNullException(nameof(dataService));
            this.showStatusMessage = showStatusMessage;
        }

        /// <summary>List row / dialog preview: em dash when empty.</summary>
        public static string FormatTagsSummaryForItemsSettings(List<string>? tags)
        {
            var n = GameDataTagHelper.NormalizeDistinct(tags);
            return n.Count == 0 ? TagsEmptyPlaceholder.ToString() : string.Join(", ", n);
        }

        /// <summary>Parse list row summary back to JSON tag list; null means omit / clear.</summary>
        public static List<string>? TagsListFromItemsSettingsSummary(string? summary)
        {
            if (string.IsNullOrWhiteSpace(summary))
                return null;
            var t = summary.Trim();
            if (t == TagsEmptyPlaceholder.ToString())
                return null;
            var parsed = GameDataTagHelper.ParseCommaSeparatedTags(t);
            return parsed.Count == 0 ? null : parsed;
        }

        /// <summary>Comma-separated text for the edit dialog (empty when no tags).</summary>
        public static string TagsEditTextFromSummary(string? tagsSummary)
        {
            if (string.IsNullOrWhiteSpace(tagsSummary))
                return "";
            var t = tagsSummary.Trim();
            return t == TagsEmptyPlaceholder.ToString() ? "" : t;
        }
        
        /// <summary>
        /// Loads weapons and armor from JSON and creates view models
        /// </summary>
        public (ObservableCollection<ItemViewModel> weapons, ObservableCollection<ItemViewModel> armor, 
                Dictionary<string, WeaponData> originalWeapons, Dictionary<string, ArmorData> originalArmor) LoadItemsData()
        {
            // Load weapons
            var weaponsData = dataService.LoadWeapons();
            var weapons = new ObservableCollection<ItemViewModel>();
            var originalWeapons = new Dictionary<string, WeaponData>();

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
                    DamageBonusMin = weapon.DamageBonusMin,
                    DamageBonusMax = weapon.DamageBonusMax,
                    AttackSpeed = weapon.AttackSpeed,
                    HitCount = 0, // Not available in WeaponData class
                    Effect = "none", // Not available in WeaponData class
                    IsWeapon = true,
                    TagsSummary = FormatTagsSummaryForItemsSettings(weapon.Tags)
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
            var armor = new ObservableCollection<ItemViewModel>();
            var originalArmor = new Dictionary<string, ArmorData>();

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
                    IsWeapon = false,
                    TagsSummary = FormatTagsSummaryForItemsSettings(armorItem.Tags)
                };

                // Add available tiers
                for (int i = 1; i <= 5; i++)
                {
                    armorVM.AvailableTiers.Add(i);
                }

                armor.Add(armorVM);
            }
            
            return (weapons, armor, originalWeapons, originalArmor);
        }
        
        /// <summary>
        /// Saves weapons and armor back to JSON
        /// </summary>
        public void SaveItems(
            ObservableCollection<ItemViewModel> weapons,
            ObservableCollection<ItemViewModel> armor,
            Dictionary<string, WeaponData> originalWeapons,
            Dictionary<string, ArmorData> originalArmor)
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
                        originalWeapon.Name = weaponVM.Name;
                        originalWeapon.Type = weaponVM.Type;
                        originalWeapon.Tier = weaponVM.SelectedTier;
                        originalWeapon.BaseDamage = weaponVM.BaseDamage;
                        originalWeapon.DamageBonusMin = weaponVM.DamageBonusMin;
                        originalWeapon.DamageBonusMax = weaponVM.DamageBonusMax;
                        originalWeapon.AttackSpeed = weaponVM.AttackSpeed;
                        originalWeapon.Tags = TagsListFromItemsSettingsSummary(weaponVM.TagsSummary);
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
                            DamageBonusMin = weaponVM.DamageBonusMin,
                            DamageBonusMax = weaponVM.DamageBonusMax,
                            AttackSpeed = weaponVM.AttackSpeed,
                            Tags = TagsListFromItemsSettingsSummary(weaponVM.TagsSummary)
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
                        originalArmorItem.Name = armorVM.Name;
                        originalArmorItem.Slot = armorVM.Slot;
                        originalArmorItem.Tier = armorVM.SelectedTier;
                        originalArmorItem.Armor = armorVM.Armor;
                        originalArmorItem.Tags = TagsListFromItemsSettingsSummary(armorVM.TagsSummary);
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
                            Armor = armorVM.Armor,
                            Tags = TagsListFromItemsSettingsSummary(armorVM.TagsSummary)
                        });
                    }
                }

                // Save back to JSON files
                dataService.SaveWeapons(allWeapons);
                dataService.SaveArmor(allArmor);
            }
            catch (Exception ex)
            {
                showStatusMessage?.Invoke($"Error saving items: {ex.Message}", false);
                throw;
            }
        }
    }
}
