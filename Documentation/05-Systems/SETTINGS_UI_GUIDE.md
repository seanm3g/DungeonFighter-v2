# Settings UI Guide

Flat-dark settings chrome with shared theme tokens and a single panel registry.

## Theme

**File:** `Code/UI/Avalonia/Resources/SettingsTheme.axaml` (merged in `App.axaml`)

Scoped with the `settings-ui` class on `SettingsPanel` and child panels. Key resources:

| Resource | Use |
|----------|-----|
| `SettingsBackground` | Shell background |
| `SettingsTextPrimary` | Headers, labels, input text |
| `SettingsTextMuted` | Descriptions, hints |
| `SettingsTextTitle` | Gold title ("GAME SETTINGS") |
| `SettingsInputBackground` | TextBox, ComboBox |
| `SettingsInputBorder` / `SettingsInputFocusBorder` | Input chrome |

Typography classes: `settings-title`, `settings-panel-title`, `settings-section-header`, `settings-field-label`, `settings-muted`.

**C# builders:** use `SettingsThemeBrushes` or `SettingsInputApplier` (`ApplyTextBlock`, `ApplyCheckBox`, `ApplyTextBox`, `ApplyComboBox`, `ApplyNumericUpDown`) for programmatic controls. Do not set `Colors.Black` or Fluent-default foreground on dark panels.

Baseline `.settings-ui TextBlock` / `.settings-ui Label` styles in the theme override Fluent light-theme defaults so unstyled labels stay readable on the dark shell.

Runtime `TextBoxColorManager` and `BorderColorManager` no longer walk panel content — the theme is the single source of truth for inputs (Appearance color pickers keep their own inline styles).

## Layout components

| Component | Purpose |
|-----------|---------|
| `SettingsPanelRoot` | ScrollViewer + spaced StackPanel (`Margin 20`, `Spacing 24`) |
| `SettingsSection` | Collapsible section header + content |
| `SettingsFieldRow` | Label + control row |
| `SliderWithTextBox` | Slider + numeric field (legacy; tuning panels use ViewModel + Slider/TextBox instead) |

## Layout rules (flat dark)

1. Add `Classes="settings-ui"` on panel `UserControl` roots.
2. Use **spacing and typography**, not bordered section cards.
3. Do **not** add per-panel `UserControl.Styles` for TextBox/CheckBox — inherit from theme.
4. Do **not** set inline `Background`/`Foreground` on TextBoxes unless required (e.g. color preview swatches).
5. Section breaks: `settings-section-header` TextBlock + `Spacing` on parent StackPanel.

## Adding a new settings panel

1. Create `MyFeatureSettingsPanel.axaml` with `Classes="settings-ui"` and flat layout.
2. Add one entry to `SettingsPanelCatalog.AllPanels` in `SettingsPanelCatalog.cs`:
   - `Tag`, `DisplayName`, `Factory`, `ContentArea` (default `MainScroll`)
   - `PanelType`, `UsesHandler`, `UsesTabManager`, `SavesViaHandler` as needed
   - **`SidebarGroup`** (`SettingsSidebarGroups.Player`, `.Developer`, `.Balance`, `.Testing`, or `.About`) and **`Order`** within that group
3. If settings persist via handler: implement `ISettingsPanelHandler`, register in `SettingsPanel.InitializeManagers()`, set `SavesViaHandler: true` on descriptor (and ensure tag is in `HandlerSaveCategoryTags` order if balance-related).
4. If CRUD/tab UI: add `panelInitializers` entry keyed by `Tag`.

Sidebar and `LoadCategoryPanel` routing are generated from the catalog — no manual `ListBoxItem` or special-case branches. Group headers are rendered automatically from `SettingsSidebarGroups.OrderedGroups`.

## Grouped sidebar

| Group ID | Header label |
|----------|--------------|
| `Player` | Player Settings |
| `Developer` | Developer Settings |
| `Balance` | Balance & Tuning |
| `Testing` | Testing |
| `About` | *(no header — About item only)* |

Non-selectable header rows use tag `SettingsSidebarGroups.HeaderTag`. Panel items use class `settings-sidebar-panel-item` (indented).

## Container panels (merged tabs)

When merging related panels under one sidebar entry:

1. Create a container `UserControl` with a `TabControl` embedding existing child panels.
2. Register the container in the catalog with a new tag (e.g. `TextAndAnimation`, `ItemAffixes`).
3. For handlers: create a delegating handler that calls child handlers' `WireUp` / `LoadSettings` / `SaveSettings`.
4. For tab managers: wire both managers in a single `panelInitializers` entry.

## UI migration status

| Panel | SettingsPanelRoot | Theme classes | Notes |
|-------|-------------------|---------------|-------|
| Gameplay | — | Yes | Uses `settings-section-header` |
| Travel | — | Yes | |
| Audio | — | Yes | |
| Text & Animation | — | Yes | Container with tab embed |
| Text Animation presets (child) | — | Partial | Preview chrome uses theme tokens; color swatches keep inline styles |
| Appearance | — | Yes | Tabbed: Settings UI, Color Codes, Templates, Keywords, Combat Text |
| Combat Tuning | — | Yes | Container with Combat + Enemy tabs; combat parameters and enemy progression/spawn weights |
| Classes | — | Partial | |
| About | — | Yes | |
| Actions / Testing / Item Generation | — | Yes | Dedicated content areas |
| Tab managers (Game Variables, Items, etc.) | — | Partial | Root wrappers pending |

Use `SettingsPanelRoot` + `SettingsSection` for new panels; migrate existing panels incrementally.

## Appearance customization

`SettingsColorManager` applies **shell chrome only** (title, sidebar, buttons). Panel content uses static theme tokens so new panels stay consistent when users change colors in Appearance.
