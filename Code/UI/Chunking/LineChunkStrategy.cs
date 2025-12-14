using System;
using System.Collections.Generic;
using System.Linq;

namespace RPGGame.UI.Chunking
{
    /// <summary>
    /// Chunks text by lines (single newlines)
    /// </summary>
    public class LineChunkStrategy : IChunkStrategy
    {
        public List<string> SplitIntoChunks(string text)
        {
            var lines = text.Split(new[] { '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries);
            return lines.Select(l => l.Trim()).Where(l => !string.IsNullOrEmpty(l)).ToList();
        }
    }
}

