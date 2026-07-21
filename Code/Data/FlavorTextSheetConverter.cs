using System;
using System.Collections.Generic;

namespace RPGGame.Data
{
    /// <summary>
    /// Flattens <see cref="FlavorTextData"/> into a long sheet table:
    /// <c>section | bank | key | text</c> (one string per data row).
    /// </summary>
    public static class FlavorTextSheetConverter
    {
        public const string HeaderSection = "section";
        public const string HeaderBank = "bank";
        public const string HeaderKey = "key";
        public const string HeaderText = "text";

        /// <summary>Builds header + data rows for OAuth push (full tab rewrite from A1).</summary>
        public static List<IList<object>> BuildPushValueRows(FlavorTextData? data)
        {
            data ??= new FlavorTextData();
            var rows = new List<IList<object>>
            {
                new List<object> { HeaderSection, HeaderBank, HeaderKey, HeaderText }
            };

            AppendNames(rows, data.Names);
            AppendItems(rows, data.Items);
            AppendEnvironments(rows, data.Environments);
            AppendClassQualifiers(rows, data.ClassQualifiers);
            AppendStringArrayMap(rows, "combatNarratives", data.CombatNarratives);
            AppendForms(rows, data.Forms);
            AppendStringArrayMap(rows, "categories", data.Categories);

            return rows;
        }

        private static void AppendNames(List<IList<object>> rows, NamesData? names)
        {
            names ??= new NamesData();
            AppendFlatBank(rows, "names", "characterFirstNames", names.CharacterFirstNames);
            AppendFlatBank(rows, "names", "characterLastNames", names.CharacterLastNames);
            AppendFlatBank(rows, "names", "bossNames", names.BossNames);
        }

        private static void AppendItems(List<IList<object>> rows, ItemsData? items)
        {
            items ??= new ItemsData();
            AppendFlatBank(rows, "items", "consumableNames", items.ConsumableNames);
        }

        private static void AppendEnvironments(List<IList<object>> rows, EnvironmentsData? env)
        {
            env ??= new EnvironmentsData();
            AppendFlatBank(rows, "environments", "locationNames", env.LocationNames);

            if (env.LocationDescriptions != null)
            {
                foreach (var kv in env.LocationDescriptions)
                {
                    string theme = kv.Key ?? "";
                    foreach (string line in kv.Value ?? Array.Empty<string>())
                        rows.Add(Row("environments", "locationDescriptions", theme, line));
                }
            }

            if (env.RoomContexts != null)
            {
                foreach (var themeKv in env.RoomContexts)
                {
                    string theme = themeKv.Key ?? "";
                    if (themeKv.Value == null)
                        continue;
                    foreach (var roomKv in themeKv.Value)
                    {
                        string roomType = roomKv.Key ?? "";
                        string key = string.IsNullOrEmpty(theme) && string.IsNullOrEmpty(roomType)
                            ? ""
                            : $"{theme}/{roomType}";
                        foreach (string line in roomKv.Value ?? Array.Empty<string>())
                            rows.Add(Row("environments", "roomContexts", key, line));
                    }
                }
            }
        }

        private static void AppendClassQualifiers(List<IList<object>> rows, ClassQualifiersData? cq)
        {
            cq ??= new ClassQualifiersData();
            var cn = cq.ClassNames ?? new ClassNamesData();
            AppendClassNameBank(rows, "barbarian", cn.Barbarian);
            AppendClassNameBank(rows, "warrior", cn.Warrior);
            AppendClassNameBank(rows, "rogue", cn.Rogue);
            AppendClassNameBank(rows, "wizard", cn.Wizard);
            AppendClassNameBank(rows, "fighter", cn.Fighter);

            AppendFlatBank(rows, "classQualifiers", "barbarianQualifiers", cq.BarbarianQualifiers);
            AppendFlatBank(rows, "classQualifiers", "warriorQualifiers", cq.WarriorQualifiers);
            AppendFlatBank(rows, "classQualifiers", "rogueQualifiers", cq.RogueQualifiers);
            AppendFlatBank(rows, "classQualifiers", "wizardQualifiers", cq.WizardQualifiers);
            AppendFlatBank(rows, "classQualifiers", "fighterQualifiers", cq.FighterQualifiers);
        }

        private static void AppendClassNameBank(List<IList<object>> rows, string classId, string[]? aliases)
        {
            foreach (string line in aliases ?? Array.Empty<string>())
                rows.Add(Row("classQualifiers", "classNames", classId, line));
        }

        private static void AppendForms(List<IList<object>> rows, Dictionary<string, FlavorFormDefinition>? forms)
        {
            if (forms == null)
                return;
            foreach (var kv in forms)
            {
                string formId = kv.Key ?? "";
                var def = kv.Value ?? new FlavorFormDefinition();
                rows.Add(Row("forms", formId, "displayName", def.DisplayName ?? ""));
                rows.Add(Row("forms", formId, "template", def.Template ?? ""));
            }
        }

        private static void AppendStringArrayMap(
            List<IList<object>> rows,
            string section,
            Dictionary<string, string[]>? map)
        {
            if (map == null)
                return;
            foreach (var kv in map)
            {
                string bank = kv.Key ?? "";
                foreach (string line in kv.Value ?? Array.Empty<string>())
                    rows.Add(Row(section, bank, "", line));
            }
        }

        private static void AppendFlatBank(
            List<IList<object>> rows,
            string section,
            string bank,
            string[]? values)
        {
            foreach (string line in values ?? Array.Empty<string>())
                rows.Add(Row(section, bank, "", line));
        }

        private static List<object> Row(string section, string bank, string key, string text) =>
            new List<object> { section, bank, key, text ?? "" };
    }
}
