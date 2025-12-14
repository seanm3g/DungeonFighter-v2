using System;
using System.IO;

namespace RPGGame.MCP.Tools.Utilities
{
    /// <summary>
    /// Utility for capturing console output during test execution
    /// </summary>
    public class ConsoleOutputCapture : IDisposable
    {
        private TextWriter? originalOut;
        private StringWriter? stringWriter;
        private bool disposed = false;
        
        /// <summary>
        /// Creates a new console output capture and redirects Console.Out
        /// </summary>
        public ConsoleOutputCapture()
        {
            originalOut = Console.Out;
            stringWriter = new StringWriter();
            Console.SetOut(stringWriter);
        }
        
        /// <summary>
        /// Gets the captured output as a string
        /// </summary>
        public string GetOutput()
        {
            return stringWriter?.ToString() ?? string.Empty;
        }
        
        /// <summary>
        /// Clears the captured output
        /// </summary>
        public void Clear()
        {
            stringWriter?.GetStringBuilder().Clear();
        }
        
        /// <summary>
        /// Restores the original console output and disposes resources
        /// </summary>
        public void Dispose()
        {
            if (!disposed)
            {
                if (originalOut != null)
                {
                    Console.SetOut(originalOut);
                }
                stringWriter?.Dispose();
                disposed = true;
            }
        }
    }
}

