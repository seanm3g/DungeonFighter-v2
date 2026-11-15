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
        }

        public void LoadSettings(UserControl panel)
        {
            if (panel is not ItemGenerationSettingsPanel p) return;
            var loot = GameConfiguration.Instance.LootSystem;
            SetInt(p, "ComboSeqBaseMaxText", loot?.ComboSequenceBaseMax ?? 2);
            SetInt(p, "ComboSeqAbsMaxText", loot?.ComboSequenceAbsoluteMax ?? 8);
        }

        public void SaveSettings(UserControl panel)
        {
            if (panel is not ItemGenerationSettingsPanel p) return;
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
