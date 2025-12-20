using System;
using System.Text.Json;
using RPGGame;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using RPGGame.UI.Avalonia;

namespace RPGGame
{
    class Program
    {
        // Initialization code. Don't use any Avalonia, third-party APIs or any
        // SynchronizationContext-reliant code before AppMain is called: things aren't initialized
        // yet and stuff might break.
        [STAThread]
        public static async Task Main(string[] args)
        {
            // Check if MCP mode is requested
            if (args.Length > 0 && args[0] == "MCP")
            {
                // Run MCP server instead of GUI
                await RPGGame.MCP.MCPServerProgram.RunMCPServer(args);
                return;
            }

            // Check if interactive play mode is requested
            if (args.Length > 0 && args[0].Equals("PLAY", StringComparison.OrdinalIgnoreCase))
            {
                // Run interactive game player using MCP tools
                await RPGGame.Game.InteractiveMCPGamePlayer.Main(args);
                return;
            }

            // Check if automated demo mode is requested
            if (args.Length > 0 && args[0].Equals("DEMO", StringComparison.OrdinalIgnoreCase))
            {
                // Run automated gameplay demo using MCP tools
                await RPGGame.Game.AutomatedGameplayDemo.Main(args);
                return;
            }

            // Check if Claude AI mode is requested
            if (args.Length > 0 && args[0].Equals("CLAUDE", StringComparison.OrdinalIgnoreCase))
            {
                // Run Claude AI game player with strategic decisions
                await RPGGame.Game.ClaudeAIGamePlayer.Main(args);
                return;
            }

            // Check if tuning mode is requested
            if (args.Length > 0 && args[0] == "TUNING")
            {
                // Run balance tuning runner
                int iterations = args.Length > 1 && int.TryParse(args[1], out int iter) ? iter : 5;
                await RPGGame.Tuning.TuningRunner.RunTuning(iterations);
                return;
            }

            // Check if test mode is requested
            if (args.Length > 0 && args[0] == "TEST")
            {
                // Run test battle comparison
                await RPGGame.Tests.TestBattleComparison.Main(args);
                return;
            }

            // Launch Avalonia GUI
            BuildAvaloniaApp().StartWithClassicDesktopLifetime(args);
        }

        // Avalonia configuration, don't remove; also used by visual designer.
        public static AppBuilder BuildAvaloniaApp()
            => AppBuilder.Configure<App>()
                .UsePlatformDetect()
                .WithInterFont()
                .LogToTrace();

        // Utility methods that are still needed by other classes
        public static async Task<WeaponItem?> CreateFallbackWeaponAsync(int playerLevel)
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

                string json = await File.ReadAllTextAsync(filePath);
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

        // Synchronous version for backward compatibility
        public static WeaponItem? CreateFallbackWeapon(int playerLevel)
        {
            return CreateFallbackWeaponAsync(playerLevel).GetAwaiter().GetResult();
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
