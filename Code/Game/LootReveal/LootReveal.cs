using System;
using System.Collections.Generic;
using System.Linq;
using RPGGame.Entity.Services;
using RPGGame.UI;

namespace RPGGame.LootReveal
{
    public enum LootVerdict { Upgrade, Sidegrade, Worse, NewSlot }

    public enum LootChoice { Equip, Keep, Salvage }

    /// <summary>One revealed item: its beats in order, its verdict and the pre-marked choice.</summary>
    public sealed class LootRevealEntry
    {
        public LootRevealEntry(Item item, string slot, List<string> beats, LootVerdict verdict, string verdictLine, LootChoice suggested, bool fast)
        {
            Item = item;
            Slot = slot;
            Beats = beats;
            Verdict = verdict;
            VerdictLine = verdictLine;
            Suggested = suggested;
            Fast = fast;
        }

        public Item Item { get; }
        /// <summary>Equipment slot the item goes in (<c>weapon</c>, <c>head</c>…), or empty when it can't be worn.</summary>
        public string Slot { get; }
        /// <summary>The staged beats, in order (D4.5): the drop, what it is, how it compares, its action, its modifier, its rule.</summary>
        public List<string> Beats { get; }
        public LootVerdict Verdict { get; }
        public string VerdictLine { get; }
        /// <summary>The choice the screen pre-marks: Equip for an upgrade or an empty slot, Salvage for a worse Common, else Keep.</summary>
        public LootChoice Suggested { get; }
        /// <summary>Commons reveal fast and in a batch: all their beats at once.</summary>
        public bool Fast { get; }
    }

    /// <summary>
    /// Rebuild 2.4 (D4.5, Phase 4 proposal 6): the end-of-dungeon loot moment. Each item reveals in steps, higher
    /// rarities taking longer; one comparison against what you wear (one line per attribute, a set gained or broken);
    /// then a verdict and Equip / Keep / Salvage.
    /// </summary>
    public static class LootRevealBuilder
    {
        private static readonly string[] RarityOrder = { "Common", "Uncommon", "Rare", "Epic", "Legendary", "Mythic" };

        public static int RarityRank(string? rarity)
        {
            int i = Array.FindIndex(RarityOrder, r => r.Equals((rarity ?? "Common").Trim(), StringComparison.OrdinalIgnoreCase));
            return i < 0 ? 0 : i;
        }

        /// <summary>Reveal order: Commons first (fast, as a batch), then the rest by rarity, the best last.</summary>
        public static List<LootRevealEntry> BuildAll(Character hero, IEnumerable<Item> items) =>
            items.Where(i => i != null)
                .OrderBy(i => RarityRank(i.Rarity))
                .Select(i => Build(hero, i))
                .ToList();

        public static LootRevealEntry Build(Character hero, Item item)
        {
            int rank = RarityRank(item.Rarity);
            string slot = RPGGame.UI.Avalonia.Layout.InventoryActionPoolEntries.GetEquipSlotForItem(item) ?? "";
            var current = string.IsNullOrEmpty(slot) ? null : hero.Equipment.GetSlotItem(slot);
            var beats = new List<string>();

            // 1. The drop: Rare and up get a beat of anticipation.
            if (rank >= 2)
                beats.Add("...");
            beats.Add(rank switch
            {
                0 => "A Common drops.",
                1 => "An Uncommon drops!",
                2 => "A RARE drops!",
                3 => "An EPIC drops!",
                4 => "A LEGENDARY drops!",
                _ => "A MYTHIC drops!!"
            });

            // 2. What it is.
            string tier = item.Tier > 0 ? $" · Tier {item.Tier}" : "";
            string material = string.IsNullOrWhiteSpace(item.Material) ? "" : $" · {item.Material}";
            beats.Add($"{item.Name} — {item.Rarity} {SlotWord(slot)}{material}{tier}");

            // 3. How it compares: one line per attribute, then sets.
            var compare = CompareLines(hero, item, slot, out double gain, out double loss);
            beats.Add(current == null ? "Your slot is empty: " + (compare.Count > 0 ? string.Join("  ", compare) : "no stats")
                : $"vs {current.Name}: " + (compare.Count > 0 ? string.Join("  ", compare) : "the same"));
            var setLines = SetLines(hero, item, slot, out string? brokenBonus);
            beats.AddRange(setLines);

            // 4. Its action (Uncommon+), 5. its modifier, then its unique rule (Legendary+): each its own beat.
            if (rank >= 1)
            {
                foreach (string action in GearActionNames.Resolve(item).Where(a => !IsRequiredBasic(item, a)))
                {
                    var data = ActionLoader.GetActionData(action);
                    string desc = RuleText.Plain(data?.Description);
                    beats.Add(desc.Length > 0 ? $"▸ {action.ToUpperInvariant()} — {desc}" : $"▸ {action.ToUpperInvariant()}");
                }
                foreach (string rule in RuleText.Lines(item.ImplicitRule, item.Name))
                    beats.Add($"◆ {rule}");
            }
            if (rank >= 4)
            {
                foreach (var mod in item.Modifications.Where(m => AffixTemplates.Is(m, CrossWires.RuleEffect)))
                    if (CrossWires.DescribeMod(mod) is { Length: > 0 } unique)
                        beats.Add($"★ {RuleText.Plain(unique)}");
            }

            // 6. The verdict.
            LootVerdict verdict;
            string why;
            if (string.IsNullOrEmpty(slot))
            {
                verdict = LootVerdict.Worse;
                why = "Can't be worn.";
            }
            else if (current == null)
            {
                verdict = LootVerdict.NewSlot;
                why = "Upgrade: fills an empty slot.";
            }
            else if (brokenBonus != null && gain > 0)
            {
                // Mock 3: breaking a set step outweighs the stat gain; keep what you wear for now.
                verdict = LootVerdict.Sidegrade;
                why = $"Sidegrade: {compare.FirstOrDefault(c => c.StartsWith('+')) ?? "better stats"}, but you lose {brokenBonus}.";
            }
            else if (gain > 0 && loss == 0 || rank > RarityRank(current.Rarity) && gain >= loss)
            {
                verdict = LootVerdict.Upgrade;
                why = "Upgrade.";
            }
            else if (gain > 0)
            {
                verdict = LootVerdict.Sidegrade;
                why = $"Sidegrade: {compare.FirstOrDefault(c => c.StartsWith('+')) ?? "gains"}, but {compare.FirstOrDefault(c => c.StartsWith('−')) ?? "losses"}.";
            }
            else if (NewActionForEmptySlot(hero, item) is { } fresh)
            {
                // An action for an empty strip slot is the item's point at this rung (the second slot, D5.6).
                verdict = LootVerdict.Sidegrade;
                why = $"Sidegrade: brings {fresh} for your empty strip slot{(compare.FirstOrDefault(c => c.StartsWith('−')) is { } cost ? $", but {cost}" : "")}.";
            }
            else
            {
                verdict = LootVerdict.Worse;
                why = "Worse than what you wear.";
            }
            if (!item.MeetsRequirements(hero))
                why += " (You don't meet its requirements yet.)";
            beats.Add(why);

            var suggested = verdict is LootVerdict.Upgrade or LootVerdict.NewSlot && item.MeetsRequirements(hero)
                ? LootChoice.Equip
                : verdict == LootVerdict.Worse && rank == 0 ? LootChoice.Salvage
                : LootChoice.Keep;
            return new LootRevealEntry(item, slot, beats, verdict, why, suggested, fast: rank == 0);
        }

        /// <summary>
        /// "+3 STR  −2 Defense  +12 Max HP": the hero's numbers with the item worn minus without it, one per attribute
        /// (base, material and suffix already summed). Computed on a copy of the hero so nothing changes.
        /// </summary>
        public static List<string> CompareLines(Character hero, Item item, string slot, out double gain, out double loss)
        {
            gain = 0;
            loss = 0;
            var lines = new List<string>();
            if (hero == null || string.IsNullOrEmpty(slot))
                return lines;
            Character? copy;
            try
            {
                var serializer = new CharacterSerializer();
                copy = serializer.CreateCharacterFromSaveData(serializer.Deserialize(serializer.Serialize(hero))!);
            }
            catch
            {
                return lines;
            }
            var before = Snapshot(copy);
            if (!copy.TryEquipItem(item, slot, out _, out _, ignoreAttributeRequirements: true))
                return lines;
            var after = Snapshot(copy);
            foreach (var (label, b) in before)
            {
                double d = after[label] - b;
                if (Math.Abs(d) < 0.5)
                    continue;
                if (d > 0) gain += d; else loss -= d;
                lines.Add($"{(d > 0 ? "+" : "−")}{Math.Abs(d):0} {label}");
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

        /// <summary>The next set step only (Mock 3 / Phase 4 proposal 3): "Iron 1 → 2 pieces" or "breaks your Leather set (3 → 2)".</summary>
        public static List<string> SetLines(Character hero, Item item, string slot) => SetLines(hero, item, slot, out _);

        /// <summary>Set steps a piece count unlocks (the 2 / 3 / 5 material ladder).</summary>
        private static readonly int[] SetSteps = { 2, 3, 5 };

        /// <param name="brokenBonus">Set when wearing the item drops a worn set below one of its steps ("Lucky", "your Iron 2-piece bonus").</param>
        public static List<string> SetLines(Character hero, Item item, string slot, out string? brokenBonus)
        {
            brokenBonus = null;
            var lines = new List<string>();
            if (hero == null || string.IsNullOrEmpty(slot))
                return lines;
            var worn = new[] { "weapon", "head", "body", "legs", "feet" }.Select(s => hero.Equipment.GetSlotItem(s)).Where(i => i != null).ToList();
            var current = hero.Equipment.GetSlotItem(slot);
            int Count(string m) => worn.Count(i => string.Equals(i!.Material, m, StringComparison.OrdinalIgnoreCase));
            string mat = item.Material ?? "";
            if (mat.Length > 0 && !string.Equals(current?.Material, mat, StringComparison.OrdinalIgnoreCase))
            {
                int now = Count(mat);
                lines.Add($"{mat} {now} → {now + 1} pieces worn");
            }
            string lost = current?.Material ?? "";
            if (lost.Length > 0 && !string.Equals(lost, mat, StringComparison.OrdinalIgnoreCase))
            {
                int now = Count(lost);
                if (now >= 2)
                {
                    bool crossesStep = SetSteps.Contains(now);
                    if (crossesStep)
                        brokenBonus = string.Equals(lost, "Leather", StringComparison.OrdinalIgnoreCase) && now == 3
                            ? "Lucky (2d20, keep the higher)"
                            : $"your {lost} {now}-piece bonus";
                    lines.Add(crossesStep ? $"breaks your {lost} set ({now} → {now - 1}): you lose {brokenBonus}" : $"breaks your {lost} set ({now} → {now - 1})");
                }
            }
            return lines;
        }

        /// <summary>The first action <paramref name="item"/> brings that the hero lacks, when their strip has a free slot; else null.</summary>
        public static string? NewActionForEmptySlot(Character hero, Item item)
        {
            if (hero == null)
                return null;
            int max = 0;
            try { max = ComboSequenceMaxHelper.GetEffectiveMax(hero); } catch { return null; }
            if (hero.GetComboActions().Count >= max)
                return null;
            var have = new HashSet<string>(hero.GetComboActions().Select(a => a.Name), StringComparer.OrdinalIgnoreCase);
            foreach (var name in GearActionNames.Resolve(hero.Equipment.GetSlotItem("weapon")))
                have.Add(name);
            return GearActionNames.Resolve(item)
                .Where(a => !IsRequiredBasic(item, a) && !have.Contains(a))
                .Select(a => a.ToUpperInvariant())
                .FirstOrDefault();
        }

        private static bool IsRequiredBasic(Item item, string action) =>
            GearActionNames.TryResolveWeaponType(item, out var wt)
            && string.Equals(WeaponRequiredComboAction.TryGetRequiredBasicActionName(wt), action, StringComparison.OrdinalIgnoreCase);

        private static string SlotWord(string slot) => slot switch
        {
            "weapon" => "weapon",
            "head" => "head",
            "body" => "body",
            "legs" => "legs",
            "feet" => "feet",
            _ => "item"
        };
    }

    /// <summary>
    /// The reveal in progress at the end of a dungeon (Rebuild 2.4). <see cref="Advance"/> shows the next beat (a
    /// Common shows all of its beats at once); once the verdict is out, <see cref="Choose"/> applies Equip / Keep /
    /// Salvage and moves to the next item. The bag already holds every item (rewards land on completion), so Keep
    /// leaves it there, Salvage removes it and Equip wears it (the old piece goes to the bag).
    /// </summary>
    public sealed class LootRevealSession
    {
        /// <summary>The reveal the completion screen is showing (one at a time; null when none).</summary>
        public static LootRevealSession? Active { get; private set; }

        /// <summary>Switch: false skips the reveal and keeps the old completion screen (loot just lands in the bag).</summary>
        public static bool Enabled { get; set; } = true;

        private readonly Character hero;
        private readonly List<string> log = new();

        public LootRevealSession(Character hero, List<LootRevealEntry> entries)
        {
            this.hero = hero;
            Entries = entries;
            if (entries.Count > 0)
                StartEntry();
        }

        /// <summary>Starts a reveal for <paramref name="items"/> and makes it <see cref="Active"/> (null when there is nothing to show).</summary>
        public static LootRevealSession? Start(Character hero, IEnumerable<Item> items)
        {
            Active = null;
            if (!Enabled || hero == null)
                return null;
            var list = items?.Where(i => i != null).ToList() ?? new List<Item>();
            if (list.Count == 0)
                return null;
            Active = new LootRevealSession(hero, LootRevealBuilder.BuildAll(hero, list));
            return Active;
        }

        public static void Clear() => Active = null;

        public List<LootRevealEntry> Entries { get; }
        public int Index { get; private set; }
        public int BeatsShown { get; private set; }
        public bool IsDone => Index >= Entries.Count;
        public LootRevealEntry? Current => IsDone ? null : Entries[Index];
        /// <summary>True when the current item's verdict is out and a choice is due.</summary>
        public bool AwaitingChoice => Current is { } e && BeatsShown >= e.Beats.Count;
        /// <summary>What happened to items already decided ("Equipped Iron Coat.").</summary>
        public IReadOnlyList<string> Log => log;

        private void StartEntry()
        {
            BeatsShown = Current is { Fast: true } e ? e.Beats.Count : Math.Min(1, Current?.Beats.Count ?? 0);
        }

        /// <summary>Shows the next beat. Returns false when the verdict is already out.</summary>
        public bool Advance()
        {
            if (Current is not { } e || BeatsShown >= e.Beats.Count)
                return false;
            BeatsShown++;
            return true;
        }

        /// <summary>Reveals every remaining beat of the current item (skip).</summary>
        public void RevealAll()
        {
            if (Current is { } e)
                BeatsShown = e.Beats.Count;
        }

        /// <summary>Applies <paramref name="choice"/> to the current item and moves on. Returns the line saying what happened.</summary>
        public string Choose(LootChoice choice)
        {
            if (Current is not { } e)
                return "";
            string result;
            switch (choice)
            {
                case LootChoice.Equip when !string.IsNullOrEmpty(e.Slot):
                    if (hero.TryEquipItem(e.Item, e.Slot, out var previous, out var why))
                    {
                        hero.Inventory.Remove(e.Item);
                        if (previous != null)
                            hero.Inventory.Add(previous);
                        result = previous != null ? $"Equipped {e.Item.Name} (your {previous.Name} went to the bag)." : $"Equipped {e.Item.Name}.";
                    }
                    else
                        result = $"Couldn't equip {e.Item.Name}: {why ?? "requirements"}. Kept it.";
                    break;
                case LootChoice.Salvage:
                    hero.Inventory.Remove(e.Item);
                    result = $"Salvaged {e.Item.Name}.";
                    break;
                default:
                    result = $"Kept {e.Item.Name} in the bag.";
                    break;
            }
            log.Add(result);
            Index++;
            if (IsDone)
                Active = null;
            else
                StartEntry();
            return result;
        }

        private int emittedLog;
        private int emittedEntry = -1;
        private int emittedBeats;
        private bool emittedPrompt;

        public const string ChoicePrompt = "[E] Equip   [K] Keep   [S] Salvage";

        /// <summary>
        /// The lines not yet printed, for a screen that appends to its log: results of decided items, the next item's
        /// header, its newly revealed beats, then the choice prompt once the verdict is out.
        /// </summary>
        public List<string> TakeNewLines()
        {
            var lines = new List<string>();
            for (; emittedLog < log.Count; emittedLog++)
                lines.Add(log[emittedLog]);
            if (Current is not { } e)
            {
                if (lines.Count > 0)
                    lines.Add("That's all the loot. Continue to the hub.");
                return lines;
            }
            if (emittedEntry != Index)
            {
                emittedEntry = Index;
                emittedBeats = 0;
                emittedPrompt = false;
                lines.Add("");
                lines.Add($"LOOT {Index + 1} of {Entries.Count}");
            }
            for (; emittedBeats < BeatsShown && emittedBeats < e.Beats.Count; emittedBeats++)
                lines.Add(e.Beats[emittedBeats]);
            if (AwaitingChoice && !emittedPrompt)
            {
                emittedPrompt = true;
                lines.Add($"{ChoicePrompt}   (Continue = {e.Suggested})");
            }
            return lines;
        }

        /// <summary>
        /// One input from the completion screen while a reveal runs: E / K / S decide once the verdict is out;
        /// Continue shows the next beat, or applies the pre-marked choice on the verdict. Returns false for keys it ignores.
        /// </summary>
        public bool HandleInput(string? input)
        {
            string key = (input ?? "").Trim().ToLowerInvariant();
            if (Current is not { } e)
                return false;
            if (AwaitingChoice)
            {
                switch (key)
                {
                    case "e": Choose(LootChoice.Equip); return true;
                    case "k": Choose(LootChoice.Keep); return true;
                    case "s": Choose(LootChoice.Salvage); return true;
                    case "1": case "enter": case "continue": case "": Choose(e.Suggested); return true;
                    default: return false;
                }
            }
            if (key is "1" or "enter" or "continue" or "" or "e" or "k" or "s")
            {
                Advance();
                return true;
            }
            if (key == "skip")
            {
                RevealAll();
                return true;
            }
            return false;
        }

        /// <summary>The lines the screen shows for the current item: its revealed beats, then the choice when due.</summary>
        public List<string> CurrentLines()
        {
            var lines = new List<string>();
            if (Current is not { } e)
                return lines;
            lines.Add($"LOOT {Index + 1} of {Entries.Count}");
            lines.AddRange(e.Beats.Take(BeatsShown));
            if (AwaitingChoice)
                lines.Add($"[E] Equip   [K] Keep   [S] Salvage      (suggested: {e.Suggested})");
            return lines;
        }
    }
}
