using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace PeopleOfMath.Editor
{
    public static class MathematicianAssetYamlSync
    {
        const string DataFolder = "Assets/Data/Mathematicians";
        const string ReportPath = "Assets/Data/mathematician_utf8_sync_report.txt";

        [MenuItem("PeopleOfMath/Sync Mathematician Assets (UTF-8 YAML)", false, 102)]
        public static void SyncMenu()
        {
            var result = SyncAllMathematicianAssets(showDialog: true);
            Debug.Log(
                $"Mathematician UTF-8 YAML sync: {result.changed} changed, {result.unchanged} unchanged.");
        }

        public static (int changed, int unchanged) SyncAllMathematicianAssets(bool showDialog = false)
        {
            AssetDatabase.SaveAssets();

            var folder = Path.Combine(Application.dataPath, "Data", "Mathematicians");
            if (!Directory.Exists(folder))
            {
                Debug.LogWarning($"Folder not found: {DataFolder}");
                return (0, 0);
            }

            var changed = 0;
            var unchanged = 0;
            var changedIds = new List<string>();

            foreach (var file in Directory.GetFiles(folder, "*.asset"))
            {
                var assetPath = ToAssetPath(file);
                if (PrettifyAssetFile(assetPath))
                {
                    changed++;
                    changedIds.Add(Path.GetFileNameWithoutExtension(file));
                }
                else
                {
                    unchanged++;
                }
            }

            if (changed > 0)
                AssetDatabase.Refresh();

            WriteReport(changed, unchanged, changedIds);

            if (showDialog)
            {
                EditorUtility.DisplayDialog(
                    "UTF-8 YAML",
                    $"Обновлено файлов: {changed}\nБез изменений: {unchanged}\n\nОтчёт: {ReportPath}",
                    "OK");
            }

            return (changed, unchanged);
        }

        public static bool PrettifyAssetFile(string assetPath)
        {
            if (string.IsNullOrEmpty(assetPath) || !File.Exists(assetPath))
                return false;

            var original = File.ReadAllText(assetPath, Encoding.UTF8);
            var prettified = UnescapeYamlQuotedStrings(original);
            if (prettified == original)
                return false;

            File.WriteAllText(assetPath, prettified, new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));
            return true;
        }

        /// <summary>
        /// Replaces escape sequences inside YAML double-quoted strings in place,
        /// preserving layout and whitespace for idempotent re-runs.
        /// </summary>
        internal static string UnescapeYamlQuotedStrings(string yaml)
        {
            if (string.IsNullOrEmpty(yaml))
                return yaml;

            var sb = new StringBuilder(yaml.Length);
            var inString = false;

            for (var i = 0; i < yaml.Length; i++)
            {
                var c = yaml[i];

                if (!inString)
                {
                    sb.Append(c);
                    if (c == '"')
                        inString = true;
                    continue;
                }

                if (c == '\\' && i + 1 < yaml.Length)
                {
                    if (TryWriteDecodedEscape(yaml, ref i, sb))
                        continue;
                }

                sb.Append(c);
                if (c == '"')
                    inString = false;
            }

            return sb.ToString();
        }

        static bool TryWriteDecodedEscape(string yaml, ref int i, StringBuilder sb)
        {
            var n = yaml[i + 1];
            switch (n)
            {
                case 'u' when i + 5 < yaml.Length:
                {
                    var hex = yaml.Substring(i + 2, 4);
                    if (!IsHex(hex))
                        return false;
                    sb.Append((char)Convert.ToInt32(hex, 16));
                    i += 5;
                    return true;
                }
                case 'x' when i + 3 < yaml.Length:
                {
                    var hex = yaml.Substring(i + 2, 2);
                    if (!IsHex(hex))
                        return false;
                    sb.Append((char)Convert.ToInt32(hex, 16));
                    i += 3;
                    return true;
                }
                case 'n':
                    sb.Append('\n');
                    i += 1;
                    return true;
                case 'r':
                    sb.Append('\r');
                    i += 1;
                    return true;
                case 't':
                    sb.Append('\t');
                    i += 1;
                    return true;
                case '"':
                    sb.Append('"');
                    i += 1;
                    return true;
                case '\\':
                    sb.Append('\\');
                    i += 1;
                    return true;
                case '_':
                    sb.Append('_');
                    i += 1;
                    return true;
                default:
                    return false;
            }
        }

        static bool IsHex(string s)
        {
            foreach (var c in s)
            {
                if (!Uri.IsHexDigit(c))
                    return false;
            }
            return true;
        }

        static void WriteReport(int changed, int unchanged, List<string> changedIds)
        {
            var report = new StringBuilder();
            report.AppendLine($"Mathematician UTF-8 YAML sync {DateTime.Now:yyyy-MM-dd HH:mm}");
            report.AppendLine($"Changed: {changed}");
            report.AppendLine($"Unchanged: {unchanged}");
            if (changedIds.Count > 0)
            {
                report.AppendLine("Updated ids:");
                foreach (var id in changedIds)
                    report.AppendLine($"  - {id}");
            }

            File.WriteAllText(ReportPath, report.ToString(), new UTF8Encoding(false));
        }

        static string ToAssetPath(string fullPath)
        {
            var normalized = fullPath.Replace('\\', '/');
            var dataPath = Application.dataPath.Replace('\\', '/');
            if (normalized.StartsWith(dataPath, StringComparison.OrdinalIgnoreCase))
                return "Assets" + normalized.Substring(dataPath.Length);
            return normalized;
        }
    }
}
