using Avalonia.Media;
using RPGGame.UI;
using RPGGame.UI.Avalonia.Managers;
using System;

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
        /// Renders the character creation screen
        /// </summary>
        public void RenderCharacterCreation(Character character, CanvasContext context)
        {
            RenderWithLayout(character, "CHARACTER CREATION", (contentX, contentY, contentWidth, contentHeight) =>
            {
                RenderCharacterCreationContent(contentX, contentY, contentWidth, contentHeight, character);
            }, context);
        }
        
        /// <summary>
        /// Renders the character creation content with welcome message and starting equipment
        /// </summary>
        private void RenderCharacterCreationContent(int x, int y, int width, int height, Character character)
        {
            int startY = y; // Store initial Y position for bottom calculations
            int centerY = y + (height / 2) - 8;
            
            // Welcome message
            canvas.AddText(x + (width / 2) - 20, centerY, "═══ YOUR HERO HAS BEEN CREATED! ═══", AsciiArtAssets.Colors.Gold);
            centerY += 3;
            
            canvas.AddText(x + 4, centerY, $"Welcome, {character.Name}!", AsciiArtAssets.Colors.White);
            centerY += 2;
            canvas.AddText(x + 4, centerY, "Your adventure in the dungeons begins now.", AsciiArtAssets.Colors.White);
            centerY += 2;
            canvas.AddText(x + 4, centerY, "Check your stats and equipment on the left.", AsciiArtAssets.Colors.White);
            centerY += 3;
            
            // Starting equipment summary
            canvas.AddText(x + 4, centerY, "Starting Equipment:", AsciiArtAssets.Colors.Gold);
            centerY++;
            string weaponDisplay = character.Weapon != null ? ItemDisplayFormatter.GetColoredItemName(character.Weapon) : "Bare Fists";
            textManager.WriteLineColored($"• {weaponDisplay}", x + 6, centerY);
            centerY++;
            if (character.Head != null)
            {
                textManager.WriteLineColored($"• {ItemDisplayFormatter.GetColoredItemName(character.Head)}", x + 6, centerY);
                centerY++;
            }
            if (character.Body != null)
            {
                textManager.WriteLineColored($"• {ItemDisplayFormatter.GetColoredItemName(character.Body)}", x + 6, centerY);
                centerY++;
            }
            if (character.Feet != null)
            {
                textManager.WriteLineColored($"• {ItemDisplayFormatter.GetColoredItemName(character.Feet)}", x + 6, centerY);
                centerY++;
            }
            
            // Action buttons at bottom
            y = startY + height - 6;
            canvas.AddText(x + 2, y, "═══ ACTIONS ═══", AsciiArtAssets.Colors.Gold);
            y += 2;
            
            var startButton = interactionManager.CreateButton(x + 4, y, 20, "1", "[1] Start Adventure");
            interactionManager.AddClickableElement(startButton);
            
            canvas.AddMenuOption(x + 4, y, 1, "Start Adventure", AsciiArtAssets.Colors.White, startButton.IsHovered);
        }
        
        /// <summary>
        /// Helper method to render with layout context
        /// </summary>
        private void RenderWithLayout(Character character, string title, Action<int, int, int, int> renderContent, CanvasContext context)
        {
            interactionManager.ClearClickableElements();
            // Render content directly without layout manager
            renderContent(0, 0, (int)canvas.Width, (int)canvas.Height);
        }
    }
}
