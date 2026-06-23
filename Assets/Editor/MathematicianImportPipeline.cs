using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using PeopleOfMath.Data;
using UnityEditor;
using UnityEngine;
using UnityEngine.Networking;

namespace PeopleOfMath.Editor
{
    public static class MathematicianImportPipeline
    {
        const string CatalogPath = "Assets/Data/mathematicians_catalog.json";
        const string DataFolder = "Assets/Data/Mathematicians";
        const int MaxBlockChars = 9200;
        const int MaxShortBioChars = 350;
        const int RequestDelayMs = 800;
        const int MaxRetries = 3;
        static readonly HashSet<string> PreserveEnIds = new(StringComparer.OrdinalIgnoreCase)
        {
            "pythagoras", "euclid", "archimedes", "newton", "euler", "gauss",
            "lobachevsky", "kovalevskaya", "poincare", "turing"
        };

        [MenuItem("PeopleOfMath/Create Catalog Assets (skeleton)")]
        public static void CreateSkeletonAssets()
        {
            var root = LoadCatalog();
            if (root == null)
                return;
            foreach (var entry in root.mathematicians)
                CreateOrUpdateSkeleton(entry);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log($"Skeleton assets: {root.mathematicians.Count}");
        }

        [MenuItem("PeopleOfMath/Import Catalog (RU texts)")]
        public static void ImportCatalogMenu()
        {
            if (!EditorUtility.DisplayDialog(
                    "Import Catalog",
                    "Загрузить/обновить карточки из ru.wikipedia по каталогу? Существующий EN у 10 карточек сохранится.",
                    "Import",
                    "Cancel"))
                return;

            ImportCatalog();
        }

        [MenuItem("PeopleOfMath/Truncate Short Bios")]
        public static void TruncateShortBiosMenu()
        {
            if (!EditorUtility.DisplayDialog(
                    "Truncate Short Bios",
                    $"Обрезать shortBioRu/shortBioEn до {MaxShortBioChars} символов во всех карточках?",
                    "Truncate",
                    "Cancel"))
                return;

            TruncateAllShortBios();
        }

        public static void TruncateAllShortBios()
        {
            var guids = AssetDatabase.FindAssets("t:MathematicianData", new[] { DataFolder });
            var updated = 0;

            foreach (var guid in guids)
            {
                var data = AssetDatabase.LoadAssetAtPath<MathematicianData>(
                    AssetDatabase.GUIDToAssetPath(guid));
                if (data == null)
                    continue;

                var changed = false;
                if (TruncateField(ref data.shortBioRu))
                    changed = true;
                if (TruncateField(ref data.shortBioEn))
                    changed = true;

                if (!changed)
                    continue;

                EditorUtility.SetDirty(data);
                updated++;
            }

            AssetDatabase.SaveAssets();
            Debug.Log($"Short bios truncated on {updated} mathematician assets.");
        }

        static bool TruncateField(ref string field)
        {
            if (string.IsNullOrEmpty(field) || field.Length <= MaxShortBioChars)
                return false;

            field = Truncate(field, MaxShortBioChars);
            return true;
        }

        public static void ImportCatalog()
        {
            var root = LoadCatalog();
            if (root == null)
                return;

            if (!Directory.Exists(DataFolder))
                Directory.CreateDirectory(DataFolder);

            var ok = 0;
            var fail = 0;
            for (var i = 0; i < root.mathematicians.Count; i++)
            {
                var entry = root.mathematicians[i];
                EditorUtility.DisplayProgressBar(
                    "Wikipedia RU",
                    entry.wikiTitleRu,
                    (float)i / root.mathematicians.Count);

                try
                {
                    ImportOne(entry);
                    ok++;
                }
                catch (Exception ex)
                {
                    fail++;
                    Debug.LogWarning($"[{entry.id}] {ex.Message}");
                }

                System.Threading.Thread.Sleep(RequestDelayMs);
            }

            EditorUtility.ClearProgressBar();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            MathematicianAssetYamlSync.SyncAllMathematicianAssets();
            Debug.Log($"Import catalog done: {ok} ok, {fail} failed.");
        }

        static void ImportOne(MathematicianCatalogEntry entry)
        {
            var path = $"{DataFolder}/{entry.id}.asset";
            var data = AssetDatabase.LoadAssetAtPath<MathematicianData>(path);
            var preserveEn = data != null && PreserveEnIds.Contains(entry.id);
            var enSnapshot = preserveEn ? SnapshotEn(data) : null;

            if (data == null)
            {
                data = ScriptableObject.CreateInstance<MathematicianData>();
                AssetDatabase.CreateAsset(data, path);
            }

            data.id = entry.id;
            data.wikiTitleRu = entry.wikiTitleRu;
            data.wikidataId = entry.wikidataId ?? "";
            data.countryKeys = entry.countryKeys ?? new List<string>();
            data.centuryKeys = entry.centuryKeys ?? new List<string>();
            data.branchKeys = entry.branchKeys ?? new List<string>();

            var titleEnc = Uri.EscapeDataString(entry.wikiTitleRu.Replace(' ', '_'));
            var summaryJson = GetWithRetry($"https://ru.wikipedia.org/api/rest_v1/page/summary/{titleEnc}");
            var summary = JsonUtility.FromJson<WikiSummary>(summaryJson);

            if (summary != null)
            {
                data.fullNameRu = !string.IsNullOrWhiteSpace(summary.title)
                    ? summary.title
                    : entry.wikiTitleRu;
                data.wikipediaUrlRu = summary.content_urls?.desktop?.page ?? "";
                if (!string.IsNullOrWhiteSpace(summary.extract))
                    data.shortBioRu = Truncate(UnicodeText.Normalize(summary.extract.Trim()), MaxShortBioChars);
            }
            else
            {
                data.fullNameRu = entry.wikiTitleRu;
            }

            var extract = FetchExtract(entry.wikiTitleRu);
            if (!string.IsNullOrWhiteSpace(extract))
            {
                SplitExtract(extract, out var achievements, out var personal);
                if (!string.IsNullOrWhiteSpace(achievements))
                    data.achievementsRu = Truncate(achievements, MaxBlockChars);
                if (!string.IsNullOrWhiteSpace(personal))
                    data.personalLifeRu = Truncate(personal, MaxBlockChars);
                if (string.IsNullOrWhiteSpace(data.shortBioRu))
                    data.shortBioRu = Truncate(extract, MaxShortBioChars);
            }

            TryParseDates(summary?.extract ?? extract ?? "", data);

            if (preserveEn && enSnapshot != null)
                RestoreEn(data, enSnapshot);
            else if (!preserveEn)
            {
                data.fullNameEn = "";
                data.shortBioEn = "";
                data.achievementsEn = "";
                data.personalLifeEn = "";
                data.interestingFactsEn = "";
            }

            EditorUtility.SetDirty(data);
        }

        static EnSnapshot SnapshotEn(MathematicianData d) => new()
        {
            fullNameEn = d.fullNameEn,
            shortBioEn = d.shortBioEn,
            achievementsEn = d.achievementsEn,
            personalLifeEn = d.personalLifeEn,
            interestingFactsEn = d.interestingFactsEn
        };

        static void RestoreEn(MathematicianData d, EnSnapshot s)
        {
            d.fullNameEn = s.fullNameEn;
            d.shortBioEn = s.shortBioEn;
            d.achievementsEn = s.achievementsEn;
            d.personalLifeEn = s.personalLifeEn;
            d.interestingFactsEn = s.interestingFactsEn;
        }

        class EnSnapshot
        {
            public string fullNameEn;
            public string shortBioEn;
            public string achievementsEn;
            public string personalLifeEn;
            public string interestingFactsEn;
        }

        static string FetchExtract(string title)
        {
            var q = Uri.EscapeDataString(title);
            var url =
                $"https://ru.wikipedia.org/w/api.php?action=query&prop=extracts&explaintext=1&format=json&titles={q}";
            var json = GetWithRetry(url);
            if (string.IsNullOrEmpty(json))
                return "";

            var marker = "\"extract\":\"";
            var idx = json.IndexOf(marker, StringComparison.Ordinal);
            if (idx < 0)
                return "";

            idx += marker.Length;
            var sb = new StringBuilder();
            for (var i = idx; i < json.Length; i++)
            {
                var c = json[i];
                if (c == '\\' && i + 1 < json.Length)
                {
                    var n = json[++i];
                    if (n == 'u' && i + 4 < json.Length)
                    {
                        var hex = json.Substring(i + 1, 4);
                        if (int.TryParse(hex, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out var code))
                        {
                            sb.Append((char)code);
                            i += 4;
                            continue;
                        }
                    }

                    switch (n)
                    {
                        case 'n': sb.Append('\n'); break;
                        case 'r': sb.Append('\r'); break;
                        case 't': sb.Append('\t'); break;
                        case '"': sb.Append('"'); break;
                        case '\\': sb.Append('\\'); break;
                        default: sb.Append(n); break;
                    }
                }
                else if (c == '"')
                    break;
                else
                    sb.Append(c);
            }

            return UnicodeText.Normalize(sb.ToString().Trim());
        }

        static void SplitExtract(string extract, out string achievements, out string personal)
        {
            achievements = "";
            personal = "";
            var markers = new[]
            {
                "Научная деятельность", "Достижения", "Вклад", "Работы", "Труды",
                "Личная жизнь", "Биография", "Семья"
            };

            var bestIdx = -1;
            var bestKey = "";
            foreach (var m in markers)
            {
                var i = extract.IndexOf(m, StringComparison.OrdinalIgnoreCase);
                if (i > 40 && (bestIdx < 0 || i < bestIdx))
                {
                    bestIdx = i;
                    bestKey = m;
                }
            }

            if (bestIdx > 0)
            {
                achievements = extract.Substring(0, bestIdx).Trim();
                personal = extract.Substring(bestIdx).Trim();
            }
            else
            {
                var mid = extract.Length / 2;
                var split = extract.LastIndexOf('\n', Math.Min(extract.Length - 1, mid + 200));
                if (split < 100)
                    split = mid;
                achievements = extract.Substring(0, split).Trim();
                personal = extract.Substring(split).Trim();
            }
        }

        static void TryParseDates(string text, MathematicianData data)
        {
            if (string.IsNullOrWhiteSpace(text))
                return;

            var m = Regex.Match(
                text,
                @"(\d{1,4}(?:\s*до\s*н\.?\s*э\.?|\s*н\.?\s*э\.?)?)\s*[—–-]\s*(\d{1,4}(?:\s*до\s*н\.?\s*э\.?|\s*н\.?\s*э\.?)?)",
                RegexOptions.IgnoreCase);
            if (m.Success)
            {
                data.birthDate = m.Groups[1].Value.Trim();
                data.deathDate = m.Groups[2].Value.Trim();
                return;
            }

            m = Regex.Match(text, @"род(?:ился|\.)\s*[^0-9]*(\d{1,4}[^,\n\.]{0,20})");
            if (m.Success)
                data.birthDate = m.Groups[1].Value.Trim();
            m = Regex.Match(text, @"умер[^0-9]*(\d{1,4}[^,\n\.]{0,20})");
            if (m.Success)
                data.deathDate = m.Groups[1].Value.Trim();
        }

        static MathematicianCatalogRoot LoadCatalog()
        {
            var json = File.ReadAllText(CatalogPath, Encoding.UTF8);
            var root = JsonUtility.FromJson<MathematicianCatalogRoot>(json);
            if (root?.mathematicians == null || root.mathematicians.Count == 0)
            {
                Debug.LogError("Catalog empty or invalid.");
                return null;
            }
            return root;
        }

        static void CreateOrUpdateSkeleton(MathematicianCatalogEntry entry)
        {
            if (!Directory.Exists(DataFolder))
                Directory.CreateDirectory(DataFolder);

            var path = $"{DataFolder}/{entry.id}.asset";
            var data = AssetDatabase.LoadAssetAtPath<MathematicianData>(path);
            if (data == null)
            {
                data = ScriptableObject.CreateInstance<MathematicianData>();
                AssetDatabase.CreateAsset(data, path);
            }

            data.id = entry.id;
            data.wikiTitleRu = entry.wikiTitleRu;
            data.wikidataId = entry.wikidataId ?? "";
            data.countryKeys = entry.countryKeys ?? new List<string>();
            data.centuryKeys = entry.centuryKeys ?? new List<string>();
            data.branchKeys = entry.branchKeys ?? new List<string>();
            if (string.IsNullOrWhiteSpace(data.fullNameRu))
                data.fullNameRu = entry.wikiTitleRu;
            EditorUtility.SetDirty(data);
        }

        static string GetWithRetry(string url)
        {
            for (var attempt = 0; attempt < MaxRetries; attempt++)
            {
                try
                {
                    return Get(url);
                }
                catch (WebException ex) when (ex.Message.Contains("429") && attempt < MaxRetries - 1)
                {
                    System.Threading.Thread.Sleep(5000 * (attempt + 1));
                }
            }
            return Get(url);
        }

        static string Get(string url)
        {
            using var req = UnityWebRequest.Get(url);
            req.SetRequestHeader("User-Agent", "PeopleOfMath/1.0 (Unity Editor import; contact@example.com)");
            var op = req.SendWebRequest();
            while (!op.isDone)
                System.Threading.Thread.Sleep(20);

            if (req.result != UnityWebRequest.Result.Success)
                throw new WebException(req.error);

            return req.downloadHandler.text;
        }

        static string Truncate(string s, int max) =>
            string.IsNullOrEmpty(s) || s.Length <= max ? s : s.Substring(0, max).TrimEnd() + "…";

        [Serializable]
        class WikiSummary
        {
            public string title;
            public string extract;
            public WikiUrls content_urls;
        }

        [Serializable]
        class WikiUrls
        {
            public WikiDesktop desktop;
        }

        [Serializable]
        class WikiDesktop
        {
            public string page;
        }
    }
}
