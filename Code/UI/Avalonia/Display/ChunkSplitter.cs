using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using RPGGame.UI;

namespace RPGGame.UI.Avalonia.Display
{
    /// <summary>
    /// Utility class for splitting text into chunks based on strategy
    /// </summary>
    public static class ChunkSplitter
    {
        /// <summary>
        /// Splits text into chunks based on strategy (from ChunkedTextReveal)
        /// </summary>
        public static List<string> SplitIntoChunks(string text, ChunkedTextReveal.ChunkStrategy strategy)
        {
            var chunks = new List<string>();
            
            switch (strategy)
            {
                case ChunkedTextReveal.ChunkStrategy.Sentence:
                    chunks = Regex.Split(text, @"(?<=[.!?])\s+(?=[A-Z\n])")
                        .Select(s => s.Trim())
                        .Where(s => !string.IsNullOrEmpty(s))
                        .ToList();
                    break;
                    
                case ChunkedTextReveal.ChunkStrategy.Paragraph:
                    chunks = text.Split(new[] { "\n\n", "\r\n\r\n" }, StringSplitOptions.RemoveEmptyEntries)
                        .Select(p => p.Trim())
                        .Where(p => !string.IsNullOrEmpty(p))
                        .ToList();
                    break;
                    
                case ChunkedTextReveal.ChunkStrategy.Line:
                    chunks = text.Split(new[] { '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries)
                        .Select(l => l.TrimEnd())
                        .Where(l => !string.IsNullOrEmpty(l))
                        .ToList();
                    break;
                    
                case ChunkedTextReveal.ChunkStrategy.Semantic:
                    chunks = text.Split(new[] { '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries)
                        .Select(l => l.TrimEnd())
                        .Where(l => !string.IsNullOrEmpty(l))
                        .ToList();
                    break;
            }
            
            return chunks;
        }
    }
}

