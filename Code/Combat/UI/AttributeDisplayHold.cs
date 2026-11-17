using System.Collections.Generic;

namespace RPGGame.Combat.UI
{
    /// <summary>
    /// Holds the hero's painted STR/AGI/TECH/INT (and hybrid) rows at their pre-swing values until the swing's
    /// OUTCOME is shown, so a bonus the action grants (EXTRA EFFORT +5 STR, tentative TURN stat bonuses) does not
    /// appear before the dice result. Works alongside <see cref="HealthBarDisplayHold"/>: the snapshot is staged when
    /// the swing starts, becomes active when the sequence HUD takes its health holds, and is released on OUTCOME
    /// (or whenever the health holds are released).
    /// </summary>
    public static class AttributeDisplayHold
    {
        private static Character? _stagedHero;
        private static Dictionary<string, int>? _staged;
        private static Character? _heldHero;
        private static Dictionary<string, int>? _held;

        /// <summary>Snapshot the painted values before a hero swing changes any stat.</summary>
        public static void Stage(Character hero, Dictionary<string, int> values)
        {
            if (CombatUiMuteScope.IsMuted)
                return;
            _stagedHero = hero;
            _staged = values;
        }

        /// <summary>Called when the sequence HUD starts holding this swing's display (see <see cref="HealthBarDisplayHold.SetFrom"/>).</summary>
        public static void ActivateStaged()
        {
            if (_staged == null || _stagedHero == null)
                return;
            _heldHero = _stagedHero;
            _held = _staged;
            DropStaged();
        }

        /// <summary>Forget a staged snapshot the HUD did not take (swing not played).</summary>
        public static void DropStaged()
        {
            _stagedHero = null;
            _staged = null;
        }

        public static void Release()
        {
            _heldHero = null;
            _held = null;
        }

        /// <summary>Painted value of <paramref name="key"/>: held snapshot while the swing plays, else <paramref name="live"/>.</summary>
        public static int Resolve(Character character, string key, int live)
        {
            var held = _held;
            if (held != null && ReferenceEquals(_heldHero, character) && held.TryGetValue(key, out int v))
                return v;
            return live;
        }

        internal static void ResetForTests()
        {
            DropStaged();
            Release();
        }
    }
}
