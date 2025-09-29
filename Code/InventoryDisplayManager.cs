using System;
using System.Collections.Generic;
using System.Linq;

namespace RPGGame
{
    /// <summary>
    /// Manages all display logic for inventory, character stats, and equipment
    /// </summary>
    public class InventoryDisplayManager
    {
        private Character player;
        private List<Item> inventory;

        public InventoryDisplayManager(Character player, List<Item> inventory)
        {
            this.player = player;
            this.inventory = inventory;
        }

        /// <summary>
        /// Shows the main inventory display with all sections
        /// </summary>
        public void ShowMainDisplay()
        {
            UIManager.WriteMenuLine("\n--- Inventory ---");
            ShowCharacterStats();
            ShowCurrentEquipment();
            ShowComboInfo();
            ShowInventory();
            ShowOptions();
        }

        /// <summary>
        /// Displays character statistics and class information
        /// </summary>
        public void ShowCharacterStats()
        {
            int weaponDamage = (player.Weapon is WeaponItem w) ? w.GetTotalDamage() : 0;
            int equipmentDamageBonus = player.GetEquipmentDamageBonus();
            int modificationDamageBonus = player.GetModificationDamageBonus();
            int damage = player.GetEffectiveStrength() + weaponDamage + equipmentDamageBonus + modificationDamageBonus;
            double attackSpeed = player.GetTotalAttackSpeed();
            int armor = player.GetTotalArmor();
            UIManager.WriteMenuLine($"{player.Name} (Level {player.Level}) - {player.GetCurrentClass()}");
            UIManager.WriteMenuLine($"Health: {player.CurrentHealth}/{player.GetEffectiveMaxHealth()}  STR: {player.GetEffectiveStrength()}  AGI: {player.GetEffectiveAgility()}  TEC: {player.GetEffectiveTechnique()}  INT: {player.GetEffectiveIntelligence()}");
            int totalRollBonus = player.GetIntelligenceRollBonus() + player.GetModificationRollBonus() + player.GetEquipmentRollBonus();
            double secondsPerAttack = attackSpeed;
            // Get current amplification
            double currentAmplification = player.GetCurrentComboAmplification();
            int magicFind = player.GetMagicFind();
            UIManager.WriteMenuLine($"Damage: {damage} (STR:{player.GetEffectiveStrength()} + Weapon:{weaponDamage} + Equipment:{equipmentDamageBonus} + Mods:{modificationDamageBonus})  Attack Time: {attackSpeed:0.00}s  Amplification: {currentAmplification:F2}x  Roll Bonus: +{totalRollBonus}  Armor: {armor}");
            if (magicFind > 0)
            {
                UIManager.WriteMenuLine($"Magic Find: +{magicFind} (improves rare item drop chances)");
            }
            // Show only classes with points > 0
            var classPointsInfo = new List<string>();
            if (player.BarbarianPoints > 0) classPointsInfo.Add($"Barbarian({player.BarbarianPoints})");
            if (player.WarriorPoints > 0) classPointsInfo.Add($"Warrior({player.WarriorPoints})");
            if (player.RoguePoints > 0) classPointsInfo.Add($"Rogue({player.RoguePoints})");
            if (player.WizardPoints > 0) classPointsInfo.Add($"Wizard({player.WizardPoints})");
            
            if (classPointsInfo.Count > 0)
            {
                UIManager.WriteMenuLine($"Class Points: {string.Join(" ", classPointsInfo)}");
                UIManager.WriteMenuLine($"Next Upgrades: {player.GetClassUpgradeInfo()}");
            }
        }

        /// <summary>
        /// Displays currently equipped items with their stats
        /// </summary>
        public void ShowCurrentEquipment()
        {
            UIManager.WriteMenuLine("");
            UIManager.WriteMenuLine("Currently Equipped:");
            
            // Show weapon with inline stats
            if (player.Weapon is WeaponItem weapon)
            {
                UIManager.WriteMenuLine($"Weapon: {weapon.Name} - Damage: {weapon.GetTotalDamage()}, Attack Speed: {weapon.GetTotalAttackSpeed():F1}x");
            }
            else
            {
                UIManager.WriteMenuLine("Weapon: None");
            }
            
            // Show armor pieces with inline stats
            if (player.Head is HeadItem head)
            {
                UIManager.WriteMenuLine($"Head: {head.Name} - Armor: {head.GetTotalArmor()}");
            }
            else
            {
                UIManager.WriteMenuLine("Head: None");
            }
            
            if (player.Body is ChestItem chest)
            {
                UIManager.WriteMenuLine($"Body: {chest.Name} - Armor: {chest.GetTotalArmor()}");
            }
            else
            {
                UIManager.WriteMenuLine("Body: None");
            }
            
            if (player.Feet is FeetItem feet)
            {
                UIManager.WriteMenuLine($"Feet: {feet.Name} - Armor: {feet.GetTotalArmor()}");
            }
            else
            {
                UIManager.WriteMenuLine("Feet: None");
            }
        }

        /// <summary>
        /// Displays the inventory list with item stats and bonuses
        /// </summary>
        public void ShowInventory()
        {
            // Don't display anything if inventory is empty
            if (inventory.Count == 0)
            {
                return;
            }

            UIManager.WriteMenuLine("");
            UIManager.WriteMenuLine("Inventory:");

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
                UIManager.WriteMenuLine($"{i + 1}. ({displayType}) {item.Name}");
                
                // Show stats on indented line
                if (!string.IsNullOrEmpty(itemStats))
                {
                    UIManager.WriteMenuLine($"    {itemStats}");
                }
                
                // Show actions on indented line if any
                if (!string.IsNullOrEmpty(itemActions))
                {
                    UIManager.WriteMenuLine($"    Actions: {itemActions.Substring(3)}"); // Remove " | " prefix
                }
                
                // Show affix bonuses if the item has any
                if (item.StatBonuses.Count > 0 || item.ActionBonuses.Count > 0 || item.Modifications.Count > 0)
                {
                    ShowItemBonuses(item);
                }
            }
        }

        /// <summary>
        /// Displays combo information and management options
        /// </summary>
        public void ShowComboInfo()
        {
            UIManager.WriteMenuLine("");
            UIManager.WriteMenuLine("Combo Actions:");
            if (player.ComboSequence.Count == 0)
            {
                UIManager.WriteMenuLine("(No combo actions set)");
            }
            else
            {
                for (int i = 0; i < player.ComboSequence.Count; i++)
                {
                    var action = player.ComboSequence[i];
                    UIManager.WriteMenuLine($"{i + 1}. {action.Name} (Length: {action.Length:F1})");
                }
            }
        }

        /// <summary>
        /// Displays the main menu options
        /// </summary>
        public void ShowOptions()
        {
            UIManager.WriteMenuLine("");
            UIManager.WriteMenuLine("Options:");
            UIManager.WriteMenuLine("1. Equip Item");
            UIManager.WriteMenuLine("2. Unequip Item");
            UIManager.WriteMenuLine("3. Discard Item");
            UIManager.WriteMenuLine("4. Manage Combo Actions");
            UIManager.WriteMenuLine("5. Continue to Dungeon");
            UIManager.WriteMenuLine("6. Return to Main Menu");
            UIManager.Write("Enter your choice: ");
        }

        /// <summary>
        /// Shows detailed item bonuses and modifications
        /// </summary>
        private void ShowItemBonuses(Item item)
        {
            // Show stat bonuses
            if (item.StatBonuses.Count > 0)
            {
                UIManager.WriteMenuLine($"    Stat Bonuses: {string.Join(", ", item.StatBonuses.Select(b => $"{b.StatType} +{b.Value}"))}");
            }
            
            // Show action bonuses (legacy system)
            if (item.ActionBonuses.Count > 0)
            {
                UIManager.WriteMenuLine($"    Action Bonuses: {string.Join(", ", item.ActionBonuses.Select(b => $"{b.Name} +{b.Weight}"))}");
            }
            
            // Show modifications
            if (item.Modifications.Count > 0)
            {
                UIManager.WriteMenuLine($"    Modifications: {string.Join(", ", item.Modifications)}");
            }
        }

        /// <summary>
        /// Gets the display type for an item
        /// </summary>
        private string GetDisplayType(Item item)
        {
            return item.Type switch
            {
                ItemType.Weapon => "Weapon",
                ItemType.Head => "Head",
                ItemType.Chest => "Chest",
                ItemType.Feet => "Feet",
                _ => "Item"
            };
        }

        /// <summary>
        /// Gets item actions as a formatted string
        /// </summary>
        private string GetItemActions(Item item)
        {
            var actions = new List<string>();
            
            if (item is WeaponItem weapon)
            {
                var weaponActions = GetWeaponActionsFromJson(weapon.WeaponType);
                actions.AddRange(weaponActions);
            }
            else if (ShouldArmorHaveActions(item))
            {
                var armorActions = GetRandomArmorActionFromJson(item);
                actions.AddRange(armorActions);
            }
            
            return actions.Count > 0 ? " | " + string.Join(" | ", actions) : "";
        }

        /// <summary>
        /// Gets armor difference display for comparison
        /// </summary>
        private string GetArmorDiff(Item invItem, Item? equipped)
        {
            if (equipped == null) return " (NEW)";
            
            int invArmor = invItem switch
            {
                HeadItem head => head.GetTotalArmor(),
                ChestItem chest => chest.GetTotalArmor(),
                FeetItem feet => feet.GetTotalArmor(),
                _ => 0
            };
            
            int equippedArmor = equipped switch
            {
                HeadItem head => head.GetTotalArmor(),
                ChestItem chest => chest.GetTotalArmor(),
                FeetItem feet => feet.GetTotalArmor(),
                _ => 0
            };
            
            int diff = invArmor - equippedArmor;
            if (diff > 0) return $" (+{diff})";
            if (diff < 0) return $" ({diff})";
            return " (=)";
        }

        /// <summary>
        /// Gets weapon difference display for comparison
        /// </summary>
        private string GetWeaponDiff(WeaponItem invWeapon, WeaponItem? equipped)
        {
            if (equipped == null) return " (NEW)";
            
            int damageDiff = invWeapon.GetTotalDamage() - equipped.GetTotalDamage();
            if (damageDiff > 0) return $" (+{damageDiff} damage)";
            if (damageDiff < 0) return $" ({damageDiff} damage)";
            return " (= damage)";
        }

        /// <summary>
        /// Determines if an armor piece should have special actions
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
            // Basic gear names moved to GameData configuration
            string[] basicGearNames = BasicGearConfig.GetBasicGearNames();
            if (basicGearNames.Contains(armor.Name))
            {
                return false; // Basic starter gear should have no actions
            }
            
            // 5. If it has a special name or properties, it might be special gear
            // For now, assume any non-basic gear might have actions
            return true;
        }

        /// <summary>
        /// Gets weapon actions from JSON data
        /// </summary>
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

        /// <summary>
        /// Gets random armor actions from JSON data
        /// </summary>
        private List<string> GetRandomArmorActionFromJson(Item armor)
        {
            // Return empty list - no action bonuses to display
            return new List<string>();
        }
        
    }
}
