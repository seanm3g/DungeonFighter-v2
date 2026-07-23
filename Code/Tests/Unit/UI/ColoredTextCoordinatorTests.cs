using System;
using System.Collections.Generic;
using RPGGame.Tests;
using RPGGame.UI;
using RPGGame.UI.Avalonia;
using RPGGame.UI.Avalonia.Coordinators;
using RPGGame.UI.Avalonia.Managers;
using RPGGame.UI.Avalonia.Renderers;
using RPGGame.UI.ColorSystem;

namespace RPGGame.Tests.Unit.UI
{
    /// <summary>
    /// Regression coverage for ColoredTextCoordinator blank-line writing.
    /// </summary>
    public static class ColoredTextCoordinatorTests
    {
        private static int _testsRun;
        private static int _testsPassed;
        private static int _testsFailed;

        public static void RunAllTests()
        {
            Console.WriteLine("=== ColoredTextCoordinator Tests ===\n");

            _testsRun = 0;
            _testsPassed = 0;
            _testsFailed = 0;

            TestWriteColoredSegments_EmptyListWritesBlankLine();
            TestWriteColoredSegments_ExitMenuLeadingBlankIsPreserved();

            TestBase.PrintSummary("ColoredTextCoordinator Tests", _testsRun, _testsPassed, _testsFailed);
        }

        private static void TestWriteColoredSegments_EmptyListWritesBlankLine()
        {
            Console.WriteLine("--- Testing empty segments write a blank line ---");

            var textManager = new RecordingCanvasTextManager();
            var coordinator = new ColoredTextCoordinator(
                textManager,
                new MessageWritingCoordinator(textManager, null!, null!));

            coordinator.WriteLineColoredSegments(new List<ColoredText>(), UIMessageType.Title);

            TestBase.AssertEqual(1, textManager.DisplayBuffer.Count,
                "Empty ColoredText segment lists must write a blank line (not be dropped)",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
            TestBase.AssertEqual("", textManager.DisplayBuffer[0],
                "Empty segment list should become an empty display buffer line",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        private static void TestWriteColoredSegments_ExitMenuLeadingBlankIsPreserved()
        {
            Console.WriteLine("--- Testing exit-choice menu blank lines survive WriteLineColoredSegments ---");

            var textManager = new RecordingCanvasTextManager();
            var coordinator = new ColoredTextCoordinator(
                textManager,
                new MessageWritingCoordinator(textManager, null!, null!));

            foreach (var line in DungeonExitChoiceHandler.BuildExitChoiceMenuLines())
            {
                coordinator.WriteLineColoredSegments(line, UIMessageType.Title);
            }

            TestBase.AssertEqual(5, textManager.DisplayBuffer.Count,
                "Exit choice menu should write all five lines including the leading blank",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
            TestBase.AssertEqual("", textManager.DisplayBuffer[0],
                "Leading blank before top divider must not be swallowed (consumable path has no room-cleared spacer)",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
            TestBase.AssertEqual(AsciiArtAssets.UIText.Divider, textManager.DisplayBuffer[1],
                "Top divider should follow the leading blank",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
            TestBase.AssertEqual(AsciiArtAssets.UIText.Divider, textManager.DisplayBuffer[4],
                "Bottom divider should sit flush under option 2 (no blank between)",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        private sealed class RecordingCanvasTextManager : ICanvasTextManager
        {
            public List<string> DisplayBuffer { get; } = new List<string>();

            public int BufferLineCount => DisplayBuffer.Count;

            public void AddMessageBatch(IEnumerable<string> messages, int delayAfterBatchMs = 0)
            {
                DisplayBuffer.AddRange(messages);
            }

            public void AddToDisplayBuffer(string message, UIMessageType messageType = UIMessageType.System)
            {
                DisplayBuffer.Add(message);
            }

            public void ClearDisplay()
            {
            }

            public void ClearDisplayBuffer()
            {
                DisplayBuffer.Clear();
            }

            public void ClearDisplayBufferWithoutRender()
            {
                DisplayBuffer.Clear();
            }

            public void RenderDisplayBuffer(int x, int y, int width, int height)
            {
            }

            public void ResetScroll()
            {
            }

            public void ScrollDown(int lines = 3)
            {
            }

            public void ScrollUp(int lines = 3)
            {
            }

            public void WriteChunked(string message, ChunkedTextReveal.RevealConfig? config = null)
            {
                DisplayBuffer.Add(message);
            }

            public void WriteLineColored(string message, int x, int y)
            {
                DisplayBuffer.Add(message);
            }

            public void WriteLineColoredSegments(List<ColoredText> segments, int x, int y)
            {
                DisplayBuffer.Add(string.Join("", segments.ConvertAll(segment => segment.Text)));
            }

            public int WriteLineColoredWrapped(string message, int x, int y, int maxWidth)
            {
                DisplayBuffer.Add(message);
                return 1;
            }
        }
    }
}
