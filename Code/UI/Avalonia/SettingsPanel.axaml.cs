using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.Threading;
using Avalonia.Input;
using Avalonia.Layout;
using RPGGame;
using RPGGame.Editors;
using RPGGame.Game.Testing.Commands;
using RPGGame.UI.Avalonia.Builders;
using RPGGame.UI.Avalonia.Validators;
using RPGGame.UI.Avalonia.Managers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Timers;
using System.Timers;

namespace RPGGame.UI.Avalonia
{
    public partial class SettingsPanel : UserControl
    {
        private GameSettings settings;
        private System.Action? onBack;
        private System.Action<string>? updateStatus;
        private System.Func<Task>? onTesting;
        private System.Func<Task>? onDeveloperMenu;
        
        // Handler references for testing and developer tools
        private TestingSystemHandler? testingSystemHandler;
        private DeveloperMenuHandler? developerMenuHandler;
        private GameCoordinator? gameCoordinator;
        private CanvasUICoordinator? canvasUI;
        private GameStateManager? gameStateManager;
        
        // Game Variables tab fields
        private VariableEditor? variableEditor;
        private string? selectedGameVariableCategory;
        private Dictionary<string, TextBox> gameVariableTextBoxes = new Dictionary<string, TextBox>();
        private Dictionary<string, TextBlock> gameVariableChangeIndicators = new Dictionary<string, TextBlock>();
        private Dictionary<string, string> gameVariableCategoryDisplayToName = new Dictionary<string, string>();
        private ChangeIndicatorManager gameVariableChangeIndicatorManager = new ChangeIndicatorManager();
        
        public SettingsPanel()
        {
            InitializeComponent();
            settings = GameSettings.Instance;
            LoadSettings();
            WireUpEvents();
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
        /// Sets the callback for when testing menu button is clicked
        /// </summary>
        public void SetTestingCallback(System.Func<Task> callback)
        {
            onTesting = callback;
        }
        
        /// <summary>
        /// Sets the callback for when developer menu button is clicked
        /// </summary>
        public void SetDeveloperMenuCallback(System.Func<Task> callback)
        {
            onDeveloperMenu = callback;
        }
        
        /// <summary>
        /// Initializes handlers for testing and developer tools
        /// </summary>
        public void InitializeHandlers(TestingSystemHandler? testingHandler, DeveloperMenuHandler? developerHandler, GameCoordinator? game, CanvasUICoordinator? ui, GameStateManager? stateManager)
        {
            testingSystemHandler = testingHandler;
            developerMenuHandler = developerHandler;
            gameCoordinator = game;
            canvasUI = ui;
            gameStateManager = stateManager;
            
            // If canvasUI is null, try to get it from UIManager as fallback
            if (canvasUI == null)
            {
                var uiManager = RPGGame.UIManager.GetCustomUIManager();
                canvasUI = uiManager as CanvasUICoordinator;
            }
            
            // Initialize Game Variables tab
            InitializeGameVariablesTab();
        }
        
        /// <summary>
        /// Initializes the Game Variables tab with VariableEditor
        /// </summary>
        private void InitializeGameVariablesTab()
        {
            variableEditor = new VariableEditor();
            LoadGameVariableCategories();
            
            // Wire up category selection
            GameVariablesCategoryListBox.SelectionChanged += OnGameVariableCategorySelectionChanged;
        }
        
        /// <summary>
        /// Loads categories into the Game Variables category list
        /// </summary>
        private void LoadGameVariableCategories()
        {
            if (variableEditor == null) return;
            
            var categories = variableEditor.GetCategories();
            gameVariableCategoryDisplayToName.Clear();
            
            // Create category items with counts
            var categoryItems = categories.Select(cat =>
            {
                var count = variableEditor.GetVariablesByCategory(cat).Count;
                var displayText = $"{cat} ({count})";
                gameVariableCategoryDisplayToName[displayText] = cat;
                return displayText;
            }).ToList();
            
            GameVariablesCategoryListBox.ItemsSource = categoryItems;
        }
        
        /// <summary>
        /// Handles category selection in Game Variables tab
        /// </summary>
        private void OnGameVariableCategorySelectionChanged(object? sender, SelectionChangedEventArgs e)
        {
            if (GameVariablesCategoryListBox.SelectedItem is string displayText && 
                gameVariableCategoryDisplayToName.TryGetValue(displayText, out var categoryName))
            {
                selectedGameVariableCategory = categoryName;
                LoadGameVariablesForCategory(categoryName);
            }
        }
        
        /// <summary>
        /// Loads variables for the selected category
        /// </summary>
        private void LoadGameVariablesForCategory(string category)
        {
            if (variableEditor == null) return;
            
            GameVariablesPanel.Children.Clear();
            gameVariableTextBoxes.Clear();
            gameVariableChangeIndicators.Clear();
            gameVariableChangeIndicatorManager.Clear();
            
            var variables = variableEditor.GetVariablesByCategory(category);
            
            // Add category info header
            var categoryHeader = CreateGameVariableCategoryHeader(category, variables.Count);
            GameVariablesPanel.Children.Add(categoryHeader);
            
            foreach (var variable in variables)
            {
                // Store original value
                gameVariableChangeIndicatorManager.SetOriginalValue(variable.Name, variable.GetValue() ?? new object());
                
                var (container, textBox, indicator) = VariableControlBuilder.CreateVariableControl(
                    variable,
                    (v, tb, ind) => UpdateGameVariableChangeIndicator(v, tb, ind),
                    (v, tb) => ValidateAndUpdateGameVariable(v, tb),
                    (v, tb, e) =>
                    {
                        if (e.Key == Key.Enter)
                        {
                            ValidateAndUpdateGameVariable(v, tb);
                            e.Handled = true;
                        }
                    });
                
                gameVariableTextBoxes[variable.Name] = textBox;
                gameVariableChangeIndicators[variable.Name] = indicator;
                GameVariablesPanel.Children.Add(container);
            }
        }
        
        /// <summary>
        /// Creates a category header for Game Variables
        /// </summary>
        private Control CreateGameVariableCategoryHeader(string category, int variableCount)
        {
            var header = new Border
            {
                Background = new SolidColorBrush(Color.FromRgb(40, 40, 60)),
                BorderBrush = new SolidColorBrush(Color.FromRgb(100, 100, 150)),
                BorderThickness = new Thickness(2),
                CornerRadius = new CornerRadius(5),
                Padding = new Thickness(15),
                Margin = new Thickness(0, 0, 0, 15)
            };
            
            var stack = new StackPanel { Spacing = 5 };
            
            var title = new TextBlock
            {
                Text = $"📊 {category} Parameters",
                FontSize = 18,
                FontWeight = FontWeight.Bold,
                Foreground = new SolidColorBrush(Color.FromRgb(255, 215, 0))
            };
            stack.Children.Add(title);
            
            var info = new TextBlock
            {
                Text = $"{variableCount} parameters available • Changes are applied immediately",
                FontSize = 11,
                Foreground = new SolidColorBrush(Color.FromRgb(180, 180, 180))
            };
            stack.Children.Add(info);
            
            header.Child = stack;
            return header;
        }
        
        /// <summary>
        /// Updates the change indicator for a game variable
        /// </summary>
        private void UpdateGameVariableChangeIndicator(EditableVariable variable, TextBox textBox, TextBlock indicator)
        {
            gameVariableChangeIndicatorManager.UpdateChangeIndicator(
                variable,
                textBox,
                indicator,
                VariableValidator.ValidateValue);
        }
        
        /// <summary>
        /// Validates and updates a game variable
        /// </summary>
        private void ValidateAndUpdateGameVariable(EditableVariable variable, TextBox textBox)
        {
            if (variableEditor == null) return;
            
            var (success, errorMessage) = VariableValidator.ValidateAndUpdate(
                variable,
                textBox,
                ShowStatusMessage);
            
            if (success)
            {
                // Update change indicator
                if (gameVariableChangeIndicators.TryGetValue(variable.Name, out var indicator))
                {
                    UpdateGameVariableChangeIndicator(variable, textBox, indicator);
                }
                
                // Update original value after successful save
                var newValue = variable.GetValue();
                if (newValue != null)
                {
                    gameVariableChangeIndicatorManager.UpdateOriginalValue(variable.Name, newValue);
                }
            }
        }
        
        /// <summary>
        /// Loads current settings into the UI controls
        /// </summary>
        private void LoadSettings()
        {
            // Narrative Settings
            NarrativeBalanceSlider.Value = settings.NarrativeBalance;
            EnableNarrativeEventsCheckBox.IsChecked = settings.EnableNarrativeEvents;
            EnableInformationalSummariesCheckBox.IsChecked = settings.EnableInformationalSummaries;
            
            // Combat Settings
            CombatSpeedSlider.Value = settings.CombatSpeed;
            ShowIndividualActionMessagesCheckBox.IsChecked = settings.ShowIndividualActionMessages;
            EnableComboSystemCheckBox.IsChecked = settings.EnableComboSystem;
            EnableTextDisplayDelaysCheckBox.IsChecked = settings.EnableTextDisplayDelays;
            FastCombatCheckBox.IsChecked = settings.FastCombat;
            
            // Gameplay Settings
            EnableAutoSaveCheckBox.IsChecked = settings.EnableAutoSave;
            AutoSaveIntervalTextBox.Text = settings.AutoSaveInterval.ToString();
            ShowDetailedStatsCheckBox.IsChecked = settings.ShowDetailedStats;
            EnableSoundEffectsCheckBox.IsChecked = settings.EnableSoundEffects;
            
            // Difficulty Settings
            EnemyHealthMultiplierSlider.Value = settings.EnemyHealthMultiplier;
            EnemyDamageMultiplierSlider.Value = settings.EnemyDamageMultiplier;
            PlayerHealthMultiplierSlider.Value = settings.PlayerHealthMultiplier;
            PlayerDamageMultiplierSlider.Value = settings.PlayerDamageMultiplier;
            
            // UI Settings
            ShowHealthBarsCheckBox.IsChecked = settings.ShowHealthBars;
            ShowDamageNumbersCheckBox.IsChecked = settings.ShowDamageNumbers;
            ShowComboProgressCheckBox.IsChecked = settings.ShowComboProgress;
        }
        
        /// <summary>
        /// Wires up event handlers for controls
        /// </summary>
        private void WireUpEvents()
        {
            // Slider value changes - update TextBoxes
            NarrativeBalanceSlider.ValueChanged += (s, e) => 
            {
                NarrativeBalanceTextBox.Text = NarrativeBalanceSlider.Value.ToString("F2");
            };
            
            CombatSpeedSlider.ValueChanged += (s, e) => 
            {
                CombatSpeedTextBox.Text = CombatSpeedSlider.Value.ToString("F2");
            };
            
            EnemyHealthMultiplierSlider.ValueChanged += (s, e) => 
            {
                EnemyHealthMultiplierTextBox.Text = EnemyHealthMultiplierSlider.Value.ToString("F2");
            };
            
            EnemyDamageMultiplierSlider.ValueChanged += (s, e) => 
            {
                EnemyDamageMultiplierTextBox.Text = EnemyDamageMultiplierSlider.Value.ToString("F2");
            };
            
            PlayerHealthMultiplierSlider.ValueChanged += (s, e) => 
            {
                PlayerHealthMultiplierTextBox.Text = PlayerHealthMultiplierSlider.Value.ToString("F2");
            };
            
            PlayerDamageMultiplierSlider.ValueChanged += (s, e) => 
            {
                PlayerDamageMultiplierTextBox.Text = PlayerDamageMultiplierSlider.Value.ToString("F2");
            };
            
            // TextBox changes - update Sliders (for manual input)
            NarrativeBalanceTextBox.LostFocus += (s, e) => 
            {
                if (double.TryParse(NarrativeBalanceTextBox.Text, out double value))
                {
                    value = Math.Clamp(value, 0.0, 1.0);
                    NarrativeBalanceSlider.Value = value;
                    NarrativeBalanceTextBox.Text = value.ToString("F2");
                }
            };
            
            CombatSpeedTextBox.LostFocus += (s, e) => 
            {
                if (double.TryParse(CombatSpeedTextBox.Text, out double value))
                {
                    value = Math.Clamp(value, 0.5, 2.0);
                    CombatSpeedSlider.Value = value;
                    CombatSpeedTextBox.Text = value.ToString("F2");
                }
            };
            
            EnemyHealthMultiplierTextBox.LostFocus += (s, e) => 
            {
                if (double.TryParse(EnemyHealthMultiplierTextBox.Text, out double value))
                {
                    value = Math.Clamp(value, 0.5, 3.0);
                    EnemyHealthMultiplierSlider.Value = value;
                    EnemyHealthMultiplierTextBox.Text = value.ToString("F2");
                }
            };
            
            EnemyDamageMultiplierTextBox.LostFocus += (s, e) => 
            {
                if (double.TryParse(EnemyDamageMultiplierTextBox.Text, out double value))
                {
                    value = Math.Clamp(value, 0.5, 3.0);
                    EnemyDamageMultiplierSlider.Value = value;
                    EnemyDamageMultiplierTextBox.Text = value.ToString("F2");
                }
            };
            
            PlayerHealthMultiplierTextBox.LostFocus += (s, e) => 
            {
                if (double.TryParse(PlayerHealthMultiplierTextBox.Text, out double value))
                {
                    value = Math.Clamp(value, 0.5, 3.0);
                    PlayerHealthMultiplierSlider.Value = value;
                    PlayerHealthMultiplierTextBox.Text = value.ToString("F2");
                }
            };
            
            PlayerDamageMultiplierTextBox.LostFocus += (s, e) => 
            {
                if (double.TryParse(PlayerDamageMultiplierTextBox.Text, out double value))
                {
                    value = Math.Clamp(value, 0.5, 3.0);
                    PlayerDamageMultiplierSlider.Value = value;
                    PlayerDamageMultiplierTextBox.Text = value.ToString("F2");
                }
            };
            
            // Buttons
            SaveButton.Click += OnSaveButtonClick;
            ResetButton.Click += OnResetButtonClick;
            BackButton.Click += OnBackButtonClick;
            
            // Testing buttons
            RunAllTestsButton.Click += async (s, e) => await ExecuteTest("1");
            ColorSystemTestsButton.Click += async (s, e) => await ExecuteTest("11");
            CharacterSystemTestsButton.Click += async (s, e) => await ExecuteTest("2");
            CombatSystemTestsButton.Click += async (s, e) => await ExecuteTest("3");
            InventoryDungeonTestsButton.Click += async (s, e) => await ExecuteTest("4");
            DataUITestsButton.Click += async (s, e) => await ExecuteTest("5");
            ActionSystemTestsButton.Click += async (s, e) => await ExecuteTest("12");
            AdvancedIntegrationTestsButton.Click += async (s, e) => await ExecuteTest("6");
            GenerateRandomItemsButton.Click += async (s, e) => await ExecuteTest("7");
            ItemGenerationAnalysisButton.Click += async (s, e) => await ExecuteTest("8");
            TierDistributionTestButton.Click += async (s, e) => await ExecuteTest("9");
            CommonItemModificationTestButton.Click += async (s, e) => await ExecuteTest("10");
            ActionEditorTestsButton.Click += async (s, e) => await ExecuteTest("13");
            
            // Developer tools buttons
            EditGameVariablesButton.Click += OnEditGameVariablesClick;
            EditActionsButton.Click += OnEditActionsClick;
            BattleStatisticsButton.Click += OnBattleStatisticsClick;
            TuningParametersButton.Click += OnTuningParametersClick;
        }
        
        /// <summary>
        /// Saves current UI values to settings
        /// </summary>
        private void SaveSettings()
        {
            try
            {
                // Narrative Settings
                settings.NarrativeBalance = NarrativeBalanceSlider.Value;
                settings.EnableNarrativeEvents = EnableNarrativeEventsCheckBox.IsChecked ?? true;
                settings.EnableInformationalSummaries = EnableInformationalSummariesCheckBox.IsChecked ?? true;
                
                // Combat Settings
                settings.CombatSpeed = CombatSpeedSlider.Value;
                settings.ShowIndividualActionMessages = ShowIndividualActionMessagesCheckBox.IsChecked ?? false;
                settings.EnableComboSystem = EnableComboSystemCheckBox.IsChecked ?? true;
                settings.EnableTextDisplayDelays = EnableTextDisplayDelaysCheckBox.IsChecked ?? true;
                settings.FastCombat = FastCombatCheckBox.IsChecked ?? false;
                
                // Gameplay Settings
                settings.EnableAutoSave = EnableAutoSaveCheckBox.IsChecked ?? true;
                if (int.TryParse(AutoSaveIntervalTextBox.Text, out int autoSaveInterval))
                {
                    settings.AutoSaveInterval = Math.Max(1, autoSaveInterval);
                }
                settings.ShowDetailedStats = ShowDetailedStatsCheckBox.IsChecked ?? true;
                settings.EnableSoundEffects = EnableSoundEffectsCheckBox.IsChecked ?? false;
                
                // Difficulty Settings
                settings.EnemyHealthMultiplier = EnemyHealthMultiplierSlider.Value;
                settings.EnemyDamageMultiplier = EnemyDamageMultiplierSlider.Value;
                settings.PlayerHealthMultiplier = PlayerHealthMultiplierSlider.Value;
                settings.PlayerDamageMultiplier = PlayerDamageMultiplierSlider.Value;
                
                // UI Settings
                settings.ShowHealthBars = ShowHealthBarsCheckBox.IsChecked ?? true;
                settings.ShowDamageNumbers = ShowDamageNumbersCheckBox.IsChecked ?? true;
                settings.ShowComboProgress = ShowComboProgressCheckBox.IsChecked ?? true;
                
                // Save to file
                settings.SaveSettings();
                
                // Save Game Variables if any were modified
                SaveGameVariables();
                
                ShowStatusMessage("Settings saved successfully!", true);
                updateStatus?.Invoke("Settings saved successfully!");
            }
            catch (Exception ex)
            {
                ShowStatusMessage($"Error saving settings: {ex.Message}", false);
                updateStatus?.Invoke($"Error saving settings: {ex.Message}");
            }
        }
        
        /// <summary>
        /// Saves game variables to file
        /// </summary>
        private void SaveGameVariables()
        {
            if (variableEditor == null) return;
            
            try
            {
                // Validate all text boxes before saving
                foreach (var kvp in gameVariableTextBoxes)
                {
                    var textBox = kvp.Value;
                    if (textBox.IsFocused)
                    {
                        textBox.Focusable = false;
                        textBox.Focusable = true;
                    }
                }
                
                // Save changes
                bool saved = variableEditor.SaveChanges();
                if (saved)
                {
                    // Refresh variable values to update original values
                    RefreshGameVariableValues();
                }
            }
            catch (Exception ex)
            {
                ShowStatusMessage($"Error saving game variables: {ex.Message}", false);
                updateStatus?.Invoke($"Error saving game variables: {ex.Message}");
            }
        }
        
        /// <summary>
        /// Refreshes game variable values after save
        /// </summary>
        private void RefreshGameVariableValues()
        {
            if (variableEditor == null || string.IsNullOrEmpty(selectedGameVariableCategory)) return;
            
            var variables = variableEditor.GetVariablesByCategory(selectedGameVariableCategory);
            foreach (var variable in variables)
            {
                if (gameVariableTextBoxes.TryGetValue(variable.Name, out var textBox))
                {
                    var currentValue = variable.GetValue();
                    textBox.Text = currentValue?.ToString() ?? "";
                    textBox.Background = new SolidColorBrush(Color.FromRgb(40, 40, 40));
                    
                    // Update original value and refresh indicator
                    if (currentValue != null)
                    {
                        gameVariableChangeIndicatorManager.UpdateOriginalValue(variable.Name, currentValue);
                    }
                    if (gameVariableChangeIndicators.TryGetValue(variable.Name, out var indicator))
                    {
                        UpdateGameVariableChangeIndicator(variable, textBox, indicator);
                    }
                }
            }
        }
        
        /// <summary>
        /// Resets settings to defaults
        /// </summary>
        private void OnResetButtonClick(object? sender, RoutedEventArgs e)
        {
            settings.ResetToDefaults();
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
                ? new SolidColorBrush(Color.FromRgb(76, 175, 80)) // Green
                : new SolidColorBrush(Color.FromRgb(244, 67, 54)); // Red
            StatusMessage.IsVisible = true;
            
            // Hide after 3 seconds
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
            // Try to get canvasUI from stored reference, or fallback to UIManager
            CanvasUICoordinator? uiToUse = canvasUI;
            if (uiToUse == null)
            {
                // Fallback: try to get UI from UIManager
                var uiManager = RPGGame.UIManager.GetCustomUIManager();
                uiToUse = uiManager as CanvasUICoordinator;
            }
            
            if (uiToUse == null)
            {
                ShowStatusMessage("UI not available - game may not be fully initialized. Please wait for the game to load completely.", false);
                return Task.CompletedTask;
            }
            
            try
            {
                // Clear previous output in the TextBlock
                Dispatcher.UIThread.Post(() =>
                {
                    TestOutputTextBlock.Text = "Running test...\n";
                });
                
                // Clear the display buffer to capture only test output
                // This ensures we only capture the test's output, not previous game content
                uiToUse.ClearDisplayBuffer();
                
                // Restore canvas rendering so test results can be displayed on canvas too
                uiToUse.RestoreDisplayBufferRendering();
                
                // Create test coordinator and use game's state manager or create new one
                var testCoordinator = new TestExecutionCoordinator(uiToUse);
                var stateManager = gameStateManager ?? new GameStateManager();
                
                // Create and execute the appropriate test command
                ITestCommand? command = commandKey switch
                {
                    "1" => new RunAllTestsCommand(uiToUse, testCoordinator, stateManager),
                    "2" => new RunSystemTestsCommand(uiToUse, testCoordinator, stateManager, "Character"),
                    "3" => new RunCombatTestsCommand(uiToUse, testCoordinator, stateManager),
                    "4" => new RunInventoryDungeonTestsCommand(uiToUse, testCoordinator, stateManager),
                    "5" => new RunDataUITestsCommand(uiToUse, testCoordinator, stateManager),
                    "6" => new RunAdvancedTestsCommand(uiToUse, testCoordinator, stateManager),
                    "7" => new GenerateRandomItemsCommand(uiToUse, testCoordinator, stateManager),
                    "8" => new RunItemGenerationTestCommand(uiToUse, testCoordinator, stateManager),
                    "9" => new RunTierDistributionTestCommand(uiToUse, testCoordinator, stateManager),
                    "10" => new RunCommonItemModificationTestCommand(uiToUse, testCoordinator, stateManager),
                    "11" => new RunColorSystemTestsCommand(uiToUse, testCoordinator, stateManager),
                    "12" => new RunActionSystemTestsCommand(uiToUse, testCoordinator, stateManager),
                    "13" => new RunActionEditorTestCommand(uiToUse, testCoordinator, stateManager),
                    _ => null
                };
                
                if (command == null)
                {
                    Dispatcher.UIThread.Post(() =>
                    {
                        TestOutputTextBlock.Text = $"Unknown test command: {commandKey}";
                        ShowStatusMessage("Unknown test", false);
                    });
                    return Task.CompletedTask;
                }
                
                // Update UI immediately to show test started
                Dispatcher.UIThread.Post(() =>
                {
                    TestOutputTextBlock.Text = "Test execution started...\n";
                    ShowStatusMessage("Test execution started", true);
                });
                
                // Set up a timer to periodically update the Test Output window with display buffer content
                System.Timers.Timer? outputUpdateTimer = null;
                outputUpdateTimer = new System.Timers.Timer(100); // Update every 100ms
                outputUpdateTimer.Elapsed += (s, e) =>
                {
                    try
                    {
                        string currentOutput = uiToUse.GetDisplayBufferText();
                        if (!string.IsNullOrWhiteSpace(currentOutput))
                        {
                            Dispatcher.UIThread.Post(() =>
                            {
                                TestOutputTextBlock.Text = currentOutput;
                            });
                        }
                    }
                    catch
                    {
                        // Ignore errors during periodic updates
                    }
                };
                outputUpdateTimer.Start();
                
                // Execute the test asynchronously (don't block UI thread)
                _ = Task.Run(async () =>
                {
                    try
                    {
                        await command.ExecuteAsync();
                        
                        // Stop the periodic update timer
                        outputUpdateTimer?.Stop();
                        outputUpdateTimer?.Dispose();
                        
                        // Final capture of the test output from the display buffer
                        string testOutput = uiToUse.GetDisplayBufferText();
                        
                        // Update the Test Output window with the final captured output
                        Dispatcher.UIThread.Post(() =>
                        {
                            if (!string.IsNullOrWhiteSpace(testOutput))
                            {
                                TestOutputTextBlock.Text = testOutput;
                            }
                            else
                            {
                                TestOutputTextBlock.Text = "Test completed, but no output was captured.\n";
                            }
                            ShowStatusMessage("Test completed", true);
                        });
                    }
                    catch (Exception ex)
                    {
                        // Stop the periodic update timer on error
                        outputUpdateTimer?.Stop();
                        outputUpdateTimer?.Dispose();
                        
                        Dispatcher.UIThread.Post(() =>
                        {
                            TestOutputTextBlock.Text = $"Error executing test: {ex.Message}\n{ex.StackTrace}";
                            ShowStatusMessage($"Error: {ex.Message}", false);
                        });
                    }
                });
                
                return Task.CompletedTask;
            }
            catch (Exception ex)
            {
                Dispatcher.UIThread.Post(() =>
                {
                    TestOutputTextBlock.Text = $"Error: {ex.Message}\n{ex.StackTrace}";
                    ShowStatusMessage($"Error: {ex.Message}", false);
                });
                return Task.CompletedTask;
            }
        }
        
        /// <summary>
        /// Gets the canvas UI, trying stored reference first, then UIManager fallback
        /// </summary>
        private CanvasUICoordinator? GetCanvasUI()
        {
            if (canvasUI != null)
                return canvasUI;
            
            // Fallback: try to get UI from UIManager
            var uiManager = RPGGame.UIManager.GetCustomUIManager();
            return uiManager as CanvasUICoordinator;
        }
        
        /// <summary>
        /// Opens the Game Variables tab in the settings panel
        /// </summary>
        private void OnEditGameVariablesClick(object? sender, RoutedEventArgs e)
        {
            // Switch to Game Variables tab
            if (SettingsTabControl != null && GameVariablesTab != null)
            {
                SettingsTabControl.SelectedItem = GameVariablesTab;
                
                // If no category is selected, select the first one
                if (GameVariablesCategoryListBox.SelectedItem == null && 
                    GameVariablesCategoryListBox.ItemsSource != null &&
                    GameVariablesCategoryListBox.ItemsSource.Cast<object>().Any())
                {
                    GameVariablesCategoryListBox.SelectedIndex = 0;
                }
            }
        }
        
        /// <summary>
        /// Opens the action editor
        /// </summary>
        private void OnEditActionsClick(object? sender, RoutedEventArgs e)
        {
            var uiToUse = GetCanvasUI();
            if (gameCoordinator == null || uiToUse == null)
            {
                ShowStatusMessage("Developer tools not available - game may not be fully initialized", false);
                return;
            }
            
            // Hide settings panel and restore canvas rendering
            onBack?.Invoke();
            
            // Small delay to ensure panel is hidden before showing editor
            Dispatcher.UIThread.Post(() =>
            {
                uiToUse.RestoreDisplayBufferRendering();
                // Use GameCoordinator method to show action editor
                gameCoordinator.ShowActionEditor();
            }, DispatcherPriority.Background);
        }
        
        /// <summary>
        /// Runs battle statistics
        /// </summary>
        private void OnBattleStatisticsClick(object? sender, RoutedEventArgs e)
        {
            var uiToUse = GetCanvasUI();
            if (gameCoordinator == null || uiToUse == null)
            {
                ShowStatusMessage("Developer tools not available - game may not be fully initialized", false);
                return;
            }
            
            // Hide settings panel and restore canvas rendering
            onBack?.Invoke();
            
            // Small delay to ensure panel is hidden before showing statistics
            Dispatcher.UIThread.Post(() =>
            {
                uiToUse.RestoreDisplayBufferRendering();
                // Use GameCoordinator method to show battle statistics
                gameCoordinator.ShowBattleStatistics();
            }, DispatcherPriority.Background);
        }
        
        /// <summary>
        /// Opens tuning parameters
        /// </summary>
        private void OnTuningParametersClick(object? sender, RoutedEventArgs e)
        {
            if (gameCoordinator == null)
            {
                ShowStatusMessage("Developer tools not available - game may not be fully initialized", false);
                return;
            }
            
            // Hide settings panel - tuning parameters will show its own panel
            onBack?.Invoke();
            
            // Small delay to ensure panel is hidden before showing tuning parameters
            Dispatcher.UIThread.Post(() =>
            {
                // Use GameCoordinator method to show tuning parameters
                // This will show the TuningMenuPanel overlay
                gameCoordinator.ShowTuningParameters();
            }, DispatcherPriority.Background);
        }
    }
}

