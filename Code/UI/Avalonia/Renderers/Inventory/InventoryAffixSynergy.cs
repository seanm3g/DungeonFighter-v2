using System;
using System.Collections.Generic;
using System.Linq;

namespace RPGGame.UI.Avalonia.Renderers.Inventory
{
    /// <summary>
    /// Which items share an affix with the hovered one, so the inventory grid can light their frames.
    /// Affixes are adjective prefixes and "of …" suffixes; material and quality prefixes are skipped
    /// because nearly every item has one. Two affixes overlap when they share a name or an effect
    /// (Light and Acrobatic are both MULTI-HIT; Heavy and "of Power" both add damage).
    /// </summary>
    public static class InventoryAffixSynergy
    {
        private const string NameKey = "name:";
        private const string EffectKey = "fx:";
        private const string MaterialKey = "mat:";

        /// <summary>Mod effects and suffix stat types that name the same thing in different vocabularies.</summary>
        private static readonly Dictionary<string, string> EffectAliases = new(StringComparer.Ordinal)
        {
            ["HEALTHBONUS"] = "HEALTH",
            ["ATTACKSPEED"] = "SPEED",
        };

        /// <param name="includeMaterial">
        /// Also key the item's material. Equipped gear passes true: worn pieces of one material build a set bonus,
        /// so they overlap with each other even though a shared material means little in the bag.
        /// </param>
        public static HashSet<string> GetAffixKeys(Item? item, bool includeMaterial = false)
        {
            var keys = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            if (item == null)
                return keys;
            foreach (var mod in item.Modifications)
            {
                if (string.IsNullOrWhiteSpace(mod.Name))
                    continue;
                string category = mod.PrefixCategory?.Trim() ?? "";
                if (category.Equals("MATERIAL", StringComparison.OrdinalIgnoreCase)
                    || category.Equals("QUALITY", StringComparison.OrdinalIgnoreCase))
                    continue;
                keys.Add(NameKey + mod.Name.Trim());
                AddEffect(keys, mod.Effect);
            }
            foreach (var bonus in item.StatBonuses)
            {
                if (!string.IsNullOrWhiteSpace(bonus.Name))
                    keys.Add(NameKey + bonus.Name.Trim());
                foreach (var (statType, _) in bonus.EnumerateContributions())
                    AddEffect(keys, statType);
            }
            if (includeMaterial)
            {
                string material = item.Material?.Trim() ?? "";
                if (material.Length > 0 && !material.Equals("Unknown", StringComparison.OrdinalIgnoreCase))
                    keys.Add(MaterialKey + material);
            }
            return keys;
        }

        private static void AddEffect(HashSet<string> keys, string? effect)
        {
            if (string.IsNullOrWhiteSpace(effect))
                return;
            string normalized = effect.Replace(" ", "").Replace("-", "").Replace("_", "").ToUpperInvariant();
            if (EffectAliases.TryGetValue(normalized, out var alias))
                normalized = alias;
            keys.Add(EffectKey + normalized);
        }

        /// <summary>True when <paramref name="item"/> is a different item carrying any of <paramref name="hoveredAffixes"/>.</summary>
        public static bool SharesAffix(Item? item, Item? hovered, HashSet<string> hoveredAffixes, bool includeMaterial = false)
        {
            if (item == null || hovered == null || ReferenceEquals(item, hovered) || hoveredAffixes.Count == 0)
                return false;
            return GetAffixKeys(item, includeMaterial).Overlaps(hoveredAffixes);
        }

        private static readonly string[] GearSlots = { "head", "body", "legs", "weapon", "feet", "charm" };

        /// <summary>True when <paramref name="item"/> is worn in one of <paramref name="character"/>'s gear slots.</summary>
        public static bool IsEquipped(Item? item, Character? character)
        {
            if (item == null || character == null)
                return false;
            foreach (var slot in GearSlots)
            {
                if (ReferenceEquals(character.Equipment.GetSlotItem(slot), item))
                    return true;
            }
            return false;
        }

        /// <summary>
        /// Worn and bagged items that light up when <paramref name="hovered"/> is hovered, with what each shares
        /// ("Combo amp", "Keen", "Bone set"). Uses the same matching as the grid frames: gear-to-gear also matches material.
        /// </summary>
        public static List<(Item Item, bool Equipped, List<string> Shared)> FindPartners(Item? hovered, Character? character)
        {
            var partners = new List<(Item, bool, List<string>)>();
            if (hovered == null || character == null)
                return partners;
            bool hoveredIsGear = IsEquipped(hovered, character);
            var bagAffixes = GetAffixKeys(hovered);
            var gearAffixes = hoveredIsGear ? GetAffixKeys(hovered, includeMaterial: true) : bagAffixes;
            var seen = new HashSet<Item>(ReferenceEqualityComparer.Instance) { hovered };
            foreach (var slot in GearSlots)
            {
                var item = character.Equipment.GetSlotItem(slot);
                if (item == null || !seen.Add(item))
                    continue;
                var shared = DescribeShared(gearAffixes, GetAffixKeys(item, hoveredIsGear));
                if (shared.Count > 0)
                    partners.Add((item, true, shared));
            }
            foreach (var item in character.Inventory ?? new List<Item>())
            {
                if (item == null || !seen.Add(item))
                    continue;
                var shared = DescribeShared(bagAffixes, GetAffixKeys(item));
                if (shared.Count > 0)
                    partners.Add((item, false, shared));
            }
            return partners;
        }

        private static List<string> DescribeShared(HashSet<string> hoveredAffixes, HashSet<string> otherAffixes)
        {
            var labels = new List<string>();
            foreach (var key in hoveredAffixes)
            {
                if (!otherAffixes.Contains(key))
                    continue;
                string label = DescribeKey(key);
                if (!labels.Contains(label, StringComparer.OrdinalIgnoreCase))
                    labels.Add(label);
            }
            return labels;
        }

        private static string DescribeKey(string key)
        {
            if (key.StartsWith(NameKey, StringComparison.OrdinalIgnoreCase))
                return key.Substring(NameKey.Length);
            if (key.StartsWith(MaterialKey, StringComparison.OrdinalIgnoreCase))
                return key.Substring(MaterialKey.Length) + " set";
            string effect = key.Substring(EffectKey.Length);
            return Helpers.ItemStatContributionCollector.GetModificationLabel(effect)
                ?? char.ToUpperInvariant(effect[0]) + effect.Substring(1).ToLowerInvariant();
        }

        /// <summary>
        /// The item under the pointer from a <see cref="LeftPanelHoverState"/> value: a bag row/tile
        /// (<c>lphover:inv:{index}</c>) or a gear slot (<c>lphover:gear:{slot}</c>); null otherwise.
        /// </summary>
        public static Item? ResolveHoveredItem(string hoverValue, IReadOnlyList<Item>? inventory, Character? character)
        {
            if (string.IsNullOrEmpty(hoverValue))
                return null;
            string invPrefix = LeftPanelHoverState.Prefix + "inv:";
            string gearPrefix = LeftPanelHoverState.Prefix + "gear:";
            if (hoverValue.StartsWith(invPrefix, StringComparison.Ordinal))
            {
                if (inventory != null && int.TryParse(hoverValue.AsSpan(invPrefix.Length), out int index)
                    && index >= 0 && index < inventory.Count)
                    return inventory[index];
                return null;
            }
            if (hoverValue.StartsWith(gearPrefix, StringComparison.Ordinal))
                return character?.Equipment.GetSlotItem(hoverValue.Substring(gearPrefix.Length));
            return null;
        }
    }
}
