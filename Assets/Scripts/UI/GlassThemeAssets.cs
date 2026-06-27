using UnityEngine;

namespace PeopleOfMath.UI
{
    public static class GlassThemeAssets
    {
        public const string BackdropTexProperty = "_GlassBackdropTex";
        public const string BackdropTexelSizeProperty = "_GlassBackdropTex_TexelSize";
        public const string FrostedGlassShaderName = "PeopleOfMath/UiFrostedGlass";
        public const string BackdropBlurShaderName = "PeopleOfMath/UiBackdropBlur";

        const string FrostedGlassResourcePath = "UI/UiFrostedGlass";
        const string BackdropBlurResourcePath = "UI/UiBackdropBlur";

        static Material _frostedGlassMaterial;
        static Material _backdropBlurMaterial;
        static Shader _frostedGlassShader;
        static Shader _backdropBlurShader;
        static bool _warnedMissingShaders;

        public static Material FrostedGlassMaterial
        {
            get
            {
                EnsureLoaded();
                if (_frostedGlassMaterial == null && _frostedGlassShader != null)
                    _frostedGlassMaterial = new Material(_frostedGlassShader);
                return _frostedGlassMaterial;
            }
        }

        public static Material BackdropBlurMaterial
        {
            get
            {
                EnsureLoaded();
                if (_backdropBlurMaterial == null && _backdropBlurShader != null)
                    _backdropBlurMaterial = new Material(_backdropBlurShader);
                return _backdropBlurMaterial;
            }
        }

        static void EnsureLoaded()
        {
            if (_frostedGlassShader == null)
            {
                var frostedMat = Resources.Load<Material>(FrostedGlassResourcePath);
                _frostedGlassShader = frostedMat != null
                    ? frostedMat.shader
                    : Shader.Find(FrostedGlassShaderName);
            }

            if (_backdropBlurShader == null)
            {
                var blurMat = Resources.Load<Material>(BackdropBlurResourcePath);
                _backdropBlurShader = blurMat != null
                    ? blurMat.shader
                    : Shader.Find(BackdropBlurShaderName);
            }

            if (!_warnedMissingShaders
                && (_frostedGlassShader == null || _backdropBlurShader == null))
            {
                _warnedMissingShaders = true;
                Debug.LogWarning(
                    "Glass theme shaders were not found. Frosted glass UI will use a semi-transparent fallback. " +
                    $"Missing: {(_frostedGlassShader == null ? FrostedGlassShaderName : "")}" +
                    $"{(_frostedGlassShader == null && _backdropBlurShader == null ? ", " : "")}" +
                    $"{(_backdropBlurShader == null ? BackdropBlurShaderName : "")}");
            }
        }
    }
}
