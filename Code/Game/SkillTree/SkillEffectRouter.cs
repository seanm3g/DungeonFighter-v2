using System;
using System.Collections.Generic;
using System.Linq;
using RPGGame.Combat.Events;
using RPGGame.Data;

namespace RPGGame
{
    /// <summary>
    /// Skill-tree effects that are still code: the hybrid trees, the conversion table pass, per-rank pack mods
    /// (node <c>damageModPerRank</c> / <c>healOnHitPerRank</c>) and the attribute bridge for rule attributes.
    /// Every other class-tree node is a rule (<c>crossWire</c>, Code/Combat/CrossWires.cs); the custom state machines
    /// were converted to rules or cut on 2026-11-18. Fight-scoped state resets when a battle narrative starts.
    /// </summary>
    public sealed partial class SkillEffectRouter
    {
        public static SkillEffectRouter Instance { get; } = new();

        private Character? _boundCharacter;
        private bool _subscribed;

        private SkillEffectRouter() { }

        public void RefreshForCharacter(Character? character)
        {
            _boundCharacter = character;
            EnsureSubscribed();
        }

        public void ResetFightState() => ResetHybridFightState();

        private void EnsureSubscribed()
        {
            var bus = CombatEventBus.Instance;
            // New games clear the bus (StartNewGameCommand / MainMenuHandler); subscribing only once left every on-hit
            // skill dead for the rest of the session. Re-subscribe whenever our handler is gone.
            if (_subscribed && bus.HasSubscriber(CombatEventType.ActionHit, OnHit)) return;
            bus.Unsubscribe(CombatEventType.ActionHit, OnHit);
            bus.Unsubscribe(CombatEventType.ActionCritical, OnHit);
            bus.Subscribe(CombatEventType.ActionHit, OnHit);
            bus.Subscribe(CombatEventType.ActionCritical, OnHit);
            _subscribed = true;
        }

        /// <summary>Rank x the node's <see cref="SkillTreeNodeDefinition.Power"/>: what per-rank magnitudes multiply by.</summary>
        private double GetPower(Character? c, string customEffectId) => SkillRanks.GetPower(c, customEffectId);

        /// <summary>
        /// Standing attribute from rule nodes (Rite of Passage, Kinship, …: <c>WHILE -> STR 10</c>), folded into
        /// <see cref="Character.GetEffectiveStrength"/> and the other effective getters.
        /// </summary>
        public int GetSkillAttributeBonus(Character? c, string? statCode)
        {
            if (c == null || string.IsNullOrWhiteSpace(statCode)) return 0;
            string code = statCode.Trim().ToUpperInvariant();
            return code is "STR" or "AGI" or "TEC" or "INT"
                ? (int)Math.Round(CrossWires.Passive(c, code))
                : 0;
        }

        private Character? AsBoundHero(Actor? source)
        {
            if (_boundCharacter != null && ReferenceEquals(source, _boundCharacter))
                return _boundCharacter;
            if (source is Character c && c is not Enemy)
                return c;
            return null;
        }

        private void OnHit(CombatEvent evt)
        {
            var hero = AsBoundHero(evt.Source);
            if (hero == null) return;
            using var skillPin = SkillRanks.Pin(hero.Progression);

            ApplyHybridOnHit(hero, evt);
            // Venom Mace, Envenomed Edge, Hemorrhage: GameData/Conversions.json (phase early).
            ConversionTable.Apply(hero, evt.Target, evt.IsCritical, "early");
            // Kindle, Thermal Shock: GameData/Conversions.json (phase late).
            ConversionTable.Apply(hero, evt.Target, evt.IsCritical, "late");
            ApplyDataDrivenRankMods(hero, evt);
        }

        private void AddDamageMod(Character hero, double value)
        {
            if (value <= 0) return;
            hero.Effects.AccumulateConsumedModifierBonuses(new List<ActionAttackBonusItem>
            {
                new() { Type = "DAMAGE_MOD", Value = value }
            });
        }

        private void AddSpeedMod(Character hero, double value)
        {
            if (value <= 0) return;
            hero.Effects.AccumulateConsumedModifierBonuses(new List<ActionAttackBonusItem>
            {
                new() { Type = "SPEED_MOD", Value = value }
            });
        }

        private void AddAmpMod(Character hero, double value)
        {
            if (value <= 0) return;
            hero.Effects.ConsumedSkillAmpPercent += value;
        }

        private void ApplyDataDrivenRankMods(Character hero, CombatEvent evt)
        {
            if (hero.Progression == null) return;
            WeaponType? path = hero.Progression.GetPrimaryClassWeaponType();
            if (path == null) return;
            if (hero.Equipment.Weapon is not WeaponItem w || w.WeaponType != path.Value)
                return;

            int dmgMod = 0;
            int heal = 0;
            foreach (var kv in hero.Progression.LearnedSkillRanks)
            {
                if (kv.Value <= 0) continue;
                var node = SkillTreeService.Trees.GetNode(kv.Key);
                if (node == null) continue;
                // Only apply pack mods from the equipped primary path tree.
                var owner = SkillTreeService.Trees.GetTreeForWeapon(path.Value);
                if (owner == null || !owner.Nodes.Any(n =>
                        string.Equals(n.Id, node.Id, StringComparison.OrdinalIgnoreCase)))
                    continue;
                int rank = kv.Value + CrossWires.SkillRankBonus(hero, kv.Key);
                if (node.DamageModPerRank > 0)
                    dmgMod += node.DamageModPerRank * rank;
                if (node.HealOnHitPerRank > 0)
                    heal += node.HealOnHitPerRank * rank;
            }

            if (dmgMod > 0)
            {
                hero.Effects.AccumulateConsumedModifierBonuses(new List<ActionAttackBonusItem>
                {
                    new() { Type = "DAMAGE_MOD", Value = dmgMod }
                });
            }
            if (heal > 0)
                hero.Heal(heal);
        }
    }
}
