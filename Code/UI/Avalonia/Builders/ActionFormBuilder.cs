using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.Media;
using RPGGame.Editors;
using RPGGame.Data;
using RPGGame.UI.Avalonia.Resources;
using System;
using System.Collections.Generic;

namespace RPGGame.UI.Avalonia.Builders
{
    /// <summary>
    /// Builds form controls for action editing. Orchestrates section builders and buttons.
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

        /// <summary>Normalizes legacy cadence values to canonical. Delegates to <see cref="ActionFormOptions.NormalizeCadence"/>.</summary>
        public static string NormalizeCadence(string? raw) => ActionFormOptions.NormalizeCadence(raw);

        public void BuildForm(Panel actionFormPanel, ActionData action, bool isCreatingNewAction)
        {
            actionFormPanel.Children.Clear();
            actionFormControls.Clear();

            var title = new TextBlock
            {
                Text = isCreatingNewAction ? "New Action (save with global Save)" : $"Edit Action: {action.Name}",
                FontSize = 18,
                FontWeight = FontWeight.Bold,
                Foreground = SettingsThemeBrushes.TextTitle,
                Margin = new Thickness(0, 0, 0, 15)
            };
            actionFormPanel.Children.Add(title);

            var factory = new ActionFormControlFactory(actionFormControls);
            var ctx = new ActionFormBuildContext(actionFormControls, factory);
            var sections = new ActionFormSectionBuilders(ctx);

            sections.BuildBasicSection(actionFormPanel, action);
            sections.BuildTagsSection(actionFormPanel, action);
            sections.BuildCadenceMechanicsSection(actionFormPanel, action);
            LastCadenceBlocks = ctx.CadenceBlocks;
            sections.BuildAdvancedExpanderSection(actionFormPanel, action);
            BuildButtons(actionFormPanel);
        }

        private void BuildButtons(Panel parent)
        {
            var buttonStack = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                Spacing = 10,
                Margin = new Thickness(0, 20, 0, 0),
                HorizontalAlignment = HorizontalAlignment.Right
            };

            var cancelButton = new Button
            {
                Content = "Cancel",
                Width = 100,
                Height = 35,
                Background = SettingsThemeBrushes.SidebarItem,
                Foreground = SettingsThemeBrushes.TextPrimary,
                BorderThickness = new Thickness(0),
                CornerRadius = new CornerRadius(3),
                Cursor = new Cursor(StandardCursorType.Hand)
            };
            cancelButton.Click += (s, e) => OnCancelAction();
            buttonStack.Children.Add(cancelButton);

            // No per-action Save/Create button: only the global Settings Save persists changes (applies to Actions and all other settings panels).
            parent.Children.Add(buttonStack);
        }

        public event System.Action? CancelActionRequested;

        /// <summary>Cadence blocks from the most recent form build (for flush on save).</summary>
        public List<CadenceEditorBlock>? LastCadenceBlocks { get; private set; }

        private void OnCancelAction()
        {
            CancelActionRequested?.Invoke();
        }
    }
}
