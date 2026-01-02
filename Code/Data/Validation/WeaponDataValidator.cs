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
