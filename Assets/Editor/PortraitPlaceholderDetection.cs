using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace PeopleOfMath.Editor
{
    public static class PortraitPlaceholderDetection
    {
        public const string ResourcesPortraitsRoot = "Assets/Resources/Portraits";
        public const string DevPlaceholdersRoot = "Assets/Data/Placeholders";
        public const string PlaceholderMarkerExtension = ".placeholder";
        public const int PlaceholderMaxBytes = 25000;

        public static bool IsPlaceholderFile(string filePath)
        {
            if (string.IsNullOrEmpty(filePath) || !File.Exists(filePath))
                return false;

            if (File.Exists(filePath + PlaceholderMarkerExtension))
                return true;

            try
            {
                return new FileInfo(filePath).Length < PlaceholderMaxBytes;
            }
            catch
            {
                return false;
            }
        }

        public static bool IsPlaceholderAssetPath(string assetPath) =>
            IsPlaceholderFile(Path.GetFullPath(assetPath));

        public static bool IsRealPortraitAssetPath(string assetPath) =>
            !string.IsNullOrEmpty(assetPath) &&
            File.Exists(Path.GetFullPath(assetPath)) &&
            !IsPlaceholderAssetPath(assetPath);

        public static int CountRealPortraits(string mathematicianId, string root = ResourcesPortraitsRoot)
        {
            var dir = $"{root}/{mathematicianId}";
            if (!Directory.Exists(dir))
                return 0;

            return Directory.GetFiles(dir)
                .Count(f => !f.EndsWith(".meta", StringComparison.OrdinalIgnoreCase) &&
                            !IsPlaceholderFile(f) &&
                            (Path.GetExtension(f).ToLowerInvariant() is ".jpg" or ".jpeg" or ".png" or ".webp"));
        }

        public static bool HasEnoughRealPortraits(string mathematicianId, int minCount = 2) =>
            CountRealPortraits(mathematicianId) >= minCount;

        public static void MarkAsPlaceholder(string imagePath)
        {
            File.WriteAllText(imagePath + PlaceholderMarkerExtension, "dev");
        }

        public static int ClearPlaceholderFiles(string mathematicianId, string root = ResourcesPortraitsRoot)
        {
            var dir = $"{root}/{mathematicianId}";
            if (!Directory.Exists(dir))
                return 0;

            var removed = 0;
            foreach (var file in Directory.GetFiles(dir))
            {
                if (file.EndsWith(".meta", StringComparison.OrdinalIgnoreCase) ||
                    file.EndsWith(PlaceholderMarkerExtension, StringComparison.OrdinalIgnoreCase))
                    continue;

                var ext = Path.GetExtension(file).ToLowerInvariant();
                if (ext is not (".jpg" or ".jpeg" or ".png" or ".webp"))
                    continue;

                if (!IsPlaceholderFile(file))
                    continue;

                var assetPath = $"{dir}/{Path.GetFileName(file)}".Replace('\\', '/');
                if (File.Exists(file))
                    File.Delete(file);
                if (File.Exists(file + PlaceholderMarkerExtension))
                    File.Delete(file + PlaceholderMarkerExtension);
                if (File.Exists(assetPath + ".meta"))
                    AssetDatabase.DeleteAsset(assetPath + ".meta");

                removed++;
            }

            return removed;
        }

        public static int ClearAllPortraitSlots(string mathematicianId, string root = ResourcesPortraitsRoot)
        {
            var dir = $"{root}/{mathematicianId}";
            if (!Directory.Exists(dir))
                return 0;

            var removed = 0;
            foreach (var file in Directory.GetFiles(dir).ToList())
            {
                var name = Path.GetFileName(file);
                if (name.EndsWith(".meta", StringComparison.OrdinalIgnoreCase))
                    continue;
                if (name.EndsWith(PlaceholderMarkerExtension, StringComparison.OrdinalIgnoreCase))
                    continue;

                var ext = Path.GetExtension(file).ToLowerInvariant();
                if (ext is not (".jpg" or ".jpeg" or ".png" or ".webp"))
                    continue;

                var assetPath = $"{dir}/{name}".Replace('\\', '/');
                File.Delete(file);
                if (File.Exists(assetPath + ".meta"))
                    AssetDatabase.DeleteAsset(assetPath + ".meta");
                if (File.Exists(file + PlaceholderMarkerExtension))
                    File.Delete(file + PlaceholderMarkerExtension);
                removed++;
            }

            return removed;
        }

        [MenuItem("PeopleOfMath/Clear Placeholder Portraits In Resources")]
        public static void ClearAllPlaceholdersInResourcesMenu()
        {
            if (!Directory.Exists(ResourcesPortraitsRoot))
                return;

            var total = 0;
            foreach (var dir in Directory.GetDirectories(ResourcesPortraitsRoot))
            {
                var id = Path.GetFileName(dir);
                total += ClearPlaceholderFiles(id);
            }

            AssetDatabase.Refresh();
            Debug.Log($"Removed {total} placeholder portrait file(s) from Resources/Portraits.");
        }
    }
}
