using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using RPGGame.Utils;

namespace RPGGame
{
    /// <summary>
    /// Loads starting gear from StartingGear.json (primary) or TuningConfig.json (fallback).
    /// Extracted from GameInitializer for testability and single responsibility.
    /// </summary>
    public static class StartingGearLoader
    {
        /// <summary>
        /// Loads starting gear data from StartingGear.json (primary) or TuningConfig.json (fallback).
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

                        if (gearData != null && gearData.ContainsKey("weapons"))
                        {
                            var weaponsArray = gearData["weapons"];
                            int weaponIndex = 0;
                            foreach (var weaponElement in weaponsArray.EnumerateArray())
                            {
                                var weaponName = weaponElement.TryGetProperty("name", out var nameProp) ? nameProp.GetString() ?? "" : "";
                                var weaponDamage = weaponElement.TryGetProperty("damage", out var damageProp) ? damageProp.GetDouble() : 0.0;
                                var weaponSpeed = weaponElement.TryGetProperty("attackSpeed", out var speedProp) ? speedProp.GetDouble() : 1.0;
                                var weaponWeight = weaponElement.TryGetProperty("weight", out var weightProp) ? weightProp.GetDouble() : 0.0;

                                if (string.IsNullOrWhiteSpace(weaponName))
                                {
                                    weaponIndex++;
                                    continue;
                                }

                                var weaponScaling = GameConfiguration.Instance.WeaponScaling;
                                double originalDamage = weaponDamage;
                                if (weaponScaling != null && weaponDamage > 0 && weaponScaling.GlobalDamageMultiplier > 0)
                                    weaponDamage = weaponDamage * weaponScaling.GlobalDamageMultiplier;

                                double finalDamage = weaponDamage;
                                if (finalDamage <= 0 && originalDamage > 0)
                                    finalDamage = originalDamage;

                                if (originalDamage > 0)
                                {
                                    var weapon = new StartingWeapon
                                    {
                                        name = weaponName,
                                        damage = finalDamage > 0 ? finalDamage : originalDamage,
                                        attackSpeed = weaponSpeed,
                                        weight = weaponWeight
                                    };
                                    startingGear.weapons.Add(weapon);
                                }
                                weaponIndex++;
                            }

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

                            if (startingGear.weapons.Count > 0)
                                return startingGear;
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Warning: Could not load from StartingGear.json: {ex.Message}. Falling back to TuningConfig.json");
                    }
                }

                var config = GameConfiguration.Instance.StartingGear;
                if (config?.Weapons != null)
                {
                    foreach (var weaponConfig in config.Weapons)
                    {
                        if (string.IsNullOrWhiteSpace(weaponConfig.Name) || weaponConfig.Damage <= 0)
                            continue;

                        var weapon = new StartingWeapon
                        {
                            name = weaponConfig.Name ?? "",
                            damage = weaponConfig.Damage,
                            attackSpeed = weaponConfig.AttackSpeed,
                            weight = weaponConfig.Weight
                        };
                        var weaponScaling = GameConfiguration.Instance.WeaponScaling;
                        if (weaponScaling != null && weaponScaling.GlobalDamageMultiplier > 0)
                            weapon.damage = weapon.damage * weaponScaling.GlobalDamageMultiplier;
                        startingGear.weapons.Add(weapon);
                    }
                }

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
