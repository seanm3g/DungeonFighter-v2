using RPGGame;
using RPGGame.UI.Avalonia.Managers;
using RPGGame.UI.ColorSystem;
using System.Collections.Generic;
using System.Linq;

namespace RPGGame.UI.Avalonia.Renderers
{
    /// <summary>
    /// Handles combat-related message display
    /// </summary>
    public class CombatMessageHandler
    {
        private readonly ICanvasTextManager textManager;

        public CombatMessageHandler(ICanvasTextManager textManager)
        {
            this.textManager = textManager;
        }

        public void AddVictoryMessage(Enemy enemy, BattleNarrative? battleNarrative)
        {
            // Add blank line for separation
            textManager.AddToDisplayBuffer("", UIMessageType.Combat);
            
            // Add victory message
            string victoryMsg = string.Format(AsciiArtAssets.UIText.VictoryPrefix, enemy.Name);
            var victorySegments = new ColoredTextBuilder()
                .Add(victoryMsg, ColorPalette.Success)
                .Build();
            if (textManager is CanvasTextManager canvasTextManager)
            {
                canvasTextManager.DisplayManager.AddMessage(victorySegments, UIMessageType.Combat);
            }
            else
            {
                textManager.AddToDisplayBuffer(victoryMsg, UIMessageType.Combat);
            }
            textManager.AddToDisplayBuffer(AsciiArtAssets.UIText.Divider, UIMessageType.Combat);
            
            // Add battle narrative highlights if available
            if (battleNarrative != null)
            {
                var narrativeLines = battleNarrative.GetTriggeredNarratives();
                if (narrativeLines != null && narrativeLines.Count > 0)
                {
                    textManager.AddToDisplayBuffer("", UIMessageType.Combat);
                    var headerSegments = new ColoredTextBuilder()
                        .Add(AsciiArtAssets.UIText.CreateHeader(AsciiArtAssets.UIText.BattleHighlightsHeader), ColorPalette.Info)
                        .Build();
                    if (textManager is CanvasTextManager canvasTextManager2)
                    {
                        canvasTextManager2.DisplayManager.AddMessage(headerSegments, UIMessageType.Combat);
                    }
                    else
                    {
                        textManager.AddToDisplayBuffer(AsciiArtAssets.UIText.CreateHeader(AsciiArtAssets.UIText.BattleHighlightsHeader), UIMessageType.Combat);
                    }
                    foreach (var line in narrativeLines.Take(3))
                    {
                        if (!string.IsNullOrWhiteSpace(line))
                        {
                            var lineSegments = new ColoredTextBuilder()
                                .Add("  ", ColorPalette.Info)
                                .Add(line, ColorPalette.Info)
                                .Build();
                            if (textManager is CanvasTextManager canvasTextManager3)
                            {
                                canvasTextManager3.DisplayManager.AddMessage(lineSegments, UIMessageType.Combat);
                            }
                            else
                            {
                                textManager.AddToDisplayBuffer($"  {line}", UIMessageType.Combat);
                            }
                        }
                    }
                }
            }
            
            textManager.AddToDisplayBuffer("", UIMessageType.Combat);
        }

        public void AddDefeatMessage()
        {
            textManager.AddToDisplayBuffer("", UIMessageType.Combat);
            var defeatSegments = new ColoredTextBuilder()
                .Add(AsciiArtAssets.UIText.DefeatMessage, ColorPalette.Damage)
                .Build();
            if (textManager is CanvasTextManager canvasTextManager)
            {
                canvasTextManager.DisplayManager.AddMessage(defeatSegments, UIMessageType.Combat);
            }
            else
            {
                textManager.AddToDisplayBuffer(AsciiArtAssets.UIText.DefeatMessage, UIMessageType.Combat);
            }
            textManager.AddToDisplayBuffer(AsciiArtAssets.UIText.Divider, UIMessageType.Combat);
            textManager.AddToDisplayBuffer("", UIMessageType.Combat);
        }

        public void AddRoomClearedMessage(Character? character)
        {
            // Use TextSpacingSystem to apply proper spacing before room cleared message
            int spacingBefore = TextSpacingSystem.GetSpacingBefore(TextSpacingSystem.BlockType.RoomCleared);
            for (int i = 0; i < spacingBefore; i++)
            {
                textManager.AddToDisplayBuffer("", UIMessageType.Combat);
            }
            
            var roomClearedSegments = new ColoredTextBuilder()
                .Add(AsciiArtAssets.UIText.RoomClearedMessage, ColorPalette.Success)
                .Build();
            if (textManager is CanvasTextManager canvasTextManager)
            {
                canvasTextManager.DisplayManager.AddMessage(roomClearedSegments, UIMessageType.Combat);
            }
            else
            {
                textManager.AddToDisplayBuffer(AsciiArtAssets.UIText.RoomClearedMessage, UIMessageType.Combat);
            }
            // Add blank line before divider
            textManager.AddToDisplayBuffer("", UIMessageType.Combat);
            textManager.AddToDisplayBuffer(AsciiArtAssets.UIText.Divider, UIMessageType.Combat);
            
            // Record that room cleared was displayed
            TextSpacingSystem.RecordBlockDisplayed(TextSpacingSystem.BlockType.RoomCleared);
        }
    }
}
