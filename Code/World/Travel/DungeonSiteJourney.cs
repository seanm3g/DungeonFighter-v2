using System;
using System.Collections.Generic;

namespace RPGGame
{
    /// <summary>
    /// The short walk from the region's hub to a picked dungeon on the region map: d6s (more for farther sites)
    /// set the journey's hours and how long the walk plays. Cosmetic only (no events, damage or loot), and only
    /// played with the canvas UI; headless runs, MCP and tests go straight in.
    /// </summary>
    public sealed class DungeonSiteJourney
    {
        public const int DieSides = 6;
        public const int MaxDice = 3;

        /// <summary>Pause on the roll before the hero sets off.</summary>
        public const int RollHoldMs = 400;
        public const int MinWalkMs = 500;
        public const int MaxWalkMs = 1500;

        public int SiteNumber { get; init; }
        public string DungeonName { get; init; } = "";
        public int DungeonLevel { get; init; }
        public int[] Dice { get; init; } = Array.Empty<int>();
        public int Total { get; init; }

        /// <summary>How long the walk animates (after <see cref="RollHoldMs"/>), within <see cref="MinWalkMs"/>..<see cref="MaxWalkMs"/>.</summary>
        public int WalkMs { get; init; }

        /// <summary>0 = at the hub, 1 = at the dungeon.</summary>
        public double Progress { get; set; }

        /// <summary>In-world hours on the road (the dice total).</summary>
        public int Hours => Total;

        /// <summary>"Den (lvl 3)  2d6: 3+5 = 8 hr" — shown over the site while walking.</summary>
        public string Label =>
            $"{DungeonName} (lvl {DungeonLevel})  {Dice.Length}d{DieSides}: {string.Join("+", Dice)} = {Hours} hr";

        /// <summary>One die for a near site, up to <see cref="MaxDice"/> for the far edge of the map.</summary>
        public static int DiceForDistance(double distance01) =>
            Math.Clamp(1 + (int)Math.Round(Math.Clamp(distance01, 0, 1) * (MaxDice - 1)), 1, MaxDice);

        /// <summary>
        /// Rolls the journey. <paramref name="distance01"/> is the site's distance from the hub as a share of the map
        /// (0 near, 1 across it): it sets the dice count and half the walk time; the roll within its range sets the rest.
        /// </summary>
        public static DungeonSiteJourney Roll(int siteNumber, string dungeonName, int dungeonLevel, double distance01, Random rng)
        {
            double distance = Math.Clamp(distance01, 0, 1);
            int count = DiceForDistance(distance);
            var dice = new int[count];
            int total = 0;
            for (int i = 0; i < count; i++)
            {
                dice[i] = rng.Next(1, DieSides + 1);
                total += dice[i];
            }

            // Where the total sits in its own range (all 1s = 0, all 6s = 1).
            double rollShare = count * (DieSides - 1) == 0 ? 0 : (total - count) / (double)(count * (DieSides - 1));
            int span = MaxWalkMs - MinWalkMs;
            int walkMs = MinWalkMs + (int)Math.Round(span * (0.5 * distance + 0.5 * rollShare));

            return new DungeonSiteJourney
            {
                SiteNumber = siteNumber,
                DungeonName = dungeonName,
                DungeonLevel = dungeonLevel,
                Dice = dice,
                Total = total,
                WalkMs = Math.Clamp(walkMs, MinWalkMs, MaxWalkMs)
            };
        }
    }

    /// <summary>
    /// The journey in progress on the dungeon selection map (read by the renderer each frame), plus each site's
    /// distance from the hub from the last map draw.
    /// </summary>
    public static class DungeonSiteJourneyState
    {
        private static readonly object Lock = new();
        private static DungeonSiteJourney? _current;
        private static readonly Dictionary<int, double> SiteDistances = new();

        public static DungeonSiteJourney? Current
        {
            get { lock (Lock) return _current; }
            set { lock (Lock) _current = value; }
        }

        /// <summary>Called by the map renderer: site number → distance from the hub as a share of the map (0..1).</summary>
        public static void SetSiteDistances(IEnumerable<KeyValuePair<int, double>> distances)
        {
            lock (Lock)
            {
                SiteDistances.Clear();
                foreach (var pair in distances)
                    SiteDistances[pair.Key] = pair.Value;
            }
        }

        public static double? GetSiteDistance(int siteNumber)
        {
            lock (Lock)
                return SiteDistances.TryGetValue(siteNumber, out double d) ? d : null;
        }
    }
}
