using System;
using System.Collections.Generic;
using System.Linq;

namespace RPGGame.UI.Avalonia.Renderers
{
    /// <summary>Color used for one travel-map glyph or cell ground; mapped to RGB by <see cref="TravelMapPainter"/>.</summary>
    public enum TravelMapInk
    {
        Blank,
        SeaGround,
        Wave,
        WaveBright,
        ShallowGround,
        Surf,
        CliffGround,
        CliffFace,
        CliffShade,
        BasaltGround,
        BasaltFace,
        ForestGround,
        ForestRim,
        TreeDark,
        TreeLight,
        Grass,
        MarshGround,
        MarshRim,
        Reed,
        ReedDark,
        LakeGround,
        LakeWave,
        AshGround,
        AshRim,
        Ash,
        Cinder,
        LavaGround,
        Lava,
        Ember,
        SandGround,
        SandRim,
        Sand,
        Dune,
        Mesa,
        Snow,
        Rock,
        FirePeak
    }

    public struct TravelMapCell
    {
        public char Glyph;
        public TravelMapInk Ink;
        public TravelMapInk Ground;
    }

    /// <summary>Character grid for the travel map, at twice the game grid's resolution on each axis.</summary>
    public sealed class TravelMapGrid
    {
        private readonly TravelMapCell[,] cells;

        public TravelMapGrid(int width, int height)
        {
            Width = Math.Max(1, width);
            Height = Math.Max(1, height);
            cells = new TravelMapCell[Width, Height];
            for (int y = 0; y < Height; y++)
                for (int x = 0; x < Width; x++)
                    cells[x, y] = new TravelMapCell { Glyph = ' ', Ink = TravelMapInk.Blank, Ground = TravelMapInk.SeaGround };
        }

        public int Width { get; }
        public int Height { get; }

        public TravelMapCell this[int x, int y] => cells[x, y];

        public bool InBounds(int x, int y) => x >= 0 && y >= 0 && x < Width && y < Height;

        public void Put(int x, int y, char glyph, TravelMapInk ink, TravelMapInk ground)
        {
            if (InBounds(x, y))
                cells[x, y] = new TravelMapCell { Glyph = glyph, Ink = glyph == ' ' ? TravelMapInk.Blank : ink, Ground = ground };
        }

        /// <summary>Clears the glyph but keeps the ground, so a landmark or pin sits on open land.</summary>
        public void ClearGlyph(int x, int y)
        {
            if (InBounds(x, y))
                cells[x, y] = new TravelMapCell { Glyph = ' ', Ink = TravelMapInk.Blank, Ground = cells[x, y].Ground };
        }
    }

    /// <summary>A large landmark character (volcano, great tree, pyramid) laid over the small terrain glyphs.</summary>
    public readonly record struct TravelMapBigGlyph(double FineX, double FineY, string Text, double Scale, TravelMapInk Ink);

    /// <summary>A region's map marker, in fine-grid cells. Number is the region's travel key (shown on the slip only).</summary>
    public readonly record struct TravelMapPin(TravelRegion Region, int Number, int FineX, int FineY);

    /// <summary>A dungeon marked on a zoomed region view, in fine-grid cells. Number is its option on the selection list.</summary>
    public readonly record struct TravelMapSite(int Number, int FineX, int FineY);

    public sealed class TravelMapGeographyResult
    {
        public TravelMapGeographyResult(TravelMapGrid terrain, bool[,] land, IReadOnlyList<TravelMapPin> pins, IReadOnlyList<TravelMapBigGlyph> bigGlyphs, IReadOnlyList<TravelMapSite> sites)
        {
            Terrain = terrain;
            Land = land;
            Pins = pins;
            BigGlyphs = bigGlyphs;
            Sites = sites;
        }

        /// <summary>Dungeon markers (region view only).</summary>
        public IReadOnlyList<TravelMapSite> Sites { get; }

        public IReadOnlyList<TravelMapBigGlyph> BigGlyphs { get; }
        public TravelMapGrid Terrain { get; }

        /// <summary>True for dry-land fine cells.</summary>
        public bool[,] Land { get; }
        public IReadOnlyList<TravelMapPin> Pins { get; }
    }

    /// <summary>
    /// Generates the travel world: one chunky landmass per biome (separated by sea channels, cliffs on their
    /// south faces) packed with small biome glyphs - forest canopy, marsh reeds and lakes, ash fields with lava
    /// seams, desert dunes - plus snow-capped ridges, rivers and a few large landmarks.
    /// Land shapes are sampled in fixed world coordinates, so the whole-world travel map and a zoomed region view
    /// show the same coastlines; the small glyph texture is sampled per cell, so zooming in reveals finer detail.
    /// Deterministic, so the map is the same every visit.
    /// </summary>
    public static class TravelMapGeography
    {
        /// <summary>Text cells are about twice as tall as wide; distances correct for it.</summary>
        private const double CellAspect = 2.0;
        private const double LandLevel = 0.40;
        private const double ChannelGap = 0.10;

        /// <summary>World size in reference fine cells; the travel map shows all of it at about one cell per unit.</summary>
        private const double WorldWidth = 240;
        private const double WorldHeight = 80;

        /// <summary>World rows shown by a region view (a quarter of the world's height), close enough that neighbouring regions sit near the edges.</summary>
        private const double RegionViewWorldHeight = 20;

        private static readonly string[] Themes = { "forest", "water", "fire", "air" };
        private static readonly (int Dx, int Dy)[] Neighbors4 = { (1, 0), (-1, 0), (0, 1), (0, -1) };

        /// <summary>Maps grid cells to world coordinates: Sx/Sy grid cells per world unit, origin at grid (0,0).</summary>
        private readonly record struct WorldView(double Sx, double Sy, double OriginX, double OriginY)
        {
            public double WorldX(int x) => OriginX + x / Sx;
            public double WorldY(int y) => OriginY + y / Sy;
            public int GridX(double wx) => (int)Math.Round((wx - OriginX) * Sx);
            public int GridY(double wy) => (int)Math.Round((wy - OriginY) * Sy);
        }

        /// <summary>The whole world, for the travel screen.</summary>
        /// <param name="regions">All regions in level order; pin numbers follow this order.</param>
        /// <param name="landWidth">Fine columns available for land (left of the log slip).</param>
        public static TravelMapGeographyResult Build(int width, int height, int landWidth, IReadOnlyList<TravelRegion> regions) =>
            Build(width, height, regions, new WorldView(landWidth / WorldWidth, height / WorldHeight, 0, 0), focusIndex: -1, siteCount: 0, siteSeed: 0);

        /// <summary>
        /// A zoomed view centred on one region's island, with <paramref name="siteCount"/> dungeon sites placed on
        /// that region's land around its town (numbered 1..siteCount in order). <paramref name="siteSeed"/> picks
        /// where they fall, so each new set of dungeons can sit somewhere else on the island.
        /// </summary>
        public static TravelMapGeographyResult BuildRegionView(int width, int height, IReadOnlyList<TravelRegion> regions, string focusRegionId, int siteCount, int siteSeed = 0)
        {
            int focusIndex = -1;
            for (int i = 0; i < regions.Count; i++)
                if (string.Equals(regions[i].Id, focusRegionId, StringComparison.OrdinalIgnoreCase))
                    focusIndex = i;

            var (fx, fy) = focusIndex >= 0 ? PinWorld(regions[focusIndex], focusIndex) : (WorldWidth / 2, WorldHeight / 2);
            double scale = height / RegionViewWorldHeight;
            double windowW = width / scale;
            double ox = Math.Clamp(fx - windowW / 2, -2, Math.Max(-2, WorldWidth + 2 - windowW));
            double oy = Math.Clamp(fy - RegionViewWorldHeight / 2, -2, Math.Max(-2, WorldHeight + 2 - RegionViewWorldHeight));
            return Build(width, height, regions, new WorldView(scale, scale, ox, oy), focusIndex, siteCount, siteSeed);
        }

        private static TravelMapGeographyResult Build(int width, int height, IReadOnlyList<TravelRegion> regions, WorldView view, int focusIndex, int siteCount, int siteSeed)
        {
            var pins = PlacePins(regions, view, width, height);
            var (land, theme) = BuildLandmasses(width, height, view, regions);
            RemoveSpecks(land, width, height);
            var landDist = DistanceToSea(land, width, height);

            var focusPin = pins.FirstOrDefault(p => p.Number == focusIndex + 1);
            var sites = focusPin.Region != null && siteCount > 0
                ? PlaceSites(land, theme, landDist, pins, focusPin, siteCount, siteSeed, view, width, height)
                : new List<TravelMapSite>();
            var markers = pins.Select(p => (p.FineX, p.FineY)).Concat(sites.Select(s => (s.FineX, s.FineY))).ToList();

            var lake = BuildLakes(land, theme, landDist, markers, view, width, height);

            // Taller cliff faces when zoomed in, so the islands keep their chunky edge.
            int cliffHeight = Math.Clamp((int)Math.Round(view.Sy * 1.3), 2, 4);
            var cliff = new int[width, height];
            for (int y = 0; y < height; y++)
                for (int x = 0; x < width; x++)
                    cliff[x, y] = CliffRow(land, x, y, cliffHeight);

            // Ranges follow world crests, but their band keeps the same width in cells at any zoom.
            double ridgeAt(int x, int y) => 1 - (1 - Ridge(view.WorldX(x), view.WorldY(y))) * Math.Max(1, view.Sy);

            var grid = new TravelMapGrid(width, height);
            var shore = DistanceFromShore(land, cliff, width, height);
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    if (land[x, y])
                        PaintLand(grid, x, y, theme[x, y], landDist[x, y], lake[x, y], ridgeAt(x, y));
                    else if (cliff[x, y] > 0)
                        PaintCliff(grid, x, y, cliff[x, y], cliffHeight, theme[x, y - cliff[x, y]]);
                    else
                        PaintSea(grid, x, y, shore[x, y]);
                }
            }

            DrawRivers(grid, land, theme, landDist, lake, markers, view, width, height);
            var bigGlyphs = PlaceLandmarks(grid, theme, landDist, markers, width, height);
            ClearAroundMarkers(grid, markers);
            return new TravelMapGeographyResult(grid, land, pins, bigGlyphs, sites);
        }

        private static (double X, double Y) PinWorld(TravelRegion region, int index)
        {
            double mapX = region.MapX ?? 10 + (index % 5) * 20;
            double mapY = region.MapY ?? 10 + (index / 5) * 25;
            return (10 + Math.Clamp(mapX, 0, 100) / 100.0 * (WorldWidth - 22), 6 + Math.Clamp(mapY, 0, 100) / 100.0 * (WorldHeight - 14));
        }

        /// <summary>Pins in view only; numbers follow region order either way.</summary>
        private static List<TravelMapPin> PlacePins(IReadOnlyList<TravelRegion> regions, WorldView view, int width, int height)
        {
            var pins = new List<TravelMapPin>(regions.Count);
            for (int i = 0; i < regions.Count; i++)
            {
                var (wx, wy) = PinWorld(regions[i], i);
                // Pins land on game-grid cells (two fine cells each way) so their hit areas line up with the text layer.
                int x = view.GridX(wx) & ~1;
                int y = view.GridY(wy) & ~1;
                if (x >= 2 && y >= 2 && x < width - 3 && y < height - 3)
                    pins.Add(new TravelMapPin(regions[i], i + 1, x, y));
            }
            return pins;
        }

        /// <summary>
        /// Dungeon sites ring the region's town a few world units out, each nudged to the nearest open land of
        /// the region's own biome and kept clear of the other markers. The seed turns the ring and jitters each
        /// site's bearing and distance; seed 0 is the even ring.
        /// </summary>
        private static List<TravelMapSite> PlaceSites(bool[,] land, string[,] theme, int[,] landDist, IReadOnlyList<TravelMapPin> pins, TravelMapPin focus, int count, int seed, WorldView view, int width, int height)
        {
            var sites = new List<TravelMapSite>(count);
            var taken = pins.Select(p => (X: p.FineX, Y: p.FineY)).ToList();
            string focusTheme = ThemeKey(focus.Region.Theme);
            double turn = Hash(focus.FineX + seed, focus.FineY, 80) * Math.PI * 2;
            int keepX = Math.Max(6, (int)(view.Sx * 7)), keepY = Math.Max(3, (int)(view.Sy * 3));

            for (int i = 0; i < count; i++)
            {
                double jitter = seed == 0 ? 0 : (Hash(i, seed, 82) - 0.5) * 0.7;
                double angle = turn + Math.PI * 2 * (i + jitter) / count;
                double reach = seed == 0 ? 2 : 4;
                double radius = (3 + reach * Hash(i + seed, focus.Number, 81)) * view.Sy;
                double tx = focus.FineX + Math.Cos(angle) * radius * CellAspect;
                double ty = focus.FineY + Math.Sin(angle) * radius;

                (int X, int Y)? best = null;
                double bestScore = double.MaxValue;
                for (int y = 2; y < height - 3; y += 2)
                {
                    for (int x = 2; x < width - 3; x += 2)
                    {
                        if (!land[x, y] || theme[x, y] != focusTheme || landDist[x, y] < 2)
                            continue;
                        if (taken.Any(t => Math.Abs(t.X - x) < keepX && Math.Abs(t.Y - y) < keepY))
                            continue;
                        double dx = (x - tx) / CellAspect, dy = y - ty;
                        double score = dx * dx + dy * dy;
                        if (score < bestScore)
                        {
                            bestScore = score;
                            best = (x, y);
                        }
                    }
                }

                // A crowded island still gets every dungeon: fall back to the ideal spot.
                var at = best ?? (Math.Clamp((int)tx & ~1, 2, width - 4), Math.Clamp((int)ty & ~1, 2, height - 4));
                taken.Add(at);
                sites.Add(new TravelMapSite(i + 1, at.X, at.Y));
            }
            return sites;
        }

        /// <summary>
        /// Each biome's regions pull up their own landmass; where two biomes' pulls are close the ground drops
        /// into a sea channel, so every biome reads as a separate island.
        /// </summary>
        private static (bool[,] Land, string[,] Theme) BuildLandmasses(int width, int height, WorldView view, IReadOnlyList<TravelRegion> regions)
        {
            var land = new bool[width, height];
            var theme = new string[width, height];
            double sigma = WorldHeight * 0.12;
            double twoSigmaSq = 2 * sigma * sigma;
            var pull = new double[Themes.Length];
            var centres = regions.Select((r, i) => (World: PinWorld(r, i), Theme: ThemeKey(r.Theme))).ToList();

            for (int y = 0; y < height; y++)
            {
                double wy = view.WorldY(y);
                for (int x = 0; x < width; x++)
                {
                    double wx = view.WorldX(x);
                    Array.Clear(pull);
                    string nearest = "";
                    double nearestD = double.MaxValue;
                    string? pinTheme = null;
                    foreach (var (world, t) in centres)
                    {
                        double dx = (wx - world.X) / CellAspect;
                        double dy = wy - world.Y;
                        double d2 = dx * dx + dy * dy;
                        int ti = Array.IndexOf(Themes, t);
                        if (ti >= 0)
                            pull[ti] = Math.Max(pull[ti], Math.Exp(-d2 / twoSigmaSq));
                        if (d2 < nearestD)
                        {
                            nearestD = d2;
                            nearest = t;
                        }
                        // Every pin sits on a patch of its own biome.
                        if (Math.Abs(wx - world.X) <= 4.5 && Math.Abs(wy - world.Y) <= 2.5)
                            pinTheme = t;
                    }

                    if (pinTheme != null)
                    {
                        land[x, y] = true;
                        theme[x, y] = pinTheme;
                        continue;
                    }

                    int best = 0, second = -1;
                    for (int t = 1; t < pull.Length; t++)
                    {
                        if (pull[t] > pull[best]) { second = best; best = t; }
                        else if (second < 0 || pull[t] > pull[second]) second = t;
                    }
                    double gap = pull[best] - (second >= 0 ? pull[second] : 0);

                    double edge = Math.Min(Math.Min(wx, WorldWidth - 1 - wx) / CellAspect, Math.Min(wy, WorldHeight - 1 - wy));
                    double falloff = Math.Max(0, 1 - edge / 5);
                    double noise = 0.55 * (Fbm(wx, wy, 17) - 0.5);
                    double value = pull[best] + noise - 0.8 * falloff;

                    theme[x, y] = pull[best] > 0.02 ? Themes[best] : nearest;
                    land[x, y] = value > LandLevel && gap + 0.4 * noise > ChannelGap;

                    // Scattered islets out in open water, like the rocks off a Mario island.
                    if (!land[x, y] && pull[best] < 0.12 && falloff == 0 && ValueNoise(wx, wy, 77, 7, 3) > 0.8
                        && Hash((int)Math.Floor(wx / 3), (int)Math.Floor(wy / 2), 78) < 0.5)
                    {
                        land[x, y] = true;
                        theme[x, y] = nearest;
                    }
                }
            }
            return (land, theme);
        }

        /// <summary>Drops single cells of land or sea that would read as noise rather than shape.</summary>
        private static void RemoveSpecks(bool[,] land, int width, int height)
        {
            for (int pass = 0; pass < 2; pass++)
            {
                for (int y = 1; y < height - 1; y++)
                {
                    for (int x = 1; x < width - 1; x++)
                    {
                        int same = 0;
                        foreach (var (dx, dy) in Neighbors4)
                            if (land[x + dx, y + dy] == land[x, y])
                                same++;
                        if (same <= 1)
                            land[x, y] = !land[x, y];
                    }
                }
            }
        }

        /// <summary>Marsh islands hold bright lakes away from the shore and the pins.</summary>
        private static bool[,] BuildLakes(bool[,] land, string[,] theme, int[,] landDist, IReadOnlyList<(int X, int Y)> markers, WorldView view, int width, int height)
        {
            var lake = new bool[width, height];
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    if (!land[x, y] || landDist[x, y] < 3 || NearMarker(markers, x, y, 6, 3))
                        continue;
                    double wx = view.WorldX(x), wy = view.WorldY(y);
                    double n = ValueNoise(wx, wy, 41, 12, 5) * 0.75 + ValueNoise(wx, wy, 42, 4, 2) * 0.25;
                    lake[x, y] = theme[x, y] switch
                    {
                        "water" => n > 0.6,
                        "forest" => n > 0.8,
                        _ => false
                    };
                }
            }
            return lake;
        }

        /// <summary>For a sea cell under a south-facing coast: how many rows down the cliff face it is (0 = no cliff).</summary>
        private static int CliffRow(bool[,] land, int x, int y, int cliffHeight)
        {
            if (land[x, y])
                return 0;
            for (int k = 1; k <= cliffHeight; k++)
            {
                if (y - k < 0)
                    return 0;
                if (land[x, y - k])
                    return k;
            }
            return 0;
        }

        private static int[,] DistanceToSea(bool[,] land, int width, int height) =>
            Bfs(width, height, (x, y) => !land[x, y]);

        private static int[,] DistanceFromShore(bool[,] land, int[,] cliff, int width, int height) =>
            Bfs(width, height, (x, y) => land[x, y] || cliff[x, y] > 0);

        /// <summary>4-way distance to the nearest seed cell.</summary>
        private static int[,] Bfs(int width, int height, Func<int, int, bool> isSeed)
        {
            var distance = new int[width, height];
            var queue = new Queue<(int X, int Y)>();
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    distance[x, y] = isSeed(x, y) ? 0 : int.MaxValue;
                    if (distance[x, y] == 0)
                        queue.Enqueue((x, y));
                }
            }

            while (queue.Count > 0)
            {
                var (x, y) = queue.Dequeue();
                foreach (var (dx, dy) in Neighbors4)
                {
                    int nx = x + dx, ny = y + dy;
                    if (nx < 0 || ny < 0 || nx >= width || ny >= height || distance[nx, ny] <= distance[x, y] + 1)
                        continue;
                    distance[nx, ny] = distance[x, y] + 1;
                    queue.Enqueue((nx, ny));
                }
            }
            return distance;
        }

        private static void PaintSea(TravelMapGrid grid, int x, int y, int shore)
        {
            if (shore <= 1)
            {
                grid.Put(x, y, Hash(x, y, 3) < 0.6 ? '~' : '-', TravelMapInk.Surf, TravelMapInk.ShallowGround);
                return;
            }

            // Open water: staggered rows of small waves, a few catching the light.
            bool wave = ((x + (y % 2) * 2) % 4) != 3;
            if (!wave)
            {
                grid.Put(x, y, ' ', TravelMapInk.Blank, shore <= 3 ? TravelMapInk.ShallowGround : TravelMapInk.SeaGround);
                return;
            }
            var ink = Hash(x, y, 4) < 0.05 ? TravelMapInk.WaveBright : TravelMapInk.Wave;
            grid.Put(x, y, '~', ink, shore <= 3 ? TravelMapInk.ShallowGround : TravelMapInk.SeaGround);
        }

        /// <summary>Mario-style cliff face under a south coast: lit rock above, shadow at the waterline.</summary>
        private static void PaintCliff(TravelMapGrid grid, int x, int y, int row, int cliffHeight, string theme)
        {
            bool fire = theme == "fire";
            var ground = fire ? TravelMapInk.BasaltGround : TravelMapInk.CliffGround;
            if (row == cliffHeight)
            {
                grid.Put(x, y, Hash(x, y, 9) < 0.5 ? '_' : '.', fire ? TravelMapInk.BasaltFace : TravelMapInk.CliffShade, ground);
                return;
            }
            char glyph = Hash(x, y, 10) < 0.7 ? '|' : '!';
            grid.Put(x, y, glyph, fire ? TravelMapInk.BasaltFace : TravelMapInk.CliffFace, ground);
        }

        private static void PaintLand(TravelMapGrid grid, int x, int y, string theme, int landDist, bool lake, double ridge)
        {
            if (lake)
            {
                grid.Put(x, y, Hash(x, y, 12) < 0.55 ? '~' : ' ', TravelMapInk.LakeWave, TravelMapInk.LakeGround);
                return;
            }

            bool rim = landDist <= 1;
            double r = Hash(x, y, 7);
            double clump = ValueNoise(x, y, 33, 6, 3);
            bool peak = landDist >= 3 && ridge > 0.87 && theme != "water";
            bool hill = landDist >= 2 && ridge > 0.8;

            switch (theme)
            {
                case "forest":
                {
                    var ground = rim ? TravelMapInk.ForestRim : TravelMapInk.ForestGround;
                    if (peak)
                        grid.Put(x, y, r < 0.6 ? '^' : 'A', r < 0.75 ? TravelMapInk.Snow : TravelMapInk.Rock, ground);
                    else if (hill && r < 0.4)
                        grid.Put(x, y, 'n', TravelMapInk.Rock, ground);
                    else if (!rim && clump > 0.4)
                        grid.Put(x, y, r < 0.5 ? '♣' : '♠', r < 0.6 ? TravelMapInk.TreeLight : TravelMapInk.TreeDark, ground);
                    else
                        grid.Put(x, y, r < 0.45 ? '"' : r < 0.75 ? ',' : '♣', r < 0.75 ? TravelMapInk.Grass : TravelMapInk.TreeDark, ground);
                    break;
                }
                case "water":
                {
                    var ground = rim ? TravelMapInk.MarshRim : TravelMapInk.MarshGround;
                    if (hill && r < 0.35)
                        grid.Put(x, y, 'n', TravelMapInk.Rock, ground);
                    else if (clump > 0.55)
                        grid.Put(x, y, r < 0.6 ? '"' : '\'', r < 0.5 ? TravelMapInk.Reed : TravelMapInk.ReedDark, ground);
                    else
                        grid.Put(x, y, r < 0.35 ? ',' : r < 0.55 ? '.' : r < 0.7 ? '♣' : ' ', r < 0.55 ? TravelMapInk.ReedDark : TravelMapInk.TreeDark, ground);
                    break;
                }
                case "fire":
                {
                    var ground = rim ? TravelMapInk.AshRim : TravelMapInk.AshGround;
                    // Lava seams follow the zero line of a noise field: thin glowing cracks.
                    double seam = Math.Abs(ValueNoise(x, y, 51, 14, 6) - 0.5);
                    if (!rim && seam < 0.035)
                        grid.Put(x, y, '~', TravelMapInk.Lava, TravelMapInk.LavaGround);
                    else if (peak || (hill && r < 0.5))
                        grid.Put(x, y, r < 0.6 ? '^' : 'A', TravelMapInk.FirePeak, ground);
                    else if (r < 0.04)
                        grid.Put(x, y, '*', TravelMapInk.Ember, ground);
                    else
                        grid.Put(x, y, clump > 0.55 ? (r < 0.5 ? '^' : '.') : (r < 0.5 ? '.' : ','), clump > 0.55 ? TravelMapInk.Cinder : TravelMapInk.Ash, ground);
                    break;
                }
                default:
                {
                    var ground = rim ? TravelMapInk.SandRim : TravelMapInk.SandGround;
                    // Dune crests run in long diagonal bands.
                    double dune = Math.Sin((x * 0.35 + y * 0.9) + ValueNoise(x, y, 55, 10, 4) * 6);
                    if (peak)
                        grid.Put(x, y, r < 0.6 ? '^' : 'A', r < 0.8 ? TravelMapInk.Snow : TravelMapInk.Rock, ground);
                    else if (hill && r < 0.45)
                        grid.Put(x, y, 'n', TravelMapInk.Mesa, ground);
                    else if (dune > 0.55)
                        grid.Put(x, y, '~', TravelMapInk.Dune, ground);
                    else
                        grid.Put(x, y, r < 0.4 ? '.' : r < 0.6 ? ':' : r < 0.63 ? 'n' : ' ', r < 0.6 ? TravelMapInk.Sand : TravelMapInk.Mesa, ground);
                    break;
                }
            }
        }

        /// <summary>Rivers start deep inland and step toward the nearest coast, wandering a little; drawn as bright water cells.</summary>
        private static void DrawRivers(TravelMapGrid grid, bool[,] land, string[,] theme, int[,] landDist, bool[,] lake, IReadOnlyList<(int X, int Y)> markers, WorldView view, int width, int height)
        {
            var candidates = new List<(int X, int Y, int D)>();
            for (int y = 2; y < height - 2; y++)
                for (int x = 2; x < width - 2; x++)
                    if (land[x, y] && !lake[x, y] && theme[x, y] is "forest" or "water" && landDist[x, y] >= 5)
                        candidates.Add((x, y, landDist[x, y]));

            var sources = new List<(int X, int Y)>();
            foreach (var c in candidates.OrderByDescending(c => c.D + Hash(c.X, c.Y, 60) * 3))
            {
                if (sources.Count >= 7)
                    break;
                if (sources.All(s => Math.Abs(s.X - c.X) / CellAspect + Math.Abs(s.Y - c.Y) > 16 * view.Sy))
                    sources.Add((c.X, c.Y));
            }

            foreach (var source in sources)
            {
                var visited = new HashSet<(int, int)>();
                int x = source.X, y = source.Y;
                for (int step = 0; step < width + height; step++)
                {
                    visited.Add((x, y));
                    if (!NearMarker(markers, x, y, 4, 1))
                    {
                        // Two columns wide, since a cell is half as wide as it is tall.
                        foreach (int cx in new[] { x, x + 1 })
                            if (grid.InBounds(cx, y) && land[cx, y])
                                grid.Put(cx, y, Hash(cx, y, 62) < 0.6 ? '~' : ' ', TravelMapInk.LakeWave, TravelMapInk.LakeGround);
                    }

                    if (!land[x, y])
                        break;

                    int bestX = x, bestY = y;
                    double best = double.MaxValue;
                    foreach (var (dx, dy) in new[] { (2, 0), (-2, 0), (0, 1), (0, -1), (2, 1), (-2, 1), (2, -1), (-2, -1) })
                    {
                        int nx = x + dx, ny = y + dy;
                        if (nx < 0 || ny < 0 || nx >= width || ny >= height || visited.Contains((nx, ny)))
                            continue;
                        double e = landDist[nx, ny] + 1.6 * ValueNoise(nx, ny, 63, 8, 4);
                        if (e < best)
                        {
                            best = e;
                            bestX = nx;
                            bestY = ny;
                        }
                    }
                    if (bestX == x && bestY == y)
                        break;
                    x = bestX;
                    y = bestY;
                }
            }
        }

        /// <summary>
        /// One or two big landmarks per biome at the heart of its land, on cleared ground so they read from afar:
        /// a smoking volcano, a great tree, a pyramid, a still pool.
        /// </summary>
        private static List<TravelMapBigGlyph> PlaceLandmarks(TravelMapGrid grid, string[,] theme, int[,] landDist, IReadOnlyList<(int X, int Y)> markers, int width, int height)
        {
            var glyphs = new List<TravelMapBigGlyph>();
            foreach (string t in Themes)
            {
                var placed = new List<(int X, int Y)>();
                var spots = new List<(int X, int Y, double Score)>();
                for (int y = 3; y < height - 3; y++)
                    for (int x = 4; x < width - 4; x++)
                        if (theme[x, y] == t && landDist[x, y] >= 4 && landDist[x, y] < int.MaxValue && !NearMarker(markers, x, y, 10, 4))
                            spots.Add((x, y, landDist[x, y] + Hash(x, y, 70) * 4));

                foreach (var s in spots.OrderByDescending(s => s.Score))
                {
                    if (placed.Count >= 2)
                        break;
                    if (placed.Any(p => Math.Abs(p.X - s.X) / CellAspect + Math.Abs(p.Y - s.Y) < 18))
                        continue;
                    placed.Add((s.X, s.Y));

                    for (int y = s.Y - 2; y <= s.Y + 1; y++)
                        for (int x = s.X - 3; x <= s.X + 3; x++)
                            grid.ClearGlyph(x, y);

                    switch (t)
                    {
                        case "fire":
                            glyphs.Add(new TravelMapBigGlyph(s.X, s.Y, "^", 3.2, TravelMapInk.FirePeak));
                            glyphs.Add(new TravelMapBigGlyph(s.X + 0.4, s.Y - 2.2, "*", 1.6, TravelMapInk.Ember));
                            break;
                        case "forest":
                            glyphs.Add(new TravelMapBigGlyph(s.X, s.Y - 0.4, "♣", 3.0, TravelMapInk.TreeLight));
                            break;
                        case "air":
                            glyphs.Add(new TravelMapBigGlyph(s.X, s.Y - 0.4, "A", 2.8, TravelMapInk.Dune));
                            break;
                        default:
                            glyphs.Add(new TravelMapBigGlyph(s.X, s.Y - 0.4, "≈", 2.6, TravelMapInk.LakeWave));
                            break;
                    }
                }
            }
            return glyphs;
        }

        /// <summary>Open ground around each town and dungeon marker so it pops like a level spot.</summary>
        private static void ClearAroundMarkers(TravelMapGrid grid, IReadOnlyList<(int X, int Y)> markers)
        {
            foreach (var (mx, my) in markers)
                for (int y = my - 1; y <= my + 2; y++)
                    for (int x = mx - 2; x <= mx + 3; x++)
                        grid.ClearGlyph(x, y);
        }

        private static bool NearMarker(IReadOnlyList<(int X, int Y)> markers, int x, int y, int rx, int ry) =>
            markers.Any(m => Math.Abs(m.X + 1 - x) <= rx && Math.Abs(m.Y + 1 - y) <= ry);

        private static string ThemeKey(string? theme) => (theme ?? "").Trim().ToLowerInvariant() switch
        {
            "lava" => "fire",
            var t => t
        };

        private static double Ridge(double x, double y) => 1 - Math.Abs(2 * ValueNoise(x, y, 91, 20, 10) - 1);

        private static double Fbm(double x, double y, int salt) =>
            ValueNoise(x, y, salt, 32, 16) * 0.5
            + ValueNoise(x, y, salt + 1, 16, 8) * 0.27
            + ValueNoise(x, y, salt + 2, 8, 4) * 0.15
            + ValueNoise(x, y, salt + 3, 4, 2) * 0.08;

        /// <summary>Deterministic 0..1 hash.</summary>
        private static double Hash(int x, int y, int salt)
        {
            unchecked
            {
                uint h = (uint)(x * 374761393 + y * 668265263 + salt * 1442695041);
                h = (h ^ (h >> 13)) * 1274126177u;
                h ^= h >> 16;
                return (h & 0xFFFFFF) / (double)0x1000000;
            }
        }

        private static double ValueNoise(int x, int y, int salt, int cellW, int cellH) => ValueNoise((double)x, y, salt, cellW, cellH);

        /// <summary>Smooth 0..1 noise: smoothstep blend of <see cref="Hash"/> on a coarse lattice.</summary>
        private static double ValueNoise(double x, double y, int salt, int cellW, int cellH)
        {
            double fx = x / cellW, fy = y / cellH;
            int gx = (int)Math.Floor(fx), gy = (int)Math.Floor(fy);
            double tx = fx - gx, ty = fy - gy;
            tx = tx * tx * (3 - 2 * tx);
            ty = ty * ty * (3 - 2 * ty);
            double a = Hash(gx, gy, salt), b = Hash(gx + 1, gy, salt);
            double c = Hash(gx, gy + 1, salt), d = Hash(gx + 1, gy + 1, salt);
            double top = a + (b - a) * tx;
            double bottom = c + (d - c) * tx;
            return top + (bottom - top) * ty;
        }
    }
}
