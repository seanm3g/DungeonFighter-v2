using System;
using System.Collections.Generic;
using System.Threading;
using Avalonia.Threading;
using RPGGame.UI.Avalonia;
using RPGGame.UI.ColorSystem;
using RPGGame.Utils;

namespace RPGGame.UI.TitleScreen
{
    /// <summary>
    /// Interface for rendering title frames
    /// Allows for different rendering implementations (console, GUI, test mock, etc.)
    /// </summary>
    public interface ITitleRenderer
    {
        /// <summary>
        /// Renders a title frame to the output
        /// </summary>
        void RenderFrame(TitleFrame frame);

        /// <summary>
        /// Clears the display
        /// </summary>
        void Clear();

        /// <summary>
        /// Refreshes the display (flushes buffer if needed)
        /// </summary>
        void Refresh();

        /// <summary>
        /// Shows the "press any key" message after animation completes
        /// </summary>
        void ShowPressKeyMessage();
    }

    /// <summary>
    /// Renders title frames to the Canvas UI (Avalonia GUI)
    /// Uses the existing CanvasUICoordinator for display
    /// </summary>
    public class CanvasTitleRenderer : ITitleRenderer
    {
        private readonly CanvasUICoordinator _canvasUI;

        public CanvasTitleRenderer(CanvasUICoordinator canvasUI)
        {
            _canvasUI = canvasUI ?? throw new ArgumentNullException(nameof(canvasUI));
        }

        public void RenderFrame(TitleFrame frame)
        {
            if (frame?.Lines == null)
            {
                return;
            }

            // Use Invoke instead of Post to ensure frame is rendered before continuing
            // This makes the animation synchronous and prevents frame skipping
            Dispatcher.UIThread.Invoke(() =>
            {
                _canvasUI.Clear();

                int startY = TitleArtAssets.TitleStartY;

                for (int i = 0; i < frame.Lines.Length; i++)
                {
                    var lineSegments = frame.Lines[i];
                    if (lineSegments != null && lineSegments.Count > 0)
                    {
                        // Calculate visible length from segments
                        int visibleLength = 0;
                        foreach (var segment in lineSegments)
                        {
                            visibleLength += segment.Text?.Length ?? 0;
                        }

                        // Center each line horizontally based on its visible length
                        int centerX = Math.Max(0, _canvasUI.CenterX - (visibleLength / 2));
                        
                        // Render the colored text segments
                        _canvasUI.WriteLineColoredSegments(lineSegments, centerX, startY + i);
                    }
                }

                _canvasUI.Refresh();
            });
        }

        public void Clear()
        {
            Dispatcher.UIThread.Invoke(() => _canvasUI.Clear());
        }

        public void Refresh()
        {
            Dispatcher.UIThread.Invoke(() => _canvasUI.Refresh());
        }

        public void ShowPressKeyMessage()
        {
            Dispatcher.UIThread.Invoke(() => _canvasUI.ShowPressKeyMessage());
        }
    }

    /// <summary>
    /// Renders title frames to the console
    /// Fallback renderer for console mode
    /// </summary>
    public class ConsoleTitleRenderer : ITitleRenderer
    {
        public void RenderFrame(TitleFrame frame)
        {
            if (frame?.Lines == null)
            {
                return;
            }

            Console.Clear();

            foreach (var lineSegments in frame.Lines)
            {
                if (lineSegments != null && lineSegments.Count > 0)
                {
                    // Render colored text segments using WriteSegments
                    ColoredConsoleWriter.WriteSegments(lineSegments);
                    Console.WriteLine();
                }
                else
                {
                    Console.WriteLine();
                }
            }
        }

        public void Clear()
        {
            Console.Clear();
        }

        public void Refresh()
        {
            // No-op for console, writes are immediate
        }

        public void ShowPressKeyMessage()
        {
            Console.WriteLine();
            Console.WriteLine("Press any key to continue...");
        }
    }

    /// <summary>
    /// Null renderer for testing or when rendering is disabled
    /// </summary>
    public class NullTitleRenderer : ITitleRenderer
    {
        public void RenderFrame(TitleFrame frame) { }
        public void Clear() { }
        public void Refresh() { }
        public void ShowPressKeyMessage() { }
    }
}

