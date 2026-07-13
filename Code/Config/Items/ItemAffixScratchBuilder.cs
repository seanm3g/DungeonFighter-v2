using System;
using System.Collections.Generic;
using System.Linq;

namespace RPGGame
{
    /// <summary>
    /// Builds an editable per-slot affix scratch map for Settings → Item Generation.
    /// Entries are deep-cloned so mutating one slot never aliases another (or live tuning).
    /// </summary>
    public static class ItemAffixScratchBuilder
    {
        public static readonly string[] AffixSlotKeys = { "Head", "Chest", "Legs", "Feet", "Weapon" };

        public static Dictionary<string, Dictionary<string, ItemAffixPerRarityEntry>> BuildFromTuning(
            ItemAffixByRaritySettings? tuning,
            IReadOnlyList<RarityData>? rarityData = null)
        {
            var scratch = new Dictionary<string, Dictionary<string, ItemAffixPerRarityEntry>>(StringComparer.OrdinalIgnoreCase);

            foreach (string slot in AffixSlotKeys)
            {
                var dict = new Dictionary<string, ItemAffixPerRarityEntry>(StringComparer.OrdinalIgnoreCase);
                ItemType it = SlotKeyToItemType(slot);

                foreach (string rarity in ItemAffixByRaritySettings.StandardLootRarities)
                {
                    var tableRow = rarityData?.FirstOrDefault(r =>
                        r.Name.Equals(rarity, StringComparison.OrdinalIgnoreCase));
                    if (tuning != null && tuning.TryGetForItemTypeAndRarity(it, rarity, out var typed))
                        dict[rarity] = CloneEntry(typed);
                    else if (tuning != null && tuning.TryGetForRarity(rarity, out var legacy))
                        dict[rarity] = CloneEntry(legacy);
                    else
                    {
                        var rule = ItemAffixByRaritySettings.GetResolvedAffixRule(rarity, tableRow, null, it);
                        dict[rarity] = new ItemAffixPerRarityEntry
                        {
                            PrefixSlots = rule.PrefixMin,
                            PrefixExtraChance = 0,
                            PrefixSlotsMax = null,
                            StatSuffixes = rule.StatMin,
                            StatSuffixExtraChance = 0,
                            StatSuffixesMax = null,
                            ActionBonuses = rule.ActionMin,
                            ActionExtraChance = 0,
                            ActionBonusesMax = null,
                            ExtraComboSlots = rule.ExtraComboSlotsMin,
                            ExtraComboSlotsExtraChance = 0,
                            ExtraComboSlotsMax = null
                        };
                    }
                }

                scratch[slot] = dict;
            }

            // Prefer authored perItemType rows (still deep-cloned — never alias live tuning objects).
            if (tuning?.PerItemType != null)
            {
                foreach (var kv in tuning.PerItemType)
                {
                    if (kv.Value == null || kv.Value.Count == 0)
                        continue;
                    string key = AffixSlotKeys.FirstOrDefault(s => s.Equals(kv.Key, StringComparison.OrdinalIgnoreCase)) ?? kv.Key;
                    var cloned = new Dictionary<string, ItemAffixPerRarityEntry>(StringComparer.OrdinalIgnoreCase);
                    foreach (var row in kv.Value)
                    {
                        if (row.Value == null)
                            continue;
                        cloned[row.Key] = CloneEntry(row.Value);
                    }

                    scratch[key] = cloned;
                }
            }

            return scratch;
        }

        public static ItemType SlotKeyToItemType(string slot) =>
            slot.Equals("Head", StringComparison.OrdinalIgnoreCase) ? ItemType.Head :
            slot.Equals("Chest", StringComparison.OrdinalIgnoreCase) ? ItemType.Chest :
            slot.Equals("Legs", StringComparison.OrdinalIgnoreCase) ? ItemType.Legs :
            slot.Equals("Feet", StringComparison.OrdinalIgnoreCase) ? ItemType.Feet :
            ItemType.Weapon;

        public static ItemAffixPerRarityEntry CloneEntry(ItemAffixPerRarityEntry e) =>
            new()
            {
                PrefixSlots = e.PrefixSlots,
                PrefixExtraChance = e.PrefixExtraChance,
                PrefixSlotsMax = e.PrefixSlotsMax,
                StatSuffixes = e.StatSuffixes,
                StatSuffixExtraChance = e.StatSuffixExtraChance,
                StatSuffixesMax = e.StatSuffixesMax,
                ActionBonuses = e.ActionBonuses,
                ActionExtraChance = e.ActionExtraChance,
                ActionBonusesMax = e.ActionBonusesMax,
                ExtraComboSlots = e.ExtraComboSlots,
                ExtraComboSlotsExtraChance = e.ExtraComboSlotsExtraChance,
                ExtraComboSlotsMax = e.ExtraComboSlotsMax
            };

        /// <summary>
        /// Resolves the next slot key from a ComboBox selection (SelectedItem string, index, or ToString).
        /// </summary>
        public static bool TryResolveSlotKey(object? selectedItem, int selectedIndex, out string slotKey)
        {
            slotKey = "";
            if (selectedItem is string s && !string.IsNullOrWhiteSpace(s))
            {
                string trimmed = s.Trim();
                foreach (string k in AffixSlotKeys)
                {
                    if (k.Equals(trimmed, StringComparison.OrdinalIgnoreCase))
                    {
                        slotKey = k;
                        return true;
                    }
                }

                return false;
            }

            if (selectedIndex >= 0 && selectedIndex < AffixSlotKeys.Length)
            {
                slotKey = AffixSlotKeys[selectedIndex];
                return true;
            }

            string? text = selectedItem?.ToString()?.Trim();
            if (string.IsNullOrEmpty(text))
                return false;

            foreach (string k in AffixSlotKeys)
            {
                if (k.Equals(text, StringComparison.OrdinalIgnoreCase))
                {
                    slotKey = k;
                    return true;
                }
            }

            return false;
        }
    }
}
