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
        GridLayoutGroup _grid;
        RectTransform _rt;
        float _lastWidth = -1f;

        void Awake()
        {
            _grid = GetComponent<GridLayoutGroup>();
            _rt = transform as RectTransform;
        }

        void OnEnable() => Apply();

        void OnRectTransformDimensionsChange() => Apply();

        void LateUpdate()
        {
            if (_rt == null)
                return;

            var width = _rt.rect.width;
            if (!Mathf.Approximately(width, _lastWidth))
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

            var width = _rt.rect.width;
            if (width <= 1f)
                return;

            _lastWidth = width;

            var columns = Mathf.Max(1, CategoryTileMetrics.Columns);
            var spacing = CategoryTileMetrics.Spacing;
            var pad = _grid.padding.left + _grid.padding.right;
            var available = width - pad - spacing * (columns - 1);
            var cellW = available / columns;
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

                le.preferredWidth = cellW;
                le.preferredHeight = cellH;
                le.minWidth = cellW;
                le.minHeight = cellH;
            }

            LayoutRebuilder.MarkLayoutForRebuild(_rt);
        }
    }
}
