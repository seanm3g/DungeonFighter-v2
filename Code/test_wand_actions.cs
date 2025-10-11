using System;
using System.Collections.Generic;
using System.Linq;

namespace RPGGame
{
    class WandActionTest
    {
        public static void TestWandActions()
        {
            Console.WriteLine("=== Testing Wand Action Loading ===");
            
            // Test ActionLoader
            var allActions = ActionLoader.GetAllActions();
            Console.WriteLine($"Total actions loaded: {allActions.Count}");
            
            // Test wand-specific actions
            var wandActions = allActions
                .Where(action => action.Tags.Contains("weapon") && 
                                action.Tags.Contains("wand") &&
                                !action.Tags.Contains("unique"))
                .ToList();
            
            Console.WriteLine($"Wand actions found: {wandActions.Count}");
            foreach (var action in wandActions)
            {
                Console.WriteLine($"  - {action.Name} (Tags: {string.Join(", ", action.Tags)})");
            }
            
            // Test GetWeaponActionsFromJson method
            var weaponActions = GetWeaponActionsFromJson(WeaponType.Wand);
            Console.WriteLine($"GetWeaponActionsFromJson returned: {weaponActions.Count} actions");
            foreach (var actionName in weaponActions)
            {
                Console.WriteLine($"  - {actionName}");
            }
            
            // Test creating a character with wand
            Console.WriteLine("\n=== Testing Character Creation ===");
            var character = new Character("TestWizard", 1);
            Console.WriteLine($"Character created with {character.ActionPool.Count} actions:");
            foreach (var actionEntry in character.ActionPool)
            {
                Console.WriteLine($"  - {actionEntry.action.Name} (Combo: {actionEntry.action.IsComboAction})");
            }
            
            // Test equipping wand
            Console.WriteLine("\n=== Testing Wand Equipment ===");
            var wand = new WeaponItem("Test Wand", 1, 5, 1.1, WeaponType.Wand);
            character.EquipItem(wand, "weapon");
            Console.WriteLine($"After equipping wand, character has {character.ActionPool.Count} actions:");
            foreach (var actionEntry in character.ActionPool)
            {
                Console.WriteLine($"  - {actionEntry.action.Name} (Combo: {actionEntry.action.IsComboAction})");
            }
            
            Console.WriteLine("\nPress any key to exit...");
            Console.ReadKey();
        }
        
        private static List<string> GetWeaponActionsFromJson(WeaponType weaponType)
        {
            var weaponTag = weaponType.ToString().ToLower();
            Console.WriteLine($"Looking for weapon tag: '{weaponTag}'");
            
            var allActions = ActionLoader.GetAllActions();
            Console.WriteLine($"Got {allActions.Count} total actions from ActionLoader");
            
            // For mace weapons, return the specific mace actions
            if (weaponType == WeaponType.Mace)
            {
                return new List<string> { "CRUSHING BLOW", "SHIELD BREAK", "THUNDER CLAP" };
            }
            
            // For other weapon types, use the original logic
            var weaponActions = allActions
                .Where(action => action.Tags.Contains("weapon") && 
                                action.Tags.Contains(weaponTag) &&
                                !action.Tags.Contains("unique"))
                .Select(action => action.Name)
                .ToList();
                
            return weaponActions;
        }
    }
}
