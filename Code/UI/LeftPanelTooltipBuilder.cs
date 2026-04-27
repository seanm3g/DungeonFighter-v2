using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using RPGGame.Actions.RollModification;
using RPGGame.Combat.Calculators;
using RPGGame.Items.Helpers;
using RPGGame.UI;
using RPGGame.UI.Avalonia.Layout;

namespace RPGGame
{
    /// <summary>
    /// Builds wrapped tooltip lines for left-panel STATS, GEAR, hero summary, thresholds, bag items (center list), etc.
    /// </summary>
    public static class LeftPanelTooltipBuilder
    {
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
                if (result.Count >= maxLines || string.IsNullOrWhiteSpace(paragraph))
                    return;
                foreach (var line in CombatActionStripBuilder.WrapTextToLines(paragraph.Trim(), maxWidth))
                {
                    if (result.Count >= maxLines) break;
                    result.Add(line);
                }
            }

            switch (key)
            {
                case "stat:damage":
                    AppendDamage(character, result, AddWrapped, maxLines);
                    break;
                case "stat:speed":
                    AppendSpeed(character, AddWrapped);
                    break;
                case "stat:amp":
                    AppendAmp(character, AddWrapped);
                    break;
                case "stat:armor":
                    AppendArmor(character, result, AddWrapped, maxLines);
                    break;
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
            addWrapped("They affect which advanced actions and passives you can unlock.");
        }

        private static void AppendThresholdCrit(Character c, Action<string> addWrapped)
        {
            var tm = RollModificationManager.GetThresholdManager();
            var cfg = GameConfiguration.Instance;
            int cur = tm.GetCriticalHitThreshold(c);
            int def = cfg.Combat.CriticalHitThreshold > 0 ? cfg.Combat.CriticalHitThreshold : 20;
            addWrapped("Critical hit threshold (crit-eval roll)");
            addWrapped($"Current: {cur} (default {def}).");
            addWrapped("Compared to your underlying d20 total for this attack (accuracy and other roll bonuses do not make crits easier).");
        }

        private static void AppendThresholdCombo(Character c, Action<string> addWrapped)
        {
            var tm = RollModificationManager.GetThresholdManager();
            var cfg = GameConfiguration.Instance;
            int cur = tm.GetComboThreshold(c);
            int def = cfg.RollSystem.ComboThreshold.Min > 0 ? cfg.RollSystem.ComboThreshold.Min : 14;
            addWrapped("Combo threshold (d20 roll)");
            addWrapped($"Current: {cur} (default {def}).");
            addWrapped("Named combo-strip swings require meeting this on the modified die (panel ACC shifts hit and post-roll combo outcomes, not which strip action is chosen).");
        }

        private static void AppendThresholdHit(Character c, Action<string> addWrapped)
        {
            var tm = RollModificationManager.GetThresholdManager();
            var cfg = GameConfiguration.Instance;
            int hit = tm.GetHitThreshold(c);
            int def = cfg.RollSystem.MissThreshold.Max > 0 ? cfg.RollSystem.MissThreshold.Max : 5;
            addWrapped("Hit vs miss (d20 roll)");
            addWrapped($"Miss threshold uses the configured band; panel shows {hit + 1} as the hit line (current tuning {hit}).");
            addWrapped($"Reference default for miss band: {def}.");
        }

        private static void AppendThresholdCritMiss(Character c, Action<string> addWrapped)
        {
            var tm = RollModificationManager.GetThresholdManager();
            int cur = tm.GetCriticalMissThreshold(c);
            addWrapped("Critical miss threshold");
            addWrapped($"Current: {cur}. Uses the same crit-eval roll as critical hit (accuracy affects hit/combo only, not this band).");
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
            int weaponDamage = (c.Weapon is WeaponItem w) ? w.GetTotalDamage() : 0;
            int equipmentDamageBonus = c.GetEquipmentDamageBonus();
            int modificationDamageBonus = c.GetModificationDamageBonus();
            int strEff = c.GetEffectiveStrength();
            int total = strEff + weaponDamage + equipmentDamageBonus + modificationDamageBonus;

            addWrapped("Damage (attack total)");
            addWrapped($"Total: {total}");
            addWrapped($"= effective STR ({strEff}) + weapon ({weaponDamage}) + equipment damage bonus ({equipmentDamageBonus}) + modification damage ({modificationDamageBonus}).");
            addWrapped("Note: combat rolls and action multipliers change outgoing hits; this is the base total shown in the panel.");
        }

        private static void AppendSpeed(Character c, Action<string> addWrapped)
        {
            double final = c.GetTotalAttackSpeed();
            addWrapped("Attack time (seconds per attack)");
            addWrapped($"Final: {final:F2}s (matches SpeedCalculator / panel).");

            var tuning = GameConfiguration.Instance;
            double baseAttackTime = tuning.Combat.BaseAttackTime;
            int agility = c.GetEffectiveAgility();
            int agilityMin = tuning.Combat.AgilityMin;
            int agilityMax = tuning.Combat.AgilityMax;
            agility = Math.Max(agilityMin, Math.Min(agilityMax, agility));
            double minMul = tuning.Combat.AgilityMinSpeedMultiplier;
            double maxMul = tuning.Combat.AgilityMaxSpeedMultiplier;
            double agilityRange = agilityMax - agilityMin;
            double normalizedProgress = agilityRange > 0 ? (agility - agilityMin) / agilityRange : 0.0;
            double curvedProgress = Math.Sqrt(normalizedProgress);
            double speedMultiplier = minMul + (maxMul - minMul) * curvedProgress;
            double agilityAdjustedTime = baseAttackTime * speedMultiplier;

            addWrapped($"1) Base time × AGI curve: {baseAttackTime:F3}s × {speedMultiplier:F3} (eff. AGI {c.GetEffectiveAgility()}, clamped {agility}) → {agilityAdjustedTime:F3}s.");
            addWrapped("   AGI uses sqrt of normalized progress between tuning min/max agility; multipliers from tuning.");

            double weaponTimeMul = 1.0;
            if (c.Weapon is WeaponItem w)
                weaponTimeMul = SpeedCalculator.GetWeaponAttackTimeMultiplier(w);
            double weaponAdjustedTime = agilityAdjustedTime * weaponTimeMul;
            addWrapped($"2) × weapon time multiplier ({weaponTimeMul:F2}; 1=baseline, >1 slower): → {weaponAdjustedTime:F3}s.");

            double equipmentSpeedBonus = c.GetEquipmentAttackSpeedBonus();
            double afterEquip = Math.Max(0.001, weaponAdjustedTime - equipmentSpeedBonus);
            addWrapped($"3) − equipment attack-speed bonus ({equipmentSpeedBonus:F3}s): → {afterEquip:F3}s.");

            if (c.SlowTurns > 0)
                addWrapped($"4) Slow debuff × {c.SlowMultiplier:F3} ({c.SlowTurns} turn(s) left).");

            double modSpeedMul = Math.Max(0.0001, c.GetModificationSpeedMultiplier());
            addWrapped($"5) ÷ modification speed multiplier ({modSpeedMul:F4}).");

            double minCap = Math.Max(0.01, tuning.Combat.MinimumAttackTime);
            addWrapped($"6) Clamp to minimum ({minCap:F2}s).");

            addWrapped($"Sanity check: reported final {final:F2}s.");
        }

        private static void AppendAmp(Character c, Action<string> addWrapped)
        {
            double baseAmp = c.GetComboAmplifier();
            addWrapped("AMP (combo growth)");
            addWrapped($"Base multiplier per combo step: {baseAmp:F2}x (from TECH; matches panel).");
            addWrapped("Damage on a given hit uses Pow(this base, strip slot index): first slot 0 → 1.0×, second 1 → baseline, etc. (order matches your sequence, not opener/finisher labels alone).");
            if (c.GetComboActions().Count == 0)
                addWrapped("No combo actions on the strip yet.");
        }

        private static void AppendArmor(Character c, List<string> result, Action<string> addWrapped, int maxLines)
        {
            int total = c.GetTotalArmor();
            addWrapped("Armor");
            addWrapped($"Total: {total}");
            int h = c.Head is HeadItem hh ? hh.GetTotalArmor() : 0;
            int b = c.Body is ChestItem ch ? ch.GetTotalArmor() : 0;
            int f = c.Feet is FeetItem ft ? ft.GetTotalArmor() : 0;
            addWrapped($"Head piece: {h}, Body: {b}, Feet: {f} (each includes that item's armor stats/mods).");
            addWrapped("Plus global Armor-type stat bonuses from all equipped items (EquipmentBonusCalculator).");
        }

        private static void AppendPrimaryStat(Character c, string code, string label, List<string> result, Action<string> addWrapped, int maxLines)
        {
            var stats = c.Stats;
            int baseVal = code switch
            {
                "STR" => stats.Strength,
                "AGI" => stats.Agility,
                "TEC" => stats.Technique,
                "INT" => stats.Intelligence,
                _ => 0
            };
            int temp = code switch
            {
                "STR" => stats.TempStrengthBonus,
                "AGI" => stats.TempAgilityBonus,
                "TEC" => stats.TempTechniqueBonus,
                "INT" => stats.TempIntelligenceBonus,
                _ => 0
            };
            int eq = c.Equipment.GetEquipmentStatBonus(code);
            int god = code == "STR" ? c.GetModificationGodlikeBonus() : 0;
            int eff = code switch
            {
                "STR" => c.GetEffectiveStrength(),
                "AGI" => c.GetEffectiveAgility(),
                "TEC" => c.GetEffectiveTechnique(),
                "INT" => c.GetEffectiveIntelligence(),
                _ => 0
            };

            addWrapped($"{label} ({code})");
            addWrapped($"Effective: {eff}");
            addWrapped($"= base ({baseVal}) + temporary bonus ({temp}) + equipment ({eq})" +
                       (code == "STR" ? $" + godlike STR mod ({god})." : "."));
        }

        private static void AppendMagFind(Character c, List<string> result, Action<string> addWrapped, int maxLines)
        {
            int v = c.GetMagicFind();
            addWrapped("Magic find");
            addWrapped($"Total: +{v}");
            addWrapped("Sum of MagicFind stat bonuses on equipment plus modification magicFind effects.");
            addWrapped("Loot: tilts the first weighted rarity roll toward higher tiers (capped at 100 for that math).");
        }

        private static void AppendSimpleStat(Character c, string title, string detail, List<string> result, Action<string> addWrapped, int maxLines)
        {
            addWrapped(title);
            addWrapped(detail);
        }

        private static void AppendGear(Character c, Item? item, string slot, List<string> result, Action<string> addWrapped, int maxLines)
        {
            addWrapped($"{slot} slot");
            if (item == null)
            {
                addWrapped("No item equipped.");
                return;
            }

            addWrapped(string.IsNullOrEmpty(item.Name) ? "(unnamed)" : item.Name);
            addWrapped($"Rarity: {item.Rarity}, tier {item.Tier}, level {item.Level}.");

            var resolvedGearActions = c.Equipment.GetGearActions(item);
            if (resolvedGearActions != null && resolvedGearActions.Count > 0)
                addWrapped("Actions: " + string.Join(", ", resolvedGearActions));

            if (item is WeaponItem wi)
            {
                addWrapped($"Weapon damage: {wi.GetTotalDamage()} (base {wi.BaseDamage} + bonus {wi.BonusDamage}).");
                addWrapped($"Weapon BaseAttackSpeed (JSON): {wi.BaseAttackSpeed.ToString("F2", CultureInfo.InvariantCulture)} (used in attack-time formula).");
                addWrapped($"Weapon type: {wi.WeaponType}.");
            }
            else if (item is HeadItem hh)
                addWrapped($"Armor (piece): {hh.GetTotalArmor()}.");
            else if (item is ChestItem ch)
                addWrapped($"Armor (piece): {ch.GetTotalArmor()}.");
            else if (item is FeetItem ft)
                addWrapped($"Armor (piece): {ft.GetTotalArmor()}.");

            if (item.StatBonuses != null && item.StatBonuses.Count > 0)
            {
                var parts = item.StatBonuses
                    .Select(sb => $"{sb.StatType}:{sb.Value:0.##}")
                    .ToArray();
                addWrapped("Stat bonuses: " + string.Join(", ", parts));
            }

            if (!string.IsNullOrEmpty(item.GearAction))
            {
                addWrapped($"Gear action: {item.GearAction}");
                var action = ResolveGearAction(c, item.GearAction);
                if (action != null)
                {
                    addWrapped(ActionDisplayFormatter.GetActionStats(action).Trim());
                    if (!string.IsNullOrWhiteSpace(action.Description))
                        addWrapped(action.Description.Trim());
                    int acc = ActionUtilities.CalculateRollBonus(c, action, consumeTempBonus: false);
                    addWrapped($"Accuracy (roll bonus): {acc:+0;-0;0}");
                }
                else
                    addWrapped("(Action not found in current action pool.)");
            }

            if (item.ActionBonuses != null && item.ActionBonuses.Count > 0)
            {
                foreach (var ab in item.ActionBonuses)
                {
                    if (result.Count >= maxLines) return;
                    string line = string.IsNullOrEmpty(ab.Name) ? "Action bonus" : ab.Name;
                    if (!string.IsNullOrEmpty(ab.Description))
                        line += ": " + ab.Description;
                    addWrapped(line);
                }
            }

            if (item.Modifications != null && item.Modifications.Count > 0)
            {
                foreach (var m in item.Modifications)
                {
                    if (result.Count >= maxLines) return;
                    string mline = string.IsNullOrEmpty(m.Name) ? "Modification" : m.Name;
                    if (!string.IsNullOrEmpty(m.Effect))
                        mline += $" [{m.Effect}]";
                    if (!string.IsNullOrEmpty(m.Description))
                        mline += " — " + m.Description;
                    mline += $" (value {m.RolledValue:0.##})";
                    addWrapped(mline);
                }
            }

            if (item.ArmorStatuses != null && item.ArmorStatuses.Count > 0)
            {
                foreach (var st in item.ArmorStatuses)
                {
                    if (result.Count >= maxLines) return;
                    string s = string.IsNullOrEmpty(st.Name) ? "Armor status" : st.Name;
                    if (!string.IsNullOrEmpty(st.Description))
                        s += ": " + st.Description;
                    addWrapped(s);
                }
            }
        }

        private static Action? ResolveGearAction(Character c, string gearActionName)
        {
            var combo = c.GetComboActions();
            if (combo != null)
            {
                foreach (var a in combo)
                {
                    if (a != null && string.Equals(a.Name, gearActionName, StringComparison.OrdinalIgnoreCase))
                        return a;
                }
            }

            var pool = c.GetActionPool();
            if (pool == null) return null;
            foreach (var a in pool)
            {
                if (a != null && string.Equals(a.Name, gearActionName, StringComparison.OrdinalIgnoreCase))
                    return a;
            }
            return null;
        }
    }
}
