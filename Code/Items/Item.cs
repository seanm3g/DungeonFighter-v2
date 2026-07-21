using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Serialization;

namespace RPGGame
{
    public enum ItemType
    {
        Head,
        Feet,
        Chest,
        Weapon,
        /// <summary>Leg armor slot; appended last so legacy numeric enum values in saves stay stable.</summary>
        Legs,
        /// <summary>Room food/potions from search; not equipment.</summary>
        Consumable
    }

    public enum WeaponType
    {
        Sword,
        Dagger,
        Mace,
        Wand
    }
    
    public enum ItemRarity
    {
        Common,
        Uncommon,
        Rare,
        Epic,
        Legendary
    }

    /// <summary>One line inside a multi-mechanic suffix (from <c>StatBonuses.json</c> <c>Mechanics</c> array).</summary>
    public class StatBonusMechanic
    {
        public string StatType { get; set; } = "";
        public double Value { get; set; }
    }

    public class StatBonus
    {
        public string Name { get; set; } = "";
        public string Description { get; set; } = "";
        public double Value { get; set; } = 0;

        /// <summary>Affix-pool tier for loot rolls (Common, Uncommon, …). Matched case-insensitively to rolled tier from <see cref="RarityData"/>.</summary>
        public string Rarity { get; set; } = "Common";

        public string StatType { get; set; } = ""; // Which stat this affects (STR, AGI, TEC, INT, Health, Armor, etc.)

        /// <summary>Optional loot/lab catalog rarity (same vocabulary as <see cref="Modification.ItemRank"/>). Empty = pool-wide.</summary>
        [JsonPropertyName("ItemRank")]
        public string ItemRank { get; set; } = "";

        /// <summary>When non-empty, each entry contributes stats; otherwise legacy <see cref="StatType"/> / <see cref="Value"/> are used.</summary>
        public List<StatBonusMechanic>? Mechanics { get; set; }

        /// <summary>
        /// Attribute thresholds an equipping character must meet for the piece carrying this suffix.
        /// Authored as <c>[strength:5,primary:15]</c>; keys are the four core attributes plus the dynamic categories
        /// (<c>primary</c> / <c>secondary</c> / <c>neglected</c> / <c>weakness</c>) resolved at equip-check time via
        /// <see cref="DynamicAttributeCategoryResolver"/>. Folded into <see cref="Item.AttributeRequirements"/> by
        /// <see cref="Item.RecomputeAttributeRequirementsIncludingModifications"/>.
        /// </summary>
        [JsonPropertyName("Requirements")]
        public Dictionary<string, int>? Requirements { get; set; }

        /// <summary>True when the suffix lists at least one parsed requirement entry.</summary>
        [JsonIgnore]
        public bool HasRequirements => Requirements != null && Requirements.Count > 0;

        /// <summary>Yields each stat contribution for equipment / UI (legacy single-line when <see cref="Mechanics"/> is null or empty).</summary>
        public IEnumerable<(string StatType, double Value)> EnumerateContributions()
        {
            if (Mechanics != null && Mechanics.Count > 0)
            {
                foreach (var m in Mechanics)
                {
                    if (m != null && !string.IsNullOrEmpty(m.StatType))
                        yield return (m.StatType, m.Value);
                }

                yield break;
            }

            if (!string.IsNullOrEmpty(StatType))
                yield return (StatType, Value);
        }

        /// <summary>Sums numeric contributions matching <paramref name="statType"/> (case-insensitive).</summary>
        public int SumContributionValuesForStatType(string statType)
        {
            int sum = 0;
            foreach (var (t, v) in EnumerateContributions())
            {
                if (string.Equals(t, statType, StringComparison.OrdinalIgnoreCase))
                    sum += (int)v;
            }

            return sum;
        }

        /// <summary>Deep-enough copy for attaching loot rows to an item without mutating <see cref="LootDataCache"/> templates.</summary>
        public StatBonus CloneForItemInstance()
        {
            List<StatBonusMechanic>? mechCopy = null;
            if (Mechanics != null && Mechanics.Count > 0)
            {
                mechCopy = new List<StatBonusMechanic>(Mechanics.Count);
                foreach (var m in Mechanics)
                {
                    if (m == null)
                        continue;
                    mechCopy.Add(new StatBonusMechanic { StatType = m.StatType, Value = m.Value });
                }
            }

            Dictionary<string, int>? reqCopy = null;
            if (Requirements != null && Requirements.Count > 0)
            {
                reqCopy = new Dictionary<string, int>(Requirements.Count, StringComparer.OrdinalIgnoreCase);
                foreach (var kv in Requirements)
                {
                    if (string.IsNullOrWhiteSpace(kv.Key))
                        continue;
                    reqCopy[kv.Key] = kv.Value;
                }
            }

            return new StatBonus
            {
                Name = Name,
                Description = Description,
                Value = Value,
                Rarity = Rarity,
                StatType = StatType,
                ItemRank = ItemRank,
                Mechanics = mechCopy,
                Requirements = reqCopy
            };
        }
    }

    public class ActionBonus
    {
        public string Name { get; set; } = "";
        public string Description { get; set; } = "";
        public int Weight { get; set; } = 0;
    }

    public class Modification
    {
        public int DiceResult { get; set; } = 0;
        public string ItemRank { get; set; } = "";
        /// <summary>Loot prefix slot: Adjective (default), Material, or Quality. Omitted in JSON = Adjective.</summary>
        [JsonPropertyName("prefixCategory")]
        public string PrefixCategory { get; set; } = "";

        public string Name { get; set; } = "";
        public string Description { get; set; } = "";
        public string Effect { get; set; } = "";
        /// <summary>
        /// Optional combat WHEN token for proc mods (e.g. ONCRITICAL, ONCONNECT, ONKILL).
        /// When blank, <see cref="Effect"/> defaults apply (weaponPoison/Burn/Bleed/Acid ⇒ ONCRITICAL).
        /// </summary>
        [JsonPropertyName("triggerWhen")]
        public string TriggerWhen { get; set; } = "";
        public double MinValue { get; set; } = 0;
        public double MaxValue { get; set; } = 0;
        public double RolledValue { get; set; } = 0; // The actual rolled value between MinValue and MaxValue
        public List<string> StatusEffects { get; set; } = new List<string>(); // Status effects to apply to Attack actions

        /// <summary>Optional equip gates from this prefix alone; merged into <see cref="Item.AttributeRequirements"/> when loot/lab builds the item.</summary>
        [JsonPropertyName("attributeRequirements")]
        public AttributeRequirements AttributeRequirements { get; set; } = new AttributeRequirements();

        /// <summary>Optional registry tags (material, class path, element, …) copied onto <see cref="Item.Tags"/> at loot time.</summary>
        [JsonPropertyName("tags")]
        public List<string>? Tags { get; set; }
    }

    public class ArmorStatus
    {
        public string Name { get; set; } = "";
        public string Description { get; set; } = "";
        public string Effect { get; set; } = "";
        public double Value { get; set; } = 0;
        public bool IsPassive { get; set; } = true;
    }

    /// <summary>
    /// Persist head/chest/feet/weapon subtype fields (e.g. <see cref="HeadItem.Armor"/>) on save and lab JSON clones.
    /// Without this, armor round-trips as a base <see cref="Item"/> and <see cref="ItemTypeConverter"/> used to invent tier-based armor.
    /// </summary>
    [JsonPolymorphic(TypeDiscriminatorPropertyName = "$itemType")]
    [JsonDerivedType(typeof(HeadItem), "head")]
    [JsonDerivedType(typeof(ChestItem), "chest")]
    [JsonDerivedType(typeof(FeetItem), "feet")]
    [JsonDerivedType(typeof(LegsItem), "legs")]
    [JsonDerivedType(typeof(WeaponItem), "weapon")]
    public class Item
    {
        public string Name { get; set; } = "";
        public ItemType Type { get; set; }
        public int Tier { get; set; } = 1;
        public int Level { get; set; } = 1; // Item level based on dungeon level when generated
        public int ComboBonus { get; set; } = 0;

        /// <summary>Catalog / sheet primary stat bonuses on base armor (before rolled suffixes). Mapped to STR/AGI/TEC/INT in <see cref="EquipmentBonusCalculator"/>.</summary>
        public int BaseStrength { get; set; }
        public int BaseAgility { get; set; }
        public int BaseTechnique { get; set; }
        public int BaseIntelligence { get; set; }

        /// <summary>Optional threshold-style bonuses from armor sheet (HIT/COMBO/CRIT stat keys).</summary>
        public int BaseHit { get; set; }
        public int BaseCombo { get; set; }
        public int BaseCrit { get; set; }

        /// <summary>Catalog-rolled extra combo sequence slots (any armor slot or weapon); affix lines use StatType <c>ExtraActionSlots</c>. Capped with tuning in <see cref="ComboSequenceMaxHelper"/>.</summary>
        public int ExtraActionSlots { get; set; }

        /// <summary>Armor catalog <c>attackSpeed</c> (seconds subtracted in <see cref="Combat.Calculators.SpeedCalculator"/> via <see cref="EquipmentBonusCalculator.GetAttackSpeedBonus"/>).</summary>
        [JsonPropertyName("catalogAttackSpeed")]
        public double CatalogAttackSpeed { get; set; }

        /// <summary>Head: minimum granted <see cref="ActionBonus"/> lines when loot affixes are applied.</summary>
        public int MinGeneratedActionBonuses { get; set; }
        public string Rarity { get; set; } = "Common";
        public List<StatBonus> StatBonuses { get; set; } = new List<StatBonus>();
        public List<ActionBonus> ActionBonuses { get; set; } = new List<ActionBonus>();
        public List<Modification> Modifications { get; set; } = new List<Modification>();
        public List<ArmorStatus> ArmorStatuses { get; set; } = new List<ArmorStatus>();
        public int BonusDamage { get; set; } = 0;
        public int BonusAttackSpeed { get; set; } = 0;
        public List<string> Tags { get; set; } = new List<string>();

        /// <summary>When <see cref="Type"/> is <see cref="ItemType.Consumable"/>, marks food vs potion and which buff line.</summary>
        public RoomSearchConsumableKind RoomSearchConsumableKind { get; set; }

        /// <summary>Food only: HP restored when eaten.</summary>
        public int ConsumableHealAmount { get; set; }

        /// <summary>Potions: magnitude (stat points or threshold adjustment units).</summary>
        public int ConsumablePotionPotency { get; set; }

        /// <summary>
        /// Persisted for <see cref="ItemType.Weapon"/> (save/load, inventory). Drives level-up stat spread and class points — same mapping as <see cref="WeaponItem"/> (Mace/Barbarian STR, etc.). Ignored for armor.
        /// </summary>
        public WeaponType WeaponType { get; set; } = WeaponType.Sword;
        
        // Attribute requirements for this item (extensible for future secondary attributes)
        public AttributeRequirements AttributeRequirements { get; set; } = new AttributeRequirements();

        /// <summary>Catalog (base) attribute gates copied at generation time; used to recompute <see cref="AttributeRequirements"/> after prefix rolls or rerolls.</summary>
        [JsonPropertyName("catalogAttributeRequirements")]
        public AttributeRequirements CatalogAttributeRequirements { get; set; } = new AttributeRequirements();
        
        // The specific action this gear provides (assigned when created)
        public string? GearAction { get; set; } = null;
        
        /// <summary>True when tagged <c>starter</c> in game data or legacy name contained "Starter".</summary>
        public bool IsStarterItem =>
            (Tags != null && Tags.Any(t => string.Equals(t, "starter", StringComparison.OrdinalIgnoreCase))) ||
            (!string.IsNullOrEmpty(Name) && Name.Contains("Starter", StringComparison.OrdinalIgnoreCase));

        /// <summary>
        /// Normalizes requirement dictionary keys from saves, editors, or legacy data (<c>INT</c>, <c>TECH</c>, typos)
        /// to the lowercase names used by <see cref="GetEffectiveValueForRequirementKey"/> and tooltips.
        /// Matches <see cref="RPGGame.Data.GameDataJsonNormalizer"/> sheet abbrev rules; dynamic categories
        /// (<c>primary</c> / <c>secondary</c> / <c>neglected</c> / <c>weakness</c>) are passed through in lowercase
        /// so <see cref="GetEffectiveValueForRequirementKey(string, Character)"/> can resolve them per-character.
        /// </summary>
        public static string CanonicalizeAttributeRequirementKey(string raw)
        {
            if (string.IsNullOrWhiteSpace(raw))
                return string.Empty;
            string u = raw.Trim().ToUpperInvariant().Replace(" ", "").Replace("_", "");
            return u switch
            {
                "STR" or "STRENGTH" or "STENGTH" or "STRENTH" => "strength",
                "AGI" or "AGILITY" or "AGILTIY" => "agility",
                "TEC" or "TECH" or "TECHNIQUE" or "TECHINQUE" or "TEHCNIQUE" => "technique",
                "INT" or "INTELLIGENCE" or "INTELIGENCE" or "INTELLEGENCE" => "intelligence",
                "PRIMARY" => "primary",
                "SECONDARY" => "secondary",
                "NEGLECTED" => "neglected",
                "WEAKNESS" => "weakness",
                _ => raw.Trim().ToLowerInvariant()
            };
        }

        /// <summary>
        /// True when <paramref name="requirementKeyLower"/> names one of the four dynamic attribute categories
        /// (<c>primary</c> / <c>secondary</c> / <c>neglected</c> / <c>weakness</c>).
        /// </summary>
        public static bool IsDynamicAttributeRequirementKey(string requirementKeyLower) =>
            requirementKeyLower is "primary" or "secondary" or "neglected" or "weakness";

        /// <summary>
        /// Effective value for a core requirement key (<c>strength</c>, <c>agility</c>, <c>technique</c>, <c>intelligence</c>).
        /// Dynamic categories return <c>0</c> on this overload; callers should pre-resolve via
        /// <see cref="GetEffectiveValueForRequirementKey(string, Character)"/> when checking suffix gates.
        /// </summary>
        public static int GetEffectiveValueForRequirementKey(string requirementKeyLower, int strength, int agility, int technique, int intelligence) =>
            requirementKeyLower switch
            {
                "strength" => strength,
                "agility" => agility,
                "technique" => technique,
                "intelligence" => intelligence,
                _ => 0
            };

        /// <summary>
        /// Effective value for a requirement key against <paramref name="character"/>; dynamic categories
        /// (<c>primary</c>...<c>weakness</c>) resolve to whichever core stat currently fills that rank.
        /// </summary>
        public static int GetEffectiveValueForRequirementKey(string requirementKeyLower, Character character)
        {
            if (character == null || character.Facade == null)
                return 0;
            int strength = character.Facade.GetEffectiveStrength();
            int agility = character.Facade.GetEffectiveAgility();
            int technique = character.Facade.GetEffectiveTechnique();
            int intelligence = character.Facade.GetEffectiveIntelligence();

            string concrete = requirementKeyLower;
            if (IsDynamicAttributeRequirementKey(requirementKeyLower))
            {
                string code = DynamicAttributeCategoryResolver.ResolveCategoryToStatCode(
                    character, requirementKeyLower.ToUpperInvariant());
                concrete = code switch
                {
                    DynamicAttributeCategoryResolver.CodeStrength => "strength",
                    DynamicAttributeCategoryResolver.CodeAgility => "agility",
                    DynamicAttributeCategoryResolver.CodeTechnique => "technique",
                    DynamicAttributeCategoryResolver.CodeIntelligence => "intelligence",
                    _ => requirementKeyLower
                };
            }

            return GetEffectiveValueForRequirementKey(concrete, strength, agility, technique, intelligence);
        }

        /// <summary>Player-facing name for a normalized requirement key (lowercase). Dynamic categories render uppercase (PRIMARY, SECONDARY, …).</summary>
        public static string FormatRequirementAttributeDisplayName(string requirementKeyLower) =>
            requirementKeyLower switch
            {
                "strength" => "Strength",
                "agility" => "Agility",
                "technique" => "Technique",
                "intelligence" => "Intelligence",
                "primary" => "PRIMARY",
                "secondary" => "SECONDARY",
                "neglected" => "NEGLECTED",
                "weakness" => "WEAKNESS",
                _ => string.IsNullOrEmpty(requirementKeyLower)
                    ? requirementKeyLower
                    : char.ToUpperInvariant(requirementKeyLower[0]) + requirementKeyLower.Substring(1)
            };

        /// <summary>
        /// When non-null, the character cannot equip this item due to attribute gates (effective STR/AGI/TEC/INT).
        /// </summary>
        public string? GetEquipBlockedReason(Character character)
        {
            if (Type == ItemType.Consumable)
                return "Room-search food and potions apply when found; they cannot be equipped.";

            if (character == null || character.Facade == null || !AttributeRequirements.HasRequirements)
                return null;

            foreach (var requirement in AttributeRequirements)
            {
                string keyLower = CanonicalizeAttributeRequirementKey(requirement.Key);
                int have = GetEffectiveValueForRequirementKey(keyLower, character);
                if (have < requirement.Value)
                {
                    string label = FormatRequirementAttributeDisplayName(keyLower);
                    return $"Requires {label} {requirement.Value} (you have {have}).";
                }
            }

            return null;
        }

        /// <summary>Single-line summary for tooltips (e.g. <c>Requires: Technique 5, Strength 10</c>).</summary>
        public string? GetAttributeRequirementsSummaryLine()
        {
            if (!AttributeRequirements.HasRequirements)
                return null;

            var parts = AttributeRequirements
                .Select(kv => $"{FormatRequirementAttributeDisplayName(CanonicalizeAttributeRequirementKey(kv.Key))} {kv.Value}");
            return "Requires: " + string.Join(", ", parts);
        }

        /// <summary>
        /// Checks if a character meets all attribute requirements for this item
        /// </summary>
        /// <param name="character">The character to check</param>
        /// <returns>True if character meets all requirements, false otherwise</returns>
        public bool MeetsRequirements(Character character)
        {
            if (character == null || character.Facade == null || !AttributeRequirements.HasRequirements)
            {
                return true; // No requirements or no character means always pass
            }

            foreach (var requirement in AttributeRequirements)
            {
                int characterValue = GetEffectiveValueForRequirementKey(
                    CanonicalizeAttributeRequirementKey(requirement.Key), character);

                if (characterValue < requirement.Value)
                    return false;
            }

            return true; // All requirements met
        }

        /// <summary>
        /// Rebuilds <see cref="AttributeRequirements"/> from <see cref="CatalogAttributeRequirements"/> plus the per-stat maximum
        /// required by each rolled <see cref="Modification"/> that defines <c>attributeRequirements</c>, and each
        /// <see cref="StatBonus"/> suffix that defines <see cref="StatBonus.Requirements"/>.
        /// </summary>
        public void RecomputeAttributeRequirementsIncludingModifications()
        {
            AttributeRequirements = CatalogAttributeRequirements != null && CatalogAttributeRequirements.Count > 0
                ? new AttributeRequirements(CatalogAttributeRequirements)
                : new AttributeRequirements();

            if (Modifications != null)
            {
                foreach (var mod in Modifications)
                {
                    if (mod?.AttributeRequirements == null || !mod.AttributeRequirements.HasRequirements)
                        continue;

                    foreach (var kv in mod.AttributeRequirements)
                        MergeRequirementMax(CanonicalizeAttributeRequirementKey(kv.Key), kv.Value);
                }
            }

            if (StatBonuses != null)
            {
                foreach (var suffix in StatBonuses)
                {
                    if (suffix == null || !suffix.HasRequirements)
                        continue;

                    foreach (var kv in suffix.Requirements!)
                        MergeRequirementMax(CanonicalizeAttributeRequirementKey(kv.Key), kv.Value);
                }
            }
        }

        private void MergeRequirementMax(string canonicalKey, int value)
        {
            if (string.IsNullOrEmpty(canonicalKey))
                return;
            if (AttributeRequirements.TryGetValue(canonicalKey, out int cur))
                AttributeRequirements[canonicalKey] = Math.Max(cur, value);
            else
                AttributeRequirements[canonicalKey] = value;
        }

        /// <summary>Armor from suffix lines and prefix modifications; null-safe for lab / partial JSON rows.</summary>
        protected static int AddArmorFromAffixes(Item item, int totalArmor)
        {
            if (item.StatBonuses != null)
            {
                foreach (var statBonus in item.StatBonuses)
                {
                    if (statBonus == null) continue;
                    totalArmor += statBonus.SumContributionValuesForStatType("Armor");
                }
            }

            if (item.Modifications != null)
            {
                foreach (var modification in item.Modifications)
                {
                    if (modification == null) continue;
                    string? eff = modification.Effect;
                    if (string.IsNullOrEmpty(eff)) continue;
                    if (eff.Contains("armor", StringComparison.OrdinalIgnoreCase))
                        totalArmor += (int)modification.RolledValue;
                }
            }

            return totalArmor;
        }

        public Item(ItemType type, string? name = null, int tier = 1, int comboBonus = 0)
        {
            Type = type;
            Name = name ?? (type == ItemType.Weapon
                ? "Unknown Weapon"
                : type == ItemType.Consumable
                    ? "Unnamed Consumable"
                    : "Unknown Armor");
            Tier = tier;
            ComboBonus = comboBonus;
        }
    }

    public class HeadItem : Item
    {
        public int Armor { get; set; }
        public HeadItem(string? name = null, int tier = 1, int armor = 1)
            : base(ItemType.Head, name, tier)
        {
            Armor = armor;
        }

        public int GetTotalArmor()
        {
            int totalArmor = AddArmorFromAffixes(this, Armor);
            double q = ItemPrefixHelper.GetGearPrimaryStatMultiplier(this);
            totalArmor = (int)Math.Round(totalArmor * q);

            // Prevent integer overflow
            return Math.Max(int.MinValue + 1, Math.Min(int.MaxValue - 1, totalArmor));
        }
    }

    public class FeetItem : Item
    {
        public int Armor { get; set; }
        public FeetItem(string? name = null, int tier = 1, int armor = 1)
            : base(ItemType.Feet, name, tier)
        {
            Armor = armor;
        }

        public int GetTotalArmor()
        {
            int totalArmor = AddArmorFromAffixes(this, Armor);
            double q = ItemPrefixHelper.GetGearPrimaryStatMultiplier(this);
            totalArmor = (int)Math.Round(totalArmor * q);
            
            // Prevent integer overflow
            return Math.Max(int.MinValue + 1, Math.Min(int.MaxValue - 1, totalArmor));
        }
    }

    public class ChestItem : Item
    {
        public int Armor { get; set; }
        public ChestItem(string? name = null, int tier = 1, int armor = 2)
            : base(ItemType.Chest, name, tier)
        {
            Armor = armor;
        }

        public int GetTotalArmor()
        {
            int totalArmor = AddArmorFromAffixes(this, Armor);
            double q = ItemPrefixHelper.GetGearPrimaryStatMultiplier(this);
            totalArmor = (int)Math.Round(totalArmor * q);
            
            // Prevent integer overflow
            return Math.Max(int.MinValue + 1, Math.Min(int.MaxValue - 1, totalArmor));
        }
    }

    public class LegsItem : Item
    {
        public int Armor { get; set; }
        public LegsItem(string? name = null, int tier = 1, int armor = 1)
            : base(ItemType.Legs, name, tier)
        {
            Armor = armor;
        }

        public int GetTotalArmor()
        {
            int totalArmor = AddArmorFromAffixes(this, Armor);
            double q = ItemPrefixHelper.GetGearPrimaryStatMultiplier(this);
            totalArmor = (int)Math.Round(totalArmor * q);

            return Math.Max(int.MinValue + 1, Math.Min(int.MaxValue - 1, totalArmor));
        }
    }

    public class WeaponItem : Item
    {
        public int BaseDamage { get; set; }

        /// <summary>Flat damage rolled once from catalog <c>DamageBonusMin</c>..<c>DamageBonusMax</c> when the weapon is created (loot, lab, starters).</summary>
        public int RolledDamageBonus { get; set; }

        public double BaseAttackSpeed { get; set; } = 0.05;

        public WeaponItem(string? name = null, int tier = 1, int baseDamage = 10, double baseAttackSpeed = 0.05, WeaponType weaponType = WeaponType.Sword)
            : base(ItemType.Weapon, name, tier)
        {
            BaseDamage = baseDamage;
            BaseAttackSpeed = baseAttackSpeed;
            WeaponType = weaponType;
        }

        public int GetTotalDamage()
        {
            int sum = BaseDamage + RolledDamageBonus + BonusDamage;
            double q = ItemPrefixHelper.GetGearPrimaryStatMultiplier(this);
            return Math.Max(1, (int)Math.Round(sum * q));
        }

        /// <summary>Single-line damage text: total, then flat components (base, optional catalog roll, optional tier <see cref="Item.BonusDamage"/>).</summary>
        public string FormatDamageBreakdownForDisplay()
        {
            int total = GetTotalDamage();
            var parts = new System.Collections.Generic.List<string> { $"base {BaseDamage}" };
            if (RolledDamageBonus != 0)
                parts.Add($"roll {RolledDamageBonus}");
            if (BonusDamage != 0)
                parts.Add($"tier {BonusDamage}");
            return $"{total} ({string.Join(" + ", parts)})";
        }

        public double GetTotalAttackSpeed()
        {
            // attackSpeed from data: time multiplier vs 1 (higher = slower cadence). BonusAttackSpeed lowers the multiplier (faster).
            // Quality (gearPrimaryStatMultiplier) should affect weapon speed as well as damage:
            // higher quality => faster => LOWER time multiplier; lower quality => slower => HIGHER time multiplier.
            double q = ItemPrefixHelper.GetGearPrimaryStatMultiplier(this);
            if (q <= 0) q = 1.0;

            double speed = (BaseAttackSpeed / q) - (BonusAttackSpeed * 0.1);
            // Keep speed within reasonable bounds (time multiplier).
            return Math.Max(0.1, Math.Min(10.0, speed));
        }
        
        /// <summary>
        /// Gets the attack speed multiplier for the weapon
        /// </summary>
        /// <returns>Attack speed multiplier (0.9 = 10% faster, 1.1 = 10% slower)</returns>
        public double GetAttackSpeedMultiplier()
        {
            // Clamp the attack speed to reasonable multiplier values
            // The scaling system has corrupted BaseAttackSpeed, so we need to normalize it
            double normalizedSpeed = Math.Max(0.1, Math.Min(10.0, BaseAttackSpeed));
            
            // If the speed is extremely high (corrupted by scaling), use a reasonable default
            if (BaseAttackSpeed > 10.0)
            {
                // Use a reasonable default for corrupted values
                return 1.0; // Normal speed
            }

            // Apply quality to weapon speed (see GetTotalAttackSpeed): higher quality => faster => lower multiplier.
            double q = ItemPrefixHelper.GetGearPrimaryStatMultiplier(this);
            if (q <= 0) q = 1.0;
            return Math.Max(0.1, Math.Min(10.0, normalizedSpeed / q));
        }
    }
} 