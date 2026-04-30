using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using RPGGame.Data;

namespace RPGGame
{
    /// <summary>
    /// One row from CLASS ACTIONS sheet / ClassActions.json: class path, tier band (and optional min class points), action name.
    /// </summary>
    public sealed class ClassActionUnlockRule
    {
        /// <summary>Tier band 1–4 uses <see cref="ClassPresentationConfig.GetTierBandIndex"/> (same as weapon-path ranks). 0 = any class points ≥ 1 (pre–tier-1 investment).</summary>
        [JsonPropertyName("tier")]
        public int Tier { get; set; }

        /// <summary>Barbarian / Warrior / Rogue / Wizard or Mace / Sword / Dagger / Wand (case-insensitive).</summary>
        [JsonPropertyName("classKey")]
        public string ClassKey { get; set; } = "";

        [JsonPropertyName("actionName")]
        public string ActionName { get; set; } = "";

        /// <summary>When set, requires this many class points on the path in addition to the tier rule.</summary>
        [JsonPropertyName("minClassPoints")]
        public int? MinClassPoints { get; set; }
    }

    /// <summary>Loaded from GameData/ClassActions.json (sheet pull) or built-in defaults.</summary>
    public sealed class ClassActionsUnlockConfig
    {
        private static readonly JsonSerializerOptions JsonRead = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        };

        [JsonPropertyName("rules")]
        public List<ClassActionUnlockRule> Rules { get; set; } = new();

        /// <summary>Built-in rules mirroring legacy ClassActionManager thresholds (tier + optional min points).</summary>
        public static ClassActionsUnlockConfig CreateBuiltInDefaults()
        {
            return new ClassActionsUnlockConfig
            {
                Rules = new List<ClassActionUnlockRule>
                {
                    new ClassActionUnlockRule { Tier = 1, ClassKey = "Barbarian", ActionName = "FOLLOW THROUGH" },
                    new ClassActionUnlockRule { Tier = 1, ClassKey = "Barbarian", ActionName = "BERSERK", MinClassPoints = 3 },
                    new ClassActionUnlockRule { Tier = 0, ClassKey = "Warrior", ActionName = "TAUNT" },
                    new ClassActionUnlockRule { Tier = 1, ClassKey = "Warrior", ActionName = "SHIELD BASH", MinClassPoints = 3 },
                    new ClassActionUnlockRule { Tier = 1, ClassKey = "Warrior", ActionName = "DEFENSIVE STANCE", MinClassPoints = 3 },
                    new ClassActionUnlockRule { Tier = 1, ClassKey = "Rogue", ActionName = "MISDIRECT" },
                    new ClassActionUnlockRule { Tier = 1, ClassKey = "Rogue", ActionName = "QUICK REFLEXES", MinClassPoints = 3 },
                    new ClassActionUnlockRule { Tier = 0, ClassKey = "Wizard", ActionName = "CHANNEL" },
                    new ClassActionUnlockRule { Tier = 1, ClassKey = "Wizard", ActionName = "FIREBALL", MinClassPoints = 3 },
                    new ClassActionUnlockRule { Tier = 1, ClassKey = "Wizard", ActionName = "FOCUS", MinClassPoints = 3 }
                }
            };
        }

        public static ClassActionsUnlockConfig? TryLoadFromGameDataFile(string? fileName = null)
        {
            fileName ??= GameConstants.ClassActionsJson;
            string? path = JsonLoader.FindGameDataFile(fileName);
            if (string.IsNullOrEmpty(path) || !File.Exists(path))
                return null;
            try
            {
                string json = File.ReadAllText(path);
                var cfg = JsonSerializer.Deserialize<ClassActionsUnlockConfig>(json, JsonRead);
                cfg = cfg?.Normalize();
                if (cfg == null || cfg.Rules.Count == 0)
                    return null;
                return cfg;
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// CLASS ACTIONS sheet often uses short names; game data in <c>Actions.json</c> uses the full definition key.
        /// </summary>
        private static string MapImportedActionNameToGameDefinition(string actionName)
        {
            if (string.Equals(actionName, "PUNCH", StringComparison.OrdinalIgnoreCase))
                return "PUNCH HARD";
            return actionName;
        }

        public ClassActionsUnlockConfig Normalize()
        {
            if (Rules == null)
                Rules = new List<ClassActionUnlockRule>();
            Rules = Rules
                .Where(r => r != null && !string.IsNullOrWhiteSpace(r.ActionName) && !string.IsNullOrWhiteSpace(r.ClassKey))
                .Select(r =>
                {
                    r.ClassKey = r.ClassKey.Trim();
                    r.ActionName = MapImportedActionNameToGameDefinition(r.ActionName.Trim());
                    r.Tier = Math.Clamp(r.Tier, 0, ClassPresentationConfig.TierSlotCount);
                    if (r.MinClassPoints is int mp && mp < 1)
                        r.MinClassPoints = null;
                    return r;
                })
                .ToList();
            return this;
        }

        public IReadOnlyCollection<string> AllRuleActionNames() =>
            Rules.Select(r => r.ActionName).Where(n => !string.IsNullOrWhiteSpace(n)).Distinct(StringComparer.OrdinalIgnoreCase).ToList();

        public static bool IsRuleUnlocked(ClassActionUnlockRule rule, int classPathPoints, ClassPresentationConfig presentation)
        {
            presentation = presentation.EnsureNormalized();
            if (classPathPoints <= 0)
                return false;
            if (rule.MinClassPoints is int mp && classPathPoints < mp)
                return false;

            int tier = rule.Tier;
            if (tier <= 0)
                return classPathPoints >= 1;

            int band = presentation.GetTierBandIndex(classPathPoints);
            return band >= tier;
        }

        /// <summary>Maps sheet/class column text to weapon path; accepts display names from <see cref="ClassPresentationConfig"/>.</summary>
        public static bool TryResolveClassKeyToWeaponType(string classKey, ClassPresentationConfig presentation, out WeaponType weaponType)
        {
            weaponType = WeaponType.Sword;
            if (string.IsNullOrWhiteSpace(classKey))
                return false;
            presentation = presentation.EnsureNormalized();
            string k = classKey.Trim();

            if (k.Equals("Mace", StringComparison.OrdinalIgnoreCase)
                || k.Equals(presentation.MaceClassDisplayName, StringComparison.OrdinalIgnoreCase)
                || k.Equals("Barbarian", StringComparison.OrdinalIgnoreCase))
            {
                weaponType = WeaponType.Mace;
                return true;
            }

            if (k.Equals("Sword", StringComparison.OrdinalIgnoreCase)
                || k.Equals(presentation.SwordClassDisplayName, StringComparison.OrdinalIgnoreCase)
                || k.Equals("Warrior", StringComparison.OrdinalIgnoreCase))
            {
                weaponType = WeaponType.Sword;
                return true;
            }

            if (k.Equals("Dagger", StringComparison.OrdinalIgnoreCase)
                || k.Equals(presentation.DaggerClassDisplayName, StringComparison.OrdinalIgnoreCase)
                || k.Equals("Rogue", StringComparison.OrdinalIgnoreCase))
            {
                weaponType = WeaponType.Dagger;
                return true;
            }

            if (k.Equals("Wand", StringComparison.OrdinalIgnoreCase)
                || k.Equals(presentation.WandClassDisplayName, StringComparison.OrdinalIgnoreCase)
                || k.Equals("Wizard", StringComparison.OrdinalIgnoreCase))
            {
                weaponType = WeaponType.Wand;
                return true;
            }

            return false;
        }

        public static int GetClassPointsForWeapon(CharacterProgression progression, WeaponType path, ClassPresentationConfig presentation)
        {
            presentation = presentation.EnsureNormalized();
            return presentation.GetClassPoints(path, progression.BarbarianPoints, progression.WarriorPoints, progression.RoguePoints, progression.WizardPoints);
        }
    }
}
