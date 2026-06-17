using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace RPGGame.UI.Avalonia.Components
{
    public partial class SettingsFieldRow : UserControl
    {
        public static readonly StyledProperty<string> LabelProperty =
            AvaloniaProperty.Register<SettingsFieldRow, string>(nameof(Label), string.Empty);

        public static new readonly StyledProperty<Control?> ContentProperty =
            AvaloniaProperty.Register<SettingsFieldRow, Control?>(nameof(Content));

        public string Label
        {
            get => GetValue(LabelProperty);
            set => SetValue(LabelProperty, value);
        }

        public new Control? Content
        {
            get => GetValue(ContentProperty);
            set => SetValue(ContentProperty, value);
        }

        public SettingsFieldRow()
        {
            InitializeComponent();
            LabelTextBlock.Bind(TextBlock.TextProperty, this.GetObservable(LabelProperty));
            ContentProperty.Changed.AddClassHandler<SettingsFieldRow>((row, _) =>
            {
                if (row.FieldContent != null)
                    row.FieldContent.Content = row.Content;
            });
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }
    }
}
