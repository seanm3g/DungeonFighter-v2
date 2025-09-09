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
                            return true; // Continue to dungeon
                        case 5:
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
            double weaponAttackSpeed = (player.Weapon is WeaponItem w2) ? w2.GetTotalAttackSpeed() : 1.0;
            int damage = player.Strength + weaponDamage;
            double attackSpeed = weaponAttackSpeed + (player.Agility * 0.03);
            if (attackSpeed < 0.2) attackSpeed = 0.2; // minimum speed
            int armor = 0;
            if (player.Head is HeadItem head) armor += head.GetTotalArmor();
            if (player.Body is ChestItem body) armor += body.GetTotalArmor();
            if (player.Feet is FeetItem feet) armor += feet.GetTotalArmor();
            Console.WriteLine($"{player.Name} (Level {player.Level}) - {player.GetCurrentClass()}");
            Console.WriteLine($"Health: {player.CurrentHealth}/{player.MaxHealth}  STR: {player.Strength}  AGI: {player.Agility}  TEC: {player.Technique}");
            Console.WriteLine($"Damage: {damage}  Attack Speed: {attackSpeed:0.00}  Armor: {armor}");
            Console.WriteLine($"Class Points: Barbarian({player.BarbarianPoints}) Warrior({player.WarriorPoints}) Rogue({player.RoguePoints}) Wizard({player.WizardPoints})");
            
            // Show action pool and combo information
            ShowActionPoolInfo();
        }

        private void ShowCurrentEquipment()
        {
            Console.WriteLine();
            Console.WriteLine("Currently Equipped:");
            Console.WriteLine($"Weapon: {player.Weapon?.Name ?? "None"}{GetItemActions(player.Weapon)}");
            Console.WriteLine($"Head: {player.Head?.Name ?? "None"}{GetItemActions(player.Head)}");
            Console.WriteLine($"Body: {player.Body?.Name ?? "None"}{GetItemActions(player.Body)}");
            Console.WriteLine($"Feet: {player.Feet?.Name ?? "None"}{GetItemActions(player.Feet)}");
            
            // Show detailed stats for equipped items
            if (player.Weapon is WeaponItem weapon)
            {
                Console.WriteLine($"  Weapon Stats: Damage {weapon.GetTotalDamage()}, Attack Speed {weapon.GetTotalAttackSpeed():F1}");
                ShowItemBonuses(weapon);
            }
            if (player.Head is HeadItem head)
            {
                Console.WriteLine($"  Head Stats: Armor {head.GetTotalArmor()}");
                ShowItemBonuses(head);
            }
            if (player.Body is ChestItem body)
            {
                Console.WriteLine($"  Body Stats: Armor {body.GetTotalArmor()}");
                ShowItemBonuses(body);
            }
            if (player.Feet is FeetItem feet)
            {
                Console.WriteLine($"  Feet Stats: Armor {feet.GetTotalArmor()}");
                ShowItemBonuses(feet);
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
                Console.WriteLine($"{i + 1}. {item.Name} ({displayType}) - {itemStats}{itemActions}");
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
                Console.WriteLine($"    Modifications: {string.Join(", ", item.Modifications.Select(m => m.Name))}");
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
                    Console.WriteLine($"      {action.Description}");
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
                    string currentStep = (player.ComboStep % comboActions.Count == i) ? " ← NEXT" : "";
                    Console.WriteLine($"  {i + 1}. {action.Name} (Order: {action.ComboOrder}){currentStep}");
                    Console.WriteLine($"      {action.Description}");
                }
                Console.WriteLine($"Current Combo Step: {player.ComboStep % comboActions.Count + 1} of {comboActions.Count}");
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
                    actions.Add($"Action: {actionBonus.Name}");
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
            
            if (actions.Count > 0)
            {
                return $" | Actions: {string.Join(", ", actions)}";
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
                    WeaponType.Sword => new[] { "PARRY", "SWORD SLASH", "SWORDMASTER STRIKE" },
                    WeaponType.Mace => new[] { "CRUSHING BLOW", "SHIELD BREAK", "CRUSHING MOMENTUM" },
                    WeaponType.Dagger => new[] { "QUICK STAB", "POISON BLADE", "VENOMOUS ASSASSIN" },
                    WeaponType.Wand => new[] { "MAGIC MISSILE", "ARCANE SHIELD", "ARCANE FURY" },
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
                Console.WriteLine("4. Reset combo to step 1");
                Console.WriteLine("5. Back to inventory");
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
                            player.ComboStep = 0;
                            Console.WriteLine("Combo reset to step 1.");
                            Console.WriteLine("Press any key to continue...");
                            Console.ReadKey();
                            break;
                        case 5:
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
            Console.WriteLine("4. Continue to dungeon");
            Console.WriteLine("5. Exit\n");
            Console.Write("Choose an option: ");
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