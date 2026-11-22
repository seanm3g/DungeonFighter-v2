using System;
using System.Collections.Generic;
using System.Linq;

namespace RPGGame.Combat.Rules
{
    /// <summary>
    /// Enemy- and room-owned rules (one rule engine, step 6, 2026-11-20; design D6.15). An enemy's trigger affixes and
    /// the room's starting status are rules on the engine, resolved in owner order (the enemy's affixes in the order it
    /// got them, then the room) and traced like hero rules:
    /// <list type="bullet">
    /// <item>on-hit affix (Venomous, Kindled, …): <c>ONCONNECT CHANCE c% -> APPLY status [n]</c> on whoever the enemy
    /// hit; the Demon's second phase multiplies its region affix's chance</item>
    /// <item>start affix (Bulwark, Molten Shell): <c>ONFIGHTSTART -> ADD status n</c> on the enemy</item>
    /// <item>the room's enemy start status (Garrisoned): a room-owned <c>ONFIGHTSTART -> ADD status n</c></item>
    /// </list>
    /// Stat levers (HP, damage, speed, damage taken, evasion, lifesteal, extra action) and the Demon phase stay spawn data
    /// in <see cref="EnemyAffixRuntime"/>.
    /// </summary>
    public static class EnemyRules
    {
        public const string FightStart = "ONFIGHTSTART";

        /// <summary>The enemy's rules, then the room's, in owner order.</summary>
        public static List<RuleEntry> For(Enemy enemy, Environment? room = null)
        {
            var list = new List<RuleEntry>();
            int i = 0;
            foreach (var affix in enemy.Affixes)
            {
                int order = RuleBook.EnemyOrder + i++;
                if (string.IsNullOrWhiteSpace(affix.Status))
                    continue;
                string status = affix.Status.Trim().ToLowerInvariant();
                string lever = affix.Lever?.Trim().ToLowerInvariant() ?? "";
                var owner = new RuleOwner(RuleOwnerKind.Enemy, affix.Name ?? enemy.Name ?? "enemy", order);
                if (lever == EnemyAffixRuntime.LeverOnHit)
                {
                    double chance = Math.Min(1.0, affix.Chance * EnemyAffixRuntime.PhaseFactor(enemy, affix));
                    if (chance <= 0)
                        continue;
                    string pct = chance >= 1 ? "" : $" CHANCE {chance * 100:0.##}%";
                    list.Add(new RuleEntry(new CrossWireRule
                    {
                        When = "ONCONNECT", Verb = "APPLY", Store = status, Amount = affix.Value,
                        Chance = chance >= 1 ? 0 : chance, Source = owner.Name,
                        Text = $"ONCONNECT{pct} -> APPLY {status}" + (affix.Value > 0 ? $" {affix.Value:0.##}" : "")
                    }, owner, 0));
                }
                else if (lever == EnemyAffixRuntime.LeverStart)
                {
                    int n = Math.Max(1, affix.Stacks);
                    list.Add(new RuleEntry(new CrossWireRule
                    {
                        When = FightStart, Verb = "ADD", Store = status, Amount = n, Source = owner.Name,
                        Text = $"{FightStart} -> ADD {status} {n}"
                    }, owner, 0));
                }
            }
            if (room?.RoomAffix is { } roomAffix && !string.IsNullOrWhiteSpace(roomAffix.EnemyStartStatus))
            {
                string status = roomAffix.EnemyStartStatus.Trim().ToLowerInvariant();
                int n = Math.Max(1, roomAffix.EnemyStartStacks);
                var owner = new RuleOwner(RuleOwnerKind.Room, roomAffix.Name ?? room.Name ?? "room", RuleBook.RoomOrder);
                list.Add(new RuleEntry(new CrossWireRule
                {
                    When = FightStart, Verb = "ADD", Store = status, Amount = n, Source = owner.Name,
                    Text = $"{FightStart} -> ADD {status} {n} (the room's enemy)"
                }, owner, 0));
            }
            return list;
        }

        /// <summary>The enemy landed a hit on <paramref name="target"/>: its on-hit rules roll their chance and apply.</summary>
        public static bool OnHit(Enemy enemy, Actor target, List<string> messages, Random random)
        {
            var matched = For(enemy).Where(e => e.Rule.When == "ONCONNECT").ToList();
            if (matched.Count == 0)
                return false;
            using var scope = RuleEngine.Scope();
            bool any = false;
            foreach (var entry in RuleEngine.InOrder(matched))
            {
                var rule = entry.Rule;
                if (rule.Chance > 0 && !(random.NextDouble() < rule.Chance))
                    continue;
                if (!CombatEffectsSimplified.ApplyNamedStatus(target, rule.Store, messages))
                    continue;
                any = true;
                RuleEngine.Record(enemy, entry, "ActionHit");
            }
            return any;
        }

        /// <summary>Fight start: the enemy's start rules and the room's put their statuses on the enemy (each stack once).</summary>
        public static void OnFightStart(Enemy enemy, Environment? room, List<string> messages)
        {
            var matched = For(enemy, room).Where(e => e.Rule.When == FightStart).ToList();
            if (matched.Count == 0)
                return;
            using var scope = RuleEngine.Scope();
            foreach (var entry in RuleEngine.InOrder(matched))
            {
                bool landed = false;
                for (int k = 0; k < (int)Math.Round(entry.Rule.Amount); k++)
                    landed |= CombatEffectsSimplified.ApplyNamedStatus(enemy, entry.Rule.Store, messages);
                if (landed)
                    RuleEngine.Record(enemy, entry, FightStart);
            }
        }
    }
}
