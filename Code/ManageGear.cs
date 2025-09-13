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
                ShowActionPoolInfo();
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
                            ManageComboActions();
                            break;
                        case 4:
                            DiscardItem();
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
            int damage = player.GetEffectiveStrength() + weaponDamage + equipmentDamageBonus;
            double attackSpeed = player.GetTotalAttackSpeed();
            int armor = 0;
            if (player.Head is HeadItem head) armor += head.GetTotalArmor();
            if (player.Body is ChestItem body) armor += body.GetTotalArmor();
            if (player.Feet is FeetItem feet) armor += feet.GetTotalArmor();
            Console.WriteLine($"{player.Name} (Level {player.Level}) - {player.GetCurrentClass()}");
            Console.WriteLine($"Health: {player.CurrentHealth}/{player.GetEffectiveMaxHealth()}  STR: {player.GetEffectiveStrength()}  AGI: {player.GetEffectiveAgility()}  TEC: {player.GetEffectiveTechnique()}  INT: {player.GetEffectiveIntelligence()}");
            Console.WriteLine($"Damage: {damage} (STR:{player.GetEffectiveStrength()} + Weapon:{weaponDamage} + Equipment:{equipmentDamageBonus})  Attack Speed: {attackSpeed:0.00} ({player.GetAttacksPerTurn()} attacks/turn)  Roll Bonus: +{player.GetIntelligenceRollBonus()}  Armor: {armor}");
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
            Console.WriteLine("\nInventory:");
            for (int i = 0; i < inventory.Count; i++)
            {
                var item = inventory[i];
                string itemStats = item switch
                {
                    WeaponItem weapon => $"Damage: {weapon.GetTotalDamage()}{GetWeaponDiff(weapon, player.Weapon as WeaponItem)}",
                    HeadItem head => $"Armor: {head.GetTotalArmor()}{GetArmorDiff(head, player.Head)}",
                    ChestItem chest => $"Armor: {chest.GetTotalArmor()}{GetArmorDiff(chest, player.Body)}",
                    FeetItem feet => $"Armor: {feet.GetTotalArmor()}{GetArmorDiff(feet, player.Feet)}",
                    _ => ""
                };
                string displayType = GetDisplayType(item);
                string itemActions = GetItemActions(item);
                
                // Show item name and type on first line
                Console.WriteLine($"{i + 1}. {item.Name} ({displayType})");
                
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
            
            // Show action bonuses
            if (item.ActionBonuses.Count > 0)
            {
                Console.WriteLine($"    Action Bonuses: {string.Join(", ", item.ActionBonuses.Select(ab => ab.Name))}");
            }
            
            // Show modifications
            if (item.Modifications.Count > 0)
            {
                var modificationBonuses = new List<string>();
                foreach (var mod in item.Modifications)
                {
                    if (!string.IsNullOrEmpty(mod.Effect))
                    {
                        modificationBonuses.Add($"{mod.Name} ({mod.Effect})");
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
                    WeaponType.Sword => "Warrior Weapon",
                    WeaponType.Dagger => "Rogue Weapon", 
                    WeaponType.Mace => "Barbarian Weapon",
                    WeaponType.Wand => "Wizard Weapon",
                    _ => "Weapon"
                };
            }
            return item.Type.ToString();
        }

        private void ShowActionPoolInfo()
        {
            // Show Combo Information
            Console.WriteLine($"\n{player.GetComboInfo()}");
            
            // Show Action Pool (all available actions)
            var actionPool = player.GetActionPool();
            if (actionPool.Count > 0)
            {
                Console.WriteLine($"\nAction Pool ({actionPool.Count} available):");
                for (int i = 0; i < actionPool.Count; i++)
                {
                    var action = actionPool[i];
                    string inCombo = player.ComboSequence.Contains(action) ? " [IN COMBO]" : "";
                    Console.WriteLine($"  {i + 1}. {action.Name}{inCombo}");
                    
                    string damageInfo = "";
                    if (action.Type == ActionType.Attack && action.DamageMultiplier > 0)
                    {
                        damageInfo = $" | Damage: {action.DamageMultiplier:F1}x";
                    }
                    
                    Console.WriteLine($"      {action.Description} | Length: {action.Length:F1}{damageInfo}");
                }
            }
            else
            {
                Console.WriteLine("\nNo actions available in action pool.");
            }
            
            // Show Combo Sequence (selected actions for combo)
            var comboActions = player.GetComboActions();
            if (comboActions.Count > 0)
            {
                Console.WriteLine($"\nCombo Sequence ({comboActions.Count} selected):");
                for (int i = 0; i < comboActions.Count; i++)
                {
                    var action = comboActions[i];
                    Console.WriteLine($"  {i + 1}. {action.Name}");
                }
            }
            else
            {
                Console.WriteLine("\nNo actions selected for combo sequence.");
            }
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
            var actions = new List<string>();
            var actionPool = player.GetActionPool();
            
            if (gear is WeaponItem weapon)
            {
                var weaponActions = weapon.WeaponType switch
                {
                    WeaponType.Sword => new[] { "PARRY", "SWORD SLASH" },
                    WeaponType.Mace => new[] { "CRUSHING BLOW", "SHIELD BREAK" },
                    WeaponType.Dagger => new[] { "QUICK STAB", "POISON BLADE" },
                    WeaponType.Wand => new[] { "MAGIC MISSILE", "ARCANE SHIELD" },
                    _ => new string[0]
                };
                
                // All weapons have their actions, so show them all
                actions.AddRange(weaponActions);
            }
            else if (gear is HeadItem)
            {
                if (actionPool.Any(a => a.Name == "HEADBUTT"))
                {
                    actions.Add("HEADBUTT");
                }
            }
            else if (gear is ChestItem)
            {
                if (actionPool.Any(a => a.Name == "CHEST BASH"))
                {
                    actions.Add("CHEST BASH");
                }
            }
            else if (gear is FeetItem)
            {
                if (actionPool.Any(a => a.Name == "KICK"))
                {
                    actions.Add("KICK");
                }
            }
            
            return actions;
        }

        private void ManageComboActions()
        {
            while (true)
            {
                Console.Clear();
                ShowCharacterStats();
                ShowCurrentEquipment();
                ShowActionPoolInfo();
                
                Console.WriteLine("\nCombo Management:");
                Console.WriteLine("1. Add action to combo");
                Console.WriteLine("2. Remove action from combo");
                Console.WriteLine("3. Swap two combo actions");
                Console.WriteLine("4. Back to inventory");
                Console.Write("Choose an option: ");

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
            
            // Filter out actions already in combo
            var availableActions = actionPool.Where(a => !comboActions.Contains(a)).ToList();
            
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
                Console.WriteLine($"  {i + 1}. {action.Name}");
                Console.WriteLine($"      {action.Description}");
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
            Console.WriteLine("3. Manage combo actions");
            Console.WriteLine("4. Discard an item");
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
    }
} 