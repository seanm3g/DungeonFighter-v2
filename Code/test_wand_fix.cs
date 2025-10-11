using System;
using System.Collections.Generic;
using System.Linq;

namespace RPGGame
{
    class TestWandFix
    {
        public static void TestWandActionFix()
        {
            Console.WriteLine("=== Testing Wand Action Fix ===");
            
            // Create a character
            var character = new Character("TestWizard", 1);
            Console.WriteLine($"Character created with {character.ActionPool.Count} actions:");
            foreach (var actionEntry in character.ActionPool)
            {
                Console.WriteLine($"  - {actionEntry.action.Name} (Combo: {actionEntry.action.IsComboAction})");
            }
            
            // Create and equip a wand
            Console.WriteLine("\n=== Equipping Wand ===");
            var wand = new WeaponItem("Test Wand", 1, 5, 1.1, WeaponType.Wand);
            character.EquipItem(wand, "weapon");
            
            Console.WriteLine($"After equipping wand, character has {character.ActionPool.Count} actions:");
            foreach (var actionEntry in character.ActionPool)
            {
                Console.WriteLine($"  - {actionEntry.action.Name} (Combo: {actionEntry.action.IsComboAction})");
            }
            
            // Check if MAGIC MISSILE is in the action pool
            var magicMissile = character.ActionPool.FirstOrDefault(a => a.action.Name == "MAGIC MISSILE");
            var arcaneShield = character.ActionPool.FirstOrDefault(a => a.action.Name == "ARCANE SHIELD");
            
            Console.WriteLine($"\n=== Wand Action Check ===");
            Console.WriteLine($"MAGIC MISSILE found: {magicMissile.action != null}");
            Console.WriteLine($"ARCANE SHIELD found: {arcaneShield.action != null}");
            
            // Check combo sequence
            Console.WriteLine($"\n=== Combo Sequence ===");
            Console.WriteLine($"Combo sequence has {character.Actions.ComboSequence.Count} actions:");
            foreach (var action in character.Actions.ComboSequence)
            {
                Console.WriteLine($"  - {action.Name} (Order: {action.ComboOrder})");
            }
            
            Console.WriteLine("\nPress any key to exit...");
            Console.ReadKey();
        }
    }
}
