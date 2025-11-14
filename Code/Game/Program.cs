using System;
using System.Text.Json;
using RPGGame;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using RPGGame.UI.Avalonia;
using RPGGame.UI.Animations;

namespace RPGGame
{
    class Program
    {
        // Initialization code. Don't use any Avalonia, third-party APIs or any
        // SynchronizationContext-reliant code before AppMain is called: things aren't initialized
        // yet and stuff might break.
        [STAThread]
        public static void Main(string[] args)
        {
            // Check for UI mode argument
            if (args.Length > 0 && args[0] == "--console")
            {
                // Launch console UI
                LaunchConsoleUI(args);
            }
            else
            {
                // Launch Avalonia GUI (default)
                BuildAvaloniaApp().StartWithClassicDesktopLifetime(args);
            }
        }

        // Avalonia configuration, don't remove; also used by visual designer.
        public static AppBuilder BuildAvaloniaApp()
            => AppBuilder.Configure<App>()
                .UsePlatformDetect()
                .WithInterFont()
                .LogToTrace();

        private static void LaunchConsoleUI(string[] args)
        {
            // Test amplification scaling if requested
            if (args.Length > 0 && args[0] == "test-amplification")
            {
                TestAmplificationScaling();
                return;
            }
            
            // Test wand actions if requested (removed during cleanup)
            // if (args.Length > 0 && args[0] == "test-wand")
            // {
            //     WandActionTest.TestWandActions();
            //     return;
            // }
            // 
            // // Debug wand actions if requested
            // if (args.Length > 0 && args[0] == "debug-wand")
            // {
            //     DebugWandActions.DebugWandActionLoading();
            //     return;
            // }
            // 
            // // Simple wand test if requested
            // if (args.Length > 0 && args[0] == "simple-wand")
            // {
            //     SimpleWandTest.TestWandActions();
            //     return;
            // }
            // 
            // // Test wand fix if requested
            // if (args.Length > 0 && args[0] == "test-wand-fix")
            // {
            //     TestWandFix.TestWandActionFix();
            //     return;
            // }
            
            // Test game data generator if requested
            if (args.Length > 0 && args[0] == "test-generator")
            {
                GameDataGeneratorTest.TestRefactoredGenerator();
                GameDataGeneratorTest.TestBackupFunctionality();
                return;
            }
            
            // Manual game data generation if requested
            if (args.Length > 0 && args[0] == "generate-data")
            {
                bool forceOverwrite = args.Length > 1 && args[1] == "--force";
                GameDataGenerator.GenerateGameDataManually(forceOverwrite);
                return;
            }
            
            // Test fade animation system if requested
            if (args.Length > 0 && args[0] == "test-fade")
            {
                Console.WriteLine("TextFadeAnimatorExamples has been removed.");
                Console.WriteLine("Use the main game instead.");
                return;
            }
            
            
            // Initialize the UI configuration system
            UIManager.ReloadConfiguration();
            
            // Force reload configuration to ensure latest changes are loaded
            UIManager.ReloadConfiguration();
            
            // Show opening animation
            OpeningAnimation.ShowOpeningAnimation();
            
            // Generate game data files based on TuningConfig at launch (if enabled)
            if (GameConfiguration.Instance.GameData.AutoGenerateOnLaunch)
            {
                try
                {
                    if (GameConfiguration.Instance.GameData.ShowGenerationMessages)
                    {
                        TextDisplayIntegration.DisplaySystem("Initializing game data...");
                    }
                    
                    // Use configuration-controlled generation for automatic startup
                    var config = GameConfiguration.Instance.GameData;
                    var result = GameDataGenerator.GenerateAllGameData(forceOverwrite: config.ForceOverwriteOnAutoGenerate);
                    
                    if (GameConfiguration.Instance.GameData.ShowGenerationMessages)
                    {
                        if (result.HasErrors)
                        {
                            TextDisplayIntegration.DisplaySystem($"Warning: Game data generation completed with errors. Check console for details.");
                        }
                        else if (result.HasWarnings)
                        {
                            TextDisplayIntegration.DisplaySystem($"Game data generation completed with warnings. Check console for details.");
                        }
                        else if (result.TotalFilesUpdated > 0)
                        {
                            TextDisplayIntegration.DisplaySystem($"Game data updated: {result.TotalFilesUpdated} files modified.");
                        }
                        else
                        {
                            TextDisplayIntegration.DisplaySystem("Game data is up to date.");
                        }
                        TextDisplayIntegration.DisplayBlankLine();
                    }
                }
                catch (Exception ex)
                {
                    TextDisplayIntegration.DisplaySystem($"Warning: Failed to generate game data files: {ex.Message}");
                    TextDisplayIntegration.DisplaySystem("Continuing with existing data files...");
                    TextDisplayIntegration.DisplayBlankLine();
                }
            }
            
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
        
        /// <summary>
        /// Test method to verify amplification scaling works correctly
        /// </summary>
        static void TestAmplificationScaling()
        {
            Console.WriteLine("=== Amplification Scaling Test ===");
            Console.WriteLine("Testing Technique 1-10 amplification values:");
            Console.WriteLine();

            // Create a test character with different Technique values
            for (int tech = 1; tech <= 10; tech++)
            {
                var character = new Character("Test");
                character.Technique = tech;
                
                var calculator = new CharacterCombatCalculator(character);
                double amplification = calculator.GetComboAmplifier();
                
                Console.WriteLine($"Technique {tech,2}: {amplification:F3}x");
            }
            
            Console.WriteLine();
            Console.WriteLine("Expected values:");
            Console.WriteLine("Technique  1: 1.010x (base)");
            Console.WriteLine("Technique  2: 1.020x");
            Console.WriteLine("Technique  3: 1.030x");
            Console.WriteLine("Technique  4: 1.040x");
            Console.WriteLine("Technique  5: 1.050x (ComboAmplifierAtTech5)");
            Console.WriteLine("Technique  6: 1.063x (scaling to max)");
            Console.WriteLine("Technique 10: 1.126x (scaling to max)");
            Console.WriteLine();
            Console.WriteLine("Press any key to exit...");
            Console.ReadKey();
        }
    }
}
