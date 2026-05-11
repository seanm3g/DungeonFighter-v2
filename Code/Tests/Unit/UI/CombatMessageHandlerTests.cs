using System;
using System.Collections.Generic;
using RPGGame.Tests;
using RPGGame.UI;
using RPGGame.UI.Avalonia;
using RPGGame.UI.Avalonia.Managers;
using RPGGame.UI.Avalonia.Renderers;

namespace RPGGame.Tests.Unit.UI
{
    public static class CombatMessageHandlerTests
    {
        private static int _testsRun = 0;
        private static int _testsPassed = 0;
        private static int _testsFailed = 0;

        public static void RunAllTests()
        {
            Console.WriteLine("=== CombatMessageHandler Tests ===\n");

            _testsRun = 0;
            _testsPassed = 0;
            _testsFailed = 0;

            TestAddRoomClearedMessage_DoesNotAddDivider();

            TestBase.PrintSummary("CombatMessageHandler Tests", _testsRun, _testsPassed, _testsFailed);
        }

        private static void TestAddRoomClearedMessage_DoesNotAddDivider()
        {
            Console.WriteLine("--- Testing room cleared message divider suppression ---");

            TextSpacingSystem.Reset();
            TextSpacingSystem.RecordBlockDisplayed(TextSpacingSystem.BlockType.CombatAction, "Hero");

            var textManager = new RecordingCanvasTextManager();
            var handler = new CombatMessageHandler(textManager);

            handler.AddRoomClearedMessage(null);

            TestBase.AssertEqual(3, textManager.DisplayBuffer.Count,
                "Room cleared message should contain spacing, message, and trailing blank only",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
            TestBase.AssertEqual("", textManager.DisplayBuffer[0],
                "Room cleared should preserve spacing after combat",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
            TestBase.AssertEqual(AsciiArtAssets.UIText.RoomClearedMessage, textManager.DisplayBuffer[1],
                "Room cleared should write the success message",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
            TestBase.AssertEqual("", textManager.DisplayBuffer[2],
                "Room cleared should leave a blank line before the exit prompt",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
            TestBase.AssertFalse(textManager.DisplayBuffer.Contains(AsciiArtAssets.UIText.Divider),
                "Room cleared should not add a divider because the exit prompt owns menu dividers",
                ref _testsRun, ref _testsPassed, ref _testsFailed);

            TextSpacingSystem.Reset();
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

            public void WriteLineColoredSegments(List<RPGGame.UI.ColorSystem.ColoredText> segments, int x, int y)
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
