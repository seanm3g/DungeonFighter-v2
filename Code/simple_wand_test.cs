using System;
using System.Collections.Generic;
using System.Linq;

namespace RPGGame
{
    class SimpleWandTest
    {
        public static void TestWandActions()
        {
            Console.WriteLine("=== Simple Wand Action Test ===");
            
            // Test ActionLoader directly
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
                Console.WriteLine($"  - {action.Name}");
                Console.WriteLine($"    Tags: {string.Join(", ", action.Tags)}");
                Console.WriteLine($"    IsComboAction: {action.IsComboAction}");
                Console.WriteLine($"    ComboOrder: {action.ComboOrder}");
            }
            
            // Test the GetWeaponActionsFromJson method directly
            Console.WriteLine("\n=== Testing GetWeaponActionsFromJson ===");
            var weaponActions = GetWeaponActionsFromJson(WeaponType.Wand);
            Console.WriteLine($"GetWeaponActionsFromJson returned: {weaponActions.Count} actions");
            foreach (var actionName in weaponActions)
            {
                Console.WriteLine($"  - {actionName}");
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
