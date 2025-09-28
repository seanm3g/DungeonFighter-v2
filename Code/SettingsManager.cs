using System;
using System.IO;

namespace RPGGame
{
    public class SettingsManager
    {
        public static void ShowSettings()
        {
            while (true)
            {
                Console.WriteLine("\n=== SETTINGS ===");
                Console.WriteLine();
                Console.WriteLine("1. Narrative Balance");
                Console.WriteLine("2. Combat Speed");
                Console.WriteLine("3. Difficulty");
                Console.WriteLine("4. Combat Display");
                Console.WriteLine("5. Gameplay Options");
                Console.WriteLine("6. Delete Saved Characters");
                Console.WriteLine("7. Back to Main Menu");
                Console.WriteLine();
                Console.Write("Choose an option: ");

                string? choice = Console.ReadLine();
                switch (choice)
                {
                    case "1":
                        ConfigureNarrativeBalance(GameSettings.Instance);
                        break;
                    case "2":
                        ConfigureCombatSpeed(GameSettings.Instance);
                        break;
                    case "3":
                        ConfigureDifficulty(GameSettings.Instance);
                        break;
                    case "4":
                        ConfigureCombatDisplay(GameSettings.Instance);
                        break;
                    case "5":
                        ConfigureGameplayOptions(GameSettings.Instance);
                        break;
                    case "6":
                        DeleteSavedCharacters();
                        break;
                    case "7":
                        return;
                    default:
                        Console.WriteLine("Invalid choice. Please try again.");
                        break;
                }
            }
        }

        private static void ConfigureNarrativeBalance(GameSettings settings)
        {
            Console.WriteLine("\n=== NARRATIVE BALANCE ===");
            Console.WriteLine($"Current balance: {settings.NarrativeBalance:F1} (0.0 = No narrative, 1.0 = Full narrative)");
            Console.WriteLine();
            Console.WriteLine("Choose narrative level:");
            Console.WriteLine("1. No narrative (0.0) - Pure combat focus");
            Console.WriteLine("2. Minimal narrative (0.2) - Brief descriptions");
            Console.WriteLine("3. Light narrative (0.4) - Some flavor text");
            Console.WriteLine("4. Balanced (0.6) - Good mix of combat and story");
            Console.WriteLine("5. Rich narrative (0.8) - Detailed descriptions");
            Console.WriteLine("6. Full narrative (1.0) - Maximum story immersion");
            Console.WriteLine("7. Custom value");
            Console.WriteLine("8. Back");
            Console.WriteLine();
            Console.Write("Choose an option: ");

            string? choice = Console.ReadLine();
            switch (choice)
            {
                case "1":
                    settings.NarrativeBalance = 0.0;
                    break;
                case "2":
                    settings.NarrativeBalance = 0.2;
                    break;
                case "3":
                    settings.NarrativeBalance = 0.4;
                    break;
                case "4":
                    settings.NarrativeBalance = 0.6;
                    break;
                case "5":
                    settings.NarrativeBalance = 0.8;
                    break;
                case "6":
                    settings.NarrativeBalance = 1.0;
                    break;
                case "7":
                    Console.Write("Enter custom value (0.0-1.0): ");
                    if (double.TryParse(Console.ReadLine(), out double customValue) && customValue >= 0.0 && customValue <= 1.0)
                    {
                        settings.NarrativeBalance = customValue;
                    }
                    else
                    {
                        Console.WriteLine("Invalid value. Must be between 0.0 and 1.0.");
                        return;
                    }
                    break;
                case "8":
                    return;
                default:
                    Console.WriteLine("Invalid choice.");
                    return;
            }

            settings.SaveSettings();
            Console.WriteLine($"Narrative balance set to {settings.NarrativeBalance:F1}");
        }

        private static void ConfigureCombatSpeed(GameSettings settings)
        {
            Console.WriteLine("\n=== COMBAT SPEED ===");
            Console.WriteLine($"Current speed: {settings.CombatSpeed:F1} (0.1 = Very slow, 2.0 = Very fast)");
            Console.WriteLine();
            Console.WriteLine("Choose combat speed:");
            Console.WriteLine("1. Very slow (0.1) - Maximum detail");
            Console.WriteLine("2. Slow (0.3) - Detailed combat");
            Console.WriteLine("3. Normal (0.5) - Balanced speed");
            Console.WriteLine("4. Fast (1.0) - Quick combat");
            Console.WriteLine("5. Very fast (2.0) - Minimal delays");
            Console.WriteLine("6. Custom value");
            Console.WriteLine("7. Back");
            Console.WriteLine();
            Console.Write("Choose an option: ");

            string? choice = Console.ReadLine();
            switch (choice)
            {
                case "1":
                    settings.CombatSpeed = 0.1;
                    break;
                case "2":
                    settings.CombatSpeed = 0.3;
                    break;
                case "3":
                    settings.CombatSpeed = 0.5;
                    break;
                case "4":
                    settings.CombatSpeed = 1.0;
                    break;
                case "5":
                    settings.CombatSpeed = 2.0;
                    break;
                case "6":
                    Console.Write("Enter custom value (0.1-5.0): ");
                    if (double.TryParse(Console.ReadLine(), out double customValue) && customValue >= 0.1 && customValue <= 5.0)
                    {
                        settings.CombatSpeed = customValue;
                    }
                    else
                    {
                        Console.WriteLine("Invalid value. Must be between 0.1 and 5.0.");
                        return;
                    }
                    break;
                case "7":
                    return;
                default:
                    Console.WriteLine("Invalid choice.");
                    return;
            }

            settings.SaveSettings();
            Console.WriteLine($"Combat speed set to {settings.CombatSpeed:F1}");
        }

        private static void ConfigureDifficulty(GameSettings settings)
        {
            Console.WriteLine("\n=== DIFFICULTY ===");
            Console.WriteLine($"Current enemy health multiplier: {settings.EnemyHealthMultiplier:F1}");
            Console.WriteLine($"Current enemy damage multiplier: {settings.EnemyDamageMultiplier:F1}");
            Console.WriteLine($"Current player health multiplier: {settings.PlayerHealthMultiplier:F1}");
            Console.WriteLine($"Current player damage multiplier: {settings.PlayerDamageMultiplier:F1}");
            Console.WriteLine();
            Console.WriteLine("Choose difficulty preset:");
            Console.WriteLine("1. Easy - Reduced enemy stats");
            Console.WriteLine("2. Normal - Standard difficulty");
            Console.WriteLine("3. Hard - Increased enemy stats");
            Console.WriteLine("4. Very Hard - Maximum challenge");
            Console.WriteLine("5. Back");
            Console.WriteLine();
            Console.Write("Choose an option: ");

            string? choice = Console.ReadLine();
            switch (choice)
            {
                case "1":
                    settings.EnemyHealthMultiplier = 0.5;
                    settings.EnemyDamageMultiplier = 0.5;
                    settings.PlayerHealthMultiplier = 1.5;
                    settings.PlayerDamageMultiplier = 1.5;
                    break;
                case "2":
                    settings.EnemyHealthMultiplier = 1.0;
                    settings.EnemyDamageMultiplier = 1.0;
                    settings.PlayerHealthMultiplier = 1.0;
                    settings.PlayerDamageMultiplier = 1.0;
                    break;
                case "3":
                    settings.EnemyHealthMultiplier = 1.5;
                    settings.EnemyDamageMultiplier = 1.5;
                    settings.PlayerHealthMultiplier = 0.8;
                    settings.PlayerDamageMultiplier = 0.8;
                    break;
                case "4":
                    settings.EnemyHealthMultiplier = 2.0;
                    settings.EnemyDamageMultiplier = 2.0;
                    settings.PlayerHealthMultiplier = 0.5;
                    settings.PlayerDamageMultiplier = 0.5;
                    break;
                case "5":
                    return;
                default:
                    Console.WriteLine("Invalid choice.");
                    return;
            }

            settings.SaveSettings();
            Console.WriteLine($"Difficulty preset applied!");
        }

        private static void ConfigureCombatDisplay(GameSettings settings)
        {
            Console.WriteLine("\n=== COMBAT DISPLAY ===");
            Console.WriteLine($"Show detailed stats: {settings.ShowDetailedStats}");
            Console.WriteLine($"Show health bars: {settings.ShowHealthBars}");
            Console.WriteLine($"Show damage numbers: {settings.ShowDamageNumbers}");
            Console.WriteLine($"Show combo progress: {settings.ShowComboProgress}");
            Console.WriteLine();
            Console.WriteLine("Choose display options:");
            Console.WriteLine("1. Toggle detailed stats");
            Console.WriteLine("2. Toggle health bars");
            Console.WriteLine("3. Toggle damage numbers");
            Console.WriteLine("4. Toggle combo progress");
            Console.WriteLine("5. Back");
            Console.WriteLine();
            Console.Write("Choose an option: ");

            string? choice = Console.ReadLine();
            switch (choice)
            {
                case "1":
                    settings.ShowDetailedStats = !settings.ShowDetailedStats;
                    Console.WriteLine($"Detailed stats: {settings.ShowDetailedStats}");
                    break;
                case "2":
                    settings.ShowHealthBars = !settings.ShowHealthBars;
                    Console.WriteLine($"Health bars: {settings.ShowHealthBars}");
                    break;
                case "3":
                    settings.ShowDamageNumbers = !settings.ShowDamageNumbers;
                    Console.WriteLine($"Damage numbers: {settings.ShowDamageNumbers}");
                    break;
                case "4":
                    settings.ShowComboProgress = !settings.ShowComboProgress;
                    Console.WriteLine($"Combo progress: {settings.ShowComboProgress}");
                    break;
                case "5":
                    return;
                default:
                    Console.WriteLine("Invalid choice.");
                    return;
            }

            settings.SaveSettings();
        }

        private static void ConfigureGameplayOptions(GameSettings settings)
        {
            Console.WriteLine("\n=== GAMEPLAY OPTIONS ===");
            Console.WriteLine($"Auto-save: {settings.EnableAutoSave}");
            Console.WriteLine($"Auto-save interval: {settings.AutoSaveInterval} encounters");
            Console.WriteLine($"Enable combo system: {settings.EnableComboSystem}");
            Console.WriteLine($"Enable text display delays: {settings.EnableTextDisplayDelays}");
            Console.WriteLine();
            Console.WriteLine("Choose gameplay options:");
            Console.WriteLine("1. Toggle auto-save");
            Console.WriteLine("2. Set auto-save interval");
            Console.WriteLine("3. Toggle combo system");
            Console.WriteLine("4. Toggle text display delays");
            Console.WriteLine("5. Back");
            Console.WriteLine();
            Console.Write("Choose an option: ");

            string? choice = Console.ReadLine();
            switch (choice)
            {
                case "1":
                    settings.EnableAutoSave = !settings.EnableAutoSave;
                    Console.WriteLine($"Auto-save: {settings.EnableAutoSave}");
                    break;
                case "2":
                    Console.Write("Enter auto-save interval (1-20 encounters): ");
                    if (int.TryParse(Console.ReadLine(), out int interval) && interval >= 1 && interval <= 20)
                    {
                        settings.AutoSaveInterval = interval;
                        Console.WriteLine($"Auto-save interval set to {interval} encounters");
                    }
                    else
                    {
                        Console.WriteLine("Invalid value. Must be between 1 and 20.");
                        return;
                    }
                    break;
                case "3":
                    settings.EnableComboSystem = !settings.EnableComboSystem;
                    Console.WriteLine($"Combo system: {settings.EnableComboSystem}");
                    break;
                case "4":
                    settings.EnableTextDisplayDelays = !settings.EnableTextDisplayDelays;
                    Console.WriteLine($"Text display delays: {settings.EnableTextDisplayDelays}");
                    break;
                case "5":
                    return;
                default:
                    Console.WriteLine("Invalid choice.");
                    return;
            }

            settings.SaveSettings();
        }

        private static void DeleteSavedCharacters()
        {
            Console.WriteLine("\n=== DELETE SAVED CHARACTERS ===");
            Console.WriteLine("This will permanently delete all saved character data.");
            Console.WriteLine("Are you sure you want to continue? (y/N)");
            Console.Write("Enter your choice: ");

            string? choice = Console.ReadLine()?.ToLower();
            if (choice == "y" || choice == "yes")
            {
                try
                {
                    string saveFile = "GameData/character_save.json";
                    if (File.Exists(saveFile))
                    {
                        File.Delete(saveFile);
                        Console.WriteLine("Saved characters deleted successfully.");
                    }
                    else
                    {
                        Console.WriteLine("No saved characters found.");
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error deleting saved characters: {ex.Message}");
                }
            }
            else
            {
                Console.WriteLine("Operation cancelled.");
            }
        }
    }
}
