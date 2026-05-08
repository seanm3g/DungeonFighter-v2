using System;
using System.Collections.Generic;
using RPGGame.Data;

namespace RPGGame
{
    /// <summary>
    /// Calculates stat bonuses provided by equipped items.
    /// Suffix lines from <c>StatBonuses.json</c> (<see cref="Item.StatBonuses"/>) are usually <b>percent of a reference total</b> on the
    /// character when <see cref="GetStatBonus"/> / <see cref="GetStatBonusDouble"/> receive a <see cref="Character"/> context
    /// (e.g. 5 ⇒ +5% of reference STR). Dice/threshold modifiers stay <b>literal</b>: ACCURACY, HIT, COMBO, CRIT, CRIT MISS, RollBonus
    /// (sheet <c>ROLL</c>) — a value of 3 means +3, not +3%. <c>ALL</c> uses the same rule per target stat (flat for those keys, % for others).
    /// Without a character context, suffix values sum as flat integers (legacy tests and tooling).
    /// </summary>
    public class EquipmentBonusCalculator
    {
        private readonly EquipmentSlotManager slots;

        public EquipmentBonusCalculator(EquipmentSlotManager slots)
        {
            this.slots = slots;
        }

        /// <inheritdoc cref="GetStatBonus"/>
        public int GetStatBonus(string statType) => GetStatBonus(statType, null);

        /// <param name="characterContext">When set, most suffix values are percentages of reference; roll/threshold stats stay literal (see class summary).</param>
        public int GetStatBonus(string statType, Character? characterContext)
        {
            string t = NormalizeEquipmentStatKey(statType);
            int flat = GetFlatEquipmentStatInteger(t);
            if (characterContext == null)
                return flat + SumSuffixStatBonusFlatInteger(t);

            if (IsLiteralRollModifierSuffixStat(t))
                return flat + SumSuffixStatBonusFlatInteger(t);

            double pct = SumSuffixPercentTotal(characterContext, t);
            if (pct <= 0)
                return flat;

            double reference = ResolveSuffixPercentReference(characterContext, t);
            int fromPercent = (int)Math.Floor(reference * (pct / 100.0) + 1e-9);
            return flat + fromPercent;
        }

        /// <inheritdoc cref="GetStatBonusDouble"/>
        public double GetStatBonusDouble(string statType) => GetStatBonusDouble(statType, null);

        /// <param name="characterContext">When set, most suffix values are percentages of reference; roll/threshold stats stay literal.</param>
        public double GetStatBonusDouble(string statType, Character? characterContext)
        {
            string t = NormalizeEquipmentStatKey(statType);
            double flat = GetFlatEquipmentStatDouble(t);
            if (characterContext == null)
                return flat + SumSuffixStatBonusFlatDouble(t);

            if (IsLiteralRollModifierSuffixStat(t))
                return flat + SumSuffixStatBonusFlatDouble(t);

            double pct = SumSuffixPercentTotal(characterContext, t);
            if (pct <= 0)
                return flat;

            double reference = ResolveSuffixPercentReference(characterContext, t);
            return flat + reference * (pct / 100.0);
        }

        /// <summary>Catalog armor on equipped head/chest/legs/feet (excludes suffix-only armor bonuses).</summary>
        public int GetIntrinsicEquippedArmorTotal()
        {
            int totalArmor = 0;
            if (slots.Head is HeadItem head)
                totalArmor += head.GetTotalArmor();
            if (slots.Body is ChestItem chest)
                totalArmor += chest.GetTotalArmor();
            if (slots.Legs is LegsItem legs)
                totalArmor += legs.GetTotalArmor();
            if (slots.Feet is FeetItem feet)
                totalArmor += feet.GetTotalArmor();
            return totalArmor;
        }

        /// <summary>STR/AGI/TEC/INT/HIT/COMBO/CRIT/Armor catalog fields and material mods only — no <see cref="Item.StatBonuses"/>.</summary>
        public int GetFlatStatBonusExcludingSuffixes(string statType) =>
            GetFlatEquipmentStatInteger(NormalizeEquipmentStatKey(statType));

        /// <summary>Includes <see cref="Item.CatalogAttackSpeed"/> when <paramref name="statType"/> is attack speed.</summary>
        public double GetFlatStatBonusExcludingSuffixesDouble(string statType) =>
            GetFlatEquipmentStatDouble(NormalizeEquipmentStatKey(statType));

        public int GetDamageBonus(Character? characterContext) => GetStatBonus("Damage", characterContext);

        public int GetHealthBonus(Character? characterContext) => GetStatBonus("Health", characterContext);

        public int GetRollBonus(Character? characterContext) => GetStatBonus("RollBonus", characterContext);

        public double GetAttackSpeedBonus(Character? characterContext) => GetStatBonusDouble("AttackSpeed", characterContext);

        public int GetHealthRegenBonus(Character? characterContext) => GetStatBonus("HealthRegen", characterContext);

        public int GetTotalArmor(Character? characterContext)
        {
            int totalArmor = GetIntrinsicEquippedArmorTotal();
            totalArmor += GetStatBonus("Armor", characterContext);
            return totalArmor;
        }

        public int GetTotalRerollCharges()
        {
            int totalRerolls = 0;
            var equippedItems = slots.GetEquippedItems();
            foreach (var item in equippedItems)
            {
                if (item != null)
                {
                    foreach (var modification in item.Modifications)
                    {
                        if (modification.Effect == "reroll")
                            totalRerolls++;
                    }
                }
            }

            return totalRerolls;
        }

        public bool HasAutoSuccess()
        {
            var equippedItems = slots.GetEquippedItems();
            foreach (var item in equippedItems)
            {
                if (item != null)
                {
                    foreach (var modification in item.Modifications)
                    {
                        if (modification.Effect == "autoSuccess")
                            return true;
                    }
                }
            }

            return false;
        }

        private int GetModificationBonusInt(string effectType)
        {
            int totalBonus = 0;
            var equippedItems = slots.GetEquippedItems();
            foreach (var item in equippedItems)
            {
                if (item != null)
                {
                    foreach (var modification in item.Modifications)
                    {
                        if (modification.Effect == effectType)
                            totalBonus += (int)modification.RolledValue;
                    }
                }
            }

            return totalBonus;
        }

        private static string NormalizeEquipmentStatKey(string? statType)
        {
            if (string.IsNullOrWhiteSpace(statType))
                return "";
            return NormalizeAffixContributionType(statType.Trim());
        }

        /// <summary>
        /// Dice / threshold modifier keys from suffix tables: numeric values are flat (+2 ⇒ +2), not percent of a reference.
        /// </summary>
        private static bool IsLiteralRollModifierSuffixStat(string normalizedStat)
        {
            if (string.IsNullOrWhiteSpace(normalizedStat))
                return false;

            string n = normalizedStat.Trim();
            if (string.Equals(n, "HIT", StringComparison.OrdinalIgnoreCase))
                return true;
            if (string.Equals(n, "COMBO", StringComparison.OrdinalIgnoreCase))
                return true;
            if (string.Equals(n, "CRIT", StringComparison.OrdinalIgnoreCase))
                return true;
            if (string.Equals(n, "ACCURACY", StringComparison.OrdinalIgnoreCase))
                return true;
            if (string.Equals(n, "RollBonus", StringComparison.OrdinalIgnoreCase))
                return true;

            string noSpace = n.Replace(" ", "", StringComparison.Ordinal).Replace("_", "", StringComparison.Ordinal);
            return string.Equals(noSpace, "CRITMISS", StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>Aligns suffix/mechanic stat labels with keys used in <see cref="BaseCatalogStatBonus"/> / equipment code.</summary>
        private static string NormalizeAffixContributionType(string raw)
        {
            if (string.IsNullOrWhiteSpace(raw))
                return "";

            string n = JsonArraySheetConverter.NormalizeStatBonusSheetStatType(raw.Trim());
            if (string.Equals(n, "TECH", StringComparison.OrdinalIgnoreCase))
                return "TEC";

            string u = n.Replace(" ", "", StringComparison.Ordinal).ToUpperInvariant();
            if (u is "BASEDAMAGE" or "WEAPONDAMAGE")
                return "Damage";

            if (n.Contains("ACTION", StringComparison.OrdinalIgnoreCase) &&
                n.Contains("SLOT", StringComparison.OrdinalIgnoreCase))
                return "ExtraActionSlots";

            return n;
        }

        private static bool ContributionMatches(string normalizedContrib, string normalizedRequested)
        {
            if (string.IsNullOrEmpty(normalizedContrib))
                return false;
            if (string.Equals(normalizedContrib, "ALL", StringComparison.OrdinalIgnoreCase))
                return true;
            return string.Equals(normalizedContrib, normalizedRequested, StringComparison.OrdinalIgnoreCase);
        }

        private int SumSuffixStatBonusFlatInteger(string normalizedStat)
        {
            int sum = 0;
            foreach (var item in slots.GetEquippedItems())
            {
                if (item == null)
                    continue;
                foreach (var statBonus in item.StatBonuses)
                {
                    foreach (var (contribType, contribValue) in statBonus.EnumerateContributions())
                    {
                        string ct = NormalizeAffixContributionType(contribType);
                        if (ContributionMatches(ct, normalizedStat))
                            sum += (int)contribValue;
                    }
                }
            }

            return sum;
        }

        private double SumSuffixStatBonusFlatDouble(string normalizedStat)
        {
            double sum = 0;
            foreach (var item in slots.GetEquippedItems())
            {
                if (item == null)
                    continue;
                foreach (var statBonus in item.StatBonuses)
                {
                    foreach (var (contribType, contribValue) in statBonus.EnumerateContributions())
                    {
                        string ct = NormalizeAffixContributionType(contribType);
                        if (ContributionMatches(ct, normalizedStat))
                            sum += contribValue;
                    }
                }
            }

            return sum;
        }

        private double SumSuffixPercentTotal(Character character, string normalizedStat)
        {
            double pct = 0;
            foreach (var item in slots.GetEquippedItems())
            {
                if (item == null)
                    continue;
                foreach (var statBonus in item.StatBonuses)
                {
                    foreach (var (contribType, contribValue) in statBonus.EnumerateContributions())
                    {
                        string ct = NormalizeAffixContributionType(contribType);
                        if (ContributionMatches(ct, normalizedStat))
                            pct += contribValue;
                    }
                }
            }

            return pct;
        }

        private double ResolveSuffixPercentReference(Character character, string norm)
        {
            if (string.IsNullOrEmpty(norm))
                return 0;

            var eq = character.Equipment;

            if (string.Equals(norm, "STR", StringComparison.OrdinalIgnoreCase))
            {
                // Godlike is added once in CharacterStats.GetEffectiveStrength(..., godlike); do not include it in % reference.
                return character.Stats.Strength + character.Stats.TempStrengthBonus
                    + eq.GetFlatEquipmentStatExcludingSuffixes("STR");
            }

            if (string.Equals(norm, "AGI", StringComparison.OrdinalIgnoreCase))
            {
                return character.Stats.Agility + character.Stats.TempAgilityBonus
                    + eq.GetFlatEquipmentStatExcludingSuffixes("AGI");
            }

            if (string.Equals(norm, "TEC", StringComparison.OrdinalIgnoreCase))
            {
                return character.Stats.Technique + character.Stats.TempTechniqueBonus
                    + eq.GetFlatEquipmentStatExcludingSuffixes("TEC");
            }

            if (string.Equals(norm, "INT", StringComparison.OrdinalIgnoreCase))
            {
                return character.Stats.Intelligence + character.Stats.TempIntelligenceBonus
                    + eq.GetFlatEquipmentStatExcludingSuffixes("INT");
            }

            if (string.Equals(norm, "HIT", StringComparison.OrdinalIgnoreCase))
                return eq.GetFlatEquipmentStatExcludingSuffixes("HIT");

            if (string.Equals(norm, "COMBO", StringComparison.OrdinalIgnoreCase))
                return eq.GetFlatEquipmentStatExcludingSuffixes("COMBO");

            if (string.Equals(norm, "CRIT", StringComparison.OrdinalIgnoreCase))
                return eq.GetFlatEquipmentStatExcludingSuffixes("CRIT");

            if (string.Equals(norm, "Health", StringComparison.OrdinalIgnoreCase))
            {
                return character.Health.MaxHealth + eq.GetFlatEquipmentStatExcludingSuffixes("Health");
            }

            if (string.Equals(norm, "Armor", StringComparison.OrdinalIgnoreCase))
                return GetIntrinsicEquippedArmorTotal();

            if (string.Equals(norm, "Damage", StringComparison.OrdinalIgnoreCase))
            {
                int strLike = character.Stats.Strength + character.Stats.TempStrengthBonus
                    + eq.GetFlatEquipmentStatExcludingSuffixes("STR");
                int weaponDmg = character.Weapon is WeaponItem w ? w.GetTotalDamage() : 0;
                return strLike + weaponDmg + eq.GetModificationDamageBonus();
            }

            if (string.Equals(norm, "RollBonus", StringComparison.OrdinalIgnoreCase))
            {
                int piece = eq.GetModificationRollBonus() + eq.GetFlatEquipmentStatExcludingSuffixes("RollBonus");
                int baseline = Math.Max(1, character.Progression.Level * 5);
                return Math.Max(baseline, piece);
            }

            if (string.Equals(norm, "MagicFind", StringComparison.OrdinalIgnoreCase))
            {
                int piece = eq.GetModificationMagicFind() + eq.GetFlatEquipmentStatExcludingSuffixes("MagicFind");
                int baseline = Math.Max(1, character.Progression.Level * 3);
                return Math.Max(baseline, piece);
            }

            if (string.Equals(norm, "HealthRegen", StringComparison.OrdinalIgnoreCase))
            {
                int piece = eq.GetFlatEquipmentStatExcludingSuffixes("HealthRegen");
                int baseline = Math.Max(1, character.Health.MaxHealth / 20);
                return Math.Max(baseline, piece);
            }

            if (string.Equals(norm, "AttackSpeed", StringComparison.OrdinalIgnoreCase))
            {
                double bt = GameConfiguration.Instance?.Combat?.BaseAttackTime ?? 0;
                if (bt <= 0 || double.IsNaN(bt) || double.IsInfinity(bt))
                    bt = 2.0;
                return bt;
            }

            if (string.Equals(norm, "ExtraActionSlots", StringComparison.OrdinalIgnoreCase))
            {
                int catalog = 0;
                foreach (var item in slots.GetEquippedItems())
                {
                    if (item != null)
                        catalog += Math.Max(0, item.ExtraActionSlots);
                }

                int baseline = GameConfiguration.Instance?.LootSystem?.ComboSequenceBaseMax ?? 2;
                if (baseline < 1)
                    baseline = 1;
                return Math.Max(baseline, catalog);
            }

            return 0;
        }

        private int GetFlatEquipmentStatInteger(string statType)
        {
            int totalBonus = 0;
            var equippedItems = slots.GetEquippedItems();
            foreach (var item in equippedItems)
            {
                if (item == null)
                    continue;
                totalBonus += BaseCatalogStatBonus(item, statType);
                foreach (var modification in item.Modifications)
                    totalBonus += MaterialStatBonus(modification, statType);
            }

            return totalBonus;
        }

        private double GetFlatEquipmentStatDouble(string statType)
        {
            if (!string.Equals(statType, "AttackSpeed", StringComparison.OrdinalIgnoreCase))
                return GetFlatEquipmentStatInteger(statType);

            double totalBonus = 0;
            var equippedItems = slots.GetEquippedItems();
            foreach (var item in equippedItems)
            {
                if (item == null)
                    continue;
                totalBonus += item.CatalogAttackSpeed;
                totalBonus += BaseCatalogStatBonus(item, statType);
                foreach (var modification in item.Modifications)
                    totalBonus += MaterialStatBonus(modification, statType);
            }

            return totalBonus;
        }

        private static int BaseCatalogStatBonus(Item item, string statType)
        {
            string t = (statType ?? "").Trim();
            if (string.Equals(t, "STR", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(t, "STRENGTH", StringComparison.OrdinalIgnoreCase))
                return item.BaseStrength;
            if (string.Equals(t, "AGI", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(t, "AGILITY", StringComparison.OrdinalIgnoreCase))
                return item.BaseAgility;
            if (string.Equals(t, "TEC", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(t, "TECHNIQUE", StringComparison.OrdinalIgnoreCase))
                return item.BaseTechnique;
            if (string.Equals(t, "INT", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(t, "INTELLIGENCE", StringComparison.OrdinalIgnoreCase))
                return item.BaseIntelligence;
            if (string.Equals(t, "HIT", StringComparison.OrdinalIgnoreCase))
                return item.BaseHit;
            if (string.Equals(t, "COMBO", StringComparison.OrdinalIgnoreCase))
                return item.BaseCombo;
            if (string.Equals(t, "CRIT", StringComparison.OrdinalIgnoreCase))
                return item.BaseCrit;
            if (string.Equals(t, "ExtraActionSlots", StringComparison.OrdinalIgnoreCase))
                return Math.Max(0, item.ExtraActionSlots);
            return 0;
        }

        private static int MaterialStatBonus(Modification modification, string statType)
        {
            string eff = modification.Effect ?? "";
            return eff switch
            {
                "equipmentStr" when string.Equals(statType, "STR", StringComparison.OrdinalIgnoreCase) =>
                    (int)Math.Round(modification.RolledValue),
                "equipmentAgi" when string.Equals(statType, "AGI", StringComparison.OrdinalIgnoreCase) =>
                    (int)Math.Round(modification.RolledValue),
                "equipmentTec" when string.Equals(statType, "TEC", StringComparison.OrdinalIgnoreCase) =>
                    (int)Math.Round(modification.RolledValue),
                "equipmentInt" when string.Equals(statType, "INT", StringComparison.OrdinalIgnoreCase) =>
                    (int)Math.Round(modification.RolledValue),
                // Prefix rows in Modifications.json (e.g. Swift → HIT, Balanced → COMBO) use these effect keys.
                "HIT" when string.Equals(statType, "HIT", StringComparison.OrdinalIgnoreCase) =>
                    (int)Math.Round(modification.RolledValue),
                "COMBO" when string.Equals(statType, "COMBO", StringComparison.OrdinalIgnoreCase) =>
                    (int)Math.Round(modification.RolledValue),
                "CRIT" when string.Equals(statType, "CRIT", StringComparison.OrdinalIgnoreCase) =>
                    (int)Math.Round(modification.RolledValue),
                _ => 0
            };
        }
    }
}
