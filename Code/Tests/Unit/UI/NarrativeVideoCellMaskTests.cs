using System;
using Avalonia.Media;
using RPGGame;
using RPGGame.Tests;
using RPGGame.UI.Avalonia.Effects;

namespace RPGGame.Tests.Unit.UI
{
    /// <summary>
    /// Unit tests for per-cell narrative video opacity mask helpers.
    /// </summary>
    public static class NarrativeVideoCellMaskTests
    {
        private static int _testsRun;
        private static int _testsPassed;
        private static int _testsFailed;

        public static void RunAllTests()
        {
            Console.WriteLine("=== NarrativeVideoCellMask Tests ===\n");
            _testsRun = _testsPassed = _testsFailed = 0;

            Luminance_WhiteIsOne_BlackIsZero();
            CellOpacity_WhitespaceIsZero();
            CellOpacity_ScalesByMaxOpacity();
            HasOccupiedGlyphs_EmptyVsContent();
            BuildOpacityGrid_AppliesHalo();
            WriteOpacityMaskBgra_WritesAlphaBlocks();
            FrameLayout_AlignsPitchAndLinesTo32();
            FrameLayout_BufferCoversAlignedStorage();
            Gate_RequiresDungeonWhenConfigured();
            Gate_RequiresNarrativeLogWhenConfigured();
            Gate_BlocksVictoryAndDeathScreens();
            Gate_RequiresLogContentToShow();
            Config_Normalize_ClampsOpacityAndLevels();
            FingerprintOpacityGrid_StableForSameValues();
            FingerprintOpacityGrid_ChangesWhenCellChanges();

            TestBase.PrintSummary("NarrativeVideoCellMask Tests", _testsRun, _testsPassed, _testsFailed);
        }

        private static void Luminance_WhiteIsOne_BlackIsZero()
        {
            Console.WriteLine("--- Luminance white/black ---");
            TestBase.AssertTrue(
                Math.Abs(NarrativeVideoCellMask.Luminance(255, 255, 255) - 1.0) < 0.001,
                "white luminance ~1", ref _testsRun, ref _testsPassed, ref _testsFailed);
            TestBase.AssertTrue(
                NarrativeVideoCellMask.Luminance(0, 0, 0) < 0.001,
                "black luminance ~0", ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        private static void CellOpacity_WhitespaceIsZero()
        {
            Console.WriteLine("--- Whitespace cell opacity ---");
            float a = NarrativeVideoCellMask.CellOpacity(' ', Colors.White, 1.0);
            TestBase.AssertEqual(0f, a, "space opacity 0", ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        private static void CellOpacity_ScalesByMaxOpacity()
        {
            Console.WriteLine("--- Max opacity scale ---");
            float a = NarrativeVideoCellMask.CellOpacity('A', Colors.White, 0.5);
            TestBase.AssertTrue(Math.Abs(a - 0.5f) < 0.001f, "white@0.5 max", ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        private static void HasOccupiedGlyphs_EmptyVsContent()
        {
            Console.WriteLine("--- Occupied glyphs gate ---");
            var empty = new char[] { ' ', '\0', '\t', '\n' };
            TestBase.AssertTrue(
                !NarrativeVideoCellMask.HasOccupiedGlyphs(empty),
                "whitespace-only is empty",
                ref _testsRun, ref _testsPassed, ref _testsFailed);

            var withText = new char[] { ' ', 'A', ' ' };
            TestBase.AssertTrue(
                NarrativeVideoCellMask.HasOccupiedGlyphs(withText),
                "letter counts as content",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        private static void BuildOpacityGrid_AppliesHalo()
        {
            Console.WriteLine("--- Halo dilation ---");
            int w = 3, h = 3;
            var glyphs = new char[9];
            var colors = new Color[9];
            Array.Fill(glyphs, ' ');
            Array.Fill(colors, Colors.White);
            glyphs[4] = 'X'; // center
            var dest = new float[9];
            NarrativeVideoCellMask.BuildOpacityGrid(dest, w, h, glyphs, colors, maxOpacity: 1.0, haloCells: 1, haloOpacityScale: 0.5);
            TestBase.AssertTrue(dest[4] > 0.9f, "center occupied", ref _testsRun, ref _testsPassed, ref _testsFailed);
            TestBase.AssertTrue(dest[1] > 0.4f && dest[1] < 0.6f, "north halo ~0.5", ref _testsRun, ref _testsPassed, ref _testsFailed);
            TestBase.AssertTrue(dest[0] > 0.4f && dest[0] < 0.6f, "corner halo ~0.5", ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        private static void WriteOpacityMaskBgra_WritesAlphaBlocks()
        {
            Console.WriteLine("--- BGRA mask blocks ---");
            int gw = 2, gh = 1, cw = 2, ch = 1;
            var grid = new float[] { 1f, 0f };
            var pixels = new byte[gw * gh * cw * ch * 4];
            NarrativeVideoCellMask.WriteOpacityMaskBgra(pixels, gw * cw, gh * ch, grid, gw, gh, cw, ch);
            TestBase.AssertEqual(255, (int)pixels[3], "first cell alpha", ref _testsRun, ref _testsPassed, ref _testsFailed);
            TestBase.AssertEqual(255, (int)pixels[7], "first cell col2 alpha", ref _testsRun, ref _testsPassed, ref _testsFailed);
            TestBase.AssertEqual(0, (int)pixels[11], "second cell alpha", ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        private static void FrameLayout_AlignsPitchAndLinesTo32()
        {
            Console.WriteLine("--- RV32 pitch/lines align ---");
            TestBase.AssertTrue(NarrativeVideoFrameLayout.Align32(1) == 32u, "align 1→32", ref _testsRun, ref _testsPassed, ref _testsFailed);
            TestBase.AssertTrue(NarrativeVideoFrameLayout.Align32(32) == 32u, "align 32→32", ref _testsRun, ref _testsPassed, ref _testsFailed);
            TestBase.AssertTrue(NarrativeVideoFrameLayout.Align32(33) == 64u, "align 33→64", ref _testsRun, ref _testsPassed, ref _testsFailed);

            NarrativeVideoFrameLayout.ComputeRv32Storage(1920, 1080, out uint pitches, out uint lines, out _);
            TestBase.AssertTrue(pitches == 1920u * 4, "1920 pitch exact", ref _testsRun, ref _testsPassed, ref _testsFailed);
            TestBase.AssertTrue(lines == 1088u, "1080 lines→1088", ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        private static void FrameLayout_BufferCoversAlignedStorage()
        {
            Console.WriteLine("--- RV32 buffer bytes ---");
            // Odd sizes must allocate pitches*lines, not width*4*height (the old undersize bug).
            NarrativeVideoFrameLayout.ComputeRv32Storage(100, 50, out uint pitches, out uint lines, out int bytes);
            TestBase.AssertTrue(pitches >= 100u * 4 && pitches % 32 == 0, "pitch padded", ref _testsRun, ref _testsPassed, ref _testsFailed);
            TestBase.AssertTrue(lines >= 50 && lines % 32 == 0, "lines padded", ref _testsRun, ref _testsPassed, ref _testsFailed);
            TestBase.AssertEqual((int)(pitches * lines), bytes, "bytes=pitches*lines", ref _testsRun, ref _testsPassed, ref _testsFailed);
            TestBase.AssertTrue(bytes > 100 * 4 * 50, "larger than naive width*height", ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        private static void Gate_RequiresDungeonWhenConfigured()
        {
            Console.WriteLine("--- Gate requires dungeon ---");
            TestBase.AssertTrue(
                !NarrativeVideoOverlayGate.ShouldAllowOverlayMode(
                    enabled: true, disposed: false,
                    onlyWhenNarrativeLog: true, isNarrativeCombatLog: true,
                    onlyWhenInDungeon: true, inDungeonRun: false),
                "blocks outside dungeon",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
            TestBase.AssertTrue(
                NarrativeVideoOverlayGate.ShouldAllowOverlayMode(
                    enabled: true, disposed: false,
                    onlyWhenNarrativeLog: true, isNarrativeCombatLog: true,
                    onlyWhenInDungeon: true, inDungeonRun: true),
                "allows in dungeon + narrative",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        private static void Gate_RequiresNarrativeLogWhenConfigured()
        {
            Console.WriteLine("--- Gate requires narrative log ---");
            TestBase.AssertTrue(
                !NarrativeVideoOverlayGate.ShouldAllowOverlayMode(
                    enabled: true, disposed: false,
                    onlyWhenNarrativeLog: true, isNarrativeCombatLog: false,
                    onlyWhenInDungeon: true, inDungeonRun: true),
                "blocks when F7 off",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        private static void Gate_BlocksVictoryAndDeathScreens()
        {
            Console.WriteLine("--- Gate blocks victory/death ---");
            TestBase.AssertTrue(
                !NarrativeVideoOverlayGate.CountsAsActiveDungeonRunForOverlay(
                    hasCurrentDungeon: true, state: GameState.DungeonCompletion),
                "victory screen not active run",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
            TestBase.AssertTrue(
                !NarrativeVideoOverlayGate.CountsAsActiveDungeonRunForOverlay(
                    hasCurrentDungeon: true, state: GameState.Death),
                "death screen not active run",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
            TestBase.AssertTrue(
                NarrativeVideoOverlayGate.CountsAsActiveDungeonRunForOverlay(
                    hasCurrentDungeon: true, state: GameState.Combat),
                "combat still active run",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
            TestBase.AssertTrue(
                NarrativeVideoOverlayGate.CountsAsActiveDungeonRunForOverlay(
                    hasCurrentDungeon: true, state: GameState.Dungeon),
                "dungeon exploration still active run",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
            TestBase.AssertTrue(
                !NarrativeVideoOverlayGate.CountsAsActiveDungeonRunForOverlay(
                    hasCurrentDungeon: false, state: GameState.Combat),
                "no dungeon membership blocks",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
            TestBase.AssertTrue(
                !NarrativeVideoOverlayGate.ShouldAllowOverlayMode(
                    enabled: true, disposed: false,
                    onlyWhenNarrativeLog: true, isNarrativeCombatLog: true,
                    onlyWhenInDungeon: true,
                    inDungeonRun: NarrativeVideoOverlayGate.CountsAsActiveDungeonRunForOverlay(
                        true, GameState.DungeonCompletion)),
                "mode gate blocks victory via provider",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        private static void Gate_RequiresLogContentToShow()
        {
            Console.WriteLine("--- Gate requires log content ---");
            TestBase.AssertTrue(
                !NarrativeVideoOverlayGate.ShouldShowOverlay(allowOverlayMode: true, logHasContent: false),
                "no show without glyphs",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
            TestBase.AssertTrue(
                NarrativeVideoOverlayGate.ShouldShowOverlay(allowOverlayMode: true, logHasContent: true),
                "show with glyphs",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
            TestBase.AssertTrue(
                !NarrativeVideoOverlayGate.ShouldShowOverlay(allowOverlayMode: false, logHasContent: true),
                "no show when mode blocked",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        private static void Config_Normalize_ClampsOpacityAndLevels()
        {
            Console.WriteLine("--- Config normalize clamps settings knobs ---");
            var raw = new NarrativeVideoOverlayConfig
            {
                MaxOpacity = 1.5,
                HaloOpacityScale = -0.2,
                HaloCells = 99,
                FileName = " "
            };
            var n = NarrativeVideoOverlayConfig.Normalize(raw);
            TestBase.AssertEqual(1.0, n.MaxOpacity, "opacity clamped to 1",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
            TestBase.AssertEqual(0.0, n.HaloOpacityScale, "blurry opacity clamped to 0",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
            TestBase.AssertEqual(8, n.HaloCells, "video levels clamped to 8",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
            TestBase.AssertEqual("Video/videoplayback.mp4", n.FileName, "blank file falls back",
                ref _testsRun, ref _testsPassed, ref _testsFailed);

            var defaults = NarrativeVideoOverlayConfig.Normalize(null);
            TestBase.AssertEqual(0.45, defaults.MaxOpacity, "default opacity 0.45",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
            TestBase.AssertEqual(0.35, defaults.HaloOpacityScale, "default blurry opacity 0.35",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
            TestBase.AssertEqual(1, defaults.HaloCells, "default video levels 1",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        private static void FingerprintOpacityGrid_StableForSameValues()
        {
            Console.WriteLine("--- Opacity fingerprint stable ---");
            var a = new float[] { 0f, 0.5f, 1f, 0.25f };
            var b = new float[] { 0f, 0.5f, 1f, 0.25f };
            int ha = NarrativeVideoCellMask.FingerprintOpacityGrid(a, a.Length);
            int hb = NarrativeVideoCellMask.FingerprintOpacityGrid(b, b.Length);
            TestBase.AssertEqual(ha, hb, "identical grids same fingerprint",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
            TestBase.AssertEqual(0, NarrativeVideoCellMask.FingerprintOpacityGrid(ReadOnlySpan<float>.Empty, 0),
                "empty grid fingerprint 0",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        private static void FingerprintOpacityGrid_ChangesWhenCellChanges()
        {
            Console.WriteLine("--- Opacity fingerprint detects change ---");
            var a = new float[] { 0f, 0.5f, 1f, 0.25f };
            var b = new float[] { 0f, 0.5f, 0.9f, 0.25f };
            int ha = NarrativeVideoCellMask.FingerprintOpacityGrid(a, a.Length);
            int hb = NarrativeVideoCellMask.FingerprintOpacityGrid(b, b.Length);
            TestBase.AssertTrue(ha != hb, "changed cell changes fingerprint",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
        }
    }
}
