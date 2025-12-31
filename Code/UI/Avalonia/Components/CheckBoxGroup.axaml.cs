using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Avalonia.Media;
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
        /// Adds a checkbox to the group
        /// </summary>
        public CheckBox AddCheckBox(string content, bool isChecked = false)
        {
            var checkBox = new CheckBox
            {
                Content = content,
                IsChecked = isChecked,
                Foreground = new SolidColorBrush(Colors.Black),
                FontSize = 15
            };
            
            checkBoxes.Add(checkBox);
            CheckBoxContainer.Children.Add(checkBox);
            
            return checkBox;
        }

        /// <summary>
        /// Gets all checkboxes in the group
        /// </summary>
        public IEnumerable<CheckBox> GetCheckBoxes()
        {
            return checkBoxes;
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

