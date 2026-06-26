using UnityEngine;

namespace PeopleOfMath.UI
{
    [DisallowMultipleComponent]
    public class GlassThemeController : MonoBehaviour
    {
        const int BlurPasses = 3;
        const int ResolutionDivisor = 4;

        [SerializeField] GlassBackdropView backdropView;
        [SerializeField] Canvas rootCanvas;

        RenderTexture _blurredBackdrop;
        RenderTexture _blurScratch;
        bool _isActive;

        void Awake()
        {
            if (rootCanvas == null)
                rootCanvas = GetComponent<Canvas>() ?? GetComponentInParent<Canvas>();
        }

        void OnDisable()
        {
            TeardownGlass();
        }

        void OnDestroy()
        {
            ReleaseTextures();
        }

        public void Apply()
        {
            if (ThemeHelper.IsGlassmorphism)
                EnableGlass();
            else
                TeardownGlass();
        }

        public void ApplyGlassSurfaces()
        {
            var enabled = ThemeHelper.IsGlassmorphism;
            foreach (var surface in FindSurfaces())
            {
                surface.ApplyGlass(enabled);
                if (enabled)
                    surface.RefreshTint();
            }
        }

        UiGlassSurface[] FindSurfaces()
        {
            if (rootCanvas != null)
                return rootCanvas.GetComponentsInChildren<UiGlassSurface>(true);

            return GetComponentsInChildren<UiGlassSurface>(true);
        }

        void EnableGlass()
        {
            if (backdropView != null)
            {
                backdropView.gameObject.SetActive(true);
                backdropView.ApplyVisuals();
            }

            RebuildBlurredBackdrop();
            _isActive = true;
        }

        void TeardownGlass()
        {
            if (backdropView != null)
                backdropView.gameObject.SetActive(false);

            Shader.SetGlobalTexture(GlassThemeAssets.BackdropTexProperty, null);
            Shader.SetGlobalVector(GlassThemeAssets.BackdropTexelSizeProperty, Vector4.zero);
            ReleaseTextures();
            _isActive = false;
        }

        void RebuildBlurredBackdrop()
        {
            ReleaseTextures();

            if (rootCanvas == null)
                rootCanvas = GetComponent<Canvas>() ?? GetComponentInParent<Canvas>();

            var pixelRect = rootCanvas != null ? rootCanvas.pixelRect : new Rect(0, 0, Screen.width, Screen.height);
            var width = Mathf.Max(1, Mathf.RoundToInt(pixelRect.width) / ResolutionDivisor);
            var height = Mathf.Max(1, Mathf.RoundToInt(pixelRect.height) / ResolutionDivisor);

            _blurredBackdrop = new RenderTexture(width, height, 0, RenderTextureFormat.ARGB32)
            {
                name = "GlassBackdropBlurred",
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp
            };

            _blurScratch = new RenderTexture(width, height, 0, RenderTextureFormat.ARGB32)
            {
                name = "GlassBackdropScratch",
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp
            };

            if (backdropView != null)
                backdropView.BlitTo(_blurredBackdrop);
            else
                Graphics.Blit(GlassBackdropView.GradientTexture, _blurredBackdrop);

            ApplyBlurPasses();
            Shader.SetGlobalTexture(GlassThemeAssets.BackdropTexProperty, _blurredBackdrop);
            Shader.SetGlobalVector(GlassThemeAssets.BackdropTexelSizeProperty, new Vector4(
                1f / width,
                1f / height,
                width,
                height));
        }

        void ApplyBlurPasses()
        {
            var blurMaterial = GlassThemeAssets.BackdropBlurMaterial;
            if (blurMaterial == null || _blurredBackdrop == null || _blurScratch == null)
                return;

            for (var pass = 0; pass < BlurPasses; pass++)
            {
                var offset = 1f + pass;
                blurMaterial.SetFloat("_Offset", offset);
                Graphics.Blit(_blurredBackdrop, _blurScratch, blurMaterial);
                Graphics.Blit(_blurScratch, _blurredBackdrop, blurMaterial);
            }
        }

        void ReleaseTextures()
        {
            if (_blurredBackdrop != null)
            {
                _blurredBackdrop.Release();
                Destroy(_blurredBackdrop);
                _blurredBackdrop = null;
            }

            if (_blurScratch != null)
            {
                _blurScratch.Release();
                Destroy(_blurScratch);
                _blurScratch = null;
            }
        }

        void OnRectTransformDimensionsChange()
        {
            if (!_isActive || !ThemeHelper.IsGlassmorphism)
                return;

            RebuildBlurredBackdrop();
        }
    }
}
