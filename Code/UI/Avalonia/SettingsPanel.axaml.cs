using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Threading;
using RPGGame;
using RPGGame.UI.Avalonia.Helpers;
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
        private ItemSuffixesTabManager? itemSuffixesTabManager;
        private ItemsTabManager? itemsTabManager;
        private EnemiesTabManager? enemiesTabManager;
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
            PopulateSidebar();
            InitializeManagers();
            SetupNavigation();
            ContentScrollViewer.SizeChanged += (_, _) => ConstrainMainScrollContentWidth();
            
            // Apply colors after panel is loaded
            this.Loaded += (s, e) =>
            {
                Dispatcher.UIThread.Post(() =>
                {
                    colorManager?.ApplyColors();
                    ConstrainMainScrollContentWidth();
                }, DispatcherPriority.Loaded);
            };
            this.Unloaded += (_, _) => actionsTabManager?.DetachFromActionLoaderEvents();
        }

        /// <summary>
        /// ScrollViewer gives children unbounded horizontal measure; star-sized columns push
        /// trailing controls (slider text boxes, spawn % fields) off the right edge.
        /// </summary>
        private void ConstrainMainScrollContentWidth()
        {
            double viewportWidth = ContentScrollViewer.Viewport.Width;
            if (viewportWidth <= 0)
                return;

            if (ContentArea.Content is Control mainPanel)
            {
                mainPanel.MaxWidth = viewportWidth;
                mainPanel.HorizontalAlignment = HorizontalAlignment.Stretch;
            }
        }
        
        private void InitializeManagers()
        {
            settingsManager = new SettingsManager(settings, ShowStatusMessage, updateStatus);
            colorManager = new Managers.SettingsColorManager(this);
            gameVariablesTabManager = new GameVariablesTabManager();
            actionsTabManager = new ActionsTabManager();
            itemModifiersTabManager = new Managers.ItemModifiersTabManager(ShowStatusMessage);
            itemSuffixesTabManager = new Managers.ItemSuffixesTabManager(ShowStatusMessage);
            itemsTabManager = new Managers.ItemsTabManager(ShowStatusMessage);
            enemiesTabManager = new EnemiesTabManager(ShowStatusMessage);
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
            panelHandlerRegistry.Register(new PatchesPanelHandler(ShowStatusMessage));
            panelHandlerRegistry.Register(new TravelPanelHandler());
            panelHandlerRegistry.Register(new AudioPanelHandler());
            var textDelaysHandler = new TextDelaysPanelHandler(settingsManager);
            var textAnimationHandler = new TextAnimationPresetsPanelHandler(ShowStatusMessage);
            panelHandlerRegistry.Register(new TextAndAnimationPanelHandler(textDelaysHandler, textAnimationHandler));
            panelHandlerRegistry.Register(new AppearancePanelHandler(settings, colorManager));
            panelHandlerRegistry.Register(new Managers.Settings.PanelHandlers.ClassesPanelHandler(ShowStatusMessage));
            panelHandlerRegistry.Register(new ItemGenerationPanelHandler(ShowStatusMessage));
            var combatTuningHandler = new Managers.Settings.PanelHandlers.CombatTuningPanelHandler(ShowStatusMessage);
            var enemyTuningHandler = new EnemyTuningPanelHandler(ShowStatusMessage);
            panelHandlerRegistry.Register(new CombatAndEnemyTuningPanelHandler(combatTuningHandler, enemyTuningHandler));
            // Testing handler will be registered when canvasUI is available
            
            // Initialize save orchestrator (single panel resolution via GetPanelForCategory)
            saveOrchestrator = new SettingsSaveOrchestrator(
                settingsManager,
                panelHandlerRegistry,
                gameVariablesTabManager,
                actionsTabManager,
                itemModifiersTabManager,
                itemSuffixesTabManager,
                itemsTabManager,
                enemiesTabManager,
                ShowStatusMessage,
                GetPanelForCategory);
            
            // Table-driven initializers: add a new tab by adding one entry here (and to SettingsPanelCatalog if main content)
            initializerContext = new PanelInitializerContext
            {
                Initialization = initialization,
                StatusEffectsTabManager = statusEffectsTabManager,
                ItemModifiersTabManager = itemModifiersTabManager,
                ItemSuffixesTabManager = itemSuffixesTabManager,
                ItemsTabManager = itemsTabManager,
                EnemiesTabManager = enemiesTabManager,
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
                ["ItemAffixes"] = (panel, ctx) =>
                {
                    if (panel is ItemAffixesSettingsPanel affixesPanel && ctx.ItemModifiersTabManager != null && ctx.ItemSuffixesTabManager != null)
                    {
                        Dispatcher.UIThread.Post(() =>
                        {
                            ctx.ItemModifiersTabManager.Initialize(affixesPanel.PrefixesPanel);
                            ctx.ItemSuffixesTabManager.Initialize(affixesPanel.SuffixesPanel);
                        }, DispatcherPriority.Loaded);
                    }
                },
                ["Items"] = (panel, ctx) =>
                {
                    if (panel is ItemsSettingsPanel itemsPanel && ctx.ItemsTabManager != null)
                        Dispatcher.UIThread.Post(() => ctx.ItemsTabManager.Initialize(itemsPanel), DispatcherPriority.Loaded);
                },
                ["Enemies"] = (panel, ctx) =>
                {
                    if (panel is EnemiesSettingsPanel enemiesPanel && ctx.EnemiesTabManager != null)
                        Dispatcher.UIThread.Post(() => ctx.EnemiesTabManager.Initialize(enemiesPanel), DispatcherPriority.Loaded);
                },
                ["Testing"] = (panel, ctx) =>
                {
                    ctx.RegisterTestingHandlerAndWireUp?.Invoke(panel);
                }
            };
        }

        /// <summary>Refreshes panel reference from GameSettings.Instance and pushes file state to all loaded panels. Call after ReloadFromFile() when opening settings, or after Google Sheets PULL (Balance Tuning).</summary>
        public void RefreshSettingsFromFile()
        {
            settings = GameSettings.Instance;
            // Tuning (class presentation, combat, etc.) must match disk when opening settings — same contract as GameSettings.ReloadFromFile().
            GameConfiguration.Instance.Reload();
            // Actions tab uses ActionEditor + ActionLoader; mirror disk after external reloads (e.g. spreadsheet resync).
            actionsTabManager?.ReloadFromDisk();
            gameVariablesTabManager?.RefreshFromConfiguration();
            enemiesTabManager?.RefreshFromFileIfLoaded();
            itemsTabManager?.RefreshFromFileIfLoaded();
            itemModifiersTabManager?.RefreshFromFileIfLoaded();
            itemSuffixesTabManager?.RefreshFromFileIfLoaded();
            // Ensure the selected category's panel is loaded (e.g. first open or SelectedIndex was set before items existed)
            if (CategoryListBox.SelectedItem is ListBoxItem selectedItem && selectedItem.Tag is string categoryTag
                && !loadedPanels.ContainsKey(categoryTag))
                LoadCategoryPanel(categoryTag);
            // Refresh every loaded panel from Instance (file state) so any tab the user switches to shows file-backed values; tab switch no longer overwrites UI.
            foreach (var kv in loadedPanels)
            {
                var handler = panelHandlerRegistry?.GetHandler(kv.Key);
                if (handler != null)
                    handler.LoadSettings(kv.Value);
            }
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
                : ActionsContentArea.IsVisible ? ActionsContentArea.Content as UserControl
                : TextAnimationContentArea.IsVisible ? TextAnimationContentArea.Content as UserControl
                : ItemGenerationContentArea.IsVisible ? ItemGenerationContentArea.Content as UserControl
                : null;
            if (target == null && loadedPanels.TryGetValue(categoryTag, out var cached))
                target = cached;
            if (target != null)
                handler.LoadSettings(target);
        }
        
        /// <summary>Selects a sidebar category and loads its panel (used for deep links from Balance Workbench).</summary>
        public void NavigateToCategory(string categoryTag)
        {
            for (int i = 0; i < CategoryListBox.ItemCount; i++)
            {
                if (CategoryListBox.Items[i] is ListBoxItem item
                    && item.Tag is string tag
                    && string.Equals(tag, categoryTag, StringComparison.OrdinalIgnoreCase))
                {
                    CategoryListBox.SelectedIndex = i;
                    return;
                }
            }

            LoadCategoryPanel(categoryTag);
        }

        public void OpenCombatTuningProgressionCurve()
        {
            CombatTuningNavigation.RequestOpen(CombatTuningNavigation.CombatTuningSubTab.ProgressionCurve);
            NavigateToCategory("CombatTuning");
        }

        private void SetupNavigation()
        {
            // Handle category selection
            CategoryListBox.SelectionChanged += (s, e) =>
            {
                if (CategoryListBox.SelectedItem is ListBoxItem selectedItem)
                {
                    if (IsSidebarHeader(selectedItem))
                    {
                        int idx = CategoryListBox.SelectedIndex;
                        int next = idx + 1 < CategoryListBox.ItemCount ? idx + 1 : idx - 1;
                        if (next >= 0 && next < CategoryListBox.ItemCount)
                            CategoryListBox.SelectedIndex = next;
                        return;
                    }

                    if (selectedItem.Tag is string categoryTag)
                        LoadCategoryPanel(categoryTag);
                }
            };
            
            // Select first panel (skip group headers)
            if (CategoryListBox.ItemCount > 0)
                CategoryListBox.SelectedIndex = FindFirstSelectableSidebarIndex(CategoryListBox);
            
            // Wire up action buttons
            SaveButton.Click += async (_, _) => await SaveSettingsAsync();
            ResetButton.Click += (s, e) => ResetSettings();
            BackButton.Click += (s, e) => onBack?.Invoke();
        }
        
        private void ScheduleInputColorRefresh()
        {
            Dispatcher.UIThread.Post(() => colorManager?.ApplyColors(), DispatcherPriority.Loaded);
        }

        private void PopulateSidebar()
        {
            CategoryListBox.Items.Clear();
            string? currentGroup = null;

            foreach (var descriptor in SettingsPanelCatalog.GetPanelsForSidebar())
            {
                if (descriptor.SidebarGroup != currentGroup)
                {
                    currentGroup = descriptor.SidebarGroup;
                    var groupDef = Array.Find(SettingsSidebarGroups.OrderedGroups, g => g.Id == currentGroup);
                    if (groupDef != null && !string.IsNullOrEmpty(groupDef.DisplayLabel))
                    {
                        CategoryListBox.Items.Add(new ListBoxItem
                        {
                            Content = groupDef.DisplayLabel,
                            Tag = SettingsSidebarGroups.HeaderTag,
                            IsHitTestVisible = false,
                            Focusable = false,
                            Classes = { "settings-sidebar-group-header" }
                        });
                    }
                }

                CategoryListBox.Items.Add(new ListBoxItem
                {
                    Content = descriptor.DisplayName,
                    Tag = descriptor.Tag,
                    Classes = { "settings-sidebar-panel-item" }
                });
            }
        }

        private static int FindFirstSelectableSidebarIndex(ListBox listBox)
        {
            for (int i = 0; i < listBox.ItemCount; i++)
            {
                if (listBox.Items[i] is ListBoxItem item &&
                    item.Tag is string tag &&
                    tag != SettingsSidebarGroups.HeaderTag)
                    return i;
            }
            return 0;
        }

        private static bool IsSidebarHeader(ListBoxItem? item) =>
            item?.Tag is string tag && tag == SettingsSidebarGroups.HeaderTag;

        private void LoadCategoryPanel(string categoryTag)
        {
            var contentArea = SettingsPanelCatalog.GetContentArea(categoryTag);

            if (loadedPanels.ContainsKey(categoryTag))
            {
                var cached = loadedPanels[categoryTag];
                ShowPanelInContentArea(contentArea, cached);
                panelHandlerRegistry?.GetHandler(categoryTag)?.LoadSettings(cached);
                ScheduleInputColorRefresh();
                return;
            }

            var panel = SettingsPanelCatalog.CreatePanel(categoryTag);
            if (panel == null) return;

            loadedPanels[categoryTag] = panel;
            ShowPanelInContentArea(contentArea, panel);

            Dispatcher.UIThread.Post(() =>
            {
                InitializePanelHandlers(categoryTag, panel);
                ScheduleInputColorRefresh();
            }, contentArea == SettingsContentArea.MainScroll ? DispatcherPriority.Loaded : DispatcherPriority.Background);
        }

        private void ShowPanelInContentArea(SettingsContentArea contentArea, UserControl panel)
        {
            ContentScrollViewer.IsVisible = contentArea == SettingsContentArea.MainScroll;
            ActionsContentArea.IsVisible = contentArea == SettingsContentArea.Actions;
            TestingContentArea.IsVisible = contentArea == SettingsContentArea.Testing;
            TextAnimationContentArea.IsVisible = contentArea == SettingsContentArea.TextAnimation;
            ItemGenerationContentArea.IsVisible = contentArea == SettingsContentArea.ItemGeneration;

            switch (contentArea)
            {
                case SettingsContentArea.Actions:
                    ActionsContentArea.Content = panel;
                    break;
                case SettingsContentArea.Testing:
                    TestingContentArea.Content = panel;
                    break;
                case SettingsContentArea.TextAnimation:
                    TextAnimationContentArea.Content = panel;
                    break;
                case SettingsContentArea.ItemGeneration:
                    ItemGenerationContentArea.Content = panel;
                    break;
                default:
                    ContentArea.Content = panel;
                    ConstrainMainScrollContentWidth();
                    break;
            }
        }
        
        private void InitializePanelHandlers(string categoryTag, UserControl panel)
        {
            // Use registry to get handler for standard panels (Gameplay, TextDelays, Appearance)
            var handler = panelHandlerRegistry?.GetHandler(categoryTag);
            if (handler != null)
            {
                handler.WireUp(panel);
                handler.LoadSettings(panel);
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
        internal static string? GetCategoryTagForPanel(UserControl? panel) =>
            SettingsPanelCatalog.GetTagForPanel(panel);

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
            gameVariablesTabManager?.SetGameStateManager(stateManager);
            actionsTabManager?.SetGameStateManager(stateManager);
            
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

            if (panelHandlerRegistry != null && !panelHandlerRegistry.HasHandler("BalanceTuning"))
                panelHandlerRegistry.Register(new BalanceTuningPanelHandler(canvasUI, () => RefreshSettingsFromFile()));
            
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
        
        private async System.Threading.Tasks.Task SaveSettingsAsync()
        {
            UserControl? displayed = ContentScrollViewer.IsVisible ? ContentArea.Content as UserControl
                : TestingContentArea.IsVisible ? TestingContentArea.Content as UserControl
                : ActionsContentArea.IsVisible ? ActionsContentArea.Content as UserControl
                : TextAnimationContentArea.IsVisible ? TextAnimationContentArea.Content as UserControl
                : ItemGenerationContentArea.IsVisible ? ItemGenerationContentArea.Content as UserControl
                : null;

            await Dispatcher.UIThread.InvokeAsync(() => { }, DispatcherPriority.Loaded);
            var owner = WindowOwnerResolver.ResolveUsableOwnerWindow(TopLevel.GetTopLevel(this) as Window);
            var result = saveOrchestrator != null
                ? await saveOrchestrator.SaveSettingsAsync(displayed, owner)
                : default;
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
