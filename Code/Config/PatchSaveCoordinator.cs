using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Layout;
using Avalonia.Media;
using RPGGame.Audio;
using RPGGame.UI.Avalonia.Settings;
using System;
using System.Text.Json;
using System.Threading.Tasks;

namespace RPGGame.Config
{
    /// <summary>
    /// Prompts for Update vs Save As when persisting config patches from Settings.
    /// </summary>
    public static class PatchSaveCoordinator
    {
        public static Task<bool> SaveGameSettingsAsync(Window? owner, GameSettings settings)
        {
            settings.ValidateAndFix();
            var doc = GeneralSettingsStore.Load();
            GeneralSettingsStore.Save(settings, doc.AudioPreferences);
            return Task.FromResult(true);
        }

        /// <summary>Persists bus volume/mute/crossfade to local general settings (no patch dialog).</summary>
        public static Task<bool> SaveAudioPreferencesAsync(AudioConfig config)
        {
            config.ValidateAndFix();
            config.SaveAudioPreferences();
            return Task.FromResult(true);
        }

        /// <summary>Prompts Update vs Save As for cue bindings and state→music mappings only.</summary>
        public static async Task<bool> SaveAudioPatchAsync(Window? owner, AudioConfig config)
        {
            config.ValidateAndFix();

            var profile = PatchProfileService.LoadProfile();
            string active = profile.GetActivePatchName(PatchCategory.Audio);
            var choice = await PatchSaveDialog.ShowAsync(owner, PatchCategory.Audio, active);
            if (choice == PatchSaveChoice.Cancelled)
                return false;

            var patch = new AudioPatchContent
            {
                CueMap = config.CueMap,
                StateMusicMap = config.StateMusicMap
            };
            string json = JsonSerializer.Serialize(patch, new JsonSerializerOptions { WriteIndented = true });
            return await PersistAsync(owner, PatchCategory.Audio, active, json, choice);
        }

        public static async Task<bool> SaveBalanceAsync(Window? owner, GameConfiguration config)
        {
            var profile = PatchProfileService.LoadProfile();
            string active = profile.GetActivePatchName(PatchCategory.Balance);
            var choice = await PatchSaveDialog.ShowAsync(owner, PatchCategory.Balance, active);
            if (choice == PatchSaveChoice.Cancelled)
                return false;

            config.ClassPresentation = config.ClassPresentation.EnsureNormalized();
            var options = new JsonSerializerOptions
            {
                WriteIndented = true,
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            };
            string json = JsonSerializer.Serialize(config, options);
            return await PersistAsync(owner, PatchCategory.Balance, active, json, choice);
        }

        private static async Task<bool> PersistAsync(
            Window? owner,
            PatchCategory category,
            string activePatchName,
            string json,
            PatchSaveChoice choice)
        {
            if (choice == PatchSaveChoice.UpdateExisting)
            {
                PatchProfileService.UpdateActivePatch(category, json);
                return true;
            }

            string? newName = await PromptNewPatchNameAsync(owner, category);
            if (string.IsNullOrWhiteSpace(newName))
                return false;

            try
            {
                PatchProfileService.CreatePatch(category, newName, json, switchActive: true);
                return true;
            }
            catch (Exception ex)
            {
                await ShowMessageAsync(owner, "Could not save patch", ex.Message);
                return false;
            }
        }

        private static async Task<string?> PromptNewPatchNameAsync(Window? owner, PatchCategory category)
        {
            var input = new TextBox
            {
                Watermark = "e.g. my-audio-volumes",
                Width = 360
            };
            var dialog = new Window
            {
                Title = $"New {PatchProfileService.GetCategoryDisplayName(category)} patch",
                Width = 420,
                Height = 180,
                WindowStartupLocation = WindowStartupLocation.CenterOwner,
                Background = Brushes.Black,
                Content = new StackPanel
                {
                    Margin = new Thickness(20),
                    Spacing = 12,
                    Children =
                    {
                        new TextBlock
                        {
                            Text = "Enter a patch name (letters, numbers, hyphens, underscores):",
                            Foreground = Brushes.White,
                            TextWrapping = TextWrapping.Wrap
                        },
                        input,
                        new StackPanel
                        {
                            Orientation = Orientation.Horizontal,
                            HorizontalAlignment = HorizontalAlignment.Right,
                            Spacing = 8,
                            Children =
                            {
                                new Button { Content = "Create", Width = 90 },
                                new Button { Content = "Cancel", Width = 90 }
                            }
                        }
                    }
                }
            };

            var buttons = ((StackPanel)((StackPanel)dialog.Content!).Children[2]).Children;
            var createBtn = (Button)buttons[0]!;
            var cancelBtn = (Button)buttons[1]!;

            string? result = null;
            createBtn.Click += (_, _) =>
            {
                try
                {
                    result = PatchProfileService.SanitizePatchName(input.Text ?? string.Empty);
                    dialog.Close(true);
                }
                catch (Exception ex)
                {
                    result = null;
                    _ = ShowMessageAsync(dialog, "Invalid name", ex.Message);
                }
            };
            cancelBtn.Click += (_, _) => dialog.Close(false);

            Window? target = owner;
            if (target == null && Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
                target = desktop.MainWindow;
            if (target == null)
                return null;

            bool ok = await dialog.ShowDialog<bool>(target);
            return ok ? result : null;
        }

        private static async Task ShowMessageAsync(Window? owner, string title, string message)
        {
            await ConfirmationDialog.ShowAsync(owner, title, message);
        }
    }
}
