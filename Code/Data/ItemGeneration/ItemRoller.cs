using System;
using System.Collections.Generic;
using System.Linq;
using RPGGame.Combat.Events;

namespace RPGGame.Data.ItemGeneration
{
    /// <summary>
    /// Rolls one item (Documentation/ITEM_GENERATION_REDESIGN_2026-11-14.md):
    /// item level → slot + class → tier → base → rarity → material → line counts → lines → name.
    /// Each stage draws from its own random stream derived from one item seed, so adding a roll to one stage does not
    /// shift the others. Lines are stored as the usual <see cref="StatBonus"/> / <see cref="Modification"/> /
    /// <see cref="ActionBonus"/> objects (with <see cref="ItemModLineInfo"/>), so combat, saves and tooltips read them as before.
    /// </summary>
    public sealed class ItemRoller
    {
        private static readonly string[] ArmorSlots = { "head", "chest", "legs", "feet" };

        /// <summary>Tools only (DUNGEONSIM --material): every rolled item takes this material.</summary>
        public static string? ToolForcedMaterial { get; set; }

        /// <summary>Tools only (DUNGEONSIM --affix): force or strip one line on every rolled item.</summary>
        public static string? ToolForcedAffix { get; set; }

        private readonly LootDataCache _cache;
        private readonly ItemGenerationConfig _cfg;

        public ItemRoller(LootDataCache cache)
        {
            _cache = cache;
            _cfg = GameConfiguration.Instance?.LootSystem?.ItemGeneration ?? new ItemGenerationConfig();
            _cfg.EnsureSensibleDefaults();
        }

        /// <summary>Rolls an item; <paramref name="seedSource"/> supplies the item seed when the request has none.</summary>
        public Item? Roll(ItemRollRequest request, Random? seedSource = null) => Roll(request, seedSource, out _);

        public Item? Roll(ItemRollRequest request, Random? seedSource, out ItemRollSpec spec)
        {
            int seed = request.Seed ?? (seedSource ?? RandomUtility.Ambient).Next();
            spec = new ItemRollSpec { Seed = seed };
            var state = new RollState(this, request, spec, seed);
            return state.Run();
        }

        /// <summary>Deterministic per-stage stream: <c>Random(hash(itemSeed, stage))</c>.</summary>
        internal static Random Stream(int seed, string stage)
        {
            unchecked
            {
                uint h = 2166136261;
                foreach (char c in stage)
                {
                    h ^= c;
                    h *= 16777619;
                }
                h ^= (uint)seed;
                h *= 16777619;
                h ^= h >> 15;
                return new Random((int)(h & 0x7FFFFFFF));
            }
        }

        private static string Norm(string? s) =>
            (s ?? "").Trim().ToLowerInvariant().Replace("_", "").Replace(" ", "").Replace("-", "");

        /// <summary>The state of one roll; keeps the stage streams, the item and the influence tags together.</summary>
        private sealed class RollState
        {
            private readonly ItemRoller _r;
            private readonly ItemRollRequest _req;
            private readonly ItemRollSpec _spec;
            private readonly int _seed;
            private readonly HashSet<string> _influence = new(StringComparer.OrdinalIgnoreCase);
            private readonly HashSet<string> _usedFamilies = new(StringComparer.OrdinalIgnoreCase);
            private readonly HashSet<string> _usedStats = new(StringComparer.OrdinalIgnoreCase);
            private readonly double _rollUpScale;
            private Item _item = null!;
            private string _slot = "";
            private int _ilvl;
            /// <summary>Rarity used for line gates and values: the drop rarity, before any rarity roll-up.</summary>
            private int _rarityIndex;
            private string? _modifierTemplate;

            public RollState(ItemRoller roller, ItemRollRequest request, ItemRollSpec spec, int seed)
            {
                _r = roller;
                _req = request;
                _spec = spec;
                _seed = seed;
                double mf = Math.Clamp(request.MagicFind ?? request.Hero?.GetMagicFind() ?? 0, 0, 100);
                _rollUpScale = 1.0 + mf / 100.0 * Math.Max(0, _r._cfg.MagicFindRollUpScale);
            }

            private Random For(string stage) => Stream(_seed, stage);
            private ItemGenerationConfig Cfg => _r._cfg;
            private LootDataCache Cache => _r._cache;

            public Item? Run()
            {
                // 1. Item level = loot level (+ roll-up).
                int hero = Math.Max(1, _req.HeroLevel);
                int dungeon = Math.Max(1, _req.DungeonLevel);
                int lootLevel = Math.Clamp(_req.ItemLevel ?? dungeon - (hero - dungeon), 1, 100);
                _spec.LootLevel = lootLevel;
                _ilvl = lootLevel;
                if (_req.AllowRollUps && _req.ItemLevel == null)
                {
                    int steps = Cfg.ItemLevelRollUp.Roll(For("ilvl"), _rollUpScale);
                    if (steps > 0 && _ilvl < 100)
                    {
                        _ilvl = Math.Min(100, _ilvl + steps);
                        _spec.RollUps.Add($"ilvl+{steps}");
                    }
                }
                _spec.ItemLevel = _ilvl;

                var context = LootContext.Create(_req.Hero, _req.DungeonTheme);

                // 2–4. Slot, class, tier, base item.
                var baseRng = For("base");
                _slot = RollSlot(baseRng);
                string cls = RollClass(baseRng, context.HeroClassTags);
                int tier = _req.Tier ?? new LootTierCalculator(Cache, For("tier")).RollTier(_ilvl);
                if (!BuildBase(baseRng, cls, tier))
                    return null;
                _spec.Slot = _slot;

                // 5. Rarity. A roll-up gives the higher rarity's line count, suffixes and name; the lines still roll as
                // the drop rarity (so a rolled-up Common keeps the common-only high tiers).
                var rarity = RollRarity(out var dropRarity);
                _item.Rarity = rarity.Name;
                _spec.Rarity = rarity.Name;
                _rarityIndex = RarityOrder.IndexOf(dropRarity.Name);
                if (!string.Equals(dropRarity.Name, rarity.Name, StringComparison.OrdinalIgnoreCase))
                {
                    _item.DropRarity = dropRarity.Name;
                    _spec.DropRarity = dropRarity.Name;
                }

                // 6. Material (+ the piece's material trigger), then the base implicit.
                ApplyMaterial(For("material"), context);
                ApplyImplicit();
                BuildInfluence();

                // 7–8. Line count, then each line's pool by weight, then the lines (actions last).
                RollLines(PlanLineCount(rarity), rarity);

                string? forcedAffix = _req.ForcedAffix ?? ToolForcedAffix;
                if (!string.IsNullOrWhiteSpace(forcedAffix))
                    ApplyForcedAffix(forcedAffix!, For("forced"));

                // 9. Finish: tags, requirements, name.
                LootBonusApplier.SyncMaterialPrefixTags(_item);
                AnimalTagHelper.SyncSuffixTags(_item);
                _item.RecomputeAttributeRequirementsIncludingModifications();
                _item.RollUps = _spec.RollUps.Count > 0 ? new List<string>(_spec.RollUps) : null;
                CommonItemRules.MakePlain(_item); // a forced affix or tool path never sneaks a mechanic onto a Common
                _item.Name = ItemGenerator.GenerateItemNameWithBonuses(_item);
                return _item;
            }

            // ---------- 2. slot and class ----------

            private string RollSlot(Random rng)
            {
                if (!string.IsNullOrWhiteSpace(_req.Slot))
                    return _req.Slot!.Trim().ToLowerInvariant();
                if (_req.WeaponType != null || _req.Weapon == true)
                    return "weapon";
                var options = Cfg.SlotWeights
                    .Where(kv => kv.Value > 0 && (_req.Weapon != false || !string.Equals(kv.Key, "weapon", StringComparison.OrdinalIgnoreCase)))
                    .Select(kv => (Key: kv.Key.ToLowerInvariant(), kv.Value))
                    .ToList();
                if (options.Count == 0)
                    return _req.Weapon == false ? ArmorSlots[rng.Next(ArmorSlots.Length)] : "weapon";
                return PickWeighted(rng, options, o => o.Value).Key;
            }

            private string RollClass(Random rng, IReadOnlyList<string> heroClasses)
            {
                if (_req.WeaponType is WeaponType wt && ClassTags.ClassOf(wt) is string weaponClass)
                    return weaponClass;
                if (!string.IsNullOrWhiteSpace(_req.Class))
                    return _req.Class!.Trim().ToLowerInvariant();
                var own = heroClasses.Where(c => ClassTags.All.Contains(c)).ToList();
                if (own.Count == 0 || own.Count >= ClassTags.All.Length)
                    return ClassTags.All[rng.Next(ClassTags.All.Length)];
                if (rng.NextDouble() < Cfg.HeroClassShare)
                    return own[rng.Next(own.Count)];
                var others = ClassTags.All.Where(c => !own.Contains(c)).ToList();
                return others[rng.Next(others.Count)];
            }

            // ---------- 4. base item ----------

            private bool BuildBase(Random rng, string cls, int tier)
            {
                _spec.Class = cls;
                if (_slot == "weapon")
                {
                    var rows = Cache.WeaponData.Where(w => _req.WeaponType == null
                        || string.Equals(w.Type, _req.WeaponType.ToString(), StringComparison.OrdinalIgnoreCase)).ToList();
                    var row = PickBase(rng, rows, w => w.Tier, w => WeaponClass(w), w => w.Tags, cls, tier);
                    if (row == null)
                        return false;
                    _item = BuildWeapon(rng, row);
                    _spec.Class = WeaponClass(row);
                }
                else
                {
                    var rows = Cache.ArmorData.Where(a => string.Equals(a.Slot, _slot, StringComparison.OrdinalIgnoreCase)).ToList();
                    var row = PickBase(rng, rows, a => a.Tier, a => a.Class ?? "", a => a.Tags, cls, tier);
                    if (row == null)
                        return false;
                    _item = BuildArmor(rng, row);
                    _spec.Class = row.Class ?? cls;
                }
                _item.ItemClass = _spec.Class;
                _spec.Tier = _item.Tier;
                _spec.BaseName = _item.BaseName;
                return true;
            }

            private static string WeaponClass(WeaponData w) =>
                !string.IsNullOrWhiteSpace(w.Class) ? w.Class!.Trim().ToLowerInvariant()
                : (w.Tags ?? new List<string>()).Select(t => t.ToLowerInvariant()).FirstOrDefault(t => ClassTags.All.Contains(t))
                  ?? (Enum.TryParse<WeaponType>(w.Type, true, out var t2) ? ClassTags.ClassOf(t2) ?? "" : "");

            /// <summary>Slot × tier × class; loosens class, then tier (nearest first), recording what it loosened.</summary>
            private T? PickBase<T>(Random rng, List<T> rows, Func<T, int> tierOf, Func<T, string> classOf,
                Func<T, List<string>?> tagsOf, string cls, int tier) where T : class
            {
                if (_req.ExcludeStarter)
                {
                    var nonStarter = rows.Where(r => !(tagsOf(r) ?? new List<string>())
                        .Any(t => string.Equals(t, "starter", StringComparison.OrdinalIgnoreCase))).ToList();
                    if (nonStarter.Count > 0)
                        rows = nonStarter;
                }
                if (rows.Count == 0)
                    return null;

                var pool = rows.Where(r => tierOf(r) == tier && string.Equals(classOf(r), cls, StringComparison.OrdinalIgnoreCase)).ToList();
                if (pool.Count == 0)
                {
                    pool = rows.Where(r => tierOf(r) == tier).ToList();
                    if (pool.Count > 0)
                        _spec.Relaxed.Add("class");
                }
                if (pool.Count == 0)
                {
                    int nearest = rows.Select(tierOf).OrderBy(t => Math.Abs(t - tier)).ThenBy(t => t).First();
                    pool = rows.Where(r => tierOf(r) == nearest && string.Equals(classOf(r), cls, StringComparison.OrdinalIgnoreCase)).ToList();
                    if (pool.Count == 0)
                        pool = rows.Where(r => tierOf(r) == nearest).ToList();
                    _spec.Relaxed.Add("tier");
                }
                return pool[rng.Next(pool.Count)];
            }

            private WeaponItem BuildWeapon(Random rng, WeaponData row)
            {
                var type = Enum.TryParse<WeaponType>(row.Type, true, out var parsed) ? parsed : WeaponType.Sword;
                int lo = row.DamageMin, hi = row.DamageMax;
                if (lo <= 0 && hi <= 0)
                {
                    lo = row.BaseDamage + Math.Min(row.DamageBonusMin, row.DamageBonusMax);
                    hi = row.BaseDamage + Math.Max(row.DamageBonusMin, row.DamageBonusMax);
                }
                if (hi < lo)
                    (lo, hi) = (hi, lo);
                // Damage is the base's own range (its tier is already in it); item level does not scale it (user 2026-11-15).
                int damage = Math.Max(1, rng.Next(lo, hi + 1));
                // Higher tiers swing faster: swing time × (1 − per-tier cut × (tier − 1)).
                double speed = row.AttackSpeed * Math.Max(0.1, 1.0 - Cfg.WeaponSpeedPerTier * (Math.Max(1, row.Tier) - 1));

                var weapon = new WeaponItem(row.Name, row.Tier, damage, Math.Round(speed, 3), type)
                {
                    BaseName = row.Name,
                    Level = _ilvl,
                    ExtraActionSlots = RollRange(rng, row.ExtraActionSlots, row.ExtraActionSlotsMin, row.ExtraActionSlotsMax),
                    Tags = GameDataTagHelper.NormalizeDistinct(row.Tags)
                };
                _modifierTemplate = row.Modifier;
                if (row.AttributeRequirements is { Count: > 0 })
                {
                    weapon.AttributeRequirements = new AttributeRequirements(row.AttributeRequirements);
                    weapon.CatalogAttributeRequirements = new AttributeRequirements(row.AttributeRequirements);
                }
                return weapon;
            }

            private Item BuildArmor(Random rng, ArmorData row)
            {
                int lo = row.DefenseMin, hi = row.DefenseMax;
                if (lo <= 0 && hi <= 0)
                    lo = hi = row.Armor;
                if (hi < lo)
                    (lo, hi) = (hi, lo);
                int rolled = rng.Next(lo, hi + 1);
                var slotType = row.Slot.ToLowerInvariant() switch
                {
                    "head" => ItemType.Head,
                    "chest" => ItemType.Chest,
                    "legs" => ItemType.Legs,
                    _ => ItemType.Feet
                };
                int defense = GameConfiguration.Instance?.EquipmentScaling?.ScalePieceArmor(rolled, slotType, _ilvl, row.Tier) ?? rolled;

                Item item = slotType switch
                {
                    ItemType.Head => new HeadItem(row.Name, row.Tier, defense),
                    ItemType.Chest => new ChestItem(row.Name, row.Tier, defense),
                    ItemType.Legs => new LegsItem(row.Name, row.Tier, defense),
                    _ => new FeetItem(row.Name, row.Tier, defense)
                };
                double attr = 1.0 + Cfg.AttributePerItemLevel * (_ilvl - 1);
                int Scale(int v) => v == 0 ? 0 : (int)Math.Round(v * attr);
                item.BaseName = row.Name;
                item.Level = _ilvl;
                item.DefenseRolled = true;
                item.Tags = GameDataTagHelper.NormalizeDistinct(row.Tags);
                _modifierTemplate = row.Modifier;
                item.BaseStrength = Scale(row.Strength);
                item.BaseAgility = Scale(row.Agility);
                item.BaseTechnique = Scale(row.Technique);
                item.BaseIntelligence = Scale(row.Intelligence);
                item.BaseHit = row.Hit;
                item.BaseCombo = row.Combo;
                item.BaseCrit = row.Crit;
                item.CatalogAttackSpeed = row.AttackSpeed;
                item.MinGeneratedActionBonuses = row.MinActionBonuses;
                item.ExtraActionSlots = RollRange(rng, row.ExtraActionSlots, row.ExtraActionSlotsMin, row.ExtraActionSlotsMax);
                if (row.AttributeRequirements is { Count: > 0 })
                {
                    item.AttributeRequirements = new AttributeRequirements(row.AttributeRequirements);
                    item.CatalogAttributeRequirements = new AttributeRequirements(row.AttributeRequirements);
                }
                return item;
            }

            private static int RollRange(Random rng, int fixedValue, int min, int max)
            {
                min = Math.Max(0, min);
                max = Math.Max(0, max);
                if (max < min)
                    (min, max) = (max, min);
                return min > 0 || max > 0 ? rng.Next(min, max + 1) : Math.Max(0, fixedValue);
            }

            // ---------- 5. rarity ----------

            /// <summary>The item's rarity after roll-ups; <paramref name="dropRarity"/> is the one first rolled.</summary>
            private RarityData RollRarity(out RarityData dropRarity)
            {
                var rows = Cache.RarityData ?? new List<RarityData>();
                if (!string.IsNullOrWhiteSpace(_req.Rarity))
                {
                    dropRarity = rows.FirstOrDefault(r => string.Equals(r.Name, _req.Rarity!.Trim(), StringComparison.OrdinalIgnoreCase))
                        ?? new RarityData { Name = _req.Rarity!.Trim(), MinLines = 1, MaxLines = 2 };
                    return dropRarity;
                }

                var unlocked = rows.Where(r => r.MinLevel <= _ilvl && Weight(r) > 0).ToList();
                if (unlocked.Count == 0)
                {
                    dropRarity = rows.FirstOrDefault() ?? new RarityData { Name = "Common", MinLines = 1, MaxLines = 2 };
                    return dropRarity;
                }
                var rarity = PickWeighted(For("rarity"), unlocked, Weight);
                dropRarity = rarity;

                if (_req.AllowRollUps)
                {
                    int steps = Cfg.RarityRollUp.Roll(For("rarity-up"), _rollUpScale);
                    for (int i = 0; i < steps; i++)
                    {
                        int next = RarityOrder.IndexOf(rarity.Name) + 1;
                        var up = next < RarityOrder.Names.Length
                            ? rows.FirstOrDefault(r => string.Equals(r.Name, RarityOrder.Names[next], StringComparison.OrdinalIgnoreCase))
                            : null;
                        if (up == null || up.MinLevel > _ilvl)
                            break;
                        rarity = up;
                        _spec.RollUps.Add($"rarity>{up.Name}");
                    }
                }
                return rarity;
            }

            private static double Weight(RarityData r) =>
                GameConfiguration.Instance?.ItemAffixByRarity?.ResolveRarityWeight(r) ?? r.Weight;

            // ---------- 6. material, implicit, influence ----------

            private void ApplyMaterial(Random rng, LootContext context)
            {
                string material;
                bool weapon = _item is WeaponItem;
                double share = weapon ? Cfg.WeaponClassMaterialShare : Cfg.ArmorClassMaterialShare;
                if (rng.NextDouble() < share && ClassTags.WeaponOf(_spec.Class) is WeaponType classWeapon
                    && ItemMaterialRules.TryPickFamilyMaterial(classWeapon, rng, out string family))
                {
                    material = family;
                }
                else if (weapon)
                {
                    material = ItemMaterialRules.ClassMaterials[rng.Next(ItemMaterialRules.ClassMaterials.Length)];
                }
                else
                {
                    var materialMods = (Cache.Modifications ?? new List<Modification>())
                        .Where(m => m.GetPrefixCategory() == ModificationPrefixCategory.Material);
                    material = ItemMaterialRules.PickNonWeaponMaterial(materialMods, _item.Rarity, rng);
                    foreach (var kv in context.ClassMaterialChance)
                    {
                        if (kv.Value <= 0 || rng.NextDouble() >= kv.Value)
                            continue;
                        if (ClassTags.WeaponOf(kv.Key) is WeaponType wt && ItemMaterialRules.TryPickFamilyMaterial(wt, rng, out string marketPick))
                        {
                            material = marketPick;
                            break;
                        }
                    }
                }
                string? forced = _req.ForcedMaterial ?? ToolForcedMaterial;
                if (!string.IsNullOrWhiteSpace(forced))
                    material = forced!;

                new LootBonusApplier(Cache, rng).ApplySpecificMaterial(_item, material, _item.Rarity);
                _spec.Material = _item.Material;
                // Materials add no trigger to the piece (2026-11-15): the base's modifier is the item's built-in line.
            }

            /// <summary>
            /// The base's modifier: fixed to the base in the catalog (<c>modifier</c>), so finding a
            /// specific mechanic means finding that base again; only its values roll. Every base carries one, starters and
            /// Commons included (user 2026-11-15). Higher tiers are more complex (conditions, a second rule).
            /// </summary>
            private void ApplyImplicit()
            {
                _item.Implicit = "";
                if (CommonItemRules.MustBePlain(_item))
                    return; // Rebuild 2.1: a Common has no base modifier.
                var rolled = BaseModifierRoller.FromCatalog(_modifierTemplate, For("implicit"));
                if (rolled is not { } r)
                    return;
                _item.ImplicitRule = r.Rule;
                _spec.Implicit = r.Rule;
                _spec.ImplicitValue = r.Value;
                foreach (string token in r.Influence)
                    AddInfluence(token);
            }

            /// <summary>Class, slot, the slot's event family, material (name, store, synthesis event), implicit event.</summary>
            private void BuildInfluence()
            {
                AddInfluence(_spec.Class);
                AddInfluence(_slot);
                if (_item is WeaponItem w)
                    AddInfluence(w.WeaponType.ToString());
                if (ListenerAffix.SlotFamily(_item) is { } family)
                    foreach (var e in TriggerEvents.All.Where(e => e.Family == family && e.Listenable))
                        AddInfluence(e.Token);
                AddInfluence(_item.Material);
                var build = MaterialBuildsLoader.FindByMaterial(_item.Material);
                if (build != null)
                {
                    AddInfluence(build.Keyword);
                    AddInfluence(build.WhenToken);
                    AddInfluence(build.AssociatedClass);
                }
            }

            private void AddInfluence(string? token)
            {
                string n = Norm(token);
                if (n.Length > 0)
                    _influence.Add(n);
            }

            private double Tilt(IEnumerable<string>? tags)
            {
                if (tags == null)
                    return 1.0;
                int matches = tags.Count(t => _influence.Contains(Norm(t)));
                return Math.Min(Cfg.InfluenceCap, 1.0 + Cfg.InfluenceAffinity * matches);
            }

            // ---------- 7. line count ----------

            /// <summary>The rarity's <c>MinLines</c>, plus exploding extras up to <c>MaxLines</c>.</summary>
            private int PlanLineCount(RarityData rarity)
            {
                int total = Math.Max(0, rarity.MinLines);
                var rng = For("explode");
                int maxLines = Math.Max(total, rarity.MaxLines);
                while (total < maxLines && rng.NextDouble() < rarity.Explode)
                    total++;
                return total;
            }

            private double PoolTilt(string pool)
            {
                if (pool == ModPools.Action)
                    return 1.0;
                double best = 1.0;
                foreach (var f in EligibleFamilies(pool))
                    best = Math.Max(best, Tilt(f.Tags));
                return best;
            }

            // ---------- 8. lines ----------

            /// <summary>
            /// The guaranteed actions (and the base's own action minimum) take the first lines; every other line picks a
            /// pool by <see cref="ItemGenerationConfig.PoolWeights"/> × influence among the pools that still have something
            /// this item can roll, and rolls at once so it tilts the next pick. Action lines roll last.
            /// </summary>
            private void RollLines(int lineCount, RarityData rarity)
            {
                // Rebuild 2.1: a Common rolls stat lines only (actions start at Uncommon, D5.6). Its material is the
                // material prefix (outside the line count); the "material" pool is listeners, TUNE / STORECOND /
                // COUNTSDOUBLE and cross-wire rules, i.e. mechanics (D0.7), so a Common never rolls from it.
                bool plain = CommonItemRules.MustBePlain(_item);
                int actions = plain ? 0 : Math.Min(lineCount, Cfg.GuaranteedActionLines);
                var lineRng = For("lines");
                // Suffixes take lines out of the rarity's count, never on top of it (a 1-line Common is just its action).
                int suffixes = RollSuffixes(rarity, For("suffixes"), lineRng, lineCount - actions);
                int weighted = Math.Max(0, lineCount - actions - suffixes);
                var poolRng = For("pools");
                // A pool whose pick fails (e.g. a listener with no event for this slot) gets a few more tries, then closes.
                var fails = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
                bool Dead(string p) => fails.GetValueOrDefault(p) >= 3;

                for (int i = 0; i < weighted; i++)
                {
                    bool rolled = false;
                    while (!rolled)
                    {
                        var open = ModPools.All
                            .Where(p => !Dead(p) && Cfg.PoolWeights.GetValueOrDefault(p) > 0
                                && !(plain && p != ModPools.Stat)
                                && (p == ModPools.Action || EligibleFamilies(p).Count > 0))
                            .ToList();
                        if (open.Count == 0)
                            break;
                        string pool = PickWeighted(poolRng, open, p => Cfg.PoolWeights.GetValueOrDefault(p) * PoolTilt(p));
                        if (pool == ModPools.Action)
                        {
                            actions++;
                            rolled = true;
                        }
                        else if (RollModLine(pool, lineRng, pool))
                        {
                            rolled = true;
                        }
                        else
                        {
                            fails[pool] = fails.GetValueOrDefault(pool) + 1;
                        }
                    }
                }

                if (!plain)
                    actions = Math.Max(actions, _item.MinGeneratedActionBonuses);
                for (int i = 0; i < actions; i++)
                    if (!RollAction(lineRng, ModPools.Action))
                        break;

                foreach (string pool in ModPools.All)
                    _spec.PlannedLines[pool] = _spec.Lines.Count(l => l.Pool == pool);
            }

            /// <summary>Families a weighted (prefix) line can roll; suffixes roll by kind in <see cref="RollSuffixes"/>.</summary>
            private List<ItemModFamily> EligibleFamilies(string pool) =>
                Cache.ItemMods.InPool(pool).Where(f => string.IsNullOrWhiteSpace(f.SuffixKind) && FamilyAllowed(f) && EligibleTiers(f).Count > 0).ToList();

            /// <summary>
            /// Suffixes: at most one of each kind (common descriptor, uncommon animal, rare poetic phrase). The item's rarity
            /// sets the kinds it gets (<see cref="ItemGenerationConfig.SuffixKinds"/>), plus a poetic phrase at
            /// <see cref="ItemGenerationConfig.PoeticSuffixChance"/> when the rarity doesn't guarantee one. No other kinds join
            /// (user 2026-11-15). A kind with nothing this item can roll is skipped, and at most <paramref name="maxLines"/>
            /// roll, highest kind first. Returns the number of suffix lines rolled.
            /// </summary>
            private int RollSuffixes(RarityData rarity, Random kindRng, Random lineRng, int maxLines)
            {
                var kinds = new HashSet<string>(Cfg.SuffixKindsFor(rarity.Name), StringComparer.OrdinalIgnoreCase);
                // A small chance of a poetic phrase on items whose rarity doesn't guarantee one.
                if (!kinds.Contains(SuffixKinds.Rare) && kindRng.NextDouble() < Cfg.PoeticSuffixChance)
                    kinds.Add(SuffixKinds.Rare);
                // Poetic (rare) suffixes are the mechanic families: never on a plain Common (D0.7).
                if (CommonItemRules.MustBePlain(_item))
                    kinds.Remove(SuffixKinds.Rare);

                // Over the line budget: keep the highest kinds (a Rare's poetic phrase before its descriptor).
                if (kinds.Count > Math.Max(0, maxLines))
                    kinds = new HashSet<string>(kinds.OrderByDescending(SuffixKinds.Rank).Take(Math.Max(0, maxLines)),
                        StringComparer.OrdinalIgnoreCase);

                int rolled = 0;
                foreach (string kind in SuffixKinds.All)
                {
                    if (!kinds.Contains(kind))
                        continue;
                    var families = Cache.ItemMods.Families
                        .Where(f => string.Equals(f.SuffixKind, kind, StringComparison.OrdinalIgnoreCase) && FamilyAllowed(f) && EligibleTiers(f).Count > 0)
                        .ToList();
                    for (int attempt = 0; attempt < 3 && families.Count > 0; attempt++)
                    {
                        var family = PickWeighted(lineRng, families, f => Math.Max(0, f.Weight) * Tilt(f.Tags));
                        if (RollModLine(family.Pool, lineRng, family.Pool, chosenFamily: family))
                        {
                            rolled++;
                            break;
                        }
                        families.Remove(family);
                    }
                }
                return rolled;
            }

            private bool FamilyAllowed(ItemModFamily f)
            {
                if (_usedFamilies.Contains(f.Family) || !f.AllowsSlot(_slot))
                    return false;
                switch (f.Kind?.ToLowerInvariant())
                {
                    case "mod":
                        var template = FindModTemplate(f.Template);
                        if (template == null)
                            return false;
                        if (ListenerAffix.IsListener(template) && ListenerAffix.SlotFamily(_item) == null)
                            return false;
                        // One Quality line per item (it multiplies the base).
                        if (template.GetPrefixCategory() == ModificationPrefixCategory.Quality
                            && _item.Modifications.Any(m => m.GetPrefixCategory() == ModificationPrefixCategory.Quality))
                            return false;
                        return true;
                    case "suffix":
                        return FindStatTemplate(f.Template) != null;
                    case "stat":
                        // One line per body stat: "of Might" and "of the Bear" never share an item.
                        return !string.IsNullOrWhiteSpace(f.StatType) && !_usedStats.Contains(f.StatType!);
                    case "slots":
                        return true;
                    case "hybrid":
                        // Every part must exist and still be free (a hybrid and its own single stat never share an item).
                        return f.Parts is { Count: > 0 }
                            && f.Parts.All(p => Cache.ItemMods.Find(p) is { } part
                                && !string.Equals(part.Kind, "hybrid", StringComparison.OrdinalIgnoreCase)
                                && FamilyAllowed(part));
                    default:
                        return false;
                }
            }

            /// <summary>Indices of tiers this item can roll: item level, slot and rarity gates.</summary>
            private List<int> EligibleTiers(ItemModFamily f)
            {
                var list = new List<int>();
                for (int i = 0; i < f.Tiers.Count; i++)
                {
                    var t = f.Tiers[i];
                    if (t.LevelFor(_slot) <= _ilvl && t.AllowsSlot(_slot) && t.AllowsRarity(_rarityIndex))
                        list.Add(i);
                }
                return list;
            }

            private bool RollModLine(string pool, Random rng, string plannedPool, ItemModFamily? forcedFamily = null, ItemModFamily? chosenFamily = null)
            {
                ItemModFamily family;
                if (forcedFamily != null)
                {
                    family = forcedFamily;
                }
                else if (chosenFamily != null)
                {
                    family = chosenFamily;
                }
                else
                {
                    var families = EligibleFamilies(pool);
                    if (families.Count == 0)
                        return false;
                    family = PickWeighted(rng, families, f => Math.Max(0, f.Weight) * Tilt(f.Tags));
                }

                var tiers = EligibleTiers(family);
                if (tiers.Count == 0)
                {
                    if (forcedFamily == null || family.Tiers.Count == 0)
                        return false;
                    tiers.Add(0);
                }
                int tierIndex = PickWeighted(rng, tiers, i => Math.Max(0, family.Tiers[i].Weight));

                // Tier roll-up: the next stronger tier this slot and rarity allow, ignoring its item level.
                bool rolledUp = false;
                if (_req.AllowRollUps && forcedFamily == null)
                {
                    int steps = Cfg.TierRollUp.Roll(rng, _rollUpScale);
                    if (steps > 0)
                    {
                        var ladder = Enumerable.Range(0, family.Tiers.Count)
                            .Where(i => family.Tiers[i].AllowsSlot(_slot) && family.Tiers[i].AllowsRarity(_rarityIndex))
                            .OrderBy(i => family.Tiers[i].Max).ThenBy(i => family.Tiers[i].LevelFor(_slot))
                            .ToList();
                        int at = ladder.IndexOf(tierIndex);
                        int target = Math.Min(ladder.Count - 1, at + steps);
                        if (at >= 0 && target > at)
                        {
                            tierIndex = ladder[target];
                            rolledUp = true;
                            _spec.RollUps.Add($"tier:{family.Family}+{target - at}");
                        }
                    }
                }

                var tier = family.Tiers[tierIndex];
                double value = tier.Min + rng.NextDouble() * Math.Max(0, tier.Max - tier.Min);
                value = family.Integer ? Math.Round(value) : Math.Round(value, 2);

                var info = new ItemModLineInfo
                {
                    Pool = pool,
                    Family = family.Family,
                    Tier = tierIndex + 1,
                    Affix = family.Affix ?? "",
                    Keyword = tier.Keyword,
                    RolledUp = rolledUp,
                    SuffixKind = string.IsNullOrWhiteSpace(family.SuffixKind) ? null : family.SuffixKind!.Trim().ToLowerInvariant()
                };
                bool materialized = string.Equals(family.Kind, "hybrid", StringComparison.OrdinalIgnoreCase)
                    ? MaterializeHybrid(family, tierIndex, tier, info, rng, out value)
                    : Materialize(family, tier, value, info, rng);
                if (!materialized)
                    return false;

                _usedFamilies.Add(family.Family);
                AddInfluence(family.Family);
                foreach (var tag in family.Tags ?? new List<string>())
                    AddInfluence(tag);
                _spec.Lines.Add(new ItemRollLine
                {
                    Pool = pool,
                    Family = family.Family,
                    Tier = tierIndex + 1,
                    Keyword = tier.Keyword,
                    Value = value,
                    RolledUp = rolledUp,
                    From = plannedPool != pool ? plannedPool : null
                });
                return true;
            }

            private bool Materialize(ItemModFamily f, ItemModTier tier, double value, ItemModLineInfo info, Random rng)
            {
                switch (f.Kind?.ToLowerInvariant())
                {
                    case "stat":
                        if (!string.IsNullOrWhiteSpace(f.StatType))
                            _usedStats.Add(f.StatType!);
                        _item.StatBonuses.Add(new StatBonus
                        {
                            Name = tier.Keyword,
                            Description = (f.Description ?? "+{n} " + f.StatType).Replace("{n}", value.ToString("0.##")),
                            Value = value,
                            StatType = f.StatType ?? "",
                            Rarity = _item.Rarity,
                            ModLine = info
                        });
                        return true;

                    case "suffix":
                        var row = FindStatTemplate(f.Template);
                        if (row == null)
                            return false;
                        var suffix = row.CloneForItemInstance();
                        suffix.Name = tier.Keyword;
                        suffix.ModLine = info;
                        _item.StatBonuses.Add(suffix);
                        return true;

                    case "mod":
                        var template = FindModTemplate(f.Template);
                        if (template == null)
                            return false;
                        var mod = CloneModification(template);
                        mod.Name = tier.Keyword;
                        mod.MinValue = tier.Min;
                        mod.MaxValue = tier.Max;
                        mod.RolledValue = value;
                        mod.ModLine = info;
                        if (ListenerAffix.IsListener(mod) && !ListenerAffix.RollEvent(mod, _item, rng))
                            return false;
                        if (AffixTemplates.Is(mod, CrossWires.RuleEffect) && CrossWires.DescribeMod(mod) is { } ruleText)
                            mod.Description = ruleText;
                        _item.Modifications.Add(mod);
                        return true;

                    case "slots":
                        int slots = Math.Max(1, (int)Math.Round(value));
                        _item.ExtraActionSlots += slots;
                        _item.Modifications.Add(new Modification
                        {
                            Name = tier.Keyword,
                            PrefixCategory = "ADJECTIVE",
                            Effect = "EXTRASLOTS",
                            Description = $"+{slots} combo strip slot{(slots == 1 ? "" : "s")}",
                            MinValue = tier.Min,
                            MaxValue = tier.Max,
                            RolledValue = slots,
                            ModLine = info
                        });
                        return true;
                }
                return false;
            }

            /// <summary>
            /// A hybrid keyword ("of the Griffin", "Savage") rolls each of its parts at the same tier index, at
            /// <see cref="ItemGenerationConfig.HybridPartShare"/> of that part's range. Each part becomes its own line object
            /// (so combat and saves read it as usual) carrying the hybrid's family and keyword; the parts count as used.
            /// </summary>
            private bool MaterializeHybrid(ItemModFamily f, int tierIndex, ItemModTier tier, ItemModLineInfo info, Random rng, out double firstValue)
            {
                firstValue = 0;
                double share = Math.Clamp(Cfg.HybridPartShare, 0.05, 1.0);
                var parts = (f.Parts ?? new List<string>()).Select(p => Cache.ItemMods.Find(p)).ToList();
                if (parts.Count == 0 || parts.Any(p => p == null || p.Tiers.Count == 0))
                    return false;
                for (int i = 0; i < parts.Count; i++)
                {
                    var part = parts[i]!;
                    var pt = part.Tiers[Math.Min(tierIndex, part.Tiers.Count - 1)];
                    double lo = pt.Min * share, hi = pt.Max * share;
                    double v = lo + rng.NextDouble() * Math.Max(0, hi - lo);
                    v = part.Integer ? Math.Max(1, Math.Round(v)) : Math.Round(v, 2);
                    var partTier = new ItemModTier { Keyword = tier.Keyword, Min = lo, Max = hi, Level = tier.Level, Weight = tier.Weight, MaxRarity = tier.MaxRarity };
                    var partInfo = new ItemModLineInfo
                    {
                        Pool = info.Pool, Family = info.Family, Tier = info.Tier, Affix = info.Affix, Keyword = info.Keyword, RolledUp = info.RolledUp,
                        SuffixKind = info.SuffixKind
                    };
                    if (!Materialize(part, partTier, v, partInfo, rng))
                        return false;
                    if (i == 0)
                        firstValue = v;
                    _usedFamilies.Add(part.Family);
                }
                return true;
            }

            private bool RollAction(Random rng, string plannedPool)
            {
                var have = new HashSet<string>(_item.ActionBonuses.Select(a => a.Name), StringComparer.OrdinalIgnoreCase);
                var candidates = (Cache.ActionBonuses ?? new List<ActionBonus>())
                    .Where(a => !string.IsNullOrEmpty(a.Name) && !have.Contains(a.Name))
                    .Select(a => (Bonus: a, Data: ActionLoader.GetActionData(a.Name)))
                    .Where(x => GearActionPools.CanRollOnGear(x.Data))
                    .ToList();
                if (candidates.Count == 0)
                    return false;

                // Weapons lean to their own class's actions; every other slot draws from the whole pool.
                if (_item is WeaponItem weapon)
                {
                    var classPool = candidates.Where(x => GearActionPools.IsClassAction(x.Data?.Tags, weapon.WeaponType)).ToList();
                    var rest = candidates.Except(classPool).ToList();
                    bool useClass = classPool.Count > 0 && (rest.Count == 0 || rng.NextDouble() < Cfg.WeaponActionClassShare);
                    candidates = useClass ? classPool : rest;
                }

                var pick = PickWeighted(rng, candidates, x => Math.Max(1, x.Bonus.Weight) * Tilt(x.Data?.Tags));
                _item.ActionBonuses.Add(new ActionBonus { Name = pick.Bonus.Name, Description = pick.Bonus.Description, Weight = pick.Bonus.Weight });
                foreach (var tag in pick.Data?.Tags ?? new List<string>())
                    AddInfluence(tag);
                _spec.Lines.Add(new ItemRollLine
                {
                    Pool = ModPools.Action,
                    Family = pick.Bonus.Name,
                    Keyword = pick.Bonus.Name,
                    From = plannedPool != ModPools.Action ? plannedPool : null
                });
                return true;
            }

            // ---------- tools ----------

            /// <summary>mod:Name forces that family (replacing one line of its pool); mod:- strips stat prefixes; stat:TYPE
            /// adds that stat family; stat:- strips stat suffixes; stat:-TYPE strips them and adds TYPE.</summary>
            private void ApplyForcedAffix(string forced, Random rng)
            {
                int colon = forced.IndexOf(':');
                if (colon <= 0)
                    return;
                string kind = forced[..colon].Trim().ToLowerInvariant();
                string key = forced[(colon + 1)..].Trim();

                if (kind == "mod" && key == "-")
                {
                    _item.Modifications.RemoveAll(m => m.ModLine is { Pool: ModPools.Stat, Affix: "prefix" } && m.GetPrefixCategory() != ModificationPrefixCategory.Quality);
                    return;
                }
                if (kind == "mod")
                {
                    var family = Cache.ItemMods.Find(key)
                        ?? Cache.ItemMods.Families.FirstOrDefault(f => string.Equals(f.Template, key, StringComparison.OrdinalIgnoreCase));
                    if (family == null || _usedFamilies.Contains(family.Family))
                        return;
                    var replace = _item.Modifications.LastOrDefault(m => m.ModLine != null && m.ModLine.Pool == family.Pool
                        && m.GetPrefixCategory() != ModificationPrefixCategory.Quality);
                    if (replace != null)
                    {
                        _item.Modifications.Remove(replace);
                        _usedFamilies.Remove(replace.ModLine!.Family);
                    }
                    RollModLine(family.Pool, rng, family.Pool, family);
                    return;
                }
                if (kind == "stat")
                {
                    if (key.StartsWith('-'))
                    {
                        _item.StatBonuses.RemoveAll(s => s.ModLine is { Pool: ModPools.Stat });
                        key = key[1..].Trim();
                        if (key.Length == 0)
                            return;
                    }
                    var family = Cache.ItemMods.Families.FirstOrDefault(f => string.Equals(f.Kind, "stat", StringComparison.OrdinalIgnoreCase)
                        && string.Equals(f.StatType, key, StringComparison.OrdinalIgnoreCase));
                    if (family == null || _item.StatBonuses.Any(s => string.Equals(s.StatType, key, StringComparison.OrdinalIgnoreCase)))
                        return;
                    _usedFamilies.Remove(family.Family);
                    RollModLine(family.Pool, rng, family.Pool, family);
                }
            }

            // ---------- helpers ----------

            private Modification? FindModTemplate(string? name) =>
                string.IsNullOrWhiteSpace(name) ? null
                : (Cache.Modifications ?? new List<Modification>()).FirstOrDefault(m => string.Equals(m.Name?.Trim(), name.Trim(), StringComparison.OrdinalIgnoreCase));

            private StatBonus? FindStatTemplate(string? name) =>
                string.IsNullOrWhiteSpace(name) ? null
                : (Cache.StatBonuses ?? new List<StatBonus>()).FirstOrDefault(s => string.Equals(s.Name?.Trim(), name.Trim(), StringComparison.OrdinalIgnoreCase));

            private static Modification CloneModification(Modification t)
            {
                var clone = new Modification
                {
                    DiceResult = t.DiceResult,
                    ItemRank = t.ItemRank,
                    Weight = t.Weight,
                    PrefixCategory = t.PrefixCategory,
                    Name = t.Name,
                    Description = t.Description,
                    Effect = t.Effect,
                    TriggerWhen = t.TriggerWhen,
                    CrossWire = t.CrossWire,
                    MinValue = t.MinValue,
                    MaxValue = t.MaxValue,
                    RolledValue = t.RolledValue,
                    StatusEffects = t.StatusEffects != null ? new List<string>(t.StatusEffects) : new List<string>(),
                    Tags = t.Tags != null ? new List<string>(t.Tags) : null
                };
                if (t.AttributeRequirements != null && t.AttributeRequirements.Count > 0)
                    clone.AttributeRequirements = new AttributeRequirements(new Dictionary<string, int>(t.AttributeRequirements));
                return clone;
            }

            private static T PickWeighted<T>(Random rng, IReadOnlyList<T> options, Func<T, double> weight)
            {
                double total = 0;
                foreach (var o in options)
                    total += Math.Max(0, weight(o));
                if (total <= 0)
                    return options[rng.Next(options.Count)];
                double roll = rng.NextDouble() * total;
                foreach (var o in options)
                {
                    roll -= Math.Max(0, weight(o));
                    if (roll < 0)
                        return o;
                }
                return options[options.Count - 1];
            }
        }
    }
}
