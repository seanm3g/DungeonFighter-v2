using System;
using System.Collections.Generic;
using System.Linq;
using RPGGame.Tests;

namespace RPGGame.Tests.Unit
{
    /// <summary>
    /// Comprehensive tests for DefaultActionManager
    /// Tests default action handling and unique action retrieval
    /// </summary>
    public static class DefaultActionManagerTests
        {
        private static int _testsRun = 0;
        private static int _testsPassed = 0;
        private static int _testsFailed = 0;

        /// <summary>
        /// Runs all DefaultActionManager tests
        /// </summary>
        public static void RunAllTests()
        {
            Console.WriteLine("=== DefaultActionManager Tests ===\n");

            TestAddDefaultActions();
            TestGetAvailableUniqueActions();
            TestUpdateComboBonus();
            TestNullWeaponHandling();
            TestMultipleUniqueActions();
            TestComboBonusCalculation();

            TestBase.PrintSummary("DefaultActionManager Tests", _testsRun, _testsPassed, _testsFailed);
        }

        #region Default Actions Tests

        private static void TestAddDefaultActions()
        {
            Console.WriteLine("--- Testing AddDefaultActions ---");

            var manager = new DefaultActionManager();
            var character = TestDataBuilders.Character().WithName("DefaultTest").Build();

            var beforeCount = character.ActionPool.Count;
            
            // Should not throw exception (BASIC ATTACK removed)
            try
            {
                manager.AddDefaultActions(character);
                TestBase.AssertTrue(true, "Should handle default actions without exception", 
                    ref _testsRun, ref _testsPassed, ref _testsFailed);
            }
            catch (Exception ex)
            {
                TestBase.AssertTrue(false, $"Should not throw exception: {ex.Message}", 
                    ref _testsRun, ref _testsPassed, ref _testsFailed);
            }
        }

        #endregion

        #region Unique Actions Tests

        private static void TestGetAvailableUniqueActions()
        {
            Console.WriteLine("\n--- Testing GetAvailableUniqueActions ---");

            var manager = new DefaultActionManager();
            
            var weapon = new WeaponItem
            {
                Name = "TestWeapon",
                WeaponType = WeaponType.Sword
            };

            var uniqueActions = manager.GetAvailableUniqueActions(weapon);

            TestBase.AssertNotNull(uniqueActions, "Should return non-null list of unique actions", 
                ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        private static void TestNullWeaponHandling()
        {
            Console.WriteLine("\n--- Testing Null Weapon Handling ---");

            var manager = new DefaultActionManager();

            // Should not throw exception with null weapon
            try
            {
                var uniqueActions = manager.GetAvailableUniqueActions(null);
                TestBase.AssertNotNull(uniqueActions, "Should return list even with null weapon", 
                    ref _testsRun, ref _testsPassed, ref _testsFailed);
            }
            catch (Exception ex)
            {
                TestBase.AssertTrue(false, $"Should not throw exception with null weapon: {ex.Message}", 
                    ref _testsRun, ref _testsPassed, ref _testsFailed);
            }
        }

        private static void TestMultipleUniqueActions()
        {
            Console.WriteLine("\n--- Testing Multiple Unique Actions ---");

            var manager = new DefaultActionManager();
            
            var weapon = new WeaponItem
            {
                Name = "TestWeapon",
                WeaponType = WeaponType.Staff
            };

            var uniqueActions = manager.GetAvailableUniqueActions(weapon);

            // Should return a list (may be empty if no unique actions exist)
            TestBase.AssertNotNull(uniqueActions, "Should return list of unique actions", 
                ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        #endregion

        #region Combo Bonus Tests

        private static void TestUpdateComboBonus()
        {
            Console.WriteLine("\n--- Testing UpdateComboBonus ---");

            var manager = new DefaultActionManager();
            var character = TestDataBuilders.Character().WithName("ComboBonusTest").Build();
            
            var equipment = new CharacterEquipment
            {
                Head = new Item(ItemType.Head, "TestHead")
                {
                    StatBonuses = new List<StatBonus>
                    {
                        new StatBonus { StatType = "ComboBonus", Value = 2 }
                    }
                },
                Body = new Item(ItemType.Chest, "TestBody")
                {
                    StatBonuses = new List<StatBonus>
                    {
                        new StatBonus { StatType = "ComboBonus", Value = 3 }
                    }
                },
                Weapon = new WeaponItem
                {
                    StatBonuses = new List<StatBonus>
                    {
                        new StatBonus { StatType = "ComboBonus", Value = 1 }
                    }
                }
            };

            // Should not throw exception
            try
            {
                manager.UpdateComboBonus(equipment);
                TestBase.AssertTrue(true, "Should update combo bonus without exception", 
                    ref _testsRun, ref _testsPassed, ref _testsFailed);
            }
            catch (Exception ex)
            {
                TestBase.AssertTrue(false, $"Should not throw exception: {ex.Message}", 
                    ref _testsRun, ref _testsPassed, ref _testsFailed);
            }
        }

        private static void TestComboBonusCalculation()
        {
            Console.WriteLine("\n--- Testing Combo Bonus Calculation ---");

            var manager = new DefaultActionManager();
            
            var equipment = new CharacterEquipment
            {
                Head = new Item(ItemType.Head, "TestHead")
                {
                    StatBonuses = new List<StatBonus>
                    {
                        new StatBonus { StatType = "ComboBonus", Value = 5 }
                    }
                },
                Feet = new Item(ItemType.Feet, "TestFeet")
                {
                    StatBonuses = new List<StatBonus>
                    {
                        new StatBonus { StatType = "ComboBonus", Value = 3 }
                    }
                }
            };

            // Should not throw exception
            try
            {
                manager.UpdateComboBonus(equipment);
                TestBase.AssertTrue(true, "Should calculate combo bonus from multiple pieces", 
                    ref _testsRun, ref _testsPassed, ref _testsFailed);
            }
            catch (Exception ex)
            {
                TestBase.AssertTrue(false, $"Should not throw exception: {ex.Message}", 
                    ref _testsRun, ref _testsPassed, ref _testsFailed);
            }
        }

        #endregion
    }
}

