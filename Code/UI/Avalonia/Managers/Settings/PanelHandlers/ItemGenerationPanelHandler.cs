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
    /// Loads/saves per-rarity affix tuning on the Item Generation tab (TuningConfig itemAffixByRarity),
    /// with separate rows per item category (Head, Chest, Feet, Weapon) and combo-sequence cap fields on LootSystem.
    /// </summary>
    public sealed class ItemGenerationPanelHandler : ISettingsPanelHandler
    {
        private static readonly string[] AffixSlotKeys = { "Head", "Chest", "Feet", "Weapon" };

        private readonly Action<string, bool>? showStatusMessage;

        /// <summary>Scratch copy: outer key Head/Chest/Feet/Weapon, inner rarity → entry.</summary>
        private Dictionary<string, Dictionary<string, ItemAffixPerRarityEntry>>? _affixScratch;

        private string _activeAffixSlot = "Head";

        public ItemGenerationPanelHandler(Action<string, bool>? showStatusMessage = null)
        {
            this.showStatusMessage = showStatusMessage;
        }

        public string PanelType => "ItemGeneration";

        public void WireUp(UserControl panel)
        {
            if (panel is not ItemGenerationSettingsPanel p) return;

            LoadSettings(panel);

            var slotCombo = p.FindControl<ComboBox>("AffixSlotCombo");
            if (slotCombo != null)
            {
                slotCombo.ItemsSource = AffixSlotKeys.ToList();
                slotCombo.SelectionChanged -= OnAffixSlotChanged;
                slotCombo.SelectionChanged += OnAffixSlotChanged;
            }
        }

        private void OnAffixSlotChanged(object? sender, SelectionChangedEventArgs e)
        {
            if (sender is not ComboBox cb || cb.SelectedItem is not string next)
                return;

            var panel = FindPanelHost(cb);
            if (panel == null || _affixScratch == null)
                return;

            PersistUiToScratch(panel, _activeAffixSlot);
            _activeAffixSlot = next;
            PaintUiFromScratch(panel, _activeAffixSlot);
        }

        private static ItemGenerationSettingsPanel? FindPanelHost(ComboBox cb)
        {
            Control? c = cb;
            while (c != null)
            {
                if (c is ItemGenerationSettingsPanel p)
                    return p;
                c = c.Parent as Control;
            }

            return null;
        }

        public void LoadSettings(UserControl panel)
        {
            if (panel is not ItemGenerationSettingsPanel p) return;

            var cache = LootDataCache.Load();
            var tuning = GameConfiguration.Instance.ItemAffixByRarity;
            _affixScratch = BuildScratchFromTuning(tuning, cache);
            _activeAffixSlot = "Head";

            var slotCombo = p.FindControl<ComboBox>("AffixSlotCombo");
            if (slotCombo != null)
            {
                if (slotCombo.ItemsSource == null)
                    slotCombo.ItemsSource = AffixSlotKeys.ToList();
                slotCombo.SelectedItem = _activeAffixSlot;
                if (slotCombo.SelectedIndex < 0)
                    slotCombo.SelectedIndex = 0;
            }

            PaintUiFromScratch(p, _activeAffixSlot);

            var loot = GameConfiguration.Instance.LootSystem;
            SetInt(p, "ComboSeqBaseMaxText", loot?.ComboSequenceBaseMax ?? 2);
            SetInt(p, "ComboSeqAbsMaxText", loot?.ComboSequenceAbsoluteMax ?? 8);
        }

        public void SaveSettings(UserControl panel)
        {
            if (panel is not ItemGenerationSettingsPanel p) return;

            if (_affixScratch == null)
                _affixScratch = new Dictionary<string, Dictionary<string, ItemAffixPerRarityEntry>>(StringComparer.OrdinalIgnoreCase);

            foreach (string rarity in ItemAffixByRaritySettings.StandardLootRarities)
            {
                if (!TryReadRow(p, rarity, out _, out string? rowErr))
                {
                    showStatusMessage?.Invoke(rowErr ?? "Invalid affix row.", false);
                    return;
                }
            }

            PersistUiToScratch(p, _activeAffixSlot);

            var cfg = GameConfiguration.Instance;
            cfg.ItemAffixByRarity ??= new ItemAffixByRaritySettings();
            cfg.ItemAffixByRarity.PerItemType = CloneScratch(_affixScratch);
            if (_affixScratch.TryGetValue("Weapon", out var weaponRows) && weaponRows != null)
                cfg.ItemAffixByRarity.PerRarity = new Dictionary<string, ItemAffixPerRarityEntry>(weaponRows, StringComparer.OrdinalIgnoreCase);

            if (!TryParseInt(p, "ComboSeqBaseMaxText", 1, 99, out int baseMax, out string? capErr))
            {
                showStatusMessage?.Invoke(capErr ?? "Invalid combo base max.", false);
                return;
            }

            if (!TryParseInt(p, "ComboSeqAbsMaxText", 1, 99, out int absMax, out capErr))
            {
                showStatusMessage?.Invoke(capErr ?? "Invalid combo absolute max.", false);
                return;
            }

            if (absMax < baseMax)
            {
                showStatusMessage?.Invoke("Combo absolute max must be ≥ base max.", false);
                return;
            }

            cfg.LootSystem ??= new LootSystemConfig();
            cfg.LootSystem.ComboSequenceBaseMax = baseMax;
            cfg.LootSystem.ComboSequenceAbsoluteMax = absMax;

            if (!cfg.SaveToFile())
                showStatusMessage?.Invoke("Failed to save TuningConfig.json (item affix / loot).", false);
        }

        private static Dictionary<string, Dictionary<string, ItemAffixPerRarityEntry>> CloneScratch(
            Dictionary<string, Dictionary<string, ItemAffixPerRarityEntry>> src)
        {
            var o = new Dictionary<string, Dictionary<string, ItemAffixPerRarityEntry>>(StringComparer.OrdinalIgnoreCase);
            foreach (var kv in src)
            {
                if (kv.Value == null)
                    continue;
                o[kv.Key] = new Dictionary<string, ItemAffixPerRarityEntry>(kv.Value, StringComparer.OrdinalIgnoreCase);
            }

            return o;
        }

        private static Dictionary<string, Dictionary<string, ItemAffixPerRarityEntry>> BuildScratchFromTuning(
            ItemAffixByRaritySettings? tuning,
            LootDataCache cache)
        {
            var scratch = new Dictionary<string, Dictionary<string, ItemAffixPerRarityEntry>>(StringComparer.OrdinalIgnoreCase);

            foreach (string slot in AffixSlotKeys)
            {
                var dict = new Dictionary<string, ItemAffixPerRarityEntry>(StringComparer.OrdinalIgnoreCase);
                ItemType it = SlotKeyToItemType(slot);

                foreach (string rarity in ItemAffixByRaritySettings.StandardLootRarities)
                {
                    var tableRow = cache.RarityData?.FirstOrDefault(r =>
                        r.Name.Equals(rarity, StringComparison.OrdinalIgnoreCase));

                    if (tuning != null && tuning.TryGetForItemTypeAndRarity(it, rarity, out var typed))
                        dict[rarity] = CloneEntry(typed);
                    else if (tuning != null && tuning.TryGetForRarity(rarity, out var legacy))
                        dict[rarity] = CloneEntry(legacy);
                    else
                    {
                        var rule = ItemAffixByRaritySettings.GetResolvedAffixRule(rarity, tableRow, null, it);
                        dict[rarity] = new ItemAffixPerRarityEntry
                        {
                            PrefixSlots = rule.PrefixMin,
                            PrefixExtraChance = 0,
                            PrefixSlotsMax = null,
                            StatSuffixes = rule.StatMin,
                            StatSuffixExtraChance = 0,
                            StatSuffixesMax = null,
                            ActionBonuses = rule.ActionMin,
                            ActionExtraChance = 0,
                            ActionBonusesMax = null,
                            ExtraComboSlots = rule.ExtraComboSlotsMin,
                            ExtraComboSlotsExtraChance = 0,
                            ExtraComboSlotsMax = null
                        };
                    }
                }

                scratch[slot] = dict;
            }

            if (tuning?.PerItemType != null)
            {
                foreach (var kv in tuning.PerItemType)
                {
                    if (kv.Value == null || kv.Value.Count == 0)
                        continue;
                    string key = AffixSlotKeys.FirstOrDefault(s => s.Equals(kv.Key, StringComparison.OrdinalIgnoreCase)) ?? kv.Key;
                    scratch[key] = new Dictionary<string, ItemAffixPerRarityEntry>(kv.Value, StringComparer.OrdinalIgnoreCase);
                }
            }

            return scratch;
        }

        private static ItemType SlotKeyToItemType(string slot) =>
            slot.Equals("Head", StringComparison.OrdinalIgnoreCase) ? ItemType.Head :
            slot.Equals("Chest", StringComparison.OrdinalIgnoreCase) ? ItemType.Chest :
            slot.Equals("Feet", StringComparison.OrdinalIgnoreCase) ? ItemType.Feet :
            ItemType.Weapon;

        private static ItemAffixPerRarityEntry CloneEntry(ItemAffixPerRarityEntry e) =>
            new()
            {
                PrefixSlots = e.PrefixSlots,
                PrefixExtraChance = e.PrefixExtraChance,
                PrefixSlotsMax = e.PrefixSlotsMax,
                StatSuffixes = e.StatSuffixes,
                StatSuffixExtraChance = e.StatSuffixExtraChance,
                StatSuffixesMax = e.StatSuffixesMax,
                ActionBonuses = e.ActionBonuses,
                ActionExtraChance = e.ActionExtraChance,
                ActionBonusesMax = e.ActionBonusesMax,
                ExtraComboSlots = e.ExtraComboSlots,
                ExtraComboSlotsExtraChance = e.ExtraComboSlotsExtraChance,
                ExtraComboSlotsMax = e.ExtraComboSlotsMax
            };

        private void PersistUiToScratch(ItemGenerationSettingsPanel p, string slotKey)
        {
            if (_affixScratch == null)
                return;

            var dict = new Dictionary<string, ItemAffixPerRarityEntry>(StringComparer.OrdinalIgnoreCase);
            foreach (string rarity in ItemAffixByRaritySettings.StandardLootRarities)
            {
                if (!TryReadRow(p, rarity, out var row, out _))
                    return;
                dict[rarity] = row;
            }

            _affixScratch[slotKey] = dict;
        }

        private void PaintUiFromScratch(ItemGenerationSettingsPanel p, string slotKey)
        {
            if (_affixScratch == null || !_affixScratch.TryGetValue(slotKey, out var dict) || dict == null)
                return;

            foreach (string rarity in ItemAffixByRaritySettings.StandardLootRarities)
            {
                if (!dict.TryGetValue(rarity, out var entry) || entry == null)
                    continue;

                SetInt(p, $"AffixPrefixMin{rarity}", entry.PrefixSlots);
                SetChancePercent(p, $"AffixPrefixChance{rarity}", entry.PrefixExtraChance);
                SetOptionalIntText(p, $"AffixPrefixMax{rarity}", entry.PrefixSlotsMax);

                SetInt(p, $"AffixStatMin{rarity}", entry.StatSuffixes);
                SetChancePercent(p, $"AffixStatChance{rarity}", entry.StatSuffixExtraChance);
                SetOptionalIntText(p, $"AffixStatMax{rarity}", entry.StatSuffixesMax);

                SetInt(p, $"AffixActionMin{rarity}", entry.ActionBonuses);
                SetChancePercent(p, $"AffixActionChance{rarity}", entry.ActionExtraChance);
                SetOptionalIntText(p, $"AffixActionMax{rarity}", entry.ActionBonusesMax);

                SetChancePercent(p, $"AffixExtraComboChance{rarity}", entry.ExtraComboSlotsExtraChance);
                SetInt(p, $"AffixExtraComboMin{rarity}", entry.ExtraComboSlots);
                SetOptionalIntText(p, $"AffixExtraComboMax{rarity}", entry.ExtraComboSlotsMax);
            }
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

            if (!TryParseChancePercent(p, $"AffixExtraComboChance{rarity}", out double eCh, ref error))
                return false;
            if (!TryParseInt(p, $"AffixExtraComboMin{rarity}", 0, 99, out int eMin, out error))
                return false;
            if (!TryParseOptionalIntNonNeg(p, $"AffixExtraComboMax{rarity}", out int? eMax, ref error))
                return false;
            if (eMax.HasValue && eMax.Value < eMin)
            {
                error = $"Extra combo slot max for {rarity} must be ≥ min ({eMin}).";
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
            entry.ExtraComboSlots = eMin;
            entry.ExtraComboSlotsExtraChance = eCh;
            entry.ExtraComboSlotsMax = eMax;
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
