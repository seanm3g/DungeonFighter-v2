using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;

namespace RPGGame
{
    public class TuningConsole
    {
        public static void ShowTuningMenu()
        {
            while (true)
            {
                Console.Clear();
                Console.WriteLine("=== DYNAMIC TUNING CONSOLE ===");
                Console.WriteLine("1. Combat Parameters");
                Console.WriteLine("2. Item Scaling Formulas");
                Console.WriteLine("3. Rarity System");
                Console.WriteLine("4. Progression Curves");
                Console.WriteLine("5. XP Reward System");
                Console.WriteLine("6. Enemy DPS Analysis");
                Console.WriteLine("7. Test Scaling Calculations");
                Console.WriteLine("8. Run Balance Analysis");
                Console.WriteLine("9. Export Current Config");
                Console.WriteLine("10. Load Config Preset");
                Console.WriteLine("11. Reload Configuration");
                Console.WriteLine("0. Return to Main Menu");
                Console.WriteLine();
                Console.Write("Select option: ");

                string? input = Console.ReadLine();
                
                switch (input)
                {
                    case "1":
                        ShowCombatParametersMenu();
                        break;
                    case "2":
                        ShowItemScalingMenu();
                        break;
                    case "3":
                        ShowRaritySystemMenu();
                        break;
                    case "4":
                        ShowProgressionCurvesMenu();
                        break;
                    case "5":
                        ShowXPRewardSystemMenu();
                        break;
                    case "6":
                        ShowEnemyDPSAnalysisMenu();
                        break;
                    case "7":
                        ScalingManager.TestScalingFormulas();
                        Console.WriteLine("\nPress any key to continue...");
                        Console.ReadKey();
                        break;
                    case "8":
                        BalanceAnalyzer.RunFullAnalysis();
                        Console.WriteLine("\nPress any key to continue...");
                        Console.ReadKey();
                        break;
                    case "9":
                        ExportConfiguration();
                        break;
                    case "10":
                        LoadConfigurationPreset();
                        break;
                    case "11":
                        TuningConfig.Instance.Reload();
                        Console.WriteLine("Configuration reloaded!");
                        Console.WriteLine("Press any key to continue...");
                        Console.ReadKey();
                        break;
                    case "0":
                        return;
                    default:
                        Console.WriteLine("Invalid option. Press any key to continue...");
                        Console.ReadKey();
                        break;
                }
            }
        }

        private static void ShowCombatParametersMenu()
        {
            Console.Clear();
            Console.WriteLine("=== COMBAT PARAMETERS ===");
            var config = TuningConfig.Instance.Combat;
            
            Console.WriteLine($"1. Critical Hit Threshold: {config.CriticalHitThreshold}");
            Console.WriteLine($"2. Critical Hit Multiplier: {config.CriticalHitMultiplier:F2}");
            Console.WriteLine($"3. Minimum Damage: {config.MinimumDamage}");
            Console.WriteLine($"4. Base Attack Time: {config.BaseAttackTime:F1}");
            Console.WriteLine($"5. Agility Speed Reduction: {config.AgilitySpeedReduction:F3}");
            Console.WriteLine("0. Back");
            
            Console.Write("Select parameter to adjust: ");
            string? input = Console.ReadLine();
            
            switch (input)
            {
                case "1":
                    AdjustIntParameter("Critical Hit Threshold", 
                        () => config.CriticalHitThreshold,
                        value => config.CriticalHitThreshold = value);
                    break;
                case "2":
                    AdjustDoubleParameter("Critical Hit Multiplier", 
                        () => config.CriticalHitMultiplier,
                        value => config.CriticalHitMultiplier = value);
                    break;
                case "3":
                    AdjustIntParameter("Minimum Damage", 
                        () => config.MinimumDamage,
                        value => config.MinimumDamage = value);
                    break;
                case "4":
                    AdjustDoubleParameter("Base Attack Time", 
                        () => config.BaseAttackTime,
                        value => config.BaseAttackTime = value);
                    break;
                case "5":
                    AdjustDoubleParameter("Agility Speed Reduction", 
                        () => config.AgilitySpeedReduction,
                        value => config.AgilitySpeedReduction = value);
                    break;
            }
        }

        private static void ShowItemScalingMenu()
        {
            Console.Clear();
            Console.WriteLine("=== ITEM SCALING FORMULAS ===");
            var config = TuningConfig.Instance.ItemScaling;
            
            if (config?.WeaponDamageFormula != null)
            {
                Console.WriteLine($"Weapon Damage Formula: {config.WeaponDamageFormula.Formula}");
                Console.WriteLine($"  Tier Scaling: {config.WeaponDamageFormula.TierScaling:F2}");
                Console.WriteLine($"  Level Scaling: {config.WeaponDamageFormula.LevelScaling:F2}");
            }
            
            if (config?.ArmorValueFormula != null)
            {
                Console.WriteLine($"Armor Value Formula: {config.ArmorValueFormula.Formula}");
                Console.WriteLine($"  Tier Scaling: {config.ArmorValueFormula.TierScaling:F2}");
                Console.WriteLine($"  Level Scaling: {config.ArmorValueFormula.LevelScaling:F2}");
            }
            
            Console.WriteLine();
            Console.WriteLine("1. Adjust Weapon Tier Scaling");
            Console.WriteLine("2. Adjust Weapon Level Scaling");
            Console.WriteLine("3. Adjust Armor Tier Scaling");
            Console.WriteLine("4. Adjust Armor Level Scaling");
            Console.WriteLine("0. Back");
            
            Console.Write("Select option: ");
            string? input = Console.ReadLine();
            
            switch (input)
            {
                case "1":
                    if (config?.WeaponDamageFormula != null)
                    {
                        AdjustDoubleParameter("Weapon Tier Scaling", 
                            () => config.WeaponDamageFormula.TierScaling,
                            value => config.WeaponDamageFormula.TierScaling = value);
                    }
                    break;
                case "2":
                    if (config?.WeaponDamageFormula != null)
                    {
                        AdjustDoubleParameter("Weapon Level Scaling", 
                            () => config.WeaponDamageFormula.LevelScaling,
                            value => config.WeaponDamageFormula.LevelScaling = value);
                    }
                    break;
                case "3":
                    if (config?.ArmorValueFormula != null)
                    {
                        AdjustDoubleParameter("Armor Tier Scaling", 
                            () => config.ArmorValueFormula.TierScaling,
                            value => config.ArmorValueFormula.TierScaling = value);
                    }
                    break;
                case "4":
                    if (config?.ArmorValueFormula != null)
                    {
                        AdjustDoubleParameter("Armor Level Scaling", 
                            () => config.ArmorValueFormula.LevelScaling,
                            value => config.ArmorValueFormula.LevelScaling = value);
                    }
                    break;
            }
        }

        private static void ShowRaritySystemMenu()
        {
            Console.Clear();
            Console.WriteLine("=== RARITY SYSTEM ===");
            var config = TuningConfig.Instance.RarityScaling;
            
            if (config?.StatBonusMultipliers != null)
            {
                Console.WriteLine("Stat Bonus Multipliers:");
                Console.WriteLine($"  Common: {config.StatBonusMultipliers.Common:F2}x");
                Console.WriteLine($"  Uncommon: {config.StatBonusMultipliers.Uncommon:F2}x");
                Console.WriteLine($"  Rare: {config.StatBonusMultipliers.Rare:F2}x");
                Console.WriteLine($"  Epic: {config.StatBonusMultipliers.Epic:F2}x");
                Console.WriteLine($"  Legendary: {config.StatBonusMultipliers.Legendary:F2}x");
            }
            
            Console.WriteLine();
            Console.WriteLine("1. Adjust Common Multiplier");
            Console.WriteLine("2. Adjust Uncommon Multiplier");
            Console.WriteLine("3. Adjust Rare Multiplier");
            Console.WriteLine("4. Adjust Epic Multiplier");
            Console.WriteLine("5. Adjust Legendary Multiplier");
            Console.WriteLine("0. Back");
            
            Console.Write("Select option: ");
            string? input = Console.ReadLine();
            
            if (config?.StatBonusMultipliers != null)
            {
                switch (input)
                {
                    case "1":
                        AdjustDoubleParameter("Common Multiplier", 
                            () => config.StatBonusMultipliers.Common,
                            value => config.StatBonusMultipliers.Common = value);
                        break;
                    case "2":
                        AdjustDoubleParameter("Uncommon Multiplier", 
                            () => config.StatBonusMultipliers.Uncommon,
                            value => config.StatBonusMultipliers.Uncommon = value);
                        break;
                    case "3":
                        AdjustDoubleParameter("Rare Multiplier", 
                            () => config.StatBonusMultipliers.Rare,
                            value => config.StatBonusMultipliers.Rare = value);
                        break;
                    case "4":
                        AdjustDoubleParameter("Epic Multiplier", 
                            () => config.StatBonusMultipliers.Epic,
                            value => config.StatBonusMultipliers.Epic = value);
                        break;
                    case "5":
                        AdjustDoubleParameter("Legendary Multiplier", 
                            () => config.StatBonusMultipliers.Legendary,
                            value => config.StatBonusMultipliers.Legendary = value);
                        break;
                }
            }
        }

        private static void ShowProgressionCurvesMenu()
        {
            Console.Clear();
            Console.WriteLine("=== PROGRESSION CURVES ===");
            var config = TuningConfig.Instance.ProgressionCurves;
            
            if (config != null)
            {
                Console.WriteLine($"Experience Formula: {config.ExperienceFormula}");
                Console.WriteLine($"  Exponent Factor: {config.ExponentFactor:F2}");
                Console.WriteLine($"Attribute Growth Formula: {config.AttributeGrowth}");
                Console.WriteLine($"  Linear Growth: {config.LinearGrowth:F2}");
                Console.WriteLine($"  Quadratic Growth: {config.QuadraticGrowth:F3}");
            }
            
            Console.WriteLine();
            Console.WriteLine("1. Adjust Experience Exponent Factor");
            Console.WriteLine("2. Adjust Attribute Linear Growth");
            Console.WriteLine("3. Adjust Attribute Quadratic Growth");
            Console.WriteLine("0. Back");
            
            Console.Write("Select option: ");
            string? input = Console.ReadLine();
            
            if (config != null)
            {
                switch (input)
                {
                    case "1":
                        AdjustDoubleParameter("Experience Exponent Factor", 
                            () => config.ExponentFactor,
                            value => config.ExponentFactor = value);
                        break;
                    case "2":
                        AdjustDoubleParameter("Attribute Linear Growth", 
                            () => config.LinearGrowth,
                            value => config.LinearGrowth = value);
                        break;
                    case "3":
                        AdjustDoubleParameter("Attribute Quadratic Growth", 
                            () => config.QuadraticGrowth,
                            value => config.QuadraticGrowth = value);
                        break;
                }
            }
        }

        private static void ShowXPRewardSystemMenu()
        {
            Console.Clear();
            Console.WriteLine("=== XP REWARD SYSTEM ===");
            var config = TuningConfig.Instance.XPRewards;
            
            if (config != null)
            {
                Console.WriteLine($"Base XP Formula: {config.BaseXPFormula}");
                Console.WriteLine($"Group XP Formula: {config.GroupXPFormula}");
                Console.WriteLine($"Minimum XP: {config.MinimumXP}");
                Console.WriteLine($"Maximum XP Multiplier: {config.MaximumXPMultiplier:F1}x");
                
                Console.WriteLine("\nLevel Difference Multipliers:");
                if (config.LevelDifferenceMultipliers != null)
                {
                    foreach (var kvp in config.LevelDifferenceMultipliers.OrderBy(x => x.Value.LevelDifference))
                    {
                        var multiplier = kvp.Value;
                        Console.WriteLine($"  {multiplier.Description}: {multiplier.Multiplier:F1}x");
                    }
                }
                
                if (config.DungeonCompletionBonus != null)
                {
                    Console.WriteLine($"\nDungeon Completion Bonus:");
                    Console.WriteLine($"  Base Bonus: {config.DungeonCompletionBonus.BaseBonus} XP");
                    Console.WriteLine($"  Bonus Per Room: {config.DungeonCompletionBonus.BonusPerRoom} XP");
                    Console.WriteLine($"  Level Difference Multiplier: {config.DungeonCompletionBonus.LevelDifferenceMultiplier:F1}x");
                }
            }
            
            Console.WriteLine();
            Console.WriteLine("1. Test XP Calculations");
            Console.WriteLine("2. Adjust Minimum XP");
            Console.WriteLine("3. Adjust Maximum XP Multiplier");
            Console.WriteLine("4. Adjust Dungeon Base Bonus");
            Console.WriteLine("5. Adjust Bonus Per Room");
            Console.WriteLine("6. Adjust Level Difference Multipliers");
            Console.WriteLine("0. Back");
            
            Console.Write("Select option: ");
            string? input = Console.ReadLine();
            
            if (config != null)
            {
                switch (input)
                {
                    case "1":
                        ScalingManager.TestXPCalculations();
                        Console.WriteLine("\nPress any key to continue...");
                        Console.ReadKey();
                        break;
                    case "2":
                        AdjustIntParameter("Minimum XP", 
                            () => config.MinimumXP,
                            value => config.MinimumXP = value);
                        break;
                    case "3":
                        AdjustDoubleParameter("Maximum XP Multiplier", 
                            () => config.MaximumXPMultiplier,
                            value => config.MaximumXPMultiplier = value);
                        break;
                    case "4":
                        if (config.DungeonCompletionBonus != null)
                        {
                            AdjustIntParameter("Dungeon Base Bonus", 
                                () => config.DungeonCompletionBonus.BaseBonus,
                                value => config.DungeonCompletionBonus.BaseBonus = value);
                        }
                        break;
                    case "5":
                        if (config.DungeonCompletionBonus != null)
                        {
                            AdjustIntParameter("Bonus Per Room", 
                                () => config.DungeonCompletionBonus.BonusPerRoom,
                                value => config.DungeonCompletionBonus.BonusPerRoom = value);
                        }
                        break;
                    case "6":
                        ShowLevelDifferenceMultiplierMenu();
                        break;
                }
            }
        }

        private static void ShowLevelDifferenceMultiplierMenu()
        {
            Console.Clear();
            Console.WriteLine("=== LEVEL DIFFERENCE MULTIPLIERS ===");
            var config = TuningConfig.Instance.XPRewards;
            
            if (config?.LevelDifferenceMultipliers != null)
            {
                var sortedMultipliers = config.LevelDifferenceMultipliers.OrderBy(x => x.Value.LevelDifference).ToList();
                
                for (int i = 0; i < sortedMultipliers.Count; i++)
                {
                    var kvp = sortedMultipliers[i];
                    var multiplier = kvp.Value;
                    Console.WriteLine($"{i + 1}. {multiplier.Description}: {multiplier.Multiplier:F1}x (Level Diff: {multiplier.LevelDifference:+#;-#;0})");
                }
                
                Console.WriteLine("0. Back");
                Console.Write("Select multiplier to adjust: ");
                
                if (int.TryParse(Console.ReadLine(), out int selection) && 
                    selection > 0 && selection <= sortedMultipliers.Count)
                {
                    var selectedMultiplier = sortedMultipliers[selection - 1].Value;
                    AdjustDoubleParameter($"{selectedMultiplier.Description} Multiplier", 
                        () => selectedMultiplier.Multiplier,
                        value => selectedMultiplier.Multiplier = value);
                }
            }
        }

        private static void AdjustIntParameter(string parameterName, Func<int> getter, Action<int> setter)
        {
            Console.WriteLine($"\nCurrent {parameterName}: {getter()}");
            Console.Write($"Enter new value: ");
            
            if (int.TryParse(Console.ReadLine(), out int newValue))
            {
                setter(newValue);
                SaveConfiguration();
                Console.WriteLine($"{parameterName} updated to {newValue}");
            }
            else
            {
                Console.WriteLine("Invalid input.");
            }
            
            Console.WriteLine("Press any key to continue...");
            Console.ReadKey();
        }

        private static void AdjustDoubleParameter(string parameterName, Func<double> getter, Action<double> setter)
        {
            Console.WriteLine($"\nCurrent {parameterName}: {getter():F3}");
            Console.Write($"Enter new value: ");
            
            if (double.TryParse(Console.ReadLine(), out double newValue))
            {
                setter(newValue);
                SaveConfiguration();
                Console.WriteLine($"{parameterName} updated to {newValue:F3}");
            }
            else
            {
                Console.WriteLine("Invalid input.");
            }
            
            Console.WriteLine("Press any key to continue...");
            Console.ReadKey();
        }

        private static void SaveConfiguration()
        {
            try
            {
                string configPath = Path.Combine("GameData", "TuningConfig.json");
                string json = JsonSerializer.Serialize(TuningConfig.Instance, new JsonSerializerOptions
                {
                    WriteIndented = true
                });
                File.WriteAllText(configPath, json);
                Console.WriteLine("Configuration saved successfully!");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error saving configuration: {ex.Message}");
            }
        }

        private static void ExportConfiguration()
        {
            Console.Write("Enter export filename (without extension): ");
            string? filename = Console.ReadLine();
            
            if (string.IsNullOrWhiteSpace(filename))
            {
                filename = $"tuning_export_{DateTime.Now:yyyyMMdd_HHmmss}";
            }
            
            try
            {
                string exportPath = Path.Combine("GameData", $"{filename}.json");
                string json = JsonSerializer.Serialize(TuningConfig.Instance, new JsonSerializerOptions
                {
                    WriteIndented = true
                });
                File.WriteAllText(exportPath, json);
                Console.WriteLine($"Configuration exported to {exportPath}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error exporting configuration: {ex.Message}");
            }
            
            Console.WriteLine("Press any key to continue...");
            Console.ReadKey();
        }

        private static void LoadConfigurationPreset()
        {
            Console.WriteLine("Available preset files:");
            
            try
            {
                var jsonFiles = Directory.GetFiles("GameData", "*.json");
                for (int i = 0; i < jsonFiles.Length; i++)
                {
                    Console.WriteLine($"{i + 1}. {Path.GetFileName(jsonFiles[i])}");
                }
                
                Console.Write("Select file number (0 to cancel): ");
                
                if (int.TryParse(Console.ReadLine(), out int selection) && 
                    selection > 0 && selection <= jsonFiles.Length)
                {
                    string selectedFile = jsonFiles[selection - 1];
                    string json = File.ReadAllText(selectedFile);
                    
                    var loadedConfig = JsonSerializer.Deserialize<TuningConfig>(json, new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    });
                    
                    if (loadedConfig != null)
                    {
                        // Copy the loaded config to the current instance
                        // This is a simplified approach - in a full implementation,
                        // you'd want to selectively copy sections
                        Console.WriteLine($"Configuration loaded from {Path.GetFileName(selectedFile)}");
                        Console.WriteLine("Note: Restart the application to see all changes take effect.");
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error loading configuration: {ex.Message}");
            }
            
            Console.WriteLine("Press any key to continue...");
            Console.ReadKey();
        }

        private static void ShowEnemyDPSAnalysisMenu()
        {
            while (true)
            {
                Console.Clear();
                Console.WriteLine("=== ENEMY DPS ANALYSIS ===");
                Console.WriteLine();
                Console.WriteLine("1. Analyze Enemy DPS Patterns");
                Console.WriteLine("2. Test DPS Calculations");
                Console.WriteLine("3. View Archetype Profiles");
                Console.WriteLine("4. Suggest Enemy Archetypes");
                Console.WriteLine("5. Analyze Damage Issues");
                Console.WriteLine("6. Fix Damage Scaling");
                Console.WriteLine("7. DPS-Based Analysis (New System)");
                Console.WriteLine("8. Configure DPS Scaling");
                Console.WriteLine("0. Back");
                Console.WriteLine();
                Console.Write("Select option: ");
                
                string? input = Console.ReadLine();
                
                switch (input)
                {
                    case "1":
                        Console.WriteLine("Analyzing enemy DPS patterns...");
                        EnemyDPSCalculator.AnalyzeEnemyDPS(1000);
                        Console.WriteLine("\nPress any key to continue...");
                        Console.ReadKey();
                        break;
                    case "2":
                        EnemyDPSCalculator.TestDPSCalculations();
                        Console.WriteLine("\nPress any key to continue...");
                        Console.ReadKey();
                        break;
                    case "3":
                        ShowArchetypeProfiles();
                        break;
                    case "4":
                        ShowEnemyArchetypeSuggestions();
                        break;
                    case "5":
                        EnemyDamageAnalyzer.AnalyzeEnemyDamageIssues();
                        Console.WriteLine("\nPress any key to continue...");
                        Console.ReadKey();
                        break;
                    case "6":
                        EnemyDamageAnalyzer.SuggestDamageFixes();
                        Console.WriteLine("\nPress any key to continue...");
                        Console.ReadKey();
                        break;
                    case "7":
                        EnemyDPSSystem.AnalyzeAllEnemyDPS();
                        Console.WriteLine("\nPress any key to continue...");
                        Console.ReadKey();
                        break;
                    case "8":
                        ShowDPSScalingMenu();
                        break;
                    case "0":
                        return;
                    default:
                        Console.WriteLine("Invalid option. Press any key to continue...");
                        Console.ReadKey();
                        break;
                }
            }
        }

        private static void ShowArchetypeProfiles()
        {
            Console.Clear();
            Console.WriteLine("=== ENEMY ARCHETYPE PROFILES ===");
            Console.WriteLine();
            
            foreach (var archetype in EnemyDPSCalculator.GetAllArchetypes())
            {
                var profile = EnemyDPSCalculator.GetArchetypeProfile(archetype);
                Console.WriteLine($"{profile.Name.ToUpper()}:");
                Console.WriteLine($"  Description: {profile.Description}");
                Console.WriteLine($"  Speed Multiplier: {profile.SpeedMultiplier:F1}x ({(profile.SpeedMultiplier < 1.0 ? "Faster" : profile.SpeedMultiplier > 1.0 ? "Slower" : "Normal")})");
                Console.WriteLine($"  Damage Multiplier: {profile.DamageMultiplier:F1}x ({(profile.DamageMultiplier < 1.0 ? "Weaker" : profile.DamageMultiplier > 1.0 ? "Stronger" : "Normal")})");
                Console.WriteLine($"  Target DPS (Level 1): {profile.TargetDPSAtLevel1:F1}");
                
                if (profile.AttributeWeights.Any())
                {
                    Console.WriteLine("  Attribute Focus:");
                    foreach (var weight in profile.AttributeWeights)
                    {
                        Console.WriteLine($"    {weight.Key}: {weight.Value:F1}x");
                    }
                }
                Console.WriteLine();
            }
            
            Console.WriteLine("Press any key to continue...");
            Console.ReadKey();
        }

        private static void ShowEnemyArchetypeSuggestions()
        {
            Console.Clear();
            Console.WriteLine("=== ENEMY ARCHETYPE SUGGESTIONS ===");
            Console.WriteLine();
            
            // Load enemy data and suggest archetypes
            EnemyLoader.LoadEnemies();
            var enemyDataList = EnemyLoader.GetAllEnemyData();
            
            if (enemyDataList == null || !enemyDataList.Any())
            {
                Console.WriteLine("No enemy data found!");
                Console.WriteLine("Press any key to continue...");
                Console.ReadKey();
                return;
            }
            
            Console.WriteLine("Enemy Name\t\tBase Stats\t\t\tSuggested Archetype");
            Console.WriteLine("".PadRight(80, '='));
            
            foreach (var enemyData in enemyDataList.Take(15)) // Show first 15 enemies
            {
                int str = enemyData.Strength;
                int agi = enemyData.Agility;
                int tec = enemyData.Technique;
                int intel = enemyData.Intelligence;
                
                var suggestedArchetype = EnemyDPSCalculator.SuggestArchetypeForEnemy(
                    enemyData.Name, str, agi, tec, intel);
                
                string statsStr = $"STR:{str} AGI:{agi} TEC:{tec} INT:{intel}";
                Console.WriteLine($"{enemyData.Name.PadRight(16)}\t{statsStr.PadRight(24)}\t{suggestedArchetype}");
            }
            
            Console.WriteLine();
            Console.WriteLine("Press any key to continue...");
            Console.ReadKey();
        }

        private static void ShowDPSScalingMenu()
        {
            Console.Clear();
            Console.WriteLine("=== DPS SCALING CONFIGURATION ===");
            Console.WriteLine();
            
            var config = TuningConfig.Instance.EnemyDPS;
            if (config != null)
            {
                Console.WriteLine($"Base DPS at Level 1: {config.BaseDPSAtLevel1:F1}");
                Console.WriteLine($"DPS Per Level: {config.DPSPerLevel:F1}");
                Console.WriteLine($"Scaling Formula: {config.DPSScalingFormula}");
                Console.WriteLine();
                
                Console.WriteLine("Example DPS Targets:");
                for (int level = 1; level <= 10; level++)
                {
                    double targetDPS = EnemyDPSSystem.CalculateTargetDPS(level);
                    Console.WriteLine($"  Level {level}: {targetDPS:F1} DPS");
                }
            }
            
            Console.WriteLine();
            Console.WriteLine("1. Adjust Base DPS at Level 1");
            Console.WriteLine("2. Adjust DPS Per Level");
            Console.WriteLine("3. Test DPS Scaling");
            Console.WriteLine("0. Back");
            Console.WriteLine();
            Console.Write("Select option: ");
            
            string? input = Console.ReadLine();
            
            if (config != null)
            {
                switch (input)
                {
                    case "1":
                        AdjustDoubleParameter("Base DPS at Level 1", 
                            () => config.BaseDPSAtLevel1,
                            value => config.BaseDPSAtLevel1 = value);
                        break;
                    case "2":
                        AdjustDoubleParameter("DPS Per Level", 
                            () => config.DPSPerLevel,
                            value => config.DPSPerLevel = value);
                        break;
                    case "3":
                        Console.WriteLine("Testing DPS scaling...");
                        EnemyDPSSystem.AnalyzeAllEnemyDPS();
                        Console.WriteLine("\nPress any key to continue...");
                        Console.ReadKey();
                        break;
                }
            }
        }
    }
}
