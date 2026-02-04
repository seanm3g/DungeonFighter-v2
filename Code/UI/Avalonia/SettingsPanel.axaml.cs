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
        private Managers.StatusEffectsTabManager? statusEffectsTabManager;
        private SettingsManager? settingsManager;
        private Managers.TestRunnerUI? testRunnerUI;
        private Managers.SettingsColorManager? colorManager;
        
        // Extracted managers
        private SettingsInitialization? initialization;
        private SettingsActionTestGenerator? actionTestGenerator;
        
        // Panel handler registry and orchestrator
        private PanelHandlerRegistry? panelHandlerRegistry;
        private SettingsSaveOrchestrator? saveOrchestrator;
        
        // Table-driven initializers for categories that don't use the handler pattern (replaces long switch)
        private Dictionary<string, Action<UserControl, PanelInitializerContext>>? panelInitializers;
        private PanelInitializerContext? initializerContext;
        
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
            statusEffectsTabManager = new Managers.StatusEffectsTabManager();
            
            // Set game variables tab manager in settings manager (needed for save operations)
            settingsManager.SetGameVariablesTabManager(gameVariablesTabManager);
            
            // Initialize extracted managers
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
            panelHandlerRegistry.Register(new GameplayPanelHandler(settings, settingsManager, ShowStatusMessage));
            panelHandlerRegistry.Register(new TextDelaysPanelHandler(settingsManager));
            panelHandlerRegistry.Register(new AppearancePanelHandler(settings, colorManager));
            // Testing handler will be registered when canvasUI is available
            
            // Initialize save orchestrator (single panel resolution via GetPanelForCategory)
            saveOrchestrator = new SettingsSaveOrchestrator(
                settingsManager,
                panelHandlerRegistry,
                gameVariablesTabManager,
                actionsTabManager,
                itemModifiersTabManager,
                itemsTabManager,
                settings,
                ShowStatusMessage,
                GetPanelForCategory);
            
            // Table-driven initializers: add a new tab by adding one entry here (and to SettingsPanelCatalog if main content)
            initializerContext = new PanelInitializerContext
            {
                Initialization = initialization,
                StatusEffectsTabManager = statusEffectsTabManager,
                ItemModifiersTabManager = itemModifiersTabManager,
                ItemsTabManager = itemsTabManager,
                PanelHandlerRegistry = panelHandlerRegistry,
                ShowStatusMessage = ShowStatusMessage
            };
            panelInitializers = new Dictionary<string, Action<UserControl, PanelInitializerContext>>(StringComparer.OrdinalIgnoreCase)
            {
                ["GameVariables"] = (panel, ctx) =>
                {
                    if (panel is GameVariablesSettingsPanel gameVarsPanel && ctx.Initialization != null)
                        Dispatcher.UIThread.Post(() => ctx.Initialization.InitializeGameVariablesTab(gameVarsPanel), DispatcherPriority.Loaded);
                },
                ["Actions"] = (panel, ctx) =>
                {
                    if (panel is ActionsSettingsPanel actionsPanel && ctx.Initialization != null)
                        Dispatcher.UIThread.Post(() => ctx.Initialization.InitializeActionsTab(actionsPanel), DispatcherPriority.Loaded);
                },
                ["StatusEffects"] = (panel, ctx) =>
                {
                    if (panel is StatusEffectsSettingsPanel statusPanel && ctx.StatusEffectsTabManager != null && ctx.ShowStatusMessage != null)
                        Dispatcher.UIThread.Post(() =>
                        {
                            var listBox = statusPanel.FindControl<ListBox>("StatusEffectsListBox");
                            var formPanel = statusPanel.FindControl<StackPanel>("StatusEffectFormPanel");
                            var createButton = statusPanel.FindControl<Button>("CreateStatusEffectButton");
                            var deleteButton = statusPanel.FindControl<Button>("DeleteStatusEffectButton");
                            if (listBox != null && formPanel != null && createButton != null && deleteButton != null)
                                ctx.StatusEffectsTabManager.Initialize(listBox, formPanel, createButton, deleteButton, ctx.ShowStatusMessage);
                        }, DispatcherPriority.Loaded);
                },
                ["BalanceTuning"] = (panel, ctx) =>
                {
                    if (panel is BalanceTuningSettingsPanel balancePanel && ctx.ShowStatusMessage != null)
                        balancePanel.SetStatusCallback(ctx.ShowStatusMessage);
                },
                ["ItemModifiers"] = (panel, ctx) =>
                {
                    if (panel is ItemModifiersSettingsPanel modPanel && ctx.ItemModifiersTabManager != null)
                        Dispatcher.UIThread.Post(() => ctx.ItemModifiersTabManager.Initialize(modPanel), DispatcherPriority.Loaded);
                },
                ["Items"] = (panel, ctx) =>
                {
                    if (panel is ItemsSettingsPanel itemsPanel && ctx.ItemsTabManager != null)
                        Dispatcher.UIThread.Post(() => ctx.ItemsTabManager.Initialize(itemsPanel), DispatcherPriority.Loaded);
                },
                ["Testing"] = (panel, ctx) =>
                {
                    ctx.RegisterTestingHandlerAndWireUp?.Invoke(panel);
                }
            };
        }

        /// <summary>Refreshes all settings references from GameSettings.Instance. Call after ReloadFromFile() when opening settings.</summary>
        public void RefreshSettingsFromFile()
        {
            settings = GameSettings.Instance;
            saveOrchestrator?.RefreshSettings(settings);
            settingsManager?.RefreshSettings(settings);
            (panelHandlerRegistry?.GetHandler("Gameplay") as Managers.Settings.PanelHandlers.GameplayPanelHandler)?.RefreshSettings(settings);
            // Ensure the selected category's panel is loaded (e.g. first open or SelectedIndex was set before items existed)
            if (CategoryListBox.SelectedItem is ListBoxItem selectedItem && selectedItem.Tag is string categoryTag
                && !loadedPanels.ContainsKey(categoryTag))
                LoadCategoryPanel(categoryTag);
            // Refresh the currently visible panel now so it shows file values before the overlay is visible (do not defer or we overwrite user changes later)
            RefreshCurrentPanelFromSettings();
        }

        private void RefreshCurrentPanelFromSettings()
        {
            if (CategoryListBox.SelectedItem is not ListBoxItem selectedItem || selectedItem.Tag is not string categoryTag)
                return;
            var handler = panelHandlerRegistry?.GetHandler(categoryTag);
            if (handler == null) return;
            // Apply to the panel actually visible so file values show on what the user sees
            UserControl? target = ContentScrollViewer.IsVisible ? ContentArea.Content as UserControl
                : TestingContentArea.IsVisible ? TestingContentArea.Content as UserControl
                : ActionsContentArea.Content as UserControl;
            if (target == null && loadedPanels.TryGetValue(categoryTag, out var cached))
                target = cached;
            if (target != null)
                handler.LoadSettings(target);
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
                    ActionsContentArea.IsVisible = false;
                    return;
                }
                
                // Create Testing panel
                var testingPanel = new TestingSettingsPanel();
                loadedPanels[categoryTag] = testingPanel;
                TestingContentArea.Content = testingPanel;
                TestingContentArea.IsVisible = true;
                ContentScrollViewer.IsVisible = false;
                ActionsContentArea.IsVisible = false;
                
                // Initialize panel-specific handlers after a short delay
                Dispatcher.UIThread.Post(() =>
                {
                    InitializePanelHandlers(categoryTag, testingPanel);
                }, DispatcherPriority.Background);
                return;
            }
            
            // Actions panel - use separate content area so list and form scroll independently
            if (categoryTag == "Actions")
            {
                if (loadedPanels.ContainsKey(categoryTag))
                {
                    ActionsContentArea.Content = loadedPanels[categoryTag];
                    ActionsContentArea.IsVisible = true;
                    ContentScrollViewer.IsVisible = false;
                    TestingContentArea.IsVisible = false;
                    return;
                }
                
                var actionsPanel = new ActionsSettingsPanel();
                loadedPanels[categoryTag] = actionsPanel;
                ActionsContentArea.Content = actionsPanel;
                ActionsContentArea.IsVisible = true;
                ContentScrollViewer.IsVisible = false;
                TestingContentArea.IsVisible = false;
                
                Dispatcher.UIThread.Post(() =>
                {
                    InitializePanelHandlers(categoryTag, actionsPanel);
                }, DispatcherPriority.Background);
                return;
            }
            
            // For all other panels, use the regular content area with ScrollViewer
            TestingContentArea.IsVisible = false;
            ActionsContentArea.IsVisible = false;
            ContentScrollViewer.IsVisible = true;
            
            // Check if panel is already loaded (e.g. user switched away and back, or reopened settings window)
            if (loadedPanels.ContainsKey(categoryTag))
            {
                var cachedPanel = loadedPanels[categoryTag];
                ContentArea.Content = cachedPanel;
                // Refresh UI from current settings so reopened window or re-selected tab shows saved values
                var handler = panelHandlerRegistry?.GetHandler(categoryTag);
                if (handler != null)
                    handler.LoadSettings(cachedPanel);
                return;
            }
            
            // Create panel on-demand (lazy loading) from catalog
            UserControl? panel = SettingsPanelCatalog.CreatePanel(categoryTag);
            if (panel != null)
            {
                loadedPanels[categoryTag] = panel;
                InitializePanelHandlers(categoryTag, panel);
                ContentArea.Content = panel;
            }
        }
        
        private void InitializePanelHandlers(string categoryTag, UserControl panel)
        {
            // Use registry to get handler for standard panels (Gameplay, TextDelays, Appearance)
            var handler = panelHandlerRegistry?.GetHandler(categoryTag);
            if (handler != null)
            {
                handler.WireUp(panel);
                return;
            }
            
            // Table-driven initializers for categories that don't use the handler pattern
            if (panelInitializers != null && initializerContext != null && panelInitializers.TryGetValue(categoryTag, out var initializer))
            {
                initializer(panel, initializerContext);
            }
        }
        
        /// <summary>Resolves the panel instance for a category: the currently displayed panel if it matches the tag, otherwise the cached panel from loadedPanels. Used by the save orchestrator for consistent panel resolution.</summary>
        public UserControl? GetPanelForCategory(string categoryTag, UserControl? currentlyDisplayed)
        {
            if (currentlyDisplayed != null && GetCategoryTagForPanel(currentlyDisplayed) == categoryTag)
                return currentlyDisplayed;
            return loadedPanels.TryGetValue(categoryTag, out var panel) ? panel : null;
        }

        /// <summary>Returns the category tag for a panel type (e.g. GameplaySettingsPanel -> "Gameplay"). Used by GetPanelForCategory.</summary>
        internal static string? GetCategoryTagForPanel(UserControl? panel)
        {
            if (panel == null) return null;
            if (panel is GameplaySettingsPanel) return "Gameplay";
            if (panel is GameVariablesSettingsPanel) return "GameVariables";
            if (panel is TextDelaysSettingsPanel) return "TextDelays";
            if (panel is AppearanceSettingsPanel) return "Appearance";
            if (panel is TestingSettingsPanel) return "Testing";
            if (panel is ActionsSettingsPanel) return "Actions";
            if (panel is StatusEffectsSettingsPanel) return "StatusEffects";
            if (panel is ItemModifiersSettingsPanel) return "ItemModifiers";
            if (panel is ItemsSettingsPanel) return "Items";
            if (panel is BalanceTuningSettingsPanel) return "BalanceTuning";
            if (panel is AboutSettingsPanel) return "About";
            return null;
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
                // Wire Testing panel initializer to register handler and WireUp when Testing tab is first loaded
                if (initializerContext != null)
                {
                    var uiForTesting = canvasUI;
                    initializerContext.RegisterTestingHandlerAndWireUp = (testingPanel) =>
                    {
                        if (testingPanel is not TestingSettingsPanel) return;
                        if (panelHandlerRegistry != null && !panelHandlerRegistry.HasHandler("Testing"))
                        {
                            var testingHandler = new TestingPanelHandler(uiForTesting);
                            panelHandlerRegistry.Register(testingHandler);
                            testingHandler.WireUp(testingPanel);
                        }
                    };
                }
            }
            
            // Set state manager on GameplayPanelHandler so it can clear in-memory player when clearing saved characters
            if (panelHandlerRegistry != null && stateManager != null)
            {
                var gameplayHandler = panelHandlerRegistry.GetHandler("Gameplay");
                if (gameplayHandler is Managers.Settings.PanelHandlers.GameplayPanelHandler gameplayPanelHandler)
                {
                    gameplayPanelHandler.SetStateManager(stateManager);
                }
            }
        }
        
        private void SaveSettings()
        {
            // Pass the panel currently visible so we read from what's on screen (avoids wrong-panel / cache issues)
            UserControl? displayed = ContentScrollViewer.IsVisible ? ContentArea.Content as UserControl
                : TestingContentArea.IsVisible ? TestingContentArea.Content as UserControl
                : ActionsContentArea.Content as UserControl;
            var result = saveOrchestrator?.SaveSettings(displayed) ?? default;
            if (result.Success)
                SettingsApplyService.ApplyAfterSave(result, gameStateManager);
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
