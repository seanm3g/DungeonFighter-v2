using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using RPGGame.Tests;

namespace RPGGame.Tests.Unit
{
    /// <summary>Tests for FlavorText reload/save, bank catalog, and sample generation.</summary>
    public static class FlavorTextBankCatalogTests
    {
        private static int _testsRun;
        private static int _testsPassed;
        private static int _testsFailed;

        public static void RunAllTests()
        {
            Console.WriteLine("=== FlavorText / Bank Catalog Tests ===\n");
            _testsRun = _testsPassed = _testsFailed = 0;

            Load_Includes_CombatNarrative_Keys_Beyond_Legacy_Subset();
            EnumerateBanks_Includes_Names_And_Combat();
            GetSetBank_RoundTrips_Entries();
            Multiline_Conversion_RoundTrips();
            GenerateSample_CharacterName_And_CombatPlaceholders();
            CombatNarratives_Dictionary_Json_RoundTrip();
            NarrativeTextProvider_Reads_From_Dictionary();
            SaveReload_TempFile_Preserves_Banks();
            FormsAndCategories_Json_RoundTrip();
            ExtractCategoryRefs_And_EnsureCategories();
            ExpandFormTemplate_Fills_And_Keeps_Missing();
            GetSetCategory_RoundTrips();
            GenerateFormMany_Produces_Lines();
            TrySanitizeId_Accepts_And_Rejects();

            TestBase.PrintSummary("FlavorText Bank Catalog Tests", _testsRun, _testsPassed, _testsFailed);
        }

        private static void Load_Includes_CombatNarrative_Keys_Beyond_Legacy_Subset()
        {
            Console.WriteLine("--- Combat narrative dictionary load ---");
            FlavorText.Reload();
            var data = FlavorText.GetData();
            TestBase.AssertTrue(data.CombatNarratives.Count > 0,
                "CombatNarratives should load entries from JSON",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
            TestBase.AssertTrue(data.CombatNarratives.ContainsKey("criticalMiss"),
                "criticalMiss should round-trip (was dropped by legacy typed model)",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
            TestBase.AssertTrue(data.CombatNarratives.ContainsKey("environmentalAction"),
                "environmentalAction should be present",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        private static void EnumerateBanks_Includes_Names_And_Combat()
        {
            Console.WriteLine("--- Enumerate banks ---");
            var banks = FlavorTextBankCatalog.EnumerateBanks();
            TestBase.AssertTrue(banks.Any(b => b.PathId == "names.characterFirstNames"),
                "Should include first names bank",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
            TestBase.AssertTrue(banks.Any(b => b.PathId.StartsWith("combatNarratives.", StringComparison.Ordinal)),
                "Should include combat narrative banks",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
            TestBase.AssertTrue(banks.Any(b => b.PathId.Contains("locationDescriptions", StringComparison.Ordinal)),
                "Should include location description banks",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        private static void GetSetBank_RoundTrips_Entries()
        {
            Console.WriteLine("--- Get/Set bank ---");
            var data = new FlavorTextData
            {
                Names = { CharacterFirstNames = new[] { "A", "B" } },
                CombatNarratives = { ["criticalHit"] = new[] { "{name} hits!" } }
            };
            data.Environments.LocationDescriptions["Forest"] = new[] { "Trees." };

            TestBase.AssertEqual(2, FlavorTextBankCatalog.GetBank(data, "names.characterFirstNames").Length,
                "GetBank first names",
                ref _testsRun, ref _testsPassed, ref _testsFailed);

            FlavorTextBankCatalog.SetBank(data, "names.characterFirstNames", new[] { "X", "Y", "Z" });
            TestBase.AssertEqual(3, data.Names.CharacterFirstNames.Length,
                "SetBank first names",
                ref _testsRun, ref _testsPassed, ref _testsFailed);

            FlavorTextBankCatalog.SetBank(data, "combatNarratives.criticalHit", new[] { "Boom {name}" });
            TestBase.AssertEqual("Boom {name}", data.CombatNarratives["criticalHit"][0],
                "SetBank combat narrative",
                ref _testsRun, ref _testsPassed, ref _testsFailed);

            FlavorTextBankCatalog.SetBank(data, "environments.locationDescriptions.Forest", new[] { "Moss." });
            TestBase.AssertEqual("Moss.", data.Environments.LocationDescriptions["Forest"][0],
                "SetBank location description",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        private static void Multiline_Conversion_RoundTrips()
        {
            Console.WriteLine("--- Multiline conversion ---");
            var entries = new[] { "one", "two", "three" };
            string text = FlavorTextBankCatalog.EntriesToMultiline(entries);
            var back = FlavorTextBankCatalog.MultilineToEntries(text);
            TestBase.AssertEqual(3, back.Length, "Multiline split count",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
            TestBase.AssertEqual("two", back[1], "Multiline middle entry",
                ref _testsRun, ref _testsPassed, ref _testsFailed);

            var withBlank = FlavorTextBankCatalog.MultilineToEntries("a\n\n\nb\n");
            TestBase.AssertEqual(2, withBlank.Length, "Blank lines skipped",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        private static void GenerateSample_CharacterName_And_CombatPlaceholders()
        {
            Console.WriteLine("--- Generate samples ---");
            var data = new FlavorTextData
            {
                Names =
                {
                    CharacterFirstNames = new[] { "Aric" },
                    CharacterLastNames = new[] { "Stormrider" }
                },
                CombatNarratives =
                {
                    ["criticalHit"] = new[] { "{name} strikes {enemy}!" }
                }
            };
            data.Environments.LocationNames = new[] { "Darkwood" };
            data.Environments.LocationDescriptions["Forest"] = new[] { "Tall pines." };
            data.Environments.RoomContexts["Forest"] = new Dictionary<string, string[]>
            {
                ["boss"] = new[] { " A boss chamber." }
            };
            data.ClassQualifiers.BarbarianQualifiers = new[] { "the Berserker" };

            string name = FlavorTextBankCatalog.GenerateSample(FlavorTextGenerateMode.CharacterName, data);
            TestBase.AssertEqual("Aric Stormrider", name, "Character name sample",
                ref _testsRun, ref _testsPassed, ref _testsFailed);

            string loc = FlavorTextBankCatalog.GenerateSample(FlavorTextGenerateMode.LocationName, data);
            TestBase.AssertEqual("Darkwood", loc, "Location name sample",
                ref _testsRun, ref _testsPassed, ref _testsFailed);

            string combat = FlavorTextBankCatalog.GenerateSample(
                FlavorTextGenerateMode.CombatNarrative, data, combatEventKey: "criticalHit");
            TestBase.AssertTrue(combat.Contains("Aric") && combat.Contains("Goblin Scout"),
                "Combat sample should fill sample placeholders",
                ref _testsRun, ref _testsPassed, ref _testsFailed);

            string filled = FlavorTextBankCatalog.FillSamplePlaceholders("{player} vs {enemy}: {effect}");
            TestBase.AssertTrue(filled.Contains("Aric Stormrider") && filled.Contains("falling rocks"),
                "FillSamplePlaceholders",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        private static void CombatNarratives_Dictionary_Json_RoundTrip()
        {
            Console.WriteLine("--- CombatNarratives JSON round-trip ---");
            var original = new FlavorTextData
            {
                CombatNarratives =
                {
                    ["firstBlood"] = new[] { "Blood!" },
                    ["criticalMiss"] = new[] { "{name} misses!" },
                    ["playerTaunt_library"] = new[] { "Shh!" }
                }
            };
            string json = JsonSerializer.Serialize(original);
            var loaded = JsonSerializer.Deserialize<FlavorTextData>(json);
            TestBase.AssertNotNull(loaded, "Deserialized FlavorTextData",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
            TestBase.AssertTrue(loaded!.CombatNarratives.ContainsKey("criticalMiss"),
                "Dictionary preserves criticalMiss",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
            TestBase.AssertTrue(loaded.CombatNarratives.ContainsKey("playerTaunt_library"),
                "Dictionary preserves taunt keys",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        private static void NarrativeTextProvider_Reads_From_Dictionary()
        {
            Console.WriteLine("--- NarrativeTextProvider dictionary lookup ---");
            FlavorText.Reload();
            var provider = new NarrativeTextProvider();
            string hit = provider.GetRandomNarrative("criticalHit");
            TestBase.AssertTrue(!string.IsNullOrWhiteSpace(hit),
                "GetRandomNarrative(criticalHit) should return text from JSON",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
            string miss = provider.GetRandomNarrative("criticalMiss");
            TestBase.AssertTrue(!string.IsNullOrWhiteSpace(miss),
                "GetRandomNarrative(criticalMiss) should return text from JSON (not only fallback)",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        private static void SaveReload_TempFile_Preserves_Banks()
        {
            Console.WriteLine("--- Save/reload via temp path (serialize path used by SaveData shape) ---");
            // Avoid mutating live GameData: verify serialize → deserialize → GetBank path.
            var data = FlavorText.GetData();
            var firstNames = FlavorTextBankCatalog.GetBank(data, "names.characterFirstNames");
            TestBase.AssertTrue(firstNames.Length > 0, "Live first names bank non-empty",
                ref _testsRun, ref _testsPassed, ref _testsFailed);

            string tempDir = Path.Combine(Path.GetTempPath(), "df-flavortext-test-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(tempDir);
            string tempFile = Path.Combine(tempDir, GameConstants.FlavorTextJson);
            try
            {
                string json = JsonSerializer.Serialize(data, new JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText(tempFile, json);
                string readBack = File.ReadAllText(tempFile);
                var reloaded = JsonSerializer.Deserialize<FlavorTextData>(readBack);
                TestBase.AssertNotNull(reloaded, "Temp file deserialize",
                    ref _testsRun, ref _testsPassed, ref _testsFailed);
                var names = FlavorTextBankCatalog.GetBank(reloaded!, "names.characterFirstNames");
                TestBase.AssertEqual(firstNames.Length, names.Length,
                    "Temp round-trip preserves first-name count",
                    ref _testsRun, ref _testsPassed, ref _testsFailed);
                TestBase.AssertTrue(reloaded!.CombatNarratives.ContainsKey("criticalMiss"),
                    "Temp round-trip preserves combat keys",
                    ref _testsRun, ref _testsPassed, ref _testsFailed);
            }
            finally
            {
                try { Directory.Delete(tempDir, true); } catch { /* ignore */ }
            }
        }

        private static void FormsAndCategories_Json_RoundTrip()
        {
            Console.WriteLine("--- Forms/Categories JSON round-trip ---");
            var original = new FlavorTextData
            {
                Forms =
                {
                    ["roomEnter"] = new FlavorFormDefinition
                    {
                        DisplayName = "Room Enter",
                        Template = "Hi <intro> then <detail>."
                    }
                },
                Categories =
                {
                    ["intro"] = new[] { "Moss.", "Dust." },
                    ["detail"] = new[] { "A chest." }
                }
            };
            string json = JsonSerializer.Serialize(original);
            var loaded = JsonSerializer.Deserialize<FlavorTextData>(json);
            TestBase.AssertNotNull(loaded, "Deserialized forms data",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
            TestBase.AssertTrue(loaded!.Forms.ContainsKey("roomEnter"),
                "Forms preserves roomEnter",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
            TestBase.AssertEqual("Room Enter", loaded.Forms["roomEnter"].DisplayName,
                "Form displayName",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
            TestBase.AssertTrue(loaded.Categories.ContainsKey("intro"),
                "Categories preserves intro",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
            TestBase.AssertEqual(2, loaded.Categories["intro"].Length,
                "Intro entry count",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        private static void ExtractCategoryRefs_And_EnsureCategories()
        {
            Console.WriteLine("--- ExtractCategoryRefs / EnsureCategories ---");
            var refs = FlavorTextBankCatalog.ExtractCategoryRefs("A <intro> and <detail> and <intro> again.");
            TestBase.AssertEqual(2, refs.Count, "Unique refs count",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
            TestBase.AssertEqual("intro", refs[0], "First ref order",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
            TestBase.AssertEqual("detail", refs[1], "Second ref order",
                ref _testsRun, ref _testsPassed, ref _testsFailed);

            var data = new FlavorTextData();
            var created = FlavorTextBankCatalog.EnsureCategoriesForTemplate(data, "See <newTag> here.");
            TestBase.AssertEqual(1, created.Count, "Created one category",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
            TestBase.AssertTrue(data.Categories.ContainsKey("newTag"),
                "newTag category exists",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
            TestBase.AssertEqual(0, data.Categories["newTag"].Length,
                "newTag starts empty",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        private static void ExpandFormTemplate_Fills_And_Keeps_Missing()
        {
            Console.WriteLine("--- ExpandFormTemplate ---");
            var categories = new Dictionary<string, string[]>(StringComparer.OrdinalIgnoreCase)
            {
                ["intro"] = new[] { "ONLY_INTRO" },
                ["empty"] = Array.Empty<string>()
            };
            var rng = new Random(42);
            string filled = FlavorTextBankCatalog.ExpandFormTemplate(
                "Start <intro> mid <missing> end <empty>.", categories, rng);
            TestBase.AssertTrue(filled.Contains("ONLY_INTRO"),
                "Filled intro from category",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
            TestBase.AssertTrue(filled.Contains("<missing>"),
                "Missing category keeps tag",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
            TestBase.AssertTrue(filled.Contains("<empty>"),
                "Empty category keeps tag",
                ref _testsRun, ref _testsPassed, ref _testsFailed);

            string withCombat = FlavorTextBankCatalog.ExpandFormTemplate(
                "{player} sees <intro>", categories, new Random(1));
            TestBase.AssertTrue(withCombat.Contains("Aric Stormrider") && withCombat.Contains("ONLY_INTRO"),
                "Sample placeholders apply after category expand",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        private static void GetSetCategory_RoundTrips()
        {
            Console.WriteLine("--- Get/Set category ---");
            var data = new FlavorTextData();
            TestBase.AssertTrue(FlavorTextBankCatalog.SetCategory(data, "intro", new[] { "A", "B" }),
                "SetCategory succeeds",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
            TestBase.AssertEqual(2, FlavorTextBankCatalog.GetCategory(data, "intro").Length,
                "GetCategory count",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
            TestBase.AssertFalse(FlavorTextBankCatalog.SetCategory(data, "bad-id!", new[] { "x" }),
                "Rejects unsafe category id",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        private static void GenerateFormMany_Produces_Lines()
        {
            Console.WriteLine("--- GenerateFormMany ---");
            var data = new FlavorTextData
            {
                Forms =
                {
                    ["roomEnter"] = new FlavorFormDefinition
                    {
                        DisplayName = "Room Enter",
                        Template = "Line <intro>"
                    }
                },
                Categories = { ["intro"] = new[] { "Alpha" } }
            };
            string many = FlavorTextBankCatalog.GenerateFormMany(data, "roomEnter", 3, new Random(7));
            var lines = many.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
            TestBase.AssertEqual(3, lines.Length, "GenerateFormMany line count",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
            TestBase.AssertTrue(lines.All(l => l.Contains("Alpha")),
                "Each line filled from category",
                ref _testsRun, ref _testsPassed, ref _testsFailed);

            string none = FlavorTextBankCatalog.GenerateFormSample(data, "missingForm");
            TestBase.AssertEqual("(no form selected)", none, "Missing form message",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        private static void TrySanitizeId_Accepts_And_Rejects()
        {
            Console.WriteLine("--- TrySanitizeId ---");
            TestBase.AssertTrue(FlavorTextBankCatalog.TrySanitizeId(" room Enter ", out string id, out _),
                "Accepts spaced id",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
            TestBase.AssertEqual("room_Enter", id, "Spaces become underscores",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
            TestBase.AssertFalse(FlavorTextBankCatalog.TrySanitizeId("   ", out _, out string? err),
                "Rejects blank",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
            TestBase.AssertTrue(!string.IsNullOrEmpty(err), "Blank has error message",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
        }
    }
}
