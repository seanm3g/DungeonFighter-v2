using System;
using System.Text.Json;
using RPGGame;

namespace RPGGame
{
    class Program
    {
        static void Main(string[] args)
        {
            // Check for test commands
            if (args.Length > 0 && args[0] == "--test-loot")
            {
                LootGenerationTest.RunLootGenerationTests();
                return;
            }
            
            if (args.Length > 0 && args[0] == "--test-inventory")
            {
                InventoryDisplayTest.RunInventoryDisplayTest();
                return;
            }
            
            // Generate game data files based on TuningConfig at launch (if enabled)
            if (GameConfiguration.Instance.GameData.AutoGenerateOnLaunch)
            {
                try
                {
                    if (GameConfiguration.Instance.GameData.ShowGenerationMessages)
                    {
                        Console.WriteLine("Initializing game data...");
                    }
                    GameDataGenerator.GenerateAllGameData();
                    if (GameConfiguration.Instance.GameData.ShowGenerationMessages)
                    {
                        Console.WriteLine("Game data initialization complete!\n");
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Warning: Failed to generate game data files: {ex.Message}");
                    Console.WriteLine("Continuing with existing data files...\n");
                }
            }
            
            // GameData path resolution has been fixed
            
            // Create and run the game
            var game = new Game();
            game.ShowMainMenu();
        }

        // Utility methods that are still needed by other classes
        public static WeaponItem? CreateFallbackWeapon(int playerLevel)
        {
            try
            {
                // Try to load weapon data and create a tier 1 weapon as fallback
                string? filePath = FindGameDataFile("Weapons.json");
                if (filePath == null) 
                {
                    Console.WriteLine("   ERROR: Weapons.json file not found in any expected location");
                    return null;
                }
                
                string json = File.ReadAllText(filePath);
                var options = new System.Text.Json.JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                };
                var weaponData = System.Text.Json.JsonSerializer.Deserialize<List<WeaponData>>(json, options);
                if (weaponData == null) 
                {
                    Console.WriteLine("   ERROR: Failed to deserialize weapon data from Weapons.json");
                    return null;
                }
                
                // Find a tier 1 weapon
                var tier1Weapons = weaponData.Where(w => w.Tier == 1).ToList();
                if (!tier1Weapons.Any()) 
                {
                    Console.WriteLine($"   ERROR: No Tier 1 weapons found in Weapons.json (total weapons: {weaponData.Count})");
                    return null;
                }
                
                // Pick a random tier 1 weapon
                var random = new Random();
                var selectedWeapon = tier1Weapons[random.Next(tier1Weapons.Count)];
                
                var weaponType = Enum.Parse<WeaponType>(selectedWeapon.Type);
                var weapon = new WeaponItem(selectedWeapon.Name, selectedWeapon.Tier, 
                    selectedWeapon.BaseDamage, selectedWeapon.AttackSpeed, weaponType);
                weapon.Rarity = "Common";
                
                return weapon;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"   ERROR: Exception in CreateFallbackWeapon: {ex.Message}");
                return null;
            }
        }
        
        public static string? FindGameDataFile(string fileName)
        {
            // Try current directory first
            if (File.Exists(fileName)) return fileName;
            
            // Try GameData subdirectory
            string gameDataPath = Path.Combine("GameData", fileName);
            if (File.Exists(gameDataPath)) return gameDataPath;
            
            // Try parent directory + GameData
            string parentGameDataPath = Path.Combine("..", "GameData", fileName);
            if (File.Exists(parentGameDataPath)) return parentGameDataPath;
            
            return null;
        }
    }
}
