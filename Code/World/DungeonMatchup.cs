using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace RPGGame
{
    /// <summary>One of the six enemy families (D8.1), from <c>GameData/EnemyFamilies.json</c>.</summary>
    public sealed class EnemyFamily
    {
        [JsonPropertyName("id")] public string Id { get; set; } = "";
        [JsonPropertyName("name")] public string Name { get; set; } = "";
        [JsonPropertyName("archetypes")] public List<string> Archetypes { get; set; } = new();
        [JsonPropertyName("playsLike")] public string PlaysLike { get; set; } = "";
        [JsonPropertyName("punishedBy")] public string PunishedBy { get; set; } = "";
        [JsonPropertyName("punishedByClasses")] public List<string> PunishedByClasses { get; set; } = new();
        [JsonPropertyName("punishesClasses")] public List<string> PunishesClasses { get; set; } = new();
        [JsonPropertyName("fernis")] public string Fernis { get; set; } = "";
    }

    /// <summary>
    /// The six families mirror the classes (D8.1): Brute, Knight, Assassin, Sage, Trickster, Beast. A family is how an
    /// enemy reads (plays like / punished by) and drives the dungeon-select matchup; the enemy's stats still come from
    /// its archetype, so this changes no numbers.
    /// </summary>
    public static class EnemyFamilies
    {
        private sealed class File
        {
            [JsonPropertyName("beastWhenNoHands")] public bool BeastWhenNoHands { get; set; } = true;
            [JsonPropertyName("families")] public List<EnemyFamily> Families { get; set; } = new();
        }

        private static File? _file;

        private static File Data => _file ??= Load();

        public static IReadOnlyList<EnemyFamily> All => Data.Families;

        public static void ResetForTests() => _file = null;

        private static File Load()
        {
            try
            {
                string? path = JsonLoader.FindGameDataFile("EnemyFamilies.json");
                if (path != null && System.IO.File.Exists(path))
                    return JsonSerializer.Deserialize<File>(System.IO.File.ReadAllText(path),
                        new JsonSerializerOptions { PropertyNameCaseInsensitive = true }) ?? new File();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error loading EnemyFamilies.json: {ex.Message}");
            }
            return new File();
        }

        public static EnemyFamily? ById(string? id) =>
            All.FirstOrDefault(f => string.Equals(f.Id, id, StringComparison.OrdinalIgnoreCase));

        /// <summary>The family for an archetype name and the enemy's tags (a Berserker with no hands is a Beast).</summary>
        public static EnemyFamily? For(string? archetype, IEnumerable<string>? tags)
        {
            var tagSet = new HashSet<string>(tags ?? Array.Empty<string>(), StringComparer.OrdinalIgnoreCase);
            if (Data.BeastWhenNoHands && string.Equals(archetype, "Berserker", StringComparison.OrdinalIgnoreCase)
                && tagSet.Contains("living") && !tagSet.Contains("has_hands"))
                return ById("beast");
            return All.FirstOrDefault(f => f.Archetypes.Any(a => string.Equals(a, archetype, StringComparison.OrdinalIgnoreCase)));
        }

        public static EnemyFamily? For(EnemyData? data) => data == null ? null : For(data.Archetype, data.Tags);
    }

    /// <summary>
    /// The dungeon-select matchup card (D0.8): the lesson (the dungeon's identity), its enemy families, its material,
    /// a ✓ / ✗ / ~ for the hero's class, and Fernis's line (the opposite of good advice, D9.7; the marks stay true).
    /// </summary>
    public sealed class DungeonMatchup
    {
        public string Lesson { get; init; } = "";
        public string Material { get; init; } = "";
        public List<(EnemyFamily Family, int Count)> Families { get; init; } = new();
        /// <summary>"✓" good for the hero's class, "✗" bad, "~" even.</summary>
        public string Mark { get; init; } = "~";
        public string Fernis { get; init; } = "";

        public const string Good = "✓", Bad = "✗", Even = "~";

        public static DungeonMatchup Evaluate(Dungeon dungeon, string? heroClass)
        {
            var families = (dungeon.PossibleEnemies ?? new List<string>())
                .Where(n => !string.IsNullOrWhiteSpace(n)).Distinct(StringComparer.OrdinalIgnoreCase)
                .Select(n => EnemyFamilies.For(EnemyLoader.GetEnemyData(n)))
                .Where(f => f != null).Cast<EnemyFamily>()
                .GroupBy(f => f.Id).Select(g => (g.First(), g.Count()))
                .OrderByDescending(x => x.Item2).ToList();

            string cls = (heroClass ?? "").Trim().ToLowerInvariant();
            int score = 0;
            foreach (var (f, n) in families)
            {
                if (f.PunishedByClasses.Contains(cls))
                    score += n;
                if (f.PunishesClasses.Contains(cls))
                    score -= n;
            }
            var identity = RPGGame.Data.AffixDataLoader.Enemy.DungeonFor(dungeon.Theme);
            string lesson = identity?.Affix != null && !string.IsNullOrWhiteSpace(identity.Affix.Name)
                ? identity.Affix.Name
                : dungeon.Theme ?? "";
            return new DungeonMatchup
            {
                Lesson = lesson,
                Material = identity?.Material ?? "",
                Families = families,
                Mark = score > 0 ? Good : score < 0 ? Bad : Even,
                Fernis = families.Count > 0 ? families[0].Item1.Fernis : ""
            };
        }

        public static DungeonMatchup Evaluate(Dungeon dungeon, Character? hero) =>
            Evaluate(dungeon, hero?.Progression?.GetPrimaryClassWeaponType() is WeaponType wt ? ClassTags.ClassOf(wt) : null);

        /// <summary>Short lines for a panel: "Brute ×2, Beast: ✓ for you", "Lesson: ...", "Fernis: ...".</summary>
        public List<string> Lines(bool withFernis = true)
        {
            var lines = new List<string>();
            if (Families.Count > 0)
                lines.Add($"{Mark} " + string.Join(", ", Families.Select(x => x.Count > 1 ? $"{x.Family.Name} ×{x.Count}" : x.Family.Name)));
            if (Families.Count > 0)
                lines.Add($"Weak to {Families[0].Family.PunishedBy}");
            if (!string.IsNullOrWhiteSpace(Lesson) || !string.IsNullOrWhiteSpace(Material))
                lines.Add(string.Join(" · ", new[] { Lesson, Material }.Where(s => !string.IsNullOrWhiteSpace(s))));
            if (withFernis && !string.IsNullOrWhiteSpace(Fernis) && System.Environment.GetEnvironmentVariable("DF_FERNIS") != "0")
                lines.Add($"Fernis: {Fernis}");
            return lines;
        }
    }
}
