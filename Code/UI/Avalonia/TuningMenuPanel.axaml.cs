using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.Layout;
using Avalonia.Input;
using Avalonia.Threading;
using RPGGame.Editors;
using RPGGame.UI.Avalonia.Builders;
using RPGGame.UI.Avalonia.Validators;
using RPGGame.UI.Avalonia.Managers;
using System;
using System.Collections.Generic;
using System.Linq;

namespace RPGGame.UI.Avalonia
{
    public partial class TuningMenuPanel : UserControl
    {
        private VariableEditor? variableEditor;
        private string? selectedCategory;
        private Dictionary<string, TextBox> variableTextBoxes = new Dictionary<string, TextBox>();
        private Dictionary<string, TextBlock> valueChangeIndicators = new Dictionary<string, TextBlock>();
        private Dictionary<string, string> categoryDisplayToName = new Dictionary<string, string>();
        private ChangeIndicatorManager changeIndicatorManager = new ChangeIndicatorManager();
        
        public event EventHandler<string>? CategorySelected;
        public event EventHandler? BackRequested;
        public event EventHandler? SaveRequested;
        
        public TuningMenuPanel()
        {
            InitializeComponent();
            
            CategoryListBox.SelectionChanged += OnCategorySelectionChanged;
            BackButton.Click += OnBackButtonClick;
            SaveButton.Click += OnSaveButtonClick;
        }
        
        public void Initialize(VariableEditor editor)
        {
            variableEditor = editor;
            LoadCategories();
            ResetToCategoryView();
        }
        
        public void ResetToCategoryView()
        {
            VariablesPanel.Children.Clear();
            variableTextBoxes.Clear();
            CategoryListBox.SelectedItem = null;
            selectedCategory = null;
        }
        
        private void LoadCategories()
        {
            if (variableEditor == null) return;
            
            var categories = variableEditor.GetCategories();
            categoryDisplayToName.Clear();
            
            // Create category items with counts
            var categoryItems = categories.Select(cat =>
            {
                var count = variableEditor.GetVariablesByCategory(cat).Count;
                var displayText = $"{cat} ({count})";
                categoryDisplayToName[displayText] = cat;
                return displayText;
            }).ToList();
            
            CategoryListBox.ItemsSource = categoryItems;
        }
        
        private void OnCategorySelectionChanged(object? sender, SelectionChangedEventArgs e)
        {
            if (CategoryListBox.SelectedItem is string displayText && 
                categoryDisplayToName.TryGetValue(displayText, out var categoryName))
            {
                selectedCategory = categoryName;
                LoadVariablesForCategory(categoryName);
                CategorySelected?.Invoke(this, categoryName);
            }
        }
        
        private void LoadVariablesForCategory(string category)
        {
            if (variableEditor == null) return;
            
            VariablesPanel.Children.Clear();
            variableTextBoxes.Clear();
            valueChangeIndicators.Clear();
            changeIndicatorManager.Clear();
            
            var variables = variableEditor.GetVariablesByCategory(category);
            
            // Add category info header
            var categoryHeader = CreateCategoryHeader(category, variables.Count);
            VariablesPanel.Children.Add(categoryHeader);
            
            foreach (var variable in variables)
            {
                // Store original value
                changeIndicatorManager.SetOriginalValue(variable.Name, variable.GetValue() ?? new object());
                
                var (container, textBox, indicator) = VariableControlBuilder.CreateVariableControl(
                    variable,
                    (v, tb, ind) => UpdateChangeIndicator(v, tb, ind),
                    (v, tb) => ValidateAndUpdateVariable(v, tb),
                    (v, tb, e) =>
                    {
                        if (e.Key == Key.Enter)
                        {
                            ValidateAndUpdateVariable(v, tb);
                            e.Handled = true;
                        }
                    });
                
                variableTextBoxes[variable.Name] = textBox;
                valueChangeIndicators[variable.Name] = indicator;
                VariablesPanel.Children.Add(container);
            }
        }
        
        private Control CreateCategoryHeader(string category, int variableCount)
        {
            var header = new Border
            {
                Background = new SolidColorBrush(Color.FromRgb(40, 40, 60)),
                BorderBrush = new SolidColorBrush(Color.FromRgb(100, 100, 150)),
                BorderThickness = new Thickness(2),
                CornerRadius = new CornerRadius(5),
                Padding = new Thickness(15),
                Margin = new Thickness(0, 0, 0, 15)
            };
            
            var stack = new StackPanel { Spacing = 5 };
            
            var title = new TextBlock
            {
                Text = $"📊 {category} Parameters",
                FontSize = 18,
                FontWeight = FontWeight.Bold,
                Foreground = new SolidColorBrush(Color.FromRgb(255, 215, 0))
            };
            stack.Children.Add(title);
            
            var info = new TextBlock
            {
                Text = $"{variableCount} parameters available • Changes are applied immediately",
                FontSize = 11,
                Foreground = new SolidColorBrush(Color.FromRgb(180, 180, 180))
            };
            stack.Children.Add(info);
            
            header.Child = stack;
            return header;
        }
        
        private void UpdateChangeIndicator(EditableVariable variable, TextBox textBox, TextBlock indicator)
        {
            changeIndicatorManager.UpdateChangeIndicator(
                variable,
                textBox,
                indicator,
                VariableValidator.ValidateValue);
        }
        
        private void ValidateAndUpdateVariable(EditableVariable variable, TextBox textBox)
        {
            if (variableEditor == null) return;
            
            var (success, errorMessage) = VariableValidator.ValidateAndUpdate(
                variable,
                textBox,
                ShowStatusMessage);
            
            if (success)
            {
                // Update change indicator
                if (valueChangeIndicators.TryGetValue(variable.Name, out var indicator))
                {
                    UpdateChangeIndicator(variable, textBox, indicator);
                }
                
                // Update original value after successful save
                var newValue = variable.GetValue();
                if (newValue != null)
                {
                    changeIndicatorManager.UpdateOriginalValue(variable.Name, newValue);
                }
            }
        }
        
        private void OnBackButtonClick(object? sender, RoutedEventArgs e)
        {
            BackRequested?.Invoke(this, EventArgs.Empty);
        }
        
        private void OnSaveButtonClick(object? sender, RoutedEventArgs e)
        {
            // Validate all text boxes before saving
            foreach (var kvp in variableTextBoxes)
            {
                var textBox = kvp.Value;
                if (textBox.IsFocused)
                {
                    textBox.Focusable = false;
                    textBox.Focusable = true;
                }
            }
            
            SaveRequested?.Invoke(this, EventArgs.Empty);
        }
        
        public void ShowStatusMessage(string message, bool isError = false)
        {
            StatusMessage.Text = message;
            StatusMessage.Foreground = isError 
                ? new SolidColorBrush(Color.FromRgb(255, 100, 100))
                : new SolidColorBrush(Color.FromRgb(100, 255, 100));
            StatusMessage.IsVisible = true;
            
            // Hide after 3 seconds
            var timer = new System.Timers.Timer(3000);
            timer.Elapsed += (s, e) =>
            {
                timer.Stop();
                Dispatcher.UIThread.Post(() =>
                {
                    StatusMessage.IsVisible = false;
                });
            };
            timer.Start();
        }
        
        public void RefreshVariableValues()
        {
            if (variableEditor == null || string.IsNullOrEmpty(selectedCategory)) return;
            
            var variables = variableEditor.GetVariablesByCategory(selectedCategory);
            foreach (var variable in variables)
            {
                if (variableTextBoxes.TryGetValue(variable.Name, out var textBox))
                {
                    var currentValue = variable.GetValue();
                    textBox.Text = currentValue?.ToString() ?? "";
                    textBox.Background = new SolidColorBrush(Color.FromRgb(40, 40, 40));
                    
                    // Update original value and refresh indicator
                    if (currentValue != null)
                    {
                        changeIndicatorManager.UpdateOriginalValue(variable.Name, currentValue);
                    }
                    if (valueChangeIndicators.TryGetValue(variable.Name, out var indicator))
                    {
                        UpdateChangeIndicator(variable, textBox, indicator);
                    }
                }
            }
        }
    }
}

