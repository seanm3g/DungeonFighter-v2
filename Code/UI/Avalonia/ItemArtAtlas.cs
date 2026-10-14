using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using Avalonia;
using Avalonia.Media.Imaging;

namespace RPGGame.UI.Avalonia
{
    /// <summary>
    /// Item icons packed by df-darkfantasy's <c>build_item_atlas.py</c>: <c>GameData/ItemArt/index.json</c> plus one
    /// JPEG sheet per material (and "Plain" for bare bases). Sheets are decoded on first use only.
    /// An item resolves by the longest catalog base name in its <see cref="Item.Name"/> and its <see cref="Item.Material"/>,
    /// falling back to the plain base when that material has no icon.
    /// </summary>
    public static class ItemArtAtlas
    {
        private const string Folder = "ItemArt";
        private const string Plain = "Plain";

        private sealed class Sheet
        {
            public string Name = "";
            public string File = "";
            public int Cols;
            public Dictionary<string, int> Slots = new(StringComparer.OrdinalIgnoreCase);
            public Bitmap? Bitmap;
            public bool LoadFailed;
        }

        private static readonly object Gate = new();
        private static bool _loaded;
        private static string _dir = "";
        private static int _tile;
        private static readonly Dictionary<string, Sheet> Sheets = new(StringComparer.OrdinalIgnoreCase);
        /// <summary>Every base name, longest first, padded with spaces for whole-word matching.</summary>
        private static List<(string Padded, string Name)> _bases = new();
        private static readonly Dictionary<string, (string Sheet, string Base)?> ResolveCache = new();

        /// <summary>True when the index loaded and has at least one sheet.</summary>
        public static bool IsAvailable
        {
            get { EnsureLoaded(); return Sheets.Count > 0; }
        }

        /// <summary>The icon for <paramref name="item"/>: a sheet bitmap and the tile's pixel rectangle on it.</summary>
        public static bool TryGetIcon(Item item, out Bitmap bitmap, out Rect sourceRect)
        {
            bitmap = null!;
            sourceRect = default;
            EnsureLoaded();
            if (Sheets.Count == 0 || item == null)
                return false;

            var hit = Resolve(item.Name ?? "", item.Material ?? "");
            if (hit == null)
                return false;

            var sheet = Sheets[hit.Value.Sheet];
            var bmp = LoadBitmap(sheet);
            if (bmp == null)
                return false;
            int slot = sheet.Slots[hit.Value.Base];
            bitmap = bmp;
            sourceRect = new Rect((slot % sheet.Cols) * _tile, (slot / sheet.Cols) * _tile, _tile, _tile);
            return true;
        }

        /// <summary>Which sheet and base an item name + material map to ("Plain" when the material has no icon); null = no icon.</summary>
        internal static (string Sheet, string Base)? ResolveName(string name, string material)
        {
            EnsureLoaded();
            return Sheets.Count == 0 ? null : Resolve(name, material);
        }

        private static (string Sheet, string Base)? Resolve(string name, string material)
        {
            string key = material + "|" + name;
            lock (Gate)
            {
                if (ResolveCache.TryGetValue(key, out var cached))
                    return cached;
            }

            string padded = " " + name.ToLowerInvariant() + " ";
            string? baseName = _bases.FirstOrDefault(b => padded.Contains(b.Padded, StringComparison.Ordinal)).Name;
            (string, string)? result = null;
            if (baseName != null)
            {
                string? mat = !string.IsNullOrWhiteSpace(material)
                    ? material.Trim()
                    : Sheets.Keys.FirstOrDefault(m => !m.Equals(Plain, StringComparison.OrdinalIgnoreCase)
                        && padded.Contains(" " + m.ToLowerInvariant() + " ", StringComparison.Ordinal));
                if (mat != null && Sheets.TryGetValue(mat, out var s) && s.Slots.ContainsKey(baseName))
                    result = (s.Name, baseName);
                else if (Sheets.TryGetValue(Plain, out var p) && p.Slots.ContainsKey(baseName))
                    result = (Plain, baseName);
            }

            lock (Gate)
                ResolveCache[key] = result;
            return result;
        }

        private static Bitmap? LoadBitmap(Sheet sheet)
        {
            lock (Gate)
            {
                if (sheet.Bitmap != null || sheet.LoadFailed)
                    return sheet.Bitmap;
                try
                {
                    sheet.Bitmap = new Bitmap(Path.Combine(_dir, sheet.File));
                }
                catch (Exception ex)
                {
                    sheet.LoadFailed = true;
                    DebugLogger.Log("ItemArtAtlas", $"Could not load {sheet.File}: {ex.Message}");
                }
                return sheet.Bitmap;
            }
        }

        private static void EnsureLoaded()
        {
            lock (Gate)
            {
                if (_loaded)
                    return;
                _loaded = true;
                try
                {
                    _dir = GameConstants.ResolveGameDataPath(Folder);
                    string indexPath = Path.Combine(_dir, "index.json");
                    if (!File.Exists(indexPath))
                        return;

                    using var doc = JsonDocument.Parse(File.ReadAllText(indexPath));
                    _tile = doc.RootElement.GetProperty("tile").GetInt32();
                    var names = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                    foreach (var prop in doc.RootElement.GetProperty("sheets").EnumerateObject())
                    {
                        var sheet = new Sheet
                        {
                            Name = prop.Name,
                            File = prop.Value.GetProperty("file").GetString() ?? "",
                            Cols = prop.Value.GetProperty("cols").GetInt32()
                        };
                        int i = 0;
                        foreach (var n in prop.Value.GetProperty("items").EnumerateArray())
                        {
                            string baseName = n.GetString() ?? "";
                            sheet.Slots[baseName] = i++;
                            names.Add(baseName);
                        }
                        Sheets[prop.Name] = sheet;
                    }
                    _bases = names
                        .OrderByDescending(n => n.Length)
                        .Select(n => (" " + n.ToLowerInvariant() + " ", n))
                        .ToList();
                }
                catch (Exception ex)
                {
                    Sheets.Clear();
                    DebugLogger.Log("ItemArtAtlas", $"Could not read item art index: {ex.Message}");
                }
            }
        }
    }
}
