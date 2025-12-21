using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.Threading;
using RPGGame;
using RPGGame.UI.Avalonia.Managers;
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
        private SettingsEventManager? settingsEventManager;
        
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
            
            // Initialize tab managers
            InitializeGameVariablesTab();
            InitializeActionsTab();
            InitializeBattleStatisticsTab();
        }
        
        private void InitializeGameVariablesTab()
        {
            if (gameVariablesTabManager != null)
            {
                gameVariablesTabManager.Initialize(
                    GameVariablesCategoryListBox,
                    GameVariablesPanel,
                    ShowStatusMessage);
            }
        }
        
        private void InitializeActionsTab()
        {
            if (actionsTabManager != null)
            {
                actionsTabManager.Initialize(
                    ActionsListBox,
                    ActionFormPanel,
                    CreateActionButton,
                    DeleteActionButton,
                    ShowStatusMessage);
            }
        }
        
        private void InitializeBattleStatisticsTab()
        {
            var progressBorder = this.FindControl<Border>("ProgressBorder");
            var progressBar = this.FindControl<ProgressBar>("ProgressBar");
            var progressStatusText = this.FindControl<TextBlock>("ProgressStatusText");
            var progressPercentageText = this.FindControl<TextBlock>("ProgressPercentageText");
            var battleStatisticsResultsText = this.FindControl<TextBlock>("BattleStatisticsResultsText");
            
            if (battleStatisticsTabManager != null && progressBorder != null && progressBar != null && 
                progressStatusText != null && progressPercentageText != null && battleStatisticsResultsText != null)
            {
                battleStatisticsTabManager.Initialize(
                    progressBorder,
                    progressBar,
                    progressStatusText,
                    progressPercentageText,
                    battleStatisticsResultsText,
                    ShowStatusMessage);
            }
        }
        
        /// <summary>
        /// Loads current settings into the UI controls
        /// </summary>
        private void LoadSettings()
        {
            if (settingsManager == null) return;
            
            settingsManager.LoadSettings(
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
        }
        
        /// <summary>
        /// Wires up event handlers for controls
        /// </summary>
        private void WireUpEvents()
        {
            if (settingsEventManager == null)
            {
                settingsEventManager = new SettingsEventManager(
                    async (key) => await ExecuteTest(key),
                    async (count) => await RunBattleTest(count),
                    async () => await RunWeaponTypeTests(),
                    async () => await RunComprehensiveWeaponEnemyTests(),
                    () => OnSaveButtonClick(null, null!),
                    () => OnResetButtonClick(null, null!),
                    () => OnBackButtonClick(null, null!),
                    () => OnBattleStatisticsClick(null, null!));
            }
            
            settingsEventManager.WireUpSliderEvents(
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
                PlayerDamageMultiplierTextBox);
            
            settingsEventManager.WireUpTextBoxEvents(
                NarrativeBalanceTextBox,
                NarrativeBalanceSlider,
                CombatSpeedTextBox,
                CombatSpeedSlider,
                EnemyHealthMultiplierTextBox,
                EnemyHealthMultiplierSlider,
                EnemyDamageMultiplierTextBox,
                EnemyDamageMultiplierSlider,
                PlayerHealthMultiplierTextBox,
                PlayerHealthMultiplierSlider,
                PlayerDamageMultiplierTextBox,
                PlayerDamageMultiplierSlider);
            
            settingsEventManager.WireUpButtonEvents(
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
                GenerateRandomItemsButton,
                ItemGenerationAnalysisButton,
                TierDistributionTestButton,
                CommonItemModificationTestButton,
                ActionEditorTestsButton,
                this.FindControl<Button>("QuickTestButton"),
                this.FindControl<Button>("StandardTestButton"),
                this.FindControl<Button>("ComprehensiveTestButton"),
                this.FindControl<Button>("WeaponTypeTestButton"),
                this.FindControl<Button>("ComprehensiveWeaponEnemyTestButton"),
                this.FindControl<Button>("BattleStatisticsButton"));
        }
        
        /// <summary>
        /// Saves current UI values to settings
        /// </summary>
        private void SaveSettings()
        {
            if (settingsManager == null) return;
            
            settingsManager.SaveSettings(
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
                ShowComboProgressCheckBox,
                () => gameVariablesTabManager?.SaveGameVariables());
        }
        
        /// <summary>
        /// Resets settings to defaults
        /// </summary>
        private void OnResetButtonClick(object? sender, RoutedEventArgs e)
        {
            if (settingsManager == null) return;
            
            settingsManager.ResetToDefaults();
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
        
        /// <summary>
        /// Executes a test by command key
        /// </summary>
        private Task ExecuteTest(string commandKey)
        {
            if (testExecutionManager == null)
            {
                ShowStatusMessage("Test execution manager not initialized", false);
                return Task.CompletedTask;
            }
            
            return testExecutionManager.ExecuteTest(commandKey);
        }
        
        /// <summary>
        /// Gets the canvas UI, trying stored reference first, then UIManager fallback
        /// </summary>
        private CanvasUICoordinator? GetCanvasUI()
        {
            if (canvasUI != null)
                return canvasUI;
            
            var uiManager = RPGGame.UIManager.GetCustomUIManager();
            return uiManager as CanvasUICoordinator;
        }
        
        /// <summary>
        /// Opens battle statistics while keeping settings panel open
        /// </summary>
        private void OnBattleStatisticsClick(object? sender, RoutedEventArgs e)
        {
            var uiToUse = GetCanvasUI();
            if (gameCoordinator == null || uiToUse == null)
            {
                ShowStatusMessage("Developer tools not available - game may not be fully initialized", false);
                return;
            }
            
            uiToUse.RestoreDisplayBufferRendering();
            gameCoordinator.ShowBattleStatistics();
        }
        
        /// <summary>
        /// Runs a battle test with the specified number of battles
        /// </summary>
        private async Task RunBattleTest(int numberOfBattles)
        {
            if (battleStatisticsTabManager == null)
            {
                ShowStatusMessage("Battle statistics manager not initialized", false);
                return;
            }
            
            await battleStatisticsTabManager.RunBattleTest(numberOfBattles);
        }
        
        /// <summary>
        /// Runs weapon type tests
        /// </summary>
        private async Task RunWeaponTypeTests()
        {
            if (battleStatisticsTabManager == null)
            {
                ShowStatusMessage("Battle statistics manager not initialized", false);
                return;
            }
            
            await battleStatisticsTabManager.RunWeaponTypeTests();
        }
        
        /// <summary>
        /// Runs comprehensive weapon-enemy tests
        /// </summary>
        private async Task RunComprehensiveWeaponEnemyTests()
        {
            if (battleStatisticsTabManager == null)
            {
                ShowStatusMessage("Battle statistics manager not initialized", false);
                return;
            }
            
            await battleStatisticsTabManager.RunComprehensiveWeaponEnemyTests();
        }
    }
}
