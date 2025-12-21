using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.Media;
using RPGGame.Editors;
using System;
using System.Collections.Generic;
using System.Linq;

namespace RPGGame.UI.Avalonia.Managers
{
    /// <summary>
    /// Manages the Actions tab in SettingsPanel
    /// Extracted from SettingsPanel.axaml.cs to reduce file size
    /// </summary>
    public class ActionsTabManager
    {
        private ActionEditor? actionEditor;
        private ActionData? selectedAction;
        private bool isCreatingNewAction = false;
        private Dictionary<string, Control> actionFormControls = new Dictionary<string, Control>();
        private Dictionary<string, ActionData> actionNameToAction = new Dictionary<string, ActionData>();
        
        private ListBox? actionsListBox;
        private Panel? actionFormPanel;
        private Button? createActionButton;
        private Button? deleteActionButton;
        private Action<string, bool>? showStatusMessage;

        public ActionsTabManager()
        {
            actionEditor = new ActionEditor();
        }

        public void Initialize(ListBox actionsListBox, Panel actionFormPanel, Button createActionButton, Button deleteActionButton, Action<string, bool> showStatusMessage)
        {
            this.actionsListBox = actionsListBox;
            this.actionFormPanel = actionFormPanel;
            this.createActionButton = createActionButton;
            this.deleteActionButton = deleteActionButton;
            this.showStatusMessage = showStatusMessage;
            
            LoadActionsList();
            createActionButton.Click += OnCreateActionClick;
            deleteActionButton.Click += OnDeleteActionClick;
            actionsListBox.SelectionChanged += OnActionSelectionChanged;
        }

        private void LoadActionsList()
        {
            if (actionEditor == null || actionsListBox == null) return;
            
            var actions = actionEditor.GetActions();
            actionNameToAction = actions.ToDictionary(a => a.Name, a => a);
            actionsListBox.ItemsSource = actions.Select(a => a.Name).ToList();
        }

        private void OnActionSelectionChanged(object? sender, SelectionChangedEventArgs e)
        {
            if (actionsListBox?.SelectedItem is string actionName && 
                actionNameToAction.TryGetValue(actionName, out var action))
            {
                selectedAction = action;
                isCreatingNewAction = false;
                LoadActionForm(action);
            }
        }

        private void OnCreateActionClick(object? sender, RoutedEventArgs e)
        {
            selectedAction = new ActionData
            {
                Name = "",
                Type = "Attack",
                TargetType = "SingleTarget",
                Cooldown = 0,
                Description = "",
                DamageMultiplier = 1.0,
                Length = 1.0,
                Tags = new List<string>()
            };
            isCreatingNewAction = true;
            if (actionsListBox != null) actionsListBox.SelectedItem = null;
            LoadActionForm(selectedAction);
        }

        private void OnDeleteActionClick(object? sender, RoutedEventArgs e)
        {
            if (actionEditor == null || selectedAction == null || isCreatingNewAction)
            {
                showStatusMessage?.Invoke("No action selected for deletion", false);
                return;
            }
            
            if (actionEditor.DeleteAction(selectedAction.Name))
            {
                showStatusMessage?.Invoke($"Action '{selectedAction.Name}' deleted successfully", true);
                LoadActionsList();
                actionFormPanel?.Children.Clear();
                selectedAction = null;
            }
            else
            {
                showStatusMessage?.Invoke($"Failed to delete action '{selectedAction.Name}'", false);
            }
        }

        private void LoadActionForm(ActionData action)
        {
            if (actionFormPanel == null) return;
            
            actionFormPanel.Children.Clear();
            actionFormControls.Clear();
            
            var title = new TextBlock
            {
                Text = isCreatingNewAction ? "Create New Action" : $"Edit Action: {action.Name}",
                FontSize = 18,
                FontWeight = FontWeight.Bold,
                Foreground = new SolidColorBrush(Color.FromRgb(255, 215, 0)),
                Margin = new Thickness(0, 0, 0, 15)
            };
            actionFormPanel.Children.Add(title);
            
            var basicSection = CreateFormSection("Basic Properties");
            actionFormPanel.Children.Add(basicSection);
            
            var basicStack = new StackPanel { Spacing = 10, Margin = new Thickness(10, 5, 0, 15) };
            basicSection.Child = basicStack;
            
            AddFormField(basicStack, "Name", action.Name, (value) => action.Name = value);
            
            var typeOptions = new[] { "Attack", "Heal", "Buff", "Debuff", "Spell", "Interact", "Move", "UseItem" };
            AddFormField(basicStack, "Type", action.Type, (value) => 
            {
                action.Type = value;
                UpdateTargetTypeOptions(action, value);
            }, typeOptions);
            
            AddFormField(basicStack, "TargetType", action.TargetType, (value) => action.TargetType = value, GetValidTargetTypes(action.Type));
            AddFormField(basicStack, "Description", action.Description, (value) => action.Description = value, isMultiline: true);
            
            var numericSection = CreateFormSection("Numeric Properties");
            actionFormPanel.Children.Add(numericSection);
            
            var numericStack = new StackPanel { Spacing = 10, Margin = new Thickness(10, 5, 0, 15) };
            numericSection.Child = numericStack;
            
            AddFormField(numericStack, "Cooldown", action.Cooldown.ToString(), (value) => { if (int.TryParse(value, out int v)) action.Cooldown = v; });
            AddFormField(numericStack, "DamageMultiplier", action.DamageMultiplier.ToString(), (value) => { if (double.TryParse(value, out double v)) action.DamageMultiplier = v; });
            AddFormField(numericStack, "Length", action.Length.ToString(), (value) => { if (double.TryParse(value, out double v)) action.Length = v; });
            
            var statusSection = CreateFormSection("Status Effects");
            actionFormPanel.Children.Add(statusSection);
            
            var statusStack = new StackPanel { Spacing = 10, Margin = new Thickness(10, 5, 0, 15) };
            statusSection.Child = statusStack;
            
            AddBooleanField(statusStack, "CausesBleed", action.CausesBleed, (value) => action.CausesBleed = value);
            AddBooleanField(statusStack, "CausesWeaken", action.CausesWeaken, (value) => action.CausesWeaken = value);
            AddBooleanField(statusStack, "CausesSlow", action.CausesSlow, (value) => action.CausesSlow = value);
            AddBooleanField(statusStack, "CausesPoison", action.CausesPoison, (value) => action.CausesPoison = value);
            AddBooleanField(statusStack, "CausesBurn", action.CausesBurn, (value) => action.CausesBurn = value);
            
            var comboSection = CreateFormSection("Combo Properties");
            actionFormPanel.Children.Add(comboSection);
            
            var comboStack = new StackPanel { Spacing = 10, Margin = new Thickness(10, 5, 0, 15) };
            comboSection.Child = comboStack;
            
            AddBooleanField(comboStack, "IsComboAction", action.IsComboAction, (value) => action.IsComboAction = value);
            AddFormField(comboStack, "ComboOrder", action.ComboOrder.ToString(), (value) => { if (int.TryParse(value, out int v)) action.ComboOrder = v; });
            AddFormField(comboStack, "ComboBonusAmount", action.ComboBonusAmount.ToString(), (value) => { if (int.TryParse(value, out int v)) action.ComboBonusAmount = v; });
            AddFormField(comboStack, "ComboBonusDuration", action.ComboBonusDuration.ToString(), (value) => { if (int.TryParse(value, out int v)) action.ComboBonusDuration = v; });
            
            var advancedSection = CreateFormSection("Advanced Mechanics");
            actionFormPanel.Children.Add(advancedSection);
            
            var advancedStack = new StackPanel { Spacing = 10, Margin = new Thickness(10, 5, 0, 15) };
            advancedSection.Child = advancedStack;
            
            AddFormField(advancedStack, "RollBonus", action.RollBonus.ToString(), (value) => { if (int.TryParse(value, out int v)) action.RollBonus = v; });
            AddFormField(advancedStack, "StatBonus", action.StatBonus.ToString(), (value) => { if (int.TryParse(value, out int v)) action.StatBonus = v; });
            AddFormField(advancedStack, "StatBonusType", action.StatBonusType, (value) => action.StatBonusType = value, 
                new[] { "", "Strength", "Agility", "Technique", "Intelligence" });
            AddFormField(advancedStack, "StatBonusDuration", action.StatBonusDuration.ToString(), (value) => { if (int.TryParse(value, out int v)) action.StatBonusDuration = v; });
            AddFormField(advancedStack, "MultiHitCount", action.MultiHitCount.ToString(), (value) => { if (int.TryParse(value, out int v) && v >= 1) action.MultiHitCount = v; });
            AddFormField(advancedStack, "SelfDamagePercent", action.SelfDamagePercent.ToString(), (value) => { if (int.TryParse(value, out int v)) action.SelfDamagePercent = v; });
            AddBooleanField(advancedStack, "SkipNextTurn", action.SkipNextTurn, (value) => action.SkipNextTurn = value);
            AddBooleanField(advancedStack, "RepeatLastAction", action.RepeatLastAction, (value) => action.RepeatLastAction = value);
            AddFormField(advancedStack, "EnemyRollPenalty", action.EnemyRollPenalty.ToString(), (value) => { if (int.TryParse(value, out int v)) action.EnemyRollPenalty = v; });
            AddFormField(advancedStack, "HealthThreshold", action.HealthThreshold.ToString("F2"), (value) => { if (double.TryParse(value, out double v) && v >= 0.0 && v <= 1.0) action.HealthThreshold = v; });
            AddFormField(advancedStack, "ConditionalDamageMultiplier", action.ConditionalDamageMultiplier.ToString("F2"), (value) => { if (double.TryParse(value, out double v)) action.ConditionalDamageMultiplier = v; });
            
            var tagsSection = CreateFormSection("Tags");
            actionFormPanel.Children.Add(tagsSection);
            
            var tagsStack = new StackPanel { Spacing = 10, Margin = new Thickness(10, 5, 0, 15) };
            tagsSection.Child = tagsStack;
            
            string tagsValue = action.Tags != null ? string.Join(", ", action.Tags) : "";
            AddFormField(tagsStack, "Tags", tagsValue, (value) => 
            {
                if (string.IsNullOrWhiteSpace(value))
                {
                    action.Tags = new List<string>();
                }
                else
                {
                    action.Tags = value.Split(',', StringSplitOptions.RemoveEmptyEntries)
                        .Select(t => t.Trim())
                        .Where(t => !string.IsNullOrWhiteSpace(t))
                        .ToList();
                }
            });
            
            var buttonStack = new StackPanel 
            { 
                Orientation = Orientation.Horizontal, 
                Spacing = 10, 
                Margin = new Thickness(0, 20, 0, 0),
                HorizontalAlignment = HorizontalAlignment.Right
            };
            
            var saveButton = new Button
            {
                Content = isCreatingNewAction ? "Create Action" : "Save Changes",
                Width = 150,
                Height = 35,
                Background = new SolidColorBrush(Color.FromRgb(76, 175, 80)),
                Foreground = new SolidColorBrush(Colors.White),
                BorderThickness = new Thickness(0),
                CornerRadius = new CornerRadius(3),
                Cursor = new Cursor(StandardCursorType.Hand)
            };
            saveButton.Click += (s, e) => SaveAction(action);
            
            var cancelButton = new Button
            {
                Content = "Cancel",
                Width = 100,
                Height = 35,
                Background = new SolidColorBrush(Color.FromRgb(117, 117, 117)),
                Foreground = new SolidColorBrush(Colors.White),
                BorderThickness = new Thickness(0),
                CornerRadius = new CornerRadius(3),
                Cursor = new Cursor(StandardCursorType.Hand)
            };
            cancelButton.Click += (s, e) => 
            {
                if (actionFormPanel != null) actionFormPanel.Children.Clear();
                if (actionsListBox != null) actionsListBox.SelectedItem = null;
                selectedAction = null;
            };
            
            buttonStack.Children.Add(cancelButton);
            buttonStack.Children.Add(saveButton);
            actionFormPanel.Children.Add(buttonStack);
        }

        private Border CreateFormSection(string title)
        {
            return new Border
            {
                Background = new SolidColorBrush(Color.FromRgb(40, 40, 60)),
                BorderBrush = new SolidColorBrush(Color.FromRgb(100, 100, 150)),
                BorderThickness = new Thickness(2),
                CornerRadius = new CornerRadius(5),
                Padding = new Thickness(15),
                Margin = new Thickness(0, 0, 0, 15)
            };
        }

        private string[] GetValidTargetTypes(string actionType)
        {
            return actionType switch
            {
                "Attack" => new[] { "SingleTarget", "SelfAndTarget" },
                "Spell" => new[] { "SingleTarget", "SelfAndTarget" },
                "Heal" => new[] { "Self", "SingleTarget" },
                "Buff" => new[] { "Self" },
                "Debuff" => new[] { "SingleTarget" },
                "Interact" => new[] { "Environment" },
                "Move" => new[] { "Self" },
                "UseItem" => new[] { "Self", "SingleTarget" },
                _ => new[] { "SingleTarget" }
            };
        }

        private void UpdateTargetTypeOptions(ActionData action, string newActionType)
        {
            if (actionFormControls.TryGetValue("TargetType", out var targetTypeControl) && targetTypeControl is ComboBox targetTypeComboBox)
            {
                var validTargetTypes = GetValidTargetTypes(newActionType);
                targetTypeComboBox.ItemsSource = validTargetTypes;
                
                if (!validTargetTypes.Contains(action.TargetType))
                {
                    action.TargetType = validTargetTypes[0];
                    targetTypeComboBox.SelectedItem = action.TargetType;
                }
                else
                {
                    targetTypeComboBox.SelectedItem = action.TargetType;
                }
            }
        }

        private void AddFormField(StackPanel parent, string label, string value, Action<string> setter, string[]? options = null, bool isMultiline = false)
        {
            var grid = new Grid();
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(200) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            
            var labelBlock = new TextBlock
            {
                Text = label + ":",
                FontSize = 15,
                Foreground = new SolidColorBrush(Colors.White),
                VerticalAlignment = VerticalAlignment.Center
            };
            Grid.SetColumn(labelBlock, 0);
            grid.Children.Add(labelBlock);
            
            Control inputControl;
            if (options != null && options.Length > 0)
            {
                var comboBox = new ComboBox
                {
                    ItemsSource = options,
                    SelectedItem = value,
                    FontSize = 14,
                    Background = new SolidColorBrush(Color.FromRgb(26, 26, 26)),
                    Foreground = new SolidColorBrush(Colors.White),
                    BorderBrush = new SolidColorBrush(Color.FromRgb(85, 85, 85))
                };
                comboBox.SelectionChanged += (s, e) => 
                {
                    if (comboBox.SelectedItem is string selected) setter(selected);
                };
                inputControl = comboBox;
            }
            else if (isMultiline)
            {
                var textBox = new TextBox
                {
                    Text = value,
                    FontSize = 14,
                    TextWrapping = TextWrapping.Wrap,
                    AcceptsReturn = true,
                    MinHeight = 80,
                    Background = new SolidColorBrush(Colors.White),
                    Foreground = new SolidColorBrush(Colors.Black),
                    BorderBrush = new SolidColorBrush(Color.FromRgb(85, 85, 85))
                };
                textBox.LostFocus += (s, e) => setter(textBox.Text ?? "");
                inputControl = textBox;
            }
            else
            {
                var textBox = new TextBox
                {
                    Text = value,
                    FontSize = 14,
                    Background = new SolidColorBrush(Colors.White),
                    Foreground = new SolidColorBrush(Colors.Black),
                    BorderBrush = new SolidColorBrush(Color.FromRgb(85, 85, 85))
                };
                textBox.LostFocus += (s, e) => setter(textBox.Text ?? "");
                inputControl = textBox;
            }
            
            Grid.SetColumn(inputControl, 1);
            grid.Children.Add(inputControl);
            parent.Children.Add(grid);
            
            actionFormControls[label] = inputControl;
        }

        private void AddBooleanField(StackPanel parent, string label, bool value, Action<bool> setter)
        {
            var grid = new Grid();
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(200) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            
            var labelBlock = new TextBlock
            {
                Text = label + ":",
                FontSize = 15,
                Foreground = new SolidColorBrush(Colors.White),
                VerticalAlignment = VerticalAlignment.Center
            };
            Grid.SetColumn(labelBlock, 0);
            grid.Children.Add(labelBlock);
            
            var checkBox = new CheckBox
            {
                IsChecked = value,
                FontSize = 14,
                VerticalAlignment = VerticalAlignment.Center
            };
            checkBox.IsCheckedChanged += (s, e) => 
            {
                if (checkBox.IsChecked.HasValue)
                    setter(checkBox.IsChecked.Value);
            };
            
            Grid.SetColumn(checkBox, 1);
            grid.Children.Add(checkBox);
            parent.Children.Add(grid);
            
            actionFormControls[label] = checkBox;
        }

        private void SaveAction(ActionData action)
        {
            if (actionEditor == null) return;
            
            foreach (var kvp in actionFormControls)
            {
                if (kvp.Value is TextBox textBox && textBox.IsFocused)
                {
                    textBox.Focusable = false;
                    textBox.Focusable = true;
                }
            }
            
            string? errorMessage = actionEditor.ValidateAction(action, isCreatingNewAction ? null : action.Name);
            if (errorMessage != null)
            {
                showStatusMessage?.Invoke(errorMessage, false);
                return;
            }
            
            bool success;
            if (isCreatingNewAction)
            {
                success = actionEditor.CreateAction(action);
                if (success)
                {
                    showStatusMessage?.Invoke($"Action '{action.Name}' created successfully", true);
                    isCreatingNewAction = false;
                }
                else
                {
                    showStatusMessage?.Invoke($"Failed to create action '{action.Name}'", false);
                    return;
                }
            }
            else
            {
                success = actionEditor.UpdateAction(action.Name, action);
                if (success)
                {
                    showStatusMessage?.Invoke($"Action '{action.Name}' updated successfully", true);
                }
                else
                {
                    showStatusMessage?.Invoke($"Failed to update action '{action.Name}'", false);
                    return;
                }
            }
            
            LoadActionsList();
        }
    }
}

