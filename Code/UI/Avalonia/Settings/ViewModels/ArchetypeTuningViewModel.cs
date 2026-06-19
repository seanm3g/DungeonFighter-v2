using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;

namespace RPGGame.UI.Avalonia.Settings.ViewModels
{
    /// <summary>Selector + filtered sliders for per-archetype stat multipliers.</summary>
    public sealed class ArchetypeTuningViewModel : INotifyPropertyChanged
    {
        public ArchetypeTuningViewModel(IReadOnlyList<CombatTuningParameterViewModel> allArchetypeParameters)
        {
            ArchetypeNames = new ObservableCollection<string>(
                allArchetypeParameters
                    .Select(p => p.FilterKey)
                    .Where(k => !string.IsNullOrEmpty(k))
                    .Distinct()
                    .OrderBy(k => k)!);

            allParameters = allArchetypeParameters;
            SelectedParameters = new ObservableCollection<CombatTuningParameterViewModel>();

            if (ArchetypeNames.Count > 0)
                SelectedArchetype = ArchetypeNames[0];
        }

        private readonly IReadOnlyList<CombatTuningParameterViewModel> allParameters;

        public ObservableCollection<string> ArchetypeNames { get; }
        public ObservableCollection<CombatTuningParameterViewModel> SelectedParameters { get; }

        private string? selectedArchetype;
        public string? SelectedArchetype
        {
            get => selectedArchetype;
            set
            {
                if (selectedArchetype == value) return;
                selectedArchetype = value;
                OnPropertyChanged();
                RefreshSelectedParameters();
            }
        }

        private void RefreshSelectedParameters()
        {
            SelectedParameters.Clear();
            if (string.IsNullOrEmpty(selectedArchetype)) return;
            foreach (var p in allParameters.Where(p =>
                         string.Equals(p.FilterKey, selectedArchetype, StringComparison.OrdinalIgnoreCase)))
                SelectedParameters.Add(p);
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        private void OnPropertyChanged([CallerMemberName] string? name = null) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}
