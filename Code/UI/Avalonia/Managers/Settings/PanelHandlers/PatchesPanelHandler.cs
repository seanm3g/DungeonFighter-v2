using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Platform.Storage;
using RPGGame.Audio;
using RPGGame.Config;
using RPGGame.UI.Avalonia.Settings;

namespace RPGGame.UI.Avalonia.Managers.Settings.PanelHandlers
{
    /// <summary>
    /// Lets the player choose active audio and balance config patches (local-only). Game settings
    /// always use the repo default patch.
    /// </summary>
    public sealed class PatchesPanelHandler : ISettingsPanelHandler
    {
        private readonly Action<string, bool>? showStatusMessage;

        public PatchesPanelHandler(Action<string, bool>? showStatusMessage = null)
        {
            this.showStatusMessage = showStatusMessage;
        }

        public string PanelType => "Patches";

        public void WireUp(UserControl panel)
        {
            if (panel is not PatchesSettingsPanel p) return;

            var refresh = p.FindControl<Button>("RefreshPatchesButton");
            var apply = p.FindControl<Button>("ApplyPatchesButton");
            var import = p.FindControl<Button>("ImportPatchButton");
            var export = p.FindControl<Button>("ExportPatchButton");
            if (refresh != null)
                refresh.Click += (_, _) => LoadSettings(panel);
            if (apply != null)
                apply.Click += (_, _) => ApplySelection(p);
            if (import != null)
                import.Click += async (_, _) => await ImportPatchAsync(p);
            if (export != null)
                export.Click += async (_, _) => await ExportPatchAsync(p);

            LoadSettings(panel);
        }

        public void LoadSettings(UserControl panel)
        {
            if (panel is not PatchesSettingsPanel p) return;

            PatchProfileService.EnsureBootstrapped();
            var profile = PatchProfileService.LoadProfile();

            var gsLabel = p.FindControl<TextBlock>("GameSettingsPatchLabel");
            if (gsLabel != null)
                gsLabel.Text = $"local — {GeneralSettingsStore.GetFilePath()}";

            PopulateCombo(p.FindControl<ComboBox>("AudioPatchCombo"), PatchCategory.Audio, profile.ActiveAudioPatch);
            PopulateCombo(p.FindControl<ComboBox>("BalancePatchCombo"), PatchCategory.Balance, profile.ActiveBalancePatch);

            var pathsBlock = p.FindControl<TextBlock>("ActivePathsTextBlock");
            if (pathsBlock != null)
            {
                pathsBlock.Text =
                    $"General settings: {GeneralSettingsStore.GetFilePath()}\n" +
                    $"Audio patch: {PatchProfileService.GetActivePatchFilePath(PatchCategory.Audio)}\n" +
                    $"Balance: {PatchProfileService.GetActivePatchFilePath(PatchCategory.Balance)}\n" +
                    $"Profile: {PatchProfileService.GetProfileFilePath()}";
            }
        }

        public void SaveSettings(UserControl panel)
        {
            // Selection is applied immediately via Apply; nothing to persist on global Save.
        }

        private static void PopulateCombo(ComboBox? combo, PatchCategory category, string activeName)
        {
            if (combo == null) return;
            var names = PatchProfileService.ListPatches(category);
            combo.ItemsSource = names;
            combo.SelectedItem = names.FirstOrDefault(n => string.Equals(n, activeName, StringComparison.OrdinalIgnoreCase))
                ?? names.FirstOrDefault();
        }

        private async Task ImportPatchAsync(PatchesSettingsPanel panel)
        {
            try
            {
                if (!TryGetIoCategory(panel, out PatchCategory category, out string? categoryErr))
                {
                    showStatusMessage?.Invoke(categoryErr ?? "Select a category.", false);
                    return;
                }

                var top = TopLevel.GetTopLevel(panel);
                if (top?.StorageProvider == null)
                {
                    showStatusMessage?.Invoke("File picker is not available.", false);
                    return;
                }

                IStorageFolder? startFolder = null;
                try
                {
                    string folder = PatchProfileService.GetCategoryFolder(category);
                    if (Directory.Exists(folder))
                        startFolder = await top.StorageProvider.TryGetFolderFromPathAsync(new Uri(folder));
                }
                catch { /* default folder */ }

                var picked = await top.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
                {
                    Title = $"Import {PatchProfileService.GetCategoryDisplayName(category)} patch",
                    AllowMultiple = false,
                    SuggestedStartLocation = startFolder,
                    FileTypeFilter = new[]
                    {
                        new FilePickerFileType("JSON patch") { Patterns = new[] { "*.json" } }
                    }
                });
                if (picked.Count == 0)
                    return;

                string sourcePath = picked[0].Path.LocalPath;
                string defaultName = Path.GetFileNameWithoutExtension(sourcePath) ?? "imported-patch";
                string? patchName = await PatchNameInputDialog.ShowAsync(
                    top as Window,
                    $"Import {PatchProfileService.GetCategoryDisplayName(category)} patch",
                    defaultName);
                if (string.IsNullOrWhiteSpace(patchName))
                    return;

                bool exists = PatchProfileService.PatchExists(category, patchName);
                if (exists)
                {
                    bool overwrite = await ConfirmationDialog.ShowAsync(
                        top as Window,
                        "Overwrite patch?",
                        $"Patch \"{patchName}\" already exists. Replace it with the imported file?");
                    if (!overwrite)
                        return;
                }

                string dest = PatchProfileService.ImportPatchFromFile(category, sourcePath, patchName, overwrite: exists);
                LoadSettings(panel);
                showStatusMessage?.Invoke($"Imported patch \"{patchName}\" to {dest}", true);
            }
            catch (Exception ex)
            {
                showStatusMessage?.Invoke($"Import failed: {ex.Message}", false);
            }
        }

        private async Task ExportPatchAsync(PatchesSettingsPanel panel)
        {
            try
            {
                if (!TryGetIoCategory(panel, out PatchCategory category, out string? categoryErr))
                {
                    showStatusMessage?.Invoke(categoryErr ?? "Select a category.", false);
                    return;
                }

                string? patchName = GetSelectedPatchName(panel, category);
                if (string.IsNullOrWhiteSpace(patchName))
                {
                    showStatusMessage?.Invoke("Select a patch to export in the dropdown above.", false);
                    return;
                }

                var top = TopLevel.GetTopLevel(panel);
                if (top?.StorageProvider == null)
                {
                    showStatusMessage?.Invoke("File picker is not available.", false);
                    return;
                }

                var picked = await top.StorageProvider.SaveFilePickerAsync(new FilePickerSaveOptions
                {
                    Title = $"Export {PatchProfileService.GetCategoryDisplayName(category)} patch",
                    SuggestedFileName = $"{patchName}.json",
                    DefaultExtension = "json",
                    FileTypeChoices = new[]
                    {
                        new FilePickerFileType("JSON patch") { Patterns = new[] { "*.json" } }
                    }
                });
                if (picked == null)
                    return;

                PatchProfileService.ExportPatchToFile(category, patchName, picked.Path.LocalPath);
                showStatusMessage?.Invoke($"Exported \"{patchName}\" to {picked.Path.LocalPath}", true);
            }
            catch (Exception ex)
            {
                showStatusMessage?.Invoke($"Export failed: {ex.Message}", false);
            }
        }

        private static bool TryGetIoCategory(PatchesSettingsPanel panel, out PatchCategory category, out string? error)
        {
            category = PatchCategory.GameSettings;
            error = null;
            var combo = panel.FindControl<ComboBox>("PatchIoCategoryCombo");
            if (combo?.SelectedItem is ComboBoxItem item && item.Content is string label)
            {
                category = PatchProfileService.ParseCategoryLabel(label);
                return true;
            }
            if (combo?.SelectedItem is string label2)
            {
                category = PatchProfileService.ParseCategoryLabel(label2);
                return true;
            }
            error = "Select a category for import/export.";
            return false;
        }

        private static string? GetSelectedPatchName(PatchesSettingsPanel panel, PatchCategory category) => category switch
        {
            PatchCategory.Audio => panel.FindControl<ComboBox>("AudioPatchCombo")?.SelectedItem as string,
            PatchCategory.Balance => panel.FindControl<ComboBox>("BalancePatchCombo")?.SelectedItem as string,
            _ => PatchProfile.DefaultPatchName
        };

        private void ApplySelection(PatchesSettingsPanel panel)
        {
            try
            {
                var profile = PatchProfileService.LoadProfile();
                bool audioChanged = TryApplyCombo(panel.FindControl<ComboBox>("AudioPatchCombo"), PatchCategory.Audio, profile);
                bool balanceChanged = TryApplyCombo(panel.FindControl<ComboBox>("BalancePatchCombo"), PatchCategory.Balance, profile);

                if (audioChanged || balanceChanged)
                {
                    if (audioChanged)
                        AudioBootstrap.ApplyConfigToEngine();

                    string message = (audioChanged, balanceChanged) switch
                    {
                        (true, true) => "Active audio and balance patches updated.",
                        (true, false) => "Active audio patch updated.",
                        (false, true) => "Active balance patch updated.",
                        _ => "Active patches updated."
                    };
                    showStatusMessage?.Invoke(message, true);
                }
                else
                {
                    showStatusMessage?.Invoke("No patch selection changes.", true);
                }

                LoadSettings(panel);
            }
            catch (Exception ex)
            {
                showStatusMessage?.Invoke($"Could not apply patches: {ex.Message}", false);
            }
        }

        private static bool TryApplyCombo(ComboBox? combo, PatchCategory category, PatchProfile profile)
        {
            if (combo?.SelectedItem is not string selected)
                return false;
            string current = profile.GetActivePatchName(category);
            if (string.Equals(current, selected, StringComparison.OrdinalIgnoreCase))
                return false;
            PatchProfileService.SetActivePatch(category, selected);
            return true;
        }
    }
}
