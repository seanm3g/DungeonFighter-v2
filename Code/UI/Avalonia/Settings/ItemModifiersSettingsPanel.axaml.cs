using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace RPGGame.UI.Avalonia.Settings
{
    public partial class ItemModifiersSettingsPanel : UserControl
    {
        public event EventHandler<ModifierViewModel?>? ModifierSelected;

        private ModifierViewModel? selectedModifier;

        public ItemModifiersSettingsPanel()
        {
            InitializeComponent();
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }

        public void OnModifierPointerPressed(object? sender, PointerPressedEventArgs e)
        {
            if (sender is Border border && border.DataContext is ModifierViewModel modifier)
            {
                SetSelectedModifier(modifier);
            }
        }

        public void SetSelectedModifier(ModifierViewModel? modifier)
        {
            // Deselect previous
            if (selectedModifier != null)
            {
                selectedModifier.IsSelected = false;
            }

            // Select new
            selectedModifier = modifier;
            if (selectedModifier != null)
            {
                selectedModifier.IsSelected = true;
            }

            ModifierSelected?.Invoke(this, selectedModifier);
        }

        public void SetButtonStates(bool canEdit, bool canDelete)
        {
            var editButton = this.FindControl<Button>("EditModifierButton");
            var deleteButton = this.FindControl<Button>("DeleteModifierButton");
            
            if (editButton != null) editButton.IsEnabled = canEdit;
            if (deleteButton != null) deleteButton.IsEnabled = canDelete;
        }
    }

    /// <summary>
    /// View model for a rarity group containing modifiers
    /// </summary>
    public class RarityGroupViewModel : INotifyPropertyChanged
    {
        private string _rarityName = "";
        public string RarityName
        {
            get => _rarityName;
            set { _rarityName = value; OnPropertyChanged(); }
        }

        public ObservableCollection<ModifierViewModel> Modifiers { get; set; } = new ObservableCollection<ModifierViewModel>();

        public event PropertyChangedEventHandler? PropertyChanged;
        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }

    /// <summary>
    /// View model for a single modifier
    /// </summary>
    public class ModifierViewModel : INotifyPropertyChanged
    {
        private string _name = "";
        private string _description = "";
        private string _currentRarity = "";
        private string _selectedRarity = "";
        private int _diceResult = 0;
        private bool _isSelected = false;
        private string _effect = "";
        private double _minValue = 0;
        private double _maxValue = 0;

        public string Name
        {
            get => _name;
            set { _name = value; OnPropertyChanged(); }
        }

        public string Description
        {
            get => _description;
            set { _description = value; OnPropertyChanged(); }
        }

        public string CurrentRarity
        {
            get => _currentRarity;
            set { _currentRarity = value; SelectedRarity = value; OnPropertyChanged(); }
        }

        public string SelectedRarity
        {
            get => _selectedRarity;
            set 
            { 
                if (_selectedRarity != value)
                {
                    _selectedRarity = value; 
                    OnPropertyChanged();
                }
            }
        }

        public int DiceResult
        {
            get => _diceResult;
            set { _diceResult = value; OnPropertyChanged(); }
        }

        public bool IsSelected
        {
            get => _isSelected;
            set { _isSelected = value; OnPropertyChanged(); OnPropertyChanged(nameof(BackgroundColor)); }
        }

        public string Effect
        {
            get => _effect;
            set { _effect = value; OnPropertyChanged(); }
        }

        public double MinValue
        {
            get => _minValue;
            set { _minValue = value; OnPropertyChanged(); }
        }

        public double MaxValue
        {
            get => _maxValue;
            set { _maxValue = value; OnPropertyChanged(); }
        }

        public string BackgroundColor => IsSelected ? "#FFE3F2FD" : "White";

        public ObservableCollection<string> AvailableRarities { get; set; } = new ObservableCollection<string>();

        public event PropertyChangedEventHandler? PropertyChanged;
        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}

