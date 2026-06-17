using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Avalonia.Media;
using RPGGame.UI.Avalonia.Resources;
using Avalonia.Threading;

namespace RPGGame.UI.Avalonia.Components
{
    public partial class SliderWithTextBox : UserControl
    {
        /// <summary>Label color; defaults to black for light strips (e.g. Difficulty panel). Set to a light brush on dark panels so Fluent theme cannot wash the label out.</summary>
        public static readonly StyledProperty<IBrush?> LabelForegroundProperty =
            AvaloniaProperty.Register<SliderWithTextBox, IBrush?>(nameof(LabelForeground), SettingsThemeBrushes.TextPrimary);

        /// <summary>When false, the built-in label row is hidden (use an external <see cref="TextBlock"/> for the caption).</summary>
        public static readonly StyledProperty<bool> ShowLabelProperty =
            AvaloniaProperty.Register<SliderWithTextBox, bool>(nameof(ShowLabel), true);

        public static readonly StyledProperty<string> LabelProperty =
            AvaloniaProperty.Register<SliderWithTextBox, string>(nameof(Label), "Label:");

        public IBrush? LabelForeground
        {
            get => GetValue(LabelForegroundProperty);
            set => SetValue(LabelForegroundProperty, value);
        }

        public bool ShowLabel
        {
            get => GetValue(ShowLabelProperty);
            set => SetValue(ShowLabelProperty, value);
        }

        public static readonly StyledProperty<double> MinimumProperty =
            AvaloniaProperty.Register<SliderWithTextBox, double>(nameof(Minimum), 0.0);

        public static readonly StyledProperty<double> MaximumProperty =
            AvaloniaProperty.Register<SliderWithTextBox, double>(nameof(Maximum), 1.0);

        public static readonly StyledProperty<double> ValueProperty =
            AvaloniaProperty.Register<SliderWithTextBox, double>(nameof(Value), 0.0);

        public static readonly StyledProperty<double> TickFrequencyProperty =
            AvaloniaProperty.Register<SliderWithTextBox, double>(nameof(TickFrequency), 0.1);

        /// <summary>TextBox foreground on dark settings panels (Fluent theme otherwise hides values).</summary>
        public static readonly StyledProperty<IBrush?> TextBoxForegroundProperty =
            AvaloniaProperty.Register<SliderWithTextBox, IBrush?>(nameof(TextBoxForeground), SettingsThemeBrushes.TextPrimary);

        public static readonly StyledProperty<IBrush?> TextBoxBackgroundBrushProperty =
            AvaloniaProperty.Register<SliderWithTextBox, IBrush?>(nameof(TextBoxBackgroundBrush), SettingsThemeBrushes.InputBackground);

        public IBrush? TextBoxForeground
        {
            get => GetValue(TextBoxForegroundProperty);
            set => SetValue(TextBoxForegroundProperty, value);
        }

        public IBrush? TextBoxBackgroundBrush
        {
            get => GetValue(TextBoxBackgroundBrushProperty);
            set => SetValue(TextBoxBackgroundBrushProperty, value);
        }

        public string Label
        {
            get => GetValue(LabelProperty);
            set => SetValue(LabelProperty, value);
        }

        public double Minimum
        {
            get => GetValue(MinimumProperty);
            set => SetValue(MinimumProperty, value);
        }

        public double Maximum
        {
            get => GetValue(MaximumProperty);
            set => SetValue(MaximumProperty, value);
        }

        public double Value
        {
            get => GetValue(ValueProperty);
            set
            {
                var oldValue = GetValue(ValueProperty);
                SetValue(ValueProperty, value);
                
                // Immediately update text box if it exists, even if component isn't fully loaded
                // This ensures the text box shows the value when set programmatically
                // Use Dispatcher to ensure the control is available
                if (ValueTextBox != null)
                {
                    UpdateTextBoxFromValue(value);
                }
                else
                {
                    ScheduleTextBoxSync();
                }
            }
        }

        /// <summary>Re-applies slider position and text box from <see cref="Value"/> (for programmatic hosts).</summary>
        public void SyncDisplayFromValue()
        {
            if (ValueSlider != null)
                ValueSlider.Value = Math.Clamp(Value, Minimum, Maximum);
            if (ValueTextBox != null)
                UpdateTextBoxFromValue(Value);
            else
                ScheduleTextBoxSync();
        }

        public double TickFrequency
        {
            get => GetValue(TickFrequencyProperty);
            set => SetValue(TickFrequencyProperty, value);
        }

        public Slider Slider => ValueSlider;
        public TextBox TextBox => ValueTextBox;

        private bool _isLoaded = false;

        public SliderWithTextBox()
        {
            InitializeComponent();

            LabelForegroundProperty.Changed.AddClassHandler<SliderWithTextBox>((o, _) => o.ApplyLabelForeground());
            ShowLabelProperty.Changed.AddClassHandler<SliderWithTextBox>((o, _) => o.ApplyShowLabel());
            TextBoxForegroundProperty.Changed.AddClassHandler<SliderWithTextBox>((o, _) => o.ApplyTextBoxChrome());
            TextBoxBackgroundBrushProperty.Changed.AddClassHandler<SliderWithTextBox>((o, _) => o.ApplyTextBoxChrome());
            
            // Wait for controls to be loaded before setting up bindings
            this.Loaded += OnLoaded;
            
            // Listen to Value property changes to update text box
            // Use PropertyChanged event instead of Subscribe (which requires System.Reactive)
            this.PropertyChanged += (s, e) =>
            {
                if (e.Property == ValueProperty)
                {
                    // Update text box if component is loaded, otherwise it will be updated in OnLoaded
                    if (_isLoaded && ValueTextBox != null)
                    {
                        UpdateTextBoxFromValue(Value);
                    }
                }
            };
        }

        private void ApplyLabelForeground()
        {
            if (LabelTextBlock == null) return;
            if (!ShowLabel)
                return;
            LabelTextBlock.Foreground = LabelForeground ?? SettingsThemeBrushes.TextPrimary;
        }

        private void ApplyShowLabel()
        {
            if (LabelTextBlock == null) return;
            LabelTextBlock.IsVisible = ShowLabel;
            if (ShowLabel)
                ApplyLabelForeground();
        }
        
        private void ScheduleTextBoxSync()
        {
            Dispatcher.UIThread.Post(() =>
            {
                if (ValueTextBox != null)
                    UpdateTextBoxFromValue(Value);
            }, DispatcherPriority.Loaded);
        }

        private void ApplyTextBoxChrome()
        {
            if (ValueTextBox == null) return;
            ValueTextBox.Foreground = TextBoxForeground ?? SettingsThemeBrushes.TextPrimary;
            ValueTextBox.CaretBrush = TextBoxForeground ?? SettingsThemeBrushes.TextPrimary;
            if (TextBoxBackgroundBrush != null)
                ValueTextBox.Background = TextBoxBackgroundBrush;
        }

        private void UpdateTextBoxFromValue(double value)
        {
            if (ValueTextBox != null)
            {
                if (ValueKind == SliderValueFormat.Integer || Maximum >= 100)
                    ValueTextBox.Text = ((int)Math.Round(value)).ToString();
                else
                    ValueTextBox.Text = value.ToString("F2");
            }
        }

        private enum SliderValueFormat
        {
            Auto,
            Integer
        }

        private SliderValueFormat ValueKind =>
            TickFrequency >= 1.0 && Maximum - Minimum >= 1.0 ? SliderValueFormat.Integer : SliderValueFormat.Auto;
        
        private void OnLoaded(object? sender, RoutedEventArgs e)
        {
            _isLoaded = true;
            
            // Bind label text (skip when host draws its own caption — avoids theme fighting the inner TextBlock)
            if (LabelTextBlock != null)
            {
                LabelTextBlock.IsVisible = ShowLabel;
                if (ShowLabel)
                {
                    LabelTextBlock.Bind(TextBlock.TextProperty, this.GetObservable(LabelProperty));
                    ApplyLabelForeground();
                    // Fluent can re-apply TextBlock brushes after Loaded; set again once layout has run.
                    Dispatcher.UIThread.Post(() =>
                    {
                        ApplyLabelForeground();
                    }, DispatcherPriority.Loaded);
                }
            }
            
            // Bind slider properties
            if (ValueSlider != null)
            {
                ValueSlider.Bind(Slider.MinimumProperty, this.GetObservable(MinimumProperty));
                ValueSlider.Bind(Slider.MaximumProperty, this.GetObservable(MaximumProperty));
                ValueSlider.Bind(Slider.ValueProperty, this.GetObservable(ValueProperty));
                ValueSlider.Bind(Slider.TickFrequencyProperty, this.GetObservable(TickFrequencyProperty));
                
                // Sync slider and textbox values when slider changes
                // Use integer format if maximum is >= 100 (likely integer values), otherwise use decimal format
                ValueSlider.ValueChanged += (s, ev) =>
                {
                    UpdateTextBoxFromValue(ValueSlider.Value);
                };
                
                // Initialize text box with current value (use Value property as source of truth)
                // The slider will be bound to this value, so we update the text box to match
                UpdateTextBoxFromValue(Value);
            }
            else
            {
                // Fallback: initialize text box with current Value property if slider isn't available yet
                UpdateTextBoxFromValue(Value);
            }
            
            ApplyTextBoxChrome();
            Dispatcher.UIThread.Post(ApplyTextBoxChrome, DispatcherPriority.Loaded);

            // Setup textbox validation
            if (ValueTextBox != null)
            {
                ValueTextBox.LostFocus += (s, ev) =>
                {
                    if (ValueSlider != null && double.TryParse(ValueTextBox.Text, out double value))
                    {
                        value = System.Math.Max(Minimum, System.Math.Min(Maximum, value));
                        ValueSlider.Value = value;
                        // Text box will be updated via ValueChanged event or property change handler
                    }
                    else if (ValueSlider != null)
                    {
                        // Restore text box to current slider value if input is invalid
                        UpdateTextBoxFromValue(ValueSlider.Value);
                    }
                };
            }
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }
    }
}

