using System;
using System.Collections.Generic;
using System.Linq;
using Avalonia.Media;
using RPGGame;
using RPGGame.Data;
using RPGGame.UI;
using RPGGame.UI.Avalonia;
using RPGGame.UI.Avalonia.Managers;
using RPGGame.UI.ColorSystem.Themes;

namespace RPGGame.UI.Avalonia.Renderers.Menu
{
    /// <summary>
    /// Renders the starter weapon screen as a 2x2 grid of class cards: weapon art, the class name in large
    /// letter-spaced glowing type, the starter weapon with its stats, and the actions the weapon starts with.
    /// Each card is one click target (value = menu number); hover comes from <see cref="FighterRosterHoverState"/>.
    /// </summary>
    public class WeaponSelectionRenderer
    {
        private const int MaxCardWidth = 58;
        private const int MaxCardHeight = 24;
        private const int CardGapX = 4;
        private const int CardGapY = 1;
        private const int HeaderRows = 4;
        private const int FooterRows = 2;

        private readonly GameCanvasControl canvas;
        private readonly List<ClickableElement> clickableElements;

        public WeaponSelectionRenderer(GameCanvasControl canvas, List<ClickableElement> clickableElements, ICanvasInteractionManager interactionManager, ICanvasTextManager textManager)
        {
            this.canvas = canvas;
            this.clickableElements = clickableElements;
        }

        public int RenderWeaponSelectionContent(int x, int y, int width, int height, List<StartingWeapon> weapons)
        {
            if (weapons == null || weapons.Count == 0)
            {
                canvas.AddText(MenuLayoutCalculator.CalculateCenteredTextX(x, width, 20), y + height / 2, "No weapons available", AsciiArtAssets.Colors.Red);
                return 1;
            }

            int columns = Math.Min(2, weapons.Count);
            int rows = (weapons.Count + columns - 1) / columns;
            int cardW = Math.Max(30, Math.Min(MaxCardWidth, (width - 4 - CardGapX * (columns - 1)) / columns));
            int cardH = Math.Max(12, Math.Min(MaxCardHeight, (height - HeaderRows - FooterRows - CardGapY * (rows - 1)) / rows));
            int gridW = cardW * columns + CardGapX * (columns - 1);
            int gridH = cardH * rows + CardGapY * (rows - 1);
            int blockH = HeaderRows + gridH + FooterRows;
            int top = y + Math.Max(0, (height - blockH) / 2);
            int gridX = MenuLayoutCalculator.CalculateCenteredTextX(x, width, gridW);

            // Header
            var gold = AsciiArtAssets.Colors.Gold;
            string title = "C H O O S E   Y O U R   P A T H";
            string rule = "-=-=-=-=-=-=-=-=-=  ◆  =-=-=-=-=-=-=-=-=-";
            canvas.AddText(MenuLayoutCalculator.CalculateCenteredTextX(x, width, title.Length), top, title, gold, gold, 0.35, 2);
            canvas.AddText(MenuLayoutCalculator.CalculateCenteredTextX(x, width, rule.Length), top + 1, rule, AsciiArtAssets.Colors.DarkGray);
            string sub = "Your starting weapon decides your class.";
            canvas.AddText(MenuLayoutCalculator.CalculateCenteredTextX(x, width, sub.Length), top + 2, sub, AsciiArtAssets.Colors.Silver);

            var classPresentation = GameConfiguration.Instance.ClassPresentation.EnsureNormalized();
            int gridTop = top + HeaderRows;
            for (int i = 0; i < weapons.Count; i++)
            {
                int col = i % columns;
                int row = i / columns;
                int cx = gridX + col * (cardW + CardGapX);
                int cy = gridTop + row * (cardH + CardGapY);
                WeaponItem preview = GameInitializer.CreateStarterWeaponForMenuIndex(i + 1);
                DrawCard(cx, cy, cardW, cardH, i + 1, preview, classPresentation.GetDisplayName(preview.WeaponType));
            }

            string footer = UIConstants.Messages.PressNumberOrClick;
            canvas.AddText(MenuLayoutCalculator.CalculateCenteredTextX(x, width, footer.Length), gridTop + gridH + 1, footer, AsciiArtAssets.Colors.Cyan);
            return blockH;
        }

        private void DrawCard(int cx, int cy, int cw, int ch, int number, WeaponItem preview, string className)
        {
            var element = new ClickableElement
            {
                X = cx,
                Y = cy,
                Width = cw,
                Height = ch,
                Type = ElementType.MenuOption,
                Value = number.ToString(),
                DisplayText = $"[{number}] {className}"
            };
            element.IsHovered = FighterRosterHoverState.IsOver(element);
            clickableElements.Add(element);

            bool hovered = element.IsHovered;
            Color accent = WeaponMenuAccentColor(preview.WeaponType);
            Color frame = hovered ? AsciiArtAssets.Colors.White : accent;
            var gray = AsciiArtAssets.Colors.Gray;
            var white = AsciiArtAssets.Colors.White;

            canvas.AddBorder(cx, cy, cw, ch, frame, hovered ? 3 : 2);
            int innerX = cx + 2;
            int innerW = cw - 4;
            int bottom = cy + ch - 2; // last usable row inside the frame

            canvas.AddText(innerX, cy + 1, $"[{number}]", hovered ? white : accent);

            // Weapon art, centered as one block so its lines stay aligned.
            int row = cy + 1;
            string[] art = WeaponArt(preview.WeaponType);
            int artW = art.Max(l => l.Length);
            int artX = cx + Math.Max(1, (cw - artW) / 2);
            foreach (string line in art)
                canvas.AddText(artX, row++, line, accent);
            row++;

            // Class name: letter-spaced capitals with glow, framed by ornaments.
            string fancy = $"~ {Spaced(className)} ~";
            Centered(cx, cw, row++, fancy, hovered ? white : accent, glow: accent);
            string flourish = "-=≡  ◆  ≡=-";
            Centered(cx, cw, row++, flourish, AsciiArtAssets.Colors.DarkGray);
            row++;

            // Starter weapon and its numbers.
            string weaponName = string.IsNullOrWhiteSpace(preview.Name) ? $"Weapon {number}" : preview.Name;
            Centered(cx, cw, row++, weaponName, GetWeaponNameColor(preview, hovered));
            Centered(cx, cw, row++, StatLine(preview), AsciiArtAssets.Colors.Silver);
            row++;

            // Default actions the weapon brings.
            var actions = StartingActions(preview.WeaponType);
            if (row <= bottom - 1 && actions.Count > 0)
            {
                canvas.AddText(innerX, row++, "Starting actions", AsciiArtAssets.Colors.Gold);
                // Descriptions only when every action gets one; otherwise names alone keep the list even.
                bool withDescriptions = actions.Count * 2 <= bottom - row + 1;
                foreach (var (name, description) in actions)
                {
                    if (row > bottom)
                        break;
                    canvas.AddText(innerX, row, "◆", accent);
                    canvas.AddText(innerX + 2, row++, Fit(name.ToUpperInvariant(), innerW - 2), white);
                    if (withDescriptions && !string.IsNullOrWhiteSpace(description))
                        canvas.AddText(innerX + 2, row++, Fit(description.Trim(), innerW - 2), gray);
                }
            }
        }

        /// <summary>Starter actions for a weapon type with their descriptions (names from the startingWeapon tag).</summary>
        internal static List<(string Name, string Description)> StartingActions(WeaponType weaponType)
        {
            var result = new List<(string, string)>();
            try
            {
                var names = new LootActionSelector(RandomUtility.Ambient).GetStartingWeaponActions(weaponType.ToString());
                var data = ActionLoader.GetActiveSetActionData();
                foreach (string name in names)
                {
                    var d = data.FirstOrDefault(a => string.Equals(a.Name, name, StringComparison.OrdinalIgnoreCase));
                    result.Add((name, d?.Description ?? ""));
                }
            }
            catch
            {
                // Action data missing: the card just shows no action list.
            }
            return result;
        }

        internal static string StatLine(WeaponItem preview)
        {
            // No hero yet: show the swing at base agility (base attack time × weapon), in seconds.
            double baseSwing = GameConfiguration.Instance.Combat.BaseAttackTime * preview.GetTotalAttackSpeed();
            string line = $"Damage {preview.GetTotalDamage()}  │  Attack {baseSwing:F1}s";
            int slots = ClassPresentationConfig.GetEquippedWeaponComboSlotBonus(preview.WeaponType);
            if (slots > 0)
                line += $"  │  +{slots} action slot{(slots == 1 ? "" : "s")}";
            return line;
        }

        internal static string Spaced(string text) =>
            string.Join(" ", (text ?? "").ToUpperInvariant().ToCharArray());

        internal static string[] WeaponArt(WeaponType weaponType) => weaponType switch
        {
            WeaponType.Mace => new[]
            {
                "              .-^-.",
                "  ============|(*)|>",
                "              '-v-'"
            },
            WeaponType.Sword => new[]
            {
                "     |\\",
                "  o==|]::::::::::::::::>",
                "     |/"
            },
            WeaponType.Dagger => new[]
            {
                "       ,",
                "  ==o{]:::::::>",
                "       '"
            },
            WeaponType.Wand => new[]
            {
                "                 \\ | /",
                "  ===============- * -",
                "                 / | \\"
            },
            _ => new[] { "", "  ----------->", "" }
        };

        private void Centered(int cx, int cw, int row, string text, Color color, Color? glow = null)
        {
            text = Fit(text, cw - 4);
            int tx = cx + Math.Max(2, (cw - text.Length) / 2);
            if (glow.HasValue)
                canvas.AddText(tx, row, text, color, glow.Value, 0.45, 3);
            else
                canvas.AddText(tx, row, text, color);
        }

        private static string Fit(string text, int max)
        {
            if (max <= 1)
                return "";
            return text.Length <= max ? text : text.Substring(0, max - 1) + ".";
        }

        internal static Color GetWeaponNameColor(WeaponItem preview, bool isHovered)
        {
            // Weapon names should read as "gear text": use rarity coloring (Common = white),
            // and keep hover from overriding that rarity signal.
            string rarity = preview?.Rarity?.Trim() ?? "Common";
            return ItemThemeProvider.GetRarityColor(rarity);
        }

        private static Color WeaponMenuAccentColor(WeaponType weaponType) => weaponType switch
        {
            WeaponType.Mace => AsciiArtAssets.Colors.Red,
            WeaponType.Sword => AsciiArtAssets.Colors.Blue,
            WeaponType.Dagger => AsciiArtAssets.Colors.Green,
            WeaponType.Wand => AsciiArtAssets.Colors.Purple,
            _ => AsciiArtAssets.Colors.Gray
        };
    }
}
