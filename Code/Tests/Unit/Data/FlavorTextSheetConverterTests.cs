using System;
using System.Collections.Generic;
using System.Linq;
using RPGGame;
using RPGGame.Data;
using RPGGame.Tests;

namespace RPGGame.Tests.Unit.Data
{
    public static class FlavorTextSheetConverterTests
    {
        public static void RunAllTests()
        {
            Console.WriteLine("=== FlavorTextSheetConverter Tests ===\n");

            int testsRun = 0, testsPassed = 0, testsFailed = 0;

            TestHeaderAndEmptyData(ref testsRun, ref testsPassed, ref testsFailed);
            TestFlatNamesBanks(ref testsRun, ref testsPassed, ref testsFailed);
            TestNestedEnvironments(ref testsRun, ref testsPassed, ref testsFailed);
            TestFormsAndCombatNarratives(ref testsRun, ref testsPassed, ref testsFailed);
            TestClassNamesMap(ref testsRun, ref testsPassed, ref testsFailed);

            TestBase.PrintSummary("FlavorTextSheetConverter Tests", testsRun, testsPassed, testsFailed);
        }

        private static void TestHeaderAndEmptyData(ref int testsRun, ref int testsPassed, ref int testsFailed)
        {
            TestBase.SetCurrentTestName(nameof(TestHeaderAndEmptyData));
            var rows = FlavorTextSheetConverter.BuildPushValueRows(null);
            TestBase.AssertTrue(rows.Count >= 1, "at least header", ref testsRun, ref testsPassed, ref testsFailed);
            TestBase.AssertEqual("section", rows[0][0]?.ToString(), "header section", ref testsRun, ref testsPassed, ref testsFailed);
            TestBase.AssertEqual("bank", rows[0][1]?.ToString(), "header bank", ref testsRun, ref testsPassed, ref testsFailed);
            TestBase.AssertEqual("key", rows[0][2]?.ToString(), "header key", ref testsRun, ref testsPassed, ref testsFailed);
            TestBase.AssertEqual("text", rows[0][3]?.ToString(), "header text", ref testsRun, ref testsPassed, ref testsFailed);
            TestBase.AssertEqual(1, rows.Count, "empty banks → header only", ref testsRun, ref testsPassed, ref testsFailed);
        }

        private static void TestFlatNamesBanks(ref int testsRun, ref int testsPassed, ref int testsFailed)
        {
            TestBase.SetCurrentTestName(nameof(TestFlatNamesBanks));
            var data = new FlavorTextData
            {
                Names = new NamesData
                {
                    CharacterFirstNames = new[] { "Aric", "Bran" },
                    CharacterLastNames = new[] { "Stone" },
                    BossNames = new[] { "Malakar" }
                }
            };
            var rows = FlavorTextSheetConverter.BuildPushValueRows(data);
            TestBase.AssertTrue(HasRow(rows, "names", "characterFirstNames", "", "Aric"), "first name Aric", ref testsRun, ref testsPassed, ref testsFailed);
            TestBase.AssertTrue(HasRow(rows, "names", "characterFirstNames", "", "Bran"), "first name Bran", ref testsRun, ref testsPassed, ref testsFailed);
            TestBase.AssertTrue(HasRow(rows, "names", "characterLastNames", "", "Stone"), "last name", ref testsRun, ref testsPassed, ref testsFailed);
            TestBase.AssertTrue(HasRow(rows, "names", "bossNames", "", "Malakar"), "boss name", ref testsRun, ref testsPassed, ref testsFailed);
        }

        private static void TestNestedEnvironments(ref int testsRun, ref int testsPassed, ref int testsFailed)
        {
            TestBase.SetCurrentTestName(nameof(TestNestedEnvironments));
            var data = new FlavorTextData
            {
                Environments = new EnvironmentsData
                {
                    LocationNames = new[] { "Ancient Forest" },
                    LocationDescriptions = new Dictionary<string, string[]>
                    {
                        ["Forest"] = new[] { "A dense forest." }
                    },
                    RoomContexts = new Dictionary<string, Dictionary<string, string[]>>
                    {
                        ["Forest"] = new Dictionary<string, string[]>
                        {
                            ["boss"] = new[] { " This ancient grove." }
                        }
                    }
                }
            };
            var rows = FlavorTextSheetConverter.BuildPushValueRows(data);
            TestBase.AssertTrue(HasRow(rows, "environments", "locationNames", "", "Ancient Forest"), "location name", ref testsRun, ref testsPassed, ref testsFailed);
            TestBase.AssertTrue(HasRow(rows, "environments", "locationDescriptions", "Forest", "A dense forest."), "location description", ref testsRun, ref testsPassed, ref testsFailed);
            TestBase.AssertTrue(HasRow(rows, "environments", "roomContexts", "Forest/boss", " This ancient grove."), "room context", ref testsRun, ref testsPassed, ref testsFailed);
        }

        private static void TestFormsAndCombatNarratives(ref int testsRun, ref int testsPassed, ref int testsFailed)
        {
            TestBase.SetCurrentTestName(nameof(TestFormsAndCombatNarratives));
            var data = new FlavorTextData
            {
                Forms = new Dictionary<string, FlavorFormDefinition>(StringComparer.OrdinalIgnoreCase)
                {
                    ["roomEnter"] = new FlavorFormDefinition
                    {
                        DisplayName = "Room Enter",
                        Template = "You step inside. <intro>"
                    }
                },
                CombatNarratives = new Dictionary<string, string[]>(StringComparer.OrdinalIgnoreCase)
                {
                    ["firstBlood"] = new[] { "The first drop of blood." }
                },
                Categories = new Dictionary<string, string[]>(StringComparer.OrdinalIgnoreCase)
                {
                    ["intro"] = new[] { "Moss coats the walls." }
                }
            };
            var rows = FlavorTextSheetConverter.BuildPushValueRows(data);
            TestBase.AssertTrue(HasRow(rows, "forms", "roomEnter", "displayName", "Room Enter"), "form displayName", ref testsRun, ref testsPassed, ref testsFailed);
            TestBase.AssertTrue(HasRow(rows, "forms", "roomEnter", "template", "You step inside. <intro>"), "form template", ref testsRun, ref testsPassed, ref testsFailed);
            TestBase.AssertTrue(HasRow(rows, "combatNarratives", "firstBlood", "", "The first drop of blood."), "combat narrative", ref testsRun, ref testsPassed, ref testsFailed);
            TestBase.AssertTrue(HasRow(rows, "categories", "intro", "", "Moss coats the walls."), "category", ref testsRun, ref testsPassed, ref testsFailed);
        }

        private static void TestClassNamesMap(ref int testsRun, ref int testsPassed, ref int testsFailed)
        {
            TestBase.SetCurrentTestName(nameof(TestClassNamesMap));
            var data = new FlavorTextData
            {
                ClassQualifiers = new ClassQualifiersData
                {
                    ClassNames = new ClassNamesData
                    {
                        Barbarian = new[] { "barbarian", "berserker" }
                    },
                    BarbarianQualifiers = new[] { "the Berserker" }
                }
            };
            var rows = FlavorTextSheetConverter.BuildPushValueRows(data);
            TestBase.AssertTrue(HasRow(rows, "classQualifiers", "classNames", "barbarian", "barbarian"), "class alias", ref testsRun, ref testsPassed, ref testsFailed);
            TestBase.AssertTrue(HasRow(rows, "classQualifiers", "classNames", "barbarian", "berserker"), "class alias 2", ref testsRun, ref testsPassed, ref testsFailed);
            TestBase.AssertTrue(HasRow(rows, "classQualifiers", "barbarianQualifiers", "", "the Berserker"), "qualifier", ref testsRun, ref testsPassed, ref testsFailed);
        }

        private static bool HasRow(List<IList<object>> rows, string section, string bank, string key, string text) =>
            rows.Skip(1).Any(r =>
                r.Count >= 4
                && string.Equals(r[0]?.ToString(), section, StringComparison.Ordinal)
                && string.Equals(r[1]?.ToString(), bank, StringComparison.Ordinal)
                && string.Equals(r[2]?.ToString(), key, StringComparison.Ordinal)
                && string.Equals(r[3]?.ToString(), text, StringComparison.Ordinal));
    }
}
