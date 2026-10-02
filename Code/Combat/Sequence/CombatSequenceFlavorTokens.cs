using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using RPGGame.Combat.Calculators;
using RPGGame.UI.ColorSystem;

namespace RPGGame.Combat.Sequence
{
    /// <summary>
    /// Plain-text placeholders for the F7 narrative combat-log sequence banks.
    /// Built from an already-resolved swing; does not re-run combat math.
    /// Roll / defense / damage / outcome tokens are poetic interpretations — never raw HUD labels
    /// like COMBO, and never raw numbers.
    /// </summary>
    public sealed class CombatSequenceFlavorTokens
    {
        private static readonly Regex Digits = new(@"\d", RegexOptions.Compiled);
        private static readonly Regex MarkupTag = new(@"\{\{[^}]*\}\}|</?[a-zA-Z][^>]*>", RegexOptions.Compiled);

        public string Attacker { get; init; } = string.Empty;
        public string Target { get; init; } = string.Empty;
        public string Outcome { get; init; } = string.Empty;
        public string Roll { get; init; } = string.Empty;
        public string Action { get; init; } = string.Empty;
        public string Defense { get; init; } = string.Empty;
        public string Damage { get; init; } = string.Empty;
        public string Effects { get; init; } = string.Empty;
        /// <summary>Who holds tempo / will act next (name).</summary>
        public string Tempo { get; init; } = string.Empty;
        /// <summary>Lingering Defense stance as damage-susceptibility prose.</summary>
        public string Stance { get; init; } = string.Empty;
        /// <summary>Critical-hit consequence prose (precision).</summary>
        public string Critical { get; init; } = string.Empty;
        /// <summary>Critical-miss consequence prose (fumble / regather footing).</summary>
        public string CriticalMiss { get; init; } = string.Empty;

        public bool IncludeDefense { get; init; }
        public bool IncludeDamage { get; init; }
        public bool IncludeEffects { get; init; }
        public bool IncludeTempo { get; init; }
        public bool IncludeStance { get; init; }
        public bool IncludeCritical { get; init; }
        public bool IncludeCriticalMiss { get; init; }
        public bool IsHeal { get; init; }
        /// <summary>True when the swing missed (including crit-miss) — action beat uses miss prose.</summary>
        public bool IsMiss { get; init; }
        /// <summary>True for room/environment hazard prose (own paragraph; no gather-will / tempo).</summary>
        public bool IsEnvironmental { get; init; }
        /// <summary>
        /// Full prose sentences for each status application (e.g. weaken on hero, weaken on foe).
        /// Played one after another so an env act + two applications read as ~three lines.
        /// </summary>
        public IReadOnlyList<string> EffectSentences { get; init; } = Array.Empty<string>();
        /// <summary>Extra identity names (status victims) for emphasis beyond Attacker/Target.</summary>
        public IReadOnlyList<string> AdditionalNames { get; init; } = Array.Empty<string>();

        public Dictionary<string, string> ToReplacements()
        {
            return new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                ["attacker"] = Attacker,
                ["target"] = Target,
                ["outcome"] = Outcome,
                ["roll"] = Roll,
                ["action"] = Action,
                ["defense"] = Defense,
                ["damage"] = Damage,
                ["effects"] = Effects,
                ["tempo"] = Tempo,
                ["stance"] = Stance,
                ["critical"] = Critical,
                ["criticalMiss"] = CriticalMiss,
                ["name"] = Attacker,
                ["effect"] = Effects
            };
        }

        /// <summary>
        /// F7 tokens for a room/environment hazard: action prose + one sentence per status application.
        /// </summary>
        public static CombatSequenceFlavorTokens FromEnvironmental(
            Actor source,
            Action action,
            IReadOnlyList<List<ColoredText>>? statusEffects,
            int damage,
            Actor? primaryTarget,
            IReadOnlyList<Actor>? affectedActors = null)
        {
            string actionName = string.IsNullOrWhiteSpace(action?.Name) ? "hazard" : action!.Name.Trim();
            string attackerName = FormatNarrativeActorName(source, fallback: "The room");
            string targetName = FormatNarrativeActorName(primaryTarget, fallback: "the field");

            var articleActors = new List<Actor?>();
            if (source != null)
                articleActors.Add(source);
            if (primaryTarget != null)
                articleActors.Add(primaryTarget);
            if (affectedActors != null)
            {
                foreach (var a in affectedActors)
                {
                    if (a != null)
                        articleActors.Add(a);
                }
            }

            var effectSentences = new List<string>();
            var additionalNames = new List<string>();
            if (statusEffects != null)
            {
                for (int i = 0; i < statusEffects.Count; i++)
                {
                    var line = statusEffects[i];
                    if (line == null || line.Count == 0)
                        continue;
                    string plain = ColoredTextRenderer.RenderAsPlainText(line);
                    string sentence = InterpretEnvironmentalStatusSentence(plain);
                    if (string.IsNullOrWhiteSpace(sentence))
                        continue;

                    sentence = ApplyEnemyArticlesToProse(sentence, articleActors.ToArray());
                    sentence = CapitalizeLeadingArticle(sentence);
                    effectSentences.Add(sentence);

                    Actor? victimActor = affectedActors != null && i < affectedActors.Count
                        ? affectedActors[i]
                        : null;
                    string? victimName = victimActor != null
                        ? FormatNarrativeActorName(victimActor)
                        : TryExtractAffectedName(plain);
                    if (!string.IsNullOrWhiteSpace(victimName)
                        && !string.Equals(victimName, attackerName, StringComparison.OrdinalIgnoreCase)
                        && !string.Equals(victimName, targetName, StringComparison.OrdinalIgnoreCase)
                        && !additionalNames.Any(n => string.Equals(n, victimName, StringComparison.OrdinalIgnoreCase)))
                    {
                        additionalNames.Add(victimName!);
                    }
                }
            }

            bool includeDamage = damage > 0;
            string damageText = string.Empty;
            if (includeDamage)
                damageText = InterpretDamage(damage, ResolveMaxHealth(primaryTarget));

            // Joined fragment for legacy {effects}/{effect} placeholders (single-beat fallback).
            string effectsJoined = effectSentences.Count == 0
                ? string.Empty
                : effectSentences.Count == 1
                    ? effectSentences[0]
                    : string.Join(" ", effectSentences);

            return new CombatSequenceFlavorTokens
            {
                Attacker = attackerName,
                Target = targetName,
                Action = actionName,
                Damage = damageText,
                Effects = effectsJoined,
                EffectSentences = effectSentences,
                AdditionalNames = additionalNames,
                IncludeDamage = includeDamage,
                IncludeEffects = effectSentences.Count > 0,
                IncludeDefense = false,
                IncludeStance = false,
                IncludeTempo = false,
                IncludeCritical = false,
                IncludeCriticalMiss = false,
                IsEnvironmental = true,
                IsMiss = false,
                IsHeal = false
            };
        }

        /// <summary>
        /// Turns mechanical env status lines into a full numeral-free prose sentence.
        /// e.g. "Xander affected by WEAKEN for 1 turn" → "Xander falters under a weaken".
        /// </summary>
        public static string InterpretEnvironmentalStatusSentence(string? raw)
        {
            string softened = SoftenStatusMessage(raw);
            if (string.IsNullOrWhiteSpace(softened))
                return string.Empty;

            // SoftenStatusMessage may still leave "Name affected by WEAKEN for N turn(s)" if unmatched —
            // InterpretEnvironmentalStatusSentence prefers the dedicated affected-by path below via Soften.
            string sentence = softened.Trim().TrimEnd('.', '!', '?');
            if (sentence.Length == 0)
                return string.Empty;

            // Promote gerund fragments ("Name faltering under a weaken") to finite prose.
            sentence = Regex.Replace(
                sentence,
                @"\bfaltering\b",
                "falters",
                RegexOptions.IgnoreCase);
            sentence = Regex.Replace(
                sentence,
                @"\breeling\b",
                "reels",
                RegexOptions.IgnoreCase);
            sentence = Regex.Replace(
                sentence,
                @"\bthreading\b",
                "threads",
                RegexOptions.IgnoreCase);
            sentence = Regex.Replace(
                sentence,
                @"\bopening\b",
                "opens",
                RegexOptions.IgnoreCase);
            sentence = Regex.Replace(
                sentence,
                @"\bclinging\b",
                "clings",
                RegexOptions.IgnoreCase);
            sentence = Regex.Replace(
                sentence,
                @"\bbiting\b",
                "bites",
                RegexOptions.IgnoreCase);

            if (sentence.Length > 0)
                sentence = char.ToUpperInvariant(sentence[0]) + sentence.Substring(1);
            return sentence;
        }

        private static string? TryExtractAffectedName(string? raw)
        {
            if (string.IsNullOrWhiteSpace(raw))
                return null;
            string plain = ToPlainStatusText(raw);
            var match = Regex.Match(
                plain,
                @"^(?<name>.+?)\s+affected\s+by\b",
                RegexOptions.IgnoreCase);
            if (match.Success)
                return match.Groups["name"].Value.Trim();
            match = Regex.Match(
                plain,
                @"^(?<name>.+?)\s+is\s+(?:poisoned|bleeding|burning|weakened|stunned)\b",
                RegexOptions.IgnoreCase);
            return match.Success ? match.Groups["name"].Value.Trim() : null;
        }

        internal static CombatSequenceFlavorTokens From(
            ActionExecutionResult result,
            Actor source,
            Actor? target)
        {
            if (result?.SelectedAction == null)
                return new CombatSequenceFlavorTokens();

            Actor? displayTarget = result.EffectiveTarget ?? target;
            var selected = result.SelectedAction;

            string actionName = string.IsNullOrWhiteSpace(selected.Name)
                ? (result.Hit ? "hit" : "miss")
                : selected.Name.Trim();

            int rollFace = result.AttackRoll != 0
                ? result.AttackRoll
                : result.ModifiedBaseRoll;

            bool includeDefense = result.Hit
                && displayTarget is Character
                && (selected.Type == ActionType.Attack || selected.Type == ActionType.Spell);

            bool isHeal = result.Hit && selected.Type == ActionType.Heal;
            bool includeDamage = isHeal
                || (result.Hit
                    && result.Damage > 0
                    && (selected.Type == ActionType.Attack || selected.Type == ActionType.Spell));

            string defenseText = string.Empty;
            if (includeDefense && displayTarget is Character defender)
            {
                bool pierce = DamageCalculator.IgnoresArmor(defender, selected);
                defenseText = InterpretDefense(defender, pierce);
            }

            string damageText = string.Empty;
            if (includeDamage)
            {
                int amount = isHeal ? result.HealAmount : result.Damage;
                int maxHp = ResolveMaxHealth(isHeal ? source : displayTarget);
                damageText = isHeal
                    ? InterpretHeal(amount, maxHp)
                    : InterpretDamage(amount, maxHp);
            }

            string effectsText = InterpretEffects(result.StatusEffectMessages);
            bool includeEffects = !string.IsNullOrWhiteSpace(effectsText);

            string attackerName = FormatNarrativeActorName(source, fallback: "Someone");
            string targetName = FormatNarrativeActorName(displayTarget, fallback: "their foe");
            string tempoName = InterpretTempoHolder(
                attackerName,
                targetName,
                selected.Length,
                result.IsCriticalMiss);
            bool includeTempo = !string.IsNullOrWhiteSpace(tempoName);

            double stanceMult = ResolveStanceMultiplier(selected, result.Hit);
            string stanceText = InterpretStance(stanceMult);
            bool includeStance = !string.IsNullOrWhiteSpace(stanceText);

            bool includeCritical = result.Hit && result.IsCritical;
            string criticalText = includeCritical ? InterpretCriticalConsequence(actionName) : string.Empty;

            bool includeCriticalMiss = result.IsCriticalMiss;
            string criticalMissText = includeCriticalMiss ? InterpretCriticalMissConsequence() : string.Empty;

            string effectsWithArticles = ApplyEnemyArticlesToProse(effectsText, source, displayTarget);

            return new CombatSequenceFlavorTokens
            {
                Attacker = attackerName,
                Target = targetName,
                Outcome = InterpretOutcome(result, actionName),
                Roll = InterpretRoll(rollFace),
                Action = actionName,
                Defense = defenseText,
                Damage = damageText,
                Effects = effectsWithArticles,
                Tempo = tempoName,
                Stance = stanceText,
                Critical = criticalText,
                CriticalMiss = criticalMissText,
                IncludeDefense = includeDefense,
                IncludeDamage = includeDamage,
                IncludeEffects = includeEffects,
                IncludeTempo = includeTempo,
                IncludeStance = includeStance,
                IncludeCritical = includeCritical,
                IncludeCriticalMiss = includeCriticalMiss,
                IsHeal = isHeal,
                IsMiss = !result.Hit
            };
        }

        /// <summary>
        /// Maps a d20 (or bonus-adjusted) face to a poetic adjudication — no numerals.
        /// </summary>
        public static string InterpretRoll(int rollFace)
        {
            if (rollFace <= 1)
                return "a cursed stumble of fortune";
            if (rollFace <= 5)
                return "thin and shaky fortune";
            if (rollFace <= 9)
                return "uncertain fortune";
            if (rollFace <= 13)
                return "steady resolve";
            if (rollFace <= 16)
                return "favorable fortune";
            if (rollFace <= 19)
                return "keen, decisive fortune";
            return "near-perfect fortune";
        }

        /// <summary>
        /// Describes how effective the blow feels through defense (inverse of DR).
        /// High DR → grazing; low DR / pierce → heavy and hard-hitting.
        /// </summary>
        public static string InterpretDefense(Character defender, bool pierce)
        {
            if (pierce)
                return "a hard-hitting blow that finds no resistance";

            var mit = ClassDefenseCalculator.ApplyIncoming(defender, 100, pierce: false, mitigationHitIndex: 0);
            double drPct = mit.DrPercent * 100.0;

            if (drPct <= 10.0)
                return "heavy and barely checked";
            if (drPct <= 25.0)
                return "hard-hitting and effective";
            if (drPct <= 40.0)
                return "solid, though the guard takes its share";
            if (drPct <= 60.0)
                return "blunted — more glancing than crushing";
            if (drPct <= 75.0)
                return "a grazing strike against stubborn steel";
            return "little more than a grazing whisper";
        }

        /// <summary>
        /// Interprets dealt damage relative to the target's max HP — no numerals.
        /// </summary>
        public static string InterpretDamage(int amount, int maxHealth)
        {
            double pct = amount / (double)Math.Max(1, maxHealth);
            if (pct < 0.03 || amount <= 2)
                return "a grazing scratch";
            if (pct < 0.08)
                return "a light wound";
            if (pct < 0.15)
                return "a solid wound";
            if (pct < 0.30)
                return "a heavy injury";
            if (pct < 0.50)
                return "a devastating wound";
            return "ruinous harm";
        }

        /// <summary>
        /// Interprets healing relative to the recipient's max HP — no numerals.
        /// </summary>
        public static string InterpretHeal(int amount, int maxHealth)
        {
            double pct = amount / (double)Math.Max(1, maxHealth);
            if (pct < 0.05 || amount <= 2)
                return "a light mend";
            if (pct < 0.15)
                return "a solid recovery";
            if (pct < 0.30)
                return "a deep restoration";
            return "a profound renewal";
        }

        /// <summary>
        /// Narrative outcome language: never the HUD word "COMBO". Prefers the attack name or
        /// flavor describing how the strike lands.
        /// </summary>
        internal static string InterpretOutcome(ActionExecutionResult result, string actionName)
        {
            string strike = string.IsNullOrWhiteSpace(actionName) ? "strike" : actionName.Trim();

            if (result.IsCriticalMiss)
                return "a cursed miss";
            if (!result.Hit)
                return "a miss";
            if (result.IsCritical && result.IsCombo)
                return $"a perfected {strike}";
            if (result.IsCritical)
                return "a critical blow";
            if (result.IsCombo)
                return $"a chaining {strike}";
            return "a solid hit";
        }

        /// <summary>
        /// Narrative display name: heroes stay bare proper nouns; enemies are common nouns
        /// and take a lowercase definite article (<c>the Wraith</c>). Callers capitalize
        /// sentence-initial <c>the</c> when the name opens a line.
        /// </summary>
        public static string FormatNarrativeActorName(Actor? actor, string fallback = "Someone")
        {
            if (actor == null || string.IsNullOrWhiteSpace(actor.Name))
                return fallback;

            string name = actor.Name.Trim();
            if (actor is Enemy)
                return "the " + name;
            return name;
        }

        /// <summary>
        /// Capitalizes a leading <c>the </c> so enemy names can open a sentence
        /// (<c>The Wraith shifts…</c>) while mid-sentence uses remain lowercase.
        /// </summary>
        public static string CapitalizeLeadingArticle(string text)
        {
            if (string.IsNullOrEmpty(text))
                return text;
            if (text.StartsWith("the ", StringComparison.Ordinal))
                return "The " + text.Substring(4);
            return text;
        }

        /// <summary>
        /// Strips a leading definite article so coloring/registration hits only the
        /// enemy name (<c>the Orc</c> → <c>Orc</c>). Heroes and bare names are unchanged.
        /// </summary>
        public static string StripLeadingArticle(string text)
        {
            if (string.IsNullOrEmpty(text))
                return text;
            if (text.StartsWith("the ", StringComparison.OrdinalIgnoreCase))
                return text.Substring(4);
            return text;
        }

        /// <summary>
        /// Rewrites bare enemy names inside prose fragments to include <c>the</c>
        /// (skips names that already have the article).
        /// </summary>
        public static string ApplyEnemyArticlesToProse(string text, params Actor?[] actors)
        {
            if (string.IsNullOrEmpty(text) || actors == null || actors.Length == 0)
                return text;

            string result = text;
            foreach (var actor in actors)
            {
                if (actor is not Enemy || string.IsNullOrWhiteSpace(actor.Name))
                    continue;

                string name = actor.Name.Trim();
                result = Regex.Replace(
                    result,
                    $@"(?<!the\s)\b{Regex.Escape(name)}\b",
                    "the " + name,
                    RegexOptions.IgnoreCase);
            }

            return result;
        }

        /// <summary>
        /// Who holds tempo after this swing (and is therefore going next), from action length.
        /// Short recovery keeps the attacker's pace; long recovery cedes the next beat to the foe.
        /// </summary>
        public static string InterpretTempoHolder(
            string attackerName,
            string targetName,
            double actionLength,
            bool isCriticalMiss)
        {
            double length = Math.Max(0.01, actionLength);
            if (isCriticalMiss)
                length *= 2.0;

            if (length <= 0.85)
                return string.IsNullOrWhiteSpace(attackerName) ? "the attacker" : attackerName.Trim();
            if (length >= 1.35)
                return string.IsNullOrWhiteSpace(targetName) ? "the foe" : targetName.Trim();

            // Mid cadence: the attacker still owns the exchange unless recovery clearly lags.
            return string.IsNullOrWhiteSpace(attackerName) ? "the attacker" : attackerName.Trim();
        }

        /// <summary>
        /// Lingering Defense stance as susceptibility prose (aggressive = open, defensive = braced).
        /// </summary>
        public static string InterpretStance(double blockMultiplier)
        {
            string name = StandingBlock.GetStanceName(blockMultiplier);
            return name switch
            {
                "aggressive" =>
                    "an aggressive stance, overextended and open to the next blow",
                "defensive" =>
                    "a defensive stance, guarded and harder to wound",
                _ =>
                    "a balanced stance, neither overextended nor fully braced"
            };
        }

        /// <summary>
        /// Critical-hit consequence: precision and lethality of the strike — not the HUD word CRIT.
        /// </summary>
        public static string InterpretCriticalConsequence(string actionName)
        {
            string strike = string.IsNullOrWhiteSpace(actionName) ? "the strike" : actionName.Trim();
            return $"extra precision in {strike}, finding the seam where flesh yields";
        }

        /// <summary>
        /// Critical-miss consequence: fumble and recovery time — not the HUD label CRIT MISS.
        /// </summary>
        public static string InterpretCriticalMissConsequence()
        {
            return "a fumbled weapon and lost footing that costs precious moments to regather";
        }

        /// <summary>
        /// Standing BLOCK after this swing: named hit keeps the action stance; miss/unnamed → neutral.
        /// </summary>
        public static double ResolveStanceMultiplier(Action? action, bool hit)
        {
            if (hit && !StandingBlock.IsUnnamedSwing(action))
                return StandingBlock.ResolveFromAction(action);
            return StandingBlock.DefaultBlockPercent;
        }

        /// <summary>
        /// Turns status-effect combat-log lines into a single prose fragment (poison, bleed, etc.).
        /// </summary>
        public static string InterpretEffects(IReadOnlyList<string>? statusEffectMessages)
        {
            if (statusEffectMessages == null || statusEffectMessages.Count == 0)
                return string.Empty;

            var phrases = new List<string>();
            foreach (var raw in statusEffectMessages)
            {
                string phrase = SoftenStatusMessage(raw);
                if (string.IsNullOrWhiteSpace(phrase))
                    continue;
                if (phrases.Any(p => string.Equals(p, phrase, StringComparison.OrdinalIgnoreCase)))
                    continue;
                phrases.Add(phrase);
            }

            if (phrases.Count == 0)
                return string.Empty;
            if (phrases.Count == 1)
                return phrases[0];
            if (phrases.Count == 2)
                return $"{phrases[0]}, and {phrases[1]}";
            return string.Join(", ", phrases.Take(phrases.Count - 1)) + ", and " + phrases[^1];
        }

        /// <summary>
        /// F7 narrative DoT tick: one numeral-free sentence for poison/burn/bleed/acid damage
        /// (replaces "takes N poison damage" + "(poison: N% of max HP)" / stacks-remain lines).
        /// </summary>
        public static string InterpretDoTTick(
            string actorName,
            string damageType,
            int damage,
            int maxHealth,
            bool stillActive)
        {
            string severity = InterpretDamage(damage, maxHealth);
            string name = string.IsNullOrWhiteSpace(actorName) ? "the afflicted" : actorName.Trim();
            string type = (damageType ?? string.Empty).Trim().ToLowerInvariant();

            return type switch
            {
                "poison" => stillActive
                    ? $"Poison still courses through {name}, leaving {severity}"
                    : $"The last of the poison wracks {name} for {severity}, then fades",
                "burn" => stillActive
                    ? $"Fire still clinging to {name} scorches them for {severity}"
                    : $"The last flames on {name} sear {severity}, then gutter out",
                "bleed" => stillActive
                    ? $"Blood still wells from {name}, costing {severity}"
                    : $"The bleeding on {name} costs {severity} before it stanches",
                "acid" => stillActive
                    ? $"Acid still biting into {name} eats away {severity}"
                    : $"The last of the acid on {name} burns for {severity}, then dies",
                _ => stillActive
                    ? $"{type} still afflicts {name}, leaving {severity}"
                    : $"The last of the {type} on {name} leaves {severity}"
            };
        }

        /// <summary>True when a narrative token still leaks a digit (defense/damage/roll must not).</summary>
        public static bool ContainsDigit(string text) =>
            !string.IsNullOrEmpty(text) && Digits.IsMatch(text);

        private static string SoftenStatusMessage(string? raw)
        {
            if (string.IsNullOrWhiteSpace(raw))
                return string.Empty;

            string plain = ToPlainStatusText(raw);
            if (string.IsNullOrWhiteSpace(plain))
                return string.Empty;

            // "Name is poisoned!" / "Name is bleeding!" → "poison threading through Name"
            var isMatch = Regex.Match(
                plain,
                @"^(?<name>.+?)\s+is\s+(?<effect>poisoned|bleeding|burning|acid-burned|weakened|stunned|silenced|confused|vulnerable|exposed|marked)\b",
                RegexOptions.IgnoreCase);
            if (isMatch.Success)
            {
                string name = isMatch.Groups["name"].Value.Trim();
                string effect = isMatch.Groups["effect"].Value.ToLowerInvariant();
                return effect switch
                {
                    "poisoned" => $"poison threading through {name}",
                    "bleeding" => $"bleed opening on {name}",
                    "burning" => $"fire clinging to {name}",
                    "acid-burned" => $"acid biting into {name}",
                    "weakened" => $"{name} faltering under a weaken",
                    "stunned" => $"{name} reeling, stunned",
                    "silenced" => $"{name} stricken mute",
                    "confused" => $"{name} lost in confusion",
                    "vulnerable" => $"{name} left vulnerable",
                    "exposed" => $"{name} left exposed",
                    "marked" => $"{name} marked for a finishing blow",
                    _ => $"{effect} upon {name}"
                };
            }

            // Environmental application: "Name affected by WEAKEN for 1 turn" → numeral-free prose.
            var affectedMatch = Regex.Match(
                plain,
                @"^(?<name>.+?)\s+affected\s+by\s+(?<effect>BLEED|WEAKEN|SLOW|POISON|STUN|BURN|FREEZE|EFFECT)\b",
                RegexOptions.IgnoreCase);
            if (affectedMatch.Success)
            {
                string name = affectedMatch.Groups["name"].Value.Trim();
                string effect = affectedMatch.Groups["effect"].Value.ToUpperInvariant();
                return effect switch
                {
                    "BLEED" => $"bleed opening on {name}",
                    "WEAKEN" => $"{name} faltering under a weaken",
                    "SLOW" => $"{name} moving as if through syrup",
                    "POISON" => $"poison threading through {name}",
                    "STUN" => $"{name} reeling, stunned",
                    "BURN" => $"fire clinging to {name}",
                    "FREEZE" => $"{name} locked in a chill",
                    _ => $"{name} caught in the hazard's grasp"
                };
            }

            // Mechanical DoT tick: "Name takes 1 poison damage" → numeral-free lingering prose.
            var takesMatch = Regex.Match(
                plain,
                @"^(?:\[)?(?<name>.+?)(?:\])?\s+takes\s+\d+\s+(?<type>poison|burn|bleed|acid)\s+damage\b",
                RegexOptions.IgnoreCase);
            if (takesMatch.Success)
            {
                string name = takesMatch.Groups["name"].Value.Trim();
                string type = takesMatch.Groups["type"].Value.ToLowerInvariant();
                return type switch
                {
                    "poison" => $"poison still coursing through {name}",
                    "burn" => $"fire still clinging to {name}",
                    "bleed" => $"blood still welling from {name}",
                    "acid" => $"acid still biting into {name}",
                    _ => $"{type} still afflicting {name}"
                };
            }

            // Detail-only remain lines ("(poison: 1% of max HP)", stacks remain) — omit; tick prose covers it.
            if (Regex.IsMatch(
                    plain,
                    @"^\(?\s*(poison|burn|bleed|acid)\s*:",
                    RegexOptions.IgnoreCase)
                || Regex.IsMatch(plain, @"stacks remain\)?\s*$", RegexOptions.IgnoreCase)
                || Regex.IsMatch(plain, @"of max HP\)?\s*$", RegexOptions.IgnoreCase))
            {
                return string.Empty;
            }

            // "Name begins regenerating health!" etc. — trim yell punctuation into prose.
            string softened = plain.Trim().TrimEnd('!', '.', '?');
            if (softened.Length == 0)
                return string.Empty;
            return softened;
        }

        private static string ToPlainStatusText(string raw)
        {
            string text = raw.Trim();
            if (text.IndexOf('{', StringComparison.Ordinal) >= 0
                || text.IndexOf('<', StringComparison.Ordinal) >= 0)
            {
                try
                {
                    var parsed = ColoredTextParser.Parse(text);
                    text = ColoredTextRenderer.RenderAsPlainText(parsed);
                }
                catch
                {
                    text = MarkupTag.Replace(text, string.Empty);
                }
            }

            return text.Trim().TrimStart();
        }

        private static int ResolveMaxHealth(Actor? actor)
        {
            if (actor is Character character)
                return Math.Max(1, character.GetEffectiveMaxHealth());
            return Math.Max(1, actor?.GetMaxHealthForPoisonDot() ?? 100);
        }

        /// <summary>
        /// Maps HUD step kinds to narrative playback order:
        /// Attacker → Outcome → Roll → Action → Critical/CriticalMiss → Defense → Damage/Heal →
        /// Effect → Stance → Tempo.
        /// </summary>
        public static IReadOnlyList<CombatSequenceStepKind> NarrativeBeatOrder { get; } =
            new[]
            {
                CombatSequenceStepKind.Attacker,
                CombatSequenceStepKind.Outcome,
                CombatSequenceStepKind.Roll,
                CombatSequenceStepKind.Action,
                CombatSequenceStepKind.Critical,
                CombatSequenceStepKind.CriticalMiss,
                CombatSequenceStepKind.Defense,
                CombatSequenceStepKind.Damage,
                CombatSequenceStepKind.Heal,
                CombatSequenceStepKind.Effect,
                CombatSequenceStepKind.Stance,
                CombatSequenceStepKind.Tempo
            };

        public static string BankKeyFor(CombatSequenceStepKind kind) => BankKeyFor(kind, isMiss: false);

        public static string BankKeyFor(CombatSequenceStepKind kind, bool isMiss) => BankKeyFor(kind, isMiss, isEnvironmental: false);

        public static string BankKeyFor(CombatSequenceStepKind kind, bool isMiss, bool isEnvironmental) => kind switch
        {
            CombatSequenceStepKind.Attacker => "sequenceAttacker",
            CombatSequenceStepKind.Outcome => "sequenceOutcome",
            CombatSequenceStepKind.Roll => "sequenceRoll",
            CombatSequenceStepKind.Action => isEnvironmental
                ? "sequenceEnvironment"
                : (isMiss ? "sequenceActionMiss" : "sequenceAction"),
            CombatSequenceStepKind.Critical => "sequenceCritical",
            CombatSequenceStepKind.CriticalMiss => "sequenceCriticalMiss",
            CombatSequenceStepKind.Defense => "sequenceDefense",
            CombatSequenceStepKind.Damage => "sequenceDamage",
            CombatSequenceStepKind.Heal => "sequenceDamage",
            CombatSequenceStepKind.Effect => isEnvironmental ? "sequenceEnvironmentEffect" : "sequenceEffects",
            CombatSequenceStepKind.Stance => "sequenceStance",
            CombatSequenceStepKind.Tempo => "sequenceTempo",
            _ => isMiss ? "sequenceActionMiss" : "sequenceAction"
        };

        public string ResolveBankKey(CombatSequenceStepKind kind) => BankKeyFor(kind, IsMiss, IsEnvironmental);

        public bool ShouldInclude(CombatSequenceStepKind kind) => kind switch
        {
            // Environment hazards: own paragraph of action (+ optional damage) + per-target status prose.
            CombatSequenceStepKind.Attacker => !IsEnvironmental,
            CombatSequenceStepKind.Outcome => !IsEnvironmental,
            CombatSequenceStepKind.Roll => !IsEnvironmental,
            CombatSequenceStepKind.Action => true,
            CombatSequenceStepKind.Critical => !IsEnvironmental && IncludeCritical,
            CombatSequenceStepKind.CriticalMiss => !IsEnvironmental && IncludeCriticalMiss,
            CombatSequenceStepKind.Defense => !IsEnvironmental && IncludeDefense,
            CombatSequenceStepKind.Damage => IncludeDamage && !IsHeal,
            CombatSequenceStepKind.Heal => !IsEnvironmental && IncludeDamage && IsHeal,
            CombatSequenceStepKind.Effect => IncludeEffects,
            CombatSequenceStepKind.Stance => !IsEnvironmental && IncludeStance,
            CombatSequenceStepKind.Tempo => !IsEnvironmental && IncludeTempo,
            _ => false
        };

        /// <summary>
        /// HUD-facing outcome labels (math strip). Narrative prose uses <see cref="InterpretOutcome"/>.
        /// </summary>
        internal static string ResolveOutcomeLabel(ActionExecutionResult result)
        {
            if (result.IsCriticalMiss)
                return "CRIT MISS";
            if (!result.Hit)
                return "MISS";
            if (result.IsCritical && result.IsCombo)
                return "CRIT COMBO";
            if (result.IsCritical)
                return "CRIT";
            if (result.IsCombo)
                return "COMBO";
            return "HIT";
        }

        public static string PlainFromColored(IReadOnlyList<ColoredText>? segments)
        {
            if (segments == null || segments.Count == 0)
                return string.Empty;
            return ColoredTextRenderer.RenderAsPlainText(segments.ToList());
        }
    }
}
