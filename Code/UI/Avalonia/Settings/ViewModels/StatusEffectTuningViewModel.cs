using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;

namespace RPGGame.UI.Avalonia.Settings.ViewModels
{
    /// <summary>Selector + filtered sliders for per-status-effect tuning.</summary>
    public sealed class StatusEffectTuningViewModel : INotifyPropertyChanged
    {
        public StatusEffectTuningViewModel(
            IReadOnlyList<CombatTuningParameterViewModel> globalParameters,
            IReadOnlyList<CombatTuningParameterViewModel> perEffectParameters)
        {
            GlobalParameters = new ObservableCollection<CombatTuningParameterViewModel>(globalParameters);
            EffectNames = new ObservableCollection<string>(
                perEffectParameters
                    .Select(p => p.FilterKey)
                    .Where(k => !string.IsNullOrEmpty(k))
                    .Distinct()
                    .OrderBy(k => k)!);

            allEffectParameters = perEffectParameters;
            SelectedEffectParameters = new ObservableCollection<CombatTuningParameterViewModel>();

            if (EffectNames.Count > 0)
                SelectedEffect = EffectNames[0];
        }

        private readonly IReadOnlyList<CombatTuningParameterViewModel> allEffectParameters;

        public ObservableCollection<CombatTuningParameterViewModel> GlobalParameters { get; }
        public ObservableCollection<string> EffectNames { get; }
        public ObservableCollection<CombatTuningParameterViewModel> SelectedEffectParameters { get; }

        private string? selectedEffect;
        public string? SelectedEffect
        {
            get => selectedEffect;
            set
            {
                if (selectedEffect == value) return;
                selectedEffect = value;
                OnPropertyChanged();
                RefreshSelectedEffectParameters();
            }
        }

        private void RefreshSelectedEffectParameters()
        {
            SelectedEffectParameters.Clear();
            if (string.IsNullOrEmpty(selectedEffect)) return;
            foreach (var p in allEffectParameters.Where(p =>
                         string.Equals(p.FilterKey, selectedEffect, StringComparison.OrdinalIgnoreCase)))
                SelectedEffectParameters.Add(p);
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        private void OnPropertyChanged([CallerMemberName] string? name = null) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}
