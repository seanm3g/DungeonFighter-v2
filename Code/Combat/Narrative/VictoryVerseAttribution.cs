using System;

namespace RPGGame.Combat.Narrative
{
    /// <summary>
    /// Formats a faux poet credit under the dungeon-completion victory verse:
    /// <c>-First Last, N aod</c> (year 0–250 inclusive).
    /// </summary>
    public static class VictoryVerseAttribution
    {
        public const int MinYearAod = 0;
        public const int MaxYearAod = 250;

        /// <summary>Builds a random artist credit from FlavorText name banks.</summary>
        public static string CreateRandom()
        {
            var data = FlavorText.GetData();
            string first = FlavorText.GetRandomName(data.Names.CharacterFirstNames);
            string last = FlavorText.GetRandomName(data.Names.CharacterLastNames);
            // Next is exclusive on the upper bound — use MaxYearAod + 1 for inclusive 0–250.
            int year = RandomUtility.Next(MinYearAod, MaxYearAod + 1);
            return Format(first, last, year);
        }

        /// <summary>Formats <c>-{first} {last}, {year} aod</c> for display.</summary>
        public static string Format(string firstName, string lastName, int yearAod)
        {
            string first = string.IsNullOrWhiteSpace(firstName) ? "Unknown" : firstName.Trim();
            string last = string.IsNullOrWhiteSpace(lastName) ? "Unknown" : lastName.Trim();
            int year = Math.Clamp(yearAod, MinYearAod, MaxYearAod);
            return $"-{first} {last}, {year} aod";
        }
    }
}
