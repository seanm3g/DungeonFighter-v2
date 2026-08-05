using System;
using System.Collections.Generic;

namespace RPGGame.Data
{
    /// <summary>
    /// Hover notes (Google Sheets cell notes) for ACTIONS header labels.
    /// </summary>
    public static class ActionSheetHeaderNotes
    {
        public static bool TryGetNote(string? contextHint, string? label, out string note)
        {
            note = "";
            if (string.IsNullOrWhiteSpace(label))
                return false;

            string normLabel = SpreadsheetHeader.NormalizeLabel(label);
            string normCtx = string.IsNullOrWhiteSpace(contextHint)
                ? ""
                : SpreadsheetHeader.NormalizeLabel(contextHint);

            if (normCtx == "TRIGGERS" || IsTriggerFamilyLabel(normLabel))
            {
                if (TryGetTriggerNote(normLabel, out note))
                    return true;
            }

            if (normCtx == "CADENCES")
            {
                if (LabelNotes.TryGetValue(normLabel, out string? cadenceNote) && !string.IsNullOrEmpty(cadenceNote))
                {
                    note = cadenceNote;
                    return true;
                }
            }

            // Do not attach CADENCES enable notes (TURN/ACTION/FIGHT/DUNGEON) to unrelated columns with the same short label.
            if (normLabel is "TURN" or "ACTION" or "FIGHT" or "DUNGEON"
                || normLabel.EndsWith("DURATION", StringComparison.Ordinal)
                || normLabel.EndsWith("→", StringComparison.Ordinal))
            {
                if (normCtx != "CADENCES" && normCtx != "TRIGGERS")
                {
                    // fall through to generic notes only for non-cadence short labels
                    if (normLabel is "TURN" or "ACTION" or "FIGHT" or "DUNGEON")
                        return false;
                }
            }

            if (LabelNotes.TryGetValue(normLabel, out string? text) && !string.IsNullOrEmpty(text))
            {
                note = text;
                return true;
            }

            return false;
        }

        private static bool IsTriggerFamilyLabel(string normLabel)
        {
            foreach (var family in ActionTriggerSheetColumns.Families)
            {
                if (SpreadsheetHeader.NormalizeLabel(family.CountLabel) == normLabel
                    || SpreadsheetHeader.NormalizeLabel(family.ScopeLabel) == normLabel
                    || SpreadsheetHeader.NormalizeLabel(family.MechanicsLabel) == normLabel)
                    return true;
            }

            return false;
        }

        private static bool TryGetTriggerNote(string normLabel, out string note)
        {
            note = "";
            foreach (var family in ActionTriggerSheetColumns.Families)
            {
                if (SpreadsheetHeader.NormalizeLabel(family.CountLabel) == normLabel)
                {
                    note = $"{family.CountLabel}: enable/count for this WHEN (usually 1). "
                        + $"For ON ROLL VALUE this is the d20 face. "
                        + $"Pairs with '{family.ScopeLabel}' and '{family.MechanicsLabel}'. "
                        + "Enabled WHENs also feed triggerConditions for status gating.";
                    return true;
                }

                if (SpreadsheetHeader.NormalizeLabel(family.ScopeLabel) == normLabel)
                {
                    note = $"{family.ScopeLabel}: cadence for a lasting grant after {family.WhenToken}. "
                        + "Blank = instant one-shot when the event fires.\n\n"
                        + CadenceScopeDescriptions.CombinedAuthoringNote();
                    return true;
                }

                if (SpreadsheetHeader.NormalizeLabel(family.MechanicsLabel) == normLabel)
                {
                    note = $"{family.MechanicsLabel}: comma-separated mechanic IDs to attach to {family.WhenToken}. "
                        + "Pointers only — magnitudes stay in the real mechanic columns "
                        + "(DAMAGE MOD, HEAL, WEAKEN, dice columns, …). "
                        + "See MECHANIC_LIST for hover descriptions of each id.";
                    return true;
                }
            }

            return false;
        }

        private static readonly Dictionary<string, string> LabelNotes = new(StringComparer.OrdinalIgnoreCase)
        {
            ["CADENCE"] =
                "Legacy compact CADENCE column — prefer CADENCES triples (TURN / ACTION / FIGHT / DUNGEON enable + DURATION + →). "
                + "Still read on pull when CADENCES are empty.\n\n"
                + CadenceScopeDescriptions.CombinedAuthoringNote()
                + "\n\nSee CADENCE_LIST tab.",
            ["DURATION"] =
                "Legacy compact DURATION — prefer TURN DURATION / ACTION DURATION / … under the CADENCES band. "
                + "Number only when used (e.g. 2 for ×2).",
            ["MECHANICS"] =
                "Legacy compact MECHANICS checklist — prefer TURN → / ACTION → / … under CADENCES. "
                + "See MECHANIC_LIST for id descriptions.",
            ["TURN"] =
                "CADENCES → TURN enable (usually 1). Pair with TURN DURATION and TURN →. "
                + CadenceScopeDescriptions.GetSummary("TURN") + ". " + CadenceScopeDescriptions.GetDetail("TURN"),
            ["TURNDURATION"] =
                "Application count for TURN cadence (e.g. 2 = TURN ×2). Blank ⇒ 1 when TURN is enabled.",
            ["TURN→"] =
                "Mechanic IDs granted on TURN cadence. Magnitudes stay in detail columns (ACCURACY, HIT, COMBO, …). "
                + "See MECHANIC_LIST.",
            ["ACTION"] =
                "CADENCES → ACTION enable. " + CadenceScopeDescriptions.GetSummary("ACTION") + ". "
                + CadenceScopeDescriptions.GetDetail("ACTION"),
            ["ACTIONDURATION"] =
                "Application count for ACTION cadence (stacks additive bank ×N).",
            ["ACTION→"] =
                "Mechanic IDs for ACTION cadence (typically next-action SPEED / DAMAGE / MULTIHIT / AMP). "
                + "Magnitudes in HERO/ENEMY BASE STATS columns.",
            ["FIGHT"] =
                "CADENCES → FIGHT enable. " + CadenceScopeDescriptions.GetSummary("FIGHT") + ". "
                + CadenceScopeDescriptions.GetDetail("FIGHT"),
            ["FIGHTDURATION"] =
                "Application count for FIGHT-scoped grants.",
            ["FIGHT→"] =
                "Mechanic IDs that last for the current fight. Magnitudes in detail columns.",
            ["DUNGEON"] =
                "CADENCES → DUNGEON enable. " + CadenceScopeDescriptions.GetSummary("DUNGEON") + ". "
                + CadenceScopeDescriptions.GetDetail("DUNGEON"),
            ["DUNGEONDURATION"] =
                "Application count for DUNGEON-scoped grants.",
            ["DUNGEON→"] =
                "Mechanic IDs that last for the dungeon run. Magnitudes in detail columns.",
            ["DAMAGE"] =
                "Swing DAMAGE(%) — percent of attack for this action's hits. "
                + "Not the same as DAMAGE MOD (next-action / hero_next_action_damage).",
            ["DAMAGE(%)"] =
                "Swing DAMAGE(%) — percent of attack for this action's hits. "
                + "Not the same as DAMAGE MOD (next-action / hero_next_action_damage).",
            ["SPEED"] =
                "SPEED(x) — action length multiplier for this swing. "
                + "Not ACTION SPEED next-action mod.",
            ["SPEED(X)"] =
                "SPEED(x) — action length multiplier for this swing. "
                + "Not ACTION SPEED next-action mod.",
            ["#OFHITS"] =
                "Number of hits this swing resolves. Not MULTIHIT MOD (bonus hits granted to a later action).",
            ["TRIGGERCONDITIONS"] =
                "Legacy free-text / comma list of WHEN and filter tokens (ONHIT, ONKILL, ONWIELD:Sword, IFCLUTCH, …). "
                + "Prefer TRIGGERS triples for outcome gates when possible; filters still live here.",
            ["ONROLLVALUE"] =
                "Exact d20 face (1–20) that can fire ONROLLVALUE effects.",
            ["ONROOMSCLEARED"] =
                "Room-clear trigger: number = only the Nth clear; other non-empty = every clear.",
            ["ACTIONSPEED"] =
                "Next-action SPEED % (HERO/ENEMY BASE STATS). ACTION cadence. Maps to hero_next_action_speed / enemy_*.",
            ["DAMAGEMOD"] =
                "Next-action DAMAGE % (BASE STATS). ACTION cadence. Maps to hero_next_action_damage — not swing DAMAGE(%).",
            ["MULTIHITMOD"] =
                "Next-action multihit layers (BASE STATS). ACTION cadence.",
            ["AMPMOD"] =
                "Next-action AMP % (BASE STATS). ACTION cadence.",
            ["AMP_MOD"] =
                "Next-action AMP % (BASE STATS). ACTION cadence.",
            ["WEAPONSPEED"] =
                "Flat weapon speed points (HERO/ENEMY BASE). Not ACTION SPEED %. Cadence-scoped (TURN/ACTION/FIGHT/DUNGEON).",
            ["WEAPONDAMAGE"] =
                "Flat weapon damage (HERO/ENEMY BASE). Not ACTION DAMAGE / DAMAGE(%). Cadence-scoped.",
            ["RESERVEPOOL"] =
                "RESERVE POOL: mark with 1/true to exclude from default weighted action rolls. "
                + "Action stays available for combo strip / explicit picks. Synced to tag reserve_pool (also accepted in TAGS).",
            ["HEAL"] =
                "Flat heal (HERO HEAL). Can be gated by TRIGGERS → heal.",
            ["WEAKEN"] =
                "Weaken application value. List 'weaken' under a TRIGGERS → cell to gate when it applies.",
        };
    }
}
