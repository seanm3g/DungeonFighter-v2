using System;
using Avalonia.Controls;
using Avalonia.Threading;
using RPGGame;
using RPGGame.UI.Avalonia.Managers;
using RPGGame.UI.Avalonia.Settings;
using RPGGame.UI.Avalonia.Settings.Helpers;
using RPGGame.UI.TextAnimation;

namespace RPGGame.UI.Avalonia.Managers.Settings.PanelHandlers
{
    public partial class TextAnimationPresetsPanelHandler
    {
        private void RequestPreviewRefresh(TextAnimationPresetsSettingsPanel panel)
        {
            RefreshPreview(panel);
        }

        private void WireSliderInteraction(TextAnimationPresetsSettingsPanel panel, Slider slider)
        {
            slider.PointerPressed += (_, _) => System.Threading.Interlocked.Increment(ref activeSliderInteractions);
            slider.PointerReleased += (_, _) =>
            {
                if (System.Threading.Interlocked.Decrement(ref activeSliderInteractions) < 0)
                    activeSliderInteractions = 0;
                if (activeSliderInteractions == 0)
                    RefreshPreview(panel);
            };
        }

        private void RefreshPreview(TextAnimationPresetsSettingsPanel panel)
        {
            try
            {
                var previewHost = panel.PreviewCharacterHostControl;
                if (previewHost == null)
                    return;

                var preset = GetSelectedPreset(panel);
                if (preset == null)
                    return;

                string presetName = GetSelectedPresetName(panel) ?? "pathIntro";
                string sample = panel.SampleTextTextBoxControl?.Text ?? panel.SampleTextTextBox?.Text ?? "Sample";
                if (string.IsNullOrWhiteSpace(sample))
                    sample = "Sample";

                string? previewTemplate = GetSelectedPreviewTemplateName(panel);
                ApplyUiToWorkingState(panel, includeAccentControls: false);
                ApplyWorkingAnimConfigToRuntime();

                TextAnimationPreviewHelper.RenderPreview(
                    previewHost,
                    preset,
                    presetName,
                    sample,
                    previewTemplate);

                UpdateAccentLayerFeedback(panel, preset);
                UpdateAccentSwatches(panel);
            }
            catch (Exception ex)
            {
                showStatusMessage?.Invoke($"Preview error: {ex.Message}", false);
                System.Diagnostics.Debug.WriteLine($"TextAnimation preview refresh failed: {ex}");
            }
        }

        private void ApplyWorkingAnimConfigToRuntime()
        {
            if (workingAnimConfig == null)
                return;

            DungeonSelectionAnimationState.Instance.ApplyConfig(workingAnimConfig);
            CritAnimationState.Instance.ApplyConfig(workingAnimConfig);
        }

        private void StartPreviewTimer(TextAnimationPresetsSettingsPanel panel)
        {
            StopPreviewTimer();
            previewTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(50) };
            previewTimer.Tick += (_, _) => RefreshPreview(panel);
            previewTimer.Start();
        }

        private void StopPreviewTimer()
        {
            if (previewTimer == null)
                return;
            previewTimer.Stop();
            previewTimer = null;
        }

        private void StopAccentFieldDebounceTimer()
        {
            if (accentFieldDebounceTimer == null)
                return;
            accentFieldDebounceTimer.Stop();
            accentFieldDebounceTimer = null;
        }

        private static void ReloadCanvasAnimationConfiguration()
        {
            try
            {
                var uiManager = UIManager.GetCustomUIManager();
                if (uiManager is CanvasUICoordinator coordinator &&
                    coordinator.GetAnimationManager() is CanvasAnimationManager canvasAnimManager)
                {
                    canvasAnimManager.ReloadAnimationConfiguration();
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"ReloadCanvasAnimationConfiguration: {ex.Message}");
            }

            DungeonSelectionAnimationState.Instance.ReloadConfiguration();
            CritAnimationState.Instance.ReloadConfiguration();
        }
    }
}
