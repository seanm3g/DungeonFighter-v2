using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Avalonia.Media;
using Avalonia.Platform.Storage;
using RPGGame.Config;
using RPGGame.Utils;
using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace RPGGame.UI.Avalonia.Settings
{
    public partial class BalanceTuningSettingsPanel : UserControl
    {
        private System.Action<string, bool>? statusCallback;

        public BalanceTuningSettingsPanel()
        {
            InitializeComponent();
            this.Loaded += OnLoaded;
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }

        private void OnLoaded(object? sender, RoutedEventArgs e)
        {
            SetupEventHandlers();
            InitializeDefaultPaths();
        }

        private void SetupEventHandlers()
        {
            if (ExportButton != null)
                ExportButton.Click += OnExportButtonClick;
            if (ImportButton != null)
                ImportButton.Click += OnImportButtonClick;
            if (BrowseExportPathButton != null)
                BrowseExportPathButton.Click += OnBrowseExportPathClick;
            if (BrowseImportPathButton != null)
                BrowseImportPathButton.Click += OnBrowseImportPathClick;
            if (ImportFilePathTextBox != null)
                ImportFilePathTextBox.TextChanged += OnImportFilePathChanged;
        }

        private void InitializeDefaultPaths()
        {
            if (ExportFilePathTextBox != null)
            {
                string defaultExportPath = BalanceTuningExportManager.GetDefaultExportPath("balance_tuning_export.json");
                ExportFilePathTextBox.Text = defaultExportPath;
            }
        }

        private async void OnBrowseExportPathClick(object? sender, RoutedEventArgs e)
        {
            try
            {
                var topLevel = TopLevel.GetTopLevel(this);
                if (topLevel == null) return;

                var file = await topLevel.StorageProvider.SaveFilePickerAsync(new FilePickerSaveOptions
                {
                    Title = "Save Balance Tuning Settings",
                    DefaultExtension = "json",
                    SuggestedFileName = "balance_tuning_export.json",
                    FileTypeChoices = new[]
                    {
                        new FilePickerFileType("JSON Files")
                        {
                            Patterns = new[] { "*.json" }
                        }
                    }
                });

                if (file != null)
                {
                    ExportFilePathTextBox.Text = file.Path.LocalPath;
                }
            }
            catch (Exception ex)
            {
                ShowStatus($"Error browsing for export path: {ex.Message}", false);
            }
        }

        private async void OnBrowseImportPathClick(object? sender, RoutedEventArgs e)
        {
            try
            {
                var topLevel = TopLevel.GetTopLevel(this);
                if (topLevel == null) return;

                var files = await topLevel.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
                {
                    Title = "Load Balance Tuning Settings",
                    AllowMultiple = false,
                    FileTypeFilter = new[]
                    {
                        new FilePickerFileType("JSON Files")
                        {
                            Patterns = new[] { "*.json" }
                        },
                        new FilePickerFileType("All Files")
                        {
                            Patterns = new[] { "*.*" }
                        }
                    }
                });

                if (files.Count > 0)
                {
                    ImportFilePathTextBox.Text = files[0].Path.LocalPath;
                }
            }
            catch (Exception ex)
            {
                ShowStatus($"Error browsing for import path: {ex.Message}", false);
            }
        }

        private void OnImportFilePathChanged(object? sender, TextChangedEventArgs e)
        {
            string filePath = ImportFilePathTextBox.Text ?? "";
            
            if (string.IsNullOrWhiteSpace(filePath) || !File.Exists(filePath))
            {
                ImportPreviewBorder.IsVisible = false;
                return;
            }

            try
            {
                var (success, errorMessage, export) = BalanceTuningExportManager.ImportFromFile(filePath);
                
                if (success && export != null)
                {
                    ImportPreviewName.Text = $"Name: {export.Metadata.Name}";
                    ImportPreviewDescription.Text = string.IsNullOrWhiteSpace(export.Metadata.Description) 
                        ? "No description provided" 
                        : $"Description: {export.Metadata.Description}";
                    ImportPreviewCreatedBy.Text = string.IsNullOrWhiteSpace(export.Metadata.CreatedBy) 
                        ? "" 
                        : $"Created By: {export.Metadata.CreatedBy}";
                    ImportPreviewDate.Text = $"Created: {export.Metadata.CreatedDate}";
                    ImportPreviewBorder.IsVisible = true;
                }
                else
                {
                    ImportPreviewBorder.IsVisible = false;
                }
            }
            catch (Exception ex)
            {
                ImportPreviewBorder.IsVisible = false;
                ScrollDebugLogger.Log($"BalanceTuningSettingsPanel: Error previewing import: {ex.Message}");
            }
        }

        private void OnExportButtonClick(object? sender, RoutedEventArgs e)
        {
            try
            {
                string filePath = ExportFilePathTextBox.Text ?? "";
                
                if (string.IsNullOrWhiteSpace(filePath))
                {
                    ShowStatus("Please specify an export file path", false);
                    return;
                }

                string name = ExportNameTextBox.Text ?? "Balance Tuning Settings";
                string description = ExportDescriptionTextBox.Text ?? "";
                string createdBy = ExportCreatedByTextBox.Text ?? "";
                string notes = ExportNotesTextBox.Text ?? "";

                bool success = BalanceTuningExportManager.ExportToFile(filePath, name, description, createdBy, notes);

                if (success)
                {
                    ShowStatus($"Settings exported successfully to: {filePath}", true);
                }
                else
                {
                    ShowStatus("Failed to export settings. Check the log for details.", false);
                }
            }
            catch (Exception ex)
            {
                ShowStatus($"Error exporting settings: {ex.Message}", false);
                ScrollDebugLogger.Log($"BalanceTuningSettingsPanel: Error exporting: {ex.Message}");
            }
        }

        private void OnImportButtonClick(object? sender, RoutedEventArgs e)
        {
            try
            {
                string filePath = ImportFilePathTextBox.Text ?? "";
                
                if (string.IsNullOrWhiteSpace(filePath))
                {
                    ShowStatus("Please specify an import file path", false);
                    return;
                }

                if (!File.Exists(filePath))
                {
                    ShowStatus("Import file not found", false);
                    return;
                }

                var (success, errorMessage, export) = BalanceTuningExportManager.ImportFromFile(filePath);

                if (!success || export == null)
                {
                    ShowStatus($"Failed to import settings: {errorMessage ?? "Unknown error"}", false);
                    return;
                }

                bool applied = BalanceTuningExportManager.ApplyImportedSettings(export);

                if (applied)
                {
                    ShowStatus($"Settings imported successfully from: {export.Metadata.Name}", true);
                    ImportPreviewBorder.IsVisible = false;
                }
                else
                {
                    ShowStatus("Failed to apply imported settings. Check the log for details.", false);
                }
            }
            catch (Exception ex)
            {
                ShowStatus($"Error importing settings: {ex.Message}", false);
                ScrollDebugLogger.Log($"BalanceTuningSettingsPanel: Error importing: {ex.Message}");
            }
        }

        private void ShowStatus(string message, bool isSuccess)
        {
            StatusTextBlock.Text = message;
            StatusTextBlock.Foreground = isSuccess 
                ? new SolidColorBrush(Color.FromRgb(40, 167, 69)) // Green
                : new SolidColorBrush(Color.FromRgb(220, 53, 69)); // Red
            StatusBorder.IsVisible = true;
            
            statusCallback?.Invoke(message, isSuccess);
            ScrollDebugLogger.Log($"BalanceTuningSettingsPanel: {message}");
        }

        public void SetStatusCallback(System.Action<string, bool> callback)
        {
            statusCallback = callback;
        }
    }
}

