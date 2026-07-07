using System;
using System.Collections.Generic;
using Avalonia.Media;
using RPGGame.Actions.RollModification;
using RPGGame.Items.Helpers;
using RPGGame.UI;
using RPGGame.UI.Avalonia.Layout;
using RPGGame.UI.ColorSystem;

namespace RPGGame
{
    /// <summary>
    /// Builds wrapped tooltip lines for left-panel STATS, GEAR, hero summary, thresholds, bag items (center list), etc.
    /// </summary>
    public static class LeftPanelTooltipBuilder
    {
        /// <summary>
        /// Colored tooltip lines for item hovers (gear slots and inventory indices). Empty when the hover key is not an item.
        /// </summary>
        public static List<List<ColoredText>> BuildColoredItemLines(Character? character, string fullHoverValue, int maxLines = 24)
        {
            if (character == null || maxLines < 1 ||
                string.IsNullOrEmpty(fullHoverValue) ||
                !fullHoverValue.StartsWith(LeftPanelHoverState.Prefix, StringComparison.Ordinal))
                return new List<List<ColoredText>>();

            string key = fullHoverValue.Substring(LeftPanelHoverState.Prefix.Length);
            var statLines = StatTooltipFormatter.TryBuild(character, key, maxLines);
            if (statLines != null && statLines.Count > 0)
                return statLines;

            return key switch
            {
                "gear:weapon" => BuildColoredGear(character, character.Weapon, "Weapon", maxLines),
                "gear:head" => BuildColoredGear(character, character.Head, "Head", maxLines),
                "gear:body" => BuildColoredGear(character, character.Body, "Body", maxLines),
                "gear:legs" => BuildColoredGear(character, character.Legs, "Legs", maxLines),
                "gear:feet" => BuildColoredGear(character, character.Feet, "Feet", maxLines),
                string invKey when invKey.StartsWith("inv:", StringComparison.Ordinal) => BuildColoredInventory(character, invKey, maxLines),
                _ => new List<List<ColoredText>>()
            };
        }

        private static List<List<ColoredText>> BuildColoredGear(Character c, Item? item, string slot, int maxLines)
        {
            if (item == null)
            {
                var empty = new List<List<ColoredText>>();
                var b = new ColoredTextBuilder();
                b.Add($"{slot} slot", Colors.DarkGray);
                empty.Add(b.Build());
                empty.Add(new List<ColoredText>());
                var b2 = new ColoredTextBuilder();
                b2.Add("No item equipped.", Colors.Gray);
                empty.Add(b2.Build());
                return empty;
            }
            return ItemTooltipFormatter.BuildItemTooltipLines(c, item, slot, maxLines);
        }

        private static List<List<ColoredText>> BuildColoredInventory(Character c, string invKey, int maxLines)
        {
            if (invKey.Length <= 4 || !int.TryParse(invKey.AsSpan(4), out int idx))
                return new List<List<ColoredText>>();
            var inv = c.Inventory;
            if (inv == null || idx < 0 || idx >= inv.Count)
                return new List<List<ColoredText>>();
            return ItemTooltipFormatter.BuildItemTooltipLines(c, inv[idx], "Inventory", maxLines);
        }

        /// <summary>
        /// Builds lines for a full hover value including <see cref="UI.LeftPanelHoverState.Prefix"/>.
        /// </summary>
        public static List<string> BuildLines(Character? character, string fullHoverValue, int maxWidth, int maxLines = 18)
        {
            var result = new List<string>();
            if (character == null || maxWidth < 4 || maxLines < 1)
                return result;

            if (string.IsNullOrEmpty(fullHoverValue) ||
                !fullHoverValue.StartsWith(LeftPanelHoverState.Prefix, StringComparison.Ordinal))
                return result;

            string key = fullHoverValue.Substring(LeftPanelHoverState.Prefix.Length);
            void AddWrapped(string? paragraph)
            {
                CombatActionStripBuilder.AddWrappedTooltipParagraph(result, paragraph, maxWidth, maxLines);
            }

            switch (key)
            {
                case "stat:damage":
                    AppendDamage(character, result, AddWrapped, maxLines);
                    break;
                case "stat:speed":
                    AppendSpeed(character, result, AddWrapped, maxLines);
                    break;
                case "stat:amp":
                    AppendAmp(character, result, AddWrapped, maxLines);
                    break;
                case "stat:armor":
                    AppendArmor(character, result, AddWrapped, maxLines);
                    break;
                case "stat:actionslots":
                {
                    int slots = ComboSequenceMaxHelper.GetEffectiveMax(character);
                    int labBonus = character.ActionLabActionSlotBonus;
                    string labNote = labBonus > 0 ? $" Includes +{labBonus} Action Lab bonus." : "";
                    AppendSimpleStat(character, "ACTION SLOTS", $"Max combo sequence length: {slots}.{labNote}", result, AddWrapped, maxLines);
                    break;
                }
                case "stat:str":
                    AppendPrimaryStat(character, "STR", "Strength", result, AddWrapped, maxLines);
                    break;
                case "stat:agi":
                    AppendPrimaryStat(character, "AGI", "Agility", result, AddWrapped, maxLines);
                    break;
                case "stat:tec":
                    AppendPrimaryStat(character, "TEC", "Technique", result, AddWrapped, maxLines);
                    break;
                case "stat:int":
                    AppendPrimaryStat(character, "INT", "Intelligence", result, AddWrapped, maxLines);
                    break;
                case "stat:magfind":
                    AppendMagFind(character, result, AddWrapped, maxLines);
                    break;
                case "stat:hpregen":
                    AppendSimpleStat(character, "HP REGEN", $"+{character.GetEquipmentHealthRegenBonus()}/turn from equipment.", result, AddWrapped, maxLines);
                    break;
                case "stat:lifesteal":
                    AppendSimpleStat(character, "LIFESTEAL", $"{character.GetModificationLifesteal():P0} from modifications.", result, AddWrapped, maxLines);
                    break;
                case "stat:bleed":
                    AppendSimpleStat(character, "BLEED", $"+{character.GetWeaponBleedPerHit()} on critical hit from weapon mods.", result, AddWrapped, maxLines);
                    break;
                case "stat:burn":
                    AppendSimpleStat(character, "BURN", $"+{character.GetWeaponBurnPerHit()} on critical hit from weapon mods.", result, AddWrapped, maxLines);
                    break;
                case "stat:poison":
                    AppendSimpleStat(character, "POISON", $"+{character.GetWeaponPoisonPercentPerHit():0.#}% max HP on critical hit from weapon mods.", result, AddWrapped, maxLines);
                    break;
                case "gear:weapon":
                    AppendGear(character, character.Weapon, "Weapon", result, AddWrapped, maxLines);
                    break;
                case "gear:head":
                    AppendGear(character, character.Head, "Head", result, AddWrapped, maxLines);
                    break;
                case "gear:body":
                    AppendGear(character, character.Body, "Body", result, AddWrapped, maxLines);
                    break;
                case "gear:legs":
                    AppendGear(character, character.Legs, "Legs", result, AddWrapped, maxLines);
                    break;
                case "gear:feet":
                    AppendGear(character, character.Feet, "Feet", result, AddWrapped, maxLines);
                    break;
                case string invKey when invKey.StartsWith("inv:", StringComparison.Ordinal):
                    AppendInventoryListItem(character, invKey, result, AddWrapped, maxLines);
                    break;
                case "hero:name":
                    AppendHeroName(character, AddWrapped);
                    break;
                case "hero:hp":
                    AppendHeroHp(character, AddWrapped);
                    break;
                case "hero:level":
                    AppendHeroLevel(character, AddWrapped);
                    break;
                case "hero:xp":
                    AppendHeroXp(character, AddWrapped);
                    break;
                case "hero:classpts":
                    AppendHeroClassPoints(character, AddWrapped);
                    break;
                case "thresh:crit":
                    AppendThresholdCrit(character, AddWrapped);
                    break;
                case "thresh:combo":
                    AppendThresholdCombo(character, AddWrapped);
                    break;
                case "thresh:hit":
                    AppendThresholdHit(character, AddWrapped);
                    break;
                case "thresh:critmiss":
                    AppendThresholdCritMiss(character, AddWrapped);
                    break;
                case "thresh:miss":
                    AppendThresholdMiss(character, AddWrapped);
                    break;
                case "status:overflow":
                    AddWrapped("More status effects");
                    AddWrapped("Additional effects are active; the list is truncated in the panel.");
                    break;
                case string st when st.StartsWith("status:", StringComparison.Ordinal):
                    AppendStatusLine(character, st, AddWrapped);
                    break;
                case "center:itemsHeader":
                    AddWrapped("Items you are carrying (not necessarily equipped).");
                    AddWrapped("Choose Equip / Unequip / Discard from ACTIONS, or use the numbered menu.");
                    break;
                case "center:actionsHeader":
                    AddWrapped("Inventory actions (keyboard shortcuts match the numbers).");
                    AddWrapped("Equip and Discard ask you to pick an item; Unequip asks for a slot.");
                    break;
                case "menu:equip":
                    AddWrapped("Equip an item from your bag into the correct slot (weapon, head, body, or feet).");
                    AddWrapped("You will choose which item; a comparison may appear if something is already equipped.");
                    break;
                case "menu:unequip":
                    AddWrapped("Remove an item from a slot back into your bag.");
                    break;
                case "menu:discard":
                    AddWrapped("Permanently remove an item from your bag.");
                    break;
                case "menu:tradeup":
                    AddWrapped("Combine five items of one rarity into one item of the next rarity.");
                    break;
                case "menu:exit":
                    AddWrapped("Return to the game menu (same as 0).");
                    break;
            }

            return result;
        }

        private static void AppendInventoryListItem(Character c, string invKey, List<string> result, Action<string> addWrapped, int maxLines)
        {
            if (invKey.Length <= 4 || !int.TryParse(invKey.AsSpan(4), out int idx))
                return;
            var inv = c.Inventory;
            if (inv == null || idx < 0 || idx >= inv.Count)
                return;
            AppendGear(c, inv[idx], "Inventory", result, addWrapped, maxLines);
        }

        private static void AppendHeroName(Character c, Action<string> addWrapped)
        {
            addWrapped("Hero name");
            addWrapped(string.IsNullOrEmpty(c.Name) ? "(unnamed)" : c.Name);
        }

        private static void AppendHeroHp(Character c, Action<string> addWrapped)
        {
            int cur = c.CurrentHealth;
            int max = c.GetEffectiveMaxHealth();
            addWrapped("Health");
            addWrapped($"{cur} / {max} HP (effective max includes bonuses from gear and effects).");
        }

        private static void AppendHeroLevel(Character c, Action<string> addWrapped)
        {
            addWrapped("Level and class");
            addWrapped($"Level {c.Level} — {c.GetCurrentClass()}.");
            addWrapped("Class points (from leveling) unlock hybrid skills; see class points line if shown.");
        }

        private static void AppendHeroXp(Character c, Action<string> addWrapped)
        {
            int need = c.Progression.GetXPRequiredForNextLevel();
            addWrapped("Experience");
            addWrapped($"{c.XP} / {need} XP toward the next level.");
        }

        private static void AppendHeroClassPoints(Character c, Action<string> addWrapped)
        {
            addWrapped("Class points");
            addWrapped("Points per class track how much you have invested in Barbarian, Warrior, Rogue, and Wizard paths.");
            addWrapped("Which combo actions unlock at each tier (and optional extra point gates) comes from GameData/ClassActions.json, usually filled from the CLASS ACTIONS sheet when you pull from Google Sheets.");
        }

        private static void AppendThresholdCrit(Character c, Action<string> addWrapped)
        {
            var snapshot = DiceRollThresholdResolver.Resolve(c);
            addWrapped("Critical hit threshold (crit-eval roll)");
            addWrapped($"Panel: {snapshot.EffectiveCrit}{ThresholdDisplayFormatting.FormatSignedDeltaSuffix(snapshot.EffectiveCrit - snapshot.DefaultCrit)} (stored {snapshot.Crit}, default {snapshot.DefaultCrit}).");
            addWrapped("Compared to your underlying d20 total for this attack (accuracy and other roll bonuses do not make crits easier).");
            ThresholdModificationTooltipBuilder.AppendLines(c, ThresholdModificationTooltipBuilder.Kind.Crit, addWrapped);
            AppendThresholdOutcomePercent(c, "Crit", addWrapped);
        }

        private static void AppendThresholdCombo(Character c, Action<string> addWrapped)
        {
            var snapshot = DiceRollThresholdResolver.Resolve(c);
            addWrapped("Combo threshold (d20 roll)");
            addWrapped($"Panel: {snapshot.EffectiveCombo}{ThresholdDisplayFormatting.FormatSignedDeltaSuffix(snapshot.EffectiveCombo - snapshot.DefaultCombo)} (stored {snapshot.Combo}, default {snapshot.DefaultCombo}).");
            addWrapped("Named combo-strip swings require meeting this on the modified die (panel ACC shifts hit and post-roll combo outcomes, not which strip action is chosen).");
            ThresholdModificationTooltipBuilder.AppendLines(c, ThresholdModificationTooltipBuilder.Kind.Combo, addWrapped);
            AppendThresholdOutcomePercent(c, "Combo", addWrapped);
        }

        private static void AppendThresholdHit(Character c, Action<string> addWrapped)
        {
            var snapshot = DiceRollThresholdResolver.Resolve(c);
            addWrapped("Hit vs miss (d20 roll)");
            addWrapped($"Panel: min roll to hit {snapshot.EffectiveHit}{ThresholdDisplayFormatting.FormatSignedDeltaSuffix(snapshot.EffectiveHit - snapshot.DefaultMinRollToHit)} (miss-band max {snapshot.Hit}, default {snapshot.DefaultHit}).");
            ThresholdModificationTooltipBuilder.AppendLines(c, ThresholdModificationTooltipBuilder.Kind.Hit, addWrapped);
            AppendThresholdOutcomePercent(c, "Hit", addWrapped);
        }

        private static void AppendThresholdCritMiss(Character c, Action<string> addWrapped)
        {
            var snapshot = DiceRollThresholdResolver.Resolve(c);
            addWrapped("Critical miss threshold");
            addWrapped($"Panel: {snapshot.EffectiveCritMiss}{ThresholdDisplayFormatting.FormatSignedDeltaSuffix(snapshot.EffectiveCritMiss - DiceRollThresholdSnapshot.DefaultCritMiss)} (stored {snapshot.CritMiss}, default {DiceRollThresholdSnapshot.DefaultCritMiss}).");
            addWrapped("Uses the same crit-eval roll as critical hit (accuracy affects hit/combo only, not this band).");
            ThresholdModificationTooltipBuilder.AppendLines(c, ThresholdModificationTooltipBuilder.Kind.CritMiss, addWrapped);
            AppendThresholdOutcomePercent(c, "Crit Miss", addWrapped);
        }

        private static void AppendThresholdMiss(Character c, Action<string> addWrapped)
        {
            addWrapped("Miss");
            addWrapped("Share of d20 outcomes that are not critical hit, combo, normal hit, or critical miss.");
            AppendThresholdOutcomePercent(c, "Miss", addWrapped);
        }

        private static void AppendThresholdOutcomePercent(Character c, string label, Action<string> addWrapped)
        {
            var snapshot = DiceRollThresholdResolver.Resolve(c);
            var chances = ThresholdDisplayFormatting.CalculateExclusiveD20OutcomeChances(
                snapshot.EffectiveCrit,
                snapshot.EffectiveCombo,
                snapshot.EffectiveHit,
                snapshot.EffectiveCritMiss);
            int percent = label switch
            {
                "Crit" => chances.CritPercent,
                "Combo" => chances.ComboPercent,
                "Hit" => chances.HitPercent,
                "Crit Miss" => chances.CritMissPercent,
                "Miss" => chances.MissPercent,
                _ => 0
            };
            addWrapped($"Outcome chance: {ThresholdDisplayFormatting.FormatD20ChancePercent(percent)}");
        }

        private static void AppendStatusLine(Character c, string key, Action<string> addWrapped)
        {
            if (key.Length <= 7 || !int.TryParse(key.AsSpan(7), out int lineIdx))
                return;
            var lines = StatusEffectDisplayLines.Build(c, c);
            if (lineIdx < 0 || lineIdx >= lines.Count)
                return;
            addWrapped("Status effect");
            addWrapped(lines[lineIdx]);
            addWrapped("Duration is in turns unless the effect says otherwise.");
        }

        private static void AppendDamage(Character c, List<string> result, Action<string> addWrapped, int maxLines)
        {
            AppendColoredStatTooltip(c, "stat:damage", result, addWrapped, maxLines);
        }

        private static void AppendSpeed(Character c, List<string> result, Action<string> addWrapped, int maxLines) =>
            AppendColoredStatTooltip(c, "stat:speed", result, addWrapped, maxLines);

        private static void AppendAmp(Character c, List<string> result, Action<string> addWrapped, int maxLines) =>
            AppendColoredStatTooltip(c, "stat:amp", result, addWrapped, maxLines);

        private static void AppendArmor(Character c, List<string> result, Action<string> addWrapped, int maxLines)
        {
            AppendColoredStatTooltip(c, "stat:armor", result, addWrapped, maxLines);
        }

        private static void AppendPrimaryStat(Character c, string code, string label, List<string> result, Action<string> addWrapped, int maxLines)
        {
            string key = code switch
            {
                "STR" => "stat:str",
                "AGI" => "stat:agi",
                "TEC" => "stat:tec",
                "INT" => "stat:int",
                _ => ""
            };
            if (!string.IsNullOrEmpty(key))
                AppendColoredStatTooltip(c, key, result, addWrapped, maxLines);
        }

        private static void AppendColoredStatTooltip(Character c, string statKey, List<string> result, Action<string> addWrapped, int maxLines)
        {
            var colored = StatTooltipFormatter.TryBuild(c, statKey, maxLines);
            if (colored == null)
                return;
            foreach (var line in colored)
            {
                if (result.Count >= maxLines)
                    return;
                if (line == null || line.Count == 0)
                {
                    result.Add("");
                    continue;
                }
                addWrapped(ColoredTextRenderer.RenderAsPlainText(line));
            }
        }

        private static void AppendMagFind(Character c, List<string> result, Action<string> addWrapped, int maxLines)
        {
            int v = c.GetMagicFind();
            addWrapped("Magic find");
            addWrapped($"Total: +{v}");
            addWrapped("Sum of MagicFind stat bonuses on equipment plus modification magicFind effects.");
            addWrapped("Loot: improves affix-line tier odds and optional extra affix chances (0–100); does not change base rarity table.");
        }

        private static void AppendSimpleStat(Character c, string title, string detail, List<string> result, Action<string> addWrapped, int maxLines)
        {
            addWrapped(title);
            addWrapped(detail);
        }

        private static void AppendGear(Character c, Item? item, string slot, List<string> result, Action<string> addWrapped, int maxLines)
        {
            if (item == null)
            {
                addWrapped($"{slot} slot");
                addWrapped("No item equipped.");
                return;
            }

            foreach (var coloredLine in ItemTooltipFormatter.BuildItemTooltipLines(c, item, slot, maxLines))
            {
                if (result.Count >= maxLines)
                    return;
                if (coloredLine == null || coloredLine.Count == 0)
                {
                    result.Add("");
                    continue;
                }
                addWrapped(ColoredTextRenderer.RenderAsPlainText(coloredLine));
            }
        }

    }
}
