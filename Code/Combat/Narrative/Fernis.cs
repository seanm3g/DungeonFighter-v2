using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using RPGGame.Combat.Events;

namespace RPGGame.Combat.Narrative
{
    /// <summary>
    /// Fernis D Man's moments (Documentation/CORE_DESIGN_PASS_2026-11-17.md, D9.1 swing moments, D9.5-D9.9 Fernis).
    /// </summary>
    public static class FernisMoments
    {
        public const string FightWon = "fightWon";
        public const string Comeback = "comeback";
        public const string Death = "death";
        public const string ChainComplete = "chainComplete";
        public const string ChainBroken = "chainBroken";
        public const string Overkill = "overkill";
        public const string CloseCall = "closeCall";
        public const string Threat = "threat";
        public const string NewConcept = "newConcept";
        public const string DungeonSummary = "dungeonSummary";
    }

    /// <summary><c>GameData/Fernis.json</c>: lines per moment, by class with an "any" fallback.</summary>
    public sealed class FernisLineBank
    {
        public sealed class Moment
        {
            [JsonPropertyName("big")] public bool Big { get; set; }
            [JsonPropertyName("lines")] public Dictionary<string, List<string>> Lines { get; set; } = new();
        }

        [JsonPropertyName("speaker")] public string Speaker { get; set; } = "Fernis";
        [JsonPropertyName("maxLinesPerSwing")] public int MaxLinesPerSwing { get; set; } = 1;
        [JsonPropertyName("smallChance")] public double SmallChance { get; set; } = 0.35;
        [JsonPropertyName("moments")] public Dictionary<string, Moment> Moments { get; set; } = new();

        private static FernisLineBank? _instance;

        public static FernisLineBank Instance => _instance ??= Load();

        public static void ResetForTests() => _instance = null;

        public static FernisLineBank Load()
        {
            try
            {
                string? path = JsonLoader.FindGameDataFile("Fernis.json");
                if (path == null || !File.Exists(path))
                    return new FernisLineBank();
                var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                return JsonSerializer.Deserialize<FernisLineBank>(File.ReadAllText(path), options) ?? new FernisLineBank();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error loading Fernis.json: {ex.Message}");
                return new FernisLineBank();
            }
        }

        public bool IsBig(string moment) => Moments.TryGetValue(moment, out var m) && m.Big;

        /// <summary>Lines said recently, per moment (oldest first); a pick skips them while the pool has others.</summary>
        private readonly Dictionary<string, List<string>> _recent = new();

        /// <summary>
        /// A line for the moment: the class's own lines and the "any" lines together, picked evenly, skipping the
        /// moment's last few lines (playtest 11-21: the same win line twice in two dungeons).
        /// </summary>
        public string? Pick(string moment, string? heroClass, Random rng)
        {
            if (!Moments.TryGetValue(moment, out var m))
                return null;
            var pool = new List<string>();
            if (!string.IsNullOrWhiteSpace(heroClass) && m.Lines.TryGetValue(heroClass.Trim().ToLowerInvariant(), out var own))
                pool.AddRange(own);
            if (m.Lines.TryGetValue("any", out var any))
                pool.AddRange(any);
            if (pool.Count == 0)
                return null;
            lock (_recent)
            {
                if (!_recent.TryGetValue(moment, out var recent))
                    _recent[moment] = recent = new List<string>();
                var fresh = pool.Where(l => !recent.Contains(l)).ToList();
                var from = fresh.Count > 0 ? fresh : pool;
                string line = from[rng.Next(from.Count)];
                recent.Remove(line);
                recent.Add(line);
                while (recent.Count > Math.Max(0, pool.Count - 1))
                    recent.RemoveAt(0);
                return line;
            }
        }

        /// <summary>Fills {token}s; unknown tokens are dropped so no brace reaches the screen.</summary>
        public static string Fill(string template, IReadOnlyDictionary<string, string>? tokens)
        {
            string s = template;
            if (tokens != null)
                foreach (var kv in tokens)
                    s = s.Replace("{" + kv.Key + "}", kv.Value);
            s = System.Text.RegularExpressions.Regex.Replace(s, @"\{[a-zA-Z]+\}", "").Replace("  ", " ").Trim();
            return s.Length > 0 ? char.ToUpperInvariant(s[0]) + s[1..] : s;
        }
    }

    /// <summary>
    /// The budget (D9 Fernis rules): at most <see cref="FernisLineBank.MaxLinesPerSwing"/> per hero swing; big moments
    /// always speak (within the budget), small ones roll <see cref="FernisLineBank.SmallChance"/>.
    /// </summary>
    public sealed class FernisNarrator
    {
        private readonly FernisLineBank _bank;
        private readonly Random _rng;
        private int _spokenThisSwing;

        public FernisNarrator(FernisLineBank? bank = null, Random? rng = null)
        {
            _bank = bank ?? FernisLineBank.Instance;
            _rng = rng ?? new Random();
        }

        public string Speaker => _bank.Speaker;

        /// <summary>The last line he said (for the shoulder sprite's speech bubble).</summary>
        public static string? LastLine { get; private set; }

        public void NewSwing() => _spokenThisSwing = 0;

        /// <summary>His line on the dungeon summary (D9.2): "Fernis: ..." with the run's best chain; null when he's off.</summary>
        public static string? SummaryLine(Character hero)
        {
            if (System.Environment.GetEnvironmentVariable("DF_FERNIS") == "0" || hero == null)
                return null;
            string? cls = hero.Progression?.GetPrimaryClassWeaponType() is WeaponType wt ? ClassTags.ClassOf(wt) : null;
            var narrator = new FernisNarrator();
            string? line = narrator.TrySay(FernisMoments.DungeonSummary, cls, new Dictionary<string, string>
            {
                ["hero"] = hero.Name ?? "you",
                ["links"] = Math.Max(1, hero.SessionStats?.HighestComboStep ?? 1).ToString()
            });
            return line == null ? null : $"{narrator.Speaker}: {line}";
        }

        public string? TrySay(string moment, string? heroClass, IReadOnlyDictionary<string, string>? tokens = null, bool ignoreBudget = false)
        {
            if (!ignoreBudget && _spokenThisSwing >= Math.Max(1, _bank.MaxLinesPerSwing))
                return null;
            if (!_bank.IsBig(moment) && _rng.NextDouble() >= _bank.SmallChance)
                return null;
            string? template = _bank.Pick(moment, heroClass, _rng);
            if (template == null)
                return null;
            _spokenThisSwing++;
            string line = FernisLineBank.Fill(template, tokens);
            LastLine = line;
            return line;
        }
    }

    /// <summary>
    /// One fight's swing moments (D9.1): chain broken (with cause), chain complete, overkill, close call, enemy threat,
    /// then the fight result (won / comeback / death). Listens to the combat bus for this fight's hero and enemy only,
    /// queues Fernis's lines and hands them to the log after each step, so a line never lands before its swing.
    /// </summary>
    public sealed class FernisFightSession : IDisposable
    {
        private readonly Character _hero;
        private readonly Character _enemy;
        private readonly FernisNarrator _narrator;
        private readonly string? _heroClass;
        private readonly List<string> _pending = new();
        private readonly Action<CombatEvent> _handler;
        private readonly object _lock = new();
        private bool _closeCall, _threat;
        private int _lowestHpPercent = 100;
        private int _heroSwings;

        /// <summary>Close-call line threshold (hero HP %).</summary>
        public const int CloseCallPercent = 20;

        /// <summary>An overkill is a killing blow worth at least this share of the enemy's max HP (playtest 11-21: 0.5 fired on most kills).</summary>
        public const double OverkillShare = 1.0;

        private static readonly CombatEventType[] Listened =
        {
            CombatEventType.ActionExecuted, CombatEventType.ActionMiss, CombatEventType.ActionHit, CombatEventType.ComboEnded,
            CombatEventType.EnemyDied, CombatEventType.HeroLowHealth, CombatEventType.StatusEffectApplied
        };

        public FernisFightSession(Character hero, Character enemy, FernisNarrator? narrator = null, bool subscribe = true)
        {
            _hero = hero;
            _enemy = enemy;
            _narrator = narrator ?? new FernisNarrator();
            _heroClass = hero.Progression?.GetPrimaryClassWeaponType() is WeaponType wt ? ClassTags.ClassOf(wt) : null;
            _handler = OnEvent;
            if (subscribe)
                foreach (var t in Listened)
                    CombatEventBus.Instance.Subscribe(t, _handler);
        }

        /// <summary>Fernis is on in live, unmuted fights and MCP play (never in sims) unless <c>DF_FERNIS=0</c>.</summary>
        public static bool Enabled =>
            ((!CombatUiMuteScope.IsMuted && !CombatManager.DisableCombatUIOutput) || McpPlay)
            && System.Environment.GetEnvironmentVariable("DF_FERNIS") != "0";

        /// <summary>Set by the MCP game wrapper: MCP play runs its fights muted like a sim, but an agent playing should
        /// still hear Fernis (his lines reach <c>NewEvents</c> through the swing log).</summary>
        public static bool McpPlay { get; set; }

        public IReadOnlyList<string> Pending { get { lock (_lock) return _pending.ToList(); } }

        /// <summary>Raised for each line written ("Fernis: ..."); the MCP swing log adds it to <c>NewEvents</c>.</summary>
        public static event System.Action<string>? LineSpoken;

        /// <summary>Called before queued lines are written. The MCP swing log writes each swing late (when the next one
        /// starts), so it flushes here and a line never lands above the swing it reacts to.</summary>
        public static System.Action? BeforeLines { get; set; }

        public int HeroSwings => _heroSwings;

        private Dictionary<string, string> Tokens(int links = 0, string cause = "") => new()
        {
            ["hero"] = (_hero.Name ?? "you").TrimEnd('.'),
            ["enemy"] = (_enemy.Name ?? "it").TrimEnd('.'),
            ["turns"] = Math.Max(1, _heroSwings) == 1 ? "one swing" : $"{_heroSwings} swings",
            ["links"] = links.ToString(),
            ["cause"] = cause,
            ["hp"] = _lowestHpPercent.ToString()
        };

        private void Say(string moment, Dictionary<string, string> tokens, bool ignoreBudget = false)
        {
            string? line = _narrator.TrySay(moment, _heroClass, tokens, ignoreBudget);
            if (line != null)
                lock (_lock)
                    _pending.Add(line);
        }

        /// <summary>Turns one bus event into a moment (public for tests).</summary>
        public void OnEvent(CombatEvent evt)
        {
            if (evt == null)
                return;
            bool fromHero = ReferenceEquals(evt.Source, _hero);
            bool fromEnemy = ReferenceEquals(evt.Source, _enemy);
            if (!fromHero && !fromEnemy)
                return;
            switch (evt.Type)
            {
                case CombatEventType.ActionExecuted when fromHero:
                    _heroSwings++;
                    _narrator.NewSwing();
                    break;
                // Only a chain that had built 2+ links counts as broken (a miss on the first link is just a miss).
                case CombatEventType.ActionMiss when fromHero && evt.IsChainBreak && _hero.ComboStep >= 2:
                    string cause = Stores.Get(_hero, "rattle") > 0 ? "Rattle" : evt.IsCriticalMiss ? "A natural 1" : "A missed roll";
                    Say(FernisMoments.ChainBroken, Tokens(cause: cause));
                    break;
                case CombatEventType.ComboEnded when fromHero:
                    int slots = _hero.GetComboActions()?.Count ?? 0;
                    if (slots >= 2 && evt.ComboStepReached >= slots)
                        Say(FernisMoments.ChainComplete, Tokens(links: evt.ComboStepReached));
                    break;
                case CombatEventType.EnemyDied when fromHero && ReferenceEquals(evt.Target, _enemy):
                    int max = Math.Max(1, _enemy.GetMaxHealthForPoisonDot());
                    if (evt.Damage >= max * OverkillShare)
                        Say(FernisMoments.Overkill, Tokens());
                    break;
                case CombatEventType.StatusEffectApplied when !string.IsNullOrWhiteSpace(evt.StatusEffectType):
                    // D9.3 NEW: the first time a status appears, it opens the Statuses rung and Fernis names it.
                    string id = CoreStatuses.Resolve(evt.StatusEffectType) ?? evt.StatusEffectType!;
                    bool first = !_hero.Unlocks.HasSeen("status", id);
                    _hero.Unlocks.OnStatusApplied(id);
                    if (first)
                    {
                        var tokens = Tokens();
                        tokens["status"] = CoreStatuses.Label(id);
                        Say(FernisMoments.NewConcept, tokens);
                    }
                    break;
                case CombatEventType.HeroLowHealth:
                case CombatEventType.ActionHit when fromEnemy:
                    TrackHeroHealth();
                    if (fromEnemy && !_threat && _enemy.ComboStep >= 3)
                    {
                        _threat = true;
                        Say(FernisMoments.Threat, Tokens(links: _enemy.ComboStep));
                    }
                    break;
            }
        }

        private void TrackHeroHealth()
        {
            int max = Math.Max(1, _hero.GetEffectiveMaxHealth());
            int pct = (int)Math.Round(100.0 * Math.Max(0, _hero.CurrentHealth) / max);
            _lowestHpPercent = Math.Min(_lowestHpPercent, pct);
            if (!_closeCall && _hero.IsAlive && pct <= CloseCallPercent)
            {
                _closeCall = true;
                Say(FernisMoments.CloseCall, Tokens());
            }
        }

        /// <summary>The fight's result line: death, a comeback after a close call, or a plain win.</summary>
        public void FinishFight()
        {
            TrackHeroHealth();
            bool resultIsBig = !_hero.IsAlive || (!_enemy.IsAlive && _closeCall);
            if (resultIsBig)
            {
                // The fight's result line takes the last swing's slot: a close-call line still waiting from that same
                // swing would land as two lines on one beat ("Low health! ..." then the death line, playtest 11-21).
                lock (_lock)
                    _pending.Clear();
            }
            if (!_hero.IsAlive)
                Say(FernisMoments.Death, Tokens(), ignoreBudget: true);
            else if (!_enemy.IsAlive)
                Say(_closeCall ? FernisMoments.Comeback : FernisMoments.FightWon, Tokens(), ignoreBudget: true);
        }

        /// <summary>Hands the queued lines to the log ("Fernis: ...").</summary>
        public void Flush()
        {
            List<string> lines;
            lock (_lock)
            {
                lines = _pending.ToList();
                _pending.Clear();
            }
            if (lines.Count > 0)
                BeforeLines?.Invoke();
            foreach (string line in lines)
            {
                string said = $"{_narrator.Speaker}: {line}";
                TextDisplayIntegration.DisplaySystem(said);
                LineSpoken?.Invoke(said);
            }
        }

        public void Dispose()
        {
            foreach (var t in Listened)
                CombatEventBus.Instance.Unsubscribe(t, _handler);
        }
    }
}
