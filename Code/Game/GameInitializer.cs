using System;
using System.Collections.Generic;
using System.Linq;
using RPGGame.Data;
using RPGGame.Utils;

namespace RPGGame
{
    public class StartingGearData
    {
        public List<StartingWeapon> weapons { get; set; } = new();
        public List<StartingArmor> armor { get; set; } = new();
    }

    public class StartingWeapon
    {
        public string name { get; set; } = "";
        public double damage { get; set; }
        public double weight { get; set; } = 0.0;
        public double attackSpeed { get; set; } = 0.05;
    }

    public class StartingArmor
    {
        public string slot { get; set; } = "";
        public string name { get; set; } = "";
        public int armor { get; set; }
        public double weight { get; set; } = 0.0;
    }

    /// <summary>
    /// Handles game initialization logic including starting gear, character setup, and dungeon generation
    /// </summary>
    public class GameInitializer
    {
        private Random random;

        public GameInitializer()
        {
            random = new Random();
        }

        /// <summary>
        /// Loads starting gear data from StartingGear.json (primary) or TuningConfig.json (fallback).
        /// Delegates to StartingGearLoader.
        /// </summary>
        public StartingGearData LoadStartingGear()
        {
            return StartingGearLoader.Load();
        }

        /// <summary>
        /// Initializes a new game with character creation and starting gear
        /// </summary>
        public void InitializeNewGame(Character player, List<Dungeon> availableDungeons, int weaponChoice = 0)
        {
            // Load starting gear from JSON
            var startingGear = LoadStartingGear();
            
            // If weaponChoice is 0, this is being called from old console UI - prompt for weapon
            if (weaponChoice == 0)
            {
                // Prompt player to choose a starter weapon (console mode)
                UIManager.WriteMenuLine("");
                UIManager.WriteTitleLine("Choose your starter weapon:");
                UIManager.WriteMenuLine(new string('-', 27)); // Creates 20 dashes
                for (int i = 0; i < startingGear.weapons.Count; i++)
                {
                    var weapon = startingGear.weapons[i];
                    UIManager.WriteMenuLine($"{i + 1}. {weapon.name}");
                }
                UIManager.WriteMenuLine("");
                
                weaponChoice = 1;
                while (true)
                {
                    UIManager.Write("Choose an option: ");
                    if (int.TryParse(Console.ReadLine(), out weaponChoice) && weaponChoice >= 1 && weaponChoice <= startingGear.weapons.Count)
                        break;
                    UIManager.WriteMenuLine("Invalid choice.");
                }
            }
            
            // Validate weapon choice
            if (weaponChoice < 1 || weaponChoice > startingGear.weapons.Count)
            {
                weaponChoice = 1; // Default to first weapon
            }
            
            // Create weapon from JSON data
            var selectedWeaponData = startingGear.weapons[weaponChoice - 1];
            string weaponName = selectedWeaponData.name.ToLower();
            WeaponType weaponType = weaponName switch
            {
                var name when name.Contains("sword") => WeaponType.Sword,
                var name when name.Contains("mace") => WeaponType.Mace,
                var name when name.Contains("dagger") => WeaponType.Dagger,
                var name when name.Contains("wand") => WeaponType.Wand,
                _ => WeaponType.Sword
            };
            
            WeaponItem starterWeapon = new WeaponItem(selectedWeaponData.name, 1, (int)selectedWeaponData.damage, selectedWeaponData.attackSpeed, weaponType);
            
            // Get starting weapon actions from Actions.json using "startingWeapon" tag
            var actionSelector = new LootActionSelector(new Random());
            var startingActions = actionSelector.GetStartingWeaponActions(weaponType.ToString());
            
            if (startingActions.Count > 0)
            {
                // If multiple starting actions (e.g., mace has 3), add all as ActionBonuses
                // If single starting action, set as GearAction
                if (startingActions.Count > 1)
                {
                    // Multiple actions: add all as ActionBonuses (like mace with SLAM, POUND, BLUDGEON)
                    foreach (var actionName in startingActions)
                    {
                        starterWeapon.ActionBonuses.Add(new ActionBonus { Name = actionName });
                    }
                }
                else
                {
                    // Single action: set as GearAction
                    starterWeapon.GearAction = startingActions[0];
                }
            }
            else
            {
                // Fallback: if no starting weapon actions found, use SelectWeaponActionForStarter
                var fallbackAction = actionSelector.SelectWeaponActionForStarter(weaponType.ToString());
                if (!string.IsNullOrEmpty(fallbackAction))
                {
                    starterWeapon.GearAction = fallbackAction;
                }
            }
            
            player.EquipItem(starterWeapon, "weapon");
            
            // Add default actions (actions marked with IsDefaultAction = true)
            player.Actions.AddDefaultActions(player);
            
            // Initialize combo sequence with weapon actions now that weapon is equipped
            player.InitializeDefaultCombo();
            
            // Equip starting armor from JSON
            foreach (var armorData in startingGear.armor)
            {
                Item armorItem = armorData.slot.ToLower() switch
                {
                    "head" => new HeadItem(armorData.name, 1, armorData.armor),
                    "chest" => new ChestItem(armorData.name, 1, armorData.armor),
                    "feet" => new FeetItem(armorData.name, 1, armorData.armor),
                    _ => new HeadItem(armorData.name, 1, armorData.armor)
                };
                
                // Starting gear should NEVER have actions - leave GearAction as null
                
                string slot = armorData.slot.ToLower() switch
                {
                    "head" => "head",
                    "chest" => "body", 
                    "feet" => "feet",
                    _ => "head"
                };
                
                player.EquipItem(armorItem, slot);
            }

            // Safety check: Ensure character has at least one action available
            // If ActionPool is empty, add fallback actions based on weapon type
            if (player.ActionPool.Count == 0)
            {
                DebugLogger.LogFormat("GameInitializer", 
                    "WARNING: Character '{0}' has no actions after initialization. Adding fallback actions.", player.Name);
                
                // Try to add weapon-type actions as fallback
                if (player.Equipment.Weapon is WeaponItem fallbackWeapon)
                {
                    var weaponTypeActions = GetWeaponTypeActionsForFallback(fallbackWeapon.WeaponType);
                    foreach (var actionName in weaponTypeActions)
                    {
                        var action = ActionLoader.GetAction(actionName);
                        if (action != null)
                        {
                            action.IsComboAction = true;
                            player.AddAction(action, 1.0);
                            DebugLogger.LogFormat("GameInitializer", 
                                "Added fallback action '{0}' to character '{1}'", actionName, player.Name);
                            
                            // Only add one fallback action to ensure we have at least one
                            if (player.ActionPool.Count > 0)
                                break;
                        }
                    }
                }
                
                // If still no actions, add a generic fallback action
                if (player.ActionPool.Count == 0)
                {
                    // Try to find any available combo action
                    var allActions = ActionLoader.GetAllActions();
                    var fallbackAction = allActions.FirstOrDefault(a => a.IsComboAction);
                    if (fallbackAction != null)
                    {
                        player.AddAction(fallbackAction, 1.0);
                        DebugLogger.LogFormat("GameInitializer", 
                            "Added generic fallback action '{0}' to character '{1}'", fallbackAction.Name, player.Name);
                    }
                }
                
                // Re-initialize combo sequence with the fallback actions
                if (player.ActionPool.Count > 0)
                {
                    player.InitializeDefaultCombo();
                }
            }

            // Generate dungeons for the new game
            GenerateDungeons(player, availableDungeons);
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

        /// <summary>
        /// Initializes game for an existing character (load game scenario)
        /// </summary>
        public void InitializeExistingGame(Character player, List<Dungeon> availableDungeons)
        {
            // For existing characters, just set up dungeons without weapon selection
            GenerateDungeons(player, availableDungeons);
        }

        /// <summary>
        /// Generates available dungeons based on player level using DungeonManagerWithRegistry
        /// </summary>
        private void GenerateDungeons(Character player, List<Dungeon> availableDungeons)
        {
            // Use DungeonManagerWithRegistry to generate dungeons from Dungeons.json
            var dungeonManager = new DungeonManagerWithRegistry();
            dungeonManager.RegenerateDungeons(player, availableDungeons);
        }
    }
}
