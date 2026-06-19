using System.Collections.ObjectModel;
using System.Linq;

namespace RPGGame.UI.Avalonia.Settings.ViewModels
{
    /// <summary>Grouped parameters for expander sections within a tuning tab.</summary>
    public sealed class CombatTuningSubGroupViewModel
    {
        public CombatTuningSubGroupViewModel(string name, ObservableCollection<CombatTuningParameterViewModel> parameters)
        {
            Name = name;
            Parameters = parameters;
        }

        public string Name { get; }
        public ObservableCollection<CombatTuningParameterViewModel> Parameters { get; }

        public string DisplayName =>
            Parameters.Count > 0 && Parameters.All(p => !p.IsImplemented)
                ? $"{Name} (unimplemented)"
                : Name;
    }
}