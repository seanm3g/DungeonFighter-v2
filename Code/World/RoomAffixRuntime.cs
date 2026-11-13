using System;
using System.Collections.Generic;
using System.Linq;
using RPGGame.Data;

namespace RPGGame
{
    /// <summary>
    /// Room affixes (<c>RoomAffixes.json</c>): authored terrain tags change the room's hazards or crit face, and each
    /// room rolls at most one affix that changes its hazards (power, activity, status, targets, acts) or its enemies
    /// (count, dungeon affix, prefix chance, starting status). Applied when the room is built, before its enemies spawn.
    /// </summary>
    public static class RoomAffixRuntime
    {
        public const string TargetsBoth = "both";
        public const string TargetsHero = "hero";
        public const string TargetsEnemies = "enemies";
        private static readonly string[] ActivityLadder = { "dormant", "cycling", "active" };

        /// <summary>Applies terrain tags and rolls the room's affix. <paramref name="dungeonTheme"/> picks the element for Infused.</summary>
        public static void Apply(Environment room, string? dungeonTheme, Random random)
        {
            if (room == null)
                return;
            var file = AffixDataLoader.Room;

            foreach (var terrain in file.Terrain)
            {
                if (!AffixFeatures.Terrain)
                    break;
                if (!room.Tags.Any(t => string.Equals(t, terrain.Tag, StringComparison.OrdinalIgnoreCase)))
                    continue;
                AddHazardStatus(room, terrain.HazardStatus);
                room.CritShift += terrain.CritShift;
            }

            if (file.Affixes.Count == 0 || random.NextDouble() >= file.RollChance)
                return;
            if (!AffixFeatures.Room)
            {
                PickWeighted(file.Affixes, random); // keep the stream aligned with the feature on
                return;
            }
            var affix = PickWeighted(file.Affixes, random);
            if (affix != null)
                ApplyAffix(room, affix, dungeonTheme);
        }

        /// <summary>Applies one rolled affix to <paramref name="room"/> (public for tests and the Action Lab).</summary>
        public static void ApplyAffix(Environment room, RoomAffixDef affix, string? dungeonTheme)
        {
            room.RoomAffix = affix;
            if (Math.Abs(affix.HazardDamage - 1.0) > 1e-9)
            {
                foreach (var hazard in Hazards(room))
                    hazard.DamageMultiplier *= affix.HazardDamage;
            }
            if (affix.ActivityStep != 0)
                StepActivity(room, affix.ActivityStep);
            if (!string.IsNullOrWhiteSpace(affix.HazardStatus))
            {
                string status = affix.HazardStatus;
                if (string.Equals(status, "element", StringComparison.OrdinalIgnoreCase))
                {
                    string? region = AffixDataLoader.Enemy.DungeonFor(dungeonTheme)?.Region;
                    status = region != null && AffixDataLoader.Room.ElementStatus.TryGetValue(region, out var s) ? s : "";
                }
                AddHazardStatus(room, status);
            }
            if (!string.IsNullOrWhiteSpace(affix.HazardTargets))
                room.HazardTargets = affix.HazardTargets.Trim().ToLowerInvariant();
            room.ExtraHazardActs += Math.Max(0, affix.ExtraHazardActs);
        }

        /// <summary>The hazard's targets after the room's target rule (hero only / enemies only / both).</summary>
        public static List<Actor> FilterHazardTargets(Environment room, Actor hero, Actor enemy) => room.HazardTargets switch
        {
            TargetsHero => new List<Actor> { hero },
            TargetsEnemies => new List<Actor> { enemy },
            _ => new List<Actor> { hero, enemy }
        };

        /// <summary>Enemy count after the room's extra enemies (capped at <see cref="RoomAffixFile.MaxEnemies"/>).</summary>
        public static int AdjustEnemyCount(int count, RoomAffixDef? affix)
        {
            if (affix == null || affix.ExtraEnemies <= 0)
                return count;
            int cap = Math.Max(count, AffixDataLoader.Room.MaxEnemies);
            return Math.Min(cap, count + affix.ExtraEnemies);
        }

        private static IEnumerable<Action> Hazards(Environment room) =>
            room.ActionPool.Select(e => e.action).Where(a => a != null);

        private static void AddHazardStatus(Environment room, string? status)
        {
            if (string.IsNullOrWhiteSpace(status))
                return;
            foreach (var hazard in Hazards(room))
                SetCauses(hazard, status.Trim().ToLowerInvariant());
        }

        internal static void SetCauses(Action action, string status)
        {
            switch (status)
            {
                case "burn": action.CausesBurn = true; break;
                case "slow": action.CausesSlow = true; break;
                case "poison":
                    action.CausesPoison = true;
                    // Same amount as enemy poison (Venom kit, affixes); the generic 1% made hazard poison the deadliest thing in a room.
                    if (action.PoisonPercentToAdd <= 0) action.PoisonPercentToAdd = 0.25;
                    break;
                case "weaken": action.CausesWeaken = true; break;
                case "bleed": action.CausesBleed = true; break;
                case "stun": action.CausesStun = true; break;
                case "acid": action.CausesAcid = true; break;
            }
        }

        private static void StepActivity(Environment room, int step)
        {
            var tags = room.Tags.ToList();
            var (current, _, _) = EnvironmentCombatStateManager.GetActivityProfile(tags);
            int index = Array.IndexOf(ActivityLadder, current ?? "cycling");
            if (index < 0)
                index = 1;
            string next = ActivityLadder[Math.Clamp(index + step, 0, ActivityLadder.Length - 1)];
            tags.RemoveAll(t => ActivityLadder.Contains(t, StringComparer.OrdinalIgnoreCase));
            tags.Add(next);
            room.SetTags(tags);
        }

        private static RoomAffixDef? PickWeighted(List<RoomAffixDef> affixes, Random random)
        {
            double total = affixes.Sum(a => Math.Max(0, a.Weight));
            if (total <= 0)
                return null;
            double roll = random.NextDouble() * total;
            foreach (var a in affixes)
            {
                roll -= Math.Max(0, a.Weight);
                if (roll < 0)
                    return a;
            }
            return affixes[^1];
        }
    }
}
