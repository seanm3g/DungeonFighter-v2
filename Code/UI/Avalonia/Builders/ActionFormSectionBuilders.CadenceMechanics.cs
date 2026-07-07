using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.Media;
using RPGGame.Data;
using RPGGame.Editors;
using RPGGame.UI;
using RPGGame.UI.Avalonia.Resources;
using System;
using System.Collections.Generic;
using System.Linq;

namespace RPGGame.UI.Avalonia.Builders
{
    public partial class ActionFormSectionBuilders
    {
        private static readonly string[] StatSubTypeOptions = { "STR", "AGI", "TECH", "INT" };

        private StackPanel? _cadencePreviewPanel;
        private StackPanel? _cadenceBlocksPanel;

        public void BuildCadenceMechanicsSection(Panel parent, ActionData action)
        {
            _ctx.ClearCadenceFlushActions();
            _ctx.CadenceBlocks = ActionCadenceEditorSync.LoadBlocks(action);
            if (_ctx.CadenceBlocks.Count == 0)
                _ctx.CadenceBlocks.Add(new CadenceEditorBlock());

            var (section, stack) = _ctx.Factory.CreateFormSection("Cadence Mechanics");
            parent.Children.Add(section);

            stack.Children.Add(new TextBlock
            {
                Text = "Each block mirrors card text: cadence header, then one mechanic per line.",
                FontSize = 12,
                Foreground = SettingsThemeBrushes.TextMuted,
                TextWrapping = TextWrapping.Wrap,
                Margin = new Thickness(0, 0, 0, 8)
            });

            var previewLabel = new TextBlock
            {
                Text = "Card preview",
                FontSize = 14,
                FontWeight = FontWeight.SemiBold,
                Foreground = SettingsThemeBrushes.TextPrimary,
                Margin = new Thickness(0, 0, 0, 4)
            };
            stack.Children.Add(previewLabel);

            _cadencePreviewPanel = new StackPanel { Spacing = 2, Margin = new Thickness(0, 0, 0, 12) };
            stack.Children.Add(_cadencePreviewPanel);

            _cadenceBlocksPanel = new StackPanel { Spacing = 12 };
            stack.Children.Add(_cadenceBlocksPanel);

            _ctx.OnCadenceBlocksChanged = () => RefreshCadencePreview();
            RebuildCadenceBlockPanels();

            var addCadenceButton = CreateCadenceButton("+ New Cadence", 140);
            addCadenceButton.Click += (_, _) =>
            {
                _ctx.CadenceBlocks.Add(new CadenceEditorBlock());
                RebuildCadenceBlockPanels();
            };
            stack.Children.Add(addCadenceButton);
        }

        private void RebuildCadenceBlockPanels()
        {
            if (_cadenceBlocksPanel == null)
                return;
            _cadenceBlocksPanel.Children.Clear();
            for (int i = 0; i < _ctx.CadenceBlocks.Count; i++)
                _cadenceBlocksPanel.Children.Add(BuildCadenceBlockCard(_ctx.CadenceBlocks[i], i));
            RefreshCadencePreview();
        }

        private Border BuildCadenceBlockCard(CadenceEditorBlock block, int blockIndex)
        {
            var card = new Border
            {
                Background = SettingsThemeBrushes.SidebarItem,
                CornerRadius = new CornerRadius(4),
                Padding = new Thickness(12),
                BorderBrush = SettingsThemeBrushes.InputBorder,
                BorderThickness = new Thickness(1)
            };

            var cardStack = new StackPanel { Spacing = 8 };

            var headerGrid = new Grid();
            headerGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(100) });
            headerGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(120) });
            headerGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

            var cadenceCombo = new ComboBox
            {
                ItemsSource = ActionMechanicsRegistry.EditorCadenceOptions,
                SelectedItem = ActionFormOptions.NormalizeCadence(block.Cadence),
                FontSize = 14,
                Background = SettingsThemeBrushes.InputBackground,
                Foreground = SettingsThemeBrushes.TextPrimary,
                BorderBrush = SettingsThemeBrushes.InputBorder
            };
            if (cadenceCombo.SelectedItem == null && !string.IsNullOrWhiteSpace(block.Cadence))
                cadenceCombo.SelectedItem = block.Cadence;
            cadenceCombo.SelectionChanged += (_, _) =>
            {
                if (cadenceCombo.SelectedItem is string selected)
                {
                    block.Cadence = selected;
                    RefreshMechanicRowsForBlock(cardStack, block, blockIndex);
                    _ctx.NotifyCadenceBlocksChanged();
                }
            };
            Grid.SetColumn(cadenceCombo, 0);
            headerGrid.Children.Add(cadenceCombo);

            var durationBox = new TextBox
            {
                Text = block.Duration.ToString(),
                Watermark = "Duration",
                FontSize = 14,
                Background = SettingsThemeBrushes.InputBackground,
                Foreground = SettingsThemeBrushes.TextPrimary,
                BorderBrush = SettingsThemeBrushes.InputBorder,
                Margin = new Thickness(8, 0, 0, 0)
            };
            void SyncDurationFromText()
            {
                if (int.TryParse(durationBox.Text, out int d) && d >= 1)
                    block.Duration = d;
                _ctx.NotifyCadenceBlocksChanged();
            }
            // TextChanged keeps block model in sync when Save is clicked before LostFocus (same pattern as Name/Description).
            durationBox.TextChanged += (_, _) => SyncDurationFromText();
            durationBox.LostFocus += (_, _) =>
            {
                if (!int.TryParse(durationBox.Text, out int d) || d < 1)
                    durationBox.Text = block.Duration.ToString();
                else
                    block.Duration = d;
                _ctx.NotifyCadenceBlocksChanged();
            };
            Grid.SetColumn(durationBox, 1);
            headerGrid.Children.Add(durationBox);

            _ctx.RegisterCadenceFlush(() =>
            {
                if (cadenceCombo.SelectedItem is string selectedCadence)
                    block.Cadence = selectedCadence;
                if (int.TryParse(durationBox.Text, out int d) && d >= 1)
                    block.Duration = d;
            });

            if (_ctx.CadenceBlocks.Count > 1)
            {
                var removeBlockBtn = CreateCadenceButton("Remove block", 110);
                removeBlockBtn.Margin = new Thickness(8, 0, 0, 0);
                removeBlockBtn.Click += (_, _) =>
                {
                    if (blockIndex >= 0 && blockIndex < _ctx.CadenceBlocks.Count)
                    {
                        _ctx.CadenceBlocks.RemoveAt(blockIndex);
                        RebuildCadenceBlockPanels();
                    }
                };
                Grid.SetColumn(removeBlockBtn, 2);
                headerGrid.Children.Add(removeBlockBtn);
            }

            cardStack.Children.Add(headerGrid);

            var mechanicRowsPanel = new StackPanel { Spacing = 6, Tag = "mechanicRows" };
            cardStack.Children.Add(mechanicRowsPanel);
            PopulateMechanicRows(mechanicRowsPanel, block, blockIndex);

            var addMechBtn = CreateCadenceButton("+ New Mechanic", 130);
            addMechBtn.Click += (_, _) =>
            {
                block.Mechanics.Add(new CadenceMechanicRow());
                PopulateMechanicRows(mechanicRowsPanel, block, blockIndex);
                _ctx.NotifyCadenceBlocksChanged();
            };
            cardStack.Children.Add(addMechBtn);

            card.Child = cardStack;
            return card;
        }

        private void RefreshMechanicRowsForBlock(StackPanel cardStack, CadenceEditorBlock block, int blockIndex)
        {
            var mechanicRowsPanel = cardStack.Children.OfType<StackPanel>()
                .FirstOrDefault(p => "mechanicRows".Equals(p.Tag));
            if (mechanicRowsPanel != null)
                PopulateMechanicRows(mechanicRowsPanel, block, blockIndex);
        }

        private void PopulateMechanicRows(StackPanel mechanicRowsPanel, CadenceEditorBlock block, int blockIndex)
        {
            mechanicRowsPanel.Children.Clear();
            if (block.Mechanics.Count == 0)
                block.Mechanics.Add(new CadenceMechanicRow());

            for (int i = 0; i < block.Mechanics.Count; i++)
                mechanicRowsPanel.Children.Add(BuildMechanicRow(block, block.Mechanics[i], blockIndex, i, mechanicRowsPanel));
        }

        private Grid BuildMechanicRow(CadenceEditorBlock block, CadenceMechanicRow row, int blockIndex, int rowIndex, StackPanel mechanicRowsPanel)
        {
            var grid = new Grid { Margin = new Thickness(0, 2, 0, 2) };
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(72) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(80) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(70) });

            var mechanicOptions = ActionMechanicsRegistry.GetMechanicIdsForCadence(block.Cadence)
                .Select(id => (id, label: $"{ActionMechanicsRegistry.GetDisplayLabel(id)} ({id})"))
                .ToList();

            var mechanicCombo = new ComboBox
            {
                ItemsSource = mechanicOptions.Select(m => m.label).ToList(),
                FontSize = 13,
                Background = SettingsThemeBrushes.InputBackground,
                Foreground = SettingsThemeBrushes.TextPrimary,
                BorderBrush = SettingsThemeBrushes.InputBorder
            };
            if (!string.IsNullOrWhiteSpace(row.MechanicId))
            {
                string match = mechanicOptions.FirstOrDefault(m => string.Equals(m.id, row.MechanicId, StringComparison.OrdinalIgnoreCase)).label;
                if (!string.IsNullOrEmpty(match))
                    mechanicCombo.SelectedItem = match;
            }
            Grid.SetColumn(mechanicCombo, 0);
            grid.Children.Add(mechanicCombo);

            var statCombo = new ComboBox
            {
                ItemsSource = StatSubTypeOptions,
                SelectedItem = string.IsNullOrWhiteSpace(row.StatSubType) ? "STR" : row.StatSubType.ToUpperInvariant(),
                FontSize = 13,
                IsVisible = ActionMechanicsRegistry.RequiresStatSubType(row.MechanicId),
                Background = SettingsThemeBrushes.InputBackground,
                Foreground = SettingsThemeBrushes.TextPrimary,
                BorderBrush = SettingsThemeBrushes.InputBorder,
                Margin = new Thickness(6, 0, 0, 0)
            };
            mechanicCombo.SelectionChanged += (_, _) =>
            {
                if (mechanicCombo.SelectedItem is string label)
                {
                    var picked = mechanicOptions.FirstOrDefault(m => m.label == label);
                    row.MechanicId = picked.id ?? "";
                    statCombo.IsVisible = ActionMechanicsRegistry.RequiresStatSubType(row.MechanicId);
                    _ctx.NotifyCadenceBlocksChanged();
                }
            };
            statCombo.SelectionChanged += (_, _) =>
            {
                if (statCombo.SelectedItem is string s)
                {
                    row.StatSubType = s;
                    _ctx.NotifyCadenceBlocksChanged();
                }
            };
            Grid.SetColumn(statCombo, 1);
            grid.Children.Add(statCombo);

            var qtyBox = new TextBox
            {
                Text = row.Quantity == 0 ? "" : row.Quantity.ToString("0.##"),
                Watermark = "Qty",
                FontSize = 13,
                Background = SettingsThemeBrushes.InputBackground,
                Foreground = SettingsThemeBrushes.TextPrimary,
                BorderBrush = SettingsThemeBrushes.InputBorder,
                Margin = new Thickness(6, 0, 0, 0)
            };
            void SyncQtyFromText()
            {
                row.Quantity = double.TryParse(qtyBox.Text, out double v) ? v : 0;
                _ctx.NotifyCadenceBlocksChanged();
            }
            qtyBox.TextChanged += (_, _) => SyncQtyFromText();
            qtyBox.LostFocus += (_, _) => SyncQtyFromText();
            Grid.SetColumn(qtyBox, 2);
            grid.Children.Add(qtyBox);

            var removeBtn = CreateCadenceButton("Remove", 64);
            removeBtn.Margin = new Thickness(6, 0, 0, 0);
            removeBtn.Click += (_, _) =>
            {
                if (rowIndex >= 0 && rowIndex < block.Mechanics.Count)
                {
                    block.Mechanics.RemoveAt(rowIndex);
                    if (block.Mechanics.Count == 0)
                        block.Mechanics.Add(new CadenceMechanicRow());
                    PopulateMechanicRows(mechanicRowsPanel, block, blockIndex);
                    _ctx.NotifyCadenceBlocksChanged();
                }
            };
            Grid.SetColumn(removeBtn, 3);
            grid.Children.Add(removeBtn);

            _ctx.RegisterCadenceFlush(() =>
            {
                if (mechanicCombo.SelectedItem is string label)
                    row.MechanicId = ParseMechanicIdFromEditorLabel(label) ?? "";
                if (statCombo.SelectedItem is string stat)
                    row.StatSubType = stat;
                row.Quantity = double.TryParse(qtyBox.Text, out double v) ? v : 0;
            });

            return grid;
        }

        private static string? ParseMechanicIdFromEditorLabel(string? label)
        {
            if (string.IsNullOrWhiteSpace(label))
                return null;
            int open = label.LastIndexOf('(');
            int close = label.LastIndexOf(')');
            if (open >= 0 && close > open)
                return ActionMechanicsRegistry.NormalizeMechanicId(label.Substring(open + 1, close - open - 1));
            return ActionMechanicsRegistry.NormalizeMechanicId(label);
        }

        private void RefreshCadencePreview()
        {
            if (_cadencePreviewPanel == null)
                return;
            _cadencePreviewPanel.Children.Clear();
            var lines = CadenceCardLineFormatter.FormatAllBlocks(_ctx.CadenceBlocks);
            if (lines.Count == 0)
            {
                _cadencePreviewPanel.Children.Add(new TextBlock
                {
                    Text = "(no cadence mechanics)",
                    FontSize = 13,
                    FontStyle = FontStyle.Italic,
                    Foreground = SettingsThemeBrushes.TextMuted
                });
                return;
            }
            foreach (var line in lines)
            {
                if (line == "")
                {
                    _cadencePreviewPanel.Children.Add(new Border { Height = 6 });
                    continue;
                }
                bool isHeader = line.Contains('(') && line.EndsWith("x)", StringComparison.Ordinal);
                _cadencePreviewPanel.Children.Add(new TextBlock
                {
                    Text = line,
                    FontSize = isHeader ? 14 : 13,
                    FontWeight = isHeader ? FontWeight.SemiBold : FontWeight.Normal,
                    Foreground = isHeader ? SettingsThemeBrushes.TextTitle : SettingsThemeBrushes.TextPrimary,
                    Margin = new Thickness(isHeader ? 0 : 8, isHeader ? 4 : 0, 0, 0)
                });
            }
        }

        private static Button CreateCadenceButton(string content, double width) => new Button
        {
            Content = content,
            Width = width,
            Height = 30,
            FontSize = 13,
            Background = new SolidColorBrush(Color.FromRgb(60, 90, 120)),
            Foreground = new SolidColorBrush(Colors.White),
            BorderThickness = new Thickness(0),
            CornerRadius = new CornerRadius(3),
            Cursor = new Cursor(StandardCursorType.Hand)
        };

        public void BuildAdvancedExpanderSection(Panel parent, ActionData action)
        {
            var expander = new Expander
            {
                Header = new TextBlock
                {
                    Text = "Advanced",
                    FontSize = 16,
                    FontWeight = FontWeight.SemiBold,
                    Foreground = SettingsThemeBrushes.TextTitle
                },
                IsExpanded = false,
                Margin = new Thickness(0, 8, 0, 0)
            };

            var advancedStack = new StackPanel { Spacing = 4 };
            BuildComboAndPositionSection(advancedStack, action);
            BuildTriggersSection(advancedStack, action);
            BuildThresholdsSection(advancedStack, action);
            BuildAccumulationsSection(advancedStack, action);
            BuildChainPositionBonusesOnlySection(advancedStack, action);
            BuildItemStatusSection(advancedStack, action);

            expander.Content = advancedStack;
            parent.Children.Add(expander);
        }

        private void BuildChainPositionBonusesOnlySection(Panel parent, ActionData action)
        {
            var (section, stack) = _ctx.Factory.CreateFormSection("Chain Position Bonuses");
            parent.Children.Add(section);

            action.NormalizeChainPositionBonuses();
            var chainRows = new StackPanel { Spacing = 6, Margin = new Thickness(0, 0, 0, 10) };
            stack.Children.Add(new TextBlock
            {
                Text = "Enable \"Chain Position MOD\" in Combo & Position to apply in combat.",
                FontSize = 12,
                Foreground = SettingsThemeBrushes.TextMuted,
                TextWrapping = TextWrapping.Wrap,
                Margin = new Thickness(0, 0, 0, 8)
            });
            stack.Children.Add(chainRows);

            foreach (var entry in action.ChainPositionBonuses)
                AddChainPositionBonusRow(chainRows, action, entry);

            var addChainBonusButton = CreateCadenceButton("Add chain position bonus", 200);
            addChainBonusButton.Click += (_, _) =>
            {
                action.NormalizeChainPositionBonuses();
                action.ChainPositionBonuses.Add(new ChainPositionBonusEntry { PositionBasis = "ComboSlotIndex1" });
                AddChainPositionBonusRow(chainRows, action, action.ChainPositionBonuses[^1]);
            };
            stack.Children.Add(addChainBonusButton);
        }

        private void BuildItemStatusSection(Panel parent, ActionData action)
        {
            var (section, stack) = _ctx.Factory.CreateFormSection("Item-Applied Status");
            parent.Children.Add(section);
            stack.Children.Add(new TextBlock
            {
                Text = "Stun, poison, burn, and bleed are granted via items — not cadence mechanics.",
                FontSize = 12,
                Foreground = SettingsThemeBrushes.TextMuted,
                TextWrapping = TextWrapping.Wrap,
                Margin = new Thickness(0, 0, 0, 8)
            });
            _ctx.Factory.AddBooleanField(stack, "CausesStun", action.CausesStun, (value) => action.CausesStun = value);
            _ctx.Factory.AddBooleanField(stack, "CausesPoison", action.CausesPoison, (value) => action.CausesPoison = value);
            _ctx.Factory.AddBooleanField(stack, "CausesBurn", action.CausesBurn, (value) => action.CausesBurn = value);
            _ctx.Factory.AddBooleanField(stack, "CausesBleed", action.CausesBleed, (value) => action.CausesBleed = value);
            _ctx.Factory.AddBooleanField(stack, "CausesExpose", action.CausesExpose, (value) => action.CausesExpose = value);
            _ctx.Factory.AddBooleanField(stack, "CausesSilence", action.CausesSilence, (value) => action.CausesSilence = value);
        }
    }
}
