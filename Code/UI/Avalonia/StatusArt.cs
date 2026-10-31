using System;
using System.Collections.Generic;
using System.IO;
using Avalonia;
using Avalonia.Media.Imaging;

namespace RPGGame.UI.Avalonia
{
    /// <summary>
    /// Status effect icons picked by df-darkfantasy's <c>build_status_icons.py --pick</c>: one flat one-colour glyph per
    /// effect at <c>GameData/StatusArt/&lt;effect&gt;.png</c> (bleed, poison, burn, stun, vulnerability, …). Each bitmap is
    /// loaded the first time it is asked for and kept; a missing file is remembered so it is not retried every frame.
    /// </summary>
    public static class StatusArt
    {
        private const string Folder = "StatusArt";

        private static readonly object Gate = new();
        private static readonly Dictionary<string, Bitmap?> Cache = new(StringComparer.OrdinalIgnoreCase);
        private static string? _dir;

        /// <summary>The icon for <paramref name="effectKey"/> (file name without extension); false when there is none.</summary>
        public static bool TryGetIcon(string? effectKey, out Bitmap bitmap, out Rect sourceRect)
        {
            bitmap = null!;
            sourceRect = default;
            if (string.IsNullOrEmpty(effectKey))
                return false;
            lock (Gate)
            {
                if (!Cache.TryGetValue(effectKey, out var bmp))
                {
                    bmp = Load(effectKey);
                    Cache[effectKey] = bmp;
                }
                if (bmp == null)
                    return false;
                bitmap = bmp;
                sourceRect = new Rect(0, 0, bmp.PixelSize.Width, bmp.PixelSize.Height);
                return true;
            }
        }

        private static Bitmap? Load(string effectKey)
        {
            try
            {
                _dir ??= GameConstants.ResolveGameDataPath(Folder);
                string path = Path.Combine(_dir, effectKey + ".png");
                return File.Exists(path) ? new Bitmap(path) : null;
            }
            catch (Exception ex)
            {
                DebugLogger.Log("StatusArt", $"Could not load {effectKey}.png: {ex.Message}");
                return null;
            }
        }
    }
}
