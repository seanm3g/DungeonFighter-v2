using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace RPGGame.UI.Avalonia.Settings
{
    public partial class ItemSuffixesSettingsPanel : UserControl
    {
        public event EventHandler<SuffixModifierViewModel?>? SuffixSelected;

        private SuffixModifierViewModel? selected;

        public ItemSuffixesSettingsPanel()
        {
            InitializeComponent();
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }

        public void OnSuffixPointerPressed(object? sender, PointerPressedEventArgs e)
        {
            if (sender is Border border && border.DataContext is SuffixModifierViewModel vm)
                SetSelectedSuffix(vm);
        }

        public void SetSelectedSuffix(SuffixModifierViewModel? vm)
        {
            if (selected != null)
                selected.IsSelected = false;
            selected = vm;
            if (selected != null)
                selected.IsSelected = true;
            SuffixSelected?.Invoke(this, selected);
        }

        public void SetButtonStates(bool canEdit, bool canDelete)
        {
            var editButton = this.FindControl<Button>("EditSuffixButton");
            var deleteButton = this.FindControl<Button>("DeleteSuffixButton");
            if (editButton != null) editButton.IsEnabled = canEdit;
            if (deleteButton != null) deleteButton.IsEnabled = canDelete;
        }
    }

    public class SuffixRarityGroupViewModel : INotifyPropertyChanged
    {
        private string _rarityName = "";
        public string RarityName
        {
            get => _rarityName;
            set { _rarityName = value; OnPropertyChanged(); }
        }

        public ObservableCollection<SuffixModifierViewModel> Modifiers { get; set; } = new ObservableCollection<SuffixModifierViewModel>();

        public event PropertyChangedEventHandler? PropertyChanged;
        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }

    public class SuffixModifierViewModel : INotifyPropertyChanged
    {
        private string _name = "";
        private string _description = "";
        private string _currentRarity = "";
        private string _selectedRarity = "";
        private string _itemRank = "";
        private string _statType = "";
        private double _value;
        private string _mechanicsText = "";
        private bool _isSelected;

        public int EntryId { get; set; }

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

        public string ItemRank
        {
            get => _itemRank;
            set { _itemRank = value; OnPropertyChanged(); }
        }

        public string StatType
        {
            get => _statType;
            set { _statType = value; OnPropertyChanged(); }
        }

        public double Value
        {
            get => _value;
            set { _value = value; OnPropertyChanged(); }
        }

        /// <summary>Multiline <c>StatType: value</c> lines or a single bracket cell <c>[A:1, B:2]</c>.</summary>
        public string MechanicsText
        {
            get => _mechanicsText;
            set { _mechanicsText = value; OnPropertyChanged(); }
        }

        public bool IsSelected
        {
            get => _isSelected;
            set { _isSelected = value; OnPropertyChanged(); OnPropertyChanged(nameof(BackgroundColor)); }
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
