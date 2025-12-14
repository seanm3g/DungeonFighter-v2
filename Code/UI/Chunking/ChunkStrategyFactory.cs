using System.Collections.Generic;
using RPGGame.UI;

namespace RPGGame.UI.Chunking
{
    /// <summary>
    /// Factory for creating chunk strategies
    /// </summary>
    public static class ChunkStrategyFactory
    {
        private static readonly Dictionary<ChunkedTextReveal.ChunkStrategy, IChunkStrategy> _strategies = new()
        {
            { ChunkedTextReveal.ChunkStrategy.Sentence, new SentenceChunkStrategy() },
            { ChunkedTextReveal.ChunkStrategy.Paragraph, new ParagraphChunkStrategy() },
            { ChunkedTextReveal.ChunkStrategy.Line, new LineChunkStrategy() },
            { ChunkedTextReveal.ChunkStrategy.Semantic, new SemanticChunkStrategy() }
        };
        
        /// <summary>
        /// Gets a chunk strategy instance for the specified strategy type
        /// </summary>
        public static IChunkStrategy GetStrategy(ChunkedTextReveal.ChunkStrategy strategy)
        {
            return _strategies[strategy];
        }
    }
}

