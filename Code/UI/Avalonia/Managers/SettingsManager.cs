using Avalonia.Controls;
using RPGGame;
using RPGGame.Config;
using RPGGame.UI;
using System;
using ActionDelegate = System.Action;

namespace RPGGame.UI.Avalonia.Managers
{
    /// <summary>
    /// Manages loading and saving of game settings
    /// Extracted from SettingsPanel.axaml.cs to reduce file size
    /// </summary>
    public class SettingsManager
    {
        private readonly GameSettings settings;
        private readonly Action<string, bool>? showStatusMessage;
        private readonly Action<string>? updateStatus;

        public SettingsManager(GameSettings settings, Action<string, bool>? showStatusMessage, Action<string>? updateStatus)
        {
            this.settings = settings ?? throw new ArgumentNullException(nameof(settings));
            this.showStatusMessage = showStatusMessage;
            this.updateStatus = updateStatus;
        }

        public void LoadSettings(
            Slider narrativeBalanceSlider,
            TextBox narrativeBalanceTextBox,
            CheckBox enableNarrativeEventsCheckBox,
            CheckBox enableInformationalSummariesCheckBox,
            Slider combatSpeedSlider,
            TextBox combatSpeedTextBox,
            CheckBox showIndividualActionMessagesCheckBox,
            CheckBox enableComboSystemCheckBox,
            CheckBox enableTextDisplayDelaysCheckBox,
            CheckBox fastCombatCheckBox,
            CheckBox enableAutoSaveCheckBox,
            TextBox autoSaveIntervalTextBox,
            CheckBox showDetailedStatsCheckBox,
            CheckBox enableSoundEffectsCheckBox,
            Slider enemyHealthMultiplierSlider,
            TextBox enemyHealthMultiplierTextBox,
            Slider enemyDamageMultiplierSlider,
            TextBox enemyDamageMultiplierTextBox,
            Slider playerHealthMultiplierSlider,
            TextBox playerHealthMultiplierTextBox,
            Slider playerDamageMultiplierSlider,
            TextBox playerDamageMultiplierTextBox,
            CheckBox showHealthBarsCheckBox,
            CheckBox showDamageNumbersCheckBox,
            CheckBox showComboProgressCheckBox)
        {
            // Narrative Settings
            narrativeBalanceSlider.Value = settings.NarrativeBalance;
            narrativeBalanceTextBox.Text = settings.NarrativeBalance.ToString("F2");
            enableNarrativeEventsCheckBox.IsChecked = settings.EnableNarrativeEvents;
            enableInformationalSummariesCheckBox.IsChecked = settings.EnableInformationalSummaries;
            
            // Combat Settings
            combatSpeedSlider.Value = settings.CombatSpeed;
            combatSpeedTextBox.Text = settings.CombatSpeed.ToString("F2");
            showIndividualActionMessagesCheckBox.IsChecked = settings.ShowIndividualActionMessages;
            enableComboSystemCheckBox.IsChecked = settings.EnableComboSystem;
            enableTextDisplayDelaysCheckBox.IsChecked = settings.EnableTextDisplayDelays;
            fastCombatCheckBox.IsChecked = settings.FastCombat;
            
            // Gameplay Settings
            enableAutoSaveCheckBox.IsChecked = settings.EnableAutoSave;
            autoSaveIntervalTextBox.Text = settings.AutoSaveInterval.ToString();
            showDetailedStatsCheckBox.IsChecked = settings.ShowDetailedStats;
            enableSoundEffectsCheckBox.IsChecked = settings.EnableSoundEffects;
            
            // Difficulty Settings
            enemyHealthMultiplierSlider.Value = settings.EnemyHealthMultiplier;
            enemyHealthMultiplierTextBox.Text = settings.EnemyHealthMultiplier.ToString("F2");
            enemyDamageMultiplierSlider.Value = settings.EnemyDamageMultiplier;
            enemyDamageMultiplierTextBox.Text = settings.EnemyDamageMultiplier.ToString("F2");
            playerHealthMultiplierSlider.Value = settings.PlayerHealthMultiplier;
            playerHealthMultiplierTextBox.Text = settings.PlayerHealthMultiplier.ToString("F2");
            playerDamageMultiplierSlider.Value = settings.PlayerDamageMultiplier;
            playerDamageMultiplierTextBox.Text = settings.PlayerDamageMultiplier.ToString("F2");
            
            // UI Settings
            showHealthBarsCheckBox.IsChecked = settings.ShowHealthBars;
            showDamageNumbersCheckBox.IsChecked = settings.ShowDamageNumbers;
            showComboProgressCheckBox.IsChecked = settings.ShowComboProgress;
        }

        public void SaveSettings(
            Slider narrativeBalanceSlider,
            CheckBox enableNarrativeEventsCheckBox,
            CheckBox enableInformationalSummariesCheckBox,
            Slider combatSpeedSlider,
            CheckBox showIndividualActionMessagesCheckBox,
            CheckBox enableComboSystemCheckBox,
            CheckBox enableTextDisplayDelaysCheckBox,
            CheckBox fastCombatCheckBox,
            CheckBox enableAutoSaveCheckBox,
            TextBox autoSaveIntervalTextBox,
            CheckBox showDetailedStatsCheckBox,
            CheckBox enableSoundEffectsCheckBox,
            Slider enemyHealthMultiplierSlider,
            Slider enemyDamageMultiplierSlider,
            Slider playerHealthMultiplierSlider,
            Slider playerDamageMultiplierSlider,
            CheckBox showHealthBarsCheckBox,
            CheckBox showDamageNumbersCheckBox,
            CheckBox showComboProgressCheckBox,
            ActionDelegate? saveGameVariables = null)
        {
            try
            {
                // Narrative Settings
                settings.NarrativeBalance = narrativeBalanceSlider.Value;
                settings.EnableNarrativeEvents = enableNarrativeEventsCheckBox.IsChecked ?? true;
                settings.EnableInformationalSummaries = enableInformationalSummariesCheckBox.IsChecked ?? true;
                
                // Combat Settings
                settings.CombatSpeed = combatSpeedSlider.Value;
                settings.ShowIndividualActionMessages = showIndividualActionMessagesCheckBox.IsChecked ?? false;
                settings.EnableComboSystem = enableComboSystemCheckBox.IsChecked ?? true;
                settings.EnableTextDisplayDelays = enableTextDisplayDelaysCheckBox.IsChecked ?? true;
                settings.FastCombat = fastCombatCheckBox.IsChecked ?? false;
                
                // Gameplay Settings
                settings.EnableAutoSave = enableAutoSaveCheckBox.IsChecked ?? true;
                if (int.TryParse(autoSaveIntervalTextBox.Text, out int autoSaveInterval))
                {
                    settings.AutoSaveInterval = Math.Max(1, autoSaveInterval);
                }
                settings.ShowDetailedStats = showDetailedStatsCheckBox.IsChecked ?? true;
                settings.EnableSoundEffects = enableSoundEffectsCheckBox.IsChecked ?? false;
                
                // Difficulty Settings
                settings.EnemyHealthMultiplier = enemyHealthMultiplierSlider.Value;
                settings.EnemyDamageMultiplier = enemyDamageMultiplierSlider.Value;
                settings.PlayerHealthMultiplier = playerHealthMultiplierSlider.Value;
                settings.PlayerDamageMultiplier = playerDamageMultiplierSlider.Value;
                
                // UI Settings
                settings.ShowHealthBars = showHealthBarsCheckBox.IsChecked ?? true;
                settings.ShowDamageNumbers = showDamageNumbersCheckBox.IsChecked ?? true;
                settings.ShowComboProgress = showComboProgressCheckBox.IsChecked ?? true;
                
                // Save to file
                settings.SaveSettings();
                
                // Save Game Variables if any were modified
                saveGameVariables?.Invoke();
                
                showStatusMessage?.Invoke("Settings saved successfully!", true);
                updateStatus?.Invoke("Settings saved successfully!");
            }
            catch (Exception ex)
            {
                showStatusMessage?.Invoke($"Error saving settings: {ex.Message}", false);
                updateStatus?.Invoke($"Error saving settings: {ex.Message}");
            }
        }

        public void ResetToDefaults()
        {
            settings.ResetToDefaults();
        }

        /// <summary>
        /// Loads text delay settings from TextDelayConfiguration into UI controls
        /// </summary>
        public void LoadTextDelaySettings(
            CheckBox enableGuiDelaysCheckBox,
            CheckBox enableConsoleDelaysCheckBox,
            Slider actionDelaySlider,
            TextBox actionDelayTextBox,
            Slider messageDelaySlider,
            TextBox messageDelayTextBox,
            TextBox combatDelayTextBox,
            TextBox systemDelayTextBox,
            TextBox menuDelayTextBox,
            TextBox titleDelayTextBox,
            TextBox mainTitleDelayTextBox,
            TextBox environmentalDelayTextBox,
            TextBox effectMessageDelayTextBox,
            TextBox damageOverTimeDelayTextBox,
            TextBox encounterDelayTextBox,
            TextBox rollInfoDelayTextBox,
            TextBox baseMenuDelayTextBox,
            TextBox progressiveReductionRateTextBox,
            TextBox progressiveThresholdTextBox,
            TextBox combatPresetBaseDelayTextBox,
            TextBox combatPresetMinDelayTextBox,
            TextBox combatPresetMaxDelayTextBox,
            TextBox dungeonPresetBaseDelayTextBox,
            TextBox dungeonPresetMinDelayTextBox,
            TextBox dungeonPresetMaxDelayTextBox,
            TextBox roomPresetBaseDelayTextBox,
            TextBox roomPresetMinDelayTextBox,
            TextBox roomPresetMaxDelayTextBox,
            TextBox narrativePresetBaseDelayTextBox,
            TextBox narrativePresetMinDelayTextBox,
            TextBox narrativePresetMaxDelayTextBox,
            TextBox defaultPresetBaseDelayTextBox,
            TextBox defaultPresetMinDelayTextBox,
            TextBox defaultPresetMaxDelayTextBox)
        {
            try
            {
                // Enable flags
                enableGuiDelaysCheckBox.IsChecked = TextDelayConfiguration.GetEnableGuiDelays();
                enableConsoleDelaysCheckBox.IsChecked = TextDelayConfiguration.GetEnableConsoleDelays();
                
                // Combat delays
                int actionDelay = TextDelayConfiguration.GetActionDelayMs();
                actionDelaySlider.Value = actionDelay;
                actionDelayTextBox.Text = actionDelay.ToString();
                
                int messageDelay = TextDelayConfiguration.GetMessageDelayMs();
                messageDelaySlider.Value = messageDelay;
                messageDelayTextBox.Text = messageDelay.ToString();
                
                // Message type delays
                combatDelayTextBox.Text = TextDelayConfiguration.GetMessageTypeDelay(UIMessageType.Combat).ToString();
                systemDelayTextBox.Text = TextDelayConfiguration.GetMessageTypeDelay(UIMessageType.System).ToString();
                menuDelayTextBox.Text = TextDelayConfiguration.GetMessageTypeDelay(UIMessageType.Menu).ToString();
                titleDelayTextBox.Text = TextDelayConfiguration.GetMessageTypeDelay(UIMessageType.Title).ToString();
                mainTitleDelayTextBox.Text = TextDelayConfiguration.GetMessageTypeDelay(UIMessageType.MainTitle).ToString();
                environmentalDelayTextBox.Text = TextDelayConfiguration.GetMessageTypeDelay(UIMessageType.Environmental).ToString();
                effectMessageDelayTextBox.Text = TextDelayConfiguration.GetMessageTypeDelay(UIMessageType.EffectMessage).ToString();
                damageOverTimeDelayTextBox.Text = TextDelayConfiguration.GetMessageTypeDelay(UIMessageType.DamageOverTime).ToString();
                encounterDelayTextBox.Text = TextDelayConfiguration.GetMessageTypeDelay(UIMessageType.Encounter).ToString();
                rollInfoDelayTextBox.Text = TextDelayConfiguration.GetMessageTypeDelay(UIMessageType.RollInfo).ToString();
                
                // Progressive menu delays
                var progressiveMenuDelays = TextDelayConfiguration.GetProgressiveMenuDelays();
                baseMenuDelayTextBox.Text = progressiveMenuDelays.BaseMenuDelay.ToString();
                progressiveReductionRateTextBox.Text = progressiveMenuDelays.ProgressiveReductionRate.ToString();
                progressiveThresholdTextBox.Text = progressiveMenuDelays.ProgressiveThreshold.ToString();
                
                // Chunked text reveal presets
                var combatPreset = TextDelayConfiguration.GetChunkedTextRevealPreset("Combat");
                if (combatPreset != null)
                {
                    combatPresetBaseDelayTextBox.Text = combatPreset.BaseDelayPerCharMs.ToString();
                    combatPresetMinDelayTextBox.Text = combatPreset.MinDelayMs.ToString();
                    combatPresetMaxDelayTextBox.Text = combatPreset.MaxDelayMs.ToString();
                }
                
                var dungeonPreset = TextDelayConfiguration.GetChunkedTextRevealPreset("Dungeon");
                if (dungeonPreset != null)
                {
                    dungeonPresetBaseDelayTextBox.Text = dungeonPreset.BaseDelayPerCharMs.ToString();
                    dungeonPresetMinDelayTextBox.Text = dungeonPreset.MinDelayMs.ToString();
                    dungeonPresetMaxDelayTextBox.Text = dungeonPreset.MaxDelayMs.ToString();
                }
                
                var roomPreset = TextDelayConfiguration.GetChunkedTextRevealPreset("Room");
                if (roomPreset != null)
                {
                    roomPresetBaseDelayTextBox.Text = roomPreset.BaseDelayPerCharMs.ToString();
                    roomPresetMinDelayTextBox.Text = roomPreset.MinDelayMs.ToString();
                    roomPresetMaxDelayTextBox.Text = roomPreset.MaxDelayMs.ToString();
                }
                
                var narrativePreset = TextDelayConfiguration.GetChunkedTextRevealPreset("Narrative");
                if (narrativePreset != null)
                {
                    narrativePresetBaseDelayTextBox.Text = narrativePreset.BaseDelayPerCharMs.ToString();
                    narrativePresetMinDelayTextBox.Text = narrativePreset.MinDelayMs.ToString();
                    narrativePresetMaxDelayTextBox.Text = narrativePreset.MaxDelayMs.ToString();
                }
                
                var defaultPreset = TextDelayConfiguration.GetChunkedTextRevealPreset("Default");
                if (defaultPreset != null)
                {
                    defaultPresetBaseDelayTextBox.Text = defaultPreset.BaseDelayPerCharMs.ToString();
                    defaultPresetMinDelayTextBox.Text = defaultPreset.MinDelayMs.ToString();
                    defaultPresetMaxDelayTextBox.Text = defaultPreset.MaxDelayMs.ToString();
                }
            }
            catch (Exception ex)
            {
                showStatusMessage?.Invoke($"Error loading text delay settings: {ex.Message}", false);
            }
        }

        /// <summary>
        /// Saves text delay settings from UI controls to TextDelayConfiguration
        /// </summary>
        public void SaveTextDelaySettings(
            CheckBox enableGuiDelaysCheckBox,
            CheckBox enableConsoleDelaysCheckBox,
            Slider actionDelaySlider,
            Slider messageDelaySlider,
            TextBox combatDelayTextBox,
            TextBox systemDelayTextBox,
            TextBox menuDelayTextBox,
            TextBox titleDelayTextBox,
            TextBox mainTitleDelayTextBox,
            TextBox environmentalDelayTextBox,
            TextBox effectMessageDelayTextBox,
            TextBox damageOverTimeDelayTextBox,
            TextBox encounterDelayTextBox,
            TextBox rollInfoDelayTextBox,
            TextBox baseMenuDelayTextBox,
            TextBox progressiveReductionRateTextBox,
            TextBox progressiveThresholdTextBox,
            TextBox combatPresetBaseDelayTextBox,
            TextBox combatPresetMinDelayTextBox,
            TextBox combatPresetMaxDelayTextBox,
            TextBox dungeonPresetBaseDelayTextBox,
            TextBox dungeonPresetMinDelayTextBox,
            TextBox dungeonPresetMaxDelayTextBox,
            TextBox roomPresetBaseDelayTextBox,
            TextBox roomPresetMinDelayTextBox,
            TextBox roomPresetMaxDelayTextBox,
            TextBox narrativePresetBaseDelayTextBox,
            TextBox narrativePresetMinDelayTextBox,
            TextBox narrativePresetMaxDelayTextBox,
            TextBox defaultPresetBaseDelayTextBox,
            TextBox defaultPresetMinDelayTextBox,
            TextBox defaultPresetMaxDelayTextBox)
        {
            try
            {
                // Enable flags
                TextDelayConfiguration.SetEnableGuiDelays(enableGuiDelaysCheckBox.IsChecked ?? true);
                TextDelayConfiguration.SetEnableConsoleDelays(enableConsoleDelaysCheckBox.IsChecked ?? true);
                
                // Combat delays
                TextDelayConfiguration.SetActionDelayMs((int)actionDelaySlider.Value);
                TextDelayConfiguration.SetMessageDelayMs((int)messageDelaySlider.Value);
                
                // Message type delays
                if (int.TryParse(combatDelayTextBox.Text, out int combatDelay))
                    TextDelayConfiguration.SetMessageTypeDelay(UIMessageType.Combat, combatDelay);
                if (int.TryParse(systemDelayTextBox.Text, out int systemDelay))
                    TextDelayConfiguration.SetMessageTypeDelay(UIMessageType.System, systemDelay);
                if (int.TryParse(menuDelayTextBox.Text, out int menuDelay))
                    TextDelayConfiguration.SetMessageTypeDelay(UIMessageType.Menu, menuDelay);
                if (int.TryParse(titleDelayTextBox.Text, out int titleDelay))
                    TextDelayConfiguration.SetMessageTypeDelay(UIMessageType.Title, titleDelay);
                if (int.TryParse(mainTitleDelayTextBox.Text, out int mainTitleDelay))
                    TextDelayConfiguration.SetMessageTypeDelay(UIMessageType.MainTitle, mainTitleDelay);
                if (int.TryParse(environmentalDelayTextBox.Text, out int environmentalDelay))
                    TextDelayConfiguration.SetMessageTypeDelay(UIMessageType.Environmental, environmentalDelay);
                if (int.TryParse(effectMessageDelayTextBox.Text, out int effectMessageDelay))
                    TextDelayConfiguration.SetMessageTypeDelay(UIMessageType.EffectMessage, effectMessageDelay);
                if (int.TryParse(damageOverTimeDelayTextBox.Text, out int damageOverTimeDelay))
                    TextDelayConfiguration.SetMessageTypeDelay(UIMessageType.DamageOverTime, damageOverTimeDelay);
                if (int.TryParse(encounterDelayTextBox.Text, out int encounterDelay))
                    TextDelayConfiguration.SetMessageTypeDelay(UIMessageType.Encounter, encounterDelay);
                if (int.TryParse(rollInfoDelayTextBox.Text, out int rollInfoDelay))
                    TextDelayConfiguration.SetMessageTypeDelay(UIMessageType.RollInfo, rollInfoDelay);
                
                // Progressive menu delays
                if (int.TryParse(baseMenuDelayTextBox.Text, out int baseMenuDelay) &&
                    int.TryParse(progressiveReductionRateTextBox.Text, out int progressiveReductionRate) &&
                    int.TryParse(progressiveThresholdTextBox.Text, out int progressiveThreshold))
                {
                    var progressiveConfig = new ProgressiveMenuDelaysConfig
                    {
                        BaseMenuDelay = baseMenuDelay,
                        ProgressiveReductionRate = progressiveReductionRate,
                        ProgressiveThreshold = progressiveThreshold
                    };
                    TextDelayConfiguration.SetProgressiveMenuDelays(progressiveConfig);
                }
                
                // Chunked text reveal presets
                if (int.TryParse(combatPresetBaseDelayTextBox.Text, out int combatBase) &&
                    int.TryParse(combatPresetMinDelayTextBox.Text, out int combatMin) &&
                    int.TryParse(combatPresetMaxDelayTextBox.Text, out int combatMax))
                {
                    var combatPreset = TextDelayConfiguration.GetChunkedTextRevealPreset("Combat") ?? new ChunkedTextRevealPreset();
                    combatPreset.BaseDelayPerCharMs = combatBase;
                    combatPreset.MinDelayMs = combatMin;
                    combatPreset.MaxDelayMs = combatMax;
                    TextDelayConfiguration.SetChunkedTextRevealPreset("Combat", combatPreset);
                }
                
                if (int.TryParse(dungeonPresetBaseDelayTextBox.Text, out int dungeonBase) &&
                    int.TryParse(dungeonPresetMinDelayTextBox.Text, out int dungeonMin) &&
                    int.TryParse(dungeonPresetMaxDelayTextBox.Text, out int dungeonMax))
                {
                    var dungeonPreset = TextDelayConfiguration.GetChunkedTextRevealPreset("Dungeon") ?? new ChunkedTextRevealPreset();
                    dungeonPreset.BaseDelayPerCharMs = dungeonBase;
                    dungeonPreset.MinDelayMs = dungeonMin;
                    dungeonPreset.MaxDelayMs = dungeonMax;
                    TextDelayConfiguration.SetChunkedTextRevealPreset("Dungeon", dungeonPreset);
                }
                
                if (int.TryParse(roomPresetBaseDelayTextBox.Text, out int roomBase) &&
                    int.TryParse(roomPresetMinDelayTextBox.Text, out int roomMin) &&
                    int.TryParse(roomPresetMaxDelayTextBox.Text, out int roomMax))
                {
                    var roomPreset = TextDelayConfiguration.GetChunkedTextRevealPreset("Room") ?? new ChunkedTextRevealPreset();
                    roomPreset.BaseDelayPerCharMs = roomBase;
                    roomPreset.MinDelayMs = roomMin;
                    roomPreset.MaxDelayMs = roomMax;
                    TextDelayConfiguration.SetChunkedTextRevealPreset("Room", roomPreset);
                }
                
                if (int.TryParse(narrativePresetBaseDelayTextBox.Text, out int narrativeBase) &&
                    int.TryParse(narrativePresetMinDelayTextBox.Text, out int narrativeMin) &&
                    int.TryParse(narrativePresetMaxDelayTextBox.Text, out int narrativeMax))
                {
                    var narrativePreset = TextDelayConfiguration.GetChunkedTextRevealPreset("Narrative") ?? new ChunkedTextRevealPreset();
                    narrativePreset.BaseDelayPerCharMs = narrativeBase;
                    narrativePreset.MinDelayMs = narrativeMin;
                    narrativePreset.MaxDelayMs = narrativeMax;
                    TextDelayConfiguration.SetChunkedTextRevealPreset("Narrative", narrativePreset);
                }
                
                if (int.TryParse(defaultPresetBaseDelayTextBox.Text, out int defaultBase) &&
                    int.TryParse(defaultPresetMinDelayTextBox.Text, out int defaultMin) &&
                    int.TryParse(defaultPresetMaxDelayTextBox.Text, out int defaultMax))
                {
                    var defaultPreset = TextDelayConfiguration.GetChunkedTextRevealPreset("Default") ?? new ChunkedTextRevealPreset();
                    defaultPreset.BaseDelayPerCharMs = defaultBase;
                    defaultPreset.MinDelayMs = defaultMin;
                    defaultPreset.MaxDelayMs = defaultMax;
                    TextDelayConfiguration.SetChunkedTextRevealPreset("Default", defaultPreset);
                }
            }
            catch (Exception ex)
            {
                showStatusMessage?.Invoke($"Error saving text delay settings: {ex.Message}", false);
            }
        }
    }
}

