using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using RPGGame.UI.ColorSystem;
using RPGGame.UI.Chunking;

namespace RPGGame.UI
{
    /// <summary>
    /// Provides chunked text reveal functionality where text appears chunk by chunk
    /// with delays proportional to the length of each chunk
    /// </summary>
    public static class ChunkedTextReveal
    {
        /// <summary>
        /// Configuration for chunked text reveal
        /// </summary>
        public class RevealConfig
        {
            /// <summary>
            /// Base delay in milliseconds per character (default: 30ms per char)
            /// </summary>
            public int BaseDelayPerCharMs { get; set; } = 30;
            
            /// <summary>
            /// Minimum delay between chunks in milliseconds (default: 500ms)
            /// </summary>
            public int MinDelayMs { get; set; } = 500;
            
            /// <summary>
            /// Maximum delay between chunks in milliseconds (default: 4000ms)
            /// </summary>
            public int MaxDelayMs { get; set; } = 4000;
            
            /// <summary>
            /// The chunking strategy to use
            /// </summary>
            public ChunkStrategy Strategy { get; set; } = ChunkStrategy.Sentence;
            
            /// <summary>
            /// Whether to add a blank line between chunks
            /// </summary>
            public bool AddBlankLineBetweenChunks { get; set; } = false;
            
            /// <summary>
            /// Whether chunked reveal is enabled (can be disabled globally)
            /// </summary>
            public bool Enabled { get; set; } = true;
        }
        
        /// <summary>
        /// Strategy for breaking text into chunks
        /// </summary>
        public enum ChunkStrategy
        {
            /// <summary>Break by sentences (periods, question marks, exclamation points)</summary>
            Sentence,
            
            /// <summary>Break by paragraphs (double newlines or explicit paragraph markers)</summary>
            Paragraph,
            
            /// <summary>Break by lines (single newlines)</summary>
            Line,
            
            /// <summary>Break by semantic sections (headers, stats blocks, etc.)</summary>
            Semantic
        }
        
        // Default configuration
        private static RevealConfig _defaultConfig = new RevealConfig();
        
        /// <summary>
        /// Gets or sets the default configuration for chunked reveals
        /// </summary>
        public static RevealConfig DefaultConfig
        {
            get => _defaultConfig;
            set => _defaultConfig = value ?? new RevealConfig();
        }
        
        /// <summary>
        /// Reveals text chunk by chunk with delays proportional to chunk length
        /// </summary>
        /// <param name="text">The text to reveal</param>
        /// <param name="config">Optional configuration (uses default if null)</param>
        public static async Task RevealTextAsync(string text, RevealConfig? config = null)
        {
            config ??= _defaultConfig;
            
            // If disabled, just write the text directly
            if (!config.Enabled)
            {
                UIManager.WriteLine(text);
                return;
            }
            
            // Split text into chunks
            var chunks = SplitIntoChunks(text, config.Strategy);
            
            // Reveal each chunk
            for (int i = 0; i < chunks.Count; i++)
            {
                var chunk = chunks[i];
                
                // Write the chunk
                UIManager.WriteLine(chunk);
                
                // Add blank line if configured
                if (config.AddBlankLineBetweenChunks && i < chunks.Count - 1)
                {
                    UIManager.WriteBlankLine();
                }
                
                // Calculate delay based on chunk length (if not the last chunk)
                if (i < chunks.Count - 1)
                {
                    int delay = CalculateDelay(chunk, config);
                    await Task.Delay(delay);
                }
            }
        }
        
        /// <summary>
        /// Synchronous version for backwards compatibility
        /// </summary>
        public static void RevealText(string text, RevealConfig? config = null)
        {
            Task.Run(async () => await RevealTextAsync(text, config)).Wait();
        }
        
        /// <summary>
        /// Splits text into chunks based on the specified strategy
        /// </summary>
        private static List<string> SplitIntoChunks(string text, ChunkStrategy strategy)
        {
            var chunkStrategy = ChunkStrategyFactory.GetStrategy(strategy);
            var chunks = chunkStrategy.SplitIntoChunks(text);
            
            // Remove empty chunks
            return chunks.Where(c => !string.IsNullOrWhiteSpace(c)).ToList();
        }
        
        /// <summary>
        /// Calculates delay based on chunk length
        /// Longer chunks get longer delays (proportional timing)
        /// </summary>
        private static int CalculateDelay(string chunk, RevealConfig config)
        {
            // Get the display length (excluding color markup)
            var segments = ColoredTextParser.Parse(chunk);
            int displayLength = ColoredTextRenderer.GetDisplayLength(segments);
            
            // Calculate delay: base delay per character * number of characters
            int calculatedDelay = displayLength * config.BaseDelayPerCharMs;
            
            // Clamp to min/max
            int delay = Math.Max(config.MinDelayMs, Math.Min(config.MaxDelayMs, calculatedDelay));
            
            return delay;
        }
        
        /// <summary>
        /// Quick method: Reveal text by sentences with default settings
        /// </summary>
        public static async Task RevealBySentencesAsync(string text)
        {
            await RevealTextAsync(text, new RevealConfig { Strategy = ChunkStrategy.Sentence });
        }
        
        public static void RevealBySentences(string text)
        {
            RevealText(text, new RevealConfig { Strategy = ChunkStrategy.Sentence });
        }
        
        /// <summary>
        /// Quick method: Reveal text by paragraphs with default settings
        /// </summary>
        public static async Task RevealByParagraphsAsync(string text)
        {
            await RevealTextAsync(text, new RevealConfig { Strategy = ChunkStrategy.Paragraph });
        }
        
        public static void RevealByParagraphs(string text)
        {
            RevealText(text, new RevealConfig { Strategy = ChunkStrategy.Paragraph });
        }
        
        /// <summary>
        /// Quick method: Reveal text by lines with default settings
        /// </summary>
        public static async Task RevealByLinesAsync(string text)
        {
            await RevealTextAsync(text, new RevealConfig { Strategy = ChunkStrategy.Line });
        }
        
        public static void RevealByLines(string text)
        {
            RevealText(text, new RevealConfig { Strategy = ChunkStrategy.Line });
        }
        
        /// <summary>
        /// Quick method: Reveal text by semantic sections with default settings
        /// </summary>
        public static async Task RevealBySemanticAsync(string text)
        {
            await RevealTextAsync(text, new RevealConfig { Strategy = ChunkStrategy.Semantic });
        }
        
        public static void RevealBySemantic(string text)
        {
            RevealText(text, new RevealConfig { Strategy = ChunkStrategy.Semantic });
        }
        
        /// <summary>
        /// Quick method: Reveal dungeon exploration text (uses semantic chunking)
        /// Optimized for dungeon room descriptions, encounters, etc.
        /// </summary>
        public static async Task RevealDungeonTextAsync(string text)
        {
            await RevealTextAsync(text, new RevealConfig 
            { 
                Strategy = ChunkStrategy.Semantic,
                BaseDelayPerCharMs = 25, // Slightly faster for gameplay
                MinDelayMs = 800,
                MaxDelayMs = 3000,
                AddBlankLineBetweenChunks = false
            });
        }
        
        public static void RevealDungeonText(string text)
        {
            RevealText(text, new RevealConfig 
            { 
                Strategy = ChunkStrategy.Semantic,
                BaseDelayPerCharMs = 25,
                MinDelayMs = 800,
                MaxDelayMs = 3000,
                AddBlankLineBetweenChunks = false
            });
        }
        
        /// <summary>
        /// Quick method: Reveal combat text (uses sentence chunking)
        /// Optimized for combat messages
        /// </summary>
        public static async Task RevealCombatTextAsync(string text)
        {
            await RevealTextAsync(text, new RevealConfig 
            { 
                Strategy = ChunkStrategy.Sentence,
                BaseDelayPerCharMs = 20, // Faster for combat
                MinDelayMs = 500,
                MaxDelayMs = 2000,
                AddBlankLineBetweenChunks = false
            });
        }
        
        public static void RevealCombatText(string text)
        {
            RevealText(text, new RevealConfig 
            { 
                Strategy = ChunkStrategy.Sentence,
                BaseDelayPerCharMs = 20,
                MinDelayMs = 500,
                MaxDelayMs = 2000,
                AddBlankLineBetweenChunks = false
            });
        }
    }
}

