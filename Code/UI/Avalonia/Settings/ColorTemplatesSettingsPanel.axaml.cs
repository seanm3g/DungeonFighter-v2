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
    public partial class ColorTemplatesSettingsPanel : UserControl
    {
        private List<ColorTemplateData> _colorTemplates = new List<ColorTemplateData>();
        private ColorTemplateData? _selectedTemplate;
        private StackPanel? _templateEditorPanel;

        public ColorTemplatesSettingsPanel()
        {
            InitializeComponent();
            LoadColorTemplates();
            SetupSaveButtons();
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }

        private void SetupSaveButtons()
        {
            var saveTemplatesButton = this.FindControl<Button>("SaveColorTemplatesButton");
            if (saveTemplatesButton != null)
            {
                saveTemplatesButton.Click += (s, e) => SaveColorTemplates();
            }

            var addTemplateButton = this.FindControl<Button>("AddTemplateButton");
            if (addTemplateButton != null)
            {
                addTemplateButton.Click += (s, e) => AddNewTemplate();
            }
        }

        private void LoadColorTemplates()
        {
            var templatesComboBox = this.FindControl<ComboBox>("TemplatesComboBox");
            if (templatesComboBox == null) return;

            var config = ColorConfigurationLoader.LoadColorConfiguration();
            _colorTemplates = config.ColorTemplates?.ToList() ?? new List<ColorTemplateData>();

            templatesComboBox.ItemsSource = _colorTemplates.OrderBy(t => t.Name).ToList();
            templatesComboBox.SelectionChanged += (s, e) =>
            {
                if (templatesComboBox.SelectedItem is ColorTemplateData template)
                {
                    SelectTemplate(template);
                }
            };
        }

        private void SelectTemplate(ColorTemplateData template)
        {
            _selectedTemplate = template;
            _templateEditorPanel = this.FindControl<StackPanel>("TemplateEditorPanel");
            if (_templateEditorPanel == null) return;

            _templateEditorPanel.Children.Clear();

            // Name
            var nameLabel = new TextBlock { Text = "Name:", FontWeight = FontWeight.Bold, Margin = new Thickness(0, 0, 0, 5) };
            _templateEditorPanel.Children.Add(nameLabel);
            var nameBox = new TextBox
            {
                Text = template.Name ?? "",
                Margin = new Thickness(0, 0, 0, 10)
            };
            nameBox.TextChanged += (s, e) => template.Name = nameBox.Text ?? "";
            _templateEditorPanel.Children.Add(nameBox);

            // Shader
            var shaderLabel = new TextBlock { Text = "Shader Type:", FontWeight = FontWeight.Bold, Margin = new Thickness(0, 0, 0, 5) };
            _templateEditorPanel.Children.Add(shaderLabel);
            var shaderBox = new ComboBox
            {
                SelectedItem = template.ShaderType ?? "sequence",
                ItemsSource = new[] { "solid", "sequence", "alternation" },
                Margin = new Thickness(0, 0, 0, 10)
            };
            shaderBox.SelectionChanged += (s, e) =>
            {
                if (shaderBox.SelectedItem is string selected)
                    template.ShaderType = selected;
            };
            _templateEditorPanel.Children.Add(shaderBox);

            // Colors
            var colorsLabel = new TextBlock { Text = "Colors (comma-separated):", FontWeight = FontWeight.Bold, Margin = new Thickness(0, 0, 0, 5) };
            _templateEditorPanel.Children.Add(colorsLabel);
            var colorsBox = new TextBox
            {
                Text = string.Join(", ", template.Colors ?? new List<string>()),
                Margin = new Thickness(0, 0, 0, 10)
            };
            colorsBox.TextChanged += (s, e) =>
            {
                template.Colors = colorsBox.Text.Split(',')
                    .Select(c => c.Trim())
                    .Where(c => !string.IsNullOrEmpty(c))
                    .ToList();
            };
            _templateEditorPanel.Children.Add(colorsBox);

            // Description
            var descLabel = new TextBlock { Text = "Description:", FontWeight = FontWeight.Bold, Margin = new Thickness(0, 0, 0, 5) };
            _templateEditorPanel.Children.Add(descLabel);
            var descBox = new TextBox
            {
                Text = template.Description ?? "",
                AcceptsReturn = true,
                TextWrapping = TextWrapping.Wrap,
                MinHeight = 60
            };
            descBox.TextChanged += (s, e) => template.Description = descBox.Text ?? "";
            _templateEditorPanel.Children.Add(descBox);

            // Delete button
            var deleteBtn = new Button
            {
                Content = "Delete Template",
                Background = new SolidColorBrush(Colors.Red),
                Foreground = new SolidColorBrush(Colors.White),
                Margin = new Thickness(0, 10, 0, 10),
                HorizontalAlignment = HorizontalAlignment.Right
            };
            deleteBtn.Click += (s, e) => DeleteTemplate(template);
            _templateEditorPanel.Children.Add(deleteBtn);
        }

        private void AddNewTemplate()
        {
            var newTemplate = new ColorTemplateData
            {
                Name = $"new_template_{_colorTemplates.Count + 1}",
                ShaderType = "sequence",
                Colors = new List<string> { "R", "O", "Y" },
                Description = "New color template"
            };
            _colorTemplates.Add(newTemplate);
            LoadColorTemplates();
            
            var templatesComboBox = this.FindControl<ComboBox>("TemplatesComboBox");
            if (templatesComboBox != null)
            {
                LoadColorTemplates();
                templatesComboBox.SelectedItem = newTemplate;
            }
        }

        private void DeleteTemplate(ColorTemplateData template)
        {
            _colorTemplates.Remove(template);
            _selectedTemplate = null;
            var editorPanel = this.FindControl<StackPanel>("TemplateEditorPanel");
            if (editorPanel != null)
            {
                editorPanel.Children.Clear();
            }
            LoadColorTemplates();
        }

        private void SaveColorTemplates()
        {
            AppearanceSettingsHelper.SaveColorTemplates(_colorTemplates);
        }
    }
}
