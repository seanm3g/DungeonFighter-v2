using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace RPGGame.UI.Avalonia.Settings
{
    public partial class ItemModifierEditDialog : Window
    {
        public ItemModifierEditViewModel ViewModel { get; }

        // Parameterless constructor required by Avalonia XAML loader
        public ItemModifierEditDialog()
        {
            InitializeComponent();
            ViewModel = new ItemModifierEditViewModel();
            DataContext = ViewModel;
        }

        public ItemModifierEditDialog(ItemModifierEditViewModel viewModel)
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
            string? errorMessage = ViewModel.GetValidationError();
            if (errorMessage == null)
            {
                Close(true);
            }
            else
            {
                // Show error message - could use a message box or status label
                // For now, we'll just not close the dialog
                // In a production app, you'd show this to the user
                System.Diagnostics.Debug.WriteLine($"Validation error: {errorMessage}");
            }
        }

        private void OnCancelClick(object? sender, RoutedEventArgs e)
        {
            Close(false);
        }
    }

    public class ItemModifierEditViewModel : INotifyPropertyChanged
    {
        private int _diceResult;
        private string _name = "";
        private string _description = "";
        private string _effect = "";
        private string _itemRank = "Common";
        private double _minValue;
        private double _maxValue;
        private bool _isEditMode;

        public string DialogTitle => IsEditMode ? "Edit Item Modifier" : "Add Item Modifier";

        public int DiceResult
        {
            get => _diceResult;
            set { _diceResult = value; OnPropertyChanged(); }
        }

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

        public string Effect
        {
            get => _effect;
            set { _effect = value; OnPropertyChanged(); }
        }

        public string ItemRank
        {
            get => _itemRank;
            set { _itemRank = value; OnPropertyChanged(); }
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

        public bool IsEditMode
        {
            get => _isEditMode;
            set { _isEditMode = value; OnPropertyChanged(nameof(DialogTitle)); }
        }

        public ObservableCollection<string> AvailableRarities { get; set; } = new ObservableCollection<string>();

        public event PropertyChangedEventHandler? PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public bool Validate()
        {
            return GetValidationError() == null;
        }

        public string? GetValidationError()
        {
            if (string.IsNullOrWhiteSpace(Name))
            {
                return "Name is required";
            }

            if (DiceResult <= 0)
            {
                return "Dice Result must be greater than 0";
            }

            if (MaxValue < MinValue)
            {
                return "Max Value must be greater than or equal to Min Value";
            }

            return null;
        }
    }
}
