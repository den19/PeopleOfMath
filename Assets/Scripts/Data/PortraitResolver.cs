using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace PeopleOfMath.Data
{
    public static class PortraitResolver
    {
        static readonly Dictionary<string, Sprite> PrimaryCache = new();
        static readonly Dictionary<string, bool> PlaceholderMarkerCache = new(StringComparer.Ordinal);
        static readonly Dictionary<int, Sprite> RuntimeSpritesByTexture = new();

        public static Sprite GetPrimaryPortrait(MathematicianData data)
        {
            if (data == null || string.IsNullOrEmpty(data.id))
                return null;

            if (PrimaryCache.TryGetValue(data.id, out var cached))
                return cached;

            var sprite = ResolvePrimaryPortrait(data);
            // Do not cache misses — a later import / Resources fix can succeed.
            if (sprite != null)
                PrimaryCache[data.id] = sprite;
            return sprite;
        }

        static Sprite ResolvePrimaryPortrait(MathematicianData data)
        {
            var valid = CollectValidPortraits(data.portraits);
            if (valid.Count > 0)
                return valid[0].sprite;

            var fromResources = LoadPortraitsFromResources(data.id);
            return fromResources.Count > 0 ? fromResources[0].sprite : null;
        }

        public static List<PortraitEntry> ResolveGalleryPortraits(
            string mathematicianId,
            IReadOnlyList<PortraitEntry> portraits)
        {
            var entries = portraits ?? new List<PortraitEntry>();
            var valid = CollectValidPortraits(entries);

            if (string.IsNullOrEmpty(mathematicianId))
                return valid;

            var fromResources = LoadPortraitsFromResources(mathematicianId);
            if (valid.Count == 0)
                return fromResources;

            if (fromResources.Count > valid.Count)
                return MergeWithResourcePortraits(entries, fromResources);

            return valid;
        }

        public static List<PortraitEntry> CollectValidPortraits(IReadOnlyList<PortraitEntry> portraits)
        {
            var valid = new List<PortraitEntry>();
            if (portraits == null)
                return valid;

            foreach (var p in portraits)
            {
                if (p?.sprite != null)
                    valid.Add(p);
            }

            return valid;
        }

        public static List<PortraitEntry> LoadPortraitsFromResources(string mathematicianId)
        {
            var path = $"Portraits/{mathematicianId}";
            var sprites = Resources.LoadAll<Sprite>(path);
            if (sprites != null && sprites.Length > 0)
            {
                return sprites
                    .Where(s => !HasPlaceholderMarker(mathematicianId, s.name))
                    .OrderBy(s => s.name, StringComparer.OrdinalIgnoreCase)
                    .Select(s => new PortraitEntry { sprite = s })
                    .ToList();
            }

            var textures = Resources.LoadAll<Texture2D>(path);
            if (textures == null || textures.Length == 0)
                return new List<PortraitEntry>();

            return textures
                .Where(t => !HasPlaceholderMarker(mathematicianId, t.name))
                .OrderBy(t => t.name, StringComparer.OrdinalIgnoreCase)
                .Select(t => new PortraitEntry { sprite = GetOrCreateRuntimeSprite(t) })
                .Where(e => e.sprite != null)
                .ToList();
        }

        static Sprite GetOrCreateRuntimeSprite(Texture2D texture)
        {
            if (texture == null)
                return null;

            var key = texture.GetInstanceID();
            if (RuntimeSpritesByTexture.TryGetValue(key, out var existing) && existing != null)
                return existing;

            var sprite = Sprite.Create(
                texture,
                new Rect(0, 0, texture.width, texture.height),
                new Vector2(0.5f, 0.5f),
                100f);
            RuntimeSpritesByTexture[key] = sprite;
            return sprite;
        }

        public static bool HasPlaceholderMarker(string mathematicianId, string assetName)
        {
            if (string.IsNullOrEmpty(mathematicianId) || string.IsNullOrEmpty(assetName))
                return false;

            var cacheKey = mathematicianId + "/" + assetName;
            if (PlaceholderMarkerCache.TryGetValue(cacheKey, out var cached))
                return cached;

            var hasMarker =
                Resources.Load<TextAsset>($"Portraits/{mathematicianId}/{assetName}.placeholder") != null;
            PlaceholderMarkerCache[cacheKey] = hasMarker;
            return hasMarker;
        }

        static List<PortraitEntry> MergeWithResourcePortraits(
            IReadOnlyList<PortraitEntry> assetEntries,
            List<PortraitEntry> fromResources)
        {
            var metadataBySpriteName = new Dictionary<string, PortraitEntry>(StringComparer.OrdinalIgnoreCase);
            if (assetEntries != null)
            {
                foreach (var entry in assetEntries)
                {
                    if (entry?.sprite != null)
                        metadataBySpriteName[entry.sprite.name] = entry;
                }
            }

            var merged = new List<PortraitEntry>(fromResources.Count);
            foreach (var resource in fromResources)
            {
                if (metadataBySpriteName.TryGetValue(resource.sprite.name, out var asset))
                {
                    merged.Add(new PortraitEntry
                    {
                        sprite = resource.sprite,
                        sourceUrl = asset.sourceUrl,
                        licenseShort = asset.licenseShort,
                        attributionRu = asset.attributionRu,
                        attributionEn = asset.attributionEn
                    });
                }
                else
                    merged.Add(resource);
            }

            return merged;
        }
    }
}
