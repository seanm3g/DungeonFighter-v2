using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Threading;
using RPGGame;
using RPGGame.Combat.Calculators;
using RPGGame.Editors;
using RPGGame.UI.Avalonia.Builders;
using RPGGame.UI.Avalonia.Settings;
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
        private Action<string, bool>? showStatusMessage;

        public ActionsTabManager()
        {
            actionEditor = new ActionEditor();
        }

        public void Initialize(ListBox actionsListBox, Panel actionFormPanel, Button createActionButton, Button deleteActionButton, Action<string, bool> showStatusMessage, ComboBox? rarityFilterComboBox = null, ComboBox? categoryFilterComboBox = null, ComboBox? cadenceFilterComboBox = null, ComboBox? tagFilterComboBox = null)
        {
            this.actionsListBox = actionsListBox;
            this.actionFormPanel = actionFormPanel;
            this.createActionButton = createActionButton;
            this.deleteActionButton = deleteActionButton;
            this.rarityFilterComboBox = rarityFilterComboBox;
            this.categoryFilterComboBox = categoryFilterComboBox;
            this.cadenceFilterComboBox = cadenceFilterComboBox;
            this.tagFilterComboBox = tagFilterComboBox;
            this.showStatusMessage = showStatusMessage;
            
            formBuilder = new ActionFormBuilder(actionFormControls, showStatusMessage);
            formBuilder.CancelActionRequested += OnCancelAction;
            
            // Defer loading actions list until after UI is ready to avoid blocking
            Dispatcher.UIThread.Post(() =>
            {
                LoadActionsList();
            }, DispatcherPriority.Background);
            
            createActionButton.Click += OnCreateActionClick;
            deleteActionButton.Click += OnDeleteActionClick;
            actionsListBox.SelectionChanged += OnActionSelectionChanged;

            if (rarityFilterComboBox != null)
                rarityFilterComboBox.SelectionChanged += (s, e) => ApplyFilter();
            if (categoryFilterComboBox != null)
                categoryFilterComboBox.SelectionChanged += (s, e) => ApplyFilter();
            if (cadenceFilterComboBox != null)
                cadenceFilterComboBox.SelectionChanged += (s, e) => ApplyFilter();
            if (tagFilterComboBox != null)
                tagFilterComboBox.SelectionChanged += (s, e) => ApplyFilter();
        }

        private void LoadActionsList()
        {
            if (actionEditor == null || actionsListBox == null) return;
            try
            {
                var actions = actionEditor.GetActions();
                // #region agent log
                try { var path = ActionLoader.GetLoadedActionsFilePath(); System.IO.File.AppendAllText(@"d:\Code Projects\github projects\DungeonFighter-v2\.cursor\debug.log", System.Text.Json.JsonSerializer.Serialize(new { hypothesisId = "H2,H5", location = "ActionsTabManager.LoadActionsList", message = "list source", data = new { loadPath = path ?? "(null)", editorActionCount = actions?.Count ?? -1 }, timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() }) + "\n"); } catch { }
                // #endregion
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
        }

        private void OnActionSelectionChanged(object? sender, SelectionChangedEventArgs e)
        {
            if (actionsListBox?.SelectedItem is string actionName && 
                actionNameToAction.TryGetValue(actionName, out var action))
            {
                selectedAction = action;
                isCreatingNewAction = false;
                LoadActionForm(action);
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
            if ((v = GetText("Target Type")) != null) action.TargetType = string.IsNullOrWhiteSpace(v) ? "SingleTarget" : v;
            if ((v = GetText("MultiHitCount")) != null && int.TryParse(v, out int i1) && i1 >= 1) action.MultiHitCount = i1;
            if ((v = GetText("DamageMultiplier")) != null && double.TryParse(v, out double d1)) action.DamageMultiplier = d1;
            if ((v = GetText("Speed")) != null && double.TryParse(v, out double d2)) action.Length = d2;
            if ((v = GetText("Cooldown")) != null && int.TryParse(v, out int i2) && i2 >= 0) action.Cooldown = i2;
            if ((v = GetText("Cadence")) != null) action.Cadence = (v == "(None)" || string.IsNullOrWhiteSpace(v)) ? "" : v;
            if ((v = GetText("Duration")) != null && int.TryParse(v, out int i3) && i3 >= 0) action.ComboBonusDuration = i3;
            if ((v = GetText("SpeedMod (%)")) != null) action.SpeedMod = v ?? "";
            if ((v = GetText("DamageMod (%)")) != null) action.DamageMod = v ?? "";
            if ((v = GetText("MultiHitMod (value)")) != null) action.MultiHitMod = v ?? "";
            if ((v = GetText("Amp Mod (%)")) != null) action.AmpMod = v ?? "";
            if ((v = GetText("Chain Position")) != null) action.ChainPosition = v ?? "";
            if ((v = GetText("Chain Position Number")) != null && int.TryParse(v, out int i4) && i4 >= -1) action.ComboOrder = i4;
            if (GetBool("Chain Position MOD") is bool b1) action.ModifyBasedOnChainPosition = b1 ? "true" : "";
            if (GetBool("Skip Next Turn") is bool b2) action.SkipNextTurn = b2;
            if (GetBool("Repeat") is bool b3) action.RepeatLastAction = b3;
            if ((v = GetText("Jump")) != null) action.Jump = v ?? "";
            if ((v = GetText("Size (Chain Length)")) != null) action.ChainLength = v ?? "";
            if (GetBool("Reset") is bool b4) action.Reset = b4 ? "true" : "";
            if (GetBool("Opener") is bool b5) action.IsOpener = b5;
            if (GetBool("Finisher") is bool b6) action.IsFinisher = b6;
            if ((v = GetText("Roll bonus: Crit Miss")) != null && int.TryParse(v, out int i5)) action.CriticalMissThresholdAdjustment = i5;
            if ((v = GetText("Roll bonus: Hit")) != null && int.TryParse(v, out int i6)) action.HitThresholdAdjustment = i6;
            if ((v = GetText("Roll bonus: Combo")) != null && int.TryParse(v, out int i7)) action.ComboThresholdAdjustment = i7;
            if ((v = GetText("Roll bonus: Crit")) != null && int.TryParse(v, out int i8)) action.CriticalHitThresholdAdjustment = i8;
            // Accuracy is not read from form here: it is kept in sync via TextChanged on the Accuracy field, so we don't overwrite with a reverted value when the user saves via the global Save button (focus has already moved and the TextBox may have reverted).
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
            if (GetBool("CausesWeaken") is bool s5) action.CausesWeaken = s5;
            if (GetBool("CausesExpose") is bool s6) action.CausesExpose = s6;
            if (GetBool("CausesSlow") is bool s7) action.CausesSlow = s7;
            if (GetBool("CausesVulnerability") is bool s8) action.CausesVulnerability = s8;
            if (GetBool("CausesHarden") is bool s9) action.CausesHarden = s9;
            if (GetBool("CausesSilence") is bool s10) action.CausesSilence = s10;
            if (GetBool("CausesPierce") is bool s11) action.CausesPierce = s11;
            if (GetBool("CausesStatDrain") is bool s12) action.CausesStatDrain = s12;
            if (GetBool("CausesFortify") is bool s13) action.CausesFortify = s13;
            if (GetBool("CausesFocus") is bool s14) action.CausesFocus = s14;
            if (GetBool("CausesCleanse") is bool s15) action.CausesCleanse = s15;
            if (GetBool("CausesReflect") is bool s16) action.CausesReflect = s16;
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
            // #region agent log
            try { System.IO.File.AppendAllText(@"d:\Code Projects\github projects\DungeonFighter-v2\.cursor\debug.log", System.Text.Json.JsonSerializer.Serialize(new { hypothesisId = "H7", location = "ActionsTabManager.FlushCurrentActionAndSaveToFile", message = "flush target", data = new { selectedName = selectedAction?.Name ?? "(null)", targetNotNull = target != null, fromPanelNotNull = fromPanel != null }, timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() }) + "\n"); } catch { }
            // #endregion
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
                    showStatusMessage?.Invoke($"Action '{target.Name}' created. Settings saved.", true);
                    var selectedName = target.Name;
                    LoadActionsList();
                    if (!string.IsNullOrEmpty(selectedName) && actionsListBox?.ItemsSource is System.Collections.IEnumerable names)
                    {
                        foreach (var n in names)
                            if (n is string s && string.Equals(s, selectedName, StringComparison.OrdinalIgnoreCase))
                            { actionsListBox.SelectedItem = n; break; }
                    }
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
            }
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
                if (player.Equipment.Weapon is WeaponItem weapon)
                    player.Actions.AddWeaponActions(player, weapon);
                if (player.Equipment.Feet != null)
                    player.Actions.AddArmorActions(player, player.Equipment.Feet);
                var weaponType = (player.Equipment.Weapon as WeaponItem)?.WeaponType;
                player.Actions.AddClassActions(player, player.Progression, weaponType);
                player.InitializeDefaultCombo();
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

