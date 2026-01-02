using System;
using System.Collections.Generic;
using System.Linq;

namespace RPGGame.Data.Validation
{
    /// <summary>
    /// Validates Armor data from Armor.json
    /// </summary>
    public class ArmorDataValidator : IDataValidator
    {
        private const string FileName = "Armor.json";

        public ValidationResult Validate()
        {
            var result = new ValidationResult();
            var armorList = JsonLoader.LoadJsonList<ArmorData>(FileName);

            if (armorList == null || armorList.Count == 0)
            {
                result.AddWarning(FileName, "Armor", "", "No armor loaded. Cannot validate.");
                return result;
            }

            result.IncrementStatistic("Total Armor");

            foreach (var armor in armorList)
            {
                ValidateArmor(armor, result);
            }

            return result;
        }

        private void ValidateArmor(ArmorData armor, ValidationResult result)
        {
            var entityName = string.IsNullOrEmpty(armor.Name) ? "<unnamed>" : armor.Name;

            // Required fields
            if (string.IsNullOrEmpty(armor.Slot))
            {
                result.AddError(FileName, entityName, "slot", "Armor slot is required");
            }

            if (string.IsNullOrEmpty(armor.Name))
            {
                result.AddError(FileName, entityName, "name", "Armor name is required");
            }

            // Slot validation
            if (!string.IsNullOrEmpty(armor.Slot) && 
                !ValidationRules.Armor.ValidSlots.Contains(armor.Slot))
            {
                result.AddWarning(FileName, entityName, "slot", 
                    $"Unknown armor slot '{armor.Slot}'. Valid slots: {string.Join(", ", ValidationRules.Armor.ValidSlots)}");
            }

            // Range checks
            if (!ValidationRules.IsInRange(armor.Armor, ValidationRules.Armor.MinArmorValue, ValidationRules.Armor.MaxArmorValue))
            {
                result.AddError(FileName, entityName, "armor", 
                    ValidationRules.FormatRangeError("armor", armor.Armor, 
                        ValidationRules.Armor.MinArmorValue, ValidationRules.Armor.MaxArmorValue));
            }

            if (!ValidationRules.IsInRange(armor.Tier, ValidationRules.Armor.MinTier, ValidationRules.Armor.MaxTier))
            {
                result.AddError(FileName, entityName, "tier", 
                    ValidationRules.FormatRangeError("tier", armor.Tier, 
                        ValidationRules.Armor.MinTier, ValidationRules.Armor.MaxTier));
            }
        }
    }
}
