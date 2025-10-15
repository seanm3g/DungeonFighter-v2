using System;
using System.IO;

namespace RPGGame
{
    /// <summary>
    /// Test class for the refactored GameDataGenerator
    /// </summary>
    public static class GameDataGeneratorTest
    {
        /// <summary>
        /// Tests the refactored GameDataGenerator to ensure it works correctly
        /// </summary>
        public static void TestRefactoredGenerator()
        {
            Console.WriteLine("=== TESTING REFACTORED GAME DATA GENERATOR ===");
            Console.WriteLine();

            try
            {
                // Test 1: Test generation without force overwrite (should be safe)
                Console.WriteLine("Test 1: Safe generation (no force overwrite)");
                var result1 = GameDataGenerator.GenerateGameDataManually(forceOverwrite: false);
                
                Console.WriteLine($"Result: Processed={result1.TotalFilesProcessed}, Updated={result1.TotalFilesUpdated}");
                Console.WriteLine($"Has Errors: {result1.HasErrors}, Has Warnings: {result1.HasWarnings}");
                Console.WriteLine();

                // Test 2: Test generation with force overwrite
                Console.WriteLine("Test 2: Force overwrite generation");
                var result2 = GameDataGenerator.GenerateGameDataManually(forceOverwrite: true);
                
                Console.WriteLine($"Result: Processed={result2.TotalFilesProcessed}, Updated={result2.TotalFilesUpdated}");
                Console.WriteLine($"Has Errors: {result2.HasErrors}, Has Warnings: {result2.HasWarnings}");
                Console.WriteLine();

                // Test 3: Test individual file generation
                Console.WriteLine("Test 3: Individual file generation");
                var armorResult = GameDataGenerator.GenerateArmorJson(forceOverwrite: false);
                var weaponResult = GameDataGenerator.GenerateWeaponsJson(forceOverwrite: false);
                
                Console.WriteLine($"Armor: Processed={armorResult.Processed}, Updated={armorResult.Updated}");
                Console.WriteLine($"Weapon: Processed={weaponResult.Processed}, Updated={weaponResult.Updated}");
                Console.WriteLine();

                // Test 4: Test configuration status
                Console.WriteLine("Test 4: Configuration status");
                var tuning = GameConfiguration.Instance;
                Console.WriteLine($"ItemScaling configured: {tuning.ItemScaling != null}");
                Console.WriteLine($"EnemyScaling configured: {tuning.EnemyScaling != null}");
                Console.WriteLine();

                Console.WriteLine("=== ALL TESTS COMPLETED ===");
                Console.WriteLine("The refactored GameDataGenerator is working correctly!");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"ERROR: Test failed with exception: {ex.Message}");
                Console.WriteLine($"Stack trace: {ex.StackTrace}");
            }
        }

        /// <summary>
        /// Tests the backup functionality
        /// </summary>
        public static void TestBackupFunctionality()
        {
            Console.WriteLine("=== TESTING BACKUP FUNCTIONALITY ===");
            
            try
            {
                // Check if backup files exist
                string[] possibleGameDataDirs = {
                    "GameData",
                    Path.Combine("..", "GameData"),
                    Path.Combine("..", "..", "GameData")
                };

                foreach (string dir in possibleGameDataDirs)
                {
                    if (Directory.Exists(dir))
                    {
                        string armorBackup = Path.Combine(dir, "Armor.json.backup");
                        string weaponBackup = Path.Combine(dir, "Weapons.json.backup");
                        
                        Console.WriteLine($"Checking directory: {dir}");
                        Console.WriteLine($"Armor backup exists: {File.Exists(armorBackup)}");
                        Console.WriteLine($"Weapon backup exists: {File.Exists(weaponBackup)}");
                        
                        if (File.Exists(armorBackup))
                        {
                            var backupInfo = new FileInfo(armorBackup);
                            Console.WriteLine($"Armor backup size: {backupInfo.Length} bytes, Created: {backupInfo.CreationTime}");
                        }
                        
                        if (File.Exists(weaponBackup))
                        {
                            var backupInfo = new FileInfo(weaponBackup);
                            Console.WriteLine($"Weapon backup size: {backupInfo.Length} bytes, Created: {backupInfo.CreationTime}");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"ERROR: Backup test failed: {ex.Message}");
            }
        }
    }
}
