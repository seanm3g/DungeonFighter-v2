using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using RPGGame.UI.ColorSystem;

namespace RPGGame
{
    /// <summary>
    /// Hidden d20 scouting roll made at the continue / leave prompt. The report never shows the roll or its band:
    /// <list type="bullet">
    /// <item>Critical: full report, and the hero strikes first with advantage in that room's fights.</item>
    /// <item>Combo: full report (room, enemy count, names and levels).</item>
    /// <item>Hit: enemy count only.</item>
    /// <item>Miss: nothing.</item>
    /// <item>Critical miss: a convincing but false report, and the enemies take the initiative.</item>
    /// </list>
    /// TECH milestones widen the good bands the same way they shift attack thresholds.
    /// </summary>
    public static class RoomScouting
    {
        public enum Outcome { CriticalMiss, Miss, Hit, Combo, Critical }

        public sealed class Report
        {
            public Outcome Outcome { get; init; }
            public Environment Room { get; init; } = null!;
            public List<List<ColoredText>> Lines { get; init; } = new();
            /// <summary>Hero strikes first with advantage on their first roll of each fight in the scouted room.</summary>
            public bool HeroAdvantage => Outcome == Outcome.Critical;
            /// <summary>Enemies strike first in the scouted room.</summary>
            public bool HeroLosesInitiative => Outcome == Outcome.CriticalMiss;
        }

        private static readonly ConditionalWeakTable<Character, Report> Pending = new();

        /// <summary>Maps a d20 face to a band; TECH milestone steps lower the miss / hit / combo ceilings.</summary>
        public static Outcome Resolve(int roll, int hitSteps = 0, int comboSteps = 0, int critSteps = 0)
        {
            int missCeiling = Math.Max(1, 5 - hitSteps);
            int hitCeiling = Math.Max(missCeiling, 13 - comboSteps);
            int comboCeiling = Math.Max(hitCeiling, 19 - critSteps);
            if (roll <= 1) return Outcome.CriticalMiss;
            if (roll <= missCeiling) return Outcome.Miss;
            if (roll <= hitCeiling) return Outcome.Hit;
            if (roll <= comboCeiling) return Outcome.Combo;
            return Outcome.Critical;
        }

        /// <summary>Rolls for <paramref name="nextRoom"/>, remembers the result for that room and returns the report.</summary>
        public static Report Roll(Character hero, Environment nextRoom, Dungeon? dungeon, Random? rng = null)
        {
            rng ??= RandomUtility.Ambient;
            int roll = rng.Next(1, 21);
            var (hitSteps, comboSteps, critSteps) = TechniqueMilestoneThresholdBonuses.GetSteps(hero.GetEffectiveTechnique());
            var outcome = Resolve(roll, hitSteps, comboSteps, critSteps);
            var report = new Report
            {
                Outcome = outcome,
                Room = nextRoom,
                Lines = BuildLines(outcome, nextRoom, dungeon, rng)
            };
            Pending.AddOrUpdate(hero, report);
            return report;
        }

        /// <summary>Takes the stored report when it was made for <paramref name="room"/>; otherwise null.</summary>
        public static Report? TakeFor(Character hero, Environment room)
        {
            if (!Pending.TryGetValue(hero, out var report))
                return null;
            Pending.Remove(hero);
            return ReferenceEquals(report.Room, room) ? report : null;
        }

        public static void Clear(Character hero) => Pending.Remove(hero);

        private static List<List<ColoredText>> BuildLines(Outcome outcome, Environment room, Dungeon? dungeon, Random rng)
        {
            var enemies = room.GetEnemies().Where(e => e.IsAlive).ToList();
            return outcome switch
            {
                Outcome.Critical or Outcome.Combo => FullReport(room.Name, enemies.Select(e => (e.Name, e.Level)).ToList()),
                Outcome.Hit => CountReport(room.Name, enemies.Count),
                Outcome.Miss => new List<List<ColoredText>>
                {
                    Line(("You strain to see ahead, but the way into ", ColorPalette.Gray), (room.Name, ColorPalette.Gray),
                        (" gives nothing away.", ColorPalette.Gray))
                },
                _ => FalseReport(room, enemies, dungeon, rng)
            };
        }

        /// <summary>A wrong report shaped exactly like a real one (full or count-only) so the player cannot tell.</summary>
        private static List<List<ColoredText>> FalseReport(Environment room, List<Enemy> real, Dungeon? dungeon, Random rng)
        {
            int realCount = real.Count;
            int fakeCount = realCount == 0 ? rng.Next(1, 3) : (rng.Next(2) == 0 ? 0 : Math.Max(1, realCount + (rng.Next(2) == 0 ? -1 : 1)));
            if (fakeCount == realCount)
                fakeCount = realCount + 1;
            if (rng.Next(2) == 0)
                return CountReport(room.Name, fakeCount);

            var names = (dungeon?.PossibleEnemies ?? new List<string>())
                .Where(n => !real.Any(e => string.Equals(e.Name, n, StringComparison.OrdinalIgnoreCase)))
                .ToList();
            if (names.Count == 0)
                names = new List<string> { "Goblin", "Wolf", "Spider" };
            int lvl = real.Count > 0 ? real[0].Level : Math.Max(1, dungeon?.MinLevel ?? 1);
            var fake = new List<(string, int)>();
            for (int i = 0; i < fakeCount; i++)
                fake.Add((names[rng.Next(names.Count)], Math.Max(1, lvl + rng.Next(-1, 2))));
            return FullReport(room.Name, fake);
        }

        private static List<List<ColoredText>> FullReport(string roomName, List<(string Name, int Level)> enemies)
        {
            if (enemies.Count == 0)
                return new List<List<ColoredText>>
                {
                    Line(("You scout ahead: ", ColorPalette.Info), (roomName, ColorPalette.White), (" looks empty.", ColorPalette.Success))
                };
            var b = new ColoredTextBuilder()
                .Add("You scout ahead: ", ColorPalette.Info)
                .Add(roomName, ColorPalette.White)
                .Add($" holds {enemies.Count} {(enemies.Count == 1 ? "enemy" : "enemies")} - ", ColorPalette.White);
            for (int i = 0; i < enemies.Count; i++)
            {
                if (i > 0)
                    b.Add(", ", ColorPalette.White);
                b.Add(enemies[i].Name, ColorPalette.Enemy);
                b.Add($" (Lv {enemies[i].Level})", ColorPalette.Gray);
            }
            b.Add(".", ColorPalette.White);
            return new List<List<ColoredText>> { b.Build() };
        }

        private static List<List<ColoredText>> CountReport(string roomName, int count)
        {
            if (count == 0)
                return new List<List<ColoredText>>
                {
                    Line(("You listen at the way into ", ColorPalette.Info), (roomName, ColorPalette.White), (": silence.", ColorPalette.Success))
                };
            return new List<List<ColoredText>>
            {
                Line(("You listen at the way into ", ColorPalette.Info), (roomName, ColorPalette.White),
                    ($": {count} {(count == 1 ? "creature" : "creatures")} moving, but you can't tell what.", ColorPalette.White))
            };
        }

        private static List<ColoredText> Line(params (string Text, ColorPalette Color)[] parts)
        {
            var b = new ColoredTextBuilder();
            foreach (var (text, color) in parts)
                b.Add(text, color);
            return b.Build();
        }
    }
}
