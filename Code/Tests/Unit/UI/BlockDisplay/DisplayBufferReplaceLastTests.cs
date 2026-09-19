using System.Collections.Generic;
using Avalonia.Media;
using RPGGame;
using RPGGame.Tests;
using RPGGame.UI;
using RPGGame.UI.Avalonia.Display;
using RPGGame.UI.BlockDisplay;
using RPGGame.UI.ColorSystem;

namespace RPGGame.Tests.Unit.UI.BlockDisplay
{
    /// <summary>
    /// Tests in-place last-line replace used by the combat setup/punchline beat.
    /// </summary>
    public static class DisplayBufferReplaceLastTests
    {
        private static int _testsRun;
        private static int _testsPassed;
        private static int _testsFailed;

        public static void RunAllTests()
        {
            Console.WriteLine("=== DisplayBuffer ReplaceLast Tests ===\n");
            _testsRun = 0;
            _testsPassed = 0;
            _testsFailed = 0;

            TestReplaceLastOverwritesWithoutGrowingCount();
            TestReplaceAtFromEndLeavesNeighborLines();
            TestReplaceAtFromEndOutOfRangeIsNoOp();
            TestReservationDumpCountMatchesFollowUps();
            TestReservedFollowUpsFillInPlaceWithoutGrowingCount();

            TestBase.PrintSummary("DisplayBuffer ReplaceLast Tests", _testsRun, _testsPassed, _testsFailed);
        }

        private static void TestReplaceLastOverwritesWithoutGrowingCount()
        {
            Console.WriteLine("--- ReplaceLast overwrites the last line ---");

            var buffer = new DisplayBuffer();
            buffer.Add(new List<ColoredText> { new ColoredText("Angus Attacks Goblin...", Colors.White) }, UIMessageType.Combat);
            int countBefore = buffer.Count;

            buffer.ReplaceLast(new List<ColoredText>
            {
                new ColoredText("Angus Attacks Goblin... and hits for 12 damage", Colors.White)
            }, UIMessageType.Combat);

            TestBase.AssertEqual(countBefore, buffer.Count,
                "ReplaceLast should not add a new line",
                ref _testsRun, ref _testsPassed, ref _testsFailed);

            string last = buffer.MessagesAsStrings[buffer.Count - 1];
            TestBase.AssertTrue(last.Contains("and hits for 12 damage", System.StringComparison.Ordinal),
                "ReplaceLast should write the punchline onto the same line",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        private static void TestReplaceAtFromEndLeavesNeighborLines()
        {
            Console.WriteLine("--- ReplaceAtFromEnd writes a reserved row without moving neighbors ---");

            var buffer = new DisplayBuffer();
            buffer.Add(new List<ColoredText> { new ColoredText("setup", Colors.White) }, UIMessageType.Combat);
            buffer.Add(new List<ColoredText>(), UIMessageType.Combat);
            buffer.Add(new List<ColoredText>(), UIMessageType.Combat);

            buffer.ReplaceAtFromEnd(2, new List<ColoredText> { new ColoredText("headline", Colors.White) }, UIMessageType.Combat);
            buffer.ReplaceAtFromEnd(1, new List<ColoredText> { new ColoredText("roll", Colors.White) }, UIMessageType.Combat);
            buffer.ReplaceAtFromEnd(0, new List<ColoredText> { new ColoredText("status", Colors.White) }, UIMessageType.Combat);

            TestBase.AssertEqual(3, buffer.Count,
                "ReplaceAtFromEnd should not add lines",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
            TestBase.AssertEqual("headline", buffer.MessagesAsStrings[buffer.Count - 3],
                "Offset 2 should be the headline",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
            TestBase.AssertEqual("roll", buffer.MessagesAsStrings[buffer.Count - 2],
                "Offset 1 should be the first follow-up",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
            TestBase.AssertEqual("status", buffer.MessagesAsStrings[buffer.Count - 1],
                "Offset 0 should be the last follow-up",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        private static void TestReplaceAtFromEndOutOfRangeIsNoOp()
        {
            Console.WriteLine("--- ReplaceAtFromEnd out of range is a no-op ---");

            var buffer = new DisplayBuffer();
            buffer.Add(new List<ColoredText> { new ColoredText("only", Colors.White) }, UIMessageType.Combat);
            buffer.ReplaceAtFromEnd(5, new List<ColoredText> { new ColoredText("nope", Colors.White) }, UIMessageType.Combat);

            TestBase.AssertEqual(1, buffer.Count,
                "Out-of-range replace should not grow the buffer",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
            TestBase.AssertEqual("only", buffer.MessagesAsStrings[0],
                "Out-of-range replace should leave the existing line",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        private static void TestReservationDumpCountMatchesFollowUps()
        {
            Console.WriteLine("--- Reservation dump is setup plus one blank per follow-up ---");

            var setup = new List<ColoredText> { new ColoredText("Angus Attacks Goblin...", Colors.White) };
            var followUps = new List<(List<ColoredText> segments, UIMessageType messageType)>
            {
                (new List<ColoredText> { new ColoredText("     (roll: 14)", Colors.White) }, UIMessageType.Combat),
                (new List<ColoredText> { new ColoredText("     poisoned", Colors.White) }, UIMessageType.Combat),
                (null!, UIMessageType.Combat)
            };

            var reserved = SetupPunchlineReservation.NonNullFollowUps(followUps);
            var dump = SetupPunchlineReservation.BuildInitialDump(setup, UIMessageType.Combat, reserved);

            TestBase.AssertEqual(2, reserved.Count,
                "Null follow-ups should not reserve a row",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
            TestBase.AssertEqual(3, dump.Count,
                "Dump should be setup plus reserved blanks",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
            TestBase.AssertEqual(0, dump[1].segments.Count,
                "Reserved follow-up rows should start blank",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
            TestBase.AssertEqual(2, SetupPunchlineReservation.HeadlineOffsetFromEnd(reserved.Count),
                "Headline offset should equal reserved follow-up count",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
            TestBase.AssertEqual(1, SetupPunchlineReservation.FollowUpOffsetFromEnd(reserved.Count, 0),
                "First follow-up fills the row above the last reserved line",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
            TestBase.AssertEqual(0, SetupPunchlineReservation.FollowUpOffsetFromEnd(reserved.Count, 1),
                "Last follow-up fills the last reserved line",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        private static void TestReservedFollowUpsFillInPlaceWithoutGrowingCount()
        {
            Console.WriteLine("--- Reserved follow-ups fill in place ---");

            var setup = new List<ColoredText> { new ColoredText("Angus Attacks Goblin...", Colors.White) };
            var complete = new List<ColoredText> { new ColoredText("Angus Attacks Goblin... and hits for 12 damage", Colors.White) };
            var reserved = new List<(List<ColoredText> segments, UIMessageType messageType)>
            {
                (new List<ColoredText> { new ColoredText("     (roll: 14)", Colors.White) }, UIMessageType.Combat),
                (new List<ColoredText> { new ColoredText("     (ON HIT feeds +1 GRAZE → 1)", Colors.White) }, UIMessageType.Combat)
            };

            var dump = SetupPunchlineReservation.BuildInitialDump(setup, UIMessageType.Combat, reserved);
            var buffer = new DisplayBuffer();
            foreach (var (segments, messageType) in dump)
                buffer.Add(segments, messageType);

            int countAfterReserve = buffer.Count;
            int n = reserved.Count;
            buffer.ReplaceAtFromEnd(SetupPunchlineReservation.HeadlineOffsetFromEnd(n), complete, UIMessageType.Combat);
            for (int i = 0; i < n; i++)
            {
                var (segments, messageType) = reserved[i];
                buffer.ReplaceAtFromEnd(SetupPunchlineReservation.FollowUpOffsetFromEnd(n, i), segments, messageType);
            }

            TestBase.AssertEqual(countAfterReserve, buffer.Count,
                "Filling reserved rows should not add lines",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
            TestBase.AssertEqual(3, buffer.Count,
                "Block should occupy setup plus two follow-up rows",
                ref _testsRun, ref _testsPassed, ref _testsFailed);

            var lines = buffer.MessagesAsStrings;
            TestBase.AssertTrue(lines[lines.Count - 3].Contains("and hits for 12 damage", System.StringComparison.Ordinal),
                "Headline row should become the punchline",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
            TestBase.AssertTrue(lines[lines.Count - 2].Contains("(roll: 14)", System.StringComparison.Ordinal),
                "First reserved row should become the roll footer",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
            TestBase.AssertTrue(lines[lines.Count - 1].Contains("feeds +1 GRAZE", System.StringComparison.Ordinal),
                "Second reserved row should become the feed line",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
        }
    }
}
