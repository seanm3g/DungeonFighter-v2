using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace RPGGame.UI.Chunking
{
    /// <summary>
    /// Chunks text by semantic sections (headers, stats blocks, etc.)
    /// </summary>
    public class SemanticChunkStrategy : IChunkStrategy
    {
        public List<string> SplitIntoChunks(string text)
        {
            var chunks = new List<string>();
            var lines = text.Split(new[] { '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries);
            
            string currentChunk = "";
            foreach (var line in lines)
            {
                var trimmedLine = line.Trim();
                
                // Check if this line starts a new semantic section
                bool isHeader = IsHeaderLine(trimmedLine);
                bool isStatsBlock = IsStatsBlock(trimmedLine);
                bool isSeparator = IsSeparatorLine(trimmedLine);
                
                if ((isHeader || isStatsBlock || isSeparator) && !string.IsNullOrEmpty(currentChunk))
                {
                    // Start a new chunk
                    chunks.Add(currentChunk.Trim());
                    currentChunk = trimmedLine;
                }
                else
                {
                    // Add to current chunk
                    if (!string.IsNullOrEmpty(currentChunk))
                    {
                        currentChunk += "\n";
                    }
                    currentChunk += trimmedLine;
                }
            }
            
            // Add the last chunk
            if (!string.IsNullOrEmpty(currentChunk))
            {
                chunks.Add(currentChunk.Trim());
            }
            
            return chunks;
        }
        
        /// <summary>
        /// Checks if a line is a header (e.g., "=== ENTERING DUNGEON ===")
        /// </summary>
        private static bool IsHeaderLine(string line)
        {
            // Lines with all caps and/or lots of = or - characters
            return Regex.IsMatch(line, @"^[=\-]{3,}|[A-Z\s]{10,}|^[=\-\s]*[A-Z\s]+[=\-\s]*$");
        }
        
        /// <summary>
        /// Checks if a line is a stats block (e.g., "Enemy Stats - Health: 69/69, Armor: 1")
        /// </summary>
        private static bool IsStatsBlock(string line)
        {
            return line.Contains("Stats") || line.Contains("Health:") || 
                   line.Contains("Attack:") || line.Contains("STR") || 
                   line.Contains("AGI") || line.Contains("TEC") || line.Contains("INT");
        }
        
        /// <summary>
        /// Checks if a line is a separator (e.g., "====================================")
        /// </summary>
        private static bool IsSeparatorLine(string line)
        {
            return Regex.IsMatch(line, @"^[=\-]{4,}$");
        }
    }
}

