using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Media;
using Avalonia.Threading;
using RPGGame;
using RPGGame.UI.Avalonia.Managers;
using RPGGame.UI.Avalonia.Managers.Settings;
using RPGGame.UI.Avalonia.Managers.Settings.PanelHandlers;
using RPGGame.UI.Avalonia.Settings;
using RPGGame.Utils;
using System;
using System.Collections.Generic;

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
        private ItemModifiersTabManager? itemModifiersTabManager;
        private ItemsTabManager? itemsTabManager;
        private SettingsManager? settingsManager;
        private Managers.TestRunnerUI? testRunnerUI;
        private Managers.SettingsColorManager? colorManager;
        
        // Extracted managers
        private SettingsPersistenceManager? persistenceManager;
        private SettingsInitialization? initialization;
        private SettingsActionTestGenerator? actionTestGenerator;
        
        // Panel handler registry and orchestrator
        private PanelHandlerRegistry? panelHandlerRegistry;
        private SettingsSaveOrchestrator? saveOrchestrator;
        
        // Lazy-loaded panels
        private Dictionary<string, UserControl> loadedPanels = new Dictionary<string, UserControl>();
        
        public SettingsPanel()
        {
            InitializeComponent();
            settings = GameSettings.Instance;
            InitializeManagers();
            SetupNavigation();
            
            // Apply colors after panel is loaded
            this.Loaded += (s, e) =>
            {
                Dispatcher.UIThread.Post(() =>
                {
                    colorManager?.ApplyColors();
                }, DispatcherPriority.Loaded);
            };
        }
        
        private void InitializeManagers()
        {
            settingsManager = new SettingsManager(settings, ShowStatusMessage, updateStatus);
            colorManager = new Managers.SettingsColorManager(this, settings);
            gameVariablesTabManager = new GameVariablesTabManager();
            actionsTabManager = new ActionsTabManager();
            itemModifiersTabManager = new Managers.ItemModifiersTabManager(ShowStatusMessage);
            itemsTabManager = new Managers.ItemsTabManager(ShowStatusMessage);
            
            // Initialize extracted managers
            persistenceManager = new SettingsPersistenceManager(settingsManager, gameVariablesTabManager);
            initialization = new SettingsInitialization(
                settingsManager,
                gameVariablesTabManager,
                actionsTabManager,
                ShowStatusMessage);
            
            // Initialize action test generator (will be fully initialized when canvasUI is set)
            actionTestGenerator = new SettingsActionTestGenerator(null, ShowStatusMessage);
            
            // Wire up animation configuration updates for real-time changes
            initialization.InitializeAnimationConfiguration();
            
            // Initialize panel handler registry
            panelHandlerRegistry = new PanelHandlerRegistry();
            panelHandlerRegistry.Register(new GameplayPanelHandler(settings, persistenceManager));
            panelHandlerRegistry.Register(new TextDelaysPanelHandler(settingsManager));
            panelHandlerRegistry.Register(new AppearancePanelHandler(settings, colorManager));
            // Testing handler will be registered when canvasUI is available
            
            // Initialize save orchestrator
            saveOrchestrator = new SettingsSaveOrchestrator(
                settingsManager,
                gameVariablesTabManager,
                itemModifiersTabManager,
                itemsTabManager,
                settings,
                ShowStatusMessage,
                loadedPanels,
                LoadCategoryPanel);
        }
        
        private void SetupNavigation()
        {
            // Handle category selection
            CategoryListBox.SelectionChanged += (s, e) =>
            {
                if (CategoryListBox.SelectedItem is ListBoxItem selectedItem && selectedItem.Tag is string categoryTag)
                {
                    LoadCategoryPanel(categoryTag);
                }
            };
            
            // Select first category by default
            if (CategoryListBox.ItemCount > 0)
            {
                CategoryListBox.SelectedIndex = 0;
            }
            
            // Wire up action buttons
            SaveButton.Click += (s, e) => SaveSettings();
            ResetButton.Click += (s, e) => ResetSettings();
            BackButton.Click += (s, e) => onBack?.Invoke();
        }
        
        private void LoadCategoryPanel(string categoryTag)
        {
            // Special handling for Testing panel - use separate content area without ScrollViewer
            if (categoryTag == "Testing")
            {
                // Check if panel is already loaded
                if (loadedPanels.ContainsKey(categoryTag))
                {
                    TestingContentArea.Content = loadedPanels[categoryTag];
                    TestingContentArea.IsVisible = true;
                    ContentScrollViewer.IsVisible = false;
                    return;
                }
                
                // Create Testing panel
                var testingPanel = new TestingSettingsPanel();
                loadedPanels[categoryTag] = testingPanel;
                TestingContentArea.Content = testingPanel;
                TestingContentArea.IsVisible = true;
                ContentScrollViewer.IsVisible = false;
                
                // Initialize panel-specific handlers after a short delay
                Dispatcher.UIThread.Post(() =>
                {
                    InitializePanelHandlers(categoryTag, testingPanel);
                }, DispatcherPriority.Background);
                return;
            }
            
            // For all other panels, use the regular content area with ScrollViewer
            TestingContentArea.IsVisible = false;
            ContentScrollViewer.IsVisible = true;
            
            // Check if panel is already loaded
            if (loadedPanels.ContainsKey(categoryTag))
            {
                ContentArea.Content = loadedPanels[categoryTag];
                return;
            }
            
            // Create panel on-demand (lazy loading)
            UserControl? panel = categoryTag switch
            {
                "Gameplay" => new GameplaySettingsPanel(),
                "GameVariables" => new GameVariablesSettingsPanel(),
                "Actions" => new ActionsSettingsPanel(),
                "TextDelays" => new TextDelaysSettingsPanel(),
                "Appearance" => new AppearanceSettingsPanel(),
                "ItemModifiers" => new ItemModifiersSettingsPanel(),
                "Items" => new ItemsSettingsPanel(),
                "BalanceTuning" => new BalanceTuningSettingsPanel(),
                "About" => new AboutSettingsPanel(),
                _ => null
            };
            
            if (panel != null)
            {
                loadedPanels[categoryTag] = panel;
                ContentArea.Content = panel;
                
                // Initialize panel-specific handlers after a short delay
                Dispatcher.UIThread.Post(() =>
                {
                    InitializePanelHandlers(categoryTag, panel);
                }, DispatcherPriority.Background);
            }
        }
        
        private void InitializePanelHandlers(string categoryTag, UserControl panel)
        {
            // Use registry to get handler for standard panels
            var handler = panelHandlerRegistry?.GetHandler(categoryTag);
            if (handler != null)
            {
                handler.WireUp(panel);
                return;
            }
            
            // Handle special cases that don't use the handler pattern
            switch (categoryTag)
            {
                case "GameVariables":
                    if (panel is GameVariablesSettingsPanel gameVarsPanel && initialization != null)
                    {
                        // Initialize the GameVariables tab with its controls
                        Dispatcher.UIThread.Post(() =>
                        {
                            initialization.InitializeGameVariablesTab(gameVarsPanel);
                        }, DispatcherPriority.Loaded);
                    }
                    break;
                case "Actions":
                    if (panel is ActionsSettingsPanel actionsPanel && initialization != null)
                    {
                        // Initialize the Actions tab with its controls
                        Dispatcher.UIThread.Post(() =>
                        {
                            initialization.InitializeActionsTab(actionsPanel);
                        }, DispatcherPriority.Loaded);
                    }
                    break;
                case "BalanceTuning":
                    if (panel is BalanceTuningSettingsPanel balanceTuningPanel)
                    {
                        balanceTuningPanel.SetStatusCallback(ShowStatusMessage);
                    }
                    break;
                case "ItemModifiers":
                    if (panel is ItemModifiersSettingsPanel itemModifiersPanel && itemModifiersTabManager != null)
                    {
                        Dispatcher.UIThread.Post(() =>
                        {
                            itemModifiersTabManager.Initialize(itemModifiersPanel);
                        }, DispatcherPriority.Loaded);
                    }
                    break;
                case "Items":
                    if (panel is ItemsSettingsPanel itemsPanel && itemsTabManager != null)
                    {
                        Dispatcher.UIThread.Post(() =>
                        {
                            itemsTabManager.Initialize(itemsPanel);
                        }, DispatcherPriority.Loaded);
                    }
                    break;
                case "Testing":
                    if (panel is TestingSettingsPanel testingPanel && canvasUI != null)
                    {
                        testRunnerUI = new Managers.TestRunnerUI(canvasUI);
                        // Register testing handler if not already registered
                        if (panelHandlerRegistry != null && !panelHandlerRegistry.HasHandler("Testing"))
                        {
                            var testingHandler = new TestingPanelHandler(canvasUI);
                            panelHandlerRegistry.Register(testingHandler);
                            testingHandler.WireUp(panel);
                        }
                    }
                    break;
            }
        }
        
        public void SetBackCallback(System.Action callback)
        {
            onBack = callback;
        }
        
        public void SetStatusCallback(System.Action<string> callback)
        {
            updateStatus = callback;
        }
        
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
            
            if (canvasUI != null)
            {
                testRunnerUI = new Managers.TestRunnerUI(canvasUI);
                actionTestGenerator = new SettingsActionTestGenerator(canvasUI, ShowStatusMessage);
            }
        }
        
        private void SaveSettings()
        {
            saveOrchestrator?.SaveSettings();
        }
        
        private void ResetSettings()
        {
            if (settingsManager == null) return;
            
            try
            {
                settingsManager.ResetToDefaults();
                // Reload current panel
                if (CategoryListBox.SelectedItem is ListBoxItem selectedItem && selectedItem.Tag is string categoryTag)
                {
                    if (loadedPanels.ContainsKey(categoryTag))
                    {
                        loadedPanels.Remove(categoryTag);
                        LoadCategoryPanel(categoryTag);
                    }
                }
                ShowStatusMessage("Settings reset to defaults", true);
            }
            catch (Exception ex)
            {
                ScrollDebugLogger.Log($"SettingsPanel: Error resetting settings: {ex.Message}");
                ShowStatusMessage($"Error resetting settings: {ex.Message}", false);
            }
        }
        
        private void ShowStatusMessage(string message, bool isSuccess)
        {
            updateStatus?.Invoke(message);
            ScrollDebugLogger.Log($"SettingsPanel: {message}");
        }
    }
}
