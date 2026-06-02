using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using RPGGame.Data;

namespace RPGGame.UI.Avalonia.Renderers.Helpers
{
    /// <summary>
    /// One mechanical line shown on item tooltips (catalog, affix, or suffix).
    /// </summary>
    public readonly struct ItemStatContribution
    {
        public ItemStatContribution(string label, string valueText, string? sourceTag = null, bool isDebuff = false)
        {
            Label = label ?? "";
            ValueText = valueText ?? "";
            SourceTag = sourceTag;
            IsDebuff = isDebuff;
        }

        public string Label { get; }
        public string ValueText { get; }
        /// <summary>Affix or suffix name when the line comes from a roll (e.g. Swift, of Protection).</summary>
        public string? SourceTag { get; }
        public bool IsDebuff { get; }

        public string ToPlainLine()
        {
            if (string.IsNullOrEmpty(SourceTag))
                return $"{Label}: {ValueText}";
            return $"{SourceTag} — {Label}: {ValueText}";
        }
    }

    /// <summary>
    /// Aggregates every stat this item modifies for tooltip and inventory stat lines.
    /// </summary>
    public static class ItemStatContributionCollector
    {
        public static List<ItemStatContribution> Collect(Item item)
        {
            var list = new List<ItemStatContribution>();
            if (item == null)
                return list;

            if (item.Type == ItemType.Consumable && item.RoomSearchConsumableKind != RoomSearchConsumableKind.None)
            {
                if (item.RoomSearchConsumableKind == RoomSearchConsumableKind.Food)
                    list.Add(new ItemStatContribution("Use", $"restores up to {Math.Max(1, item.ConsumableHealAmount)} HP"));
                else
                    list.Add(new ItemStatContribution("Use", $"dungeon buff (+{Math.Max(1, item.ConsumablePotionPotency)})"));
                return list;
            }

            if (item is WeaponItem weapon)
            {
                list.Add(new ItemStatContribution("Damage", weapon.GetTotalDamage().ToString(CultureInfo.InvariantCulture)));
                list.Add(new ItemStatContribution("Attack speed", $"{weapon.GetTotalAttackSpeed().ToString("F2", CultureInfo.InvariantCulture)}×"));
            }
            else
            {
                int? armor = item switch
                {
                    HeadItem h => h.GetTotalArmor(),
                    ChestItem c => c.GetTotalArmor(),
                    LegsItem l => l.GetTotalArmor(),
                    FeetItem f => f.GetTotalArmor(),
                    _ => null
                };
                if (armor.HasValue)
                    list.Add(new ItemStatContribution("Armor", $"+{armor.Value}"));
            }

            AddCatalogAttribute(list, item, item.BaseStrength, "Strength");
            AddCatalogAttribute(list, item, item.BaseAgility, "Agility");
            AddCatalogAttribute(list, item, item.BaseTechnique, "Technique");
            AddCatalogAttribute(list, item, item.BaseIntelligence, "Intelligence");
            AddCatalogAttribute(list, item, item.BaseHit, "Hit threshold");
            AddCatalogAttribute(list, item, item.BaseCombo, "Combo threshold");
            AddCatalogAttribute(list, item, item.BaseCrit, "Crit threshold");

            if (Math.Abs(item.CatalogAttackSpeed) > 1e-9)
                list.Add(new ItemStatContribution("Attack speed", $"+{item.CatalogAttackSpeed:F3}"));

            int actionSlots = ItemStatFormatter.GetActionSlotDisplayTotalPublic(item);
            if (item is FeetItem || actionSlots > 0)
                list.Add(new ItemStatContribution("Action slots", $"+{actionSlots}"));

            if (item.Modifications != null)
            {
                foreach (var mod in item.Modifications)
                {
                    if (mod == null)
                        continue;
                    TryAddModification(list, mod);
                }
            }

            double quality = ItemPrefixHelper.GetGearPrimaryStatMultiplier(item);
            if (Math.Abs(quality - 1.0) > 0.001)
            {
                string pct = quality > 1.0
                    ? $"+{(quality - 1.0) * 100:F0}% armor, damage, speed"
                    : $"−{(1.0 - quality) * 100:F0}% armor, damage, speed";
                list.Add(new ItemStatContribution("Quality", pct, sourceTag: null, isDebuff: quality < 1.0));
            }

            if (item.StatBonuses != null)
            {
                foreach (var bonus in item.StatBonuses)
                {
                    if (bonus == null)
                        continue;
                    foreach (var (contribType, contribValue) in bonus.EnumerateContributions())
                    {
                        if (Math.Abs(contribValue) < 1e-9)
                            continue;
                        string label = GetDisplayLabel(contribType);
                        string value = FormatSuffixValue(contribType, contribValue);
                        string? tag = string.IsNullOrEmpty(bonus.Name) ? null : bonus.Name;
                        list.Add(new ItemStatContribution(label, value, tag, isDebuff: contribValue < 0));
                    }
                }
            }

            return list;
        }

        private static void AddCatalogAttribute(List<ItemStatContribution> list, Item item, int value, string label)
        {
            if (value == 0)
                return;
            string sign = value > 0 ? "+" : "";
            list.Add(new ItemStatContribution(label, $"{sign}{value}", sourceTag: "Item base", isDebuff: value < 0));
        }

        private static void TryAddModification(List<ItemStatContribution> list, Modification mod)
        {
            string effect = (mod.Effect ?? "").Trim();
            if (effect.Length == 0)
                return;

            if (string.Equals(effect, "gearPrimaryStatMultiplier", StringComparison.OrdinalIgnoreCase))
                return;

            if (effect.Contains("armor", StringComparison.OrdinalIgnoreCase))
            {
                int v = (int)Math.Round(mod.RolledValue);
                if (v != 0)
                    list.Add(new ItemStatContribution("Armor", $"+{v}", mod.Name, isDebuff: v < 0));
                return;
            }

            string? label = GetModificationLabel(effect);
            if (label == null)
            {
                list.Add(new ItemStatContribution(effect, mod.RolledValue.ToString("0.##", CultureInfo.InvariantCulture), mod.Name,
                    isDebuff: mod.RolledValue < 0));
                return;
            }

            string? valueText = FormatModificationValue(effect, mod.RolledValue);
            if (valueText == null)
                return;

            list.Add(new ItemStatContribution(label, valueText, mod.Name, isDebuff: mod.RolledValue < 0 && !IsMultiplierEffect(effect)));
        }

        private static bool IsMultiplierEffect(string effect) =>
            effect.Contains("Multiplier", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(effect, "gearPrimaryStatMultiplier", StringComparison.OrdinalIgnoreCase);

        private static string? GetModificationLabel(string effect)
        {
            string e = effect.Trim();
            return e.ToUpperInvariant() switch
            {
                "STR" or "EQUIPMENTSTR" or "STRENGTH" => "Strength",
                "AGI" or "EQUIPMENTAGI" or "AGILITY" => "Agility",
                "TEC" or "EQUIPMENTTEC" or "TECHNIQUE" or "TECH" => "Technique",
                "INT" or "EQUIPMENTINT" or "INTELLIGENCE" => "Intelligence",
                "HIT" => "Hit threshold",
                "COMBO" => "Combo threshold",
                "CRIT" => "Crit threshold",
                "ARMOR" => "Armor",
                "SPEED" => "Attack speed",
                "MULTI-HIT" or "MULTIHIT" => "Extra hits",
                "MAGIC FIND" or "MAGICFIND" => "Magic find",
                "HEALTH BONUS" or "HEALTH" => "Max health",
                "AMP" => "Combo amp",
                "DAMAGE" => "Damage",
                "ROLLBONUS" => "Roll bonus",
                "ACCURACY" => "Accuracy",
                _ => null
            };
        }

        private static string? FormatModificationValue(string effect, double rolled)
        {
            string e = effect.Trim();
            if (string.Equals(e, "gearPrimaryStatMultiplier", StringComparison.OrdinalIgnoreCase))
                return null;

            if (e.Contains("armor", StringComparison.OrdinalIgnoreCase))
                return $"+{(int)Math.Round(rolled)}";

            if (string.Equals(e, "SPEED", StringComparison.OrdinalIgnoreCase))
            {
                if (Math.Abs(rolled) < 1e-9)
                    return null;
                if (rolled < 1.0)
                    return $"{(1.0 - rolled) * 100:F0}% faster";
                return $"{(rolled - 1.0) * 100:F0}% slower";
            }

            if (string.Equals(e, "damageMultiplier", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(e, "speedMultiplier", StringComparison.OrdinalIgnoreCase))
            {
                double pct = (rolled - 1.0) * 100.0;
                return $"{(pct >= 0 ? "+" : "")}{pct:F0}%";
            }

            if (IsAttributeOrThresholdEffect(e))
            {
                int v = (int)Math.Round(rolled);
                return v >= 0 ? $"+{v}" : v.ToString(CultureInfo.InvariantCulture);
            }

            if (rolled >= 0)
                return $"+{rolled:0.##}";
            return rolled.ToString("0.##", CultureInfo.InvariantCulture);
        }

        private static bool IsAttributeOrThresholdEffect(string effect)
        {
            string u = effect.Replace(" ", "", StringComparison.Ordinal).ToUpperInvariant();
            return u is "STR" or "STRENGTH" or "EQUIPMENTSTR" or "AGI" or "AGILITY" or "EQUIPMENTAGI" or
                "TEC" or "TECH" or "TECHNIQUE" or "EQUIPMENTTEC" or "INT" or "INTELLIGENCE" or "EQUIPMENTINT" or
                "HIT" or "COMBO" or "CRIT" or "ARMOR" or "MULTIHIT" or "MULTI-HIT" or "MAGICFIND" or "ROLLBONUS" or
                "ACCURACY" or "DAMAGE" or "AMP" or "HEALTH" or "HEALTHBONUS";
        }

        private static string FormatSuffixValue(string contribType, double value)
        {
            string norm = NormalizeContribType(contribType);
            bool literal = IsLiteralSuffixStat(norm);
            if (literal)
            {
                int v = (int)Math.Round(value);
                return v >= 0 ? $"+{v}" : v.ToString(CultureInfo.InvariantCulture);
            }

            string sign = value >= 0 ? "+" : "";
            return $"{sign}{value:0.##}%";
        }

        private static bool IsLiteralSuffixStat(string normalized)
        {
            if (string.IsNullOrEmpty(normalized))
                return false;
            string n = normalized.Trim();
            if (string.Equals(n, "HIT", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(n, "COMBO", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(n, "CRIT", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(n, "ACCURACY", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(n, "RollBonus", StringComparison.OrdinalIgnoreCase))
                return true;
            string noSpace = n.Replace(" ", "", StringComparison.Ordinal).Replace("_", "", StringComparison.Ordinal);
            return string.Equals(noSpace, "CRITMISS", StringComparison.OrdinalIgnoreCase);
        }

        private static string NormalizeContribType(string raw)
        {
            if (string.IsNullOrWhiteSpace(raw))
                return "";
            string n = JsonArraySheetConverter.NormalizeStatBonusSheetStatType(raw.Trim());
            if (string.Equals(n, "TECH", StringComparison.OrdinalIgnoreCase))
                return "TEC";
            string u = n.Replace(" ", "", StringComparison.Ordinal).ToUpperInvariant();
            if (u is "HEALTHBONUS" or "HP" or "HPBONUS")
                return "Health";
            if (u is "BASEDAMAGE" or "WEAPONDAMAGE")
                return "Damage";
            if (n.Contains("ACTION", StringComparison.OrdinalIgnoreCase) &&
                n.Contains("SLOT", StringComparison.OrdinalIgnoreCase))
                return "ExtraActionSlots";
            return n;
        }

        private static string GetDisplayLabel(string contribType)
        {
            string norm = NormalizeContribType(contribType);
            return norm.ToUpperInvariant() switch
            {
                "STR" or "STRENGTH" => "Strength",
                "AGI" or "AGILITY" => "Agility",
                "TEC" or "TECH" or "TECHNIQUE" => "Technique",
                "INT" or "INTELLIGENCE" => "Intelligence",
                "HIT" => "Hit threshold",
                "COMBO" => "Combo threshold",
                "CRIT" => "Crit threshold",
                "ARMOR" => "Armor",
                "HEALTH" => "Max health",
                "HEALTHREGEN" => "Health regen",
                "MAGICFIND" => "Magic find",
                "DAMAGE" or "BASEDAMAGE" or "WEAPONDAMAGE" => "Damage",
                "ATTACKSPEED" or "SPEED" => "Attack speed",
                "EXTRAACTIONSLOTS" => "Action slots",
                "ROLLBONUS" => "Roll bonus",
                "ACCURACY" => "Accuracy",
                _ => norm
            };
        }
    }
}
