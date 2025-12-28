using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.Threading;
using Avalonia.VisualTree;
using RPGGame;
using RPGGame.Data;
using RPGGame.UI.Avalonia.Managers;
using RPGGame.UI.Avalonia.Helpers;
using RPGGame.Utils;
using System;
using System.Collections.Generic;
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
        private Managers.TestRunnerUI? testRunnerUI;
        
        // Extracted managers
        private SettingsPersistenceManager? persistenceManager;
        private SettingsEventWiring? eventWiring;
        private SettingsInitialization? initialization;
        private bool eventsWired = false;
        
        public SettingsPanel()
        {
            InitializeComponent();
            settings = GameSettings.Instance;
            InitializeManagers();
            
            // Load settings immediately (doesn't require event wiring)
            // Delay event wiring until after controls are fully loaded and handlers are initialized
            this.AttachedToVisualTree += (s, e) =>
            {
                LoadSettings();
                RestoreLastTab();
                SetupKeyboardNavigation();
                // Events will be wired in InitializeHandlers after testExecutor is ready
            };
            
            // Fallback: if AttachedToVisualTree doesn't fire, load settings after a short delay
            Dispatcher.UIThread.Post(() =>
            {
                LoadSettings();
                RestoreLastTab();
                SetupKeyboardNavigation();
            }, DispatcherPriority.Loaded);
        }
        
        /// <summary>
        /// Restores the last viewed tab from user preferences
        /// </summary>
        private void RestoreLastTab()
        {
            try
            {
                if (SettingsTabControl != null)
                {
                    // Try to load last tab index from a simple storage mechanism
                    // For now, default to first tab (Gameplay)
                    int lastTabIndex = 0;
                    
                    // In the future, this could load from a user preferences file
                    // For now, we'll just ensure the tab control is properly initialized
                    if (SettingsTabControl.Items != null && SettingsTabControl.Items.Count > 0)
                    {
                        SettingsTabControl.SelectedIndex = Math.Clamp(lastTabIndex, 0, SettingsTabControl.Items.Count - 1);
                    }
                }
            }
            catch (Exception ex)
            {
                ScrollDebugLogger.Log($"SettingsPanel: Error restoring last tab: {ex.Message}");
            }
        }
        
        /// <summary>
        /// Saves the current tab index for persistence
        /// </summary>
        private void SaveCurrentTab()
        {
            try
            {
                if (SettingsTabControl != null && SettingsTabControl.SelectedIndex >= 0)
                {
                    // In the future, this could save to a user preferences file
                    // For now, we'll just log it for debugging
                    ScrollDebugLogger.Log($"SettingsPanel: Current tab index: {SettingsTabControl.SelectedIndex}");
                }
            }
            catch (Exception ex)
            {
                ScrollDebugLogger.Log($"SettingsPanel: Error saving current tab: {ex.Message}");
            }
        }
        
        /// <summary>
        /// Sets up keyboard navigation for the settings panel
        /// </summary>
        private void SetupKeyboardNavigation()
        {
            try
            {
                // Handle Tab key navigation
                this.KeyDown += (s, e) =>
                {
                    // Tab key navigation is handled automatically by Avalonia
                    // Arrow keys for tab navigation (when TabControl has focus)
                    if (SettingsTabControl != null && SettingsTabControl.IsFocused)
                    {
                        if (e.Key == Key.Left)
                        {
                            if (SettingsTabControl.SelectedIndex > 0)
                            {
                                SettingsTabControl.SelectedIndex--;
                                e.Handled = true;
                            }
                        }
                        else if (e.Key == Key.Right)
                        {
                            if (SettingsTabControl.SelectedIndex < SettingsTabControl.Items.Count - 1)
                            {
                                SettingsTabControl.SelectedIndex++;
                                e.Handled = true;
                            }
                        }
                    }
                };
                
                // Save tab when it changes
                if (SettingsTabControl != null)
                {
                    SettingsTabControl.SelectionChanged += (s, e) =>
                    {
                        SaveCurrentTab();
                    };
                }
            }
            catch (Exception ex)
            {
                ScrollDebugLogger.Log($"SettingsPanel: Error setting up keyboard navigation: {ex.Message}");
            }
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
        /// Initializes handlers for developer tools
        /// </summary>
        public void InitializeHandlers(DeveloperMenuHandler? developerHandler, GameCoordinator? game, CanvasUICoordinator? ui, GameStateManager? stateManager)
        {
            gameCoordinator = game;
            canvasUI = ui;
            gameStateManager = stateManager;
            
            if (canvasUI == null)
            {
                var uiManager = RPGGame.UIManager.GetCustomUIManager();
                canvasUI = uiManager as CanvasUICoordinator;
            }
            
            // Initialize tab managers
            if (initialization != null)
            {
                initialization.InitializeHandlers(
                    GameVariablesCategoryListBox,
                    GameVariablesPanel,
                    ActionsListBox,
                    ActionFormPanel,
                    CreateActionButton,
                    DeleteActionButton,
                    this);
            }
            
            // Initialize test runner UI
            if (canvasUI != null)
            {
                testRunnerUI = new Managers.TestRunnerUI(canvasUI);
                // Wire test buttons after a short delay to ensure controls are loaded
                Dispatcher.UIThread.Post(() =>
                {
                    WireUpTestButtons();
                }, DispatcherPriority.Loaded);
            }
            
            // Wire events if not already wired
            if (!eventsWired)
            {
                WireUpEvents();
            }
        }
        
        /// <summary>
        /// Loads current settings into the UI controls
        /// </summary>
        private void LoadSettings()
        {
            if (persistenceManager == null)
            {
                ScrollDebugLogger.Log("SettingsPanel: PersistenceManager is null, cannot load settings");
                return;
            }

            try
            {
            
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
            catch (Exception ex)
            {
                ScrollDebugLogger.Log($"SettingsPanel: Error loading settings: {ex.Message}\n{ex.StackTrace}");
                ShowStatusMessage($"Error loading settings: {ex.Message}", false);
            }
        }
        
        /// <summary>
        /// Loads animation settings into UI controls
        /// </summary>
        private void LoadAnimationSettings()
        {
            if (persistenceManager == null) return;
            
            try
            {
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
            catch (Exception ex)
            {
                ScrollDebugLogger.Log($"SettingsPanel: Error loading animation settings: {ex.Message}");
            }
        }
        
        /// <summary>
        /// Loads text delay settings into UI controls
        /// </summary>
        private void LoadTextDelaySettings()
        {
            if (persistenceManager == null) return;
            
            try
            {
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
            catch (Exception ex)
            {
                ScrollDebugLogger.Log($"SettingsPanel: Error loading text delay settings: {ex.Message}");
            }
        }
        
        /// <summary>
        /// Wires up event handlers for controls
        /// </summary>
        private void WireUpEvents()
        {
            try
            {
                // Verify critical controls exist before wiring
                if (NarrativeBalanceSlider == null || NarrativeBalanceTextBox == null ||
                    CombatSpeedSlider == null || CombatSpeedTextBox == null ||
                    SaveButton == null || ResetButton == null || BackButton == null)
                {
                    ScrollDebugLogger.Log("SettingsPanel: Critical controls not initialized. Event wiring skipped.");
                    return;
                }

                // Only wire events once to prevent duplicate handlers
                // If events are already wired, skip (unless we're re-wiring after testExecutor is initialized)
                if (eventsWired && eventWiring != null)
                {
                    ScrollDebugLogger.Log("SettingsPanel: Events already wired, skipping duplicate wiring.");
                    return;
                }

                // Create eventWiring
                eventWiring = new SettingsEventWiring(
                    async (count) => 
                    {
                        // Battle test functionality can be added here if needed
                        ScrollDebugLogger.Log($"SettingsPanel: Battle test functionality not yet implemented");
                        await Task.CompletedTask;
                    },
                    async () => 
                    {
                        // Weapon type test functionality can be added here if needed
                        ScrollDebugLogger.Log($"SettingsPanel: Weapon type test functionality not yet implemented");
                        await Task.CompletedTask;
                    },
                    async () => 
                    {
                        // Comprehensive test functionality can be added here if needed
                        ScrollDebugLogger.Log($"SettingsPanel: Comprehensive test functionality not yet implemented");
                        await Task.CompletedTask;
                    },
                    (System.Action)(() => OnSaveButtonClick(null, null!)),
                    (System.Action)(() => OnResetButtonClick(null, null!)),
                    (System.Action)(() => OnBackButtonClick(null, null!)),
                    (System.Action)(() => 
                    {
                        // Battle statistics functionality can be added here if needed
                        ScrollDebugLogger.Log($"SettingsPanel: Battle statistics functionality not yet implemented");
                    }));
                
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
                    BackButton);
                
                eventsWired = true;
            }
            catch (Exception ex)
            {
                ScrollDebugLogger.Log($"SettingsPanel: Error wiring up events: {ex.Message}\n{ex.StackTrace}");
                ShowStatusMessage($"Error initializing controls: {ex.Message}", false);
            }
        }
        
        /// <summary>
        /// Saves current UI values to settings
        /// </summary>
        private void SaveSettings()
        {
            if (persistenceManager == null)
            {
                ScrollDebugLogger.Log("SettingsPanel: PersistenceManager is null, cannot save settings");
                ShowStatusMessage("Error: Settings manager not initialized", false);
                return;
            }

            try
            {
            
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
            catch (Exception ex)
            {
                ScrollDebugLogger.Log($"SettingsPanel: Error saving settings: {ex.Message}\n{ex.StackTrace}");
                ShowStatusMessage($"Error saving settings: {ex.Message}", false);
            }
        }
        
        /// <summary>
        /// Saves animation settings from UI controls
        /// </summary>
        private void SaveAnimationSettings()
        {
            if (persistenceManager == null) return;
            
            try
            {
                persistenceManager.SaveAnimationSettings(
                BrightnessMaskEnabledCheckBox,
                BrightnessMaskIntensitySlider,
                BrightnessMaskWaveLengthSlider,
                BrightnessMaskUpdateIntervalTextBox,
                    UndulationSpeedSlider,
                    UndulationWaveLengthSlider,
                    UndulationIntervalTextBox);
            }
            catch (Exception ex)
            {
                ScrollDebugLogger.Log($"SettingsPanel: Error saving animation settings: {ex.Message}");
            }
        }
        
        /// <summary>
        /// Saves text delay settings from UI controls
        /// </summary>
        private void SaveTextDelaySettings()
        {
            if (persistenceManager == null) return;
            
            try
            {
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
            catch (Exception ex)
            {
                ScrollDebugLogger.Log($"SettingsPanel: Error saving text delay settings: {ex.Message}");
            }
        }
        
        private bool resetConfirmationPending = false;
        
        /// <summary>
        /// Resets settings to defaults with two-click confirmation
        /// </summary>
        private void OnResetButtonClick(object? sender, RoutedEventArgs e)
        {
            if (persistenceManager == null) return;
            
            if (!resetConfirmationPending)
            {
                // First click: show confirmation message
                resetConfirmationPending = true;
                ShowStatusMessage("Click Reset again to confirm. This will reset all settings to defaults.", false);
                updateStatus?.Invoke("Click Reset again to confirm resetting all settings");
                
                // Reset confirmation after 5 seconds
                var timer = new System.Timers.Timer(5000);
                timer.Elapsed += (s, args) =>
                {
                    timer.Stop();
                    Dispatcher.UIThread.Post(() =>
                    {
                        resetConfirmationPending = false;
                    });
                };
                timer.Start();
            }
            else
            {
                // Second click: perform reset
                resetConfirmationPending = false;
                try
                {
                    persistenceManager.ResetToDefaults();
                    LoadSettings();
                    ShowStatusMessage("Settings reset to defaults", true);
                    updateStatus?.Invoke("Settings reset to defaults");
                }
                catch (Exception ex)
                {
                    ScrollDebugLogger.Log($"SettingsPanel: Error resetting settings: {ex.Message}");
                    ShowStatusMessage($"Error resetting settings: {ex.Message}", false);
                }
            }
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
        /// Wires up test runner button events
        /// </summary>
        private void WireUpTestButtons()
        {
            try
            {
                if (testRunnerUI == null) return;
                
                // Find test buttons by name
                var runAllTestsButton = this.FindControl<Button>("RunAllTestsButton");
                var runQuickTestsButton = this.FindControl<Button>("RunQuickTestsButton");
                var runActionSystemTestsButton = this.FindControl<Button>("RunActionSystemTestsButton");
                var runDiceMechanicsTestsButton = this.FindControl<Button>("RunDiceMechanicsTestsButton");
                var runComboSystemTestsButton = this.FindControl<Button>("RunComboSystemTestsButton");
                var runColorSystemTestsButton = this.FindControl<Button>("RunColorSystemTestsButton");
                var runDisplaySystemTestsButton = this.FindControl<Button>("RunDisplaySystemTestsButton");
                var runCharacterSystemTestsButton = this.FindControl<Button>("RunCharacterSystemTestsButton");
                var runStatusEffectsTestsButton = this.FindControl<Button>("RunStatusEffectsTestsButton");
                var runIntegrationTestsButton = this.FindControl<Button>("RunIntegrationTestsButton");
                
                if (runAllTestsButton != null)
                {
                    runAllTestsButton.Click += (s, e) =>
                    {
                        Task.Run(() => testRunnerUI.RunAllTests());
                        ShowStatusMessage("Running all tests...", true);
                    };
                }
                
                if (runQuickTestsButton != null)
                {
                    runQuickTestsButton.Click += (s, e) =>
                    {
                        Task.Run(() => testRunnerUI.RunQuickTests());
                        ShowStatusMessage("Running quick tests...", true);
                    };
                }
                
                if (runActionSystemTestsButton != null)
                {
                    runActionSystemTestsButton.Click += (s, e) =>
                    {
                        Task.Run(() => testRunnerUI.RunActionSystemTests());
                        ShowStatusMessage("Running action system tests...", true);
                    };
                }
                
                if (runDiceMechanicsTestsButton != null)
                {
                    runDiceMechanicsTestsButton.Click += (s, e) =>
                    {
                        Task.Run(() => testRunnerUI.RunDiceMechanicsTests());
                        ShowStatusMessage("Running dice mechanics tests...", true);
                    };
                }
                
                if (runComboSystemTestsButton != null)
                {
                    runComboSystemTestsButton.Click += (s, e) =>
                    {
                        Task.Run(() => testRunnerUI.RunComboSystemTests());
                        ShowStatusMessage("Running combo system tests...", true);
                    };
                }
                
                if (runColorSystemTestsButton != null)
                {
                    runColorSystemTestsButton.Click += (s, e) =>
                    {
                        Task.Run(() => testRunnerUI?.RunColorSystemTests());
                        ShowStatusMessage("Running color system tests...", true);
                    };
                }
                
                if (runDisplaySystemTestsButton != null)
                {
                    runDisplaySystemTestsButton.Click += (s, e) =>
                    {
                        Task.Run(() => testRunnerUI.RunDisplaySystemTests());
                        ShowStatusMessage("Running display system tests...", true);
                    };
                }
                
                if (runCharacterSystemTestsButton != null)
                {
                    runCharacterSystemTestsButton.Click += (s, e) =>
                    {
                        Task.Run(() => testRunnerUI.RunCharacterSystemTests());
                        ShowStatusMessage("Running character system tests...", true);
                    };
                }
                
                if (runStatusEffectsTestsButton != null)
                {
                    runStatusEffectsTestsButton.Click += (s, e) =>
                    {
                        Task.Run(() => testRunnerUI.RunStatusEffectsTests());
                        ShowStatusMessage("Running status effects tests...", true);
                    };
                }
                
                if (runIntegrationTestsButton != null)
                {
                    runIntegrationTestsButton.Click += (s, e) =>
                    {
                        Task.Run(() => testRunnerUI.RunIntegrationTests());
                        ShowStatusMessage("Running integration tests...", true);
                    };
                }
                
                var generateRandomActionBlockButton = this.FindControl<Button>("GenerateRandomActionBlockButton");
                if (generateRandomActionBlockButton != null)
                {
                    generateRandomActionBlockButton.Click += (s, e) =>
                    {
                        GenerateAndDisplayRandomActionBlock();
                        ShowStatusMessage("Generated random action block", false);
                    };
                }
            }
            catch (Exception ex)
            {
                ScrollDebugLogger.Log($"SettingsPanel: Error wiring up test buttons: {ex.Message}\n{ex.StackTrace}");
            }
        }
        
        /// <summary>
        /// Generates a random action block and displays it in the center panel
        /// </summary>
        private void GenerateAndDisplayRandomActionBlock()
        {
            try
            {
                // Get a random action from all available actions
                var allActions = ActionLoader.GetAllActions();
                if (allActions == null || allActions.Count == 0)
                {
                    ShowStatusMessage("No actions available", false);
                    return;
                }
                
                var random = new Random();
                var randomAction = allActions[random.Next(allActions.Count)];
                
                // Create mock characters for the action
                var attacker = Tests.TestDataBuilders.Character()
                    .WithName("Test Hero")
                    .Build();
                var target = Tests.TestDataBuilders.Enemy()
                    .WithName("Test Enemy")
                    .Build();
                
                // Generate random roll values
                int roll = random.Next(1, 21); // 1-20
                int rollBonus = random.Next(-3, 4); // -3 to +3
                int rawDamage = random.Next(10, 51); // 10-50
                int targetDefense = random.Next(0, 21); // 0-20
                double actualSpeed = random.NextDouble() * 2.0 + 0.5; // 0.5-2.5
                double? comboAmplifier = randomAction.IsComboAction ? (double?)random.NextDouble() * 0.5 + 1.0 : null; // 1.0-1.5 if combo
                
                // Generate action text
                var actionText = GenerateActionText(attacker, target, randomAction, rawDamage);
                
                // Generate roll info
                var rollInfo = Combat.Formatting.RollInfoFormatter.FormatRollInfoColored(
                    roll, rollBonus, rawDamage, targetDefense, actualSpeed, comboAmplifier, randomAction);
                
                // Display the action block
                BlockDisplayManager.DisplayActionBlock(actionText, rollInfo);
            }
            catch (Exception ex)
            {
                ScrollDebugLogger.Log($"SettingsPanel: Error generating random action block: {ex.Message}\n{ex.StackTrace}");
                ShowStatusMessage($"Error: {ex.Message}", false);
            }
        }
        
        /// <summary>
        /// Generates action text for a combat action
        /// </summary>
        private List<UI.ColorSystem.ColoredText> GenerateActionText(Actor attacker, Actor target, Action action, int damage)
        {
            var builder = new UI.ColorSystem.ColoredTextBuilder();
            
            // Attacker name with appropriate color
            builder.Add(attacker.Name, UI.ColorSystem.EntityColorHelper.GetActorColor(attacker));
            
            // Action verb and name
            string actionName = action.Name;
            bool isComboAction = action.IsComboAction;
            
            // Determine if it's a critical hit (random for demo)
            var random = new Random();
            bool isCritical = random.Next(100) < 10; // 10% chance
            
            if (isCritical)
            {
                actionName = $"CRITICAL {actionName}";
            }
            
            // Use appropriate color for action
            var actionColor = isComboAction ? UI.ColorSystem.ColorPalette.Green : UI.ColorSystem.ColorPalette.White;
            if (isCritical)
            {
                actionColor = UI.ColorSystem.ColorPalette.Warning;
            }
            
            builder.AddSpace();
            builder.Add("hits", UI.ColorSystem.ColorPalette.Success);
            builder.AddSpace();
            builder.Add(target.Name, UI.ColorSystem.EntityColorHelper.GetActorColor(target));
            builder.AddSpace();
            builder.Add("with", UI.ColorSystem.ColorPalette.White);
            builder.AddSpace();
            builder.Add(actionName, actionColor);
            builder.AddSpace();
            builder.Add("for", UI.ColorSystem.ColorPalette.White);
            builder.AddSpace();
            builder.Add(damage.ToString(), UI.ColorSystem.ColorPalette.Error);
            builder.AddSpace();
            builder.Add("damage", UI.ColorSystem.ColorPalette.White);
            
            return builder.Build();
        }
        
    }
}
