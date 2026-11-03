using System;
using System.Collections.Generic;

namespace RPGGame.Combat
{
    /// <summary>
    /// Room tags and room fields that change a fighter, read as standing status effects: they show in STATUS EFFECTS
    /// while the fight's room has them and end when the fight leaves the room. The numbers come from the same code the
    /// rolls use (<see cref="EnvironmentRollModifier"/>), so the HUD and the combat log agree.
    /// </summary>
    public static class RoomStatusEffects
    {
        public const string ElegantName = "Elegant";
        public const string DilapidatedName = "Dilapidated";
        public const string UnstableName = "Unstable";
        public const string DormantName = "Dormant room";
        public const string ActiveName = "Active room";

        private static readonly string[] AllNames = { ElegantName, DilapidatedName, UnstableName, DormantName, ActiveName };

        public readonly struct RoomStatus
        {
            public RoomStatus(string name, int value, string roomName)
            {
                Name = name;
                Value = value;
                RoomName = roomName;
            }

            /// <summary>One of the <c>*Name</c> constants.</summary>
            public string Name { get; }
            /// <summary>Signed size: die shift for Elegant / Dilapidated, threshold shift for Unstable, acts per fight for Dormant / Active.</summary>
            public int Value { get; }
            public string RoomName { get; }
        }

        /// <summary>The room of the fight in progress: the combat flow's room, else the on-screen fight's (HUD thread).</summary>
        public static Environment? ActiveRoom => CombatEnvironmentContext.CurrentRoom ?? CombatTurnQueue.Room;

        /// <summary>Room statuses <paramref name="actor"/> carries in <paramref name="room"/> (hero-only die shift; Unstable on everyone).</summary>
        public static List<RoomStatus> For(Actor? actor, Environment? room)
        {
            var list = new List<RoomStatus>();
            if (actor == null || room == null || ReferenceEquals(actor, room))
                return list;
            string roomName = room.Name ?? "the room";

            if (actor is Character hero && actor is not Enemy)
            {
                int shift = EnvironmentRollModifier.GetStructureShift(room, hero);
                if (shift > 0)
                    list.Add(new RoomStatus(ElegantName, shift, roomName));
                else if (shift < 0)
                    list.Add(new RoomStatus(DilapidatedName, shift, roomName));
            }

            if (room.UnstableThresholdMod != 0)
                list.Add(new RoomStatus(UnstableName, room.UnstableThresholdMod, roomName));

            // Activity tags change how often the room's hazards strike, and they hit everyone. A room that never acts
            // (not hostile) or one on the default cadence (cycling / untagged) changes nothing.
            if (room.IsHostile)
            {
                var activity = EnvironmentCombatStateManager.GetActivityProfile(room.Tags);
                if (activity.Tag == "dormant")
                    list.Add(new RoomStatus(DormantName, activity.MaxActions, roomName));
                else if (activity.Tag == "active")
                    list.Add(new RoomStatus(ActiveName, activity.MaxActions, roomName));
            }
            return list;
        }

        /// <summary>STATUS EFFECTS row: "Elegant +5", "Dilapidated −5", "Unstable +2", "Dormant room", "Active room".</summary>
        public static string FormatLine(RoomStatus status) =>
            IsActivity(status.Name)
                ? status.Name
                : $"{status.Name} {(status.Value > 0 ? "+" : "−")}{Math.Abs(status.Value)}";

        private static bool IsActivity(string name) => name == DormantName || name == ActiveName;

        /// <summary>The room status a <see cref="FormatLine"/> row names, or null for any other row.</summary>
        public static string? NameFromLine(string? line)
        {
            string text = (line ?? "").Trim();
            foreach (string name in AllNames)
            {
                if (IsActivity(name) ? text == name : text.StartsWith(name + " ", StringComparison.Ordinal))
                    return name;
            }
            return null;
        }

        /// <summary>Hover copy for a room status row on <paramref name="actor"/> (live numbers from <see cref="ActiveRoom"/>).</summary>
        public static List<string> Explain(Actor? actor, string name)
        {
            var lines = new List<string>();
            RoomStatus? live = null;
            foreach (var s in For(actor, ActiveRoom))
            {
                if (s.Name == name)
                    live = s;
            }
            int n = Math.Abs(live?.Value ?? 0);

            switch (name)
            {
                case ElegantName:
                    lines.Add("Elegant: fine footing and clear sightlines in this room.");
                    lines.Add("How it works: each of your d20 rolls is raised by your level"
                        + CapNote() + ", up to 20. The combat log shows the raise under the roll as (Elegant: +N).");
                    if (n > 0)
                        lines.Add($"Right now: +{n} to every roll.");
                    break;
                case DilapidatedName:
                    lines.Add("Dilapidated: crumbling floors and bad footing in this room.");
                    lines.Add("How it works: each of your d20 rolls is lowered by your level"
                        + CapNote() + ", down to 1. The combat log shows the drop under the roll as (Dilapidated: −N).");
                    if (n > 0)
                        lines.Add($"Right now: −{n} to every roll.");
                    break;
                case UnstableName:
                    lines.Add("Unstable: the room shifts underfoot, so blows swing wilder for everyone in it, enemies too.");
                    lines.Add("How it works: every roll's bands move by the room's value. Positive makes hit, combo and "
                        + "critical easier and critical misses more likely; negative does the reverse.");
                    if (live is RoomStatus u)
                        lines.Add(u.Value > 0
                            ? $"Right now: hit, combo and critical need {u.Value} less; critical miss covers {u.Value} more faces."
                            : $"Right now: hit, combo and critical need {-u.Value} more; critical miss covers {-u.Value} fewer faces.");
                    break;
                case DormantName:
                    lines.Add("Dormant room: the room's hazards are mostly asleep. They can hit anyone in the room, enemies too.");
                    lines.Add("How it works: the room rolls a d20 each turn and strikes on a high face; every quiet turn widens "
                        + "the range. Here it widens at a quarter of the usual pace, and the room strikes at most once a fight "
                        + $"(usually {EnvironmentCombatStateManager.DefaultMaxActions}).");
                    break;
                case ActiveName:
                    lines.Add("Active room: the room's hazards are restless. They can hit anyone in the room, enemies too.");
                    lines.Add("How it works: the room rolls a d20 each turn and strikes on a high face; every quiet turn widens "
                        + $"the range. Here it widens twice as fast, and the room can strike up to {live?.Value ?? 3} times a fight "
                        + $"(usually {EnvironmentCombatStateManager.DefaultMaxActions}).");
                    break;
            }

            if (live is RoomStatus src)
                lines.Add($"Applied by: {src.RoomName} (room).");
            lines.Add("Ends: when you leave the room.");
            return lines;
        }

        private static string CapNote()
        {
            int cap = GameConfiguration.Instance.Combat.RoomStructureRollShiftCap;
            return cap > 0 ? $" (at most {cap})" : "";
        }
    }
}
