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

            // Use Invoke to ensure frame is rendered on UI thread synchronously
            // This blocks until the UI thread has processed the render
            Dispatcher.UIThread.Invoke(() =>
            {
                // Clear the entire canvas for title screen animation
                // This ensures no text from previous frames remains
                _canvasUI.Clear();

                int startY = TitleArtAssets.TitleStartY;
                
                // Calculate title line ranges in the frame
                // Frame structure: 15 top padding + 2 blank + 6 DUNGEON + 1 blank + 1 decorator + 1 blank + 6 FIGHTERS + ...
                const int dungeonStartIndex = 17; // 15 padding + 2 blank lines
                const int dungeonEndIndex = 22;    // dungeonStartIndex + 6 lines - 1
                // const int decoratorIndex = 24; // dungeonEndIndex + 1 blank + 1 (reserved for future use)
                const int fighterStartIndex = 26; // decoratorIndex + 1 blank + 1
                const int fighterEndIndex = 31;   // fighterStartIndex + 6 lines - 1
                const int titleOffset = 2; // Offset to shift title right
                const int globalLeftShift = -6; // Shift all lines 6 spaces to the left

                for (int i = 0; i < frame.Lines.Length; i++)
                {
                    var lineSegments = frame.Lines[i];
                    int currentY = startY + i;
                    
                    if (lineSegments != null && lineSegments.Count > 0)
                    {
                        // Calculate visible length from segments (excluding leading spaces for centering)
                        int visibleLength = 0;
                        bool isTitleLine = (i >= dungeonStartIndex && i <= dungeonEndIndex) || 
                                         (i >= fighterStartIndex && i <= fighterEndIndex);
                        
                        foreach (var segment in lineSegments)
                        {
                            visibleLength += segment.Text?.Length ?? 0;
                        }

                        // Center each line horizontally based on its visible length
                        int centerX = Math.Max(0, _canvasUI.CenterX - (visibleLength / 2));
                        
                        // Apply global left shift to all lines
                        centerX += globalLeftShift;
                        
                        // Add offset for title lines to shift them right
                        if (isTitleLine)
                        {
                            centerX += titleOffset;
                            // Move FIGHTERS one character to the left (relative to DUNGEON)
                            if (i >= fighterStartIndex && i <= fighterEndIndex)
                            {
                                centerX -= 1;
                            }
                        }
                        
                        // Render the colored text segments
                        _canvasUI.WriteLineColoredSegments(lineSegments, centerX, currentY);
                    }
                }

                // Refresh to display the new frame
                // InvalidateVisual() schedules a render, but we need to ensure it happens
                _canvasUI.Refresh();
                
                // Force the UI thread to process pending operations
                // This helps ensure the frame is actually rendered before we continue
                Dispatcher.UIThread.RunJobs(DispatcherPriority.Render);
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

