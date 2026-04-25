using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using RPGGame.Utils;

namespace RPGGame
{
    /// <summary>
    /// Loads legacy starting armor slots from StartingGear.json (primary) or TuningConfig.json (fallback)
    /// when <c>Armor.json</c> has no <c>starter</c>-tagged rows (<see cref="StarterCatalogItems.LoadStarterArmorItems"/>).
    /// Starter weapons are not loaded here — the menu uses all <c>starter</c>-tagged rows in <c>Weapons.json</c> when present
    /// (<see cref="StarterCatalogItems.ResolveStarterWeaponMenuCatalogRows"/>).
    /// </summary>
    public static class StartingGearLoader
    {
        /// <summary>
        /// Loads fallback starting armor from StartingGear.json (primary) or TuningConfig.json when the catalog has no <c>starter</c> armor.
        /// The returned <see cref="StartingGearData.weapons"/> list is always empty.
        /// </summary>
        public static StartingGearData Load()
        {
            try
            {
                var startingGear = new StartingGearData();

                var startingGearFile = JsonLoader.FindGameDataFile("StartingGear.json");
                if (startingGearFile != null)
                {
                    try
                    {
                        var jsonContent = File.ReadAllText(startingGearFile);
                        var gearData = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(jsonContent, new JsonSerializerOptions
                        {
                            PropertyNameCaseInsensitive = true
                        });

                        if (gearData != null)
                        {
                            if (gearData.ContainsKey("armor"))
                            {
                                var armorArray = gearData["armor"];
                                foreach (var armorElement in armorArray.EnumerateArray())
                                {
                                    var armor = new StartingArmor
                                    {
                                        slot = armorElement.TryGetProperty("slot", out var slotProp) ? slotProp.GetString() ?? "" : "",
                                        name = armorElement.TryGetProperty("name", out var nameProp) ? nameProp.GetString() ?? "" : "",
                                        armor = armorElement.TryGetProperty("armor", out var armorProp) ? armorProp.GetInt32() : 0,
                                        weight = armorElement.TryGetProperty("weight", out var weightProp) ? weightProp.GetDouble() : 0.0
                                    };
                                    if (!string.IsNullOrWhiteSpace(armor.name))
                                        startingGear.armor.Add(armor);
                                }
                            }

                            return startingGear;
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Warning: Could not load from StartingGear.json: {ex.Message}. Falling back to TuningConfig.json");
                    }
                }

                var config = GameConfiguration.Instance.StartingGear;
                if (config?.Armor != null)
                {
                    foreach (var armorConfig in config.Armor)
                    {
                        var armor = new StartingArmor
                        {
                            slot = armorConfig.Slot,
                            name = armorConfig.Name,
                            armor = armorConfig.Armor,
                            weight = armorConfig.Weight
                        };
                        startingGear.armor.Add(armor);
                    }
                }

                return startingGear;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error loading starting gear: {ex.Message}");
                return new StartingGearData();
            }
        }
    }
}
