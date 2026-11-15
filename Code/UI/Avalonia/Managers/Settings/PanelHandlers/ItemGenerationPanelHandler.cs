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
    /// Item Generation tab: combo-sequence cap fields on LootSystem. Line counts per rarity live in
    /// <c>RarityTable.json</c> and mod tiers in <c>ItemMods.json</c> (Documentation/ITEM_GENERATION_REDESIGN_2026-11-14.md).
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
            if (panel is not ItemGenerationSettingsPanel p) return;
            LoadSettings(panel);
            WireLootCapFieldsToConfiguration(p);
            WireModifierChanceFields(p);
        }

        /// <summary>Rarities shown in the "Base modifier chance by rarity" row (TextBox names are ModChance + rarity).</summary>
        private static readonly string[] ModifierChanceRarities = { "Common", "Uncommon", "Rare", "Epic", "Legendary", "Mythic" };

        private static void LoadModifierChances(ItemGenerationSettingsPanel p)
        {
            foreach (string rarity in ModifierChanceRarities)
                if (p.FindControl<TextBox>("ModChance" + rarity) is { } tb)
                    tb.Text = (RPGGame.Data.ItemGeneration.BaseModifierRoller.ChanceFor(rarity) * 100).ToString("0.##", CultureInfo.InvariantCulture);
            UpdateModifierChanceSummary(p, null);
        }

        private static void UpdateModifierChanceSummary(ItemGenerationSettingsPanel p, string? error)
        {
            if (p.FindControl<TextBlock>("ModChanceSummaryText") is not { } summary)
                return;
            summary.Text = error ?? "Each box is the % of that rarity's items whose base carries its modifier. Saved to BaseModifiers.json when a box loses focus.";
        }

        /// <summary>Reads all six boxes (0–100). False with a message when one is not a number in range.</summary>
        private static bool TryReadModifierChances(ItemGenerationSettingsPanel p, out Dictionary<string, double> chances, out string? error)
        {
            chances = new Dictionary<string, double>(StringComparer.OrdinalIgnoreCase);
            error = null;
            foreach (string rarity in ModifierChanceRarities)
            {
                if (p.FindControl<TextBox>("ModChance" + rarity) is not { } tb)
                    continue;
                string raw = (tb.Text ?? "").Trim().TrimEnd('%');
                if (!double.TryParse(raw, NumberStyles.Float, CultureInfo.InvariantCulture, out double pct) || pct < 0 || pct > 100)
                {
                    error = $"{rarity}: enter a percent from 0 to 100.";
                    return false;
                }
                chances[rarity] = pct / 100.0;
            }
            return true;
        }

        private bool SaveModifierChances(ItemGenerationSettingsPanel p)
        {
            if (!TryReadModifierChances(p, out var chances, out string? error))
            {
                UpdateModifierChanceSummary(p, error);
                showStatusMessage?.Invoke(error ?? "Invalid modifier chance.", false);
                return false;
            }
            // Unchanged values skip the file write.
            if (chances.All(kv => Math.Abs(RPGGame.Data.ItemGeneration.BaseModifierRoller.ChanceFor(kv.Key) - kv.Value) < 1e-9))
                return true;
            try
            {
                RPGGame.Data.ItemGeneration.BaseModifierRoller.SaveRarityChances(chances);
                UpdateModifierChanceSummary(p, null);
                showStatusMessage?.Invoke("Base modifier chances saved to BaseModifiers.json.", true);
                return true;
            }
            catch (Exception ex)
            {
                showStatusMessage?.Invoke($"Could not save BaseModifiers.json: {ex.Message}", false);
                return false;
            }
        }

        private void WireModifierChanceFields(ItemGenerationSettingsPanel p)
        {
            foreach (string rarity in ModifierChanceRarities)
                if (p.FindControl<TextBox>("ModChance" + rarity) is { } tb)
                    tb.LostFocus += (_, _) => SaveModifierChances(p);
        }

        public void LoadSettings(UserControl panel)
        {
            if (panel is not ItemGenerationSettingsPanel p) return;
            var loot = GameConfiguration.Instance.LootSystem;
            SetInt(p, "ComboSeqBaseMaxText", loot?.ComboSequenceBaseMax ?? 2);
            SetInt(p, "ComboSeqAbsMaxText", loot?.ComboSequenceAbsoluteMax ?? 8);
            LoadModifierChances(p);
        }

        public void SaveSettings(UserControl panel)
        {
            if (panel is not ItemGenerationSettingsPanel p) return;
            SaveModifierChances(p);

            if (!TryReadLootCapsFromUi(p, out int baseMax, out int absMax, out string? capErr))
            {
                showStatusMessage?.Invoke(capErr ?? "Invalid combo caps.", false);
                return;
            }

            var cfg = GameConfiguration.Instance;
            cfg.LootSystem ??= new LootSystemConfig();
            cfg.LootSystem.ComboSequenceBaseMax = baseMax;
            cfg.LootSystem.ComboSequenceAbsoluteMax = absMax;
        }

        /// <summary>
        /// When combo cap TextBoxes lose focus, push valid values into <see cref="GameConfiguration.Instance"/> so any later
        /// <see cref="GameConfiguration.SaveToFile"/> (e.g. Classes tab) persists the same caps the user sees.
        /// </summary>
        private void WireLootCapFieldsToConfiguration(ItemGenerationSettingsPanel p)
        {
            void sync()
            {
                if (!TryReadLootCapsFromUi(p, out int baseMax, out int absMax, out _))
                    return;
                var cfg = GameConfiguration.Instance;
                cfg.LootSystem ??= new LootSystemConfig();
                cfg.LootSystem.ComboSequenceBaseMax = baseMax;
                cfg.LootSystem.ComboSequenceAbsoluteMax = absMax;
            }

            if (p.FindControl<TextBox>("ComboSeqBaseMaxText") is { } baseTb)
                baseTb.LostFocus += (_, _) => sync();
            if (p.FindControl<TextBox>("ComboSeqAbsMaxText") is { } absTb)
                absTb.LostFocus += (_, _) => sync();
        }

        /// <summary>Reads combo base / hard cap from the panel when both are valid integers and absolute ≥ base.</summary>
        private static bool TryReadLootCapsFromUi(
            ItemGenerationSettingsPanel p,
            out int baseMax,
            out int absMax,
            out string? error)
        {
            baseMax = 0;
            absMax = 0;
            error = null;
            if (!TryParseInt(p, "ComboSeqBaseMaxText", 1, 99, out baseMax, out error))
                return false;
            if (!TryParseInt(p, "ComboSeqAbsMaxText", 1, 99, out absMax, out error))
                return false;
            if (absMax < baseMax)
            {
                error = "Combo absolute max must be ≥ base max.";
                return false;
            }

            return true;
        }

        private static void SetInt(ItemGenerationSettingsPanel p, string controlName, int value)
        {
            var tb = p.FindControl<TextBox>(controlName);
            if (tb != null)
                tb.Text = value.ToString(CultureInfo.InvariantCulture);
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
    }
}
