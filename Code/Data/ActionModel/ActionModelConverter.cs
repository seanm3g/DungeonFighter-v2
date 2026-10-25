using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Text.Json;

namespace RPGGame.Data.ActionModel
{
    /// <summary>
    /// Reads one Actions.json row (<see cref="SpreadsheetActionJson"/>) into the draft <see cref="ActionModel"/>
    /// and reports what didn't survive. Read-only: nothing at runtime calls this yet.
    /// <para>
    /// Canonical path: CADENCES bundles point at mechanic ids, the flat columns hold the magnitudes; TRIGGER bundles
    /// add the WHEN gate. <c>actionAttackBonusesJson</c> is a second copy of the self bonuses, rebuilt independently and
    /// diffed against the canonical result. Any non-empty string column nothing consumed is reported as a Loss.
    /// </para>
    /// </summary>
    public static class ActionModelConverter
    {
        private static readonly Dictionary<string, PropertyInfo> StringColumns = typeof(SpreadsheetActionJson)
            .GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Where(p => p.PropertyType == typeof(string) && p.CanRead)
            .ToDictionary(p => p.Name, StringComparer.Ordinal);

        /// <summary>Identity / presentation columns the model doesn't need to carry.</summary>
        private static readonly HashSet<string> IgnoredColumns = new(StringComparer.Ordinal)
        {
            nameof(SpreadsheetActionJson.Action), nameof(SpreadsheetActionJson.Description),
            nameof(SpreadsheetActionJson.Rarity), nameof(SpreadsheetActionJson.Tags),
            nameof(SpreadsheetActionJson.Category), nameof(SpreadsheetActionJson.ColumnC),
            nameof(SpreadsheetActionJson.WeaponTypes), nameof(SpreadsheetActionJson.IsDefaultAction),
        };

        /// <summary>WHEN tokens whose COUNT cell is the event's operand (face, room count) rather than an enable flag.</summary>
        private static readonly HashSet<string> CountIsWhenArg = new(StringComparer.OrdinalIgnoreCase)
        {
            "ONROLLVALUE", "ONNATURALROLL", "ONROOMSCLEARED",
        };

        /// <summary>Quantities <c>actionAttackBonusesJson</c> can express, for the copy diff.</summary>
        private static readonly Dictionary<string, Quantity> BonusTypeToQuantity = new(StringComparer.OrdinalIgnoreCase)
        {
            ["STR"] = Quantity.STR, ["AGI"] = Quantity.AGI, ["TECH"] = Quantity.TECH, ["INT"] = Quantity.INT,
            ["ACCURACY"] = Quantity.Accuracy, ["HIT"] = Quantity.Hit, ["COMBO"] = Quantity.Combo,
            ["CRIT"] = Quantity.Crit, ["CRIT_MISS"] = Quantity.CritMiss,
            ["DAMAGE_MOD"] = Quantity.DamagePercent, ["SPEED_MOD"] = Quantity.SpeedPercent, ["AMP_MOD"] = Quantity.AmpPercent,
            ["MULTIHIT_MOD"] = Quantity.Hits, ["WEAPON_DAMAGE"] = Quantity.WeaponDamage, ["WEAPON_SPEED"] = Quantity.WeaponSpeed,
        };

        /// <summary>Words in a description that promise an effect; used to flag rows whose behaviour isn't in the data.</summary>
        private static readonly string[] EffectWords = { "+", "On ", "on connect", "Gains", "triggers", " per ", " add " };

        public static (ActionModel Model, List<ConversionIssue> Issues) Convert(SpreadsheetActionJson row)
        {
            var ctx = new Context(row);

            var chassis = ReadChassis(ctx);
            bool opener = IsTruthy(ctx.Use(nameof(SpreadsheetActionJson.Opener)));
            bool finisher = IsTruthy(ctx.Use(nameof(SpreadsheetActionJson.Finisher)));

            var selfTargeted = SplitList(ctx.Use(nameof(SpreadsheetActionJson.SelfTargetEffects)))
                .Select(ActionMechanicsRegistry.NormalizeMechanicId)
                .ToHashSet(StringComparer.OrdinalIgnoreCase);
            ctx.SelfTargeted = selfTargeted;

            var triggers = ParseJson<List<ActionTriggerBundle>>(ctx, nameof(SpreadsheetActionJson.TriggerBundlesJson)) ?? new();
            var cadences = ParseJson<List<ActionCadenceBundle>>(ctx, nameof(SpreadsheetActionJson.CadenceBundlesJson)) ?? new();
            var filters = ReadTriggerConditions(ctx, triggers);

            var triggerMechanics = triggers
                .SelectMany(t => t.ParseMechanicIds())
                .Select(id => ActionMechanicsRegistry.NormalizeMechanicId(SplitArg(id).Id))
                .ToHashSet(StringComparer.OrdinalIgnoreCase);

            foreach (var bundle in cadences.Where(b => b.IsEnabled))
            {
                var scope = ParseScope(ctx, "cadenceBundlesJson.cadence", bundle.Cadence, bundle.ResolveDurationCount());
                foreach (string raw in bundle.ParseMechanicIds())
                {
                    var (id, arg) = SplitArg(raw);
                    id = ActionMechanicsRegistry.NormalizeMechanicId(id);
                    if (id == "heal" && triggerMechanics.Contains("heal"))
                    {
                        ctx.Note("cadenceBundlesJson", $"heal listed in both a {bundle.Cadence} cadence and a trigger; kept the trigger copy only");
                        continue;
                    }
                    MapMechanic(ctx, id, arg, scope, when: null, bundleValue: null, scaleFrom: null, countArg: null);
                }
            }

            foreach (var bundle in triggers.Where(b => b.IsEnabled))
            {
                string count = (bundle.Count ?? "").Trim();
                bool countIsWhenArg = CountIsWhenArg.Contains(bundle.When ?? "");
                var bundleFilters = (bundle.Filters ?? new List<string>()).Concat(filters).Distinct().ToList();
                var when = new When((bundle.When ?? "").Trim().ToUpperInvariant(), countIsWhenArg ? count : null, bundleFilters);
                var scope = string.IsNullOrWhiteSpace(bundle.Scope)
                    ? ActionScope.Instant
                    : ParseScope(ctx, "triggerBundlesJson.scope", bundle.Scope, 1);
                string? countArg = !countIsWhenArg && count != "1" ? count : null;

                foreach (string raw in bundle.ParseMechanicIds())
                {
                    var (id, arg) = SplitArg(raw);
                    id = ActionMechanicsRegistry.NormalizeMechanicId(id);
                    MapMechanic(ctx, id, arg, scope, when, bundle.Value, bundle.ScaleFrom, countArg);
                }
            }

            CheckFlatTriggerColumns(ctx, triggers);
            ReadScaling(ctx);
            DiffAttackBonusCopy(ctx);
            CheckDescription(ctx, row);
            ctx.ReportUnconsumed();

            var model = new ActionModel(
                row.Action,
                chassis,
                ctx.Modifiers,
                ctx.RollOps,
                ctx.StripOps,
                SplitList(row.Tags).ToList(),
                opener,
                finisher);
            return (model, ctx.Issues);
        }

        private static Chassis ReadChassis(Context ctx)
        {
            double damage = ctx.Num(nameof(SpreadsheetActionJson.Damage), 100);
            double speed = ctx.Num(nameof(SpreadsheetActionJson.Speed), 1);
            int hits = (int)ctx.Num(nameof(SpreadsheetActionJson.NumberOfHits), 1);
            double block = ctx.Num(nameof(SpreadsheetActionJson.Block), 0);

            string target = ctx.Use(nameof(SpreadsheetActionJson.Target));
            var strikeTarget = ModTarget.Enemy;
            if (target.Equals("self", StringComparison.OrdinalIgnoreCase))
                strikeTarget = ModTarget.Self;
            else if (target.Length > 0 && !target.Equals("enemy", StringComparison.OrdinalIgnoreCase))
                ctx.Loss("target", $"unknown target '{target}'");

            // DPS is derived; the model drops it, so only say when the stored copy has drifted.
            string dpsRaw = ctx.Use(nameof(SpreadsheetActionJson.DPS));
            if (TryNum(dpsRaw, out double dps) && speed > 0)
            {
                double computed = damage * hits / speed;
                if (Math.Abs(dps - computed) > 1.5)
                    ctx.Note("dps", $"stored {dpsRaw} but damage×hits/speed = {computed:0.#}%");
            }

            return new Chassis(damage, speed, hits, block, strikeTarget);
        }

        private static void MapMechanic(Context ctx, string id, string? arg, ActionScope scope, When? when,
            double? bundleValue, string? scaleFrom, string? countArg)
        {
            void Self(Quantity q, string column) => ctx.AddFromColumn(q, ModOp.Add, ModTarget.Self, column, scope, when, scaleFrom, bundleValue, id);
            void Status(Quantity q, string column)
            {
                var target = q == Quantity.Harden || ctx.SelfTargeted.Contains(id) ? ModTarget.Self : ModTarget.Enemy;
                ctx.AddFromColumn(q, ModOp.Stack, target, column, scope, when, scaleFrom, bundleValue, id);
            }
            void Strip(StripOpKind kind, string? a = null) => ctx.StripOps.Add(new StripOp(kind, a, ModTarget.Self, when));
            void Roll(RollOpKind kind, int? v = null) => ctx.RollOps.Add(new RollOp(kind, v, scope, when));

            switch (id)
            {
                case "hero_stat_bonus":
                    int before = ctx.Modifiers.Count;
                    foreach (var (q, col) in new[]
                    {
                        (Quantity.STR, nameof(SpreadsheetActionJson.HeroSTR)), (Quantity.AGI, nameof(SpreadsheetActionJson.HeroAGI)),
                        (Quantity.TECH, nameof(SpreadsheetActionJson.HeroTECH)), (Quantity.INT, nameof(SpreadsheetActionJson.HeroINT)),
                    })
                    {
                        if (ctx.Peek(col).Length > 0)
                            Self(q, col);
                    }
                    if (ctx.Modifiers.Count == before)
                        ctx.Loss(id, "points at hero STR/AGI/TECH/INT but all four columns are empty");
                    break;
                case "hero_accuracy": Self(Quantity.Accuracy, nameof(SpreadsheetActionJson.HeroAccuracy)); break;
                case "hero_hit_threshold": Self(Quantity.Hit, nameof(SpreadsheetActionJson.HeroHit)); break;
                case "hero_combo_threshold": Self(Quantity.Combo, nameof(SpreadsheetActionJson.HeroCombo)); break;
                case "hero_crit_threshold": Self(Quantity.Crit, nameof(SpreadsheetActionJson.HeroCrit)); break;
                case "hero_crit_miss_threshold": Self(Quantity.CritMiss, nameof(SpreadsheetActionJson.HeroCritMiss)); break;
                case "hero_next_action_damage": Self(Quantity.DamagePercent, nameof(SpreadsheetActionJson.DamageMod)); break;
                case "hero_next_action_speed": Self(Quantity.SpeedPercent, nameof(SpreadsheetActionJson.SpeedMod)); break;
                case "hero_next_action_amp": Self(Quantity.AmpPercent, nameof(SpreadsheetActionJson.AmpMod)); break;
                case "hero_next_action_multihit": Self(Quantity.Hits, nameof(SpreadsheetActionJson.MultiHitMod)); break;
                case "hero_weapon_damage": Self(Quantity.WeaponDamage, nameof(SpreadsheetActionJson.WeaponDamageMod)); break;
                case "hero_weapon_speed": Self(Quantity.WeaponSpeed, nameof(SpreadsheetActionJson.WeaponSpeedMod)); break;
                case "hero_action_damage":
                    // This-hit damage (BERSERKER): magnitude on the trigger bundle, not a column.
                    scope = scope.Kind == ScopeKind.Instant ? ActionScope.Hit : scope;
                    Self(Quantity.DamagePercent, nameof(SpreadsheetActionJson.DamageMod));
                    break;

                case "weaken": Status(Quantity.Weaken, nameof(SpreadsheetActionJson.Weaken)); break;
                case "slow": Status(Quantity.Slow, nameof(SpreadsheetActionJson.Slow)); break;
                case "vulnerability": Status(Quantity.Vulnerability, nameof(SpreadsheetActionJson.Vulnerability)); break;
                case "expose": Status(Quantity.Expose, nameof(SpreadsheetActionJson.Expose)); break;
                case "harden": Status(Quantity.Harden, nameof(SpreadsheetActionJson.Harden)); break;
                case "pierce": Status(Quantity.Pierce, nameof(SpreadsheetActionJson.Pierce)); break;
                case "confuse": Status(Quantity.Confuse, nameof(SpreadsheetActionJson.Confuse)); break;

                case "heal":
                    ctx.AddFromColumn(Quantity.Health, ModOp.Add, ModTarget.Self, nameof(SpreadsheetActionJson.HeroHeal),
                        when == null ? scope : ActionScope.Instant, when, scaleFrom, bundleValue, id);
                    break;

                case "disrupt":
                    ctx.Use(nameof(SpreadsheetActionJson.Disrupt));
                    ctx.StripOps.Add(new StripOp(StripOpKind.Disrupt, null, ModTarget.Enemy, when));
                    break;

                case "advantage": Roll(RollOpKind.Advantage); break;
                case "disadvantage": Roll(RollOpKind.Disadvantage); break;
                case "exploding_dice":
                    string threshold = ctx.Use(nameof(SpreadsheetActionJson.ExplodingDiceThreshold));
                    Roll(RollOpKind.Explode, TryNum(threshold, out double t) ? (int)t : null);
                    break;
                case "salvage_miss": Roll(RollOpKind.SalvageMiss); break;
                case "crit_face_min": Roll(RollOpKind.CritFaceMin, ParseIntArg(ctx, id, arg)); break;
                case "replace_next_roll": Roll(RollOpKind.ReplaceNext, ParseIntArg(ctx, id, arg)); break;

                case "strip_jump":
                    // COUNT is the destination slot for a jump.
                    Strip(StripOpKind.Jump, arg ?? countArg);
                    countArg = null;
                    break;
                case "strip_skip": Strip(StripOpKind.Skip); break;
                case "strip_repeat": Strip(StripOpKind.Repeat); break;
                case "strip_loop": Strip(StripOpKind.Loop); break;
                case "strip_stop": Strip(StripOpKind.Stop); break;
                case "strip_random": Strip(StripOpKind.Random); break;
                case "strip_shuffle": Strip(StripOpKind.Shuffle); break;
                case "strip_replace_next": Strip(StripOpKind.ReplaceNext, arg); break;
                case "strip_disable": Strip(StripOpKind.Disable); break;
                case "retrigger_next": Strip(StripOpKind.RetriggerNext); break;
                case "retrigger_opener": Strip(StripOpKind.RetriggerOpener); break;
                case "retrigger_finisher": Strip(StripOpKind.RetriggerFinisher); break;
                case "retrigger_slot": Strip(StripOpKind.RetriggerSlot, arg); break;

                default:
                    ctx.Loss("mechanics", $"no primitive for mechanic '{id}'{(arg != null ? ":" + arg : "")}");
                    break;
            }

            if (countArg != null)
                ctx.Loss("triggerBundlesJson.count", $"count '{countArg}' on {id} has no slot in the model");
        }

        /// <summary>IF… tokens become filters on every trigger; ON… tokens must repeat a trigger bundle's WHEN.</summary>
        private static List<string> ReadTriggerConditions(Context ctx, List<ActionTriggerBundle> triggers)
        {
            var filters = new List<string>();
            foreach (string token in SplitList(ctx.Use(nameof(SpreadsheetActionJson.TriggerConditions))))
            {
                string t = token.ToUpperInvariant();
                if (t.StartsWith("IF", StringComparison.Ordinal))
                {
                    if (triggers.Count == 0)
                        ctx.Loss("triggerConditions", $"filter {t} has no trigger to gate");
                    filters.Add(t);
                    continue;
                }

                var (whenToken, whenArg) = SplitArg(t);
                bool matched = triggers.Any(b => string.Equals(b.When, whenToken, StringComparison.OrdinalIgnoreCase)
                    && (whenArg == null || string.Equals((b.Count ?? "").Trim(), whenArg, StringComparison.OrdinalIgnoreCase)));
                if (matched)
                    ctx.Note("triggerConditions", $"{t} repeats a trigger bundle's WHEN");
                else
                    ctx.Loss("triggerConditions", $"{t} has no matching trigger bundle");
            }
            return filters;
        }

        /// <summary>The flat ON… columns duplicate trigger bundles; check they agree.</summary>
        private static void CheckFlatTriggerColumns(Context ctx, List<ActionTriggerBundle> triggers)
        {
            void Check(string column, string when, bool valueIsCount)
            {
                string v = ctx.Use(column);
                if (v.Length == 0)
                    return;
                bool matched = triggers.Any(b => string.Equals(b.When, when, StringComparison.OrdinalIgnoreCase)
                    && (!valueIsCount || string.Equals((b.Count ?? "").Trim(), v, StringComparison.Ordinal)));
                if (!matched)
                    ctx.Loss(column, $"'{v}' but no {when} trigger bundle{(valueIsCount ? " with count " + v : "")}");
            }

            Check(nameof(SpreadsheetActionJson.OnHit), "ONHIT", false);
            Check(nameof(SpreadsheetActionJson.OnMiss), "ONMISS", false);
            Check(nameof(SpreadsheetActionJson.OnCrit), "ONCRITICAL", false);
            Check(nameof(SpreadsheetActionJson.OnKill), "ONKILL", false);
            Check(nameof(SpreadsheetActionJson.OnRoomsCleared), "ONROOMSCLEARED", true);
            Check(nameof(SpreadsheetActionJson.OnRollValue), "ONROLLVALUE", true);
        }

        /// <summary>Material keyword scaling and lifesteal: this-hit modifiers whose value scales from a counter.</summary>
        private static void ReadScaling(Context ctx)
        {
            string material = ctx.Use(nameof(SpreadsheetActionJson.MaterialScale));
            string keyword = ctx.Use(nameof(SpreadsheetActionJson.KeywordScale));
            string formula = ctx.Use(nameof(SpreadsheetActionJson.ScaleFormula));
            if (material.Length > 0 || keyword.Length > 0 || formula.Length > 0)
            {
                if (!keyword.Equals("DAMAGE", StringComparison.OrdinalIgnoreCase))
                    ctx.Loss("keywordScale", $"'{keyword}' has no quantity mapping (only DAMAGE)");
                else if (!TryNum(material, out double per) || formula.Length == 0)
                    ctx.Loss("materialScale", $"incomplete scale: materialScale='{material}' scaleFormula='{formula}'");
                else
                    ctx.Modifiers.Add(new Modifier(Quantity.DamageFlat, ModOp.Add, per, ModTarget.Self, ActionScope.Hit, ScaleFrom: formula.ToUpperInvariant()));
            }

            string lifesteal = ctx.Use(nameof(SpreadsheetActionJson.Lifesteal));
            if (lifesteal.Length > 0)
            {
                if (TryNum(lifesteal, out double fraction))
                    ctx.Modifiers.Add(new Modifier(Quantity.Health, ModOp.Add, fraction, ModTarget.Self, ActionScope.Hit, ScaleFrom: "DAMAGE_DEALT"));
                else
                    ctx.Loss("lifesteal", $"not a number: '{lifesteal}'");
            }
        }

        /// <summary>Rebuild self bonuses from actionAttackBonusesJson and diff against the cadence-derived ones.</summary>
        private static void DiffAttackBonusCopy(Context ctx)
        {
            var bonuses = ParseJson<ActionAttackBonuses>(ctx, nameof(SpreadsheetActionJson.ActionAttackBonusesJson));
            var fromBonuses = new List<string>();
            foreach (var group in bonuses?.BonusGroups ?? new List<ActionAttackBonusGroup>())
            {
                string scopeName = string.IsNullOrWhiteSpace(group.DurationType) ? group.CadenceType : group.DurationType;
                var scope = ParseScope(ctx, "actionAttackBonusesJson.durationType", scopeName, Math.Max(1, group.Count));
                foreach (var b in group.Bonuses)
                {
                    if (BonusTypeToQuantity.TryGetValue(b.Type, out var q))
                        fromBonuses.Add(new Modifier(q, ModOp.Add, b.Value, ModTarget.Self, scope).ToString());
                    else if (b.Type.Equals("ADVANTAGE", StringComparison.OrdinalIgnoreCase))
                        fromBonuses.Add(new RollOp(RollOpKind.Advantage, null, scope).ToString());
                    else if (b.Type.Equals("DISADVANTAGE", StringComparison.OrdinalIgnoreCase))
                        fromBonuses.Add(new RollOp(RollOpKind.Disadvantage, null, scope).ToString());
                    else
                        ctx.Loss("actionAttackBonusesJson", $"bonus type '{b.Type}' has no primitive");
                }
            }

            var expressible = BonusTypeToQuantity.Values.ToHashSet();
            var fromCadence = ctx.Modifiers
                .Where(m => m.Target == ModTarget.Self && m.Op == ModOp.Add && m.When == null && m.ScaleFrom == null && expressible.Contains(m.Quantity))
                .Select(m => m.ToString())
                .Concat(ctx.RollOps
                    .Where(r => r.When == null && r.Kind is RollOpKind.Advantage or RollOpKind.Disadvantage)
                    .Select(r => r.ToString()))
                .ToList();

            var onlyBonuses = fromBonuses.Except(fromCadence).ToList();
            var onlyCadence = fromCadence.Except(fromBonuses).ToList();
            if (onlyBonuses.Count > 0)
                ctx.Loss("actionAttackBonusesJson", "copy has bonuses the cadence path doesn't: " + string.Join("; ", onlyBonuses));
            if (onlyCadence.Count > 0)
                ctx.Loss("actionAttackBonusesJson", "copy is missing cadence bonuses: " + string.Join("; ", onlyCadence));
        }

        /// <summary>Rows whose description promises an effect but whose data encodes none (behaviour in code, or missing).</summary>
        private static void CheckDescription(Context ctx, SpreadsheetActionJson row)
        {
            if (ctx.Modifiers.Count > 0 || ctx.RollOps.Count > 0 || ctx.StripOps.Count > 0)
                return;
            string desc = row.Description ?? "";
            var tags = SplitList(row.Tags).ToList();
            // Enemy swarm rows are flavour text over a multi-hit chassis.
            if (tags.Any(t => t.Equals("enemy", StringComparison.OrdinalIgnoreCase))
                || !EffectWords.Any(w => desc.Contains(w, StringComparison.Ordinal)))
                return;

            bool hybrid = tags.Any(t => t.Equals("hybrid", StringComparison.OrdinalIgnoreCase));
            ctx.Loss("description", hybrid
                ? $"hybrid scaling lives in SkillEffectRouter.Hybrids.cs, not in the row: \"{desc}\""
                : $"promises an effect the row doesn't encode: \"{desc}\"");
        }

        private static ActionScope ParseScope(Context ctx, string field, string? raw, int count)
        {
            string s = (raw ?? "").Trim().ToUpperInvariant();
            ScopeKind? kind = s switch
            {
                "TURN" => ScopeKind.Turn,
                "ACTION" => ScopeKind.Action,
                "FIGHT" => ScopeKind.Fight,
                "DUNGEON" => ScopeKind.Dungeon,
                "" => ScopeKind.Instant,
                _ => null,
            };
            if (kind == null)
            {
                ctx.Loss(field, $"scope '{raw}' has no ScopeKind");
                return ActionScope.Instant;
            }
            return new ActionScope(kind.Value, Math.Max(1, count));
        }

        private static int? ParseIntArg(Context ctx, string id, string? arg)
        {
            if (arg != null && int.TryParse(arg, NumberStyles.Integer, CultureInfo.InvariantCulture, out int v))
                return v;
            ctx.Loss("mechanics", $"{id} needs an integer argument, got '{arg}'");
            return null;
        }

        private static T? ParseJson<T>(Context ctx, string column) where T : class
        {
            string json = ctx.Use(column);
            if (json.Length == 0)
                return null;
            try
            {
                return JsonSerializer.Deserialize<T>(json);
            }
            catch (JsonException ex)
            {
                ctx.Loss(column, "unparseable JSON: " + ex.Message);
                return null;
            }
        }

        /// <summary>"strip_replace_next:STRIKE" → (strip_replace_next, STRIKE). The arg keeps its case.</summary>
        private static (string Id, string? Arg) SplitArg(string raw)
        {
            int i = raw.IndexOf(':');
            return i < 0 ? (raw.Trim(), null) : (raw[..i].Trim(), raw[(i + 1)..].Trim());
        }

        private static IEnumerable<string> SplitList(string? raw) =>
            (raw ?? "").Split(new[] { ',', ';', '|' }, StringSplitOptions.RemoveEmptyEntries)
                .Select(s => s.Trim())
                .Where(s => s.Length > 0);

        private static bool TryNum(string? raw, out double value) =>
            double.TryParse((raw ?? "").Trim().TrimEnd('%'), NumberStyles.Float, CultureInfo.InvariantCulture, out value);

        private static bool IsTruthy(string raw) =>
            raw.Length > 0 && raw is not ("0" or "false" or "FALSE" or "no" or "NO" or "-");

        /// <summary>Per-row state: which columns were read, what was built, what was lost.</summary>
        private sealed class Context
        {
            private readonly SpreadsheetActionJson _row;
            private readonly HashSet<string> _consumed = new(StringComparer.Ordinal);

            public readonly List<Modifier> Modifiers = new();
            public readonly List<RollOp> RollOps = new();
            public readonly List<StripOp> StripOps = new();
            public readonly List<ConversionIssue> Issues = new();
            public HashSet<string> SelfTargeted = new(StringComparer.OrdinalIgnoreCase);

            public Context(SpreadsheetActionJson row) => _row = row;

            public string Peek(string column) =>
                ((string?)StringColumns[column].GetValue(_row) ?? "").Trim();

            public string Use(string column)
            {
                _consumed.Add(column);
                return Peek(column);
            }

            public double Num(string column, double fallback)
            {
                string raw = Use(column);
                if (raw.Length == 0)
                    return fallback;
                if (TryNum(raw, out double v))
                    return v;
                Loss(column, $"not a number: '{raw}'");
                return fallback;
            }

            /// <summary>Magnitude from the column the mechanic points at; a trigger bundle's own value wins when present.</summary>
            public void AddFromColumn(Quantity q, ModOp op, ModTarget target, string column, ActionScope scope,
                When? when, string? scaleFrom, double? bundleValue, string mechanicId)
            {
                string raw = Use(column);
                double value;
                if (bundleValue.HasValue)
                {
                    value = bundleValue.Value;
                    if (raw.Length > 0 && TryNum(raw, out double col) && Math.Abs(col - value) > 1e-9)
                        Note(column, $"{mechanicId}: trigger value {value} overrides column value {col}");
                }
                else if (TryNum(raw, out double col))
                {
                    value = col;
                }
                else
                {
                    Loss(column, $"{mechanicId} points at {column} but it is {(raw.Length == 0 ? "empty" : $"'{raw}'")}");
                    return;
                }
                Modifiers.Add(new Modifier(q, op, value, target, scope, when, string.IsNullOrWhiteSpace(scaleFrom) ? null : scaleFrom));
            }

            public void Loss(string field, string message) => Issues.Add(new ConversionIssue(IssueSeverity.Loss, field, message));
            public void Note(string field, string message) => Issues.Add(new ConversionIssue(IssueSeverity.Note, field, message));

            public void ReportUnconsumed()
            {
                foreach (var (name, _) in StringColumns)
                {
                    if (_consumed.Contains(name) || IgnoredColumns.Contains(name))
                        continue;
                    string v = Peek(name);
                    if (v.Length > 0 && v != "0")
                        Loss(name, $"'{v}' isn't read by any mechanic pointer");
                }
            }
        }
    }
}
