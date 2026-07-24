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
            bool IsEquipEffect = false,
            string? ScaleFrom = null,
            string Description = "");

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
                    : identity.Filters.ToList(),
                ScaleFrom = string.IsNullOrWhiteSpace(identity.ScaleFrom) ? null : identity.ScaleFrom
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
                row.IsEquipEffect,
                string.IsNullOrWhiteSpace(row.ScaleFrom) ? null : row.ScaleFrom.Trim(),
                row.Description ?? "");
        }

        /// <summary>Built-in seed used when <c>Triggers.json</c> is absent and for first-time export.</summary>
        public static List<TriggerIdentityData> BuildSeedRows()
        {
            return BuildSeedIdentities()
                .Select(id => new TriggerIdentityData
                {
                    Id = id.Index,
                    Name = id.Name,
                    Description = id.Description ?? "",
                    When = id.When,
                    Count = "1",
                    Scope = id.Scope ?? "",
                    Mechanics = id.Mechanics,
                    Value = id.Value,
                    Filters = id.Filters == null || id.Filters.Count == 0
                        ? null
                        : string.Join(",", id.Filters),
                    Channel = id.IsEquipEffect ? "equip" : "combat",
                    ScaleFrom = string.IsNullOrWhiteSpace(id.ScaleFrom) ? null : id.ScaleFrom
                })
                .ToList();
        }

        private static Identity[] BuildSeedIdentities()
        {
            // 0–54: original 55 identities
            // 55–65: affordance demos for the 11 example designs
            // 66–105: Wave 2 — tag amps, scaleFrom, material/set, combat hybrids
            return new[]
            {
                new Identity(0, "WoundMomentum", "ONCONNECT", "ACTION", "hero_next_action_damage", 10,
                    Description: "On connect, your next action deals +10% damage."),
                new Identity(1, "RecoverFooting", "ONAFTERMISS", "ACTION", "hero_next_action_speed", 10,
                    Description: "After a miss, your next action is +10% faster."),
                new Identity(2, "ComboSpray", "ONCOMBO", "ACTION", "hero_next_action_multihit", 1,
                    Description: "On combo, your next action gains +1 multihit."),
                new Identity(3, "CritAmpCarry", "ONCRITICAL", "ACTION", "hero_next_action_amp", 10,
                    Description: "On crit, your next action gains +10% amp."),
                new Identity(4, "FoeOvercommit", "ONMISS", "ACTION", "enemy_next_action_damage", 10,
                    Description: "On miss, the foe's next action deals +10% damage (overcommit)."),
                new Identity(5, "EndBreathDamage", "ONCOMBOEND", "ACTION", "hero_next_action_damage", 10,
                    Description: "On combo end, your next action deals +10% damage."),
                new Identity(6, "FirstBloodAmp", "ONFIRSTHIT", "ACTION", "hero_next_action_amp", 10,
                    Description: "On first hit of the fight, your next action gains +10% amp."),
                new Identity(7, "KillSpray", "ONKILL", "ACTION", "hero_next_action_multihit", 1,
                    Description: "On kill, your next action gains +1 multihit."),

                new Identity(8, "DesperationAim", "ONAFTERMISS", "TURN", "hero_hit_threshold", -1,
                    Description: "After a miss, hit threshold improves by 1 for the turn."),
                new Identity(9, "OpenTheLane", "ONFIRSTHIT", "TURN", "hero_combo_threshold", -1,
                    Description: "On first hit, combo threshold improves by 1 for the turn."),
                new Identity(10, "BloodlustCrit", "ONKILL", "TURN", "hero_crit_threshold", -1,
                    Description: "On kill, crit threshold improves by 1 for the turn."),
                new Identity(11, "SteadyHand", "ONCRITICALMISS", "TURN", "hero_crit_miss_threshold", 1,
                    Description: "On crit miss, crit-miss threshold is eased by 1 for the turn."),
                new Identity(12, "LockedInAcc", "ONCONNECT", "TURN", "hero_accuracy", 1,
                    Description: "On connect, accuracy +1 for the turn."),
                new Identity(13, "EnemyFlinch", "ONCOMBOEND", "TURN", "enemy_hit_threshold", 1,
                    Description: "On combo end, foes need +1 more to hit this turn."),
                new Identity(14, "EnemyAccTax", "ONMISS", "TURN", "enemy_accuracy", 1,
                    Description: "On miss, tax foe accuracy by 1 for the turn."),
                new Identity(15, "CritThresholdPush", "ONCRITICAL", "TURN", "hero_crit_threshold", -1,
                    Description: "On crit, crit threshold improves by 1 for the turn."),
                new Identity(16, "ComboThresholdPush", "ONCOMBO", "TURN", "hero_combo_threshold", -1,
                    Description: "On combo, combo threshold improves by 1 for the turn."),
                new Identity(17, "LuckySevenHit", "ONNATURALROLL:7", "TURN", "hero_hit_threshold", -1,
                    Description: "On natural 7, hit threshold improves by 1 for the turn."),
                new Identity(18, "ExactFifteenCombo", "ONROLLVALUE:15", "TURN", "hero_combo_threshold", -1,
                    Description: "On attack total 15, combo threshold improves by 1 for the turn."),
                new Identity(19, "ClutchForgive", "ONCONNECT", "TURN", "hero_hit_threshold", -1,
                    Filters: new[] { "IFCLUTCH" },
                    Description: "While clutch, a connect improves hit threshold by 1 for the turn."),

                new Identity(20, "CritFace19", "ONCONNECT", "TURN", "crit_face_min:19", 19,
                    Description: "On connect, crit faces start at 19 for the turn."),
                new Identity(21, "CritFace18", "ONFIRSTHIT", "TURN", "crit_face_min:18", 18,
                    Description: "On first hit, crit faces start at 18 for the turn."),
                new Identity(22, "SalvageCharm", "ONCONNECT", "FIGHT", "salvage_miss", 1,
                    Description: "On connect, gain miss salvage for the fight."),
                new Identity(23, "KillReload20", "ONKILL", "", "replace_next_roll:20", 20,
                    Description: "On kill, replace your next d20 with a 20."),
                new Identity(24, "ComboEndReload15", "ONCOMBOEND", "", "replace_next_roll:15", 15,
                    Description: "On combo end, replace your next d20 with a 15."),

                new Identity(25, "SharpenedEdge", "ONCOMBO", "TURN", "hero_weapon_damage", 1,
                    Description: "On combo, weapon damage +1 for the turn."),
                new Identity(26, "QuickGrip", "ONCONNECT", "TURN", "hero_weapon_speed", 1,
                    Description: "On connect, weapon speed +1 for the turn."),
                new Identity(27, "MarchCadence", "ONROOMSCLEARED", "FIGHT", "hero_weapon_damage", 1,
                    Description: "On room clear, weapon damage +1 for the fight."),
                new Identity(28, "EnemySlowWeapon", "ONMISS", "TURN", "enemy_weapon_speed", 1,
                    Description: "On miss, foe weapon speed is slowed by 1 for the turn."),

                new Identity(29, "ChaosOpen", "ONFIRSTHIT", "", "strip_shuffle",
                    Description: "On first hit, shuffle your combo strip."),
                new Identity(30, "EchoStep", "ONCOMBO", "", "strip_repeat",
                    Description: "On combo, repeat the current strip slot."),
                new Identity(31, "SkipFiller", "ONCONNECT", "", "strip_skip",
                    Description: "On connect, skip the next strip slot."),
                new Identity(32, "LeapAhead", "ONCONNECT", "", "strip_jump",
                    Description: "On connect, jump ahead on the combo strip."),
                new Identity(33, "LoopChain", "ONCONNECT", "", "strip_loop",
                    Description: "On connect, loop the combo strip."),
                new Identity(34, "BreakRhythm", "ONMISS", "", "strip_stop",
                    Description: "On miss, stop strip progression."),
                new Identity(35, "WildNext", "ONCONNECT", "", "strip_random",
                    Description: "On connect, pick a random next strip action."),
                new Identity(36, "SealSlot", "ONCONNECT", "", "strip_disable",
                    Description: "On connect, disable a strip slot."),
                new Identity(37, "ReplaceNextStrip", "ONCRITICAL", "", "strip_replace_next",
                    Description: "On crit, replace the next strip action."),

                new Identity(38, "DoubleBeat", "ONCOMBO", "", "retrigger_next",
                    Description: "On combo, retrigger the next strip action."),
                new Identity(39, "ResetOpener", "ONKILL", "", "retrigger_opener",
                    Description: "On kill, retrigger your opener."),
                new Identity(40, "FinisherEncore", "ONCOMBOEND", "", "retrigger_finisher",
                    Description: "On combo end, retrigger your finisher."),
                new Identity(41, "SlotTwoEncore", "ONCRITICAL", "", "retrigger_slot:2",
                    Description: "On crit, retrigger combo strip slot 2."),

                new Identity(42, "CritExpose", "ONCRITICAL", "", "expose",
                    Description: "On crit, apply Expose."),
                new Identity(43, "ComboWeaken", "ONCOMBO", "", "weaken",
                    Description: "On combo, apply Weaken."),
                new Identity(44, "SteadyFocus", "ONCONNECT", "", "focus",
                    Description: "On connect, apply Focus."),
                new Identity(45, "GuardUp", "ONMISS", "", "harden",
                    Description: "On miss, apply Harden."),
                new Identity(46, "FirstFortify", "ONFIRSTHIT", "", "fortify",
                    Description: "On first hit, apply Fortify."),
                new Identity(47, "ConnectPierce", "ONCONNECT", "", "pierce",
                    Description: "On connect, apply Pierce."),
                new Identity(48, "EndVulnerability", "ONCOMBOEND", "", "vulnerability",
                    Description: "On combo end, apply Vulnerability."),
                new Identity(49, "CritMissSlow", "ONCRITICALMISS", "", "slow",
                    Description: "On crit miss, apply Slow."),
                new Identity(50, "PunishDot", "ONCONNECT", "", "pierce",
                    Filters: new[] { "IFTARGETUNDERDOT" },
                    Description: "On connect vs a DoT'd foe, apply Pierce."),
                new Identity(51, "SpiteFocus", "ONCONNECT", "", "focus",
                    Filters: new[] { "IFSOURCEUNDERDOT" },
                    Description: "On connect while you have DoT, apply Focus."),

                new Identity(52, "ComboEndHeal", "ONCOMBOEND", "", "heal", 2,
                    Description: "On combo end, heal 2 HP."),
                new Identity(53, "RoomClearHeal", "ONROOMSCLEARED", "", "heal", 2,
                    Description: "On room clear, heal 2 HP."),
                new Identity(54, "AfterMissHeal", "ONAFTERMISS", "", "heal", 2,
                    Description: "After a miss, heal 2 HP."),

                new Identity(55, "CritLadderDungeon", "ONCRITICAL", "DUNGEON", "hero_crit_threshold", -1,
                    Description: "On crit, crit threshold improves by 1 for the dungeon."),
                new Identity(56, "HitLadderDungeon", "ONCONNECT", "DUNGEON", "hero_hit_threshold", -1,
                    Description: "On connect, hit threshold improves by 1 for the dungeon."),
                new Identity(57, "MissHitTaxDungeon", "ONMISS", "DUNGEON", "hero_hit_threshold", 1,
                    Description: "On miss, hit threshold worsens by 1 for the dungeon."),
                new Identity(58, "GoldSetArmor", "WHILE_EQUIPPED", "", "armor", 2,
                    Filters: new[] { "IFGEARHASTAG:gold" }, IsEquipEffect: true,
                    Description: "While equipped with gold-tagged gear, +2 armor."),
                new Identity(59, "ClassPrimaryAttr", "WHILE_EQUIPPED", "", "hero_stat_bonus:PRIMARY", 1,
                    Filters: new[] { "IFCLASSTAG:barbarian" }, IsEquipEffect: true,
                    Description: "While equipped as Barbarian, +1 to your primary attribute."),
                new Identity(60, "EvenRollHeal", "ONEVEN", "", "heal", 1,
                    Description: "On an even roll, heal 1 HP."),
                new Identity(61, "BarbarianActionDamage", "ONCONNECT", "ACTION", "hero_next_action_damage", 5,
                    Filters: new[] { "IFACTIONHASTAG:barbarian" },
                    Description: "On connect with a Barbarian-tagged action, next action +5% damage."),
                new Identity(62, "GrantBarbarianTag", "WHILE_EQUIPPED", "", "grant_action_tag:barbarian",
                    IsEquipEffect: true,
                    Description: "While equipped, grant the Barbarian action tag overlay."),
                new Identity(63, "SlotThreeSpeedDungeon", "ONCONNECT", "DUNGEON", "hero_next_action_speed", 5,
                    Filters: new[] { "IFSLOT:3" },
                    Description: "On connect from strip slot 3, next action +5% speed (dungeon)."),
                new Identity(64, "MirrorSwingDamage", "ONCONNECT", "", "hero_action_damage", 100,
                    Filters: new[] { "IFSAMESACTION" },
                    Description: "On connect with the same action as last swing, this swing +100% damage."),
                new Identity(65, "UnarmedPunchGrant", "WHILE_EQUIPPED", "", "grant_action:PUNCH HARD",
                    Filters: new[] { "IFUNARMED" }, IsEquipEffect: true,
                    Description: "While equipped and unarmed, grant Punch Hard."),

                new Identity(66, "SwiftSchool", "WHILE_EQUIPPED", "", "hero_action_speed", 8,
                    Filters: new[] { "IFACTIONHASTAG:swift" }, IsEquipEffect: true,
                    Description: "While equipped, Swift-tagged actions are +8% faster this swing."),
                new Identity(67, "BludgeonChorus", "WHILE_EQUIPPED", "", "hero_action_damage", 12,
                    Filters: new[] { "IFACTIONHASTAG:bludgeon" }, IsEquipEffect: true,
                    Description: "While equipped, Bludgeon-tagged actions deal +12% damage this swing."),
                new Identity(68, "FocusLens", "WHILE_EQUIPPED", "", "hero_action_amp", 10,
                    Filters: new[] { "IFACTIONHASTAG:focus" }, IsEquipEffect: true,
                    Description: "While equipped, Focus-tagged actions gain +10% amp this swing."),
                new Identity(69, "InsightClarity", "WHILE_EQUIPPED", "", "hero_action_amp", 8,
                    Filters: new[] { "IFACTIONHASTAG:insight" }, IsEquipEffect: true,
                    Description: "While equipped, Insight-tagged actions gain +8% amp this swing."),
                new Identity(70, "AimTrue", "WHILE_EQUIPPED", "", "hero_hit_threshold", -1,
                    Filters: new[] { "IFACTIONHASTAG:aim" }, IsEquipEffect: true,
                    Description: "While equipped, Aim-tagged actions improve hit threshold by 1."),
                new Identity(71, "FootworkGreaves", "WHILE_EQUIPPED", "", "hero_action_speed", 6,
                    Filters: new[] { "IFACTIONHASTAG:footwork" }, IsEquipEffect: true,
                    Description: "While equipped, Footwork-tagged actions are +6% faster this swing."),
                new Identity(72, "Firebrand", "WHILE_EQUIPPED", "", "hero_action_damage", 10,
                    Filters: new[] { "IFACTIONHASTAG:fire" }, IsEquipEffect: true,
                    Description: "While equipped, Fire-tagged actions deal +10% damage this swing."),
                new Identity(73, "OpenerCrown", "WHILE_EQUIPPED", "", "hero_action_amp", 12,
                    Filters: new[] { "IFACTIONHASTAG:opener" }, IsEquipEffect: true,
                    Description: "While equipped, Opener-tagged actions gain +12% amp this swing."),
                new Identity(74, "FinisherSeal", "WHILE_EQUIPPED", "", "hero_action_damage", 15,
                    Filters: new[] { "IFACTIONHASTAG:finisher" }, IsEquipEffect: true,
                    Description: "While equipped, Finisher-tagged actions deal +15% damage this swing."),
                new Identity(75, "RogueActionAmp", "WHILE_EQUIPPED", "", "hero_action_damage", 8,
                    Filters: new[] { "IFACTIONHASTAG:rogue" }, IsEquipEffect: true,
                    Description: "While equipped, Rogue-tagged actions deal +8% damage this swing."),
                new Identity(76, "WarriorActionAmp", "WHILE_EQUIPPED", "", "hero_action_damage", 8,
                    Filters: new[] { "IFACTIONHASTAG:warrior" }, IsEquipEffect: true,
                    Description: "While equipped, Warrior-tagged actions deal +8% damage this swing."),
                new Identity(77, "WizardActionAmp", "WHILE_EQUIPPED", "", "hero_action_amp", 8,
                    Filters: new[] { "IFACTIONHASTAG:wizard" }, IsEquipEffect: true,
                    Description: "While equipped, Wizard-tagged actions gain +8% amp this swing."),
                new Identity(78, "WaterBrand", "WHILE_EQUIPPED", "", "hero_action_damage", 10,
                    Filters: new[] { "IFACTIONHASTAG:water" }, IsEquipEffect: true,
                    Description: "While equipped, Water-tagged actions deal +10% damage this swing."),
                new Identity(79, "AirBrand", "WHILE_EQUIPPED", "", "hero_action_speed", 8,
                    Filters: new[] { "IFACTIONHASTAG:air" }, IsEquipEffect: true,
                    Description: "While equipped, Air-tagged actions are +8% faster this swing."),
                new Identity(80, "EarthBrand", "WHILE_EQUIPPED", "", "hero_action_damage", 10,
                    Filters: new[] { "IFACTIONHASTAG:earth" }, IsEquipEffect: true,
                    Description: "While equipped, Earth-tagged actions deal +10% damage this swing."),

                new Identity(81, "StrCleave", "WHILE_EQUIPPED", "", "hero_action_damage", 1,
                    IsEquipEffect: true, ScaleFrom: "STR",
                    Description: "While equipped, this swing damage scales +1% per Strength."),
                new Identity(82, "AgiSkirmish", "WHILE_EQUIPPED", "", "hero_action_speed", 1,
                    IsEquipEffect: true, ScaleFrom: "AGI",
                    Description: "While equipped, this swing speed scales +1% per Agility."),
                new Identity(83, "TecPrecision", "WHILE_EQUIPPED", "", "hero_hit_threshold", -1,
                    IsEquipEffect: true,
                    Description: "While equipped, improve hit threshold by 1."),
                new Identity(84, "IntArchitect", "WHILE_EQUIPPED", "", "hero_action_amp", 1,
                    IsEquipEffect: true, ScaleFrom: "INT",
                    Description: "While equipped, this swing amp scales +1% per Intelligence."),
                new Identity(85, "PrimaryPulse", "WHILE_EQUIPPED", "", "armor", 1,
                    IsEquipEffect: true, ScaleFrom: "PRIMARY",
                    Description: "While equipped, armor scales +1 per primary attribute point."),
                new Identity(86, "BarbarianBlood", "WHILE_EQUIPPED", "", "hero_action_damage", 2,
                    Filters: new[] { "IFACTIONHASTAG:barbarian" }, IsEquipEffect: true, ScaleFrom: "BARBARIAN",
                    Description: "While equipped, Barbarian actions deal +2% damage per Barbarian point."),
                new Identity(87, "LevelLadderArmor", "WHILE_EQUIPPED", "", "armor", 1,
                    IsEquipEffect: true, ScaleFrom: "LEVEL",
                    Description: "While equipped, armor scales +1 per character level."),
                new Identity(88, "WarriorPointsGuard", "WHILE_EQUIPPED", "", "armor", 1,
                    IsEquipEffect: true, ScaleFrom: "WARRIOR",
                    Description: "While equipped, armor scales +1 per Warrior point."),
                new Identity(89, "RoguePointsSpeed", "WHILE_EQUIPPED", "", "hero_action_speed", 1,
                    IsEquipEffect: true, ScaleFrom: "ROGUE",
                    Description: "While equipped, this swing speed scales +1% per Rogue point."),
                new Identity(90, "WizardPointsAmp", "WHILE_EQUIPPED", "", "hero_action_amp", 1,
                    IsEquipEffect: true, ScaleFrom: "WIZARD",
                    Description: "While equipped, this swing amp scales +1% per Wizard point."),

                new Identity(91, "SteelPlateEcho", "WHILE_EQUIPPED", "", "armor", 3,
                    Filters: new[] { "IFGEARHASTAG:steel" }, IsEquipEffect: true,
                    Description: "While equipped with steel-tagged gear, +3 armor."),
                new Identity(92, "ObsidianEdge", "ONCONNECT", "", "pierce",
                    Filters: new[] { "IFGEARHASTAG:obsidian" },
                    Description: "On connect while wearing obsidian-tagged gear, apply Pierce."),
                new Identity(93, "WillowSwift", "WHILE_EQUIPPED", "", "hero_action_speed", 5,
                    Filters: new[] { "IFGEARHASTAG:willow" }, IsEquipEffect: true,
                    Description: "While equipped with willow-tagged gear, this swing +5% speed."),
                new Identity(94, "SilverWard", "WHILE_EQUIPPED", "", "armor", 2,
                    Filters: new[] { "IFGEARHASTAG:silver" }, IsEquipEffect: true,
                    Description: "While equipped with silver-tagged gear, +2 armor."),
                new Identity(95, "GrantRogueTag", "WHILE_EQUIPPED", "", "grant_action_tag:rogue",
                    IsEquipEffect: true,
                    Description: "While equipped, grant the Rogue action tag overlay."),
                new Identity(96, "ClassWarriorPrimary", "WHILE_EQUIPPED", "", "hero_stat_bonus:PRIMARY", 1,
                    Filters: new[] { "IFCLASSTAG:warrior" }, IsEquipEffect: true,
                    Description: "While equipped as Warrior, +1 to your primary attribute."),

                new Identity(97, "BossNeedle", "ONCONNECT", "ACTION", "hero_next_action_amp", 15,
                    Filters: new[] { "IFTARGETHASTAG:boss" },
                    Description: "On connect vs a boss, your next action gains +15% amp."),
                new Identity(98, "MinionScythe", "ONCONNECT", "ACTION", "hero_next_action_multihit", 1,
                    Filters: new[] { "IFTARGETHASTAG:minion" },
                    Description: "On connect vs a minion, your next action gains +1 multihit."),
                new Identity(99, "ClutchIron", "WHILE_EQUIPPED", "", "armor", 4,
                    Filters: new[] { "IFCLUTCH" }, IsEquipEffect: true,
                    Description: "While equipped and clutch, +4 armor."),
                new Identity(100, "OddSpite", "ONODD", "", "slow",
                    Description: "On an odd roll, apply Slow."),
                new Identity(101, "HurtPride", "ONTAKEHIT", "ACTION", "hero_next_action_damage", 15,
                    Description: "When you take a hit, your next action deals +15% damage."),
                new Identity(102, "HurtHarden", "ONTAKEHIT", "", "harden",
                    Description: "When you take a hit, apply Harden."),
                new Identity(103, "DepthHunger", "ONROOMSCLEARED", "DUNGEON", "hero_weapon_damage", 1,
                    Description: "On room clear, weapon damage +1 for the dungeon."),
                new Identity(104, "StrongarmGate", "ONCONNECT", "ACTION", "hero_next_action_damage", 10,
                    Filters: new[] { "IFATTR:STR>=8" },
                    Description: "On connect with STR ≥ 8, your next action deals +10% damage."),
                new Identity(105, "SwitchUpSpeed", "ONCONNECT", "ACTION", "hero_next_action_speed", 10,
                    Filters: new[] { "IFDIFFERENTACTION" },
                    Description: "On connect with a different action than last, next action +10% speed."),
            };
        }
    }
}
