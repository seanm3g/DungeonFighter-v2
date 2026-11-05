using RPGGame.Data;
using RPGGame.UI.Avalonia.Settings;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace RPGGame.UI.Avalonia.Managers
{
    /// <summary>
    /// Handles data loading, saving, and view model creation for item modifiers.
    /// Extracted from ItemModifiersTabManager to improve Single Responsibility Principle compliance.
    /// </summary>
    public class ItemModifiersDataCoordinator
    {
        private readonly ItemModifiersDataService dataService;
        private readonly Action<string, bool>? showStatusMessage;
        
        public ItemModifiersDataCoordinator(ItemModifiersDataService dataService, Action<string, bool>? showStatusMessage = null)
        {
            this.dataService = dataService ?? throw new ArgumentNullException(nameof(dataService));
            this.showStatusMessage = showStatusMessage;
        }
        
        /// <summary>
        /// Loads modifiers from JSON and creates view models grouped by rarity
        /// </summary>
        public ObservableCollection<RarityGroupViewModel> LoadModifiersData()
        {
            return BuildRarityGroups(dataService.LoadModifications(), dataService.GetAvailableRarities());
        }

        /// <summary>View models grouped by rarity; each keeps a reference to its source row.</summary>
        public static ObservableCollection<RarityGroupViewModel> BuildRarityGroups(
            List<ItemModifiersDataService.ModificationData> modifications,
            List<string> availableRarities)
        {
            // Group modifiers by their current rarity
            var groupedByRarity = modifications
                .GroupBy(m => m.ItemRank ?? "Common")
                .OrderBy(g => GetRarityOrder(g.Key))
                .ToList();
            
            var rarityGroups = new ObservableCollection<RarityGroupViewModel>();

            foreach (var group in groupedByRarity)
            {
                var rarityGroup = new RarityGroupViewModel
                {
                    RarityName = group.Key
                };
                
                foreach (var mod in group.OrderBy(m => m.DiceResult))
                {
                    var modifierVM = new ModifierViewModel
                    {
                        Source = mod,
                        Name = mod.Name ?? "",
                        Description = mod.Description ?? "",
                        CurrentRarity = mod.ItemRank ?? "Common",
                        SelectedRarity = mod.ItemRank ?? "Common",
                        DiceResult = mod.DiceResult,
                        Effect = mod.Effect ?? "",
                        MinValue = mod.MinValue,
                        MaxValue = mod.MaxValue
                    };
                    
                    // Add all available rarities to the dropdown
                    foreach (var rarity in availableRarities)
                    {
                        modifierVM.AvailableRarities.Add(rarity);
                    }
                    
                    rarityGroup.Modifiers.Add(modifierVM);
                }
                
                rarityGroups.Add(rarityGroup);
            }
            
            return rarityGroups;
        }
        
        /// <summary>
        /// Saves modifiers back to JSON
        /// </summary>
        public void SaveModifiers(ObservableCollection<RarityGroupViewModel> rarityGroups)
        {
            if (rarityGroups == null)
            {
                showStatusMessage?.Invoke("No modifier data to save", false);
                return;
            }
            
            try
            {
                dataService.SaveModifierRarities(CollectModifiers(rarityGroups));
            }
            catch (Exception ex)
            {
                showStatusMessage?.Invoke($"Error saving modifiers: {ex.Message}", false);
                throw;
            }
        }

        /// <summary>
        /// The rows to write, in UI order. Each view model updates its own source row: matching rows by DiceResult
        /// collapsed every row into the last one (all sheet rows have DiceResult 0), as on 2026-10-28.
        /// </summary>
        public static List<ItemModifiersDataService.ModificationData> CollectModifiers(
            ObservableCollection<RarityGroupViewModel> rarityGroups)
        {
            var allModifiers = new List<ItemModifiersDataService.ModificationData>();
            {
                foreach (var group in rarityGroups)
                {
                    foreach (var modifier in group.Modifiers)
                    {
                        if (modifier.Source is { } originalMod)
                        {
                            // Update existing modifier
                            originalMod.Name = modifier.Name;
                            originalMod.Description = modifier.Description;
                            originalMod.Effect = modifier.Effect;
                            originalMod.ItemRank = modifier.SelectedRarity;
                            originalMod.MinValue = modifier.MinValue;
                            originalMod.MaxValue = modifier.MaxValue;
                            allModifiers.Add(originalMod);
                        }
                        else
                        {
                            // New modifier
                            allModifiers.Add(new ItemModifiersDataService.ModificationData
                            {
                                DiceResult = modifier.DiceResult,
                                Name = modifier.Name,
                                Description = modifier.Description,
                                Effect = modifier.Effect,
                                ItemRank = modifier.SelectedRarity,
                                MinValue = modifier.MinValue,
                                MaxValue = modifier.MaxValue
                            });
                        }
                    }
                }
            }
            return allModifiers;
        }
        
        /// <summary>
        /// Gets available rarities from the data service
        /// </summary>
        public List<string> GetAvailableRarities()
        {
            return dataService.GetAvailableRarities();
        }
        
        /// <summary>
        /// Gets the next available dice result for a new modifier
        /// </summary>
        public int GetNextAvailableDiceResult(ObservableCollection<RarityGroupViewModel>? rarityGroups)
        {
            if (rarityGroups == null) return 1;
            
            var existingResults = new HashSet<int>();
            foreach (var group in rarityGroups)
            {
                foreach (var modifier in group.Modifiers)
                {
                    existingResults.Add(modifier.DiceResult);
                }
            }
            
            int next = 1;
            while (existingResults.Contains(next))
            {
                next++;
            }
            return next;
        }
        
        /// <summary>
        /// Get rarity order for sorting (lower = more common)
        /// </summary>
        private static int GetRarityOrder(string rarity)
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
