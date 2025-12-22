using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.Media;
using RPGGame.Editors;
using RPGGame.UI.Avalonia.Builders;
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
        private ActionFormBuilder? formBuilder;
        
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
            
            formBuilder = new ActionFormBuilder(actionFormControls, showStatusMessage);
            formBuilder.SaveActionRequested += SaveAction;
            formBuilder.CancelActionRequested += OnCancelAction;
            
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
            if (actionFormPanel == null || formBuilder == null) return;
            formBuilder.BuildForm(actionFormPanel, action, isCreatingNewAction);
        }

        private void OnCancelAction()
        {
            if (actionFormPanel != null) actionFormPanel.Children.Clear();
            if (actionsListBox != null) actionsListBox.SelectedItem = null;
            selectedAction = null;
        }

        private void SaveAction(ActionData action, bool isCreatingNew)
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
            
            string? errorMessage = actionEditor.ValidateAction(action, isCreatingNew ? null : action.Name);
            if (errorMessage != null)
            {
                showStatusMessage?.Invoke(errorMessage, false);
                return;
            }
            
            bool success;
            if (isCreatingNew)
            {
                success = actionEditor.CreateAction(action);
                if (success)
                {
                    showStatusMessage?.Invoke($"Action '{action.Name}' created successfully", true);
                    isCreatingNewAction = false;
                    LoadActionsList();
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
        }
    }
}

