using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.Media;
using RPGGame;
using RPGGame.UI.Avalonia.Helpers;
using RPGGame.UI.Avalonia.Settings;

namespace RPGGame.UI.Avalonia.Managers.Settings.PanelHandlers
{
    /// <summary>List row for a flavor text form.</summary>
    public sealed class FlavorFormListItem
    {
        public FlavorFormListItem(string id, string displayName)
        {
            Id = id;
            DisplayName = displayName;
        }

        public string Id { get; }
        public string DisplayName { get; }
        public override string ToString() =>
            string.IsNullOrWhiteSpace(DisplayName) || string.Equals(DisplayName, Id, StringComparison.Ordinal)
                ? Id
                : $"{DisplayName} ({Id})";
    }

    /// <summary>
    /// Settings → Developer → Flavor Text: author forms with &lt;category&gt; tags,
    /// edit global category lists, and keep legacy banks editable.
    /// </summary>
    public sealed class FlavorTextPanelHandler : ISettingsPanelHandler
    {
        private static readonly (string HostName, string Title, double Width, double Height)[] DetachableSections =
        {
            ("FlavorTextFormsHost", "Forms", 520, 200),
            ("FlavorTextTemplateHost", "Form template", 640, 360),
            ("FlavorTextCategoriesHost", "Categories", 720, 420),
            ("FlavorTextLegacyHost", "Legacy banks", 760, 400),
        };

        private readonly Action<string, bool>? showStatusMessage;
        private FlavorTextSettingsPanel? _panel;
        private FlavorTextData? _working;
        private string? _selectedFormId;
        private string? _selectedCategoryKey;
        private string? _selectedBankPathId;
        private bool _suppressSelection;
        private bool _dirty;
        private bool _sectionsExpanded;
        private bool _contractingAll;
        private readonly Dictionary<string, FlavorTextSectionWindow> _sectionWindows = new(StringComparer.Ordinal);

        public FlavorTextPanelHandler(Action<string, bool>? showStatusMessage = null)
        {
            this.showStatusMessage = showStatusMessage;
        }

        public string PanelType => "FlavorText";

        public void WireUp(UserControl panel)
        {
            if (panel is not FlavorTextSettingsPanel p) return;

            if (_panel != null)
                _panel.Unloaded -= OnPanelUnloaded;
            _panel = p;
            p.Unloaded += OnPanelUnloaded;

            WireButton(p, "ExpandFlavorTextSectionsButton", OnExpandContractClick);
            WireButton(p, "ReloadFlavorTextButton", OnReloadClick);
            WireButton(p, "SaveFlavorTextButton", OnSaveClick);
            WireButton(p, "GenerateFlavorTextButton", OnGenerateClick);
            WireButton(p, "GenerateFlavorTextx10Button", OnGenerate10Click);
            WireButton(p, "AddFlavorTextFormButton", OnAddFormClick);
            WireButton(p, "AddFlavorTextCategoryButton", OnAddCategoryClick);

            var forms = p.FindControl<ListBox>("FlavorTextFormsListBox");
            if (forms != null)
            {
                forms.SelectionChanged -= OnFormSelectionChanged;
                forms.SelectionChanged += OnFormSelectionChanged;
            }

            var categories = p.FindControl<ListBox>("FlavorTextCategoriesListBox");
            if (categories != null)
            {
                categories.SelectionChanged -= OnCategorySelectionChanged;
                categories.SelectionChanged += OnCategorySelectionChanged;
            }

            var banks = p.FindControl<ListBox>("FlavorTextBanksListBox");
            if (banks != null)
            {
                banks.SelectionChanged -= OnBankSelectionChanged;
                banks.SelectionChanged += OnBankSelectionChanged;
            }

            var template = p.FindControl<TextBox>("FlavorTextFormTemplateTextBox");
            if (template != null)
            {
                template.TextChanged -= OnFormTemplateTextChanged;
                template.TextChanged += OnFormTemplateTextChanged;
                template.LostFocus -= OnFormTemplateLostFocus;
                template.LostFocus += OnFormTemplateLostFocus;
            }

            var catEntries = p.FindControl<TextBox>("FlavorTextCategoryEntriesTextBox");
            if (catEntries != null)
            {
                catEntries.TextChanged -= OnCategoryEntriesTextChanged;
                catEntries.TextChanged += OnCategoryEntriesTextChanged;
            }

            var bankEntries = p.FindControl<TextBox>("FlavorTextEntriesTextBox");
            if (bankEntries != null)
            {
                bankEntries.TextChanged -= OnBankEntriesTextChanged;
                bankEntries.TextChanged += OnBankEntriesTextChanged;
            }

            UpdateExpandButtonLabel(p);
            LoadSettings(panel);
        }

        public void LoadSettings(UserControl panel)
        {
            if (panel is not FlavorTextSettingsPanel p) return;
            _panel = p;

            FlavorText.Reload();
            _working = CloneData(FlavorText.GetData());
            _dirty = false;
            _selectedFormId = null;
            _selectedCategoryKey = null;
            _selectedBankPathId = null;

            PopulateFormsList(p);
            PopulateCategoriesList(p);
            PopulateBanksList(p);
            ClearFormEditor(p);
            ClearCategoryEditor(p);
            ClearBankEditor(p);
            SetStatus(p, $"Loaded: {FlavorText.GetResolvedFilePath() ?? "(not found)"}");

            // Select first form if any.
            var forms = p.FindControl<ListBox>("FlavorTextFormsListBox");
            if (forms?.ItemsSource is IEnumerable<FlavorFormListItem> items && items.Any())
                forms.SelectedIndex = 0;
        }

        public void SaveSettings(UserControl panel)
        {
            if (panel is not FlavorTextSettingsPanel p) return;
            CommitAll(p);
            if (_working == null) return;

            try
            {
                FlavorText.SaveData(_working);
                _dirty = false;
                string path = FlavorText.GetResolvedFilePath() ?? "FlavorText.json";
                SetStatus(p, $"Saved: {path}");
                showStatusMessage?.Invoke("Flavor text saved.", true);
            }
            catch (Exception ex)
            {
                SetStatus(p, $"Save failed: {ex.Message}");
                showStatusMessage?.Invoke($"Flavor text save failed: {ex.Message}", false);
            }
        }

        private static void WireButton(FlavorTextSettingsPanel p, string name, EventHandler<RoutedEventArgs> handler)
        {
            var btn = p.FindControl<Button>(name);
            if (btn == null) return;
            btn.Click -= handler;
            btn.Click += handler;
        }

        private void OnPanelUnloaded(object? sender, RoutedEventArgs e) => ContractSections();

        private void OnExpandContractClick(object? sender, RoutedEventArgs e)
        {
            if (_sectionsExpanded)
                ContractSections();
            else
                ExpandSections();
        }

        private void ExpandSections()
        {
            if (_panel == null || _sectionsExpanded) return;
            CommitAll(_panel);

            Window? owner = TopLevel.GetTopLevel(_panel) as Window;
            PixelPoint fallback = owner?.Position ?? new PixelPoint(80, 80);

            foreach (var (hostName, title, width, height) in DetachableSections)
            {
                if (_sectionWindows.ContainsKey(hostName))
                    continue;

                var host = _panel.FindControl<ContentControl>(hostName);
                if (host?.Content is not Control section)
                    continue;

                host.Content = null;
                host.IsVisible = false;

                var window = FlavorTextSectionWindow.Open(
                    owner,
                    hostName,
                    title,
                    section,
                    fallback,
                    new Size(width, height));
                window.ClosedWithContent += OnSectionWindowClosedWithContent;
                _sectionWindows[hostName] = window;
            }

            ApplyExpandedLayout(_panel, preferGenerateOnly: true);
            _sectionsExpanded = _sectionWindows.Count > 0;
            UpdateExpandButtonLabel(_panel);

            if (owner != null && _sectionWindows.Count > 0)
            {
                var byHost = new Dictionary<string, Window>(StringComparer.Ordinal);
                foreach (var kv in _sectionWindows)
                    byHost[kv.Key] = kv.Value;
                FlavorTextWindowPlacement.ApplyAfterLayout(owner, byHost);
            }
        }

        private void ContractSections()
        {
            if (_panel == null) return;
            if (!_sectionsExpanded && _sectionWindows.Count == 0) return;

            _contractingAll = true;
            try
            {
                foreach (var hostName in DetachableSections.Select(s => s.HostName).ToList())
                {
                    if (!_sectionWindows.TryGetValue(hostName, out var window))
                        continue;

                    _sectionWindows.Remove(hostName);
                    window.ClosedWithContent -= OnSectionWindowClosedWithContent;
                    var section = window.TakeContentWithoutRedock();
                    RedockSection(hostName, section);
                    try { window.Close(); } catch { /* best effort */ }
                }

                ApplyExpandedLayout(_panel, preferGenerateOnly: false);
                _sectionsExpanded = false;
                UpdateExpandButtonLabel(_panel);
            }
            finally
            {
                _contractingAll = false;
            }
        }

        private void OnSectionWindowClosedWithContent(FlavorTextSectionWindow window, Control? section)
        {
            if (_contractingAll) return;

            _sectionWindows.Remove(window.HostName);
            RedockSection(window.HostName, section);

            if (_panel == null) return;

            _sectionsExpanded = _sectionWindows.Count > 0;
            ApplyExpandedLayout(_panel, preferGenerateOnly: _sectionsExpanded);
            UpdateExpandButtonLabel(_panel);
        }

        private void RedockSection(string hostName, Control? section)
        {
            if (_panel == null || section == null) return;
            var host = _panel.FindControl<ContentControl>(hostName);
            if (host == null) return;
            host.Content = section;
            host.IsVisible = true;
        }

        private void ApplyExpandedLayout(FlavorTextSettingsPanel panel, bool preferGenerateOnly)
        {
            if (panel.Content is not Grid root || root.RowDefinitions.Count < 6)
                return;

            // Never Clear() RowDefinitions — children keep Grid.Row 0..5; shrinking the
            // definition list makes Avalonia Grid.MeasureCellsGroup throw ArgumentOutOfRange.
            root.RowDefinitions[0].Height = GridLength.Auto;
            root.RowDefinitions[1].Height = new GridLength(1, GridUnitType.Star);

            bool Collapse(string hostName)
            {
                if (!preferGenerateOnly)
                    return false;
                var host = panel.FindControl<ContentControl>(hostName);
                return host == null || !host.IsVisible || host.Content == null;
            }

            root.RowDefinitions[2].Height = Collapse("FlavorTextFormsHost")
                ? new GridLength(0)
                : GridLength.Auto;
            root.RowDefinitions[3].Height = Collapse("FlavorTextTemplateHost")
                ? new GridLength(0)
                : new GridLength(1, GridUnitType.Star);
            root.RowDefinitions[4].Height = Collapse("FlavorTextCategoriesHost")
                ? new GridLength(0)
                : new GridLength(1, GridUnitType.Star);
            root.RowDefinitions[5].Height = Collapse("FlavorTextLegacyHost")
                ? new GridLength(0)
                : GridLength.Auto;
        }

        private void UpdateExpandButtonLabel(FlavorTextSettingsPanel p)
        {
            var btn = p.FindControl<Button>("ExpandFlavorTextSectionsButton");
            if (btn == null) return;
            btn.Content = _sectionsExpanded ? "CONTRACT" : "EXPAND";
        }

        private void OnReloadClick(object? sender, RoutedEventArgs e)
        {
            if (_panel == null) return;
            LoadSettings(_panel);
            showStatusMessage?.Invoke("Flavor text reloaded from disk.", true);
        }

        private void OnSaveClick(object? sender, RoutedEventArgs e)
        {
            if (_panel == null) return;
            SaveSettings(_panel);
        }

        private void OnGenerateClick(object? sender, RoutedEventArgs e) => RunGenerate(1);

        private void OnGenerate10Click(object? sender, RoutedEventArgs e) => RunGenerate(10);

        private async void OnAddFormClick(object? sender, RoutedEventArgs e)
        {
            if (_panel == null || _working == null) return;
            CommitAll(_panel);

            string? raw = await ShowIdPromptAsync(_panel, "Add Form", "Form id (letters, numbers, underscores):", "newForm");
            if (raw == null) return;
            if (!FlavorTextBankCatalog.TrySanitizeId(raw, out string id, out string? error))
            {
                showStatusMessage?.Invoke(error ?? "Invalid form id.", false);
                return;
            }
            if (_working.Forms.ContainsKey(id))
            {
                showStatusMessage?.Invoke($"Form '{id}' already exists.", false);
                return;
            }

            _working.Forms[id] = new FlavorFormDefinition
            {
                DisplayName = id,
                Template = ""
            };
            _dirty = true;
            _selectedFormId = id;
            PopulateFormsList(_panel);
            SelectFormInList(_panel, id);
            LoadFormEditor(_panel, id);
            SetStatus(_panel, $"Added form '{id}'");
        }

        private async void OnAddCategoryClick(object? sender, RoutedEventArgs e)
        {
            if (_panel == null || _working == null) return;
            CommitAll(_panel);

            string? raw = await ShowIdPromptAsync(_panel, "Add Category", "Category id (letters, numbers, underscores):", "intro");
            if (raw == null) return;
            if (!FlavorTextBankCatalog.TrySanitizeId(raw, out string id, out string? error))
            {
                showStatusMessage?.Invoke(error ?? "Invalid category id.", false);
                return;
            }
            if (_working.Categories.ContainsKey(id))
            {
                showStatusMessage?.Invoke($"Category '{id}' already exists.", false);
                return;
            }

            _working.Categories[id] = Array.Empty<string>();
            _dirty = true;
            _selectedCategoryKey = id;
            PopulateCategoriesList(_panel);
            SelectCategoryInList(_panel, id);
            LoadCategoryEditor(_panel, id);
            SetStatus(_panel, $"Added category '{id}'");
        }

        private void OnFormSelectionChanged(object? sender, SelectionChangedEventArgs e)
        {
            if (_suppressSelection || _panel == null || _working == null) return;
            CommitFormTemplate(_panel);
            CommitCategoryEntries(_panel);

            if (sender is not ListBox list || list.SelectedItem is not FlavorFormListItem item)
            {
                ClearFormEditor(_panel);
                return;
            }

            _selectedFormId = item.Id;
            LoadFormEditor(_panel, item.Id);
            HighlightReferencedCategories(_panel, item.Id);
        }

        private void OnCategorySelectionChanged(object? sender, SelectionChangedEventArgs e)
        {
            if (_suppressSelection || _panel == null || _working == null) return;
            CommitCategoryEntries(_panel);

            if (sender is not ListBox list || list.SelectedItem is not string key)
            {
                ClearCategoryEditor(_panel);
                return;
            }

            _selectedCategoryKey = key;
            LoadCategoryEditor(_panel, key);
        }

        private void OnBankSelectionChanged(object? sender, SelectionChangedEventArgs e)
        {
            if (_suppressSelection || _panel == null || _working == null) return;
            CommitBankEntries(_panel);

            if (sender is not ListBox list || list.SelectedItem is not FlavorTextBankInfo info)
            {
                ClearBankEditor(_panel);
                return;
            }

            _selectedBankPathId = info.PathId;
            var entriesBox = _panel.FindControl<TextBox>("FlavorTextEntriesTextBox");
            var pathLabel = _panel.FindControl<TextBlock>("FlavorTextBankPathLabel");
            if (pathLabel != null)
                pathLabel.Text = $"{info.DisplayLabel}  ({info.PathId})";
            if (entriesBox != null)
                entriesBox.Text = FlavorTextBankCatalog.EntriesToMultiline(FlavorTextBankCatalog.GetBank(_working, info.PathId));
        }

        private void OnFormTemplateTextChanged(object? sender, TextChangedEventArgs e) => _dirty = true;

        private void OnFormTemplateLostFocus(object? sender, RoutedEventArgs e)
        {
            if (_panel == null || _working == null) return;
            CommitFormTemplate(_panel);
            PopulateCategoriesList(_panel);
        }

        private void OnCategoryEntriesTextChanged(object? sender, TextChangedEventArgs e) => _dirty = true;

        private void OnBankEntriesTextChanged(object? sender, TextChangedEventArgs e) => _dirty = true;

        private void RunGenerate(int count)
        {
            if (_panel == null || _working == null) return;
            CommitAll(_panel);

            var output = _panel.FindControl<TextBox>("FlavorTextGenerateOutputTextBox");
            if (output == null) return;

            if (string.IsNullOrEmpty(_selectedFormId) || !_working.Forms.ContainsKey(_selectedFormId))
            {
                output.Text = "(select a form first)";
                return;
            }

            FlavorTextBankCatalog.EnsureCategoriesForTemplate(_working, _working.Forms[_selectedFormId].Template);
            PopulateCategoriesList(_panel);

            try
            {
                output.Text = FlavorTextBankCatalog.GenerateFormMany(_working, _selectedFormId, count);
            }
            catch (Exception ex)
            {
                output.Text = $"Generate failed: {ex.Message}";
            }
        }

        private void CommitAll(FlavorTextSettingsPanel p)
        {
            CommitFormTemplate(p);
            CommitCategoryEntries(p);
            CommitBankEntries(p);
        }

        private void CommitFormTemplate(FlavorTextSettingsPanel p)
        {
            if (_working == null || string.IsNullOrEmpty(_selectedFormId)) return;
            if (!_working.Forms.TryGetValue(_selectedFormId, out var form) || form == null) return;

            var box = p.FindControl<TextBox>("FlavorTextFormTemplateTextBox");
            if (box == null) return;
            form.Template = box.Text ?? "";
            FlavorTextBankCatalog.EnsureCategoriesForTemplate(_working, form.Template);
        }

        private void CommitCategoryEntries(FlavorTextSettingsPanel p)
        {
            if (_working == null || string.IsNullOrEmpty(_selectedCategoryKey)) return;
            var box = p.FindControl<TextBox>("FlavorTextCategoryEntriesTextBox");
            if (box == null) return;
            var entries = FlavorTextBankCatalog.MultilineToEntries(box.Text ?? "");
            FlavorTextBankCatalog.SetCategory(_working, _selectedCategoryKey, entries);
        }

        private void CommitBankEntries(FlavorTextSettingsPanel p)
        {
            if (_working == null || string.IsNullOrEmpty(_selectedBankPathId)) return;
            var box = p.FindControl<TextBox>("FlavorTextEntriesTextBox");
            if (box == null) return;
            var entries = FlavorTextBankCatalog.MultilineToEntries(box.Text ?? "");
            FlavorTextBankCatalog.SetBank(_working, _selectedBankPathId, entries);
        }

        private void PopulateFormsList(FlavorTextSettingsPanel p)
        {
            var list = p.FindControl<ListBox>("FlavorTextFormsListBox");
            if (list == null || _working == null) return;

            _suppressSelection = true;
            try
            {
                var items = _working.Forms
                    .OrderBy(k => k.Key, StringComparer.OrdinalIgnoreCase)
                    .Select(kvp => new FlavorFormListItem(
                        kvp.Key,
                        string.IsNullOrWhiteSpace(kvp.Value?.DisplayName) ? kvp.Key : kvp.Value!.DisplayName))
                    .ToList();
                list.ItemsSource = items;
                if (!string.IsNullOrEmpty(_selectedFormId))
                {
                    var match = items.FirstOrDefault(i =>
                        string.Equals(i.Id, _selectedFormId, StringComparison.OrdinalIgnoreCase));
                    list.SelectedItem = match;
                }
                else
                {
                    list.SelectedIndex = -1;
                }
            }
            finally
            {
                _suppressSelection = false;
            }
        }

        private void PopulateCategoriesList(FlavorTextSettingsPanel p)
        {
            var list = p.FindControl<ListBox>("FlavorTextCategoriesListBox");
            if (list == null || _working == null) return;

            _suppressSelection = true;
            try
            {
                var keys = _working.Categories.Keys
                    .OrderBy(k => k, StringComparer.OrdinalIgnoreCase)
                    .ToList();
                list.ItemsSource = keys;
                if (!string.IsNullOrEmpty(_selectedCategoryKey)
                    && keys.Contains(_selectedCategoryKey, StringComparer.OrdinalIgnoreCase))
                {
                    list.SelectedItem = keys.First(k =>
                        string.Equals(k, _selectedCategoryKey, StringComparison.OrdinalIgnoreCase));
                }
                else
                {
                    list.SelectedIndex = -1;
                    if (string.IsNullOrEmpty(_selectedCategoryKey) || !keys.Contains(_selectedCategoryKey, StringComparer.OrdinalIgnoreCase))
                        ClearCategoryEditor(p);
                }
            }
            finally
            {
                _suppressSelection = false;
            }
        }

        private void PopulateBanksList(FlavorTextSettingsPanel p)
        {
            var list = p.FindControl<ListBox>("FlavorTextBanksListBox");
            if (list == null || _working == null) return;

            _suppressSelection = true;
            try
            {
                var banks = FlavorTextBankCatalog.EnumerateBanks(_working).ToList();
                list.ItemsSource = banks;
                if (!string.IsNullOrEmpty(_selectedBankPathId))
                {
                    var match = banks.FirstOrDefault(b => b.PathId == _selectedBankPathId);
                    list.SelectedItem = match;
                }
                else
                {
                    list.SelectedIndex = -1;
                }
            }
            finally
            {
                _suppressSelection = false;
            }
        }

        private void SelectFormInList(FlavorTextSettingsPanel p, string id)
        {
            var list = p.FindControl<ListBox>("FlavorTextFormsListBox");
            if (list?.ItemsSource is not IEnumerable<FlavorFormListItem> items) return;
            _suppressSelection = true;
            try
            {
                list.SelectedItem = items.FirstOrDefault(i =>
                    string.Equals(i.Id, id, StringComparison.OrdinalIgnoreCase));
            }
            finally
            {
                _suppressSelection = false;
            }
        }

        private void SelectCategoryInList(FlavorTextSettingsPanel p, string key)
        {
            var list = p.FindControl<ListBox>("FlavorTextCategoriesListBox");
            if (list?.ItemsSource is not IEnumerable<string> keys) return;
            _suppressSelection = true;
            try
            {
                list.SelectedItem = keys.FirstOrDefault(k =>
                    string.Equals(k, key, StringComparison.OrdinalIgnoreCase));
            }
            finally
            {
                _suppressSelection = false;
            }
        }

        private void LoadFormEditor(FlavorTextSettingsPanel p, string formId)
        {
            if (_working == null || !_working.Forms.TryGetValue(formId, out var form) || form == null)
            {
                ClearFormEditor(p);
                return;
            }

            var label = p.FindControl<TextBlock>("FlavorTextFormTemplateLabel");
            var box = p.FindControl<TextBox>("FlavorTextFormTemplateTextBox");
            if (label != null)
                label.Text = $"Form template — {form.DisplayName} ({formId})";
            if (box != null)
                box.Text = form.Template ?? "";
        }

        private void LoadCategoryEditor(FlavorTextSettingsPanel p, string key)
        {
            if (_working == null) return;
            var label = p.FindControl<TextBlock>("FlavorTextCategoryPathLabel");
            var box = p.FindControl<TextBox>("FlavorTextCategoryEntriesTextBox");
            if (label != null)
                label.Text = $"<{key}>";
            if (box != null)
                box.Text = FlavorTextBankCatalog.EntriesToMultiline(FlavorTextBankCatalog.GetCategory(_working, key));
        }

        private void HighlightReferencedCategories(FlavorTextSettingsPanel p, string formId)
        {
            if (_working == null || !_working.Forms.TryGetValue(formId, out var form) || form == null)
                return;

            var refs = FlavorTextBankCatalog.ExtractCategoryRefs(form.Template);
            if (refs.Count == 0) return;

            // Prefer selecting the first referenced category that exists.
            string? first = refs.FirstOrDefault(r => _working.Categories.ContainsKey(r));
            if (first == null) return;
            _selectedCategoryKey = first;
            SelectCategoryInList(p, first);
            LoadCategoryEditor(p, first);
        }

        private void ClearFormEditor(FlavorTextSettingsPanel p)
        {
            _selectedFormId = null;
            var label = p.FindControl<TextBlock>("FlavorTextFormTemplateLabel");
            var box = p.FindControl<TextBox>("FlavorTextFormTemplateTextBox");
            if (label != null) label.Text = "Form template";
            if (box != null) box.Text = "";
        }

        private void ClearCategoryEditor(FlavorTextSettingsPanel p)
        {
            _selectedCategoryKey = null;
            var label = p.FindControl<TextBlock>("FlavorTextCategoryPathLabel");
            var box = p.FindControl<TextBox>("FlavorTextCategoryEntriesTextBox");
            if (label != null) label.Text = "Select a category";
            if (box != null) box.Text = "";
        }

        private void ClearBankEditor(FlavorTextSettingsPanel p)
        {
            _selectedBankPathId = null;
            var pathLabel = p.FindControl<TextBlock>("FlavorTextBankPathLabel");
            var entriesBox = p.FindControl<TextBox>("FlavorTextEntriesTextBox");
            if (pathLabel != null) pathLabel.Text = "Select a bank";
            if (entriesBox != null) entriesBox.Text = "";
        }

        private void SetStatus(FlavorTextSettingsPanel p, string message)
        {
            var status = p.FindControl<TextBlock>("FlavorTextStatusText");
            if (status != null)
                status.Text = _dirty ? $"{message} (unsaved edits)" : message;
        }

        private static FlavorTextData CloneData(FlavorTextData source)
        {
            string json = System.Text.Json.JsonSerializer.Serialize(source);
            var clone = System.Text.Json.JsonSerializer.Deserialize<FlavorTextData>(json) ?? new FlavorTextData();
            if (clone.CombatNarratives == null || clone.CombatNarratives.Comparer != StringComparer.OrdinalIgnoreCase)
            {
                clone.CombatNarratives = new Dictionary<string, string[]>(
                    clone.CombatNarratives ?? new Dictionary<string, string[]>(),
                    StringComparer.OrdinalIgnoreCase);
            }
            if (clone.Forms == null || clone.Forms.Comparer != StringComparer.OrdinalIgnoreCase)
            {
                clone.Forms = new Dictionary<string, FlavorFormDefinition>(
                    clone.Forms ?? new Dictionary<string, FlavorFormDefinition>(),
                    StringComparer.OrdinalIgnoreCase);
            }
            if (clone.Categories == null || clone.Categories.Comparer != StringComparer.OrdinalIgnoreCase)
            {
                clone.Categories = new Dictionary<string, string[]>(
                    clone.Categories ?? new Dictionary<string, string[]>(),
                    StringComparer.OrdinalIgnoreCase);
            }
            return clone;
        }

        private static async Task<string?> ShowIdPromptAsync(
            Control panel,
            string title,
            string prompt,
            string defaultValue)
        {
            Window? owner = TopLevel.GetTopLevel(panel) as Window;
            if (owner == null && Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
                owner = desktop.MainWindow;
            if (owner == null) return null;

            var input = new TextBox
            {
                Text = defaultValue,
                MinWidth = 320,
                Margin = new Thickness(0, 8, 0, 0)
            };
            var ok = new Button
            {
                Content = "OK",
                Width = 80,
                Margin = new Thickness(0, 0, 8, 0),
                Background = new SolidColorBrush(Color.Parse("#FF0078D4")),
                Foreground = Brushes.White,
                BorderThickness = new Thickness(0),
                CornerRadius = new CornerRadius(4)
            };
            var cancel = new Button
            {
                Content = "Cancel",
                Width = 80,
                BorderThickness = new Thickness(0),
                CornerRadius = new CornerRadius(4)
            };

            var dialog = new Window
            {
                Title = title,
                Width = 420,
                Height = 180,
                WindowStartupLocation = WindowStartupLocation.CenterOwner,
                CanResize = false,
                Content = new StackPanel
                {
                    Margin = new Thickness(16),
                    Spacing = 8,
                    Children =
                    {
                        new TextBlock { Text = prompt, TextWrapping = TextWrapping.Wrap },
                        input,
                        new StackPanel
                        {
                            Orientation = Orientation.Horizontal,
                            HorizontalAlignment = HorizontalAlignment.Right,
                            Margin = new Thickness(0, 12, 0, 0),
                            Children = { ok, cancel }
                        }
                    }
                }
            };

            string? result = null;
            ok.Click += (_, _) =>
            {
                result = input.Text;
                dialog.Close(true);
            };
            cancel.Click += (_, _) => dialog.Close(false);

            bool accepted = await dialog.ShowDialog<bool>(owner);
            return accepted ? result : null;
        }
    }
}
