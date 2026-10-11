using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using RPGGame.Combat.Calculators;

namespace RPGGame.Combat.Narrative
{
    /// <summary>Builds <see cref="CombatScenePacket"/> rows from live combat / dungeon state.</summary>
    public static class CombatScenePacketBuilder
    {
        /// <summary>
        /// Action beats keep the establishing ultra-wide framing; fighters stay small in one corner
        /// so ComfyUI img2img / regional prompts can lock the room from row 0.
        /// </summary>
        public const string DefaultCamera =
            "same ultra-wide room camera, fighters small in the far corner";
        public const string CritCamera =
            "same ultra-wide room camera, impact beat with fighters small in the far corner";
        public const string MissCamera =
            "same ultra-wide room camera, recovery stumble with fighters small in the far corner";
        public const string EstablishingCamera =
            "ultra-wide empty room establishing shot, no characters";

        /// <summary>
        /// Shared composition lock for ComfyUI: generate row 0 first, then img2img / regional
        /// prompt later rows so the room stays dominant and combat stays in one corner.
        /// </summary>
        public const string CornerActionComposition =
            "environment fills the frame; combatants occupy a small far-corner region only";

        private static readonly JsonSerializerOptions JsonOptions = new()
        {
            WriteIndented = true,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
        };

        public static IReadOnlyList<CombatScenePacket> BuildLog(
            Character? hero,
            Enemy? enemy,
            Environment? room,
            Dungeon? dungeon,
            BattleNarrative? narrative,
            Action? lastPlayerAction = null)
        {
            string environment = ResolveEnvironment(room, dungeon, narrative);
            var events = narrative?.GetAllEvents() ?? new List<BattleEvent>();
            if (events.Count == 0)
            {
                var snapshot = BuildSnapshot(hero, enemy, environment, lastPlayerAction);
                if (snapshot == null)
                    return Array.Empty<CombatScenePacket>();
                return new[] { BuildEstablishingShot(environment), snapshot };
            }

            var scenes = new List<CombatScenePacket>(events.Count + 1)
            {
                BuildEstablishingShot(environment)
            };
            foreach (var evt in events)
            {
                if (evt == null)
                    continue;
                scenes.Add(BuildFromEvent(evt, hero, enemy, environment, lastPlayerAction));
            }

            return scenes;
        }

        /// <summary>
        /// Room-only opener: no fighters, gear, or combat action — wide establishing camera.
        /// </summary>
        public static CombatScenePacket BuildEstablishingShot(string environment)
        {
            string place = NonEmpty(environment, "the dungeon");
            return new CombatScenePacket
            {
                Character = null,
                Action = null,
                Target = null,
                Environment = place,
                Emotion = FormatEnvironmentEmotion(place),
                Camera = EstablishingCamera,
                Composition = CornerActionComposition,
                Gear = null,
                Effects = BuildEnvironmentEffects(place)
            };
        }

        public static CombatScenePacket BuildFromEvent(
            BattleEvent evt,
            Character? hero,
            Enemy? enemy,
            string environment,
            Action? lastPlayerAction = null)
        {
            if (evt == null) throw new ArgumentNullException(nameof(evt));

            Character? actor = ResolveActor(evt.Actor, hero, enemy);
            Character? targetChar = ResolveActor(evt.Target, hero, enemy);
            Action? action = ResolveAction(evt.Action, actor, hero, lastPlayerAction);

            return new CombatScenePacket
            {
                Character = NonEmpty(evt.Actor, actor?.Name, "Hero"),
                Action = FormatActionPhrase(evt, action, actor),
                Target = NonEmpty(evt.Target, targetChar?.Name, "foe"),
                Environment = NonEmpty(environment, "the dungeon"),
                Emotion = FormatEmotion(evt, action, actor),
                Camera = FormatCamera(evt),
                Composition = CornerActionComposition,
                Gear = BuildGear(actor),
                Effects = BuildEffects(evt, action, actor, targetChar, environment)
            };
        }

        public static string Serialize(IReadOnlyList<CombatScenePacket> scenes)
        {
            if (scenes == null || scenes.Count == 0)
                return "[]";
            if (scenes.Count == 1)
                return JsonSerializer.Serialize(scenes[0], JsonOptions);
            return JsonSerializer.Serialize(scenes, JsonOptions);
        }

        public static string ResolveEnvironment(Environment? room, Dungeon? dungeon, BattleNarrative? narrative)
        {
            if (!string.IsNullOrWhiteSpace(room?.Name))
                return room!.Name.Trim();
            if (!string.IsNullOrWhiteSpace(narrative?.Location) &&
                !string.Equals(narrative.Location, "the dungeon", StringComparison.OrdinalIgnoreCase))
                return narrative.Location.Trim();
            if (!string.IsNullOrWhiteSpace(dungeon?.Theme))
                return dungeon!.Theme.Trim();
            if (!string.IsNullOrWhiteSpace(dungeon?.Name))
                return dungeon!.Name.Trim();
            if (!string.IsNullOrWhiteSpace(narrative?.Location))
                return narrative.Location.Trim();
            return "the dungeon";
        }

        internal static string FormatActionPhrase(BattleEvent evt, Action? action, Character? actor)
        {
            string raw = (action?.Name ?? evt.Action ?? "strike").Trim();
            if (string.IsNullOrEmpty(raw))
                raw = "strike";
            string lower = raw.ToLowerInvariant();

            string stancePrefix = "";
            if (action != null)
            {
                string stance = StandingBlock.GetStanceName(action.BlockPercent);
                if (stance == "aggressive" && !lower.Contains("lunging", StringComparison.Ordinal))
                    stancePrefix = "lunging ";
                else if (stance == "defensive" && !lower.Contains("guarded", StringComparison.Ordinal))
                    stancePrefix = "guarded ";
            }

            // Heroes only: enemies get a random combat-math weapon (e.g. Bludgeon) that is not
            // their visual identity — never invent "mace rat swarm" from that placeholder kit.
            string weaponFlavor = "";
            if (actor is Character and not Enemy && actor.Weapon is WeaponItem weapon)
            {
                string wt = weapon.WeaponType.ToString().ToLowerInvariant();
                if (!string.IsNullOrEmpty(wt) && !lower.Contains(wt, StringComparison.Ordinal))
                    weaponFlavor = wt + " ";
            }

            return (stancePrefix + weaponFlavor + lower).Trim();
        }

        internal static string FormatEmotion(BattleEvent evt, Action? action, Character? actor)
        {
            if (evt.NaturalRoll == 1 || (!evt.IsSuccess && !evt.IsHeal))
                return "off-balance";
            if (evt.IsCritical && evt.IsSuccess)
                return "fierce and aggressive";
            if (evt.IsCombo && evt.IsSuccess)
                return "focused and aggressive";

            double block = action?.BlockPercent
                ?? actor?.StandingBlockPercent
                ?? StandingBlock.DefaultBlockPercent;
            string stance = StandingBlock.GetStanceName(block);
            return stance switch
            {
                "aggressive" => "focused and aggressive",
                "defensive" => "guarded",
                _ => "focused"
            };
        }

        internal static string FormatCamera(BattleEvent evt)
        {
            if (evt.IsCritical && evt.IsSuccess)
                return CritCamera;
            if (!evt.IsSuccess && !evt.IsHeal)
                return MissCamera;
            return DefaultCamera;
        }

        internal static CombatSceneGear? BuildGear(Character? actor)
        {
            // Enemies receive a random tier-1 weapon for combat math only — omit that from
            // image prompts so Rats / packs are not described as holding Bludgeon / etc.
            if (actor == null || actor is Enemy)
                return null;

            var gear = new CombatSceneGear
            {
                Helmet = ItemName(actor.Head),
                Cloak = ResolveCloak(actor),
                Armor = ItemName(actor.Body),
                WeaponMain = ItemName(actor.Weapon),
                WeaponOffhand = null
            };
            if (gear.Helmet == null &&
                gear.Cloak == null &&
                gear.Armor == null &&
                gear.WeaponMain == null &&
                gear.WeaponOffhand == null)
                return null;
            return gear;
        }

        internal static string FormatEnvironmentEmotion(string environment)
        {
            string env = environment ?? "";
            if (ContainsAny(env, "crypt", "tomb", "grave", "haunt", "undead", "necro"))
                return "ominous and still";
            if (ContainsAny(env, "crumbl", "ruin", "dust", "cave", "dungeon", "hall"))
                return "weathered and still";
            if (ContainsAny(env, "forest", "grove", "wood", "glade"))
                return "quiet and watchful";
            if (ContainsAny(env, "fire", "ash", "scorch", "lava"))
                return "smoldering and tense";
            return "still and waiting";
        }

        internal static IReadOnlyList<string> BuildEnvironmentEffects(string environment)
        {
            var effects = new List<string>();
            void Add(string? effect)
            {
                if (string.IsNullOrWhiteSpace(effect))
                    return;
                if (!effects.Exists(e => string.Equals(e, effect, StringComparison.OrdinalIgnoreCase)))
                    effects.Add(effect);
            }

            string env = environment ?? "";
            if (ContainsAny(env, "crypt", "cave", "dungeon", "dust", "ruin", "tomb", "crumbl", "hall"))
                Add("dust");
            if (ContainsAny(env, "crypt", "undead", "necro", "grave", "tomb", "haunt"))
                Add("faint green necrotic glow");
            if (ContainsAny(env, "fire", "ash", "scorch", "lava"))
                Add("embers");
            if (ContainsAny(env, "forest", "grove", "wood", "glade"))
                Add("drifting motes");

            return effects;
        }

        internal static IReadOnlyList<string> BuildEffects(
            BattleEvent evt,
            Action? action,
            Character? actor,
            Character? target,
            string environment)
        {
            var effects = new List<string>(BuildEnvironmentEffects(environment));
            void Add(string? effect)
            {
                if (string.IsNullOrWhiteSpace(effect))
                    return;
                if (!effects.Exists(e => string.Equals(e, effect, StringComparison.OrdinalIgnoreCase)))
                    effects.Add(effect);
            }

            if (evt.IsCritical && evt.IsSuccess)
                Add("sparks");

            if (LooksNecrotic(environment ?? "", action, target, evt))
                Add("faint green necrotic glow");

            if (target != null)
            {
                if (target.PoisonPercentOfMaxHealth > 0)
                    Add("faint green necrotic glow");
                if (target.BurnIntensity > 0 || target.PendingBurnFromHits > 0)
                    Add("embers");
                if (target.IsBleeding)
                    Add("blood mist");
                if (target.AcidIntensity > 0 || target.PendingAcidFromHits > 0)
                    Add("acid haze");
            }

            if (evt.CausesBleed)
                Add("blood mist");
            if (action?.CausesBurn == true)
                Add("embers");
            if (action?.CausesPoison == true)
                Add("faint green necrotic glow");
            if (action?.CausesAcid == true)
                Add("acid haze");

            if (ContainsAny(action?.Name, "slam", "pound", "smash") ||
                ContainsAny(evt.Action, "slam", "pound", "smash"))
                Add("dust");

            if (effects.Count == 0 && evt.IsSuccess)
                Add("sparks");

            return effects;
        }

        private static CombatScenePacket? BuildSnapshot(
            Character? hero,
            Enemy? enemy,
            string environment,
            Action? lastPlayerAction)
        {
            if (hero == null && enemy == null)
                return null;

            var evt = new BattleEvent
            {
                Actor = hero?.Name ?? "Hero",
                Target = enemy?.Name ?? "foe",
                Action = lastPlayerAction?.Name ?? "ready",
                IsSuccess = true
            };
            return BuildFromEvent(evt, hero, enemy, environment, lastPlayerAction);
        }

        private static Character? ResolveActor(string? name, Character? hero, Enemy? enemy)
        {
            if (string.IsNullOrWhiteSpace(name))
                return hero;
            if (hero != null && string.Equals(hero.Name, name, StringComparison.OrdinalIgnoreCase))
                return hero;
            if (enemy != null && string.Equals(enemy.Name, name, StringComparison.OrdinalIgnoreCase))
                return enemy;
            return null;
        }

        private static Action? ResolveAction(
            string? actionName,
            Character? actor,
            Character? hero,
            Action? lastPlayerAction)
        {
            bool actorIsHero = actor != null &&
                hero != null &&
                ReferenceEquals(actor, hero);

            // Only reuse the hero's last swing for hero beats — never stamp it onto enemies
            // (that produced "lunging mace slam" on Rats when RAT SWARM failed to resolve).
            if (actorIsHero &&
                !string.IsNullOrWhiteSpace(actionName) &&
                lastPlayerAction != null &&
                string.Equals(lastPlayerAction.Name, actionName, StringComparison.OrdinalIgnoreCase))
                return lastPlayerAction;

            if (!string.IsNullOrWhiteSpace(actionName) && actor?.ActionPool != null)
            {
                foreach (var entry in actor.ActionPool)
                {
                    if (entry.action != null &&
                        string.Equals(entry.action.Name, actionName, StringComparison.OrdinalIgnoreCase))
                        return entry.action;
                }
            }

            if (!string.IsNullOrWhiteSpace(actionName))
            {
                try
                {
                    var loaded = ActionLoader.GetAction(actionName);
                    if (loaded != null)
                        return loaded;
                }
                catch
                {
                    // Catalog may be unavailable in isolated unit tests.
                }
            }

            if (actorIsHero)
                return lastPlayerAction;

            return null;
        }

        private static string? ResolveCloak(Character actor)
        {
            string? legs = ItemName(actor.Legs);
            if (LooksLikeCloak(legs))
                return legs;
            return null;
        }

        internal static bool LooksLikeCloak(string? name)
        {
            return ContainsAny(name, "cloak", "mantle", "cape", "shawl", "wrap");
        }

        private static bool LooksNecrotic(string environment, Action? action, Character? target, BattleEvent evt)
        {
            if (ContainsAny(environment, "crypt", "undead", "necro", "grave", "tomb", "haunt"))
                return true;
            if (ContainsAny(action?.Name, "necro", "drain", "wither"))
                return true;
            if (ContainsAny(evt.Action, "necro", "drain", "wither"))
                return true;
            if (ContainsAny(target?.Name, "skeleton", "wight", "zombie", "ghoul", "lich"))
                return true;
            return false;
        }

        private static string? ItemName(Item? item)
        {
            if (item == null || string.IsNullOrWhiteSpace(item.Name))
                return null;
            return item.Name.Trim();
        }

        private static string NonEmpty(params string?[] values)
        {
            foreach (var value in values)
            {
                if (!string.IsNullOrWhiteSpace(value))
                    return value.Trim();
            }
            return "";
        }

        private static bool ContainsAny(string? haystack, params string[] needles)
        {
            if (string.IsNullOrWhiteSpace(haystack) || needles == null || needles.Length == 0)
                return false;
            foreach (var needle in needles)
            {
                if (!string.IsNullOrEmpty(needle) &&
                    haystack.Contains(needle, StringComparison.OrdinalIgnoreCase))
                    return true;
            }
            return false;
        }
    }
}
