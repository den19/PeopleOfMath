using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using PeopleOfMath.Data;
using UnityEditor;
using UnityEngine;
namespace PeopleOfMath.Editor
{
    public static class WikimediaPortraitImporter
    {
        const string DataFolder = "Assets/Data/Mathematicians";
        const string ReportPath = "Assets/Data/import_report.txt";
        const int MaxImages = 4;
        const int MinImages = 2;
        const int MaxSide = 512;
        const int JpegQuality = 85;
        const int RequestDelayMs = 2500;

        static readonly Dictionary<string, List<CommonsFile>> CommonsCache =
            new(StringComparer.OrdinalIgnoreCase);

        static readonly string[] AllowedLicenseFragments =
        {
            "public domain", "cc-by-sa", "cc by-sa", "cc-by", "cc by",
            "pd-", "pd ", "gfdl", "creative commons", "cc0"
        };

        static readonly string[] SkipTitleFragments =
        {
            "signature", "diagram", "graph", "plot", "theorem", "formula", "commemorative",
            "stamp", "coin", "map", "facsimile", "manuscript", "banner", "memphis",
            "tasman", "madonna", "concert", "screw", "column closeup"
        };

        static readonly string[] BadNameFragments =
        {
            "tasman", "memphis", "madonna", "banner", "screw", "column", "rebel heart"
        };

        [MenuItem("PeopleOfMath/Import Portraits (Wikimedia)")]
        public static void ImportPortraitsMenu()
        {
            if (!EditorUtility.DisplayDialog(
                    "Wikimedia",
                    "Скачать портреты с Commons? Существующие реальные файлы сохраняются.",
                    "Import",
                    "Cancel"))
                return;

            ImportAllPortraits(forceReplace: false);
        }

        [MenuItem("PeopleOfMath/Import Real Portraits (replace placeholders)")]
        public static void ImportRealPortraitsMenu()
        {
            if (!EditorUtility.DisplayDialog(
                    "Wikimedia",
                    "Удалить заглушки и скачать реальные портреты для всех карточек? ~15–20 мин.",
                    "Import",
                    "Cancel"))
                return;

            ImportAllPortraits(forceReplace: true, finalize: true);
        }

        [MenuItem("PeopleOfMath/Import Portraits (empty folders only)")]
        public static void ImportEmptyFoldersMenu()
        {
            if (!EditorUtility.DisplayDialog(
                    "Wikimedia",
                    "Скачать портреты только для папок без ≥2 реальных JPEG? Медленно, с паузами при 429.",
                    "Import",
                    "Cancel"))
                return;

            ImportAllPortraits(forceReplace: false, onlyEmpty: true);
        }

        [MenuItem("PeopleOfMath/Resume Failed Portraits From Report")]
        public static void ResumeFailedFromReportMenu()
        {
            var ids = ParseFailedIdsFromReport();
            if (ids.Count == 0)
            {
                EditorUtility.DisplayDialog("Wikimedia", "В import_report.txt нет строк FAIL/WARN.", "OK");
                return;
            }

            if (!EditorUtility.DisplayDialog(
                    "Wikimedia",
                    $"Повторить импорт для {ids.Count} id из отчёта?",
                    "Resume",
                    "Cancel"))
                return;

            ImportAllPortraits(forceReplace: false, onlyIds: ids);
        }

        [MenuItem("PeopleOfMath/Link Portraits From Folders")]
        public static void LinkPortraitsFromFoldersMenu()
        {
            var n = LinkAllFromFolders();
            AssetDatabase.SaveAssets();
            Debug.Log($"Linked real portraits for {n} mathematicians.");
        }

        public static int LinkAllFromFolders(int minLinked = MinImages)
        {
            var count = 0;
            var guids = AssetDatabase.FindAssets("t:MathematicianData", new[] { DataFolder });
            foreach (var guid in guids)
            {
                var data = AssetDatabase.LoadAssetAtPath<MathematicianData>(
                    AssetDatabase.GUIDToAssetPath(guid));
                if (data != null && LinkFromFolder(data) >= minLinked)
                    count++;
            }
            return count;
        }

        public static void ImportAllPortraits(
            bool forceReplace = false,
            bool finalize = false,
            bool onlyEmpty = false,
            IReadOnlyList<string> onlyIds = null)
        {
            WikimediaHttpClient.ResetSession();
            CommonsCache.Clear();

            var guids = AssetDatabase.FindAssets("t:MathematicianData", new[] { DataFolder });
            var report = new StringBuilder();
            report.AppendLine(
                $"Portrait import {DateTime.Now:yyyy-MM-dd HH:mm} forceReplace={forceReplace} onlyEmpty={onlyEmpty}");
            var totalBytes = 0L;
            var ok = 0;
            var partial = 0;
            var skip = 0;

            for (var i = 0; i < guids.Length; i++)
            {
                var path = AssetDatabase.GUIDToAssetPath(guids[i]);
                var data = AssetDatabase.LoadAssetAtPath<MathematicianData>(path);
                if (data == null)
                    continue;

                if (onlyIds != null && onlyIds.Count > 0 &&
                    !onlyIds.Contains(data.id, StringComparer.OrdinalIgnoreCase))
                    continue;

                if (onlyEmpty && PortraitPlaceholderDetection.HasEnoughRealPortraits(data.id, MinImages))
                {
                    skip++;
                    report.AppendLine($"SKIP has_images {data.id}");
                    continue;
                }

                EditorUtility.DisplayProgressBar("Wikimedia", data.id, (float)i / guids.Length);
                try
                {
                    var count = ImportForMathematician(data, report, ref totalBytes, forceReplace);
                    if (count >= MinImages)
                        ok++;
                    else if (count > 0)
                        partial++;
                    else
                        skip++;
                }
                catch (Exception ex)
                {
                    skip++;
                    var msg = ex.Message;
                    if (msg.Contains("429", StringComparison.OrdinalIgnoreCase) ||
                        msg.Contains("Rate limit", StringComparison.OrdinalIgnoreCase))
                        report.AppendLine($"FAIL after retries {data.id}: RATE_LIMIT {msg}");
                    else
                        report.AppendLine($"FAIL {data.id}: {msg}");
                }

                System.Threading.Thread.Sleep(RequestDelayMs);
            }

            EditorUtility.ClearProgressBar();
            report.AppendLine($"OK (>={MinImages}): {ok}, partial: {partial}, skip/fail: {skip}");
            report.AppendLine($"Total new image bytes (approx): {totalBytes / 1024} KB");
            File.WriteAllText(ReportPath, report.ToString(), Encoding.UTF8);

            if (finalize)
                FinalizePortraitImport();

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log(report.ToString());
        }

        public static void FinalizePortraitImport()
        {
            LinkAllFromFolders();
            PortraitTextureImportFix.FixAll();
        }

        static int LinkFromFolder(MathematicianData data)
        {
            var dir = $"{PortraitPlaceholderDetection.ResourcesPortraitsRoot}/{data.id}";
            if (!Directory.Exists(dir))
                return 0;

            var files = Directory.GetFiles(dir)
                .Where(f => !f.EndsWith(".meta", StringComparison.OrdinalIgnoreCase))
                .Where(f => !PortraitPlaceholderDetection.IsPlaceholderFile(f))
                .Where(f =>
                {
                    var ext = Path.GetExtension(f).ToLowerInvariant();
                    return ext is ".jpg" or ".jpeg" or ".png" or ".webp";
                })
                .OrderBy(f => f, StringComparer.OrdinalIgnoreCase)
                .Take(MaxImages)
                .ToList();

            if (files.Count == 0)
                return 0;

            data.portraits ??= new List<PortraitEntry>();
            data.portraits.Clear();

            foreach (var file in files)
            {
                var assetPath = file.Replace('\\', '/');
                if (!assetPath.StartsWith("Assets/", StringComparison.Ordinal))
                    assetPath = ToAssetsPath(file);

                ConfigureTextureImporter(assetPath);
                var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(assetPath);
                if (sprite == null)
                    continue;

                data.portraits.Add(new PortraitEntry
                {
                    sprite = sprite,
                    sourceUrl = "",
                    licenseShort = "",
                    attributionRu = "",
                    attributionEn = ""
                });
            }

            EditorUtility.SetDirty(data);
            return data.portraits.Count;
        }

        static int ImportForMathematician(
            MathematicianData data,
            StringBuilder report,
            ref long totalBytes,
            bool forceReplace)
        {
            if (forceReplace)
                PortraitPlaceholderDetection.ClearAllPortraitSlots(data.id);
            else
                PortraitPlaceholderDetection.ClearPlaceholderFiles(data.id);

            var dir = $"{PortraitPlaceholderDetection.ResourcesPortraitsRoot}/{data.id}";
            if (!Directory.Exists(dir))
                Directory.CreateDirectory(dir);

            var candidates = FindCommonsImages(data);
            var imported = 0;
            data.portraits ??= new List<PortraitEntry>();
            data.portraits.Clear();

            foreach (var file in candidates)
            {
                if (imported >= MaxImages)
                    break;
                if (!IsLicenseAllowed(file.license))
                    continue;
                if (file.url.EndsWith(".svg", StringComparison.OrdinalIgnoreCase))
                    continue;

                var index = imported + 1;
                var dest = $"{dir}/{index:D2}.jpg";

                if (PortraitPlaceholderDetection.IsRealPortraitAssetPath(dest) && !forceReplace)
                {
                    BindExistingSprite(data, dest, file, ref imported, ref totalBytes, report);
                    continue;
                }

                if (!DownloadAndResize(file.url, dest))
                    continue;

                ConfigureTextureImporter(dest);
                BindExistingSprite(data, dest, file, ref imported, ref totalBytes, report);
            }

            if (imported < MinImages)
                report.AppendLine($"WARN {data.id}: only {imported} image(s), need {MinImages}");

            EditorUtility.SetDirty(data);
            return imported;
        }

        static void BindExistingSprite(
            MathematicianData data,
            string dest,
            CommonsFile file,
            ref int imported,
            ref long totalBytes,
            StringBuilder report)
        {
            var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(dest);
            if (sprite == null)
            {
                AssetDatabase.ImportAsset(dest, ImportAssetOptions.ForceUpdate);
                sprite = AssetDatabase.LoadAssetAtPath<Sprite>(dest);
            }
            if (sprite == null)
                return;

            data.portraits.Add(new PortraitEntry
            {
                sprite = sprite,
                sourceUrl = file.pageUrl,
                licenseShort = file.license,
                attributionRu = file.attribution,
                attributionEn = file.attribution
            });

            imported++;
            if (File.Exists(dest))
                totalBytes += new FileInfo(dest).Length;
            report.AppendLine($"OK {data.id} #{imported}: {file.title} ({file.license})");
        }

        static List<string> ParseFailedIdsFromReport()
        {
            if (!File.Exists(ReportPath))
                return new List<string>();

            var ids = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var line in File.ReadAllLines(ReportPath))
            {
                var m = Regex.Match(
                    line,
                    @"^(?:FAIL(?:\s+after\s+retries)?|WARN)\s+([a-z0-9_]+)\s*:",
                    RegexOptions.IgnoreCase);
                if (m.Success)
                    ids.Add(m.Groups[1].Value);
            }

            return ids.ToList();
        }

        static List<CommonsFile> FindCommonsImages(MathematicianData data)
        {
            var context = PortraitRelevance.BuildContext(data, fetchWikidataLabel: false);
            var p18 = new List<CommonsFile>();

            if (!string.IsNullOrWhiteSpace(data.wikidataId))
                p18 = FetchWikidataP18Portraits(data.wikidataId, context);

            var licensedP18 = p18
                .Where(f => IsLicenseAllowed(f.license))
                .Where(f => PortraitRelevance.Score(f, context) >= 0)
                .OrderByDescending(f => PortraitRelevance.Score(f, context))
                .ToList();

            if (licensedP18.Count >= MinImages)
                return licensedP18.Take(MaxImages * 2).ToList();

            if (licensedP18.Count < MinImages && !string.IsNullOrWhiteSpace(data.wikidataId))
                PortraitRelevance.EnrichWithWikidataLabel(context, data.wikidataId);

            var results = new List<CommonsFile>(licensedP18);
            var wikiImage = FetchWikipediaPageImage(data.wikiTitleRu);
            if (wikiImage != null)
                results.Add(wikiImage);

            if (results.Count(f => IsLicenseAllowed(f.license) && PortraitRelevance.Score(f, context) > 0) < MinImages)
            {
                var query = data.wikiTitleRu ?? data.fullNameRu;
                if (!string.IsNullOrWhiteSpace(query))
                {
                    var family = query.Contains(',')
                        ? query.Split(',')[0].Trim()
                        : query.Trim();
                    results.AddRange(SearchCommonsFiles($"{family} portrait", context));
                }
            }

            return results
                .GroupBy(f => f.title, StringComparer.OrdinalIgnoreCase)
                .Select(g => g.First())
                .Where(f => IsLicenseAllowed(f.license))
                .Where(f => PortraitRelevance.Score(f, context) > 0)
                .OrderByDescending(f => PortraitRelevance.Score(f, context))
                .Take(16)
                .ToList();
        }

        static List<CommonsFile> FetchWikidataP18Portraits(string wikidataId, PortraitRelevance.Context context)
        {
            var wd = wikidataId.Trim();
            if (!wd.StartsWith("Q", StringComparison.OrdinalIgnoreCase))
                wd = "Q" + wd;

            var url =
                $"https://www.wikidata.org/w/api.php?action=wbgetentities&ids={wd}&props=claims&format=json";
            var json = WikimediaHttpClient.GetText(url);
            var files = new List<CommonsFile>();

            foreach (Match m in Regex.Matches(json, "\"P18\"[\\s\\S]*?\"value\"\\s*:\\s*\"([^\"]+\\.(?:jpg|jpeg|png|webp))\""))
            {
                var fileName = m.Groups[1].Value;
                if (fileName.Contains("Commons-logo", StringComparison.OrdinalIgnoreCase))
                    continue;
                var info = FetchCommonsFileInfo("File:" + fileName);
                if (info != null)
                    files.Add(info);
            }

            return files;
        }

        static CommonsFile FetchWikipediaPageImage(string wikiTitleRu)
        {
            if (string.IsNullOrWhiteSpace(wikiTitleRu))
                return null;

            var title = Uri.EscapeDataString(wikiTitleRu.Replace(' ', '_'));
            var url =
                "https://ru.wikipedia.org/w/api.php?action=query&prop=pageimages" +
                $"&piprop=thumbnail&pithumbsize=800&titles={title}&format=json";
            var json = WikimediaHttpClient.GetText(url);
            var thumb = Regex.Match(json, "\"thumbnail\":\\{\"source\":\"(https://[^\"]+)\"");
            if (!thumb.Success)
                return null;

            var imageUrl = thumb.Groups[1].Value.Replace("\\/", "/");
            return new CommonsFile
            {
                title = "File:Wikipedia_thumbnail",
                url = imageUrl,
                pageUrl = $"https://ru.wikipedia.org/wiki/{title}",
                license = "See Wikipedia",
                attribution = "Wikipedia"
            };
        }

        static List<CommonsFile> SearchCommonsFiles(string term, PortraitRelevance.Context context)
        {
            var enc = Uri.EscapeDataString(term);
            var url =
                "https://commons.wikimedia.org/w/api.php?action=query&generator=search" +
                $"&gsrnamespace=6&gsrsearch={enc}&gsrlimit=12&prop=imageinfo&iiprop=url|extmetadata&iiurlwidth=800&format=json";
            if (CommonsCache.TryGetValue("search:" + term, out var cachedSearch))
                return cachedSearch
                    .Where(f => PortraitRelevance.Score(f, context) > 0)
                    .ToList();

            var json = WikimediaHttpClient.GetText(url);
            var parsed = ParseCommonsSearch(json)
                .Where(f => PortraitRelevance.Score(f, context) > 0)
                .ToList();
            CommonsCache["search:" + term] = parsed;
            return parsed;
        }

        static CommonsFile FetchCommonsFileInfo(string fileTitle)
        {
            if (CommonsCache.TryGetValue("file:" + fileTitle, out var cached) && cached.Count > 0)
                return cached[0];

            var enc = Uri.EscapeDataString(fileTitle);
            var url =
                "https://commons.wikimedia.org/w/api.php?action=query&titles=" + enc +
                "&prop=imageinfo&iiprop=url|extmetadata&iiurlwidth=800&format=json";
            var json = WikimediaHttpClient.GetText(url);
            var file = ParseCommonsSearch(json).FirstOrDefault();
            CommonsCache["file:" + fileTitle] = file != null
                ? new List<CommonsFile> { file }
                : new List<CommonsFile>();
            return file;
        }

        static List<CommonsFile> ParseCommonsSearch(string json)
        {
            var list = new List<CommonsFile>();
            if (string.IsNullOrEmpty(json))
                return list;

            foreach (Match page in Regex.Matches(json, "\"title\":\"(File:[^\"]+)\""))
            {
                var title = page.Groups[1].Value;
                var blockIdx = json.IndexOf(title, StringComparison.Ordinal);
                if (blockIdx < 0)
                    continue;

                var block = json.Substring(blockIdx, Math.Min(5000, json.Length - blockIdx));
                var urlM = Regex.Match(block, "\"url\":\"(https://upload[^\"\\\\]+)\"");
                if (!urlM.Success)
                    continue;

                var license = ExtractMeta(block, "LicenseShortName");
                if (string.IsNullOrEmpty(license))
                    license = ExtractMeta(block, "UsageTerms");
                if (string.IsNullOrEmpty(license))
                    license = ExtractMeta(block, "License");

                list.Add(new CommonsFile
                {
                    title = title,
                    url = urlM.Groups[1].Value.Replace("\\/", "/"),
                    pageUrl = "https://commons.wikimedia.org/wiki/" + Uri.EscapeDataString(title),
                    license = license ?? "",
                    attribution = $"{ExtractMeta(block, "Artist")} {ExtractMeta(block, "Credit")}".Trim()
                });
            }

            return list;
        }

        static string ExtractMeta(string block, string key)
        {
            var m = Regex.Match(
                block,
                $"\"{key}\"\\s*:\\s*\\{{\\s*\"value\"\\s*:\\s*\"((?:\\\\.|[^\"\\\\])*)\"",
                RegexOptions.Singleline);
            return m.Success ? Regex.Unescape(m.Groups[1].Value.Replace("\\n", "\n")) : "";
        }

        static bool IsLicenseAllowed(string license)
        {
            if (string.IsNullOrWhiteSpace(license))
                return false;
            var l = license.ToLowerInvariant();
            if (l.Contains("see wikipedia"))
                return true;
            if (l.Contains("nc") || l.Contains("nd") || l.Contains("non-commercial") ||
                l.Contains("no derivatives"))
                return false;
            return AllowedLicenseFragments.Any(f => l.Contains(f));
        }

        static bool DownloadAndResize(string url, string destPath)
        {
            byte[] raw;
            try
            {
                raw = WikimediaHttpClient.DownloadBytes(url);
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"Download failed: {url} — {ex.Message}");
                return false;
            }

            var tex = new Texture2D(2, 2);
            if (!tex.LoadImage(raw))
            {
                UnityEngine.Object.DestroyImmediate(tex);
                return false;
            }

            var w = tex.width;
            var h = tex.height;
            var scale = Mathf.Max(w, h) > MaxSide ? MaxSide / (float)Mathf.Max(w, h) : 1f;
            Texture2D output = tex;
            if (scale < 1f)
            {
                var nw = Mathf.Max(1, Mathf.RoundToInt(w * scale));
                var nh = Mathf.Max(1, Mathf.RoundToInt(h * scale));
                var rt = RenderTexture.GetTemporary(nw, nh);
                Graphics.Blit(tex, rt);
                var prev = RenderTexture.active;
                RenderTexture.active = rt;
                output = new Texture2D(nw, nh, TextureFormat.RGB24, false);
                output.ReadPixels(new Rect(0, 0, nw, nh), 0, 0);
                output.Apply();
                RenderTexture.active = prev;
                RenderTexture.ReleaseTemporary(rt);
                UnityEngine.Object.DestroyImmediate(tex);
            }

            var jpg = output.EncodeToJPG(JpegQuality);
            File.WriteAllBytes(destPath, jpg);
            UnityEngine.Object.DestroyImmediate(output);
            return true;
        }

        static void ConfigureTextureImporter(string assetPath)
        {
            AssetDatabase.ImportAsset(assetPath, ImportAssetOptions.ForceUpdate);
            var importer = AssetImporter.GetAtPath(assetPath) as TextureImporter;
            if (importer == null)
                return;
            importer.maxTextureSize = MaxSide;
            PortraitTextureImportFix.ApplyPortraitImportSettings(importer, assetPath);
            importer.SaveAndReimport();
        }

        static string ToAssetsPath(string fullPath)
        {
            var project = Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
            return fullPath.Replace('\\', '/')
                .Replace(project.Replace('\\', '/'), "")
                .TrimStart('/');
        }

        class CommonsFile
        {
            public string title;
            public string url;
            public string pageUrl;
            public string license;
            public string attribution;
        }

        static class PortraitRelevance
        {
            public class Context
            {
                public string id;
                public List<string> tokens = new();
            }

            public static Context BuildContext(MathematicianData data, bool fetchWikidataLabel = true)
            {
                var ctx = new Context { id = data.id ?? "" };
                AddTokens(ctx, data.id);
                AddTokens(ctx, data.wikiTitleRu);
                AddTokens(ctx, data.fullNameRu);

                if (fetchWikidataLabel && !string.IsNullOrWhiteSpace(data.wikidataId))
                    EnrichWithWikidataLabel(ctx, data.wikidataId);

                return ctx;
            }

            public static void EnrichWithWikidataLabel(Context ctx, string wikidataId)
            {
                AddTokens(ctx, FetchWikidataLabelEn(wikidataId));
            }

            static string FetchWikidataLabelEn(string wikidataId)
            {
                var wd = wikidataId.Trim();
                if (!wd.StartsWith("Q", StringComparison.OrdinalIgnoreCase))
                    wd = "Q" + wd;
                var url =
                    $"https://www.wikidata.org/w/api.php?action=wbgetentities&ids={wd}&props=labels&languages=en&format=json";
                try
                {
                    var json = WikimediaHttpClient.GetText(url);
                    var m = Regex.Match(json, "\"en\"\\s*:\\s*\\{\\s*\"value\"\\s*:\\s*\"([^\"]+)\"");
                    return m.Success ? m.Groups[1].Value : "";
                }
                catch
                {
                    return "";
                }
            }

            static void AddTokens(Context ctx, string text)
            {
                if (string.IsNullOrWhiteSpace(text))
                    return;

                foreach (var part in text.ToLowerInvariant().Split(',', ' ', '-', '_'))
                {
                    var t = part.Trim();
                    if (t.Length < 3)
                        continue;
                    if (!ctx.tokens.Contains(t))
                        ctx.tokens.Add(t);
                }
            }

            public static int Score(CommonsFile file, Context ctx)
            {
                if (file == null || string.IsNullOrEmpty(file.title))
                    return -100;

                var t = file.title.ToLowerInvariant();
                foreach (var skip in SkipTitleFragments)
                {
                    if (t.Contains(skip))
                        return -100;
                }

                foreach (var bad in BadNameFragments)
                {
                    if (t.Contains(bad))
                        return -100;
                }

                if (!t.StartsWith("file:"))
                    return -100;

                var hasImageExt = t.EndsWith(".jpg") || t.EndsWith(".jpeg") || t.EndsWith(".png") ||
                                  t.EndsWith(".webp");
                var hasPortraitWord = t.Contains("portrait") || t.Contains("photo");
                if (!hasImageExt && !hasPortraitWord)
                    return -100;

                var score = 0;
                foreach (var token in ctx.tokens)
                {
                    if (t.Contains(token))
                        score += 12;
                }

                if (!string.IsNullOrEmpty(ctx.id) && t.Contains(ctx.id.Replace('_', ' ')))
                    score += 8;
                if (!string.IsNullOrEmpty(ctx.id) && t.Contains(ctx.id.Replace('_', '-')))
                    score += 8;

                if (hasPortraitWord)
                    score += 4;

                if (file.title == "File:Wikipedia_thumbnail")
                    score += 15;

                return score >= 8 ? score : 0;
            }
        }
    }
}
