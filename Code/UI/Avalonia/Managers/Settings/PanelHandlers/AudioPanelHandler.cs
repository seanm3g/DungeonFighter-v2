using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Platform.Storage;
using Avalonia.Threading;
using RPGGame.Audio;
using RPGGame.UI.Avalonia.Settings;

namespace RPGGame.UI.Avalonia.Managers.Settings.PanelHandlers
{
    /// <summary>
    /// Wires up the Audio settings panel and persists <see cref="AudioConfig"/> on save.
    /// </summary>
    /// <remarks>
    /// Volume / mute changes are applied live to the audio engine (no need to click Save for the
    /// player to hear the new volume). File bindings, state→music dropdowns, and rate-limit edits
    /// take effect immediately too because <see cref="AudioCueDispatcher"/> reads the singleton
    /// <see cref="AudioConfig.Instance"/> at every trigger. Save persists the in-memory config to
    /// <c>GameData/Audio/AudioConfig.json</c>.
    /// </remarks>
    public class AudioPanelHandler : ISettingsPanelHandler
    {
        public string PanelType => "Audio";

        public void WireUp(UserControl panel)
        {
            if (panel is not AudioSettingsPanel audioPanel) return;

            // Always read the latest config from disk when opening the panel.
            AudioConfig.ReloadFromFile();
            var config = AudioConfig.Instance;

            WireVolumeControls(audioPanel, config);
            BuildStateMusicMappingRows(audioPanel, config);
            BuildCueBindingRows(audioPanel, config);

            Dispatcher.UIThread.Post(() => LoadSettings(panel), DispatcherPriority.Loaded);
        }

        public void LoadSettings(UserControl panel)
        {
            if (panel is not AudioSettingsPanel audioPanel) return;

            AudioConfig.ReloadFromFile();
            var config = AudioConfig.Instance;

            var masterSlider = audioPanel.FindControl<Slider>("MasterVolumeSlider");
            var musicSlider  = audioPanel.FindControl<Slider>("MusicVolumeSlider");
            var sfxSlider    = audioPanel.FindControl<Slider>("SfxVolumeSlider");
            var masterMute   = audioPanel.FindControl<CheckBox>("MasterMuteCheckBox");
            var musicMute    = audioPanel.FindControl<CheckBox>("MusicMuteCheckBox");
            var sfxMute      = audioPanel.FindControl<CheckBox>("SfxMuteCheckBox");
            var crossfade    = audioPanel.FindControl<NumericUpDown>("MusicCrossfadeMsBox");
            var masterLbl    = audioPanel.FindControl<TextBlock>("MasterVolumeLabel");
            var musicLbl     = audioPanel.FindControl<TextBlock>("MusicVolumeLabel");
            var sfxLbl       = audioPanel.FindControl<TextBlock>("SfxVolumeLabel");

            if (masterSlider != null) masterSlider.Value = config.MasterVolume;
            if (musicSlider  != null) musicSlider.Value  = config.MusicVolume;
            if (sfxSlider    != null) sfxSlider.Value    = config.SfxVolume;
            if (masterMute   != null) masterMute.IsChecked = !ResolveMasterUnmuted();
            if (musicMute    != null) musicMute.IsChecked  = !config.MusicEnabled;
            if (sfxMute      != null) sfxMute.IsChecked    = !config.SfxEnabled;
            if (crossfade    != null) crossfade.Value      = config.MusicCrossfadeMs;
            if (masterLbl    != null) masterLbl.Text       = $"{(int)Math.Round(config.MasterVolume * 100)}%";
            if (musicLbl     != null) musicLbl.Text        = $"{(int)Math.Round(config.MusicVolume * 100)}%";
            if (sfxLbl       != null) sfxLbl.Text          = $"{(int)Math.Round(config.SfxVolume * 100)}%";

            BuildStateMusicMappingRows(audioPanel, config);
            BuildCueBindingRows(audioPanel, config);
        }

        public void SaveSettings(UserControl panel)
        {
            if (panel is not AudioSettingsPanel audioPanel) return;
            var config = AudioConfig.Instance;

            var masterSlider = audioPanel.FindControl<Slider>("MasterVolumeSlider");
            var musicSlider  = audioPanel.FindControl<Slider>("MusicVolumeSlider");
            var sfxSlider    = audioPanel.FindControl<Slider>("SfxVolumeSlider");
            var masterMute   = audioPanel.FindControl<CheckBox>("MasterMuteCheckBox");
            var musicMute    = audioPanel.FindControl<CheckBox>("MusicMuteCheckBox");
            var sfxMute      = audioPanel.FindControl<CheckBox>("SfxMuteCheckBox");
            var crossfade    = audioPanel.FindControl<NumericUpDown>("MusicCrossfadeMsBox");

            if (masterSlider != null) config.MasterVolume     = (float)masterSlider.Value;
            if (musicSlider  != null) config.MusicVolume      = (float)musicSlider.Value;
            if (sfxSlider    != null) config.SfxVolume        = (float)sfxSlider.Value;
            if (musicMute    != null) config.MusicEnabled     = !(musicMute.IsChecked ?? false);
            if (sfxMute      != null) config.SfxEnabled       = !(sfxMute.IsChecked ?? false);
            if (crossfade    != null) config.MusicCrossfadeMs = (int)(crossfade.Value ?? 200m);

            // Master mute folds into GameSettings.EnableSoundEffects so legacy code that gates on
            // that flag continues to honour the panel's master mute. We don't toggle it from this
            // panel beyond that — leave per-bus mutes independent.
            if (masterMute != null)
                GameSettings.Instance.EnableSoundEffects = !(masterMute.IsChecked ?? false);

            config.Save();
            AudioBootstrap.ApplyConfigToEngine();
        }

        private void WireVolumeControls(AudioSettingsPanel panel, AudioConfig config)
        {
            var masterSlider = panel.FindControl<Slider>("MasterVolumeSlider");
            var musicSlider  = panel.FindControl<Slider>("MusicVolumeSlider");
            var sfxSlider    = panel.FindControl<Slider>("SfxVolumeSlider");
            var masterMute   = panel.FindControl<CheckBox>("MasterMuteCheckBox");
            var musicMute    = panel.FindControl<CheckBox>("MusicMuteCheckBox");
            var sfxMute      = panel.FindControl<CheckBox>("SfxMuteCheckBox");
            var crossfade    = panel.FindControl<NumericUpDown>("MusicCrossfadeMsBox");
            var masterLbl    = panel.FindControl<TextBlock>("MasterVolumeLabel");
            var musicLbl     = panel.FindControl<TextBlock>("MusicVolumeLabel");
            var sfxLbl       = panel.FindControl<TextBlock>("SfxVolumeLabel");

            if (masterSlider != null)
                masterSlider.PropertyChanged += (_, e) =>
                {
                    if (e.Property != Slider.ValueProperty) return;
                    float v = (float)masterSlider.Value;
                    config.MasterVolume = v;
                    if (masterLbl != null) masterLbl.Text = $"{(int)Math.Round(v * 100)}%";
                    AudioBootstrap.Engine?.SetMasterVolume(v);
                };
            if (musicSlider != null)
                musicSlider.PropertyChanged += (_, e) =>
                {
                    if (e.Property != Slider.ValueProperty) return;
                    float v = (float)musicSlider.Value;
                    config.MusicVolume = v;
                    if (musicLbl != null) musicLbl.Text = $"{(int)Math.Round(v * 100)}%";
                    AudioBootstrap.Engine?.SetBusVolume(AudioBusKind.Music, v);
                };
            if (sfxSlider != null)
                sfxSlider.PropertyChanged += (_, e) =>
                {
                    if (e.Property != Slider.ValueProperty) return;
                    float v = (float)sfxSlider.Value;
                    config.SfxVolume = v;
                    if (sfxLbl != null) sfxLbl.Text = $"{(int)Math.Round(v * 100)}%";
                    AudioBootstrap.Engine?.SetBusVolume(AudioBusKind.Sfx, v);
                };

            if (masterMute != null)
                masterMute.IsCheckedChanged += (_, _) =>
                {
                    bool muted = masterMute.IsChecked ?? false;
                    GameSettings.Instance.EnableSoundEffects = !muted;
                    AudioBootstrap.Engine?.SetMasterMute(muted);
                };
            if (musicMute != null)
                musicMute.IsCheckedChanged += (_, _) =>
                {
                    bool muted = musicMute.IsChecked ?? false;
                    config.MusicEnabled = !muted;
                    AudioBootstrap.Engine?.SetBusMute(AudioBusKind.Music, muted);
                };
            if (sfxMute != null)
                sfxMute.IsCheckedChanged += (_, _) =>
                {
                    bool muted = sfxMute.IsChecked ?? false;
                    config.SfxEnabled = !muted;
                    AudioBootstrap.Engine?.SetBusMute(AudioBusKind.Sfx, muted);
                };
            if (crossfade != null)
                crossfade.ValueChanged += (_, _) =>
                {
                    if (crossfade.Value is decimal d)
                        config.MusicCrossfadeMs = (int)d;
                };
        }

        private void BuildStateMusicMappingRows(AudioSettingsPanel panel, AudioConfig config)
        {
            var host = panel.FindControl<ItemsControl>("StateMusicMapItemsControl");
            if (host == null) return;

            // Only show states that are currently mapped or that are commonly used for music. We
            // populate from the existing stateMusicMap so the user can edit what they already have.
            var rows = new List<Control>();
            var musicCueNames = new List<string> { "(none)" };
            musicCueNames.AddRange(Enum.GetNames(typeof(AudioCue)).Where(n => n.StartsWith("Music_", StringComparison.Ordinal)));

            // Iterate a stable order over the configured map.
            foreach (var stateName in config.StateMusicMap.Keys.OrderBy(n => n).ToList())
            {
                rows.Add(BuildStateMusicMappingRow(stateName, config, musicCueNames));
            }

            host.ItemsSource = rows;
        }

        private Control BuildStateMusicMappingRow(string stateName, AudioConfig config, List<string> musicCueNames)
        {
            var grid = new Grid
            {
                ColumnDefinitions = new ColumnDefinitions("260,*")
            };
            grid.Children.Add(new TextBlock
            {
                Text = stateName,
                Foreground = new SolidColorBrush(Color.FromArgb(0xFF, 0xE8, 0xE8, 0xE8)),
                VerticalAlignment = VerticalAlignment.Center
            });
            var combo = new ComboBox { ItemsSource = musicCueNames, MinWidth = 240 };
            string current = config.StateMusicMap.TryGetValue(stateName, out var c) ? c : string.Empty;
            int idx = musicCueNames.IndexOf(string.IsNullOrEmpty(current) ? "(none)" : current);
            combo.SelectedIndex = idx < 0 ? 0 : idx;
            combo.SelectionChanged += (_, _) =>
            {
                if (combo.SelectedItem is not string selected) return;
                config.StateMusicMap[stateName] = selected == "(none)" ? string.Empty : selected;
            };
            Grid.SetColumn(combo, 1);
            grid.Children.Add(combo);
            return grid;
        }

        private void BuildCueBindingRows(AudioSettingsPanel panel, AudioConfig config)
        {
            var host = panel.FindControl<ItemsControl>("CueBindingItemsControl");
            if (host == null) return;
            config.EnsureDefaultEntriesForAllCues();

            var rows = new List<Control>();
            foreach (AudioCue cue in Enum.GetValues(typeof(AudioCue)))
            {
                if (cue == AudioCue.None) continue;
                rows.Add(BuildCueBindingRow(panel, cue, config));
            }
            host.ItemsSource = rows;
        }

        private Control BuildCueBindingRow(AudioSettingsPanel panel, AudioCue cue, AudioConfig config)
        {
            var binding = config.GetBinding(cue) ?? new AudioCueBinding();
            // Ensure the binding is stored so live edits stick.
            if (!config.CueMap.ContainsKey(cue.ToString())) config.CueMap[cue.ToString()] = binding;

            var grid = new Grid
            {
                ColumnDefinitions = new ColumnDefinitions("220,*,160,90,80,80")
            };

            var nameLabel = new TextBlock
            {
                Text = cue.ToString(),
                Foreground = new SolidColorBrush(Color.FromArgb(0xFF, 0xE8, 0xE8, 0xE8)),
                VerticalAlignment = VerticalAlignment.Center
            };
            Grid.SetColumn(nameLabel, 0);
            grid.Children.Add(nameLabel);

            var fileBox = new TextBox
            {
                Text = binding.File,
                Margin = new global::Avalonia.Thickness(4, 0, 4, 0),
                VerticalContentAlignment = VerticalAlignment.Center
            };
            fileBox.TextChanged += (_, _) => binding.File = fileBox.Text ?? string.Empty;
            Grid.SetColumn(fileBox, 1);
            grid.Children.Add(fileBox);

            var browseBtn = new Button { Content = "Browse…", Margin = new global::Avalonia.Thickness(4, 0, 4, 0) };
            browseBtn.Click += async (_, _) =>
            {
                var top = TopLevel.GetTopLevel(panel);
                if (top?.StorageProvider == null) return;
                IStorageFolder? startFolder = null;
                try
                {
                    string baseDir = Path.GetDirectoryName(AudioConfig.GetConfigFilePath()) ?? string.Empty;
                    if (Directory.Exists(baseDir))
                        startFolder = await top.StorageProvider.TryGetFolderFromPathAsync(new Uri(baseDir));
                }
                catch { /* fall back to default folder */ }

                var picked = await top.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
                {
                    Title = $"Pick audio file for {cue}",
                    AllowMultiple = false,
                    SuggestedStartLocation = startFolder,
                    FileTypeFilter = new[]
                    {
                        new FilePickerFileType("Audio (WAV / MP3 / FLAC)")
                        {
                            Patterns = new[] { "*.wav", "*.mp3", "*.flac", "*.ogg" }
                        }
                    }
                });
                if (picked.Count == 0) return;
                string chosen = picked[0].Path.LocalPath;
                // If under GameData/Audio/, store the relative path so the file is portable.
                string baseDir2 = Path.GetDirectoryName(AudioConfig.GetConfigFilePath()) ?? string.Empty;
                if (!string.IsNullOrEmpty(baseDir2) && chosen.StartsWith(baseDir2, StringComparison.OrdinalIgnoreCase))
                {
                    string relative = chosen.Substring(baseDir2.Length).TrimStart(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
                    binding.File = relative;
                    fileBox.Text = relative;
                }
                else
                {
                    binding.File = chosen;
                    fileBox.Text = chosen;
                }
            };
            Grid.SetColumn(browseBtn, 2);
            grid.Children.Add(browseBtn);

            var volSlider = new Slider { Minimum = 0, Maximum = 1, Value = binding.Volume, TickFrequency = 0.05, VerticalAlignment = VerticalAlignment.Center };
            volSlider.PropertyChanged += (_, e) =>
            {
                if (e.Property != Slider.ValueProperty) return;
                binding.Volume = (float)volSlider.Value;
            };
            Grid.SetColumn(volSlider, 3);
            grid.Children.Add(volSlider);

            var testBtn = new Button { Content = "Test", Margin = new global::Avalonia.Thickness(4, 0, 4, 0) };
            testBtn.Click += (_, _) => AudioCues.Trigger(cue);
            Grid.SetColumn(testBtn, 4);
            grid.Children.Add(testBtn);

            var clearBtn = new Button { Content = "Clear" };
            clearBtn.Click += (_, _) =>
            {
                binding.File = string.Empty;
                fileBox.Text = string.Empty;
            };
            Grid.SetColumn(clearBtn, 5);
            grid.Children.Add(clearBtn);

            return grid;
        }

        private static bool ResolveMasterUnmuted() => GameSettings.Instance.EnableSoundEffects;
    }
}
