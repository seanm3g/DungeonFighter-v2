using System;
using System.Collections.Generic;
using System.Linq;

namespace RPGGame
{
    /// <summary>
    /// Manages character equipment, inventory, and equipment-related bonuses
    /// </summary>
    public class CharacterEquipment
    {
        public List<Item> Inventory { get; set; }
        public Item? Head { get; set; }
        public Item? Body { get; set; }
        public Item? Weapon { get; set; }
        public Item? Feet { get; set; }

        public CharacterEquipment()
        {
            Inventory = new List<Item>();
            Head = null;
            Body = null;
            Weapon = null;
            Feet = null;
        }

        public Item? EquipItem(Item item, string slot)
        {
            Item? previousItem = null;
            switch (slot.ToLower())
            {
                case "head": 
                    previousItem = Head;
                    Head = item;
                    break;
                case "body": 
                    previousItem = Body;
                    Body = item;
                    break;
                case "weapon": 
                    previousItem = Weapon;
                    Weapon = item;
                    break;
                case "feet": 
                    previousItem = Feet;
                    Feet = item;
                    break;
            }
            return previousItem;
        }

        public Item? UnequipItem(string slot)
        {
            Item? unequippedItem = null;
            switch (slot.ToLower())
            {
                case "head": 
                    unequippedItem = Head;
                    Head = null;
                    break;
                case "body": 
                    unequippedItem = Body;
                    Body = null;
                    break;
                case "weapon": 
                    unequippedItem = Weapon;
                    Weapon = null;
                    break;
                case "feet": 
                    unequippedItem = Feet;
                    Feet = null;
                    break;
            }
            return unequippedItem;
        }

        public void AddToInventory(Item item) => Inventory.Add(item);
        public bool RemoveFromInventory(Item item) => Inventory.Remove(item);

        public int GetEquipmentStatBonus(string statType)
        {
            int totalBonus = 0;
            
            var equippedItems = new[] { Head, Body, Weapon, Feet };
            foreach (var item in equippedItems)
            {
                if (item != null)
                {
                    foreach (var statBonus in item.StatBonuses)
                    {
                        if (statBonus.StatType == statType)
                        {
                            totalBonus += (int)statBonus.Value;
                        }
                        else if (statBonus.StatType == "ALL")
                        {
                            totalBonus += (int)statBonus.Value;
                        }
                    }
                }
            }
            
            return totalBonus;
        }

        public double GetEquipmentStatBonusDouble(string statType)
        {
            double totalBonus = 0.0;
            
            var equippedItems = new[] { Head, Body, Weapon, Feet };
            foreach (var item in equippedItems)
            {
                if (item != null)
                {
                    foreach (var statBonus in item.StatBonuses)
                    {
                        if (statBonus.StatType == statType)
                        {
                            totalBonus += statBonus.Value;
                        }
                        else if (statBonus.StatType == "ALL")
                        {
                            totalBonus += statBonus.Value;
                        }
                    }
                }
            }
            
            return totalBonus;
        }

        public int GetEquipmentDamageBonus()
        {
            return GetEquipmentStatBonus("Damage");
        }

        public int GetEquipmentHealthBonus()
        {
            return GetEquipmentStatBonus("Health");
        }

        public int GetEquipmentRollBonus()
        {
            return GetEquipmentStatBonus("RollBonus");
        }

        public int GetMagicFind()
        {
            return GetEquipmentStatBonus("MagicFind") + GetModificationMagicFind();
        }

        public double GetEquipmentAttackSpeedBonus()
        {
            return GetEquipmentStatBonusDouble("AttackSpeed");
        }

        public int GetEquipmentHealthRegenBonus()
        {
            return GetEquipmentStatBonus("HealthRegen");
        }

        public int GetTotalArmor()
        {
            int totalArmor = 0;
            
            if (Head is HeadItem head) totalArmor += head.GetTotalArmor();
            if (Body is ChestItem chest) totalArmor += chest.GetTotalArmor();
            if (Feet is FeetItem feet) totalArmor += feet.GetTotalArmor();
            
            totalArmor += GetEquipmentStatBonus("Armor");
            
            return totalArmor;
        }

        public int GetTotalRerollCharges()
        {
            int totalRerolls = 0;
            
            var equippedItems = new[] { Head, Body, Weapon, Feet };
            foreach (var item in equippedItems)
            {
                if (item != null)
                {
                    foreach (var modification in item.Modifications)
                    {
                        if (modification.Effect == "reroll")
                        {
                            totalRerolls++;
                        }
                    }
                }
            }
            
            return totalRerolls;
        }

        public int GetModificationMagicFind()
        {
            int totalMagicFind = 0;
            
            var equippedItems = new[] { Head, Body, Weapon, Feet };
            foreach (var item in equippedItems)
            {
                if (item != null)
                {
                    foreach (var modification in item.Modifications)
                    {
                        if (modification.Effect == "magicFind")
                        {
                            totalMagicFind += (int)modification.RolledValue;
                        }
                    }
                }
            }
            
            return totalMagicFind;
        }

        public int GetModificationRollBonus()
        {
            int totalRollBonus = 0;
            
            var equippedItems = new[] { Head, Body, Weapon, Feet };
            foreach (var item in equippedItems)
            {
                if (item != null)
                {
                    foreach (var modification in item.Modifications)
                    {
                        if (modification.Effect == "rollBonus")
                        {
                            totalRollBonus += (int)modification.RolledValue;
                        }
                    }
                }
            }
            
            return totalRollBonus;
        }

        public int GetModificationDamageBonus()
        {
            int totalDamageBonus = 0;
            
            var equippedItems = new[] { Head, Body, Weapon, Feet };
            foreach (var item in equippedItems)
            {
                if (item != null)
                {
                    foreach (var modification in item.Modifications)
                    {
                        if (modification.Effect == "damage")
                        {
                            totalDamageBonus += (int)modification.RolledValue;
                        }
                    }
                }
            }
            
            return totalDamageBonus;
        }

        public double GetModificationSpeedMultiplier()
        {
            double totalSpeedMultiplier = 1.0;
            
            var equippedItems = new[] { Head, Body, Weapon, Feet };
            foreach (var item in equippedItems)
            {
                if (item != null)
                {
                    foreach (var modification in item.Modifications)
                    {
                        if (modification.Effect == "speedMultiplier")
                        {
                            totalSpeedMultiplier *= modification.RolledValue;
                        }
                    }
                }
            }
            
            return totalSpeedMultiplier;
        }

        public double GetModificationDamageMultiplier()
        {
            double totalDamageMultiplier = 1.0;
            
            var equippedItems = new[] { Head, Body, Weapon, Feet };
            foreach (var item in equippedItems)
            {
                if (item != null)
                {
                    foreach (var modification in item.Modifications)
                    {
                        if (modification.Effect == "damageMultiplier")
                        {
                            totalDamageMultiplier *= modification.RolledValue;
                        }
                    }
                }
            }
            
            return totalDamageMultiplier;
        }

        public double GetModificationLifesteal()
        {
            double totalLifesteal = 0.0;
            
            var equippedItems = new[] { Head, Body, Weapon, Feet };
            foreach (var item in equippedItems)
            {
                if (item != null)
                {
                    foreach (var modification in item.Modifications)
                    {
                        if (modification.Effect == "lifesteal")
                        {
                            totalLifesteal += modification.RolledValue;
                        }
                    }
                }
            }
            
            return totalLifesteal;
        }

        public int GetModificationGodlikeBonus()
        {
            int totalGodlikeBonus = 0;
            
            var equippedItems = new[] { Head, Body, Weapon, Feet };
            foreach (var item in equippedItems)
            {
                if (item != null)
                {
                    foreach (var modification in item.Modifications)
                    {
                        if (modification.Effect == "godlike")
                        {
                            totalGodlikeBonus += (int)modification.RolledValue;
                        }
                    }
                }
            }
            
            return totalGodlikeBonus;
        }

        public double GetModificationBleedChance()
        {
            double totalBleedChance = 0.0;
            
            var equippedItems = new[] { Head, Body, Weapon, Feet };
            foreach (var item in equippedItems)
            {
                if (item != null)
                {
                    foreach (var modification in item.Modifications)
                    {
                        if (modification.Effect == "bleedChance")
                        {
                            totalBleedChance += modification.RolledValue;
                        }
                    }
                }
            }
            
            return totalBleedChance;
        }

        public double GetModificationUniqueActionChance()
        {
            double totalUniqueActionChance = 0.0;
            
            var equippedItems = new[] { Head, Body, Weapon, Feet };
            foreach (var item in equippedItems)
            {
                if (item != null)
                {
                    foreach (var modification in item.Modifications)
                    {
                        if (modification.Effect == "uniqueActionChance")
                        {
                            totalUniqueActionChance += modification.RolledValue;
                        }
                    }
                }
            }
            
            return totalUniqueActionChance;
        }

        public double GetArmorSpikeDamage()
        {
            double totalSpikeDamage = 0.0;
            
            var equippedItems = new[] { Head, Body, Weapon, Feet };
            foreach (var item in equippedItems)
            {
                if (item != null)
                {
                    foreach (var armorStatus in item.ArmorStatuses)
                    {
                        if (armorStatus.Effect == "armorSpikes")
                        {
                            totalSpikeDamage += armorStatus.Value;
                        }
                    }
                }
            }
            
            return totalSpikeDamage;
        }

        public List<ArmorStatus> GetEquippedArmorStatuses()
        {
            var allStatuses = new List<ArmorStatus>();
            
            var equippedItems = new[] { Head, Body, Weapon, Feet };
            foreach (var item in equippedItems)
            {
                if (item != null)
                {
                    allStatuses.AddRange(item.ArmorStatuses);
                }
            }
            
            return allStatuses;
        }

        public bool HasAutoSuccess()
        {
            var equippedItems = new[] { Head, Body, Weapon, Feet };
            foreach (var item in equippedItems)
            {
                if (item != null)
                {
                    foreach (var modification in item.Modifications)
                    {
                        if (modification.Effect == "autoSuccess")
                        {
                            return true;
                        }
                    }
                }
            }
            
            return false;
        }

        public List<string> GetGearActions(Item gear)
        {
            var actions = new List<string>();
            
            if (gear is WeaponItem weapon)
            {
                actions.AddRange(GetWeaponActionsFromJson(weapon.WeaponType));
            }
            else if (gear is HeadItem || gear is ChestItem || gear is FeetItem)
            {
                if (HasSpecialArmorActions(gear))
                {
                    if (!string.IsNullOrEmpty(gear.GearAction))
                    {
                        actions.Add(gear.GearAction);
                    }
                    else
                    {
                        actions.AddRange(GetRandomArmorActionFromJson(gear));
                    }
                }
            }
            
            foreach (var actionBonus in gear.ActionBonuses)
            {
                if (!string.IsNullOrEmpty(actionBonus.Name))
                {
                    actions.Add(actionBonus.Name);
                }
            }
            
            return actions;
        }

        private List<string> GetWeaponActionsFromJson(WeaponType weaponType)
        {
            var weaponTag = weaponType.ToString().ToLower();
            
            var allActions = ActionLoader.GetAllActions();
            
            if (weaponType == WeaponType.Mace)
            {
                return new List<string> { "CRUSHING BLOW", "SHIELD BREAK", "THUNDER CLAP" };
            }
            
            var weaponActions = allActions
                .Where(action => action.Tags.Contains("weapon") && 
                                action.Tags.Contains(weaponTag) &&
                                !action.Tags.Contains("unique"))
                .Select(action => action.Name)
                .ToList();
                
            return weaponActions;
        }

        private List<string> GetRandomArmorActionFromJson(Item armor)
        {
            var randomAction = GetRandomArmorActionName();
            if (!string.IsNullOrEmpty(randomAction))
            {
                return new List<string> { randomAction };
            }
            
            var allActions = ActionLoader.GetAllActions();
            
            var armorActions = allActions
                .Where(action => action.Tags.Contains("armor") && 
                                !action.Tags.Contains("environment"))
                .Select(action => action.Name)
                .ToList();

            if (armorActions.Count == 0)
            {
                armorActions = allActions
                    .Where(action => action.IsComboAction && 
                                    !action.Tags.Contains("environment") &&
                                    !action.Tags.Contains("enemy") &&
                                    !action.Tags.Contains("weapon"))
                    .Select(action => action.Name)
                    .ToList();
            }

            if (armorActions.Count > 0)
            {
                var fallbackAction = armorActions[Random.Shared.Next(armorActions.Count)];
                return new List<string> { fallbackAction };
            }
            
            return new List<string>();
        }

        private string? GetRandomArmorActionName()
        {
            var allActions = ActionLoader.GetAllActions();
            var availableActions = allActions
                .Where(action => action.IsComboAction && 
                               !action.Tags.Contains("environment") &&
                               !action.Tags.Contains("enemy") &&
                               !action.Tags.Contains("unique"))
                .Select(action => action.Name)
                .ToList();

            if (availableActions.Count > 0)
            {
                return availableActions[Random.Shared.Next(availableActions.Count)];
            }
            
            return null;
        }

        private bool HasSpecialArmorActions(Item armor)
        {
            if (armor.Modifications.Count > 0)
            {
                return true;
            }
            
            if (armor.StatBonuses.Count > 0)
            {
                return true;
            }
            
            if (armor.ActionBonuses.Count > 0)
            {
                return true;
            }
            
            string[] basicGearNames = { "Leather Helmet", "Leather Armor", "Leather Boots", "Cloth Hood", "Cloth Robes", "Cloth Shoes" };
            if (basicGearNames.Contains(armor.Name))
            {
                return false;
            }
            
            return false;
        }
    }
}
