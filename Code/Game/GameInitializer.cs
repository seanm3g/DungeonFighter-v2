using System;
using System.Collections.Generic;
using System.Linq;
using RPGGame.Data;
using RPGGame.Utils;

namespace RPGGame
{
    public class StartingGearData
    {
        /// <summary>Unused; starter weapons come from <c>Weapons.json</c> rows tagged <c>starter</c> when present (<see cref="StarterCatalogItems.ResolveStarterWeaponMenuCatalogRows"/>).</summary>
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
        /// Weapon type for the new-game weapon menu index (1-based), from <see cref="StarterCatalogItems.ResolveStarterWeaponMenuCatalogRows"/> row order.
        /// </summary>
        public static WeaponType GetWeaponTypeForStarterMenuIndex(int weaponChoice1Based)
        {
            var rows = StarterCatalogItems.ResolveStarterWeaponMenuCatalogRows();
            int idx = weaponChoice1Based - 1;
            if (idx < 0 || idx >= rows.Count)
                return WeaponType.Sword;
            if (!Enum.TryParse(rows[idx].Type?.Trim(), ignoreCase: true, out WeaponType weaponType))
                return WeaponType.Sword;
            return weaponType;
        }

        /// <summary>
        /// One menu row per <see cref="StarterCatalogItems.ResolveStarterWeaponMenuCatalogRows"/> entry (all <c>starter</c>-tagged weapons in file order,
        /// or legacy fallback: one tier-1 row per class path), with the same display damage scaling as <see cref="ApplyStartingWeaponTuning"/>.
        /// </summary>
        public static List<StartingWeapon> BuildStarterWeaponsForMenu()
        {
            var list = new List<StartingWeapon>();
            var scaling = GameConfiguration.Instance?.WeaponScaling;
            foreach (var row in StarterCatalogItems.ResolveStarterWeaponMenuCatalogRows())
            {
                if (!Enum.TryParse(row.Type?.Trim(), ignoreCase: true, out WeaponType weaponType))
                    weaponType = WeaponType.Sword;

                double dmg = row.BaseDamage;
                if (scaling != null && dmg > 0 && scaling.GlobalDamageMultiplier > 0)
                    dmg *= scaling.GlobalDamageMultiplier;
                double displayDamage = Math.Max(1, Math.Round(dmg));

                list.Add(new StartingWeapon
                {
                    name = row.Name ?? "",
                    damage = displayDamage,
                    attackSpeed = row.AttackSpeed,
                    weight = 0
                });
            }

            return list;
        }

        /// <summary>
        /// Builds the same <see cref="WeaponItem"/> as <see cref="InitializeNewGame"/> for a 1-based weapon menu index
        /// over <see cref="StarterCatalogItems.ResolveStarterWeaponMenuCatalogRows"/>. Does not attach starting actions.
        /// </summary>
        public static WeaponItem CreateStarterWeaponForMenuIndex(int weaponChoice1Based)
        {
            var rows = StarterCatalogItems.ResolveStarterWeaponMenuCatalogRows();
            if (weaponChoice1Based < 1 || weaponChoice1Based > rows.Count)
                throw new ArgumentOutOfRangeException(nameof(weaponChoice1Based));

            var catalogRow = rows[weaponChoice1Based - 1];
            if (!Enum.TryParse(catalogRow.Type?.Trim(), ignoreCase: true, out WeaponType weaponType))
                weaponType = WeaponType.Sword;

            var starterWeapon = ItemGenerator.GenerateWeaponItem(catalogRow);
            ApplyStartingWeaponTuning(starterWeapon, weaponType, slotFallback: null, baseDamageFromWeaponsCatalog: true);
            return starterWeapon;
        }

        /// <summary>
        /// Loads starting gear data from StartingGear.json (primary) or TuningConfig.json (fallback).
        /// Delegates to StartingGearLoader.
        /// </summary>
        public StartingGearData LoadStartingGear()
        {
            return StartingGearLoader.Load();
        }

        /// <summary>
        /// Initializes a new game with character creation and starting gear
        /// </summary>
        public void InitializeNewGame(Character player, List<Dungeon> availableDungeons, int weaponChoice = 0)
        {
            // Load starting gear from JSON
            var startingGear = LoadStartingGear();
            
            // If weaponChoice is 0, this is being called from old console UI - prompt for weapon
            var menuWeapons = BuildStarterWeaponsForMenu();
            int menuCount = menuWeapons.Count;
            if (menuCount == 0)
                throw new InvalidOperationException("No starter weapons: tag at least one row in Weapons.json with \"starter\", or ensure tier-1 weapons exist for every class weapon path.");

            if (weaponChoice == 0)
            {
                // Prompt player to choose a starter weapon (console mode)
                UIManager.WriteMenuLine("");
                UIManager.WriteTitleLine("Choose your starter weapon:");
                UIManager.WriteMenuLine(new string('-', 27)); // Creates 20 dashes
                for (int i = 0; i < menuWeapons.Count; i++)
                {
                    var weapon = menuWeapons[i];
                    UIManager.WriteMenuLine($"{i + 1}. {weapon.name}");
                }
                UIManager.WriteMenuLine("");
                
                weaponChoice = 1;
                while (true)
                {
                    UIManager.Write("Choose an option: ");
                    if (int.TryParse(Console.ReadLine(), out weaponChoice) && weaponChoice >= 1 && weaponChoice <= menuCount)
                        break;
                    UIManager.WriteMenuLine("Invalid choice.");
                }
            }
            
            // Validate weapon choice
            if (weaponChoice < 1 || weaponChoice > menuCount)
            {
                weaponChoice = 1; // Default to first weapon
            }
            
            WeaponItem starterWeapon = CreateStarterWeaponForMenuIndex(weaponChoice);
            WeaponType weaponType = starterWeapon.WeaponType;
            
            // Get starting weapon actions from Actions.json using "startingWeapon" tag
            var actionSelector = new LootActionSelector(new Random());
            var startingActions = actionSelector.GetStartingWeaponActions(weaponType.ToString());
            
            if (startingActions.Count > 0)
            {
                // If multiple starting actions (e.g., mace has 3), add all as ActionBonuses
                // If single starting action, set as GearAction
                if (startingActions.Count > 1)
                {
                    // Multiple actions: add all as ActionBonuses (like mace with SLAM, POUND, BLUDGEON)
                    foreach (var actionName in startingActions)
                    {
                        starterWeapon.ActionBonuses.Add(new ActionBonus { Name = actionName });
                    }
                }
                else
                {
                    // Single action: set as GearAction
                    starterWeapon.GearAction = startingActions[0];
                }
            }
            else
            {
                // Fallback: if no starting weapon actions found, use SelectWeaponActionForStarter
                var fallbackAction = actionSelector.SelectWeaponActionForStarter(weaponType.ToString());
                if (!string.IsNullOrEmpty(fallbackAction))
                {
                    starterWeapon.GearAction = fallbackAction;
                }
            }
            
            player.EquipItem(starterWeapon, "weapon");
            
            // Add default actions (actions marked with IsDefaultAction = true)
            player.Actions.AddDefaultActions(player);
            
            // Initialize combo sequence with weapon actions now that weapon is equipped
            player.InitializeDefaultCombo();
            
            // Starting armor: Armor.json rows tagged "starter" (first per slot); else StartingGear.json / tuning fallback
            var catalogStarterArmor = StarterCatalogItems.LoadStarterArmorItems();
            if (catalogStarterArmor.Count > 0)
            {
                foreach (var armorItem in catalogStarterArmor)
                {
                    string slot = StarterCatalogItems.GetEquipmentSlotKey(armorItem);
                    player.EquipItem(armorItem, slot);
                }
            }
            else
            {
                foreach (var armorData in startingGear.armor)
                {
                    Item armorItem = armorData.slot.ToLower() switch
                    {
                        "head" => new HeadItem(armorData.name, 1, armorData.armor),
                        "chest" => new ChestItem(armorData.name, 1, armorData.armor),
                        "feet" => new FeetItem(armorData.name, 1, armorData.armor),
                        _ => new HeadItem(armorData.name, 1, armorData.armor)
                    };

                    string slot = armorData.slot.ToLower() switch
                    {
                        "head" => "head",
                        "chest" => "body",
                        "feet" => "feet",
                        _ => "head"
                    };

                    player.EquipItem(armorItem, slot);
                }
            }

            // Safety check: Ensure character has at least one action available
            // If ActionPool is empty, add fallback actions based on weapon type
            if (player.ActionPool.Count == 0)
            {
                DebugLogger.LogFormat("GameInitializer", 
                    "WARNING: Character '{0}' has no actions after initialization. Adding fallback actions.", player.Name);
                
                // Try to add weapon-type actions as fallback
                if (player.Equipment.Weapon is WeaponItem fallbackWeapon)
                {
                    var weaponTypeActions = GetWeaponTypeActionsForFallback(fallbackWeapon.WeaponType);
                    foreach (var actionName in weaponTypeActions)
                    {
                        var action = ActionLoader.GetAction(actionName);
                        if (action != null)
                        {
                            action.IsComboAction = true;
                            player.AddAction(action, 1.0);
                            DebugLogger.LogFormat("GameInitializer", 
                                "Added fallback action '{0}' to character '{1}'", actionName, player.Name);
                            
                            // Only add one fallback action to ensure we have at least one
                            if (player.ActionPool.Count > 0)
                                break;
                        }
                    }
                }
                
                // If still no actions, add a generic fallback action
                if (player.ActionPool.Count == 0)
                {
                    // Try to find any available combo action
                    var allActions = ActionLoader.GetAllActions();
                    var fallbackAction = allActions.FirstOrDefault(a =>
                        a.IsComboAction &&
                        (a.Tags == null ||
                         (!a.Tags.Any(t => t.Equals("enemy", StringComparison.OrdinalIgnoreCase)) &&
                          !a.Tags.Any(t => t.Equals("environment", StringComparison.OrdinalIgnoreCase)))));
                    if (fallbackAction != null)
                    {
                        player.AddAction(fallbackAction, 1.0);
                        DebugLogger.LogFormat("GameInitializer", 
                            "Added generic fallback action '{0}' to character '{1}'", fallbackAction.Name, player.Name);
                    }
                }
                
                // Re-initialize combo sequence with the fallback actions
                if (player.ActionPool.Count > 0)
                {
                    player.InitializeDefaultCombo();
                }
            }

            // Generate dungeons for the new game
            GenerateDungeons(player, availableDungeons);
        }

        /// <summary>
        /// Gets weapon-type actions for fallback when no actions are available
        /// </summary>
        private List<string> GetWeaponTypeActionsForFallback(WeaponType weaponType)
        {
            var weaponTag = weaponType.ToString().ToLower();
            var allActions = ActionLoader.GetAllActions();

            // Get weapon-specific actions from JSON using tag matching
            var weaponActions = allActions
                .Where(action => action.Tags != null &&
                                action.Tags.Any(tag => tag.Equals("weapon", StringComparison.OrdinalIgnoreCase)) &&
                                action.Tags.Any(tag => tag.Equals(weaponTag, StringComparison.OrdinalIgnoreCase)) &&
                                !action.Tags.Any(tag => tag.Equals("unique", StringComparison.OrdinalIgnoreCase)) &&
                                !action.Tags.Any(tag => tag.Equals("class", StringComparison.OrdinalIgnoreCase)) &&
                                !action.Tags.Any(tag => tag.Equals("enemy", StringComparison.OrdinalIgnoreCase)) &&
                                !action.Tags.Any(tag => tag.Equals("environment", StringComparison.OrdinalIgnoreCase)))
                .Select(action => action.Name)
                .ToList();

            return weaponActions;
        }

        /// <summary>
        /// Applies <see cref="WeaponScalingConfig.StartingWeaponDamage"/> when set to a positive value; otherwise sets base damage from
        /// <c>Weapons.json</c> when <paramref name="baseDamageFromWeaponsCatalog"/> is true, or from the starting-gear slot’s
        /// legacy <c>damage</c> field when false. In both non-override cases, applies <see cref="WeaponScalingConfig.GlobalDamageMultiplier"/>.
        /// When <paramref name="baseDamageFromWeaponsCatalog"/> is true, weapon attack speed stays on the catalog value.
        /// Legacy slot path (<paramref name="baseDamageFromWeaponsCatalog"/> false) may override speed from <paramref name="slotFallback"/>.
        /// </summary>
        internal static void ApplyStartingWeaponTuning(WeaponItem weapon, WeaponType weaponType, StartingWeapon? slotFallback, bool baseDamageFromWeaponsCatalog)
        {
            var scaling = GameConfiguration.Instance?.WeaponScaling;
            int fromConfig = weaponType switch
            {
                WeaponType.Mace => scaling?.StartingWeaponDamage.Mace ?? 0,
                WeaponType.Sword => scaling?.StartingWeaponDamage.Sword ?? 0,
                WeaponType.Dagger => scaling?.StartingWeaponDamage.Dagger ?? 0,
                WeaponType.Wand => scaling?.StartingWeaponDamage.Wand ?? 0,
                _ => 0
            };

            if (fromConfig > 0)
                weapon.BaseDamage = fromConfig;
            else if (baseDamageFromWeaponsCatalog)
            {
                double dmg = weapon.BaseDamage;
                if (scaling != null && dmg > 0 && scaling.GlobalDamageMultiplier > 0)
                    dmg *= scaling.GlobalDamageMultiplier;
                weapon.BaseDamage = Math.Max(1, (int)Math.Round(dmg));
            }
            else
            {
                if (slotFallback == null)
                    throw new ArgumentNullException(nameof(slotFallback), "Legacy starter tuning requires a starting-gear slot.");
                double dmg = slotFallback.damage;
                if (scaling != null && dmg > 0 && scaling.GlobalDamageMultiplier > 0)
                    dmg *= scaling.GlobalDamageMultiplier;
                weapon.BaseDamage = Math.Max(1, (int)Math.Round(dmg));
            }

            if (!baseDamageFromWeaponsCatalog && slotFallback != null && slotFallback.attackSpeed > 0)
                weapon.BaseAttackSpeed = slotFallback.attackSpeed;
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
        /// Generates available dungeons based on player level using DungeonManagerWithRegistry
        /// </summary>
        private void GenerateDungeons(Character player, List<Dungeon> availableDungeons)
        {
            // Use DungeonManagerWithRegistry to generate dungeons from Dungeons.json
            var dungeonManager = new DungeonManagerWithRegistry();
            dungeonManager.RegenerateDungeons(player, availableDungeons);
        }
    }
}
