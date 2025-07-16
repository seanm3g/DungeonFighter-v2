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
                            return true; // Continue to dungeon
                        case 4:
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
            int weaponDamage = (player.Weapon is WeaponItem w) ? w.Damage : 0;
            double weaponWeight = (player.Weapon is WeaponItem w2) ? w2.Weight : 1.0;
            int damage = player.Strength + weaponDamage;
            double attackSpeed = 1.0 + (player.Agility * 0.03) - (weaponWeight * 0.1);
            if (attackSpeed < 0.2) attackSpeed = 0.2; // minimum speed
            int armor = 0;
            if (player.Head is HeadItem head) armor += head.Armor;
            if (player.Body is ChestItem body) armor += body.Armor;
            if (player.Feet is FeetItem feet) armor += feet.Armor;
            Console.WriteLine($"{player.Name} (Level {player.Level})");
            Console.WriteLine($"Health: {player.CurrentHealth}/{player.MaxHealth}  STR: {player.Strength}  AGI: {player.Agility}  TEC: {player.Technique}");
            Console.WriteLine($"Damage: {damage}  Attack Speed: {attackSpeed:0.00}  Armor: {armor}");
        }

        private void ShowCurrentEquipment()
        {
            Console.WriteLine();
            Console.WriteLine("Currently Equipped:");
            Console.WriteLine($"Weapon: {player.Weapon?.Name ?? "None"}");
            Console.WriteLine($"Head: {player.Head?.Name ?? "None"}");
            Console.WriteLine($"Body: {player.Body?.Name ?? "None"}");
            Console.WriteLine($"Feet: {player.Feet?.Name ?? "None"}");
        }

        private void ShowInventory()
        {
            Console.WriteLine("\nInventory:");
            for (int i = 0; i < inventory.Count; i++)
            {
                var item = inventory[i];
                string itemStats = item switch
                {
                    WeaponItem weapon => $"Damage: {weapon.Damage}{GetWeaponDiff(weapon, player.Weapon as WeaponItem)}",
                    HeadItem head => $"Armor: {head.Armor}{GetArmorDiff(head, player.Head)}",
                    ChestItem chest => $"Armor: {chest.Armor}{GetArmorDiff(chest, player.Body)}",
                    FeetItem feet => $"Armor: {feet.Armor}{GetArmorDiff(feet, player.Feet)}",
                    _ => ""
                };
                Console.WriteLine($"{i + 1}. {item.Name} ({item.Type}) - {itemStats}");
            }
        }

        private string GetArmorDiff(Item invItem, Item? equipped)
        {
            if (invItem is HeadItem head && equipped is HeadItem eqHead)
            {
                int diff = head.Armor - eqHead.Armor;
                return diff > 0 ? $" [+{diff}]" : "";
            }
            if (invItem is ChestItem chest && equipped is ChestItem eqChest)
            {
                int diff = chest.Armor - eqChest.Armor;
                return diff > 0 ? $" [+{diff}]" : "";
            }
            if (invItem is FeetItem feet && equipped is FeetItem eqFeet)
            {
                int diff = feet.Armor - eqFeet.Armor;
                return diff > 0 ? $" [+{diff}]" : "";
            }
            return "";
        }

        private string GetWeaponDiff(WeaponItem invWeapon, WeaponItem? equipped)
        {
            if (equipped != null)
            {
                int diff = invWeapon.Damage - equipped.Damage;
                return diff > 0 ? $" [+{diff}]" : "";
            }
            return "";
        }

        private void ShowOptions()
        {
            Console.WriteLine("\nOptions:");
            Console.WriteLine("1. Equip an item");
            Console.WriteLine("2. Unequip an item");
            Console.WriteLine("3. Continue to dungeon");
            Console.WriteLine("4. Exit\n");
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