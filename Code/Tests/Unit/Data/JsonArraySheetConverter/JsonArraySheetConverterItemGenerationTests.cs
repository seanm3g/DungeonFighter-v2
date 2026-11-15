using System;
using System.IO;
using System.Linq;
using System.Text.Json;
using RPGGame;
using RPGGame.Data;
using RPGGame.Data.ItemGeneration;
using RPGGame.Tests;
using SheetConverter = RPGGame.Data.JsonArraySheetConverter;

namespace RPGGame.Tests.Unit.Data.JsonArraySheetConverter
{
    /// <summary>
    /// Sheet push → CSV → pull round-trips for the item roller's data: ITEM MODS (ItemMods.json), RARITY
    /// (RarityTable.json) and the new WEAPONS / ARMOR columns (class, damage / defense ranges, implicit triggerName).
    /// Uses the live GameData files so a push writes everything the game reads.
    /// </summary>
    public static class JsonArraySheetConverterItemGenerationTests
    {
        public static void RunAll(ref int run, ref int pass, ref int fail)
        {
            ItemModsRoundTrip(ref run, ref pass, ref fail);
            RarityRoundTrip(ref run, ref pass, ref fail);
            WeaponColumnsRoundTrip(ref run, ref pass, ref fail);
            ArmorColumnsRoundTrip(ref run, ref pass, ref fail);
        }

        private static string? ReadGameData(string file)
        {
            string? path = JsonLoader.FindGameDataFile(file);
            return path != null && File.Exists(path) ? File.ReadAllText(path) : null;
        }

        private static string RoundTrip(string json, GameDataTabularSheetKind kind)
        {
            var rows = SheetConverter.BuildPushValueRows(json, kind);
            return SheetConverter.CsvToJsonArrayText(JsonArraySheetConverterTestHelpers.RowsToCsv(rows), kind);
        }

        private static string Signature(ItemModTable t) => string.Join("\n", t.Families.SelectMany(f => f.Tiers.Select(x =>
            string.Join("|", f.Family, f.Pool, f.Affix, f.Kind, f.StatType ?? "", f.Template ?? "", f.Description ?? "",
                string.Join(",", f.Slots ?? new()), f.Weight.ToString("R"), string.Join(",", f.Tags ?? new()), f.Integer,
                x.Keyword, x.Min.ToString("R"), x.Max.ToString("R"), x.Level, x.Weight.ToString("R"), x.MaxRarity ?? "",
                string.Join(",", (x.SlotLevel ?? new()).Select(kv => kv.Key + ":" + kv.Value)), string.Join(",", x.Slots ?? new())))));

        private static void ItemModsRoundTrip(ref int run, ref int pass, ref int fail)
        {
            TestBase.SetCurrentTestName(nameof(ItemModsRoundTrip));
            string? json = ReadGameData("ItemMods.json");
            TestBase.AssertNotNull(json, "ItemMods.json found", ref run, ref pass, ref fail);
            if (json == null)
                return;

            var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
            var before = JsonSerializer.Deserialize<ItemModTable>(json, options)!;
            var rows = SheetConverter.BuildPushValueRows(json, GameDataTabularSheetKind.ItemMods);
            int tierCount = before.Families.Sum(f => f.Tiers.Count);
            TestBase.AssertEqual(tierCount + 1, rows.Count, "one sheet row per tier (+ header)", ref run, ref pass, ref fail);

            var after = JsonSerializer.Deserialize<ItemModTable>(RoundTrip(json, GameDataTabularSheetKind.ItemMods), options)!;
            TestBase.AssertEqual(before.Families.Count, after.Families.Count, "family count survives", ref run, ref pass, ref fail);
            string a = Signature(before), b = Signature(after);
            string firstDiff = a.Split('\n').Zip(b.Split('\n')).FirstOrDefault(p => p.First != p.Second) is var (x, y) && x != null ? $" first diff: {x} ≠ {y}" : "";
            TestBase.AssertTrue(a == b, "every family and tier survives push → pull" + firstDiff, ref run, ref pass, ref fail);
        }

        private static void RarityRoundTrip(ref int run, ref int pass, ref int fail)
        {
            TestBase.SetCurrentTestName(nameof(RarityRoundTrip));
            string? json = ReadGameData("RarityTable.json");
            if (json == null)
            {
                TestBase.AssertNotNull(json, "RarityTable.json found", ref run, ref pass, ref fail);
                return;
            }
            var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
            var before = JsonSerializer.Deserialize<RarityData[]>(json, options)!;
            var after = JsonSerializer.Deserialize<RarityData[]>(RoundTrip(json, GameDataTabularSheetKind.Rarity), options)!;
            string Sig(RarityData r) => $"{r.Name}|{r.Weight}|{r.MinLevel}|{r.MinLines}|{r.MaxLines}|{r.Explode}";
            TestBase.AssertEqual(string.Join("\n", before.Select(Sig)), string.Join("\n", after.Select(Sig)),
                "rarity weights, unlock level, line counts and explode chance survive push → pull", ref run, ref pass, ref fail);
        }

        private static void WeaponColumnsRoundTrip(ref int run, ref int pass, ref int fail)
        {
            TestBase.SetCurrentTestName(nameof(WeaponColumnsRoundTrip));
            string? json = ReadGameData("Weapons.json");
            if (json == null)
                return;
            var header = SheetConverter.BuildPushValueRows(json, GameDataTabularSheetKind.Weapons)[0].Select(c => c?.ToString()).ToList();
            TestBase.AssertTrue(header.IndexOf("class") > 0 && header.IndexOf("damageMin") > 0 && header.IndexOf("damageMax") > 0
                    && header.IndexOf("class") < header.IndexOf("triggerName"),
                "WEAPONS push has class / damageMin / damageMax as named columns", ref run, ref pass, ref fail);

            var before = JsonSerializer.Deserialize<WeaponData[]>(json)!;
            var after = JsonSerializer.Deserialize<WeaponData[]>(RoundTrip(json, GameDataTabularSheetKind.Weapons))!;
            string Sig(WeaponData w) => $"{w.Name}|{w.Class}|{w.DamageMin}|{w.DamageMax}|{w.TriggerName}";
            var b = before.Select(Sig).OrderBy(s => s).ToList();
            var a = after.Select(Sig).OrderBy(s => s).ToList();
            TestBase.AssertTrue(b.SequenceEqual(a), "weapon class, damage range and implicit survive push → pull", ref run, ref pass, ref fail);
        }

        private static void ArmorColumnsRoundTrip(ref int run, ref int pass, ref int fail)
        {
            TestBase.SetCurrentTestName(nameof(ArmorColumnsRoundTrip));
            string? json = ReadGameData("Armor.json");
            if (json == null)
                return;
            var header = SheetConverter.BuildPushValueRows(json, GameDataTabularSheetKind.Armor)[0].Select(c => c?.ToString()).ToList();
            TestBase.AssertTrue(header.Contains("class") && header.Contains("defenseMin") && header.Contains("defenseMax"),
                "ARMOR push has class / defenseMin / defenseMax as named columns", ref run, ref pass, ref fail);

            var opts = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
            var before = JsonSerializer.Deserialize<ArmorData[]>(json, opts)!;
            var after = JsonSerializer.Deserialize<ArmorData[]>(RoundTrip(json, GameDataTabularSheetKind.Armor), opts)!;
            string Sig(ArmorData x) => $"{x.Slot}|{x.Name}|{x.Class}|{x.DefenseMin}|{x.DefenseMax}|{x.TriggerName}";
            var b = before.Select(Sig).OrderBy(s => s).ToList();
            var a = after.Select(Sig).OrderBy(s => s).ToList();
            TestBase.AssertTrue(b.SequenceEqual(a), "armor class, defense range and implicit survive push → pull", ref run, ref pass, ref fail);
        }
    }
}
