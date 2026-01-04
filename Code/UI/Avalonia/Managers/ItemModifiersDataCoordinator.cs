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
        public (ObservableCollection<RarityGroupViewModel> rarityGroups, Dictionary<int, ItemModifiersDataService.ModificationData> originalModifications) LoadModifiersData()
        {
            // Get available rarities from RarityTable.json
            var availableRarities = dataService.GetAvailableRarities();
            
            // Load modifications from Modifications.json
            var modifications = dataService.LoadModifications();
            
            // Group modifiers by their current rarity
            var groupedByRarity = modifications
                .GroupBy(m => m.ItemRank ?? "Common")
                .OrderBy(g => GetRarityOrder(g.Key))
                .ToList();
            
            var rarityGroups = new ObservableCollection<RarityGroupViewModel>();
            var originalModifications = new Dictionary<int, ItemModifiersDataService.ModificationData>();
            
            foreach (var group in groupedByRarity)
            {
                var rarityGroup = new RarityGroupViewModel
                {
                    RarityName = group.Key
                };
                
                foreach (var mod in group.OrderBy(m => m.DiceResult))
                {
                    // Store original modification data
                    originalModifications[mod.DiceResult] = mod;
                    
                    var modifierVM = new ModifierViewModel
                    {
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
            
            return (rarityGroups, originalModifications);
        }
        
        /// <summary>
        /// Saves modifiers back to JSON
        /// </summary>
        public void SaveModifiers(
            ObservableCollection<RarityGroupViewModel> rarityGroups,
            Dictionary<int, ItemModifiersDataService.ModificationData> originalModifications)
        {
            if (rarityGroups == null)
            {
                showStatusMessage?.Invoke("No modifier data to save", false);
                return;
            }
            
            try
            {
                // Collect all modifiers (existing and new)
                var allModifiers = new List<ItemModifiersDataService.ModificationData>();
                var existingDiceResults = new HashSet<int>();
                
                // Process modifiers from UI
                foreach (var group in rarityGroups)
                {
                    foreach (var modifier in group.Modifiers)
                    {
                        existingDiceResults.Add(modifier.DiceResult);
                        
                        // Check if this is a new modifier or existing one
                        if (originalModifications.TryGetValue(modifier.DiceResult, out var originalMod))
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
                
                // Save back to Modifications.json using data service
                dataService.SaveModifierRarities(allModifiers);
            }
            catch (Exception ex)
            {
                showStatusMessage?.Invoke($"Error saving modifiers: {ex.Message}", false);
                throw;
            }
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
        private int GetRarityOrder(string rarity)
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
