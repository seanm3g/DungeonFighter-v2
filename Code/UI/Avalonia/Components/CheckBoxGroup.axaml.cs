using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using RPGGame.UI.Avalonia.Resources;
using System.Collections.Generic;

namespace RPGGame.UI.Avalonia.Components
{
    public partial class CheckBoxGroup : UserControl
    {
        private List<CheckBox> checkBoxes = new List<CheckBox>();

        public CheckBoxGroup()
        {
            InitializeComponent();
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }

        /// <summary>
        /// Clears all checkboxes from the group
        /// </summary>
        public void Clear()
        {
            checkBoxes.Clear();
            CheckBoxContainer.Children.Clear();
        }
    }
}

