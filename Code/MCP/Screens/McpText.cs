using System;
using System.Collections.Generic;
using System.Linq;
using RPGGame.UI.ColorSystem;
using GameAction = RPGGame.Action;

namespace RPGGame.MCP.Screens
{
    /// <summary>
    /// Plain-text versions of what the canvas shows for items and actions, built from the same formatters the
    /// game uses (item tooltip, action card tooltip), so an agent reads the wording a player reads.
    /// </summary>
    public static class McpText
    {
        public static readonly string[] EquipSlots = { "weapon", "head", "body", "legs", "feet", "charm" };

        /// <summary>Full item tooltip as plain lines (name, rarity, stats, lines, base modifier, triggers, requirements).</summary>
        public static List<string> ItemLines(Character? hero, Item? item, string slotLabel = "Inventory", int maxLines = 40)
        {
            if (item == null)
                return new List<string>();
            try
            {
                return ItemTooltipFormatter.BuildItemTooltipLines(hero, item, slotLabel, maxLines, includeExtendedDetails: true)
                    .Select(ColoredTextRenderer.RenderAsPlainText)
                    .Select(l => l.TrimEnd())
                    .Where(l => l.Length > 0)
                    .ToList();
            }
            catch (Exception ex)
            {
                return new List<string> { item.Name, $"(tooltip failed: {ex.Message})" };
            }
        }

        /// <summary>One line for lists: name, slot, rarity, tier, item level, requirement flag.</summary>
        public static string ItemShort(Character? hero, Item item)
        {
            string slot = SlotOf(item);
            string rarity = string.IsNullOrEmpty(item.Rarity) ? "Common" : item.Rarity;
            string req = hero != null && !item.MeetsRequirements(hero) ? " [requirements not met]" : "";
            string material = string.IsNullOrEmpty(item.Material) ? "" : $", {item.Material}";
            return $"{item.Name} ({slot}, {rarity} T{item.Tier}, ilvl {item.Level}{material}){req}";
        }

        /// <summary>Action card as plain lines (damage, speed, effects, cadence), as the strip tooltip shows it.</summary>
        public static List<string> ActionLines(Character? hero, GameAction? action, int maxLines = 16)
        {
            if (action == null)
                return new List<string>();
            try
            {
                var lines = CombatActionStripBuilder.BuildActionTooltipLinesForAction(hero, action, maxWidth: 70, maxLines: maxLines)
                    .Select(l => l.TrimEnd())
                    .Where(l => l.Length > 0)
                    .ToList();
                return lines.Count > 0 ? lines : new List<string> { action.Name };
            }
            catch (Exception ex)
            {
                return new List<string> { action.Name, $"(card failed: {ex.Message})" };
            }
        }

        /// <summary>Action card collapsed into one line ("NAME — line · line · line").</summary>
        public static string ActionShort(Character? hero, GameAction action, int maxParts = 4)
        {
            var lines = ActionLines(hero, action, maxLines: 14);
            var rest = lines.Select(l => l.Trim().TrimStart('|').Trim())
                .Where(l => l.Length > 0
                    && !string.Equals(l, action.Name, StringComparison.OrdinalIgnoreCase)
                    && !l.StartsWith("Tags:", StringComparison.OrdinalIgnoreCase)
                    && !IsSectionHeader(l))
                .Take(maxParts);
            string tail = string.Join(" · ", rest);
            return tail.Length > 0 ? $"{action.Name} — {tail}" : action.Name;
        }

        /// <summary>Card section titles ("Does", "Stats", ...) carry no information on one line.</summary>
        private static bool IsSectionHeader(string line) =>
            line.Length <= 14 && line.IndexOf(':') < 0 && line.IndexOf(' ') < 0 && !line.Any(char.IsDigit);

        public static string SlotOf(Item item) => item.Type switch
        {
            ItemType.Weapon => "weapon",
            ItemType.Head => "head",
            ItemType.Chest => "body",
            ItemType.Legs => "legs",
            ItemType.Feet => "feet",
            ItemType.Charm => "charm",
            _ => item.Type.ToString().ToLowerInvariant()
        };

        public static Item? EquippedIn(Character hero, string slot)
        {
            try { return hero.Equipment.GetSlotItem(slot); }
            catch { return null; }
        }
    }
}
