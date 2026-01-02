using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace RPGGame.UI.Avalonia.Settings
{
    public partial class ItemEditDialog : Window
    {
        public ItemEditViewModel ViewModel { get; }

        // Parameterless constructor required by Avalonia XAML loader
        public ItemEditDialog()
        {
            InitializeComponent();
            ViewModel = new ItemEditViewModel();
            DataContext = ViewModel;
        }

        public ItemEditDialog(ItemEditViewModel viewModel)
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
                System.Diagnostics.Debug.WriteLine($"Validation error: {errorMessage}");
            }
        }

        private void OnCancelClick(object? sender, RoutedEventArgs e)
        {
            Close(false);
        }
    }

    public class ItemEditViewModel : INotifyPropertyChanged
    {
        private string _name = "";
        private string _type = "";
        private string _slot = "";
        private int _tier = 1;
        private int _baseDamage = 0;
        private double _attackSpeed = 0.0;
        private int _armor = 0;
        private bool _isWeapon = true;

        public string DialogTitle => IsWeapon ? "Edit Weapon" : "Edit Armor";

        public string Name
        {
            get => _name;
            set { _name = value; OnPropertyChanged(); }
        }

        public string Type
        {
            get => _type;
            set { _type = value; OnPropertyChanged(); }
        }

        public string Slot
        {
            get => _slot;
            set { _slot = value; OnPropertyChanged(); }
        }

        public int Tier
        {
            get => _tier;
            set { _tier = value; OnPropertyChanged(); }
        }

        public int BaseDamage
        {
            get => _baseDamage;
            set { _baseDamage = value; OnPropertyChanged(); }
        }

        public double AttackSpeed
        {
            get => _attackSpeed;
            set { _attackSpeed = value; OnPropertyChanged(); }
        }

        public int Armor
        {
            get => _armor;
            set { _armor = value; OnPropertyChanged(); }
        }

        public bool IsWeapon
        {
            get => _isWeapon;
            set 
            { 
                _isWeapon = value; 
                OnPropertyChanged(); 
                OnPropertyChanged(nameof(DialogTitle));
                OnPropertyChanged(nameof(ShowWeaponFields));
                OnPropertyChanged(nameof(ShowArmorFields));
            }
        }

        public bool ShowWeaponFields => IsWeapon;
        public bool ShowArmorFields => !IsWeapon;

        public ObservableCollection<string> AvailableWeaponTypes { get; set; } = new ObservableCollection<string>();
        public ObservableCollection<string> AvailableSlots { get; set; } = new ObservableCollection<string>();
        public ObservableCollection<int> AvailableTiers { get; set; } = new ObservableCollection<int>();

        public event PropertyChangedEventHandler? PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public string? GetValidationError()
        {
            if (string.IsNullOrWhiteSpace(Name))
            {
                return "Name is required";
            }

            if (IsWeapon)
            {
                if (string.IsNullOrWhiteSpace(Type))
                {
                    return "Weapon type is required";
                }
                if (BaseDamage < 0)
                {
                    return "Base damage must be non-negative";
                }
                if (AttackSpeed < 0)
                {
                    return "Attack speed must be non-negative";
                }
            }
            else
            {
                if (string.IsNullOrWhiteSpace(Slot))
                {
                    return "Armor slot is required";
                }
                if (Armor < 0)
                {
                    return "Armor value must be non-negative";
                }
            }

            if (Tier < 1 || Tier > 5)
            {
                return "Tier must be between 1 and 5";
            }

            return null;
        }

        public bool Validate()
        {
            return GetValidationError() == null;
        }
    }
}
