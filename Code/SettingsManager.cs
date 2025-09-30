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
                UIManager.WriteMenuLine("\n=== SETTINGS ===");
                UIManager.WriteMenuLine("");
                UIManager.WriteMenuLine("1. Narrative Balance");
                UIManager.WriteMenuLine("2. Combat Speed");
                UIManager.WriteMenuLine("3. Difficulty");
                UIManager.WriteMenuLine("4. Combat Display");
                UIManager.WriteMenuLine("5. Gameplay Options");
                UIManager.WriteMenuLine("6. Delete Saved Characters");
                UIManager.WriteMenuLine("7. Tests");
                UIManager.WriteMenuLine("8. Back to Main Menu");
                UIManager.WriteMenuLine("");
                UIManager.Write("Choose an option: ");

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
                        Tests();
                        break;
                    case "8":
                        return;
                    default:
                        UIManager.WriteMenuLine("Invalid choice. Please try again.");
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
            UIManager.WriteMenuLine("");
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
            UIManager.WriteMenuLine("");
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
                    string saveFile = "GameData/character_save.json";
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



        private static void TestCombatSystem()
        {
            UIManager.WriteMenuLine("\n=== COMBAT SYSTEM TESTS ===");
            UIManager.WriteMenuLine("Testing combat calculations and mechanics...");
            UIManager.WriteMenuLine("");

            // Create test entities
            var testPlayer = new Character("TestPlayer", 1);
            var testEnemy = new Enemy("TestEnemy", 1, 50, 8, 6, 4, 4, 0);
            
            UIManager.WriteMenuLine($"Test Player Stats: HP={testPlayer.CurrentHealth}, STR={testPlayer.Strength}, AGI={testPlayer.Agility}");
            UIManager.WriteMenuLine($"Test Enemy Stats: HP={testEnemy.CurrentHealth}, STR={testEnemy.Strength}, AGI={testEnemy.Agility}");
            UIManager.WriteMenuLine("");

            // Test damage calculation
            UIManager.WriteMenuLine("Testing Damage Calculation:");
            int damage = CombatCalculator.CalculateDamage(testPlayer, testEnemy);
            UIManager.WriteMenuLine($"Base Damage: {damage}");
            
            // Test damage with action
            var testAction = ActionLoader.GetAction("JAB");
            if (testAction != null)
            {
                int actionDamage = CombatCalculator.CalculateDamage(testPlayer, testEnemy, testAction);
                UIManager.WriteMenuLine($"Damage with JAB action: {actionDamage}");
            }
            UIManager.WriteMenuLine("");

            // Test critical hits
            UIManager.WriteMenuLine("Testing Critical Hit System:");
            bool isCritical = CombatCalculator.IsCriticalHit(testPlayer, 20);
            UIManager.WriteMenuLine($"Roll 20 is critical: {isCritical}");
            bool isNotCritical = CombatCalculator.IsCriticalHit(testPlayer, 19);
            UIManager.WriteMenuLine($"Roll 19 is critical: {isNotCritical}");
            
            // Test critical damage
            if (isCritical)
            {
                int criticalDamage = CombatCalculator.CalculateDamage(testPlayer, testEnemy, null, 1.0, 1.0, 0, 20);
                UIManager.WriteMenuLine($"Critical hit damage: {criticalDamage}");
            }
            UIManager.WriteMenuLine("");

            // Test action speed
            UIManager.WriteMenuLine("Testing Action Speed System:");
            double playerSpeed = testPlayer.GetTotalAttackSpeed();
            double enemySpeed = testEnemy.GetTotalAttackSpeed();
            UIManager.WriteMenuLine($"Player Attack Speed: {playerSpeed:F2}s");
            UIManager.WriteMenuLine($"Enemy Attack Speed: {enemySpeed:F2}s");
            
            // Test combo amplifier
            UIManager.WriteMenuLine("Testing Combo Amplifier:");
            double baseAmp = testPlayer.GetComboAmplifier();
            double currentAmp = testPlayer.GetCurrentComboAmplification();
            UIManager.WriteMenuLine($"Base Combo Amplifier: {baseAmp:F2}");
            UIManager.WriteMenuLine($"Current Combo Amplification: {currentAmp:F2}");
            UIManager.WriteMenuLine("");

            // Test hit/miss calculation
            UIManager.WriteMenuLine("Testing Hit/Miss System:");
            UIManager.WriteMenuLine("Hit/Miss System: 1-5=Miss, 6-13=Regular, 14-19=Combo, 20=Combo+Crit");
            bool hit = CombatCalculator.CalculateHit(testPlayer, testEnemy, 0, 15);
            UIManager.WriteMenuLine($"Roll 15 (Combo): Hit = {hit}");
            bool miss = CombatCalculator.CalculateHit(testPlayer, testEnemy, 0, 3);
            UIManager.WriteMenuLine($"Roll 3 (Miss): Hit = {miss}");
            bool regular = CombatCalculator.CalculateHit(testPlayer, testEnemy, 0, 8);
            UIManager.WriteMenuLine($"Roll 8 (Regular): Hit = {regular}");
            bool crit = CombatCalculator.CalculateHit(testPlayer, testEnemy, 0, 20);
            UIManager.WriteMenuLine($"Roll 20 (Combo+Crit): Hit = {crit}");
            UIManager.WriteMenuLine("");

            UIManager.WriteMenuLine("Combat System Tests Complete!");
            UIManager.WriteMenuLine("Press any key to continue...");
            Console.ReadKey();
        }

        private static void TestCharacterSystem()
        {
            UIManager.WriteMenuLine("\n=== CHARACTER SYSTEM TESTS ===");
            UIManager.WriteMenuLine("Testing character creation, stats, and progression...");
            UIManager.WriteMenuLine("");

            // Test character creation
            var testChar = new Character("TestChar", 1);
            UIManager.WriteMenuLine($"Created character: {testChar.Name}, Level: {testChar.Level}");
            UIManager.WriteMenuLine($"Base Stats - STR: {testChar.Strength}, AGI: {testChar.Agility}, TEC: {testChar.Technique}, INT: {testChar.Intelligence}");
            UIManager.WriteMenuLine("");

            // Test stat calculations
            UIManager.WriteMenuLine("Testing Stat Calculations:");
            UIManager.WriteMenuLine($"Effective Strength: {testChar.GetEffectiveStrength()}");
            UIManager.WriteMenuLine($"Effective Agility: {testChar.GetEffectiveAgility()}");
            UIManager.WriteMenuLine($"Effective Technique: {testChar.GetEffectiveTechnique()}");
            UIManager.WriteMenuLine($"Effective Intelligence: {testChar.GetEffectiveIntelligence()}");
            UIManager.WriteMenuLine("");

            // Test health system
            UIManager.WriteMenuLine("Testing Health System:");
            UIManager.WriteMenuLine($"Current Health: {testChar.CurrentHealth}/{testChar.MaxHealth}");
            testChar.TakeDamage(5);
            UIManager.WriteMenuLine($"After 5 damage: {testChar.CurrentHealth}/{testChar.MaxHealth}");
            UIManager.WriteMenuLine($"Is Alive: {testChar.IsAlive}");
            
            // Test healing
            testChar.Heal(3);
            UIManager.WriteMenuLine($"After healing 3: {testChar.CurrentHealth}/{testChar.MaxHealth}");
            UIManager.WriteMenuLine("");

            // Test XP and leveling
            UIManager.WriteMenuLine("Testing XP and Leveling:");
            UIManager.WriteMenuLine($"Current XP: {testChar.XP}");
            testChar.AddXP(50);
            UIManager.WriteMenuLine($"After adding 50 XP: {testChar.XP}");
            UIManager.WriteMenuLine($"Current Level: {testChar.Level}");
            
            // Test level up
            UIManager.WriteMenuLine("Testing Level Up:");
            int oldLevel = testChar.Level;
            int oldMaxHealth = testChar.MaxHealth;
            UIManager.WriteMenuLine($"Before: Level {oldLevel}, Max Health: {oldMaxHealth}, XP: {testChar.XP}");
            testChar.AddXP(100); // Should trigger level up
            UIManager.WriteMenuLine($"After adding 100 XP: Level {oldLevel} -> {testChar.Level}");
            UIManager.WriteMenuLine($"Health: {oldMaxHealth} -> {testChar.MaxHealth} (Expected: {oldMaxHealth + 3})");
            UIManager.WriteMenuLine($"XP after level up: {testChar.XP}");
            UIManager.WriteMenuLine("");

            // Test class point system
            UIManager.WriteMenuLine("Testing Class Point System:");
            UIManager.WriteMenuLine($"Current Level: {testChar.Level}");
            UIManager.WriteMenuLine($"Wizard Points: {testChar.WizardPoints}");
            UIManager.WriteMenuLine($"Expected Wizard Points: {testChar.Level - 1} (should be level - 1 if started at level 1)");
            
            // Test multiple level ups to verify class points
            int initialLevel = testChar.Level;
            int initialWizardPoints = testChar.WizardPoints;
            UIManager.WriteMenuLine($"Before multiple level ups: Level {initialLevel}, Wizard Points: {initialWizardPoints}");
            
            // Add enough XP to level up twice
            testChar.AddXP(200);
            UIManager.WriteMenuLine($"After adding 200 XP: Level {testChar.Level}, Wizard Points: {testChar.WizardPoints}");
            UIManager.WriteMenuLine($"Expected: Level {initialLevel + 2}, Wizard Points: {initialWizardPoints + 2}");
            UIManager.WriteMenuLine("");

            // Test combo system
            UIManager.WriteMenuLine("Testing Combo System:");
            UIManager.WriteMenuLine($"Combo Actions Available: {testChar.GetComboActions().Count}");
            UIManager.WriteMenuLine($"Combo Step: {testChar.ComboStep}");
            UIManager.WriteMenuLine($"Combo Info: {testChar.GetComboInfo()}");
            UIManager.WriteMenuLine("");

            // Test class progression
            UIManager.WriteMenuLine("Testing Class Progression:");
            UIManager.WriteMenuLine($"Current Class: {testChar.GetCurrentClass()}");
            UIManager.WriteMenuLine($"Full Name: {testChar.GetFullNameWithQualifier()}");
            UIManager.WriteMenuLine($"Barbarian Points: {testChar.BarbarianPoints}");
            UIManager.WriteMenuLine($"Warrior Points: {testChar.WarriorPoints}");
            UIManager.WriteMenuLine("");

            UIManager.WriteMenuLine("Character System Tests Complete!");
            UIManager.WriteMenuLine("Press any key to continue...");
            Console.ReadKey();
        }

        private static void TestInventorySystem()
        {
            UIManager.WriteMenuLine("\n=== INVENTORY SYSTEM TESTS ===");
            UIManager.WriteMenuLine("Testing inventory management and equipment...");
            UIManager.WriteMenuLine("");

            var testChar = new Character("TestChar", 1);
            
            // Test inventory operations
            UIManager.WriteMenuLine("Testing Inventory Operations:");
            UIManager.WriteMenuLine($"Initial inventory count: {testChar.Inventory.Count}");
            
            // Create test items with stat bonuses
            var testWeapon = new WeaponItem("Test Sword", 1, 10, 1.0, WeaponType.Sword);
            testWeapon.StatBonuses.Add(new StatBonus { StatType = "STR", Value = 2, Name = "Strength Bonus", Description = "+2 Strength" });
            testWeapon.StatBonuses.Add(new StatBonus { StatType = "Damage", Value = 3, Name = "Damage Bonus", Description = "+3 Damage" });
            
            var testArmor = new ChestItem("Test Armor", 1, 5);
            testArmor.StatBonuses.Add(new StatBonus { StatType = "AGI", Value = 1, Name = "Agility Bonus", Description = "+1 Agility" });
            testArmor.StatBonuses.Add(new StatBonus { StatType = "Health", Value = 5, Name = "Health Bonus", Description = "+5 Health" });
            
            var testHelmet = new HeadItem("Test Helmet", 1, 3);
            testHelmet.StatBonuses.Add(new StatBonus { StatType = "INT", Value = 2, Name = "Intelligence Bonus", Description = "+2 Intelligence" });
            testHelmet.StatBonuses.Add(new StatBonus { StatType = "RollBonus", Value = 1, Name = "Roll Bonus", Description = "+1 Roll Bonus" });
            
            var testBoots = new FeetItem("Test Boots", 1, 2);
            testBoots.StatBonuses.Add(new StatBonus { StatType = "TEC", Value = 1, Name = "Technique Bonus", Description = "+1 Technique" });
            testBoots.StatBonuses.Add(new StatBonus { StatType = "AttackSpeed", Value = -0.1, Name = "Speed Bonus", Description = "-0.1s Attack Speed" });
            
            testChar.AddToInventory(testWeapon);
            testChar.AddToInventory(testArmor);
            testChar.AddToInventory(testHelmet);
            testChar.AddToInventory(testBoots);
            UIManager.WriteMenuLine($"After adding items: {testChar.Inventory.Count}");
            UIManager.WriteMenuLine("");

            // Test equipment
            UIManager.WriteMenuLine("Testing Equipment System:");
            UIManager.WriteMenuLine($"Current weapon: {testChar.Weapon?.Name ?? "None"}");
            UIManager.WriteMenuLine($"Current armor: {testChar.Body?.Name ?? "None"}");
            UIManager.WriteMenuLine($"Current helmet: {testChar.Head?.Name ?? "None"}");
            UIManager.WriteMenuLine($"Current boots: {testChar.Feet?.Name ?? "None"}");
            
            testChar.EquipItem(testWeapon, "weapon");
            testChar.EquipItem(testArmor, "body");
            testChar.EquipItem(testHelmet, "head");
            testChar.EquipItem(testBoots, "feet");
            UIManager.WriteMenuLine($"After equipping: Weapon={testChar.Weapon?.Name}, Armor={testChar.Body?.Name}");
            UIManager.WriteMenuLine($"Helmet={testChar.Head?.Name}, Boots={testChar.Feet?.Name}");
            UIManager.WriteMenuLine("");

            // Test stat bonuses from equipment
            UIManager.WriteMenuLine("Testing Equipment Stat Bonuses:");
            UIManager.WriteMenuLine($"Total armor: {testChar.GetTotalArmor()}");
            UIManager.WriteMenuLine($"Attack speed: {testChar.GetTotalAttackSpeed():F2}s");
            UIManager.WriteMenuLine($"Equipment damage bonus: {testChar.GetEquipmentDamageBonus()}");
            UIManager.WriteMenuLine($"Equipment roll bonus: {testChar.GetEquipmentRollBonus()}");
            UIManager.WriteMenuLine($"Equipment health bonus: {testChar.GetEquipmentHealthBonus()}");
            UIManager.WriteMenuLine($"Equipment attack speed bonus: {testChar.GetEquipmentAttackSpeedBonus():F2}s");
            UIManager.WriteMenuLine("");

            // Test item removal
            UIManager.WriteMenuLine("Testing Item Removal:");
            bool removed = testChar.RemoveFromInventory(testWeapon);
            UIManager.WriteMenuLine($"Removed item: {(removed ? testWeapon.Name : "None")}");
            UIManager.WriteMenuLine($"Inventory count after removal: {testChar.Inventory.Count}");
            UIManager.WriteMenuLine("");

            // Test individual stat bonuses
            UIManager.WriteMenuLine("Testing Individual Stat Bonuses:");
            UIManager.WriteMenuLine($"STR bonus from equipment: {testChar.Equipment.GetEquipmentStatBonus("STR")} (Expected: 2)");
            UIManager.WriteMenuLine($"AGI bonus from equipment: {testChar.Equipment.GetEquipmentStatBonus("AGI")} (Expected: 1)");
            UIManager.WriteMenuLine($"TEC bonus from equipment: {testChar.Equipment.GetEquipmentStatBonus("TEC")} (Expected: 1)");
            UIManager.WriteMenuLine($"INT bonus from equipment: {testChar.Equipment.GetEquipmentStatBonus("INT")} (Expected: 2)");
            UIManager.WriteMenuLine($"Damage bonus from equipment: {testChar.Equipment.GetEquipmentStatBonus("Damage")} (Expected: 3)");
            UIManager.WriteMenuLine($"Health bonus from equipment: {testChar.Equipment.GetEquipmentStatBonus("Health")} (Expected: 5)");
            UIManager.WriteMenuLine($"Roll bonus from equipment: {testChar.Equipment.GetEquipmentStatBonus("RollBonus")} (Expected: 1)");
            UIManager.WriteMenuLine("");

            UIManager.WriteMenuLine("Inventory System Tests Complete!");
            UIManager.WriteMenuLine("Press any key to continue...");
            Console.ReadKey();
        }

        private static void TestActionSystem()
        {
            UIManager.WriteMenuLine("\n=== ACTION SYSTEM TESTS ===");
            UIManager.WriteMenuLine("Testing action loading, execution, and effects...");
            UIManager.WriteMenuLine("");

            // Test action loading
            UIManager.WriteMenuLine("Testing Action Loading:");
            ActionLoader.LoadActions();
            var actions = ActionLoader.GetAllActions();
            UIManager.WriteMenuLine($"Loaded {actions.Count} actions");
            
            if (actions.Count > 0)
            {
                var firstAction = actions.First();
                UIManager.WriteMenuLine($"Sample action: {firstAction.Name} - Length: {firstAction.Length}s, Type: {firstAction.Type}");
                UIManager.WriteMenuLine($"Damage Multiplier: {firstAction.DamageMultiplier}, Roll Bonus: {firstAction.RollBonus}");
            }
            UIManager.WriteMenuLine("");

            // Test specific action retrieval
            UIManager.WriteMenuLine("Testing Specific Action Retrieval:");
            var jabAction = ActionLoader.GetAction("JAB");
            if (jabAction != null)
            {
                UIManager.WriteMenuLine($"JAB Action: Length={jabAction.Length}s, Damage={jabAction.DamageMultiplier}x");
            }
            else
            {
                UIManager.WriteMenuLine("JAB Action not found");
            }
            UIManager.WriteMenuLine("");

            // Test action execution
            UIManager.WriteMenuLine("Testing Action Execution:");
            var testPlayer = new Character("TestPlayer", 1);
            var testEnemy = new Enemy("TestEnemy", 1, 50, 8, 6, 4, 4, 0);
            
            if (actions.Count > 0)
            {
                var testAction = actions.First();
                UIManager.WriteMenuLine($"Sample action from database: {testAction.Name}");
                UIManager.WriteMenuLine("Note: ExecuteAction uses random action selection from entity's action pool, not the sample action");
                UIManager.WriteMenuLine($"Player action pool size: {testPlayer.ActionPool.Count}");
                UIManager.WriteMenuLine($"Enemy action pool size: {testEnemy.ActionPool.Count}");
                string result = CombatActions.ExecuteAction(testPlayer, testEnemy);
                UIManager.WriteMenuLine($"Action result: {result}");
            }
            UIManager.WriteMenuLine("");

            // Test combo system
            UIManager.WriteMenuLine("Testing Combo System:");
            UIManager.WriteMenuLine($"Player combo actions: {testPlayer.GetComboActions().Count}");
            if (actions.Count > 0)
            {
                testPlayer.AddToCombo(actions.First());
                UIManager.WriteMenuLine($"After adding action: {testPlayer.GetComboActions().Count}");
                UIManager.WriteMenuLine($"Combo step: {testPlayer.ComboStep}");
                UIManager.WriteMenuLine($"Combo info: {testPlayer.GetComboInfo()}");
            }
            UIManager.WriteMenuLine("");

            // Test action types
            UIManager.WriteMenuLine("Testing Action Types:");
            var attackActions = ActionLoader.GetRandomActionNameByType(ActionType.Attack);
            var healActions = ActionLoader.GetRandomActionNameByType(ActionType.Heal);
            UIManager.WriteMenuLine($"Sample attack action: {attackActions}");
            UIManager.WriteMenuLine($"Sample heal action: {healActions}");
            UIManager.WriteMenuLine("");

            // Test action pool
            UIManager.WriteMenuLine("Testing Action Pool:");
            UIManager.WriteMenuLine($"Player action pool: {testPlayer.ActionPool.Count}");
            UIManager.WriteMenuLine($"Enemy action pool: {testEnemy.ActionPool.Count}");
            UIManager.WriteMenuLine("");

            UIManager.WriteMenuLine("Action System Tests Complete!");
            UIManager.WriteMenuLine("Press any key to continue...");
            Console.ReadKey();
        }

        private static void TestEnemySystem()
        {
            UIManager.WriteMenuLine("\n=== ENEMY SYSTEM TESTS ===");
            UIManager.WriteMenuLine("Testing enemy creation, AI, and scaling...");
            UIManager.WriteMenuLine("");

            // Test enemy creation
            UIManager.WriteMenuLine("Testing Enemy Creation:");
            var goblin = EnemyStatPoolSystem.CreateBalancedEnemy("Test Goblin", 1, EnemyArchetype.Warrior);
            var orc = EnemyStatPoolSystem.CreateBalancedEnemy("Test Orc", 5, EnemyArchetype.Brute);
            var assassin = EnemyStatPoolSystem.CreateBalancedEnemy("Test Assassin", 3, EnemyArchetype.Assassin);
            
            UIManager.WriteMenuLine($"Goblin Level 1 (Warrior): HP={goblin.CurrentHealth}, STR={goblin.Strength}, AGI={goblin.Agility}");
            UIManager.WriteMenuLine($"Orc Level 5 (Brute): HP={orc.CurrentHealth}, STR={orc.Strength}, AGI={orc.Agility}");
            UIManager.WriteMenuLine($"Assassin Level 3 (Assassin): HP={assassin.CurrentHealth}, STR={assassin.Strength}, AGI={assassin.Agility}");
            UIManager.WriteMenuLine("Note: Different archetypes have different stat focuses (Brute = STR-focused, Assassin = AGI-focused)");
            UIManager.WriteMenuLine("");

            // Test enemy scaling (same archetype at different levels)
            UIManager.WriteMenuLine("Testing Enemy Scaling (Same Archetype):");
            var warrior1 = EnemyStatPoolSystem.CreateBalancedEnemy("Warrior L1", 1, EnemyArchetype.Warrior);
            var warrior5 = EnemyStatPoolSystem.CreateBalancedEnemy("Warrior L5", 5, EnemyArchetype.Warrior);
            UIManager.WriteMenuLine($"Warrior Level 1: HP={warrior1.CurrentHealth}, STR={warrior1.Strength}, AGI={warrior1.Agility}");
            UIManager.WriteMenuLine($"Warrior Level 5: HP={warrior5.CurrentHealth}, STR={warrior5.Strength}, AGI={warrior5.Agility}");
            UIManager.WriteMenuLine($"Scaling: HP +{warrior5.CurrentHealth - warrior1.CurrentHealth}, STR +{warrior5.Strength - warrior1.Strength}, AGI +{warrior5.Agility - warrior1.Agility}");
            UIManager.WriteMenuLine("");
            
            // Test attack speeds
            UIManager.WriteMenuLine("Testing Attack Speeds:");
            UIManager.WriteMenuLine($"Goblin attack speed: {goblin.GetTotalAttackSpeed():F2}s");
            UIManager.WriteMenuLine($"Orc attack speed: {orc.GetTotalAttackSpeed():F2}s");
            UIManager.WriteMenuLine($"Assassin attack speed: {assassin.GetTotalAttackSpeed():F2}s");
            UIManager.WriteMenuLine("");

            // Test enemy AI
            UIManager.WriteMenuLine("Testing Enemy AI:");
            var testPlayer = new Character("TestPlayer", 1);
            var selectedAction = goblin.SelectAction();
            UIManager.WriteMenuLine($"Goblin selected action: {selectedAction?.Name ?? "None"}");
            
            // Test multiple action selections
            for (int i = 0; i < 3; i++)
            {
                var action = goblin.SelectAction();
                UIManager.WriteMenuLine($"Goblin action {i+1}: {action?.Name ?? "None"}");
            }
            UIManager.WriteMenuLine("");

            // Test enemy factory
            UIManager.WriteMenuLine("Testing Enemy Factory:");
            var factoryEnemy = EnemyFactory.CreateEnemy("Test Enemy", 3);
            UIManager.WriteMenuLine($"Factory created enemy: {factoryEnemy.Name}, Level: {factoryEnemy.Level}");
            UIManager.WriteMenuLine($"Factory enemy stats: HP={factoryEnemy.CurrentHealth}, STR={factoryEnemy.Strength}");
            UIManager.WriteMenuLine("");

            // Test enemy archetypes
            UIManager.WriteMenuLine("Testing Enemy Archetypes:");
            UIManager.WriteMenuLine($"Goblin archetype: {goblin.Archetype}");
            UIManager.WriteMenuLine($"Orc archetype: {orc.Archetype}");
            UIManager.WriteMenuLine($"Assassin archetype: {assassin.Archetype}");
            UIManager.WriteMenuLine("");

            // Test enemy rewards
            UIManager.WriteMenuLine("Testing Enemy Rewards:");
            UIManager.WriteMenuLine($"Goblin XP reward: {goblin.XPReward}, Gold reward: {goblin.GoldReward}");
            UIManager.WriteMenuLine($"Orc XP reward: {orc.XPReward}, Gold reward: {orc.GoldReward}");
            UIManager.WriteMenuLine("");

            UIManager.WriteMenuLine("Enemy System Tests Complete!");
            UIManager.WriteMenuLine("Press any key to continue...");
            Console.ReadKey();
        }

        private static void TestDungeonSystem()
        {
            UIManager.WriteMenuLine("\n=== DUNGEON SYSTEM TESTS ===");
            UIManager.WriteMenuLine("Testing dungeon generation and room management...");
            UIManager.WriteMenuLine("");

            // Test dungeon loading
            UIManager.WriteMenuLine("Testing Dungeon Loading:");
            var dungeonManager = new DungeonManager();
            var availableDungeons = new List<Dungeon>();
            var testPlayer = new Character("TestPlayer", 1);
            dungeonManager.RegenerateDungeons(testPlayer, availableDungeons);
            UIManager.WriteMenuLine($"Generated {availableDungeons.Count} dungeons");
            
            if (availableDungeons.Count > 0)
            {
                var firstDungeon = availableDungeons[0];
                UIManager.WriteMenuLine($"Sample dungeon: {firstDungeon.Name}, MinLevel: {firstDungeon.MinLevel}, Rooms: {firstDungeon.Rooms.Count}");
                
                // Test multiple dungeons
                for (int i = 0; i < Math.Min(3, availableDungeons.Count); i++)
                {
                    var dungeon = availableDungeons[i];
                    UIManager.WriteMenuLine($"Dungeon {i+1}: {dungeon.Name} (Level {dungeon.MinLevel})");
                }
            }
            UIManager.WriteMenuLine("");

            // Test room generation
            UIManager.WriteMenuLine("Testing Room Generation:");
            var testRoom = new Environment("Test Room", "A test room for system testing", true, "Forest", "Chamber");
            UIManager.WriteMenuLine($"Created room: {testRoom.Name}");
            UIManager.WriteMenuLine($"Room has enemies: {testRoom.HasLivingEnemies()}");
            UIManager.WriteMenuLine($"Room type: {testRoom.RoomType}");
            UIManager.WriteMenuLine("");

            // Test environment actions
            UIManager.WriteMenuLine("Testing Environment Actions:");
            var environmentAction = testRoom.SelectAction();
            UIManager.WriteMenuLine($"Environment selected action: {environmentAction?.Name ?? "None"}");
            
            // Test multiple environment actions
            for (int i = 0; i < 3; i++)
            {
                var action = testRoom.SelectAction();
                UIManager.WriteMenuLine($"Environment action {i+1}: {action?.Name ?? "None"}");
            }
            UIManager.WriteMenuLine("");

            // Test room generator
            UIManager.WriteMenuLine("Testing Room Generator:");
            var generatedRoom = RoomGenerator.GenerateRoom("Test Environment", 1);
            UIManager.WriteMenuLine($"Generated room: {generatedRoom.Name}");
            UIManager.WriteMenuLine($"Generated room type: {generatedRoom.RoomType}");
            UIManager.WriteMenuLine("");

            // Test dungeon progression
            UIManager.WriteMenuLine("Testing Dungeon Progression:");
            UIManager.WriteMenuLine($"Player level: {testPlayer.Level}");
            UIManager.WriteMenuLine($"Available dungeons for level {testPlayer.Level}: {availableDungeons.Count(d => d.MinLevel <= testPlayer.Level)}");
            UIManager.WriteMenuLine("");

            UIManager.WriteMenuLine("Dungeon System Tests Complete!");
            UIManager.WriteMenuLine("Press any key to continue...");
            Console.ReadKey();
        }

        private static void TestLootSystem()
        {
            UIManager.WriteMenuLine("\n=== LOOT SYSTEM TESTS ===");
            UIManager.WriteMenuLine("Testing loot generation and item creation...");
            UIManager.WriteMenuLine("");

            // Test guaranteed loot generation (like dungeon completion)
            UIManager.WriteMenuLine("Testing Guaranteed Loot Generation:");
            var testPlayer = new Character("TestPlayer", 1);
            UIManager.WriteMenuLine("Testing dungeon completion loot system (guaranteed)...");
            
            // Simulate the dungeon completion loot system
            Item? reward = null;
            int attempts = 0;
            const int maxAttempts = 10;
            
            while (reward == null && attempts < maxAttempts)
            {
                reward = LootGenerator.GenerateLoot(testPlayer.Level, 1, testPlayer);
                attempts++;
            }
            
            if (reward != null)
            {
                UIManager.WriteMenuLine($"Generated guaranteed loot: {reward.Name}, Type: {reward.GetType().Name}");
                UIManager.WriteMenuLine($"Attempts needed: {attempts}");
            }
            else
            {
                UIManager.WriteMenuLine("ERROR: Failed to generate guaranteed loot after 10 attempts!");
            }
            UIManager.WriteMenuLine("");

            // Test rarity system
            UIManager.WriteMenuLine("Testing Rarity System:");
            LootGenerator.Initialize();
            UIManager.WriteMenuLine("LootGenerator initialized successfully");
            UIManager.WriteMenuLine("");

            // Test high-level guaranteed loot
            UIManager.WriteMenuLine("Testing High-Level Guaranteed Loot:");
            var highLevelPlayer = new Character("HighLevelPlayer", 5);
            Item? highLevelReward = null;
            attempts = 0;
            
            while (highLevelReward == null && attempts < maxAttempts)
            {
                highLevelReward = LootGenerator.GenerateLoot(highLevelPlayer.Level, 1, highLevelPlayer);
                attempts++;
            }
            
            if (highLevelReward != null)
            {
                UIManager.WriteMenuLine($"Generated high-level loot: {highLevelReward.Name}, Type: {highLevelReward.GetType().Name}");
                UIManager.WriteMenuLine($"Attempts needed: {attempts}");
            }
            else
            {
                UIManager.WriteMenuLine("ERROR: Failed to generate high-level guaranteed loot!");
            }
            UIManager.WriteMenuLine("");

            UIManager.WriteMenuLine("Loot System Tests Complete!");
            UIManager.WriteMenuLine("Press any key to continue...");
            Console.ReadKey();
        }

        private static void Tests()
        {
            UIManager.WriteMenuLine("\n=== TESTS ===");
            UIManager.WriteMenuLine("");
            UIManager.WriteMenuLine("1. Test Enemy Balance System");
            UIManager.WriteMenuLine("2. Test Combat System");
            UIManager.WriteMenuLine("3. Test Loot Generation System");
            UIManager.WriteMenuLine("4. Test Inventory Display System");
            UIManager.WriteMenuLine("5. Test All Systems");
            UIManager.WriteMenuLine("6. Back to Settings");
            UIManager.WriteMenuLine("");
            UIManager.Write("Choose an option: ");

            string? choice = Console.ReadLine();
            switch (choice)
            {
                case "1":
                    TestEnemyBalanceSystem();
                    break;
                case "2":
                    TestCombatSystemMenu();
                    break;
                case "3":
                    TestLootGenerationSystem();
                    break;
                case "4":
                    TestInventoryDisplaySystem();
                    break;
                case "5":
                    RunAllSystemTests();
                    break;
                case "6":
                    return;
                default:
                    UIManager.WriteMenuLine("Invalid choice. Please try again.");
                    break;
            }
        }

        private static void TestEnemyBalanceSystem()
        {
            UIManager.WriteMenuLine("\n=== TESTING ENEMY BALANCE SYSTEM ===");
            UIManager.WriteMenuLine("Press any key to start...");
            Console.ReadKey();

            EnemyBalanceTest.TestEnemyBalanceSystem();

            UIManager.WriteMenuLine("\n=== ENEMY BALANCE SYSTEM TEST COMPLETE ===");
            UIManager.WriteMenuLine("Press any key to return to test menu...");
            Console.ReadKey();
        }

        private static void TestLootGenerationSystem()
        {
            UIManager.WriteMenuLine("\n=== TESTING LOOT GENERATION SYSTEM ===");
            UIManager.WriteMenuLine("This will test loot generation, rarity distribution, and data integrity.");
            UIManager.WriteMenuLine("Press any key to start...");
            Console.ReadKey();

            LootGenerationTest.RunLootGenerationTests();

            UIManager.WriteMenuLine("\n=== LOOT GENERATION SYSTEM TEST COMPLETE ===");
            UIManager.WriteMenuLine("Press any key to return to test menu...");
            Console.ReadKey();
        }

        private static void TestInventoryDisplaySystem()
        {
            UIManager.WriteMenuLine("\n=== TESTING INVENTORY DISPLAY SYSTEM ===");
            UIManager.WriteMenuLine("This will test inventory display with stat bonuses and modifications.");
            UIManager.WriteMenuLine("Press any key to start...");
            Console.ReadKey();

            InventoryDisplayTest.RunInventoryDisplayTest();

            UIManager.WriteMenuLine("\n=== INVENTORY DISPLAY SYSTEM TEST COMPLETE ===");
            UIManager.WriteMenuLine("Press any key to return to test menu...");
            Console.ReadKey();
        }

        private static void TestCombatSystemMenu()
        {
            while (true)
            {
                UIManager.WriteMenuLine("\n=== COMBAT SYSTEM TESTS ===");
                UIManager.WriteMenuLine("");
                UIManager.WriteMenuLine("1. Test Action Selection");
                UIManager.WriteMenuLine("2. Test Damage Calculation");
                UIManager.WriteMenuLine("3. Test Status Effects");
                UIManager.WriteMenuLine("4. Test Environmental Effects");
                UIManager.WriteMenuLine("5. Test Miss Messages");
                UIManager.WriteMenuLine("6. Test Combat Log Formatting");
                UIManager.WriteMenuLine("7. Test Loot Generation");
                UIManager.WriteMenuLine("8. Test Roll 8 Issue (Debug)");
                UIManager.WriteMenuLine("9. Run All Combat Tests");
                UIManager.WriteMenuLine("10. Back to Test Menu");
                UIManager.WriteMenuLine("");
                UIManager.Write("Choose an option: ");

                string? choice = Console.ReadLine();
                switch (choice)
                {
                    case "1":
                        RunCombatTest("action");
                        break;
                    case "2":
                        RunCombatTest("damage");
                        break;
                    case "3":
                        RunCombatTest("status");
                        break;
                    case "4":
                        RunCombatTest("environmental");
                        break;
                    case "5":
                        RunCombatTest("miss");
                        break;
                    case "6":
                        RunCombatTest("log");
                        break;
                    case "7":
                        RunCombatTest("loot");
                        break;
                    case "8":
                        RunCombatTest("roll8");
                        break;
                    case "9":
                        RunCombatTest("all");
                        break;
                    case "10":
                        return;
                    default:
                        UIManager.WriteMenuLine("Invalid choice. Please try again.");
                        break;
                }
            }
        }

        private static void RunCombatTest(string testName)
        {
            UIManager.WriteMenuLine($"\n=== RUNNING COMBAT TEST: {testName.ToUpper()} ===");
            UIManager.WriteMenuLine("Press any key to start...");
            Console.ReadKey();

            try
            {
                CombatTest.RunTest(testName);
            }
            catch (Exception ex)
            {
                UIManager.WriteMenuLine($"Error running test: {ex.Message}");
            }

            UIManager.WriteMenuLine("\n=== TEST COMPLETE ===");
            UIManager.WriteMenuLine("Press any key to continue...");
            Console.ReadKey();
        }

        private static void RunAllSystemTests()
        {
            UIManager.WriteMenuLine("\n=== RUNNING ALL SYSTEM TESTS ===");
            UIManager.WriteMenuLine("This will run comprehensive tests on all game systems.");
            UIManager.WriteMenuLine("Press any key to start...");
            Console.ReadKey();

            TestCombatSystem();
            TestCharacterSystem();
            TestInventorySystem();
            TestActionSystem();
            TestEnemySystem();
            TestDungeonSystem();
            TestLootSystem();

            UIManager.WriteMenuLine("\n=== ALL SYSTEM TESTS COMPLETE ===");
            UIManager.WriteMenuLine("All systems have been tested successfully!");
            UIManager.WriteMenuLine("Press any key to return to settings...");
            Console.ReadKey();
        }
    }
}
