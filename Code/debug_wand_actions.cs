using System;
using System.Collections.Generic;
using System.Linq;

namespace RPGGame
{
    class DebugWandActions
    {
        public static void DebugWandActionLoading()
        {
            Console.WriteLine("=== Debugging Wand Action Loading ===");
            
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
                Console.WriteLine($"  - {action.Name} (Tags: {string.Join(", ", action.Tags)})");
                Console.WriteLine($"    ComboOrder: {action.ComboOrder}, IsComboAction: {action.IsComboAction}");
            }
            
            // Test GetWeaponActionsFromJson method
            var weaponActions = GetWeaponActionsFromJson(WeaponType.Wand);
            Console.WriteLine($"GetWeaponActionsFromJson returned: {weaponActions.Count} actions");
            foreach (var actionName in weaponActions)
            {
                Console.WriteLine($"  - {actionName}");
            }
            
            // Test individual action loading
            Console.WriteLine("\n=== Testing Individual Action Loading ===");
            var magicMissile = ActionLoader.GetAction("MAGIC MISSILE");
            var arcaneShield = ActionLoader.GetAction("ARCANE SHIELD");
            
            Console.WriteLine($"MAGIC MISSILE loaded: {magicMissile != null}");
            if (magicMissile != null)
            {
                Console.WriteLine($"  - Tags: {string.Join(", ", magicMissile.Tags)}");
                Console.WriteLine($"  - IsComboAction: {magicMissile.IsComboAction}");
            }
            
            Console.WriteLine($"ARCANE SHIELD loaded: {arcaneShield != null}");
            if (arcaneShield != null)
            {
                Console.WriteLine($"  - Tags: {string.Join(", ", arcaneShield.Tags)}");
                Console.WriteLine($"  - IsComboAction: {arcaneShield.IsComboAction}");
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
