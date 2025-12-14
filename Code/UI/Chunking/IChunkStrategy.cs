using System.Collections.Generic;

namespace RPGGame.UI.Chunking
{
    /// <summary>
    /// Interface for text chunking strategies
    /// </summary>
    public interface IChunkStrategy
    {
        /// <summary>
        /// Splits text into chunks according to the strategy
        /// </summary>
        List<string> SplitIntoChunks(string text);
    }
}

