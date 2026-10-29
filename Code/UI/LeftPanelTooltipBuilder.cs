using System;
using System.Collections.Generic;
using Avalonia.Media;
using RPGGame.Actions.RollModification;
using RPGGame.Items.Helpers;
using RPGGame.UI;
using RPGGame.UI.Avalonia.Layout;
using RPGGame.UI.ColorSystem;
using RPGGame.UI.ColorSystem.Applications;

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

            if (key.StartsWith("set:", StringComparison.Ordinal))
                return BuildColoredSet(character, key.Substring(4), maxLines);

            return key switch
            {
                "gear:weapon" => BuildColoredGear(character, character.Weapon, "Weapon", maxLines),
                "gear:head" => BuildColoredGear(character, character.Head, "Head", maxLines),
                "gear:body" => BuildColoredGear(character, character.Body, "Body", maxLines),
                "gear:legs" => BuildColoredGear(character, character.Legs, "Legs", maxLines),
                "gear:feet" => BuildColoredGear(character, character.Feet, "Feet", maxLines),
                "gear:charm" => BuildColoredGear(character, character.Charm, "Charm", maxLines),
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
            return ItemTooltipFormatter.BuildItemTooltipLines(
                c, item, slot, maxLines, UI.HoverTooltipDetailState.IsAltDetailActive);
        }

        private static List<List<ColoredText>> BuildColoredInventory(Character c, string invKey, int maxLines)
        {
            if (invKey.Length <= 4 || !int.TryParse(invKey.AsSpan(4), out int idx))
                return new List<List<ColoredText>>();
            var inv = c.Inventory;
            if (inv == null || idx < 0 || idx >= inv.Count)
                return new List<List<ColoredText>>();
            return ItemTooltipFormatter.BuildItemTooltipLines(
                c, inv[idx], "Inventory", maxLines, UI.HoverTooltipDetailState.IsAltDetailActive);
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

            void addWrappedAnimal(string label)
            {
                AddWrapped(label);
                foreach (string line in AnimalSetController.DescribeSetBonuses(character))
                    AddWrapped(line);
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
                    AppendActionSlotBreakdown(character, AddWrapped);
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
                    AddWrapped("Sum of HealthRegen stat lines on all equipped gear and affixes. Heals that much each turn in combat, up to max HP.");
                    break;
                case "stat:lifesteal":
                    AppendSimpleStat(character, "LIFESTEAL", $"{character.GetModificationLifesteal():P0} from modifications.", result, AddWrapped, maxLines);
                    AddWrapped("Sum of lifesteal mods on your gear (ModificationTables.json). After a hit: heal = round(damage dealt × (gear % + the action's own lifesteal %)), at least 1.");
                    AddWrapped($"Example: a 20-damage hit heals {(character.GetModificationLifesteal() > 0 ? Math.Max(1, (int)Math.Round(20 * character.GetModificationLifesteal())) : 0)}.");
                    break;
                case "stat:bleed":
                    AppendSimpleStat(character, "BLEED", $"+{character.GetWeaponBleedPerHit()} on critical hit from weapon mods.", result, AddWrapped, maxLines);
                    AppendWeaponDotSource(AddWrapped, "weaponBleed", "bleed stacks");
                    break;
                case "stat:burn":
                    AppendSimpleStat(character, "BURN", $"+{character.GetWeaponBurnPerHit()} on critical hit from weapon mods.", result, AddWrapped, maxLines);
                    AppendWeaponDotSource(AddWrapped, "weaponBurn", "burn stacks");
                    break;
                case "stat:acid":
                    AppendSimpleStat(character, "ACID", $"+{character.GetWeaponAcidPerHit()} on critical hit from weapon mods.", result, AddWrapped, maxLines);
                    AppendWeaponDotSource(AddWrapped, "weaponAcid", "acid stacks");
                    break;
                case "stat:poison":
                    AppendSimpleStat(character, "POISON", $"+{character.GetWeaponPoisonPercentPerHit():0.#}% max HP on critical hit from weapon mods.", result, AddWrapped, maxLines);
                    AppendWeaponDotSource(AddWrapped, "weaponPoison", "% of the target's max HP per poison tick");
                    break;
                case string attrKey when attrKey.StartsWith("attr:", StringComparison.Ordinal):
                    AppendRequirementAttribute(character, attrKey.Substring("attr:".Length), result, AddWrapped, maxLines);
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
                case "gear:charm":
                    AppendGear(character, character.Charm, "Charm", result, AddWrapped, maxLines);
                    break;
                case string setKey when setKey.StartsWith("set:", StringComparison.Ordinal):
                    AppendSet(character, setKey.Substring(4), result, AddWrapped, maxLines);
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
                case string animalKey when animalKey.StartsWith("animal:", StringComparison.Ordinal):
                    addWrappedAnimal(animalKey.Substring("animal:".Length));
                    break;
                case string est when est.StartsWith("estatus:", StringComparison.Ordinal):
                    AppendEnemyStatusLine(character, est, AddWrapped);
                    break;
                case string eaff when eaff.StartsWith("eaff:", StringComparison.Ordinal):
                    AppendEnemyAffinityLine(eaff, AddWrapped);
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
                case "menu:snapshot":
                    AddWrapped("Save this character's stats, gear, and combo strip as an Action Lab snapshot.");
                    AddWrapped("Load it later from the Action Lab tools panel or Settings → Testing.");
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
            addWrapped(string.IsNullOrEmpty(c.Name) ? "(unnamed)" : c.Name);
            addWrapped("Equipped gear");
            AppendCompactGearSlot(c.Weapon, "Weapon", addWrapped);
            AppendCompactGearSlot(c.Head, "Head", addWrapped);
            AppendCompactGearSlot(c.Body, "Body", addWrapped);
            AppendCompactGearSlot(c.Legs, "Legs", addWrapped);
            AppendCompactGearSlot(c.Feet, "Feet", addWrapped);
            AppendCompactGearSlot(c.Charm, "Charm", addWrapped);

            var materialSets = MaterialSetController.GetFormingSets(c);
            if (materialSets.Count > 0)
            {
                addWrapped("Sets");
                foreach (var (material, count) in materialSets)
                    addWrapped(MaterialSetController.FormatFormingSetHudLine(material, count));
            }

            var animalSets = AnimalSetController.GetFormingSets(c);
            if (animalSets.Count > 0)
            {
                addWrapped("Animals");
                foreach (var (label, _) in animalSets)
                    addWrapped(label);
            }
        }

        private static void AppendCompactGearSlot(Item? item, string slot, Action<string> addWrapped)
        {
            if (item == null)
            {
                addWrapped($"{slot}: (empty)");
                return;
            }

            string name = ColoredTextRenderer.RenderAsPlainText(ItemDisplayColoredText.FormatFullItemName(item));
            if (string.IsNullOrWhiteSpace(name))
                name = string.IsNullOrEmpty(item.Name) ? "(unnamed)" : item.Name;
            addWrapped($"{slot}: {name}");
        }

        private static void AppendHeroHp(Character c, Action<string> addWrapped)
        {
            int cur = c.CurrentHealth;
            int max = c.GetEffectiveMaxHealth();
            int baseMax = c.MaxHealth;
            int gear = max - baseMax;
            var tuning = GameConfiguration.Instance;
            int perLevel = tuning.Character.HealthPerLevel > 0 ? tuning.Character.HealthPerLevel : 3;
            double classHpMult = c.Weapon is WeaponItem w
                ? ClassBalanceHelper.GetMultipliersForWeapon(w.WeaponType).HealthMultiplier
                : 0;
            int nextLevelGain = classHpMult > 0 ? Math.Max(1, (int)(perLevel * classHpMult)) : perLevel;

            addWrapped("Health");
            addWrapped($"{cur} / {max} HP.");
            addWrapped($"Equation: character max {baseMax} + gear Health {gear:+0;-0;+0} = {max}.");
            addWrapped($"Character max starts at Character.PlayerBaseHealth ({tuning.Character.PlayerBaseHealth}) and grows each level-up by Character.HealthPerLevel ({perLevel})" +
                (classHpMult > 0 ? $" × your weapon class's ClassBalance HealthMultiplier ({classHpMult:0.##}) = +{nextLevelGain} next level." : $" = +{nextLevelGain} next level."));
            addWrapped("Level-ups heal to full. Health stat lines on gear and affixes add to the max.");
            if (c.GetEquipmentHealthRegenBonus() > 0)
                addWrapped($"Regenerates +{c.GetEquipmentHealthRegenBonus()} HP per turn from gear (hover HP REGEN).");
        }

        private static void AppendHeroLevel(Character c, Action<string> addWrapped)
        {
            var attrs = GameConfiguration.Instance.Attributes;
            int primary = attrs.PlayerPrimaryStatPerLevel > 0 ? attrs.PlayerPrimaryStatPerLevel : 3;
            int secondary = attrs.PlayerSecondaryStatPerLevel > 0 ? attrs.PlayerSecondaryStatPerLevel : 1;
            string weaponText = c.Weapon is WeaponItem w ? $"your {w.WeaponType}" : "your weapon";
            addWrapped("Level and class");
            addWrapped($"Level {c.Level} — {c.GetCurrentClass()}.");
            addWrapped($"Class title comes from your class points (highest path first; hover the class points row).");
            addWrapped($"Each level-up gives: +HP (hover the health bar), +{primary} to the stat {weaponText} favors and +{secondary} to the others (+2 each unarmed), and 1 class point + 1 skill point on {weaponText}'s path.");
            addWrapped("Tuning: Attributes.PlayerPrimaryStatPerLevel / PlayerSecondaryStatPerLevel, Character.HealthPerLevel.");
        }

        private static void AppendHeroXp(Character c, Action<string> addWrapped)
        {
            int need = c.Progression.GetXPRequiredForNextLevel();
            var prog = GameConfiguration.Instance.Progression;
            prog.EnsureValidEnemyXpAndGoldDefaults();
            int enemyXp = CharacterProgression.GetBaseXpForContentLevel(c.Level);
            int clearXp = CharacterProgression.GetStandardDungeonCompletionXpForLevel(c.Level);
            addWrapped("Experience");
            addWrapped($"{c.XP} / {need} XP toward the next level ({Math.Max(0, need - c.XP)} to go).");
            addWrapped($"Enemy XP = Progression.EnemyXPBase {prog.EnemyXPBase} + enemy level × EnemyXPPerLevel {prog.EnemyXPPerLevel} (a level {c.Level} enemy gives {enemyXp}). Clearing a level {c.Level} dungeon gives {clearXp}.");
            addWrapped($"XP to level = estimated dungeon-run XP at your level × expected dungeons per level, anchored so level 1 → 2 costs Progression.BaseXPToLevel2 ({prog.BaseXPToLevel2}), × XPScalingFactor ({(prog.XPScalingFactor > 0 ? prog.XPScalingFactor : 1.0):0.##}).");
            addWrapped("Leftover XP carries into the next level.");
        }

        private static void AppendHeroClassPoints(Character c, Action<string> addWrapped)
        {
            var progression = c.Progression;
            var pres = GameConfiguration.Instance.ClassPresentation.EnsureNormalized();
            addWrapped("Class points");
            addWrapped("You earn 1 point on a path each level-up, on the path of the weapon you have equipped (Mace → Barbarian, Sword → Warrior, Dagger → Rogue, Wand → Wizard).");
            foreach (var (label, path) in new[] { ("Barbarian", WeaponType.Mace), ("Warrior", WeaponType.Sword), ("Rogue", WeaponType.Dagger), ("Wizard", WeaponType.Wand) })
            {
                int pts = progression.GetClassPoints(path);
                if (pts <= 0)
                    continue;
                int next = progression.GetNextClassThreshold(path);
                string nextText = next > pts ? $", next tier at {next}" : ", top tier";
                addWrapped($"  {label}: {pts} pts{nextText}; skill points free {progression.GetAvailableSkillPoints(path)} = {pts} − {progression.GetSpentSkillPoints(path)} spent.");
            }
            addWrapped($"Tier gates: {string.Join(", ", pres.TierThresholds)} points (ClassPresentation.TierThresholds). Each tier reached on any path adds 1 action slot (now +{progression.GetClassUpgradeActionSlotBonus()}).");
            addWrapped("Which combo actions unlock at each tier (and optional extra point gates) comes from GameData/ClassActions.json, usually filled from the CLASS ACTIONS sheet when you pull from Google Sheets.");
        }

        private static void AppendThresholdCrit(Character c, Action<string> addWrapped)
        {
            var snapshot = DiceRollThresholdResolver.Resolve(c);
            var parts = DiceRollThresholdResolver.ResolveShiftParts(c);
            addWrapped("Critical hit threshold (crit-eval roll)");
            addWrapped($"Panel: {snapshot.EffectiveCrit}{ThresholdDisplayFormatting.FormatSignedDeltaSuffix(snapshot.EffectiveCrit - snapshot.DefaultCrit)} (stored {snapshot.Crit}, default {snapshot.DefaultCrit}).");
            addWrapped("Compared to your underlying d20 total for this attack (accuracy and other roll bonuses do not make crits easier).");
            addWrapped($"Equation: stored {snapshot.Crit} − shifts {parts.CritRowShift} = {snapshot.Crit - parts.CritRowShift}{FloorSuffix(snapshot.EffectiveCrit, snapshot.Crit - parts.CritRowShift)}.");
            ThresholdModificationTooltipBuilder.AppendLines(c, ThresholdModificationTooltipBuilder.Kind.Crit, addWrapped);
            int perfectAim = SkillEffectRouter.PerfectAimCritSteps(c);
            if (perfectAim > 0)
                addWrapped($"  Perfect Aim (+{perfectAim}): applied when the swing resolves; not in the panel number.");
            addWrapped($"Crit damage: ×{CritDamageMultiplier():0.##} (CombatBalance.CriticalHitDamageMultiplier).");
            AppendLadderExplainer(snapshot, addWrapped);
            AppendThresholdOutcomePercent(c, "Crit", addWrapped);
            addWrapped("Default from the balance patch: Combat.CriticalHitThreshold.");
        }

        private static void AppendThresholdCombo(Character c, Action<string> addWrapped)
        {
            var snapshot = DiceRollThresholdResolver.Resolve(c);
            var parts = DiceRollThresholdResolver.ResolveShiftParts(c);
            addWrapped("Action threshold (d20 roll)");
            addWrapped($"Panel: {snapshot.EffectiveCombo}{ThresholdDisplayFormatting.FormatSignedDeltaSuffix(snapshot.EffectiveCombo - snapshot.DefaultCombo)} (stored {snapshot.Combo}, default {snapshot.DefaultCombo}).");
            addWrapped("Named combo-strip swings require meeting this on the modified die (panel ACC shifts hit and post-roll combo outcomes, not which strip action is chosen).");
            addWrapped($"Equation: stored {snapshot.Combo} − shifts {parts.ComboRowShift} = {snapshot.Combo - parts.ComboRowShift}{FloorSuffix(snapshot.EffectiveCombo, snapshot.Combo - parts.ComboRowShift)}.");
            ThresholdModificationTooltipBuilder.AppendLines(c, ThresholdModificationTooltipBuilder.Kind.Combo, addWrapped);
            int cadence = ClassCadence.ComboSteps(c);
            if (cadence > 0)
                addWrapped($"  Class cadence (+{cadence}): applied when the swing resolves; not in the panel number.");
            double comboMult = GameConfiguration.Instance.CombatBalance.RollDamageMultipliers.ComboRollDamageMultiplier;
            addWrapped($"Action roll damage: ×{(comboMult > 0 ? comboMult : 1.0):0.##} (CombatBalance.RollDamageMultipliers.ComboRollDamageMultiplier).");
            AppendLadderExplainer(snapshot, addWrapped);
            AppendThresholdOutcomePercent(c, "Action", addWrapped);
            addWrapped("Default from the balance patch: RollSystem.ComboThreshold.Min.");
        }

        private static void AppendThresholdHit(Character c, Action<string> addWrapped)
        {
            var snapshot = DiceRollThresholdResolver.Resolve(c);
            var parts = DiceRollThresholdResolver.ResolveShiftParts(c);
            addWrapped("Hit vs miss (d20 roll)");
            addWrapped($"Panel: min roll to hit {snapshot.EffectiveHit}{ThresholdDisplayFormatting.FormatSignedDeltaSuffix(snapshot.EffectiveHit - snapshot.DefaultMinRollToHit)} (miss-band max {snapshot.Hit}, default {snapshot.DefaultHit}).");
            addWrapped($"Equation: (miss-band max {snapshot.Hit} + 1) − shifts {parts.HitRowShift} = {snapshot.Hit + 1 - parts.HitRowShift}{FloorSuffix(snapshot.EffectiveHit, snapshot.Hit + 1 - parts.HitRowShift)}.");
            ThresholdModificationTooltipBuilder.AppendLines(c, ThresholdModificationTooltipBuilder.Kind.Hit, addWrapped);
            double basicMult = GameConfiguration.Instance.CombatBalance.RollDamageMultipliers.BasicRollDamageMultiplier;
            addWrapped($"Normal hit damage: ×{(basicMult > 0 ? basicMult : 1.0):0.##} (CombatBalance.RollDamageMultipliers.BasicRollDamageMultiplier).");
            AppendLadderExplainer(snapshot, addWrapped);
            AppendThresholdOutcomePercent(c, "Hit", addWrapped);
            addWrapped("Default from the balance patch: RollSystem.MissThreshold.Max (the highest face that misses).");
        }

        private static void AppendThresholdCritMiss(Character c, Action<string> addWrapped)
        {
            var snapshot = DiceRollThresholdResolver.Resolve(c);
            var parts = DiceRollThresholdResolver.ResolveShiftParts(c);
            addWrapped("Critical miss threshold");
            addWrapped($"Panel: {snapshot.EffectiveCritMiss}{ThresholdDisplayFormatting.FormatSignedDeltaSuffix(snapshot.EffectiveCritMiss - DiceRollThresholdSnapshot.DefaultCritMiss)} (stored {snapshot.CritMiss}, default {DiceRollThresholdSnapshot.DefaultCritMiss}).");
            addWrapped("Uses the same crit-eval roll as critical hit (accuracy affects hit/combo only, not this band).");
            addWrapped($"Equation: stored {snapshot.CritMiss} − shifts {parts.CritMissRowShift} = {snapshot.CritMiss - parts.CritMissRowShift}{FloorSuffix(snapshot.EffectiveCritMiss, snapshot.CritMiss - parts.CritMissRowShift)}. Higher = more faces crit-miss.");
            ThresholdModificationTooltipBuilder.AppendLines(c, ThresholdModificationTooltipBuilder.Kind.CritMiss, addWrapped);
            AppendLadderExplainer(snapshot, addWrapped);
            AppendThresholdOutcomePercent(c, "Crit Miss", addWrapped);
            addWrapped("Default: 1 (only a natural 1 crit-misses).");
        }

        private static void AppendThresholdMiss(Character c, Action<string> addWrapped)
        {
            var snapshot = DiceRollThresholdResolver.Resolve(c);
            addWrapped("Miss");
            addWrapped("Share of d20 outcomes that are not critical hit, combo, normal hit, or critical miss.");
            addWrapped($"Miss faces sit between crit miss (≤ {snapshot.EffectiveCritMiss}) and the minimum hit ({snapshot.EffectiveHit}).");
            AppendLadderExplainer(snapshot, addWrapped);
            AppendThresholdOutcomePercent(c, "Miss", addWrapped);
        }

        /// <summary>The order a swing's d20 is checked in, with the panel's effective numbers.</summary>
        private static void AppendLadderExplainer(DiceRollThresholdSnapshot snapshot, Action<string> addWrapped)
        {
            addWrapped("How a swing resolves");
            addWrapped($"  Roll d20, add accuracy. Crit if the die ≥ {snapshot.EffectiveCrit}; else action if the total ≥ {snapshot.EffectiveCombo}; else hit if ≥ {snapshot.EffectiveHit}; crit miss if the die ≤ {snapshot.EffectiveCritMiss}; anything else misses.");
            addWrapped("  Shifts: positive values lower crit/action/hit (easier). Sources: queued action accuracy, TECH milestones (TEC 10, 15, 20…), gear HIT/ACTION/CRIT lines, dungeon search buffs.");
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
                "Action" => chances.ComboOrCritPercent,
                "Hit" => chances.HitPercent,
                "Crit Miss" => chances.CritMissPercent,
                "Miss" => chances.MissPercent,
                _ => 0
            };
            addWrapped($"Outcome chance: {ThresholdDisplayFormatting.FormatD20ChancePercent(percent)}");

            // Same exclusive priority as CalculateExclusiveD20OutcomeChances: crit, action, hit, crit miss, miss.
            var faces = new List<int>();
            for (int roll = 1; roll <= 20; roll++)
            {
                string outcome = roll >= snapshot.EffectiveCrit ? "Crit"
                    : roll >= snapshot.EffectiveCombo ? "Action"
                    : roll >= snapshot.EffectiveHit ? "Hit"
                    : roll <= snapshot.EffectiveCritMiss ? "Crit Miss"
                    : "Miss";
                if (outcome == label || (label == "Action" && outcome == "Crit"))
                    faces.Add(roll);
            }
            string faceText = faces.Count == 0 ? "no faces" : $"faces {FormatFaceRanges(faces)}";
            string note = label == "Action" ? " (crits count as actions)" : "";
            addWrapped($"  = {faces.Count} of 20 d20 faces × 5%: {faceText}{note}. Before accuracy bonuses on the roll.");
        }

        private static string FormatFaceRanges(List<int> faces)
        {
            var ranges = new List<string>();
            int start = faces[0];
            int prev = start;
            for (int i = 1; i <= faces.Count; i++)
            {
                if (i < faces.Count && faces[i] == prev + 1)
                {
                    prev = faces[i];
                    continue;
                }
                ranges.Add(start == prev ? start.ToString() : $"{start}–{prev}");
                if (i < faces.Count)
                {
                    start = faces[i];
                    prev = start;
                }
            }
            return string.Join(", ", ranges);
        }

        private static string FloorSuffix(int effective, int unclamped) =>
            effective != unclamped ? $", shown as {effective} (floored at 1)" : "";

        private static double CritDamageMultiplier()
        {
            double m = GameConfiguration.Instance.CombatBalance.CriticalHitDamageMultiplier;
            return m > 0 ? m : 2.0;
        }

        private static void AppendStatusLine(Character c, string key, Action<string> addWrapped)
        {
            if (key.Length <= 7 || !int.TryParse(key.AsSpan(7), out int lineIdx))
                return;
            var lines = StatusEffectDisplayLines.Build(c, c);
            if (lineIdx < 0 || lineIdx >= lines.Count)
                return;
            addWrapped(lines[lineIdx]);
            foreach (string detail in HoverEffectCopy.ExplainStatusLine(c, lines[lineIdx]))
                addWrapped(detail);
        }

        /// <summary>Right-panel enemy status row (<c>estatus:N</c>) for <see cref="StatusEffectHoverDetails.CurrentEnemy"/>.</summary>
        private static void AppendEnemyStatusLine(Character hero, string key, Action<string> addWrapped)
        {
            var enemy = StatusEffectHoverDetails.CurrentEnemy;
            if (enemy == null || !int.TryParse(key.AsSpan("estatus:".Length), out int lineIdx))
                return;
            var lines = StatusEffectDisplayLines.Build(enemy, enemy);
            if (lineIdx < 0 || lineIdx >= lines.Count)
                return;
            addWrapped($"{enemy.Name}: {lines[lineIdx]}");
            foreach (string detail in HoverEffectCopy.ExplainActorStatusLine(enemy, lines[lineIdx], hero))
                addWrapped(detail);
        }

        /// <summary>Right-panel enemy Immune / Weak / Resists row (<c>eaff:N</c>).</summary>
        private static void AppendEnemyAffinityLine(string key, Action<string> addWrapped)
        {
            var enemy = StatusEffectHoverDetails.CurrentEnemy;
            if (enemy == null || !int.TryParse(key.AsSpan("eaff:".Length), out int lineIdx))
                return;
            var rows = StatusEffectDisplayLines.GetEnemyAffinityLines(enemy);
            if (lineIdx < 0 || lineIdx >= rows.Count)
                return;
            addWrapped($"{enemy.Name}: {rows[lineIdx]}");
            foreach (string detail in StatusEffectHoverDetails.DescribeAffinityRow(enemy, rows[lineIdx]))
                addWrapped(detail);
        }

        private static void AppendDamage(Character c, List<string> result, Action<string> addWrapped, int maxLines)
        {
            AppendColoredStatTooltip(c, "stat:damage", result, maxLines);
        }

        private static void AppendSpeed(Character c, List<string> result, Action<string> addWrapped, int maxLines) =>
            AppendColoredStatTooltip(c, "stat:speed", result, maxLines);

        private static void AppendAmp(Character c, List<string> result, Action<string> addWrapped, int maxLines) =>
            AppendColoredStatTooltip(c, "stat:amp", result, maxLines);

        private static void AppendArmor(Character c, List<string> result, Action<string> addWrapped, int maxLines)
        {
            AppendColoredStatTooltip(c, "stat:armor", result, maxLines);
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
                AppendColoredStatTooltip(c, key, result, maxLines);
        }

        /// <summary>Gear requirement attribute (<c>attr:finesse</c>, <c>attr:strength</c>, <c>attr:primary</c>…): what it is and the hero's value.</summary>
        private static void AppendRequirementAttribute(Character c, string keyLower, List<string> result, Action<string> addWrapped, int maxLines)
        {
            switch (keyLower)
            {
                case "strength": AppendPrimaryStat(c, "STR", "Strength", result, addWrapped, maxLines); return;
                case "agility": AppendPrimaryStat(c, "AGI", "Agility", result, addWrapped, maxLines); return;
                case "technique": AppendPrimaryStat(c, "TEC", "Technique", result, addWrapped, maxLines); return;
                case "intelligence": AppendPrimaryStat(c, "INT", "Intelligence", result, addWrapped, maxLines); return;
            }

            string label = Item.FormatRequirementAttributeDisplayName(keyLower);
            int have = Item.GetEffectiveValueForRequirementKey(keyLower, c);

            if (HybridTrees.ByAttributeName(keyLower) is HybridInfo h)
            {
                int a = AttributeValue(c, h.StatA);
                int b = AttributeValue(c, h.StatB);
                int fromFight = have - a - b;
                addWrapped(label.ToUpperInvariant());
                addWrapped($"Hybrid attribute of the {h.Key} tree ({h.ClassA} + {h.ClassB}): {AttributeName(h.StatA)} + {AttributeName(h.StatB)}.");
                string extra = fromFight != 0 ? $" {fromFight:+0;-0} this fight" : "";
                addWrapped($"Yours: {have} ({h.StatA} {a} + {h.StatB} {b}{extra}).");
                addWrapped($"Raise either {AttributeName(h.StatA)} or {AttributeName(h.StatB)} to meet {label} requirements.");
                return;
            }

            if (Item.IsDynamicAttributeRequirementKey(keyLower))
            {
                string rank = keyLower switch
                {
                    "primary" => "highest",
                    "secondary" => "second-highest",
                    "neglected" => "third-highest",
                    _ => "lowest"
                };
                addWrapped(label);
                addWrapped($"Your {rank} attribute (STR, AGI, TEC, INT), whichever that is right now.");
                addWrapped($"Yours: {have}.");
            }
        }

        private static int AttributeValue(Character c, string code) => code switch
        {
            "STR" => c.GetEffectiveStrength(),
            "AGI" => c.GetEffectiveAgility(),
            "TEC" => c.GetEffectiveTechnique(),
            "INT" => c.GetEffectiveIntelligence(),
            _ => 0
        };

        private static string AttributeName(string code) => code switch
        {
            "STR" => "Strength",
            "AGI" => "Agility",
            "TEC" => "Technique",
            "INT" => "Intelligence",
            _ => code
        };

        private static void AppendColoredStatTooltip(Character c, string statKey, List<string> result, int maxLines)
        {
            var colored = StatTooltipFormatter.TryBuild(c, statKey, maxLines);
            if (colored == null)
                return;
            AppendPlainLinesFromColored(colored, result, maxLines);
        }

        /// <summary>
        /// Converts pre-formatted colored tooltip rows to plain lines without inserting extra
        /// paragraph blanks (those rows already encode spacing; <c>addWrapped</c> would double them and burn maxLines).
        /// </summary>
        private static void AppendPlainLinesFromColored(List<List<ColoredText>> colored, List<string> result, int maxLines)
        {
            foreach (var line in colored)
            {
                if (result.Count >= maxLines)
                    return;
                if (line == null || line.Count == 0)
                {
                    result.Add("");
                    continue;
                }
                result.Add(ColoredTextRenderer.RenderAsPlainText(line));
            }
        }

        private static void AppendMagFind(Character c, List<string> result, Action<string> addWrapped, int maxLines)
        {
            int v = c.GetMagicFind();
            addWrapped("Magic find");
            addWrapped($"Total: +{v}");
            int fromMods = c.Equipment.GetModificationMagicFind();
            addWrapped($"Equation: gear MagicFind lines {v - fromMods:+0;-0;+0} + magicFind mods {fromMods:+0;-0;+0} = +{v}.");
            addWrapped("Sum of MagicFind stat bonuses on equipment plus modification magicFind effects.");
            addWrapped("Loot: improves affix-line tier odds and optional extra affix chances (0–100); does not change base rarity table.");
        }

        /// <summary>Spells out <see cref="ComboSequenceMaxHelper.GetEffectiveMax"/>.</summary>
        private static void AppendActionSlotBreakdown(Character c, Action<string> addWrapped)
        {
            var loot = GameConfiguration.Instance.LootSystem;
            int baseMax = Math.Max(1, loot?.ComboSequenceBaseMax ?? 2);
            int absMax = Math.Max(baseMax, loot?.ComboSequenceAbsoluteMax ?? 8);
            int gear = Math.Max(0, c.Equipment.GetEquipmentStatBonus("ExtraActionSlots", c));
            int cls = Math.Max(0, c.Progression.GetClassUpgradeActionSlotBonus());
            int levelCap = loot?.GetComboSlotCapForLevel(c.Level) ?? 0;
            int earned = baseMax + cls;
            addWrapped($"Equation: base {baseMax} + class tiers {cls} = {earned}" +
                (levelCap > 0 && earned > levelCap ? $", capped at {levelCap} for level {c.Level}" : "") +
                $", + gear {gear}" +
                (c.ActionLabActionSlotBonus > 0 ? $", + lab {c.ActionLabActionSlotBonus}" : "") +
                $"; never above {absMax}.");
            addWrapped("Gear: ExtraActionSlots on items and affixes (not limited by the level cap). Class tiers: +1 per tier reached on each class path.");
            addWrapped("Tuning: LootSystem.ComboSequenceBaseMax / ComboSequenceAbsoluteMax / comboSlotCapByLevel.");
        }

        private static void AppendWeaponDotSource(Action<string> addWrapped, string modEffect, string unit)
        {
            addWrapped($"Sum of {modEffect} mods on your equipped weapon only, in {unit}. Each mod fires on its own trigger (critical hit unless the mod says otherwise) and adds its rolled value to the target.");
            addWrapped("Mod values come from ModificationTables.json, rolled when the item drops.");
        }

        private static void AppendSimpleStat(Character c, string title, string detail, List<string> result, Action<string> addWrapped, int maxLines)
        {
            addWrapped(title);
            addWrapped(detail);
        }

        private static List<List<ColoredText>> BuildColoredSet(Character c, string material, int maxLines)
        {
            var lines = new List<List<ColoredText>>();
            foreach (var summary in MaterialSetController.FormatSetStatusLinesForMaterial(c, material))
            {
                if (lines.Count >= maxLines)
                    break;
                var b = new ColoredTextBuilder();
                b.Add(summary, Colors.White);
                lines.Add(b.Build());
            }
            return lines;
        }

        private static void AppendSet(Character c, string material, List<string> result, Action<string> addWrapped, int maxLines)
        {
            foreach (var line in MaterialSetController.FormatSetStatusLinesForMaterial(c, material))
            {
                if (result.Count >= maxLines)
                    return;
                addWrapped(line);
            }
        }

        private static void AppendGear(Character c, Item? item, string slot, List<string> result, Action<string> addWrapped, int maxLines)
        {
            if (item == null)
            {
                addWrapped($"{slot} slot");
                addWrapped("No item equipped.");
                return;
            }

            AppendPlainLinesFromColored(
                ItemTooltipFormatter.BuildItemTooltipLines(
                    c, item, slot, maxLines, UI.HoverTooltipDetailState.IsAltDetailActive),
                result,
                maxLines);
        }

    }
}
