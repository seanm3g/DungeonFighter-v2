using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.Layout;
using Avalonia.Input;
using Avalonia.Threading;
using RPGGame.Editors;
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
        private Dictionary<string, object> originalValues = new Dictionary<string, object>();
        private Dictionary<string, TextBlock> valueChangeIndicators = new Dictionary<string, TextBlock>();
        private Dictionary<string, string> categoryDisplayToName = new Dictionary<string, string>();
        
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
            originalValues.Clear();
            valueChangeIndicators.Clear();
            
            var variables = variableEditor.GetVariablesByCategory(category);
            
            // Add category info header
            var categoryHeader = CreateCategoryHeader(category, variables.Count);
            VariablesPanel.Children.Add(categoryHeader);
            
            foreach (var variable in variables)
            {
                // Store original value
                originalValues[variable.Name] = variable.GetValue();
                
                var variableContainer = CreateVariableControl(variable);
                VariablesPanel.Children.Add(variableContainer);
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
        
        private Control CreateVariableControl(EditableVariable variable)
        {
            var container = new Border
            {
                Background = Brushes.Transparent,
                BorderBrush = new SolidColorBrush(Color.FromRgb(85, 85, 85)),
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(3),
                Padding = new Thickness(10),
                Margin = new Thickness(0, 0, 0, 10)
            };
            
            var grid = new Grid();
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(200) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(120) });
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            
            // Variable name
            var nameText = new TextBlock
            {
                Text = variable.Name,
                FontSize = 14,
                FontWeight = FontWeight.Bold,
                Foreground = Brushes.White,
                Margin = new Thickness(0, 0, 0, 5)
            };
            Grid.SetColumn(nameText, 0);
            Grid.SetRow(nameText, 0);
            Grid.SetColumnSpan(nameText, 2);
            grid.Children.Add(nameText);
            
            // Description
            var descText = new TextBlock
            {
                Text = variable.Description,
                FontSize = 11,
                Foreground = new SolidColorBrush(Color.FromRgb(200, 200, 200)),
                Margin = new Thickness(0, 0, 0, 8),
                TextWrapping = TextWrapping.Wrap
            };
            Grid.SetColumn(descText, 0);
            Grid.SetRow(descText, 1);
            Grid.SetColumnSpan(descText, 2);
            grid.Children.Add(descText);
            
            // Value label
            var valueLabel = new TextBlock
            {
                Text = "Value:",
                FontSize = 12,
                Foreground = Brushes.White,
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(0, 0, 10, 0)
            };
            Grid.SetColumn(valueLabel, 0);
            Grid.SetRow(valueLabel, 2);
            grid.Children.Add(valueLabel);
            
            // Value text box
            var valueTextBox = new TextBox
            {
                Text = variable.GetValue()?.ToString() ?? "",
                FontSize = 12,
                Background = new SolidColorBrush(Color.FromRgb(40, 40, 40)),
                Foreground = Brushes.White,
                BorderBrush = new SolidColorBrush(Color.FromRgb(100, 100, 100)),
                BorderThickness = new Thickness(1),
                Padding = new Thickness(5),
                VerticalAlignment = VerticalAlignment.Center,
                Watermark = "Enter value..."
            };
            
            // Set appropriate input type
            var valueType = variable.GetValueType();
            if (valueType == typeof(int))
            {
                valueTextBox.Watermark = "Enter integer...";
            }
            else if (valueType == typeof(double))
            {
                valueTextBox.Watermark = "Enter number...";
            }
            else if (valueType == typeof(bool))
            {
                valueTextBox.Watermark = "Enter true/false...";
            }
            
            // Real-time change indicator
            var changeIndicator = new TextBlock
            {
                FontSize = 10,
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(5, 0, 0, 0),
                IsVisible = false
            };
            
            // Add real-time text change tracking
            valueTextBox.TextChanged += (s, e) =>
            {
                UpdateChangeIndicator(variable, valueTextBox, changeIndicator);
            };
            
            // Add validation on lost focus
            valueTextBox.LostFocus += (s, e) => ValidateAndUpdateVariable(variable, valueTextBox);
            
            // Allow Enter key to commit
            valueTextBox.KeyDown += (s, e) =>
            {
                if (e.Key == Key.Enter)
                {
                    ValidateAndUpdateVariable(variable, valueTextBox);
                    e.Handled = true;
                }
            };
            
            Grid.SetColumn(valueTextBox, 1);
            Grid.SetRow(valueTextBox, 2);
            grid.Children.Add(valueTextBox);
            
            Grid.SetColumn(changeIndicator, 2);
            Grid.SetRow(changeIndicator, 2);
            grid.Children.Add(changeIndicator);
            
            valueChangeIndicators[variable.Name] = changeIndicator;
            
            // Add row for value input
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            
            variableTextBoxes[variable.Name] = valueTextBox;
            
            container.Child = grid;
            return container;
        }
        
        private void UpdateChangeIndicator(EditableVariable variable, TextBox textBox, TextBlock indicator)
        {
            if (!originalValues.TryGetValue(variable.Name, out var originalValue)) return;
            
            var currentText = textBox.Text?.Trim() ?? "";
            if (string.IsNullOrEmpty(currentText))
            {
                indicator.IsVisible = false;
                return;
            }
            
            try
            {
                var valueType = variable.GetValueType();
                object? newValue = null;
                bool isValid = false;
                
                if (valueType == typeof(int) && int.TryParse(currentText, out int intVal))
                {
                    newValue = intVal;
                    isValid = true;
                }
                else if (valueType == typeof(double) && double.TryParse(currentText, out double doubleVal))
                {
                    newValue = doubleVal;
                    isValid = true;
                }
                else if (valueType == typeof(bool))
                {
                    string trimmed = currentText.ToLower();
                    if (bool.TryParse(trimmed, out bool boolVal))
                    {
                        newValue = boolVal;
                        isValid = true;
                    }
                    else if (trimmed == "1" || trimmed == "true" || trimmed == "t")
                    {
                        newValue = true;
                        isValid = true;
                    }
                    else if (trimmed == "0" || trimmed == "false" || trimmed == "f")
                    {
                        newValue = false;
                        isValid = true;
                    }
                }
                else if (valueType == typeof(string))
                {
                    newValue = currentText;
                    isValid = true;
                }
                
                if (isValid && newValue != null)
                {
                    // Compare with original
                    if (!newValue.Equals(originalValue))
                    {
                        // Calculate change percentage for numeric types
                        if (valueType == typeof(int) || valueType == typeof(double))
                        {
                            try
                            {
                                double orig = Convert.ToDouble(originalValue);
                                double newVal = Convert.ToDouble(newValue);
                                double changePercent = orig != 0 ? ((newVal - orig) / orig) * 100 : 0;
                                
                                indicator.Text = changePercent >= 0 
                                    ? $"+{changePercent:F1}%" 
                                    : $"{changePercent:F1}%";
                                indicator.Foreground = changePercent >= 0 
                                    ? new SolidColorBrush(Color.FromRgb(100, 255, 100))
                                    : new SolidColorBrush(Color.FromRgb(255, 150, 100));
                            }
                            catch
                            {
                                indicator.Text = "Changed";
                                indicator.Foreground = new SolidColorBrush(Color.FromRgb(255, 215, 0));
                            }
                        }
                        else
                        {
                            indicator.Text = "Changed";
                            indicator.Foreground = new SolidColorBrush(Color.FromRgb(255, 215, 0));
                        }
                        indicator.IsVisible = true;
                    }
                    else
                    {
                        indicator.IsVisible = false;
                    }
                }
                else
                {
                    indicator.Text = "Invalid";
                    indicator.Foreground = new SolidColorBrush(Color.FromRgb(255, 100, 100));
                    indicator.IsVisible = true;
                }
            }
            catch
            {
                indicator.IsVisible = false;
            }
        }
        
        private void ValidateAndUpdateVariable(EditableVariable variable, TextBox textBox)
        {
            if (variableEditor == null) return;
            
            try
            {
                var valueType = variable.GetValueType();
                object? newValue = null;
                bool isValid = false;
                
                if (valueType == typeof(int))
                {
                    if (int.TryParse(textBox.Text, out int intValue))
                    {
                        newValue = intValue;
                        isValid = true;
                    }
                }
                else if (valueType == typeof(double))
                {
                    if (double.TryParse(textBox.Text, out double doubleValue))
                    {
                        newValue = doubleValue;
                        isValid = true;
                    }
                }
                else if (valueType == typeof(bool))
                {
                    string trimmed = textBox.Text?.Trim().ToLower() ?? "";
                    if (bool.TryParse(trimmed, out bool boolValue))
                    {
                        newValue = boolValue;
                        isValid = true;
                    }
                    else if (trimmed == "1" || trimmed == "true" || trimmed == "t")
                    {
                        newValue = true;
                        isValid = true;
                    }
                    else if (trimmed == "0" || trimmed == "false" || trimmed == "f")
                    {
                        newValue = false;
                        isValid = true;
                    }
                }
                else
                {
                    // String or other types
                    newValue = textBox.Text?.Trim() ?? "";
                    isValid = true;
                }
                
                if (isValid && newValue != null)
                {
                    variable.SetValue(newValue);
                    textBox.Background = new SolidColorBrush(Color.FromRgb(40, 40, 40));
                    
                    // Update change indicator
                    if (valueChangeIndicators.TryGetValue(variable.Name, out var indicator))
                    {
                        UpdateChangeIndicator(variable, textBox, indicator);
                    }
                    
                    ShowStatusMessage($"✓ Updated {variable.Name} to {newValue}", isError: false);
                }
                else
                {
                    textBox.Background = new SolidColorBrush(Color.FromRgb(80, 40, 40));
                    ShowStatusMessage($"Invalid value for {variable.Name}. Expected {valueType.Name}.", isError: true);
                    // Reset to current value
                    textBox.Text = variable.GetValue()?.ToString() ?? "";
                }
            }
            catch (Exception ex)
            {
                textBox.Background = new SolidColorBrush(Color.FromRgb(80, 40, 40));
                ShowStatusMessage($"Error updating {variable.Name}: {ex.Message}", isError: true);
                // Reset to current value
                textBox.Text = variable.GetValue()?.ToString() ?? "";
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
                        originalValues[variable.Name] = currentValue;
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

