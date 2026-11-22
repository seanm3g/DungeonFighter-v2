using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using RPGGame.LootReveal;
using RPGGame.UI.Avalonia;
using RPGGame.UI.ColorSystem;
using RPGGame.UI.ColorSystem.Applications;

namespace RPGGame.UI
{
    /// <summary>
    /// Rebuild 2.2 (Phase 4 proposal 3, accepted 2026-11-18): the item card's fixed reading order, about five lines.
    /// <code>
    /// IRON BREECHES OF THE YOUNG OX                 Uncommon legs · Iron · T1
    ///   vs Leather Pants: +7 STR  +3 AGI  −2 Defense · breaks your Leather set (3 → 2): you lose Lucky
    ///   ▸ LOW BLOW — applies 2 Weaken
    ///   ◆ every 2nd LOW BLOW that connects also applies Expose
    ///   Iron 1/5 — wear 2 to start storing CRISIS
    ///   Requires STR 10
    ///   Alt: details
    /// </code>
    /// Comparison first, one line per attribute (base, material and suffix summed), the next set step only, and the
    /// full breakdown behind the Alt toggle. Nothing shows before its rung: the set line waits for material sets
    /// (<see cref="SystemUnlocks"/>) unless this piece would start one.
    /// </summary>
    public static class ItemCard
    {
        /// <summary>Switch: false restores the old tooltip order.</summary>
        public static bool Enabled { get; set; } = true;

        private sealed class Cached
        {
            public string Stamp = "";
            public List<string> Compare = new();
        }

        private static readonly ConditionalWeakTable<Item, Cached> CompareCache = new();

        public static List<List<ColoredText>> BuildLines(Character? hero, Item item, bool equipBlocked)
        {
            var lines = new List<List<ColoredText>>();
            if (item == null)
                return lines;

            // 1. Name, then rarity · slot · material · tier on one line.
            lines.Add(ItemDisplayColoredText.FormatFullItemName(item));
            string slot = RPGGame.UI.Avalonia.Layout.InventoryActionPoolEntries.GetEquipSlotForItem(item) ?? "";
            var head = new List<string> { $"{item.Rarity} {(slot.Length > 0 ? slot : "item")}" };
            if (!string.IsNullOrWhiteSpace(item.Material)) head.Add(item.Material);
            if (item.Tier > 0) head.Add($"T{item.Tier}");
            lines.Add(Line("  " + string.Join(" · ", head), Colors.Gray));

            // 2. The comparison (or, for a worn piece, what it gives), one line per attribute.
            if (hero != null && slot.Length > 0)
            {
                foreach (string c in CompareFor(hero, item, slot))
                    lines.Add(Line("  " + c, c.Contains('−') && !c.Contains('+') ? Colors.Red : Colors.White));
            }

            // 3. Its action (gear actions; a weapon's required basic is the weapon itself), 4. its modifier.
            foreach (string action in GearActionNames.Resolve(item).Where(a => !IsRequiredBasic(item, a)))
            {
                string desc = RuleText.Plain(ActionLoader.GetActionData(action)?.Description);
                lines.Add(Line(desc.Length > 0 ? $"  ▸ {action.ToUpperInvariant()} — {desc}" : $"  ▸ {action.ToUpperInvariant()}", Colors.Cyan));
            }
            foreach (string rule in RuleText.Lines(item.ImplicitRule, item.Name))
                lines.Add(Line($"  ◆ {rule}", Colors.Gold));

            // 5. The next set step only.
            if (hero != null && SetLine(hero, item, slot) is { } set)
                lines.Add(Line("  " + set, Colors.Silver));

            // Requirements stay (D4.4).
            string? req = item.GetAttributeRequirementsSummaryLine();
            if (!string.IsNullOrEmpty(req))
                lines.Add(Line("  " + req, equipBlocked ? Colors.Red : Colors.Gray));
            return lines;
        }

        /// <summary>"vs Leather Pants: +7 STR +3 AGI · breaks …" for a bag item; "Gives: +11 Defense +5 STR" for a worn one.</summary>
        public static List<string> CompareFor(Character hero, Item item, string slot)
        {
            var worn = hero.Equipment.GetSlotItem(slot);
            string stamp = $"{hero.Level}|{string.Join("|", new[] { "weapon", "head", "body", "legs", "feet" }.Select(s => hero.Equipment.GetSlotItem(s)?.Name ?? "-"))}|{string.Join(",", hero.GetComboActions().Select(a => a.Name))}";
            var cached = CompareCache.GetOrCreateValue(item);
            if (cached.Stamp == stamp)
                return cached.Compare;

            var result = new List<string>();
            if (ReferenceEquals(worn, item))
            {
                var gives = ContributionLines(hero, slot);
                result.Add(gives.Count > 0 ? "Gives: " + string.Join("  ", gives) : "Gives: no stats");
            }
            else
            {
                var deltas = LootRevealBuilder.CompareLines(hero, item, slot, out _, out _);
                string body = deltas.Count > 0 ? string.Join("  ", deltas) : "the same";
                result.Add(worn == null ? $"Empty slot: {body}" : $"vs {worn.Name}: {body}");
                foreach (string s in LootRevealBuilder.SetLines(hero, item, slot).Where(s => s.StartsWith("breaks")))
                    result.Add(s);
                if (LootRevealBuilder.StripLossLine(hero, item, slot) is { } stripLoss)
                    result.Add(stripLoss);
            }
            cached.Stamp = stamp;
            cached.Compare = result;
            return result;
        }

        /// <summary>What the piece in <paramref name="slot"/> adds, one per attribute: the hero with it minus without it.</summary>
        public static List<string> ContributionLines(Character hero, string slot)
        {
            var lines = new List<string>();
            try
            {
                var serializer = new Entity.Services.CharacterSerializer();
                var copy = serializer.CreateCharacterFromSaveData(serializer.Deserialize(serializer.Serialize(hero))!);
                var with = Snapshot(copy);
                copy.UnequipItem(slot);
                var without = Snapshot(copy);
                foreach (var (label, value) in with)
                {
                    double d = value - without[label];
                    if (Math.Abs(d) >= 0.5)
                        lines.Add($"{(d > 0 ? "+" : "−")}{Math.Abs(d):0} {label}");
                }
            }
            catch
            {
                // a hero that can't be copied just shows no contribution line
            }
            return lines;
        }

        private static Dictionary<string, double> Snapshot(Character c) => new()
        {
            ["Damage"] = (c.Weapon as WeaponItem)?.GetTotalDamage() ?? 0,
            ["Defense"] = c.GetTotalArmor(),
            ["STR"] = c.GetEffectiveStrength(),
            ["AGI"] = c.GetEffectiveAgility(),
            ["TEC"] = c.GetEffectiveTechnique(),
            ["INT"] = c.GetEffectiveIntelligence(),
            ["Max HP"] = c.GetEffectiveMaxHealth(),
        };

        /// <summary>
        /// "Iron 1/5 — wear 2 to start storing CRISIS" / "Iron 3/5 — wear 5 for the full set". Null when the hero hasn't
        /// met material sets yet and this piece wouldn't start one (nothing before its rung), or the item has no material.
        /// </summary>
        public static string? SetLine(Character hero, Item item, string slot)
        {
            string mat = item.Material ?? "";
            if (mat.Length == 0)
                return null;
            int count = new[] { "weapon", "head", "body", "legs", "feet" }
                .Select(s => hero.Equipment.GetSlotItem(s))
                .Count(i => i != null && string.Equals(i.Material, mat, StringComparison.OrdinalIgnoreCase));
            bool worn = ReferenceEquals(hero.Equipment.GetSlotItem(slot), item);
            int withIt = worn ? count : count + 1;
            if (!hero.Unlocks.IsUnlocked(GameSystem.MaterialSets) && withIt < 2)
                return null;
            if (string.Equals(mat, LeatherSetBonus.MaterialName, StringComparison.OrdinalIgnoreCase))
            {
                // Leather's ladder is Lucky: on at 3 pieces, one Lucky roll per piece each fight up to 5.
                string leatherNext = count < LeatherSetBonus.LuckUnlockCount
                    ? $"wear {LeatherSetBonus.LuckUnlockCount} for Lucky"
                    : count < 5 ? $"Lucky on {count} rolls a fight, +1 per piece"
                    : "Lucky on 5 rolls a fight (full set)";
                return $"{mat} {count}/5 — {leatherNext}";
            }
            string store = Data.MaterialBuildsLoader.FindByMaterial(mat)?.Keyword ?? "";
            string next = count < 2
                ? $"wear 2 to start{(store.Length > 0 ? $" storing {store.ToUpperInvariant()}" : " the set")}"
                : count < 3 ? "wear 3 for the next step"
                : count < 5 ? "wear 5 for the full set"
                : "full set";
            return $"{mat} {count}/5 — {next}";
        }

        private static bool IsRequiredBasic(Item item, string action) =>
            GearActionNames.TryResolveWeaponType(item, out var wt)
            && string.Equals(WeaponRequiredComboAction.TryGetRequiredBasicActionName(wt), action, StringComparison.OrdinalIgnoreCase);

        private static List<ColoredText> Line(string text, global::Avalonia.Media.Color color) =>
            new() { new ColoredText(text, color) };

        private static class Colors
        {
            public static global::Avalonia.Media.Color White => AsciiArtAssets.Colors.White;
            public static global::Avalonia.Media.Color Gray => AsciiArtAssets.Colors.Gray;
            public static global::Avalonia.Media.Color Silver => AsciiArtAssets.Colors.Silver;
            public static global::Avalonia.Media.Color Red => AsciiArtAssets.Colors.Red;
            public static global::Avalonia.Media.Color Cyan => AsciiArtAssets.Colors.Cyan;
            public static global::Avalonia.Media.Color Gold => AsciiArtAssets.Colors.Gold;
        }
    }
}
