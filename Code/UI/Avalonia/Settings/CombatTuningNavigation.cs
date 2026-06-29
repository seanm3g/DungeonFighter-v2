using System;
using Avalonia.Controls;
using Avalonia.Threading;
using RPGGame.UI.Avalonia.Settings.ViewModels;

namespace RPGGame.UI.Avalonia.Settings
{
    /// <summary>Deep-link requests into Combat Tuning sub-tabs (e.g. from Balance Workbench).</summary>
    public static class CombatTuningNavigation
    {
        public enum CombatTuningSubTab
        {
            ParametersCore,
            ProgressionCurve
        }

        public static CombatTuningSubTab? PendingSubTab { get; private set; }

        public static void RequestOpen(CombatTuningSubTab subTab) => PendingSubTab = subTab;

        /// <summary>Set by MainWindow — opens settings overlay and navigates to Combat Tuning → Progression Curve.</summary>
        public static System.Action? OpenSettingsAndNavigateToProgressionCurve { get; set; }

        public static void RequestOpenProgressionCurveInSettings()
        {
            PendingSubTab = CombatTuningSubTab.ProgressionCurve;
            OpenSettingsAndNavigateToProgressionCurve?.Invoke();
        }

        public static void ApplyToPanel(CombatTuningSettingsPanel? panel)
        {
            if (panel == null || PendingSubTab == null)
                return;

            var target = PendingSubTab.Value;
            PendingSubTab = null;
            Dispatcher.UIThread.Post(() => panel.SelectSubTab(target));
        }
    }
}
