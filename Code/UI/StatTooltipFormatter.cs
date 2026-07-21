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
                "stat:naivete" => BuildNaivete(character, maxLines),
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
                        $"Milestones: +{steps.HitSteps} HIT, +{steps.ComboSteps} COMBO, +{steps.CritSteps} CRIT threshold steps (easier bands).");
                else
                    AddNoteLine(lines, "Milestones at TECH 10+ improve HIT/COMBO/CRIT thresholds each attack.");
                double amp = ComboAmplifierCurve.Compute(b.Effective, GameConfiguration.Instance.ComboSystem);
                AddNoteLine(lines, $"Feeds combo AMP: {amp:F2}× per combo step (slow TECH power ramp + late finish).");
            }

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
            int total = attrBonus + weaponDamage + equipmentDamageBonus + modificationDamageBonus;
            var (primaryCode, primaryLabel) = GetPrimaryAttributeDisplay(c);
            bool primaryIsStrength = primaryCode == DynamicAttributeCategoryResolver.CodeStrength;

            AddTitle(lines, "Damage");
            AddBlank(lines);
            AddHighlight(lines, "Base attack total", total.ToString(CultureInfo.InvariantCulture));
            AddBlank(lines);
            AddSection(lines, "Attributes (STR + primary)");
            AddStatRow(lines, "Strength (always)", strEff);
            AddStatRow(lines, $"Primary ({primaryCode})", primaryEff);
            AddStatRow(lines, "Attribute subtotal", attrBonus);
            AddBlank(lines);
            AddSection(lines, "Gear & mods");
            AddStatRow(lines, "Weapon", weaponDamage);
            AddSignedStatRow(lines, "Equipment", equipmentDamageBonus);
            AddSignedStatRow(lines, "Modifications", modificationDamageBonus);
            AddBlank(lines);
            AddDamageEquationLine(lines, strEff, primaryCode, primaryEff, weaponDamage, equipmentDamageBonus, modificationDamageBonus, total);
            if (primaryIsStrength)
                AddNoteLine(lines, "STR is your primary — Strength and Primary both use the same value.");
            else
                AddNoteLine(lines, $"Primary is {primaryLabel} ({primaryCode}), your highest effective stat.");
            AddNoteLine(lines, "Combat adds d20 rolls, AMP, and action multipliers on top of this base.");

            return Trim(lines, maxLines);
        }

        private static List<List<ColoredText>> BuildNaivete(Character c, int maxLines)
        {
            var lines = new List<List<ColoredText>>();
            int naivete = NaiveteBalanceHelper.ComputeNaivete(c);
            if (naivete <= 0)
                return Trim(lines, maxLines);

            int hitSteps = NaiveteBalanceHelper.GetHitSteps(c);
            var config = NaiveteBalanceHelper.GetConfig();

            AddTitle(lines, "Naiveté");
            AddBlank(lines);
            AddHighlight(lines, "Remaining", naivete.ToString(CultureInfo.InvariantCulture));
            AddBlank(lines);
            AddSection(lines, "Effect");
            AddStatRow(lines, "HIT threshold steps", hitSteps);
            AddNoteLine(lines, "Easier to hit while you are still inexperienced in combat.");
            AddBlank(lines);
            AddSection(lines, "Progress");
            AddStatRow(lines, "Hero level", c.Level);
            if (config != null)
            {
                AddStatRow(lines, "Starts at", config.StartingNaivete);
                AddStatRow(lines, "Fades by", 1);
            }
            AddNoteLine(lines, "Starts as a single digit and drops by 1 each level.");

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
            int weaponDamage,
            int equipmentDamageBonus,
            int modificationDamageBonus,
            int total)
        {
            var seg = new ColoredTextBuilder();
            seg.Add("Equation: ", ColorPalette.Info.GetColor());
            seg.Add(strEff.ToString(CultureInfo.InvariantCulture), Colors.White);
            seg.Add(" + ", Colors.Gray);
            seg.Add($"{primaryCode} {primaryEff}", Colors.White);
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

            AddTitle(lines, "Armor");
            AddBlank(lines);
            AddHighlight(lines, "Total", total.ToString(CultureInfo.InvariantCulture));
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
                AddSignedStatRow(lines, "From all gear (Armor stat)", globalBonus);
            }
            AddBlank(lines);
            AddNoteLine(lines, "Piece values include that item's armor stats and affixes.");
            AddNoteLine(lines, "Armor is flat damage reduction and is not consumed by hits.");

            return Trim(lines, maxLines);
        }

        private static List<List<ColoredText>> BuildSpeed(Character c, int maxLines)
        {
            var lines = new List<List<ColoredText>>();
            var b = ComputeSpeedBreakdown(c);
            var agiBreakdown = BuildAttributeBreakdown(c, "AGI");

            AddTitle(lines, "Attack time");
            AddBlank(lines);
            AddHighlight(lines, "Final", $"{b.FinalSeconds:F2}s");
            AddBlank(lines);
            AddSection(lines, "AGI (feeds speed)");
            AppendAttributeSummaryRows(lines, agiBreakdown, effectiveLabel: "Effective AGI");
            AddBlank(lines);
            AddSection(lines, "Calculation");
            AddDecimalStatRow(lines, "Base time", b.BaseAttackTime, "F3", "s");
            AddDecimalStatRow(lines, "AGI curve multiplier", b.AgiMultiplier, "F3");
            AddDecimalStatRow(lines, "After AGI curve", b.AfterAgiSeconds, "F3", "s");
            AddDecimalStatRow(lines, "Weapon time multiplier", b.WeaponTimeMultiplier, "F2");
            AddDecimalStatRow(lines, "After weapon", b.AfterWeaponSeconds, "F3", "s");
            AddDecimalStatRow(lines, "Equipment attack-speed", b.EquipmentSpeedBonus, "F3", "s (subtracted)");
            if (b.SlowTurns > 0)
                AddDecimalStatRow(lines, $"Slow (×{b.SlowMultiplier:F3}, {b.SlowTurns}t)", b.AfterSlowSeconds, "F3", "s");
            if (Math.Abs(b.ModSpeedMultiplier - 1.0) > 0.0001)
                AddDecimalStatRow(lines, "Modification speed mult.", b.ModSpeedMultiplier, "F4");
            AddDecimalStatRow(lines, "Minimum cap", b.MinimumCapSeconds, "F2", "s");
            AddBlank(lines);
            AddNoteLine(lines,
                $"{b.BaseAttackTime:F3}s × {b.AgiMultiplier:F3} × {b.WeaponTimeMultiplier:F2} − {b.EquipmentSpeedBonus:F3}s" +
                (b.SlowTurns > 0 ? $" × {b.SlowMultiplier:F3}" : "") +
                (Math.Abs(b.ModSpeedMultiplier - 1.0) > 0.0001 ? $" ÷ {b.ModSpeedMultiplier:F4}" : "") +
                $" = {b.FinalSeconds:F2}s (clamped ≥ {b.MinimumCapSeconds:F2}s).");
            AddNoteLine(lines, "Seconds per attack on the panel; action length scales time in combat rolls.");

            return Trim(lines, maxLines);
        }

        private static List<List<ColoredText>> BuildAmp(Character c, int maxLines)
        {
            var lines = new List<List<ColoredText>>();
            double baseAmp = c.GetComboAmplifier();
            var combo = c.GetComboActions();
            int slotCount = combo.Count > 0 ? combo.Count : Math.Max(1, ComboSequenceMaxHelper.GetEffectiveMax(c));
            var tecBreakdown = BuildAttributeBreakdown(c, "TEC");
            double queuedSheetAmpPct = c.PeekQueuedSheetAmpModPercentForDisplay();

            AddTitle(lines, "AMP");
            AddBlank(lines);
            AddHighlight(lines, "Base per combo step", $"{baseAmp:F2}×");
            AddBlank(lines);
            AddAmpScalingExample(lines, baseAmp);
            AddBlank(lines);
            AddSection(lines, "TECH (feeds AMP)");
            AppendAttributeSummaryRows(lines, tecBreakdown, effectiveLabel: "Effective TECH");
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
            AddNoteLine(lines, $"TECH eff. {tecBreakdown.Effective} → base {baseAmp:F2}×; combo steps use Pow(base, zero-based index).");
            if (queuedSheetAmpPct > 0.05)
                AddNoteLine(lines, $"Queued sheet AMP_MOD on next hero damage swing: +{queuedSheetAmpPct:0.#}%.");
            AddNoteLine(lines, "Non-combo strip actions stay at 1.00×; combat rolls may apply further swing multipliers.");

            return Trim(lines, maxLines);
        }

        private readonly struct SpeedBreakdown
        {
            public double FinalSeconds { get; init; }
            public double BaseAttackTime { get; init; }
            public int ClampedAgility { get; init; }
            public double AgiMultiplier { get; init; }
            public double AfterAgiSeconds { get; init; }
            public double WeaponTimeMultiplier { get; init; }
            public double AfterWeaponSeconds { get; init; }
            public double EquipmentSpeedBonus { get; init; }
            public int SlowTurns { get; init; }
            public double SlowMultiplier { get; init; }
            public double AfterSlowSeconds { get; init; }
            public double ModSpeedMultiplier { get; init; }
            public double MinimumCapSeconds { get; init; }
        }

        private static SpeedBreakdown ComputeSpeedBreakdown(Character c)
        {
            var tuning = GameConfiguration.Instance;
            double baseAttackTime = tuning.Combat.BaseAttackTime;
            int agility = GetEffectiveAttribute(c, "AGI");
            int agilityMin = tuning.Combat.AgilityMin;
            int agilityMax = tuning.Combat.AgilityMax;
            int clampedAgility = Math.Max(agilityMin, Math.Min(agilityMax, agility));
            double minMul = tuning.Combat.AgilityMinSpeedMultiplier;
            double maxMul = tuning.Combat.AgilityMaxSpeedMultiplier;
            double agilityRange = agilityMax - agilityMin;
            double normalizedProgress = agilityRange > 0 ? (clampedAgility - agilityMin) / agilityRange : 0.0;
            double curvedProgress = Math.Sqrt(normalizedProgress);
            double agiMultiplier = minMul + (maxMul - minMul) * curvedProgress;
            double afterAgi = baseAttackTime * agiMultiplier;

            double weaponTimeMul = 1.0;
            if (c.Weapon is WeaponItem w)
                weaponTimeMul = SpeedCalculator.GetWeaponAttackTimeMultiplier(w);
            double afterWeapon = afterAgi * weaponTimeMul;

            double equipmentSpeedBonus = c.GetEquipmentAttackSpeedBonus();
            double afterEquip = Math.Max(0.001, afterWeapon - equipmentSpeedBonus);

            int slowTurns = c.SlowTurns;
            double slowMultiplier = c.SlowMultiplier;
            double afterSlow = slowTurns > 0 ? afterEquip * slowMultiplier : afterEquip;

            double modSpeedMul = Math.Max(0.0001, c.GetModificationSpeedMultiplier());
            double afterMod = afterSlow / modSpeedMul;

            double minCap = Math.Max(0.01, tuning.Combat.MinimumAttackTime);
            double final = Math.Max(minCap, afterMod);

            return new SpeedBreakdown
            {
                FinalSeconds = c.GetTotalAttackSpeed(),
                BaseAttackTime = baseAttackTime,
                ClampedAgility = clampedAgility,
                AgiMultiplier = agiMultiplier,
                AfterAgiSeconds = afterAgi,
                WeaponTimeMultiplier = weaponTimeMul,
                AfterWeaponSeconds = afterWeapon,
                EquipmentSpeedBonus = equipmentSpeedBonus,
                SlowTurns = slowTurns,
                SlowMultiplier = slowMultiplier,
                AfterSlowSeconds = afterSlow,
                ModSpeedMultiplier = modSpeedMul,
                MinimumCapSeconds = minCap
            };
        }

        private static void AppendAttributeSummaryRows(List<List<ColoredText>> lines, AttributeBreakdown b, string effectiveLabel = "Effective STR")
        {
            AddStatRow(lines, "Base", b.BaseValue);
            if (b.TempBonus != 0)
                AddSignedStatRow(lines, "Temp bonus", b.TempBonus);
            if (b.GodlikeBonus != 0)
                AddSignedStatRow(lines, "Godlike mod", b.GodlikeBonus);
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
                int attributeModifiedValue,
                int gearFlatBonus,
                int gearSuffixBonus,
                int gearTotalBonus,
                int effective)
            {
                BaseValue = baseValue;
                TempBonus = tempBonus;
                GodlikeBonus = godlikeBonus;
                AttributeModifiedValue = attributeModifiedValue;
                GearFlatBonus = gearFlatBonus;
                GearSuffixBonus = gearSuffixBonus;
                GearTotalBonus = gearTotalBonus;
                Effective = effective;
            }

            public int BaseValue { get; }
            public int TempBonus { get; }
            public int GodlikeBonus { get; }
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
            int gearFlat = c.Equipment.GetFlatEquipmentStatExcludingSuffixes(code);
            int gearTotal = c.Equipment.GetEquipmentStatBonus(code, c);
            int gearSuffix = gearTotal - gearFlat;
            int attributeModified = baseVal + temp + god;
            int effective = code switch
            {
                "STR" => c.GetEffectiveStrength(),
                "AGI" => c.GetEffectiveAgility(),
                "TEC" => c.GetEffectiveTechnique(),
                "INT" => c.GetEffectiveIntelligence(),
                _ => attributeModified + gearTotal
            };

            return new AttributeBreakdown(baseVal, temp, god, attributeModified, gearFlat, gearSuffix, gearTotal, effective);
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
