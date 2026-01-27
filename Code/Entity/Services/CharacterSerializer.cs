using System;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using RPGGame;
using RPGGame.Data;
using RPGGame.Utils;

namespace RPGGame.Entity.Services
{
    /// <summary>
    /// Handles serialization and deserialization of character data.
    /// Extracted from CharacterSaveManager to separate serialization logic from file operations.
    /// </summary>
    public class CharacterSerializer
    {
        /// <summary>
        /// Serializes a character to JSON
        /// </summary>
        /// <param name="character">The character to serialize</param>
        /// <returns>The JSON string</returns>
        public string Serialize(Character character)
        {
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

            return JsonSerializer.Serialize(saveData, options);
        }

        /// <summary>
        /// Deserializes a character from JSON
        /// </summary>
        /// <param name="json">The JSON string</param>
        /// <returns>The deserialized CharacterSaveData, or null if deserialization failed</returns>
        public CharacterSaveData? Deserialize(string json)
        {
            var options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            };
            
            return JsonSerializer.Deserialize<CharacterSaveData>(json, options);
        }

        /// <summary>
        /// Creates a character from save data
        /// </summary>
        /// <param name="saveData">The save data</param>
        /// <returns>The created character</returns>
        public Character CreateCharacterFromSaveData(CharacterSaveData saveData)
        {
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
            
            // Restore equipment with proper type conversion
            character.Equipment.Inventory = ItemTypeConverter.ConvertItemsToProperTypes(saveData.Inventory);
            character.Equipment.Head = ItemTypeConverter.ConvertItemToProperType(saveData.Head);
            character.Equipment.Body = ItemTypeConverter.ConvertItemToProperType(saveData.Body);
            character.Equipment.Weapon = ItemTypeConverter.ConvertItemToProperType(saveData.Weapon) as WeaponItem;
            character.Equipment.Feet = ItemTypeConverter.ConvertItemToProperType(saveData.Feet);

            // Rebuild action pool with proper structure
            character.ActionPool.Clear();
            // Removed: AddDefaultActions (BASIC ATTACK removed)
            // Removed: AddClassActions (no longer adding all class actions)
            // Actions are now only added via weapon GearAction property
            
            // Re-add gear actions for equipped items (with probability for non-starter items)
            if (character.Equipment.Head != null)
                character.Actions.AddArmorActions(character, character.Equipment.Head);
            if (character.Equipment.Body != null)
                character.Actions.AddArmorActions(character, character.Equipment.Body);
            if (character.Equipment.Weapon is WeaponItem weapon)
                character.Actions.AddWeaponActions(character, weapon);
            if (character.Equipment.Feet != null)
                character.Actions.AddArmorActions(character, character.Equipment.Feet);

            // Re-add class actions based on character progression
            // Class actions should persist regardless of equipment changes
            var weaponType = (character.Equipment.Weapon as WeaponItem)?.WeaponType;
            character.Actions.AddClassActions(character, character.Progression, weaponType);

            // Initialize combo sequence after all actions are loaded
            character.InitializeDefaultCombo();
            
            // Reset combo step to first action when loading (InitializeDefaultCombo may have already done this via AddToCombo, but ensure it's reset)
            character.ComboStep = 0;

            // Safety check: Ensure character has at least one action available
            // If ActionPool is empty, add fallback actions based on weapon type
            if (character.ActionPool.Count == 0)
            {
                DebugLogger.LogFormat("CharacterSerializer", 
                    "WARNING: Character '{0}' has no actions after loading. Adding fallback actions.", character.Name);
                
                // Try to add weapon-type actions as fallback
                if (character.Equipment.Weapon is WeaponItem fallbackWeapon)
                {
                    var weaponTypeActions = GetWeaponTypeActionsForFallback(fallbackWeapon.WeaponType);
                    foreach (var actionName in weaponTypeActions)
                    {
                        var action = ActionLoader.GetAction(actionName);
                        if (action != null)
                        {
                            action.IsComboAction = true;
                            character.AddAction(action, 1.0);
                            DebugLogger.LogFormat("CharacterSerializer", 
                                "Added fallback action '{0}' to character '{1}'", actionName, character.Name);
                            
                            // Only add one fallback action to ensure we have at least one
                            if (character.ActionPool.Count > 0)
                                break;
                        }
                    }
                }
                
                // If still no actions, add a generic fallback action
                if (character.ActionPool.Count == 0)
                {
                    // Try to find any available combo action
                    var allActions = ActionLoader.GetAllActions();
                    var fallbackAction = allActions.FirstOrDefault(a => a.IsComboAction);
                    if (fallbackAction != null)
                    {
                        character.AddAction(fallbackAction, 1.0);
                        DebugLogger.LogFormat("CharacterSerializer", 
                            "Added generic fallback action '{0}' to character '{1}'", fallbackAction.Name, character.Name);
                    }
                }
                
                // Re-initialize combo sequence with the fallback actions
                if (character.ActionPool.Count > 0)
                {
                    character.InitializeDefaultCombo();
                }
            }

            return character;
        }

        /// <summary>
        /// Gets weapon-type actions for fallback when no actions are available
        /// </summary>
        private List<string> GetWeaponTypeActionsForFallback(WeaponType weaponType)
        {
            var weaponTag = weaponType.ToString().ToLower();
            var allActions = ActionLoader.GetAllActions();

            // Get weapon-specific actions from JSON using tag matching
            var weaponActions = allActions
                .Where(action => action.Tags != null &&
                                action.Tags.Any(tag => tag.Equals("weapon", StringComparison.OrdinalIgnoreCase)) &&
                                action.Tags.Any(tag => tag.Equals(weaponTag, StringComparison.OrdinalIgnoreCase)) &&
                                !action.Tags.Any(tag => tag.Equals("unique", StringComparison.OrdinalIgnoreCase)) &&
                                !action.Tags.Any(tag => tag.Equals("class", StringComparison.OrdinalIgnoreCase)))
                .Select(action => action.Name)
                .ToList();

            return weaponActions;
        }
    }
}

