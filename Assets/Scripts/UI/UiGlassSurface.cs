using UnityEngine;
using UnityEngine.UI;

namespace PeopleOfMath.UI
{
    [DisallowMultipleComponent]
    public class UiGlassSurface : MonoBehaviour
    {
        [SerializeField] Image targetImage;
        [SerializeField] bool useFrostedMaterial = true;
        [SerializeField] UiThemeToken tintToken = UiThemeToken.CardFill;

        Material _defaultMaterial;
        Outline _outline;
        bool _outlineWasEnabled;
        bool _glassActive;

        void Awake()
        {
            if (targetImage == null)
                targetImage = GetComponent<Image>();
        }

        public void ApplyGlass(bool enabled)
        {
            if (targetImage == null)
                targetImage = GetComponent<Image>();
            if (targetImage == null)
                return;

            if (enabled)
            {
                if (!_glassActive)
                    _defaultMaterial = targetImage.material;

                targetImage.color = Color.white;

                if (useFrostedMaterial)
                {
                    var material = GlassThemeAssets.FrostedGlassMaterial;
                    if (material != null)
                    {
                        targetImage.material = new Material(material);
                        targetImage.material.SetColor("_GlassTint", ResolveTintColor());
                    }
                }

                DisableOutlineForGlass();

                _glassActive = true;
                return;
            }

            RestoreOutlineAfterGlass();
            RestoreDefault();
        }

        void DisableOutlineForGlass()
        {
            if (_outline == null)
                _outline = GetComponent<Outline>();
            if (_outline == null)
                return;

            _outlineWasEnabled = _outline.enabled;
            _outline.enabled = false;
        }

        void RestoreOutlineAfterGlass()
        {
            if (_outline == null)
                return;

            _outline.enabled = _outlineWasEnabled;
        }

        public void RefreshTint()
        {
            if (!_glassActive || targetImage == null || targetImage.material == null)
                return;

            if (targetImage.material.HasProperty("_GlassTint"))
                targetImage.material.SetColor("_GlassTint", ResolveTintColor());
        }

        Color ResolveTintColor()
        {
            var binding = GetComponent<UiThemeBinding>();
            if (binding != null)
                return UiTheme.GetToken(binding.Token);

            return UiTheme.GetToken(tintToken);
        }

        void RestoreDefault()
        {
            if (targetImage == null)
                return;

            if (_glassActive && targetImage.material != null && targetImage.material != _defaultMaterial)
                Destroy(targetImage.material);

            targetImage.material = _defaultMaterial;
            _glassActive = false;
        }

        void OnDestroy()
        {
            if (_glassActive && targetImage != null && targetImage.material != null && targetImage.material != _defaultMaterial)
                Destroy(targetImage.material);
        }
    }
}
