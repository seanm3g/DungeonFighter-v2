using System.Collections.Generic;
using RPGGame.UI;

namespace RPGGame.UI.Avalonia.Display.Helpers
{
    /// <summary>
    /// Helper for splitting text into chunks
    /// </summary>
    public static class ChunkSplitterHelper
    {
        /// <summary>
        /// Splits text into chunks using the chunk splitter
        /// </summary>
        public static List<string> SplitIntoChunks(string text, ChunkedTextReveal.ChunkStrategy strategy)
        {
            return ChunkSplitter.SplitIntoChunks(text, strategy);
        }
    }
}

