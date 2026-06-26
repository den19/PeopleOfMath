using UnityEngine;
using UnityEngine.UI;

namespace PeopleOfMath.UI
{
    public static class GlassThemeAssets
    {
        public const string BackdropTexProperty = "_GlassBackdropTex";
        public const string BackdropTexelSizeProperty = "_GlassBackdropTex_TexelSize";
        public const string FrostedGlassShaderName = "PeopleOfMath/UiFrostedGlass";
        public const string BackdropBlurShaderName = "PeopleOfMath/UiBackdropBlur";

        static Material _frostedGlassMaterial;
        static Material _backdropBlurMaterial;
        static Shader _frostedGlassShader;
        static Shader _backdropBlurShader;

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
                _frostedGlassShader = Shader.Find(FrostedGlassShaderName);
            if (_backdropBlurShader == null)
                _backdropBlurShader = Shader.Find(BackdropBlurShaderName);
        }
    }
}
