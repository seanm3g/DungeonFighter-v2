using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using Avalonia.Controls;
using RPGGame.Data;

namespace RPGGame.UI.Avalonia.Settings
{
    public partial class MaterialBuildsSettingsPanel : UserControl
    {
        public ObservableCollection<MaterialBuildRowViewModel> Rows { get; } = new();

        public MaterialBuildsSettingsPanel()
        {
            InitializeComponent();
            var list = this.FindControl<ItemsControl>("RowsItemsControl");
            if (list != null)
                list.ItemsSource = Rows;

            var add = this.FindControl<Button>("AddMaterialBuildButton");
            if (add != null)
                add.Click += (_, _) => AddRow();
        }

        public void LoadFromCatalog()
        {
            Rows.Clear();
            foreach (var row in MaterialBuildsLoader.GetAll())
                Rows.Add(MaterialBuildRowViewModel.FromData(row, RemoveRow));
        }

        public void AddRow()
        {
            Rows.Add(new MaterialBuildRowViewModel(RemoveRow)
            {
                AssociatedClass = "",
                Material = "",
                Synthesis = "",
                ConvertAction = "",
                Feed = "+/x per ",
                Stack2 = "SYNTHESIS + CONVERT UNLOCK",
                Stack3 = "+ feed",
                Stack5 = "x feed"
            });
        }

        private void RemoveRow(MaterialBuildRowViewModel row)
        {
            Rows.Remove(row);
        }
    }

    public sealed class MaterialBuildRowViewModel : INotifyPropertyChanged
    {
        private string _associatedClass = "";
        private string _material = "";
        private string _synthesis = "";
        private string _convertAction = "";
        private string _feed = "";
        private string _stack2 = "SYNTHESIS + CONVERT UNLOCK";
        private string _stack3 = "+ feed";
        private string _stack5 = "x feed";

        public MaterialBuildRowViewModel(System.Action<MaterialBuildRowViewModel> onDelete)
        {
            DeleteCommand = new SimpleRelayCommand(() => onDelete(this));
        }

        public ICommand DeleteCommand { get; }

        public string Header =>
            string.IsNullOrWhiteSpace(Material) ? "(new material)" : $"{AssociatedClass} / {Material}".Trim(' ', '/');

        public string AssociatedClass
        {
            get => _associatedClass;
            set { if (Set(ref _associatedClass, value)) OnPropertyChanged(nameof(Header)); }
        }

        public string Material
        {
            get => _material;
            set { if (Set(ref _material, value)) OnPropertyChanged(nameof(Header)); }
        }

        public string Synthesis
        {
            get => _synthesis;
            set => Set(ref _synthesis, value);
        }

        public string ConvertAction
        {
            get => _convertAction;
            set => Set(ref _convertAction, value);
        }

        public string Feed
        {
            get => _feed;
            set => Set(ref _feed, value);
        }

        public string Stack2
        {
            get => _stack2;
            set => Set(ref _stack2, value);
        }

        public string Stack3
        {
            get => _stack3;
            set => Set(ref _stack3, value);
        }

        public string Stack5
        {
            get => _stack5;
            set => Set(ref _stack5, value);
        }

        public static MaterialBuildRowViewModel FromData(MaterialBuildData data, System.Action<MaterialBuildRowViewModel> onDelete)
        {
            return new MaterialBuildRowViewModel(onDelete)
            {
                AssociatedClass = data.AssociatedClass ?? "",
                Material = data.Material ?? "",
                Synthesis = data.Synthesis ?? "",
                ConvertAction = data.ConvertAction ?? "",
                Feed = data.Feed ?? "",
                Stack2 = string.IsNullOrWhiteSpace(data.Stack2) ? "SYNTHESIS + CONVERT UNLOCK" : data.Stack2,
                Stack3 = string.IsNullOrWhiteSpace(data.Stack3) ? "+ feed" : data.Stack3,
                Stack5 = string.IsNullOrWhiteSpace(data.Stack5) ? "x feed" : data.Stack5
            };
        }

        public MaterialBuildData ToData()
        {
            var data = new MaterialBuildData
            {
                AssociatedClass = AssociatedClass?.Trim() ?? "",
                Material = Material?.Trim() ?? "",
                Synthesis = Synthesis?.Trim() ?? "",
                ConvertAction = ConvertAction?.Trim() ?? "",
                Feed = Feed?.Trim() ?? "",
                Stack2 = Stack2?.Trim() ?? "",
                Stack3 = Stack3?.Trim() ?? "",
                Stack5 = Stack5?.Trim() ?? ""
            };
            data.RefreshParsedFields();
            return data;
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        private bool Set<T>(ref T field, T value, [CallerMemberName] string? name = null)
        {
            if (Equals(field, value))
                return false;
            field = value;
            OnPropertyChanged(name);
            return true;
        }

        private void OnPropertyChanged([CallerMemberName] string? name = null) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }

    internal sealed class SimpleRelayCommand : ICommand
    {
        private readonly System.Action _execute;
        public SimpleRelayCommand(System.Action execute) => _execute = execute;
        public bool CanExecute(object? parameter) => true;
        public void Execute(object? parameter) => _execute();
#pragma warning disable CS0067
        public event EventHandler? CanExecuteChanged;
#pragma warning restore CS0067
    }
}
