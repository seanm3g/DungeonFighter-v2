using System;
using System.Collections.Generic;
using System.Linq;
using RPGGame.Handlers.Inventory;
using RPGGame.MCP.Models;
using RPGGame.UI.Avalonia.Renderers;
using RPGGame.UI.Avalonia.Renderers.Inventory;

namespace RPGGame.MCP.Screens
{
    /// <summary>What is on screen right now: title, sub-prompt, body text and the inputs that work there.</summary>
    public sealed class ScreenView
    {
        public string Title { get; set; } = "";
        /// <summary>Open prompt inside the screen (e.g. "EquipPicker", "Compare", "ActionSequence"), or null.</summary>
        public string? SubScreen { get; set; }
        public List<string> Body { get; set; } = new();
        public List<AgentChoice> Choices { get; set; } = new();
        public List<string> Hints { get; set; } = new();
    }

    /// <summary>
    /// Builds a <see cref="ScreenView"/> for the screens a headless (MCP) game can reach. The canvas draws these
    /// screens itself, so headless play has no menu text; this reads the same state the renderers read.
    /// Returns null for screens it does not describe (callers fall back to <see cref="AgentChoiceBuilder"/>).
    /// </summary>
    public static class ScreenDescriber
    {
        private static readonly string[] RarityOrder = { "Common", "Uncommon", "Rare", "Epic", "Legendary", "Mythic" };

        public static ScreenView? Describe(GameCoordinator game)
        {
            var hero = game.CurrentPlayer;
            switch (game.CurrentState)
            {
                case GameState.WeaponSelection:
                    return DescribeWeaponSelection();
                case GameState.GameLoop when hero != null:
                    return DescribeHub(game, hero);
                case GameState.Inventory when hero != null:
                    return DescribeInventory(game, hero);
                case GameState.SkillTree when hero != null:
                    return DescribeSkillTree(game, hero);
                case GameState.RegionTravel when hero != null:
                    return DescribeTravel(game, hero);
                case GameState.DungeonCompletion when hero != null:
                    return DescribeCompletion(hero);
                case GameState.Death when hero != null:
                    return DescribeDeath(game, hero);
                default:
                    return null;
            }
        }

        // ---------- Weapon selection ----------

        private static ScreenView DescribeWeaponSelection()
        {
            var view = new ScreenView { Title = "Weapon Selection" };
            List<StartingWeapon> weapons;
            try { weapons = GameInitializer.BuildStarterWeaponsForMenu(); }
            catch { weapons = new List<StartingWeapon>(); }

            List<WeaponData> rows;
            try { rows = StarterCatalogItems.ResolveStarterWeaponMenuCatalogRows(); }
            catch { rows = new List<WeaponData>(); }

            view.Body.Add("Pick a starting weapon. It sets your class path (skill tree) and starting actions.");
            for (int i = 0; i < weapons.Count; i++)
            {
                var w = weapons[i];
                string type = i < rows.Count ? (rows[i].Type ?? "").Trim() : "";
                string cls = Enum.TryParse(type, ignoreCase: true, out WeaponType wt) ? ClassForWeapon(wt) : "";
                string kind = type.Length == 0 ? "" : $" ({type}{(cls.Length > 0 ? $", {cls} path" : "")})";
                string line = $"{w.name}{kind}: damage {w.damage:0.#}, swing {w.attackSpeed:0.##}s";
                view.Body.Add($"[{i + 1}] {line}");
                view.Choices.Add(new AgentChoice { Input = (i + 1).ToString(), Label = line, Recommended = i == 0 });
            }
            if (view.Choices.Count == 0)
                view.Choices.Add(new AgentChoice { Input = "1", Label = "Starting weapon 1", Recommended = true });
            return view;
        }

        private static string ClassForWeapon(WeaponType weaponType) => weaponType switch
        {
            WeaponType.Mace => "Barbarian",
            WeaponType.Sword => "Warrior",
            WeaponType.Dagger => "Rogue",
            WeaponType.Wand => "Wizard",
            _ => ""
        };

        // ---------- Hub (game loop) ----------

        private static ScreenView DescribeHub(GameCoordinator game, Character hero)
        {
            var view = new ScreenView { Title = "Game Hub" };
            view.Body.AddRange(CharacterSheet(hero));
            view.Choices.Add(new AgentChoice { Input = "1", Label = "Dungeon selection", Recommended = true });
            view.Choices.Add(new AgentChoice { Input = "2", Label = "Inventory (equip gear, edit action sequence, trade up)" });
            int points = AvailableSkillPoints(hero);
            view.Choices.Add(new AgentChoice { Input = "3", Label = points > 0 ? $"Skill tree ({points} point(s) to spend)" : "Skill tree" });
            view.Choices.Add(new AgentChoice { Input = "4", Label = "Region travel (each region is a fixed level band)" });
            view.Choices.Add(new AgentChoice { Input = "0", Label = "Save and return to main menu" });
            return view;
        }

        // ---------- Inventory and its prompts ----------

        private static ScreenView DescribeInventory(GameCoordinator game, Character hero)
        {
            var handler = game.GetInventoryMenuHandler();
            var st = handler?.StateTracker;
            var inventory = game.CurrentInventory ?? hero.Inventory ?? new List<Item>();

            if (st == null)
                return MainInventory(hero, inventory, null);

            if (st.InComboManagement)
                return ActionSequence(hero, st);
            if (st.WaitingForComparisonChoice)
                return Comparison(hero, inventory, st);
            if (st.WaitingForItemSelection)
                return ItemPicker(hero, inventory, st);
            if (st.WaitingForSlotSelection)
                return UnequipPicker(hero);
            if (st.WaitingForRaritySelection)
                return TradeUpRarity(inventory);
            if (st.WaitingForTradeUpConfirmation)
                return TradeUpConfirm(hero, st);
            if (st.WaitingForMenuMutatingActionConfirmation)
                return ConfirmMenuChoice(st.PendingMutatingMenuChoice);

            return MainInventory(hero, inventory, st);
        }

        private static List<InventoryDisplayEntry> VisibleEntries(Character hero, List<Item> inventory, InventoryStateManager? st)
        {
            try
            {
                return InventoryScreenRenderer.BuildDisplayEntries(
                    inventory,
                    hero,
                    st?.ItemSortMode ?? InventoryItemSortMode.InventoryOrder,
                    st?.HideRequirementBlockedItems ?? false,
                    st?.InventoryEquipSlotFilter,
                    st?.InventoryMaterialFilter);
            }
            catch
            {
                return inventory.Select((it, i) => new InventoryDisplayEntry(i, it)).ToList();
            }
        }

        private static ScreenView MainInventory(Character hero, List<Item> inventory, InventoryStateManager? st)
        {
            var view = new ScreenView { Title = "Inventory" };
            view.Body.Add("Equipped:");
            foreach (var slot in McpText.EquipSlots)
            {
                var item = McpText.EquippedIn(hero, slot);
                view.Body.Add($"  {slot}: {(item == null ? "(empty)" : McpText.ItemShort(hero, item))}");
            }

            var entries = VisibleEntries(hero, inventory, st);
            view.Body.Add($"Bag ({inventory.Count} item(s){FilterNote(st)}):");
            if (entries.Count == 0)
                view.Body.Add("  (empty)");
            foreach (var e in entries)
                view.Body.Add($"  {e.DisplayNumber}. {McpText.ItemShort(hero, e.Item)}");

            view.Body.Add($"Action sequence ({hero.GetComboActions().Count}/{ComboSequenceMaxHelper.GetEffectiveMax(hero)} slots): "
                + string.Join(" > ", hero.GetComboActions().Select(a => a.Name)));

            view.Choices.Add(new AgentChoice { Input = "1", Label = "Equip an item (opens the bag picker, then a side-by-side comparison)", Recommended = inventory.Count > 0 });
            view.Choices.Add(new AgentChoice { Input = HubTabBar.ActionsToken, Label = "Edit the action sequence (combo strip)" });
            view.Choices.Add(new AgentChoice { Input = "*", Label = "Auto-equip into empty slots" });
            view.Choices.Add(new AgentChoice { Input = "2", Label = "Unequip a slot (asks to confirm)" });
            view.Choices.Add(new AgentChoice { Input = "3", Label = "Discard an item (asks to confirm)" });
            view.Choices.Add(new AgentChoice { Input = "4", Label = "Trade up 5 items of one rarity for 1 of the next (asks to confirm)" });
            view.Choices.Add(new AgentChoice { Input = "+", Label = "Cycle bag sort order" });
            view.Choices.Add(new AgentChoice { Input = "-", Label = "Toggle hiding items with unmet requirements" });
            view.Choices.Add(new AgentChoice { Input = "/", Label = "Cycle bag slot filter" });
            view.Choices.Add(new AgentChoice { Input = "m", Label = "Cycle bag material filter" });
            view.Choices.Add(new AgentChoice { Input = "0", Label = "Back to the hub (dungeons, skill tree, travel)", Recommended = inventory.Count == 0 });
            view.Hints.Add("Call get_item_details with a bag number or slot name for the full item tooltip.");
            return view;
        }

        private static string FilterNote(InventoryStateManager? st)
        {
            if (st == null) return "";
            var parts = new List<string>();
            if (st.ItemSortMode != InventoryItemSortMode.InventoryOrder) parts.Add($"sort {st.ItemSortMode}");
            if (st.HideRequirementBlockedItems) parts.Add("hiding unmet requirements");
            if (!string.IsNullOrEmpty(st.InventoryEquipSlotFilter)) parts.Add($"slot {st.InventoryEquipSlotFilter}");
            if (!string.IsNullOrEmpty(st.InventoryMaterialFilter)) parts.Add($"material {st.InventoryMaterialFilter}");
            return parts.Count == 0 ? "" : "; " + string.Join(", ", parts);
        }

        private static ScreenView ItemPicker(Character hero, List<Item> inventory, InventoryStateManager st)
        {
            bool discard = st.ItemSelectionAction == "discard";
            var view = new ScreenView { Title = "Inventory", SubScreen = discard ? "DiscardPicker" : "EquipPicker" };
            view.Body.Add(discard ? "Select the item to discard:" : "Select the item to equip (a comparison with your current item opens next):");
            var entries = VisibleEntries(hero, inventory, st).Take(ItemSelectionRenderer.MaxSelectableItems).ToList();
            for (int i = 0; i < entries.Count; i++)
            {
                var item = entries[i].Item;
                var current = McpText.EquippedIn(hero, McpText.SlotOf(item));
                string vs = current == null ? "slot empty" : $"replaces {current.Name}";
                view.Body.Add($"  {i + 1}. {McpText.ItemShort(hero, item)} — {vs}");
                view.Choices.Add(new AgentChoice { Input = (i + 1).ToString(), Label = $"{item.Name} ({vs})" });
            }
            view.Choices.Add(new AgentChoice { Input = "0", Label = "Cancel" });
            return view;
        }

        private static ScreenView Comparison(Character hero, List<Item> inventory, InventoryStateManager st)
        {
            var view = new ScreenView { Title = "Inventory", SubScreen = "Compare" };
            Item? newItem = st.SelectedItemIndex >= 0 && st.SelectedItemIndex < inventory.Count ? inventory[st.SelectedItemIndex] : null;
            Item? current = string.IsNullOrEmpty(st.SelectedSlot) ? null : McpText.EquippedIn(hero, st.SelectedSlot);

            view.Body.Add($"NEW ({st.SelectedSlot}):");
            view.Body.AddRange(McpText.ItemLines(hero, newItem, "New").Select(l => "  " + l));
            view.Body.Add("CURRENT:");
            if (current == null)
                view.Body.Add("  (slot empty)");
            else
                view.Body.AddRange(McpText.ItemLines(hero, current, "Equipped").Select(l => "  " + l));

            bool blocked = newItem != null && !newItem.MeetsRequirements(hero);
            view.Choices.Add(new AgentChoice { Input = "2", Label = blocked ? "Equip new (refused: requirements not met)" : "Equip the new item", Recommended = !blocked });
            view.Choices.Add(new AgentChoice { Input = "1", Label = "Keep the current item", Recommended = blocked });
            view.Choices.Add(new AgentChoice { Input = "3", Label = newItem?.ExcludeFromTradeUp == true ? "Unlock the new item for trade-up" : "Lock the new item out of trade-up" });
            view.Choices.Add(new AgentChoice { Input = "0", Label = "Cancel" });
            return view;
        }

        private static ScreenView UnequipPicker(Character hero)
        {
            var view = new ScreenView { Title = "Inventory", SubScreen = "UnequipPicker" };
            for (int i = 0; i < McpText.EquipSlots.Length; i++)
            {
                string slot = McpText.EquipSlots[i];
                var item = McpText.EquippedIn(hero, slot);
                string label = $"{slot}: {(item == null ? "(empty)" : item.Name)}";
                view.Body.Add($"  {i + 1}. {label}");
                if (item != null)
                    view.Choices.Add(new AgentChoice { Input = (i + 1).ToString(), Label = $"Unequip {label}" });
            }
            view.Choices.Add(new AgentChoice { Input = "0", Label = "Cancel", Recommended = true });
            return view;
        }

        private static ScreenView TradeUpRarity(List<Item> inventory)
        {
            var view = new ScreenView { Title = "Inventory", SubScreen = "TradeUpRarity" };
            var groups = inventory
                .Where(InventoryTradeUpHandler.IsTradeUpCandidate)
                .GroupBy(item => item.Rarity ?? "Common")
                .Where(g => g.Count() >= 5)
                .OrderBy(g => RarityRank(g.Key))
                .Where(g => RarityRank(g.Key) >= 0 && RarityRank(g.Key) < RarityOrder.Length - 1)
                .ToList();
            view.Body.Add("Trade 5 unlocked items of one rarity for 1 item of the next rarity (cannot be cancelled once started):");
            for (int i = 0; i < groups.Count; i++)
            {
                string next = RarityOrder[RarityRank(groups[i].Key) + 1];
                string label = $"{groups[i].Count()} {groups[i].Key} → 1 {next}";
                view.Body.Add($"  {i + 1}. {label}");
                view.Choices.Add(new AgentChoice { Input = (i + 1).ToString(), Label = label, Recommended = i == 0 });
            }
            return view;
        }

        private static int RarityRank(string rarity) =>
            Array.FindIndex(RarityOrder, r => string.Equals(r, rarity, StringComparison.OrdinalIgnoreCase));

        private static ScreenView TradeUpConfirm(Character hero, InventoryStateManager st)
        {
            var view = new ScreenView { Title = "Inventory", SubScreen = "TradeUpConfirm" };
            view.Body.Add($"Trade up {st.SelectedTradeUpRarity} items.");
            if (st.PreviewTradeUpItem != null)
            {
                view.Body.Add("Preview:");
                view.Body.AddRange(McpText.ItemLines(hero, st.PreviewTradeUpItem, "Trade-up").Select(l => "  " + l));
            }
            view.Choices.Add(new AgentChoice { Input = "1", Label = "Confirm the trade-up", Recommended = true });
            return view;
        }

        private static ScreenView ConfirmMenuChoice(string pending)
        {
            string what = pending switch
            {
                "2" => "unequip a slot",
                "3" => "discard an item",
                "4" => "trade up items",
                _ => "continue"
            };
            var view = new ScreenView { Title = "Inventory", SubScreen = "ConfirmMenuChoice" };
            view.Body.Add($"Confirm: {what}?");
            view.Choices.Add(new AgentChoice { Input = "1", Label = $"Yes, {what}", Recommended = true });
            view.Choices.Add(new AgentChoice { Input = "0", Label = "Cancel" });
            return view;
        }

        // ---------- Action sequence (combo strip) editor ----------

        private static ScreenView ActionSequence(Character hero, InventoryStateManager st)
        {
            var view = new ScreenView { Title = "Inventory", SubScreen = "ActionSequence" };
            var combo = hero.GetComboActions();
            var pool = hero.GetActionPool();
            int cap = ComboSequenceMaxHelper.GetEffectiveMax(hero);

            view.Body.Add($"Sequence ({combo.Count}/{cap} slots, played left to right; each slot must land its combo roll to advance):");
            for (int i = 0; i < combo.Count; i++)
                view.Body.Add($"  slot {i + 1}: {McpText.ActionShort(hero, combo[i])}"
                    + (WeaponRequiredComboAction.CanRemoveFromCombo(hero, combo[i]) ? "" : "  [required by your weapon]"));
            if (combo.Count == 0)
                view.Body.Add("  (empty)");

            view.Body.Add("Action pool (from gear, class and skills):");
            for (int i = 0; i < pool.Count; i++)
            {
                bool inSeq = combo.Any(a => ReferenceEquals(a, pool[i]));
                view.Body.Add($"  pool {i + 1}: {McpText.ActionShort(hero, pool[i])}{(inSeq ? "  [in sequence]" : "")}");
            }

            if (st.WaitingForComboActionSelection)
            {
                view.SubScreen = "ActionSequence/AddPicker";
                for (int i = 0; i < pool.Count; i++)
                    view.Choices.Add(new AgentChoice { Input = (i + 1).ToString(), Label = $"Add {pool[i].Name}" });
                view.Choices.Add(new AgentChoice { Input = "0", Label = "Cancel" });
                return view;
            }
            if (st.WaitingForComboRemoveSelection)
            {
                view.SubScreen = "ActionSequence/RemovePicker";
                for (int i = 0; i < combo.Count; i++)
                    view.Choices.Add(new AgentChoice { Input = (i + 1).ToString(), Label = $"Remove slot {i + 1} ({combo[i].Name})" });
                view.Choices.Add(new AgentChoice { Input = "0", Label = "Cancel" });
                return view;
            }
            if (st.WaitingForComboReorderInput)
            {
                view.SubScreen = "ActionSequence/Reorder";
                view.Body.Add($"Reorder entered so far: \"{st.ReorderInputSequence}\" — type each slot number once in the new order, then 0.");
                for (int i = 1; i <= Math.Min(9, combo.Count); i++)
                    if (!st.ReorderInputSequence.Contains(i.ToString()))
                        view.Choices.Add(new AgentChoice { Input = i.ToString(), Label = $"Next: slot {i} ({combo[i - 1].Name})" });
                view.Choices.Add(new AgentChoice { Input = "0", Label = "Apply the order (cancels if nothing entered)" });
                view.Choices.Add(new AgentChoice { Input = "cancel", Label = "Cancel reorder" });
                return view;
            }

            for (int i = 0; i < pool.Count; i++)
            {
                bool inSeq = combo.Any(a => ReferenceEquals(a, pool[i]));
                if (combo.Count < cap)
                    view.Choices.Add(new AgentChoice { Input = $"{ComboPointerInput.Prefix}pool:{i}", Label = $"Append {pool[i].Name} to the end" });
                else if (!inSeq && pool[i].IsComboAction)
                {
                    // Full strip: offer swapping each unused pool action into each slot whose action may leave.
                    for (int s = 0; s < combo.Count; s++)
                        if (WeaponRequiredComboAction.CanRemoveFromCombo(hero, combo[s]))
                        view.Choices.Add(new AgentChoice
                        {
                            Input = $"{ComboPointerInput.Prefix}replace:{i}:{s}",
                            Label = $"Put {pool[i].Name} in slot {s + 1} (replaces {combo[s].Name})"
                        });
                }
            }
            if (combo.Count >= cap)
                view.Body.Add($"The sequence is full ({cap} slots). Swap actions in, or raise capacity with gear (feet) and levels.");
            for (int i = 0; i < combo.Count; i++)
                if (WeaponRequiredComboAction.CanRemoveFromCombo(hero, combo[i]))
                    view.Choices.Add(new AgentChoice { Input = $"{ComboPointerInput.Prefix}rm:{i}", Label = $"Remove slot {i + 1} ({combo[i].Name})" });
            view.Choices.Add(new AgentChoice { Input = $"{ComboPointerInput.Prefix}addall", Label = "Fill the sequence with every pool action" });
            view.Choices.Add(new AgentChoice { Input = "0", Label = "Done — back to inventory", Recommended = true });

            view.Hints.Add("Tokens use 0-based numbers: slot N above is index N-1, pool N is index N-1.");
            view.Hints.Add($"Move slot: {ComboPointerInput.Prefix}move:FROM:TO   Swap slots: {ComboPointerInput.Prefix}swap:A:B");
            view.Hints.Add($"Insert pool action at slot: {ComboPointerInput.Prefix}insert:POOL:SLOT   Replace slot with pool action: {ComboPointerInput.Prefix}replace:POOL:SLOT");
            view.Hints.Add("The sequence cannot be changed during a dungeon run.");
            return view;
        }

        // ---------- Skill tree ----------

        private static ScreenView DescribeSkillTree(GameCoordinator game, Character hero)
        {
            var view = new ScreenView { Title = "Skill Tree" };
            var handler = game.GetSkillTreeMenuHandler();
            var progression = hero.Progression;
            var model = SkillTreeService.BuildDisplayModel(progression);
            var nodes = model.AllNodes;
            var path = SkillTreeService.GetViewedPath(progression);

            view.Body.Add($"Viewing {path?.ToString() ?? "no"} tree" + (path != null ? $", {progression.GetAvailableSkillPoints(path.Value)} point(s) available." : "."));
            if (!string.IsNullOrEmpty(handler?.StatusMessage))
                view.Body.Add($"Status: {handler!.StatusMessage}");
            int selected = handler?.SelectedIndex ?? 0;

            for (int i = 0; i < nodes.Count; i++)
            {
                var n = nodes[i];
                var state = SkillTreeService.GetNodeState(progression, n);
                int rank = progression.GetSkillRank(n.Id);
                string rankText = n.MaxRank > 1 ? $" rank {rank}/{n.MaxRank}" : (rank > 0 ? " learned" : "");
                string rail = model.IsRailIndex(i) ? $" [{model.RailTitle ?? "shared rail"}]" : "";
                string sel = i == selected ? " <selected>" : "";
                string effect = string.IsNullOrWhiteSpace(n.Effect) ? n.Payoff : n.Effect;
                string unlock = string.IsNullOrEmpty(n.UnlockActionName) ? "" : $" Unlocks action {n.UnlockActionName}.";
                view.Body.Add($"  {i + 1}. {n.Name} (tier {n.Tier}, {n.Branch}, cost {n.Cost}, {state}{rankText}){rail}{sel}: {effect}{unlock}");
            }

            var available = Enumerable.Range(0, nodes.Count)
                .Where(i => SkillTreeService.GetNodeState(progression, nodes[i]) == SkillTreeService.NodeViewState.Available)
                .ToList();
            if (selected >= 0 && selected < nodes.Count && available.Contains(selected))
                view.Choices.Add(new AgentChoice { Input = "l", Label = $"Learn the selected node ({nodes[selected].Name})", Recommended = true });
            foreach (int i in available)
                if (i != selected)
                    view.Choices.Add(new AgentChoice { Input = (i + 1).ToString(), Label = $"Select {nodes[i].Name} (then send l to learn)" });
            if (SkillTreeService.GetViewablePaths(progression).Count > 1)
                view.Choices.Add(new AgentChoice { Input = "t", Label = "Switch to another weapon path's tree" });
            view.Choices.Add(new AgentChoice { Input = "0", Label = "Back to the hub", Recommended = available.Count == 0 });
            view.Hints.Add("Any node number selects it; only Available nodes can be learned.");
            return view;
        }

        // ---------- Region travel ----------

        private static ScreenView DescribeTravel(GameCoordinator game, Character hero)
        {
            var view = new ScreenView { Title = "Region Travel" };
            var catalog = new TravelRegionCatalog();
            var here = catalog.GetRegionForCharacter(hero);
            var regions = RegionTravelRenderer.OrderRegions(catalog.GetAllRegions());
            view.Body.Add($"You are in {here.DisplayName} (levels {here.MinLevel}-{here.MaxLevel}). Dungeons roll near the region's level band.");
            for (int i = 0; i < regions.Count; i++)
            {
                var r = regions[i];
                bool isHere = string.Equals(r.Id, here.Id, StringComparison.OrdinalIgnoreCase);
                string line = $"{r.DisplayName} (levels {r.MinLevel}-{r.MaxLevel}, {r.Theme})";
                view.Body.Add($"  {i + 1}. {line}{(isHere ? "  <you are here>" : "")}");
                if (!isHere)
                    view.Choices.Add(new AgentChoice
                    {
                        Input = (i + 1).ToString(),
                        Label = $"Travel to {line}",
                        Recommended = hero.Level >= r.MinLevel && hero.Level <= r.MaxLevel
                    });
            }
            view.Choices.Add(new AgentChoice { Input = "0", Label = "Back" });
            return view;
        }

        // ---------- End of run ----------

        private static ScreenView DescribeCompletion(Character hero)
        {
            var view = new ScreenView { Title = "Dungeon Complete" };
            view.Body.AddRange(CharacterSheet(hero));
            view.Choices.Add(new AgentChoice { Input = "1", Label = "Dungeon selection", Recommended = true });
            view.Choices.Add(new AgentChoice { Input = "2", Label = "Inventory (check new loot)" });
            view.Choices.Add(new AgentChoice { Input = "3", Label = "Skill tree" });
            view.Choices.Add(new AgentChoice { Input = "4", Label = "Region travel" });
            view.Choices.Add(new AgentChoice { Input = "0", Label = "Save and return to main menu" });
            return view;
        }

        private static ScreenView DescribeDeath(GameCoordinator game, Character hero)
        {
            var view = new ScreenView { Title = "Death" };
            view.Body.Add($"{hero.Name} fell at level {hero.Level}" + (game.CurrentDungeon != null ? $" in {game.CurrentDungeon.Name}." : "."));
            view.Choices.Add(new AgentChoice { Input = "1", Label = "Clone this hero (keeps level and gear) and return to the hub", Recommended = true });
            view.Choices.Add(new AgentChoice { Input = "0", Label = "Return to the main menu (hero becomes a tombstone)" });
            return view;
        }

        // ---------- Shared ----------

        public static List<string> CharacterSheet(Character hero)
        {
            var lines = new List<string>
            {
                $"{hero.Name}, level {hero.Level}, {hero.CurrentHealth}/{hero.GetEffectiveMaxHealth()} HP, XP {hero.XP}.",
                $"STR {hero.Strength}  AGI {hero.Agility}  TEC {hero.Technique}  INT {hero.Intelligence}"
            };
            try
            {
                var region = new TravelRegionCatalog().GetRegionForCharacter(hero);
                lines.Add($"Region: {region.DisplayName} (levels {region.MinLevel}-{region.MaxLevel}).");
            }
            catch { }
            foreach (var slot in McpText.EquipSlots)
            {
                var item = McpText.EquippedIn(hero, slot);
                if (item != null)
                    lines.Add($"  {slot}: {McpText.ItemShort(hero, item)}");
            }
            var combo = hero.GetComboActions();
            lines.Add($"Action sequence ({combo.Count}/{ComboSequenceMaxHelper.GetEffectiveMax(hero)}): {string.Join(" > ", combo.Select(a => a.Name))}");
            int points = AvailableSkillPoints(hero);
            if (points > 0)
                lines.Add($"Unspent skill points: {points}.");
            return lines;
        }

        public static int AvailableSkillPoints(Character hero)
        {
            try
            {
                var path = SkillTreeService.GetViewedPath(hero.Progression);
                return path == null ? 0 : hero.Progression.GetAvailableSkillPoints(path.Value);
            }
            catch { return 0; }
        }
    }
}
