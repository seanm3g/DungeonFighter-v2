using RPGGame.Data;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace RPGGame.UI.Avalonia.Managers
{
    /// <summary>
    /// Handles data persistence operations for weapons and armor.
    /// Similar to ItemModifiersDataService but for items.
    /// </summary>
    public class ItemsDataService
    {
        private readonly Action<string, bool>? showStatusMessage;
        
        public ItemsDataService(Action<string, bool>? showStatusMessage = null)
        {
            this.showStatusMessage = showStatusMessage;
        }
        
        /// <summary>
        /// Load weapons from Weapons.json
        /// </summary>
        public List<WeaponData> LoadWeapons()
        {
            try
            {
                string? filePath = JsonLoader.FindGameDataFile("Weapons.json");
                if (filePath != null && File.Exists(filePath))
                {
                    string json = File.ReadAllText(filePath);
                    var options = new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    };
                    return JsonSerializer.Deserialize<List<WeaponData>>(json, options) ?? new List<WeaponData>();
                }
            }
            catch (Exception ex)
            {
                showStatusMessage?.Invoke($"Error loading weapons: {ex.Message}", false);
            }

            return new List<WeaponData>();
        }
        
        /// <summary>
        /// Load armor from Armor.json
        /// </summary>
        public List<ArmorData> LoadArmor()
        {
            try
            {
                string? filePath = JsonLoader.FindGameDataFile("Armor.json");
                if (filePath != null && File.Exists(filePath))
                {
                    string json = File.ReadAllText(filePath);
                    var options = new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    };
                    return JsonSerializer.Deserialize<List<ArmorData>>(json, options) ?? new List<ArmorData>();
                }
            }
            catch (Exception ex)
            {
                showStatusMessage?.Invoke($"Error loading armor: {ex.Message}", false);
            }

            return new List<ArmorData>();
        }
        
        /// <summary>
        /// Save weapons back to Weapons.json
        /// </summary>
        public void SaveWeapons(List<WeaponData> weapons)
        {
            try
            {
                string? filePath = JsonLoader.FindGameDataFile("Weapons.json");
                if (filePath != null)
                {
                    // Use camelCase to match original format
                    var options = new JsonSerializerOptions
                    {
                        WriteIndented = true,
                        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                    };
                    string json = JsonSerializer.Serialize(weapons, options);
                    File.WriteAllText(filePath, json);
                    
                    showStatusMessage?.Invoke("Weapons saved successfully", true);
                }
                else
                {
                    showStatusMessage?.Invoke("Could not find Weapons.json file", false);
                }
            }
            catch (Exception ex)
            {
                showStatusMessage?.Invoke($"Error saving weapons: {ex.Message}", false);
            }
        }
        
        /// <summary>
        /// Save armor back to Armor.json
        /// </summary>
        public void SaveArmor(List<ArmorData> armor)
        {
            try
            {
                string? filePath = JsonLoader.FindGameDataFile("Armor.json");
                if (filePath != null)
                {
                    // Use camelCase to match original format
                    var options = new JsonSerializerOptions
                    {
                        WriteIndented = true,
                        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                    };
                    string json = JsonSerializer.Serialize(armor, options);
                    File.WriteAllText(filePath, json);
                    
                    showStatusMessage?.Invoke("Armor saved successfully", true);
                }
                else
                {
                    showStatusMessage?.Invoke("Could not find Armor.json file", false);
                }
            }
            catch (Exception ex)
            {
                showStatusMessage?.Invoke($"Error saving armor: {ex.Message}", false);
            }
        }
    }
}
