using Avalonia.Media;
using RPGGame.UI;
using System;
using System.Collections.Generic;
using System.Linq;

namespace RPGGame.UI.Avalonia.Renderers
{
    /// <summary>
    /// Handles rendering of combat-related screens (combat log, combat results)
    /// </summary>
    public class CombatRenderer : IScreenRenderer
    {
        private readonly GameCanvasControl canvas;
        private readonly ColoredTextWriter textWriter;
        private int currentLineCount;
        
        public CombatRenderer(GameCanvasControl canvas, ColoredTextWriter textWriter)
        {
            this.canvas = canvas;
            this.textWriter = textWriter;
            this.currentLineCount = 0;
        }
        
        // IScreenRenderer implementation
        public void Render()
        {
            // This is a placeholder - specific render methods are called directly
            // Future refactor could use a state machine pattern here
        }
        
        public void Clear()
        {
            currentLineCount = 0;
        }
        
        public int GetLineCount()
        {
            return currentLineCount;
        }
        
        /// <summary>
        /// Renders the main combat screen with enemy info and combat log
        /// </summary>
        public void RenderCombat(int x, int y, int width, int height, Character player, Enemy enemy, List<string> combatLog)
        {
            currentLineCount = 0;
            int startY = y;
            
            // Enemy info section (name and level - health is in left panel)
            canvas.AddText(x + 2, y, AsciiArtAssets.UIText.CreateHeader(AsciiArtAssets.UIText.EnemyHeader), AsciiArtAssets.Colors.Gold);
            y += 2;
            currentLineCount += 2;
            
            canvas.AddText(x + 2, y, $"{AsciiArtAssets.CombatIcons.Enemy} {enemy.Name}", AsciiArtAssets.Colors.White);
            y++;
            currentLineCount++;
            canvas.AddText(x + 2, y, $"Level {enemy.Level}", AsciiArtAssets.Colors.White);
            y += 3;
            currentLineCount += 3;
            
            // Combat log section
            canvas.AddText(x + 2, y, AsciiArtAssets.UIText.CreateHeader(AsciiArtAssets.UIText.CombatLogHeader), AsciiArtAssets.Colors.Gold);
            y += 2;
            currentLineCount += 2;
            
            // Calculate available width for combat log
            int availableWidth = width - 4;
            
            foreach (var logEntry in combatLog.TakeLast(20))
            {
                if (y < startY + height - 8)
                {
                    // Render with color markup and text wrapping
                    int linesRendered = textWriter.WriteLineColoredWrapped(logEntry, x + 2, y, availableWidth);
                    y += linesRendered;
                    currentLineCount += linesRendered;
                }
            }
            
            // Actions at bottom
            y = startY + height - 6;
            canvas.AddText(x + 2, y, "═══ ACTIONS ═══", AsciiArtAssets.Colors.Gold);
            y += 2;
            currentLineCount += 2;
            
            canvas.AddMenuOption(x + 2, y, 1, "Attack", AsciiArtAssets.Colors.White);
            y++;
            currentLineCount++;
            canvas.AddMenuOption(x + 2, y, 2, "Use Item", AsciiArtAssets.Colors.White);
            y++;
            currentLineCount++;
            canvas.AddMenuOption(x + 2, y, 3, "Flee", AsciiArtAssets.Colors.White);
            currentLineCount++;
        }
        
        /// <summary>
        /// Renders the enemy encounter screen with accumulated dungeon log
        /// </summary>
        public void RenderEnemyEncounter(int x, int y, int width, int height, List<string> dungeonLog)
        {
            currentLineCount = 0;
            
            // Display all accumulated dungeon/room/enemy information
            int availableWidth = width - 4;
            
            foreach (var logEntry in dungeonLog)
            {
                if (y < height - 2)
                {
                    // Use WriteLineColoredWrapped to handle both color markup and text wrapping
                    int linesRendered = textWriter.WriteLineColoredWrapped(logEntry, x + 2, y, availableWidth);
                    y += linesRendered;
                    currentLineCount += linesRendered;
                }
            }
        }
        
        /// <summary>
        /// Renders the combat result screen (victory/defeat)
        /// </summary>
        public void RenderCombatResult(int x, int y, int width, int height, bool playerSurvived, Enemy enemy, BattleNarrative? battleNarrative)
        {
            currentLineCount = 0;
            
            // Display victory or defeat message
            if (enemy.CurrentHealth <= 0)
            {
                canvas.AddText(x + 2, y, string.Format(AsciiArtAssets.UIText.VictoryPrefix, enemy.Name), AsciiArtAssets.Colors.Green);
                currentLineCount++;
            }
            else if (playerSurvived == false)
            {
                canvas.AddText(x + 2, y, AsciiArtAssets.UIText.DefeatMessage, AsciiArtAssets.Colors.Red);
                currentLineCount++;
            }
            y += 2;
            currentLineCount++;
            
            // Battle narrative if available - compact display
            if (battleNarrative != null)
            {
                var narrativeLines = battleNarrative.GetTriggeredNarratives();
                if (narrativeLines != null && narrativeLines.Count > 0)
                {
                    foreach (var line in narrativeLines.Take(5))
                    {
                        if (!string.IsNullOrWhiteSpace(line))
                        {
                            string narrativeLine = line;
                            // Use display length to handle color markup correctly
                            if (ColorParser.GetDisplayLength(narrativeLine) > width - 8)
                            {
                                // Strip markup before truncating to avoid cutting markup in the middle
                                string strippedLine = ColorParser.StripColorMarkup(narrativeLine);
                                narrativeLine = strippedLine.Substring(0, Math.Min(strippedLine.Length, width - 11)) + "...";
                            }
                            canvas.AddText(x + 2, y, narrativeLine, AsciiArtAssets.Colors.Cyan);
                            y++;
                            currentLineCount++;
                        }
                    }
                }
            }
        }
    }
}

