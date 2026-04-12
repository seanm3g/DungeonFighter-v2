using System;
using System.Collections.Generic;
using System.Linq;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using RPGGame;
using RPGGame.UI.Avalonia.Settings;

namespace RPGGame.UI.Avalonia.Managers.Settings.PanelHandlers
{
    public class ClassesPanelHandler : ISettingsPanelHandler
    {
        /// <summary>Four named tier bands per path (engine tier indices 1–4); excludes pre-threshold band 0.</summary>
        private const int HybridDuoMatrixBandSlots = 4;

        private static readonly SolidColorBrush HybridLabelBrush =
            new(global::Avalonia.Media.Color.Parse("#1a1a1a"));

        /// <summary>Hybrid matrix fields use the dark Fluent TextBox chrome; keep typed text light.</summary>
        private static readonly SolidColorBrush HybridMatrixInputForeground =
            new(global::Avalonia.Media.Color.Parse("#f2f2f2"));

        private readonly Action<string, bool>? showStatusMessage;

        public ClassesPanelHandler(Action<string, bool>? showStatusMessage = null)
        {
            this.showStatusMessage = showStatusMessage;
        }

        public string PanelType => "Classes";

        public void WireUp(UserControl panel)
        {
            if (panel is not ClassesSettingsPanel p) return;
            void hook(TextBox? tb)
            {
                if (tb != null) tb.TextChanged += (_, _) => RefreshSummary(p);
            }
            hook(p.FindControl<TextBox>("MaceClassTextBox"));
            hook(p.FindControl<TextBox>("SwordClassTextBox"));
            hook(p.FindControl<TextBox>("DaggerClassTextBox"));
            hook(p.FindControl<TextBox>("WandClassTextBox"));
            hook(p.FindControl<TextBox>("TierThreshold1TextBox"));
            hook(p.FindControl<TextBox>("TierThreshold2TextBox"));
            hook(p.FindControl<TextBox>("TierThreshold3TextBox"));
            hook(p.FindControl<TextBox>("TierThreshold4TextBox"));
            hook(p.FindControl<TextBox>("MeaningfulAttributeMinimumTextBox"));
            hook(p.FindControl<TextBox>("PreviewClassPointsForTierTextBox"));
            foreach (var name in new[]
                     {
                         "AttrSoloTrioPrefix1TextBox", "AttrSoloTrioPrefix2TextBox", "AttrSoloTrioPrefix3TextBox", "AttrSoloTrioPrefix4TextBox",
                         "AttrQuadTier1TextBox", "AttrQuadTier2TextBox", "AttrQuadTier3TextBox", "AttrQuadTier4TextBox",
                         "AttrModMaceTextBox", "AttrModSwordTextBox", "AttrModDaggerTextBox", "AttrModWandTextBox",
                         "AttrDuoMaceSwordTextBox", "AttrDuoMaceDaggerTextBox", "AttrDuoMaceWandTextBox",
                         "AttrDuoSwordDaggerTextBox", "AttrDuoSwordWandTextBox", "AttrDuoDaggerWandTextBox",
                         "AttrTrioMaceSwordDaggerTextBox", "AttrTrioMaceSwordWandTextBox", "AttrTrioMaceDaggerWandTextBox", "AttrTrioSwordDaggerWandTextBox"
                     })
                hook(p.FindControl<TextBox>(name));
            hook(p.FindControl<TextBox>("PreviewStrTextBox"));
            hook(p.FindControl<TextBox>("PreviewAgiTextBox"));
            hook(p.FindControl<TextBox>("PreviewTecTextBox"));
            hook(p.FindControl<TextBox>("PreviewIntTextBox"));
            hook(p.FindControl<TextBox>("DefaultClassTextBox"));
            LoadSettings(p);
        }

        public void LoadSettings(UserControl panel)
        {
            if (panel is not ClassesSettingsPanel p) return;
            var c = GameConfiguration.Instance.ClassPresentation.EnsureNormalized();
            Set(p, "MaceClassTextBox", c.MaceClassDisplayName);
            Set(p, "SwordClassTextBox", c.SwordClassDisplayName);
            Set(p, "DaggerClassTextBox", c.DaggerClassDisplayName);
            Set(p, "WandClassTextBox", c.WandClassDisplayName);
            Set(p, "TierThreshold1TextBox", c.TierThresholds[0].ToString());
            Set(p, "TierThreshold2TextBox", c.TierThresholds[1].ToString());
            Set(p, "TierThreshold3TextBox", c.TierThresholds[2].ToString());
            Set(p, "TierThreshold4TextBox", c.TierThresholds[3].ToString());
            Set(p, "MeaningfulAttributeMinimumTextBox", c.MeaningfulAttributeMinimum.ToString());
            Set(p, "PreviewClassPointsForTierTextBox", "30");
            var st = c.AttributeSoloTrioTierPrefixes;
            Set(p, "AttrSoloTrioPrefix1TextBox", st[0]);
            Set(p, "AttrSoloTrioPrefix2TextBox", st[1]);
            Set(p, "AttrSoloTrioPrefix3TextBox", st[2]);
            Set(p, "AttrSoloTrioPrefix4TextBox", st[3]);
            var qt = c.AttributeQuadTierNames;
            Set(p, "AttrQuadTier1TextBox", qt[0]);
            Set(p, "AttrQuadTier2TextBox", qt[1]);
            Set(p, "AttrQuadTier3TextBox", qt[2]);
            Set(p, "AttrQuadTier4TextBox", qt[3]);
            Set(p, "AttrModMaceTextBox", c.AttributeModifierMace);
            Set(p, "AttrModSwordTextBox", c.AttributeModifierSword);
            Set(p, "AttrModDaggerTextBox", c.AttributeModifierDagger);
            Set(p, "AttrModWandTextBox", c.AttributeModifierWand);
            Set(p, "AttrDuoMaceSwordTextBox", c.AttributeDuoMaceSword);
            Set(p, "AttrDuoMaceDaggerTextBox", c.AttributeDuoMaceDagger);
            Set(p, "AttrDuoMaceWandTextBox", c.AttributeDuoMaceWand);
            Set(p, "AttrDuoSwordDaggerTextBox", c.AttributeDuoSwordDagger);
            Set(p, "AttrDuoSwordWandTextBox", c.AttributeDuoSwordWand);
            Set(p, "AttrDuoDaggerWandTextBox", c.AttributeDuoDaggerWand);
            Set(p, "AttrTrioMaceSwordDaggerTextBox", c.AttributeTrioMaceSwordDagger);
            Set(p, "AttrTrioMaceSwordWandTextBox", c.AttributeTrioMaceSwordWand);
            Set(p, "AttrTrioMaceDaggerWandTextBox", c.AttributeTrioMaceDaggerWand);
            Set(p, "AttrTrioSwordDaggerWandTextBox", c.AttributeTrioSwordDaggerWand);
            Set(p, "PreviewStrTextBox", "14");
            Set(p, "PreviewAgiTextBox", "12");
            Set(p, "PreviewTecTextBox", "10");
            Set(p, "PreviewIntTextBox", "8");
            Set(p, "DefaultClassTextBox", c.DefaultNoPointsClassName);
            RefreshSummary(p);
        }

        private static void UpdateEvolvedGridHeaders(ClassesSettingsPanel p)
        {
            if (p.FindControl<TextBlock>("EvolvedHeaderMace") == null)
                return;
            static string PathLabel(ClassesSettingsPanel panel, string textBoxName, WeaponType wt)
            {
                string t = T(panel, textBoxName);
                if (!string.IsNullOrWhiteSpace(t)) return t;
                return GameConfiguration.Instance.ClassPresentation.EnsureNormalized().GetDisplayName(wt);
            }
            void SetHeader(string name, string text)
            {
                if (p.FindControl<TextBlock>(name) is { } tb)
                    tb.Text = text;
            }
            SetHeader("EvolvedHeaderMace", PathLabel(p, "MaceClassTextBox", WeaponType.Mace));
            SetHeader("EvolvedHeaderSword", PathLabel(p, "SwordClassTextBox", WeaponType.Sword));
            SetHeader("EvolvedHeaderDagger", PathLabel(p, "DaggerClassTextBox", WeaponType.Dagger));
            SetHeader("EvolvedHeaderWand", PathLabel(p, "WandClassTextBox", WeaponType.Wand));
        }

        private static void LoadPathTiers(ClassesSettingsPanel p, string prefix, string[]? tiers)
        {
            for (int i = 0; i < ClassPresentationConfig.TierSlotCount; i++)
            {
                string box = $"{prefix}Tier{i + 1}TextBox";
                string v = (tiers != null && i < tiers.Length) ? tiers[i] : "";
                Set(p, box, v ?? "");
            }
        }

        public void SaveSettings(UserControl panel)
        {
            if (panel is not ClassesSettingsPanel p) return;
            try
            {
                var cfg = ReadFromPanel(p).EnsureNormalized();
                GameConfiguration.Instance.ClassPresentation = cfg;
                if (!GameConfiguration.Instance.SaveToFile())
                    throw new InvalidOperationException("SaveToFile returned false.");
                showStatusMessage?.Invoke("Class presentation saved to TuningConfig.json.", true);
            }
            catch (Exception ex)
            {
                showStatusMessage?.Invoke($"Classes settings: {ex.Message}", false);
            }
        }

        private static void Set(ClassesSettingsPanel p, string name, string value)
        {
            if (p.FindControl<TextBox>(name) is { } tb)
                tb.Text = value;
        }

        private static string T(ClassesSettingsPanel p, string name) =>
            p.FindControl<TextBox>(name)?.Text?.Trim() ?? "";

        private static StackPanel? DuoMatrixHost(ClassesSettingsPanel p) =>
            p.FindControl<StackPanel>("HybridDuoMatrixHost");

        private static StackPanel? TrioRowsHost(ClassesSettingsPanel p) =>
            p.FindControl<StackPanel>("HybridTrioRowsHost");

        private static string THybrid(ClassesSettingsPanel p, string textBoxName)
        {
            var host = DuoMatrixHost(p);
            if (host?.FindControl<TextBox>(textBoxName) is { } tb)
                return tb.Text?.Trim() ?? "";
            var trio = TrioRowsHost(p);
            if (trio?.FindControl<TextBox>(textBoxName) is { } t2)
                return t2.Text?.Trim() ?? "";
            return "";
        }

        private static void RegisterOnHybridHost(Control nameScopeOwner, Control c, string name)
        {
            c.Name = name;
            var scope = NameScope.GetNameScope(nameScopeOwner)
                        ?? throw new InvalidOperationException("Hybrid host is missing a NameScope.");
            scope.Register(name, c);
        }

        private static string[]? ReadPathTiersFromPanel(ClassesSettingsPanel p, string prefix)
        {
            string a = T(p, $"{prefix}Tier1TextBox");
            string b = T(p, $"{prefix}Tier2TextBox");
            string c = T(p, $"{prefix}Tier3TextBox");
            string d = T(p, $"{prefix}Tier4TextBox");
            if (a.Length == 0 && b.Length == 0 && c.Length == 0 && d.Length == 0)
                return null;
            return new[] { a, b, c, d };
        }

        private static ClassPresentationConfig ReadFromPanel(ClassesSettingsPanel p)
        {
            var cur = GameConfiguration.Instance.ClassPresentation?.EnsureNormalized()
                      ?? new ClassPresentationConfig().EnsureNormalized();
            if (!int.TryParse(T(p, "TierThreshold1TextBox"), out int t1)
                || !int.TryParse(T(p, "TierThreshold2TextBox"), out int t2)
                || !int.TryParse(T(p, "TierThreshold3TextBox"), out int t3)
                || !int.TryParse(T(p, "TierThreshold4TextBox"), out int t4))
                throw new FormatException("Tier thresholds must be integers.");
            int meaningfulAttr = int.TryParse(T(p, "MeaningfulAttributeMinimumTextBox"), out int mm) ? mm : 8;
            var duo = DuoMatrixHost(p) != null ? ReadHybridDuoFromMatrix(p) : cur.HybridDuoTierRules;
            var trio = TrioRowsHost(p) != null ? ReadHybridTrioFromRows(p) : cur.HybridTrioRules;
            string[]? quadHybrid;
            if (p.FindControl<TextBox>("HybridQuadTitlesTextBox") is { } quadBox)
            {
                var quad = ParseQuadFromPanel(quadBox.Text);
                quadHybrid = quad.Length > 0 ? quad : null;
            }
            else
                quadHybrid = cur.QuadHybridTitles;
            return new ClassPresentationConfig
            {
                MaceClassDisplayName = T(p, "MaceClassTextBox"),
                SwordClassDisplayName = T(p, "SwordClassTextBox"),
                DaggerClassDisplayName = T(p, "DaggerClassTextBox"),
                WandClassDisplayName = T(p, "WandClassTextBox"),
                TierNames = cur.TierNames,
                MaceTierNames = ReadPathTiersFromPanel(p, "Mace") ?? cur.MaceTierNames,
                SwordTierNames = ReadPathTiersFromPanel(p, "Sword") ?? cur.SwordTierNames,
                DaggerTierNames = ReadPathTiersFromPanel(p, "Dagger") ?? cur.DaggerTierNames,
                WandTierNames = ReadPathTiersFromPanel(p, "Wand") ?? cur.WandTierNames,
                TierThresholds = new[] { t1, t2, t3, t4 },
                DefaultNoPointsClassName = T(p, "DefaultClassTextBox"),
                PreTierLabel = cur.PreTierLabel,
                HybridJoiner = cur.HybridJoiner,
                HybridDuoTierRules = duo,
                HybridTrioRules = trio,
                QuadHybridTitles = quadHybrid,
                MeaningfulAttributeMinimum = meaningfulAttr,
                AttributeSoloTrioTierPrefixes = new[]
                {
                    T(p, "AttrSoloTrioPrefix1TextBox"), T(p, "AttrSoloTrioPrefix2TextBox"),
                    T(p, "AttrSoloTrioPrefix3TextBox"), T(p, "AttrSoloTrioPrefix4TextBox")
                },
                AttributeQuadTierNames = new[]
                {
                    T(p, "AttrQuadTier1TextBox"), T(p, "AttrQuadTier2TextBox"),
                    T(p, "AttrQuadTier3TextBox"), T(p, "AttrQuadTier4TextBox")
                },
                AttributeModifierMace = T(p, "AttrModMaceTextBox"),
                AttributeModifierSword = T(p, "AttrModSwordTextBox"),
                AttributeModifierDagger = T(p, "AttrModDaggerTextBox"),
                AttributeModifierWand = T(p, "AttrModWandTextBox"),
                AttributeDuoMaceSword = T(p, "AttrDuoMaceSwordTextBox"),
                AttributeDuoMaceDagger = T(p, "AttrDuoMaceDaggerTextBox"),
                AttributeDuoMaceWand = T(p, "AttrDuoMaceWandTextBox"),
                AttributeDuoSwordDagger = T(p, "AttrDuoSwordDaggerTextBox"),
                AttributeDuoSwordWand = T(p, "AttrDuoSwordWandTextBox"),
                AttributeDuoDaggerWand = T(p, "AttrDuoDaggerWandTextBox"),
                AttributeTrioMaceSwordDagger = T(p, "AttrTrioMaceSwordDaggerTextBox"),
                AttributeTrioMaceSwordWand = T(p, "AttrTrioMaceSwordWandTextBox"),
                AttributeTrioMaceDaggerWand = T(p, "AttrTrioMaceDaggerWandTextBox"),
                AttributeTrioSwordDaggerWand = T(p, "AttrTrioSwordDaggerWandTextBox")
            };
        }

        private static void RefreshSummary(ClassesSettingsPanel p)
        {
            UpdateEvolvedGridHeaders(p);
            UpdateHybridDynamicLabels(p);
            if (p.FindControl<TextBlock>("SummaryTextBlock") is not { } tb) return;
            try
            {
                tb.Text = CharacterProgression.BuildClassSystemSettingsSummary(ReadFromPanel(p));
            }
            catch
            {
                tb.Text = "(Fix invalid values to refresh summary.)";
            }

            RefreshAttributeClassPreview(p);
        }

        private static void RefreshAttributeClassPreview(ClassesSettingsPanel p)
        {
            if (p.FindControl<TextBlock>("AttributeClassPreviewTextBlock") is not { } tb) return;
            try
            {
                var cfg = ReadFromPanel(p).EnsureNormalized();
                int str = int.TryParse(T(p, "PreviewStrTextBox"), out int s) ? s : 14;
                int agi = int.TryParse(T(p, "PreviewAgiTextBox"), out int a) ? a : 12;
                int tec = int.TryParse(T(p, "PreviewTecTextBox"), out int t) ? t : 10;
                int intel = int.TryParse(T(p, "PreviewIntTextBox"), out int i) ? i : 8;
                var stats = new CharacterStats(1)
                {
                    Strength = str,
                    Agility = agi,
                    Technique = tec,
                    Intelligence = intel
                };
                int previewPts = int.TryParse(T(p, "PreviewClassPointsForTierTextBox"), out int pp) ? Math.Max(0, pp) : 0;
                var previewProg = new CharacterProgression();
                if (previewPts > 0)
                    previewProg.WarriorPoints = previewPts;
                string composed = AttributeClassNameComposer.ComposeDisplayClass(stats, previewProg, cfg);
                tb.Text = $"Preview title: {composed}";
            }
            catch
            {
                tb.Text = "Preview title: (fix class / attribute numeric fields)";
            }
        }

        private static void EnsureHybridGrids(ClassesSettingsPanel p, Action<TextBox?> hook)
        {
            var duoHost = DuoMatrixHost(p);
            if (duoHost == null) return;
            if (duoHost.FindControl<TextBox>(DuoCellName(WeaponType.Mace, WeaponType.Sword, 1, 1)) != null)
                return;

            if (NameScope.GetNameScope(duoHost) == null)
                NameScope.SetNameScope(duoHost, new NameScope());

            foreach ((WeaponType low, WeaponType high) in EnumerateUnorderedDuoPairs())
                duoHost.Children.Add(BuildDuoUnorderedPairBlock(duoHost, low, high, hook));

            if (TrioRowsHost(p) is { } trioHost)
            {
                if (NameScope.GetNameScope(trioHost) == null)
                    NameScope.SetNameScope(trioHost, new NameScope());
                foreach (WeaponType[] triple in EnumerateTrioWeaponSets())
                {
                    trioHost.Children.Add(BuildTrioRow(trioHost, triple, hook));
                }
            }
        }

        private static Control BuildDuoUnorderedPairBlock(StackPanel duoNameHost, WeaponType low, WeaponType high, Action<TextBox?> hook)
        {
            var outer = new StackPanel { Spacing = 6, Margin = new global::Avalonia.Thickness(0, 0, 0, 4) };
            var title = new TextBlock
            {
                FontWeight = FontWeight.SemiBold,
                Foreground = HybridLabelBrush,
                TextWrapping = global::Avalonia.Media.TextWrapping.Wrap
            };
            RegisterOnHybridHost(duoNameHost, title, DuoPairTitleName(low, high));
            outer.Children.Add(title);

            var grid = new Grid { UseLayoutRounding = true };
            for (int i = 0; i < HybridDuoMatrixBandSlots + 1; i++)
            {
                grid.ColumnDefinitions.Add(i == 0
                    ? new ColumnDefinition(new GridLength(118))
                    : new ColumnDefinition(new GridLength(1, GridUnitType.Star)));
            }
            for (int i = 0; i < HybridDuoMatrixBandSlots + 1; i++)
                grid.RowDefinitions.Add(new RowDefinition(GridLength.Auto));

            var corner = new TextBlock
            {
                FontSize = 10,
                Foreground = HybridLabelBrush,
                VerticalAlignment = VerticalAlignment.Center,
                TextWrapping = global::Avalonia.Media.TextWrapping.Wrap,
                Text = " "
            };
            RegisterOnHybridHost(duoNameHost, corner, DuoCornerName(low, high));
            Grid.SetRow(corner, 0);
            Grid.SetColumn(corner, 0);
            grid.Children.Add(corner);

            for (int sb = 1; sb <= HybridDuoMatrixBandSlots; sb++)
            {
                var colHead = new TextBlock
                {
                    FontSize = 10,
                    Foreground = HybridLabelBrush,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    TextWrapping = global::Avalonia.Media.TextWrapping.Wrap,
                    Text = " "
                };
                RegisterOnHybridHost(duoNameHost, colHead, DuoColHeadName(low, high, sb));
                Grid.SetRow(colHead, 0);
                Grid.SetColumn(colHead, sb);
                grid.Children.Add(colHead);
            }

            for (int pb = 1; pb <= HybridDuoMatrixBandSlots; pb++)
            {
                var rowHead = new TextBlock
                {
                    FontSize = 10,
                    Foreground = HybridLabelBrush,
                    VerticalAlignment = VerticalAlignment.Center,
                    TextWrapping = global::Avalonia.Media.TextWrapping.Wrap,
                    Text = " "
                };
                RegisterOnHybridHost(duoNameHost, rowHead, DuoRowHeadName(low, high, pb));
                Grid.SetRow(rowHead, pb);
                Grid.SetColumn(rowHead, 0);
                grid.Children.Add(rowHead);

                for (int sb = 1; sb <= HybridDuoMatrixBandSlots; sb++)
                {
                    var tb = new TextBox
                    {
                        Watermark = "—",
                        MinWidth = 52,
                        Foreground = HybridMatrixInputForeground,
                        CaretBrush = Brushes.White,
                        SelectionBrush = new SolidColorBrush(global::Avalonia.Media.Color.Parse("#4a6fa8"))
                    };
                    RegisterOnHybridHost(duoNameHost, tb, DuoCellName(low, high, pb, sb));
                    string lowDisp = GameConfiguration.Instance.ClassPresentation.GetDisplayName(low);
                    string highDisp = GameConfiguration.Instance.ClassPresentation.GetDisplayName(high);
                    ToolTip.SetTip(tb, $"{lowDisp}({pb}) + {highDisp}({sb}) — order in play is by points; use | for alternatives");
                    Grid.SetRow(tb, pb);
                    Grid.SetColumn(tb, sb);
                    grid.Children.Add(tb);
                    hook(tb);
                }
            }

            outer.Children.Add(grid);
            return new Border
            {
                BorderBrush = new SolidColorBrush(global::Avalonia.Media.Color.Parse("#404040")),
                BorderThickness = new global::Avalonia.Thickness(1),
                CornerRadius = new global::Avalonia.CornerRadius(4),
                Padding = new global::Avalonia.Thickness(10),
                Child = outer
            };
        }

        private static Control BuildTrioRow(StackPanel trioNameHost, WeaponType[] triple, Action<TextBox?> hook)
        {
            string suffix = TrioRowSuffix(triple);
            var grid = new Grid();
            grid.ColumnDefinitions.Add(new ColumnDefinition(new GridLength(1, GridUnitType.Star)));
            grid.ColumnDefinitions.Add(new ColumnDefinition(new GridLength(2, GridUnitType.Star)));
            var lbl = new TextBlock
            {
                VerticalAlignment = VerticalAlignment.Center,
                Foreground = HybridLabelBrush,
                TextWrapping = global::Avalonia.Media.TextWrapping.Wrap,
                Margin = new global::Avalonia.Thickness(0, 0, 8, 0)
            };
            RegisterOnHybridHost(trioNameHost, lbl, $"HybridTrioLabel_{suffix}");
            Grid.SetColumn(lbl, 0);
            var tb = new TextBox
            {
                Foreground = HybridMatrixInputForeground,
                CaretBrush = Brushes.White,
                SelectionBrush = new SolidColorBrush(global::Avalonia.Media.Color.Parse("#4a6fa8"))
            };
            RegisterOnHybridHost(trioNameHost, tb, $"HybridTrio_{suffix}");
            Grid.SetColumn(tb, 1);
            hook(tb);
            grid.Children.Add(lbl);
            grid.Children.Add(tb);
            return grid;
        }

        private static void UpdateHybridDynamicLabels(ClassesSettingsPanel p)
        {
            ClassPresentationConfig cfg;
            try
            {
                cfg = ReadFromPanel(p).EnsureNormalized();
            }
            catch
            {
                cfg = GameConfiguration.Instance.ClassPresentation.EnsureNormalized();
                cfg = new ClassPresentationConfig
                {
                    MaceClassDisplayName = T(p, "MaceClassTextBox"),
                    SwordClassDisplayName = T(p, "SwordClassTextBox"),
                    DaggerClassDisplayName = T(p, "DaggerClassTextBox"),
                    WandClassDisplayName = T(p, "WandClassTextBox"),
                    DefaultNoPointsClassName = cfg.DefaultNoPointsClassName,
                    PreTierLabel = cfg.PreTierLabel,
                    HybridJoiner = cfg.HybridJoiner,
                    TierNames = cfg.TierNames,
                    TierThresholds = cfg.TierThresholds,
                    MeaningfulAttributeMinimum = cfg.MeaningfulAttributeMinimum
                }.EnsureNormalized();
            }

            var duoHost = DuoMatrixHost(p);
            if (duoHost != null)
            {
                foreach ((WeaponType low, WeaponType high) in EnumerateUnorderedDuoPairs())
                {
                    if (duoHost.FindControl<TextBlock>(DuoPairTitleName(low, high)) is not { } tb) continue;
                    string ln = cfg.GetDisplayName(low);
                    string hn = cfg.GetDisplayName(high);
                    tb.Text = $"{ln} + {hn}";
                    if (duoHost.FindControl<TextBlock>(DuoCornerName(low, high)) is { } corner)
                        corner.Text = $"{ln} (rows)\n{hn} (cols)";
                    for (int sb = 1; sb <= HybridDuoMatrixBandSlots; sb++)
                    {
                        if (duoHost.FindControl<TextBlock>(DuoColHeadName(low, high, sb)) is { } ch)
                            ch.Text = $"{hn} ({sb})";
                    }
                    for (int pb = 1; pb <= HybridDuoMatrixBandSlots; pb++)
                    {
                        if (duoHost.FindControl<TextBlock>(DuoRowHeadName(low, high, pb)) is { } rh)
                            rh.Text = $"{ln} ({pb})";
                    }
                }
            }

            if (TrioRowsHost(p) is { } trioHost)
            {
                foreach (WeaponType[] triple in EnumerateTrioWeaponSets())
                {
                    string suffix = TrioRowSuffix(triple);
                    if (trioHost.FindControl<TextBlock>($"HybridTrioLabel_{suffix}") is not { } lbl) continue;
                    lbl.Text = string.Join(" + ", triple.Select(w => cfg.GetDisplayName(w)));
                }
            }
        }

        private static void LoadHybridDuoMatrix(ClassesSettingsPanel p, List<HybridDuoTierRule> rules)
        {
            var duoHost = DuoMatrixHost(p);
            if (duoHost == null) return;
            foreach ((WeaponType low, WeaponType high) in EnumerateUnorderedDuoPairs())
            {
                for (int bandLow = 1; bandLow <= HybridDuoMatrixBandSlots; bandLow++)
                {
                    for (int bandHigh = 1; bandHigh <= HybridDuoMatrixBandSlots; bandHigh++)
                    {
                        string name = DuoCellName(low, high, bandLow, bandHigh);
                        if (duoHost.FindControl<TextBox>(name) is not { } tb) continue;
                        string text = PickDuoCellTextFromRules(rules, low, high, bandLow, bandHigh);
                        tb.Text = text;
                    }
                }
            }
        }

        /// <summary>Rows/columns follow weapon order (Mace…Wand); rules may be stored in either play order.</summary>
        private static string PickDuoCellTextFromRules(
            List<HybridDuoTierRule> rules,
            WeaponType low,
            WeaponType high,
            int bandLow,
            int bandHigh)
        {
            string lowS = low.ToString();
            string highS = high.ToString();
            foreach (var r in rules)
            {
                if (r.Titles == null || r.Titles.Length == 0) continue;
                if (string.Equals(r.PrimaryPath, lowS, StringComparison.OrdinalIgnoreCase)
                    && string.Equals(r.SecondaryPath, highS, StringComparison.OrdinalIgnoreCase)
                    && r.PrimaryTierBand == bandLow
                    && r.SecondaryTierBand == bandHigh)
                {
                    return string.Join("|", r.Titles.Where(x => !string.IsNullOrWhiteSpace(x)).Select(x => x.Trim()));
                }
            }
            foreach (var r in rules)
            {
                if (r.Titles == null || r.Titles.Length == 0) continue;
                if (string.Equals(r.PrimaryPath, highS, StringComparison.OrdinalIgnoreCase)
                    && string.Equals(r.SecondaryPath, lowS, StringComparison.OrdinalIgnoreCase)
                    && r.PrimaryTierBand == bandHigh
                    && r.SecondaryTierBand == bandLow)
                {
                    return string.Join("|", r.Titles.Where(x => !string.IsNullOrWhiteSpace(x)).Select(x => x.Trim()));
                }
            }
            return "";
        }

        private static void LoadHybridTrioRows(ClassesSettingsPanel p, List<HybridPathComboRule> rules)
        {
            foreach (WeaponType[] triple in EnumerateTrioWeaponSets())
            {
                string suffix = TrioRowSuffix(triple);
                string key = ClassPresentationConfig.CanonicalPathKey(triple);
                var match = rules.FirstOrDefault(r =>
                {
                    if (r.Paths == null || r.Paths.Length != 3) return false;
                    var wts = new List<WeaponType>();
                    foreach (var s in r.Paths)
                    {
                        if (!HybridTitleRuleText.TryParseWeapon(s, out var w)) return false;
                        wts.Add(w);
                    }
                    return ClassPresentationConfig.CanonicalPathKey(wts) == key;
                });
                string text = "";
                if (match?.Titles is { Length: > 0 } t)
                    text = string.Join("|", t.Where(x => !string.IsNullOrWhiteSpace(x)).Select(x => x.Trim()));
                if (TrioRowsHost(p)?.FindControl<TextBox>($"HybridTrio_{suffix}") is { } trioTb)
                    trioTb.Text = text;
            }
        }

        private static List<HybridDuoTierRule> ReadHybridDuoFromMatrix(ClassesSettingsPanel p)
        {
            var list = new List<HybridDuoTierRule>();
            foreach ((WeaponType low, WeaponType high) in EnumerateUnorderedDuoPairs())
            {
                for (int bandLow = 1; bandLow <= HybridDuoMatrixBandSlots; bandLow++)
                {
                    for (int bandHigh = 1; bandHigh <= HybridDuoMatrixBandSlots; bandHigh++)
                    {
                        string cell = THybrid(p, DuoCellName(low, high, bandLow, bandHigh));
                        if (cell.Length == 0) continue;
                        var titles = cell.Split('|', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
                        if (titles.Length == 0) continue;
                        list.Add(new HybridDuoTierRule
                        {
                            PrimaryPath = low.ToString(),
                            SecondaryPath = high.ToString(),
                            PrimaryTierBand = bandLow,
                            SecondaryTierBand = bandHigh,
                            Titles = titles
                        });
                        list.Add(new HybridDuoTierRule
                        {
                            PrimaryPath = high.ToString(),
                            SecondaryPath = low.ToString(),
                            PrimaryTierBand = bandHigh,
                            SecondaryTierBand = bandLow,
                            Titles = titles
                        });
                    }
                }
            }
            return list;
        }

        private static List<HybridPathComboRule> ReadHybridTrioFromRows(ClassesSettingsPanel p)
        {
            var list = new List<HybridPathComboRule>();
            foreach (WeaponType[] triple in EnumerateTrioWeaponSets())
            {
                string suffix = TrioRowSuffix(triple);
                string raw = THybrid(p, $"HybridTrio_{suffix}");
                if (raw.Length == 0) continue;
                var titles = raw.Split('|', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
                if (titles.Length == 0) continue;
                list.Add(new HybridPathComboRule
                {
                    Paths = triple.Select(w => w.ToString()).OrderBy(x => x, StringComparer.Ordinal).ToArray(),
                    Titles = titles
                });
            }
            return list;
        }

        private static string[] ParseQuadFromPanel(string? text)
        {
            if (string.IsNullOrWhiteSpace(text)) return Array.Empty<string>();
            return text.Split('|', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
        }

        private static string FormatQuadForPanel(string[]? titles)
        {
            if (titles == null || titles.Length == 0) return "";
            return string.Join("|", titles.Where(x => !string.IsNullOrWhiteSpace(x)).Select(x => x.Trim()));
        }

        private static IEnumerable<WeaponType[]> EnumerateTrioWeaponSets()
        {
            var w = ClassPresentationConfig.ClassWeaponOrder;
            yield return new[] { w[0], w[1], w[2] };
            yield return new[] { w[0], w[1], w[3] };
            yield return new[] { w[0], w[2], w[3] };
            yield return new[] { w[1], w[2], w[3] };
        }

        /// <summary>Weapon order Mace, Sword, Dagger, Wand — each pair once (rows = first path tiers, cols = second).</summary>
        private static IEnumerable<(WeaponType Low, WeaponType High)> EnumerateUnorderedDuoPairs()
        {
            var w = ClassPresentationConfig.ClassWeaponOrder;
            for (int i = 0; i < w.Length; i++)
            {
                for (int j = i + 1; j < w.Length; j++)
                    yield return (w[i], w[j]);
            }
        }

        private static string TrioRowSuffix(IReadOnlyList<WeaponType> triple) =>
            string.Join("_", triple.Select(x => x.ToString()).OrderBy(s => s, StringComparer.Ordinal));

        /// <param name="low">Earlier in <see cref="ClassPresentationConfig.ClassWeaponOrder"/> than <paramref name="high"/>.</param>
        private static string DuoCellName(WeaponType low, WeaponType high, int bandLow1To4, int bandHigh1To4) =>
            $"HybridDuo_{low}_{high}_{bandLow1To4}_{bandHigh1To4}";

        private static string DuoPairTitleName(WeaponType primary, WeaponType secondary) =>
            $"HybridDuoPairTitle_{primary}_{secondary}";

        private static string DuoCornerName(WeaponType primary, WeaponType secondary) =>
            $"HybridDuoCorner_{primary}_{secondary}";

        private static string DuoColHeadName(WeaponType primary, WeaponType secondary, int secondaryBand1To4) =>
            $"HybridDuoCol_{primary}_{secondary}_{secondaryBand1To4}";

        private static string DuoRowHeadName(WeaponType primary, WeaponType secondary, int primaryBand1To4) =>
            $"HybridDuoRow_{primary}_{secondary}_{primaryBand1To4}";
    }
}
