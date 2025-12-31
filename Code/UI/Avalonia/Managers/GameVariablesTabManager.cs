using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Input;
using Avalonia.Threading;
using RPGGame.Editors;
using RPGGame.UI.Avalonia.Builders;
using RPGGame.UI.Avalonia.Validators;
using System;
using System.Collections.Generic;
using System.Linq;

namespace RPGGame.UI.Avalonia.Managers
{
    /// <summary>
    /// Manages the Game Variables tab in SettingsPanel
    /// Extracted from SettingsPanel.axaml.cs to reduce file size
    /// </summary>
    public class GameVariablesTabManager
    {
        private VariableEditor? variableEditor;
        private string? selectedGameVariableCategory;
        private Dictionary<string, TextBox> gameVariableTextBoxes = new Dictionary<string, TextBox>();
        private Dictionary<string, TextBlock> gameVariableChangeIndicators = new Dictionary<string, TextBlock>();
        private Dictionary<string, string> gameVariableCategoryDisplayToName = new Dictionary<string, string>();
        private ChangeIndicatorManager gameVariableChangeIndicatorManager = new ChangeIndicatorManager();
        
        private ListBox? categoryListBox;
        private Panel? variablesPanel;
        private Action<string, bool>? showStatusMessage;

        public GameVariablesTabManager()
        {
            variableEditor = new VariableEditor();
        }

        public void Initialize(ListBox categoryListBox, Panel variablesPanel, Action<string, bool> showStatusMessage)
        {
            this.categoryListBox = categoryListBox;
            this.variablesPanel = variablesPanel;
            this.showStatusMessage = showStatusMessage;
            
            // Defer loading categories until after UI is ready to avoid blocking
            Dispatcher.UIThread.Post(() =>
            {
                LoadGameVariableCategories();
            }, DispatcherPriority.Background);
            
            categoryListBox.SelectionChanged += OnGameVariableCategorySelectionChanged;
        }

        public void LoadGameVariableCategories()
        {
            if (variableEditor == null || categoryListBox == null) return;
            
            var categories = variableEditor.GetCategories();
            gameVariableCategoryDisplayToName.Clear();
            
            var categoryItems = categories.Select(cat =>
            {
                var count = variableEditor.GetVariablesByCategory(cat).Count;
                var displayText = $"{cat} ({count})";
                gameVariableCategoryDisplayToName[displayText] = cat;
                return displayText;
            }).ToList();
            
            categoryListBox.ItemsSource = categoryItems;
        }

        private void OnGameVariableCategorySelectionChanged(object? sender, SelectionChangedEventArgs e)
        {
            if (categoryListBox?.SelectedItem is string displayText && 
                gameVariableCategoryDisplayToName.TryGetValue(displayText, out var categoryName))
            {
                selectedGameVariableCategory = categoryName;
                LoadGameVariablesForCategory(categoryName);
            }
        }

        private void LoadGameVariablesForCategory(string category)
        {
            if (variableEditor == null || variablesPanel == null) return;
            
            variablesPanel.Children.Clear();
            gameVariableTextBoxes.Clear();
            gameVariableChangeIndicators.Clear();
            gameVariableChangeIndicatorManager.Clear();
            
            var variables = variableEditor.GetVariablesByCategory(category);
            
            var categoryHeader = CreateGameVariableCategoryHeader(category, variables.Count);
            variablesPanel.Children.Add(categoryHeader);
            
            var variablesGrid = new Grid();
            variablesGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            variablesGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(10) });
            variablesGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            
            int numRows = (variables.Count + 1) / 2;
            for (int r = 0; r < numRows; r++)
            {
                variablesGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            }
            
            for (int i = 0; i < variables.Count; i++)
            {
                var variable = variables[i];
                
                gameVariableChangeIndicatorManager.SetOriginalValue(variable.Name, variable.GetValue() ?? new object());
                
                var (container, textBox, indicator) = VariableControlBuilder.CreateVariableControl(
                    variable,
                    (v, tb, ind) => UpdateGameVariableChangeIndicator(v, tb, ind),
                    (v, tb) => ValidateAndUpdateGameVariable(v, tb),
                    (v, tb, e) =>
                    {
                        if (e.Key == Key.Enter)
                        {
                            ValidateAndUpdateGameVariable(v, tb);
                            e.Handled = true;
                        }
                    });
                
                gameVariableTextBoxes[variable.Name] = textBox;
                gameVariableChangeIndicators[variable.Name] = indicator;
                
                int row = i / 2;
                int column = (i % 2 == 0) ? 0 : 2;
                
                Grid.SetColumn(container, column);
                Grid.SetRow(container, row);
                variablesGrid.Children.Add(container);
            }
            
            variablesPanel.Children.Add(variablesGrid);
        }

        private Control CreateGameVariableCategoryHeader(string category, int variableCount)
        {
            var header = new Border
            {
                Background = new SolidColorBrush(Color.FromRgb(40, 40, 60)),
                BorderBrush = new SolidColorBrush(Color.FromRgb(100, 100, 150)),
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(2),
                Padding = new Thickness(4, 3),
                Margin = new Thickness(0, 0, 0, 2)
            };
            
            var stack = new StackPanel { Spacing = 2 };
            
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
                FontSize = 13,
                Foreground = new SolidColorBrush(Color.FromRgb(180, 180, 180))
            };
            stack.Children.Add(info);
            
            header.Child = stack;
            return header;
        }

        private void UpdateGameVariableChangeIndicator(EditableVariable variable, TextBox textBox, TextBlock indicator)
        {
            gameVariableChangeIndicatorManager.UpdateChangeIndicator(
                variable,
                textBox,
                indicator,
                VariableValidator.ValidateValue);
        }

        private void ValidateAndUpdateGameVariable(EditableVariable variable, TextBox textBox)
        {
            if (variableEditor == null) return;
            
            var (success, errorMessage) = VariableValidator.ValidateAndUpdate(
                variable,
                textBox,
                (msg, isSuccess) => showStatusMessage?.Invoke(msg, isSuccess));
            
            if (success)
            {
                if (gameVariableChangeIndicators.TryGetValue(variable.Name, out var indicator))
                {
                    UpdateGameVariableChangeIndicator(variable, textBox, indicator);
                }
                
                var newValue = variable.GetValue();
                if (newValue != null)
                {
                    gameVariableChangeIndicatorManager.UpdateOriginalValue(variable.Name, newValue);
                }
            }
        }

        public void SaveGameVariables()
        {
            if (variableEditor == null) return;
            
            try
            {
                // Ensure any focused text box commits its value
                foreach (var kvp in gameVariableTextBoxes)
                {
                    var textBox = kvp.Value;
                    if (textBox.IsFocused)
                    {
                        // Force the text box to lose focus so any pending changes are committed
                        textBox.Focusable = false;
                        textBox.Focusable = true;
                        // Trigger validation to ensure the value is updated in memory
                        var variable = variableEditor.GetVariables().FirstOrDefault(v => v.Name == kvp.Key);
                        if (variable != null)
                        {
                            ValidateAndUpdateGameVariable(variable, textBox);
                        }
                    }
                }
                
                // Save all changes to file and reload GameConfiguration singleton
                bool saved = variableEditor.SaveChanges();
                if (saved)
                {
                    if (!string.IsNullOrEmpty(selectedGameVariableCategory))
                    {
                        RefreshGameVariableValues();
                    }
                    showStatusMessage?.Invoke("Game variables saved successfully", true);
                }
                else
                {
                    showStatusMessage?.Invoke("Failed to save game variables", false);
                }
            }
            catch (Exception ex)
            {
                showStatusMessage?.Invoke($"Error saving game variables: {ex.Message}", false);
            }
        }

        private void RefreshGameVariableValues()
        {
            if (variableEditor == null || string.IsNullOrEmpty(selectedGameVariableCategory)) return;
            
            var variables = variableEditor.GetVariablesByCategory(selectedGameVariableCategory);
            foreach (var variable in variables)
            {
                if (gameVariableTextBoxes.TryGetValue(variable.Name, out var textBox))
                {
                    var currentValue = variable.GetValue();
                    textBox.Text = currentValue?.ToString() ?? "";
                    textBox.Background = new SolidColorBrush(Color.FromRgb(40, 40, 40));
                    
                    if (currentValue != null)
                    {
                        gameVariableChangeIndicatorManager.UpdateOriginalValue(variable.Name, currentValue);
                    }
                    if (gameVariableChangeIndicators.TryGetValue(variable.Name, out var indicator))
                    {
                        UpdateGameVariableChangeIndicator(variable, textBox, indicator);
                    }
                }
            }
        }
    }
}

