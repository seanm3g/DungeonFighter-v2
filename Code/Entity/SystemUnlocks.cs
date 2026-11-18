using System;
using System.Collections.Generic;
using System.Linq;

namespace RPGGame
{
    /// <summary>
    /// The systems of the complexity ladder (core design pass 2c, track 1). Each one arrives on its first event
    /// (G1.5), not at a fixed level. Saved by name, so the order here can change freely.
    /// </summary>
    public enum GameSystem
    {
        /// <summary>Dice bands, one action slot and the combat board. Open from the first fight.</summary>
        Board,
        /// <summary>Items and the comparison: the first drop.</summary>
        Items,
        /// <summary>A second strip slot: combos and AMP growth.</summary>
        Combos,
        /// <summary>Order matters: the first gear action next to the starter.</summary>
        ActionInteractions,
        /// <summary>Dungeon selection as a real choice (more than one dungeon to pick from).</summary>
        DungeonSelection,
        /// <summary>Room twists (affix or terrain) and non-hostile encounters.</summary>
        Rooms,
        /// <summary>The first band boss.</summary>
        Bosses,
        /// <summary>Region travel.</summary>
        Regions,
        /// <summary>Dice modifications: the first nudge (Focus / Rattle) or extra die.</summary>
        DiceModifiers,
        /// <summary>Statuses: the first status applied in a fight.</summary>
        Statuses,
        /// <summary>Synergies: the first Uncommon (an action and a modifier about it).</summary>
        Synergies,
        /// <summary>Loot depth: the first Rare (more lines, item level, trade-up).</summary>
        LootDepth,
        /// <summary>Material bonuses: two worn pieces of one material.</summary>
        MaterialSets,
        /// <summary>The skill tree: the first skill point.</summary>
        SkillTree,
        /// <summary>Stores and verbs: the first currency minted.</summary>
        Stores
    }

    /// <summary>
    /// A per-character record of which systems have arrived (Rebuild 0.3). Each system unlocks on its first event and
    /// is saved with the character. Screens, item cards and tabs ask <see cref="IsUnlocked(GameSystem)"/> what to show
    /// ("nothing appears before its rung").
    /// <para>Besides the <see cref="GameSystem"/> rungs it keeps free-form "seen" keys (for example <c>status:bleed</c>),
    /// so later steps can show a NEW card the first time a status or concept appears (D9.3).</para>
    /// </summary>
    public sealed class SystemUnlocks
    {
        private readonly HashSet<string> _unlocked = new(StringComparer.OrdinalIgnoreCase);

        /// <summary>Raised once per key, the moment it first unlocks. Arguments: the key and its cause (may be empty).</summary>
        public event Action<string, string>? Unlocked;

        public SystemUnlocks()
        {
            _unlocked.Add(KeyOf(GameSystem.Board));
        }

        /// <summary>Rungs that are open from the very first fight.</summary>
        public static readonly GameSystem[] OpenFromStart = { GameSystem.Board };

        public static string KeyOf(GameSystem system) => system.ToString();

        /// <summary>Key for a first-seen concept (for example <c>Seen("status", "bleed")</c> = <c>status:bleed</c>).</summary>
        public static string SeenKey(string kind, string name) =>
            $"{kind.Trim().ToLowerInvariant()}:{name.Trim().ToLowerInvariant()}";

        public bool IsUnlocked(GameSystem system) => _unlocked.Contains(KeyOf(system));

        public bool IsUnlocked(string key) => !string.IsNullOrWhiteSpace(key) && _unlocked.Contains(key.Trim());

        /// <summary>Every unlocked key, sorted (what the save writes).</summary>
        public IReadOnlyList<string> Keys => _unlocked.OrderBy(k => k, StringComparer.OrdinalIgnoreCase).ToList();

        /// <summary>Unlocks <paramref name="system"/>. Returns true only the first time.</summary>
        public bool Unlock(GameSystem system, string cause = "") => Unlock(KeyOf(system), cause);

        /// <summary>Unlocks a key (a <see cref="GameSystem"/> name or a seen key). Returns true only the first time.</summary>
        public bool Unlock(string key, string cause = "")
        {
            if (string.IsNullOrWhiteSpace(key))
                return false;
            if (!_unlocked.Add(key.Trim()))
                return false;
            Unlocked?.Invoke(key.Trim(), cause ?? "");
            return true;
        }

        /// <summary>Marks a concept as seen (see <see cref="SeenKey"/>). Returns true the first time.</summary>
        public bool MarkSeen(string kind, string name) => Unlock(SeenKey(kind, name), $"first {kind}");

        public bool HasSeen(string kind, string name) => IsUnlocked(SeenKey(kind, name));

        /// <summary>Replaces the record with saved keys (unknown keys are kept as they are). <see cref="GameSystem.Board"/> stays open.</summary>
        public void Restore(IEnumerable<string>? keys)
        {
            _unlocked.Clear();
            foreach (var system in OpenFromStart)
                _unlocked.Add(KeyOf(system));
            if (keys == null)
                return;
            foreach (string k in keys)
                if (!string.IsNullOrWhiteSpace(k))
                    _unlocked.Add(k.Trim());
        }

        // ---- First events (G1.5). Each returns true when it opened something new. ----

        /// <summary>An item reached the hero (a drop, a reward). Opens Items, and Synergies / LootDepth by rarity.</summary>
        public bool OnItemReceived(Item? item)
        {
            if (item == null)
                return false;
            bool any = Unlock(GameSystem.Items, $"first item: {item.Name}");
            int rank = RarityRank(item.Rarity);
            if (rank >= RarityRank("Uncommon"))
                any |= Unlock(GameSystem.Synergies, $"first {item.Rarity}: {item.Name}");
            if (rank >= RarityRank("Rare"))
                any |= Unlock(GameSystem.LootDepth, $"first {item.Rarity}: {item.Name}");
            return any;
        }

        /// <summary>The strip can hold <paramref name="slots"/> cards. Two or more opens Combos.</summary>
        public bool OnStripLengthChanged(int slots) =>
            slots >= 2 && Unlock(GameSystem.Combos, $"strip has {slots} slots");

        /// <summary>The worn gear changed. Two worn pieces of one material opens MaterialSets.</summary>
        public bool OnEquipmentChanged(IEnumerable<Item?> worn)
        {
            var best = worn
                .Where(i => i != null && !string.IsNullOrWhiteSpace(i.Material))
                .GroupBy(i => i!.Material.Trim(), StringComparer.OrdinalIgnoreCase)
                .OrderByDescending(g => g.Count())
                .FirstOrDefault();
            return best != null && best.Count() >= 2
                && Unlock(GameSystem.MaterialSets, $"two {best.Key} pieces worn");
        }

        /// <summary>The hero has an unspent skill point. Opens the skill tree.</summary>
        public bool OnSkillPointAvailable(int unspentPoints) =>
            unspentPoints > 0 && Unlock(GameSystem.SkillTree, "first skill point");

        /// <summary>A status landed in a fight (on either side). Opens Statuses and marks that status seen.</summary>
        public bool OnStatusApplied(string statusId)
        {
            if (string.IsNullOrWhiteSpace(statusId))
                return false;
            bool any = Unlock(GameSystem.Statuses, $"first status: {statusId}");
            any |= MarkSeen("status", statusId);
            return any;
        }

        /// <summary>A boss fight began.</summary>
        public bool OnBossEncountered(string bossName) => Unlock(GameSystem.Bosses, $"first boss: {bossName}");

        /// <summary>A store (currency) was minted for the first time.</summary>
        public bool OnStoreMinted(string store)
        {
            if (string.IsNullOrWhiteSpace(store))
                return false;
            bool any = Unlock(GameSystem.Stores, $"first currency: {store}");
            any |= MarkSeen("store", store);
            return any;
        }

        /// <summary>
        /// Builds the record for a save written before system unlocks existed: every rung the character's state shows
        /// they've already reached is open, so an old hero never loses a screen.
        /// </summary>
        public static SystemUnlocks InferFromCharacter(Character character)
        {
            var unlocks = new SystemUnlocks();
            if (character == null)
                return unlocks;

            var worn = new List<Item?>
            {
                character.Equipment.Head, character.Equipment.Body, character.Equipment.Legs,
                character.Equipment.Feet, character.Equipment.Weapon
            };
            var all = worn.Concat(character.Equipment.Inventory ?? new List<Item>()).Where(i => i != null).ToList();

            if (character.Level >= 2 || (character.Equipment.Inventory?.Count ?? 0) > 0)
                unlocks.Unlock(GameSystem.Items, "existing save");
            foreach (var item in all)
                unlocks.OnItemReceived(item);
            unlocks.OnEquipmentChanged(worn);

            int slots = 0;
            try { slots = ComboSequenceMaxHelper.GetEffectiveMax(character); } catch { /* data not loaded */ }
            unlocks.OnStripLengthChanged(slots);

            if (character.Level >= 2)
            {
                // A saved hero past level 1 has already seen the early rungs (they predate this record).
                unlocks.Unlock(GameSystem.ActionInteractions, "existing save");
                unlocks.Unlock(GameSystem.DungeonSelection, "existing save");
                unlocks.Unlock(GameSystem.Rooms, "existing save");
                unlocks.Unlock(GameSystem.Statuses, "existing save");
                unlocks.Unlock(GameSystem.DiceModifiers, "existing save");
            }
            if (character.Level >= 5)
            {
                unlocks.Unlock(GameSystem.Bosses, "existing save");
                unlocks.Unlock(GameSystem.Regions, "existing save");
            }
            if (character.Progression.LearnedSkillRanks.Count > 0 || character.Level >= 2)
                unlocks.Unlock(GameSystem.SkillTree, "existing save");
            if (character.Level >= 13)
                unlocks.Unlock(GameSystem.Stores, "existing save");
            return unlocks;
        }

        private static readonly string[] RarityOrder = { "Common", "Uncommon", "Rare", "Epic", "Legendary", "Mythic" };

        internal static int RarityRank(string? rarity)
        {
            if (string.IsNullOrWhiteSpace(rarity))
                return 0;
            int i = Array.FindIndex(RarityOrder, r => r.Equals(rarity.Trim(), StringComparison.OrdinalIgnoreCase));
            return i < 0 ? 0 : i;
        }
    }
}
