using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.Media;
using RPGGame;
using RPGGame.Editors;
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
            _ctx.Factory.AddFormField(stack, "Name", action.Name, (value) => action.Name = value, description: "e.g. Basic Attack or action id");
            _ctx.Factory.AddFormField(stack, "Description", action.Description, (value) => action.Description = value, isMultiline: true, description: "Flavor text shown in combat.");

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

            _ctx.Factory.AddFormField(stack, "MultiHitCount", action.MultiHitCount.ToString(), (value) => { if (int.TryParse(value, out int v) && v >= 1) action.MultiHitCount = v; }, description: "e.g. 1 (number of hits)");
            _ctx.Factory.AddFormField(stack, "DamageMultiplier", action.DamageMultiplier.ToString(), (value) => { if (double.TryParse(value, out double v)) action.DamageMultiplier = v; }, description: "e.g. 1.0");
            _ctx.Factory.AddFormField(stack, "Speed", action.Length.ToString(), (value) => { if (double.TryParse(value, out double v)) action.Length = v; }, description: "e.g. 1.0 (action length)");
            _ctx.Factory.AddFormField(stack, "Cooldown", action.Cooldown.ToString(), (value) => { if (int.TryParse(value, out int v) && v >= 0) action.Cooldown = v; }, description: "e.g. 0 (turns before reuse)");

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
                Foreground = new SolidColorBrush(Color.FromRgb(200, 200, 200)),
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
            _ctx.Factory.AddFormField(stack, "Duration", action.ComboBonusDuration.ToString(), (value) => { if (int.TryParse(value, out int v) && v >= 0) action.ComboBonusDuration = v; }, description: "e.g. 3 (combo bonus duration)");
        }

        public void BuildModifiersSection(Panel parent, ActionData action)
        {
            var (section, stack) = _ctx.Factory.CreateFormSection("Modifiers");
            parent.Children.Add(section);
            stack.Children.Add(new TextBlock
            {
                Text = "Apply to the next action or ability. Speed/Damage/Amp are %; Multi-hit is a raw value.",
                FontSize = 12,
                Foreground = new SolidColorBrush(Color.FromRgb(200, 200, 200)),
                TextWrapping = TextWrapping.Wrap,
                Margin = new Thickness(0, 0, 0, 8)
            });
            _ctx.Factory.AddFormField(stack, "SpeedMod (%)", action.SpeedMod ?? "", (value) => action.SpeedMod = value ?? "", description: "e.g. 10 = faster (10% shorter time)");
            _ctx.Factory.AddFormField(stack, "DamageMod (%)", action.DamageMod ?? "", (value) => action.DamageMod = value ?? "", description: "e.g. 10 for 10% more damage on next");
            _ctx.Factory.AddFormField(stack, "MultiHitMod (value)", action.MultiHitMod ?? "", (value) => action.MultiHitMod = value ?? "", description: "e.g. 2 extra hits on next");
            _ctx.Factory.AddFormField(stack, "Amp Mod (%)", action.AmpMod ?? "", (value) => action.AmpMod = value ?? "", description: "e.g. 10 for 10% combo amp on next");
        }

        public void BuildComboAndPositionSection(Panel parent, ActionData action)
        {
            var (section, stack) = _ctx.Factory.CreateFormSection("Combo & Position Modifications");
            parent.Children.Add(section);
            _ctx.Factory.AddFormField(stack, "Chain Position", action.ChainPosition ?? "", (value) => action.ChainPosition = value ?? "", description: "e.g. First, Last");
            _ctx.Factory.AddFormField(stack, "Chain Position Number", action.ComboOrder.ToString(), (value) => { if (int.TryParse(value, out int v) && v >= -1) action.ComboOrder = v; }, description: "e.g. 0 or 1");
            _ctx.Factory.AddBooleanField(stack, "Chain Position MOD", !string.IsNullOrWhiteSpace(action.ModifyBasedOnChainPosition), (value) => action.ModifyBasedOnChainPosition = value ? "true" : "");
            _ctx.Factory.AddBooleanField(stack, "Skip Next Turn", action.SkipNextTurn, (value) => action.SkipNextTurn = value);
            _ctx.Factory.AddBooleanField(stack, "Repeat", action.RepeatLastAction, (value) => action.RepeatLastAction = value);
            _ctx.Factory.AddFormField(stack, "Jump", action.Jump ?? "", (value) => action.Jump = value ?? "", description: "e.g. 2 (jump steps in chain)");
            _ctx.Factory.AddFormField(stack, "Size (Chain Length)", action.ChainLength ?? "", (value) => action.ChainLength = value ?? "", description: "e.g. 3");
            _ctx.Factory.AddBooleanField(stack, "Reset", !string.IsNullOrWhiteSpace(action.Reset), (value) => action.Reset = value ? "true" : "");
            stack.Children.Add(new TextBlock
            {
                Text = "Opener: first slot only; Finisher: last slot only.",
                FontSize = 12,
                Foreground = new SolidColorBrush(Color.FromRgb(200, 200, 200)),
                TextWrapping = TextWrapping.Wrap,
                Margin = new Thickness(0, 0, 0, 4)
            });
            _ctx.Factory.AddBooleanField(stack, "Opener", action.IsOpener, (value) => action.IsOpener = value);
            _ctx.Factory.AddBooleanField(stack, "Finisher", action.IsFinisher, (value) => action.IsFinisher = value);
        }

        public void BuildRollBonusesSection(Panel parent, ActionData action)
        {
            var (section, stack) = _ctx.Factory.CreateFormSection("Roll Bonuses (including Accuracy)");
            parent.Children.Add(section);
            _ctx.Factory.AddFormField(stack, "Roll bonus: Crit Miss", action.CriticalMissThresholdAdjustment.ToString(), (value) => { if (int.TryParse(value, out int v)) action.CriticalMissThresholdAdjustment = v; }, description: "e.g. -2 (integer threshold adjustment)");
            _ctx.Factory.AddFormField(stack, "Roll bonus: Hit", action.HitThresholdAdjustment.ToString(), (value) => { if (int.TryParse(value, out int v)) action.HitThresholdAdjustment = v; }, description: "e.g. 0 (integer)");
            _ctx.Factory.AddFormField(stack, "Roll bonus: Combo", action.ComboThresholdAdjustment.ToString(), (value) => { if (int.TryParse(value, out int v)) action.ComboThresholdAdjustment = v; }, description: "e.g. 0 (integer)");
            _ctx.Factory.AddFormField(stack, "Roll bonus: Crit", action.CriticalHitThresholdAdjustment.ToString(), (value) => { if (int.TryParse(value, out int v)) action.CriticalHitThresholdAdjustment = v; }, description: "e.g. 2 (integer)");
            _ctx.Factory.AddFormField(stack, "Accuracy", action.RollBonus.ToString(), (value) => { if (int.TryParse(value, out int v)) action.RollBonus = v; }, description: "e.g. 0 (integer)");
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
                Text = "When checked, this action's status effects apply only on the selected outcome(s). Leave all unchecked to apply on any result.",
                FontSize = 12,
                Foreground = new SolidColorBrush(Color.FromRgb(180, 180, 200)),
                TextWrapping = TextWrapping.Wrap,
                Margin = new Thickness(0, 0, 0, 8)
            });
            _ctx.Factory.AddBooleanField(stack, "On Hit", HasTrigger(action, "ONHIT"), (value) => SetTrigger(action, "ONHIT", value));
            _ctx.Factory.AddBooleanField(stack, "On Miss", HasTrigger(action, "ONMISS"), (value) => SetTrigger(action, "ONMISS", value));
            _ctx.Factory.AddBooleanField(stack, "On Combo", HasTrigger(action, "ONCOMBO"), (value) => SetTrigger(action, "ONCOMBO", value));
            _ctx.Factory.AddBooleanField(stack, "On Crit", HasTrigger(action, "ONCRITICAL"), (value) => SetTrigger(action, "ONCRITICAL", value));
        }

        public void BuildStatusSection(Panel parent, ActionData action)
        {
            var (section, stack) = _ctx.Factory.CreateFormSection("Status Effects");
            parent.Children.Add(section);
            stack.Children.Add(new TextBlock
            {
                Text = "This action adds the selected status component(s). Effect strength and duration are determined by the item granting the action.",
                FontSize = 12,
                Foreground = new SolidColorBrush(Color.FromRgb(180, 180, 200)),
                TextWrapping = TextWrapping.Wrap,
                Margin = new Thickness(0, 0, 0, 8)
            });
            _ctx.Factory.AddBooleanField(stack, "CausesStun", action.CausesStun, (value) => action.CausesStun = value);
            _ctx.Factory.AddBooleanField(stack, "CausesPoison", action.CausesPoison, (value) => action.CausesPoison = value);
            _ctx.Factory.AddBooleanField(stack, "CausesBurn", action.CausesBurn, (value) => action.CausesBurn = value);
            _ctx.Factory.AddBooleanField(stack, "CausesBleed", action.CausesBleed, (value) => action.CausesBleed = value);
            _ctx.Factory.AddBooleanField(stack, "CausesWeaken", action.CausesWeaken, (value) => action.CausesWeaken = value);
            _ctx.Factory.AddBooleanField(stack, "CausesExpose", action.CausesExpose, (value) => action.CausesExpose = value);
            _ctx.Factory.AddBooleanField(stack, "CausesSlow", action.CausesSlow, (value) => action.CausesSlow = value);
            _ctx.Factory.AddBooleanField(stack, "CausesVulnerability", action.CausesVulnerability, (value) => action.CausesVulnerability = value);
            _ctx.Factory.AddBooleanField(stack, "CausesHarden", action.CausesHarden, (value) => action.CausesHarden = value);
            _ctx.Factory.AddBooleanField(stack, "CausesSilence", action.CausesSilence, (value) => action.CausesSilence = value);
            _ctx.Factory.AddBooleanField(stack, "CausesPierce", action.CausesPierce, (value) => action.CausesPierce = value);
            _ctx.Factory.AddBooleanField(stack, "CausesStatDrain", action.CausesStatDrain, (value) => action.CausesStatDrain = value);
            _ctx.Factory.AddBooleanField(stack, "CausesFortify", action.CausesFortify, (value) => action.CausesFortify = value);
            _ctx.Factory.AddBooleanField(stack, "CausesFocus", action.CausesFocus, (value) => action.CausesFocus = value);
            _ctx.Factory.AddBooleanField(stack, "CausesCleanse", action.CausesCleanse, (value) => action.CausesCleanse = value);
            _ctx.Factory.AddBooleanField(stack, "CausesReflect", action.CausesReflect, (value) => action.CausesReflect = value);
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
    }
}
