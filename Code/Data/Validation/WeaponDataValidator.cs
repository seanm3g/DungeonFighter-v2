using System;
using System.Collections.Generic;
using System.Linq;

namespace RPGGame.Data.Validation
{
    /// <summary>
    /// Validates Weapon data from Weapons.json
    /// </summary>
    public class WeaponDataValidator : IDataValidator
    {
        private const string FileName = "Weapons.json";

        public ValidationResult Validate()
        {
            var result = new ValidationResult();
            var weapons = JsonLoader.LoadJsonList<WeaponData>(FileName);

            if (weapons == null || weapons.Count == 0)
            {
                result.AddWarning(FileName, "Weapons", "", "No weapons loaded. Cannot validate.");
                return result;
            }

            result.IncrementStatistic("Total Weapons");

            foreach (var weapon in weapons)
            {
                ValidateWeapon(weapon, result);
            }

            return result;
        }

        private void ValidateWeapon(WeaponData weapon, ValidationResult result)
        {
            var entityName = string.IsNullOrEmpty(weapon.Name) ? "<unnamed>" : weapon.Name;

            // Required fields
            if (string.IsNullOrEmpty(weapon.Type))
            {
                result.AddError(FileName, entityName, "type", "Weapon type is required");
            }

            if (string.IsNullOrEmpty(weapon.Name))
            {
                result.AddError(FileName, entityName, "name", "Weapon name is required");
            }

            // Type validation
            if (!string.IsNullOrEmpty(weapon.Type) && 
                !ValidationRules.Weapons.ValidTypes.Contains(weapon.Type))
            {
                result.AddWarning(FileName, entityName, "type", 
                    $"Unknown weapon type '{weapon.Type}'. Valid types: {string.Join(", ", ValidationRules.Weapons.ValidTypes)}");
            }

            if (weapon.Tags != null)
            {
                foreach (var t in weapon.Tags)
                {
                    if (string.IsNullOrWhiteSpace(t))
                    {
                        result.AddWarning(FileName, entityName, "tags", "tags list contains an empty entry");
                        break;
                    }
                }
            }

            // Range checks
            if (!ValidationRules.IsInRange(weapon.BaseDamage, ValidationRules.Weapons.MinBaseDamage, ValidationRules.Weapons.MaxBaseDamage))
            {
                result.AddError(FileName, entityName, "baseDamage", 
                    ValidationRules.FormatRangeError("baseDamage", weapon.BaseDamage, 
                        ValidationRules.Weapons.MinBaseDamage, ValidationRules.Weapons.MaxBaseDamage));
            }

            if (!ValidationRules.IsInRange(weapon.AttackSpeed, ValidationRules.Weapons.MinAttackSpeed, ValidationRules.Weapons.MaxAttackSpeed))
            {
                result.AddError(FileName, entityName, "attackSpeed", 
                    ValidationRules.FormatRangeError("attackSpeed", weapon.AttackSpeed, 
                        ValidationRules.Weapons.MinAttackSpeed, ValidationRules.Weapons.MaxAttackSpeed));
            }

            if (!ValidationRules.IsInRange(weapon.Tier, ValidationRules.Weapons.MinTier, ValidationRules.Weapons.MaxTier))
            {
                result.AddError(FileName, entityName, "tier", 
                    ValidationRules.FormatRangeError("tier", weapon.Tier, 
                        ValidationRules.Weapons.MinTier, ValidationRules.Weapons.MaxTier));
            }

            const int maxDamageBonusBound = 50;
            if (weapon.DamageBonusMin < 0 || weapon.DamageBonusMax < 0)
            {
                result.AddWarning(FileName, entityName, "damageBonus",
                    "damageBonusMin and damageBonusMax should be non-negative (values are clamped at generation).");
            }

            if (weapon.DamageBonusMin > maxDamageBonusBound || weapon.DamageBonusMax > maxDamageBonusBound)
            {
                result.AddWarning(FileName, entityName, "damageBonus",
                    $"damageBonus range unusually high (>{maxDamageBonusBound}); verify sheet values.");
            }

            if (weapon.DamageBonusMax < weapon.DamageBonusMin)
            {
                result.AddWarning(FileName, entityName, "damageBonus",
                    "damageBonusMax is less than damageBonusMin; they are swapped when rolling loot.");
            }

            // Business rules
            if (weapon.BaseDamage <= 0)
            {
                result.AddError(FileName, entityName, "baseDamage", "Base damage must be greater than 0");
            }

            if (weapon.AttackSpeed <= 0)
            {
                result.AddError(FileName, entityName, "attackSpeed", "Attack speed must be greater than 0");
            }
        }
    }
}
