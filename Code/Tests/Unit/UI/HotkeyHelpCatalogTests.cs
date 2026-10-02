using System;
using RPGGame.Tests;
using RPGGame.UI.Avalonia.Help;
using RPGGame.UI.Avalonia.Layout;

namespace RPGGame.Tests.Unit.UI
{
    /// <summary>
    /// Hotkey help catalog + bottom-pad footer hint used by the H overlay.
    /// </summary>
    public static class HotkeyHelpCatalogTests
    {
        private static int _run, _passed, _failed;

        public static void RunAllTests()
        {
            Console.WriteLine("=== HotkeyHelpCatalog Tests ===\n");
            _run = _passed = _failed = 0;

            TestFooterHintCopy();
            TestFooterHintRowIsOuterBottomPad();
            TestFooterHintIsRightAligned();
            TestCatalogListsPlayHotkeys();
            TestFormatHelpBodyIncludesSections();

            TestBase.PrintSummary("HotkeyHelpCatalog Tests", _run, _passed, _failed);
        }

        private static void TestFooterHintCopy()
        {
            Console.WriteLine("--- Footer hint reads Press H for Help ---");
            TestBase.AssertEqual(
                "Press H for Help",
                HotkeyHelpCatalog.FooterHint,
                "footer hint copy",
                ref _run, ref _passed, ref _failed);
        }

        private static void TestFooterHintRowIsOuterBottomPad()
        {
            Console.WriteLine("--- Footer hint row sits in outer bottom pad ---");
            TestBase.AssertEqual(
                LayoutConstants.LEFT_PANEL_HEIGHT,
                HotkeyHelpCatalog.FooterHintRow,
                "footer uses first row below panel borders",
                ref _run, ref _passed, ref _failed);
            TestBase.AssertEqual(
                CanvasGridSizer.OuterPaddingBottom,
                1,
                "design still reserves one bottom pad row",
                ref _run, ref _passed, ref _failed);
        }

        private static void TestFooterHintIsRightAligned()
        {
            Console.WriteLine("--- Footer hint start column is right-aligned ---");
            int hintLen = HotkeyHelpCatalog.FooterHint.Length;
            TestBase.AssertEqual(
                100 - hintLen,
                HotkeyHelpCatalog.ResolveFooterHintStartColumn(100, hintLen),
                "ends flush with layout right edge",
                ref _run, ref _passed, ref _failed);
            TestBase.AssertEqual(
                0,
                HotkeyHelpCatalog.ResolveFooterHintStartColumn(5, hintLen),
                "clamps when edge is narrower than hint",
                ref _run, ref _passed, ref _failed);
            int liveEdge = LayoutConstants.RIGHT_PANEL_X + LayoutConstants.RIGHT_PANEL_WIDTH;
            TestBase.AssertEqual(
                HotkeyHelpCatalog.ResolveFooterHintStartColumn(liveEdge, hintLen),
                HotkeyHelpCatalog.ResolveFooterHintStartColumn(),
                "live resolve matches right-panel edge",
                ref _run, ref _passed, ref _failed);
        }

        private static void TestCatalogListsPlayHotkeys()
        {
            Console.WriteLine("--- Catalog covers F7, speed, copy, inventory shortcuts ---");
            TestBase.AssertTrue(HotkeyHelpCatalog.MentionsKey("F3"),
                "lists F3 font cycle", ref _run, ref _passed, ref _failed);
            TestBase.AssertTrue(HotkeyHelpCatalog.MentionsKey("Ctrl+/-"),
                "lists Ctrl+/- UI size", ref _run, ref _passed, ref _failed);
            TestBase.AssertTrue(HotkeyHelpCatalog.MentionsKey("Ctrl+0"),
                "lists Ctrl+0 UI size reset", ref _run, ref _passed, ref _failed);
            TestBase.AssertTrue(HotkeyHelpCatalog.MentionsKey("Ctrl+Shift+0"),
                "lists Ctrl+Shift+0 set default UI size", ref _run, ref _passed, ref _failed);
            TestBase.AssertTrue(HotkeyHelpCatalog.MentionsKey("F5"),
                "lists F5 video feed", ref _run, ref _passed, ref _failed);
            TestBase.AssertTrue(HotkeyHelpCatalog.MentionsKey("F7"),
                "lists F7 narrative log", ref _run, ref _passed, ref _failed);
            TestBase.AssertTrue(HotkeyHelpCatalog.MentionsKey("F6"),
                "lists F6 distortion toggle", ref _run, ref _passed, ref _failed);
            TestBase.AssertTrue(HotkeyHelpCatalog.MentionsKey("Page Up"),
                "lists Page Up combat speed", ref _run, ref _passed, ref _failed);
            TestBase.AssertTrue(HotkeyHelpCatalog.MentionsKey("Ctrl+C"),
                "lists combat log copy", ref _run, ref _passed, ref _failed);
            TestBase.AssertTrue(HotkeyHelpCatalog.MentionsKey("F8"),
                "lists F8 Action Lab", ref _run, ref _passed, ref _failed);
            TestBase.AssertTrue(HotkeyHelpCatalog.MentionsKey("Numpad +"),
                "lists inventory sort", ref _run, ref _passed, ref _failed);
            TestBase.AssertTrue(HotkeyHelpCatalog.MentionsKey("Mouse wheel (over left panel)"),
                "lists left-panel wheel scroll", ref _run, ref _passed, ref _failed);
            TestBase.AssertTrue(HotkeyHelpCatalog.Entries.Count >= 10,
                "enough entries for a useful help sheet", ref _run, ref _passed, ref _failed);
        }

        private static void TestFormatHelpBodyIncludesSections()
        {
            Console.WriteLine("--- FormatHelpBody groups by section ---");
            string body = HotkeyHelpCatalog.FormatHelpBody();
            TestBase.AssertTrue(body.Contains("COMBAT", StringComparison.Ordinal),
                "includes COMBAT section", ref _run, ref _passed, ref _failed);
            TestBase.AssertTrue(body.Contains("narrative", StringComparison.OrdinalIgnoreCase),
                "describes narrative log", ref _run, ref _passed, ref _failed);
            TestBase.AssertTrue(body.Contains("1x", StringComparison.Ordinal)
                    || body.Contains("speed", StringComparison.OrdinalIgnoreCase),
                "describes combat speed", ref _run, ref _passed, ref _failed);
        }
    }
}
