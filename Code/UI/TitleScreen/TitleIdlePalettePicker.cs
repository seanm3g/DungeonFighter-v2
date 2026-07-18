using System;
using System.Collections.Generic;
using System.Linq;
using Avalonia.Media;
using RPGGame.Data;
using RPGGame.UI.ColorSystem;
using RPGGame.Utils;

namespace RPGGame.UI.TitleScreen
{
    /// <summary>
    /// Chooses a multi-color sequence template for the title idle gradient each launch.
    /// </summary>
    public sealed class TitleIdlePalette
    {
        public string TemplateName { get; }
        public IReadOnlyList<string> DungeonColorCodes { get; }
        public IReadOnlyList<string> FighterColorCodes { get; }

        public TitleIdlePalette(
            string templateName,
            IReadOnlyList<string> dungeonColorCodes,
            IReadOnlyList<string> fighterColorCodes)
        {
            TemplateName = templateName ?? throw new ArgumentNullException(nameof(templateName));
            DungeonColorCodes = dungeonColorCodes ?? throw new ArgumentNullException(nameof(dungeonColorCodes));
            FighterColorCodes = fighterColorCodes ?? throw new ArgumentNullException(nameof(fighterColorCodes));
        }

        /// <summary>
        /// First DEMON palette color as RGB accent for complementary background derivation.
        /// </summary>
        public Color ResolveAccentColor()
        {
            string code = DungeonColorCodes.Count > 0 ? DungeonColorCodes[0] : "C";
            return ColorTemplateLibrary.ColorCodeToColor(code);
        }

        /// <summary>
        /// Bright complementary neon backdrop for the current palette accent.
        /// </summary>
        public Color ResolveComplementaryBackground()
        {
            return ColorValidator.ComplementaryVibrantBackground(ResolveAccentColor());
        }
    }

    /// <summary>
    /// Picks a random sequence/alternation template with at least two distinct color codes.
    /// Both title words use the same pattern; FIGHTER uses the color list reversed for contrast.
    /// </summary>
    public static class TitleIdlePalettePicker
    {
        public const string FallbackDungeonTemplate = "title_dungeon_yellow_orange";
        public const string FallbackFighterTemplate = "title_fighter_yellow_orange";
        public const string TitleNeonPrefix = "title_neon_";

        /// <summary>
        /// Picks a random idle palette from loaded color templates.
        /// Prefers <c>title_neon_*</c> templates when any are loaded.
        /// </summary>
        public static TitleIdlePalette PickRandom()
        {
            return PickRandom(EnumerateEligibleTemplates());
        }

        /// <summary>
        /// Picks a random palette, preferring a different template than <paramref name="excludeTemplateName"/>.
        /// </summary>
        public static TitleIdlePalette PickRandomExcept(string? excludeTemplateName)
        {
            return PickRandomExcept(EnumerateEligibleTemplates(), excludeTemplateName);
        }

        /// <summary>
        /// Picks from an injectable candidate list (for tests). Falls back when empty.
        /// </summary>
        public static TitleIdlePalette PickRandom(IEnumerable<ColorTemplateData> candidates)
        {
            var eligible = FilterEligible(candidates);
            if (eligible.Count == 0)
                return CreateFallback();

            var chosen = RandomUtility.NextElement(eligible);
            return FromTemplate(chosen);
        }

        /// <summary>
        /// Picks from candidates, excluding <paramref name="excludeTemplateName"/> when alternatives exist.
        /// </summary>
        public static TitleIdlePalette PickRandomExcept(
            IEnumerable<ColorTemplateData> candidates,
            string? excludeTemplateName)
        {
            var eligible = FilterEligible(candidates);
            if (eligible.Count == 0)
                return CreateFallback();

            var alternatives = string.IsNullOrWhiteSpace(excludeTemplateName)
                ? eligible
                : eligible
                    .Where(t => !string.Equals(t.Name, excludeTemplateName, StringComparison.OrdinalIgnoreCase))
                    .ToList();

            var pool = alternatives.Count > 0 ? alternatives : eligible;
            return FromTemplate(RandomUtility.NextElement(pool));
        }

        /// <summary>
        /// Builds a palette from a specific template (fighter colors reversed).
        /// </summary>
        public static TitleIdlePalette FromTemplate(ColorTemplateData template)
        {
            if (template == null)
                throw new ArgumentNullException(nameof(template));

            var colors = template.Colors?
                .Where(c => !string.IsNullOrWhiteSpace(c))
                .ToList() ?? new List<string>();

            if (colors.Count < 2 || colors.Distinct(StringComparer.OrdinalIgnoreCase).Count() < 2)
                return CreateFallback();

            var reversed = colors.AsEnumerable().Reverse().ToList();
            return new TitleIdlePalette(template.Name, colors, reversed);
        }

        /// <summary>
        /// Eligible title idle templates: prefers <c>title_neon_*</c> when present;
        /// otherwise any sequence/alternation with ≥2 distinct color codes.
        /// </summary>
        public static IReadOnlyList<ColorTemplateData> EnumerateEligibleTemplates()
        {
            ColorTemplateLoader.LoadColorTemplates();
            var structural = new List<ColorTemplateData>();

            foreach (var name in ColorTemplateLoader.GetAllTemplateNames())
            {
                var template = ColorTemplateLoader.GetTemplate(name);
                if (template != null && IsEligible(template))
                    structural.Add(template);
            }

            return PreferTitleNeon(structural);
        }

        /// <summary>
        /// When any <c>title_neon_*</c> templates exist in <paramref name="candidates"/>, only those are used.
        /// </summary>
        public static IReadOnlyList<ColorTemplateData> PreferTitleNeon(IEnumerable<ColorTemplateData> candidates)
        {
            var structural = (candidates ?? Enumerable.Empty<ColorTemplateData>())
                .Where(IsEligible)
                .ToList();
            var neon = structural.Where(IsTitleNeonTemplate).ToList();
            return neon.Count > 0 ? neon : structural;
        }

        public static bool IsTitleNeonTemplate(ColorTemplateData template)
        {
            return template != null
                && !string.IsNullOrWhiteSpace(template.Name)
                && template.Name.StartsWith(TitleNeonPrefix, StringComparison.OrdinalIgnoreCase);
        }

        public static bool IsEligible(ColorTemplateData template)
        {
            if (template == null || string.IsNullOrWhiteSpace(template.Name))
                return false;

            string shader = template.ShaderType?.Trim().ToLowerInvariant() ?? "sequence";
            if (shader != "sequence" && shader != "alternation")
                return false;

            var codes = template.Colors?
                .Where(c => !string.IsNullOrWhiteSpace(c))
                .ToList() ?? new List<string>();

            return codes.Count >= 2
                && codes.Distinct(StringComparer.OrdinalIgnoreCase).Count() >= 2;
        }

        private static List<ColorTemplateData> FilterEligible(IEnumerable<ColorTemplateData> candidates)
        {
            return PreferTitleNeon(candidates).ToList();
        }

        public static TitleIdlePalette CreateFallback()
        {
            // Prefer a neon template when available.
            var neon = ColorTemplateLoader.GetTemplate("title_neon_cyber");
            if (neon != null && IsEligible(neon))
                return FromTemplate(neon);

            var dungeon = ColorTemplateLoader.GetTemplate(FallbackDungeonTemplate);
            if (dungeon != null && dungeon.Colors != null && dungeon.Colors.Count >= 2)
            {
                var colors = dungeon.Colors.Where(c => !string.IsNullOrWhiteSpace(c)).ToList();
                return new TitleIdlePalette(FallbackDungeonTemplate, colors, colors.AsEnumerable().Reverse().ToList());
            }

            var fighter = ColorTemplateLoader.GetTemplate(FallbackFighterTemplate);
            if (fighter != null && fighter.Colors != null && fighter.Colors.Count >= 2)
            {
                var colors = fighter.Colors.Where(c => !string.IsNullOrWhiteSpace(c)).ToList();
                return new TitleIdlePalette(FallbackFighterTemplate, colors, colors.AsEnumerable().Reverse().ToList());
            }

            var defaultColors = new List<string> { "neon_cyan", "neon_magenta", "neon_pink", "neon_blue" };
            return new TitleIdlePalette(
                "title_neon_cyber",
                defaultColors,
                defaultColors.AsEnumerable().Reverse().ToList());
        }
    }
}
