using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Markup.Xaml;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace RPGGame.UI.Avalonia.Settings
{
    public partial class ItemsSettingsPanel : UserControl
    {
        public event EventHandler<ItemViewModel?>? ItemSelected;

        private ItemViewModel? selectedItem;

        public ItemsSettingsPanel()
        {
            InitializeComponent();
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }

        public void OnItemPointerPressed(object? sender, PointerPressedEventArgs e)
        {
            if (sender is Border border && border.DataContext is ItemViewModel item)
            {
                SetSelectedItem(item);
            }
        }

        public void SetSelectedItem(ItemViewModel? item)
        {
            // Deselect previous
            if (selectedItem != null)
            {
                selectedItem.IsSelected = false;
            }

            // Select new
            selectedItem = item;
            if (selectedItem != null)
            {
                selectedItem.IsSelected = true;
            }

            ItemSelected?.Invoke(this, selectedItem);
        }

        public void SetButtonStates(bool canEdit, bool canDelete)
        {
            var editButton = this.FindControl<Button>("EditItemButton");
            var deleteButton = this.FindControl<Button>("DeleteItemButton");
            
            if (editButton != null) editButton.IsEnabled = canEdit;
            if (deleteButton != null) deleteButton.IsEnabled = canDelete;
        }
    }

    /// <summary>
    /// View model for a single item (weapon or armor)
    /// </summary>
    public class ItemViewModel : INotifyPropertyChanged
    {
        private string _name = "";
        private string _type = "";
        private string _slot = "";
        private int _currentTier = 1;
        private int _selectedTier = 1;
        private bool _isSelected = false;
        private string _details = "";
        private bool _isWeapon = false;

        // Weapon properties
        private int _baseDamage = 0;
        private double _attackSpeed = 0;
        private int _hitCount = 0;
        private string _effect = "";

        // Armor properties
        private int _armor = 0;

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

        public int CurrentTier
        {
            get => _currentTier;
            set { _currentTier = value; SelectedTier = value; OnPropertyChanged(); }
        }

        public int SelectedTier
        {
            get => _selectedTier;
            set 
            { 
                if (_selectedTier != value)
                {
                    _selectedTier = value; 
                    OnPropertyChanged();
                }
            }
        }

        public bool IsSelected
        {
            get => _isSelected;
            set { _isSelected = value; OnPropertyChanged(); OnPropertyChanged(nameof(BackgroundColor)); }
        }

        public string Details
        {
            get => _details;
            set { _details = value; OnPropertyChanged(); }
        }

        public bool IsWeapon
        {
            get => _isWeapon;
            set { _isWeapon = value; OnPropertyChanged(); }
        }

        public int BaseDamage
        {
            get => _baseDamage;
            set { _baseDamage = value; UpdateDetails(); OnPropertyChanged(); }
        }

        public double AttackSpeed
        {
            get => _attackSpeed;
            set { _attackSpeed = value; UpdateDetails(); OnPropertyChanged(); }
        }

        public int HitCount
        {
            get => _hitCount;
            set { _hitCount = value; UpdateDetails(); OnPropertyChanged(); }
        }

        public string Effect
        {
            get => _effect;
            set { _effect = value; UpdateDetails(); OnPropertyChanged(); }
        }

        public int Armor
        {
            get => _armor;
            set { _armor = value; UpdateDetails(); OnPropertyChanged(); }
        }

        public string BackgroundColor => IsSelected ? "#FFE3F2FD" : "White";

        public ObservableCollection<int> AvailableTiers { get; set; } = new ObservableCollection<int> { 1, 2, 3, 4, 5 };

        private void UpdateDetails()
        {
            if (IsWeapon)
            {
                Details = $"Damage: {BaseDamage}, Speed: {AttackSpeed:F1}";
                if (HitCount > 0)
                {
                    Details += $", Hits: {HitCount}";
                }
                if (!string.IsNullOrEmpty(Effect) && Effect != "none")
                {
                    Details += $", Effect: {Effect}";
                }
            }
            else
            {
                Details = $"Armor: {Armor}";
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
