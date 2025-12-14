using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace RPGGame.UI.Chunking
{
    /// <summary>
    /// Chunks text by sentences (periods, question marks, exclamation points)
    /// </summary>
    public class SentenceChunkStrategy : IChunkStrategy
    {
        public List<string> SplitIntoChunks(string text)
        {
            // Split on sentence-ending punctuation followed by space or newline
            // But preserve the punctuation with the sentence
            var pattern = @"(?<=[.!?])\s+(?=[A-Z\n])";
            var sentences = Regex.Split(text, pattern);
            
            var chunks = new List<string>();
            foreach (var sentence in sentences)
            {
                var trimmed = sentence.Trim();
                if (!string.IsNullOrEmpty(trimmed))
                {
                    chunks.Add(trimmed);
                }
            }
            
            return chunks;
        }
    }
}

