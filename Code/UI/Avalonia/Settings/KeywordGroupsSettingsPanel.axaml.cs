using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Markup.Xaml;
using Avalonia.Media;
using RPGGame.Data;
using RPGGame.UI.Avalonia.Settings.Helpers;
using System;
using System.Collections.Generic;
using System.Linq;
using Colors = Avalonia.Media.Colors;
using TextWrapping = Avalonia.Media.TextWrapping;

namespace RPGGame.UI.Avalonia.Settings
{
    public partial class KeywordGroupsSettingsPanel : UserControl
    {
        private List<KeywordGroupData> _keywordGroups = new List<KeywordGroupData>();
        private KeywordGroupData? _selectedKeywordGroup;
        private StackPanel? _keywordEditorPanel;

        public KeywordGroupsSettingsPanel()
        {
            InitializeComponent();
            LoadKeywordColors();
            SetupSaveButtons();
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }

        private void SetupSaveButtons()
        {
            var saveKeywordsButton = this.FindControl<Button>("SaveKeywordColorsButton");
            if (saveKeywordsButton != null)
            {
                saveKeywordsButton.Click += (s, e) => SaveKeywordColors();
            }

            var addKeywordGroupButton = this.FindControl<Button>("AddKeywordGroupButton");
            if (addKeywordGroupButton != null)
            {
                addKeywordGroupButton.Click += (s, e) => AddNewKeywordGroup();
            }
        }

        private void LoadKeywordColors()
        {
            var keywordsComboBox = this.FindControl<ComboBox>("KeywordsComboBox");
            if (keywordsComboBox == null) return;

            _keywordGroups = ColorConfigurationLoader.GetKeywordGroups()?.ToList() ?? new List<KeywordGroupData>();

            keywordsComboBox.ItemsSource = _keywordGroups.OrderBy(g => g.Name).ToList();
            keywordsComboBox.SelectionChanged += (s, e) =>
            {
                if (keywordsComboBox.SelectedItem is KeywordGroupData group)
                {
                    SelectKeywordGroup(group);
                }
            };
        }

        private void SelectKeywordGroup(KeywordGroupData group)
        {
            _selectedKeywordGroup = group;
            _keywordEditorPanel = this.FindControl<StackPanel>("KeywordEditorPanel");
            if (_keywordEditorPanel == null) return;

            _keywordEditorPanel.Children.Clear();

            // Name
            var nameLabel = new TextBlock { Text = "Group Name:", FontWeight = FontWeight.Bold, Margin = new Thickness(0, 0, 0, 5) };
            _keywordEditorPanel.Children.Add(nameLabel);
            var nameBox = new TextBox
            {
                Text = group.Name ?? "",
                Margin = new Thickness(0, 0, 0, 10)
            };
            nameBox.TextChanged += (s, e) => group.Name = nameBox.Text ?? "";
            _keywordEditorPanel.Children.Add(nameBox);

            // Color Pattern
            var patternLabel = new TextBlock { Text = "Color Pattern:", FontWeight = FontWeight.Bold, Margin = new Thickness(0, 0, 0, 5) };
            _keywordEditorPanel.Children.Add(patternLabel);
            var patternBox = new TextBox
            {
                Text = group.ColorPattern ?? "",
                Margin = new Thickness(0, 0, 0, 10)
            };
            patternBox.TextChanged += (s, e) => group.ColorPattern = patternBox.Text ?? "";
            _keywordEditorPanel.Children.Add(patternBox);

            // Case Sensitive
            var caseCheck = new CheckBox
            {
                Content = "Case Sensitive",
                IsChecked = group.CaseSensitive,
                Margin = new Thickness(0, 0, 0, 10)
            };
            caseCheck.IsCheckedChanged += (s, e) => group.CaseSensitive = caseCheck.IsChecked ?? false;
            _keywordEditorPanel.Children.Add(caseCheck);

            // Keywords
            var keywordsLabel = new TextBlock { Text = "Keywords (comma-separated):", FontWeight = FontWeight.Bold, Margin = new Thickness(0, 0, 0, 5) };
            _keywordEditorPanel.Children.Add(keywordsLabel);
            var keywordsBox = new TextBox
            {
                Text = string.Join(", ", group.Keywords ?? new List<string>()),
                AcceptsReturn = true,
                TextWrapping = TextWrapping.Wrap,
                MinHeight = 100,
                Margin = new Thickness(0, 0, 0, 10)
            };
            keywordsBox.TextChanged += (s, e) =>
            {
                group.Keywords = keywordsBox.Text.Split(',')
                    .Select(k => k.Trim())
                    .Where(k => !string.IsNullOrEmpty(k))
                    .ToList();
            };
            _keywordEditorPanel.Children.Add(keywordsBox);

            // Delete button
            var deleteBtn = new Button
            {
                Content = "Delete Group",
                Background = new SolidColorBrush(Colors.Red),
                Foreground = new SolidColorBrush(Colors.White),
                Margin = new Thickness(0, 10, 0, 10),
                HorizontalAlignment = HorizontalAlignment.Right
            };
            deleteBtn.Click += (s, e) => DeleteKeywordGroup(group);
            _keywordEditorPanel.Children.Add(deleteBtn);
        }

        private void AddNewKeywordGroup()
        {
            var newGroup = new KeywordGroupData
            {
                Name = $"new_group_{_keywordGroups.Count + 1}",
                ColorPattern = "fiery",
                CaseSensitive = false,
                Keywords = new List<string> { "example", "keyword" }
            };
            _keywordGroups.Add(newGroup);
            LoadKeywordColors();
            
            var keywordsComboBox = this.FindControl<ComboBox>("KeywordsComboBox");
            if (keywordsComboBox != null)
            {
                LoadKeywordColors();
                keywordsComboBox.SelectedItem = newGroup;
            }
        }

        private void DeleteKeywordGroup(KeywordGroupData group)
        {
            _keywordGroups.Remove(group);
            _selectedKeywordGroup = null;
            var editorPanel = this.FindControl<StackPanel>("KeywordEditorPanel");
            if (editorPanel != null)
            {
                editorPanel.Children.Clear();
            }
            LoadKeywordColors();
        }

        private void SaveKeywordColors()
        {
            AppearanceSettingsHelper.SaveKeywordColors(_keywordGroups);
        }
    }
}
