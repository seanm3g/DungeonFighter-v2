using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.Media;
using RPGGame;
using RPGGame.Actions;
using RPGGame.Actions.Conditional;
using RPGGame.Data;
using RPGGame.Editors;
using RPGGame.UI.Avalonia.Resources;
using System;
using System.Collections.Generic;
using System.Linq;

namespace RPGGame.UI.Avalonia.Builders
{
    /// <summary>
    /// Builds individual form sections for the action editor. Split into partials for size.
    /// </summary>
    public partial class ActionFormSectionBuilders
    {
        private readonly ActionFormBuildContext _ctx;

        public ActionFormSectionBuilders(ActionFormBuildContext ctx)
        {
            _ctx = ctx;
        }

        public void BuildBasicSection(Panel parent, ActionData action)
        {
            var (section, stack) = _ctx.Factory.CreateFormSection("Basic Properties");
            parent.Children.Add(section);
            _ctx.Factory.AddFormField(stack, "Name", action.Name, (value) => action.Name = value, description: "e.g. Basic Attack or action id", onTextChanged: (value) => action.Name = value);
            _ctx.Factory.AddFormField(stack, "Description", action.Description, (value) => action.Description = value, isMultiline: true, description: "Optional flavor / design notes. Combat cards and strip hover list mechanical mods only.", onTextChanged: (value) => action.Description = value);

            var rarityOptions = new List<string> { ActionFormOptions.NoneOption };
            rarityOptions.AddRange(ActionFormOptions.RarityDropdownOptions.Skip(1));
            if (!string.IsNullOrWhiteSpace(action.Rarity) && !rarityOptions.Any(r => string.Equals(r, action.Rarity, StringComparison.OrdinalIgnoreCase)))
                rarityOptions.Add(action.Rarity);
            string rarityDisplay = string.IsNullOrWhiteSpace(action.Rarity) ? ActionFormOptions.NoneOption : action.Rarity;
            _ctx.Factory.AddFormField(stack, "Rarity", rarityDisplay, (value) => action.Rarity = (value == ActionFormOptions.NoneOption || string.IsNullOrWhiteSpace(value)) ? "" : value, rarityOptions.ToArray());

            var categoryOptions = new List<string> { ActionFormOptions.GeneralOption };
            categoryOptions.AddRange(ActionFormOptions.CategoryDropdownOptions);
            if (!string.IsNullOrWhiteSpace(action.Category) && !categoryOptions.Any(c => string.Equals(c, action.Category, StringComparison.OrdinalIgnoreCase)))
                categoryOptions.Add(action.Category);
            string categoryDisplay = string.IsNullOrWhiteSpace(action.Category) ? ActionFormOptions.GeneralOption : action.Category;
            _ctx.Factory.AddFormField(stack, "Category", categoryDisplay, (value) => action.Category = (value == ActionFormOptions.GeneralOption || string.IsNullOrWhiteSpace(value)) ? "" : value, categoryOptions.ToArray());

            var targetTypeOptions = new List<string>(ActionFormOptions.TargetTypeDropdownOptions);
            string targetTypeDisplay = string.IsNullOrWhiteSpace(action.TargetType) ? "SingleTarget" : action.TargetType;
            if (!targetTypeOptions.Any(t => string.Equals(t, targetTypeDisplay, StringComparison.OrdinalIgnoreCase)))
                targetTypeOptions.Add(targetTypeDisplay);
            _ctx.Factory.AddFormField(stack, "Target Type", targetTypeDisplay, (value) => action.TargetType = string.IsNullOrWhiteSpace(value) ? "SingleTarget" : value, targetTypeOptions.ToArray());

            void SetMultiHitCount(string value)
            {
                if (int.TryParse(value, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out int v) && v >= 1)
                    action.MultiHitCount = v;
            }
            void SetDamageMultiplier(string value)
            {
                if (double.TryParse(value, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out double v))
                    action.DamageMultiplier = v;
            }
            void SetSpeed(string value)
            {
                if (double.TryParse(value, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out double v))
                    action.Length = v;
            }
            _ctx.Factory.AddFormField(stack, "MultiHitCount", action.MultiHitCount.ToString(), SetMultiHitCount, description: "e.g. 1 (number of hits)", onTextChanged: SetMultiHitCount);
            _ctx.Factory.AddFormField(stack, "DamageMultiplier", action.DamageMultiplier.ToString(), SetDamageMultiplier, description: "e.g. 1.0", onTextChanged: SetDamageMultiplier);
            _ctx.Factory.AddFormField(stack, "Speed", action.Length.ToString(), SetSpeed, description: "e.g. 1.0 (action length)", onTextChanged: SetSpeed);

            AddActionAssignmentToStack(stack, action);
        }

        public void BuildTagsSection(Panel parent, ActionData action)
        {
            var (section, stack) = _ctx.Factory.CreateFormSection("Tags");
            parent.Children.Add(section);
            stack.Children.Add(new TextBlock
            {
                Text = "Separate multiple tags with a comma (e.g. melee, physical, starter).",
                FontSize = 12,
                Foreground = SettingsThemeBrushes.TextMuted,
                TextWrapping = TextWrapping.Wrap,
                Margin = new Thickness(0, 0, 0, 8)
            });
            string tagsValue = action.Tags != null ? string.Join(", ", action.Tags) : "";
            _ctx.Factory.AddFormField(stack, "Tags", tagsValue, (value) =>
            {
                if (string.IsNullOrWhiteSpace(value))
                    action.Tags = new List<string>();
                else
                    action.Tags = value.Split(',', StringSplitOptions.RemoveEmptyEntries)
                        .Select(t => t.Trim())
                        .Where(t => !string.IsNullOrWhiteSpace(t))
                        .ToList();
            }, description: "e.g. melee, physical, starter");
        }

        public void BuildSpreadsheetTypeSection(Panel parent, ActionData action)
        {
            var (section, stack) = _ctx.Factory.CreateFormSection("Type (Spreadsheet)");
            parent.Children.Add(section);
            var cadenceOptions = new List<string> { ActionFormOptions.NoneOption };
            cadenceOptions.AddRange(ActionFormOptions.CanonicalCadences);
            string cadenceDisplay = string.IsNullOrWhiteSpace(action.Cadence) ? ActionFormOptions.NoneOption : ActionFormOptions.NormalizeCadence(action.Cadence);
            if (!string.IsNullOrWhiteSpace(cadenceDisplay) && !cadenceOptions.Any(c => string.Equals(c, cadenceDisplay, StringComparison.OrdinalIgnoreCase)))
                cadenceOptions.Add(cadenceDisplay);
            _ctx.Factory.AddFormField(stack, "Cadence", cadenceDisplay, (value) => action.Cadence = (value == ActionFormOptions.NoneOption || string.IsNullOrWhiteSpace(value)) ? "" : value, cadenceOptions.ToArray());
            _ctx.Factory.AddFormField(stack, "Duration", action.ComboBonusDuration.ToString(), (value) => { if (int.TryParse(value, out int v) && v >= 0) { action.ComboBonusDuration = v; ActionCadenceDurationResolver.SyncBonusGroupCountsFromDuration(action); } }, description: "Cadence duration (sheet column K): how many TURN/ACTION applications, e.g. 2 for ACTION x2");

            var cadenceDesignNote = new TextBlock
            {
                FontSize = 12,
                Foreground = SettingsThemeBrushes.TextMuted,
                TextWrapping = TextWrapping.Wrap,
                Margin = new Thickness(0, 2, 0, 0)
            };

            void SyncCadenceDesignNote()
            {
                var normalized = ActionFormOptions.NormalizeCadence(action.Cadence);
                cadenceDesignNote.Text = GetCadenceDesignNote(normalized);
                cadenceDesignNote.IsVisible = !string.IsNullOrWhiteSpace(cadenceDesignNote.Text);
            }

            SyncCadenceDesignNote();
            if (_ctx.ActionFormControls.TryGetValue("Cadence", out var cadenceControl) && cadenceControl is ComboBox cadenceCombo)
            {
                cadenceCombo.SelectionChanged += (_, _) => SyncCadenceDesignNote();
            }

            stack.Children.Add(cadenceDesignNote);
        }

        /// <summary>Short in-editor explanation of cadence semantics (matches spreadsheet / combat design).</summary>
        private static string GetCadenceDesignNote(string normalizedCadence)
        {
            if (string.Equals(normalizedCadence, "Fight", StringComparison.OrdinalIgnoreCase))
            {
                return "Fight: benefits accumulate while you are engaged with this enemy — each time the action succeeds, its combo-style bonuses can apply again (e.g. Tension Hold adding its combo modifier on every trigger). Intended to last for that enemy and reset when it dies; temporary hero effects are also cleared when entering a new room.";
            }
            if (string.Equals(normalizedCadence, "Dungeon", StringComparison.OrdinalIgnoreCase))
            {
                return "Dungeon: long-lived bonuses meant to persist across fights in the same run until you leave the dungeon or temp effects are cleared at room boundaries.";
            }
            return "";
        }

        public void BuildModifiersSection(Panel parent, ActionData action)
        {
            // Editor UX: hero mods first (used more often); sheet columns AD–AG enemy / AJ–AM hero.
            BuildHeroModifiersSection(parent, action);
            BuildEnemyModifiersSection(parent, action);
        }

        private void BuildHeroModifiersSection(Panel parent, ActionData action)
        {
            var (section, stack) = _ctx.Factory.CreateFormSection("Hero base stats");
            parent.Children.Add(section);
            stack.Children.Add(new TextBlock
            {
                Text = "Spreadsheet row 1 block \"HERO BASE STATS\", columns AJ–AM: ACTION SPEED, ACTION DAMAGE, MULTIHIT MOD, AMP_MOD. Applies to the hero's next action or ability. Speed / damage / amp are %; multi-hit is a raw value.",
                FontSize = 12,
                Foreground = SettingsThemeBrushes.TextMuted,
                TextWrapping = TextWrapping.Wrap,
                Margin = new Thickness(0, 0, 0, 8)
            });
            _ctx.Factory.AddFormField(stack, "ACTION SPEED [hero] (%)", action.SpeedMod ?? "", (value) => action.SpeedMod = value ?? "", description: "Column AJ. e.g. 10 = 10% faster next hero action");
            _ctx.Factory.AddFormField(stack, "ACTION DAMAGE [hero] (%)", action.DamageMod ?? "", (value) => action.DamageMod = value ?? "", description: "Column AK. e.g. 10 for 10% more damage on next");
            _ctx.Factory.AddFormField(stack, "MULTIHIT MOD [hero]", action.MultiHitMod ?? "", (value) => action.MultiHitMod = value ?? "", description: "Column AL. e.g. 2 extra hits on next");
            _ctx.Factory.AddFormField(stack, "AMP_MOD [hero] (%)", action.AmpMod ?? "", (value) => action.AmpMod = value ?? "", description: "Column AM. e.g. 10 for 10% combo amp on next");
            _ctx.Factory.AddFormField(stack, "WEAPON SPEED [hero]", action.WeaponSpeedMod ?? "", (value) => action.WeaponSpeedMod = value ?? "", description: "HERO BASE → WEAPON SPEED. Flat points (each ≈ −0.1 weapon time). Cadence-scoped.");
            _ctx.Factory.AddFormField(stack, "WEAPON DAMAGE [hero]", action.WeaponDamageMod ?? "", (value) => action.WeaponDamageMod = value ?? "", description: "HERO BASE → WEAPON DAMAGE. Flat damage added to weapon. Cadence-scoped.");
        }

        private void BuildEnemyModifiersSection(Panel parent, ActionData action)
        {
            var (section, stack) = _ctx.Factory.CreateFormSection("Enemy base stats");
            parent.Children.Add(section);
            stack.Children.Add(new TextBlock
            {
                Text = "Spreadsheet row 1 block \"ENEMY BASE STATS\", columns AD–AG: ACTION SPEED, DAMAGE MOD, MULTIHIT MOD, AMP_MOD. Same units as hero (speed/damage/amp %, multi-hit raw).",
                FontSize = 12,
                Foreground = SettingsThemeBrushes.TextMuted,
                TextWrapping = TextWrapping.Wrap,
                Margin = new Thickness(0, 0, 0, 8)
            });
            _ctx.Factory.AddFormField(stack, "ACTION SPEED [enemy] (%)", action.EnemySpeedMod ?? "", (value) => action.EnemySpeedMod = value ?? "", description: "Column AD. e.g. 10 = 10% faster next enemy action");
            _ctx.Factory.AddFormField(stack, "DAMAGE MOD [enemy] (%)", action.EnemyDamageMod ?? "", (value) => action.EnemyDamageMod = value ?? "", description: "Column AE. e.g. 10 for 10% more damage on enemy next");
            _ctx.Factory.AddFormField(stack, "MULTIHIT MOD [enemy]", action.EnemyMultiHitMod ?? "", (value) => action.EnemyMultiHitMod = value ?? "", description: "Column AF. e.g. 2 extra hits on enemy next");
            _ctx.Factory.AddFormField(stack, "AMP_MOD [enemy] (%)", action.EnemyAmpMod ?? "", (value) => action.EnemyAmpMod = value ?? "", description: "Column AG. e.g. 10 for 10% combo amp on enemy next");
            _ctx.Factory.AddFormField(stack, "WEAPON SPEED [enemy]", action.EnemyWeaponSpeedMod ?? "", (value) => action.EnemyWeaponSpeedMod = value ?? "", description: "ENEMY BASE → WEAPON SPEED. Flat points; cadence-scoped.");
            _ctx.Factory.AddFormField(stack, "WEAPON DAMAGE [enemy]", action.EnemyWeaponDamageMod ?? "", (value) => action.EnemyWeaponDamageMod = value ?? "", description: "ENEMY BASE → WEAPON DAMAGE. Flat damage; cadence-scoped.");
        }

        public void BuildComboAndPositionSection(Panel parent, ActionData action)
        {
            var (section, stack) = _ctx.Factory.CreateFormSection("Combo & Position Modifications");
            parent.Children.Add(section);
            _ctx.Factory.AddFormField(stack, "Chain Position", action.ChainPosition ?? "", (value) => action.ChainPosition = value ?? "", description: "e.g. First, Last");
            _ctx.Factory.AddFormField(stack, "Chain Position Number", action.ComboOrder.ToString(), (value) => { if (int.TryParse(value, out int v) && v >= -1) action.ComboOrder = v; }, description: "e.g. 0 or 1");
            _ctx.Factory.AddBooleanField(stack, "Chain Position MOD", !string.IsNullOrWhiteSpace(action.ModifyBasedOnChainPosition), (value) =>
            {
                action.ModifyBasedOnChainPosition = value ? "true" : "";
                action.NormalizeChainPositionBonuses();
            });
            _ctx.Factory.AddBooleanField(stack, "Skip Next Turn", action.SkipNextTurn, (value) => action.SkipNextTurn = value);
            _ctx.Factory.AddBooleanField(stack, "Repeat", action.RepeatLastAction, (value) => action.RepeatLastAction = value);
            _ctx.Factory.AddFormField(stack, "Jump", action.Jump ?? "", (value) => action.Jump = value ?? "", description: "e.g. 2 (jump steps in chain)");
            _ctx.Factory.AddFormField(stack, "Jump (+slots)", action.JumpRelative ?? "", (value) => action.JumpRelative = value ?? "", description: "Extra slots after the usual next step (e.g. combo position 2 → +1 → position 4). Empty or 0 disables. If Jump is set, it wins and this is ignored.");
            _ctx.Factory.AddFormField(stack, "Size (Chain Length)", action.ChainLength ?? "", (value) => action.ChainLength = value ?? "", description: "e.g. 3");
            _ctx.Factory.AddBooleanField(stack, "Reset", !string.IsNullOrWhiteSpace(action.Reset), (value) => action.Reset = value ? "true" : "");
            stack.Children.Add(new TextBlock
            {
                Text = "Opener: first slot only; Finisher: last slot only.",
                FontSize = 12,
                Foreground = SettingsThemeBrushes.TextMuted,
                TextWrapping = TextWrapping.Wrap,
                Margin = new Thickness(0, 0, 0, 4)
            });
            _ctx.Factory.AddBooleanField(stack, "Opener", action.IsOpener, (value) => action.IsOpener = value);
            _ctx.Factory.AddBooleanField(stack, "Finisher", action.IsFinisher, (value) => action.IsFinisher = value);
            stack.Children.Add(new TextBlock
            {
                Text = "Reserve Pool: excluded from default weighted rolls; still usable on the combo strip / explicit picks.",
                FontSize = 12,
                Foreground = SettingsThemeBrushes.TextMuted,
                TextWrapping = TextWrapping.Wrap,
                Margin = new Thickness(0, 0, 0, 4)
            });
            _ctx.Factory.AddBooleanField(stack, "Reserve Pool", action.IsReservePool, (value) =>
            {
                action.IsReservePool = value;
                ActionTagSyncHelper.SyncCanonicalTags(action);
            });
        }

        public void BuildRollBonusesSection(Panel parent, ActionData action)
        {
            var (section, stack) = _ctx.Factory.CreateFormSection("Roll Bonuses — Hero (including Accuracy)");
            parent.Children.Add(section);
            _ctx.Factory.AddFormField(stack, "Roll bonus: Crit Miss", action.CriticalMissThresholdAdjustment.ToString(), (value) => { if (int.TryParse(value, out int v)) action.CriticalMissThresholdAdjustment = v; }, description: "e.g. -2 (integer threshold adjustment)");
            _ctx.Factory.AddFormField(stack, "Roll bonus: Hit", action.HitThresholdAdjustment.ToString(), (value) => { if (int.TryParse(value, out int v)) action.HitThresholdAdjustment = v; }, description: "e.g. 0 (integer)");
            _ctx.Factory.AddFormField(stack, "Roll bonus: Combo", action.ComboThresholdAdjustment.ToString(), (value) => { if (int.TryParse(value, out int v)) action.ComboThresholdAdjustment = v; }, description: "e.g. 0 (integer)");
            _ctx.Factory.AddFormField(stack, "Roll bonus: Crit", action.CriticalHitThresholdAdjustment.ToString(), (value) => { if (int.TryParse(value, out int v)) action.CriticalHitThresholdAdjustment = v; }, description: "e.g. 2 (integer)");
            void SetRollBonus(string value) { action.RollBonus = string.IsNullOrWhiteSpace(value) ? 0 : (int.TryParse(value, out int v) ? v : action.RollBonus); }
            _ctx.Factory.AddFormField(stack, "Accuracy", action.RollBonus.ToString(), SetRollBonus, description: "e.g. 0 (integer)", onTextChanged: SetRollBonus);
        }

        public void BuildEnemyRollBonusesSection(Panel parent, ActionData action)
        {
            var (section, stack) = _ctx.Factory.CreateFormSection("Roll Bonuses — Enemy (including Accuracy)");
            parent.Children.Add(section);
            _ctx.Factory.AddFormField(stack, "Enemy roll bonus: Crit Miss", action.EnemyCriticalMissThresholdAdjustment.ToString(), (value) => { if (int.TryParse(value, out int v)) action.EnemyCriticalMissThresholdAdjustment = v; }, description: "e.g. -2 (integer threshold adjustment)");
            _ctx.Factory.AddFormField(stack, "Enemy roll bonus: Hit", action.EnemyHitThresholdAdjustment.ToString(), (value) => { if (int.TryParse(value, out int v)) action.EnemyHitThresholdAdjustment = v; }, description: "e.g. 0 (integer)");
            _ctx.Factory.AddFormField(stack, "Enemy roll bonus: Combo", action.EnemyComboThresholdAdjustment.ToString(), (value) => { if (int.TryParse(value, out int v)) action.EnemyComboThresholdAdjustment = v; }, description: "e.g. 0 (integer)");
            _ctx.Factory.AddFormField(stack, "Enemy roll bonus: Crit", action.EnemyCriticalHitThresholdAdjustment.ToString(), (value) => { if (int.TryParse(value, out int v)) action.EnemyCriticalHitThresholdAdjustment = v; }, description: "e.g. 2 (integer)");
            void SetEnemyRollBonus(string value) { action.EnemyRollBonus = string.IsNullOrWhiteSpace(value) ? 0 : (int.TryParse(value, out int v) ? v : action.EnemyRollBonus); }
            _ctx.Factory.AddFormField(stack, "Enemy Accuracy", action.EnemyRollBonus.ToString(), SetEnemyRollBonus, description: "e.g. 0 (integer)", onTextChanged: SetEnemyRollBonus);
        }

        private static bool HasTrigger(ActionData action, string condition)
        {
            return action.TriggerConditions != null && action.TriggerConditions.Any(c => string.Equals(c, condition, StringComparison.OrdinalIgnoreCase));
        }

        private static void SetTrigger(ActionData action, string condition, bool on)
        {
            action.TriggerConditions ??= new List<string>();
            var existing = action.TriggerConditions.FirstOrDefault(c => string.Equals(c, condition, StringComparison.OrdinalIgnoreCase));
            if (on && existing == null)
                action.TriggerConditions.Add(condition);
            if (!on && existing != null)
                action.TriggerConditions.RemoveAll(c => string.Equals(c, condition, StringComparison.OrdinalIgnoreCase));
        }

        public void BuildTriggersSection(Panel parent, ActionData action)
        {
            var (section, stack) = _ctx.Factory.CreateFormSection("Triggers (apply effects only when)");
            parent.Children.Add(section);
            stack.Children.Add(new TextBlock
            {
                Text = "When checked, this action's status effects apply only on the selected outcome(s). Leave all unchecked to apply on any successful hit (not miss/kill). Filters (On Wield, Clutch, DoT, tags, …) AND with outcomes; filter-only ⇒ any hit. Chain punctuation = On Combo End.",
                FontSize = 12,
                Foreground = SettingsThemeBrushes.TextMuted,
                TextWrapping = TextWrapping.Wrap,
                Margin = new Thickness(0, 0, 0, 8)
            });
            _ctx.Factory.AddBooleanField(stack, "On Hit (normal)", HasTrigger(action, "ONHIT"), (value) => SetTrigger(action, "ONHIT", value));
            _ctx.Factory.AddBooleanField(stack, "On Connect (any hit)", HasTrigger(action, "ONCONNECT"), (value) => SetTrigger(action, "ONCONNECT", value));
            _ctx.Factory.AddBooleanField(stack, "On Miss", HasTrigger(action, "ONMISS"), (value) => SetTrigger(action, "ONMISS", value));
            _ctx.Factory.AddBooleanField(stack, "On Crit Miss", HasTrigger(action, "ONCRITICALMISS"), (value) => SetTrigger(action, "ONCRITICALMISS", value));
            _ctx.Factory.AddBooleanField(stack, "On Combo", HasTrigger(action, "ONCOMBO"), (value) => SetTrigger(action, "ONCOMBO", value));
            _ctx.Factory.AddBooleanField(stack, "On Combo End", HasTrigger(action, "ONCOMBOEND"), (value) => SetTrigger(action, "ONCOMBOEND", value));
            _ctx.Factory.AddBooleanField(stack, "On Rooms Cleared", HasTrigger(action, "ONROOMSCLEARED") || action.RoomsClearedTriggerValue > 0,
                (value) =>
                {
                    SetTrigger(action, "ONROOMSCLEARED", value);
                    if (!value) action.RoomsClearedTriggerValue = 0;
                });
            void SetRoomsClearedN(string value)
            {
                if (string.IsNullOrWhiteSpace(value))
                {
                    action.RoomsClearedTriggerValue = 0;
                    return;
                }
                if (int.TryParse(value.Trim(), out int n) && n > 0)
                {
                    action.RoomsClearedTriggerValue = n;
                    SetTrigger(action, "ONROOMSCLEARED", true);
                }
            }
            _ctx.Factory.AddFormField(stack, "Rooms cleared count (optional N)", action.RoomsClearedTriggerValue > 0 ? action.RoomsClearedTriggerValue.ToString() : "",
                SetRoomsClearedN, description: "Blank = every room clear; set N to fire only on the Nth clear.", onTextChanged: SetRoomsClearedN);
            _ctx.Factory.AddBooleanField(stack, "On Crit", HasTrigger(action, "ONCRITICAL"), (value) => SetTrigger(action, "ONCRITICAL", value));
            _ctx.Factory.AddBooleanField(stack, "On Kill", HasTrigger(action, "ONKILL"), (value) => SetTrigger(action, "ONKILL", value));
            _ctx.Factory.AddBooleanField(stack, "On Health Threshold", HasTrigger(action, "ONHEALTHTHRESHOLD"), (value) => SetTrigger(action, "ONHEALTHTHRESHOLD", value));
            _ctx.Factory.AddBooleanField(stack, "On Exact Roll", HasTrigger(action, "ONROLLVALUE") || action.ExactRollTriggerValue > 0,
                (value) =>
                {
                    SetTrigger(action, "ONROLLVALUE", value);
                    if (!value) action.ExactRollTriggerValue = 0;
                });
            void SetExactRoll(string value)
            {
                if (string.IsNullOrWhiteSpace(value))
                {
                    action.ExactRollTriggerValue = 0;
                    return;
                }
                if (int.TryParse(value.Trim(), out int face) && face >= 1 && face <= 20)
                {
                    action.ExactRollTriggerValue = face;
                    SetTrigger(action, "ONROLLVALUE", true);
                }
            }
            _ctx.Factory.AddFormField(stack, "Exact roll face (1-20)", action.ExactRollTriggerValue > 0 ? action.ExactRollTriggerValue.ToString() : "",
                SetExactRoll, description: "When set, status effects also apply when the attack roll equals this face.", onTextChanged: SetExactRoll);
            _ctx.Factory.AddBooleanField(stack, "On Wield (Sword)", HasTrigger(action, "ONWIELD:Sword"), (value) => SetTrigger(action, "ONWIELD:Sword", value));
            _ctx.Factory.AddBooleanField(stack, "On Wield (Dagger)", HasTrigger(action, "ONWIELD:Dagger"), (value) => SetTrigger(action, "ONWIELD:Dagger", value));
            _ctx.Factory.AddBooleanField(stack, "On Wield (Mace)", HasTrigger(action, "ONWIELD:Mace"), (value) => SetTrigger(action, "ONWIELD:Mace", value));
            _ctx.Factory.AddBooleanField(stack, "On Wield (Wand)", HasTrigger(action, "ONWIELD:Wand"), (value) => SetTrigger(action, "ONWIELD:Wand", value));
            _ctx.Factory.AddBooleanField(stack, "On First Blood", HasTrigger(action, "ONFIRSTHIT"), (value) => SetTrigger(action, "ONFIRSTHIT", value));
            _ctx.Factory.AddBooleanField(stack, "After Miss", HasTrigger(action, "ONAFTERMISS"), (value) => SetTrigger(action, "ONAFTERMISS", value));
            _ctx.Factory.AddBooleanField(stack, "Clutch (HP ≤ 25%)", HasTrigger(action, "IFCLUTCH"), (value) => SetTrigger(action, "IFCLUTCH", value));
            _ctx.Factory.AddBooleanField(stack, "Same Action (Mirror)", HasTrigger(action, "IFSAMESACTION"), (value) => SetTrigger(action, "IFSAMESACTION", value));
            _ctx.Factory.AddBooleanField(stack, "Different Action (Switch-up)", HasTrigger(action, "IFDIFFERENTACTION"), (value) => SetTrigger(action, "IFDIFFERENTACTION", value));
            _ctx.Factory.AddBooleanField(stack, "Last Enemy (Last Stand)", HasTrigger(action, "IFLASTENEMY"), (value) => SetTrigger(action, "IFLASTENEMY", value));
            _ctx.Factory.AddBooleanField(stack, "Source Under DoT", HasTrigger(action, "IFSOURCEUNDERDOT"), (value) => SetTrigger(action, "IFSOURCEUNDERDOT", value));
            _ctx.Factory.AddBooleanField(stack, "Target Under DoT", HasTrigger(action, "IFTARGETUNDERDOT"), (value) => SetTrigger(action, "IFTARGETUNDERDOT", value));
            void SetPrefixedTrigger(string prefix, string value)
            {
                action.TriggerConditions ??= new List<string>();
                action.TriggerConditions.RemoveAll(c => c.StartsWith(prefix, StringComparison.OrdinalIgnoreCase));
                if (string.IsNullOrWhiteSpace(value))
                    return;
                foreach (var part in value.Split(new[] { ',', ';' }, StringSplitOptions.RemoveEmptyEntries))
                {
                    string arg = part.Trim();
                    if (arg.Length == 0) continue;
                    string token = $"{prefix}:{arg}";
                    var parsed = ActionTriggerGate.ParseTriggerConditionList(token);
                    foreach (var p in parsed)
                        if (!action.TriggerConditions.Exists(c => string.Equals(c, p, StringComparison.OrdinalIgnoreCase)))
                            action.TriggerConditions.Add(p);
                }
            }
            string JoinPrefixed(string prefix) =>
                action.TriggerConditions == null
                    ? ""
                    : string.Join(", ", action.TriggerConditions
                        .Where(c => c.StartsWith(prefix + ":", StringComparison.OrdinalIgnoreCase))
                        .Select(c => c.Substring(prefix.Length + 1)));
            _ctx.Factory.AddFormField(stack, "Source status (poison,burn,…)", JoinPrefixed("IFSOURCESTATUS"),
                v => SetPrefixedTrigger("IFSOURCESTATUS", v), description: "Comma-separated; OR within list. Also AND with outcomes / other filters.", onTextChanged: v => SetPrefixedTrigger("IFSOURCESTATUS", v));
            _ctx.Factory.AddFormField(stack, "Target status (poison,burn,…)", JoinPrefixed("IFTARGETSTATUS"),
                v => SetPrefixedTrigger("IFTARGETSTATUS", v), description: "Comma-separated statuses required on the target (OR).", onTextChanged: v => SetPrefixedTrigger("IFTARGETSTATUS", v));
            _ctx.Factory.AddFormField(stack, "Action tag required", JoinPrefixed("IFACTIONHASTAG"),
                v => SetPrefixedTrigger("IFACTIONHASTAG", v), description: "Tag synergy: action must have this tag.", onTextChanged: v => SetPrefixedTrigger("IFACTIONHASTAG", v));
            _ctx.Factory.AddFormField(stack, "Gear tag required", JoinPrefixed("IFGEARHASTAG"),
                v => SetPrefixedTrigger("IFGEARHASTAG", v), description: "Tag synergy: equipped gear must have this tag.", onTextChanged: v => SetPrefixedTrigger("IFGEARHASTAG", v));
            _ctx.Factory.AddFormField(stack, "Target tag required", JoinPrefixed("IFTARGETHASTAG"),
                v => SetPrefixedTrigger("IFTARGETHASTAG", v), description: "Tag synergy: enemy tags must include this.", onTextChanged: v => SetPrefixedTrigger("IFTARGETHASTAG", v));
            _ctx.Factory.AddFormField(stack, "Source HP at/below (0-1 or %)",
                JoinPrefixed("IFSOURCEHEALTHBELOW"),
                v => SetPrefixedTrigger("IFSOURCEHEALTHBELOW", v),
                description: "e.g. 0.25 or 25 for clutch-style gating (in addition to Clutch checkbox).",
                onTextChanged: v => SetPrefixedTrigger("IFSOURCEHEALTHBELOW", v));
        }

        public void BuildStatusSection(Panel parent, ActionData action)
        {
            var (section, stack) = _ctx.Factory.CreateFormSection("Status Effects");
            parent.Children.Add(section);
            stack.Children.Add(new TextBlock
            {
                Text = "This action adds the selected status component(s). Effect strength and duration are determined by the item granting the action.",
                FontSize = 12,
                Foreground = SettingsThemeBrushes.TextMuted,
                TextWrapping = TextWrapping.Wrap,
                Margin = new Thickness(0, 0, 0, 8)
            });
            _ctx.Factory.AddBooleanField(stack, "CausesStun", action.CausesStun, (value) => action.CausesStun = value);
            _ctx.Factory.AddBooleanField(stack, "CausesPoison", action.CausesPoison, (value) => action.CausesPoison = value);
            _ctx.Factory.AddBooleanField(stack, "CausesBurn", action.CausesBurn, (value) => action.CausesBurn = value);
            _ctx.Factory.AddBooleanField(stack, "CausesAcid", action.CausesAcid, (value) => action.CausesAcid = value);
            _ctx.Factory.AddBooleanField(stack, "CausesBleed", action.CausesBleed, (value) => action.CausesBleed = value);
            _ctx.Factory.AddBooleanField(stack, "CausesWeaken", action.CausesWeaken, (value) => action.CausesWeaken = value);
            _ctx.Factory.AddBooleanField(stack, "CausesExpose", action.CausesExpose, (value) => action.CausesExpose = value);
            _ctx.Factory.AddBooleanField(stack, "CausesSlow", action.CausesSlow, (value) => action.CausesSlow = value);
            _ctx.Factory.AddBooleanField(stack, "CausesVulnerability", action.CausesVulnerability, (value) => action.CausesVulnerability = value);
            _ctx.Factory.AddBooleanField(stack, "CausesHarden", action.CausesHarden, (value) => action.CausesHarden = value);
            _ctx.Factory.AddBooleanField(stack, "CausesSilence", action.CausesSilence, (value) => action.CausesSilence = value);
            _ctx.Factory.AddBooleanField(stack, "CausesPierce", action.CausesPierce, (value) => action.CausesPierce = value);
            _ctx.Factory.AddBooleanField(stack, "CausesStatDrain", action.CausesStatDrain, (value) => action.CausesStatDrain = value);
            _ctx.Factory.AddBooleanField(stack, "CausesFocus", action.CausesFocus, (value) => action.CausesFocus = value);
        }

        private void AddActionAssignmentToStack(StackPanel stack, ActionData action)
        {
            bool defaultOrStarting = action.IsDefaultAction || action.IsStartingAction;
            _ctx.Factory.AddBooleanField(stack, "Default/Starting", defaultOrStarting, (value) =>
            {
                action.IsDefaultAction = value;
                action.IsStartingAction = value;
                if (value && action.WeaponTypes != null)
                {
                    action.WeaponTypes.Clear();
                    UpdateWeaponTypeCheckboxes(action);
                }
                else if (!value)
                {
                    UpdateDefaultActionCheckbox(action);
                }
            }, "DefaultStartingCheckBox");

            _ctx.Factory.AddBooleanField(stack, "Required Weapon Basic", HasTag(action, WeaponRequiredComboAction.WeaponBasicTag), (value) =>
            {
                SetTag(action, WeaponRequiredComboAction.WeaponBasicTag, value);
            });

            stack.Children.Add(new TextBlock
            {
                Text = "Weapon type checks grant this action to every weapon of that type. Required Weapon Basic marks the one action that must stay in that weapon's combo sequence.",
                FontSize = 12,
                Foreground = SettingsThemeBrushes.TextMuted,
                TextWrapping = TextWrapping.Wrap,
                Margin = new Thickness(0, 0, 0, 4)
            });

            var weaponTypesLabel = new TextBlock
            {
                Text = "Assign to Weapon Types:",
                FontSize = 15,
                Foreground = new SolidColorBrush(Colors.White),
                Margin = new Thickness(0, 10, 0, 5)
            };
            stack.Children.Add(weaponTypesLabel);

            var weaponTypesGrid = new Grid();
            weaponTypesGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            weaponTypesGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            weaponTypesGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            weaponTypesGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

            var weaponTypes = new[] { "Sword", "Dagger", "Mace", "Wand" };
            action.WeaponTypes ??= new List<string>();

            int column = 0;
            int row = 0;
            foreach (var weaponType in weaponTypes)
            {
                if (column >= 4)
                {
                    column = 0;
                    row++;
                    weaponTypesGrid.RowDefinitions.Add(new RowDefinition());
                }

                var checkBox = new CheckBox
                {
                    Content = weaponType,
                    IsChecked = action.WeaponTypes.Contains(weaponType),
                    FontSize = 14,
                    VerticalAlignment = VerticalAlignment.Center,
                    IsEnabled = !action.IsDefaultAction,
                    Foreground = new SolidColorBrush(Colors.White),
                    Background = new SolidColorBrush(Color.FromRgb(40, 40, 60))
                };

                checkBox.IsCheckedChanged += (s, e) =>
                {
                    if (checkBox.IsChecked.HasValue)
                    {
                        if (checkBox.IsChecked.Value)
                        {
                            if (!action.WeaponTypes!.Contains(weaponType))
                            {
                                action.WeaponTypes.Add(weaponType);
                                if (action.IsDefaultAction)
                                {
                                    action.IsDefaultAction = false;
                                    action.IsStartingAction = false;
                                    UpdateDefaultActionCheckbox(action);
                                }
                            }
                        }
                        else
                        {
                            action.WeaponTypes.Remove(weaponType);
                        }
                    }
                };

                Grid.SetColumn(checkBox, column);
                Grid.SetRow(checkBox, row);
                weaponTypesGrid.Children.Add(checkBox);
                _ctx.ActionFormControls[$"WeaponType_{weaponType}"] = checkBox;
                column++;
            }

            while (weaponTypesGrid.RowDefinitions.Count <= row)
                weaponTypesGrid.RowDefinitions.Add(new RowDefinition());

            stack.Children.Add(weaponTypesGrid);
        }

        private void UpdateWeaponTypeCheckboxes(ActionData action)
        {
            var weaponTypes = new[] { "Sword", "Dagger", "Mace", "Wand" };
            foreach (var weaponType in weaponTypes)
            {
                if (_ctx.ActionFormControls.TryGetValue($"WeaponType_{weaponType}", out var control) && control is CheckBox checkBox)
                {
                    checkBox.IsChecked = false;
                    checkBox.IsEnabled = false;
                }
            }
        }

        private void UpdateDefaultActionCheckbox(ActionData action)
        {
            if (_ctx.ActionFormControls.TryGetValue("Default/Starting", out var control) && control is CheckBox checkBox)
                checkBox.IsChecked = action.IsDefaultAction;

            var weaponTypes = new[] { "Sword", "Dagger", "Mace", "Wand" };
            foreach (var weaponType in weaponTypes)
            {
                if (_ctx.ActionFormControls.TryGetValue($"WeaponType_{weaponType}", out var weaponControl) && weaponControl is CheckBox weaponCheckBox)
                {
                    weaponCheckBox.IsEnabled = true;
                    weaponCheckBox.IsChecked = action.WeaponTypes?.Contains(weaponType) ?? false;
                }
            }
        }

        private static bool HasTag(ActionData action, string tag)
        {
            return action.Tags != null && action.Tags.Any(t => string.Equals(t, tag, StringComparison.OrdinalIgnoreCase));
        }

        private static void SetTag(ActionData action, string tag, bool enabled)
        {
            action.Tags ??= new List<string>();
            action.Tags.RemoveAll(t => string.Equals(t, tag, StringComparison.OrdinalIgnoreCase));
            if (enabled)
                action.Tags.Add(tag);
        }
    }
}
