using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.Threading;
using Avalonia.VisualTree;
using RPGGame;
using RPGGame.UI.Avalonia.Managers;
using RPGGame.UI.Avalonia.Helpers;
using System;
using System.Threading.Tasks;
using System.Timers;

namespace RPGGame.UI.Avalonia
{
    public partial class SettingsPanel : UserControl
    {
        private GameSettings settings;
        private System.Action? onBack;
        private System.Action<string>? updateStatus;
        
        // Handler references for testing and developer tools
        private GameCoordinator? gameCoordinator;
        private CanvasUICoordinator? canvasUI;
        private GameStateManager? gameStateManager;
        
        // Managers
        private GameVariablesTabManager? gameVariablesTabManager;
        private ActionsTabManager? actionsTabManager;
        private BattleStatisticsTabManager? battleStatisticsTabManager;
        private SettingsManager? settingsManager;
        private TestExecutionManager? testExecutionManager;
        
        // Extracted managers
        private SettingsPersistenceManager? persistenceManager;
        private SettingsTestExecutor? testExecutor;
        private SettingsEventWiring? eventWiring;
        private SettingsInitialization? initialization;
        
        public SettingsPanel()
        {
            InitializeComponent();
            settings = GameSettings.Instance;
            InitializeManagers();
            LoadSettings();
            WireUpEvents();
        }
        
        private void InitializeManagers()
        {
            settingsManager = new SettingsManager(settings, ShowStatusMessage, updateStatus);
            gameVariablesTabManager = new GameVariablesTabManager();
            actionsTabManager = new ActionsTabManager();
            battleStatisticsTabManager = new BattleStatisticsTabManager();
            
            // Initialize extracted managers
            persistenceManager = new SettingsPersistenceManager(settingsManager, gameVariablesTabManager);
            initialization = new SettingsInitialization(
                settingsManager,
                gameVariablesTabManager,
                actionsTabManager,
                battleStatisticsTabManager,
                ShowStatusMessage);
            
            // Wire up animation configuration updates for real-time changes
            initialization.InitializeAnimationConfiguration();
        }
        
        /// <summary>
        /// Sets the callback for when back button is clicked
        /// </summary>
        public void SetBackCallback(System.Action callback)
        {
            onBack = callback;
        }
        
        /// <summary>
        /// Sets the callback for status updates
        /// </summary>
        public void SetStatusCallback(System.Action<string> callback)
        {
            updateStatus = callback;
        }
        
        /// <summary>
        /// Initializes handlers for testing and developer tools
        /// </summary>
        public void InitializeHandlers(TestingSystemHandler? testingHandler, DeveloperMenuHandler? developerHandler, GameCoordinator? game, CanvasUICoordinator? ui, GameStateManager? stateManager)
        {
            gameCoordinator = game;
            canvasUI = ui;
            gameStateManager = stateManager;
            
            if (canvasUI == null)
            {
                var uiManager = RPGGame.UIManager.GetCustomUIManager();
                canvasUI = uiManager as CanvasUICoordinator;
            }
            
            // Initialize test execution manager
            testExecutionManager = new TestExecutionManager(
                canvasUI,
                gameStateManager,
                TestOutputTextBlock,
                TestOutputScrollViewer,
                ShowStatusMessage);
            
            // Initialize test executor
            testExecutor = new SettingsTestExecutor(
                testExecutionManager,
                battleStatisticsTabManager,
                gameCoordinator,
                canvasUI,
                ShowStatusMessage);
            
            // Initialize tab managers
            if (initialization != null)
            {
                initialization.InitializeHandlers(
                    testExecutionManager,
                    testExecutor,
                    GameVariablesCategoryListBox,
                    GameVariablesPanel,
                    ActionsListBox,
                    ActionFormPanel,
                    CreateActionButton,
                    DeleteActionButton,
                    this);
            }
        }
        
        /// <summary>
        /// Loads current settings into the UI controls
        /// </summary>
        private void LoadSettings()
        {
            if (persistenceManager == null) return;
            
            persistenceManager.LoadSettings(
                NarrativeBalanceSlider,
                NarrativeBalanceTextBox,
                EnableNarrativeEventsCheckBox,
                EnableInformationalSummariesCheckBox,
                CombatSpeedSlider,
                CombatSpeedTextBox,
                ShowIndividualActionMessagesCheckBox,
                EnableComboSystemCheckBox,
                EnableTextDisplayDelaysCheckBox,
                FastCombatCheckBox,
                EnableAutoSaveCheckBox,
                AutoSaveIntervalTextBox,
                ShowDetailedStatsCheckBox,
                EnableSoundEffectsCheckBox,
                EnemyHealthMultiplierSlider,
                EnemyHealthMultiplierTextBox,
                EnemyDamageMultiplierSlider,
                EnemyDamageMultiplierTextBox,
                PlayerHealthMultiplierSlider,
                PlayerHealthMultiplierTextBox,
                PlayerDamageMultiplierSlider,
                PlayerDamageMultiplierTextBox,
                ShowHealthBarsCheckBox,
                ShowDamageNumbersCheckBox,
                ShowComboProgressCheckBox);
            
            // Load text delay settings
            LoadTextDelaySettings();
            
            // Load animation settings
            LoadAnimationSettings();
        }
        
        /// <summary>
        /// Loads animation settings into UI controls
        /// </summary>
        private void LoadAnimationSettings()
        {
            if (persistenceManager == null) return;
            
            persistenceManager.LoadAnimationSettings(
                BrightnessMaskEnabledCheckBox,
                BrightnessMaskIntensitySlider,
                BrightnessMaskIntensityTextBox,
                BrightnessMaskWaveLengthSlider,
                BrightnessMaskWaveLengthTextBox,
                BrightnessMaskUpdateIntervalTextBox,
                UndulationSpeedSlider,
                UndulationSpeedTextBox,
                UndulationWaveLengthSlider,
                UndulationWaveLengthTextBox,
                UndulationIntervalTextBox);
        }
        
        /// <summary>
        /// Loads text delay settings into UI controls
        /// </summary>
        private void LoadTextDelaySettings()
        {
            if (persistenceManager == null) return;
            
            var controls = TextDelayControlsHelper.FindControls(this);
            
            persistenceManager.LoadTextDelaySettings(
                controls.EnableGuiDelaysCheckBox,
                controls.EnableConsoleDelaysCheckBox,
                controls.ActionDelaySlider,
                controls.ActionDelayTextBox,
                controls.MessageDelaySlider,
                controls.MessageDelayTextBox,
                controls.CombatDelayTextBox,
                controls.SystemDelayTextBox,
                controls.MenuDelayTextBox,
                controls.TitleDelayTextBox,
                controls.MainTitleDelayTextBox,
                controls.EnvironmentalDelayTextBox,
                controls.EffectMessageDelayTextBox,
                controls.DamageOverTimeDelayTextBox,
                controls.EncounterDelayTextBox,
                controls.RollInfoDelayTextBox,
                controls.BaseMenuDelayTextBox,
                controls.ProgressiveReductionRateTextBox,
                controls.ProgressiveThresholdTextBox,
                controls.CombatPresetBaseDelayTextBox,
                controls.CombatPresetMinDelayTextBox,
                controls.CombatPresetMaxDelayTextBox,
                controls.DungeonPresetBaseDelayTextBox,
                controls.DungeonPresetMinDelayTextBox,
                controls.DungeonPresetMaxDelayTextBox,
                controls.RoomPresetBaseDelayTextBox,
                controls.RoomPresetMinDelayTextBox,
                controls.RoomPresetMaxDelayTextBox,
                controls.NarrativePresetBaseDelayTextBox,
                controls.NarrativePresetMinDelayTextBox,
                controls.NarrativePresetMaxDelayTextBox,
                controls.DefaultPresetBaseDelayTextBox,
                controls.DefaultPresetMinDelayTextBox,
                controls.DefaultPresetMaxDelayTextBox,
                (slider, textBox) => { /* Action delay slider wired in manager */ },
                (slider, textBox) => { /* Message delay slider wired in manager */ });
        }
        
        /// <summary>
        /// Wires up event handlers for controls
        /// </summary>
        private void WireUpEvents()
        {
            if (eventWiring == null)
            {
                eventWiring = new SettingsEventWiring(
                    async (key) => 
                    {
                        if (testExecutor != null)
                            await testExecutor.ExecuteTest(key);
                    },
                    async (count) => 
                    {
                        if (testExecutor != null)
                            await testExecutor.RunBattleTest(count);
                    },
                    async () => 
                    {
                        if (testExecutor != null)
                            await testExecutor.RunWeaponTypeTests();
                    },
                    async () => 
                    {
                        if (testExecutor != null)
                            await testExecutor.RunComprehensiveWeaponEnemyTests();
                    },
                    (System.Action)(() => OnSaveButtonClick(null, null!)),
                    (System.Action)(() => OnResetButtonClick(null, null!)),
                    (System.Action)(() => OnBackButtonClick(null, null!)),
                    (System.Action)(() => 
                    {
                        if (testExecutor != null)
                            testExecutor.ShowBattleStatistics();
                    }));
            }
            
            eventWiring.WireUpAllEvents(
                this,
                NarrativeBalanceSlider,
                NarrativeBalanceTextBox,
                CombatSpeedSlider,
                CombatSpeedTextBox,
                EnemyHealthMultiplierSlider,
                EnemyHealthMultiplierTextBox,
                EnemyDamageMultiplierSlider,
                EnemyDamageMultiplierTextBox,
                PlayerHealthMultiplierSlider,
                PlayerHealthMultiplierTextBox,
                PlayerDamageMultiplierSlider,
                PlayerDamageMultiplierTextBox,
                BrightnessMaskIntensitySlider,
                BrightnessMaskIntensityTextBox,
                BrightnessMaskWaveLengthSlider,
                BrightnessMaskWaveLengthTextBox,
                UndulationSpeedSlider,
                UndulationSpeedTextBox,
                UndulationWaveLengthSlider,
                UndulationWaveLengthTextBox,
                SaveButton,
                ResetButton,
                BackButton,
                RunAllTestsButton,
                ColorSystemTestsButton,
                CharacterSystemTestsButton,
                CombatSystemTestsButton,
                InventoryDungeonTestsButton,
                DataUITestsButton,
                ActionSystemTestsButton,
                AdvancedIntegrationTestsButton,
                CombatLogFilteringTestsButton,
                GenerateRandomItemsButton,
                ItemGenerationAnalysisButton,
                TierDistributionTestButton,
                CommonItemModificationTestButton,
                ActionEditorTestsButton);
        }
        
        /// <summary>
        /// Saves current UI values to settings
        /// </summary>
        private void SaveSettings()
        {
            if (persistenceManager == null) return;
            
            persistenceManager.SaveSettings(
                NarrativeBalanceSlider,
                EnableNarrativeEventsCheckBox,
                EnableInformationalSummariesCheckBox,
                CombatSpeedSlider,
                ShowIndividualActionMessagesCheckBox,
                EnableComboSystemCheckBox,
                EnableTextDisplayDelaysCheckBox,
                FastCombatCheckBox,
                EnableAutoSaveCheckBox,
                AutoSaveIntervalTextBox,
                ShowDetailedStatsCheckBox,
                EnableSoundEffectsCheckBox,
                EnemyHealthMultiplierSlider,
                EnemyDamageMultiplierSlider,
                PlayerHealthMultiplierSlider,
                PlayerDamageMultiplierSlider,
                ShowHealthBarsCheckBox,
                ShowDamageNumbersCheckBox,
                ShowComboProgressCheckBox);
            
            // Save text delay settings
            SaveTextDelaySettings();
            
            // Save animation settings
            SaveAnimationSettings();
        }
        
        /// <summary>
        /// Saves animation settings from UI controls
        /// </summary>
        private void SaveAnimationSettings()
        {
            if (persistenceManager == null) return;
            
            persistenceManager.SaveAnimationSettings(
                BrightnessMaskEnabledCheckBox,
                BrightnessMaskIntensitySlider,
                BrightnessMaskWaveLengthSlider,
                BrightnessMaskUpdateIntervalTextBox,
                UndulationSpeedSlider,
                UndulationWaveLengthSlider,
                UndulationIntervalTextBox);
        }
        
        /// <summary>
        /// Saves text delay settings from UI controls
        /// </summary>
        private void SaveTextDelaySettings()
        {
            if (persistenceManager == null) return;
            
            var controls = TextDelayControlsHelper.FindControls(this);
            
            persistenceManager.SaveTextDelaySettings(
                controls.EnableGuiDelaysCheckBox,
                controls.EnableConsoleDelaysCheckBox,
                controls.ActionDelaySlider,
                controls.MessageDelaySlider,
                controls.CombatDelayTextBox,
                controls.SystemDelayTextBox,
                controls.MenuDelayTextBox,
                controls.TitleDelayTextBox,
                controls.MainTitleDelayTextBox,
                controls.EnvironmentalDelayTextBox,
                controls.EffectMessageDelayTextBox,
                controls.DamageOverTimeDelayTextBox,
                controls.EncounterDelayTextBox,
                controls.RollInfoDelayTextBox,
                controls.BaseMenuDelayTextBox,
                controls.ProgressiveReductionRateTextBox,
                controls.ProgressiveThresholdTextBox,
                controls.CombatPresetBaseDelayTextBox,
                controls.CombatPresetMinDelayTextBox,
                controls.CombatPresetMaxDelayTextBox,
                controls.DungeonPresetBaseDelayTextBox,
                controls.DungeonPresetMinDelayTextBox,
                controls.DungeonPresetMaxDelayTextBox,
                controls.RoomPresetBaseDelayTextBox,
                controls.RoomPresetMinDelayTextBox,
                controls.RoomPresetMaxDelayTextBox,
                controls.NarrativePresetBaseDelayTextBox,
                controls.NarrativePresetMinDelayTextBox,
                controls.NarrativePresetMaxDelayTextBox,
                controls.DefaultPresetBaseDelayTextBox,
                controls.DefaultPresetMinDelayTextBox,
                controls.DefaultPresetMaxDelayTextBox);
        }
        
        /// <summary>
        /// Resets settings to defaults
        /// </summary>
        private void OnResetButtonClick(object? sender, RoutedEventArgs e)
        {
            if (persistenceManager == null) return;
            
            persistenceManager.ResetToDefaults();
            LoadSettings();
            ShowStatusMessage("Settings reset to defaults", true);
            updateStatus?.Invoke("Settings reset to defaults");
        }
        
        /// <summary>
        /// Saves and closes settings panel
        /// </summary>
        private void OnSaveButtonClick(object? sender, RoutedEventArgs e)
        {
            SaveSettings();
        }
        
        /// <summary>
        /// Closes settings panel without saving
        /// </summary>
        private void OnBackButtonClick(object? sender, RoutedEventArgs e)
        {
            onBack?.Invoke();
        }
        
        /// <summary>
        /// Shows a status message
        /// </summary>
        private void ShowStatusMessage(string message, bool isSuccess)
        {
            StatusMessage.Text = message;
            StatusMessage.Foreground = isSuccess 
                ? new SolidColorBrush(Color.FromRgb(76, 175, 80))
                : new SolidColorBrush(Color.FromRgb(244, 67, 54));
            StatusMessage.IsVisible = true;
            
            var timer = new System.Timers.Timer(3000);
            timer.Elapsed += (s, e) =>
            {
                timer.Stop();
                Dispatcher.UIThread.Post(() =>
                {
                    StatusMessage.IsVisible = false;
                });
            };
            timer.Start();
        }
        
    }
}
