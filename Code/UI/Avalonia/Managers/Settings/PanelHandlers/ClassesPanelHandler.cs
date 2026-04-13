using System;
using Avalonia.Controls;
using Avalonia.Media;
using RPGGame;
using RPGGame.UI.Avalonia.Settings;

namespace RPGGame.UI.Avalonia.Managers.Settings.PanelHandlers
{
    public class ClassesPanelHandler : ISettingsPanelHandler
    {
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
            hook(p.FindControl<TextBox>("PreviewMaceClassPointsTextBox"));
            hook(p.FindControl<TextBox>("PreviewSwordClassPointsTextBox"));
            hook(p.FindControl<TextBox>("PreviewDaggerClassPointsTextBox"));
            hook(p.FindControl<TextBox>("PreviewWandClassPointsTextBox"));
            foreach (var name in new[]
                     {
                         "AttrSoloTrioPrefix1TextBox", "AttrSoloTrioPrefix2TextBox", "AttrSoloTrioPrefix3TextBox", "AttrSoloTrioPrefix4TextBox",
                         "AttrQuadTier1TextBox", "AttrQuadTier2TextBox", "AttrQuadTier3TextBox", "AttrQuadTier4TextBox",
                         "AttrModMaceTextBox", "AttrModSwordTextBox", "AttrModDaggerTextBox", "AttrModWandTextBox",
                         "AttrDuoMaceSwordTextBox", "AttrDuoMaceDaggerTextBox", "AttrDuoMaceWandTextBox",
                         "AttrDuoSwordDaggerTextBox", "AttrDuoSwordWandTextBox", "AttrDuoDaggerWandTextBox"
                     })
                hook(p.FindControl<TextBox>(name));
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
            Set(p, "PreviewMaceClassPointsTextBox", "0");
            Set(p, "PreviewSwordClassPointsTextBox", "0");
            Set(p, "PreviewDaggerClassPointsTextBox", "0");
            Set(p, "PreviewWandClassPointsTextBox", "0");
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

        private static ClassPresentationConfig ReadFromPanel(ClassesSettingsPanel p)
        {
            var cur = GameConfiguration.Instance.ClassPresentation?.EnsureNormalized()
                      ?? new ClassPresentationConfig().EnsureNormalized();
            if (!int.TryParse(T(p, "TierThreshold1TextBox"), out int t1)
                || !int.TryParse(T(p, "TierThreshold2TextBox"), out int t2)
                || !int.TryParse(T(p, "TierThreshold3TextBox"), out int t3)
                || !int.TryParse(T(p, "TierThreshold4TextBox"), out int t4))
                throw new FormatException("Tier thresholds must be integers.");
            return new ClassPresentationConfig
            {
                MaceClassDisplayName = T(p, "MaceClassTextBox"),
                SwordClassDisplayName = T(p, "SwordClassTextBox"),
                DaggerClassDisplayName = T(p, "DaggerClassTextBox"),
                WandClassDisplayName = T(p, "WandClassTextBox"),
                TierThresholds = new[] { t1, t2, t3, t4 },
                DefaultNoPointsClassName = T(p, "DefaultClassTextBox"),
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
                AttributeDuoDaggerWand = T(p, "AttrDuoDaggerWandTextBox")
            };
        }

        private static void RefreshSummary(ClassesSettingsPanel p)
        {
            UpdateEvolvedGridHeaders(p);
            RefreshAttributeClassPreview(p);
        }

        private static void RefreshAttributeClassPreview(ClassesSettingsPanel p)
        {
            if (p.FindControl<TextBlock>("AttributeClassPreviewTextBlock") is not { } tb) return;
            try
            {
                var cfg = ReadFromPanel(p).EnsureNormalized();
                int m = int.TryParse(T(p, "PreviewMaceClassPointsTextBox"), out int mb) ? Math.Max(0, mb) : 0;
                int w = int.TryParse(T(p, "PreviewSwordClassPointsTextBox"), out int ws) ? Math.Max(0, ws) : 0;
                int r = int.TryParse(T(p, "PreviewDaggerClassPointsTextBox"), out int rd) ? Math.Max(0, rd) : 0;
                int z = int.TryParse(T(p, "PreviewWandClassPointsTextBox"), out int zw) ? Math.Max(0, zw) : 0;
                var previewProg = new CharacterProgression();
                previewProg.BarbarianPoints = m;
                previewProg.WarriorPoints = w;
                previewProg.RoguePoints = r;
                previewProg.WizardPoints = z;
                string composed = AttributeClassNameComposer.ComposeDisplayClass(previewProg, cfg);
                tb.Text = $"Preview title: {composed}";
            }
            catch
            {
                tb.Text = "Preview title: (fix preview / threshold numeric fields)";
            }
        }
    }
}
