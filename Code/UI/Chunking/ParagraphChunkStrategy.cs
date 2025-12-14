using System;
using System.Collections.Generic;
using System.Linq;

namespace RPGGame.UI.Chunking
{
    /// <summary>
    /// Chunks text by paragraphs (double newlines)
    /// </summary>
    public class ParagraphChunkStrategy : IChunkStrategy
    {
        public List<string> SplitIntoChunks(string text)
        {
            var paragraphs = text.Split(new[] { "\n\n", "\r\n\r\n" }, StringSplitOptions.RemoveEmptyEntries);
            return paragraphs.Select(p => p.Trim()).Where(p => !string.IsNullOrEmpty(p)).ToList();
        }
    }
}

