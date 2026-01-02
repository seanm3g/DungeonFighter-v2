using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.Media;
using RPGGame;
using System;
using System.Collections.Generic;

namespace RPGGame.UI.Avalonia.Builders
{
    /// <summary>
    /// Builds form controls for status effect editing
    /// </summary>
    public class StatusEffectFormBuilder
    {
        private readonly Dictionary<string, Control> formControls;
        private readonly Action<string, bool>? showStatusMessage;

        public event Action<StatusEffectConfig, bool>? SaveStatusEffectRequested;
        public event System.Action? CancelStatusEffectRequested;

        public StatusEffectFormBuilder(Dictionary<string, Control> formControls, Action<string, bool>? showStatusMessage)
        {
            this.formControls = formControls;
            this.showStatusMessage = showStatusMessage;
        }

        public void BuildForm(Panel formPanel, string effectName, StatusEffectConfig effect, bool isCreatingNew)
        {
            formPanel.Children.Clear();
            formControls.Clear();
            
            var title = new TextBlock
            {
                Text = isCreatingNew ? "Create New Status Effect" : $"Edit Status Effect: {effectName}",
                FontSize = 18,
                FontWeight = FontWeight.Bold,
                Foreground = new SolidColorBrush(Color.FromRgb(255, 215, 0)),
                Margin = new Thickness(0, 0, 0, 15)
            };
            formPanel.Children.Add(title);
            
            // Name field (only for new effects or when editing)
            if (isCreatingNew)
            {
                var nameSection = CreateFormSection("Basic Properties");
                formPanel.Children.Add(nameSection);
                
                var nameStack = new StackPanel { Spacing = 10, Margin = new Thickness(10, 5, 0, 15) };
                nameSection.Child = nameStack;
                
                var nameField = new TextBox
                {
                    Text = effectName,
                    Watermark = "Enter status effect name (e.g., Bleed, Burn, Stun)",
                    Background = new SolidColorBrush(Color.FromRgb(40, 40, 40)),
                    Foreground = new SolidColorBrush(Color.FromRgb(255, 255, 255)),
                    BorderBrush = new SolidColorBrush(Color.FromRgb(100, 100, 100)),
                    Padding = new Thickness(8),
                    Margin = new Thickness(0, 5)
                };
                formControls["Name"] = nameField;
                nameStack.Children.Add(new TextBlock { Text = "Name:", Foreground = new SolidColorBrush(Color.FromRgb(200, 200, 200)), Margin = new Thickness(0, 0, 0, 3) });
                nameStack.Children.Add(nameField);
            }
            else
            {
                var nameDisplay = new TextBlock
                {
                    Text = $"Name: {effectName}",
                    FontSize = 14,
                    FontWeight = FontWeight.SemiBold,
                    Foreground = new SolidColorBrush(Color.FromRgb(200, 200, 200)),
                    Margin = new Thickness(0, 0, 0, 15)
                };
                formPanel.Children.Add(nameDisplay);
            }
            
            // Status Effect Properties
            var propertiesSection = CreateFormSection("Status Effect Properties");
            formPanel.Children.Add(propertiesSection);
            
            var propertiesStack = new StackPanel { Spacing = 10, Margin = new Thickness(10, 5, 0, 15) };
            propertiesSection.Child = propertiesStack;
            
            AddNumericField(propertiesStack, "DamagePerTick", effect.DamagePerTick.ToString(), 
                (value) => { if (int.TryParse(value, out int v) && v >= 0) effect.DamagePerTick = v; });
            
            AddNumericField(propertiesStack, "TickInterval", effect.TickInterval.ToString("F2"), 
                (value) => { if (double.TryParse(value, out double v) && v >= 0) effect.TickInterval = v; });
            
            AddNumericField(propertiesStack, "MaxStacks", effect.MaxStacks.ToString(), 
                (value) => { if (int.TryParse(value, out int v) && v >= 0) effect.MaxStacks = v; });
            
            AddNumericField(propertiesStack, "StacksPerApplication", effect.StacksPerApplication.ToString(), 
                (value) => { if (int.TryParse(value, out int v) && v >= 0) effect.StacksPerApplication = v; });
            
            AddNumericField(propertiesStack, "SpeedReduction", effect.SpeedReduction.ToString("F2"), 
                (value) => { if (double.TryParse(value, out double v) && v >= 0 && v <= 1) effect.SpeedReduction = v; });
            
            AddNumericField(propertiesStack, "Duration", effect.Duration.ToString("F2"), 
                (value) => { if (double.TryParse(value, out double v) && v >= 0) effect.Duration = v; });
            
            AddNumericField(propertiesStack, "SkipTurns", effect.SkipTurns.ToString(), 
                (value) => { if (int.TryParse(value, out int v) && v >= 0) effect.SkipTurns = v; });
            
            // Buttons
            BuildButtons(formPanel, isCreatingNew);
        }

        private void AddNumericField(StackPanel parent, string label, string value, Action<string> onValueChanged)
        {
            var labelBlock = new TextBlock
            {
                Text = label + ":",
                Foreground = new SolidColorBrush(Color.FromRgb(200, 200, 200)),
                Margin = new Thickness(0, 0, 0, 3)
            };
            parent.Children.Add(labelBlock);
            
            var textBox = new TextBox
            {
                Text = value,
                Background = new SolidColorBrush(Color.FromRgb(40, 40, 40)),
                Foreground = new SolidColorBrush(Color.FromRgb(255, 255, 255)),
                BorderBrush = new SolidColorBrush(Color.FromRgb(100, 100, 100)),
                Padding = new Thickness(8),
                Margin = new Thickness(0, 0, 0, 10)
            };
            
            textBox.LostFocus += (s, e) => onValueChanged(textBox.Text ?? "");
            textBox.KeyDown += (s, e) =>
            {
                if (e.Key == Key.Enter)
                {
                    onValueChanged(textBox.Text ?? "");
                    e.Handled = true;
                }
            };
            
            formControls[label] = textBox;
            parent.Children.Add(textBox);
        }

        private Border CreateFormSection(string title)
        {
            var border = new Border
            {
                Background = new SolidColorBrush(Color.FromRgb(30, 30, 30)),
                BorderBrush = new SolidColorBrush(Color.FromRgb(80, 80, 80)),
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(4),
                Padding = new Thickness(10),
                Margin = new Thickness(0, 0, 0, 15)
            };
            
            var sectionTitle = new TextBlock
            {
                Text = title,
                FontSize = 16,
                FontWeight = FontWeight.Bold,
                Foreground = new SolidColorBrush(Color.FromRgb(255, 215, 0)),
                Margin = new Thickness(0, 0, 0, 10)
            };
            
            var stack = new StackPanel();
            stack.Children.Add(sectionTitle);
            border.Child = stack;
            
            return border;
        }

        private void BuildButtons(Panel parent, bool isCreatingNew)
        {
            var buttonStack = new StackPanel 
            { 
                Orientation = Orientation.Horizontal, 
                Spacing = 10,
                HorizontalAlignment = HorizontalAlignment.Right,
                Margin = new Thickness(0, 20, 0, 0)
            };
            
            var saveButton = new Button
            {
                Content = isCreatingNew ? "Create" : "Save",
                Width = 100,
                Height = 35,
                Background = new SolidColorBrush(Color.FromRgb(40, 120, 40)),
                Foreground = new SolidColorBrush(Color.FromRgb(255, 255, 255)),
                BorderThickness = new Thickness(0),
                CornerRadius = new CornerRadius(4),
                FontSize = 14,
                FontWeight = FontWeight.SemiBold
            };
            
            saveButton.Click += (s, e) =>
            {
                // Get the effect config from form controls
                var effect = new StatusEffectConfig();
                
                if (formControls.TryGetValue("DamagePerTick", out var control) && control is TextBox tb1)
                {
                    if (int.TryParse(tb1.Text, out int dpt)) effect.DamagePerTick = dpt;
                }
                
                if (formControls.TryGetValue("TickInterval", out var control2) && control2 is TextBox tb2)
                {
                    if (double.TryParse(tb2.Text, out double ti)) effect.TickInterval = ti;
                }
                
                if (formControls.TryGetValue("MaxStacks", out var control3) && control3 is TextBox tb3)
                {
                    if (int.TryParse(tb3.Text, out int ms)) effect.MaxStacks = ms;
                }
                
                if (formControls.TryGetValue("StacksPerApplication", out var control4) && control4 is TextBox tb4)
                {
                    if (int.TryParse(tb4.Text, out int spa)) effect.StacksPerApplication = spa;
                }
                
                if (formControls.TryGetValue("SpeedReduction", out var control5) && control5 is TextBox tb5)
                {
                    if (double.TryParse(tb5.Text, out double sr)) effect.SpeedReduction = sr;
                }
                
                if (formControls.TryGetValue("Duration", out var control6) && control6 is TextBox tb6)
                {
                    if (double.TryParse(tb6.Text, out double d)) effect.Duration = d;
                }
                
                if (formControls.TryGetValue("SkipTurns", out var control7) && control7 is TextBox tb7)
                {
                    if (int.TryParse(tb7.Text, out int st)) effect.SkipTurns = st;
                }
                
                SaveStatusEffectRequested?.Invoke(effect, isCreatingNew);
            };
            
            var cancelButton = new Button
            {
                Content = "Cancel",
                Width = 100,
                Height = 35,
                Background = new SolidColorBrush(Color.FromRgb(120, 40, 40)),
                Foreground = new SolidColorBrush(Color.FromRgb(255, 255, 255)),
                BorderThickness = new Thickness(0),
                CornerRadius = new CornerRadius(4),
                FontSize = 14,
                FontWeight = FontWeight.SemiBold
            };
            
            cancelButton.Click += (s, e) => CancelStatusEffectRequested?.Invoke();
            
            buttonStack.Children.Add(saveButton);
            buttonStack.Children.Add(cancelButton);
            parent.Children.Add(buttonStack);
        }
    }
}

