using System;
using System.Collections.Generic;
using System.Linq;
using RPGGame;
using RPGGame.Data;

namespace RPGGame.UI.Avalonia.Layout
{
    /// <summary>
    /// Builds human-readable lines for the status-effect HUD (hero left panel, enemy right panel).
    /// </summary>
    public static class StatusEffectDisplayLines
    {
        /// <summary>Line prefix (as written by <see cref="Build"/>) → <c>GameData/StatusArt</c> icon. Keep in step with the strings below.</summary>
        private static readonly (string Prefix, string Icon)[] IconPrefixes =
        {
            ("Weakened (", "weaken"), ("Stunned (", "stun"), ("Poison ", "poison"), ("Bleed ", "bleed"),
            ("Burn ", "burn"), ("Acid ", "acid"), ("Vuln x", "vulnerability"), ("Harden x", "harden"),
            ("Fortify x", "fortify"), ("Focus x", "focus"), ("Expose x", "expose"), ("HP Regen x", "hpregen"),
            ("Armor brk x", "armorbreak"), ("Pierce (", "pierce"), ("Silence (", "silence"), ("Absorb (", "absorb"),
            ("Temp HP (", "temporaryhp"), ("Confused (", "confusion"), ("Marked (", "mark"), ("Slow (", "slow"),
            ("Stat drain x", "statdrain"), ("Accuracy ", "accuracy"), ("Advantage ", "advantage"),
            ("Disadvantage ", "disadvantage"), ("Crit miss (", "critmiss"), ("Reflect x", "reflect"),
            ("Extra atk x", "extraattack"), ("Reroll x", "reroll"),
        };

        /// <summary>
        /// Lines whose words stay beside their icon (a bare name, a bonus list or a keyword bank would read as nothing
        /// with the name cut off). Material keywords (MaterialBuilds.json's synthesis banks) match as "KEYWORD x".
        /// </summary>
        private static readonly (string Prefix, string Icon)[] IconKeepTextPrefixes =
        {
            (LeatherSetBonus.LuckyStatusName, "lucky"), ("Shield", "shield"), ("Skip turn", "skipturn"),
            ("Guarantee hit", "surehit"), ("Combo mode", "combomode"), ("Next action", "nextaction"), ("Action ", "nextaction"),
            ("TURN", "turnbonus"), ("Fight:", "fightbonus"), ("Dungeon:", "dungeonbonus"),
            ("CRISIS x", "crisis"), ("TRIUMPH x", "triumph"), ("RAGE x", "rage"), ("BEAT x", "beat"), ("DRAG x", "drag"),
            ("STREAK x", "streak"), ("GRAZE x", "graze"), ("SWING x", "swing"), ("TROPHY x", "trophy"),
            ("CHAIN x", "chain"), ("LENS x", "lens"), ("CONTACT x", "contact"),
        };

        /// <summary>The <see cref="StatusArt"/> icon for a <see cref="Build"/> line; null for lines with no icon.</summary>
        public static string? IconKeyFor(string line) => SplitIcon(line).Icon;

        /// <summary>
        /// A <see cref="Build"/> line's icon and the text left once the icon stands in for the effect's name
        /// ("Bleed 4" → bleed, "4"; "Vuln x2 (3t)" → vulnerability, "x2 (3t)"). Lines in <see cref="IconKeepTextPrefixes"/>
        /// keep their words ("Shield" → shield, "Shield"); lines with no icon come back whole.
        /// </summary>
        public static (string? Icon, string Text) SplitIcon(string line)
        {
            foreach (var (prefix, icon) in IconPrefixes)
            {
                if (line.StartsWith(prefix, StringComparison.Ordinal))
                    return (icon, line.Substring(prefix.LastIndexOf(' ') + 1));
            }
            foreach (var (prefix, icon) in IconKeepTextPrefixes)
            {
                if (line.StartsWith(prefix, StringComparison.Ordinal))
                    return (icon, line);
            }
            return (null, line);
        }

        /// <summary>
        /// An enemy affinity line ("Weak: Burn ×2, Acid ×2") as its label and one item per status: the status key
        /// (the <see cref="StatusArt"/> icon), its shown name and what follows it (the multiplier; empty for Immune).
        /// </summary>
        public static (string Label, List<(string Status, string Name, string Suffix)> Items) SplitAffinityLine(string line)
        {
            var items = new List<(string, string, string)>();
            int colon = line.IndexOf(':');
            if (colon < 0)
                return (line, items);
            foreach (var part in line.Substring(colon + 1).Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
            {
                int space = part.IndexOf(' ');
                string name = space < 0 ? part : part.Substring(0, space);
                items.Add((RPGGame.Combat.StatusAffinity.NormalizeStatus(name), name, space < 0 ? "" : part.Substring(space + 1)));
            }
            return (line.Substring(0, colon + 1), items);
        }

        /// <summary>
        /// Builds display strings for an actor. Pass <paramref name="asCharacter"/> when <paramref name="actor"/> is a <see cref="Character"/> to include character-only effects.
        /// </summary>
        public static List<string> Build(Actor actor, Character? asCharacter)
        {
            var lines = new List<string>();
            Character? charHud = asCharacter ?? actor as Character;
            if (charHud != null)
            {
                lines.AddRange(BuildMaterialKeywordStatusLines(charHud));
                // Leather starter set: standing Lucky (WHILE_EQUIPPED attack-roll advantage).
                if (LeatherSetBonus.HasLuckyStatus(charHud))
                    lines.Add(LeatherSetBonus.FormatLuckyStatusLine(charHud));
            }
            // Room tags that change this fighter (Elegant / Dilapidated die shift, Unstable bands) while the fight's room has them.
            foreach (var roomStatus in RPGGame.Combat.RoomStatusEffects.For(actor, RPGGame.Combat.RoomStatusEffects.ActiveRoom))
                lines.Add(RPGGame.Combat.RoomStatusEffects.FormatLine(roomStatus));
            if (actor.IsWeakened && actor.WeakenTurns > 0)
                lines.Add($"Weakened ({actor.WeakenTurns} turn{(actor.WeakenTurns != 1 ? "s" : "")})");
            if (actor.IsStunned && actor.StunTurnsRemaining > 0)
                lines.Add($"Stunned ({actor.StunTurnsRemaining} turn{(actor.StunTurnsRemaining != 1 ? "s" : "")})");
            if (actor.RollPenalty != 0 && actor.RollPenaltyTurns > 0)
                lines.Add($"Accuracy -{actor.RollPenalty} {CadenceBonusDisplayFormatter.FormatTurnDurationSuffix(actor.RollPenaltyTurns)}");
            if (actor.PoisonPercentOfMaxHealth > 0)
                lines.Add($"Poison {actor.PoisonPercentOfMaxHealth:0.##}% max HP");
            if (actor.BleedIntensity > 0 || actor.PendingBleedFromHits > 0)
                lines.Add($"Bleed {actor.BleedIntensity + actor.PendingBleedFromHits}");
            if (actor.BurnIntensity > 0 || actor.PendingBurnFromHits > 0)
                lines.Add($"Burn {actor.BurnIntensity + actor.PendingBurnFromHits}");
            if (actor.AcidIntensity > 0 || actor.PendingAcidFromHits > 0 || actor.AcidArmorReduction > 0)
            {
                int acidDisplay = actor.AcidIntensity + actor.PendingAcidFromHits;
                if (acidDisplay > 0 && actor.AcidArmorReduction > 0)
                    lines.Add($"Acid {acidDisplay} (−{actor.AcidArmorReduction} armor)");
                else if (acidDisplay > 0)
                    lines.Add($"Acid {acidDisplay}");
                else
                    lines.Add($"Acid (−{actor.AcidArmorReduction} armor)");
            }
            if (actor.HasCriticalMissPenalty && actor.CriticalMissPenaltyTurns > 0)
                lines.Add($"Crit miss ({actor.CriticalMissPenaltyTurns} turn{(actor.CriticalMissPenaltyTurns != 1 ? "s" : "")})");
            if (actor.VulnerabilityStacks > 0 && actor.VulnerabilityTurns > 0)
                lines.Add($"Vuln x{actor.VulnerabilityStacks} ({actor.VulnerabilityTurns}t)");
            if (actor.HardenStacks > 0 && actor.HardenTurns > 0)
                lines.Add($"Harden x{actor.HardenStacks} ({actor.HardenTurns}t)");
            if (actor.FortifyStacks > 0 && actor.FortifyTurns > 0)
                lines.Add($"Fortify x{actor.FortifyStacks} ({actor.FortifyTurns}t)");
            if (actor.FocusStacks > 0 && actor.FocusTurns > 0)
                lines.Add($"Focus x{actor.FocusStacks} ({actor.FocusTurns}t)");
            if (actor.ExposeStacks > 0 && actor.ExposeTurns > 0)
                lines.Add($"Expose x{actor.ExposeStacks} ({actor.ExposeTurns}t)");
            if (actor.HPRegenStacks > 0 && actor.HPRegenTurns > 0)
                lines.Add($"HP Regen x{actor.HPRegenStacks} ({actor.HPRegenTurns}t)");
            if (actor.ArmorBreakStacks > 0 && actor.ArmorBreakTurns > 0)
                lines.Add($"Armor brk x{actor.ArmorBreakStacks} ({actor.ArmorBreakTurns}t)");
            if (actor.StatDrainStacks > 0 && actor.StatDrainTurns > 0)
                lines.Add($"Stat drain x{actor.StatDrainStacks} ({actor.StatDrainTurns}t)");
            if (actor.HasPierce && actor.PierceTurns > 0)
                lines.Add($"Pierce ({actor.PierceTurns} turn{(actor.PierceTurns != 1 ? "s" : "")})");
            if (actor.ReflectStacks > 0 && actor.ReflectTurns > 0)
                lines.Add($"Reflect x{actor.ReflectStacks} ({actor.ReflectTurns}t)");
            if (actor.IsSilenced && actor.SilenceTurns > 0)
                lines.Add($"Silence ({actor.SilenceTurns} turn{(actor.SilenceTurns != 1 ? "s" : "")})");
            if (actor.HasAbsorb && actor.AbsorbTurns > 0)
                lines.Add($"Absorb ({actor.AbsorbTurns} turn{(actor.AbsorbTurns != 1 ? "s" : "")})");
            if (actor.TemporaryHP > 0 && actor.TemporaryHPTurns > 0)
                lines.Add($"Temp HP ({actor.TemporaryHPTurns} turn{(actor.TemporaryHPTurns != 1 ? "s" : "")})");
            if (actor.IsConfused && actor.ConfusionTurns > 0)
                lines.Add($"Confused ({actor.ConfusionTurns} turn{(actor.ConfusionTurns != 1 ? "s" : "")})");
            if (actor.IsMarked && actor.MarkTurns > 0)
                lines.Add($"Marked ({actor.MarkTurns} turn{(actor.MarkTurns != 1 ? "s" : "")})");
            if (charHud != null)
            {
                lines.AddRange(BuildTurnBonusStatusLines(charHud));
                lines.AddRange(BuildPendingActionBonusStatusLines(charHud));
                lines.AddRange(BuildScopedCadenceStatusLines(charHud));

                if (charHud.Effects.SlowTurns > 0)
                    lines.Add($"Slow ({charHud.Effects.SlowTurns} turn{(charHud.Effects.SlowTurns != 1 ? "s" : "")})");
                if (charHud.Effects.HasShield)
                    lines.Add("Shield");
                if (charHud.Effects.SkipNextTurn)
                    lines.Add("Skip turn");
                if (charHud.Effects.GuaranteeNextSuccess)
                    lines.Add("Guarantee hit");
                if (charHud.Effects.ExtraAttacks > 0)
                    lines.Add($"Extra atk x{charHud.Effects.ExtraAttacks}");
                if (charHud.Effects.ComboModeActive)
                    lines.Add("Combo mode");
                if (charHud.Effects.RerollCharges > 0)
                    lines.Add($"Reroll x{charHud.Effects.RerollCharges}");
            }
            return lines;
        }

        /// <summary>Dungeon-run material-set keyword bank (CRISIS, DRAG, …). Count updates as synthesis mints; cleared at dungeon end.</summary>
        internal static List<string> BuildMaterialKeywordStatusLines(Character c)
        {
            var lines = new List<string>();
            foreach (var (keyword, count) in c.Effects.GetMaterialKeywordCounts())
                lines.Add($"{keyword} x{count}");
            return lines;
        }

        /// <summary>Per-group TURN bonus lines so stacking (e.g. TURN x3 from multiple sources) is visible.</summary>
        internal static List<string> BuildTurnBonusStatusLines(Character c)
        {
            var lines = new List<string>();
            var turnGroups = c.Effects.SnapshotTurnBonuses();
            bool hasTurnGroups = turnGroups.Any(g => g != null && g.Count > 0 && g.Bonuses?.Count > 0);

            foreach (var group in turnGroups)
            {
                if (group == null || group.Count <= 0 || group.Bonuses == null || group.Bonuses.Count == 0)
                    continue;
                string items = CadenceBonusDisplayFormatter.FormatBonusItemsShort(group.Bonuses);
                if (string.IsNullOrEmpty(items))
                    continue;
                string suffix = CadenceBonusDisplayFormatter.FormatTurnDurationSuffix(group.Count);
                if (group.Bonuses.Count == 1 && IsSingleAccuracyOnly(group.Bonuses))
                    lines.Add($"Accuracy +{group.Bonuses[0].Value:0} {suffix}");
                else if (group.Bonuses.Count == 1 && IsSingleAdvantageOnly(group.Bonuses))
                    lines.Add($"Advantage {suffix}");
                else if (group.Bonuses.Count == 1 && IsSingleDisadvantageOnly(group.Bonuses))
                    lines.Add($"Disadvantage {suffix}");
                else
                    lines.Add($"{CadenceKeywords.GetDisplayLabel(CadenceKeywords.Turn, group.Count)}: {items} {suffix}");
            }

            if (!hasTurnGroups)
            {
                int accTemp = c.Effects.TempRollBonusTurns > 0 ? c.Effects.GetTempRollBonus() : 0;
                if (accTemp != 0)
                {
                    string suffix = CadenceBonusDisplayFormatter.FormatTurnDurationSuffix(c.Effects.TempRollBonusTurns);
                    lines.Add(accTemp > 0 ? $"Accuracy +{accTemp} {suffix}" : $"Accuracy {accTemp} {suffix}");
                }
            }

            return lines;
        }

        internal static List<string> BuildPendingActionBonusStatusLines(Character c)
        {
            var lines = new List<string>();

            var nextRollBonuses = c.Effects.PeekPendingActionBonusesNextHeroRoll();
            if (nextRollBonuses.Count > 0)
            {
                string items = CadenceBonusDisplayFormatter.FormatBonusItemsShort(nextRollBonuses);
                if (!string.IsNullOrEmpty(items))
                {
                    int layers = c.Effects.GetPendingActionCadenceLayerCount();
                    string suffix = layers > 1 ? $"(x{layers} stacked, next action)" : "(next action)";
                    if (IsSingleAdvantageOnly(nextRollBonuses))
                        lines.Add($"Advantage {suffix}");
                    else if (IsSingleDisadvantageOnly(nextRollBonuses))
                        lines.Add($"Disadvantage {suffix}");
                    else
                        lines.Add($"Next action: {items}");
                }
            }

            foreach (var slot in c.Effects.GetPendingActionBonusSlots().OrderBy(s => s))
            {
                var bonuses = c.Effects.GetPendingActionBonusesForSlot(slot);
                if (bonuses.Count == 0) continue;
                string items = CadenceBonusDisplayFormatter.FormatBonusItemsShort(bonuses);
                if (string.IsNullOrEmpty(items)) continue;
                int displaySlot = slot + 1;
                if (IsSingleAdvantageOnly(bonuses))
                    lines.Add($"Advantage (action {displaySlot})");
                else if (IsSingleDisadvantageOnly(bonuses))
                    lines.Add($"Disadvantage (action {displaySlot})");
                else
                    lines.Add($"Action {displaySlot}: {items}");
            }

            return lines;
        }

        internal static List<string> BuildScopedCadenceStatusLines(Character c)
        {
            var lines = new List<string>();
            AppendScopedLine(lines, "Fight", c.FightCadenceBuffs);
            AppendScopedLine(lines, "Dungeon", c.DungeonCadenceBuffs);
            return lines;
        }

        private static void AppendScopedLine(List<string> lines, string label, CadenceScopedBonusState scope)
        {
            if (!scope.HasAny) return;
            string items = CadenceBonusDisplayFormatter.FormatBonusItemsShort(scope.CopyBonuses());
            if (!string.IsNullOrEmpty(items))
                lines.Add($"{label}: {items}");
        }

        private static bool IsSingleAccuracyOnly(List<ActionAttackBonusItem> bonuses)
        {
            if (bonuses.Count != 1) return false;
            return string.Equals(ActionAttackBonusItem.NormalizeBonusType(bonuses[0].Type), "ACCURACY", StringComparison.OrdinalIgnoreCase)
                && bonuses[0].Value != 0;
        }

        private static bool IsSingleAdvantageOnly(List<ActionAttackBonusItem> bonuses) =>
            bonuses.Count == 1
            && string.Equals(ActionAttackBonusItem.NormalizeBonusType(bonuses[0].Type), MultiDiceRollMapper.AdvantageBonusType, StringComparison.OrdinalIgnoreCase);

        private static bool IsSingleDisadvantageOnly(List<ActionAttackBonusItem> bonuses) =>
            bonuses.Count == 1
            && string.Equals(ActionAttackBonusItem.NormalizeBonusType(bonuses[0].Type), MultiDiceRollMapper.DisadvantageBonusType, StringComparison.OrdinalIgnoreCase);

        /// <summary>
        /// Enemies with <see cref="Enemy.IsLiving"/> false (spreadsheet <c>isLiving</c> false: undead, elementals, etc.)
        /// do not take poison DoT or bleed damage — surface that in the status-effects HUD.
        /// </summary>
        public static string? GetNonLivingEnemyImmunityLine(Enemy enemy)
        {
            if (enemy == null)
                return null;
            return GetEnemyAffinityLines(enemy).FirstOrDefault(l => l.StartsWith("Immune", StringComparison.Ordinal));
        }

        /// <summary>Immune / Weak / Resists lines from <see cref="RPGGame.Combat.StatusAffinity"/> (enemy tags).</summary>
        public static List<string> GetEnemyAffinityLines(Enemy enemy) =>
            enemy == null ? new List<string>() : RPGGame.Combat.StatusAffinity.GetHudLines(enemy);
    }
}
