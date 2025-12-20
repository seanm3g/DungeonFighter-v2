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
using RPGGame.Data;
using RPGGame.UI.Avalonia.Builders;
using RPGGame.UI.Avalonia.Validators;
using RPGGame.UI.Avalonia.Managers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
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
        
        // Actions tab fields
        private ActionEditor? actionEditor;
        private ActionData? selectedAction;
        private bool isCreatingNewAction = false;
        private Dictionary<string, Control> actionFormControls = new Dictionary<string, Control>();
        
        // Battle Statistics tab fields
        private BattleStatisticsRunner.StatisticsResult? currentBattleStatisticsResults;
        private List<BattleStatisticsRunner.WeaponTestResult>? currentWeaponTestResults;
        private BattleStatisticsRunner.ComprehensiveWeaponEnemyTestResult? currentComprehensiveResults;
        private bool isBattleStatisticsRunning = false;
        
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
            
            // Initialize Actions tab
            InitializeActionsTab();
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
            
            // Create a 2-column grid for the variables
            var variablesGrid = new Grid();
            variablesGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            variablesGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(10) }); // Spacer
            variablesGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            
            // Calculate number of rows needed
            int numRows = (variables.Count + 1) / 2; // Round up
            for (int r = 0; r < numRows; r++)
            {
                variablesGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            }
            
            for (int i = 0; i < variables.Count; i++)
            {
                var variable = variables[i];
                
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
                
                // Determine row and column
                int row = i / 2;
                int column = (i % 2 == 0) ? 0 : 2;
                
                Grid.SetColumn(container, column);
                Grid.SetRow(container, row);
                variablesGrid.Children.Add(container);
            }
            
            GameVariablesPanel.Children.Add(variablesGrid);
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
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(2),
                Padding = new Thickness(4, 3),
                Margin = new Thickness(0, 0, 0, 2)
            };
            
            var stack = new StackPanel { Spacing = 2 };
            
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
                FontSize = 13,
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
            NarrativeBalanceTextBox.Text = settings.NarrativeBalance.ToString("F2");
            EnableNarrativeEventsCheckBox.IsChecked = settings.EnableNarrativeEvents;
            EnableInformationalSummariesCheckBox.IsChecked = settings.EnableInformationalSummaries;
            
            // Combat Settings
            CombatSpeedSlider.Value = settings.CombatSpeed;
            CombatSpeedTextBox.Text = settings.CombatSpeed.ToString("F2");
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
            EnemyHealthMultiplierTextBox.Text = settings.EnemyHealthMultiplier.ToString("F2");
            EnemyDamageMultiplierSlider.Value = settings.EnemyDamageMultiplier;
            EnemyDamageMultiplierTextBox.Text = settings.EnemyDamageMultiplier.ToString("F2");
            PlayerHealthMultiplierSlider.Value = settings.PlayerHealthMultiplier;
            PlayerHealthMultiplierTextBox.Text = settings.PlayerHealthMultiplier.ToString("F2");
            PlayerDamageMultiplierSlider.Value = settings.PlayerDamageMultiplier;
            PlayerDamageMultiplierTextBox.Text = settings.PlayerDamageMultiplier.ToString("F2");
            
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
            
            // Battle Statistics tab buttons - use FindControl to avoid XAML generation issues
            var quickTestButton = this.FindControl<Button>("QuickTestButton");
            var standardTestButton = this.FindControl<Button>("StandardTestButton");
            var comprehensiveTestButton = this.FindControl<Button>("ComprehensiveTestButton");
            var weaponTypeTestButton = this.FindControl<Button>("WeaponTypeTestButton");
            var comprehensiveWeaponEnemyTestButton = this.FindControl<Button>("ComprehensiveWeaponEnemyTestButton");
            var battleStatisticsButton = this.FindControl<Button>("BattleStatisticsButton");
            
            if (quickTestButton != null) quickTestButton.Click += async (s, e) => await RunBattleTest(100);
            if (standardTestButton != null) standardTestButton.Click += async (s, e) => await RunBattleTest(500);
            if (comprehensiveTestButton != null) comprehensiveTestButton.Click += async (s, e) => await RunBattleTest(1000);
            if (weaponTypeTestButton != null) weaponTypeTestButton.Click += async (s, e) => await RunWeaponTypeTests();
            if (comprehensiveWeaponEnemyTestButton != null) comprehensiveWeaponEnemyTestButton.Click += async (s, e) => await RunComprehensiveWeaponEnemyTests();
            if (battleStatisticsButton != null) battleStatisticsButton.Click += OnBattleStatisticsClick;
            
            // Actions tab buttons
            CreateActionButton.Click += OnCreateActionClick;
            DeleteActionButton.Click += OnDeleteActionClick;
            ActionsListBox.SelectionChanged += OnActionSelectionChanged;
        }
        
        /// <summary>
        /// Initializes the Actions tab with ActionEditor
        /// </summary>
        private void InitializeActionsTab()
        {
            actionEditor = new ActionEditor();
            LoadActionsList();
        }
        
        /// <summary>
        /// Loads actions into the list
        /// </summary>
        private void LoadActionsList()
        {
            if (actionEditor == null) return;
            
            var actions = actionEditor.GetActions();
            // Store actions in a dictionary for lookup by name
            actionNameToAction = actions.ToDictionary(a => a.Name, a => a);
            // Use action names for display (strings work with compiled bindings)
            ActionsListBox.ItemsSource = actions.Select(a => a.Name).ToList();
        }
        
        // Dictionary to map action names back to ActionData objects
        private Dictionary<string, ActionData> actionNameToAction = new Dictionary<string, ActionData>();
        
        /// <summary>
        /// Handles action selection in the list
        /// </summary>
        private void OnActionSelectionChanged(object? sender, SelectionChangedEventArgs e)
        {
            if (ActionsListBox.SelectedItem is string actionName && 
                actionNameToAction.TryGetValue(actionName, out var action))
            {
                selectedAction = action;
                isCreatingNewAction = false;
                LoadActionForm(action);
            }
        }
        
        /// <summary>
        /// Creates a new action
        /// </summary>
        private void OnCreateActionClick(object? sender, RoutedEventArgs e)
        {
            selectedAction = new ActionData
            {
                Name = "",
                Type = "Attack",
                TargetType = "SingleTarget",
                BaseValue = 0,
                Range = 1,
                Cooldown = 0,
                Description = "",
                DamageMultiplier = 1.0,
                Length = 1.0,
                Tags = new List<string>()
            };
            isCreatingNewAction = true;
            ActionsListBox.SelectedItem = null;
            LoadActionForm(selectedAction);
        }
        
        /// <summary>
        /// Deletes the selected action
        /// </summary>
        private void OnDeleteActionClick(object? sender, RoutedEventArgs e)
        {
            if (actionEditor == null || selectedAction == null || isCreatingNewAction)
            {
                ShowStatusMessage("No action selected for deletion", false);
                return;
            }
            
            if (actionEditor.DeleteAction(selectedAction.Name))
            {
                ShowStatusMessage($"Action '{selectedAction.Name}' deleted successfully", true);
                LoadActionsList();
                ActionFormPanel.Children.Clear();
                selectedAction = null;
            }
            else
            {
                ShowStatusMessage($"Failed to delete action '{selectedAction.Name}'", false);
            }
        }
        
        /// <summary>
        /// Loads the action form with all fields
        /// </summary>
        private void LoadActionForm(ActionData action)
        {
            ActionFormPanel.Children.Clear();
            actionFormControls.Clear();
            
            // Title
            var title = new TextBlock
            {
                Text = isCreatingNewAction ? "Create New Action" : $"Edit Action: {action.Name}",
                FontSize = 18,
                FontWeight = FontWeight.Bold,
                Foreground = new SolidColorBrush(Color.FromRgb(255, 215, 0)),
                Margin = new Thickness(0, 0, 0, 15)
            };
            ActionFormPanel.Children.Add(title);
            
            // Basic Properties Section
            var basicSection = CreateFormSection("Basic Properties");
            ActionFormPanel.Children.Add(basicSection);
            
            var basicStack = new StackPanel { Spacing = 10, Margin = new Thickness(10, 5, 0, 15) };
            basicSection.Child = basicStack;
            
            AddFormField(basicStack, "Name", action.Name, (value) => action.Name = value);
            
            // Type field with handler to update TargetType options
            var typeOptions = new[] { "Attack", "Heal", "Buff", "Debuff", "Spell", "Interact", "Move", "UseItem" };
            AddFormField(basicStack, "Type", action.Type, (value) => 
            {
                action.Type = value;
                // Update TargetType options based on selected Type
                UpdateTargetTypeOptions(action, value);
            }, typeOptions);
            
            // TargetType field - will be updated when Type changes
            AddFormField(basicStack, "TargetType", action.TargetType, (value) => action.TargetType = value, GetValidTargetTypes(action.Type));
            AddFormField(basicStack, "Description", action.Description, (value) => action.Description = value, isMultiline: true);
            
            // Numeric Properties Section
            var numericSection = CreateFormSection("Numeric Properties");
            ActionFormPanel.Children.Add(numericSection);
            
            var numericStack = new StackPanel { Spacing = 10, Margin = new Thickness(10, 5, 0, 15) };
            numericSection.Child = numericStack;
            
            AddFormField(numericStack, "BaseValue", action.BaseValue.ToString(), (value) => { if (int.TryParse(value, out int v)) action.BaseValue = v; });
            AddFormField(numericStack, "Range", action.Range.ToString(), (value) => { if (int.TryParse(value, out int v)) action.Range = v; });
            AddFormField(numericStack, "Cooldown", action.Cooldown.ToString(), (value) => { if (int.TryParse(value, out int v)) action.Cooldown = v; });
            AddFormField(numericStack, "DamageMultiplier", action.DamageMultiplier.ToString(), (value) => { if (double.TryParse(value, out double v)) action.DamageMultiplier = v; });
            AddFormField(numericStack, "Length", action.Length.ToString(), (value) => { if (double.TryParse(value, out double v)) action.Length = v; });
            
            // Status Effects Section
            var statusSection = CreateFormSection("Status Effects");
            ActionFormPanel.Children.Add(statusSection);
            
            var statusStack = new StackPanel { Spacing = 10, Margin = new Thickness(10, 5, 0, 15) };
            statusSection.Child = statusStack;
            
            AddBooleanField(statusStack, "CausesBleed", action.CausesBleed, (value) => action.CausesBleed = value);
            AddBooleanField(statusStack, "CausesWeaken", action.CausesWeaken, (value) => action.CausesWeaken = value);
            AddBooleanField(statusStack, "CausesSlow", action.CausesSlow, (value) => action.CausesSlow = value);
            AddBooleanField(statusStack, "CausesPoison", action.CausesPoison, (value) => action.CausesPoison = value);
            AddBooleanField(statusStack, "CausesBurn", action.CausesBurn, (value) => action.CausesBurn = value);
            
            // Combo Properties Section
            var comboSection = CreateFormSection("Combo Properties");
            ActionFormPanel.Children.Add(comboSection);
            
            var comboStack = new StackPanel { Spacing = 10, Margin = new Thickness(10, 5, 0, 15) };
            comboSection.Child = comboStack;
            
            AddBooleanField(comboStack, "IsComboAction", action.IsComboAction, (value) => action.IsComboAction = value);
            AddFormField(comboStack, "ComboOrder", action.ComboOrder.ToString(), (value) => { if (int.TryParse(value, out int v)) action.ComboOrder = v; });
            AddFormField(comboStack, "ComboBonusAmount", action.ComboBonusAmount.ToString(), (value) => { if (int.TryParse(value, out int v)) action.ComboBonusAmount = v; });
            AddFormField(comboStack, "ComboBonusDuration", action.ComboBonusDuration.ToString(), (value) => { if (int.TryParse(value, out int v)) action.ComboBonusDuration = v; });
            
            // Advanced Mechanics Section
            var advancedSection = CreateFormSection("Advanced Mechanics");
            ActionFormPanel.Children.Add(advancedSection);
            
            var advancedStack = new StackPanel { Spacing = 10, Margin = new Thickness(10, 5, 0, 15) };
            advancedSection.Child = advancedStack;
            
            AddFormField(advancedStack, "RollBonus", action.RollBonus.ToString(), (value) => { if (int.TryParse(value, out int v)) action.RollBonus = v; });
            AddFormField(advancedStack, "StatBonus", action.StatBonus.ToString(), (value) => { if (int.TryParse(value, out int v)) action.StatBonus = v; });
            AddFormField(advancedStack, "StatBonusType", action.StatBonusType, (value) => action.StatBonusType = value, 
                new[] { "", "Strength", "Agility", "Technique", "Intelligence" });
            AddFormField(advancedStack, "StatBonusDuration", action.StatBonusDuration.ToString(), (value) => { if (int.TryParse(value, out int v)) action.StatBonusDuration = v; });
            AddFormField(advancedStack, "MultiHitCount", action.MultiHitCount.ToString(), (value) => { if (int.TryParse(value, out int v) && v >= 1) action.MultiHitCount = v; });
            AddFormField(advancedStack, "SelfDamagePercent", action.SelfDamagePercent.ToString(), (value) => { if (int.TryParse(value, out int v)) action.SelfDamagePercent = v; });
            AddBooleanField(advancedStack, "SkipNextTurn", action.SkipNextTurn, (value) => action.SkipNextTurn = value);
            AddBooleanField(advancedStack, "RepeatLastAction", action.RepeatLastAction, (value) => action.RepeatLastAction = value);
            AddFormField(advancedStack, "EnemyRollPenalty", action.EnemyRollPenalty.ToString(), (value) => { if (int.TryParse(value, out int v)) action.EnemyRollPenalty = v; });
            AddFormField(advancedStack, "HealthThreshold", action.HealthThreshold.ToString("F2"), (value) => { if (double.TryParse(value, out double v) && v >= 0.0 && v <= 1.0) action.HealthThreshold = v; });
            AddFormField(advancedStack, "ConditionalDamageMultiplier", action.ConditionalDamageMultiplier.ToString("F2"), (value) => { if (double.TryParse(value, out double v)) action.ConditionalDamageMultiplier = v; });
            
            // Tags Section
            var tagsSection = CreateFormSection("Tags");
            ActionFormPanel.Children.Add(tagsSection);
            
            var tagsStack = new StackPanel { Spacing = 10, Margin = new Thickness(10, 5, 0, 15) };
            tagsSection.Child = tagsStack;
            
            string tagsValue = action.Tags != null ? string.Join(", ", action.Tags) : "";
            AddFormField(tagsStack, "Tags", tagsValue, (value) => 
            {
                if (string.IsNullOrWhiteSpace(value))
                {
                    action.Tags = new List<string>();
                }
                else
                {
                    action.Tags = value.Split(',', StringSplitOptions.RemoveEmptyEntries)
                        .Select(t => t.Trim())
                        .Where(t => !string.IsNullOrWhiteSpace(t))
                        .ToList();
                }
            });
            
            // Save/Cancel buttons
            var buttonStack = new StackPanel 
            { 
                Orientation = Orientation.Horizontal, 
                Spacing = 10, 
                Margin = new Thickness(0, 20, 0, 0),
                HorizontalAlignment = HorizontalAlignment.Right
            };
            
            var saveButton = new Button
            {
                Content = isCreatingNewAction ? "Create Action" : "Save Changes",
                Width = 150,
                Height = 35,
                Background = new SolidColorBrush(Color.FromRgb(76, 175, 80)),
                Foreground = new SolidColorBrush(Colors.White),
                BorderThickness = new Thickness(0),
                CornerRadius = new CornerRadius(3),
                Cursor = new Cursor(StandardCursorType.Hand)
            };
            saveButton.Click += (s, e) => SaveAction(action);
            
            var cancelButton = new Button
            {
                Content = "Cancel",
                Width = 100,
                Height = 35,
                Background = new SolidColorBrush(Color.FromRgb(117, 117, 117)),
                Foreground = new SolidColorBrush(Colors.White),
                BorderThickness = new Thickness(0),
                CornerRadius = new CornerRadius(3),
                Cursor = new Cursor(StandardCursorType.Hand)
            };
            cancelButton.Click += (s, e) => 
            {
                ActionFormPanel.Children.Clear();
                ActionsListBox.SelectedItem = null;
                selectedAction = null;
            };
            
            buttonStack.Children.Add(cancelButton);
            buttonStack.Children.Add(saveButton);
            ActionFormPanel.Children.Add(buttonStack);
        }
        
        /// <summary>
        /// Creates a form section header
        /// </summary>
        private Border CreateFormSection(string title)
        {
            return new Border
            {
                Background = new SolidColorBrush(Color.FromRgb(40, 40, 60)),
                BorderBrush = new SolidColorBrush(Color.FromRgb(100, 100, 150)),
                BorderThickness = new Thickness(2),
                CornerRadius = new CornerRadius(5),
                Padding = new Thickness(15),
                Margin = new Thickness(0, 0, 0, 15)
            };
        }
        
        /// <summary>
        /// Gets valid target types for a given action type
        /// </summary>
        private string[] GetValidTargetTypes(string actionType)
        {
            return actionType switch
            {
                "Attack" => new[] { "SingleTarget" },
                "Spell" => new[] { "SingleTarget" },
                "Heal" => new[] { "Self", "SingleTarget" },
                "Buff" => new[] { "Self" },
                "Debuff" => new[] { "SingleTarget" },
                "Interact" => new[] { "Environment" },
                "Move" => new[] { "Self" },
                "UseItem" => new[] { "Self", "SingleTarget" },
                _ => new[] { "SingleTarget" }
            };
        }
        
        /// <summary>
        /// Updates the TargetType dropdown options when ActionType changes
        /// </summary>
        private void UpdateTargetTypeOptions(ActionData action, string newActionType)
        {
            if (actionFormControls.TryGetValue("TargetType", out var targetTypeControl) && targetTypeControl is ComboBox targetTypeComboBox)
            {
                var validTargetTypes = GetValidTargetTypes(newActionType);
                targetTypeComboBox.ItemsSource = validTargetTypes;
                
                // If current target type is not valid for new action type, set to first valid option
                if (!validTargetTypes.Contains(action.TargetType))
                {
                    action.TargetType = validTargetTypes[0];
                    targetTypeComboBox.SelectedItem = action.TargetType;
                }
                else
                {
                    targetTypeComboBox.SelectedItem = action.TargetType;
                }
            }
        }
        
        /// <summary>
        /// Adds a form field to the stack panel
        /// </summary>
        private void AddFormField(StackPanel parent, string label, string value, Action<string> setter, string[]? options = null, bool isMultiline = false)
        {
            var grid = new Grid();
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(200) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            
            var labelBlock = new TextBlock
            {
                Text = label + ":",
                FontSize = 15,
                Foreground = new SolidColorBrush(Colors.White),
                VerticalAlignment = VerticalAlignment.Center
            };
            Grid.SetColumn(labelBlock, 0);
            grid.Children.Add(labelBlock);
            
            Control inputControl;
            if (options != null && options.Length > 0)
            {
                var comboBox = new ComboBox
                {
                    ItemsSource = options,
                    SelectedItem = value,
                    FontSize = 14,
                    Background = new SolidColorBrush(Color.FromRgb(26, 26, 26)),
                    Foreground = new SolidColorBrush(Colors.White),
                    BorderBrush = new SolidColorBrush(Color.FromRgb(85, 85, 85))
                };
                comboBox.SelectionChanged += (s, e) => 
                {
                    if (comboBox.SelectedItem is string selected) setter(selected);
                };
                inputControl = comboBox;
            }
            else if (isMultiline)
            {
                var textBox = new TextBox
                {
                    Text = value,
                    FontSize = 14,
                    TextWrapping = TextWrapping.Wrap,
                    AcceptsReturn = true,
                    MinHeight = 80,
                    Background = new SolidColorBrush(Colors.White),
                    Foreground = new SolidColorBrush(Colors.Black),
                    BorderBrush = new SolidColorBrush(Color.FromRgb(85, 85, 85))
                };
                textBox.LostFocus += (s, e) => setter(textBox.Text ?? "");
                inputControl = textBox;
            }
            else
            {
                var textBox = new TextBox
                {
                    Text = value,
                    FontSize = 14,
                    Background = new SolidColorBrush(Colors.White),
                    Foreground = new SolidColorBrush(Colors.Black),
                    BorderBrush = new SolidColorBrush(Color.FromRgb(85, 85, 85))
                };
                textBox.LostFocus += (s, e) => setter(textBox.Text ?? "");
                inputControl = textBox;
            }
            
            Grid.SetColumn(inputControl, 1);
            grid.Children.Add(inputControl);
            parent.Children.Add(grid);
            
            actionFormControls[label] = inputControl;
        }
        
        /// <summary>
        /// Adds a boolean field (checkbox) to the stack panel
        /// </summary>
        private void AddBooleanField(StackPanel parent, string label, bool value, Action<bool> setter)
        {
            var grid = new Grid();
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(200) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            
            var labelBlock = new TextBlock
            {
                Text = label + ":",
                FontSize = 15,
                Foreground = new SolidColorBrush(Colors.White),
                VerticalAlignment = VerticalAlignment.Center
            };
            Grid.SetColumn(labelBlock, 0);
            grid.Children.Add(labelBlock);
            
            var checkBox = new CheckBox
            {
                IsChecked = value,
                FontSize = 14,
                VerticalAlignment = VerticalAlignment.Center
            };
            checkBox.IsCheckedChanged += (s, e) => 
            {
                if (checkBox.IsChecked.HasValue)
                    setter(checkBox.IsChecked.Value);
            };
            
            Grid.SetColumn(checkBox, 1);
            grid.Children.Add(checkBox);
            parent.Children.Add(grid);
            
            actionFormControls[label] = checkBox;
        }
        
        /// <summary>
        /// Saves the action
        /// </summary>
        private void SaveAction(ActionData action)
        {
            if (actionEditor == null) return;
            
            // Update all form fields before saving
            foreach (var kvp in actionFormControls)
            {
                if (kvp.Value is TextBox textBox && textBox.IsFocused)
                {
                    textBox.Focusable = false;
                    textBox.Focusable = true;
                }
            }
            
            // Validate action
            string? errorMessage = actionEditor.ValidateAction(action, isCreatingNewAction ? null : action.Name);
            if (errorMessage != null)
            {
                ShowStatusMessage(errorMessage, false);
                return;
            }
            
            // Save action
            bool success;
            if (isCreatingNewAction)
            {
                success = actionEditor.CreateAction(action);
                if (success)
                {
                    ShowStatusMessage($"Action '{action.Name}' created successfully", true);
                    isCreatingNewAction = false;
                }
                else
                {
                    ShowStatusMessage($"Failed to create action '{action.Name}'", false);
                    return;
                }
            }
            else
            {
                success = actionEditor.UpdateAction(action.Name, action);
                if (success)
                {
                    ShowStatusMessage($"Action '{action.Name}' updated successfully", true);
                }
                else
                {
                    ShowStatusMessage($"Failed to update action '{action.Name}'", false);
                    return;
                }
            }
            
            // Reload actions list
            LoadActionsList();
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
            // #region agent log
            try
            {
                string logPath = @"d:\Code Projects\github projects\DungeonFighter-v2\.cursor\debug.log";
                string? logDir = System.IO.Path.GetDirectoryName(logPath);
                if (logDir != null && !System.IO.Directory.Exists(logDir))
                {
                    System.IO.Directory.CreateDirectory(logDir);
                }
                System.IO.File.AppendAllText(logPath, System.Text.Json.JsonSerializer.Serialize(new { sessionId = "debug-session", runId = "run1", hypothesisId = "ENTRY", location = "SettingsPanel.axaml.cs:1044", message = "ExecuteTest called", data = new { commandKey = commandKey }, timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() }) + "\n");
            }
            catch (Exception ex)
            {
                // Log to a fallback location if primary fails
                try { System.IO.File.AppendAllText(@"d:\Code Projects\github projects\DungeonFighter-v2\debug_fallback.log", $"Log error: {ex.Message}\n"); } catch { }
            }
            // #endregion
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
                // #region agent log
                try
                {
                    System.IO.File.AppendAllText(@"d:\Code Projects\github projects\DungeonFighter-v2\.cursor\debug.log", System.Text.Json.JsonSerializer.Serialize(new { sessionId = "debug-session", runId = "run1", hypothesisId = "ENTRY", location = "SettingsPanel.axaml.cs:1055", message = "UI not available - early return", data = new { }, timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() }) + "\n");
                }
                catch { }
                // #endregion
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
                        // #region agent log
                        System.IO.File.AppendAllText(@"d:\Code Projects\github projects\DungeonFighter-v2\.cursor\debug.log", System.Text.Json.JsonSerializer.Serialize(new { sessionId = "debug-session", runId = "run1", hypothesisId = "C", location = "SettingsPanel.axaml.cs:1123", message = "Timer elapsed - calling GetDisplayBufferText", data = new { timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() }, timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() }) + "\n");
                        // #endregion
                        string currentOutput = uiToUse.GetDisplayBufferText();
                        // #region agent log
                        System.IO.File.AppendAllText(@"d:\Code Projects\github projects\DungeonFighter-v2\.cursor\debug.log", System.Text.Json.JsonSerializer.Serialize(new { sessionId = "debug-session", runId = "run1", hypothesisId = "B", location = "SettingsPanel.axaml.cs:1126", message = "GetDisplayBufferText result", data = new { outputLength = currentOutput?.Length ?? 0, isEmpty = string.IsNullOrWhiteSpace(currentOutput), firstChars = currentOutput != null && currentOutput.Length > 0 ? currentOutput.Substring(0, Math.Min(50, currentOutput.Length)) : "null" }, timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() }) + "\n");
                        // #endregion
                        if (!string.IsNullOrWhiteSpace(currentOutput))
                        {
                            Dispatcher.UIThread.Post(() =>
                            {
                                // #region agent log
                                System.IO.File.AppendAllText(@"d:\Code Projects\github projects\DungeonFighter-v2\.cursor\debug.log", System.Text.Json.JsonSerializer.Serialize(new { sessionId = "debug-session", runId = "run1", hypothesisId = "A", location = "SettingsPanel.axaml.cs:1130", message = "Updating TestOutputTextBlock.Text", data = new { textLength = currentOutput.Length, beforeUpdate = TestOutputTextBlock.Text?.Length ?? 0 }, timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() }) + "\n");
                                // #endregion
                                TestOutputTextBlock.Text = currentOutput;
                                // Scroll to bottom after text update
                                if (TestOutputScrollViewer != null)
                                {
                                    // Wait for layout to update, then scroll to bottom
                                    Dispatcher.UIThread.Post(() =>
                                    {
                                        if (TestOutputScrollViewer.Extent.Height > TestOutputScrollViewer.Viewport.Height)
                                        {
                                            double maxScroll = TestOutputScrollViewer.Extent.Height - TestOutputScrollViewer.Viewport.Height;
                                            TestOutputScrollViewer.Offset = new Vector(TestOutputScrollViewer.Offset.X, maxScroll);
                                        }
                                    }, DispatcherPriority.Background);
                                }
                                // #region agent log
                                var scrollViewer = TestOutputScrollViewer ?? (TestOutputTextBlock.Parent as ScrollViewer);
                                double scrollableHeight = 0;
                                if (scrollViewer != null && scrollViewer.Extent.Height > scrollViewer.Viewport.Height)
                                {
                                    scrollableHeight = scrollViewer.Extent.Height - scrollViewer.Viewport.Height;
                                }
                                System.IO.File.AppendAllText(@"d:\Code Projects\github projects\DungeonFighter-v2\.cursor\debug.log", System.Text.Json.JsonSerializer.Serialize(new { sessionId = "debug-session", runId = "run1", hypothesisId = "D", location = "SettingsPanel.axaml.cs:1134", message = "After text update - checking ScrollViewer", data = new { scrollViewerFound = scrollViewer != null, scrollableHeight = scrollableHeight, viewportHeight = scrollViewer?.Viewport.Height ?? 0, extentHeight = scrollViewer?.Extent.Height ?? 0, offsetY = scrollViewer?.Offset.Y ?? 0 }, timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() }) + "\n");
                                // #endregion
                            });
                        }
                    }
                    catch (Exception ex)
                    {
                        // #region agent log
                        System.IO.File.AppendAllText(@"d:\Code Projects\github projects\DungeonFighter-v2\.cursor\debug.log", System.Text.Json.JsonSerializer.Serialize(new { sessionId = "debug-session", runId = "run1", hypothesisId = "C", location = "SettingsPanel.axaml.cs:1138", message = "Timer error", data = new { error = ex.Message, stackTrace = ex.StackTrace }, timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() }) + "\n");
                        // #endregion
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
                            // #region agent log
                            System.IO.File.AppendAllText(@"d:\Code Projects\github projects\DungeonFighter-v2\.cursor\debug.log", System.Text.Json.JsonSerializer.Serialize(new { sessionId = "debug-session", runId = "run1", hypothesisId = "B", location = "SettingsPanel.axaml.cs:1154", message = "Final test output capture", data = new { outputLength = testOutput?.Length ?? 0, isEmpty = string.IsNullOrWhiteSpace(testOutput) }, timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() }) + "\n");
                            // #endregion
                            if (!string.IsNullOrWhiteSpace(testOutput))
                            {
                                // #region agent log
                                System.IO.File.AppendAllText(@"d:\Code Projects\github projects\DungeonFighter-v2\.cursor\debug.log", System.Text.Json.JsonSerializer.Serialize(new { sessionId = "debug-session", runId = "run1", hypothesisId = "A", location = "SettingsPanel.axaml.cs:1158", message = "Setting final test output text", data = new { textLength = testOutput.Length }, timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() }) + "\n");
                                // #endregion
                                TestOutputTextBlock.Text = testOutput;
                                // Scroll to bottom after final text update
                                if (TestOutputScrollViewer != null)
                                {
                                    // Wait for layout to update, then scroll to bottom
                                    Dispatcher.UIThread.Post(() =>
                                    {
                                        if (TestOutputScrollViewer.Extent.Height > TestOutputScrollViewer.Viewport.Height)
                                        {
                                            double maxScroll = TestOutputScrollViewer.Extent.Height - TestOutputScrollViewer.Viewport.Height;
                                            TestOutputScrollViewer.Offset = new Vector(TestOutputScrollViewer.Offset.X, maxScroll);
                                        }
                                    }, DispatcherPriority.Background);
                                }
                                // #region agent log
                                var scrollViewer = TestOutputScrollViewer ?? (TestOutputTextBlock.Parent as ScrollViewer);
                                double scrollableHeight = 0;
                                if (scrollViewer != null && scrollViewer.Extent.Height > scrollViewer.Viewport.Height)
                                {
                                    scrollableHeight = scrollViewer.Extent.Height - scrollViewer.Viewport.Height;
                                }
                                System.IO.File.AppendAllText(@"d:\Code Projects\github projects\DungeonFighter-v2\.cursor\debug.log", System.Text.Json.JsonSerializer.Serialize(new { sessionId = "debug-session", runId = "run1", hypothesisId = "D", location = "SettingsPanel.axaml.cs:1162", message = "After final text update - checking ScrollViewer", data = new { scrollViewerFound = scrollViewer != null, scrollableHeight = scrollableHeight, viewportHeight = scrollViewer?.Viewport.Height ?? 0, extentHeight = scrollViewer?.Extent.Height ?? 0, offsetY = scrollViewer?.Offset.Y ?? 0 }, timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() }) + "\n");
                                // #endregion
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
        /// Helper methods to get Battle Statistics UI elements using FindControl
        /// </summary>
        private Border? GetProgressBorder() => this.FindControl<Border>("ProgressBorder");
        private ProgressBar? GetProgressBar() => this.FindControl<ProgressBar>("ProgressBar");
        private TextBlock? GetProgressStatusText() => this.FindControl<TextBlock>("ProgressStatusText");
        private TextBlock? GetProgressPercentageText() => this.FindControl<TextBlock>("ProgressPercentageText");
        private TextBlock? GetBattleStatisticsResultsText() => this.FindControl<TextBlock>("BattleStatisticsResultsText");
        
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
            
            // Keep settings panel open - don't close it
            // Restore canvas rendering so battle statistics can be displayed
            uiToUse.RestoreDisplayBufferRendering();
            
            // Use GameCoordinator method to show battle statistics
            // This will render on the canvas while settings panel remains visible
            gameCoordinator.ShowBattleStatistics();
        }
        
        /// <summary>
        /// Opens the action editor (legacy - now redirects to tab)
        /// </summary>
        private void OnEditActionsClickLegacy(object? sender, RoutedEventArgs e)
        {
            var uiToUse = GetCanvasUI();
            if (gameCoordinator == null || uiToUse == null)
            {
                ShowStatusMessage("Developer tools not available - game may not be fully initialized", false);
                return;
            }
            
            // Keep settings panel open - don't close it
            // onBack?.Invoke(); // Removed - keep settings panel visible
            
            // Small delay to ensure panel is hidden before showing editor
            Dispatcher.UIThread.Post(() =>
            {
                uiToUse.RestoreDisplayBufferRendering();
                // Use GameCoordinator method to show action editor
                gameCoordinator.ShowActionEditor();
            }, DispatcherPriority.Background);
        }
        
        /// <summary>
        /// Runs a battle test with the specified number of battles
        /// </summary>
        private async Task RunBattleTest(int numberOfBattles)
        {
            if (isBattleStatisticsRunning)
            {
                ShowStatusMessage("A test is already running. Please wait for it to complete.", false);
                return;
            }

            if (gameCoordinator == null)
            {
                ShowStatusMessage("Game not available - cannot run battle statistics", false);
                return;
            }

            isBattleStatisticsRunning = true;
            currentBattleStatisticsResults = null;
            currentWeaponTestResults = null;
            currentComprehensiveResults = null;

            // Show progress UI
            Dispatcher.UIThread.Post(() =>
            {
                var progressBorder = GetProgressBorder();
                var progressBar = GetProgressBar();
                var progressStatusText = GetProgressStatusText();
                var progressPercentageText = GetProgressPercentageText();
                var resultsText = GetBattleStatisticsResultsText();
                
                if (progressBorder != null) progressBorder.IsVisible = true;
                if (progressBar != null) progressBar.Value = 0;
                if (progressStatusText != null) progressStatusText.Text = "Initializing test...";
                if (progressPercentageText != null) progressPercentageText.Text = "0%";
                if (resultsText != null) resultsText.Text = "Running test...";
            });

            try
            {
                // Default test configuration
                var config = new BattleStatisticsRunner.BattleConfiguration
                {
                    PlayerDamage = 10,
                    PlayerAttackSpeed = 1.0,
                    PlayerArmor = 2,
                    PlayerHealth = 100,
                    EnemyDamage = 8,
                    EnemyAttackSpeed = 1.2,
                    EnemyArmor = 1,
                    EnemyHealth = 80
                };

                var progress = new Progress<(int completed, int total, string status)>(p =>
                {
                    Dispatcher.UIThread.Post(() =>
                    {
                        var progressBar = GetProgressBar();
                        var progressStatusText = GetProgressStatusText();
                        var progressPercentageText = GetProgressPercentageText();
                        
                        double percentage = p.total > 0 ? (double)p.completed / p.total * 100 : 0;
                        if (progressBar != null) progressBar.Value = percentage;
                        if (progressStatusText != null) progressStatusText.Text = $"{p.status} ({p.completed}/{p.total})";
                        if (progressPercentageText != null) progressPercentageText.Text = $"{percentage:F1}%";
                    });
                });

                currentBattleStatisticsResults = await BattleStatisticsRunner.RunParallelBattles(
                    config,
                    numberOfBattles,
                    progress
                );

                // Display results
                DisplayBattleStatisticsResults(currentBattleStatisticsResults);
            }
            catch (Exception ex)
            {
                Dispatcher.UIThread.Post(() =>
                {
                    var resultsText = GetBattleStatisticsResultsText();
                    if (resultsText != null) resultsText.Text = $"Error running battle test: {ex.Message}\n\n{ex.StackTrace}";
                    ShowStatusMessage($"Error: {ex.Message}", false);
                });
            }
            finally
            {
                isBattleStatisticsRunning = false;
                Dispatcher.UIThread.Post(() =>
                {
                    var progressBorder = GetProgressBorder();
                    if (progressBorder != null) progressBorder.IsVisible = false;
                });
            }
        }

        /// <summary>
        /// Runs weapon type tests
        /// </summary>
        private async Task RunWeaponTypeTests()
        {
            if (isBattleStatisticsRunning)
            {
                ShowStatusMessage("A test is already running. Please wait for it to complete.", false);
                return;
            }

            if (gameCoordinator == null)
            {
                ShowStatusMessage("Game not available - cannot run battle statistics", false);
                return;
            }

            isBattleStatisticsRunning = true;
            currentBattleStatisticsResults = null;
            currentWeaponTestResults = null;
            currentComprehensiveResults = null;

            // Show progress UI
            Dispatcher.UIThread.Post(() =>
            {
                var progressBorder = GetProgressBorder();
                var progressBar = GetProgressBar();
                var progressStatusText = GetProgressStatusText();
                var progressPercentageText = GetProgressPercentageText();
                var resultsText = GetBattleStatisticsResultsText();
                
                if (progressBorder != null) progressBorder.IsVisible = true;
                if (progressBar != null) progressBar.Value = 0;
                if (progressStatusText != null) progressStatusText.Text = "Initializing weapon type tests...";
                if (progressPercentageText != null) progressPercentageText.Text = "0%";
                if (resultsText != null) resultsText.Text = "Running weapon type tests...";
            });

            try
            {
                var progress = new Progress<(int completed, int total, string status)>(p =>
                {
                    Dispatcher.UIThread.Post(() =>
                    {
                        var progressBar = GetProgressBar();
                        var progressStatusText = GetProgressStatusText();
                        var progressPercentageText = GetProgressPercentageText();
                        
                        double percentage = p.total > 0 ? (double)p.completed / p.total * 100 : 0;
                        if (progressBar != null) progressBar.Value = percentage;
                        if (progressStatusText != null) progressStatusText.Text = $"{p.status} ({p.completed}/{p.total})";
                        if (progressPercentageText != null) progressPercentageText.Text = $"{percentage:F1}%";
                    });
                });

                currentWeaponTestResults = await BattleStatisticsRunner.RunWeaponTypeTests(
                    battlesPerWeapon: 50,
                    playerLevel: 1,
                    enemyLevel: 1,
                    progress
                );

                // Display weapon test results
                DisplayWeaponTestResults(currentWeaponTestResults);
            }
            catch (Exception ex)
            {
                Dispatcher.UIThread.Post(() =>
                {
                    var resultsText = GetBattleStatisticsResultsText();
                    if (resultsText != null) resultsText.Text = $"Error running weapon type tests: {ex.Message}\n\n{ex.StackTrace}";
                    ShowStatusMessage($"Error: {ex.Message}", false);
                });
            }
            finally
            {
                isBattleStatisticsRunning = false;
                Dispatcher.UIThread.Post(() =>
                {
                    var progressBorder = GetProgressBorder();
                    if (progressBorder != null) progressBorder.IsVisible = false;
                });
            }
        }

        /// <summary>
        /// Runs comprehensive weapon-enemy tests
        /// </summary>
        private async Task RunComprehensiveWeaponEnemyTests()
        {
            if (isBattleStatisticsRunning)
            {
                ShowStatusMessage("A test is already running. Please wait for it to complete.", false);
                return;
            }

            if (gameCoordinator == null)
            {
                ShowStatusMessage("Game not available - cannot run battle statistics", false);
                return;
            }

            isBattleStatisticsRunning = true;
            currentBattleStatisticsResults = null;
            currentWeaponTestResults = null;
            currentComprehensiveResults = null;

            // Show progress UI
            Dispatcher.UIThread.Post(() =>
            {
                var progressBorder = GetProgressBorder();
                var progressBar = GetProgressBar();
                var progressStatusText = GetProgressStatusText();
                var progressPercentageText = GetProgressPercentageText();
                var resultsText = GetBattleStatisticsResultsText();
                
                if (progressBorder != null) progressBorder.IsVisible = true;
                if (progressBar != null) progressBar.Value = 0;
                if (progressStatusText != null) progressStatusText.Text = "Initializing comprehensive tests...";
                if (progressPercentageText != null) progressPercentageText.Text = "0%";
                if (resultsText != null) resultsText.Text = "Running comprehensive weapon vs enemy tests...";
            });

            try
            {
                var progress = new Progress<(int completed, int total, string status)>(p =>
                {
                    Dispatcher.UIThread.Post(() =>
                    {
                        var progressBar = GetProgressBar();
                        var progressStatusText = GetProgressStatusText();
                        var progressPercentageText = GetProgressPercentageText();
                        
                        double percentage = p.total > 0 ? (double)p.completed / p.total * 100 : 0;
                        if (progressBar != null) progressBar.Value = percentage;
                        if (progressStatusText != null) progressStatusText.Text = $"{p.status} ({p.completed}/{p.total})";
                        if (progressPercentageText != null) progressPercentageText.Text = $"{percentage:F1}%";
                    });
                });

                currentComprehensiveResults = await BattleStatisticsRunner.RunComprehensiveWeaponEnemyTests(
                    battlesPerCombination: 10,
                    playerLevel: 1,
                    enemyLevel: 1,
                    progress
                );

                // Display comprehensive results
                DisplayComprehensiveResults(currentComprehensiveResults);
            }
            catch (Exception ex)
            {
                Dispatcher.UIThread.Post(() =>
                {
                    var resultsText = GetBattleStatisticsResultsText();
                    if (resultsText != null) resultsText.Text = $"Error running comprehensive tests: {ex.Message}\n\n{ex.StackTrace}";
                    ShowStatusMessage($"Error: {ex.Message}", false);
                });
            }
            finally
            {
                isBattleStatisticsRunning = false;
                Dispatcher.UIThread.Post(() =>
                {
                    var progressBorder = GetProgressBorder();
                    if (progressBorder != null) progressBorder.IsVisible = false;
                });
            }
        }

        /// <summary>
        /// Displays battle statistics results in the results text block
        /// </summary>
        private void DisplayBattleStatisticsResults(BattleStatisticsRunner.StatisticsResult results)
        {
            if (results == null) return;

            var output = new System.Text.StringBuilder();
            output.AppendLine("=== BATTLE STATISTICS RESULTS ===");
            output.AppendLine();
            
            output.AppendLine("Configuration:");
            output.AppendLine($"  Player: {results.Config.PlayerDamage} dmg, {results.Config.PlayerAttackSpeed:F2} speed, {results.Config.PlayerArmor} armor, {results.Config.PlayerHealth} HP");
            output.AppendLine($"  Enemy:  {results.Config.EnemyDamage} dmg, {results.Config.EnemyAttackSpeed:F2} speed, {results.Config.EnemyArmor} armor, {results.Config.EnemyHealth} HP");
            output.AppendLine();
            
            output.AppendLine("Results:");
            output.AppendLine($"  Total Battles: {results.TotalBattles}");
            output.AppendLine($"  Player Wins: {results.PlayerWins} ({results.WinRate:F1}%)");
            output.AppendLine($"  Enemy Wins: {results.EnemyWins} ({100.0 - results.WinRate:F1}%)");
            output.AppendLine();
            
            output.AppendLine("Turn Statistics:");
            output.AppendLine($"  Average Turns: {results.AverageTurns:F2}");
            output.AppendLine($"  Min Turns: {results.MinTurns}");
            output.AppendLine($"  Max Turns: {results.MaxTurns}");
            output.AppendLine();
            
            output.AppendLine("Damage Statistics:");
            output.AppendLine($"  Average Player Damage Dealt: {results.AveragePlayerDamageDealt:F2}");
            output.AppendLine($"  Average Enemy Damage Dealt: {results.AverageEnemyDamageDealt:F2}");

            Dispatcher.UIThread.Post(() =>
            {
                var resultsText = GetBattleStatisticsResultsText();
                if (resultsText != null) resultsText.Text = output.ToString();
            });
        }

        /// <summary>
        /// Displays weapon test results
        /// </summary>
        private void DisplayWeaponTestResults(List<BattleStatisticsRunner.WeaponTestResult> results)
        {
            if (results == null || results.Count == 0) return;

            var output = new System.Text.StringBuilder();
            output.AppendLine("=== WEAPON TYPE TEST RESULTS ===");
            output.AppendLine();
            
            foreach (var result in results.OrderByDescending(r => r.WinRate))
            {
                output.AppendLine($"{result.WeaponType}:");
                output.AppendLine($"  Wins: {result.PlayerWins}/{result.TotalBattles} ({result.WinRate:F1}%)");
                output.AppendLine($"  Average Turns: {result.AverageTurns:F2}");
                output.AppendLine($"  Average Player Damage: {result.AveragePlayerDamageDealt:F2}");
                output.AppendLine($"  Average Enemy Damage: {result.AverageEnemyDamageDealt:F2}");
                output.AppendLine();
            }

            Dispatcher.UIThread.Post(() =>
            {
                var resultsText = GetBattleStatisticsResultsText();
                if (resultsText != null) resultsText.Text = output.ToString();
            });
        }

        /// <summary>
        /// Displays comprehensive weapon-enemy test results
        /// </summary>
        private void DisplayComprehensiveResults(BattleStatisticsRunner.ComprehensiveWeaponEnemyTestResult results)
        {
            if (results == null) return;

            var output = new System.Text.StringBuilder();
            output.AppendLine("=== COMPREHENSIVE WEAPON VS ENEMY TEST RESULTS ===");
            output.AppendLine();
            
            output.AppendLine("Overall Statistics:");
            output.AppendLine($"  Total Battles: {results.TotalBattles}");
            output.AppendLine($"  Player Wins: {results.TotalPlayerWins} ({results.OverallWinRate:F1}%)");
            output.AppendLine($"  Enemy Wins: {results.TotalEnemyWins}");
            output.AppendLine($"  Average Turns: {results.OverallAverageTurns:F1}");
            output.AppendLine($"  Average Player Damage: {results.OverallAveragePlayerDamage:F1}");
            output.AppendLine($"  Average Enemy Damage: {results.OverallAverageEnemyDamage:F1}");
            output.AppendLine();
            
            if (results.WeaponStatistics != null && results.WeaponStatistics.Count > 0)
            {
                output.AppendLine("Weapon Performance (across all enemies):");
                foreach (var weaponType in results.WeaponTypes)
                {
                    if (results.WeaponStatistics.TryGetValue(weaponType, out var weaponStats))
                    {
                        output.AppendLine($"  {weaponType,-8}: {weaponStats.Wins,3}/{weaponStats.TotalBattles,3} wins ({weaponStats.WinRate,5:F1}%) | Avg Turns: {weaponStats.AverageTurns,5:F1} | Avg Damage: {weaponStats.AverageDamage,5:F1}");
                    }
                }
                output.AppendLine();
            }
            
            if (results.EnemyStatistics != null && results.EnemyStatistics.Count > 0)
            {
                output.AppendLine("Enemy Difficulty (across all weapons):");
                foreach (var enemyType in results.EnemyTypes.OrderByDescending(e => 
                    results.EnemyStatistics.TryGetValue(e, out var enemyStats) ? enemyStats.WinRate : 0))
                {
                    if (results.EnemyStatistics.TryGetValue(enemyType, out var enemyStats))
                    {
                        output.AppendLine($"  {enemyType,-15}: {enemyStats.Wins,3}/{enemyStats.TotalBattles,3} wins ({enemyStats.WinRate,5:F1}%) | Avg Turns: {enemyStats.AverageTurns,5:F1}");
                    }
                }
            }

            Dispatcher.UIThread.Post(() =>
            {
                var resultsText = GetBattleStatisticsResultsText();
                if (resultsText != null) resultsText.Text = output.ToString();
            });
        }
        
    }
}

