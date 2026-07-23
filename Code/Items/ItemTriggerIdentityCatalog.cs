using System;
using System.Collections.Generic;
using System.Linq;
using RPGGame.Data;

namespace RPGGame
{
    /// <summary>
    /// Facade over <see cref="TriggersLoader"/> for item trigger identities.
    /// Seed rows (when <c>Triggers.json</c> is missing) live in <see cref="BuildSeedRows"/>.
    /// </summary>
    public static class ItemTriggerIdentityCatalog
    {
        public sealed record Identity(
            int Index,
            string Name,
            string When,
            string Scope,
            string Mechanics,
            double? Value = null,
            IReadOnlyList<string>? Filters = null,
            bool IsEquipEffect = false);

        public static int Count => Math.Max(1, TriggersLoader.Count);

        public static IReadOnlyList<Identity> Identities =>
            TriggersLoader.GetAll().Select(ToIdentity).ToList();

        public static Identity Get(int index) => ToIdentity(TriggersLoader.GetByIndex(index));

        public static ActionTriggerBundle ToBundle(Identity identity)
        {
            return new ActionTriggerBundle
            {
                When = identity.When,
                Count = "1",
                Scope = identity.Scope ?? "",
                Mechanics = identity.Mechanics,
                Value = identity.Value,
                Filters = identity.Filters == null || identity.Filters.Count == 0
                    ? null
                    : identity.Filters.ToList()
            };
        }

        public static ActionTriggerBundle BundleForCatalogIndex(int catalogIndex) =>
            ToBundle(Get(catalogIndex));

        public static bool IsKnownMechanic(string? mechanicsCell)
        {
            if (string.IsNullOrWhiteSpace(mechanicsCell))
                return false;
            string id = mechanicsCell.Trim();
            int colon = id.IndexOf(':');
            if (colon > 0)
                id = id.Substring(0, colon);
            id = ActionMechanicsRegistry.NormalizeMechanicId(id);
            return TriggersLoader.GetAll().Any(x =>
            {
                string m = x.Mechanics ?? "";
                int c = m.IndexOf(':');
                string mid = c > 0 ? m.Substring(0, c) : m;
                return string.Equals(
                    ActionMechanicsRegistry.NormalizeMechanicId(mid),
                    id,
                    StringComparison.OrdinalIgnoreCase);
            });
        }

        public static Identity ToIdentity(TriggerIdentityData row)
        {
            var filters = row.ParseFilters();
            return new Identity(
                row.Id,
                row.Name ?? "",
                row.When ?? "",
                row.Scope ?? "",
                row.Mechanics ?? "",
                row.Value,
                filters.Count == 0 ? null : filters,
                row.IsEquipEffect);
        }

        /// <summary>Built-in seed used when <c>Triggers.json</c> is absent and for first-time export.</summary>
        public static List<TriggerIdentityData> BuildSeedRows()
        {
            return BuildSeedIdentities()
                .Select(id => new TriggerIdentityData
                {
                    Id = id.Index,
                    Name = id.Name,
                    When = id.When,
                    Count = "1",
                    Scope = id.Scope ?? "",
                    Mechanics = id.Mechanics,
                    Value = id.Value,
                    Filters = id.Filters == null || id.Filters.Count == 0
                        ? null
                        : string.Join(",", id.Filters),
                    Channel = id.IsEquipEffect ? "equip" : "combat"
                })
                .ToList();
        }

        private static Identity[] BuildSeedIdentities()
        {
            // 0–54: original 55 identities
            // 55–65: affordance demos for the 11 example designs
            return new[]
            {
                new Identity(0, "WoundMomentum", "ONCONNECT", "ACTION", "hero_next_action_damage", 10),
                new Identity(1, "RecoverFooting", "ONAFTERMISS", "ACTION", "hero_next_action_speed", 10),
                new Identity(2, "ComboSpray", "ONCOMBO", "ACTION", "hero_next_action_multihit", 1),
                new Identity(3, "CritAmpCarry", "ONCRITICAL", "ACTION", "hero_next_action_amp", 10),
                new Identity(4, "FoeOvercommit", "ONMISS", "ACTION", "enemy_next_action_damage", 10),
                new Identity(5, "EndBreathDamage", "ONCOMBOEND", "ACTION", "hero_next_action_damage", 10),
                new Identity(6, "FirstBloodAmp", "ONFIRSTHIT", "ACTION", "hero_next_action_amp", 10),
                new Identity(7, "KillSpray", "ONKILL", "ACTION", "hero_next_action_multihit", 1),

                new Identity(8, "DesperationAim", "ONAFTERMISS", "TURN", "hero_hit_threshold", -1),
                new Identity(9, "OpenTheLane", "ONFIRSTHIT", "TURN", "hero_combo_threshold", -1),
                new Identity(10, "BloodlustCrit", "ONKILL", "TURN", "hero_crit_threshold", -1),
                new Identity(11, "SteadyHand", "ONCRITICALMISS", "TURN", "hero_crit_miss_threshold", 1),
                new Identity(12, "LockedInAcc", "ONCONNECT", "TURN", "hero_accuracy", 1),
                new Identity(13, "EnemyFlinch", "ONCOMBOEND", "TURN", "enemy_hit_threshold", 1),
                new Identity(14, "EnemyAccTax", "ONMISS", "TURN", "enemy_accuracy", 1),
                new Identity(15, "CritThresholdPush", "ONCRITICAL", "TURN", "hero_crit_threshold", -1),
                new Identity(16, "ComboThresholdPush", "ONCOMBO", "TURN", "hero_combo_threshold", -1),
                new Identity(17, "LuckySevenHit", "ONNATURALROLL:7", "TURN", "hero_hit_threshold", -1),
                new Identity(18, "ExactFifteenCombo", "ONROLLVALUE:15", "TURN", "hero_combo_threshold", -1),
                new Identity(19, "ClutchForgive", "ONCONNECT", "TURN", "hero_hit_threshold", -1,
                    new[] { "IFCLUTCH" }),

                new Identity(20, "CritFace19", "ONCONNECT", "TURN", "crit_face_min:19", 19),
                new Identity(21, "CritFace18", "ONFIRSTHIT", "TURN", "crit_face_min:18", 18),
                new Identity(22, "SalvageCharm", "ONCONNECT", "FIGHT", "salvage_miss", 1),
                new Identity(23, "KillReload20", "ONKILL", "", "replace_next_roll:20", 20),
                new Identity(24, "ComboEndReload15", "ONCOMBOEND", "", "replace_next_roll:15", 15),

                new Identity(25, "SharpenedEdge", "ONCOMBO", "TURN", "hero_weapon_damage", 1),
                new Identity(26, "QuickGrip", "ONCONNECT", "TURN", "hero_weapon_speed", 1),
                new Identity(27, "MarchCadence", "ONROOMSCLEARED", "FIGHT", "hero_weapon_damage", 1),
                new Identity(28, "EnemySlowWeapon", "ONMISS", "TURN", "enemy_weapon_speed", 1),

                new Identity(29, "ChaosOpen", "ONFIRSTHIT", "", "strip_shuffle"),
                new Identity(30, "EchoStep", "ONCOMBO", "", "strip_repeat"),
                new Identity(31, "SkipFiller", "ONCONNECT", "", "strip_skip"),
                new Identity(32, "LeapAhead", "ONCONNECT", "", "strip_jump"),
                new Identity(33, "LoopChain", "ONCONNECT", "", "strip_loop"),
                new Identity(34, "BreakRhythm", "ONMISS", "", "strip_stop"),
                new Identity(35, "WildNext", "ONCONNECT", "", "strip_random"),
                new Identity(36, "SealSlot", "ONCONNECT", "", "strip_disable"),
                new Identity(37, "ReplaceNextStrip", "ONCRITICAL", "", "strip_replace_next"),

                new Identity(38, "DoubleBeat", "ONCOMBO", "", "retrigger_next"),
                new Identity(39, "ResetOpener", "ONKILL", "", "retrigger_opener"),
                new Identity(40, "FinisherEncore", "ONCOMBOEND", "", "retrigger_finisher"),
                new Identity(41, "SlotTwoEncore", "ONCRITICAL", "", "retrigger_slot:2"),

                new Identity(42, "CritExpose", "ONCRITICAL", "", "expose"),
                new Identity(43, "ComboWeaken", "ONCOMBO", "", "weaken"),
                new Identity(44, "SteadyFocus", "ONCONNECT", "", "focus"),
                new Identity(45, "GuardUp", "ONMISS", "", "harden"),
                new Identity(46, "FirstFortify", "ONFIRSTHIT", "", "fortify"),
                new Identity(47, "ConnectPierce", "ONCONNECT", "", "pierce"),
                new Identity(48, "EndVulnerability", "ONCOMBOEND", "", "vulnerability"),
                new Identity(49, "CritMissSlow", "ONCRITICALMISS", "", "slow"),
                new Identity(50, "PunishDot", "ONCONNECT", "", "pierce",
                    Filters: new[] { "IFTARGETUNDERDOT" }),
                new Identity(51, "SpiteFocus", "ONCONNECT", "", "focus",
                    Filters: new[] { "IFSOURCEUNDERDOT" }),

                new Identity(52, "ComboEndHeal", "ONCOMBOEND", "", "heal", 2),
                new Identity(53, "RoomClearHeal", "ONROOMSCLEARED", "", "heal", 2),
                new Identity(54, "AfterMissHeal", "ONAFTERMISS", "", "heal", 2),

                new Identity(55, "CritLadderDungeon", "ONCRITICAL", "DUNGEON", "hero_crit_threshold", -1),
                new Identity(56, "HitLadderDungeon", "ONCONNECT", "DUNGEON", "hero_hit_threshold", -1),
                new Identity(57, "MissHitTaxDungeon", "ONMISS", "DUNGEON", "hero_hit_threshold", 1),
                new Identity(58, "GoldSetArmor", "WHILE_EQUIPPED", "", "armor", 2,
                    Filters: new[] { "IFGEARHASTAG:gold" }, IsEquipEffect: true),
                new Identity(59, "ClassPrimaryAttr", "WHILE_EQUIPPED", "", "hero_stat_bonus:PRIMARY", 1,
                    Filters: new[] { "IFCLASSTAG:barbarian" }, IsEquipEffect: true),
                new Identity(60, "EvenRollHeal", "ONEVEN", "", "heal", 1),
                new Identity(61, "BarbarianActionDamage", "ONCONNECT", "ACTION", "hero_next_action_damage", 5,
                    Filters: new[] { "IFACTIONHASTAG:barbarian" }),
                new Identity(62, "GrantBarbarianTag", "WHILE_EQUIPPED", "", "grant_action_tag:barbarian",
                    IsEquipEffect: true),
                new Identity(63, "SlotThreeSpeedDungeon", "ONCONNECT", "DUNGEON", "hero_next_action_speed", 5,
                    Filters: new[] { "IFSLOT:3" }),
                new Identity(64, "MirrorSwingDamage", "ONCONNECT", "", "hero_action_damage", 100,
                    Filters: new[] { "IFSAMESACTION" }),
                new Identity(65, "UnarmedPunchGrant", "WHILE_EQUIPPED", "", "grant_action:PUNCH HARD",
                    Filters: new[] { "IFUNARMED" }, IsEquipEffect: true),
            };
        }
    }
}
