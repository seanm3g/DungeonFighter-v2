using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.Media;
using RPGGame.Editors;
using System;
using System.Collections.Generic;
using System.Linq;

namespace RPGGame.UI.Avalonia.Builders
{
    /// <summary>
    /// Builds form controls for action editing
    /// </summary>
    public class ActionFormBuilder
    {
        private readonly Dictionary<string, Control> actionFormControls;
        private readonly Action<string, bool>? showStatusMessage;

        public ActionFormBuilder(Dictionary<string, Control> actionFormControls, Action<string, bool>? showStatusMessage)
        {
            this.actionFormControls = actionFormControls;
            this.showStatusMessage = showStatusMessage;
        }

        public void BuildForm(Panel actionFormPanel, ActionData action, bool isCreatingNewAction)
        {
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
            
            BuildBasicSection(actionFormPanel, action);
            BuildNumericSection(actionFormPanel, action);
            BuildStatusSection(actionFormPanel, action);
            BuildComboSection(actionFormPanel, action);
            BuildAdvancedSection(actionFormPanel, action);
            BuildTagsSection(actionFormPanel, action);
            BuildButtons(actionFormPanel, action, isCreatingNewAction);
        }

        private void BuildBasicSection(Panel parent, ActionData action)
        {
            var section = CreateFormSection("Basic Properties");
            parent.Children.Add(section);
            
            var stack = new StackPanel { Spacing = 10, Margin = new Thickness(10, 5, 0, 15) };
            section.Child = stack;
            
            AddFormField(stack, "Name", action.Name, (value) => action.Name = value);
            
            var typeOptions = new[] { "Attack", "Heal", "Buff", "Debuff", "Spell", "Interact", "Move", "UseItem" };
            AddFormField(stack, "Type", action.Type, (value) => 
            {
                action.Type = value;
                UpdateTargetTypeOptions(action, value);
            }, typeOptions);
            
            AddFormField(stack, "TargetType", action.TargetType, (value) => action.TargetType = value, GetValidTargetTypes(action.Type));
            AddFormField(stack, "Description", action.Description, (value) => action.Description = value, isMultiline: true);
        }

        private void BuildNumericSection(Panel parent, ActionData action)
        {
            var section = CreateFormSection("Numeric Properties");
            parent.Children.Add(section);
            
            var stack = new StackPanel { Spacing = 10, Margin = new Thickness(10, 5, 0, 15) };
            section.Child = stack;
            
            AddFormField(stack, "Cooldown", action.Cooldown.ToString(), (value) => { if (int.TryParse(value, out int v)) action.Cooldown = v; });
            AddFormField(stack, "DamageMultiplier", action.DamageMultiplier.ToString(), (value) => { if (double.TryParse(value, out double v)) action.DamageMultiplier = v; });
            AddFormField(stack, "Length", action.Length.ToString(), (value) => { if (double.TryParse(value, out double v)) action.Length = v; });
        }

        private void BuildStatusSection(Panel parent, ActionData action)
        {
            var section = CreateFormSection("Status Effects");
            parent.Children.Add(section);
            
            var stack = new StackPanel { Spacing = 10, Margin = new Thickness(10, 5, 0, 15) };
            section.Child = stack;
            
            AddBooleanField(stack, "CausesBleed", action.CausesBleed, (value) => action.CausesBleed = value);
            AddBooleanField(stack, "CausesWeaken", action.CausesWeaken, (value) => action.CausesWeaken = value);
            AddBooleanField(stack, "CausesSlow", action.CausesSlow, (value) => action.CausesSlow = value);
            AddBooleanField(stack, "CausesPoison", action.CausesPoison, (value) => action.CausesPoison = value);
            AddBooleanField(stack, "CausesBurn", action.CausesBurn, (value) => action.CausesBurn = value);
        }

        private void BuildComboSection(Panel parent, ActionData action)
        {
            var section = CreateFormSection("Combo Properties");
            parent.Children.Add(section);
            
            var stack = new StackPanel { Spacing = 10, Margin = new Thickness(10, 5, 0, 15) };
            section.Child = stack;
            
            AddBooleanField(stack, "IsComboAction", action.IsComboAction, (value) => action.IsComboAction = value);
            AddFormField(stack, "ComboOrder", action.ComboOrder.ToString(), (value) => { if (int.TryParse(value, out int v)) action.ComboOrder = v; });
            AddFormField(stack, "ComboBonusAmount", action.ComboBonusAmount.ToString(), (value) => { if (int.TryParse(value, out int v)) action.ComboBonusAmount = v; });
            AddFormField(stack, "ComboBonusDuration", action.ComboBonusDuration.ToString(), (value) => { if (int.TryParse(value, out int v)) action.ComboBonusDuration = v; });
        }

        private void BuildAdvancedSection(Panel parent, ActionData action)
        {
            var section = CreateFormSection("Advanced Mechanics");
            parent.Children.Add(section);
            
            var stack = new StackPanel { Spacing = 10, Margin = new Thickness(10, 5, 0, 15) };
            section.Child = stack;
            
            AddFormField(stack, "RollBonus", action.RollBonus.ToString(), (value) => { if (int.TryParse(value, out int v)) action.RollBonus = v; });
            AddFormField(stack, "StatBonus", action.StatBonus.ToString(), (value) => { if (int.TryParse(value, out int v)) action.StatBonus = v; });
            AddFormField(stack, "StatBonusType", action.StatBonusType, (value) => action.StatBonusType = value, 
                new[] { "", "Strength", "Agility", "Technique", "Intelligence" });
            AddFormField(stack, "StatBonusDuration", action.StatBonusDuration.ToString(), (value) => { if (int.TryParse(value, out int v)) action.StatBonusDuration = v; });
            AddFormField(stack, "MultiHitCount", action.MultiHitCount.ToString(), (value) => { if (int.TryParse(value, out int v) && v >= 1) action.MultiHitCount = v; });
            AddFormField(stack, "SelfDamagePercent", action.SelfDamagePercent.ToString(), (value) => { if (int.TryParse(value, out int v)) action.SelfDamagePercent = v; });
            AddBooleanField(stack, "SkipNextTurn", action.SkipNextTurn, (value) => action.SkipNextTurn = value);
            AddBooleanField(stack, "RepeatLastAction", action.RepeatLastAction, (value) => action.RepeatLastAction = value);
            AddFormField(stack, "EnemyRollPenalty", action.EnemyRollPenalty.ToString(), (value) => { if (int.TryParse(value, out int v)) action.EnemyRollPenalty = v; });
            AddFormField(stack, "HealthThreshold", action.HealthThreshold.ToString("F2"), (value) => { if (double.TryParse(value, out double v) && v >= 0.0 && v <= 1.0) action.HealthThreshold = v; });
            AddFormField(stack, "ConditionalDamageMultiplier", action.ConditionalDamageMultiplier.ToString("F2"), (value) => { if (double.TryParse(value, out double v)) action.ConditionalDamageMultiplier = v; });
        }

        private void BuildTagsSection(Panel parent, ActionData action)
        {
            var section = CreateFormSection("Tags");
            parent.Children.Add(section);
            
            var stack = new StackPanel { Spacing = 10, Margin = new Thickness(10, 5, 0, 15) };
            section.Child = stack;
            
            string tagsValue = action.Tags != null ? string.Join(", ", action.Tags) : "";
            AddFormField(stack, "Tags", tagsValue, (value) => 
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
        }

        private void BuildButtons(Panel parent, ActionData action, bool isCreatingNewAction)
        {
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
            saveButton.Click += (s, e) => OnSaveAction(action, isCreatingNewAction);
            
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
            cancelButton.Click += (s, e) => OnCancelAction();
            
            buttonStack.Children.Add(cancelButton);
            buttonStack.Children.Add(saveButton);
            parent.Children.Add(buttonStack);
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

        public event System.Action<ActionData, bool>? SaveActionRequested;
        public event System.Action? CancelActionRequested;

        private void OnSaveAction(ActionData action, bool isCreatingNewAction)
        {
            SaveActionRequested?.Invoke(action, isCreatingNewAction);
        }

        private void OnCancelAction()
        {
            CancelActionRequested?.Invoke();
        }
    }
}

