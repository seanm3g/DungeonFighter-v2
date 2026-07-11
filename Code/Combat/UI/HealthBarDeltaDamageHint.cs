using System;
using System.Collections.Generic;
using RPGGame;
using RPGGame.Combat;

namespace RPGGame.Combat.UI
{
    /// <summary>
    /// Stable ids for <see cref="RPGGame.UI.Avalonia.HealthTracker"/> / health bar rendering (see CharacterPanelRenderer / RightPanelRenderer).
    /// </summary>
    public static class HealthBarEntityId
    {
        public static string? ForActor(Actor actor)
        {
            // Enemy extends Character — test Enemy first so the bar id matches RightPanelRenderer.
            if (actor is Enemy e)
                return $"enemy_{e.Name}";
            if (actor is Character c)
                return $"player_{c.Name}";
            return null;
        }
    }

    public enum HealthBarDotDamageKind
    {
        Poison,
        Burn,
        Acid,
        Bleed
    }

    public readonly struct HealthBarDeltaDamagePart
    {
        public HealthBarDotDamageKind Kind { get; init; }
        public int Amount { get; init; }
    }

    /// <summary>
    /// Lets combat code describe how much of the next <c>TakeDamage</c> call comes from poison / burn / acid / bleed
    /// so the UI health bar delta can color segments. Consumed once when <see cref="RPGGame.UI.Avalonia.HealthTracker"/> detects a health drop.
    /// </summary>
    public static class HealthBarDeltaDamageHint
    {
        private static readonly Dictionary<string, (int poison, int burn, int acid, int bleed)> Pending = new();

        public static void SetPending(string entityId, int poisonDamage, int burnDamage, int acidDamage, int bleedDamage)
        {
            if (CombatUiMuteScope.IsMuted)
                return;
            Pending[entityId] = (poisonDamage, burnDamage, acidDamage, bleedDamage);
        }

        /// <summary>
        /// Stores a hint whose parts sum to <paramref name="actualHpLost"/> (after shields / damage reduction / clamp),
        /// scaling poison/burn/acid/bleed from the pre-mitigation <paramref name="requestedTotal"/> composition.
        /// Order: poison → burn → acid → bleed.
        /// </summary>
        public static void RecordAfterMitigation(
            string entityId,
            int poisonRequested,
            int burnRequested,
            int acidRequested,
            int bleedRequested,
            int requestedTotal,
            int actualHpLost)
        {
            if (CombatUiMuteScope.IsMuted)
                return;
            if (string.IsNullOrEmpty(entityId) || actualHpLost <= 0)
                return;

            if (requestedTotal <= 0)
                return;

            if (requestedTotal == actualHpLost)
            {
                SetPending(entityId, poisonRequested, burnRequested, acidRequested, bleedRequested);
                return;
            }

            int p = (int)Math.Round(poisonRequested * (double)actualHpLost / requestedTotal);
            int b = (int)Math.Round(burnRequested * (double)actualHpLost / requestedTotal);
            int a = (int)Math.Round(acidRequested * (double)actualHpLost / requestedTotal);
            int l = (int)Math.Round(bleedRequested * (double)actualHpLost / requestedTotal);
            int sum = p + b + a + l;
            int diff = actualHpLost - sum;
            if (diff != 0)
            {
                if (poisonRequested > 0)
                    p += diff;
                else if (burnRequested > 0)
                    b += diff;
                else if (acidRequested > 0)
                    a += diff;
                else if (bleedRequested > 0)
                    l += diff;
                else if (p > 0)
                    p += diff;
                else if (b > 0)
                    b += diff;
                else if (a > 0)
                    a += diff;
                else
                    l += diff;
            }

            SetPending(entityId, p, b, a, l);
        }

        /// <summary>
        /// If a pending hint exists for <paramref name="entityId"/> and its parts sum to <paramref name="totalDamage"/>,
        /// returns ordered segments (poison, burn, acid, bleed; zero amounts omitted). Otherwise removes a mismatched hint and returns false.
        /// </summary>
        public static bool TryConsume(string entityId, int totalDamage, out List<HealthBarDeltaDamagePart>? parts)
        {
            parts = null;
            if (!Pending.TryGetValue(entityId, out var p))
                return false;

            Pending.Remove(entityId);
            int sum = p.poison + p.burn + p.acid + p.bleed;
            if (sum != totalDamage || totalDamage <= 0)
                return false;

            var list = new List<HealthBarDeltaDamagePart>(4);
            if (p.poison > 0)
                list.Add(new HealthBarDeltaDamagePart { Kind = HealthBarDotDamageKind.Poison, Amount = p.poison });
            if (p.burn > 0)
                list.Add(new HealthBarDeltaDamagePart { Kind = HealthBarDotDamageKind.Burn, Amount = p.burn });
            if (p.acid > 0)
                list.Add(new HealthBarDeltaDamagePart { Kind = HealthBarDotDamageKind.Acid, Amount = p.acid });
            if (p.bleed > 0)
                list.Add(new HealthBarDeltaDamagePart { Kind = HealthBarDotDamageKind.Bleed, Amount = p.bleed });
            parts = list;
            return true;
        }

        public static void ClearAll() => Pending.Clear();
    }
}
