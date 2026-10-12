using System;
using System.Collections.Generic;
using System.Linq;

namespace RPGGame
{
    /// <summary>
    /// The six hybrid trees (class pairs). Each has a secondary attribute (parent stat A + parent stat B), a hybrid tag
    /// that both parents' class tags count as, two unique actions and two accumulation nodes.
    /// See Documentation/SKILL_TREE_DESIGN.md (Hybrid trees).
    /// </summary>
    public sealed record HybridInfo(
        string Key,
        string Tag,
        string Attribute,
        string StatA,
        string StatB,
        WeaponType ParentA,
        WeaponType ParentB,
        string AttributeEffect,
        string MaterialEffect,
        string AccumulateAttributeEffect,
        string AccumulateTagEffect)
    {
        public string ClassA => ClassTags.ClassOf(ParentA)!;
        public string ClassB => ClassTags.ClassOf(ParentB)!;
        public bool Has(WeaponType w) => w == ParentA || w == ParentB;
    }

    public static class HybridTrees
    {
        /// <summary>Class points needed in each parent before the hybrid tree opens.</summary>
        public const int UnlockPointsPerParent = 10;

        /// <summary>Lower parent's class points needed per hybrid tier (index = tier).</summary>
        public static readonly int[] TierGates = { 10, 10, 15, 20, 30 };

        public static readonly HybridInfo[] All =
        {
            Make("Warbrute", "Body", "STR", "AGI", WeaponType.Mace, WeaponType.Sword),
            Make("Ravager", "Prowess", "STR", "TEC", WeaponType.Mace, WeaponType.Dagger),
            Make("Ragecaller", "Will", "STR", "INT", WeaponType.Mace, WeaponType.Wand),
            Make("Duelist", "Finesse", "AGI", "TEC", WeaponType.Sword, WeaponType.Dagger),
            Make("Spellblade", "Wits", "AGI", "INT", WeaponType.Sword, WeaponType.Wand),
            Make("Hexblade", "Mind", "TEC", "INT", WeaponType.Dagger, WeaponType.Wand)
        };

        private static HybridInfo Make(string key, string attribute, string a, string b, WeaponType pa, WeaponType pb)
        {
            string k = key.ToLowerInvariant();
            return new HybridInfo(key, k, attribute, a, b, pa, pb,
                $"{k}_attribute", $"{k}_material", $"{k}_accumulate_attribute", $"{k}_accumulate_tag");
        }

        public static HybridInfo? ByKey(string? key) =>
            All.FirstOrDefault(h => string.Equals(h.Key, key, StringComparison.OrdinalIgnoreCase));

        public static HybridInfo? ForPair(WeaponType a, WeaponType b) =>
            All.FirstOrDefault(h => (h.ParentA == a && h.ParentB == b) || (h.ParentA == b && h.ParentB == a));

        public static HybridInfo? ForTree(SkillTreeDefinition? tree) => tree == null ? null : ByKey(tree.ClassKey);

        public static HybridInfo? ByAttributeName(string? name) =>
            All.FirstOrDefault(h => string.Equals(h.Attribute, name?.Trim(), StringComparison.OrdinalIgnoreCase));

        public static int LowerParentPoints(CharacterProgression p, HybridInfo h) =>
            Math.Min(p.GetClassPoints(h.ParentA), p.GetClassPoints(h.ParentB));

        public static bool IsUnlocked(CharacterProgression? p, HybridInfo h) =>
            p != null && LowerParentPoints(p, h) >= UnlockPointsPerParent;

        public static int TierGate(int tier) => TierGates[Math.Clamp(tier, 0, TierGates.Length - 1)];

        /// <summary>Points the hybrid tree has consumed from <paramref name="parent"/>: half of the tree's spend (first parent rounds up).</summary>
        public static int SpentFromParent(IReadOnlyDictionary<string, int> learned, SkillTreeDefinition hybridTree, WeaponType parent)
        {
            var h = ForTree(hybridTree);
            if (h == null || !h.Has(parent))
                return 0;
            int total = 0;
            foreach (var node in hybridTree.Nodes)
                if (learned.TryGetValue(node.Id, out int rank) && rank > 0)
                    total += Math.Max(0, node.Cost) * rank;
            return parent == h.ParentA ? (total + 1) / 2 : total / 2;
        }

        // ------------------------------------------------------------ attributes and tags

        private static int Stat(Character c, string code) => code switch
        {
            "STR" => c.GetEffectiveStrength(),
            "AGI" => c.GetEffectiveAgility(),
            "TEC" => c.GetEffectiveTechnique(),
            "INT" => c.GetEffectiveIntelligence(),
            _ => 0
        };

        /// <summary>Secondary attribute: parent stat A + parent stat B, plus this fight's accumulation.</summary>
        public static int SecondaryAttribute(Character c, HybridInfo h) =>
            Stat(c, h.StatA) + Stat(c, h.StatB) + SkillEffectRouter.Instance.GetHybridAttributeAccumulation(c, h);

        /// <summary>The hybrid whose secondary attribute is the hero's primary attribute (attribute node learned), if any.</summary>
        public static HybridInfo? PrimaryAttributeHybrid(Character? c)
        {
            if (c == null || c is Enemy) return null;
            return All.FirstOrDefault(h => SkillRanks.GetRank(c, h.AttributeEffect) > 0);
        }

        /// <summary>Hybrid tags: both parents' class tags (once the material node is learned) plus this fight's accumulation.</summary>
        public static int TagCount(Character c, HybridInfo h)
        {
            int count = SkillRanks.GetRank(c, h.MaterialEffect) > 0
                ? ClassTags.CountOwn(c, h.ClassA) + ClassTags.CountOwn(c, h.ClassB)
                : 0;
            return count + SkillEffectRouter.Instance.GetHybridTagAccumulation(c, h);
        }

        /// <summary>Partner classes whose tags also count for <paramref name="classTag"/> (learned hybrid material nodes).</summary>
        public static IEnumerable<string> MergedPartners(Character? c, string classTag)
        {
            if (c == null) yield break;
            foreach (var h in All)
            {
                if (SkillRanks.GetRank(c, h.MaterialEffect) <= 0) continue;
                if (string.Equals(h.ClassA, classTag, StringComparison.OrdinalIgnoreCase)) yield return h.ClassB;
                else if (string.Equals(h.ClassB, classTag, StringComparison.OrdinalIgnoreCase)) yield return h.ClassA;
            }
        }
    }
}
