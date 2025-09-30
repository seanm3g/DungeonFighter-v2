using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;

namespace RPGGame
{
    /// <summary>
    /// Handles character save and load operations
    /// Extracted from Character.cs to reduce complexity
    /// </summary>
    public static class CharacterSaveManager
    {
        /// <summary>
        /// Saves a character to a JSON file
        /// </summary>
        /// <param name="character">The character to save</param>
        /// <param name="filename">The filename to save to</param>
        public static void SaveCharacter(Character character, string? filename = null)
        {
            try
            {
                // Use proper path resolution if no filename provided
                if (string.IsNullOrEmpty(filename))
                {
                    filename = GameConstants.GetGameDataFilePath(GameConstants.CharacterSaveJson);
                }
                var saveData = new CharacterSaveData
                {
                    Name = character.Name,
                    Level = character.Level,
                    XP = character.Progression.XP,
                    CurrentHealth = character.CurrentHealth,
                    MaxHealth = character.MaxHealth,
                    Strength = character.Stats.Strength,
                    Agility = character.Stats.Agility,
                    Technique = character.Stats.Technique,
                    Intelligence = character.Stats.Intelligence,
                    BarbarianPoints = character.Progression.BarbarianPoints,
                    WarriorPoints = character.Progression.WarriorPoints,
                    RoguePoints = character.Progression.RoguePoints,
                    WizardPoints = character.Progression.WizardPoints,
                    ComboStep = character.Effects.ComboStep,
                    ComboBonus = character.Effects.ComboBonus,
                    TempComboBonus = character.Effects.TempComboBonus,
                    TempComboBonusTurns = character.Effects.TempComboBonusTurns,
                    DamageReduction = character.DamageReduction,
                    Inventory = character.Equipment.Inventory,
                    Head = character.Equipment.Head,
                    Body = character.Equipment.Body,
                    Weapon = character.Equipment.Weapon,
                    Feet = character.Equipment.Feet
                };

                var options = new JsonSerializerOptions
                {
                    WriteIndented = true,
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                };

                string json = JsonSerializer.Serialize(saveData, options);
                File.WriteAllText(filename, json);
            }
            catch (Exception ex)
            {
                UIManager.WriteLine($"Error saving character: {ex.Message}");
            }
        }

        /// <summary>
        /// Loads a character from a JSON file
        /// </summary>
        /// <param name="filename">The filename to load from</param>
        /// <returns>The loaded character, or null if loading failed</returns>
        public static Character? LoadCharacter(string? filename = null)
        {
            try
            {
                // Use proper path resolution if no filename provided
                if (string.IsNullOrEmpty(filename))
                {
                    filename = GameConstants.GetGameDataFilePath(GameConstants.CharacterSaveJson);
                }
                
                if (!File.Exists(filename))
                {
                    UIManager.WriteLine($"No save file found at {filename}");
                    return null;
                }

                string json = File.ReadAllText(filename);
                var saveData = JsonSerializer.Deserialize<CharacterSaveData>(json);

                if (saveData == null)
                {
                    UIManager.WriteLine("Failed to deserialize character data");
                    return null;
                }

                // Create character with loaded data
                var character = new Character(saveData.Name, saveData.Level);
                
                // Restore all character data
                character.Progression.XP = saveData.XP;
                character.CurrentHealth = saveData.CurrentHealth;
                character.MaxHealth = saveData.MaxHealth;
                character.Stats.Strength = saveData.Strength;
                character.Stats.Agility = saveData.Agility;
                character.Stats.Technique = saveData.Technique;
                character.Stats.Intelligence = saveData.Intelligence;
                character.Progression.BarbarianPoints = saveData.BarbarianPoints;
                character.Progression.WarriorPoints = saveData.WarriorPoints;
                character.Progression.RoguePoints = saveData.RoguePoints;
                character.Progression.WizardPoints = saveData.WizardPoints;
                character.Effects.ComboStep = saveData.ComboStep;
                character.Effects.ComboBonus = saveData.ComboBonus;
                character.Effects.TempComboBonus = saveData.TempComboBonus;
                character.Effects.TempComboBonusTurns = saveData.TempComboBonusTurns;
                character.DamageReduction = saveData.DamageReduction;
                character.Equipment.Inventory = saveData.Inventory;
                character.Equipment.Head = saveData.Head;
                character.Equipment.Body = saveData.Body;
                character.Equipment.Weapon = saveData.Weapon;
                character.Equipment.Feet = saveData.Feet;

                // Rebuild action pool with proper structure
                character.ActionPool.Clear();
                character.Actions.AddDefaultActions(character); // Add basic attack
                character.Actions.AddClassActions(character, character.Progression, (character.Equipment.Weapon as WeaponItem)?.WeaponType); // Add class actions based on weapon
                
                // Re-add gear actions for equipped items (with probability for non-starter items)
                if (character.Head != null)
                    character.Actions.AddArmorActions(character, character.Head);
                if (character.Body != null)
                    character.Actions.AddArmorActions(character, character.Body);
                if (character.Weapon is WeaponItem weapon)
                    character.Actions.AddWeaponActions(character, weapon);
                if (character.Feet != null)
                    character.Actions.AddArmorActions(character, character.Feet);

                // Initialize combo sequence after all actions are loaded
                character.InitializeDefaultCombo();

                UIManager.WriteLine($"Character loaded from {filename}");
                return character;
            }
            catch (Exception ex)
            {
                UIManager.WriteLine($"Error loading character: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Deletes a character save file
        /// </summary>
        /// <param name="filename">The filename to delete</param>
        public static void DeleteSaveFile(string? filename = null)
        {
            try
            {
                // Use proper path resolution if no filename provided
                if (string.IsNullOrEmpty(filename))
                {
                    filename = GameConstants.GetGameDataFilePath(GameConstants.CharacterSaveJson);
                }
                
                if (File.Exists(filename))
                {
                    File.Delete(filename);
                    UIManager.WriteLine($"Save file deleted: {filename}");
                }
            }
            catch (Exception ex)
            {
                UIManager.WriteLine($"Error deleting save file: {ex.Message}");
            }
        }

        /// <summary>
        /// Checks if a save file exists
        /// </summary>
        /// <param name="filename">The filename to check</param>
        /// <returns>True if the save file exists</returns>
        public static bool SaveFileExists(string? filename = null)
        {
            // Use proper path resolution if no filename provided
            if (string.IsNullOrEmpty(filename))
            {
                filename = GameConstants.GetGameDataFilePath(GameConstants.CharacterSaveJson);
            }
            
            return File.Exists(filename);
        }

        /// <summary>
        /// Gets information about a saved character without loading it
        /// </summary>
        /// <param name="filename">The filename to check</param>
        /// <returns>Tuple of (characterName, level) or (null, 0) if not found</returns>
        public static (string? characterName, int level) GetSavedCharacterInfo(string? filename = null)
        {
            try
            {
                // Use proper path resolution if no filename provided
                if (string.IsNullOrEmpty(filename))
                {
                    filename = GameConstants.GetGameDataFilePath(GameConstants.CharacterSaveJson);
                }
                
                if (!File.Exists(filename))
                    return (null, 0);

                string json = File.ReadAllText(filename);
                var saveData = JsonSerializer.Deserialize<CharacterSaveData>(json);
                
                return saveData != null ? (saveData.Name, saveData.Level) : (null, 0);
            }
            catch
            {
                return (null, 0);
            }
        }
    }

    /// <summary>
    /// Data class for character save/load operations
    /// </summary>
    public class CharacterSaveData
    {
        public string Name { get; set; } = "";
        public int Level { get; set; }
        public int XP { get; set; }
        public int CurrentHealth { get; set; }
        public int MaxHealth { get; set; }
        public int Strength { get; set; }
        public int Agility { get; set; }
        public int Technique { get; set; }
        public int Intelligence { get; set; }
        public int BarbarianPoints { get; set; }
        public int WarriorPoints { get; set; }
        public int RoguePoints { get; set; }
        public int WizardPoints { get; set; }
        public int ComboStep { get; set; }
        public int ComboBonus { get; set; }
        public int TempComboBonus { get; set; }
        public int TempComboBonusTurns { get; set; }
        public double DamageReduction { get; set; }
        public List<Item> Inventory { get; set; } = new List<Item>();
        public Item? Head { get; set; }
        public Item? Body { get; set; }
        public Item? Weapon { get; set; }
        public Item? Feet { get; set; }
    }
}
