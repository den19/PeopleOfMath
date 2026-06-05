using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using PeopleOfMath.Data;
using UnityEditor;
using UnityEngine;

namespace PeopleOfMath.Editor
{
    public static class ManualPortraitSync
    {
        const string ReportPath = "Assets/Data/manual_portrait_sync_report.txt";

        [MenuItem("PeopleOfMath/Sync Manual Portraits (From Resources)", false, 101)]
        public static void SyncManualPortraitsMenu()
        {
            var result = SyncManualPortraits();
            EditorUtility.DisplayDialog(
                "Manual portraits",
                $"Переимпортировано файлов: {result.reimported}\n" +
                $"Привязано карточек: {result.linked}\n" +
                $"Папок без SO: {result.orphanFolders.Count}\n\n" +
                $"Отчёт: {ReportPath}",
                "OK");
        }

        public static (int reimported, int linked, List<string> orphanFolders) SyncManualPortraits()
        {
            var reimported = PortraitTextureImportFix.ReimportAll();
            AssetDatabase.Refresh();

            var linked = WikimediaPortraitImporter.LinkAllFromFolders(minLinked: 1);
            var orphanFolders = FindOrphanPortraitFolders();

            AssetDatabase.SaveAssets();

            var report = new StringBuilder();
            report.AppendLine($"Manual portrait sync {System.DateTime.Now:yyyy-MM-dd HH:mm}");
            report.AppendLine($"Reimported textures: {reimported}");
            report.AppendLine($"Linked mathematicians: {linked}");
            if (orphanFolders.Count == 0)
                report.AppendLine("Orphan folders: none");
            else
            {
                report.AppendLine("Orphan folders (no MathematicianData with matching id):");
                foreach (var folder in orphanFolders)
                    report.AppendLine($"  - {folder}");
            }

            File.WriteAllText(ReportPath, report.ToString(), Encoding.UTF8);
            Debug.Log(report.ToString());
            return (reimported, linked, orphanFolders);
        }

        static List<string> FindOrphanPortraitFolders()
        {
            var root = PortraitPlaceholderDetection.ResourcesPortraitsRoot;
            if (!Directory.Exists(root))
                return new List<string>();

            var knownIds = new HashSet<string>(
                AssetDatabase.FindAssets("t:MathematicianData", new[] { "Assets/Data/Mathematicians" })
                    .Select(AssetDatabase.GUIDToAssetPath)
                    .Select(path => AssetDatabase.LoadAssetAtPath<MathematicianData>(path))
                    .Where(data => data != null && !string.IsNullOrEmpty(data.id))
                    .Select(data => data.id));

            var orphans = new List<string>();
            foreach (var dir in Directory.GetDirectories(root))
            {
                var id = Path.GetFileName(dir);
                if (knownIds.Contains(id))
                    continue;

                if (PortraitPlaceholderDetection.CountRealPortraits(id) > 0)
                    orphans.Add(id);
            }

            return orphans.OrderBy(x => x, System.StringComparer.OrdinalIgnoreCase).ToList();
        }
    }
}
