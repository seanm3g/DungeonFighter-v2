namespace RPGGame.Utils
{
    /// <summary>"A" or "An" before a name ("An Echo Stalker", "An Undead Cursed Bear", "A Unicorn" is close enough).</summary>
    public static class EnglishArticle
    {
        public static string For(string? word, bool capitalized = true)
        {
            string w = (word ?? "").TrimStart();
            bool vowel = w.Length > 0 && "AEIOUaeiou".IndexOf(w[0]) >= 0;
            string a = vowel ? "an" : "a";
            return capitalized ? char.ToUpperInvariant(a[0]) + a.Substring(1) : a;
        }
    }
}
