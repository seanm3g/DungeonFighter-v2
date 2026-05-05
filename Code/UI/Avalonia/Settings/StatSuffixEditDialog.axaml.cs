using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Globalization;
using System.Runtime.CompilerServices;

namespace RPGGame.UI.Avalonia.Settings
{
    public partial class StatSuffixEditDialog : Window
    {
        public StatSuffixEditViewModel ViewModel { get; }

        public StatSuffixEditDialog()
        {
            InitializeComponent();
            ViewModel = new StatSuffixEditViewModel();
            DataContext = ViewModel;
        }

        public StatSuffixEditDialog(StatSuffixEditViewModel viewModel)
        {
            InitializeComponent();
            DataContext = ViewModel = viewModel;
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }

        private void OnSaveClick(object? sender, RoutedEventArgs e)
        {
            if (ViewModel.GetValidationError() == null)
                Close(true);
        }

        private void OnCancelClick(object? sender, RoutedEventArgs e)
        {
            Close(false);
        }
    }

    public class StatSuffixEditViewModel : INotifyPropertyChanged
    {
        private string _name = "";
        private string _description = "";
        private string _rarity = "Common";
        private string _itemRank = "";
        private string _statType = "";
        private string _valueText = "0";
        private string _mechanicsText = "";
        private bool _isEditMode;

        public string DialogTitle => IsEditMode ? "Edit suffix" : "Add suffix";

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

        public string Rarity
        {
            get => _rarity;
            set { _rarity = value; OnPropertyChanged(); }
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

        public string Value
        {
            get => _valueText;
            set { _valueText = value; OnPropertyChanged(); }
        }

        public string MechanicsText
        {
            get => _mechanicsText;
            set { _mechanicsText = value; OnPropertyChanged(); }
        }

        public bool IsEditMode
        {
            get => _isEditMode;
            set { _isEditMode = value; OnPropertyChanged(nameof(DialogTitle)); }
        }

        public ObservableCollection<string> AvailableRarities { get; } = new ObservableCollection<string>();

        public event PropertyChangedEventHandler? PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public string? GetValidationError()
        {
            if (string.IsNullOrWhiteSpace(Name))
                return "Name is required";
            if (!double.TryParse(Value.Trim(), NumberStyles.Float, CultureInfo.InvariantCulture, out _))
                return "Value must be a number (legacy field)";
            return null;
        }

        public double GetLegacyValueDouble()
        {
            double.TryParse(Value.Trim(), NumberStyles.Float, CultureInfo.InvariantCulture, out double v);
            return v;
        }
    }
}
