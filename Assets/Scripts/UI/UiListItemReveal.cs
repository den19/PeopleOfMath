using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace PeopleOfMath.UI
{
    public static class UiListItemReveal
    {
        public const float ItemStagger = 0.04f;
        public const float ItemDuration = 0.22f;
        public const int MaxStaggeredItems = 12;

        struct RevealState
        {
            public CanvasGroup Group;
        }

        public static void HideImmediate(Transform item)
        {
            if (item == null)
                return;

            var group = GetOrAddCanvasGroup(item.gameObject);
            group.alpha = 0f;
        }

        public static Coroutine RevealStaggered(
            MonoBehaviour host,
            IEnumerable<Transform> items,
            float stagger = ItemStagger,
            float duration = ItemDuration,
            int maxStaggeredItems = MaxStaggeredItems)
        {
            if (host == null)
                return null;

            return host.StartCoroutine(RevealRoutine(items, stagger, duration, maxStaggeredItems));
        }

        static IEnumerator RevealRoutine(
            IEnumerable<Transform> items,
            float stagger,
            float duration,
            int maxStaggeredItems)
        {
            var states = new List<RevealState>();
            foreach (var item in items)
            {
                if (item == null)
                    continue;

                states.Add(new RevealState
                {
                    Group = GetOrAddCanvasGroup(item.gameObject)
                });
            }

            if (states.Count == 0)
                yield break;

            var maxAnimated = Mathf.Min(states.Count, maxStaggeredItems);
            var startTimes = new float[states.Count];
            for (var i = 0; i < states.Count; i++)
                startTimes[i] = i < maxAnimated ? i * stagger : (maxAnimated - 1) * stagger;

            var totalDuration = startTimes[states.Count - 1] + duration;
            var elapsed = 0f;

            while (elapsed < totalDuration)
            {
                elapsed += Time.unscaledDeltaTime;

                for (var i = 0; i < states.Count; i++)
                {
                    var state = states[i];
                    var localT = Mathf.Clamp01((elapsed - startTimes[i]) / duration);
                    var eased = 1f - Mathf.Pow(1f - localT, 3f);
                    state.Group.alpha = eased;
                }

                yield return null;
            }

            foreach (var state in states)
                state.Group.alpha = 1f;
        }

        static CanvasGroup GetOrAddCanvasGroup(GameObject go)
        {
            var group = go.GetComponent<CanvasGroup>();
            if (group != null)
                return group;

            return go.AddComponent<CanvasGroup>();
        }
    }
}
