using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using PeopleOfMath.Data;
using UnityEditor;
using UnityEngine;
using UnityEngine.Networking;

namespace PeopleOfMath.Editor
{
    public static class WikimediaPortraitImporter
    {
        const string DataFolder = "Assets/Data/Mathematicians";
        const string ImagesRoot = "Assets/Data/Images";
        const string ResourcesPortraitsRoot = "Assets/Resources/Portraits";
        const string ReportPath = "Assets/Data/import_report.txt";
        const int MaxImages = 4;
        const int MinImages = 2;
        const int MaxSide = 512;
        const int JpegQuality = 85;
        const int RequestDelayMs = 400;

        static readonly string[] AllowedLicenseFragments =
        {
            "public domain", "cc-by-sa", "cc by-sa", "cc-by", "cc by",
            "pd-", "pd ", "gfdl", "creative commons", "cc0"
        };

        static readonly string[] SkipTitleFragments =
        {
            "signature", "diagram", "graph", "plot", "theorem", "formula", "commemorative",
            "stamp", "coin", "map", "facsimile", "manuscript"
        };

        [MenuItem("PeopleOfMath/Import Portraits (Wikimedia)")]
        public static void ImportPortraitsMenu()
        {
            if (!EditorUtility.DisplayDialog(
                    "Wikimedia",
                    "Скачать портреты с Commons для всех SO? Требуется сеть.",
                    "Import",
                    "Cancel"))
                return;

            ImportAllPortraits();
        }

        [MenuItem("PeopleOfMath/Link Portraits From Folders")]
        public static void LinkPortraitsFromFoldersMenu()
        {
            var n = LinkAllFromFolders();
            AssetDatabase.SaveAssets();
            Debug.Log($"Linked portraits on disk for {n} mathematicians.");
        }

        public static void ImportAllPortraits()
        {
            var guids = AssetDatabase.FindAssets("t:MathematicianData", new[] { DataFolder });
            var report = new StringBuilder();
            report.AppendLine($"Portrait import {DateTime.Now:yyyy-MM-dd HH:mm}");
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

                EditorUtility.DisplayProgressBar("Wikimedia", data.id, (float)i / guids.Length);
                try
                {
                    var count = ImportForMathematician(data, report, ref totalBytes);
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
                    report.AppendLine($"FAIL {data.id}: {ex.Message}");
                }

                System.Threading.Thread.Sleep(RequestDelayMs);
            }

            EditorUtility.ClearProgressBar();
            report.AppendLine($"OK (>={MinImages}): {ok}, partial: {partial}, skip/fail: {skip}");
            report.AppendLine($"Total image bytes on disk (approx): {totalBytes / 1024} KB");
            File.WriteAllText(ReportPath, report.ToString(), Encoding.UTF8);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log(report.ToString());
        }

        public static int LinkAllFromFolders()
        {
            var count = 0;
            var guids = AssetDatabase.FindAssets("t:MathematicianData", new[] { DataFolder });
            foreach (var guid in guids)
            {
                var data = AssetDatabase.LoadAssetAtPath<MathematicianData>(
                    AssetDatabase.GUIDToAssetPath(guid));
                if (data != null && LinkFromFolder(data) >= MinImages)
                    count++;
            }
            return count;
        }

        static void CopyToResourcesPortrait(string id, string sourceAssetPath, int index)
        {
            var resDir = $"{ResourcesPortraitsRoot}/{id}";
            if (!Directory.Exists(resDir))
                Directory.CreateDirectory(resDir);

            var dest = $"{resDir}/{index:D2}.jpg";
            if (File.Exists(dest))
                return;

            var srcFull = Path.GetFullPath(sourceAssetPath);
            var destFull = Path.GetFullPath(dest);
            if (srcFull != destFull && File.Exists(srcFull))
                File.Copy(srcFull, destFull, overwrite: true);
            ConfigureTextureImporter(dest);
        }

        static int LinkFromFolder(MathematicianData data)
        {
            SyncImagesToResources(data.id);
            var dir = $"{ResourcesPortraitsRoot}/{data.id}";
            if (!Directory.Exists(dir))
                dir = $"{ImagesRoot}/{data.id}";
            if (!Directory.Exists(dir))
                return 0;

            var files = Directory.GetFiles(dir)
                .Where(f => !f.EndsWith(".meta", StringComparison.OrdinalIgnoreCase))
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

        static void SyncImagesToResources(string id)
        {
            var srcDir = $"{ImagesRoot}/{id}";
            if (!Directory.Exists(srcDir))
                return;
            foreach (var file in Directory.GetFiles(srcDir))
            {
                var name = Path.GetFileName(file);
                if (name.EndsWith(".meta", StringComparison.OrdinalIgnoreCase))
                    continue;
                var ext = Path.GetExtension(file).ToLowerInvariant();
                if (ext is not (".jpg" or ".jpeg" or ".png" or ".webp"))
                    continue;
                var assetPath = $"{srcDir}/{name}".Replace('\\', '/');
                if (int.TryParse(Path.GetFileNameWithoutExtension(name), out var idx))
                    CopyToResourcesPortrait(id, assetPath, idx);
            }
        }

        static string ToAssetsPath(string fullPath)
        {
            var project = Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
            return fullPath.Replace('\\', '/')
                .Replace(project.Replace('\\', '/'), "")
                .TrimStart('/');
        }

        static int ImportForMathematician(MathematicianData data, StringBuilder report, ref long totalBytes)
        {
            var dir = $"{ImagesRoot}/{data.id}";
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
                if (!File.Exists(dest))
                {
                    if (!DownloadAndResize(file.url, dest))
                        continue;
                    ConfigureTextureImporter(dest);
                    CopyToResourcesPortrait(data.id, dest, index);
                }
                else
                    CopyToResourcesPortrait(data.id, dest, index);

                var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(dest);
                if (sprite == null)
                {
                    AssetDatabase.ImportAsset(dest, ImportAssetOptions.ForceUpdate);
                    sprite = AssetDatabase.LoadAssetAtPath<Sprite>(dest);
                }
                if (sprite == null)
                    continue;

                data.portraits.Add(new PortraitEntry
                {
                    sprite = sprite,
                    sourceUrl = file.pageUrl,
                    licenseShort = file.license,
                    attributionRu = file.attribution,
                    attributionEn = file.attribution
                });

                imported++;
                totalBytes += new FileInfo(dest).Length;
                report.AppendLine($"OK {data.id} #{index}: {file.title} ({file.license})");
            }

            if (imported < MinImages)
                report.AppendLine($"WARN {data.id}: only {imported} image(s), need {MinImages}");

            EditorUtility.SetDirty(data);
            return imported;
        }

        static List<CommonsFile> FindCommonsImages(MathematicianData data)
        {
            var results = new List<CommonsFile>();

            if (!string.IsNullOrWhiteSpace(data.wikidataId))
                results.AddRange(FetchWikidataPortraits(data.wikidataId));

            var query = data.wikiTitleRu ?? data.fullNameRu;
            if (!string.IsNullOrWhiteSpace(query))
            {
                var family = query.Contains(',')
                    ? query.Split(',')[0].Trim()
                    : query.Trim();
                results.AddRange(SearchCommonsFiles($"{family} portrait"));
                results.AddRange(SearchCommonsFiles(family));
            }

            return results
                .GroupBy(f => f.title, StringComparer.OrdinalIgnoreCase)
                .Select(g => g.First())
                .Where(f => LooksLikePortrait(f.title))
                .Take(16)
                .ToList();
        }

        static List<CommonsFile> FetchWikidataPortraits(string wikidataId)
        {
            var wd = wikidataId.Trim();
            if (!wd.StartsWith("Q", StringComparison.OrdinalIgnoreCase))
                wd = "Q" + wd;

            var url =
                $"https://www.wikidata.org/w/api.php?action=wbgetentities&ids={wd}&props=claims&format=json";
            var json = Get(url);
            var files = new List<CommonsFile>();
            foreach (Match m in Regex.Matches(json, "\"value\":\"([^\"]+\\.(?:jpg|jpeg|png|webp))\""))
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

        static CommonsFile FetchCommonsFileInfo(string fileTitle)
        {
            var enc = Uri.EscapeDataString(fileTitle);
            var url =
                "https://commons.wikimedia.org/w/api.php?action=query&titles=" + enc +
                "&prop=imageinfo&iiprop=url|extmetadata&iiurlwidth=800&format=json";
            var json = Get(url);
            var list = ParseCommonsSearch(json);
            return list.FirstOrDefault();
        }

        static List<CommonsFile> SearchCommonsFiles(string term)
        {
            var enc = Uri.EscapeDataString(term);
            var url =
                "https://commons.wikimedia.org/w/api.php?action=query&generator=search" +
                $"&gsrnamespace=6&gsrsearch={enc}&gsrlimit=10&prop=imageinfo&iiprop=url|extmetadata&iiurlwidth=800&format=json";
            var json = Get(url);
            return ParseCommonsSearch(json);
        }

        static bool LooksLikePortrait(string title)
        {
            var t = title.ToLowerInvariant();
            if (!t.StartsWith("file:"))
                return false;
            foreach (var skip in SkipTitleFragments)
            {
                if (t.Contains(skip))
                    return false;
            }
            return t.EndsWith(".jpg") || t.EndsWith(".jpeg") || t.EndsWith(".png") ||
                   t.EndsWith(".webp") || t.Contains("portrait") || t.Contains("photo");
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
            return m.Success ? Unescape(m.Groups[1].Value) : "";
        }

        static string Unescape(string s) =>
            Regex.Unescape(s.Replace("\\n", "\n"));

        static bool IsLicenseAllowed(string license)
        {
            if (string.IsNullOrWhiteSpace(license))
                return false;
            var l = license.ToLowerInvariant();
            if (l.Contains("nc") || l.Contains("nd") || l.Contains("non-commercial") ||
                l.Contains("no derivatives"))
                return false;
            return AllowedLicenseFragments.Any(f => l.Contains(f));
        }

        static bool DownloadAndResize(string url, string destPath)
        {
            using var req = UnityWebRequest.Get(url);
            req.SetRequestHeader("User-Agent", "PeopleOfMath/1.0 (Unity Editor; educational app)");
            var op = req.SendWebRequest();
            while (!op.isDone)
                System.Threading.Thread.Sleep(20);

            if (req.result != UnityWebRequest.Result.Success)
                return false;

            var tex = new Texture2D(2, 2);
            if (!tex.LoadImage(req.downloadHandler.data))
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
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.maxTextureSize = MaxSide;
            importer.SaveAndReimport();
        }

        static string Get(string url)
        {
            using var req = UnityWebRequest.Get(url);
            req.SetRequestHeader("User-Agent", "PeopleOfMath/1.0 (Unity Editor; educational app)");
            var op = req.SendWebRequest();
            while (!op.isDone)
                System.Threading.Thread.Sleep(20);
            return req.result == UnityWebRequest.Result.Success ? req.downloadHandler.text : "";
        }

        class CommonsFile
        {
            public string title;
            public string url;
            public string pageUrl;
            public string license;
            public string attribution;
        }
    }
}
