using System;
using System.Collections.Generic;
using System.Globalization;
using Avalonia.Media;
using RPGGame.Combat.Calculators;
using RPGGame.Items;
using RPGGame.UI.ColorSystem;

namespace RPGGame
{
    /// <summary>
    /// Colored hover tooltips for left-panel stat rows (attributes, damage, armor, attack time, AMP).
    /// </summary>
    public static class StatTooltipFormatter
    {
        public static List<List<ColoredText>>? TryBuild(Character? character, string key, int maxLines = 24)
        {
            if (character == null || maxLines < 1)
                return null;

            return key switch
            {
                "stat:damage" => BuildDamage(character, maxLines),
                "stat:armor" => BuildArmor(character, maxLines),
                "stat:speed" => BuildSpeed(character, maxLines),
                "stat:amp" => BuildAmp(character, maxLines),
                "stat:str" => BuildPrimaryStat(character, "STR", "Strength", maxLines),
                "stat:agi" => BuildPrimaryStat(character, "AGI", "Agility", maxLines),
                "stat:tec" => BuildPrimaryStat(character, "TEC", "Technique", maxLines),
                "stat:int" => BuildPrimaryStat(character, "INT", "Intelligence", maxLines),
                _ => null
            };
        }

        private static List<List<ColoredText>> BuildPrimaryStat(Character c, string code, string label, int maxLines)
        {
            var lines = new List<List<ColoredText>>();
            var b = BuildAttributeBreakdown(c, code);

            AddTitle(lines, $"{label} ({code})");
            AddBlank(lines);
            AddHighlight(lines, "Effective", b.Effective.ToString(CultureInfo.InvariantCulture));
            AddBlank(lines);
            AddSection(lines, "Character");
            AddStatRow(lines, "Base", b.BaseValue);
            if (b.TempBonus != 0)
                AddSignedStatRow(lines, "Temp bonus", b.TempBonus);
            if (code == "STR" && b.GodlikeBonus != 0)
                AddSignedStatRow(lines, "Godlike mod", b.GodlikeBonus);
            if (b.SkillBonus != 0)
                AddSignedStatRow(lines, "Skill tree", b.SkillBonus);
            AddStatRow(lines, "After character mods", b.AttributeModifiedValue);

            if (b.GearTotalBonus != 0 || b.GearFlatBonus != 0 || b.GearSuffixBonus != 0)
            {
                AddBlank(lines);
                AddSection(lines, "Gear");
                AddSignedStatRow(lines, "Total from gear", b.GearTotalBonus);
                if (b.GearFlatBonus != 0)
                    AddSignedStatRow(lines, "Flat / catalog / material", b.GearFlatBonus);
                if (b.GearSuffixBonus != 0)
                    AddSignedStatRow(lines, "Suffix", b.GearSuffixBonus);
            }

            AddBlank(lines);
            AddEquationLine(lines, b, includeGodlike: code == "STR");

            if (code == "TEC")
            {
                var steps = TechniqueMilestoneThresholdBonuses.GetSteps(b.Effective);
                if (steps.HitSteps + steps.ComboSteps + steps.CritSteps > 0)
                    AddNoteLine(lines,
                        $"Milestones: +{steps.HitSteps} HIT, +{steps.ComboSteps} ACTION, +{steps.CritSteps} CRIT threshold steps (easier bands).");
                else
                    AddNoteLine(lines, "Milestones at TECH 10+ improve HIT/ACTION/CRIT thresholds each attack.");
                AddNoteLine(lines, "TECH only moves dice-roll thresholds; it does not change AMP.");
            }
            else if (code == "INT")
            {
                double amp = ComboAmplifierCurve.Compute(b.Effective, GameConfiguration.Instance.ComboSystem);
                AddNoteLine(lines, $"Feeds combo AMP: {amp:F2}× per combo step (slow INT power ramp + late finish).");
            }
            else if (code == "STR")
            {
                AddNoteLine(lines, GameConfiguration.Instance.Combat.StrengthDamagePerPoint > 0
                    ? $"Multiplies hero damage: ×{c.GetStrengthDamageMultiplier():0.00} now (hover Damage for the formula)."
                    : "Adds flat damage with your primary stat (hover Damage for the formula).");
            }
            else if (code == "AGI")
            {
                AddNoteLine(lines, $"Shortens attack time: ×{SpeedCalculator.GetBreakdown(c).AgilityFactor:0.000} on swing time now (hover Speed for the formula).");
            }

            var attrs = GameConfiguration.Instance.Attributes;
            int perLevelPrimary = attrs.PlayerPrimaryStatPerLevel > 0 ? attrs.PlayerPrimaryStatPerLevel : 3;
            int perLevelSecondary = attrs.PlayerSecondaryStatPerLevel > 0 ? attrs.PlayerSecondaryStatPerLevel : 1;
            string favoredWeapon = code switch
            {
                "STR" => "Mace",
                "AGI" => "Sword",
                "TEC" => "Dagger",
                _ => "Wand"
            };
            AddNoteLine(lines,
                $"Base comes from your starting stats plus each level-up: +{perLevelPrimary} {code} with a {favoredWeapon} equipped, +{perLevelSecondary} with any other weapon, +2 unarmed.");
            AddNoteLine(lines, "Tuning: Attributes.PlayerPrimaryStatPerLevel / PlayerSecondaryStatPerLevel. Gear lines come from item stats and affixes; skill tree from learned nodes.");

            return Trim(lines, maxLines);
        }

        private static List<List<ColoredText>> BuildDamage(Character c, int maxLines)
        {
            var lines = new List<List<ColoredText>>();
            int weaponDamage = (c.Weapon is WeaponItem w) ? w.GetTotalDamage() : 0;
            int equipmentDamageBonus = c.GetEquipmentDamageBonus();
            int modificationDamageBonus = c.GetModificationDamageBonus();
            int strEff = c.GetEffectiveStrength();
            int primaryEff = c.GetEffectivePrimaryAttributeValue();
            int attrBonus = c.GetAttributeDamageBonus();
            double strMult = c.GetStrengthDamageMultiplier();
            int total = c.Combat.GetTotalDamage();
            var (primaryCode, primaryLabel) = GetPrimaryAttributeDisplay(c);
            bool primaryIsStrength = primaryCode == DynamicAttributeCategoryResolver.CodeStrength;

            AddTitle(lines, "Damage");
            AddBlank(lines);
            AddHighlight(lines, "Base attack total", total.ToString(CultureInfo.InvariantCulture));
            AddBlank(lines);
            double attrScale = GameConfiguration.Instance.Combat.AttributeDamageScale;
            if (attrScale <= 0) attrScale = 0.05;
            if (strMult != 1.0)
            {
                var combatCfg = GameConfiguration.Instance.Combat;
                int strCap = combatCfg.StrengthDamageMaxStat > 0
                    ? combatCfg.StrengthDamageMaxStat + SkillEffectRouter.StrengthCapBonus(c)
                    : int.MaxValue;
                AddSection(lines, "Strength multiplies damage");
                AddStatRow(lines, "Strength", strEff);
                AddDecimalStatRow(lines, "Damage multiplier", strMult, "0.00");
                string capText = strCap == int.MaxValue ? "no cap" : $"cap {strCap}";
                string overflowText = strEff > strCap && combatCfg.StrengthOverflowEfficiency > 0
                    ? $" + ({strEff} − {strCap}) × {combatCfg.StrengthOverflowEfficiency:0.##} overflow"
                    : "";
                AddNoteLine(lines,
                    $"Multiplier = 1 + min(STR {strEff}, {capText}){overflowText} × {combatCfg.StrengthDamagePerPoint:0.####} per point = {strMult:0.00}");
                AddBlank(lines);
            }
            else
            {
                AddSection(lines, "Attributes (STR + primary) × scale");
                AddStatRow(lines, "Strength (always)", strEff);
                AddStatRow(lines, $"Primary ({primaryCode})", primaryEff);
                AddDecimalStatRow(lines, "Damage scale", attrScale, "0.##");
                AddStatRow(lines, "Attribute subtotal", attrBonus);
                AddBlank(lines);
            }
            AddSection(lines, "Gear & mods");
            AddStatRow(lines, "Weapon", weaponDamage);
            AddSignedStatRow(lines, "Equipment", equipmentDamageBonus);
            AddSignedStatRow(lines, "Modifications", modificationDamageBonus);
            AddBlank(lines);
            if (strMult != 1.0)
                AddNoteLine(lines, $"({weaponDamage} + {equipmentDamageBonus} + {modificationDamageBonus}) × {strMult:0.00} = {total}");
            else
                AddDamageEquationLine(lines, strEff, primaryCode, primaryEff, attrScale, attrBonus, weaponDamage, equipmentDamageBonus, modificationDamageBonus, total);
            if (strMult != 1.0)
                AddNoteLine(lines, "Each point of Strength adds the same share of damage; your primary stat no longer adds damage.");
            else if (primaryIsStrength)
                AddNoteLine(lines, "STR is your primary — Strength and Primary both use the same value.");
            else
                AddNoteLine(lines, $"Primary is {primaryLabel} ({primaryCode}), your highest effective stat.");

            AppendPerSwingDamageSection(lines, c, total);
            AddBlank(lines);
            AddSection(lines, "Where the numbers come from");
            AddNoteLine(lines, "Weapon: catalog damage (Weapons.json) plus its affixes. Equipment: Damage stat lines on all gear. Modifications: damage mods (ModificationTables.json).");
            AddNoteLine(lines, strMult != 1.0
                ? "Tuning (balance patch): Combat.StrengthDamagePerPoint / StrengthDamageMaxStat / StrengthOverflowEfficiency."
                : "Tuning (balance patch): Combat.AttributeDamageScale.");

            return Trim(lines, maxLines);
        }

        private static (string Code, string Label) GetPrimaryAttributeDisplay(Character c)
        {
            string code = DynamicAttributeCategoryResolver.GetRankedAttributeCodes(c)[0];
            string label = code switch
            {
                DynamicAttributeCategoryResolver.CodeStrength => "Strength",
                DynamicAttributeCategoryResolver.CodeAgility => "Agility",
                DynamicAttributeCategoryResolver.CodeTechnique => "Technique",
                DynamicAttributeCategoryResolver.CodeIntelligence => "Intelligence",
                _ => code
            };
            return (code, label);
        }

        private static void AddDamageEquationLine(
            List<List<ColoredText>> lines,
            int strEff,
            string primaryCode,
            int primaryEff,
            double attrScale,
            int attrBonus,
            int weaponDamage,
            int equipmentDamageBonus,
            int modificationDamageBonus,
            int total)
        {
            var seg = new ColoredTextBuilder();
            seg.Add("Equation: floor((", ColorPalette.Info.GetColor());
            seg.Add(strEff.ToString(CultureInfo.InvariantCulture), Colors.White);
            seg.Add(" + ", Colors.Gray);
            seg.Add($"{primaryCode} {primaryEff}", Colors.White);
            seg.Add(") × ", Colors.Gray);
            seg.Add(attrScale.ToString("0.##", CultureInfo.InvariantCulture), Colors.White);
            seg.Add(") = ", Colors.Gray);
            seg.Add(attrBonus.ToString(CultureInfo.InvariantCulture), Colors.White);
            seg.Add(" + ", Colors.Gray);
            seg.Add(weaponDamage.ToString(CultureInfo.InvariantCulture), Colors.White);
            seg.Add(" + ", Colors.Gray);
            seg.Add(FormatSigned(equipmentDamageBonus), ValueColor(equipmentDamageBonus));
            seg.Add(" + ", Colors.Gray);
            seg.Add(FormatSigned(modificationDamageBonus), ValueColor(modificationDamageBonus));
            seg.Add(" = ", Colors.Gray);
            seg.Add(total.ToString(CultureInfo.InvariantCulture), ColorPalette.Success.GetColor());
            AddLine(lines, seg.Build());
        }

        /// <summary>
        /// The multipliers <see cref="Combat.Calculators.DamageCalculator.CalculateRawDamage"/> stacks on the base
        /// attack total for each swing, with live tuning values.
        /// </summary>
        private static void AppendPerSwingDamageSection(List<List<ColoredText>> lines, Character c, int baseTotal)
        {
            var balance = GameConfiguration.Instance.CombatBalance;
            var combatCfg = GameConfiguration.Instance.Combat;
            double critMult = balance.CriticalHitDamageMultiplier > 0 ? balance.CriticalHitDamageMultiplier : 2.0;
            double comboMult = balance.RollDamageMultipliers.ComboRollDamageMultiplier > 0 ? balance.RollDamageMultipliers.ComboRollDamageMultiplier : 1.0;
            double basicMult = balance.RollDamageMultipliers.BasicRollDamageMultiplier > 0 ? balance.RollDamageMultipliers.BasicRollDamageMultiplier : 1.0;
            double classMult = c.Weapon is WeaponItem cw ? ClassBalanceHelper.GetDamageMultiplier(cw.WeaponType) : 1.0;
            double overflowMult = SpeedCalculator.GetSpeedOverflowDamageMultiplier(c);

            AddBlank(lines);
            AddSection(lines, "Each swing in combat");
            AddNoteLine(lines,
                $"floor({baseTotal} × action damage % × AMP × roll band × class {classMult:0.##}" +
                (Math.Abs(overflowMult - 1.0) > 0.0001 ? $" × speed overflow {overflowMult:0.00}" : "") +
                ") − target armor, capped at " + Math.Max(1, combatCfg.MaximumDamageCap) + ".");
            AddNoteLine(lines,
                $"Roll band: crit ×{critMult:0.##}, action roll ×{comboMult:0.##}, normal hit ×{basicMult:0.##}. " +
                "Action damage % and AMP come from the strip card (hover a card to see them).");
            AddNoteLine(lines, "Tuning: CombatBalance.CriticalHitDamageMultiplier, CombatBalance.RollDamageMultipliers, ClassBalance (per weapon class), Combat.MaximumDamageCap.");
        }

        private static List<List<ColoredText>> BuildArmor(Character c, int maxLines)
        {
            var lines = new List<List<ColoredText>>();
            int total = c.GetTotalArmor();
            int h = c.Head is HeadItem hh ? hh.GetTotalArmor() : 0;
            int b = c.Body is ChestItem ch ? ch.GetTotalArmor() : 0;
            int lg = c.Legs is LegsItem li ? li.GetTotalArmor() : 0;
            int f = c.Feet is FeetItem ft ? ft.GetTotalArmor() : 0;
            int slotSum = h + b + lg + f;
            int globalBonus = total - slotSum;

            AddTitle(lines, "Defense");
            AddBlank(lines);
            AddHighlight(lines, "Damage reduction", HeroDefenseHudFormatter.FormatDamageReductionPercent(c));
            AddBlank(lines);
            AddStatRow(lines, "Current", ClassDefenseCalculator.GetDisplayedDefense(c));
            int rating = c.GetMaxArmor();
            AddStatRow(lines, "Base", rating);
            int playerBase = Math.Max(0, GameConfiguration.Instance.Combat.PlayerBaseArmor);
            if (playerBase > 0)
                AddStatRow(lines, "Character base", playerBase);
            if (c is not Enemy)
                AddStatRow(lines, "Level", Math.Max(0, c.Level));
            AddBlank(lines);
            AddSection(lines, "Incoming hits");
            double stanceMult = ClassDefenseCalculator.GetBlockPercent(c);
            int block = (int)Math.Round(stanceMult * 100.0, MidpointRounding.AwayFromZero);
            AddTextStatRow(lines, "Stance", $"{StandingBlock.GetStanceName(stanceMult)} ({block}%)");
            var preview = ClassDefenseCalculator.ApplyIncoming(c, 100, pierce: false);
            int effPts = (int)Math.Round(preview.EffectiveDefense, MidpointRounding.AwayFromZero);
            int kPts = (int)Math.Round(preview.K, MidpointRounding.AwayFromZero);
            AddTextStatRow(lines, "Effective", effPts.ToString(CultureInfo.InvariantCulture));
            AddTextStatRow(lines, "K", kPts.ToString(CultureInfo.InvariantCulture));
            AddBlank(lines);
            AddSection(lines, "Equipped pieces");
            AddStatRow(lines, "Head", h);
            AddStatRow(lines, "Body", b);
            AddStatRow(lines, "Legs", lg);
            AddStatRow(lines, "Feet", f);
            if (globalBonus != 0)
            {
                AddBlank(lines);
                AddSection(lines, "Global bonuses");
                AddSignedStatRow(lines, "From all gear (Defense stat)", globalBonus);
            }
            AddBlank(lines);
            AddSection(lines, "How it's calculated");
            int drPct = (int)Math.Round(preview.DrPercent * 100.0, MidpointRounding.AwayFromZero);
            AddNoteLine(lines,
                $"Effective = rating {preview.Rating} × stance {preview.ActionMult:0.##}" +
                (Math.Abs(preview.EffectiveDefense - preview.Rating * preview.ActionMult) > 0.5
                    ? $" + status {preview.EffectiveDefense - preview.Rating * preview.ActionMult:+0;-0}"
                    : "") +
                $" = {effPts}.");
            AddNoteLine(lines, $"Reduction = effective ÷ (effective + K) = {effPts} ÷ ({effPts} + {kPts}) = {drPct}% off each incoming hit (negative defense adds up to +50%).");
            AddNoteLine(lines, "Tuning: Combat.ArmorReductionFactor (K), Combat.PlayerBaseArmor. Piece armor: Armor.json + affixes.");
            AddNoteLine(lines, "Piece values include that item's defense rating and affixes.");
            AddNoteLine(lines, "Current Defense is base rating × stance (aggressive 0%, neutral 100%, defensive 180%). A named hit sets that stance. A miss or a normal hit sets neutral. An incoming hit also returns you to neutral. Level is added to the rating. Damage reduction is live incoming DR. Incoming multi-hit uses the stance on tick 0 only.");

            return Trim(lines, maxLines);
        }

        private static List<List<ColoredText>> BuildSpeed(Character c, int maxLines)
        {
            var lines = new List<List<ColoredText>>();
            var b = SpeedCalculator.GetBreakdown(c);
            var agiBreakdown = BuildAttributeBreakdown(c, "AGI");
            double pctPerPoint = GameConfiguration.Instance.Combat.GetWeaponSpeedPercentPerPoint();

            AddTitle(lines, "Attack time");
            AddBlank(lines);
            AddHighlight(lines, "Final", $"{b.FinalSeconds:F2}s");
            AddBlank(lines);
            AddSection(lines, "AGI (feeds speed)");
            AppendAttributeSummaryRows(lines, agiBreakdown, effectiveLabel: "Effective AGI");
            AddBlank(lines);
            AddSection(lines, "Calculation");
            AddDecimalStatRow(lines, "Base time", b.BaseSeconds, "F2", "s");
            AddDecimalStatRow(lines, $"Agility ({b.Agility})", b.AgilityFactor, "F3", "×");
            AddDecimalStatRow(lines, "Weapon base", b.WeaponBaseFactor, "F3", "×");
            if (Math.Abs(b.WeaponSpeedPoints) > 0.0001)
                AddTextStatRow(lines, "Weapon speed points",
                    $"{b.WeaponSpeedPoints:+0.##;-0.##} (+{b.WeaponSpeedPoints * pctPerPoint:0.#}% speed)");
            AddDecimalStatRow(lines, "Weapon total", b.WeaponFactor, "F3", "×");
            if (b.SlowFactor > 1.0001)
                AddDecimalStatRow(lines, $"Slow ({c.SlowTurns}t)", b.SlowFactor, "F3", "×");
            if (b.SetSpeedPercent > 0)
                AddTextStatRow(lines, "Set bonus", $"+{b.SetSpeedPercent:0.#}% speed");
            AddDecimalStatRow(lines, "Minimum", b.FloorSeconds, "F2", "s");
            AddBlank(lines);
            AddNoteLine(lines,
                $"{b.BaseSeconds:F2}s × {b.AgilityFactor:F3} (AGI) × {b.WeaponFactor:F3} (weapon)" +
                (Math.Abs(b.TempoFactor - 1.0) > 0.0001 ? $" × {b.TempoFactor:F3} (tempo)" : "") +
                $" = {b.RawSeconds:F2}s, minimum {b.FloorSeconds:F2}s → {b.FinalSeconds:F2}s.");
            AddNoteLine(lines, $"Each weapon speed point is +{pctPerPoint:0.#}% speed. Action length and next-action speed % scale this per swing.");
            double overflowMult = SpeedCalculator.GetSpeedOverflowDamageMultiplier(c);
            if (overflowMult > 1.0001)
                AddNoteLine(lines,
                    $"Faster than the minimum: the excess becomes damage ×{overflowMult:0.00} = 1 + {GameConfiguration.Instance.Combat.SpeedOverflowDamageEfficiency:0.##} × ({b.FloorSeconds:F2} ÷ {b.RawSeconds:F2} − 1).");
            AddNoteLine(lines, "Tuning: Combat.BaseAttackTime, Combat.MinimumAttackTime, Combat.AgilityMax (AGI curve), weapon speed per point; weapon catalog speed from Weapons.json, class speed from ClassBalance.");

            return Trim(lines, maxLines);
        }

        private static List<List<ColoredText>> BuildAmp(Character c, int maxLines)
        {
            var lines = new List<List<ColoredText>>();
            double baseAmp = c.GetComboAmplifier();
            var combo = c.GetComboActions();
            int slotCount = combo.Count > 0 ? combo.Count : Math.Max(1, ComboSequenceMaxHelper.GetEffectiveMax(c));
            var intBreakdown = BuildAttributeBreakdown(c, "INT");
            double queuedSheetAmpPct = c.PeekQueuedSheetAmpModPercentForDisplay();

            AddTitle(lines, "AMP");
            AddBlank(lines);
            AddHighlight(lines, "Base per combo step", $"{baseAmp:F2}×");
            AddBlank(lines);
            AddAmpScalingExample(lines, baseAmp);
            AddBlank(lines);
            AddSection(lines, "INT (feeds AMP)");
            AppendAttributeSummaryRows(lines, intBreakdown, effectiveLabel: "Effective INT");
            AddBlank(lines);
            AddSection(lines, combo.Count > 0 ? "Combo strip" : "Strip preview");
            for (int i = 0; i < slotCount; i++)
            {
                Action? a = combo.Count > i ? combo[i] : null;
                string slotLabel = $"Slot {i + 1}";
                if (a != null)
                {
                    string name = string.IsNullOrWhiteSpace(a.Name) ? "(unnamed)" : a.Name.Trim();
                    double mult = a.IsComboAction ? Math.Pow(baseAmp, i) : 1.0;
                    string suffix = a.IsComboAction ? "" : " (not combo)";
                    AddTextStatRow(lines, $"{slotLabel} ({name}){suffix}", $"{mult:F2}×");
                }
                else
                    AddTextStatRow(lines, slotLabel, $"{Math.Pow(baseAmp, i):F2}×");
            }
            AddBlank(lines);
            AddNoteLine(lines, $"INT eff. {intBreakdown.Effective} → base {baseAmp:F2}×; combo steps use Pow(base, zero-based index).");
            AppendAmpCurveNotes(lines, c, intBreakdown.Effective, baseAmp);
            if (queuedSheetAmpPct > 0.05)
                AddNoteLine(lines, $"Queued sheet AMP_MOD on next hero damage swing: +{queuedSheetAmpPct:0.#}%.");
            AddNoteLine(lines, "Non-combo strip actions stay at 1.00×; combat rolls may apply further swing multipliers.");

            return Trim(lines, maxLines);
        }

        /// <summary>Spells out <see cref="ComboAmplifierCurve.Compute"/> for the hero's INT and the combat-time INT potency multiplier.</summary>
        private static void AppendAmpCurveNotes(List<List<ColoredText>> lines, Character c, int effectiveInt, double baseAmp)
        {
            var comboCfg = GameConfiguration.Instance.ComboSystem;
            comboCfg.EnsureValidComboAmplifierDefaults();
            double maxAmp = comboCfg.ComboAmplifierMax;
            int maxInt = comboCfg.ComboAmplifierMaxTech;
            int kneeInt = Math.Max(1, maxInt - Utils.GameConstants.COMBO_AMPLIFIER_KNEE_TECH_OFFSET);
            double kneeAmp = Math.Max(1.0, maxAmp - Utils.GameConstants.COMBO_AMPLIFIER_KNEE_AMP_DELTA);
            int i = Math.Max(0, effectiveInt);

            string curve;
            if (maxAmp <= 1.0)
                curve = "AMP curve is off (max ≤ 1.00×), so the base is 1.00×.";
            else if (i >= maxInt)
                curve = $"INT {i} ≥ {maxInt}: base is the max, {maxAmp:0.00}×.";
            else if (i >= kneeInt)
                curve = $"INT {i} is past the knee ({kneeInt}): {kneeAmp:0.00} + ({i} − {kneeInt}) ÷ {Math.Max(1, maxInt - kneeInt)} × ({maxAmp:0.00} − {kneeAmp:0.00}).";
            else
                curve = $"Base = 1 + ({kneeAmp:0.00} − 1) × ({i} ÷ {kneeInt})^{comboCfg.ComboAmplifierCurveExponent:0.##}; reaches {kneeAmp:0.00}× at INT {kneeInt} and {maxAmp:0.00}× at INT {maxInt}.";
            AddNoteLine(lines, curve);

            double mythicPct = AnimalSetController.GetAmpBonusPct(c);
            if (mythicPct > 0)
                AddNoteLine(lines, $"Mythic animal set: base × (1 + {mythicPct:0.#}%).");

            double intPotency = c.GetIntelligenceAmpMultiplier();
            if (Math.Abs(intPotency - 1.0) > 0.0001)
                AddNoteLine(lines, $"In combat, AMP is also × {intPotency:0.00} from INT potency (1 + INT × Combat.IntelligenceAmpPerPoint).");
            AddNoteLine(lines, "Tuning: ComboSystem.ComboAmplifierMax / ComboAmplifierMaxTech / ComboAmplifierCurveExponent.");
        }

        private static void AppendAttributeSummaryRows(List<List<ColoredText>> lines, AttributeBreakdown b, string effectiveLabel = "Effective STR")
        {
            AddStatRow(lines, "Base", b.BaseValue);
            if (b.TempBonus != 0)
                AddSignedStatRow(lines, "Temp bonus", b.TempBonus);
            if (b.GodlikeBonus != 0)
                AddSignedStatRow(lines, "Godlike mod", b.GodlikeBonus);
            if (b.SkillBonus != 0)
                AddSignedStatRow(lines, "Skill tree", b.SkillBonus);
            AddStatRow(lines, "After character mods", b.AttributeModifiedValue);
            if (b.GearTotalBonus != 0)
                AddSignedStatRow(lines, "Gear", b.GearTotalBonus);
            AddStatRow(lines, effectiveLabel, b.Effective);
        }

        private readonly struct AttributeBreakdown
        {
            public AttributeBreakdown(
                int baseValue,
                int tempBonus,
                int godlikeBonus,
                int skillBonus,
                int attributeModifiedValue,
                int gearFlatBonus,
                int gearSuffixBonus,
                int gearTotalBonus,
                int effective)
            {
                BaseValue = baseValue;
                TempBonus = tempBonus;
                GodlikeBonus = godlikeBonus;
                SkillBonus = skillBonus;
                AttributeModifiedValue = attributeModifiedValue;
                GearFlatBonus = gearFlatBonus;
                GearSuffixBonus = gearSuffixBonus;
                GearTotalBonus = gearTotalBonus;
                Effective = effective;
            }

            public int BaseValue { get; }
            public int TempBonus { get; }
            public int GodlikeBonus { get; }
            public int SkillBonus { get; }
            public int AttributeModifiedValue { get; }
            public int GearFlatBonus { get; }
            public int GearSuffixBonus { get; }
            public int GearTotalBonus { get; }
            public int Effective { get; }
        }

        /// <summary>Plain-text one-liner for speed/amp tooltips that still use string lines.</summary>
        public static string FormatAttributeInputSummary(Character c, string code, string label, string? afterEffective = null)
        {
            var b = BuildAttributeBreakdown(c, code);
            string effectivePart = afterEffective == null
                ? $"effective {b.Effective}"
                : $"effective {b.Effective}, {afterEffective}";
            return $"{label}: base {b.BaseValue}; after character mods {b.AttributeModifiedValue}; gear {FormatSigned(b.GearTotalBonus)} => {effectivePart}.";
        }

        public static int GetEffectiveAttribute(Character c, string code) =>
            BuildAttributeBreakdown(c, code).Effective;

        private static AttributeBreakdown BuildAttributeBreakdown(Character c, string code)
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
            int god = code == "STR" ? c.GetModificationGodlikeBonus() : 0;
            int skill = SkillEffectRouter.Instance.GetSkillAttributeBonus(c, code);
            int gearFlat = c.Equipment.GetFlatEquipmentStatExcludingSuffixes(code);
            int gearTotal = c.Equipment.GetEquipmentStatBonus(code, c);
            int gearSuffix = gearTotal - gearFlat;
            int attributeModified = baseVal + temp + god + skill;
            int effective = code switch
            {
                "STR" => c.GetEffectiveStrength(),
                "AGI" => c.GetEffectiveAgility(),
                "TEC" => c.GetEffectiveTechnique(),
                "INT" => c.GetEffectiveIntelligence(),
                _ => attributeModified + gearTotal
            };

            return new AttributeBreakdown(baseVal, temp, god, skill, attributeModified, gearFlat, gearSuffix, gearTotal, effective);
        }

        private static void AddEquationLine(List<List<ColoredText>> lines, AttributeBreakdown b, bool includeGodlike)
        {
            var seg = new ColoredTextBuilder();
            seg.Add("Sum: ", Colors.Gray);
            seg.Add(b.BaseValue.ToString(CultureInfo.InvariantCulture), Colors.White);
            if (b.TempBonus != 0)
            {
                seg.Add(" ", Colors.Gray);
                seg.Add(FormatSigned(b.TempBonus), ValueColor(b.TempBonus));
            }
            if (includeGodlike && b.GodlikeBonus != 0)
            {
                seg.Add(" ", Colors.Gray);
                seg.Add(FormatSigned(b.GodlikeBonus), ValueColor(b.GodlikeBonus));
            }
            if (b.SkillBonus != 0)
            {
                seg.Add(" ", Colors.Gray);
                seg.Add(FormatSigned(b.SkillBonus), ValueColor(b.SkillBonus));
            }
            if (b.GearTotalBonus != 0)
            {
                seg.Add(" ", Colors.Gray);
                seg.Add(FormatSigned(b.GearTotalBonus), ValueColor(b.GearTotalBonus));
            }
            seg.Add(" = ", Colors.Gray);
            seg.Add(b.Effective.ToString(CultureInfo.InvariantCulture), ColorPalette.Success.GetColor());
            AddLine(lines, seg.Build());
        }

        private static void AddTitle(List<List<ColoredText>> lines, string title)
        {
            var b = new ColoredTextBuilder();
            b.Add(title, ColorPalette.Warning.GetColor());
            AddLine(lines, b.Build());
        }

        private static void AddHighlight(List<List<ColoredText>> lines, string label, string value)
        {
            var b = new ColoredTextBuilder();
            b.Add(label + ": ", ColorPalette.Info.GetColor());
            b.Add(value, ColorPalette.Success.GetColor());
            AddLine(lines, b.Build());
        }

        private static void AddSection(List<List<ColoredText>> lines, string title)
        {
            var b = new ColoredTextBuilder();
            b.Add(title, ColorPalette.Info.GetColor());
            AddLine(lines, b.Build());
        }

        private static void AddStatRow(List<List<ColoredText>> lines, string label, int value)
        {
            var b = new ColoredTextBuilder();
            b.Add("  ", Colors.Gray);
            b.Add(PadLabel(label), Colors.Gray);
            b.Add(value.ToString(CultureInfo.InvariantCulture), Colors.White);
            AddLine(lines, b.Build());
        }

        private static void AddSignedStatRow(List<List<ColoredText>> lines, string label, int value)
        {
            var b = new ColoredTextBuilder();
            b.Add("  ", Colors.Gray);
            b.Add(PadLabel(label), Colors.Gray);
            b.Add(FormatSigned(value), ValueColor(value));
            AddLine(lines, b.Build());
        }

        private static void AddDecimalStatRow(List<List<ColoredText>> lines, string label, double value, string format, string suffix = "")
        {
            var b = new ColoredTextBuilder();
            b.Add("  ", Colors.Gray);
            b.Add(PadLabel(label), Colors.Gray);
            b.Add(value.ToString(format, CultureInfo.InvariantCulture) + suffix, Colors.White);
            AddLine(lines, b.Build());
        }

        private static void AddTextStatRow(List<List<ColoredText>> lines, string label, string value)
        {
            var b = new ColoredTextBuilder();
            b.Add("  ", Colors.Gray);
            b.Add(PadLabel(label), Colors.Gray);
            b.Add(value, Colors.White);
            AddLine(lines, b.Build());
        }

        private static void AddNoteLine(List<List<ColoredText>> lines, string text)
        {
            var b = new ColoredTextBuilder();
            b.Add(text, Colors.DarkGray);
            AddLine(lines, b.Build());
        }

        /// <summary>One-line preview: slot 0 (base) through combo 4 multipliers side by side.</summary>
        private static void AddAmpScalingExample(List<List<ColoredText>> lines, double baseAmp)
        {
            var b = new ColoredTextBuilder();
            b.Add("Scaling: ", ColorPalette.Info.GetColor());
            string[] labels = { "Base", "Combo 1", "Combo 2", "Combo 3", "Combo 4" };
            for (int i = 0; i < labels.Length; i++)
            {
                if (i > 0)
                    b.Add("  ", Colors.DarkGray);
                b.Add(labels[i] + " ", Colors.Gray);
                b.Add($"{Math.Pow(baseAmp, i):F2}×", ColorPalette.Success.GetColor());
            }
            AddLine(lines, b.Build());
        }

        private static string PadLabel(string label)
        {
            const int width = 22;
            return label.Length >= width ? label + " " : label.PadRight(width);
        }

        private static Color ValueColor(int value) =>
            value > 0 ? ColorPalette.Success.GetColor()
            : value < 0 ? ColorPalette.Error.GetColor()
            : Colors.White;

        private static string FormatSigned(int value) =>
            value >= 0
                ? "+" + value.ToString(CultureInfo.InvariantCulture)
                : value.ToString(CultureInfo.InvariantCulture);

        private static void AddLine(List<List<ColoredText>> lines, List<ColoredText> line)
        {
            if (line != null)
                lines.Add(line);
        }

        private static void AddBlank(List<List<ColoredText>> lines) => lines.Add(new List<ColoredText>());

        private static List<List<ColoredText>> Trim(List<List<ColoredText>> lines, int maxLines) =>
            lines.Count <= maxLines ? lines : lines.GetRange(0, maxLines);
    }
}
