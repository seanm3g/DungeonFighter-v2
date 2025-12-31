using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Avalonia.Media;
using System;

namespace RPGGame.UI.Avalonia.Components
{
    public partial class SettingsSection : UserControl
    {
        public static readonly StyledProperty<string> TitleProperty =
            AvaloniaProperty.Register<SettingsSection, string>(nameof(Title), "Section Title");

        public static readonly StyledProperty<bool> IsExpandedProperty =
            AvaloniaProperty.Register<SettingsSection, bool>(nameof(IsExpanded), true);

        public static new readonly StyledProperty<Control> ContentProperty =
            AvaloniaProperty.Register<SettingsSection, Control>(nameof(Content));

        public string Title
        {
            get => GetValue(TitleProperty);
            set => SetValue(TitleProperty, value);
        }

        public bool IsExpanded
        {
            get => GetValue(IsExpandedProperty);
            set
            {
                SetValue(IsExpandedProperty, value);
                UpdateContentVisibility();
            }
        }

        public new Control Content
        {
            get => GetValue(ContentProperty);
            set
            {
                SetValue(ContentProperty, value);
                if (ContentArea != null && value != null)
                {
                    ContentArea.Content = value;
                }
            }
        }

        public SettingsSection()
        {
            InitializeComponent();
            
            // Bind title
            HeaderButton.Bind(Button.ContentProperty, this.GetObservable(TitleProperty));
            
            // Handle expand/collapse
            HeaderButton.Click += (s, e) =>
            {
                IsExpanded = !IsExpanded;
            };
            
            // Watch for content changes using property changed
            ContentProperty.Changed.AddClassHandler<SettingsSection>((x, e) =>
            {
                if (x.ContentArea != null && x.Content != null)
                {
                    x.ContentArea.Content = x.Content;
                }
            });
            
            // Watch for expanded state changes
            IsExpandedProperty.Changed.AddClassHandler<SettingsSection>((x, e) =>
            {
                x.UpdateContentVisibility();
            });
            
            UpdateContentVisibility();
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }

        private void UpdateContentVisibility()
        {
            if (ContentArea != null)
            {
                ContentArea.IsVisible = IsExpanded;
            }
        }
    }
}

