using System;
using System.Collections.Generic;
using UnityEngine;

namespace PeopleOfMath.Data
{
    public static class FavoritesHelper
    {
        public const string PrefsKey = "favorite_mathematician_ids";

        public static event Action FavoritesChanged;

        static readonly List<string> _orderedIds = new();

        public static void Initialize()
        {
            _orderedIds.Clear();
            var raw = PlayerPrefs.GetString(PrefsKey, "");
            if (string.IsNullOrEmpty(raw))
                return;

            foreach (var part in raw.Split(','))
            {
                var id = part.Trim();
                if (!string.IsNullOrEmpty(id) && !_orderedIds.Contains(id))
                    _orderedIds.Add(id);
            }
        }

        public static bool IsFavorite(string id) =>
            !string.IsNullOrEmpty(id) && _orderedIds.Contains(id);

        public static void Toggle(string id)
        {
            if (string.IsNullOrEmpty(id))
                return;

            if (_orderedIds.Contains(id))
                _orderedIds.Remove(id);
            else
                _orderedIds.Add(id);

            Save();
            FavoritesChanged?.Invoke();
        }

        public static IReadOnlyList<string> GetOrderedIds() => _orderedIds;

        static void Save()
        {
            PlayerPrefs.SetString(PrefsKey, string.Join(",", _orderedIds));
            PlayerPrefs.Save();
        }
    }
}
