using System;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using ModelContextProtocol.Server;
using RPGGame.MCP.Screens;
using Tools = RPGGame.MCP.Tools;

namespace RPGGame.MCP
{
    /// <summary>
    /// Agent gameplay context tools (get_agent_context, set_agent_directive, item / action details).
    /// </summary>
    public static partial class McpTools
    {
        [McpServerTool(Name = "get_agent_context", Title = "[Gameplay] Get Agent Context")]
        [Description("The current screen: Screen.Body (what it shows: bag, comparison, action sequence, skill nodes, regions), labeled Choices (send a choice's Input to handle_input), hints, player brief, recent events.")]
        public static Task<string> GetAgentContext(
            [Description("Number of recent output lines (default 15, max 50)")] int recentEventCount = 15)
        {
            return Tools.AgentGameplayTools.GetAgentContext(recentEventCount);
        }

        [McpServerTool(Name = "set_agent_directive", Title = "[Gameplay] Set Agent Directive")]
        [Description("Stores session-scoped strategy echoed in get_agent_context.")]
        public static Task<string> SetAgentDirective(
            [Description("Natural-language strategy for the agent")] string directive)
        {
            return Tools.AgentGameplayTools.SetAgentDirective(directive);
        }

        [McpServerTool(Name = "get_item_details", Title = "[Gameplay] Get Item Details")]
        [Description("Full item tooltip (stats, mod lines, base modifier, triggers, requirements) plus the equipped item in the same slot. Target: a bag number as listed on the Inventory screen, or a slot name (weapon, head, body, legs, feet, charm).")]
        public static Task<string> GetItemDetails(
            [Description("Bag number (as shown on the Inventory screen) or slot name")] string target)
        {
            return Tools.McpToolExecutor.ExecuteAsync(() =>
            {
                var game = Tools.McpToolState.GameWrapper?.Game ?? throw new InvalidOperationException("No game running.");
                var hero = game.CurrentPlayer ?? throw new InvalidOperationException("No hero yet.");
                string t = (target ?? "").Trim().ToLowerInvariant();

                Item? item;
                string where;
                if (McpText.EquipSlots.Contains(t))
                {
                    item = McpText.EquippedIn(hero, t);
                    where = $"equipped {t}";
                }
                else if (int.TryParse(t, out int number))
                {
                    var inventory = game.CurrentInventory ?? hero.Inventory;
                    var st = game.GetInventoryMenuHandler()?.StateTracker;
                    var entries = UI.Avalonia.Renderers.Inventory.InventoryScreenRenderer.BuildDisplayEntries(
                        inventory, hero,
                        st?.ItemSortMode ?? Handlers.Inventory.InventoryItemSortMode.InventoryOrder,
                        st?.HideRequirementBlockedItems ?? false,
                        st?.InventoryEquipSlotFilter,
                        st?.InventoryMaterialFilter);
                    var entry = entries.FirstOrDefault(e => e.DisplayNumber == number);
                    item = entry.Item;
                    where = $"bag {number}";
                }
                else
                    throw new ArgumentException("Target must be a bag number or one of: " + string.Join(", ", McpText.EquipSlots));

                if (item == null)
                    return (object)new { target = where, found = false };

                var current = McpText.EquippedIn(hero, McpText.SlotOf(item));
                bool isEquipped = ReferenceEquals(current, item);
                return new
                {
                    target = where,
                    found = true,
                    slot = McpText.SlotOf(item),
                    meetsRequirements = item.MeetsRequirements(hero),
                    item = McpText.ItemLines(hero, item, isEquipped ? "Equipped" : "Inventory"),
                    equippedInSameSlot = isEquipped ? null : (current == null ? null : McpText.ItemLines(hero, current, "Equipped"))
                };
            }, writeIndented: true);
        }

        [McpServerTool(Name = "get_action_details", Title = "[Gameplay] Get Action Details")]
        [Description("Full action card for an action in the hero's sequence or pool, by name or sequence slot number (1-based).")]
        public static Task<string> GetActionDetails(
            [Description("Action name, or a sequence slot number")] string target)
        {
            return Tools.McpToolExecutor.ExecuteAsync(() =>
            {
                var game = Tools.McpToolState.GameWrapper?.Game ?? throw new InvalidOperationException("No game running.");
                var hero = game.CurrentPlayer ?? throw new InvalidOperationException("No hero yet.");
                string t = (target ?? "").Trim();
                var combo = hero.GetComboActions();
                var action = int.TryParse(t, out int slot) && slot >= 1 && slot <= combo.Count
                    ? combo[slot - 1]
                    : combo.Concat(hero.GetActionPool()).FirstOrDefault(a => string.Equals(a.Name, t, StringComparison.OrdinalIgnoreCase));
                if (action == null)
                    return (object)new { target = t, found = false, sequence = combo.Select(a => a.Name), pool = hero.GetActionPool().Select(a => a.Name) };
                int index = combo.FindIndex(a => ReferenceEquals(a, action));
                return new
                {
                    target = t,
                    found = true,
                    sequenceSlot = index >= 0 ? index + 1 : (int?)null,
                    card = McpText.ActionLines(hero, action, maxLines: 40)
                };
            }, writeIndented: true);
        }
    }
}
