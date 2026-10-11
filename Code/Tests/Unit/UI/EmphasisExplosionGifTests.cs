using System;
using System.Collections.Generic;
using System.IO;
using Avalonia.Media;
using RPGGame;
using RPGGame.Tests;
using RPGGame.UI.Avalonia.Canvas;
using RPGGame.UI.Avalonia.Effects;
using RPGGame.UI.Avalonia.Layout;
using SkiaSharp;

namespace RPGGame.Tests.Unit.UI
{
    public static class EmphasisExplosionGifTests
    {
        private static int _run, _passed, _failed;

        public static void RunAllTests()
        {
            Console.WriteLine("=== EmphasisExplosionGif Tests ===\n");
            _run = _passed = _failed = 0;

            bool prevDistortion = DeveloperModeState.AreDistortionEffectsEnabled;
            try
            {
                DeveloperModeState.SetDistortionEffectsEnabled(true);
                TestDecoderLoadsExplosionGif();
                TestKeyOutClearsNearBlack();
                TestPlayerTriggerPlaysThenEnds();
                TestPlayerDisabledNoOp();
                TestOpacityCutsInThenFadesOut();
                TestDefaultDiameterIsTriplePrior();
                TestLetterMaskOnlyOccupiedGlyphs();
            }
            finally
            {
                DeveloperModeState.SetDistortionEffectsEnabled(prevDistortion);
            }

            TestBase.PrintSummary("EmphasisExplosionGif Tests", _run, _passed, _failed);
        }

        private static string? ResolveExplosionPath()
        {
            return GameConstants.TryGetExistingGameDataFilePath("Video/explosion.gif")
                ?? GameConstants.TryGetExistingGameDataFilePath(Path.Combine("Video", "explosion.gif"));
        }

        private static void TestDecoderLoadsExplosionGif()
        {
            Console.WriteLine("--- Decoder loads GameData/Video/explosion.gif ---");
            string? path = ResolveExplosionPath();
            TestBase.AssertTrue(path != null && File.Exists(path),
                $"explosion.gif present (got {path ?? "null"})",
                ref _run, ref _passed, ref _failed);
            if (path == null)
                return;

            bool ok = EmphasisExplosionGifDecoder.TryLoad(path, out var frames, out string? error);
            TestBase.AssertTrue(ok, $"decode ok (err={error})", ref _run, ref _passed, ref _failed);
            TestBase.AssertTrue(frames.Count >= 2, $"multi-frame GIF (got {frames.Count})", ref _run, ref _passed, ref _failed);
            if (ok && frames.Count > 0)
            {
                TestBase.AssertTrue(frames[0].Width > 0 && frames[0].Height > 0,
                    "frame has size", ref _run, ref _passed, ref _failed);
                TestBase.AssertTrue(frames[0].BgraPremul.Length >= frames[0].Width * frames[0].Height * 4,
                    "frame has pixel buffer", ref _run, ref _passed, ref _failed);
            }
        }

        private static void TestKeyOutClearsNearBlack()
        {
            Console.WriteLine("\n--- Key-out clears near-black ---");
            using var bmp = new SKBitmap(4, 4, SKColorType.Bgra8888, SKAlphaType.Premul);
            bmp.Erase(new SKColor(0, 0, 0, 255));
            bmp.SetPixel(1, 1, new SKColor(255, 200, 0, 255));
            int cleared = EmphasisExplosionGifDecoder.KeyOutNearBlackInPlace(bmp);
            TestBase.AssertTrue(cleared >= 15, $"cleared black pixels (got {cleared})", ref _run, ref _passed, ref _failed);
            var kept = bmp.GetPixel(1, 1);
            TestBase.AssertTrue(kept.Red > 200 && kept.Alpha > 200,
                "yellow pixel preserved",
                ref _run, ref _passed, ref _failed);
            var corner = bmp.GetPixel(0, 0);
            TestBase.AssertTrue(corner.Alpha == 0, "black corner transparent", ref _run, ref _passed, ref _failed);
        }

        private static void TestPlayerTriggerPlaysThenEnds()
        {
            Console.WriteLine("\n--- Player trigger plays then ends ---");
            var player = new TextEmphasisExplosionPlayer();
            player.ApplyConfig(new TextEmphasisPulseConfig
            {
                Enabled = true,
                ExplosionGifEnabled = true,
                ExplosionGifFileName = "Video/explosion.gif",
                ExplosionDiameterCells = 8
            });
            player.SetTestClockSeconds(0);

            bool started = player.Trigger(100, 80, 10, 16);
            TestBase.AssertTrue(started, $"trigger starts playback (err={player.LastLoadError})", ref _run, ref _passed, ref _failed);
            TestBase.AssertTrue(player.FrameCountForTests >= 2,
                $"raw frames loaded (got {player.FrameCountForTests})",
                ref _run, ref _passed, ref _failed);
            TestBase.AssertTrue(player.IsActive, "active after trigger", ref _run, ref _passed, ref _failed);
            player.TickForTests(120); // far past typical GIF length
            TestBase.AssertTrue(!player.IsActive, "inactive after duration", ref _run, ref _passed, ref _failed);
        }

        private static void TestPlayerDisabledNoOp()
        {
            Console.WriteLine("\n--- Player disabled no-op ---");
            var player = new TextEmphasisExplosionPlayer();
            player.ApplyConfig(new TextEmphasisPulseConfig
            {
                Enabled = true,
                ExplosionGifEnabled = false,
                ExplosionGifFileName = "Video/explosion.gif"
            });
            bool started = player.Trigger(50, 50, 10, 16);
            TestBase.AssertTrue(!started, "disabled gif does not start", ref _run, ref _passed, ref _failed);
            TestBase.AssertTrue(!player.IsActive, "stays inactive", ref _run, ref _passed, ref _failed);
        }

        private static void TestOpacityCutsInThenFadesOut()
        {
            Console.WriteLine("\n--- Opacity cuts in then fades out in 0.5s ---");
            const int fadeMs = 500;
            double atStart = TextEmphasisExplosionPlayer.ResolveOpacity(0, fadeMs);
            double atMid = TextEmphasisExplosionPlayer.ResolveOpacity(250, fadeMs);
            double atEnd = TextEmphasisExplosionPlayer.ResolveOpacity(499, fadeMs);
            double pastEnd = TextEmphasisExplosionPlayer.ResolveOpacity(500, fadeMs);

            TestBase.AssertTrue(Math.Abs(atStart - 1.0) < 1e-9, $"cut-in at full opacity (got {atStart})", ref _run, ref _passed, ref _failed);
            TestBase.AssertTrue(Math.Abs(atMid - 0.5) < 1e-9, $"mid fade ~0.5 (got {atMid})", ref _run, ref _passed, ref _failed);
            TestBase.AssertTrue(atEnd > 0.0 && atEnd < 0.01, $"near-zero before end (got {atEnd})", ref _run, ref _passed, ref _failed);
            TestBase.AssertTrue(pastEnd == 0.0, "zero at 0.5s", ref _run, ref _passed, ref _failed);

            var player = new TextEmphasisExplosionPlayer();
            player.ApplyConfig(new TextEmphasisPulseConfig
            {
                Enabled = true,
                ExplosionGifEnabled = true,
                ExplosionGifFileName = "Video/explosion.gif",
                ExplosionFadeOutSeconds = 0.5
            });
            player.SetTestClockSeconds(0);
            bool started = player.Trigger(100, 80, 10, 16);
            TestBase.AssertTrue(started, $"trigger for fade window (err={player.LastLoadError})", ref _run, ref _passed, ref _failed);
            TestBase.AssertTrue(player.IsActive, "active at cut-in", ref _run, ref _passed, ref _failed);
            TestBase.AssertTrue(Math.Abs(player.GetOpacityForTests() - 1.0) < 1e-9, "opacity 1 at t=0", ref _run, ref _passed, ref _failed);
            player.TickForTests(0.25);
            TestBase.AssertTrue(player.IsActive, "still active at 0.25s", ref _run, ref _passed, ref _failed);
            TestBase.AssertTrue(Math.Abs(player.GetOpacityForTests() - 0.5) < 1e-9,
                $"opacity ~0.5 at 0.25s (got {player.GetOpacityForTests()})",
                ref _run, ref _passed, ref _failed);
            player.TickForTests(0.5);
            TestBase.AssertTrue(!player.IsActive, "gone at 0.5s", ref _run, ref _passed, ref _failed);
        }

        private static void TestDefaultDiameterIsTriplePrior()
        {
            Console.WriteLine("\n--- Default diameter 60 / fade 0.5s ---");
            var cfg = new TextEmphasisPulseConfig();
            TestBase.AssertTrue(Math.Abs(cfg.ExplosionDiameterCells - 60.0) < 1e-9,
                $"default diameter 60 (got {cfg.ExplosionDiameterCells})",
                ref _run, ref _passed, ref _failed);
            TestBase.AssertTrue(Math.Abs(cfg.ExplosionFadeOutSeconds - 0.5) < 1e-9,
                $"default fade-out 0.5s (got {cfg.ExplosionFadeOutSeconds})",
                ref _run, ref _passed, ref _failed);
        }

        private static void TestLetterMaskOnlyOccupiedGlyphs()
        {
            Console.WriteLine("\n--- Letter mask only occupied glyphs ---");
            LayoutConstants.UpdateGridDimensions(100, 40);
            int x = LayoutConstants.CENTER_PANEL_X + 2;
            int y = LayoutConstants.CENTER_PANEL_Y + 5;
            var texts = new List<CanvasText>
            {
                new CanvasText { X = x, Y = y, Content = "SLAM", Color = Colors.White },
                new CanvasText
                {
                    X = x, Y = y + 1, Content = "skip", Color = Colors.White, IsOverlay = true
                }
            };

            EmphasisExplosionLetterMask.ResolveGridWindow(
                x * 10.0, y * 16.0, (x + 8) * 10.0, (y + 2) * 16.0,
                10, 16,
                out int originCol, out int originRow, out int cols, out int rows);

            TestBase.AssertTrue(cols >= 4 && rows >= 1, $"grid window ({cols}x{rows})", ref _run, ref _passed, ref _failed);

            var grid = new float[cols * rows];
            EmphasisExplosionLetterMask.BuildOccupiedGlyphOpacity(
                grid, originCol, originRow, cols, rows, texts, maxOpacity: 1.0);

            int slamLocalX = x - originCol;
            int slamLocalY = y - originRow;
            float slamOpacity = grid[slamLocalY * cols + slamLocalX];
            TestBase.AssertTrue(slamOpacity > 0.5f, $"SLAM cell opaque (got {slamOpacity})", ref _run, ref _passed, ref _failed);

            // Space after SLAM within window (if present) and overlay row stay zero.
            int afterWord = slamLocalX + 4;
            if (afterWord < cols)
            {
                TestBase.AssertTrue(grid[slamLocalY * cols + afterWord] <= 0.001f,
                    "empty cell after word is transparent",
                    ref _run, ref _passed, ref _failed);
            }

            int overlayLocalY = (y + 1) - originRow;
            if (overlayLocalY >= 0 && overlayLocalY < rows)
            {
                float overlaySum = 0f;
                for (int gx = 0; gx < cols; gx++)
                    overlaySum += grid[overlayLocalY * cols + gx];
                TestBase.AssertTrue(overlaySum <= 0.001f, "overlay glyphs excluded from mask", ref _run, ref _passed, ref _failed);
            }
        }
    }
}
