using System;
using System.Collections.Generic;
using System.IO;
using Avalonia;
using Avalonia.Media.Imaging;

namespace RPGGame.UI.Avalonia
{
    /// <summary>
    /// Status effect icons made by df-darkfantasy's <c>build_status_icons.py</c>: one transparent PNG per effect,
    /// <c>GameData/StatusArt/&lt;effect&gt;.png</c> (bleed, poison, burn, ...), decoded on first use.
    /// The HUD's status lines (<see cref="Layout.StatusEffectDisplayLines"/>) map to an effect by their leading word.
    /// </summary>
    public static class StatusIconArt
    {
        private const string Folder = "StatusArt";

        /// <summary>HUD line prefix -> icon file name, longest prefixes first so "HP Regen" is not read as something shorter.</summary>
        private static readonly (string Prefix, string Effect)[] LinePrefixes =
        {
            ("Armor brk", "armorbreak"),
            ("Stat drain", "statdrain"),
            ("HP Regen", "hpregen"),
            ("Temp HP", "temporaryhp"),
            ("Weakened", "weaken"),
            ("Stunned", "stun"),
            ("Poison", "poison"),
            ("Bleed", "bleed"),
            ("Burn", "burn"),
            ("Acid", "acid"),
            ("Vuln", "vulnerability"),
            ("Harden", "harden"),
            ("Fortify", "fortify"),
            ("Focus", "focus"),
            ("Expose", "expose"),
            ("Pierce", "pierce"),
            ("Silence", "silence"),
            ("Absorb", "absorb"),
            ("Confused", "confusion"),
            ("Marked", "mark"),
            ("Slow", "slow"),
        };

        private static readonly object Gate = new();
        private static string? _dir;
        /// <summary>Decoded icons; null = file missing or unreadable (not retried).</summary>
        private static readonly Dictionary<string, Bitmap?> Cache = new(StringComparer.OrdinalIgnoreCase);

        /// <summary>The effect a HUD status line shows ("Burn 3" -> "burn"), or null for lines without an icon.</summary>
        public static string? EffectForLine(string? line)
        {
            if (string.IsNullOrEmpty(line))
                return null;
            string s = line.TrimStart();
            foreach (var (prefix, effect) in LinePrefixes)
            {
                if (s.StartsWith(prefix, StringComparison.Ordinal))
                    return effect;
            }
            return null;
        }

        /// <summary>The icon for a HUD status line, when its effect has one in <c>GameData/StatusArt</c>.</summary>
        public static bool TryGetIconForLine(string? line, out Bitmap bitmap, out Rect sourceRect) =>
            TryGetIcon(EffectForLine(line), out bitmap, out sourceRect);

        /// <summary>The icon for an effect name ("bleed", "temporaryhp", ...).</summary>
        public static bool TryGetIcon(string? effect, out Bitmap bitmap, out Rect sourceRect)
        {
            bitmap = null!;
            sourceRect = default;
            if (string.IsNullOrEmpty(effect))
                return false;
            Bitmap? bmp;
            lock (Gate)
            {
                if (!Cache.TryGetValue(effect, out bmp))
                {
                    _dir ??= GameConstants.ResolveGameDataPath(Folder);
                    string path = Path.Combine(_dir, effect + ".png");
                    try
                    {
                        bmp = File.Exists(path) ? new Bitmap(path) : null;
                    }
                    catch
                    {
                        bmp = null;
                    }
                    Cache[effect] = bmp;
                }
            }
            if (bmp == null)
                return false;
            bitmap = bmp;
            sourceRect = new Rect(0, 0, bmp.PixelSize.Width, bmp.PixelSize.Height);
            return true;
        }
    }
}
