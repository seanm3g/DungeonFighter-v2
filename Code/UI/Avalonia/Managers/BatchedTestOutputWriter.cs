using System;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Threading;

namespace RPGGame.UI.Avalonia.Managers
{
    /// <summary>
    /// Custom TextWriter that batches updates to prevent flicker
    /// Updates the TextBox in batches rather than character-by-character
    /// </summary>
    public class BatchedTestOutputWriter : TextWriter
    {
        private readonly TextBox? textBox;
        private readonly StringBuilder buffer = new StringBuilder();
        private readonly object bufferLock = new object();
        private Timer? flushTimer;
        private const int FlushIntervalMs = 100; // Update UI every 100ms
        private const int MaxBufferSize = 10000; // Flush if buffer gets too large

        public BatchedTestOutputWriter(TextBox? textBox)
        {
            this.textBox = textBox;
            
            // Set up periodic flush timer
            flushTimer = new Timer(FlushBuffer, null, FlushIntervalMs, FlushIntervalMs);
        }

        public override Encoding Encoding => Encoding.UTF8;

        public void Append(string text)
        {
            lock (bufferLock)
            {
                buffer.Append(text);
                
                // Force flush if buffer is getting too large
                if (buffer.Length > MaxBufferSize)
                {
                    FlushBuffer(null);
                }
            }
        }

        public override void Write(char value)
        {
            if (value == '\r') return; // Ignore carriage returns
            
            lock (bufferLock)
            {
                if (value == '\n')
                {
                    buffer.AppendLine();
                }
                else
                {
                    buffer.Append(value);
                }
                
                // Force flush if buffer is getting too large
                if (buffer.Length > MaxBufferSize)
                {
                    FlushBuffer(null);
                }
            }
        }

        public override void Write(string? value)
        {
            if (value == null) return;
            
            lock (bufferLock)
            {
                buffer.Append(value);
                
                // Force flush if buffer is getting too large
                if (buffer.Length > MaxBufferSize)
                {
                    FlushBuffer(null);
                }
            }
        }

        public override void WriteLine(string? value)
        {
            lock (bufferLock)
            {
                if (value != null)
                {
                    buffer.AppendLine(value);
                }
                else
                {
                    buffer.AppendLine();
                }
                
                // Force flush if buffer is getting too large
                if (buffer.Length > MaxBufferSize)
                {
                    FlushBuffer(null);
                }
            }
        }

        private void FlushBuffer(object? state)
        {
            string contentToAppend;
            
            lock (bufferLock)
            {
                if (buffer.Length == 0) return;
                
                contentToAppend = buffer.ToString();
                buffer.Clear();
            }
            
            if (!string.IsNullOrEmpty(contentToAppend) && textBox != null)
            {
                Dispatcher.UIThread.Post(() =>
                {
                    try
                    {
                        // Append to existing text
                        textBox.Text += contentToAppend;
                        
                        // Auto-scroll to bottom
                        textBox.CaretIndex = textBox.Text.Length;
                    }
                    catch (Exception ex)
                    {
                        // Silently handle any UI update errors
                        System.Diagnostics.Debug.WriteLine($"Error updating TextBox: {ex.Message}");
                    }
                });
            }
        }

        public new void Flush()
        {
            // Stop the timer first to prevent new scheduled flushes
            flushTimer?.Dispose();
            flushTimer = null;
            
            // Flush any remaining buffer content synchronously
            string? finalContent = null;
            lock (bufferLock)
            {
                if (buffer.Length > 0)
                {
                    finalContent = buffer.ToString();
                    buffer.Clear();
                }
            }
            
            // Write final content directly to UI thread synchronously if possible
            if (!string.IsNullOrEmpty(finalContent) && textBox != null)
            {
                try
                {
                    // Use Invoke to ensure synchronous update
                    Dispatcher.UIThread.Invoke(() =>
                    {
                        textBox.Text += finalContent;
                        textBox.CaretIndex = textBox.Text.Length;
                    });
                }
                catch
                {
                    // Fallback to async if sync fails
                    Dispatcher.UIThread.Post(() =>
                    {
                        textBox.Text += finalContent;
                        textBox.CaretIndex = textBox.Text.Length;
                    });
                }
            }
            
            // Also call the async flush buffer in case there's any remaining content
            FlushBuffer(null);
        }
    }
}
