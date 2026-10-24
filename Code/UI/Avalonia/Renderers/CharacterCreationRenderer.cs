using Avalonia.Media;
using RPGGame.UI;
using RPGGame.UI.Avalonia.Layout;
using RPGGame.UI.Avalonia.Managers;
using RPGGame.UI.ColorSystem;
using RPGGame.UI.ColorSystem.Applications;
using System;
using System.Collections.Generic;

namespace RPGGame.UI.Avalonia.Renderers
{
    /// <summary>
    /// Specialized renderer for character creation screens
    /// </summary>
    public class CharacterCreationRenderer
    {
        private readonly GameCanvasControl canvas;
        private readonly ICanvasTextManager textManager;
        private readonly ICanvasInteractionManager interactionManager;
        
        public CharacterCreationRenderer(GameCanvasControl canvas, ICanvasTextManager textManager, ICanvasInteractionManager interactionManager)
        {
            this.canvas = canvas;
            this.textManager = textManager;
            this.interactionManager = interactionManager;
        }
        
        /// <summary>
        /// Renders the character creation screen full-screen (no side panels), every line centered.
        /// </summary>
        public void RenderCharacterCreation(Character character, CanvasContext context)
        {
            interactionManager.ClearClickableElements();
            canvas.Clear();

            int centerX = canvas.CenterX;
            var equipment = new List<string>
            {
                character.Weapon != null ? ItemDisplayFormatter.GetColoredItemName(character.Weapon) : "Bare Fists"
            };
            foreach (var item in new[] { character.Head, character.Body, character.Legs, character.Feet })
            {
                if (item != null)
                    equipment.Add(ItemDisplayFormatter.GetColoredItemName(item));
            }

            // Header, welcome (4), adventure line (3), equipment header + items (3), button
            int blockHeight = 1 + 4 + 3 + 1 + equipment.Count + 3 + 1;
            int y = Math.Max(1, (LayoutConstants.SCREEN_HEIGHT - blockHeight) / 2);

            string header = AsciiArtAssets.UIText.CreateHeader(UIConstants.Headers.YourHeroHasBeenCreated);
            canvas.AddText(CenteredX(centerX, header.Length), y, header, AsciiArtAssets.Colors.Gold);
            y += 3;

            // Render welcome message with character name in Player color
            var welcomeSegments = new ColoredTextBuilder()
                .Add("Welcome, ", Colors.White)
                .Add(character.Name, ColorPalette.Player)
                .Add("!", Colors.White)
                .Build();
            textManager.WriteLineColoredSegments(welcomeSegments, CenteredX(centerX, ColoredTextRenderer.GetDisplayLength(welcomeSegments)), y);
            y += 2;
            const string adventure = "Your adventure in the dungeons begins now.";
            canvas.AddText(CenteredX(centerX, adventure.Length), y, adventure, AsciiArtAssets.Colors.White);
            y += 3;

            // Starting equipment summary
            const string equipmentHeader = "Starting Equipment:";
            canvas.AddText(CenteredX(centerX, equipmentHeader.Length), y, equipmentHeader, AsciiArtAssets.Colors.Gold);
            y++;
            foreach (var name in equipment)
            {
                string line = $"• {name}";
                textManager.WriteLineColored(line, CenteredX(centerX, ColoredTextRenderer.GetDisplayLength(ColoredTextParser.Parse(line))), y);
                y++;
            }
            y += 2;

            string buttonText = MenuOptionFormatter.Format(1, UIConstants.Messages.PressAnyButton);
            int buttonX = CenteredX(centerX, buttonText.Length);
            var startButton = interactionManager.CreateButton(buttonX, y, buttonText.Length, "1", buttonText);
            interactionManager.AddClickableElement(startButton);
            canvas.AddMenuOption(buttonX, y, 1, UIConstants.Messages.PressAnyButton, AsciiArtAssets.Colors.White, startButton.IsHovered);

            canvas.Refresh();
        }

        private static int CenteredX(int centerX, int length) => Math.Max(0, centerX - length / 2);
    }
}
