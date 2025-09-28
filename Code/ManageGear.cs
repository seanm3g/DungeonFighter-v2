using System;

namespace RPGGame
{
    public class Inventory
    {
        private Character player;
        private List<Item> inventory;

        public Inventory(Character player, List<Item> inventory)
        {
            this.player = player;
            this.inventory = inventory;
        }

        public bool ShowGearMenu()
        {
            while (true)
            {
                Console.WriteLine("\n--- Inventory ---");
                ShowCharacterStats();
                ShowCurrentEquipment();
                ShowComboInfo();
                ShowInventory();
                ShowOptions();

                if (int.TryParse(Console.ReadLine(), out int choice))
                {
                    switch (choice)
                    {
                        case 1:
                            EquipItem();
                            break;
                        case 2:
                            UnequipItem();
                            break;
                        case 3:
                            DiscardItem();
                            break;
                        case 4:
                            ManageComboActions();
                            break;
                        case 5:
                            return true; // Continue to dungeon
                        case 6:
                            return false; // Exit inventory menu and return to main menu
                        default:
                            Console.WriteLine("Invalid option.");
                            break;
                    }
                }
                else
                {
                    Console.WriteLine("Invalid input.");
                }
            }
        }

        private void ShowCharacterStats()
        {
            int weaponDamage = (player.Weapon is WeaponItem w) ? w.GetTotalDamage() : 0;
            int equipmentDamageBonus = player.GetEquipmentDamageBonus();
            int modificationDamageBonus = player.GetModificationDamageBonus();
            int damage = player.GetEffectiveStrength() + weaponDamage + equipmentDamageBonus + modificationDamageBonus;
            double attackSpeed = player.GetTotalAttackSpeed();
            int armor = 0;
            if (player.Head is HeadItem head) armor += head.GetTotalArmor();
            if (player.Body is ChestItem body) armor += body.GetTotalArmor();
            if (player.Feet is FeetItem feet) armor += feet.GetTotalArmor();
            Console.WriteLine($"{player.Name} (Level {player.Level}) - {player.GetCurrentClass()}");
            Console.WriteLine($"Health: {player.CurrentHealth}/{player.GetEffectiveMaxHealth()}  STR: {player.GetEffectiveStrength()}  AGI: {player.GetEffectiveAgility()}  TEC: {player.GetEffectiveTechnique()}  INT: {player.GetEffectiveIntelligence()}");
            int totalRollBonus = player.GetIntelligenceRollBonus() + player.GetModificationRollBonus() + player.GetEquipmentRollBonus();
            double secondsPerAttack = attackSpeed;
            // Get current amplification
            double currentAmplification = player.GetCurrentComboAmplification();
            int magicFind = player.GetMagicFind();
            Console.WriteLine($"Damage: {damage} (STR:{player.GetEffectiveStrength()} + Weapon:{weaponDamage} + Equipment:{equipmentDamageBonus} + Mods:{modificationDamageBonus})  Attack Time: {attackSpeed:0.00}s  Amplification: {currentAmplification:F2}x  Roll Bonus: +{totalRollBonus}  Armor: {armor}");
            if (magicFind > 0)
            {
                Console.WriteLine($"Magic Find: +{magicFind} (improves rare item drop chances)");
            }
            // Show only classes with points > 0
            var classPointsInfo = new List<string>();
            if (player.BarbarianPoints > 0) classPointsInfo.Add($"Barbarian({player.BarbarianPoints})");
            if (player.WarriorPoints > 0) classPointsInfo.Add($"Warrior({player.WarriorPoints})");
            if (player.RoguePoints > 0) classPointsInfo.Add($"Rogue({player.RoguePoints})");
            if (player.WizardPoints > 0) classPointsInfo.Add($"Wizard({player.WizardPoints})");
            
            if (classPointsInfo.Count > 0)
            {
                Console.WriteLine($"Class Points: {string.Join(" ", classPointsInfo)}");
                Console.WriteLine($"Next Upgrades: {player.GetClassUpgradeInfo()}");
            }
        }

        private void ShowCurrentEquipment()
        {
            Console.WriteLine();
            Console.WriteLine("Currently Equipped:");
            
            // Show weapon with inline stats
            if (player.Weapon is WeaponItem weapon)
            {
                Console.WriteLine($"Weapon: {weapon.Name} - Damage: {weapon.GetTotalDamage()}, Attack Speed: {weapon.GetTotalAttackSpeed():F1}{GetItemActions(weapon)}");
                ShowItemBonuses(weapon);
            }
            else
            {
                Console.WriteLine($"Weapon: None{GetItemActions(player.Weapon)}");
            }
            
            // Show head with inline stats
            if (player.Head is HeadItem head)
            {
                Console.WriteLine($"Head: {head.Name} - Armor: {head.GetTotalArmor()}{GetItemActions(head)}");
                ShowItemBonuses(head);
            }
            else
            {
                Console.WriteLine($"Head: None{GetItemActions(player.Head)}");
            }
            
            // Show body with inline stats
            if (player.Body is ChestItem body)
            {
                Console.WriteLine($"Body: {body.Name} - Armor: {body.GetTotalArmor()}{GetItemActions(body)}");
                ShowItemBonuses(body);
            }
            else
            {
                Console.WriteLine($"Body: None{GetItemActions(player.Body)}");
            }
            
            // Show feet with inline stats
            if (player.Feet is FeetItem feet)
            {
                Console.WriteLine($"Feet: {feet.Name} - Armor: {feet.GetTotalArmor()}{GetItemActions(feet)}");
                ShowItemBonuses(feet);
            }
            else
            {
                Console.WriteLine($"Feet: None{GetItemActions(player.Feet)}");
            }
        }

        private void ShowInventory()
        {
            // Only show inventory header if there are items
            if (inventory.Count > 0)
            {
                Console.WriteLine("\nInventory:");
            }
            
            for (int i = 0; i < inventory.Count; i++)
            {
                var item = inventory[i];
                string itemStats = item switch
                {
                    WeaponItem weapon => $"Damage: {weapon.GetTotalDamage()}{GetWeaponDiff(weapon, player.Weapon as WeaponItem)}, Attack Speed: {weapon.GetTotalAttackSpeed():F1}x",
                    HeadItem head => $"Armor: {head.GetTotalArmor()}{GetArmorDiff(head, player.Head)}",
                    ChestItem chest => $"Armor: {chest.GetTotalArmor()}{GetArmorDiff(chest, player.Body)}",
                    FeetItem feet => $"Armor: {feet.GetTotalArmor()}{GetArmorDiff(feet, player.Feet)}",
                    _ => ""
                };
                string displayType = GetDisplayType(item);
                string itemActions = GetItemActions(item);
                
                // Show item type and name on first line
                Console.WriteLine($"{i + 1}. ({displayType}) {item.Name}");
                
                // Show stats on indented line
                if (!string.IsNullOrEmpty(itemStats))
                {
                    Console.WriteLine($"    {itemStats}");
                }
                
                // Show actions on indented line if any
                if (!string.IsNullOrEmpty(itemActions))
                {
                    Console.WriteLine($"    Actions: {itemActions.Substring(3)}"); // Remove " | " prefix
                }
                
                // Show affix bonuses if the item has any
                if (item.StatBonuses.Count > 0 || item.ActionBonuses.Count > 0 || item.Modifications.Count > 0)
                {
                    ShowItemBonuses(item);
                }
            }
        }

        private string GetArmorDiff(Item invItem, Item? equipped)
        {
            if (invItem is HeadItem head && equipped is HeadItem eqHead)
            {
                int diff = head.GetTotalArmor() - eqHead.GetTotalArmor();
                return diff > 0 ? $" [+{diff}]" : "";
            }
            if (invItem is ChestItem chest && equipped is ChestItem eqChest)
            {
                int diff = chest.GetTotalArmor() - eqChest.GetTotalArmor();
                return diff > 0 ? $" [+{diff}]" : "";
            }
            if (invItem is FeetItem feet && equipped is FeetItem eqFeet)
            {
                int diff = feet.GetTotalArmor() - eqFeet.GetTotalArmor();
                return diff > 0 ? $" [+{diff}]" : "";
            }
            return "";
        }

        private string GetWeaponDiff(WeaponItem invWeapon, WeaponItem? equipped)
        {
            if (equipped != null)
            {
                int diff = invWeapon.GetTotalDamage() - equipped.GetTotalDamage();
                return diff > 0 ? $" [+{diff}]" : "";
            }
            return "";
        }

        private void ShowItemBonuses(Item item)
        {
            // Show stat bonuses
            if (item.StatBonuses.Count > 0)
            {
                Console.WriteLine($"    Stat Bonuses: {string.Join(", ", item.StatBonuses.Select(sb => $"{sb.Name} (+{sb.Value} {sb.StatType})"))}");
            }
            
            // Show action bonuses (only if they have non-empty names)
            var actionBonusNames = item.ActionBonuses
                .Where(ab => !string.IsNullOrEmpty(ab.Name))
                .Select(ab => ab.Name)
                .ToList();
            if (actionBonusNames.Count > 0)
            {
                Console.WriteLine($"    Action Bonuses: {string.Join(", ", actionBonusNames)}");
            }
            
            // Show armor statuses
            if (item.ArmorStatuses.Count > 0)
            {
                var armorStatusBonuses = new List<string>();
                foreach (var status in item.ArmorStatuses)
                {
                    if (status.Effect == "armorSpikes")
                    {
                        armorStatusBonuses.Add($"Armor Spikes ({status.Value:F1}x damage on contact)");
                    }
                    else
                    {
                        armorStatusBonuses.Add($"{status.Name} ({status.Description})");
                    }
                }
                Console.WriteLine($"    Armor Statuses: {string.Join(", ", armorStatusBonuses)}");
            }
            
            // Show modifications
            if (item.Modifications.Count > 0)
            {
                var modificationBonuses = new List<string>();
                foreach (var mod in item.Modifications)
                {
                    if (!string.IsNullOrEmpty(mod.Effect))
                    {
                        // Show the actual value for modifications
                        string valueDisplay = mod.Effect switch
                        {
                            "damage" => $"{(mod.RolledValue >= 0 ? "+" : "")}{mod.RolledValue:F0} Damage",
                            "speedMultiplier" => $"{mod.RolledValue:F2}x Speed", 
                            "rollBonus" => $"+{mod.RolledValue:F0} Roll",
                            "reroll" => "Divine Reroll",
                            "lifesteal" => $"{(mod.RolledValue * 100):F1}% Lifesteal",
                            "autoSuccess" => "Auto Success",
                            "bleedChance" => $"{(mod.RolledValue * 100):F1}% Bleed",
                            "uniqueActionChance" => $"{(mod.RolledValue * 100):F1}% Unique Action",
                            "damageMultiplier" => $"{mod.RolledValue:F1}x Damage",
                            "magicFind" => $"+{mod.RolledValue:F0} Magic Find",
                            _ => $"({mod.Effect})"
                        };
                        modificationBonuses.Add($"{mod.Name} ({valueDisplay})");
                    }
                    else
                    {
                        modificationBonuses.Add(mod.Name);
                    }
                }
                Console.WriteLine($"    Modifications: {string.Join(", ", modificationBonuses)}");
            }
        }

        private string GetDisplayType(Item item)
        {
            if (item is WeaponItem weapon)
            {
                return weapon.WeaponType switch
                {
                    WeaponType.Sword => "WARRIOR WEAPON",
                    WeaponType.Dagger => "ROGUE WEAPON", 
                    WeaponType.Mace => "BARBARIAN WEAPON",
                    WeaponType.Wand => "WIZARD WEAPON",
                    _ => "WEAPON"
                };
            }
            return item.Type switch
            {
                ItemType.Head => "HEAD",
                ItemType.Chest => "BODY",
                ItemType.Feet => "FEET",
                ItemType.Weapon => "WEAPON",
                _ => item.Type.ToString().ToUpper()
            };
        }

        private void ShowActionPoolInfo()
        {
            // Show Action Pool (all available actions)
            var actionPool = player.GetActionPool();
            if (actionPool.Count > 0)
            {
                Console.WriteLine($"\nAction Pool ({actionPool.Count} available):");
                for (int i = 0; i < actionPool.Count; i++)
                {
                    var action = actionPool[i];
                    string inCombo = player.ComboSequence.Any(comboAction => comboAction.Name == action.Name) ? " [IN COMBO]" : "";
                    
                    // Check for class tags and display class information
                    string classInfo = "";
                    if (action.Tags != null && action.Tags.Contains("class"))
                    {
                        var classTag = action.Tags.FirstOrDefault(tag => tag != "class");
                        if (!string.IsNullOrEmpty(classTag))
                        {
                            classInfo = $" - {classTag.ToUpper()} ACTION";
                        }
                    }
                    
                    Console.WriteLine($"  {i + 1}. {action.Name}{inCombo}{classInfo}");
                    
                    Console.WriteLine($"      {action.Description} | Length: {action.Length:F1}x");
                }
            }
            else
            {
                Console.WriteLine("\nNo actions available in action pool.");
            }
            
            // Show Combo Sequence (selected actions for combo)
            var actionPoolCount = player.GetActionPool().Count;
            var comboActions = player.GetComboActions();
            if (comboActions.Count > 0)
            {
                Console.WriteLine($"\nCombo Sequence ({comboActions.Count} selected, {actionPoolCount} available):");
                for (int i = 0; i < comboActions.Count; i++)
                {
                    var action = comboActions[i];
                    Console.WriteLine($"  {i + 1}. {action.Name}");
                }
            }
            else
            {
                Console.WriteLine($"\nNo actions selected for combo sequence. ({actionPoolCount} available)");
            }
        }

        private void ShowComboManagementInfo()
        {
            // Show Action Pool (all available actions)
            var actionPool = player.GetActionPool();
            if (actionPool.Count > 0)
            {
                Console.WriteLine($"\nAction Pool ({actionPool.Count} available):");
                for (int i = 0; i < actionPool.Count; i++)
                {
                    var action = actionPool[i];
                    string inCombo = player.ComboSequence.Any(comboAction => comboAction.Name == action.Name) ? " [IN COMBO]" : "";
                    
                    // Check for class tags and display class information
                    string classInfo = "";
                    if (action.Tags != null && action.Tags.Contains("class"))
                    {
                        var classTag = action.Tags.FirstOrDefault(tag => tag != "class");
                        if (!string.IsNullOrEmpty(classTag))
                        {
                            classInfo = $" - {classTag.ToUpper()} ACTION";
                        }
                    }
                    
                    Console.WriteLine($"  {i + 1}. {action.Name}{inCombo}{classInfo}");
                    
                    Console.WriteLine($"      {action.Description} | Length: {action.Length:F1}x");
                }
            }
            else
            {
                Console.WriteLine("\nNo actions available in action pool.");
            }
            
            // Show Combo Sequence (selected actions for combo) with available count
            var actionPoolCount = player.GetActionPool().Count;
            var comboActions = player.GetComboActions();
            if (comboActions.Count > 0)
            {
                Console.WriteLine($"\nCombo Sequence ({comboActions.Count} selected, {actionPoolCount} available):");
                for (int i = 0; i < comboActions.Count; i++)
                {
                    var action = comboActions[i];
                    Console.WriteLine($"  {i + 1}. {action.Name}");
                }
            }
            else
            {
                Console.WriteLine($"\nNo actions selected for combo sequence. ({actionPoolCount} available)");
            }
        }

        private void ShowComboInfo()
        {
            // Show Combo Sequence (selected actions for combo) with available count
            var actionPoolCount = player.GetActionPool().Count;
            var comboActions = player.GetComboActions();
            if (comboActions.Count > 0)
            {
                Console.WriteLine($"\nCombo Sequence ({comboActions.Count} selected, {actionPoolCount} available):");
                for (int i = 0; i < comboActions.Count; i++)
                {
                    var action = comboActions[i];
                    Console.WriteLine($"  {i + 1}. {action.Name}");
                }
            }
            else
            {
                Console.WriteLine($"\nNo actions selected for combo sequence. ({actionPoolCount} available)");
            }
        }

        private double CalculateActionSpeedPercentage(Action action)
        {
            // Get the player's total attack speed (time in seconds)
            double playerAttackSpeed = player.GetTotalAttackSpeed();
            
            // Calculate the action duration by multiplying player speed by action length
            double actionDuration = playerAttackSpeed * action.Length;
            
            // Get base attack time from tuning config
            var tuning = TuningConfig.Instance;
            double baseAttackTime = tuning.Combat.BaseAttackTime;
            
            // Calculate speed percentage: (base time / action time) * 100
            // Higher percentage = faster action
            double speedPercentage = (baseAttackTime / actionDuration) * 100.0;
            
            return speedPercentage;
        }
        
        private string GetSpeedDescription(double speedPercentage)
        {
            if (speedPercentage >= 200) return "extremely fast";
            if (speedPercentage >= 150) return "very fast";
            if (speedPercentage >= 125) return "fast";
            if (speedPercentage >= 100) return "normal speed";
            if (speedPercentage >= 75) return "slow";
            if (speedPercentage >= 50) return "very slow";
            return "extremely slow";
        }

        private string GetItemActions(Item? item)
        {
            if (item == null)
                return "";
                
            var actions = new List<string>();
            
            // Get actions based on item type (using the same logic as the gear system)
            var gearActions = GetGearActionsForDisplay(item);
            
            if (gearActions.Count > 0)
            {
                actions.AddRange(gearActions);
            }
            
            // Check for action bonuses (legacy system)
            if (item.ActionBonuses.Count > 0)
            {
                foreach (var actionBonus in item.ActionBonuses)
                {
                    actions.Add(actionBonus.Name);
                }
            }
            
            // Check for modifications that might add actions
            if (item.Modifications.Count > 0)
            {
                foreach (var mod in item.Modifications)
                {
                    if (!string.IsNullOrEmpty(mod.Name) && mod.Name.ToLower().Contains("action"))
                    {
                        actions.Add($"Mod: {mod.Name}");
                    }
                }
            }
            
            // Filter out empty action names
            actions = actions.Where(a => !string.IsNullOrEmpty(a)).ToList();
            
            if (actions.Count > 0)
            {
                return $" | {string.Join(", ", actions)}";
            }
            
            return "";
        }

        private List<string> GetGearActionsForDisplay(Item gear)
        {
            // Use the same logic as Character.cs AddGearActions method
            if (gear is WeaponItem weapon)
            {
                // Get weapon actions from Actions.json based on weapon type
                return GetWeaponActionsFromJson(weapon.WeaponType);
            }
            else if (gear is HeadItem || gear is ChestItem || gear is FeetItem)
            {
                // Only show armor actions if this armor piece should have them
                // Use the same logic as Character.cs HasSpecialArmorActions
                if (ShouldArmorHaveActions(gear))
                {
                    // Use the gear's assigned action if it has one
                    if (!string.IsNullOrEmpty(gear.GearAction))
                    {
                        return new List<string> { gear.GearAction };
                    }
                    else
                    {
                        // Fallback to random selection for gear without assigned actions
                        return GetRandomArmorActionFromJson(gear);
                    }
                }
                else
                {
                    return new List<string>(); // No actions for basic starter gear
                }
            }
            
            return new List<string>();
        }

        /// <summary>
        /// Determines if an armor piece should have special actions (same logic as Character.cs)
        /// Only special armor pieces (looted gear with modifications, stat bonuses, etc.) should have actions
        /// Default/starter armor should have no actions
        /// </summary>
        private bool ShouldArmorHaveActions(Item armor)
        {
            // Check if this armor piece has special properties that indicate it should have actions
            
            // 1. Check if it has modifications (indicates special gear)
            if (armor.Modifications.Count > 0)
            {
                return true;
            }
            
            // 2. Check if it has stat bonuses (indicates special gear)
            if (armor.StatBonuses.Count > 0)
            {
                return true;
            }
            
            // 3. Check if it has action bonuses (legacy system)
            if (armor.ActionBonuses.Count > 0)
            {
                return true;
            }
            
            // 4. Check if it's not basic starter gear by name
            string[] basicGearNames = { "Leather Helmet", "Leather Armor", "Leather Boots", "Cloth Hood", "Cloth Robes", "Cloth Shoes" };
            if (basicGearNames.Contains(armor.Name))
            {
                return false; // Basic starter gear should have no actions
            }
            
            // 5. If it has a special name or properties, it might be special gear
            // For now, assume any non-basic gear might have actions
            return true;
        }

        private void ManageComboActions()
        {
            while (true)
            {
                Console.Clear();
                ShowCharacterStats();
                ShowCurrentEquipment();
                ShowComboManagementInfo();
                
                Console.WriteLine("\nCombo Management:");
                Console.WriteLine("1. Add action to combo");
                Console.WriteLine("2. Remove action from combo");
                Console.WriteLine("3. Swap two combo actions");
                Console.WriteLine("4. Back to inventory");
                Console.Write("\nChoose an option: ");

                if (int.TryParse(Console.ReadLine(), out int choice))
                {
                    switch (choice)
                    {
                        case 1:
                            AddActionToCombo();
                            break;
                        case 2:
                            RemoveActionFromCombo();
                            break;
                        case 3:
                            SwapComboActions();
                            break;
                        case 4:
                            return;
                        default:
                            Console.WriteLine("Invalid option.");
                            Console.WriteLine("Press any key to continue...");
                            Console.ReadKey();
                            break;
                    }
                }
                else
                {
                    Console.WriteLine("Invalid input.");
                    Console.WriteLine("Press any key to continue...");
                    Console.ReadKey();
                }
            }
        }

        private void AddActionToCombo()
        {
            var actionPool = player.GetActionPool();
            var comboActions = player.GetComboActions();
            
            // Show all available actions (allow duplicates)
            var availableActions = actionPool.ToList();
            
            if (availableActions.Count == 0)
            {
                Console.WriteLine("\nNo actions available to add to combo.");
                Console.WriteLine("Press any key to continue...");
                Console.ReadKey();
                return;
            }
            
            Console.WriteLine("\nAvailable actions to add to combo:");
            for (int i = 0; i < availableActions.Count; i++)
            {
                var action = availableActions[i];
                int timesInCombo = comboActions.Count(ca => ca.Name == action.Name);
                int timesAvailable = actionPool.Count(ap => ap.Name == action.Name);
                string usageInfo = timesInCombo > 0 ? $" [In combo: {timesInCombo}/{timesAvailable}]" : "";
                Console.WriteLine($"  {i + 1}. {action.Name}{usageInfo}");
                
                // Calculate speed percentage
                double speedPercentage = CalculateActionSpeedPercentage(action);
                string speedText = GetSpeedDescription(speedPercentage);
                
                // Build action stats line
                string statsLine = $"      {action.Description} | Damage: {action.DamageMultiplier:F1}x | Speed: {speedPercentage:F0}% ({speedText})";
                
                // Add any special effects
                if (action.CausesBleed) statsLine += ", Causes Bleed";
                if (action.CausesWeaken) statsLine += ", Causes Weaken";
                if (action.CausesSlow) statsLine += ", Causes Slow";
                if (action.CausesPoison) statsLine += ", Causes Poison";
                if (action.CausesStun) statsLine += ", Causes Stun";
                
                Console.WriteLine(statsLine);
            }
            
            Console.Write($"\nEnter action number to add (1-{availableActions.Count}): ");
            if (int.TryParse(Console.ReadLine(), out int choice) && choice >= 1 && choice <= availableActions.Count)
            {
                var selectedAction = availableActions[choice - 1];
                player.AddToCombo(selectedAction);
                Console.WriteLine($"Added {selectedAction.Name} to combo sequence.");
                Console.WriteLine("Press any key to continue...");
                Console.ReadKey();
            }
            else
            {
                Console.WriteLine("Invalid choice.");
                Console.WriteLine("Press any key to continue...");
                Console.ReadKey();
            }
        }
        
        private void RemoveActionFromCombo()
        {
            var comboActions = player.GetComboActions();
            
            if (comboActions.Count == 0)
            {
                Console.WriteLine("\nNo actions in combo sequence to remove.");
                Console.WriteLine("Press any key to continue...");
                Console.ReadKey();
                return;
            }
            
            Console.WriteLine("\nCurrent combo sequence:");
            for (int i = 0; i < comboActions.Count; i++)
            {
                var action = comboActions[i];
                string currentStep = (player.ComboStep % comboActions.Count == i) ? " ← NEXT" : "";
                Console.WriteLine($"  {i + 1}. {action.Name}{currentStep}");
                Console.WriteLine($"      {action.Description}");
            }
            
            Console.Write($"\nEnter action number to remove (1-{comboActions.Count}): ");
            if (int.TryParse(Console.ReadLine(), out int choice) && choice >= 1 && choice <= comboActions.Count)
            {
                var selectedAction = comboActions[choice - 1];
                player.RemoveFromCombo(selectedAction);
                Console.WriteLine($"Removed {selectedAction.Name} from combo sequence.");
                Console.WriteLine("Press any key to continue...");
                Console.ReadKey();
            }
            else
            {
                Console.WriteLine("Invalid choice.");
                Console.WriteLine("Press any key to continue...");
                Console.ReadKey();
            }
        }

        private void SwapComboActions()
        {
            var comboActions = player.GetComboActions();
            
            if (comboActions.Count < 2)
            {
                Console.WriteLine("\nYou need at least 2 actions to swap them.");
                Console.WriteLine("Press any key to continue...");
                Console.ReadKey();
                return;
            }
            
            Console.WriteLine("\nCurrent combo sequence:");
            for (int i = 0; i < comboActions.Count; i++)
            {
                var action = comboActions[i];
                string currentStep = (player.ComboStep % comboActions.Count == i) ? " ← NEXT" : "";
                Console.WriteLine($"  {i + 1}. {action.Name}{currentStep}");
            }
            
            Console.Write($"\nEnter first action number (1-{comboActions.Count}): ");
            if (int.TryParse(Console.ReadLine(), out int firstIndex) && firstIndex >= 1 && firstIndex <= comboActions.Count)
            {
                Console.Write($"Enter second action number (1-{comboActions.Count}): ");
                if (int.TryParse(Console.ReadLine(), out int secondIndex) && secondIndex >= 1 && secondIndex <= comboActions.Count)
                {
                    if (firstIndex == secondIndex)
                    {
                        Console.WriteLine("Cannot swap an action with itself.");
                    }
                    else
                    {
                        // Swap the actions in the action pool
                        var firstAction = comboActions[firstIndex - 1];
                        var secondAction = comboActions[secondIndex - 1];
                        
                        // Find and swap in the ActionPool
                        var firstPoolEntry = player.ActionPool.FirstOrDefault(a => a.action.Name == firstAction.Name);
                        var secondPoolEntry = player.ActionPool.FirstOrDefault(a => a.action.Name == secondAction.Name);
                        
                        if (firstPoolEntry.action != null && secondPoolEntry.action != null)
                        {
                            // Swap the combo orders
                            int tempOrder = firstPoolEntry.action.ComboOrder;
                            firstPoolEntry.action.ComboOrder = secondPoolEntry.action.ComboOrder;
                            secondPoolEntry.action.ComboOrder = tempOrder;
                            
                            Console.WriteLine($"Swapped {firstAction.Name} and {secondAction.Name} in combo sequence.");
                        }
                        else
                        {
                            Console.WriteLine("Error: Could not find actions to swap.");
                        }
                    }
                }
                else
                {
                    Console.WriteLine("Invalid second action number.");
                }
            }
            else
            {
                Console.WriteLine("Invalid first action number.");
            }
            
            Console.WriteLine("Press any key to continue...");
            Console.ReadKey();
        }

        private void ShowOptions()
        {
            Console.WriteLine("\nOptions:");
            Console.WriteLine("1. Equip an item");
            Console.WriteLine("2. Unequip an item");
            Console.WriteLine("3. Discard an item");
            Console.WriteLine("4. Manage combo actions");
            Console.WriteLine("5. Continue to dungeon");
            Console.WriteLine("6. Exit\n");
            Console.Write("Choose an option: ");
        }

        private void DiscardItem()
        {
            if (inventory.Count == 0)
            {
                Console.WriteLine("No items to discard.");
                Console.WriteLine("Press any key to continue...");
                Console.ReadKey();
                return;
            }

            Console.Write("Enter the number of the item to discard: ");
            if (int.TryParse(Console.ReadLine(), out int discardChoice) && 
                discardChoice >= 1 && discardChoice <= inventory.Count)
            {
                var itemToDiscard = inventory[discardChoice - 1];
                Console.WriteLine($"Are you sure you want to discard {itemToDiscard.Name}? (y/n)");
                string confirm = Console.ReadLine()?.ToLower() ?? "";
                
                if (confirm == "y" || confirm == "yes")
                {
                    // Clean up actions associated with the discarded item
                    player.RemoveItemActions(itemToDiscard);
                    
                    inventory.RemoveAt(discardChoice - 1);
                    Console.WriteLine($"{itemToDiscard.Name} has been discarded.");
                }
                else
                {
                    Console.WriteLine("Discard cancelled.");
                }
            }
            else
            {
                Console.WriteLine("Invalid item number.");
            }
            
            Console.WriteLine("Press any key to continue...");
            Console.ReadKey();
        }

        private void EquipItem()
        {
            Console.Write("Enter the number of the item to equip: ");
            if (int.TryParse(Console.ReadLine(), out int equipChoice) && 
                equipChoice > 0 && equipChoice <= inventory.Count)
            {
                var item = inventory[equipChoice - 1];
                string slot = item.Type switch
                {
                    ItemType.Weapon => "weapon",
                    ItemType.Head => "head",
                    ItemType.Chest => "body",
                    ItemType.Feet => "feet",
                    _ => ""
                };
                // Get the previously equipped item (if any)
                var previousItem = player.EquipItem(item, slot);
                // Remove the new item from inventory
                inventory.RemoveAt(equipChoice - 1);
                // Destroy the previous item (do not add back to inventory)
                if (previousItem != null)
                {
                    Console.WriteLine($"Unequipped and destroyed {previousItem.Name}. Equipped {item.Name}.");
                }
                else
                {
                    Console.WriteLine($"Equipped {item.Name}.");
                }
            }
            else
            {
                Console.WriteLine("Invalid choice.");
            }
        }

        private void UnequipItem()
        {
            Console.WriteLine("Choose slot to unequip:");
            Console.WriteLine("1. Weapon");
            Console.WriteLine("2. Head");
            Console.WriteLine("3. Body");
            Console.WriteLine("4. Feet");
            Console.Write("Enter your choice: ");

            if (int.TryParse(Console.ReadLine(), out int slotChoice) && 
                slotChoice >= 1 && slotChoice <= 4)
            {
                string slot = slotChoice switch
                {
                    1 => "weapon",
                    2 => "head",
                    3 => "body",
                    4 => "feet",
                    _ => ""
                };
                var unequippedItem = player.UnequipItem(slot);
                if (unequippedItem != null)
                {
                    inventory.Add(unequippedItem); // Add the unequipped item back to inventory
                    Console.WriteLine($"Unequipped {unequippedItem.Name}.");
                }
                else
                {
                    Console.WriteLine($"No item was equipped in the {slot} slot.");
                }
            }
            else
            {
                Console.WriteLine("Invalid choice.");
            }
        }

        // Helper methods from Character.cs for data-driven action selection
        private List<string> GetWeaponActionsFromJson(WeaponType weaponType)
        {
            var allActions = ActionLoader.GetAllActions();
            var weaponActions = new List<string>();
            
            // Get actions that match the weapon type and have "weapon" tag, but exclude "unique" actions
            foreach (var actionData in allActions)
            {
                if (actionData.Tags.Contains("weapon") && 
                    actionData.Tags.Contains(weaponType.ToString().ToLower()) &&
                    !actionData.Tags.Contains("unique"))
                {
                    weaponActions.Add(actionData.Name);
                }
            }
            
            return weaponActions;
        }

        private List<string> GetRandomArmorActionFromJson(Item armor)
        {
            // Map specific gear types to specific actions
            if (armor is FeetItem)
            {
                // Feet items should provide KICK action
                return new List<string> { "KICK" };
            }
            else if (armor is HeadItem)
            {
                // Head items should provide HELMET RAM action
                return new List<string> { "HELMET RAM" };
            }
            else if (armor is ChestItem)
            {
                // Chest items should provide CHEST PLATE RAM action
                return new List<string> { "CHEST PLATE RAM" };
            }
            
            // Fallback to random selection for unknown armor types
            var allActions = ActionLoader.GetAllActions();
            var armorActions = new List<string>();
            
            // Get all armor actions, excluding environmental actions
            foreach (var actionData in allActions)
            {
                if (actionData.Tags.Contains("armor") && 
                    !actionData.Tags.Contains("environment"))
                {
                    armorActions.Add(actionData.Name);
                }
            }
            
            // If no armor actions found, get general non-environmental combo actions
            if (armorActions.Count == 0)
            {
                foreach (var actionData in allActions)
                {
                    if (actionData.IsComboAction && 
                        !actionData.Tags.Contains("environment") &&
                        !actionData.Tags.Contains("unique"))
                    {
                        armorActions.Add(actionData.Name);
                    }
                }
            }
            
            // Return a random action if any are available
            if (armorActions.Count > 0)
            {
                var randomAction = armorActions[Random.Shared.Next(armorActions.Count)];
                return new List<string> { randomAction };
            }
            
            return armorActions;
        }
    }
} 