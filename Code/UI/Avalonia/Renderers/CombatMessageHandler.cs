using RPGGame;
using RPGGame.UI.Avalonia.Managers;
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
            textManager.AddToDisplayBuffer($"&G{victoryMsg}", UIMessageType.Combat);
            textManager.AddToDisplayBuffer(AsciiArtAssets.UIText.Divider, UIMessageType.Combat);
            
            // Add battle narrative highlights if available
            if (battleNarrative != null)
            {
                var narrativeLines = battleNarrative.GetTriggeredNarratives();
                if (narrativeLines != null && narrativeLines.Count > 0)
                {
                    textManager.AddToDisplayBuffer("", UIMessageType.Combat);
                    textManager.AddToDisplayBuffer("&C" + AsciiArtAssets.UIText.CreateHeader(AsciiArtAssets.UIText.BattleHighlightsHeader), UIMessageType.Combat);
                    foreach (var line in narrativeLines.Take(3))
                    {
                        if (!string.IsNullOrWhiteSpace(line))
                        {
                            textManager.AddToDisplayBuffer($"&C  {line}", UIMessageType.Combat);
                        }
                    }
                }
            }
            
            textManager.AddToDisplayBuffer("", UIMessageType.Combat);
        }

        public void AddDefeatMessage()
        {
            textManager.AddToDisplayBuffer("", UIMessageType.Combat);
            textManager.AddToDisplayBuffer($"&R{AsciiArtAssets.UIText.DefeatMessage}", UIMessageType.Combat);
            textManager.AddToDisplayBuffer(AsciiArtAssets.UIText.Divider, UIMessageType.Combat);
            textManager.AddToDisplayBuffer("", UIMessageType.Combat);
        }

        public void AddRoomClearedMessage(Character? character)
        {
            textManager.AddToDisplayBuffer("", UIMessageType.Combat);
            textManager.AddToDisplayBuffer($"&G{AsciiArtAssets.UIText.RoomClearedMessage}", UIMessageType.Combat);
            textManager.AddToDisplayBuffer(AsciiArtAssets.UIText.Divider, UIMessageType.Combat);
            
            if (character != null)
            {
                string healthMsg = string.Format(AsciiArtAssets.UIText.RemainingHealth, 
                    character.CurrentHealth, character.GetEffectiveMaxHealth());
                textManager.AddToDisplayBuffer($"&W{healthMsg}", UIMessageType.Combat);
            }
            
            textManager.AddToDisplayBuffer("", UIMessageType.Combat);
        }
    }
}
