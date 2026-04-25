using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace RPGGame.UI.Avalonia.Settings
{
    public partial class EnemiesSettingsPanel : UserControl
    {
        public EnemiesSettingsPanel()
        {
            InitializeComponent();
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }
    }

    public class EnemySettingsRowViewModel : INotifyPropertyChanged
    {
        private string _tags;

        public EnemySettingsRowViewModel(int sourceIndex, string name, string archetype, string tagsCommaSeparated)
        {
            SourceIndex = sourceIndex;
            Name = name;
            Archetype = archetype;
            _tags = tagsCommaSeparated;
        }

        public int SourceIndex { get; }

        public string Name { get; }

        public string Archetype { get; }

        public string Tags
        {
            get => _tags;
            set
            {
                if (_tags == value)
                    return;
                _tags = value;
                OnPropertyChanged();
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
