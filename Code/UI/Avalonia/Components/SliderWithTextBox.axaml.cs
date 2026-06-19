using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Avalonia.Media;
using RPGGame.UI.Avalonia.Resources;
using Avalonia.Threading;

namespace RPGGame.UI.Avalonia.Components
{
    /// <summary>
    /// Slider + numeric text box composite for simple static XAML fields.
    /// Tuning panels use <see cref="Settings.ViewModels.CombatTuningParameterViewModel"/> with native Slider/TextBox bindings instead.
    /// </summary>
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
                SetValue(ValueProperty, value);
                PushDisplayTextForValue(value);
            }
        }

        /// <summary>Re-applies slider position and text box from <see cref="Value"/> (for programmatic hosts).</summary>
        public void SyncDisplayFromValue()
        {
            if (Minimum > Maximum)
                return;

            double clamped = Math.Clamp(Value, Minimum, Maximum);
            if (ValueSlider != null)
            {
                _isSyncingFromSlider = true;
                try
                {
                    ValueSlider.Value = clamped;
                }
                finally
                {
                    _isSyncingFromSlider = false;
                }
            }

            PushDisplayTextForValue(clamped);
        }

        public double TickFrequency
        {
            get => GetValue(TickFrequencyProperty);
            set => SetValue(TickFrequencyProperty, value);
        }

        public Slider Slider => ValueSlider;
        public TextBox TextBox => ValueTextBox;

        private bool _isSyncingFromSlider;
        private bool _bindingsInitialized;
        private int _textBoxSyncAttempts;

        public SliderWithTextBox()
        {
            InitializeComponent();

            LabelForegroundProperty.Changed.AddClassHandler<SliderWithTextBox>((o, _) => o.ApplyLabelForeground());
            ShowLabelProperty.Changed.AddClassHandler<SliderWithTextBox>((o, _) => o.ApplyShowLabel());
            TextBoxForegroundProperty.Changed.AddClassHandler<SliderWithTextBox>((o, _) => o.ApplyTextBoxChrome());
            TextBoxBackgroundBrushProperty.Changed.AddClassHandler<SliderWithTextBox>((o, _) => o.ApplyTextBoxChrome());

            this.Loaded += OnLoaded;
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

        private void PushDisplayTextForValue(double value)
        {
            if (Minimum > Maximum)
                return;

            if (ValueTextBox != null)
                UpdateTextBoxFromValue(value);
            else
                ScheduleTextBoxSync();
        }

        private void ScheduleTextBoxSync()
        {
            _textBoxSyncAttempts = 0;
            TryScheduleTextBoxSync(DispatcherPriority.Loaded);
        }

        private void TryScheduleTextBoxSync(DispatcherPriority priority)
        {
            Dispatcher.UIThread.Post(() =>
            {
                if (ValueTextBox != null)
                {
                    UpdateTextBoxFromValue(Value);
                    return;
                }

                _textBoxSyncAttempts++;
                if (_textBoxSyncAttempts < 3)
                    TryScheduleTextBoxSync(DispatcherPriority.Background);
            }, priority);
        }

        private void ApplyTextBoxChrome()
        {
            if (ValueTextBox == null) return;
            SettingsInputApplier.ApplyTextBox(ValueTextBox);
            ValueTextBox.Foreground = TextBoxForeground ?? SettingsThemeBrushes.TextPrimary;
            ValueTextBox.CaretBrush = TextBoxForeground ?? SettingsThemeBrushes.TextPrimary;
            if (TextBoxBackgroundBrush != null)
                ValueTextBox.Background = TextBoxBackgroundBrush;
        }

        /// <summary>Clamps then formats a value for display (shared by control and tests).</summary>
        public static string ClampAndFormatForDisplay(double value, double minimum, double maximum, double tickFrequency)
        {
            if (minimum > maximum)
                return string.Empty;

            double clamped = Math.Clamp(value, minimum, maximum);
            return FormatValueForDisplay(clamped, minimum, maximum, tickFrequency);
        }

        /// <summary>Formats a value for display in the text box (shared by control and tests).</summary>
        public static string FormatValueForDisplay(double value, double minimum, double maximum, double tickFrequency)
        {
            bool asInteger = tickFrequency >= 1.0 && maximum - minimum >= 1.0;
            if (asInteger || maximum >= 100)
                return ((int)Math.Round(value)).ToString();
            return value.ToString("F2");
        }

        private void UpdateTextBoxFromValue(double value)
        {
            if (ValueTextBox != null)
                ValueTextBox.Text = FormatValueForDisplay(value, Minimum, Maximum, TickFrequency);
        }

        private void OnLoaded(object? sender, RoutedEventArgs e)
        {
            if (LabelTextBlock != null)
            {
                LabelTextBlock.IsVisible = ShowLabel;
                if (ShowLabel)
                {
                    LabelTextBlock.Bind(TextBlock.TextProperty, this.GetObservable(LabelProperty));
                    ApplyLabelForeground();
                    Dispatcher.UIThread.Post(ApplyLabelForeground, DispatcherPriority.Loaded);
                }
            }

            if (ValueSlider != null && !_bindingsInitialized)
            {
                _bindingsInitialized = true;
                _isSyncingFromSlider = true;
                try
                {
                    ValueSlider.Bind(Slider.MinimumProperty, this.GetObservable(MinimumProperty));
                    ValueSlider.Bind(Slider.MaximumProperty, this.GetObservable(MaximumProperty));
                    ValueSlider.Bind(Slider.TickFrequencyProperty, this.GetObservable(TickFrequencyProperty));
                }
                finally
                {
                    _isSyncingFromSlider = false;
                }

                ValueSlider.ValueChanged += (_, _) =>
                {
                    if (_isSyncingFromSlider)
                        return;

                    double clamped = Math.Clamp(ValueSlider.Value, Minimum, Maximum);
                    _isSyncingFromSlider = true;
                    try
                    {
                        SetValue(ValueProperty, clamped);
                        UpdateTextBoxFromValue(clamped);
                    }
                    finally
                    {
                        _isSyncingFromSlider = false;
                    }
                };
            }

            ApplyTextBoxChrome();
            Dispatcher.UIThread.Post(ApplyTextBoxChrome, DispatcherPriority.Loaded);

            if (ValueTextBox != null)
            {
                ValueTextBox.LostFocus += (_, _) =>
                {
                    if (double.TryParse(ValueTextBox.Text, out double parsed))
                    {
                        Value = Math.Clamp(parsed, Minimum, Maximum);
                        SyncDisplayFromValue();
                    }
                    else
                    {
                        SyncDisplayFromValue();
                    }
                };
            }

            SyncDisplayFromValue();
            Dispatcher.UIThread.Post(SyncDisplayFromValue, DispatcherPriority.Background);
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }
    }
}
