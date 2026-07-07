using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Threading;
using RPGGame;
using RPGGame.Actions;
using RPGGame.Combat.Calculators;
using RPGGame.Data;
using RPGGame.Editors;
using RPGGame.UI.Avalonia.Builders;
using RPGGame.UI.Avalonia.Settings;
using RPGGame.Utils;
using System;
using System.Collections.Generic;
using System.Linq;

namespace RPGGame.UI.Avalonia.Managers
{
    /// <summary>
    /// Manages the Actions tab in SettingsPanel
    /// Extracted from SettingsPanel.axaml.cs to reduce file size
    /// </summary>
    public class ActionsTabManager
    {
        /// <summary>Set to true when an action is created or updated in this session; cleared after refreshing current player's action pool on Settings close.</summary>
        public static bool ActionsSavedThisSession { get; set; }
        private ActionEditor? actionEditor;
        private ActionData? selectedAction;
        private bool isCreatingNewAction = false;
        private Dictionary<string, Control> actionFormControls = new Dictionary<string, Control>();
        private Dictionary<string, ActionData> actionNameToAction = new Dictionary<string, ActionData>();
        private ActionFormBuilder? formBuilder;
        
        private ListBox? actionsListBox;
        private Panel? actionFormPanel;
        private Button? createActionButton;
        private Button? deleteActionButton;
        private ComboBox? rarityFilterComboBox;
        private ComboBox? categoryFilterComboBox;
        private ComboBox? cadenceFilterComboBox;
        private ComboBox? tagFilterComboBox;
        private TextBlock? selectedActionTagsPreview;
        private Action<string, bool>? showStatusMessage;

        /// <summary>Incremental keyboard prefix for jumping in the actions <see cref="ListBox"/> (type-ahead).</summary>
        private string _actionsListTypeAheadBuffer = "";
        private DateTime _actionsListTypeAheadLastUtc;
        private bool _actionsListTypeAheadHandlersAttached;

        public ActionsTabManager()
        {
            actionEditor = new ActionEditor();
        }

        public void Initialize(ListBox actionsListBox, Panel actionFormPanel, Button createActionButton, Button deleteActionButton, Action<string, bool> showStatusMessage, ComboBox? rarityFilterComboBox = null, ComboBox? categoryFilterComboBox = null, ComboBox? cadenceFilterComboBox = null, ComboBox? tagFilterComboBox = null, TextBlock? selectedActionTagsPreview = null)
        {
            ActionLoader.ActionsReloaded += OnGlobalActionsReloaded;
            this.actionsListBox = actionsListBox;
            this.actionFormPanel = actionFormPanel;
            this.createActionButton = createActionButton;
            this.deleteActionButton = deleteActionButton;
            this.rarityFilterComboBox = rarityFilterComboBox;
            this.categoryFilterComboBox = categoryFilterComboBox;
            this.cadenceFilterComboBox = cadenceFilterComboBox;
            this.tagFilterComboBox = tagFilterComboBox;
            this.selectedActionTagsPreview = selectedActionTagsPreview;
            this.showStatusMessage = showStatusMessage;
            
            formBuilder = new ActionFormBuilder(actionFormControls, showStatusMessage);
            formBuilder.CancelActionRequested += OnCancelAction;
            
            // Always sync editor state from ActionLoader (may have changed via spreadsheet pull before this tab opened).
            actionEditor?.ReloadFromDisk();

            // Defer loading actions list until after UI is ready to avoid blocking
            Dispatcher.UIThread.Post(() =>
            {
                LoadActionsList();
            }, DispatcherPriority.Background);
            
            createActionButton.Click += OnCreateActionClick;
            deleteActionButton.Click += OnDeleteActionClick;
            actionsListBox.SelectionChanged += OnActionSelectionChanged;

            if (actionFormPanel.Parent is ScrollViewer scroll
                && scroll.Parent is Grid actionsGrid)
            {
                var openSheet = actionsGrid.FindControl<Button>("OpenActionsSheetButton");
                if (openSheet != null)
                    openSheet.Click += (_, _) => OpenActionsSheetInBrowser();
            }

            if (rarityFilterComboBox != null)
                rarityFilterComboBox.SelectionChanged += (s, e) => ApplyFilter();
            if (categoryFilterComboBox != null)
                categoryFilterComboBox.SelectionChanged += (s, e) => ApplyFilter();
            if (cadenceFilterComboBox != null)
                cadenceFilterComboBox.SelectionChanged += (s, e) => ApplyFilter();
            if (tagFilterComboBox != null)
                tagFilterComboBox.SelectionChanged += (s, e) => ApplyFilter();

            AttachActionsListTypeAheadHandlers(actionsListBox);
        }

        private void AttachActionsListTypeAheadHandlers(ListBox listBox)
        {
            if (_actionsListTypeAheadHandlersAttached)
                return;
            listBox.AddHandler(InputElement.TextInputEvent, OnActionsListTypeAheadTextInput, RoutingStrategies.Bubble);
            listBox.AddHandler(InputElement.KeyDownEvent, OnActionsListTypeAheadKeyDown, RoutingStrategies.Bubble);
            _actionsListTypeAheadHandlersAttached = true;
        }

        private void DetachActionsListTypeAheadHandlers()
        {
            if (!_actionsListTypeAheadHandlersAttached || actionsListBox == null)
                return;
            actionsListBox.RemoveHandler(InputElement.TextInputEvent, OnActionsListTypeAheadTextInput);
            actionsListBox.RemoveHandler(InputElement.KeyDownEvent, OnActionsListTypeAheadKeyDown);
            _actionsListTypeAheadHandlersAttached = false;
        }

        private void OnActionsListTypeAheadTextInput(object? sender, TextInputEventArgs e)
        {
            if (string.IsNullOrEmpty(e.Text))
                return;

            bool advanced = false;
            foreach (var ch in e.Text)
            {
                if (char.IsControl(ch))
                    continue;
                if (TryAppendTypeAheadAndSelect(ch))
                    advanced = true;
            }

            if (advanced)
                e.Handled = true;
        }

        private void OnActionsListTypeAheadKeyDown(object? sender, KeyEventArgs e)
        {
            if (e.Key == Key.Escape && _actionsListTypeAheadBuffer.Length > 0)
            {
                _actionsListTypeAheadBuffer = "";
                e.Handled = true;
                return;
            }

            if (e.Key == Key.Back && _actionsListTypeAheadBuffer.Length > 0 &&
                !e.KeyModifiers.HasFlag(KeyModifiers.Control) && !e.KeyModifiers.HasFlag(KeyModifiers.Alt))
            {
                _actionsListTypeAheadBuffer = _actionsListTypeAheadBuffer[..^1];
                if (string.IsNullOrEmpty(_actionsListTypeAheadBuffer))
                    e.Handled = true;
                else
                {
                    SelectFirstActionMatchingTypeAhead(_actionsListTypeAheadBuffer);
                    e.Handled = true;
                }
                return;
            }

            if (e.Key is Key.Up or Key.Down or Key.Home or Key.End or Key.PageUp or Key.PageDown)
                _actionsListTypeAheadBuffer = "";
        }

        private bool TryAppendTypeAheadAndSelect(char ch)
        {
            if (actionsListBox == null)
                return false;
            var now = DateTime.UtcNow;
            if ((now - _actionsListTypeAheadLastUtc).TotalMilliseconds > 750)
                _actionsListTypeAheadBuffer = "";
            _actionsListTypeAheadLastUtc = now;

            var extended = _actionsListTypeAheadBuffer + ch;
            if (extended.Length > 64)
                extended = extended[^32..];

            int index = IndexOfFirstNameWithPrefix(actionsListBox.Items, extended);
            if (index < 0 && extended.Length > 1)
            {
                extended = ch.ToString();
                index = IndexOfFirstNameWithPrefix(actionsListBox.Items, extended);
            }

            if (index < 0)
                return false;

            _actionsListTypeAheadBuffer = extended;
            var item = actionsListBox.Items[index]!;
            actionsListBox.SelectedItem = item;
            actionsListBox.ScrollIntoView(item);
            return true;
        }

        private void SelectFirstActionMatchingTypeAhead(string prefix)
        {
            if (actionsListBox == null || string.IsNullOrEmpty(prefix))
                return;
            int index = IndexOfFirstNameWithPrefix(actionsListBox.Items, prefix);
            if (index < 0)
                return;
            var item = actionsListBox.Items[index]!;
            actionsListBox.SelectedItem = item;
            actionsListBox.ScrollIntoView(item);
        }

        private static int IndexOfFirstNameWithPrefix(ItemCollection items, string prefix)
        {
            if (string.IsNullOrEmpty(prefix))
                return -1;
            for (int i = 0; i < items.Count; i++)
            {
                if (items[i] is string name && name.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                    return i;
            }
            return -1;
        }

        /// <summary>First index in <paramref name="orderedNames"/> whose value starts with <paramref name="prefix"/> (ordinal ignore case), or -1.</summary>
        internal static int FindFirstActionNamePrefixIndex(IReadOnlyList<string> orderedNames, string prefix)
        {
            if (string.IsNullOrEmpty(prefix))
                return -1;
            for (int i = 0; i < orderedNames.Count; i++)
            {
                var name = orderedNames[i];
                if (!string.IsNullOrEmpty(name) && name.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                    return i;
            }
            return -1;
        }

        /// <summary>Unsubscribe from <see cref="ActionLoader.ActionsReloaded"/> (e.g. when Settings panel unloads).</summary>
        public void DetachFromActionLoaderEvents()
        {
            ActionLoader.ActionsReloaded -= OnGlobalActionsReloaded;
            DetachActionsListTypeAheadHandlers();
        }

        private void OnGlobalActionsReloaded()
        {
            Dispatcher.UIThread.Post(ReloadFromDisk, DispatcherPriority.Normal);
        }

        private static void OpenActionsSheetInBrowser()
        {
            var cfg = SheetsConfig.Load();
            string? url = GoogleSheetsUrlHelper.TryBuildTabEditUrl(cfg, cfg.ActionsSheetUrl);
            if (string.IsNullOrWhiteSpace(url))
                url = GoogleSheetsUrlHelper.TryResolveSpreadsheetEditBaseUrl(cfg);
            BrowserLaunchHelper.TryOpenUrl(url);
        }

        /// <summary>Syncs the tab's <see cref="ActionEditor"/> and list/form from <see cref="ActionLoader"/> after disk changes.</summary>
        public void ReloadFromDisk()
        {
            if (actionEditor == null) return;

            string? keepName = actionsListBox?.SelectedItem as string;
            actionEditor.ReloadFromDisk();

            if (actionsListBox == null)
                return;

            LoadActionsList();
            if (string.IsNullOrEmpty(keepName)) return;
            if (actionsListBox.ItemsSource is System.Collections.IEnumerable enumerable)
            {
                foreach (var item in enumerable)
                {
                    if (item is string s && string.Equals(s, keepName, StringComparison.OrdinalIgnoreCase))
                    {
                        actionsListBox.SelectedItem = item;
                        return;
                    }
                }
            }
            actionsListBox.SelectedItem = null;
            actionFormPanel?.Children.Clear();
            selectedAction = null;
        }

        private void LoadActionsList()
        {
            if (actionEditor == null || actionsListBox == null) return;
            try
            {
                var actions = actionEditor.GetActions();
                if (actions == null || actions.Count == 0)
                {
                    showStatusMessage?.Invoke("No actions found. Check Actions.json file.", false);
                    actionsListBox.ItemsSource = new List<string>();
                    PopulateFilterOptions(Array.Empty<ActionData>());
                    return;
                }
                
                actionNameToAction = actions.Where(a => !string.IsNullOrEmpty(a.Name))
                    .ToDictionary(a => a.Name, a => a);
                
                PopulateFilterOptions(actions);
                ApplyFilter();
                showStatusMessage?.Invoke($"Loaded {actionNameToAction.Count} actions", true);
            }
            catch (Exception ex)
            {
                showStatusMessage?.Invoke($"Error loading actions: {ex.Message}", false);
                actionsListBox.ItemsSource = new List<string>();
            }
        }

        private const string FilterAll = "(All)";

        private void PopulateFilterOptions(IEnumerable<ActionData> actions)
        {
            var rarities = actions.Select(a => a.Rarity).Where(s => !string.IsNullOrWhiteSpace(s)).Distinct().OrderBy(s => s).ToList();
            var categories = actions.Select(a => a.Category).Where(s => !string.IsNullOrWhiteSpace(s)).Distinct().OrderBy(s => s, StringComparer.OrdinalIgnoreCase).ToList();
            var cadences = actions.Select(a => ActionFormBuilder.NormalizeCadence(a.Cadence)).Where(s => !string.IsNullOrWhiteSpace(s)).Distinct().OrderBy(s => s).ToList();
            var tags = actions
                .Where(a => a.Tags != null)
                .SelectMany(a => a.Tags)
                .Where(s => !string.IsNullOrWhiteSpace(s))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderBy(s => s, StringComparer.OrdinalIgnoreCase)
                .ToList();
            var allRarity = new List<string> { FilterAll };
            allRarity.AddRange(rarities);
            var allCategory = new List<string> { FilterAll, ActionFormOptions.GeneralOption };
            allCategory.AddRange(categories);
            var allCadence = new List<string> { FilterAll };
            allCadence.AddRange(cadences);
            var allTags = new List<string> { FilterAll };
            allTags.AddRange(tags);
            if (rarityFilterComboBox != null) rarityFilterComboBox.ItemsSource = allRarity;
            if (categoryFilterComboBox != null) categoryFilterComboBox.ItemsSource = allCategory;
            if (cadenceFilterComboBox != null) cadenceFilterComboBox.ItemsSource = allCadence;
            if (tagFilterComboBox != null) tagFilterComboBox.ItemsSource = allTags;
        }

        private void ApplyFilter()
        {
            if (actionsListBox == null || actionNameToAction == null) return;
            string? selectedRarity = rarityFilterComboBox?.SelectedItem as string;
            string? selectedCategory = categoryFilterComboBox?.SelectedItem as string;
            string? selectedCadence = cadenceFilterComboBox?.SelectedItem as string;
            string? selectedTag = tagFilterComboBox?.SelectedItem as string;
            bool filterByRarity = !string.IsNullOrEmpty(selectedRarity) && selectedRarity != FilterAll;
            bool filterByCategory = !string.IsNullOrEmpty(selectedCategory) && selectedCategory != FilterAll;
            bool filterByCadence = !string.IsNullOrEmpty(selectedCadence) && selectedCadence != FilterAll;
            bool filterByTag = !string.IsNullOrEmpty(selectedTag) && selectedTag != FilterAll;
            _actionsListTypeAheadBuffer = "";
            var actions = actionEditor?.GetActions() ?? new List<ActionData>();
            var filtered = actions
                .Where(a => !string.IsNullOrEmpty(a.Name))
                .Where(a => (!filterByRarity) || string.Equals(a.Rarity, selectedRarity, StringComparison.OrdinalIgnoreCase))
                .Where(a => (!filterByCategory) || (selectedCategory == ActionFormOptions.GeneralOption ? string.IsNullOrWhiteSpace(a.Category) : string.Equals(a.Category, selectedCategory, StringComparison.OrdinalIgnoreCase)))
                .Where(a => (!filterByCadence) || string.Equals(ActionFormBuilder.NormalizeCadence(a.Cadence), selectedCadence, StringComparison.OrdinalIgnoreCase))
                .Where(a => (!filterByTag) || (a.Tags != null && a.Tags.Any(t => string.Equals(t, selectedTag, StringComparison.OrdinalIgnoreCase))))
                .Select(a => a.Name)
                .OrderBy(n => n)
                .ToList();
            actionsListBox.ItemsSource = filtered;
            UpdateTagsPreviewForCurrentSelection();
        }

        private void UpdateTagsPreviewForCurrentSelection()
        {
            if (actionsListBox?.SelectedItem is string actionName &&
                actionNameToAction.TryGetValue(actionName, out var action))
            {
                UpdateTagsPreview(action);
                return;
            }

            UpdateTagsPreview(null);
        }

        private void UpdateTagsPreview(ActionData? action)
        {
            if (selectedActionTagsPreview == null)
                return;
            if (action == null)
            {
                selectedActionTagsPreview.Text = "Select an action to see runtime tags.";
                return;
            }

            string tagsLine = action.Tags != null && action.Tags.Count > 0
                ? string.Join(", ", action.Tags)
                : "(none)";
            selectedActionTagsPreview.Text =
                "Runtime tags: " + tagsLine +
                "\nIncludes category and rarity when set (same list as the Tags field on the right).";
        }

        private void OnActionSelectionChanged(object? sender, SelectionChangedEventArgs e)
        {
            if (actionsListBox?.SelectedItem is string actionName && 
                actionNameToAction.TryGetValue(actionName, out var action))
            {
                selectedAction = action;
                isCreatingNewAction = false;
                UpdateTagsPreview(action);
                LoadActionForm(action);
            }
            else
            {
                UpdateTagsPreview(null);
            }
        }

        private void OnCreateActionClick(object? sender, RoutedEventArgs e)
        {
            selectedAction = new ActionData
            {
                Name = "",
                Type = "Attack",
                TargetType = "SingleTarget",
                Cooldown = 0,
                Description = "",
                DamageMultiplier = 1.0,
                Length = 1.0,
                Tags = new List<string>()
            };
            isCreatingNewAction = true;
            if (actionsListBox != null) actionsListBox.SelectedItem = null;
            UpdateTagsPreview(selectedAction);
            LoadActionForm(selectedAction);
        }

        private void OnDeleteActionClick(object? sender, RoutedEventArgs e)
        {
            if (actionEditor == null || selectedAction == null || isCreatingNewAction)
            {
                showStatusMessage?.Invoke("No action selected for deletion", false);
                return;
            }
            
            if (actionEditor.DeleteAction(selectedAction.Name))
            {
                showStatusMessage?.Invoke($"Action '{selectedAction.Name}' deleted successfully", true);
                LoadActionsList();
                actionFormPanel?.Children.Clear();
                selectedAction = null;
                UpdateTagsPreview(null);
            }
            else
            {
                showStatusMessage?.Invoke($"Failed to delete action '{selectedAction.Name}'", false);
            }
        }

        private void LoadActionForm(ActionData action)
        {
            if (actionFormPanel == null || formBuilder == null) return;
            formBuilder.BuildForm(actionFormPanel, action, isCreatingNewAction);
        }

        private void OnCancelAction()
        {
            if (actionFormPanel != null) actionFormPanel.Children.Clear();
            if (actionsListBox != null) actionsListBox.SelectedItem = null;
            selectedAction = null;
            UpdateTagsPreview(null);
        }

        /// <summary>Reads current values from form controls into the action so unsaved edits (e.g. last TextBox without LostFocus) are persisted.</summary>
        private void FlushFormToAction(ActionData action)
        {
            string? GetText(string label)
            {
                if (!actionFormControls.TryGetValue(label, out var c)) return null;
                if (c is TextBox tb) return tb.Text ?? "";
                if (c is ComboBox cb && cb.SelectedItem is string s) return s;
                return null;
            }
            bool? GetBool(string label)
            {
                if (!actionFormControls.TryGetValue(label, out var c) || c is not CheckBox cb) return null;
                return cb.IsChecked == true;
            }
            string? v;
            // Name and Description are synced to the action via TextChanged (like Accuracy), so we don't read from form here and overwrite with a reverted value when the user saves via the global Save button (focus may move before LostFocus fires).
            if ((v = GetText("Rarity")) != null) action.Rarity = (v == "(None)" || string.IsNullOrWhiteSpace(v)) ? "" : v;
            if ((v = GetText("Category")) != null) action.Category = (v == ActionFormOptions.GeneralOption || string.IsNullOrWhiteSpace(v)) ? "" : v;
            if ((v = GetText("Tags")) != null) action.Tags = string.IsNullOrWhiteSpace(v) ? new List<string>() : v.Split(',', StringSplitOptions.RemoveEmptyEntries).Select(t => t.Trim()).Where(t => !string.IsNullOrWhiteSpace(t)).ToList();
            if (actionFormControls.TryGetValue("Required Weapon Basic", out var requiredControl) && requiredControl is CheckBox requiredCheckBox)
            {
                action.Tags ??= new List<string>();
                action.Tags.RemoveAll(t => string.Equals(t, WeaponRequiredComboAction.WeaponBasicTag, StringComparison.OrdinalIgnoreCase));
                if (requiredCheckBox.IsChecked == true)
                    action.Tags.Add(WeaponRequiredComboAction.WeaponBasicTag);
            }
            if ((v = GetText("Target Type")) != null) action.TargetType = string.IsNullOrWhiteSpace(v) ? "SingleTarget" : v;
            if ((v = GetText("MultiHitCount")) != null && int.TryParse(v, out int i1) && i1 >= 1) action.MultiHitCount = i1;
            if ((v = GetText("DamageMultiplier")) != null && double.TryParse(v, out double d1)) action.DamageMultiplier = d1;
            if ((v = GetText("Speed")) != null && double.TryParse(v, out double d2)) action.Length = d2;
            if ((v = GetText("Chain Position")) != null) action.ChainPosition = v ?? "";
            if ((v = GetText("Chain Position Number")) != null && int.TryParse(v, out int i4) && i4 >= -1) action.ComboOrder = i4;
            if (GetBool("Chain Position MOD") is bool b1) action.ModifyBasedOnChainPosition = b1 ? "true" : "";
            if (GetBool("Skip Next Turn") is bool b2) action.SkipNextTurn = b2;
            if (GetBool("Repeat") is bool b3) action.RepeatLastAction = b3;
            if ((v = GetText("Jump")) != null) action.Jump = v ?? "";
            if ((v = GetText("Jump (+slots)")) != null) action.JumpRelative = v ?? "";
            if ((v = GetText("Size (Chain Length)")) != null) action.ChainLength = v ?? "";
            if (GetBool("Reset") is bool b4) action.Reset = b4 ? "true" : "";
            if (GetBool("Opener") is bool b5) action.IsOpener = b5;
            if (GetBool("Finisher") is bool b6) action.IsFinisher = b6;
            action.TriggerConditions ??= new List<string>();
            if (GetBool("On Hit") == true && !action.TriggerConditions.Any(c => string.Equals(c, "ONHIT", StringComparison.OrdinalIgnoreCase))) action.TriggerConditions.Add("ONHIT");
            if (GetBool("On Hit") == false) action.TriggerConditions.RemoveAll(c => string.Equals(c, "ONHIT", StringComparison.OrdinalIgnoreCase));
            if (GetBool("On Miss") == true && !action.TriggerConditions.Any(c => string.Equals(c, "ONMISS", StringComparison.OrdinalIgnoreCase))) action.TriggerConditions.Add("ONMISS");
            if (GetBool("On Miss") == false) action.TriggerConditions.RemoveAll(c => string.Equals(c, "ONMISS", StringComparison.OrdinalIgnoreCase));
            if (GetBool("On Combo") == true && !action.TriggerConditions.Any(c => string.Equals(c, "ONCOMBO", StringComparison.OrdinalIgnoreCase))) action.TriggerConditions.Add("ONCOMBO");
            if (GetBool("On Combo") == false) action.TriggerConditions.RemoveAll(c => string.Equals(c, "ONCOMBO", StringComparison.OrdinalIgnoreCase));
            if (GetBool("On Crit") == true && !action.TriggerConditions.Any(c => string.Equals(c, "ONCRITICAL", StringComparison.OrdinalIgnoreCase))) action.TriggerConditions.Add("ONCRITICAL");
            if (GetBool("On Crit") == false) action.TriggerConditions.RemoveAll(c => string.Equals(c, "ONCRITICAL", StringComparison.OrdinalIgnoreCase));
            if (GetBool("CausesStun") is bool s1) action.CausesStun = s1;
            if (GetBool("CausesPoison") is bool s2) action.CausesPoison = s2;
            if (GetBool("CausesBurn") is bool s3) action.CausesBurn = s3;
            if (GetBool("CausesBleed") is bool s4) action.CausesBleed = s4;
            if (GetBool("CausesExpose") is bool s6) action.CausesExpose = s6;
            if (GetBool("CausesSilence") is bool s10) action.CausesSilence = s10;

            if (formBuilder?.LastCadenceBlocks != null)
                ActionCadenceEditorSync.ApplyBlocks(action, formBuilder.LastCadenceBlocks);
            // Flush weapon-type checkboxes so save (from any tab) persists "Assign to Weapon Types"
            // Only update from form when the form has weapon-type controls (otherwise we'd clear existing data)
            var weaponTypes = new[] { "Sword", "Dagger", "Mace", "Wand" };
            bool hasWeaponTypeControls = weaponTypes.Any(wt => actionFormControls.ContainsKey($"WeaponType_{wt}"));
            if (hasWeaponTypeControls)
            {
                action.WeaponTypes ??= new List<string>();
                action.WeaponTypes.Clear();
                bool anyWeaponTypeChecked = false;
                foreach (var wt in weaponTypes)
                {
                    if (actionFormControls.TryGetValue($"WeaponType_{wt}", out var c) && c is CheckBox cb && cb.IsChecked == true)
                    {
                        action.WeaponTypes.Add(wt);
                        anyWeaponTypeChecked = true;
                    }
                }
                // Sync "weapon" tag with Assign to Weapon Types so unchecking all removes action from weapon tag filter
                action.Tags ??= new List<string>();
                const string weaponTag = "weapon";
                if (anyWeaponTypeChecked)
                {
                    if (!action.Tags.Any(t => string.Equals(t, weaponTag, StringComparison.OrdinalIgnoreCase)))
                        action.Tags.Add(weaponTag);
                }
                else
                {
                    action.Tags.RemoveAll(t => string.Equals(t, weaponTag, StringComparison.OrdinalIgnoreCase));
                }
            }
        }

        /// <summary>
        /// Flushes the current action form into the selected action (if any) and persists all actions to file.
        /// Handles both new actions (create on global Save) and edits. When fromPanel is provided, reads Default/Starting from that panel.
        /// </summary>
        public void FlushCurrentActionAndSaveToFile(ActionsSettingsPanel? fromPanel = null)
        {
            if (actionEditor == null) return;
            var action = selectedAction;
            var target = action != null
                ? actionEditor.GetActions().FirstOrDefault(a => string.Equals(a.Name, action.Name, StringComparison.OrdinalIgnoreCase)) ?? action
                : null;
            if (target != null)
            {
                FlushFormToAction(target);
                bool defaultStarting = false;
                if (fromPanel != null)
                {
                    var cb = fromPanel.FindControl<CheckBox>("DefaultStartingCheckBox");
                    defaultStarting = cb?.IsChecked == true;
                    if (!defaultStarting && actionFormControls.TryGetValue("Default/Starting", out var fallback) && fallback is CheckBox fc)
                        defaultStarting = fc.IsChecked == true;
                }
                else if (actionFormControls.TryGetValue("Default/Starting", out var defaultControl) && defaultControl is CheckBox defaultCheckBox)
                    defaultStarting = defaultCheckBox.IsChecked == true;
                target.IsDefaultAction = defaultStarting;
                target.IsStartingAction = defaultStarting;
            }

            // New action: add to editor and save (global Save is the only save; no per-action button).
            if (isCreatingNewAction && target != null)
            {
                string? errorMessage = actionEditor!.ValidateAction(target, null);
                if (errorMessage != null)
                {
                    showStatusMessage?.Invoke(errorMessage, false);
                    return;
                }
                if (actionEditor!.CreateAction(target))
                {
                    ActionsSavedThisSession = true;
                    isCreatingNewAction = false;
                    var selectedName = target.Name;
                    LoadActionsList();
                    if (!string.IsNullOrEmpty(selectedName) && actionsListBox?.ItemsSource is System.Collections.IEnumerable names)
                    {
                        foreach (var n in names)
                            if (n is string s && string.Equals(s, selectedName, StringComparison.OrdinalIgnoreCase))
                            { actionsListBox.SelectedItem = n; break; }
                    }
                    ShowActionsSavedPathMessage($"Action '{target.Name}' created");
                }
                else
                    showStatusMessage?.Invoke($"Failed to create action '{target.Name}' (e.g. duplicate name).", false);
                return;
            }

            // Existing action or no selection: save all actions to file (global Save applies to all settings including Actions).
            if (actionEditor!.SaveActionsToFile())
            {
                ActionsSavedThisSession = true;
                var selectedName = action?.Name;
                LoadActionsList();
                if (!string.IsNullOrEmpty(selectedName) && actionsListBox?.ItemsSource is System.Collections.IEnumerable names)
                {
                    foreach (var n in names)
                        if (n is string s && string.Equals(s, selectedName, StringComparison.OrdinalIgnoreCase))
                        { actionsListBox.SelectedItem = n; break; }
                }
                ShowActionsSavedPathMessage("Actions saved");
            }
        }

        private void ShowActionsSavedPathMessage(string prefix)
        {
            var path = actionEditor?.LastSavedActionsFilePath;
            var message = string.IsNullOrWhiteSpace(path)
                ? $"{prefix}. Saved to Actions.json."
                : $"{prefix}. Saved to {path}";
            showStatusMessage?.Invoke(message, true);
        }

        /// <summary>
        /// Refreshes the current player's action pool after actions were saved in Settings (reload from ActionLoader, re-add default/weapon/armor/class actions).
        /// Call when closing Settings if ActionsSavedThisSession is true.
        /// </summary>
        public static void RefreshCurrentPlayerActionPool(Character? player)
        {
            if (player == null) return;
            try
            {
                player.ActionPool.Clear();
                player.Actions.AddDefaultActions(player);
                if (player.Equipment.Head != null)
                    player.Actions.AddArmorActions(player, player.Equipment.Head);
                if (player.Equipment.Body != null)
                    player.Actions.AddArmorActions(player, player.Equipment.Body);
                if (player.Equipment.Legs != null)
                    player.Actions.AddArmorActions(player, player.Equipment.Legs);
                if (player.Equipment.Weapon != null)
                    player.Actions.AddWeaponActions(player, player.Equipment.Weapon);
                if (player.Equipment.Feet != null)
                    player.Actions.AddArmorActions(player, player.Equipment.Feet);
                WeaponType? weaponType = GearActionNames.TryResolveWeaponType(player.Equipment.Weapon, out var resolvedWeaponType)
                    ? resolvedWeaponType
                    : null;
                player.Actions.AddClassActions(player, player.Progression, weaponType);
                player.Actions.InitializeDefaultCombo(player, player.Equipment.Weapon);
                player.ComboStep = 0;
                DamageCalculator.InvalidateCache(player);
            }
            catch (Exception)
            {
                // Best effort; don't crash Settings close
            }
        }
    }
}

