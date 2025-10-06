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
                var settingsOptions = MenuConfiguration.GetSettingsMenuOptions();
                
                TextDisplayIntegration.DisplayMenu("", settingsOptions);
                Console.WriteLine(); // Ensure we're on a new line
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
                        TextDisplaySettings.ShowSettingsMenu();
                        break;
                    case "7":
                        DeleteSavedCharacters();
                        break;
                    case "0":
                        return;
                    default:
                        TextDisplayIntegration.DisplaySystem("Invalid choice. Please try again.");
                        break;
                }
            }
        }

        private static void ConfigureNarrativeBalance(GameSettings settings)
        {
            UIManager.WriteMenuLine("\n=== NARRATIVE BALANCE ===");
            UIManager.WriteMenuLine($"Current balance: {settings.NarrativeBalance:F1} (0.0 = No narrative, 1.0 = Full narrative)");
            UIManager.WriteMenuLine("");
            UIManager.WriteMenuLine("Choose narrative level:");
            UIManager.WriteMenuLine("1. No narrative (0.0) - Pure combat focus");
            UIManager.WriteMenuLine("2. Minimal narrative (0.2) - Brief descriptions");
            UIManager.WriteMenuLine("3. Light narrative (0.4) - Some flavor text");
            UIManager.WriteMenuLine("4. Balanced (0.6) - Good mix of combat and story");
            UIManager.WriteMenuLine("5. Rich narrative (0.8) - Detailed descriptions");
            UIManager.WriteMenuLine("6. Full narrative (1.0) - Maximum story immersion");
            UIManager.WriteMenuLine("7. Custom value");
            UIManager.WriteMenuLine("8. Back");
            UIManager.WriteMenuLine(""); // Blank line before prompt
            UIManager.Write("Choose an option: ");

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
                    UIManager.Write("Enter custom value (0.0-1.0): ");
                    if (double.TryParse(Console.ReadLine(), out double customValue) && customValue >= 0.0 && customValue <= 1.0)
                    {
                        settings.NarrativeBalance = customValue;
                    }
                    else
                    {
                        UIManager.WriteMenuLine("Invalid value. Must be between 0.0 and 1.0.");
                        return;
                    }
                    break;
                case "8":
                    return;
                default:
                    UIManager.WriteMenuLine("Invalid choice.");
                    return;
            }

            settings.SaveSettings();
            UIManager.WriteMenuLine($"Narrative balance set to {settings.NarrativeBalance:F1}");
        }

        private static void ConfigureCombatSpeed(GameSettings settings)
        {
            UIManager.WriteMenuLine("\n=== COMBAT SPEED ===");
            UIManager.WriteMenuLine($"Current speed: {settings.CombatSpeed:F1} (0.1 = Very slow, 2.0 = Very fast)");
            UIManager.WriteMenuLine($"Fast Combat: {settings.GetFastCombatDescription()}");
            UIManager.WriteMenuLine("");
            UIManager.WriteMenuLine("Choose combat speed:");
            UIManager.WriteMenuLine("1. Very slow (0.1) - Maximum detail");
            UIManager.WriteMenuLine("2. Slow (0.3) - Detailed combat");
            UIManager.WriteMenuLine("3. Normal (0.5) - Balanced speed");
            UIManager.WriteMenuLine("4. Fast (1.0) - Quick combat");
            UIManager.WriteMenuLine("5. Very fast (2.0) - Minimal delays");
            UIManager.WriteMenuLine("6. Custom value");
            UIManager.WriteMenuLine("7. Toggle FAST Combat (Zero delays)");
            UIManager.WriteMenuLine("8. Back");
            UIManager.WriteMenuLine(""); // Blank line before prompt
            UIManager.Write("Choose an option: ");

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
                    UIManager.Write("Enter custom value (0.1-5.0): ");
                    if (double.TryParse(Console.ReadLine(), out double customValue) && customValue >= 0.1 && customValue <= 5.0)
                    {
                        settings.CombatSpeed = customValue;
                    }
                    else
                    {
                        UIManager.WriteMenuLine("Invalid value. Must be between 0.1 and 5.0.");
                        return;
                    }
                    break;
                case "7":
                    settings.FastCombat = !settings.FastCombat;
                    UIManager.WriteMenuLine($"FAST Combat: {settings.GetFastCombatDescription()}");
                    break;
                case "8":
                    return;
                default:
                    UIManager.WriteMenuLine("Invalid choice.");
                    return;
            }

            settings.SaveSettings();
            UIManager.WriteMenuLine($"Combat speed set to {settings.CombatSpeed:F1}");
        }

        private static void ConfigureDifficulty(GameSettings settings)
        {
            UIManager.WriteMenuLine("\n=== DIFFICULTY ===");
            UIManager.WriteMenuLine($"Current enemy health multiplier: {settings.EnemyHealthMultiplier:F1}");
            UIManager.WriteMenuLine($"Current enemy damage multiplier: {settings.EnemyDamageMultiplier:F1}");
            UIManager.WriteMenuLine($"Current player health multiplier: {settings.PlayerHealthMultiplier:F1}");
            UIManager.WriteMenuLine($"Current player damage multiplier: {settings.PlayerDamageMultiplier:F1}");
            UIManager.WriteMenuLine("");
            UIManager.WriteMenuLine("Choose difficulty preset:");
            UIManager.WriteMenuLine("1. Easy - Reduced enemy stats");
            UIManager.WriteMenuLine("2. Normal - Standard difficulty");
            UIManager.WriteMenuLine("3. Hard - Increased enemy stats");
            UIManager.WriteMenuLine("4. Very Hard - Maximum challenge");
            UIManager.WriteMenuLine("5. Back");
            UIManager.WriteMenuLine("");
            UIManager.Write("Choose an option: ");

            string? choice = Console.ReadLine();
            switch (choice)
            {
                case "1":
                    // Easy difficulty - use TuningConfig values
                    var easyConfig = GameConfiguration.Instance.DifficultySettings.Easy;
                    settings.EnemyHealthMultiplier = easyConfig.EnemyHealthMultiplier;
                    settings.EnemyDamageMultiplier = easyConfig.EnemyDamageMultiplier;
                    settings.PlayerHealthMultiplier = 1.0 / easyConfig.EnemyHealthMultiplier; // Inverse for player
                    settings.PlayerDamageMultiplier = 1.0 / easyConfig.EnemyDamageMultiplier; // Inverse for player
                    break;
                case "2":
                    // Normal difficulty - use TuningConfig values
                    var normalConfig = GameConfiguration.Instance.DifficultySettings.Normal;
                    settings.EnemyHealthMultiplier = normalConfig.EnemyHealthMultiplier;
                    settings.EnemyDamageMultiplier = normalConfig.EnemyDamageMultiplier;
                    settings.PlayerHealthMultiplier = 1.0;
                    settings.PlayerDamageMultiplier = 1.0;
                    break;
                case "3":
                    // Hard difficulty - use TuningConfig values
                    var hardConfig = GameConfiguration.Instance.DifficultySettings.Hard;
                    settings.EnemyHealthMultiplier = hardConfig.EnemyHealthMultiplier;
                    settings.EnemyDamageMultiplier = hardConfig.EnemyDamageMultiplier;
                    settings.PlayerHealthMultiplier = 1.0 / hardConfig.EnemyHealthMultiplier; // Inverse for player
                    settings.PlayerDamageMultiplier = 1.0 / hardConfig.EnemyDamageMultiplier; // Inverse for player
                    break;
                case "4":
                    // Extreme difficulty - custom values
                    settings.EnemyHealthMultiplier = 2.0;
                    settings.EnemyDamageMultiplier = 2.0;
                    settings.PlayerHealthMultiplier = 0.5;
                    settings.PlayerDamageMultiplier = 0.5;
                    break;
                case "5":
                    return;
                default:
                    UIManager.WriteMenuLine("Invalid choice.");
                    return;
            }

            settings.SaveSettings();
            UIManager.WriteMenuLine($"Difficulty preset applied!");
        }

        private static void ConfigureCombatDisplay(GameSettings settings)
        {
            UIManager.WriteMenuLine("\n=== COMBAT DISPLAY ===");
            UIManager.WriteMenuLine($"Show detailed stats: {settings.ShowDetailedStats}");
            UIManager.WriteMenuLine($"Show health bars: {settings.ShowHealthBars}");
            UIManager.WriteMenuLine($"Show damage numbers: {settings.ShowDamageNumbers}");
            UIManager.WriteMenuLine($"Show combo progress: {settings.ShowComboProgress}");
            UIManager.WriteMenuLine("");
            UIManager.WriteMenuLine("Choose display options:");
            UIManager.WriteMenuLine("1. Toggle detailed stats");
            UIManager.WriteMenuLine("2. Toggle health bars");
            UIManager.WriteMenuLine("3. Toggle damage numbers");
            UIManager.WriteMenuLine("4. Toggle combo progress");
            UIManager.WriteMenuLine("5. Back");
            UIManager.WriteMenuLine("");
            UIManager.Write("Choose an option: ");

            string? choice = Console.ReadLine();
            switch (choice)
            {
                case "1":
                    settings.ShowDetailedStats = !settings.ShowDetailedStats;
                    UIManager.WriteMenuLine($"Detailed stats: {settings.ShowDetailedStats}");
                    break;
                case "2":
                    settings.ShowHealthBars = !settings.ShowHealthBars;
                    UIManager.WriteMenuLine($"Health bars: {settings.ShowHealthBars}");
                    break;
                case "3":
                    settings.ShowDamageNumbers = !settings.ShowDamageNumbers;
                    UIManager.WriteMenuLine($"Damage numbers: {settings.ShowDamageNumbers}");
                    break;
                case "4":
                    settings.ShowComboProgress = !settings.ShowComboProgress;
                    UIManager.WriteMenuLine($"Combo progress: {settings.ShowComboProgress}");
                    break;
                case "5":
                    return;
                default:
                    UIManager.WriteMenuLine("Invalid choice.");
                    return;
            }

            settings.SaveSettings();
        }

        private static void ConfigureGameplayOptions(GameSettings settings)
        {
            UIManager.WriteMenuLine("\n=== GAMEPLAY OPTIONS ===");
            UIManager.WriteMenuLine($"Auto-save: {settings.EnableAutoSave}");
            UIManager.WriteMenuLine($"Auto-save interval: {settings.AutoSaveInterval} encounters");
            UIManager.WriteMenuLine($"Enable combo system: {settings.EnableComboSystem}");
            UIManager.WriteMenuLine($"Enable text display delays: {settings.EnableTextDisplayDelays}");
            UIManager.WriteMenuLine("");
            UIManager.WriteMenuLine("Choose gameplay options:");
            UIManager.WriteMenuLine("1. Toggle auto-save");
            UIManager.WriteMenuLine("2. Set auto-save interval");
            UIManager.WriteMenuLine("3. Toggle combo system");
            UIManager.WriteMenuLine("4. Toggle text display delays");
            UIManager.WriteMenuLine("5. Back");
            UIManager.WriteMenuLine("");
            UIManager.Write("Choose an option: ");

            string? choice = Console.ReadLine();
            switch (choice)
            {
                case "1":
                    settings.EnableAutoSave = !settings.EnableAutoSave;
                    UIManager.WriteMenuLine($"Auto-save: {settings.EnableAutoSave}");
                    break;
                case "2":
                    UIManager.Write("Enter auto-save interval (1-20 encounters): ");
                    if (int.TryParse(Console.ReadLine(), out int interval) && interval >= 1 && interval <= 20)
                    {
                        settings.AutoSaveInterval = interval;
                        UIManager.WriteMenuLine($"Auto-save interval set to {interval} encounters");
                    }
                    else
                    {
                        UIManager.WriteMenuLine("Invalid value. Must be between 1 and 20.");
                        return;
                    }
                    break;
                case "3":
                    settings.EnableComboSystem = !settings.EnableComboSystem;
                    UIManager.WriteMenuLine($"Combo system: {settings.EnableComboSystem}");
                    break;
                case "4":
                    settings.EnableTextDisplayDelays = !settings.EnableTextDisplayDelays;
                    UIManager.WriteMenuLine($"Text display delays: {settings.EnableTextDisplayDelays}");
                    break;
                case "5":
                    return;
                default:
                    UIManager.WriteMenuLine("Invalid choice.");
                    return;
            }

            settings.SaveSettings();
        }

        private static void DeleteSavedCharacters()
        {
            UIManager.WriteMenuLine("\n=== DELETE SAVED CHARACTERS ===");
            UIManager.WriteMenuLine("This will permanently delete all saved character data.");
            UIManager.WriteMenuLine("Are you sure you want to continue? (y/N)");
            UIManager.Write("Enter your choice: ");

            string? choice = Console.ReadLine()?.ToLower();
            if (choice == "y" || choice == "yes")
            {
                try
                {
                    string saveFile = GameConstants.GetGameDataFilePath(GameConstants.CharacterSaveJson);
                    if (File.Exists(saveFile))
                    {
                        File.Delete(saveFile);
                        UIManager.WriteMenuLine("Saved characters deleted successfully.");
                    }
                    else
                    {
                        UIManager.WriteMenuLine("No saved characters found.");
                    }
                }
                catch (Exception ex)
                {
                    UIManager.WriteMenuLine($"Error deleting saved characters: {ex.Message}");
                }
            }
            else
            {
                UIManager.WriteMenuLine("Operation cancelled.");
            }
        }

        /// <summary>
        /// Calculates approximate DPS for an enemy based on their stats
        /// </summary>
        private static double CalculateApproximateDPS(Enemy enemy)
        {
            // Simple DPS calculation: (Strength * 0.5) / (Attack Speed * 0.1)
            // This is a rough approximation for display purposes
            double baseDamage = enemy.Strength * 0.5;
            double attackSpeed = enemy.GetTotalAttackSpeed() * 0.1;
            return attackSpeed > 0 ? baseDamage / attackSpeed : 0;
        }
    }
}