using System;
using System.Collections.Generic;
using Avalonia.Media;
using RPGGame;
using RPGGame.Tests;
using RPGGame.UI;
using RPGGame.UI.Avalonia.Display;
using RPGGame.UI.ColorSystem;

namespace RPGGame.Tests.Unit.UI
{
    /// <summary>
    /// F7 dual-view: existing buffered swings convert between narrative prose and mechanical log.
    /// </summary>
    public static class CombatLogDualViewTests
    {
        public static void RunAllTests()
        {
            Console.WriteLine("=== CombatLogDualView Tests ===\n");
            int run = 0, passed = 0, failed = 0;

            TestSwapNarrativeToMechanicalExpandsHoverTip(ref run, ref passed, ref failed);
            TestSwapMechanicalToNarrativeCollapsesBlock(ref run, ref passed, ref failed);
            TestRoundTripPreservesProse(ref run, ref passed, ref failed);
            TestPassThroughWithoutAlternate(ref run, ref passed, ref failed);
            TestDisplayBufferSwapRebuildsLines(ref run, ref passed, ref failed);

            TestBase.PrintSummary("CombatLogDualView Tests", run, passed, failed);
        }

        private static void TestSwapNarrativeToMechanicalExpandsHoverTip(ref int run, ref int passed, ref int failed)
        {
            Console.WriteLine("--- Narrative → mechanical expands hover tip ---");
            var prose = Seg("    The hero answers with a chaining SLAM.");
            var tip = new List<List<ColoredText>>
            {
                Seg("Hero Attacks Goblin and hits with SLAM"),
                Seg("(roll: 18 | speed: 1.0)")
            };

            var source = new List<CombatLogDualView.Line>
            {
                new CombatLogDualView.Line(prose, UIMessageType.Combat, tip, dualSpan: 1)
            };

            var swapped = CombatLogDualView.Swap(source, currentlyShowingNarrative: true);
            TestBase.AssertEqual(2, swapped.Count, "Expanded to tip line count", ref run, ref passed, ref failed);
            TestBase.AssertTrue(
                ColoredTextRenderer.RenderAsPlainText(swapped[0].Segments).Contains("SLAM", StringComparison.Ordinal),
                "First mechanical line is action headline",
                ref run, ref passed, ref failed);
            TestBase.AssertEqual(2, swapped[0].DualSpan, "Mechanical anchor span matches tip count", ref run, ref passed, ref failed);
            var alt = swapped[0].AlternateLines;
            TestBase.AssertTrue(
                alt != null
                && alt.Count > 0
                && ColoredTextRenderer.RenderAsPlainText(alt[0]).Contains("hero", StringComparison.OrdinalIgnoreCase),
                "Prose stored as alternate on mechanical anchor",
                ref run, ref passed, ref failed);
        }

        private static void TestSwapMechanicalToNarrativeCollapsesBlock(ref int run, ref int passed, ref int failed)
        {
            Console.WriteLine("--- Mechanical → narrative collapses dual block ---");
            var prose = Seg("    The Wraith gathers its will.");
            var source = new List<CombatLogDualView.Line>
            {
                new CombatLogDualView.Line(
                    Seg("Wraith Attacks Hero and hits with CLAW"),
                    UIMessageType.Combat,
                    new List<List<ColoredText>> { prose },
                    dualSpan: 2),
                new CombatLogDualView.Line(
                    Seg("(roll: 12 | speed: 0.8)"),
                    UIMessageType.RollInfo,
                    alternateLines: null,
                    dualSpan: 0)
            };

            var swapped = CombatLogDualView.Swap(source, currentlyShowingNarrative: false);
            TestBase.AssertEqual(1, swapped.Count, "Collapsed to one prose line", ref run, ref passed, ref failed);
            TestBase.AssertTrue(
                ColoredTextRenderer.RenderAsPlainText(swapped[0].Segments).Contains("Wraith", StringComparison.Ordinal),
                "Prose restored",
                ref run, ref passed, ref failed);
            TestBase.AssertEqual(1, swapped[0].DualSpan, "Narrative anchor span is 1", ref run, ref passed, ref failed);
            TestBase.AssertEqual(2, swapped[0].AlternateLines!.Count, "Mechanical tip restored", ref run, ref passed, ref failed);
        }

        private static void TestRoundTripPreservesProse(ref int run, ref int passed, ref int failed)
        {
            Console.WriteLine("--- Round-trip narrative → mechanical → narrative ---");
            string proseText = "    Poison still courses through the Salamander.";
            var tip = new List<List<ColoredText>>
            {
                Seg("Salamander takes 3 poison damage")
            };
            var start = new List<CombatLogDualView.Line>
            {
                new CombatLogDualView.Line(Seg(proseText), UIMessageType.Combat, tip, dualSpan: 1)
            };

            var mid = CombatLogDualView.Swap(start, currentlyShowingNarrative: true);
            var back = CombatLogDualView.Swap(mid, currentlyShowingNarrative: false);
            TestBase.AssertEqual(1, back.Count, "Back to one line", ref run, ref passed, ref failed);
            TestBase.AssertEqual(
                proseText,
                ColoredTextRenderer.RenderAsPlainText(back[0].Segments),
                "Original prose restored",
                ref run, ref passed, ref failed);
        }

        private static void TestPassThroughWithoutAlternate(ref int run, ref int passed, ref int failed)
        {
            Console.WriteLine("--- Lines without alternate pass through ---");
            var source = new List<CombatLogDualView.Line>
            {
                new CombatLogDualView.Line(Seg("Room cleared!"), UIMessageType.System, null, 0)
            };
            var swapped = CombatLogDualView.Swap(source, currentlyShowingNarrative: true);
            TestBase.AssertEqual(1, swapped.Count, "Still one line", ref run, ref passed, ref failed);
            TestBase.AssertEqual(
                "Room cleared!",
                ColoredTextRenderer.RenderAsPlainText(swapped[0].Segments),
                "Unchanged text",
                ref run, ref passed, ref failed);
        }

        private static void TestDisplayBufferSwapRebuildsLines(ref int run, ref int passed, ref int failed)
        {
            Console.WriteLine("--- DisplayBuffer.SwapDualView rebuilds content ---");
            var buffer = new DisplayBuffer(maxLines: 40, maxLineWidth: 80);
            buffer.Add(Seg("    The hero lunges with SLAM."), UIMessageType.Combat);
            buffer.SetHoverInfoLinesAtFromEnd(0, new List<List<ColoredText>>
            {
                Seg("Hero Attacks Goblin and hits with SLAM"),
                Seg("(roll: 16)")
            });

            TestBase.AssertEqual(1, buffer.Count, "Started with one prose line", ref run, ref passed, ref failed);
            buffer.SwapDualView(currentlyShowingNarrative: true);
            TestBase.AssertEqual(2, buffer.Count, "Swapped to mechanical line count", ref run, ref passed, ref failed);
            TestBase.AssertTrue(
                buffer.MessagesAsStrings[0].Contains("Hero Attacks", StringComparison.Ordinal),
                "Mechanical headline visible",
                ref run, ref passed, ref failed);

            buffer.SwapDualView(currentlyShowingNarrative: false);
            TestBase.AssertEqual(1, buffer.Count, "Swapped back to prose", ref run, ref passed, ref failed);
            TestBase.AssertTrue(
                buffer.MessagesAsStrings[0].Contains("lunges", StringComparison.Ordinal),
                "Prose restored in buffer",
                ref run, ref passed, ref failed);
        }

        private static List<ColoredText> Seg(string text) =>
            new List<ColoredText> { new ColoredText(text, Colors.White) };
    }
}
