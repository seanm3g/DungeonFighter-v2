using System;

namespace RPGGame.UI.Avalonia.Renderers.Menu
{
    /// <summary>
    /// Tiny three-line ASCII face per fighter (hair/hat, eyes, mouth), picked from the name so the same fighter
    /// always gets the same face. Every line has odd width so centered lines stack into one face.
    /// </summary>
    public static class FighterFace
    {
        private static readonly string[] Tops =
        {
            "/^^^\\", ",~~~,", "_===_", "(@@@)", "/vvv\\", "{~~~}", "[###]", "\\\\|//", ".:::.", "_/^\\_", "~~~~~", "/\"\"\"\\"
        };

        private static readonly string[] Eyes =
        {
            "o.o", "^.^", "-.-", "o.O", "0.0", "*.*", "e.e", "u.u", ">.<", "=.=", "O.o", "'.'", "@.@", "-.o"
        };

        private static readonly string[] Mouths =
        {
            "\\_-_/", " ~~~ ", "\\___/", " vVv ", "\\===/", " -o- ", "\\_v_/", "{www}", " \\_/ ", " ._. ", " ~-~ ", "\\_~_/"
        };

        /// <summary>Face lines for <paramref name="name"/>: top (5 wide), eyes (7 wide), mouth (5 wide).</summary>
        public static string[] For(string? name, bool dead = false)
        {
            uint h = Hash(name ?? "");
            string top = Tops[h % (uint)Tops.Length];
            string eyes = dead ? "x.x" : Eyes[(h / 13) % (uint)Eyes.Length];
            string mouth = dead ? " ___ " : Mouths[(h / 191) % (uint)Mouths.Length];
            return new[] { top, $"( {eyes} )", mouth };
        }

        /// <summary>Blank face with a "+" for the new-fighter card.</summary>
        public static string[] NewFighter => new[] { ".---.", "(  +  )", "'---'" };

        /// <summary>FNV-1a: stable across runs (string.GetHashCode is randomized per process).</summary>
        private static uint Hash(string s)
        {
            uint h = 2166136261;
            foreach (char c in s)
            {
                h ^= c;
                h *= 16777619;
            }
            return h;
        }
    }
}
