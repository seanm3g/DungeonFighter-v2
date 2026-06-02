using System;
using Avalonia.Controls;
using RPGGame;
using RPGGame.UI.Avalonia.Components;
using RPGGame.UI.Avalonia.Settings;

namespace RPGGame.UI.Avalonia.Managers.Settings.PanelHandlers
{
    /// <summary>
    /// Loads/saves enemy progression scales and spawn tier weights from the Enemy tuning settings tab.
    /// </summary>
    public sealed class EnemyTuningPanelHandler : ISettingsPanelHandler
    {
        private readonly Action<string, bool>? showStatusMessage;

        public EnemyTuningPanelHandler(Action<string, bool>? showStatusMessage = null)
        {
            this.showStatusMessage = showStatusMessage;
        }

        public string PanelType => "EnemyTuning";

        public void WireUp(UserControl panel)
        {
            if (panel is not EnemyTuningSettingsPanel p) return;

            void hook(SliderWithTextBox? c)
            {
                if (c?.Slider != null)
                    c.Slider.ValueChanged += (_, _) => PushFromPanelToConfig(p);
            }

            hook(ResolveSlider(p, p.EnemyBaseHealthScaleControl, "EnemyBaseHealthScaleControl"));
            hook(ResolveSlider(p, p.EnemyHealthGrowthScaleControl, "EnemyHealthGrowthScaleControl"));
            hook(ResolveSlider(p, p.EnemyAttributeGrowthScaleControl, "EnemyAttributeGrowthScaleControl"));

            var baseHealth = ResolveSlider(p, p.EnemyBaseHealthScaleControl, "EnemyBaseHealthScaleControl");
            var healthGrowth = ResolveSlider(p, p.EnemyHealthGrowthScaleControl, "EnemyHealthGrowthScaleControl");
            var attributeGrowth = ResolveSlider(p, p.EnemyAttributeGrowthScaleControl, "EnemyAttributeGrowthScaleControl");

            if (baseHealth?.TextBox != null)
                baseHealth.TextBox.LostFocus += (_, _) => PushFromPanelToConfig(p);
            if (healthGrowth?.TextBox != null)
                healthGrowth.TextBox.LostFocus += (_, _) => PushFromPanelToConfig(p);
            if (attributeGrowth?.TextBox != null)
                attributeGrowth.TextBox.LostFocus += (_, _) => PushFromPanelToConfig(p);

            foreach (var box in SpawnWeightFields.AllTextBoxes(p))
            {
                if (box == null) continue;
                box.TextChanged += (_, _) =>
                {
                    PushFromPanelToConfig(p);
                    UpdateTotalLabels(p);
                };
                box.LostFocus += (_, _) =>
                {
                    PushFromPanelToConfig(p);
                    UpdateTotalLabels(p);
                };
            }

            LoadSettings(p);
        }

        public void LoadSettings(UserControl panel)
        {
            if (panel is not EnemyTuningSettingsPanel p) return;

            GameConfiguration.Instance.EnemySystem?.EnsureSanitizedDefaults();
            var sys = GameConfiguration.Instance.EnemySystem;
            var s = sys?.ProgressionScales ?? new EnemyProgressionScalesConfig();

            SetSlider(ResolveSlider(p, p.EnemyBaseHealthScaleControl, "EnemyBaseHealthScaleControl"), s.BaseHealthScale);
            SetSlider(ResolveSlider(p, p.EnemyHealthGrowthScaleControl, "EnemyHealthGrowthScaleControl"), s.HealthGrowthScale);
            SetSlider(ResolveSlider(p, p.EnemyAttributeGrowthScaleControl, "EnemyAttributeGrowthScaleControl"), s.AttributeGrowthScale);

            var bySettlement = sys?.SpawnTierWeightsBySettlement ?? new EnemySpawnTierWeightsBySettlementConfig();
            SpawnWeightFields.WriteToPanel(p, SettlementType.Rural, bySettlement.Rural);
            SpawnWeightFields.WriteToPanel(p, SettlementType.Town, bySettlement.Town);
            SpawnWeightFields.WriteToPanel(p, SettlementType.City, bySettlement.City);
            UpdateTotalLabels(p);
        }

        public void SaveSettings(UserControl panel)
        {
            if (panel is not EnemyTuningSettingsPanel p) return;
            try
            {
                PushFromPanelToConfig(p);
                GameConfiguration.Instance.EnemySystem?.EnsureSanitizedDefaults();
                LoadSettings(p);
                showStatusMessage?.Invoke("Enemy tuning updated (save settings to write balance patch).", true);
            }
            catch (Exception ex)
            {
                showStatusMessage?.Invoke($"Enemy tuning: {ex.Message}", false);
            }
        }

        private static void SetSlider(SliderWithTextBox? control, double value)
        {
            if (control == null) return;
            double v = Math.Clamp(value, control.Minimum, control.Maximum);
            control.Value = v;
        }

        private static void PushFromPanelToConfig(EnemyTuningSettingsPanel p)
        {
            var sys = GameConfiguration.Instance.EnemySystem ??= new EnemySystemConfig();
            sys.ProgressionScales ??= new EnemyProgressionScalesConfig();

            var baseHealth = ResolveSlider(p, p.EnemyBaseHealthScaleControl, "EnemyBaseHealthScaleControl");
            var healthGrowth = ResolveSlider(p, p.EnemyHealthGrowthScaleControl, "EnemyHealthGrowthScaleControl");
            var attributeGrowth = ResolveSlider(p, p.EnemyAttributeGrowthScaleControl, "EnemyAttributeGrowthScaleControl");

            if (baseHealth != null)
                sys.ProgressionScales.BaseHealthScale = Math.Clamp(baseHealth.Value, baseHealth.Minimum, baseHealth.Maximum);
            if (healthGrowth != null)
                sys.ProgressionScales.HealthGrowthScale = Math.Clamp(healthGrowth.Value, healthGrowth.Minimum, healthGrowth.Maximum);
            if (attributeGrowth != null)
                sys.ProgressionScales.AttributeGrowthScale = Math.Clamp(attributeGrowth.Value, attributeGrowth.Minimum, attributeGrowth.Maximum);

            sys.SpawnTierWeightsBySettlement ??= new EnemySpawnTierWeightsBySettlementConfig();
            SpawnWeightFields.ReadFromPanel(p, SettlementType.Rural, sys.SpawnTierWeightsBySettlement.Rural);
            SpawnWeightFields.ReadFromPanel(p, SettlementType.Town, sys.SpawnTierWeightsBySettlement.Town);
            SpawnWeightFields.ReadFromPanel(p, SettlementType.City, sys.SpawnTierWeightsBySettlement.City);

            sys.EnsureSanitizedDefaults();
            SpawnWeightFields.WriteToPanel(p, SettlementType.Rural, sys.SpawnTierWeightsBySettlement.Rural);
            SpawnWeightFields.WriteToPanel(p, SettlementType.Town, sys.SpawnTierWeightsBySettlement.Town);
            SpawnWeightFields.WriteToPanel(p, SettlementType.City, sys.SpawnTierWeightsBySettlement.City);
            UpdateTotalLabels(p);
        }

        private static void UpdateTotalLabels(EnemyTuningSettingsPanel p)
        {
            UpdateTotalLabel(ResolveLabel(p, p.RuralTotalLabel, "RuralTotalLabel"), SpawnWeightFields.ReadRawTotal(p, SettlementType.Rural));
            UpdateTotalLabel(ResolveLabel(p, p.TownTotalLabel, "TownTotalLabel"), SpawnWeightFields.ReadRawTotal(p, SettlementType.Town));
            UpdateTotalLabel(ResolveLabel(p, p.CityTotalLabel, "CityTotalLabel"), SpawnWeightFields.ReadRawTotal(p, SettlementType.City));
        }

        private static SliderWithTextBox? ResolveSlider(EnemyTuningSettingsPanel p, SliderWithTextBox? named, string name) =>
            named ?? p.FindControl<SliderWithTextBox>(name);

        private static TextBlock? ResolveLabel(EnemyTuningSettingsPanel p, TextBlock? named, string name) =>
            named ?? p.FindControl<TextBlock>(name);

        private static void UpdateTotalLabel(TextBlock? label, int total)
        {
            if (label == null) return;
            label.Text = total == 100
                ? "Total: 100%"
                : $"Total: {total}% (will normalize on save)";
        }

        private static class SpawnWeightFields
        {
            public static TextBox?[] AllTextBoxes(EnemyTuningSettingsPanel p) => new TextBox?[]
            {
                GetField(p, SettlementType.Rural, SpawnWeightField.Common),
                GetField(p, SettlementType.Rural, SpawnWeightField.UncommonBiome),
                GetField(p, SettlementType.Rural, SpawnWeightField.UncommonRegion),
                GetField(p, SettlementType.Rural, SpawnWeightField.UncommonLocation),
                GetField(p, SettlementType.Rural, SpawnWeightField.RareLocation),
                GetField(p, SettlementType.Rural, SpawnWeightField.Anywhere),
                GetField(p, SettlementType.Town, SpawnWeightField.Common),
                GetField(p, SettlementType.Town, SpawnWeightField.UncommonBiome),
                GetField(p, SettlementType.Town, SpawnWeightField.UncommonRegion),
                GetField(p, SettlementType.Town, SpawnWeightField.UncommonLocation),
                GetField(p, SettlementType.Town, SpawnWeightField.RareLocation),
                GetField(p, SettlementType.Town, SpawnWeightField.Anywhere),
                GetField(p, SettlementType.City, SpawnWeightField.Common),
                GetField(p, SettlementType.City, SpawnWeightField.UncommonBiome),
                GetField(p, SettlementType.City, SpawnWeightField.UncommonRegion),
                GetField(p, SettlementType.City, SpawnWeightField.UncommonLocation),
                GetField(p, SettlementType.City, SpawnWeightField.RareLocation),
                GetField(p, SettlementType.City, SpawnWeightField.Anywhere)
            };

            public static void WriteToPanel(EnemyTuningSettingsPanel p, SettlementType type, EnemySpawnTierWeightsConfig weights)
            {
                SetFieldText(GetField(p, type, SpawnWeightField.Common), weights.CommonPercent);
                SetFieldText(GetField(p, type, SpawnWeightField.UncommonBiome), weights.UncommonBiomePercent);
                SetFieldText(GetField(p, type, SpawnWeightField.UncommonRegion), weights.UncommonRegionPercent);
                SetFieldText(GetField(p, type, SpawnWeightField.UncommonLocation), weights.UncommonLocationPercent);
                SetFieldText(GetField(p, type, SpawnWeightField.RareLocation), weights.RareLocationPercent);
                SetFieldText(GetField(p, type, SpawnWeightField.Anywhere), weights.AnywherePercent);
            }

            public static void ReadFromPanel(EnemyTuningSettingsPanel p, SettlementType type, EnemySpawnTierWeightsConfig weights)
            {
                weights.CommonPercent = ParsePercent(GetField(p, type, SpawnWeightField.Common)?.Text);
                weights.UncommonBiomePercent = ParsePercent(GetField(p, type, SpawnWeightField.UncommonBiome)?.Text);
                weights.UncommonRegionPercent = ParsePercent(GetField(p, type, SpawnWeightField.UncommonRegion)?.Text);
                weights.UncommonLocationPercent = ParsePercent(GetField(p, type, SpawnWeightField.UncommonLocation)?.Text);
                weights.RareLocationPercent = ParsePercent(GetField(p, type, SpawnWeightField.RareLocation)?.Text);
                weights.AnywherePercent = ParsePercent(GetField(p, type, SpawnWeightField.Anywhere)?.Text);
            }

            public static int ReadRawTotal(EnemyTuningSettingsPanel p, SettlementType type)
            {
                return ParsePercent(GetField(p, type, SpawnWeightField.Common)?.Text)
                       + ParsePercent(GetField(p, type, SpawnWeightField.UncommonBiome)?.Text)
                       + ParsePercent(GetField(p, type, SpawnWeightField.UncommonRegion)?.Text)
                       + ParsePercent(GetField(p, type, SpawnWeightField.UncommonLocation)?.Text)
                       + ParsePercent(GetField(p, type, SpawnWeightField.RareLocation)?.Text)
                       + ParsePercent(GetField(p, type, SpawnWeightField.Anywhere)?.Text);
            }

            private static void SetFieldText(TextBox? field, int value)
            {
                if (field == null) return;
                field.Text = value.ToString();
            }

            private static int ParsePercent(string? text) =>
                int.TryParse(text?.Trim(), out int value) ? Math.Clamp(value, 0, 100) : 0;

            private static TextBox? GetField(EnemyTuningSettingsPanel p, SettlementType type, SpawnWeightField field) =>
                p.FindControl<TextBox>(ResolveName(type, field));

            private static string ResolveName(SettlementType type, SpawnWeightField field) =>
                (type, field) switch
                {
                    (SettlementType.Rural, SpawnWeightField.Common) => "RuralCommonPercent",
                    (SettlementType.Rural, SpawnWeightField.UncommonBiome) => "RuralUncommonBiomePercent",
                    (SettlementType.Rural, SpawnWeightField.UncommonRegion) => "RuralUncommonRegionPercent",
                    (SettlementType.Rural, SpawnWeightField.UncommonLocation) => "RuralUncommonLocationPercent",
                    (SettlementType.Rural, SpawnWeightField.RareLocation) => "RuralRareLocationPercent",
                    (SettlementType.Rural, SpawnWeightField.Anywhere) => "RuralAnywherePercent",
                    (SettlementType.Town, SpawnWeightField.Common) => "TownCommonPercent",
                    (SettlementType.Town, SpawnWeightField.UncommonBiome) => "TownUncommonBiomePercent",
                    (SettlementType.Town, SpawnWeightField.UncommonRegion) => "TownUncommonRegionPercent",
                    (SettlementType.Town, SpawnWeightField.UncommonLocation) => "TownUncommonLocationPercent",
                    (SettlementType.Town, SpawnWeightField.RareLocation) => "TownRareLocationPercent",
                    (SettlementType.Town, SpawnWeightField.Anywhere) => "TownAnywherePercent",
                    (SettlementType.City, SpawnWeightField.Common) => "CityCommonPercent",
                    (SettlementType.City, SpawnWeightField.UncommonBiome) => "CityUncommonBiomePercent",
                    (SettlementType.City, SpawnWeightField.UncommonRegion) => "CityUncommonRegionPercent",
                    (SettlementType.City, SpawnWeightField.UncommonLocation) => "CityUncommonLocationPercent",
                    (SettlementType.City, SpawnWeightField.RareLocation) => "CityRareLocationPercent",
                    _ => "CityAnywherePercent"
                };

            private enum SpawnWeightField
            {
                Common,
                UncommonBiome,
                UncommonRegion,
                UncommonLocation,
                RareLocation,
                Anywhere
            }
        }
    }
}
