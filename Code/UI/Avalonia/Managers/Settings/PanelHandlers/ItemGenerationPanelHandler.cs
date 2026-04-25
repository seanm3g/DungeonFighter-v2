using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Avalonia.Controls;
using RPGGame;
using RPGGame.UI.Avalonia.Settings;

namespace RPGGame.UI.Avalonia.Managers.Settings.PanelHandlers
{
    /// <summary>
    /// Loads/saves per-rarity affix min / extra chance / max on the Item Generation settings tab (TuningConfig itemAffixByRarity).
    /// </summary>
    public sealed class ItemGenerationPanelHandler : ISettingsPanelHandler
    {
        private readonly Action<string, bool>? showStatusMessage;

        public ItemGenerationPanelHandler(Action<string, bool>? showStatusMessage = null)
        {
            this.showStatusMessage = showStatusMessage;
        }

        public string PanelType => "ItemGeneration";

        public void WireUp(UserControl panel)
        {
            if (panel is not ItemGenerationSettingsPanel) return;
            LoadSettings(panel);
        }

        public void LoadSettings(UserControl panel)
        {
            if (panel is not ItemGenerationSettingsPanel p) return;

            var cache = LootDataCache.Load();
            var tuning = GameConfiguration.Instance.ItemAffixByRarity;

            foreach (string rarity in ItemAffixByRaritySettings.StandardLootRarities)
            {
                var tableRow = cache.RarityData?.FirstOrDefault(r =>
                    r.Name.Equals(rarity, StringComparison.OrdinalIgnoreCase));

                if (tuning != null && tuning.TryGetForRarity(rarity, out var entry))
                {
                    SetInt(p, $"AffixPrefixMin{rarity}", entry.PrefixSlots);
                    SetChancePercent(p, $"AffixPrefixChance{rarity}", entry.PrefixExtraChance);
                    SetOptionalIntText(p, $"AffixPrefixMax{rarity}", entry.PrefixSlotsMax);

                    SetInt(p, $"AffixStatMin{rarity}", entry.StatSuffixes);
                    SetChancePercent(p, $"AffixStatChance{rarity}", entry.StatSuffixExtraChance);
                    SetOptionalIntText(p, $"AffixStatMax{rarity}", entry.StatSuffixesMax);

                    SetInt(p, $"AffixActionMin{rarity}", entry.ActionBonuses);
                    SetChancePercent(p, $"AffixActionChance{rarity}", entry.ActionExtraChance);
                    SetOptionalIntText(p, $"AffixActionMax{rarity}", entry.ActionBonusesMax);
                }
                else
                {
                    var rule = ItemAffixByRaritySettings.GetResolvedAffixRule(rarity, tableRow, null);
                    SetInt(p, $"AffixPrefixMin{rarity}", rule.PrefixMin);
                    SetText(p, $"AffixPrefixChance{rarity}", "0");
                    SetText(p, $"AffixPrefixMax{rarity}", "");

                    SetInt(p, $"AffixStatMin{rarity}", rule.StatMin);
                    SetText(p, $"AffixStatChance{rarity}", "0");
                    SetText(p, $"AffixStatMax{rarity}", "");

                    SetInt(p, $"AffixActionMin{rarity}", rule.ActionMin);
                    SetText(p, $"AffixActionChance{rarity}", "0");
                    SetText(p, $"AffixActionMax{rarity}", "");
                }
            }
        }

        public void SaveSettings(UserControl panel)
        {
            if (panel is not ItemGenerationSettingsPanel p) return;

            var dict = new Dictionary<string, ItemAffixPerRarityEntry>(StringComparer.OrdinalIgnoreCase);
            foreach (string rarity in ItemAffixByRaritySettings.StandardLootRarities)
            {
                if (!TryReadRow(p, rarity, out var row, out string? err))
                {
                    showStatusMessage?.Invoke(err ?? "Invalid affix row.", false);
                    return;
                }

                dict[rarity] = row;
            }

            var cfg = GameConfiguration.Instance;
            cfg.ItemAffixByRarity ??= new ItemAffixByRaritySettings();
            cfg.ItemAffixByRarity.PerRarity = dict;

            if (!cfg.SaveToFile())
                showStatusMessage?.Invoke("Failed to save TuningConfig.json (item affix).", false);
        }

        private static void SetInt(ItemGenerationSettingsPanel p, string controlName, int value)
        {
            var tb = p.FindControl<TextBox>(controlName);
            if (tb != null)
                tb.Text = value.ToString(CultureInfo.InvariantCulture);
        }

        private static void SetText(ItemGenerationSettingsPanel p, string controlName, string text)
        {
            var tb = p.FindControl<TextBox>(controlName);
            if (tb != null)
                tb.Text = text;
        }

        private static void SetChancePercent(ItemGenerationSettingsPanel p, string controlName, double chance01)
        {
            double pct = Math.Clamp(chance01, 0, 1) * 100.0;
            string s = Math.Abs(pct - Math.Round(pct)) < 0.0001
                ? ((int)Math.Round(pct)).ToString(CultureInfo.InvariantCulture)
                : pct.ToString("0.##", CultureInfo.InvariantCulture);
            SetText(p, controlName, s);
        }

        private static void SetOptionalIntText(ItemGenerationSettingsPanel p, string controlName, int? value)
        {
            if (!value.HasValue)
                SetText(p, controlName, "");
            else
                SetInt(p, controlName, value.Value);
        }

        private static bool TryReadRow(
            ItemGenerationSettingsPanel p,
            string rarity,
            out ItemAffixPerRarityEntry entry,
            out string? error)
        {
            entry = new ItemAffixPerRarityEntry();
            error = null;

            if (!TryParseInt(p, $"AffixPrefixMin{rarity}", 0, 3, out int pMin, out error))
                return false;
            if (!TryParseChancePercent(p, $"AffixPrefixChance{rarity}", out double pCh, ref error))
                return false;
            if (!TryParseOptionalIntNonNeg(p, $"AffixPrefixMax{rarity}", out int? pMax, ref error))
                return false;
            if (pMax.HasValue)
            {
                if (pMax.Value > 3)
                {
                    error = $"Prefix max for {rarity} must be ≤ 3.";
                    return false;
                }

                if (pMax.Value < pMin)
                {
                    error = $"Prefix max for {rarity} must be ≥ min ({pMin}).";
                    return false;
                }
            }

            if (!TryParseInt(p, $"AffixStatMin{rarity}", 0, 999, out int sMin, out error))
                return false;
            if (!TryParseChancePercent(p, $"AffixStatChance{rarity}", out double sCh, ref error))
                return false;
            if (!TryParseOptionalIntNonNeg(p, $"AffixStatMax{rarity}", out int? sMax, ref error))
                return false;
            if (sMax.HasValue && sMax.Value < sMin)
            {
                error = $"Stat max for {rarity} must be ≥ min ({sMin}).";
                return false;
            }

            if (!TryParseInt(p, $"AffixActionMin{rarity}", 0, 999, out int aMin, out error))
                return false;
            if (!TryParseChancePercent(p, $"AffixActionChance{rarity}", out double aCh, ref error))
                return false;
            if (!TryParseOptionalIntNonNeg(p, $"AffixActionMax{rarity}", out int? aMax, ref error))
                return false;
            if (aMax.HasValue && aMax.Value < aMin)
            {
                error = $"Action max for {rarity} must be ≥ min ({aMin}).";
                return false;
            }

            entry.PrefixSlots = pMin;
            entry.PrefixExtraChance = pCh;
            entry.PrefixSlotsMax = pMax;
            entry.StatSuffixes = sMin;
            entry.StatSuffixExtraChance = sCh;
            entry.StatSuffixesMax = sMax;
            entry.ActionBonuses = aMin;
            entry.ActionExtraChance = aCh;
            entry.ActionBonusesMax = aMax;
            return true;
        }

        private static bool TryParseInt(
            ItemGenerationSettingsPanel p,
            string name,
            int min,
            int max,
            out int value,
            out string? error)
        {
            value = 0;
            error = null;
            var tb = p.FindControl<TextBox>(name);
            if (!int.TryParse((tb?.Text ?? "").Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out value) ||
                value < min || value > max)
            {
                error = $"Invalid integer for {name} (need {min}–{max}).";
                return false;
            }

            return true;
        }

        private static bool TryParseOptionalIntNonNeg(
            ItemGenerationSettingsPanel p,
            string name,
            out int? value,
            ref string? error)
        {
            value = null;
            var tb = p.FindControl<TextBox>(name);
            string t = (tb?.Text ?? "").Trim();
            if (string.IsNullOrEmpty(t))
                return true;

            if (!int.TryParse(t, NumberStyles.Integer, CultureInfo.InvariantCulture, out int v) || v < 0)
            {
                error = $"Invalid optional max for {name} (non-negative int or empty).";
                return false;
            }

            value = v;
            return true;
        }

        private static bool TryParseChancePercent(
            ItemGenerationSettingsPanel p,
            string name,
            out double chance01,
            ref string? error)
        {
            chance01 = 0;
            var tb = p.FindControl<TextBox>(name);
            string t = (tb?.Text ?? "").Trim();
            if (string.IsNullOrEmpty(t))
                return true;

            if (!double.TryParse(t, NumberStyles.Float, CultureInfo.InvariantCulture, out double pct) ||
                pct < 0 || pct > 100)
            {
                error = $"Invalid extra chance for {name} (0–100).";
                return false;
            }

            chance01 = pct / 100.0;
            return true;
        }
    }
}
