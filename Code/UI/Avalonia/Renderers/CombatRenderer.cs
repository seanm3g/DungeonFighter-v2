using Avalonia.Media;
using RPGGame.UI;
using System;
using System.Collections.Generic;
using System.Linq;
using RPGGame.UI.ColorSystem;
using RPGGame.UI.Avalonia;

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
        /// Renders the main combat screen with dungeon/room/enemy info header at top and combat log below
        /// </summary>
        public void RenderCombat(int x, int y, int width, int height, Character player, Enemy enemy, List<string> combatLog, 
            string? dungeonName = null, string? roomName = null, List<string>? dungeonContext = null)
        {
            currentLineCount = 0;
            int startY = y;
            int currentY = y;
            
            // Calculate available width for content
            int availableWidth = width - 4;
            
            // Render dungeon/room/enemy info header at the top
            int headerLines = 0;
            if (dungeonContext != null && dungeonContext.Count > 0)
            {
                // #region agent log
                try { System.IO.File.AppendAllText(@"d:\Code Projects\github projects\DungeonFighter-v2\.cursor\debug.log", System.Text.Json.JsonSerializer.Serialize(new { id = $"log_{DateTime.UtcNow.Ticks}", timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(), location = "CombatRenderer.cs:RenderCombatScreen", message = "Rendering dungeon context", data = new { contextCount = dungeonContext.Count, hasEnemy = enemy != null, enemyName = enemy?.Name ?? "null" }, sessionId = "debug-session", runId = "run1", hypothesisId = "H9" }) + "\n"); } catch { }
                // #endregion
                // Render the dungeon context info (dungeon header, room info, enemy encounter)
                // This includes dungeon name, room name, room number, enemy stats, etc.
                foreach (var contextLine in dungeonContext)
                {
                    if (!string.IsNullOrWhiteSpace(contextLine) && currentY < startY + height - 10)
                    {
                        // Render with color markup and text wrapping
                        int linesRendered = textWriter.WriteLineColoredWrapped(contextLine, x + 2, currentY, availableWidth);
                        currentY += linesRendered;
                        headerLines += linesRendered;
                        currentLineCount += linesRendered;
                    }
                }
                
                // Add a clear separator/marker between pre-combat info and combat log
                if (headerLines > 0 && currentY < startY + height - 2)
                {
                    // Add blank lines for separation
                    currentY += 2;
                    headerLines += 2;
                    currentLineCount += 2;
                    
                    // Add a visual separator line (gray color for subtle separation)
                    string separator = new string('═', Math.Min(availableWidth - 4, 50));
                    var separatorSegments = new ColoredTextBuilder()
                        .Add(separator, ColorPalette.Gray)
                        .Build();
                    textWriter.WriteLineColoredWrapped(separatorSegments, x + 2, currentY, availableWidth);
                    currentY += 1;
                    headerLines += 1;
                    currentLineCount += 1;
                    
                    // Add another blank line
                    currentY += 1;
                    headerLines += 1;
                    currentLineCount += 1;
                }
            }
            else
            {
                // Fallback: Show basic info if context is not available
                if (!string.IsNullOrEmpty(dungeonName) || !string.IsNullOrEmpty(roomName) || enemy != null)
                {
                    if (!string.IsNullOrEmpty(dungeonName))
                    {
                        var dungeonSegments = new ColoredTextBuilder()
                            .Add("Dungeon: ", ColorPalette.Info)
                            .Add(dungeonName, ColorPalette.Info)
                            .Build();
                        int linesRendered = textWriter.WriteLineColoredWrapped(dungeonSegments, x + 2, currentY, availableWidth);
                        currentY += linesRendered;
                        headerLines += linesRendered;
                        currentLineCount += linesRendered;
                    }
                    if (!string.IsNullOrEmpty(roomName))
                    {
                        var roomSegments = new ColoredTextBuilder()
                            .Add("Room: ", ColorPalette.Info)
                            .Add(roomName, ColorPalette.Info)
                            .Build();
                        int linesRendered = textWriter.WriteLineColoredWrapped(roomSegments, x + 2, currentY, availableWidth);
                        currentY += linesRendered;
                        headerLines += linesRendered;
                        currentLineCount += linesRendered;
                    }
                    if (enemy != null)
                    {
                        string enemyWeaponInfo = enemy.Weapon != null 
                            ? string.Format(AsciiArtAssets.UIText.WeaponSuffix, enemy.Weapon.Name)
                            : "";
                        var enemySegments = new ColoredTextBuilder()
                            .Add("Enemy: ", ColorPalette.Warning)
                            .Add($"{enemy.Name}{enemyWeaponInfo}", ColorPalette.Warning)
                            .Build();
                        int linesRendered = textWriter.WriteLineColoredWrapped(enemySegments, x + 2, currentY, availableWidth);
                        currentY += linesRendered;
                        headerLines += linesRendered;
                        currentLineCount += linesRendered;
                    }
                    currentY++;
                    headerLines++;
                    currentLineCount++;
                }
            }
            
            // Calculate remaining height for combat log
            int remainingHeight = height - headerLines - 2;
            int combatLogStartY = currentY;
            
            // Clear the combat log area before rendering to prevent text overlap
            // Clear from the start of combat log to the end of the render area
            if (combatLogStartY < startY + height)
            {
                int clearHeight = Math.Min(startY + height - combatLogStartY, remainingHeight + 2);
                textWriter.ClearTextInArea(x, combatLogStartY, width, clearHeight);
            }
            
            // Render combat log below the header
            foreach (var logEntry in combatLog.TakeLast(20))
            {
                if (currentY < startY + height - 2 && currentY < combatLogStartY + remainingHeight)
                {
                    // Render with color markup and text wrapping
                    int linesRendered = textWriter.WriteLineColoredWrapped(logEntry, x + 2, currentY, availableWidth);
                    currentY += linesRendered;
                    currentLineCount += linesRendered;
                }
            }
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
                            var segments = ColoredTextParser.Parse(narrativeLine);
                            int displayLength = ColoredTextRenderer.GetDisplayLength(segments);
                            if (displayLength > width - 8)
                            {
                                // Strip markup before truncating to avoid cutting markup in the middle
                                string strippedLine = ColoredTextRenderer.RenderAsPlainText(segments);
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

