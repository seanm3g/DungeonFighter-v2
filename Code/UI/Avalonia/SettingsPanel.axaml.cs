using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.Threading;
using Avalonia.VisualTree;
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
            
            // Load text delay settings
            LoadTextDelaySettings();
        }
        
        /// <summary>
        /// Loads text delay settings into UI controls
        /// </summary>
        private void LoadTextDelaySettings()
        {
            if (settingsManager == null) return;
            
            var enableGuiDelaysCheckBox = this.FindControl<CheckBox>("EnableGuiDelaysCheckBox");
            var enableConsoleDelaysCheckBox = this.FindControl<CheckBox>("EnableConsoleDelaysCheckBox");
            var actionDelaySlider = this.FindControl<Slider>("ActionDelaySlider");
            var actionDelayTextBox = this.FindControl<TextBox>("ActionDelayTextBox");
            var messageDelaySlider = this.FindControl<Slider>("MessageDelaySlider");
            var messageDelayTextBox = this.FindControl<TextBox>("MessageDelayTextBox");
            var combatDelayTextBox = this.FindControl<TextBox>("CombatDelayTextBox");
            var systemDelayTextBox = this.FindControl<TextBox>("SystemDelayTextBox");
            var menuDelayTextBox = this.FindControl<TextBox>("MenuDelayTextBox");
            var titleDelayTextBox = this.FindControl<TextBox>("TitleDelayTextBox");
            var mainTitleDelayTextBox = this.FindControl<TextBox>("MainTitleDelayTextBox");
            var environmentalDelayTextBox = this.FindControl<TextBox>("EnvironmentalDelayTextBox");
            var effectMessageDelayTextBox = this.FindControl<TextBox>("EffectMessageDelayTextBox");
            var damageOverTimeDelayTextBox = this.FindControl<TextBox>("DamageOverTimeDelayTextBox");
            var encounterDelayTextBox = this.FindControl<TextBox>("EncounterDelayTextBox");
            var rollInfoDelayTextBox = this.FindControl<TextBox>("RollInfoDelayTextBox");
            var baseMenuDelayTextBox = this.FindControl<TextBox>("BaseMenuDelayTextBox");
            var progressiveReductionRateTextBox = this.FindControl<TextBox>("ProgressiveReductionRateTextBox");
            var progressiveThresholdTextBox = this.FindControl<TextBox>("ProgressiveThresholdTextBox");
            var combatPresetBaseDelayTextBox = this.FindControl<TextBox>("CombatPresetBaseDelayTextBox");
            var combatPresetMinDelayTextBox = this.FindControl<TextBox>("CombatPresetMinDelayTextBox");
            var combatPresetMaxDelayTextBox = this.FindControl<TextBox>("CombatPresetMaxDelayTextBox");
            var dungeonPresetBaseDelayTextBox = this.FindControl<TextBox>("DungeonPresetBaseDelayTextBox");
            var dungeonPresetMinDelayTextBox = this.FindControl<TextBox>("DungeonPresetMinDelayTextBox");
            var dungeonPresetMaxDelayTextBox = this.FindControl<TextBox>("DungeonPresetMaxDelayTextBox");
            var roomPresetBaseDelayTextBox = this.FindControl<TextBox>("RoomPresetBaseDelayTextBox");
            var roomPresetMinDelayTextBox = this.FindControl<TextBox>("RoomPresetMinDelayTextBox");
            var roomPresetMaxDelayTextBox = this.FindControl<TextBox>("RoomPresetMaxDelayTextBox");
            var narrativePresetBaseDelayTextBox = this.FindControl<TextBox>("NarrativePresetBaseDelayTextBox");
            var narrativePresetMinDelayTextBox = this.FindControl<TextBox>("NarrativePresetMinDelayTextBox");
            var narrativePresetMaxDelayTextBox = this.FindControl<TextBox>("NarrativePresetMaxDelayTextBox");
            var defaultPresetBaseDelayTextBox = this.FindControl<TextBox>("DefaultPresetBaseDelayTextBox");
            var defaultPresetMinDelayTextBox = this.FindControl<TextBox>("DefaultPresetMinDelayTextBox");
            var defaultPresetMaxDelayTextBox = this.FindControl<TextBox>("DefaultPresetMaxDelayTextBox");
            
            if (enableGuiDelaysCheckBox != null && enableConsoleDelaysCheckBox != null &&
                actionDelaySlider != null && actionDelayTextBox != null &&
                messageDelaySlider != null && messageDelayTextBox != null &&
                combatDelayTextBox != null && systemDelayTextBox != null &&
                menuDelayTextBox != null && titleDelayTextBox != null &&
                mainTitleDelayTextBox != null && environmentalDelayTextBox != null &&
                effectMessageDelayTextBox != null && damageOverTimeDelayTextBox != null &&
                encounterDelayTextBox != null && rollInfoDelayTextBox != null &&
                baseMenuDelayTextBox != null && progressiveReductionRateTextBox != null &&
                progressiveThresholdTextBox != null && combatPresetBaseDelayTextBox != null &&
                combatPresetMinDelayTextBox != null && combatPresetMaxDelayTextBox != null &&
                dungeonPresetBaseDelayTextBox != null && dungeonPresetMinDelayTextBox != null &&
                dungeonPresetMaxDelayTextBox != null && roomPresetBaseDelayTextBox != null &&
                roomPresetMinDelayTextBox != null && roomPresetMaxDelayTextBox != null &&
                narrativePresetBaseDelayTextBox != null && narrativePresetMinDelayTextBox != null &&
                narrativePresetMaxDelayTextBox != null && defaultPresetBaseDelayTextBox != null &&
                defaultPresetMinDelayTextBox != null && defaultPresetMaxDelayTextBox != null)
            {
                settingsManager.LoadTextDelaySettings(
                    enableGuiDelaysCheckBox,
                    enableConsoleDelaysCheckBox,
                    actionDelaySlider,
                    actionDelayTextBox,
                    messageDelaySlider,
                    messageDelayTextBox,
                    combatDelayTextBox,
                    systemDelayTextBox,
                    menuDelayTextBox,
                    titleDelayTextBox,
                    mainTitleDelayTextBox,
                    environmentalDelayTextBox,
                    effectMessageDelayTextBox,
                    damageOverTimeDelayTextBox,
                    encounterDelayTextBox,
                    rollInfoDelayTextBox,
                    baseMenuDelayTextBox,
                    progressiveReductionRateTextBox,
                    progressiveThresholdTextBox,
                    combatPresetBaseDelayTextBox,
                    combatPresetMinDelayTextBox,
                    combatPresetMaxDelayTextBox,
                    dungeonPresetBaseDelayTextBox,
                    dungeonPresetMinDelayTextBox,
                    dungeonPresetMaxDelayTextBox,
                    roomPresetBaseDelayTextBox,
                    roomPresetMinDelayTextBox,
                    roomPresetMaxDelayTextBox,
                    narrativePresetBaseDelayTextBox,
                    narrativePresetMinDelayTextBox,
                    narrativePresetMaxDelayTextBox,
                    defaultPresetBaseDelayTextBox,
                    defaultPresetMinDelayTextBox,
                    defaultPresetMaxDelayTextBox);
                
                // Wire up slider events for action/message delays
                actionDelaySlider.ValueChanged += (s, e) =>
                {
                    if (actionDelayTextBox != null)
                        actionDelayTextBox.Text = ((int)actionDelaySlider.Value).ToString();
                };
                
                messageDelaySlider.ValueChanged += (s, e) =>
                {
                    if (messageDelayTextBox != null)
                        messageDelayTextBox.Text = ((int)messageDelaySlider.Value).ToString();
                };
            }
        }
        
        /// <summary>
        /// Wires up event handlers for controls
        /// </summary>
        private void WireUpEvents()
        {
            // Fix TextBox focus styling to prevent white-on-white text
            FixTextBoxFocusStyling();
            
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
            
            // Save text delay settings
            SaveTextDelaySettings();
        }
        
        /// <summary>
        /// Saves text delay settings from UI controls
        /// </summary>
        private void SaveTextDelaySettings()
        {
            if (settingsManager == null) return;
            
            var enableGuiDelaysCheckBox = this.FindControl<CheckBox>("EnableGuiDelaysCheckBox");
            var enableConsoleDelaysCheckBox = this.FindControl<CheckBox>("EnableConsoleDelaysCheckBox");
            var actionDelaySlider = this.FindControl<Slider>("ActionDelaySlider");
            var messageDelaySlider = this.FindControl<Slider>("MessageDelaySlider");
            var combatDelayTextBox = this.FindControl<TextBox>("CombatDelayTextBox");
            var systemDelayTextBox = this.FindControl<TextBox>("SystemDelayTextBox");
            var menuDelayTextBox = this.FindControl<TextBox>("MenuDelayTextBox");
            var titleDelayTextBox = this.FindControl<TextBox>("TitleDelayTextBox");
            var mainTitleDelayTextBox = this.FindControl<TextBox>("MainTitleDelayTextBox");
            var environmentalDelayTextBox = this.FindControl<TextBox>("EnvironmentalDelayTextBox");
            var effectMessageDelayTextBox = this.FindControl<TextBox>("EffectMessageDelayTextBox");
            var damageOverTimeDelayTextBox = this.FindControl<TextBox>("DamageOverTimeDelayTextBox");
            var encounterDelayTextBox = this.FindControl<TextBox>("EncounterDelayTextBox");
            var rollInfoDelayTextBox = this.FindControl<TextBox>("RollInfoDelayTextBox");
            var baseMenuDelayTextBox = this.FindControl<TextBox>("BaseMenuDelayTextBox");
            var progressiveReductionRateTextBox = this.FindControl<TextBox>("ProgressiveReductionRateTextBox");
            var progressiveThresholdTextBox = this.FindControl<TextBox>("ProgressiveThresholdTextBox");
            var combatPresetBaseDelayTextBox = this.FindControl<TextBox>("CombatPresetBaseDelayTextBox");
            var combatPresetMinDelayTextBox = this.FindControl<TextBox>("CombatPresetMinDelayTextBox");
            var combatPresetMaxDelayTextBox = this.FindControl<TextBox>("CombatPresetMaxDelayTextBox");
            var dungeonPresetBaseDelayTextBox = this.FindControl<TextBox>("DungeonPresetBaseDelayTextBox");
            var dungeonPresetMinDelayTextBox = this.FindControl<TextBox>("DungeonPresetMinDelayTextBox");
            var dungeonPresetMaxDelayTextBox = this.FindControl<TextBox>("DungeonPresetMaxDelayTextBox");
            var roomPresetBaseDelayTextBox = this.FindControl<TextBox>("RoomPresetBaseDelayTextBox");
            var roomPresetMinDelayTextBox = this.FindControl<TextBox>("RoomPresetMinDelayTextBox");
            var roomPresetMaxDelayTextBox = this.FindControl<TextBox>("RoomPresetMaxDelayTextBox");
            var narrativePresetBaseDelayTextBox = this.FindControl<TextBox>("NarrativePresetBaseDelayTextBox");
            var narrativePresetMinDelayTextBox = this.FindControl<TextBox>("NarrativePresetMinDelayTextBox");
            var narrativePresetMaxDelayTextBox = this.FindControl<TextBox>("NarrativePresetMaxDelayTextBox");
            var defaultPresetBaseDelayTextBox = this.FindControl<TextBox>("DefaultPresetBaseDelayTextBox");
            var defaultPresetMinDelayTextBox = this.FindControl<TextBox>("DefaultPresetMinDelayTextBox");
            var defaultPresetMaxDelayTextBox = this.FindControl<TextBox>("DefaultPresetMaxDelayTextBox");
            
            if (enableGuiDelaysCheckBox != null && enableConsoleDelaysCheckBox != null &&
                actionDelaySlider != null && messageDelaySlider != null &&
                combatDelayTextBox != null && systemDelayTextBox != null &&
                menuDelayTextBox != null && titleDelayTextBox != null &&
                mainTitleDelayTextBox != null && environmentalDelayTextBox != null &&
                effectMessageDelayTextBox != null && damageOverTimeDelayTextBox != null &&
                encounterDelayTextBox != null && rollInfoDelayTextBox != null &&
                baseMenuDelayTextBox != null && progressiveReductionRateTextBox != null &&
                progressiveThresholdTextBox != null && combatPresetBaseDelayTextBox != null &&
                combatPresetMinDelayTextBox != null && combatPresetMaxDelayTextBox != null &&
                dungeonPresetBaseDelayTextBox != null && dungeonPresetMinDelayTextBox != null &&
                dungeonPresetMaxDelayTextBox != null && roomPresetBaseDelayTextBox != null &&
                roomPresetMinDelayTextBox != null && roomPresetMaxDelayTextBox != null &&
                narrativePresetBaseDelayTextBox != null && narrativePresetMinDelayTextBox != null &&
                narrativePresetMaxDelayTextBox != null && defaultPresetBaseDelayTextBox != null &&
                defaultPresetMinDelayTextBox != null && defaultPresetMaxDelayTextBox != null)
            {
                settingsManager.SaveTextDelaySettings(
                    enableGuiDelaysCheckBox,
                    enableConsoleDelaysCheckBox,
                    actionDelaySlider,
                    messageDelaySlider,
                    combatDelayTextBox,
                    systemDelayTextBox,
                    menuDelayTextBox,
                    titleDelayTextBox,
                    mainTitleDelayTextBox,
                    environmentalDelayTextBox,
                    effectMessageDelayTextBox,
                    damageOverTimeDelayTextBox,
                    encounterDelayTextBox,
                    rollInfoDelayTextBox,
                    baseMenuDelayTextBox,
                    progressiveReductionRateTextBox,
                    progressiveThresholdTextBox,
                    combatPresetBaseDelayTextBox,
                    combatPresetMinDelayTextBox,
                    combatPresetMaxDelayTextBox,
                    dungeonPresetBaseDelayTextBox,
                    dungeonPresetMinDelayTextBox,
                    dungeonPresetMaxDelayTextBox,
                    roomPresetBaseDelayTextBox,
                    roomPresetMinDelayTextBox,
                    roomPresetMaxDelayTextBox,
                    narrativePresetBaseDelayTextBox,
                    narrativePresetMinDelayTextBox,
                    narrativePresetMaxDelayTextBox,
                    defaultPresetBaseDelayTextBox,
                    defaultPresetMinDelayTextBox,
                    defaultPresetMaxDelayTextBox);
            }
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
        
        /// <summary>
        /// Ensures all TextBoxes maintain dark background and white text when focused
        /// This is a backup to the XAML styles to handle cases where the template overrides styles
        /// </summary>
        private void FixTextBoxFocusStyling()
        {
            var darkBackground = new SolidColorBrush(Color.FromRgb(42, 42, 42)); // #FF2A2A2A
            var whiteForeground = Brushes.White;
            var blueBorder = new SolidColorBrush(Color.FromRgb(0, 120, 212)); // #FF0078D4
            var defaultBorder = new SolidColorBrush(Color.FromRgb(85, 85, 85)); // #FF555555
            
            // Use Dispatcher to ensure this runs after the visual tree is loaded
            Dispatcher.UIThread.Post(() =>
            {
                // Find all TextBoxes using Avalonia's visual tree traversal
                var textBoxes = new List<TextBox>();
                void FindTextBoxes(Control control)
                {
                    if (control is TextBox tb)
                    {
                        textBoxes.Add(tb);
                    }
                    
                    // Traverse visual children
                    var visualChildren = control.GetVisualChildren();
                    foreach (var child in visualChildren)
                    {
                        if (child is Control childControl)
                        {
                            FindTextBoxes(childControl);
                        }
                    }
                    
                    // Also check logical children for ContentControls
                    if (control is ContentControl cc && cc.Content is Control content)
                    {
                        FindTextBoxes(content);
                    }
                    
                    // Check Panel children
                    if (control is Panel panel)
                    {
                        foreach (var child in panel.Children)
                        {
                            if (child is Control childControl)
                            {
                                FindTextBoxes(childControl);
                            }
                        }
                    }
                }
                
                FindTextBoxes(this);
                
                // Apply focus handlers to all found TextBoxes
                foreach (var textBox in textBoxes)
                {
                    // Only fix TextBoxes that have dark backgrounds (settings TextBoxes)
                    var bg = textBox.Background as SolidColorBrush;
                    if (bg != null && (bg.Color.R == 42 && bg.Color.G == 42 && bg.Color.B == 42))
                    {
                        textBox.GotFocus += (s, e) =>
                        {
                            textBox.Background = darkBackground;
                            textBox.Foreground = whiteForeground;
                            textBox.BorderBrush = blueBorder;
                        };
                        
                        textBox.LostFocus += (s, e) =>
                        {
                            textBox.Background = darkBackground;
                            textBox.Foreground = whiteForeground;
                            textBox.BorderBrush = defaultBorder;
                        };
                    }
                }
            }, DispatcherPriority.Loaded);
        }
    }
}
