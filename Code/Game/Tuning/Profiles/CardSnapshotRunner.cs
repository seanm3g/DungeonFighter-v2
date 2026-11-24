using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using RPGGame.ActionInteractionLab;
using RPGGame.Combat.Events;
using RPGGame.Data;

namespace RPGGame.Tuning.Profiles
{
    /// <summary>
    /// CARDSNAP (card rules plan phase 0, Documentation/CARD_RULES_PLAN_2026-11-23.md): what every card does today, on
    /// fixed seeds, as text. Each hero card runs in two strips (card first, card last, weapon basics around it) for a few
    /// seeded fights. The record is the per-event combat log plus a state line whenever HP, attributes, stores (statuses,
    /// resources, currencies on both sides) or queued grants change. Two runs of the same code are bit-identical, so a
    /// later run that differs on a card means that card's behaviour changed.
    /// <code>DF.exe CARDSNAP [--level 30] [--fights 3] [--seed 1] [--card NAME] [--out dir] [--compare old-summary.tsv]</code>
    /// </summary>
    public static class CardSnapshotRunner
    {
        private static readonly Dictionary<string, string> WeaponOfTag = new(StringComparer.OrdinalIgnoreCase)
        {
            ["mace"] = "Mace", ["sword"] = "Sword", ["dagger"] = "Dagger", ["wand"] = "Wand",
            ["barbarian"] = "Mace", ["warrior"] = "Sword", ["rogue"] = "Dagger", ["wizard"] = "Wand"
        };

        public static async Task<int> RunAsync(string[] args)
        {
            int level = TuningCliArgs.GetIntFlag(args, "--level") ?? 30;
            int fights = TuningCliArgs.GetIntFlag(args, "--fights") ?? 3;
            int seed = TuningCliArgs.GetSeedFlag(args) ?? 1;
            string? only = TuningCliArgs.GetFlagValue(args, "--card");
            string outDir = TuningCliArgs.GetFlagValue(args, "--out") ?? Path.Combine("GameData", "TuningRuns", "cardsnap");
            string? compare = TuningCliArgs.GetFlagValue(args, "--compare");
            Directory.CreateDirectory(outDir);

            ActionLoader.LoadActions();
            var cards = SequenceLabRunner.HeroActions()
                .Where(a => only == null || a.Name.Equals(only, StringComparison.OrdinalIgnoreCase))
                .ToList();
            Console.WriteLine($"Balance patch: {FundamentalsMatrixRunner.ActiveBalancePatchPath()}");
            Console.WriteLine($"=== CARDSNAP L{level} | {cards.Count} cards × 2 strips × {fights} fights | seed {seed} ===");

            // Every material currency starts at 5 so converts and cash-ins have something to spend.
            var seedBank = ActionLabEncounterSimulator.PlayerMaterialBankSeed;
            seedBank.Clear();
            foreach (var build in MaterialBuildsLoader.GetAll())
                if (!string.IsNullOrWhiteSpace(build.Keyword))
                    seedBank[build.Keyword] = 5;

            var probe = new StateProbe();
            var summary = new StringBuilder();
            var full = new StringBuilder();
            ActionLabEncounterSimulator.CaptureEventLog = true;
            // Long fights: the hero survives ~20 enemy swings and the foe ~20 hero turns, so effects get to show.
            double heroScale = ActionLabEncounterSimulator.PlayerHealthScale, foeScale = ActionLabEncounterSimulator.EnemyHealthScale;
            ActionLabEncounterSimulator.PlayerHealthScale = 20;
            ActionLabEncounterSimulator.EnemyHealthScale = 0.3;
            try
            {
                foreach (var card in cards)
                {
                    var section = new StringBuilder();
                    string weapon = WeaponFor(card);
                    // Filler: two plain weapon basics (no effects; a strip holds each card once).
                    var basics = new[] { "SLAM", "STRIKE", "STAB", "MAGIC MISSILE" }.Where(b => !b.Equals(card.Name, StringComparison.OrdinalIgnoreCase)).ToList();
                    string basic = SequenceLabRunner.WeaponBasics[weapon];
                    if (basic.Equals(card.Name, StringComparison.OrdinalIgnoreCase))
                        basic = basics[0];
                    string basic2 = basics.First(b => !b.Equals(basic, StringComparison.OrdinalIgnoreCase));
                    // A material's convert card needs that set formed: typical gear, every piece that material.
                    string? material = (card.Tags ?? new List<string>()).Select(t => MaterialBuildsLoader.FindByMaterial(t.Trim())?.Material)
                        .FirstOrDefault(m => m != null);
                    section.AppendLine($"## {card.Name} | {weapon}" + (material != null ? $" | {material} gear" : ""));
                    foreach (var strip in new[] { new[] { card.Name, basic, basic2 }, new[] { basic, basic2, card.Name } })
                    {
                        section.AppendLine("strip " + string.Join(" > ", strip));
                        await RunStrip(section, probe, weapon, material, level, strip, fights, seed).ConfigureAwait(false);
                    }
                    string text = section.ToString();
                    full.Append(text);
                    summary.AppendLine($"{card.Name}\t{Hash(text)}");
                }

                // Enemy pass: each kit template (EnemyActions.json) as the Goblin's only action, against a Sword hero with
                // plain basics. Kits use the same effect columns as hero cards.
                var kits = EnemyActionCatalog.TemplateNames
                    .Where(t => only == null || t.Equals(only, StringComparison.OrdinalIgnoreCase))
                    .OrderBy(t => t, StringComparer.OrdinalIgnoreCase).ToList();
                Console.WriteLine($"Enemy pass: {kits.Count} kit templates");
                foreach (string kit in kits)
                {
                    var section = new StringBuilder();
                    section.AppendLine($"## enemy:{kit} | Goblin vs Sword");
                    if (EnemyActionCatalog.TryCreate(kit) == null)
                    {
                        section.AppendLine("  kit did not build");
                    }
                    else
                    {
                        ActionLabEncounterSimulator.EnemySetup = enemy =>
                        {
                            enemy.ActionPool.Clear();
                            enemy.AddAction(EnemyActionCatalog.TryCreate(kit)!, 1.0);
                            enemy.InitializeActionSequence();
                        };
                        try
                        {
                            await RunStrip(section, probe, "Sword", null, level, new[] { "STRIKE", "SLAM", "STAB" }, fights, seed).ConfigureAwait(false);
                        }
                        finally
                        {
                            ActionLabEncounterSimulator.EnemySetup = null;
                        }
                    }
                    string text = section.ToString();
                    full.Append(text);
                    summary.AppendLine($"enemy:{kit}\t{Hash(text)}");
                }
            }
            finally
            {
                ActionLabEncounterSimulator.EnemySetup = null;
                ActionLabEncounterSimulator.CaptureEventLog = false;
                ActionLabEncounterSimulator.PlayerHealthScale = heroScale;
                ActionLabEncounterSimulator.EnemyHealthScale = foeScale;
                seedBank.Clear();
                probe.Detach();
            }

            string summaryPath = Path.Combine(outDir, "cardsnap-summary.tsv");
            File.WriteAllText(Path.Combine(outDir, "cardsnap.txt"), full.ToString());
            File.WriteAllText(summaryPath, summary.ToString());
            Console.WriteLine($"Wrote {Path.Combine(outDir, "cardsnap.txt")} and {summaryPath}");

            if (compare != null)
                return Compare(compare, summaryPath);
            return 0;
        }

        private static async Task RunStrip(StringBuilder section, StateProbe probe, string weapon, string? material, int level, string[] strip, int fights, int seed)
        {
            var config = new SimulationProfileConfig
            {
                Mode = TuningSimulationModes.FundamentalsEncounter,
                PlayerLevel = level,
                EnemyLevel = level,
                WeaponType = weapon,
                EnemyType = "Goblin",
                Gear = material != null ? "typical" : "none"
            };
            LabCombatSnapshot snapshot;
            FundamentalsCombatSetup.ForcedMaterial = material;
            try { snapshot = FundamentalsCombatSetup.BuildSnapshot(config, strip); }
            catch (Exception ex) { section.AppendLine("  setup error: " + ex.Message); return; }
            finally { FundamentalsCombatSetup.ForcedMaterial = null; }

            ActionLabEncounterSimulator.UseNaturalComboRouting = true;
            try
            {
                for (int i = 0; i < fights; i++)
                {
                    probe.Begin();
                    var rng = new Random(ActionLabEncounterSimulator.DeriveEncounterRandomSeed(seed, i));
                    var report = await ActionLabEncounterSimulator.RunBatchAsync(snapshot, 1, rng, maxDegreeOfParallelism: 1).ConfigureAwait(false);
                    var states = probe.End();
                    var e = report.Encounters.FirstOrDefault();
                    section.AppendLine($" fight {i + 1}: {(e == null ? "no result" : e.ErrorMessage ?? (e.PlayerWon ? "won" : "lost"))}");
                    foreach (string line in e?.EventLog ?? new List<string>())
                        section.AppendLine("  " + line);
                    foreach (string line in states)
                        section.AppendLine("  = " + line);
                }
            }
            finally
            {
                ActionLabEncounterSimulator.UseNaturalComboRouting = false;
            }
        }

        private static string WeaponFor(Action card)
        {
            foreach (string tag in card.Tags ?? new List<string>())
                if (WeaponOfTag.TryGetValue(tag.Trim(), out string? w))
                    return w;
            return "Sword";
        }

        private static string Hash(string text) =>
            Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(text)))[..16];

        private static int Compare(string oldPath, string newPath)
        {
            static Dictionary<string, string> Read(string p) => File.ReadAllLines(p)
                .Select(l => l.Split('\t')).Where(p => p.Length == 2)
                .GroupBy(p => p[0]).ToDictionary(g => g.Key, g => g.First()[1]);
            var a = Read(oldPath);
            var b = Read(newPath);
            var changed = a.Keys.Union(b.Keys).OrderBy(k => k)
                .Where(k => !a.TryGetValue(k, out var x) || !b.TryGetValue(k, out var y) || x != y).ToList();
            Console.WriteLine(changed.Count == 0
                ? $"CARDSNAP compare: all {b.Count} cards identical"
                : $"CARDSNAP compare: {changed.Count} of {b.Count} cards differ: {string.Join(", ", changed)}");
            return changed.Count == 0 ? 0 : 1;
        }

        /// <summary>Writes a state line on every combat event whose state differs from the last one written.</summary>
        private sealed class StateProbe
        {
            private CombatEventBus? _bus;
            private readonly List<string> _lines = new();
            private string _last = "";
            private Character? _hero;
            private Enemy? _enemy;

            public void Begin()
            {
                _lines.Clear();
                _last = "";
                _hero = null;
                _enemy = null;
                Attach();
            }

            public List<string> End() => new(_lines);

            private void Attach()
            {
                var bus = CombatEventBus.Instance;
                if (ReferenceEquals(bus, _bus))
                {
                    // Already on this bus; Clear() would have dropped us, so check one handler.
                    if (bus.HasSubscriber(CombatEventType.ActionHit, OnEvent))
                        return;
                }
                else
                {
                    Detach();
                    _bus = bus;
                    bus.Cleared += Resubscribe;
                }
                Resubscribe();
            }

            private void Resubscribe()
            {
                if (_bus == null)
                    return;
                foreach (CombatEventType t in Enum.GetValues(typeof(CombatEventType)))
                {
                    _bus.Unsubscribe(t, OnEvent);
                    _bus.Subscribe(t, OnEvent);
                }
            }

            public void Detach()
            {
                if (_bus == null)
                    return;
                foreach (CombatEventType t in Enum.GetValues(typeof(CombatEventType)))
                    _bus.Unsubscribe(t, OnEvent);
                _bus.Cleared -= Resubscribe;
                _bus = null;
            }

            private void OnEvent(CombatEvent evt)
            {
                foreach (var actor in new[] { evt.Source, evt.Target })
                {
                    if (actor is Enemy en) _enemy = en;
                    else if (actor is Character c) _hero = c;
                }
                if (_hero == null)
                    return;
                string state = $"H {_hero.CurrentHealth}/{_hero.MaxHealth} STR {_hero.GetEffectiveStrength()} AGI {_hero.GetEffectiveAgility()} "
                    + $"TEC {_hero.GetEffectiveTechnique()} INT {_hero.GetEffectiveIntelligence()} {StoresOf(_hero)} {Grants(_hero)}"
                    + (_enemy == null ? "" : $" | E {_enemy.CurrentHealth} {StoresOf(_enemy)}");
                if (state == _last)
                    return;
                _last = state;
                _lines.Add($"{evt.Type}: {state}");
            }

            private static string StoresOf(Actor actor)
            {
                var parts = new List<string>();
                foreach (var def in Stores.All)
                {
                    int n;
                    try { n = Stores.Get(actor, def.Id); }
                    catch { continue; }
                    if (n != 0)
                        parts.Add($"{def.Id}={n}");
                }
                return "[" + string.Join(",", parts) + "]";
            }

            private static string Grants(Character hero)
            {
                var turn = hero.Effects.SnapshotTurnBonuses()
                    .Select(g => $"{g.CadenceType}x{g.Count}:" + string.Join("+", g.Bonuses.Select(b => $"{b.Type}{b.Value:0.##}")));
                var slots = hero.Effects.GetPendingActionBonusSlots().OrderBy(s => s)
                    .Select(s => $"s{s}:" + string.Join("+", hero.Effects.GetPendingActionBonusesForSlot(s).Select(b => $"{b.Type}{b.Value:0.##}")));
                var bank = hero.Effects.PeekNextActionBank().Select(b => $"{b.Type}{b.Value:0.##}");
                return "T[" + string.Join(";", turn) + "] S[" + string.Join(";", slots) + "] N[" + string.Join("+", bank) + "]";
            }
        }
    }
}
