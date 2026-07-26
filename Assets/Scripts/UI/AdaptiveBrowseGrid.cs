using UnityEngine;
using UnityEngine.UI;

namespace PeopleOfMath.UI
{
    /// <summary>
    /// Fits a 2-column browse grid to the available width so the right column
    /// does not overflow on tall/narrow phone canvases.
    /// </summary>
    [RequireComponent(typeof(GridLayoutGroup))]
    public class AdaptiveBrowseGrid : MonoBehaviour
    {
        /// <summary>
        /// Sub-pixel / rounding slack so 2*cell + spacing never exceeds the rect.
        /// </summary>
        const float FitSlack = 2f;

        GridLayoutGroup _grid;
        RectTransform _rt;
        float _lastWidth = -1f;

        void Awake()
        {
            _grid = GetComponent<GridLayoutGroup>();
            _rt = transform as RectTransform;
        }

        void OnEnable() => Apply();

        void OnRectTransformDimensionsChange()
        {
            if (_rt == null)
                _rt = transform as RectTransform;
            if (_rt == null || Mathf.Approximately(ResolveWidth(), _lastWidth))
                return;

            Apply();
        }

        // Layout-driven width changes are not always reported via
        // OnRectTransformDimensionsChange (esp. first frame / CanvasScaler).
        void LateUpdate()
        {
            if (_rt == null)
                return;

            var width = ResolveWidth();
            if (width > 1f && !Mathf.Approximately(width, _lastWidth))
                Apply();
        }

        public void Apply()
        {
            if (_grid == null)
                _grid = GetComponent<GridLayoutGroup>();
            if (_rt == null)
                _rt = transform as RectTransform;
            if (_grid == null || _rt == null)
                return;

            var width = ResolveWidth();
            if (width <= 1f)
                return;

            _lastWidth = width;

            var columns = Mathf.Max(1, CategoryTileMetrics.Columns);
            var spacing = CategoryTileMetrics.Spacing;
            var pad = _grid.padding.left + _grid.padding.right;
            var available = width - pad - spacing * (columns - 1) - FitSlack;
            // Floor so column positions cannot round past the right edge.
            var cellW = Mathf.Floor(available / columns);
            if (cellW <= 1f)
                return;

            var aspect = CategoryTileMetrics.CellHeight / CategoryTileMetrics.CellWidth;
            var cellH = cellW * aspect;
            _grid.cellSize = new Vector2(cellW, cellH);
            _grid.spacing = new Vector2(spacing, spacing);
            _grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            _grid.constraintCount = columns;

            foreach (Transform child in transform)
            {
                var le = child.GetComponent<LayoutElement>();
                if (le == null)
                    continue;

                // Prefer/flexible only — fixed minWidth from the prefab (492)
                // can push preferred layout wider than the viewport.
                le.minWidth = -1f;
                le.minHeight = -1f;
                le.preferredWidth = cellW;
                le.preferredHeight = cellH;
            }

            LayoutRebuilder.MarkLayoutForRebuild(_rt);
        }

        float ResolveWidth()
        {
            // Prefer parent inner width: own rect can still hold the scene-serialized
            // sizeDelta before the VerticalLayoutGroup assigns the real width.
            if (_rt != null && _rt.parent is RectTransform parentRt)
            {
                var parentWidth = parentRt.rect.width;
                if (parentWidth > 1f)
                {
                    var parentPad = 0;
                    if (parentRt.TryGetComponent<HorizontalOrVerticalLayoutGroup>(out var layout))
                        parentPad = layout.padding.left + layout.padding.right;

                    var inner = parentWidth - parentPad;
                    if (inner > 1f)
                        return inner;
                }
            }

            return _rt != null ? _rt.rect.width : 0f;
        }
    }
}
