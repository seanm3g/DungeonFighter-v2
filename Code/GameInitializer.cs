using System.Text.Json;

namespace RPGGame
{
    public class StartingGearData
    {
        public List<StartingWeapon> weapons { get; set; } = new();
        public List<StartingArmor> armor { get; set; } = new();
    }

    public class StartingWeapon
    {
        public string name { get; set; } = "";
        public double damage { get; set; }
        public double weight { get; set; } = 0.0;
        public double attackSpeed { get; set; } = 0.05;
    }

    public class StartingArmor
    {
        public string slot { get; set; } = "";
        public string name { get; set; } = "";
        public int armor { get; set; }
        public double weight { get; set; } = 0.0;
    }

    /// <summary>
    /// Handles game initialization logic including starting gear, character setup, and dungeon generation
    /// </summary>
    public class GameInitializer
    {
        private Random random;

        public GameInitializer()
        {
            random = new Random();
        }

        /// <summary>
        /// Loads starting gear data from JSON file
        /// </summary>
        public StartingGearData LoadStartingGear()
        {
            try
            {
                string jsonPath = Path.Combine("..", "GameData", "StartingGear.json");
                string jsonContent = File.ReadAllText(jsonPath);
                var startingGear = JsonSerializer.Deserialize<StartingGearData>(jsonContent) ?? new StartingGearData();
                
                // Apply weapon scaling from tuning config
                var weaponScaling = GameConfiguration.Instance.WeaponScaling;
                if (weaponScaling != null)
                {
                    foreach (var weapon in startingGear.weapons)
                    {
                        // Apply global damage multiplier
                        weapon.damage = weapon.damage * weaponScaling.GlobalDamageMultiplier;
                        
                        // Apply weapon-specific starting damage from tuning config
                        string weaponName = weapon.name.ToLower();
                        weapon.damage = weaponName switch
                        {
                            var name when name.Contains("mace") => weaponScaling.StartingWeaponDamage.Mace,
                            var name when name.Contains("sword") => weaponScaling.StartingWeaponDamage.Sword,
                            var name when name.Contains("dagger") => weaponScaling.StartingWeaponDamage.Dagger,
                            var name when name.Contains("wand") => weaponScaling.StartingWeaponDamage.Wand,
                            _ => weapon.damage
                        };
                    }
                }
                
                return startingGear;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error loading starting gear: {ex.Message}");
                return new StartingGearData();
            }
        }

        /// <summary>
        /// Initializes a new game with character creation and starting gear
        /// </summary>
        public void InitializeNewGame(Character player, List<Dungeon> availableDungeons)
        {
            // Load starting gear from JSON
            var startingGear = LoadStartingGear();
            
            // Prompt player to choose a starter weapon
            UIManager.WriteMenuLine("");
            UIManager.WriteTitleLine("Choose your starter weapon:");
            UIManager.WriteMenuLine(new string('-', 25)); // Creates 20 dashes
            for (int i = 0; i < startingGear.weapons.Count; i++)
            {
                var weapon = startingGear.weapons[i];
                UIManager.WriteMenuLine($"{i + 1}. {weapon.name}");
            }
            UIManager.WriteMenuLine("");
            
            int weaponChoice = 1;
            while (true)
            {
                UIManager.Write("Choose an option: ");
                if (int.TryParse(Console.ReadLine(), out weaponChoice) && weaponChoice >= 1 && weaponChoice <= startingGear.weapons.Count)
                    break;
                UIManager.WriteMenuLine("Invalid choice.");
            }
            
            // Create weapon from JSON data
            var selectedWeaponData = startingGear.weapons[weaponChoice - 1];
            string weaponName = selectedWeaponData.name.ToLower();
            WeaponType weaponType = weaponName switch
            {
                var name when name.Contains("sword") => WeaponType.Sword,
                var name when name.Contains("mace") => WeaponType.Mace,
                var name when name.Contains("dagger") => WeaponType.Dagger,
                var name when name.Contains("wand") => WeaponType.Wand,
                _ => WeaponType.Sword
            };
            
            WeaponItem starterWeapon = new WeaponItem(selectedWeaponData.name, 1, (int)selectedWeaponData.damage, selectedWeaponData.attackSpeed, weaponType);
            player.EquipItem(starterWeapon, "weapon");
            
            // Initialize combo sequence with weapon actions now that weapon is equipped
            player.InitializeDefaultCombo();
            
            // Equip starting armor from JSON
            foreach (var armorData in startingGear.armor)
            {
                Item armorItem = armorData.slot.ToLower() switch
                {
                    "head" => new HeadItem(armorData.name, 1, armorData.armor),
                    "chest" => new ChestItem(armorData.name, 1, armorData.armor),
                    "feet" => new FeetItem(armorData.name, 1, armorData.armor),
                    _ => new HeadItem(armorData.name, 1, armorData.armor)
                };
                
                // Starting gear should NEVER have actions - leave GearAction as null
                
                string slot = armorData.slot.ToLower() switch
                {
                    "head" => "head",
                    "chest" => "body", 
                    "feet" => "feet",
                    _ => "head"
                };
                
                player.EquipItem(armorItem, slot);
            }

            // Generate dungeons for the new game
            GenerateDungeons(player, availableDungeons);
        }

        /// <summary>
        /// Initializes game for an existing character (load game scenario)
        /// </summary>
        public void InitializeExistingGame(Character player, List<Dungeon> availableDungeons)
        {
            // For existing characters, just set up dungeons without weapon selection
            GenerateDungeons(player, availableDungeons);
        }

        /// <summary>
        /// Generates available dungeons based on player level
        /// </summary>
        private void GenerateDungeons(Character player, List<Dungeon> availableDungeons)
        {
            availableDungeons.Clear();
            int playerLevel = player.Level;
            int[] dungeonLevels = new int[] { Math.Max(1, playerLevel - 1), playerLevel, playerLevel + 1 };
            
            // Load dungeon themes from config
            var dungeonConfig = Game.LoadDungeonConfig();
            var themes = dungeonConfig.dungeonThemes.ToArray();
            
            // Shuffle themes to ensure no repeats
            var shuffledThemes = themes.OrderBy(x => random.Next()).ToArray();
            
            // Create unique dungeon combinations with proper theme selection
            var usedThemes = new HashSet<string>();
            int dungeonCount = 0;
            int themeIndex = 0;
            
            // Sort dungeon levels to ensure proper ordering
            Array.Sort(dungeonLevels);
            
            while (dungeonCount < 3 && themeIndex < shuffledThemes.Length)
            {
                string currentTheme = shuffledThemes[themeIndex];
                if (!usedThemes.Contains(currentTheme))
                {
                    usedThemes.Add(currentTheme);
                    int level = dungeonLevels[dungeonCount % dungeonLevels.Length];
                    string themedName = $"{currentTheme} Dungeon (Level {level})";
                    availableDungeons.Add(new Dungeon(themedName, level, level, currentTheme));
                    dungeonCount++;
                }
                themeIndex++;
            }
            
            // Sort dungeons by level (lowest to highest)
            availableDungeons.Sort((d1, d2) => d1.MinLevel.CompareTo(d2.MinLevel));
        }
    }
}
