using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Threading;
using RPGGame;
using RPGGame.Editors;
using RPGGame.UI.Avalonia.Builders;
using System;
using System.Collections.Generic;
using System.Linq;

namespace RPGGame.UI.Avalonia.Managers
{
    /// <summary>
    /// Manages the Status Effects tab in SettingsPanel
    /// Allows adding, removing, and editing status effects dynamically
    /// </summary>
    public class StatusEffectsTabManager
    {
        private StatusEffectEditor? statusEffectEditor;
        private string? selectedStatusEffectName;
        private bool isCreatingNewStatusEffect = false;
        private Dictionary<string, Control> statusEffectFormControls = new Dictionary<string, Control>();
        private Dictionary<string, StatusEffectConfig> statusEffectNameToConfig = new Dictionary<string, StatusEffectConfig>();
        private StatusEffectFormBuilder? formBuilder;
        
        private ListBox? statusEffectsListBox;
        private Panel? statusEffectFormPanel;
        private Button? createStatusEffectButton;
        private Button? deleteStatusEffectButton;
        private Action<string, bool>? showStatusMessage;

        public StatusEffectsTabManager()
        {
            statusEffectEditor = new StatusEffectEditor();
        }

        public void Initialize(ListBox statusEffectsListBox, Panel statusEffectFormPanel, Button createStatusEffectButton, Button deleteStatusEffectButton, Action<string, bool> showStatusMessage)
        {
            this.statusEffectsListBox = statusEffectsListBox;
            this.statusEffectFormPanel = statusEffectFormPanel;
            this.createStatusEffectButton = createStatusEffectButton;
            this.deleteStatusEffectButton = deleteStatusEffectButton;
            this.showStatusMessage = showStatusMessage;
            
            formBuilder = new StatusEffectFormBuilder(statusEffectFormControls, showStatusMessage);
            formBuilder.SaveStatusEffectRequested += SaveStatusEffect;
            formBuilder.CancelStatusEffectRequested += OnCancelStatusEffect;
            
            // Defer loading status effects list until after UI is ready to avoid blocking
            Dispatcher.UIThread.Post(() =>
            {
                LoadStatusEffectsList();
            }, DispatcherPriority.Background);
            
            createStatusEffectButton.Click += OnCreateStatusEffectClick;
            deleteStatusEffectButton.Click += OnDeleteStatusEffectClick;
            statusEffectsListBox.SelectionChanged += OnStatusEffectSelectionChanged;
            
            // Initialize button state
            UpdateDeleteButtonState();
        }

        private void LoadStatusEffectsList()
        {
            if (statusEffectEditor == null || statusEffectsListBox == null) return;
            
            var statusEffects = statusEffectEditor.GetStatusEffects();
            statusEffectNameToConfig.Clear();
            
            var effectNames = new List<string>();
            foreach (var effectInfo in statusEffects)
            {
                effectNames.Add(effectInfo.Name);
                statusEffectNameToConfig[effectInfo.Name] = effectInfo.Config;
            }
            
            statusEffectsListBox.ItemsSource = effectNames;
        }

        private void OnStatusEffectSelectionChanged(object? sender, SelectionChangedEventArgs e)
        {
            if (statusEffectsListBox?.SelectedItem is string effectName && 
                statusEffectNameToConfig.TryGetValue(effectName, out var config))
            {
                selectedStatusEffectName = effectName;
                isCreatingNewStatusEffect = false;
                LoadStatusEffectForm(effectName, config);
                UpdateDeleteButtonState();
            }
            else
            {
                selectedStatusEffectName = null;
                isCreatingNewStatusEffect = false;
                UpdateDeleteButtonState();
            }
        }

        private void OnCreateStatusEffectClick(object? sender, RoutedEventArgs e)
        {
            selectedStatusEffectName = "";
            isCreatingNewStatusEffect = true;
            if (statusEffectsListBox != null) statusEffectsListBox.SelectedItem = null;
            
            var newConfig = new StatusEffectConfig();
            LoadStatusEffectForm("", newConfig);
            UpdateDeleteButtonState();
        }

        private void OnDeleteStatusEffectClick(object? sender, RoutedEventArgs e)
        {
            if (statusEffectEditor == null || selectedStatusEffectName == null || isCreatingNewStatusEffect)
            {
                showStatusMessage?.Invoke("No status effect selected for deletion", false);
                return;
            }
            
            if (statusEffectEditor.DeleteStatusEffect(selectedStatusEffectName, out string? errorMessage))
            {
                showStatusMessage?.Invoke($"Status effect '{selectedStatusEffectName}' deleted successfully", true);
                LoadStatusEffectsList();
                statusEffectFormPanel?.Children.Clear();
                selectedStatusEffectName = null;
                UpdateDeleteButtonState();
            }
            else
            {
                showStatusMessage?.Invoke(errorMessage ?? $"Failed to delete status effect '{selectedStatusEffectName}'", false);
            }
        }

        private void LoadStatusEffectForm(string effectName, StatusEffectConfig config)
        {
            if (statusEffectFormPanel == null || formBuilder == null) return;
            formBuilder.BuildForm(statusEffectFormPanel, effectName, config, isCreatingNewStatusEffect);
        }

        private void OnCancelStatusEffect()
        {
            if (statusEffectFormPanel != null) statusEffectFormPanel.Children.Clear();
            if (statusEffectsListBox != null) statusEffectsListBox.SelectedItem = null;
            selectedStatusEffectName = null;
            isCreatingNewStatusEffect = false;
            UpdateDeleteButtonState();
        }

        /// <summary>
        /// Update the delete button state based on current selection
        /// </summary>
        private void UpdateDeleteButtonState()
        {
            if (deleteStatusEffectButton == null || statusEffectEditor == null) return;
            
            bool canDelete = selectedStatusEffectName != null && 
                           !isCreatingNewStatusEffect && 
                           !statusEffectEditor.IsDefaultEffect(selectedStatusEffectName);
            
            deleteStatusEffectButton.IsEnabled = canDelete;
            
            // Update tooltip to explain why button might be disabled
            if (selectedStatusEffectName != null && statusEffectEditor.IsDefaultEffect(selectedStatusEffectName))
            {
                ToolTip.SetTip(deleteStatusEffectButton, $"'{selectedStatusEffectName}' is a default status effect and cannot be deleted");
            }
            else if (selectedStatusEffectName == null || isCreatingNewStatusEffect)
            {
                ToolTip.SetTip(deleteStatusEffectButton, "Select a status effect to delete");
            }
            else
            {
                ToolTip.SetTip(deleteStatusEffectButton, $"Delete '{selectedStatusEffectName}'");
            }
        }

        private void SaveStatusEffect(StatusEffectConfig effectConfig, bool isCreatingNew)
        {
            if (statusEffectEditor == null) return;
            
            // Get the name from the form if creating new
            string effectName = selectedStatusEffectName ?? "";
            if (isCreatingNew && statusEffectFormControls.TryGetValue("Name", out var nameControl) && nameControl is TextBox nameTextBox)
            {
                effectName = nameTextBox.Text?.Trim() ?? "";
            }
            
            if (string.IsNullOrWhiteSpace(effectName))
            {
                showStatusMessage?.Invoke("Status effect name cannot be empty", false);
                return;
            }
            
            // Validate the status effect
            string? errorMessage = statusEffectEditor.ValidateStatusEffect(effectName, isCreatingNew ? null : selectedStatusEffectName);
            if (errorMessage != null)
            {
                showStatusMessage?.Invoke(errorMessage, false);
                return;
            }
            
            bool success;
            if (isCreatingNew)
            {
                success = statusEffectEditor.CreateStatusEffect(effectName, effectConfig);
                if (success)
                {
                    showStatusMessage?.Invoke($"Status effect '{effectName}' created successfully", true);
                    isCreatingNewStatusEffect = false;
                    selectedStatusEffectName = effectName;
                    LoadStatusEffectsList();
                    // Reload the form in edit mode
                    LoadStatusEffectForm(effectName, effectConfig);
                    UpdateDeleteButtonState();
                }
                else
                {
                    showStatusMessage?.Invoke($"Failed to create status effect '{effectName}'. It may already exist.", false);
                    return;
                }
            }
            else
            {
                success = statusEffectEditor.UpdateStatusEffect(selectedStatusEffectName ?? "", effectName, effectConfig);
                if (success)
                {
                    showStatusMessage?.Invoke($"Status effect '{effectName}' updated successfully", true);
                    selectedStatusEffectName = effectName;
                    LoadStatusEffectsList();
                    UpdateDeleteButtonState();
                }
                else
                {
                    showStatusMessage?.Invoke($"Failed to update status effect '{effectName}'", false);
                    return;
                }
            }
        }
    }
}
