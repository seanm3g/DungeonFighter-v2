using System.Collections.Generic;

namespace RPGGame.Combat.UI
{
    /// <summary>
    /// Holds painted HP at a pre-swing snapshot until the sequence HUD DAMAGE/HEAL cue,
    /// so the bar does not drop during ROLL/OUTCOME while HP is already applied in Execute.
    /// </summary>
    public static class HealthBarDisplayHold
    {
        private static readonly Dictionary<string, int> Held = new();

        public static void Set(string entityId, int health)
        {
            if (string.IsNullOrEmpty(entityId))
                return;
            if (CombatUiMuteScope.IsMuted)
                return;
            Held[entityId] = health;
        }

        public static void SetFrom(IReadOnlyList<(string EntityId, int Health)>? holds)
        {
            if (holds == null)
                return;
            for (int i = 0; i < holds.Count; i++)
            {
                var (id, health) = holds[i];
                Set(id, health);
            }
        }

        public static void Release(string entityId)
        {
            if (string.IsNullOrEmpty(entityId))
                return;
            Held.Remove(entityId);
        }

        public static void ReleaseAll()
        {
            Held.Clear();
        }

        public static bool TryGet(string entityId, out int health)
        {
            if (string.IsNullOrEmpty(entityId))
            {
                health = 0;
                return false;
            }
            return Held.TryGetValue(entityId, out health);
        }

        /// <summary>Painted HP: held snapshot when present, otherwise <paramref name="actualHealth"/>.</summary>
        public static int Resolve(string entityId, int actualHealth) =>
            TryGet(entityId, out int held) ? held : actualHealth;

        internal static void ResetForTests() => Held.Clear();
    }
}
