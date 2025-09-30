using System;
using System.Collections.Generic;
using System.Linq;

namespace RPGGame
{
    /// <summary>
    /// Test for inventory display system to verify stat bonuses and modifications are shown correctly
    /// </summary>
    public class InventoryDisplayTest
    {
        public static void RunInventoryDisplayTest()
        {
            Console.WriteLine("=== INVENTORY DISPLAY TEST ===");
            Console.WriteLine();

            // Create a test character
            var testCharacter = new Character("TestHero", 5);
            
            // Create some test items with bonuses
            var testWeapon = CreateTestWeaponWithBonuses();
            var testArmor = CreateTestArmorWithBonuses();
            
            // Equip the test items
            testCharacter.EquipItem(testWeapon, "weapon");
            testCharacter.EquipItem(testArmor, "head");
            
            // Create inventory with some items
            var inventory = new List<Item>
            {
                CreateTestWeaponWithBonuses(),
                CreateTestArmorWithBonuses(),
                CreateTestItemWithoutBonuses()
            };
            
            // Create display manager and show the display
            var displayManager = new InventoryDisplayManager(testCharacter, inventory);
            
            Console.WriteLine("Testing inventory display with equipped items and inventory items...");
            Console.WriteLine();
            
            displayManager.ShowMainDisplay();
            
            Console.WriteLine();
            Console.WriteLine("=== INVENTORY DISPLAY TEST COMPLETE ===");
        }

        private static WeaponItem CreateTestWeaponWithBonuses()
        {
            var weapon = new WeaponItem("Test Sword", 2, 10, 1.0, WeaponType.Sword);
            weapon.Rarity = "Rare";
            
            // Add some stat bonuses
            weapon.StatBonuses.Add(new StatBonus { StatType = "Strength", Value = 3 });
            weapon.StatBonuses.Add(new StatBonus { StatType = "Agility", Value = 2 });
            
            // Add some modifications
            weapon.Modifications.Add(new Modification 
            { 
                Name = "Sharp", 
                Description = "Increases damage", 
                RolledValue = 5.0,
                Effect = "damage_bonus"
            });
            
            return weapon;
        }

        private static HeadItem CreateTestArmorWithBonuses()
        {
            var armor = new HeadItem("Test Helmet", 2, 8);
            armor.Rarity = "Uncommon";
            
            // Add some stat bonuses
            armor.StatBonuses.Add(new StatBonus { StatType = "Intelligence", Value = 4 });
            
            // Add some modifications
            armor.Modifications.Add(new Modification 
            { 
                Name = "Sturdy", 
                Description = "Increases armor", 
                RolledValue = 3.0,
                Effect = "armor_bonus"
            });
            
            return armor;
        }

        private static Item CreateTestItemWithoutBonuses()
        {
            return new WeaponItem("Basic Sword", 1, 5, 1.0, WeaponType.Sword);
        }
    }
}
